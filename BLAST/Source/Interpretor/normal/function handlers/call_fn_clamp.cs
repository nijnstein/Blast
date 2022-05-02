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
        /// clamp a value between 2 others: math.clamp(a, min, max) 
        /// - 3 parameters - equal vectorsize or min-max == 1 and a > 1
        /// - input = output vectorsize 
        /// </summary>
        void CALL_FN_CLAMP(ref int code_pointer, ref V4 f4, out byte vector_size, out BlastVariableDataType datatype)
        {
            byte vector_size_mm;
            BlastVariableDataType datatype_mm;
            bool neg1, neg2, neg3;

            code_pointer += 1;
            void* pa = pop_p_info(ref code_pointer, out datatype, out vector_size, out neg3);
            void* p1 = pop_p_info(ref code_pointer, out datatype_mm, out vector_size_mm, out neg1);
            void* p2 = pop_p_info(ref code_pointer, out datatype, out vector_size, out neg2);       // could optimize this to pop_fx in release but not much win

#if DEVELOPMENT_BUILD || TRACE
            if (datatype_mm != datatype || vector_size_mm != vector_size)
            {
                Debug.LogError($"Blast.Interpretor.clamp: parameter type mismatch, p1 = {datatype_mm}.{vector_size_mm}, p2 = {datatype}.{vector_size} at codepointer {code_pointer}");
                return;
            }
            if (vector_size_mm != vector_size && vector_size_mm != 1)
            {
                Debug.LogError($"Blast.Interpretor.clamp: parameter type mismatch, min/max = {datatype_mm}.{vector_size_mm}, value = {datatype}.{vector_size} at codepointer {code_pointer}, vectorsizes must be equal or minmax must have size 1");
                return;
            }
#endif

            // vectorsize of minmax range => either equal or 1 
            switch (datatype.Combine(vector_size, vector_size_mm == 1))
            {

                case BlastVectorType.float4_n: f4 = math.clamp(negate(neg3, ((float4*)pa)[0]), negate(neg1, ((float*)p1)[0]), negate(neg2, ((float*)p2)[0])); vector_size_mm = 4; vector_size = 4; break;
                case BlastVectorType.float3_n: f4.xyz = math.clamp(negate(neg3, ((float3*)pa)[0]), negate(neg1, ((float*)p1)[0]), negate(neg2, ((float*)p2)[0])); vector_size = 3; break;
                case BlastVectorType.float2_n: f4.xy = math.clamp(negate(neg3, ((float2*)pa)[0]), negate(neg1, ((float*)p1)[0]), negate(neg2, ((float*)p2)[0])); vector_size = 2; break;
                case BlastVectorType.float1_n: f4.x = math.clamp(negate(neg3, ((float*)pa)[0]), negate(neg1, ((float*)p1)[0]), negate(neg2, ((float*)p2)[0])); vector_size = 1; break;


                case BlastVectorType.float4: f4 = math.clamp(negate(neg3, ((float4*)pa)[0]), negate(neg1, ((float4*)p1)[0]), negate(neg2, ((float4*)p2)[0])); vector_size_mm = 4; break;
                case BlastVectorType.float3: f4.xyz = math.clamp(negate(neg3, ((float3*)pa)[0]), negate(neg1, ((float3*)p1)[0]), negate(neg2, ((float3*)p2)[0])); vector_size = 3; break;
                case BlastVectorType.float2: f4.xy = math.clamp(negate(neg3, ((float2*)pa)[0]), negate(neg1, ((float2*)p1)[0]), negate(neg2, ((float2*)p2)[0])); vector_size = 2; break;
                case BlastVectorType.float1: f4.x = math.clamp(negate(neg3, ((float*)pa)[0]), negate(neg1, ((float*)p1)[0]), negate(neg2, ((float*)p2)[0])); vector_size = 1; break;

                default:
                    {
#if DEVELOPMENT_BUILD || TRACE
                        Debug.LogError($"Blast.Interpretor.clamp: datatype {datatype} with vector size '{vector_size}' not supported ");
#endif
                        break;
                    }
            }
        }

    }
}
