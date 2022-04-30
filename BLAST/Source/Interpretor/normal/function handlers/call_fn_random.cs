//############################################################################################################################
// BLAST v1.0.4c                                                                                                             #
// Copyright © 2022 Rob Lemmens | NijnStein Software <rob.lemmens.s31 gmail com> All Rights Reserved                   ^__^\ #
// Unauthorized copying of this file, via any medium is strictly prohibited proprietary and confidential               (oo)\ #
//                                                                                                                     (__)  #
//############################################################################################################################
#pragma warning disable CS1591
#pragma warning disable CS0162

#if STANDALONE_VSBUILD
using NSS.Blast.Standalone;
    using System.Reflection;
#else
using UnityEngine;
    using Unity.Burst.CompilerServices;
#endif


using Unity.Burst;
using Unity.Mathematics;

using V1 = System.Single;
using V2 = Unity.Mathematics.float2;
using V3 = Unity.Mathematics.float3;
using V4 = Unity.Mathematics.float4;

namespace NSS.Blast.Interpretor
{
    unsafe public partial struct BlastInterpretor
    {


        /// <summary>
        /// 
        /// </summary>
        void CALL_FN_RANDOM(ref int code_pointer, ref byte vector_size, ref BlastVariableDataType datatype, ref V4 f4)
        {
            // get vector size from next byte  
            code_pointer++;
            byte c = code[code_pointer];

            c = BlastInterpretor.decode62(in c, ref vector_size);
            code_pointer++;

            // for random, c specifies range:
            // 0 = 0..1  == default
            // 2 = 2 ops with values after this op    min..max 
            // 1 = 1 ops   0...max 
            bool isneg1;
            byte size1;

            switch ((BlastVectorSizes)vector_size)
            {
                case BlastVectorSizes.float1:
                    switch (c)
                    {
                        default:
                            {
#if DEVELOPMENT_BUILD || TRACE
                                goto NOT_SUPPORTED;
#endif
                                break;
                            }

                        // 0 params -> output always float 
                        case 0: datatype = BlastVariableDataType.Numeric; f4.x = random.NextFloat(); break;

                        // n params -> outputtype depends on inputyype
                        case 1:
                            {
                                void* p = pop_p_info(ref code_pointer, out datatype, out size1, out isneg1);

#if TRACE
                                if (size1 != vector_size) goto VECTOR_SIZE_MISMATCH;
#endif 

                                if (datatype == BlastVariableDataType.Numeric)
                                {
                                    f4.x = ((float*)p)[0];
                                    f4.x = random.NextFloat(math.select(f4.x, -f4.x, isneg1)); break;
                                }
                                else
                                {
                                    int i = ((int*)p)[0]; 
                                    f4.x = CodeUtils.ReInterpret<int, float>(random.NextInt(math.select(i, -i, isneg1))); break;
                                }
                            }
                            break;

                        case 2:
                            {
                                void* p = pop_p_info(ref code_pointer, out datatype, out size1, out isneg1);

#if TRACE
                                if (size1 != vector_size) goto VECTOR_SIZE_MISMATCH;
#endif 

                                if (datatype == BlastVariableDataType.Numeric)
                                {
                                    f4.x = ((float*)p)[0];
                                    f4.x= random.NextFloat(math.select(f4.x, -f4.x, isneg1), pop_as_f1(ref code_pointer)); break;
                                }
                                else
                                {
                                    int i = ((int*)p)[0];
                                    f4.x = CodeUtils.ReInterpret<int, float>(random.NextInt(math.select(i, -i, isneg1), pop_as_i1(ref code_pointer))); break;
                                }
                            }
                            break;
                    }
                    vector_size = 1;
                    break;

                case BlastVectorSizes.float2:
                    switch (c)
                    {
                        default:
                            {
#if DEVELOPMENT_BUILD || TRACE
                                goto NOT_SUPPORTED;
#endif
                                break;
                            }

                        // 0 params -> output always float 
                        case 0: datatype = BlastVariableDataType.Numeric; f4.xy = random.NextFloat2(); break;

                        // n params -> outputtype depends on inputyype
                        case 1:
                            {
                                void* p = pop_p_info(ref code_pointer, out datatype, out size1, out isneg1);

#if TRACE
                                if (size1 != vector_size) goto VECTOR_SIZE_MISMATCH;
#endif 

                                if (datatype == BlastVariableDataType.Numeric)
                                {
                                    f4.xy = ((float2*)p)[0];
                                    f4.xy = random.NextFloat2(math.select(f4.xy, -f4.xy, isneg1)); break;
                                }
                                else
                                {
                                    int2 i2 = ((int2*)p)[0];
                                    f4.xy = CodeUtils.ReInterpret<int2, float2>(random.NextInt2(math.select(i2, -i2, isneg1))); break;
                                }
                            }
                            break;

                        case 2:
                            {
                                void* p = pop_p_info(ref code_pointer, out datatype, out size1, out isneg1);
#if TRACE
                                if (size1 != vector_size) goto VECTOR_SIZE_MISMATCH;
#endif 

                                if (datatype == BlastVariableDataType.Numeric)
                                {
                                    f4.xy = ((float2*)p)[0];
                                    f4.xy = random.NextFloat2(math.select(f4.xy, -f4.xy, isneg1), pop_as_f2(ref code_pointer)); break;
                                }
                                else
                                {
                                    int2 i2 = ((int2*)p)[0];
                                    f4.xy = CodeUtils.ReInterpret<int2, float2>(random.NextInt2(math.select(i2, -i2, isneg1), pop_as_i2(ref code_pointer))); break;
                                }
                            }
                            break;
                    }
                    vector_size = 2;
                    break;


                case BlastVectorSizes.float3:
                    switch (c)
                    {
                        default:
                            {
#if DEVELOPMENT_BUILD || TRACE
                                goto NOT_SUPPORTED;
#endif
                                break;
                            }

                        // 0 params -> output always float 
                        case 0: datatype = BlastVariableDataType.Numeric; f4.xyz = random.NextFloat3(); break;

                        // n params -> outputtype depends on inputyype
                        case 1:
                            {
                                void* p = pop_p_info(ref code_pointer, out datatype, out size1, out isneg1);

#if TRACE
                                if (size1 != vector_size) goto VECTOR_SIZE_MISMATCH;
#endif 

                                if (datatype == BlastVariableDataType.Numeric)
                                {
                                    f4.xyz = ((float3*)p)[0];
                                    f4.xyz = random.NextFloat3(math.select(f4.xyz, -f4.xyz, isneg1)); break;
                                }
                                else
                                {
                                    int3 i3 = ((int3*)p)[0];
                                    f4.xyz = CodeUtils.ReInterpret<int3, float3>(random.NextInt3(math.select(i3, -i3, isneg1))); break;
                                }
                            }
                            break;

                        case 2:
                            {
                                void* p = pop_p_info(ref code_pointer, out datatype, out size1, out isneg1);
#if TRACE
                                if (size1 != vector_size) goto VECTOR_SIZE_MISMATCH;
#endif 

                                if (datatype == BlastVariableDataType.Numeric)
                                {
                                    f4.xyz = ((float3*)p)[0];
                                    f4.xyz = random.NextFloat3(math.select(f4.xyz, -f4.xyz, isneg1), pop_as_f3(ref code_pointer)); break;
                                }
                                else
                                {
                                    int3 i3 = ((int3*)p)[0];
                                    f4.xyz = CodeUtils.ReInterpret<int3, float3>(random.NextInt3(math.select(i3, -i3, isneg1), pop_as_i3(ref code_pointer))); break;
                                }
                            }
                            break;
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
                                goto NOT_SUPPORTED;
#endif
                                break;
                            }

                        // 0 params -> output always float 
                        case 0: datatype = BlastVariableDataType.Numeric; f4.xy = random.NextFloat2(); break;

                        case 1:
                            {
                                void* p = pop_p_info(ref code_pointer, out datatype, out size1, out isneg1);

#if TRACE
                                if (size1 != vector_size) goto VECTOR_SIZE_MISMATCH;
#endif 

                                if (datatype == BlastVariableDataType.Numeric)
                                {
                                    f4 = ((float4*)p)[0];
                                    f4 = random.NextFloat4(math.select(f4, -f4, isneg1)); break;
                                }
                                else
                                {
                                    int4 i4 = ((int4*)p)[0];
                                    f4 = CodeUtils.ReInterpret<int4, float4>(random.NextInt4(math.select(i4, -i4, isneg1))); break;
                                }
                            }
                            break;

                        case 2:
                            {
                                void* p = pop_p_info(ref code_pointer, out datatype, out size1, out isneg1);
#if TRACE
                                if (size1 != vector_size) goto VECTOR_SIZE_MISMATCH;
#endif 

                                if (datatype == BlastVariableDataType.Numeric)
                                {
                                    f4 = ((float4*)p)[0];
                                    f4 = random.NextFloat4(math.select(f4, -f4, isneg1), pop_as_f4(ref code_pointer)); break;
                                }
                                else
                                {
                                    int4 i4 = ((int4*)p)[0];
                                    f4 = CodeUtils.ReInterpret<int4, float4>(random.NextInt4(math.select(i4, -i4, isneg1), pop_as_i4(ref code_pointer))); break;
                                }
                            }
                            break;
                    }
                    vector_size = 4;
                    break;

                default:
                    {
#if DEVELOPMENT_BUILD || TRACE
                        goto NOT_SUPPORTED;
#endif
                        break;
                    }
            }

            return;
#if TRACE         
        NOT_SUPPORTED:
            Debug.LogError($"Blast.Interpretor.Random: parametercount {c} or vectorsize '{vector_size}' not supported ");
            return;

        VECTOR_SIZE_MISMATCH:
            Debug.LogError($"Blast.Interpretor.Random: vectorsize mismatch, expected {vector_size} but found {size1} at codepointer {code_pointer}");
            return;
#endif
        }








    }
}
