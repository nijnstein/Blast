//##########################################################################################################
// Copyright © 2022 Rob Lemmens | NijnStein Software <rob.lemmens.s31@gmail.com> All Rights Reserved  ^__^\#
// Unauthorized copying of this file, via any medium is strictly prohibited                           (oo)\#
// Proprietary and confidential                                                                       (__) #
//##########################################################################################################
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

#if STANDALONE_VSBUILD
    using NSS.Blast.Standalone;
    using UnityEngine.Assertions;
#else
    using UnityEngine;
    using UnityEngine.Assertions;
#endif


using NSS.Blast.Cache;
using NSS.Blast.Compiler;
using NSS.Blast.Interpretor;
using NSS.Blast.SSMD;

using Unity.Collections;
using Unity.Mathematics;
using Unity.Burst;

using Random = Unity.Mathematics.Random;
using System.Runtime.CompilerServices;
using System.Linq;

/// <summary>
/// BLAST Namespace
/// </summary>
/// <remarks>
/// Using Blast: https://github.com/nijnstein/BLAST-Documentation/blob/main/samples.md
/// 
/// BlastScript Language Reference: https://github.com/nijnstein/BLAST-Documentation/blob/main/LanguageReference.md
///  
/// </remarks>
namespace NSS.Blast
{
    /// <summary>
    /// BLAST execution data - used during execution of scripts
    /// </summary>
    [BurstCompile]
    unsafe public struct BlastEngineData
    {
        /// <summary>
        /// constant values selected by opcodes and shared among threads 
        /// - these are set by the constant value operations and they can be replaced with other values... if you are brave|mad|both
        /// </summary>
        public float* constants;

        /// <summary>
        /// constant values linked to constants == UnityEngine.Time.deltaTime 
        /// </summary>
        public float DeltaTime 
            =>
#if DEVELOPMENT_BUILD || TRACE
            constants == null ? -1f :
#endif 
            constants[(int)blast_operation.deltatime - BlastInterpretor.opt_value];

        /// <summary>
        /// the constant: FixedDeltaTime == UnityEngine.Time.fixedDeltaTime
        /// </summary>
        public float FixedDeltaTime
            =>
#if DEVELOPMENT_BUILD || TRACE
            constants == null ? -1f :
#endif 
            constants[(int)blast_operation.fixeddeltatime - BlastInterpretor.opt_value];

        /// <summary>
        /// the constant: Time == UnityEngine.Time.Time
        /// </summary>
        public float Time
            =>
#if DEVELOPMENT_BUILD || TRACE
            constants == null ? -1f :
#endif 
            constants[(int)blast_operation.time - BlastInterpretor.opt_value];

        /// <summary>
        /// the constant: FixedTime == UnityEngine.Time.FixedTime 
        /// </summary>
        public float FixedTime
            =>
#if DEVELOPMENT_BUILD || TRACE
            constants == null ? -1f :
#endif 
            constants[(int)blast_operation.fixedtime - BlastInterpretor.opt_value];

        /// <summary>
        /// the constant: FrameCount == UnityEngine.Time.FrameCount 
        /// </summary>
        public float FrameCount
            =>
#if DEVELOPMENT_BUILD || TRACE
            constants == null ? -1f:
#endif 
            constants[(int)blast_operation.framecount - BlastInterpretor.opt_value];

        /// <summary>
        /// external function info array. these provide api access to the script  
        /// </summary>
        public BlastScriptFunction* Functions;

        /// <summary>
        /// function pointer data
        /// </summary>
        public IntPtr* FunctionPointers; 

        /// <summary>
        /// number of function pointers in the fp array. function id's dont match indices.....  
        /// - TODO   this means each function call needs a lookup...
        /// </summary>
        public int FunctionCount;

        /// <summary>
        /// base random number generator, all random actions have their origin in this random number generator
        /// </summary>
        public Random random;

        /// <summary>
        /// seed the base random number generator, altering the origen for all random actions in blast 
        /// </summary>
        /// <param name="i">the new seed value</param>
        public void Seed(uint i)
        {
            random.InitState(i);
        }

        /// <summary>
        /// lookup function attached to operation, this assumes the function exists 
        /// - TODO -> we could update this to a tableindexer instead of a scan by building a lookuptable[operation] == function
        /// </summary>
        /// <param name="op">the operation to lookup the function for</param>
        /// <returns>function record</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public BlastScriptFunction GetFunction(blast_operation op)
        {
            for(int i = 0; i < FunctionCount; i++)
            {
                if(Functions[i].ScriptOp == op) return Functions[i];
            }
            return default; 
        }

        /// <summary>
        /// lookup a function using its name, this uses string input so wont be burst compiled 
        /// </summary>
        /// <param name="name_match">function name to match</param>
        /// <param name="function">returned function</param>
        /// <returns>true if found</returns>
        [BurstDiscard]
        internal bool TryGetFunctionByName(string name_match, out BlastScriptFunction function)
        {
            Assert.IsTrue(!string.IsNullOrWhiteSpace(name_match), "damn those feeding me empty strings");

            name_match = name_match.ToLower().Trim(); 
            char first = name_match[0];

            for (int i = 1; i < FunctionCount; i++)
            {
                if(Functions[i].Match[0] == first)
                {
                    for(int j = 1; j < name_match.Length; j++)
                    {
                        if (Functions[i].Match[j] != name_match[j]) break;
                        if (j == name_match.Length - 1)
                        {
                            if (Functions[i].Match[j + 1] == (char)0)
                            {
                                function = Functions[i];
                                return true;
                            }
                        }
                    }
                }
            }
            function = default; 
            return false; 
        }

        /// <summary>
        /// get the function matching to the name 
        /// </summary>
        /// <param name="name_match">name to match</param>
        /// <returns>the function struct, note that it returns the default zero'd struct on matching failure</returns>
        [BurstDiscard]
        public BlastScriptFunction GetFunction(string name_match)
        {
            Assert.IsTrue(!string.IsNullOrWhiteSpace(name_match), "damn those feeding me empty strings");

            name_match = name_match.ToLower().Trim();
            char first = name_match[0];

            for (int i = 0; i < FunctionCount; i++)
            {
                if (Functions[i].Match[0] == first)
                {
                    for (int j = 1; j < name_match.Length; j++)
                    {
                        if (Functions[i].Match[j] != name_match[j]) continue;
                        if (j == name_match.Length - 1)
                        {
                            return Functions[i];
                        }
                    }
                }
            }
            return default;
        }

        /// <summary>
        /// return if the id is in the correct range and its pointer is not null
        /// </summary>
        /// <param name="id">the index/id</param>
        /// <returns>true if possibly valid</returns>
        public bool CanBeAValidFunctionId(int id)
        {
#if STANDALONE_VSBUILD
            return id > 0 && id < FunctionCount;
#else
            return id > 0 && id < FunctionCount && FunctionPointers[id] != IntPtr.Zero;
#endif
        }

        /// <summary>
        /// lookup a function mapping to the given operation, performs a linear scan through the function table 
        /// </summary>
        /// <param name="op">the operation to lookup</param>
        /// <returns>returns true if the function is found and can have a variable parameter count</returns>
        public bool IsVariableParamFunction(blast_operation op)
        {
            if (op == blast_operation.nop) return false;

            for (int i = 0; i < FunctionCount; i++)
            {
                if (Functions[i].ScriptOp == op) return Functions[i].HasVariableParameterCount;
            }

            return false; 
        }   
    }

    /// <summary>
    /// abstraction for enginedata pointer
    /// </summary>
    public struct BlastEngineDataPtr
    {
        /// <summary>
        /// pointer to engine data 
        /// </summary>
        public IntPtr ptr;

        /// <summary>
        /// unsafe casted BlastEngineData pointer
        /// </summary>
        unsafe public BlastEngineData* Data 
        { 
            get
            { 
                return (BlastEngineData*) ptr.ToPointer(); 
            } 
        }

        /// <summary>
        /// returns if the pointer is not null / it doesnt actually track creation
        /// </summary>
        public bool IsCreated
        {
            get
            {
                return ptr != IntPtr.Zero;
            }
        }


        /// <summary>
        /// implicit conversion of this to an intptr 
        /// </summary>
        /// <param name="p"></param>
        public static implicit operator BlastEngineDataPtr(IntPtr p) => new BlastEngineDataPtr() { ptr = p };
    }

    /// <summary>
    /// Blast Engine
    /// </summary>
    unsafe public class Blast : IDisposable
    {
        /// <summary>
        /// the singleton instance if created 
        /// </summary>
        static public Blast Instance { get; protected set; } 

