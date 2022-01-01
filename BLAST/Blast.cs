using System;
using System.Collections.Generic;
using System.IO;

using NSS.Blast.Cache;
using NSS.Blast.Compiler;
using NSS.Blast.Interpretor;
using NSS.Blast.Register;

#if UNITY
using Unity.Assertions;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
#else

using Unity.Assertions;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;

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
        //unsafe public float4 constant_vectors;

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

        unsafe public void SetConstant(BlastEngineSystemConstant constant, float f)
        {
            constants[(int)constant] = f;
        }

        unsafe public void SetConstant(BlastEngineSystemConstant constant, int i)
        {
            // encode int as float
            float f = math.asfloat(i);
            constants[(int)constant] = f;
        }

        unsafe public float GetConstant(BlastEngineSystemConstant constant)
        {
            return constants[(int)constant];
        }

        unsafe public NonGenericFunctionPointer GetFunctionPointer(int id)
        {
            NonGenericFunctionPointer p; 
            if(TryGetFunctionPointer(id, out p))
            {
                return p;
            }
            return default(NonGenericFunctionPointer); 
        }

        unsafe public bool TryGetFunctionPointer(int id, out NonGenericFunctionPointer p)
        {
            for(int i = 0; i < functionpointer_count; i++)
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

        public enum exitcode : int
        {
            ok = 0,
            yield = 1,
            error = -1,
            error_engine_not_created = -100
        }

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
            if(mt_lock == null) mt_lock = new object(); 
            blast.allocator = allocator;
            blast.data = (BlastEngineData*)UnsafeUtility.Malloc(sizeof(BlastEngineData), 8, blast.allocator);
            blast.data->constants = (float*)UnsafeUtility.Malloc(4 * 256 * 2, 4, blast.allocator);
            blast.data->random = Random.CreateFromIndex(1);
            
            for (int b = 0; b < 256; b++)
            {
                blast.data->constants[b] = BlastValues.GetConstantValue((script_op)b);
            }

            blast.data->functionpointer_count = FunctionCalls.Count; 
            blast.data->functionpointers = (NonGenericFunctionPointer*)UnsafeUtility.Malloc(sizeof(NonGenericFunctionPointer) * FunctionCalls.Count, 8, blast.allocator); 

            for(int i = 0; i < FunctionCalls.Count; i++)
            { 
                ExternalFunctionCall call = FunctionCalls[i]; 
                blast.data->functionpointers[i] = call.FunctionPointer; 
            }

#if UNITY
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
                    UnsafeUtility.Free(data->constants, allocator);
                }
                if(data->functionpointers != null)
                {
                    UnsafeUtility.Free(data->functionpointers, allocator); 
                }
                UnsafeUtility.Free(data, allocator);
                data = null;
            }
            is_created = false;
        }
#endregion

