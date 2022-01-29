using NSS.Blast.Interpretor;

#if STANDALONE_VSBUILD
    using NSS.Blast.Standalone;
    using Unity.Assertions;
#else
    using UnityEngine; 
#endif

using System;
using Unity.Burst;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using Unity.Collections;
using System.Runtime.CompilerServices;

namespace NSS.Blast.SSMD
{

    /// <summary>
    /// The SSMD Interpretor 
    /// </summary>
    [BurstCompile]
    unsafe public struct BlastSSMDInterpretor
    {
        #region Internal Data 
        internal BlastPackageData package;

        internal int code_pointer;
        internal int stack_offset;

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
        internal unsafe void** data;
        internal int ssmd_datacount;

        [NoAlias]
        [NativeDisableUnsafePtrRestriction]
        internal unsafe void** stack;


        [NoAlias]
        [NativeDisableUnsafePtrRestriction]
        internal unsafe float4* register;

        /// <summary>
        /// metadata segment pointer
        /// </summary>
        [NoAlias]
        [NativeDisableUnsafePtrRestriction]
        internal unsafe byte* metadata;

        #endregion

        #region Public SetPackage & Execute 
        /// <summary>
        /// set ssmd package data 
        /// </summary>
        public BlastError SetPackage(in BlastPackageData pkg)
        {
            if (pkg.PackageMode != BlastPackageMode.SSMD) return BlastError.ssmd_invalid_packagemode;

            package = pkg;

            code = package.Code;
            data = null;
            metadata = package.Metadata;

            return BlastError.success;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="packagedata"></param>
        /// <param name="blast"></param>
        /// <param name="environment"></param>
        /// <param name="ssmddata"></param>
        /// <param name="ssmd_data_count"></param>
        /// <returns></returns>
        public int Execute(in BlastPackageData packagedata, [NoAlias]BlastEngineDataPtr blast, [NoAlias]IntPtr environment, [NoAlias]BlastSSMDDataStack* ssmddata, int ssmd_data_count)
        {
            BlastError res = SetPackage(packagedata);
            if (res != BlastError.success) return (int)res;

            return Execute(blast, environment, ssmddata, ssmd_datacount);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="blast"></param>
        /// <param name="environment"></param>
        /// <param name="ssmddata"></param>
        /// <param name="ssmd_data_count"></param>
        /// <returns></returns>
        public int Execute([NoAlias]BlastEngineDataPtr blast, [NoAlias]IntPtr environment, [NoAlias]BlastSSMDDataStack* ssmddata, int ssmd_data_count)
        {
            if (!blast.IsCreated) return (int)BlastError.error_blast_not_initialized;
            if (code == null || metadata == null || IntPtr.Zero == package.CodeSegmentPtr) return (int)BlastError.error_execute_package_not_correctly_set;

            // set shared environment 
            engine_ptr = blast.Data;
            environment_ptr = environment;

            // a register for each of max vectorsize
            float4* register = stackalloc float4[ssmd_data_count];

            // generate a list of datasegment pointers on the stack:
            byte** datastack = stackalloc byte*[ssmd_data_count];
            int datastack_stride = package.SSMDDataSize;

            for (int i = 0; i < ssmd_data_count; i++)
                datastack[i] = ((byte*)ssmddata) + (datastack_stride * i);

            // execute 
            return Execute(datastack, ssmd_data_count, register);
        }
        #endregion

        #region Stack Functions 
        /// <summary>
        /// push register[n].x as float
        /// </summary>
        void push_register_f1()
        {
            // update metadata with type set 
            BlastInterpretor.SetMetaData(metadata, BlastVariableDataType.Numeric, 1, (byte)stack_offset);

            // then push the actual data for each element 
            for (int i = 0; i < ssmd_datacount; i++)
            {
                ((float*)stack[i])[stack_offset] = register[i].x;
            }

            // advance 1 element in stack offset 
            stack_offset += 1;
        }

        /// <summary>
        /// push data value on stack
        /// </summary>
        /// <param name="index">index of data to lookup</param>
        void push_data_f1(int index)
        {
            // update metadata with type set 
            BlastInterpretor.SetMetaData(metadata, BlastVariableDataType.Numeric, 1, (byte)stack_offset);

            // then push the actual data for each element 
            for (int i = 0; i < ssmd_datacount; i++)
            {
                float* p = ((float*)stack[i]);
                p[stack_offset] = p[i];
            }

            // advance 1 element in stack offset 
            stack_offset += 1;
        }

        /// <summary>
        /// push register[n].xy as float2 onto stack
        /// </summary>
        void push_register_f2()
        {
            BlastInterpretor.SetMetaData(metadata, BlastVariableDataType.Numeric, 2, (byte)stack_offset);
            for (int i = 0; i < ssmd_datacount; i++)
            {
                ((float*)stack[i])[stack_offset] = register[i].x;
                ((float*)stack[i])[stack_offset + 1] = register[i].y;
            }
            stack_offset += 2;
        }

        /// <summary>
        /// push register[n].xyz as float3 onto stack
        /// </summary>
        void push_register_f3()
        {
            BlastInterpretor.SetMetaData(metadata, BlastVariableDataType.Numeric, 3, (byte)stack_offset);
            for (int i = 0; i < ssmd_datacount; i++)
            {
                ((float*)stack[i])[stack_offset] = register[i].x;
                ((float*)stack[i])[stack_offset + 1] = register[i].y;
                ((float*)stack[i])[stack_offset + 2] = register[i].z;
            }
            stack_offset += 3;  // size 3
        }

        /// <summary>
        /// push register[n] as float4 onto stack
        /// </summary>
        void push_register_f4()
        {
            BlastInterpretor.SetMetaData(metadata, BlastVariableDataType.Numeric, 4, (byte)stack_offset);
            for (int i = 0; i < ssmd_datacount; i++)
            {
                ((float*)stack[i])[stack_offset] = register[i].x;
                ((float*)stack[i])[stack_offset + 1] = register[i].y;
                ((float*)stack[i])[stack_offset + 2] = register[i].z;
                ((float*)stack[i])[stack_offset + 3] = register[i].w;
            }
            stack_offset += 4;  // size 4
        }

        /// <summary>
        /// push a float1 value on te stack retrieved via 1 pop 
        /// </summary>
        void push_f1_pop_1(in int code_pointer)
        {
            push_pop_f(code_pointer, 1);
        }

        /// <summary>
        /// push a float2 value on te stack retrieved via 2x 1 pop 
        /// - each element may have a diferent origen, we need to account for that 
        /// </summary>
        void push_f2_pop_11(in int code_pointer)
        {
            // we should not use register data here but temp 
            float* f1 = stackalloc float[ssmd_datacount];
            float* f2 = stackalloc float[ssmd_datacount];

            // we have to first get all data and then copy it, 
            // we cannot do this with 1 buffer because we do no know what the stack_offset we end up being without the pops
            pop_f1_into(code_pointer, f1);
            pop_f1_into(code_pointer + 1, f2);

            for (int i = 0; i < ssmd_datacount; i++)
            {
                // Burst compiler crashed over previous abbomination
                ((float*)stack[i])[stack_offset] = f1[i];
                ((float*)stack[i])[stack_offset + 1] = f2[i];
            }

            // update metadata and stack offset 
            BlastInterpretor.SetMetaData(metadata, BlastVariableDataType.Numeric, 2, (byte)stack_offset);
            stack_offset += 2;
        }

        /// <summary>
        /// push an float3 value on te stack retrieved via 3x 1 pop 
        /// - each element may have a diferent origen, we need to account for that 
        /// </summary>
        void push_f3_pop_111(in int code_pointer)
        {
            // we should not use register data here but temp 
            float* f1 = stackalloc float[ssmd_datacount];
            float* f2 = stackalloc float[ssmd_datacount];
            float* f3 = stackalloc float[ssmd_datacount];

            // we have to first get all data and then copy it, 
            // we cannot do this with 1 buffer because we do no know what the stack_offset we end up being without the pops
            pop_f1_into(code_pointer, f1);
            pop_f1_into(code_pointer + 1, f2);
            pop_f1_into(code_pointer + 2, f3);

            for (int i = 0; i < ssmd_datacount; i++)
            {
                ((float*)stack[i])[stack_offset] = f1[i];
                ((float*)stack[i])[stack_offset + 1] = f2[i];
                ((float*)stack[i])[stack_offset + 2] = f3[i];
            }

            // update metadata and stack offset 
            BlastInterpretor.SetMetaData(metadata, BlastVariableDataType.Numeric, 3, (byte)stack_offset);
            stack_offset += 3;
        }

        /// <summary>
        /// push an float4 value on te stack retrieved via 4x 1 pop 
        /// - each element may have a diferent origen, we need to account for that 
        /// </summary>
        void push_f4_pop_1111(in int code_pointer)
        {
            // we should not use register data here but temp 
            float* f1 = stackalloc float[ssmd_datacount];
            float* f2 = stackalloc float[ssmd_datacount];
            float* f3 = stackalloc float[ssmd_datacount];
            float* f4 = stackalloc float[ssmd_datacount];

            // we have to first get all data and then copy it, 
            // we cannot do this with 1 buffer because we do no know what the stack_offset we end up being without the pops
            pop_f1_into(code_pointer, f1);
            pop_f1_into(code_pointer + 1, f2);
            pop_f1_into(code_pointer + 2, f3);
            pop_f1_into(code_pointer + 3, f4);

            for (int i = 0; i < ssmd_datacount; i++)
            {
                ((float*)stack[i])[stack_offset] = f1[i];
                ((float*)stack[i])[stack_offset + 1] = f2[i];
                ((float*)stack[i])[stack_offset + 2] = f3[i];
                ((float*)stack[i])[stack_offset + 3] = f4[i];
            }

            // update metadata and stack offset 
            BlastInterpretor.SetMetaData(metadata, BlastVariableDataType.Numeric, 4, (byte)stack_offset);
            stack_offset += 4;
        }

        /// <summary>
        /// pop a float1 value from the stack and copy it into destination 
        /// </summary>
        /// <param name="destination">destination f1*</param>
        void stack_pop_f1_into([NoAlias]float* destination)
        {
            stack_offset = stack_offset - 1;

#if DEVELOPMENT_BUILD
            if (stack_offset < 0)
            {
                Debug.LogError($"blast.ssmd.stack: attempting to pop too much data (1 float) from the stack, offset = {stack_offset}");
                for (int i = 0; i < ssmd_datacount; i++) destination[i] = float.NaN;
                return;
            }
            if (BlastInterpretor.GetMetaDataType(metadata, (byte)stack_offset) != BlastVariableDataType.Numeric)
            {
                Debug.LogError($"blast.ssmd.stack: pop(float) type mismatch, stack contains Integer/ID datatype");
                for (int i = 0; i < ssmd_datacount; i++) destination[i] = float.NaN;
                return;
            }
            byte size = BlastInterpretor.GetMetaDataSize(metadata, (byte)stack_offset);
            if (size != 1)
            {
                Debug.LogError($"blast.ssmd.stack: pop(float) size mismatch, stack contains vectorsize {size} datatype while size 1 is expected");
                for (int i = 0; i < ssmd_datacount; i++) destination[i] = float.NaN; 
                return;
            }
#endif

            for (int i = 0; i < ssmd_datacount; i++)
            {
                destination[i] = ((float*)stack[i])[stack_offset];
            }
        }


        void pop_op_meta(in byte operation, out BlastVariableDataType datatype, out byte vector_size)
        {
            if (operation >= BlastInterpretor.opt_id)
            {
                datatype = BlastInterpretor.GetMetaDataType(metadata, (byte)(operation - BlastInterpretor.opt_id));
                vector_size = BlastInterpretor.GetMetaDataSize(metadata, (byte)(operation - BlastInterpretor.opt_id));
            }
            else
            if (operation == (byte)blast_operation.pop)
            {
                datatype = BlastInterpretor.GetMetaDataType(metadata, (byte)(stack_offset - 1));
                vector_size = BlastInterpretor.GetMetaDataSize(metadata, (byte)(stack_offset - 1));
            }
            else
            if (operation >= BlastInterpretor.opt_value)
            {
                datatype = BlastVariableDataType.Numeric;
                vector_size = 1;
            }
            else
            {
#if DEVELOPMENT_BUILD
                Debug.LogError($"pop_op_meta -> select op by constant value is not supported, called with opcode: {operation}");
#endif
                datatype = BlastVariableDataType.Numeric;
                vector_size = 1;
            }
        }

        void pop_fn_into(in int code_pointer, [NoAlias]float4* destination, bool minus, ref byte vector_size)
        {
            vector_size = 0;
            byte c = code[code_pointer];

            if (c >= BlastInterpretor.opt_id)
            {
                BlastVariableDataType type = BlastInterpretor.GetMetaDataType(metadata, (byte)(c - BlastInterpretor.opt_id));
                vector_size = BlastInterpretor.GetMetaDataSize(metadata, (byte)(c - BlastInterpretor.opt_id));
#if DEVELOPMENT_BUILD
                if (type != BlastVariableDataType.Numeric)
                {
                    Debug.LogError($"blast.ssmd.stack, pop_fn_into -> data mismatch, expecting numeric of size n, found {type} of size {vector_size} at data offset {c - BlastInterpretor.opt_id}");
                    return;
                }
#endif
                // variable acccess
                switch (vector_size)
                {
                    case 1:
                        if (minus)
                        {
                            for (int i = 0; i < ssmd_datacount; i++)
                                destination[i].x = -(((float*)data[i])[c - BlastInterpretor.opt_id]);
                        }
                        else
                        {
                            for (int i = 0; i < ssmd_datacount; i++)
                                destination[i].x = ((float*)data[i])[c - BlastInterpretor.opt_id];
                        }
                        break;

                    case 2:
                        if (minus)
                        {
                            for (int i = 0; i < ssmd_datacount; i++)
                            {
                                destination[i].x = -((float*)data[i])[c - BlastInterpretor.opt_id];
                                destination[i].y = -((float*)data[i])[c - BlastInterpretor.opt_id + 1];
                            }
                        }
                        else
                        {
                            for (int i = 0; i < ssmd_datacount; i++)
                            {
                                destination[i].x = ((float*)data[i])[c - BlastInterpretor.opt_id];
                                destination[i].y = ((float*)data[i])[c - BlastInterpretor.opt_id + 1];
                            }
                            // no problem in visual studio but its in unity/burst ? 
                            //  destination[i].xy = ((float2*)(void*)&((float*)data[i])[c - BlastInterpretor.opt_id])[0];
                        }
                        break;
                    case 3:
                        if (minus)
                        {
                            for (int i = 0; i < ssmd_datacount; i++)
                                destination[i].xyz = -((float3*)(void*)&((float*)data[i])[c - BlastInterpretor.opt_id])[0];
                        }
                        else
                        {
                            for (int i = 0; i < ssmd_datacount; i++)
                                destination[i].xyz = ((float3*)(void*)&((float*)data[i])[c - BlastInterpretor.opt_id])[0];
                        }
                        break;
                    case 0:
                    case 4:
                        vector_size = 4;
                        if (minus)
                        {
                            for (int i = 0; i < ssmd_datacount; i++)
                                destination[i].xyzw = -((float4*)(void*)&((float*)data[i])[c - BlastInterpretor.opt_id])[0];
                        }
                        else
                        {
                            for (int i = 0; i < ssmd_datacount; i++)
                                destination[i].xyzw = ((float4*)(void*)&((float*)data[i])[c - BlastInterpretor.opt_id])[0];
                        }
                        break;
#if DEVELOPMENT_BUILD
                   default:
                        Debug.LogError($"blast.ssmd.stack, pop_fn_into -> vectorsize not supported, found {type} of size {vector_size} at data offset {c - BlastInterpretor.opt_id}");
                        break;
#endif

                }
            }
            else
            if (c == (byte)blast_operation.pop)
            {
                BlastVariableDataType type = BlastInterpretor.GetMetaDataType(metadata, (byte)(stack_offset - 1));
                vector_size = BlastInterpretor.GetMetaDataSize(metadata, (byte)(stack_offset - 1));
#if DEVELOPMENT_BUILD
                if (type != BlastVariableDataType.Numeric)
                {
                    Debug.LogError($"blast.ssmd.stack, pop_fn_into -> stackdata mismatch, expecting numeric of size n, found {type} of size {vector_size} at stack offset {stack_offset}");
                    return;
                }
#endif
                // stack pop 
                switch (vector_size)
                {
                    case 1:
                        stack_offset = stack_offset - 1;
                        if (minus)
                        {
                            for (int i = 0; i < ssmd_datacount; i++)
                                destination[i].x = -(((float*)stack[i])[stack_offset]);
                        }
                        else
                        {
                            for (int i = 0; i < ssmd_datacount; i++)
                                destination[i].x = ((float*)stack[i])[stack_offset];
                        }
                        break;

                    case 2:
                        stack_offset = stack_offset - 2;
                        if (minus)
                        {
                            for (int i = 0; i < ssmd_datacount; i++)
                                destination[i].xy = -((float2*)(void*)&((float*)stack[i])[stack_offset])[0];
                        }
                        else
                        {
                            for (int i = 0; i < ssmd_datacount; i++)
                                destination[i].xy = ((float2*)(void*)&((float*)stack[i])[stack_offset])[0];
                        }
                        break;
                    case 3:
                        stack_offset = stack_offset - 3;
                        if (minus)
                        {
                            for (int i = 0; i < ssmd_datacount; i++)
                                destination[i].xyz = -((float3*)(void*)&((float*)stack[i])[stack_offset])[0];
                        }
                        else
                        {
                            for (int i = 0; i < ssmd_datacount; i++)
                                destination[i].xyz = ((float3*)(void*)&((float*)stack[i])[stack_offset])[0];
                        }
                        break;
                    case 0:
                    case 4:
                        vector_size = 4;
                        stack_offset = stack_offset - 4;
                        if (minus)
                        {
                            for (int i = 0; i < ssmd_datacount; i++)
                                destination[i].xyzw = -((float4*)(void*)&((float*)stack[i])[stack_offset])[0];
                        }
                        else
                        {
                            for (int i = 0; i < ssmd_datacount; i++)
                                destination[i].xyzw = ((float4*)(void*)&((float*)stack[i])[stack_offset])[0];
                        }
                        break;
#if DEVELOPMENT_BUILD
                    default:
                        Debug.LogError($"blast.ssmd.stack, pop_fn_into -> vectorsize not supported, found numeric of size {vector_size}, found {type} of size {vector_size} at data offset {c - BlastInterpretor.opt_id}");
                        break;
#endif

                }
            }
            else
            if (c >= BlastInterpretor.opt_value)
            {
                float constant = engine_ptr->constants[c];

                constant = math.select(constant, -constant, minus);
                // update f4 from f1    
                for (int i = 0; i < ssmd_datacount; i++)
                {
                    destination[i] = constant;
                }
            }
            else
            {
                // error or.. constant by operation value.... 
#if DEVELOPMENT_BUILD
                Debug.LogError("pop_fn_into -> select op by constant value is not supported");
#endif
            }
        }

        #region pop_f[1|2|3|4]_into 

        /// <summary>
        /// pop a float1 value form stack data or constant source and put it in destination 
        /// </summary>
        void pop_f1_into(in int code_pointer, [NoAlias]float* destination)
        {
            // we get either a pop instruction or an instruction to get a value 
            byte c = code[code_pointer];

            if (c >= BlastInterpretor.opt_id)
            {
#if DEVELOPMENT_BUILD
                BlastVariableDataType type = BlastInterpretor.GetMetaDataType(metadata, (byte)(c - BlastInterpretor.opt_id));
                int size = BlastInterpretor.GetMetaDataSize(metadata, (byte)(c - BlastInterpretor.opt_id));
                if (size != 1 || type != BlastVariableDataType.Numeric)
                {
                    Debug.LogError($"blast.ssmd.stack, pop_f1_into -> data mismatch, expecting numeric of size 1, found {type} of size {size} at data offset {c - BlastInterpretor.opt_id}");
                    return;
                }
#endif
                // variable acccess
                for (int i = 0; i < ssmd_datacount; i++)
                {
                    destination[i] = ((float*)data[i])[c - BlastInterpretor.opt_id];
                }
            }
            else
            if (c == (byte)blast_operation.pop)
            {
#if DEVELOPMENT_BUILD
                BlastVariableDataType type = BlastInterpretor.GetMetaDataType(metadata, (byte)(stack_offset - 1));
                int size = BlastInterpretor.GetMetaDataSize(metadata, (byte)(stack_offset - 1));
                if (size != 1 || type != BlastVariableDataType.Numeric)
                {
                    Debug.LogError($"blast.ssmd.stack, pop_f1_into -> stackdata mismatch, expecting numeric of size 1, found {type} of size {size} at stack offset {stack_offset}");
                    return;
                }
#endif
                // stack pop 
                stack_offset = stack_offset - 1;
                for (int i = 0; i < ssmd_datacount; i++)
                {
                    destination[i] = ((float*)stack[i])[stack_offset];
                }
            }
            else
            if (c >= BlastInterpretor.opt_value)
            {
                float constant = engine_ptr->constants[c];
                for (int i = 0; i < ssmd_datacount; i++)
                {
                    destination[i] = constant;
                }
            }
            else
            {
                // error or.. constant by operation value.... 
#if DEVELOPMENT_BUILD
                Debug.LogError("pop_f1_into -> select op by constant value is not supported");
#endif
            }
        }


        /// <summary>
        /// pop a float and assign it to dataindex inside a datasegment 
        /// - shouldt the compiler just directly assign the dataindex (if possible)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="code_pointer"></param>
        /// <param name="dataindex"></param>
        void pop_fx_into<T>(in int code_pointer, byte dataindex) where T : unmanaged
        {
            int vector_size = sizeof(T) / 4;

            BlastVariableDataType datatype = BlastInterpretor.GetMetaDataType(metadata, dataindex);
            int datasize = BlastInterpretor.GetMetaDataSize(metadata, dataindex);

#if DEVELOPMENT_BUILD
            if (datatype != BlastVariableDataType.Numeric)
            {
                Debug.LogError($"blast.ssmd.pop_fx_into -> datatype of data at index {dataindex} is not numeric but {datatype}");
            }
            datasize = (byte)math.select(datasize, 4, datasize == 0); 
            if (datasize != (byte)vector_size)
            {
                Debug.LogError($"blast.ssmd.pop_fx_into -> datasize of data at index {dataindex} is different from what the caller expects. Data size: {datasize}, caller size: {vector_size}");
            }
#endif
            // we only allow equal vector sizes, compiler should use conversion ops 
            switch (vector_size)
            {
                case 1:
                case 2:
                case 3:
                case 4:

                    stack_offset = stack_offset - vector_size;

#if DEVELOPMENT_BUILD
                    // validate the stack
                    BlastVariableDataType type = BlastInterpretor.GetMetaDataType(metadata, (byte)(stack_offset));
                    int size = BlastInterpretor.GetMetaDataSize(metadata, (byte)(stack_offset));
                    if (size != vector_size || type != BlastVariableDataType.Numeric)
                    {
                        Debug.LogError($"blast.ssmd.pop_fx_into<T>(assignee = {dataindex}) -> stackdata mismatch, expecting numeric of size {vector_size}, found {type} of size {size} at stack offset {stack_offset}");
                        return;
                    }
#endif

                    for (int i = 0; i < ssmd_datacount; i++)
                    {
                        ((T*)(void*)&((float*)data[i])[dataindex])[0] = ((T*)(void*)&((float*)stack[i])[stack_offset])[0];
                    }
                    break;

#if DEVELOPMENT_BUILD
                default:
                    {
                        Debug.LogError($"blast.ssmd.pop_fx_into<T>(assignee = {dataindex}) -> vectorsize {vector_size} not supported");
                    }
                    break;
#endif                     

            }
        }


        /// <summary>
        /// pop a float[1|2|3|4] value form stack data or constant source and put it in destination 
        /// </summary>
        void pop_fx_into<T>(in int code_pointer, [NoAlias]T* destination) where T : unmanaged
        {
            int vector_size = sizeof(T) / 4;

            // we get either a pop instruction or an instruction to get a value 
            byte c = code[code_pointer];

            if (c >= BlastInterpretor.opt_id)
            {
#if DEVELOPMENT_BUILD
                BlastVariableDataType type = BlastInterpretor.GetMetaDataType(metadata, (byte)(c - BlastInterpretor.opt_id));
                int size = BlastInterpretor.GetMetaDataSize(metadata, (byte)(c - BlastInterpretor.opt_id));
                if (size != vector_size || type != BlastVariableDataType.Numeric)
                {
                    Debug.LogError($"blast.ssmd.pop_fx_into<T> -> data mismatch, expecting numeric of size {vector_size}, found {type} of size {size} at data offset {c - BlastInterpretor.opt_id}");
                    return;
                }
#endif
                // variable acccess
                for (int i = 0; i < ssmd_datacount; i++)
                {
                    destination[i] = ((T*)data[i])[c - BlastInterpretor.opt_id];
                }
            }
            else
            if (c == (byte)blast_operation.pop)
            {
#if DEVELOPMENT_BUILD
                BlastVariableDataType type = BlastInterpretor.GetMetaDataType(metadata, (byte)(stack_offset - 1));
                int size = BlastInterpretor.GetMetaDataSize(metadata, (byte)(stack_offset - 1));
                if (size != vector_size || type != BlastVariableDataType.Numeric)
                {
                    Debug.LogError($"blast.ssmd.pop_fx_into<T> -> stackdata mismatch, expecting numeric of size {vector_size}, found {type} of size {size} at stack offset {stack_offset}");
                    return;
                }
#endif
                // stack pop 
                stack_offset = stack_offset - vector_size;
                for (int i = 0; i < ssmd_datacount; i++)
                {
                    destination[i] = ((T*)(void*)&((float*)stack[i])[stack_offset])[0];
                }
            }
            else
            if (c >= BlastInterpretor.opt_value)
            {
                float constant = engine_ptr->constants[c];
                switch (vector_size)
                {
                    case 1: for (int i = 0; i < ssmd_datacount; i++) ((float*)(void*)destination)[i] = constant; break;
                    case 2: for (int i = 0; i < ssmd_datacount; i++) ((float2*)(void*)destination)[i].xy = constant; break;
                    case 3: for (int i = 0; i < ssmd_datacount; i++) ((float3*)(void*)destination)[i].xyz = constant; break;
                    case 0:
                    case 4: for (int i = 0; i < ssmd_datacount; i++) ((float4*)(void*)destination)[i].xyzw = constant; break;
                }
            }

            else
            {
                // error or.. constant by operation value.... 
#if DEVELOPMENT_BUILD
                Debug.LogError("blast.ssmd.pop_fx_into<T> -> select op by constant value is not supported");
#endif
            }
        }

        #endregion

        #region pop_fx_with_op_into_fx


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
        /// </summary>
        /// <param name="op">the operation to check</param>
        /// <returns>true if handled by the ssmd interpretor</returns>
        static public bool OperationIsSSMDHandled(blast_operation op)
        {
            return op >= blast_operation.add && op <= blast_operation.not_equals;
        }

        /// <summary>
        /// pop a float1 value from data/stack/constants and perform arithmetic op ( + - * / ) with buffer, writing the value back to m11
        /// </summary>
        /// <param name="code_pointer">current code pointer</param>
        /// <param name="buffer">buffer memory, may equal output</param>
        /// <param name="output">output memory</param>
        /// <param name="op">the operation to perform</param>
        void pop_f1_with_op_into_f1(in int code_pointer, [NoAlias]float* buffer, [NoAlias]float* output, blast_operation op)
        {
#if DEVELOPMENT_BUILD
            if (!OperationIsSSMDHandled(op))
            {
                Debug.LogError($"blast.pop_f1_op: -> unsupported operation supplied: {(byte)op}, only + - * / are supported");
                return; 
            }
#endif

            // we get either a pop instruction or an instruction to get a value 
            byte c = code[code_pointer];

            if (c >= BlastInterpretor.opt_id)
            {
#if DEVELOPMENT_BUILD
                BlastVariableDataType type = BlastInterpretor.GetMetaDataType(metadata, (byte)(c - BlastInterpretor.opt_id));
                int size = BlastInterpretor.GetMetaDataSize(metadata, (byte)(c - BlastInterpretor.opt_id));
                if (size != 1 || type != BlastVariableDataType.Numeric)
                {
                    Debug.LogError($"blast.pop_f1_op: data mismatch, expecting numeric of size 1, found {type} of size {size} at data offset {c - BlastInterpretor.opt_id} with operation: {op}");
                    return;
                }
#endif
                // variable acccess

                switch (op)
                {
                    case blast_operation.multiply:
                        for (int i = 0; i < ssmd_datacount; i++) output[i] = buffer[i] * ((float*)(data[i]))[c - BlastInterpretor.opt_id];
                        break;

                    case blast_operation.add:
                        for (int i = 0; i < ssmd_datacount; i++) output[i] = buffer[i] + ((float*)data[i])[c - BlastInterpretor.opt_id];
                        break;

                    case blast_operation.substract:
                        for (int i = 0; i < ssmd_datacount; i++) output[i] = buffer[i] - ((float*)data[i])[c - BlastInterpretor.opt_id];
                        break;

                    case blast_operation.divide:
                        for (int i = 0; i < ssmd_datacount; i++) output[i] = buffer[i] / ((float*)data[i])[c - BlastInterpretor.opt_id];
                        break;

                    case blast_operation.and:
                        for (int i = 0; i < ssmd_datacount; i++) output[i] = math.select(0, 1f, buffer[i] != 0 && ((float*)data[i])[c - BlastInterpretor.opt_id] != 0);
                        break;

#if DEVELOPMENT_BUILD
                    default:
                        {
                            Debug.LogError($"blast.pop_f1_with_op_into: -> codepointer {code_pointer} unsupported operation supplied: {(byte)op}");
                            return;
                        }

#endif
                }

            }
            else
            if (c == (byte)blast_operation.pop)
            {
#if DEVELOPMENT_BUILD
                BlastVariableDataType type = BlastInterpretor.GetMetaDataType(metadata, (byte)(stack_offset - 1));
                int size = BlastInterpretor.GetMetaDataSize(metadata, (byte)(stack_offset - 1));
                if (size != 1 || type != BlastVariableDataType.Numeric)
                {
                    Debug.LogError($"blast.pop_f1_op: stackdata mismatch, expecting numeric of size 1, found {type} of size {size} at stack offset {stack_offset} with operation: {op}");
                    return;
                }
#endif
                // stack pop 
                stack_offset = stack_offset - 1;

                switch (op)
                {
                    case blast_operation.multiply:
                        for (int i = 0; i < ssmd_datacount; i++) output[i] = buffer[i] * ((float*)stack[i])[stack_offset];
                        break;

                    case blast_operation.add:
                        for (int i = 0; i < ssmd_datacount; i++) output[i] = buffer[i] + ((float*)stack[i])[stack_offset];
                        break;

                    case blast_operation.substract:
                        for (int i = 0; i < ssmd_datacount; i++) output[i] = buffer[i] - ((float*)stack[i])[stack_offset];
                        break;

                    case blast_operation.divide:
                        for (int i = 0; i < ssmd_datacount; i++) output[i] = buffer[i] / ((float*)stack[i])[stack_offset];
                        break;

                    case blast_operation.and:
                        for (int i = 0; i < ssmd_datacount; i++) output[i] = math.select(0, 1f, buffer[i] != 0 && ((float*)stack[i])[stack_offset] != 0);
                        break;

#if DEVELOPMENT_BUILD
                    default:
                        {
                            Debug.LogError($"blast.pop_f1_with_op_into: -> codepointer {code_pointer} unsupported operation supplied: {op}");
                            return;
                        }

#endif
                }
            }
            else
            if (c >= BlastInterpretor.opt_value)
            {
                float constant = engine_ptr->constants[c];
                switch (op)
                {
                    case blast_operation.multiply:
                        for (int i = 0; i < ssmd_datacount; i++) output[i] = buffer[i] * constant;
                        break;

                    case blast_operation.add:
                        for (int i = 0; i < ssmd_datacount; i++) output[i] = buffer[i] + constant;
                        break;

                    case blast_operation.substract:
                        for (int i = 0; i < ssmd_datacount; i++) output[i] = buffer[i] - constant;
                        break;

                    case blast_operation.divide:
                        for (int i = 0; i < ssmd_datacount; i++) output[i] = buffer[i] / constant;
                        break;

                    case blast_operation.and:
                        for (int i = 0; i < ssmd_datacount; i++) output[i] = math.select(0, 1f, buffer[i] != 0 && constant != 0);
                        break;


#if DEVELOPMENT_BUILD
                    default:
                        {
                            Debug.LogError($"blast.pop_f1_with_op_into: -> codepointer {code_pointer} unsupported operation supplied: {(byte)op}");
                            return;
                        }

#endif
                }
            }
            else
            {
                // error or.. constant by operation value.... 
#if DEVELOPMENT_BUILD
                Debug.LogError("blast.pop_f1_op: select op by constant value is not supported at codepointer {code_pointer} with operation: {(byte)op}");
#endif
            }
        }

        /// <summary>
        /// pop a float2 value from data/stack/constants and perform arithmetic op ( + - * / ) with buffer, writing the value back to output
        /// </summary>
        void pop_f2_with_op_into_f2(in int code_pointer, float2* buffer, float2* output, blast_operation op)
        {
#if DEVELOPMENT_BUILD
            if (!OperationIsSSMDHandled(op))
            {
                Debug.LogError($"blast.pop_f2_with_op_into_f2: -> unsupported operation supplied: {(byte)op}, only + - * / are supported");
                return; 
            }
#endif

            // we get either a pop instruction or an instruction to get a value 
            byte c = code[code_pointer];

            if (c >= BlastInterpretor.opt_id)
            {
#if DEVELOPMENT_BUILD
                BlastVariableDataType type = BlastInterpretor.GetMetaDataType(metadata, (byte)(c - BlastInterpretor.opt_id));
                int size = BlastInterpretor.GetMetaDataSize(metadata, (byte)(c - BlastInterpretor.opt_id));
                if (size != 2 || type != BlastVariableDataType.Numeric)
                {
                    Debug.LogError($"blast.pop_f2_with_op_into_f2: data mismatch, expecting numeric of size 2, found {type} of size {size} at data offset {c - BlastInterpretor.opt_id} with operation: {op}");
                    return;
                }
#endif
                // variable acccess

                switch (op)
                {
                    case blast_operation.multiply:
                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            //
                            // whats faster? casting like this or nicely as float with 2 muls
                            //
                            output[i] = buffer[i] * ((float2*)(void*)&((float*)data[i])[c - BlastInterpretor.opt_id])[0];
                        }
                        break;

                    case blast_operation.add:
                        for (int i = 0; i < ssmd_datacount; i++) output[i] = buffer[i] + ((float2*)(void*)&((float*)data[i])[c - BlastInterpretor.opt_id])[0];
                        break;

                    case blast_operation.substract:
                        for (int i = 0; i < ssmd_datacount; i++) output[i] = buffer[i] - ((float2*)(void*)&((float*)data[i])[c - BlastInterpretor.opt_id])[0];
                        break;

                    case blast_operation.divide:
                        for (int i = 0; i < ssmd_datacount; i++) output[i] = buffer[i] / ((float2*)(void*)&((float*)data[i])[c - BlastInterpretor.opt_id])[0];
                        break;

#if DEVELOPMENT_BUILD
                    default:
                        {
                            Debug.LogError($"blast.pop_f2_with_op_into_f2: -> codepointer {code_pointer} unsupported operation supplied: {op}");
                            return;
                        }

#endif
                }

            }
            else
            if (c == (byte)blast_operation.pop)
            {
#if DEVELOPMENT_BUILD
                BlastVariableDataType type = BlastInterpretor.GetMetaDataType(metadata, (byte)(stack_offset - 1));
                int size = BlastInterpretor.GetMetaDataSize(metadata, (byte)(stack_offset - 1));
                if (size != 2 || type != BlastVariableDataType.Numeric)
                {
                    Debug.LogError($"blast.pop_f2_with_op_into_f2: stackdata mismatch, expecting numeric of size 2, found {type} of size {size} at stack offset {stack_offset} with operation: {op}");
                    return;
                }
#endif
                // stack pop 
                stack_offset = stack_offset - 2;

                switch (op)
                {
                    case blast_operation.multiply:
                        for (int i = 0; i < ssmd_datacount; i++) output[i] = buffer[i] * new float2(((float*)stack[i])[stack_offset], ((float*)stack[i])[stack_offset + 1]);
                        break;

                    case blast_operation.add:
                        for (int i = 0; i < ssmd_datacount; i++) output[i] = buffer[i] + new float2(((float*)stack[i])[stack_offset], ((float*)stack[i])[stack_offset + 1]);
                        break;

                    case blast_operation.substract:
                        for (int i = 0; i < ssmd_datacount; i++) output[i] = buffer[i] - new float2(((float*)stack[i])[stack_offset], ((float*)stack[i])[stack_offset + 1]);
                        break;

                    case blast_operation.divide:
                        for (int i = 0; i < ssmd_datacount; i++) output[i] = buffer[i] / new float2(((float*)stack[i])[stack_offset], ((float*)stack[i])[stack_offset + 1]);
                        break;
#if DEVELOPMENT_BUILD
                    default:
                        {
                            Debug.LogError($"blast.pop_f2_with_op_into_f2: -> codepointer {code_pointer} unsupported operation supplied: {op}");
                            return;
                        }

#endif
                }
            }
            else
            if (c >= BlastInterpretor.opt_value)
            {
                float constant = engine_ptr->constants[c];
                switch (op)
                {
                    case blast_operation.multiply:
                        for (int i = 0; i < ssmd_datacount; i++) output[i] = buffer[i] * constant;
                        break;

                    case blast_operation.add:
                        for (int i = 0; i < ssmd_datacount; i++) output[i] = buffer[i] + constant;
                        break;

                    case blast_operation.substract:
                        for (int i = 0; i < ssmd_datacount; i++) output[i] = buffer[i] - constant;
                        break;

                    case blast_operation.divide:
                        for (int i = 0; i < ssmd_datacount; i++) output[i] = buffer[i] / constant;
                        break;
#if DEVELOPMENT_BUILD
                    default:
                        {
                            Debug.LogError($"blast.pop_f2_with_op_into_f2: -> codepointer {code_pointer} unsupported operation supplied: {op}");
                            return;
                        }

#endif
                }
            }
            else
            {
                // error or.. constant by operation value.... 
#if DEVELOPMENT_BUILD
                Debug.LogError("blast.pop_f2_with_op_into_f2: select op by constant value is not supported at codepointer {code_pointer} with operation: {op}");
#endif
            }
        }


        /// <summary>
        /// pop a float3 value from data/stack/constants and perform arithmetic op ( + - * / ) with buffer, writing the value back to output
        /// </summary>
        void pop_f3_with_op_into_f3(in int code_pointer, float3* buffer, float3* output, blast_operation op)
        {
#if DEVELOPMENT_BUILD
            if (!OperationIsSSMDHandled(op))
            {
                Debug.LogError($"blast.pop_f3_with_op_into_f3: -> unsupported operation supplied: {op}, only + - * / are supported");
                return;
            }
#endif

            // we get either a pop instruction or an instruction to get a value 
            byte c = code[code_pointer];

            if (c >= BlastInterpretor.opt_id)
            {
#if DEVELOPMENT_BUILD
                BlastVariableDataType type = BlastInterpretor.GetMetaDataType(metadata, (byte)(c - BlastInterpretor.opt_id));
                int size = BlastInterpretor.GetMetaDataSize(metadata, (byte)(c - BlastInterpretor.opt_id));
                if (size != 3 || type != BlastVariableDataType.Numeric)
                {
                    Debug.LogError($"blast.pop_f3_with_op_into_f3: data mismatch, expecting numeric of size 3, found {type} of size {size} at data offset {c - BlastInterpretor.opt_id} with operation: {op}");
                    return;
                }
#endif
                // variable acccess

                switch (op)
                {
                    case blast_operation.multiply:
                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            //
                            // whats faster? casting like this or nicely as float with 2 muls
                            //
                            output[i] = buffer[i] * ((float3*)(void*)&((float*)data[i])[c - BlastInterpretor.opt_id])[0];
                        }
                        break;

                    case blast_operation.add:
                        for (int i = 0; i < ssmd_datacount; i++) output[i] = buffer[i] + ((float3*)(void*)&((float*)data[i])[c - BlastInterpretor.opt_id])[0];
                        break;

                    case blast_operation.substract:
                        for (int i = 0; i < ssmd_datacount; i++) output[i] = buffer[i] - ((float3*)(void*)&((float*)data[i])[c - BlastInterpretor.opt_id])[0];
                        break;

                    case blast_operation.divide:
                        for (int i = 0; i < ssmd_datacount; i++) output[i] = buffer[i] / ((float3*)(void*)&((float*)data[i])[c - BlastInterpretor.opt_id])[0];
                        break;
#if DEVELOPMENT_BUILD
                    default:
                        {
                            Debug.LogError($"blast.pop_f3_with_op_into_f3: -> codepointer {code_pointer} unsupported operation supplied: {op}");
                            return;
                        }

#endif
                }

            }
            else
            if (c == (byte)blast_operation.pop)
            {
#if DEVELOPMENT_BUILD
                BlastVariableDataType type = BlastInterpretor.GetMetaDataType(metadata, (byte)(stack_offset - 1));
                int size = BlastInterpretor.GetMetaDataSize(metadata, (byte)(stack_offset - 1));
                if (size != 3 || type != BlastVariableDataType.Numeric)
                {
                    Debug.LogError($"blast.pop_f3_with_op_into_f3: stackdata mismatch, expecting numeric of size 3, found {type} of size {size} at stack offset {stack_offset} with operation: {op}");
                    return;
                }
#endif
                // stack pop 
                stack_offset = stack_offset - 3;

                switch (op)
                {
                    case blast_operation.multiply:
                        for (int i = 0; i < ssmd_datacount; i++) output[i] = buffer[i] * new float3(((float*)stack[i])[stack_offset], ((float*)stack[i])[stack_offset + 1], ((float*)stack[i])[stack_offset + 2]);
                        break;

                    case blast_operation.add:
                        for (int i = 0; i < ssmd_datacount; i++) output[i] = buffer[i] + new float3(((float*)stack[i])[stack_offset], ((float*)stack[i])[stack_offset + 1], ((float*)stack[i])[stack_offset + 2]);
                        break;

                    case blast_operation.substract:
                        for (int i = 0; i < ssmd_datacount; i++) output[i] = buffer[i] - new float3(((float*)stack[i])[stack_offset], ((float*)stack[i])[stack_offset + 1], ((float*)stack[i])[stack_offset + 2]);
                        break;

                    case blast_operation.divide:
                        for (int i = 0; i < ssmd_datacount; i++) output[i] = buffer[i] / new float3(((float*)stack[i])[stack_offset], ((float*)stack[i])[stack_offset + 1], ((float*)stack[i])[stack_offset + 2]);
                        break;
#if DEVELOPMENT_BUILD
                    default:
                        {
                            Debug.LogError($"blast.pop_f3_with_op_into_f3: -> codepointer {code_pointer} unsupported operation supplied: {op}");
                            return;
                        }

#endif
                }
            }
            else
            if (c >= BlastInterpretor.opt_value)
            {
                float constant = engine_ptr->constants[c];
                switch (op)
                {
                    case blast_operation.multiply:
                        for (int i = 0; i < ssmd_datacount; i++) output[i] = buffer[i] * constant;
                        break;

                    case blast_operation.add:
                        for (int i = 0; i < ssmd_datacount; i++) output[i] = buffer[i] + constant;
                        break;

                    case blast_operation.substract:
                        for (int i = 0; i < ssmd_datacount; i++) output[i] = buffer[i] - constant;
                        break;

                    case blast_operation.divide:
                        for (int i = 0; i < ssmd_datacount; i++) output[i] = buffer[i] / constant;
                        break;
#if DEVELOPMENT_BUILD
                    default:
                        {
                            Debug.LogError($"blast.pop_f3_with_op_into_f3: -> codepointer {code_pointer} unsupported operation supplied: {op}");
                            return;
                        }

#endif
                }
            }
            else
            {
                // error or.. constant by operation value.... 
#if DEVELOPMENT_BUILD
                Debug.LogError("blast.pop_f3_with_op_into_f3: select op by constant value is not supported at codepointer {code_pointer} with operation: {op}");
#endif
            }
        }



