﻿#if !STANDALONE_VSBUILD
    using Unity.Jobs;
#endif

using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;


namespace NSS.Blast.SSMD
{
    unsafe public partial struct simd
    {

        #region mul_array_fxfx
        /// <summary>
        /// a.xyzw = a.xyzw * b.x
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public void mul_array_f4f1([NoAlias] float4* a, [NoAlias] float4* b, in int ssmd_datacount)
        {
            if (IsUnrolled)
            {
                float* fa = (float*)(void*)a;
                float* fb = (float*)(void*)b;
                int i = 0;
                while (i < (ssmd_datacount & ~3))
                {
                    fa[0] = fa[0] * fb[0];
                    fa[1] = fa[1] * fb[0];
                    fa[2] = fa[2] * fb[0];
                    fa[3] = fa[3] * fb[0];

                    fa[4] = fa[4] * fb[4];
                    fa[5] = fa[5] * fb[4];
                    fa[6] = fa[6] * fb[4];
                    fa[7] = fa[7] * fb[4];

                    fa[8] = fa[8] * fb[8];
                    fa[9] = fa[9] * fb[8];
                    fa[10] = fa[10] * fb[8];
                    fa[11] = fa[11] * fb[8];

                    fa[12] = fa[12] * fb[12];
                    fa[13] = fa[13] * fb[12];
                    fa[14] = fa[14] * fb[12];
                    fa[15] = fa[15] * fb[12];

                    fa += 16;
                    fb += 16;
                    i += 4;
                }

                while (i < ssmd_datacount)
                {
                    fa[0] = fa[0] * fb[0];
                    fa[1] = fa[1] * fb[0];
                    fa[2] = fa[2] * fb[0];
                    fa[3] = fa[3] * fb[0];
                    fa += 4;
                    fb += 4;
                    i++;
                }
            }
            else
            {
                for (int i = 0; i < ssmd_datacount; i++) a[i] = a[i] * b[i].x;
            }
        }

        /// <summary>
        /// a.xyzw = a.xyzw * constant
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public void mul_array_f4f1([NoAlias] float4* a, in float constant, in int ssmd_datacount)
        {
            if (IsUnrolled)
            {
                float* fa = (float*)(void*)a;
                int i = 0;
                while (i < (ssmd_datacount & ~3))
                {
                    fa[0] = fa[0] * constant;
                    fa[1] = fa[1] * constant;
                    fa[2] = fa[2] * constant;
                    fa[3] = fa[3] * constant;

                    fa[4] = fa[4] * constant;
                    fa[5] = fa[5] * constant;
                    fa[6] = fa[6] * constant;
                    fa[7] = fa[7] * constant;

                    fa[8] = fa[8] * constant;
                    fa[9] = fa[9] * constant;
                    fa[10] = fa[10] * constant;
                    fa[11] = fa[11] * constant;

                    fa[12] = fa[12] * constant;
                    fa[13] = fa[13] * constant;
                    fa[14] = fa[14] * constant;
                    fa[15] = fa[15] * constant;

                    fa += 16;
                    i += 4;
                }
                while (i < ssmd_datacount)
                {
                    fa[0] = fa[0] * constant;
                    fa[1] = fa[1] * constant;
                    fa[2] = fa[2] * constant;
                    fa[3] = fa[3] * constant;
                    fa += 4;
                    i++;
                }
            }
            else
            {
                for (int i = 0; i < ssmd_datacount; i++) a[i] = a[i] * constant;
            }
        }

        /// <summary>
        /// a.xyzw = a.xyzw * b.xyzw
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public void mul_array_f4f4([NoAlias] float4* a, [NoAlias] float4* b, in int ssmd_datacount)
        {
            if (IsUnrolled)
            {
                float* fa = (float*)(void*)a;
                float* fb = (float*)(void*)b;
                int i = 0;

                while (i < (ssmd_datacount & ~3))
                {
                    fa[0] = fa[0] * fb[0];
                    fa[1] = fa[1] * fb[1];
                    fa[2] = fa[2] * fb[2];
                    fa[3] = fa[3] * fb[3];

                    fa[4] = fa[4] * fb[4];
                    fa[5] = fa[5] * fb[5];
                    fa[6] = fa[6] * fb[6];
                    fa[7] = fa[7] * fb[7];

                    fa[8] = fa[8] * fb[8];
                    fa[9] = fa[9] * fb[9];
                    fa[10] = fa[10] * fb[10];
                    fa[11] = fa[11] * fb[11];

                    fa[12] = fa[12] * fb[12];
                    fa[13] = fa[13] * fb[13];
                    fa[14] = fa[14] * fb[14];
                    fa[15] = fa[15] * fb[15];

                    fa += 16;
                    fb += 16;
                    i += 4;
                }

                while (i < ssmd_datacount)
                {
                    fa[0] = fa[0] * fb[0];
                    fa[1] = fa[1] * fb[0];
                    fa[2] = fa[2] * fb[0];
                    fa[3] = fa[3] * fb[0];
                    fa += 4;
                    fb += 4;
                    i++;
                }
            }
            else
            {
                for (int i = 0; i < ssmd_datacount; i++) a[i] = a[i] * b[i];
            }
        }

        /// <summary>
        /// a.xyz = a.xyz * b.x
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public void mul_array_f3f1([NoAlias] float4* a, [NoAlias] float4* b, in int ssmd_datacount)
        {
            if (IsUnrolled)
            {
                float* fa = (float*)(void*)a;
                float* fb = (float*)(void*)b;
                int i = 0;

                while (i < (ssmd_datacount & ~3))
                {
                    fa[0] = fa[0] * fb[0];
                    fa[1] = fa[1] * fb[0];
                    fa[2] = fa[2] * fb[0];

                    fa[4] = fa[4] * fb[4];
                    fa[5] = fa[5] * fb[4];
                    fa[6] = fa[6] * fb[4];

                    fa[8] = fa[8] * fb[8];
                    fa[9] = fa[9] * fb[8];
                    fa[10] = fa[10] * fb[8];

                    fa[12] = fa[12] * fb[12];
                    fa[13] = fa[13] * fb[12];
                    fa[14] = fa[14] * fb[12];

                    fa += 16;
                    fb += 16;
                    i += 4;
                }

                while (i < ssmd_datacount)
                {
                    fa[0] = fa[0] * fb[0];
                    fa[1] = fa[1] * fb[0];
                    fa[2] = fa[2] * fb[0];
                    fa += 4;
                    fb += 4;
                    i++;
                }
            }
            else
            {
                for (int i = 0; i < ssmd_datacount; i++) a[i].xyz = a[i].xyz * b[i].x;
            }
        }

