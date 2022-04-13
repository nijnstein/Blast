using System;
using System.Collections.Generic;
using System.Text;

using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Mathematics;

namespace NSS.Blast.SSMD
{
    unsafe public partial struct simd
    {

        #region move_indexed_data_as_f1_to_f1

        /// <summary>
        /// f1[] = data[][].x
        /// </summary>

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public void move_indexed_data_as_f1_to_f1([NoAlias] float* f1, [NoAlias] void** indexbuffer, int index_rowsize, bool is_aligned, int index, int ssmd_datacount)
        {
            if (is_aligned)
            {
                float* s = &((float*)indexbuffer[0])[index];
                float* p = f1;

                int stride_s = index_rowsize >> 2;

                for (int i = 0; i < ssmd_datacount; i++)
                {
                    p[0] = s[0];
                    s += stride_s;
                    p += 1;
                }
            }
            else
            {
                for (int i = 0; i < ssmd_datacount; i++)
                {
                    f1[i] = ((float*)indexbuffer[i])[index];
                }
            }
        }

        /// <summary>
        /// f2[] = data[][].xy
        /// </summary>

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public void move_indexed_data_as_f1_to_f2([NoAlias] float2* f2, [NoAlias] void** indexbuffer, int index_rowsize, bool is_aligned, int index, int ssmd_datacount)
        {
            if (is_aligned)
            {
                float* s = &((float*)indexbuffer[0])[index];
                float* p = (float*)(void*)f2;

                int stride_s = index_rowsize >> 2;

                for (int i = 0; i < ssmd_datacount; i++)
                {
                    p[0] = s[0];
                    p[1] = s[1];
                    s += stride_s;
                    p += 2;
                }
            }
            else
            {
                for (int i = 0; i < ssmd_datacount; i++)
                {
                    f2[i] = ((float2*)(void*)&((float*)indexbuffer[i])[index])[0];
                }
            }
        }

        /// <summary>
        /// f3[] = data[][].xyz
        /// </summary>

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public void move_indexed_data_as_f1_to_f3([NoAlias] float3* f3, [NoAlias] void** indexbuffer, int index_rowsize, bool is_aligned, int index, int ssmd_datacount)
        {
            if (is_aligned)
            {
                float* s = &((float*)indexbuffer[0])[index];
                float* p = (float*)(void*)f3;

                int stride_s = index_rowsize >> 2;

                for (int i = 0; i < ssmd_datacount; i++)
                {
                    p[0] = s[0];
                    p[1] = s[1];
                    p[2] = s[2];
                    s += stride_s;
                    p += 3;
                }
            }
            else
            {
                for (int i = 0; i < ssmd_datacount; i++)
                {
                    f3[i] = ((float3*)(void*)&((float*)indexbuffer[i])[index])[0];
                }
            }
        }

        /// <summary>
        /// f4[] = data[][].xyzw
        /// </summary>

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public void move_indexed_data_as_f1_to_f4([NoAlias] float4* f4, [NoAlias] void** indexbuffer, int index_rowsize, bool is_aligned, int index, int ssmd_datacount)
        {
            if (is_aligned)
            {
                float* s = &((float*)indexbuffer[0])[index];
                float* p = (float*)(void*)f4;

                int stride_s = index_rowsize >> 2;

                for (int i = 0; i < ssmd_datacount; i++)
                {
                    p[0] = s[0];
                    p[1] = s[1];
                    p[2] = s[2];
                    p[3] = s[3];
                    s += stride_s;
                    p += 4;
                }
            }
            else
            {
                for (int i = 0; i < ssmd_datacount; i++)
                {
                    f4[i] = ((float4*)(void*)&((float*)indexbuffer[i])[index])[0];
                }
            }
        }

        #endregion



        #region move_indexed_data_as_f1_to_f1 negated

        /// <summary>
        /// f1[] = -data[][].x
        /// </summary>

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public void move_indexed_data_as_f1_to_f1_n([NoAlias] float* f1, [NoAlias] void** indexbuffer, int index_rowsize, bool is_aligned, int index, int ssmd_datacount)
        {
            if (is_aligned)
            {
                float* s = &((float*)indexbuffer[0])[index];
                float* p = f1;

                int stride_s = index_rowsize >> 2;

                for (int i = 0; i < ssmd_datacount; i++)
                {
                    p[0] = -s[0];
                    s += stride_s;
                    p += 1;
                }
            }
            else
            {
                for (int i = 0; i < ssmd_datacount; i++)
                {
                    f1[i] = -((float*)indexbuffer[i])[index];
                }
            }
        }

        /// <summary>
        /// f2[] = -data[][].xy
        /// </summary>

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public void move_indexed_data_as_f1_to_f2_n([NoAlias] float2* f2, [NoAlias] void** indexbuffer, int index_rowsize, bool is_aligned, int index, int ssmd_datacount)
        {
            if (is_aligned)
            {
                float* s = &((float*)indexbuffer[0])[index];
                float* p = (float*)(void*)f2;

                int stride_s = index_rowsize >> 2;

                for (int i = 0; i < ssmd_datacount; i++)
                {
                    p[0] = -s[0];
                    p[1] = -s[1];
                    s += stride_s;
                    p += 2;
                }
            }
            else
            {
                for (int i = 0; i < ssmd_datacount; i++)
                {
                    f2[i] = -((float2*)(void*)&((float*)indexbuffer[i])[index])[0];
                }
            }
        }

        /// <summary>
        /// f3[] = -data[][].xyz
        /// </summary>

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public void move_indexed_data_as_f1_to_f3_n([NoAlias] float3* f3, [NoAlias] void** indexbuffer, int index_rowsize, bool is_aligned, int index, int ssmd_datacount)
        {
            if (is_aligned)
            {
                float* s = &((float*)indexbuffer[0])[index];
                float* p = (float*)(void*)f3;

                int stride_s = index_rowsize >> 2;

                for (int i = 0; i < ssmd_datacount; i++)
                {
                    p[0] = -s[0];
                    p[1] = -s[1];
                    p[2] = -s[2];
                    s += stride_s;
                    p += 3;
                }
            }
            else
            {
                for (int i = 0; i < ssmd_datacount; i++)
                {
                    f3[i] = - ((float3*)(void*)&((float*)indexbuffer[i])[index])[0];
                }
            }
        }

        /// <summary>
        /// f4[] = - data[][].xyzw
        /// </summary>

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public void move_indexed_data_as_f1_to_f4_n([NoAlias] float4* f4, [NoAlias] void** indexbuffer, int index_rowsize, bool is_aligned, int index, int ssmd_datacount)
        {
            if (is_aligned)
            {
                float* s = &((float*)indexbuffer[0])[index];
                float* p = (float*)(void*)f4;

                int stride_s = index_rowsize >> 2;

                for (int i = 0; i < ssmd_datacount; i++)
                {
                    p[0] = -s[0];
                    p[1] = -s[1];
                    p[2] = -s[2];
                    p[3] = -s[3];
                    s += stride_s;
                    p += 4;
                }
            }
            else
            {
                for (int i = 0; i < ssmd_datacount; i++)
                {
                    f4[i] = -((float4*)(void*)&((float*)indexbuffer[i])[index])[0];
                }
            }
        }

        #endregion


    }
}
