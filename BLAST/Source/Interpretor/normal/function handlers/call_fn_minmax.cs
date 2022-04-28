//############################################################################################################################
// BLAST v1.0.4c                                                                                                             #
// Copyright © 2022 Rob Lemmens | NijnStein Software <rob.lemmens.s31 gmail com> All Rights Reserved                   ^__^\ #
// Unauthorized copying of this file, via any medium is strictly prohibited proprietary and confidential               (oo)\ #
//                                                                                                                     (__)  #
//############################################################################################################################

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
        /// get max value    [float] 
        ///  -> all parameters should have the same vectorsize and datatype as the output
        /// - returns vector of same size as input
        /// </summary>
        void CALL_FN_MAX(ref int code_pointer, ref byte vector_size, ref BlastVariableDataType type, ref V4 v4)
        {

            // (c) 2022  codegen by monkey 

            // get compressed value of count and vector_size
            code_pointer++;
            byte c = decode62(in code[code_pointer], ref vector_size);
            code_pointer++;

            // this will compile into a jump table (CHECK THIS !!) todo
            switch ((BlastVectorSizes)vector_size)
            {
                case BlastVectorSizes.id1:
                    {
                        switch (c)
                        {
                            default:
#if DEVELOPMENT_BUILD || TRACE
                                Debug.LogError($"Blast.Interpretor.MAX: ID({vector_size}) parameter count '{c}' not supported");
#endif
                                return;

                            case 2: v4.x = CodeUtils.ReInterpret<int, V1>(math.max(pop_as_i1(ref code_pointer), pop_as_i1(ref code_pointer))); break;
                            case 3: v4.x = CodeUtils.ReInterpret<int, V1>(math.max(math.max(pop_as_i1(ref code_pointer), pop_as_i1(ref code_pointer)), pop_as_i1(ref code_pointer))); break;
                            case 4: v4.x = CodeUtils.ReInterpret<int, V1>(math.max(math.max(pop_as_i1(ref code_pointer), pop_as_i1(ref code_pointer)), math.max(pop_as_i1(ref code_pointer), pop_as_i1(ref code_pointer)))); break;
                            case 5: v4.x = CodeUtils.ReInterpret<int, V1>(math.max(math.max(math.max(pop_as_i1(ref code_pointer), pop_as_i1(ref code_pointer)), math.max(pop_as_i1(ref code_pointer), pop_as_i1(ref code_pointer))), pop_as_i1(ref code_pointer))); break;
                            case 6: v4.x = CodeUtils.ReInterpret<int, V1>(math.max(math.max(math.max(pop_as_i1(ref code_pointer), pop_as_i1(ref code_pointer)), math.max(pop_as_i1(ref code_pointer), pop_as_i1(ref code_pointer))), math.max(pop_as_i1(ref code_pointer), pop_as_i1(ref code_pointer)))); break;
                            case 7: v4.x = CodeUtils.ReInterpret<int, V1>(math.max(math.max(math.max(math.max(pop_as_i1(ref code_pointer), pop_as_i1(ref code_pointer)), math.max(pop_as_i1(ref code_pointer), pop_as_i1(ref code_pointer))), math.max(pop_as_i1(ref code_pointer), pop_as_i1(ref code_pointer))), pop_as_i1(ref code_pointer))); break;
                            case 8: v4.x = CodeUtils.ReInterpret<int, V1>(math.max(math.max(math.max(math.max(pop_as_i1(ref code_pointer), pop_as_i1(ref code_pointer)), math.max(pop_as_i1(ref code_pointer), pop_as_i1(ref code_pointer))), math.max(pop_as_i1(ref code_pointer), pop_as_i1(ref code_pointer))), math.max(pop_as_i1(ref code_pointer), pop_as_i1(ref code_pointer)))); break;
                        }
                        break;
                    }

                case BlastVectorSizes.id2:
                    {
                        switch (c)
                        {
                            default:
#if DEVELOPMENT_BUILD || TRACE
                                Debug.LogError($"Blast.Interpretor.MAX: ID({vector_size}) parameter count '{c}' not supported");
#endif
                                return;

                            case 2: v4.xy = CodeUtils.ReInterpret<int2, V2>(math.max(pop_as_i2(ref code_pointer), pop_as_i2(ref code_pointer))); break;
                            case 3: v4.xy = CodeUtils.ReInterpret<int2, V2>(math.max(math.max(pop_as_i2(ref code_pointer), pop_as_i2(ref code_pointer)), pop_as_i2(ref code_pointer))); break;
                            case 4: v4.xy = CodeUtils.ReInterpret<int2, V2>(math.max(math.max(pop_as_i2(ref code_pointer), pop_as_i2(ref code_pointer)), math.max(pop_as_i2(ref code_pointer), pop_as_i2(ref code_pointer)))); break;
                            case 5: v4.xy = CodeUtils.ReInterpret<int2, V2>(math.max(math.max(math.max(pop_as_i2(ref code_pointer), pop_as_i2(ref code_pointer)), math.max(pop_as_i2(ref code_pointer), pop_as_i2(ref code_pointer))), pop_as_i2(ref code_pointer))); break;
                            case 6: v4.xy = CodeUtils.ReInterpret<int2, V2>(math.max(math.max(math.max(pop_as_i2(ref code_pointer), pop_as_i2(ref code_pointer)), math.max(pop_as_i2(ref code_pointer), pop_as_i2(ref code_pointer))), math.max(pop_as_i2(ref code_pointer), pop_as_i2(ref code_pointer)))); break;
                            case 7: v4.xy = CodeUtils.ReInterpret<int2, V2>(math.max(math.max(math.max(math.max(pop_as_i2(ref code_pointer), pop_as_i2(ref code_pointer)), math.max(pop_as_i2(ref code_pointer), pop_as_i2(ref code_pointer))), math.max(pop_as_i2(ref code_pointer), pop_as_i2(ref code_pointer))), pop_as_i2(ref code_pointer))); break;
                            case 8: v4.xy = CodeUtils.ReInterpret<int2, V2>(math.max(math.max(math.max(math.max(pop_as_i2(ref code_pointer), pop_as_i2(ref code_pointer)), math.max(pop_as_i2(ref code_pointer), pop_as_i2(ref code_pointer))), math.max(pop_as_i2(ref code_pointer), pop_as_i2(ref code_pointer))), math.max(pop_as_i2(ref code_pointer), pop_as_i2(ref code_pointer)))); break;
                        }
                        break;
                    }

                case BlastVectorSizes.id3:
                    {
                        switch (c)
                        {
                            default:
#if DEVELOPMENT_BUILD || TRACE
                                Debug.LogError($"Blast.Interpretor.MAX: ID({vector_size}) parameter count '{c}' not supported");
#endif
                                return;

                            case 2: v4.xyz = CodeUtils.ReInterpret<int3, V3>(math.max(pop_as_i3(ref code_pointer), pop_as_i3(ref code_pointer))); break;
                            case 3: v4.xyz = CodeUtils.ReInterpret<int3, V3>(math.max(math.max(pop_as_i3(ref code_pointer), pop_as_i3(ref code_pointer)), pop_as_i3(ref code_pointer))); break;
                            case 4: v4.xyz = CodeUtils.ReInterpret<int3, V3>(math.max(math.max(pop_as_i3(ref code_pointer), pop_as_i3(ref code_pointer)), math.max(pop_as_i3(ref code_pointer), pop_as_i3(ref code_pointer)))); break;
                            case 5: v4.xyz = CodeUtils.ReInterpret<int3, V3>(math.max(math.max(math.max(pop_as_i3(ref code_pointer), pop_as_i3(ref code_pointer)), math.max(pop_as_i3(ref code_pointer), pop_as_i3(ref code_pointer))), pop_as_i3(ref code_pointer))); break;
                            case 6: v4.xyz = CodeUtils.ReInterpret<int3, V3>(math.max(math.max(math.max(pop_as_i3(ref code_pointer), pop_as_i3(ref code_pointer)), math.max(pop_as_i3(ref code_pointer), pop_as_i3(ref code_pointer))), math.max(pop_as_i3(ref code_pointer), pop_as_i3(ref code_pointer)))); break;
                            case 7: v4.xyz = CodeUtils.ReInterpret<int3, V3>(math.max(math.max(math.max(math.max(pop_as_i3(ref code_pointer), pop_as_i3(ref code_pointer)), math.max(pop_as_i3(ref code_pointer), pop_as_i3(ref code_pointer))), math.max(pop_as_i3(ref code_pointer), pop_as_i3(ref code_pointer))), pop_as_i3(ref code_pointer))); break;
                            case 8: v4.xyz = CodeUtils.ReInterpret<int3, V3>(math.max(math.max(math.max(math.max(pop_as_i3(ref code_pointer), pop_as_i3(ref code_pointer)), math.max(pop_as_i3(ref code_pointer), pop_as_i3(ref code_pointer))), math.max(pop_as_i3(ref code_pointer), pop_as_i3(ref code_pointer))), math.max(pop_as_i3(ref code_pointer), pop_as_i3(ref code_pointer)))); break;
                        }
                        break;
                    }

                case BlastVectorSizes.id4:
                    {
                        switch (c)
                        {
                            default:
#if DEVELOPMENT_BUILD || TRACE
                                Debug.LogError($"Blast.Interpretor.MAX: ID({vector_size}) parameter count '{c}' not supported");
#endif
                                return;

                            case 2: v4 = CodeUtils.ReInterpret<int4, V4>(math.max(pop_as_i4(ref code_pointer), pop_as_i4(ref code_pointer))); break;
                            case 3: v4 = CodeUtils.ReInterpret<int4, V4>(math.max(math.max(pop_as_i4(ref code_pointer), pop_as_i4(ref code_pointer)), pop_as_i4(ref code_pointer))); break;
                            case 4: v4 = CodeUtils.ReInterpret<int4, V4>(math.max(math.max(pop_as_i4(ref code_pointer), pop_as_i4(ref code_pointer)), math.max(pop_as_i4(ref code_pointer), pop_as_i4(ref code_pointer)))); break;
                            case 5: v4 = CodeUtils.ReInterpret<int4, V4>(math.max(math.max(math.max(pop_as_i4(ref code_pointer), pop_as_i4(ref code_pointer)), math.max(pop_as_i4(ref code_pointer), pop_as_i4(ref code_pointer))), pop_as_i4(ref code_pointer))); break;
                            case 6: v4 = CodeUtils.ReInterpret<int4, V4>(math.max(math.max(math.max(pop_as_i4(ref code_pointer), pop_as_i4(ref code_pointer)), math.max(pop_as_i4(ref code_pointer), pop_as_i4(ref code_pointer))), math.max(pop_as_i4(ref code_pointer), pop_as_i4(ref code_pointer)))); break;
                            case 7: v4 = CodeUtils.ReInterpret<int4, V4>(math.max(math.max(math.max(math.max(pop_as_i4(ref code_pointer), pop_as_i4(ref code_pointer)), math.max(pop_as_i4(ref code_pointer), pop_as_i4(ref code_pointer))), math.max(pop_as_i4(ref code_pointer), pop_as_i4(ref code_pointer))), pop_as_i4(ref code_pointer))); break;
                            case 8: v4 = CodeUtils.ReInterpret<int4, V4>(math.max(math.max(math.max(math.max(pop_as_i4(ref code_pointer), pop_as_i4(ref code_pointer)), math.max(pop_as_i4(ref code_pointer), pop_as_i4(ref code_pointer))), math.max(pop_as_i4(ref code_pointer), pop_as_i4(ref code_pointer))), math.max(pop_as_i4(ref code_pointer), pop_as_i4(ref code_pointer)))); break;
                        }
                        break;
                    }



                case BlastVectorSizes.float1:
                    {
                        // c == function parameter count 
                        switch (c)
                        {
                            default:
#if DEVELOPMENT_BUILD || TRACE
                                Debug.LogError($"Blast.Interpretor.MAX: NUMERIC({vector_size}) parameter count '{c}' not supported");
#endif
                                return;

                            case 2: v4.x = CodeUtils.ReInterpret<float, V1>(math.max(pop_as_f1(ref code_pointer), pop_as_f1(ref code_pointer))); break;
                            case 3: v4.x = CodeUtils.ReInterpret<float, V1>(math.max(math.max(pop_as_f1(ref code_pointer), pop_as_f1(ref code_pointer)), pop_as_f1(ref code_pointer))); break;
                            case 4: v4.x = CodeUtils.ReInterpret<float, V1>(math.max(math.max(pop_as_f1(ref code_pointer), pop_as_f1(ref code_pointer)), math.max(pop_as_f1(ref code_pointer), pop_as_f1(ref code_pointer)))); break;
                            case 5: v4.x = CodeUtils.ReInterpret<float, V1>(math.max(math.max(math.max(pop_as_f1(ref code_pointer), pop_as_f1(ref code_pointer)), math.max(pop_as_f1(ref code_pointer), pop_as_f1(ref code_pointer))), pop_as_f1(ref code_pointer))); break;
                            case 6: v4.x = CodeUtils.ReInterpret<float, V1>(math.max(math.max(math.max(pop_as_f1(ref code_pointer), pop_as_f1(ref code_pointer)), math.max(pop_as_f1(ref code_pointer), pop_as_f1(ref code_pointer))), math.max(pop_as_f1(ref code_pointer), pop_as_f1(ref code_pointer)))); break;
                            case 7: v4.x = CodeUtils.ReInterpret<float, V1>(math.max(math.max(math.max(math.max(pop_as_f1(ref code_pointer), pop_as_f1(ref code_pointer)), math.max(pop_as_f1(ref code_pointer), pop_as_f1(ref code_pointer))), math.max(pop_as_f1(ref code_pointer), pop_as_f1(ref code_pointer))), pop_as_f1(ref code_pointer))); break;
                            case 8: v4.x = CodeUtils.ReInterpret<float, V1>(math.max(math.max(math.max(math.max(pop_as_f1(ref code_pointer), pop_as_f1(ref code_pointer)), math.max(pop_as_f1(ref code_pointer), pop_as_f1(ref code_pointer))), math.max(pop_as_f1(ref code_pointer), pop_as_f1(ref code_pointer))), math.max(pop_as_f1(ref code_pointer), pop_as_f1(ref code_pointer)))); break;
                        }
                        break;
                    }

                case BlastVectorSizes.float2:
                    {
                        switch (c)
                        {
                            default:
#if DEVELOPMENT_BUILD || TRACE
                                Debug.LogError($"Blast.Interpretor.MAX: NUMERIC({vector_size}) parameter count '{c}' not supported");
#endif
                                v4 = float.NaN;
                                break;

                            case 2: v4.xy = CodeUtils.ReInterpret<float2, V2>(math.max(pop_as_f2(ref code_pointer), pop_as_f2(ref code_pointer))); break;
                            case 3: v4.xy = CodeUtils.ReInterpret<float2, V2>(math.max(math.max(pop_as_f2(ref code_pointer), pop_as_f2(ref code_pointer)), pop_as_f2(ref code_pointer))); break;
                            case 4: v4.xy = CodeUtils.ReInterpret<float2, V2>(math.max(math.max(pop_as_f2(ref code_pointer), pop_as_f2(ref code_pointer)), math.max(pop_as_f2(ref code_pointer), pop_as_f2(ref code_pointer)))); break;
                            case 5: v4.xy = CodeUtils.ReInterpret<float2, V2>(math.max(math.max(math.max(pop_as_f2(ref code_pointer), pop_as_f2(ref code_pointer)), math.max(pop_as_f2(ref code_pointer), pop_as_f2(ref code_pointer))), pop_as_f2(ref code_pointer))); break;
                            case 6: v4.xy = CodeUtils.ReInterpret<float2, V2>(math.max(math.max(math.max(pop_as_f2(ref code_pointer), pop_as_f2(ref code_pointer)), math.max(pop_as_f2(ref code_pointer), pop_as_f2(ref code_pointer))), math.max(pop_as_f2(ref code_pointer), pop_as_f2(ref code_pointer)))); break;
                            case 7: v4.xy = CodeUtils.ReInterpret<float2, V2>(math.max(math.max(math.max(math.max(pop_as_f2(ref code_pointer), pop_as_f2(ref code_pointer)), math.max(pop_as_f2(ref code_pointer), pop_as_f2(ref code_pointer))), math.max(pop_as_f2(ref code_pointer), pop_as_f2(ref code_pointer))), pop_as_f2(ref code_pointer))); break;
                            case 8: v4.xy = CodeUtils.ReInterpret<float2, V2>(math.max(math.max(math.max(math.max(pop_as_f2(ref code_pointer), pop_as_f2(ref code_pointer)), math.max(pop_as_f2(ref code_pointer), pop_as_f2(ref code_pointer))), math.max(pop_as_f2(ref code_pointer), pop_as_f2(ref code_pointer))), math.max(pop_as_f2(ref code_pointer), pop_as_f2(ref code_pointer)))); break;
                        }
                        break;
                    }

                case BlastVectorSizes.float3:
                    {
                        switch (c)
                        {
                            default:
#if DEVELOPMENT_BUILD || TRACE
                                Debug.LogError($"Blast.Interpretor.MAX: NUMERIC({vector_size}) parameter count '{c}' not supported");
#endif
                                v4 = float.NaN;
                                break;


                            case 2: v4.xyz = CodeUtils.ReInterpret<float3, V3>(math.max(pop_as_f3(ref code_pointer), pop_as_f3(ref code_pointer))); break;
                            case 3: v4.xyz = CodeUtils.ReInterpret<float3, V3>(math.max(math.max(pop_as_f3(ref code_pointer), pop_as_f3(ref code_pointer)), pop_as_f3(ref code_pointer))); break;
                            case 4: v4.xyz = CodeUtils.ReInterpret<float3, V3>(math.max(math.max(pop_as_f3(ref code_pointer), pop_as_f3(ref code_pointer)), math.max(pop_as_f3(ref code_pointer), pop_as_f3(ref code_pointer)))); break;
                            case 5: v4.xyz = CodeUtils.ReInterpret<float3, V3>(math.max(math.max(math.max(pop_as_f3(ref code_pointer), pop_as_f3(ref code_pointer)), math.max(pop_as_f3(ref code_pointer), pop_as_f3(ref code_pointer))), pop_as_f3(ref code_pointer))); break;
                            case 6: v4.xyz = CodeUtils.ReInterpret<float3, V3>(math.max(math.max(math.max(pop_as_f3(ref code_pointer), pop_as_f3(ref code_pointer)), math.max(pop_as_f3(ref code_pointer), pop_as_f3(ref code_pointer))), math.max(pop_as_f3(ref code_pointer), pop_as_f3(ref code_pointer)))); break;
                            case 7: v4.xyz = CodeUtils.ReInterpret<float3, V3>(math.max(math.max(math.max(math.max(pop_as_f3(ref code_pointer), pop_as_f3(ref code_pointer)), math.max(pop_as_f3(ref code_pointer), pop_as_f3(ref code_pointer))), math.max(pop_as_f3(ref code_pointer), pop_as_f3(ref code_pointer))), pop_as_f3(ref code_pointer))); break;
                            case 8: v4.xyz = CodeUtils.ReInterpret<float3, V3>(math.max(math.max(math.max(math.max(pop_as_f3(ref code_pointer), pop_as_f3(ref code_pointer)), math.max(pop_as_f3(ref code_pointer), pop_as_f3(ref code_pointer))), math.max(pop_as_f3(ref code_pointer), pop_as_f3(ref code_pointer))), math.max(pop_as_f3(ref code_pointer), pop_as_f3(ref code_pointer)))); break;
                        }
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
                                Debug.LogError($"Blast.Interpretor.MAX: NUMERIC(4) parameter count '{c}' not supported -> parser/compiler/optimizer error");
#endif
                                v4 = float.NaN;
                                break;

                            case 2: v4 = CodeUtils.ReInterpret<float4, V4>(math.max(pop_as_f4(ref code_pointer), pop_as_f4(ref code_pointer))); break;
                            case 3: v4 = CodeUtils.ReInterpret<float4, V4>(math.max(math.max(pop_as_f4(ref code_pointer), pop_as_f4(ref code_pointer)), pop_as_f4(ref code_pointer))); break;
                            case 4: v4 = CodeUtils.ReInterpret<float4, V4>(math.max(math.max(pop_as_f4(ref code_pointer), pop_as_f4(ref code_pointer)), math.max(pop_as_f4(ref code_pointer), pop_as_f4(ref code_pointer)))); break;
                            case 5: v4 = CodeUtils.ReInterpret<float4, V4>(math.max(math.max(math.max(pop_as_f4(ref code_pointer), pop_as_f4(ref code_pointer)), math.max(pop_as_f4(ref code_pointer), pop_as_f4(ref code_pointer))), pop_as_f4(ref code_pointer))); break;
                            case 6: v4 = CodeUtils.ReInterpret<float4, V4>(math.max(math.max(math.max(pop_as_f4(ref code_pointer), pop_as_f4(ref code_pointer)), math.max(pop_as_f4(ref code_pointer), pop_as_f4(ref code_pointer))), math.max(pop_as_f4(ref code_pointer), pop_as_f4(ref code_pointer)))); break;
                            case 7: v4 = CodeUtils.ReInterpret<float4, V4>(math.max(math.max(math.max(math.max(pop_as_f4(ref code_pointer), pop_as_f4(ref code_pointer)), math.max(pop_as_f4(ref code_pointer), pop_as_f4(ref code_pointer))), math.max(pop_as_f4(ref code_pointer), pop_as_f4(ref code_pointer))), pop_as_f4(ref code_pointer))); break;
                            case 8: v4 = CodeUtils.ReInterpret<float4, V4>(math.max(math.max(math.max(math.max(pop_as_f4(ref code_pointer), pop_as_f4(ref code_pointer)), math.max(pop_as_f4(ref code_pointer), pop_as_f4(ref code_pointer))), math.max(pop_as_f4(ref code_pointer), pop_as_f4(ref code_pointer))), math.max(pop_as_f4(ref code_pointer), pop_as_f4(ref code_pointer)))); break;
                        }
                        break;
                    }

                default:
                    {
#if DEVELOPMENT_BUILD || TRACE
                        Debug.LogError($"Blast.Interpretor.MAX: vector size '{vector_size}' not allowed");
#endif
                        v4 = CodeUtils.ReInterpret<float, V1>(float.NaN);
                        type = BlastVariableDataType.Numeric;
                        break;
                    }
            }
        }


        /// <summary>
        /// get max value    [float] 
        ///  -> all parameters should have the same vectorsize and datatype as the output
        /// - returns vector of same size as input
        /// </summary>
        void CALL_FN_MIN(ref int code_pointer, ref byte vector_size, ref BlastVariableDataType type, ref V4 v4)
        {

            // (c) 2022  codegen by monkey 

            // get compressed value of count and vector_size
            code_pointer++;
            byte c = decode62(in code[code_pointer], ref vector_size);
            code_pointer++;

            // this will compile into a jump table (CHECK THIS !!) todo
            switch ((BlastVectorSizes)vector_size)
            {
                case BlastVectorSizes.id1:
                    {
                        switch (c)
                        {
                            default:
#if DEVELOPMENT_BUILD || TRACE
                                Debug.LogError($"Blast.Interpretor.MIN: ID({vector_size}) parameter count '{c}' not supported");
#endif
                                return;

                            case 2: v4.x = CodeUtils.ReInterpret<int, V1>(math.min(pop_as_i1(ref code_pointer), pop_as_i1(ref code_pointer))); break;
                            case 3: v4.x = CodeUtils.ReInterpret<int, V1>(math.min(math.min(pop_as_i1(ref code_pointer), pop_as_i1(ref code_pointer)), pop_as_i1(ref code_pointer))); break;
                            case 4: v4.x = CodeUtils.ReInterpret<int, V1>(math.min(math.min(pop_as_i1(ref code_pointer), pop_as_i1(ref code_pointer)), math.min(pop_as_i1(ref code_pointer), pop_as_i1(ref code_pointer)))); break;
                            case 5: v4.x = CodeUtils.ReInterpret<int, V1>(math.min(math.min(math.min(pop_as_i1(ref code_pointer), pop_as_i1(ref code_pointer)), math.min(pop_as_i1(ref code_pointer), pop_as_i1(ref code_pointer))), pop_as_i1(ref code_pointer))); break;
                            case 6: v4.x = CodeUtils.ReInterpret<int, V1>(math.min(math.min(math.min(pop_as_i1(ref code_pointer), pop_as_i1(ref code_pointer)), math.min(pop_as_i1(ref code_pointer), pop_as_i1(ref code_pointer))), math.min(pop_as_i1(ref code_pointer), pop_as_i1(ref code_pointer)))); break;
                            case 7: v4.x = CodeUtils.ReInterpret<int, V1>(math.min(math.min(math.min(math.min(pop_as_i1(ref code_pointer), pop_as_i1(ref code_pointer)), math.min(pop_as_i1(ref code_pointer), pop_as_i1(ref code_pointer))), math.min(pop_as_i1(ref code_pointer), pop_as_i1(ref code_pointer))), pop_as_i1(ref code_pointer))); break;
                            case 8: v4.x = CodeUtils.ReInterpret<int, V1>(math.min(math.min(math.min(math.min(pop_as_i1(ref code_pointer), pop_as_i1(ref code_pointer)), math.min(pop_as_i1(ref code_pointer), pop_as_i1(ref code_pointer))), math.min(pop_as_i1(ref code_pointer), pop_as_i1(ref code_pointer))), math.min(pop_as_i1(ref code_pointer), pop_as_i1(ref code_pointer)))); break;
                        }
                        break;
                    }

                case BlastVectorSizes.id2:
                    {
                        switch (c)
                        {
                            default:
#if DEVELOPMENT_BUILD || TRACE
                                Debug.LogError($"Blast.Interpretor.MIN: ID({vector_size}) parameter count '{c}' not supported");
#endif
                                return;

                            case 2: v4.xy = CodeUtils.ReInterpret<int2, V2>(math.min(pop_as_i2(ref code_pointer), pop_as_i2(ref code_pointer))); break;
                            case 3: v4.xy = CodeUtils.ReInterpret<int2, V2>(math.min(math.min(pop_as_i2(ref code_pointer), pop_as_i2(ref code_pointer)), pop_as_i2(ref code_pointer))); break;
                            case 4: v4.xy = CodeUtils.ReInterpret<int2, V2>(math.min(math.min(pop_as_i2(ref code_pointer), pop_as_i2(ref code_pointer)), math.min(pop_as_i2(ref code_pointer), pop_as_i2(ref code_pointer)))); break;
                            case 5: v4.xy = CodeUtils.ReInterpret<int2, V2>(math.min(math.min(math.min(pop_as_i2(ref code_pointer), pop_as_i2(ref code_pointer)), math.min(pop_as_i2(ref code_pointer), pop_as_i2(ref code_pointer))), pop_as_i2(ref code_pointer))); break;
                            case 6: v4.xy = CodeUtils.ReInterpret<int2, V2>(math.min(math.min(math.min(pop_as_i2(ref code_pointer), pop_as_i2(ref code_pointer)), math.min(pop_as_i2(ref code_pointer), pop_as_i2(ref code_pointer))), math.min(pop_as_i2(ref code_pointer), pop_as_i2(ref code_pointer)))); break;
                            case 7: v4.xy = CodeUtils.ReInterpret<int2, V2>(math.min(math.min(math.min(math.min(pop_as_i2(ref code_pointer), pop_as_i2(ref code_pointer)), math.min(pop_as_i2(ref code_pointer), pop_as_i2(ref code_pointer))), math.min(pop_as_i2(ref code_pointer), pop_as_i2(ref code_pointer))), pop_as_i2(ref code_pointer))); break;
                            case 8: v4.xy = CodeUtils.ReInterpret<int2, V2>(math.min(math.min(math.min(math.min(pop_as_i2(ref code_pointer), pop_as_i2(ref code_pointer)), math.min(pop_as_i2(ref code_pointer), pop_as_i2(ref code_pointer))), math.min(pop_as_i2(ref code_pointer), pop_as_i2(ref code_pointer))), math.min(pop_as_i2(ref code_pointer), pop_as_i2(ref code_pointer)))); break;
                        }
                        break;
                    }

                case BlastVectorSizes.id3:
                    {
                        switch (c)
                        {
                            default:
#if DEVELOPMENT_BUILD || TRACE
                                Debug.LogError($"Blast.Interpretor.MIN: ID({vector_size}) parameter count '{c}' not supported");
#endif
                                return;

                            case 2: v4.xyz = CodeUtils.ReInterpret<int3, V3>(math.min(pop_as_i3(ref code_pointer), pop_as_i3(ref code_pointer))); break;
                            case 3: v4.xyz = CodeUtils.ReInterpret<int3, V3>(math.min(math.min(pop_as_i3(ref code_pointer), pop_as_i3(ref code_pointer)), pop_as_i3(ref code_pointer))); break;
                            case 4: v4.xyz = CodeUtils.ReInterpret<int3, V3>(math.min(math.min(pop_as_i3(ref code_pointer), pop_as_i3(ref code_pointer)), math.min(pop_as_i3(ref code_pointer), pop_as_i3(ref code_pointer)))); break;
                            case 5: v4.xyz = CodeUtils.ReInterpret<int3, V3>(math.min(math.min(math.min(pop_as_i3(ref code_pointer), pop_as_i3(ref code_pointer)), math.min(pop_as_i3(ref code_pointer), pop_as_i3(ref code_pointer))), pop_as_i3(ref code_pointer))); break;
                            case 6: v4.xyz = CodeUtils.ReInterpret<int3, V3>(math.min(math.min(math.min(pop_as_i3(ref code_pointer), pop_as_i3(ref code_pointer)), math.min(pop_as_i3(ref code_pointer), pop_as_i3(ref code_pointer))), math.min(pop_as_i3(ref code_pointer), pop_as_i3(ref code_pointer)))); break;
                            case 7: v4.xyz = CodeUtils.ReInterpret<int3, V3>(math.min(math.min(math.min(math.min(pop_as_i3(ref code_pointer), pop_as_i3(ref code_pointer)), math.min(pop_as_i3(ref code_pointer), pop_as_i3(ref code_pointer))), math.min(pop_as_i3(ref code_pointer), pop_as_i3(ref code_pointer))), pop_as_i3(ref code_pointer))); break;
                            case 8: v4.xyz = CodeUtils.ReInterpret<int3, V3>(math.min(math.min(math.min(math.min(pop_as_i3(ref code_pointer), pop_as_i3(ref code_pointer)), math.min(pop_as_i3(ref code_pointer), pop_as_i3(ref code_pointer))), math.min(pop_as_i3(ref code_pointer), pop_as_i3(ref code_pointer))), math.min(pop_as_i3(ref code_pointer), pop_as_i3(ref code_pointer)))); break;
                        }
                        break;
                    }

                case BlastVectorSizes.id4:
                    {
                        switch (c)
                        {
                            default:
#if DEVELOPMENT_BUILD || TRACE
                                Debug.LogError($"Blast.Interpretor.MIN: ID({vector_size}) parameter count '{c}' not supported");
#endif
                                return;

                            case 2: v4 = CodeUtils.ReInterpret<int4, V4>(math.min(pop_as_i4(ref code_pointer), pop_as_i4(ref code_pointer))); break;
                            case 3: v4 = CodeUtils.ReInterpret<int4, V4>(math.min(math.min(pop_as_i4(ref code_pointer), pop_as_i4(ref code_pointer)), pop_as_i4(ref code_pointer))); break;
                            case 4: v4 = CodeUtils.ReInterpret<int4, V4>(math.min(math.min(pop_as_i4(ref code_pointer), pop_as_i4(ref code_pointer)), math.min(pop_as_i4(ref code_pointer), pop_as_i4(ref code_pointer)))); break;
                            case 5: v4 = CodeUtils.ReInterpret<int4, V4>(math.min(math.min(math.min(pop_as_i4(ref code_pointer), pop_as_i4(ref code_pointer)), math.min(pop_as_i4(ref code_pointer), pop_as_i4(ref code_pointer))), pop_as_i4(ref code_pointer))); break;
                            case 6: v4 = CodeUtils.ReInterpret<int4, V4>(math.min(math.min(math.min(pop_as_i4(ref code_pointer), pop_as_i4(ref code_pointer)), math.min(pop_as_i4(ref code_pointer), pop_as_i4(ref code_pointer))), math.min(pop_as_i4(ref code_pointer), pop_as_i4(ref code_pointer)))); break;
                            case 7: v4 = CodeUtils.ReInterpret<int4, V4>(math.min(math.min(math.min(math.min(pop_as_i4(ref code_pointer), pop_as_i4(ref code_pointer)), math.min(pop_as_i4(ref code_pointer), pop_as_i4(ref code_pointer))), math.min(pop_as_i4(ref code_pointer), pop_as_i4(ref code_pointer))), pop_as_i4(ref code_pointer))); break;
                            case 8: v4 = CodeUtils.ReInterpret<int4, V4>(math.min(math.min(math.min(math.min(pop_as_i4(ref code_pointer), pop_as_i4(ref code_pointer)), math.min(pop_as_i4(ref code_pointer), pop_as_i4(ref code_pointer))), math.min(pop_as_i4(ref code_pointer), pop_as_i4(ref code_pointer))), math.min(pop_as_i4(ref code_pointer), pop_as_i4(ref code_pointer)))); break;
                        }
                        break;
                    }



                case BlastVectorSizes.float1:
                    {
                        // c == function parameter count 
                        switch (c)
                        {
                            default:
#if DEVELOPMENT_BUILD || TRACE
                                Debug.LogError($"Blast.Interpretor.MIN: NUMERIC({vector_size}) parameter count '{c}' not supported");
#endif
                                return;

                            case 2: v4.x = CodeUtils.ReInterpret<float, V1>(math.min(pop_as_f1(ref code_pointer), pop_as_f1(ref code_pointer))); break;
                            case 3: v4.x = CodeUtils.ReInterpret<float, V1>(math.min(math.min(pop_as_f1(ref code_pointer), pop_as_f1(ref code_pointer)), pop_as_f1(ref code_pointer))); break;
                            case 4: v4.x = CodeUtils.ReInterpret<float, V1>(math.min(math.min(pop_as_f1(ref code_pointer), pop_as_f1(ref code_pointer)), math.min(pop_as_f1(ref code_pointer), pop_as_f1(ref code_pointer)))); break;
                            case 5: v4.x = CodeUtils.ReInterpret<float, V1>(math.min(math.min(math.min(pop_as_f1(ref code_pointer), pop_as_f1(ref code_pointer)), math.min(pop_as_f1(ref code_pointer), pop_as_f1(ref code_pointer))), pop_as_f1(ref code_pointer))); break;
                            case 6: v4.x = CodeUtils.ReInterpret<float, V1>(math.min(math.min(math.min(pop_as_f1(ref code_pointer), pop_as_f1(ref code_pointer)), math.min(pop_as_f1(ref code_pointer), pop_as_f1(ref code_pointer))), math.min(pop_as_f1(ref code_pointer), pop_as_f1(ref code_pointer)))); break;
                            case 7: v4.x = CodeUtils.ReInterpret<float, V1>(math.min(math.min(math.min(math.min(pop_as_f1(ref code_pointer), pop_as_f1(ref code_pointer)), math.min(pop_as_f1(ref code_pointer), pop_as_f1(ref code_pointer))), math.min(pop_as_f1(ref code_pointer), pop_as_f1(ref code_pointer))), pop_as_f1(ref code_pointer))); break;
                            case 8: v4.x = CodeUtils.ReInterpret<float, V1>(math.min(math.min(math.min(math.min(pop_as_f1(ref code_pointer), pop_as_f1(ref code_pointer)), math.min(pop_as_f1(ref code_pointer), pop_as_f1(ref code_pointer))), math.min(pop_as_f1(ref code_pointer), pop_as_f1(ref code_pointer))), math.min(pop_as_f1(ref code_pointer), pop_as_f1(ref code_pointer)))); break;
                        }
                        break;
                    }

                case BlastVectorSizes.float2:
                    {
                        switch (c)
                        {
                            default:
#if DEVELOPMENT_BUILD || TRACE
                                Debug.LogError($"Blast.Interpretor.MIN: NUMERIC({vector_size}) parameter count '{c}' not supported");
#endif
                                v4 = float.NaN;
                                break;

                            case 2: v4.xy = CodeUtils.ReInterpret<float2, V2>(math.min(pop_as_f2(ref code_pointer), pop_as_f2(ref code_pointer))); break;
                            case 3: v4.xy = CodeUtils.ReInterpret<float2, V2>(math.min(math.min(pop_as_f2(ref code_pointer), pop_as_f2(ref code_pointer)), pop_as_f2(ref code_pointer))); break;
                            case 4: v4.xy = CodeUtils.ReInterpret<float2, V2>(math.min(math.min(pop_as_f2(ref code_pointer), pop_as_f2(ref code_pointer)), math.min(pop_as_f2(ref code_pointer), pop_as_f2(ref code_pointer)))); break;
                            case 5: v4.xy = CodeUtils.ReInterpret<float2, V2>(math.min(math.min(math.min(pop_as_f2(ref code_pointer), pop_as_f2(ref code_pointer)), math.min(pop_as_f2(ref code_pointer), pop_as_f2(ref code_pointer))), pop_as_f2(ref code_pointer))); break;
                            case 6: v4.xy = CodeUtils.ReInterpret<float2, V2>(math.min(math.min(math.min(pop_as_f2(ref code_pointer), pop_as_f2(ref code_pointer)), math.min(pop_as_f2(ref code_pointer), pop_as_f2(ref code_pointer))), math.min(pop_as_f2(ref code_pointer), pop_as_f2(ref code_pointer)))); break;
                            case 7: v4.xy = CodeUtils.ReInterpret<float2, V2>(math.min(math.min(math.min(math.min(pop_as_f2(ref code_pointer), pop_as_f2(ref code_pointer)), math.min(pop_as_f2(ref code_pointer), pop_as_f2(ref code_pointer))), math.min(pop_as_f2(ref code_pointer), pop_as_f2(ref code_pointer))), pop_as_f2(ref code_pointer))); break;
                            case 8: v4.xy = CodeUtils.ReInterpret<float2, V2>(math.min(math.min(math.min(math.min(pop_as_f2(ref code_pointer), pop_as_f2(ref code_pointer)), math.min(pop_as_f2(ref code_pointer), pop_as_f2(ref code_pointer))), math.min(pop_as_f2(ref code_pointer), pop_as_f2(ref code_pointer))), math.min(pop_as_f2(ref code_pointer), pop_as_f2(ref code_pointer)))); break;
                        }
                        break;
                    }

                case BlastVectorSizes.float3:
                    {
                        switch (c)
                        {
                            default:
#if DEVELOPMENT_BUILD || TRACE
                                Debug.LogError($"Blast.Interpretor.MIN: NUMERIC({vector_size}) parameter count '{c}' not supported");
#endif
                                v4 = float.NaN;
                                break;


                            case 2: v4.xyz = CodeUtils.ReInterpret<float3, V3>(math.min(pop_as_f3(ref code_pointer), pop_as_f3(ref code_pointer))); break;
                            case 3: v4.xyz = CodeUtils.ReInterpret<float3, V3>(math.min(math.min(pop_as_f3(ref code_pointer), pop_as_f3(ref code_pointer)), pop_as_f3(ref code_pointer))); break;
                            case 4: v4.xyz = CodeUtils.ReInterpret<float3, V3>(math.min(math.min(pop_as_f3(ref code_pointer), pop_as_f3(ref code_pointer)), math.min(pop_as_f3(ref code_pointer), pop_as_f3(ref code_pointer)))); break;
                            case 5: v4.xyz = CodeUtils.ReInterpret<float3, V3>(math.min(math.min(math.min(pop_as_f3(ref code_pointer), pop_as_f3(ref code_pointer)), math.min(pop_as_f3(ref code_pointer), pop_as_f3(ref code_pointer))), pop_as_f3(ref code_pointer))); break;
                            case 6: v4.xyz = CodeUtils.ReInterpret<float3, V3>(math.min(math.min(math.min(pop_as_f3(ref code_pointer), pop_as_f3(ref code_pointer)), math.min(pop_as_f3(ref code_pointer), pop_as_f3(ref code_pointer))), math.min(pop_as_f3(ref code_pointer), pop_as_f3(ref code_pointer)))); break;
                            case 7: v4.xyz = CodeUtils.ReInterpret<float3, V3>(math.min(math.min(math.min(math.min(pop_as_f3(ref code_pointer), pop_as_f3(ref code_pointer)), math.min(pop_as_f3(ref code_pointer), pop_as_f3(ref code_pointer))), math.min(pop_as_f3(ref code_pointer), pop_as_f3(ref code_pointer))), pop_as_f3(ref code_pointer))); break;
                            case 8: v4.xyz = CodeUtils.ReInterpret<float3, V3>(math.min(math.min(math.min(math.min(pop_as_f3(ref code_pointer), pop_as_f3(ref code_pointer)), math.min(pop_as_f3(ref code_pointer), pop_as_f3(ref code_pointer))), math.min(pop_as_f3(ref code_pointer), pop_as_f3(ref code_pointer))), math.min(pop_as_f3(ref code_pointer), pop_as_f3(ref code_pointer)))); break;
                        }
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
                                Debug.LogError($"Blast.Interpretor.MIN: NUMERIC(4) parameter count '{c}' not supported -> parser/compiler/optimizer error");
#endif
                                v4 = float.NaN;
                                break;

                            case 2: v4 = CodeUtils.ReInterpret<float4, V4>(math.min(pop_as_f4(ref code_pointer), pop_as_f4(ref code_pointer))); break;
                            case 3: v4 = CodeUtils.ReInterpret<float4, V4>(math.min(math.min(pop_as_f4(ref code_pointer), pop_as_f4(ref code_pointer)), pop_as_f4(ref code_pointer))); break;
                            case 4: v4 = CodeUtils.ReInterpret<float4, V4>(math.min(math.min(pop_as_f4(ref code_pointer), pop_as_f4(ref code_pointer)), math.min(pop_as_f4(ref code_pointer), pop_as_f4(ref code_pointer)))); break;
                            case 5: v4 = CodeUtils.ReInterpret<float4, V4>(math.min(math.min(math.min(pop_as_f4(ref code_pointer), pop_as_f4(ref code_pointer)), math.min(pop_as_f4(ref code_pointer), pop_as_f4(ref code_pointer))), pop_as_f4(ref code_pointer))); break;
                            case 6: v4 = CodeUtils.ReInterpret<float4, V4>(math.min(math.min(math.min(pop_as_f4(ref code_pointer), pop_as_f4(ref code_pointer)), math.min(pop_as_f4(ref code_pointer), pop_as_f4(ref code_pointer))), math.min(pop_as_f4(ref code_pointer), pop_as_f4(ref code_pointer)))); break;
                            case 7: v4 = CodeUtils.ReInterpret<float4, V4>(math.min(math.min(math.min(math.min(pop_as_f4(ref code_pointer), pop_as_f4(ref code_pointer)), math.min(pop_as_f4(ref code_pointer), pop_as_f4(ref code_pointer))), math.min(pop_as_f4(ref code_pointer), pop_as_f4(ref code_pointer))), pop_as_f4(ref code_pointer))); break;
                            case 8: v4 = CodeUtils.ReInterpret<float4, V4>(math.min(math.min(math.min(math.min(pop_as_f4(ref code_pointer), pop_as_f4(ref code_pointer)), math.min(pop_as_f4(ref code_pointer), pop_as_f4(ref code_pointer))), math.min(pop_as_f4(ref code_pointer), pop_as_f4(ref code_pointer))), math.min(pop_as_f4(ref code_pointer), pop_as_f4(ref code_pointer)))); break;
                        }
                        break;
                    }

                default:
                    {
#if DEVELOPMENT_BUILD || TRACE
                        Debug.LogError($"Blast.Interpretor.MIN: vector size '{vector_size}' not allowed");
#endif
                        v4 = CodeUtils.ReInterpret<float, V1>(float.NaN);
                        type = BlastVariableDataType.Numeric;
                        break;
                    }
            }
        }


    }
}
