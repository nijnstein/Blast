//############################################################################################################################
// BLAST v1.0.4c                                                                                                             #
// Copyright © 2022 Rob Lemmens | NijnStein Software <rob.lemmens.s31 gmail com> All Rights Reserved                   ^__^\ #
// Unauthorized copying of this file, via any medium is strictly prohibited proprietary and confidential               (oo)\ #
//                                                                                                                     (__)  #
//############################################################################################################################

#define AUTO_EXPAND
#define CHECK_CDATA


#if STANDALONE_VSBUILD
using NSS.Blast.Standalone;
#else
    using UnityEngine;
    using Unity.Burst.CompilerServices;
#endif

using System;
using System.Runtime.CompilerServices;
using Unity.Burst;
using UnityEngine.Assertions;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;


using V1 = System.Single;
using V2 = Unity.Mathematics.float2;
using V3 = Unity.Mathematics.float3;
using V4 = Unity.Mathematics.float4;


namespace NSS.Blast.Interpretor
{

    /// <summary>
    /// 
    /// </summary>
    [BurstCompile]

    unsafe public partial struct BlastInterpretor
    {

        #region Constant Compiletime Defines 
#if DEVELOPMENT_BUILD
        /// <summary>
        /// true if compiled in devmode 
        /// </summary>
        public const bool IsDevelopmentBuild = true;
#else 
        /// <summary>
        /// false if compiled in releasemode 
        /// </summary>
        public const bool IsDevelopmentBuild = false;
#endif
#if TRACE 
        /// <summary>
        /// TRUE if compiled with TRACE define enabled
        /// </summary>
        public const bool IsTrace = true;
#else
        /// <summary>
        /// TRUE if compiled with TRACE define enabled
        /// </summary>
        public const bool IsTrace = false; 
#endif
#if AUTO_EXPAND
        /// <summary>
        /// TRUE if scalars are automatically expanded on assignment to the assigned vectorsize 
        /// </summary>
        public const bool IsAutoExpanding = true;
#else
        public const bool IsAutoExpanding = false; 
#endif
        #endregion

        /// <summary>
        /// >= if the opcode between opt_value and (including) opt_id then its an opcode for a constant
        /// </summary>
        public const byte opt_value = (byte)blast_operation.pi;

        /// <summary>
        /// >= opt_id is starting opcode for parameters, everything until 255|ExOp is considered a datasegment index
        /// </summary>
        public const byte opt_id = (byte)blast_operation.id;

        /// <summary>
        /// maxiumum iteration count, usefull to avoid endless loop bugs in input
        /// </summary>
        public const int max_iterations = 100000;

        /// <summary>
        /// pointer to engine data
        /// </summary>
        [NoAlias]
        [NativeDisableUnsafePtrRestriction]
        internal unsafe BlastEngineData* engine_ptr;

        /// <summary>
        /// optional pointer to environment data
        /// </summary>
        [NoAlias]
        [NativeDisableUnsafePtrRestriction]
        internal unsafe IntPtr environment_ptr;

        /// <summary>
        /// optional pointer to caller information
        /// </summary>
        [NoAlias]
        [NativeDisableUnsafePtrRestriction]
        internal unsafe IntPtr caller_ptr;


        internal BlastPackageData package;

        internal int code_pointer;
        internal int stack_offset;


        /// <summary>
        /// code segment pointer
        /// </summary>
        [NoAlias]
        [NativeDisableUnsafePtrRestriction]
        internal unsafe byte* code;

        /// <summary>
        /// data segment pointer
        /// - for now each data element is 4 bytes long FIXED
        /// </summary>
        [NoAlias]
        [NativeDisableUnsafePtrRestriction]
        internal unsafe void* data;

        /// <summary>
        /// data segment pointer
        /// - for now each data element is 4 bytes long FIXED
        /// </summary>
        [NoAlias]
        [NativeDisableUnsafePtrRestriction]
        internal unsafe void* stack;

        /// <summary>
        /// metadata segment pointer
        /// </summary>
        [NoAlias]
        [NativeDisableUnsafePtrRestriction]
        internal unsafe byte* metadata;

        /// <summary>
        /// a random number generetor set once and if needed updated by scripts with the seed instruction
        /// </summary>
        internal Unity.Mathematics.Random random;

        /// <summary>
        /// if true, the script is executed in validation mode:
        /// - external calls just return 0's
        /// </summary>
        public bool ValidationMode;

        /// <summary>
        /// 
        /// </summary>
        internal bool ValidateOnce;



        #region Reset & Set Package

        /// <summary>
        /// get a copy of the current package data
        /// </summary>
        public BlastPackageData CurrentPackage => package;

        /// <summary>
        /// datasize in currently set package 
        /// </summary>
        public int CurrentDataSize => package.DataSize;

        /// <summary>
        /// codesize in currently set package
        /// </summary>
        public int CurrentCodeSize => package.CodeSize;


        /// <summary>
        /// reset code_pointer and stack_offset to their initial states 
        /// </summary>
        /// <param name="blast">pointer to blast engine data</param>
#pragma warning disable CS1573 // Parameter 'pkg' has no matching param tag in the XML comment for 'BlastInterpretor.Reset(BlastEngineData*, BlastPackageData)' (but other parameters do)
        public void Reset([NoAlias] BlastEngineData* blast, BlastPackageData pkg)
#pragma warning restore CS1573 // Parameter 'pkg' has no matching param tag in the XML comment for 'BlastInterpretor.Reset(BlastEngineData*, BlastPackageData)' (but other parameters do)
        {
            engine_ptr = blast;
            SetPackage(pkg, true);
        }

        /// <summary>
        /// we might need to know if we need to copy back data  (or we use a dummy)
        /// </summary>
        public void SetPackage(BlastPackageData pkg, bool on_reset_dont_update_pointers = false)
        {
            // ensure it is turned of by default when reusing this structure 
            ValidationMode = false;

            package = pkg;

            // the compiler supllies its own pointers to various segments 
            if (pkg.PackageMode != BlastPackageMode.Compiler && !on_reset_dont_update_pointers)
            {
                code = package.Code;
                data = package.Data;
                stack = data;
                metadata = package.Metadata;
            }

            code_pointer = 0;

            // stack starts at an aligned point in the datasegment
            stack_offset = package.DataSegmentStackOffset / 4; // package talks in bytes, interpretor uses elements in data made of 4 bytes 
        }


        /// <summary>
        /// set package and attach a seperate buffer for the datasegment 
        /// </summary>
        /// <param name="pkg"></param>
        /// <param name="datasegment"></param>
        public void SetPackage(BlastPackageData pkg, void* datasegment)
        {
            SetPackage(pkg);
            //
            // update datasegment pointer
            // stackoffset doesnt change, it will still index the same data in the package
            //
            data = datasegment;
        }

        /// <summary>
        /// update package datasegment and reset codepointer, assumes package is correctly set
        /// </summary>
        /// <param name="datasegment"></param>
        public void ResetPackageData(void* datasegment)
        {
            data = datasegment;
            code_pointer = 0;
        }

        /// <summary>
        /// set package data from package and seperate buffers 
        /// </summary>
        /// <param name="pkg">the package data</param>
        /// <param name="_code">code pointer</param>
        /// <param name="_data">datastack pointer</param>
        /// <param name="_metadata">metadata</param>
        /// <param name="initial_stack_offset"></param>
        public void SetPackage(BlastPackageData pkg, byte* _code, float* _data, byte* _metadata, int initial_stack_offset)
        {
            ValidateOnce = false;

            package = pkg;

            code = _code;
            data = _data;
            stack = _data;
            metadata = _metadata;

            code_pointer = 0;
            stack_offset = initial_stack_offset;
        }

        /// <summary>
        /// enable/disable validation run mode
        /// </summary>
        /// <param name="validationmode"></param>
        public void SetValidationMode(bool validationmode)
        {
            ValidationMode = validationmode;
        }

        #endregion

        #region Execute Overloads       
        /// <summary>
        /// execute bytecode
        /// </summary>
        /// <param name="blast">engine data</param>
        /// <param name="environment">pointer to environment data in native memory</param>
        /// <param name="caller">pointer to caller data in native memory</param>
        /// <param name="resume_state">resume state from yield or reset state to initial</param>
        /// <returns>success</returns>
        unsafe int execute([NoAlias] BlastEngineData* blast, [NoAlias] IntPtr environment, [NoAlias] IntPtr caller, bool resume_state)
        {
            V4 register = V4.zero;

            // yield state or reset it otherwise
            if (resume_state & peek(1))
            {
                // resume execution, pop frame counter 
                pop(out register.x);

                if (register.x > 0)
                {
                    register--;
                    push(register.x, BlastVariableDataType.Numeric);
                    // return to yield state
                    return (int)BlastError.yield;
                }
                else
                {
                    // pop state and resume execution
                    pop(out register);
                }
            }
            else
            {
                ValidateOnce = ValidationMode;
                Reset(blast, package);
            }

            // killing yield.. 
            /*int */

            code_pointer = 0;
            return execute(blast, environment, caller); // ref package->code_pointer);
        }

        /// <summary>
        /// execute bytecode
        /// </summary>
        /// <param name="blast">engine data</param>
        /// <param name="environment">pointer to environment data in native memory</param>
        /// <param name="caller">pointer to caller data in native memory</param>
        /// <returns>exit code</returns>
        public int Execute([NoAlias] BlastEngineDataPtr blast, [NoAlias] IntPtr environment, [NoAlias] IntPtr caller)
        {
            return execute(blast.Data, environment, caller, false);
        }

        /// <summary>
        /// execute bytecode set to the interpretor 
        /// </summary>
        public int Execute([NoAlias] BlastEngineDataPtr blast)
        {
            return execute(blast.Data, IntPtr.Zero, IntPtr.Zero, false);
        }

        /// <summary>
        /// resume executing bytecode after yielding
        /// </summary>
        /// <param name="blast">engine data</param>
        /// <param name="environment">pointer to environment data in native memory</param>
        /// <param name="caller">pointer to caller data in native memory</param>
        /// <returns>exit code</returns>
        public int ResumeYield([NoAlias] BlastEngineDataPtr blast, [NoAlias] IntPtr environment, [NoAlias] IntPtr caller)
        {
            return execute(blast.Data, environment, caller, true);
        }

        /// <summary>
        /// resume executing bytecode after yielding
        /// </summary>
        public int ResumeYield([NoAlias] BlastEngineDataPtr blast)
        {
            return execute(blast.Data, IntPtr.Zero, IntPtr.Zero, true);
        }
        #endregion

        #region Decode 62 44 


        /// <summary>
        /// decode info byte of data, 62 uses the upper 6 bits from the parametercount and the lower 2 for vectorsize, resulting in 64 parameters and size 4 vectors..
        /// - update to decode44? 16 params max and 16 vec size?
        /// </summary>
        /// <param name="c">input byte</param>
        /// <param name="vector_size"></param>
        /// <returns>parameter count</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public byte decode62(in byte c, ref byte vector_size)
        {
            vector_size = (byte)(c & 0b11);
            return (byte)((c & 0b1111_1100) >> 2);
        }

        /// <summary>
        /// decode info byte of data, 44 uses 4 bytes for each, that is 16 differen vectorsizes and param types or counts 
        /// </summary>
        /// <param name="c">input byte</param>
        /// <param name="vector_size"></param>
        /// <returns>parameter count</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public byte decode44(in byte c, ref byte vector_size)
        {
            vector_size = (byte)(c & 0b1111);
            return (byte)((c & 0b1111_0000) >> 4);
        }

        /// <summary>
        /// decode info byte of data, 44 uses 4 bytes for each, that is 16 differen vectorsizes and param types or counts 
        /// </summary>
        /// <param name="c">input byte</param>
        /// <param name="vector_size"></param>
        /// <returns>parameter count</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public byte decode44(in byte c, out BlastVectorSizes datatype)
        {
            datatype = (BlastVectorSizes)(c & 0b1111);
            return (byte)((c & 0b1111_0000) >> 4);
        }




        #endregion

        #region Metadata 

        //
        // - the interpretor needs metadata for determining datatype and vector in some situations (example: pops to functions accepting multiple types and sizes..)
        // - ONLY the metadata for the variable data needs to be retained in the package for storage, but for execution it should have room matching stack size 
        //
        // - metadata holds 1 byte of information per datapoint
        // - lower 4 bits = vectorsize   1-16
        // - upper 4 bits = datatype     16 possible, 2 used 
        //
        // - on storage, metadata needs 1*datacount bytes
        // - on execution it needs 1*datacount + 1*stack_capacity bytes
        // 
        //  

        //

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static internal void SetMetaData(in byte* metadata, in BlastVariableDataType type, in byte size, in byte offset)
        {
            byte meta = (byte)((byte)(size & 0b0000_1111) | (byte)((byte)type << 4));

            // set on first byte
            metadata[offset] = meta;

            // - not sure if making this conditional is an optimization but its a write that can flush soo.. 
            if (size == 1)
            {
            }
            else
            {
                // pop also needs on last byte 
                metadata[offset + size - 1] = meta;
            }
        }

        /// <summary>
        /// get the size of a vector from metadata
        /// </summary>
        /// <param name="metadata">the metadata segment</param>
        /// <param name="offset">the variable offset</param>
        /// <returns>size of vector, 0 means 4..</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public byte GetMetaDataSize(in byte* metadata, in byte offset)
        {
            return (byte)(metadata[offset] & 0b0000_1111);
        }

        /// <summary>
        /// get datatype of variable from metadata
        /// </summary>
        /// <param name="metadata">the metadata segment</param>
        /// <param name="offset">the variable offset</param>
        /// <returns>the datatype</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public BlastVariableDataType GetMetaDataType(in byte* metadata, in byte offset)
        {
            return (BlastVariableDataType)((byte)(metadata[offset] & 0b1111_0000) >> 4);
        }

