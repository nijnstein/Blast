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
using NSS.Blast.Interpretor;

namespace NSS.Blast.SSMD
{
    unsafe public partial struct BlastSSMDInterpretor
    {


        /// <summary>
        /// get bit and return result to f4 register.x
        /// </summary>
        void get_getbit_result([NoAlias] void* temp, ref int code_pointer, ref byte vector_size, int ssmd_datacount, [NoAlias] float4* f4_result)
        {
            float* fp_index = (float*)temp;

            // index would be the same for each ssmd record
            int dataindex = code[code_pointer] - BlastInterpretor.opt_id;
            code_pointer++;

#if DEVELOPMENT_BUILD || TRACE
            // dataindex must point to a valid id 
            if (dataindex < 0 || dataindex + BlastInterpretor.opt_id >= 255)
            {
                Debug.LogError($"ssmd.get_bit: first parameter must directly point to a dataindex but its '{dataindex + BlastInterpretor.opt_id}' instead");
                return;
            }
            BlastVariableDataType datatype = BlastInterpretor.GetMetaDataType(metadata, (byte)dataindex);
            if (datatype != BlastVariableDataType.Bool32)
            {
                Debug.LogError($"ssmd.get_bit: retrieving a bit from a non Bool32 datatype, codepointer = {code_pointer}, data = {datatype}.{BlastInterpretor.GetMetaDataSize(metadata, (byte)dataindex)}");
                return;
            }
#else
            vector_size = 1;
#endif
            // get index
#if DEVELOPMENT_BUILD || TRACE
            pop_op_meta_cp(code_pointer, out datatype, out vector_size);
            if (vector_size != 1)
            {
                Debug.LogError($"ssmd.get_bit: the index to set may only be of vectorsize 1 p1 = {datatype}.{vector_size}");
                return;
            }
#else
            // in release we assume compiler did its job
#endif

            bool b_constant_index = pop_fx_into_ref<float>(ref code_pointer, fp_index);

#if DEVELOPMENT_BUILD || TRACE
            if (b_constant_index)
            {
                for (int i = 1; i < ssmd_datacount; i++)
                {
                    if (fp_index[i] != fp_index[0])
                    {
                        // without string magics this is burst compatible so we wont create a function but just have this code everywhere... codegen someday; 
                        Debug.LogError($"ssmd.get_bit: the index is marked constant but is not equal for all ssmd records at {code_pointer}");
                        return;
                    }
                }
            }
#endif

            if (b_constant_index)
            {
                // in most cases get bit is called with a constant index 
                uint mask = (uint)(1 << (byte)fp_index[0]);

                // saving a shift and memindex every index:
                for (int i = 0; i < ssmd_datacount; i++)
                {
                    uint current = ((uint*)data[i])[dataindex];
                    f4_result[i].x = math.select(0f, 1f, (mask & current) == mask);
                }

            }
            else
            {
                for (int i = 0; i < ssmd_datacount; i++)
                {
                    uint current = ((uint*)data[i])[dataindex];
                    uint mask = (uint)(1 << (byte)fp_index[i]);

                    f4_result[i].x = math.select(0f, 1f, (mask & current) == mask);
                }
            }

            code_pointer--;
        }
    }
}
