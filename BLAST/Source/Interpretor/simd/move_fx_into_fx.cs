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

        #region move_fx_constant_into_fx_array_as_fx


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public void move_f1_constant_into_f4_array_as_f1([NoAlias] float4* destination, float constant, int ssmd_datacount)
        {
            float* fa = (float*)(void*)destination;
            for (int i = 0; i < ssmd_datacount; i++)
            {
                fa[0] = constant;   // x
                fa += 4;            // the constant index and stride help the compiler to output simd assebly
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public void move_f1_constant_into_f4_array_as_f2([NoAlias] float4* destination, float constant, int ssmd_datacount)
        {
            float* fa = (float*)(void*)destination;
            for (int i = 0; i < ssmd_datacount; i++)
            {
                fa[0] = constant;   // x
                fa[1] = constant;   // y
                fa += 4;            // the constant index and stride help the compiler to output simd assebly
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public void move_f1_constant_into_f4_array_as_f3([NoAlias] float4* destination, float constant, int ssmd_datacount)
        {
            float* fa = (float*)(void*)destination;
            for (int i = 0; i < ssmd_datacount; i++)
            {
                fa[0] = constant;   // x
                fa[1] = constant;   // y
                fa[2] = constant;   // z
                fa += 4;            // the constant index and stride help the compiler to output simd assebly
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public void move_f1_constant_into_f4_array_as_f4([NoAlias] float4* destination, float constant, int ssmd_datacount)
        {
            float* fa = (float*)(void*)destination;
            for (int i = 0; i < ssmd_datacount; i++)
            {
                fa[0] = constant;   // x
                fa[1] = constant;   // y
                fa[2] = constant;   // z
                fa[3] = constant;   // w
                fa += 4;            // the constant index and stride help the compiler to output simd assebly
            }
        }


        #endregion

        #region  move_fx_array_into_fx_array_as_fx

        /// <summary>
        /// a[i].xyz = b[i].x
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public void move_f1_array_into_f4_array_as_f3([NoAlias] float4* destination, [NoAlias] float4* source, int ssmd_datacount)
        {
            float* fa = (float*)(void*)destination;
            float* fb = (float*)(void*)source;

            for (int i = 0; i < ssmd_datacount; i++)
            {
                fa[0] = fb[0];   // x
                fa[1] = fb[0];   // y
                fa[2] = fb[0];   // z
                fa += 4;         // the constant index and stride help the compiler to output simd assebly
                fb += 4;
            }
        }
        /// <summary>
        /// a[i].xyz = b[i].x
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public void move_f1_array_into_f4_array_as_f1([NoAlias] float4* destination, [NoAlias] float4* source, int ssmd_datacount)
        {
            float* fa = (float*)(void*)destination;
            float* fb = (float*)(void*)source;

            for (int i = 0; i < ssmd_datacount; i++)
            {
                fa[0] = fb[0];   // x
                fa += 4;         // the constant index and stride help the compiler to output simd assebly
                fb += 4;
            }
        }

        /// <summary>
        /// a[i].x = b[i]    (float4.x = float1.x)
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public void move_f1_array_into_f4_array_as_f1([NoAlias] float4* destination, [NoAlias] float* source, int ssmd_datacount)
        {
            float* fa = (float*)(void*)destination;
            float* fb = (float*)(void*)source;

            for (int i = 0; i < ssmd_datacount; i++)
            {
                fa[0] = fb[0]; 
                fa += 4;       
                fb += 1;
            }
        }

        /// <summary>
        /// a[i].xy = b[i]    (float4.xy = float2.xy)
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public void move_f2_array_into_f4_array_as_f2([NoAlias] float4* destination, [NoAlias] float2* source, int ssmd_datacount)
        {
            float* fa = (float*)(void*)destination;
            float* fb = (float*)(void*)source;

            for (int i = 0; i < ssmd_datacount; i++)
            {
                fa[0] = fb[0];
                fa[1] = fb[1];
                fa += 4;
                fb += 2;
            }
        }

        /// <summary>
        /// a[i].xyz = b[i]    (float4.xyz = float3.xyz)
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public void move_f3_array_into_f4_array_as_f3([NoAlias] float4* destination, [NoAlias] float3* source, int ssmd_datacount)
        {
            float* fa = (float*)(void*)destination;
            float* fb = (float*)(void*)source;

            for (int i = 0; i < ssmd_datacount; i++)
            {
                fa[0] = fb[0];
                fa[1] = fb[1];
                fa[1] = fb[1];
                fa += 4;
                fb += 3;
            }
        }

        /// <summary>
        /// a[i].xyz = b[i].x
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public void move_f1_array_into_f4_array_as_f2([NoAlias] float4* destination, [NoAlias] float4* source, int ssmd_datacount)
        {
            float* fa = (float*)(void*)destination;
            float* fb = (float*)(void*)source;

            for (int i = 0; i < ssmd_datacount; i++)
            {
                fa[0] = fb[0];   // x
                fa[1] = fb[0];   // y
                fa += 4;         // the constant index and stride help the compiler to output simd assebly
                fb += 4;
            }
        }


        /// <summary>
        /// a[i].xyzw = b[i].x
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public void move_f1_array_into_f4_array_as_f4([NoAlias] float4* destination, [NoAlias] float4* source, int ssmd_datacount)
        {
            float* fa = (float*)(void*)destination;
            float* fb = (float*)(void*)source;

            for (int i = 0; i < ssmd_datacount; i++)
            {
                fa[0] = fb[0];   // x
                fa[1] = fb[0];   // y
                fa[2] = fb[0];   // z
                fa[3] = fb[0];   // z
                fa += 4;         // the constant index and stride help the compiler to output simd assebly
                fb += 4;
            }
        }


        /// <summary>
        /// a[i].xyzw = b[i].x
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public void move_f2_array_into_f4_array_as_f2([NoAlias] float4* destination, [NoAlias] float4* source, int ssmd_datacount)
        {
            // you would expect memcpy to be faster but it isnt
            float* fa = (float*)(void*)destination;
            float* fb = (float*)(void*)source;

            for (int i = 0; i < ssmd_datacount; i++)
            {
                fa[0] = fb[0];   // x
                fa[1] = fb[1];   // y
                fa += 4;         // the constant index and stride help the compiler to output simd assebly
                fb += 4;
            }
        }


        /// <summary>
        /// a[i].xyzw = b[i].x
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public void move_f3_array_into_f4_array_as_f3([NoAlias] float4* destination, [NoAlias] float4* source, int ssmd_datacount)
        {
#if STANDALONE_VSBUILD
            // you would expect memcpy to be faster but it isnt
            float* fa = (float*)(void*)destination;
            float* fb = (float*)(void*)source;

            for (int i = 0; i < ssmd_datacount; i++)
            {
                fa[0] = fb[0];   // x
                fa[1] = fb[1];   // y
                fa[2] = fb[2];   // z
                fa += 4;         // the constant index and stride help the compiler to output simd assebly
                fb += 4;
            }
#else
            UnsafeUtils.MemCpy(destination, source, ssmd_datacount * 16);
#endif 
        }



        /// <summary>
        /// a[i].xyzw = b[i].x
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public void move_f4_array_into_f4_array_as_f4([NoAlias] float4* destination, [NoAlias] float4* source, int ssmd_datacount)
        {
#if STANDALONE_VSBUILD
            // you would expect memcpy to be faster but it isnt
            float* fa = (float*)(void*)destination;
            float* fb = (float*)(void*)source;

            for (int i = 0; i < ssmd_datacount; i++)
            {
                fa[0] = fb[0];   // x
                fa[1] = fb[1];   // y
                fa[2] = fb[2];   // z
                fa[3] = fb[3];   // w
                fa += 4;         // the constant index and stride help the compiler to output simd assebly
                fb += 4;
            }
#else
            UnsafeUtils.MemCpy(destination, source, ssmd_datacount * 16);
#endif 
        }

        #endregion


        #region  move_fx_array_into_fx_array_as_fx negated

        /// <summary>
        /// a[i].xyz = - b[i].x
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public void move_f1_array_into_f4_array_as_f3_n([NoAlias] float4* destination, [NoAlias] float4* source, int ssmd_datacount)
        {
            float* fa = (float*)(void*)destination;
            float* fb = (float*)(void*)source;

            for (int i = 0; i < ssmd_datacount; i++)
            {
                float f = -fb[0];
                fa[0] = f;   // x
                fa[1] = f;   // y
                fa[2] = f;   // z
                fa += 4;         // the constant index and stride help the compiler to output simd assebly
                fb += 4;
            }
        }
        /// <summary>
        /// a[i].xyz = -b[i].x
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public void move_f1_array_into_f4_array_as_f1_n([NoAlias] float4* destination, [NoAlias] float4* source, int ssmd_datacount)
        {
            float* fa = (float*)(void*)destination;
            float* fb = (float*)(void*)source;

            for (int i = 0; i < ssmd_datacount; i++)
            {
                fa[0] = -fb[0];   // x
                fa += 4;         // the constant index and stride help the compiler to output simd assebly
                fb += 4;
            }
        }
        /// <summary>
        /// a[i].xyz = -b[i].x
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public void move_f1_array_into_f4_array_as_f2_n([NoAlias] float4* destination, [NoAlias] float4* source, int ssmd_datacount)
        {
            float* fa = (float*)(void*)destination;
            float* fb = (float*)(void*)source;

            for (int i = 0; i < ssmd_datacount; i++)
            {
                float f = -fb[0];
                fa[0] = f;   // x
                fa[1] = f;   // y
                fa += 4;  
                fb += 4;
            }
        }


        /// <summary>
        /// a[i].xyzw = - b[i].x
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public void move_f1_array_into_f4_array_as_f4_n([NoAlias] float4* destination, [NoAlias] float4* source, int ssmd_datacount)
        {
            float* fa = (float*)(void*)destination;
            float* fb = (float*)(void*)source;

            for (int i = 0; i < ssmd_datacount; i++)
            {
                float f = -fb[0];
                fa[0] = f;   // x
                fa[1] = f;   // y
                fa[2] = f;   // z
                fa[3] = f;   // z
                fa += 4;         // the constant index and stride help the compiler to output simd assebly
                fb += 4;
            }
        }


        /// <summary>
        /// a[i].xyzw = -b[i].x
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public void move_f2_array_into_f4_array_as_f2_n([NoAlias] float4* destination, [NoAlias] float4* source, int ssmd_datacount)
        {
            // you would expect memcpy to be faster but it isnt
            float* fa = (float*)(void*)destination;
            float* fb = (float*)(void*)source;

            for (int i = 0; i < ssmd_datacount; i++)
            {
                fa[0] = -fb[0];   // x
                fa[1] = -fb[1];   // y
                fa += 4;         // the constant index and stride help the compiler to output simd assebly
                fb += 4;
            }
        }


        /// <summary>
        /// a[i].xyzw = b[i].x
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public void move_f3_array_into_f4_array_as_f3_n([NoAlias] float4* destination, [NoAlias] float4* source, int ssmd_datacount)
        {
            float* fa = (float*)(void*)destination;
            float* fb = (float*)(void*)source;

            for (int i = 0; i < ssmd_datacount; i++)
            {
                fa[0] = -fb[0];   // x
                fa[1] = -fb[1];   // y
                fa[2] = -fb[2];   // z
                fa += 4;         // the constant index and stride help the compiler to output simd assebly
                fb += 4;
            }
        }



        /// <summary>
        /// a[i].xyzw = b[i].x
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public void move_f4_array_into_f4_array_as_f4_n([NoAlias] float4* destination, [NoAlias] float4* source, int ssmd_datacount)
        {
            float* fa = (float*)(void*)destination;
            float* fb = (float*)(void*)source;

            for (int i = 0; i < ssmd_datacount; i++)
            {
                fa[0] = -fb[0];   // x
                fa[1] = -fb[1];   // y
                fa[2] = -fb[2];   // z
                fa[3] = -fb[3];   // w
                fa += 4;          // the constant index and stride help the compiler to output simd assebly
                fb += 4;
            }
        }

        #endregion

    }
}