        /// <summary>
        /// get datatype and vectorsize of a variable from metadata 
        /// </summary>
        /// <param name="metadata">the metadata segment</param>
        /// <param name="offset">offset of variable</param>
        /// <param name="size">output vectorsize</param>
        /// <param name="type">output parametertype</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public void GetMetaData(in byte* metadata, in byte offset, out byte size, out BlastVariableDataType type)
        {
            byte b = metadata[offset];
            size = (byte)(b & 0b0000_1111);
            type = (BlastVariableDataType)((b & 0b1111_0000) >> 4);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public BlastVectorType GetMetaDataVectorType(in byte* metadata, in byte offset)
        {
            byte b = metadata[offset];
            int size = b & 0b0000_1111; 
            size = math.select(size, 4, size == 0);
            return ((BlastVariableDataType)((b & 0b1111_0000) >> 4)).Combine((byte)size, false);           
        }



        #endregion

        #region Yield

        /// <summary>
        /// yield - stacks state
        /// - note that it consumes 20 bytes of stack space (on top of max used by script) to use yield
        /// </summary>
        /// <param name="f4_register"></param>
        internal void yield(float4 f4_register)
        {
            BlastVariableDataType datatype = BlastVariableDataType.Numeric; 

            // stack state 
            push(f4_register, datatype);

            // get nr of frames to yield 
            byte b = code[code_pointer];
            if (b >= opt_value)
            {
                push(pop_f1(ref code_pointer, ref datatype), in datatype);
            }
            else
            {
                // return next frame (remember to pop everything back to correct state) 
                //  -> allow b to be a number < opt_value in opcode
                push((float)b);
            }
        }
        #endregion

        #region Static Execution Helpers

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        float negate(in bool negate, float f) { return math.select(f, -f, negate); }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        float2 negate(in bool negate, float2 f) { return math.select(f, -f, negate); }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        float3 negate(in bool negate, float3 f) { return math.select(f, -f, negate); }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        float4 negate(in bool negate, float4 f) { return math.select(f, -f, negate); }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        int negate(in bool negate, int f) { return math.select(f, -f, negate); }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        int2 negate(in bool negate, int2 f) { return math.select(f, -f, negate); }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        int3 negate(in bool negate, int3 f) { return math.select(f, -f, negate); }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        int4 negate(in bool negate, int4 f) { return math.select(f, -f, negate); }

        /// <summary>
        /// return true if op is a value: 
        /// - pop counts!!
        /// - byte value between lowest constant and extended op id  (dont allow constants in extended op id's)
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool BlastOperationIsValue(in byte op)
        {
            return
                // its a value if from stack 
                op == (byte)blast_operation.pop
                ||
                // or between the lowest constant and the highest possible variable 
                (op >= (byte)blast_operation.pi && op < 255);
        }

        /// <summary>
        /// true if op is expand_vx
        /// </summary>
        public static bool IsExpansionOperator(blast_operation op)
        {
            switch (op)
            {
                case blast_operation.expand_v2:
                case blast_operation.expand_v3:
                case blast_operation.expand_v4:
                    return true;
            }
            return false;
        }


        /// <summary>
        /// WARNING checks if is operation, uses value op enum blast_operation.add until blast_operation.not_equals 
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsMathematicalOrBooleanOperation(blast_operation op)
        {
            return op >= blast_operation.add && op <= blast_operation.not_equals;
        }


        /// <summary>
        /// check if the operation is one of the assignment operations: assing|assigns|assingf|assingfn|assingfen|assingv
        /// </summary>
        public static bool IsAssignmentOperation(blast_operation op)
        {
            return op == blast_operation.assign || op == blast_operation.assigns || op == blast_operation.assignf
                || op == blast_operation.assignfn || op == blast_operation.assignfen || op == blast_operation.assignv;
        }

        /// <summary>
        /// WARNING checks if +-*/ uses value op enum!!  
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsMathematicalOperation(blast_operation op)
        {
            return op >= blast_operation.add && op < blast_operation.and;
        }

        /// <summary>
        /// WARNING checks if boolean operaion, uses value op enum!! 
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsBooleanOperation(blast_operation op)
        {
            return op >= blast_operation.and && op <= blast_operation.not_equals;
        }


        /// <summary>
        /// get the offset specified by an indexer: [xyzwrgba]
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetIndexerOperationIndex(blast_operation op)
        {
            switch (op)
            {
                case blast_operation.index_x: return 0;
                case blast_operation.index_y: return 1;
                case blast_operation.index_z: return 2;
                case blast_operation.index_w: return 3;
                case blast_operation.index_n:
                default: return -1;
            }
        }




        #endregion
 
        #region Debug Data
        /// <summary>
        /// handle command to show the given field in debug    (debug skips negation)
        /// </summary>
        /// <param name="code_pointer"></param>
        /// <param name="vector_size"></param>
        /// <param name="op_id"></param>
        /// <param name="datatype"></param>
        /// <param name="pdata"></param>
        static void Handle_DebugData(int code_pointer, byte vector_size, int op_id, BlastVariableDataType datatype, void* pdata, bool is_negated = false)
        {
#if DEVELOPMENT_BUILD || TRACE
            if (pdata != null)
            {
                // in burst we can only directly log to debug.. so this gets verbose .. 
                switch (datatype)
                {
                    case BlastVariableDataType.ID:
                        int* idata = (int*)pdata;
                        switch (vector_size)
                        {
                            case 1: Debug.Log($"Blast.Debug - codepointer: {code_pointer}, id: {op_id}, ID: {idata[0]}, vectorsize: {vector_size}"); break;
                            case 2: Debug.Log($"Blast.Debug - codepointer: {code_pointer}, id: {op_id}, ID: [{idata[0]}, {idata[1]}], vectorsize: {vector_size}"); break;
                            case 3: Debug.Log($"Blast.Debug - codepointer: {code_pointer}, id: {op_id}, ID: [{idata[0]}, {idata[1]}, {idata[2]}], vectorsize: {vector_size}"); break;
                            case 4: Debug.Log($"Blast.Debug - codepointer: {code_pointer}, id: {op_id}, ID: [{idata[0]}, {idata[1]}, {idata[2]}, {idata[3]}], vectorsize: {vector_size}"); break;
                            case 5: Debug.Log($"Blast.Debug - codepointer: {code_pointer}, id: {op_id}, ID: [{idata[0]}, {idata[1]}, {idata[2]}, {idata[3]}, {idata[4]}], vectorsize: {vector_size}"); break;
                            case 6: Debug.Log($"Blast.Debug - codepointer: {code_pointer}, id: {op_id}, ID: [{idata[0]}, {idata[1]}, {idata[2]}, {idata[3]}, {idata[4]}, {idata[5]}], vectorsize: {vector_size}"); break;
                            case 7: Debug.Log($"Blast.Debug - codepointer: {code_pointer}, id: {op_id}, ID: [{idata[0]}, {idata[1]}, {idata[2]}, {idata[3]}, {idata[4]}, {idata[5]}, {idata[6]}], vectorsize: {vector_size}"); break;
                            case 8: Debug.Log($"Blast.Debug - codepointer: {code_pointer}, id: {op_id}, ID: [{idata[0]}, {idata[1]}, {idata[2]}, {idata[3]}, {idata[4]}, {idata[5]}, {idata[6]}, {idata[7]}], vectorsize: {vector_size}"); break;
                            case 9: Debug.Log($"Blast.Debug - codepointer: {code_pointer}, id: {op_id}, ID: [{idata[0]}, {idata[1]}, {idata[2]}, {idata[3]}, {idata[4]}, {idata[5]}, {idata[6]}, {idata[7]}, {idata[8]}], vectorsize: {vector_size}"); break;
                            case 10: Debug.Log($"Blast.Debug - codepointer: {code_pointer}, id: {op_id}, ID: [{idata[0]}, {idata[1]}, {idata[2]}, {idata[3]}, {idata[4]}, {idata[5]}, {idata[6]}, {idata[7]}, {idata[8]}, {idata[9]}], vectorsize: {vector_size}"); break;
                            case 11: Debug.Log($"Blast.Debug - codepointer: {code_pointer}, id: {op_id}, ID: [{idata[0]}, {idata[1]}, {idata[2]}, {idata[3]}, {idata[4]}, {idata[5]}, {idata[6]}, {idata[7]}, {idata[8]}, {idata[9]}, {idata[10]}], vectorsize: {vector_size}"); break;
                            case 12: Debug.Log($"Blast.Debug - codepointer: {code_pointer}, id: {op_id}, ID: [{idata[0]}, {idata[1]}, {idata[2]}, {idata[3]}, {idata[4]}, {idata[5]}, {idata[6]}, {idata[7]}, {idata[8]}, {idata[9]}, {idata[10]}, {idata[11]}], vectorsize: {vector_size}"); break;
                            case 13: Debug.Log($"Blast.Debug - codepointer: {code_pointer}, id: {op_id}, ID: [{idata[0]}, {idata[1]}, {idata[2]}, {idata[3]}, {idata[4]}, {idata[5]}, {idata[6]}, {idata[7]}, {idata[8]}, {idata[9]}, {idata[10]}, {idata[11]}, {idata[12]}], vectorsize: {vector_size}"); break;
                            case 14: Debug.Log($"Blast.Debug - codepointer: {code_pointer}, id: {op_id}, ID: [{idata[0]}, {idata[1]}, {idata[2]}, {idata[3]}, {idata[4]}, {idata[5]}, {idata[6]}, {idata[7]}, {idata[8]}, {idata[9]}, {idata[10]}, {idata[11]}, {idata[12]}, {idata[13]}], vectorsize: {vector_size}"); break;
                            case 15: Debug.Log($"Blast.Debug - codepointer: {code_pointer}, id: {op_id}, ID: [{idata[0]}, {idata[1]}, {idata[2]}, {idata[3]}, {idata[4]}, {idata[5]}, {idata[6]}, {idata[7]}, {idata[8]}, {idata[9]}, {idata[10]}, {idata[11]}, {idata[12]}, {idata[13]}, {idata[14]}], vectorsize: {vector_size}"); break;
                            case 16: Debug.Log($"Blast.Debug - codepointer: {code_pointer}, id: {op_id}, ID: [{idata[0]}, {idata[1]}, {idata[2]}, {idata[3]}, {idata[4]}, {idata[5]}, {idata[6]}, {idata[7]}, {idata[8]}, {idata[9]}, {idata[10]}, {idata[11]}, {idata[12]}, {idata[13]}, {idata[14]}, {idata[15]}], vectorsize: {vector_size}"); break;
                            default:
                                Debug.LogError($"Blast.Debug(data) - Vectorsize of {vector_size} is not supported in variable/operation with id {op_id} and datatype ID");
                                break;
                        }
                        break;

                    case BlastVariableDataType.Numeric:
                        float* fdata = (float*)pdata;
                        switch (vector_size)
                        {
#if STANDALONE_VSBUILD
                            case 1: Debug.Log($"Blast.Debug - codepointer: {code_pointer}, id: {op_id}, NUMERIC: {fdata[0].ToString("0.00")}, vectorsize: {vector_size}"); break;
                            case 2: Debug.Log($"Blast.Debug - codepointer: {code_pointer}, id: {op_id}, NUMERIC: [{fdata[0].ToString("0.00")}, {fdata[1].ToString("0.00")}], vectorsize: {vector_size}"); break;
                            case 3: Debug.Log($"Blast.Debug - codepointer: {code_pointer}, id: {op_id}, NUMERIC: [{fdata[0].ToString("0.00")}, {fdata[1].ToString("0.00")}, {fdata[2].ToString("0.00")}], vectorsize: {vector_size}"); break;
                            case 4: Debug.Log($"Blast.Debug - codepointer: {code_pointer}, id: {op_id}, NUMERIC: [{fdata[0].ToString("0.00")}, {fdata[1].ToString("0.00")}, {fdata[2].ToString("0.00")}, {fdata[3].ToString("0.00")}], vectorsize: {vector_size}"); break;
                            case 5: Debug.Log($"Blast.Debug - codepointer: {code_pointer}, id: {op_id}, NUMERIC: [{fdata[0].ToString("0.00")}, {fdata[1].ToString("0.00")}, {fdata[2].ToString("0.00")}, {fdata[3].ToString("0.00")}, {fdata[4].ToString("0.00")}], vectorsize: {vector_size}"); break;
                            case 6: Debug.Log($"Blast.Debug - codepointer: {code_pointer}, id: {op_id}, NUMERIC: [{fdata[0].ToString("0.00")}, {fdata[1].ToString("0.00")}, {fdata[2].ToString("0.00")}, {fdata[3].ToString("0.00")}, {fdata[4].ToString("0.00")}, {fdata[5].ToString("0.00")}], vectorsize: {vector_size}"); break;
                            case 7: Debug.Log($"Blast.Debug - codepointer: {code_pointer}, id: {op_id}, NUMERIC: [{fdata[0].ToString("0.00")}, {fdata[1].ToString("0.00")}, {fdata[2].ToString("0.00")}, {fdata[3].ToString("0.00")}, {fdata[4].ToString("0.00")}, {fdata[5].ToString("0.00")}, {fdata[6].ToString("0.00")}], vectorsize: {vector_size}"); break;
                            case 8: Debug.Log($"Blast.Debug - codepointer: {code_pointer}, id: {op_id}, NUMERIC: [{fdata[0].ToString("0.00")}, {fdata[1].ToString("0.00")}, {fdata[2].ToString("0.00")},  {fdata[3].ToString("0.00")}, {fdata[4].ToString("0.00")}, {fdata[5].ToString("0.00")}, {fdata[6].ToString("0.00")}, {fdata[7].ToString("0.00")}], vectorsize: {vector_size}"); break;
                            case 9: Debug.Log($"Blast.Debug - codepointer: {code_pointer}, id: {op_id}, NUMERIC: [{fdata[0].ToString("0.00")}, {fdata[1].ToString("0.00")}, {fdata[2].ToString("0.00")}, {fdata[3].ToString("0.00")}, {fdata[4].ToString("0.00")}, {fdata[5].ToString("0.00")}, {fdata[6].ToString("0.00")}, {fdata[7].ToString("0.00")}, {fdata[8].ToString("0.00")}], vectorsize: {vector_size}"); break;
                            case 10: Debug.Log($"Blast.Debug - codepointer: {code_pointer}, id: {op_id}, NUMERIC: [{fdata[0].ToString("0.00")}, {fdata[1].ToString("0.00")}, {fdata[2].ToString("0.00")}, {fdata[3].ToString("0.00")}, {fdata[4].ToString("0.00")}, {fdata[5].ToString("0.00")}, {fdata[6].ToString("0.00")}, {fdata[7].ToString("0.00")}, {fdata[8].ToString("0.00")}, {fdata[9].ToString("0.00")}], vectorsize: {vector_size}"); break;
                            case 11: Debug.Log($"Blast.Debug - codepointer: {code_pointer}, id: {op_id}, NUMERIC: [{fdata[0].ToString("0.00")}, {fdata[1].ToString("0.00")}, {fdata[2].ToString("0.00")}, {fdata[3].ToString("0.00")}, {fdata[4].ToString("0.00")}, {fdata[5].ToString("0.00")}, {fdata[6].ToString("0.00")}, {fdata[7].ToString("0.00")}, {fdata[8].ToString("0.00")}, {fdata[9].ToString("0.00")}, {fdata[10].ToString("0.00")}], vectorsize: {vector_size}"); break;
                            case 12: Debug.Log($"Blast.Debug - codepointer: {code_pointer}, id: {op_id}, NUMERIC: [{fdata[0].ToString("0.00")}, {fdata[1].ToString("0.00")}, {fdata[2].ToString("0.00")}, {fdata[3].ToString("0.00")}, {fdata[4].ToString("0.00")}, {fdata[5].ToString("0.00")}, {fdata[6].ToString("0.00")}, {fdata[7].ToString("0.00")}, {fdata[8].ToString("0.00")}, {fdata[9].ToString("0.00")}, {fdata[10].ToString("0.00")}, {fdata[11].ToString("0.00")}], vectorsize: {vector_size}"); break;
                            case 13: Debug.Log($"Blast.Debug - codepointer: {code_pointer}, id: {op_id}, NUMERIC: [{fdata[0].ToString("0.00")}, {fdata[1].ToString("0.00")}, {fdata[2].ToString("0.00")}, {fdata[3].ToString("0.00")}, {fdata[4].ToString("0.00")}, {fdata[5].ToString("0.00")}, {fdata[6].ToString("0.00")}, {fdata[7].ToString("0.00")}, {fdata[8].ToString("0.00")}, {fdata[9].ToString("0.00")}, {fdata[10].ToString("0.00")}, {fdata[11].ToString("0.00")}, {fdata[12].ToString("0.00")}], vectorsize: {vector_size}"); break;
                            case 14: Debug.Log($"Blast.Debug - codepointer: {code_pointer}, id: {op_id}, NUMERIC: [{fdata[0].ToString("0.00")}, {fdata[1].ToString("0.00")}, {fdata[2].ToString("0.00")}, {fdata[3].ToString("0.00")}, {fdata[4].ToString("0.00")}, {fdata[5].ToString("0.00")}, {fdata[6].ToString("0.00")}, {fdata[7].ToString("0.00")}, {fdata[8].ToString("0.00")}, {fdata[9].ToString("0.00")}, {fdata[10].ToString("0.00")}, {fdata[11].ToString("0.00")}, {fdata[12].ToString("0.00")}, {fdata[13].ToString("0.00")}], vectorsize: {vector_size}"); break;
                            case 15: Debug.Log($"Blast.Debug - codepointer: {code_pointer}, id: {op_id}, NUMERIC: [{fdata[0].ToString("0.00")}, {fdata[1].ToString("0.00")}, {fdata[2].ToString("0.00")}, {fdata[3].ToString("0.00")}, {fdata[4].ToString("0.00")}, {fdata[5].ToString("0.00")}, {fdata[6].ToString("0.00")}, {fdata[7].ToString("0.00")}, {fdata[8].ToString("0.00")}, {fdata[9].ToString("0.00")}, {fdata[10].ToString("0.00")}, {fdata[11].ToString("0.00")}, {fdata[12].ToString("0.00")}, {fdata[13].ToString("0.00")}, {fdata[14].ToString("0.00")}], vectorsize: {vector_size}"); break;
                            case 16: Debug.Log($"Blast.Debug - codepointer: {code_pointer}, id: {op_id}, NUMERIC: [{fdata[0].ToString("0.00")}, {fdata[1].ToString("0.00")}, {fdata[2].ToString("0.00")}, {fdata[3].ToString("0.00")}, {fdata[4].ToString("0.00")}, {fdata[5].ToString("0.00")}, {fdata[6].ToString("0.00")}, {fdata[7].ToString("0.00")}, {fdata[8].ToString("0.00")}, {fdata[9].ToString("0.00")}, {fdata[10].ToString("0.00")}, {fdata[11].ToString("0.00")}, {fdata[12].ToString("0.00")}, {fdata[13].ToString("0.00")}, {fdata[14].ToString("0.00")}, {fdata[15].ToString("0.00")}], vectorsize: {vector_size}"); break;
#else
                            // BURST : no string functions other then those in Debug 
                            case 1: Debug.Log($"Blast.Debug - codepointer: {code_pointer}, id: {op_id}, NUMERIC: {fdata[0]}, vectorsize: {vector_size}"); break;
                            case 2: Debug.Log($"Blast.Debug - codepointer: {code_pointer}, id: {op_id}, NUMERIC: [{fdata[0]}, {fdata[1]}], vectorsize: {vector_size}"); break;
                            case 3: Debug.Log($"Blast.Debug - codepointer: {code_pointer}, id: {op_id}, NUMERIC: [{fdata[0]}, {fdata[1]}, {fdata[2]}], vectorsize: {vector_size}"); break;
                            case 4: Debug.Log($"Blast.Debug - codepointer: {code_pointer}, id: {op_id}, NUMERIC: [{fdata[0]}, {fdata[1]}, {fdata[2]}, {fdata[3]}], vectorsize: {vector_size}"); break;
                            case 5: Debug.Log($"Blast.Debug - codepointer: {code_pointer}, id: {op_id}, NUMERIC: [{fdata[0]}, {fdata[1]}, {fdata[2]}, {fdata[3]}, {fdata[4]}], vectorsize: {vector_size}"); break;
                            case 6: Debug.Log($"Blast.Debug - codepointer: {code_pointer}, id: {op_id}, NUMERIC: [{fdata[0]}, {fdata[1]}, {fdata[2]}, {fdata[3]}, {fdata[4]}, {fdata[5]}], vectorsize: {vector_size}"); break;
                            case 7: Debug.Log($"Blast.Debug - codepointer: {code_pointer}, id: {op_id}, NUMERIC: [{fdata[0]}, {fdata[1]}, {fdata[2]}, {fdata[3]}, {fdata[4]}, {fdata[5]}, {fdata[6]}], vectorsize: {vector_size}"); break;
                            case 8: Debug.Log($"Blast.Debug - codepointer: {code_pointer}, id: {op_id}, NUMERIC: [{fdata[0]}, {fdata[1]}, {fdata[2]}, {fdata[3]}, {fdata[4]}, {fdata[5]}, {fdata[6]}, {fdata[7]}], vectorsize: {vector_size}"); break;
                            case 9: Debug.Log($"Blast.Debug - codepointer: {code_pointer}, id: {op_id}, NUMERIC: [{fdata[0]}, {fdata[1]}, {fdata[2]}, {fdata[3]}, {fdata[4]}, {fdata[5]}, {fdata[6]}, {fdata[7]},  {fdata[8]}], vectorsize: {vector_size}"); break;
                            case 10: Debug.Log($"Blast.Debug - codepointer: {code_pointer}, id: {op_id}, NUMERIC: [{fdata[0]}, {fdata[1]}, {fdata[2]}, {fdata[3]}, {fdata[4]}, {fdata[5]}, {fdata[6]}, {fdata[7]}, {fdata[8]}, {fdata[9]}], vectorsize: {vector_size}"); break;
                            case 11: Debug.Log($"Blast.Debug - codepointer: {code_pointer}, id: {op_id}, NUMERIC: [{fdata[0]}, {fdata[1]}, {fdata[2]}, {fdata[3]}, {fdata[4]}, {fdata[5]}, {fdata[6]}, {fdata[7]}, {fdata[8]}, {fdata[9]}, {fdata[10]}], vectorsize: {vector_size}"); break;
                            case 12: Debug.Log($"Blast.Debug - codepointer: {code_pointer}, id: {op_id}, NUMERIC: [{fdata[0]}, {fdata[1]}, {fdata[2]}, {fdata[3]}, {fdata[4]}, {fdata[5]}, {fdata[6]}, {fdata[7]}, {fdata[8]}, {fdata[9]}, {fdata[10]}, {fdata[11]}], vectorsize: {vector_size}"); break;
                            case 13: Debug.Log($"Blast.Debug - codepointer: {code_pointer}, id: {op_id}, NUMERIC: [{fdata[0]}, {fdata[1]}, {fdata[2]}, {fdata[3]}, {fdata[4]}, {fdata[5]}, {fdata[6]}, {fdata[7]}, {fdata[8]}, {fdata[9]}, {fdata[10]}, {fdata[11]}, {fdata[12]}], vectorsize: {vector_size}"); break;
                            case 14: Debug.Log($"Blast.Debug - codepointer: {code_pointer}, id: {op_id}, NUMERIC: [{fdata[0]}, {fdata[1]}, {fdata[2]}, {fdata[3]}, {fdata[4]}, {fdata[5]}, {fdata[6]}, {fdata[7]}, {fdata[8]}, {fdata[9]}, {fdata[10]}, {fdata[11]}, {fdata[12]}, {fdata[13]}], vectorsize: {vector_size}"); break;
                            case 15: Debug.Log($"Blast.Debug - codepointer: {code_pointer}, id: {op_id}, NUMERIC: [{fdata[0]}, {fdata[1]}, {fdata[2]}, {fdata[3]}, {fdata[4]}, {fdata[5]}, {fdata[6]}, {fdata[7]}, {fdata[8]}, {fdata[9]}, {fdata[10]}, {fdata[11]}, {fdata[12]}, {fdata[13]}, {fdata[14]}], vectorsize: {vector_size}"); break;
                            case 16: Debug.Log($"Blast.Debug - codepointer: {code_pointer}, id: {op_id}, NUMERIC: [{fdata[0]}, {fdata[1]}, {fdata[2]}, {fdata[3]}, {fdata[4]}, {fdata[5]}, {fdata[6]}, {fdata[7]}, {fdata[8]}, {fdata[9]}, {fdata[10]}, {fdata[11]}, {fdata[12]}, {fdata[13]}, {fdata[14]}, {fdata[15]}], vectorsize: {vector_size}"); break;
#endif
                            default:
                                Debug.LogError($"Blast.Debug(data) - Vectorsize of {vector_size} is not supported in variable/operation with id {op_id} and datatype ID");
                                break;
                        }
                        break;

                    case BlastVariableDataType.Bool32:
                        {
                            // vectorsize must be 1 

                            uint b32 = ((uint*)pdata)[0];
#if STANDALONE_VSBUILD
                            Debug.Log($"Blast.Debug - codepointer: {code_pointer}, id: {op_id}, BOOL32: {CodeUtils.FormatBool32(b32)}");
#else
                            Debug.Log($"Blast.Debug - codepointer: {code_pointer}, id: {op_id}, BOOL32: {b32}"); 
#endif
                        }
                        break;


                    case BlastVariableDataType.CData:
                        {
                            // This exclusively points to a dataarray in the datasegment 

                            // debug/alert is the only function accepting CData for now
                            // others should just fail completely with error in trace modes-> fix that

                            Debug.Log($"Blast.Debug - codepointer: {code_pointer}, id: {op_id}, <CDATA> in datasegment aligned to vectorsize {vector_size}");
                        }
                        break;

                    default:
                        Debug.LogError($"Blast.Debug(data) - Datatype '{datatype}' not supported in variable/operation with id {op_id} and vectorsize {vector_size}");
                        break;
                }
            }
#endif
        }


        /// <summary>
        /// handle debugdata given as constant cdata inline encoded 
        /// </summary>
        void Handle_DebugData_CData(int code_pointer)
        {
            int jump_offset = (code[code_pointer + 1] << 8) + code[code_pointer + 2];

            // get data location 
            int index = code_pointer - jump_offset + 1;
            code_pointer += 2;

            CDATAEncodingType encoding = (CDATAEncodingType)code[index];

#if TRACE || DEVELOPMENT_BUILD
            if (!IsValidCDATAStart(encoding))
            {
                Debug.LogError($"Blast.Interpretor.Handle_DebugData_CData: expected cdata encoding at {index} as jumped to from cdataref at {code_pointer}");
                return;
            }
#endif
            
            // should be cdata TODO validate in debug 
            int length = (code[index + 1] << 8) + code[index + 2];
            int data_start = index + 3;


            unsafe
            {
#if STANDALONE_VSBUILD
                char* ch = stackalloc char[length + 1];
                bool ascii = true;

                for (int i = data_start; i < data_start + length; i++)
                {

                    // set from 8 bit...
                    int j = i - data_start; 
                    ch[j] = (char)code[i];
                    ascii = ascii && (char.IsLetterOrDigit(ch[j]) || char.IsWhiteSpace(ch[j]));
                }

                if (ascii)
                {
                    // zero terminate and show as string 
                    ch[length] = (char)0;
                    Debug.Log(new string(ch));
                }
                else
                {
                    // show as bytes 
                    System.Text.StringBuilder sb = StringBuilderCache.Acquire();
                    sb.Append($"DEBUG codepointer: {code_pointer} <CDATA>[{length}] at index {data_start}: "); 
                    for (int i = data_start; i < data_start + length; i++)
                    {
                        sb.Append(code[i].ToString().PadLeft(3, '0'));
                        sb.Append(" ");
                    }
                    Debug.Log(StringBuilderCache.GetStringAndRelease(ref sb));
#else
                {
                    Debug.Log($"DEBUG codepointer: {code_pointer} <CDATA>[{length}] at index {data_start}");
#endif 
                }
            }
        }

        /// <summary>
        /// printout an overview of the datasegment/stack (if shared)
        /// </summary>
        void Handle_DebugStack()
        {
#if DEVELOPMENT_BUILD || TRACE
            if (ValidateOnce) return;

#if STANDALONE_VSBUILD
            for (int i = 0; i < stack_offset; i++)
            {
                if (i < package.DataSegmentStackOffset / 4)
                {
                    Debug.Log($"DATA  {i} = {((float*)data)[i].ToString().PadRight(10)}   {GetMetaDataSize(metadata, (byte)i)}  {GetMetaDataType(metadata, (byte)i)}");
                }
                else
                {
                    Debug.Log($"STACK {i} = {((float*)data)[i].ToString().PadRight(10)}   {GetMetaDataSize(metadata, (byte)i)}  {GetMetaDataType(metadata, (byte)i)}");
                }
            }
#else
            // less string magic in burstable code => less readable 
            for (int i = 0; i < stack_offset; i++)
            {
                if (i < package.DataSegmentStackOffset / 4)
                {
                    Debug.Log($"DATA  {i} = {((float*)data)[i]}   {GetMetaDataSize(metadata, (byte)i)}  {GetMetaDataType(metadata, (byte)i)}");
                }
                else
                {
                    Debug.Log($"STACK {i} = {((float*)data)[i]}   {GetMetaDataSize(metadata, (byte)i)}  {GetMetaDataType(metadata, (byte)i)}");
                }
            }
#endif
#endif
        }

        #endregion

        #region External Functionpointer calls 


        /// <summary>
        /// call an external function pointer, pointed to by an 8|16|32 bit identifier 
        ///</summary>
        void CallExternalFunction(ref int code_pointer, ref byte vector_size, out float4 f4)
        {
            // get 16 bit id from code stream 
            int id = (code[code_pointer + 1] << 8) + code[code_pointer + 2];

            // advance code pointer beyond the script id
            code_pointer += 3;

            // call the function 
#if DEVELOPMENT_BUILD || TRACE
            if (engine_ptr->CanBeAValidFunctionId(id))
            {
#endif
                BlastScriptFunction p = engine_ptr->Functions[id];

#if TRACE 
                Debug.Log($"call fp id: {id} {environment_ptr.ToInt64()} {caller_ptr.ToInt64()}, parameter count = {p.MinParameterCount}");
#endif 

                f4 = CALL_EF(ref code_pointer, in p);

                //
                // codepointer might be 1 too far if paramater or not
                //


                vector_size = p.ReturnsVectorSize;

#if DEVELOPMENT_BUILD || TRACE
            }
            else
            {                 
                Debug.LogError($"blast: failed to call function pointer with id: {id}");
                f4 = float.NaN;
            }
#endif

        }

        #endregion

        #region Function Handlers 

                   



        /// <summary>
        /// get dot product of 2 paramaters 
        /// - 2 inputs
        /// - inputvector size = any
        /// - output vectorsize == 1
        /// </summary>
        void get_dot_result(ref int code_pointer, ref byte vector_size, out float4 f4)
        {
            f4 = float.NaN;
            BlastVariableDataType datatype;
            bool neg1;

            code_pointer++;
            float* d1 = (float*)pop_p_info(ref code_pointer, out datatype, out vector_size, out neg1);

#if DEVELOPMENT_BUILD || TRACE
            BlastVariableDataType datatype_2;
            byte vector_size_2;
            bool neg2;

            float* d2 = (float*)pop_p_info(ref code_pointer, out datatype_2, out vector_size_2, out neg2);

            if (datatype == BlastVariableDataType.ID)
            {
                Debug.LogError($"ID datatype in dot not implemented, codepointer: {code_pointer} = > {code[code_pointer]}");
                return;
            }
            if (datatype != datatype_2 || vector_size != vector_size_2)
            {
                Debug.LogError($"dot: input parameter datatype|vector_size mismatch, these must be equal. p1 = {datatype}.{vector_size}, p2 = {datatype_2}.{vector_size_2}");
                return;
            }
#endif

            switch ((BlastVectorSizes)vector_size)
            {
                case 0:
#if DEVELOPMENT_BUILD || TRACE
                case BlastVectorSizes.float4: f4.x = math.dot(negate(neg1, ((float4*)d1)[0]), negate(neg1, ((float4*)d2)[0])); break;
                case BlastVectorSizes.float1: f4.x = math.dot(negate(neg1, ((float*)d1)[0]), negate(neg1, ((float*)d2)[0])); break;
                case BlastVectorSizes.float2: f4.x = math.dot(negate(neg1, ((float2*)d1)[0]), negate(neg1, ((float2*)d2)[0])); break;
                case BlastVectorSizes.float3: f4.x = math.dot(negate(neg1, ((float3*)d1)[0]), negate(neg1, ((float3*)d2)[0])); break;
#else
                // in release build we dont need information on the second parameter 
                case BlastVectorSizes.float4: f4.x = math.dot(negate(neg1, ((float4*)d1)[0]), pop_as_f4(ref code_pointer)); break;
                case BlastVectorSizes.float1: f4.x = math.dot(negate(neg1, ((float*)d1)[0]),  pop_as_f1(ref code_pointer)); break;
                case BlastVectorSizes.float2: f4.x = math.dot(negate(neg1, ((float2*)d1)[0]), pop_as_f2(ref code_pointer)); break;
                case BlastVectorSizes.float3: f4.x = math.dot(negate(neg1, ((float3*)d1)[0]), pop_as_f3(ref code_pointer)); break;
#endif

#if DEVELOPMENT_BUILD || TRACE
                default:
                    {
                        Debug.LogError($"dot: {datatype} vector size '{vector_size}' not supported ");
                        break;
                    }
#endif
            }

            code_pointer += 2;
            vector_size = 1;
        }


        /// <summary>
        /// cross product of 2 inputs 
        /// - 2 input parameters 
        /// - input vectorsize == 3
        /// - output vectorsize == 3
        /// </summary>
        void get_cross_result(ref int code_pointer, ref byte vector_size, out float4 f4)
        {
            f4 = float.NaN;
            BlastVariableDataType datatype;
            bool neg1;

            code_pointer++;
            float* d1 = (float*)pop_p_info(ref code_pointer, out datatype, out vector_size, out neg1);

#if DEVELOPMENT_BUILD || TRACE
            BlastVariableDataType datatype_2;
            byte vector_size_2;
            bool neg2;

            float* d2 = (float*)pop_p_info(ref code_pointer, out datatype_2, out vector_size_2, out neg2);

            if (datatype == BlastVariableDataType.ID)
            {
                Debug.LogError($"ID datatype in cross not implemented, codepointer: {code_pointer} = > {code[code_pointer]}");
                return;
            }
            if (datatype != datatype_2 || vector_size != vector_size_2)
            {
                Debug.LogError($"cross: input parameter datatype|vector_size mismatch, these must be equal. p1 = {datatype}.{vector_size}, p2 = {datatype_2}.{vector_size_2}");
                return;
            }
#endif

            switch ((BlastVectorSizes)vector_size)
            {
#if DEVELOPMENT_BUILD || TRACE
                case BlastVectorSizes.float3: f4.xyz = math.cross(negate(neg1, ((float3*)d1)[0]), negate(neg2, ((float3*)d2)[0])); break;
#else
                // in release build we dont need information on the second parameter 
                case BlastVectorSizes.float3: f4.xyz = math.cross(negate(neg1, ((float3*)d1)[0]), pop_as_f3(ref code_pointer)); break;
#endif

#if DEVELOPMENT_BUILD || TRACE
                default:
                    {
                        Debug.LogError($"cross: {datatype} vector size '{vector_size}' not supported ");
                        break;
                    }
#endif
            }

            code_pointer += 2;
        }
               
 
        /// <summary>
        /// fused multiply add 
        /// - 3 float params: m1 * m2 + a1
        /// </summary>
        /// <returns></returns>
#if !DEBUG
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        void get_fma_result(ref int code_pointer, ref byte vector_size, out float4 f4)
        {
            BlastVariableDataType datatype = default;
            f4 = float.NaN;

            // always 3 params, input vector size == output vectorsize

#if DEVELOPMENT_BUILD || TRACE
            BlastVariableDataType popped_datatype;
            byte popped_vector_size;
            bool neg1, neg2, neg3;

            code_pointer++;
            void* pm1 = pop_p_info(ref code_pointer, out datatype, out vector_size, out neg1);
            if (datatype == BlastVariableDataType.ID)
            {
                Debug.Log($"fma: ID datatype not yet supported, codepointer {code_pointer}, vectorsize: {vector_size}");
                return;
            }

            void* pm2 = pop_p_info(ref code_pointer, out popped_datatype, out popped_vector_size, out neg2);
            if (datatype != popped_datatype || vector_size != popped_vector_size)
            {
                Debug.Log($"fma: [datatype|vectorsize] mismatch, codepointer {code_pointer}, all parameters must share the same datatype, m1 = {datatype}.{vector_size} while m2 = {popped_datatype}.{popped_vector_size}");
                return;
            }

            void* pa1 = pop_p_info(ref code_pointer, out datatype, out vector_size, out neg3);
            if (datatype != popped_datatype || vector_size != popped_vector_size)
            {
                Debug.Log($"fma: [datatype|vectorsize] mismatch, codepointer {code_pointer}, all parameters must share the same datatype, m1 = {datatype}.{vector_size} while a1 = {popped_datatype}.{popped_vector_size}");
                return;
            }

            // in debug pop all with info and verify datatypes 
            switch ((BlastVectorSizes)vector_size)
            {
                case BlastVectorSizes.float1: f4.x = math.mad(negate(neg1, ((float*)pm1)[0]), negate(neg2, ((float*)pm2)[0]), negate(neg3, ((float*)pa1)[0])); break;
                case BlastVectorSizes.float2: f4.xy = math.mad(negate(neg1, ((float2*)pm1)[0]), negate(neg2, ((float2*)pm2)[0]), negate(neg3, ((float2*)pa1)[0])); break;
                case BlastVectorSizes.float3: f4.xyz = math.mad(negate(neg1, ((float3*)pm1)[0]), negate(neg2, ((float3*)pm2)[0]), negate(neg3, ((float3*)pa1)[0])); break;
                case 0:
                case BlastVectorSizes.float4:
                    f4 = math.mad(negate(neg1, ((float4*)pm1)[0]), negate(neg2, ((float4*)pm2)[0]), negate(neg3, ((float4*)pa1)[0]));
                    vector_size = 4;
                    break;
            }

#else
            code_pointer++;             

            // in release => pop once with info, others direct 
            bool is_negated; 
            void* pm1 = pop_p_info(ref code_pointer, out datatype, out vector_size, out is_negated);

            switch ((BlastVectorSizes)vector_size)
            {
                case BlastVectorSizes.float1: f4.x   = math.mad( negate(is_negated, ((float*)pm1)[0]),  pop_as_f1(ref code_pointer), pop_as_f1(ref code_pointer)); break;
                case BlastVectorSizes.float2: f4.xy  = math.mad( negate(is_negated, ((float2*)pm1)[0]), pop_as_f2(ref code_pointer), pop_as_f2(ref code_pointer)); break;
                case BlastVectorSizes.float3: f4.xyz = math.mad( negate(is_negated, ((float3*)pm1)[0]), pop_as_f3(ref code_pointer), pop_as_f3(ref code_pointer)); break;
                case 0:
                case BlastVectorSizes.float4:
                    f4 = math.mad(((float4*)pm1)[0], pop_as_f4(ref code_pointer), pop_as_f4(ref code_pointer));
                    vector_size = 4;
                    break;
            }

#endif
        }


 
         /// <summary>
        /// 3 inputs, first 2 any type, 3rd type equal or scalar bool
        /// select(a, b, condition)
        /// - input = outputvectorsize
        /// - 1 output  
        /// </summary>
        void get_select_result(ref int code_pointer, ref byte vector_size, out float4 f4)
        {
            BlastVariableDataType datatype, datatype2;
            BlastVariableDataType datatype_condition;
            byte vsize_condition, vsize2;
            bool neg1, neg2;


            code_pointer++;
            void* p1 = pop_p_info(ref code_pointer, out datatype, out vector_size, out neg1);

            // we cant skip popping the second with information on release builds because our stackpointer could stray
            void* p2 = pop_p_info(ref code_pointer, out datatype2, out vsize2, out neg2);

            void* pcondition = pop_p_info(ref code_pointer, out datatype_condition, out vsize_condition, out bool neg3);// dont care -1 = stil true

            // instead of init to NaN we init to float p1 (even if its wrong)
            f4 = ((float*)p1)[0];

#if DEVELOPMENT_BUILD || TRACE
            if (datatype != datatype2 || vector_size != vsize2)
            {
                Debug.LogError($"get_select_result: parameter type mismatch, p1 = {datatype}.{vector_size}, p2 = {datatype_condition}.{vsize_condition} at codepointer {code_pointer}");
                return;
            }
            if (datatype != BlastVariableDataType.Numeric)
            {
                Debug.LogError($"get_select_result: datatype mismatch, only numerics are supported at codepointer {code_pointer}");
                return;
            }
            if (vector_size != vsize_condition && vsize_condition != 1)
            {
                Debug.LogError($"get_select_result: condition parameter type mismatch, p1 = {datatype}.{vector_size}, p2 = {datatype_condition}.{vsize_condition} at codepointer {code_pointer}, vectorsizes must be equal or conditional must have size 1");
                return;
            }
#endif

            if (vsize_condition == 1)
            {
                switch ((BlastVectorSizes)vector_size)
                {
                    case 0:
                    case BlastVectorSizes.float4: f4 = math.select(negate(neg1, ((float4*)p1)[0]), negate(neg2, ((float4*)p2)[0]), ((float*)pcondition)[0] != 0); vector_size = 4; break;
                    case BlastVectorSizes.float3: f4.xyz = math.select(negate(neg1, ((float3*)p1)[0]), negate(neg2, ((float3*)p2)[0]), ((float*)pcondition)[0] != 0); break;
                    case BlastVectorSizes.float2: f4.xy = math.select(negate(neg1, ((float2*)p1)[0]), negate(neg2, ((float2*)p2)[0]), ((float*)pcondition)[0] != 0); break;
                    case BlastVectorSizes.float1: f4.x = math.select(negate(neg1, ((float*)p1)[0]), negate(neg2, ((float*)p2)[0]), ((float*)pcondition)[0] != 0); break;

                    default:
                        {
#if DEVELOPMENT_BUILD || TRACE
                            Debug.LogError($"select: vector size '{vector_size}' not supported ");
#endif
                            break;
                        }
                }
            }
            else
            {
                switch ((BlastVectorSizes)vector_size)
                {
                    case 0:
                    case BlastVectorSizes.float4: f4 = math.select(negate(neg1, ((float4*)p1)[0]), negate(neg2, ((float4*)p2)[0]), ((float4*)pcondition)[0] != 0); vector_size = 4; break;
                    case BlastVectorSizes.float3: f4.xyz = math.select(negate(neg1, ((float3*)p1)[0]), negate(neg2, ((float3*)p2)[0]), ((float3*)pcondition)[0] != 0); break;
                    case BlastVectorSizes.float2: f4.xy = math.select(negate(neg1, ((float2*)p1)[0]), negate(neg2, ((float2*)p2)[0]), ((float2*)pcondition)[0] != 0); break;
                    case BlastVectorSizes.float1: f4.x = math.select(negate(neg1, ((float*)p1)[0]), negate(neg2, ((float*)p2)[0]), ((float*)pcondition)[0] != 0); break;

                    default:
                        {
#if DEVELOPMENT_BUILD || TRACE
                            Debug.LogError($"select: vector size '{vector_size}' not supported ");
#endif
                            break;
                        }
                }
            }
        }

        /// <summary>
        /// clamp a value between 2 others: math.clamp(a, min, max) 
        /// - 3 parameters - equal vectorsize or min-max == 1 and a > 1
        /// - input = output vectorsize 
        /// </summary>
        void get_clamp_result(ref int code_pointer, ref byte vector_size, out float4 f4)
        {
            BlastVariableDataType datatype_a;
            f4 = float.NaN;
            byte vector_size_mm;
            BlastVariableDataType datatype_mm;
            bool neg1, neg2, neg3;

            code_pointer += 1;
            void* pa = pop_p_info(ref code_pointer, out datatype_a, out vector_size, out neg3);
            void* p1 = pop_p_info(ref code_pointer, out datatype_mm, out vector_size_mm, out neg1);
            void* p2 = pop_p_info(ref code_pointer, out datatype_a, out vector_size, out neg2);       // could optimize this to pop_fx in release but not much win

#if DEVELOPMENT_BUILD || TRACE

            if (datatype_mm != datatype_a || vector_size_mm != vector_size)
            {
                Debug.LogError($"get_clamp_result: parameter type mismatch, p1 = {datatype_mm}.{vector_size_mm}, p2 = {datatype_a}.{vector_size} at codepointer {code_pointer}");
                return;
            }
            if (datatype_mm != BlastVariableDataType.Numeric)
            {
                Debug.LogError($"get_clamp_result: datatype mismatch, only numerics are supported at codepointer {code_pointer}");
                return;
            }
#endif

#if DEVELOPMENT_BUILD || TRACE
            if (vector_size_mm != vector_size && vector_size_mm != 1)
            {
                Debug.LogError($"clamp: parameter type mismatch, min/max = {datatype_mm}.{vector_size_mm}, value = {datatype_a}.{vector_size} at codepointer {code_pointer}, vectorsizes must be equal or minmax must have size 1");
                return;
            }
#endif

            // vectorsize of minmax range => either equal or 1 
            if (vector_size_mm == 1)
            {
                switch ((BlastVectorSizes)vector_size)
                {
                    case 0:
                    case BlastVectorSizes.float4: f4 = math.clamp(negate(neg3, ((float4*)pa)[0]), negate(neg1, ((float*)p1)[0]), negate(neg2, ((float*)p2)[0])); vector_size_mm = 4; break;
                    case BlastVectorSizes.float3: f4.xyz = math.clamp(negate(neg3, ((float3*)pa)[0]), negate(neg1, ((float*)p1)[0]), negate(neg2, ((float*)p2)[0])); break;
                    case BlastVectorSizes.float2: f4.xy = math.clamp(negate(neg3, ((float2*)pa)[0]), negate(neg1, ((float*)p1)[0]), negate(neg2, ((float*)p2)[0])); break;
                    case BlastVectorSizes.float1: f4.x = math.clamp(negate(neg3, ((float*)pa)[0]), negate(neg1, ((float*)p1)[0]), negate(neg2, ((float*)p2)[0])); break;
                    default:
                        {
#if DEVELOPMENT_BUILD || TRACE
                            Debug.LogError($"clamp: value vector size '{vector_size}' not supported ");
#endif
                            break;
                        }
                }
            }
            else
            {
                switch ((BlastVectorSizes)vector_size)
                {
                    case 0:
                    case BlastVectorSizes.float4: f4 = math.clamp(negate(neg3, ((float4*)pa)[0]), negate(neg1, ((float4*)p1)[0]), negate(neg2, ((float4*)p2)[0])); vector_size_mm = 4; break;
                    case BlastVectorSizes.float3: f4.xyz = math.clamp(negate(neg3, ((float3*)pa)[0]), negate(neg1, ((float3*)p1)[0]), negate(neg2, ((float3*)p2)[0])); break;
                    case BlastVectorSizes.float2: f4.xy = math.clamp(negate(neg3, ((float2*)pa)[0]), negate(neg1, ((float2*)p1)[0]), negate(neg2, ((float2*)p2)[0])); break;
                    case BlastVectorSizes.float1: f4.x = math.clamp(negate(neg3, ((float*)pa)[0]), negate(neg1, ((float*)p1)[0]), negate(neg2, ((float*)p2)[0])); break;

                    default:
                        {
#if DEVELOPMENT_BUILD || TRACE
                            Debug.LogError($"clamp: value vector size '{vector_size}' not supported ");
#endif
                            break;
                        }
                }
            }
        }

        /// <summary>
        /// linear interpolation from a to b at step c 
        /// 
        /// - 3 inputs:   3 vectors equal size or first 2 equal and last = 1 
        /// - only numeric input
        /// 
        /// - lerp((1 2), (10 20), 0.5); 
        /// - lerp((1 2), (10 20), (0.5 0.1)); 
        /// 
        /// returns vector size of input values 
        /// </summary>
        void get_lerp_result(ref int code_pointer, ref byte vector_size, out float4 f4)
        {
            BlastVariableDataType p1_datatype;
            BlastVariableDataType p2_datatype; byte p2_vector_size;
            BlastVariableDataType p3_datatype; byte p3_vector_size;
            bool neg1, neg2, neg3;

            code_pointer++;
            void* p1 = pop_p_info(ref code_pointer, out p1_datatype, out vector_size, out neg1);
            void* p2 = pop_p_info(ref code_pointer, out p2_datatype, out p2_vector_size, out neg2);
            void* p3 = pop_p_info(ref code_pointer, out p3_datatype, out p3_vector_size, out neg3);

            f4 = float.NaN;

#if DEVELOPMENT_BUILD || TRACE
            if (p1_datatype != p2_datatype || vector_size != p2_vector_size)
            {
                Debug.LogError($"lerp: parameter type mismatch, min = {p1_datatype}.{vector_size}, max = {p2_datatype}.{p2_vector_size} at codepointer {code_pointer}, min/max vectorsizes must be equal");
                return;
            }
            if (p1_datatype != BlastVariableDataType.Numeric)
            {
                Debug.LogError($"lerp: ID datatype not supported");
                return;
            }
            if (vector_size != p3_vector_size && p3_vector_size != 1)
            {
                Debug.LogError($"lerp: parameter type mismatch, min/max = {p1_datatype}.{vector_size}, c = {p3_datatype}.{p3_vector_size} at codepointer {code_pointer}, if position is not a scalar then min/max vectorsize must be equal to position vectorsize");
                return;
            }
#endif

            if (p3_vector_size == 1)
            {
                switch ((BlastVectorSizes)vector_size)
                {
                    case 0:
                    case BlastVectorSizes.float4: f4 = math.lerp(negate(neg1, ((float4*)p1)[0]), negate(neg2, ((float4*)p2)[0]), negate(neg3, ((float*)p3)[0])); vector_size = 4; break;
                    case BlastVectorSizes.float1: f4.x = math.lerp(negate(neg1, ((float*)p1)[0]), negate(neg2, ((float*)p2)[0]), negate(neg3, ((float*)p3)[0])); break;
                    case BlastVectorSizes.float2: f4.xy = math.lerp(negate(neg1, ((float2*)p1)[0]), negate(neg2, ((float2*)p2)[0]), negate(neg3, ((float*)p3)[0])); break;
                    case BlastVectorSizes.float3: f4.xyz = math.lerp(negate(neg1, ((float3*)p1)[0]), negate(neg2, ((float3*)p2)[0]), negate(neg3, ((float*)p3)[0])); break;
#if DEVELOPMENT_BUILD || TRACE
                    default:
                        Debug.LogError($"lerp: vector_size {vector_size} not supported");
                        break;
#endif
                }
            }
            else
            {
                switch ((BlastVectorSizes)vector_size)
                {
                    case 0:
                    case BlastVectorSizes.float4: f4 = math.lerp(negate(neg1, ((float4*)p1)[0]), negate(neg2, ((float4*)p2)[0]), negate(neg3, ((float4*)p3)[0])); vector_size = 4; break;
                    case BlastVectorSizes.float1: f4.x = math.lerp(negate(neg1, ((float*)p1)[0]), negate(neg2, ((float*)p2)[0]), negate(neg3, ((float*)p3)[0])); break;
                    case BlastVectorSizes.float2: f4.xy = math.lerp(negate(neg1, ((float2*)p1)[0]), negate(neg2, ((float2*)p2)[0]), negate(neg3, ((float2*)p3)[0])); break;
                    case BlastVectorSizes.float3: f4.xyz = math.lerp(negate(neg1, ((float3*)p1)[0]), negate(neg2, ((float3*)p2)[0]), negate(neg3, ((float3*)p3)[0])); break;
#if DEVELOPMENT_BUILD || TRACE
                    default:
                        Debug.LogError($"lerp: vector_size {vector_size} not supported");
                        break;
#endif
                }
            }
        }

        /// <summary>
        /// spherical linear interpolation 
        /// - 2 quaternion + float in
        /// returns float4/quaternion
        /// </summary>
        void get_slerp_result(ref int code_pointer, ref byte vector_size, out float4 f4)
        {
            BlastVariableDataType p1_datatype;
            BlastVariableDataType p2_datatype; byte p2_vector_size;
            BlastVariableDataType p3_datatype; byte p3_vector_size;
            bool neg1, neg2, neg3;

            code_pointer++;
            void* p1 = pop_p_info(ref code_pointer, out p1_datatype, out vector_size, out neg1);
            void* p2 = pop_p_info(ref code_pointer, out p2_datatype, out p2_vector_size, out neg2);
            void* p3 = pop_p_info(ref code_pointer, out p3_datatype, out p3_vector_size, out neg3);

            f4 = float.NaN;

#if DEVELOPMENT_BUILD || TRACE
            if (p1_datatype != p2_datatype || vector_size != p2_vector_size)
            {
                Debug.LogError($"slerp: parameter type mismatch, min = {p1_datatype}.{vector_size}, max = {p2_datatype}.{p2_vector_size} at codepointer {code_pointer}, only float4/quaternions are supported in slerp");
                return;
            }
            if (p1_datatype != BlastVariableDataType.Numeric)
            {
                Debug.LogError($"slerp: ID datatype not supported");
                return;
            }
            if (vector_size != p3_vector_size && p3_vector_size != 1)
            {
                Debug.LogError($"slerp: parameter type mismatch, min/max = {p1_datatype}.{vector_size}, c = {p3_datatype}.{p3_vector_size} at codepointer {code_pointer}, if position is not a scalar then min/max vectorsize must be equal to position vectorsize");
                return;
            }
            if (neg1 || neg2)
            {
                Debug.LogError($"slerp: negation of quaternions, min/max = {p1_datatype}.{vector_size}, c = {p3_datatype}.{p3_vector_size} at codepointer {code_pointer}, if position is not a scalar then min/max vectorsize must be equal to position vectorsize");
                return;
            }

#endif

            if (p3_vector_size == 1)
            {
                switch ((BlastVectorSizes)vector_size)
                {
                    case 0:
                    case BlastVectorSizes.float4: f4 = math.slerp(((quaternion*)p1)[0], ((quaternion*)p2)[0], negate(neg3, ((float*)p3)[0])).value; vector_size = 4; break;
#if DEVELOPMENT_BUILD || TRACE
                    default:
                        Debug.LogError($"slerp: vector_size {vector_size} not supported");
                        break;
#endif
                }
            }
            else
            {
                switch ((BlastVectorSizes)vector_size)
                {
                    case 0:
                    case BlastVectorSizes.float4: f4 = math.slerp(((quaternion*)p1)[0], ((quaternion*)p2)[0], negate(neg3, ((float*)p3)[0])).value; vector_size = 4; break;
#if DEVELOPMENT_BUILD || TRACE
                    default:
                        Debug.LogError($"slerp: vector_size {vector_size} not supported");
                        break;
#endif
                }
            }
        }

        /// <summary>
        /// normalized linear interpolation 
        /// - 2 quaternion + float in
        /// returns float4/quaternion
        /// </summary>
        void get_nlerp_result(ref int code_pointer, ref byte vector_size, out float4 f4)
        {
            BlastVariableDataType p1_datatype;
            BlastVariableDataType p2_datatype; byte p2_vector_size;
            BlastVariableDataType p3_datatype; byte p3_vector_size;
            bool neg1, neg2, neg3;

            code_pointer++;
            void* p1 = pop_p_info(ref code_pointer, out p1_datatype, out vector_size, out neg1);
            void* p2 = pop_p_info(ref code_pointer, out p2_datatype, out p2_vector_size, out neg2);
            void* p3 = pop_p_info(ref code_pointer, out p3_datatype, out p3_vector_size, out neg3);

            f4 = float.NaN;

#if DEVELOPMENT_BUILD || TRACE
            if (p1_datatype != p2_datatype || vector_size != p2_vector_size)
            {
                Debug.LogError($"nlerp: parameter type mismatch, min = {p1_datatype}.{vector_size}, max = {p2_datatype}.{p2_vector_size} at codepointer {code_pointer}, only float4/quaternions are supported in slerp");
                return;
            }
            if (p1_datatype != BlastVariableDataType.Numeric)
            {
                Debug.LogError($"nlerp: ID datatype not supported");
                return;
            }
            if (vector_size != p3_vector_size && p3_vector_size != 1)
            {
                Debug.LogError($"nlerp: parameter type mismatch, min/max = {p1_datatype}.{vector_size}, c = {p3_datatype}.{p3_vector_size} at codepointer {code_pointer}, if position is not a scalar then min/max vectorsize must be equal to position vectorsize");
                return;
            }
#endif

            if (p3_vector_size == 1)
            {
                switch ((BlastVectorSizes)vector_size)
                {
                    case 0:
                    case BlastVectorSizes.float4: f4 = math.nlerp(((quaternion*)p1)[0], ((quaternion*)p2)[0], ((float*)p3)[0]).value; vector_size = 4; break;
#if DEVELOPMENT_BUILD || TRACE
                    default:
                        Debug.LogError($"nlerp: vector_size {vector_size} not supported");
                        break;
#endif
                }
            }
            else
            {
                switch ((BlastVectorSizes)vector_size)
                {
                    case 0:
                    case BlastVectorSizes.float4: f4 = math.nlerp(((quaternion*)p1)[0], ((quaternion*)p2)[0], ((float*)p3)[0]).value; vector_size = 4; break;
#if DEVELOPMENT_BUILD || TRACE
                    default:
                        Debug.LogError($"nlerp: vector_size {vector_size} not supported");
                        break;
#endif
                }
            }
        }



        /// <summary>
        /// normalize between minmax -> undo lerp
        /// 
        /// - 3 inputs:   3 vectors equal size or first 2 equal and last = 1 
        /// - only numeric input
        /// 
        /// - ulerp((1 2), (10 20), 0.5); 
        /// - ulerp((1 2), (10 20), (0.5 0.1)); 
        /// 
        /// returns vector size of input values 
        /// </summary>
        void get_unlerp_result(ref int code_pointer, ref byte vector_size, out float4 f4)
        {
            BlastVariableDataType p1_datatype;
            BlastVariableDataType p2_datatype; byte p2_vector_size;
            BlastVariableDataType p3_datatype; byte p3_vector_size;
            bool neg1, neg2, neg3;

            code_pointer++;
            void* p1 = pop_p_info(ref code_pointer, out p1_datatype, out vector_size, out neg1);
            void* p2 = pop_p_info(ref code_pointer, out p2_datatype, out p2_vector_size, out neg2);
            void* p3 = pop_p_info(ref code_pointer, out p3_datatype, out p3_vector_size, out neg3);

            f4 = float.NaN;

#if DEVELOPMENT_BUILD || TRACE
            if (p1_datatype != p2_datatype || vector_size != p2_vector_size)
            {
                Debug.LogError($"unlerp: parameter type mismatch, min = {p1_datatype}.{vector_size}, max = {p2_datatype}.{p2_vector_size} at codepointer {code_pointer}, min/max vectorsizes must be equal");
                return;
            }
            if (p1_datatype != BlastVariableDataType.Numeric)
            {
                Debug.LogError($"unlerp: ID datatype not supported");
                return;
            }
            if (vector_size != p3_vector_size && p3_vector_size != 1)
            {
                Debug.LogError($"unlerp: parameter type mismatch, min/max = {p1_datatype}.{vector_size}, c = {p3_datatype}.{p3_vector_size} at codepointer {code_pointer}, if position is not a scalar then min/max vectorsize must be equal to position vectorsize");
                return;
            }
#endif

            if (p3_vector_size == 1)
            {
                switch ((BlastVectorSizes)vector_size)
                {
                    case 0:
                    case BlastVectorSizes.float4: f4 = math.unlerp(negate(neg1, ((float4*)p1)[0]), negate(neg2, ((float4*)p2)[0]), negate(neg3, ((float*)p3)[0])); vector_size = 4; break;
                    case BlastVectorSizes.float1: f4.x = math.unlerp(negate(neg1, ((float*)p1)[0]), negate(neg2, ((float*)p2)[0]), negate(neg3, ((float*)p3)[0])); break;
                    case BlastVectorSizes.float2: f4.xy = math.unlerp(negate(neg1, ((float2*)p1)[0]), negate(neg2, ((float2*)p2)[0]), negate(neg3, ((float*)p3)[0])); break;
                    case BlastVectorSizes.float3: f4.xyz = math.unlerp(negate(neg1, ((float3*)p1)[0]), negate(neg2, ((float3*)p2)[0]), negate(neg3, ((float*)p3)[0])); break;
#if DEVELOPMENT_BUILD || TRACE
                    default:
                        Debug.LogError($"unlerp: vector_size {vector_size} not supported");
                        break;
#endif
                }
            }
            else
            {
                switch ((BlastVectorSizes)vector_size)
                {
                    case 0:
                    case BlastVectorSizes.float4: f4 = math.unlerp(negate(neg1, ((float4*)p1)[0]), negate(neg2, ((float4*)p2)[0]), negate(neg3, ((float4*)p3)[0])); vector_size = 4; break;
                    case BlastVectorSizes.float1: f4.x = math.unlerp(negate(neg1, ((float*)p1)[0]), negate(neg2, ((float*)p2)[0]), negate(neg3, ((float*)p3)[0])); break;
                    case BlastVectorSizes.float2: f4.xy = math.unlerp(negate(neg1, ((float2*)p1)[0]), negate(neg2, ((float2*)p2)[0]), negate(neg3, ((float2*)p3)[0])); break;
                    case BlastVectorSizes.float3: f4.xyz = math.unlerp(negate(neg1, ((float3*)p1)[0]), negate(neg2, ((float3*)p2)[0]), negate(neg3, ((float3*)p3)[0])); break;
#if DEVELOPMENT_BUILD || TRACE
                    default:
                        Debug.LogError($"unlerp: vector_size {vector_size} not supported");
                        break;
#endif
                }
            }
        }


        /// <summary>
        /// math.remap - remap(a, b, c, d, e)   => remap e from range ab to cd
        /// </summary>
        void get_remap_result(ref int code_pointer, ref byte vector_size, out float4 f4)
        {
            BlastVariableDataType p1_datatype;
            BlastVariableDataType p2_datatype; byte p2_vector_size;
            BlastVariableDataType p3_datatype; byte p3_vector_size;
            BlastVariableDataType p4_datatype; byte p4_vector_size;
            BlastVariableDataType p5_datatype; byte p5_vector_size;
            bool neg1, neg2, neg3, neg4, neg5;

            code_pointer++;
            void* p1 = pop_p_info(ref code_pointer, out p1_datatype, out vector_size, out neg1);
            void* p2 = pop_p_info(ref code_pointer, out p2_datatype, out p2_vector_size, out neg2);
            void* p3 = pop_p_info(ref code_pointer, out p3_datatype, out p3_vector_size, out neg3);
            void* p4 = pop_p_info(ref code_pointer, out p4_datatype, out p4_vector_size, out neg4);
            void* p5 = pop_p_info(ref code_pointer, out p5_datatype, out p5_vector_size, out neg5);

            f4 = float.NaN;

#if DEVELOPMENT_BUILD || TRACE
            if (p1_datatype != p2_datatype || vector_size != p2_vector_size)
            {
                Debug.LogError($"last.interpretor.remap: parameter type mismatch, min = {p1_datatype}.{vector_size}, max = {p2_datatype}.{p2_vector_size} at codepointer {code_pointer}, min/max vectorsizes must be equal");
                return;
            }
            if (p1_datatype != BlastVariableDataType.Numeric)
            {
                Debug.LogError($"last.interpretor.remap: ID datatype not supported");
                return;
            }
            if (vector_size != p3_vector_size)
            {
                Debug.LogError($"last.interpretor.remap: parameter type mismatch, min/max = {p1_datatype}.{vector_size}, c = {p3_datatype}.{p3_vector_size} at codepointer {code_pointer}, if position is not a scalar then min/max vectorsize must be equal to position vectorsize");
                return;
            }
            if (vector_size != p4_vector_size)
            {
                Debug.LogError($"last.interpretor.remap: parameter type mismatch, min/max = {p1_datatype}.{vector_size}, d = {p4_datatype}.{p4_vector_size} at codepointer {code_pointer}, if position is not a scalar then min/max vectorsize must be equal to position vectorsize");
                return;
            }
            if (vector_size != p5_vector_size)
            {
                Debug.LogError($"blast.interpretor.remap: parameter type mismatch, min/max = {p1_datatype}.{vector_size}, e = {p5_datatype}.{p5_vector_size} at codepointer {code_pointer}, if position is not a scalar then min/max vectorsize must be equal to position vectorsize");
                return;
            }
#endif

            switch ((BlastVectorSizes)vector_size)
            {
                case 0:
                case BlastVectorSizes.float4: f4 = math.remap(negate(neg1, ((float4*)p1)[0]), negate(neg2, ((float4*)p2)[0]), negate(neg3, ((float4*)p3)[0]), negate(neg4, ((float4*)p4)[0]), negate(neg5, ((float4*)p5)[0])); vector_size = 4; break;
                case BlastVectorSizes.float1: f4.x = math.remap(negate(neg1, ((float*)p1)[0]), negate(neg2, ((float*)p2)[0]), negate(neg3, ((float*)p3)[0]), negate(neg4, ((float*)p4)[0]), negate(neg5, ((float*)p5)[0])); break;
                case BlastVectorSizes.float2: f4.xy = math.remap(negate(neg1, ((float2*)p1)[0]), negate(neg2, ((float2*)p2)[0]), negate(neg3, ((float2*)p3)[0]), negate(neg4, ((float2*)p4)[0]), negate(neg5, ((float2*)p5)[0])); break;
                case BlastVectorSizes.float3: f4.xyz = math.remap(negate(neg1, ((float3*)p1)[0]), negate(neg2, ((float3*)p2)[0]), negate(neg3, ((float3*)p3)[0]), negate(neg4, ((float3*)p5)[0]), negate(neg5, ((float3*)p5)[0])); break;
#if DEVELOPMENT_BUILD || TRACE
                default:
                    Debug.LogError($"blast.interpretor.remap: vector_size {vector_size} not supported");
                    break;
#endif
            }
        }

        #endregion

        #region Reinterpret XXXX [DataIndex]

        /// <summary>
        /// reinterpret the value at index as a boolean value (set metadata type to bool32)
        /// </summary> 
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void reinterpret_bool32(ref int code_pointer, ref byte vector_size)
        {
            // index would be the same for each ssmd record

            //
            // - packager messes up with prefilling constant data 
            // 

            code_pointer++;
            int dataindex = code[code_pointer] - BlastInterpretor.opt_id;
            code_pointer++;

#if DEVELOPMENT_BUILD || TRACE
            // dataindex must point to a valid id 
            if (dataindex < 0 || dataindex + BlastInterpretor.opt_id >= 255)
            {
                Debug.LogError($"reinterpret_bool32: first parameter must directly point to a dataindex but its '{dataindex + BlastInterpretor.opt_id}' instead");
                return;
            }

            // validate if within package bounds, catch packaging errors early on 
            // datasize is in bytes 
            if (dataindex >= package.DataSize >> 2)
            {
                Debug.LogError($"reinterpret_bool32: compilation or packaging error, dataindex {dataindex} is out of bounds");
                return;
            }
#else
            vector_size = 1;
#endif

            BlastInterpretor.SetMetaData(in metadata, BlastVariableDataType.Bool32, 1, (byte)dataindex);
        }

        /// <summary>
        /// reinterpret the value at index as a float value (set metadata type to float[1])
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void reinterpret_float(ref int code_pointer, ref byte vector_size)
        {
            // index would be the same for each ssmd record
            code_pointer++;
            int dataindex = code[code_pointer] - BlastInterpretor.opt_id;
            code_pointer++;

#if DEVELOPMENT_BUILD || TRACE
            // dataindex must point to a valid id 
            if (dataindex < 0 || dataindex + BlastInterpretor.opt_id >= 255)
            {
                Debug.LogError($"reinterpret_float: first parameter must directly point to a dataindex but its '{dataindex + BlastInterpretor.opt_id}' instead");
                return;
            }

            // validate if within package bounds, catch packaging errors early on 
            // datasize is in bytes 
            if (dataindex >= package.DataSize >> 2)
            {
                Debug.LogError($"reinterpret_float: compilation or packaging error, dataindex {dataindex} is out of bounds");
                return;
            }
#else
            vector_size = 1;
#endif

            BlastInterpretor.SetMetaData(in metadata, BlastVariableDataType.Numeric, 1, (byte)dataindex);
        }

        /// <summary>
        /// reinterpret the value at index as a float value (set metadata type to float[1])
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void reinterpret_int32(ref int code_pointer, ref byte vector_size)
        {
            // index would be the same for each ssmd record
            code_pointer++;
            int dataindex = code[code_pointer] - BlastInterpretor.opt_id;
            code_pointer++;

#if DEVELOPMENT_BUILD || TRACE
            // dataindex must point to a valid id 
            if (dataindex < 0 || dataindex + BlastInterpretor.opt_id >= 255)
            {
                Debug.LogError($"reinterpret_int32: first parameter must directly point to a dataindex but its '{dataindex + BlastInterpretor.opt_id}' instead");
                return;
            }

            // validate if within package bounds, catch packaging errors early on 
            // datasize is in bytes 
            if (dataindex >= package.DataSize >> 2)
            {
                Debug.LogError($"reinterpret_int32: compilation or packaging error, dataindex {dataindex} is out of bounds");
                return;
            }
#else
            vector_size = 1;
#endif

            BlastInterpretor.SetMetaData(in metadata, BlastVariableDataType.ID, 1, (byte)dataindex);
        }

        #endregion

        #region validation fuction 

        void RunValidate(ref int code_pointer)
        {
            // read the 2 parameters, we can use temp and register
            BlastVariableDataType type1;
            byte size1;
            bool neg1;

            code_pointer++;

            void* p1 = pop_p_info(ref code_pointer, out type1, out size1, out neg1);


            switch (type1)
            {
                case BlastVariableDataType.Bool32:
                    {
                        // dont care about type, bool looks at the bit pattern 
                        uint b1 = ((uint*)p1)[0];

                        uint b2 = pop_b32(ref code_pointer).Unsigned; 

                        b1 = math.select(b1, ~b1, neg1);
                        
                        if (b2 != b1)
                        {
                            if (IsTrace)
                            {
                                Debug.LogError($"blast.validate: validation error at codepointer {code_pointer}, bool32: {b1} != {b2}");
                            }
                        }
                    }
                    return;

                case BlastVariableDataType.ID:

                    switch (size1)
                    {
                        case 1:
                            {
                                int i1 = CodeUtils.ReInterpret<float, int>(((float*)p1)[0]);
                                int i2 = pop_as_i1(ref code_pointer);
                                if (i2 != i1)
                                {
                                    if (IsTrace)
                                    {
                                        Debug.LogError($"blast.validate: validation error at codepointer {code_pointer}, int1: {i1} != {i2}");
                                    }
                                }
                            }
                            return;

                        case 0:
                        case 4:
                            {
                                int4 i41 = CodeUtils.ReInterpret<float4, int4>(((float4*)p1)[0]);
                                int4 i42 = pop_as_i4(ref code_pointer);
                                if (math.any(i42 != i41))
                                {
                                    if (IsTrace)
                                    {
                                        Debug.LogError($"blast.validate: validation error at codepointer {code_pointer}, int4: {i41} != {i42}");
                                    }
                                }
                            }
                            return;

                        case 3:
                            {
                                int3 i31 = CodeUtils.ReInterpret<float3, int3>(((float3*)p1)[0]);
                                int3 i32 = pop_as_i3(ref code_pointer);
                                if (math.any(i32 != i31))
                                {
                                    if (IsTrace)
                                    {
                                        Debug.LogError($"blast.validate: validation error at codepointer {code_pointer}, int3: {i31} != {i32}");
                                    }
                                }
                            }
                            return;

                        case 2:
                            {
                                int2 i21 = CodeUtils.ReInterpret<float2, int2>(((float2*)p1)[0]);
                                int2 i22 = pop_as_i2(ref code_pointer);
                                if (math.any(i22 != i21))
                                {
                                    if (IsTrace)
                                    {
                                        Debug.LogError($"blast.validate: validation error at codepointer {code_pointer}, int2: {i21} != {i22}");
                                    }
                                }
                            }
                            return;
                    }
                    break; 

                case BlastVariableDataType.Numeric:
                    switch (size1)
                    {
                        case 1:
                            {
                                float f1 = ((float*)p1)[0];
                                float f2 = pop_as_f1(ref code_pointer); 
                                f1 = math.select(f1, -f1, neg1);
                                if (f2 != f1)
                                {
                                    if (IsTrace)
                                    {
                                        Debug.LogError($"blast.validate: validation error at codepointer {code_pointer}, float1: {f1} != {f2}");
                                    }
                                }
                            }
                            return;

                        case 2:
                            {
                                float2 f21 = ((float2*)p1)[0];
                                float2 f22 = pop_as_f2(ref code_pointer);
                                f21 = math.select(f21, -f21, neg1);
                                if (math.any(f22 != f21))
                                {
                                    if (IsTrace)
                                    {
                                        Debug.LogError($"blast.validate: validation error at codepointer {code_pointer}, float2: {f21} != {f22}");
                                    }
                                }
                            }
                            return;

                        case 3:
                            {
                                float3 f31 = ((float3*)p1)[0];
                                float3 f32 = pop_as_f3(ref code_pointer);
                                f31 = math.select(f31, -f31, neg1);
                                if (math.any(f32 != f31))
                                {
                                    if (IsTrace)
                                    {
                                        Debug.LogError($"blast.validate: validation error at codepointer {code_pointer}, float3: {f31} != {f32}");
                                    }
                                }
                            }
                            return;

                        case 0:
                        case 4:
                            {
                                float4 f41 = ((float4*)p1)[0];
                                float4 f42 = pop_as_f4(ref code_pointer); 
                                f41 = math.select(f41, -f41, neg1);
                                if (math.any(f42 != f41))
                                {                     
                                    Debug.LogError($"blast.validate: validation error at codepointer {code_pointer}, float4: {f41} != {f42}");
                                }
                            }
                            return;
                    }
                    break;
            }

            if (IsTrace)
            {
                Debug.LogError($"blast.validate: datatype {type1} vectorsize {size1} not supported at codepointer {code_pointer}");
            }
        }

        #endregion

        #region Send Procedure

        void Handle_Send(ref int code_pointer)
        {
            // check out, this might get called with cdataref  
            code_pointer++; 
            blast_operation op = (blast_operation)code[code_pointer];
            if (op == blast_operation.cdataref)
            {
                if (!ValidateOnce)
                {
                    int jump_offset = (code[code_pointer + 1] << 8) + code[code_pointer + 2];

                    int index = code_pointer - jump_offset + 1;
                    code_pointer += 3;

                    op = (blast_operation)code[index];

                    // should be cdata TODO validate in debug 

                    int length = (code[index + 1] << 8) + code[index + 2];
                    int data_start = index + 3;

                    // 'send' data - to be used as an event handler - now connected to log
                    //
                    // send data to some sink 
                    // 
                    // for now its just logged as if ascii 8bit

                    // 16 bit chars.. 
                    LogConstantCData(length, data_start);
                }
                else
                {
                    code_pointer += 3;
                }
            }
            else                                    
            {
                byte vector_size;
                BlastVariableDataType type;
                bool negate;

                // regardless validation state, reaching here means pop it for consistancy
                void* pdata = pop_p_info(ref code_pointer, out type, out vector_size, out negate);

                //
                if (!ValidateOnce)
                {
                    // normal parameters 

                    switch (type)
                    {
                        // bool32 flags  
                        case BlastVariableDataType.Bool32:
                            {
                                Bool32 b32 = ((Bool32*)pdata)[0];
#if STANDALONE_VSBUILD
                                Debug.Log(b32.ToString());
#else
                                // bursted string magic 
                                // or very rude code.. 
                                // Debug.Log($"{(b32.b1 ? 1 : 0)}{(b32.b2 ? 1 : 0)}{(b32.b3 ? 1 : 0)}{(b32.b4 ? 1 : 0)}"); 
                                Debug.Log("BOOL32"); 
#endif
                            }
                            break;

                        // variable cdata segment 
                        case BlastVariableDataType.CData:
                            {
                                // jump to it and outoput 
                                Debug.Log("CDATA indatasegment ");

                            }
                            break;

                        // numerics 
                        case BlastVariableDataType.Numeric:
                            {
                                switch (vector_size)
                                {
                                    case 0:
                                    case 4: Debug.Log($"{((float4*)pdata)[0]}"); break;
                                    case 3: Debug.Log($"{((float3*)pdata)[0]}"); break;
                                    case 2: Debug.Log($"{((float2*)pdata)[0]}"); break;
                                    case 1: Debug.Log($"{((float*)pdata)[0]}"); break;
#if TRACE || DEVELOPMENT_BUILD
                                    default:
                                        Debug.LogError($"Blast.Interpretor.Send: unsupported datatype: {type}, vectorsize: {vector_size} at codepointer: {code_pointer}");
                                        break;
#endif
                                }
                            }
                            break;

#if TRACE || DEVELOPMENT_BUILD
                        default:
                            Debug.LogError($"Blast.Interpretor.Send: unsupported datatype: {type} at codepointer: {code_pointer}");
                            break;
#endif

                    }
                }
                else
                {


                }
            }
            return;
        }

        /// <summary>
        /// log constant cdata encoded in the code stream
        /// </summary>
        /// <param name="length">length of data stream in bytes</param>
        /// <param name="data_start">start index of data in code segment</param>
        private readonly void LogConstantCData(int length, int data_start)
        {
            unsafe
            {
                char* ch = stackalloc char[length + 1];
                bool ascii = true;

                for (int i = data_start, c = 0; i < data_start + length; i++, c++)
                {
                    // set from 8 bit...
                    ch[c] = (char)code[i];
                    ascii = ascii && (code[i] >= 32 && code[i] < 127); // (char.IsLetterOrDigit(ch[i]) || char.IsWhiteSpace(ch[i]));
                }

#if STANDALONE_VSBUILD
                if (ascii)
                {
                    // zero terminate and show as string 
                    ch[length] = (char)0;
                    Debug.Log(new string(ch));
                }
                else
                {
                    // show as bytes 
                    System.Text.StringBuilder sb = StringBuilderCache.Acquire();
                    for (int i = data_start; i < data_start + length; i++)
                    {
                        sb.Append(code[i].ToString().PadLeft(3, '0'));
                        sb.Append(" ");
                    }
                    Debug.Log(StringBuilderCache.GetStringAndRelease(ref sb));
                }
#else
                // burst compatible string magic to show it in debugger as burst does not support char 
#endif

            }
        }

        #endregion
   
        #region Push[cfv]

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void pushc(ref int code_pointer, ref byte vector_size, ref BlastVariableDataType datatype, ref V4 f4_register)
        {
            get_sequence_result(ref code_pointer, ref vector_size, ref datatype, ref f4_register);
            switch (vector_size)
            {
                case 1: push(f4_register.x, datatype); break;
                case 2: push(f4_register.xy, datatype); break;
                case 3: push(f4_register.xyz, datatype); break;
                case 0:
                case 4: push(f4_register.xyzw, datatype); break;
#if DEVELOPMENT_BUILD || TRACE
                default:
                    Debug.LogError("blastscript.interpretor.pushc error: variable vector size not yet supported on stack push");
                    break;
#endif
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void push(ref int code_pointer, ref byte vector_size, ref BlastVariableDataType datatype, ref float4 f4_register)
        {
            if (code[code_pointer] == (byte)blast_operation.begin)
            {
                code_pointer++;

                // push the vector result of a compound 
                get_sequence_result(ref code_pointer, ref vector_size, ref datatype, ref f4_register);
                switch (vector_size)
                {
                    case 1:
                        push(f4_register.x, datatype);
                        break;
                    case 2:
                        push(f4_register.xy, datatype);
                        break;
                    case 3:
                        push(f4_register.xyz, datatype);
                        break;
                    case 4:
                        push(f4_register, datatype);
                        break;
#if DEVELOPMENT_BUILD || TRACE
                    default:
                        Debug.LogError("burstscript.interpretor error: variable vector size not yet supported on stack push of compound");
                        break;
#endif
                }
                // code_pointer should index after compound now
            }
            else
            {
                // else push 1 element referced by id from datasegment 
                V1* fdata = (V1*)stack;
                push(fdata[code[code_pointer++] - opt_id]);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void pushv(ref int code_pointer, ref byte vector_size, ref BlastVariableDataType datatype, ref V4 f4_register)
        {
            byte param_count = code[code_pointer++];

            param_count = decode44(param_count, ref vector_size);

            if (param_count == 1)
            {
                switch (vector_size)
                {
                    case 1:
                        push(pop_f1(ref code_pointer, ref datatype), in datatype);
                        break;
                    case 2:
                        push(pop_f2(ref code_pointer, ref datatype), in datatype);
                        break;
                    case 3:
                        push(pop_f3(ref code_pointer, ref datatype), in datatype);
                        break;
                    case 4:
                        push(pop_f4(ref code_pointer, ref datatype), in datatype);
                        break;
#if DEVELOPMENT_BUILD || TRACE
                    default:
                        Debug.LogError($"blastscript.interpretor error: vectorsize {vector_size} not yet supported on stack push");
                        break;
#endif
                        // return (int)BlastError.error_vector_size_not_supported;
                }
            }
            else
            {
#if DEVELOPMENT_BUILD || TRACE
                if (param_count != vector_size)
                {
                    Debug.LogError($"blast.interpretor error: codepointer {code_pointer}  pushv => param_count > 1 && vector_size != param_count, this is not allowed. Vectorsize: {vector_size}, Paramcount: {param_count}");
                    // return (int)BlastError.error_unsupported_operation;
                }
#endif
                // param_count == vector_size and vector_size > 1, compiler enforces it so we should not check it in release 
                // this means that vector is build from multiple data points
                switch (vector_size)
                {
                    case 1:
                        push(pop_f1(ref code_pointer, ref datatype)); 
                        break;
                    case 2:
                        push(new float2(pop_f1(ref code_pointer, ref datatype), pop_f1(ref code_pointer, ref datatype)), in datatype);
                        break;
                    case 3:
                        push(new float3(pop_f1(ref code_pointer, ref datatype), pop_f1(ref code_pointer, ref datatype), pop_f1(ref code_pointer, ref datatype)), in datatype);
                        break;
                    case 4:
                        push(new float4(pop_f1(ref code_pointer, ref datatype), pop_f1(ref code_pointer, ref datatype), pop_f1(ref code_pointer, ref datatype), pop_f1(ref code_pointer, ref datatype)), in datatype);
                        break;
                }
                // we could handle the case of different sized vectors, pushing them into 1 of vectorsize...
                // - currently the compiler wont do this and we catch this as an error in debug builds some lines back
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void pushf(ref int code_pointer, ref byte vector_size, ref BlastVariableDataType datatype, ref float4 f4_register)
        {
            get_function_result(ref code_pointer, ref vector_size, ref datatype, ref f4_register);
            code_pointer++;


            // combine datatype with vectorsize in a byte sized enum: 
            // - supporting additional datatypes|vectorsizes wont cost any performance 
            // - il2cpp|burst will generate a jump table
            switch (datatype.Combine(vector_size, false))
            {
                case BlastVectorType.float1: push(f4_register.x, BlastVariableDataType.Numeric); break;
                case BlastVectorType.float2: push(f4_register.xy, BlastVariableDataType.Numeric); break;
                case BlastVectorType.float3: push(f4_register.xyz, BlastVariableDataType.Numeric); break;
                case BlastVectorType.float4: push(f4_register.xyzw, BlastVariableDataType.Numeric); break;

                case BlastVectorType.int1: push(f4_register.x, BlastVariableDataType.ID); break;
                case BlastVectorType.int2: push(f4_register.xy, BlastVariableDataType.ID); break;
                case BlastVectorType.int3: push(f4_register.xyz, BlastVariableDataType.ID); break;
                case BlastVectorType.int4: push(f4_register.xyzw, BlastVariableDataType.ID); break;


#if DEVELOPMENT_BUILD || TRACE
                default:
                    Debug.LogError($"blast.interpretor pushf error: datatype {datatype}, vectorsize {vector_size} not yet supported on stack push");
                    //  return (int)BlastError.error_variable_vector_op_not_supported;
                    break;
#endif 
            }
        }

        #endregion

        #region AssignV Vector 

        /// <summary>
        /// builds a [n] vector from [1] values 
        /// </summary>
        /// <param name="code_pointer"></param>
        /// <param name="vector_size"></param>
        /// <param name="assignee"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void assignv(ref int code_pointer, in BlastVectorType vector_type, [NoAlias] in V1* assignee)
        {
            // param_count == vector_size and vector_size > 1, compiler enforces it so we should not check it in release 
            // this means that vector is build from multiple data points
            switch (vector_type)
            {
                case BlastVectorType.int1:
                    assignee[0] = CodeUtils.ReInterpret<int, V1>(pop_as_i1(ref code_pointer));
                    break;

                case BlastVectorType.int2:
                    assignee[0] = CodeUtils.ReInterpret<int, V1>(pop_as_i1(ref code_pointer));
                    assignee[1] = CodeUtils.ReInterpret<int, V1>(pop_as_i1(ref code_pointer));
                    break;   

                case BlastVectorType.int3:
                    assignee[0] = CodeUtils.ReInterpret<int, V1>(pop_as_i1(ref code_pointer));
                    assignee[1] = CodeUtils.ReInterpret<int, V1>(pop_as_i1(ref code_pointer));
                    assignee[2] = CodeUtils.ReInterpret<int, V1>(pop_as_i1(ref code_pointer));
                    break;

                case BlastVectorType.int4:
                    assignee[0] = CodeUtils.ReInterpret<int, V1>(pop_as_i1(ref code_pointer));
                    assignee[1] = CodeUtils.ReInterpret<int, V1>(pop_as_i1(ref code_pointer));
                    assignee[2] = CodeUtils.ReInterpret<int, V1>(pop_as_i1(ref code_pointer));
                    assignee[3] = CodeUtils.ReInterpret<int, V1>(pop_as_i1(ref code_pointer));
                    break;

                case BlastVectorType.float1: 
                    assignee[0] = CodeUtils.ReInterpret<float, V1>(pop_as_f1(ref code_pointer));
                    break;                 

                case BlastVectorType.float2:
                    assignee[0] = CodeUtils.ReInterpret<float, V1>(pop_as_f1(ref code_pointer)); 
                    assignee[1] = CodeUtils.ReInterpret<float, V1>(pop_as_f1(ref code_pointer));
                    break;

                case BlastVectorType.float3:
                    assignee[0] = CodeUtils.ReInterpret<float, V1>(pop_as_f1(ref code_pointer));
                    assignee[1] = CodeUtils.ReInterpret<float, V1>(pop_as_f1(ref code_pointer));
                    assignee[2] = CodeUtils.ReInterpret<float, V1>(pop_as_f1(ref code_pointer));
                    break;

                case BlastVectorType.float4:
                    assignee[0] = CodeUtils.ReInterpret<float, V1>(pop_as_f1(ref code_pointer));
                    assignee[1] = CodeUtils.ReInterpret<float, V1>(pop_as_f1(ref code_pointer));
                    assignee[2] = CodeUtils.ReInterpret<float, V1>(pop_as_f1(ref code_pointer));
                    assignee[3] = CodeUtils.ReInterpret<float, V1>(pop_as_f1(ref code_pointer));
                    break;

                default:
#if DEVELOPMENT_BUILD || TRACE
                    Debug.LogError($"blast.interpretor.assignv error: vector type {vector_type} not supported");
#endif
                    // ExitCode = BlastError.error_assign_vector_size_mismatch; 
                    break;
            }
        }


        #endregion
   
        #region ByteCode Execution

        /// <summary>
        /// get the result of a function encoded in the byte code, support all fuctions in op, exop and external calls 
        /// although external calls should be directly called when possible
        /// </summary>
        void get_function_result(ref int code_pointer, ref byte vector_size, ref BlastVariableDataType datatype, ref float4 f4_result)
        {
            //
            //  THIS IS A COPY OF THE CODE IN GET_COMPOUND, SHOULD TEST IF THE FUNCTION CALL IN COMPOUND EVEN MATTERS ANYMORE.. 
            //  due to all optimizing and flattening i guess there be few compounds calling functions and when
            //  they do it not a real problem to have 1 extra indirection 
            //

            byte op = code[code_pointer];

            switch ((blast_operation)op)
            {
                // math functions 
                case blast_operation.abs:   CALL_FN_ABS   (ref code_pointer, ref f4_result, out vector_size, out datatype); break;
                case blast_operation.trunc: CALL_FN_TRUNC (ref code_pointer, ref f4_result, out vector_size, out datatype); break;

                case blast_operation.maxa: CALL_FN_CMAX(ref code_pointer, ref datatype, ref vector_size, ref f4_result); break;
                case blast_operation.mina: CALL_FN_CMIN(ref code_pointer, ref datatype, ref vector_size, ref f4_result); break;
                case blast_operation.csum: CALL_FN_CSUM(ref code_pointer, ref datatype, ref vector_size, ref f4_result); break;

                case blast_operation.max:   CALL_FN_MAX   (ref code_pointer, ref vector_size, ref datatype, ref f4_result); break;
                case blast_operation.min:   CALL_FN_MIN   (ref code_pointer, ref vector_size, ref datatype, ref f4_result); break;

                // math utils 
                case blast_operation.select: get_select_result(ref code_pointer, ref vector_size, out f4_result); break;
                case blast_operation.fma: get_fma_result(ref code_pointer, ref vector_size, out f4_result); break;

                // mul|a family
                case blast_operation.mula: CALL_FN_MULA(ref code_pointer, ref vector_size, ref datatype, ref f4_result); break;
                case blast_operation.adda: CALL_FN_ADDA(ref code_pointer, ref vector_size, ref datatype, ref f4_result); break;
                case blast_operation.suba: CALL_FN_SUBA(ref code_pointer, ref vector_size, ref datatype, ref f4_result); break;
                case blast_operation.diva: CALL_FN_DIVA(ref code_pointer, ref vector_size, ref datatype, ref f4_result); break;

                // random|seed
                case blast_operation.random: CALL_FN_RANDOM(ref code_pointer, ref vector_size, ref datatype, ref f4_result); break;

                // and|all
                case blast_operation.all: CALL_FN_ALL(ref code_pointer, ref vector_size, ref datatype, ref f4_result); break;
                case blast_operation.any: CALL_FN_ANY(ref code_pointer, ref vector_size, ref datatype, ref f4_result); break;

                // indexing operations 
                case blast_operation.index_x: CALL_FN_INDEX(ref code_pointer, ref datatype, ref vector_size, ref f4_result, 0); break;
                case blast_operation.index_y: CALL_FN_INDEX(ref code_pointer, ref datatype, ref vector_size, ref f4_result, 1); break;
                case blast_operation.index_z: CALL_FN_INDEX(ref code_pointer, ref datatype, ref vector_size, ref f4_result, 2); break;
                case blast_operation.index_w: CALL_FN_INDEX(ref code_pointer, ref datatype, ref vector_size, ref f4_result, 3); break;
                case blast_operation.index_n: CALL_FN_INDEX_N(ref code_pointer, ref datatype, ref vector_size, ref f4_result); break;

                case blast_operation.expand_v2: CALL_EXPAND(ref code_pointer, ref datatype, ref vector_size, 2, ref f4_result); break;
                case blast_operation.expand_v3: CALL_EXPAND(ref code_pointer, ref datatype, ref vector_size, 3, ref f4_result); break;
                case blast_operation.expand_v4: CALL_EXPAND(ref code_pointer, ref datatype, ref vector_size, 4, ref f4_result); break;

                // zero a vector | cdata
                case blast_operation.zero: CALL_ZERO(ref code_pointer, ref vector_size); f4_result = 0; break;

                // get size of vector | cdata
                case blast_operation.size: CALL_FN_SIZE(ref code_pointer, ref datatype, ref vector_size, ref f4_result); break;

                // extended function ops  
                case blast_operation.ex_op:

                    code_pointer++;
                    extended_blast_operation exop = (extended_blast_operation)code[code_pointer];

                    if (exop == extended_blast_operation.call)
                    {
                        // call external function 
                        CallExternalFunction(ref code_pointer, ref vector_size, out f4_result);
                    }
                    else
                    {
                        switch (exop)
                        {
                            case extended_blast_operation.seed:
                                code_pointer++;
                                f4_result = pop_as_f1(ref code_pointer);
                                engine_ptr->Seed(math.asuint(f4_result.x));
                                break;

                            case extended_blast_operation.logn: CALL_FN_LOGN(ref code_pointer, ref f4_result, out vector_size, out datatype); break;
                            case extended_blast_operation.log10: CALL_FN_LOG10(ref code_pointer, ref f4_result, out vector_size, out datatype); break;
                            case extended_blast_operation.exp10: CALL_FN_EXP10(ref code_pointer, ref f4_result, out vector_size, out datatype); break;

                            case extended_blast_operation.exp:   CALL_FN_EXP(ref code_pointer, ref f4_result, out vector_size, out datatype);  break;
                            case extended_blast_operation.log2:  CALL_FN_LOG2(ref code_pointer, ref f4_result, out vector_size, out datatype); break;
                            case extended_blast_operation.sqrt:  CALL_FN_SQRT(ref code_pointer, ref f4_result, out vector_size, out datatype); break;
                            case extended_blast_operation.rsqrt: CALL_FN_RSQRT(ref code_pointer, ref f4_result, out vector_size, out datatype);break;

                            case extended_blast_operation.sin: CALL_FN_SIN(ref code_pointer, ref f4_result, out vector_size, out datatype); break;
                            case extended_blast_operation.cos: CALL_FN_COS(ref code_pointer, ref f4_result, out vector_size, out datatype); break;
                            case extended_blast_operation.tan: CALL_FN_TAN(ref code_pointer, ref f4_result, out vector_size, out datatype); break;
                            case extended_blast_operation.sinh: CALL_FN_SINH(ref code_pointer, ref f4_result, out vector_size, out datatype); break;
                            case extended_blast_operation.cosh: CALL_FN_COSH(ref code_pointer, ref f4_result, out vector_size, out datatype); break;
                            case extended_blast_operation.atan: CALL_FN_ATAN(ref code_pointer, ref f4_result, out vector_size, out datatype); break;
                            case extended_blast_operation.degrees: CALL_FN_DEGREES(ref code_pointer, ref f4_result, out vector_size, out datatype); break;
                            case extended_blast_operation.radians: CALL_FN_RADIANS(ref code_pointer, ref f4_result, out vector_size, out datatype); break;

                            case extended_blast_operation.normalize: CALL_FN_NORMALIZE(ref code_pointer, ref f4_result, out vector_size, out datatype); break; 
                            case extended_blast_operation.saturate: CALL_FN_SATURATE(ref code_pointer, ref f4_result, out vector_size, out datatype); break;
              
                            case extended_blast_operation.ceil: CALL_FN_CEIL(ref code_pointer, ref f4_result, out vector_size, out datatype); break;
                            case extended_blast_operation.floor: CALL_FN_FLOOR(ref code_pointer, ref f4_result, out vector_size, out datatype); break;
                            case extended_blast_operation.frac: CALL_FN_FRAC(ref code_pointer, ref f4_result, out vector_size, out datatype); break;

                            case extended_blast_operation.ceillog2: CALL_FN_CEILLOG2(ref code_pointer, ref f4_result, out vector_size, out datatype); break;
                            case extended_blast_operation.ceilpow2: CALL_FN_CEILPOW2(ref code_pointer, ref f4_result, out vector_size, out datatype); break;
                            case extended_blast_operation.floorlog2: CALL_FN_FLOORLOG2(ref code_pointer, ref f4_result, out vector_size, out datatype); break;

                            case extended_blast_operation.fmod: CALL_FN_FMOD(ref code_pointer, ref f4_result, out vector_size, out datatype); break;
                            case extended_blast_operation.pow: CALL_FN_POW(ref code_pointer, ref f4_result, out vector_size, out datatype); break;
                            case extended_blast_operation.atan2: CALL_FN_ATAN2(ref code_pointer, ref f4_result, out vector_size, out datatype); break;

                            case extended_blast_operation.sign: CALL_FN_SIGN(ref code_pointer, ref f4_result, out vector_size, out datatype); break;
                            case extended_blast_operation.length: CALL_FN_LENGTH(ref code_pointer, ref f4_result, out vector_size, out datatype); break;
                            case extended_blast_operation.square: CALL_FN_SQUARE(ref code_pointer, ref f4_result, out vector_size, out datatype); break;
                            case extended_blast_operation.distance: CALL_FN_DISTANCE(ref code_pointer, ref f4_result, out vector_size, out datatype); break;
                            case extended_blast_operation.distancesq: CALL_FN_DISTANCESQ(ref code_pointer, ref f4_result, out vector_size, out datatype); break;
                            case extended_blast_operation.reflect: CALL_FN_REFLECT(ref code_pointer, ref f4_result, out vector_size, out datatype); break;
                            case extended_blast_operation.project: CALL_FN_PROJECT(ref code_pointer, ref f4_result, out vector_size, out datatype); break;
 
                            case extended_blast_operation.up:       vector_size = 3; datatype = BlastVariableDataType.Numeric; f4_result.xyz = math.up(); break;
                            case extended_blast_operation.down:     vector_size = 3; datatype = BlastVariableDataType.Numeric; f4_result.xyz = math.down(); break;
                            case extended_blast_operation.forward:  vector_size = 3; datatype = BlastVariableDataType.Numeric; f4_result.xyz = math.forward(); break;
                            case extended_blast_operation.back:     vector_size = 3; datatype = BlastVariableDataType.Numeric; f4_result.xyz = math.back(); break;
                            case extended_blast_operation.left:     vector_size = 3; datatype = BlastVariableDataType.Numeric; f4_result.xyz = math.left(); break;
                            case extended_blast_operation.right:    vector_size = 3; datatype = BlastVariableDataType.Numeric; f4_result.xyz = math.right(); break;

                            case extended_blast_operation.EulerXYZ: CALL_FN_EULER(ref code_pointer, ref f4_result, out vector_size, out datatype, math.RotationOrder.XYZ); break;
                            case extended_blast_operation.EulerXZY: CALL_FN_EULER(ref code_pointer, ref f4_result, out vector_size, out datatype, math.RotationOrder.XZY); break;
                            case extended_blast_operation.EulerYXZ: CALL_FN_EULER(ref code_pointer, ref f4_result, out vector_size, out datatype, math.RotationOrder.YXZ); break;
                            case extended_blast_operation.EulerYZX: CALL_FN_EULER(ref code_pointer, ref f4_result, out vector_size, out datatype, math.RotationOrder.YZX); break;
                            case extended_blast_operation.EulerZXY: CALL_FN_EULER(ref code_pointer, ref f4_result, out vector_size, out datatype, math.RotationOrder.ZXY); break;
                            case extended_blast_operation.EulerZYX: CALL_FN_EULER(ref code_pointer, ref f4_result, out vector_size, out datatype, math.RotationOrder.ZYX); break;
                            case extended_blast_operation.Euler: CALL_FN_EULER(ref code_pointer, ref f4_result, out vector_size, out datatype); break;

                            case extended_blast_operation.QuaternionXYZ: CALL_FN_QUATERNION(ref code_pointer, ref f4_result, out vector_size, out datatype, math.RotationOrder.XYZ); break;
                            case extended_blast_operation.QuaternionXZY: CALL_FN_QUATERNION(ref code_pointer, ref f4_result, out vector_size, out datatype, math.RotationOrder.XZY); break;
                            case extended_blast_operation.QuaternionYXZ: CALL_FN_QUATERNION(ref code_pointer, ref f4_result, out vector_size, out datatype, math.RotationOrder.YXZ); break;
                            case extended_blast_operation.QuaternionYZX: CALL_FN_QUATERNION(ref code_pointer, ref f4_result, out vector_size, out datatype, math.RotationOrder.YZX); break;
                            case extended_blast_operation.QuaternionZXY: CALL_FN_QUATERNION(ref code_pointer, ref f4_result, out vector_size, out datatype, math.RotationOrder.ZXY); break;
                            case extended_blast_operation.QuaternionZYX: CALL_FN_QUATERNION(ref code_pointer, ref f4_result, out vector_size, out datatype, math.RotationOrder.ZYX); break;
                            case extended_blast_operation.Quaternion: CALL_FN_QUATERNION(ref code_pointer, ref f4_result, out vector_size, out datatype); break;

                            case extended_blast_operation.Conjugate: CALL_FN_CONJUGATE(ref code_pointer, ref f4_result, out vector_size, out datatype); break;
                            case extended_blast_operation.Inverse: CALL_FN_INVERSE(ref code_pointer, ref f4_result, out vector_size, out datatype); break;

                            case extended_blast_operation.RotateX: CALL_FN_ROTATEX(ref code_pointer, ref f4_result, out vector_size, out datatype); break;
                            case extended_blast_operation.RotateY: CALL_FN_ROTATEY(ref code_pointer, ref f4_result, out vector_size, out datatype); break;
                            case extended_blast_operation.RotateZ: CALL_FN_ROTATEZ(ref code_pointer, ref f4_result, out vector_size, out datatype); break;
                            case extended_blast_operation.LookRotation: CALL_FN_LOOKROTATION(ref code_pointer, ref f4_result, out vector_size, out datatype); break;
                            case extended_blast_operation.LookRotationSafe: CALL_FN_LOOKROTATIONSAFE(ref code_pointer, ref f4_result, out vector_size, out datatype); break;

                            case extended_blast_operation.Mul: CALL_FN_MUL(ref code_pointer, ref f4_result, out vector_size, out datatype); break;
                            case extended_blast_operation.Rotate: CALL_FN_ROTATE(ref code_pointer, ref f4_result, out vector_size, out datatype); break;
                            case extended_blast_operation.Angle: CALL_FN_ANGLE(ref code_pointer, ref f4_result, out vector_size, out datatype); break;

                            case extended_blast_operation.dot:   get_dot_result(ref code_pointer, ref vector_size, out f4_result); break;
                            case extended_blast_operation.cross: get_cross_result(ref code_pointer, ref vector_size, out f4_result); break;
                            case extended_blast_operation.clamp: get_clamp_result(ref code_pointer, ref vector_size, out f4_result); break;
                            case extended_blast_operation.lerp: get_lerp_result(ref code_pointer, ref vector_size, out f4_result); break;
                            case extended_blast_operation.slerp: get_slerp_result(ref code_pointer, ref vector_size, out f4_result); break;
                            case extended_blast_operation.nlerp: get_nlerp_result(ref code_pointer, ref vector_size, out f4_result); break;
                            case extended_blast_operation.remap: get_remap_result(ref code_pointer, ref vector_size, out f4_result); break;
                            case extended_blast_operation.unlerp: get_unlerp_result(ref code_pointer, ref vector_size, out f4_result); break;

                            case extended_blast_operation.set_bit: get_setbit_result(ref code_pointer, ref vector_size); f4_result = 0; break;
                            case extended_blast_operation.set_bits: get_setbits_result(ref code_pointer, ref vector_size); f4_result = 0; break;
                            case extended_blast_operation.get_bit: get_getbit_result(ref code_pointer, ref vector_size, out f4_result); break;
                            case extended_blast_operation.get_bits: get_getbits_result(ref code_pointer, ref vector_size, out f4_result); break;
                            case extended_blast_operation.count_bits: get_countbits_result(ref code_pointer, ref vector_size, out f4_result); break;
                            case extended_blast_operation.reverse_bits: get_reversebits_result(ref code_pointer, ref vector_size); f4_result = 0; break;
                            case extended_blast_operation.lzcnt: get_lzcnt_result(ref code_pointer, ref vector_size, out f4_result); break;
                            case extended_blast_operation.tzcnt: get_tzcnt_result(ref code_pointer, ref vector_size, out f4_result); break;
                            case extended_blast_operation.ror: get_ror_result(ref code_pointer, ref vector_size); f4_result = 0; break;
                            case extended_blast_operation.rol: get_rol_result(ref code_pointer, ref vector_size); f4_result = 0; break;
                            case extended_blast_operation.shl: get_shl_result(ref code_pointer, ref vector_size); f4_result = 0; break;
                            case extended_blast_operation.shr: get_shr_result(ref code_pointer, ref vector_size); f4_result = 0; break;


                            case extended_blast_operation.reinterpret_bool32: reinterpret_bool32(ref code_pointer, ref vector_size); f4_result = 0; break;
                            case extended_blast_operation.reinterpret_float: reinterpret_float(ref code_pointer, ref vector_size); f4_result = 0; break;
                            case extended_blast_operation.reinterpret_int32: reinterpret_int32(ref code_pointer, ref vector_size); f4_result = 0; break;

                            default:
#if DEVELOPMENT_BUILD || TRACE
                                Debug.LogError($"blast.interpretor: get_function_result: codepointer: {code_pointer} => {code[code_pointer]}, extended operation {exop} not handled");
#endif
                                f4_result = float.NaN;
                                return;
                        }
                    }
                    break;

                default:
                    {
#if DEVELOPMENT_BUILD || TRACE
                        Debug.LogError($"blast.interpretor: get_function_result: codepointer: {code_pointer} => {code[code_pointer]}, function operation {op} not handled");
#endif
                        // undefined results when calling undefined functions 
                        f4_result = float.NaN;
                        return;
                    }
            }

            code_pointer--;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="code_pointer"></param>
        /// <param name="vector_size"></param>
        /// <param name="f4_result"></param>
        void get_sequence_result(ref int code_pointer, ref byte vector_size, ref BlastVariableDataType datatype, ref float4 f4_result)
        {
            byte op = 0, prev_op = 0;

            blast_operation current_op = blast_operation.nop;

            bool not = false;
            bool minus = false;

            float4 f4 = float.NaN;

            vector_size = 1; // vectors dont shrink in normal operations 

            BlastVariableDataType current_datatype = BlastVariableDataType.Numeric;
            byte current_vector_size = 1;
            byte nesting_level = 0;

            //
            // each run:
            // - store value in f4
            // - if nop op -> f4_result = f4
            // -    else -> f4_result = f4_result * f4


            while (code_pointer < package.CodeSize)
            {
                bool last_is_bool_or_math_operation = false;
                prev_op = op;
                op = code[code_pointer];

                // process operation, put any value in f4, set current_op if something else

                switch ((blast_operation)op)
                {
                    case blast_operation.end:
                        {
                            if (nesting_level > 0)
                            {
                                nesting_level--;
                                code_pointer++;
                                continue;
                            }
                            //
                            //
                            // !! when assigning this might not be the end, peek next instruction.
                            //
                            // - if not a nop or a new assign, then continue reading the compound 
                            //
                            if (code_pointer < package.CodeSize - 1)
                            {
                                blast_operation next_op = (blast_operation)code[code_pointer + 1];

                                if (next_op != blast_operation.nop && BlastInterpretor.IsAssignmentOperation(next_op))
                                {
                                    // for now restrict ourselves to only accepting operations at this point 
                                    if (IsMathematicalOrBooleanOperation(next_op))
                                    {
                                        // break out of switch before returning, there is more to this operation then known at the moment 
                                        break;
                                    }
                                    else
                                    {
                                        // most probably a bug, error when the message is usefull
#if DEVELOPMENT_BUILD || TRACE
                                        Debug.LogError($"blast.interpretor: Possible BUG: encountered non mathematical operation '{next_op}' after compound in statement at codepointer {code_pointer + 1}, expecting nop or assign");
#endif
                                        return;
                                    }
                                }
                            }
                            return;
                        }


                    case blast_operation.nop:
                        {
                            //
                            // assignments will end with nop and not with end as a compounded statement
                            // at this point vector_size is decisive for the returned value
                            //
                            code_pointer++;
                            // this is normal when reading a zero terminated sequence 
                            return;
                        }


                    case blast_operation.ret:
                        // termination of interpretation
                        code_pointer = package.CodeSize + 1;
                        return;

                    // arithmetic ops 
                    case blast_operation.add:
                    case blast_operation.multiply:
                    case blast_operation.divide:
                    // boolean ops 
                    case blast_operation.or:
                    case blast_operation.and:
                    case blast_operation.xor:
                    case blast_operation.smaller:
                    case blast_operation.smaller_equals:
                    case blast_operation.greater:
                    case blast_operation.greater_equals:
                    case blast_operation.equals:
                    case blast_operation.not_equals:
                        {
                            // handle operation AFTER reading next value
                            current_op = (blast_operation)op;
                            last_is_bool_or_math_operation = true;
                            not = false;
                            minus = false;
                            code_pointer++;
                            prev_op = op;
                        }
                        continue;

                    case blast_operation.not:
                        {
                            // if the first in a compound or previous was an operation
                            if (prev_op == 0 || IsMathematicalOrBooleanOperation((blast_operation)prev_op))
                            {
#if DEVELOPMENT_BUILD || TRACE
                                if (not)
                                {
                                    Debug.LogError("blast.interpretor: 4: ! !, this will be become nothing, it is not supported as it cant be determined to be intentional and will raise this error in editor mode only");
                                }
#endif
                                not = !not;
                            }
                            else
                            {
                                current_op = blast_operation.not;
                            }
                        }
                        code_pointer++;
                        continue;

                    case blast_operation.substract:
                        {
                            // first or after an op 
                            if (prev_op == 0 || IsMathematicalOrBooleanOperation((blast_operation)prev_op))
                            {
#if DEVELOPMENT_BUILD || TRACE
                                if (minus)
                                {
                                    Debug.LogError("blast.interpretor: double token: - -, this will be become +, it is not supported as it cant be determined to be intentional and will raise this error in editor mode only");
                                }
#endif
                                minus = !minus;
                            }
                            else
                            {
                                current_op = blast_operation.substract;
                            }
                        }
                        code_pointer++;
                        continue;


                    case blast_operation.begin:

                        // allow to nest simple negations (maybe more in the future)
                        if (code[code_pointer + 1] == (byte)blast_operation.substract)
                        {
                            minus = true;
                            code_pointer += 2;
                            nesting_level++;

                            // level + 1, negate next value
                            continue;
                        }

#if DEVELOPMENT_BUILD || TRACE
                        Assert.IsTrue(false, "should not be nesting compounds... compiler did not do its job wel");
#endif
                        break;

                    // in development builds assert on non supported operations
                    // in release mode skip over crashing into the unknown 

#if DEVELOPMENT_BUILD || TRACE
                    // jumps are not allowed in compounds 
                    case blast_operation.jz:
                    case blast_operation.jnz:
                    case blast_operation.jump:
                    case blast_operation.jump_back:
                    case blast_operation.long_jump:
                        Assert.IsTrue(false, $"BlastInterpretor.GetCompound: jump operation {(blast_operation)op} not allowed in compounds, codepointer = {code_pointer}");
                        break;

                    // assignments are not allowed in compounds 
                    case blast_operation.yield:
                    case blast_operation.assigns:
                    case blast_operation.assign:
                    case blast_operation.assignf:
                    case blast_operation.assignfe:
                    case blast_operation.assignfn:
                    case blast_operation.assignfen:
                    case blast_operation.assignv:
                        Assert.IsTrue(false, $"BlastInterpretor.GetCompound: operation {(blast_operation)op} not allowed in compounds, codepointer = {code_pointer}");
                        break;

                    // any stack operation has no business in a compound other then pops
                    case blast_operation.push:
                    case blast_operation.pushv:
                    case blast_operation.peek:
                    case blast_operation.pushf:
                    case blast_operation.pushc:
                        Assert.IsTrue(false, $"BlastInterpretor.GetCompound: stack operation {(blast_operation)op} not allowed in compounds, codepointer = {code_pointer}");
                        break;


                    //
                    //                    case blast_operation.assign:
                    //                    case blast_operation.assigns:
                    //                    case blast_operation.assignf:
                    //                    case blast_operation.assignfe:
                    //                    case blast_operation.push:
                    //                    case blast_operation.pushf:
                    //                    case blast_operation.pushc:
                    //                    case blast_operation.pushv:
                    //                    case blast_operation.send:
                    //                    case blast_operation.jump:
                    //                    case blast_operation.jump_back:
                    //                    case blast_operation.jz:
                    //                    case blast_operation.jnz:
                    //                    case blast_operation.long_jump:
                    //                    case blast_operation.jz_long:
                    //                    case blast_operation.jnz_long:
                    //                        //
                    //                        //   TODO 
                    //                        //
                    //                        // - we could save a byte in the code, assign is not allowed in a 
                    //                        //   sequence as wel as push
                    //                        //   so any sequence terminator 0 can be omitted if the sequence is followed with an assign|push|jump|send 
                    //                        //    
                    //                        return; 
                    //
                    //
#endif
#if DEVELOPMENT_BUILD || TRACE
#endif

                    case blast_operation.pop:
                        {
                            BlastVariableDataType popped_type;
                            byte popped_vector_size;

                            GetMetaData(metadata, (byte)(stack_offset - 1), out popped_vector_size, out popped_type);

                            float* fdata = (float*)stack;

                            switch (popped_vector_size)
                            {
                                case 0:
                                case 4:
                                    stack_offset -= 4;
                                    f4 = ((float4*)(void*)&fdata[stack_offset])[0]; 
                                    vector_size = 4;
                                    break;

                                case 1:
                                    stack_offset -= 1;
                                    ((float*)(void*)&f4)[0] = fdata[stack_offset];
                                    vector_size = 1;
                                    break;

                                case 2:
                                    stack_offset -= 2;
                                    ((float2*)(void*)&f4)[0] = ((float2*)(void*)&fdata[stack_offset])[0];
                                    vector_size = 2;
                                    break;

                                case 3:
                                    stack_offset -= 3;
                                    ((float3*)(void*)&f4)[0] = ((float3*)(void*)&fdata[stack_offset])[0];
                                    vector_size = 3;
                                    break;

                                default:
#if DEVELOPMENT_BUILD || TRACE
                                    Debug.LogError($"BlastInterpretor.GetCompound: codepointer: {code_pointer} => pop vector too large, vectortype {vector_size} not supported");
#endif
                                    f4_result = float.NaN;
                                    return;
                            }
                        }
                        break;

                    //
                    // CoreAPI functions mapping to opcodes 
                    // 

                    // math functions
                    case blast_operation.abs: CALL_FN_ABS(ref code_pointer, ref f4, out vector_size, out datatype); code_pointer--; break;
                    case blast_operation.maxa: CALL_FN_CMAX(ref code_pointer, ref datatype, ref vector_size, ref f4); code_pointer--; break;
                    case blast_operation.mina: CALL_FN_CMIN(ref code_pointer, ref datatype, ref vector_size, ref f4); code_pointer--; break;
                    case blast_operation.csum: CALL_FN_CSUM(ref code_pointer, ref datatype, ref vector_size, ref f4); code_pointer--; break;

                    case blast_operation.max:   CALL_FN_MAX(ref code_pointer, ref vector_size, ref datatype, ref f4_result); break;
                    case blast_operation.min:   CALL_FN_MIN(ref code_pointer, ref vector_size, ref datatype, ref f4_result); break;
                    case blast_operation.trunc: CALL_FN_TRUNC(ref code_pointer, ref f4, out vector_size, out datatype); code_pointer--; break;

                    // math utils 
                    case blast_operation.select: get_select_result(ref code_pointer, ref vector_size, out f4); code_pointer--; break;
                    case blast_operation.fma: get_fma_result(ref code_pointer, ref vector_size, out f4); code_pointer--; break;

                    // mula family
                    case blast_operation.mula: CALL_FN_MULA(ref code_pointer, ref vector_size, ref datatype, ref f4); code_pointer--; break;
                    case blast_operation.adda: CALL_FN_ADDA(ref code_pointer, ref vector_size, ref datatype, ref f4); code_pointer--; break;
                    case blast_operation.suba: CALL_FN_SUBA(ref code_pointer, ref vector_size, ref datatype, ref f4); code_pointer--; break;
                    case blast_operation.diva: CALL_FN_DIVA(ref code_pointer, ref vector_size, ref datatype, ref f4); code_pointer--; break;


                    // any all 
                    case blast_operation.all: CALL_FN_ALL(ref code_pointer, ref vector_size, ref datatype, ref f4); code_pointer--; break;
                    case blast_operation.any: CALL_FN_ANY(ref code_pointer, ref vector_size, ref datatype, ref f4); code_pointer--; break;

                    // random 
                    case blast_operation.random: CALL_FN_RANDOM(ref code_pointer, ref vector_size, ref datatype, ref f4); code_pointer--; break;

                    // indexing operations 
                    case blast_operation.index_x: CALL_FN_INDEX(ref code_pointer, ref datatype, ref vector_size, ref f4, 0); code_pointer--; break;
                    case blast_operation.index_y: CALL_FN_INDEX(ref code_pointer, ref datatype, ref vector_size, ref f4, 1); code_pointer--; break;
                    case blast_operation.index_z: CALL_FN_INDEX(ref code_pointer, ref datatype, ref vector_size, ref f4, 2); code_pointer--; break;
                    case blast_operation.index_w: CALL_FN_INDEX(ref code_pointer, ref datatype, ref vector_size, ref f4, 3); code_pointer--; break;
                    case blast_operation.index_n: CALL_FN_INDEX_N(ref code_pointer, ref datatype, ref vector_size, ref f4); break;

                    case blast_operation.expand_v2: CALL_EXPAND(ref code_pointer, ref datatype, ref vector_size, 2, ref f4); code_pointer--; break;
                    case blast_operation.expand_v3: CALL_EXPAND(ref code_pointer, ref datatype, ref vector_size, 3, ref f4); code_pointer--; break;
                    case blast_operation.expand_v4: CALL_EXPAND(ref code_pointer, ref datatype, ref vector_size, 4, ref f4); code_pointer--; break;

                    // zero data index / clear bits 
                    case blast_operation.zero: CALL_ZERO(ref code_pointer, ref vector_size); code_pointer--; break;
                    case blast_operation.size: CALL_FN_SIZE(ref code_pointer, ref datatype, ref current_vector_size, ref f4); code_pointer--; break;

                    // extended operations 
                    case blast_operation.ex_op:
                        {
                            code_pointer++;
                            extended_blast_operation exop = (extended_blast_operation)code[code_pointer];

                            if (exop == extended_blast_operation.call)
                            {
                                // call external function 
                                CallExternalFunction(ref code_pointer, ref vector_size, out f4);
                            }
                            else
                            {
                                switch (exop)
                                {
                                    case extended_blast_operation.logn: CALL_FN_LOGN(ref code_pointer, ref f4, out vector_size, out datatype); break;
                                    case extended_blast_operation.log10: CALL_FN_LOG10(ref code_pointer, ref f4, out vector_size, out datatype); break;
                                    case extended_blast_operation.exp10: CALL_FN_EXP10(ref code_pointer, ref f4, out vector_size, out datatype); break;
                                    case extended_blast_operation.exp: CALL_FN_EXP(ref code_pointer, ref f4, out vector_size, out datatype); break;
                                    case extended_blast_operation.log2: CALL_FN_LOG2(ref code_pointer, ref f4, out vector_size, out datatype); break;
                                    case extended_blast_operation.sqrt: CALL_FN_SQRT(ref code_pointer, ref f4, out vector_size, out datatype); break;
                                    case extended_blast_operation.rsqrt: CALL_FN_RSQRT(ref code_pointer, ref f4, out vector_size, out datatype); break;
                                     case extended_blast_operation.sin: CALL_FN_SIN(ref code_pointer, ref f4, out vector_size, out datatype); break;
                                    case extended_blast_operation.cos: CALL_FN_COS(ref code_pointer, ref f4, out vector_size, out datatype); break;
                                    case extended_blast_operation.tan: CALL_FN_TAN(ref code_pointer, ref f4, out vector_size, out datatype); break;
                                    case extended_blast_operation.sinh: CALL_FN_SINH(ref code_pointer, ref f4, out vector_size, out datatype); break;
                                    case extended_blast_operation.cosh: CALL_FN_COSH(ref code_pointer, ref f4, out vector_size, out datatype); break;
                                    case extended_blast_operation.atan: CALL_FN_ATAN(ref code_pointer, ref f4, out vector_size, out datatype); break;
                                    case extended_blast_operation.degrees: CALL_FN_DEGREES(ref code_pointer, ref f4, out vector_size, out datatype); break;
                                    case extended_blast_operation.radians: CALL_FN_RADIANS(ref code_pointer, ref f4, out vector_size, out datatype); break;

                                    case extended_blast_operation.normalize: CALL_FN_NORMALIZE(ref code_pointer, ref f4, out vector_size, out datatype); break;
                                    case extended_blast_operation.saturate: CALL_FN_SATURATE(ref code_pointer, ref f4, out vector_size, out datatype); break;

                                    case extended_blast_operation.ceil: CALL_FN_CEIL(ref code_pointer, ref f4, out vector_size, out datatype); break;
                                    case extended_blast_operation.floor: CALL_FN_FLOOR(ref code_pointer, ref f4, out vector_size, out datatype); break;
                                    case extended_blast_operation.frac: CALL_FN_FRAC(ref code_pointer, ref f4, out vector_size, out datatype); break;
                                    
                                    case extended_blast_operation.atan2: CALL_FN_ATAN2(ref code_pointer, ref f4, out vector_size, out datatype); break;
                                    case extended_blast_operation.fmod: CALL_FN_FMOD(ref code_pointer, ref f4, out vector_size, out datatype); break;
                                    case extended_blast_operation.pow: CALL_FN_POW(ref code_pointer, ref f4, out vector_size, out datatype); break;

                                    case extended_blast_operation.ceillog2: CALL_FN_CEILLOG2(ref code_pointer, ref f4, out vector_size, out datatype); break;
                                    case extended_blast_operation.ceilpow2: CALL_FN_CEILPOW2(ref code_pointer, ref f4, out vector_size, out datatype); break;
                                    case extended_blast_operation.floorlog2: CALL_FN_FLOORLOG2(ref code_pointer, ref f4, out vector_size, out datatype); break;

                                    case extended_blast_operation.sign: CALL_FN_SIGN(ref code_pointer, ref f4, out vector_size, out datatype); break;
                                    case extended_blast_operation.length: CALL_FN_LENGTH(ref code_pointer, ref f4, out vector_size, out datatype); break;
                                    case extended_blast_operation.square: CALL_FN_SQUARE(ref code_pointer, ref f4, out vector_size, out datatype); break;
                                    case extended_blast_operation.distance: CALL_FN_DISTANCE(ref code_pointer, ref f4, out vector_size, out datatype); break;
                                    case extended_blast_operation.distancesq: CALL_FN_DISTANCESQ(ref code_pointer, ref f4, out vector_size, out datatype); break;
                                    case extended_blast_operation.reflect: CALL_FN_REFLECT(ref code_pointer, ref f4, out vector_size, out datatype); break;
                                    case extended_blast_operation.project: CALL_FN_PROJECT(ref code_pointer, ref f4, out vector_size, out datatype); break;

                                    case extended_blast_operation.up:       vector_size = 3; datatype = BlastVariableDataType.Numeric; f4.xyz = math.up(); break;
                                    case extended_blast_operation.down:     vector_size = 3; datatype = BlastVariableDataType.Numeric; f4.xyz = math.down(); break;
                                    case extended_blast_operation.forward:  vector_size = 3; datatype = BlastVariableDataType.Numeric; f4.xyz = math.forward(); break;
                                    case extended_blast_operation.back:     vector_size = 3; datatype = BlastVariableDataType.Numeric; f4.xyz = math.back(); break;
                                    case extended_blast_operation.left:     vector_size = 3; datatype = BlastVariableDataType.Numeric; f4.xyz = math.left(); break;
                                    case extended_blast_operation.right:    vector_size = 3; datatype = BlastVariableDataType.Numeric; f4.xyz = math.right(); break;

                                    case extended_blast_operation.EulerXYZ: CALL_FN_EULER(ref code_pointer, ref f4, out vector_size, out datatype, math.RotationOrder.XYZ); break;
                                    case extended_blast_operation.EulerXZY: CALL_FN_EULER(ref code_pointer, ref f4, out vector_size, out datatype, math.RotationOrder.XZY); break;
                                    case extended_blast_operation.EulerYXZ: CALL_FN_EULER(ref code_pointer, ref f4, out vector_size, out datatype, math.RotationOrder.YXZ); break;
                                    case extended_blast_operation.EulerYZX: CALL_FN_EULER(ref code_pointer, ref f4, out vector_size, out datatype, math.RotationOrder.YZX); break;
                                    case extended_blast_operation.EulerZXY: CALL_FN_EULER(ref code_pointer, ref f4, out vector_size, out datatype, math.RotationOrder.ZXY); break;
                                    case extended_blast_operation.EulerZYX: CALL_FN_EULER(ref code_pointer, ref f4, out vector_size, out datatype, math.RotationOrder.ZYX); break;
                                    case extended_blast_operation.Euler: CALL_FN_EULER(ref code_pointer, ref f4, out vector_size, out datatype); break;

                                    case extended_blast_operation.QuaternionXYZ: CALL_FN_QUATERNION(ref code_pointer, ref f4, out vector_size, out datatype, math.RotationOrder.XYZ); break;
                                    case extended_blast_operation.QuaternionXZY: CALL_FN_QUATERNION(ref code_pointer, ref f4, out vector_size, out datatype, math.RotationOrder.XZY); break;
                                    case extended_blast_operation.QuaternionYXZ: CALL_FN_QUATERNION(ref code_pointer, ref f4, out vector_size, out datatype, math.RotationOrder.YXZ); break;
                                    case extended_blast_operation.QuaternionYZX: CALL_FN_QUATERNION(ref code_pointer, ref f4, out vector_size, out datatype, math.RotationOrder.YZX); break;
                                    case extended_blast_operation.QuaternionZXY: CALL_FN_QUATERNION(ref code_pointer, ref f4, out vector_size, out datatype, math.RotationOrder.ZXY); break;
                                    case extended_blast_operation.QuaternionZYX: CALL_FN_QUATERNION(ref code_pointer, ref f4, out vector_size, out datatype, math.RotationOrder.ZYX); break;
                                    case extended_blast_operation.Quaternion:    CALL_FN_QUATERNION(ref code_pointer, ref f4, out vector_size, out datatype); break;

                                    case extended_blast_operation.Conjugate: CALL_FN_CONJUGATE(ref code_pointer, ref f4, out vector_size, out datatype); break;
                                    case extended_blast_operation.Inverse: CALL_FN_INVERSE(ref code_pointer, ref f4, out vector_size, out datatype); break;

                                    case extended_blast_operation.RotateX: CALL_FN_ROTATEX(ref code_pointer, ref f4_result, out vector_size, out datatype); break;
                                    case extended_blast_operation.RotateY: CALL_FN_ROTATEY(ref code_pointer, ref f4_result, out vector_size, out datatype); break;
                                    case extended_blast_operation.RotateZ: CALL_FN_ROTATEZ(ref code_pointer, ref f4_result, out vector_size, out datatype); break;
                                    case extended_blast_operation.LookRotation: CALL_FN_LOOKROTATION(ref code_pointer, ref f4_result, out vector_size, out datatype); break;
                                    case extended_blast_operation.LookRotationSafe: CALL_FN_LOOKROTATIONSAFE(ref code_pointer, ref f4_result, out vector_size, out datatype); break;

                                    case extended_blast_operation.Mul: CALL_FN_MUL(ref code_pointer, ref f4_result, out vector_size, out datatype); break;
                                    case extended_blast_operation.Rotate: CALL_FN_ROTATE(ref code_pointer, ref f4_result, out vector_size, out datatype); break;
                                    case extended_blast_operation.Angle: CALL_FN_ANGLE(ref code_pointer, ref f4_result, out vector_size, out datatype); break;

                                    case extended_blast_operation.cross: get_cross_result(ref code_pointer, ref vector_size, out f4); break;
                                    case extended_blast_operation.dot: get_dot_result(ref code_pointer, ref vector_size, out f4); break;

                                    case extended_blast_operation.clamp: get_clamp_result(ref code_pointer, ref vector_size, out f4); break;
                                    case extended_blast_operation.lerp: get_lerp_result(ref code_pointer, ref vector_size, out f4); break;
                                    case extended_blast_operation.slerp: get_slerp_result(ref code_pointer, ref vector_size, out f4); break;
                                    case extended_blast_operation.nlerp: get_nlerp_result(ref code_pointer, ref vector_size, out f4); break;
                                    case extended_blast_operation.remap: get_remap_result(ref code_pointer, ref vector_size, out f4); break;
                                    case extended_blast_operation.unlerp: get_unlerp_result(ref code_pointer, ref vector_size, out f4); break;

                                    case extended_blast_operation.set_bit: get_setbit_result(ref code_pointer, ref vector_size); code_pointer--; break;
                                    case extended_blast_operation.set_bits: get_setbits_result(ref code_pointer, ref vector_size); code_pointer--; break;
                                    case extended_blast_operation.get_bit: get_getbit_result(ref code_pointer, ref vector_size, out f4); code_pointer--; break;
                                    case extended_blast_operation.get_bits: get_getbits_result(ref code_pointer, ref vector_size, out f4); code_pointer--; break;
                                    case extended_blast_operation.count_bits: get_countbits_result(ref code_pointer, ref vector_size, out f4); break;
                                    case extended_blast_operation.reverse_bits: get_reversebits_result(ref code_pointer, ref vector_size); break;
                                    case extended_blast_operation.lzcnt: get_lzcnt_result(ref code_pointer, ref vector_size, out f4); break;
                                    case extended_blast_operation.tzcnt: get_tzcnt_result(ref code_pointer, ref vector_size, out f4); break;
                                    case extended_blast_operation.ror: get_ror_result(ref code_pointer, ref vector_size); break;
                                    case extended_blast_operation.rol: get_rol_result(ref code_pointer, ref vector_size); break;
                                    case extended_blast_operation.shl: get_shl_result(ref code_pointer, ref vector_size); break;
                                    case extended_blast_operation.shr: get_shr_result(ref code_pointer, ref vector_size); break;

                                    case extended_blast_operation.reinterpret_bool32: reinterpret_bool32(ref code_pointer, ref vector_size); break;
                                    case extended_blast_operation.reinterpret_float: reinterpret_float(ref code_pointer, ref vector_size); break;
                                    case extended_blast_operation.reinterpret_int32: reinterpret_int32(ref code_pointer, ref vector_size); break;


#if DEVELOPMENT_BUILD || TRACE
                                    default:
                                        Debug.LogError($"codepointer: {code_pointer} => {code[code_pointer]}, extended operation {exop} not handled");
                                        break;
#endif
                                }
                            }
                        }
                        code_pointer--;                           
                        break;

                    case blast_operation.pi:                    f4 = math.PI;                   datatype = BlastVariableDataType.Numeric; break;
                    case blast_operation.inv_pi:                f4 = 1f / math.PI;              datatype = BlastVariableDataType.Numeric; break;
                    case blast_operation.epsilon:               f4 = math.EPSILON;              datatype = BlastVariableDataType.Numeric; break;
                    case blast_operation.infinity:              f4 = math.INFINITY;             datatype = BlastVariableDataType.Numeric; break;
                    case blast_operation.negative_infinity:     f4 = -math.INFINITY;            datatype = BlastVariableDataType.Numeric; break;
                    case blast_operation.nan:                   f4 = math.NAN;                  datatype = BlastVariableDataType.Numeric; break;
                    case blast_operation.min_value:             f4 =  math.FLT_MIN_NORMAL;      datatype = BlastVariableDataType.Numeric; break;
                    case blast_operation.value_1:               f4 = 1f;                        datatype = BlastVariableDataType.Numeric; break;
                    case blast_operation.value_2:               f4 = 2f;                        datatype = BlastVariableDataType.Numeric; break;
                    case blast_operation.value_3:               f4 = 3f;                        datatype = BlastVariableDataType.Numeric; break;
                    case blast_operation.value_4:               f4 = 4f;                        datatype = BlastVariableDataType.Numeric; break;
                    case blast_operation.value_8:               f4 = 8f;                        datatype = BlastVariableDataType.Numeric; break;
                    case blast_operation.value_0:               f4 = 0f;                        datatype = BlastVariableDataType.Numeric; break;
                    case blast_operation.value_10:              f4 = 10f;                       datatype = BlastVariableDataType.Numeric; break;
                    case blast_operation.value_16:              f4 = 16f;                       datatype = BlastVariableDataType.Numeric; break;
                    case blast_operation.value_24:              f4 = 24f;                       datatype = BlastVariableDataType.Numeric; break;
                    case blast_operation.value_32:              f4 = 32f;                       datatype = BlastVariableDataType.Numeric; break;
                    case blast_operation.value_64:              f4 = 64f;                       datatype = BlastVariableDataType.Numeric; break;
                    case blast_operation.value_100:             f4 = 100f;                      datatype = BlastVariableDataType.Numeric; break;
                    case blast_operation.value_128:             f4 = 128f;                      datatype = BlastVariableDataType.Numeric; break;
                    case blast_operation.value_256:             f4 = 256f;                      datatype = BlastVariableDataType.Numeric; break;
                    case blast_operation.value_512:             f4 = 512f;                      datatype = BlastVariableDataType.Numeric; break;
                    case blast_operation.value_1000:            f4 = 1000f;                     datatype = BlastVariableDataType.Numeric; break;
                    case blast_operation.value_1024:            f4 = 1024f;                     datatype = BlastVariableDataType.Numeric; break;
                    case blast_operation.value_30:              f4 = 30f;                       datatype = BlastVariableDataType.Numeric; break;
                    case blast_operation.value_45:              f4 = 45f;                       datatype = BlastVariableDataType.Numeric; break;
                    case blast_operation.value_90:              f4 = 90f;                       datatype = BlastVariableDataType.Numeric; break;
                    case blast_operation.value_180:             f4 = 180f;                      datatype = BlastVariableDataType.Numeric; break;
                    case blast_operation.value_270:             f4 = 270f;                      datatype = BlastVariableDataType.Numeric; break;
                    case blast_operation.value_360:             f4 = 360f;                      datatype = BlastVariableDataType.Numeric; break;
                    case blast_operation.inv_value_2:           f4 = 1 / 2f;                    datatype = BlastVariableDataType.Numeric; break;
                    case blast_operation.inv_value_3:           f4 = 1 / 3f;                    datatype = BlastVariableDataType.Numeric; break;
                    case blast_operation.inv_value_4:           f4 = 1 / 4f;                    datatype = BlastVariableDataType.Numeric; break;
                    case blast_operation.inv_value_8:           f4 = 1 / 8f;                    datatype = BlastVariableDataType.Numeric; break;
                    case blast_operation.inv_value_10:          f4 = 1 / 10f;                   datatype = BlastVariableDataType.Numeric; break;
                    case blast_operation.inv_value_16:          f4 = 1 / 16f;                   datatype = BlastVariableDataType.Numeric; break;
                    case blast_operation.inv_value_24:          f4 = 1 / 24f;                   datatype = BlastVariableDataType.Numeric; break;
                    case blast_operation.inv_value_32:          f4 = 1 / 32f;                   datatype = BlastVariableDataType.Numeric; break;
                    case blast_operation.inv_value_64:          f4 = 1 / 64f;                   datatype = BlastVariableDataType.Numeric; break;
                    case blast_operation.inv_value_100:         f4 = 1 / 100f;                  datatype = BlastVariableDataType.Numeric; break;
                    case blast_operation.inv_value_128:         f4 = 1 / 128f;                  datatype = BlastVariableDataType.Numeric; break;
                    case blast_operation.inv_value_256:         f4 = 1 / 256f;                  datatype = BlastVariableDataType.Numeric; break;
                    case blast_operation.inv_value_512:         f4 = 1 / 512f;                  datatype = BlastVariableDataType.Numeric; break;
                    case blast_operation.inv_value_1000:        f4 = 1 / 1000f;                 datatype = BlastVariableDataType.Numeric; break;
                    case blast_operation.inv_value_1024:        f4 = 1 / 1024f;                 datatype = BlastVariableDataType.Numeric; break;
                    case blast_operation.inv_value_30:          f4 = 1 / 30f;                   datatype = BlastVariableDataType.Numeric; break;

                    case blast_operation.framecount:            f4 = engine_ptr->constants[(byte)blast_operation.framecount];       datatype = BlastVariableDataType.Numeric; break;
                    case blast_operation.fixedtime:             f4 = engine_ptr->constants[(byte)blast_operation.fixedtime];        datatype = BlastVariableDataType.Numeric; break;
                    case blast_operation.time:                  f4 = engine_ptr->constants[(byte)blast_operation.time];             datatype = BlastVariableDataType.Numeric; break;
                    case blast_operation.fixeddeltatime:        f4 = engine_ptr->constants[(byte)blast_operation.fixeddeltatime];   datatype = BlastVariableDataType.Numeric; break;
                    case blast_operation.deltatime:             f4 = engine_ptr->constants[(byte)blast_operation.deltatime];        datatype = BlastVariableDataType.Numeric; break;


                    // handle identifiers/variables
                    default:
                        {
                            if (op >= opt_id) // only exop is higher and that is handled earlier so this check is safe 
                            {
                                // lookup identifier value
                                byte id = (byte)(op - opt_id);
                                byte this_vector_size = BlastInterpretor.GetMetaDataSize(metadata, id);

                                // and put it in f4 
                                switch ((BlastVectorSizes)this_vector_size)
                                {
                                    case BlastVectorSizes.float1: f4.x = ((float*)data)[id]; break;
                                    
                                    // althoug horrible to read, this compiles into vector instructions in .net on my machine, all other forms dont 
                                    case BlastVectorSizes.float2: ((float2*)(void*)&f4)[0] = ((float2*)(void*)&((float*)data)[id])[0]; break;

                                    case BlastVectorSizes.float3: ((float3*)(void*)&f4)[0] = ((float3*)(void*)&((float*)data)[id])[0]; break;
                                    case 0:
                                    case BlastVectorSizes.float4: ((float4*)(void*)&f4)[0] = ((float4*)(void*)&((float*)data)[id])[0]; break;
                                }

                                // grow current vector size 

                                vector_size = vector_size > this_vector_size && current_op == blast_operation.nop ? vector_size : this_vector_size; 
                            }
                            else
                            {
                                // we receive an operation while expecting a value 
                                //
                                // if we predict this at the compiler we could use the opcodes value as value constant
                                //
#if DEVELOPMENT_BUILD || TRACE
                                Debug.LogError($"codepointer: {code_pointer} => {code[code_pointer]}, encountered unknown op {(blast_operation)op}");
#endif

                                // this should royally screw stuff up 
                                f4_result = float.NaN;
                                return;
                            }
                        }

                        break;
                }

                //
                // handle value in sequenced operation 
                //
                if (current_op == 0 && !((minus || not) && prev_op == 0))
                {
                    // just set according to vector size on the first value
                    switch ( datatype.Combine(vector_size, minus || not) )
                    {
                        case BlastVectorType.float1: f4_result.x = f4.x; current_vector_size = 1; break;
                        case BlastVectorType.float1_n: f4_result.x = -f4.x; current_vector_size = 1; break;
                        case BlastVectorType.float2: fixed (float4* p = &f4_result) ((float2*)(void*)p)[0] = ((float2*)(void*)&f4)[0]; current_vector_size = 2; break;
                        case BlastVectorType.float2_n: fixed (float4* p = &f4_result) ((float2*)(void*)p)[0] = -((float2*)(void*)&f4)[0]; current_vector_size = 2; break;
                        case BlastVectorType.float3: fixed (float4* p = &f4_result) ((float3*)(void*)p)[0] = ((float3*)(void*)&f4)[0]; current_vector_size = 3; break;
                        case BlastVectorType.float3_n: fixed (float4* p = &f4_result) ((float3*)(void*)p)[0] = -((float3*)(void*)&f4)[0]; current_vector_size = 3; break;
                        case BlastVectorType.float4: f4_result = f4; current_vector_size = 4; break;
                        case BlastVectorType.float4_n: f4_result = -f4; current_vector_size = 4; break;
                        
                        // for id, as its the first we copy it and set datatype 
                        case BlastVectorType.int1: f4_result.x = f4.x; current_vector_size = 1; current_datatype = BlastVariableDataType.ID; break;
                        case BlastVectorType.int2: fixed (float4* p = &f4_result) ((float2*)(void*)p)[0] = ((float2*)(void*)&f4)[0]; current_vector_size = 2; current_datatype = BlastVariableDataType.ID; break;
                        case BlastVectorType.int3: fixed (float4* p = &f4_result) ((float3*)(void*)p)[0] = ((float3*)(void*)&f4)[0]; current_vector_size = 3; current_datatype = BlastVariableDataType.ID; break;
                        case BlastVectorType.int4: f4_result = f4; current_vector_size = 4; current_datatype = BlastVariableDataType.ID; break;

                        // negations need a conversion inlined to perform the negation correctly on the int backed in float memory 
                        case BlastVectorType.int1_n: f4_result.x = CodeUtils.ReInterpret<int, float>(-CodeUtils.ReInterpret<float, int>(f4.x)); current_vector_size = 1; current_datatype = BlastVariableDataType.ID; break;
                        case BlastVectorType.int2_n: fixed (float4* p = &f4_result) ((float2*)(void*)p)[0] = CodeUtils.ReInterpret<int2, float2>(-CodeUtils.ReInterpret<float2, int2>(((float2*)(void*)&f4)[0])); current_vector_size = 2; current_datatype = BlastVariableDataType.ID; break;
                        case BlastVectorType.int3_n: fixed (float4* p = &f4_result) ((float3*)(void*)p)[0] = CodeUtils.ReInterpret<int3, float3>(-CodeUtils.ReInterpret<float3, int3>(((float3*)(void*)&f4)[0])); current_vector_size = 3; current_datatype = BlastVariableDataType.ID; break;
                        case BlastVectorType.int4_n: f4_result = CodeUtils.ReInterpret<int4, float4>(-CodeUtils.ReInterpret<float4, int4>(f4)); current_vector_size = 4; current_datatype = BlastVariableDataType.ID; break;

                        // bool32 requires a conversion for not to be correct 
                        case BlastVectorType.bool32: f4_result.x = f4.x; current_vector_size = 1; current_datatype = BlastVariableDataType.Bool32; break; 
                        case BlastVectorType.bool32_n: f4_result.x = CodeUtils.ReInterpret<uint, float>(~CodeUtils.ReInterpret<float, uint>(f4.x)); current_vector_size = 1; current_datatype = BlastVariableDataType.Bool32; break;

                        default: goto NO_SUPPORT;
                    }
                }
                else
                {
                    if (!last_is_bool_or_math_operation)
                    {
                        switch(current_datatype.Combine(current_vector_size, false))
                        {
                            case BlastVectorType.float1:
                                {
                                    // the operation starts as a float -> end as a float 
                                    switch(datatype.Combine(vector_size, minus || not))
                                    {
                                        // f1result = f1a + f1b
                                        case BlastVectorType.float1:    BlastInterpretor.handle_op_f1f1(current_op, ref f4_result.x, f4.x); break;
                                        case BlastVectorType.int1:      BlastInterpretor.handle_op_f1i1(current_op, ref f4_result.x, f4.x); break;
                                        case BlastVectorType.bool32:    BlastInterpretor.handle_op_f1i1(current_op, ref f4_result.x, f4.x); break;

                                        // f1result = f1a + -f1b
                                        case BlastVectorType.float1_n:  BlastInterpretor.handle_op_f1f1(current_op, ref f4_result.x, -f4.x); minus = false; not = false; break;
                                        case BlastVectorType.int1_n:    BlastInterpretor.handle_op_f1i1_n(current_op, ref f4_result.x, f4.x); minus = false; not = false; break;
                                        case BlastVectorType.bool32_n:  BlastInterpretor.handle_op_f1b1_n(current_op, ref f4_result.x, f4.x); minus = false; not = false; break;

                                        // f2result = f1a op [-]f2b (f4 is referenced where x is read and xy is written as result 
                                        case BlastVectorType.float2:    BlastInterpretor.handle_op_f1f2(current_op, ref f4_result, f4.xy); current_vector_size = 2; break;
                                        case BlastVectorType.float2_n:  BlastInterpretor.handle_op_f1f2(current_op, ref f4_result, -f4.xy); current_vector_size = 2; minus = false; not = false; break;

                                        // f3result = f1a op [-]f3b 
                                        case BlastVectorType.float3:    BlastInterpretor.handle_op_f1f3(current_op, ref f4_result, f4.xyz); current_vector_size = 3; break;
                                        case BlastVectorType.float3_n:  BlastInterpretor.handle_op_f1f3(current_op, ref f4_result, -f4.xyz); current_vector_size = 3; minus = false; not = false; break;

                                        // f4result = f1a op [-]f4b 
                                        case BlastVectorType.float4:    BlastInterpretor.handle_op_f1f4(current_op, ref f4_result, f4); current_vector_size = 4; break;
                                        case BlastVectorType.float4_n:  BlastInterpretor.handle_op_f1f4(current_op, ref f4_result, -f4); current_vector_size = 4; minus = false; not = false; break;

                                        default: goto NO_SUPPORT_FOR_HANDLE_OP;
                                    }
                                }
                                break;

                            case BlastVectorType.bool32:
                            case BlastVectorType.int1:
                                {
                                    // the operation starts with an integer, if we handle against a float, update of datatype is needed
                                    switch (datatype.Combine(vector_size, minus || not))
                                    {
                                        case BlastVectorType.bool32:
                                        case BlastVectorType.int1:      BlastInterpretor.handle_op_i1i1(current_op, ref f4_result.x, f4.x); break;
                                        case BlastVectorType.bool32_n:  BlastInterpretor.handle_op_i1b1_n(current_op, ref f4_result.x, f4.x); minus = false; not = false; break;
                                        case BlastVectorType.int1_n:    BlastInterpretor.handle_op_i1i1_n(current_op, ref f4_result.x, f4.x); minus = false; not = false; break;
                                        case BlastVectorType.int2:      BlastInterpretor.handle_op_i1i2(current_op, ref f4_result, f4.xy); current_vector_size = 2; break;
                                        case BlastVectorType.int2_n:    BlastInterpretor.handle_op_i1i2_n(current_op, ref f4_result, f4.xy); current_vector_size = 2; minus = false; not = false; break;
                                        case BlastVectorType.int3:      BlastInterpretor.handle_op_i1i3(current_op, ref f4_result, f4.xyz); current_vector_size = 3; break;
                                        case BlastVectorType.int3_n:    BlastInterpretor.handle_op_i1i3_n(current_op, ref f4_result, f4.xyz); current_vector_size = 3; minus = false; not = false; break;
                                        case BlastVectorType.int4:      BlastInterpretor.handle_op_i1i4(current_op, ref f4_result, f4); current_vector_size = 4; break;
                                        case BlastVectorType.int4_n:    BlastInterpretor.handle_op_i1i4_n(current_op, ref f4_result, f4); current_vector_size = 4; minus = false; not = false; break;
                                        case BlastVectorType.float1:    BlastInterpretor.handle_op_i1f1(current_op, ref f4_result.x, f4.x); datatype = current_datatype; break;
                                        case BlastVectorType.float1_n:  BlastInterpretor.handle_op_i1f1(current_op, ref f4_result.x, -f4.x); datatype = current_datatype; minus = false; not = false; break;
                                        case BlastVectorType.float2:    BlastInterpretor.handle_op_i1f2(current_op, ref f4_result, f4.xy); datatype = current_datatype; current_vector_size = 2; break;
                                        case BlastVectorType.float2_n:  BlastInterpretor.handle_op_i1f2(current_op, ref f4_result, -f4.xy); datatype = current_datatype; current_vector_size = 2; minus = false; not = false; break;
                                        case BlastVectorType.float3:    BlastInterpretor.handle_op_i1f3(current_op, ref f4_result, f4.xyz); datatype = current_datatype; current_vector_size = 3; break;
                                        case BlastVectorType.float3_n:  BlastInterpretor.handle_op_i1f3(current_op, ref f4_result, -f4.xyz); datatype = current_datatype; current_vector_size = 3; minus = false; not = false; break;
                                        case BlastVectorType.float4:    BlastInterpretor.handle_op_i1f4(current_op, ref f4_result, f4); datatype = current_datatype; current_vector_size = 4; break;
                                        case BlastVectorType.float4_n:  BlastInterpretor.handle_op_i1f4(current_op, ref f4_result, -f4); datatype = current_datatype; current_vector_size = 4; minus = false; not = false; break;

                                        default: goto NO_SUPPORT_FOR_HANDLE_OP; 
                                    }
                                }
                                break; 

                            case BlastVectorType.float2:
                                {
                                    switch (datatype.Combine(vector_size, minus || not))
                                    {
                                        // f4.xy = f4.xy op [-]f1
                                        case BlastVectorType.float1:    BlastInterpretor.handle_op_f2f1(current_op, ref f4_result, f4.x); break;
                                        case BlastVectorType.float1_n:  BlastInterpretor.handle_op_f2f1(current_op, ref f4_result, -f4.x); minus = false; not = false; break;
                                        // f4.xy = f4.xy op [-]f2
                                        case BlastVectorType.float2:    BlastInterpretor.handle_op_f2f2(current_op, ref f4_result, f4.xy); vector_size = 2; break;
                                        case BlastVectorType.float2_n:  BlastInterpretor.handle_op_f2f2(current_op, ref f4_result, -f4.xy); vector_size = 2; minus = false; not = false; break;

                                        default: goto NO_SUPPORT_FOR_HANDLE_OP;
                                    }
                                }
                                break;

                            case BlastVectorType.float3:
                                {
                                    switch (datatype.Combine(vector_size, minus || not))
                                    {
                                        case BlastVectorType.float1:    BlastInterpretor.handle_op_f3f1(current_op, ref f4_result, f4.x); vector_size = 3; break;
                                        case BlastVectorType.float1_n:  BlastInterpretor.handle_op_f3f1(current_op, ref f4_result, -f4.x); vector_size = 3; minus = false; not = false; break;

                                        case BlastVectorType.float3:    BlastInterpretor.handle_op_f3f3(current_op, ref f4_result, f4.xyz); break;
                                        case BlastVectorType.float3_n:  BlastInterpretor.handle_op_f3f3(current_op, ref f4_result, -f4.xyz); minus = false; not = false; break;

                                        default: goto NO_SUPPORT_FOR_HANDLE_OP; 
                                    }
                                }
                                break; 


                            case BlastVectorType.float4:
                                {
                                    switch (datatype.Combine(vector_size, minus || not))
                                    {
                                        case BlastVectorType.float1:    BlastInterpretor.handle_op_f4f1(current_op, ref f4_result, f4.x); vector_size = 4; break;
                                        case BlastVectorType.float1_n:  BlastInterpretor.handle_op_f4f1(current_op, ref f4_result, -f4.x); vector_size = 4; minus = false; not = false; break;

                                        case BlastVectorType.float4:    BlastInterpretor.handle_op_f4f4(current_op, ref f4_result, f4); break;
                                        case BlastVectorType.float4_n:  BlastInterpretor.handle_op_f4f4(current_op, ref f4_result, -f4); minus = false; not = false; break;

                                        default: goto NO_SUPPORT_FOR_HANDLE_OP; 
                                    }                                    
                                }
                                break;

                            default: goto NO_SUPPORT_FOR_HANDLE_OP;
                        }

                        // reset current operation 
                        current_op = blast_operation.nop;
                    }
                    else
                    {
                        // apply minus
                        if (minus || not)
                        {
                            switch (datatype.Combine(vector_size, true))
                            {
                                case BlastVectorType.float1_n:
                                case BlastVectorType.float2_n:
                                case BlastVectorType.float3_n:
                                case BlastVectorType.float4_n: f4_result = -f4_result; break;

                                case BlastVectorType.int1_n: f4_result.x = CodeUtils.ReInterpret<int, float>(-CodeUtils.ReInterpret<float, int>(f4_result.x)); break;
                                case BlastVectorType.int2_n: f4_result.xy= CodeUtils.ReInterpret<int2, float2>(-CodeUtils.ReInterpret<float2, int2>(f4_result.xy)); break;
                                case BlastVectorType.int3_n: f4_result.xyz = CodeUtils.ReInterpret<int3, float3>(-CodeUtils.ReInterpret<float3, int3>(f4_result.xyz)); break;
                                case BlastVectorType.int4_n: f4_result.xyzw = CodeUtils.ReInterpret<int4, float4>(-CodeUtils.ReInterpret<float4, int4>(f4_result.xyzw)); break;

                                case BlastVectorType.bool32_n: f4_result.x = CodeUtils.ReInterpret<uint, float>(~CodeUtils.ReInterpret<float, uint>(f4_result.x)); break;

                                default: goto NO_SUPPORT_NEGATE;
                            }
                            minus = false;
                            not = false;
                        }
                    }
                }

                // next
                code_pointer++;
            }

            return;

        NO_SUPPORT_FOR_HANDLE_OP:
#if TRACE || DEVELOPMENT_BUILD
            Debug.LogError($"execute-sequence: codepointer: {code_pointer} => {code[code_pointer]}, no support for operation: {datatype}[{vector_size}] op {current_datatype}[{current_vector_size}]");
#endif 
            return;
        NO_SUPPORT:
#if TRACE || DEVELOPMENT_BUILD
            Debug.LogError($"execute-sequence: codepointer: {code_pointer} => {code[code_pointer]}, no support for {datatype}[{vector_size}]");
#endif 
            return;
        NO_SUPPORT_NEGATE:
#if TRACE || DEVELOPMENT_BUILD
            Debug.LogError($"execute-sequence: codepointer: {code_pointer} => {code[code_pointer]}, no support for negation of {datatype}[{vector_size}]");
#endif 
            return;
        }



        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void EvalPushedJumpCondition(ref int code_pointer, ref float4 f4_register, ref byte vector_size, ref BlastVariableDataType datatype)
        {
            // eval all push commands before running the compound
            bool is_push = true;
            do
            {
                switch ((blast_operation)code[code_pointer])
                {
                    case blast_operation.pushc:
                        code_pointer++;
                        pushc(ref code_pointer, ref vector_size, ref datatype, ref f4_register);
                        code_pointer++;
                        break;
                    case blast_operation.push:
                        code_pointer++;
                        push(ref code_pointer, ref vector_size, ref datatype, ref f4_register);
                        code_pointer++;
                        break;
                    case blast_operation.pushf:
                        {
                            code_pointer++;

                            // pushf -> inlined so we may skip the push-pop pair 
                            get_function_result(ref code_pointer, ref vector_size, ref datatype, ref f4_register);
                            code_pointer++;

                            // save on branches by merging states 
                            switch (math.select(    vector_size,
                                                    255,
                                                    // if next up is (pop) then skip the push-pop altogehter 
                                                    ((blast_operation)code[code_pointer + 0] == blast_operation.begin &&
                                                    (blast_operation)code[code_pointer + 1] == blast_operation.pop &&
                                                    (blast_operation)code[code_pointer + 2] == blast_operation.end)))
                            {
                                case 1: push(f4_register.x, datatype); code_pointer++; break;
                                case 2: push(f4_register.xy, datatype); code_pointer++; break;
                                case 3: push(f4_register.xyz, datatype); code_pointer++; break;
                                case 4: push(f4_register, datatype); code_pointer++; break;

                                case 255:
                                    {
                                        // we save a stack operation -> compiler should not compile this sequence to start with...  TODO FIX flatten_condition
                                        // no need to do anything but skip the code_pointer 
                                        //  - save a branch by misusing the switch 
                                        code_pointer += 3;
                                        return;
                                    }
#if DEVELOPMENT_BUILD || TRACE
                                    default:
                                        Debug.LogError("blast.interpretor pushf error: variable vector size not yet supported on stack push");
                                        break;
#endif
                            }
                        }
                        break;
                    case blast_operation.pushv:
                        code_pointer++;
                        pushv(ref code_pointer, ref vector_size, ref datatype, ref f4_register);
                        code_pointer++;
                        break;
                    default:
                        is_push = false;
                        break;
                }
            }
            while (is_push && code_pointer < package.CodeSize);

            // get result from condition
            // compiler has put braces|compound around it in certain situations    TODO FIX
            // while they are not always needed 
            code_pointer = math.select(code_pointer, code_pointer + 1, (blast_operation)code[code_pointer] == blast_operation.begin);

            // if the next is pop followed by end, we hold f4_register and just decrease stackpointer 
            get_sequence_result(ref code_pointer, ref vector_size, ref datatype, ref f4_register);
        }



        /// <summary>
        /// 
        /// 
        /// </summary>
        /// <param name="blast"></param>
        /// <param name="environment"></param>
        /// <param name="caller"></param>
        /// <returns></returns>
        unsafe int execute([NoAlias] BlastEngineData* blast, [NoAlias] IntPtr environment, [NoAlias] IntPtr caller)
        {
            engine_ptr = blast;
            environment_ptr = environment;
            caller_ptr = caller;

            // init random state if needed 
            if (engine_ptr != null && random.state == 0)
            {
                random = Unity.Mathematics.Random.CreateFromIndex(blast->random.NextUInt());
            }

            // pointer to datasegment and 1 register 
            float* fdata = (float*)data;
            float4 f4_register = 0;

            // some variables to hold state 
            int index = -1;
            byte assignee = 0;
            byte s_assignee = 1;
            int cdata_offset = 0;
            int iterations = 0;

#if DEVELOPMENT_BUILD || TRACE
            // it should not be used except in trace builds 
            int cdata_length = 0;
#endif

            BlastVariableDataType assignee_type = BlastVariableDataType.Numeric;

            // main loop 
            while (code_pointer < package.CodeSize && iterations++ < max_iterations)
            {

                // default to Numeric[1]
                byte vector_size = 1;
                BlastVariableDataType datatype = BlastVariableDataType.Numeric; 

                // get operation 
                blast_operation op = (blast_operation)code[code_pointer];
                code_pointer++;

                // 
                //   determine if next op needs a compound result
                // - if compound needed determine offset into start 
                // - execute compound here inline 
                // - then switch further (codepointer needs different value) 
                //
                // this would eliminate the getcompoundresult function call 
                //


                //  if getcompound needed
                //   - copy codepointer
                //   - execute compound 
                //   - put back code pointer 
                //   - switch to operation 
                //   --- in that operation case we know it needed compound so there we advance codepointer

                switch (op)
                {
                    case blast_operation.ret:
                        ValidateOnce = false;
                        return (int)BlastError.success;

                    case blast_operation.nop:
                    case blast_operation.end:
                    case blast_operation.begin:
                        break;

                    case blast_operation.yield:
                        yield(f4_register);
                        return (int)BlastError.yield;

                    #region Stack

                    case blast_operation.push:
                        push(ref code_pointer, ref vector_size, ref datatype, ref f4_register);
                        break;

                    //
                    // pushv knows more about what it is pushing up the stack, it can do it without running the compound 
                    //
                    case blast_operation.pushv:
                        pushv(ref code_pointer, ref vector_size, ref datatype, ref f4_register);
                        break;

                    //
                    // push the result of a function directly onto the stack instead of assigning it first or something
                    // 
                    case blast_operation.pushf:
                        pushf(ref code_pointer, ref vector_size, ref datatype, ref f4_register);
                        break;

                    //
                    // push the result of a function directly onto the stack instead of assigning it first or something
                    //          
                    case blast_operation.pushc:
                        pushc(ref code_pointer, ref vector_size, ref datatype, ref f4_register);
                        break;

                    #endregion

                    #region Index & Assingments 

                    //
                    // compiler should send indexed assignements here in normal packaging
                    // - by first encoding the index we save a several branches performing indexed assignments 
                    // - when an index is set >= 0 then we MUST NOT forget to reset it to -1 as nothing will do that 
                    // 
                    case blast_operation.index_x: index = 0; goto case (blast_operation)blast_operation_jumptarget.jump_assign_indexed;
                    case blast_operation.index_y: index = 1; goto case (blast_operation)blast_operation_jumptarget.jump_assign_indexed;
                    case blast_operation.index_z: index = 2; goto case (blast_operation)blast_operation_jumptarget.jump_assign_indexed;
                    case blast_operation.index_w: index = 3; goto case (blast_operation)blast_operation_jumptarget.jump_assign_indexed;

                    case blast_operation.cdataref:
                        {
                            // this is an assignment of a cdataref, possibly indexed 
                            CDATAEncodingType encoding = CDATAEncodingType.None; 

#if DEVELOPMENT_BUILD || TRACE
                            follow_cdataref(code, code_pointer - 1, out cdata_offset, out cdata_length, out encoding);
#else
                            // dont care for lengths 
                            follow_cdataref(code, code_pointer - 1, out cdata_offset, out int length, out encoding); 
#endif

                            assignee_type = BlastVariableDataType.CData;
                            code_pointer += 2;

                        // on indexers: 
                        LABEL_INDEX_CDATAREF:
                            // switch to assign|indexer 
                            switch ((blast_operation)code[code_pointer])
                            {
                                case blast_operation.index_x: code_pointer++; index = 0; goto LABEL_INDEX_CDATAREF;
                                case blast_operation.index_y: code_pointer++; index = 1; goto LABEL_INDEX_CDATAREF;
                                case blast_operation.index_z: code_pointer++; index = 2; goto LABEL_INDEX_CDATAREF;
                                case blast_operation.index_w: code_pointer++; index = 3; goto LABEL_INDEX_CDATAREF;
                                case blast_operation.index_n:
                                    {
                                        // read indexer, must be vector_size 1 cant be nested cdata for now
                                        code_pointer++;
                                        V1 v1 = pop_f1(ref code_pointer, ref datatype);
                                        switch (datatype)
                                        {
                                            case BlastVariableDataType.Numeric:
                                                index = (int)v1; 
                                                break;

                                            case BlastVariableDataType.ID:
                                                index = CodeUtils.ReInterpret<V1, int>(v1);
                                                break;

                                            default:
                                                {
                                                    if(IsTrace) Debug.LogError($"Blast.Interpretor: assign cdataref - invalid assign operation: {code[code_pointer]} at codepointer: {code_pointer}, indexer datatype {datatype} not supported");
                                                    return (int)BlastError.error;
                                                }
                                        }
                                    }
                                    goto LABEL_INDEX_CDATAREF;

                                case blast_operation.assigns: goto LABEL_JUMP_ASSIGNS;
                                case blast_operation.assignf: goto LABEL_JUMP_ASSIGNF;
                                case blast_operation.assignfe: goto LABEL_JUMP_ASSIGNFE;
                                case blast_operation.assignfn: goto LABEL_JUMP_ASSIGNFN;
                                case blast_operation.assignfen: goto LABEL_JUMP_ASSIGNFEN;
                                case blast_operation.assign: goto LABEL_JUMP_ASSIGN;

#if DEVELOPMENT_BUILD || TRACE
                                default:
                                    {
                                        Debug.LogError($"Blast.Interpretor: assign cdataref - invalid assign operation: {code[code_pointer]} at codepointer: {code_pointer}");
                                    }
                                    return (int)BlastError.error;
#else
                                default: return (int)BlastError.error; 
#endif
                            }
                        }

                    case blast_operation.index_n:
                        {
                            // get index 
                            Debug.LogError("[] indexer found where none is expected");
                        }
                        goto case (blast_operation)blast_operation_jumptarget.jump_assign_indexed;

                    // *****************************************************************************************
                    // this opcode is NOT interpreted in the root and is used as jump target for other opcodes
                    // *****************************************************************************************
                    case (blast_operation)blast_operation_jumptarget.jump_assign_indexed:
                        {
                            // read data on assignee  (set s_assignee and assignee ) 
                            byte assignee_op = code[code_pointer];
                            code_pointer++;

                            // get metadata on destination 
                            assignee = (byte)(assignee_op - opt_id);
                            BlastInterpretor.GetMetaData(in metadata, in assignee, out s_assignee, out assignee_type);

                            // indexing a CDATAREF looks like this: idx_W cdataref #000 #029 sets 3.3
                            // but should look like                 cdataref #000 #029  idx_w  sets 3.3
                            // which saves us a branch at every assign operation 
                            // - so on compiling an assignment to a CDATAREF we compile the ref first

                            // next is the acual assignment operation 
                            switch ((blast_operation)code[code_pointer])
                            {
                                case blast_operation.assigns: goto LABEL_JUMP_ASSIGNS;
                                case blast_operation.assignf: goto LABEL_JUMP_ASSIGNF;
                                case blast_operation.assignfe: goto LABEL_JUMP_ASSIGNFE;
                                case blast_operation.assignfn: goto LABEL_JUMP_ASSIGNFN;
                                case blast_operation.assignfen: goto LABEL_JUMP_ASSIGNFEN;
                                case blast_operation.assign: goto LABEL_JUMP_ASSIGN;

#if DEVELOPMENT_BUILD || TRACE
                                default:
                                    {
                                        Debug.LogError($"Blast.Interpretor: jump_assign_indexed - invalid assign operation: {code[code_pointer]} at codepointer: {code_pointer}");
                                    }
                                    return (int)BlastError.error;
#else
                                default: return (int)BlastError.error; 
#endif
                            }
                        }

                    //
                    // Assign single value/vector 
                    // - assignS assigns 1 datapoint to a assignee, from stack or other data that is possibly indexed 
                    //

                    // entry for non indexed assignee
                    case blast_operation.assigns:
                        {
                            // assign 1 value to destination: assignee 
                            byte assignee_op = code[code_pointer];

                            // get metadata on destination 
                            assignee = (byte)(assignee_op - opt_id);
                            s_assignee = BlastInterpretor.GetMetaDataSize(in metadata, in assignee);
                        }
                        //
                        // this is a short fixed non conditional jump and should be very fast without any stack operations 
                        // todo: would be nice if burst just doesnt jump and rolls into jump_assigns below
                        //
                        goto case (blast_operation)blast_operation_jumptarget.jump_assigns;

                    // continue assignin a single vector 
                    case (blast_operation)blast_operation_jumptarget.jump_assigns:
                    LABEL_JUMP_ASSIGNS:
                        {
                            // and pop the source (which might be indexed) 
                            BlastVariableDataType datatype_datasource;

                            code_pointer++;
                            f4_register = ((float*)pop_with_info_indexed(ref code_pointer, out datatype_datasource, out vector_size))[0];

#if DEVELOPMENT_BUILD || TRACE
                            // the compiler should have cought this, if it didt wel that would be noticed before going in release
                            if (vector_size != 1 && index >= 0)
                            {
                                Debug.LogError($"blast.assignsingle: cannot set component from vector at #{code_pointer}, component: {index}");
                                return (int)BlastError.error_assign_component_from_vector;
                            }
#endif
                            // unconditional jumps are very fast and have no stack operations as opposed to function calls
                            goto case (blast_operation)blast_operation_jumptarget.jump_assign_result;
                        }

                    // 
                    // set the result of an assign operation 
                    //
                    // note: AUTO_EXPAND is now enabled forever and not optional anymore 
                    //
                    case (blast_operation)blast_operation_jumptarget.jump_assign_result:
                        {
                        // save on branches combining the datatype with vectorsize in switch 
                        switch ((byte)(assignee_type != BlastVariableDataType.CData ? (byte)assignee_type.Combine(vector_size, false) : 255))
                        {

                                case (byte)BlastVectorType.bool32:
                                case (byte)BlastVectorType.float1:
                                    {

                                        //
                                        // datatype now contains the datatype set to the register, if its different
                                        //   from that in metadata we have to cast the data into float after reinterpreting the register as the other type
                                        // 
                                        // while added in integersupport this at the same time reduces the branche count by 1 (even while supporting more datatypes)
                                        //
                                        // 

                                        switch (datatype.Combine(s_assignee, index >= 0))
                                        {
                                            case BlastVectorType.float1_n:
                                            case BlastVectorType.float2_n:
                                            case BlastVectorType.float3_n:
                                            case BlastVectorType.float4_n:
                                            case BlastVectorType.bool32_n:
                                                {
                                                    // indexer active 
                                                    // if an indexer is set, then get offset (x = 0 , y = 1 etc) and add that to assignee
                                                    int offset = assignee + index;

                                                    // because assignee offsets into the datasegment we can just add the vector component index to that offset 
                                                    fdata[offset] = f4_register[0];
                                                }
                                                break;

                                            case BlastVectorType.int1_n:
                                            case BlastVectorType.int2_n:
                                            case BlastVectorType.int3_n:
                                            case BlastVectorType.int4_n: fdata[assignee + index] = (float)CodeUtils.ReInterpret<float, int>(f4_register[0]); break;

                                            // assign float1 to float1
                                            case BlastVectorType.float1: fdata[assignee] = f4_register[0]; break;
                                            case BlastVectorType.float2: fdata[assignee] = f4_register[0]; fdata[assignee + 1] = f4_register[0]; break;
                                            case BlastVectorType.float3: fdata[assignee] = f4_register[0]; fdata[assignee + 1] = f4_register[0]; fdata[assignee + 2] = f4_register[0]; break;
                                            case BlastVectorType.float4: fdata[assignee] = f4_register[0]; fdata[assignee + 1] = f4_register[0]; fdata[assignee + 2] = f4_register[0]; fdata[assignee + 3] = f4_register[0]; break;

                                            // assign int to float backed 
                                            case BlastVectorType.int1: fdata[assignee] = (float)CodeUtils.ReInterpret<float, int>(f4_register[0]); break;
                                            case BlastVectorType.int2: ((float2*)(void*)&fdata[assignee])[0] = (float)CodeUtils.ReInterpret<float, int>(f4_register[0]); break;
                                            case BlastVectorType.int3: ((float3*)(void*)&fdata[assignee])[0] = (float)CodeUtils.ReInterpret<float, int>(f4_register[0]); break;
                                            case BlastVectorType.int4: ((float4*)(void*)&fdata[assignee])[0] = (float)CodeUtils.ReInterpret<float, int>(f4_register[0]); break;

                                            // a bit pattern would remain the same in memory 
                                            case BlastVectorType.bool32: fdata[assignee] = f4_register[0]; break;

                                            default:
#if DEVELOPMENT_BUILD || TRACE
                                                Debug.LogError($"Blast.Interpretor.assign_result: assigned vector size mismatch at #{code_pointer}, should be size '{s_assignee}', evaluated '{vector_size}', data id = {assignee}");
#endif
                                                return (int)BlastError.error_assign_vector_size_mismatch;
                                        }
                                    }
                                    break;


                                case (byte)BlastVectorType.float2:

                                    // a size2 vector can only assign to other size2 vectors 
                                    // - still we need to switch on datatype assigned
                                    switch (datatype.Combine(s_assignee, false))
                                    {
                                        // f2 = f2 
                                        case BlastVectorType.float2: ((float2*)(void*)&fdata[assignee])[0] = ((float2*)(void*)&f4_register)[0]; break;
                                        // f2 = i2
                                        case BlastVectorType.int2: ((float2*)(void*)&fdata[assignee])[0] = (float2)CodeUtils.ReInterpret<float2, int2>(((float2*)(void*)&f4_register)[0]); break;

                                        default:
#if DEVELOPMENT_BUILD || TRACE
                                            Debug.LogError($"Blast.Interpretor.assign_result: assigned vector size mismatch at #{code_pointer}, should be size '{s_assignee}', evaluated '2'");
#endif
                                            return (int)BlastError.error_assign_vector_size_mismatch;
                                    }
                                    break;


                                case (byte)BlastVectorType.float3:
                                    switch (datatype.Combine(s_assignee, false))
                                    {
                                        // f3 = f3
                                        case BlastVectorType.float3: ((float3*)(void*)&fdata[assignee])[0] = ((float3*)(void*)&f4_register)[0]; break;
                                        // f3 = i3
                                        case BlastVectorType.int3: ((float3*)(void*)&fdata[assignee])[0] = (float3)CodeUtils.ReInterpret<float3, int3>(((float3*)(void*)&f4_register)[0]); break;
                                        default:
#if DEVELOPMENT_BUILD || TRACE
                                            Debug.LogError($"Blast.Interpretor.assign_result: assigned vector size mismatch at #{code_pointer}, should be size '{s_assignee}', evaluated '3'");
#endif
                                            return (int)BlastError.error_assign_vector_size_mismatch;
                                    }
                                    break;

                                case (byte)BlastVectorType.float4:
                                    switch (datatype.Combine(s_assignee, false))
                                    {
                                        // f4 = f4
                                        case BlastVectorType.float4: ((float4*)(void*)&fdata[assignee])[0] = f4_register; break;
                                        // f4 = i4
                                        case BlastVectorType.int4: ((float4*)(void*)&fdata[assignee])[0] = (float4)CodeUtils.ReInterpret<float4, int4>(f4_register); break;
                                        default:
#if DEVELOPMENT_BUILD || TRACE
                                            Debug.LogError($"Blast.Interpretor.assign_result: assigned vector size mismatch at #{code_pointer}, should be size '{s_assignee}', evaluated '4'");
#endif
                                            return (int)BlastError.error_assign_vector_size_mismatch;
                                    }
                                    break; 



                                case (byte)BlastVectorType.int1:
                                    {
                                        switch (datatype.Combine(s_assignee, index >= 0))
                                        {
                                            case BlastVectorType.float1_n:
                                            case BlastVectorType.float2_n:
                                            case BlastVectorType.float3_n:
                                            case BlastVectorType.float4_n:
                                            case BlastVectorType.bool32_n:
                                                {
                                                    // if an indexer is set, then get offset (x = 0 , y = 1 etc) and add that to assignee
                                                    int offset = assignee + index;

                                                    // because assignee offsets into the datasegment we can just add the vector component index to that offset 
                                                    fdata[offset] = (float)CodeUtils.ReInterpret<float, int>(f4_register[0]);
                                                }
                                                break;

                                             // int = int[index]
                                            case BlastVectorType.int1_n:
                                            case BlastVectorType.int2_n:
                                            case BlastVectorType.int3_n:
                                            case BlastVectorType.int4_n: fdata[assignee] = f4_register[0]; break;

                                            // assign float to int1 float backed 
                                            case BlastVectorType.float1: fdata[assignee] = CodeUtils.ReInterpret<int, float>((int)f4_register[0]); break;
                                            case BlastVectorType.float2: ((float2*)(void*)&fdata[assignee])[0] = CodeUtils.ReInterpret<int, float>((int)f4_register[0]); break;
                                            case BlastVectorType.float3: ((float3*)(void*)&fdata[assignee])[0] = CodeUtils.ReInterpret<int, float>((int)f4_register[0]); break;
                                            case BlastVectorType.float4: ((float4*)(void*)&fdata[assignee])[0] = CodeUtils.ReInterpret<int, float>((int)f4_register[0]); break;

                                            // assign int to int float backed 
                                            case BlastVectorType.int1: fdata[assignee] = f4_register[0]; break;
                                            case BlastVectorType.int2: ((float2*)(void*)&fdata[assignee])[0] = f4_register[0]; break;
                                            case BlastVectorType.int3: ((float3*)(void*)&fdata[assignee])[0] = f4_register[0]; break;
                                            case BlastVectorType.int4: ((float4*)(void*)&fdata[assignee])[0] = f4_register[0]; break;

                                            // a bit pattern would remain the same in memory 
                                            case BlastVectorType.bool32: fdata[assignee] = f4_register[0]; break;

                                            default:
#if DEVELOPMENT_BUILD || TRACE
                                                Debug.LogError($"Blast.Interpretor.assign_result: assigned vector size mismatch at #{code_pointer}, should be size '{s_assignee}', evaluated '{vector_size}', data id = {assignee}");
#endif
                                                return (int)BlastError.error_assign_vector_size_mismatch;
                                        }
                                    }
                                    break;


                                case (byte)BlastVectorType.int2:
                                    switch (datatype.Combine(s_assignee, false))
                                    {
                                        // i2 = f2 
                                        case BlastVectorType.float2: ((float2*)(void*)&fdata[assignee])[0] = CodeUtils.ReInterpret<int2, float2>((int2)((float2*)(void*)&f4_register)[0]); break;
                                        // i2 = i2
                                        case BlastVectorType.int2: ((float2*)(void*)&fdata[assignee])[0] =  ((float2*)(void*)&f4_register)[0]; break;

                                        default:
#if DEVELOPMENT_BUILD || TRACE
                                            Debug.LogError($"Blast.Interpretor.assign_result: assigned vector size mismatch at #{code_pointer}, should be size '{s_assignee}', evaluated '2'");
#endif
                                            return (int)BlastError.error_assign_vector_size_mismatch;
                                    }
                                    break;

                                case (byte)BlastVectorType.int3:
                                    switch (datatype.Combine(s_assignee, false))
                                    {
                                        // i3 = f3 
                                        case BlastVectorType.float3: ((float3*)(void*)&fdata[assignee])[0] = CodeUtils.ReInterpret<int3, float3>((int3)((float3*)(void*)&f4_register)[0]); break;
                                        // i3 = i3
                                        case BlastVectorType.int3: ((float3*)(void*)&fdata[assignee])[0] = ((float3*)(void*)&f4_register)[0]; break;

                                        default:
#if DEVELOPMENT_BUILD || TRACE
                                            Debug.LogError($"Blast.Interpretor.assign_result: assigned vector size mismatch at #{code_pointer}, should be size '{s_assignee}', evaluated '2'");
#endif
                                            return (int)BlastError.error_assign_vector_size_mismatch;
                                    }
                                    break;

                                case (byte)BlastVectorType.int4:
                                    switch (datatype.Combine(s_assignee, false))
                                    {
                                        // i4 = f4 
                                        case BlastVectorType.float4: ((float4*)(void*)&fdata[assignee])[0] = CodeUtils.ReInterpret<int4, float4>((int4)((float4*)(void*)&f4_register)[0]); break;
                                        // i4 = i4
                                        case BlastVectorType.int4: ((float4*)(void*)&fdata[assignee])[0] = ((float4*)(void*)&f4_register)[0]; break;

                                        default:
#if DEVELOPMENT_BUILD || TRACE
                                            Debug.LogError($"Blast.Interpretor.assign_result: assigned vector size mismatch at #{code_pointer}, should be size '{s_assignee}', evaluated '2'");
#endif
                                            return (int)BlastError.error_assign_vector_size_mismatch;
                                    }
                                    break;

                                case 255:
                                    {
                                        // cdata assignment of something with vector_size 
                                        // - all cdata assignments at this point should be indexed so vector_size SHOULD BE 1 
#if DEVELOPMENT_BUILD || TRACE
                                        if (index > GetCDATAElementCount(code, cdata_offset))
                                        {
                                            Debug.LogError($"Blast.Interpretor.assign_result: assigned cdata index {index} is out of bounds for cdata at {cdata_offset} with byte length {cdata_length}");
                                            return (int)BlastError.error_indexer_out_of_bounds;
                                        }
#endif
                                        set_cdata_float(code, cdata_offset, index, f4_register.x);
                                    }
                                    break;


                                default:
#if DEVELOPMENT_BUILD || TRACE
                                    Debug.LogError($"Blast.Interpretor.assign_result: vector size {vector_size} not allowed at codepointer {code_pointer}");
#endif
                                    return (int)BlastError.error_unsupported_operation_in_root;
                            }

                            // reset the assigned type and indexer after setting result 
                            assignee_type = BlastVariableDataType.Numeric;
                            index = -1;

                            code_pointer++;
                        }
                        break;

                    //
                    // assign the result of a function directly to the assignee
                    //
                    case blast_operation.assignf:
                        {
                            byte assignee_op = code[code_pointer];
                            assignee = (byte)(assignee_op - opt_id);
                        }
                        goto case (blast_operation)blast_operation_jumptarget.jump_assignf;


                    case (blast_operation)blast_operation_jumptarget.jump_assignf:
                    LABEL_JUMP_ASSIGNF:
                        {
                            // assignee known get result from function 
                            s_assignee = BlastInterpretor.GetMetaDataSize(in metadata, in assignee);
                            code_pointer++;
                            get_function_result(ref code_pointer, ref vector_size, ref datatype, ref f4_register);
                        }
                        goto case (blast_operation)blast_operation_jumptarget.jump_assign_result;


                    //
                    // assign the negated result of a function directly to the assignee
                    //
                    case blast_operation.assignfn:
                        {
                            byte assignee_op = code[code_pointer];
                            assignee = (byte)(assignee_op - opt_id);
                        }
                        goto case (blast_operation)blast_operation_jumptarget.jump_assignfn;


                    case (blast_operation)blast_operation_jumptarget.jump_assignfn:
                    LABEL_JUMP_ASSIGNFN:
                        {
                            // assignee known get result from function 
                            s_assignee = BlastInterpretor.GetMetaDataSize(in metadata, in assignee);
                            code_pointer++;
                            get_function_result(ref code_pointer, ref vector_size, ref datatype, ref f4_register);
                            f4_register = -f4_register;
                        }
                        goto case (blast_operation)blast_operation_jumptarget.jump_assign_result;


                    //
                    // assign the resultof a function directly to the assignee
                    //
                    case blast_operation.assignfe:
                        {
                            byte assignee_op = code[code_pointer];
                            assignee = (byte)(assignee_op - opt_id);
                        }
                        goto case (blast_operation)blast_operation_jumptarget.jump_assignfe;


                    case (blast_operation)blast_operation_jumptarget.jump_assignfe:
                    LABEL_JUMP_ASSIGNFE:
                        {
                            s_assignee = BlastInterpretor.GetMetaDataSize(in metadata, in assignee);
                            code_pointer++;
                            CallExternalFunction(ref code_pointer, ref vector_size, out f4_register);
                        }
                        goto case (blast_operation)blast_operation_jumptarget.jump_assign_result;


                    //
                    // assign the resultof a function directly to the assignee
                    //
                    case blast_operation.assignfen:
                        {
                            byte assignee_op = code[code_pointer];
                            assignee = (byte)(assignee_op - opt_id);
                        }
                        goto case (blast_operation)blast_operation_jumptarget.jump_assignfen;


                    case (blast_operation)blast_operation_jumptarget.jump_assignfen:
                    LABEL_JUMP_ASSIGNFEN:
                        {
                            s_assignee = BlastInterpretor.GetMetaDataSize(in metadata, in assignee);
                            code_pointer++;
                            CallExternalFunction(ref code_pointer, ref vector_size, out f4_register);
                            f4_register = -f4_register;
                        }
                        goto case (blast_operation)blast_operation_jumptarget.jump_assign_result;


                    //
                    // assign a datapoint from vector elements, much like pushv does but then to stack.. lots of almost duplicate code that we dont want control flow for..  
                    // 
                    case blast_operation.assignv:
                        {
                            // assignv cannot set a vector in an indexed thing and thus works like this
                            byte assignee_op = code[code_pointer];

                            bool is_indexed = assignee_op < opt_id;
                            if (is_indexed)
                            {
                                //compile 
                                if (IsTrace)
                                {
                                    Debug.LogError($"blast.assignv: cannot set component from vector at #{code_pointer}");
                                }
                                return (int)BlastError.error_assign_component_from_vector;
                            }

                            assignee = (byte)(assignee_op - opt_id);
                           
                            code_pointer++;
                            assignv(ref code_pointer, BlastInterpretor.GetMetaDataVectorType(in metadata, in assignee), &fdata[assignee]);
                        }
                        break;

                    //
                    // The one, expensive operation we want to avoid: Assign result of compound
                    // -> after all refactoring and optimizing this actually became fast.. 
                    case blast_operation.assign:
                        {
                            // advance 1 if on assign 
                            code_pointer += math.select(0, 1, code[code_pointer] == (byte)blast_operation.assign);
                            byte assignee_op = code[code_pointer];

                            // get any index, branch free
                            bool is_indexed = assignee_op < opt_id;
                            blast_operation indexer = (blast_operation)math.select(0, assignee_op, is_indexed);
                            code_pointer = math.select(code_pointer, code_pointer + 1, is_indexed);

                            assignee_op = code[code_pointer];
                            assignee = (byte)(assignee_op - opt_id);

                            goto LABEL_JUMP_ASSIGN;
                        }
                    //
                    // :0  this gives a fairly clean path nevertheless 
                    // 
                    LABEL_JUMP_ASSIGN:
                        {
                            s_assignee = BlastInterpretor.GetMetaDataSize(in metadata, in assignee);

                            code_pointer++;
                            get_sequence_result(ref code_pointer, ref vector_size, ref datatype, ref f4_register);

                            code_pointer--;

                            goto case (blast_operation)blast_operation_jumptarget.jump_assign_result;
                        }

                    #endregion

                    #region Jumps 

                    //
                    // JZ Long: a condition may contain push commands that should run from the root
                    // 
                    case blast_operation.jz_long:
                        {
                            // calc endlocation of 
                            int offset = ((short)(code[code_pointer] << 8) + code[code_pointer + 1]);
                            int jump_to = code_pointer + offset - 1;

                            // eval condition 
                            code_pointer += math.select(2, 3, code[code_pointer + 2] == (byte)blast_operation.begin);

                            EvalPushedJumpCondition(ref code_pointer, ref f4_register, ref vector_size, ref datatype);

                            // jump if zero to else condition or after then when no else 
                            code_pointer = math.select(code_pointer, jump_to, f4_register.x == 0);

                        }
                        break;

                    //
                    // JZ Short: a condition may contain push commands that should run from the root
                    // 
                    case blast_operation.jz:
                        {
                            // calc endlocation of 
                            int offset = code[code_pointer];
                            int jump_to = code_pointer + offset;

                            // eval condition 
                            code_pointer += math.select(1, 2, code[code_pointer + 1] == (byte)blast_operation.begin);

                            EvalPushedJumpCondition(ref code_pointer, ref f4_register, ref vector_size, ref datatype);

                            // jump if zero to else condition or after then when no else 
                            code_pointer = math.select(code_pointer, jump_to, f4_register.x == 0);
                        }
                        break;

                    case blast_operation.jnz:
                        {
                            // calc endlocation of 
                            int offset = code[code_pointer];
                            int jump_to = code_pointer + offset;

                            // eval condition => TODO -> check if we still write the begin, if not we could skip this select
                            code_pointer += math.select(1, 2, code[code_pointer + 1] == (byte)blast_operation.begin);

                            EvalPushedJumpCondition(ref code_pointer, ref f4_register, ref vector_size, ref datatype);

                            // jump if NOT zero to else condition or after then when no else 
                            code_pointer = math.select(code_pointer, jump_to, f4_register.x != 0);
                        }
                        break;

                    case blast_operation.jnz_long:
                        {
                            // calc endlocation of 
                            int offset = ((short)(code[code_pointer] << 8) + code[code_pointer + 1]);
                            int jump_to = code_pointer + offset - 1;

                            // eval condition => TODO -> check if we still write the begin, if not we could skip this select
                            code_pointer += math.select(2, 3, code[code_pointer + 2] == (byte)blast_operation.begin);

                            EvalPushedJumpCondition(ref code_pointer, ref f4_register, ref vector_size, ref datatype);

                            // jump if NOT zero to else condition or after then when no else 
                            code_pointer = math.select(code_pointer, jump_to, f4_register.x != 0);
                        }
                        break;

                    // non-conditional jump forward
                    case blast_operation.jump:
                        {
                            int offset = code[code_pointer];
                            code_pointer = code_pointer + offset;
                            break;
                        }

                    // non-conditional jumb backward
                    case blast_operation.jump_back:
                        {
                            int offset = code[code_pointer];
                            code_pointer = code_pointer - offset;
                            break;
                        }

                    // long-jump, singed (forward && backward)
                    case blast_operation.long_jump:
                        {
                            short offset = (short)((code[code_pointer] << 8) + code[code_pointer + 1]);
                            code_pointer = code_pointer + offset;
                            break;
                        }

                    #endregion

                    #region Root Procedures

                    //    
                    // by executing these in the root we save a lot on function calls and stack operations
                    // 



                    case blast_operation.zero:
                        code_pointer--;
                        CALL_ZERO(ref code_pointer, ref vector_size);
                        break;
                    #endregion


                    //
                    // Extended Op == 255 => next byte encodes an operation not used in switch above (more non standard ops)
                    //
                    case blast_operation.ex_op:
                        {
                            extended_blast_operation exop = (extended_blast_operation)code[code_pointer];
                            switch (exop)
                            {
                                // 
                                // directly call procedures encoded instead of using get_function_result 
                                // - the compiler should not allow functions to be called without them returning their result into something 
                                // 
                                case extended_blast_operation.reverse_bits:
                                    {
                                        get_reversebits_result(ref code_pointer, ref vector_size);
                                    }
                                    break;

                                case extended_blast_operation.set_bit:
                                    get_setbit_result(ref code_pointer, ref vector_size);
                                    break;

                                case extended_blast_operation.set_bits:
                                    get_setbits_result(ref code_pointer, ref vector_size);
                                    break;


                                case extended_blast_operation.rol:
                                    {
                                        get_rol_result(ref code_pointer, ref vector_size);
                                    }
                                    break;

                                case extended_blast_operation.ror:
                                    {
                                        get_ror_result(ref code_pointer, ref vector_size);
                                    }
                                    break;

                                case extended_blast_operation.shl:
                                    {
                                        get_shl_result(ref code_pointer, ref vector_size);
                                    }
                                    break;

                                case extended_blast_operation.shr:
                                    {
                                        get_shr_result(ref code_pointer, ref vector_size);
                                    }
                                    break;

                                case extended_blast_operation.reinterpret_bool32:
                                    {
                                        reinterpret_bool32(ref code_pointer, ref vector_size);
                                    }
                                    break;

                                case extended_blast_operation.reinterpret_float:
                                    {
                                        reinterpret_float(ref code_pointer, ref vector_size);
                                    }
                                    break;

                                case extended_blast_operation.reinterpret_int32:
                                    {
                                        reinterpret_int32(ref code_pointer, ref vector_size);
                                    }
                                    break;

                                case extended_blast_operation.call:
                                    {
                                        CallExternalFunction(ref code_pointer, ref vector_size, out f4_register);
                                        code_pointer++;
                                    }
                                    break;

                                case extended_blast_operation.send:
                                    {
                                        Handle_Send(ref code_pointer);
                                        break;
                                    }

                                case extended_blast_operation.seed:
                                    code_pointer++;
                                    f4_register.x = pop_as_f1(ref code_pointer);
                                    random = Unity.Mathematics.Random.CreateFromIndex((uint)f4_register.x);
                                    break;

                                case extended_blast_operation.validate:
                                    {
                                        RunValidate(ref code_pointer);
                                    }
                                    break;

                                case extended_blast_operation.debugstack:
                                    {
                                        // write contents of stack to the debug stream as a detailed report; 
                                        // write contents of a data element to the debug stream 
                                        code_pointer++;
#if DEVELOPMENT_BUILD || TRACE
                                        if (!ValidateOnce)
                                        {
                                            Handle_DebugStack();
                                        }
#endif
                                    }
                                    break;

                                case extended_blast_operation.debug:
                                    {
                                        // write contents of a data element to the debug stream 
                                        code_pointer++;

                                        int op_id = code[code_pointer];

                                        if ((blast_operation)op_id == blast_operation.cdataref)
                                        {
                                            if (!ValidateOnce)
                                            {
                                                Handle_DebugData_CData(code_pointer);
                                            }
                                            code_pointer += 3;
                                        }
                                        else
                                        {
                                            // even if not in debug, if the command is encoded any stack op needs to be popped 
                                            bool is_negated;
                                            void* pdata = pop_p_info(ref code_pointer, out datatype, out vector_size, out is_negated);

#if DEVELOPMENT_BUILD || TRACE
                                            if (!ValidateOnce)
                                            {
                                                Handle_DebugData(code_pointer, vector_size, op_id, datatype, pdata, is_negated);
                                            }
#endif
                                        }
                                    }
                                    break;

                                default:
                                    {
#if DEVELOPMENT_BUILD || TRACE
                                        Debug.LogError($"extended op: only call and debug operation is supported from root, not {exop} ");
#endif
                                        return (int)BlastError.error_unsupported_operation_in_root;
                                    }
                            }
                            break;
                        }

                    default:
                        {
#if DEVELOPMENT_BUILD || TRACE
                            Debug.LogError($"blast: operation {(byte)op} '{op}' not supported in root codepointer = {code_pointer}");
#endif

                        }
                        return (int)BlastError.error_unsupported_operation_in_root;
                }
            }

            ValidateOnce = false;

            // by combining iterations with the success state and codeindex check we save a brach each main loop 
            return math.select((int)BlastError.success, (int)BlastError.error_max_iterations, iterations >= max_iterations); 
        }

        #endregion

    }
}


