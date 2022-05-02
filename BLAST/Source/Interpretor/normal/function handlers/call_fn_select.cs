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
        /// 3 inputs, first 2 any type, 3rd type equal or scalar bool
        /// select(a, b, condition)
        /// - input = outputvectorsize
        /// - 1 output  
        /// </summary>
        void CALL_FN_SELECT(ref int code_pointer, ref V4 f4, out byte vector_size, out BlastVariableDataType datatype)
        {
            BlastVariableDataType datatype2;
            BlastVariableDataType datatype_condition;
            byte vsize_condition, vsize2;
            bool neg1, neg2;


            code_pointer++;
            void* p1 = pop_p_info(ref code_pointer, out datatype, out vector_size, out neg1);

            // we cant skip popping the second with information on release builds because our stackpointer could stray
            void* p2 = pop_p_info(ref code_pointer, out datatype2, out vsize2, out neg2);

            void* pcondition = pop_p_info(ref code_pointer, out datatype_condition, out vsize_condition, out bool neg3);// dont care -1 = stil true

#if DEVELOPMENT_BUILD || TRACE
            if (datatype != datatype2 || vector_size != vsize2)
            {
                Debug.LogError($"Blast.Interpretor.select: parameter type mismatch, p1 = {datatype}.{vector_size}, p2 = {datatype_condition}.{vsize_condition} at codepointer {code_pointer}");
                return;
            }
            if (vector_size != vsize_condition && vsize_condition != 1)
            {
                Debug.LogError($"Blast.Interpretor.select: condition parameter type mismatch, p1 = {datatype}.{vector_size}, p2 = {datatype_condition}.{vsize_condition} at codepointer {code_pointer}, vectorsizes must be equal or conditional must have size 1");
                return;
            }
#endif

            switch (datatype.Combine(vector_size, vsize_condition == 1))
            {
                // vectorsize 1 condition, negation handled seperate
                case BlastVectorType.float4_n: f4 = math.select(negate(neg1, ((float4*)p1)[0]), negate(neg2, ((float4*)p2)[0]), ((float*)pcondition)[0] != 0); vector_size = 4; break;
                case BlastVectorType.float3_n: f4.xyz = math.select(negate(neg1, ((float3*)p1)[0]), negate(neg2, ((float3*)p2)[0]), ((float*)pcondition)[0] != 0); break;
                case BlastVectorType.float2_n: f4.xy = math.select(negate(neg1, ((float2*)p1)[0]), negate(neg2, ((float2*)p2)[0]), ((float*)pcondition)[0] != 0); break;
                case BlastVectorType.float1_n: f4.x = math.select(negate(neg1, ((float*)p1)[0]), negate(neg2, ((float*)p2)[0]), ((float*)pcondition)[0] != 0); break;

                // equal vectorsize condition 
                case BlastVectorType.float4: f4 = math.select(negate(neg1, ((float4*)p1)[0]), negate(neg2, ((float4*)p2)[0]), ((float4*)pcondition)[0] != 0); vector_size = 4; break;
                case BlastVectorType.float3: f4.xyz = math.select(negate(neg1, ((float3*)p1)[0]), negate(neg2, ((float3*)p2)[0]), ((float3*)pcondition)[0] != 0); break;
                case BlastVectorType.float2: f4.xy = math.select(negate(neg1, ((float2*)p1)[0]), negate(neg2, ((float2*)p2)[0]), ((float2*)pcondition)[0] != 0); break;
                case BlastVectorType.float1: f4.x = math.select(negate(neg1, ((float*)p1)[0]), negate(neg2, ((float*)p2)[0]), ((float*)pcondition)[0] != 0); break;

                default:
                    {
#if DEVELOPMENT_BUILD || TRACE
                        Debug.LogError($"Blast.Interpretor.select: datatype {datatype} with vector size {vector_size} not supported ");
#endif
                        break;
                    }

            }
        }
    }
}
