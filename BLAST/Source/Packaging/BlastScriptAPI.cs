//############################################################################################################################
//                                                                                                                           #
//  ██████╗ ██╗      █████╗ ███████╗████████╗                           Copyright © 2022 Rob Lemmens | NijnStein Software    #
//  ██╔══██╗██║     ██╔══██╗██╔════╝╚══██╔══╝                                    <rob.lemmens.s31 gmail com>                 #
//  ██████╔╝██║     ███████║███████╗   ██║                                           All Rights Reserved                     #
//  ██╔══██╗██║     ██╔══██║╚════██║   ██║                                                                                   #
//  ██████╔╝███████╗██║  ██║███████║   ██║     V1.0.4e                                                                       #
//  ╚═════╝ ╚══════╝╚═╝  ╚═╝╚══════╝   ╚═╝                                                                                   #
//                                                                                                                           #
//############################################################################################################################
//                                                                                                                     (__)  #
//       Unauthorized copying of this file, via any medium is strictly prohibited proprietary and confidential         (oo)  #
//                                                                                                                     (__)  #
//############################################################################################################################
#pragma warning disable CS1591
#pragma warning disable CS0162
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
using NSS.Blast.Register;
using System.Reflection;
using System.Runtime.InteropServices;

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

        /// <summary>
        /// definition of the function omits pointers to engine and environment
        /// </summary>
        ShortDefine = 8
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
        /// Validate 2 values to be equal, same as Assert.IsTrue(a == b)  
        /// </summary>
        Validate = 13,

        /// <summary>
        /// all other functions start indexing from this offset
        /// </summary>
        Offset = 32
    }


    /// <summary>
    /// burst compatible function description 
    /// </summary>
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
        /// id of the delegate profile 
        /// </summary>
        public byte FunctionDelegateId;

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
       
        /// <summary>
        /// encoding scheme for function parameters
        /// </summary>
        public BlastParameterEncoding ParameterEncoding;

        /// <summary>
        /// fixed outputtype if other then none == 0
        /// </summary>
        public BlastVectorSizes FixedOutputType; 


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
        /// true if the function targets a reserved function name 
        /// </summary>
        public bool IsShortDefinition
        {
            get
            {
                return (Flags & BlastScriptFunctionFlag.ShortDefine) == BlastScriptFunctionFlag.ShortDefine;
            }
        }

        /// <summary>
        /// True if the functioncall is an external functionpointer
        /// </summary>
        public bool IsExternalCall => 
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

        public bool IsIndexFunction
        {
            get
            {
                switch(this.ScriptOp)
                {
                    case blast_operation.index_x:
                    case blast_operation.index_y:
                    case blast_operation.index_z:
                    case blast_operation.index_w:
                    case blast_operation.index_n: return true; 
                }

                return false;
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
                for (int i = 0; i < Blast.MaximumFunctionNameLength; i++)
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
                    ParameterEncoding = BlastParameterEncoding.Encode62,
                    FixedOutputType = BlastVectorSizes.none,
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
        /// managed delegate to any external function 
        /// </summary>
        public Delegate FunctionDelegate;

        /// <summary>
        /// points to the external function profile
        /// </summary>
        public byte FunctionDelegateId; 

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
    /// attribute used to mark functions to register as external functions in the blast script api
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    public class BlastFunctionAttribute : Attribute
    {
        /// <summary>
        /// name to use in blast api registration
        /// </summary>
        public string Name { get; set; } = null;

        /// <summary>
        /// default constructor
        /// </summary>
        public BlastFunctionAttribute()
        {
            Name = null; 
        }

        /// <summary>
        /// constructor to set the name 
        /// </summary>
        /// <param name="name"></param>
        public BlastFunctionAttribute(string name)
        {
            Name = name; 
        }
    }





    /// <summary>
    /// provides a link between a parameter profile and an external function 
    /// so we can get all information needed while in native code
    /// </summary>
    public struct ExternalFunctionProfile
    {
        public byte DelegateType;

        public byte ParameterCount;
        public BlastVectorSizes Parameter1;
        public BlastVectorSizes Parameter2;
        public BlastVectorSizes Parameter3;
        public BlastVectorSizes Parameter4;
        public BlastVectorSizes Parameter5;
        public BlastVectorSizes Parameter6;
        public BlastVectorSizes Parameter7;
        public BlastVectorSizes Parameter8;

        public bool ReturnsValue => ReturnParameter != BlastVectorSizes.none;
        public BlastVectorSizes ReturnParameter;

        public bool IsShortDefinition;
        public bool IsSSMD;

        public BlastVectorSizes ParameterType(int index)
        {
            switch(index)
            {
                case 0: return Parameter1;
                case 1: return Parameter2;
                case 2: return Parameter3;
                case 3: return Parameter4;
                case 4: return Parameter5;
                case 5: return Parameter6;
                case 6: return Parameter7;
                case 7: return Parameter8;
                default: return BlastVectorSizes.none; 
            }
        }
            
    }

    public static class BlastVectorSizesExtensions
    {
        public static string FullTypeName(this BlastVectorSizes vsize)
        {
            switch (vsize)
            {
                default:
                case BlastVectorSizes.none: return "void";
                case BlastVectorSizes.float1: return "float";
                case BlastVectorSizes.float2: return "float2";
                case BlastVectorSizes.float3: return "float3";
                case BlastVectorSizes.float4: return "float4";
                case BlastVectorSizes.bool32: return "Bool32";
                case BlastVectorSizes.id1: return "int";
                case BlastVectorSizes.id2: return "int2";
                case BlastVectorSizes.id3: return "int3";
                case BlastVectorSizes.id4: return "int4";
                case BlastVectorSizes.id64: return "long";
                case BlastVectorSizes.half2: return "half2";
                case BlastVectorSizes.half4: return "half4";
                case BlastVectorSizes.ptr: return "void*";
            }
        }
        public static string ShortTypeName(this BlastVectorSizes vsize)
        {
            switch (vsize)
            {
                default:
                case BlastVectorSizes.none: return "";
                case BlastVectorSizes.float1: return "f1";
                case BlastVectorSizes.float2: return "f2";
                case BlastVectorSizes.float3: return "f3";
                case BlastVectorSizes.float4: return "f4";
                case BlastVectorSizes.bool32: return "b32";
                case BlastVectorSizes.id1: return "i1";
                case BlastVectorSizes.id2: return "i2";
                case BlastVectorSizes.id3: return "i3";
                case BlastVectorSizes.id4: return "i4";
                case BlastVectorSizes.id64: return "l1";
                case BlastVectorSizes.half2: return "h2";
                case BlastVectorSizes.half4: return "h4";
                case BlastVectorSizes.ptr: return "";
            }
        }
        public static byte VectorSize(this BlastVectorSizes vsize)
        {
            switch (vsize)
            {
                default:
                case BlastVectorSizes.none: return 0;
                case BlastVectorSizes.float1: return 1;
                case BlastVectorSizes.float2: return 2;
                case BlastVectorSizes.float3: return 3;
                case BlastVectorSizes.float4: return 4;
                case BlastVectorSizes.bool32: return 1;
                case BlastVectorSizes.id1: return 1;
                case BlastVectorSizes.id2: return 2;
                case BlastVectorSizes.id3: return 3;
                case BlastVectorSizes.id4: return 4;
                case BlastVectorSizes.id64: return 1;
                case BlastVectorSizes.half2: return 2;
                case BlastVectorSizes.half4: return 4;
                case BlastVectorSizes.ptr: return 1;
            }
        }
    }



    /// <summary>
    /// For now, just a collection of function pointers that holds a native list with function information
    /// </summary>
    public abstract partial class BlastScriptAPI : IDisposable
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
        /// list of the possible profiles
        /// </summary>
        public List<ExternalFunctionProfile> ExternalFunctionProfiles;

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
        public BlastScriptAPI Initialize(bool enumerate_attributes = true)
        {
            Assert.IsFalse(IsInitialized, "BlastScriptAPI: already initialized");

            // enum all methods marked as blast external and register them with this api
            /* if (enumerate_attributes)
            {
                BlastError res = EnumerateFunctionAttributes();
                if(res != BlastError.success)
                {
                    Debug.LogError("BlastScriptAPI: could not register all functions marked as a blastfunction through attributes"); 
                }
            } */

            // create native information
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
        /// <param name="encoding">parameter encoding type, this is hardcoded into the interpretors implementation but should be configured for the compiler, defaults to 62</param>
        /// <returns>returns a unique (within the blast instance) function id</returns>
        internal int RegisterFunction(ReservedBlastScriptFunctionIds id, string name, int min_param_count, int max_param_count, int accept_vector_size, int return_vector_size, blast_operation op, BlastParameterEncoding encoding = BlastParameterEncoding.Encode62, BlastVectorSizes fixed_output_type = BlastVectorSizes.none)
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

            info.Function.ParameterEncoding = encoding;
            info.Function.FixedOutputType = fixed_output_type; 

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
        /// <param name="encoding">parameter encoding type, this is hardcoded into the interpretors implementation but should be configured for the compiler, defaults to 62</param>
        /// <returns>returns a unique (within the blast instance) function id</returns>
        internal int RegisterFunction(ReservedBlastScriptFunctionIds id, string name, int min_param_count, int max_param_count, int accept_vector_size, int return_vector_size, extended_blast_operation op, BlastParameterEncoding encoding = BlastParameterEncoding.Encode62, BlastVectorSizes fixed_output_type = BlastVectorSizes.none)
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

            info.Function.ParameterEncoding = encoding;
            info.Function.FixedOutputType = fixed_output_type;

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
        /// <param name="short_definition">the function is defined without enviroment and engine pointers </param>
        /// <param name="encoding">parameter encoding type, this is hardcoded into the interpretors implementation but should be configured for the compiler, defaults to 62</param>
        /// <returns>returns a unique (within the blast instance) function id</returns>
        internal int RegisterFunction(string name, EFDelegateType delegate_type, IntPtr nativefunction, int parameter_count, int accept_vector_size, int return_vector_size, bool short_definition, BlastParameterEncoding encoding = BlastParameterEncoding.Encode62, BlastVectorSizes fixed_output_type = BlastVectorSizes.none)
        {
            Assert.IsTrue(!string.IsNullOrWhiteSpace(name));
            Assert.IsTrue(name.Length < Blast.MaximumFunctionNameLength, $"functionname '{name}' to long");

            if (IsInitialized) Destroy();

            BlastScriptFunctionInfo info = new BlastScriptFunctionInfo();
            info.Function.FunctionId = FunctionInfo.Count;
            info.Function.FunctionDelegateId = (byte)delegate_type;
            info.FunctionDelegateId = (byte)delegate_type;
            FunctionInfo.Add(info);

            //ExternalFunctionProfile profile = ExternalFunctionProfiles[(byte)delegate_type];
            //Assert.IsNotNull(profile); 

            unsafe
            {
                fixed (char* pch = info.Function.Match)
                    CodeUtils.FillCharArray(pch, name.ToLower().Trim(), Blast.MaximumFunctionNameLength);
            }

            info.Function.MinParameterCount = (byte)parameter_count;
            info.Function.MaxParameterCount = (byte)parameter_count;
            info.Function.AcceptsVectorSize = (byte)accept_vector_size;
            info.Function.ReturnsVectorSize = (byte)return_vector_size;

            info.Function.ParameterEncoding = encoding;
            info.Function.FixedOutputType = fixed_output_type;

            info.Function.ScriptOp = blast_operation.ex_op;
            info.Function.ExtendedScriptOp = extended_blast_operation.call;
            info.Function.NativeFunctionPointer = nativefunction;
            info.Function.Flags = BlastScriptFunctionFlag.NativeFunction;

            if(short_definition)
            {
                info.Function.Flags |= BlastScriptFunctionFlag.ShortDefine;
            }

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
        /// <param name="encoding">parameter encoding type, this is hardcoded into the interpretors implementation but should be configured for the compiler, defaults to 62</param>
        /// <returns>returns a unique (within the blast instance) function id</returns>
        internal int RegisterFunction(string name, int min_param_count, int max_param_count, int accept_vector_size, int return_vector_size, blast_operation op, BlastParameterEncoding encoding = BlastParameterEncoding.Encode62, BlastVectorSizes fixed_output_type = BlastVectorSizes.none)
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

            info.Function.ParameterEncoding = encoding;
            info.Function.FixedOutputType = fixed_output_type;

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
        /// <param name="encoding">parameter encoding type, this is hardcoded into the interpretors implementation but should be configured for the compiler, defaults to 62</param>
        /// <returns>returns a unique (within the blast instance) function id</returns>
        internal int RegisterFunction(string name, int min_param_count, int max_param_count, int accept_vector_size, int return_vector_size, extended_blast_operation op, BlastParameterEncoding encoding = BlastParameterEncoding.Encode62, BlastVectorSizes fixed_output_type = BlastVectorSizes.none)
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

            info.Function.ParameterEncoding = encoding;
            info.Function.FixedOutputType = fixed_output_type;

            info.Function.ScriptOp = blast_operation.ex_op;
            info.Function.ExtendedScriptOp = op;
            info.Function.NativeFunctionPointer = IntPtr.Zero;

            return info.Function.FunctionId;
        }

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
        internal int RegisterFunction(EFDelegateType delegate_type, string name, BlastVariableDataType returns, string[] parameters)
        {
            return RegisterFunction(delegate_type, IntPtr.Zero, name, returns, parameters);
        }


        /// <summary>
        /// register functionpointer as an external call 
        /// </summary>
        /// <param name="fp">the function pointer, if null a reservation is still made for the id</param>
        /// <param name="name">the function name, may not contain tokens or reserved words</param>
        /// <param name="returns">the returned datatype</param>
        /// <param name="parameters">array of parameter names, used for visualizations only</param>
        /// <returns>an unique id for this function, negative errorcodes on error</returns>
        internal int RegisterFunction(EFDelegateType delegate_type, IntPtr fp, string name, BlastVariableDataType returns, string[] parameters)
        {
            if (IsInitialized) Destroy();

            Assert.IsFalse(string.IsNullOrWhiteSpace(name));
            Assert.IsNotNull(FunctionInfo);

            BlastScriptFunctionInfo existing = GetFunctionByName(name); 
            if (existing != null)
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
            finfo.Function.FunctionDelegateId = (byte)delegate_type;
            finfo.FunctionDelegateId = (byte)delegate_type;
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
            unsafe
            {
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

        #region Attribute Enumeration (reflection)

        /// <summary>
        /// enumerate and register all static methods marked with the [BlastFunctionAttribute] as external functions
        /// </summary>
        /*
        protected BlastError EnumerateFunctionAttributes()
        {
            bool has_error = false; 
            List<MethodInfo> methods = BlastReflect.FindBlastFunctionAttributes();
            
            foreach(MethodInfo method in methods)
            {
                ParameterInfo[] parameters = method.GetParameters();
                int parameter_count = parameters != null ? parameters.Length : 0;

                if(method.ReturnParameter == null || method.ReturnParameter.ParameterType != typeof(float))
                {
                    Debug.LogError($"blast.scriptapi.enumeratefunctionattributes: cannot add function {method.Name}, it has an unsupported return type. only float is supported");
                    has_error = true; 
                    continue; 
                }

                // fast out on simlest case 
                string name = method.GetCustomAttribute<BlastFunctionAttribute>().Name;
                name = string.IsNullOrWhiteSpace(name) ? method.Name : name;

                if (parameter_count == 0)
                {
                    // simplest case
                    Register((BlastDelegate_f0_s)method.CreateDelegate(typeof(BlastDelegate_f0_s)), name);
                    continue; 
                }


                bool is_short = parameter_count == 0;
                if (!is_short) is_short = parameters[0].ParameterType != typeof(IntPtr);

                // all parameters must map to a float or intptr 
                foreach (ParameterInfo info in parameters)
                {
                    Type matchto = typeof(float);
                    if (info.Position < 3 && !is_short)
                    {
                        matchto = typeof(IntPtr);
                    }
                    if (info.ParameterType != matchto)
                    {
                        Debug.LogError($"blast.scriptapi.enumeratefunctionattributes: cannot add function {method.Name}, parameter '{info.Name}' not of the correct type, only floats or intptrs are supported at select positions");
                        has_error = true;
                        continue;
                    }
                }

                // Register the method;'s delegate with blast 
                int result = -1; 
                if (is_short)
                {
                    switch(parameter_count)
                    {
                        case 1: result = Register((BlastDelegate_f1_s)method.CreateDelegate(typeof(BlastDelegate_f1_s)), name); break;
                        case 2: result = Register((BlastDelegate_f11_s)method.CreateDelegate(typeof(BlastDelegate_f11_s)), name); break;
                        default:
                            Debug.LogError($"blast.scriptapi.enumeratefunctionattributes: cannot add function {method.Name}, function template not yet implemented");
                            break; 
                    }
                }
                else
                {
                    switch(parameter_count - 3)
                    {
                        case 1: result = Register((BlastDelegate_f1)method.CreateDelegate(typeof(BlastDelegate_f1)), name); break;
                        case 2: result = Register((BlastDelegate_f11)method.CreateDelegate(typeof(BlastDelegate_f11)), name); break;
                        case 3: result = Register((BlastDelegate_f111)method.CreateDelegate(typeof(BlastDelegate_f111)), name); break;
                        case 4: result = Register((BlastDelegate_f1111)method.CreateDelegate(typeof(BlastDelegate_f1111)), name); break;

                        case 5: result = Register((BlastDelegate_f1111_1)method.CreateDelegate(typeof(BlastDelegate_f1111_1)), name); break;
                        case 6: result = Register((BlastDelegate_f1111_11)method.CreateDelegate(typeof(BlastDelegate_f1111_11)), name); break;
                        case 7: result = Register((BlastDelegate_f1111_111)method.CreateDelegate(typeof(BlastDelegate_f1111_111)), name); break;
                        case 8: result = Register((BlastDelegate_f1111_1111)method.CreateDelegate(typeof(BlastDelegate_f1111_1111)), name); break;

                        case 9: result =  Register((BlastDelegate_f1111_1111_1)method.CreateDelegate(typeof(BlastDelegate_f1111_1111_1)), name); break;
                        case 10: result = Register((BlastDelegate_f1111_1111_11)method.CreateDelegate(typeof(BlastDelegate_f1111_1111_11)), name); break;
                        case 11: result = Register((BlastDelegate_f1111_1111_111)method.CreateDelegate(typeof(BlastDelegate_f1111_1111_111)), name); break;
                        case 12: result = Register((BlastDelegate_f1111_1111_1111)method.CreateDelegate(typeof(BlastDelegate_f1111_1111_1111)), name); break;
                        case 13: result = Register((BlastDelegate_f1111_1111_1111_1)method.CreateDelegate(typeof(BlastDelegate_f1111_1111_1111_1)), name); break;
                        case 14: result = Register((BlastDelegate_f1111_1111_1111_11)method.CreateDelegate(typeof(BlastDelegate_f1111_1111_1111_11)), name); break;
                        case 15: result = Register((BlastDelegate_f1111_1111_1111_111)method.CreateDelegate(typeof(BlastDelegate_f1111_1111_1111_111)), name); break;
                        case 16: result = Register((BlastDelegate_f1111_1111_1111_1111)method.CreateDelegate(typeof(BlastDelegate_f1111_1111_1111_1111)), name); break;
                        default:
                            Debug.LogError($"blast.scriptapi.enumeratefunctionattributes: cannot add function {method.Name}, function template not yet implemented");
                            break;
                    }
                }
                if (result < 0) has_error = true;
            }

            if (has_error) return BlastError.error_enumerate_attributed_external_functions;
            else return BlastError.success;
        }
          */
        #endregion

        #region External Function Profiles 


        
        public bool ExternalProfileExists(in ExternalFunctionProfile profile)
        {
            if (ExternalFunctionProfiles == null) return false;
            foreach (ExternalFunctionProfile f in ExternalFunctionProfiles)
            {
                if (f.IsShortDefinition != profile.IsShortDefinition
                    ||
                    f.IsSSMD != profile.IsSSMD
                    ||
                    f.ParameterCount != profile.ParameterCount
                    ||
                    f.ReturnParameter != profile.ReturnParameter
                    ||
                    f.Parameter1 != profile.Parameter1
                    ||
                    f.Parameter2 != profile.Parameter2
                    ||
                    f.Parameter3 != profile.Parameter3
                    ||
                    f.Parameter4 != profile.Parameter4
                    ||
                    f.Parameter5 != profile.Parameter5
                    ||
                    f.Parameter6 != profile.Parameter6
                    ||
                    f.Parameter7 != profile.Parameter7
                    ||
                    f.Parameter8 != profile.Parameter8)
                {
                    continue;
                }
                else
                {
                    return true;
                }
            }
            return false; 
        }

        public ExternalFunctionProfile CreateExternalProfile(BlastVectorSizes returns, bool is_ssmd, bool is_short, params BlastVectorSizes[] parameters)
        {
            if (ExternalFunctionProfiles == null)
            {
                ExternalFunctionProfiles = new List<ExternalFunctionProfile>();
            }

            Assert.IsTrue(ExternalFunctionProfiles.Count < 255, "too many external function profiles defined");

            ExternalFunctionProfile p = default;
            p.DelegateType = (byte)ExternalFunctionProfiles.Count;
            p.ReturnParameter = returns;
            p.IsShortDefinition = is_short;
            p.IsSSMD = is_ssmd;

            int c = parameters != null ? parameters.Length : 0;

            p.ParameterCount = (byte)c;
            if (c > 0) p.Parameter1 = parameters[0];
            if (c > 1) p.Parameter2 = parameters[1];
            if (c > 2) p.Parameter3 = parameters[2];
            if (c > 3) p.Parameter4 = parameters[3];
            if (c > 4) p.Parameter5 = parameters[4];
            if (c > 5) p.Parameter6 = parameters[5];
            if (c > 6) p.Parameter7 = parameters[6];
            if (c > 7) p.Parameter8 = parameters[7];

            // check if the pattern already exists, ifso -> assert 
            Assert.IsFalse(ExternalProfileExists(p), "an external function profile with the same parameters already exists");

            // add new if unique
            ExternalFunctionProfiles.Add(p);
            return p;
        }

        #endregion 


        #region External Functions (public registrations) 


        /// <summary>
        /// register an external function with this api 
        /// </summary>
        /// <param name="function_delegate"></param>
        /// <param name="name"></param>
        /// <param name="parameter_count"></param>
        /// <param name="short_definition">short define: without engine and environment pointers</param>
        /// <param name="is_short"></param>
        /// <returns>a positive function id, or if negative a blasterror </returns>
        internal int RegisterFunction<T>(T function_delegate, string name, int parameter_count, bool short_definition = false, byte delegateid = 0) where T:class 
        {
            Assert.IsNotNull(function_delegate);

            if(string.IsNullOrWhiteSpace(name))
            {
                name = (function_delegate as Delegate).Method.Name; 
            }

            int function_id = RegisterFunction(name, (EFDelegateType) delegateid, IntPtr.Zero, parameter_count, 1, 1, short_definition);
      
            BlastError res = UpdateFunctionDelegate(function_id, delegateid, (function_delegate as Delegate));
            if(res != BlastError.success)
            {
#if DEVELOPMENT_BUILD || TRACE
                Debug.LogError($"blast.scriptapi.Register: function {name} not registered|updated");
#endif
                return (int)res; 
            }


#if !STANDALONE_VSBUILD

            IntPtr fp = BurstCompiler.CompileFunctionPointer(function_delegate).Value;  

            // IntPtr fp = BurstCompilerUtil<T>.CompileFunctionPointer(function_delegate);
            res = UpdateNativeFunctionPointer(function_id, fp); 
            if(res != BlastError.success)
            {
                Debug.LogError($"blast.scriptapi.Register: function {name}, native function pointer not registered|updated");
                return (int)res; 
            }
#endif


#if TRACE
            // show an overview of the functions available in the log 
            Debug.Log($"BlastScriptAPI: registered function {function_id} {name}");
#endif


            return function_id;
        }

        /// <summary>
        /// update an external functions delegate and functionpointer 
        /// </summary>
        /// <param name="function_id"></param>
        /// <param name="function_delegate"></param>
        /// <returns></returns>
        public BlastError UpdateRegistration<T>(int function_id, T function_delegate) where T: class
        {
            Assert.IsNotNull(function_delegate);
            Assert.IsTrue(function_id > 0);
            
            BlastError res = UpdateFunctionDelegate(function_id, 0, function_delegate as Delegate);
            if (res != BlastError.success) return res;

#if !STANDALONE_VSBUILD
            IntPtr fp = BurstCompilerUtil<T>.CompileFunctionPointer(function_delegate);
            res = UpdateNativeFunctionPointer(function_id, fp); 
            if (res != BlastError.success) return res;
#endif
            return BlastError.success;
        }


        /// <summary>
        /// update the managed delegate 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="function_delegate"></param>
        /// <returns></returns>
        internal BlastError UpdateFunctionDelegate(int id, byte delegate_id, Delegate function_delegate)
        {
            Assert.IsNotNull(FunctionInfo);

            BlastScriptFunctionInfo info = GetFunctionById(id);
            // program flow error if not already registred 
            if (info == null)
            {
#if DEVELOPMENT_BUILD || TRACE
                Debug.LogError($"blast.scriptapi.updateregistration: a function with the id '{id}' could not be found in the registry.");
#endif
                return BlastError.error_scriptapi_function_not_registered;
            }


#if DEVELOPMENT_BUILD || TRACE
            if (function_delegate == null)
            {
                Debug.LogWarning($"blast.scriptapi.updateregistration: a function with the id '{id}' is updated to a null function delegate, is this intended?");
            }
#endif 

            info.FunctionDelegate = function_delegate;
            info.FunctionDelegateId = delegate_id; 
            return BlastError.success; 
        }


        /// <summary>
        /// Update the function pointer belonging to a registration 
        /// </summary>
        /// <param name="id">the function's id, it is asserted to exist</param>
        /// <param name="fp">the function pointer, may update to zero</param>
        /// <returns>success or error code</returns>
        internal BlastError UpdateNativeFunctionPointer(int id, IntPtr fp)
        {
            Assert.IsNotNull(FunctionInfo);

            BlastScriptFunctionInfo info = GetFunctionById(id);

            // program flow error if not already registred 
            if (info == null)
            {
#if DEVELOPMENT_BUILD || TRACE
                Debug.LogError($"blast.scriptapi.updateregistration: a function with the id '{id}' could not be found in the registry.");
#endif
                return BlastError.error_scriptapi_function_not_registered;
            }

#if DEVELOPMENT_BUILD || TRACE
            if (fp == IntPtr.Zero)
            {
                Debug.LogWarning($"blast.scriptapi.updateregistration: a function with the id '{id}' is updated to a null function pointer, is this intended?");
            }
#endif 

            // update function pointer 
            info.Function.NativeFunctionPointer = fp;

            // update native data if initialized already
            if (IsInitialized)
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
            RegisterFunction(ReservedBlastScriptFunctionIds.Seed, "seed", 1, 1, 0, 0, extended_blast_operation.seed);
            RegisterFunction(ReservedBlastScriptFunctionIds.Debug, "debug", 1, 1, 0, 0, extended_blast_operation.debug);
            RegisterFunction(ReservedBlastScriptFunctionIds.DebugStack, "debugstack", 0, 0, 0, 0, extended_blast_operation.debugstack);
            RegisterFunction(ReservedBlastScriptFunctionIds.Validate, "validate", 2, 2, 0, 0, extended_blast_operation.validate); 

            RegisterFunction("return", 1, 1, 0, 0, blast_operation.ret);

            RegisterFunction("abs", 1, 1, 0, 0, blast_operation.abs);
            RegisterFunction("min", 2, 63, 0, 0, blast_operation.min);
            RegisterFunction("max", 2, 63, 0, 0, blast_operation.max);
            RegisterFunction("mina", 1, 63, 0, 1, blast_operation.mina);
            RegisterFunction("maxa", 1, 63, 0, 1, blast_operation.maxa);
            
            RegisterFunction("not", 1, 1, 0, 0, blast_operation.not); 
            RegisterFunction("any", 1, 15, 0, 0, blast_operation.any, BlastParameterEncoding.Encode44);
            RegisterFunction("all", 1, 15, 0, 0, blast_operation.all, BlastParameterEncoding.Encode44);

            RegisterFunction("adda", 2, 63, 0, 0, blast_operation.adda);
            RegisterFunction("suba", 2, 63, 0, 0, blast_operation.suba);
            RegisterFunction("diva", 2, 63, 0, 0, blast_operation.diva);
            RegisterFunction("mula", 2, 63, 0, 0, blast_operation.mula);
            RegisterFunction("fma", 3, 3, 0, 0, blast_operation.fma);
            RegisterFunction("fmod", 2, 2, 0, 0, extended_blast_operation.fmod);
            RegisterFunction("trunc", 1, 1, 0, 0, blast_operation.trunc);
            RegisterFunction("csum", 1, 63, 0, 1, blast_operation.csum);

            RegisterFunction("select", 3, 3, 0, 0, blast_operation.select);
            RegisterFunction("random", 0, 2, 0, 0, blast_operation.random);

            RegisterFunction("idx", 1, 1, 0, 1, blast_operation.index_x);
            RegisterFunction("idy", 1, 1, 0, 1, blast_operation.index_y);
            RegisterFunction("idz", 1, 1, 0, 1, blast_operation.index_z);
            RegisterFunction("idw", 1, 1, 0, 1, blast_operation.index_w);
            RegisterFunction("idxn", 2, 2, 0, 1, blast_operation.index_n);

            RegisterFunction("expand2", 1, 1, 1, 2, blast_operation.expand_v2);
            RegisterFunction("expand3", 1, 1, 1, 3, blast_operation.expand_v3);
            RegisterFunction("expand4", 1, 1, 1, 4, blast_operation.expand_v4);

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

            RegisterFunction("set_bits", 3, 3, 1, 0, extended_blast_operation.set_bits);
            RegisterFunction("set_bit", 3, 3, 1, 0, extended_blast_operation.set_bit);
            RegisterFunction("get_bits", 2, 2, 1, 1, extended_blast_operation.get_bits); // returns only 1 if all bits in mask are 1 (all)
            RegisterFunction("get_bit", 2, 2, 1, 1, extended_blast_operation.get_bit);

            RegisterFunction("count_bits",   1, 1, 1, 1, extended_blast_operation.count_bits);
            RegisterFunction("reverse_bits", 1, 1, 1, 0, extended_blast_operation.reverse_bits);

            RegisterFunction("shl", 2, 2, 1, 0, extended_blast_operation.shl);
            RegisterFunction("shr", 2, 2, 1, 0, extended_blast_operation.shr);
            RegisterFunction("rol", 2, 2, 1, 0, extended_blast_operation.rol);
            RegisterFunction("ror", 2, 2, 1, 0, extended_blast_operation.ror);

            RegisterFunction("lzcnt", 1, 1, 1, 1, extended_blast_operation.lzcnt);
            RegisterFunction("tzcnt", 1, 1, 1, 1, extended_blast_operation.tzcnt);

            RegisterFunction("zero", 1, 1, 0, 0, blast_operation.zero);
            RegisterFunction("clear_bits", 1, 1, 0, 0, blast_operation.zero);

            RegisterFunction("reinterpret_int32", 1, 1, 1, 0, extended_blast_operation.reinterpret_int32, BlastParameterEncoding.Encode62, BlastVectorSizes.id1);
            RegisterFunction("reinterpret_bool32", 1, 1, 1, 0, extended_blast_operation.reinterpret_bool32, BlastParameterEncoding.Encode62, BlastVectorSizes.bool32);
            RegisterFunction("reinterpret_float32", 1, 1, 1, 0, extended_blast_operation.reinterpret_float, BlastParameterEncoding.Encode62, BlastVectorSizes.float1);

            RegisterFunction("send", 1, 1, 0, 0, extended_blast_operation.send, BlastParameterEncoding.Encode44, BlastVectorSizes.none);
            RegisterFunction("size", 1, 1, 0, 1, blast_operation.size, BlastParameterEncoding.Encode62, BlastVectorSizes.id1);

            RegisterFunction("length", 1, 1, 0, 1, extended_blast_operation.length);
            RegisterFunction("lengthsq", 1, 1, 0, 1, extended_blast_operation.lengthsq);
            RegisterFunction("square", 1, 1, 0, 1, extended_blast_operation.square);
            RegisterFunction("distance", 2, 2, 0, 1, extended_blast_operation.distance);
            RegisterFunction("distancesq", 2, 2, 0, 1, extended_blast_operation.distancesq);

            RegisterFunction("reflect", 2, 2, 0, 0, extended_blast_operation.reflect);
            RegisterFunction("project", 2, 2, 0, 0, extended_blast_operation.project);

            RegisterFunction("up", 0, 0, 0, 3, extended_blast_operation.up);
            RegisterFunction("down", 0, 0, 0, 3, extended_blast_operation.down);
            RegisterFunction("forward", 0, 0, 0, 3, extended_blast_operation.forward);
            RegisterFunction("back", 0, 0, 0, 3, extended_blast_operation.back);
            RegisterFunction("left", 0, 0, 0, 3, extended_blast_operation.left);
            RegisterFunction("right", 0, 0, 0, 3, extended_blast_operation.right);

            RegisterFunction("eulerxyz", 1, 1, 3, 4, extended_blast_operation.EulerXYZ);
            RegisterFunction("eulerxzy", 1, 1, 3, 4, extended_blast_operation.EulerXZY);
            RegisterFunction("euleryxz", 1, 1, 3, 4, extended_blast_operation.EulerYXZ);
            RegisterFunction("euleryzx", 1, 1, 3, 4, extended_blast_operation.EulerYZX);
            RegisterFunction("eulerzxy", 1, 1, 3, 4, extended_blast_operation.EulerZXY);
            RegisterFunction("eulerzyx", 1, 1, 3, 4, extended_blast_operation.EulerZYX);
            RegisterFunction("euler", 2, 2, 3, 4, extended_blast_operation.Euler);

            RegisterFunction("quaternionxyz", 1, 1, 4, 3, extended_blast_operation.QuaternionXYZ);
            RegisterFunction("quaternionxzy", 1, 1, 4, 3, extended_blast_operation.QuaternionXZY);
            RegisterFunction("quaternionyxz", 1, 1, 4, 3, extended_blast_operation.QuaternionYXZ);
            RegisterFunction("quaternionyzx", 1, 1, 4, 3, extended_blast_operation.QuaternionYZX);
            RegisterFunction("quaternionzxy", 1, 1, 4, 3, extended_blast_operation.QuaternionZXY);
            RegisterFunction("quaternionzyx", 1, 1, 4, 3, extended_blast_operation.QuaternionZYX);
            RegisterFunction("quaternion", 2, 2, 4, 3, extended_blast_operation.Quaternion);

            RegisterFunction("lookrotation", 2, 2, 3, 4, extended_blast_operation.LookRotation);
            RegisterFunction("lookrotationsafe", 2, 2, 3, 4, extended_blast_operation.LookRotationSafe);

            RegisterFunction("rotate", 2, 2, 0, 3, extended_blast_operation.Rotate);
            RegisterFunction("angle", 2, 2, 0, 1, extended_blast_operation.Angle);
            RegisterFunction("mul", 2, 2, 0, 0, extended_blast_operation.Mul);

            RegisterFunction("magnitude", 1, 1, 0, 0, extended_blast_operation.Magnitude);
            RegisterFunction("sqrmagnitude", 1, 1, 0, 0, extended_blast_operation.SqrMagnitude);
            RegisterFunction("circlecenter", 4, 4, 3, 3, extended_blast_operation.CircleCenter);


            // full parameterset default external profiles 
            CreateExternalProfile(BlastVectorSizes.none,   false, false);
            CreateExternalProfile(BlastVectorSizes.float1, false, false);
            CreateExternalProfile(BlastVectorSizes.float2, false, false);
            CreateExternalProfile(BlastVectorSizes.float3, false, false);
            CreateExternalProfile(BlastVectorSizes.float4, false, false);
            CreateExternalProfile(BlastVectorSizes.id1,    false, false);
            CreateExternalProfile(BlastVectorSizes.id2,    false, false);
            CreateExternalProfile(BlastVectorSizes.id3,    false, false);
            CreateExternalProfile(BlastVectorSizes.id4,    false, false);
            CreateExternalProfile(BlastVectorSizes.bool32, false, false);

            CreateExternalProfile(BlastVectorSizes.float1, false, false, BlastVectorSizes.float1);
            CreateExternalProfile(BlastVectorSizes.float1, false, false, BlastVectorSizes.float1, BlastVectorSizes.float1);
            CreateExternalProfile(BlastVectorSizes.float1, false, false, BlastVectorSizes.float1, BlastVectorSizes.float1, BlastVectorSizes.float1);
            CreateExternalProfile(BlastVectorSizes.float1, false, false, BlastVectorSizes.float1, BlastVectorSizes.float1, BlastVectorSizes.float1, BlastVectorSizes.float1);
            CreateExternalProfile(BlastVectorSizes.float1, false, false, BlastVectorSizes.float1, BlastVectorSizes.float1, BlastVectorSizes.float1, BlastVectorSizes.float1, BlastVectorSizes.float1);
            CreateExternalProfile(BlastVectorSizes.float1, false, false, BlastVectorSizes.float1, BlastVectorSizes.float1, BlastVectorSizes.float1, BlastVectorSizes.float1, BlastVectorSizes.float1, BlastVectorSizes.float1);
            CreateExternalProfile(BlastVectorSizes.float1, false, false, BlastVectorSizes.float1, BlastVectorSizes.float1, BlastVectorSizes.float1, BlastVectorSizes.float1, BlastVectorSizes.float1, BlastVectorSizes.float1, BlastVectorSizes.float1);
            CreateExternalProfile(BlastVectorSizes.float1, false, false, BlastVectorSizes.float1, BlastVectorSizes.float1, BlastVectorSizes.float1, BlastVectorSizes.float1, BlastVectorSizes.float1, BlastVectorSizes.float1, BlastVectorSizes.float1, BlastVectorSizes.float1);
            CreateExternalProfile(BlastVectorSizes.float1, false, false, BlastVectorSizes.float2);
            CreateExternalProfile(BlastVectorSizes.float1, false, false, BlastVectorSizes.float2, BlastVectorSizes.float2);
            CreateExternalProfile(BlastVectorSizes.float1, false, false, BlastVectorSizes.float2, BlastVectorSizes.float2, BlastVectorSizes.float2);
            CreateExternalProfile(BlastVectorSizes.float1, false, false, BlastVectorSizes.float3);
            CreateExternalProfile(BlastVectorSizes.float1, false, false, BlastVectorSizes.float3, BlastVectorSizes.float3);
            CreateExternalProfile(BlastVectorSizes.float1, false, false, BlastVectorSizes.float3, BlastVectorSizes.float3, BlastVectorSizes.float3);
            CreateExternalProfile(BlastVectorSizes.float1, false, false, BlastVectorSizes.float4);
            CreateExternalProfile(BlastVectorSizes.float1, false, false, BlastVectorSizes.float4, BlastVectorSizes.float4);
            CreateExternalProfile(BlastVectorSizes.float1, false, false, BlastVectorSizes.float4, BlastVectorSizes.float4, BlastVectorSizes.float4);
            CreateExternalProfile(BlastVectorSizes.float2, false, false, BlastVectorSizes.float2);
            CreateExternalProfile(BlastVectorSizes.float2, false, false, BlastVectorSizes.float2, BlastVectorSizes.float2);
            CreateExternalProfile(BlastVectorSizes.float2, false, false, BlastVectorSizes.float2, BlastVectorSizes.float2, BlastVectorSizes.float2);
            CreateExternalProfile(BlastVectorSizes.float3, false, false, BlastVectorSizes.float3);
            CreateExternalProfile(BlastVectorSizes.float3, false, false, BlastVectorSizes.float3, BlastVectorSizes.float3);
            CreateExternalProfile(BlastVectorSizes.float3, false, false, BlastVectorSizes.float3, BlastVectorSizes.float3, BlastVectorSizes.float3);
            CreateExternalProfile(BlastVectorSizes.float4, false, false, BlastVectorSizes.float4);
            CreateExternalProfile(BlastVectorSizes.float4, false, false, BlastVectorSizes.float4, BlastVectorSizes.float4);
            CreateExternalProfile(BlastVectorSizes.float4, false, false, BlastVectorSizes.float4, BlastVectorSizes.float4, BlastVectorSizes.float4);
            CreateExternalProfile(BlastVectorSizes.none,   false, false, BlastVectorSizes.bool32);
            CreateExternalProfile(BlastVectorSizes.bool32, false, false, BlastVectorSizes.bool32);
            CreateExternalProfile(BlastVectorSizes.bool32, false, false, BlastVectorSizes.bool32, BlastVectorSizes.float4, BlastVectorSizes.float1);

            CreateExternalProfile(BlastVectorSizes.none,   false, false, BlastVectorSizes.id1);
            CreateExternalProfile(BlastVectorSizes.id1,    false, false, BlastVectorSizes.id1);
            CreateExternalProfile(BlastVectorSizes.id1,    false, false, BlastVectorSizes.id1, BlastVectorSizes.id1);
            CreateExternalProfile(BlastVectorSizes.id1,    false, false, BlastVectorSizes.id1, BlastVectorSizes.id1, BlastVectorSizes.id1);
            CreateExternalProfile(BlastVectorSizes.id1,    false, false, BlastVectorSizes.id1, BlastVectorSizes.float3, BlastVectorSizes.float3, BlastVectorSizes.float3);
            CreateExternalProfile(BlastVectorSizes.id1,    false, false, BlastVectorSizes.id1, BlastVectorSizes.float3, BlastVectorSizes.float4, BlastVectorSizes.float3);
            CreateExternalProfile(BlastVectorSizes.bool32, false, false, BlastVectorSizes.id1, BlastVectorSizes.float3, BlastVectorSizes.float3, BlastVectorSizes.float3);
            CreateExternalProfile(BlastVectorSizes.bool32, false, false, BlastVectorSizes.id1, BlastVectorSizes.float3, BlastVectorSizes.float4, BlastVectorSizes.float3);

            // short parameterset default external profiles 
            CreateExternalProfile(BlastVectorSizes.none,   false, true);
            CreateExternalProfile(BlastVectorSizes.float1, false, true);
            CreateExternalProfile(BlastVectorSizes.float2, false, true);
            CreateExternalProfile(BlastVectorSizes.float3, false, true);
            CreateExternalProfile(BlastVectorSizes.float4, false, true);
            CreateExternalProfile(BlastVectorSizes.bool32, false, true);
            CreateExternalProfile(BlastVectorSizes.float1, false, true, BlastVectorSizes.float1);
            CreateExternalProfile(BlastVectorSizes.float1, false, true, BlastVectorSizes.float1, BlastVectorSizes.float1);
            CreateExternalProfile(BlastVectorSizes.float1, false, true, BlastVectorSizes.float1, BlastVectorSizes.float1, BlastVectorSizes.float1);
            CreateExternalProfile(BlastVectorSizes.float1, false, true, BlastVectorSizes.float1, BlastVectorSizes.float1, BlastVectorSizes.float1, BlastVectorSizes.float1);
            CreateExternalProfile(BlastVectorSizes.float1, false, true, BlastVectorSizes.float1, BlastVectorSizes.float1, BlastVectorSizes.float1, BlastVectorSizes.float1, BlastVectorSizes.float1);
            CreateExternalProfile(BlastVectorSizes.float1, false, true, BlastVectorSizes.float1, BlastVectorSizes.float1, BlastVectorSizes.float1, BlastVectorSizes.float1, BlastVectorSizes.float1, BlastVectorSizes.float1);
            CreateExternalProfile(BlastVectorSizes.float1, false, true, BlastVectorSizes.float1, BlastVectorSizes.float1, BlastVectorSizes.float1, BlastVectorSizes.float1, BlastVectorSizes.float1, BlastVectorSizes.float1, BlastVectorSizes.float1);
            CreateExternalProfile(BlastVectorSizes.float1, false, true, BlastVectorSizes.float1, BlastVectorSizes.float1, BlastVectorSizes.float1, BlastVectorSizes.float1, BlastVectorSizes.float1, BlastVectorSizes.float1, BlastVectorSizes.float1, BlastVectorSizes.float1);
            CreateExternalProfile(BlastVectorSizes.float1, false, true, BlastVectorSizes.float2);
            CreateExternalProfile(BlastVectorSizes.float1, false, true, BlastVectorSizes.float2, BlastVectorSizes.float2);
            CreateExternalProfile(BlastVectorSizes.float1, false, true, BlastVectorSizes.float2, BlastVectorSizes.float2, BlastVectorSizes.float2);
            CreateExternalProfile(BlastVectorSizes.float1, false, true, BlastVectorSizes.float3);
            CreateExternalProfile(BlastVectorSizes.float1, false, true, BlastVectorSizes.float3, BlastVectorSizes.float3);
            CreateExternalProfile(BlastVectorSizes.float1, false, true, BlastVectorSizes.float3, BlastVectorSizes.float3, BlastVectorSizes.float3);
            CreateExternalProfile(BlastVectorSizes.float1, false, true, BlastVectorSizes.float4);
            CreateExternalProfile(BlastVectorSizes.float1, false, true, BlastVectorSizes.float4, BlastVectorSizes.float4);
            CreateExternalProfile(BlastVectorSizes.float1, false, true, BlastVectorSizes.float4, BlastVectorSizes.float4, BlastVectorSizes.float4);
            CreateExternalProfile(BlastVectorSizes.float2, false, true, BlastVectorSizes.float2);
            CreateExternalProfile(BlastVectorSizes.float2, false, true, BlastVectorSizes.float2, BlastVectorSizes.float2);
            CreateExternalProfile(BlastVectorSizes.float2, false, true, BlastVectorSizes.float2, BlastVectorSizes.float2, BlastVectorSizes.float2);
            CreateExternalProfile(BlastVectorSizes.float3, false, true, BlastVectorSizes.float3);
            CreateExternalProfile(BlastVectorSizes.float3, false, true, BlastVectorSizes.float3, BlastVectorSizes.float3);
            CreateExternalProfile(BlastVectorSizes.float3, false, true, BlastVectorSizes.float3, BlastVectorSizes.float3, BlastVectorSizes.float3);
            CreateExternalProfile(BlastVectorSizes.float4, false, true, BlastVectorSizes.float4);
            CreateExternalProfile(BlastVectorSizes.float4, false, true, BlastVectorSizes.float4, BlastVectorSizes.float4);
            CreateExternalProfile(BlastVectorSizes.float4, false, true, BlastVectorSizes.float4, BlastVectorSizes.float4, BlastVectorSizes.float4);
            CreateExternalProfile(BlastVectorSizes.none,   false, true, BlastVectorSizes.bool32);
            CreateExternalProfile(BlastVectorSizes.bool32, false, true, BlastVectorSizes.bool32);
            CreateExternalProfile(BlastVectorSizes.bool32, false, true, BlastVectorSizes.bool32, BlastVectorSizes.float4, BlastVectorSizes.float1);

            CreateExternalProfile(BlastVectorSizes.none,   false, true, BlastVectorSizes.id1);
            CreateExternalProfile(BlastVectorSizes.id1,    false, true, BlastVectorSizes.id1);
            CreateExternalProfile(BlastVectorSizes.id1,    false, true, BlastVectorSizes.id1, BlastVectorSizes.id1);
            CreateExternalProfile(BlastVectorSizes.id1,    false, true, BlastVectorSizes.id1, BlastVectorSizes.id1, BlastVectorSizes.id1);
            CreateExternalProfile(BlastVectorSizes.id1,    false, true, BlastVectorSizes.id1, BlastVectorSizes.float3, BlastVectorSizes.float3, BlastVectorSizes.float3);
            CreateExternalProfile(BlastVectorSizes.id1,    false, true, BlastVectorSizes.id1, BlastVectorSizes.float3, BlastVectorSizes.float4, BlastVectorSizes.float3);
            CreateExternalProfile(BlastVectorSizes.bool32, false, true, BlastVectorSizes.id1, BlastVectorSizes.float3, BlastVectorSizes.float3, BlastVectorSizes.float3);
            CreateExternalProfile(BlastVectorSizes.bool32, false, true, BlastVectorSizes.id1, BlastVectorSizes.float3, BlastVectorSizes.float4, BlastVectorSizes.float3);

            // default profiles for ssmd interpretor, vectorized parameters  
            CreateExternalProfile(BlastVectorSizes.none,   true, false);
            CreateExternalProfile(BlastVectorSizes.float1, true, false);
            CreateExternalProfile(BlastVectorSizes.float2, true, false);
            CreateExternalProfile(BlastVectorSizes.float3, true, false);
            CreateExternalProfile(BlastVectorSizes.float4, true, false);
            CreateExternalProfile(BlastVectorSizes.bool32, true, false);
            CreateExternalProfile(BlastVectorSizes.float1, true, false, BlastVectorSizes.float1);
            CreateExternalProfile(BlastVectorSizes.float1, true, false, BlastVectorSizes.float1, BlastVectorSizes.float1);
            CreateExternalProfile(BlastVectorSizes.float1, true, false, BlastVectorSizes.float1, BlastVectorSizes.float1, BlastVectorSizes.float1);
            CreateExternalProfile(BlastVectorSizes.float1, true, false, BlastVectorSizes.float1, BlastVectorSizes.float1, BlastVectorSizes.float1, BlastVectorSizes.float1);
            CreateExternalProfile(BlastVectorSizes.float1, true, false, BlastVectorSizes.float1, BlastVectorSizes.float1, BlastVectorSizes.float1, BlastVectorSizes.float1, BlastVectorSizes.float1);
            CreateExternalProfile(BlastVectorSizes.float1, true, false, BlastVectorSizes.float1, BlastVectorSizes.float1, BlastVectorSizes.float1, BlastVectorSizes.float1, BlastVectorSizes.float1, BlastVectorSizes.float1);
            CreateExternalProfile(BlastVectorSizes.float1, true, false, BlastVectorSizes.float1, BlastVectorSizes.float1, BlastVectorSizes.float1, BlastVectorSizes.float1, BlastVectorSizes.float1, BlastVectorSizes.float1, BlastVectorSizes.float1);
            CreateExternalProfile(BlastVectorSizes.float1, true, false, BlastVectorSizes.float1, BlastVectorSizes.float1, BlastVectorSizes.float1, BlastVectorSizes.float1, BlastVectorSizes.float1, BlastVectorSizes.float1, BlastVectorSizes.float1, BlastVectorSizes.float1);
            CreateExternalProfile(BlastVectorSizes.float1, true, false, BlastVectorSizes.float2);
            CreateExternalProfile(BlastVectorSizes.float1, true, false, BlastVectorSizes.float2, BlastVectorSizes.float2);
            CreateExternalProfile(BlastVectorSizes.float1, true, false, BlastVectorSizes.float2, BlastVectorSizes.float2, BlastVectorSizes.float2);
            CreateExternalProfile(BlastVectorSizes.float1, true, false, BlastVectorSizes.float3);
            CreateExternalProfile(BlastVectorSizes.float1, true, false, BlastVectorSizes.float3, BlastVectorSizes.float3);
            CreateExternalProfile(BlastVectorSizes.float1, true, false, BlastVectorSizes.float3, BlastVectorSizes.float3, BlastVectorSizes.float3);
            CreateExternalProfile(BlastVectorSizes.float1, true, false, BlastVectorSizes.float4);
            CreateExternalProfile(BlastVectorSizes.float1, true, false, BlastVectorSizes.float4, BlastVectorSizes.float4);
            CreateExternalProfile(BlastVectorSizes.float1, true, false, BlastVectorSizes.float4, BlastVectorSizes.float4, BlastVectorSizes.float4);
            CreateExternalProfile(BlastVectorSizes.float2, true, false, BlastVectorSizes.float2);
            CreateExternalProfile(BlastVectorSizes.float2, true, false, BlastVectorSizes.float2, BlastVectorSizes.float2);
            CreateExternalProfile(BlastVectorSizes.float2, true, false, BlastVectorSizes.float2, BlastVectorSizes.float2, BlastVectorSizes.float2);
            CreateExternalProfile(BlastVectorSizes.float3, true, false, BlastVectorSizes.float3);
            CreateExternalProfile(BlastVectorSizes.float3, true, false, BlastVectorSizes.float3, BlastVectorSizes.float3);
            CreateExternalProfile(BlastVectorSizes.float3, true, false, BlastVectorSizes.float3, BlastVectorSizes.float3, BlastVectorSizes.float3);
            CreateExternalProfile(BlastVectorSizes.float4, true, false, BlastVectorSizes.float4);
            CreateExternalProfile(BlastVectorSizes.float4, true, false, BlastVectorSizes.float4, BlastVectorSizes.float4);
            CreateExternalProfile(BlastVectorSizes.float4, true, false, BlastVectorSizes.float4, BlastVectorSizes.float4, BlastVectorSizes.float4);
            CreateExternalProfile(BlastVectorSizes.none,   true, false, BlastVectorSizes.bool32);
            CreateExternalProfile(BlastVectorSizes.bool32, true, false, BlastVectorSizes.bool32);
            CreateExternalProfile(BlastVectorSizes.bool32, true, false, BlastVectorSizes.bool32, BlastVectorSizes.float4, BlastVectorSizes.float1);

            return this;
        }
    }
}