        /// <summary>
        /// returns TRUE if the static instance is initialized 
        /// </summary>
        static public bool IsInstantiated => Instance != null && Instance.IsCreated;

        /// <summary>
        /// default interpretor 
        /// </summary>
        [ThreadStatic]
        static public BlastInterpretor blaster = default;

        /// <summary>
        /// default compiler options for script packages 
        /// </summary>
        static public BlastCompilerOptions CompilerOptions = BlastCompilerOptions.Default; 

        /// <summary>
        /// ssmd interpretor
        /// </summary>
        [ThreadStatic]
        static public BlastSSMDInterpretor ssmd_blaster = default;

        /// <summary>
        /// default options for compiler to use packaging ssmd scripts 
        /// </summary>
        static public BlastCompilerOptions SSMDCompilerOptions = BlastCompilerOptions.SSMD; 

        /// <summary>
        /// The API, initialize the instance before using static functions!
        /// </summary>
        static public BlastScriptAPI ScriptAPI 
        { 
            get 
            {
                if (IsInstantiated) return Instance.API;
                else
                {
                    if (script_api == null) script_api = new CoreAPI(Allocator.Persistent);
                    return script_api;
                }                                
            }
        }
        static private BlastScriptAPI script_api = null; 


        /// <summary>
        /// delegate used to execute scripts
        /// </summary>
        /// <param name="scriptid">the id of the script</param>
        /// <param name="p_stack">the stack pointer</param>
        /// <param name="data">environment data</param>
        /// <param name="caller">caller data</param>
        /// <returns></returns>
        public delegate int BlastExecute(int scriptid, [NoAlias]float* p_stack, [NoAlias]IntPtr data, [NoAlias]IntPtr caller);

        /// <summary>
        /// the comment character 
        /// </summary>
        public const char Comment = '#';

        /// <summary>
        /// the maximum length of a function name in number of ASCII characters
        /// </summary>
        public const int MaximumFunctionNameLength = 32;

        /// <summary>
        /// the value used for invalid numerics 
        /// </summary>
        public const float InvalidNumeric = float.NaN;

        /// <summary>
        /// Pointer to native memory holding data used during interpretation:
        /// - function pointers
        /// - constant data 
        /// </summary>
        private BlastEngineData* data;

        private Allocator allocator;
        private bool is_created;

        /// <summary>
        /// true if the structure is initialized and memory is allocated 
        /// </summary>
        public bool IsCreated { get { return is_created; } }

        /// <summary>
        /// IntPtr to global data object used by interpretor, it holds references to constant values and function pointers 
        /// </summary>
        public IntPtr Engine => (IntPtr)data;


        /// <summary>
        /// simple object/lock method for some threadsafety
        /// </summary>
        static internal object mt_lock = new object();




#region Create / Destroy

        /// <summary>
        /// create a static instance, this instance will be used if no blast reference is given to functions that need it:
        /// "Package, Compile, Prepare, Execute"  
        /// </summary>
        /// <param name="api">the api to use</param>
        public static Blast Initialize(BlastScriptAPI api = null)
        {
            if (!IsInstantiated)
            {
                Instance = Create(api == null ? script_api : api);
            }
            return Instance;
        }

        /// <summary>
        /// initialize blast, force destroy if already initialized
        /// </summary>
        /// <param name="api"></param>
        /// <returns></returns>
        /// <remarks>
        /// mainly used in unittests for loading different external calls in test batches 
        /// </remarks>
        public static Blast ReInitialize(BlastScriptAPI api = null)
        {
            if (IsInstantiated)
            {
                Instance.Destroy(); 
            }

            Instance = Create(api == null ? script_api : api);            
            return Instance;
        }

        /// <summary>
        /// disable all non essential logging 
        /// </summary>
        public Blast Silent()
        {
            CompilerOptions.Silent();
            SSMDCompilerOptions.Silent();
            return Instance;
        }

        /// <summary>
        /// enable verbose logging 
        /// </summary>
        public Blast Verbose()
        {
            CompilerOptions.Verbose();
            SSMDCompilerOptions.Verbose();
            return Instance; 
        }

        /// <summary>
        /// seed the base random number generator and regenerate the randomizers in the interpretors for this thread  
        /// </summary>
        /// <param name="seed"></param>
        /// <returns></returns>
        public Blast Seed(uint seed = 1851936439)
        {
            Assert.IsTrue(IsInstantiated);
            this.data->random.InitState(seed);
            return this; 
        }

        /// <summary>
        /// enable trace logging 
        /// </summary>
        public Blast Trace()
        {
            CompilerOptions.Trace();
            SSMDCompilerOptions.Trace();
            return Instance; 
        }

        /// <summary>
        /// create a new instance of BLAST use the core scriptfunction api
        /// </summary>
        /// <param name="allocator">allocator to use, dont use temp allocator for bursted code</param>
        /// <returns>the blast struct</returns>
        public unsafe static Blast Create(Allocator allocator = Allocator.Persistent)
        {
            return Create(null, allocator);
        }


        /// <summary>
        /// create a new instance of blast using a customized api 
        /// </summary>
        /// <param name="api">function api to use</param>
        /// <param name="allocator">allocator to use, dont use temp allocator for bursted code</param>
        /// <returns>the blast struct|class should refactor TODO</returns>
        public unsafe static Blast Create(BlastScriptAPI api, Allocator allocator = Allocator.Persistent)
        {
            Blast blast = new Blast();// default;
            if (mt_lock == null) mt_lock = new object();
            blast.allocator = allocator;
            blast.data = (BlastEngineData*)UnsafeUtils.Malloc(sizeof(BlastEngineData), 8, blast.allocator);
            blast.data->constants = (float*)UnsafeUtils.Malloc(4 * 256 * 2, 4, blast.allocator);
            blast.data->random = Random.CreateFromIndex(1);

            // setup randomizers in interpretors
            // - note: this only initializes them in -this- thread and this might be useless ...  
            blaster.random = Random.CreateFromIndex(blast.data->random.NextUInt());
            ssmd_blaster.random = Random.CreateFromIndex(blast.data->random.NextUInt());

            // setup constant data 
            for (int b = 0; b < 256; b++)
            {
                blast.data->constants[b] = Blast.GetConstantValueDefault((blast_operation)b);
            }

            // setup api 
            if (api == null)
            {
                api = script_api;
                blast.OwnScriptAPIMemory = false;
            }
            else
            {
                blast.OwnScriptAPIMemory = false;
            }
            if (api == null)
            {
                api = (new CoreAPI(allocator)).Initialize();
                blast.OwnScriptAPIMemory = true;
            }
            else
            {
                blast.OwnScriptAPIMemory = false;
            }
            if (!api.IsInitialized)
            {
                api.Initialize(); 
            }
            Assert.IsTrue(api.IsInitialized, "Blast: script api not initialized");

            blast.API = api;
            blast.data->FunctionCount = api.FunctionCount;
            blast.data->Functions = api.Functions; // memory is just referenced, not copied 

            // we only allocate some memory for the intptr array
            blast.data->FunctionPointers = (IntPtr*)UnsafeUtils.Malloc(sizeof(IntPtr) * api.FunctionCount, 8, blast.allocator);
            for (int i = 1; i < api.FunctionCount; i++)
            {
                if (api.FunctionInfo[i] != null)
                {
                    blast.data->FunctionPointers[i] = api.FunctionInfo[i].Function.NativeFunctionPointer;
                }
            }

#if TRACE
            // show an overview of the functions available in the log 
            StringBuilder fsb = new StringBuilder(4098);

            fsb.AppendLine();
            fsb.AppendLine("BLAST: known script functions: ");
            fsb.AppendLine();
            fsb.AppendLine($"{"Index".PadLeft(6)} {"FID".PadLeft(6)}   {"FunctionName".PadRight(32)} {"Operation".PadRight(12)} {"Extended Op".PadRight(12)} ");

            fsb.AppendLine("".PadRight(80, '-'));
            for (int i = 1; i < api.FunctionCount; i++)
            {
                BlastScriptFunction f = blast.data->Functions[i];
                if (f.IsValid)
                {
                    fsb.AppendLine($"{i.ToString().PadLeft(6)} {f.FunctionId.ToString().PadLeft(6)}   {f.GetFunctionName().PadRight(32)} {f.ScriptOp.ToString().PadRight(12)} {f.ExtendedScriptOp.ToString().PadRight(12)}");
                }
            }
            fsb.AppendLine("".PadRight(80, '-'));
            fsb.AppendLine();

            Debug.Log(fsb.ToString());
            fsb = null;
#endif

            blast.is_created = true;
            return blast;
        }

