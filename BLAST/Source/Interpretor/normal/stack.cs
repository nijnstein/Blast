//############################################################################################################################
// BLAST v1.0.4c                                                                                                             #
// Copyright © 2022 Rob Lemmens | NijnStein Software <rob.lemmens.s31 gmail com> All Rights Reserved                   ^__^\ #
// Unauthorized copying of this file, via any medium is strictly prohibited proprietary and confidential               (oo)\ #
//                                                                                                                     (__)  #
//############################################################################################################################
#pragma warning disable CS1591
#if STANDALONE_VSBUILD
using NSS.Blast.Standalone;
    using System.Reflection;
#else
    using UnityEngine;
    using Unity.Burst.CompilerServices;
#endif


using Unity.Burst;
using Unity.Mathematics;
using System.Runtime.CompilerServices;

using V1 = System.Single;
using V2 = Unity.Mathematics.float2;
using V3 = Unity.Mathematics.float3;
using V4 = Unity.Mathematics.float4;

namespace NSS.Blast.Interpretor
{
    unsafe public partial struct BlastInterpretor
    {

        #region pop_f[1|2|3|4] (all copies of f1 with minor changes)

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        V1 pop_f1(ref int code_pointer, ref BlastVariableDataType type)
        {
            int datasize = 1;

            // we get either a pop instruction or an instruction to get a value 
            byte c = code[code_pointer];
            code_pointer++;

#if DEVELOPMENT_BUILD || TRACE
            int size;
#endif

        LBL_LOOP:
            switch ((blast_operation)c)
            {
                case blast_operation.substract:
                    c = code[code_pointer];
                    code_pointer++;

                    // jumping into a signed loop saves a cmov and negation on most value acces assuming most values are not negated in script 
                    goto SIGNED_LOOP;

                //
                // stack pop 
                // 
                case blast_operation.pop:
                    type = GetMetaDataType(metadata, (byte)(stack_offset - datasize));

#if DEVELOPMENT_BUILD || TRACE 
                    size = GetMetaDataSize(metadata, (byte)(stack_offset - datasize));
                    if (size != datasize)
                    {
                        goto STACK_ERROR; 
                    }
#endif
                    // stack pop 
                    stack_offset = stack_offset - datasize;

                    return ((V1*)stack)[stack_offset];

                case blast_operation.pi: return math.PI;
                case blast_operation.inv_pi: return 1f / math.PI;
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
                case blast_operation.inv_value_2: return 1 / 2f;
                case blast_operation.inv_value_3: return 1 / 3f;
                case blast_operation.inv_value_4: return 1 / 4f;
                case blast_operation.inv_value_8: return 1 / 8f;
                case blast_operation.inv_value_10: return 1 / 10f;
                case blast_operation.inv_value_16: return 1 / 16f;
                case blast_operation.inv_value_24: return 1 / 24f;
                case blast_operation.inv_value_32: return 1 / 32f;
                case blast_operation.inv_value_64: return 1 / 64f;
                case blast_operation.inv_value_100: return 1 / 100f;
                case blast_operation.inv_value_128: return 1 / 128f;
                case blast_operation.inv_value_256: return 1 / 256f;
                case blast_operation.inv_value_512: return 1 / 512f;
                case blast_operation.inv_value_1000: return 1 / 1000f;
                case blast_operation.inv_value_1024: return 1 / 1024f;
                case blast_operation.inv_value_30: return 1 / 30f;
                case blast_operation.framecount: return engine_ptr->constants[(byte)blast_operation.framecount];
                case blast_operation.fixedtime: return engine_ptr->constants[(byte)blast_operation.fixedtime];
                case blast_operation.time: return engine_ptr->constants[(byte)blast_operation.time];
                case blast_operation.fixeddeltatime: return engine_ptr->constants[(byte)blast_operation.fixeddeltatime];
                case blast_operation.deltatime: return engine_ptr->constants[(byte)blast_operation.deltatime];


                default:
                case blast_operation.id:
#if DEVELOPMENT_BUILD || TRACE

                    if (c < opt_id)
                    {
                        goto COMPILER_ERROR; 
                    }

                    type = GetMetaDataType(metadata, (byte)(c - opt_id));
                    size = GetMetaDataSize(metadata, (byte)(c - opt_id));
                    if (size != datasize)
                    {
                        goto ID_ERROR; 
                    }
#else
                    type = GetMetaDataType(metadata, (byte)(c - opt_id));
#endif
                    // variable acccess
                    return ((V1*)data)[c - opt_id];

            }

        SIGNED_LOOP:
            switch ((blast_operation)c)
            {
                case blast_operation.substract:
                    c = code[code_pointer];
                    code_pointer++;
                    // jumping back to the signed loop on double negation (never compiled.. for completeness)
                    goto LBL_LOOP;

                //
                // stack pop 
                // 
                case blast_operation.pop:
                    type = GetMetaDataType(metadata, (byte)(stack_offset - datasize));

#if DEVELOPMENT_BUILD || TRACE 
                    size = GetMetaDataSize(metadata, (byte)(stack_offset - datasize));
                    if (size != datasize)
                    {
                        goto STACK_ERROR;
                    }
#endif
                    // stack pop 
                    stack_offset = stack_offset - datasize;

                    // SIGNED !!
                    return -((V1*)stack)[stack_offset];


                case blast_operation.pi: return -math.PI;
                case blast_operation.inv_pi: return 1f / -math.PI;
                case blast_operation.epsilon: return -math.EPSILON;
                case blast_operation.infinity: return -math.INFINITY;
                case blast_operation.negative_infinity: return math.INFINITY;
                case blast_operation.nan: return math.NAN;
                case blast_operation.min_value: return -math.FLT_MIN_NORMAL;
                case blast_operation.value_0: return -0f;
                case blast_operation.value_1: return -1f;
                case blast_operation.value_2: return -2f;
                case blast_operation.value_3: return -3f;
                case blast_operation.value_4: return -4f;
                case blast_operation.value_8: return -8f;
                case blast_operation.value_10: return -10f;
                case blast_operation.value_16: return -16f;
                case blast_operation.value_24: return -24f;
                case blast_operation.value_32: return -32f;
                case blast_operation.value_64: return -64f;
                case blast_operation.value_100: return -100f;
                case blast_operation.value_128: return -128f;
                case blast_operation.value_256: return -256f;
                case blast_operation.value_512: return -512f;
                case blast_operation.value_1000: return -1000f;
                case blast_operation.value_1024: return -1024f;
                case blast_operation.value_30: return -30f;
                case blast_operation.value_45: return -45f;
                case blast_operation.value_90: return -90f;
                case blast_operation.value_180: return -180f;
                case blast_operation.value_270: return -270f;
                case blast_operation.value_360: return -360f;
                case blast_operation.inv_value_2: return 1 / -2f;
                case blast_operation.inv_value_3: return 1 / -3f;
                case blast_operation.inv_value_4: return 1 / -4f;
                case blast_operation.inv_value_8: return 1 / -8f;
                case blast_operation.inv_value_10: return 1 / -10f;
                case blast_operation.inv_value_16: return 1 / -16f;
                case blast_operation.inv_value_24: return 1 / -24f;
                case blast_operation.inv_value_32: return 1 / -32f;
                case blast_operation.inv_value_64: return 1 / -64f;
                case blast_operation.inv_value_100: return 1 / -100f;
                case blast_operation.inv_value_128: return 1 / -128f;
                case blast_operation.inv_value_256: return 1 / -256f;
                case blast_operation.inv_value_512: return 1 / -512f;
                case blast_operation.inv_value_1000: return 1 / -1000f;
                case blast_operation.inv_value_1024: return 1 / -1024f;
                case blast_operation.inv_value_30: return 1 / -30f;
                case blast_operation.framecount: return -engine_ptr->constants[(byte)blast_operation.framecount];
                case blast_operation.fixedtime: return -engine_ptr->constants[(byte)blast_operation.fixedtime];
                case blast_operation.time: return -engine_ptr->constants[(byte)blast_operation.time];
                case blast_operation.fixeddeltatime: return -engine_ptr->constants[(byte)blast_operation.fixeddeltatime];
                case blast_operation.deltatime: return -engine_ptr->constants[(byte)blast_operation.deltatime];

                default:
                case blast_operation.id:
#if DEVELOPMENT_BUILD || TRACE

                    if (c < opt_id)
                    {
                        goto COMPILER_ERROR;
                    }

                    type = GetMetaDataType(metadata, (byte)(c - opt_id));
                    size = GetMetaDataSize(metadata, (byte)(c - opt_id));
                    if (size != datasize)
                    {
                        goto ID_ERROR; 
                    }
#else
                    type = GetMetaDataType(metadata, (byte)(c - opt_id));
#endif
                    // SIGNED
                    return -((V1*)data)[c - opt_id];

            }

            // should never reach here
            return V1.NaN;


#if DEVELOPMENT_BUILD || TRACE
        STACK_ERROR:
            Debug.LogError($"Blast.Interpretor.Stack.pop_f1 -> stackdata mismatch, expecting numeric of size {datasize}, found {type} of size {size} at stack offset {stack_offset}");
            return V1.NaN;

        ID_ERROR: 
            Debug.LogError($"Blast.Interpretor.Stack.pop_f1 -> data mismatch, expecting numeric of size {datasize}, found {type} of size {size} at data offset {c - opt_id}");
            return V1.NaN;

        COMPILER_ERROR:
            Debug.LogError("Blast.Interpretor.Stack.pop_f1 -> select op by constant value is not supported");
            return V1.NaN;
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        V2 pop_f2(ref int code_pointer, ref BlastVariableDataType type)
        {
            int datasize = 2;

            // we get either a pop instruction or an instruction to get a value 
            byte c = code[code_pointer];
            code_pointer++;

#if DEVELOPMENT_BUILD || TRACE
            int size;
#endif

        LBL_LOOP:
            switch ((blast_operation)c)
            {
                case blast_operation.substract:
                    c = code[code_pointer];
                    code_pointer++;

                    // jumping into a signed loop saves a cmov and negation on most value acces assuming most values are not negated in script 
                    goto SIGNED_LOOP;

                //
                // stack pop 
                // 
                case blast_operation.pop:
                    type = GetMetaDataType(metadata, (byte)(stack_offset - datasize));

#if DEVELOPMENT_BUILD || TRACE 
                    size = GetMetaDataSize(metadata, (byte)(stack_offset - datasize));
                    if (size != datasize)
                    {
                        goto STACK_ERROR; 
                    }
#endif
                    // stack pop 
                    stack_offset = stack_offset - datasize;
                    return ((V2*)(void*)&((V1*)stack)[stack_offset])[0];

                case blast_operation.pi: return math.PI;
                case blast_operation.inv_pi: return 1f / math.PI;
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
                case blast_operation.inv_value_2: return 1 / 2f;
                case blast_operation.inv_value_3: return 1 / 3f;
                case blast_operation.inv_value_4: return 1 / 4f;
                case blast_operation.inv_value_8: return 1 / 8f;
                case blast_operation.inv_value_10: return 1 / 10f;
                case blast_operation.inv_value_16: return 1 / 16f;
                case blast_operation.inv_value_24: return 1 / 24f;
                case blast_operation.inv_value_32: return 1 / 32f;
                case blast_operation.inv_value_64: return 1 / 64f;
                case blast_operation.inv_value_100: return 1 / 100f;
                case blast_operation.inv_value_128: return 1 / 128f;
                case blast_operation.inv_value_256: return 1 / 256f;
                case blast_operation.inv_value_512: return 1 / 512f;
                case blast_operation.inv_value_1000: return 1 / 1000f;
                case blast_operation.inv_value_1024: return 1 / 1024f;
                case blast_operation.inv_value_30: return 1 / 30f;
                case blast_operation.framecount: return engine_ptr->constants[(byte)blast_operation.framecount];
                case blast_operation.fixedtime: return engine_ptr->constants[(byte)blast_operation.fixedtime];
                case blast_operation.time: return engine_ptr->constants[(byte)blast_operation.time];
                case blast_operation.fixeddeltatime: return engine_ptr->constants[(byte)blast_operation.fixeddeltatime];
                case blast_operation.deltatime: return engine_ptr->constants[(byte)blast_operation.deltatime];


                default:
                case blast_operation.id:
#if DEVELOPMENT_BUILD || TRACE

                    if (c < opt_id)
                    {
                        goto COMPILER_ERROR; 
                    }

                    type = GetMetaDataType(metadata, (byte)(c - opt_id));
                    size = GetMetaDataSize(metadata, (byte)(c - opt_id));
                    if (size != datasize)
                    {
                        goto ID_ERROR; 
                    }
#else
                    type = GetMetaDataType(metadata, (byte)(c - opt_id));
#endif
                    // variable acccess
                    return ((V2*)(void*)&((V1*)data)[c - opt_id])[0];
            }

        SIGNED_LOOP:
            switch ((blast_operation)c)
            {
                case blast_operation.substract:
                    c = code[code_pointer];
                    code_pointer++;
                    // jumping back to the signed loop on double negation (never compiled.. for completeness)
                    goto LBL_LOOP;

                //
                // stack pop 
                // 
                case blast_operation.pop:
                    type = GetMetaDataType(metadata, (byte)(stack_offset - datasize));

#if DEVELOPMENT_BUILD || TRACE 
                    size = GetMetaDataSize(metadata, (byte)(stack_offset - datasize));
                    if (size != datasize)
                    {
                        goto STACK_ERROR; 
                    }
#endif
                    // stack pop 
                    stack_offset = stack_offset - datasize;

                    // SIGNED !!
                    return -((V2*)(void*)&((V1*)stack)[stack_offset])[0];


                case blast_operation.pi: return -math.PI;
                case blast_operation.inv_pi: return 1f / -math.PI;
                case blast_operation.epsilon: return -math.EPSILON;
                case blast_operation.infinity: return -math.INFINITY;
                case blast_operation.negative_infinity: return math.INFINITY;
                case blast_operation.nan: return math.NAN;
                case blast_operation.min_value: return -math.FLT_MIN_NORMAL;
                case blast_operation.value_0: return -0f;
                case blast_operation.value_1: return -1f;
                case blast_operation.value_2: return -2f;
                case blast_operation.value_3: return -3f;
                case blast_operation.value_4: return -4f;
                case blast_operation.value_8: return -8f;
                case blast_operation.value_10: return -10f;
                case blast_operation.value_16: return -16f;
                case blast_operation.value_24: return -24f;
                case blast_operation.value_32: return -32f;
                case blast_operation.value_64: return -64f;
                case blast_operation.value_100: return -100f;
                case blast_operation.value_128: return -128f;
                case blast_operation.value_256: return -256f;
                case blast_operation.value_512: return -512f;
                case blast_operation.value_1000: return -1000f;
                case blast_operation.value_1024: return -1024f;
                case blast_operation.value_30: return -30f;
                case blast_operation.value_45: return -45f;
                case blast_operation.value_90: return -90f;
                case blast_operation.value_180: return -180f;
                case blast_operation.value_270: return -270f;
                case blast_operation.value_360: return -360f;
                case blast_operation.inv_value_2: return 1 / -2f;
                case blast_operation.inv_value_3: return 1 / -3f;
                case blast_operation.inv_value_4: return 1 / -4f;
                case blast_operation.inv_value_8: return 1 / -8f;
                case blast_operation.inv_value_10: return 1 / -10f;
                case blast_operation.inv_value_16: return 1 / -16f;
                case blast_operation.inv_value_24: return 1 / -24f;
                case blast_operation.inv_value_32: return 1 / -32f;
                case blast_operation.inv_value_64: return 1 / -64f;
                case blast_operation.inv_value_100: return 1 / -100f;
                case blast_operation.inv_value_128: return 1 / -128f;
                case blast_operation.inv_value_256: return 1 / -256f;
                case blast_operation.inv_value_512: return 1 / -512f;
                case blast_operation.inv_value_1000: return 1 / -1000f;
                case blast_operation.inv_value_1024: return 1 / -1024f;
                case blast_operation.inv_value_30: return 1 / -30f;
                case blast_operation.framecount: return -engine_ptr->constants[(byte)blast_operation.framecount];
                case blast_operation.fixedtime: return -engine_ptr->constants[(byte)blast_operation.fixedtime];
                case blast_operation.time: return -engine_ptr->constants[(byte)blast_operation.time];
                case blast_operation.fixeddeltatime: return -engine_ptr->constants[(byte)blast_operation.fixeddeltatime];
                case blast_operation.deltatime: return -engine_ptr->constants[(byte)blast_operation.deltatime];

                default:
                case blast_operation.id:
#if DEVELOPMENT_BUILD || TRACE

                    if (c < opt_id)
                    {
                        goto COMPILER_ERROR; 
                    }

                    type = GetMetaDataType(metadata, (byte)(c - opt_id));
                    size = GetMetaDataSize(metadata, (byte)(c - opt_id));
                    if (size != datasize)
                    {
                        goto ID_ERROR; 
                    }
#else
                    type = GetMetaDataType(metadata, (byte)(c - opt_id));
#endif
                    // SIGNED
                    return -((V2*)(void*)&((V1*)data)[c - opt_id])[0];

            }

            // should never reach here
            return V1.NaN;

#if DEVELOPMENT_BUILD || TRACE
        STACK_ERROR:
            Debug.LogError($"Blast.Interpretor.Stack.pop_f2 -> stackdata mismatch, expecting numeric of size {datasize}, found {type} of size {size} at stack offset {stack_offset}");
            return V1.NaN;

        ID_ERROR: 
            Debug.LogError($"Blast.Interpretor.Stack.pop_f2 -> data mismatch, expecting numeric of size {datasize}, found {type} of size {size} at data offset {c - opt_id}");
            return V1.NaN;

        COMPILER_ERROR:
            Debug.LogError("Blast.Interpretor.Stack.pop_f2 -> select op by constant value is not supported");
            return V1.NaN;
#endif 
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        V3 pop_f3(ref int code_pointer, ref BlastVariableDataType type)
        {
            int datasize = 3;

            // we get either a pop instruction or an instruction to get a value 
            byte c = code[code_pointer];
            code_pointer++;

#if DEVELOPMENT_BUILD || TRACE
            int size;
#endif

        LBL_LOOP:
            switch ((blast_operation)c)
            {
                case blast_operation.substract:
                    c = code[code_pointer];
                    code_pointer++;

                    // jumping into a signed loop saves a cmov and negation on most value acces assuming most values are not negated in script 
                    goto SIGNED_LOOP;

                //
                // stack pop 
                // 
                case blast_operation.pop:
                    type = GetMetaDataType(metadata, (byte)(stack_offset - datasize));

#if DEVELOPMENT_BUILD || TRACE 
                    size = GetMetaDataSize(metadata, (byte)(stack_offset - datasize));
                    if (size != datasize)
                    {
                        goto STACK_ERROR; 
                    }
#endif
                    // stack pop 
                    stack_offset = stack_offset - datasize;
                    return ((V3*)(void*)&((V1*)stack)[stack_offset])[0];

                case blast_operation.pi: return math.PI;
                case blast_operation.inv_pi: return 1f / math.PI;
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
                case blast_operation.inv_value_2: return 1 / 2f;
                case blast_operation.inv_value_3: return 1 / 3f;
                case blast_operation.inv_value_4: return 1 / 4f;
                case blast_operation.inv_value_8: return 1 / 8f;
                case blast_operation.inv_value_10: return 1 / 10f;
                case blast_operation.inv_value_16: return 1 / 16f;
                case blast_operation.inv_value_24: return 1 / 24f;
                case blast_operation.inv_value_32: return 1 / 32f;
                case blast_operation.inv_value_64: return 1 / 64f;
                case blast_operation.inv_value_100: return 1 / 100f;
                case blast_operation.inv_value_128: return 1 / 128f;
                case blast_operation.inv_value_256: return 1 / 256f;
                case blast_operation.inv_value_512: return 1 / 512f;
                case blast_operation.inv_value_1000: return 1 / 1000f;
                case blast_operation.inv_value_1024: return 1 / 1024f;
                case blast_operation.inv_value_30: return 1 / 30f;
                case blast_operation.framecount: return engine_ptr->constants[(byte)blast_operation.framecount];
                case blast_operation.fixedtime: return engine_ptr->constants[(byte)blast_operation.fixedtime];
                case blast_operation.time: return engine_ptr->constants[(byte)blast_operation.time];
                case blast_operation.fixeddeltatime: return engine_ptr->constants[(byte)blast_operation.fixeddeltatime];
                case blast_operation.deltatime: return engine_ptr->constants[(byte)blast_operation.deltatime];


                default:
                case blast_operation.id:
#if DEVELOPMENT_BUILD || TRACE

                    if (c < opt_id)
                    {
                        goto COMPILER_ERROR; 
                    }

                    type = GetMetaDataType(metadata, (byte)(c - opt_id));
                    size = GetMetaDataSize(metadata, (byte)(c - opt_id));
                    if (size != datasize)
                    {
                        goto ID_ERROR; 
                    }
#else
                    type = GetMetaDataType(metadata, (byte)(c - opt_id));
#endif
                    // variable acccess
                    return ((V3*)(void*)&((V1*)data)[c - opt_id])[0];
            }

        SIGNED_LOOP:
            switch ((blast_operation)c)
            {
                case blast_operation.substract:
                    c = code[code_pointer];
                    code_pointer++;
                    // jumping back to the signed loop on double negation (never compiled.. for completeness)
                    goto LBL_LOOP;

                //
                // stack pop 
                // 
                case blast_operation.pop:
                    type = GetMetaDataType(metadata, (byte)(stack_offset - datasize));

#if DEVELOPMENT_BUILD || TRACE 
                    size = GetMetaDataSize(metadata, (byte)(stack_offset - datasize));
                    if (size != datasize)
                    {
                        goto STACK_ERROR; 
                    }
#endif
                    // stack pop 
                    stack_offset = stack_offset - datasize;

                    // SIGNED !!
                    return -((V3*)(void*)&((V1*)stack)[stack_offset])[0];


                case blast_operation.pi: return -math.PI;
                case blast_operation.inv_pi: return 1f / -math.PI;
                case blast_operation.epsilon: return -math.EPSILON;
                case blast_operation.infinity: return -math.INFINITY;
                case blast_operation.negative_infinity: return math.INFINITY;
                case blast_operation.nan: return math.NAN;
                case blast_operation.min_value: return -math.FLT_MIN_NORMAL;
                case blast_operation.value_0: return -0f;
                case blast_operation.value_1: return -1f;
                case blast_operation.value_2: return -2f;
                case blast_operation.value_3: return -3f;
                case blast_operation.value_4: return -4f;
                case blast_operation.value_8: return -8f;
                case blast_operation.value_10: return -10f;
                case blast_operation.value_16: return -16f;
                case blast_operation.value_24: return -24f;
                case blast_operation.value_32: return -32f;
                case blast_operation.value_64: return -64f;
                case blast_operation.value_100: return -100f;
                case blast_operation.value_128: return -128f;
                case blast_operation.value_256: return -256f;
                case blast_operation.value_512: return -512f;
                case blast_operation.value_1000: return -1000f;
                case blast_operation.value_1024: return -1024f;
                case blast_operation.value_30: return -30f;
                case blast_operation.value_45: return -45f;
                case blast_operation.value_90: return -90f;
                case blast_operation.value_180: return -180f;
                case blast_operation.value_270: return -270f;
                case blast_operation.value_360: return -360f;
                case blast_operation.inv_value_2: return 1 / -2f;
                case blast_operation.inv_value_3: return 1 / -3f;
                case blast_operation.inv_value_4: return 1 / -4f;
                case blast_operation.inv_value_8: return 1 / -8f;
                case blast_operation.inv_value_10: return 1 / -10f;
                case blast_operation.inv_value_16: return 1 / -16f;
                case blast_operation.inv_value_24: return 1 / -24f;
                case blast_operation.inv_value_32: return 1 / -32f;
                case blast_operation.inv_value_64: return 1 / -64f;
                case blast_operation.inv_value_100: return 1 / -100f;
                case blast_operation.inv_value_128: return 1 / -128f;
                case blast_operation.inv_value_256: return 1 / -256f;
                case blast_operation.inv_value_512: return 1 / -512f;
                case blast_operation.inv_value_1000: return 1 / -1000f;
                case blast_operation.inv_value_1024: return 1 / -1024f;
                case blast_operation.inv_value_30: return 1 / -30f;
                case blast_operation.framecount: return -engine_ptr->constants[(byte)blast_operation.framecount];
                case blast_operation.fixedtime: return -engine_ptr->constants[(byte)blast_operation.fixedtime];
                case blast_operation.time: return -engine_ptr->constants[(byte)blast_operation.time];
                case blast_operation.fixeddeltatime: return -engine_ptr->constants[(byte)blast_operation.fixeddeltatime];
                case blast_operation.deltatime: return -engine_ptr->constants[(byte)blast_operation.deltatime];

                default:
                case blast_operation.id:
#if DEVELOPMENT_BUILD || TRACE

                    if (c < opt_id)
                    {
                        goto COMPILER_ERROR; 
                    }

                    type = GetMetaDataType(metadata, (byte)(c - opt_id));
                    size = GetMetaDataSize(metadata, (byte)(c - opt_id));
                    if (size != datasize)
                    {
                        goto ID_ERROR; 
                    }
#else
                    type = GetMetaDataType(metadata, (byte)(c - opt_id));
#endif
                    // SIGNED
                    return -((V3*)(void*)&((V1*)data)[c - opt_id])[0];

            }

            // should never reach here
            return V1.NaN;

#if DEVELOPMENT_BUILD || TRACE
        STACK_ERROR:
            Debug.LogError($"Blast.Interpretor.Stack.pop_f3 -> stackdata mismatch, expecting numeric of size {datasize}, found {type} of size {size} at stack offset {stack_offset}");
            return V1.NaN;

        ID_ERROR: 
            Debug.LogError($"Blast.Interpretor.Stack.pop_f3 -> data mismatch, expecting numeric of size {datasize}, found {type} of size {size} at data offset {c - opt_id}");
            return V1.NaN;

        COMPILER_ERROR:
            Debug.LogError("Blast.Interpretor.Stack.pop_f3 -> select op by constant value is not supported");
            return V1.NaN;
#endif 
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        V4 pop_f4(ref int code_pointer, ref BlastVariableDataType type)
        {
            int datasize = 4;

            // we get either a pop instruction or an instruction to get a value 
            byte c = code[code_pointer];
            code_pointer++;

#if DEVELOPMENT_BUILD || TRACE
            int size;
#endif

        LBL_LOOP:
            switch ((blast_operation)c)
            {
                case blast_operation.substract:
                    c = code[code_pointer];
                    code_pointer++;

                    // jumping into a signed loop saves a cmov and negation on most value acces assuming most values are not negated in script 
                    goto SIGNED_LOOP;

                //
                // stack pop 
                // 
                case blast_operation.pop:
                    type = GetMetaDataType(metadata, (byte)(stack_offset - datasize));

#if DEVELOPMENT_BUILD || TRACE 
                    size = GetMetaDataSize(metadata, (byte)(stack_offset - datasize));
                    if (size != datasize)
                    {
                        goto STACK_ERROR; 
                    }
#endif
                    // stack pop 
                    stack_offset = stack_offset - datasize;
                    return ((V4*)(void*)&((V1*)stack)[stack_offset])[0];

                case blast_operation.pi: return math.PI;
                case blast_operation.inv_pi: return 1f / math.PI;
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
                case blast_operation.inv_value_2: return 1 / 2f;
                case blast_operation.inv_value_3: return 1 / 3f;
                case blast_operation.inv_value_4: return 1 / 4f;
                case blast_operation.inv_value_8: return 1 / 8f;
                case blast_operation.inv_value_10: return 1 / 10f;
                case blast_operation.inv_value_16: return 1 / 16f;
                case blast_operation.inv_value_24: return 1 / 24f;
                case blast_operation.inv_value_32: return 1 / 32f;
                case blast_operation.inv_value_64: return 1 / 64f;
                case blast_operation.inv_value_100: return 1 / 100f;
                case blast_operation.inv_value_128: return 1 / 128f;
                case blast_operation.inv_value_256: return 1 / 256f;
                case blast_operation.inv_value_512: return 1 / 512f;
                case blast_operation.inv_value_1000: return 1 / 1000f;
                case blast_operation.inv_value_1024: return 1 / 1024f;
                case blast_operation.inv_value_30: return 1 / 30f;
                case blast_operation.framecount: return engine_ptr->constants[(byte)blast_operation.framecount];
                case blast_operation.fixedtime: return engine_ptr->constants[(byte)blast_operation.fixedtime];
                case blast_operation.time: return engine_ptr->constants[(byte)blast_operation.time];
                case blast_operation.fixeddeltatime: return engine_ptr->constants[(byte)blast_operation.fixeddeltatime];
                case blast_operation.deltatime: return engine_ptr->constants[(byte)blast_operation.deltatime];


                default:
                case blast_operation.id:
#if DEVELOPMENT_BUILD || TRACE

                    if (c < opt_id)
                    {
                        goto COMPILER_ERROR; 
                    }

                    type = GetMetaDataType(metadata, (byte)(c - opt_id));
                    size = GetMetaDataSize(metadata, (byte)(c - opt_id));
                    if (size != datasize)
                    {
                        goto ID_ERROR; 
                    }
#else
                    type = GetMetaDataType(metadata, (byte)(c - opt_id));
#endif
                    // variable acccess
                    return ((V4*)(void*)&((V1*)data)[c - opt_id])[0];
            }

        SIGNED_LOOP:
            switch ((blast_operation)c)
            {
                case blast_operation.substract:
                    c = code[code_pointer];
                    code_pointer++;
                    // jumping back to the signed loop on double negation (never compiled.. for completeness)
                    goto LBL_LOOP;

                //
                // stack pop 
                // 
                case blast_operation.pop:
                    type = GetMetaDataType(metadata, (byte)(stack_offset - datasize));

#if DEVELOPMENT_BUILD || TRACE 
                    size = GetMetaDataSize(metadata, (byte)(stack_offset - datasize));
                    if (size != datasize)
                    {
                        goto STACK_ERROR; 
                    }
#endif
                    // stack pop 
                    stack_offset = stack_offset - datasize;

                    // SIGNED !!
                    return -((V4*)(void*)&((V1*)stack)[stack_offset])[0];


                case blast_operation.pi: return -math.PI;
                case blast_operation.inv_pi: return 1f / -math.PI;
                case blast_operation.epsilon: return -math.EPSILON;
                case blast_operation.infinity: return -math.INFINITY;
                case blast_operation.negative_infinity: return math.INFINITY;
                case blast_operation.nan: return math.NAN;
                case blast_operation.min_value: return -math.FLT_MIN_NORMAL;
                case blast_operation.value_0: return -0f;
                case blast_operation.value_1: return -1f;
                case blast_operation.value_2: return -2f;
                case blast_operation.value_3: return -3f;
                case blast_operation.value_4: return -4f;
                case blast_operation.value_8: return -8f;
                case blast_operation.value_10: return -10f;
                case blast_operation.value_16: return -16f;
                case blast_operation.value_24: return -24f;
                case blast_operation.value_32: return -32f;
                case blast_operation.value_64: return -64f;
                case blast_operation.value_100: return -100f;
                case blast_operation.value_128: return -128f;
                case blast_operation.value_256: return -256f;
                case blast_operation.value_512: return -512f;
                case blast_operation.value_1000: return -1000f;
                case blast_operation.value_1024: return -1024f;
                case blast_operation.value_30: return -30f;
                case blast_operation.value_45: return -45f;
                case blast_operation.value_90: return -90f;
                case blast_operation.value_180: return -180f;
                case blast_operation.value_270: return -270f;
                case blast_operation.value_360: return -360f;
                case blast_operation.inv_value_2: return 1 / -2f;
                case blast_operation.inv_value_3: return 1 / -3f;
                case blast_operation.inv_value_4: return 1 / -4f;
                case blast_operation.inv_value_8: return 1 / -8f;
                case blast_operation.inv_value_10: return 1 / -10f;
                case blast_operation.inv_value_16: return 1 / -16f;
                case blast_operation.inv_value_24: return 1 / -24f;
                case blast_operation.inv_value_32: return 1 / -32f;
                case blast_operation.inv_value_64: return 1 / -64f;
                case blast_operation.inv_value_100: return 1 / -100f;
                case blast_operation.inv_value_128: return 1 / -128f;
                case blast_operation.inv_value_256: return 1 / -256f;
                case blast_operation.inv_value_512: return 1 / -512f;
                case blast_operation.inv_value_1000: return 1 / -1000f;
                case blast_operation.inv_value_1024: return 1 / -1024f;
                case blast_operation.inv_value_30: return 1 / -30f;
                case blast_operation.framecount: return -engine_ptr->constants[(byte)blast_operation.framecount];
                case blast_operation.fixedtime: return -engine_ptr->constants[(byte)blast_operation.fixedtime];
                case blast_operation.time: return -engine_ptr->constants[(byte)blast_operation.time];
                case blast_operation.fixeddeltatime: return -engine_ptr->constants[(byte)blast_operation.fixeddeltatime];
                case blast_operation.deltatime: return -engine_ptr->constants[(byte)blast_operation.deltatime];

                default:
                case blast_operation.id:
#if DEVELOPMENT_BUILD || TRACE

                    if (c < opt_id)
                    {
                        goto COMPILER_ERROR; 
                    }

                    type = GetMetaDataType(metadata, (byte)(c - opt_id));
                    size = GetMetaDataSize(metadata, (byte)(c - opt_id));
                    if (size != datasize)
                    {
                        goto ID_ERROR; 
                    }
#else
                    type = GetMetaDataType(metadata, (byte)(c - opt_id));
#endif
                    // SIGNED
                    return -((V4*)(void*)&((V1*)data)[c - opt_id])[0];

            }

            // should never reach here
            return V1.NaN;

#if DEVELOPMENT_BUILD || TRACE
        STACK_ERROR:                                      
            Debug.LogError($"Blast.Interpretor.Stack.pop_f4 -> stackdata mismatch, expecting numeric of size {datasize}, found {type} of size {size} at stack offset {stack_offset}");
            return V1.NaN;

        ID_ERROR: 
            Debug.LogError($"Blast.Interpretor.Stack.pop_f4 -> data mismatch, expecting numeric of size {datasize}, found {type} of size {size} at data offset {c - opt_id}");
            return V1.NaN;

        COMPILER_ERROR:
            Debug.LogError("Blast.Interpretor.Stack.pop_f4 -> select op by constant value is not supported");
            return V1.NaN;
#endif 
        }

        #endregion

        #region derivations of pop_fx: pop_as_ix, pop_as_fx, pop_as_boolean

        /// <summary>
        /// pop float of vectorsize 1, forced float type, verify type on debug   (equals pop_or_value)
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        Bool32 pop_b32(ref int code_pointer)
        {
            // we get either a pop instruction or an instruction to get a value 
            byte c = code[code_pointer];
            code_pointer++;

            if (c >= opt_id)
            {
#if DEVELOPMENT_BUILD || TRACE 
                BlastVariableDataType type = GetMetaDataType(metadata, (byte)(c - opt_id));
                int size = GetMetaDataSize(metadata, (byte)(c - opt_id));
                if (size != 1 || type != BlastVariableDataType.Bool32)
                {
                    Debug.LogError($"blast.stack, pop_b32 -> data mismatch, expecting bool32 of size 1, found {type} of size {size} at data offset {c - opt_id}");
                    return Bool32.FailPattern;
                }
#endif
                // variable acccess
                return ((Bool32*)data)[c - opt_id];
            }
            else
            if (c == (byte)blast_operation.pop)
            {
#if DEVELOPMENT_BUILD || TRACE
                BlastVariableDataType type = GetMetaDataType(metadata, (byte)(stack_offset - 1));
                int size = GetMetaDataSize(metadata, (byte)(stack_offset - 1));
                if (size != 1 || type != BlastVariableDataType.Bool32)
                {
                    Debug.LogError($"blast.stack, pop_b32 -> stackdata mismatch, expecting bool32 of size 1, found {type} of size {size} at stack offset {stack_offset}");
                    return Bool32.FailPattern;
                }
#endif
                // stack pop 
                stack_offset = stack_offset - 1;
                return ((Bool32*)data)[stack_offset];
            }
            else
            if (c >= opt_value)
            {
                return Bool32.From(engine_ptr->constants[c]);
            }
            else
            {
                // error or.. constant by operation value.... 
#if DEVELOPMENT_BUILD || TRACE
                Debug.LogError("blast.stack, pop_b32 -> select op by constant value is not supported");
#endif
                return Bool32.FailPattern;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        int pop_as_i1(ref int code_pointer)
        {
            BlastVariableDataType datatype = BlastVariableDataType.Numeric;

            V1 v1 = pop_f1(ref code_pointer, ref datatype);

            switch (datatype)
            {
                case BlastVariableDataType.Numeric: return (int)v1;

                default:
                case BlastVariableDataType.ID: return CodeUtils.ReInterpret<V1, int>(v1);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        int2 pop_as_i2(ref int code_pointer)
        {
            BlastVariableDataType datatype = BlastVariableDataType.Numeric;

            V2 v2 = pop_f2(ref code_pointer, ref datatype);

            switch (datatype)
            {
                case BlastVariableDataType.Numeric: return (int2)v2;

                default:
                case BlastVariableDataType.ID: return CodeUtils.ReInterpret<V2, int2>(v2);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        int3 pop_as_i3(ref int code_pointer)
        {
            BlastVariableDataType datatype = BlastVariableDataType.Numeric;

            V3 v3 = pop_f3(ref code_pointer, ref datatype);

            switch (datatype)
            {
                case BlastVariableDataType.Numeric: return (int3)v3;

                default:
                case BlastVariableDataType.ID: return CodeUtils.ReInterpret<V3, int3>(v3);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        int4 pop_as_i4(ref int code_pointer)
        {
            BlastVariableDataType datatype = BlastVariableDataType.Numeric;

            V4 v4 = pop_f4(ref code_pointer, ref datatype);

            switch (datatype)
            {
                case BlastVariableDataType.Numeric: return (int4)v4;

                default:
                case BlastVariableDataType.ID: return CodeUtils.ReInterpret<V4, int4>(v4);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        float pop_as_f1(ref int code_pointer)
        {
            BlastVariableDataType datatype = BlastVariableDataType.Numeric;

            V1 v1 = pop_f1(ref code_pointer, ref datatype);

            // the select is most probably faster then a branch, todo: test the difference 
            return math.select(
                (float)CodeUtils.ReInterpret<V1, int>(v1),  // datatype should be interpreted as type (has to be int), then casted 
                v1,                                         // datatype matches , no change 
                datatype == BlastVariableDataType.Numeric);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        float2 pop_as_f2(ref int code_pointer)
        {
            BlastVariableDataType datatype = BlastVariableDataType.Numeric;

            V2 v2 = pop_f2(ref code_pointer, ref datatype);

            // the select is most probably faster then a branch, todo: test the difference 
            return math.select(
                (float2)CodeUtils.ReInterpret<V2, int2>(v2),  // datatype should be interpreted as type (has to be int), then casted 
                v2,                                           // datatype matches , no change 
                datatype == BlastVariableDataType.Numeric);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        float3 pop_as_f3(ref int code_pointer)
        {
            BlastVariableDataType datatype = BlastVariableDataType.Numeric;

            V3 v3 = pop_f3(ref code_pointer, ref datatype);

            // the select is most probably faster then a branch, todo: test the difference 
            return math.select(
                (float3)CodeUtils.ReInterpret<V3, int3>(v3),  // datatype should be interpreted as type (has to be int), then casted 
                v3,                                           // datatype matches , no change 
                datatype == BlastVariableDataType.Numeric);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        float4 pop_as_f4(ref int code_pointer)
        {
            BlastVariableDataType datatype = BlastVariableDataType.Numeric;

            V4 v4 = pop_f4(ref code_pointer, ref datatype);

            // the select is most probably faster then a branch, todo: test the difference 
            return math.select(
                (float4)CodeUtils.ReInterpret<V4, int4>(v4),  // datatype should be interpreted as type (has to be int), then casted 
                v4,                                           // datatype matches , no change 
                datatype == BlastVariableDataType.Numeric);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        bool pop_as_boolean(ref int code_pointer)
        {
            BlastVariableDataType datatype = default;
            return pop_f1(ref code_pointer, ref datatype) != 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        bool2 pop_as_boolean2(ref int code_pointer)
        {
            BlastVariableDataType datatype = default;
            return pop_f2(ref code_pointer, ref datatype) != 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        bool3 pop_as_boolean3(ref int code_pointer)
        {
            BlastVariableDataType datatype = default;
            return pop_f3(ref code_pointer, ref datatype) != 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        bool4 pop_as_boolean4(ref int code_pointer)
        {
            BlastVariableDataType datatype = default;
            return pop_f4(ref code_pointer, ref datatype) != 0;
        }



        #endregion

        #region pop* + info

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void* pop_p_info(ref int code_pointer, out BlastVariableDataType type, out byte vector_size, out bool is_negated)
        {
            // we get either a pop instruction or an instruction to get a value 
            byte c = code[code_pointer];
            code_pointer++;
            is_negated = false;


        LBL_LOOP:
            switch ((blast_operation)c)
            {
                case blast_operation.substract:
                    is_negated = !is_negated;
                    c = code[code_pointer];
                    code_pointer++;
                    goto LBL_LOOP;

                //
                // stack pop 
                // 
                case blast_operation.pop:
                    type = GetMetaDataType(metadata, (byte)(stack_offset - 1));
                    vector_size = GetMetaDataSize(metadata, (byte)(stack_offset - 1));
                    vector_size = (byte)math.select(vector_size, 4, vector_size == 0);

#if DEVELOPMENT_BUILD || TRACE
                    if (!type.IsValidOnStack(vector_size))
                    {
                        Debug.LogError($"blast.stack, pop_or_value -> stackdata mismatch, found {type} of size {vector_size} at stack offset {stack_offset}");
                        vector_size = 0;
                        return null;
                    }
#endif
                    // stack pop 
                    stack_offset = stack_offset - vector_size;
                    return &((float*)stack)[stack_offset];


                // No inlined constants in normal interpretation 
                case blast_operation.constant_f1:
                case blast_operation.constant_f1_h:
                case blast_operation.constant_short_ref:
                case blast_operation.constant_long_ref:
                    break;

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
                case blast_operation.framecount:
                case blast_operation.fixedtime:
                case blast_operation.time:
                case blast_operation.fixeddeltatime:
                case blast_operation.deltatime:
                    vector_size = 1;
                    type = BlastVariableDataType.Numeric;
                    return &engine_ptr->constants[c];


                default:
                case blast_operation.id:
#if DEVELOPMENT_BUILD || TRACE

                    if (c < opt_id)
                    {
                        // bug in compiler 
                        Debug.LogError("pop_f1 -> select op by constant value is not supported");
                        type = default;
                        vector_size = 0;
                        return null;
                    }
#endif

                    type = GetMetaDataType(metadata, (byte)(c - opt_id));
                    vector_size = GetMetaDataSize(metadata, (byte)(c - opt_id));
                    vector_size = (byte)math.select(vector_size, 4, vector_size == 0);

#if DEVELOPMENT_BUILD || TRACE
                    if (!type.IsValidOnStack(vector_size))
                    {
                        Debug.LogError($"blast.stack, pop_or_value -> data mismatch, expecting numeric, found {type} of size {vector_size} at data offset {c - opt_id}");
                        return null;
                    }
#endif
                    // variable acccess
                    return &((float*)data)[c - opt_id];

            }

#if TRACE
            Debug.LogError($"blast.interpretor.pop_p_info: expected data at {code_pointer}"); 
#endif 

            // should never reach here
            vector_size = 0;
            type = default;
            return null;
        }


        internal void* pop_with_info_indexed(ref int code_pointer, out BlastVariableDataType type, out byte vector_size)
        {
            // we get either a pop instruction or an instruction to get a value 
            int index = -1;

        LABEL_POP_WITH_INFO_LOOP:
            byte c = code[code_pointer];

            switch ((blast_operation)c)
            {
                case blast_operation.pop:
                    {
                        // stacked 
                        vector_size = GetMetaDataSize(metadata, (byte)(stack_offset - 1));
                        type = GetMetaDataType(metadata, (byte)(stack_offset - 1));

                        // need to advance past vectorsize instead of only 1 that we need to probe 

                        vector_size = (byte)math.select(vector_size, 4, vector_size == 0);
                        stack_offset -= vector_size;

                        // if indexed then vectorsize == 1 
                        vector_size = (byte)math.select(vector_size, 1, index >= 0);

                        // add any index to stackoffset    
                        stack_offset = math.select(stack_offset, stack_offset + index, index >= 0);

                        return &((float*)stack)[stack_offset];
                    };

                case blast_operation.index_x: index = 0; code_pointer++; goto LABEL_POP_WITH_INFO_LOOP;
                case blast_operation.index_y: index = 1; code_pointer++; goto LABEL_POP_WITH_INFO_LOOP;
                case blast_operation.index_z: index = 2; code_pointer++; goto LABEL_POP_WITH_INFO_LOOP;
                case blast_operation.index_w: index = 3; code_pointer++; goto LABEL_POP_WITH_INFO_LOOP;

                default:
                    {
                        if (c >= opt_id)
                        {
                            int data_offset = c - opt_id;

                            // variable 
                            GetMetaData(metadata, (byte)data_offset, out vector_size, out type);

                            // if indexed then vectorsize == 1 and increase dataoffset with index 
                            vector_size = (byte)math.select(vector_size, 1, index >= 0);
                            data_offset = math.select(data_offset, data_offset + index, index >= 0);

                            return &((float*)data)[data_offset];
                        }
                        else
                        //
                        // TODO     include constant jumptable ditching this branch.. 
                        // 
                        if (c >= opt_value)
                        {
                            type = BlastVariableDataType.Numeric;
                            vector_size = 1;
                            return &engine_ptr->constants[c];
                        }
                        else
                        {
                            // error or.. constant by operation value....    (value < 77 not equal to pop)
                            //#if DEVELOPMENT_BUILD || TRACE
                            Debug.LogError($"BlastInterpretor: pop_or_value -> select op by constant value is not supported, at codepointer: {code_pointer} => {code[code_pointer]}");
                            //#endif
                            type = BlastVariableDataType.Numeric;
                            vector_size = 1;
                            return null;
                        }

                    }
            }
        }

        #endregion

        #region the actual push-pop


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void push(in V1 v, in BlastVariableDataType datatype = BlastVariableDataType.Numeric)
        {
            // set stack value 
            ((V1*)stack)[stack_offset] = v;

            // update metadata with type set 
            SetMetaData(metadata, datatype, 1, (byte)stack_offset);

            // advance 1 element in stack offset 
            stack_offset += 1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void push(V2 v2, in BlastVariableDataType datatype = BlastVariableDataType.Numeric)
        {
            // really... wonder how this compiles (should be simple 8byte move) TODO 
            ((V2*)(void*)&((float*)stack)[stack_offset])[0] = v2;

            SetMetaData(metadata, datatype, 2, (byte)stack_offset);

            stack_offset += 2;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void push(V3 v3, in BlastVariableDataType datatype = BlastVariableDataType.Numeric)
        {
            ((V3*)(void*)&((float*)stack)[stack_offset])[0] = v3;

            SetMetaData(metadata, datatype, 3, (byte)stack_offset);

            stack_offset += 3;  // size 3
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void push(V4 v4, in BlastVariableDataType datatype = BlastVariableDataType.Numeric)
        {
            // really... wonder how this compiles (should be simple 16byte move) TODO 
            ((V4*)(void*)&((float*)stack)[stack_offset])[0] = v4;

            SetMetaData(metadata, datatype, 4, (byte)stack_offset);

            stack_offset += 4;  // size 4
        }


        /// <summary>
        /// only used in yield before execute start
        /// </summary>
        /// <param name="f"></param>
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

            f = ((float*)stack)[stack_offset];
        }

        /// <summary>
        /// only used in yield before execute 
        /// </summary>
        /// <param name="f"></param>

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
            float* fdata = (float*)stack;
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
            int* idata = (int*)stack;
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

        #endregion

    }
}
