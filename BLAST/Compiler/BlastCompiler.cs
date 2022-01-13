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
using Unity.Mathematics;
using NSS.Blast.Standalone;
using Unity.Assertions;

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
            
            // package bytecode into the intermediate 
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

            // make sure offsets are set 
            if (result.HasVariables && !result.HasOffsets || result.VariableCount != result.OffsetCount)
            {
                result.CalculateVariableOffsets();
                if (result.VariableCount != result.OffsetCount)
                {
                    result.LogError("validate: failure calculating variable offsets");
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

                    f_a = result.Executable.GetDataSegmentElement(result.Offsets[v_a.Id]);

                    // b can be a float or a parameter 
                    if (float.IsNaN(f_b = node.AsFloat(b)))
                    {
                        // try to get as parameter 
                        var v_b = result.GetVariable(b);
                        f_b = result.Executable.GetDataSegmentElement(result.Offsets[v_b.Id]);
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
                if (result.root != null && result.root.CheckIfFunctionIsUsedInTree(script_op.yield))
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
        /// Package in normal mode. 
        /// 
        /// - packagemode       = NORMAL
        /// - languageversion   = BS1
        /// </summary>
        /// <param name="cdata">Compiler data</param>
        /// <param name="allocator">Allocation type</param>
        /// <param name="code_size">size of code in bytes</param>
        /// <param name="metadata_size">size of metadata in bytes, 1 byte per data element in data and stack segment combined</param>
        /// <param name="data_size">size of datasegment in bytes</param>
        /// <param name="stack_size">size of stacksegement in bytes</param>
        /// <param name="flags">flags: alignment, stack</param>
        /// <returns>blast package data</returns>
        static BlastPackageData PackageNormal(CompilationData cdata, Allocator allocator, int code_size, int metadata_size, int data_size, int stack_size, BlastPackageFlags flags = BlastPackageFlags.None)
        {
            Assert.IsTrue(metadata_size == (data_size + stack_size) / 4, 
                $"PackageNormal: metadata size misaligned with combined datastack size, there are {metadata_size} entries in metadata while there are {((data_size + stack_size) / 4)} combined data/stack entries.");

            bool a8 = (flags & BlastPackageFlags.Aligned8) == BlastPackageFlags.Aligned8;
            bool a4 = !a8 && (flags & BlastPackageFlags.Aligned4) == BlastPackageFlags.Aligned4;
            bool has_stack = (flags & BlastPackageFlags.NoStack) == BlastPackageFlags.NoStack;

            // align data offsets 
            int alignto = (a4 | a8) ? (a4 ? 4 : 8) : 0;
            int align_data = 0;
            int align_stack = 0;

            if (a4 || a8)
            {
                // align data offset 
                while (((code_size + metadata_size + align_data) % alignto) != 0) align_data++;

                // if a4, then no need to align stackoffset because each dataelement is 32bit
                if (a8)
                {
                    // align stack offset to 64bit boundary
                    while (((code_size + metadata_size + align_data + data_size + align_stack) % alignto) != 0) align_stack++;
                }
            }

            // calculate package size including alignments 
            int package_size = code_size + metadata_size + align_data + data_size + align_stack + stack_size;

            /// [----CODE----|----METADATA----|----DATA----|----STACK----]
            ///              1                2            3             4  
            /// 
            /// ** ALL OFFSETS IN PACKAGE IN BYTES ***
            /// 
            /// 1 = metadata offset   | codesize 
            /// 2 = data_offset       | codesize + metadatasize 
            /// 3 = stack_offset 
            /// 4 = package_size  

            BlastPackageData data = default;
            data.PackageMode = BlastPackageMode.Normal;
            data.Flags = flags;
            data.LanguageVersion = BlastLanguageVersion.BS1;
            data.O1 = (ushort)code_size;
            data.O2 = (ushort)(code_size + metadata_size + align_data);
            data.O3 = (ushort)(code_size + metadata_size + align_data + data_size + align_stack);
            data.O4 = (ushort)package_size;
            data.P2 = IntPtr.Zero;

            // allocate 
            unsafe
            {
                data.CodeSegmentPtr = new IntPtr(UnsafeUtils.Malloc(package_size, 8, allocator));
                data.Allocator = (byte)allocator;

                // copy code segment 
                int i = 0;
                while (i < code_size)
                {
                    data.Code[i] = cdata.Executable.code[i]; 
                    i++;
                }

                // copy metadata for variables 
                i = 0;
                while (i < cdata.Executable.data_count)
                {
                    data.Metadata[i] = cdata.Executable.metadata[i];
                    i++; 
                }
                while (i < metadata_size)
                {
                    // fill stack metadata with 0's
                    data.Metadata[i] = 0;
                    i++; 
                }

                // copy initial variable data => datasegment 
                i = 0;
                while (i < cdata.Executable.data_count)
                {
                    data.Data[i] = cdata.Executable.data[i];
                    i++;
                }

                // fill stack with NaNs
                if (has_stack && stack_size > 0)
                {
                    i = 0;
                    while (i < math.floor(stack_size / 4f)) // could be we have odd bits if not aligned 
                    {
                        data.Stack[i] = float.NaN;
                        i++;
                    }
                }
            }

            // return the datapackage 
            return data; 
        }




        /// <summary>
        /// estimate size of package needed to execute the script
        /// </summary>
        /// <returns>size in bytes</returns>
        static public BlastPackageData Package(CompilationData result, BlastCompilerOptions options)
        {
            BlastPackageMode package_mode = options.PackageMode; 

            // get sizes of seperate segments 
            int stack_size = EstimateStackSize(result);
            int code_size = result.Executable.code_size;
            int data_size = result.Executable.data_count * 4;
            int metadata_size = result.Executable.data_count * 1; 

            switch (package_mode)
            {
                case BlastPackageMode.Normal:

                    // code - meta - data - stack 
                    return PackageNormal(result, Allocator.Persistent,
                                         code_size,
                                         metadata_size,
                                         data_size,
                                         stack_size);

                case BlastPackageMode.Compiler:
                case BlastPackageMode.Entity:
                case BlastPackageMode.SSMD:
                    Debug.LogError($"BlastCompiler.Package: package mode {package_mode} not supported");
                    break;
            }
            return default;
        }

        #endregion

        #region compile 

        static public CompilationData Compile(Blast blast, string code, BlastCompilerOptions options = null)
        {
            return Compile(blast, BlastScript.FromText(code), options);
        }

        static public CompilationData Compile(string code, BlastCompilerOptions options = null)
        {
            return Compile(BlastScript.FromText(code), options);
        }

        static public CompilationData Compile(BlastScript script, BlastCompilerOptions options = null)
        {
            Blast blast = Blast.Create(Allocator.Temp);
            try
            {
                return Compile(blast, script, options);
            }
            finally
            {
                blast.Destroy();
            }
        }       

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
                    if (options.Verbose || options.Trace)
                    {
                        Standalone.Debug.Log(result.root.ToNodeTreeString());
                        Standalone.Debug.Log(result.GetHumanReadableCode() + "\n");
                        Standalone.Debug.Log(result.GetHumanReadableBytes() + "\n");
                    }
                    return result;
                }
            }

            if (options.Verbose || options.Trace)
            {
                Standalone.Debug.Log(result.root.ToNodeTreeString());
                Standalone.Debug.Log(result.GetHumanReadableCode() + "\n");
                Standalone.Debug.Log(result.GetHumanReadableBytes() + "\n");

           //     Debug.Log(Blast.GetReadableByteCode(result.Executable.code, result.Executable.code_size); 
            }

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
                    if (options.Verbose || options.Trace)
                    {
                        Standalone.Debug.Log(result.root.ToNodeTreeString());
                        Standalone.Debug.Log(result.GetHumanReadableCode());
                    }
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
                    if (options.Verbose || options.Trace)
                    {
                        Standalone.Debug.Log(result.root.ToNodeTreeString());
                        Standalone.Debug.Log(result.GetHumanReadableCode());
                    }
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
                BlastScriptPackage pkg = new BlastScriptPackage()
                {
                    Package = Package(result, options),
                    Variables = result.Variables.ToArray(),
                    VariableOffsets = result.Offsets.ToArray(),
                    Inputs = result.Inputs.ToArray(),
                    Outputs = result.Outputs.ToArray()
                };

                return pkg;
            }
            else
            {
                Standalone.Debug.LogError(result.LastErrorMessage);
                return null;
            }
        }

        #endregion 

    }

}