        /// <summary>
        /// release all memory allocated by this instance of blast 
        /// </summary>
        public unsafe void Destroy()
        {
            if (data != null)
            {
                // constant values
                if (data->constants != null)
                {
                    UnsafeUtils.Free(data->constants, allocator);
                }
                // function pointers
                if (data->FunctionPointers != null)
                {
                    UnsafeUtils.Free(data->FunctionPointers, allocator);
                }
                UnsafeUtils.Free(data, allocator);
                data = null;
            }
            // if the api is owned by this instance then destroy it
            // - TODO => should reference count its use and destroy when its not referenced by any blast instance anymore 
            if (OwnScriptAPIMemory && API != null && API.IsInitialized)
            {
                API.Dispose();
                API = null;
                OwnScriptAPIMemory = false;
            }

            is_created = false;
        }

        #endregion

        #region Execute 
#if !STANDALONE_VSBUILD


        public NSS.Blast.Jobs.blast_execute_package_burst_job CreateScriptJob(BlastPackageData package)
        {
            return CreateScriptJob(package, IntPtr.Zero, IntPtr.Zero); 
        }


        public NSS.Blast.Jobs.blast_execute_package_burst_job CreateScriptJob(BlastPackageData package, IntPtr environment, IntPtr caller)
        {
            return new Jobs.blast_execute_package_burst_job()
            {
                engine = Engine,
                package = package,
                environment = environment,
                caller = caller
            };
        }

        public NSS.Blast.Jobs.blast_execute_package_ssmd_burst_job CreateSSMDJob(BlastPackageData package, IntPtr ssmd_data, int ssmd_count)
        {
            return CreateSSMDJob(package, IntPtr.Zero, ssmd_data, ssmd_count); 
        }

        public NSS.Blast.Jobs.blast_execute_package_ssmd_burst_job CreateSSMDJob(BlastPackageData package, IntPtr environment, IntPtr ssmd_data, int ssmd_count)
        {
            return new Jobs.blast_execute_package_ssmd_burst_job()
            {
                engine = Engine,
                package = package,
                environment = environment,
                data_buffer = ssmd_data,
                ssmd_datacount = ssmd_count
            };
        }

#endif
        //
        // after problems with overloads in partially bursted structures i just settled for a dumb list of overloads, its probably fixed in a future release of burst 
        //

        /// <summary>
        /// Execute a Package
        /// </summary>
        /// <param name="package">the package data</param>
        /// <returns>succes or an error code</returns>
        public BlastError Execute(in BlastPackageData package)
        {
            return Execute(Engine, in package, IntPtr.Zero, IntPtr.Zero);
        }

        /// <summary>
        /// Execute a Package
        /// </summary>
        /// <param name="package">the package data to execute</param>
        /// <param name="environment">the environment data to use</param>
        /// <returns>succes or an error code</returns>
        [BurstDiscard]
        public BlastError Execute(in BlastPackageData package, IntPtr environment)
        {
            return Execute(Engine, in package, environment, IntPtr.Zero);
        }

        /// <summary>
        /// Execute a Package
        /// </summary>
        /// <param name="package">the package data to execute</param>
        /// <param name="environment">the environment data to use</param>
        /// <param name="caller">the caller data to use</param>
        /// <returns>succes or an error code</returns>
        [BurstDiscard]
        public BlastError Execute(in BlastPackageData package, IntPtr environment, IntPtr caller)
        {
            return Execute(Engine, in package, environment, caller);
        }

        /// <summary>
        /// Execute a Package
        /// </summary>
        /// <param name="package">the package data to execute</param>
        /// <returns>succes or an error code</returns>
        [BurstDiscard]
        public BlastError Execute(in BlastScriptPackage package)
        {
            return Execute(Engine, in package.Package, IntPtr.Zero, IntPtr.Zero);
        }

        /// <summary>
        /// Execute a Package
        /// </summary>
        /// <param name="package">the package data to execute</param>
        /// <param name="environment">the environment data to use</param>
        /// <returns>succes or an error code</returns>
        [BurstDiscard]
        public BlastError Execute(in BlastScriptPackage package, IntPtr environment)
        {
            return Execute(Engine, in package.Package, environment, IntPtr.Zero);
        }
        /// <summary>
        /// Execute a Package
        /// </summary>
        /// <param name="package">the package data to execute</param>
        /// <param name="environment">the environment data to use</param>
        /// <param name="caller">the caller data to use</param>
        /// <returns>succes or an error code</returns>
        [BurstDiscard]
        public BlastError Execute(in BlastScriptPackage package, IntPtr environment, IntPtr caller)
        {
            return Execute(Engine, in package.Package, environment, caller);
        }
        /// <summary>
        /// Execute a Package in ssmd mode
        /// </summary>
        /// <param name="package">the package data to execute</param>
        /// <param name="ssmd_data">data for each entity|script|component</param>
        /// <param name="ssmd_count">the number of data elements</param>
        /// <returns>succes or an error code</returns>
        [BurstDiscard]
        public BlastError Execute(in BlastPackageData package, [NoAlias]IntPtr ssmd_data, int ssmd_count)
        {
            return Execute(package, IntPtr.Zero, ssmd_data, ssmd_count);
        }
        /// <summary>
        /// Execute a Package in ssmd mode
        /// </summary>
        /// <param name="package">the package data to execute</param>
        /// <param name="environment">the environment data to use</param>
        /// <param name="ssmd_data">data for each entity|script|component</param>
        /// <param name="ssmd_count">the number of data elements</param>
        /// <returns>succes or an error code</returns>
        [BurstDiscard]
        public BlastError Execute(in BlastPackageData package, [NoAlias]IntPtr environment, [NoAlias]IntPtr ssmd_data, int ssmd_count)
        {
            return Blast.Execute(Engine, package, environment, ssmd_data, ssmd_count);
        }
        /// <summary>
        /// Execute a Package in ssmd mode
        /// </summary>
        /// <param name="package">the package data to execute</param>
        /// <param name="ssmd_data">data for each entity|script|component</param>
        /// <param name="ssmd_count">the number of data elements</param>
        /// <returns>succes or an error code</returns>
        [BurstDiscard]
        public BlastError Execute(in BlastScriptPackage package, [NoAlias]IntPtr ssmd_data, int ssmd_count)
        {
            return Execute(package.Package, IntPtr.Zero, ssmd_data, ssmd_count);
        }

        /// <summary>
        /// Execute a Package in ssmd mode
        /// </summary>
        /// <param name="package">the package data to execute</param>
        /// <param name="environment">the environment data to use</param>
        /// <param name="ssmd_data">data for each entity|script|component</param>
        /// <param name="ssmd_count">the number of data elements</param>
        /// <returns>succes or an error code</returns>
        [BurstDiscard]
        public BlastError Execute(in BlastScriptPackage package, [NoAlias]IntPtr environment, [NoAlias]IntPtr ssmd_data, int ssmd_count)
        {
            return Execute(package.Package, environment, ssmd_data, ssmd_count);
        }

        /// <summary>
        /// Execute a Package
        /// </summary>
        /// <param name="blast">Blast engine data</param>
        /// <param name="package">the package data to execute</param>
        /// <param name="environment">the environment data to use</param>
        /// <param name="caller"></param>
        /// <returns>succes or an error code</returns>
        [BurstDiscard]
        public static BlastError Execute(in BlastEngineDataPtr blast, in BlastPackageData package, [NoAlias]IntPtr environment, [NoAlias]IntPtr caller)
        {
            if (!blast.IsCreated) return BlastError.error_blast_not_initialized;
            if (!package.IsAllocated) return BlastError.error_package_not_allocated;


            unsafe
            {
                blaster.SetPackage(package);
                return (BlastError)blaster.Execute(blast, environment, caller);
            }
        }

        /// <summary>
        /// Execute a Package in ssmd mode
        /// </summary>
        /// <param name="blast"></param>
        /// <param name="package">the package data to execute</param>
        /// <param name="environment">the environment data to use</param>
        /// <param name="ssmd_data">data for each entity|script|component</param>
        /// <param name="ssmd_count">the number of data elements</param>
        /// <returns>succes or an error code</returns>
        [BurstDiscard]
        public static BlastError Execute(in BlastEngineDataPtr blast, in BlastPackageData package, [NoAlias]IntPtr environment, [NoAlias]IntPtr ssmd_data, int ssmd_count)
        {
            if (!blast.IsCreated) return BlastError.error_blast_not_initialized;
            if (!package.IsAllocated) return BlastError.error_package_not_allocated;
            if (ssmd_count == 0) return BlastError.error_nothing_to_execute;
            
            unsafe
            {
                ssmd_blaster.SetPackage(package);
                return (BlastError)ssmd_blaster.Execute(blast, environment, (BlastSSMDDataStack*)ssmd_data, ssmd_count);
            }
        }


#endregion

#region Package