        /// <summary>
        /// pop a float4 value from data/stack/constants and perform arithmetic op ( + - * / ) with buffer, writing the value back to output
        /// </summary>
        void pop_f4_with_op_into_f4(in int code_pointer, float4* buffer, float4* output, blast_operation op)
        {
#if DEVELOPMENT_BUILD
            if (!OperationIsSSMDHandled(op))
            {
                Debug.LogError($"blast.pop_f4_with_op_into_f4: -> unsupported operation supplied: {op}, only + - * / are supported");
                return;
            }
#endif

            // we get either a pop instruction or an instruction to get a value 
            byte c = code[code_pointer];

            if (c >= BlastInterpretor.opt_id)
            {
#if DEVELOPMENT_BUILD
                BlastVariableDataType type = BlastInterpretor.GetMetaDataType(metadata, (byte)(c - BlastInterpretor.opt_id));
                int size = BlastInterpretor.GetMetaDataSize(metadata, (byte)(c - BlastInterpretor.opt_id));
                if ((size != 4 && size != 0)  || type != BlastVariableDataType.Numeric)
                {
                    Debug.LogError($"blast.pop_f4_with_op_into_f4: data mismatch, expecting numeric of size 4, found {type} of size {size} at data offset {c - BlastInterpretor.opt_id} with operation: {op}");
                    return;
                }
#endif
                // variable acccess

                switch (op)
                {
                    case blast_operation.multiply:
                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            //
                            // whats faster? casting like this or nicely as float with 2 muls
                            //
                            output[i] = buffer[i] * ((float4*)(void*)&((float*)data[i])[c - BlastInterpretor.opt_id])[0];
                        }
                        break;

                    case blast_operation.add:
                        for (int i = 0; i < ssmd_datacount; i++) output[i] = buffer[i] + ((float4*)(void*)&((float*)data[i])[c - BlastInterpretor.opt_id])[0];
                        break;

                    case blast_operation.substract:
                        for (int i = 0; i < ssmd_datacount; i++) output[i] = buffer[i] - ((float4*)(void*)&((float*)data[i])[c - BlastInterpretor.opt_id])[0];
                        break;

                    case blast_operation.divide:
                        for (int i = 0; i < ssmd_datacount; i++) output[i] = buffer[i] / ((float4*)(void*)&((float*)data[i])[c - BlastInterpretor.opt_id])[0];
                        break;
#if DEVELOPMENT_BUILD
                    default:
                        {
                            Debug.LogError($"blast.pop_f4_with_op_into_f4: -> codepointer {code_pointer} unsupported operation supplied: {op}");
                            return;
                        }

#endif
                }

            }
            else
            if (c == (byte)blast_operation.pop)
            {
#if DEVELOPMENT_BUILD
                BlastVariableDataType type = BlastInterpretor.GetMetaDataType(metadata, (byte)(stack_offset - 1));
                int size = BlastInterpretor.GetMetaDataSize(metadata, (byte)(stack_offset - 1));
                if (size != 4 || type != BlastVariableDataType.Numeric)
                {
                    Debug.LogError($"blast.pop_f4_with_op_into_f4: stackdata mismatch, expecting numeric of size 4, found {type} of size {size} at stack offset {stack_offset} with operation: {op}");
                    return;
                }
#endif
                // stack pop 
                stack_offset = stack_offset - 4;

                switch (op)
                {
                    case blast_operation.multiply:
                        for (int i = 0; i < ssmd_datacount; i++) output[i] = buffer[i] * new float4(((float*)stack[i])[stack_offset], ((float*)stack[i])[stack_offset + 1], ((float*)stack[i])[stack_offset + 2], ((float*)stack[i])[stack_offset + 3]);
                        break;

                    case blast_operation.add:
                        for (int i = 0; i < ssmd_datacount; i++) output[i] = buffer[i] + new float4(((float*)stack[i])[stack_offset], ((float*)stack[i])[stack_offset + 1], ((float*)stack[i])[stack_offset + 2], ((float*)stack[i])[stack_offset + 3]);
                        break;

                    case blast_operation.substract:
                        for (int i = 0; i < ssmd_datacount; i++) output[i] = buffer[i] - new float4(((float*)stack[i])[stack_offset], ((float*)stack[i])[stack_offset + 1], ((float*)stack[i])[stack_offset + 2], ((float*)stack[i])[stack_offset + 3]);
                        break;

                    case blast_operation.divide:
                        for (int i = 0; i < ssmd_datacount; i++) output[i] = buffer[i] / new float4(((float*)stack[i])[stack_offset], ((float*)stack[i])[stack_offset + 1], ((float*)stack[i])[stack_offset + 2], ((float*)stack[i])[stack_offset + 3]);
                        break;
#if DEVELOPMENT_BUILD
                    default:
                        {
                            Debug.LogError($"blast.pop_f4_with_op_into_f4: -> codepointer {code_pointer} unsupported operation supplied: {op}");
                            return;
                        }

#endif
                }
            }
            else
            if (c >= BlastInterpretor.opt_value)
            {
                float constant = engine_ptr->constants[c];
                switch (op)
                {
                    case blast_operation.multiply:
                        for (int i = 0; i < ssmd_datacount; i++) output[i] = buffer[i] * constant;
                        break;

                    case blast_operation.add:
                        for (int i = 0; i < ssmd_datacount; i++) output[i] = buffer[i] + constant;
                        break;

                    case blast_operation.substract:
                        for (int i = 0; i < ssmd_datacount; i++) output[i] = buffer[i] - constant;
                        break;

                    case blast_operation.divide:
                        for (int i = 0; i < ssmd_datacount; i++) output[i] = buffer[i] / constant;
                        break;
#if DEVELOPMENT_BUILD
                    default:
                        {
                            Debug.LogError($"blast.pop_f4_with_op_into_f4: -> codepointer {code_pointer} unsupported operation supplied: {op}");
                            return;
                        }

#endif
                }
            }
            else
            {
                // error or.. constant by operation value.... 
#if DEVELOPMENT_BUILD
                Debug.LogError("blast.pop_f3_with_op_into_f3: select op by constant value is not supported at codepointer {code_pointer} with operation: {op}");
#endif
            }
        }