#region Constants 

        /// <summary>
        /// synchronize 'constants' with platform, do this once every frame for each engine/thread 
        /// </summary>
        unsafe public exitcode SyncConstants()
        {
            if (!is_created) return exitcode.error_engine_not_created;

            BlastEngineData* p = (BlastEngineData*)data;

            p->SetConstant(BlastEngineSystemConstant.Time, UnityEngine.Time.time);
            p->SetConstant(BlastEngineSystemConstant.DeltaTime, UnityEngine.Time.deltaTime);
            p->SetConstant(BlastEngineSystemConstant.FixedDeltaTime, UnityEngine.Time.fixedDeltaTime);
            p->SetConstant(BlastEngineSystemConstant.FrameCount, UnityEngine.Time.frameCount);

            return exitcode.ok;
        }

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
                float f2 = BlastValues.GetConstantValue(value_op);
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

        /// <summary>
        /// get a constant float value represented by an operation 
        /// </summary>
        /// <param name="op"></param>
        /// <returns></returns>
        public float GetScriptOpFloatValue(script_op op)
        {
            return BlastValues.GetConstantValue(op);
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
#endregion

#region Functions 

        /// <summary>
        /// defined functions for script
        /// </summary>
        static public List<ScriptFunctionDefinition> Functions = new List<ScriptFunctionDefinition>()
        {
            new ScriptFunctionDefinition(0, "abs", 1, 1, script_op.abs, "a"),
            new ScriptFunctionDefinition(1, "min", 2, 63, script_op.min),
            new ScriptFunctionDefinition(2, "max", 2, 63, script_op.max),

            new ScriptFunctionDefinition(3, "mina", 2, 63, script_op.mina),
            new ScriptFunctionDefinition(4, "maxa", 2, 63, script_op.maxa),

            new ScriptFunctionDefinition(5, "select", 3, 3, script_op.select, "a", "b", "c"),

            new ScriptFunctionDefinition(6, "random", 0, 2, script_op.random),  // random value from 0 to 1 (inclusive), 0 to max, or min to max depending on parameter count 1
            new ScriptFunctionDefinition((int)ReservedScriptFunctionIds.Seed, "seed", 1, 1, script_op.seed),

            new ScriptFunctionDefinition(7, "sqrt", 1, 1, script_op.sqrt),
            new ScriptFunctionDefinition(8, "rsqrt", 1, 1, script_op.rsqrt),

            new ScriptFunctionDefinition(9, "lerp", 3, 3, script_op.lerp, "a", "b", "t"),
            new ScriptFunctionDefinition(10, "slerp", 3, 3, script_op.slerp, "a", "b", "t"),

            new ScriptFunctionDefinition(11, "any", 2, 63, script_op.any),
            new ScriptFunctionDefinition(12, "all", 2, 63, script_op.all),
            new ScriptFunctionDefinition(13, "adda", 2, 63, script_op.suba),
            new ScriptFunctionDefinition(14, "suba", 2, 63, script_op.adda),
            new ScriptFunctionDefinition(15, "diva", 2, 63, script_op.diva),
            new ScriptFunctionDefinition(16, "mula", 2, 63, script_op.mula),

            new ScriptFunctionDefinition(17, "ceil", 1, 1, script_op.ceil),
            new ScriptFunctionDefinition(18, "floor", 1, 1, script_op.floor),
            new ScriptFunctionDefinition(19, "frac", 1, 1, script_op.frac),

            new ScriptFunctionDefinition(21, "sin", 1, 1, script_op.sin),
            new ScriptFunctionDefinition(22, "cos", 1, 1, script_op.cos),
            new ScriptFunctionDefinition(23, "tan", 1, 1, script_op.tan),
            new ScriptFunctionDefinition(24, "atan", 1, 1, script_op.atan),
            new ScriptFunctionDefinition(25, "cosh", 1, 1, script_op.cosh),
            new ScriptFunctionDefinition(26, "sinh", 1, 1, script_op.sinh),

            new ScriptFunctionDefinition(28, "degrees", 1, 1, script_op.degrees),
            new ScriptFunctionDefinition(29, "rad", 1, 1, script_op.radians),

            new ScriptFunctionDefinition(30, "saturate", 1, 1, script_op.saturate, "a"),
            new ScriptFunctionDefinition(31, "clamp", 3, 3, script_op.clamp, "a", "min", "max"),

            new ScriptFunctionDefinition(32, "normalize", 1, 1, script_op.normalize, "a"),

            new ScriptFunctionDefinition(33, "pow", 2, 2, script_op.pow, "a", "power"),

            new ScriptFunctionDefinition(34, "distance", 2, 2, script_op.distance, "a", "b"),
            new ScriptFunctionDefinition(35, "distancesq", 2, 2, script_op.distancesq, "a", "b"),
            new ScriptFunctionDefinition(36, "length", 2, 2, script_op.length, "a", "b"),
            new ScriptFunctionDefinition(37, "lengthsq", 2, 2, script_op.lengthsq, "a", "b"),

            new ScriptFunctionDefinition(38, "cross", 2, 2, extended_script_op.cross, null, "a", "b"),
            new ScriptFunctionDefinition(39, "dot", 2, 2, script_op.dot, "a", "b"),

            new ScriptFunctionDefinition(40, "exp", 2, 2, script_op.exp, "a", "b"),
            new ScriptFunctionDefinition(41, "log2", 1, 1, script_op.log2, "a", "b"),

            new ScriptFunctionDefinition(42, "log10", 1, 1, extended_script_op.log10, null, "a"),
            new ScriptFunctionDefinition(43, "log", 1, 1, extended_script_op.logn, null, "a"),
            new ScriptFunctionDefinition(44, "exp10", 2, 2, extended_script_op.exp10, null, "a", "b"),

            new ScriptFunctionDefinition(45, "return", 0, 0, script_op.ret),

            new ScriptFunctionDefinition((int)ReservedScriptFunctionIds.Push, "push", 1, 4, script_op.push, "n", "a"),
            new ScriptFunctionDefinition((int)ReservedScriptFunctionIds.PushFunction, "pushf", 1, 1, script_op.pushf, "n", "a"),
            new ScriptFunctionDefinition((int)ReservedScriptFunctionIds.Pop, "pop", 0, 0, script_op.pop, "n"),
            new ScriptFunctionDefinition((int)ReservedScriptFunctionIds.Pop2, "pop2", 0, 0, script_op.pop2, "n"),
            new ScriptFunctionDefinition((int)ReservedScriptFunctionIds.Pop3, "pop3", 0, 0, script_op.pop3, "n"),
            new ScriptFunctionDefinition((int)ReservedScriptFunctionIds.Pop4, "pop4", 0, 0, script_op.pop4, "n"),
            new ScriptFunctionDefinition((int)ReservedScriptFunctionIds.Peek, "peek", 0, 1, script_op.peek, "n"),
            new ScriptFunctionDefinition((int)ReservedScriptFunctionIds.Yield, "yield", 0, 1, script_op.yield, "n"),
            new ScriptFunctionDefinition((int)ReservedScriptFunctionIds.Input, "input", 3, 3, script_op.nop, "id", "offset", "length"),
            new ScriptFunctionDefinition((int)ReservedScriptFunctionIds.Output, "output", 3, 3, script_op.nop, "id", "offset", "length")
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

        internal ScriptFunctionDefinition GetFunctionById(ReservedScriptFunctionIds function_id)
        {
            return GetFunctionById((int)function_id); 
        }

        internal ScriptFunctionDefinition GetFunctionById(int function_id)
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
            foreach(var f in Functions)
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
            foreach(ExternalFunctionCall call in FunctionCalls)
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
                        UnityEngine.Debug.LogError("failed to compile script: " + script); 
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
                UnityEngine.Debug.LogError("failed to compile script: " + script);
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
                            foreach(var j in Reflection.BlastReflect.FindHPCJobs())
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
                if(HPCJobs.TryGetValue(script_id, out job))
                {
                    return job;
                }
            }
            return null;
        }
#endregion

#region Execute Scripts (UNITY)
#if UNITY
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
                    UnityEngine.Debug.LogError($"runtype not supported: {run.Type}");
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

            lock(mt_lock)
            {
                if(runners == null)
                {
                    runners = new Dictionary<int, runner>();
                }
                if(runners != null)
                {
                    if (runners.TryGetValue(script_id, out r)) return r;  
                }
            }

            // to create a runner iterate fastest method first
#if UNITY
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

#if UNITY
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

#endregion

    }


}

