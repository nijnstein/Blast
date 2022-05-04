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

namespace NSS.Blast.SSMD
{
    unsafe public partial struct BlastSSMDInterpretor
    {


        /// <summary>
        /// select first input on a false condition, select the second on a true condition that is put in parameter 3
        /// </summary>
        /// <remarks>
        /// 3 inputs, first 2 any type, 3rd type equal or scalar bool
        /// 
        /// c[x] = select(a[x], b[x], condition[1/x])
        /// 
        /// - input = outputvectorsize
        /// - 1 output  
        /// 
        /// Assembly for scalar constants<c>a = select(1, 2, 1) => setf a select 1 2 1 nop</c>
        /// 
        /// Assembly sample with vectors: <c>a = select((1 1), (2 2), 1) => setf a select 1 pop pop nop</c>
        /// </remarks>
#if !STANDALONE_VSBUILD
        [SkipLocalsInit]
#endif
        void get_select_result([NoAlias] void* temp, ref int code_pointer, ref byte vector_size, int ssmd_datacount, [NoAlias] float4* f4)
        {
            // upon entry code_pointer is located on the function operation 
            BlastVariableDataType datatype;
            byte vector_size_condition;

            //
            // need to reorder parameters while compiling for better memory efficienty executing it 
            // - if we can execute the condition first we can skip reading 1 of the parameters (we do need to 
            //   move stackpointers though)
            // - compiler update for SSMD only? or also normal?
            // 

            // peek vectorsize of first option
            pop_op_meta_cp(code_pointer, out datatype, out vector_size);

            //
            // IMPORTANT
            // stack allocations are moved to the start of the function and are always done,
            // regardless conditionals, avoid allocating too much memory by calculating the amount needed 
            // and casting the resulting pointers in each vectorsize-case
            //
            int data_byte_size = 4 * ssmd_datacount * vector_size;

            // we could use memory from f4 to store 1 array, but that would probably mess up vectorizing the inner loops
            byte* local1 = stackalloc byte[data_byte_size];
            byte* local2 = stackalloc byte[data_byte_size];

            //
            // allocations could also be avoided, with each combination (constant/pop/var) and each vectorsize of each parameter in a seperate case... 
            // this will happen a lot as generics is unusable.. codegen? TODO
            //

            switch (vector_size)
            {

                case 1:

                    float* o1 = (float*)(void*)local1;
                    float* o2 = (float*)(void*)local2;

                    pop_fx_into_ref<float>(ref code_pointer, o1);
                    pop_fx_into_ref<float>(ref code_pointer, o2);
                    pop_fx_into_ref<float>(ref code_pointer, (float*)temp);

                    for (int i = 0; i < ssmd_datacount; i++)
                    {
                        // not using f4 for storing any operand should allow for the compiler to optimize better
                        // we could also optimize this by handling 4 at once in a float4.. TODO
                        f4[i].x = math.select(o1[i], o2[i], ((float*)temp)[i] != 0);
                    }
                    break;


                case 2:
                    float2* o21 = (float2*)(void*)local1;
                    float2* o22 = (float2*)(void*)local2;

                    pop_fx_into_ref<float2>(ref code_pointer, o21);
                    pop_fx_into_ref<float2>(ref code_pointer, o22);

                    pop_op_meta_cp(code_pointer, out datatype, out vector_size_condition);

                    if (vector_size_condition == 1)
                    {
                        pop_fx_into_ref<float>(ref code_pointer, (float*)temp);
                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            f4[i].xy = math.select(o21[i], o22[i], ((float*)temp)[i] != 0);
                        }
                    }
                    else
                    {
                        pop_fx_into_ref<float2>(ref code_pointer, (float2*)temp);
                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            f4[i].xy = math.select(o21[i], o22[i], ((float2*)temp)[i] != 0);
                        }
                    }
                    break;

                case 3:
                    float3* o31 = (float3*)(void*)local1;
                    float3* o32 = (float3*)(void*)local2;

                    pop_fx_into_ref<float3>(ref code_pointer, o31);
                    pop_fx_into_ref<float3>(ref code_pointer, o32);

                    pop_op_meta_cp(code_pointer, out datatype, out vector_size_condition);

                    if (vector_size_condition == 1)
                    {
                        pop_fx_into_ref<float>(ref code_pointer, (float*)temp);
                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            f4[i].xyz = math.select(o31[i], o32[i], ((float*)temp)[i] != 0);
                        }
                    }
                    else
                    {
                        pop_fx_into_ref<float3>(ref code_pointer, (float3*)temp);
                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            f4[i].xyz = math.select(o31[i], o32[i], ((float3*)temp)[i] != 0);
                        }
                    }
                    break;

                case 0:
                case 4:
                    {
                        vector_size = 4;

                        float4* o41 = (float4*)(void*)local1;
                        float4* o42 = (float4*)(void*)local2;

                        pop_fx_into_ref<float4>(ref code_pointer, o41);
                        pop_fx_into_ref<float4>(ref code_pointer, o42);
                        pop_op_meta_cp(code_pointer, out datatype, out vector_size_condition);

                        if (vector_size_condition == 1)
                        {
                            pop_fx_into_ref<float>(ref code_pointer, (float*)temp);
                            for (int i = 0; i < ssmd_datacount; i++)
                            {
                                f4[i] = math.select(o41[i], o42[i], ((float*)temp)[i] != 0);
                            }
                        }
                        else
                        {
                            pop_fx_into_ref<float4>(ref code_pointer, (float4*)temp);
                            for (int i = 0; i < ssmd_datacount; i++)
                            {
                                f4[i] = math.select(o41[i], o42[i], ((float4*)temp)[i] != 0);
                            }
                        }
                    }
                    break;

#if DEVELOPMENT_BUILD || TRACE
                default:
                    Debug.LogError($"blast.ssmd.interpretor.select: datatype|vectorsize mismatch, vector_size: {vector_size}, datatype: {datatype} not supported");
                    break;
#endif
            }

            code_pointer--;
        }









    }
}
