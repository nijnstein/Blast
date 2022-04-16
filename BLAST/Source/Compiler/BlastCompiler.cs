//############################################################################################################################
// BLAST v1.0.4c                                                                                                             #
// Copyright © 2022 Rob Lemmens | NijnStein Software <rob.lemmens.s31 gmail com> All Rights Reserved                   ^__^\ #
// Unauthorized copying of this file, via any medium is strictly prohibited proprietary and confidential               (oo)\ #
//                                                                                                                     (__)  #
//############################################################################################################################

#if STANDALONE_VSBUILD
using NSS.Blast.Standalone;
    using UnityEngine.Assertions;
#else
    using UnityEngine;
    using UnityEngine.Assertions;
#endif

using NSS.Blast.Compiler.Stage;
using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;

namespace NSS.Blast.Compiler
{
    /// <summary>
    /// Options for compiling and packaging scripts 
    /// </summary>
    public class BlastCompilerOptions
    {
        /// <summary>
        /// The languageversion to use for the output of the compilation. Packagemode and language version are coupled but there may be deviations:
        /// - You can execute BS1 language version files with SSMD package modes if you guarentee that the code doesnt branch (the interpretor wont check this)
        /// - You can exectue BS1SSMD compiled packages with the normal interpretor in normal package mode
        /// </summary>
        public BlastLanguageVersion Language = BlastLanguageVersion.BS1;

        /// <summary>
        /// the package mode for the output, this determines how code and data is organized in memory 
        /// </summary>
        public BlastPackageMode PackageMode = BlastPackageMode.Normal;

        /// <summary>
        /// Allocator for data package 
        /// </summary>
        public Allocator PackageAllocator = Allocator.Persistent;

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
        public readonly bool CompileWithSystemConstants = true;

        /// <summary>
        /// compare epsilon for constants, on fast float mode it wont match on float.epsilon
        /// </summary>
        public float ConstantEpsilon = 0.001f;     // in fast floating point mode it wont match constants on float.epsilon

        /// <summary>
        /// constant precision expressed as a string value
        /// </summary>
        public string ConstantPrecision = "0.000"; // precision used when using constant values as identifier names, everything is rounded to this 

        /// <summary>
        /// default stack size in number of bytes
        /// - overridden if the script defines a stack size 
        /// </summary>
        public int DefaultStackSize = 0;

        /// <summary>
        /// packages include stackdata 
        /// </summary>
        public bool PackageStack = true;

        /// <summary>
        /// estimate stack size using input, output and validation parameters 
        /// - overridden if the script defines a stack size
        /// </summary>
        public bool EstimateStackSize = true;

        /// <summary>
        /// verbose report logging, log is also blitted to unity
        /// </summary>
        public bool VerboseLogging = false;

        /// <summary>
        /// enable trace level logging -> will generate a massive report 
        /// </summary>
        public bool TraceLogging = false;

        /// <summary>
        /// if yield is not emitted we can save 20 bytes on the stack, with very small stuff that might be handy
        /// not implemented .. todo
        /// </summary>
        public readonly bool SupportYield = false;

        /// <summary>
        /// compile the debug function only if enabled 
        /// </summary>
        public readonly bool CompileDebug = true;

        /// <summary>
        /// enable for partially parallel compilation [forcebly disabled]
        /// </summary>
        public readonly bool ParallelCompilation = false;

        /// <summary>
        /// [SSMD packaging mode] inline any constant data in the codesegment, normally these would be put in the datasegment and referenced
        /// but when executing many equal scripts with differing data its more usefull to have constantdata in the constant code
        /// segment while only having variable data in the data segment. This does come at the cost of more control flow when 
        /// interpretors need to locate the data for instructions, for this reason it is only supported for SSMD|Entity package modes
        /// as the normal interpretor would have severe performance penalties from variable parameter/instruction length, normally 
        /// it knows every datapoint is referenced with a single byte, this wont be true if a constant vector is inlined and all 
        /// controlflow would need to account for that costing performance. In SSMD mode this performance cost is divided by all processed
        /// datasegments while using less memory for storing each datasegment, it has the side effect of allowing more variable indices
        /// to be allocated as there is no index into the datasegment reserved for constant data 
        /// </summary>
        public bool InlineConstantData = false;

        /// <summary>
        /// allow to shared data in cdata segments declared through #cdata defines 
        /// - this lifts the constant restriction and allows to write to it 
        /// </summary>
        public bool SharedCDATA = false;

        /// <summary>
        /// [SSMD packaging mode] optionally inline index- and expanding operations 
        /// </summary>
        public bool InlineIndexers = false; 

        /// <summary>
        /// additional compiler defines 
        /// </summary>
        public Dictionary<string, string> Defines = new Dictionary<string, string>();

        /// <summary>
        /// dont touch if you need to ask 
        /// </summary>
        public bool Experimental = false;

        /// <summary>
        /// Default compiler options
        /// </summary>
#if DEVELOPMENT_BUILD && TRACE
        public static BlastCompilerOptions Default => new BlastCompilerOptions().Verbose().Trace(); 
#elif TRACE
        public static BlastCompilerOptions Default => new BlastCompilerOptions().Trace();
#else
        public static BlastCompilerOptions Default => new BlastCompilerOptions().Silent(); 
#endif

