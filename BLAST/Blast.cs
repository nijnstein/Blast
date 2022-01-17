using System;
using System.Collections.Generic;
using System.IO;

#if NOT_USING_UNITY
    using NSS.Blast.Standalone;
    using Unity.Assertions;
#else
    using UnityEngine;
    using UnityEngine.Assertions; 
    using Unity.Jobs;
    using Unity.Collections.LowLevel.Unsafe;
#endif

using NSS.Blast.Cache;
using NSS.Blast.Compiler;
using NSS.Blast.Register;

using Unity.Collections;
using Unity.Mathematics;
using System.Text;
using System.Linq; 
using Unity.Burst;
using NSS.Blast.Interpretor;
using NSS.Blast.SSMD;


using Random = Unity.Mathematics.Random;


namespace NSS.Blast
{
    /// <summary>
    /// BLAST execution data
    /// - used during execution of scripts
    /// - shared by all threads
    /// </summary>
    [BurstCompile]
    unsafe public struct BlastEngineData
    {
        public float* constants;

        public NonGenericFunctionPointer* functionpointers;
        public int functionpointer_count;

        public Random random;

        #region Helper Functions / Get-Set

        public void Seed(uint i)
        {
            if (i == 0)
            {
                random.InitState();
            }
            else
            {
                random.InitState(i);
            }
        }

        unsafe public NonGenericFunctionPointer GetFunctionPointer(int id)
        {
            NonGenericFunctionPointer p;
            if (TryGetFunctionPointer(id, out p))
            {
                return p;
            }
            return default(NonGenericFunctionPointer);
        }

        unsafe public bool TryGetFunctionPointer(int id, out NonGenericFunctionPointer p)
        {
            for (int i = 0; i < functionpointer_count; i++)
            {
                if (functionpointers[i].id == id)
                {
                    p = functionpointers[i];
                    return true;
                }
            }
            p = default(NonGenericFunctionPointer);
            return false;
        }

        #endregion

    }


    /// <summary>
    /// Blast Engine
    /// - use seperate instances for threads
    /// </summary>
    [BurstCompile]
    unsafe public struct Blast
    {
        public delegate int BlastExecute(int scriptid, float* p_stack, IntPtr data, IntPtr caller);

        public delegate float BlastDelegate_f0(IntPtr engine, IntPtr data, IntPtr caller);
        public delegate float BlastDelegate_f1(IntPtr engine, IntPtr data, IntPtr caller, float a);
        public delegate float BlastDelegate_f2(IntPtr engine, IntPtr data, IntPtr caller, float a, float b);
        public delegate float BlastDelegate_f3(IntPtr engine, IntPtr data, IntPtr caller, float a, float b, float c);
        public delegate float BlastDelegate_f4(IntPtr engine, IntPtr data, IntPtr caller, float a, float b, float c, float d);
        public delegate float BlastDelegate_f5(IntPtr engine, IntPtr data, IntPtr caller, float a, float b, float c, float d, float e);
        public delegate float BlastDelegate_f6(IntPtr engine, IntPtr data, IntPtr caller, float a, float b, float c, float d, float e, float f);
        public delegate float BlastDelegate_f7(IntPtr engine, IntPtr data, IntPtr caller, float a, float b, float c, float d, float e, float f, float g);
        public delegate float BlastDelegate_f8(IntPtr engine, IntPtr data, IntPtr caller, float a, float b, float c, float d, float e, float f, float g, float h);

        public const char comment = '#';

        private BlastEngineData* data;
        private Allocator allocator;
        private bool is_created;
        public bool IsCreated { get { return is_created; } }
        public IntPtr Engine => (IntPtr)data;

        // lots of statics ahead
        static internal object mt_lock = new object();

        #region Create / Destroy
        public unsafe static Blast Create(Allocator allocator)
        {
            Blast blast;
            if (mt_lock == null) mt_lock = new object();
            blast.allocator = allocator;
            blast.data = (BlastEngineData*)UnsafeUtils.Malloc(sizeof(BlastEngineData), 8, blast.allocator);
            blast.data->constants = (float*)UnsafeUtils.Malloc(4 * 256 * 2, 4, blast.allocator);
            blast.data->random = Random.CreateFromIndex(1);

            for (int b = 0; b < 256; b++)
            {
                blast.data->constants[b] = Blast.GetConstantValue((blast_operation)b);
            }

            blast.data->functionpointer_count = FunctionCalls.Count;
            blast.data->functionpointers = (NonGenericFunctionPointer*)UnsafeUtils.Malloc(sizeof(NonGenericFunctionPointer) * FunctionCalls.Count, 8, blast.allocator);

            for (int i = 0; i < FunctionCalls.Count; i++)
            {
                ExternalFunctionCall call = FunctionCalls[i];
                blast.data->functionpointers[i] = call.FunctionPointer;
            }

#if !NOT_USING_UNITY
            blast._burst_hpc_once_job = default;
            blast._burst_once_job = default;
            blast._burst_once_job.interpretor = default; 
#endif
            blast.is_created = true;
            return blast;
        }