        /// <summary>
        /// a.xyzw = a.xyzw * constant
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public void mul_array_f3f1([NoAlias] float4* a, in float constant, in int ssmd_datacount)
        {
            if (IsUnrolled)
            {
                float* fa = (float*)(void*)a;
                int i = 0;
                while (i < (ssmd_datacount & ~3))
                {
                    fa[0] = fa[0] * constant;
                    fa[1] = fa[1] * constant;
                    fa[2] = fa[2] * constant;

                    fa[4] = fa[4] * constant;
                    fa[5] = fa[5] * constant;
                    fa[6] = fa[6] * constant;

                    fa[8] = fa[8] * constant;
                    fa[9] = fa[9] * constant;
                    fa[10] = fa[10] * constant;

                    fa[12] = fa[12] * constant;
                    fa[13] = fa[13] * constant;
                    fa[14] = fa[14] * constant;

                    fa += 16;
                    i += 4;
                }
                while (i < ssmd_datacount)
                {
                    fa[0] = fa[0] * constant;
                    fa[1] = fa[1] * constant;
                    fa[2] = fa[2] * constant;
                    fa += 4;
                    i++;
                }
            }
            else
            {
                // with unity in burst this compiles nicely, in vs.net is does not
                for (int i = 0; i < ssmd_datacount; i++) a[i] = a[i] * constant;
            }
        }

        /// <summary>
        /// a.xyz = a.xyz * b.xyz (in float4 arrays)
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public void mul_array_f3f3([NoAlias] float4* a, [NoAlias] float4* b, in int ssmd_datacount)
        {
            if (IsUnrolled)
            {
                float* fa = (float*)(void*)a;
                float* fb = (float*)(void*)b;
                int i = 0;
                while (i < (ssmd_datacount & ~3))
                {
                    fa[0] = fa[0] * fb[0];
                    fa[1] = fa[1] * fb[1];
                    fa[2] = fa[2] * fb[2];

                    fa[4] = fa[4] * fb[4];
                    fa[5] = fa[5] * fb[5];
                    fa[6] = fa[6] * fb[6];

                    fa[8] = fa[8] * fb[8];
                    fa[9] = fa[9] * fb[9];
                    fa[10] = fa[10] * fb[10];

                    fa[12] = fa[12] * fb[12];
                    fa[13] = fa[13] * fb[13];
                    fa[14] = fa[14] * fb[14];

                    fa += 16;
                    fb += 16;
                    i += 4;
                }
                while (i < ssmd_datacount)
                {
                    fa[0] = fa[0] * fb[0];
                    fa[1] = fa[1] * fb[1];
                    fa[2] = fa[2] * fb[2];
                    fa += 4;
                    fb += 4;
                    i++;
                }
            }
            else
            {
                for (int i = 0; i < ssmd_datacount; i++) a[i].xyz = a[i].xyz * b[i].xyz;
            }
        }

        /// <summary>
        /// a.xy = a.xy * b.x (in float4 arrays) 
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public void mul_array_f2f1([NoAlias] float4* a, [NoAlias] float4* b, in int ssmd_datacount)
        {
            if (IsUnrolled)
            {
                float* fa = (float*)(void*)a;
                float* fb = (float*)(void*)b;
                int i = 0;

                while (i < (ssmd_datacount & ~3))
                {
                    fa[0] = fa[0] * fb[0];
                    fa[1] = fa[1] * fb[0];

                    fa[4] = fa[4] * fb[4];
                    fa[5] = fa[5] * fb[4];

                    fa[8] = fa[8] * fb[8];
                    fa[9] = fa[9] * fb[8];

                    fa[12] = fa[12] * fb[12];
                    fa[13] = fa[13] * fb[12];

                    fa += 16;
                    fb += 16;
                    i += 4;
                }
                while (i < ssmd_datacount)
                {
                    fa[0] = fa[0] * fb[0];
                    fa[1] = fa[1] * fb[0];
                    fa += 4;
                    fb += 4;
                    i++;
                }
            }
            else
            {
                for (int i = 0; i < ssmd_datacount; i++) a[i].xy = a[i].xy * b[i].x;
            }
        }

        /// <summary>
        /// a.xy = a.xy * constant (in float4)
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public void mul_array_f2f1([NoAlias] float4* a, in float constant, in int ssmd_datacount)
        {
            if (IsUnrolled)
            {
                float* fa = (float*)(void*)a;
                int i = 0;

                while (i < (ssmd_datacount & ~3))
                {
                    fa[0] = fa[0] * constant;
                    fa[1] = fa[1] * constant;

                    fa[4] = fa[4] * constant;
                    fa[5] = fa[5] * constant;

                    fa[8] = fa[8] * constant;
                    fa[9] = fa[9] * constant;

                    fa[12] = fa[12] * constant;
                    fa[13] = fa[13] * constant;

                    fa += 16;
                    i += 4;
                }
                while (i < ssmd_datacount)
                {
                    fa[0] = fa[0] * constant;
                    fa[1] = fa[1] * constant;
                    fa += 4;
                    i++;
                }
            }
            else
            {
                for (int i = 0; i < ssmd_datacount; i++) a[i].xy = a[i].xy * constant;
            }
        }

