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
        /// get dot product of 2 paramaters 
        /// - 2 inputs
        /// - inputvector size = any
        /// - output vectorsize == 1
        /// </summary>
        void CALL_FN_DOT(ref int code_pointer, ref V4 v4, out byte vector_size, out BlastVariableDataType datatype)
        {
            bool neg1;

            code_pointer++;
            float* d1 = (float*)pop_p_info(ref code_pointer, out datatype, out vector_size, out neg1);

            switch ((BlastVectorSizes)vector_size)
            {
                case 0:
                case BlastVectorSizes.float4: v4.x = math.dot(negate(neg1, ((float4*)d1)[0]), pop_as_f4(ref code_pointer)); break;
                case BlastVectorSizes.float1: v4.x = math.dot(negate(neg1, ((float*)d1)[0]),  pop_as_f1(ref code_pointer)); break;
                case BlastVectorSizes.float2: v4.x = math.dot(negate(neg1, ((float2*)d1)[0]), pop_as_f2(ref code_pointer)); break;
                case BlastVectorSizes.float3: v4.x = math.dot(negate(neg1, ((float3*)d1)[0]), pop_as_f3(ref code_pointer)); break;

                default:
                    {
                        if (IsTrace)
                        {
                            Debug.LogError($"Blast.Interpretor.ssmd.dot: {datatype} vector size '{vector_size}' not supported ");
                        }
                        v4 = float.NaN;
                        vector_size = 0;
                        break;
                    }
            }

            vector_size = 1;
        }


        /// <summary>
        /// cross product of 2 inputs 
        /// - 2 input parameters 
        /// - input vectorsize == 3
        /// - output vectorsize == 3
        /// </summary>
        void CALL_FN_CROSS(ref int code_pointer, ref V4 v4, out byte vector_size, out BlastVariableDataType datatype)
        {
            bool neg1;
            code_pointer++;
            float* d1 = (float*)pop_p_info(ref code_pointer, out datatype, out vector_size, out neg1);

            switch ((BlastVectorSizes)vector_size)
            {
                // in release build we dont need information on the second parameter 
                case BlastVectorSizes.float3: v4.xyz = math.cross(negate(neg1, ((float3*)d1)[0]), pop_as_f3(ref code_pointer)); break;

                default:
                    {
                        if (IsTrace)
                        {
                            Debug.LogError($"Blast.Interpretor.ssmd.cross: {datatype} vector size '{vector_size}' not supported ");
                        }
                        v4 = float.NaN; 
                        vector_size = 0;
                        break;
                    }
            }
        }



    }
}