        /// <summary>
        /// Destroy blast handler 
        /// </summary>
        public unsafe void Destroy()
        {
            if (data != null)
            {
                if (data->constants != null)
                {
                    UnsafeUtils.Free(data->constants, allocator);
                }
                if (data->functionpointers != null)
                {
                    UnsafeUtils.Free(data->functionpointers, allocator);
                }
                UnsafeUtils.Free(data, allocator);
                data = null;
            }
            is_created = false;
        }

        #endregion

        #region Execute 

        [BurstDiscard]
        public static int Execute(string code, int stack_size = 64)
        {
            Blast blast = Blast.Create(Allocator.Temp);
            try
            {
                BlastIntermediate blast_im = BlastCompiler.Compile(blast, code, new BlastCompilerOptions(stack_size)).Executable;
                return blast_im.Execute(blast.Engine);
            }
            finally
            {
                blast.Destroy(); 
            }
        }

        [BurstDiscard]
        public static int Execute(BlastScriptPackage package)
        {
            return Execute(package.Package); 
        }

        [BurstDiscard]
        public static int Execute(BlastPackageData package)
        {
            unsafe
            {
                Blast blast = Blast.Create(Allocator.Temp);
                try
                {
                    BlastInterpretor blaster = default;
                    blaster.SetPackage(package);
                    return blaster.Execute(blast.Engine);
                }
                finally
                {
                    blast.Destroy();
                }
            }
        }

        [BurstDiscard]
        public int Execute(string code)
        {
            return BlastCompiler
                .Compile(this, code, new BlastCompilerOptions(64))
                .Executable
                .Execute(Engine); 
        }

        /// <summary>
        /// execute package in given environment with attached caller data
        /// </summary>
        /// <param name="package">the package to execute</param>
        /// <param name="environment">optional environment data [global-data]</param>
        /// <param name="caller">optional callerdata [threadonly]</param>
        /// <returns>exitcode 0 on success</returns>
        [BurstDiscard]
        public int Execute(BlastScriptPackage package, IntPtr environment, IntPtr caller)
        {
            BlastInterpretor blaster = default;
            blaster.SetPackage(package.Package);
            return blaster.Execute(Engine, environment, caller);
        }


        [BurstDiscard]
        public int Execute(BlastPackageData package, IntPtr environment, BlastSSMDDataStack* ssmd_data, int ssmd_datacount)
        {
            Assert.IsTrue(package.IsAllocated, "package not allocated"); 
            Assert.IsTrue(package.PackageMode == BlastPackageMode.SSMD, "only ssmd packages are supported for this execute overload"); 

            BlastSSMDInterpretor blaster = default;
            blaster.SetPackage(package);
            return blaster.Execute((BlastEngineData*)Engine, environment, ssmd_data, ssmd_datacount); 
        }


        #endregion

        #region Package

        [BurstDiscard]
        public static BlastScriptPackage Package(BlastScript script)
        {
            Blast blast = Blast.Create(Allocator.Temp);
            try
            {
                return blast.Package(script);
            }
            finally
            {
                blast.Destroy();
            }
        }
        [BurstDiscard]
        public static BlastScriptPackage Package(string code)
        {
            return Blast.Package(BlastScript.FromText(code)); 
        }

        [BurstDiscard]
        public BlastScriptPackage Package(string code, BlastCompilerOptions options = null)
        {
            return BlastCompiler.CompilePackage(this, BlastScript.FromText(code), options);
        }

        [BurstDiscard]
        public BlastScriptPackage Package(BlastScript script, BlastCompilerOptions options = null)
        {
            return BlastCompiler.CompilePackage(this, script, options);
        }

        #endregion

        #region Constants 

