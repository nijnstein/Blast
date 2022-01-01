#if UNITY_EDITOR
#define LOG_ERRORS // this forces inclusion of burststring in script exe, disable in relaese
#define CHECK_STACK
#define SHOW_TODO  // show todo warnings 
// #define MULTITHREADED
#endif
 
using NSS.Blast.Interpretor;
using NSS.Blast.Compiler.Stage;
using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;

namespace NSS.Blast.Compiler
{
    /// <summary>
    /// a compiled bytecode script fragment executable from burst with input and output variables and a small stack    
    /// 
    /// - no functions as parameters                (not yet)
    /// - no if {} then {}                          (not yet)
    /// - no functions                              (wont)    -> any function should be either hardcoded or scripted seperately and added to the interpretor    
    /// - no strings                                (never)   -> no burst support anyway
    /// - no arrays                                 (future)
    /// - foreach and while loops                   (future) 
    /// - stack functions                           (not yet)
    /// - external data structures via . notation   (future)
    /// - vector operations (float3 and float4)     (future)
    /// 
    /// - hard limits: 
    /// -- max combined variable & constant count:  64 floats       64x4    =   256 byte
    /// -- max code size:                           512 byte        512     =   512 byte  
    /// -- max stack size:                          32 floats       32x4    =   128 byte
    ///                                                             total   =   786 bytes without other parameters (counts and ids and call refs (for functions))
    /// 
    /// from:  https://software.intel.com/content/www/us/en/develop/articles/align-and-organize-data-for-better-performance.html
    /// 
    /// Align data on natural operand size address boundaries.If the data will be accessed with vector instruction loads and stores, align the data on 16-byte boundaries. For best performance, align data as follows:
    ///
    /// Align 8-bit data at any address.
    /// Align 16-bit data to be contained within an aligned four-byte word.
    /// Align 32-bit data so that its base address is a multiple of four.
    /// Align 64-bit data so that its base address is a multiple of eight.
    /// Align 80-bit data so that its base address is a multiple of sixteen.
    /// Align 128-bit data so that its base address is a multiple of sixteen.
    /// 
    /// In addition, pad data structures defined in the source code so that every data element is aligned to a 
    /// natural operand size address boundary. If the operands are packed in a SIMD instruction, 
    /// align to the packed element size (64- or 128-bit). Align data by providing padding inside structures and 
    /// arrays. Programmers can reorganize structures and arrays to minimize the amount of memory wasted by padding.
    /// 
    /// 
    /// optimization help:
    /// https://gametorrahod.com/analyzing-burst-generated-assemblies/
    /// 
    /// </summary>


    /// <summary>
    /// Options for compiling and packaging scripts 
    /// </summary>
    public class BlastCompilerOptions
    {
        /// <summary>
        /// run validation if set in script 
        /// </summary>
        public bool AutoValidate = false;

        /// <summary>
        /// run code optimizer 
        /// </summary>
        public bool Optimize = true;

        /// <summary>
        /// compile script using constant data fields 
        /// </summary>
        public bool CompileWithSystemConstants = true;

        /// <summary>
        /// compare epsilon for constants, on fast float mode it wont match on float.epsilon
        /// </summary>
        public float ConstantEpsilon = 0.001f;     // in fast floating point mode it wont match constants on float.epsilon
        public string ConstantPrecision = "0.000"; // precision used when using constant values as identifier names, everything is rounded to this 

        /// <summary>
        /// default stack size in number of bytes
        /// - overridden if the script defines a stack size 
        /// </summary>
        public int DefaultStackSize = 16;

        /// <summary>
        /// estimate stack size using input, output and validation parameters 
        /// - overridden if the script defines a stack size
        /// </summary>
        public bool EstimateStackSize = true;

        /// <summary>
        /// verbose report logging, log is also blitted to unity
        /// </summary>
        public bool Verbose = true;

        /// <summary>
        /// enable trace level logging -> will generate a massive report 
        /// </summary>
        public bool Trace = true;

        /// <summary>
        /// build a report during compilation 
        /// </summary>
        public bool Report = true;

        /// <summary>
        /// if yield is not emitted we can save 20 bytes on the stack, with very small stuff that might be handy
        /// not implemented .. todo
        /// </summary>
        public bool SupportYield = true;

        /// <summary>
        /// package mode
        /// </summary>
        public BlastPackageMode PackageMode = BlastPackageMode.CodeDataStack;

        public Allocator PackageAllocator = Allocator.Temp;