        /// <summary>
        /// Default SSMD compiler options
        /// </summary>
#if DEVELOPMENT_BUILD && TRACE
        public static BlastCompilerOptions SSMD => new BlastCompilerOptions().SetPackageMode(BlastPackageMode.SSMD).PackageWithoutStack().Verbose().Trace();
#elif TRACE
        public static BlastCompilerOptions SSMD => new BlastCompilerOptions().SetPackageMode(BlastPackageMode.SSMD).PackageWithoutStack().Trace();
#else
        public static BlastCompilerOptions SSMD => new BlastCompilerOptions().SetPackageMode(BlastPackageMode.SSMD).PackageWithoutStack().Silent();
#endif

        /// <summary>
        /// Default compiler options for packaging entities:
        /// BS1 - Entity Packagemode - Optimizing
        /// </summary>
        public static BlastCompilerOptions Entity => new BlastCompilerOptions().SetPackageMode(BlastPackageMode.Entity);

        /// <summary>
        /// initialize default options with given stacksize 
        /// </summary>
        /// <param name="stack_size">stacksize, stack estimation will be turned off</param>
        public BlastCompilerOptions(int stack_size)
        {
            DefaultStackSize = stack_size;
            EstimateStackSize = false;
        }

        /// <summary>
        /// initilize default options in given packaging mode
        /// </summary>
        /// <param name="packagemode">the package mode to set</param>
        public BlastCompilerOptions(BlastPackageMode packagemode)
        {
            PackageMode = packagemode;
        }

        /// <summary>
        /// initialize default options in given packagemode with a pre-determined stack size
        /// </summary>
        /// <param name="stack_size">stacksize, stack estimation will be turned off</param>
        /// <param name="packagemode">packaging mode</param>
        public BlastCompilerOptions(int stack_size, BlastPackageMode packagemode)
        {
            DefaultStackSize = stack_size;
            EstimateStackSize = false;
            PackageMode = packagemode;
        }

        /// <summary>
        /// initialize with default options 
        /// </summary>
        public BlastCompilerOptions()
        {
        }

        #region Method chains to setup options 


        /// <summary>
        /// enable validation (default = off) 
        /// </summary>
        /// <param name="run_auto_validation">true to enable validation</param>
        /// <returns>options</returns>
        public BlastCompilerOptions EnableValidation(bool run_auto_validation = true)
        {
            this.AutoValidate = run_auto_validation;
            return this;
        }

        /// <summary>
        /// set the size of the stack, if 0 is set stack is estimated during compilation 
        /// </summary>
        /// <param name="stack_size"></param>
        /// <returns>compiler options</returns>
        public BlastCompilerOptions SetStackSize(int stack_size = 0)
        {
            this.DefaultStackSize = stack_size;
            this.EstimateStackSize = stack_size <= 0 && (this.PackageMode == BlastPackageMode.Normal || this.PackageMode == BlastPackageMode.Compiler);
            return this;
        }

        /// <summary>
        /// package without a stack segment, use stackmemory in the interpretor 
        /// </summary>
        /// <returns>compiler options</returns>
        public BlastCompilerOptions PackageWithoutStack()
        {
            this.PackageStack = false;
            return this;
        }

        /// <summary>
        /// Set language target 
        /// </summary>
        /// <param name="language">Supported language: BS1 | BSSSMD1 | HPC </param>
        /// <returns>compiler options</returns>
        public BlastCompilerOptions SetLanguage(BlastLanguageVersion language = BlastLanguageVersion.BS1)
        {
            this.Language = language;
            return this;
        }

        /// <summary>
        /// Set packaging mode
        /// </summary>
        /// <param name="packagemode">Packagemode: Normal, SSMD or Entity</param>
        /// <returns>compiler options</returns>
        public BlastCompilerOptions SetPackageMode(BlastPackageMode packagemode = BlastPackageMode.Normal)
        {
            this.PackageMode = packagemode;

            // in ssmd force inlined constant data 
            if(this.PackageMode == BlastPackageMode.SSMD)
            {
                this.InlineConstantData = true;
                this.InlineIndexers = true;
                this.PackageStack = false;
                this.EstimateStackSize = true;
            }

            // in normal it is not allowed (as of now... could be sometime..
            if(this.PackageMode == BlastPackageMode.Normal)
            {
                this.InlineConstantData = false;
                this.InlineIndexers = false;
                this.PackageStack = true;
                this.EstimateStackSize = true;
            }

            // entity mode should decide for itself 
            return this;
        }

        /// <summary>
        /// Set allocation mode for packages, use temp with caution in combination with bursted code
        /// </summary>
        /// <param name="allocator">the unity memory allocation type</param>
        /// <returns>compiler options</returns>
        public BlastCompilerOptions SetAllocator(Allocator allocator)
        {
            this.PackageAllocator = allocator;
            return this;
        }

        /// <summary>
        /// enable compiler optimizations, default == enabled
        /// </summary>
        /// <returns>compiler options</returns>
        public BlastCompilerOptions EnableOptimisations()
        {
            this.Optimize = true;
            return this;
        }

