
#if STANDALONE_VSBUILD
#if DEVELOPMENT_BUILD
#define HANDLE_DEBUG_OP
#endif

using NSS.Blast.Standalone;
using Unity.Assertions;
#else 
    using UnityEngine;
    using UnityEngine.Assertions;
#endif 

using NSS.Blast.Interpretor;
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

        /// <summary>
        /// if true, the script is executed in validation mode:
        /// - external calls just return 0's
        /// </summary>
        public bool ValidationMode;


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

        void pop_fn_into(in int code_pointer, [NoAlias]float4* destination, bool minus, out byte vector_size)
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
        /// pop a float1 value form stack|data|constant source and put it in destination 
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
        /// pop an f1 from stack|data|constant and move it into f4.x
        /// </summary>
        void pop_f1_into_f4(in int code_pointer, [NoAlias]float4* destination)
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
                    Debug.LogError($"blast.ssmd.stack.pop_f1_into_f4 -> data mismatch, expecting numeric of size 1, found {type} of size {size} at data offset {c - BlastInterpretor.opt_id}");
                    return;
                }
#endif
                // variable acccess
                for (int i = 0; i < ssmd_datacount; i++)
                {
                    destination[i].x = ((float*)data[i])[c - BlastInterpretor.opt_id];
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
                    Debug.LogError($"blast.ssmd.stack.pop_f1_into_f4 -> stackdata mismatch, expecting numeric of size 1, found {type} of size {size} at stack offset {stack_offset}");
                    return;
                }