        /// <summary>
        /// enable for partially parallel compilation 
        /// - fails somewhere in optimizer or jumpresolver 
        /// </summary>
        public bool ParallelCompilation = false;

        /// <summary>
        /// additional compiler defines 
        /// </summary>
        public Dictionary<string, string> Defines = new Dictionary<string, string>();

        /// <summary>
        /// default compiler options 
        /// </summary>
        public static BlastCompilerOptions Default => new BlastCompilerOptions();

        /// <summary>
        /// add a compiler define 
        /// </summary>
        /// <param name="key">the key value</param>
        /// <param name="value">single, or parameter name when brave</param>
        public void AddDefine(string key, string value)
        {
            key = key.ToLower().Trim();
            if (!Defines.ContainsKey(key))
            {
                // set new
                Defines.Add(key, value);
            }
            else
            {
                // update existing
                Defines[key] = value;
            }
        }
    }



    /// <summary>
    /// Blast Compiler 
    /// 
    /// Compile scripts into bytecode, the process consists of several stages:
    /// 
    /// 1   - tokenize:            convert code into a list of tokens 
    /// 2   - parse:               parse list of tokens into a tree of nodes 
    /// 3   - transform:           transform nodes sequences  
    /// 4.1 - analyse parameters:  determine parameter types and check inputs and outputs
    /// 4.2 - analyse:             analyse nodes, reorder were needed, optimize for execution
    /// 5   - flatten:             flatten execution path
    /// 6   - compile:             generate bytecode from the internal representation 
    /// 7   - optimize:            optimize bytecode patterns 
    /// 8   - package:             package bytecode and set it up for execution 
    ///  
    /// Grammer: 
    /// 
    /// - identifiers seperated by operations, control characters (); and whitespace
    /// - variables and functions are identifiers, functions must be known during compiling
    /// - numerics are . seperated identifiers with 2 elements and no whitespace [whitespace is not maintained..]
    /// - operators: - + / * & | ^ < > = != !     todo: %
    /// - identifier: [a..z]([a..z|0..9])*
    /// - statement: sequence terminated with ; 
    /// 
    /// Flow:  
    /// 
    /// >> a sequence is a list of functions, operations and variables terminated with a ; 
    /// >> a statement is a function call or assigment
    /// >> a compound is a collection of statements contained within ( .. ) 
    /// 
    /// - sequence = [function/op/var]* ;
    /// - statement = [function(sequence)] / [assignment(sequence)] / [controlflow(condition, compound)]
    /// - compound = ([statement]*)
    /// 
    /// - nesting in: functions, conditions, ifthenelse while and switch is supported
    /// - assignment: var = sequence = var op var op func( [sequence] );
    /// - condition: var = var | func( compound );
    /// - if (condition) then ( compound ) else ( compound ); 
    /// - while(condition) ( compound )
    /// 
    /// 
    /// 
    /// </summary>
    static public class BlastCompiler
    {
       // do not change! without this cow code wont compile
       //   ^__^
       //   (oo)\_______
       //   (__)\       )\/\
       //       ||----w |
       //       ||     ||
        public const byte opt_value = (byte)script_op.pi;
        public const byte opt_ident = (byte)script_op.id;

        /// <summary>
        /// Default compilation setup 
        /// </summary>
        public static List<IBlastCompilerStage> ByteCodeStages = new List<IBlastCompilerStage>
        {
            // convert into token array, build names of identifiers
            new BlastTokenizer(),
            
            // parse nodes from tokens, map out identifiers
            new BlastParser(),
            
            // transform constructs into simpler ones
            new BlastTransform(), 
            
            // analyse parameter use
            new BlastParameterAnalysis(),
            
            // analyse ast nodes, apply rules and optimizations to its structure 
            new BlastAnalysis(),
            
            // flatten node structure into a linear path 
            new BlastFlatten(),
            
            // cleanup unreference parameters, some might not be used anymore
            new BlastPreCompileCleanup(),
            
            // compile the ast into bytecode
            new BlastBytecodeCompiler(),
            
            // use pattern recognition to transfrom sequences of bytecode into more efficient ones 
            new BlastBytecodeOptimizer(),
            
            // resolve jumps 
            new BlastJumpResolver(),
            
            // package bytecode
            new BlastPackaging()
        };