        /// <summary>
        /// disable compiler optimisations, default = enabled
        /// </summary>
        /// <returns>compiler options</returns>
        public BlastCompilerOptions DisableOptimisations()
        {
            this.Optimize = false;
            return this;
        }

        /// <summary>
        /// disable all logging, fails silently
        /// </summary>
        /// <returns>compiler options</returns>
        public BlastCompilerOptions Silent()
        {
            this.VerboseLogging = false;
            this.TraceLogging = false;
            return this;
        }

        /// <summary>
        /// set verbose logging
        /// </summary>
        /// <returns>compiler options</returns>
        public BlastCompilerOptions Verbose()
        {
            this.VerboseLogging = true;
            this.TraceLogging = false;
            return this;
        }

        /// <summary>
        /// set trace logging
        /// </summary>
        /// <returns>compiler options</returns>
        public BlastCompilerOptions Trace()
        {
            this.VerboseLogging = true;
            this.TraceLogging = true;
            return this;
        }

     

        /// <summary>
        /// add a compiled define
        /// </summary>
        /// <param name="key">defined name</param>
        /// <param name="value">value</param>
        /// <param name="allow_overwrite_existing"></param>
        /// <returns>compiler options</returns>
        public BlastCompilerOptions AddDefine(string key, string value, bool allow_overwrite_existing = false)
        {
            Assert.IsNotNull(Defines);
            Assert.IsFalse(string.IsNullOrEmpty(key));
            Assert.IsFalse(string.IsNullOrEmpty(value));

            key = key.ToLower().Trim();
            if (!Defines.ContainsKey(key))
            {
                // set new
                Defines.Add(key, value);
            }
            else
            {
                Assert.IsTrue(allow_overwrite_existing, $"key {key} is already defined");

                // update existing
                Defines[key] = value;
            }
            return this;
        }


        /// <summary>
        /// Check if the key is defined in compiler options 
        /// </summary>
        /// <param name="key">the case insensitive key to check</param>
        /// <returns>true if its defined</returns>
        public bool IsDefined(string key)
        {
            Assert.IsNotNull(Defines);
            if (string.IsNullOrEmpty(key)) return false;

            key = key.Trim().ToLowerInvariant();

            return Defines.ContainsKey(key);
        }

        /// <summary>
        /// Try to lookup a key and get its value
        /// </summary>
        /// <param name="key">case insensitive key</param>
        /// <param name="value">the value if present</param>
        /// <returns>true if the key was defined</returns>
        public bool TryGetDefine(string key, string value)
        {
            Assert.IsNotNull(Defines);
            if (string.IsNullOrEmpty(key)) return false;

            key = key.Trim().ToLowerInvariant();

            return Defines.TryGetValue(key, out value);
        }

        /// <summary>
        /// remove all defines from compiler options 
        /// </summary>
        public void ClearDefines()
        {
            Assert.IsNotNull(Defines);
            Defines.Clear();
        }

        /// <summary>
        /// Remove any value possibly assosiated with the key
        /// </summary>
        /// <param name="key">case insensitive key</param>
        /// <returns>true if the key was defined and subsequently removed</returns>
        public bool RemoveDefine(string key)
        {
            Assert.IsNotNull(Defines);
            return Defines.Remove(key);
        }

        #endregion
    }


    namespace Stage
    {
        /// <summary>
        /// the types of compiler stages that run in sequence to produce the output
        /// </summary>
        public enum BlastCompilerStageType
        {
            /// <summary>
            /// unknown - not set
            /// </summary>
            None,
            /// <summary>
            /// convert input script into a list of tokens 
            /// </summary>
            Tokenizer,
            /// <summary>
            /// parses the tokens into an ast-tree
            /// </summary>
            Parser,
            /// <summary>
            /// identify all identifiers 
            /// </summary>
            IdentifierMapping,
            /// <summary>
            /// transform constructs in the ast: switch -> ifthen, while,for, etc -> ifthen 
            /// making later stages having less to worry about 
            /// </summary>
            Transform,
            /// <summary>
            /// analyse parameter use
            /// - determine vectors 
            /// - enforce multiplication rules 
            /// </summary>
            ParameterAnalysis,
            /// <summary>
            /// analyze ast structure
            /// - basic removal of some useless structures
            /// - rules of multiplication
            /// </summary>
            Analysis,
            /// <summary>
            /// flatten execution path 
            /// </summary>
            Flatten,
            /// <summary>
            /// optimize ast structure
            /// - transform expensive constructs into less expensive ones
            /// - this should be done after flattening the tree, any optimization that reduces compounds should happen in analysis
            /// </summary>
            Optimization,
            /// <summary>
            /// pre compile cleanup 
            /// </summary>
            Cleanup,
            /// <summary>
            /// resolve stack operations into stack-variables (HPC/CS only)
            /// </summary>
            StackResolver,
            /// <summary>
            /// a [bytecode/hpc/cs] compiler
            /// </summary>
            Compile,
            /// <summary>
            /// post-compile: bytecode optimizer
            /// </summary>
            BytecodeOptimizer,
            /// <summary>
            /// post-compile: resolve jumps 
            /// </summary>
            JumpResolver,
            /// <summary>
            /// post-compile: package result
            /// </summary>
            Packaging,
        }

