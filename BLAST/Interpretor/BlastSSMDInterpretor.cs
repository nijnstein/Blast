﻿using NSS.Blast.Interpretor;
using NSS.Blast.Standalone;
using System;
using System.Collections.Generic;
using System.Text;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

namespace NSS.Blast.SSMD
{
    [BurstCompatible]
    unsafe public struct BlastSSMDInterpretor
    {
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
        internal unsafe float4* register;

        /// <summary>
        /// metadata segment pointer
        /// </summary>
        [NoAlias]
        [NativeDisableUnsafePtrRestriction]
        internal unsafe byte* metadata;


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

            code_pointer = 0;
            stack_offset = package.DataSegmentStackOffset / 4;

            return BlastError.success;
        }

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
                ((float*)data[i])[stack_offset] = register[i].x;
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
                float* p = ((float*)data[i]); 
                p[stack_offset] = p[i];
            }

            // advance 1 element in stack offset 
            stack_offset += 1;
        }

        /// <summary>
        /// push register[n].xy as float2
        /// </summary>
        void push_register_f2()
        {
            BlastInterpretor.SetMetaData(metadata, BlastVariableDataType.Numeric, 2, (byte)stack_offset);
            for (int i = 0; i < ssmd_datacount; i++)
            {
                ((float2*)(void*)&((float*)data[i])[stack_offset])[0] = register[i].xy;
            }
            stack_offset += 2;
        }


        /// <summary>
        /// push register[n].xyz as float3
        /// </summary>
        void push_register_f3()
        {
            BlastInterpretor.SetMetaData(metadata, BlastVariableDataType.Numeric, 3, (byte)stack_offset);
            for (int i = 0; i < ssmd_datacount; i++)
            {
                ((float3*)(void*)&((float*)data[i])[stack_offset])[0] = register[i].xyz;
            }
            stack_offset += 3;  // size 3
        }

        /// <summary>
        /// push register[n] as float4
        /// </summary>
        void push_register_f4()
        {
            BlastInterpretor.SetMetaData(metadata, BlastVariableDataType.Numeric, 4, (byte)stack_offset);
            for (int i = 0; i < ssmd_datacount; i++)
            {
                ((float4*)(void*)&((float*)data)[stack_offset])[0] = register[i];
            }
            stack_offset += 4;  // size 4
        }

        /// <summary>
        /// push an float1 value on te stack retrieved via 1 pop 
        /// </summary>
        void push_f1_pop_1(in int code_pointer)
        {
            push_pop_f(code_pointer, 1);
        }

        /// <summary>
        /// push an float2 value on te stack retrieved via 2x 1 pop 
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
                float2* f2p = &((float2*)(void*)&((float*)data[i])[stack_offset])[0];
                f2p->x = f1[i];
                f2p->y = f2[i];
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
                float3* f3p = &((float3*)(void*)&((float*)data[i])[stack_offset])[0];
                f3p->x = f1[i];
                f3p->y = f2[i];
                f3p->z = f3[i];
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
                float4* f4p = &((float4*)(void*)&((float*)data[i])[stack_offset])[0];
                f4p->x = f1[i];
                f4p->y = f2[i];
                f4p->z = f3[i];
                f4p->w = f4[i];
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

