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
using System.Runtime.CompilerServices;

using V1 = System.Single;
using V2 = Unity.Mathematics.float2;
using V3 = Unity.Mathematics.float3;
using V4 = Unity.Mathematics.float4;

namespace NSS.Blast.Interpretor
{
    unsafe public partial struct BlastInterpretor
    {


#if STANDALONE_VSBUILD || ENABLE_IL2CPP
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        void CALL_FN_EULER(ref int code_pointer, ref V4 v4, out byte vector_size, out BlastVariableDataType datatype, math.RotationOrder rotation_order = math.RotationOrder.XYZ)
        {
            code_pointer += 1;
            void* fdata = (void*)pop_p_info(ref code_pointer, out datatype, out vector_size, out bool is_negated);

            switch (datatype.Combine(vector_size, is_negated))
            {
                case BlastVectorType.float4: v4.xyz = mathex.Euler(((float4*)fdata)[0], rotation_order); break;
                case BlastVectorType.float4_n: v4.xyz = mathex.Euler(-((float4*)fdata)[0], rotation_order); break;

#if DEVELOPMENT_BUILD || TRACE
                default:
                    {
                        Debug.LogError($"Blast.Interpretor.EULER{rotation_order}: vectorsize {vector_size}, datatype {datatype} not supported");
                        break;
                    }
#endif
            }

            // regardless the input 
            vector_size = 3;
        }



#if STANDALONE_VSBUILD || ENABLE_IL2CPP
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        void CALL_FN_EULER(ref int code_pointer, ref V4 v4, out byte vector_size, out BlastVariableDataType datatype)
        {
            code_pointer += 1;
            void* fdata = (void*)pop_p_info(ref code_pointer, out datatype, out vector_size, out bool is_negated);

            int rotation_order = pop_as_i1(ref code_pointer);

#if TRACE
            // validate rotation order 
            if (rotation_order < 0 || rotation_order > 5)
            {
                Debug.LogError($"Blast.Interpretor.EULER: invalid rotation order {rotation_order}");
                return; 
            }
#endif 

            switch (datatype.Combine(vector_size, is_negated))
            {
                case BlastVectorType.float4: v4.xyz = mathex.Euler(((float4*)fdata)[0], (math.RotationOrder)rotation_order); break;
                case BlastVectorType.float4_n: v4.xyz = mathex.Euler(-((float4*)fdata)[0], (math.RotationOrder)rotation_order); break;

#if DEVELOPMENT_BUILD || TRACE
                default:
                    {
                        Debug.LogError($"Blast.Interpretor.EULER{rotation_order}: vectorsize {vector_size}, datatype {datatype} not supported");
                        break;
                    }
#endif
            }

            // regardless the input 
            vector_size = 3;
        }




#if STANDALONE_VSBUILD || ENABLE_IL2CPP
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        void CALL_FN_QUATERNION(ref int code_pointer, ref V4 v4, out byte vector_size, out BlastVariableDataType datatype, math.RotationOrder rotation_order = math.RotationOrder.XYZ)
        {
            code_pointer += 1;
            void* fdata = (void*)pop_p_info(ref code_pointer, out datatype, out vector_size, out bool is_negated);

            switch (datatype.Combine(vector_size, is_negated))
            {
               case BlastVectorType.float3: v4 = quaternion.Euler(((float3*)fdata)[0], rotation_order).value; break;
               case BlastVectorType.float3_n: v4 = quaternion.Euler(-((float3*)fdata)[0], rotation_order).value; break;

#if DEVELOPMENT_BUILD || TRACE
                default:
                    {
                        Debug.LogError($"Blast.Interpretor.QUATERNION{rotation_order}: vectorsize {vector_size}, datatype {datatype} not supported");
                        break;
                    }
#endif
            }

            // regardless the input 
            vector_size = 4;
        }



#if STANDALONE_VSBUILD || ENABLE_IL2CPP
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        void CALL_FN_QUATERNION(ref int code_pointer, ref V4 v4, out byte vector_size, out BlastVariableDataType datatype)
        {
            bool is_negated;
            code_pointer += 1;
            void* fdata = (void*)pop_p_info(ref code_pointer, out datatype, out vector_size, out is_negated);

            int rotation_order = pop_as_i1(ref code_pointer);

#if TRACE
            // validate rotation order 
            if (rotation_order < 0 || rotation_order > 5)
            {
                Debug.LogError($"Blast.Interpretor.QUATERNION: invalid rotation order {rotation_order}");
                return;
            }
#endif 

            switch (datatype.Combine(vector_size, is_negated))
            {
                case BlastVectorType.float3: v4 = quaternion.Euler(((float3*)fdata)[0], (math.RotationOrder)rotation_order).value; break;
                case BlastVectorType.float3_n: v4 = quaternion.Euler(-((float3*)fdata)[0], (math.RotationOrder)rotation_order).value; break;

#if DEVELOPMENT_BUILD || TRACE
                default:
                    {
                        Debug.LogError($"Blast.Interpretor.QUATERNIOON{rotation_order}: vectorsize {vector_size}, datatype {datatype} not supported");
                        break;
                    }
#endif
            }

            // regardless the input 
            vector_size = 4;
        }





#if STANDALONE_VSBUILD || ENABLE_IL2CPP
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        void CALL_FN_MUL(ref int code_pointer, ref V4 v4, out byte vector_size, out BlastVariableDataType datatype)
        {
            code_pointer += 1;

            void* fdata = (void*)pop_p_info(ref code_pointer, out datatype, out vector_size, out bool is_negated);

            void* fdata2 = (void*)pop_p_info(ref code_pointer, out BlastVariableDataType datatype2, out byte vector_size2, out bool is_negated2);

            switch (datatype.Combine(vector_size, false))
            {
                case BlastVectorType.float4:
                    float4 f4 = ((float4*)fdata)[0];
                    f4 = math.select(f4, -f4, is_negated);

                    switch (datatype2.Combine(vector_size2, is_negated2))
                    {
                        case BlastVectorType.float4: v4 = math.mul(new quaternion(f4), new quaternion(((float4*)fdata2)[0])).value; vector_size = 4; break;
                        case BlastVectorType.float3: v4.xyz = math.mul(new quaternion(f4), ((float3*)fdata2)[0]); vector_size = 3; break;
                        case BlastVectorType.float4_n: v4 = math.mul(new quaternion(f4), -((float4*)fdata2)[0]).value; vector_size = 4; break;
                        case BlastVectorType.float3_n: v4.xyz = math.mul(new quaternion(f4), -((float3*)fdata2)[0]); vector_size = 3; break;
                    }
                    break;

#if DEVELOPMENT_BUILD || TRACE
                default:
                    {
                        Debug.LogError($"Blast.Interpretor.MUL: vectorsize {vector_size} x {vector_size2}, datatype {datatype} x {datatype2} not supported");
                        break;
                    }
#endif
            }

            // regardless the input 
            datatype = BlastVariableDataType.Numeric;
        }



#if STANDALONE_VSBUILD || ENABLE_IL2CPP
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        void CALL_FN_ROTATE(ref int code_pointer, ref V4 v4, out byte vector_size, out BlastVariableDataType datatype, math.RotationOrder rotation_order = math.RotationOrder.XYZ)
        {
            code_pointer += 1;
            void* fdata = (void*)pop_p_info(ref code_pointer, out datatype, out vector_size, out bool is_negated);

            switch (datatype.Combine(vector_size, is_negated))
            {
                case BlastVectorType.float4: v4.xyz = math.rotate(new quaternion(((float4*)fdata)[0]), pop_as_f3(ref code_pointer)); vector_size = 3; break;
                case BlastVectorType.float4_n: v4.xyz = math.rotate(new quaternion(-((float4*)fdata)[0]), pop_as_f3(ref code_pointer)); vector_size = 3; break;

#if DEVELOPMENT_BUILD || TRACE
                default:
                    {
                        Debug.LogError($"Blast.Interpretor.ROTATE{rotation_order}: vectorsize {vector_size}, datatype {datatype} not supported");
                        break;
                    }
#endif
            }
            datatype = BlastVariableDataType.Numeric;
        }


#if STANDALONE_VSBUILD || ENABLE_IL2CPP
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        void CALL_FN_ANGLE(ref int code_pointer, ref V4 v4, out byte vector_size, out BlastVariableDataType datatype, math.RotationOrder rotation_order = math.RotationOrder.XYZ)
        {
            code_pointer += 1;
            void* fdata = (void*)pop_p_info(ref code_pointer, out datatype, out vector_size, out bool is_negated);

            switch (datatype.Combine(vector_size, is_negated))
            {
                case BlastVectorType.float4: v4 = mathex.angle(((float4*)fdata)[0], pop_as_f4(ref code_pointer)); break;
                case BlastVectorType.float4_n: v4 = mathex.angle(-((float4*)fdata)[0], pop_as_f4(ref code_pointer)); break;

#if DEVELOPMENT_BUILD || TRACE
                default:
                    {
                        Debug.LogError($"Blast.Interpretor.ANGLE{rotation_order}: vectorsize {vector_size}, datatype {datatype} not supported");
                        break;
                    }
#endif
            }

            // regardless the input 
            vector_size = 4;
        }

    


    }






}