        /// <summary>
        /// a compiler stage - employs 1 step of the compilation process
        /// </summary>
        public interface IBlastCompilerStage
        {
            /// <summary>
            /// stage version, for future differentiation 
            /// </summary>
            System.Version Version
            {
                get;
            }

            /// <summary>
            /// the type of stage implemented 
            /// </summary>
            BlastCompilerStageType StageType
            {
                get;
            }

            /// <summary>
            /// execute interface 
            /// </summary>
            /// <param name="data">compiler data object</param>
            /// <returns>blasterror code|success</returns>
            int Execute(IBlastCompilationData data);
        }


    }

    /// <summary>
    /// Blast Compiler 
    /// 
    /// Compile scripts into bytecode, the process consists of several stages:
    /// 
    /// 
    /// For bytecode these stages might be executed:
    /// 
    /// 1   - tokenize:            convert code into a list of tokens 
    /// 2   - parse:               parse list of tokens into a tree of nodes 
    /// 2.1 - variable mapping     map all identifiers
    /// 3   - transform:           transform nodes sequences  
    /// 4.1 - analyse parameters:  determine parameter types and check inputs and outputs
    /// 4.2 - analyse:             analyse nodes, reorder were needed, optimize for execution
    /// 5   - flatten:             flatten execution path
    /// 6   - node optimizer:      optimize nodes 
    /// 7   - compile:             generate bytecode from the internal representation 
    /// 8   - optimize bytecode:   optimize bytecode patterns 
    /// 9   - package:             package bytecode and set it up for execution 
    /// 
    ///</summary>
    static public class BlastCompiler
    {
       //   ^__^
       //   (oo)\_______
       //   (__)\       )\/\
       //       ||----w |
       //       ||     ||

       /// <summary>
       /// from this opcode until opt_ident constant values are encoded
       /// </summary>
        public const byte opt_value = (byte)blast_operation.pi;
       
        /// <summary>
        /// form this opcode until 255 variables are encoded
        /// </summary>
        public const byte opt_ident = (byte)blast_operation.id;