#endif
                // stack pop 
                stack_offset = stack_offset - 1;
                for (int i = 0; i < ssmd_datacount; i++)
                {
                    destination[i].x = ((float*)stack[i])[stack_offset];
                }
            }
            else
            if (c >= BlastInterpretor.opt_value)
            {
                float constant = engine_ptr->constants[c];
                for (int i = 0; i < ssmd_datacount; i++)
                {
                    destination[i].x = constant;
                }
            }
            else
            {
                // error or.. constant by operation value.... 
#if DEVELOPMENT_BUILD
                Debug.LogError("blast.ssmd.stack.pop_f1_into_f4 -> select op by constant value is not supported");
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

        /// <summary>
        /// pop operation [stack/constant/variabledata] and move it into dataindex 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="code_pointer"></param>
        /// <param name="dataindex"></param>
        void pop_fx_into<T>(in int code_pointer, in byte dataindex) where T : unmanaged
        {
            int vector_size = sizeof(T) / 4;

            // get datatype of destination, it should match T
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

                    blast_operation c = (blast_operation)code[code_pointer];
                    if (c == blast_operation.pop)
                    {
                        //
                        // STACK operation 
                        //

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
                    }
                    else
                    if (c >= blast_operation.id)
                    {
                        //
                        // DATASEG operation
                        // 
                        int source_index = (int)c - (int)BlastInterpretor.opt_id;
                        if (source_index == dataindex)
                        {
                            // dont need to move anything .. 
                            // if this would ever be the case, compiler should be checked
                            return;
                        }

#if DEVELOPMENT_BUILD
                        // validate source index
                        BlastVariableDataType type = BlastInterpretor.GetMetaDataType(metadata, (byte)(source_index));
                        int size = BlastInterpretor.GetMetaDataSize(metadata, (byte)(source_index));
                        if (size != vector_size || type != BlastVariableDataType.Numeric)
                        {
                            Debug.LogError($"blast.ssmd.pop_fx_into<T>(assignee = {dataindex}) -> datasegment mismatch, expecting numeric of size {vector_size}, found {type} of size {size} at stack offset {stack_offset}");
                            return;
                        }
#endif

                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            ((T*)(void*)&((float*)data[i])[dataindex])[0] = ((T*)(void*)&((float*)data[i])[source_index])[0];
                        }
                    }
                    else
                    if (c >= blast_operation.pi)
                    {
                        //
                        // CONSTANT operation                         
                        //

                        // set a vector of max size, and get a pointer to it
                        // regardless of vector_size(T) we still would write correct value 
                        float4 constant = engine_ptr->constants[(int)c];
                        {
                            for (int i = 0; i < ssmd_datacount; i++)
                            {
                                ((T*)(void*)&((float*)data[i])[dataindex])[0] = ((T*)(void*)(&constant))[0];
                            }
                        }

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
        /// pop a float from stack and assign it to dataindex inside a datasegment 
        /// - shouldt the compiler just directly assign the dataindex (if possible)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="code_pointer"></param>
        /// <param name="dataindex"></param>
        void pop_stack_fx_into<T>(in int code_pointer, byte dataindex) where T : unmanaged
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
        /// pop a float and assign it to dataindex inside a datasegment, if there is a minus sign at the poplocation pop the next and keep the sign
        /// </summary>
        void pop_f1_into_with_minus(in int code_pointer, byte dataindex, ref int cp)
        {
            BlastVariableDataType datatype = BlastInterpretor.GetMetaDataType(metadata, dataindex);
            int datasize = BlastInterpretor.GetMetaDataSize(metadata, dataindex);

            // we get either a pop instruction or an instruction to get a value possibly preceded by a minus sign 
            byte c = code[code_pointer];

            bool minus = false;
            if (c == (byte)blast_operation.substract)
            {
                cp++;
                c = code[code_pointer + 1];
                minus = true;
            }

            if (c >= BlastInterpretor.opt_id)
            {
#if DEVELOPMENT_BUILD
                BlastVariableDataType type = BlastInterpretor.GetMetaDataType(metadata, (byte)(c - BlastInterpretor.opt_id));
                int size = BlastInterpretor.GetMetaDataSize(metadata, (byte)(c - BlastInterpretor.opt_id));
                if (size != 1 || type != BlastVariableDataType.Numeric)
                {
                    Debug.LogError($"Blast SSMD Interpretor: pop_f1_into_with_minus -> data mismatch, expecting numeric of size 1, found {type} of size {size} at data offset {c - BlastInterpretor.opt_id}");
                    return;
                }
#endif
                // variable acccess: copy variable to variable 
                if (!minus)
                {
                    for (int i = 0; i < ssmd_datacount; i++)
                    {
                        ((float*)data[i])[dataindex] = ((float*)data[i])[c - BlastInterpretor.opt_id];
                    }
                }
                else
                {
                    for (int i = 0; i < ssmd_datacount; i++)
                    {
                        ((float*)data[i])[dataindex] = -((float*)data[i])[c - BlastInterpretor.opt_id];
                    }

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
                    Debug.LogError($"Blast SSMD Interpretor: pop_f1_into_with_minus -> stackdata mismatch, expecting numeric of size 1, found {type} of size {size} at stack offset {stack_offset}");
                    return;
                }
#endif
                // stack pop 
                stack_offset = stack_offset - 1;

                if (!minus)
                {
                    for (int i = 0; i < ssmd_datacount; i++)
                    {
                        ((float*)data[i])[dataindex] = ((float*)stack[i])[stack_offset];
                    }
                }
                else
                {
                    for (int i = 0; i < ssmd_datacount; i++)
                    {
                        ((float*)data[i])[dataindex] = -((float*)stack[i])[stack_offset];
                    }
                }
            }
            else
            if (c >= BlastInterpretor.opt_value)
            {
                float constant = engine_ptr->constants[c];
                constant = math.select(constant, -constant, minus);
                for (int i = 0; i < ssmd_datacount; i++)
                {
                    ((float*)data[i])[dataindex] = constant;
                }
            }
            else
            {
                // error or.. constant by operation value.... 
#if DEVELOPMENT_BUILD
                Debug.LogError("Blast SSMD Interpretor: pop_f1_into_with_minus -> select op by constant value is not supported");
#endif
            }
        }


        /// <summary>
        /// pop a float and assign it to dataindex inside a datasegment, if there is a minus sign at the poplocation pop the next and keep the sign
        /// </summary>
        void pop_f2_into_with_minus(in int code_pointer, byte dataindex, ref int cp)
        {
            BlastVariableDataType datatype = BlastInterpretor.GetMetaDataType(metadata, dataindex);
            int datasize = BlastInterpretor.GetMetaDataSize(metadata, dataindex);

            // we get either a pop instruction or an instruction to get a value possibly preceded by a minus sign 
            byte c = code[code_pointer];

            bool minus = false;
            if (c == (byte)blast_operation.substract)
            {
                cp++;
                c = code[code_pointer + 1];
                minus = true;
            }

            if (c >= BlastInterpretor.opt_id)
            {
#if DEVELOPMENT_BUILD
                BlastVariableDataType type = BlastInterpretor.GetMetaDataType(metadata, (byte)(c - BlastInterpretor.opt_id));
                int size = BlastInterpretor.GetMetaDataSize(metadata, (byte)(c - BlastInterpretor.opt_id));
                if (size != 2 || type != BlastVariableDataType.Numeric)
                {
                    Debug.LogError($"Blast SSMD Interpretor: pop_f2_into_with_minus -> data mismatch, expecting numeric of size 1, found {type} of size {size} at data offset {c - BlastInterpretor.opt_id}");
                    return;
                }
#endif
                // variable acccess: copy variable to variable 
                if (!minus)
                {
                    for (int i = 0; i < ssmd_datacount; i++)
                    {
                        ((float*)data[i])[dataindex] = ((float*)data[i])[c - BlastInterpretor.opt_id];
                        ((float*)data[i])[dataindex + 1] = ((float*)data[i])[c - BlastInterpretor.opt_id + 1];
                    }
                }
                else
                {
                    for (int i = 0; i < ssmd_datacount; i++)
                    {
                        ((float*)data[i])[dataindex] = -((float*)data[i])[c - BlastInterpretor.opt_id];
                        ((float*)data[i])[dataindex + 1] = -((float*)data[i])[c - BlastInterpretor.opt_id + 1];
                    }
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
                    Debug.LogError($"Blast SSMD Interpretor: pop_f2_into_with_minus -> stackdata mismatch, expecting numeric of size 1, found {type} of size {size} at stack offset {stack_offset}");
                    return;
                }
#endif
                // stack pop 
                stack_offset = stack_offset - 2;

                if (!minus)
                {
                    for (int i = 0; i < ssmd_datacount; i++)
                    {
                        ((float*)data[i])[dataindex] = ((float*)stack[i])[stack_offset];
                        ((float*)data[i])[dataindex + 1] = ((float*)stack[i])[stack_offset + 1];
                    }
                }
                else
                {
                    for (int i = 0; i < ssmd_datacount; i++)
                    {
                        ((float*)data[i])[dataindex] = -((float*)stack[i])[stack_offset];
                        ((float*)data[i])[dataindex + 1] = -((float*)stack[i])[stack_offset + 1];
                    }
                }
            }
            else
            if (c >= BlastInterpretor.opt_value)
            {
                float constant = engine_ptr->constants[c];
                constant = math.select(constant, -constant, minus);
                for (int i = 0; i < ssmd_datacount; i++)
                {
                    ((float*)data[i])[dataindex] = constant;
                    ((float*)data[i])[dataindex + 1] = constant;
                }
            }
            else
            {
                // error or.. constant by operation value.... 
#if DEVELOPMENT_BUILD
                Debug.LogError("Blast SSMD Interpretor: pop_f2_into_with_minus -> select op by constant value is not supported");
#endif
            }
        }


        /// <summary>
        /// pop a float and assign it to dataindex inside a datasegment, if there is a minus sign at the poplocation pop the next and keep the sign
        /// </summary>
        void pop_f3_into_with_minus(in int code_pointer, byte dataindex, ref int cp)
        {
            BlastVariableDataType datatype = BlastInterpretor.GetMetaDataType(metadata, dataindex);
            int datasize = BlastInterpretor.GetMetaDataSize(metadata, dataindex);

            // we get either a pop instruction or an instruction to get a value possibly preceded by a minus sign 
            byte c = code[code_pointer];

            bool minus = false;
            if (c == (byte)blast_operation.substract)
            {
                cp++;
                c = code[code_pointer + 1];
                minus = true;
            }

            if (c >= BlastInterpretor.opt_id)
            {
#if DEVELOPMENT_BUILD
                BlastVariableDataType type = BlastInterpretor.GetMetaDataType(metadata, (byte)(c - BlastInterpretor.opt_id));
                int size = BlastInterpretor.GetMetaDataSize(metadata, (byte)(c - BlastInterpretor.opt_id));
                if (size != 3 || type != BlastVariableDataType.Numeric)
                {
                    Debug.LogError($"Blast SSMD Interpretor: pop_f3_into_with_minus -> data mismatch, expecting numeric of size 1, found {type} of size {size} at data offset {c - BlastInterpretor.opt_id}");
                    return;
                }
#endif
                // variable acccess: copy variable to variable 
                if (!minus)
                {
                    for (int i = 0; i < ssmd_datacount; i++)
                    {
                        ((float*)data[i])[dataindex] = ((float*)data[i])[c - BlastInterpretor.opt_id];
                        ((float*)data[i])[dataindex + 1] = ((float*)data[i])[c - BlastInterpretor.opt_id + 1];
                        ((float*)data[i])[dataindex + 2] = ((float*)data[i])[c - BlastInterpretor.opt_id + 2];
                    }
                }
                else
                {
                    for (int i = 0; i < ssmd_datacount; i++)
                    {
                        ((float*)data[i])[dataindex] = -((float*)data[i])[c - BlastInterpretor.opt_id];
                        ((float*)data[i])[dataindex + 1] = -((float*)data[i])[c - BlastInterpretor.opt_id + 1];
                        ((float*)data[i])[dataindex + 2] = -((float*)data[i])[c - BlastInterpretor.opt_id + 2];
                    }
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
                    Debug.LogError($"Blast SSMD Interpretor: pop_f3_into_with_minus -> stackdata mismatch, expecting numeric of size 1, found {type} of size {size} at stack offset {stack_offset}");
                    return;
                }
#endif
                // stack pop 
                stack_offset = stack_offset - 3;

                if (!minus)
                {
                    for (int i = 0; i < ssmd_datacount; i++)
                    {
                        ((float*)data[i])[dataindex] = ((float*)stack[i])[stack_offset];
                        ((float*)data[i])[dataindex + 1] = ((float*)stack[i])[stack_offset + 1];
                        ((float*)data[i])[dataindex + 2] = ((float*)stack[i])[stack_offset + 2];
                    }
                }
                else
                {
                    for (int i = 0; i < ssmd_datacount; i++)
                    {
                        ((float*)data[i])[dataindex] = -((float*)stack[i])[stack_offset];
                        ((float*)data[i])[dataindex + 1] = -((float*)stack[i])[stack_offset + 1];
                        ((float*)data[i])[dataindex + 2] = -((float*)stack[i])[stack_offset + 2];
                    }
                }
            }
            else
            if (c >= BlastInterpretor.opt_value)
            {
                float constant = engine_ptr->constants[c];
                constant = math.select(constant, -constant, minus);
                for (int i = 0; i < ssmd_datacount; i++)
                {
                    ((float*)data[i])[dataindex] = constant;
                    ((float*)data[i])[dataindex + 1] = constant;
                    ((float*)data[i])[dataindex + 2] = constant;
                }
            }
            else
            {
                // error or.. constant by operation value.... 
#if DEVELOPMENT_BUILD
                Debug.LogError("Blast SSMD Interpretor: pop_f3_into_with_minus -> select op by constant value is not supported");
#endif
            }
        }

        /// <summary>
        /// pop a float and assign it to dataindex inside a datasegment, if there is a minus sign at the poplocation pop the next and keep the sign
        /// </summary>
        void pop_f4_into_with_minus(in int code_pointer, byte dataindex, ref int cp)
        {
            BlastVariableDataType datatype = BlastInterpretor.GetMetaDataType(metadata, dataindex);
            int datasize = BlastInterpretor.GetMetaDataSize(metadata, dataindex);

            // we get either a pop instruction or an instruction to get a value possibly preceded by a minus sign 
            byte c = code[code_pointer];

            bool minus = false;
            if (c == (byte)blast_operation.substract)
            {
                cp++;
                c = code[code_pointer + 1];
                minus = true;
            }

            if (c >= BlastInterpretor.opt_id)
            {
#if DEVELOPMENT_BUILD
                BlastVariableDataType type = BlastInterpretor.GetMetaDataType(metadata, (byte)(c - BlastInterpretor.opt_id));
                int size = BlastInterpretor.GetMetaDataSize(metadata, (byte)(c - BlastInterpretor.opt_id));
                if (size != 4 || type != BlastVariableDataType.Numeric)
                {
                    Debug.LogError($"Blast SSMD Interpretor: pop_f4_into_with_minus -> data mismatch, expecting numeric of size 1, found {type} of size {size} at data offset {c - BlastInterpretor.opt_id}");
                    return;
                }
#endif
                // variable acccess: copy variable to variable 
                if (!minus)
                {
                    for (int i = 0; i < ssmd_datacount; i++)
                    {
                        ((float*)data[i])[dataindex] = ((float*)data[i])[c - BlastInterpretor.opt_id];
                        ((float*)data[i])[dataindex + 1] = ((float*)data[i])[c - BlastInterpretor.opt_id + 1];
                        ((float*)data[i])[dataindex + 2] = ((float*)data[i])[c - BlastInterpretor.opt_id + 2];
                        ((float*)data[i])[dataindex + 3] = ((float*)data[i])[c - BlastInterpretor.opt_id + 3];
                    }
                }
                else
                {
                    for (int i = 0; i < ssmd_datacount; i++)
                    {
                        ((float*)data[i])[dataindex] = -((float*)data[i])[c - BlastInterpretor.opt_id];
                        ((float*)data[i])[dataindex + 1] = -((float*)data[i])[c - BlastInterpretor.opt_id + 1];
                        ((float*)data[i])[dataindex + 2] = -((float*)data[i])[c - BlastInterpretor.opt_id + 2];
                        ((float*)data[i])[dataindex + 3] = -((float*)data[i])[c - BlastInterpretor.opt_id + 3];
                    }
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
                    Debug.LogError($"Blast SSMD Interpretor: pop_f4_into_with_minus -> stackdata mismatch, expecting numeric of size 1, found {type} of size {size} at stack offset {stack_offset}");
                    return;
                }
#endif
                // stack pop 
                stack_offset = stack_offset - 4;

                if (!minus)
                {
                    for (int i = 0; i < ssmd_datacount; i++)
                    {
                        ((float*)data[i])[dataindex] = ((float*)stack[i])[stack_offset];
                        ((float*)data[i])[dataindex + 1] = ((float*)stack[i])[stack_offset + 1];
                        ((float*)data[i])[dataindex + 2] = ((float*)stack[i])[stack_offset + 2];
                        ((float*)data[i])[dataindex + 3] = ((float*)stack[i])[stack_offset + 3];
                    }
                }
                else
                {
                    for (int i = 0; i < ssmd_datacount; i++)
                    {
                        ((float*)data[i])[dataindex] = -((float*)stack[i])[stack_offset];
                        ((float*)data[i])[dataindex + 1] = -((float*)stack[i])[stack_offset + 1];
                        ((float*)data[i])[dataindex + 2] = -((float*)stack[i])[stack_offset + 2];
                        ((float*)data[i])[dataindex + 3] = -((float*)stack[i])[stack_offset + 3];
                    }
                }
            }
            else
            if (c >= BlastInterpretor.opt_value)
            {
                float constant = engine_ptr->constants[c];
                constant = math.select(constant, -constant, minus);
                for (int i = 0; i < ssmd_datacount; i++)
                {
                    ((float*)data[i])[dataindex] = constant;
                    ((float*)data[i])[dataindex + 1] = constant;
                    ((float*)data[i])[dataindex + 2] = constant;
                    ((float*)data[i])[dataindex + 3] = constant;
                }
            }
            else
            {
                // error or.. constant by operation value.... 
#if DEVELOPMENT_BUILD
                Debug.LogError("Blast SSMD Interpretor: pop_f4_into_with_minus -> select op by constant value is not supported");
#endif
            }
        }


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


        #endregion

        #region pop_fx_with_op_into_fx

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
            if (!Blast.IsOperationSSMDHandled(op))
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

                    case blast_operation.or:
                        for (int i = 0; i < ssmd_datacount; i++) output[i] = math.select(0, 1f, buffer[i] != 0 || ((float*)data[i])[c - BlastInterpretor.opt_id] != 0);
                        break;

                    case blast_operation.min:
                        for (int i = 0; i < ssmd_datacount; i++) output[i] = math.min(buffer[i], ((float*)data[i])[c - BlastInterpretor.opt_id]);
                        break;

                    case blast_operation.max:
                        for (int i = 0; i < ssmd_datacount; i++) output[i] = math.min(buffer[i], ((float*)data[i])[c - BlastInterpretor.opt_id]);
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

                    case blast_operation.or:
                        for (int i = 0; i < ssmd_datacount; i++) output[i] = math.select(0, 1f, buffer[i] != 0 || ((float*)stack[i])[stack_offset] != 0);
                        break;

                    case blast_operation.min:
                        for (int i = 0; i < ssmd_datacount; i++) output[i] = math.min(buffer[i], ((float*)stack[i])[stack_offset]);
                        break;

                    case blast_operation.max:
                        for (int i = 0; i < ssmd_datacount; i++) output[i] = math.min(buffer[i], ((float*)stack[i])[stack_offset]);
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
                        bool2 b = new bool2(constant != 0, constant != 0);
                        if (constant == 0)
                        {
                            for (int i = 0; i < ssmd_datacount; i++) output[i] = 0f;
                        }
                        else
                        {
                            for (int i = 0; i < ssmd_datacount; i++) output[i] = math.select(0, 1f, buffer[i] != 0);
                        }
                        break;

                    case blast_operation.or:
                        if (constant == 0)
                        {
                            // nothing changes if its the same buffer so skip doing stuff in that case
                            if (output != buffer)
                            {
                                // if not the same buffer then we still need to copy the data 
                                for (int i = 0; i < ssmd_datacount; i++) output[i] = buffer[i];
                            }
                        }
                        else
                        {
                            // just write true,  | true is always true 
                            for (int i = 0; i < ssmd_datacount; i++) output[i] = 1f;
                        }
                        break;

                    case blast_operation.min:
                        for (int i = 0; i < ssmd_datacount; i++) output[i] = math.min(buffer[i], constant);
                        break;

                    case blast_operation.max:
                        for (int i = 0; i < ssmd_datacount; i++) output[i] = math.max(buffer[i], constant);
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
        void pop_f2_with_op_into_f2(in int code_pointer, [NoAlias]float2* buffer, [NoAlias]float2* output, blast_operation op)
        {
#if DEVELOPMENT_BUILD
            if (!Blast.IsOperationSSMDHandled(op))
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

                    case blast_operation.and:
                        for (int i = 0; i < ssmd_datacount; i++) output[i] = math.select(0, 1f, math.any(buffer[i]) && math.any(((float2*)(void*)&((float*)data[i])[c - BlastInterpretor.opt_id])[0]));
                        break;

                    case blast_operation.or:
                        for (int i = 0; i < ssmd_datacount; i++) output[i] = math.select(0, 1f, math.any(buffer[i]) || math.any(((float2*)(void*)&((float*)data[i])[c - BlastInterpretor.opt_id])[0]));
                        break;

                    case blast_operation.min:
                        for (int i = 0; i < ssmd_datacount; i++) output[i] = math.min(buffer[i], ((float2*)(void*)&((float*)data[i])[c - BlastInterpretor.opt_id])[0]);
                        break;

                    case blast_operation.max:
                        for (int i = 0; i < ssmd_datacount; i++) output[i] = math.min(buffer[i], ((float2*)(void*)&((float*)data[i])[c - BlastInterpretor.opt_id])[0]);
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

                    case blast_operation.and:
                        for (int i = 0; i < ssmd_datacount; i++) output[i] = math.select(0, 1f, math.any(buffer[i]) && math.any(((float2*)(void*)&((float*)stack[i])[stack_offset])[0]));
                        break;

                    case blast_operation.or:
                        for (int i = 0; i < ssmd_datacount; i++) output[i] = math.select(0, 1f, math.any(buffer[i]) || math.any(((float2*)(void*)&((float*)stack[i])[stack_offset])[0]));
                        break;

                    case blast_operation.min:
                        for (int i = 0; i < ssmd_datacount; i++) output[i] = math.min(buffer[i], ((float2*)(void*)&((float*)stack[i])[stack_offset])[0]);
                        break;

                    case blast_operation.max:
                        for (int i = 0; i < ssmd_datacount; i++) output[i] = math.min(buffer[i], ((float2*)(void*)&((float*)stack[i])[stack_offset])[0]);
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

                    case blast_operation.and:
                        bool2 b = new bool2(constant != 0, constant != 0);
                        if (constant == 0)
                        {
                            for (int i = 0; i < ssmd_datacount; i++) output[i] = 0f;
                        }
                        else
                        {
                            for (int i = 0; i < ssmd_datacount; i++) output[i] = math.select(0, 1f, buffer[i] != 0);
                        }
                        break;

                    case blast_operation.or:
                        if (constant == 0)
                        {
                            // nothing changes if its the same buffer so skip doing stuff in that case
                            if (output != buffer)
                            {
                                // if not the same buffer then we still need to copy the data 
                                for (int i = 0; i < ssmd_datacount; i++) output[i] = buffer[i];
                            }
                        }
                        else
                        {
                            // just write true,  | true is always true 
                            for (int i = 0; i < ssmd_datacount; i++) output[i] = 1f;
                        }
                        break;

                    case blast_operation.min:
                        for (int i = 0; i < ssmd_datacount; i++) output[i] = math.min(buffer[i], constant);
                        break;

                    case blast_operation.max:
                        for (int i = 0; i < ssmd_datacount; i++) output[i] = math.max(buffer[i], constant);
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
        void pop_f3_with_op_into_f3(in int code_pointer, [NoAlias]float3* buffer, [NoAlias]float3* output, blast_operation op)
        {
#if DEVELOPMENT_BUILD
            if (!Blast.IsOperationSSMDHandled(op))
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

                    case blast_operation.and:
                        for (int i = 0; i < ssmd_datacount; i++) output[i] = math.select(0, 1f, math.any(buffer[i]) && math.any(((float3*)(void*)&((float*)data[i])[c - BlastInterpretor.opt_id])[0]));
                        break;

                    case blast_operation.or:
                        for (int i = 0; i < ssmd_datacount; i++) output[i] = math.select(0, 1f, math.any(buffer[i]) || math.any(((float3*)(void*)&((float*)data[i])[c - BlastInterpretor.opt_id])[0]));
                        break;

                    case blast_operation.min:
                        for (int i = 0; i < ssmd_datacount; i++) output[i] = math.min(buffer[i], ((float3*)(void*)&((float*)data[i])[c - BlastInterpretor.opt_id])[0]);
                        break;

                    case blast_operation.max:
                        for (int i = 0; i < ssmd_datacount; i++) output[i] = math.min(buffer[i], ((float3*)(void*)&((float*)data[i])[c - BlastInterpretor.opt_id])[0]);
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

                    case blast_operation.and:
                        for (int i = 0; i < ssmd_datacount; i++) output[i] = math.select(0, 1f, math.any(buffer[i]) && math.any(((float3*)(void*)&((float*)stack[i])[stack_offset])[0]));
                        break;

                    case blast_operation.or:
                        for (int i = 0; i < ssmd_datacount; i++) output[i] = math.select(0, 1f, math.any(buffer[i]) || math.any(((float3*)(void*)&((float*)stack[i])[stack_offset])[0]));
                        break;

                    case blast_operation.min:
                        for (int i = 0; i < ssmd_datacount; i++) output[i] = math.min(buffer[i], ((float3*)(void*)&((float*)stack[i])[stack_offset])[0]);
                        break;

                    case blast_operation.max:
                        for (int i = 0; i < ssmd_datacount; i++) output[i] = math.min(buffer[i], ((float3*)(void*)&((float*)stack[i])[stack_offset])[0]);
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

                    case blast_operation.and:
                        if (constant == 0)
                        {
                            for (int i = 0; i < ssmd_datacount; i++) output[i] = 0f;
                        }
                        else
                        {
                            for (int i = 0; i < ssmd_datacount; i++) output[i] = math.select(0, 1f, buffer[i] != 0);
                        }
                        break;

                    case blast_operation.or:
                        if (constant == 0)
                        {
                            // nothing changes if its the same buffer so skip doing stuff in that case
                            if (output != buffer)
                            {
                                // if not the same buffer then we still need to copy the data 
                                for (int i = 0; i < ssmd_datacount; i++) output[i] = buffer[i];
                            }
                        }
                        else
                        {
                            // just write true,  | true is always true 
                            for (int i = 0; i < ssmd_datacount; i++) output[i] = 1f;
                        }
                        break;

                    case blast_operation.min:
                        for (int i = 0; i < ssmd_datacount; i++) output[i] = math.min(buffer[i], constant);
                        break;

                    case blast_operation.max:
                        for (int i = 0; i < ssmd_datacount; i++) output[i] = math.max(buffer[i], constant);
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
        void pop_f4_with_op_into_f4(in int code_pointer, [NoAlias]float4* buffer, [NoAlias]float4* output, blast_operation op)
        {
#if DEVELOPMENT_BUILD
            if (!Blast.IsOperationSSMDHandled(op))
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
                if ((size != 4 && size != 0) || type != BlastVariableDataType.Numeric)
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
                            // whats faster? casting like this or nicely as float with 2 muls  TODO
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

                    case blast_operation.and:
                        for (int i = 0; i < ssmd_datacount; i++) output[i] = math.select(0, 1f, math.any(buffer[i]) && math.any(((float4*)(void*)&((float*)stack[i])[stack_offset])[0]));
                        break;

                    case blast_operation.or:
                        for (int i = 0; i < ssmd_datacount; i++) output[i] = math.select(0, 1f, math.any(buffer[i]) || math.any(((float4*)(void*)&((float*)stack[i])[stack_offset])[0]));
                        break;

                    case blast_operation.min:
                        for (int i = 0; i < ssmd_datacount; i++) output[i] = math.min(buffer[i], ((float4*)(void*)&((float*)stack[i])[stack_offset])[0]);
                        break;

                    case blast_operation.max:
                        for (int i = 0; i < ssmd_datacount; i++) output[i] = math.min(buffer[i], ((float4*)(void*)&((float*)stack[i])[stack_offset])[0]);
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

                    case blast_operation.and:
                        for (int i = 0; i < ssmd_datacount; i++) output[i] = math.select(0, 1f, math.any(buffer[i]) && math.any(((float4*)(void*)&((float*)stack[i])[stack_offset])[0]));
                        break;

                    case blast_operation.or:
                        for (int i = 0; i < ssmd_datacount; i++) output[i] = math.select(0, 1f, math.any(buffer[i]) || math.any(((float4*)(void*)&((float*)stack[i])[stack_offset])[0]));
                        break;

                    case blast_operation.min:
                        for (int i = 0; i < ssmd_datacount; i++) output[i] = math.min(buffer[i], ((float4*)(void*)&((float*)stack[i])[stack_offset])[0]);
                        break;

                    case blast_operation.max:
                        for (int i = 0; i < ssmd_datacount; i++) output[i] = math.min(buffer[i], ((float4*)(void*)&((float*)stack[i])[stack_offset])[0]);
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

                    case blast_operation.and:
                        if (constant == 0)
                        {
                            for (int i = 0; i < ssmd_datacount; i++) output[i] = 0f;
                        }
                        else
                        {
                            for (int i = 0; i < ssmd_datacount; i++) output[i] = math.select(0, 1f, buffer[i] != 0);
                        }
                        break;

                    case blast_operation.or:
                        if (constant == 0)
                        {
                            // nothing changes if its the same buffer so skip doing stuff in that case
                            if (output != buffer)
                            {
                                // if not the same buffer then we still need to copy the data 
                                for (int i = 0; i < ssmd_datacount; i++) output[i] = buffer[i];
                            }
                        }
                        else
                        {
                            // just write true,  | true is always true 
                            for (int i = 0; i < ssmd_datacount; i++) output[i] = 1f;
                        }
                        break;

                    case blast_operation.min:
                        for (int i = 0; i < ssmd_datacount; i++) output[i] = math.min(buffer[i], constant);
                        break;

                    case blast_operation.max:
                        for (int i = 0; i < ssmd_datacount; i++) output[i] = math.max(buffer[i], constant);
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
        void pop_f1_with_op_into_f4(in int code_pointer, [NoAlias]float* buffer, [NoAlias]float4* output, blast_operation op)
        {
#if DEVELOPMENT_BUILD
            if (!Blast.IsOperationSSMDHandled(op))
            {
                Debug.LogError($"blast.ssmd.interpretor.pop_f1_with_op_into_f4: unsupported operation supplied: {op}, only + - * / are supported");
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
                    Debug.LogError($"blast.ssmd.interpretor.pop_f1_with_op_into_f4: data mismatch, expecting numeric of size 1, found {type} of size {size} at data offset {c - BlastInterpretor.opt_id} with operation: {op}");
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

                    case blast_operation.and:
                        for (int i = 0; i < ssmd_datacount; i++) output[i].x = math.select(0, 1f, buffer[i] != 0 && ((float*)data[i])[c - BlastInterpretor.opt_id] != 0);
                        break;

                    case blast_operation.or:
                        for (int i = 0; i < ssmd_datacount; i++) output[i].x = math.select(0, 1f, buffer[i] != 0 || ((float*)data[i])[c - BlastInterpretor.opt_id] != 0);
                        break;

                    case blast_operation.min:
                        for (int i = 0; i < ssmd_datacount; i++) output[i].x = math.min(buffer[i], ((float*)data[i])[c - BlastInterpretor.opt_id]);
                        break;

                    case blast_operation.max:
                        for (int i = 0; i < ssmd_datacount; i++) output[i].x = math.min(buffer[i], ((float*)data[i])[c - BlastInterpretor.opt_id]);
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
                    Debug.LogError($"blast.ssmd.interpretor.pop_f1_with_op_into_f4: stackdata mismatch, expecting numeric of size 1, found {type} of size {size} at stack offset {stack_offset} with operation: {op}");
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

                    case blast_operation.and:
                        for (int i = 0; i < ssmd_datacount; i++) output[i].x = math.select(0, 1f, buffer[i] != 0 && ((float*)stack[i])[stack_offset] != 0);
                        break;

                    case blast_operation.or:
                        for (int i = 0; i < ssmd_datacount; i++) output[i].x = math.select(0, 1f, buffer[i] != 0 || ((float*)stack[i])[stack_offset] != 0);
                        break;

                    case blast_operation.min:
                        for (int i = 0; i < ssmd_datacount; i++) output[i].x = math.min(buffer[i], ((float*)stack[i])[stack_offset]);
                        break;

                    case blast_operation.max:
                        for (int i = 0; i < ssmd_datacount; i++) output[i].x = math.min(buffer[i], ((float*)stack[i])[stack_offset]);
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
                    case blast_operation.and:
                        bool2 b = new bool2(constant != 0, constant != 0);
                        if (constant == 0)
                        {
                            for (int i = 0; i < ssmd_datacount; i++) output[i] = 0f;
                        }
                        else
                        {
                            for (int i = 0; i < ssmd_datacount; i++) output[i] = math.select(0, 1f, buffer[i] != 0);
                        }
                        break;

                    case blast_operation.or:
                        if (constant == 0)
                        {
                            // nothing changes if its the same buffer so skip doing stuff in that case
                            if (output != buffer)
                            {
                                // if not the same buffer then we still need to copy the data 
                                for (int i = 0; i < ssmd_datacount; i++) output[i] = buffer[i];
                            }
                        }
                        else
                        {
                            // just write true,  | true is always true 
                            for (int i = 0; i < ssmd_datacount; i++) output[i] = 1f;
                        }
                        break;

                    case blast_operation.min:
                        for (int i = 0; i < ssmd_datacount; i++) output[i] = math.min(buffer[i], constant);
                        break;

                    case blast_operation.max:
                        for (int i = 0; i < ssmd_datacount; i++) output[i] = math.max(buffer[i], constant);
                        break;
                }
            }
            else
            {
                // error or.. constant by operation value.... 
#if DEVELOPMENT_BUILD
                Debug.LogError("blast.ssmd.interpretor.pop_f1_with_op_into_f4: select op by constant value is not supported at codepointer {code_pointer} with operation: {op}");
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
        void pop_f2_with_op_into_f4(in int code_pointer, [NoAlias]float2* buffer, [NoAlias]float4* output, blast_operation op)
        {
#if DEVELOPMENT_BUILD
            if (!Blast.IsOperationSSMDHandled(op))
            {
                Debug.LogError($"blast.ssmd.interpretor.pop_f2_with_op_into_f4: -> unsupported operation supplied: {op}, only + - * / are supported");
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
                    Debug.LogError($"blast.ssmd.interpretor.pop_f2_with_op_into_f4: data mismatch, expecting numeric of size 2, found {type} of size {size} at data offset {c - BlastInterpretor.opt_id} with operation: {op}");
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

                    case blast_operation.and:
                        for (int i = 0; i < ssmd_datacount; i++) output[i].xy = math.select(0, 1f, math.any(buffer[i]) && math.any(((float2*)(void*)&((float*)data[i])[c - BlastInterpretor.opt_id])[0]));
                        break;

                    case blast_operation.or:
                        for (int i = 0; i < ssmd_datacount; i++) output[i].xy = math.select(0, 1f, math.any(buffer[i]) || math.any(((float2*)(void*)&((float*)data[i])[c - BlastInterpretor.opt_id])[0]));
                        break;

                    case blast_operation.min:
                        for (int i = 0; i < ssmd_datacount; i++) output[i].xy = math.min(buffer[i], ((float2*)(void*)&((float*)data[i])[c - BlastInterpretor.opt_id])[0]);
                        break;

                    case blast_operation.max:
                        for (int i = 0; i < ssmd_datacount; i++) output[i].xy = math.min(buffer[i], ((float2*)(void*)&((float*)data[i])[c - BlastInterpretor.opt_id])[0]);
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
                    Debug.LogError($"blast.ssmd.interpretor.pop_f2_with_op_into_f4: stackdata mismatch, expecting numeric of size 2, found {type} of size {size} at stack offset {stack_offset} with operation: {op}");
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

                    case blast_operation.and:
                        for (int i = 0; i < ssmd_datacount; i++) output[i].xy = math.select(0, 1f, math.any(buffer[i]) && math.any(((float2*)(void*)&((float*)stack[i])[stack_offset])[0]));
                        break;

                    case blast_operation.or:
                        for (int i = 0; i < ssmd_datacount; i++) output[i].xy = math.select(0, 1f, math.any(buffer[i]) || math.any(((float2*)(void*)&((float*)stack[i])[stack_offset])[0]));
                        break;

                    case blast_operation.min:
                        for (int i = 0; i < ssmd_datacount; i++) output[i].xy = math.min(buffer[i], ((float2*)(void*)&((float*)stack[i])[stack_offset])[0]);
                        break;

                    case blast_operation.max:
                        for (int i = 0; i < ssmd_datacount; i++) output[i].xy = math.min(buffer[i], ((float2*)(void*)&((float*)stack[i])[stack_offset])[0]);
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

                    case blast_operation.and:
                        bool2 b = new bool2(constant != 0, constant != 0);
                        if (constant == 0)
                        {
                            for (int i = 0; i < ssmd_datacount; i++) output[i].xy = 0f;
                        }
                        else
                        {
                            for (int i = 0; i < ssmd_datacount; i++) output[i].xy = math.select(0, 1f, buffer[i] != 0);
                        }
                        break;

                    case blast_operation.or:
                        if (constant == 0)
                        {
                            // nothing changes if its the same buffer so skip doing stuff in that case
                            if (output != buffer)
                            {
                                // if not the same buffer then we still need to copy the data 
                                for (int i = 0; i < ssmd_datacount; i++) output[i].xy = buffer[i];
                            }
                        }
                        else
                        {
                            // just write true,  | true is always true 
                            for (int i = 0; i < ssmd_datacount; i++) output[i].xy = 1f;
                        }
                        break;

                    case blast_operation.min:
                        for (int i = 0; i < ssmd_datacount; i++) output[i].xy = math.min(buffer[i], constant);
                        break;

                    case blast_operation.max:
                        for (int i = 0; i < ssmd_datacount; i++) output[i].xy = math.max(buffer[i], constant);
                        break;
                }
            }
            else
            {
                // error or.. constant by operation value.... 
#if DEVELOPMENT_BUILD
                Debug.LogError("blast.ssmd.interpretor.pop_f2_with_op_into_f4: select op by constant value is not supported at codepointer {code_pointer} with operation: {op}");
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
        void pop_f3_with_op_into_f4(in int code_pointer, [NoAlias]float3* buffer, [NoAlias]float4* output, blast_operation op)
        {
#if DEVELOPMENT_BUILD
            if (!Blast.IsOperationSSMDHandled(op))
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

                    case blast_operation.and:
                        for (int i = 0; i < ssmd_datacount; i++) output[i].xyz = math.select(0, 1f, math.any(buffer[i]) && math.any(((float3*)(void*)&((float*)data[i])[c - BlastInterpretor.opt_id])[0]));
                        break;

                    case blast_operation.or:
                        for (int i = 0; i < ssmd_datacount; i++) output[i].xyz = math.select(0, 1f, math.any(buffer[i]) || math.any(((float3*)(void*)&((float*)data[i])[c - BlastInterpretor.opt_id])[0]));
                        break;

                    case blast_operation.min:
                        for (int i = 0; i < ssmd_datacount; i++) output[i].xyz = math.min(buffer[i], ((float3*)(void*)&((float*)data[i])[c - BlastInterpretor.opt_id])[0]);
                        break;

                    case blast_operation.max:
                        for (int i = 0; i < ssmd_datacount; i++) output[i].xyz = math.min(buffer[i], ((float3*)(void*)&((float*)data[i])[c - BlastInterpretor.opt_id])[0]);
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

                    case blast_operation.and:
                        for (int i = 0; i < ssmd_datacount; i++) output[i].xyz = math.select(0, 1f, math.any(buffer[i]) && math.any(((float3*)(void*)&((float*)stack[i])[stack_offset])[0]));
                        break;

                    case blast_operation.or:
                        for (int i = 0; i < ssmd_datacount; i++) output[i].xyz = math.select(0, 1f, math.any(buffer[i]) || math.any(((float3*)(void*)&((float*)stack[i])[stack_offset])[0]));
                        break;

                    case blast_operation.min:
                        for (int i = 0; i < ssmd_datacount; i++) output[i].xyz = math.min(buffer[i], ((float3*)(void*)&((float*)stack[i])[stack_offset])[0]);
                        break;

                    case blast_operation.max:
                        for (int i = 0; i < ssmd_datacount; i++) output[i].xyz = math.min(buffer[i], ((float3*)(void*)&((float*)stack[i])[stack_offset])[0]);
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

                    case blast_operation.and:
                        bool2 b = new bool2(constant != 0, constant != 0);
                        if (constant == 0)
                        {
                            for (int i = 0; i < ssmd_datacount; i++) output[i].xyz = 0f;
                        }
                        else
                        {
                            for (int i = 0; i < ssmd_datacount; i++) output[i].xyz = math.select(0, 1f, buffer[i] != 0);
                        }
                        break;

                    case blast_operation.or:
                        if (constant == 0)
                        {
                            // nothing changes if its the same buffer so skip doing stuff in that case
                            if (output != buffer)
                            {
                                // if not the same buffer then we still need to copy the data 
                                for (int i = 0; i < ssmd_datacount; i++) output[i].xyz = buffer[i];
                            }
                        }
                        else
                        {
                            // just write true,  | true is always true 
                            for (int i = 0; i < ssmd_datacount; i++) output[i].xyz = 1f;
                        }
                        break;

                    case blast_operation.min:
                        for (int i = 0; i < ssmd_datacount; i++) output[i].xyz = math.min(buffer[i], constant);
                        break;

                    case blast_operation.max:
                        for (int i = 0; i < ssmd_datacount; i++) output[i].xyz = math.max(buffer[i], constant);
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
        /// set a float1 data location from register location  
        /// </summary>
        /// <param name="index">data index</param>
        void set_data_from_register_f1_n(in int index)
        {
            for (int i = 0; i < ssmd_datacount; i++)
            {
                ((float*)data[i])[index] = -register[i].x;
            }
        }


        /// <summary>
        /// set a dest.x buffer from data[offset]
        /// </summary>
        void set_from_data_f1([NoAlias]float4* destination, [NoAlias]void** data, int offset)
        {
            for (int i = 0; i < ssmd_datacount; i++)
            {
                destination[i].x = ((float*)data[i])[offset];
            }
        }

        /// <summary>
        /// set a dest.xy buffer from data[offset]
        /// </summary>
        void set_from_data_f2([NoAlias]float4* destination, [NoAlias]void** data, int offset)
        {
            int offsetb = offset + 1;
            for (int i = 0; i < ssmd_datacount; i++)
            {
                destination[i].x = ((float*)data[i])[offset];
                destination[i].y = ((float*)data[i])[offsetb];
            }
        }

        /// <summary>
        /// set a dest.xyz buffer from data[offset]
        /// </summary>
        void set_from_data_f3([NoAlias]float4* destination, [NoAlias]void** data, int offset)
        {
            int offsetb = offset + 1;
            int offsetc = offset + 2;
            for (int i = 0; i < ssmd_datacount; i++)
            {
                destination[i].x = ((float*)data[i])[offset];
                destination[i].y = ((float*)data[i])[offsetb];
                destination[i].y = ((float*)data[i])[offsetc];
            }
        }

        /// <summary>
        /// set a dest.xyzw buffer from data[offset]
        /// </summary>
        void set_from_data_f4([NoAlias]float4* destination, [NoAlias]void** data, int offset)
        {
            for (int i = 0; i < ssmd_datacount; i++)
            {
                destination[i] = ((float4*)(void*)&((float*)data[i])[offset])[0];
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
        /// set a float2 data location from register location  
        /// </summary>
        /// <param name="index">data index</param>
        void set_data_from_register_f2_n(in int index)
        {
            for (int i = 0; i < ssmd_datacount; i++)
            {
                float* p = (float*)(data[i]);
                p[index] = -register[i].x;
                p[index + 1] = -register[i].y;
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
        /// set a float3 data location from register location  
        /// </summary>
        /// <param name="index">data index</param>
        void set_data_from_register_f3_n(in int index)
        {
            for (int i = 0; i < ssmd_datacount; i++)
            {
                float* p = (float*)(data[i]);
                p[index] = -register[i].x;
                p[index + 1] = -register[i].y;
                p[index + 2] = -register[i].z;
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
        /// <summary>
        /// set a float4 data location from register location  
        /// </summary>
        /// <param name="index">data index</param>
        void set_data_from_register_f4_n(in int index)
        {
            for (int i = 0; i < ssmd_datacount; i++)
            {
                float* p = (float*)(data[i]);
                p[index] = -register[i].x;
                p[index + 1] = -register[i].y;
                p[index + 2] = -register[i].z;
                p[index + 3] = -register[i].w;
            }
        }

        void set_data_from_register(in byte vector_size, in byte assignee, bool minus)
        {
            if (minus)
            {
                // set minus register to assignee
                switch (vector_size)
                {
                    case 1: set_data_from_register_f1_n(assignee); break;
                    case 2: set_data_from_register_f2_n(assignee); break;
                    case 3: set_data_from_register_f3_n(assignee); break;
                    case 0:
                    case 4: set_data_from_register_f4_n(assignee); break;
                }
            }
            else
            {
                // set to assignee
                switch (vector_size)
                {
                    case 1: set_data_from_register_f1(assignee); break;
                    case 2: set_data_from_register_f2(assignee); break;
                    case 3: set_data_from_register_f3(assignee); break;
                    case 0:
                    case 4: set_data_from_register_f4(assignee); break;
                }
            }
        }


        #endregion

        /// <summary>
        /// set register[n].x from data 
        /// </summary>
        void set_register_from_data_f1([NoAlias]float* f1)
        {
            for (int i = 0; i < ssmd_datacount; i++)
            {
                register[i].x = f1[i];
            }
        }


        /// <summary>
        /// set register[n].xyzw from data 
        /// </summary>
        void set_register_from_data_f4([NoAlias]float4* f4)
        {
            UnsafeUtils.MemCpy(register, f4, ssmd_datacount * sizeof(float4));
        }

        #endregion

        #region Operations Handlers 

        /// <summary>
        /// handle operation a.x and b.x
        /// </summary>
        void handle_op_f1_f1(in blast_operation op, [NoAlias]float4* a, [NoAlias]float4* b, ref byte vector_size)
        {
            vector_size = 1;
            switch (op)
            {
                default: return;
                case blast_operation.nop: for (int i = 0; i < ssmd_datacount; i++) a[i].x = b[i].x; break;

                case blast_operation.add: for (int i = 0; i < ssmd_datacount; i++) a[i].x = a[i].x + b[i].x; return;
                case blast_operation.substract: for (int i = 0; i < ssmd_datacount; i++) a[i].x = a[i].x - b[i].x; return;
                case blast_operation.multiply: for (int i = 0; i < ssmd_datacount; i++) a[i].x = a[i].x * b[i].x; return;
                case blast_operation.divide: for (int i = 0; i < ssmd_datacount; i++) a[i].x = a[i].x / b[i].x; return;

                case blast_operation.and: for (int i = 0; i < ssmd_datacount; i++) a[i].x = math.select(0, 1, a[i].x != 0 && b[i].x != 0); return;
                case blast_operation.or: for (int i = 0; i < ssmd_datacount; i++) a[i].x = math.select(0, 1, a[i].x != 0 || b[i].x != 0); return;
                case blast_operation.xor: for (int i = 0; i < ssmd_datacount; i++) a[i].x = math.select(0, 1, a[i].x != 0 ^ b[i].x != 0); return;

                case blast_operation.greater: for (int i = 0; i < ssmd_datacount; i++) a[i].x = math.select(0f, 1f, a[i].x > b[i].x); return;
                case blast_operation.smaller: for (int i = 0; i < ssmd_datacount; i++) a[i].x = math.select(0f, 1f, a[i].x < b[i].x); return;
                case blast_operation.smaller_equals: for (int i = 0; i < ssmd_datacount; i++) a[i].x = math.select(0f, 1f, a[i].x <= b[i].x); return;
                case blast_operation.greater_equals: for (int i = 0; i < ssmd_datacount; i++) a[i].x = math.select(0f, 1f, a[i].x >= b[i].x); return;
                case blast_operation.equals: for (int i = 0; i < ssmd_datacount; i++) a[i].x = math.select(0f, 1f, a[i].x == b[i].x); return;

                case blast_operation.not:
#if DEVELOPMENT_BUILD
                    Debug.LogError("not");
#endif
                    return;
            }
        }
        void handle_op_f1_f2(in blast_operation op, [NoAlias]float4* a, [NoAlias]float4* b, ref byte vector_size)
        {
            vector_size = 2;
            switch (op)
            {
                default: return;
                case blast_operation.nop: for (int i = 0; i < ssmd_datacount; i++) a[i].xy = b[i].xy; break;

                case blast_operation.add: for (int i = 0; i < ssmd_datacount; i++) a[i].xy = a[i].x + b[i].xy; return;
                case blast_operation.substract: for (int i = 0; i < ssmd_datacount; i++) a[i].xy = a[i].x - b[i].xy; return;
                case blast_operation.multiply: for (int i = 0; i < ssmd_datacount; i++) a[i].xy = a[i].x * b[i].xy; return;
                case blast_operation.divide: for (int i = 0; i < ssmd_datacount; i++) a[i].xy = a[i].x / b[i].xy; return;

                case blast_operation.and: for (int i = 0; i < ssmd_datacount; i++) a[i].xy = math.select(0, 1, a[i].x != 0 && math.any(b[i].xy)); return;
                case blast_operation.or: for (int i = 0; i < ssmd_datacount; i++) a[i].xy = math.select(0, 1, a[i].x != 0 || math.any(b[i].xy)); return;
                case blast_operation.xor: for (int i = 0; i < ssmd_datacount; i++) a[i].xy = math.select(0, 1, a[i].x != 0 ^ math.any(b[i].xy)); return;

                case blast_operation.greater: for (int i = 0; i < ssmd_datacount; i++) a[i].xy = math.select(0f, 1f, a[i].x > b[i].xy); return;
                case blast_operation.smaller: for (int i = 0; i < ssmd_datacount; i++) a[i].xy = math.select(0f, 1f, a[i].x < b[i].xy); return;
                case blast_operation.smaller_equals: for (int i = 0; i < ssmd_datacount; i++) a[i].xy = math.select(0f, 1f, a[i].x <= b[i].xy); return;
                case blast_operation.greater_equals: for (int i = 0; i < ssmd_datacount; i++) a[i].xy = math.select(0f, 1f, a[i].x >= b[i].xy); return;
                case blast_operation.equals: for (int i = 0; i < ssmd_datacount; i++) a[i].xy = math.select(0f, 1f, a[i].x == b[i].xy); return;

                case blast_operation.not:
#if DEVELOPMENT_BUILD
                    Debug.LogError("not");
#endif
                    return;
            }
        }
        void handle_op_f1_f3(in blast_operation op, [NoAlias]float4* a, [NoAlias]float4* b, ref byte vector_size)
        {
            vector_size = 3;
            switch (op)
            {
                default: return;
                case blast_operation.nop: for (int i = 0; i < ssmd_datacount; i++) a[i].xyz = b[i].xyz; break;

                case blast_operation.add: for (int i = 0; i < ssmd_datacount; i++) a[i].xyz = a[i].x + b[i].xyz; return;
                case blast_operation.substract: for (int i = 0; i < ssmd_datacount; i++) a[i].xyz = a[i].x - b[i].xyz; return;
                case blast_operation.multiply: for (int i = 0; i < ssmd_datacount; i++) a[i].xyz = a[i].x * b[i].xyz; return;
                case blast_operation.divide: for (int i = 0; i < ssmd_datacount; i++) a[i].xyz = a[i].x / b[i].xyz; return;

                case blast_operation.and: for (int i = 0; i < ssmd_datacount; i++) a[i].xyz = math.select(0, 1, a[i].x != 0 && math.any(b[i].xyz)); return;
                case blast_operation.or: for (int i = 0; i < ssmd_datacount; i++) a[i].xyz = math.select(0, 1, a[i].x != 0 || math.any(b[i].xyz)); return;
                case blast_operation.xor: for (int i = 0; i < ssmd_datacount; i++) a[i].xyz = math.select(0, 1, a[i].x != 0 ^ math.any(b[i].xyz)); return;

                case blast_operation.greater: for (int i = 0; i < ssmd_datacount; i++) a[i].xyz = math.select(0f, 1f, a[i].x > b[i].xyz); return;
                case blast_operation.smaller: for (int i = 0; i < ssmd_datacount; i++) a[i].xyz = math.select(0f, 1f, a[i].x < b[i].xyz); return;
                case blast_operation.smaller_equals: for (int i = 0; i < ssmd_datacount; i++) a[i].xyz = math.select(0f, 1f, a[i].x <= b[i].xyz); return;
                case blast_operation.greater_equals: for (int i = 0; i < ssmd_datacount; i++) a[i].xyz = math.select(0f, 1f, a[i].x >= b[i].xyz); return;
                case blast_operation.equals: for (int i = 0; i < ssmd_datacount; i++) a[i].xyz = math.select(0f, 1f, a[i].x == b[i].xyz); return;

                case blast_operation.not:
#if DEVELOPMENT_BUILD
                    Debug.LogError("not");
#endif
                    return;
            }
        }
        void handle_op_f1_f4(in blast_operation op, [NoAlias]float4* a, [NoAlias]float4* b, ref byte vector_size)
        {
            vector_size = 4;
            switch (op)
            {
                default: return;
                case blast_operation.nop: for (int i = 0; i < ssmd_datacount; i++) a[i] = b[i]; break;

                case blast_operation.add: for (int i = 0; i < ssmd_datacount; i++) a[i] = a[i].x + b[i]; return;
                case blast_operation.substract: for (int i = 0; i < ssmd_datacount; i++) a[i] = a[i].x - b[i]; return;
                case blast_operation.multiply: for (int i = 0; i < ssmd_datacount; i++) a[i] = a[i].x * b[i]; return;
                case blast_operation.divide: for (int i = 0; i < ssmd_datacount; i++) a[i] = a[i].x / b[i]; return;

                case blast_operation.and: for (int i = 0; i < ssmd_datacount; i++) a[i] = math.select(0, 1, a[i].x != 0 && math.any(b[i])); return;
                case blast_operation.or: for (int i = 0; i < ssmd_datacount; i++) a[i] = math.select(0, 1, a[i].x != 0 || math.any(b[i])); return;
                case blast_operation.xor: for (int i = 0; i < ssmd_datacount; i++) a[i] = math.select(0, 1, a[i].x != 0 ^ math.any(b[i])); return;

                case blast_operation.greater: for (int i = 0; i < ssmd_datacount; i++) a[i] = math.select(0f, 1f, a[i].x > b[i]); return;
                case blast_operation.smaller: for (int i = 0; i < ssmd_datacount; i++) a[i] = math.select(0f, 1f, a[i].x < b[i]); return;
                case blast_operation.smaller_equals: for (int i = 0; i < ssmd_datacount; i++) a[i] = math.select(0f, 1f, a[i].x <= b[i]); return;
                case blast_operation.greater_equals: for (int i = 0; i < ssmd_datacount; i++) a[i] = math.select(0f, 1f, a[i].x >= b[i]); return;
                case blast_operation.equals: for (int i = 0; i < ssmd_datacount; i++) a[i] = math.select(0f, 1f, a[i].x == b[i]); return;

                case blast_operation.not:
#if DEVELOPMENT_BUILD
                    Debug.LogError("not");
#endif
                    return;
            }
        }
        void handle_op_f2_f1(in blast_operation op, [NoAlias]float4* a, [NoAlias]float4* b, ref byte vector_size)
        {
            vector_size = 2;
            switch (op)
            {
                default: return;
                case blast_operation.nop: for (int i = 0; i < ssmd_datacount; i++) a[i].xy = b[i].x; break;

                case blast_operation.add: for (int i = 0; i < ssmd_datacount; i++) a[i].xy = a[i].xy + b[i].x; return;
                case blast_operation.substract: for (int i = 0; i < ssmd_datacount; i++) a[i].xy = a[i].xy - b[i].x; return;
                case blast_operation.multiply: for (int i = 0; i < ssmd_datacount; i++) a[i].xy = a[i].xy * b[i].x; return;
                case blast_operation.divide: for (int i = 0; i < ssmd_datacount; i++) a[i].xy = a[i].xy / b[i].x; return;

                case blast_operation.and: for (int i = 0; i < ssmd_datacount; i++) a[i].xy = math.select(0, 1, math.any(a[i].xy) && b[i].x != 0); return;
                case blast_operation.or: for (int i = 0; i < ssmd_datacount; i++) a[i].xy = math.select(0, 1, math.any(a[i].xy) || b[i].x != 0); return;
                case blast_operation.xor: for (int i = 0; i < ssmd_datacount; i++) a[i].xy = math.select(0, 1, math.any(a[i].xy) ^ b[i].x != 0); return;

                case blast_operation.greater: for (int i = 0; i < ssmd_datacount; i++) a[i].xy = math.select(0f, 1f, a[i].xy > b[i].x); return;
                case blast_operation.smaller: for (int i = 0; i < ssmd_datacount; i++) a[i].xy = math.select(0f, 1f, a[i].xy < b[i].x); return;
                case blast_operation.smaller_equals: for (int i = 0; i < ssmd_datacount; i++) a[i].xy = math.select(0f, 1f, a[i].xy <= b[i].x); return;
                case blast_operation.greater_equals: for (int i = 0; i < ssmd_datacount; i++) a[i].xy = math.select(0f, 1f, a[i].xy >= b[i].x); return;
                case blast_operation.equals: for (int i = 0; i < ssmd_datacount; i++) a[i].xy = math.select(0f, 1f, a[i].xy == b[i].x); return;

                case blast_operation.not:
#if DEVELOPMENT_BUILD
                    Debug.LogError("not");
#endif
                    return;
            }
        }
        void handle_op_f2_f2(in blast_operation op, [NoAlias]float4* a, [NoAlias]float4* b, ref byte vector_size)
        {
            vector_size = 2;
            switch (op)
            {
                default: return;
                case blast_operation.nop: for (int i = 0; i < ssmd_datacount; i++) a[i].xy = b[i].xy; break;

                case blast_operation.add: for (int i = 0; i < ssmd_datacount; i++) a[i].xy = a[i].xy + b[i].xy; return;
                case blast_operation.substract: for (int i = 0; i < ssmd_datacount; i++) a[i].xy = a[i].xy - b[i].xy; return;
                case blast_operation.multiply: for (int i = 0; i < ssmd_datacount; i++) a[i].xy = a[i].xy * b[i].xy; return;
                case blast_operation.divide: for (int i = 0; i < ssmd_datacount; i++) a[i].xy = a[i].xy / b[i].xy; return;

                case blast_operation.and: for (int i = 0; i < ssmd_datacount; i++) a[i].xy = math.select(0, 1, math.any(a[i].xy) && math.any(b[i].xy)); return;
                case blast_operation.or: for (int i = 0; i < ssmd_datacount; i++) a[i].xy = math.select(0, 1, math.any(a[i].xy) || math.any(b[i].xy)); return;
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
        void handle_op_f3_f1(in blast_operation op, [NoAlias]float4* a, [NoAlias]float4* b, ref byte vector_size)
        {
            vector_size = 3;
            switch (op)
            {
                default: return;
                case blast_operation.nop: for (int i = 0; i < ssmd_datacount; i++) a[i].xyz = b[i].x; break;

                case blast_operation.add: for (int i = 0; i < ssmd_datacount; i++) a[i].xyz = a[i].xyz + b[i].x; return;
                case blast_operation.substract: for (int i = 0; i < ssmd_datacount; i++) a[i].xyz = a[i].xyz - b[i].x; return;
                case blast_operation.multiply: for (int i = 0; i < ssmd_datacount; i++) a[i].xyz = a[i].xyz * b[i].x; return;
                case blast_operation.divide: for (int i = 0; i < ssmd_datacount; i++) a[i].xyz = a[i].xyz / b[i].x; return;

                case blast_operation.and: for (int i = 0; i < ssmd_datacount; i++) a[i].xyz = math.select(0, 1, math.any(a[i].xyz) && b[i].x != 0); return;
                case blast_operation.or: for (int i = 0; i < ssmd_datacount; i++) a[i].xyz = math.select(0, 1, math.any(a[i].xyz) || b[i].x != 0); return;
                case blast_operation.xor: for (int i = 0; i < ssmd_datacount; i++) a[i].xyz = math.select(0, 1, math.any(a[i].xyz) ^ b[i].x != 0); return;

                case blast_operation.greater: for (int i = 0; i < ssmd_datacount; i++) a[i].xyz = math.select(0f, 1f, a[i].xyz > b[i].x); return;
                case blast_operation.smaller: for (int i = 0; i < ssmd_datacount; i++) a[i].xyz = math.select(0f, 1f, a[i].xyz < b[i].x); return;
                case blast_operation.smaller_equals: for (int i = 0; i < ssmd_datacount; i++) a[i].xyz = math.select(0f, 1f, a[i].xyz <= b[i].x); return;
                case blast_operation.greater_equals: for (int i = 0; i < ssmd_datacount; i++) a[i].xyz = math.select(0f, 1f, a[i].xyz >= b[i].x); return;
                case blast_operation.equals: for (int i = 0; i < ssmd_datacount; i++) a[i].xyz = math.select(0f, 1f, a[i].xyz == b[i].x); return;

                case blast_operation.not:
#if DEVELOPMENT_BUILD
                    Debug.LogError("not");
#endif
                    return;
            }
        }
        void handle_op_f3_f3(in blast_operation op, [NoAlias]float4* a, [NoAlias]float4* b, ref byte vector_size)
        {
            vector_size = 3;
            switch (op)
            {
                default: return;
                case blast_operation.nop: for (int i = 0; i < ssmd_datacount; i++) a[i].xyz = b[i].xyz; break;

                case blast_operation.add: for (int i = 0; i < ssmd_datacount; i++) a[i].xyz = a[i].xyz + b[i].xyz; return;
                case blast_operation.substract: for (int i = 0; i < ssmd_datacount; i++) a[i].xyz = a[i].xyz - b[i].xyz; return;
                case blast_operation.multiply: for (int i = 0; i < ssmd_datacount; i++) a[i].xyz = a[i].xyz * b[i].xyz; return;
                case blast_operation.divide: for (int i = 0; i < ssmd_datacount; i++) a[i].xyz = a[i].xyz / b[i].xyz; return;

                case blast_operation.and: for (int i = 0; i < ssmd_datacount; i++) a[i].xyz = math.select(0, 1, math.any(a[i].xyz) && math.any(b[i].xyz)); return;
                case blast_operation.or: for (int i = 0; i < ssmd_datacount; i++) a[i].xyz = math.select(0, 1, math.any(a[i].xyz) || math.any(b[i].xyz)); return;
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
        void handle_op_f4_f1(in blast_operation op, [NoAlias]float4* a, [NoAlias]float4* b, ref byte vector_size)
        {
            vector_size = 4;
            switch (op)
            {
                default: return;
                case blast_operation.nop: for (int i = 0; i < ssmd_datacount; i++) a[i] = b[i].x; break;

                case blast_operation.add: for (int i = 0; i < ssmd_datacount; i++) a[i] = a[i] + b[i].x; return;
                case blast_operation.substract: for (int i = 0; i < ssmd_datacount; i++) a[i] = a[i] - b[i].x; return;
                case blast_operation.multiply: for (int i = 0; i < ssmd_datacount; i++) a[i] = a[i] * b[i].x; return;
                case blast_operation.divide: for (int i = 0; i < ssmd_datacount; i++) a[i] = a[i] / b[i].x; return;

                case blast_operation.and: for (int i = 0; i < ssmd_datacount; i++) a[i] = math.select(0, 1, math.any(a[i]) && b[i].x != 0); return;
                case blast_operation.or: for (int i = 0; i < ssmd_datacount; i++) a[i] = math.select(0, 1, math.any(a[i]) || b[i].x != 0); return;
                case blast_operation.xor: for (int i = 0; i < ssmd_datacount; i++) a[i] = math.select(0, 1, math.any(a[i]) ^ b[i].x != 0); return;

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
        void handle_op_f4_f4(in blast_operation op, [NoAlias]float4* a, [NoAlias]float4* b, ref byte vector_size)
        {
            vector_size = 4;
            switch (op)
            {
                default: return;
                case blast_operation.nop: for (int i = 0; i < ssmd_datacount; i++) a[i] = b[i]; break;

                case blast_operation.add: for (int i = 0; i < ssmd_datacount; i++) a[i] = a[i] + b[i]; return;
                case blast_operation.substract: for (int i = 0; i < ssmd_datacount; i++) a[i] = a[i] - b[i]; return;
                case blast_operation.multiply: for (int i = 0; i < ssmd_datacount; i++) a[i] = a[i] * b[i]; return;
                case blast_operation.divide: for (int i = 0; i < ssmd_datacount; i++) a[i] = a[i] / b[i]; return;

                case blast_operation.and: for (int i = 0; i < ssmd_datacount; i++) a[i] = math.select(0, 1, math.any(a[i]) && math.any(b[i])); return;
                case blast_operation.or: for (int i = 0; i < ssmd_datacount; i++) a[i] = math.select(0, 1, math.any(a[i]) || math.any(b[i])); return;
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

        #region Single Input functions (abs, sin etc) 

        //
        // Although for all of these we could use functionpointers the compiler will have a hard time
        // vectorizing most inner loops if the use functionpointers instead of the known math library for
        // which it has buildin support
        //
        // -> we could use some form of codegen in future version but these will not really change much i guess
        //

        void get_abs_result([NoAlias]void* temp, ref int code_pointer, ref byte vector_size, [NoAlias]float4* register)
        {
            BlastVariableDataType datatype;
            pop_op_meta(code[code_pointer + 1], out datatype, out vector_size);
#if DEVELOPMENT_BUILD || TRACE
            if (datatype != BlastVariableDataType.Numeric)
            {
                Debug.LogError($"blast.ssmd.interpretor.abs: datatype|vectorsize mismatch, vector_size: {vector_size}, datatype: {datatype} not supported");
                return;
            }
#endif

            switch (vector_size)
            {
                case 1:
                    {
                        pop_fx_into<float>(code_pointer + 1, (float*)temp);
                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            register[i].x = math.abs(((float*)temp)[i]);
                        }
                    }
                    break;

                case 2:
                    {
                        pop_fx_into<float2>(code_pointer + 1, (float2*)temp);
                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            register[i].xy = math.abs(((float2*)temp)[0]);
                        }
                    }
                    break;

                case 3:
                    {
                        pop_fx_into<float3>(code_pointer + 1, (float3*)temp);
                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            register[i].xyz = math.abs(((float3*)temp)[0]);
                        }
                    }
                    break;

                case 0:
                case 4:
                    {
                        vector_size = 4;
                        pop_fx_into<float4>(code_pointer + 1, (float4*)temp);
                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            register[i] = math.abs(((float4*)temp)[0]);
                        }
                    }
                    break;

#if DEVELOPMENT_BUILD || TRACE
                default:
                    Debug.LogError($"blast.ssmd.interpretor.abs: datatype|vectorsize mismatch, vector_size: {vector_size}, datatype: {datatype} not supported");
                    break;
#endif
            }
            code_pointer += 1;
        }

        void get_ceil_result([NoAlias]void* temp, ref int code_pointer, ref byte vector_size, [NoAlias]float4* register)
        {
            BlastVariableDataType datatype;
            pop_op_meta(code[code_pointer + 1], out datatype, out vector_size);
#if DEVELOPMENT_BUILD || TRACE
            if (datatype != BlastVariableDataType.Numeric)
            {
                Debug.LogError($"blast.ssmd.interpretor.ceil: datatype|vectorsize mismatch, vector_size: {vector_size}, datatype: {datatype} not supported");
                return;
            }
#endif

            switch (vector_size)
            {
                case 1:
                    {
                        pop_fx_into<float>(code_pointer + 1, (float*)temp);
                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            register[i].x = math.ceil(((float*)temp)[i]);
                        }
                    }
                    break;

                case 2:
                    {
                        pop_fx_into<float2>(code_pointer + 1, (float2*)temp);
                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            register[i].xy = math.ceil(((float2*)temp)[0]);
                        }
                    }
                    break;

                case 3:
                    {
                        pop_fx_into<float3>(code_pointer + 1, (float3*)temp);
                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            register[i].xyz = math.ceil(((float3*)temp)[0]);
                        }
                    }
                    break;

                case 0:
                case 4:
                    {
                        vector_size = 4;
                        pop_fx_into<float4>(code_pointer + 1, (float4*)temp);
                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            register[i] = math.ceil(((float4*)temp)[0]);
                        }
                    }
                    break;

#if DEVELOPMENT_BUILD || TRACE
                default:
                    Debug.LogError($"blast.ssmd.interpretor.ceil: datatype|vectorsize mismatch, vector_size: {vector_size}, datatype: {datatype} not supported");
                    break;
#endif
            }

            code_pointer += 1;
        }

        void get_floor_result([NoAlias]void* temp, ref int code_pointer, ref byte vector_size, [NoAlias]float4* register)
        {
            BlastVariableDataType datatype;
            pop_op_meta(code[code_pointer + 1], out datatype, out vector_size);
#if DEVELOPMENT_BUILD || TRACE
            if (datatype != BlastVariableDataType.Numeric)
            {
                Debug.LogError($"blast.ssmd.interpretor.floor: datatype|vectorsize mismatch, vector_size: {vector_size}, datatype: {datatype} not supported");
                return;
            }
#endif

            switch (vector_size)
            {
                case 1:
                    {
                        pop_fx_into<float>(code_pointer + 1, (float*)temp);
                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            register[i].x = math.floor(((float*)temp)[i]);
                        }
                    }
                    break;

                case 2:
                    {
                        pop_fx_into<float2>(code_pointer + 1, (float2*)temp);
                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            register[i].xy = math.floor(((float2*)temp)[0]);
                        }
                    }
                    break;

                case 3:
                    {
                        pop_fx_into<float3>(code_pointer + 1, (float3*)temp);
                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            register[i].xyz = math.floor(((float3*)temp)[0]);
                        }
                    }
                    break;

                case 0:
                case 4:
                    {
                        vector_size = 4;
                        pop_fx_into<float4>(code_pointer + 1, (float4*)temp);
                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            register[i] = math.floor(((float4*)temp)[0]);
                        }
                    }
                    break;

#if DEVELOPMENT_BUILD || TRACE
                default:
                    Debug.LogError($"blast.ssmd.interpretor.floor: datatype|vectorsize mismatch, vector_size: {vector_size}, datatype: {datatype} not supported");
                    break;
#endif
            }

            code_pointer += 1;
        }

        void get_frac_result([NoAlias]void* temp, ref int code_pointer, ref byte vector_size, [NoAlias]float4* register)
        {
            BlastVariableDataType datatype;
            pop_op_meta(code[code_pointer + 1], out datatype, out vector_size);
#if DEVELOPMENT_BUILD || TRACE
            if (datatype != BlastVariableDataType.Numeric)
            {
                Debug.LogError($"blast.ssmd.interpretor.frac: datatype|vectorsize mismatch, vector_size: {vector_size}, datatype: {datatype} not supported");
                return;
            }
#endif

            switch (vector_size)
            {
                case 1:
                    {
                        pop_fx_into<float>(code_pointer + 1, (float*)temp);
                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            register[i].x = math.frac(((float*)temp)[i]);
                        }
                    }
                    break;

                case 2:
                    {
                        pop_fx_into<float2>(code_pointer + 1, (float2*)temp);
                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            register[i].xy = math.frac(((float2*)temp)[0]);
                        }
                    }
                    break;

                case 3:
                    {
                        pop_fx_into<float3>(code_pointer + 1, (float3*)temp);
                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            register[i].xyz = math.frac(((float3*)temp)[0]);
                        }
                    }
                    break;

                case 0:
                case 4:
                    {
                        vector_size = 4;
                        pop_fx_into<float4>(code_pointer + 1, (float4*)temp);
                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            register[i] = math.frac(((float4*)temp)[0]);
                        }
                    }
                    break;

#if DEVELOPMENT_BUILD || TRACE
                default:
                    Debug.LogError($"blast.ssmd.interpretor.frac: datatype|vectorsize mismatch, vector_size: {vector_size}, datatype: {datatype} not supported");
                    break;
#endif
            }

            code_pointer += 1;
        }

        void get_normalize_result([NoAlias]void* temp, ref int code_pointer, ref byte vector_size, [NoAlias]float4* register)
        {
            BlastVariableDataType datatype;
            pop_op_meta(code[code_pointer + 1], out datatype, out vector_size);
#if DEVELOPMENT_BUILD || TRACE
            if (datatype != BlastVariableDataType.Numeric)
            {
                Debug.LogError($"blast.ssmd.interpretor.normalize: datatype|vectorsize mismatch, vector_size: {vector_size}, datatype: {datatype} not supported");
                return;
            }
#endif

            switch (vector_size)
            {
                case 2:
                    {
                        pop_fx_into<float2>(code_pointer + 1, (float2*)temp);
                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            register[i].xy = math.normalize(((float2*)temp)[0]);
                        }
                    }
                    break;

                case 3:
                    {
                        pop_fx_into<float3>(code_pointer + 1, (float3*)temp);
                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            register[i].xyz = math.normalize(((float3*)temp)[0]);
                        }
                    }
                    break;

                case 0:
                case 4:
                    {
                        vector_size = 4;
                        pop_fx_into<float4>(code_pointer + 1, (float4*)temp);
                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            register[i] = math.normalize(((float4*)temp)[0]);
                        }
                    }
                    break;

#if DEVELOPMENT_BUILD || TRACE
                default:
                    Debug.LogError($"blast.ssmd.interpretor.normalize: datatype|vectorsize mismatch, vector_size: {vector_size}, datatype: {datatype} not supported");
                    break;
#endif
            }

            code_pointer += 1;
        }

        void get_saturate_result([NoAlias]void* temp, ref int code_pointer, ref byte vector_size, [NoAlias]float4* register)
        {
            BlastVariableDataType datatype;
            pop_op_meta(code[code_pointer + 1], out datatype, out vector_size);
#if DEVELOPMENT_BUILD || TRACE
            if (datatype != BlastVariableDataType.Numeric)
            {
                Debug.LogError($"blast.ssmd.interpretor.saturate: datatype|vectorsize mismatch, vector_size: {vector_size}, datatype: {datatype} not supported");
                return;
            }
#endif

            switch (vector_size)
            {
                case 1:
                    {
                        pop_fx_into<float>(code_pointer + 1, (float*)temp);
                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            register[i].x = math.saturate(((float*)temp)[i]);
                        }
                    }
                    break;

                case 2:
                    {
                        pop_fx_into<float2>(code_pointer + 1, (float2*)temp);
                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            register[i].xy = math.saturate(((float2*)temp)[0]);
                        }
                    }
                    break;

                case 3:
                    {
                        pop_fx_into<float3>(code_pointer + 1, (float3*)temp);
                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            register[i].xyz = math.saturate(((float3*)temp)[0]);
                        }
                    }
                    break;

                case 0:
                case 4:
                    {
                        vector_size = 4;
                        pop_fx_into<float4>(code_pointer + 1, (float4*)temp);
                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            register[i] = math.saturate(((float4*)temp)[0]);
                        }
                    }
                    break;

#if DEVELOPMENT_BUILD || TRACE
                default:
                    Debug.LogError($"blast.ssmd.interpretor.saturate: datatype|vectorsize mismatch, vector_size: {vector_size}, datatype: {datatype} not supported");
                    break;
#endif
            }

            code_pointer += 1;
        }

        void get_log_result([NoAlias]void* temp, ref int code_pointer, ref byte vector_size, [NoAlias]float4* register)
        {
            BlastVariableDataType datatype;
            pop_op_meta(code[code_pointer + 1], out datatype, out vector_size);
#if DEVELOPMENT_BUILD || TRACE
            if (datatype != BlastVariableDataType.Numeric)
            {
                Debug.LogError($"blast.ssmd.interpretor.log: datatype|vectorsize mismatch, vector_size: {vector_size}, datatype: {datatype} not supported");
                return;
            }
#endif

            switch (vector_size)
            {
                case 1:
                    {
                        pop_fx_into<float>(code_pointer + 1, (float*)temp);
                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            register[i].x = math.log(((float*)temp)[i]);
                        }
                    }
                    break;

                case 2:
                    {
                        pop_fx_into<float2>(code_pointer + 1, (float2*)temp);
                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            register[i].xy = math.log(((float2*)temp)[0]);
                        }
                    }
                    break;

                case 3:
                    {
                        pop_fx_into<float3>(code_pointer + 1, (float3*)temp);
                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            register[i].xyz = math.log(((float3*)temp)[0]);
                        }
                    }
                    break;

                case 0:
                case 4:
                    {
                        vector_size = 4;
                        pop_fx_into<float4>(code_pointer + 1, (float4*)temp);
                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            register[i] = math.log(((float4*)temp)[0]);
                        }
                    }
                    break;