        #endregion

        #region pop_fx_with_op_into_f4
        /// <summary>
        /// pop a float1 value from data/stack/constants and perform arithmetic op ( + - * / ) with buffer, writing the value back to m11
        /// </summary>
        /// <param name="code_pointer"></param>
        /// <param name="buffer">temp buffer that may equal output</param>
        /// <param name="output">output buffer that may equal temp buffer</param>
        /// <param name="op">operation to handle</param>
        void pop_f1_with_op_into_f4(in int code_pointer, float* buffer, float4* output, blast_operation op)
        {
#if DEVELOPMENT_BUILD
            if (op != blast_operation.add && op != blast_operation.multiply && op != blast_operation.substract && op != blast_operation.divide)
            {
                Debug.LogError($"blast.pop_f1_op: -> unsupported operation supplied: {op}, only + - * / are supported");
                return;
            }
#endif

            // we get either a pop instruction or an instruction to get a value 
            byte c = code[code_pointer];

            if (c >= BlastInterpretor.opt_id)
            {
#if DEVELOPMENT_BUILD
                BlastVariableDataType type = BlastInterpretor.GetMetaDataType(metadata, (byte)(c - BlastInterpretor.opt_id));
                int size = BlastInterpretor.GetMetaDataSize(metadata, (byte)(c - BlastInterpretor.opt_id));
                if (size != 1 || type != BlastVariableDataType.Numeric)
                {
                    Debug.LogError($"blast.pop_f1_op: data mismatch, expecting numeric of size 1, found {type} of size {size} at data offset {c - BlastInterpretor.opt_id} with operation: {op}");
                    return;
                }
#endif
                // variable acccess

                switch (op)
                {
                    case blast_operation.multiply:
                        for (int i = 0; i < ssmd_datacount; i++) output[i].x = buffer[i] * ((float*)data[i])[c - BlastInterpretor.opt_id];
                        break;

                    case blast_operation.add:
                        for (int i = 0; i < ssmd_datacount; i++) output[i].x = buffer[i] + ((float*)data[i])[c - BlastInterpretor.opt_id];
                        break;

                    case blast_operation.substract:
                        for (int i = 0; i < ssmd_datacount; i++) output[i].x = buffer[i] - ((float*)data[i])[c - BlastInterpretor.opt_id];
                        break;

                    case blast_operation.divide:
                        for (int i = 0; i < ssmd_datacount; i++) output[i].x = buffer[i] / ((float*)data[i])[c - BlastInterpretor.opt_id];
                        break;
                }

            }
            else
            if (c == (byte)blast_operation.pop)
            {
#if DEVELOPMENT_BUILD
                BlastVariableDataType type = BlastInterpretor.GetMetaDataType(metadata, (byte)(stack_offset - 1));
                int size = BlastInterpretor.GetMetaDataSize(metadata, (byte)(stack_offset - 1));
                if (size != 1 || type != BlastVariableDataType.Numeric)
                {
                    Debug.LogError($"blast.pop_f1_op: stackdata mismatch, expecting numeric of size 1, found {type} of size {size} at stack offset {stack_offset} with operation: {op}");
                    return;
                }
#endif
                // stack pop 
                stack_offset = stack_offset - 1;

                switch (op)
                {
                    case blast_operation.multiply:
                        for (int i = 0; i < ssmd_datacount; i++) output[i].x = buffer[i] * ((float*)stack[i])[stack_offset];
                        break;

                    case blast_operation.add:
                        for (int i = 0; i < ssmd_datacount; i++) output[i].x = buffer[i] + ((float*)stack[i])[stack_offset];
                        break;

                    case blast_operation.substract:
                        for (int i = 0; i < ssmd_datacount; i++) output[i].x = buffer[i] - ((float*)stack[i])[stack_offset];
                        break;

                    case blast_operation.divide:
                        for (int i = 0; i < ssmd_datacount; i++) output[i].x = buffer[i] / ((float*)stack[i])[stack_offset];
                        break;
                }
            }
            else
            if (c >= BlastInterpretor.opt_value)
            {
                float constant = engine_ptr->constants[c];
                switch (op)
                {
                    case blast_operation.multiply:
                        for (int i = 0; i < ssmd_datacount; i++) output[i].x = buffer[i] * constant;
                        break;

                    case blast_operation.add:
                        for (int i = 0; i < ssmd_datacount; i++) output[i].x = buffer[i] + constant;
                        break;

                    case blast_operation.substract:
                        for (int i = 0; i < ssmd_datacount; i++) output[i].x = buffer[i] - constant;
                        break;

                    case blast_operation.divide:
                        for (int i = 0; i < ssmd_datacount; i++) output[i].x = buffer[i] / constant;
                        break;
                }
            }
            else
            {
                // error or.. constant by operation value.... 
#if DEVELOPMENT_BUILD
                Debug.LogError("blast.pop_f1_op: select op by constant value is not supported at codepointer {code_pointer} with operation: {op}");
#endif
            }
        }

