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
        /// get the maximum value of all arguments of any vectorsize 
        /// </summary>
        void CALL_FN_CMAX(ref int code_pointer, ref BlastVariableDataType datatype, ref byte vector_size, ref float4 f4)
        {
            code_pointer++;
            byte c = decode62(in code[code_pointer], ref vector_size);
            code_pointer++;

            byte popped_vector_size = 0;
            bool is_negated = false;

            // as we always assign to x, fix a pointer to it instead of using the property
            // while burst knows about a float4.x the standard .net compiler calls property accessors 
            fixed (float4* p4 = &f4)
            {
                vector_size = 1;
                float* output = (float*)(void*)p4;

                // get first -> it determines output type and initializes our v4 output 
                c--;
                float* fdata = (float*)pop_p_info(ref code_pointer, out datatype, out popped_vector_size, out is_negated);

                // combine all data into 1 branch on a byte typed enumeration -> burst will compile this into a jump table 
                switch (datatype.Combine(popped_vector_size, is_negated))
                {
                    case BlastVectorType.float1: output[0] = fdata[0]; break;
                    case BlastVectorType.float2: output[0] = math.cmax(((float2*)(void*)&fdata[0])[0]); break;
                    case BlastVectorType.float3: output[0] = math.cmax(((float3*)(void*)&fdata[0])[0]); break;
                    case BlastVectorType.float4: output[0] = math.cmax(((float4*)(void*)&fdata[0])[0]); break;

                    case BlastVectorType.float1_n: output[0] = -fdata[0]; break;
                    case BlastVectorType.float2_n: output[0] = math.cmax(-((float2*)(void*)&fdata[0])[0]); break;
                    case BlastVectorType.float3_n: output[0] = math.cmax(-((float3*)(void*)&fdata[0])[0]); break;
                    case BlastVectorType.float4_n: output[0] = math.cmax(-((float4*)(void*)&fdata[0])[0]); break;

                    case BlastVectorType.int1: output[0] = fdata[0]; break;
                    case BlastVectorType.int2: output[0] = CodeUtils.ReInterpret<int, float>(math.cmax(((int2*)(void*)&fdata[0])[0])); break;
                    case BlastVectorType.int3: output[0] = CodeUtils.ReInterpret<int, float>(math.cmax(((int3*)(void*)&fdata[0])[0])); break;
                    case BlastVectorType.int4: output[0] = CodeUtils.ReInterpret<int, float>(math.cmax(((int4*)(void*)&fdata[0])[0])); break;

                    // doesnt look nice, but still compiles fast as most of the casting is resolved at compiletime 
                    case BlastVectorType.int1_n: output[0] = fdata[0]; break;
                    case BlastVectorType.int2_n: output[0] = CodeUtils.ReInterpret<int, float>(math.cmax(-((int2*)(void*)&fdata[0])[0])); break;
                    case BlastVectorType.int3_n: output[0] = CodeUtils.ReInterpret<int, float>(math.cmax(-((int3*)(void*)&fdata[0])[0])); break;
                    case BlastVectorType.int4_n: output[0] = CodeUtils.ReInterpret<int, float>(math.cmax(-((int4*)(void*)&fdata[0])[0])); break;

                    default: goto ERROR_NOT_SUPPORTED;
                }

                // then every other parameter
                while (c > 0)
                {
                    c--;

                    // we need to pop only once due to how the elements of the vector are organized in the data/stack
                    // - pop_or_value needs one call/float
                    fdata = (float*)pop_p_info(ref code_pointer, out datatype, out popped_vector_size, out is_negated);

                    switch (datatype.Combine(popped_vector_size, is_negated))
                    {
                        case BlastVectorType.float1: output[0] = math.max(output[0], fdata[0]); break;
                        case BlastVectorType.float2: output[0] = math.max(output[0], math.cmax(((float2*)(void*)&fdata[0])[0])); break;
                        case BlastVectorType.float3: output[0] = math.max(output[0], math.cmax(((float3*)(void*)&fdata[0])[0])); break;
                        case BlastVectorType.float4: output[0] = math.max(output[0], math.cmax(((float4*)(void*)&fdata[0])[0])); break;

                        case BlastVectorType.float1_n: output[0] = math.max(output[0], -fdata[0]); break;
                        case BlastVectorType.float2_n: output[0] = math.max(output[0], math.cmax(-((float2*)(void*)&fdata[0])[0])); break;
                        case BlastVectorType.float3_n: output[0] = math.max(output[0], math.cmax(-((float3*)(void*)&fdata[0])[0])); break;
                        case BlastVectorType.float4_n: output[0] = math.max(output[0], math.cmax(-((float4*)(void*)&fdata[0])[0])); break;

                        // doesnt look nice, but still compiles fast as most of the casting is resolved at compiletime 
                        case BlastVectorType.int1: output[0] = CodeUtils.ReInterpret<int, float>(math.max(CodeUtils.ReInterpret<float, int>(output[0]), CodeUtils.ReInterpret<float, int>(fdata[0]))); break;
                        case BlastVectorType.int2: output[0] = CodeUtils.ReInterpret<int, float>(math.max(CodeUtils.ReInterpret<float, int>(output[0]), math.cmax(((int2*)(void*)&fdata[0])[0]))); break;
                        case BlastVectorType.int3: output[0] = CodeUtils.ReInterpret<int, float>(math.max(CodeUtils.ReInterpret<float, int>(output[0]), math.cmax(((int3*)(void*)&fdata[0])[0]))); break;
                        case BlastVectorType.int4: output[0] = CodeUtils.ReInterpret<int, float>(math.max(CodeUtils.ReInterpret<float, int>(output[0]), math.cmax(((int4*)(void*)&fdata[0])[0]))); break;
                        case BlastVectorType.int1_n: output[0] = CodeUtils.ReInterpret<int, float>(math.max(CodeUtils.ReInterpret<float, int>(output[0]), -CodeUtils.ReInterpret<float, int>(fdata[0]))); break;
                        case BlastVectorType.int2_n: output[0] = CodeUtils.ReInterpret<int, float>(math.max(CodeUtils.ReInterpret<float, int>(output[0]), math.cmax(-((int2*)(void*)&fdata[0])[0]))); break;
                        case BlastVectorType.int3_n: output[0] = CodeUtils.ReInterpret<int, float>(math.max(CodeUtils.ReInterpret<float, int>(output[0]), math.cmax(-((int3*)(void*)&fdata[0])[0]))); break;
                        case BlastVectorType.int4_n: output[0] = CodeUtils.ReInterpret<int, float>(math.max(CodeUtils.ReInterpret<float, int>(output[0]), math.cmax(-((int4*)(void*)&fdata[0])[0]))); break;

                        default: goto ERROR_NOT_SUPPORTED;
                    }
                }
            }

            return;
        ERROR_NOT_SUPPORTED:
#if DEVELOPMENT_BUILD || TRACE
            Debug.LogError($"Blast.Interpretor.maxa: combination of datatype {datatype} and vector size {popped_vector_size} not supported for output to vectorsize {vector_size} at codepointer {code_pointer} => {code[code_pointer]} ");
#endif
            return;
        }

        /// <summary>
        /// return the smallest of the arguments of any vectorsize
        /// </summary>
        void CALL_FN_CMIN(ref int code_pointer, ref BlastVariableDataType datatype, ref byte vector_size, ref float4 f4)
        {
            code_pointer++;
            byte c = decode62(in code[code_pointer], ref vector_size);
            code_pointer++;

            byte popped_vector_size = 0;
            bool is_negated = false;

            // as we always assign to x, fix a pointer to it instead of using the property
            // while burst knows about a float4.x the standard .net compiler calls property accessors 
            fixed (float4* p4 = &f4)
            {
                vector_size = 1;
                float* output = (float*)(void*)p4;

                // get first -> it determines output type and initializes our v4 output 
                c--;
                float* fdata = (float*)pop_p_info(ref code_pointer, out datatype, out popped_vector_size, out is_negated);

                // combine all data into 1 branch on a byte typed enumeration -> burst will compile this into a jump table 
                switch (datatype.Combine(popped_vector_size, is_negated))
                {
                    case BlastVectorType.float1: output[0] = fdata[0]; break;
                    case BlastVectorType.float2: output[0] = math.cmin(((float2*)(void*)&fdata[0])[0]); break;
                    case BlastVectorType.float3: output[0] = math.cmin(((float3*)(void*)&fdata[0])[0]); break;
                    case BlastVectorType.float4: output[0] = math.cmin(((float4*)(void*)&fdata[0])[0]); break;

                    case BlastVectorType.float1_n: output[0] = -fdata[0]; break;
                    case BlastVectorType.float2_n: output[0] = math.cmin(-((float2*)(void*)&fdata[0])[0]); break;
                    case BlastVectorType.float3_n: output[0] = math.cmin(-((float3*)(void*)&fdata[0])[0]); break;
                    case BlastVectorType.float4_n: output[0] = math.cmin(-((float4*)(void*)&fdata[0])[0]); break;

                    case BlastVectorType.int1: output[0] = fdata[0]; break;
                    case BlastVectorType.int2: output[0] = CodeUtils.ReInterpret<int, float>(math.cmin(((int2*)(void*)&fdata[0])[0])); break;
                    case BlastVectorType.int3: output[0] = CodeUtils.ReInterpret<int, float>(math.cmin(((int3*)(void*)&fdata[0])[0])); break;
                    case BlastVectorType.int4: output[0] = CodeUtils.ReInterpret<int, float>(math.cmin(((int4*)(void*)&fdata[0])[0])); break;

                    // doesnt look nice, but still compiles fast as most of the casting is resolved at compiletime 
                    case BlastVectorType.int1_n: output[0] = fdata[0]; break;
                    case BlastVectorType.int2_n: output[0] = CodeUtils.ReInterpret<int, float>(math.cmin(-((int2*)(void*)&fdata[0])[0])); break;
                    case BlastVectorType.int3_n: output[0] = CodeUtils.ReInterpret<int, float>(math.cmin(-((int3*)(void*)&fdata[0])[0])); break;
                    case BlastVectorType.int4_n: output[0] = CodeUtils.ReInterpret<int, float>(math.cmin(-((int4*)(void*)&fdata[0])[0])); break;

                    default: goto ERROR_NOT_SUPPORTED;
                }

                // then every other parameter
                while (c > 0)
                {
                    c--;

                    // we need to pop only once due to how the elements of the vector are organized in the data/stack
                    // - pop_or_value needs one call/float
                    fdata = (float*)pop_p_info(ref code_pointer, out datatype, out popped_vector_size, out is_negated);

                    switch (datatype.Combine(popped_vector_size, is_negated))
                    {
                        case BlastVectorType.float1: output[0] = math.min(output[0], fdata[0]); break;
                        case BlastVectorType.float2: output[0] = math.min(output[0], math.cmin(((float2*)(void*)&fdata[0])[0])); break;
                        case BlastVectorType.float3: output[0] = math.min(output[0], math.cmin(((float3*)(void*)&fdata[0])[0])); break;
                        case BlastVectorType.float4: output[0] = math.min(output[0], math.cmin(((float4*)(void*)&fdata[0])[0])); break;

                        case BlastVectorType.float1_n: output[0] = math.min(output[0], -fdata[0]); break;
                        case BlastVectorType.float2_n: output[0] = math.min(output[0], math.cmin(-((float2*)(void*)&fdata[0])[0])); break;
                        case BlastVectorType.float3_n: output[0] = math.min(output[0], math.cmin(-((float3*)(void*)&fdata[0])[0])); break;
                        case BlastVectorType.float4_n: output[0] = math.min(output[0], math.cmin(-((float4*)(void*)&fdata[0])[0])); break;

                        // doesnt look nice, but still compiles fast as most of the casting is resolved at compiletime 
                        case BlastVectorType.int1: output[0] = CodeUtils.ReInterpret<int, float>(math.min(CodeUtils.ReInterpret<float, int>(output[0]), CodeUtils.ReInterpret<float, int>(fdata[0]))); break;
                        case BlastVectorType.int2: output[0] = CodeUtils.ReInterpret<int, float>(math.min(CodeUtils.ReInterpret<float, int>(output[0]), math.cmin(((int2*)(void*)&fdata[0])[0]))); break;
                        case BlastVectorType.int3: output[0] = CodeUtils.ReInterpret<int, float>(math.min(CodeUtils.ReInterpret<float, int>(output[0]), math.cmin(((int3*)(void*)&fdata[0])[0]))); break;
                        case BlastVectorType.int4: output[0] = CodeUtils.ReInterpret<int, float>(math.min(CodeUtils.ReInterpret<float, int>(output[0]), math.cmin(((int4*)(void*)&fdata[0])[0]))); break;
                        case BlastVectorType.int1_n: output[0] = CodeUtils.ReInterpret<int, float>(math.min(CodeUtils.ReInterpret<float, int>(output[0]), -CodeUtils.ReInterpret<float, int>(fdata[0]))); break;
                        case BlastVectorType.int2_n: output[0] = CodeUtils.ReInterpret<int, float>(math.min(CodeUtils.ReInterpret<float, int>(output[0]), math.cmin(-((int2*)(void*)&fdata[0])[0]))); break;
                        case BlastVectorType.int3_n: output[0] = CodeUtils.ReInterpret<int, float>(math.min(CodeUtils.ReInterpret<float, int>(output[0]), math.cmin(-((int3*)(void*)&fdata[0])[0]))); break;
                        case BlastVectorType.int4_n: output[0] = CodeUtils.ReInterpret<int, float>(math.min(CodeUtils.ReInterpret<float, int>(output[0]), math.cmin(-((int4*)(void*)&fdata[0])[0]))); break;

                        default: goto ERROR_NOT_SUPPORTED;
                    }
                }
            }

            return;
        ERROR_NOT_SUPPORTED:
#if DEVELOPMENT_BUILD || TRACE
            Debug.LogError($"Blast.Interpretor.mina: combination of datatype {datatype} and vector size {popped_vector_size} not supported for output to vectorsize {vector_size} at codepointer {code_pointer} => {code[code_pointer]} ");
#endif
            return;
        }


        /// <summary>
        /// return the smallest of the arguments of any vectorsize
        /// </summary>
        void CALL_FN_CSUM(ref int code_pointer, ref BlastVariableDataType datatype, ref byte vector_size, ref float4 f4)
        {
            code_pointer++;
            byte c = decode62(in code[code_pointer], ref vector_size);
            code_pointer++;

            byte popped_vector_size = 0;
            bool is_negated = false;

            // as we always assign to x, fix a pointer to it instead of using the property
            // while burst knows about a float4.x the standard .net compiler calls property accessors 
            fixed (float4* p4 = &f4)
            {
                vector_size = 1;
                float* output = (float*)(void*)p4;

                // get first -> it determines output type and initializes our v4 output 
                c--;
                float* fdata = (float*)pop_p_info(ref code_pointer, out datatype, out popped_vector_size, out is_negated);

                // combine all data into 1 branch on a byte typed enumeration -> burst will compile this into a jump table 
                switch (datatype.Combine(popped_vector_size, is_negated))
                {
                    case BlastVectorType.float1: output[0] = fdata[0]; break;
                    case BlastVectorType.float2: output[0] = math.csum(((float2*)(void*)&fdata[0])[0]); break;
                    case BlastVectorType.float3: output[0] = math.csum(((float3*)(void*)&fdata[0])[0]); break;
                    case BlastVectorType.float4: output[0] = math.csum(((float4*)(void*)&fdata[0])[0]); break;

                    case BlastVectorType.float1_n: output[0] = -fdata[0]; break;
                    case BlastVectorType.float2_n: output[0] = math.csum(-((float2*)(void*)&fdata[0])[0]); break;
                    case BlastVectorType.float3_n: output[0] = math.csum(-((float3*)(void*)&fdata[0])[0]); break;
                    case BlastVectorType.float4_n: output[0] = math.csum(-((float4*)(void*)&fdata[0])[0]); break;

                    case BlastVectorType.int1: output[0] = fdata[0]; break;
                    case BlastVectorType.int2: output[0] = CodeUtils.ReInterpret<int, float>(math.csum(((int2*)(void*)&fdata[0])[0])); break;
                    case BlastVectorType.int3: output[0] = CodeUtils.ReInterpret<int, float>(math.csum(((int3*)(void*)&fdata[0])[0])); break;
                    case BlastVectorType.int4: output[0] = CodeUtils.ReInterpret<int, float>(math.csum(((int4*)(void*)&fdata[0])[0])); break;

                    // doesnt look nice, but still compiles fast as most of the casting is resolved at compiletime 
                    case BlastVectorType.int1_n: output[0] = fdata[0]; break;
                    case BlastVectorType.int2_n: output[0] = CodeUtils.ReInterpret<int, float>(math.csum(-((int2*)(void*)&fdata[0])[0])); break;
                    case BlastVectorType.int3_n: output[0] = CodeUtils.ReInterpret<int, float>(math.csum(-((int3*)(void*)&fdata[0])[0])); break;
                    case BlastVectorType.int4_n: output[0] = CodeUtils.ReInterpret<int, float>(math.csum(-((int4*)(void*)&fdata[0])[0])); break;

                    default: goto ERROR_NOT_SUPPORTED;
                }

                // then every other parameter
                while (c > 0)
                {
                    c--;

                    // we need to pop only once due to how the elements of the vector are organized in the data/stack
                    // - pop_or_value needs one call/float
                    fdata = (float*)pop_p_info(ref code_pointer, out datatype, out popped_vector_size, out is_negated);

                    switch (datatype.Combine(popped_vector_size, is_negated))
                    {
                        case BlastVectorType.float1: output[0] = output[0] + fdata[0]; break;
                        case BlastVectorType.float2: output[0] = output[0] + math.csum(((float2*)(void*)&fdata[0])[0]); break;
                        case BlastVectorType.float3: output[0] = output[0] + math.csum(((float3*)(void*)&fdata[0])[0]); break;
                        case BlastVectorType.float4: output[0] = output[0] + math.csum(((float4*)(void*)&fdata[0])[0]); break;

                        case BlastVectorType.float1_n: output[0] = output[0] - fdata[0]; break;
                        case BlastVectorType.float2_n: output[0] = output[0] + math.csum(-((float2*)(void*)&fdata[0])[0]); break;
                        case BlastVectorType.float3_n: output[0] = output[0] + math.csum(-((float3*)(void*)&fdata[0])[0]); break;
                        case BlastVectorType.float4_n: output[0] = output[0] + math.csum(-((float4*)(void*)&fdata[0])[0]); break;

                        // doesnt look nice, but still compiles fast as most of the casting is resolved at compiletime 
                        case BlastVectorType.int1: output[0] = CodeUtils.ReInterpret<int, float>(CodeUtils.ReInterpret<float, int>(output[0]) + CodeUtils.ReInterpret<float, int>(fdata[0])); break;
                        case BlastVectorType.int2: output[0] = CodeUtils.ReInterpret<int, float>(CodeUtils.ReInterpret<float, int>(output[0]) + math.cmin(((int2*)(void*)&fdata[0])[0])); break;
                        case BlastVectorType.int3: output[0] = CodeUtils.ReInterpret<int, float>(CodeUtils.ReInterpret<float, int>(output[0]) + math.cmin(((int3*)(void*)&fdata[0])[0])); break;
                        case BlastVectorType.int4: output[0] = CodeUtils.ReInterpret<int, float>(CodeUtils.ReInterpret<float, int>(output[0]) + math.cmin(((int4*)(void*)&fdata[0])[0])); break;
                        case BlastVectorType.int1_n: output[0] = CodeUtils.ReInterpret<int, float>(CodeUtils.ReInterpret<float, int>(output[0]) - CodeUtils.ReInterpret<float, int>(fdata[0])); break;
                        case BlastVectorType.int2_n: output[0] = CodeUtils.ReInterpret<int, float>(CodeUtils.ReInterpret<float, int>(output[0]) + math.cmin(-((int2*)(void*)&fdata[0])[0])); break;
                        case BlastVectorType.int3_n: output[0] = CodeUtils.ReInterpret<int, float>(CodeUtils.ReInterpret<float, int>(output[0]) + math.cmin(-((int3*)(void*)&fdata[0])[0])); break;
                        case BlastVectorType.int4_n: output[0] = CodeUtils.ReInterpret<int, float>(CodeUtils.ReInterpret<float, int>(output[0]) + math.cmin(-((int4*)(void*)&fdata[0])[0])); break;

                        default: goto ERROR_NOT_SUPPORTED;
                    }                            
                }
            }

            return;
        ERROR_NOT_SUPPORTED:
#if DEVELOPMENT_BUILD || TRACE
            Debug.LogError($"Blast.Interpretor.csum: combination of datatype {datatype} and vector size {popped_vector_size} not supported for output to vectorsize {vector_size} at codepointer {code_pointer} => {code[code_pointer]} ");
#endif
            return;
        }






    }
}