        /// <summary>
        /// Complile the code and package into blastscriptpackage 
        /// </summary>
        /// <param name="code">the script code</param>
        /// <param name="options">compiler options</param>
        /// <returns>the blastscript package</returns>
        public BlastScriptPackage Package(string code, BlastCompilerOptions options = null)
        {
            BlastScript script = BlastScript.FromText(code); 

            BlastError res = Package(script, options);
            if (res == BlastError.success) 
            {
                BlastScriptPackage package = script.Package;
                script.Package = null; 
                return package;
            }
            else
            {
                return null; 
            }
        }

        /// <summary>
        /// Compile and package the script 
        /// </summary>
        /// <param name="script">the script to compile</param>
        /// <param name="options">compiler options</param>
        /// <returns>the blastscript package</returns>
        public BlastError Package(BlastScript script, BlastCompilerOptions options = null)
        {
            return Blast.Package(Engine, script, options);
        }
        /// <summary>
        /// Compile and package script code
        /// </summary>
        /// <param name="blast">blast engine data</param>
        /// <param name="code">script code to compile</param>
        /// <param name="options">compiler options</param>
        /// <returns>the code packaged and ready to execute</returns>
        static public BlastScriptPackage Package(BlastEngineDataPtr blast, string code, BlastCompilerOptions options = null)
        {
            BlastScript script = BlastScript.FromText(code);

            if (Package(blast, BlastScript.FromText(code), options) == BlastError.success)
            {
                BlastScriptPackage package = script.Package;
                script.Package = null;
                return package;
            }
            else
            {
                return null; 
            }
        }

        /// <summary>
        /// Compile and package script code
        /// </summary>
        /// <param name="blast">blast engine data</param>
        /// <param name="script">script code to compile</param>
        /// <param name="options">compiler options</param>
        /// <returns>the code packaged and ready to execute</returns>
        static public BlastError Package(BlastEngineDataPtr blast, BlastScript script, BlastCompilerOptions options = null)
        {
            return BlastCompiler.CompilePackage(blast, script, options);
        }


        /// <summary>
        /// compile the compiler intermediate
        /// </summary>
        /// <param name="blast">blast engine data</param>
        /// <param name="script">script</param>
        /// <param name="options">compiler options</param>
        /// <returns></returns>
        public IBlastCompilationData Intermediate(BlastEngineDataPtr blast, BlastScript script, BlastCompilerOptions options = null)
        {
            return BlastCompiler.Compile(blast, script, options);
        }

#endregion

#region Constants 

        /// <summary>
        /// list all value operations, these operations directly encode constant values 
        /// -> burst should be able to access this if needed.. 
        /// </summary>
        public static blast_operation[] ValueOperations =
        {
            blast_operation.pi,
            blast_operation.inv_pi,
            blast_operation.epsilon,
            blast_operation.infinity,
            blast_operation.negative_infinity,
            blast_operation.nan,
            blast_operation.min_value,

            blast_operation.value_0,
            blast_operation.value_1,
            blast_operation.value_2,
            blast_operation.value_3,
            blast_operation.value_4,
            blast_operation.value_8,
            blast_operation.value_10,
            blast_operation.value_16,
            blast_operation.value_24,
            blast_operation.value_32,
            blast_operation.value_64,
            blast_operation.value_100,
            blast_operation.value_128,
            blast_operation.value_256,
            blast_operation.value_512,
            blast_operation.value_1000,
            blast_operation.value_1024,
            blast_operation.value_30,
            blast_operation.value_45,
            blast_operation.value_90,
            blast_operation.value_180,
            blast_operation.value_270,
            blast_operation.value_360,
            blast_operation.inv_value_2,
            blast_operation.inv_value_3,
            blast_operation.inv_value_4,
            blast_operation.inv_value_8,
            blast_operation.inv_value_10,
            blast_operation.inv_value_16,
            blast_operation.inv_value_24,
            blast_operation.inv_value_32,
            blast_operation.inv_value_64,
            blast_operation.inv_value_100,
            blast_operation.inv_value_128,
            blast_operation.inv_value_256,
            blast_operation.inv_value_512,
            blast_operation.inv_value_1000,
            blast_operation.inv_value_1024,
            blast_operation.inv_value_30,
            blast_operation.framecount,
            blast_operation.fixedtime,
            blast_operation.time,
            blast_operation.fixeddeltatime,
            blast_operation.deltatime
        };

        /// <summary>
        /// Constant Values
        /// </summary>
        public static float[] Constant = new float[]
        {
            math.PI,
            math.PI,
            math.EPSILON,
            math.INFINITY,
            -math.INFINITY,
            math.NAN,
            math.FLT_MIN_NORMAL,
            0f,
            1f,
            2f,
            3f,
            4f,
            8f,
            10f,
            16f,
            24f,
            32f,
            64f,
            100f,
            128f,
            256f,
            512f,
            1000f,
            1024f,
            30f,
            45f,
            90f,
            180f,
            270f,
            360f,
            1f / 2f,
            1f / 3f,
            1f / 4f,
            1f / 8f,
            1f / 10f,
            1f / 16f,
            1f / 24f,
            1f / 32f,
            1f / 64f,
            1f / 100f,
            1f / 128f,
            1f / 256f,
            1f / 512f,
            1f / 1000f,
            1f / 1024f,
            1f / 30f,
            -1f, // framecount
            -1f, // fixedtime
            -1f, // time
            -1f, // fixeddeltatime
            -1f, // deltatime
        };

        /// <summary>
        /// get the default constant numeric value of the operation 
        /// </summary>
        /// <param name="op">the operation to return the constant for</param>
        /// <returns>a constant float value</returns>
        [BurstCompile]
        public static float GetConstantValueDefault(blast_operation op)
        {
            switch (op)
            {
                //case script_op.deltatime: return Time.deltaTime;
                //case script_op.time: return Time.time;
                //case script_op.framecount: return Time.frameCount;
                //case script_op.fixeddeltatime: return Time.fixedDeltaTime; 
                // The difference between 1.0f and the next representable f32/single precision number.
                case blast_operation.pi: return math.PI;
                case blast_operation.inv_pi: return 1 / math.PI;
                case blast_operation.epsilon: return math.EPSILON;
                case blast_operation.infinity: return math.INFINITY;
                case blast_operation.negative_infinity: return -math.INFINITY;
                case blast_operation.nan: return math.NAN;
                case blast_operation.min_value: return math.FLT_MIN_NORMAL;
                case blast_operation.value_0: return 0f;
                case blast_operation.value_1: return 1f;
                case blast_operation.value_2: return 2f;
                case blast_operation.value_3: return 3f;
                case blast_operation.value_4: return 4f;
                case blast_operation.value_8: return 8f;
                case blast_operation.value_10: return 10f;
                case blast_operation.value_16: return 16f;
                case blast_operation.value_24: return 24f;
                case blast_operation.value_32: return 32f;
                case blast_operation.value_64: return 64f;
                case blast_operation.value_100: return 100f;
                case blast_operation.value_128: return 128f;
                case blast_operation.value_256: return 256f;
                case blast_operation.value_512: return 512f;
                case blast_operation.value_1000: return 1000f;
                case blast_operation.value_1024: return 1024f;
                case blast_operation.value_30: return 30f;
                case blast_operation.value_45: return 45f;
                case blast_operation.value_90: return 90f;
                case blast_operation.value_180: return 180f;
                case blast_operation.value_270: return 270f;
                case blast_operation.value_360: return 360f;
                case blast_operation.inv_value_2: return 1f / 2f;
                case blast_operation.inv_value_3: return 1f / 3f;
                case blast_operation.inv_value_4: return 1f / 4f;
                case blast_operation.inv_value_8: return 1f / 8f;
                case blast_operation.inv_value_10: return 1f / 10f;
                case blast_operation.inv_value_16: return 1f / 16f;
                case blast_operation.inv_value_24: return 1f / 24f;
                case blast_operation.inv_value_32: return 1f / 32f;
                case blast_operation.inv_value_64: return 1f / 64f;
                case blast_operation.inv_value_100: return 1f / 100f;
                case blast_operation.inv_value_128: return 1f / 128f;
                case blast_operation.inv_value_256: return 1f / 256f;
                case blast_operation.inv_value_512: return 1f / 512f;
                case blast_operation.inv_value_1000: return 1f / 1000f;
                case blast_operation.inv_value_1024: return 1f / 1024f;
                case blast_operation.inv_value_30: return 1f / 30f;
                case blast_operation.framecount: return Constant[(int)(blast_operation.framecount - BlastInterpretor.opt_value)];
                case blast_operation.fixedtime: return Constant[(int)(blast_operation.framecount - BlastInterpretor.opt_value)];
                case blast_operation.time: return Constant[(int)(blast_operation.framecount - BlastInterpretor.opt_value)];
                case blast_operation.fixeddeltatime: return Constant[(int)(blast_operation.framecount - BlastInterpretor.opt_value)];
                case blast_operation.deltatime: return Constant[(int)(blast_operation.framecount - BlastInterpretor.opt_value)];

                // what to do? screw up intentionally? yeah :)
                default:

#if CHECK_STACK
#if !STANDALONE_VSBUILD
                    Standalone.Debug.LogError($"blast.get_constant_value({op}) -> not a value -> NaN returned to caller");
#else
                    System.Diagnostics.Debug.WriteLine($"blast.get_constant_value({op}) -> not a value -> NaN returned to caller");
#endif
#endif
                    return float.NaN;
            }          
        }
 