        /// <summary>
        /// compilation stages for compilation into a job,
        /// - use the same precompilation, make sure behaviour is equal even if some unknown bug is there
        /// </summary>
        public static List<IBlastCompilerStage> HPCStages = new List<IBlastCompilerStage>
        {
            new BlastTokenizer(),
            new BlastParser(),
            new BlastTransform(),
            new BlastParameterAnalysis(),
            new BlastAnalysis(),
            new BlastFlatten(),
            new BlastPreCompileCleanup(),
            // transform stack operation like push/pop into stack-variables 
            // new BlastHPCStackResolver(), // or just use stack ops... this was we kill yields 
            new BlastHPCCompiler()
        };

        /// <summary>
        /// C# compilation uses the output from the hpc stage
        /// </summary>
        public static List<IBlastCompilerStage> CSStages = new List<IBlastCompilerStage>
        {
            new BlastTokenizer(),
            new BlastParser(),
            new BlastTransform(),
            new BlastParameterAnalysis(),
            new BlastAnalysis(),
            new BlastFlatten(),
            new BlastPreCompileCleanup(),
            new BlastHPCStackResolver(),
            new BlastHPCCompiler(),
            // compile the output into a function pointer [windows only]
            new BlastCSCompiler()
        };


        #region validation 

        static bool validate(CompilationData result)
        {
            Blast blast = Blast.Create(Unity.Collections.Allocator.Persistent);
            blast.SyncConstants();

            bool b_result = Validate(result, blast.Engine);

            blast.Destroy();

            return b_result;
        }

        static public bool Validate(CompilationData result, IntPtr blast)
        {
            if (!result.CompilerOptions.EstimateStackSize)
            {
                if (!result.CanValidate)
                {
                    result.LogError($"validate: the script defines no validations, dont call validate if this was intentional");
                    return false;
                }
            }

            result.Executable.Validate(blast);

            // check results 
            if (result.Validations.Count > 0)
            {
                foreach (var validation in result.Validations)
                {
                    string a = validation.Key;
                    string b = validation.Value;

                    float f_a = 0, f_b = 0;

                    // a must be a parameter
                    var v_a = result.GetVariable(a);
                    if (v_a == null)
                    {
                        result.LogError($"Validation failed on: {a} = {b}, could not locate parameter {a}");
                        continue;
                    }

                    // determine vector size...... from get_datasize 
                    f_a = result.Executable.get_float(result.Offsets[v_a.Id]);

                    // b can be a float or a parameter 
                    if (float.IsNaN(f_b = node.AsFloat(b)))
                    {
                        // try to get as parameter 
                        var v_b = result.GetVariable(b);
                        f_b = result.Executable.get_float(result.Offsets[v_b.Id]);
                    }

                    // compare validations using the epsilon set in compiler options 
                    if (f_a < f_b - result.CompilerOptions.ConstantEpsilon || f_a > f_b + result.CompilerOptions.ConstantEpsilon)
                    {
                        result.LogError($"Validation failed on rule: {a} == {b} ({f_a} != {f_b})");
                        continue; 
                    }
                }
            }

            return result.Success;
        }

        #endregion

        #region packaging 

        /// <summary>
        /// estimate stack size by running script with a selection of parameters from
        /// input, output and validation settings 
        /// </summary>
        /// <returns>estimated stack size in bytes</returns>
        static unsafe public int EstimateStackSize(CompilationData result)
        {
            int yield = 0; 
            int max_size = 0;

            BlastIntermediate exe = result.Executable;

            // should add 20 bytes / 5 floats if supporting yield 
            if(result.CompilerOptions.SupportYield)
            {
                if (result.root != null && result.root.check_if_function_is_used_in_tree(script_op.yield))
                {
                    yield = 20;
                    result.LogTrace("Package.EstimateStackSize: reserving 20 bytes of stack for supporting Yield.");
                }
            }

            if (result.CompilerOptions.EstimateStackSize)
            {
                // stack size was estimated during validation or in a seperate run 
                max_size = result.Executable.max_stack_size * 4;
                result.LogTrace($"Package.EstimateStackSize: estimated {max_size} bytes of stack use from compiled output");
            }
            else
            {
                if(result.CompilerOptions.DefaultStackSize > 0)
                {
                    result.LogTrace($"Package.EstimateStackSize: compiler options dictate {max_size} bytes of stack use");
                }

                max_size = math.max(result.Executable.max_stack_size * 4, result.CompilerOptions.DefaultStackSize);
            }

            // account for possible yield 
            max_size += yield; 

            // check defines 
            string s_defined_size;
            if (result.Defines.TryGetValue("stack_size", out s_defined_size))
            {
                int i_defined_size;
                if (int.TryParse(s_defined_size, out i_defined_size))
                {
                    max_size = math.max(max_size, i_defined_size);

                    if (result.CompilerOptions.DefaultStackSize > 0)
                    {
                        result.LogTrace($"Package.EstimateStackSize: script defines updated stack_size to {max_size}");
                    }
                 }
            }
            else
            {
                // if not defined in script only then use value from compiler defines 
                if (result.CompilerOptions.Defines.TryGetValue("stack_size", out s_defined_size))
                {
                    int i_defined_size;
                    if (int.TryParse(s_defined_size, out i_defined_size))
                    {
                        max_size = math.max(max_size, i_defined_size);

                        if (result.CompilerOptions.DefaultStackSize > 0)
                        {
                            result.LogTrace($"Package.EstimateStackSize: compiler defines updated stack_size to {max_size}");
                        }
                    }
                }
            }

            if (result.CompilerOptions.DefaultStackSize > 0 && max_size > result.CompilerOptions.DefaultStackSize)
            {
                result.LogWarning($"Package.EstimateStackSize: compiler options dictate {result.CompilerOptions.DefaultStackSize} bytes of stack use but the script uses at least {max_size} bytes, minimal stacksize as been increased to {max_size}");
            }

            return max_size;
        }