#if DEVELOPMENT_BUILD || TRACE
                default:
                    Debug.LogError($"blast.ssmd.interpretor.log: datatype|vectorsize mismatch, vector_size: {vector_size}, datatype: {datatype} not supported");
                    break;
#endif
            }

            code_pointer += 1;
        }

        void get_log2_result([NoAlias]void* temp, ref int code_pointer, ref byte vector_size, [NoAlias]float4* register)
        {
            BlastVariableDataType datatype;
            pop_op_meta(code[code_pointer + 1], out datatype, out vector_size);
#if DEVELOPMENT_BUILD || TRACE
            if (datatype != BlastVariableDataType.Numeric)
            {
                Debug.LogError($"blast.ssmd.interpretor.log2: datatype|vectorsize mismatch, vector_size: {vector_size}, datatype: {datatype} not supported");
                return;
            }
#endif

            switch (vector_size)
            {
                case 1:
                    {
                        pop_fx_into<float>(code_pointer + 1, (float*)temp);
                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            register[i].x = math.log2(((float*)temp)[i]);
                        }
                    }
                    break;

                case 2:
                    {
                        pop_fx_into<float2>(code_pointer + 1, (float2*)temp);
                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            register[i].xy = math.log2(((float2*)temp)[0]);
                        }
                    }
                    break;

                case 3:
                    {
                        pop_fx_into<float3>(code_pointer + 1, (float3*)temp);
                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            register[i].xyz = math.log2(((float3*)temp)[0]);
                        }
                    }
                    break;

                case 0:
                case 4:
                    {
                        vector_size = 4;
                        pop_fx_into<float4>(code_pointer + 1, (float4*)temp);
                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            register[i] = math.log2(((float4*)temp)[0]);
                        }
                    }
                    break;

