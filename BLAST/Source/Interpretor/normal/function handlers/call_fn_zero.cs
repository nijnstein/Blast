//############################################################################################################################
// BLAST v1.0.4c                                                                                                             #
// Copyright © 2022 Rob Lemmens | NijnStein Software <rob.lemmens.s31 gmail com> All Rights Reserved                   ^__^\ #
// Unauthorized copying of this file, via any medium is strictly prohibited proprietary and confidential               (oo)\ #
//                                                                                                                     (__)  #
//############################################################################################################################
#pragma warning disable CS1591
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

        /// <summary>
        /// fill target with zeros 
        /// </summary>
        void CALL_ZERO(ref int code_pointer, ref byte vector_size)
        {
            code_pointer++;
            int dataindex = code[code_pointer] - opt_id;
            code_pointer++;

#if DEVELOPMENT_BUILD || TRACE
            // dataindex must point to a valid id 
            if (dataindex < 0 || dataindex + opt_id >= 255)
            {
                Debug.LogError($"Blast.Interpretor.Zero: first parameter must directly point to a dataindex but its '{dataindex + opt_id}' instead");
                vector_size = 0;
                return;
            }
#endif
            // dont need type, all set to 0 anyway
            vector_size = GetMetaDataSize(metadata, (byte)dataindex);
            switch (vector_size)
            {
                case 0:
                case 4:
                    ((uint*)data)[dataindex] = 0;
                    ((uint*)data)[dataindex + 1] = 0;
                    ((uint*)data)[dataindex + 2] = 0;
                    ((uint*)data)[dataindex + 3] = 0;
                    break;
                case 3:
                    ((uint*)data)[dataindex] = 0;
                    ((uint*)data)[dataindex + 1] = 0;
                    ((uint*)data)[dataindex + 2] = 0;
                    break;
                case 2:
                    ((uint*)data)[dataindex] = 0;
                    ((uint*)data)[dataindex + 1] = 0;
                    break;
                case 1:
                    ((uint*)data)[dataindex] = 0;
                    break;

#if DEVELOPMENT_BUILD || TRACE
                default:
                    Debug.LogError($"Blast.Interpretor.Zero: unsupported vectorsize {vector_size} in '{dataindex + opt_id}' at codepointer {code_pointer}");
                    vector_size = 0;
                    break;
#endif
            }
        }


        /// <summary>
        /// grow from 1 element to an n-vector 
        /// </summary>
        void CALL_EXPAND(ref int code_pointer, ref BlastVariableDataType datatype, ref byte vector_size, in byte expand_to, ref float4 f4)
        {
            bool neg;
            code_pointer++;
            float* d1 = (float*)pop_p_info(ref code_pointer, out datatype, out vector_size, out neg);

#if DEVELOPMENT_BUILD || TRACE
            if ((datatype != BlastVariableDataType.Numeric && (datatype != BlastVariableDataType.Bool32)) || vector_size != 1)
            {
                Debug.LogError($"expand: input parameter datatype|vector_size mismatch, must be Numeric or Bool32 but found: p1 = {datatype}.{vector_size}");
               f4 = float.NaN;
                return;
            }
#endif
            vector_size = expand_to;
            switch ((BlastVectorSizes)vector_size)
            {
                case 0:
                case BlastVectorSizes.float4: f4 = math.select(d1[0], -d1[0], neg); vector_size = 4; break;
                
                // this would be wierd for the compiler to encode.. 
                case BlastVectorSizes.float1:
                    f4.x = math.select(d1[0], -d1[0], neg);
#if TRACE
                    Debug.LogWarning($"expand: compiler should not compile an expand operation resulting in a scalar, codepointer = {code_pointer}");
#endif
                    break;
                
                case BlastVectorSizes.float2: f4.xy = math.select(d1[0], -d1[0], neg); break;
                case BlastVectorSizes.float3: f4.xyz = math.select(d1[0], -d1[0], neg); break;

#if DEVELOPMENT_BUILD || TRACE
                default:
                    {
                        Debug.LogError($"expand: {datatype} vector size '{vector_size}' not supported ");
                        f4 = float.NaN;
                        break;
                    }
#endif
            }
        }

    }
}