#if DEBUG 
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
                destination[i] = ((float*)data[i])[stack_offset];
            }
        }


        /// <summary>
        /// pop a float1 value form stack data or constant source and put it in destination 
        /// </summary>
        void pop_f1_into(in int code_pointer, [NoAlias]float* destination)
        {
            // we get either a pop instruction or an instruction to get a value 
            byte c = code[code_pointer];

            if (c >= BlastInterpretor.opt_id)
            {
#if DEBUG 
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
#if DEBUG 
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
                    destination[i] = ((float*)data[i])[stack_offset];
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
#if DEBUG
                Debug.LogError("pop_f1_into -> select op by constant value is not supported");
#endif
            }
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

        /// <summary>
        /// read codebyte, determine data location, pop data and push it on the stack
        /// </summary>
        void push_pop_f(in int code_pointer, in int vector_size)
        {
            // we get either a pop instruction or an instruction to get a value 
            byte c = code[code_pointer];

            if (c >= BlastInterpretor.opt_id)
            {
#if DEBUG
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
                            ((float4*)data[i])[stack_offset] = ((float4*)data[i])[c - BlastInterpretor.opt_id];
                        }
                        stack_offset += 4;
                        break;
                    case 1:
                        BlastInterpretor.SetMetaData(metadata, BlastVariableDataType.Numeric, 1, (byte)stack_offset);
                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            ((float*)data[i])[stack_offset] = ((float*)data[i])[c - BlastInterpretor.opt_id];
                        }
                        stack_offset += 1;
                        break;
                    case 2:
                        BlastInterpretor.SetMetaData(metadata, BlastVariableDataType.Numeric, 2, (byte)stack_offset);
                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            ((float2*)data[i])[stack_offset] = ((float2*)data[i])[c - BlastInterpretor.opt_id];
                        }
                        stack_offset += 2;
                        break;

                    case 3:
                        BlastInterpretor.SetMetaData(metadata, BlastVariableDataType.Numeric, 3, (byte)stack_offset);
                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            ((float3*)data[i])[stack_offset] = ((float3*)data[i])[c - BlastInterpretor.opt_id];
                        }
                        stack_offset += 3;
                        break;
                }
            }
            else
            if (c == (byte)blast_operation.pop)
            {
#if DEBUG
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
                            ((float4*)data[i])[stack_offset] = constant;
                        }
                        stack_offset += 4;
                        break;
                    case 1:
                        BlastInterpretor.SetMetaData(metadata, BlastVariableDataType.Numeric, 1, (byte)stack_offset);
                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            ((float*)data[i])[stack_offset] = constant;
                        }
                        stack_offset += 1;
                        break;
                    case 2:
                        BlastInterpretor.SetMetaData(metadata, BlastVariableDataType.Numeric, 2, (byte)stack_offset);
                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            ((float2*)data[i])[stack_offset] = constant;
                        }
                        stack_offset += 2;
                        break;

                    case 3:
                        BlastInterpretor.SetMetaData(metadata, BlastVariableDataType.Numeric, 3, (byte)stack_offset);
                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            ((float3*)data[i])[stack_offset] = constant;
                        }
                        stack_offset += 3;
                        break;
                }
            }
            else
            {
#if DEBUG
                Debug.LogError("push_pop_f -> select op by constant value is not supported");
#endif
            }
        }

        /// <summary>
        /// set register[n].x from data 
        /// </summary>
        void set_register_from_data_f1(float* f1)
        {
            for(int i = 0; i < ssmd_datacount; i++)
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

       
        void handle_op(in blast_operation op, in byte current, in byte previous, ref float4 a, in float4 b, ref bool last_is_op_or_first, ref byte vector_size)
        {
            last_is_op_or_first = false;
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

                case blast_operation.not:
#if DEBUG 
                    Debug.LogError("not");
#endif
                    return;
            }
        }

        void handle_op(in blast_operation op, in byte current, in byte previous, ref float4 a, in float3 b, ref bool last_is_op_or_first, ref byte vector_size)
        {
            last_is_op_or_first = false;
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

                case blast_operation.and: a.xyz = math.select(0f, 1f, math.all(a.xyz) && math.all(b)); return;
                case blast_operation.or: a.xyz = math.select(0f, 1f, math.any(a.xyz) || math.any(b)); return;
                case blast_operation.xor: a.xyz = math.select(0f, 1f, math.any(a.xyz) ^ math.any(b)); return;

                case blast_operation.greater: a.xyz = math.select(0f, 1f, a.xyz > b); return;
                case blast_operation.smaller: a.xyz = math.select(0f, 1f, a.xyz < b); return;
                case blast_operation.smaller_equals: a.xyz = math.select(0f, 1f, a.xyz <= b); return;
                case blast_operation.greater_equals: a.xyz = math.select(0f, 1f, a.xyz >= b); return;
                case blast_operation.equals: a.xyz = math.select(0f, 1f, a.xyz == b); return;

                case blast_operation.not:
#if DEBUG
                    Debug.LogError("not");
#endif
                    return;
            }
        }

        void handle_op(in blast_operation op, in byte current, in byte previous, ref float4 a, in float2 b, ref bool last_is_op_or_first, ref byte vector_size)
        {
            last_is_op_or_first = false;
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

                case blast_operation.and: a.xy = math.select(0, 1, math.all(a.xy) && math.all(b)); return;
                case blast_operation.or: a.xy = math.select(0, 1, math.any(a.xy) || math.any(b)); return;
                case blast_operation.xor: a.xy = math.select(0, 1, math.any(a.xy) ^ math.any(b)); return;

                case blast_operation.greater: a.xy = math.select(0f, 1f, a.xy > b); return;
                case blast_operation.smaller: a.xy = math.select(0f, 1f, a.xy < b); return;
                case blast_operation.smaller_equals: a.xy = math.select(0f, 1f, a.xy <= b); return;
                case blast_operation.greater_equals: a.xy = math.select(0f, 1f, a.xy >= b); return;
                case blast_operation.equals: a.xy = math.select(0f, 1f, a.xy == b); return;

                case blast_operation.not:
#if DEBUG 
                    Debug.LogError("not");
#endif
                    return;
            }
        }

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
#if DEBUG 
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
#if DEBUG 
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
#if DEBUG 
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
#if DEBUG 
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
#if DEBUG 
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
#if DEBUG 
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
#if DEBUG 
                    Debug.LogError("not");