        [BurstDiscard]
        public IEnumerable<blast_operation> ValueOperations()
        {
            // we dont want this as static, but on a struct its also no good as field 
            // structs dont carry arround function tables so this seems ok... 
            yield return blast_operation.pi;
            yield return blast_operation.inv_pi;
            yield return blast_operation.epsilon;
            yield return blast_operation.infinity;
            yield return blast_operation.negative_infinity;
            yield return blast_operation.nan;
            yield return blast_operation.min_value;

            yield return blast_operation.value_0;
            yield return blast_operation.value_1;
            yield return blast_operation.value_2;
            yield return blast_operation.value_3;
            yield return blast_operation.value_4;
            yield return blast_operation.value_8;
            yield return blast_operation.value_10;
            yield return blast_operation.value_16;
            yield return blast_operation.value_24;
            yield return blast_operation.value_32;
            yield return blast_operation.value_64;
            yield return blast_operation.value_100;
            yield return blast_operation.value_128;
            yield return blast_operation.value_256;
            yield return blast_operation.value_512;
            yield return blast_operation.value_1000;
            yield return blast_operation.value_1024;
            yield return blast_operation.value_30;
            yield return blast_operation.value_45;
            yield return blast_operation.value_90;
            yield return blast_operation.value_180;
            yield return blast_operation.value_270;
            yield return blast_operation.value_360;
            yield return blast_operation.inv_value_2;
            yield return blast_operation.inv_value_3;
            yield return blast_operation.inv_value_4;
            yield return blast_operation.inv_value_8;
            yield return blast_operation.inv_value_10;
            yield return blast_operation.inv_value_16;
            yield return blast_operation.inv_value_24;
            yield return blast_operation.inv_value_32;
            yield return blast_operation.inv_value_64;
            yield return blast_operation.inv_value_100;
            yield return blast_operation.inv_value_128;
            yield return blast_operation.inv_value_256;
            yield return blast_operation.inv_value_512;
            yield return blast_operation.inv_value_1000;
            yield return blast_operation.inv_value_1024;
            yield return blast_operation.inv_value_30;
            yield return blast_operation.inv_value_45;
            yield return blast_operation.inv_value_90;
            yield return blast_operation.inv_value_180;
            yield return blast_operation.inv_value_270;
            yield return blast_operation.inv_value_360;
        }

        [BurstCompile]
        public static float GetConstantValue(blast_operation op)
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
                case blast_operation.inv_value_45: return 1f / 45f;
                case blast_operation.inv_value_90: return 1f / 90f;
                case blast_operation.inv_value_180: return 1f / 180f;
                case blast_operation.inv_value_270: return 1f / 270f;
                case blast_operation.inv_value_360: return 1f / 360f;

                // what to do? screw up intentionally? yeah :)
                default:

#if CHECK_STACK
#if !NOT_USING_UNITY
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
        /// <param name="blast">blast engine data</param>
        /// <param name="value">the value to match</param>
        /// <param name="constant_epsilon">the epsilon to use matching constant values</param>
        /// <returns>nop on no match, nan of not a string match and no float, operation on match</returns>
        [BurstDiscard]
        public blast_operation GetConstantValueOperation(string value, float constant_epsilon = 0.0001f)
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
            if (float.IsNaN(f = node.AsFloat(value)))
            {
                return blast_operation.nan;
            }