        /// <summary>
        /// pop a float2 value from data/stack/constants and perform arithmetic op ( + - * / ) with buffer, writing the value back to m11
        /// </summary>
        /// <param name="code_pointer">current codepointer</param>
        /// <param name="buffer">buffer that may equal output buffer</param>
        /// <param name="output">output buffer</param>
        /// <param name="op">operation to execute on value </param>
        void pop_f2_with_op_into_f4(in int code_pointer, float2* buffer, float4* output, blast_operation op)
        {
#if DEVELOPMENT_BUILD
            if (op != blast_operation.add && op != blast_operation.multiply && op != blast_operation.substract && op != blast_operation.divide)
            {
                Debug.LogError($"blast.pop_f1_op: -> unsupported operation supplied: {op}, only + - * / are supported");
                return;
            }
#endif

            // we get either a pop instruction or an instruction to get a value 
            byte c = code[code_pointer];

            if (c >= BlastInterpretor.opt_id)
            {
#if DEVELOPMENT_BUILD
                BlastVariableDataType type = BlastInterpretor.GetMetaDataType(metadata, (byte)(c - BlastInterpretor.opt_id));
                int size = BlastInterpretor.GetMetaDataSize(metadata, (byte)(c - BlastInterpretor.opt_id));
                if (size != 2 || type != BlastVariableDataType.Numeric)
                {
                    Debug.LogError($"blast.pop_f2_with_op_into_f4: data mismatch, expecting numeric of size 2, found {type} of size {size} at data offset {c - BlastInterpretor.opt_id} with operation: {op}");
                    return;
                }
#endif
                // variable acccess

                switch (op)
                {
                    case blast_operation.multiply:
                        for (int i = 0; i < ssmd_datacount; i++) output[i].xy = buffer[i] * ((float2*)data[i])[c - BlastInterpretor.opt_id];
                        break;

                    case blast_operation.add:
                        for (int i = 0; i < ssmd_datacount; i++) output[i].xy = buffer[i] + ((float2*)data[i])[c - BlastInterpretor.opt_id];
                        break;

                    case blast_operation.substract:
                        for (int i = 0; i < ssmd_datacount; i++) output[i].xy = buffer[i] - ((float2*)data[i])[c - BlastInterpretor.opt_id];
                        break;

                    case blast_operation.divide:
                        for (int i = 0; i < ssmd_datacount; i++) output[i].xy = buffer[i] / ((float2*)data[i])[c - BlastInterpretor.opt_id];
                        break;
                }

            }
            else
            if (c == (byte)blast_operation.pop)
            {
#if DEVELOPMENT_BUILD
                BlastVariableDataType type = BlastInterpretor.GetMetaDataType(metadata, (byte)(stack_offset - 1));
                int size = BlastInterpretor.GetMetaDataSize(metadata, (byte)(stack_offset - 1));
                if (size != 2 || type != BlastVariableDataType.Numeric)
                {
                    Debug.LogError($"blast.pop_f2_with_op_into_f4: stackdata mismatch, expecting numeric of size 2, found {type} of size {size} at stack offset {stack_offset} with operation: {op}");
                    return;
                }
#endif
                // stack pop 
                stack_offset = stack_offset - 2;

                switch (op)
                {
                    case blast_operation.multiply:
                        for (int i = 0; i < ssmd_datacount; i++) output[i].xy = buffer[i] * ((float2*)(void*)&((float*)stack[i])[stack_offset])[0];
                        break;

                    case blast_operation.add:
                        for (int i = 0; i < ssmd_datacount; i++) output[i].xy = buffer[i] + ((float2*)(void*)&((float*)data[i])[stack_offset])[0];
                        break;

                    case blast_operation.substract:
                        for (int i = 0; i < ssmd_datacount; i++) output[i].xy = buffer[i] - ((float2*)(void*)&((float*)data[i])[stack_offset])[0];
                        break;

                    case blast_operation.divide:
                        for (int i = 0; i < ssmd_datacount; i++) output[i].xy = buffer[i] / ((float2*)(void*)&((float*)data[i])[stack_offset])[0];
                        break;
                }
            }
            else
            if (c >= BlastInterpretor.opt_value)
            {
                float constant = engine_ptr->constants[c];
                switch (op)
                {
                    case blast_operation.multiply:
                        for (int i = 0; i < ssmd_datacount; i++) output[i].xy = buffer[i] * new float2(constant, constant);
                        break;

                    case blast_operation.add:
                        for (int i = 0; i < ssmd_datacount; i++) output[i].xy = buffer[i] + new float2(constant, constant);
                        break;

                    case blast_operation.substract:
                        for (int i = 0; i < ssmd_datacount; i++) output[i].xy = buffer[i] - new float2(constant, constant);
                        break;

                    case blast_operation.divide:
                        for (int i = 0; i < ssmd_datacount; i++) output[i].xy = buffer[i] / new float2(constant, constant);
                        break;
                }
            }
            else
            {
                // error or.. constant by operation value.... 
#if DEVELOPMENT_BUILD
                Debug.LogError("blast.pop_f2_with_op_into_f4: select op by constant value is not supported at codepointer {code_pointer} with operation: {op}");
#endif
            }
        }

        /// <summary>
        /// pop a float2 value from data/stack/constants and perform arithmetic op ( + - * / ) with buffer, writing the value back to m11
        /// </summary>
        /// <param name="code_pointer"></param>
        /// <param name="buffer"></param>
        /// <param name="output"></param>
        /// <param name="op"></param>
        void pop_f3_with_op_into_f4(in int code_pointer, float3* buffer, float4* output, blast_operation op)
        {
#if DEVELOPMENT_BUILD
            if (!OperationIsSSMDHandled(op))
            {
                Debug.LogError($"blast.pop_f3_with_op_into_f4: -> unsupported operation supplied: {op}, only + - * / are supported");
                return;
            }
#endif

            // we get either a pop instruction or an instruction to get a value 
            byte c = code[code_pointer];

            if (c >= BlastInterpretor.opt_id)
            {
#if DEVELOPMENT_BUILD
                BlastVariableDataType type = BlastInterpretor.GetMetaDataType(metadata, (byte)(c - BlastInterpretor.opt_id));
                int size = BlastInterpretor.GetMetaDataSize(metadata, (byte)(c - BlastInterpretor.opt_id));
                if (size != 3 || type != BlastVariableDataType.Numeric)
                {
                    Debug.LogError($"blast.pop_f3_with_op_into_f4: data mismatch, expecting numeric of size 3, found {type} of size {size} at data offset {c - BlastInterpretor.opt_id} with operation: {op}");
                    return;
                }
#endif
                // variable acccess
                switch (op)
                {
                    case blast_operation.multiply:
                        for (int i = 0; i < ssmd_datacount; i++) output[i].xyz = buffer[i] * ((float3*)data[i])[c - BlastInterpretor.opt_id];
                        break;

                    case blast_operation.add:
                        for (int i = 0; i < ssmd_datacount; i++) output[i].xyz = buffer[i] + ((float3*)data[i])[c - BlastInterpretor.opt_id];
                        break;

                    case blast_operation.substract:
                        for (int i = 0; i < ssmd_datacount; i++) output[i].xyz = buffer[i] - ((float3*)data[i])[c - BlastInterpretor.opt_id];
                        break;

                    case blast_operation.divide:
                        for (int i = 0; i < ssmd_datacount; i++) output[i].xyz = buffer[i] / ((float3*)data[i])[c - BlastInterpretor.opt_id];
                        break;
                }

            }
            else
            if (c == (byte)blast_operation.pop)
            {
#if DEVELOPMENT_BUILD
                BlastVariableDataType type = BlastInterpretor.GetMetaDataType(metadata, (byte)(stack_offset - 1));
                int size = BlastInterpretor.GetMetaDataSize(metadata, (byte)(stack_offset - 1));
                if (size != 3 || type != BlastVariableDataType.Numeric)
                {
                    Debug.LogError($"blast.pop_f3_with_op_into_f4: stackdata mismatch, expecting numeric of size 3, found {type} of size {size} at stack offset {stack_offset} with operation: {op}");
                    return;
                }
#endif
                // stack pop 
                stack_offset = stack_offset - 3;

                switch (op)
                {
                    case blast_operation.multiply:
                        for (int i = 0; i < ssmd_datacount; i++) output[i].xyz = buffer[i] * ((float3*)(void*)&((float*)stack[i])[stack_offset])[0];
                        break;

                    case blast_operation.add:
                        for (int i = 0; i < ssmd_datacount; i++) output[i].xyz = buffer[i] + ((float3*)(void*)&((float*)data[i])[stack_offset])[0];
                        break;

                    case blast_operation.substract:
                        for (int i = 0; i < ssmd_datacount; i++) output[i].xyz = buffer[i] - ((float3*)(void*)&((float*)data[i])[stack_offset])[0];
                        break;

                    case blast_operation.divide:
                        for (int i = 0; i < ssmd_datacount; i++) output[i].xyz = buffer[i] / ((float3*)(void*)&((float*)data[i])[stack_offset])[0];
                        break;
                }
            }
            else
            if (c >= BlastInterpretor.opt_value)
            {
                float constant = engine_ptr->constants[c];
                float3 cval = new float3(constant, constant, constant);
                switch (op)
                {
                    case blast_operation.multiply:
                        for (int i = 0; i < ssmd_datacount; i++) output[i].xyz = buffer[i] * cval;
                        break;

                    case blast_operation.add:
                        for (int i = 0; i < ssmd_datacount; i++) output[i].xyz = buffer[i] + cval;
                        break;

                    case blast_operation.substract:
                        for (int i = 0; i < ssmd_datacount; i++) output[i].xyz = buffer[i] - cval;
                        break;

                    case blast_operation.divide:
                        for (int i = 0; i < ssmd_datacount; i++) output[i].xyz = buffer[i] / cval;
                        break;
                }
            }
            else
            {
                // error or.. constant by operation value.... 
#if DEVELOPMENT_BUILD
                Debug.LogError("blast.pop_f3_with_op_into_f4: select op by constant value is not supported at codepointer {code_pointer} with operation: {op}");
#endif
            }
        }


        #endregion

        #region set_data_from_register_fx


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        T* get_data_pointer<T>(in int ssmd_index, int index) where T : unmanaged
        {
            return (T*)(void*)&((float*)data[ssmd_index])[index];
        }

        /// <summary>
        /// set a float1 data location from register location  
        /// </summary>
        /// <param name="index">data index</param>
        void set_data_from_register_f1(in int index)
        {
            for (int i = 0; i < ssmd_datacount; i++)
            {
                ((float*)data[i])[index] = register[i].x;
            }
        }

