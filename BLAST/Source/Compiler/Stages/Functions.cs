//##########################################################################################################
// Copyright © 2022 Rob Lemmens | NijnStein Software <rob.lemmens.s31@gmail.com> All Rights Reserved  ^__^\#
// Unauthorized copying of this file, via any medium is strictly prohibited                           (oo)\#
// Proprietary and confidential                                                                       (__) #
//##########################################################################################################
using System;
using System.Collections.Generic;
using System.Text;

#if STANDALONE_VSBUILD
using NSS.Blast.Standalone;
using UnityEngine.Assertions;
#else
    using UnityEngine;
    using UnityEngine.Assertions;
#endif

using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using NSS.Blast.Compiler;

namespace NSS.Blast
{

    /// <summary>
    /// flags for extra information on script functions
    /// </summary>
    [Flags]
    public enum BlastScriptFunctionFlag : byte
    {
        /// <summary>
        /// function targets a blast reserved function (seed, push, peek) 
        /// </summary>
        Reserved = 1,

        /// <summary>
        /// function uses a native function pointer for execution
        /// </summary>
        NativeFunction = 2,

        /// <summary>
        /// function is an inlined temporary object that only exists during compilation
        /// </summary>
        Inlined = 4,
    }


    /// <summary>
    /// IDs reserved for functions used in blast, these get registered to blast with a name (that doesnt need to be equal) 
    /// </summary>
    public enum ReservedBlastScriptFunctionIds : int
    {
        /// <summary>
        /// input mapping function - reserved for internal use
        /// </summary>
        Input = 1,

        /// <summary>
        /// output mapping function - reserved for internal use
        /// </summary>
        Output = 2,

        /// <summary>
        /// Yield execution 
        /// </summary>
        Yield = 3,

        /// <summary>
        /// Pop something from the stack 
        /// </summary>
        Pop = 4,

        /// <summary>
        /// Peek stack top 
        /// </summary>
        Peek = 5,

        /// <summary>
        /// Seed the random number generator 
        /// </summary>
        Seed = 6,

        /// <summary>
        /// Push something to the stack 
        /// </summary>
        Push = 7,

        /// <summary>
        /// Push a function's return value to the stack 
        /// </summary>
        PushFunction = 8,

        /// <summary>
        /// Push the result from executing a compound onto the stack 
        /// </summary>
        PushCompound = 9,

        /// <summary>
        /// Push a vector directly onto the stack 
        /// </summary>
        PushVector = 10,

        /// <summary>
        /// Output debuginformation to the debug stream about given parameter 
        /// </summary>
        Debug = 11,

        /// <summary>
        /// Output debuginformation to the debug stream
        /// </summary>
        DebugStack = 12, 

        /// <summary>
        /// all other functions start indexing from this offset
        /// </summary>
        Offset = 32
    }


    /// <summary>
    /// burst compatible function description 
    /// </summary>
    [BurstCompatible]
    public struct BlastScriptFunction
    {
        /// <summary>
        /// a native function pointer (1st member, makes sure its aligned if this struct is)
        /// </summary>
        [NativeDisableUnsafePtrRestriction]
        public IntPtr NativeFunctionPointer;

        /// <summary>
        /// function Id, doubles as index into function tables
        /// </summary>
        public int FunctionId;

        /// <summary>
        /// char array to match to in lowercase, only ASCII
        /// - length == Blast.MaximumFunctionNameLength
        /// </summary>
        unsafe public fixed char Match[Blast.MaximumFunctionNameLength];  

        /// <summary>
        /// minimal number of parameters 
        /// </summary>
        public byte MinParameterCount;

        /// <summary>
        /// maximal number of parameters, 0 for none, max = 31 
        /// </summary>
        public byte MaxParameterCount;

        /// <summary>
        /// vectorsize of returnvalue, 0 if no value is returned
        /// </summary>
        public byte ReturnsVectorSize;

        /// <summary>
        /// vectorsize of accepted values in parameters, set to 0 to perform no check
        /// - TODO, V2/3/4?? could make this more strict
        /// </summary>
        public byte AcceptsVectorSize;

        /// <summary>
        /// Flags: 
        /// - reserved: function targets a reserved function
        /// </summary>
        public BlastScriptFunctionFlag Flags;

        /// <summary>
        /// Built-in functions directly compile into a script operation
        /// </summary>
        public blast_operation ScriptOp;

        /// <summary>
        /// Built-in functions directly compile into a script operation
        /// </summary>
        public extended_blast_operation ExtendedScriptOp;


        #region Deduced property getters
        /// <summary>
        /// true if the function targets a reserved function name 
        /// </summary>
        public bool IsReserved
        {
            get
            {
                return (Flags & BlastScriptFunctionFlag.Reserved) == BlastScriptFunctionFlag.Reserved;
            }
        }