        /// <summary>
        /// estimate size of package needed to execute the script
        /// </summary>
        /// <returns>size in bytes</returns>
        static public unsafe BlastPackageInfo EstimatePackageInfo(CompilationData result, BlastPackageMode package_mode)
        {
            // target package sizes, if larger then allocation dynamic 
            int[] target_sizes = new int[] {
                (int)BlastPackageCapacities.c32,
                (int)BlastPackageCapacities.c48,
                (int)BlastPackageCapacities.c64,
                (int)BlastPackageCapacities.c96,
                (int)BlastPackageCapacities.c128,
                (int)BlastPackageCapacities.c192,
                (int)BlastPackageCapacities.c256,
                (int)BlastPackageCapacities.c320,
                (int)BlastPackageCapacities.c384,
                (int)BlastPackageCapacities.c448,
                (int)BlastPackageCapacities.c512,
                (int)BlastPackageCapacities.c640,
                (int)BlastPackageCapacities.c768,
                (int)BlastPackageCapacities.c896,
                (int)BlastPackageCapacities.c960
            };

            // get sizes of seperate segments 
            int stack_size = EstimateStackSize(result);
            int code_size = result.Executable.code_size;
            int data_size = result.Executable.data_offset * 4;

            // total package size without size of datasizes[] 
            int package_size = 0;

            switch (package_mode)
            {
                case BlastPackageMode.CodeDataStack:
                    package_size = stack_size + code_size + data_size + 12; // max 3x 4 byte align  // + 20 for yield support 
                    break;

                case BlastPackageMode.Code:
                    package_size = code_size;
                    break;

                default:
                    result.LogError($"BIG TODO PACKAG MODE {package_mode}"); 
                    break;
            }

            // size of datasizes[] depends on data capacity which is the left over space in the given package size

            int target_size = 0;

            for (int i = 0; i < target_sizes.Length; i++)
            {
                if (package_mode == BlastPackageMode.CodeDataStack)
                {
                    // need to fit code, data and stack 
                    int target_data_sizes = (target_sizes[i] - code_size) / 4;
                    if (target_data_sizes < 0) continue;

                    int minimal_package_size = package_size + target_data_sizes;

                    if (minimal_package_size < target_sizes[i])
                    {
                        target_size = target_sizes[i];
                        break;
                    }
                }
                else
                {
                    // need only to fit the code
                    if (package_size < target_sizes[i])
                    {
                        target_size = target_sizes[i];
                        break;
                    }
                }
            }

            if (target_size == 0)
            {
                // on var size mode determines package size
                switch (package_mode)
                {
                    case BlastPackageMode.Code:
                        package_size = code_size;
                        break;
                    case BlastPackageMode.CodeDataStack:
                        package_size = package_size + (stack_size + data_size) / 4;
                        break;
                }

                // variable package size -> outside allocation 
                return new BlastPackageInfo()
                {
                    package_mode = package_mode,
                    code_size = (ushort)code_size,
                    data_size = (ushort)data_size,
                    package_size = (ushort)package_size,
                    stack_size = (ushort)stack_size,
                    allocator = (byte)Allocator.None
                };
            }
            else
            {
                return new BlastPackageInfo()
                {
                    package_mode = package_mode,
                    code_size = (ushort)code_size,
                    data_size = (ushort)data_size,
                    package_size = (ushort)target_size,
                    stack_size = (ushort)stack_size,
                    allocator = (byte)Allocator.Invalid
                };
            }
        }