        /// <summary>
        /// get the script_op belonging to a constant value, eiter by name or value 
        /// </summary>
        /// <param name="value">the value to match</param>
        /// <param name="constant_epsilon">the epsilon to use matching constant values</param>
        /// <returns>nop on no match, nan of not a string match and no float, operation on match</returns>
        [BurstDiscard]
        public static blast_operation GetConstantValueDefaultOperation(string value, float constant_epsilon = 0.0001f)
        {
            // prevent matching to epsilon and/or min flt values (as they are very close to 0) 
            if (value == "0") return blast_operation.value_0;

            // constant names should have been done earlier. check anyway
            blast_operation constant_op = IsNamedSystemConstant(value);
            if (constant_op != blast_operation.nop)
            {
                return constant_op;
            }

            // it should be a float in system interpretation if it starts with a digit (dont call function otherwise)
            float f;
            if (float.IsNaN(f = value.AsFloat()))
            {
                return blast_operation.nan;
            }

            foreach (blast_operation value_op in ValueOperations)
            {
                float f2 = Blast.GetConstantValueDefault(value_op);
                if (f2 > f - constant_epsilon && f2 < f + constant_epsilon)
                {
                    return value_op;
                }
            }

            return blast_operation.nop;
        }

        /// <summary>
        /// check if name matches a named system constant like  'PI' or 'NaN'
        /// </summary>                                                   
        [BurstDiscard]
        public static blast_operation IsNamedSystemConstant(string name)
        {
            switch (name.Trim().ToLower())
            {
                case "pi": return blast_operation.pi;
                case "epsilon": return blast_operation.epsilon;
                case "infinity": return blast_operation.infinity;
                case "inf": return blast_operation.infinity;
                case "nan": return blast_operation.nan;
                case "flt_min": return blast_operation.min_value;
                case "deltatime": return blast_operation.deltatime;
                case "fixeddeltatime": return blast_operation.fixeddeltatime;
                case "framecount": return blast_operation.framecount; 
                case "time": return blast_operation.time; 
                case "fixedtime": return blast_operation.fixedtime; 
                default: return blast_operation.nop;
            }
        }

        /// <summary>
        /// get the value of a named system constant 
        /// </summary>
        [BurstDiscard]
        public static float GetNamedSystemConstantValue(string name)
        {
            switch (name.Trim().ToLower())
            {
                case "pi": return math.PI;
                //case "deltatime": return script_op.deltatime;
                case "epsilon": return math.EPSILON;
                case "infinity": return math.INFINITY;
                case "nan": return math.NAN;
                case "flt_min": return math.FLT_MIN_NORMAL;
                default: return float.NaN;
            }
        }

        /// <summary>
        /// lookup if identifier matches a named constant and return the matching operation 
        /// </summary>
        /// <param name="identifier"></param>
        /// <param name="constant_op"></param>
        /// <returns></returns>
        [BurstDiscard]
        public static bool TryGetNamedConstant(string identifier, out blast_operation constant_op)
        {
            identifier = identifier.ToLower().ToLower(); // TODO we do this WAY to often
            constant_op = IsNamedSystemConstant(identifier);
            return constant_op != blast_operation.nop; 
        }

        /// <summary>
        /// - synchronize constant data (for example, it updates deltatime value) 
        /// </summary>
        [BurstDiscard]
        public void Synchronize(bool update_static = true)
        {
#if !STANDALONE_VSBUILD

            SetConstantValue(blast_operation.time, UnityEngine.Time.time, update_static);
            SetConstantValue(blast_operation.fixedtime, UnityEngine.Time.fixedTime, update_static);
            SetConstantValue(blast_operation.framecount, UnityEngine.Time.frameCount, update_static);
            SetConstantValue(blast_operation.deltatime, UnityEngine.Time.deltaTime, update_static);
            SetConstantValue(blast_operation.fixeddeltatime, UnityEngine.Time.fixedDeltaTime, update_static);
#endif
        }

        public static void SynchronizeConstants()
        {
            if (IsInstantiated) Instance.Synchronize(true); 
        }

        /// <summary>
        /// used to synchronize constant value data (deltatime is, for blastscript, a constant) 
        /// can also be used to update constant values for tighter code
        /// </summary>
        [BurstDiscard]
        public void SetConstantValue(blast_operation op, float value, bool update_static = true)
        {
            Assert.IsFalse(op < blast_operation.pi || op >= blast_operation.id);

            if (update_static)
            {
                Constant[(int)(op - blast_operation.pi)] = value;
            }
            if(data != null)
            {
                data->constants[(int)op] = value;                      
            }
        }


#endregion

#region Tokens 

        /// <summary>
        /// defines tokens that can be used in script, not all tokens are referenced here, only those not built on keywords (if then/ switch etc.)
        /// </summary>
        static public BlastScriptTokenDefinition[] Tokens = new BlastScriptTokenDefinition[] 
        {
            new BlastScriptTokenDefinition(BlastScriptToken.Add, '+', "adds 2 operands together, supports all datatypes"),
            new BlastScriptTokenDefinition(BlastScriptToken.Substract, '-', "substracts right operand from left, supports all datatypes"),
            new BlastScriptTokenDefinition(BlastScriptToken.Divide, '/', "divides left operand by right, supports all datatypes"),
            new BlastScriptTokenDefinition(BlastScriptToken.Multiply, '*', "multiplies operands, supports all datatypes"),
            new BlastScriptTokenDefinition(BlastScriptToken.Equals, '=', "compares operands, supports all datatypes, vector compares return a scalar"),
#if SUPPORT_TERNARY
            new BlastScriptTokenDefinition(BlastScriptToken.Ternary, '?'),
#endif
            new BlastScriptTokenDefinition(BlastScriptToken.TernaryOption, ':', "ternary option and case seperation, ternary use not yet supported"),
            new BlastScriptTokenDefinition(BlastScriptToken.SmallerThen, '<', "compare operands, supports all datatypes"),
            new BlastScriptTokenDefinition(BlastScriptToken.GreaterThen, '>', "compare operands, supports all datatypes"),
            new BlastScriptTokenDefinition(BlastScriptToken.And, '&', "logical and, anything != 0 == true, supports all datatypes"),
            new BlastScriptTokenDefinition(BlastScriptToken.Or, '|', "logical or, anything != 0 == true, supports all datatypes"),
            new BlastScriptTokenDefinition(BlastScriptToken.Xor, '^', "logical xor, anything != 0 == true, supports all datatypes"),
            new BlastScriptTokenDefinition(BlastScriptToken.Not, '!', "logical not, anything != 0 == true, supports all datatypes"),
            new BlastScriptTokenDefinition(BlastScriptToken.OpenParenthesis, '(', "open a compound"),
            new BlastScriptTokenDefinition(BlastScriptToken.CloseParenthesis, ')', "close a compound"),

            new BlastScriptTokenDefinition(BlastScriptToken.DotComma, ';', "statement terminator"),
            new BlastScriptTokenDefinition(BlastScriptToken.Comma, ',', "statement seperator"),
                            
            // treat everything left as string literal for now
            new BlastScriptTokenDefinition(BlastScriptToken.Identifier, (char)0, "any identifier"),
            new BlastScriptTokenDefinition(BlastScriptToken.Indexer, '.', "indexer - not supported"),
            new BlastScriptTokenDefinition(BlastScriptToken.IndexOpen, '[', "indexer open - not supported"),
            new BlastScriptTokenDefinition(BlastScriptToken.IndexClose, ']', "indexer close - not supported"),
        };

