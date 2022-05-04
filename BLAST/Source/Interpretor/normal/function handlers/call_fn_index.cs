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

using V1 = System.Single;
using V2 = Unity.Mathematics.float2;
using V3 = Unity.Mathematics.float3;
using V4 = Unity.Mathematics.float4;

namespace NSS.Blast.Interpretor
{
    unsafe public partial struct BlastInterpretor
    {
        void CALL_FN_SIZE(ref int code_pointer, ref BlastVariableDataType datatype, ref byte vector_size, ref V4 f4)
        {
            code_pointer++;
            byte op = code[code_pointer];

            // either points to a variable or cdata 
            if (op == (byte)blast_operation.cdataref)
            {
                // compiler could fill in a constant if that constant directly maps to an operand to save space as CData is of constant size 
                int offset = (code[code_pointer + 1] << 8) + code[code_pointer + 2];
                int index = code_pointer - offset + 1;

#if DEVELOPMENT_BUILD || TRACE
                // should end up at cdata
                if (!IsValidCDATAStart(code[index]))  // at some point we probably replace cdata with a datatype cdata_float etc.. 
                {
                    Debug.LogError($"Blast.Interpretor.Size: error in cdata reference at {code_pointer}");
                    vector_size = 0;
                    f4 = float.NaN;
                    return;
                }
#endif
                // TODO when supporting integers, this should return integer data 
                f4.x = CodeUtils.ReInterpret<int, V1>(GetCDATAElementCount(code, index));

                datatype = BlastVariableDataType.ID;
                vector_size = 1;
                code_pointer = code_pointer + 3;
            }
            else
            {
                bool is_negated;


                void* p = pop_p_info(ref code_pointer, out datatype, out vector_size, out is_negated);

                switch (datatype.Combine(1, is_negated))
                {
                    case BlastVectorType.float1:
                    case BlastVectorType.int1:
                    case BlastVectorType.bool32:
                        f4.x = CodeUtils.ReInterpret<int, V1>(vector_size);
                        vector_size = 1;
                        datatype = BlastVariableDataType.ID;
                        break;

                    case BlastVectorType.float1_n:
                    case BlastVectorType.int1_n:
                    case BlastVectorType.bool32_n:
                        f4.x = CodeUtils.ReInterpret<int, V1>(-vector_size);
                        vector_size = 1;
                        datatype = BlastVariableDataType.ID;
                        break;


                    default:
                        f4 = float.NaN;
                        vector_size = 0;
                        if (IsTrace)
                        {
                            Debug.LogError($"Blast.Interpretor.Size: datatype {datatype} not implemented at reference at {code_pointer}");
                        }
                        return;
                }
            }
        }


        void CALL_FN_INDEX(ref int code_pointer, ref BlastVariableDataType datatype, ref byte vector_size, ref float4 f4, in byte offset)
        {
            code_pointer += 1;

            if (code[code_pointer] == (byte)blast_operation.cdataref)
            {
                // this indexes a cdata in the code segment
                int cdata_offset, length;
                CDATAEncodingType encoding;

#if DEVELOPMENT_BUILD || TRACE
                if (!follow_cdataref(code, code_pointer, out cdata_offset, out length, out encoding))
                {
                    goto ON_ERROR;
                }
#else
                follow_cdataref(code, code_pointer, out cdata_offset, out length, out encoding);
#endif

                /*BlastError res = */
                reinterpret_cdata(code, cdata_offset, offset, length, ref f4, out datatype, out vector_size);

                code_pointer += 3;
            }
            else
            {
                bool is_negated;
                float* fdata = (float*)pop_p_info(ref code_pointer, out datatype, out vector_size, out is_negated);


#if DEVELOPMENT_BUILD || TRACE
                if (datatype == BlastVariableDataType.Bool32)
                {
                    Debug.LogError($"index[{offset}] on Bool32 not implemented, codepointer: {code_pointer} = > {code[code_pointer]}");
                    goto ON_ERROR; 
                }
                if (math.select(vector_size, 4, vector_size == 0) < offset + 1)
                {
                    Debug.LogError($"index[{offset}] Invalid indexing component {offset} for vectorsize {vector_size}, codepointer: {code_pointer} = > {code[code_pointer]}");
                    goto ON_ERROR;
                }
#endif                                                                                   

                switch (datatype.Combine(vector_size, is_negated))
                {
                    case BlastVectorType.float1: f4.x = ((float*)fdata)[0]; break;
                    case BlastVectorType.float2: f4.x = ((float*)fdata)[offset]; break;
                    case BlastVectorType.float3: f4.x = ((float*)fdata)[offset]; break;
                    case BlastVectorType.float4: f4.x = ((float*)fdata)[offset]; break;

                    case BlastVectorType.int1: f4.x = ((float*)fdata)[0]; break;
                    case BlastVectorType.int2: f4.x = ((float*)fdata)[offset]; break;
                    case BlastVectorType.int3: f4.x = ((float*)fdata)[offset]; break;
                    case BlastVectorType.int4: f4.x = ((float*)fdata)[offset]; break;

                    case BlastVectorType.float1_n: f4.x = -((float*)fdata)[0]; break;
                    case BlastVectorType.float2_n: f4.x = -((float*)fdata)[offset]; break;
                    case BlastVectorType.float3_n: f4.x = -((float*)fdata)[offset]; break;
                    case BlastVectorType.float4_n: f4.x = -((float*)fdata)[offset]; break;

                    case BlastVectorType.int1_n: f4.x = -((float*)fdata)[0]; break;
                    case BlastVectorType.int2_n: f4.x = -((float*)fdata)[offset]; break;
                    case BlastVectorType.int3_n: f4.x = -((float*)fdata)[offset]; break;
                    case BlastVectorType.int4_n: f4.x = -((float*)fdata)[offset]; break;

#if DEVELOPMENT_BUILD || TRACE
                    default:
                        {
                            Debug.LogError($"Index[{offset}]: datatype {datatype}, vectorsize {vector_size} not supported");
                            goto ON_ERROR;
                        }
#endif
                }
                vector_size = 1; 
            }

            return;

        ON_ERROR:
            vector_size = 0;
            f4 = float.NaN;
        }


        void CALL_FN_INDEX_N(ref int code_pointer, ref BlastVariableDataType datatype, ref byte vector_size, ref float4 f4)
        {
            code_pointer += 1;
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
                        byte size;
                        bool is_negated;

                        float* fdata = (float*)pop_p_info(ref code_pointer, out datatype, out size, out is_negated);

                        // if cdata -> get length and start of data 
                        int index = pop_as_i1(ref code_pointer);

                        // this will change when handling multiple datatypes 
                        switch (datatype.Combine(1, is_negated))
                        {
                            case BlastVectorType.float1: f4.x = fdata[index]; break;
                            case BlastVectorType.float1_n: f4.x = -fdata[index]; break;
                            case BlastVectorType.int1: f4.x = fdata[index]; break;
                            case BlastVectorType.int1_n: f4.x = CodeUtils.ReInterpret<int, float>(-CodeUtils.ReInterpret<float, int>(fdata[index])); break;

#if DEVELOPMENT_BUILD || TRACE
                            default:
                                {
                                    Debug.LogError($"Index[{index}]: datatype {datatype}, vectorsize {size} not supported");
                                    f4 = float.NaN; 
                                    break;
                                }
#endif
                        }


                        vector_size = 1; 
                    }
                    break;
            }
        }
    }
}
