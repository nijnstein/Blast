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

        void CALL_GETSSMDINDEX(ref int code_pointer, ref byte vector_size, in BlastVariableDataType datatype, int ssmd_datacount, [NoAlias] void* register)
        {
            //
            // datatype would depend upon assignment target
            //
            switch (datatype)
            {

                case BlastVariableDataType.Numeric:
                    {
                        float4* f4 = (float4*)register; 
                        switch (vector_size)
                        {
                            case 0:
                            case 4:
                                for (int i = 0; i < ssmd_datacount; i++) f4[i] = i;
                                break;

                            case 1:
                                for (int i = 0; i < ssmd_datacount; i++) f4[i].x = i;
                                break;
                            
                            case 2:
                                for (int i = 0; i < ssmd_datacount; i++) f4[i].xy = i;
                                break;
                            
                            case 3:
                                for (int i = 0; i < ssmd_datacount; i++) f4[i].xyz = i;
                                break;

                            default: goto DATATYPE_VECTOR_SIZE_ERROR;
                        }
                    }
                    break;

                case BlastVariableDataType.ID:
                    {
                        int4* i4 = (int4*)register;
                        switch (vector_size)
                        {
                            case 0:
                            case 4:
                                for (int i = 0; i < ssmd_datacount; i++) i4[i] = i;
                                break;

                            case 1:
                                for (int i = 0; i < ssmd_datacount; i++) i4[i].x = i;
                                break;

                            case 2:
                                for (int i = 0; i < ssmd_datacount; i++) i4[i].xy = i;
                                break;

                            case 3:
                                for (int i = 0; i < ssmd_datacount; i++) i4[i].xyz = i;
                                break;

                            default: goto DATATYPE_VECTOR_SIZE_ERROR;
                        }
                    }
                    break;

                case BlastVariableDataType.Bool32:
                    {
                        if (vector_size != 1)
                        {
                            goto DATATYPE_VECTOR_SIZE_ERROR;
                        }
                        uint4* b4 = (uint4*)register;
                        for (int i = 0; i < ssmd_datacount; i++) b4[i].x = (uint)i;
                        break; 
                    }

                default: goto DATATYPE_VECTOR_SIZE_ERROR; 
            }


            return; 

        DATATYPE_VECTOR_SIZE_ERROR:
            if (IsTrace)
            {
                Debug.LogError($"blast.ssmd.interpretor.call_get_ssmd_index: vectorsize|datatype not supported at codepointer {code_pointer}, datatype: {datatype}, vectorsize: {vector_size}");
            }
            return;
        }
    }
}