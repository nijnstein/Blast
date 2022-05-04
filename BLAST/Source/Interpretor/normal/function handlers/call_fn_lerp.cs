//############################################################################################################################
//                                                                                                                           #
//  ██████╗ ██╗      █████╗ ███████╗████████╗                           Copyright © 2022 Rob Lemmens | NijnStein Software    #
//  ██╔══██╗██║     ██╔══██╗██╔════╝╚══██╔══╝                                    <rob.lemmens.s31 gmail com>                 #
//  ██████╔╝██║     ███████║███████╗   ██║                                           All Rights Reserved                     #
//  ██╔══██╗██║     ██╔══██║╚════██║   ██║                                                                                   #
//  ██████╔╝███████╗██║  ██║███████║   ██║     V1.0.4e                                                                       #
//  ╚═════╝ ╚══════╝╚═╝  ╚═╝╚══════╝   ╚═╝                                                                                   #
//                                                                                                                           #
//############################################################################################################################
//                                                                                                                     (__)  #
//       Unauthorized copying of this file, via any medium is strictly prohibited proprietary and confidential         (oo)  #
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
        /// linear interpolation from a to b at step c 
        /// 
        /// - 3 inputs:   3 vectors equal size or first 2 equal and last = 1 
        /// - only numeric input
        /// 
        /// - lerp((1 2), (10 20), 0.5); 
        /// - lerp((1 2), (10 20), (0.5 0.1)); 
        /// 
        /// returns vector size of input values 
        /// </summary>
        void CALL_FN_LERP(ref int code_pointer, ref V4 f4, out byte vector_size, out BlastVariableDataType datatype)
        {
            BlastVariableDataType p2_datatype; byte p2_vector_size;
            BlastVariableDataType p3_datatype; byte p3_vector_size;
            bool neg1, neg2, neg3;

            code_pointer++;
            void* p1 = pop_p_info(ref code_pointer, out datatype, out vector_size, out neg1);
            void* p2 = pop_p_info(ref code_pointer, out p2_datatype, out p2_vector_size, out neg2);
            void* p3 = pop_p_info(ref code_pointer, out p3_datatype, out p3_vector_size, out neg3);


#if DEVELOPMENT_BUILD || TRACE
            if (datatype != p2_datatype || vector_size != p2_vector_size)
            {
                Debug.LogError($"Blast.Interpretor.lerp: parameter type mismatch, min = {datatype}.{vector_size}, max = {p2_datatype}.{p2_vector_size} at codepointer {code_pointer}, min/max vectorsizes must be equal");
                return;
            }
            if (vector_size != p3_vector_size && p3_vector_size != 1)
            {
                Debug.LogError($"Blast.Interpretor.lerp: parameter type mismatch, min/max = {datatype}.{vector_size}, c = {p3_datatype}.{p3_vector_size} at codepointer {code_pointer}, if position is not a scalar then min/max vectorsize must be equal to position vectorsize");
                return;
            }
#endif

            switch (datatype.Combine(vector_size, p3_vector_size == 1))
            {
                case BlastVectorType.float4_n: f4 = math.lerp(negate(neg1, ((float4*)p1)[0]), negate(neg2, ((float4*)p2)[0]), negate(neg3, ((float*)p3)[0])); vector_size = 4; break;
                case BlastVectorType.float1_n: f4.x = math.lerp(negate(neg1, ((float*)p1)[0]), negate(neg2, ((float*)p2)[0]), negate(neg3, ((float*)p3)[0])); break;
                case BlastVectorType.float2_n: f4.xy = math.lerp(negate(neg1, ((float2*)p1)[0]), negate(neg2, ((float2*)p2)[0]), negate(neg3, ((float*)p3)[0])); break;
                case BlastVectorType.float3_n: f4.xyz = math.lerp(negate(neg1, ((float3*)p1)[0]), negate(neg2, ((float3*)p2)[0]), negate(neg3, ((float*)p3)[0])); break;

                case 0:
                case BlastVectorType.float4: f4 = math.lerp(negate(neg1, ((float4*)p1)[0]), negate(neg2, ((float4*)p2)[0]), negate(neg3, ((float4*)p3)[0])); vector_size = 4; break;
                case BlastVectorType.float1: f4.x = math.lerp(negate(neg1, ((float*)p1)[0]), negate(neg2, ((float*)p2)[0]), negate(neg3, ((float*)p3)[0])); break;
                case BlastVectorType.float2: f4.xy = math.lerp(negate(neg1, ((float2*)p1)[0]), negate(neg2, ((float2*)p2)[0]), negate(neg3, ((float2*)p3)[0])); break;
                case BlastVectorType.float3: f4.xyz = math.lerp(negate(neg1, ((float3*)p1)[0]), negate(neg2, ((float3*)p2)[0]), negate(neg3, ((float3*)p3)[0])); break;


#if DEVELOPMENT_BUILD || TRACE
                default:
                    Debug.LogError($"Blast.Interpretor.lerp: datatype {datatype} with vector_size {vector_size} not supported");
                    break;
#endif
            }
        }



        /// <summary>
        /// spherical linear interpolation 
        /// - 2 quaternion + float in
        /// returns float4/quaternion
        /// </summary>
        void CALL_FN_SLERP(ref int code_pointer, ref V4 f4, out byte vector_size, out BlastVariableDataType datatype)
        {
            BlastVariableDataType p2_datatype; byte p2_vector_size;
            BlastVariableDataType p3_datatype; byte p3_vector_size;
            bool neg1, neg2, neg3;

            code_pointer++;
            void* p1 = pop_p_info(ref code_pointer, out datatype, out vector_size, out neg1);
            void* p2 = pop_p_info(ref code_pointer, out p2_datatype, out p2_vector_size, out neg2);
            void* p3 = pop_p_info(ref code_pointer, out p3_datatype, out p3_vector_size, out neg3);

#if DEVELOPMENT_BUILD || TRACE
            if (datatype != p2_datatype || vector_size != p2_vector_size)
            {
                Debug.LogError($"Blast.Interpretor.slerp: parameter type mismatch, min = {datatype}.{vector_size}, max = {p2_datatype}.{p2_vector_size} at codepointer {code_pointer}, only float4/quaternions are supported in slerp");
                return;
            }
            if (vector_size != p3_vector_size && p3_vector_size != 1)
            {
                Debug.LogError($"Blast.Interpretor.slerp: parameter type mismatch, min/max = {datatype}.{vector_size}, c = {p3_datatype}.{p3_vector_size} at codepointer {code_pointer}, if position is not a scalar then min/max vectorsize must be equal to position vectorsize");
                return;
            }
            if (neg1 || neg2)
            {
                Debug.LogError($"Blast.Interpretor.slerp: negation of quaternions, min/max = {datatype}.{vector_size}, c = {p3_datatype}.{p3_vector_size} at codepointer {code_pointer}, if position is not a scalar then min/max vectorsize must be equal to position vectorsize");
                return;
            }
#endif

            switch (datatype.Combine(vector_size, p3_vector_size == 1))
            {
                case BlastVectorType.float4_n: f4 = math.slerp(((quaternion*)p1)[0], ((quaternion*)p2)[0], negate(neg3, ((float*)p3)[0])).value; vector_size = 4; break;

#if DEVELOPMENT_BUILD || TRACE
                default:
                    Debug.LogError($"Blast.Interpretor.slerp: datatype {datatype} with vector_size {vector_size} not supported");
                    break;
#endif
            }
        }

        /// <summary>
        /// normalized linear interpolation 
        /// - 2 quaternion + float in
        /// returns float4/quaternion
        /// </summary>
        void CALL_FN_NLERP(ref int code_pointer, ref V4 f4, out byte vector_size, out BlastVariableDataType datatype)
        {
            BlastVariableDataType p2_datatype; byte p2_vector_size;
            BlastVariableDataType p3_datatype; byte p3_vector_size;
            bool neg1, neg2, neg3;

            code_pointer++;
            void* p1 = pop_p_info(ref code_pointer, out datatype, out vector_size, out neg1);
            void* p2 = pop_p_info(ref code_pointer, out p2_datatype, out p2_vector_size, out neg2);
            void* p3 = pop_p_info(ref code_pointer, out p3_datatype, out p3_vector_size, out neg3);

#if DEVELOPMENT_BUILD || TRACE
            if (datatype != p2_datatype || vector_size != p2_vector_size)
            {
                Debug.LogError($"Blast.Interpretor.nlerp: parameter type mismatch, min = {datatype}.{vector_size}, max = {p2_datatype}.{p2_vector_size} at codepointer {code_pointer}, only float4/quaternions are supported in slerp");
                return;
            }
            if (vector_size != p3_vector_size && p3_vector_size != 1)
            {
                Debug.LogError($"Blast.Interpretor.nlerp: parameter type mismatch, min/max = {datatype}.{vector_size}, c = {p3_datatype}.{p3_vector_size} at codepointer {code_pointer}, if position is not a scalar then min/max vectorsize must be equal to position vectorsize");
                return;
            }
            if (neg1 || neg2)
            {
                Debug.LogError($"Blast.Interpretor.nlerp: negation of quaternions, min/max = {datatype}.{vector_size}, c = {p3_datatype}.{p3_vector_size} at codepointer {code_pointer}, if position is not a scalar then min/max vectorsize must be equal to position vectorsize");
                return;
            }
#endif

            switch (datatype.Combine(vector_size, p3_vector_size == 1))
            {
                case BlastVectorType.float4_n: f4 = math.nlerp(((quaternion*)p1)[0], ((quaternion*)p2)[0], negate(neg3, ((float*)p3)[0])).value; vector_size = 4; break;

#if DEVELOPMENT_BUILD || TRACE
                default:
                    Debug.LogError($"Blast.Interpretor.nlerp: datatype {datatype} with vector_size {vector_size} not supported");
                    break;
#endif
            }
        }


        /// <summary>
        /// normalize between minmax -> undo lerp
        /// 
        /// - 3 inputs:   3 vectors equal size or first 2 equal and last = 1 
        /// - only numeric input
        /// 
        /// - ulerp((1 2), (10 20), 0.5); 
        /// - ulerp((1 2), (10 20), (0.5 0.1)); 
        /// 
        /// returns vector size of input values 
        /// </summary>
        void CALL_FN_UNLERP(ref int code_pointer, ref V4 f4, out byte vector_size, out BlastVariableDataType datatype)
        {
            BlastVariableDataType p2_datatype; byte p2_vector_size;
            BlastVariableDataType p3_datatype; byte p3_vector_size;
            bool neg1, neg2, neg3;

            code_pointer++;
            void* p1 = pop_p_info(ref code_pointer, out datatype, out vector_size, out neg1);
            void* p2 = pop_p_info(ref code_pointer, out p2_datatype, out p2_vector_size, out neg2);
            void* p3 = pop_p_info(ref code_pointer, out p3_datatype, out p3_vector_size, out neg3);

#if DEVELOPMENT_BUILD || TRACE
            if (datatype != p2_datatype || vector_size != p2_vector_size)
            {
                Debug.LogError($"Blast.Interpretor.unlerp: parameter type mismatch, min = {datatype}.{vector_size}, max = {p2_datatype}.{p2_vector_size} at codepointer {code_pointer}, min/max vectorsizes must be equal");
                return;
            }
            if (datatype != BlastVariableDataType.Numeric)
            {
                Debug.LogError($"Blast.Interpretor.unlerp: ID datatype not supported");
                return;
            }
            if (vector_size != p3_vector_size && p3_vector_size != 1)
            {
                Debug.LogError($"Blast.Interpretor.unlerp: parameter type mismatch, min/max = {datatype}.{vector_size}, c = {p3_datatype}.{p3_vector_size} at codepointer {code_pointer}, if position is not a scalar then min/max vectorsize must be equal to position vectorsize");
                return;
            }
#endif

            switch (datatype.Combine(vector_size, p3_vector_size == 1))
            {
                case BlastVectorType.float4_n: f4 = math.unlerp(negate(neg1, ((float4*)p1)[0]), negate(neg2, ((float4*)p2)[0]), negate(neg3, ((float*)p3)[0])); vector_size = 4; break;
                case BlastVectorType.float1_n: f4.x = math.unlerp(negate(neg1, ((float*)p1)[0]), negate(neg2, ((float*)p2)[0]), negate(neg3, ((float*)p3)[0])); break;
                case BlastVectorType.float2_n: f4.xy = math.unlerp(negate(neg1, ((float2*)p1)[0]), negate(neg2, ((float2*)p2)[0]), negate(neg3, ((float*)p3)[0])); break;
                case BlastVectorType.float3_n: f4.xyz = math.unlerp(negate(neg1, ((float3*)p1)[0]), negate(neg2, ((float3*)p2)[0]), negate(neg3, ((float*)p3)[0])); break;

                case BlastVectorType.float4: f4 = math.unlerp(negate(neg1, ((float4*)p1)[0]), negate(neg2, ((float4*)p2)[0]), negate(neg3, ((float4*)p3)[0])); vector_size = 4; break;
                case BlastVectorType.float1: f4.x = math.unlerp(negate(neg1, ((float*)p1)[0]), negate(neg2, ((float*)p2)[0]), negate(neg3, ((float*)p3)[0])); break;
                case BlastVectorType.float2: f4.xy = math.unlerp(negate(neg1, ((float2*)p1)[0]), negate(neg2, ((float2*)p2)[0]), negate(neg3, ((float2*)p3)[0])); break;
                case BlastVectorType.float3: f4.xyz = math.unlerp(negate(neg1, ((float3*)p1)[0]), negate(neg2, ((float3*)p2)[0]), negate(neg3, ((float3*)p3)[0])); break;

#if DEVELOPMENT_BUILD || TRACE
                default:
                    Debug.LogError($"Blast.Interpretor.unlerp: datatype {datatype} with vector_size {vector_size} not supported with option datatype {p3_datatype} vectorsize {p3_vector_size}");
                    break;
#endif

            }
        }


        /// <summary>
        /// math.remap - remap(a, b, c, d, e)   => remap e from range ab to cd    
        /// </summary>
        void CALL_FN_REMAP(ref int code_pointer, ref V4 f4, out byte vector_size, out BlastVariableDataType datatype)
        {
            BlastVariableDataType p2_datatype; byte p2_vector_size;
            BlastVariableDataType p3_datatype; byte p3_vector_size;
            BlastVariableDataType p4_datatype; byte p4_vector_size;
            BlastVariableDataType p5_datatype; byte p5_vector_size;
            bool neg1, neg2, neg3, neg4, neg5;

            code_pointer++;
            void* p1 = pop_p_info(ref code_pointer, out datatype, out vector_size, out neg1);
            void* p2 = pop_p_info(ref code_pointer, out p2_datatype, out p2_vector_size, out neg2);
            void* p3 = pop_p_info(ref code_pointer, out p3_datatype, out p3_vector_size, out neg3);
            void* p4 = pop_p_info(ref code_pointer, out p4_datatype, out p4_vector_size, out neg4);
            void* p5 = pop_p_info(ref code_pointer, out p5_datatype, out p5_vector_size, out neg5);

#if DEVELOPMENT_BUILD || TRACE
            if (datatype != p2_datatype || vector_size != p2_vector_size)
            {
                Debug.LogError($"Blast.Interpretor.remap: parameter type mismatch, min = {datatype}.{vector_size}, max = {p2_datatype}.{p2_vector_size} at codepointer {code_pointer}, min/max vectorsizes must be equal");
                return;
            }
            if (datatype != BlastVariableDataType.Numeric)
            {
                Debug.LogError($"Blast.Interpretor.remap: ID datatype not supported");
                return;
            }
            if (vector_size != p3_vector_size)
            {
                Debug.LogError($"Blast.Interpretor.remap: parameter type mismatch, min/max = {datatype}.{vector_size}, c = {p3_datatype}.{p3_vector_size} at codepointer {code_pointer}, if position is not a scalar then min/max vectorsize must be equal to position vectorsize");
                return;
            }
            if (vector_size != p4_vector_size)
            {                                                                                
                Debug.LogError($"Blast.Interpretor.remap: parameter type mismatch, min/max = {datatype}.{vector_size}, d = {p4_datatype}.{p4_vector_size} at codepointer {code_pointer}, if position is not a scalar then min/max vectorsize must be equal to position vectorsize");
                return;
            }
            if (vector_size != p5_vector_size)
            {
                Debug.LogError($"Blast.Interpretor.remap: parameter type mismatch, min/max = {datatype}.{vector_size}, e = {p5_datatype}.{p5_vector_size} at codepointer {code_pointer}, if position is not a scalar then min/max vectorsize must be equal to position vectorsize");
                return;
            }
#endif

            switch ((BlastVectorSizes)vector_size)
            {
                case 0:
                case BlastVectorSizes.float4: f4 = math.remap(negate(neg1, ((float4*)p1)[0]), negate(neg2, ((float4*)p2)[0]), negate(neg3, ((float4*)p3)[0]), negate(neg4, ((float4*)p4)[0]), negate(neg5, ((float4*)p5)[0])); vector_size = 4; break;
                case BlastVectorSizes.float1: f4.x = math.remap(negate(neg1, ((float*)p1)[0]), negate(neg2, ((float*)p2)[0]), negate(neg3, ((float*)p3)[0]), negate(neg4, ((float*)p4)[0]), negate(neg5, ((float*)p5)[0])); break;
                case BlastVectorSizes.float2: f4.xy = math.remap(negate(neg1, ((float2*)p1)[0]), negate(neg2, ((float2*)p2)[0]), negate(neg3, ((float2*)p3)[0]), negate(neg4, ((float2*)p4)[0]), negate(neg5, ((float2*)p5)[0])); break;
                case BlastVectorSizes.float3: f4.xyz = math.remap(negate(neg1, ((float3*)p1)[0]), negate(neg2, ((float3*)p2)[0]), negate(neg3, ((float3*)p3)[0]), negate(neg4, ((float3*)p5)[0]), negate(neg5, ((float3*)p5)[0])); break;
#if DEVELOPMENT_BUILD || TRACE
                default:
                    Debug.LogError($"Blast.Interpretor.remap: datatype {datatype} with vector_size {vector_size} not supported");
                    break;
#endif
            }
        }


    }
}
