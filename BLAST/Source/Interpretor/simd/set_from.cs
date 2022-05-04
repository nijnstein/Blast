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


using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Mathematics;

namespace NSS.Blast.SSMD
{
    unsafe public partial struct simd
    {

        /// <summary>
        /// f4[].x = constant
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public void set_f1_from_constant([NoAlias] float4* destination, in float constant, in int ssmd_datacount)
        {
            float* p = (float*)(void*)destination;
            int i = 0;
            while (i < (ssmd_datacount & ~3))
            {
                p[0] = constant;
                p[4] = constant;
                p[8] = constant;
                p[12] = constant;
                p += 16;
                i += 4;
            }
            while (i < ssmd_datacount)
            {
                p[0] = constant;
                p += 4;
                i += 1;
            }

        }

        /// <summary>
        /// f1[] = constant
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void set_f1_from_constant([NoAlias] float* destination, in float constant, in int ssmd_datacount)
        {
            // somehow, in .net core, memset is a lot slower
            float* p = (float*)(void*)destination;
            int i = 0;
            while (i < (ssmd_datacount & ~3))
            {
                p[0] = constant;
                p[1] = constant;
                p[2] = constant;
                p[3] = constant;
                p += 4;
                i += 4;
            }
            while (i < ssmd_datacount)
            {
                p[0] = constant;
                p += 1;
                i += 1;
            }
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void set_f2_from_constant([NoAlias] float4* destination, in float constant, in int ssmd_datacount)
        {
            float* p = (float*)(void*)destination;
            for (int i = 0; i < ssmd_datacount; i++)
            {
                p[0] = constant;
                p[1] = constant;
                p += 4;
            }
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public void set_f3_from_constant([NoAlias] float4* destination, in float constant, in int ssmd_datacount)
        {
            float* p = (float*)(void*)destination;
            for (int i = 0; i < ssmd_datacount; i++)
            {
                p[0] = constant;
                p[1] = constant;
                p[2] = constant;
                p += 4;
            }
        }
       
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public void set_f4_from_constant([NoAlias] float4* destination, in float constant, in int ssmd_datacount)
        {
            float* p = (float*)(void*)destination;
            for (int i = 0; i < ssmd_datacount; i++)
            {
                p[0] = constant;
                p[1] = constant;
                p[2] = constant;
                p[3] = constant;
                p += 4;
            }
        }

        /// <summary>
        /// set a dest.x buffer from data[offset]
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public void set_from_data_f1([NoAlias] float4* destination, [NoAlias] void** data, in int offset, in bool minus, in bool not, bool is_aligned, int stride, int ssmd_datacount)
        {
            if (minus || not)
            {
                if (minus)
                {
                    simd.move_indexed_data_as_f1_to_f1f4_negated(destination, data, stride, is_aligned, offset, ssmd_datacount);
                    return;
                }

                if (not)
                {
                    for (int i = 0; i < ssmd_datacount; i++)
                    {
                        destination[i].x = math.select(0, 1, ((float*)data[i])[offset] == 0);
                    }
                    return;
                }
            }

            simd.move_indexed_data_as_f1_to_f1f4(destination, data, stride, is_aligned, offset, ssmd_datacount);
        }

        /// <summary>
        /// set a dest.xy buffer from data[offset]
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public void set_from_data_f2([NoAlias] float4* destination, [NoAlias] void** data, in int offset, in bool minus, in bool not, bool is_aligned, int stride, int ssmd_datacount)
        {
            if (minus || not)
            {
                if (minus)
                {
                    simd.move_indexed_data_as_f2_to_f2_negated(destination, data, stride, is_aligned, offset, ssmd_datacount);
                    return;
                }
                if (not)
                {
                    int offsetb = offset + 1;
                    for (int i = 0; i < ssmd_datacount; i++)
                    {
                        destination[i].x = math.select(0f, 1f, ((float*)data[i])[offset] == 0);
                        destination[i].y = math.select(0f, 1f, ((float*)data[i])[offsetb] == 0);
                    }
                    return;
                }
            }
            simd.move_indexed_data_as_f2_to_f2(destination, data, stride, is_aligned, offset, ssmd_datacount);
        }

        /// <summary>
        /// set a dest.xyz buffer from data[offset]
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public void set_from_data_f3([NoAlias] float4* destination, [NoAlias] void** data, in int offset, in bool minus, in bool not, bool is_aligned, int stride, int ssmd_datacount)
        {
            if (minus || not)
            {
                if (minus)
                {
                    simd.move_indexed_data_as_f3_to_f3_negated(destination, data, stride, is_aligned, offset, ssmd_datacount);
                    return;
                }
                if (not)
                {
                    int offsetb = offset + 1;
                    int offsetc = offset + 2;
                    for (int i = 0; i < ssmd_datacount; i++)
                    {
                        destination[i].x = math.select(0f, 1f, ((float*)data[i])[offset] == 0);
                        destination[i].y = math.select(0f, 1f, ((float*)data[i])[offsetb] == 0);
                        destination[i].z = math.select(0f, 1f, ((float*)data[i])[offsetc] == 0);
                    }
                    return;
                }
            }
            simd.move_indexed_data_as_f3_to_f3(destination, data, stride, is_aligned, offset, ssmd_datacount);
        }


        /// <summary>
        /// set a dest.xyzw buffer from data[offset]
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public void set_from_data_f4([NoAlias] float4* destination, [NoAlias] void** data, in int offset, in bool minus, in bool not, bool is_aligned, int stride, int ssmd_datacount)
        {
            if (minus || not)
            {
                if (minus)
                {
                    simd.move_indexed_data_as_f4_to_f4_negated(destination, data, stride, is_aligned, offset, ssmd_datacount);
                    return;
                }
                if (not)
                {
                    for (int i = 0; i < ssmd_datacount; i++)
                    {
                        destination[i] = math.select((float4)0f, (float4)1f, ((float4*)(void*)&((float*)data[i])[offset])[0] == (float4)0f);
                    }
                    return;
                }
            }
            simd.move_indexed_data_as_f4_to_f4(destination, data, stride, is_aligned, offset, ssmd_datacount);
        }

    }
}
