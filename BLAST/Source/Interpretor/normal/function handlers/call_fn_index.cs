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

using V1 = System.Single;
using V2 = Unity.Mathematics.float2;
using V3 = Unity.Mathematics.float3;
using V4 = Unity.Mathematics.float4;

namespace NSS.Blast.Interpretor
{
    unsafe public partial struct BlastInterpretor
    {
        void CALL_FN_INDEX(ref int code_pointer, ref BlastVariableDataType datatype, ref byte vector_size, out float4 f4, in byte offset)
        {
            code_pointer += 1;
            f4 = float.NaN;

            if (code[code_pointer] == (byte)blast_operation.cdataref)
            {
                // this indexes a cdata in the code segment
                int cdata_offset, length;
                CDATAEncodingType encoding;

#if DEVELOPMENT_BUILD || TRACE
                if (!follow_cdataref(code, code_pointer, out cdata_offset, out length, out encoding))
                {
                    return;
                }
#else
                follow_cdataref(code, code_pointer, out cdata_offset, out length, out encoding);
#endif

                /*BlastError res = */
                reinterpret_cdata(code, cdata_offset, offset, length, ref f4, out datatype, out vector_size);

                // f4.x = index_cdata_f1(code, cdata_offset, offset, length);

                code_pointer += 3;
                return;
            }
            else
            {
                bool is_negated;
                float* fdata = (float*)pop_p_info(ref code_pointer, out datatype, out vector_size, out is_negated);


#if DEVELOPMENT_BUILD || TRACE
                if (datatype == BlastVariableDataType.ID)
                {
                    Debug.LogError($"ID datatype in index[{offset}] not yet implemented, codepointer: {code_pointer} = > {code[code_pointer]}");
                    return;
                }
                if (math.select(vector_size, 4, vector_size == 0) < offset + 1)
                {
                    Debug.LogError($"index[{offset}] Invalid indexing component {offset} for vectorsize {vector_size}, codepointer: {code_pointer} = > {code[code_pointer]}");
                    return;
                }
#endif

                switch ((BlastVectorSizes)vector_size)
                {
                    // result is always the same regardless.. could raise errors on this condition as compiler could do better then waste precious bytes
                    case BlastVectorSizes.float1: f4.x = ((float*)fdata)[0]; vector_size = 1; break;
                    case BlastVectorSizes.float2: f4.x = ((float*)fdata)[offset]; vector_size = 1; break;
                    case BlastVectorSizes.float3: f4.x = ((float*)fdata)[offset]; vector_size = 1; break;
                    case 0:
                    case BlastVectorSizes.float4: f4.x = ((float*)fdata)[offset]; vector_size = 1; break;
#if DEVELOPMENT_BUILD || TRACE
                    default:
                        {
                            Debug.LogError($"Index[{offset}]: NUMERIC({vector_size}) vectorsize '{vector_size}' not supported");
                            break;
                        }
#endif
                }

                f4.x = math.select(f4.x, -f4.x, is_negated);
            }
        }


        void CALL_FN_INDEX_N(ref int code_pointer, ref BlastVariableDataType datatype, ref byte vector_size, out float4 f4)
        {
            code_pointer += 1;
            f4 = float.NaN;

            byte op = code[code_pointer];

            switch (op)
            {
                // data points to a constant cdata section 
                case (byte)blast_operation.cdataref:
                    {
                        int offset, length;
                        CDATAEncodingType encoding;

#if DEVELOPMENT_BUILD || TRACE || CHECK_CDATA
                        if (!follow_cdataref(code, code_pointer, out offset, out length, out encoding))
                        {
                            // error following reference ... 
                            f4 = float.NaN;
                            return;
                        }
#else
                        // in release we assume all grass is green -> this is why stuff is validated during compilation 
                        follow_cdataref(code, code_pointer, out offset, out length, out encoding); 
#endif
                        // get indexer, must be a v[1] 
                        code_pointer += 3;

                        // in debug this logs errors
                        int index = pop_as_i1(ref code_pointer); // if indexer is a float, it is casted to int 

                        // index and return data
                        //f4.x = index_cdata_f1(code, offset, index, length);

                        reinterpret_cdata(code, offset, index, length, ref f4, out datatype, out vector_size);

                    }
                    break;


                // default parameter handling, the first could be of any type 
                default:
                    {
                        // for now, handle all cases normally 



                        byte size;
                        bool is_negated;

                        float* fdata = (float*)pop_p_info(ref code_pointer, out datatype, out size, out is_negated);

                        // if cdata -> get length and start of data 

                        bool is_negated2;
                        float* findex = (float*)pop_p_info(ref code_pointer, out datatype, out vector_size, out is_negated2);

                        int index = (int)findex[0];
                        index = math.select(index, -index, is_negated2);

                        // this will change when handling multiple datatypes 
                        f4.x = math.select(fdata[index], -fdata[index], is_negated2);
                    }
                    break;
            }
        }
    }
}