        /// <summary>
        /// Reserverd words by BLAST Script 
        /// </summary>
        /// <remarks>
        /// Reserved words are keywords used by blast to identify built in (flow)control operations:
        /// <code>
        /// 
        /// Flow control:
        ///     IF THEN ELSE
        ///     SWITCH CASE DEFAULT    
        ///     WHILE | FOR
        ///     
        /// Compiler control:
        ///     DEFINE 
        ///     INPUT
        ///     OUTPUT
        ///     VALIDATE
        /// 
        /// And any defined function name.
        /// 
        /// </code>
        /// 
        /// </remarks>
        static public string[] ReservedWords = new string[]
        {
           "if", "then", "else",
           "while",
           "switch",
           "case",
           "default",
           "for"
        };

        /// <summary>
        /// check if the operation is a jump (jz, jnz, jump, jump_back)
        /// </summary>
        /// <param name="op">operation to check</param>
        /// <returns>true if a jump</returns>
        [BurstCompile]
        public static bool IsJumpOperation(blast_operation op)
        {
            return op == blast_operation.jump || op == blast_operation.jump_back || op == blast_operation.jz || op == blast_operation.jnz || op == blast_operation.long_jump;
        }

        /// <summary>
        /// check if the operation is a jump (jz, jnz, jump, jump_back)
        /// </summary>
        /// <param name="op">operation to check</param>
        /// <returns>true if a jump</returns>
        [BurstCompile]
        public static bool IsConstantJumpOperation(blast_operation op)
        {
            return op == blast_operation.jump || op == blast_operation.jump_back || op == blast_operation.long_jump;
        }

        /// <summary>
        /// returns true for ssmd valid operations: 
        /// 
        ///        add = 2,  
        ///        substract = 3,
        ///        divide = 4,
        ///        multiply = 5,
        ///        and = 6,
        ///        or = 7,
        ///        not = 8,
        ///        xor = 9,
        ///
        ///        greater = 10,
        ///        greater_equals = 11,
        ///        smaller = 12,
        ///        smaller_equals,
        ///        equals,
        ///        not_equals
        ///        
        ///        max
        ///        min
        /// </summary>
        /// <param name="op">the operation to check</param>
        /// <returns>true if handled by the ssmd sequencer</returns>
        [BurstCompile]
        static public bool IsOperationSSMDHandled(blast_operation op)
        {
            return (op >= blast_operation.add && op <= blast_operation.not_equals)
                ||
                op == blast_operation.max || op == blast_operation.min;
        }


        /// <summary>
        /// visualize a list of tokens and identifiers into a somewhat readable string
        /// </summary>
        /// <param name="tokens">the tuples with token and identifier</param>
        /// <param name="idx">start of range to view</param>
        /// <param name="idx_max">end of range to view</param>
        /// <returns>a single line string with token descriptions</returns>
        [BurstDiscard]
        public static string VisualizeTokens(List<Tuple<BlastScriptToken, string>> tokens, int idx, int idx_max)
        {
            StringBuilder sb = StringBuilderCache.Acquire();

            // obviously should be more verbose ... todo
            for (int i = idx; i <= idx_max; i++)
            {
                if (i < tokens.Count)
                {
                    Tuple<BlastScriptToken, string> token = tokens[i];
                    sb.Append(token.Item1);
                    sb.Append(" ");
                    sb.Append(token.Item2);
                    sb.Append(" ");
                }
            }
            return StringBuilderCache.GetStringAndRelease(ref sb);
        }

        /// <summary>
        /// get the corresponding operation opcode from a given script token 
        /// </summary>
        /// <param name="token">the script token</param>
        /// <returns>corresponding opcode</returns>
        [BurstCompile]
        public static blast_operation GetBlastOperationFromToken(BlastScriptToken token)
        {
            switch (token)
            {
                case BlastScriptToken.Add: return blast_operation.add;
                case BlastScriptToken.Substract: return blast_operation.substract;
                case BlastScriptToken.Divide: return blast_operation.divide;
                case BlastScriptToken.Multiply: return blast_operation.multiply;
                case BlastScriptToken.And: return blast_operation.and;
                case BlastScriptToken.Or: return blast_operation.or;
                case BlastScriptToken.Not: return blast_operation.not;
                case BlastScriptToken.Xor: return blast_operation.xor;
                case BlastScriptToken.Equals: return blast_operation.equals;
                case BlastScriptToken.NotEquals: return blast_operation.not_equals;
                case BlastScriptToken.GreaterThen: return blast_operation.greater;
                case BlastScriptToken.SmallerThen: return blast_operation.smaller;
                case BlastScriptToken.GreaterThenEquals: return blast_operation.greater_equals;
                case BlastScriptToken.SmallerThenEquals: return blast_operation.smaller_equals;
                default: return blast_operation.nop;
            }
        }



        /// <summary>
        /// determine precedance of operations, if a must be executed before b then -1 is returned, if b must be executed first 1 is returned
        /// on equal operation precedance 0 is returned 
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static int OperationPrecedance(blast_operation a, blast_operation b)
        {
            switch(a)
            {
                case blast_operation.add:
                case blast_operation.substract:
                    switch (b)
                    {
                        case blast_operation.add:
                        case blast_operation.substract: return 0;

                        case blast_operation.multiply:
                        case blast_operation.divide: return 1;
                    }
                    break;

                case blast_operation.multiply:
                case blast_operation.divide:

                    switch(b)
                    {
                        case blast_operation.add:
                        case blast_operation.substract: return -1;

                        case blast_operation.multiply:
                        case blast_operation.divide:   return 0; 
                    }
                    break; 

            }

            return 0;
        }


#endregion

#region Functions 

     
        /// <summary>
        /// get the function for a sequence of op, add -> adda
        /// </summary>
        [BurstCompile]
        static internal blast_operation GetSequenceOperation(blast_operation op)
        {
            // transform the sequence into a function call matching the operation 
            switch (op)
            {
                case blast_operation.add: return blast_operation.adda;
                case blast_operation.substract: return blast_operation.suba;
                case blast_operation.divide: return blast_operation.diva;
                case blast_operation.multiply: return blast_operation.mula;
                case blast_operation.and: return blast_operation.all;
                case blast_operation.or: return blast_operation.any;
            }
            return blast_operation.nop;
        }


        /// <summary>
        /// return if there is a sequence function for the operation
        /// </summary>
        [BurstCompile]
        static internal bool HasSequenceOperation(blast_operation op)
        {
            // transform the sequence into a function call matching the operation 
            switch (op)
            {
                case blast_operation.add:
                case blast_operation.substract:
                case blast_operation.divide:
                case blast_operation.multiply:
                case blast_operation.and:
                case blast_operation.or: return true;
            }
            return false;
        }

#endregion

#region Blast Script API:   Compiletime Function Pointers 

        /// <summary>
        /// the current set of script accessible functions, check OwnScriptAPIMemory to see if this instance of blast is the owner of the memory used in the API
        /// </summary>
        public BlastScriptAPI API;

        /// <summary>
        /// true if memory is owned by this instance of blast and should be destroyed with it 
        /// </summary>
        private bool OwnScriptAPIMemory; 


#endregion

#region Designtime CompileRegistry

        /// <summary>
        /// Enumerates all scripts known by blast 
        /// </summary>
        [BurstDiscard]
        static public IEnumerable<BlastScript> Scripts => NSS.Blast.Register.BlastScriptRegistry.All();


#if UNITY_EDITOR
        /// <summary>
        /// Designtime compilation of script registry into hpc jobs
        /// </summary>
        [BurstDiscard]
        static public void CompileRegistry(Blast blast, string target)
        {
            List<BlastScript> scripts = NSS.Blast.Register.BlastScriptRegistry.All().ToList();
            if (scripts.Count > 0)
            {
                BlastCompilerOptions options = new BlastCompilerOptions()
                {
                    Optimize = false,
                    ConstantEpsilon = 0.001f
                };

                List<string> code = new List<string>();
                foreach (BlastScript script in scripts)
                {
                    HPCCompilationData data = BlastCompiler.CompileHPC(blast.Engine, script, options);
                    if (data.IsOK)
                    {
                        code.Add(data.HPCCode);
                    }
                    else
                    {
                        Debug.LogError("failed to compile script: " + script);
                    }
                }

                File.WriteAllText(target, HPCNamespace.CombineScriptsIntoNamespace(code));
            }
        }
#endif
#endregion

#region Runtime compilation of configuration into compiletime script for use next compilation

