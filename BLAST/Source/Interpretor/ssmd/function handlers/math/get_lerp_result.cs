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
        /// lerp value from min to max 
        /// </summary>
        /// <remarks>
        /// 
        /// a = lerp(min, max, s); 
        /// 
        /// TODO :     for all 3 input functions:   we should test stackallocs if they are conditional or not
        /// - if they are always allocated move every case with stackalloc into its own function ? code explosin without templates/generics
        ///  
        /// </remarks>
#if !STANDALONE_VSBUILD
        [SkipLocalsInit]
#endif
        void get_lerp_result([NoAlias] void* temp, ref int code_pointer, ref byte vector_size, int ssmd_datacount, [NoAlias] float4* f4)
        {
            BlastVariableDataType datatype;
            pop_op_meta_cp(code_pointer, out datatype, out vector_size);

#if DEVELOPMENT_BUILD || TRACE
            if (datatype != BlastVariableDataType.Numeric)
            {
                Debug.LogError($"blast.ssmd.interpretor.lerp: datatype|vectorsize mismatch, vector_size: {vector_size}, datatype: {datatype} not supported");
                return;
            }
#endif

            //
            // NOTE the third parameter might have a different vectorsize, if different is MUST be size 1
            //
            BlastVariableDataType p_datatype;
            byte p_vector_size;


            switch (vector_size)
            {
                case 1:
                    {
                        // in case of v1 there is no memory problem 
                        float* fpa = (float*)temp;
                        float* fp_min = &fpa[ssmd_datacount];
                        float* fp_max = &fp_min[ssmd_datacount];

                        pop_fx_into_ref<float>(ref code_pointer, fp_min);
                        pop_fx_into_ref<float>(ref code_pointer, fp_max);

                        // the third param will always equal size 1
                        pop_fx_into_ref<float>(ref code_pointer, fpa);


                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            register[i].x = math.lerp(fp_min[i], fp_max[i], fpa[i]);
                        }
                    }
                    break;

                case 2:
                    {
                        float2* fpa = (float2*)temp;
                        float2* fp_min = &fpa[ssmd_datacount];
                        float2* fp_max = stackalloc float2[ssmd_datacount];

                        pop_fx_into_ref<float2>(ref code_pointer, fp_min);
                        pop_fx_into_ref<float2>(ref code_pointer, fp_max);

                        pop_op_meta_cp(code_pointer, out p_datatype, out p_vector_size);


                        // c param == size 1
                        if (p_vector_size == 1)
                        {
                            // just cast the first piece of temp to a float1, the second half is used for min
                            pop_fx_into_ref<float>(ref code_pointer, (float*)temp);

                            for (int i = 0; i < ssmd_datacount; i++)
                            {
                                register[i].xy = math.lerp(fp_min[i], fp_max[i], ((float*)temp)[i]);
                            }
                        }
                        // same vectorsize, this relies on pop_fx_into to log errors on vectorsize mismatch
                        else //if (p_vector_size == 2)
                        {
                            pop_fx_into_ref<float2>(ref code_pointer, fpa);

                            for (int i = 0; i < ssmd_datacount; i++)
                            {
                                register[i].xy = math.lerp(fp_min[i], fp_max[i], fpa[i]);
                            }
                        }
                    }
                    break;

                case 3:
                    {
                        float3* fpa = (float3*)temp;
                        float3* fp_min = stackalloc float3[ssmd_datacount];
                        float3* fp_max = stackalloc float3[ssmd_datacount];

                        pop_fx_into_ref<float3>(ref code_pointer, fp_min);
                        pop_fx_into_ref<float3>(ref code_pointer, fp_max);

                        pop_op_meta_cp(code_pointer, out p_datatype, out p_vector_size);


                        // c param == size 1
                        if (p_vector_size == 1)
                        {
                            // just cast the first piece of temp to a float1
                            pop_fx_into_ref<float>(ref code_pointer, (float*)temp);

                            for (int i = 0; i < ssmd_datacount; i++)
                            {
                                register[i].xyz = math.lerp(fp_min[i], fp_max[i], ((float*)temp)[i]);
                            }
                        }
                        else
                        {
                            pop_fx_into_ref<float3>(ref code_pointer, fpa);

                            for (int i = 0; i < ssmd_datacount; i++)
                            {
                                register[i].xyz = math.lerp(fp_min[i], fp_max[i], fpa[i]);
                            }
                        }
                    }
                    break;

                case 0:
                case 4:
                    {
                        float4* fpa = (float4*)temp;
                        float4* fp_min = stackalloc float4[ssmd_datacount];
                        float4* fp_max = f4; // only on equal vectorsize can we reuse output buffer because of how we overwrite our values writing to the output if the source is smaller in size

                        pop_fx_into_ref<float4>(ref code_pointer, fp_min);
                        pop_fx_into_ref<float4>(ref code_pointer, fp_max);

                        pop_op_meta_cp(code_pointer, out p_datatype, out p_vector_size);


                        // c param == size 1
                        if (p_vector_size == 1)
                        {
                            // just cast the first piece of temp to a float1
                            pop_fx_into_ref<float>(ref code_pointer, (float*)temp);

                            for (int i = 0; i < ssmd_datacount; i++)
                            {
                                register[i] = math.lerp(fp_min[i], fp_max[i], ((float*)temp)[i]);
                            }
                        }
                        else
                        {
                            pop_fx_into_ref<float4>(ref code_pointer, fpa);

                            for (int i = 0; i < ssmd_datacount; i++)
                            {
                                register[i] = math.lerp(fp_min[i], fp_max[i], fpa[i]);
                            }
                        }
                    }
                    break;

#if DEVELOPMENT_BUILD || TRACE
                default:
                    Debug.LogError($"blast.ssmd.interpretor.lerp: datatype|vectorsize mismatch, vector_size: {vector_size}, datatype: {datatype} not supported");
                    break;
#endif
            }
            code_pointer--;
        }









    }
}
