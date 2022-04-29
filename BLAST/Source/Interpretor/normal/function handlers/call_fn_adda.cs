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

using V1 = System.Single;
using V2 = Unity.Mathematics.float2;
using V3 = Unity.Mathematics.float3;
using V4 = Unity.Mathematics.float4;

namespace NSS.Blast.Interpretor
{
    unsafe public partial struct BlastInterpretor
    {
        /// <summary>
        /// add all inputs together 
        /// - n elements of equal vector size 
        /// </summary>
        void CALL_FN_ADDA(ref int code_pointer, ref byte vector_size, ref BlastVariableDataType datatype, ref V4 result)
        {
            code_pointer = code_pointer + 1;
            byte c = code[code_pointer];

            // decode 62
            c = BlastInterpretor.decode62(in c, ref vector_size);
            code_pointer++;

            byte item_vector_size;
            bool is_negated;

            void* first = pop_p_info(ref code_pointer, out datatype, out item_vector_size, out is_negated);

#if TRACE
            if (item_vector_size != vector_size)
            {
                Debug.LogError($"Blast.Interpretor.adda: vectorsize mismatch at {code_pointer}, function adda is encoded for size {vector_size} but one of the parameters is size {item_vector_size}");
                return;
            }
#endif

            // use type from first parameter
            switch (datatype.Combine(vector_size, false))
            {
                case BlastVectorType.int1:
                    {
                        int i1 = ((int*)first)[0];
                        i1 = math.select(i1, -i1, is_negated);

                        for (int i = 2; i <= c; i++)
                        {
                            i1 += pop_as_i1(ref code_pointer);
                        }
                        result.x = CodeUtils.ReInterpret<int, float>(i1);
                    }
                    break;

                case BlastVectorType.int2:
                    {
                        int2 i2 = ((int2*)first)[0];
                        i2 = math.select(i2, -i2, is_negated);

                        for (int i = 2; i <= c; i++)
                        {
                            i2 += pop_as_i2(ref code_pointer);
                        }
                        result.xy = CodeUtils.ReInterpret<int2, float2>(i2);
                    }
                    break;

                case BlastVectorType.int3:
                    {
                        int3 i3 = ((int3*)first)[0];
                        i3 = math.select(i3, -i3, is_negated);

                        for (int i = 2; i <= c; i++)
                        {
                            i3 += pop_as_i3(ref code_pointer);
                        }
                        result.xyz = CodeUtils.ReInterpret<int3, float3>(i3);
                    }
                    break;

                case BlastVectorType.int4:
                    {
                        int4 i4 = ((int4*)first)[0];
                        i4 = math.select(i4, -i4, is_negated);

                        for (int i = 2; i <= c; i++)
                        {
                            i4 += pop_as_i4(ref code_pointer);
                        }
                        result = CodeUtils.ReInterpret<int4, float4>(i4);
                    }
                    break;

                case BlastVectorType.float1:
                    {
                        float f = ((float*)first)[0];
                        f = math.select(f, -f, is_negated);

                        for (int i = 2; i <= c; i++)
                        {
                            f += pop_as_f1(ref code_pointer);
                        }
                        result.x = f;
                    }
                    break;

                case BlastVectorType.float2:
                    {
                        float2 f2 = ((float2*)first)[0];
                        f2 = math.select(f2, -f2, is_negated);
                        for (int i = 2; i <= c; i++)
                        {
                            f2 += pop_as_f2(ref code_pointer);
                        }
                        result.xy = f2;
                    }
                    break;

                case BlastVectorType.float3:
                    {
                        float3 f3 = ((float3*)first)[0];
                        f3 = math.select(f3, -f3, is_negated);
                        for (int i = 2; i <= c; i++)
                        {
                            f3 += pop_as_f3(ref code_pointer);
                        }
                        result.xyz = f3;
                    }
                    break;

                case BlastVectorType.float4:
                    {
                        result = ((float4*)first)[0];
                        result = math.select(result, -result, is_negated);
                        for (int i = 2; i <= c; i++)
                        {
                            result += pop_as_f4(ref code_pointer);
                        }
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

        /// <summary>
        /// 
        /// </summary>
        void CALL_FN_SUBA(ref int code_pointer, ref byte vector_size, ref BlastVariableDataType datatype, ref V4 result)
        {
            code_pointer = code_pointer + 1;
            byte c = code[code_pointer];

            // decode 62
            c = BlastInterpretor.decode62(in c, ref vector_size);
            code_pointer++;

            byte item_vector_size;
            bool is_negated;

            void* first = pop_p_info(ref code_pointer, out datatype, out item_vector_size, out is_negated);

#if TRACE
            if (item_vector_size != vector_size)
            {
                Debug.LogError($"Blast.Interpretor.suba: vectorsize mismatch at {code_pointer}, function adda is encoded for size {vector_size} but one of the parameters is size {item_vector_size}");
                return;
            }
#endif

            // use type from first parameter
            switch (datatype.Combine(vector_size, false))
            {
                case BlastVectorType.int1:
                    {
                        int i1 = ((int*)first)[0];
                        i1 = math.select(i1, -i1, is_negated);

                        for (int i = 2; i <= c; i++)
                        {
                            i1 -= pop_as_i1(ref code_pointer);
                        }
                        result.x = CodeUtils.ReInterpret<int, float>(i1);
                    }
                    break;

                case BlastVectorType.int2:
                    {
                        int2 i2 = ((int2*)first)[0];
                        i2 = math.select(i2, -i2, is_negated);

                        for (int i = 2; i <= c; i++)
                        {
                            i2 -= pop_as_i2(ref code_pointer);
                        }
                        result.xy = CodeUtils.ReInterpret<int2, float2>(i2);
                    }
                    break;

                case BlastVectorType.int3:
                    {
                        int3 i3 = ((int3*)first)[0];
                        i3 = math.select(i3, -i3, is_negated);

                        for (int i = 2; i <= c; i++)
                        {
                            i3 -= pop_as_i3(ref code_pointer);
                        }
                        result.xyz = CodeUtils.ReInterpret<int3, float3>(i3);
                    }
                    break;

                case BlastVectorType.int4:
                    {
                        int4 i4 = ((int4*)first)[0];
                        i4 = math.select(i4, -i4, is_negated);

                        for (int i = 2; i <= c; i++)
                        {
                            i4 -= pop_as_i4(ref code_pointer);
                        }
                        result = CodeUtils.ReInterpret<int4, float4>(i4);
                    }
                    break;

                case BlastVectorType.float1:
                    {
                        float f = ((float*)first)[0];
                        f = math.select(f, -f, is_negated);

                        for (int i = 2; i <= c; i++)
                        {
                            f -= pop_as_f1(ref code_pointer);
                        }
                        result.x = f;
                    }
                    break;

                case BlastVectorType.float2:
                    {
                        float2 f2 = ((float2*)first)[0];
                        f2 = math.select(f2, -f2, is_negated);
                        for (int i = 2; i <= c; i++)
                        {
                            f2 -= pop_as_f2(ref code_pointer);
                        }
                        result.xy = f2;
                    }
                    break;

                case BlastVectorType.float3:
                    {
                        float3 f3 = ((float3*)first)[0];
                        f3 = math.select(f3, -f3, is_negated);
                        for (int i = 2; i <= c; i++)
                        {
                            f3 -= pop_as_f3(ref code_pointer);
                        }
                        result.xyz = f3;
                    }
                    break;

                case BlastVectorType.float4:
                    {
                        result = ((float4*)first)[0];
                        result = math.select(result, -result, is_negated);
                        for (int i = 2; i <= c; i++)
                        {
                            result -= pop_as_f4(ref code_pointer);
                        }
                        vector_size = 4;
                    }
                    break;

                default:
                    {
#if DEVELOPMENT_BUILD || TRACE
                        Debug.LogError($"get_suba_result: vector size '{vector_size}' not allowed");
#endif
                        result = float.NaN;

                    }
                    break;

            }
        }

        /// <summary>
        /// 
        /// </summary>
        void CALL_FN_DIVA(ref int code_pointer, ref byte vector_size, ref BlastVariableDataType datatype, ref V4 result)
        {
            code_pointer = code_pointer + 1;
            byte c = code[code_pointer];

            // decode 62
            c = BlastInterpretor.decode62(in c, ref vector_size);
            code_pointer++;

            byte item_vector_size;
            bool is_negated;

            void* first = pop_p_info(ref code_pointer, out datatype, out item_vector_size, out is_negated);

#if TRACE
            if (item_vector_size != vector_size)
            {
                Debug.LogError($"Blast.Interpretor.diva: vectorsize mismatch at {code_pointer}, function adda is encoded for size {vector_size} but one of the parameters is size {item_vector_size}");
                return;
            }
#endif

            // use type from first parameter
            switch (datatype.Combine(vector_size, false))
            {
                case BlastVectorType.int1:
                    {
                        int i1 = ((int*)first)[0];
                        i1 = math.select(i1, -i1, is_negated);

                        for (int i = 2; i <= c; i++)
                        {
                            i1 /= pop_as_i1(ref code_pointer);
                        }
                        result.x = CodeUtils.ReInterpret<int, float>(i1);
                    }
                    break;

                case BlastVectorType.int2:
                    {
                        int2 i2 = ((int2*)first)[0];
                        i2 = math.select(i2, -i2, is_negated);

                        for (int i = 2; i <= c; i++)
                        {
                            i2 /= pop_as_i2(ref code_pointer);
                        }
                        result.xy = CodeUtils.ReInterpret<int2, float2>(i2);
                    }
                    break;

                case BlastVectorType.int3:
                    {
                        int3 i3 = ((int3*)first)[0];
                        i3 = math.select(i3, -i3, is_negated);

                        for (int i = 2; i <= c; i++)
                        {
                            i3 /= pop_as_i3(ref code_pointer);
                        }
                        result.xyz = CodeUtils.ReInterpret<int3, float3>(i3);
                    }
                    break;

                case BlastVectorType.int4:
                    {
                        int4 i4 = ((int4*)first)[0];
                        i4 = math.select(i4, -i4, is_negated);

                        for (int i = 2; i <= c; i++)
                        {
                            i4 /= pop_as_i4(ref code_pointer);
                        }
                        result = CodeUtils.ReInterpret<int4, float4>(i4);
                    }
                    break;

                case BlastVectorType.float1:
                    {
                        float f = ((float*)first)[0];
                        f = math.select(f, -f, is_negated);

                        for (int i = 2; i <= c; i++)
                        {
                            f /= pop_as_f1(ref code_pointer);
                        }
                        result.x = f;
                    }
                    break;

                case BlastVectorType.float2:
                    {
                        float2 f2 = ((float2*)first)[0];
                        f2 = math.select(f2, -f2, is_negated);
                        for (int i = 2; i <= c; i++)
                        {
                            f2 /= pop_as_f2(ref code_pointer);
                        }
                        result.xy = f2;
                    }
                    break;

                case BlastVectorType.float3:
                    {
                        float3 f3 = ((float3*)first)[0];
                        f3 = math.select(f3, -f3, is_negated);
                        for (int i = 2; i <= c; i++)
                        {
                            f3 /= pop_as_f3(ref code_pointer);
                        }
                        result.xyz = f3;
                    }
                    break;

                case BlastVectorType.float4:
                    {
                        result = ((float4*)first)[0];
                        result = math.select(result, -result, is_negated);
                        for (int i = 2; i <= c; i++)
                        {
                            result /= pop_as_f4(ref code_pointer);
                        }
                        vector_size = 4;
                    }
                    break;

                default:
                    {
#if DEVELOPMENT_BUILD || TRACE
                        Debug.LogError($"get_diva_result: vector size '{vector_size}' not allowed");
#endif
                        result = float.NaN;

                    }
                    break;
            }
        }

        /// <summary>
        /// multiply all inputs together
        /// - equal sized vectors 
        /// - n elements 
        /// </summary>
        void CALL_FN_MULA(ref int code_pointer, ref byte vector_size, ref BlastVariableDataType datatype, ref V4 result)
        {
            code_pointer++;
            byte c = code[code_pointer];

            c = BlastInterpretor.decode62(in c, ref vector_size);
            vector_size = (byte)math.select(vector_size, 4, vector_size == 0);
                        
            code_pointer++;
            result = float.NaN;

            Unity.Burst.Intrinsics.v128 v1 = new Unity.Burst.Intrinsics.v128();
            Unity.Burst.Intrinsics.v128 v2 = new Unity.Burst.Intrinsics.v128();

            // get the first parameter 
            byte item_vector_size;
            bool is_negated;

            void* first = pop_p_info(ref code_pointer, out datatype, out item_vector_size, out is_negated);

#if TRACE
            if (item_vector_size != vector_size)
            {
                Debug.LogError($"Blast.Interpretor.mula: vectorsize mismatch at {code_pointer}, function mula is encoded for size {vector_size} but one of the parameters is size {item_vector_size}");
                return;
            }
#endif
           
            switch (datatype.Combine(vector_size, false))
            {
                case BlastVectorType.int1:
                    int i1 = ((int*)first)[0];
                    for (int i = 2; i <= c; i++)
                    {
                        i1 = i1 * pop_as_i1(ref code_pointer);
                    }
                    result.x = CodeUtils.ReInterpret<int, float>(i1); 
                    break;

                case BlastVectorType.int2:
                    int2 i2 = ((int2*)first)[0];
                    for (int i = 2; i <= c; i++)
                    {
                        i2 = i2 * pop_as_i2(ref code_pointer);
                    }
                    result.xy = CodeUtils.ReInterpret<int2, float2>(i2);
                    break;

                case BlastVectorType.int3:
                    int3 i3 = ((int3*)first)[0];
                    for (int i = 2; i <= c; i++)
                    {
                        i3 = i3 * pop_as_i3(ref code_pointer);
                    }
                    result.xyz = CodeUtils.ReInterpret<int3, float3>(i3);
                    break;

                case BlastVectorType.int4:
                    int4 i4 = ((int4*)first)[0];
                    for (int i = 2; i <= c; i++)
                    {
                        i4 = i4 * pop_as_i4(ref code_pointer);
                    }
                    result = CodeUtils.ReInterpret<int4, float4>(i4);
                    break;

                case BlastVectorType.float1:
                    {
                        switch (c)
                        {
                            case 1:  // should probably just raise a error on this and case 2 
                                result.x = ((float*)first)[0];
                                result.x = math.select(result.x, -result.x, is_negated); 
                                break;

                            case 2:
                                result.x = ((float*)first)[0];
                                result.x = math.select(result.x, -result.x, is_negated);
                                result.x *= pop_as_f1(ref code_pointer);
                                break;

                            case 3:
                                result.x = ((float*)first)[0];
                                result.x = math.select(result.x, -result.x, is_negated);
                                result.x *= pop_as_f1(ref code_pointer);
                                result.x *= pop_as_f1(ref code_pointer);
                                break;

                            case 4:
                                v1.Float0 = ((float*)first)[0];
                                v1.Float0 = math.select(v1.Float0, -v1.Float0, is_negated);
                                v1.Float1 = pop_as_f1(ref code_pointer);
                                v2.Float0 = pop_as_f1(ref code_pointer);
                                v2.Float1 = pop_as_f1(ref code_pointer);
                                v1 = Unity.Burst.Intrinsics.X86.Sse.mul_ps(v1, v2);
                                result.x = v1.Float0 * v1.Float1;
                                break;

                            case 5:
                                v1.Float0 = ((float*)first)[0];
                                v1.Float0 = math.select(v1.Float0, -v1.Float0, is_negated);
                                v1.Float1 = pop_as_f1(ref code_pointer);
                                v2.Float0 = pop_as_f1(ref code_pointer);
                                v2.Float1 = pop_as_f1(ref code_pointer);
                                v1 = Unity.Burst.Intrinsics.X86.Sse.mul_ps(v1, v2);

                                result.x = v1.Float0 * v1.Float1 * pop_as_f1(ref code_pointer);
                                break;

                            case 6:
                                v1.Float0 = ((float*)first)[0];
                                v1.Float0 = math.select(v1.Float0, -v1.Float0, is_negated);
                                v1.Float1 = pop_as_f1(ref code_pointer);
                                v1.Float2 = pop_as_f1(ref code_pointer);
                                v2.Float0 = pop_as_f1(ref code_pointer);
                                v2.Float1 = pop_as_f1(ref code_pointer);
                                v2.Float2 = pop_as_f1(ref code_pointer);

                                v1 = Unity.Burst.Intrinsics.X86.Sse.mul_ps(v1, v2);

                                result.x = v1.Float0 * v1.Float1 * v1.Float2;
                                break;

                            case 7:
                                v1.Float0 = ((float*)first)[0];
                                v1.Float0 = math.select(v1.Float0, -v1.Float0, is_negated);
                                v1.Float1 = pop_as_f1(ref code_pointer);
                                v1.Float2 = pop_as_f1(ref code_pointer);
                                v2.Float0 = pop_as_f1(ref code_pointer);
                                v2.Float1 = pop_as_f1(ref code_pointer);
                                v2.Float2 = pop_as_f1(ref code_pointer);

                                v1 = Unity.Burst.Intrinsics.X86.Sse.mul_ps(v1, v2);
                                result.x = v1.Float0 * v1.Float1 * v1.Float2 * pop_as_f1(ref code_pointer);
                                break;

                            case 8:
                                v1.Float0 = ((float*)first)[0];
                                v1.Float0 = math.select(v1.Float0, -v1.Float0, is_negated);
                                v1.Float1 = pop_as_f1(ref code_pointer);
                                v1.Float2 = pop_as_f1(ref code_pointer);
                                v1.Float3 = pop_as_f1(ref code_pointer);
                                v2.Float0 = pop_as_f1(ref code_pointer);
                                v2.Float1 = pop_as_f1(ref code_pointer);
                                v2.Float2 = pop_as_f1(ref code_pointer);
                                v2.Float3 = pop_as_f1(ref code_pointer);

                                v1 = Unity.Burst.Intrinsics.X86.Sse.mul_ps(v1, v2);

                                result.x = v1.Float0 * v1.Float1 * v1.Float2 * v1.Float3;
                                break;

                            case 9:
                                v1.Float0 = ((float*)first)[0];
                                v1.Float0 = math.select(v1.Float0, -v1.Float0, is_negated);
                                v1.Float1 = pop_as_f1(ref code_pointer);
                                v1.Float2 = pop_as_f1(ref code_pointer);
                                v1.Float3 = pop_as_f1(ref code_pointer);
                                v2.Float0 = pop_as_f1(ref code_pointer);
                                v2.Float1 = pop_as_f1(ref code_pointer);
                                v2.Float2 = pop_as_f1(ref code_pointer);
                                v2.Float3 = pop_as_f1(ref code_pointer);

                                v1 = Unity.Burst.Intrinsics.X86.Sse.mul_ps(v1, v2);

                                result.x = v1.Float0 * v1.Float1 * v1.Float2 * v1.Float3 * pop_as_f1(ref code_pointer);
                                break;

                            case 10:
                                v1.Float0 = ((float*)first)[0];
                                v1.Float0 = math.select(v1.Float0, -v1.Float0, is_negated);
                                v1.Float1 = pop_as_f1(ref code_pointer);
                                v1.Float2 = pop_as_f1(ref code_pointer);
                                v1.Float3 = pop_as_f1(ref code_pointer);
                                v2.Float0 = pop_as_f1(ref code_pointer);
                                v2.Float1 = pop_as_f1(ref code_pointer);
                                v2.Float2 = pop_as_f1(ref code_pointer);
                                v2.Float3 = pop_as_f1(ref code_pointer);

                                v1 = Unity.Burst.Intrinsics.X86.Sse.mul_ps(v1, v2);

                                v2.Float0 = pop_as_f1(ref code_pointer);
                                v2.Float1 = pop_as_f1(ref code_pointer);
                                v2.Float2 = v1.Float3; // save multiplication by v1.float3 later
                                v2.Float3 = 1;

                                v1 = Unity.Burst.Intrinsics.X86.Sse.mul_ps(v1, v2);

                                result.x = v1.Float0 * v1.Float1 * v1.Float2;
                                break;

                            case 11:
                                v1.Float0 = ((float*)first)[0];
                                v1.Float0 = math.select(v1.Float0, -v1.Float0, is_negated);
                                v1.Float1 = pop_as_f1(ref code_pointer);
                                v1.Float2 = pop_as_f1(ref code_pointer);
                                v1.Float3 = pop_as_f1(ref code_pointer);
                                v2.Float0 = pop_as_f1(ref code_pointer);
                                v2.Float1 = pop_as_f1(ref code_pointer);
                                v2.Float2 = pop_as_f1(ref code_pointer);
                                v2.Float3 = pop_as_f1(ref code_pointer);

                                v1 = Unity.Burst.Intrinsics.X86.Sse.mul_ps(v1, v2);

                                v2.Float0 = pop_as_f1(ref code_pointer);
                                v2.Float1 = pop_as_f1(ref code_pointer);
                                v2.Float2 = pop_as_f1(ref code_pointer);
                                v2.Float3 = 1;

                                v1 = Unity.Burst.Intrinsics.X86.Sse.mul_ps(v1, v2);

                                result.x = v1.Float0 * v1.Float1 * v1.Float2 * v1.Float3;
                                break;

                            case 12:
                                v1.Float0 = ((float*)first)[0];
                                v1.Float0 = math.select(v1.Float0, -v1.Float0, is_negated);
                                v1.Float1 = pop_as_f1(ref code_pointer);
                                v1.Float2 = pop_as_f1(ref code_pointer);
                                v1.Float3 = pop_as_f1(ref code_pointer);
                                v2.Float0 = pop_as_f1(ref code_pointer);
                                v2.Float1 = pop_as_f1(ref code_pointer);
                                v2.Float2 = pop_as_f1(ref code_pointer);
                                v2.Float3 = pop_as_f1(ref code_pointer);

                                v1 = Unity.Burst.Intrinsics.X86.Sse.mul_ps(v1, v2);

                                v2.Float0 = pop_as_f1(ref code_pointer);
                                v2.Float1 = pop_as_f1(ref code_pointer);
                                v2.Float2 = pop_as_f1(ref code_pointer);
                                v2.Float3 = pop_as_f1(ref code_pointer);

                                v1 = Unity.Burst.Intrinsics.X86.Sse.mul_ps(v1, v2);

                                result.x = v1.Float0 * v1.Float1 * v1.Float2 * v1.Float3;
                                break;

                            case 13:
                                v1.Float0 = ((float*)first)[0];
                                v1.Float0 = math.select(v1.Float0, -v1.Float0, is_negated);
                                v1.Float1 = pop_as_f1(ref code_pointer);
                                v1.Float2 = pop_as_f1(ref code_pointer);
                                v1.Float3 = pop_as_f1(ref code_pointer);
                                v2.Float0 = pop_as_f1(ref code_pointer);
                                v2.Float1 = pop_as_f1(ref code_pointer);
                                v2.Float2 = pop_as_f1(ref code_pointer);
                                v2.Float3 = pop_as_f1(ref code_pointer);

                                v1 = Unity.Burst.Intrinsics.X86.Sse.mul_ps(v1, v2);

                                v2.Float0 = pop_as_f1(ref code_pointer);
                                v2.Float1 = pop_as_f1(ref code_pointer);
                                v2.Float2 = pop_as_f1(ref code_pointer);
                                v2.Float3 = pop_as_f1(ref code_pointer);

                                v1 = Unity.Burst.Intrinsics.X86.Sse.mul_ps(v1, v2);

                                result.x = v1.Float0 * v1.Float1 * v1.Float2 * v1.Float3 * pop_as_f1(ref code_pointer);
                                break;

                            case 14:
                                v1.Float0 = ((float*)first)[0];
                                v1.Float0 = math.select(v1.Float0, -v1.Float0, is_negated);
                                v1.Float1 = pop_as_f1(ref code_pointer);
                                v1.Float2 = pop_as_f1(ref code_pointer);
                                v1.Float3 = pop_as_f1(ref code_pointer);
                                v2.Float0 = pop_as_f1(ref code_pointer);
                                v2.Float1 = pop_as_f1(ref code_pointer);
                                v2.Float2 = pop_as_f1(ref code_pointer);
                                v2.Float3 = pop_as_f1(ref code_pointer);

                                v1 = Unity.Burst.Intrinsics.X86.Sse.mul_ps(v1, v2);

                                v2.Float0 = pop_as_f1(ref code_pointer);
                                v2.Float1 = pop_as_f1(ref code_pointer);
                                v2.Float2 = pop_as_f1(ref code_pointer);
                                v2.Float3 = pop_as_f1(ref code_pointer);

                                v1 = Unity.Burst.Intrinsics.X86.Sse.mul_ps(v1, v2);

                                v2.Float0 = pop_as_f1(ref code_pointer);
                                v2.Float1 = pop_as_f1(ref code_pointer);
                                v2.Float2 = v1.Float3; // save multiplication by v1.float3 later
                                v2.Float3 = 1;

                                v1 = Unity.Burst.Intrinsics.X86.Sse.mul_ps(v1, v2);

                                result.x = v1.Float0 * v1.Float1 * v1.Float2;
                                break;

                            case 15:
                                v1.Float0 = ((float*)first)[0];
                                v1.Float0 = math.select(v1.Float0, -v1.Float0, is_negated);
                                v1.Float1 = pop_as_f1(ref code_pointer);
                                v1.Float2 = pop_as_f1(ref code_pointer);
                                v1.Float3 = pop_as_f1(ref code_pointer);
                                v2.Float0 = pop_as_f1(ref code_pointer);
                                v2.Float1 = pop_as_f1(ref code_pointer);
                                v2.Float2 = pop_as_f1(ref code_pointer);
                                v2.Float3 = pop_as_f1(ref code_pointer);

                                v1 = Unity.Burst.Intrinsics.X86.Sse.mul_ps(v1, v2);

                                v2.Float0 = pop_as_f1(ref code_pointer);
                                v2.Float1 = pop_as_f1(ref code_pointer);
                                v2.Float2 = pop_as_f1(ref code_pointer);
                                v2.Float3 = pop_as_f1(ref code_pointer);

                                v1 = Unity.Burst.Intrinsics.X86.Sse.mul_ps(v1, v2);

                                v2.Float0 = pop_as_f1(ref code_pointer);
                                v2.Float1 = pop_as_f1(ref code_pointer);
                                v2.Float2 = pop_as_f1(ref code_pointer);
                                v2.Float3 = 1;

                                v1 = Unity.Burst.Intrinsics.X86.Sse.mul_ps(v1, v2);

                                result.x = v1.Float0 * v1.Float1 * v1.Float2 * v1.Float3;
                                break;

                            case 16:
                                v1.Float0 = ((float*)first)[0];
                                v1.Float0 = math.select(v1.Float0, -v1.Float0, is_negated);
                                v1.Float1 = pop_as_f1(ref code_pointer);
                                v1.Float2 = pop_as_f1(ref code_pointer);
                                v1.Float3 = pop_as_f1(ref code_pointer);
                                v2.Float0 = pop_as_f1(ref code_pointer);
                                v2.Float1 = pop_as_f1(ref code_pointer);
                                v2.Float2 = pop_as_f1(ref code_pointer);
                                v2.Float3 = pop_as_f1(ref code_pointer);

                                v1 = Unity.Burst.Intrinsics.X86.Sse.mul_ps(v1, v2);

                                v2.Float0 = pop_as_f1(ref code_pointer);
                                v2.Float1 = pop_as_f1(ref code_pointer);
                                v2.Float2 = pop_as_f1(ref code_pointer);
                                v2.Float3 = pop_as_f1(ref code_pointer);

                                v1 = Unity.Burst.Intrinsics.X86.Sse.mul_ps(v1, v2);

                                v2.Float0 = pop_as_f1(ref code_pointer);
                                v2.Float1 = pop_as_f1(ref code_pointer);
                                v2.Float2 = pop_as_f1(ref code_pointer);
                                v2.Float3 = pop_as_f1(ref code_pointer);

                                v1 = Unity.Burst.Intrinsics.X86.Sse.mul_ps(v1, v2);

                                result.x = v1.Float0 * v1.Float1 * v1.Float2 * v1.Float3;
                                break;

                            // any other size run manually as 1 float
                            default:
                                {
                                    float r1= ((float*)first)[0];
                                    r1 = math.select(r1, -r1, is_negated);

                                    for (int i = 1; i < c; i++)
                                    {
                                        r1 *= pop_as_f1(ref code_pointer);
                                    }

                                    result = new float4(r1, 0, 0, 0);
                                }
                                break;
                        }
                    }
                    break;

                case BlastVectorType.float2:
                    {
                        result.xy = ((float2*)first)[0];
                        for (int i = 2; i <= c; i++)
                        {
                            result.xy = result.xy * pop_as_f2(ref code_pointer);
                        }
                    }
                    break;

                case BlastVectorType.float3:
                    {
                        result.xyz = ((float3*)first)[0];
                        for (int i = 2; i <= c; i++)
                        {
                            result.xyz = result.xyz * pop_as_f3(ref code_pointer);
                        }
                    }
                    break;

                case BlastVectorType.float4:
                    {
                        result = ((float4*)first)[0];
                        for (int i = 2; i <= c; i++)
                        {
                            result = result * pop_as_f4(ref code_pointer);
                        }
                    }
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
        /// a = any(a b c d), vectorsizes must all be equal, output vectorsize == input vectorsize
        /// </summary>
        void CALL_FN_ANY(ref int code_pointer, ref byte vector_size, ref BlastVariableDataType datatype, ref V4 f)
        {
            code_pointer++;

            BlastVectorSizes vectortype; 
            byte c = BlastInterpretor.decode44(in code[code_pointer], out vectortype);
            code_pointer++;

            // this should compile into a jump table (CHECK THIS !!) todo

            // for any|all etc the result is the same for int|float -> 0 is 0 in bits 
            // so we only need to differrentiate on size 

            switch (vectortype)
            {
                case BlastVectorSizes.bool32:
                    {
                        vector_size = 1; 
                        f = math.select(0, 1, pop_b32(ref code_pointer).Any);
                        for (int i = 1; i < c && f.x == 0; i++)
                        {
                            f.x = math.select(0f, 1f, pop_b32(ref code_pointer).Any);
                        }
                        break;
                    }

                case BlastVectorSizes.float1:
                    {
                        vector_size = 1; 
                        switch (c)
                        {
                            default:
#if DEVELOPMENT_BUILD || TRACE
                                Debug.LogError($"blast.interpretor.get_any_result: parameter count '{c}' not supported for float datatypes -> parser/compiler/optimizer error");
#endif
                                f = float.NaN;
                                break;

                            case 2:
                                f = math.select(0f, 1f, pop_as_boolean(ref code_pointer) || pop_as_boolean(ref code_pointer));
                                break;

                            case 3:
                                f = math.select(0f, 1f, pop_as_boolean(ref code_pointer) || pop_as_boolean(ref code_pointer) || pop_as_boolean(ref code_pointer));
                                break;

                            case 4:
                                f = math.select(0f, 1f, pop_as_boolean(ref code_pointer) || pop_as_boolean(ref code_pointer) || pop_as_boolean(ref code_pointer) || pop_as_boolean(ref code_pointer));
                                break;

                            case 5:
                                f = math.select(0f, 1f, pop_as_boolean(ref code_pointer) || pop_as_boolean(ref code_pointer) || pop_as_boolean(ref code_pointer) || pop_as_boolean(ref code_pointer) || pop_as_boolean(ref code_pointer));
                                break;

                            case 6:
                                f = math.select(0f, 1f, pop_as_boolean(ref code_pointer) || pop_as_boolean(ref code_pointer) || pop_as_boolean(ref code_pointer) || pop_as_boolean(ref code_pointer) || pop_as_boolean(ref code_pointer) || pop_as_boolean(ref code_pointer));
                                break;

                            case 7:
                                f = math.select(0f, 1f, pop_as_boolean(ref code_pointer) || pop_as_boolean(ref code_pointer) || pop_as_boolean(ref code_pointer) || pop_as_boolean(ref code_pointer) || pop_as_boolean(ref code_pointer) || pop_as_boolean(ref code_pointer) || pop_as_boolean(ref code_pointer));
                                break;

                            case 8:
                                f = math.select(0f, 1f, pop_as_boolean(ref code_pointer) || pop_as_boolean(ref code_pointer) || pop_as_boolean(ref code_pointer) || pop_as_boolean(ref code_pointer) || pop_as_boolean(ref code_pointer) || pop_as_boolean(ref code_pointer) || pop_as_boolean(ref code_pointer) || pop_as_boolean(ref code_pointer));
                                break;
                        }
                        break;
                    }

                case BlastVectorSizes.float2:
                    {
                        vector_size = 2;
                        switch (c)
                        {
                            default:
#if DEVELOPMENT_BUILD || TRACE
                                Debug.LogError($"blast.interpretor.get_any_result: parameter count '{c}' not supported for float datatypes -> parser/compiler/optimizer error");
#endif
                                f = float.NaN;
                                break;

                            case 2:
                                f = math.select(0f, 1f, math.any(pop_as_boolean2(ref code_pointer)) || math.any(pop_as_boolean2(ref code_pointer)));
                                break;

                            case 3:
                                f = math.select(0f, 1f, math.any(pop_as_boolean2(ref code_pointer)) || math.any(pop_as_boolean2(ref code_pointer)) || math.any(pop_as_boolean2(ref code_pointer)));
                                break;

                            case 4:
                                f = math.select(0f, 1f, math.any(pop_as_boolean2(ref code_pointer)) || math.any(pop_as_boolean2(ref code_pointer)) || math.any(pop_as_boolean2(ref code_pointer)) || math.any(pop_as_boolean2(ref code_pointer)));
                                break;

                            case 5:
                                f = math.select(0f, 1f, math.any(pop_as_boolean2(ref code_pointer)) || math.any(pop_as_boolean2(ref code_pointer)) || math.any(pop_as_boolean2(ref code_pointer)) || math.any(pop_as_boolean2(ref code_pointer)) || math.any(pop_as_boolean2(ref code_pointer)));
                                break;

                            case 6:
                                f = math.select(0f, 1f, math.any(pop_as_boolean2(ref code_pointer)) || math.any(pop_as_boolean2(ref code_pointer)) || math.any(pop_as_boolean2(ref code_pointer)) || math.any(pop_as_boolean2(ref code_pointer)) || math.any(pop_as_boolean2(ref code_pointer)) || math.any(pop_as_boolean2(ref code_pointer)));
                                break;

                            case 7:
                                f = math.select(0f, 1f, math.any(pop_as_boolean2(ref code_pointer)) || math.any(pop_as_boolean2(ref code_pointer)) || math.any(pop_as_boolean2(ref code_pointer)) || math.any(pop_as_boolean2(ref code_pointer)) || math.any(pop_as_boolean2(ref code_pointer)) || math.any(pop_as_boolean2(ref code_pointer)) || math.any(pop_as_boolean2(ref code_pointer)));
                                break;

                            case 8:
                                f = math.select(0f, 1f, math.any(pop_as_boolean2(ref code_pointer)) || math.any(pop_as_boolean2(ref code_pointer)) || math.any(pop_as_boolean2(ref code_pointer)) || math.any(pop_as_boolean2(ref code_pointer)) || math.any(pop_as_boolean2(ref code_pointer)) || math.any(pop_as_boolean2(ref code_pointer)) || math.any(pop_as_boolean2(ref code_pointer)) || math.any(pop_as_boolean2(ref code_pointer)));
                                break;
                        }
                        break;
                    }

                case BlastVectorSizes.float3:
                    {
                        vector_size = 3;
                        switch (c)
                        {
                            default:
#if DEVELOPMENT_BUILD || TRACE
                                Debug.LogError($"blast.interpretor.get_any_result: parameter count '{c}' not supported -> parser/compiler/optimizer error");
#endif
                                f = float.NaN;
                                break;

                            case 2:
                                f = math.select(0f, 1f, math.any(pop_as_boolean3(ref code_pointer)) || math.any(pop_as_boolean3(ref code_pointer)));
                                break;

                            case 3:
                                f = math.select(0f, 1f, math.any(pop_as_boolean3(ref code_pointer)) || math.any(pop_as_boolean3(ref code_pointer)) || math.any(pop_as_boolean3(ref code_pointer)));
                                break;

                            case 4:
                                f = math.select(0f, 1f, math.any(pop_as_boolean3(ref code_pointer)) || math.any(pop_as_boolean3(ref code_pointer)) || math.any(pop_as_boolean3(ref code_pointer)) || math.any(pop_as_boolean3(ref code_pointer)));
                                break;

                            case 5:
                                f = math.select(0f, 1f, math.any(pop_as_boolean3(ref code_pointer)) || math.any(pop_as_boolean3(ref code_pointer)) || math.any(pop_as_boolean3(ref code_pointer)) || math.any(pop_as_boolean3(ref code_pointer)) || math.any(pop_as_boolean3(ref code_pointer)));
                                break;

                            case 6:
                                f = math.select(0f, 1f, math.any(pop_as_boolean3(ref code_pointer)) || math.any(pop_as_boolean3(ref code_pointer)) || math.any(pop_as_boolean3(ref code_pointer)) || math.any(pop_as_boolean3(ref code_pointer)) || math.any(pop_as_boolean3(ref code_pointer)) || math.any(pop_as_boolean3(ref code_pointer)));
                                break;

                            case 7:
                                f = math.select(0f, 1f, math.any(pop_as_boolean3(ref code_pointer)) || math.any(pop_as_boolean3(ref code_pointer)) || math.any(pop_as_boolean3(ref code_pointer)) || math.any(pop_as_boolean3(ref code_pointer)) || math.any(pop_as_boolean3(ref code_pointer)) || math.any(pop_as_boolean3(ref code_pointer)) || math.any(pop_as_boolean3(ref code_pointer)));
                                break;

                            case 8:
                                f = math.select(0f, 1f, math.any(pop_as_boolean3(ref code_pointer)) || math.any(pop_as_boolean3(ref code_pointer)) || math.any(pop_as_boolean3(ref code_pointer)) || math.any(pop_as_boolean3(ref code_pointer)) || math.any(pop_as_boolean3(ref code_pointer)) || math.any(pop_as_boolean3(ref code_pointer)) || math.any(pop_as_boolean3(ref code_pointer)) || math.any(pop_as_boolean3(ref code_pointer)));
                                break;
                        }
                        break;
                    }

                case 0:
                case BlastVectorSizes.float4:
                    {
                        vector_size = 4;
                        switch (c)
                        {
                            default:
#if DEVELOPMENT_BUILD || TRACE
                                Debug.LogError($"blast.interpretor.get_any_result: parameter count '{c}' not supported -> parser/compiler/optimizer error");
#endif
                                f = float.NaN;
                                break;

                            case 2:
                                f = math.select(0f, 1f, math.any(pop_as_boolean4(ref code_pointer)) || math.any(pop_as_boolean4(ref code_pointer)));
                                break;

                            case 3:
                                f = math.select(0f, 1f, math.any(pop_as_boolean4(ref code_pointer)) || math.any(pop_as_boolean4(ref code_pointer)) || math.any(pop_as_boolean4(ref code_pointer)));
                                break;

                            case 4:
                                f = math.select(0f, 1f, math.any(pop_as_boolean4(ref code_pointer)) || math.any(pop_as_boolean4(ref code_pointer)) || math.any(pop_as_boolean4(ref code_pointer)) || math.any(pop_as_boolean4(ref code_pointer)));
                                break;

                            case 5:
                                f = math.select(0f, 1f, math.any(pop_as_boolean4(ref code_pointer)) || math.any(pop_as_boolean4(ref code_pointer)) || math.any(pop_as_boolean4(ref code_pointer)) || math.any(pop_as_boolean4(ref code_pointer)) || math.any(pop_as_boolean4(ref code_pointer)));
                                break;

                            case 6:
                                f = math.select(0f, 1f, math.any(pop_as_boolean4(ref code_pointer)) || math.any(pop_as_boolean4(ref code_pointer)) || math.any(pop_as_boolean4(ref code_pointer)) || math.any(pop_as_boolean4(ref code_pointer)) || math.any(pop_as_boolean4(ref code_pointer)) || math.any(pop_as_boolean4(ref code_pointer)));
                                break;

                            case 7:
                                f = math.select(0f, 1f, math.any(pop_as_boolean4(ref code_pointer)) || math.any(pop_as_boolean4(ref code_pointer)) || math.any(pop_as_boolean4(ref code_pointer)) || math.any(pop_as_boolean4(ref code_pointer)) || math.any(pop_as_boolean4(ref code_pointer)) || math.any(pop_as_boolean4(ref code_pointer)) || math.any(pop_as_boolean4(ref code_pointer)));
                                break;

                            case 8:
                                f = math.select(0f, 1f, math.any(pop_as_boolean4(ref code_pointer)) || math.any(pop_as_boolean4(ref code_pointer)) || math.any(pop_as_boolean4(ref code_pointer)) || math.any(pop_as_boolean4(ref code_pointer)) || math.any(pop_as_boolean4(ref code_pointer)) || math.any(pop_as_boolean4(ref code_pointer)) || math.any(pop_as_boolean4(ref code_pointer)) || math.any(pop_as_boolean4(ref code_pointer)));
                                break;
                        }
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
        void CALL_FN_ALL(ref int code_pointer, ref byte vector_size, ref BlastVariableDataType datatype, ref V4 f)
        {
            code_pointer++;
            BlastVectorSizes vectortype;
            byte c = BlastInterpretor.decode44(in code[code_pointer], out vectortype);
            code_pointer++;


#if DEVELOPMENT_BUILD || TRACE
            if (c < 1)
            {
                Debug.LogError($"get_any_result: parameter count '{c}' not supported");
                f = float.NaN;
                return;
            }
#endif

            switch ((BlastVectorSizes)vectortype)
            {
                default:
                    {
#if DEVELOPMENT_BUILD || TRACE
                        Debug.LogError($"get_any_result: vector size '{vector_size}' not (yet) supported");
#endif
                        f = float.NaN;
                        break;
                    }

                case BlastVectorSizes.bool32:
                    {
                        vector_size = 1;
                        f = math.select(0f, 1f, pop_b32(ref code_pointer).All);
                        for (int i = 2; i <= c; i++)
                        {
                            f.x = math.select(0f, 1f, pop_b32(ref code_pointer).All && f.x == 1);
                        }
                        break;
                    }


                case BlastVectorSizes.float1:
                    {
                        vector_size = 1;
                        f = pop_as_boolean(ref code_pointer) ? 1f : 0f;
                        //
                        //    CANNOT STOP EARLY DUE TO STACK OPERATIONS  
                        //    TODO   we could only process stack-offset changes in one is zero, doubt if there is much benefit
                        //                        
                        for (int i = 2; i <= c /*&& x != 0*/; i++)
                        {
                            f.x = math.select(0f, 1f, pop_as_boolean(ref code_pointer) && f.x != 0);
                        }
                        break;
                    }

                case BlastVectorSizes.float2:
                    {
                        vector_size = 2;
                        bool2 b2 = pop_as_boolean2(ref code_pointer);

                        for (int i = 2; i <= c; i++)
                        {
                            bool2 b22 = pop_as_boolean(ref code_pointer);
                            b2.x = b2.x && b22.x;
                            b2.y = b2.y && b22.y;
                        }

                        f = new float4(
                         math.select(0f, 1f, b2.x),
                         math.select(0f, 1f, b2.y),
                         0, 0);
                        break;
                    }

                case BlastVectorSizes.float3:
                    {
                        vector_size = 3;
                        bool3 b3 = pop_as_boolean3(ref code_pointer);

                        for (int i = 2; i <= c; i++)
                        {
                            bool3 b32 = pop_as_boolean3(ref code_pointer);
                            b3.x = b3.x && b32.x;
                            b3.y = b3.y && b32.y;
                            b3.z = b3.z && b32.z;
                        }

                        f.x = math.select(0f, 1f, b3.x);
                        f.y = math.select(0f, 1f, b3.y);
                        f.z = math.select(0f, 1f, b3.z);
                        f.w = 0;
                        break;
                    }

                case 0:
                case BlastVectorSizes.float4:
                    {
                        vector_size = 4;
                        bool4 b4 = pop_as_boolean4(ref code_pointer);

                        for (int i = 2; i <= c; i++)
                        {
                            bool4 b42 = pop_as_boolean4(ref code_pointer);
                            b4.x = b4.x && b42.x;
                            b4.y = b4.y && b42.y;
                            b4.z = b4.z && b42.z;
                            b4.w = b4.w && b42.w;
                        }

                        f.x = math.select(0f, 1f, b4.x);
                        f.y = math.select(0f, 1f, b4.y);
                        f.z = math.select(0f, 1f, b4.z);
                        f.w = math.select(0f, 1f, b4.w);
                        break;
                    }
            }
        }



    }
}