        /// <summary>
        /// compile the script into the designtime registry 
        /// </summary>
        /// <param name="blast">blast engine data</param>
        /// <param name="script">the script to compile</param>
        /// <param name="script_directory">the directory to place the script in</param>
        /// <returns>true on success</returns>
        static public bool CompileIntoDesignTimeRegistry(BlastEngineDataPtr blast, BlastScript script, string script_directory)

        {
            Assert.IsNotNull(script);
            Assert.IsFalse(string.IsNullOrWhiteSpace(script.Name));
            Assert.IsFalse(string.IsNullOrWhiteSpace(script_directory));

            BlastCompilerOptions options = new BlastCompilerOptions()
            {
                Optimize = false,
                ConstantEpsilon = 0.001f
            };

            HPCCompilationData data = BlastCompiler.CompileHPC(blast, script, options);
            if (!data.IsOK)
            {
#if !STANDALONE_VSBUILD
                Debug.LogError("failed to compile script: " + script);
#else
                System.Diagnostics.Debug.WriteLine("ERROR: Failed to compile script: " + script); 
#endif
                return false;
            }

            string target = script_directory + "//" + script.Name + ".cs";

            try
            {
                File.WriteAllText(
                    target,
                    HPCNamespace.CombineScriptsIntoNamespace(new string[] { data.HPCCode }));
            }
            catch(Exception ex)
            {
                // 99% sure this is an IO error
#if !STANDALONE_VSBUILD
                Debug.LogError($"Blast.CompileIntoRegistry: Failed to write script '{script}' to target location at: {target}\n\nError: {ex.ToString()}\n");
#else
                System.Diagnostics.Debug.WriteLine($"Blast.CompileIntoRegistry: Failed to write script '{script}' to target location at: {target}\n\nError: {ex.ToString()}\n");
#endif

                return false; 
            }
            return true;
        }


#endregion

#region HPC Jobs 
        static Dictionary<int, IBlastHPCScriptJob> _hpc_jobs = null;
        [BurstDiscard]
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member 'Blast.HPCJobs'
        public Dictionary<int, IBlastHPCScriptJob> HPCJobs
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member 'Blast.HPCJobs'
        {
            get
            {
                if (is_created)
                {
                    lock (mt_lock)
                    {
                        if (_hpc_jobs == null)
                        {
                            _hpc_jobs = new Dictionary<int, IBlastHPCScriptJob>();
                            foreach (var j in Register.BlastReflect.FindHPCJobs())
                            {
                                _hpc_jobs.Add(j.ScriptId, j);
                            }
                        }
                        return Blast._hpc_jobs;
                    }
                }
                return null;
            }
        }
        [BurstDiscard]
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member 'Blast.GetHPCJob(int)'
        public IBlastHPCScriptJob GetHPCJob(int script_id)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member 'Blast.GetHPCJob(int)'
        {
            if (is_created && script_id > 0)
            {
                IBlastHPCScriptJob job;
                if (HPCJobs.TryGetValue(script_id, out job))
                {
                    return job;
                }
            }
            return null;
        }
#endregion

#region bytecode reading / tostring

        /// <summary>
        /// get somewhat readable assembly from bytes
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        [BurstDiscard]
        unsafe public static string GetReadableByteCode(in byte* bytes, in int length)
        {
            StringBuilder sb = new StringBuilder();

            int i = 0;
            int asnumber = 0; 

            while (i < length)
            {
                blast_operation op = (blast_operation)bytes[i];
                if(asnumber > 0)
                {
                    asnumber--;
                    sb.Append($"#{((byte)op).ToString().PadLeft(3, '0')} ");
                    i++;
                    continue; 
                }
                switch (op)
                {
                    case blast_operation.nop: sb.Append("\n"); break;

                    case blast_operation.assign: sb.Append("assign "); break;
                    case blast_operation.assigns: sb.Append("assign s"); break;
                    case blast_operation.assignf: sb.Append("assign f"); break;
                    case blast_operation.assignfe: sb.Append("assign fe"); break;

                    case blast_operation.assignfn: sb.Append("assign fn"); break;
                    case blast_operation.assignfen: sb.Append("assign fen"); break;

                    case blast_operation.assignv: sb.Append("assign vector"); break;

                    case blast_operation.add: sb.Append("+ "); break;
                    case blast_operation.substract: sb.Append("- "); break;
                    case blast_operation.multiply: sb.Append("* "); break;
                    case blast_operation.divide: sb.Append("/ "); break;
                    case blast_operation.and: sb.Append("& "); break;
                    case blast_operation.or: sb.Append("| "); break;
                    case blast_operation.not: sb.Append("! "); break;
                    case blast_operation.xor: sb.Append("^ "); break;
                    case blast_operation.smaller: sb.Append("< "); break;
                    case blast_operation.greater: sb.Append("> "); break;
                    case blast_operation.smaller_equals: sb.Append("<= "); break;
                    case blast_operation.greater_equals: sb.Append(">= "); break;
                    case blast_operation.equals: sb.Append("= "); break;
                    case blast_operation.not_equals: sb.Append("!= "); break;

                    case blast_operation.ret: sb.Append("return "); break;
                    case blast_operation.yield: sb.Append("yield "); break;

                    case blast_operation.begin: sb.Append("("); break;
                    case blast_operation.end: sb.Append(")"); break;

                    case blast_operation.jz: sb.Append("jz "); break;
                    case blast_operation.jnz: sb.Append("jnz "); break;

                    case blast_operation.jump: sb.Append("jump "); break;
                    case blast_operation.jump_back: sb.Append("jumpback "); break;
                    case blast_operation.long_jump: sb.Append("jumplong "); break;

                    case blast_operation.push: sb.Append("push "); break;
                    case blast_operation.pop: sb.Append("pop "); break;
                    case blast_operation.pushv: sb.Append("pushv "); break;
                    case blast_operation.peek: sb.Append("peek "); break;
                    case blast_operation.pushf: sb.Append("pushf "); break;
                    case blast_operation.pushc: sb.Append("pushc "); break;

                    case blast_operation.fma:
                        break;
                    case blast_operation.fmod: sb.Append("fmod "); break;
                    case blast_operation.csum: sb.Append("csum "); break;
                    case blast_operation.trunc: sb.Append("trunc "); break; 

                    case blast_operation.adda:
                        break;
                    case blast_operation.mula:
                        break;
                    case blast_operation.diva:
                        break;
                    case blast_operation.suba:
                        break;
                    case blast_operation.all:
                        break;
                    case blast_operation.any:
                        break;
                    case blast_operation.abs:
                        break;
                    case blast_operation.select:
                        break;
                    case blast_operation.random:
                        break;
                    case blast_operation.seed:
                        break;
                    case blast_operation.max:
                        break;
                    case blast_operation.min:
                        break;
                    case blast_operation.maxa:
                        break;
                    case blast_operation.mina:
                        break;
                    case blast_operation.value_0: sb.Append("0 "); break;
                    case blast_operation.value_1: sb.Append("1 "); break;
                    case blast_operation.value_2: sb.Append("2 "); break;
                    case blast_operation.value_3: sb.Append("3 "); break;
                    case blast_operation.value_4: sb.Append("4 "); break;
                    case blast_operation.value_8: sb.Append("8 "); break;
                    case blast_operation.value_10: sb.Append("10 "); break;
                    case blast_operation.value_16: sb.Append("16 "); break;
                    case blast_operation.value_24: sb.Append("24 "); break;
                    case blast_operation.value_30: sb.Append("30 "); break;
                    case blast_operation.value_32: sb.Append("32 "); break;
                    case blast_operation.value_45: sb.Append("45 "); break;
                    case blast_operation.value_64: sb.Append("64 "); break;
                    case blast_operation.value_90: sb.Append("90 "); break;
                    case blast_operation.value_100: sb.Append("100 "); break;
                    case blast_operation.value_128: sb.Append("128 "); break;
                    case blast_operation.value_180: sb.Append("180 "); break;
                    case blast_operation.value_256: sb.Append("256 "); break;
                    case blast_operation.value_270: sb.Append("270 "); break;
                    case blast_operation.value_360: sb.Append("360 "); break;
                    case blast_operation.value_512: sb.Append("512 "); break;
                    case blast_operation.value_1024: sb.Append("1024 "); break;
                    case blast_operation.inv_value_2: sb.Append((1f / 2f).ToString("0.000") + " "); break;
                    case blast_operation.inv_value_3: sb.Append((1f / 3f).ToString("0.000") + " "); break;
                    case blast_operation.inv_value_4: sb.Append((1f / 4f).ToString("0.000") + " "); break;
                    case blast_operation.inv_value_8: sb.Append((1f / 8f).ToString("0.000") + " "); break;
                    case blast_operation.inv_value_10: sb.Append((1f / 10f).ToString("0.000") + " "); break;
                    case blast_operation.inv_value_16: sb.Append((1f / 16f).ToString("0.000") + " "); break;
                    case blast_operation.inv_value_24: sb.Append((1f / 24f).ToString("0.000") + " "); break;
                    case blast_operation.inv_value_30: sb.Append((1f / 30f).ToString("0.000") + " "); break;
                    case blast_operation.inv_value_32: sb.Append((1f / 32f).ToString("0.000") + " "); break;
                    case blast_operation.inv_value_64: sb.Append((1f / 64f).ToString("0.000") + " "); break;
                    case blast_operation.inv_value_100: sb.Append((1f / 100f).ToString("0.000") + " "); break;
                    case blast_operation.inv_value_128: sb.Append((1f / 128f).ToString("0.000") + " "); break;
                    case blast_operation.inv_value_256: sb.Append((1f / 256f).ToString("0.000") + " "); break;
                    case blast_operation.inv_value_512: sb.Append((1f / 512f).ToString("0.000") + " "); break;
                    case blast_operation.inv_value_1024: sb.Append((1f / 1024f).ToString("0.000") + " "); break;

                    case blast_operation.fixeddeltatime: sb.Append("fixeddeltatime "); break;
                    case blast_operation.deltatime: sb.Append("deltatime "); break;
                    case blast_operation.framecount: sb.Append("framecount "); break;
                    case blast_operation.fixedtime: sb.Append("fixedtime "); break;
                    case blast_operation.time: sb.Append("time "); break;
                    
                    case blast_operation.constant_f1: sb.Append("cf1 "); asnumber = 4; break;
                    case blast_operation.constant_f1_h: sb.Append("cf1h "); asnumber = 2; break;
                    case blast_operation.constant_long_ref: sb.Append("clref "); asnumber = 2; break;
                    case blast_operation.constant_short_ref: sb.Append("csref "); asnumber = 1; break;

                    case blast_operation.ex_op:
                        i++;
                        extended_blast_operation ex = (extended_blast_operation)bytes[i];
                        switch (ex)
                        {
                            case extended_blast_operation.nop: break;
                            case extended_blast_operation.exp:
                            case extended_blast_operation.log2:
                            case extended_blast_operation.exp10:
                            case extended_blast_operation.log10:
                            case extended_blast_operation.logn:
                            case extended_blast_operation.cross:
                            case extended_blast_operation.dot:
                            case extended_blast_operation.ex:
                            case extended_blast_operation.sqrt:
                            case extended_blast_operation.rsqrt:
                            case extended_blast_operation.pow:
                            case extended_blast_operation.sin: 
                            case extended_blast_operation.cos: 
                            case extended_blast_operation.tan:
                            case extended_blast_operation.atan:
                            case extended_blast_operation.atan2:
                            case extended_blast_operation.cosh: 
                            case extended_blast_operation.sinh: 
                            case extended_blast_operation.degrees: 
                            case extended_blast_operation.radians:
                            case extended_blast_operation.lerp:
                            case extended_blast_operation.slerp:
                            case extended_blast_operation.saturate:
                            case extended_blast_operation.clamp:
                            case extended_blast_operation.normalize:
                            case extended_blast_operation.ceil:
                            case extended_blast_operation.floor:
                            case extended_blast_operation.frac:
                            case extended_blast_operation.remap:
                            case extended_blast_operation.unlerp:
                            case extended_blast_operation.ceillog2:
                            case extended_blast_operation.floorlog2:
                            case extended_blast_operation.ceilpow2: 
                                sb.Append($"{ex} ");
                                break;

                            case extended_blast_operation.call:
                                break;
                        }
                        break;


                    default:
                        // value by id 
                        sb.Append($"var{(byte)op} ");
                        break;
                };


                i++;
            }
            return sb.ToString();
        }