#if DEVELOPMENT_BUILD || TRACE
                default:
                    Debug.LogError($"blast.ssmd.interpretor.log2: datatype|vectorsize mismatch, vector_size: {vector_size}, datatype: {datatype} not supported");
                    break;
#endif
            }

            code_pointer += 1;
        }

        void get_log10_result([NoAlias]void* temp, ref int code_pointer, ref byte vector_size, [NoAlias]float4* register)
        {
            BlastVariableDataType datatype;
            pop_op_meta(code[code_pointer + 1], out datatype, out vector_size);
#if DEVELOPMENT_BUILD || TRACE
            if (datatype != BlastVariableDataType.Numeric)
            {
                Debug.LogError($"blast.ssmd.interpretor.log10: datatype|vectorsize mismatch, vector_size: {vector_size}, datatype: {datatype} not supported");
                return;
            }
#endif

            switch (vector_size)
            {
                case 1:
                    {
                        pop_fx_into<float>(code_pointer + 1, (float*)temp);
                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            register[i].x = math.log10(((float*)temp)[i]);
                        }
                    }
                    break;

                case 2:
                    {
                        pop_fx_into<float2>(code_pointer + 1, (float2*)temp);
                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            register[i].xy = math.log10(((float2*)temp)[0]);
                        }
                    }
                    break;

                case 3:
                    {
                        pop_fx_into<float3>(code_pointer + 1, (float3*)temp);
                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            register[i].xyz = math.log10(((float3*)temp)[0]);
                        }
                    }
                    break;

                case 0:
                case 4:
                    {
                        vector_size = 4;
                        pop_fx_into<float4>(code_pointer + 1, (float4*)temp);
                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            register[i] = math.log10(((float4*)temp)[0]);
                        }
                    }
                    break;

