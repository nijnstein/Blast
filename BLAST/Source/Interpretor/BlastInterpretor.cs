﻿//##########################################################################################################
// Copyright © 2022 Rob Lemmens | NijnStein Software <rob.lemmens.s31@gmail.com> All Rights Reserved  ^__^\#
// Unauthorized copying of this file, via any medium is strictly prohibited                           (oo)\#
// Proprietary and confidential                                                                       (__) #
//##########################################################################################################
#if STANDALONE_VSBUILD
    using NSS.Blast.Standalone; 
#else 
    using UnityEngine;
#endif 

using System;
using System.Runtime.CompilerServices;
using Unity.Burst;
using UnityEngine.Assertions;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

namespace NSS.Blast.Interpretor
{

    /// <summary>
    /// 
    /// </summary>
#if !STANDALONE_VSBUILD
    [BurstCompile]
#endif

    unsafe public struct BlastInterpretor
    {
        /// <summary>
        /// >= if the opcode between opt_value and (including) opt_id then its an opcode for a constant
        /// </summary>
        public const byte opt_value = (byte)blast_operation.pi;

        /// <summary>
        /// >= opt_id is starting opcode for parameters, everything until 255|ExOp is considered a datasegment index
        /// </summary>
        public const byte opt_id = (byte)blast_operation.id;

        /// <summary>
        /// maxiumum iteration count, usefull to avoid endless loop bugs
        /// </summary>
        public const int max_iterations = 10000;

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
        /// metadata segment pointer
        /// </summary>
        [NoAlias]
        [NativeDisableUnsafePtrRestriction]
        internal unsafe byte* metadata;

        /// <summary>
        /// if true, the script is executed in validation mode:
        /// - external calls just return 0's
        /// </summary>
        public bool ValidationMode;

        #region Reset & Set Package

        /// <summary>
        /// reset code_pointer and stack_offset to their initial states 
        /// </summary>
        /// <param name="blast">pointer to blast engine data</param>
#pragma warning disable CS1573 // Parameter 'pkg' has no matching param tag in the XML comment for 'BlastInterpretor.Reset(BlastEngineData*, BlastPackageData)' (but other parameters do)
        public void Reset([NoAlias]BlastEngineData* blast, BlastPackageData pkg)
#pragma warning restore CS1573 // Parameter 'pkg' has no matching param tag in the XML comment for 'BlastInterpretor.Reset(BlastEngineData*, BlastPackageData)' (but other parameters do)
        {
            engine_ptr = blast;
            SetPackage(pkg);
        }

