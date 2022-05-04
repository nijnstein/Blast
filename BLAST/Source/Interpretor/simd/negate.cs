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
        /// negate f4[i].xyzw
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public void negate_f4([NoAlias] float4* f4, int ssmd_datacount)
        {
            if (IsUnrolled)
            {
                int i = 0;
#if STANDALONE_VSBUILD
                float* p = (float*)(void*)f4;
                while (i < (ssmd_datacount & ~3))
                {
                    p[0] = -p[0];
                    p[1] = -p[1];
                    p[2] = -p[2];
                    p[3] = -p[3];
                    p[4] = -p[4];
                    p[5] = -p[5];
                    p[6] = -p[6];
                    p[7] = -p[7];
                    p[8] = -p[8];
                    p[9] = -p[9];
                    p[10] = -p[10];
                    p[11] = -p[11];
                    p[12] = -p[12];
                    p[13] = -p[13];
                    p[14] = -p[14];
                    p[15] = -p[15];
                    p += 16;
                    i += 4;
                }
                while (i < ssmd_datacount)
                {
                    p[0] = -p[0];
                    p[1] = -p[1];
                    p[2] = -p[2];
                    p[3] = -p[3];
                    p += 4;
                    i += 1;
                }
#else 
                // in unity use float4, it maps better to assembly 
                float4* p = f4;
                while(i < (ssmd_datacount & ~3))
                {
                    p[0] = -p[0];
                    p[1] = -p[1];
                    p[2] = -p[2];
                    p[3] = -p[3];
                    p += 4;
                    i += 4; 
                }
                while(i < ssmd_datacount)
                {
                    p[0] = -p[0];
                    p += 1;
                    i += 1;
                }
#endif
            }
            else
            {
                for (int i = 0; i < ssmd_datacount; i++)
                {
                    f4[i] = -f4[i];
                }
            }
        }




        /// <summary>
        /// negate f4[i].xyz
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public void negate_f3([NoAlias] float4* f4, int ssmd_datacount)
        {
            if (IsUnrolled)
            {
                int i = 0;
#if STANDALONE_VSBUILD
                float* p = (float*)(void*)f4;
                while (i < (ssmd_datacount & ~3))
                {
                    p[0] = -p[0];
                    p[1] = -p[1];
                    p[2] = -p[2];
                    
                    p[4] = -p[4];
                    p[5] = -p[5];
                    p[6] = -p[6];
                    
                    p[8] = -p[8];
                    p[9] = -p[9];
                    p[10] = -p[10];
                    
                    p[12] = -p[12];
                    p[13] = -p[13];
                    p[14] = -p[14];
                    
                    p += 16;
                    i += 4;
                }
                while (i < ssmd_datacount)
                {
                    p[0] = -p[0];
                    p[1] = -p[1];
                    p[2] = -p[2];
                    
                    p += 4;
                    i += 1;
                }
#else 
                // in unity use float4, it maps better to assembly 
                float4* p = f4;
                while(i < (ssmd_datacount & ~3))
                {
                    p[0].xyz = -p[0].xyz;
                    p[1].xyz = -p[1].xyz;
                    p[2].xyz = -p[2].xyz;
                    p[3].xyz = -p[3].xyz;
                    p += 4;
                    i += 4; 
                }
                while(i < ssmd_datacount)
                {
                    p[0].xyz = -p[0].xyz;
                    p += 1;
                    i += 1;
                }
#endif
            }
            else
            {
                for (int i = 0; i < ssmd_datacount; i++)
                {
                    f4[i].xyz = -f4[i].xyz;
                }
            }
        }




        /// <summary>
        /// negate f4[i].xy
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public void negate_f2([NoAlias] float4* f4, int ssmd_datacount)
        {
            if (IsUnrolled)
            {
                int i = 0;
#if STANDALONE_VSBUILD
                float* p = (float*)(void*)f4;
                while (i < (ssmd_datacount & ~3))
                {
                    p[0] = -p[0];
                    p[1] = -p[1];

                    p[4] = -p[4];
                    p[5] = -p[5];

                    p[8] = -p[8];
                    p[9] = -p[9];

                    p[12] = -p[12];
                    p[13] = -p[13];

                    p += 16;
                    i += 4;
                }
                while (i < ssmd_datacount)
                {
                    p[0] = -p[0];
                    p[1] = -p[1];

                    p += 4;
                    i += 1;
                }
#else 
                // in unity use float4, it should map better to assembly  TODO TEST THIS
                float4* p = f4;
                while(i < (ssmd_datacount & ~3))
                {
                    p[0].xy = -p[0].xy;
                    p[1].xy = -p[1].xy;
                    p[2].xy = -p[2].xy;
                    p[3].xy = -p[3].xy;
                    p += 4;
                    i += 4; 
                }
                while(i < ssmd_datacount)
                {
                    p[0].xy = -p[0].xy;
                    p += 1;
                    i += 1;
                }
#endif
            }
            else
            {
                for (int i = 0; i < ssmd_datacount; i++)
                {
                    f4[i].xy = -f4[i].xy;
                }
            }
        }



        /// <summary>
        /// negate f4[i].x
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public void negate_f1([NoAlias] float4* f4, int ssmd_datacount)
        {
            if (IsUnrolled)
            {
                int i = 0;
                float* p = (float*)(void*)f4;
                while (i < (ssmd_datacount & ~3))
                {
                    p[0] = -p[0];
                    p[4] = -p[4];
                    p[8] = -p[8];
                    p[12] = -p[12];

                    p += 16;
                    i += 4;
                }
                while (i < ssmd_datacount)
                {
                    p[0] = -p[0];

                    p += 4;
                    i += 1;
                }
            }
            else
            {
                for (int i = 0; i < ssmd_datacount; i++)
                {
                    f4[i].x = -f4[i].x;
                }
            }
        }


    }
}
