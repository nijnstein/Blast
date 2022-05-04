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
        /// count leading bits set to 0 and return result to f4 register.x
        /// </summary>
        void get_lzcnt_result([NoAlias] void* temp, ref int code_pointer, ref byte vector_size, int ssmd_datacount, [NoAlias] float4* f4_result)
        {
            // index would be the same for each ssmd record
            int dataindex = code[code_pointer] - BlastInterpretor.opt_id;
            code_pointer++;

#if DEVELOPMENT_BUILD || TRACE
            // dataindex must point to a valid id 
            if (dataindex < 0 || dataindex + BlastInterpretor.opt_id >= 255)
            {
                Debug.LogError($"ssmd.get_lzcnt: first parameter must directly point to a dataindex but its '{dataindex + BlastInterpretor.opt_id}' instead");
                return;
            }
            BlastVariableDataType datatype = BlastInterpretor.GetMetaDataType(metadata, (byte)dataindex);
            if (datatype != BlastVariableDataType.Bool32)
            {
                Debug.LogError($"ssmd.get_lzcnt: retrieving a bit from a non Bool32 datatype, codepointer = {code_pointer}, data = {datatype}.{BlastInterpretor.GetMetaDataSize(metadata, (byte)dataindex)}");
                return;
            }
#else
            vector_size = 1;
#endif

            for (int i = 0; i < ssmd_datacount; i++)
            {
                uint current = ((uint*)data[i])[dataindex];
                f4_result[i].x = math.lzcnt(current);
            }

            code_pointer--;
        }
    }
}