        /// <summary>
        /// True if the functioncall is an external functionpointer
        /// </summary>
        public bool IsExternalCall => NativeFunctionPointer != IntPtr.Zero
            &&
            ExtendedScriptOp == extended_blast_operation.call
            &&
            ScriptOp == blast_operation.ex_op
            &&
            (Flags & BlastScriptFunctionFlag.NativeFunction) == BlastScriptFunctionFlag.NativeFunction;


        /// <summary>
        /// true if function encodes a pop function
        /// </summary>
        public bool IsPopVariant => (ReservedBlastScriptFunctionIds)FunctionId == ReservedBlastScriptFunctionIds.Pop;

        /// <summary>
        /// true function encodes a push operation 
        /// </summary>
        public bool IsPushVariant
        {
            get
            {
                switch ((ReservedBlastScriptFunctionIds)FunctionId)
                {
                    case ReservedBlastScriptFunctionIds.Push:
                    case ReservedBlastScriptFunctionIds.PushFunction:
                    case ReservedBlastScriptFunctionIds.PushVector:
                    case ReservedBlastScriptFunctionIds.PushCompound:
                        return true;

                    default: return false;
                }
            }
        }

        /// <summary>
        /// true if the function can have parameters
        /// </summary>
        public bool CanHaveParameters => MaxParameterCount > 0;


        /// <summary>
        /// returns true if the function accepts a variable sized list of paramaters 
        /// </summary>
        public bool HasVariableParameterCount => MaxParameterCount != MinParameterCount;

        /// <summary>
        /// returns if the function is NOT valid (checks id, assumes memory is initialized to zeros)
        /// </summary>
        public bool IsNotValid => FunctionId < 0;
        
        /// <summary>
        /// returns if the function is valid (checks id, assumes memory is initialized to zeros)
        /// </summary>
        public bool IsValid => FunctionId >= 0 && ScriptOp != blast_operation.nop;


        #endregion


        /// <summary>
        /// cast our functionpointer into the delegate used to call it
        /// </summary>
        /// <typeparam name="T">the delegate type</typeparam>
        /// <returns></returns>
        public FunctionPointer<T> Generic<T>()
        {
            return new FunctionPointer<T>(NativeFunctionPointer);
        }


        /// <summary>
        /// rebuilds the functionname from a native character array (expensive operation) 
        /// </summary>
        /// <returns>the string representing the functionname</returns>
        [BurstDiscard]
        public string GetFunctionName()
        {
            unsafe
            {
                StringBuilder sb = StringBuilderCache.Acquire(); 
                for(int i = 0; i < Blast.MaximumFunctionNameLength; i++)
                {
                    char ch = Match[i];
                    if (ch > (char)0)
                    {
                        sb.Append(Match[i]);
                    }
                    else break; 
                }
                return StringBuilderCache.GetStringAndRelease(ref sb); 
            }
        }
    }

    namespace Compiler
    {
        /// <summary>
        /// an inlined script function definition 
        /// </summary>
        public class BlastScriptInlineFunction
        {
            /// <summary>
            /// inlined function name
            /// </summary>
            public string Name;
            /// <summary>
            /// parameter count as defined in script 
            /// </summary>
            public int ParameterCount;
            /// <summary>
            /// root node of function 
            /// </summary>
            public node Node;

            internal BlastScriptFunction GenerateDummyScriptFunction()
            {
                BlastScriptFunction f = new BlastScriptFunction
                {
                    Flags = BlastScriptFunctionFlag.Inlined,
                    MinParameterCount = (byte)ParameterCount,
                    MaxParameterCount = (byte)ParameterCount,
                    AcceptsVectorSize = 0,
                    ReturnsVectorSize = 0,
                    ScriptOp = blast_operation.nop,
                    ExtendedScriptOp = extended_blast_operation.nop,
                    FunctionId = -1,
                    NativeFunctionPointer = IntPtr.Zero
                };
                unsafe
                {
                   // fixed (char* pch = f.Match)
                    {
                        CodeUtils.FillCharArray(f.Match, Name.ToLower().Trim(), Blast.MaximumFunctionNameLength);
                    }
                }

                return f; 
            }
        }

    }

    /// <summary>
    /// managed information on functions 
    /// </summary>
    public class BlastScriptFunctionInfo
    {
        /// <summary>
        /// the native function data contains id, information and function pointer 
        /// </summary>
        public BlastScriptFunction Function; 

        /// <summary>
        /// the identifier to match the function to = function name
        /// </summary>
        public string Match;

        /// <summary>
        /// the identifier used in CS code - DONT USE
        /// </summary>
        public string CSName; 