        /// <summary>
        /// a.xy = a.xy * b.xy (in float4 arrays)
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public void mul_array_f2f2([NoAlias] float4* a, [NoAlias] float4* b, in int ssmd_datacount)
        {
            if (IsUnrolled)
            {
                float* fa = (float*)(void*)a;
                float* fb = (float*)(void*)b;
                int i = 0;

                while (i < (ssmd_datacount & ~3))
                {
                    fa[0] = fa[0] * fb[0];
                    fa[1] = fa[1] * fb[1];

                    fa[4] = fa[4] * fb[4];
                    fa[5] = fa[5] * fb[5];

                    fa[8] = fa[8] * fb[8];
                    fa[9] = fa[9] * fb[9];

                    fa[12] = fa[12] * fb[12];
                    fa[13] = fa[13] * fb[13];

                    fa += 16;
                    fb += 16;
                    i += 4;
                }
                while (i < ssmd_datacount)
                {
                    fa[0] = fa[0] * fb[0];
                    fa[1] = fa[1] * fb[1];
                    fa += 4;
                    fb += 4;
                    i++;
                }
            }
            else
            {
                for (int i = 0; i < ssmd_datacount; i++) a[i].xy = a[i].xy * b[i].xy;
            }
        }

        /// <summary>
        /// a.xy = a.x * b.xy (in float4 arrays)
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public void mul_array_f1f2([NoAlias] float4* a, [NoAlias] float4* b, in int ssmd_datacount)
        {
            if (IsUnrolled)
            {
                float* fa = (float*)(void*)a;
                float* fb = (float*)(void*)b;
                int i = 0;

                while (i < (ssmd_datacount & ~3))
                {
                    fa[0] = fa[0] * fb[0];
                    fa[1] = fa[0] * fb[1];

                    fa[4] = fa[4] * fb[4];
                    fa[5] = fa[4] * fb[5];

                    fa[8] = fa[8] * fb[8];
                    fa[9] = fa[8] * fb[9];

                    fa[12] = fa[12] * fb[12];
                    fa[13] = fa[12] * fb[13];

                    fa += 16;
                    fb += 16;
                    i += 4;
                }
                while (i < ssmd_datacount)
                {
                    fa[0] = fa[0] * fb[0];
                    fa[1] = fa[0] * fb[1];
                    fa += 4;
                    fb += 4;
                    i++;
                }
            }
            else
            {
                for (int i = 0; i < ssmd_datacount; i++) a[i].xy = a[i].x * b[i].xy;
            }
        }

        /// <summary>
        /// a.xyz = a.x * b.xyz (in float4 arrays)
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public void mul_array_f1f3([NoAlias] float4* a, [NoAlias] float4* b, in int ssmd_datacount)
        {
            if (IsUnrolled)
            {
                float* fa = (float*)(void*)a;
                float* fb = (float*)(void*)b;
                int i = 0;

                while (i < (ssmd_datacount & ~3))
                {
                    fa[0] = fa[0] * fb[0];
                    fa[1] = fa[0] * fb[1];
                    fa[2] = fa[0] * fb[2];

                    fa[4] = fa[4] * fb[4];
                    fa[5] = fa[4] * fb[5];
                    fa[6] = fa[4] * fb[6];

                    fa[8] = fa[8] * fb[8];
                    fa[9] = fa[8] * fb[9];
                    fa[10] = fa[8] * fb[10];

                    fa[12] = fa[12] * fb[12];
                    fa[13] = fa[12] * fb[13];
                    fa[14] = fa[12] * fb[14];

                    fa += 16;
                    fb += 16;
                    i += 4;
                }
                while (i < ssmd_datacount)
                {
                    fa[0] = fa[0] * fb[0];
                    fa[1] = fa[0] * fb[1];
                    fa[2] = fa[0] * fb[2];
                    fa += 4;
                    fb += 4;
                    i++;
                }
            }
            else
            {
                for (int i = 0; i < ssmd_datacount; i++) a[i].xyz = a[i].x * b[i].xyz;
            }
        }

        /// <summary>
        /// a.xyz = a.x * b.xyz (in float4 arrays)
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public void mul_array_f1f4([NoAlias] float4* a, [NoAlias] float4* b, in int ssmd_datacount)
        {
            if (IsUnrolled)
            {
                float* fa = (float*)(void*)a;
                float* fb = (float*)(void*)b;
                int i = 0;

                while (i < (ssmd_datacount & ~3))
                {
                    fa[0] = fa[0] * fb[0];
                    fa[1] = fa[0] * fb[1];
                    fa[2] = fa[0] * fb[2];
                    fa[3] = fa[0] * fb[3];

                    fa[4] = fa[4] * fb[4];
                    fa[5] = fa[4] * fb[5];
                    fa[6] = fa[4] * fb[6];
                    fa[7] = fa[4] * fb[7];

                    fa[8] = fa[8] * fb[8];
                    fa[9] = fa[8] * fb[9];
                    fa[10] = fa[8] * fb[10];
                    fa[11] = fa[8] * fb[11];

                    fa[12] = fa[12] * fb[12];
                    fa[13] = fa[12] * fb[13];
                    fa[14] = fa[12] * fb[14];
                    fa[15] = fa[12] * fb[15];

                    fa += 16;
                    fb += 16;
                    i += 4;
                }
                while (i < ssmd_datacount)
                {
                    fa[0] = fa[0] * fb[0];
                    fa[1] = fa[0] * fb[1];
                    fa[2] = fa[0] * fb[2];
                    fa[3] = fa[0] * fb[3];
                    fa += 4;
                    fb += 4;
                    i++;
                }
            }
            else
            {
                for (int i = 0; i < ssmd_datacount; i++) a[i].xyz = a[i].x * b[i].xyz;
            }
        }