#endif
                    return;
            }
        }

        #endregion



        /// <summary>
        /// process a compound, on function exit write back data to register 
        /// </summary>
        int GetCompoundResult(ref int code_pointer, ref byte vector_size)
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
            float* temp = stackalloc float[ssmd_datacount];

            float4* f4 = stackalloc float4[ssmd_datacount];
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
                                for(int i = 0; i < ssmd_datacount; i++) f4[i].x = f1[i];
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

                // depending on next operation this might not be needed ?!!!  TODO 
                // 
                // many more operations overwrite it.. 
                //
                //  NOT SURE BUT THIS SHOULD MOVE AWAY
                // 
                if ((blast_operation)op == blast_operation.end || op == 0)
                {
                    // only fi returning... 
                    UnsafeUtils.MemCpy(f4_result, f4, ssmd_datacount * sizeof(float4));

                    //- used in handle op,, we could void the memcpy when testing done
                }

                //
                //
                //
                 

                // switch on an operation to take
                switch ((blast_operation)op)
                {
                    case blast_operation.end:
                        end_of_compound = true; 
                        break;

                    case blast_operation.nop:
                        // assignments will end with nop and not with end as a compounded statement
                        // at this point vector_size is decisive for the returned value
                        code_pointer++;
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
#if DEBUG
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
#if DEBUG 
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
#if DEBUG
                                    Debug.LogWarning("should not be nesting compounds... compiler did not do its job wel");
#endif

                                    // copy register? side effects!!!!!!!!!!!!!
                                    /// f4_result = get_compound_result(ref code_pointer, ref vector_size);
                                    break;

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

                                // fma and friends 
                                case blast_operation.fma: get_fma_result(ref code_pointer, ref vector_size, out f4_result); break;

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
                                    BlastVariableDataType popped_type;
                                    byte popped_vector_size;

                                     make a peek then use pop_fn_into() 

                                    void* pdata = pop_with_info(code_pointer, out popped_type, out popped_vector_size);
                                    switch (popped_vector_size)
                                    {
                                        case 0:
                                        case 4: f4_result = ((float4*)pdata)[0]; vector_size = 4; f4_result = math.select(f4_result, -f4_result, minus); break;
                                        case 1: f4_result.x = ((float*)pdata)[0]; vector_size = 1; f4_result.x = math.select(f4_result.x, -f4_result.x, minus); break;
                                        case 2: f4_result.xy = ((float2*)pdata)[0]; vector_size = 2; f4_result.xy = math.select(f4_result.xy, -f4_result.xy, minus); break;
                                        case 3: f4_result.xyz = ((float3*)pdata)[0]; vector_size = 3; f4_result.xyz = math.select(f4_result.xyz, -f4_result.xyz, minus); break;
                                        default:
#if DEBUG
                                            Debug.LogError($"codepointer: {code_pointer} => pop vector too large, vectortype {vector_size} not supported");
#endif
                                            return -1;
                                    }

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
#if DEBUG
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
                                                case BlastVectorSizes.float1: for (int i = 0; i < ssmd_datacount; i++) f4_result[i].x = constant; break;
                                                case BlastVectorSizes.float2: for (int i = 0; i < ssmd_datacount; i++) f4_result[i].xy = constant; break;
                                                case BlastVectorSizes.float3: for (int i = 0; i < ssmd_datacount; i++) f4_result[i].xyz = constant; break;
                                                case BlastVectorSizes.float4: for (int i = 0; i < ssmd_datacount; i++) f4_result[i].xyzw = constant; break;
                                                default:
#if DEBUG
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
#if DEBUG
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
                                                            for(int i = 0; i < ssmd_datacount; i++)
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
#if DEBUG
                                            Debug.LogError($"codepointer: {code_pointer} => {code[code_pointer]}, encountered unknown op {(blast_operation)op}");
#endif

                                            // this should screw stuff up 
                                            return (int)BlastError.ssmd_error_expecting_value;
                                        }
                                    }
                                    break;
                            }
                            {

                                // after an operation with growvector we should stack value

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
#if DEBUG
                                                    // these will be in log because of growing a vector from a compound of unknown sizes  
                                                    Debug.LogWarning($"execute.ssmd: codepointer: {code_pointer} => {code[code_pointer]}, no support for vector of size {vector_size} -> {max_vector_size}");
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
#if DEBUG
                                                    // these will be in log because of growing a vector from a compound of unknown sizes  
                                                    Debug.LogWarning($"execute.ssmd: codepointer: {code_pointer} => {code[code_pointer]}, no support for vector of size {vector_size} -> {max_vector_size}");
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
                                                    f4 = new float4(f1, f1, f1, f1);
                                                    break;
                                                case BlastVectorSizes.float2:
                                                    f4 = new float4(f4.xy, 0, 0);
                                                    break;
                                                case BlastVectorSizes.float3:
                                                    f4 = new float4(f4.xyz, 0);
                                                    // usually we are growing from float4 to a 4 vector, current op would be nop then 
                                                    break;
                                                case BlastVectorSizes.float4:
                                                    // have a float 4 and one from earlier.
                                                    break;
                                                default:
#if DEBUG
                                                    Debug.LogWarning($"execute.ssmd: codepointer: {code_pointer} => {code[code_pointer]}, no support for vector of size {vector_size} -> {max_vector_size}");
#endif
                                                    break;
                                            }
                                            handle_op(current_operation, op, prev_op, ref f4, f4_result, ref last_is_op_or_first, ref vector_size);
                                        }
                                        break;

                                    default:
                                        {
#if DEBUG
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





        public int Execute([NoAlias]BlastEngineData* blast, [NoAlias]IntPtr environment, [NoAlias]BlastMetaDataStack* ssmddata, int ssmd_data_count)
        {
            // set shared environment 
            engine_ptr = blast;
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

        int Execute([NoAlias]byte** datastack, int ssmd_data_count, float4* f4_register)
        {
            data = (void**)datastack;
            ssmd_datacount = ssmd_data_count;
            register = f4_register; 

            // assume all data is set 
            int iterations = 0;

            // main interpretor loop 
            while (code_pointer < package.CodeSize)
            {
                iterations++;
                if (iterations > BlastInterpretor.max_iterations) return (int)BlastError.error_max_iterations;

                byte vector_size = 1;
                blast_operation op = (blast_operation)code[code_pointer];
                code_pointer++;

                switch (op)
                {
                    case blast_operation.ret:
                        {
                            return 0;
                        }

                    case blast_operation.nop:
                        break;

                    case blast_operation.push:

                        if (code[code_pointer] == (byte)blast_operation.begin)
                        {
                            code_pointer++;

                            // push the vector result of a compound 
                            GetCompoundResult(ref code_pointer, ref vector_size);
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
#if DEBUG
                                    Debug.LogError("burstscript.interpretor error: variable vector size not yet supported on stack push of compound");
#endif
                                    return (int)BlastError.error_variable_vector_compound_not_supported;
                            }
                            // code_pointer should index after compound now
                        }
                        else
                        {
                            // else push 1 float of data
                            push_data_f1(code[code_pointer++] - BlastInterpretor.opt_id); 
                        }

                        break;

                    //
                    // pushv knows more about what it is pushing up the stack, it can do it without running the compound 
                    //
                    case blast_operation.pushv:
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
#if DEBUG
                                    Debug.LogError($"blast.ssmd.interpretor error: vectorsize {vector_size} not yet supported on stack push");
#endif
                                    return (int)BlastError.error_vector_size_not_supported;
                            }
                        }
                        else
                        {
#if DEBUG
                            if (param_count != vector_size)
                            {
                                Debug.LogError($"blast.ssmd.interpretor error: pushv => param_count > 1 && vector_size != param_count, this is not allowed. Vectorsize: {vector_size}, Paramcount: {param_count}");
                                return (int)BlastError.error_unsupported_operation;
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
                        break;

                    //
                    // push the result from a function directly onto the stack, saving a call to getcompound 
                    //
                    case blast_operation.pushf:

                        GetCompoundResult(ref code_pointer, ref vector_size);
                        switch (vector_size)
                        {
                            case 1: push_register_f1(); break;
                            case 2: push_register_f2(); break;
                            case 3: push_register_f3(); break;
                            case 4: push_register_f4(); break;
                            default:
#if DEBUG
                                Debug.LogError("blast.ssmd.interpretor error: variable vector size not yet supported on stack push");
#endif
                                return (int)BlastError.error_variable_vector_op_not_supported;
                        }
                        break;

                    case blast_operation.begin:
                        // jumping in from somewhere
                        break;

                    case blast_operation.end: break;

                    /// if the compiler can detect:
                    /// - simple assign (1 function call or 1 parameter set)
                    /// then use an extra op:  assign1 that doesnt go into get_compound
                    case blast_operation.assign:
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


                            GetCompoundResult(ref code_pointer, ref vector_size);

                            // set assigned data fields in stack 
                            switch ((byte)vector_size)
                            {
                                case (byte)BlastVectorSizes.float1:
                                    //if(Unity.Burst.CompilerServices.Hint.Unlikely(s_assignee != 1))
                                    if (s_assignee != 1)
                                    {
#if DEBUG
                                        Debug.LogError($"blast: assigned vector size mismatch at #{code_pointer}, should be size '{s_assignee}', evaluated '1', data id = {assignee}");
#endif
                                        return (int)BlastError.error_assign_vector_size_mismatch;
                                    }
                                    set_data_from_register_f1(assignee); 
                                    break;

                                case (byte)BlastVectorSizes.float2:
                                    if (s_assignee != 2)
                                    {
#if DEBUG
                                        Debug.LogError($"blast: assigned vector size mismatch at #{code_pointer}, should be size '{s_assignee}', evaluated '2'");
#endif
                                        return (int)BlastError.error_assign_vector_size_mismatch;
                                    }
                                    set_data_from_register_f2(assignee); 
                                    break;

                                case (byte)BlastVectorSizes.float3:
                                    if (s_assignee != 3)
                                    {
#if DEBUG
                                        Debug.LogError($"blast: assigned vector size mismatch at #{code_pointer}, should be size '{s_assignee}', evaluated '3'");
#endif
                                        return (int)BlastError.error_assign_vector_size_mismatch;
                                    }
                                    set_data_from_register_f3(assignee);
                                    break;

                                case (byte)BlastVectorSizes.float4:
                                    if (s_assignee != 4)
                                    {
#if DEBUG
                                        Debug.LogError($"blast: assigned vector size mismatch at #{code_pointer}, should be size '{s_assignee}', evaluated '4'");
#endif
                                        return (int)BlastError.error_assign_vector_size_mismatch;
                                    }
                                    set_data_from_register_f4(assignee);
                                    break;

                                default:
#if DEBUG
                                    Debug.LogError($"blast.interpretor: vector size {vector_size} not allowed at codepointer {code_pointer}");
#endif
                                    return (int)BlastError.error_unsupported_operation_in_root;
                            }
                        }
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
#if DEBUG
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

#if DEBUG
                                        Handle_DebugData(code_pointer, vector_size, op_id, datatype, pdata);
#endif
                                        code_pointer++;
                                    }
                                    break;
                                */ 
                                default:
                                    {
#if DEBUG
                                        Debug.LogError($"blast.ssmd.extended op: only call and debug operation is supported from root, not {exop} ");
#endif
                                        return (int)BlastError.error_unsupported_operation_in_ssmd_root;
                                    }
                            }
                            break;
                        }

                    default:
                        {
#if DEBUG
                            Debug.LogError($"blast.ssmd: operation {(byte)op} '{op}' not supported in root codepointer = {code_pointer}");
#endif
                            return (int)BlastError.error_unsupported_operation_in_root;
                        }
                }
            }

            return (int)BlastError.success;
        }
    }
}