        /// <summary>
        /// we might need to know if we need to copy back data  (or we use a dummy)
        /// </summary>
        public void SetPackage(BlastPackageData pkg)
        {
            package = pkg;

            // the compiler supllies its own pointers to various segments 
            if (pkg.PackageMode != BlastPackageMode.Compiler)
            {
                code = package.Code;
                data = package.Data;
                metadata = package.Metadata;
            }
            code_pointer = 0;

            // stack starts at an aligned point in the datasegment
            stack_offset = package.DataSegmentStackOffset / 4; // package talks in bytes, interpretor uses elements in data made of 4 bytes 
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
            package = pkg;
            code = _code;
            data = _data;
            metadata = _metadata;

            code_pointer = 0;
            stack_offset = initial_stack_offset;
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
        unsafe int execute([NoAlias]BlastEngineData* blast, [NoAlias]IntPtr environment, [NoAlias]IntPtr caller, bool resume_state)
        {
            float4 register = float4.zero;

            // yield state or reset it otherwise
            if (resume_state & peek(1))
            {
                // resume execution, pop frame counter 
                pop(out register.x);

                if (register.x > 0)
                {
                    register--;
                    push(register);
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
        public int Execute([NoAlias]BlastEngineDataPtr blast, [NoAlias]IntPtr environment, [NoAlias]IntPtr caller)
        {
            return execute(blast.Data, environment, caller, false);
        }

        /// <summary>
        /// execute bytecode set to the interpretor 
        /// </summary>
        public int Execute([NoAlias]BlastEngineDataPtr blast)
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
        public int ResumeYield([NoAlias]BlastEngineDataPtr blast, [NoAlias]IntPtr environment, [NoAlias]IntPtr caller)
        {
            return execute(blast.Data, environment, caller, true);
        }

        /// <summary>
        /// resume executing bytecode after yielding
        /// </summary>
        public int ResumeYield([NoAlias]BlastEngineDataPtr blast)
        {
            return execute(blast.Data, IntPtr.Zero, IntPtr.Zero, true);
        }
        #endregion

        #region Stack 

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void push(float f)
        {
            // set stack value 
            ((float*)data)[stack_offset] = f;

            // update metadata with type set 
            SetMetaData(metadata, BlastVariableDataType.Numeric, 1, (byte)stack_offset);

            // advance 1 element in stack offset 
            stack_offset += 1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void push(float2 f2)
        {
            // really... wonder how this compiles (should be simple 8byte move) TODO 
            ((float2*)(void*)&((float*)data)[stack_offset])[0] = f2;

            SetMetaData(metadata, BlastVariableDataType.Numeric, 2, (byte)stack_offset);

            stack_offset += 2;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void push(float3 f3)
        {
            ((float3*)(void*)&((float*)data)[stack_offset])[0] = f3;

            SetMetaData(metadata, BlastVariableDataType.Numeric, 3, (byte)stack_offset);

            stack_offset += 3;  // size 3
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void push(float4 f4)
        {
            // really... wonder how this compiles (should be simple 16byte move) TODO 
            ((float4*)(void*)&((float*)data)[stack_offset])[0] = f4;

            SetMetaData(metadata, BlastVariableDataType.Numeric, 4, (byte)stack_offset);

            stack_offset += 4;  // size 4
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void push(int i)
        {
#if DEVELOPMENT_BUILD || TRACE || CHECK_STACK
            if (stack_offset + 1 > package.StackCapacity)
            {
                Debug.LogError($"blast.stack: attempting to push [{i}] onto a full stack, stack capacity = {package.StackCapacity}");
                return;
            }
#endif
            ((int*)data)[stack_offset] = i;
            SetMetaData(metadata, BlastVariableDataType.ID, 1, (byte)stack_offset);
            stack_offset += 1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void pop(out float f)
        {
            stack_offset = stack_offset - 1;

#if DEVELOPMENT_BUILD || TRACE || CHECK_STACK
            if (stack_offset < 0)
            {
                Debug.LogError($"blast.stack: attempting to pop too much data (1 float) from the stack, offset = {stack_offset}");
                f = float.NaN;
                return;
            }
            if (GetMetaDataType(metadata, (byte)stack_offset) != BlastVariableDataType.Numeric)
            {
                Debug.LogError($"blast.stack: pop(float) type mismatch, stack contains Integer/ID datatype");
                f = float.NaN;
                return;
            }
            byte size = GetMetaDataSize(metadata, (byte)stack_offset);
            if (size != 1)
            {
                Debug.LogError($"blast.stack: pop(float) size mismatch, stack contains vectorsize {size} datatype while size 1 is expected");
                f = float.NaN;
                return;
            }
#endif

            f = ((float*)data)[stack_offset];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void pop(out int i)
        {
            stack_offset = stack_offset - 1;

#if DEVELOPMENT_BUILD || TRACE || CHECK_STACK
            if (stack_offset < 0)
            {
                Debug.LogError($"blast.stack: attempting to pop too much data (1 int) from the stack, offset = {stack_offset}");
                i = int.MinValue;
                return;
            }
            if (GetMetaDataType(metadata, (byte)stack_offset) != BlastVariableDataType.ID)
            {
                Debug.LogError($"blast.stack: pop(int) type mismatch, stack contains Numeric datatype");
                i = int.MinValue;
                return;
            }
            byte size = GetMetaDataSize(metadata, (byte)stack_offset);
            if (size != 1)
            {
                Debug.LogError($"blast.stack: pop(int) size mismatch, stack contains vectorsize {size} datatype while size 1 is expected");
                i = int.MinValue;
                return;
            }
#endif

            i = ((int*)data)[stack_offset];
        }



        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void pop(out float4 f)
        {
            stack_offset = stack_offset - 4;

#if DEVELOPMENT_BUILD || TRACE || CHECK_STACK
            if (stack_offset < 0)
            {
                Debug.LogError($"blast.stack: attempting to pop too much data (4 numerics) from the stack, offset = {stack_offset}");
                f = float.NaN;
                return;
            }
            if (GetMetaDataType(metadata, (byte)stack_offset) != BlastVariableDataType.Numeric)
            {
                Debug.LogError($"blast.stack: pop(numeric4) type mismatch, stack contains ID datatype");
                f = float.NaN;
                return;
            }
            byte size = GetMetaDataSize(metadata, (byte)stack_offset);
            if (size != 4)
            {
                Debug.LogError($"blast.stack: pop(numeric4) size mismatch, stack contains vectorsize {size} datatype while size 4 is expected");
                f = float.NaN;
                return;
            }
#endif
            float* fdata = (float*)data;
            f.x = fdata[stack_offset];
            f.y = fdata[stack_offset + 1];
            f.z = fdata[stack_offset + 2];
            f.w = fdata[stack_offset + 3];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void pop(out int4 i)
        {
            stack_offset = stack_offset - 4;

#if DEVELOPMENT_BUILD || TRACE || CHECK_STACK
            if (stack_offset < 0)
            {
                Debug.LogError($"blast.stack: attempting to pop too much data (4 ints) from the stack, offset = {stack_offset}");
                i = int.MinValue;
                return;
            }
            BlastVariableDataType type = GetMetaDataType(metadata, (byte)stack_offset);
            if (type != BlastVariableDataType.ID)
            {
                Debug.LogError($"blast.stack: pop(int4) type mismatch, stack contains {type} datatype");
                i = int.MinValue;
                return;
            }
            byte size = GetMetaDataSize(metadata, (byte)stack_offset);
            if (size != 4)
            {
                Debug.LogError($"blast.stack: pop(int4) size mismatch, stack contains vectorsize {size} datatype while size 4 is expected");
                i = int.MinValue;
                return;
            }
#endif
            int* idata = (int*)data;
            i.x = idata[stack_offset];
            i.y = idata[stack_offset + 1];
            i.z = idata[stack_offset + 2];
            i.w = idata[stack_offset + 3];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        bool peek(int n = 1)
        {
            return stack_offset >= n;
        }

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


        #region pop_or_value

        #region pop_fXXX
        /// <summary>
        /// pop float of vectorsize 1, forced float type, verify type on debug   (equals pop_or_value)
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        float pop_f1(in int code_pointer)
        {
            // we get either a pop instruction or an instruction to get a value 
            byte c = code[code_pointer];

            if (c >= opt_id)
            {
#if DEVELOPMENT_BUILD || TRACE && CHECK_STACK
                BlastVariableDataType type = GetMetaDataType(metadata, (byte)(c - opt_id));
                int size = GetMetaDataSize(metadata, (byte)(c - opt_id));
                if (size != 1 || type != BlastVariableDataType.Numeric)
                {
                    Debug.LogError($"blast.stack, pop_or_value -> data mismatch, expecting numeric of size 1, found {type} of size {size} at data offset {c - opt_id}");
                    return float.NaN;
                }
#endif 
                // variable acccess
                return ((float*)data)[c - opt_id];
            }
            else
            if (c == (byte)blast_operation.pop)
            {
#if DEVELOPMENT_BUILD || TRACE && CHECK_STACK
                BlastVariableDataType type = GetMetaDataType(metadata, (byte)(stack_offset - 1));
                int size = GetMetaDataSize(metadata, (byte)(stack_offset - 1));
                if (size != 1 || type != BlastVariableDataType.Numeric)
                {
                    Debug.LogError($"blast.stack, pop_or_value -> stackdata mismatch, expecting numeric of size 1, found {type} of size {size} at stack offset {stack_offset}");
                    return float.NaN;                                                                                                                                      
                }
#endif         
                // stack pop 
                stack_offset = stack_offset - 1;
                return ((float*)data)[stack_offset];
            }
            else
            if (c >= opt_value)
            {
                return engine_ptr->constants[c];
            }
            else
            {
                // error or.. constant by operation value.... 
#if DEVELOPMENT_BUILD || TRACE
                Debug.LogError("pop_f1 -> select op by constant value is not supported");
#endif
                return float.NaN;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        float2 pop_f2(in int code_pointer)
        {
            byte c = code[code_pointer];
            if (c >= opt_id)
            {
#if DEVELOPMENT_BUILD || TRACE && CHECK_STACK
                // verify type of data / eventhough it is not stack 
                BlastVariableDataType type = GetMetaDataType(metadata, (byte)(c - opt_id));
                int size = GetMetaDataSize(metadata, (byte)(c - opt_id));
                if (size != 2 || type != BlastVariableDataType.Numeric)
                {
                    Debug.LogError($"blast.stack, pop_f2 -> data mismatch, expecting numeric of size 2, found {type} of size {size} at data offset {c - opt_id}");
                    return float.NaN;
                }
#endif 
                // variable acccess
                // TODO   SHOULD TEST NOT SWITCHING VECTOR ELEMENT ORDER ON STACK OPERATIONS 
                return new float2(((float*)data)[c - opt_id], ((float*)data)[c - opt_id + 1]);
            }
            else
            if (c == (byte)blast_operation.pop)
            {
                stack_offset = stack_offset - 2;

#if DEVELOPMENT_BUILD || TRACE && CHECK_STACK
                // verify type of data / eventhough it is not stack 
                BlastVariableDataType type = GetMetaDataType(metadata, (byte)stack_offset);
                int size = GetMetaDataSize(metadata, (byte)stack_offset);
                if (size != 2 || type != BlastVariableDataType.Numeric)
                {
                    Debug.LogError($"blast.stack, pop_or_value -> stackdata mismatch, expecting numeric of size 2, found {type} of size {size} at stack offset {stack_offset}");
                    return float.NaN;
                }
#endif         
                // stack pop 
                return new float2(((float*)data)[stack_offset], ((float*)data)[stack_offset + 1]);
            }
            else
            if (c >= opt_value)
            {
                return engine_ptr->constants[c];
            }
            else
            {
#if DEVELOPMENT_BUILD || TRACE
                Debug.LogError("pop_f2 -> select op by constant value is not supported");
#endif
                return float.NaN;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        float3 pop_f3(in int code_pointer)
        {
            byte c = code[code_pointer];
            if (c >= opt_id)
            {
#if DEVELOPMENT_BUILD || TRACE && CHECK_STACK
                // verify type of data / eventhough it is not stack 
                BlastVariableDataType type = GetMetaDataType(metadata, (byte)(c - opt_id));
                int size = GetMetaDataSize(metadata, (byte)(c - opt_id));
                if (size != 3 || type != BlastVariableDataType.Numeric)
                {
                    Debug.LogError($"blast.stack, pop_f3 -> data mismatch, expecting numeric of size 3, found {type} of size {size} at data offset {c - opt_id}");
                    return float.NaN;
                }
#endif 
                // variable acccess
                // TODO   SHOULD TEST NOT SWITCHING VECTOR ELEMENT ORDER ON STACK OPERATIONS 
                float* fdata = (float*)data;
                int index = c - opt_id;
                return new float3(fdata[index], fdata[index + 1], fdata[index + 2]);
            }
            else
            if (c == (byte)blast_operation.pop)
            {
                stack_offset = stack_offset - 3;

#if DEVELOPMENT_BUILD || TRACE && CHECK_STACK
                // verify type of data / eventhough it is not stack 
                BlastVariableDataType type = GetMetaDataType(metadata, (byte)stack_offset);
                int size = GetMetaDataSize(metadata, (byte)stack_offset);
                if (size != 3 || type != BlastVariableDataType.Numeric)
                {
                    Debug.LogError($"blast.stack, pop_or_value -> stackdata mismatch, expecting numeric of size 3, found {type} of size {size} at stack offset {stack_offset}");
                    return float.NaN;
                }
#endif         
                // stack pop 
                float* fdata = (float*)data;
                return new float3(fdata[stack_offset], fdata[stack_offset + 1], fdata[stack_offset + 2]);
            }
            else
            if (c >= opt_value)
            {
                return engine_ptr->constants[c];
            }
            else
            {
#if DEVELOPMENT_BUILD || TRACE
                Debug.LogError("pop_f3 -> select op by constant value is not supported");
#endif
                return float.NaN;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        float4 pop_f4(in int code_pointer)
        {
            byte c = code[code_pointer];
            if (c >= opt_id)
            {
#if DEVELOPMENT_BUILD || TRACE && CHECK_STACK
                // verify type of data / eventhough it is not stack 
                BlastVariableDataType type = GetMetaDataType(metadata, (byte)(c - opt_id));
                int size = GetMetaDataSize(metadata, (byte)(c - opt_id));
                if (size != 4 || type != BlastVariableDataType.Numeric)
                {
                    Debug.LogError($"blast.stack, pop_f4 -> data mismatch, expecting numeric of size 4, found {type} of size {size} at data offset {c - opt_id}");
                    return float.NaN;
                }
#endif 
                // variable acccess
                // TODO   SHOULD TEST NOT SWITCHING VECTOR ELEMENT ORDER ON STACK OPERATIONS 
                return new float4(
                    ((float*)data)[c - opt_id],
                    ((float*)data)[c - opt_id + 1],
                    ((float*)data)[c - opt_id + 2],
                    ((float*)data)[c - opt_id + 3]);
            }
            else
            if (c == (byte)blast_operation.pop)
            {
                stack_offset = stack_offset - 4;

#if DEVELOPMENT_BUILD || TRACE && CHECK_STACK
                // verify type of data / eventhough it is not stack 
                BlastVariableDataType type = GetMetaDataType(metadata, (byte)stack_offset);
                int size = GetMetaDataSize(metadata, (byte)stack_offset);
                if (size != 4 || type != BlastVariableDataType.Numeric)
                {
                    Debug.LogError($"blast.stack, pop_or_value -> stackdata mismatch, expecting numeric of size 4, found {type} of size {size} at stack offset {stack_offset}");
                    return float.NaN;
                }
#endif         
                // stack pop 
                float* stack = (float*)data;
                return new float4(stack[stack_offset], stack[stack_offset + 1], stack[stack_offset + 2], stack[stack_offset + 3]);
            }
            else
            if (c >= opt_value)
            {
                return engine_ptr->constants[c];
            }
            else
            {
#if DEVELOPMENT_BUILD || TRACE
                Debug.LogError("pop_f4 -> select op by constant value is not supported");
#endif
                return float.NaN;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        float3x2 pop_f3x2(in int code_pointer)
        {
            Debug.LogError("pop_fxxx not implemented");
            return float.NaN;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        float3x3 pop_f3x3(in int code_pointer)
        {
            Debug.LogError("pop_fxxx not implemented");
            return float.NaN;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        float3x4 pop_f3x4(in int code_pointer)
        {
            Debug.LogError("pop_fxxx not implemented");
            return float.NaN;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        float4x4 pop_f4x4(in int code_pointer)
        {
            Debug.LogError("pop_fxxx not implemented");
            return float.NaN;
        }

        #endregion


        /// <summary>
        /// get the next value from the datasegment, stack or constant dictionary 
        /// </summary>
        /// <param name="type">datatype of element popped</param>
        /// <param name="vector_size">vectorsize of element popped</param>
        /// <returns>pointer to data castable to valuetype*</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#pragma warning disable CS1573 // Parameter 'code_pointer' has no matching param tag in the XML comment for 'BlastInterpretor.pop_with_info(in int, out BlastVariableDataType, out byte)' (but other parameters do)
        internal void* pop_with_info(in int code_pointer, out BlastVariableDataType type, out byte vector_size)
#pragma warning restore CS1573 // Parameter 'code_pointer' has no matching param tag in the XML comment for 'BlastInterpretor.pop_with_info(in int, out BlastVariableDataType, out byte)' (but other parameters do)
        {
            // we get either a pop instruction or an instruction to get a value 
            byte c = code[code_pointer];

            if (c >= opt_id)
            {
                // variable 
                type = GetMetaDataType(metadata, (byte)(c - opt_id));
                vector_size = GetMetaDataSize(metadata, (byte)(c - opt_id));
                return &((float*)data)[c - opt_id];
            }
            else
            if (c == (byte)blast_operation.pop)
            {
                // stacked 
                vector_size = GetMetaDataSize(metadata, (byte)(stack_offset - 1));
                type = GetMetaDataType(metadata, (byte)(stack_offset - 1));

                // need to advance past vectorsize instead of only 1 that we need to probe 

                stack_offset -= math.select(vector_size, 4, vector_size == 0);

                return &((float*)data)[stack_offset];
            }
            else
            if (c >= opt_value)
            {
                type = BlastVariableDataType.Numeric;
                vector_size = 1;
                return &engine_ptr->constants[c];
            }
            else
            {
                // error or.. constant by operation value.... 
#if DEVELOPMENT_BUILD || TRACE
                Debug.LogError($"BlastInterpretor: pop_or_value -> select op by constant value is not supported, at codepointer: {code_pointer} => {code[code_pointer]}");
#endif
                type = BlastVariableDataType.Numeric;
                vector_size = 1;
                return null;
            }
        }

        /// <summary>
        /// 
        /// 1.68 ms on 100000x on the same script as the non-branch version
        /// 
        /// without the branch and 2 selects its 2.23 ms on the same test
        /// </summary>
        /// <param name="code_pointer"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal float pop_or_value(in int code_pointer)
        {
            // we get either a pop instruction or an instruction to get a value 
            byte c = code[code_pointer];

            if (c >= opt_id)
            {
#if DEVELOPMENT_BUILD || TRACE || CHECK_STACK
                // verify type of data / eventhough it is not stack 
                BlastVariableDataType type = GetMetaDataType(metadata, (byte)(c - opt_id));
                int size = GetMetaDataSize(metadata, (byte)(c - opt_id));
                if (size != 1 || type != BlastVariableDataType.Numeric)
                {
                    Debug.LogError($"blast.stack, pop_or_value -> data mismatch, expecting numeric of size 1, found {type} of size {size} at data offset {c - opt_id}");
                    return float.NaN;
                }
#endif 
                // variable acccess
                return ((float*)data)[c - opt_id];
            }
            else
            if (c == (byte)blast_operation.pop)
            {
                stack_offset = stack_offset - 1;

#if DEVELOPMENT_BUILD || TRACE || CHECK_STACK
                // verify type of data / eventhough it is not stack 
                BlastVariableDataType type = GetMetaDataType(metadata, (byte)stack_offset);
                int size = GetMetaDataSize(metadata, (byte)stack_offset);
                if (size != 1 || type != BlastVariableDataType.Numeric)
                {
                    Debug.LogError($"blast.stack, pop_or_value -> stackdata mismatch, expecting numeric of size 1, found {type} of size {size} at stack offset {stack_offset}");
                    return float.NaN;
                }
#endif         
                // stack pop 
                return ((float*)data)[stack_offset];
            }
            else
            if (c >= opt_value)
            {
#if DEVELOPMENT_BUILD || TRACE || CHECK_STACK
                // verify type of data / eventhough it is not stack 
                //BlastVariableDataType type = GetMetaDataType(metadata, (byte)(c - opt_value));
                //int size = GetMetaDataSize(metadata, (byte)(c - opt_value));
                //if (size != 1 || type != BlastVariableDataType.Numeric)
                //{
                //Standalone.Debug.LogError($"blast.stack, pop_or_value -> constant data mismatch, expecting numeric of size 1, found {type} of size {size} at offset {c - opt_value}");
                //return float.NaN;
                // }
#endif
                // op encoded constant                
                // //
                return engine_ptr->constants[c];
            }
            else
            {
                // error or.. constant by operation value.... 
#if DEVELOPMENT_BUILD || TRACE
                Debug.LogError("BlastInterpretor: pop_or_value -> select op by constant value is not supported");
#endif
                return float.NaN;
            }
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal float4 pop_or_value_4(in int code_pointer)
        {
            // no constant check for vectors, we dont use it

            // expect a single pop,  NOT A POPV inside functions that have var length params and vectorsize
            // vector size is always known when used inside vector functions 
            byte c = code[code_pointer];
            float* fdata = (float*)data;

            if (c >= opt_id)
            {
#if DEVELOPMENT_BUILD || TRACE || CHECK_STACK
                // verify type of data / eventhough it is not stack 
                BlastVariableDataType type = GetMetaDataType(metadata, (byte)(c - opt_id));
                int size = GetMetaDataSize(metadata, (byte)(c - opt_id));
                if (size != 4 || type != BlastVariableDataType.Numeric)
                {
                    Debug.LogError($"blast.stack, pop_or_value4 -> data mismatch, expecting numeric of size 4, found {type} of size {size} at data offset {c - opt_id}");
                    return float.NaN;
                }
#endif 

                int offset = c - opt_id;
                return new float4(fdata[offset + 0], fdata[offset + 1], fdata[offset + 2], fdata[offset + 3]);
            }
            else
            // pop4
            if (c == (byte)blast_operation.pop)
            {
                stack_offset = stack_offset - 4;
#if DEVELOPMENT_BUILD || TRACE || CHECK_STACK
                // verify type of data / eventhough it is not stack 
                BlastVariableDataType type = GetMetaDataType(metadata, (byte)stack_offset);
                int size = GetMetaDataSize(metadata, (byte)stack_offset);
                if (size != 4 || type != BlastVariableDataType.Numeric)
                {
                    Debug.LogError($"blast.stack, pop_or_value4 -> stackdata mismatch, expecting numeric of size 1, found {type} of size {size} at stack offset {stack_offset}");
                    return float.NaN;
                }
#endif         
                float* stack = (float*)data;
                return new float4(stack[stack_offset + 0], stack[stack_offset + 1], stack[stack_offset + 2], stack[stack_offset + 3]);
            }
            else
            if (c >= opt_value)
            {
#if DEVELOPMENT_BUILD || TRACE || CHECK_STACK
                // verify type of data / eventhough it is not stack 
                BlastVariableDataType type = GetMetaDataType(metadata, (byte)(c - opt_value));
                int size = GetMetaDataSize(metadata, (byte)(c - opt_value));
                if (size != 4 || type != BlastVariableDataType.Numeric)
                {
                    Debug.LogError($"blast.stack, pop_or_value4 -> constant data mismatch, expecting numeric of size 4, found {type} of size {size} at offset {c - opt_value}");
                    return float.NaN;
                }
                Debug.LogWarning("?? constant vectors ??");
#endif         
                // constant                
                return engine_ptr->constants[c];
            }
            else
            {
                // error or.. constant by operation value.... 
#if DEVELOPMENT_BUILD || TRACE
                Debug.LogError("BlastInterpretor: pop_or_value4 -> select op by constant value is not supported");
#endif
                return float.NaN;
            }
        }
        #endregion


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
            type = (BlastVariableDataType)(b & 0b1111_0000);
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
            // stack state 
            push(f4_register);

            // get nr of frames to yield 
            byte b = code[code_pointer];
            if (b >= opt_value)
            {
                push(pop_or_value(code_pointer));
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




        #endregion

        #region Operations Handlers 


        /// <summary>
        /// handle an operation between 2 values in the form: <c>a = a + b;</c>
        /// </summary>
        /// <param name="op"></param>
        /// <param name="current"></param>
        /// <param name="previous"></param>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="vector_size"></param>

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        static void handle_op(in blast_operation op, in byte current, in byte previous, ref float4 a, in float4 b, ref byte vector_size)
        {
            switch (op)
            {
                default:
                case blast_operation.nop:
                    {
                        a = b;
                    }
                    return;

                case blast_operation.add: a = a + b; return;
                case blast_operation.substract: a = a - b; return;
                case blast_operation.multiply: a = a * b; return;
                case blast_operation.divide: a = a / b; return;

                case blast_operation.and: a = math.select(0f, 1f, math.all(a) && math.all(b)); return;
                case blast_operation.or: a = math.select(0f, 1f, math.any(a) || math.any(b)); return;
                case blast_operation.xor: a = math.select(0f, 1f, math.any(a) ^ math.any(b)); return;

                case blast_operation.greater: a = math.select(0f, 1f, a > b); return;
                case blast_operation.smaller: a = math.select(0f, 1f, a < b); return;
                case blast_operation.smaller_equals: a = math.select(0f, 1f, a <= b); return;
                case blast_operation.greater_equals: a = math.select(0f, 1f, a >= b); return;
                case blast_operation.equals: a = math.select(0f, 1f, a == b); return;
                case blast_operation.not_equals: a = math.select(0f, 1f, a != b); return;

                case blast_operation.not: a = math.select(0f, 1f, b == 0); return; 
            }
        }


        /// <summary>
        /// handle operation between 2 singles of vectorsize 1, handles in the form: <c>result = a + b;</c>
        /// </summary>
        /// <param name="op">operation to take</param>
        /// <param name="a">operand a</param>
        /// <param name="b">operand b</param>
        /// <returns>result of operation</returns>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        static float handle_op(in blast_operation op, in float a, in float b)
        {
            switch (op)
            {
                default:
                case blast_operation.nop: return b;
                case blast_operation.add: return a + b;
                case blast_operation.substract: return a - b;
                case blast_operation.multiply: return a * b;
                case blast_operation.divide: return a / b;

                case blast_operation.and: return math.select(0f, 1f, a != 0 && b != 0);
                case blast_operation.or: return math.select(0f, 1f, a != 0 || b != 0);
                case blast_operation.xor: return math.select(0f, 1f, a != 0 ^ b != 0);

                case blast_operation.greater: return math.select(0f, 1f, a > b);
                case blast_operation.smaller: return math.select(0f, 1f, a < b);
                case blast_operation.smaller_equals: return math.select(0f, 1f, a <= b);
                case blast_operation.greater_equals: return math.select(0f, 1f, a >= b);
                case blast_operation.equals: return math.select(0f, 1f, a == b);
                case blast_operation.not_equals: return math.select(0f, 1f, a != b);

                case blast_operation.not: return math.select(0f, 1f, b == 0);
            }
        }


        /// <summary>
        /// handle operation between 2 singles of vectorsize 1, handles in the form: <c>result = a + b;</c>
        /// </summary>
        /// <param name="op">operation to take</param>
        /// <param name="a">operand a</param>
        /// <param name="b">operand b</param>
        /// <returns>result of operation</returns>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        static float2 handle_op(in blast_operation op, in float2 a, in float b)
        {
            switch (op)
            {
                default:
                case blast_operation.nop: return b;
                case blast_operation.add: return a + b;
                case blast_operation.substract: return a - b;
                case blast_operation.multiply: return a * b;
                case blast_operation.divide: return a / b;

                case blast_operation.and: return math.select(0f, 1f, math.any(a) && b != 0);
                case blast_operation.or: return math.select(0f, 1f, math.any(a) || b != 0);
                case blast_operation.xor: return math.select(0f, 1f, math.any(a) ^ b != 0);  // this isnt really correct but then xor on 3 operands is wierd.. 

                case blast_operation.greater: return math.select(0f, 1f, a > b);
                case blast_operation.smaller: return math.select(0f, 1f, a < b);
                case blast_operation.smaller_equals: return math.select(0f, 1f, a <= b);
                case blast_operation.greater_equals: return math.select(0f, 1f, a >= b);
                case blast_operation.equals: return math.select(0f, 1f, a == b);
                case blast_operation.not_equals: return math.select(0f, 1f, a != b);

                case blast_operation.not:
                    return math.select(0f, 1f, b == 0);
            }
        }


        /// <summary>
        /// handle operation between 2 singles of vectorsize 1, handles in the form: <c>result = a + b;</c>
        /// </summary>
        /// <param name="op">operation to take</param>
        /// <param name="a">operand a</param>
        /// <param name="b">operand b</param>
        /// <returns>result of operation</returns>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        static float3 handle_op(in blast_operation op, in float3 a, in float b)
        {
            switch (op)
            {
                default:
                case blast_operation.nop: return b;
                case blast_operation.add: return a + b;
                case blast_operation.substract: return a - b;
                case blast_operation.multiply: return a * b;
                case blast_operation.divide: return a / b;

                case blast_operation.and: return math.select(0f, 1f, math.any(a) && b != 0);
                case blast_operation.or: return math.select(0f, 1f, math.any(a) || b != 0);
                case blast_operation.xor: return math.select(0f, 1f, math.any(a) ^ b != 0);  // this isnt really correct but then xor on 3 operands is wierd.. 

                case blast_operation.greater: return math.select(0f, 1f, a > b);
                case blast_operation.smaller: return math.select(0f, 1f, a < b);
                case blast_operation.smaller_equals: return math.select(0f, 1f, a <= b);
                case blast_operation.greater_equals: return math.select(0f, 1f, a >= b);
                case blast_operation.equals: return math.select(0f, 1f, a == b);
                case blast_operation.not_equals: return math.select(0f, 1f, a != b);

                case blast_operation.not:
                    return math.select(0f, 1f, b == 0);
            }
        }

        /// <summary>
        /// handle operation between 2 singles of vectorsize 1, handles in the form: <c>result = a + b;</c>
        /// </summary>
        /// <param name="op">operation to take</param>
        /// <param name="a">operand a</param>
        /// <param name="b">operand b</param>
        /// <returns>result of operation</returns>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        static float4 handle_op(in blast_operation op, in float4 a, in float b)
        {
            switch (op)
            {
                default:
                case blast_operation.nop: return b;
                case blast_operation.add: return a + b;
                case blast_operation.substract: return a - b;
                case blast_operation.multiply: return a * b;
                case blast_operation.divide: return a / b;

                case blast_operation.and: return math.select(0f, 1f, math.any(a) && b != 0);
                case blast_operation.or: return math.select(0f, 1f, math.any(a) || b != 0);
                case blast_operation.xor: return math.select(0f, 1f, math.any(a) ^ b != 0);  // this isnt really correct but then xor on 3 operands is wierd.. 

                case blast_operation.greater: return math.select(0f, 1f, a > b);
                case blast_operation.smaller: return math.select(0f, 1f, a < b);
                case blast_operation.smaller_equals: return math.select(0f, 1f, a <= b);
                case blast_operation.greater_equals: return math.select(0f, 1f, a >= b);
                case blast_operation.equals: return math.select(0f, 1f, a == b);
                case blast_operation.not_equals: return math.select(0f, 1f, a != b); 

                case blast_operation.not:
                    return math.select(0f, 1f, b == 0);
            }
        }

        /// <summary>
        /// handle operation between 2 floats of differing vectorsize, resulting vectorisize is the largest of the 2, handles in the form: <c>result = a + b;</c>
        /// </summary>
        /// <param name="op">the operation to take</param>
        /// <param name="a">left operand</param>
        /// <param name="b">right operand</param>
        /// <returns>returns result of operation</returns>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        static float2 handle_op(in blast_operation op, in float a, in float2 b)
        {
            switch (op)
            {
                default:
                case blast_operation.nop: return b;
                case blast_operation.add: return a + b;
                case blast_operation.substract: return a - b;
                case blast_operation.multiply: return a * b;
                case blast_operation.divide: return a / b;

                case blast_operation.and: return math.select(0f, 1f, a != 0 && math.any(b));
                case blast_operation.or: return math.select(0f, 1f, a != 0 || math.any(b));
                case blast_operation.xor: return math.select(0f, 1f, a != 0 ^ math.any(b));

                case blast_operation.greater: return (float2)(a > b);
                case blast_operation.smaller: return (float2)(a < b);
                case blast_operation.smaller_equals: return (float2)(a <= b);
                case blast_operation.greater_equals: return (float2)(a >= b);
                case blast_operation.equals: return (float2)(a == b);
                case blast_operation.not_equals: return (float2)(a != b);

                case blast_operation.not:
                    return math.select(0f, 1f, b == 0);
            }
        }

        /// <summary>
        /// handle operation between 2 floats of differing vectorsize, resulting vectorisize is the largest of the 2, handles in the form: <c>result = a + b;</c>
        /// </summary>
        /// <param name="op">the operation to take</param>
        /// <param name="a">left operand</param>
        /// <param name="b">right operand</param>
        /// <returns>returns result of operation</returns>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        static float2 handle_op(in blast_operation op, in float2 a, in float2 b)
        {
            switch (op)
            {
                default:
                case blast_operation.nop: return b;
                case blast_operation.add: return a + b;
                case blast_operation.substract: return a - b;
                case blast_operation.multiply: return a * b;
                case blast_operation.divide: return a / b;

                case blast_operation.and: return math.select(0f, 1f, math.any(a) && math.any(b));
                case blast_operation.or: return math.select(0f, 1f, math.any(a) || math.any(b));
                case blast_operation.xor: return math.select(0f, 1f, math.any(a) ^ math.any(b));

                case blast_operation.greater: return (float2)(a > b);
                case blast_operation.smaller: return (float2)(a < b);
                case blast_operation.smaller_equals: return (float2)(a <= b);
                case blast_operation.greater_equals: return (float2)(a >= b);
                case blast_operation.equals: return (float2)(a == b);
                case blast_operation.not_equals: return (float2)(a != b);

                case blast_operation.not:
                    return math.select(0f, 1f, b == 0);
            }
        }

        /// <summary>
        /// handle operation between 2 floats of differing vectorsize, resulting vectorisize is the largest of the 2, handles in the form: <c>result = a + b;</c>
        /// </summary>
        /// <param name="op">the operation to take</param>
        /// <param name="a">left operand</param>
        /// <param name="b">right operand</param>
        /// <returns>returns result of operation</returns>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        static float3 handle_op(in blast_operation op, in float a, in float3 b)
        {
            switch (op)
            {
                default:
                case blast_operation.nop: return b;
                case blast_operation.add: return a + b;
                case blast_operation.substract: return a - b;
                case blast_operation.multiply: return a * b;
                case blast_operation.divide: return a / b;

                case blast_operation.and: return math.select(0f, 1f, a != 0 && math.any(b));
                case blast_operation.or: return math.select(0f, 1f, a != 0 || math.any(b));
                case blast_operation.xor: return math.select(0f, 1f, a != 0 ^ math.any(b));

                case blast_operation.greater: return (float3)(a > b);
                case blast_operation.smaller: return (float3)(a < b);
                case blast_operation.smaller_equals: return (float3)(a <= b);
                case blast_operation.greater_equals: return (float3)(a >= b);
                case blast_operation.equals: return (float3)(a == b);
                case blast_operation.not_equals: return (float3)(a != b);

                case blast_operation.not:
                    return math.select(0f, 1f, b == 0);
            }
        }


        /// <summary>
        /// handle operation between 2 floats of differing vectorsize, resulting vectorisize is the largest of the 2, handles in the form: <c>result = a + b;</c>
        /// </summary>
        /// <param name="op">the operation to take</param>
        /// <param name="a">left operand</param>
        /// <param name="b">right operand</param>
        /// <returns>returns result of operation</returns>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        static float3 handle_op(in blast_operation op, in float3 a, in float3 b)
        {
            switch (op)
            {
                default:
                case blast_operation.nop: return b;
                case blast_operation.add: return a + b;
                case blast_operation.substract: return a - b;
                case blast_operation.multiply: return a * b;
                case blast_operation.divide: return a / b;

                case blast_operation.and: return math.select(0f, 1f, math.any(a) && math.any(b));
                case blast_operation.or: return math.select(0f, 1f, math.any(a) || math.any(b));
                case blast_operation.xor: return math.select(0f, 1f, math.any(a) ^ math.any(b));

                case blast_operation.greater: return (float3)(a > b);
                case blast_operation.smaller: return (float3)(a < b);
                case blast_operation.smaller_equals: return (float3)(a <= b);
                case blast_operation.greater_equals: return (float3)(a >= b);
                case blast_operation.equals: return (float3)(a == b);
                case blast_operation.not_equals: return (float3)(a != b);

                case blast_operation.not:
                    return math.select(0f, 1f, b == 0);
            }
        }

        /// <summary>
        /// handle operation between 2 floats of differing vectorsize, resulting vectorisize is the largest of the 2, handles in the form: <c>result = a + b;</c>
        /// </summary>
        /// <param name="op">the operation to take</param>
        /// <param name="a">left operand</param>
        /// <param name="b">right operand</param>
        /// <returns>returns result of operation</returns>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        static float4 handle_op(in blast_operation op, in float a, in float4 b)
        {
            switch (op)
            {
                default:
                case blast_operation.nop: return b;
                case blast_operation.add: return a + b;
                case blast_operation.substract: return a - b;
                case blast_operation.multiply: return a * b;
                case blast_operation.divide: return a / b;

                case blast_operation.and: return math.select(0f, 1f, a != 0 && math.any(b));
                case blast_operation.or: return math.select(0f, 1f, a != 0 || math.any(b));
                case blast_operation.xor: return math.select(0f, 1f, a != 0 ^ math.any(b));

                case blast_operation.greater: return (float4)(a > b);
                case blast_operation.smaller: return (float4)(a < b);
                case blast_operation.smaller_equals: return (float4)(a <= b);
                case blast_operation.greater_equals: return (float4)(a >= b);
                case blast_operation.equals: return (float4)(a == b);
                case blast_operation.not_equals: return (float4)(a != b);

                case blast_operation.not:
                    return math.select(0f, 1f, b == 0);
            }
        }


        /// <summary>
        /// handle operation between 2 floats of differing vectorsize, resulting vectorisize is the largest of the 2, handles in the form: <c>result = a + b;</c>
        /// </summary>
        /// <param name="op">the operation to take</param>
        /// <param name="a">left operand</param>
        /// <param name="b">right operand</param>
        /// <returns>returns result of operation</returns>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        static float4 handle_op(in blast_operation op, in float4 a, in float4 b)
        {
            switch (op)
            {
                default:
                case blast_operation.nop: return b;
                case blast_operation.add: return a + b;
                case blast_operation.substract: return a - b;
                case blast_operation.multiply: return a * b;
                case blast_operation.divide: return a / b;

                case blast_operation.and: return math.select(0f, 1f, math.any(a) && math.any(b));
                case blast_operation.or: return math.select(0f, 1f, math.any(a) || math.any(b));
                case blast_operation.xor: return math.select(0f, 1f, math.any(a) ^ math.any(b));

                case blast_operation.greater: return (float4)(a > b);
                case blast_operation.smaller: return (float4)(a < b);
                case blast_operation.smaller_equals: return (float4)(a <= b);
                case blast_operation.greater_equals: return (float4)(a >= b);
                case blast_operation.equals: return (float4)(a == b);
                case blast_operation.not_equals: return (float4)(a != b);

                case blast_operation.not:
                    return math.select(0f, 1f, b == 0);
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        static void handle_op(in blast_operation op, in byte current, in byte previous, ref float4 a, in float3 b, ref byte vector_size)
        {
            switch (op)
            {
                default:
                case blast_operation.nop:
                    {
                        a.xyz = b;
                    }
                    return;

                case blast_operation.add: a.xyz = a.xyz + b; return;
                case blast_operation.substract: a.xyz = a.xyz - b; return;
                case blast_operation.multiply: a.xyz = a.xyz * b; return;
                case blast_operation.divide: a.xyz = a.xyz / b; return;

                case blast_operation.and: a.xyz = math.select(0f, 1f, math.any(a.xyz) && math.any(b)); return;
                case blast_operation.or: a.xyz = math.select(0f, 1f, math.any(a.xyz) || math.any(b)); return;
                case blast_operation.xor: a.xyz = math.select(0f, 1f, math.any(a.xyz) ^ math.any(b)); return;

                case blast_operation.greater: a.xyz = math.select(0f, 1f, a.xyz > b); return;
                case blast_operation.smaller: a.xyz = math.select(0f, 1f, a.xyz < b); return;
                case blast_operation.smaller_equals: a.xyz = math.select(0f, 1f, a.xyz <= b); return;
                case blast_operation.greater_equals: a.xyz = math.select(0f, 1f, a.xyz >= b); return;
                case blast_operation.equals: a.xyz = math.select(0f, 1f, a.xyz == b); return;
                case blast_operation.not_equals: a.xyz = (float3)(a.xyz != b); return; 

                case blast_operation.not: a.xyz = math.select(0f, 1f, b == 0); return;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        static void handle_op(in blast_operation op, in byte current, in byte previous, ref float4 a, in float2 b, ref byte vector_size)
        {
            switch (op)
            {
                default:
                case blast_operation.nop:
                    {
                        a.xy = b;
                    }
                    return;

                case blast_operation.add: a.xy = a.xy + b; return;
                case blast_operation.substract: a.xy = a.xy - b; return;
                case blast_operation.multiply: a.xy = a.xy * b; return;
                case blast_operation.divide: a.xy = a.xy / b; return;

                case blast_operation.and: a.xy = math.select(0, 1, math.any(a.xy) && math.any(b)); return;
                case blast_operation.or: a.xy = math.select(0, 1, math.any(a.xy) || math.any(b)); return;
                case blast_operation.xor: a.xy = math.select(0, 1, math.any(a.xy) ^ math.any(b)); return;

                case blast_operation.greater: a.xy = math.select(0f, 1f, a.xy > b); return;
                case blast_operation.smaller: a.xy = math.select(0f, 1f, a.xy < b); return;
                case blast_operation.smaller_equals: a.xy = math.select(0f, 1f, a.xy <= b); return;
                case blast_operation.greater_equals: a.xy = math.select(0f, 1f, a.xy >= b); return;
                case blast_operation.equals: a.xy = math.select(0f, 1f, a.xy == b); return;
                case blast_operation.not_equals: a.xy = (float2)(a.xy != b); return;

                case blast_operation.not:
                    a.xy = math.select(0f, 1f, b == 0); return;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        static void handle_op(in blast_operation op, in byte current, in byte previous, ref float a, in float b, ref byte vector_size)
        {
            switch (op)
            {
                default: return;
                case blast_operation.nop:
                    {
                        a = b;
                    }
                    return;

                case blast_operation.add: a = a + b; return;
                case blast_operation.substract: a = a - b; return;
                case blast_operation.multiply: a = a * b; return;
                case blast_operation.divide: a = a / b; return;

                case blast_operation.and: a = math.select(0, 1, a != 0 && b != 0); return;
                case blast_operation.or: a = math.select(0, 1, a != 0 || b != 0); return;
                case blast_operation.xor: a = math.select(0, 1, a != 0 ^ b != 0); return;

                case blast_operation.greater: a = math.select(0f, 1f, a > b); return;
                case blast_operation.smaller: a = math.select(0f, 1f, a < b); return;
                case blast_operation.smaller_equals: a = math.select(0f, 1f, a <= b); return;
                case blast_operation.greater_equals: a = math.select(0f, 1f, a >= b); return;
                case blast_operation.equals: a = math.select(0f, 1f, a == b); return;
                case blast_operation.not_equals: a = math.select(0f, 1f, a != b); return;

                case blast_operation.not:
                    a = math.select(0f, 1f, b == 0); return;
            }
        }

        #endregion

        #region Debug Data
        /// <summary>
        /// handle command to show the given field in debug 
        /// </summary>
        /// <param name="code_pointer"></param>
        /// <param name="vector_size"></param>
        /// <param name="op_id"></param>
        /// <param name="datatype"></param>
        /// <param name="pdata"></param>
        static void Handle_DebugData(int code_pointer, byte vector_size, int op_id, BlastVariableDataType datatype, void* pdata)
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
                            case 8: Debug.Log($"Blast.Debug - codepointer: {code_pointer}, id: {op_id}, NUMERIC: [{fdata[0].ToString("0.00")}, {fdata[1].ToString("0.00")}, {fdata[2].ToString("0.00")}, {fdata[3].ToString("0.00")}, {fdata[4].ToString("0.00")}, {fdata[5].ToString("0.00")}, {fdata[6].ToString("0.00")}, {fdata[7].ToString("0.00")}], vectorsize: {vector_size}"); break;
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

                    default:
                        Debug.LogError($"Blast.Debug(data) - Datatype '{datatype}' not supported in variable/operation with id {op_id} and vectorsize {vector_size}");
                        break;
                }
            }
#endif
        }

        /// <summary>
        /// printout an overview of the datasegment/stack (if shared)
        /// </summary>
        void Handle_DebugStack()
        {
#if DEVELOPMENT_BUILD || TRACE
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
        }

        #endregion

        #region External Functionpointer calls 
        /// <summary>
        /// call an external function pointer, pointed to by an 8|16|32 bit identifier 
        ///</summary>
        void CallExternalFunction(ref int code_pointer, ref byte vector_size, out float4 f4)
        {
            // get int id from next 4 ops/bytes 
            int id =
                (code[code_pointer + 1] << 24)
                +
                (code[code_pointer + 2] << 16)
                +
                (code[code_pointer + 3] << 8)
                +
                code[code_pointer + 4];

            // advance code pointer beyond the script id
            code_pointer += 4;

            // get function pointer 
#if DEVELOPMENT_BUILD || TRACE
            if (engine_ptr->CanBeAValidFunctionId(id))
            {
#endif 
                BlastScriptFunction p = engine_ptr->Functions[id];

                // get parameter count (expected from script excluding the 3 data pointers: engine, env, caller) 
                // but dont call it in validation mode 
                if (ValidationMode == true)
                {
                    // external functions (to blast) always have a fixed amount of parameters 
#if DEVELOPMENT_BUILD || TRACE
                    if (id > (int)ReservedBlastScriptFunctionIds.Offset && p.MinParameterCount != p.MaxParameterCount)
                    {
                        Debug.LogError($"function {id} min/max parameters should be of equal size");
                    }
#endif
                    code_pointer += p.MinParameterCount;
                    f4 = float.NaN;
                    return;
                }

#if DEVELOPMENT_BUILD || TRACE 
                Debug.Log($"call fp id: {id} {environment_ptr.ToInt64()} {caller_ptr.ToInt64()}, parmetercount = {p.MinParameterCount}");
#endif


#if DEVELOPMENT_BUILD || TRACE
                vector_size = (byte)math.select((int)p.ReturnsVectorSize, p.AcceptsVectorSize, p.ReturnsVectorSize == 0);
                if (vector_size != 1)
                {
                    Debug.LogError($"function {id} a return vector size > 1 is not currently supported in external functions");
                    f4 = float.NaN;
                    return;
                }
                if (p.AcceptsVectorSize != 1)
                {
                    Debug.LogError($"function {id} a parameter vector size > 1 is not currently supported in external functions");
                    f4 = float.NaN;
                    return;
                }
#else 
                vector_size = 1; 
#endif 

                switch (p.MinParameterCount)
                {
                    case 0:
                        FunctionPointer<Blast.BlastDelegate_f0> fp0 = p.Generic<Blast.BlastDelegate_f0>();
                        if (fp0.IsCreated && fp0.Value != IntPtr.Zero)
                        {
                            vector_size = 1;
                            f4 = fp0.Invoke((IntPtr)engine_ptr, environment_ptr, caller_ptr);
                            return;
                        }
                        break;

                    case 1:
                        FunctionPointer<Blast.BlastDelegate_f1> fp1 = p.Generic<Blast.BlastDelegate_f1>();
                        if (fp1.IsCreated && fp1.Value != IntPtr.Zero)
                        {
                            float a = pop_f1(code_pointer + 1);
                            code_pointer++;

                            f4 = fp1.Invoke((IntPtr)engine_ptr, environment_ptr, caller_ptr, a);
                            return;
                        }
                        break;

                    case 2:
                        FunctionPointer<Blast.BlastDelegate_f2> fp2 = p.Generic<Blast.BlastDelegate_f2>();
                        if (fp2.IsCreated && fp2.Value != IntPtr.Zero)
                        {
                            float a = pop_f1(code_pointer + 1);
                            float b = pop_f1(code_pointer + 2);
                            code_pointer += 2;
                            f4 = fp2.Invoke((IntPtr)engine_ptr, environment_ptr, caller_ptr, a, b);
                            return;
                        }
                        break;

                    case 3:
                        FunctionPointer<Blast.BlastDelegate_f3> fp3 = p.Generic<Blast.BlastDelegate_f3>();
                        if (fp3.IsCreated && fp3.Value != IntPtr.Zero)
                        {
                            float a = pop_f1(code_pointer + 1);
                            float b = pop_f1(code_pointer + 2);
                            float c = pop_f1(code_pointer + 3);
                            code_pointer += 3;
                            f4 = fp3.Invoke((IntPtr)engine_ptr, environment_ptr, caller_ptr, a, b, c);
                            return;
                        }
                        break;

                    case 4:
                        FunctionPointer<Blast.BlastDelegate_f4> fp4 = p.Generic<Blast.BlastDelegate_f4>();
                        if (fp4.IsCreated && fp4.Value != IntPtr.Zero)
                        {
                            float a = pop_f1(code_pointer + 1);
                            float b = pop_f1(code_pointer + 2);
                            float c = pop_f1(code_pointer + 3);
                            float d = pop_f1(code_pointer + 4);
                            code_pointer += 4;
                            f4 = (int)fp4.Invoke((IntPtr)engine_ptr, environment_ptr, caller_ptr, a, b, c, d);
                            return;
                        }
                        break;


#if DEVELOPMENT_BUILD || TRACE
                    default:
                        Debug.LogError($"function id {id}, with parametercount {p.MinParameterCount} is not supported yet");
                        break;
#endif
                }
#if DEVELOPMENT_BUILD || TRACE
            }
#endif

#if DEVELOPMENT_BUILD || TRACE 
            Debug.LogError($"blast: failed to call function pointer with id: {id}");
#endif
            f4 = float.NaN;
        }

        #endregion

        #region Function Handlers 


        #region get_XXXX_result Operation Handlers (updated to used MetaDataSize) 

        #region get_[min/max/mina/maxa]_result
        /// <summary>
        /// get the maximum value of all arguments of any vectorsize 
        /// </summary>
        void get_maxa_result(ref int code_pointer, ref byte vector_size, out float4 f4)
        {
            code_pointer++;
            byte c = decode62(in code[code_pointer], ref vector_size);

            BlastVariableDataType datatype = default;
            byte popped_vector_size = 0;

            f4 = new float4(int.MinValue);
            vector_size = 1;

            // c == nr of parameters ...  
            while (c > 0)
            {
                code_pointer++;
                c--;

                // we need to pop only once due to how the elements of the vector are organized in the data/stack
                // - pop_or_value needs one call/float
                float* fdata = (float*)pop_with_info(in code_pointer, out datatype, out popped_vector_size);

#if DEVELOPMENT_BUILD || TRACE
                if (datatype == BlastVariableDataType.ID)
                {
                    Debug.LogError($"ID datatype in maxa not yet implemented, codepointer: {code_pointer} = > {code[code_pointer]}");
                    f4.x = float.NaN;
                    return;
                }
#endif

                switch ((BlastVectorSizes)popped_vector_size)
                {
                    case BlastVectorSizes.float1: f4.x = math.max(fdata[0], f4.x); break;
                    case BlastVectorSizes.float2: f4.x = math.max(fdata[0], math.max(fdata[1], f4.x)); break;
                    case BlastVectorSizes.float3: f4.x = math.max(fdata[0], math.max(fdata[1], math.max(fdata[2], f4.x))); break;
                    case 0:
                    case BlastVectorSizes.float4: f4.x = math.max(fdata[0], math.max(fdata[1], math.max(fdata[2], math.max(fdata[3], f4.x)))); break;
                    default:
                        {
#if DEVELOPMENT_BUILD || TRACE
                            Debug.LogError($"maxa: vector size '{vector_size}' not supported at codepointer {code_pointer} => {code[code_pointer]} ");
#endif
                            f4.x = float.NaN;
                            return;
                        }
                }
            }
        }

        /// <summary>
        /// return the smallest of the arguments of any vectorsize
        /// </summary>
        void get_mina_result(ref int code_pointer, ref byte vector_size, out float4 f4)
        {
            code_pointer++;
            byte c = decode62(in code[code_pointer], ref vector_size);

            BlastVariableDataType datatype = default;
            byte popped_vector_size = 0;

            f4 = new float4(float.MaxValue);
            vector_size = 1;

            // c == nr of parameters ...  
            while (c > 0)
            {
                code_pointer++;
                c--;

                // we need to pop only once due to how the elements of the vector are organized in the data/stack
                // - pop_or_value needs one call/float
                float* fdata = (float*)pop_with_info(in code_pointer, out datatype, out popped_vector_size);

#if DEVELOPMENT_BUILD || TRACE
                if (datatype == BlastVariableDataType.ID)
                {
                    Debug.LogError("ID datatype in mina not yet implemented");
                    f4.x = float.NaN;
                    return;
                }
#endif

                switch ((BlastVectorSizes)popped_vector_size)
                {
                    case BlastVectorSizes.float1: f4.x = math.min(fdata[0], f4.x); break;
                    case BlastVectorSizes.float2: f4.x = math.min(fdata[0], math.min(fdata[1], f4.x)); break;
                    case BlastVectorSizes.float3: f4.x = math.min(fdata[0], math.min(fdata[1], math.min(fdata[2], f4.x))); break;
                    case 0:
                    case BlastVectorSizes.float4: f4.x = math.min(fdata[0], math.min(fdata[1], math.min(fdata[2], math.min(fdata[3], f4.x)))); break;
                    default:
                        {
#if DEVELOPMENT_BUILD || TRACE
                            Debug.LogError($"mina: vector size '{vector_size}' not supported at codepointer {code_pointer} => {code[code_pointer]} ");
#endif
                            f4.x = float.NaN;
                            return;
                        }
                }
            }
        }


        /// <summary>
        /// return the component sum of the arguments of any vectorsize
        /// </summary>
        void get_csum_result(ref int code_pointer, ref byte vector_size, out float4 f4)
        {
            code_pointer++;
            byte c = decode62(in code[code_pointer], ref vector_size);

            BlastVariableDataType datatype = default;
            byte popped_vector_size = 0;

            f4 = 0;
            vector_size = 1;

            // c == nr of parameters ...  
            while (c > 0)
            {
                code_pointer++;
                c--;

                // we need to pop only once due to how the elements of the vector are organized in the data/stack
                // - pop_or_value needs one call/float
                void* pdata = (void*)pop_with_info(in code_pointer, out datatype, out popped_vector_size);

#if DEVELOPMENT_BUILD || TRACE
                if (datatype == BlastVariableDataType.ID)
                {
                    Debug.LogError("ID datatype in csum not yet implemented");
                    f4.x = float.NaN;
                    return;
                }
#endif                           

                switch ((BlastVectorSizes)popped_vector_size)
                {
                    case BlastVectorSizes.float1: f4.x += ((float*)pdata)[0]; break;
                    case BlastVectorSizes.float2: f4.x += math.csum(((float2*)pdata)[0]); break; 
                    case BlastVectorSizes.float3: f4.x += math.csum(((float3*)pdata)[0]); break;
                    case 0:
                    case BlastVectorSizes.float4: f4.x += math.csum(((float4*)pdata)[0]); vector_size = 4; break;
                    default:
                        {
#if DEVELOPMENT_BUILD || TRACE
                            Debug.LogError($"csum: vector size '{vector_size}' not supported at codepointer {code_pointer} => {code[code_pointer]} ");
#endif
                            f4.x = float.NaN;
                            return;
                        }
                }
            }
        }


        /// <summary>
        /// get max value    [float] 
        ///  -> all parameters should have the same vectorsize as the output
        /// - returns vector of same size as input
        /// </summary>
        void get_max_result(ref int code_pointer, ref byte vector_size, out float4 f4)
        {
            // get compressed value of count and vector_size
            code_pointer++;

            byte c = decode62(in code[code_pointer], ref vector_size);

            f4 = float.MinValue;

            // this will compile into a jump table (CHECK THIS !!) todo
            switch ((BlastVectorSizes)vector_size)
            {
                case BlastVectorSizes.float1:
                    {
                        // c == function parameter count 
                        switch (c)
                        {
                            default:
#if DEVELOPMENT_BUILD || TRACE
                                Debug.LogError($"MAX: NUMERIC({vector_size}) parameter count '{c}' not supported");
#endif
                                return;

                            case 2: f4.x = math.max(pop_f1(code_pointer + 1), pop_f1(code_pointer + 2)); break;
                            case 3: f4.x = math.max(math.max(pop_f1(code_pointer + 1), pop_f1(code_pointer + 2)), pop_f1(code_pointer + 3)); break;
                            case 4: f4.x = math.max(math.max(pop_f1(code_pointer + 1), pop_f1(code_pointer + 2)), math.max(pop_f1(code_pointer + 3), pop_f1(code_pointer + 4))); break;
                            case 5: f4.x = math.max(math.max(math.max(pop_f1(code_pointer + 1), pop_f1(code_pointer + 2)), math.max(pop_f1(code_pointer + 3), pop_f1(code_pointer + 4))), pop_f1(code_pointer + 5)); break;
                            case 6: f4.x = math.max(math.max(math.max(pop_f1(code_pointer + 1), pop_f1(code_pointer + 2)), math.max(pop_f1(code_pointer + 3), pop_f1(code_pointer + 4))), math.max(pop_f1(code_pointer + 5), pop_f1(code_pointer + 6))); break;
                            case 7: f4.x = math.max(math.max(math.max(math.max(pop_f1(code_pointer + 1), pop_f1(code_pointer + 2)), math.max(pop_f1(code_pointer + 3), pop_f1(code_pointer + 4))), math.max(pop_f1(code_pointer + 5), pop_f1(code_pointer + 6))), pop_f1(code_pointer + 7)); break;
                            case 8: f4.x = math.max(math.max(math.max(math.max(pop_f1(code_pointer + 1), pop_f1(code_pointer + 2)), math.max(pop_f1(code_pointer + 3), pop_f1(code_pointer + 4))), math.max(pop_f1(code_pointer + 5), pop_f1(code_pointer + 6))), math.max(pop_f1(code_pointer + 7), pop_f1(code_pointer + 8))); break;
                        }
                        code_pointer += c;
                        break;
                    }

                case BlastVectorSizes.float2:
                    {
                        switch (c)
                        {
                            default:
#if DEVELOPMENT_BUILD || TRACE
                                Debug.LogError($"MAX: NUMERIC({vector_size}) parameter count '{c}' not supported");
#endif
                                f4 = float.NaN;
                                break;

                            case 2: f4.xy = math.max(pop_f2(code_pointer + 1), pop_f2(code_pointer + 2)); break;
                            case 3: f4.xy = math.max(math.max(pop_f2(code_pointer + 1), pop_f2(code_pointer + 2)), pop_f2(code_pointer + 3)); break;
                            case 4: f4.xy = math.max(math.max(pop_f2(code_pointer + 1), pop_f2(code_pointer + 2)), math.max(pop_f2(code_pointer + 3), pop_f2(code_pointer + 4))); break;
                            case 5: f4.xy = math.max(math.max(math.max(pop_f2(code_pointer + 1), pop_f2(code_pointer + 2)), math.max(pop_f2(code_pointer + 3), pop_f2(code_pointer + 4))), pop_f2(code_pointer + 5)); break;
                            case 6: f4.xy = math.max(math.max(math.max(pop_f2(code_pointer + 1), pop_f2(code_pointer + 2)), math.max(pop_f2(code_pointer + 3), pop_f2(code_pointer + 4))), math.max(pop_f2(code_pointer + 5), pop_f2(code_pointer + 6))); break;
                            case 7: f4.xy = math.max(math.max(math.max(math.max(pop_f2(code_pointer + 1), pop_f2(code_pointer + 2)), math.max(pop_f2(code_pointer + 3), pop_f2(code_pointer + 4))), math.max(pop_f2(code_pointer + 5), pop_f2(code_pointer + 6))), pop_f2(code_pointer + 7)); break;
                            case 8: f4.xy = math.max(math.max(math.max(math.max(pop_f2(code_pointer + 1), pop_f2(code_pointer + 2)), math.max(pop_f2(code_pointer + 3), pop_f2(code_pointer + 4))), math.max(pop_f2(code_pointer + 5), pop_f2(code_pointer + 6))), math.max(pop_f2(code_pointer + 7), pop_f2(code_pointer + 8))); break;
                        }
                        code_pointer += c;
                        break;
                    }

                case BlastVectorSizes.float3:
                    {
                        switch (c)
                        {
                            default:
#if DEVELOPMENT_BUILD || TRACE
                                Debug.LogError($"MAX: NUMERIC({vector_size}) parameter count '{c}' not supported");
#endif
                                f4 = float.NaN;
                                break;


                            case 2: f4.xyz = math.max(pop_f3(code_pointer + 1), pop_f3(code_pointer + 2)); break;
                            case 3: f4.xyz = math.max(math.max(pop_f3(code_pointer + 1), pop_f3(code_pointer + 2)), pop_f3(code_pointer + 3)); break;
                            case 4: f4.xyz = math.max(math.max(pop_f3(code_pointer + 1), pop_f3(code_pointer + 2)), math.max(pop_f3(code_pointer + 3), pop_f3(code_pointer + 4))); break;
                            case 5: f4.xyz = math.max(math.max(math.max(pop_f3(code_pointer + 1), pop_f3(code_pointer + 2)), math.max(pop_f3(code_pointer + 3), pop_f3(code_pointer + 4))), pop_f3(code_pointer + 5)); break;
                            case 6: f4.xyz = math.max(math.max(math.max(pop_f3(code_pointer + 1), pop_f3(code_pointer + 2)), math.max(pop_f3(code_pointer + 3), pop_f3(code_pointer + 4))), math.max(pop_f3(code_pointer + 5), pop_f3(code_pointer + 6))); break;
                            case 7: f4.xyz = math.max(math.max(math.max(math.max(pop_f3(code_pointer + 1), pop_f3(code_pointer + 2)), math.max(pop_f3(code_pointer + 3), pop_f3(code_pointer + 4))), math.max(pop_f3(code_pointer + 5), pop_f3(code_pointer + 6))), pop_f3(code_pointer + 7)); break;
                            case 8: f4.xyz = math.max(math.max(math.max(math.max(pop_f3(code_pointer + 1), pop_f3(code_pointer + 2)), math.max(pop_f3(code_pointer + 3), pop_f3(code_pointer + 4))), math.max(pop_f3(code_pointer + 5), pop_f3(code_pointer + 6))), math.max(pop_f3(code_pointer + 7), pop_f3(code_pointer + 8))); break;
                        }
                        code_pointer += c;
                        break;
                    }


                // vector_size 0 actually means 4 because of using only 2 bits 
                case 0:
                case BlastVectorSizes.float4:
                    {
                        // only in this case do we need to update vector_size to correct value 
                        vector_size = 4;
                        switch (c)
                        {
                            default:
#if DEVELOPMENT_BUILD || TRACE
                                Debug.LogError($"MAX: NUMERIC(4) parameter count '{c}' not supported -> parser/compiler/optimizer error");
#endif
                                f4 = float.NaN;
                                break;

                            case 2: f4 = math.max(pop_f4(code_pointer + 1), pop_f4(code_pointer + 2)); break;
                            case 3: f4 = math.max(math.max(pop_f4(code_pointer + 1), pop_f4(code_pointer + 2)), pop_f4(code_pointer + 3)); break;
                            case 4: f4 = math.max(math.max(pop_f4(code_pointer + 1), pop_f4(code_pointer + 2)), math.max(pop_f4(code_pointer + 3), pop_f4(code_pointer + 4))); break;
                            case 5: f4 = math.max(math.max(math.max(pop_f4(code_pointer + 1), pop_f4(code_pointer + 2)), math.max(pop_f4(code_pointer + 3), pop_f4(code_pointer + 4))), pop_f4(code_pointer + 5)); break;
                            case 6: f4 = math.max(math.max(math.max(pop_f4(code_pointer + 1), pop_f4(code_pointer + 2)), math.max(pop_f4(code_pointer + 3), pop_f4(code_pointer + 4))), math.max(pop_f4(code_pointer + 5), pop_f4(code_pointer + 6))); break;
                            case 7: f4 = math.max(math.max(math.max(math.max(pop_f4(code_pointer + 1), pop_f4(code_pointer + 2)), math.max(pop_f4(code_pointer + 3), pop_f4(code_pointer + 4))), math.max(pop_f4(code_pointer + 5), pop_f4(code_pointer + 6))), pop_f4(code_pointer + 7)); break;
                            case 8: f4 = math.max(math.max(math.max(math.max(pop_f4(code_pointer + 1), pop_f4(code_pointer + 2)), math.max(pop_f4(code_pointer + 3), pop_f4(code_pointer + 4))), math.max(pop_f4(code_pointer + 5), pop_f4(code_pointer + 6))), math.max(pop_f4(code_pointer + 7), pop_f4(code_pointer + 8))); break;
                        }
                        code_pointer += c;
                        break;
                    }

                default:
                    {
#if DEVELOPMENT_BUILD || TRACE
                        Debug.LogError($"MAX: NUMERIC vector size '{vector_size}' not allowed");
#endif
                        f4 = float.NaN;
                        break;
                    }
            }
        }

        /// <summary>
        /// get min value
        /// - all params should have the same size
        /// - returns vector of same size as input
        /// </summary>
        void get_min_result(ref int code_pointer, ref byte vector_size, out float4 f4)
        {
            code_pointer++;
            byte c = decode62(in code[code_pointer], ref vector_size);

            f4 = float.MaxValue;

            switch ((BlastVectorSizes)vector_size)
            {
                case BlastVectorSizes.float1:
                    {
                        switch (c)
                        {
                            default:
#if DEVELOPMENT_BUILD || TRACE
                                Debug.LogError($"MIN: NUMERIC({vector_size}) parameter count '{c}' not supported");
#endif
                                return;

                            case 2: f4.x = math.min(pop_f1(code_pointer + 1), pop_f1(code_pointer + 2)); break;
                            case 3: f4.x = math.min(math.min(pop_f1(code_pointer + 1), pop_f1(code_pointer + 2)), pop_f1(code_pointer + 3)); break;
                            case 4: f4.x = math.min(math.min(pop_f1(code_pointer + 1), pop_f1(code_pointer + 2)), math.min(pop_f1(code_pointer + 3), pop_f1(code_pointer + 4))); break;
                            case 5: f4.x = math.min(math.min(math.min(pop_f1(code_pointer + 1), pop_f1(code_pointer + 2)), math.min(pop_f1(code_pointer + 3), pop_f1(code_pointer + 4))), pop_f1(code_pointer + 5)); break;
                            case 6: f4.x = math.min(math.min(math.min(pop_f1(code_pointer + 1), pop_f1(code_pointer + 2)), math.min(pop_f1(code_pointer + 3), pop_f1(code_pointer + 4))), math.min(pop_f1(code_pointer + 5), pop_f1(code_pointer + 6))); break;
                            case 7: f4.x = math.min(math.min(math.min(math.min(pop_f1(code_pointer + 1), pop_f1(code_pointer + 2)), math.min(pop_f1(code_pointer + 3), pop_f1(code_pointer + 4))), math.min(pop_f1(code_pointer + 5), pop_f1(code_pointer + 6))), pop_f1(code_pointer + 7)); break;
                            case 8: f4.x = math.min(math.min(math.min(math.min(pop_f1(code_pointer + 1), pop_f1(code_pointer + 2)), math.min(pop_f1(code_pointer + 3), pop_f1(code_pointer + 4))), math.min(pop_f1(code_pointer + 5), pop_f1(code_pointer + 6))), math.min(pop_f1(code_pointer + 7), pop_f1(code_pointer + 8))); break;
                        }
                        code_pointer += c;
                        break;
                    }

                case BlastVectorSizes.float2:
                    {
                        switch (c)
                        {
                            default:
#if DEVELOPMENT_BUILD || TRACE
                                Debug.LogError($"MIN: NUMERIC({vector_size}) parameter count '{c}' not supported");
#endif
                                f4 = float.NaN;
                                break;

                            case 2: f4.xy = math.min(pop_f2(code_pointer + 1), pop_f2(code_pointer + 2)); break;
                            case 3: f4.xy = math.min(math.min(pop_f2(code_pointer + 1), pop_f2(code_pointer + 2)), pop_f2(code_pointer + 3)); break;
                            case 4: f4.xy = math.min(math.min(pop_f2(code_pointer + 1), pop_f2(code_pointer + 2)), math.min(pop_f2(code_pointer + 3), pop_f2(code_pointer + 4))); break;
                            case 5: f4.xy = math.min(math.min(math.min(pop_f2(code_pointer + 1), pop_f2(code_pointer + 2)), math.min(pop_f2(code_pointer + 3), pop_f2(code_pointer + 4))), pop_f2(code_pointer + 5)); break;
                            case 6: f4.xy = math.min(math.min(math.min(pop_f2(code_pointer + 1), pop_f2(code_pointer + 2)), math.min(pop_f2(code_pointer + 3), pop_f2(code_pointer + 4))), math.min(pop_f2(code_pointer + 5), pop_f2(code_pointer + 6))); break;
                            case 7: f4.xy = math.min(math.min(math.min(math.min(pop_f2(code_pointer + 1), pop_f2(code_pointer + 2)), math.min(pop_f2(code_pointer + 3), pop_f2(code_pointer + 4))), math.min(pop_f2(code_pointer + 5), pop_f2(code_pointer + 6))), pop_f2(code_pointer + 7)); break;
                            case 8: f4.xy = math.min(math.min(math.min(math.min(pop_f2(code_pointer + 1), pop_f2(code_pointer + 2)), math.min(pop_f2(code_pointer + 3), pop_f2(code_pointer + 4))), math.min(pop_f2(code_pointer + 5), pop_f2(code_pointer + 6))), math.min(pop_f2(code_pointer + 7), pop_f2(code_pointer + 8))); break;
                        }
                        code_pointer += c;
                        break;
                    }

                case BlastVectorSizes.float3:
                    {
                        switch (c)
                        {
                            default:
#if DEVELOPMENT_BUILD || TRACE
                                Debug.LogError($"MIN: NUMERIC({vector_size}) parameter count '{c}' not supported");
#endif
                                f4 = float.NaN;
                                break;

                            case 2: f4.xyz = math.min(pop_f3(code_pointer + 1), pop_f3(code_pointer + 2)); break;
                            case 3: f4.xyz = math.min(math.min(pop_f3(code_pointer + 1), pop_f3(code_pointer + 2)), pop_f3(code_pointer + 3)); break;
                            case 4: f4.xyz = math.min(math.min(pop_f3(code_pointer + 1), pop_f3(code_pointer + 2)), math.min(pop_f3(code_pointer + 3), pop_f3(code_pointer + 4))); break;
                            case 5: f4.xyz = math.min(math.min(math.min(pop_f3(code_pointer + 1), pop_f3(code_pointer + 2)), math.min(pop_f3(code_pointer + 3), pop_f3(code_pointer + 4))), pop_f3(code_pointer + 5)); break;
                            case 6: f4.xyz = math.min(math.min(math.min(pop_f3(code_pointer + 1), pop_f3(code_pointer + 2)), math.min(pop_f3(code_pointer + 3), pop_f3(code_pointer + 4))), math.min(pop_f3(code_pointer + 5), pop_f3(code_pointer + 6))); break;
                            case 7: f4.xyz = math.min(math.min(math.min(math.min(pop_f3(code_pointer + 1), pop_f3(code_pointer + 2)), math.min(pop_f3(code_pointer + 3), pop_f3(code_pointer + 4))), math.min(pop_f3(code_pointer + 5), pop_f3(code_pointer + 6))), pop_f3(code_pointer + 7)); break;
                            case 8: f4.xyz = math.min(math.min(math.min(math.min(pop_f3(code_pointer + 1), pop_f3(code_pointer + 2)), math.min(pop_f3(code_pointer + 3), pop_f3(code_pointer + 4))), math.min(pop_f3(code_pointer + 5), pop_f3(code_pointer + 6))), math.min(pop_f3(code_pointer + 7), pop_f3(code_pointer + 8))); break;
                        }
                        code_pointer += c;
                        break;
                    }


                // vector_size 0 actually means 4 because of using only 2 bits 
                case 0:
                case BlastVectorSizes.float4:
                    {
                        vector_size = 4;
                        switch (c)
                        {
                            default:
#if DEVELOPMENT_BUILD || TRACE
                                Debug.LogError($"MIN: NUMERIC(4) parameter count '{c}' not supported -> parser/compiler/optimizer error");
#endif
                                f4 = float.NaN;
                                break;

                            case 2: f4 = math.min(pop_f4(code_pointer + 1), pop_f4(code_pointer + 2)); break;
                            case 3: f4 = math.min(math.min(pop_f4(code_pointer + 1), pop_f4(code_pointer + 2)), pop_f4(code_pointer + 3)); break;
                            case 4: f4 = math.min(math.min(pop_f4(code_pointer + 1), pop_f4(code_pointer + 2)), math.min(pop_f4(code_pointer + 3), pop_f4(code_pointer + 4))); break;
                            case 5: f4 = math.min(math.min(math.min(pop_f4(code_pointer + 1), pop_f4(code_pointer + 2)), math.min(pop_f4(code_pointer + 3), pop_f4(code_pointer + 4))), pop_f4(code_pointer + 5)); break;
                            case 6: f4 = math.min(math.min(math.min(pop_f4(code_pointer + 1), pop_f4(code_pointer + 2)), math.min(pop_f4(code_pointer + 3), pop_f4(code_pointer + 4))), math.min(pop_f4(code_pointer + 5), pop_f4(code_pointer + 6))); break;
                            case 7: f4 = math.min(math.min(math.min(math.min(pop_f4(code_pointer + 1), pop_f4(code_pointer + 2)), math.min(pop_f4(code_pointer + 3), pop_f4(code_pointer + 4))), math.min(pop_f4(code_pointer + 5), pop_f4(code_pointer + 6))), pop_f4(code_pointer + 7)); break;
                            case 8: f4 = math.min(math.min(math.min(math.min(pop_f4(code_pointer + 1), pop_f4(code_pointer + 2)), math.min(pop_f4(code_pointer + 3), pop_f4(code_pointer + 4))), math.min(pop_f4(code_pointer + 5), pop_f4(code_pointer + 6))), math.min(pop_f4(code_pointer + 7), pop_f4(code_pointer + 8))); break;
                        }
                        code_pointer += c;
                        break;
                    }

                default:
                    {
#if DEVELOPMENT_BUILD || TRACE
                        Debug.LogError($"MIN: NUMERIC vector size '{vector_size}' not allowed");
#endif
                        f4 = float.NaN;
                        break;
                    }
            }
        }

        #endregion

        #region single input same output math functions 

        /// <summary>
        /// get absolute value of input
        /// - single input function 
        /// - same output vector size as input
        /// </summary>
        void get_abs_result(ref int code_pointer, ref byte vector_size, out float4 f4)
        {
            code_pointer += 1;
            f4 = float.NaN;

            BlastVariableDataType datatype;
            float* fdata = (float*)pop_with_info(in code_pointer, out datatype, out vector_size);

#if DEVELOPMENT_BUILD || TRACE
            if (datatype == BlastVariableDataType.ID)
            {
                Debug.LogError($"ID datatype in abs not yet implemented, codepointer: {code_pointer} = > {code[code_pointer]}");
                return;
            }
#endif

            switch ((BlastVectorSizes)vector_size)
            {
                case BlastVectorSizes.float1: f4.x = math.abs(((float*)fdata)[0]); break;
                case BlastVectorSizes.float2: f4.xy = math.abs(((float2*)fdata)[0]); break;
                case BlastVectorSizes.float3: f4.xyz = math.abs(((float3*)fdata)[0]); break;
                case 0:
                case BlastVectorSizes.float4: f4.xyzw = math.abs(((float4*)fdata)[0]); vector_size = 4; break;
#if DEVELOPMENT_BUILD || TRACE
                default:
                    {
                        Debug.LogError($"ABS: NUMERIC({vector_size}) vectorsize '{vector_size}' not supported");
                        break;
                    }
#endif
            }
        }

        /// <summary>
        /// normalize vector 
        /// - single vector input, size > 1 
        /// - same sized vector output
        /// </summary>
        void get_normalize_result(ref int code_pointer, ref byte vector_size, out float4 f4)
        {
            code_pointer += 1;
            f4 = float.NaN;
            BlastVariableDataType datatype;
            float* fdata = (float*)pop_with_info(in code_pointer, out datatype, out vector_size);

#if DEVELOPMENT_BUILD || TRACE
            if (datatype == BlastVariableDataType.ID)
            {
                Debug.LogError($"ID datatype in normalize not implemented, codepointer: {code_pointer} = > {code[code_pointer]}");
                return;
            }
#endif

            switch ((BlastVectorSizes)vector_size)
            {

                case BlastVectorSizes.float2: f4.xy = math.normalize(((float2*)fdata)[0]); break;
                case BlastVectorSizes.float3: f4.xyz = math.normalize(((float3*)fdata)[0]); break;
                case 0:
                case BlastVectorSizes.float4: f4.xyzw = math.normalize(((float4*)fdata)[0]); vector_size = 4; break;
#if DEVELOPMENT_BUILD || TRACE
                case BlastVectorSizes.float1:
                    {
                        Debug.LogError($"normalize: cannot normalize a scalar variable");
                        break;
                    }
                default:
                    {
                        Debug.LogError($"normalize: NUMERIC({vector_size}) vectorsize '{vector_size}' not supported");
                        break;
                    }
#endif
            }
        }

        /// <summary>
        /// saturate a vector (clamp between 0.0 and 1.0 including 0 and 1)
        /// - 
        /// </summary>
        void get_saturate_result(ref int code_pointer, ref byte vector_size, out float4 f4)
        {
            code_pointer += 1;
            f4 = float.NaN;
            BlastVariableDataType datatype;
            float* fdata = (float*)pop_with_info(in code_pointer, out datatype, out vector_size);

#if DEVELOPMENT_BUILD || TRACE
            if (datatype == BlastVariableDataType.ID)
            {
                Debug.LogError($"ID datatype in saturate not implemented, codepointer: {code_pointer} = > {code[code_pointer]}");
                return;
            }
#endif

            switch ((BlastVectorSizes)vector_size)
            {
                case BlastVectorSizes.float1: f4.x = math.saturate(((float*)fdata)[0]); break;
                case BlastVectorSizes.float2: f4.xy = math.saturate(((float2*)fdata)[0]); break;
                case BlastVectorSizes.float3: f4.xyz = math.saturate(((float3*)fdata)[0]); break;
                case 0:
                case BlastVectorSizes.float4: f4.xyzw = math.saturate(((float4*)fdata)[0]); vector_size = 4; break;
#if DEVELOPMENT_BUILD || TRACE
                default:
                    {
                        Debug.LogError($"saturate: NUMERIC({vector_size}) vectorsize '{vector_size}' not supported");
                        break;
                    }
#endif
            }
        }

        /// <summary>
        /// - 1 parameter fixed
        /// - outputvectorsize == inputvectorsize 
        /// </summary>
        void get_ceilpow2_result(ref int code_pointer, ref byte vector_size, out float4 f4)
        {
            code_pointer += 1;
            f4 = float.NaN;

            BlastVariableDataType datatype;
            float* fdata = (float*)pop_with_info(in code_pointer, out datatype, out vector_size);

#if DEVELOPMENT_BUILD || TRACE
            if (datatype == BlastVariableDataType.ID)
            {
                Debug.LogError($"ID datatype in ceilpow2 not yet implemented, codepointer: {code_pointer} = > {code[code_pointer]}");
                return;
            }
#endif

            // vector size is set by input            
            switch ((BlastVectorSizes)vector_size)
            {
                case 0:
                case BlastVectorSizes.float4: f4 = math.ceilpow2((int4)((float4*)fdata)[0]); vector_size = 4; break;
                case BlastVectorSizes.float1: f4.x = math.ceilpow2((int)((float*)fdata)[0]); break;
                case BlastVectorSizes.float2: f4.xy = math.ceilpow2((int2)((float2*)fdata)[0]); break;
                case BlastVectorSizes.float3: f4.xyz = math.ceilpow2((int3)((float3*)fdata)[0]); break;
#if DEVELOPMENT_BUILD || TRACE
                default:
                    {
                        Debug.LogError($"ceilpow2: numeric vector size '{vector_size}' not supported ");
                        break;
                    }
#endif
            }
        }

        /// <summary>
        /// - 1 parameter fixed
        /// - outputvectorsize == inputvectorsize 
        /// </summary>
        void get_ceillog2_result(ref int code_pointer, ref byte vector_size, out float4 f4)
        {
            code_pointer += 1;
            f4 = float.NaN;

            BlastVariableDataType datatype;
            float* fdata = (float*)pop_with_info(in code_pointer, out datatype, out vector_size);

#if DEVELOPMENT_BUILD || TRACE
            if (datatype == BlastVariableDataType.ID)
            {
                Debug.LogError($"ID datatype in ceil log 2 not yet implemented, codepointer: {code_pointer} = > {code[code_pointer]}");
                return;
            }
#endif

            // vector size is set by input            
            switch ((BlastVectorSizes)vector_size)
            {
                case 0:
                case BlastVectorSizes.float4: f4 = math.ceillog2((int4)((float4*)fdata)[0]); vector_size = 4; break;
                case BlastVectorSizes.float1: f4.x = math.ceillog2((int)((float*)fdata)[0]); break;
                case BlastVectorSizes.float2: f4.xy = math.ceillog2((int2)((float2*)fdata)[0]); break;
                case BlastVectorSizes.float3: f4.xyz = math.ceillog2((int3)((float3*)fdata)[0]); break;
#if DEVELOPMENT_BUILD || TRACE
                default:
                    {
                        Debug.LogError($"ceillog2: numeric vector size '{vector_size}' not supported ");
                        break;
                    }
#endif                                    
            }
        }

        /// <summary>
        /// get value rounded up (ceiling)
        /// - 1 parameter fixed
        /// - outputvectorsize == inputvectorsize 
        /// </summary>
        void get_ceil_result(ref int code_pointer, ref byte vector_size, out float4 f4)
        {
            code_pointer += 1;
            f4 = float.NaN;

            BlastVariableDataType datatype;
            float* fdata = (float*)pop_with_info(in code_pointer, out datatype, out vector_size);

#if DEVELOPMENT_BUILD || TRACE
            if (datatype == BlastVariableDataType.ID)
            {
                Debug.LogError($"ID datatype in ceil not yet implemented, codepointer: {code_pointer} = > {code[code_pointer]}");
                return;
            }
#endif

            // vector size is set by input            
            switch ((BlastVectorSizes)vector_size)
            {
                case 0:
                case BlastVectorSizes.float4: f4 = math.ceil(((float4*)fdata)[0]); vector_size = 4; break;
                case BlastVectorSizes.float1: f4.x = math.ceil(((float*)fdata)[0]); break;
                case BlastVectorSizes.float2: f4.xy = math.ceil(((float2*)fdata)[0]); break;
                case BlastVectorSizes.float3: f4.xyz = math.ceil(((float3*)fdata)[0]); break;
#if DEVELOPMENT_BUILD || TRACE
                default:
                    {
                        Debug.LogError($"ceil: numeric vector size '{vector_size}' not supported ");
                        break;
                    }
#endif
            }
        }

        /// <summary>
        /// truncate value
        /// - 1 parameter fixed
        /// - outputvectorsize == inputvectorsize 
        /// </summary>
        void get_trunc_result(ref int code_pointer, ref byte vector_size, out float4 f4)
        {
            code_pointer += 1;
            f4 = float.NaN;

            BlastVariableDataType datatype;
            float* fdata = (float*)pop_with_info(in code_pointer, out datatype, out vector_size);

#if DEVELOPMENT_BUILD || TRACE
            if (datatype == BlastVariableDataType.ID)
            {
                Debug.LogError($"ID datatype in trunc not yet implemented, codepointer: {code_pointer} = > {code[code_pointer]}");
                return;
            }
#endif

            // vector size is set by input            
            switch ((BlastVectorSizes)vector_size)
            {
                case 0:
                case BlastVectorSizes.float4: f4 = math.trunc(((float4*)fdata)[0]); vector_size = 4; break;
                case BlastVectorSizes.float1: f4.x = math.trunc(((float*)fdata)[0]); break;
                case BlastVectorSizes.float2: f4.xy = math.trunc(((float2*)fdata)[0]); break;
                case BlastVectorSizes.float3: f4.xyz = math.trunc(((float3*)fdata)[0]); break;
#if DEVELOPMENT_BUILD || TRACE
                default:
                    {
                        Debug.LogError($"trunc: numeric vector size '{vector_size}' not supported ");
                        break;
                    }
#endif
            }
        }

        /// <summary>
        /// get value rounded down (floor)
        /// - 1 parameter fixed
        /// - outputvectorsize == inputvectorsize 
        /// </summary>
        void get_floor_result(ref int code_pointer, ref byte vector_size, out float4 f4)
        {
            code_pointer += 1;
            f4 = float.NaN;

            BlastVariableDataType datatype;
            float* fdata = (float*)pop_with_info(in code_pointer, out datatype, out vector_size);

#if DEVELOPMENT_BUILD || TRACE
            if (datatype == BlastVariableDataType.ID)
            {
                Debug.LogError($"ID datatype in floor not yet implemented, codepointer: {code_pointer} = > {code[code_pointer]}");
                return;
            }
#endif

            // vector size is set by input            
            switch ((BlastVectorSizes)vector_size)
            {
                case 0:
                case BlastVectorSizes.float4: f4 = math.floor(((float4*)fdata)[0]); vector_size = 4; break;
                case BlastVectorSizes.float1: f4.x = math.floor(((float*)fdata)[0]); break;
                case BlastVectorSizes.float2: f4.xy = math.floor(((float2*)fdata)[0]); break;
                case BlastVectorSizes.float3: f4.xyz = math.floor(((float3*)fdata)[0]); break;
#if DEVELOPMENT_BUILD || TRACE
                default:
                    {
                        Debug.LogError($"floor: numeric vector size '{vector_size}' not supported ");
                        break;
                    }
#endif
            }
        }

        /// <summary>
        /// - 1 parameter fixed
        /// - outputvectorsize == inputvectorsize 
        /// </summary>
        void get_floorlog2_result(ref int code_pointer, ref byte vector_size, out float4 f4)
        {
            code_pointer += 1;
            f4 = float.NaN;

            BlastVariableDataType datatype;
            float* fdata = (float*)pop_with_info(in code_pointer, out datatype, out vector_size);

#if DEVELOPMENT_BUILD || TRACE
            if (datatype == BlastVariableDataType.ID)
            {
                Debug.LogError($"ID datatype in floor not yet implemented, codepointer: {code_pointer} = > {code[code_pointer]}");
                return;
            }
#endif
            // vector size is set by input            
            switch ((BlastVectorSizes)vector_size)
            {
                case 0:
                case BlastVectorSizes.float4: f4 = math.floorlog2((int4)((float4*)fdata)[0]); vector_size = 4; break;
                case BlastVectorSizes.float1: f4.x = math.floorlog2((int)((float*)fdata)[0]); break;
                case BlastVectorSizes.float2: f4.xy = math.floorlog2((int2)((float2*)fdata)[0]); break;
                case BlastVectorSizes.float3: f4.xyz = math.floorlog2((int3)((float3*)fdata)[0]); break;
#if DEVELOPMENT_BUILD || TRACE
                default:
                    {
                        Debug.LogError($"floor: numeric vector size '{vector_size}' not supported ");
                        break;
                    }
#endif
            }
        }


        /// <summary>
        /// get fraction(s) of value (math.frac)
        /// - 1 parameter fixed
        /// - outputvectorsize == inputvectorsize 
        /// </summary>
        void get_frac_result(ref int code_pointer, ref byte vector_size, out float4 f4)
        {
            code_pointer += 1;
            f4 = float.NaN;

            BlastVariableDataType datatype;
            float* fdata = (float*)pop_with_info(in code_pointer, out datatype, out vector_size);

#if DEVELOPMENT_BUILD || TRACE
            if (datatype == BlastVariableDataType.ID)
            {
                Debug.LogError($"ID datatype in frac not yet implemented, codepointer: {code_pointer} = > {code[code_pointer]}");
                return;
            }
#endif

            // vector size is set by input            
            switch ((BlastVectorSizes)vector_size)
            {
                case 0:
                case BlastVectorSizes.float4: f4 = math.frac(((float4*)fdata)[0]); vector_size = 4; break;
                case BlastVectorSizes.float1: f4.x = math.frac(((float*)fdata)[0]); break;
                case BlastVectorSizes.float2: f4.xy = math.frac(((float2*)fdata)[0]); break;
                case BlastVectorSizes.float3: f4.xyz = math.frac(((float3*)fdata)[0]); break;
#if DEVELOPMENT_BUILD || TRACE
                default:
                    {
                        Debug.LogError($"frac: numeric vector size '{vector_size}' not supported ");
                        break;
                    }
#endif
            }
        }

        /// <summary>
        /// get roots(s) of value (math.sqrt)
        /// - 1 parameter fixed
        /// - outputvectorsize == inputvectorsize 
        /// </summary>
        void get_sqrt_result(ref int code_pointer, ref byte vector_size, out float4 f4)
        {
            code_pointer += 1;
            f4 = float.NaN;

            BlastVariableDataType datatype;
            float* fdata = (float*)pop_with_info(in code_pointer, out datatype, out vector_size);

#if DEVELOPMENT_BUILD || TRACE
            if (datatype == BlastVariableDataType.ID)
            {
                Debug.LogError($"ID datatype in sqrt not yet implemented, codepointer: {code_pointer} = > {code[code_pointer]}");
                return;
            }
#endif

            // vector size is set by input            
            switch ((BlastVectorSizes)vector_size)
            {
                case 0:
                case BlastVectorSizes.float4: f4 = math.sqrt(((float4*)fdata)[0]); vector_size = 4; break;
                case BlastVectorSizes.float1: f4.x = math.sqrt(((float*)fdata)[0]); break;
                case BlastVectorSizes.float2: f4.xy = math.sqrt(((float2*)fdata)[0]); break;
                case BlastVectorSizes.float3: f4.xyz = math.sqrt(((float3*)fdata)[0]); break;
#if DEVELOPMENT_BUILD || TRACE
                default:
                    {
                        Debug.LogError($"sqrt: numeric vector size '{vector_size}' not supported ");
                        break;
                    }
#endif
            }
        }

        /// <summary>
        /// get reciprocal roots(s) of value (math.rsqrt)
        /// - 1 parameter fixed
        /// - outputvectorsize == inputvectorsize 
        /// </summary>
        void get_rsqrt_result(ref int code_pointer, ref byte vector_size, out float4 f4)
        {
            code_pointer += 1;
            f4 = float.NaN;

            BlastVariableDataType datatype;
            float* fdata = (float*)pop_with_info(in code_pointer, out datatype, out vector_size);

#if DEVELOPMENT_BUILD || TRACE
            if (datatype == BlastVariableDataType.ID)
            {
                Debug.LogError($"ID datatype in rsqrt not yet implemented, codepointer: {code_pointer} = > {code[code_pointer]}");
                return;
            }
#endif

            // vector size is set by input            
            switch ((BlastVectorSizes)vector_size)
            {
                case 0:
                case BlastVectorSizes.float4: f4 = math.rsqrt(((float4*)fdata)[0]); vector_size = 4; break;
                case BlastVectorSizes.float1: f4.x = math.rsqrt(((float*)fdata)[0]); break;
                case BlastVectorSizes.float2: f4.xy = math.rsqrt(((float2*)fdata)[0]); break;
                case BlastVectorSizes.float3: f4.xyz = math.rsqrt(((float3*)fdata)[0]); break;
#if DEVELOPMENT_BUILD || TRACE
                default:
                    {
                        Debug.LogError($"rsqrt: numeric vector size '{vector_size}' not supported ");
                        break;
                    }
#endif
            }
        }

        /// <summary>
        /// get sinus of value 
        /// - 1 parameter fixed
        /// - outputvectorsize == inputvectorsize 
        /// </summary>
        void get_sin_result(ref int code_pointer, ref byte vector_size, out float4 f4)
        {
            code_pointer += 1;
            f4 = float.NaN;

            BlastVariableDataType datatype;
            float* fdata = (float*)pop_with_info(in code_pointer, out datatype, out vector_size);

#if DEVELOPMENT_BUILD || TRACE
            if (datatype == BlastVariableDataType.ID)
            {
                Debug.LogError($"ID datatype in sin not yet implemented, codepointer: {code_pointer} = > {code[code_pointer]}");
                return;
            }
#endif

            // vector size is set by input            
            switch ((BlastVectorSizes)vector_size)
            {
                case 0:
                case BlastVectorSizes.float4: f4 = math.sin(((float4*)fdata)[0]); vector_size = 4; break;
                case BlastVectorSizes.float1: f4.x = math.sin(((float*)fdata)[0]); break;
                case BlastVectorSizes.float2: f4.xy = math.sin(((float2*)fdata)[0]); break;
                case BlastVectorSizes.float3: f4.xyz = math.sin(((float3*)fdata)[0]); break;
#if DEVELOPMENT_BUILD || TRACE
                default:
                    {
                        Debug.LogError($"sin: numeric vector size '{vector_size}' not supported ");
                        break;
                    }
#endif
            }
        }

        /// <summary>
        /// get cosine
        /// - 1 parameter fixed
        /// - outputvectorsize == inputvectorsize 
        /// </summary>
        void get_cos_result(ref int code_pointer, ref byte vector_size, out float4 f4)
        {
            code_pointer += 1;
            f4 = float.NaN;

            BlastVariableDataType datatype;
            float* fdata = (float*)pop_with_info(in code_pointer, out datatype, out vector_size);

#if DEVELOPMENT_BUILD || TRACE
            if (datatype == BlastVariableDataType.ID)
            {
                Debug.LogError($"ID datatype in cos not yet implemented, codepointer: {code_pointer} = > {code[code_pointer]}");
                return;
            }
#endif

            // vector size is set by input            
            switch ((BlastVectorSizes)vector_size)
            {
                case 0:
                case BlastVectorSizes.float4: f4 = math.cos(((float4*)fdata)[0]); vector_size = 4; break;
                case BlastVectorSizes.float1: f4.x = math.cos(((float*)fdata)[0]); break;
                case BlastVectorSizes.float2: f4.xy = math.cos(((float2*)fdata)[0]); break;
                case BlastVectorSizes.float3: f4.xyz = math.cos(((float3*)fdata)[0]); break;
#if DEVELOPMENT_BUILD || TRACE
                default:
                    {
                        Debug.LogError($"cos: numeric vector size '{vector_size}' not supported ");
                        break;
                    }
#endif
            }
        }

        /// <summary>
        /// calculate tangent (radians)
        /// - 1 parameter fixed
        /// - outputvectorsize == inputvectorsize 
        /// </summary>
        void get_tan_result(ref int code_pointer, ref byte vector_size, out float4 f4)
        {
            code_pointer += 1;
            f4 = float.NaN;

            BlastVariableDataType datatype;
            float* fdata = (float*)pop_with_info(in code_pointer, out datatype, out vector_size);

#if DEVELOPMENT_BUILD || TRACE
            if (datatype == BlastVariableDataType.ID)
            {
                Debug.LogError($"ID datatype in tan not yet implemented, codepointer: {code_pointer} = > {code[code_pointer]}");
                return;
            }
#endif

            // vector size is set by input            
            switch ((BlastVectorSizes)vector_size)
            {
                case 0:
                case BlastVectorSizes.float4: f4 = math.tan(((float4*)fdata)[0]); vector_size = 4; break;
                case BlastVectorSizes.float1: f4.x = math.tan(((float*)fdata)[0]); break;
                case BlastVectorSizes.float2: f4.xy = math.tan(((float2*)fdata)[0]); break;
                case BlastVectorSizes.float3: f4.xyz = math.tan(((float3*)fdata)[0]); break;
#if DEVELOPMENT_BUILD || TRACE
                default:
                    {
                        Debug.LogError($"tan: numeric vector size '{vector_size}' not supported ");
                        break;
                    }
#endif
            }
        }

        /// <summary>
        /// calculate sin-1 (radians)
        /// - 1 parameter fixed
        /// - outputvectorsize == inputvectorsize 
        /// </summary>
        void get_sinh_result(ref int code_pointer, ref byte vector_size, out float4 f4)
        {
            code_pointer += 1;
            f4 = float.NaN;

            BlastVariableDataType datatype;
            float* fdata = (float*)pop_with_info(in code_pointer, out datatype, out vector_size);

#if DEVELOPMENT_BUILD || TRACE
            if (datatype == BlastVariableDataType.ID)
            {
                Debug.LogError($"ID datatype in sinh not yet implemented, codepointer: {code_pointer} = > {code[code_pointer]}");
                return;
            }
#endif

            // vector size is set by input            
            switch ((BlastVectorSizes)vector_size)
            {
                case 0:
                case BlastVectorSizes.float4: f4 = math.sinh(((float4*)fdata)[0]); vector_size = 4; break;
                case BlastVectorSizes.float1: f4.x = math.sinh(((float*)fdata)[0]); break;
                case BlastVectorSizes.float2: f4.xy = math.sinh(((float2*)fdata)[0]); break;
                case BlastVectorSizes.float3: f4.xyz = math.sinh(((float3*)fdata)[0]); break;
#if DEVELOPMENT_BUILD || TRACE
                default:
                    {
                        Debug.LogError($"sinh: numeric vector size '{vector_size}' not supported ");
                        break;
                    }
#endif
            }
        }

        /// <summary>
        /// calculate arc tangent (radians)
        /// - 1 parameter fixed
        /// - outputvectorsize == inputvectorsize 
        /// </summary>
        void get_atan_result(ref int code_pointer, ref byte vector_size, out float4 f4)
        {
            code_pointer += 1;
            f4 = float.NaN;

            BlastVariableDataType datatype;
            float* fdata = (float*)pop_with_info(in code_pointer, out datatype, out vector_size);

#if DEVELOPMENT_BUILD || TRACE
            if (datatype == BlastVariableDataType.ID)
            {
                Debug.LogError($"ID datatype in atan not yet implemented, codepointer: {code_pointer} = > {code[code_pointer]}");
                return;
            }
#endif

            // vector size is set by input            
            switch ((BlastVectorSizes)vector_size)
            {
                case 0:
                case BlastVectorSizes.float4: f4 = math.atan(((float4*)fdata)[0]); vector_size = 4; break;
                case BlastVectorSizes.float1: f4.x = math.atan(((float*)fdata)[0]); break;
                case BlastVectorSizes.float2: f4.xy = math.atan(((float2*)fdata)[0]); break;
                case BlastVectorSizes.float3: f4.xyz = math.atan(((float3*)fdata)[0]); break;
#if DEVELOPMENT_BUILD || TRACE
                default:
                    {
                        Debug.LogError($"atan: numeric vector size '{vector_size}' not supported ");
                        break;
                    }
#endif
            }
        }

        /// <summary>
        /// calculate cos-1 (radians)
        /// - 1 parameter fixed
        /// - outputvectorsize == inputvectorsize 
        /// </summary>
        void get_cosh_result(ref int code_pointer, ref byte vector_size, out float4 f4)
        {
            code_pointer += 1;
            f4 = float.NaN;

            BlastVariableDataType datatype;
            float* fdata = (float*)pop_with_info(in code_pointer, out datatype, out vector_size);

#if DEVELOPMENT_BUILD || TRACE
            if (datatype == BlastVariableDataType.ID)
            {
                Debug.LogError($"ID datatype in cosh not yet implemented, codepointer: {code_pointer} = > {code[code_pointer]}");
                return;
            }
#endif

            // vector size is set by input            
            switch ((BlastVectorSizes)vector_size)
            {
                case 0:
                case BlastVectorSizes.float4: f4 = math.cosh(((float4*)fdata)[0]); vector_size = 4; break;
                case BlastVectorSizes.float1: f4.x = math.cosh(((float*)fdata)[0]); break;
                case BlastVectorSizes.float2: f4.xy = math.cosh(((float2*)fdata)[0]); break;
                case BlastVectorSizes.float3: f4.xyz = math.cosh(((float3*)fdata)[0]); break;
#if DEVELOPMENT_BUILD || TRACE
                default:
                    {
                        Debug.LogError($"cosh: numeric vector size '{vector_size}' not supported ");
                        break;
                    }
#endif
            }
        }

        /// <summary>
        /// degrees from radians
        /// - 1 parameter fixed
        /// - outputvectorsize == inputvectorsize 
        /// </summary>
        void get_degrees_result(ref int code_pointer, ref byte vector_size, out float4 f4)
        {
            code_pointer += 1;
            f4 = float.NaN;

            BlastVariableDataType datatype;
            float* fdata = (float*)pop_with_info(in code_pointer, out datatype, out vector_size);

#if DEVELOPMENT_BUILD || TRACE
            if (datatype == BlastVariableDataType.ID)
            {
                Debug.LogError($"ID datatype in degrees not yet implemented, codepointer: {code_pointer} = > {code[code_pointer]}");
                return;
            }
#endif

            // vector size is set by input            
            switch ((BlastVectorSizes)vector_size)
            {
                case 0:
                case BlastVectorSizes.float4: f4 = math.degrees(((float4*)fdata)[0]); vector_size = 4; break;
                case BlastVectorSizes.float1: f4.x = math.degrees(((float*)fdata)[0]); break;
                case BlastVectorSizes.float2: f4.xy = math.degrees(((float2*)fdata)[0]); break;
                case BlastVectorSizes.float3: f4.xyz = math.degrees(((float3*)fdata)[0]); break;
#if DEVELOPMENT_BUILD || TRACE
                default:
                    {
                        Debug.LogError($"degrees: numeric vector size '{vector_size}' not supported ");
                        break;
                    }
#endif
            }
        }

        /// <summary>
        /// radians from degrees 
        /// - 1 parameter fixed
        /// - outputvectorsize == inputvectorsize 
        /// </summary>
        void get_rad_result(ref int code_pointer, ref byte vector_size, out float4 f4)
        {
            code_pointer += 1;
            f4 = float.NaN;

            BlastVariableDataType datatype;
            float* fdata = (float*)pop_with_info(in code_pointer, out datatype, out vector_size);

#if DEVELOPMENT_BUILD || TRACE
            if (datatype == BlastVariableDataType.ID)
            {
                Debug.LogError($"ID datatype in radians not yet implemented, codepointer: {code_pointer} = > {code[code_pointer]}");
                return;
            }
#endif

            // vector size is set by input            
            switch ((BlastVectorSizes)vector_size)
            {
                case 0:
                case BlastVectorSizes.float4: f4 = math.radians(((float4*)fdata)[0]); vector_size = 4; break;
                case BlastVectorSizes.float1: f4.x = math.radians(((float*)fdata)[0]); break;
                case BlastVectorSizes.float2: f4.xy = math.radians(((float2*)fdata)[0]); break;
                case BlastVectorSizes.float3: f4.xyz = math.radians(((float3*)fdata)[0]); break;
#if DEVELOPMENT_BUILD || TRACE
                default:
                    {
                        Debug.LogError($"radians: numeric vector size '{vector_size}' not supported ");
                        break;
                    }
#endif
            }
        }

        /// <summary>
        /// calc base2 log from params
        /// - 1 parameter 
        /// - input == output vectorsize 
        /// </summary>
        void get_log2_result(ref int code_pointer, ref byte vector_size, out float4 f4)
        {
            code_pointer += 1;
            f4 = float.NaN;

            BlastVariableDataType datatype;
            float* fdata = (float*)pop_with_info(in code_pointer, out datatype, out vector_size);

#if DEVELOPMENT_BUILD || TRACE
            if (datatype == BlastVariableDataType.ID)
            {
                Debug.LogError($"ID datatype in log2 not yet implemented, codepointer: {code_pointer} = > {code[code_pointer]}");
                return;
            }
#endif

            // vector size is set by input            
            switch ((BlastVectorSizes)vector_size)
            {
                case 0:
                case BlastVectorSizes.float4: f4 = math.log2(((float4*)fdata)[0]); vector_size = 4; break;
                case BlastVectorSizes.float1: f4.x = math.log2(((float*)fdata)[0]); break;
                case BlastVectorSizes.float2: f4.xy = math.log2(((float2*)fdata)[0]); break;
                case BlastVectorSizes.float3: f4.xyz = math.log2(((float3*)fdata)[0]); break;
#if DEVELOPMENT_BUILD || TRACE
                default:
                    {
                        Debug.LogError($"log2: numeric vector size '{vector_size}' not supported ");
                        break;
                    }
#endif
            }
        }

        /// <summary>
        /// calculate natural logarithm
        /// - 1 input
        /// - input == output vectorsize 
        /// </summary>
        void get_log_result(ref int code_pointer, ref byte vector_size, out float4 f4)
        {
            code_pointer += 1;
            f4 = float.NaN;

            BlastVariableDataType datatype;
            float* fdata = (float*)pop_with_info(in code_pointer, out datatype, out vector_size);

#if DEVELOPMENT_BUILD || TRACE
            if (datatype == BlastVariableDataType.ID)
            {
                Debug.LogError($"ID datatype in log not yet implemented, codepointer: {code_pointer} = > {code[code_pointer]}");
                return;
            }
#endif

            // vector size is set by input            
            switch ((BlastVectorSizes)vector_size)
            {
                case 0:
                case BlastVectorSizes.float4: f4 = math.log(((float4*)fdata)[0]); vector_size = 4; break;
                case BlastVectorSizes.float1: f4.x = math.log(((float*)fdata)[0]); break;
                case BlastVectorSizes.float2: f4.xy = math.log(((float2*)fdata)[0]); break;
                case BlastVectorSizes.float3: f4.xyz = math.log(((float3*)fdata)[0]); break;
#if DEVELOPMENT_BUILD || TRACE
                default:
                    {
                        Debug.LogError($"log: numeric vector size '{vector_size}' not supported ");
                        break;
                    }
#endif
            }
        }

        /// <summary>
        /// get base 10 logarithm
        /// - 1 parameter 
        /// - input = output vectorsize 
        /// </summary>
        void get_log10_result(ref int code_pointer, ref byte vector_size, out float4 f4)
        {
            code_pointer += 1;
            f4 = float.NaN;

            BlastVariableDataType datatype;
            float* fdata = (float*)pop_with_info(in code_pointer, out datatype, out vector_size);

#if DEVELOPMENT_BUILD || TRACE
            if (datatype == BlastVariableDataType.ID)
            {
                Debug.LogError($"ID datatype in log10 not yet implemented, codepointer: {code_pointer} = > {code[code_pointer]}");
                return;
            }
#endif

            // vector size is set by input            
            switch ((BlastVectorSizes)vector_size)
            {
                case 0:
                case BlastVectorSizes.float4: f4 = math.log10(((float4*)fdata)[0]); vector_size = 4; break;
                case BlastVectorSizes.float1: f4.x = math.log10(((float*)fdata)[0]); break;
                case BlastVectorSizes.float2: f4.xy = math.log10(((float2*)fdata)[0]); break;
                case BlastVectorSizes.float3: f4.xyz = math.log10(((float3*)fdata)[0]); break;
#if DEVELOPMENT_BUILD || TRACE
                default:
                    {
                        Debug.LogError($"log10: numeric vector size '{vector_size}' not supported ");
                        break;
                    }
#endif
            }
        }

        /// <summary>
        /// raise e to given power
        /// - 1 parameter 
        /// - input = output vectorsize 
        /// </summary>
        void get_exp_result(ref int code_pointer, ref byte vector_size, out float4 f4)
        {
            code_pointer += 1;
            f4 = float.NaN;

            BlastVariableDataType datatype;
            float* fdata = (float*)pop_with_info(in code_pointer, out datatype, out vector_size);

#if DEVELOPMENT_BUILD || TRACE
            if (datatype == BlastVariableDataType.ID)
            {
                Debug.LogError($"ID datatype in exp not yet implemented, codepointer: {code_pointer} = > {code[code_pointer]}");
                return;
            }
#endif

            // vector size is set by input            
            switch ((BlastVectorSizes)vector_size)
            {
                case 0:
                case BlastVectorSizes.float4: f4 = math.exp(((float4*)fdata)[0]); vector_size = 4; break;
                case BlastVectorSizes.float1: f4.x = math.exp(((float*)fdata)[0]); break;
                case BlastVectorSizes.float2: f4.xy = math.exp(((float2*)fdata)[0]); break;
                case BlastVectorSizes.float3: f4.xyz = math.exp(((float3*)fdata)[0]); break;
#if DEVELOPMENT_BUILD || TRACE
                default:
                    {
                        Debug.LogError($"exp: numeric vector size '{vector_size}' not supported ");
                        break;
                    }
#endif
            }
        }

        /// <summary>
        /// get base 10 exponential for parameter 
        /// - 1 parameter 
        /// - input = output vectorsize 
        /// </summary>
        void get_exp10_result(ref int code_pointer, ref byte vector_size, out float4 f4)
        {
            code_pointer += 1;
            f4 = float.NaN;

            BlastVariableDataType datatype;
            float* fdata = (float*)pop_with_info(in code_pointer, out datatype, out vector_size);

#if DEVELOPMENT_BUILD || TRACE
            if (datatype == BlastVariableDataType.ID)
            {
                Debug.LogError($"ID datatype in exp10 not yet implemented, codepointer: {code_pointer} = > {code[code_pointer]}");
                return;
            }
#endif

            // vector size is set by input            
            switch ((BlastVectorSizes)vector_size)
            {
                case 0:
                case BlastVectorSizes.float4: f4 = math.exp10(((float4*)fdata)[0]); vector_size = 4; break;
                case BlastVectorSizes.float1: f4.x = math.exp10(((float*)fdata)[0]); break;
                case BlastVectorSizes.float2: f4.xy = math.exp10(((float2*)fdata)[0]); break;
                case BlastVectorSizes.float3: f4.xyz = math.exp10(((float3*)fdata)[0]); break;
#if DEVELOPMENT_BUILD || TRACE
                default:
                    {
                        Debug.LogError($"exp10: numeric vector size '{vector_size}' not supported ");
                        break;
                    }
#endif
            }
        }

        #endregion

        #region fixed input count math functions

        /// <summary>
        /// raise x to power of y 
        /// - always 2 parameters 
        /// - vectorsize in == vectorsize out
        /// </summary>
        void get_pow_result(ref int code_pointer, ref byte vector_size, out float4 f4)
        {
            f4 = float.NaN;
            BlastVariableDataType datatype;

            float* d1 = (float*)pop_with_info(code_pointer + 1, out datatype, out vector_size);

#if DEVELOPMENT_BUILD || TRACE
            BlastVariableDataType datatype_2;
            byte vector_size_2;

            float* d2 = (float*)pop_with_info(code_pointer + 2, out datatype_2, out vector_size_2);

            if (datatype == BlastVariableDataType.ID)
            {
                Debug.LogError($"ID datatype in pow not implemented, codepointer: {code_pointer} = > {code[code_pointer]}");
                return;
            }
            if (datatype != datatype_2 || vector_size != vector_size_2)
            {
                Debug.LogError($"pow: input parameter datatype|vector_size mismatch, these must be equal. p1 = {datatype}.{vector_size}, p2 = {datatype_2}.{vector_size_2}");
                return;
            }
#endif

            switch ((BlastVectorSizes)vector_size)
            {
                case 0:
#if DEVELOPMENT_BUILD || TRACE
                case BlastVectorSizes.float4: f4 = math.pow(((float4*)d1)[0], ((float4*)d2)[0]); vector_size = 4; break;
                case BlastVectorSizes.float1: f4.x = math.pow(((float*)d1)[0], ((float*)d2)[0]); break;
                case BlastVectorSizes.float2: f4.xy = math.pow(((float2*)d1)[0], ((float2*)d2)[0]); break;
                case BlastVectorSizes.float3: f4.xyz = math.pow(((float3*)d1)[0], ((float3*)d2)[0]); break;
#else
                // in release build we dont need information on the second parameter 
                case BlastVectorSizes.float4: f4 = math.pow(((float4*)d1)[0], pop_f4(code_pointer + 2)); vector_size = 4; break;
                case BlastVectorSizes.float1: f4.x = math.pow(((float*)d1)[0], pop_f1(code_pointer + 2)); break;
                case BlastVectorSizes.float2: f4.xy = math.pow(((float2*)d1)[0], pop_f2(code_pointer + 2)); break;
                case BlastVectorSizes.float3: f4.xyz = math.pow(((float3*)d1)[0], pop_f3(code_pointer + 2)); break;
#endif

#if DEVELOPMENT_BUILD || TRACE
                default:
                    {
                        Debug.LogError($"pow: {datatype} vector size '{vector_size}' not supported ");
                        break;
                    }
#endif
            }

            code_pointer += 2;
        }


        /// <summary>
        /// floating point modulus | rest of division
        /// </summary>
        /// <param name="code_pointer"></param>
        /// <param name="vector_size"></param>
        /// <param name="f4"></param>
#if !DEBUG
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        void get_fmod_result(ref int code_pointer, ref byte vector_size, out float4 f4)
        {
            f4 = float.NaN;
            BlastVariableDataType datatype;

            float* d1 = (float*)pop_with_info(code_pointer + 1, out datatype, out vector_size);

#if DEVELOPMENT_BUILD || TRACE
            BlastVariableDataType datatype_2;
            byte vector_size_2;

            float* d2 = (float*)pop_with_info(code_pointer + 2, out datatype_2, out vector_size_2);

            if (datatype != BlastVariableDataType.Numeric)
            {
                Debug.LogError($"Only the numeric datatype in fmod is implemented, codepointer: {code_pointer} = > {code[code_pointer]}");
                return;
            }
            if (datatype != datatype_2 || vector_size != vector_size_2)
            {
                Debug.LogError($"fmod: input parameter datatype|vector_size mismatch, these must be equal. p1 = {datatype}.{vector_size}, p2 = {datatype_2}.{vector_size_2}");
                return;
            }
#endif

            switch ((BlastVectorSizes)vector_size)
            {
                case 0:
#if DEVELOPMENT_BUILD || TRACE
                case BlastVectorSizes.float4: f4 = math.fmod(((float4*)d1)[0], ((float4*)d2)[0]); vector_size = 4; break;
                case BlastVectorSizes.float1: f4.x = math.fmod(((float*)d1)[0], ((float*)d2)[0]); break;
                case BlastVectorSizes.float2: f4.xy = math.fmod(((float2*)d1)[0], ((float2*)d2)[0]); break;
                case BlastVectorSizes.float3: f4.xyz = math.fmod(((float3*)d1)[0], ((float3*)d2)[0]); break;
#else
                // in release build we dont need information on the second parameter 
                case BlastVectorSizes.float4: f4 = math.fmod(((float4*)d1)[0], pop_f4(code_pointer + 2)); vector_size = 4; break;
                case BlastVectorSizes.float1: f4.x = math.fmod(((float*)d1)[0], pop_f1(code_pointer + 2)); break;
                case BlastVectorSizes.float2: f4.xy = math.fmod(((float2*)d1)[0], pop_f2(code_pointer + 2)); break;
                case BlastVectorSizes.float3: f4.xyz = math.fmod(((float3*)d1)[0], pop_f3(code_pointer + 2)); break;
#endif

#if DEVELOPMENT_BUILD || TRACE
                default:
                    {
                        Debug.LogError($"fmod: {datatype} vector size '{vector_size}' not supported ");
                        break;
                    }
#endif
            }

            code_pointer += 2;
        }


        /// <summary>
        /// get the full arctangengs -pi to +pi radians
        /// </summary>
        void get_atan2_result(ref int code_pointer, ref byte vector_size, out float4 f4)
        {
            f4 = float.NaN;
            BlastVariableDataType datatype;

            float* d1 = (float*)pop_with_info(code_pointer + 1, out datatype, out vector_size);

#if DEVELOPMENT_BUILD || TRACE
            BlastVariableDataType datatype_2;
            byte vector_size_2;

            float* d2 = (float*)pop_with_info(code_pointer + 2, out datatype_2, out vector_size_2);

            if (datatype == BlastVariableDataType.ID)
            {
                Debug.LogError($"ID datatype in atan2 not implemented, codepointer: {code_pointer} = > {code[code_pointer]}");
                return;
            }
            if (datatype != datatype_2 || vector_size != vector_size_2)
            {
                Debug.LogError($"atan2: input parameter datatype|vector_size mismatch, these must be equal. p1 = {datatype}.{vector_size}, p2 = {datatype_2}.{vector_size_2}");
                return;
            }
#endif

            switch ((BlastVectorSizes)vector_size)
            {
                case 0:
#if DEVELOPMENT_BUILD || TRACE
                case BlastVectorSizes.float4: f4 = math.atan2(((float4*)d1)[0], ((float4*)d2)[0]); vector_size = 4; break;
                case BlastVectorSizes.float1: f4.x = math.atan2(((float*)d1)[0], ((float*)d2)[0]); break;
                case BlastVectorSizes.float2: f4.xy = math.atan2(((float2*)d1)[0], ((float2*)d2)[0]); break;
                case BlastVectorSizes.float3: f4.xyz = math.atan2(((float3*)d1)[0], ((float3*)d2)[0]); break;
#else
                // in release build we dont need information on the second parameter 
                case BlastVectorSizes.float4: f4 = math.atan2(((float4*)d1)[0], pop_f4(code_pointer + 2)); vector_size = 4; break;
                case BlastVectorSizes.float1: f4.x = math.atan2(((float*)d1)[0], pop_f1(code_pointer + 2)); break;
                case BlastVectorSizes.float2: f4.xy = math.atan2(((float2*)d1)[0], pop_f2(code_pointer + 2)); break;
                case BlastVectorSizes.float3: f4.xyz = math.atan2(((float3*)d1)[0], pop_f3(code_pointer + 2)); break;
#endif

#if DEVELOPMENT_BUILD || TRACE
                default:
                    {
                        Debug.LogError($"atan2: {datatype} vector size '{vector_size}' not supported ");
                        break;
                    }
#endif
            }

            code_pointer += 2;
        }


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

            float* d1 = (float*)pop_with_info(code_pointer + 1, out datatype, out vector_size);

#if DEVELOPMENT_BUILD || TRACE
            BlastVariableDataType datatype_2;
            byte vector_size_2;

            float* d2 = (float*)pop_with_info(code_pointer + 2, out datatype_2, out vector_size_2);

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
                case BlastVectorSizes.float4: f4.x = math.dot(((float4*)d1)[0], ((float4*)d2)[0]); break;
                case BlastVectorSizes.float1: f4.x = math.dot(((float*)d1)[0], ((float*)d2)[0]); break;
                case BlastVectorSizes.float2: f4.x = math.dot(((float2*)d1)[0], ((float2*)d2)[0]); break;
                case BlastVectorSizes.float3: f4.x = math.dot(((float3*)d1)[0], ((float3*)d2)[0]); break;
#else
                // in release build we dont need information on the second parameter 
                case BlastVectorSizes.float4: f4.x = math.dot(((float4*)d1)[0], pop_f4(code_pointer + 2)); break;
                case BlastVectorSizes.float1: f4.x = math.dot(((float*)d1)[0], pop_f1(code_pointer + 2)); break;
                case BlastVectorSizes.float2: f4.x = math.dot(((float2*)d1)[0], pop_f2(code_pointer + 2)); break;
                case BlastVectorSizes.float3: f4.x = math.dot(((float3*)d1)[0], pop_f3(code_pointer + 2)); break;
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

            float* d1 = (float*)pop_with_info(code_pointer + 1, out datatype, out vector_size);

#if DEVELOPMENT_BUILD || TRACE
            BlastVariableDataType datatype_2;
            byte vector_size_2;

            float* d2 = (float*)pop_with_info(code_pointer + 2, out datatype_2, out vector_size_2);

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
                case BlastVectorSizes.float3: f4.xyz = math.cross(((float3*)d1)[0], ((float3*)d2)[0]); break;
#else
                // in release build we dont need information on the second parameter 
                case BlastVectorSizes.float3: f4.xyz = math.cross(((float3*)d1)[0], pop_f3(code_pointer + 2)); break;
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


        #endregion

        #region fused substract multiply actions

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

            void* pm1 = pop_with_info(code_pointer + 1, out datatype, out vector_size);
            if (datatype == BlastVariableDataType.ID)
            {
                Debug.Log($"fma: ID datatype not yet supported, codepointer {code_pointer}, vectorsize: {vector_size}");
                return;
            }

            void* pm2 = pop_with_info(code_pointer + 2, out popped_datatype, out popped_vector_size);
            if (datatype != popped_datatype || vector_size != popped_vector_size)
            {
                Debug.Log($"fma: [datatype|vectorsize] mismatch, codepointer {code_pointer}, all parameters must share the same datatype, m1 = {datatype}.{vector_size} while m2 = {popped_datatype}.{popped_vector_size}");
                return;
            }

            void* pa1 = pop_with_info(code_pointer + 3, out datatype, out vector_size);
            if (datatype != popped_datatype || vector_size != popped_vector_size)
            {
                Debug.Log($"fma: [datatype|vectorsize] mismatch, codepointer {code_pointer}, all parameters must share the same datatype, m1 = {datatype}.{vector_size} while a1 = {popped_datatype}.{popped_vector_size}");
                return;
            }

            // in debug pop all with info and verify datatypes 
            switch ((BlastVectorSizes)vector_size)
            {
                case BlastVectorSizes.float1: f4.x = math.mad(((float*)pm1)[0], ((float*)pm2)[0], ((float*)pa1)[0]); break;
                case BlastVectorSizes.float2: f4.xy = math.mad(((float2*)pm1)[0], ((float2*)pm2)[0], ((float2*)pa1)[0]); break;
                case BlastVectorSizes.float3: f4.xyz = math.mad(((float3*)pm1)[0], ((float3*)pm2)[0], ((float3*)pa1)[0]); break;
                case 0:
                case BlastVectorSizes.float4:
                    f4 = math.mad(((float4*)pm1)[0], ((float4*)pm2)[0], ((float4*)pa1)[0]);
                    vector_size = 4;
                    break;
            }

#else
            // in release => pop once with info, others direct 
            void* pm1 = pop_with_info(code_pointer + 1, out datatype, out vector_size);

            switch ((BlastVectorSizes)vector_size)
            {
                case BlastVectorSizes.float1: f4.x = math.mad(((float*)pm1)[0], pop_f1(code_pointer + 2), pop_f1(code_pointer + 3)); break;
                case BlastVectorSizes.float2: f4.xy = math.mad(((float2*)pm1)[0], pop_f2(code_pointer + 2), pop_f2(code_pointer + 3)); break;
                case BlastVectorSizes.float3: f4.xyz = math.mad(((float3*)pm1)[0], pop_f3(code_pointer + 2), pop_f3(code_pointer + 3)); break;
                case 0:
                case BlastVectorSizes.float4:
                    f4 = math.mad(((float4*)pm1)[0], pop_f4(code_pointer + 2), pop_f4(code_pointer + 3));
                    vector_size = 4;
                    break;
            }

#endif

            code_pointer += 3;
        }


        #endregion

        #region math utility functions (select, lerp etc) 

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


            void* p1 = pop_with_info(code_pointer + 1, out datatype, out vector_size);

            // we cant skip popping the second with information on release builds because out stackpointer could stray
            void* p2 = pop_with_info(code_pointer + 2, out datatype2, out vsize2);

            void* pcondition = pop_with_info(code_pointer + 3, out datatype_condition, out vsize_condition);

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
            code_pointer += 3;

            if (vsize_condition == 1)
            {
                switch ((BlastVectorSizes)vector_size)
                {
                    case 0:
                    case BlastVectorSizes.float4: f4 = math.select(((float4*)p1)[0], ((float4*)p2)[0], ((float*)pcondition)[0] != 0); vector_size = 4; break;
                    case BlastVectorSizes.float3: f4.xyz = math.select(((float3*)p1)[0], ((float3*)p2)[0], ((float*)pcondition)[0] != 0); break;
                    case BlastVectorSizes.float2: f4.xy = math.select(((float2*)p1)[0], ((float2*)p2)[0], ((float*)pcondition)[0] != 0); break;
                    case BlastVectorSizes.float1: f4.x = math.select(((float*)p1)[0], ((float*)p2)[0], ((float*)pcondition)[0] != 0); break;

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
                    case BlastVectorSizes.float4: f4 = math.select(((float4*)p1)[0], ((float4*)p2)[0], ((float4*)pcondition)[0] != 0); vector_size = 4; break;
                    case BlastVectorSizes.float3: f4.xyz = math.select(((float3*)p1)[0], ((float3*)p2)[0], ((float3*)pcondition)[0] != 0); break;
                    case BlastVectorSizes.float2: f4.xy = math.select(((float2*)p1)[0], ((float2*)p2)[0], ((float2*)pcondition)[0] != 0); break;
                    case BlastVectorSizes.float1: f4.x = math.select(((float*)p1)[0], ((float*)p2)[0], ((float*)pcondition)[0] != 0); break;

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

            void* p1 = pop_with_info(code_pointer + 2, out datatype_mm, out vector_size_mm);
            void* p2 = pop_with_info(code_pointer + 3, out datatype_a, out vector_size);       // could optimize this to pop_fx in release but not much win

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

            void* pa = pop_with_info(code_pointer + 1, out datatype_a, out vector_size);
            code_pointer += 3;

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
                    case BlastVectorSizes.float4: f4 = math.clamp(((float4*)pa)[0], ((float*)p1)[0], ((float*)p2)[0]); vector_size_mm = 4; break;
                    case BlastVectorSizes.float3: f4.xyz = math.clamp(((float3*)pa)[0], ((float*)p1)[0], ((float*)p2)[0]); break;
                    case BlastVectorSizes.float2: f4.xy = math.clamp(((float2*)pa)[0], ((float*)p1)[0], ((float*)p2)[0]); break;
                    case BlastVectorSizes.float1: f4.x = math.clamp(((float*)pa)[0], ((float*)p1)[0], ((float*)p2)[0]); break;
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
                    case BlastVectorSizes.float4: f4 = math.clamp(((float4*)pa)[0], ((float4*)p1)[0], ((float4*)p2)[0]); vector_size_mm = 4; break;
                    case BlastVectorSizes.float3: f4.xyz = math.clamp(((float3*)pa)[0], ((float3*)p1)[0], ((float3*)p2)[0]); break;
                    case BlastVectorSizes.float2: f4.xy = math.clamp(((float2*)pa)[0], ((float2*)p1)[0], ((float2*)p2)[0]); break;
                    case BlastVectorSizes.float1: f4.x = math.clamp(((float*)pa)[0], ((float*)p1)[0], ((float*)p2)[0]); break;

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

            void* p1 = pop_with_info(code_pointer + 1, out p1_datatype, out vector_size);
            void* p2 = pop_with_info(code_pointer + 2, out p2_datatype, out p2_vector_size);
            void* p3 = pop_with_info(code_pointer + 3, out p3_datatype, out p3_vector_size);

            f4 = float.NaN;
            code_pointer += 3;

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
                    case BlastVectorSizes.float4: f4 = math.lerp(((float4*)p1)[0], ((float4*)p2)[0], ((float*)p3)[0]); vector_size = 4; break;
                    case BlastVectorSizes.float1: f4.x = math.lerp(((float*)p1)[0], ((float*)p2)[0], ((float*)p3)[0]); break;
                    case BlastVectorSizes.float2: f4.xy = math.lerp(((float2*)p1)[0], ((float2*)p2)[0], ((float*)p3)[0]); break;
                    case BlastVectorSizes.float3: f4.xyz = math.lerp(((float3*)p1)[0], ((float3*)p2)[0], ((float*)p3)[0]); break;
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
                    case BlastVectorSizes.float4: f4 = math.lerp(((float4*)p1)[0], ((float4*)p2)[0], ((float4*)p3)[0]); vector_size = 4; break;
                    case BlastVectorSizes.float1: f4.x = math.lerp(((float*)p1)[0], ((float*)p2)[0], ((float*)p3)[0]); break;
                    case BlastVectorSizes.float2: f4.xy = math.lerp(((float2*)p1)[0], ((float2*)p2)[0], ((float2*)p3)[0]); break;
                    case BlastVectorSizes.float3: f4.xyz = math.lerp(((float3*)p1)[0], ((float3*)p2)[0], ((float3*)p3)[0]); break;
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

            void* p1 = pop_with_info(code_pointer + 1, out p1_datatype, out vector_size);
            void* p2 = pop_with_info(code_pointer + 2, out p2_datatype, out p2_vector_size);
            void* p3 = pop_with_info(code_pointer + 3, out p3_datatype, out p3_vector_size);

            f4 = float.NaN;
            code_pointer += 3;

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
#endif

            if (p3_vector_size == 1)
            {
                switch ((BlastVectorSizes)vector_size)
                {
                    case 0:
                    case BlastVectorSizes.float4: f4 = math.slerp(((quaternion*)p1)[0], ((quaternion*)p2)[0], ((float*)p3)[0]).value; vector_size = 4; break;
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
                    case BlastVectorSizes.float4: f4 = math.slerp(((quaternion*)p1)[0], ((quaternion*)p2)[0], ((float*)p3)[0]).value; vector_size = 4; break;
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

            void* p1 = pop_with_info(code_pointer + 1, out p1_datatype, out vector_size);
            void* p2 = pop_with_info(code_pointer + 2, out p2_datatype, out p2_vector_size);
            void* p3 = pop_with_info(code_pointer + 3, out p3_datatype, out p3_vector_size);

            f4 = float.NaN;
            code_pointer += 3;

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

            void* p1 = pop_with_info(code_pointer + 1, out p1_datatype, out vector_size);
            void* p2 = pop_with_info(code_pointer + 2, out p2_datatype, out p2_vector_size);
            void* p3 = pop_with_info(code_pointer + 3, out p3_datatype, out p3_vector_size);

            f4 = float.NaN;
            code_pointer += 3;

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
                    case BlastVectorSizes.float4: f4 = math.unlerp(((float4*)p1)[0], ((float4*)p2)[0], ((float*)p3)[0]); vector_size = 4; break;
                    case BlastVectorSizes.float1: f4.x = math.unlerp(((float*)p1)[0], ((float*)p2)[0], ((float*)p3)[0]); break;
                    case BlastVectorSizes.float2: f4.xy = math.unlerp(((float2*)p1)[0], ((float2*)p2)[0], ((float*)p3)[0]); break;
                    case BlastVectorSizes.float3: f4.xyz = math.unlerp(((float3*)p1)[0], ((float3*)p2)[0], ((float*)p3)[0]); break;
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
                    case BlastVectorSizes.float4: f4 = math.unlerp(((float4*)p1)[0], ((float4*)p2)[0], ((float4*)p3)[0]); vector_size = 4; break;
                    case BlastVectorSizes.float1: f4.x = math.unlerp(((float*)p1)[0], ((float*)p2)[0], ((float*)p3)[0]); break;
                    case BlastVectorSizes.float2: f4.xy = math.unlerp(((float2*)p1)[0], ((float2*)p2)[0], ((float2*)p3)[0]); break;
                    case BlastVectorSizes.float3: f4.xyz = math.unlerp(((float3*)p1)[0], ((float3*)p2)[0], ((float3*)p3)[0]); break;
#if DEVELOPMENT_BUILD || TRACE
                    default:
                        Debug.LogError($"lerp: vector_size {vector_size} not supported");
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

            void* p1 = pop_with_info(code_pointer + 1, out p1_datatype, out vector_size);
            void* p2 = pop_with_info(code_pointer + 2, out p2_datatype, out p2_vector_size);
            void* p3 = pop_with_info(code_pointer + 3, out p3_datatype, out p3_vector_size);
            void* p4 = pop_with_info(code_pointer + 4, out p4_datatype, out p4_vector_size);
            void* p5 = pop_with_info(code_pointer + 5, out p5_datatype, out p5_vector_size);

            f4 = float.NaN;
            code_pointer += 5;

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
                case BlastVectorSizes.float4: f4 = math.remap(((float4*)p1)[0], ((float4*)p2)[0], ((float4*)p3)[0], ((float4*)p4)[0], ((float4*)p5)[0]); vector_size = 4; break;
                case BlastVectorSizes.float1: f4.x = math.remap(((float*)p1)[0], ((float*)p2)[0], ((float*)p3)[0], ((float*)p4)[0], ((float*)p5)[0]); break;
                case BlastVectorSizes.float2: f4.xy = math.remap(((float2*)p1)[0], ((float2*)p2)[0], ((float2*)p3)[0], ((float2*)p4)[0], ((float2*)p5)[0]); break;
                case BlastVectorSizes.float3: f4.xyz = math.remap(((float3*)p1)[0], ((float3*)p2)[0], ((float3*)p3)[0], ((float3*)p5)[0], ((float3*)p5)[0]); break;
#if DEVELOPMENT_BUILD || TRACE
                default:
                    Debug.LogError($"blast.interpretor.remap: vector_size {vector_size} not supported");
                    break;
#endif
            }
        }

        #endregion

        #region mula family 


        /// <summary>
        /// multiply all inputs together
        /// - equal sized vectors 
        /// - n elements 
        /// </summary>
        void get_mula_result(ref int code_pointer, ref byte vector_size, out float4 result)
        {
            code_pointer++;
            byte c = code[code_pointer];

            vector_size = (byte)(c & 0b11);
            c = (byte)((c & 0b1111_1100) >> 2);

            result = float.NaN;

            Unity.Burst.Intrinsics.v128 v1 = new Unity.Burst.Intrinsics.v128();
            Unity.Burst.Intrinsics.v128 v2 = new Unity.Burst.Intrinsics.v128();
            switch (vector_size)
            {
                case 1:
                    {
                        switch (c)
                        {
                            case 1:  // should probably just raise a error on this and case 2 
                                result.x = pop_f1(code_pointer + 1); //  data[code[code_pointer + 1] - opt_id];
                                code_pointer += 1;
                                break;

                            case 2:
                                result.x = pop_f1(code_pointer + 1) * pop_f1(code_pointer + 2);
                                code_pointer += 2;
                                break;

                            case 3:
                                result.x = pop_f1(code_pointer + 1) * pop_f1(code_pointer + 2) * pop_f1(code_pointer + 3);
                                code_pointer += 3;
                                break;

                            case 4:
                                v1.Float0 = pop_f1(code_pointer + 1);
                                v1.Float1 = pop_f1(code_pointer + 2);
                                v2.Float0 = pop_f1(code_pointer + 3);
                                v2.Float1 = pop_f1(code_pointer + 4);
                                v1 = Unity.Burst.Intrinsics.X86.Sse.mul_ps(v1, v2);
                                result.x = v1.Float0 * v1.Float1;
                                code_pointer += c;
                                break;

                            case 5:
                                v1.Float0 = pop_f1(code_pointer + 1);
                                v1.Float1 = pop_f1(code_pointer + 2);
                                v2.Float0 = pop_f1(code_pointer + 3);
                                v2.Float1 = pop_f1(code_pointer + 4);

                                v1 = Unity.Burst.Intrinsics.X86.Sse.mul_ps(v1, v2);
                                result.x = v1.Float0 * v1.Float1 * pop_f1(code_pointer + 5);
                                code_pointer += c;
                                break;

                            case 6:
                                v1.Float0 = pop_f1(code_pointer + 1);
                                v1.Float1 = pop_f1(code_pointer + 2);
                                v1.Float2 = pop_f1(code_pointer + 3);
                                v2.Float0 = pop_f1(code_pointer + 4);
                                v2.Float1 = pop_f1(code_pointer + 5);
                                v2.Float2 = pop_f1(code_pointer + 6);

                                v1 = Unity.Burst.Intrinsics.X86.Sse.mul_ps(v1, v2);

                                result.x = v1.Float0 * v1.Float1 * v1.Float2;
                                code_pointer += c;
                                break;

                            case 7:
                                v1.Float0 = pop_f1(code_pointer + 1);
                                v1.Float1 = pop_f1(code_pointer + 2);
                                v1.Float2 = pop_f1(code_pointer + 3);
                                v2.Float0 = pop_f1(code_pointer + 4);
                                v2.Float1 = pop_f1(code_pointer + 5);
                                v2.Float2 = pop_f1(code_pointer + 6);
                                v1 = Unity.Burst.Intrinsics.X86.Sse.mul_ps(v1, v2);
                                result.x = v1.Float0 * v1.Float1 * v1.Float2 * pop_f1(code_pointer + 7);
                                code_pointer += c;
                                break;

                            case 8:
                                v1.Float0 = pop_f1(code_pointer + 1);
                                v1.Float1 = pop_f1(code_pointer + 2);
                                v1.Float2 = pop_f1(code_pointer + 3);
                                v1.Float3 = pop_f1(code_pointer + 4);
                                v2.Float0 = pop_f1(code_pointer + 5);
                                v2.Float1 = pop_f1(code_pointer + 6);
                                v2.Float2 = pop_f1(code_pointer + 7);
                                v2.Float3 = pop_f1(code_pointer + 8);

                                v1 = Unity.Burst.Intrinsics.X86.Sse.mul_ps(v1, v2);

                                result.x = v1.Float0 * v1.Float1 * v1.Float2 * v1.Float3;
                                code_pointer += c;
                                break;

                            case 9:
                                v1.Float0 = pop_f1(code_pointer + 1);
                                v1.Float1 = pop_f1(code_pointer + 2);
                                v1.Float2 = pop_f1(code_pointer + 3);
                                v1.Float3 = pop_f1(code_pointer + 4);
                                v2.Float0 = pop_f1(code_pointer + 5);
                                v2.Float1 = pop_f1(code_pointer + 6);
                                v2.Float2 = pop_f1(code_pointer + 7);
                                v2.Float3 = pop_f1(code_pointer + 8);

                                v1 = Unity.Burst.Intrinsics.X86.Sse.mul_ps(v1, v2);

                                result.x = v1.Float0 * v1.Float1 * v1.Float2 * v1.Float3 * pop_f1(code_pointer + 9);
                                code_pointer += c;
                                break;

                            case 10:
                                v1.Float0 = pop_f1(code_pointer + 1);
                                v1.Float1 = pop_f1(code_pointer + 2);
                                v1.Float2 = pop_f1(code_pointer + 3);
                                v1.Float3 = pop_f1(code_pointer + 4);
                                v2.Float0 = pop_f1(code_pointer + 5);
                                v2.Float1 = pop_f1(code_pointer + 6);
                                v2.Float2 = pop_f1(code_pointer + 7);
                                v2.Float3 = pop_f1(code_pointer + 8);

                                v1 = Unity.Burst.Intrinsics.X86.Sse.mul_ps(v1, v2);

                                v2.Float0 = pop_f1(code_pointer + 9);
                                v2.Float1 = pop_f1(code_pointer + 10);
                                v2.Float2 = v1.Float3; // save multiplication by v1.float3 later
                                v2.Float3 = 1;

                                v1 = Unity.Burst.Intrinsics.X86.Sse.mul_ps(v1, v2);

                                result.x = v1.Float0 * v1.Float1 * v1.Float2;// * v1.Float3;
                                code_pointer += c;
                                break;

                            case 11:
                                v1.Float0 = pop_f1(code_pointer + 1);
                                v1.Float1 = pop_f1(code_pointer + 2);
                                v1.Float2 = pop_f1(code_pointer + 3);
                                v1.Float3 = pop_f1(code_pointer + 4);
                                v2.Float0 = pop_f1(code_pointer + 5);
                                v2.Float1 = pop_f1(code_pointer + 6);
                                v2.Float2 = pop_f1(code_pointer + 7);
                                v2.Float3 = pop_f1(code_pointer + 8);

                                v1 = Unity.Burst.Intrinsics.X86.Sse.mul_ps(v1, v2);

                                v2.Float0 = pop_f1(code_pointer + 9);
                                v2.Float1 = pop_f1(code_pointer + 10);
                                v2.Float2 = pop_f1(code_pointer + 11);
                                v2.Float3 = 1;

                                v1 = Unity.Burst.Intrinsics.X86.Sse.mul_ps(v1, v2);

                                result.x = v1.Float0 * v1.Float1 * v1.Float2 * v1.Float3;
                                code_pointer += c;
                                break;

                            case 12:
                                v1.Float0 = pop_f1(code_pointer + 1);
                                v1.Float1 = pop_f1(code_pointer + 2);
                                v1.Float2 = pop_f1(code_pointer + 3);
                                v1.Float3 = pop_f1(code_pointer + 4);
                                v2.Float0 = pop_f1(code_pointer + 5);
                                v2.Float1 = pop_f1(code_pointer + 6);
                                v2.Float2 = pop_f1(code_pointer + 7);
                                v2.Float3 = pop_f1(code_pointer + 8);

                                v1 = Unity.Burst.Intrinsics.X86.Sse.mul_ps(v1, v2);

                                v2.Float0 = pop_f1(code_pointer + 9);
                                v2.Float1 = pop_f1(code_pointer + 10);
                                v2.Float2 = pop_f1(code_pointer + 11);
                                v2.Float3 = pop_f1(code_pointer + 12);

                                v1 = Unity.Burst.Intrinsics.X86.Sse.mul_ps(v1, v2);

                                result.x = v1.Float0 * v1.Float1 * v1.Float2 * v1.Float3;
                                code_pointer += c;
                                break;

                            case 13:
                                v1.Float0 = pop_f1(code_pointer + 1);
                                v1.Float1 = pop_f1(code_pointer + 2);
                                v1.Float2 = pop_f1(code_pointer + 3);
                                v1.Float3 = pop_f1(code_pointer + 4);
                                v2.Float0 = pop_f1(code_pointer + 5);
                                v2.Float1 = pop_f1(code_pointer + 6);
                                v2.Float2 = pop_f1(code_pointer + 7);
                                v2.Float3 = pop_f1(code_pointer + 8);

                                v1 = Unity.Burst.Intrinsics.X86.Sse.mul_ps(v1, v2);

                                v2.Float0 = pop_f1(code_pointer + 9);
                                v2.Float1 = pop_f1(code_pointer + 10);
                                v2.Float2 = pop_f1(code_pointer + 11);
                                v2.Float3 = pop_f1(code_pointer + 12);

                                v1 = Unity.Burst.Intrinsics.X86.Sse.mul_ps(v1, v2);

                                result.x = v1.Float0 * v1.Float1 * v1.Float2 * v1.Float3 * pop_f1(code_pointer + 13);
                                code_pointer += c;
                                break;

                            case 14:
                                v1.Float0 = pop_f1(code_pointer + 1);
                                v1.Float1 = pop_f1(code_pointer + 2);
                                v1.Float2 = pop_f1(code_pointer + 3);
                                v1.Float3 = pop_f1(code_pointer + 4);
                                v2.Float0 = pop_f1(code_pointer + 5);
                                v2.Float1 = pop_f1(code_pointer + 6);
                                v2.Float2 = pop_f1(code_pointer + 7);
                                v2.Float3 = pop_f1(code_pointer + 8);

                                v1 = Unity.Burst.Intrinsics.X86.Sse.mul_ps(v1, v2);

                                v2.Float0 = pop_f1(code_pointer + 9);
                                v2.Float1 = pop_f1(code_pointer + 10);
                                v2.Float2 = pop_f1(code_pointer + 11);
                                v2.Float3 = pop_f1(code_pointer + 12);

                                v1 = Unity.Burst.Intrinsics.X86.Sse.mul_ps(v1, v2);

                                v2.Float0 = pop_f1(code_pointer + 13);
                                v2.Float1 = pop_f1(code_pointer + 14);
                                v2.Float2 = v1.Float3; // save multiplication by v1.float3 later
                                v2.Float3 = 1;

                                v1 = Unity.Burst.Intrinsics.X86.Sse.mul_ps(v1, v2);

                                result.x = v1.Float0 * v1.Float1 * v1.Float2;
                                code_pointer += c;
                                break;

                            case 15:
                                v1.Float0 = pop_f1(code_pointer + 1);
                                v1.Float1 = pop_f1(code_pointer + 2);
                                v1.Float2 = pop_f1(code_pointer + 3);
                                v1.Float3 = pop_f1(code_pointer + 4);
                                v2.Float0 = pop_f1(code_pointer + 5);
                                v2.Float1 = pop_f1(code_pointer + 6);
                                v2.Float2 = pop_f1(code_pointer + 7);
                                v2.Float3 = pop_f1(code_pointer + 8);

                                v1 = Unity.Burst.Intrinsics.X86.Sse.mul_ps(v1, v2);

                                v2.Float0 = pop_f1(code_pointer + 9);
                                v2.Float1 = pop_f1(code_pointer + 10);
                                v2.Float2 = pop_f1(code_pointer + 11);
                                v2.Float3 = pop_f1(code_pointer + 12);

                                v1 = Unity.Burst.Intrinsics.X86.Sse.mul_ps(v1, v2);

                                v2.Float0 = pop_f1(code_pointer + 13);
                                v2.Float1 = pop_f1(code_pointer + 14);
                                v2.Float2 = pop_f1(code_pointer + 15);
                                v2.Float3 = 1;

                                v1 = Unity.Burst.Intrinsics.X86.Sse.mul_ps(v1, v2);

                                result.x = v1.Float0 * v1.Float1 * v1.Float2 * v1.Float3;
                                code_pointer += c;
                                break;

                            case 16:
                                v1.Float0 = pop_f1(code_pointer + 1);
                                v1.Float1 = pop_f1(code_pointer + 2);
                                v1.Float2 = pop_f1(code_pointer + 3);
                                v1.Float3 = pop_f1(code_pointer + 4);
                                v2.Float0 = pop_f1(code_pointer + 5);
                                v2.Float1 = pop_f1(code_pointer + 6);
                                v2.Float2 = pop_f1(code_pointer + 7);
                                v2.Float3 = pop_f1(code_pointer + 8);

                                v1 = Unity.Burst.Intrinsics.X86.Sse.mul_ps(v1, v2);

                                v2.Float0 = pop_f1(code_pointer + 9);
                                v2.Float1 = pop_f1(code_pointer + 10);
                                v2.Float2 = pop_f1(code_pointer + 11);
                                v2.Float3 = pop_f1(code_pointer + 12);

                                v1 = Unity.Burst.Intrinsics.X86.Sse.mul_ps(v1, v2);

                                v2.Float0 = pop_f1(code_pointer + 13);
                                v2.Float1 = pop_f1(code_pointer + 14);
                                v2.Float2 = pop_f1(code_pointer + 15);
                                v2.Float3 = pop_f1(code_pointer + 16);

                                v1 = Unity.Burst.Intrinsics.X86.Sse.mul_ps(v1, v2);

                                result.x = v1.Float0 * v1.Float1 * v1.Float2 * v1.Float3;
                                code_pointer += c;
                                break;

                            // any other size run manually as 1 float
                            default:
                                {
                                    code_pointer++;
                                    float r1 = pop_or_value(code_pointer);

                                    for (int i = 1; i < c; i++)
                                    {
                                        r1 *= pop_or_value(code_pointer + i);
                                    }

                                    result = new float4(r1, 0, 0, 0);
                                }
                                code_pointer += c - 1;
                                break;
                        }
                    }
                    break;

                case 2:
                    switch (c)
                    {
                        case 1: result.xy = pop_f2(++code_pointer); break;
                        case 2: result.xy = pop_f2(++code_pointer) * pop_f2(++code_pointer); break;
                        case 3: result.xy = pop_f2(++code_pointer) * pop_f2(++code_pointer) * pop_f2(++code_pointer); break;
                        case 4: result.xy = pop_f2(++code_pointer) * pop_f2(++code_pointer) * pop_f2(++code_pointer) * pop_f2(++code_pointer); break;
                        case 5: result.xy = pop_f2(++code_pointer) * pop_f2(++code_pointer) * pop_f2(++code_pointer) * pop_f2(++code_pointer) * pop_f2(++code_pointer); break;
                        case 6: result.xy = pop_f2(++code_pointer) * pop_f2(++code_pointer) * pop_f2(++code_pointer) * pop_f2(++code_pointer) * pop_f2(++code_pointer) * pop_f2(++code_pointer); break;
                        case 7: result.xy = pop_f2(++code_pointer) * pop_f2(++code_pointer) * pop_f2(++code_pointer) * pop_f2(++code_pointer) * pop_f2(++code_pointer) * pop_f2(++code_pointer) * pop_f2(++code_pointer); break;
                        case 8: result.xy = pop_f2(++code_pointer) * pop_f2(++code_pointer) * pop_f2(++code_pointer) * pop_f2(++code_pointer) * pop_f2(++code_pointer) * pop_f2(++code_pointer) * pop_f2(++code_pointer) * pop_f2(++code_pointer); break;
                        default:
                            {
                                // this probably is just as fast as an unrolled version because of the jumping in pop.. 
                                result.xy = pop_f2(code_pointer + 1);
                                for (int i = 2; 2 <= c; i++)
                                {
                                    result.xy = result.xy * pop_f2(code_pointer + i);
                                }
                            }
                            break;
                    }
                    break;

                case 3:
                    switch (c)
                    {
                        case 1: result.xyz = pop_f3(++code_pointer); break;
                        case 2: result.xyz = pop_f3(++code_pointer) * pop_f3(++code_pointer); break;
                        case 3: result.xyz = pop_f3(++code_pointer) * pop_f3(++code_pointer) * pop_f3(++code_pointer); break;
                        case 4: result.xyz = pop_f3(++code_pointer) * pop_f3(++code_pointer) * pop_f3(++code_pointer) * pop_f3(++code_pointer); break;
                        case 5: result.xyz = pop_f3(++code_pointer) * pop_f3(++code_pointer) * pop_f3(++code_pointer) * pop_f3(++code_pointer) * pop_f3(++code_pointer); break;
                        case 6: result.xyz = pop_f3(++code_pointer) * pop_f3(++code_pointer) * pop_f3(++code_pointer) * pop_f3(++code_pointer) * pop_f3(++code_pointer) * pop_f3(++code_pointer); break;
                        case 7: result.xyz = pop_f3(++code_pointer) * pop_f3(++code_pointer) * pop_f3(++code_pointer) * pop_f3(++code_pointer) * pop_f3(++code_pointer) * pop_f3(++code_pointer) * pop_f3(++code_pointer); break;
                        case 8: result.xyz = pop_f3(++code_pointer) * pop_f3(++code_pointer) * pop_f3(++code_pointer) * pop_f3(++code_pointer) * pop_f3(++code_pointer) * pop_f3(++code_pointer) * pop_f3(++code_pointer) * pop_f3(++code_pointer); break;
                        default:
                            {
                                // this probably is just as fast as an unrolled version because of the jumping in pop.. 
                                result.xyz = pop_f3(code_pointer + 1);
                                for (int i = 2; 2 <= c; i++)
                                {
                                    result.xyz = result.xyz * pop_f3(code_pointer + i);
                                }
                            }
                            break;
                    }
                    break;

                case 4:
                case 0: // actually 4
                    {
                        vector_size = 4;
                        switch (c)
                        {
                            case 1:
                                {
                                    result = pop_f4(++code_pointer);
                                }
                                break;

                            case 2:
                                {
                                    result = pop_f4(++code_pointer) * pop_f4(++code_pointer);
                                }
                                break;

                            case 3:
                                {
                                    result = pop_f4(++code_pointer) *
                                        pop_f4(++code_pointer) *
                                        pop_f4(++code_pointer);
                                }
                                break;

                            case 4:
                                {
                                    result = pop_f4(++code_pointer) *
                                        pop_f4(++code_pointer) *
                                        pop_f4(++code_pointer) *
                                        pop_f4(++code_pointer);
                                }
                                break;

                            case 5:
                                {
                                    result = pop_f4(++code_pointer) *
                                        pop_f4(++code_pointer) *
                                        pop_f4(++code_pointer) *
                                        pop_f4(++code_pointer) *
                                        pop_f4(++code_pointer);
                                }
                                break;

                            case 6:
                                {
                                    result = pop_f4(++code_pointer) *
                                        pop_f4(++code_pointer) *
                                        pop_f4(++code_pointer) *
                                        pop_f4(++code_pointer) *
                                        pop_f4(++code_pointer) *
                                        pop_f4(++code_pointer);
                                }
                                break;

                            case 7:
                                {
                                    result = pop_f4(++code_pointer) *
                                        pop_f4(++code_pointer) *
                                        pop_f4(++code_pointer) *
                                        pop_f4(++code_pointer) *
                                        pop_f4(++code_pointer) *
                                        pop_f4(++code_pointer) *
                                        pop_f4(++code_pointer);
                                }
                                break;

                            case 8:
                                {
                                    result = pop_f4(++code_pointer) *
                                        pop_f4(++code_pointer) *
                                        pop_f4(++code_pointer) *
                                        pop_f4(++code_pointer) *
                                        pop_f4(++code_pointer) *
                                        pop_f4(++code_pointer) *
                                        pop_f4(++code_pointer) *
                                        pop_f4(++code_pointer);
                                }
                                break;

                            default:
                                {
                                    // this probably is just as fast as an unrolled version because of the jumping in pop.. 
                                    result = pop_f4(code_pointer + 1);
                                    for (int i = 2; 2 <= c; i++)
                                    {
                                        result = result * pop_f4(code_pointer + i);
                                    }
                                }
                                break;
                        }
                    }
                    vector_size = 4;
                    break;

                default:
                    {
#if DEVELOPMENT_BUILD || TRACE
                        Debug.LogError($"mula: vector size '{vector_size}' not supported ");
#endif
                    }
                    break;
            }
        }

        /// <summary>
        /// add all inputs together 
        /// - n elements of equal vector size 
        /// </summary>
        void get_adda_result(ref int code_pointer, ref byte vector_size, out float4 result)
        {
            code_pointer = code_pointer + 1;
            byte c = code[code_pointer];

            // decode 62
            c = BlastInterpretor.decode62(in c, ref vector_size);

            switch ((BlastVectorSizes)vector_size)
            {
                case BlastVectorSizes.float1:
                    {
                        float f = pop_f1(code_pointer + 1);
                        for (int i = 2; i <= c; i++)
                        {
                            f += pop_f1(code_pointer + i);
                        }
                        code_pointer += c;
                        result = f;
                    }
                    break;

                case BlastVectorSizes.float2:
                    {
                        float2 f2 = pop_f2(code_pointer + 1);
                        for (int i = 2; i <= c; i++)
                        {
                            f2 += pop_f2(code_pointer + i);
                        }
                        code_pointer += c;
                        result = new float4(f2, 0, 0);
                    }
                    break;

                case BlastVectorSizes.float3:
                    {
                        float3 f3 = pop_f3(code_pointer + 1);
                        for (int i = 2; i <= c; i++)
                        {
                            f3 += pop_f3(code_pointer + i);
                        }
                        code_pointer += c;
                        result = new float4(f3, 0);
                    }
                    break;

                case 0:
                case BlastVectorSizes.float4:
                    {
                        result = pop_f4(code_pointer + 1);
                        for (int i = 2; i <= c; i++)
                        {
                            result += pop_f4(code_pointer + i);
                        }
                        code_pointer += c;
                        vector_size = 4;
                    }
                    break;

                default:
                    {
#if DEVELOPMENT_BUILD || TRACE
                        Debug.LogError($"get_adda_result: vector size '{vector_size}' not allowed");
#endif
                        result = float.NaN;

                    }
                    break;

            }
        }

        void get_suba_result(ref int code_pointer, ref byte vector_size, out float4 f4)
        {
            code_pointer = code_pointer + 1;
            byte c = code[code_pointer];

#if DECODE_44
            c = BlastInterpretor.decode44in c, ref vector_size);
#else
            c = BlastInterpretor.decode62(in c, ref vector_size);
#endif

            switch ((BlastVectorSizes)vector_size)
            {
                case BlastVectorSizes.float1:
                    {
                        float f = pop_f1(code_pointer + 1);
                        for (int i = 2; i <= c; i++)
                        {
                            f -= pop_f1(code_pointer + i);
                        }
                        code_pointer += c;
                        f4 = f;
                        break;
                    }

                case BlastVectorSizes.float2:
                    {
                        float2 f2 = pop_f2(code_pointer + 1);
                        for (int i = 2; i <= c; i++)
                        {
                            f2 -= pop_f2(code_pointer + i);
                        }
                        code_pointer += c;
                        f4 = new float4(f2, 0, 0);
                        break;
                    }

                case BlastVectorSizes.float3:
                    {
                        float3 f3 = pop_f3(code_pointer + 1);
                        for (int i = 2; i <= c; i++)
                        {
                            f3 -= pop_f3(code_pointer + i);
                        }
                        code_pointer += c;
                        f4 = new float4(f3, 0);
                        break;
                    }

                case 0:
                case BlastVectorSizes.float4:
                    {
                        f4 = pop_f4(code_pointer + 1);
                        for (int i = 2; i <= c; i++)
                        {
                            f4 -= pop_f4(code_pointer + i);
                        }
                        code_pointer += c;
                        vector_size = 4;
                        break;
                    }

                default:
                    {
#if DEVELOPMENT_BUILD || TRACE
                        Debug.LogError($"get_suba_result: vector size '{vector_size}' not allowed");
#endif
                        f4 = float.NaN;
                        break;
                    }
            }
        }

        void get_diva_result(ref int code_pointer, ref byte vector_size, out float4 f4)
        {
            code_pointer = code_pointer + 1;
            byte c = code[code_pointer];

#if DECODE_44
            c = BlastInterpretor.decode44(in c, ref vector_size);
#else
            c = BlastInterpretor.decode62(in c, ref vector_size);
#endif

            switch ((BlastVectorSizes)vector_size)
            {
                case BlastVectorSizes.float1:
                    {
                        float f = pop_f1(code_pointer + 1);
                        for (int i = 2; i <= c; i++)
                        {
                            f /= pop_f1(code_pointer + i);
                        }
                        code_pointer += c;
                        f4 = f;
                        break;
                    }

                case BlastVectorSizes.float2:
                    {
                        float2 f2 = pop_f2(code_pointer + 1);
                        for (int i = 2; i <= c; i++)
                        {
                            f2 /= pop_f2(code_pointer + i);
                        }
                        code_pointer += c;
                        f4 = new float4(f2, 0, 0);
                        break;
                    }

                case BlastVectorSizes.float3:
                    {
                        float3 f3 = pop_f3(code_pointer + 1);
                        for (int i = 2; i <= c; i++)
                        {
                            f3 /= pop_f3(code_pointer + i);
                        }
                        code_pointer += c;
                        f4 = new float4(f3, 0);
                        break;
                    }


                case 0:
                case BlastVectorSizes.float4:
                    {
                        f4 = pop_f4(code_pointer + 1);
                        for (int i = 2; i <= c; i++)
                        {
                            f4 /= pop_f4(code_pointer + i);
                        }
                        code_pointer += c;
                        vector_size = 4;
                        break;
                    }

                default:
                    {
#if DEVELOPMENT_BUILD || TRACE
                        Debug.LogError($"get_diva_result: vector size '{vector_size}' not allowed");
#endif
                        f4 = float.NaN;
                        break;
                    }
            }
        }

        /// <summary>
        /// a = any(a b c d), vectorsizes must all be equal, output vectorsize == input vectorsize
        /// </summary>
        void get_any_result(ref int code_pointer, ref byte vector_size, out float4 f)
        {
            code_pointer++;
            byte c = code[code_pointer];

            //  no way for bytecode optimizor to do this.. 
#if DECODE_44
            c = BlastInterpretor.decode44(in c, ref vector_size);
#else
            c = BlastInterpretor.decode62(in c, ref vector_size);
#endif

            // this will compile into a jump table (CHECK THIS !!) todo
            switch ((BlastVectorSizes)vector_size)
            {
                case BlastVectorSizes.float1:
                    {
                        switch (c)
                        {
                            default:
#if DEVELOPMENT_BUILD || TRACE
                                Debug.LogError($"blast.interpretor.get_any_result: parameter count '{c}' not supported -> parser/compiler/optimizer error");
#endif
                                f = float.NaN;
                                break;

                            case 2:
                                f = math.select(0f, 1f, pop_f1(code_pointer + 1) != 0 || pop_f1(code_pointer + 2) != 0);
                                code_pointer += 2;
                                break;

                            case 3:
                                f = math.select(0f, 1f, pop_f1(code_pointer + 1) != 0 || pop_f1(code_pointer + 2) != 0 || pop_f1(code_pointer + 3) != 0);
                                code_pointer += 3;
                                break;

                            case 4:
                                f = math.select(0f, 1f, pop_f1(code_pointer + 1) != 0 || pop_f1(code_pointer + 2) != 0 || pop_or_value(code_pointer + 3) != 0 || pop_or_value(code_pointer + 4) != 0);
                                code_pointer += 4;
                                break;

                            case 5:
                                f = math.select(0f, 1f, pop_f1(code_pointer + 1) != 0 || pop_f1(code_pointer + 2) != 0 || pop_f1(code_pointer + 3) != 0 || pop_f1(code_pointer + 4) != 0 || pop_f1(code_pointer + 5) != 0);
                                code_pointer += 5;
                                break;

                            case 6:
                                f = math.select(0f, 1f, pop_f1(code_pointer + 1) != 0 || pop_f1(code_pointer + 2) != 0 || pop_f1(code_pointer + 3) != 0 || pop_f1(code_pointer + 4) != 0 || pop_f1(code_pointer + 5) != 0 || pop_f1(code_pointer + 6) != 0);
                                code_pointer += 6;
                                break;

                            case 7:
                                f = math.select(0f, 1f, pop_f1(code_pointer + 1) != 0 || pop_f1(code_pointer + 2) != 0 || pop_f1(code_pointer + 3) != 0 || pop_f1(code_pointer + 4) != 0 || pop_f1(code_pointer + 5) != 0 || pop_f1(code_pointer + 6) != 0 || pop_f1(code_pointer + 7) != 0);
                                code_pointer += 7;
                                break;

                            case 8:
                                f = math.select(0f, 1f, pop_f1(code_pointer + 1) != 0 || pop_f1(code_pointer + 2) != 0 || pop_f1(code_pointer + 3) != 0 || pop_f1(code_pointer + 4) != 0 || pop_f1(code_pointer + 5) != 0 || pop_f1(code_pointer + 6) != 0 || pop_f1(code_pointer + 7) != 0 || pop_f1(code_pointer + 8) != 0);
                                code_pointer += 8;
                                break;
                        }
                        break;
                    }

                case BlastVectorSizes.float2:
                    {
                        switch (c)
                        {
                            default:
#if DEVELOPMENT_BUILD || TRACE
                                Debug.LogError($"blast.interpretor.get_any_result: parameter count '{c}' not supported -> parser/compiler/optimizer error");
#endif
                                f = float.NaN;
                                break;

                            case 2:
                                f = math.select(0f, 1f, math.any(pop_f2(code_pointer + 1)) || math.any(pop_f2(code_pointer + 2)));
                                code_pointer += 2;
                                break;

                            case 3:
                                f = math.select(0f, 1f, math.any(pop_f2(code_pointer + 1)) || math.any(pop_f2(code_pointer + 2)) || math.any(pop_f2(code_pointer + 3)));
                                code_pointer += 3;
                                break;

                            case 4:
                                f = math.select(0f, 1f, math.any(pop_f2(code_pointer + 1)) || math.any(pop_f2(code_pointer + 2)) || math.any(pop_f2(code_pointer + 3)) || math.any(pop_f2(code_pointer + 4)));
                                code_pointer += 4;
                                break;

                            case 5:
                                f = math.select(0f, 1f, math.any(pop_f2(code_pointer + 1)) || math.any(pop_f2(code_pointer + 2)) || math.any(pop_f2(code_pointer + 3)) || math.any(pop_f2(code_pointer + 4)) || math.any(pop_f2(code_pointer + 5)));
                                code_pointer += 5;
                                break;

                            case 6:
                                f = math.select(0f, 1f, math.any(pop_f2(code_pointer + 1)) || math.any(pop_f2(code_pointer + 2)) || math.any(pop_f2(code_pointer + 3)) || math.any(pop_f2(code_pointer + 4)) || math.any(pop_f2(code_pointer + 5)) || math.any(pop_f2(code_pointer + 6)));
                                code_pointer += 6;
                                break;

                            case 7:
                                f = math.select(0f, 1f, math.any(pop_f2(code_pointer + 1)) || math.any(pop_f2(code_pointer + 2)) || math.any(pop_f2(code_pointer + 3)) || math.any(pop_f2(code_pointer + 4)) || math.any(pop_f2(code_pointer + 5)) || math.any(pop_f2(code_pointer + 6)) || math.any(pop_f2(code_pointer + 7)));
                                code_pointer += 7;
                                break;

                            case 8:
                                f = math.select(0f, 1f, math.any(pop_f2(code_pointer + 1)) || math.any(pop_f2(code_pointer + 2)) || math.any(pop_f2(code_pointer + 3)) || math.any(pop_f2(code_pointer + 4)) || math.any(pop_f2(code_pointer + 5)) || math.any(pop_f2(code_pointer + 6)) || math.any(pop_f2(code_pointer + 7)) || math.any(pop_f2(code_pointer + 8)));
                                code_pointer += 8;
                                break;
                        }
                        break;
                    }

                case BlastVectorSizes.float3:
                    {
                        switch (c)
                        {
                            default:
#if DEVELOPMENT_BUILD || TRACE
                                Debug.LogError($"blast.interpretor.get_any_result: parameter count '{c}' not supported -> parser/compiler/optimizer error");
#endif
                                f = float.NaN;
                                break;

                            case 2:
                                f = math.select(0f, 1f, math.any(pop_f3(code_pointer + 1)) || math.any(pop_f3(code_pointer + 2)));
                                code_pointer += 2;
                                break;

                            case 3:
                                f = math.select(0f, 1f, math.any(pop_f3(code_pointer + 1)) || math.any(pop_f3(code_pointer + 2)) || math.any(pop_f3(code_pointer + 3)));
                                code_pointer += 3;
                                break;

                            case 4:
                                f = math.select(0f, 1f, math.any(pop_f3(code_pointer + 1)) || math.any(pop_f3(code_pointer + 2)) || math.any(pop_f3(code_pointer + 3)) || math.any(pop_f3(code_pointer + 4)));
                                code_pointer += 4;
                                break;

                            case 5:
                                f = math.select(0f, 1f, math.any(pop_f3(code_pointer + 1)) || math.any(pop_f3(code_pointer + 2)) || math.any(pop_f3(code_pointer + 3)) || math.any(pop_f3(code_pointer + 4)) || math.any(pop_f3(code_pointer + 5)));
                                code_pointer += 5;
                                break;

                            case 6:
                                f = math.select(0f, 1f, math.any(pop_f3(code_pointer + 1)) || math.any(pop_f3(code_pointer + 2)) || math.any(pop_f3(code_pointer + 3)) || math.any(pop_f3(code_pointer + 4)) || math.any(pop_f3(code_pointer + 5)) || math.any(pop_f3(code_pointer + 6)));
                                code_pointer += 6;
                                break;

                            case 7:
                                f = math.select(0f, 1f, math.any(pop_f3(code_pointer + 1)) || math.any(pop_f3(code_pointer + 2)) || math.any(pop_f3(code_pointer + 3)) || math.any(pop_f3(code_pointer + 4)) || math.any(pop_f3(code_pointer + 5)) || math.any(pop_f3(code_pointer + 6)) || math.any(pop_f3(code_pointer + 7)));
                                code_pointer += 7;
                                break;

                            case 8:
                                f = math.select(0f, 1f, math.any(pop_f3(code_pointer + 1)) || math.any(pop_f3(code_pointer + 2)) || math.any(pop_f3(code_pointer + 3)) || math.any(pop_f3(code_pointer + 4)) || math.any(pop_f3(code_pointer + 5)) || math.any(pop_f3(code_pointer + 6)) || math.any(pop_f3(code_pointer + 7)) || math.any(pop_f3(code_pointer + 8)));
                                code_pointer += 8;
                                break;
                        }
                        break;
                    }

                case 0:
                case BlastVectorSizes.float4:
                    {
                        switch (c)
                        {
                            default:
#if DEVELOPMENT_BUILD || TRACE
                                Debug.LogError($"blast.interpretor.get_any_result: parameter count '{c}' not supported -> parser/compiler/optimizer error");
#endif
                                f = float.NaN;
                                break;

                            case 2:
                                f = math.select(0f, 1f, math.any(pop_f4(code_pointer + 1)) || math.any(pop_f4(code_pointer + 2)));
                                code_pointer += 2;
                                break;

                            case 3:
                                f = math.select(0f, 1f, math.any(pop_f4(code_pointer + 1)) || math.any(pop_f4(code_pointer + 2)) || math.any(pop_f4(code_pointer + 3)));
                                code_pointer += 3;
                                break;

                            case 4:
                                f = math.select(0f, 1f, math.any(pop_f4(code_pointer + 1)) || math.any(pop_f4(code_pointer + 2)) || math.any(pop_f4(code_pointer + 3)) || math.any(pop_f4(code_pointer + 4)));
                                code_pointer += 4;
                                break;

                            case 5:
                                f = math.select(0f, 1f, math.any(pop_f4(code_pointer + 1)) || math.any(pop_f4(code_pointer + 2)) || math.any(pop_f4(code_pointer + 3)) || math.any(pop_f4(code_pointer + 4)) || math.any(pop_f4(code_pointer + 5)));
                                code_pointer += 5;
                                break;

                            case 6:
                                f = math.select(0f, 1f, math.any(pop_f4(code_pointer + 1)) || math.any(pop_f4(code_pointer + 2)) || math.any(pop_f4(code_pointer + 3)) || math.any(pop_f4(code_pointer + 4)) || math.any(pop_f4(code_pointer + 5)) || math.any(pop_f4(code_pointer + 6)));
                                code_pointer += 6;
                                break;

                            case 7:
                                f = math.select(0f, 1f, math.any(pop_f4(code_pointer + 1)) || math.any(pop_f4(code_pointer + 2)) || math.any(pop_f4(code_pointer + 3)) || math.any(pop_f4(code_pointer + 4)) || math.any(pop_f4(code_pointer + 5)) || math.any(pop_f4(code_pointer + 6)) || math.any(pop_f4(code_pointer + 7)));
                                code_pointer += 7;
                                break;

                            case 8:
                                f = math.select(0f, 1f, math.any(pop_f4(code_pointer + 1)) || math.any(pop_f4(code_pointer + 2)) || math.any(pop_f4(code_pointer + 3)) || math.any(pop_f4(code_pointer + 4)) || math.any(pop_f4(code_pointer + 5)) || math.any(pop_f4(code_pointer + 6)) || math.any(pop_f4(code_pointer + 7)) || math.any(pop_f4(code_pointer + 8)));
                                code_pointer += 8;
                                break;
                        }
                        vector_size = 4;
                        break;
                    }

                default:
                    {
#if DEVELOPMENT_BUILD || TRACE
                        Debug.LogError($"blast.interpretor.get_any_result: vector size '{vector_size}' not (yet) supported");
#endif
                        f = float.NaN;
                        break;
                    }
            }
        }

        /// <summary>
        /// check if all operands are true ==  != 0,  works vector component wise and thus returns the same vectorsize as input
        /// </summary>
        void get_all_result(ref int code_pointer, ref byte vector_size, out float4 f)
        {
            f = 0f;
            code_pointer++;
            byte c = code[code_pointer];

#if DECODE_44
            c = BlastInterpretor.decode44(in c, ref vector_size);
#else
            c = BlastInterpretor.decode62(in c, ref vector_size);
#endif

#if DEVELOPMENT_BUILD || TRACE
            if (c < 1)
            {
                Debug.LogError($"get_any_result: parameter count '{c}' not supported");
                f = float.NaN;
                return;
            }
#endif

            switch ((BlastVectorSizes)vector_size)
            {
                default:
                    {
#if DEVELOPMENT_BUILD || TRACE
                        Debug.LogError($"get_any_result: vector size '{vector_size}' not (yet) supported");
#endif
                        f = float.NaN;
                        break;
                    }

                case BlastVectorSizes.float1:
                    {
                        f.x = pop_f1(code_pointer + 1) == 0 ? 0f : 1f;
                        //
                        //    CANNOT STOP EARLY DUE TO STACK OPERATIONS  
                        //    TODO   we could only process stack-offset changes in one is zero, doubt if there is much benefit
                        //                        
                        for (int i = 2; i <= c /*&& x != 0*/; i++)
                        {
                            f.x = math.select(0f, 1f, pop_f1(code_pointer + i) != 0 && f.x != 0);
                        }
                        break;
                    }

                case BlastVectorSizes.float2:
                    {
                        bool2 b2 = pop_f2(code_pointer + 1) != 0; 
                       
                        for (int i = 2; i <= c; i++)
                        {
                            bool2 b22 = pop_f2(code_pointer + 1) != 0;
                            b2.x = b2.x && b22.x;
                            b2.y = b2.y && b22.y;
                        }

                        f.x = math.select(0f, 1f, b2.x);
                        f.y = math.select(0f, 1f, b2.y);
                        break;
                    }

                case BlastVectorSizes.float3:
                    {
                        bool3 b3 = pop_f3(code_pointer + 1) != 0;

                        for (int i = 2; i <= c; i++)
                        {
                            bool3 b32 = pop_f3(code_pointer + 1) != 0;
                            b3.x = b3.x && b32.x;
                            b3.y = b3.y && b32.y;
                            b3.z = b3.z && b32.z;
                        }

                        f.x = math.select(0f, 1f, b3.x);
                        f.y = math.select(0f, 1f, b3.y);
                        f.z = math.select(0f, 1f, b3.z);
                        break;
                    }

                case 0:
                case BlastVectorSizes.float4:
                    {
                        bool4 b4 = pop_f4(code_pointer + 1) != 0;

                        for (int i = 2; i <= c; i++)
                        {
                            bool4 b42 = pop_f4(code_pointer + 1) != 0;
                            b4.x = b4.x && b42.x;
                            b4.y = b4.y && b42.y;
                            b4.z = b4.z && b42.z;
                            b4.w = b4.w && b42.w;
                        }

                        f.x = math.select(0f, 1f, b4.x);
                        f.y = math.select(0f, 1f, b4.y);
                        f.z = math.select(0f, 1f, b4.z);
                        f.w = math.select(0f, 1f, b4.w); 
                        vector_size = 4;
                        break;
                    }
            }
            code_pointer += c;
        }


        #endregion

        #region random

        /// <summary>
        /// 
        /// </summary>
        void get_random_result(ref int code_pointer, ref byte vector_size, out float4 f4)
        {
            // get vector size from next byte  
            code_pointer++;
            byte c = code[code_pointer];

#if DECODE_44
            c = BlastInterpretor.decode44in c, ref vector_size);
#else
            c = BlastInterpretor.decode62(in c, ref vector_size);
#endif

            // for random, c specifies range:
            // 0 = 0..1  == default
            // 2 =  2 ops with values after this op    min..max 
            // 1 = 1 ops   0...max 

            switch ((BlastVectorSizes)vector_size)
            {
                case BlastVectorSizes.float1:
                    switch (c)
                    {
                        default:
                            {
#if DEVELOPMENT_BUILD || TRACE
                                Debug.LogError($"random: range type '{c}' not supported ");
#endif
                                f4 = float.NaN;
                                break;
                            }

                        case 0: f4 = engine_ptr->random.NextFloat(); break;
                        case 1: f4 = engine_ptr->random.NextFloat(pop_f1(code_pointer + 1)); code_pointer++; break;
                        case 2: f4 = engine_ptr->random.NextFloat(pop_f1(code_pointer + 1), pop_f1(code_pointer + 2)); code_pointer += 2; break;
                    }
                    vector_size = 1;
                    break;

                case BlastVectorSizes.float2:
                    switch (c)
                    {
                        default:
                            {
#if DEVELOPMENT_BUILD || TRACE
                                Debug.LogError($"random: range type '{c}' not supported ");
#endif
                                f4 = float.NaN;
                                break;
                            }

                        case 0: f4 = new float4(engine_ptr->random.NextFloat2(), 0, 0); break;
                        case 1: f4 = new float4(engine_ptr->random.NextFloat2(pop_f2(code_pointer + 1)), 0, 0); code_pointer++; break;
                        case 2: f4 = new float4(engine_ptr->random.NextFloat2(pop_f2(code_pointer + 1), pop_f2(code_pointer + 2)), 0, 0); code_pointer += 2; break;
                    }
                    vector_size = 2;
                    break;


                case BlastVectorSizes.float3:
                    switch (c)
                    {
                        default:
                            {
#if DEVELOPMENT_BUILD || TRACE
                                Debug.LogError($"random: range type '{c}' not supported ");
#endif
                                f4 = float.NaN;
                                break;
                            }

                        case 0: f4 = new float4(engine_ptr->random.NextFloat3(), 0); break;
                        case 1: f4 = new float4(engine_ptr->random.NextFloat3(pop_f3(code_pointer + 1)), 0); code_pointer++; break;
                        case 2: f4 = new float4(engine_ptr->random.NextFloat3(pop_f3(code_pointer + 1), pop_f3(code_pointer + 2)), 0); code_pointer += 2; break;
                    }
                    vector_size = 3;
                    break;

                case BlastVectorSizes.float4:
                case 0:
                    switch (c)
                    {
                        default:
                            {
#if DEVELOPMENT_BUILD || TRACE
                                Debug.LogError($"random: range type '{c}' not supported ");
#endif
                                f4 = float.NaN;
                                break;
                            }

                        case 0: f4 = engine_ptr->random.NextFloat4(); break;
                        case 1: f4 = engine_ptr->random.NextFloat4(pop_f4(code_pointer + 1)); code_pointer++; break;
                        case 2: f4 = engine_ptr->random.NextFloat4(pop_f4(code_pointer + 1), pop_f4(code_pointer + 2)); code_pointer += 2; break;
                    }
                    vector_size = 4;
                    break;

                default:
                    {
#if DEVELOPMENT_BUILD || TRACE
                        Debug.LogError($"random: vector size '{vector_size}' not supported ");
#endif
                        f4 = float.NaN;
                        break;
                    }
            }
        }


        #endregion

        #region indexers 

        void get_index_result(ref int code_pointer, ref byte vector_size, out float4 f4, in byte offset)
        {
            code_pointer += 1;
            f4 = float.NaN;

            BlastVariableDataType datatype;
            float* fdata = (float*)pop_with_info(in code_pointer, out datatype, out vector_size);

#if DEVELOPMENT_BUILD || TRACE
            if (datatype == BlastVariableDataType.ID)
            {
                Debug.LogError($"ID datatype in index[{offset}] not yet implemented, codepointer: {code_pointer} = > {code[code_pointer]}");
                return;
            }
            if (math.select(vector_size, 4, vector_size == 0) < offset + 1)
            {
                Debug.LogError($"index[{offset}] Invalid indexing component {offset} for vectorsize {vector_size}, codepointer: {code_pointer} = > {code[code_pointer]}");
                return;
            }
#endif

            switch ((BlastVectorSizes)vector_size)
            {
                // result is always the same regardless.. could raise errors on this condition as compiler could do better then wast precious bytes
                case BlastVectorSizes.float1: f4.x = ((float*)fdata)[0]; vector_size = 1; break;
                case BlastVectorSizes.float2: f4.x = ((float*)fdata)[offset]; vector_size = 1; break;
                case BlastVectorSizes.float3: f4.x = ((float*)fdata)[offset]; vector_size = 1; break;
                case 0:
                case BlastVectorSizes.float4: f4.x = ((float*)fdata)[offset]; vector_size = 1; break;
#if DEVELOPMENT_BUILD || TRACE
                default:
                    {
                        Debug.LogError($"Index[{offset}]: NUMERIC({vector_size}) vectorsize '{vector_size}' not supported");
                        break;
                    }
#endif
            }

        }

        #endregion 
        #endregion
        #endregion

        #region Push[cfv]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void pushc(ref int code_pointer, ref byte vector_size, ref float4 f4_register)
        {
            get_sequence_result(ref code_pointer, ref vector_size, out f4_register);
            switch (vector_size)
            {
                case 1: push(f4_register.x); break;
                case 2: push(f4_register.xy); break;
                case 3: push(f4_register.xyz); break;
                case 4: push(f4_register.xyzw); break;
#if DEVELOPMENT_BUILD || TRACE
                default:
                    Debug.LogError("blastscript.interpretor.pushc error: variable vector size not yet supported on stack push");
                    break;
#endif
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void push(ref int code_pointer, ref byte vector_size, ref float4 f4_register)
        {
            if (code[code_pointer] == (byte)blast_operation.begin)
            {
                code_pointer++;

                // push the vector result of a compound 
                get_sequence_result(ref code_pointer, ref vector_size, out f4_register);
                switch (vector_size)
                {
                    case 1:
                        push(f4_register.x);
                        break;
                    case 2:
                        push(f4_register.xy);
                        break;
                    case 3:
                        push(f4_register.xyz);
                        break;
                    case 4:
                        push(f4_register);
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
                // else push 1 float of data
                float* fdata = (float*)data;
                push(fdata[code[code_pointer++] - opt_id]);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void pushv(ref int code_pointer, ref byte vector_size, ref float4 f4_register)
        {
            byte param_count = code[code_pointer++];

            param_count = decode44(param_count, ref vector_size);

            if (param_count == 1 && vector_size != param_count)
            {
                // push vector sourced from vector parameter (this is a wierd thing to do but ok.. i have a test with it so it should work)
                switch (vector_size)
                {
                    case 1: push(pop_f1(code_pointer)); break;
                    case 2: push(pop_f2(code_pointer)); break;
                    case 3: push(pop_f3(code_pointer)); break;
                    case 4:
                    case 0: push(pop_f4(code_pointer)); break;
#if DEVELOPMENT_BUILD || TRACE
                    default:
                        Debug.LogError($"blast.interpretor.pushv: unsupported vectorsize {vector_size} in parameter at codepointer {code_pointer}");
                        break;
#endif
                }
                code_pointer += 1;
            }
            else
            {
                if (param_count == 1)
                {
                    switch (vector_size)
                    {
                        case 1:
                            push(pop_f1(code_pointer));
                            code_pointer++;
                            break;
                        case 2:
                            push(pop_f2(code_pointer));
                            code_pointer++;
                            break;
                        case 3:
                            push(pop_f3(code_pointer));
                            code_pointer++;
                            break;
                        case 4:
                            push(pop_f4(code_pointer));
                            code_pointer++;
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
                            push(pop_f1(code_pointer));
                            code_pointer += 1;
                            break;
                        case 2:
                            push(new float2(pop_f1(code_pointer), pop_f1(code_pointer + 1)));
                            code_pointer += 2;
                            break;
                        case 3:
                            push(new float3(pop_f1(code_pointer), pop_f1(code_pointer + 1), pop_f1(code_pointer + 2)));
                            code_pointer += 3;
                            break;
                        case 4:
                            push(new float4(pop_f1(code_pointer), pop_f1(code_pointer + 1), pop_f1(code_pointer + 2), pop_f1(code_pointer + 3)));
                            code_pointer += 4;
                            break;
                    }
                    // we could handle the case of different sized vectors, pushing them into 1 of vectorsize...
                    // - currently the compiler wont do this and we catch this as an error in debug builds some lines back
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void pushf(ref int code_pointer, ref byte vector_size, ref float4 f4_register)
        {
            get_function_result(ref code_pointer, ref vector_size, out f4_register);
            code_pointer++;

            switch (vector_size)
            {
                case 1: push(f4_register.x); break;
                case 2: push(f4_register.xy); break;
                case 3: push(f4_register.xyz); break;
                case 4: push(f4_register.xyzw); break;
#if DEVELOPMENT_BUILD || TRACE
                default:
                    Debug.LogError("blast.interpretor pushf error: variable vector size not yet supported on stack push");
                    //  return (int)BlastError.error_variable_vector_op_not_supported;
                    break;
#endif
            }
        }

        #endregion

        #region AssignV Vector 


        /// <summary>
        /// pop 1 value from input stream, checks if its the minus sign, if so reads next value and negates it, otherwise returns current value 
        /// </summary>
        /// <param name="code_pointer"></param>
        /// <param name="cp"></param>
        /// <returns></returns>
        float pop_f1_with_minus(int code_pointer, ref int cp)
        {
            if (code[code_pointer] == (byte)blast_operation.substract)
            {
                cp++;
                return -pop_f1(code_pointer + 1);

            }
            else
            {
                return pop_f1(code_pointer);
            }
        }


        /// <summary>
        /// builds a [n] vector from [1] values 
        /// </summary>
        /// <param name="code_pointer"></param>
        /// <param name="vector_size"></param>
        /// <param name="assignee"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void assignv(ref int code_pointer, in byte vector_size, [NoAlias]in float* assignee)
        {
            // param_count == vector_size and vector_size > 1, compiler enforces it so we should not check it in release 
            // this means that vector is build from multiple data points
            int cp = 0;
            switch (vector_size)
            {
                case 1:
                    assignee[0] = pop_f1_with_minus(code_pointer, ref cp);
                    code_pointer += 1 + cp;
                    break;
                case 2:
                    assignee[0] = pop_f1_with_minus(code_pointer, ref cp);
                    assignee[1] = pop_f1_with_minus(code_pointer + 1 + cp, ref cp);
                    code_pointer += 2 + cp;
                    break;
                case 3:
                    assignee[0] = pop_f1_with_minus(code_pointer, ref cp);
                    assignee[1] = pop_f1_with_minus(code_pointer + 1 + cp, ref cp);
                    assignee[2] = pop_f1_with_minus(code_pointer + 2 + cp, ref cp);
                    code_pointer += 3 + cp;
                    break;
                case 4:
                    assignee[0] = pop_f1_with_minus(code_pointer, ref cp);
                    assignee[1] = pop_f1_with_minus(code_pointer + 1 + cp, ref cp);
                    assignee[2] = pop_f1_with_minus(code_pointer + 2 + cp, ref cp);
                    assignee[3] = pop_f1_with_minus(code_pointer + 3 + cp, ref cp);
                    code_pointer += 4 + cp;
                    break;

                default:
#if DEVELOPMENT_BUILD || TRACE
                    Debug.LogError($"blast.interpretor.assignv error: vector size {vector_size} not supported");
#endif
                    break;
            }
        }


        #endregion 

        #region ByteCode Execution

        /// <summary>
        /// get the result of a function encoded in the byte code, support all fuctions in op, exop and external calls 
        /// although external calls should be directly called when possible
        /// </summary>
        void get_function_result(ref int code_pointer, ref byte vector_size, out float4 f4_result)
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
                case blast_operation.abs: get_abs_result(ref code_pointer, ref vector_size, out f4_result); break;
                case blast_operation.maxa: get_maxa_result(ref code_pointer, ref vector_size, out f4_result); break;
                case blast_operation.mina: get_mina_result(ref code_pointer, ref vector_size, out f4_result); break;
                case blast_operation.max: get_max_result(ref code_pointer, ref vector_size, out f4_result); break;
                case blast_operation.min: get_min_result(ref code_pointer, ref vector_size, out f4_result); break;
                case blast_operation.csum: get_csum_result(ref code_pointer, ref vector_size, out f4_result); break;
                case blast_operation.trunc: get_trunc_result(ref code_pointer, ref vector_size, out f4_result); break;

                // math utils 
                case blast_operation.select: get_select_result(ref code_pointer, ref vector_size, out f4_result); break;
                case blast_operation.fma: get_fma_result(ref code_pointer, ref vector_size, out f4_result); break;
                case blast_operation.fmod: get_fmod_result(ref code_pointer, ref vector_size, out f4_result); break;

                // mul|a family
                case blast_operation.mula: get_mula_result(ref code_pointer, ref vector_size, out f4_result); break;
                case blast_operation.adda: get_adda_result(ref code_pointer, ref vector_size, out f4_result); break;
                case blast_operation.suba: get_suba_result(ref code_pointer, ref vector_size, out f4_result); break;
                case blast_operation.diva: get_diva_result(ref code_pointer, ref vector_size, out f4_result); break;

                // random|seed
                case blast_operation.random: get_random_result(ref code_pointer, ref vector_size, out f4_result); break;
                case blast_operation.seed:
                    f4_result = pop_f1(++code_pointer);
                    engine_ptr->Seed(math.asuint(f4_result.x));
                    break;

                // and|all
                case blast_operation.all: get_all_result(ref code_pointer, ref vector_size, out f4_result); break;
                case blast_operation.any: get_any_result(ref code_pointer, ref vector_size, out f4_result); break;

                // indexing operations 
                case blast_operation.index_x: get_index_result(ref code_pointer, ref vector_size, out f4_result, 0); break;
                case blast_operation.index_y: get_index_result(ref code_pointer, ref vector_size, out f4_result, 1); break;
                case blast_operation.index_z: get_index_result(ref code_pointer, ref vector_size, out f4_result, 2); break;
                case blast_operation.index_w: get_index_result(ref code_pointer, ref vector_size, out f4_result, 3); break;

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
                            case extended_blast_operation.logn: get_log_result(ref code_pointer, ref vector_size, out f4_result); break;
                            case extended_blast_operation.log10: get_log10_result(ref code_pointer, ref vector_size, out f4_result); break;
                            case extended_blast_operation.exp10: get_exp10_result(ref code_pointer, ref vector_size, out f4_result); break;
                            case extended_blast_operation.cross: get_cross_result(ref code_pointer, ref vector_size, out f4_result); break;
                            case extended_blast_operation.exp: get_exp_result(ref code_pointer, ref vector_size, out f4_result); break;
                            case extended_blast_operation.dot: get_dot_result(ref code_pointer, ref vector_size, out f4_result); break;
                            case extended_blast_operation.log2: get_log2_result(ref code_pointer, ref vector_size, out f4_result); break;
                            case extended_blast_operation.sqrt: get_sqrt_result(ref code_pointer, ref vector_size, out f4_result); break;
                            case extended_blast_operation.rsqrt: get_rsqrt_result(ref code_pointer, ref vector_size, out f4_result); break;
                            case extended_blast_operation.pow: get_pow_result(ref code_pointer, ref vector_size, out f4_result); break;
                            case extended_blast_operation.sin: get_sin_result(ref code_pointer, ref vector_size, out f4_result); break;
                            case extended_blast_operation.cos: get_cos_result(ref code_pointer, ref vector_size, out f4_result); break;
                            case extended_blast_operation.tan: get_tan_result(ref code_pointer, ref vector_size, out f4_result); break;
                            case extended_blast_operation.sinh: get_sinh_result(ref code_pointer, ref vector_size, out f4_result); break;
                            case extended_blast_operation.cosh: get_cosh_result(ref code_pointer, ref vector_size, out f4_result); break;
                            case extended_blast_operation.atan: get_atan_result(ref code_pointer, ref vector_size, out f4_result); break;
                            case extended_blast_operation.atan2: get_atan2_result(ref code_pointer, ref vector_size, out f4_result); break;
                            case extended_blast_operation.degrees: get_degrees_result(ref code_pointer, ref vector_size, out f4_result); break;
                            case extended_blast_operation.radians: get_rad_result(ref code_pointer, ref vector_size, out f4_result); break;
                            case extended_blast_operation.clamp: get_clamp_result(ref code_pointer, ref vector_size, out f4_result); break;
                            case extended_blast_operation.normalize: get_normalize_result(ref code_pointer, ref vector_size, out f4_result); break;
                            case extended_blast_operation.saturate: get_saturate_result(ref code_pointer, ref vector_size, out f4_result); break;
                            case extended_blast_operation.lerp: get_lerp_result(ref code_pointer, ref vector_size, out f4_result); break;
                            case extended_blast_operation.slerp: get_slerp_result(ref code_pointer, ref vector_size, out f4_result); break;
                            case extended_blast_operation.nlerp: get_nlerp_result(ref code_pointer, ref vector_size, out f4_result); break;
                            case extended_blast_operation.ceil: get_ceil_result(ref code_pointer, ref vector_size, out f4_result); break;
                            case extended_blast_operation.floor: get_floor_result(ref code_pointer, ref vector_size, out f4_result); break;
                            case extended_blast_operation.frac: get_frac_result(ref code_pointer, ref vector_size, out f4_result); break;
                            case extended_blast_operation.remap: get_remap_result(ref code_pointer, ref vector_size, out f4_result); break;
                            case extended_blast_operation.ceillog2: get_ceillog2_result(ref code_pointer, ref vector_size, out f4_result); break;
                            case extended_blast_operation.ceilpow2: get_ceilpow2_result(ref code_pointer, ref vector_size, out f4_result); break;
                            case extended_blast_operation.unlerp: get_unlerp_result(ref code_pointer, ref vector_size, out f4_result); break;
                            case extended_blast_operation.floorlog2: get_floorlog2_result(ref code_pointer, ref vector_size, out f4_result); break;
                            default:
#if DEVELOPMENT_BUILD || TRACE
                                Debug.LogError($"blast.interpretor: get_function_result: codepointer: {code_pointer} => {code[code_pointer]}, extended operation {exop} not handled");
#endif
                                f4_result = float.NaN;
                                break;
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
                        break;
                    }
            }

        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="code_pointer"></param>
        /// <param name="vector_size"></param>
        /// <param name="f4_result"></param>
        void get_sequence_result(ref int code_pointer, ref byte vector_size, out float4 f4_result)
        {
            byte op = 0, prev_op = 0;

            blast_operation current_op = blast_operation.nop;

            bool not = false;
            bool minus = false;

            float4 f4 = float.NaN;
            f4_result = float.NaN; // not really needed but compiler cannot know controlflow so it thinks the value will be unassigned

            vector_size = 1; // vectors dont shrink in normal operations 
            byte current_vector_size = 1;

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
#else
                                        Debug.LogWarning($"Possible BUG: encountered non mathematical operation '{next_op}' after compound in statement at codepointer {code_pointer + 1}, expecting nop or assign");
#endif
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

                            // read next
                            last_is_bool_or_math_operation = true;
                            code_pointer++;
                            prev_op = op;
                        }
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
                            last_is_bool_or_math_operation = true;
                            code_pointer++;
                            prev_op = op;
                        }
                        continue;


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

                    case blast_operation.begin:
                        Assert.IsTrue(false, "should not be nesting compounds... compiler did not do its job wel");
                        break;

#endif                      
#if DEVELOPMENT_BUILD || TRACE
#endif

                    case blast_operation.pop:
                        {
                            BlastVariableDataType popped_type;
                            byte popped_vector_size;

                            void* pdata = pop_with_info(code_pointer, out popped_type, out popped_vector_size);
                            switch (popped_vector_size)
                            {
                                case 0:
                                case 4: f4 = ((float4*)pdata)[0]; vector_size = 4; break;
                                case 1: f4.x = ((float*)pdata)[0]; vector_size = 1; break;
                                case 2: f4.xy = ((float2*)pdata)[0]; vector_size = 2; break;
                                case 3: f4.xyz = ((float3*)pdata)[0]; vector_size = 3; break;
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
                    case blast_operation.abs: get_abs_result(ref code_pointer, ref vector_size, out f4); break;
                    case blast_operation.maxa: get_maxa_result(ref code_pointer, ref vector_size, out f4); break;
                    case blast_operation.mina: get_mina_result(ref code_pointer, ref vector_size, out f4); break;
                    case blast_operation.max: get_max_result(ref code_pointer, ref vector_size, out f4); break;
                    case blast_operation.min: get_min_result(ref code_pointer, ref vector_size, out f4); break;
                    case blast_operation.csum: get_csum_result(ref code_pointer, ref vector_size, out f4); break;
                    case blast_operation.trunc: get_trunc_result(ref code_pointer, ref vector_size, out f4); break;

                    // math utils 
                    case blast_operation.select: get_select_result(ref code_pointer, ref vector_size, out f4); break;
                    case blast_operation.fma: get_fma_result(ref code_pointer, ref vector_size, out f4); break;
                    case blast_operation.fmod: get_fmod_result(ref code_pointer, ref vector_size, out f4); break; 

                    // mula family
                    case blast_operation.mula: get_mula_result(ref code_pointer, ref vector_size, out f4); break;
                    case blast_operation.adda: get_adda_result(ref code_pointer, ref vector_size, out f4); break;
                    case blast_operation.suba: get_suba_result(ref code_pointer, ref vector_size, out f4); break;
                    case blast_operation.diva: get_diva_result(ref code_pointer, ref vector_size, out f4); break;

                    // any all 
                    case blast_operation.all: get_all_result(ref code_pointer, ref vector_size, out f4); break;
                    case blast_operation.any: get_any_result(ref code_pointer, ref vector_size, out f4); break;

                    // random 
                    case blast_operation.random: get_random_result(ref code_pointer, ref vector_size, out f4); break;

                    // indexing operations 
                    case blast_operation.index_x: get_index_result(ref code_pointer, ref vector_size, out f4, 0); break;
                    case blast_operation.index_y: get_index_result(ref code_pointer, ref vector_size, out f4, 1); break;
                    case blast_operation.index_z: get_index_result(ref code_pointer, ref vector_size, out f4, 2); break;
                    case blast_operation.index_w: get_index_result(ref code_pointer, ref vector_size, out f4, 3); break;

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
                                    case extended_blast_operation.logn: get_log_result(ref code_pointer, ref vector_size, out f4); break;
                                    case extended_blast_operation.log10: get_log10_result(ref code_pointer, ref vector_size, out f4); break;
                                    case extended_blast_operation.exp10: get_exp10_result(ref code_pointer, ref vector_size, out f4); break;
                                    case extended_blast_operation.cross: get_cross_result(ref code_pointer, ref vector_size, out f4); break;
                                    case extended_blast_operation.exp: get_exp_result(ref code_pointer, ref vector_size, out f4); break;
                                    case extended_blast_operation.dot: get_dot_result(ref code_pointer, ref vector_size, out f4); break;
                                    case extended_blast_operation.log2: get_log2_result(ref code_pointer, ref vector_size, out f4); break;
                                    case extended_blast_operation.sqrt: get_sqrt_result(ref code_pointer, ref vector_size, out f4); break;
                                    case extended_blast_operation.rsqrt: get_rsqrt_result(ref code_pointer, ref vector_size, out f4); break;
                                    case extended_blast_operation.pow: get_pow_result(ref code_pointer, ref vector_size, out f4); break;
                                    case extended_blast_operation.sin: get_sin_result(ref code_pointer, ref vector_size, out f4); break;
                                    case extended_blast_operation.cos: get_cos_result(ref code_pointer, ref vector_size, out f4); break;
                                    case extended_blast_operation.tan: get_tan_result(ref code_pointer, ref vector_size, out f4); break;
                                    case extended_blast_operation.sinh: get_sinh_result(ref code_pointer, ref vector_size, out f4); break;
                                    case extended_blast_operation.cosh: get_cosh_result(ref code_pointer, ref vector_size, out f4); break;
                                    case extended_blast_operation.atan: get_atan_result(ref code_pointer, ref vector_size, out f4); break;
                                    case extended_blast_operation.atan2: get_atan2_result(ref code_pointer, ref vector_size, out f4); break;
                                    case extended_blast_operation.degrees: get_degrees_result(ref code_pointer, ref vector_size, out f4); break;
                                    case extended_blast_operation.radians: get_rad_result(ref code_pointer, ref vector_size, out f4); break;
                                    case extended_blast_operation.ceil: get_ceil_result(ref code_pointer, ref vector_size, out f4); break;
                                    case extended_blast_operation.floor: get_floor_result(ref code_pointer, ref vector_size, out f4); break;
                                    case extended_blast_operation.frac: get_frac_result(ref code_pointer, ref vector_size, out f4); break;
                                    case extended_blast_operation.normalize: get_normalize_result(ref code_pointer, ref vector_size, out f4); break;
                                    case extended_blast_operation.saturate: get_saturate_result(ref code_pointer, ref vector_size, out f4); break;
                                    case extended_blast_operation.clamp: get_clamp_result(ref code_pointer, ref vector_size, out f4); break;
                                    case extended_blast_operation.lerp: get_lerp_result(ref code_pointer, ref vector_size, out f4); break;
                                    case extended_blast_operation.slerp: get_slerp_result(ref code_pointer, ref vector_size, out f4); break;
                                    case extended_blast_operation.nlerp: get_nlerp_result(ref code_pointer, ref vector_size, out f4); break;
                                    case extended_blast_operation.remap: get_remap_result(ref code_pointer, ref vector_size, out f4); break;
                                    case extended_blast_operation.ceillog2: get_ceillog2_result(ref code_pointer, ref vector_size, out f4); break;
                                    case extended_blast_operation.ceilpow2: get_ceilpow2_result(ref code_pointer, ref vector_size, out f4); break;
                                    case extended_blast_operation.unlerp: get_unlerp_result(ref code_pointer, ref vector_size, out f4); break;
                                    case extended_blast_operation.floorlog2: get_floorlog2_result(ref code_pointer, ref vector_size, out f4); break;

#if DEVELOPMENT_BUILD || TRACE
                                    default:
                                        Debug.LogError($"codepointer: {code_pointer} => {code[code_pointer]}, extended operation {exop} not handled");
                                        break;
#endif
                                }
                            }
                        }
                        break;

#if DEVELOPMENT_BUILD || TRACE 
                    case blast_operation.seed:
                        Assert.IsTrue(false, "NOT IMPLEMENTED YET");
                        break;
#endif 

                    case blast_operation.pi:
                    case blast_operation.inv_pi:
                    case blast_operation.epsilon:
                    case blast_operation.infinity:
                    case blast_operation.negative_infinity:
                    case blast_operation.nan:
                    case blast_operation.min_value:
                    case blast_operation.value_0:
                    case blast_operation.value_1:
                    case blast_operation.value_2:
                    case blast_operation.value_3:
                    case blast_operation.value_4:
                    case blast_operation.value_8:
                    case blast_operation.value_10:
                    case blast_operation.value_16:
                    case blast_operation.value_24:
                    case blast_operation.value_32:
                    case blast_operation.value_64:
                    case blast_operation.value_100:
                    case blast_operation.value_128:
                    case blast_operation.value_256:
                    case blast_operation.value_512:
                    case blast_operation.value_1000:
                    case blast_operation.value_1024:
                    case blast_operation.value_30:
                    case blast_operation.value_45:
                    case blast_operation.value_90:
                    case blast_operation.value_180:
                    case blast_operation.value_270:
                    case blast_operation.value_360:
                    case blast_operation.inv_value_2:
                    case blast_operation.inv_value_3:
                    case blast_operation.inv_value_4:
                    case blast_operation.inv_value_8:
                    case blast_operation.inv_value_10:
                    case blast_operation.inv_value_16:
                    case blast_operation.inv_value_24:
                    case blast_operation.inv_value_32:
                    case blast_operation.inv_value_64:
                    case blast_operation.inv_value_100:
                    case blast_operation.inv_value_128:
                    case blast_operation.inv_value_256:
                    case blast_operation.inv_value_512:
                    case blast_operation.inv_value_1000:
                    case blast_operation.inv_value_1024:
                    case blast_operation.inv_value_30:
                    case blast_operation.inv_value_45:
                    case blast_operation.inv_value_90:
                    case blast_operation.inv_value_180:
                    case blast_operation.inv_value_270:
                    case blast_operation.inv_value_360:
                        {
                            // read value
                            switch ((BlastVectorSizes)vector_size)
                            {
                                case BlastVectorSizes.float1: f4.x = engine_ptr->constants[op]; break;
                                case BlastVectorSizes.float2: f4.xy = engine_ptr->constants[op]; break;
                                case BlastVectorSizes.float3: f4.xyz = engine_ptr->constants[op]; break;
                                case BlastVectorSizes.float4: f4.xyzw = engine_ptr->constants[op]; break;
                                default:
#if DEVELOPMENT_BUILD || TRACE
                                    Debug.LogError($"BlastInterpretor.get_sequence_result: codepointer: {code_pointer} => {code[code_pointer]}, error: unsupported vectorsize for setting result from constant");
#endif
                                    break;
                            }
                        }
                        break;


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
                                    case BlastVectorSizes.float2: f4.xy = new float2(((float*)data)[id], ((float*)data)[id + 1]); break;
                                    case BlastVectorSizes.float3: f4.xyz = new float3(((float*)data)[id], ((float*)data)[id + 1], ((float*)data)[id + 2]); break;
                                    case 0:
                                    case BlastVectorSizes.float4: f4 = new float4(((float*)data)[id], ((float*)data)[id + 1], ((float*)data)[id + 2], ((float*)data)[id + 3]); break;
                                }


                                vector_size = vector_size > this_vector_size ? vector_size : this_vector_size; // probably massively incorrect but its too late 
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
                // if not an operation overwrite result
                //
                // -> on first value
                //
                //
                if (current_op == 0 && !((minus || not) && prev_op == 0))
                {
                    if ((minus || not))
                    {
#if STANDALONE_VSBUILD
                        if (minus && not)
                        {
                            Debug.LogError("minus and not together active: error");
                        }
#endif
                        if (minus)
                        {
                            f4 = -f4;
                            minus = false;
                        }
                        if (not)
                        {
                            f4 = math.select((float4)0, (float4)1, f4 == 0);
                            not = false;
                            // f4_result = math.select(f4_result, -f4_result, minus && !last_is_bool_or_math_operation);
                        }
                    }

                    // just set according to vector size
                    switch ((BlastVectorSizes)vector_size)
                    {
                        case BlastVectorSizes.float1: f4_result.x = f4.x; current_vector_size = 1; break;
                        case BlastVectorSizes.float2: f4_result.xy = f4.xy; current_vector_size = 2; break;
                        case BlastVectorSizes.float3: f4_result.xyz = f4.xyz; current_vector_size = 3; break;
                        case 0:
                        case BlastVectorSizes.float4: f4_result = f4; current_vector_size = 4; break;
                    }
                }
                else
                {
                    if (!last_is_bool_or_math_operation)
                    {
                        if ((minus || not))
                        {
#if STANDALONE_VSBUILD
                            if (minus && not)
                            {
                                Debug.LogError("minus and not together active: error");
                            }
#endif
                            if (minus)
                            {
                                f4 = -f4;
                                minus = false;
                            }
                            if (not)
                            {
                                f4 = math.select((float4)0, (float4)1, f4 == 0);
                                not = false;
                                // f4_result = math.select(f4_result, -f4_result, minus && !last_is_bool_or_math_operation);
                            }
                        }


                        switch ((BlastVectorSizes)current_vector_size)
                        {
                            case BlastVectorSizes.float1:

                                switch ((BlastVectorSizes)vector_size)
                                {
                                    // v1 = v1 + v1 -> no change in vectorsize 
                                    case BlastVectorSizes.float1: f4_result.x = BlastInterpretor.handle_op(current_op, f4_result.x, f4.x); break;
                                    case BlastVectorSizes.float2: f4_result.xy = BlastInterpretor.handle_op(current_op, f4_result.x, f4.xy); current_vector_size = 2; break;
                                    case BlastVectorSizes.float3: f4_result.xyz = BlastInterpretor.handle_op(current_op, f4_result.x, f4.xyz); current_vector_size = 3; break;
                                    case 0:
                                    case BlastVectorSizes.float4: f4_result = BlastInterpretor.handle_op(current_op, f4_result.x, f4); current_vector_size = 4; break;
#if DEVELOPMENT_BUILD || TRACE
                                    default:
                                        Debug.LogError($"execute-sequence: codepointer: {code_pointer} => {code[code_pointer]}, no support for vector of size {vector_size} -> {current_vector_size}");
                                        break;
#endif
                                }
                                break;

                            case BlastVectorSizes.float2:

                                switch ((BlastVectorSizes)vector_size)
                                {
                                    // adding a float1 to a float2 operation -> results in float2 
                                    case BlastVectorSizes.float1: f4_result.xy = BlastInterpretor.handle_op(current_op, f4_result.xy, f4.x); vector_size = 2; break;
                                    case BlastVectorSizes.float2: f4_result.xy = BlastInterpretor.handle_op(current_op, f4_result.xy, f4.xy); break;

#if DEVELOPMENT_BUILD || TRACE
                                    default:
                                        Debug.LogError($"execute-sequence: codepointer: {code_pointer} => {code[code_pointer]}, no support for vector of size {vector_size} -> {current_vector_size}");
                                        break;
#endif
                                }
                                break;

                            case BlastVectorSizes.float3:

                                switch ((BlastVectorSizes)vector_size)
                                {
                                    case BlastVectorSizes.float1: f4_result.xyz = BlastInterpretor.handle_op(current_op, f4_result.xyz, f4.x); vector_size = 3; break;
                                    case BlastVectorSizes.float3: f4_result.xyz = BlastInterpretor.handle_op(current_op, f4_result.xyz, f4.xyz); break;
#if DEVELOPMENT_BUILD || TRACE
                                    default:
                                        Debug.LogError($"execute-sequence: codepointer: {code_pointer} => {code[code_pointer]}, no support for vector of size {vector_size} -> {current_vector_size}");
                                        break;
#endif
                                }
                                break;

                            case 0:
                            case BlastVectorSizes.float4:

                                switch ((BlastVectorSizes)vector_size)
                                {
                                    case BlastVectorSizes.float1: f4_result = BlastInterpretor.handle_op(current_op, f4_result, f4.x); current_vector_size = 4; vector_size = 4; break;
                                    case 0:
                                    case BlastVectorSizes.float4: f4_result = BlastInterpretor.handle_op(current_op, f4_result, f4); current_vector_size = 4; vector_size = 4; break;
#if DEVELOPMENT_BUILD || TRACE
                                    default:
                                        Debug.LogError($"execute-sequence: codepointer: {code_pointer} => {code[code_pointer]}, no support for vector of size {vector_size} -> {current_vector_size}");
                                        break;
#endif
                                }
                                break;





#if DEVELOPMENT_BUILD || TRACE
                            default:
                                Debug.LogError($"execute-sequence: codepointer: {code_pointer} => {code[code_pointer]}, no support for vector of size {vector_size} -> {current_vector_size}");
                                break;
#endif


                        }
                    }
                }

                // apply minus (if in sequence)...  
                //
                //  TODO     dont think this will ever run after the changes 
                //
                if ((minus || not) && !last_is_bool_or_math_operation && prev_op != 0)
                {
#if STANDALONE_VSBUILD
                    if(minus && not)
                    {
                        Debug.LogError("minus and not together active: error"); 
                    }
#endif 
                    if (minus)
                    {
                        f4_result = -f4_result;
                        minus = false;
                    }
                    if(not)
                    {
                        f4_result = math.select((float4)0, (float4)1, f4_result == 0);
                        not = false;
                    }
                }
          
                //minus = minus && !last_is_bool_or_math_operation;

                // reset current operation if last thing was a value 
                if (!last_is_bool_or_math_operation) current_op = blast_operation.nop;

                // next
                code_pointer++;
            }

            return;
        }



        /// <summary>
        /// 
        /// </summary>
        /// <param name="blast"></param>
        /// <param name="environment"></param>
        /// <param name="caller"></param>
        /// <returns></returns>
        unsafe int execute([NoAlias]BlastEngineData* blast, [NoAlias]IntPtr environment, [NoAlias]IntPtr caller)
        {
            int iterations = 0;

            engine_ptr = blast;
            environment_ptr = environment;
            caller_ptr = caller;

            float* fdata = (float*)data;
            float4 f4_register = 0;

            // main loop 
            while (code_pointer < package.CodeSize)
            {
                iterations++;
                if (iterations > max_iterations) return (int)BlastError.error_max_iterations;

                byte vector_size = 1;
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
                    case blast_operation.ret: return (int)BlastError.success;
                    case blast_operation.nop: break;

                    case blast_operation.push:
                        push(ref code_pointer, ref vector_size, ref f4_register);
                        break;

                    //
                    // pushv knows more about what it is pushing up the stack, it can do it without running the compound 
                    //
                    case blast_operation.pushv:
                        pushv(ref code_pointer, ref vector_size, ref f4_register);
                        break;

                    //
                    // push the result of a function directly onto the stack instead of assigning it first or something
                    // 
                    case blast_operation.pushf:
                        pushf(ref code_pointer, ref vector_size, ref f4_register);
                        break;

                    //
                    // push the result of a function directly onto the stack instead of assigning it first or something
                    //          
                    case blast_operation.pushc:
                        pushc(ref code_pointer, ref vector_size, ref f4_register);
                        break;

                    case blast_operation.yield:
                        yield(f4_register);
                        return (int)BlastError.yield;

                    case blast_operation.seed:
                        f4_register.x = pop_or_value(++code_pointer);
                        engine_ptr->Seed(math.asuint(f4_register.x));
                        break;

                    case blast_operation.begin:
#if DEVELOPMENT_BUILD || TRACE
                        // jumping in from somewhere
                        //                          Debug.LogWarning($"burstscript.interpretor warning: scriptop.begin not expected in root at codepointer: {code_pointer}, f4: {f4_register}");
#endif
                        break;

                    case blast_operation.end: break;


                    //
                    // Assign single value/vector 
                    // - assignS assigns 1 datapoint to a assignee, from stack or other data 
                    //
                    case blast_operation.assigns:
                        {
                            // assign 1 value
                            byte assignee_op = code[code_pointer];

                            // determine indexer 
                            bool is_indexed = assignee_op < opt_id; 
                            blast_operation indexer = (blast_operation)math.select(0, assignee_op, is_indexed);
                            code_pointer = math.select(code_pointer, code_pointer + 1, is_indexed);
                            assignee_op = code[code_pointer];  // reading twice will probably be a lot faster then if() could test sometime.. todo

                            // get metadata on destination 
                            byte assignee = (byte)(assignee_op - opt_id);
                            byte s_assignee = BlastInterpretor.GetMetaDataSize(in metadata, in assignee);

                            // and pop the source 
                            BlastVariableDataType dt_pop;
                            void* pdata = pop_with_info(code_pointer + 1, out dt_pop, out vector_size);

#if DEVELOPMENT_BUILD || TRACE
                            // the compiler should have cought this, if it didt wel that would be noticed before going in release
                            // so we define this only for debug saving the if
                                // cannot set a component from a vector (yet)
                                if (vector_size != 1 && is_indexed)
                                {
                                    Debug.LogError($"blast.assignsingle: cannot set component from vector at #{code_pointer}, component: {indexer}");
                                    return (int)BlastError.error_assign_component_from_vector;
                                }
#endif 

                            switch ((byte)vector_size)
                            {
                                case (byte)BlastVectorSizes.float1:
                                    //if(Unity.Burst.CompilerServices.Hint.Unlikely(s_assignee != 1))
                                    if (s_assignee != 1 && !is_indexed)
                                    {
#if DEVELOPMENT_BUILD || TRACE
                                        Debug.LogError($"blast.assignsingle: assigned vector size mismatch at #{code_pointer}, should be size '{s_assignee}', evaluated '1', data id = {assignee}");
#endif
                                        return (int)BlastError.error_assign_vector_size_mismatch;
                                    }

                                    // if an indexer is set, then get offset (x = 0 , y = 1 etc) and add that to assignee
                                    int offset = math.select(assignee, assignee + (int)(indexer - blast_operation.index_x), is_indexed);

                                    // because assignee offsets into the datasegment we can just add the vector component index
                                    // to that offset 
                                    fdata[offset] = ((float*)pdata)[0];
                                    break;
                              

                                case (byte)BlastVectorSizes.float2:
                                    if (s_assignee != 2)
                                    {
#if DEVELOPMENT_BUILD || TRACE
                                        Debug.LogError($"blast.assignsingle: assigned vector size mismatch at #{code_pointer}, should be size '{s_assignee}', evaluated '2'");
#endif
                                        return (int)BlastError.error_assign_vector_size_mismatch;
                                    }
                                    fdata[assignee] = ((float*)pdata)[0];
                                    fdata[assignee + 1] = ((float*)pdata)[1];
                                    break;

                                case (byte)BlastVectorSizes.float3:
                                    if (s_assignee != 3)
                                    {
#if DEVELOPMENT_BUILD || TRACE
                                        Debug.LogError($"blast.assignsingle: assigned vector size mismatch at #{code_pointer}, should be size '{s_assignee}', evaluated '3'");
#endif
                                        return (int)BlastError.error_assign_vector_size_mismatch;
                                    }
                                    fdata[assignee] = ((float*)pdata)[0];
                                    fdata[assignee + 1] = ((float*)pdata)[1];
                                    fdata[assignee + 2] = ((float*)pdata)[2];
                                    break;

                                case (byte)BlastVectorSizes.float4:
                                    if (s_assignee != 4 || s_assignee == 0)
                                    {
#if DEVELOPMENT_BUILD || TRACE
                                        Debug.LogError($"blast.assignsingle: assigned vector size mismatch at #{code_pointer}, should be size '{s_assignee}', evaluated '4'");
#endif
                                        return (int)BlastError.error_assign_vector_size_mismatch;
                                    }
                                    fdata[assignee] = ((float*)pdata)[0];
                                    fdata[assignee + 1] = ((float*)pdata)[1];
                                    fdata[assignee + 2] = ((float*)pdata)[2];
                                    fdata[assignee + 3] = ((float*)pdata)[3];
                                    break;

                                default:
#if DEVELOPMENT_BUILD || TRACE
                                    Debug.LogError($"blast.assignsingle: vector size {vector_size} not allowed at codepointer {code_pointer}");
#endif
                                    return (int)BlastError.error_unsupported_operation_in_root;
                            }

                            code_pointer += 2;
                        }
                        break;

                    //
                    // assign the result of a function directly to the assignee
                    //
                    //
                    case blast_operation.assignf:
                        {
                            // get assignee 
                            byte assignee_op = code[code_pointer];

                            // get any index, branch free
                            bool is_indexed = assignee_op < opt_id;
                            blast_operation indexer = (blast_operation)math.select(0, assignee_op, is_indexed);
                            code_pointer = math.select(code_pointer, code_pointer + 1, is_indexed);
                            assignee_op = code[code_pointer];  // reading twice will probably be a lot faster then if() could test sometime.. todo

                            byte assignee = (byte)(assignee_op - opt_id);

#if DEVELOPMENT_BUILD || TRACE
                            // check its size?? in debug only, functions return size in vector_size 
                            byte s_assignee = BlastInterpretor.GetMetaDataSize(in metadata, in assignee);
#endif

                            code_pointer++;
                            get_function_result(ref code_pointer, ref vector_size, out f4_register);
                            code_pointer++;

#if DEVELOPMENT_BUILD || TRACE
                            // 4 == 0 depending on decoding 
                            s_assignee = (byte)math.select(s_assignee, 4, s_assignee == 0);
                            if (!is_indexed && s_assignee != vector_size)
                            {
                                Debug.LogError($"blast: assignf function, vector size mismatch at #{code_pointer}, should be size '{s_assignee}', function returned '{vector_size}', data id = {assignee}");
                                return (int)BlastError.error_assign_vector_size_mismatch;
                            }
                            // can only set vectorcomponents (.x) from float1
                            if (vector_size != 1 && is_indexed)
                            {
                                Debug.LogError($"blast.assignf: cannot set component from vector at #{code_pointer}, component: {indexer}");
                                return (int)BlastError.error_assign_component_from_vector; 
                            }
#endif
                            // set to assignee
                            switch (vector_size)
                            {
                                case 1:
                                    int offset = math.select(assignee, assignee + (int)(indexer - blast_operation.index_x), is_indexed);
                                    fdata[offset] = f4_register.x;
                                    break;
                                case 2: fdata[assignee] = f4_register.x; fdata[assignee + 1] = f4_register.y; break;
                                case 3: fdata[assignee] = f4_register.x; fdata[assignee + 1] = f4_register.y; fdata[assignee + 2] = f4_register.z; break;
                                case 4: fdata[assignee] = f4_register.x; fdata[assignee + 1] = f4_register.y; fdata[assignee + 2] = f4_register.z; fdata[assignee + 3] = f4_register.w; break;
                            }
                        }
                        break;

                    //
                    // assign the negated result of a function directly to the assignee
                    //
                    //
                    case blast_operation.assignfn:
                        {
                            // get assignee 
                            byte assignee_op = code[code_pointer];

                            // get any index, branch free
                            bool is_indexed = assignee_op < opt_id;
                            blast_operation indexer = (blast_operation)math.select(0, assignee_op, is_indexed);
                            code_pointer = math.select(code_pointer, code_pointer + 1, is_indexed);
                            assignee_op = code[code_pointer];  // reading twice will probably be a lot faster then if() could test sometime.. todo

                            byte assignee = (byte)(assignee_op - opt_id);

#if DEVELOPMENT_BUILD || TRACE
                            // check its size?? in debug only, functions return size in vector_size 
                            byte s_assignee = BlastInterpretor.GetMetaDataSize(in metadata, in assignee);
#endif

                            code_pointer++;
                            get_function_result(ref code_pointer, ref vector_size, out f4_register);
                            code_pointer++;
#if DEVELOPMENT_BUILD || TRACE
                            // 4 == 0 depending on decoding 
                            s_assignee = (byte)math.select(s_assignee, 4, s_assignee == 0);
                            if (!is_indexed && s_assignee != vector_size)
                            {
                                Debug.LogError($"blast: assignnf negated function, vector size mismatch at #{code_pointer}, should be size '{s_assignee}', function returned '{vector_size}', data id = {assignee}");
                                return (int)BlastError.error_assign_vector_size_mismatch;
                            }
                            // can only set vectorcomponents (.x) from float1
                            if (vector_size != 1 && is_indexed)
                            {
                                Debug.LogError($"blast.assignnf: cannot set component from vector at #{code_pointer}, component: {indexer}");
                                return (int)BlastError.error_assign_component_from_vector;
                            }
#endif
                            // set to assignee
                            switch (vector_size)
                            {
                                case 1:
                                    int offset = math.select(assignee, assignee + (int)(indexer - blast_operation.index_x), is_indexed);
                                    fdata[offset] = -f4_register.x; 
                                    break;
                                case 2: fdata[assignee] = -f4_register.x; fdata[assignee + 1] = -f4_register.y; break;
                                case 3: fdata[assignee] = -f4_register.x; fdata[assignee + 1] = -f4_register.y; fdata[assignee + 2] = -f4_register.z; break;
                                case 4: fdata[assignee] = -f4_register.x; fdata[assignee + 1] = -f4_register.y; fdata[assignee + 2] = -f4_register.z; fdata[assignee + 3] = -f4_register.w; break;
                            }
                        }
                        break;

                    // 
                    // assign the resultof a function directly to the assignee
                    // 
                    case blast_operation.assignfe:
                        {
                            // get assignee 
                            byte assignee_op = code[code_pointer];

                            // get any index, branch free
                            bool is_indexed = assignee_op < opt_id;
                            blast_operation indexer = (blast_operation)math.select(0, assignee_op, is_indexed);
                            code_pointer = math.select(code_pointer, code_pointer + 1, is_indexed);
                            assignee_op = code[code_pointer];  // reading twice will probably be a lot faster then if() could test sometime.. todo

                            byte assignee = (byte)(assignee_op - opt_id);

#if DEVELOPMENT_BUILD || TRACE
                            // check its size?? in debug only, functions return size in vector_size 
                            byte s_assignee = BlastInterpretor.GetMetaDataSize(in metadata, in assignee);
#endif
                            code_pointer++;
                            CallExternalFunction(ref code_pointer, ref vector_size, out f4_register);
                            code_pointer++;

#if DEVELOPMENT_BUILD || TRACE
                            // 4 == 0 depending on decoding 
                            s_assignee = (byte)math.select(s_assignee, 4, s_assignee == 0);
                            if (!is_indexed && s_assignee != vector_size)
                            {
                                Debug.LogError($"blast: assignfe external function, vector size mismatch at #{code_pointer}, should be size '{s_assignee}', function returned '{vector_size}', data id = {assignee}");
                                return (int)BlastError.error_assign_vector_size_mismatch;
                            }
                            // can only set vectorcomponents (.x) from float1
                            if (vector_size != 1 && is_indexed)
                            {
                                Debug.LogError($"blast.assignfe: cannot set component from vector at #{code_pointer}, component: {indexer}");
                                return (int)BlastError.error_assign_component_from_vector;
                            }
#endif
                            // set to assignee
                            switch (vector_size)
                            {
                                case 1:
                                    int offset = math.select(assignee, assignee + (int)(indexer - blast_operation.index_x), is_indexed);
                                    fdata[offset] = f4_register.x; 
                                    break;
                                case 2: fdata[assignee] = f4_register.x; fdata[assignee + 1] = f4_register.y; break;
                                case 3: fdata[assignee] = f4_register.x; fdata[assignee + 1] = f4_register.y; fdata[assignee + 2] = f4_register.z; break;
                                case 4: fdata[assignee] = f4_register.x; fdata[assignee + 1] = f4_register.y; fdata[assignee + 2] = f4_register.z; fdata[assignee + 3] = f4_register.w; break;
                            }
                        }
                        break;

                    // 
                    // assign the negated resultof a function directly to the assignee
                    // 
                    case blast_operation.assignfen:
                        {
                            // get assignee 
                            byte assignee_op = code[code_pointer];

                            // get any index, branch free
                            bool is_indexed = assignee_op < opt_id;
                            blast_operation indexer = (blast_operation)math.select(0, assignee_op, is_indexed);
                            code_pointer = math.select(code_pointer, code_pointer + 1, is_indexed);
                            assignee_op = code[code_pointer];  // reading twice will probably be a lot faster then if() could test sometime.. todo

                            byte assignee = (byte)(assignee_op - opt_id);

#if DEVELOPMENT_BUILD || TRACE
                            // check its size?? in debug only, functions return size in vector_size 
                            byte s_assignee = BlastInterpretor.GetMetaDataSize(in metadata, in assignee);
#endif

                            code_pointer++;
                            CallExternalFunction(ref code_pointer, ref vector_size, out f4_register);
                            code_pointer++;

#if DEVELOPMENT_BUILD || TRACE
                            // 4 == 0 depending on decoding 
                            s_assignee = (byte)math.select(s_assignee, 4, s_assignee == 0);
                            if (!is_indexed && s_assignee != vector_size)
                            {
                                Debug.LogError($"blast: assignfen, vector size mismatch at #{code_pointer}, should be size '{s_assignee}', function returned '{vector_size}', data id = {assignee}");
                                return (int)BlastError.error_assign_vector_size_mismatch;
                            }
                            // can only set vectorcomponents (.x) from float1
                            if (vector_size != 1 && is_indexed)
                            {
                                Debug.LogError($"blast.assignfen: cannot set component from vector at #{code_pointer}, component: {indexer}");
                                return (int)BlastError.error_assign_component_from_vector;
                            }
#endif
                            // set to assignee
                            switch (vector_size)
                            {
                                case 1:
                                    int offset = math.select(assignee, assignee + (int)(indexer - blast_operation.index_x), is_indexed);
                                    fdata[assignee] = -f4_register.x; 
                                    break;
                                case 2: fdata[assignee] = -f4_register.x; fdata[assignee + 1] = -f4_register.y; break;
                                case 3: fdata[assignee] = -f4_register.x; fdata[assignee + 1] = -f4_register.y; fdata[assignee + 2] = -f4_register.z; break;
                                case 4: fdata[assignee] = -f4_register.x; fdata[assignee + 1] = -f4_register.y; fdata[assignee + 2] = -f4_register.z; fdata[assignee + 3] = -f4_register.w; break;
                            }
                        }
                        break;


                    //
                    // assign a datapoint from vector elements, much like pushv does but then to stack.. lots of almost duplicate code that we dont want control flow for..  
                    // 
                    case blast_operation.assignv:
                        {
                            byte assignee_op = code[code_pointer];

                            bool is_indexed = assignee_op < opt_id;
                            if (is_indexed)
                            {
                                Debug.LogError($"blast.assignv: cannot set component from vector at #{code_pointer}");
                                return (int)BlastError.error_assign_component_from_vector;
                            }

                            byte assignee = (byte)(assignee_op - opt_id);
                            byte s_assignee = BlastInterpretor.GetMetaDataSize(in metadata, in assignee);

                            code_pointer++;
                            assignv(ref code_pointer, in s_assignee, &fdata[assignee]);
                        }
                        break;



                    //
                    // The one, expensive operation we want to avoid: Assign result of compound
                    //
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
                            byte assignee = (byte)(assignee_op - opt_id);

                            // advance 2 if on begin 1 otherwise
                            code_pointer += math.select(1, 2, code[code_pointer + 1] == (byte)blast_operation.begin);

                            // get vectorsize of the assigned parameter 
                            // vectorsize assigned MUST match 
                            byte s_assignee = BlastInterpretor.GetMetaDataSize(in metadata, in assignee);

                            // correct vectorsize 4 => it is 0 after compressing into 2 bits

                            // -> compounds
                            //  s_assignee = (byte) math.select(4, s_assignee, s_assignee == 0); 


                            //
                            // - a mixed sequence - no nested compound
                            // - a compound -> vector definition? push? compiler flattens everything else
                            // - a function call with sequence after... 
                            //

                            //
                            // for some functions the return vector size depends on the input vectorsize 
                            // in those cases the compiler should have correctly estimated the return vector size
                            // if it did not it would overwrite other data when the compiler allocates to few bytes for the assignee
                            // 
                            // !!- any function that is not correctly estimated will result in "BlastError.error_assign_vector_size_mismatch" !!
                            //



                            ////   TODO : compiler should identify if assigning 1 simple value and use different instruction !!!!! 


                            //                             f4_register = get_compound_result(ref code_pointer, ref vector_size, false);

                            get_sequence_result(ref code_pointer, ref vector_size, out f4_register);

#if DEVELOPMENT_BUILD || TRACE
                            // the compiler should have cought this, if it didt wel that would be noticed before going in release
                            // so we define this only for debug saving the if
                            // cannot set a component from a vector (yet)
                            if (vector_size != 1 && is_indexed)
                            {
                                Debug.LogError($"blast.assign: cannot set component from vector at #{code_pointer}, component: {indexer}");
                                return (int)BlastError.error_assign_component_from_vector;
                            }
#endif 

                            // set assigned data fields in stack 
                            switch ((byte)vector_size)
                            {
                                case (byte)BlastVectorSizes.float1:
                                    //if(Unity.Burst.CompilerServices.Hint.Unlikely(s_assignee != 1))
                                    if (s_assignee != 1 && !is_indexed)
                                    {
#if DEVELOPMENT_BUILD || TRACE
                                        Debug.LogError($"blast: assigned vector size mismatch at #{code_pointer}, should be size '{s_assignee}', evaluated '1', data id = {assignee}");
#endif
                                        return (int)BlastError.error_assign_vector_size_mismatch;
                                    }
                                    int offset = math.select(assignee, assignee + (int)(indexer - blast_operation.index_x), is_indexed);
                                    fdata[offset] = f4_register.x;
                                    break;

                                case (byte)BlastVectorSizes.float2:
                                    if (s_assignee != 2)
                                    {
#if DEVELOPMENT_BUILD || TRACE
                                        Debug.LogError($"blast: assigned vector size mismatch at #{code_pointer}, should be size '{s_assignee}', evaluated '2'");
#endif
                                        return (int)BlastError.error_assign_vector_size_mismatch;
                                    }
                                    fdata[assignee] = f4_register.x;
                                    fdata[assignee + 1] = f4_register.y;
                                    break;

                                case (byte)BlastVectorSizes.float3:
                                    if (s_assignee != 3)
                                    {
#if DEVELOPMENT_BUILD || TRACE
                                        Debug.LogError($"blast: assigned vector size mismatch at #{code_pointer}, should be size '{s_assignee}', evaluated '3'");
#endif
                                        return (int)BlastError.error_assign_vector_size_mismatch;
                                    }
                                    fdata[assignee] = f4_register.x;
                                    fdata[assignee + 1] = f4_register.y;
                                    fdata[assignee + 2] = f4_register.z;
                                    break;

                                case (byte)BlastVectorSizes.float4:
                                    if (s_assignee != 4)
                                    {
#if DEVELOPMENT_BUILD || TRACE
                                        Debug.LogError($"blast: assigned vector size mismatch at #{code_pointer}, should be size '{s_assignee}', evaluated '4'");
#endif
                                        return (int)BlastError.error_assign_vector_size_mismatch;
                                    }
                                    fdata[assignee] = f4_register.x;
                                    fdata[assignee + 1] = f4_register.y;
                                    fdata[assignee + 2] = f4_register.z;
                                    fdata[assignee + 3] = f4_register.w;
                                    break;

                                default:
#if DEVELOPMENT_BUILD || TRACE
                                    Debug.LogError($"blast.interpretor: vector size {vector_size} not allowed at codepointer {code_pointer}");
#endif
                                    return (int)BlastError.error_unsupported_operation_in_root;
                            }

                        }
                        break;

                    //
                    // JZ: a condition may contain push commands that should run from the root
                    // 
                    //


                    case blast_operation.jz:
                        {
                            // calc endlocation of 
                            int offset = code[code_pointer];
                            int jump_to = code_pointer + offset;

                            // eval condition 
                            code_pointer += math.select(1, 2, code[code_pointer + 1] == (byte)blast_operation.begin);

                            // eval all push commands before running the compound
                            bool is_push = true;
                            do
                            {
                                switch ((blast_operation)code[code_pointer])
                                {
                                    case blast_operation.pushc:
                                        code_pointer++;
                                        pushc(ref code_pointer, ref vector_size, ref f4_register);
                                        code_pointer++;
                                        break;
                                    case blast_operation.push:
                                        code_pointer++;
                                        push(ref code_pointer, ref vector_size, ref f4_register);
                                        code_pointer++;
                                        break;
                                    case blast_operation.pushf:
                                        code_pointer++;
                                        pushf(ref code_pointer, ref vector_size, ref f4_register);
                                        code_pointer++;
                                        break;
                                    case blast_operation.pushv:
                                        code_pointer++;
                                        pushv(ref code_pointer, ref vector_size, ref f4_register);
                                        code_pointer++;
                                        break;
                                    default:
                                        is_push = false;
                                        break;
                                }
                            }
                            while (is_push && code_pointer < package.CodeSize);

                            // get result from condition
                            get_sequence_result(ref code_pointer, ref vector_size, out f4_register);

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

                            // eval all push commands before running the compound
                            bool is_push = true;
                            do
                            {
                                switch ((blast_operation)code[code_pointer])
                                {
                                    case blast_operation.pushc:
                                        code_pointer++;
                                        pushc(ref code_pointer, ref vector_size, ref f4_register);
                                        code_pointer++;
                                        break;
                                    case blast_operation.push:
                                        code_pointer++;
                                        push(ref code_pointer, ref vector_size, ref f4_register);
                                        code_pointer++;
                                        break;
                                    case blast_operation.pushf:
                                        code_pointer++;
                                        pushf(ref code_pointer, ref vector_size, ref f4_register);
                                        code_pointer++;
                                        break;
                                    case blast_operation.pushv:
                                        code_pointer++;
                                        pushv(ref code_pointer, ref vector_size, ref f4_register);
                                        code_pointer++;
                                        break;
                                    default:
                                        is_push = false;
                                        break;
                                }
                            }
                            while (is_push && code_pointer < package.CodeSize);

                            // get result from condition 
                            get_sequence_result(ref code_pointer, ref vector_size, out f4_register);

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
                            short offset = (short)(code[code_pointer] << 8 + code[code_pointer + 1]);
                            code_pointer = code_pointer - offset;
                            break;
                        }


                    //
                    // Extended Op == 255 => next byte encodes an operation not used in switch above (more non standard ops)
                    //
                    case blast_operation.ex_op:
                        {
                            extended_blast_operation exop = (extended_blast_operation)code[code_pointer];
                            switch (exop)
                            {
                                case extended_blast_operation.call:
                                    {
                                        CallExternalFunction(ref code_pointer, ref vector_size, out f4_register);
                                        code_pointer++;
                                    }
                                    break;

                                case extended_blast_operation.debugstack:
                                    {
                                        // write contents of stack to the debug stream as a detailed report; 
                                        // write contents of a data element to the debug stream 
                                        code_pointer++;
#if DEVELOPMENT_BUILD || TRACE
                                        Handle_DebugStack();
#endif
                                    }
                                    break;

                                case extended_blast_operation.debug:
                                    {
                                        // write contents of a data element to the debug stream 
                                        code_pointer++;

                                        int op_id = code[code_pointer];
                                        BlastVariableDataType datatype = default;

                                        // even if not in debug, if the command is encoded any stack op needs to be popped 
                                        void* pdata = pop_with_info(code_pointer, out datatype, out vector_size);

#if DEVELOPMENT_BUILD || TRACE
                                        Handle_DebugData(code_pointer, vector_size, op_id, datatype, pdata);
#endif
                                        code_pointer++;
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
                            return (int)BlastError.error_unsupported_operation_in_root;
                        }
                }
            }

            return (int)BlastError.success;
        }



        #endregion

    };
}