        /// <summary>
        /// a.x = a.x * b.x (in float4 arrays) 
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public void mul_array_f1f1([NoAlias] float4* a, [NoAlias] float4* b, in int ssmd_datacount)
        {
            if (IsUnrolled)
            {
                float* fa = (float*)(void*)a;
                float* fb = (float*)(void*)b;
                int i = 0;

                while (i < (ssmd_datacount & ~7))
                {
                    // the burst compiler still is faster then the intrinsics below.. 
                    // i wonder in .net TODO 
#if false && !STANDALONE_VSBUILD
                if (IsAvx2Supported)
                {
                    // Code path for AVX2 instructions
                    // note bug parameters are reversed !!
                    v256 va = new v256(fa[0], fa[4], fa[8], fa[12], fa[16], fa[20], fa[24], fa[28]);
                    v256 vb = new v256(fb[0], fb[4], fb[8], fb[12], fb[16], fb[20], fb[24], fb[28]);

                    va = Avx2.mm256_mul_epi32(va, vb);
                    fa[0] = va.Float0;
                    fa[4] = va.Float1;
                    fa[8] = va.Float2;
                    fa[12] = va.Float3;
                    fa[16] = va.Float4;
                    fa[20] = va.Float5;
                    fa[24] = va.Float6;
                    fa[28] = va.Float7;
                }
                else
                if (IsSse42Supported)
                {
                    v128 va = new v128(fa[0], fa[4], fa[8], fa[12]);
                    v128 vb = new v128(fb[0], fb[4], fb[8], fb[12]);
                    Sse4_1.mul_epi32(va, vb);
                    fa[0] = va.Float0;
                    fa[4] = va.Float1;
                    fa[8] = va.Float2;
                    fa[12] = va.Float3;

                    va = new v128(fa[16], fa[20], fa[24], fa[28]);
                    vb = new v128(fb[16], fb[20], fb[24], fb[28]);
                    Sse4_1.mul_epi32(va, vb);
                    fa[16] = va.Float0;
                    fa[20] = va.Float1;
                    fa[24] = va.Float2;
                    fa[28] = va.Float3;
                }
                else
#endif
                    {
                        // fallback, let compiler handle others 
                        fa[0] = fa[0] * fb[0];
                        fa[4] = fa[4] * fb[4];
                        fa[8] = fa[8] * fb[8];
                        fa[12] = fa[12] * fb[12];
                        fa[16] = fa[16] * fb[16];
                        fa[20] = fa[20] * fb[20];
                        fa[24] = fa[24] * fb[24];
                        fa[28] = fa[28] * fb[28];
                    }
                    fa += 32;
                    fb += 32;
                    i += 8;
                }
                while (i < (ssmd_datacount & ~3))
                {
                    fa[0] = fa[0] * fb[0];
                    fa[4] = fa[4] * fb[4];
                    fa[8] = fa[8] * fb[8];
                    fa[12] = fa[12] * fb[12];

                    fa += 16;
                    fb += 16;
                    i += 4;
                }
                while (i < ssmd_datacount)
                {
                    fa[0] = fa[0] * fb[0];
                    fa += 4;
                    fb += 4;
                    i++;
                }
            }
            else
            {
                for (int i = 0; i < ssmd_datacount; i++) a[i].x = a[i].x * b[i].x;
            }
        }

        /// <summary>
        /// a.x = a.x * constant (in float4)
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public void mul_array_f1f1([NoAlias] float4* a, in float constant, in int ssmd_datacount)
        {
            if (IsUnrolled)
            {
                float* fa = (float*)(void*)a;
                int i = 0;

                while (i < (ssmd_datacount & ~7))
                {
                    fa[0] = fa[0] * constant;
                    fa[4] = fa[4] * constant;
                    fa[8] = fa[8] * constant;
                    fa[12] = fa[12] * constant;
                    fa[16] = fa[16] * constant;
                    fa[20] = fa[20] * constant;
                    fa[24] = fa[24] * constant;
                    fa[28] = fa[28] * constant;
                    /*fa[32] = fa[32] * constant;
                    fa[36] = fa[36] * constant;
                    fa[40] = fa[40] * constant;
                    fa[44] = fa[44] * constant;
                    fa[48] = fa[48] * constant;
                    fa[52] = fa[52] * constant;
                    fa[56] = fa[56] * constant;
                    fa[60] = fa[60] * constant;*/

                    fa += 32;
                    i += 8;
                }
                while (i < (ssmd_datacount & ~3))
                {
                    fa[0] = fa[0] * constant;
                    fa[4] = fa[4] * constant;
                    fa[8] = fa[8] * constant;
                    fa[12] = fa[12] * constant;

                    fa += 16;
                    i += 4;
                }
                while (i < ssmd_datacount)
                {
                    fa[0] = fa[0] * constant;
                    fa += 4;
                    i++;
                }
            }
            else
            {
                for (int i = 0; i < ssmd_datacount; i++) a[i].x = a[i].x * constant;
            }
        }

        #endregion

        #region mul_array_fxfx_indexed 