#if DEVELOPMENT_BUILD || TRACE
                default:
                    Debug.LogError($"blast.ssmd.interpretor.log10: datatype|vectorsize mismatch, vector_size: {vector_size}, datatype: {datatype} not supported");
                    break;
#endif
            }

            code_pointer += 1;
        }

        void get_exp10_result([NoAlias]void* temp, ref int code_pointer, ref byte vector_size, [NoAlias]float4* register)
        {
            BlastVariableDataType datatype;
            pop_op_meta(code[code_pointer + 1], out datatype, out vector_size);
#if DEVELOPMENT_BUILD || TRACE
            if (datatype != BlastVariableDataType.Numeric)
            {
                Debug.LogError($"blast.ssmd.interpretor.exp10: datatype|vectorsize mismatch, vector_size: {vector_size}, datatype: {datatype} not supported");
                return;
            }
#endif

            switch (vector_size)
            {
                case 1:
                    {
                        pop_fx_into<float>(code_pointer + 1, (float*)temp);
                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            register[i].x = math.exp10(((float*)temp)[i]);
                        }
                    }
                    break;

                case 2:
                    {
                        pop_fx_into<float2>(code_pointer + 1, (float2*)temp);
                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            register[i].xy = math.exp10(((float2*)temp)[0]);
                        }
                    }
                    break;

                case 3:
                    {
                        pop_fx_into<float3>(code_pointer + 1, (float3*)temp);
                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            register[i].xyz = math.exp10(((float3*)temp)[0]);
                        }
                    }
                    break;

                case 0:
                case 4:
                    {
                        vector_size = 4;
                        pop_fx_into<float4>(code_pointer + 1, (float4*)temp);
                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            register[i] = math.exp10(((float4*)temp)[0]);
                        }
                    }
                    break;

#if DEVELOPMENT_BUILD || TRACE
                default:
                    Debug.LogError($"blast.ssmd.interpretor.exp10: datatype|vectorsize mismatch, vector_size: {vector_size}, datatype: {datatype} not supported");
                    break;
#endif
            }

            code_pointer += 1;
        }

        void get_exp_result([NoAlias]void* temp, ref int code_pointer, ref byte vector_size, [NoAlias]float4* register)
        {
            BlastVariableDataType datatype;
            pop_op_meta(code[code_pointer + 1], out datatype, out vector_size);
#if DEVELOPMENT_BUILD || TRACE
            if (datatype != BlastVariableDataType.Numeric)
            {
                Debug.LogError($"blast.ssmd.interpretor.exp: datatype|vectorsize mismatch, vector_size: {vector_size}, datatype: {datatype} not supported");
                return;
            }
#endif

            switch (vector_size)
            {
                case 1:
                    {
                        pop_fx_into<float>(code_pointer + 1, (float*)temp);
                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            register[i].x = math.exp(((float*)temp)[i]);
                        }
                    }
                    break;

                case 2:
                    {
                        pop_fx_into<float2>(code_pointer + 1, (float2*)temp);
                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            register[i].xy = math.exp(((float2*)temp)[0]);
                        }
                    }
                    break;

                case 3:
                    {
                        pop_fx_into<float3>(code_pointer + 1, (float3*)temp);
                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            register[i].xyz = math.exp(((float3*)temp)[0]);
                        }
                    }
                    break;

                case 0:
                case 4:
                    {
                        vector_size = 4;
                        pop_fx_into<float4>(code_pointer + 1, (float4*)temp);
                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            register[i] = math.exp(((float4*)temp)[0]);
                        }
                    }
                    break;

#if DEVELOPMENT_BUILD || TRACE
                default:
                    Debug.LogError($"blast.ssmd.interpretor.exp: datatype|vectorsize mismatch, vector_size: {vector_size}, datatype: {datatype} not supported");
                    break;
#endif
            }

            code_pointer += 1;
        }

        void get_sqrt_result([NoAlias]void* temp, ref int code_pointer, ref byte vector_size, [NoAlias]float4* register)
        {
            BlastVariableDataType datatype;
            pop_op_meta(code[code_pointer + 1], out datatype, out vector_size);
#if DEVELOPMENT_BUILD || TRACE
            if (datatype != BlastVariableDataType.Numeric)
            {
                Debug.LogError($"blast.ssmd.interpretor.sqrt: datatype|vectorsize mismatch, vector_size: {vector_size}, datatype: {datatype} not supported");
                return;
            }
#endif

            switch (vector_size)
            {
                case 1:
                    {
                        pop_fx_into<float>(code_pointer + 1, (float*)temp);
                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            register[i].x = math.sqrt(((float*)temp)[i]);
                        }
                    }
                    break;

                case 2:
                    {
                        pop_fx_into<float2>(code_pointer + 1, (float2*)temp);
                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            register[i].xy = math.sqrt(((float2*)temp)[0]);
                        }
                    }
                    break;

                case 3:
                    {
                        pop_fx_into<float3>(code_pointer + 1, (float3*)temp);
                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            register[i].xyz = math.sqrt(((float3*)temp)[0]);
                        }
                    }
                    break;

                case 0:
                case 4:
                    {
                        vector_size = 4;
                        pop_fx_into<float4>(code_pointer + 1, (float4*)temp);
                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            register[i] = math.sqrt(((float4*)temp)[0]);
                        }
                    }
                    break;

#if DEVELOPMENT_BUILD || TRACE
                default:
                    Debug.LogError($"blast.ssmd.interpretor.sqrt: datatype|vectorsize mismatch, vector_size: {vector_size}, datatype: {datatype} not supported");
                    break;
#endif
            }

            code_pointer += 1;
        }

        void get_rsqrt_result([NoAlias]void* temp, ref int code_pointer, ref byte vector_size, [NoAlias]float4* register)
        {
            BlastVariableDataType datatype;
            pop_op_meta(code[code_pointer + 1], out datatype, out vector_size);
#if DEVELOPMENT_BUILD || TRACE
            if (datatype != BlastVariableDataType.Numeric)
            {
                Debug.LogError($"blast.ssmd.interpretor.rsqrt: datatype|vectorsize mismatch, vector_size: {vector_size}, datatype: {datatype} not supported");
                return;
            }
#endif

            switch (vector_size)
            {
                case 1:
                    {
                        pop_fx_into<float>(code_pointer + 1, (float*)temp);
                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            register[i].x = math.rsqrt(((float*)temp)[i]);
                        }
                    }
                    break;

                case 2:
                    {
                        pop_fx_into<float2>(code_pointer + 1, (float2*)temp);
                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            register[i].xy = math.rsqrt(((float2*)temp)[0]);
                        }
                    }
                    break;

                case 3:
                    {
                        pop_fx_into<float3>(code_pointer + 1, (float3*)temp);
                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            register[i].xyz = math.rsqrt(((float3*)temp)[0]);
                        }
                    }
                    break;

                case 0:
                case 4:
                    {
                        vector_size = 4;
                        pop_fx_into<float4>(code_pointer + 1, (float4*)temp);
                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            register[i] = math.rsqrt(((float4*)temp)[0]);
                        }
                    }
                    break;

#if DEVELOPMENT_BUILD || TRACE
                default:
                    Debug.LogError($"blast.ssmd.interpretor.rsqrt: datatype|vectorsize mismatch, vector_size: {vector_size}, datatype: {datatype} not supported");
                    break;
#endif
            }

            code_pointer += 1;
        }

        #region Trigonometry
        void get_sin_result([NoAlias]void* temp, ref int code_pointer, ref byte vector_size, [NoAlias]float4* register)
        {
            BlastVariableDataType datatype;
            pop_op_meta(code[code_pointer + 1], out datatype, out vector_size);
#if DEVELOPMENT_BUILD || TRACE
            if (datatype != BlastVariableDataType.Numeric)
            {
                Debug.LogError($"blast.ssmd.interpretor.sin: datatype|vectorsize mismatch, vector_size: {vector_size}, datatype: {datatype} not supported");
                return;
            }
#endif

            switch (vector_size)
            {
                case 1:
                    {
                        pop_fx_into<float>(code_pointer + 1, (float*)temp);
                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            register[i].x = math.sin(((float*)temp)[i]);
                        }
                    }
                    break;

                case 2:
                    {
                        pop_fx_into<float2>(code_pointer + 1, (float2*)temp);
                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            register[i].xy = math.sin(((float2*)temp)[0]);
                        }
                    }
                    break;

                case 3:
                    {
                        pop_fx_into<float3>(code_pointer + 1, (float3*)temp);
                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            register[i].xyz = math.sin(((float3*)temp)[0]);
                        }
                    }
                    break;

                case 0:
                case 4:
                    {
                        pop_fx_into<float4>(code_pointer + 1, (float4*)temp);
                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            register[i] = math.sin(((float4*)temp)[0]);
                        }
                    }
                    break;

#if DEVELOPMENT_BUILD || TRACE
                default:
                    Debug.LogError($"blast.ssmd.interpretor.sin: datatype|vectorsize mismatch, vector_size: {vector_size}, datatype: {datatype} not supported");
                    break;
#endif
            }
            code_pointer += 1;
        }

        void get_cos_result([NoAlias]void* temp, ref int code_pointer, ref byte vector_size, [NoAlias]float4* register)
        {
            BlastVariableDataType datatype;
            pop_op_meta(code[code_pointer + 1], out datatype, out vector_size);
#if DEVELOPMENT_BUILD || TRACE
            if (datatype != BlastVariableDataType.Numeric)
            {
                Debug.LogError($"blast.ssmd.interpretor.cos: datatype|vectorsize mismatch, vector_size: {vector_size}, datatype: {datatype} not supported");
                return;
            }
#endif

            switch (vector_size)
            {
                case 1:
                    {
                        pop_fx_into<float>(code_pointer + 1, (float*)temp);
                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            register[i].x = math.cos(((float*)temp)[i]);
                        }
                    }
                    break;

                case 2:
                    {
                        pop_fx_into<float2>(code_pointer + 1, (float2*)temp);
                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            register[i].xy = math.cos(((float2*)temp)[0]);
                        }
                    }
                    break;

                case 3:
                    {
                        pop_fx_into<float3>(code_pointer + 1, (float3*)temp);
                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            register[i].xyz = math.cos(((float3*)temp)[0]);
                        }
                    }
                    break;

                case 0:
                case 4:
                    {
                        pop_fx_into<float4>(code_pointer + 1, (float4*)temp);
                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            register[i] = math.cos(((float4*)temp)[0]);
                        }
                    }
                    break;

#if DEVELOPMENT_BUILD || TRACE
                default:
                    Debug.LogError($"blast.ssmd.interpretor.cos: datatype|vectorsize mismatch, vector_size: {vector_size}, datatype: {datatype} not supported");
                    break;
#endif
            }

            code_pointer += 1;
        }

        void get_tan_result([NoAlias]void* temp, ref int code_pointer, ref byte vector_size, [NoAlias]float4* register)
        {
            BlastVariableDataType datatype;
            pop_op_meta(code[code_pointer + 1], out datatype, out vector_size);
#if DEVELOPMENT_BUILD || TRACE
            if (datatype != BlastVariableDataType.Numeric)
            {
                Debug.LogError($"blast.ssmd.interpretor.tan: datatype|vectorsize mismatch, vector_size: {vector_size}, datatype: {datatype} not supported");
                return;
            }
#endif

            switch (vector_size)
            {
                case 1:
                    {
                        pop_fx_into<float>(code_pointer + 1, (float*)temp);
                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            register[i].x = math.tan(((float*)temp)[i]);
                        }
                    }
                    break;

                case 2:
                    {
                        pop_fx_into<float2>(code_pointer + 1, (float2*)temp);
                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            register[i].xy = math.tan(((float2*)temp)[0]);
                        }
                    }
                    break;

                case 3:
                    {
                        pop_fx_into<float3>(code_pointer + 1, (float3*)temp);
                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            register[i].xyz = math.tan(((float3*)temp)[0]);
                        }
                    }
                    break;

                case 0:
                case 4:
                    {
                        pop_fx_into<float4>(code_pointer + 1, (float4*)temp);
                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            register[i] = math.tan(((float4*)temp)[0]);
                        }
                    }
                    break;

#if DEVELOPMENT_BUILD || TRACE
                default:
                    Debug.LogError($"blast.ssmd.interpretor.tan: datatype|vectorsize mismatch, vector_size: {vector_size}, datatype: {datatype} not supported");
                    break;
#endif
            }

            code_pointer += 1;
        }

        void get_sinh_result([NoAlias]void* temp, ref int code_pointer, ref byte vector_size, [NoAlias]float4* register)
        {
            BlastVariableDataType datatype;
            pop_op_meta(code[code_pointer + 1], out datatype, out vector_size);
#if DEVELOPMENT_BUILD || TRACE
            if (datatype != BlastVariableDataType.Numeric)
            {
                Debug.LogError($"blast.ssmd.interpretor.sinh: datatype|vectorsize mismatch, vector_size: {vector_size}, datatype: {datatype} not supported");
                return;
            }
#endif

            switch (vector_size)
            {
                case 1:
                    {
                        pop_fx_into<float>(code_pointer + 1, (float*)temp);
                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            register[i].x = math.sinh(((float*)temp)[i]);
                        }
                    }
                    break;

                case 2:
                    {
                        pop_fx_into<float2>(code_pointer + 1, (float2*)temp);
                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            register[i].xy = math.sinh(((float2*)temp)[0]);
                        }
                    }
                    break;

                case 3:
                    {
                        pop_fx_into<float3>(code_pointer + 1, (float3*)temp);
                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            register[i].xyz = math.sinh(((float3*)temp)[0]);
                        }
                    }
                    break;

                case 0:
                case 4:
                    {
                        pop_fx_into<float4>(code_pointer + 1, (float4*)temp);
                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            register[i] = math.sinh(((float4*)temp)[0]);
                        }
                    }
                    break;

#if DEVELOPMENT_BUILD || TRACE
                default:
                    Debug.LogError($"blast.ssmd.interpretor.sinh: datatype|vectorsize mismatch, vector_size: {vector_size}, datatype: {datatype} not supported");
                    break;
#endif
            }
            code_pointer += 1;
        }

        void get_cosh_result([NoAlias]void* temp, ref int code_pointer, ref byte vector_size, [NoAlias]float4* register)
        {
            BlastVariableDataType datatype;
            pop_op_meta(code[code_pointer + 1], out datatype, out vector_size);
#if DEVELOPMENT_BUILD || TRACE
            if (datatype != BlastVariableDataType.Numeric)
            {
                Debug.LogError($"blast.ssmd.interpretor.cosh: datatype|vectorsize mismatch, vector_size: {vector_size}, datatype: {datatype} not supported");
                return;
            }
#endif

            switch (vector_size)
            {
                case 1:
                    {
                        pop_fx_into<float>(code_pointer + 1, (float*)temp);
                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            register[i].x = math.cosh(((float*)temp)[i]);
                        }
                    }
                    break;

                case 2:
                    {
                        pop_fx_into<float2>(code_pointer + 1, (float2*)temp);
                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            register[i].xy = math.cosh(((float2*)temp)[0]);
                        }
                    }
                    break;

                case 3:
                    {
                        pop_fx_into<float3>(code_pointer + 1, (float3*)temp);
                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            register[i].xyz = math.cosh(((float3*)temp)[0]);
                        }
                    }
                    break;

                case 0:
                case 4:
                    {
                        pop_fx_into<float4>(code_pointer + 1, (float4*)temp);
                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            register[i] = math.cosh(((float4*)temp)[0]);
                        }
                    }
                    break;

#if DEVELOPMENT_BUILD || TRACE
                default:
                    Debug.LogError($"blast.ssmd.interpretor.cosh: datatype|vectorsize mismatch, vector_size: {vector_size}, datatype: {datatype} not supported");
                    break;
#endif
            }

            code_pointer += 1;
        }

        void get_atan_result([NoAlias]void* temp, ref int code_pointer, ref byte vector_size, [NoAlias]float4* register)
        {
            BlastVariableDataType datatype;
            pop_op_meta(code[code_pointer + 1], out datatype, out vector_size);
#if DEVELOPMENT_BUILD || TRACE
            if (datatype != BlastVariableDataType.Numeric)
            {
                Debug.LogError($"blast.ssmd.interpretor.atan: datatype|vectorsize mismatch, vector_size: {vector_size}, datatype: {datatype} not supported");
                return;
            }
#endif

            switch (vector_size)
            {
                case 1:
                    {
                        pop_fx_into<float>(code_pointer + 1, (float*)temp);
                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            register[i].x = math.atan(((float*)temp)[i]);
                        }
                    }
                    break;

                case 2:
                    {
                        pop_fx_into<float2>(code_pointer + 1, (float2*)temp);
                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            register[i].xy = math.atan(((float2*)temp)[0]);
                        }
                    }
                    break;

                case 3:
                    {
                        pop_fx_into<float3>(code_pointer + 1, (float3*)temp);
                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            register[i].xyz = math.atan(((float3*)temp)[0]);
                        }
                    }
                    break;

                case 0:
                case 4:
                    {
                        pop_fx_into<float4>(code_pointer + 1, (float4*)temp);
                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            register[i] = math.atan(((float4*)temp)[0]);
                        }
                    }
                    break;

#if DEVELOPMENT_BUILD || TRACE
                default:
                    Debug.LogError($"blast.ssmd.interpretor.atan: datatype|vectorsize mismatch, vector_size: {vector_size}, datatype: {datatype} not supported");
                    break;
#endif
            }

            code_pointer += 1;
        }

        void get_atan2_result([NoAlias]void* temp, ref int code_pointer, ref byte vector_size, [NoAlias]float4* register)
        {
            BlastVariableDataType datatype;
            pop_op_meta(code[code_pointer + 1], out datatype, out vector_size);

#if DEVELOPMENT_BUILD || TRACE
            if (datatype != BlastVariableDataType.Numeric)
            {
                Debug.LogError($"blast.ssmd.interpretor.atan2: datatype|vectorsize mismatch, vector_size: {vector_size}, datatype: {datatype} not supported");
                return;
            }
#endif

            switch (vector_size)
            {
                case 1:
                    {
                        float* fpa = (float*)temp;
                        float* fpb = &fpa[ssmd_datacount];

                        pop_f1_into(code_pointer + 1, fpa);
                        pop_f1_into(code_pointer + 2, fpb);

                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            register[i].x = math.atan2(fpa[i], fpb[i]);
                        }
                    }
                    break;

                case 2:
                    {
                        float2* fpa = (float2*)temp;
                        float2* fpb = &fpa[ssmd_datacount];

                        pop_fx_into<float2>(code_pointer + 1, fpa);
                        pop_fx_into<float2>(code_pointer + 2, fpb);

                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            register[i].xy = math.atan2(fpa[i], fpb[i]);
                        }
                    }
                    break;

                case 3:
                    {
                        float3* fpa = (float3*)temp;
                        float3* fpb = stackalloc float3[ssmd_datacount];

                        pop_fx_into<float3>(code_pointer + 1, fpa);
                        pop_fx_into<float3>(code_pointer + 2, fpb);

                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            register[i].xyz = math.atan2(fpa[i], fpb[i]);
                        }
                    }
                    break;

                case 0:
                case 4:
                    {
                        vector_size = 4;

                        float4* fpa = (float4*)temp;
                        float4* fpb = stackalloc float4[ssmd_datacount];

                        pop_fx_into<float4>(code_pointer + 1, fpa);
                        pop_fx_into<float4>(code_pointer + 1, fpb);

                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            register[i] = math.atan2(fpa[i], fpb[i]);
                        }
                    }
                    break;

#if DEVELOPMENT_BUILD || TRACE
                default:
                    Debug.LogError($"blast.ssmd.interpretor.atan2: datatype|vectorsize mismatch, vector_size: {vector_size}, datatype: {datatype} not supported");
                    break;
#endif
            }

            code_pointer += 2;
        }

        void get_degrees_result([NoAlias]void* temp, ref int code_pointer, ref byte vector_size, [NoAlias]float4* register)
        {
            BlastVariableDataType datatype;
            pop_op_meta(code[code_pointer + 1], out datatype, out vector_size);
#if DEVELOPMENT_BUILD || TRACE
            if (datatype != BlastVariableDataType.Numeric)
            {
                Debug.LogError($"blast.ssmd.interpretor.degrees: datatype|vectorsize mismatch, vector_size: {vector_size}, datatype: {datatype} not supported");
                return;
            }
#endif

            switch (vector_size)
            {
                case 1:
                    {
                        pop_fx_into<float>(code_pointer + 1, (float*)temp);
                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            register[i].x = math.degrees(((float*)temp)[i]);
                        }
                    }
                    break;

                case 2:
                    {
                        pop_fx_into<float2>(code_pointer + 1, (float2*)temp);
                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            register[i].xy = math.degrees(((float2*)temp)[0]);
                        }
                    }
                    break;

                case 3:
                    {
                        pop_fx_into<float3>(code_pointer + 1, (float3*)temp);
                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            register[i].xyz = math.degrees(((float3*)temp)[0]);
                        }
                    }
                    break;

                case 0:
                case 4:
                    {
                        pop_fx_into<float4>(code_pointer + 1, (float4*)temp);
                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            register[i] = math.degrees(((float4*)temp)[0]);
                        }
                    }
                    break;

