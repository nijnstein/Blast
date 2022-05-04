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
#else
    using UnityEngine;
    using Unity.Burst.CompilerServices;
#endif


using Unity.Burst;
using Unity.Mathematics;
using System.Runtime.CompilerServices;
using NSS.Blast.Interpretor;

namespace NSS.Blast.SSMD
{
    unsafe public partial struct BlastSSMDInterpretor
    {
        // 
        // - seperate function, non-inlined on the stackallocating ones
        // - LLVM doesnt conditionally stackallocate: instead all stackallocations are done at function entry
        //   that would mean we would have a lot of useless allocations going on when handling small datatypes 
        // - it is very likely other compilers behave the same


        void CALL_RANDOM_F1(ref int code_pointer, int ssmd_datacount, [NoAlias] float4* register)
        {
            for (int i = 0; i < ssmd_datacount; i++)
            {
                register[i].x = random.NextFloat(0, 1);
            }
        }

        void CALL_RANDOM_F2(ref int code_pointer, int ssmd_datacount, [NoAlias] float4* register)
        {
            for (int i = 0; i < ssmd_datacount; i++)
            {
                register[i].x = random.NextFloat(0, 1);
                register[i].y = random.NextFloat(0, 1);
            }
        }
        void CALL_RANDOM_F3(ref int code_pointer, int ssmd_datacount, [NoAlias] float4* register)
        {
            for (int i = 0; i < ssmd_datacount; i++)
            {
                register[i].x = random.NextFloat(0, 1);
                register[i].y = random.NextFloat(0, 1);
                register[i].z = random.NextFloat(0, 1);
            }
        }
        void CALL_RANDOM_F4(ref int code_pointer, int ssmd_datacount, [NoAlias] float4* register)
        {
            for (int i = 0; i < ssmd_datacount; i++)
            {
                register[i].x = random.NextFloat(0, 1);
                register[i].y = random.NextFloat(0, 1);
                register[i].z = random.NextFloat(0, 1);
                register[i].w = random.NextFloat(0, 1);
            }
        }


#if !STANDALONE_VSBUILD
        [Unity.Burst.CompilerServices.SkipLocalsInit]
#endif
        [MethodImpl(MethodImplOptions.NoInlining)]
        void CALL_RANDOM_F1_f1(ref int code_pointer, int ssmd_datacount, [NoAlias] float4* register)
        {
            float* fpa = stackalloc float[ssmd_datacount];

            pop_fx_into_ref<float>(ref code_pointer, fpa, ssmd_datacount);

            for (int i = 0; i < ssmd_datacount; i++)
            {
                register[i].x = random.NextFloat(0, fpa[i]);
            }
        }

#if !STANDALONE_VSBUILD
        [Unity.Burst.CompilerServices.SkipLocalsInit]
#endif
        [MethodImpl(MethodImplOptions.NoInlining)]
        void CALL_RANDOM_F2_f2(ref int code_pointer, int ssmd_datacount, [NoAlias] float4* register)
        {
            float2* fpa = stackalloc float2[ssmd_datacount];

            pop_fx_into_ref<float2>(ref code_pointer, fpa, ssmd_datacount);

            for (int i = 0; i < ssmd_datacount; i++)
            {
                register[i].x = random.NextFloat(0, fpa[i].x);
                register[i].y = random.NextFloat(0, fpa[i].y);
            }
        }

#if !STANDALONE_VSBUILD
        [Unity.Burst.CompilerServices.SkipLocalsInit]
#endif
        [MethodImpl(MethodImplOptions.NoInlining)]
        void CALL_RANDOM_F3_f3(ref int code_pointer, int ssmd_datacount, [NoAlias] float4* register)
        {
            float3* fpa = stackalloc float3[ssmd_datacount];

            pop_fx_into_ref<float3>(ref code_pointer, fpa, ssmd_datacount);

            for (int i = 0; i < ssmd_datacount; i++)
            {
                register[i].x = random.NextFloat(0, fpa[i].x);
                register[i].y = random.NextFloat(0, fpa[i].y);
                register[i].z = random.NextFloat(0, fpa[i].z);
            }
        }

#if !STANDALONE_VSBUILD
        [Unity.Burst.CompilerServices.SkipLocalsInit]
#endif
        [MethodImpl(MethodImplOptions.NoInlining)]
        void CALL_RANDOM_F4_f4(ref int code_pointer, int ssmd_datacount, [NoAlias] float4* register)
        {
            float4* fpa = stackalloc float4[ssmd_datacount];

            pop_fx_into_ref<float4>(ref code_pointer, fpa, ssmd_datacount);

            for (int i = 0; i < ssmd_datacount; i++)
            {
                register[i].x = random.NextFloat(0, fpa[i].x);
                register[i].y = random.NextFloat(0, fpa[i].y);
                register[i].z = random.NextFloat(0, fpa[i].z);
                register[i].w = random.NextFloat(0, fpa[i].w);
            }
        }

#if !STANDALONE_VSBUILD
        [Unity.Burst.CompilerServices.SkipLocalsInit]
#endif
        [MethodImpl(MethodImplOptions.NoInlining)]
        void CALL_RANDOM_F1_f1f1(ref int code_pointer, int ssmd_datacount, [NoAlias] float4* register)
        {
            float* fpa = stackalloc float[ssmd_datacount];
            float* fpb = stackalloc float[ssmd_datacount];

            pop_fx_into_ref<float>(ref code_pointer, fpa, ssmd_datacount);  
            pop_fx_into_ref<float>(ref code_pointer, fpb, ssmd_datacount);  

            for (int i = 0; i < ssmd_datacount; i++)
            {
                register[i].x = random.NextFloat(fpa[i], fpb[i]);
            }
        }

#if !STANDALONE_VSBUILD
        [Unity.Burst.CompilerServices.SkipLocalsInit]
#endif
        [MethodImpl(MethodImplOptions.NoInlining)]
        void CALL_RANDOM_F2_f2f2(ref int code_pointer, int ssmd_datacount, [NoAlias] float4* register)
        {
            float2* fpa = stackalloc float2[ssmd_datacount];
            float2* fpb = stackalloc float2[ssmd_datacount];

            pop_fx_into_ref<float2>(ref code_pointer, fpa, ssmd_datacount); 
            pop_fx_into_ref<float2>(ref code_pointer, fpb, ssmd_datacount); 

            for (int i = 0; i < ssmd_datacount; i++)
            {
                register[i].x = random.NextFloat(fpa[i].x, fpb[i].x);
                register[i].y = random.NextFloat(fpa[i].y, fpb[i].y);
            }
        }

#if !STANDALONE_VSBUILD
        [Unity.Burst.CompilerServices.SkipLocalsInit]
#endif
        [MethodImpl(MethodImplOptions.NoInlining)]
        void CALL_RANDOM_F3_f3f3(ref int code_pointer, int ssmd_datacount, [NoAlias] float4* register)
        {
            float3* fpa = stackalloc float3[ssmd_datacount];
            float3* fpb = stackalloc float3[ssmd_datacount];

            pop_fx_into_ref<float3>(ref code_pointer, fpa, ssmd_datacount); 
            pop_fx_into_ref<float3>(ref code_pointer, fpb, ssmd_datacount); 

            for (int i = 0; i < ssmd_datacount; i++)
            {
                register[i].x = random.NextFloat(fpa[i].x, fpb[i].x);
                register[i].y = random.NextFloat(fpa[i].y, fpb[i].y);
                register[i].z = random.NextFloat(fpa[i].z, fpb[i].z);
            }
        }

#if !STANDALONE_VSBUILD
        [Unity.Burst.CompilerServices.SkipLocalsInit]
#endif
        [MethodImpl(MethodImplOptions.NoInlining)]
        void CALL_RANDOM_F4_f4f4(ref int code_pointer, int ssmd_datacount, [NoAlias] float4* register)
        {                                              
            float4* fpa = stackalloc float4[ssmd_datacount];
            float4* fpb = stackalloc float4[ssmd_datacount];

            pop_fx_into_ref<float4>(ref code_pointer, fpa, ssmd_datacount);   // _ref increases code_pointer according to data
            pop_fx_into_ref<float4>(ref code_pointer, fpb, ssmd_datacount);   // _ref increases code_pointer according to data

            for (int i = 0; i < ssmd_datacount; i++)
            {
                register[i].x = random.NextFloat(fpa[i].x, fpb[i].x);
                register[i].y = random.NextFloat(fpa[i].y, fpb[i].y);
                register[i].z = random.NextFloat(fpa[i].z, fpb[i].z);
                register[i].w = random.NextFloat(fpa[i].w, fpb[i].w);
            }
        }