        /// <summary>
        /// vn[].x = v1[] * v1_index[][]  where n = t/4
        /// </summary>
        static public void mul_array_f1f1_indexed<T>([NoAlias] T* fn_out, [NoAlias] float* f1_in, [NoAlias] float** indexed_data, in int data_offset, in bool is_aligned, in int data_rowsize, in int ssmd_datacount) where T : unmanaged
        {
            float* p_out = (float*)(void*)fn_out;
            float* p_in = f1_in;

            int out_stride = sizeof(T) >> 2; // this should compile as constant, giving constant stride for eacht T -> better vectorization by native compilers

            if (is_aligned)
            {
                float* p_data = &indexed_data[0][data_offset];
                int data_stride = data_rowsize >> 2;

                int i = 0;
                while (i < (ssmd_datacount & ~7))
                {
                    p_out[0] = p_in[0] * p_data[0];
                    p_out[out_stride] = p_in[1] * p_data[data_stride];
                    p_out[out_stride + out_stride] = p_in[2] * p_data[data_stride + data_stride];
                    p_out[out_stride + out_stride + out_stride] = p_in[3] * p_data[data_stride + data_stride + data_stride];

                    p_out += out_stride * 4;
                    p_data += data_stride * 4;

                    p_out[0] = p_in[4] * p_data[0];
                    p_out[out_stride] = p_in[5] * p_data[data_stride];
                    p_out[out_stride + out_stride] = p_in[6] * p_data[data_stride + data_stride];
                    p_out[out_stride + out_stride + out_stride] = p_in[7] * p_data[data_stride + data_stride + data_stride];

                    p_out += out_stride * 4;
                    p_data += data_stride * 4;
                    p_in += 8;
                    i += 8;
                }
                while (i < ssmd_datacount)
                {
                    p_out[0] = p_in[0] * p_data[0];

                    p_out += out_stride;
                    p_in += 1;
                    p_data += data_stride;

                    i++;
                }
            }
            else
            {
                for (int i = 0; i < ssmd_datacount; i++)
                {
                    // when not alligned we have to calculate the the offset on each data word
                    p_out[0] = p_in[0] * indexed_data[i][data_offset];
                    p_out += out_stride;
                    p_in += 1;
                }
            }
        }

        /// <summary>
        ///  f2_out[i] = f2_in[i] * ((float2*)(void*)&((float*)data[i])[offset])[0];
        ///  or
        ///  f4_out[i].xy = f2_in[i] * ((float2*)(void*)&((float*)data[i])[offset])[0];
        ///  
        /// depending on type of T
        /// </summary>
        static public void mul_array_f2f2_indexed<T>([NoAlias] T* fn_out, [NoAlias] float2* f2_in, [NoAlias] float** indexed_data, in int data_offset, in bool is_aligned, in int data_rowsize, in int ssmd_datacount) where T : unmanaged
        {
            float* p_out = (float*)(void*)fn_out;
            float* p_in = (float*)(void*)f2_in;

            int out_stride = sizeof(T) >> 2; // this should compile as constant, giving constant stride for eacht T -> better vectorization by native compilers

            if (is_aligned)
            {
                float* p_data = &indexed_data[0][data_offset];
                int data_stride = data_rowsize >> 2;

                if (IsUnrolled)
                {
                    int i = 0;
                    while (i < (ssmd_datacount & ~3))
                    {
                        p_out[0] = p_in[0] * p_data[0];
                        p_out[1] = p_in[1] * p_data[1];

                        p_out[out_stride + 0] = p_in[3] * p_data[data_stride + 0];
                        p_out[out_stride + 1] = p_in[4] * p_data[data_stride + 1];

                        p_out[out_stride + 2] = p_in[3] * p_data[data_stride + 2];
                        p_out[out_stride + 3] = p_in[4] * p_data[data_stride + 3];

                        p_out[out_stride + 4] = p_in[3] * p_data[data_stride + 4];
                        p_out[out_stride + 5] = p_in[4] * p_data[data_stride + 5];

                        p_out += out_stride << 2;
                        p_in += 8;
                        p_data += data_stride << 2;
                        i += 4;
                    }
                    while (i < ssmd_datacount)
                    {
                        p_out[0] = p_in[0] * p_data[0];
                        p_out[1] = p_in[1] * p_data[1];

                        p_out += out_stride;
                        p_in += 2;
                        p_data += data_stride;
                        i++;
                    }
                }
                else
                {
                    for (int i = 0; i < ssmd_datacount; i++)
                    {
                        p_out[0] = p_in[0] * p_data[0];
                        p_out[1] = p_in[1] * p_data[1];

                        p_out += out_stride;
                        p_in += 2;
                        p_data += data_stride;
                    }
                }
            }
            else
            {
                for (int i = 0; i < ssmd_datacount; i++)
                {
                    // when not alligned we have to calculate the offset on each data word
                    p_out[0] = p_in[0] * indexed_data[i][data_offset];
                    p_out[1] = p_in[1] * indexed_data[i][data_offset + 1];
                    p_out += out_stride;
                    p_in += 2;
                }
            }
        }

        // 
        /// <summary>
        /// f3_out[i + 0] = f3_in[i + 0] * ((float3*)(void*)&((float*) data[i])[offset])[0]; 
        /// or
        /// f4_out[i + 0].xyz = f3_in[i + 0] * ((float3*)(void*)&((float*) data[i])[offset])[0]; 
        /// </summary>        
        static public void mul_array_f3f3_indexed<T>([NoAlias] T* fn_out, [NoAlias] float3* f3_in, [NoAlias] float** indexed_data, in int data_offset, in bool is_aligned, in int data_rowsize, in int ssmd_datacount) where T : unmanaged
        {
            float* p_out = (float*)(void*)fn_out;
            float* p_in = (float*)(void*)f3_in;

            int out_stride = sizeof(T) >> 2; // this should compile as constant, giving constant stride for eacht T -> better vectorization by native compilers

            if (is_aligned)
            {
                float* p_data = &indexed_data[0][data_offset];
                int data_stride = data_rowsize >> 2;

                for (int i = 0; i < ssmd_datacount; i++)
                {
                    p_out[0] = p_in[0] * p_data[0];
                    p_out[1] = p_in[1] * p_data[1];
                    p_out[2] = p_in[2] * p_data[2];

                    p_out += out_stride;
                    p_in += 3;
                    p_data += data_stride;
                }
            }
            else
            {
                for (int i = 0; i < ssmd_datacount; i++)
                {
                    // when not alligned we have to calculate the offset on each data word
                    p_out[0] = p_in[0] * indexed_data[i][data_offset];
                    p_out[1] = p_in[1] * indexed_data[i][data_offset + 1];
                    p_out[2] = p_in[2] * indexed_data[i][data_offset + 2];
                    p_out += out_stride;
                    p_in += 3;
                }
            }
        }

