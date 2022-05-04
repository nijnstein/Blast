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
using NSS.Blast.Interpretor; 

namespace NSS.Blast.SSMD
{
    unsafe public partial struct BlastSSMDInterpretor
    {
        /// <summary>
        /// set zero to given index
        /// </summary>
        BlastError CALL_ZERO([NoAlias] void* temp, ref int code_pointer, ref byte vector_size, int ssmd_datacount)
        {
            // index would be the same for each ssmd record
            int dataindex = code[code_pointer] - BlastInterpretor.opt_id;

            // dataindex must point to a valid id 
            if (dataindex < 0 || dataindex + BlastInterpretor.opt_id >= 255)
            {
                if (IsTrace)
                {
                    Debug.LogError($"Blast.Interpretor.ssmd.zero: first parameter must directly point to a dataindex but its '{dataindex + BlastInterpretor.opt_id}' instead");
                }
                return BlastError.ssmd_parameter_must_be_variable;
            }

            // 
            // ZERO is 0 for any datatype, so we dont need to check 
            //

            switch (BlastInterpretor.GetMetaDataSize(in metadata, (byte)dataindex))
            {
                case 1:
                    simd.move_f1_constant_to_indexed_data_as_f1(data, data_rowsize, data_is_aligned, dataindex, 0, ssmd_datacount);
                    break;

                case 2:
                    simd.move_f1_constant_to_indexed_data_as_f2(data, data_rowsize, data_is_aligned, dataindex, 0, ssmd_datacount);
                    break;
                case 3:
                    simd.move_f1_constant_to_indexed_data_as_f3(data, data_rowsize, data_is_aligned, dataindex, 0, ssmd_datacount);
                    break;

                case 4:
                case 0:
                    simd.move_f1_constant_to_indexed_data_as_f4(data, data_rowsize, data_is_aligned, dataindex, 0, ssmd_datacount);
                    break;

                default:
                    if (IsTrace)
                    {
                        Debug.LogError($"Blast.Interpretor.ssmd.zero: first parameter must directly point to a dataindex but its '{dataindex + BlastInterpretor.opt_id}' instead");
                    }
                    return BlastError.error_vector_size_not_supported;
            }

            return BlastError.success;
        }
    }
}