        /// <summary>
        /// Default compilation setup 
        /// </summary>
        public static List<IBlastCompilerStage> ByteCodeStages = new List<IBlastCompilerStage>
        {
            // convert into token array, build names of identifiers
            new BlastTokenizer(),
            
            // parse nodes from tokens, map out identifiers
            new BlastParser(),

            // map out all parameters
            new BlastIdentifierMapping(),

            // transform constructs into simpler ones
            new BlastTransform(), 
            
            // analyse parameter use
            new BlastParameterAnalysis(),
            
            // analyse ast nodes, apply rules and optimizations to its structure 
            new BlastAnalysis(),
            
            // flatten node structure into a linear path 
            new BlastFlatten(),

            // try to optimize common sequences 
            new BlastOptimizer(),
            
            // cleanup unreferenced parameters and determine constant data fields
            new BlastPreCompileCleanup(),
            
            // compile the ast into bytecode
            new BlastBytecodeCompiler(),
            
            // use pattern recognition to transfrom sequences of bytecode into more efficient ones 
            // - after all updates this became pretty useless and a bug source
            // new BlastBytecodeOptimizer(),
            
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
            new BlastIdentifierMapping(),
            new BlastTransform(),
            new BlastParameterAnalysis(),
            new BlastAnalysis(),
            new BlastFlatten(),
            new BlastOptimizer(), 
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
            new BlastIdentifierMapping(),
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


        #region output validation 

        /// <summary>
        /// - Validate output using data set in script for NULL inputs 
        /// - Double as a method to estimate stack size 
        /// </summary>
        /// <param name="result">bytecode compiler data</param>
        /// <param name="blast">blast engine data</param>
        /// <returns>true if validation succeeded</returns>
        static public bool ValidateResultsAndStacksize(IBlastCompilationData result, IntPtr blast)
        {
            if (!result.CompilerOptions.EstimateStackSize && !result.CanValidate)
            {
                result.LogError($"validate: the script defines no validations, dont call validate if this was intentional");
                return false;
            }

            CompilationData cdata = result as CompilationData;
            Assert.IsNotNull(cdata, "dont call validate on anything but normal script packages");

            // make sure offsets are set 
            if (result.HasVariables && !result.HasOffsets || result.VariableCount != result.OffsetCount)
            {
                cdata.CalculateVariableOffsets();
                if (result.VariableCount != result.OffsetCount)
                {
                    result.LogError("validate: failure calculating variable offsets");
                    return false;
                }
            }

            switch (result.CompilerOptions.PackageMode)
            {
                case BlastPackageMode.SSMD:
                    {
                        cdata.Executable.ValidateSSMD(blast);
                    }
                    break;
                    
                case BlastPackageMode.Normal:
                    {
                        cdata.Executable.Validate(blast);
                    }
                    break;

                default:                                      
                    result.LogError($"validate: packagemode {result.CompilerOptions.PackageMode} not supported");
                    return false;
            }

            // check results 
            if (result.Validations.Count > 0)
            {
                foreach (var validation in result.Validations)
                {
                    string a = validation.Key;
                    string b = validation.Value ?? "";
                    float f_a, f_b;

                    // a must be a parameter
                    var v_a = result.GetVariable(a);
                    if (v_a == null)
                    {
                        result.LogError($"Validation failed on: {a} = {b}, could not locate parameter {a}");
                        continue;
                    }

                    f_a = cdata.Executable.GetDataSegmentElement(result.Offsets[v_a.Id]);

                    // b can be a float or a parameter 
                    if (float.IsNaN(f_b = b.AsFloat()))
                    {
                        // try to get as parameter 
                        var v_b = result.GetVariable(b);
                        f_b = cdata.Executable.GetDataSegmentElement(result.Offsets[v_b.Id]);
                    }

                    // compare validations using the epsilon set in compiler options 
                    if (f_a < f_b - result.CompilerOptions.ConstantEpsilon || f_a > f_b + result.CompilerOptions.ConstantEpsilon)
                    {
                        result.LogError($"Validation failed on rule: {a} == {b} ({f_a} != {f_b})");
                        continue;
                    }
                }
            }

            // reset defaults after validating  
            unsafe
            {
                fixed (float* pdata = cdata.Executable.data)
                {
                    if (result.HasInputs)
                    {
                        BlastScriptPackage.SetDefaultData(result.Inputs.ToArray(), pdata);
                    }
                    if (result.HasOutputs)
                    {
                        BlastScriptPackage.SetDefaultData(result.Outputs.ToArray(), pdata);
                    }
                }
            }

            return result.IsOK;
        }

        #endregion

        #region code and data packaging 


        /// <summary>
        /// return stacksize in bytes as defined, or -1 if no define was found 
        /// </summary>
        static int GetDefinedStackSize(CompilationData result)
        {
            string s_defined_size;
            if (result.Defines.TryGetValue("stack_size", out s_defined_size) || result.CompilerOptions.Defines.TryGetValue("stack_size", out s_defined_size))
            {
                int i_defined_size;
                if (int.TryParse(s_defined_size, out i_defined_size))
                {
                    return i_defined_size; 
                }
                else
                {
                    result.LogError($"BlastCompiler.GetDefinedStackSize(result): failed to read defined value as a valid integer: {s_defined_size} ");
                }
            }

            return -1; 
        }


        /// <summary>
        /// estimate stack size by running script with a selection of parameters from
        /// input, output and validation settings 
        /// 
        /// TODO: EstimateStackSize and Validate should be combined 
        /// </summary>
        /// <returns>estimated stack size in bytes</returns>
        static unsafe public int EstimateStackSize(CompilationData result)
        {
            Assert.IsNotNull(result);

            // ### Stacksize Determination
            //            Blast determines the size of the stack to allocate as follows:
            //
            // -First; if **stack_size * * is defined as:  `#define stack_size 12` blast will fix the stacksize to 12 bytes, additional space will be added if yield is to be supported.
            // -Second; if compileroptions specify** EstimateStack**then during compilation the package is executed once using default data and the stack size is determined from analysis of the stack. Note that this will force an extra compilation step if the packagemode is ssmd as currently stack estimation is only supported by the normal package mode which will always use more stack then the ssmd packager. (which should not matter too much if the no - stack option is used to use the threadlocal stack in the interpretor and not actually package the stack in the package memory) 
            // -Third; if by now no stacksize is known, blast will count overlapping pairs of push - pop pairs and estimates stack from that.If no push command is used then the stacksize will be set to 0; If Yield is used in the package, 20 bytes are additionally allocated to support stacking internal state on yield.
            // 

            // get yield and default
            int reserve_yield = 0;
            if (result.CompilerOptions.SupportYield)
            {
                // only reserve stack for yield if its actually used in the bytecode
                if (result.root != null && result.root.CheckIfFunctionIsUsedInTree(blast_operation.yield))
                {
                    reserve_yield = 20;
                }
            }

            // 1> defined stacksizes, script defines have precedence over compiler defines  
            int idefined = GetDefinedStackSize(result);
            if (idefined > 0)
            {
                result.LogTrace($"BlastCompiler.EstimateStackSize: defined: {idefined} bytes, reserved for yield: {reserve_yield} bytes, total stacksize = {idefined + reserve_yield} bytes");
                return idefined + reserve_yield;
            }
            if (idefined == 0)
            {
                if (reserve_yield > 0)
                {
                    result.LogWarning($"BlastCompiler.EstimateStackSize: defined: 0 bytes BUT reserved for yield: {reserve_yield} bytes, total stacksize = {idefined + reserve_yield} bytes");
                }
                else
                {
                    result.LogTrace($"BlastCompiler.EstimateStackSize: defined: 0 bytes, reserved for yield: 0 bytes, total stacksize = 0 bytes");
                }
                return idefined + reserve_yield;
            }

            // 2> estimate (this is combined with validation) 
            // during that step max_stack_size is determined if either validate or estimatestack is true
            if(result.CompilerOptions.EstimateStackSize)
            {
                int stack_size = result.Executable.max_stack_size * 4 + reserve_yield;
                result.LogTrace($"BlastCompiler.EstimateStackSize: estimated a total stacksize of: {stack_size}");
                return stack_size; 
            }

            // 3> estimate from node structure and push-pop pairs
            int estimated_size = result.root.EstimateStackUse(0);
            int default_size = result.CompilerOptions.DefaultStackSize; // TODO this is not documented well 

            if(default_size > 0 && estimated_size < default_size)
            {
#if TRACE || DEVELOPMENT_BUILD
                result.LogWarning($"BlastCompiler.EstimateStackSize: estimated size {estimated_size} smaller then non zero defaultsize, setting estimate to default: {default_size}");
#endif 
                estimated_size = default_size;
            }

            result.LogTrace($"BlastCompiler.EstimateStackSize: estimated from push-pop: {estimated_size} bytes, reserved for yield: {reserve_yield} bytes, total stacksize = {estimated_size + reserve_yield} bytes");
            return estimated_size + reserve_yield;
        }


        
        /// <summary>
        /// Package in normal mode. 
        /// 
        /// - packagemode       = NORMAL
        /// - languageversion   = BS1
        /// </summary>
        /// <param name="cdata">Compiler data</param>
        /// <param name="code_size">size of code in bytes</param>
        /// <param name="metadata_size">size of metadata in bytes, 1 byte per data element in data and stack segment combined</param>
        /// <param name="data_size">size of datasegment in bytes</param>
        /// <param name="stack_size">size of stacksegement in bytes</param>
        /// <param name="flags">flags: alignment, stack</param>
        /// <returns>blast package data</returns>
        static BlastPackageData PackageNormal(CompilationData cdata, int code_size, int metadata_size, int data_size, int stack_size, BlastPackageFlags flags = BlastPackageFlags.None)
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

            
            // [----CODE----|----METADATA----|----DATA----|----STACK----]
            //              1                2            3             4  
            // 
            // ** ALL OFFSETS IN PACKAGE IN BYTES ***
            // 
            // 1 = metadata offset   | codesize 
            // 2 = data_offset       | codesize + metadatasize 
            // 3 = stack_offset 
            // 4 = package_size  

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
                data.CodeSegmentPtr = new IntPtr(UnsafeUtils.Malloc(package_size, 8, cdata.CompilerOptions.PackageAllocator));
                data.Allocator = (byte)cdata.CompilerOptions.PackageAllocator;

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
        /// package for ssmd use:  [code-metadata] [data-stack]
        /// </summary>
        /// <param name="cdata">compiler result data</param>
        /// <param name="code_size"></param>
        /// <param name="metadata_size"></param>
        /// <param name="data_size"></param>
        /// <param name="stack_size"></param>
        /// <param name="flags"></param>
        /// <returns></returns>
        static BlastPackageData PackageSSMD(CompilationData cdata, int code_size, int metadata_size, int data_size, int stack_size, BlastPackageFlags flags = BlastPackageFlags.None)
        {
            Assert.IsTrue(metadata_size == (data_size + stack_size) / 4,
                $"PackageSSMD: metadata size misaligned with combined datastack size, there are {metadata_size} entries in metadata while there are {((data_size + stack_size) / 4)} combined data/stack entries.");

            bool a8 = (flags & BlastPackageFlags.Aligned8) == BlastPackageFlags.Aligned8;
            bool has_stack = (flags & BlastPackageFlags.NoStack) == BlastPackageFlags.NoStack;

            // SSMD Package with code & metadata seperate from data&stack
            // 
            // - SSMD requirement: while data changes, its intent does not so metadata is shared
            // 
            // ? could allow branches and split from there? 
            // 
            // [----CODE----|----METADATA----]       [----DATA----|----STACK----]
            //              1                2                    3             4
            // 
            // 1 = metadata offset 
            // 2 = codesegment size 
            // 3 = stack offset 
            // 4 = datasegment size 
            // 
            // prop stacksize (in elements) => (datasegment size - stack_offset) / 4

            int align_stack = 0;
            if (a8 && has_stack)
            {
                // no use to align to 4 as stack and data are always a multiple of that 
                while (((data_size + align_stack) % 8) != 0) align_stack++;
            }

            //
            //
            // if using inlined constant data then constant data should not be packaged
            // - this frees variable index space, normally constants cost variable indices
            // - now the datasegment wont have repeating data on many in ssmd
            //
            // *> datasize gets 4x constantcount smaller (all constants are float1 and datasize is in bytes)
            // *> metadatasize gets 1x constantcount smaller, 1 byte of metadata is used for all variables/stacklocations  (4 bytes per location = 5 bytes total per float of data)
            //
            //

            int constant_count = 0;
            if (cdata.CompilerOptions.InlineConstantData)
            {
                for (int i = 0; i < cdata.VariableCount; i++)
                {
                    if (cdata.Variables[i].IsConstant)
                    {
                        // if inlineconstant data is not enabled then we just treat everything ass a variable,
                        // otherwise we substract constants from it while writing the package
                        constant_count++;
                    }
                }
            }


            // init package data
            BlastPackageData data = default;
            data.PackageMode = BlastPackageMode.SSMD;
            data.Flags = flags;
            data.LanguageVersion = BlastLanguageVersion.BS1;
            data.Allocator = (byte)cdata.CompilerOptions.PackageAllocator;

            // setup code and segment data
            data.O1 = (ushort)code_size;
            data.O2 = (ushort)(code_size + metadata_size - constant_count);
            data.O3 = (ushort)(data_size - (constant_count * 4) + align_stack);
            data.O4 = (ushort)(data_size - (constant_count * 4) + align_stack + stack_size);

            unsafe 
            { 
                data.CodeSegmentPtr = new IntPtr(UnsafeUtils.Malloc(code_size + metadata_size - constant_count, 8, cdata.CompilerOptions.PackageAllocator));

                // copy code segment 
                int i = 0;
                while (i < code_size)
                {
                    data.Code[i] = cdata.Executable.code[i];
                    i++;
                }

                // copy metadata for variables 
                i = 0;
                while (i < cdata.Executable.data_count - constant_count)
                {
                    data.Metadata[i] = cdata.Executable.metadata[i];
                    i++;
                }
                while (i < metadata_size - constant_count)
                {
                    // fill stack metadata with 0's
                    data.Metadata[i] = 0;
                    i++;
                }

                data.P2 = new IntPtr(UnsafeUtils.Malloc(data_size - (constant_count * 4) + align_stack + stack_size, 8, cdata.CompilerOptions.PackageAllocator));

                // copy initial variable data => datasegment 
                i = 0;
                while (i < cdata.Executable.data_count - constant_count)
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

            return data; 
        }


        /// <summary>
        /// package the result of the compiler into an executable package 
        /// </summary>
        /// <returns>the native blast package data structure</returns>
        static public BlastPackageData Package(IBlastCompilationData compilerdata, BlastCompilerOptions options)
        {
            if (options == null) options = BlastCompilerOptions.Default; 

            // this heavily depends on bytecode 
            Assert.IsTrue(options.Language == BlastLanguageVersion.BS1 || options.Language == BlastLanguageVersion.BSSMD1);

            // get compilerdata for the bytecode languages 
            CompilationData result = compilerdata as CompilationData; 

            // get sizes of seperate segments 
            int stack_size = EstimateStackSize(result);
            int code_size = result.Executable.code_size;
            int data_size = result.Executable.data_count * 4;
            int metadata_size = result.Executable.data_count + stack_size / 4;

            BlastPackageMode package_mode = options.PackageMode;
            switch (package_mode)
            {
                case BlastPackageMode.Normal:

                    // [code-meta-data-stack]
                    return PackageNormal(result,
                                         code_size,
                                         metadata_size,
                                         data_size,
                                         stack_size);


                    // [code-meta] [data-stack] 
                case BlastPackageMode.SSMD:
                    return PackageSSMD(result,
                                        code_size,
                                        metadata_size,
                                        data_size,
                                        stack_size,
                                        options.PackageStack ? BlastPackageFlags.None : BlastPackageFlags.NoStack); 

                case BlastPackageMode.Entity:
                    // [code] [meta-data-stack]

                // [undefined]
                case BlastPackageMode.Compiler:
                    Debug.LogError($"BlastCompiler.Package: package mode {package_mode} not supported");
                    break;
            }
            return default;
        }

        #endregion

        #region Compile overloads

        /// <summary>
        /// Compile script from text
        /// </summary>
        /// <param name="blast">blast engine data</param>
        /// <param name="code">the code</param>
        /// <param name="options">compiler options</param>
        /// <returns>compiler data</returns>
        static public IBlastCompilationData Compile(BlastEngineDataPtr blast, string code, BlastCompilerOptions options = default)
        {
            return Compile(blast, BlastScript.FromText(code), options);
        }

        /// <summary>
        /// Compile a script
        /// </summary>
        /// <param name="blast">blast engine data</param>
        /// <param name="script">the script to compile</param>
        /// <param name="options">compileoptions</param>
        /// <returns>compiler data</returns>
        static public IBlastCompilationData Compile(BlastEngineDataPtr blast, BlastScript script, BlastCompilerOptions options = default)
        {
            if (options == null) options = BlastCompilerOptions.Default;

            // setup compiler for given compiler options  
            IBlastCompilationData result = null;
            List<IBlastCompilerStage> stages = null;

            switch (options.Language)
            {
                // when we get to implementing special jumps for ssmd these will differ 
                case BlastLanguageVersion.BSSMD1: 
                case BlastLanguageVersion.BS1:
                    result = new CompilationData(blast, script, options); 
                    stages = ByteCodeStages; 
                    break;

                // setup compilation chain for compiling into c#
                case BlastLanguageVersion.HPC:
                    result = new HPCCompilationData(blast, script, options); 
                    stages = HPCStages;
                    break;

                // only windows 
                case BlastLanguageVersion.CS:
                    result = new CSCompilationData(blast, script, options);
                    stages = CSStages;
                    break; 
            }
            if (stages == null || stages.Count == 0)
            {
                result.LogError($"Blast Compilation Error: language {options.Language} not supported.", (int)BlastError.error_language_version_not_supported);
                return result; 
            }

            // execute each compilation stage 
            for(int istage = 0; istage < stages.Count; istage++)
            {
                int exitcode = stages[istage].Execute(result);

                if (exitcode != (int)BlastError.success)
                {
                    // error condition 
                    result.LogError($"Blast Compilation Error: stage: {istage}, type {stages[istage].GetType().Name}, exitcode: {exitcode}");
                    break;
                }
            }

            if (result.HasErrors || options.TraceLogging)
            {
#if TRACE || STANDALONE_VSBUILD
                Debug.Log(result.AST.ToNodeTreeString());
                switch(result.CompilerOptions.Language)
                {
                    case BlastLanguageVersion.BS1:
                    case BlastLanguageVersion.BSSMD1:
                        CompilationData data = result as CompilationData;
                        Debug.Log("Code: \n" + result.Script.Code + "\n"); 
                        Debug.Log(data.GetHumanReadableCode() + "\n");
                        Debug.Log(data.GetHumanReadableBytes() + "\n");
                        break; 
                }
#endif
            }

            if (result.IsOK)
            {
                // run validations if any are defined
                // - OR if stacksize is estimated as they do a bit of the same thing
                if (result.CompilerOptions.EstimateStackSize
                    ||
                    (result.CompilerOptions.AutoValidate && result.CanValidate))
                {
                    switch (result.CompilerOptions.Language)
                    {
                        case BlastLanguageVersion.BS1:
                        case BlastLanguageVersion.BSSMD1:
                            CompilationData data = result as CompilationData;
                            if(!ValidateResultsAndStacksize(data, blast.ptr))
                            {
                                result.LogError("Compile: failed to validate script or estimate stack size"); 
                            }
                            break;

                        default:
                            result.LogError($"Compile: cannot validate script in language version {result.CompilerOptions.Language}, please use a different set of compiler options"); 
                            break; 
                    }
                }
            }

            return result;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="blast"></param>
        /// <param name="script"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public static HPCCompilationData CompileHPC(BlastEngineDataPtr blast, BlastScript script, BlastCompilerOptions options)
        {
            if (options == null) options = BlastCompilerOptions.Default;

            HPCCompilationData result = new HPCCompilationData(blast, script, options);

            for (int istage = 0; istage < HPCStages.Count; istage++)
            {
                int exitcode = HPCStages[istage].Execute(result);

                if (exitcode != (int)BlastError.success)
                {
                    result.LogError($"Compilation Error, stage: {istage}, type {HPCStages[istage].GetType().Name}, exitcode: {exitcode}");
                    if (options.VerboseLogging || options.TraceLogging)
                    {
                        Debug.Log(result.root.ToNodeTreeString());
                        Debug.Log(result.GetHumanReadableCode());
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
        public static HPCCompilationData CompileCS(BlastEngineDataPtr blast, BlastScript script, BlastCompilerOptions options)
        {
            if (options == null) options = BlastCompilerOptions.Default;

            CSCompilationData result = new CSCompilationData(blast, script, options);

            for (int istage = 0; istage < CSStages.Count; istage++)
            {
                int exitcode = CSStages[istage].Execute(result);

                if (exitcode != (int)BlastError.success)
                {
                    result.LogError($"Compilation Error, stage: {istage}, type {CSStages[istage].GetType().Name}, exitcode: {exitcode}");
                    if (options.VerboseLogging || options.TraceLogging)
                    {
                        Debug.Log(result.root.ToNodeTreeString());
                        Debug.Log(result.GetHumanReadableCode());
                    }
                    return result;
                }
            }

            return result;
        }

        
        /// <summary>
        /// compile script into a managed blastscriptpackage containing blastpackagedata
        /// </summary>
        /// <param name="blast"></param>
        /// <param name="script"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        unsafe static public BlastError CompilePackage(BlastEngineDataPtr blast, BlastScript script, BlastCompilerOptions options = null)
        {
            if (options == null) options = new BlastCompilerOptions();

            IBlastCompilationData result = Compile(blast, script, options);
            if (result.IsOK)
            {
                script.Package = new BlastScriptPackage()
                {
                    Package = Package(result, options),
                    Variables = result.Variables.ToArray(),
                    VariableOffsets = result.Offsets.ToArray(),
                    Inputs = result.Inputs.ToArray(),
                    Outputs = result.Outputs.ToArray()
                };
                return BlastError.success; 
            }
            else
            {
                return BlastError.compile_packaging_error;
            }
        }

        /// <summary>
        /// Compile the script into a native blastscript datapackage
        /// </summary>
        /// <param name="blast"></param>
        /// <param name="script"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        unsafe static public BlastPackageData CompileDataPackage(BlastEngineDataPtr blast, BlastScript script, BlastCompilerOptions options = null)
        {
            if (options == null) options = new BlastCompilerOptions();

            IBlastCompilationData result = Compile(blast, script, options);
            if (result.IsOK)
            {
                return Package(result, options); 
            }
            else
            {
                return default;
            }
        }
        #endregion 

    }

}