        /// <summary>
        /// set a float2 data location from register location  
        /// </summary>
        /// <param name="index">data index</param>
        void set_data_from_register_f2(in int index)
        {
            for (int i = 0; i < ssmd_datacount; i++)
            {
                float* p = (float*)(data[i]);
                p[index] = register[i].x;
                p[index + 1] = register[i].y;
            }
        }

        /// <summary>
        /// set a float3 data location from register location  
        /// </summary>
        /// <param name="index">data index</param>
        void set_data_from_register_f3(in int index)
        {
            for (int i = 0; i < ssmd_datacount; i++)
            {
                float* p = (float*)(data[i]);
                p[index] = register[i].x;
                p[index + 1] = register[i].y;
                p[index + 2] = register[i].z;
            }
        }
        /// <summary>
        /// set a float4 data location from register location  
        /// </summary>
        /// <param name="index">data index</param>
        void set_data_from_register_f4(in int index)
        {
            for (int i = 0; i < ssmd_datacount; i++)
            {
                float* p = (float*)(data[i]);
                p[index] = register[i].x;
                p[index + 1] = register[i].y;
                p[index + 2] = register[i].z;
                p[index + 3] = register[i].w;
            }
        }
        #endregion

        /// <summary>
        /// read codebyte, determine data location, pop data and push it on the stack
        /// </summary>
        void push_pop_f(in int code_pointer, in int vector_size)
        {
            // we get either a pop instruction or an instruction to get a value 
            byte c = code[code_pointer];

            if (c >= BlastInterpretor.opt_id)
            {
#if DEVELOPMENT_BUILD
                BlastVariableDataType type = BlastInterpretor.GetMetaDataType(metadata, (byte)(c - BlastInterpretor.opt_id));
                int size = BlastInterpretor.GetMetaDataSize(metadata, (byte)(c - BlastInterpretor.opt_id));
                if (size != vector_size || type != BlastVariableDataType.Numeric)
                {
                    Debug.LogError($"blast.stack, push_pop_f1 -> data mismatch, expecting numeric of size {vector_size}, found {type} of size {size} at data offset {c - BlastInterpretor.opt_id}");
                    return;
                }
#endif
                // data comes from variable acccess, update each according to vectorsize 
                switch (vector_size)
                {
                    case 0:
                    case 4:
                        BlastInterpretor.SetMetaData(metadata, BlastVariableDataType.Numeric, 4, (byte)stack_offset);
                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            float* fdata = &((float*)data[i])[c - BlastInterpretor.opt_id];
                            float* fstack = &((float*)stack[i])[stack_offset];

                            ((float4*)fstack)[0] = ((float4*)(void*)fdata)[0];
                        }
                        stack_offset += 4;
                        break;
                    case 1:
                        BlastInterpretor.SetMetaData(metadata, BlastVariableDataType.Numeric, 1, (byte)stack_offset);
                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            float* fdata = &((float*)data[i])[c - BlastInterpretor.opt_id];
                            ((float*)stack[i])[stack_offset] = fdata[0];
                        }
                        stack_offset += 1;
                        break;
                    case 2:
                        BlastInterpretor.SetMetaData(metadata, BlastVariableDataType.Numeric, 2, (byte)stack_offset);
                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            float* fdata = &((float*)data[i])[c - BlastInterpretor.opt_id];
                            float* fstack = &((float*)stack[i])[stack_offset];

                            ((float2*)fstack)[0] = ((float2*)(void*)fdata)[0];
                        }
                        stack_offset += 2;
                        break;

                    case 3:
                        BlastInterpretor.SetMetaData(metadata, BlastVariableDataType.Numeric, 3, (byte)stack_offset);
                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            float* fdata = &((float*)data[i])[c - BlastInterpretor.opt_id];
                            float* fstack = &((float*)stack[i])[stack_offset];

                            ((float3*)fstack)[0] = ((float3*)(void*)fdata)[0];
                        }
                        stack_offset += 3;
                        break;
                }
            }
            else
            if (c == (byte)blast_operation.pop)
            {
#if DEVELOPMENT_BUILD
                BlastVariableDataType type = BlastInterpretor.GetMetaDataType(metadata, (byte)(stack_offset - 1));
                int size = BlastInterpretor.GetMetaDataSize(metadata, (byte)(stack_offset - 1));
                if (size != 1 || type != BlastVariableDataType.Numeric)
                {
                    Debug.LogError($"blast.stack, push_pop_f1 -> stackdata mismatch, expecting numeric of size 1, found {type} of size {size} at stack offset {stack_offset}");
                    return;
                }
#endif
                // stack pop, vector size is known, but its also pushed back again at same resulting location and bitorders.. 
                //
                // -> in this case we do nothing
                //
            }
            else
            if (c >= BlastInterpretor.opt_value)
            {
                float constant = engine_ptr->constants[c];

                switch (vector_size)
                {
                    case 0:
                    case 4:
                        BlastInterpretor.SetMetaData(metadata, BlastVariableDataType.Numeric, 4, (byte)stack_offset);
                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            ((float*)stack[i])[stack_offset] = constant;
                            ((float*)stack[i])[stack_offset + 1] = constant;
                            ((float*)stack[i])[stack_offset + 2] = constant;
                            ((float*)stack[i])[stack_offset + 3] = constant;
                        }
                        stack_offset += 4;
                        break;
                    case 1:
                        BlastInterpretor.SetMetaData(metadata, BlastVariableDataType.Numeric, 1, (byte)stack_offset);
                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            ((float*)stack[i])[stack_offset] = constant;
                        }
                        stack_offset += 1;
                        break;
                    case 2:
                        BlastInterpretor.SetMetaData(metadata, BlastVariableDataType.Numeric, 2, (byte)stack_offset);
                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            ((float*)stack[i])[stack_offset] = constant;
                            ((float*)stack[i])[stack_offset + 1] = constant;
                        }
                        stack_offset += 2;
                        break;

                    case 3:
                        BlastInterpretor.SetMetaData(metadata, BlastVariableDataType.Numeric, 3, (byte)stack_offset);
                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            ((float*)stack[i])[stack_offset] = constant;
                            ((float*)stack[i])[stack_offset + 1] = constant;
                            ((float*)stack[i])[stack_offset + 2] = constant;
                        }
                        stack_offset += 3;
                        break;
                }
            }
            else
            {
#if DEVELOPMENT_BUILD
                Debug.LogError("push_pop_f -> select op by constant value is not supported");
#endif
            }
        }

        /// <summary>
        /// set register[n].x from data 
        /// </summary>
        void set_register_from_data_f1(float* f1)
        {
            for (int i = 0; i < ssmd_datacount; i++)
            {
                register[i].x = f1[i];
            }
        }


        /// <summary>
        /// set register[n].xyzw from data 
        /// </summary>
        void set_register_from_data_f4(float4* f4)
        {
            UnsafeUtils.MemCpy(register, f4, ssmd_datacount * sizeof(float4));
        }

        #endregion

        #region Operations Handlers 

        /// <summary>
        /// handle operation a.x and b.x
        /// </summary>
        void handle_op_f1_f1(in blast_operation op, in byte current, in byte previous, [NoAlias]float* a, [NoAlias]float4* b, ref bool last_is_op_or_first, ref byte vector_size)
        {
            last_is_op_or_first = false;
            switch (op)
            {
                default: return;
                case blast_operation.nop: for (int i = 0; i < ssmd_datacount; i++) a[i] = b[i].x; break;

                case blast_operation.add: for (int i = 0; i < ssmd_datacount; i++) a[i] = a[i] + b[i].x; return;
                case blast_operation.substract: for (int i = 0; i < ssmd_datacount; i++) a[i] = a[i] - b[i].x; return;
                case blast_operation.multiply: for (int i = 0; i < ssmd_datacount; i++) a[i] = a[i] * b[i].x; return;
                case blast_operation.divide: for (int i = 0; i < ssmd_datacount; i++) a[i] = a[i] / b[i].x; return;

                case blast_operation.and: for (int i = 0; i < ssmd_datacount; i++) a[i] = math.select(0, 1, a[i] != 0 && b[i].x != 0); return;
                case blast_operation.or: for (int i = 0; i < ssmd_datacount; i++) a[i] = math.select(0, 1, a[i] != 0 || b[i].x != 0); return;
                case blast_operation.xor: for (int i = 0; i < ssmd_datacount; i++) a[i] = math.select(0, 1, a[i] != 0 ^ b[i].x != 0); return;

                case blast_operation.greater: for (int i = 0; i < ssmd_datacount; i++) a[i] = math.select(0f, 1f, a[i] > b[i].x); return;
                case blast_operation.smaller: for (int i = 0; i < ssmd_datacount; i++) a[i] = math.select(0f, 1f, a[i] < b[i].x); return;
                case blast_operation.smaller_equals: for (int i = 0; i < ssmd_datacount; i++) a[i] = math.select(0f, 1f, a[i] <= b[i].x); return;
                case blast_operation.greater_equals: for (int i = 0; i < ssmd_datacount; i++) a[i] = math.select(0f, 1f, a[i] >= b[i].x); return;
                case blast_operation.equals: for (int i = 0; i < ssmd_datacount; i++) a[i] = math.select(0f, 1f, a[i] == b[i].x); return;

                case blast_operation.not:
#if DEVELOPMENT_BUILD
                    Debug.LogError("not");
#endif
                    return;
            }
        }

        /// <summary>
        /// handle operation a.xy and b.xx
        /// </summary>
        void handle_op_f2_f1(in blast_operation op, in byte current, in byte previous, [NoAlias]float4* a, [NoAlias]float4* b, ref bool last_is_op_or_first, ref byte vector_size)
        {
            last_is_op_or_first = false;
            switch (op)
            {
                default: return;
                case blast_operation.nop: for (int i = 0; i < ssmd_datacount; i++) a[i].xy = b[i].xx; break;

                case blast_operation.add: for (int i = 0; i < ssmd_datacount; i++) a[i].xy = a[i].xy + b[i].xx; return;
                case blast_operation.substract: for (int i = 0; i < ssmd_datacount; i++) a[i].xy = a[i].xy - b[i].xx; return;
                case blast_operation.multiply: for (int i = 0; i < ssmd_datacount; i++) a[i].xy = a[i].xy * b[i].xx; return;
                case blast_operation.divide: for (int i = 0; i < ssmd_datacount; i++) a[i].xy = a[i].xy / b[i].xx; return;

                case blast_operation.and: for (int i = 0; i < ssmd_datacount; i++) a[i].xy = math.select(0, 1, math.all(a[i].xy) && math.all(b[i].xx)); return;
                case blast_operation.or: for (int i = 0; i < ssmd_datacount; i++) a[i].xy = math.select(0, 1, math.any(a[i].xy) || math.any(b[i].xx != 0)); return;
                case blast_operation.xor: for (int i = 0; i < ssmd_datacount; i++) a[i].xy = math.select(0, 1, math.any(a[i].xy) ^ math.any(b[i].xx)); return;

                case blast_operation.greater: for (int i = 0; i < ssmd_datacount; i++) a[i].xy = math.select(0f, 1f, a[i].xy > b[i].xx); return;
                case blast_operation.smaller: for (int i = 0; i < ssmd_datacount; i++) a[i].xy = math.select(0f, 1f, a[i].xy < b[i].xx); return;
                case blast_operation.smaller_equals: for (int i = 0; i < ssmd_datacount; i++) a[i].xy = math.select(0f, 1f, a[i].xy <= b[i].xx); return;
                case blast_operation.greater_equals: for (int i = 0; i < ssmd_datacount; i++) a[i].xy = math.select(0f, 1f, a[i].xy >= b[i].xx); return;
                case blast_operation.equals: for (int i = 0; i < ssmd_datacount; i++) a[i].xy = math.select(0f, 1f, a[i].xy == b[i].xx); return;

                case blast_operation.not:
#if DEVELOPMENT_BUILD
                    Debug.LogError("not");
#endif
                    return;
            }
        }

        /// <summary>
        /// handle operation a.xyz and b.xxx
        /// </summary>
        void handle_op_f3_f1(in blast_operation op, in byte current, in byte previous, [NoAlias]float4* a, [NoAlias]float4* b, ref bool last_is_op_or_first, ref byte vector_size)
        {
            last_is_op_or_first = false;
            switch (op)
            {
                default: return;
                case blast_operation.nop: for (int i = 0; i < ssmd_datacount; i++) a[i].xyz = b[i].xxx; break;

                case blast_operation.add: for (int i = 0; i < ssmd_datacount; i++) a[i].xyz = a[i].xyz + b[i].xxx; return;
                case blast_operation.substract: for (int i = 0; i < ssmd_datacount; i++) a[i].xyz = a[i].xyz - b[i].xxx; return;
                case blast_operation.multiply: for (int i = 0; i < ssmd_datacount; i++) a[i].xyz = a[i].xyz * b[i].xxx; return;
                case blast_operation.divide: for (int i = 0; i < ssmd_datacount; i++) a[i].xyz = a[i].xyz / b[i].xxx; return;

                case blast_operation.and: for (int i = 0; i < ssmd_datacount; i++) a[i].xyz = math.select(0, 1, math.all(a[i].xyz) && math.all(b[i].xxx)); return;
                case blast_operation.or: for (int i = 0; i < ssmd_datacount; i++) a[i].xyz = math.select(0, 1, math.any(a[i].xyz) || math.any(b[i].xxx != 0)); return;
                case blast_operation.xor: for (int i = 0; i < ssmd_datacount; i++) a[i].xyz = math.select(0, 1, math.any(a[i].xyz) ^ math.any(b[i].xxx)); return;

                case blast_operation.greater: for (int i = 0; i < ssmd_datacount; i++) a[i].xyz = math.select(0f, 1f, a[i].xyz > b[i].xxx); return;
                case blast_operation.smaller: for (int i = 0; i < ssmd_datacount; i++) a[i].xyz = math.select(0f, 1f, a[i].xyz < b[i].xxx); return;
                case blast_operation.smaller_equals: for (int i = 0; i < ssmd_datacount; i++) a[i].xyz = math.select(0f, 1f, a[i].xyz <= b[i].xxx); return;
                case blast_operation.greater_equals: for (int i = 0; i < ssmd_datacount; i++) a[i].xyz = math.select(0f, 1f, a[i].xyz >= b[i].xxx); return;
                case blast_operation.equals: for (int i = 0; i < ssmd_datacount; i++) a[i].xyz = math.select(0f, 1f, a[i].xyz == b[i].xxx); return;

                case blast_operation.not:
#if DEVELOPMENT_BUILD
                    Debug.LogError("not");
#endif
                    return;
            }
        }

        /// <summary>
        /// handle operation a.xyzw and b.xxxx
        /// </summary>
        void handle_op_f4_f1(in blast_operation op, in byte current, in byte previous, [NoAlias]float4* a, [NoAlias]float4* b, ref bool last_is_op_or_first, ref byte vector_size)
        {
            last_is_op_or_first = false;
            switch (op)
            {
                default: return;
                case blast_operation.nop: for (int i = 0; i < ssmd_datacount; i++) a[i] = b[i].xxxx; break;

                case blast_operation.add: for (int i = 0; i < ssmd_datacount; i++) a[i] = a[i].xyzw + b[i].xxxx; return;
                case blast_operation.substract: for (int i = 0; i < ssmd_datacount; i++) a[i] = a[i].xyzw - b[i].xxxx; return;
                case blast_operation.multiply: for (int i = 0; i < ssmd_datacount; i++) a[i] = a[i].xyzw * b[i].xxxx; return;
                case blast_operation.divide: for (int i = 0; i < ssmd_datacount; i++) a[i] = a[i].xyzw / b[i].xxxx; return;

                case blast_operation.and: for (int i = 0; i < ssmd_datacount; i++) a[i] = math.select(0, 1, math.all(a[i]) && math.all(b[i].xxxx)); return;
                case blast_operation.or: for (int i = 0; i < ssmd_datacount; i++) a[i] = math.select(0, 1, math.any(a[i]) || math.any(b[i].xxxx != 0)); return;
                case blast_operation.xor: for (int i = 0; i < ssmd_datacount; i++) a[i] = math.select(0, 1, math.any(a[i]) ^ math.any(b[i].xxxx)); return;

                case blast_operation.greater: for (int i = 0; i < ssmd_datacount; i++) a[i] = math.select(0f, 1f, a[i] > b[i].xxxx); return;
                case blast_operation.smaller: for (int i = 0; i < ssmd_datacount; i++) a[i] = math.select(0f, 1f, a[i] < b[i].xxxx); return;
                case blast_operation.smaller_equals: for (int i = 0; i < ssmd_datacount; i++) a[i] = math.select(0f, 1f, a[i] <= b[i].xxxx); return;
                case blast_operation.greater_equals: for (int i = 0; i < ssmd_datacount; i++) a[i] = math.select(0f, 1f, a[i] >= b[i].xxxx); return;
                case blast_operation.equals: for (int i = 0; i < ssmd_datacount; i++) a[i] = math.select(0f, 1f, a[i] == b[i].xxxx); return;

                case blast_operation.not:
#if DEVELOPMENT_BUILD
                    Debug.LogError("not");
#endif
                    return;
            }
        }

        /// <summary>
        /// handle operation a.xy and b.xy
        /// </summary>
        void handle_op_f2_f2(in blast_operation op, in byte current, in byte previous, [NoAlias]float4* a, [NoAlias]float4* b, ref bool last_is_op_or_first, ref byte vector_size)
        {
            last_is_op_or_first = false;
            switch (op)
            {
                default: return;
                case blast_operation.nop: for (int i = 0; i < ssmd_datacount; i++) a[i].xy = b[i].xy; break;

                case blast_operation.add: for (int i = 0; i < ssmd_datacount; i++) a[i].xy = a[i].xy + b[i].xy; return;
                case blast_operation.substract: for (int i = 0; i < ssmd_datacount; i++) a[i].xy = a[i].xy - b[i].xy; return;
                case blast_operation.multiply: for (int i = 0; i < ssmd_datacount; i++) a[i].xy = a[i].xy * b[i].xy; return;
                case blast_operation.divide: for (int i = 0; i < ssmd_datacount; i++) a[i].xy = a[i].xy / b[i].xy; return;

                case blast_operation.and: for (int i = 0; i < ssmd_datacount; i++) a[i].xy = math.select(0, 1, math.all(a[i].xy) && math.all(b[i].xy)); return;
                case blast_operation.or: for (int i = 0; i < ssmd_datacount; i++) a[i].xy = math.select(0, 1, math.any(a[i].xy) || math.any(b[i].xy != 0)); return;
                case blast_operation.xor: for (int i = 0; i < ssmd_datacount; i++) a[i].xy = math.select(0, 1, math.any(a[i].xy) ^ math.any(b[i].xy)); return;

                case blast_operation.greater: for (int i = 0; i < ssmd_datacount; i++) a[i].xy = math.select(0f, 1f, a[i].xy > b[i].xy); return;
                case blast_operation.smaller: for (int i = 0; i < ssmd_datacount; i++) a[i].xy = math.select(0f, 1f, a[i].xy < b[i].xy); return;
                case blast_operation.smaller_equals: for (int i = 0; i < ssmd_datacount; i++) a[i].xy = math.select(0f, 1f, a[i].xy <= b[i].xy); return;
                case blast_operation.greater_equals: for (int i = 0; i < ssmd_datacount; i++) a[i].xy = math.select(0f, 1f, a[i].xy >= b[i].xy); return;
                case blast_operation.equals: for (int i = 0; i < ssmd_datacount; i++) a[i].xy = math.select(0f, 1f, a[i].xy == b[i].xy); return;

                case blast_operation.not:
#if DEVELOPMENT_BUILD
                    Debug.LogError("not");
#endif
                    return;
            }
        }

        /// <summary>
        /// handle operation a.xyz and b.xyz
        /// </summary>
        void handle_op_f3_f3(in blast_operation op, in byte current, in byte previous, [NoAlias]float4* a, [NoAlias]float4* b, ref bool last_is_op_or_first, ref byte vector_size)
        {
            last_is_op_or_first = false;
            switch (op)
            {
                default: return;
                case blast_operation.nop: for (int i = 0; i < ssmd_datacount; i++) a[i].xyz = b[i].xyz; break;

                case blast_operation.add: for (int i = 0; i < ssmd_datacount; i++) a[i].xyz = a[i].xyz + b[i].xyz; return;
                case blast_operation.substract: for (int i = 0; i < ssmd_datacount; i++) a[i].xyz = a[i].xyz - b[i].xyz; return;
                case blast_operation.multiply: for (int i = 0; i < ssmd_datacount; i++) a[i].xyz = a[i].xyz * b[i].xyz; return;
                case blast_operation.divide: for (int i = 0; i < ssmd_datacount; i++) a[i].xyz = a[i].xyz / b[i].xyz; return;

                case blast_operation.and: for (int i = 0; i < ssmd_datacount; i++) a[i].xyz = math.select(0, 1, math.all(a[i].xyz) && math.all(b[i].xyz)); return;
                case blast_operation.or: for (int i = 0; i < ssmd_datacount; i++) a[i].xyz = math.select(0, 1, math.any(a[i].xyz) || math.any(b[i].xyz != 0)); return;
                case blast_operation.xor: for (int i = 0; i < ssmd_datacount; i++) a[i].xyz = math.select(0, 1, math.any(a[i].xyz) ^ math.any(b[i].xyz)); return;

                case blast_operation.greater: for (int i = 0; i < ssmd_datacount; i++) a[i].xyz = math.select(0f, 1f, a[i].xyz > b[i].xyz); return;
                case blast_operation.smaller: for (int i = 0; i < ssmd_datacount; i++) a[i].xyz = math.select(0f, 1f, a[i].xyz < b[i].xyz); return;
                case blast_operation.smaller_equals: for (int i = 0; i < ssmd_datacount; i++) a[i].xyz = math.select(0f, 1f, a[i].xyz <= b[i].xyz); return;
                case blast_operation.greater_equals: for (int i = 0; i < ssmd_datacount; i++) a[i].xyz = math.select(0f, 1f, a[i].xyz >= b[i].xyz); return;
                case blast_operation.equals: for (int i = 0; i < ssmd_datacount; i++) a[i].xyz = math.select(0f, 1f, a[i].xyz == b[i].xyz); return;

                case blast_operation.not:
#if DEVELOPMENT_BUILD
                    Debug.LogError("not");
#endif
                    return;
            }
        }

        /// <summary>
        /// handle operation a.xyz and b.xyz
        /// </summary>
        void handle_op_f4_f4(in blast_operation op, in byte current, in byte previous, [NoAlias]float4* a, [NoAlias]float4* b, ref bool last_is_op_or_first, ref byte vector_size)
        {
            last_is_op_or_first = false;
            switch (op)
            {
                default: return;
                case blast_operation.nop: for (int i = 0; i < ssmd_datacount; i++) a[i] = b[i]; break;

                case blast_operation.add: for (int i = 0; i < ssmd_datacount; i++) a[i] = a[i] + b[i]; return;
                case blast_operation.substract: for (int i = 0; i < ssmd_datacount; i++) a[i] = a[i] - b[i]; return;
                case blast_operation.multiply: for (int i = 0; i < ssmd_datacount; i++) a[i] = a[i] * b[i]; return;
                case blast_operation.divide: for (int i = 0; i < ssmd_datacount; i++) a[i] = a[i] / b[i]; return;

                case blast_operation.and: for (int i = 0; i < ssmd_datacount; i++) a[i] = math.select(0, 1, math.all(a[i]) && math.all(b[i])); return;
                case blast_operation.or: for (int i = 0; i < ssmd_datacount; i++) a[i] = math.select(0, 1, math.any(a[i]) || math.any(b[i] != 0)); return;
                case blast_operation.xor: for (int i = 0; i < ssmd_datacount; i++) a[i] = math.select(0, 1, math.any(a[i]) ^ math.any(b[i])); return;

                case blast_operation.greater: for (int i = 0; i < ssmd_datacount; i++) a[i] = math.select(0f, 1f, a[i] > b[i]); return;
                case blast_operation.smaller: for (int i = 0; i < ssmd_datacount; i++) a[i] = math.select(0f, 1f, a[i] < b[i]); return;
                case blast_operation.smaller_equals: for (int i = 0; i < ssmd_datacount; i++) a[i] = math.select(0f, 1f, a[i] <= b[i]); return;
                case blast_operation.greater_equals: for (int i = 0; i < ssmd_datacount; i++) a[i] = math.select(0f, 1f, a[i] >= b[i]); return;
                case blast_operation.equals: for (int i = 0; i < ssmd_datacount; i++) a[i] = math.select(0f, 1f, a[i] == b[i]); return;

                case blast_operation.not:
#if DEVELOPMENT_BUILD
                    Debug.LogError("not");
#endif
                    return;
            }
        }

        #endregion

        #region Function Handlers 
        #region mula etc. 

        /// <summary>
        /// handle a sequence of operations
        /// - mula e62 p1 p2 .. pn
        /// - every parameter must be of same vector size 
        /// - future language versions might allow multiple vectorsizes in mula 
        /// </summary>
        /// <param name="temp">temp scratch data</param>
        /// <param name="code_pointer">current codepointer</param>
        /// <param name="vector_size">output vectorsize</param>
        /// <param name="f4">output data vector</param>