        /// <summary>
        /// for (int i = 0; i<ssmd_datacount; i++) f4_out[i] = f4_in[i] * ((float4*)(void*)&((float*) data[i])[offset])[0];
        /// </summary>
        /// <typeparam name="T"></typeparam>
        static public void mul_array_f4f4_indexed([NoAlias] float4* f4_out, [NoAlias] float4* f4_in, [NoAlias] float** indexed_data, in int data_offset, in bool is_aligned, in int data_rowsize, in int ssmd_datacount)
        {
            float* p_out = (float*)(void*)f4_out;
            float* p_in = (float*)(void*)f4_in;

            if (is_aligned)
            {
                float* p_data = &indexed_data[0][data_offset];
                int data_stride = data_rowsize >> 2;

                for (int i = 0; i < ssmd_datacount; i++)
                {
                    p_out[0] = p_in[0] * p_data[0];
                    p_out[1] = p_in[1] * p_data[1];
                    p_out[2] = p_in[2] * p_data[2];
                    p_out[3] = p_in[3] * p_data[3];
                    p_out += 4;
                    p_in += 4;
                    p_data += data_stride;
                }
            }
            else
            {
                for (int i = 0; i < ssmd_datacount; i++)
                {
                    // when not alligned we have to calculate the offset on each data word
                    p_out[0] = p_in[0] * indexed_data[i][data_offset];
                    p_out[1] = p_in[1] * indexed_data[i][data_offset + 1];
                    p_out[2] = p_in[2] * indexed_data[i][data_offset + 2];
                    p_out[3] = p_in[3] * indexed_data[i][data_offset + 3];
                    p_out += 4;
                    p_in += 4;
                }
            }
        }

        #endregion

        #region mul_array_fxfx_constant

        /// <summary>
        /// f4_out[i] = f4_in[i] * constant
        /// </summary>
        static public void mul_array_f4f4_constant([NoAlias] float4* f4_out, [NoAlias] float4* f4_in, float constant, in int ssmd_datacount)
        {
            if (IsUnrolled)
            {
                float* p_out = (float*)(void*)f4_out;
                float* p_in = (float*)(void*)f4_in;

                int i = 0;
                while (i < (ssmd_datacount & ~3))
                {
                    p_out[0] = p_in[0] * constant;
                    p_out[1] = p_in[1] * constant;
                    p_out[2] = p_in[2] * constant;
                    p_out[3] = p_in[3] * constant;

                    p_out[4] = p_in[4] * constant;
                    p_out[5] = p_in[5] * constant;
                    p_out[6] = p_in[6] * constant;
                    p_out[7] = p_in[7] * constant;

                    p_out[8] = p_in[8] * constant;
                    p_out[9] = p_in[9] * constant;
                    p_out[10] = p_in[10] * constant;
                    p_out[11] = p_in[11] * constant;

                    p_out[12] = p_in[12] * constant;
                    p_out[13] = p_in[13] * constant;
                    p_out[14] = p_in[14] * constant;
                    p_out[15] = p_in[15] * constant;

                    p_out += 16;
                    p_in += 16;
                    i += 4;
                }
                while (i < ssmd_datacount)
                {
                    p_out[0] = p_in[0] * constant;
                    p_out[1] = p_in[1] * constant;
                    p_out[2] = p_in[2] * constant;
                    p_out[3] = p_in[3] * constant;

                    p_out += 4;
                    p_in += 4;
                    i += 1;
                }
            }
            else
            {
                for (int i = 0; i < ssmd_datacount; i++) f4_out[i] = f4_in[i] * constant;
            }
        }

        /// <summary>
        /// f4_out[i].x = f1_in[i] * constant
        /// </summary>
        static public void mul_array_f14f1_constant([NoAlias] float4* f4_out, [NoAlias] float* f1_in, float constant, in int ssmd_datacount)
        {
            if (IsUnrolled)
            {
                float* p_out = (float*)(void*)f4_out;
                float* p_in = (float*)(void*)f1_in;

                int i = 0;
                while (i < (ssmd_datacount & ~7))
                {
                    p_out[0] = p_in[0] * constant;
                    p_out[4] = p_in[1] * constant;
                    p_out[8] = p_in[2] * constant;
                    p_out[12] = p_in[3] * constant;
                    p_out[16] = p_in[4] * constant;
                    p_out[20] = p_in[5] * constant;
                    p_out[24] = p_in[6] * constant;
                    p_out[28] = p_in[7] * constant;
                    p_out += 32;
                    p_in += 8;
                    i += 8;
                }
                while (i < (ssmd_datacount & ~3))
                {
                    p_out[0] = p_in[0] * constant;
                    p_out[4] = p_in[1] * constant;
                    p_out[8] = p_in[2] * constant;
                    p_out[12] = p_in[3] * constant;
                    p_out += 16;
                    p_in += 4;
                    i += 4;
                }
                while (i < ssmd_datacount)
                {
                    p_out[0] = p_in[0] * constant;
                    p_out += 4;
                    p_in += 1;
                    i++;
                }
            }
            else
            {
                for (int i = 0; i < ssmd_datacount; i++) f4_out[i].x = f1_in[i] * constant;
            }
        }

        /// <summary>
        /// f3_out[i] = f3_in[i] * constant
        /// </summary>
        static public void mul_array_f3f3_constant([NoAlias] float3* f3_out, [NoAlias] float3* f3_in, float constant, in int ssmd_datacount)
        {
            if (constant == 0)
            {
                UnsafeUtils.MemClear(f3_out, 12 * ssmd_datacount);
            }
            if (IsUnrolled)
            {
                float* p_out = (float*)(void*)f3_out;
                float* p_in = (float*)(void*)f3_in;

                for (int i = 0; i < ssmd_datacount; i++)
                {
                    p_out[0] = p_in[0] * constant;
                    p_out[1] = p_in[1] * constant;
                    p_out[2] = p_in[2] * constant;
                    p_out += 3;
                    p_in += 3;
                }
            }
            else
            {
                for (int i = 0; i < ssmd_datacount; i++) f3_out[i] = f3_in[i] * constant;
            }
        }