        /// <summary>
        /// return if an error (!yield and !success)
        /// </summary>
        /// <param name="res"></param>
        /// <returns></returns>
        [BurstCompile]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]                                               
        static internal bool IsError(BlastError res)
        {
            return res != BlastError.success && res != BlastError.yield;
        }

        /// <summary>
        /// return if an error code actually means success 
        /// </summary>
        [BurstCompile]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static internal bool IsSuccess(BlastError res)
        {
            return res == BlastError.success;
        }

        /// <summary>
        /// get datatype information from its name 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="datatype"></param>
        /// <param name="vector_size"></param>
        /// <param name="byte_size"></param>
        /// <returns></returns>
        [BurstDiscard]
        static public bool GetDataTypeInfo(string name, out BlastVariableDataType datatype, out int vector_size, out int byte_size)
        {
            if (!string.IsNullOrWhiteSpace(name))
            {

                switch (name.ToLower().Trim())
                {
                    case "single":
                    case "float":
                    case "float1":
                    case "numeric":
                    case "numeric1":
                        datatype = BlastVariableDataType.Numeric;
                        vector_size = 1;
                        byte_size = 4;
                        return true;

                    case "float2":
                    case "numeric2":
                    case "vector2":
                        datatype = BlastVariableDataType.Numeric;
                        vector_size = 2;
                        byte_size = 8;
                        return true;

                    case "float3":
                    case "numeric3":
                    case "vector3":
                        datatype = BlastVariableDataType.Numeric;
                        vector_size = 3;
                        byte_size = 12;
                        return true;

                    case "float4":
                    case "numeric4":
                    case "vector4":
                        datatype = BlastVariableDataType.Numeric;
                        vector_size = 4;
                        byte_size = 16;
                        return true;
                }
            }

            datatype = BlastVariableDataType.Numeric;
            vector_size = 0;
            byte_size = 0;
            return false;
        }

#endregion

#region Execute Scripts (UNITY)
#if !STANDALONE_VSBUILD && FALSE
        [BurstCompile(FloatMode = FloatMode.Fast, FloatPrecision = FloatPrecision.Low, DisableSafetyChecks = true)]
        public struct burst_once : IJob
        {
            [NativeDisableUnsafePtrRestriction]
            public IntPtr blast;
            [NativeDisableUnsafePtrRestriction]
            public BlastInterpretor interpretor;

            public void Execute()
            {
                for (int i = 0; i < 1000; i++) interpretor.Execute(blast);
            }
        }

        [BurstCompile(FloatMode = FloatMode.Fast, FloatPrecision = FloatPrecision.Low, DisableSafetyChecks = true)]
        public struct burst_hpc_once : IJob
        {
            [NativeDisableUnsafePtrRestriction]
            public IntPtr blast;

            public NativeArray<float> variables;

            public FunctionPointer<BSExecuteDelegate> bsd;

            public void Execute()
            {
                unsafe
                {
                    for (int i = 0; i < 1000; i++)
                        bsd.Invoke(blast, (float*)variables.GetUnsafePtr());
                }
            }
        }

        burst_once _burst_once_job;
        burst_hpc_once _burst_hpc_once_job;

        [BurstDiscard]
        public int Execute(int script_id)
        {
            if (!is_created) return (int)BlastError.error_blast_not_initialized;
            if (script_id <= 0) return (int)BlastError.error_invalid_script_id;

            runner run = GetRunner(script_id);

            if (run == null)
            {
                return (int)BlastError.script_id_not_found;
            }
            else
            {
                return Execute(run);
            }
        }

        [BurstDiscard]
        internal int Execute(runner run)
        {
            if (!is_created) return (int)BlastError.error_blast_not_initialized;

            switch (run.Type)
            {
                case runner_type.bs:
                    if (run.Package.IsAllocated)
                    {
                        _burst_once_job.blast = Engine;
                        _burst_once_job.interpretor.SetPackage(run.Package);
                        _burst_once_job.Run();
                    }
                    break;

                case runner_type.hpc:
                    _burst_hpc_once_job.blast = Engine;
                    _burst_hpc_once_job.bsd = run.HPCJobBSD;
                    _burst_hpc_once_job.variables = run.HPCVariables;
                    _burst_hpc_once_job.Run();
                    break;

                default:
                    Debug.LogError($"runtype not supported: {run.Type}");
                    break;
            }
            return 0;
        }
#endif
#endregion


        /// <summary>
        /// support dispose, to enable using clause
        /// </summary>
        /// <remarks>
        /// using(Blast blast = Blast.Create(Allocator.Persistant))
        /// {
        /// 
        /// 
        /// 
        /// }
        /// </remarks>
        public void Dispose()
        {
            if (IsCreated)
            {
                Destroy();
            }
        }


    }


}