#if DEVELOPMENT_BUILD || TRACE
                default:
                    Debug.LogError($"blast.ssmd.interpretor.degrees: datatype|vectorsize mismatch, vector_size: {vector_size}, datatype: {datatype} not supported");
                    break;
#endif
            }

            code_pointer += 1;
        }

        void get_radians_result([NoAlias]void* temp, ref int code_pointer, ref byte vector_size, [NoAlias]float4* register)
        {
            BlastVariableDataType datatype;
            pop_op_meta(code[code_pointer + 1], out datatype, out vector_size);
#if DEVELOPMENT_BUILD || TRACE
            if (datatype != BlastVariableDataType.Numeric)
            {
                Debug.LogError($"blast.ssmd.interpretor.radians: datatype|vectorsize mismatch, vector_size: {vector_size}, datatype: {datatype} not supported");
                return;
            }
#endif

            switch (vector_size)
            {
                case 1:
                    {
                        pop_fx_into<float>(code_pointer + 1, (float*)temp);
                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            register[i].x = math.radians(((float*)temp)[i]);
                        }
                    }
                    break;

                case 2:
                    {
                        pop_fx_into<float2>(code_pointer + 1, (float2*)temp);
                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            register[i].xy = math.radians(((float2*)temp)[0]);
                        }
                    }
                    break;

                case 3:
                    {
                        pop_fx_into<float3>(code_pointer + 1, (float3*)temp);
                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            register[i].xyz = math.radians(((float3*)temp)[0]);
                        }
                    }
                    break;

                case 0:
                case 4:
                    {
                        pop_fx_into<float4>(code_pointer + 1, (float4*)temp);
                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            register[i] = math.radians(((float4*)temp)[0]);
                        }
                    }
                    break;

#if DEVELOPMENT_BUILD || TRACE
                default:
                    Debug.LogError($"blast.ssmd.interpretor.radians: datatype|vectorsize mismatch, vector_size: {vector_size}, datatype: {datatype} not supported");
                    break;
#endif
            }

            code_pointer += 1;
        }
        #endregion
        #endregion

        #region Dual input functions: math.pow

        void get_pow_result([NoAlias]void* temp, ref int code_pointer, ref byte vector_size, [NoAlias]float4* register)
        {
            BlastVariableDataType datatype;
            pop_op_meta(code[code_pointer + 1], out datatype, out vector_size);

#if DEVELOPMENT_BUILD || TRACE
            if (datatype != BlastVariableDataType.Numeric)
            {
                Debug.LogError($"blast.ssmd.interpretor.pow: datatype|vectorsize mismatch, vector_size: {vector_size}, datatype: {datatype} not supported");
                return;
            }
#endif

            switch (vector_size)
            {
                case 1:
                    {
                        float* fpa = (float*)temp;
                        float* fpb = &fpa[ssmd_datacount];

                        pop_f1_into(code_pointer + 1, fpa);
                        pop_f1_into(code_pointer + 2, fpb);

                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            register[i].x = math.pow(fpa[i], fpb[i]);
                        }
                    }
                    break;

                case 2:
                    {
                        float2* fpa = (float2*)temp;
                        float2* fpb = &fpa[ssmd_datacount];

                        pop_fx_into<float2>(code_pointer + 1, fpa);
                        pop_fx_into<float2>(code_pointer + 2, fpb);

                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            register[i].xy = math.pow(fpa[i], fpb[i]);
                        }
                    }
                    break;

                case 3:
                    {
                        float3* fpa = (float3*)temp;
                        float3* fpb = stackalloc float3[ssmd_datacount]; // TODO test if this does not result in 2 x allocation in every case.,,..

                        pop_fx_into<float3>(code_pointer + 1, fpa);
                        pop_fx_into<float3>(code_pointer + 2, fpb);

                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            register[i].xyz = math.pow(fpa[i], fpb[i]);
                        }
                    }
                    break;

                case 0:
                case 4:
                    {
                        vector_size = 4;

                        float4* fpa = (float4*)temp;
                        float4* fpb = stackalloc float4[ssmd_datacount]; // TODO test if this does not result in 2 x allocation in every case.,,..

                        pop_fx_into<float4>(code_pointer + 1, fpa);
                        pop_fx_into<float4>(code_pointer + 1, fpb);

                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            register[i] = math.pow(fpa[i], fpb[i]);
                        }
                    }
                    break;

#if DEVELOPMENT_BUILD || TRACE
                default:
                    Debug.LogError($"blast.ssmd.interpretor.pow: datatype|vectorsize mismatch, vector_size: {vector_size}, datatype: {datatype} not supported");
                    break;
#endif
            }

            code_pointer += 2;
        }

        /// <summary>
        /// only accepts size 3 vectors 
        /// </summary>
        void get_cross_result([NoAlias]void* temp, ref int code_pointer, ref byte vector_size, [NoAlias]float4* register)
        {
            BlastVariableDataType datatype;
            pop_op_meta(code[code_pointer + 1], out datatype, out vector_size);

#if DEVELOPMENT_BUILD || TRACE
            if (datatype != BlastVariableDataType.Numeric || vector_size != 3)
            {
                Debug.LogError($"blast.ssmd.interpretor.cross: datatype|vectorsize mismatch, vector_size: {vector_size}, datatype: {datatype} not supported");
                return;
            }
#endif

            switch (vector_size)
            {
                case 3:
                    {
                        float3* fpa = (float3*)temp;
                        float3* fpb = stackalloc float3[ssmd_datacount];

                        pop_fx_into<float3>(code_pointer + 1, fpa);
                        pop_fx_into<float3>(code_pointer + 2, fpb);

                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            register[i].xyz = math.pow(fpa[i], fpb[i]);
                        }
                    }
                    break;

#if DEVELOPMENT_BUILD || TRACE
                default:
                    Debug.LogError($"blast.ssmd.interpretor.pow: datatype|vectorsize mismatch, vector_size: {vector_size}, datatype: {datatype} not supported");
                    break;
#endif
            }

            code_pointer += 2;

        }


        /// <summary>
        /// result vector size == 1 in all cases
        /// </summary>
        /// <param name="temp"></param>
        /// <param name="code_pointer"></param>
        /// <param name="vector_size"></param>
        /// <param name="register"></param>
        void get_dot_result([NoAlias]void* temp, ref int code_pointer, ref byte vector_size, [NoAlias]float4* register)
        {
            BlastVariableDataType datatype;
            pop_op_meta(code[code_pointer + 1], out datatype, out vector_size);

#if DEVELOPMENT_BUILD || TRACE
            if (datatype != BlastVariableDataType.Numeric)
            {
                Debug.LogError($"blast.ssmd.interpretor.dot: datatype|vectorsize mismatch, vector_size: {vector_size}, datatype: {datatype} not supported");
                return;
            }
#endif
            switch (vector_size)
            {
                case 1:
                    {
                        float* fpa = (float*)temp;
                        float* fpb = &fpa[ssmd_datacount];

                        pop_f1_into(code_pointer + 1, fpa);
                        pop_f1_into(code_pointer + 2, fpb);

                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            register[i].x = math.dot(fpa[i], fpb[i]);
                        }
                    }
                    break;

                case 2:
                    {
                        float2* fpa = (float2*)temp;
                        float2* fpb = &fpa[ssmd_datacount];

                        pop_fx_into<float2>(code_pointer + 1, fpa);
                        pop_fx_into<float2>(code_pointer + 2, fpb);

                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            register[i].x = math.dot(fpa[i], fpb[i]);
                        }
                    }
                    break;

                case 3:
                    {
                        float3* fpa = (float3*)temp;
                        float3* fpb = stackalloc float3[ssmd_datacount];

                        pop_fx_into<float3>(code_pointer + 1, fpa);
                        pop_fx_into<float3>(code_pointer + 2, fpb);

                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            register[i].x = math.dot(fpa[i], fpb[i]);
                        }
                    }
                    break;

                case 0:
                case 4:
                    {
                        float4* fpa = (float4*)temp;
                        float4* fpb = stackalloc float4[ssmd_datacount];

                        pop_fx_into<float4>(code_pointer + 1, fpa);
                        pop_fx_into<float4>(code_pointer + 1, fpb);

                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            register[i].x = math.dot(fpa[i], fpb[i]);
                        }
                    }
                    break;

#if DEVELOPMENT_BUILD || TRACE
                default:
                    Debug.LogError($"blast.ssmd.interpretor.dot: datatype|vectorsize mismatch, vector_size: {vector_size}, datatype: {datatype} not supported");
                    break;
#endif
            }

            code_pointer += 2;
            vector_size = 1;
        }




        #endregion

        #region Tripple inputs: select, clamp, lerp etc

        /// <summary>
        /// select first input on a false condition, select the second on a true condition that is put in parameter 3
        /// </summary>
        /// <remarks>
        /// 3 inputs, first 2 any type, 3rd type equal or scalar bool
        /// 
        /// c[x] = select(a[x], b[x], condition[1/x])
        /// 
        /// - input = outputvectorsize
        /// - 1 output  
        /// 
        /// Assembly for scalar constants<c>a = select(1, 2, 1) => setf a select 1 2 1 nop</c>
        /// 
        /// Assembly sample with vectors: <c>a = select((1 1), (2 2), 1) => setf a select 1 pop pop nop</c>
        /// </remarks>
        void get_select_result([NoAlias]void* temp, ref int code_pointer, ref byte vector_size, [NoAlias]float4* f4)
        {
            // upon entry code_pointer is located on the function operation 
            BlastVariableDataType datatype;
            byte vector_size_condition;

            //
            // need to reorder parameters while compiling for better memory efficienty executing it 
            // - if we can execute the condition first we can skip reading 1 of the parameters (we do need to 
            //   move stackpointers though)
            // - compiler update for SSMD only? or also normal?
            // 

            // peek vectorsize of first option
            pop_op_meta(code[code_pointer + 1], out datatype, out vector_size);

            //
            // IMPORTANT
            // stack allocations are moved to the start of the function and are always done,
            // regardless conditionals, avoid allocating too much memory by calculating the amount needed 
            // and casting the resulting pointers in each vectorsize-case
            //
            int data_byte_size = 4 * ssmd_datacount * vector_size;

            // we could use memory from f4 to store 1 array, but that would probably mess up vectorizing the inner loops
            byte* local1 = stackalloc byte[data_byte_size];
            byte* local2 = stackalloc byte[data_byte_size];

            //
            // allocations could also be avoided, with each combination (constant/pop/var) and each vectorsize of each parameter in a seperate case... 
            // this will happen a lot as generics is unusable.. codegen? TODO
            //

            switch (vector_size)
            {

                case 1:

                    float* o1 = (float*)(void*)local1;
                    float* o2 = (float*)(void*)local2;

                    pop_f1_into(code_pointer + 1, o1);
                    pop_f1_into(code_pointer + 2, o2);
                    pop_f1_into(code_pointer + 3, (float*)temp);

                    for (int i = 0; i < ssmd_datacount; i++)
                    {
                        // not using f4 for storing any operand should allow for the compiler to optimize better
                        // we could also optimize this by handling 4 at once in a float4.. TODO
                        f4[i].x = math.select(o1[i], o2[i], ((float*)temp)[i] != 0);
                    }
                    break;


                case 2:
                    float2* o21 = (float2*)(void*)local1;
                    float2* o22 = (float2*)(void*)local2;

                    pop_fx_into<float2>(code_pointer + 1, o21);
                    pop_fx_into<float2>(code_pointer + 2, o22);
                    pop_op_meta(code[code_pointer + 3], out datatype, out vector_size_condition);

                    if (vector_size_condition == 1)
                    {
                        pop_fx_into<float>(code_pointer + 3, (float*)temp);
                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            f4[i].xy = math.select(o21[i], o22[i], ((float*)temp)[i] != 0);
                        }
                    }
                    else
                    {
                        pop_fx_into<float2>(code_pointer + 3, (float2*)temp);
                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            f4[i].xy = math.select(o21[i], o22[i], ((float2*)temp)[i] != 0);
                        }
                    }
                    break;

                case 3:
                    float3* o31 = (float3*)(void*)local1;
                    float3* o32 = (float3*)(void*)local2;

                    pop_fx_into<float3>(code_pointer + 1, o31);
                    pop_fx_into<float3>(code_pointer + 2, o32);
                    pop_op_meta(code[code_pointer + 3], out datatype, out vector_size_condition);

                    if (vector_size_condition == 1)
                    {
                        pop_fx_into<float>(code_pointer + 3, (float*)temp);
                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            f4[i].xyz = math.select(o31[i], o32[i], ((float*)temp)[i] != 0);
                        }
                    }
                    else
                    {
                        pop_fx_into<float3>(code_pointer + 3, (float3*)temp);
                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            f4[i].xyz = math.select(o31[i], o32[i], ((float3*)temp)[i] != 0);
                        }
                    }
                    break;

                case 0:
                case 4:
                    {
                        vector_size = 4;

                        float4* o41 = (float4*)(void*)local1;
                        float4* o42 = (float4*)(void*)local2;

                        pop_fx_into<float4>(code_pointer + 1, o41);
                        pop_fx_into<float4>(code_pointer + 2, o42);
                        pop_op_meta(code[code_pointer + 3], out datatype, out vector_size_condition);

                        if (vector_size_condition == 1)
                        {
                            pop_fx_into<float>(code_pointer + 3, (float*)temp);
                            for (int i = 0; i < ssmd_datacount; i++)
                            {
                                f4[i] = math.select(o41[i], o42[i], ((float4*)temp)[i].x != 0);
                            }
                        }
                        else
                        {
                            pop_fx_into<float4>(code_pointer + 3, (float4*)temp);
                            for (int i = 0; i < ssmd_datacount; i++)
                            {
                                f4[i] = math.select(o41[i], o42[i], ((float4*)temp)[i] != 0);
                            }
                        }
                    }
                    break;

#if DEVELOPMENT_BUILD || TRACE
                default:
                    Debug.LogError($"blast.ssmd.interpretor.select: datatype|vectorsize mismatch, vector_size: {vector_size}, datatype: {datatype} not supported");
                    break;
#endif
            }

            code_pointer += 3;
        }


        /// <summary>
        /// clamp value between operands
        /// </summary>
        /// <remarks>
        /// first min then max => saves 1 memory buffer doing this in sequence (math lib will also do min(max( so this should be equal in performance but turned upside down in flow
        /// <code>
        /// 
        /// a = clamp(a, min, max); 
        /// 
        /// </code>
        /// </remarks>
        void get_clamp_result([NoAlias]void* temp, ref int code_pointer, ref byte vector_size, [NoAlias] float4* f4)
        {
            BlastVariableDataType datatype;
            pop_op_meta(code[code_pointer + 1], out datatype, out vector_size);

#if DEVELOPMENT_BUILD || TRACE
            if (datatype != BlastVariableDataType.Numeric)
            {
                Debug.LogError($"blast.ssmd.interpretor.clamp: datatype|vectorsize mismatch, vector_size: {vector_size}, datatype: {datatype} not supported");
                return;
            }
#endif

            switch (vector_size)
            {
                case 1:
                    {
                        // in case of v1 there is no memory problem 
                        float* fpa = (float*)temp;
                        float* fp_min = &fpa[ssmd_datacount];
                        float* fp_max = &fp_min[ssmd_datacount];

                        pop_f1_into(code_pointer + 1, fpa);
                        pop_f1_into(code_pointer + 2, fp_min);
                        pop_f1_into(code_pointer + 3, fp_max);

                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            register[i].x = math.clamp(fpa[i], fp_min[i], fp_max[i]); 
                        }
                    }
                    break;

                case 2:
                    {
                        // 2 float2's fit in temp, split op in 2 to save on the allocation
                        float2* fpa = (float2*)temp;
                        float2* fp_m = &fpa[ssmd_datacount];

                        pop_fx_into<float2>(code_pointer + 1, fpa);
                        pop_fx_into<float2>(code_pointer + 2, fp_m);
                        
                        // first the lower bound 
                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            fpa[i] = math.max(fpa[i], fp_m[i]);
                        }

                        // then the upper bound and write to output 
                        pop_fx_into<float2>(code_pointer + 3, fp_m);
                        for(int i = 0; i < ssmd_datacount; i++)
                        {
                            register[i].xy = math.min(fpa[i], fp_m[i]); 
                        }
                    }
                    break;

                case 3:
                    {
                        // 2 float3;s wont fit in temp anymore 
                        float3* fpa = (float3*)temp;
                        float3* fp_m = stackalloc float3[ssmd_datacount]; 

                        pop_fx_into<float3>(code_pointer + 1, fpa);
                        pop_fx_into<float3>(code_pointer + 2, fp_m);

                        // first the lower bound 
                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            fpa[i] = math.max(fpa[i], fp_m[i]);
                        }

                        // then the upper bound and write to output 
                        pop_fx_into<float3>(code_pointer + 3, fp_m);
                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            register[i].xyz = math.min(fpa[i], fp_m[i]);
                        }
                    }
                    break;

                case 0:
                case 4:
                    {
                        // 2 float4;s wont fit in temp anymore 
                        float4* fpa = (float4*)temp;
                        float4* fp_m = stackalloc float4[ssmd_datacount];

                        pop_fx_into<float4>(code_pointer + 1, fpa);
                        pop_fx_into<float4>(code_pointer + 2, fp_m);

                        // first the lower bound 
                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            fpa[i] = math.max(fpa[i], fp_m[i]);
                        }

                        // then the upper bound and write to output 
                        pop_fx_into<float4>(code_pointer + 3, fp_m);
                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            register[i] = math.min(fpa[i], fp_m[i]);
                        }
                    }
                    break;

#if DEVELOPMENT_BUILD || TRACE
                default:
                    Debug.LogError($"blast.ssmd.interpretor.clamp: datatype|vectorsize mismatch, vector_size: {vector_size}, datatype: {datatype} not supported");
                    break;
#endif
            }

            code_pointer += 3;
        }

        /// <summary>
        /// lerp value from min to max 
        /// </summary>
        /// <remarks>
        /// 
        /// a = lerp(min, max, s); 
        /// 
        /// TODO :     for all 3 input functions:   we should test stackallocs if they are conditional or not
        /// - if they are always allocated move every case with stackalloc into its own function ? code explosin without templates/generics
        ///  
        /// </remarks>
        void get_lerp_result([NoAlias]void* temp, ref int code_pointer, ref byte vector_size, [NoAlias] float4* f4)
        {
            BlastVariableDataType datatype;
            pop_op_meta(code[code_pointer + 1], out datatype, out vector_size);

#if DEVELOPMENT_BUILD || TRACE
            if (datatype != BlastVariableDataType.Numeric)
            {
                Debug.LogError($"blast.ssmd.interpretor.lerp: datatype|vectorsize mismatch, vector_size: {vector_size}, datatype: {datatype} not supported");
                return;
            }
#endif

            switch (vector_size)
            {
                case 1:
                    {
                        // in case of v1 there is no memory problem 
                        float* fpa = (float*)temp;
                        float* fp_min = &fpa[ssmd_datacount];
                        float* fp_max = &fp_min[ssmd_datacount];

                        pop_f1_into(code_pointer + 1, fpa);
                        pop_f1_into(code_pointer + 2, fp_min);
                        pop_f1_into(code_pointer + 3, fp_max);

                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            register[i].x = math.lerp(fp_min[i], fp_max[i], fpa[i]);
                        }
                    }
                    break;

                case 2:
                    {
                        float2* fpa = (float2*)temp;
                        float2* fp_min = &fpa[ssmd_datacount];
                        float2* fp_max = stackalloc float2[ssmd_datacount]; 

                        pop_fx_into<float2>(code_pointer + 1, fpa);
                        pop_fx_into<float2>(code_pointer + 2, fp_min);
                        pop_fx_into<float2>(code_pointer + 3, fp_max);

                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            register[i].xy = math.lerp(fp_min[i], fp_max[i], fpa[i]);
                        }

                    }
                    break;

                case 3:
                    {
                        float3* fpa = (float3*)temp;
                        float3* fp_min = stackalloc float3[ssmd_datacount];
                        float3* fp_max = stackalloc float3[ssmd_datacount];

                        pop_fx_into<float3>(code_pointer + 1, fpa);
                        pop_fx_into<float3>(code_pointer + 2, fp_min);
                        pop_fx_into<float3>(code_pointer + 3, fp_max);

                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            register[i].xyz = math.lerp(fp_min[i], fp_max[i], fpa[i]);
                        }
                    }
                    break;

                case 0:
                case 4:
                    {
                        float4* fpa = (float4*)temp;
                        float4* fp_min = stackalloc float4[ssmd_datacount];
                        float4* fp_max = f4; // only on equal vectorsize can we reuse output buffer because of how we overwrite our values writing to the output if the source is smaller in size

                        pop_fx_into<float4>(code_pointer + 1, fpa);
                        pop_fx_into<float4>(code_pointer + 2, fp_min);
                        pop_fx_into<float4>(code_pointer + 3, fp_max);

                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            register[i] = math.lerp(fp_min[i], fp_max[i], fpa[i]);
                        }
                    }
                    break;

