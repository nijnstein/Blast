using System;
using System.Collections.Generic;
using System.IO;

using NSS.Blast.Standalone;
using NSS.Blast.Cache;
using NSS.Blast.Compiler;
using NSS.Blast.Register;

using Unity.Assertions;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using System.Text;
using Unity.Burst;

#if !NOT_USING_UNITY
using Unity.Entities;
using Unity.Jobs;
#endif

using Random = Unity.Mathematics.Random;


namespace NSS.Blast
{
    /// <summary>
    /// BLAST execution data
    /// - used during execution of scripts
    /// - shared by all threads
    /// </summary>
    [BurstCompatible]
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
    [BurstCompatible]
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
                blast.data->constants[b] = Blast.GetConstantValue((script_op)b);
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
            blast._burst_once_job.interpretor = new BlastInterpretor();       // ??????? TODO why? or always use it? 
#endif
            blast.is_created = true;
            return blast;
        }

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

        #region Constants 

        [BurstDiscard]
        public IEnumerable<script_op> ValueOperations()
        {
            // we dont want this as static, but on a struct its also no good as field 
            // structs dont carry arround function tables so this seems ok... 
            yield return script_op.pi;
            yield return script_op.inv_pi;
            yield return script_op.value_0;
            yield return script_op.value_1;
            yield return script_op.value_2;
            yield return script_op.value_3;
            yield return script_op.value_4;
            yield return script_op.value_8;
            yield return script_op.value_10;
            yield return script_op.value_16;
            yield return script_op.value_24;
            yield return script_op.value_32;
            yield return script_op.value_64;
            yield return script_op.value_100;
            yield return script_op.value_128;
            yield return script_op.value_256;
            yield return script_op.value_512;
            yield return script_op.value_1000;
            yield return script_op.value_1024;
            yield return script_op.value_30;
            yield return script_op.value_45;
            yield return script_op.value_90;
            yield return script_op.value_180;
            yield return script_op.value_270;
            yield return script_op.value_360;
            yield return script_op.inv_value_2;
            yield return script_op.inv_value_3;
            yield return script_op.inv_value_4;
            yield return script_op.inv_value_8;
            yield return script_op.inv_value_10;
            yield return script_op.inv_value_16;
            yield return script_op.inv_value_24;
            yield return script_op.inv_value_32;
            yield return script_op.inv_value_64;
            yield return script_op.inv_value_100;
            yield return script_op.inv_value_128;
            yield return script_op.inv_value_256;
            yield return script_op.inv_value_512;
            yield return script_op.inv_value_1000;
            yield return script_op.inv_value_1024;
            yield return script_op.inv_value_30;
            yield return script_op.inv_value_45;
            yield return script_op.inv_value_90;
            yield return script_op.inv_value_180;
            yield return script_op.inv_value_270;
            yield return script_op.inv_value_360;
        }

        [BurstCompatible]
        public static float GetConstantValue(script_op op)
        {
            switch (op)
            {
                //case script_op.deltatime: return Time.deltaTime;
                //case script_op.time: return Time.time;
                //case script_op.framecount: return Time.frameCount;
                //case script_op.fixeddeltatime: return Time.fixedDeltaTime; 

                case script_op.pi: return math.PI;
                case script_op.inv_pi: return 1 / math.PI;
                case script_op.epsilon: return math.EPSILON;
                case script_op.infinity: return math.INFINITY;
                case script_op.negative_infinity: return -math.INFINITY;
                case script_op.nan: return math.NAN;
                case script_op.min_value: return math.FLT_MIN_NORMAL;
                case script_op.value_0: return 0f;
                case script_op.value_1: return 1f;
                case script_op.value_2: return 2f;
                case script_op.value_3: return 3f;
                case script_op.value_4: return 4f;
                case script_op.value_8: return 8f;
                case script_op.value_10: return 10f;
                case script_op.value_16: return 16f;
                case script_op.value_24: return 24f;
                case script_op.value_32: return 32f;
                case script_op.value_64: return 64f;
                case script_op.value_100: return 100f;
                case script_op.value_128: return 128f;
                case script_op.value_256: return 256f;
                case script_op.value_512: return 512f;
                case script_op.value_1000: return 1000f;
                case script_op.value_1024: return 1024f;
                case script_op.value_30: return 30f;
                case script_op.value_45: return 45f;
                case script_op.value_90: return 90f;
                case script_op.value_180: return 180f;
                case script_op.value_270: return 270f;
                case script_op.value_360: return 360f;
                case script_op.inv_value_2: return 1f / 2f;
                case script_op.inv_value_3: return 1f / 3f;
                case script_op.inv_value_4: return 1f / 4f;
                case script_op.inv_value_8: return 1f / 8f;
                case script_op.inv_value_10: return 1f / 10f;
                case script_op.inv_value_16: return 1f / 16f;
                case script_op.inv_value_24: return 1f / 24f;
                case script_op.inv_value_32: return 1f / 32f;
                case script_op.inv_value_64: return 1f / 64f;
                case script_op.inv_value_100: return 1f / 100f;
                case script_op.inv_value_128: return 1f / 128f;
                case script_op.inv_value_256: return 1f / 256f;
                case script_op.inv_value_512: return 1f / 512f;
                case script_op.inv_value_1000: return 1f / 1000f;
                case script_op.inv_value_1024: return 1f / 1024f;
                case script_op.inv_value_30: return 1f / 30f;
                case script_op.inv_value_45: return 1f / 45f;
                case script_op.inv_value_90: return 1f / 90f;
                case script_op.inv_value_180: return 1f / 180f;
                case script_op.inv_value_270: return 1f / 270f;
                case script_op.inv_value_360: return 1f / 360f;

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
        public script_op GetConstantValueOperation(string value, float constant_epsilon = 0.0001f)
        {
            // constant names should have been done earlier. check anyway
            script_op constant_op = IsSystemConstant(value);
            if (constant_op != script_op.nop)
            {
                return constant_op;
            }

            // it should be a float in system interpretation if it starts with a digit (dont call function otherwise)
            float f;
            if (float.IsNaN(f = node.AsFloat(value)))
            {
                return script_op.nan;
            }

            foreach (script_op value_op in ValueOperations())
            {
                float f2 = Blast.GetConstantValue(value_op);
                if (f2 > f - constant_epsilon && f2 < f + constant_epsilon)
                {
                    return value_op;
                }
            }

            return script_op.nop;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public script_op IsSystemConstant(string name)
        {
            switch (name.Trim().ToLower())
            {
                case "pi": return script_op.pi;
                //case "deltatime": return script_op.deltatime;
                case "epsilon": return script_op.epsilon;
                case "infinity": return script_op.infinity;
                case "nan": return script_op.nan;
                case "flt_min": return script_op.min_value;
                default: return script_op.nop;
            }
        }

        public float GetSystemConstantFloatValue(string name)
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
        /// tokens that can be used in script
        /// </summary>
        static public List<BlastScriptTokenDefinition> Tokens = new List<BlastScriptTokenDefinition>()
        {
            new BlastScriptTokenDefinition(BlastScriptToken.Add, '+'),
            new BlastScriptTokenDefinition(BlastScriptToken.Substract, '-'),
            new BlastScriptTokenDefinition(BlastScriptToken.Divide, '/'),
            new BlastScriptTokenDefinition(BlastScriptToken.Multiply, '*'),
            new BlastScriptTokenDefinition(BlastScriptToken.Equals, '='),
            new BlastScriptTokenDefinition(BlastScriptToken.Ternary, '?'),
            new BlastScriptTokenDefinition(BlastScriptToken.TernaryOption, ':'),
            new BlastScriptTokenDefinition(BlastScriptToken.SmallerThen, '<'),
            new BlastScriptTokenDefinition(BlastScriptToken.GreaterThen, '>'),
            new BlastScriptTokenDefinition(BlastScriptToken.And, '&'),
            new BlastScriptTokenDefinition(BlastScriptToken.Or, '|'),
            new BlastScriptTokenDefinition(BlastScriptToken.Xor, '^'),
            new BlastScriptTokenDefinition(BlastScriptToken.Not, '!'),
            new BlastScriptTokenDefinition(BlastScriptToken.OpenParenthesis, '('),
            new BlastScriptTokenDefinition(BlastScriptToken.CloseParenthesis, ')'),

            new BlastScriptTokenDefinition(BlastScriptToken.DotComma, ';'),
            new BlastScriptTokenDefinition(BlastScriptToken.Comma, ','),
                            
            // treat everything left as string literal for now
            new BlastScriptTokenDefinition(BlastScriptToken.Identifier, (char)0),
            new BlastScriptTokenDefinition(BlastScriptToken.Indexer, '.'),
            new BlastScriptTokenDefinition(BlastScriptToken.IndexOpen, '['),
            new BlastScriptTokenDefinition(BlastScriptToken.IndexClose, ']'),
        };


        /// <summary>
        /// check if the operation is a jump (jz, jnz, jump, jump_back)
        /// </summary>
        /// <param name="op">operation to check</param>
        /// <returns>true if a jump</returns>
        public static bool IsJumpOperation(script_op op)
        {
            return op == script_op.jump || op == script_op.jump_back || op == script_op.jz || op == script_op.jnz;
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
            new ScriptFunctionDefinition(0, "abs", 1, 1, 0, 0, script_op.abs),

            new ScriptFunctionDefinition(1, "min", 2, 63, 0, 0, script_op.min),
            new ScriptFunctionDefinition(2, "max", 2, 63, 0, 0, script_op.max),
            new ScriptFunctionDefinition(3, "mina", 1, 63, 1, 0, script_op.mina),
            new ScriptFunctionDefinition(4, "maxa", 1, 63, 1, 0, script_op.maxa),

            new ScriptFunctionDefinition(5, "select", 3, 3, 0, 0, script_op.select, "a", "b", "c"),

            new ScriptFunctionDefinition(6, "random", 0, 2, 0, 0, script_op.random),  // random value from 0 to 1 (inclusive), 0 to max, or min to max depending on parameter count 1
            new ScriptFunctionDefinition((int)ReservedScriptFunctionIds.Seed, "seed", 1, 1, 0, 0, script_op.seed),

            new ScriptFunctionDefinition(7, "sqrt", 1, 1, 0, 0, script_op.sqrt),
            new ScriptFunctionDefinition(8, "rsqrt", 1, 1, 0, 0, script_op.rsqrt),

            new ScriptFunctionDefinition(9, "lerp", 3, 3, 0, 0, script_op.lerp, "a", "b", "t"),
            new ScriptFunctionDefinition(10, "slerp", 3, 3, 0, 0,script_op.slerp, "a", "b", "t"),

            new ScriptFunctionDefinition(11, "any", 2, 63, 0, 0, script_op.any),
            new ScriptFunctionDefinition(12, "all", 2, 63, 0, 0, script_op.all),
            new ScriptFunctionDefinition(13, "adda", 2, 63, 0, 0, script_op.adda),
            new ScriptFunctionDefinition(14, "suba", 2, 63, 0, 0, script_op.suba),
            new ScriptFunctionDefinition(15, "diva", 2, 63, 0, 0, script_op.diva),
            new ScriptFunctionDefinition(16, "mula", 2, 63, 0, 0, script_op.mula),

            new ScriptFunctionDefinition(17, "ceil", 1, 1, 0, 0, script_op.ceil),
            new ScriptFunctionDefinition(18, "floor", 1, 1, 0, 0, script_op.floor),
            new ScriptFunctionDefinition(19, "frac", 1, 1, 0, 0, script_op.frac),

            new ScriptFunctionDefinition(21, "sin", 1, 1, 0, 0, script_op.sin),
            new ScriptFunctionDefinition(22, "cos", 1, 1, 0, 0, script_op.cos),
            new ScriptFunctionDefinition(23, "tan", 1, 1, 0, 0, script_op.tan),
            new ScriptFunctionDefinition(24, "atan", 1, 1, 0, 0, script_op.atan),
            new ScriptFunctionDefinition(25, "cosh", 1, 1, 0, 0, script_op.cosh),
            new ScriptFunctionDefinition(26, "sinh", 1, 1, 0, 0, script_op.sinh),

            new ScriptFunctionDefinition(28, "degrees", 1, 1, 0, 0, script_op.degrees),
            new ScriptFunctionDefinition(29, "rad", 1, 1, 0, 0, script_op.radians),

            new ScriptFunctionDefinition(46, "fma", 3, 3, 0, 0, script_op.fma, "m1", "m2", "a1"),

            new ScriptFunctionDefinition(33, "pow", 2, 2, 0, 0,script_op.pow, "a", "power"),

            // would be nice to have a parameter giving min and max vector size 
            new ScriptFunctionDefinition(32, "normalize", 1, 1, 0, 0, script_op.normalize, "a"),
            new ScriptFunctionDefinition(30, "saturate", 1, 1, 0, 0, script_op.saturate, "a"),


            new ScriptFunctionDefinition(31, "clamp", 3, 3, 0, 0, script_op.clamp, "a", "min", "max"),

            

            new ScriptFunctionDefinition(34, "distance", 2, 2,  0, 0,script_op.distance, "a", "b"),
            new ScriptFunctionDefinition(35, "distancesq", 2, 2,  0, 0,script_op.distancesq, "a", "b"),
            new ScriptFunctionDefinition(36, "length", 2, 2,  0, 0,script_op.length, "a", "b"),
            new ScriptFunctionDefinition(37, "lengthsq", 2, 2,  0, 0,script_op.lengthsq, "a", "b"),


            new ScriptFunctionDefinition(41, "log2", 1, 1,  0, 0, extended_script_op.log2, null, "a", "b"),
            new ScriptFunctionDefinition(42, "log10", 1, 1, 0, 0, extended_script_op.log10, null, "a"),
            new ScriptFunctionDefinition(43, "log", 1, 1, 0, 0, extended_script_op.logn, null, "a"),
            new ScriptFunctionDefinition(40, "exp", 1, 1,  0, 0, extended_script_op.exp, null, "exponent_of_e"),
            new ScriptFunctionDefinition(44, "exp10", 1, 1, 0, 0, extended_script_op.exp10, null, "a", "b"),
            new ScriptFunctionDefinition(38, "cross", 2, 2, 3, 3, extended_script_op.cross, null, "a", "b"),
            new ScriptFunctionDefinition(39, "dot", 2, 2, 1, 0, extended_script_op.dot, null, "a", "b"),

            new ScriptFunctionDefinition(45, "return", 0, 0,  0, 0,script_op.ret),





            new ScriptFunctionDefinition((int)ReservedScriptFunctionIds.Push, "push", 1, 4, 0, 0,script_op.push, "n", "a"),
            new ScriptFunctionDefinition((int)ReservedScriptFunctionIds.PushFunction, "pushf", 1, 1, 0, 0,script_op.pushf, "n", "a"),
            new ScriptFunctionDefinition((int)ReservedScriptFunctionIds.Pop, "pop", 0, 0, 0, 0,script_op.pop, "n"),
            new ScriptFunctionDefinition((int)ReservedScriptFunctionIds.Peek, "peek", 0, 1, 0, 0, script_op.peek, "n"),
            new ScriptFunctionDefinition((int)ReservedScriptFunctionIds.Yield, "yield", 0, 1, 0, 0,script_op.yield, "n"),
            new ScriptFunctionDefinition((int)ReservedScriptFunctionIds.Input, "input", 3, 3, 0, 0,script_op.nop, "id", "offset", "length"),
            new ScriptFunctionDefinition((int)ReservedScriptFunctionIds.Output, "output", 3, 3, 0, 0,script_op.nop, "id", "offset", "length"),

            new ScriptFunctionDefinition((int)ReservedScriptFunctionIds.Debug, "debug", 1, 1, 0, 0, extended_script_op.debug, null, "id"),
            new ScriptFunctionDefinition((int)ReservedScriptFunctionIds.Debug, "debugstack",0, 0, 0, 0, extended_script_op.debugstack)
        };

        public ScriptFunctionDefinition GetFunctionByScriptOp(script_op op)
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
        public bool IsVariableParamFunction(script_op op)
        {
            if (op == script_op.nop)
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
        static public int RegisterFunction(string name, SimulationFunctionHandlerReturnType returns, string[] parameters)
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
        static public int RegisterFunction(NonGenericFunctionPointer fp, string name, SimulationFunctionHandlerReturnType returns, string[] parameters)
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
                extended_script_op.call,
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
                        Standalone.Debug.LogError("failed to compile script: " + script); 
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
                Standalone.Debug.LogError("failed to compile script: " + script);
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
                script_op op = (script_op)bytes[i];

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
                    case script_op.nop: sb.Append("\n"); break;

                    case script_op.assign: sb.Append("assign "); break;

                    case script_op.add: sb.Append("+ "); break;
                    case script_op.substract: sb.Append("- "); break;
                    case script_op.multiply: sb.Append("* "); break;
                    case script_op.divide: sb.Append("/ "); break;
                    case script_op.and: sb.Append("& "); break;
                    case script_op.or: sb.Append("| "); break;
                    case script_op.not: sb.Append("! "); break;
                    case script_op.xor: sb.Append("^ "); break;
                    case script_op.smaller: sb.Append("< "); break;
                    case script_op.greater: sb.Append("> "); break;
                    case script_op.smaller_equals: sb.Append("<= "); break;
                    case script_op.greater_equals: sb.Append(">= "); break;
                    case script_op.equals: sb.Append("= "); break;
                    case script_op.not_equals: sb.Append("!= "); break;

                    case script_op.ret: sb.Append("return "); break;
                    case script_op.yield: sb.Append("yield "); break;

                    case script_op.begin: sb.Append("("); break;
                    case script_op.end: sb.Append(")"); break;

                    case script_op.jz: sb.Append("jz "); break;
                    case script_op.jnz: sb.Append("jnz "); break;

                    case script_op.jump: sb.Append("jump "); break;
                    case script_op.jump_back: sb.Append("jumpback "); break;

                    case script_op.push: sb.Append("push "); break;
                    case script_op.pop: sb.Append("pop "); break;
                    case script_op.pushv: sb.Append("pushv "); break;
                    case script_op.popv: sb.Append("popv "); break;
                    case script_op.peek: sb.Append("peek "); break;
                    case script_op.peekv: sb.Append("peekv "); break;
                    case script_op.pushf: sb.Append("pushf "); break;

                    case script_op.fma:
                        break;
                    case script_op.undefined1:
                        break;
                    case script_op.undefined2:
                        break;
                    case script_op.adda:
                        break;
                    case script_op.mula:
                        break;
                    case script_op.diva:
                        break;
                    case script_op.suba:
                        break;
                    case script_op.all:
                        break;
                    case script_op.any:
                        break;
                    case script_op.abs:
                        break;
                    case script_op.select:
                        break;
                    case script_op.random:
                        break;
                    case script_op.seed:
                        break;
                    case script_op.max:
                        break;
                    case script_op.min:
                        break;
                    case script_op.maxa:
                        break;
                    case script_op.mina:
                        break;
                    case script_op.lerp:
                        break;
                    case script_op.slerp:
                        break;
                    case script_op.saturate:
                        break;
                    case script_op.clamp:
                        break;
                    case script_op.normalize:
                        break;
                    case script_op.ceil:
                        break;
                    case script_op.floor:
                        break;
                    case script_op.frac:
                        break;
                    case script_op.sin:
                        break;
                    case script_op.cos:
                        break;
                    case script_op.tan:
                        break;
                    case script_op.atan:
                        break;
                    case script_op.cosh:
                        break;
                    case script_op.sinh:
                        break;
                    case script_op.distance:
                        break;
                    case script_op.distancesq:
                        break;
                    case script_op.length:
                        break;
                    case script_op.lengthsq:
                        break;
                    case script_op.degrees:
                        break;
                    case script_op.radians:
                        break;
                    case script_op.sqrt:
                        break;
                    case script_op.rsqrt:
                        break;
                    case script_op.pow:
                        break;


                    case script_op.value_0: sb.Append("0 "); break;
                    case script_op.value_1: sb.Append("1 "); break;
                    case script_op.value_2: sb.Append("2 "); break;
                    case script_op.value_3: sb.Append("3 "); break;
                    case script_op.value_4: sb.Append("4 "); break;
                    case script_op.value_8: sb.Append("8 "); break;
                    case script_op.value_10: sb.Append("10 "); break;
                    case script_op.value_16: sb.Append("16 "); break;
                    case script_op.value_24: sb.Append("24 "); break;
                    case script_op.value_30: sb.Append("30 "); break;
                    case script_op.value_32: sb.Append("32 "); break;
                    case script_op.value_45: sb.Append("45 "); break;
                    case script_op.value_64: sb.Append("64 "); break;
                    case script_op.value_90: sb.Append("90 "); break;
                    case script_op.value_100: sb.Append("100 "); break;
                    case script_op.value_128: sb.Append("128 "); break;
                    case script_op.value_180: sb.Append("180 "); break;
                    case script_op.value_256: sb.Append("256 "); break;
                    case script_op.value_270: sb.Append("270 "); break;
                    case script_op.value_360: sb.Append("360 "); break;
                    case script_op.value_512: sb.Append("512 "); break;
                    case script_op.value_1024: sb.Append("1024 "); break;
                    case script_op.inv_value_2: sb.Append((1f / 2f).ToString("0.000") + " "); break;
                    case script_op.inv_value_3: sb.Append((1f / 3f).ToString("0.000") + " "); break;
                    case script_op.inv_value_4: sb.Append((1f / 4f).ToString("0.000") + " "); break;
                    case script_op.inv_value_8: sb.Append((1f / 8f).ToString("0.000") + " "); break;
                    case script_op.inv_value_10: sb.Append((1f / 10f).ToString("0.000") + " "); break;
                    case script_op.inv_value_16: sb.Append((1f / 16f).ToString("0.000") + " "); break;
                    case script_op.inv_value_24: sb.Append((1f / 24f).ToString("0.000") + " "); break;
                    case script_op.inv_value_30: sb.Append((1f / 30f).ToString("0.000") + " "); break;
                    case script_op.inv_value_32: sb.Append((1f / 32f).ToString("0.000") + " "); break;
                    case script_op.inv_value_45: sb.Append((1f / 45f).ToString("0.000") + " "); break;
                    case script_op.inv_value_64: sb.Append((1f / 64f).ToString("0.000") + " "); break;
                    case script_op.inv_value_90: sb.Append((1f / 90f).ToString("0.000") + " "); break;
                    case script_op.inv_value_100: sb.Append((1f / 100f).ToString("0.000") + " "); break;
                    case script_op.inv_value_128: sb.Append((1f / 128f).ToString("0.000") + " "); break;
                    case script_op.inv_value_180: sb.Append((1f / 180f).ToString("0.000") + " "); break;
                    case script_op.inv_value_256: sb.Append((1f / 256f).ToString("0.000") + " "); break;
                    case script_op.inv_value_270: sb.Append((1f / 270f).ToString("0.000") + " "); break;
                    case script_op.inv_value_360: sb.Append((1f / 360f).ToString("0.000") + " "); break;
                    case script_op.inv_value_512: sb.Append((1f / 512f).ToString("0.000") + " "); break;
                    case script_op.inv_value_1024: sb.Append((1f / 1024f).ToString("0.000") + " "); break;

                    case script_op.ex_op:
                        i++;
                        extended_script_op ex = (extended_script_op)bytes[i];
                        switch (ex)
                        {
                            case extended_script_op.nop: break;
                            case extended_script_op.exp:
                            case extended_script_op.log2:
                            case extended_script_op.exp10:
                            case extended_script_op.log10:
                            case extended_script_op.logn:
                            case extended_script_op.cross:
                            case extended_script_op.dot:
                            case extended_script_op.ex:
                                sb.Append($"{ex} ");
                                break;

                            case extended_script_op.call:
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
                    if (run.Package != null)
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
                    Standalone.Debug.LogError($"runtype not supported: {run.Type}");
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
                        Package = pkg.BlastPackage,
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
            public BlastPackage* Package;

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
        static Dictionary<int, BlastScriptPackage> _bytecode_packages = null;
        public Dictionary<int, BlastScriptPackage> BytecodePackages
        {
            get
            {
                if (is_created)
                {
                    lock (mt_lock)
                    {
                        if (_bytecode_packages == null)
                        {
                            _bytecode_packages = new Dictionary<int, BlastScriptPackage>();
                        }
                        return _bytecode_packages;
                    }
                }
                return null;
            }
        }

        /// <summary>
        /// add a package to the internal packages repo
        /// </summary>
        /// <param name="pkg">the package to add</param>
        /// <returns>the nr of packages in the repo after adding this one or 0 on failure</returns>
        public int AddPackage(int script_id, BlastScriptPackage pkg)
        {
            if (is_created && pkg != null)
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

        public BlastScriptPackage GetPackage(int script_id)
        {
            if (!is_created || script_id <= 0) return null;
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
            blast.AddPackage(id, pkg);

            return id;
        }

        static public int RegisterAndCompile(Blast blast, string code, string name = null, int id = 0)
        {
            id = BlastScriptRegistry.Register(code, name, id);
            if (id <= 0) return 0;

            BlastScript script = BlastScriptRegistry.Get(id);
            if (script == null) return 0;

            BlastScriptPackage pkg = BlastCompiler.CompilePackage(blast, script);
            if (pkg == null) return 0;

            blast.AddPackage(id, pkg);
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

            blast.AddPackage(id, pkg);
            return id;
        }
#endif

        #endregion

    }


}