            foreach (blast_operation value_op in ValueOperations())
            {
                float f2 = Blast.GetConstantValue(value_op);
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
        public blast_operation IsNamedSystemConstant(string name)
        {
            switch (name.Trim().ToLower())
            {
                case "pi": return blast_operation.pi;
                //case "deltatime": return script_op.deltatime;
                case "epsilon": return blast_operation.epsilon;
                case "infinity": return blast_operation.infinity;
                case "nan": return blast_operation.nan;
                case "flt_min": return blast_operation.min_value;
                default: return blast_operation.nop;
            }
        }

        /// <summary>
        /// get the value of a named system constant 
        /// </summary>
        [BurstDiscard]
        public float GetNamedSystemConstantValue(string name)
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


#endregion

        #region Tokens 

        /// <summary>
        /// defines tokens that can be used in script
        /// </summary>
        static public List<BlastScriptTokenDefinition> Tokens = new List<BlastScriptTokenDefinition>()
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
        /// check if the operation is a jump (jz, jnz, jump, jump_back)
        /// </summary>
        /// <param name="op">operation to check</param>
        /// <returns>true if a jump</returns>
        public static bool IsJumpOperation(blast_operation op)
        {
            return op == blast_operation.jump || op == blast_operation.jump_back || op == blast_operation.jz || op == blast_operation.jnz;
        }

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


#endregion

#region Functions 

        /// <summary>
        /// defined functions for script
        /// </summary>
        static public List<ScriptFunctionDefinition> Functions = new List<ScriptFunctionDefinition>()
        {
            new ScriptFunctionDefinition(0, "abs", 1, 1, 0, 0, blast_operation.abs),

            new ScriptFunctionDefinition(1, "min", 2, 63, 0, 0, blast_operation.min),
            new ScriptFunctionDefinition(2, "max", 2, 63, 0, 0, blast_operation.max),
            new ScriptFunctionDefinition(3, "mina", 1, 63, 1, 0, blast_operation.mina),
            new ScriptFunctionDefinition(4, "maxa", 1, 63, 1, 0, blast_operation.maxa),

            new ScriptFunctionDefinition(5, "select", 3, 3, 0, 0, blast_operation.select, "a", "b", "condition"),

            new ScriptFunctionDefinition(6, "random", 0, 2, 0, 0, blast_operation.random),  // random value from 0 to 1 (inclusive), 0 to max, or min to max depending on parameter count 1
            new ScriptFunctionDefinition((int)ReservedScriptFunctionIds.Seed, "seed", 1, 1, 0, 0, blast_operation.seed),

            new ScriptFunctionDefinition(7, "sqrt", 1, 1, 0, 0, blast_operation.sqrt),
            new ScriptFunctionDefinition(8, "rsqrt", 1, 1, 0, 0, extended_blast_operation.rsqrt),

            new ScriptFunctionDefinition(9, "lerp", 3, 3, 0, 0, blast_operation.lerp, "a", "b", "t"),
            new ScriptFunctionDefinition(10, "slerp", 3, 3, 4, 4, blast_operation.slerp, "a", "b", "t"),
            new ScriptFunctionDefinition(47, "nlerp", 3, 3, 4, 4, blast_operation.nlerp, "a", "b", "t"),

            new ScriptFunctionDefinition(11, "any", 2, 63, 0, 0, blast_operation.any),
            new ScriptFunctionDefinition(12, "all", 2, 63, 0, 0, blast_operation.all),
            new ScriptFunctionDefinition(13, "adda", 2, 63, 0, 0, blast_operation.adda),
            new ScriptFunctionDefinition(14, "suba", 2, 63, 0, 0, blast_operation.suba),
            new ScriptFunctionDefinition(15, "diva", 2, 63, 0, 0, blast_operation.diva),
            new ScriptFunctionDefinition(16, "mula", 2, 63, 0, 0, blast_operation.mula),

            new ScriptFunctionDefinition(17, "ceil", 1, 1, 0, 0, blast_operation.ceil),
            new ScriptFunctionDefinition(18, "floor", 1, 1, 0, 0, blast_operation.floor),
            new ScriptFunctionDefinition(19, "frac", 1, 1, 0, 0, blast_operation.frac),

            new ScriptFunctionDefinition(21, "sin", 1, 1, 0, 0, blast_operation.sin),
            new ScriptFunctionDefinition(22, "cos", 1, 1, 0, 0, blast_operation.cos),
            new ScriptFunctionDefinition(23, "tan", 1, 1, 0, 0, blast_operation.tan),
            new ScriptFunctionDefinition(24, "atan", 1, 1, 0, 0, blast_operation.atan),
            new ScriptFunctionDefinition(25, "cosh", 1, 1, 0, 0, blast_operation.cosh),
            new ScriptFunctionDefinition(26, "sinh", 1, 1, 0, 0, blast_operation.sinh),

            new ScriptFunctionDefinition(28, "degrees", 1, 1, 0, 0, blast_operation.degrees),
            new ScriptFunctionDefinition(29, "rad", 1, 1, 0, 0, blast_operation.radians),

            new ScriptFunctionDefinition(46, "fma", 3, 3, 0, 0, blast_operation.fma, "m1", "m2", "a1"),

            new ScriptFunctionDefinition(33, "pow", 2, 2, 0, 0, extended_blast_operation.pow, null, "a", "power"),

            // would be nice to have a parameter giving min and max vector size 
            new ScriptFunctionDefinition(32, "normalize", 1, 1, 0, 0, blast_operation.normalize, "a"),
            new ScriptFunctionDefinition(30, "saturate", 1, 1, 0, 0, blast_operation.saturate, "a"),
            new ScriptFunctionDefinition(31, "clamp", 3, 3, 0, 0, blast_operation.clamp, "a", "min", "max"),

            new ScriptFunctionDefinition(41, "log2", 1, 1,  0, 0, extended_blast_operation.log2, null, "a", "b"),
            new ScriptFunctionDefinition(42, "log10", 1, 1, 0, 0, extended_blast_operation.log10, null, "a"),
            new ScriptFunctionDefinition(43, "log", 1, 1, 0, 0, extended_blast_operation.logn, null, "a"),
            new ScriptFunctionDefinition(40, "exp", 1, 1,  0, 0, extended_blast_operation.exp, null, "exponent_of_e"),
            new ScriptFunctionDefinition(44, "exp10", 1, 1, 0, 0, extended_blast_operation.exp10, null, "a", "b"),
            new ScriptFunctionDefinition(38, "cross", 2, 2, 3, 3, extended_blast_operation.cross, null, "a", "b"),
            new ScriptFunctionDefinition(39, "dot", 2, 2, 1, 0, extended_blast_operation.dot, null, "a", "b"),

            new ScriptFunctionDefinition(45, "return", 0, 0,  0, 0,blast_operation.ret),

            new ScriptFunctionDefinition((int)ReservedScriptFunctionIds.Push, "push", 1, 4, 0, 0,blast_operation.push, "n", "a"),
            new ScriptFunctionDefinition((int)ReservedScriptFunctionIds.PushFunction, "pushf", 1, 1, 0, 0,blast_operation.pushf, "n", "a"),
            new ScriptFunctionDefinition((int)ReservedScriptFunctionIds.Pop, "pop", 0, 0, 0, 0,blast_operation.pop, "n"),
            new ScriptFunctionDefinition((int)ReservedScriptFunctionIds.Peek, "peek", 0, 1, 0, 0, blast_operation.peek, "n"),
            new ScriptFunctionDefinition((int)ReservedScriptFunctionIds.Yield, "yield", 0, 1, 0, 0,blast_operation.yield, "n"),
            new ScriptFunctionDefinition((int)ReservedScriptFunctionIds.Input, "input", 3, 3, 0, 0,blast_operation.nop, "id", "offset", "length"),
            new ScriptFunctionDefinition((int)ReservedScriptFunctionIds.Output, "output", 3, 3, 0, 0,blast_operation.nop, "id", "offset", "length"),

            new ScriptFunctionDefinition((int)ReservedScriptFunctionIds.Debug, "debug", 1, 1, 0, 0, extended_blast_operation.debug, null, "id"),
            new ScriptFunctionDefinition((int)ReservedScriptFunctionIds.Debug, "debugstack",0, 0, 0, 0, extended_blast_operation.debugstack)
        };

        public ScriptFunctionDefinition GetFunctionByScriptOp(blast_operation op)
        {
            foreach (var f in Blast.Functions)
                if (f.ScriptOp == op)
                    return f;

            // look in user defined functions 
            return null;
        }

        public ScriptFunctionDefinition GetFunctionByName(string name)
        {
            foreach (var f in Blast.Functions)
                if (string.Compare(name, f.Match, true) == 0)
                    return f;

            // look in user defined functions 
            return null;
        }

        public ScriptFunctionDefinition GetFunctionById(ReservedScriptFunctionIds function_id)
        {
            return GetFunctionById((int)function_id);
        }

        public ScriptFunctionDefinition GetFunctionById(int function_id)
        {
            foreach (var f in Blast.Functions)
                if (f.FunctionId == function_id)
                    return f;

            // look in user defined functions 
            return null;
        }

        /// <summary>
        /// returns true if the function accepts a variable sized list of paramaters 
        /// </summary>
        /// <param name="op">the operation mapping to a function</param>
        /// <returns>true if the function exists and has a variable parameter list</returns>
        public bool IsVariableParamFunction(blast_operation op)
        {
            if (op == blast_operation.nop)
            {
                return false;
            }

            ScriptFunctionDefinition function = GetFunctionByScriptOp(op);
            if (function == null)
            {
                return false;
            }

            return function.MinParameterCount != function.MaxParameterCount;
        }
#endregion

#region Compiletime Function Pointers 

        static public List<ExternalFunctionCall> FunctionCalls = new List<ExternalFunctionCall>();

        static public void DestroyAnyFunctionPointers()
        {
            if (FunctionCalls != null && FunctionCalls.Count > 0)
            {
                FunctionCalls.Clear();
            }
        }

        static public int GetMaxFunctionId()
        {
            int max = int.MinValue;
            foreach (var f in Functions)
            {
                max = math.max(max, f.FunctionId);
            }
            return max;
        }

        static public ExternalFunctionCall GetFunctionCallById(int id)
        {
            foreach (ExternalFunctionCall call in FunctionCalls)
            {
                if (call.ScriptFunction.FunctionId == id)
                {
                    return call;
                }
            }
            return null;
        }
        static public bool TryGetFunctionCallById(int id, out ExternalFunctionCall fc)
        {
            foreach (ExternalFunctionCall call in FunctionCalls)
            {
                if (call.ScriptFunction.FunctionId == id)
                {
                    fc = call;
                    return true;
                }
            }
            fc = null;
            return false;
        }

        static public ExternalFunctionCall GetFunctionCallByName(string name)
        {
            foreach (ExternalFunctionCall call in FunctionCalls)
            {
                if (string.Compare(call.Match, name, true) == 0)
                {
                    return call;
                }
            }
            return null;
        }

        static public bool ExistsFunctionCall(string name)
        {
            return GetFunctionCallByName(name) != null;
        }


        /// <summary>
        /// register functionpointer as an external call  / reserve id 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="returns"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        static public int RegisterFunction(string name, BlastVariableDataType returns, string[] parameters)
        {
            return RegisterFunction(default(NonGenericFunctionPointer), name, returns, parameters);
        }

        /// <summary>
        /// update the functionpointer for an external call 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="fp"></param>
        static public void UpdateFunctionPointer(int id, NonGenericFunctionPointer fp)
        {
            Assert.IsNotNull(FunctionCalls);

            ExternalFunctionCall call = GetFunctionCallById(id);

            Assert.IsNotNull(call);
            Assert.IsTrue(fp.id == call.ScriptFunction.FunctionId); // force it to be set on update 

            if (call != null)
            {
                call.FunctionPointer = fp;
            }
        }


        /// <summary>
        /// register functionpointer as an external call 
        /// </summary>
        /// <param name="fp"></param>
        /// <param name="name"></param>
        /// <param name="returns"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        static public int RegisterFunction(NonGenericFunctionPointer fp, string name, BlastVariableDataType returns, string[] parameters)
        {
            Assert.IsNotNull(FunctionCalls);

            ExternalFunctionCall call = GetFunctionCallByName(name);
            if (call != null)
            {
                // upgrade function pointer 
                call.FunctionPointer = fp;

                // Assert.IsTrue(false, $"external function call with name '{name}' already registrered");
                return call.ScriptFunction.FunctionId;
            }

            int id = GetMaxFunctionId() + 1;
            int parameter_count = parameters == null ? 0 : parameters.Length;

            // create the external function call 
            call = new ExternalFunctionCall(name, 1, fp);

            // attach it to a script function definition 
            call.ScriptFunction = new ScriptFunctionDefinition
            (
                id, name,
                parameter_count, parameter_count,
                0, 0,
                extended_blast_operation.call,
                call,
                parameters
            );

            // add to registred function list 
            Functions.Add(call.ScriptFunction);
            FunctionCalls.Add(call);

            return call.ScriptFunction.FunctionId;
        }


#endregion

#region Designtime CompileRegistry

        /// <summary>
        /// Enumerates all scripts known by blast 
        /// </summary>
        static public IEnumerable<BlastScript> Scripts => NSS.Blast.Register.BlastScriptRegistry.All();


#if UNITY_EDITOR
        /// <summary>
        /// Designtime compilation of script registry into hpc jobs
        /// </summary>
        static public void CompileRegistry(Blast blast, string target)
        {
            List<BlastScript> scripts = NSS.Blast.Register.BlastScriptRegistry.All().ToList(); 
            if(scripts.Count > 0)
            {
                BlastCompilerOptions options = new BlastCompilerOptions() {
                    Optimize = false, ConstantEpsilon = 0.001f
                };

                List<string> code = new List<string>(); 
                foreach (BlastScript script in scripts)
                {
                    HPCCompilationData data = BlastCompiler.CompileHPC(blast, script, options);
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

        static public bool CompileIntoRegistry(Blast blast, BlastScript script, string script_directory)
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
#if !NOT_USING_UNITY
                Debug.LogError("failed to compile script: " + script);
#else
                System.Diagnostics.Debug.WriteLine("ERROR: Failed to compile script: " + script); 
#endif
                return false;
            }

            string target = script_directory + "//" + script.Name + ".cs";
            File.WriteAllText(
                target,
                HPCNamespace.CombineScriptsIntoNamespace(new string[] { data.HPCCode }));

            return true;
        }


#endregion

#region HPC Jobs 
        static Dictionary<int, IBlastHPCScriptJob> _hpc_jobs = null;
        public Dictionary<int, IBlastHPCScriptJob> HPCJobs
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
        public IBlastHPCScriptJob GetHPCJob(int script_id)
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
        unsafe public static string GetByteCodeByteText(in byte* bytes, in int length, int column_count)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < length; i++)
            {
                sb.Append($"{bytes[i].ToString().PadLeft(3, '0').PadRight(4)}");
                if ((i + 1) % column_count == 0 || i == length - 1)
                {
                    sb.Append("\n");
                }
            }
            return sb.ToString();
        }