#if DEVELOPMENT_BUILD || TRACE
                default:
                    Debug.LogError($"blast.ssmd.interpretor.lerp: datatype|vectorsize mismatch, vector_size: {vector_size}, datatype: {datatype} not supported");
                    break;
#endif
            }

            code_pointer += 3;
        }


        #endregion
 
        #region maxa|mina|mula|op-a etc. 

        /// <summary>
        /// get the minimal component from all arguments of any vectorsize
        /// </summary>
        void get_mina_result([NoAlias]void* temp, ref int code_pointer, ref byte vector_size, [NoAlias]float4* f4)
        {
            code_pointer++;
            byte c = BlastInterpretor.decode62(in code[code_pointer], ref vector_size);

            // regardless, output vectorsize is always 1
            vector_size = 1;
            BlastVariableDataType datatype = default;
            byte popped_vector_size = 0;

            pop_op_meta(code[code_pointer], out datatype, out popped_vector_size);
            switch (popped_vector_size)
            {
                case 1:
                    {
                        // direct pop into f4.x
                        pop_f1_into_f4(code_pointer, f4);
                        break; 
                    }
 
                case 2:
                    {
                        pop_fx_into<float2>(code_pointer, (float2*)temp);
                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            f4[i].x = math.cmin(((float2*)temp)[i]);
                        }
                        break; 
                    }

                case 3:
                    {
                        pop_fx_into<float3>(code_pointer, (float3*)temp);
                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            f4[i].x = math.cmin(((float3*)temp)[i]);
                        }
                        break;
                    }

                case 0:
                case 4:
                    {
                        pop_fx_into<float4>(code_pointer, (float4*)temp);
                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            f4[i].x = math.cmin(((float4*)temp)[i]);
                        }

                        break;
                    }
#if DEVELOPMENT_BUILD || TRACE
                default:
                    Debug.LogError($"blast.ssmd.interpretor.mina: datatype|vectorsize mismatch, vector_size: {vector_size}, datatype: {datatype} not supported");
                    break;
#endif
            }
            c--;

            // c == nr of parameters ...  
            // run the rest of the parameters
            while (c > 0)
            {
                code_pointer++;
                c--;

                pop_op_meta(code[code_pointer], out datatype, out popped_vector_size);
                switch (popped_vector_size)
                {
                    case 0:
                    case 4:
                        pop_fx_into<float4>(code_pointer, (float4*)temp);

                        for(int i = 0; i < ssmd_datacount; i++)
                        {
                            f4[i].x = math.min(f4[i].x, math.cmin(((float4*)temp)[i])); 
                        }
                        break;

                    case 1:
                        pop_fx_into<float>(code_pointer, (float*)temp);
                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            f4[i].x = math.min(f4[i].x, ((float*)temp)[i]);
                        }
                        break;

                    case 2:
                        pop_fx_into<float2>(code_pointer, (float2*)temp);
                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            f4[i].x = math.min(f4[i].x, math.cmin(((float2*)temp)[i]));
                        }
                        break;

                    case 3:
                        pop_fx_into<float3>(code_pointer, (float3*)temp);
                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            f4[i].x = math.min(f4[i].x, math.cmin(((float3*)temp)[i]));
                        }
                        break;
#if DEVELOPMENT_BUILD || TRACE
                    default:
                        Debug.LogError($"blast.ssmd.interpretor.mina: datatype|vectorsize mismatch, vector_size: {vector_size}, datatype: {datatype} not supported");
                        break;
#endif
                }
            }
        }

        /// <summary>
        /// get the max component from all arguments of any vectorsize
        /// </summary>
        void get_maxa_result([NoAlias]void* temp, ref int code_pointer, ref byte vector_size, [NoAlias]float4* f4)
        {
            code_pointer++;
            byte c = BlastInterpretor.decode62(in code[code_pointer], ref vector_size);

            // regardless, output vectorsize is always 1
            vector_size = 1;
            BlastVariableDataType datatype = default;
            byte popped_vector_size = 0;

            pop_op_meta(code[code_pointer], out datatype, out popped_vector_size);
            switch (popped_vector_size)
            {
                case 1:
                    {
                        // direct pop into f4.x
                        pop_f1_into_f4(code_pointer, f4);
                        break;
                    }

                case 2:
                    {
                        pop_fx_into<float2>(code_pointer, (float2*)temp);
                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            f4[i].x = math.cmax(((float2*)temp)[i]);
                        }
                        break;
                    }

                case 3:
                    {
                        pop_fx_into<float3>(code_pointer, (float3*)temp);
                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            f4[i].x = math.cmax(((float3*)temp)[i]);
                        }
                        break;
                    }

                case 0:
                case 4:
                    {
                        pop_fx_into<float4>(code_pointer, (float4*)temp);
                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            f4[i].x = math.cmax(((float4*)temp)[i]);
                        }

                        break;
                    }
#if DEVELOPMENT_BUILD || TRACE
                default:
                    Debug.LogError($"blast.ssmd.interpretor.maxa: datatype|vectorsize mismatch, vector_size: {vector_size}, datatype: {datatype} not supported");
                    break;
#endif
            }
            c--;

            // c == nr of parameters ...  
            // run the rest of the parameters
            while (c > 0)
            {
                code_pointer++;
                c--;

                pop_op_meta(code[code_pointer], out datatype, out popped_vector_size);
                switch (popped_vector_size)
                {
                    case 0:
                    case 4:
                        pop_fx_into<float4>(code_pointer, (float4*)temp);

                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            f4[i].x = math.max(f4[i].x, math.cmax(((float4*)temp)[i]));
                        }
                        break;

                    case 1:
                        pop_fx_into<float>(code_pointer, (float*)temp);
                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            f4[i].x = math.max(f4[i].x, ((float*)temp)[i]);
                        }
                        break;

                    case 2:
                        pop_fx_into<float2>(code_pointer, (float2*)temp);
                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            f4[i].x = math.max(f4[i].x, math.cmax(((float2*)temp)[i]));
                        }
                        break;

                    case 3:
                        pop_fx_into<float3>(code_pointer, (float3*)temp);
                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            f4[i].x = math.max(f4[i].x, math.cmax(((float3*)temp)[i]));
                        }
                        break;
#if DEVELOPMENT_BUILD || TRACE
                    default:
                        Debug.LogError($"blast.ssmd.interpretor.max: datatype|vectorsize mismatch, vector_size: {vector_size}, datatype: {datatype} not supported");
                        break;
#endif
                }
            }
        }


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
        /// <param name="operation">operation to take on the vectors</param>
        void get_op_a_result([NoAlias]void* temp, ref int code_pointer, ref byte vector_size, [NoAlias]ref float4* f4, blast_operation operation)
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
                        pop_f4_with_op_into_f4(code_pointer + j, (float4*)temp, f4, blast_operation.multiply);
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
                    code_pointer++;
                    pop_fx_into<float2>(in code_pointer, (float2*)temp);

                    for (int j = 1; j < c - 1; j++)
                    {
                        pop_f2_with_op_into_f2(code_pointer + j, (float2*)temp, (float2*)temp, operation);
                    }

                    pop_f2_with_op_into_f4(code_pointer + c - 1, (float2*)temp, f4, operation);
                    code_pointer += c - 1;
                    break;

                case 3:
                    code_pointer++;
                    pop_fx_into<float3>(in code_pointer, (float3*)temp);

                    for (int j = 1; j < c - 1; j++)
                    {
                        pop_f3_with_op_into_f3(code_pointer + j, (float3*)temp, (float3*)temp, operation);
                    }

                    pop_f3_with_op_into_f4(code_pointer + c - 1, (float3*)temp, f4, operation);
                    code_pointer += c - 1;
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
        void get_fma_result([NoAlias]void* temp, ref int code_pointer, ref byte vector_size, [NoAlias]ref float4* f4)
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

        #region External Function Handler 

        void get_external_function_result([NoAlias]void* temp, ref int code_pointer, ref byte vector_size, [NoAlias] float4* f4)
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
#if DEVELOPMENT_BUILD
                    if (id > (int)ReservedBlastScriptFunctionIds.Offset && p.MinParameterCount != p.MaxParameterCount)
                    {
                        Debug.LogError($"blast.ssmd.interpretor.call-external: function {id} min/max parameters should be of equal size");
                    }
#endif
                    code_pointer += p.MinParameterCount;
                    return;
                }

#if DEVELOPMENT_BUILD || TRACE 
                Debug.Log($"call fp id: {id} <env:{environment_ptr.ToInt64()}> <ssmd:no-caller-data>, parmetercount = {p.MinParameterCount}");
#endif


#if DEVELOPMENT_BUILD || TRACE
                vector_size = (byte)math.select((int)p.ReturnsVectorSize, p.AcceptsVectorSize, p.ReturnsVectorSize == 0);
                if (vector_size != 1)
                {
                    Debug.LogError($"blast.ssmd.interpretor.call-external: function {id} a return vector size > 1 is not currently supported in external functions");
                    return;
                }
                if (p.AcceptsVectorSize != 1)
                {
                    Debug.LogError($"blast.ssmd.interpretor.call-external: function {id} a parameter vector size > 1 is not currently supported in external functions");
                    return;
                }
#else 
                vector_size = 1; 
#endif 

                //
                // as we enforce vectorsize == 1 on external function calls, we can cast all down to float and save memory
                //

                switch (p.MinParameterCount)
                {
                    // zero parameters 
                    case 0:
                        FunctionPointer<Blast.BlastDelegate_f0> fp0 = p.Generic<Blast.BlastDelegate_f0>();
                        if (fp0.IsCreated && fp0.Value != IntPtr.Zero)
                        {
                            vector_size = 1;

                            for (int i = 0; i < ssmd_datacount; i++)
                            {
                                f4[i].x = fp0.Invoke((IntPtr)engine_ptr, environment_ptr, IntPtr.Zero);
                            }
                            return;
                        }
                        break;

                    //
                    // obviously this would be a whole lot more efficient if we vectorize the external functions 
                    // - this is however not always good: 
                    // - if the function is expensive to execute there would be very little benefit
                    // - small lightweight functions would benefit greatly
                    // - user should be able to optionally register a vectorized variant and we use that here if present
                    // 
                    case 1:
                        FunctionPointer<Blast.BlastDelegate_f1> fp1 = p.Generic<Blast.BlastDelegate_f1>();
                        if (fp1.IsCreated && fp1.Value != IntPtr.Zero)
                        {
                            pop_f1_into(code_pointer + 1, (float*)temp);

                            code_pointer++;

                            for (int i = 0; i < ssmd_datacount; i++)
                            {
                                f4[i].x = fp1.Invoke((IntPtr)engine_ptr, environment_ptr, IntPtr.Zero, ((float*)temp)[i]);
                            }
                            return;
                        }
                        break;

                    case 2:
                        FunctionPointer<Blast.BlastDelegate_f2> fp2 = p.Generic<Blast.BlastDelegate_f2>();
                        if (fp2.IsCreated && fp2.Value != IntPtr.Zero)
                        {
                            float* fpa = (float*)temp;
                            float* fpb = &fpa[ssmd_datacount]; // this works because temp is of size full vector

                            pop_f1_into(code_pointer + 1, fpa);
                            pop_f1_into(code_pointer + 2, fpb);

                            code_pointer += 2;
                            for (int i = 0; i < ssmd_datacount; i++)
                            {
                                f4[i].x = fp2.Invoke((IntPtr)engine_ptr, environment_ptr, IntPtr.Zero, fpa[i], fpb[i]);
                            }

                            return;
                        }
                        break;

                    case 3:
                        FunctionPointer<Blast.BlastDelegate_f3> fp3 = p.Generic<Blast.BlastDelegate_f3>();
                        if (fp3.IsCreated && fp3.Value != IntPtr.Zero)
                        {
                            float* fpa = (float*)temp;
                            float* fpb = &fpa[ssmd_datacount];
                            float* fpc = &fpb[ssmd_datacount];

                            pop_f1_into(code_pointer + 1, fpa);
                            pop_f1_into(code_pointer + 2, fpb);
                            pop_f1_into(code_pointer + 3, fpc);

                            code_pointer += 3;
                            for (int i = 0; i < ssmd_datacount; i++)
                            {
                                f4[i].x = fp3.Invoke((IntPtr)engine_ptr, environment_ptr, IntPtr.Zero, fpa[i], fpb[i], fpc[i]);
                            }
                            return;
                        }
                        break;

                    case 4:
                        FunctionPointer<Blast.BlastDelegate_f4> fp4 = p.Generic<Blast.BlastDelegate_f4>();
                        if (fp4.IsCreated && fp4.Value != IntPtr.Zero)
                        {
                            float* fpa = (float*)temp;
                            float* fpb = &fpa[ssmd_datacount];
                            float* fpc = &fpb[ssmd_datacount];
                            float* fpd = &fpc[ssmd_datacount];

                            pop_f1_into(code_pointer + 1, fpa);
                            pop_f1_into(code_pointer + 2, fpb);
                            pop_f1_into(code_pointer + 3, fpc);
                            pop_f1_into(code_pointer + 4, fpd);

                            code_pointer += 4;
                            for (int i = 0; i < ssmd_datacount; i++)
                            {
                                f4[i].x = fp4.Invoke((IntPtr)engine_ptr, environment_ptr, IntPtr.Zero, fpa[i], fpb[i], fpc[i], fpd[i]);
                            }
                            return;
                        }
                        break;

                    //
                    // For 5 6 and 7 parameters we can re-use the memory in f4 although it is sparse... 
                    // might be better to stackalloc some for more parameters 
                    // 


#if DEVELOPMENT_BUILD || TRACE
                    default:
                        Debug.LogError($"blast.ssmd.interpretor.call-external: function id {id}, with parametercount {p.MinParameterCount} is not supported yet");
                        break;
#endif
                }
#if DEVELOPMENT_BUILD || TRACE
            }
#endif

#if DEVELOPMENT_BUILD || TRACE 
            Debug.LogError($"blast.ssmd.interpretor.call-external: failed to call function pointer with id: {id}");
#endif
        }

        #endregion 

        #region GetFunction|Sequence and Execute (privates)

        /// <summary>
        /// get the result of a function encoded in the byte code, support all fuctions in op, exop and external calls 
        /// although external calls should be directly called when possible
        /// </summary>
        int GetFunctionResult([NoAlias]void* temp, ref int code_pointer, ref byte vector_size, [NoAlias]ref float4* f4_result, bool error_if_not_exists = true)
        {
            byte op = code[code_pointer];

            switch ((blast_operation)op)
            {
                // math functions
                case blast_operation.abs: get_abs_result(temp, ref code_pointer, ref vector_size, f4_result); break;

                case blast_operation.maxa: get_maxa_result(temp, ref code_pointer, ref vector_size, f4_result); break;
                case blast_operation.mina: get_mina_result(temp, ref code_pointer, ref vector_size, f4_result); break;

                case blast_operation.max: get_op_a_result(temp, ref code_pointer, ref vector_size, ref f4_result, blast_operation.max); break;
                case blast_operation.min: get_op_a_result(temp, ref code_pointer, ref vector_size, ref f4_result, blast_operation.min); break;
                case blast_operation.all: get_op_a_result(temp, ref code_pointer, ref vector_size, ref f4_result, blast_operation.and); break;
                case blast_operation.any: get_op_a_result(temp, ref code_pointer, ref vector_size, ref f4_result, blast_operation.or); break;

                case blast_operation.select: get_select_result(temp, ref code_pointer, ref vector_size, f4_result); break;

                // fma and friends 
                case blast_operation.fma: get_fma_result(temp, ref code_pointer, ref vector_size, ref f4_result); break;

                case blast_operation.mula: get_op_a_result(temp, ref code_pointer, ref vector_size, ref f4_result, blast_operation.multiply); break;
                case blast_operation.adda: get_op_a_result(temp, ref code_pointer, ref vector_size, ref f4_result, blast_operation.add); break;
                case blast_operation.suba: get_op_a_result(temp, ref code_pointer, ref vector_size, ref f4_result, blast_operation.substract); break;
                case blast_operation.diva: get_op_a_result(temp, ref code_pointer, ref vector_size, ref f4_result, blast_operation.diva); break;

                case blast_operation.ex_op:
                    {
                        code_pointer++;
                        extended_blast_operation exop = (extended_blast_operation)code[code_pointer];

                        switch (exop)
                        {
                            // external calls 
                            case extended_blast_operation.call: get_external_function_result(temp, ref code_pointer, ref vector_size, f4_result); break;

                            // single inputs
                            case extended_blast_operation.sqrt: get_sqrt_result(temp, ref code_pointer, ref vector_size, f4_result); break;
                            case extended_blast_operation.rsqrt: get_rsqrt_result(temp, ref code_pointer, ref vector_size, f4_result); break;
                            case extended_blast_operation.sin: get_sin_result(temp, ref code_pointer, ref vector_size, f4_result); break;
                            case extended_blast_operation.cos: get_cos_result(temp, ref code_pointer, ref vector_size, f4_result); break;
                            case extended_blast_operation.tan: get_tan_result(temp, ref code_pointer, ref vector_size, f4_result); break;
                            case extended_blast_operation.sinh: get_sinh_result(temp, ref code_pointer, ref vector_size, f4_result); break;
                            case extended_blast_operation.cosh: get_cosh_result(temp, ref code_pointer, ref vector_size, f4_result); break;
                            case extended_blast_operation.atan: get_atan_result(temp, ref code_pointer, ref vector_size, f4_result); break;
                            case extended_blast_operation.degrees: get_degrees_result(temp, ref code_pointer, ref vector_size, f4_result); break;
                            case extended_blast_operation.radians: get_radians_result(temp, ref code_pointer, ref vector_size, f4_result); break;
                            case extended_blast_operation.ceil: get_ceil_result(temp, ref code_pointer, ref vector_size, f4_result); break;
                            case extended_blast_operation.floor: get_floor_result(temp, ref code_pointer, ref vector_size, f4_result); break;
                            case extended_blast_operation.frac: get_frac_result(temp, ref code_pointer, ref vector_size, f4_result); break;
                            case extended_blast_operation.normalize: get_normalize_result(temp, ref code_pointer, ref vector_size, f4_result); break;
                            case extended_blast_operation.saturate: get_saturate_result(temp, ref code_pointer, ref vector_size, f4_result); break;
                            case extended_blast_operation.logn: get_log_result(temp, ref code_pointer, ref vector_size, f4_result); break;
                            case extended_blast_operation.log10: get_log10_result(temp, ref code_pointer, ref vector_size, f4_result); break;
                            case extended_blast_operation.log2: get_log2_result(temp, ref code_pointer, ref vector_size, f4_result); break;
                            case extended_blast_operation.exp10: get_exp10_result(temp, ref code_pointer, ref vector_size, f4_result); break;
                            case extended_blast_operation.exp: get_exp_result(temp, ref code_pointer, ref vector_size, f4_result); break;

                            // dual inputs 
                            case extended_blast_operation.pow: get_pow_result(temp, ref code_pointer, ref vector_size, f4_result); break;
                            case extended_blast_operation.cross: get_cross_result(temp, ref code_pointer, ref vector_size, f4_result); break;
                            case extended_blast_operation.dot: get_dot_result(temp, ref code_pointer, ref vector_size, f4_result); break;
                            case extended_blast_operation.atan2: get_atan2_result(temp, ref code_pointer, ref vector_size, f4_result); break;

                            // tripple inputs                 
                            case extended_blast_operation.clamp: get_clamp_result(temp, ref code_pointer, ref vector_size, f4_result); break;
                            case extended_blast_operation.lerp: get_lerp_result(temp, ref code_pointer, ref vector_size, f4_result); break;
                            //case extended_blast_operation.slerp: get_slerp_result(temp, ref code_pointer, ref vector_size, f4_result); break;
                            //case extended_blast_operation.nlerp: get_nlerp_result(temp, ref code_pointer, ref vector_size, f4_result); break;

                            case extended_blast_operation.debugstack:
                                {
                                    // write contents of stack to the debug stream as a detailed report; 
                                    // write contents of a data element to the debug stream 
                                    code_pointer++;
#if DEVELOPMENT_BUILD
                                 //  Handle_DebugStack();
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
                                   // void* pdata = pop_with_info(code_pointer, out datatype, out vector_size);

#if DEVELOPMENT_BUILD
                                 //   Handle_DebugData(code_pointer, vector_size, op_id, datatype, pdata);
#endif
                                    code_pointer++;
                                }
                                break;

                            default:
#if DEVELOPMENT_BUILD
                                if (error_if_not_exists)
                                {
                                    Debug.LogError($"SSMD Interpretor: GetFunctionResult: codepointer: {code_pointer} => {code[code_pointer]}, extended operation {exop} not handled");
                                }
#endif
                                return (int)BlastError.ssmd_function_not_handled;
                        }
                }
                break;


            default:
#if DEVELOPMENT_BUILD
                    if (error_if_not_exists)
                    {
                        Debug.LogError($"SSMD Interpretor: GetFunctionResult: codepointer: {code_pointer} => {code[code_pointer]}, operation {op} not handled");
                    }
#endif
                return (int)BlastError.ssmd_function_not_handled;
        }

        return (int)BlastError.success;
    }

    /// <summary>
    /// process a sequence of operations into a new register value 
    /// </summary>        
    int GetSequenceResult([NoAlias]void* temp, ref int code_pointer, ref byte vector_size)
    {
        byte op = 0, prev_op = 0;

        blast_operation current_op = blast_operation.nop;

        bool not = false;
        bool minus = false;

        float4* f4 = (float4*)temp;
        float4* f4_result = register; // here we accumelate the resulting value in 

        vector_size = 1; // vectors dont shrink in normal operations 
        byte current_vector_size = 1;


        while (code_pointer < package.CodeSize)
        {
            bool last_is_bool_or_math_operation = false;

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
                                if (BlastInterpretor.IsMathematicalOrBooleanOperation(next_op))
                                {
                                    // break out of switch before returning, there is more to this operation then known at the moment 
                                    break;
                                }
                                else
                                {
                                    // most probably a bug, error when the message is usefull
#if DEVELOPMENT_BUILD
                                    Debug.LogError($"SSMD Interpretor: Possible BUG: encountered non mathematical operation '{next_op}' after compound in statement at codepointer {code_pointer + 1}, expecting nop or assign");
#else
                                    Debug.LogWarning($"SSMD Interpretor: Possible BUG: encountered non mathematical operation '{next_op}' after compound in statement at codepointer {code_pointer + 1}, expecting nop or assign");
#endif
                                }
                            }
                        }
                        return (int)BlastError.success;
                    }

                case blast_operation.nop:
                    {
                        //
                        // assignments will end with nop and not with end as a compounded statement
                        // at this point vector_size is decisive for the returned value
                        //
                        code_pointer++;
                        return (int)BlastError.success;
                    }

                case blast_operation.ret:
                    // termination of interpretation
                    code_pointer = package.CodeSize + 1;
                    return (int)BlastError.success;

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
                    }
                    break;

                case blast_operation.not:
                    {
                        // if the first in a compound or previous was an operation
                        if (prev_op == 0 || last_is_bool_or_math_operation)
                        {
#if DEVELOPMENT_BUILD
                            if (not)
                            {
                                Debug.LogError("SSMD Interpretor: double token: ! !, this will be become nothing, it is not supported as it cant be determined to be intentional and will raise this error in editor mode only");
                            }
#endif
                            not = !not;
                        }
                        else
                        {
                            current_op = blast_operation.not;
                            last_is_bool_or_math_operation = true;
                        }
                    }
                    break;

                case blast_operation.substract:
                    {
                        // first or after an op 
                        if (prev_op == 0 || last_is_bool_or_math_operation)
                        {
#if DEVELOPMENT_BUILD
                            if (minus)
                            {
                                Debug.LogError("SSMD Interpretor: double token: - -, this will be become +, it is not supported as it cant be determined to be intentional and will raise this error in editor mode only");
                            }
#endif
                            minus = !minus;
                        }
                        else
                        {
                            current_op = blast_operation.substract;
                            last_is_bool_or_math_operation = true;
                        }
                    }
                    break;


                // in development builds assert on non supported operations
                // in release mode skip over crashing into the unknown 