        static unsafe public BlastPackage512 Package512(CompilationData result, BlastPackageInfo info)
        {
            BlastPackage512 package = default(BlastPackage512);

            Package(result, info, (byte*)(void*)&package);

            return package;
        }

        static unsafe public BlastPackagePtr* Package(CompilationData result, BlastPackageInfo info, byte* package_ptr)
        {
            // cast to generic package and set information 
            BlastPackage* p = (BlastPackage*)package_ptr;
            p->info = info;
            p->code_pointer = 0;

            // get a pointer to the data buffer 
            BlastPackagePtr* ptr = (BlastPackagePtr*)package_ptr;
            byte* p_segment = (byte*)p + sizeof(BlastPackage);

            BlastIntermediate exe = result.Executable;

            /// the script buffer:
            /// [....code...|...variables&constants...|.datasizes.|...stack.....................]

            int i = 0;
            while (i < exe.code_size)
            {
                p_segment[i] = exe.code[i];  //todo memset... 
                i++;
            }
            
            if(info.package_mode == BlastPackageMode.Code)
            {
                // code mode
                return ptr; 
            }

            // align data segment on 4 byte boundary from code segment start
            //  
            // - have to account for this while estimating buffer sizes... always + 12?? 

            while (i % 4 != 0) { p_segment[i] = 0; i++; }
            p->data_start = i;

            // copy data 
            int j = 0;
            byte* p_data = (byte*)((void*)exe.data);
            while (j < exe.data_offset * 4)
            {
                p_segment[i] = p_data[j];        //   todo   memset.. 
                i++;
                j++;
            }

            // todo

            while (i < p->info.package_size)
            {
                p_segment[i] = 0;  /// todo  memset
                i++;
            }

            p->data_sizes_start = i;
            return ptr; 

            // fixed capacity niet handig als we van pointers op maat afgaan....


            // copy data-sizes

            int k = 0;
            while (k < BlastIntermediate.data_capacity / 4)
            {
                p_segment[i] = exe.data_sizes[k];    // memset ?? todo 
                i++;
                k++;
            }

            // align stack segment on 4 byte boundary
            while (i % 4 != 0) { p_segment[i] = 0; i++; }
          
            p->data_offset = i;
            p->stack_offset = p->data_offset;

            // zero the rest of the segment 
            while (i < p->info.package_size)
            {
                p_segment[i] = 0;  /// todo  memset
                i++;
            }

            // that should be it... 
            return ptr;
        }

        static unsafe BlastPackage* Package(CompilationData result, BlastCompilerOptions options)
        {
            if (!result.Success) return null;

            BlastPackageInfo package_info = EstimatePackageInfo(result, options.PackageMode);

            // force allocation as we dont have a ref 
            package_info.allocator = (byte)options.PackageAllocator;

            switch (package_info.package_mode)
            {
                case BlastPackageMode.CodeDataStack:
                case BlastPackageMode.Code:
                    {
                        BlastPackage* package_ptr = (BlastPackage*)UnsafeUtility.Malloc(32 + (int)package_info.package_size, 4, options.PackageAllocator);
                        Package(result, package_info, (byte*)package_ptr);

#if !NOT_USING_UNITY && DEBUG
                        BlastPackage* pkg = package_ptr; 
                        Debug.Log($"<blastcompiler.package> package size: {package_info.package_size}, code size: {pkg->info.code_size}, data size: {pkg->info.data_size}, metadata size: {pkg->info.data_sizes_size}, data start: {pkg->data_start}, data offset: {pkg->data_offset}, stack offset: {pkg->stack_offset}, allocator: {options.PackageAllocator}");
#endif

                        return package_ptr; 
                    }

                default:
                    result.LogError($"BIG TODO PACKAG MODE");
                    break;

            }

            result.LogError("package: only code data stack package mode is supported ");
            return null;
        }


        #endregion