#pragma warning disable CS1573 // Parameter 'operation' has no matching param tag in the XML comment for 'BlastSSMDInterpretor.get_op_a_result(void*, ref int, ref byte, ref float4*, blast_operation)' (but other parameters do)
        void get_op_a_result(void* temp, ref int code_pointer, ref byte vector_size, ref float4* f4, blast_operation operation)
#pragma warning restore CS1573 // Parameter 'operation' has no matching param tag in the XML comment for 'BlastSSMDInterpretor.get_op_a_result(void*, ref int, ref byte, ref float4*, blast_operation)' (but other parameters do)
        {
            // decode parametercount and vectorsize from code in 62 format 
            code_pointer++;
            byte c = BlastInterpretor.decode62(code[code_pointer], ref vector_size);

            // all operations should be of equal vectorsize we dont need to check this
            // - but could in debug 
            // - we could also allow it ....

            switch (vector_size)
            {
                case 0:
                case 4:
                    vector_size = 4;

                    code_pointer++;
                    pop_fx_into<float4>(in code_pointer, f4);

                    for (int j = 1; j < c; j++)
                    {
                        // pop_f4_with_op_into_f4(code_pointer + j, (float4*)temp, f4, blast_operation.multiply); 
                    }

                    code_pointer += c - 1;
                    break;


                case 1:
                    code_pointer++;
                    pop_fx_into<float>(in code_pointer, (float*)temp);

                    for (int j = 1; j < c - 1; j++)
                    {
                        pop_f1_with_op_into_f1(code_pointer + j, (float*)temp, (float*)temp, operation);
                    }

                    pop_f1_with_op_into_f4(code_pointer + c - 1, (float*)temp, f4, operation);
                    code_pointer += c - 1;
                    break;

                case 2:
                    break;

                case 3:
                    break;
            }
        }


        #endregion 
        #region fused substract multiply actions

        /// <summary>
        /// fused multiply add 
        /// - always 3 params, input vector size == output vectorsize
        /// </summary>
        /// <returns></returns>
        void get_fma_result(void* temp, ref int code_pointer, ref byte vector_size, ref float4* f4)
        {
            BlastVariableDataType datatype;

            // peek metadata 
            pop_op_meta(code[code_pointer + 1], out datatype, out vector_size);

            // all 3 inputs share datatype and vectorsize 
            switch (vector_size)
            {
                case 1:
                    float* m11 = (float*)temp;
                    pop_f1_into(code_pointer + 1, m11);
                    pop_f1_with_op_into_f1(code_pointer + 2, m11, m11, blast_operation.multiply);
                    pop_f1_with_op_into_f4(code_pointer + 3, m11, f4, blast_operation.add);
                    break;

                case 2:
                    float2* m12 = (float2*)temp;
                    pop_fx_into(code_pointer + 1, m12);
                    pop_f2_with_op_into_f2(code_pointer + 2, m12, m12, blast_operation.multiply);
                    pop_f2_with_op_into_f4(code_pointer + 3, m12, f4, blast_operation.add);
                    break;

                case 3:
                    float3* m13 = (float3*)temp;
                    pop_fx_into(code_pointer + 1, m13);
                    pop_f3_with_op_into_f3(code_pointer + 2, m13, m13, blast_operation.multiply);
                    pop_f3_with_op_into_f4(code_pointer + 3, m13, f4, blast_operation.add);
                    break;

                case 0:
                case 4:
                    float4* m14 = (float4*)temp;
                    pop_fx_into(code_pointer + 1, f4); // with f4 we can avoid reading/writing to the same buffer by alternating as long as we end at f4
                    pop_f4_with_op_into_f4(code_pointer + 2, f4, m14, blast_operation.multiply);
                    pop_f4_with_op_into_f4(code_pointer + 3, m14, f4, blast_operation.add);
                    break;
            }
            code_pointer += 3;
        }

        #endregion


        #endregion

        #region GetCompound and Execute (privates)

        /// <summary>
        /// process a compound, on function exit write back data to register 
        /// </summary>
        int GetCompoundResult(void* temp, ref int code_pointer, ref byte vector_size)
        {
            byte op = 0;
            byte prev_op = 0;

            bool end_of_compound = false;

            bool op_is_value = false;
            bool prev_op_is_value = false;

            bool last_is_op_or_first = true;

            bool minus = false;   // shouldnt we treat these as 1?
            bool not = false;

            float* f1 = stackalloc float[ssmd_datacount];

            float4* f4 = register; // stackalloc float4[ssmd_datacount];
            float4* f4_result = stackalloc float4[ssmd_datacount];

            byte max_vector_size = 1;
            blast_operation current_operation = blast_operation.nop;

            while (code_pointer < package.CodeSize && !end_of_compound)
            {
                prev_op = op;
                prev_op_is_value = op_is_value;

                op = code[code_pointer];
                op_is_value = op >= BlastInterpretor.opt_value && op != 255;

                max_vector_size = max_vector_size < vector_size ? vector_size : max_vector_size;

                // increase vectorsize if a consecutive operation, reset to 1 if not, do nothing on nops or ends
                // results in wierdnes on wierd ops.. should filter those in the analyser if that could occur 

                // determine if growing a vector, ifso update the next element
                if (op != 0 && op != (byte)blast_operation.end)
                {
                    bool grow_vector = false;

                    if (op_is_value && prev_op_is_value)
                    {
                        grow_vector = true;
                    }
                    else
                    {
                        // if the previous op also gave back a value 
                        if (prev_op_is_value || prev_op == (byte)blast_operation.pop)
                        {
                            // we grow vector 
                            switch ((blast_operation)op)
                            {
                                case blast_operation.peek:
                                    // f1 = peek(1) ? 1f : 0f;
                                    // grow_vector = true;
                                    return (int)BlastError.stack_error_peek;

                                case blast_operation.peekv:
                                    return (int)BlastError.stack_error_peek;

                                case blast_operation.pop:
                                    stack_pop_f1_into(f1);
                                    grow_vector = true;
                                    break;

                                default:
                                    // reset vector on all other operations 
                                    vector_size = 1;
                                    break;
                            }
                        }
                    }

                    // update the current vector
                    if (grow_vector)
                    {
                        switch ((BlastVectorSizes)vector_size)
                        {
                            case BlastVectorSizes.float1:
                                for (int i = 0; i < ssmd_datacount; i++) f4[i].x = f1[i];
                                break;
                            case BlastVectorSizes.float2:
                                break;
                            case BlastVectorSizes.float3:
                                break;
                            case BlastVectorSizes.float4:
#if LOG_ERRORS
                                Debug.LogError("interpretor error, expanding vector beyond size 4, this is not supported");
#endif
                                return (int)BlastError.error_update_vector_fail;
                        }
                        vector_size++;
                    }
                }

                // set result to current value 
                // 
                if ((blast_operation)op == blast_operation.end || op == 0)
                {
                    // only fi returning... 
                    // UnsafeUtils.MemCpy(f4_result, f4, ssmd_datacount * sizeof(float4));
                    //- used in handle op,, we could void the memcpy when testing done
                }

                //
                // switch on an operation to take and handle it:  a = a + b; 
                //
                switch ((blast_operation)op)
                {
                    case blast_operation.end:

                        // when assigning this might not be the end, peek next instruction.
                        //
                        // - if not a nop or a new assign, then continue reading the compound 
                        //
                        if (code_pointer < package.CodeSize - 1)
                        {
                            blast_operation next_op = (blast_operation)code[code_pointer + 1];

                            if (next_op != blast_operation.nop && !BlastInterpretor.IsAssignmentOperation(next_op))
                            {
                                // for now restrict ourselves to only accepting operations at this point 
                                if (BlastInterpretor.IsMathematicalOrBooleanOperation(next_op))
                                {
                                    // break out of switch before returning, there is more to this operation then known at the moment 
                                    break;
                                }
                                else
                                {
                                    // probably a bug
                                    Debug.LogWarning($"Possible BUG: encountered non mathematical operation '{next_op}' after compound in statement at codepointer {code_pointer + 1}, expecting nop or assign");
                                }
                            }
                        }

                        end_of_compound = true;
                        break;

                    case blast_operation.nop:
                        // assignments will end with nop and not with end as a compounded statement
                        // at this point vector_size is decisive for the returned value
                        end_of_compound = true;
                        break;

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
                        {
                            current_operation = (blast_operation)op;
                            if (prev_op == 0) // nop op -> any - is at start of ops
                            {
                                if ((blast_operation)op == blast_operation.substract)
                                {
                                    minus = true;
                                }
                            }

                            last_is_op_or_first = true;
                        }
                        break;

                    case blast_operation.not:
                        {
                            if (last_is_op_or_first)
                            {
#if DEVELOPMENT_BUILD
                                if (not)
                                {
                                    Debug.LogError("double token: ! !, this will be become nothing, it is not supported as it cant be determined to be intentional and will raise this error in editor mode only");
                                }
#endif
                                not = !not;
                            }
                            else
                            {
                                current_operation = blast_operation.not;
                                last_is_op_or_first = true;
                            }
                        }
                        break;

                    case blast_operation.substract:
                        {
                            if (last_is_op_or_first)
                            {
#if DEVELOPMENT_BUILD
                                if (minus)
                                {
                                    Debug.LogError("double token: - -, this will be become +, it is not supported as it cant be determined to be intentional and will raise this error in editor mode only");
                                }
#endif
                                minus = !minus;
                            }
                            else
                            {
                                current_operation = blast_operation.substract;
                                last_is_op_or_first = true;
                            }
                        }
                        break;

                    default:
                        {
                            // get result for op 
                            switch ((blast_operation)op)
                            {
                                case blast_operation.begin:
                                    ++code_pointer;
                                    // should not reach here
#if DEVELOPMENT_BUILD
                                    Debug.LogWarning("should not be nesting compounds... compiler did not do its job wel");
#endif

                                    // copy register? side effects!!!!!!!!!!!!!

#pragma warning disable CS1587 // XML comment is not placed on a valid language element
                                    /// f4_result = get_compound_result(ref code_pointer, ref vector_size);
                                    break;
#pragma warning restore CS1587 // XML comment is not placed on a valid language element

                                // standard functions
                                case blast_operation.fma: get_fma_result(temp, ref code_pointer, ref vector_size, ref f4_result); break;

                                case blast_operation.mula: get_op_a_result(temp, ref code_pointer, ref vector_size, ref f4_result, blast_operation.multiply); break;
                                case blast_operation.adda: get_op_a_result(temp, ref code_pointer, ref vector_size, ref f4_result, blast_operation.add); break;
                                case blast_operation.suba: get_op_a_result(temp, ref code_pointer, ref vector_size, ref f4_result, blast_operation.substract); break;
                                case blast_operation.diva: get_op_a_result(temp, ref code_pointer, ref vector_size, ref f4_result, blast_operation.diva); break;





                                /*
               // math functions 
               case blast_operation.abs: get_abs_result(ref code_pointer, ref vector_size, out f4_result); break;
               case blast_operation.normalize: get_normalize_result(ref code_pointer, ref vector_size, out f4_result); break;
               case blast_operation.saturate: get_saturate_result(ref code_pointer, ref vector_size, out f4_result); break;
               case blast_operation.maxa: get_maxa_result(ref code_pointer, ref vector_size, out f4_result); break;
               case blast_operation.mina: get_mina_result(ref code_pointer, ref vector_size, out f4_result); break;
               case blast_operation.max: get_max_result(ref code_pointer, ref vector_size, out f4_result); break;
               case blast_operation.min: get_min_result(ref code_pointer, ref vector_size, out f4_result); break;
               case blast_operation.ceil: get_ceil_result(ref code_pointer, ref vector_size, out f4_result); break;
               case blast_operation.floor: get_floor_result(ref code_pointer, ref vector_size, out f4_result); break;
               case blast_operation.frac: get_frac_result(ref code_pointer, ref vector_size, out f4_result); break;
               case blast_operation.sqrt: get_sqrt_result(ref code_pointer, ref vector_size, out f4_result); break;
               case blast_operation.sin: get_sin_result(ref code_pointer, ref vector_size, out f4_result); break;
               case blast_operation.cos: get_cos_result(ref code_pointer, ref vector_size, out f4_result); break;
               case blast_operation.tan: get_tan_result(ref code_pointer, ref vector_size, out f4_result); break;
               case blast_operation.sinh: get_sinh_result(ref code_pointer, ref vector_size, out f4_result); break;
               case blast_operation.cosh: get_cosh_result(ref code_pointer, ref vector_size, out f4_result); break;
               case blast_operation.atan: get_atan_result(ref code_pointer, ref vector_size, out f4_result); break;
               case blast_operation.degrees: get_degrees_result(ref code_pointer, ref vector_size, out f4_result); break;
               case blast_operation.radians: get_rad_result(ref code_pointer, ref vector_size, out f4_result); break;

               // math utils 
               case blast_operation.select: get_select_result(ref code_pointer, ref vector_size, out f4_result); break;
               case blast_operation.clamp: get_clamp_result(ref code_pointer, ref vector_size, out f4_result); break;
               case blast_operation.lerp: get_lerp_result(ref code_pointer, ref vector_size, out f4_result); break;
               case blast_operation.slerp: get_slerp_result(ref code_pointer, ref vector_size, out f4_result); break;
               case blast_operation.nlerp: get_nlerp_result(ref code_pointer, ref vector_size, out f4_result); break;

               // mula family
               case blast_operation.mula: get_mula_result(ref code_pointer, ref vector_size, out f4_result); max_vector_size = vector_size; break;

               ///
               ///  FROM HERE NOT CONVERTED YET TO METADATA USE
               /// 

               case blast_operation.random:
                   get_random_result(ref code_pointer, ref minus, ref vector_size, ref f4_result);
                   break;

               case blast_operation.adda:
                   f4_result = get_adda_result(ref code_pointer, ref minus, ref vector_size);
                   break;

               case blast_operation.suba:
                   f4_result = get_suba_result(ref code_pointer, ref minus, ref vector_size);
                   break;

               case blast_operation.diva:
                   f4_result = get_diva_result(ref code_pointer, ref minus, ref vector_size);
                   break;

               case blast_operation.all:
                   f4_result = get_all_result(ref code_pointer, ref minus, ref not, ref vector_size);
                   break;

               case blast_operation.any:
                   f4_result = get_any_result(ref code_pointer, ref minus, ref not, ref vector_size);
                   break;
                   */


                                case blast_operation.pop:
                                    pop_fn_into(code_pointer, f4_result, minus, ref vector_size);
                                    minus = false;
                                    break;
                                /*
                                                                case blast_operation.ex_op:

                                                                    code_pointer++;
                                                                    extended_blast_operation exop = (extended_blast_operation)code[code_pointer];

                                                                    if (exop == extended_blast_operation.call)
                                                                    {
                                                                        // call external function 
                                                                        f4_result = CallExternal(ref code_pointer, ref minus, ref vector_size, ref f4);
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
                                                                            case extended_blast_operation.rsqrt: get_rsqrt_result(ref code_pointer, ref vector_size, out f4_result); break;
                                                                            case extended_blast_operation.pow: get_pow_result(ref code_pointer, ref vector_size, out f4_result); break;
#if DEVELOPMENT_BUILD
                                                                            default:
                                                                                Debug.LogError($"codepointer: {code_pointer} => {code[code_pointer]}, extended operation {exop} not handled");
                                                                                break;
#endif
                                                                        }
                                                                    }
                                                                    // prev_op = (byte)script_op.ex_op;
                                                                    // prev_op_is_value = false;
                                                                    op = (byte)blast_operation.nop;
                                                                    op_is_value = true;
                                                                    last_is_op_or_first = code_pointer == 0;
                                                                    break;*/

                                case blast_operation.ret:
                                    code_pointer = package.CodeSize + 1;
                                    // RETURN => QUIT
                                    return 0;

                                // 
                                // this should give us a decent jump-table, saving some conditional jumps later  
                                //
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
                                        // index constant , MUST be float1
                                        // if there is a minus, each script will have it 
                                        float constant = engine_ptr->constants[op];
                                        constant = math.select(constant, -constant, minus);

                                        minus = false;
                                        not = false;

                                        if (last_is_op_or_first)
                                        {
                                            //the last thing was an operation or the first thing, set value
                                            switch ((BlastVectorSizes)vector_size)
                                            {
                                                case BlastVectorSizes.float1: for (int i = 0; i < ssmd_datacount; i++) { f4[i].x = f4_result[i].x; f4_result[i].x = constant; } break;
                                                case BlastVectorSizes.float2: for (int i = 0; i < ssmd_datacount; i++) f4_result[i].xy = constant; break;
                                                case BlastVectorSizes.float3: for (int i = 0; i < ssmd_datacount; i++) f4_result[i].xyz = constant; break;
                                                case BlastVectorSizes.float4: for (int i = 0; i < ssmd_datacount; i++) f4_result[i].xyzw = constant; break;
                                                default:
#if DEVELOPMENT_BUILD
                                                    Debug.LogError($"codepointer: {code_pointer} => {code[code_pointer]}, error: growing vector beyond size 4 from constant");
#endif
                                                    break;
                                            }
                                        }
                                        else
                                        {
                                            // growing a vector
                                            switch ((BlastVectorSizes)vector_size)
                                            {
                                                case BlastVectorSizes.float1: for (int i = 0; i < ssmd_datacount; i++) f4_result[i].x = constant; break;
                                                case BlastVectorSizes.float2: for (int i = 0; i < ssmd_datacount; i++) f4_result[i].y = constant; break;
                                                case BlastVectorSizes.float3: for (int i = 0; i < ssmd_datacount; i++) f4_result[i].z = constant; break;
                                                case BlastVectorSizes.float4: for (int i = 0; i < ssmd_datacount; i++) f4_result[i].w = constant; break;
                                                default:
#if DEVELOPMENT_BUILD
                                                    Debug.LogError($"codepointer: {code_pointer} => {code[code_pointer]}, error: growing vector beyond size 4 from constant");
#endif
                                                    break;
                                            }
                                        }
                                    }
                                    break;

                                default:
                                    {
                                        if (op >= BlastInterpretor.opt_id)
                                        {
                                            // lookup identifier value
                                            byte id = (byte)(op - BlastInterpretor.opt_id);
                                            byte this_vector_size = BlastInterpretor.GetMetaDataSize(metadata, id); // this now uses an offsetted index into metadata...   this should work...

                                            if (last_is_op_or_first)
                                            {
                                                switch ((BlastVectorSizes)this_vector_size)
                                                {
                                                    case BlastVectorSizes.float1:
                                                        {
                                                            for (int i = 0; i < ssmd_datacount; i++)
                                                            {
                                                                float tf = ((float*)data[i])[id];
                                                                f4_result[i].x = math.select(tf, -tf, minus);
                                                            }
                                                            minus = false;
                                                        }
                                                        break;

                                                    case BlastVectorSizes.float2:
                                                        {
                                                            for (int i = 0; i < ssmd_datacount; i++)
                                                            {
                                                                float2 t2 = new float2(((float*)data[i])[id], ((float*)data[i])[id + 1]);
                                                                f4_result[i].xy = math.select(t2, -t2, minus);
                                                            }
                                                            minus = false;
                                                        }
                                                        break;

                                                    case BlastVectorSizes.float3:
                                                        {
                                                            for (int i = 0; i < ssmd_datacount; i++)
                                                            {
                                                                float3 t3 = new float3(((float*)data[i])[id], ((float*)data[i])[id + 1], ((float*)data[i])[id + 2]);
                                                                f4_result[i].xyz = math.select(t3, -t3, minus);
                                                            }
                                                            minus = false;
                                                        }
                                                        break;
                                                    case BlastVectorSizes.float4:
                                                        {
                                                            for (int i = 0; i < ssmd_datacount; i++)
                                                            {
                                                                float4 t4 = new float4(((float*)data[i])[id], ((float*)data[i])[id + 1], ((float*)data[i])[id + 2], ((float*)data[i])[id + 3]);
                                                                f4_result[i] = math.select(t4, -t4, minus);
                                                            }
                                                            minus = false;
                                                        }
                                                        break;
                                                }
                                            }
                                            else
                                            {
                                                // growing a vector
                                                switch ((BlastVectorSizes)this_vector_size)
                                                {
                                                    case BlastVectorSizes.float1:
                                                        for (int i = 0; i < ssmd_datacount; i++)
                                                        {
                                                            float tf = ((float*)data[i])[id];
                                                            f4_result[i][vector_size - 1] = math.select(tf, -tf, minus);
                                                        }
                                                        minus = false;
                                                        break;
                                                    case BlastVectorSizes.float2:
                                                    case BlastVectorSizes.float3:
                                                    case BlastVectorSizes.float4:
                                                    default:
                                                        Debug.LogError("codepointer: {code_pointer} => {code[code_pointer]}, error: growing vector from other vectors not fully supported");
                                                        break;
                                                }
                                            }

                                            vector_size = vector_size > this_vector_size ? vector_size : this_vector_size; // probably massively incorrect but its too late 
                                        }
                                        else
                                        {
                                            // we receive an operation while expecting a value 
                                            //
                                            // if we predict this at the compiler we could use the opcodes value as value constant
                                            //
#if DEVELOPMENT_BUILD
                                            Debug.LogError($"codepointer: {code_pointer} => {code[code_pointer]}, encountered unknown op {(blast_operation)op}");
#endif

                                            // this should screw stuff up 
                                            return (int)BlastError.ssmd_error_expecting_value;
                                        }
                                    }
                                    break;
                            }


                            // above the other switch cases handle tokens and process them into f4_result (overwriting the copy.. 

                            //
                            // after an operation with growvector we should stack value
                            //
                            // - processes a = a * b;   f4 = a,   f4_result = b; 
                            //
                            switch ((BlastVectorSizes)vector_size)
                            {
                                case BlastVectorSizes.float1:
                                    {
                                        switch ((BlastVectorSizes)max_vector_size)
                                        {
                                            case BlastVectorSizes.float1:
                                                handle_op_f1_f1(current_operation, op, prev_op, f1, f4_result, ref last_is_op_or_first, ref vector_size);
                                                break;
                                            case BlastVectorSizes.float2:
                                                handle_op_f2_f1(current_operation, op, prev_op, f4, f4_result, ref last_is_op_or_first, ref vector_size);
                                                break;
                                            case BlastVectorSizes.float3:
                                                handle_op_f3_f1(current_operation, op, prev_op, f4, f4_result, ref last_is_op_or_first, ref vector_size);
                                                break;
                                            case BlastVectorSizes.float4:
                                                handle_op_f4_f1(current_operation, op, prev_op, f4, f4_result, ref last_is_op_or_first, ref vector_size);
                                                break;
                                        }
                                    }
                                    break;

                                case BlastVectorSizes.float2:
                                    {
                                        switch ((BlastVectorSizes)max_vector_size)
                                        {
                                            case BlastVectorSizes.float1:
                                                // normal when growing a vector 
                                                break;
                                            case BlastVectorSizes.float2:
                                            case BlastVectorSizes.float3:
                                            case BlastVectorSizes.float4:
                                            default:
#if DEVELOPMENT_BUILD
                                                    // these will be in log because of growing a vector from a compound of unknown sizes  
//                                                    Debug.LogWarning($"execute.ssmd: codepointer: {code_pointer} => {code[code_pointer]}, no support for vector of size {vector_size} -> {max_vector_size}");
#endif
                                                break;
                                        }
                                        handle_op_f2_f2(current_operation, op, prev_op, f4, f4_result, ref last_is_op_or_first, ref vector_size);
                                    }
                                    break;

                                case BlastVectorSizes.float3:
                                    {
                                        switch ((BlastVectorSizes)max_vector_size)
                                        {
                                            case BlastVectorSizes.float2:
                                                // normal when growing a vector
                                                break;

                                            case BlastVectorSizes.float1:
                                            case BlastVectorSizes.float3:
                                            case BlastVectorSizes.float4:
                                            default:
#if DEVELOPMENT_BUILD
                                                    // these will be in log because of growing a vector from a compound of unknown sizes  
//                                                    Debug.LogWarning($"execute.ssmd: codepointer: {code_pointer} => {code[code_pointer]}, no support for vector of size {vector_size} -> {max_vector_size}");
#endif
                                                break;
                                        }
                                        handle_op_f3_f3(current_operation, op, prev_op, f4, f4_result, ref last_is_op_or_first, ref vector_size);
                                    }
                                    break;


                                case 0:
                                case BlastVectorSizes.float4:
                                    {
                                        switch ((BlastVectorSizes)max_vector_size)
                                        {
                                            case BlastVectorSizes.float1:
                                                // have a float 4 but are multiplying with a float1 from earlier
                                                for (int i = 0; i < ssmd_datacount; i++) f4[i].xyzw = f1[i];
                                                break;
                                            case BlastVectorSizes.float2:
                                                for (int i = 0; i < ssmd_datacount; i++) f4[i].xyzw = new float4(f4[i].xy, 0, 0);
                                                break;
                                            case BlastVectorSizes.float3:
                                                for (int i = 0; i < ssmd_datacount; i++) f4[i].xyzw = new float4(f4[1].xyz, 0);
                                                // usually we are growing from float4 to a 4 vector, current op would be nop then 
                                                break;
                                            case BlastVectorSizes.float4:
                                                // have a float 4 and one from earlier.
                                                break;
                                            default:
#if DEVELOPMENT_BUILD
//                                                    Debug.LogWarning($"execute.ssmd: codepointer: {code_pointer} => {code[code_pointer]}, no support for vector of size {vector_size} -> {max_vector_size}");
#endif
                                                break;
                                        }

                                        handle_op_f4_f4(current_operation, op, prev_op, f4, f4_result, ref last_is_op_or_first, ref vector_size);
                                    }
                                    break;

                                default:
                                    {
#if DEVELOPMENT_BUILD
                                            Debug.LogError($"execute.ssmd: codepointer: {code_pointer} => {code[code_pointer]}, encountered unsupported vector size of {vector_size}");
#endif
                                        return (int)BlastError.ssmd_error_unsupported_vector_size;
                                    }
                            }

                            if (current_operation != blast_operation.nop)
                            {
                                // f1 = f4.x;
                            }
                        }

                        break;

                }


                code_pointer++;

            } // while ()


            // processing done, set results from register and return success 
            vector_size = vector_size > max_vector_size ? vector_size : max_vector_size;
            if (vector_size > 1)
            {
                set_register_from_data_f4(f4);
            }
            else
            {
                set_register_from_data_f1(f1);
            }
            return (int)BlastError.success;
        }


        #region Stack Operation Handlers 

        BlastError push(ref int code_pointer, ref byte vector_size, [NoAlias]void* temp)
        {
            if (code[code_pointer] == (byte)blast_operation.begin)
            {
                code_pointer++;

                // push the vector result of a compound 
                BlastError res = (BlastError)GetCompoundResult(temp, ref code_pointer, ref vector_size);
                if (res < 0)
                {
#if DEVELOPMENT_BUILD
                    Debug.LogError($"blast.ssmd.execute.push: failed to read compound data for push operation at codepointer {code_pointer}");
#endif
                    return res;
                }

                switch (vector_size)
                {
                    case 1:
                        push_register_f1();
                        break;
                    case 2:
                        push_register_f2();
                        break;
                    case 3:
                        push_register_f3();
                        break;
                    case 4:
                        push_register_f4();
                        break;
                    default:
#if DEVELOPMENT_BUILD
                        Debug.LogError("blast.ssmd push error: variable vector size not yet supported on stack push of compound");
#endif
                        return BlastError.error_variable_vector_compound_not_supported;
                }
                // code_pointer should index after compound now
            }
            else
            {
                // else push 1 float of data
                push_data_f1(code[code_pointer++] - BlastInterpretor.opt_id);
            }

            return BlastError.success;
        }

        BlastError pushv(ref int code_pointer, ref byte vector_size, [NoAlias]void* temp)
        {
            byte param_count = code[code_pointer++];
            vector_size = (byte)(param_count & 0b00001111); // vectorsize == lower 4 
            param_count = (byte)((param_count & 0b11110000) >> 4);  // param_count == upper 4 

            // either: vector_size or paramcount but NOT both 

            if (param_count == 1)
            {
                switch (vector_size)
                {
                    case 1:
                        push_pop_f(code_pointer, vector_size);
                        code_pointer++;
                        break;

                    default:
#if DEVELOPMENT_BUILD
                        Debug.LogError($"blast.ssmd.interpretor.pushv error: vectorsize {vector_size} not yet supported on stack push");
#endif
                        return BlastError.error_vector_size_not_supported;
                }
            }
            else
            {
#if DEVELOPMENT_BUILD
                if (param_count != vector_size)
                {
                    Debug.LogError($"blast.ssmd.interpretor.pushv error: pushv => param_count > 1 && vector_size != param_count, this is not allowed. Vectorsize: {vector_size}, Paramcount: {param_count}");
                    return BlastError.error_unsupported_operation;
                }
#endif
                // param_count == vector_size and vector_size > 1, compiler enforces it so we should not check it in release 
                // this means that vector is build from multiple data points
                switch (vector_size)
                {
                    case 1:
                        push_f1_pop_1(code_pointer);
                        code_pointer += 1;
                        break;
                    case 2:
                        push_f2_pop_11(code_pointer);
                        code_pointer += 2;
                        break;
                    case 3:
                        push_f3_pop_111(code_pointer);
                        code_pointer += 3;
                        break;
                    case 4:
                        push_f4_pop_1111(code_pointer);
                        code_pointer += 4;
                        break;
                }
            }

            return BlastError.success;
        }

        BlastError pushc(ref int code_pointer, ref byte vector_size, [NoAlias]void* temp)
        {
            // push the vector result of a compound 
            BlastError res = (BlastError)GetCompoundResult(temp, ref code_pointer, ref vector_size);
            if (res < 0)
            {
#if DEVELOPMENT_BUILD
                Debug.LogError($"blast.ssmd.execute.pushc : failed to read compound data for pushc operation at codepointer {code_pointer}");
#endif
                return res;
            }
            switch (vector_size)
            {
                case 1:
                    push_register_f1();
                    break;
                case 2:
                    push_register_f2();
                    break;
                case 3:
                    push_register_f3();
                    break;
                case 4:
                    push_register_f4();
                    break;
                default:
#if DEVELOPMENT_BUILD
                    Debug.LogError("blast.ssmd push error: variable vector size not yet supported on stack push of compound");
#endif
                    return BlastError.error_variable_vector_compound_not_supported;
            }

            return BlastError.success;
        }

        BlastError pushf(ref int code_pointer, ref byte vector_size, [NoAlias]void* temp)
        {
            //
            //  TODO -> split out GetFunctionResult() from GetCompoundResult 
            //
            BlastError res = (BlastError)GetCompoundResult(temp, ref code_pointer, ref vector_size);
            if (res < 0)
            {
#if DEVELOPMENT_BUILD
                Debug.LogError($"blast.ssmd.execute.pushf: failed to read compound data for pushf operation at codepointer {code_pointer}");
#endif
                return res;
            }
            switch (vector_size)
            {
                case 1: push_register_f1(); break;
                case 2: push_register_f2(); break;
                case 3: push_register_f3(); break;
                case 4: push_register_f4(); break;
                default:
#if DEVELOPMENT_BUILD
                    Debug.LogError("blast.ssmd.interpretor.pushf error: variable vector size not yet supported on stack push");
#endif
                    return BlastError.error_variable_vector_op_not_supported;
            }

            return BlastError.success;
        }

        #endregion


        #region Assign[s|f] operation handlers

        BlastError assigns(ref int code_pointer, ref byte vector_size, [NoAlias]void* temp)
        {
            // assign 1 val;ue
            byte assignee_op = code[code_pointer];
            byte assignee = (byte)(assignee_op - BlastInterpretor.opt_id);
            byte s_assignee = BlastInterpretor.GetMetaDataSize(in metadata, in assignee);

            switch (s_assignee)
            {
                case 1: pop_fx_into<float>(code_pointer + 1, assignee); vector_size = 1; break;
                case 2: pop_fx_into<float2>(code_pointer + 1, assignee); vector_size = 2; break;
                case 3: pop_fx_into<float3>(code_pointer + 1, assignee); vector_size = 3; break;
                case 0:
                case 4: pop_fx_into<float4>(code_pointer + 1, assignee); vector_size = 4; break;
            }

            code_pointer += 2;

            return BlastError.success;
        }



        BlastError assign(ref int code_pointer, ref byte vector_size, [NoAlias]void* temp)
        {
            // advance 1 if on assign 
            code_pointer += math.select(0, 1, code[code_pointer] == (byte)blast_operation.assign);

            byte assignee_op = code[code_pointer];
            byte assignee = (byte)(assignee_op - BlastInterpretor.opt_id);

            // advance 2 if on begin 1 otherwise
            code_pointer += math.select(1, 2, code[code_pointer + 1] == (byte)blast_operation.begin);

            // get vectorsize of the assigned parameter 
            // vectorsize assigned MUST match 
            byte s_assignee = BlastInterpretor.GetMetaDataSize(in metadata, in assignee);

            // correct vectorsize 4 => it is 0 after compressing into 2 bits

            BlastError res = (BlastError)GetCompoundResult(temp, ref code_pointer, ref vector_size);
            if (res != BlastError.success)
            {
#if DEVELOPMENT_BUILD
                Debug.LogError($"blast.ssmd.assign: failed to read compound data for assign operation at codepointer {code_pointer}");
#endif
                return res;
            }

            // set assigned data fields in stack 
            switch ((byte)vector_size)
            {
                case (byte)BlastVectorSizes.float1:
                    //if(Unity.Burst.CompilerServices.Hint.Unlikely(s_assignee != 1))
                    if (s_assignee != 1)
                    {
#if DEVELOPMENT_BUILD
                        Debug.LogError($"blast.ssmd.assign: assigned vector size mismatch at #{code_pointer}, should be size '{s_assignee}', evaluated '1', data id = {assignee}");
#endif
                        return BlastError.error_assign_vector_size_mismatch;
                    }
                    set_data_from_register_f1(assignee);
                    break;

                case (byte)BlastVectorSizes.float2:
                    if (s_assignee != 2)
                    {
#if DEVELOPMENT_BUILD
                        Debug.LogError($"blast.ssmd.assign: assigned vector size mismatch at #{code_pointer}, should be size '{s_assignee}', evaluated '2'");
#endif
                        return BlastError.error_assign_vector_size_mismatch;
                    }
                    set_data_from_register_f2(assignee);
                    break;

                case (byte)BlastVectorSizes.float3:
                    if (s_assignee != 3)
                    {
#if DEVELOPMENT_BUILD
                        Debug.LogError($"blast.ssmd.assign: assigned vector size mismatch at #{code_pointer}, should be size '{s_assignee}', evaluated '3'");
#endif
                        return BlastError.error_assign_vector_size_mismatch;
                    }
                    set_data_from_register_f3(assignee);
                    break;

                case 0: // artifact from cutting 4 off at 2 bits 
                case (byte)BlastVectorSizes.float4:
                    if (s_assignee != 4)
                    {
#if DEVELOPMENT_BUILD
                        Debug.LogError($"blast.ssmd.assign: assigned vector size mismatch at #{code_pointer}, should be size '{s_assignee}', evaluated '4'");
#endif
                        return BlastError.error_assign_vector_size_mismatch;
                    }
                    set_data_from_register_f4(assignee);
                    break;

                default:
#if DEVELOPMENT_BUILD
                    Debug.LogError($"blast.ssmd.assign: vector size {vector_size} not allowed at codepointer {code_pointer}");
#endif
                    return BlastError.error_unsupported_operation_in_root;
            }

            return BlastError.success;
        }

        #endregion 


        /// <summary>
        /// main interpretor loop 
        /// </summary>
        /// <param name="datastack"></param>
        /// <param name="ssmd_data_count"></param>
        /// <param name="f4_register"></param>
        /// <returns></returns>
        int Execute([NoAlias]byte** datastack, int ssmd_data_count, float4* f4_register)
        {
            data = (void**)datastack;
            ssmd_datacount = ssmd_data_count;
            register = f4_register;

            // run with internal stacks? 
            if (package.HasStack)
            {
                stack = data;
                stack_offset = package.DataSegmentStackOffset / 4;
            }
            else
            {
                // allocate local stack 
                byte* p = stackalloc byte[package.StackSize * ssmd_datacount];

                // generate a list of datasegment pointers on the stack:
                byte** stack_indices = stackalloc byte*[ssmd_datacount];

                for (int i = 0; i < ssmd_data_count;
                    i++)
                {
                    stack_indices[i] = p + (package.StackSize * i);
                }

                stack = (void**)stack_indices;
                stack_offset = 0;
            }

            // local temp buffer 
            void* temp = stackalloc float4[ssmd_datacount];  //temp buffer of max vectorsize * 4 * ssmd_datacount 

            // assume all data is set 
            int iterations = 0;
            int ires = 0; // anything < 0 == error
            code_pointer = 0;

            // start the main interpretor loop 
            while (code_pointer < package.CodeSize)
            {
                iterations++;
                if (iterations > BlastInterpretor.max_iterations) return (int)BlastError.error_max_iterations;

                byte vector_size = 1;
                blast_operation op = (blast_operation)code[code_pointer];
                code_pointer++;

                switch (op)
                {
                    case blast_operation.ret: return 0;
                    case blast_operation.begin: // jumping in from somewhere
                    case blast_operation.end:
                    case blast_operation.nop: break;

                    //
                    // push value or compound result 
                    //
                    case blast_operation.push:
                        if ((ires = (int)push(ref code_pointer, ref vector_size, temp)) < 0) return ires;
                        break;

                    //
                    // pushv knows more about what it is pushing up the stack, it can do it without running the compound 
                    //
                    case blast_operation.pushv:
                        if ((ires = (int)pushv(ref code_pointer, ref vector_size, temp)) < 0) return ires;
                        break;

                    //
                    // push the result from a function directly onto the stack, saving a call to getcompound 
                    //
                    case blast_operation.pushf:
                        if ((ires = (int)pushf(ref code_pointer, ref vector_size, temp)) < 0) return ires;
                        break;
                    //
                    // push the result from a compound directly onto the stack 
                    //
                    case blast_operation.pushc:
                        if ((ires = (int)pushc(ref code_pointer, ref vector_size, temp)) < 0) return ires;
                        break;


                    case blast_operation.assigns:
                        if ((ires = (int)assigns(ref code_pointer, ref vector_size, temp)) < 0) return ires;
                        break;



#pragma warning disable CS1587 // XML comment is not placed on a valid language element
                    /// assign result of a compound to some dataindex in the datasegment 

                    case blast_operation.assign:
#pragma warning restore CS1587 // XML comment is not placed on a valid language element
                        if ((ires = (int)assign(ref code_pointer, ref vector_size, temp)) < 0) return ires;
                        break;


                    //
                    // Extended Op == 255 => next byte encodes an operation not used in switch above (more non standard ops)
                    //
                    case blast_operation.ex_op:
                        {
                            extended_blast_operation exop = (extended_blast_operation)code[code_pointer];
                            switch (exop)
                            {
                                /*                                case extended_blast_operation.call:
                                                                    {
                                                                        // call external function, any returned data is neglected on root 
                                                                        bool minus = false;
                                                                        CallExternal(ref code_pointer, ref minus, ref vector_size);
                                                                        code_pointer++;
                                                                    }
                                                                    break;*/
                                /*
                                                                case extended_blast_operation.debugstack:
                                                                    {
                                                                        // write contents of stack to the debug stream as a detailed report; 
                                                                        // write contents of a data element to the debug stream 
                                                                        code_pointer++;
                                #if DEVELOPMENT_BUILD
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

                                #if DEVELOPMENT_BUILD
                                                                        Handle_DebugData(code_pointer, vector_size, op_id, datatype, pdata);
                                #endif
                                                                        code_pointer++;
                                                                    }
                                                                    break;
                                                                */
                                default:
                                    {
#if DEVELOPMENT_BUILD
                                        Debug.LogError($"blast.ssmd.extended op: only call and debug operation is supported from root, not {exop} ");
#endif
                                        return (int)BlastError.error_unsupported_operation_in_ssmd_root;
                                    }
                            }
#pragma warning disable CS0162 // Unreachable code detected
                            break;
#pragma warning restore CS0162 // Unreachable code detected
                        }

                    default:
                        {
#if DEVELOPMENT_BUILD
                            Debug.LogError($"blast.ssmd: operation {(byte)op} '{op}' not supported in root codepointer = {code_pointer}");
#endif
                            return (int)BlastError.error_unsupported_operation_in_root;
                        }
                }
            }

            return (int)BlastError.success;
        }

        #endregion
    }
}