#if DEVELOPMENT_BUILD || TRACE
                // jumps are not allowed in compounds 
                case blast_operation.jz:
                case blast_operation.jnz:
                case blast_operation.jump:
                case blast_operation.jump_back:
                    Assert.IsTrue(false, $"SSMD Interpretor: jump operation {(blast_operation)op} not allowed in compounds, codepointer = {code_pointer}");
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
                    Assert.IsTrue(false, $"SSMD Interpretor: operation {(blast_operation)op} not allowed in compounds, codepointer = {code_pointer}");
                    break;

                // any stack operation has no business in a compound other then pops
                case blast_operation.push:
                case blast_operation.pushv:
                case blast_operation.peek:
                case blast_operation.peekv:
                case blast_operation.pushf:
                case blast_operation.pushc:
                    Assert.IsTrue(false, $"SSMD Interpretor: stack operation {(blast_operation)op} not allowed in compounds, codepointer = {code_pointer}");
                    break;

                case blast_operation.begin:
                    Assert.IsTrue(false, "should not be nesting compounds... compiler did not do its job wel");
                    break;

#endif
#if DEVELOPMENT_BUILD || TRACE
#endif

                case blast_operation.pop:
                    {
                        pop_fn_into(code_pointer, f4, minus, out vector_size);
                        minus = false;

#if DEVELOPMENT_BUILD
                        if (vector_size > 4)
                        {
                            Debug.LogError($"BlastInterpretor.GetCompound: codepointer: {code_pointer} => pop vector too large, vectortype {vector_size} not supported");
                            return (int)BlastError.ssmd_error_unsupported_vector_size;
                        }
#endif
                    }
                    break;

                //
                // CoreAPI functions mapping to opcodes 
                // 
                case blast_operation.abs:
                case blast_operation.maxa:
                case blast_operation.mina:
                case blast_operation.max:
                case blast_operation.min:
                case blast_operation.select:
                case blast_operation.fma:
                case blast_operation.mula:
                case blast_operation.adda:
                case blast_operation.suba:
                case blast_operation.diva:
                case blast_operation.all:
                case blast_operation.any:
                case blast_operation.random:
                case blast_operation.ex_op: GetFunctionResult(temp, ref code_pointer, ref vector_size, ref f4_result); break;


#if DEVELOPMENT_BUILD
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
                        // set constant, if a minus is present apply it 
                        float constant = engine_ptr->constants[op];
                        constant = math.select(constant, -constant, minus);
                        minus = false;
                        switch ((BlastVectorSizes)vector_size)
                        {
                            case BlastVectorSizes.float1: for (int i = 0; i < ssmd_datacount; i++) f4[i].x = constant; break;
                            case BlastVectorSizes.float2: for (int i = 0; i < ssmd_datacount; i++) f4[i].xy = constant; break;
                            case BlastVectorSizes.float3: for (int i = 0; i < ssmd_datacount; i++) f4[i].xyz = constant; break;
                            case BlastVectorSizes.float4: for (int i = 0; i < ssmd_datacount; i++) f4[i].xyzw = constant; break;
#if DEVELOPMENT_BUILD
                            default:
                                Debug.LogError($"SSMD Interpretor, set constantvalue, codepointer: {code_pointer} => {code[code_pointer]}, error: vectorsize {vector_size} not supported");
                                return (int)BlastError.ssmd_error_unsupported_vector_size;
#endif
                        }
                    }
                    break;


                // handle identifiers/variables
                default:
                    {
                        if (op >= BlastInterpretor.opt_id) // only exop is higher and that is handled earlier so this check is safe 
                        {
                            // lookup identifier value
                            byte id = (byte)(op - BlastInterpretor.opt_id);
                            byte this_vector_size = BlastInterpretor.GetMetaDataSize(metadata, id);

                            // and put it in f4 


                            switch ((BlastVectorSizes)this_vector_size)
                            {
                                case BlastVectorSizes.float1: set_from_data_f1(f4, data, id); vector_size = 1; break;
                                case BlastVectorSizes.float2: set_from_data_f2(f4, data, id); vector_size = 2; break;
                                case BlastVectorSizes.float3: set_from_data_f3(f4, data, id); vector_size = 3; break;
                                case 0:
                                case BlastVectorSizes.float4: set_from_data_f4(f4, data, id); vector_size = 4; break;
                            }
                        }
                        else
                        {
                            // we receive an operation while expecting a value 
                            //
                            // if we predict this at the compiler we could use the opcodes value as value constant
                            //
#if DEVELOPMENT_BUILD
                            Debug.LogError($"SSMD Interpretor, codepointer: {code_pointer} => {code[code_pointer]}, encountered unknown operation {(blast_operation)op}");
#endif
                            return (int)BlastError.ssmd_invalid_operation;
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
            if (current_op == 0 && !(minus && prev_op == 0))
            {
                // just set according to vector size
                if (minus)
                {
                    switch ((BlastVectorSizes)vector_size)
                    {
                        case BlastVectorSizes.float1: for (int i = 0; i < ssmd_datacount; i++) f4_result[i].x = -f4[i].x; current_vector_size = 1; break;
                        case BlastVectorSizes.float2: for (int i = 0; i < ssmd_datacount; i++) f4_result[i].xy = -f4[i].xy; current_vector_size = 2; break;
                        case BlastVectorSizes.float3: for (int i = 0; i < ssmd_datacount; i++) f4_result[i].xyz = -f4[i].xyz; current_vector_size = 3; break;
                        case 0:
                        case BlastVectorSizes.float4: for (int i = 0; i < ssmd_datacount; i++) f4_result[i] = -f4[i]; current_vector_size = 4; break;
                    }
                    minus = false;
                }
                else
                {
                    switch ((BlastVectorSizes)vector_size)
                    {
                        case BlastVectorSizes.float1: for (int i = 0; i < ssmd_datacount; i++) f4_result[i].x = f4[i].x; current_vector_size = 1; break;
                        case BlastVectorSizes.float2: for (int i = 0; i < ssmd_datacount; i++) f4_result[i].xy = f4[i].xy; current_vector_size = 2; break;
                        case BlastVectorSizes.float3: for (int i = 0; i < ssmd_datacount; i++) f4_result[i].xyz = f4[i].xyz; current_vector_size = 3; break;
                        case 0:
                        case BlastVectorSizes.float4: for (int i = 0; i < ssmd_datacount; i++) f4_result[i] = f4[i]; current_vector_size = 4; break;
                    }
                }
            }
            else
            {
                if (!last_is_bool_or_math_operation)
                {
                    switch ((BlastVectorSizes)current_vector_size)
                    {
                        case BlastVectorSizes.float1:

                            switch ((BlastVectorSizes)vector_size)
                            {
                                case BlastVectorSizes.float1:
                                    handle_op_f1_f1(current_op, f4_result, f4, ref vector_size);
                                    break;
                                case BlastVectorSizes.float2:
                                    handle_op_f1_f2(current_op, f4_result, f4, ref vector_size);
                                    break;
                                case BlastVectorSizes.float3:
                                    handle_op_f1_f3(current_op, f4_result, f4, ref vector_size);
                                    break;
                                case BlastVectorSizes.float4:
                                    handle_op_f1_f4(current_op, f4_result, f4, ref vector_size);
                                    break;
#if DEVELOPMENT_BUILD
                                default:
                                    Debug.LogError($"SSMD Interpretor: execute-sequence: codepointer: {code_pointer} => {code[code_pointer]}, no support for vector of size {vector_size} -> {current_vector_size}");
                                    break;
#endif
                            }
                            break;


                        case BlastVectorSizes.float2:

                            switch ((BlastVectorSizes)vector_size)
                            {
                                case BlastVectorSizes.float1: handle_op_f2_f1(current_op, f4_result, f4, ref vector_size); break;
                                case BlastVectorSizes.float2: handle_op_f2_f2(current_op, f4_result, f4, ref vector_size); break;
#if DEVELOPMENT_BUILD
                                default:
                                    Debug.LogError($"SSMD Interpretor: execute-sequence: codepointer: {code_pointer} => {code[code_pointer]}, vector operation undefined: {vector_size} -> {current_vector_size}");
                                    break;
#endif
                            }
                            break;

                        case BlastVectorSizes.float3:

                            switch ((BlastVectorSizes)vector_size)
                            {
                                case BlastVectorSizes.float1: handle_op_f3_f1(current_op, f4_result, f4, ref vector_size); break;
                                case BlastVectorSizes.float3: handle_op_f3_f3(current_op, f4_result, f4, ref vector_size); break;
#if DEVELOPMENT_BUILD
                                default:
                                    Debug.LogError($"SSMD Interpretor: execute-sequence: codepointer: {code_pointer} => {code[code_pointer]}, vector operation undefined: {vector_size} -> {current_vector_size}");
                                    break;
#endif
                            }
                            break;


                        case 0:
                        case BlastVectorSizes.float4:

                            switch ((BlastVectorSizes)vector_size)
                            {
                                case BlastVectorSizes.float1: handle_op_f4_f1(current_op, f4_result, f4, ref vector_size); break;
                                case BlastVectorSizes.float4: handle_op_f4_f4(current_op, f4_result, f4, ref vector_size); break;
#if DEVELOPMENT_BUILD
                                default:
                                    Debug.LogError($"SSMD Interpretor: execute-sequence: codepointer: {code_pointer} => {code[code_pointer]}, vector operation undefined: {vector_size} -> {current_vector_size}");
                                    break;
#endif
                            }
                            break;


#if DEVELOPMENT_BUILD
                        default:
                            Debug.LogError($"SSMD Interpretor: execute-sequence: codepointer: {code_pointer} => {code[code_pointer]}, no support for vector of size {vector_size} -> {current_vector_size}");
                            break;
#endif


                    }
                }
            }

            // apply minus (if in sequence and not already applied)...  
            if (minus && !last_is_bool_or_math_operation)
            {
                switch ((BlastVectorSizes)vector_size)
                {
                    case BlastVectorSizes.float1: for (int i = 0; i < ssmd_datacount; i++) f4_result[i].x = -f4_result[i].x; break;
                    case BlastVectorSizes.float2: for (int i = 0; i < ssmd_datacount; i++) f4_result[i].xy = -f4_result[i].xy; break;
                    case BlastVectorSizes.float3: for (int i = 0; i < ssmd_datacount; i++) f4_result[i].xyz = -f4_result[i].xyz; break;
                    case 0:
                    case BlastVectorSizes.float4: for (int i = 0; i < ssmd_datacount; i++) f4_result[i] = -f4_result[i]; break;
                }
                minus = false;
            }

            // reset current operation if last thing was a value 
            if (!last_is_bool_or_math_operation) current_op = blast_operation.nop;

            // next
            prev_op = op;
            code_pointer++;
        }

        // return OK 
        return (int)BlastError.success;
    }




    #region Stack Operation Handlers 

    BlastError push(ref int code_pointer, ref byte vector_size, [NoAlias]void* temp)
    {
        if (code[code_pointer] == (byte)blast_operation.begin)
        {
            code_pointer++;

            // push the vector result of a compound 
            BlastError res = (BlastError)GetSequenceResult(temp, ref code_pointer, ref vector_size);
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
        BlastError res = (BlastError)GetSequenceResult(temp, ref code_pointer, ref vector_size);
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
        BlastError res = (BlastError)GetFunctionResult(temp, ref code_pointer, ref vector_size, ref register);
        if (res < 0)
        {
#if DEVELOPMENT_BUILD
            Debug.LogError($"blast.ssmd.execute.pushf: failed to read function result data for pushf operation at codepointer {code_pointer}");
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
        BlastError res = (BlastError)GetSequenceResult(temp, ref code_pointer, ref vector_size);
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

    BlastError assignf(ref int code_pointer, ref byte vector_size, [NoAlias]void* temp, bool minus)
    {
        // get assignee 
        byte assignee_op = code[code_pointer];
        byte assignee = (byte)(assignee_op - BlastInterpretor.opt_id);

#if DEVELOPMENT_BUILD || TRACE
        // check its size?? in debug only, functions return size in vector_size 
        byte s_assignee = BlastInterpretor.GetMetaDataSize(in metadata, in assignee);
#endif

        code_pointer++;
        int res = GetFunctionResult(temp, ref code_pointer, ref vector_size, ref register);
        code_pointer++;

#if DEVELOPMENT_BUILD || TRACE
        // 4 == 0 depending on decoding 
        s_assignee = (byte)math.select(s_assignee, 4, s_assignee == 0);
        if (s_assignee != vector_size)
        {
            Debug.LogError($"SSMD Interpretor: assign function, vector size mismatch at #{code_pointer}, should be size '{s_assignee}', function returned '{vector_size}', data id = {assignee}");
            return BlastError.error_assign_vector_size_mismatch;
        }
#endif
        set_data_from_register(vector_size, assignee, minus);
        return BlastError.success;
    }

    BlastError assignfe(ref int code_pointer, ref byte vector_size, [NoAlias]void* temp, bool minus)
    {
        // get assignee 
        byte assignee_op = code[code_pointer];
        byte assignee = (byte)(assignee_op - BlastInterpretor.opt_id);

#if DEVELOPMENT_BUILD
        // check its size?? in debug only, functions return size in vector_size 
        byte s_assignee = BlastInterpretor.GetMetaDataSize(in metadata, in assignee);
#endif
        code_pointer++;

        Debug.LogError("exteranl  todo");
        // CallExternalFunction(temp, ref code_pointer, ref vector_size, ref register);
        code_pointer++;

#if DEVELOPMENT_BUILD
        // 4 == 0 depending on decoding 
        s_assignee = (byte)math.select(s_assignee, 4, s_assignee == 0);
        if (s_assignee != vector_size)
        {
            Debug.LogError($"SSMD Interpretor: assign external function, vector size mismatch at #{code_pointer}, should be size '{s_assignee}', function returned '{vector_size}', data id = {assignee}");
            return BlastError.error_assign_vector_size_mismatch;
        }
#endif


        set_data_from_register(vector_size, assignee, minus);
        return BlastError.success;
    }

    BlastError assignv(ref int code_pointer, ref byte vector_size, [NoAlias]void* temp)
    {
        byte assignee_op = code[code_pointer];
        byte assignee = (byte)(assignee_op - BlastInterpretor.opt_id);
        vector_size = BlastInterpretor.GetMetaDataSize(in metadata, in assignee);

        code_pointer++;

        int cp = 0;
        switch (vector_size)
        {
            case 1:
                pop_f1_into_with_minus(code_pointer, assignee, ref cp);
                // assignee[0] = pop_f1_with_minus(code_pointer, ref cp);
                code_pointer += 1 + cp;
                break;
            case 2:
                pop_f2_into_with_minus(code_pointer, assignee, ref cp);
                code_pointer += 2 + cp;
                break;
            case 3:
                pop_f3_into_with_minus(code_pointer, assignee, ref cp);
                code_pointer += 3 + cp;
                break;
            case 0:
            case 4:
                pop_f4_into_with_minus(code_pointer, assignee, ref cp);
                code_pointer += 4 + cp;
                break;

            default:
#if DEVELOPMENT_BUILD
                Debug.LogError($"Blast SSMD Interpretor: assignv error: vector size {vector_size} not supported");
#endif
                break;
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
    int Execute([NoAlias]byte** datastack, int ssmd_data_count, [NoAlias]float4* f4_register)
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

            // generate a list of stacksegment pointers on the stack:
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

        //*************************************************************************************************************************
        // start the main interpretor loop 
        //*************************************************************************************************************************
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

                case blast_operation.assignf:
                    if ((ires = (int)assignf(ref code_pointer, ref vector_size, temp, false)) < 0) return ires;
                    break;

                case blast_operation.assignfe:
                    if ((ires = (int)assignfe(ref code_pointer, ref vector_size, temp, false)) < 0) return ires;
                    break;

                case blast_operation.assignfn:
                    if ((ires = (int)assignf(ref code_pointer, ref vector_size, temp, true)) < 0) return ires;
                    break;

                case blast_operation.assignfen:
                    if ((ires = (int)assignfe(ref code_pointer, ref vector_size, temp, true)) < 0) return ires;
                    break;

                case blast_operation.assignv:
                    if ((ires = (int)assignv(ref code_pointer, ref vector_size, temp)) < 0) return ires;
                    break;

                // assign result of a compound to some dataindex in the datasegment 
                case blast_operation.assign:
                    if ((ires = (int)assign(ref code_pointer, ref vector_size, temp)) < 0) return ires;
                    break;

                default:
                        {
                            if (GetFunctionResult(temp, ref code_pointer, ref vector_size, ref f4_register, false) == (int)BlastError.ssmd_function_not_handled)
                            {
                                // if not a function in root, then its nothing 

#if DEVELOPMENT_BUILD
                                Debug.LogError($"blast.ssmd: operation {(byte)op} '{op}' not supported in root codepointer = {code_pointer}");
#endif
                                return (int)BlastError.error_unsupported_operation_in_root;
                            }

                            break;
                        }
                }
            }

            return (int)BlastError.success;
        }

        #endregion
    }
}
