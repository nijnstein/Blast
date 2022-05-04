﻿//############################################################################################################################
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
using NSS.Blast.Interpretor;

namespace NSS.Blast.SSMD
{
    unsafe public partial struct BlastSSMDInterpretor
    {

        /// <summary>
        /// shift bits right in place 
        /// </summary>
        void get_shr_result([NoAlias] void* temp, ref int code_pointer, ref byte vector_size, int ssmd_datacount)
        {
            // index would be the same for each ssmd record
            int dataindex = code[code_pointer] - BlastInterpretor.opt_id;
            code_pointer++;

#if DEVELOPMENT_BUILD || TRACE
            // dataindex must point to a valid id 
            if (dataindex < 0 || dataindex + BlastInterpretor.opt_id >= 255)
            {
                Debug.LogError($"ssmd.get_shr: first parameter must directly point to a dataindex but its '{dataindex + BlastInterpretor.opt_id}' instead");
                return;
            }
            BlastVariableDataType datatype = BlastInterpretor.GetMetaDataType(metadata, (byte)dataindex);
            if (datatype != BlastVariableDataType.Bool32)
            {
                Debug.LogError($"ssmd.get_shr: retrieving a bit from a non Bool32 datatype, codepointer = {code_pointer}, data = {datatype}.{BlastInterpretor.GetMetaDataSize(metadata, (byte)dataindex)}");
                return;
            }
#else
            vector_size = 1;
#endif

            float* fp = (float*)temp;

#if DEVELOPMENT_BUILD || TRACE
            pop_op_meta_cp(code_pointer, out datatype, out vector_size);
            if (vector_size != 1)
            {
                Debug.LogError($"ssmd.get_shr: n may only be of vectorsize 1 p1 = {datatype}.{vector_size}");
                return;
            }
#else
            // in release we assume compiler did its job
#endif

            bool constant_shift = pop_fx_into_ref<float>(ref code_pointer, fp);

#if DEVELOPMENT_BUILD || TRACE
            if (constant_shift && !UnsafeUtils.AllEqual(fp, ssmd_datacount))
            {
                Debug.LogError($"ssmd.get_shr: shift constant not equal across all ssmd records at codepointer {code_pointer}");
                return;
            }
#endif
            if (constant_shift)
            {
                int shift = (int)fp[0];
                for (int i = 0; i < ssmd_datacount; i++)
                {
                    uint* current = &((uint*)data[i])[dataindex];
                    current[0] = current[0] >> shift;
                }
            }
            else
            {
                for (int i = 0; i < ssmd_datacount; i++)
                {
                    uint* current = &((uint*)data[i])[dataindex];
                    current[0] = current[0] >> (int)fp[i];
                }
            }

            code_pointer--;
        }
    }
}
