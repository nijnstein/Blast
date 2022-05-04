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

namespace NSS.Blast.SSMD
{
    unsafe public partial struct BlastSSMDInterpretor
    {


        /// <summary>
        /// clamp value between operands
        /// </summary>
        /// <remarks>
        /// first min then max => saves 1 memory buffer doing this in sequence (math lib will also do min(max( so this should be equal in performance but turned upside down in flow
        /// <code>
        /// 
        /// a = clamp(a, min, max); 
        /// 
        /// </code>
        /// </remarks>
#if !STANDALONE_VSBUILD
        [SkipLocalsInit]
#endif
        void get_clamp_result([NoAlias] void* temp, ref int code_pointer, ref byte vector_size, int ssmd_datacount, [NoAlias] float4* f4)
        {
            BlastVariableDataType datatype;
            BlastVariableDataType datatype2;
            byte vector_size_2;

            pop_op_meta_cp(code_pointer, out datatype, out vector_size);

#if DEVELOPMENT_BUILD || TRACE
            if (datatype != BlastVariableDataType.Numeric)
            {
                Debug.LogError($"blast.ssmd.interpretor.clamp: datatype|vectorsize mismatch, vector_size: {vector_size}, datatype: {datatype} not supported");
                return;
            }
#endif

            switch (vector_size)
            {
                case 1:
                    {
                        // in case of v1 there is no memory problem 
                        float* fpa = (float*)temp;
                        float* fp_min = &fpa[ssmd_datacount];
                        float* fp_max = &fp_min[ssmd_datacount];

                        pop_fx_into_ref<float>(ref code_pointer, fpa);
                        pop_fx_into_ref<float>(ref code_pointer, fp_min);
                        pop_fx_into_ref<float>(ref code_pointer, fp_max);

                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            register[i].x = math.clamp(fpa[i], fp_min[i], fp_max[i]);
                        }
                    }
                    break;

                case 2:
                    {
                        // 2 float2's fit in temp, split op in 2 to save on the allocation
                        float2* fpa = (float2*)temp;

                        pop_fx_into_ref<float2>(ref code_pointer, fpa);


                        // first the lower bound 
                        pop_op_meta_cp(code_pointer, out datatype2, out vector_size_2);
                        // must be size 1 or 2 
#if DEVELOPMENT_BUILD || TRACE
                        if (vector_size_2 != 1 && vector_size_2 != 2)
                        {
                            Debug.LogError($"blast.ssmd.interpretor.clamp: datatype|vectorsize mismatch, vector_size: {vector_size}, datatype: {datatype}, minmax vectorsize {vector_size_2} not supported");
                            return;
                        }
                        if (datatype != datatype2)
                        {
                            Debug.LogError($"blast.ssmd.interpretor.clamp: datatype|vectorsize mismatch, vector_size: {vector_size}, datatype: {datatype}, minmax datatype = {datatype2}");
                            return;
                        }
#endif
                        if (vector_size_2 == 2)
                        {
                            float2* fp_m = &fpa[ssmd_datacount];
                            pop_fx_into_ref<float2>(ref code_pointer, fp_m);

                            // constrain lower bound 
                            for (int i = 0; i < ssmd_datacount; i++)
                            {
                                fpa[i] = math.max(fpa[i], fp_m[i]);
                            }
                        }
                        else
                        {
                            float* fp_m = (float*)(void*)&fpa[ssmd_datacount];
                            pop_fx_into_ref<float>(ref code_pointer, fp_m);

                            // constrain lower bound 
                            for (int i = 0; i < ssmd_datacount; i++)
                            {
                                fpa[i] = math.max(fpa[i], (float2)fp_m[i]);
                            }
                        }

                        // then the upper bound and write to output 
                        pop_op_meta_cp(code_pointer, out datatype2, out vector_size_2);
#if DEVELOPMENT_BUILD || TRACE
                        if (vector_size_2 != 1 && vector_size_2 != 2)
                        {
                            Debug.LogError($"blast.ssmd.interpretor.clamp: datatype|vectorsize mismatch, vector_size: {vector_size}, datatype: {datatype}, minmax vectorsize {vector_size_2} not supported");
                            return;
                        }
                        if (datatype != datatype2)
                        {
                            Debug.LogError($"blast.ssmd.interpretor.clamp: datatype|vectorsize mismatch, vector_size: {vector_size}, datatype: {datatype}, minmax datatype = {datatype2}");
                            return;
                        }
#endif
                        if (vector_size_2 == 2)
                        {
                            float2* fp_m = &fpa[ssmd_datacount];
                            pop_fx_into_ref<float2>(ref code_pointer, fp_m);

                            // constrain lower bound 
                            for (int i = 0; i < ssmd_datacount; i++)
                            {
                                register[i].xy = math.min(fpa[i], fp_m[i]);
                            }
                        }
                        else
                        {
                            float* fp_m = (float*)(void*)&fpa[ssmd_datacount];
                            pop_fx_into_ref<float>(ref code_pointer, fp_m);

                            // constrain lower bound 
                            for (int i = 0; i < ssmd_datacount; i++)
                            {
                                register[i].xy = math.min(fpa[i], (float2)fp_m[i]);
                            }
                        }
                    }
                    break;

                case 3:
                    {
                        // 2 float3;s wont fit in temp anymore 
                        float3* fpa = (float3*)temp;
                        float3* fp_m = stackalloc float3[ssmd_datacount];


                        pop_fx_into_ref<float3>(ref code_pointer, fpa);

                        // first the lower bound 
                        pop_op_meta_cp(code_pointer, out datatype2, out vector_size_2);
                        // must be size 1 or 3 
#if DEVELOPMENT_BUILD || TRACE
                        if (vector_size_2 != 1 && vector_size_2 != vector_size)
                        {
                            Debug.LogError($"blast.ssmd.interpretor.clamp: datatype|vectorsize mismatch, vector_size: {vector_size}, datatype: {datatype}, minmax vectorsize {vector_size_2} not supported");
                            return;
                        }
                        if (datatype != datatype2)
                        {
                            Debug.LogError($"blast.ssmd.interpretor.clamp: datatype|vectorsize mismatch, vector_size: {vector_size}, datatype: {datatype}, minmax datatype = {datatype2}");
                            return;
                        }
#endif
                        if (vector_size_2 == vector_size)
                        {
                            pop_fx_into_ref<float3>(ref code_pointer, fp_m);

                            // constrain lower bound 
                            for (int i = 0; i < ssmd_datacount; i++)
                            {
                                fpa[i] = math.max(fpa[i], fp_m[i]);
                            }
                        }
                        else
                        {
                            float* fp_m1 = (float*)(void*)fp_m;
                            pop_fx_into_ref<float>(ref code_pointer, fp_m1);

                            // constrain lower bound 
                            for (int i = 0; i < ssmd_datacount; i++)
                            {
                                fpa[i] = math.max(fpa[i], (float3)fp_m1[i]);
                            }
                        }

                        // then the upper bound and write to output 
                        pop_op_meta_cp(code_pointer, out datatype2, out vector_size_2);
#if DEVELOPMENT_BUILD || TRACE
                        if (vector_size_2 != 1 && vector_size_2 != vector_size)
                        {
                            Debug.LogError($"blast.ssmd.interpretor.clamp: datatype|vectorsize mismatch, vector_size: {vector_size}, datatype: {datatype}, minmax vectorsize {vector_size_2} not supported");
                            return;
                        }
                        if (datatype != datatype2)
                        {
                            Debug.LogError($"blast.ssmd.interpretor.clamp: datatype|vectorsize mismatch, vector_size: {vector_size}, datatype: {datatype}, minmax datatype = {datatype2}");
                            return;
                        }
#endif
                        if (vector_size_2 == vector_size)
                        {
                            pop_fx_into_ref<float3>(ref code_pointer, fp_m);

                            // constrain lower bound 
                            for (int i = 0; i < ssmd_datacount; i++)
                            {
                                register[i].xyz = math.min(fpa[i], fp_m[i]);
                            }
                        }
                        else
                        {
                            float* fp_m1 = (float*)(void*)fp_m;
                            pop_fx_into_ref<float>(ref code_pointer, fp_m1);

                            // constrain lower bound 
                            for (int i = 0; i < ssmd_datacount; i++)
                            {
                                register[i].xyz = math.min(fpa[i], (float3)fp_m1[i]);
                            }
                        }
                    }
                    break;

                case 0:
                case 4:
                    {
                        // 2 float4;s wont fit in temp anymore 
                        float4* fpa = (float4*)temp;
                        float4* fp_m = stackalloc float4[ssmd_datacount];

                        pop_fx_into_ref<float4>(ref code_pointer, fpa);

                        // first the lower bound 
                        pop_op_meta_cp(code_pointer, out datatype2, out vector_size_2);
                        // must be size 1 or 4 
#if DEVELOPMENT_BUILD || TRACE
                        if (vector_size_2 != 1 && vector_size_2 != vector_size)
                        {
                            Debug.LogError($"blast.ssmd.interpretor.clamp: datatype|vectorsize mismatch, vector_size: {vector_size}, datatype: {datatype}, minmax vectorsize {vector_size_2} not supported");
                            return;
                        }
                        if (datatype != datatype2)
                        {
                            Debug.LogError($"blast.ssmd.interpretor.clamp: datatype|vectorsize mismatch, vector_size: {vector_size}, datatype: {datatype}, minmax datatype = {datatype2}");
                            return;
                        }
#endif
                        if (vector_size_2 == vector_size)
                        {
                            pop_fx_into_ref<float4>(ref code_pointer, fp_m);

                            // constrain lower bound 
                            for (int i = 0; i < ssmd_datacount; i++)
                            {
                                fpa[i] = math.max(fpa[i], fp_m[i]);
                            }
                        }
                        else
                        {
                            float* fp_m1 = (float*)(void*)fp_m;
                            pop_fx_into_ref<float>(ref code_pointer, fp_m1);

                            // constrain lower bound 
                            for (int i = 0; i < ssmd_datacount; i++)
                            {
                                fpa[i] = math.max(fpa[i], (float4)fp_m1[i]);
                            }
                        }

                        // then the upper bound and write to output 
                        pop_op_meta_cp(code_pointer, out datatype2, out vector_size_2);
#if DEVELOPMENT_BUILD || TRACE
                        if (vector_size_2 != 1 && vector_size_2 != vector_size)
                        {
                            Debug.LogError($"blast.ssmd.interpretor.clamp: datatype|vectorsize mismatch, vector_size: {vector_size}, datatype: {datatype}, minmax vectorsize {vector_size_2} not supported");
                            return;
                        }
                        if (datatype != datatype2)
                        {
                            Debug.LogError($"blast.ssmd.interpretor.clamp: datatype|vectorsize mismatch, vector_size: {vector_size}, datatype: {datatype}, minmax datatype = {datatype2}");
                            return;
                        }
#endif
                        if (vector_size_2 == vector_size)
                        {
                            pop_fx_into_ref<float4>(ref code_pointer, fp_m);

                            // constrain lower bound 
                            for (int i = 0; i < ssmd_datacount; i++)
                            {
                                register[i] = math.min(fpa[i], fp_m[i]);
                            }
                        }
                        else
                        {
                            float* fp_m1 = (float*)(void*)fp_m;
                            pop_fx_into_ref<float>(ref code_pointer, fp_m1);

                            // constrain lower bound 
                            for (int i = 0; i < ssmd_datacount; i++)
                            {
                                register[i] = math.min(fpa[i], (float4)fp_m1[i]);
                            }
                        }
                    }
                    break;

#if DEVELOPMENT_BUILD || TRACE
                default:
                    Debug.LogError($"blast.ssmd.interpretor.clamp: datatype|vectorsize mismatch, vector_size: {vector_size}, datatype: {datatype} not supported");
                    break;
#endif
            }

            code_pointer--;
        }






    }
}
