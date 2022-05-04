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
        /// fused multiply add 
        /// 
        /// todo: convert this into a fused condition list, with n conditions and n+1 parameters ?:
        ///
        /// fused_op:
        /// # 1 byte = encode62 = parametercount + vectorsize
        /// # 1 byte * ceil(parametercount / 4) = conditions: +-/* encoded in 2 bits, 4 in byte 
        /// # parameters 
        /// 
        /// - always 3 params, input vector size == output vectorsize
        /// </summary>
        /// <returns></returns>
        void get_fma_result([NoAlias] void* temp, ref int code_pointer, ref byte vector_size, int ssmd_datacount, [NoAlias] ref float4* f4)
        {
            BlastVariableDataType datatype, datatype2;
            byte vector_size2;

            // peek metadata of the first parameter
            pop_op_meta_cp(code_pointer + 0, out datatype, out vector_size);

            // first 2 inputs share datatype and vectorsize 
            switch (vector_size)
            {
                case 1:
                    // in case of v1. all parameters should equal in size
                    float* m11 = (float*)temp;
                    pop_fx_into_ref<float>(ref code_pointer, m11);
                    pop_fx_with_op_into_fx_ref(ref code_pointer, m11, m11, blast_operation.multiply);
                    pop_fx_with_op_into_fx_ref(ref code_pointer, m11, f4, blast_operation.add);
                    // this is needed because pop_fx_with_op_into_fx_ref leaves codepointer after the last data while function should end at the last
                    code_pointer--;
                    break;

                case 2:
                    // the last op might be in another vectorsize 
                    float2* m12 = (float2*)temp;
                    pop_fx_into_ref<float2>(ref code_pointer, m12);
                    pop_fx_with_op_into_fx_ref<float2, float2>(ref code_pointer, m12, m12, blast_operation.multiply);

                    pop_op_meta_cp(code_pointer, out datatype2, out vector_size2);
                    pop_fx_with_op_into_fx_ref<float2, float4>(ref code_pointer, m12, f4, blast_operation.add, datatype2, vector_size2 != 2);
                    code_pointer--;
                    break;

                case 3:
                    float3* m13 = (float3*)temp;
                    pop_fx_into_ref<float3>(ref code_pointer, m13);
                    pop_fx_with_op_into_fx_ref<float3, float3>(ref code_pointer, m13, m13, blast_operation.multiply);

                    pop_op_meta_cp(code_pointer, out datatype2, out vector_size2);
                    pop_fx_with_op_into_fx_ref<float3, float4>(ref code_pointer, m13, f4, blast_operation.add, datatype2, vector_size2 != 3);
                    code_pointer--;
                    break;

                case 0:
                case 4:
                    float4* m14 = (float4*)temp;
                    pop_fx_into_ref<float4>(ref code_pointer, f4); // with f4 we can avoid reading/writing to the same buffer by alternating as long as we end at f4
                    pop_fx_with_op_into_fx_ref<float4, float4>(ref code_pointer, f4, m14, blast_operation.multiply);

                    // last may differ in vectorsize 
                    pop_op_meta_cp(code_pointer, out datatype2, out vector_size2);
                    pop_fx_with_op_into_fx_ref<float4, float4>(ref code_pointer, m14, f4, blast_operation.add, datatype2, vector_size2 != 4);

                    code_pointer--;
                    break;
            }
        }







    }
}