        /// <summary>
        /// [Optional] parameter names, these dont dictate min/max parametercount 
        /// </summary>
        public string[] Parameters;

        /// <summary>
        /// tostring override for better view in debugger
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"Function Definition: {Function.FunctionId} {Match}, parameter count: {Function.MinParameterCount} - {Function.MaxParameterCount}";
        }


        #region Deduced Properties 
        /// <summary>
        /// true if the function has a variable length parameter list
        /// </summary>
        public bool HasVariableParameterCount
        {
            get
            {
                return Function.MaxParameterCount != Function.MinParameterCount;
            }
        }
        #endregion 
    }


    /// <summary>
    /// For now, just a collection of function pointers that holds a native list with function information
    /// </summary>
    public abstract class BlastScriptAPI : IDisposable
    {
        /// <summary>
        /// name of the api
        /// </summary>
        public string Name { get; } = "core";

        /// <summary>
        /// native function definition info 
        /// </summary>
        unsafe public BlastScriptFunction* Functions;

        /// <summary>
        /// managed information, includes a copy of the native info
        /// </summary>
        public List<BlastScriptFunctionInfo> FunctionInfo;

        /// <summary>
        /// allocated to be used for any data comming from this api
        /// </summary>
        public Allocator Allocator; 


        /// <summary>
        /// true if native function pointer data is initialized/allocated
        /// </summary>
        public bool IsInitialized { get; protected set; }

        /// <summary>
        /// number of functions defined in this api
        /// </summary>
        public int FunctionCount { get { return FunctionInfo == null ? 0 : FunctionInfo.Count; } }

        #region Initialize / Destroy 
        /// <summary>
        /// Constructor for a new blastscriptapi instance, provides native allocator
        /// </summary>
        /// <param name="allocator"></param>
        public BlastScriptAPI(Allocator allocator)
        {
            FunctionInfo = new List<BlastScriptFunctionInfo>(); 
            Allocator = allocator;
            IsInitialized = false;

            // reserve room for reserved functions 
            for (int i = 0; i < (int)ReservedBlastScriptFunctionIds.Offset; i++)
            {
                FunctionInfo.Add(default);
            }
        }

        /// <summary>
        /// Initialize native function pointer stack 
        /// </summary>
        /// <returns></returns>
        public BlastScriptAPI Initialize()
        {
            Assert.IsFalse(IsInitialized, "BlastScriptAPI: already initialized"); 
            if (FunctionInfo.Count > 0)
            {
                unsafe
                {
                    int c = FunctionInfo.Count;

                    // initialize native array and copy function data into it making it accessible by the interpretor 
                    Functions = (BlastScriptFunction*)UnsafeUtils.Malloc(sizeof(BlastScriptFunction) * c, 8, Allocator);
                    for (int i = 1; i < c; i++)
                    {
                        if (FunctionInfo[i] != null)
                        {
                            Functions[i] = FunctionInfo[i].Function;
                        }
                        else
                        {
                            Functions[i] = default; 
                        }
                    }

                    IsInitialized = true;
                }
            }
            return this; 
        }


        /// <summary>
        /// dispose of any native memory that could be allocated for holding function pointer information 
        /// </summary>
        public void Dispose()
        {
            Destroy(); 
        }
        
        /// <summary>
        /// destroy native allocations, return to uninitialized state 
        /// </summary>
        protected void Destroy()
        {
            if (Allocator != Allocator.None)
                unsafe
                {
                    if (Functions != null)
                    {
                        UnsafeUtils.Free(Functions, Allocator);
                        Functions = null;
                    }
                    Allocator = Allocator.None;
                }
            IsInitialized = false; 
        }

        #endregion 

        #region RegisterFunction overloads

        /// <summary>
        /// Register a function with blast
        /// </summary>
        /// <param name="id">a reserved function id</param>
        /// <param name="name">functino name</param>
        /// <param name="min_param_count">minimal parameter count</param>
        /// <param name="max_param_count">maximum parameter count</param>
        /// <param name="accept_vector_size">accepted vector size, 0 for any</param>
        /// <param name="return_vector_size">returned verctor size, 0 for any</param>
        /// <param name="op">blast operation used to encode it</param>
        /// <returns>returns a unique (within the blast instance) function id</returns>
        public int RegisterFunction(ReservedBlastScriptFunctionIds id, string name, int min_param_count, int max_param_count, int accept_vector_size, int return_vector_size, blast_operation op)
        {
            Assert.IsTrue(!string.IsNullOrWhiteSpace(name));
            Assert.IsTrue(name.Length < Blast.MaximumFunctionNameLength, $"functionname '{name}' to long");

            if (IsInitialized) Destroy(); 

            BlastScriptFunctionInfo info = new BlastScriptFunctionInfo();
            info.Function.FunctionId = (int)id;

            // reserved functions always have the same ids 
            while (FunctionInfo.Count < info.Function.FunctionId + 1) FunctionInfo.Add(null); 
            FunctionInfo[info.Function.FunctionId] = info;

            unsafe
            {
                fixed (char* pch = info.Function.Match)
                    CodeUtils.FillCharArray(pch, name.ToLower().Trim(), Blast.MaximumFunctionNameLength);
            }

            info.Function.Flags = BlastScriptFunctionFlag.Reserved;
            
            info.Function.MinParameterCount = (byte)min_param_count;
            info.Function.MaxParameterCount = (byte)max_param_count;
            info.Function.AcceptsVectorSize = (byte)accept_vector_size; 
            info.Function.ReturnsVectorSize = (byte)return_vector_size;
            
            info.Function.ScriptOp = op; 
            info.Function.ExtendedScriptOp = extended_blast_operation.nop;
            info.Function.NativeFunctionPointer = IntPtr.Zero;

            return info.Function.FunctionId; 
        }

        /// <summary>
        /// Register a function with blast
        /// </summary>
        /// <param name="id">a reserved function id</param>
        /// <param name="name">functino name</param>
        /// <param name="min_param_count">minimal parameter count</param>
        /// <param name="max_param_count">maximum parameter count</param>
        /// <param name="accept_vector_size">minimal vector size, 0 for any</param>
        /// <param name="return_vector_size">max verctor size, 0 for any</param>
        /// <param name="op">extended blast operation used to encode it</param>
        /// <returns>returns a unique (within the blast instance) function id</returns>
        public int RegisterFunction(ReservedBlastScriptFunctionIds id, string name, int min_param_count, int max_param_count, int accept_vector_size, int return_vector_size, extended_blast_operation op)
        {
            Assert.IsTrue(!string.IsNullOrWhiteSpace(name));
            Assert.IsTrue(name.Length < Blast.MaximumFunctionNameLength, $"functionname '{name}' to long");

            if (IsInitialized) Destroy();

            BlastScriptFunctionInfo info = new BlastScriptFunctionInfo();
            info.Function.FunctionId = (int)id;

            // reserved functions always have the same ids 
            while (FunctionInfo.Count < info.Function.FunctionId + 1) FunctionInfo.Add(null);
            FunctionInfo[info.Function.FunctionId] = info;

            unsafe
            {
                fixed (char* pch = info.Function.Match)
                    CodeUtils.FillCharArray(pch, name.ToLower().Trim(), Blast.MaximumFunctionNameLength);
            }

            info.Function.Flags = BlastScriptFunctionFlag.Reserved;

            info.Function.MinParameterCount = (byte)min_param_count;
            info.Function.MaxParameterCount = (byte)max_param_count;
            info.Function.AcceptsVectorSize = (byte)accept_vector_size;
            info.Function.ReturnsVectorSize = (byte)return_vector_size;

            info.Function.ScriptOp = blast_operation.ex_op;
            info.Function.ExtendedScriptOp = op;
            info.Function.NativeFunctionPointer = IntPtr.Zero;

            return info.Function.FunctionId;
        }

        /// <summary>
        /// Register a function with blast
        /// </summary>
        /// <param name="nativefunction">a native function pointer</param>
        /// <param name="name">functino name</param>
        /// <param name="parameter_count">parameter count</param>
        /// <param name="accept_vector_size">minimal vector size, 0 for any</param>
        /// <param name="return_vector_size">max verctor size, 0 for any</param>
        /// <returns>returns a unique (within the blast instance) function id</returns>
        public int RegisterFunction(string name, IntPtr nativefunction, int parameter_count, int accept_vector_size, int return_vector_size)
        {
            Assert.IsTrue(!string.IsNullOrWhiteSpace(name));
            Assert.IsTrue(name.Length < Blast.MaximumFunctionNameLength, $"functionname '{name}' to long");

            if (IsInitialized) Destroy();

            BlastScriptFunctionInfo info = new BlastScriptFunctionInfo();
            info.Function.FunctionId = FunctionInfo.Count;
            FunctionInfo.Add(info);

            unsafe
            {
                fixed (char* pch = info.Function.Match)
                    CodeUtils.FillCharArray(pch, name.ToLower().Trim(), Blast.MaximumFunctionNameLength);
            }

            info.Function.MinParameterCount = (byte)parameter_count;
            info.Function.MaxParameterCount = (byte)parameter_count; 
            info.Function.AcceptsVectorSize = (byte)accept_vector_size;
            info.Function.ReturnsVectorSize = (byte)return_vector_size;

            info.Function.ScriptOp = blast_operation.ex_op;
            info.Function.ExtendedScriptOp = extended_blast_operation.call;
            info.Function.NativeFunctionPointer = nativefunction;

            return info.Function.FunctionId;
        }

        /// <summary>
        /// Register a function with blast
        /// </summary>
        /// <param name="name">functino name</param>
        /// <param name="min_param_count">minimal parameter count</param>
        /// <param name="max_param_count">maximum parameter count</param>
        /// <param name="accept_vector_size">minimal vector size, 0 for any</param>
        /// <param name="return_vector_size">max verctor size, 0 for any</param>
        /// <param name="op">blast operation used to encode it</param>
        /// <returns>returns a unique (within the blast instance) function id</returns>
        public int RegisterFunction(string name, int min_param_count, int max_param_count, int accept_vector_size, int return_vector_size, blast_operation op)
        {
            Assert.IsTrue(!string.IsNullOrWhiteSpace(name));
            Assert.IsTrue(name.Length < Blast.MaximumFunctionNameLength, $"functionname '{name}' to long");

            if (IsInitialized) Destroy();

            BlastScriptFunctionInfo info = new BlastScriptFunctionInfo();
            info.Function.FunctionId = FunctionInfo.Count;
            FunctionInfo.Add(info);

            unsafe
            {
                fixed (char* pch = info.Function.Match)
                    CodeUtils.FillCharArray(pch, name.ToLower().Trim(), Blast.MaximumFunctionNameLength);
            }

            info.Function.MinParameterCount = (byte)min_param_count;
            info.Function.MaxParameterCount = (byte)max_param_count;
            info.Function.AcceptsVectorSize = (byte)accept_vector_size;
            info.Function.ReturnsVectorSize = (byte)return_vector_size;

            info.Function.ScriptOp = op;
            info.Function.ExtendedScriptOp = extended_blast_operation.nop;
            info.Function.NativeFunctionPointer = IntPtr.Zero;

            return info.Function.FunctionId;

        }
        /// <summary>
        /// Register a function with blast
        /// </summary>
        /// <param name="name">functino name</param>
        /// <param name="min_param_count">minimal parameter count</param>
        /// <param name="max_param_count">maximum parameter count</param>
        /// <param name="accept_vector_size">minimal vector size, 0 for any</param>
        /// <param name="return_vector_size">max verctor size, 0 for any</param>
        /// <param name="op">blast operation used to encode it</param>
        /// <returns>returns a unique (within the blast instance) function id</returns>
        public int RegisterFunction(string name, int min_param_count, int max_param_count, int accept_vector_size, int return_vector_size, extended_blast_operation op)
        {
            Assert.IsTrue(!string.IsNullOrWhiteSpace(name));
            Assert.IsTrue(name.Length < Blast.MaximumFunctionNameLength, $"functionname '{name}' to long");

            if (IsInitialized) Destroy();

            BlastScriptFunctionInfo info = new BlastScriptFunctionInfo();
            info.Function.FunctionId = FunctionInfo.Count;
            FunctionInfo.Add(info);
            
            unsafe 
            { 
                fixed( char* pch = info.Function.Match) 
                    CodeUtils.FillCharArray(pch, name.ToLower().Trim(), Blast.MaximumFunctionNameLength);
            }

            info.Function.MinParameterCount = (byte)min_param_count;
            info.Function.MaxParameterCount = (byte)max_param_count;
            info.Function.AcceptsVectorSize = (byte)accept_vector_size;
            info.Function.ReturnsVectorSize = (byte)return_vector_size;

            info.Function.ScriptOp = blast_operation.ex_op;
            info.Function.ExtendedScriptOp = op;
            info.Function.NativeFunctionPointer = IntPtr.Zero;

            return info.Function.FunctionId;
        }

        #endregion

        #region Register External Functions


        //
        //  if register after api init:      functions add to end op api list, rebuilds api pointers 
        // 



        /// <summary>
        /// register an external function name within the blast script API, while no functionpointer is supplied 
        /// the name is registered for later use
        /// </summary>
        /// <remarks>
        /// 
        /// </remarks>
        /// <param name="name">the functionname, which is asserted to be unique</param>
        /// <param name="returns">the variable type returned</param>
        /// <param name="parameters">a list of parameter names, used for visualization only</param>
        /// <returns>an unique id for this function</returns>
        public int RegisterFunction(string name, BlastVariableDataType returns, string[] parameters)
        {
            return RegisterFunction(IntPtr.Zero, name, returns, parameters);
        }


        /// <summary>
        /// register functionpointer as an external call 
        /// </summary>
        /// <param name="fp">the function pointer, if null a reservation is still made for the id</param>
        /// <param name="name">the function name, may not contain tokens or reserved words</param>
        /// <param name="returns">the returned datatype</param>
        /// <param name="parameters">array of parameter names, used for visualizations only</param>
        /// <returns>an unique id for this function, negative errorcodes on error</returns>
        public int RegisterFunction(IntPtr fp, string name, BlastVariableDataType returns, string[] parameters)
        {
            if (IsInitialized) Destroy();

            Assert.IsFalse(string.IsNullOrWhiteSpace(name));
            Assert.IsNotNull(FunctionInfo); 

            if(FunctionExists(name))
            {
#if DEVELOPMENT_BUILD || TRACE
                // consider this an error in program flow
                Debug.LogError($"blast.scriptapi.register: a function with the name '{name}' already exists and thus cannot be registered again.");
#endif
                return (int)BlastError.error_scriptapi_function_already_exists; 
            }

            int id = FunctionInfo.Count;

            BlastScriptFunctionInfo finfo = new BlastScriptFunctionInfo();
            finfo.Function.FunctionId = id;
            finfo.Function.NativeFunctionPointer = fp;
            finfo.Function.Flags = BlastScriptFunctionFlag.NativeFunction;
            finfo.Function.ScriptOp = blast_operation.ex_op;
            finfo.Function.ExtendedScriptOp = extended_blast_operation.call;

            finfo.Function.AcceptsVectorSize = 1;
            finfo.Function.ReturnsVectorSize = 1;
            finfo.Function.MinParameterCount = (byte)parameters.Length;
            finfo.Function.MaxParameterCount = (byte)parameters.Length;
            unsafe
            {
                fixed (char* pch = finfo.Function.Match)
                    CodeUtils.FillCharArray(pch, name.ToLower().Trim(), Blast.MaximumFunctionNameLength);
            }


            finfo.Match = name;
            finfo.Parameters = parameters;

            FunctionInfo.Add(finfo);

            return finfo.Function.FunctionId;
        }
 
        /// <summary>
        /// Update the function pointer belonging to a registration 
        /// </summary>
        /// <param name="id">the function's id, it is asserted to exist</param>
        /// <param name="fp">the function pointer, may update to zero</param>
        /// <returns>success or error code</returns>
        public BlastError UpdateRegistration(int id, IntPtr fp)
        {
            Assert.IsNotNull(FunctionInfo); 

            BlastScriptFunctionInfo info = GetFunctionById(id);

            // program flow error if not already registred 
            if(info == null)
            {
#if DEVELOPMENT_BUILD || TRACE
                Debug.LogError($"blast.scriptapi.updateregistration: a function with the id '{id}' could not be found in the registry.");
#endif
                return BlastError.error_scriptapi_function_not_registered; 
            }

#if TRACE
            if(fp == IntPtr.Zero)
            {
                Debug.LogWarning($"blast.scriptapi.updateregistration: a function with the id '{id}' is updated to a null function pointer, is this intended?");
            }
#endif 

            // update function pointer 
            info.Function.NativeFunctionPointer = fp; 

            if(IsInitialized)
            {
                unsafe
                {
                    if (id >= 32 || id < FunctionCount) // doesnt hurt to be sure
                    {
                        Functions[id] = info.Function;
                    }
                }
            }

            // return success 
            return BlastError.success; 
        }


        #endregion

        #region Function Information 

        /// <summary>
        /// lookup a function definition that is directly linked to an interpretor operation 
        /// </summary>
        /// <param name="op">the operation that should translate to a function used during interpretation</param>
        /// <returns>a function definition</returns>
        unsafe public BlastScriptFunctionInfo GetFunctionByOpCode(blast_operation op)
        {
            for (int i = 0; i < FunctionInfo.Count; i++)
            {
                if (FunctionInfo[i].Function.ScriptOp == op)
                {
                    return FunctionInfo[i];
                }
            }
            return null;
        }

        /// <summary>
        /// check if a function exists by its name 
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public bool FunctionExists(string name)
        {
            for (int i = 0; i < FunctionInfo.Count; i++)
                if (string.Compare(name, FunctionInfo[i].Match, true) == 0)
                {
                    return true; 
                }

            return false; 
        }


        /// <summary>
        /// lookup a function definition that is directly linked to an interpretor operation 
        /// </summary>
        /// <param name="name">the name of the function to lookup</param>
        /// <returns>a function definition</returns>
        public BlastScriptFunctionInfo GetFunctionByName(string name)
        {
            for (int i = 0; i < FunctionInfo.Count; i++)
                if (string.Compare(name, FunctionInfo[i].Match, true) == 0)
                {
                    return FunctionInfo[i];
                }

            // look in user defined functions 
            return null;
        }


        /// <summary>
        /// lookup a function definition that is directly linked to an interpretor operation 
        /// </summary>
        /// <param name="function_id">the reserved id of the function to lookup</param>
        /// <returns>a function definition</returns>
        public BlastScriptFunctionInfo GetFunctionById(ReservedBlastScriptFunctionIds function_id)
        {
            return FunctionInfo[(int)function_id];
        }


        /// <summary>
        /// lookup a function definition that is directly linked to an interpretor operation 
        /// </summary>
        /// <param name="function_id">the unique id of the function to lookup</param>
        /// <returns>a function definition</returns>
        public BlastScriptFunctionInfo GetFunctionById(int function_id)
        {
            if (function_id < FunctionInfo.Count)
            {
                return FunctionInfo[function_id];
            }
            else
            {
                return null;
            }
        }


        /// <summary>
        /// Directly get a function using the id as an index 
        /// </summary>
        /// <param name="id">function id/index</param>
        /// <returns>the script function object</returns>
        public BlastScriptFunction GetFunctionDataById(int id)
        {
            unsafe
            {
#if DEVELOPMENT_BUILD || TRACE
                Assert.IsTrue(Functions != null && id < FunctionInfo.Count); // in release,a the ref to functioninfo is removed, after that this would burstcompile, if only assertions would be correctly handled in burst... 
#endif
                return Functions[id]; 
            }
        }

        /// <summary>
        /// try to get a function by id, in release it just gets it, in debug it checks  
        /// </summary>
        /// <param name="id">function id/index</param>
        /// <param name="function">function</param>
        /// <returns>true if found</returns>
        public bool TryGetFunctionCallById(int id, out BlastScriptFunction function)
        {
            unsafe {
#if DEVELOPMENT_BUILD || TRACE
                if (Functions != null && id >= 0 && id < FunctionInfo.Count && Functions[id].FunctionId == id)
                {
                    function = Functions[id]; 
                    return true;
                }
                else
                {
                    function = default;
                    return false;
                }
#else
                // release mode is unforbidding 
                function = Functions[id];
                return true; 
#endif 
            }
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

            BlastScriptFunctionInfo info = GetFunctionByOpCode(op);
            if (info == null)
            {
                return false;
            }

            return info.HasVariableParameterCount;
        }



        #endregion 
    }



    /// <summary>
    /// Implements Core-API functionality 
    /// </summary>
    public class CoreAPI : BlastScriptAPI
    {
        /// <summary>
        /// Extend BlastScriptAPI (core just is a shell) 
        /// </summary>
        /// <param name="allocator"></param>
        public CoreAPI(Allocator allocator) : base(allocator)
        {               
            RegisterCoreAPI();
        }

        /// <summary>
        /// register all base functions
        /// </summary>
        public virtual BlastScriptAPI RegisterCoreAPI()
        {
            RegisterFunction(ReservedBlastScriptFunctionIds.Push, "push", 1, 4, 0, 0, blast_operation.push);
            RegisterFunction(ReservedBlastScriptFunctionIds.PushFunction, "pushf", 1, 1, 0, 0, blast_operation.pushf);
            RegisterFunction(ReservedBlastScriptFunctionIds.PushCompound, "pushc", 1, 1, 0, 0, blast_operation.pushc);
            RegisterFunction(ReservedBlastScriptFunctionIds.PushVector, "pushv", 1, 1, 0, 0, blast_operation.pushv);
            RegisterFunction(ReservedBlastScriptFunctionIds.Pop, "pop", 0, 0, 0, 0, blast_operation.pop);
            RegisterFunction(ReservedBlastScriptFunctionIds.Peek, "peek", 0, 1, 0, 0, blast_operation.peek);
            RegisterFunction(ReservedBlastScriptFunctionIds.Yield, "yield", 0, 1, 0, 0, blast_operation.yield);
            RegisterFunction(ReservedBlastScriptFunctionIds.Input, "input", 3, 3, 0, 0, blast_operation.nop);
            RegisterFunction(ReservedBlastScriptFunctionIds.Output, "output", 3, 3, 0, 0, blast_operation.nop);
            RegisterFunction(ReservedBlastScriptFunctionIds.Seed, "seed", 1, 1, 0, 0, blast_operation.seed);
            RegisterFunction(ReservedBlastScriptFunctionIds.Debug, "debug", 1, 1, 0, 0, extended_blast_operation.debug);
            RegisterFunction(ReservedBlastScriptFunctionIds.DebugStack, "debugstack", 0, 0, 0, 0, extended_blast_operation.debugstack);

            RegisterFunction("return", 1, 1, 0, 0, blast_operation.ret);

            RegisterFunction("abs", 1, 1, 0, 0, blast_operation.abs);
            RegisterFunction("min", 2, 63, 0, 0, blast_operation.min);
            RegisterFunction("max", 2, 63, 0, 0, blast_operation.max);
            RegisterFunction("mina", 1, 63, 0, 1, blast_operation.mina);
            RegisterFunction("maxa", 1, 63, 0, 1, blast_operation.maxa);
            RegisterFunction("any", 2, 63, 0, 0, blast_operation.any);
            RegisterFunction("all", 2, 63, 0, 0, blast_operation.all);
            RegisterFunction("adda", 2, 63, 0, 0, blast_operation.adda);
            RegisterFunction("suba", 2, 63, 0, 0, blast_operation.suba);
            RegisterFunction("diva", 2, 63, 0, 0, blast_operation.diva);
            RegisterFunction("mula", 2, 63, 0, 0, blast_operation.mula);
            RegisterFunction("fma", 3, 3, 0, 0, blast_operation.fma);
            RegisterFunction("fmod", 2, 2, 0, 0, blast_operation.fmod);
            RegisterFunction("trunc", 1, 63, 0, 0, blast_operation.trunc);
            RegisterFunction("csum", 1, 63, 0, 0, blast_operation.csum);

            RegisterFunction("select", 3, 3, 0, 0, blast_operation.select);
            RegisterFunction("random", 0, 2, 0, 0, blast_operation.random);

            RegisterFunction("idx", 1, 1, 0, 1, blast_operation.index_x);
            RegisterFunction("idy", 1, 1, 0, 1, blast_operation.index_y);
            RegisterFunction("idz", 1, 1, 0, 1, blast_operation.index_z);
            RegisterFunction("idw", 1, 1, 0, 1, blast_operation.index_w);
            
            RegisterFunction("sin", 1, 1, 0, 0, extended_blast_operation.sin);
            RegisterFunction("cos", 1, 1, 0, 0, extended_blast_operation.cos);
            RegisterFunction("tan", 1, 1, 0, 0, extended_blast_operation.tan);
            RegisterFunction("atan", 1, 1, 0, 0, extended_blast_operation.atan);
            RegisterFunction("atan2", 1, 1, 0, 0, extended_blast_operation.atan2);
            RegisterFunction("cosh", 1, 1, 0, 0, extended_blast_operation.cosh);
            RegisterFunction("sinh", 1, 1, 0, 0, extended_blast_operation.sinh);
            RegisterFunction("degrees", 1, 1, 0, 0, extended_blast_operation.degrees);
            RegisterFunction("rad", 1, 1, 0, 0, extended_blast_operation.radians);

            RegisterFunction("sqrt", 1, 1, 0, 0, extended_blast_operation.sqrt);
            RegisterFunction("rsqrt", 1, 1, 0, 0, extended_blast_operation.rsqrt);
            RegisterFunction("pow", 2, 2, 0, 0, extended_blast_operation.pow);
            RegisterFunction("normalize", 1, 1, 0, 0, extended_blast_operation.normalize);
            RegisterFunction("saturate", 1, 1, 0, 0, extended_blast_operation.saturate);
            RegisterFunction("clamp", 3, 3, 0, 0, extended_blast_operation.clamp);
            RegisterFunction("log2", 1, 1, 0, 0, extended_blast_operation.log2);
            RegisterFunction("log10", 1, 1, 0, 0, extended_blast_operation.log10);
            RegisterFunction("log", 1, 1, 0, 0, extended_blast_operation.logn);
            RegisterFunction("exp", 1, 1, 0, 0, extended_blast_operation.exp);
            RegisterFunction("exp10", 1, 1, 0, 0, extended_blast_operation.exp10);
            RegisterFunction("cross", 2, 2, 3, 3, extended_blast_operation.cross);
            RegisterFunction("dot", 2, 2, 0, 1, extended_blast_operation.dot);

            RegisterFunction("ceil", 1, 1, 0, 0, extended_blast_operation.ceil);
            RegisterFunction("floor", 1, 1, 0, 0, extended_blast_operation.floor);
            RegisterFunction("frac", 1, 1, 0, 0, extended_blast_operation.frac);
            RegisterFunction("lerp", 3, 3, 0, 0, extended_blast_operation.lerp);
            RegisterFunction("slerp", 3, 3, 4, 4, extended_blast_operation.slerp);
            RegisterFunction("nlerp", 3, 3, 4, 4, extended_blast_operation.nlerp);

            RegisterFunction("unlerp", 3, 3, 0, 0, extended_blast_operation.unlerp);
            RegisterFunction("remap", 5, 5, 0, 0, extended_blast_operation.remap);
            RegisterFunction("ceillog2", 1, 1, 0, 0, extended_blast_operation.ceillog2);
            RegisterFunction("floorlog2", 1, 1, 0, 0, extended_blast_operation.floorlog2);
            RegisterFunction("ceilpow2", 1, 1, 0, 0, extended_blast_operation.ceilpow2);


            return this;
        }
    }
}