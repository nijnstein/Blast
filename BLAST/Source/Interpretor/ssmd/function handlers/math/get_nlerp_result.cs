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

namespace NSS.Blast.SSMD
{
    unsafe public partial struct BlastSSMDInterpretor
    {

        /// <summary>
        /// normalized lerp on quaternions
        /// </summary>
#if !STANDALONE_VSBUILD
        [SkipLocalsInit]
#endif
        void get_nlerp_result([NoAlias] void* temp, ref int code_pointer, ref byte vector_size, int ssmd_datacount, [NoAlias] float4* f4)
        {
            BlastVariableDataType datatype;
            pop_op_meta_cp(code_pointer + 1, out datatype, out vector_size);

#if DEVELOPMENT_BUILD || TRACE
            if (datatype != BlastVariableDataType.Numeric)
            {
                Debug.LogError($"blast.ssmd.interpretor.nlerp: datatype|vectorsize mismatch, vector_size: {vector_size}, datatype: {datatype} not supported");
                return;
            }
#endif

            switch (vector_size)
            {

                case 0:
                case 4:
                    {
                        float* fpa = stackalloc float[ssmd_datacount];
                        float4* fp_min = (float4*)temp;
                        float4* fp_max = f4; // only on equal vectorsize can we reuse output buffer because of how we overwrite our values writing to the output if the source is smaller in size

                        pop_fx_into_ref<float4>(ref code_pointer, fp_min);
                        pop_fx_into_ref<float4>(ref code_pointer, fp_max);
                        pop_fx_into_ref<float>(ref code_pointer, fpa);

                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            register[i] = math.nlerp((quaternion)fp_min[i], (quaternion)fp_max[i], fpa[i]).value;
                        }
                    }
                    break;

#if DEVELOPMENT_BUILD || TRACE
                default:
                    Debug.LogError($"blast.ssmd.interpretor.nlerp: datatype|vectorsize mismatch, vector_size: {vector_size}, datatype: {datatype} not supported");
                    break;
#endif
            }

            code_pointer--;
        }


    }
}