        unsafe public static string GetReadableByteCode(in byte* bytes, in int length)
        {
            StringBuilder sb = new StringBuilder();

            int i = 0;

            while (i < length)
            {
                blast_operation op = (blast_operation)bytes[i];

                // generate a somewhat readable assembly view
                //
                // offset |  translation
                //-------------------------------
                // 000    |  assign a 2 nop
                // 004    |  assign b (4 * 4) nop
                // etc. 
                //
                //

                switch (op)
                {
                    case blast_operation.nop: sb.Append("\n"); break;

                    case blast_operation.assign: sb.Append("assign "); break;

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

                    case blast_operation.push: sb.Append("push "); break;
                    case blast_operation.pop: sb.Append("pop "); break;
                    case blast_operation.pushv: sb.Append("pushv "); break;
                    case blast_operation.peek: sb.Append("peek "); break;
                    case blast_operation.peekv: sb.Append("peekv "); break;
                    case blast_operation.pushf: sb.Append("pushf "); break;

                    case blast_operation.fma:
                        break;
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
                    case blast_operation.lerp:
                        break;
                    case blast_operation.slerp:
                        break;
                    case blast_operation.saturate:
                        break;
                    case blast_operation.clamp:
                        break;
                    case blast_operation.normalize:
                        break;
                    case blast_operation.ceil:
                        break;
                    case blast_operation.floor:
                        break;
                    case blast_operation.frac:
                        break;
                    case blast_operation.sin:
                        break;
                    case blast_operation.cos:
                        break;
                    case blast_operation.tan:
                        break;
                    case blast_operation.atan:
                        break;
                    case blast_operation.cosh:
                        break;
                    case blast_operation.sinh:
                        break;
                    case blast_operation.degrees:
                        break;
                    case blast_operation.radians:
                        break;
                    case blast_operation.sqrt:
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
                    case blast_operation.inv_value_45: sb.Append((1f / 45f).ToString("0.000") + " "); break;
                    case blast_operation.inv_value_64: sb.Append((1f / 64f).ToString("0.000") + " "); break;
                    case blast_operation.inv_value_90: sb.Append((1f / 90f).ToString("0.000") + " "); break;
                    case blast_operation.inv_value_100: sb.Append((1f / 100f).ToString("0.000") + " "); break;
                    case blast_operation.inv_value_128: sb.Append((1f / 128f).ToString("0.000") + " "); break;
                    case blast_operation.inv_value_180: sb.Append((1f / 180f).ToString("0.000") + " "); break;
                    case blast_operation.inv_value_256: sb.Append((1f / 256f).ToString("0.000") + " "); break;
                    case blast_operation.inv_value_270: sb.Append((1f / 270f).ToString("0.000") + " "); break;
                    case blast_operation.inv_value_360: sb.Append((1f / 360f).ToString("0.000") + " "); break;
                    case blast_operation.inv_value_512: sb.Append((1f / 512f).ToString("0.000") + " "); break;
                    case blast_operation.inv_value_1024: sb.Append((1f / 1024f).ToString("0.000") + " "); break;

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
                            case extended_blast_operation.rsqrt:
                            case extended_blast_operation.pow:
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
        static public bool IsError(BlastError res)
        {
            return res != BlastError.success && res != BlastError.yield; 
        }

        /// <summary>
        /// return if an error code actually means success 
        /// </summary>
        static public bool IsSuccess(BlastError res)
        {
            return res == BlastError.success; 
        }

        #endregion

        #region Execute Scripts (UNITY)
#if !NOT_USING_UNITY
        [BurstCompile(FloatMode = FloatMode.Fast, FloatPrecision = FloatPrecision.Low, DisableSafetyChecks = true)]
        public struct burst_once : IJob
        {
            [NativeDisableUnsafePtrRestriction]
            public IntPtr blast;
            [NativeDisableUnsafePtrRestriction]
            public BlastInterpretor interpretor;

            public void Execute()
            {
                for(int i = 0; i < 1000;i++) interpretor.Execute(blast);
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

        #region Runners 

        static Dictionary<int, runner> runners;

        internal enum runner_type
        {
            bs, hpc, cs
        }

        internal runner GetRunner(int script_id)
        {
            if (!is_created) return null;
            if (script_id <= 0) return null;

            runner r = null;

            lock (mt_lock)
            {
                if (runners == null)
                {
                    runners = new Dictionary<int, runner>();
                }
                if (runners != null)
                {
                    if (runners.TryGetValue(script_id, out r)) return r;
                }
            }

            // to create a runner iterate fastest method first
#if !NOT_USING_UNITY
            IBlastHPCScriptJob job = GetHPCJob(script_id);
            if (job != null)
            {
                r = new runner(script_id, runner_type.hpc)
                {
                    HPCJobBSD = job.GetBSD(),
                    HPCVariables = new NativeArray<float>(new float[20], Allocator.TempJob)
                };
            }
            else
#endif
            {
                BlastScriptPackage pkg = GetPackage(script_id);
                if (pkg != null)
                {
                    r = new runner(script_id, runner_type.bs)
                    {
                        Package = pkg.Package,
                    };
                }
            }

            if (r != null)
            {
                lock (mt_lock)
                {
                    if (!runners.ContainsKey(script_id))
                    {
                        runners.Add(script_id, r);
                    }
                }
                return r;
            }
            else
            {
                return null;
            }
        }


        /// <summary>
        /// todo: update ? or just keep like this?
        /// </summary>
        internal class runner
        {
            public int ScriptId;
            public runner_type Type;

            // option 1
            public BlastPackageData Package;

#if !NOT_USING_UNITY
            // option 2
            public FunctionPointer<BSExecuteDelegate> HPCJobBSD;
            public NativeArray<float> HPCVariables;
#endif

            // option 3
            // c# script (windows) 

            public runner(int script_id, runner_type type)
            {
                ScriptId = script_id;
                Type = type;
            }
        }

#endregion

#region Bytecode Packages 
        static Dictionary<int, BlastScriptPackage> _bytecode_packages = new Dictionary<int, BlastScriptPackage>();
        static public int BytecodePackageCount => _bytecode_packages == null ? 0 : _bytecode_packages.Count;

        /// <summary>
        /// Using this list is not thread safe 
        /// </summary>
        static public Dictionary<int, BlastScriptPackage> BytecodePackages
        {
            get
            {
                if (_bytecode_packages == null)
                {
                    lock (mt_lock)
                    {
                        if (_bytecode_packages == null)
                        {
                            _bytecode_packages = new Dictionary<int, BlastScriptPackage>();
                        }
                    }
                }
                return _bytecode_packages;
            }
        }


        /// <summary>
        /// add a package to the internal packages repo
        /// </summary>
        /// <param name="pkg">the package to add</param>
        /// <returns>the nr of packages in the repo after adding this one or 0 on failure</returns>
        static public int AddPackage(int script_id, BlastScriptPackage pkg)
        {
            if (pkg != null)
            {
                lock (mt_lock)
                {
                    if (_bytecode_packages == null)
                    {
                        _bytecode_packages = new Dictionary<int, BlastScriptPackage>();
                    }
                    if (_bytecode_packages.ContainsKey(script_id))
                    {
                        _bytecode_packages[script_id] = pkg;
                    }
                    else
                    {
                        _bytecode_packages.Add(script_id, pkg);
                    }
                    return _bytecode_packages.Count;
                }
            }
            return 0;
        }

        static public BlastScriptPackage GetPackage(int script_id)
        {
            if (script_id <= 0) return null;
            lock (mt_lock)
            {
                if (_bytecode_packages != null)
                {
                    BlastScriptPackage pkg;
                    if (_bytecode_packages.TryGetValue(script_id, out pkg))
                    {
                        return pkg;
                    }
                }
            }
            return null;
        }

#endregion

#region Runtime Registration/Compilation of scripts 

        static public int RegisterAndCompile(Blast blast, BlastScript script)
        {
            // register script 
            int id = BlastScriptRegistry.Register(script);
            if (id <= 0) return 0;

            // compile into package 
            BlastScriptPackage pkg = BlastCompiler.CompilePackage(blast, script);
            if (pkg == null) return 0;

            // store package 
            Blast.AddPackage(id, pkg);

            return id;
        }

        public int RegisterAndCompile(string code, string name = null, int id = 0)
        {
            return RegisterAndCompile(this, code, name, id); 
        }

        static public int RegisterAndCompile(Blast blast, string code, string name = null, int id = 0)
        {
            id = BlastScriptRegistry.Register(code, name, id);
            if (id <= 0) return 0;

            BlastScript script = BlastScriptRegistry.Get(id);
            if (script == null) return 0;

            BlastScriptPackage pkg = BlastCompiler.CompilePackage(blast, script);
            if (pkg == null) return 0;

            Blast.AddPackage(id, pkg);
            return id;
        }

#if !NOT_USING_UNITY
        static public int RegisterAndCompile(Blast blast, string resource_filename_without_extension, int id = 0)
        {
            id = BlastScriptRegistry.RegisterScriptResource(resource_filename_without_extension, resource_filename_without_extension, id);
            if (id <= 0) return 0;

            BlastScript script = BlastScriptRegistry.Get(id);
            if (script == null) return 0;

            BlastScriptPackage pkg = BlastCompiler.CompilePackage(blast, script);
            if (pkg == null) return 0;

            Blast.AddPackage(id, pkg);
            return id;
        }
#endif

#endregion

    }


}