        /// <summary>
        ///  f4_out[i].xyz = f3_in[i] * constant
        /// </summary>
        static public void mul_array_f3f3_constant([NoAlias] float4* f4_out, [NoAlias] float3* f3_in, float constant, in int ssmd_datacount)
        {
            if (IsUnrolled)
            {
                float* p_out = (float*)(void*)f4_out;
                float* p_in = (float*)(void*)f3_in;

                for (int i = 0; i < ssmd_datacount; i++)
                {
                    p_out[0] = p_in[0] * constant;
                    p_out[1] = p_in[1] * constant;
                    p_out[2] = p_in[2] * constant;
                    p_out += 4;
                    p_in += 3;
                }
            }
            else
            {
                for (int i = 0; i < ssmd_datacount; i++) f4_out[i].xyz = f3_in[i] * constant;
            }
        }

        /// <summary>
        ///f2_out[i] = f2_in[i] * constant
        /// </summary>
        static public void mul_array_f2f2_constant([NoAlias] float2* f2_out, [NoAlias] float2* f2_in, float constant, in int ssmd_datacount)
        {
            if (IsUnrolled)
            {
                float* p_out = (float*)(void*)f2_out;
                float* p_in = (float*)(void*)f2_in;

                int i = 0;
                while (i < (ssmd_datacount & ~3))
                {
                    p_out[0] = p_in[0] * constant;
                    p_out[1] = p_in[1] * constant;

                    p_out[2] = p_in[2] * constant;
                    p_out[3] = p_in[3] * constant;

                    p_out[4] = p_in[4] * constant;
                    p_out[5] = p_in[5] * constant;

                    p_out[6] = p_in[6] * constant;
                    p_out[7] = p_in[7] * constant;

                    p_out += 8;
                    p_in += 8;
                    i += 4;
                }
                while (i < ssmd_datacount)
                {
                    p_out[0] = p_in[0] * constant;
                    p_out[1] = p_in[1] * constant;
                    p_out += 2;
                    p_in += 2;
                    i++;
                }
            }
            else
            {
                for (int i = 0; i < ssmd_datacount; i++) f2_out[i] = f2_in[i] * constant;
            }
        }
      
        /// <summary>
        /// f4_out[i].xy = f2_in[i].xy * constant
        /// </summary>
        static public void mul_array_f2f2_constant([NoAlias] float4* f4_out, [NoAlias] float2* f2_in, float constant, in int ssmd_datacount)
        {
            if (IsUnrolled)
            {
                float* p_out = (float*)(void*)f4_out;
                float* p_in = (float*)(void*)f2_in;

                int i = 0;
                while (i < (ssmd_datacount & ~3))
                {
                    p_out[0] = p_in[0] * constant;
                    p_out[1] = p_in[1] * constant;

                    p_out[4] = p_in[2] * constant;
                    p_out[5] = p_in[3] * constant;

                    p_out[8] = p_in[4] * constant;
                    p_out[9] = p_in[5] * constant;

                    p_out[12] = p_in[6] * constant;
                    p_out[13] = p_in[7] * constant;

                    p_out += 16;
                    p_in += 8;
                    i += 4;
                }
                while (i < ssmd_datacount)
                {
                    p_out[0] = p_in[0] * constant;
                    p_out[1] = p_in[1] * constant;
                    p_out += 4;
                    p_in += 2;
                    i++;
                }
            }
            else
            {
                for (int i = 0; i < ssmd_datacount; i++) f4_out[i].xy = f2_in[i] * constant;
            }
        }
        
        /// <summary>
        /// f1_out[i] = f1_in[i] * constant
        /// </summary>
        static public void mul_array_f1f1_constant([NoAlias] float* f1_out, [NoAlias] float* f1_in, float constant, in int ssmd_datacount)
        {
            if (IsUnrolled)
            {
                float* p_out = f1_out;
                float* p_in = f1_in;
                int i = 0;
                while (i < (ssmd_datacount & ~7))
                {
                    p_out[0] = p_in[0] * constant;
                    p_out[1] = p_in[1] * constant;
                    p_out[2] = p_in[2] * constant;
                    p_out[3] = p_in[3] * constant;
                    p_out[4] = p_in[4] * constant;
                    p_out[5] = p_in[5] * constant;
                    p_out[6] = p_in[6] * constant;
                    p_out[7] = p_in[7] * constant;
                    p_out += 8;

                    p_in += 8;
                    i += 8;
                }
                while (i < (ssmd_datacount & ~3))
                {
                    p_out[0] = p_in[0] * constant;
                    p_out[1] = p_in[1] * constant;
                    p_out[2] = p_in[2] * constant;
                    p_out[3] = p_in[3] * constant;
                    p_out += 4;
                    p_in += 4;
                    i += 4;
                }
                while (i < ssmd_datacount)
                {
                    p_out[0] = p_in[0] * constant;
                    p_out += 1;
                    p_in += 1;
                    i++;
                }
            }
            else
            {
                for (int i = 0; i < ssmd_datacount; i++) f1_out[i] = f1_in[i] * constant;
            }
        }

        #endregion

    }



    /// <summary>
    /// we check the pattern of operation once (for mul)
    /// then copy that into add/sub/div
    /// </summary>

    namespace vectorization_tests
    {
        [BurstCompile]
        unsafe public struct test_mul_array_f4f4 : IJob
        {
            public int datacount;
            public int repeatcount;

            public void Execute()
            {
                float4* a = stackalloc float4[datacount];
                float4* b = stackalloc float4[datacount];

                for (int i = 0; i < repeatcount; i++)
                {
                    simd.mul_array_f4f4(a, b, datacount);
                }
            }
        }

        [BurstCompile]
        unsafe public struct test_mul_array_f4f4_constant : IJob
        {
            public int datacount;
            public int repeatcount;

            public void Execute()
            {
                float4* a = stackalloc float4[datacount];
                float4* b = stackalloc float4[datacount];

                for (int i = 0; i < repeatcount; i++)
                {
                    simd.mul_array_f4f4_constant(a, b, 3f, datacount);
                }
            }
        }