        static public CompilationData Compile(Blast blast, BlastScript script, BlastCompilerOptions options = null)
        {
            if (options == null) options = BlastCompilerOptions.Default;

            CompilationData result = new CompilationData(blast, script, options);

            // execute each compilation stage 
            for(int istage = 0; istage < ByteCodeStages.Count; istage++)
            {
                int exitcode = ByteCodeStages[istage].Execute(result);

                if (exitcode != (int)BlastError.success)
                {
                    // error condition 
                    result.LogError($"Compilation Error, stage: {istage}, type {ByteCodeStages[istage].GetType().Name}, exitcode: {exitcode}");
#if !NOT_USING_UNITY && DEBUG
                    if (options.Verbose || options.Trace)
                    {
                        UnityEngine.Debug.Log(result.root.ToNodeTreeString());
                        UnityEngine.Debug.Log(result.GetHumanReadableCode());
                    }
#endif
                    return result;
                }
            }


#if !NOT_USING_UNITY && DEBUG
            if (options.Verbose || options.Trace)
            {
                UnityEngine.Debug.Log(result.root.ToNodeTreeString());
                UnityEngine.Debug.Log(result.GetHumanReadableCode());
            }
#endif

            // run validations if any are defined
            if (result.CompilerOptions.EstimateStackSize || result.CompilerOptions.AutoValidate && result.CanValidate)
            {
                validate(result);
            }

            return result;
        }

        /// <summary>
        /// compile to hpc code 
        /// </summary>
        /// <param name="blast"></param>
        /// <param name="blastScript"></param>
        /// <param name="blastCompilerOptions"></param>
        /// <returns></returns>
        public static HPCCompilationData CompileHPC(Blast blast, BlastScript script, BlastCompilerOptions options)
        {
            if (options == null) options = BlastCompilerOptions.Default;

            HPCCompilationData result = new HPCCompilationData(blast, script, options);

            for (int istage = 0; istage < HPCStages.Count; istage++)
            {
                int exitcode = HPCStages[istage].Execute(result);

                if (exitcode != (int)BlastError.success)
                {
                    result.LogError($"Compilation Error, stage: {istage}, type {HPCStages[istage].GetType().Name}, exitcode: {exitcode}");
#if !NOT_USING_UNITY && DEBUG
                    if (options.Verbose || options.Trace)
                    {
                        UnityEngine.Debug.Log(result.root.ToNodeTreeString());
                        UnityEngine.Debug.Log(result.GetHumanReadableCode());
                    }
#endif
                    return result;
                }
            }
           
            return result;
        }

        /// <summary>
        /// Compile using .net c# compiler 
        /// - only on windows .net framework > 4.5 
        /// </summary>
        /// <param name="blast"></param>
        /// <param name="script"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public static HPCCompilationData CompileCS(Blast blast, BlastScript script, BlastCompilerOptions options)
        {
            if (options == null) options = BlastCompilerOptions.Default;

            CSCompilationData result = new CSCompilationData(blast, script, options);

            for (int istage = 0; istage < CSStages.Count; istage++)
            {
                int exitcode = CSStages[istage].Execute(result);

                if (exitcode != (int)BlastError.success)
                {
                    result.LogError($"Compilation Error, stage: {istage}, type {CSStages[istage].GetType().Name}, exitcode: {exitcode}");
#if !NOT_USING_UNITY
                    if (options.Verbose || options.Trace)
                    {
                        UnityEngine.Debug.Log(result.root.ToNodeTreeString());
                        UnityEngine.Debug.Log(result.GetHumanReadableCode());
                    }
#endif
                    return result;
                }
            }

            return result;
        }


        unsafe static public BlastScriptPackage CompilePackage(Blast blast, BlastScript script, BlastCompilerOptions options = null)
        {
            if (options == null) options = new BlastCompilerOptions();

            CompilationData result = Compile(blast, script, options);
            if (result.Success)
            {
                //return CompilePackage(result, EstimatePackageInfo(result, options.PackageMode), options);
                BlastPackage* ptr = Package(result, options);

                BlastScriptPackage pkg = new BlastScriptPackage()
                {
                    PackagePtr = (IntPtr)ptr,
                    Variables = result.Variables.ToArray(),
                    VariableOffsets = result.Offsets.ToArray(),
                    Inputs = result.Inputs.ToArray(),
                    Outputs = result.Outputs.ToArray()
                };

                return pkg;
            }
            else
            {
#if !NOT_USING_UNITY
                UnityEngine.Debug.LogError(result.LastErrorMessage);
#else
                System.Diagnostics.Debug.WriteLine("ERROR: " + result.LastErrorMessage);
#endif
                return null;
            }
        }

    }

}