        /// <summary>
        /// 
        /// </summary>
        void CALL_RANDOM(ref int code_pointer, ref byte vector_size, int ssmd_datacount, [NoAlias] float4* register)
        {
            byte parameter_count = code[code_pointer];
            byte defined_vector_size = vector_size; 

#if DECODE_44
            c = BlastInterpretor.decode44in c, ref vector_size);
#else
            parameter_count = BlastInterpretor.decode62(in parameter_count, ref defined_vector_size);
#endif                               
            code_pointer++;                        

            BlastVariableDataType datatype;
            if (parameter_count > 0)
            {
                pop_op_meta_cp(code_pointer, out datatype, out vector_size);
                if (vector_size != defined_vector_size) goto VECTOR_SIZE_ERROR;
            }
            else
            {
                // without paramaters we fix the datatype to numeric 
                datatype = BlastVariableDataType.Numeric; 
                vector_size = defined_vector_size; 
            }

            // tripple select: datatype | parametercount | vectorsize
            switch (datatype)
            {
                case BlastVariableDataType.Numeric:
                    {
                        // for random, c specifies range:
                        switch (parameter_count)
                        {
                            // 0 = 0..1  == default
                            case 0:
                                switch (vector_size)
                                {
                                    case 1: CALL_RANDOM_F1(ref code_pointer, ssmd_datacount, register); break;
                                    case 2: CALL_RANDOM_F2(ref code_pointer, ssmd_datacount, register); break;
                                    case 3: CALL_RANDOM_F3(ref code_pointer, ssmd_datacount, register); break;
                                    case 0:
                                    case 4: CALL_RANDOM_F4(ref code_pointer, ssmd_datacount, register); break;
                                    default: goto CALL_ERROR;
                                }
                                break;
                            // 1 = 1 ops   0...max 
                            case 1:
                                switch (vector_size)
                                {
                                    case 1: CALL_RANDOM_F1_f1(ref code_pointer, ssmd_datacount, register); break;
                                    case 2: CALL_RANDOM_F2_f2(ref code_pointer, ssmd_datacount, register); break;
                                    case 3: CALL_RANDOM_F3_f3(ref code_pointer, ssmd_datacount, register); break;
                                    case 0:
                                    case 4: CALL_RANDOM_F4_f4(ref code_pointer, ssmd_datacount, register); break;
                                    default: goto CALL_ERROR;
                                }
                                break;
                            // 2 = 2 ops with values after this op    min..max 
                            case 2:
                                switch (vector_size)
                                {
                                    case 1: CALL_RANDOM_F1_f1f1(ref code_pointer, ssmd_datacount, register); break;
                                    case 2: CALL_RANDOM_F2_f2f2(ref code_pointer, ssmd_datacount, register); break;
                                    case 3: CALL_RANDOM_F3_f3f3(ref code_pointer, ssmd_datacount, register); break;
                                    case 0:
                                    case 4: CALL_RANDOM_F4_f4f4(ref code_pointer, ssmd_datacount, register); break;
                                    default: goto CALL_ERROR;
                                }
                                break;

                            default: goto CALL_ERROR; 
                        }                         
                    }
                    break;

                default: goto CALL_ERROR; 
            }

            code_pointer--;
            return;


        VECTOR_SIZE_ERROR:
            if (IsTrace)
            {
                Debug.LogError($"blast.ssmd.interpretor.random: vectorsize mismatch, function defines {defined_vector_size} while parameter = vector_size: {vector_size} , datatype: {datatype} at codepointer: {code_pointer}");
            }
            return;

        CALL_ERROR:
            if (IsTrace)
            {
                Debug.LogError($"blast.ssmd.interpretor.random: datatype|vectorsize mismatch, vector_size: {vector_size}, datatype: {datatype} not supported");
            }
            return;
        }


    }
}