        [BurstCompile]
        unsafe public struct test_mul_array_f4f1 : IJob
        {
            public int datacount;
            public int repeatcount;

            public void Execute()
            {
                float4* a = stackalloc float4[datacount];
                float4* b = stackalloc float4[datacount];

                for (int i = 0; i < repeatcount; i++)
                {
                    simd.mul_array_f4f1(a, b, datacount);
                }
            }
        }

        [BurstCompile]
        unsafe public struct test_mul_array_f4f1_constant : IJob
        {
            public int datacount;
            public int repeatcount;

            public void Execute()
            {
                float4* a = stackalloc float4[datacount];
                float4* b = stackalloc float4[datacount];

                for (int i = 0; i < repeatcount; i++)
                {
                    simd.mul_array_f4f1(a, 3f, datacount);
                }
            }
        }


        [BurstCompile]
        unsafe public struct test_mul_array_f1f1 : IJob
        {
            public int datacount;
            public int repeatcount;

            public void Execute()
            {
                float4* a = stackalloc float4[datacount];
                float4* b = stackalloc float4[datacount];

                for (int i = 0; i < repeatcount; i++)
                {
                    simd.mul_array_f1f1(a, b, datacount);
                }
            }
        }

        [BurstCompile]
        unsafe public struct test_mul_array_f1f1_constant : IJob
        {
            public int datacount;
            public int repeatcount;

            public void Execute()
            {
                float4* a = stackalloc float4[datacount];

                for (int i = 0; i < repeatcount; i++)
                {
                    simd.mul_array_f1f1(a, 3f, datacount);
                }
            }
        }

        /// <summary>
        /// vn[].x = v1[] * v1_index[][]
        /// </summary>
        [BurstCompile]
        unsafe public struct test_mul_array_f1f1_indexed : IJob
        {
            public int datacount;
            public int repeatcount;

            public void Execute()
            {
                float* a = stackalloc float[datacount];
                float* b = stackalloc float[datacount];

                float* data = stackalloc float[datacount * 4];
                float** indexed = stackalloc float*[datacount];

                for (int i = 0; i < datacount; i++) indexed[i] = &data[i * 4];

                for (int i = 0; i < repeatcount; i++)
                {
                    simd.mul_array_f1f1_indexed<float>(b, a, indexed, 2, true, 4, datacount);
                }
            }
        }

        /// <summary>
        /// vn[].x = v1[] * v1_index[][]
        /// </summary>
        [BurstCompile]
        unsafe public struct test_mul_array_f1f1_indexed_unaligned : IJob
        {
            public int datacount;
            public int repeatcount;

            public void Execute()
            {
                float* a = stackalloc float[datacount];
                float* b = stackalloc float[datacount];

                float* data = stackalloc float[datacount * 4];
                float** indexed = stackalloc float*[datacount];

                for (int i = 0; i < datacount; i++) indexed[i] = &data[i * 4];

                for (int i = 0; i < repeatcount; i++)
                {
                    simd.mul_array_f1f1_indexed<float>(b, a, indexed, 2, false, 4, datacount);
                }
            }
        }



        /// <summary>
        /// f4_out[i] = f4_in[i] * ((float4*)(void*)&((float*) data[i])[offset])[0]
        /// </summary>
        [BurstCompile]
        unsafe public struct test_mul_array_f4f4_indexed : IJob
        {
            public int datacount;
            public int repeatcount;

            public void Execute()
            {
                float4* a = stackalloc float4[datacount];
                float4* b = stackalloc float4[datacount];

                float* data = stackalloc float[datacount * 4];
                float** indexed = stackalloc float*[datacount];

                for (int i = 0; i < datacount; i++) indexed[i] = &data[i * 4];

                for (int i = 0; i < repeatcount; i++)
                {
                    simd.mul_array_f4f4_indexed(b, a, indexed, 2, true, 4, datacount);
                }
            }
        }

        [BurstCompile]
        unsafe public struct test_mul_array_f4f4_indexed_unaligned : IJob
        {
            public int datacount;
            public int repeatcount;

            public void Execute()
            {
                float4* a = stackalloc float4[datacount];
                float4* b = stackalloc float4[datacount];

                float* data = stackalloc float[datacount * 4];
                float** indexed = stackalloc float*[datacount];

                for (int i = 0; i < datacount; i++) indexed[i] = &data[i * 4];

                for (int i = 0; i < repeatcount; i++)
                {
                    simd.mul_array_f4f4_indexed(b, a, indexed, 2, false, 4, datacount);
                }
            }
        }



        [BurstCompile]
        unsafe public struct test_mul_array_f3f3_indexed : IJob
        {
            public int datacount;
            public int repeatcount;

            public void Execute()
            {
                float3* a = stackalloc float3[datacount];
                float3* b = stackalloc float3[datacount];

                float* data = stackalloc float[datacount * 4];
                float** indexed = stackalloc float*[datacount];

                for (int i = 0; i < datacount; i++) indexed[i] = &data[i * 4];

                for (int i = 0; i < repeatcount; i++)
                {
                    simd.mul_array_f3f3_indexed(b, a, indexed, 2, true, 4, datacount);
                }
            }
        }

        [BurstCompile]
        unsafe public struct test_mul_array_f2f2_indexed : IJob
        {
            public int datacount;
            public int repeatcount;

            public void Execute()
            {
                float2* a = stackalloc float2[datacount];
                float2* b = stackalloc float2[datacount];

                float* data = stackalloc float[datacount * 4];
                float** indexed = stackalloc float*[datacount];

                for (int i = 0; i < datacount; i++) indexed[i] = &data[i * 4];

                for (int i = 0; i < repeatcount; i++)
                {
                    simd.mul_array_f2f2_indexed(b, a, indexed, 2, true, 4, datacount);
                }
            }
        }
    }
}