using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Mathematics;

namespace NSS.Blast.SSMD
{
    unsafe public partial struct simd
    {

        #region move_fx_array_to_indexed_data_as_fx + negated
        /// <summary>
        /// move a float[1] array into an indexed segment 
        /// </summary>

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public void move_f1_array_to_indexed_data_as_f1([NoAlias] void** indexbuffer, int index_rowsize, bool is_aligned, int index, [NoAlias] float* f1, int ssmd_datacount)
        {
            if (is_aligned)
            {
                float* p = &((float*)indexbuffer[0])[index];
                float* s = f1;

                int stride_p = index_rowsize >> 2;

                for (int i = 0; i < ssmd_datacount; i++)
                {
                    p[0] = s[0];
                    p += stride_p;
                    s += 1;
                }
            }
            else
            {
                for (int i = 0; i < ssmd_datacount; i++)
                {
                    ((float*)indexbuffer[i])[index] = f1[i];
                }
            }
        }


        /// <summary>
        /// f[][] = f4.x
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public void move_f4_array_to_indexed_data_as_f1([NoAlias] void** indexbuffer, int index_rowsize, bool is_aligned, int index, [NoAlias] float4* f4, int ssmd_datacount)
        {
            if (is_aligned)
            {
                float* p = &((float*)indexbuffer[0])[index];
                float* s = (float*)(void*)f4;

                int stride_p = index_rowsize >> 2;

                int i = 0;
                while (i < (ssmd_datacount & ~3))
                {
                    p[0] = s[0];
                    p[stride_p] = s[4];
                    p += stride_p + stride_p;
                    p[0] = s[8];
                    p[stride_p] = s[12];
                    p += stride_p + stride_p;
                    s += 16;
                    i += 4;
                }
                while (i < ssmd_datacount)
                {
                    p[0] = s[0];
                    p += stride_p;
                    s += 4;
                    i++;
                }
            }
            else
            {
                for (int i = 0; i < ssmd_datacount; i++)
                {
                    ((float*)indexbuffer[i])[index] = f4[i].x;
                }
            }
        }


        /// <summary>
        /// f[][] = - f4.x
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public void move_f4_array_to_indexed_data_as_f1_negated([NoAlias] void** indexbuffer, int index_rowsize, bool is_aligned, int index, [NoAlias] float4* f4, int ssmd_datacount)
        {
            if (is_aligned)
            {
                float* p = &((float*)indexbuffer[0])[index];
                float* s = (float*)(void*)f4;

                int stride_p = index_rowsize >> 2;

                int i = 0;
                while (i < (ssmd_datacount & ~3))
                {
                    p[0] = -s[0];
                    p[stride_p] = -s[4];
                    p += stride_p + stride_p;
                    p[0] = -s[8];
                    p[stride_p] = -s[12];
                    p += stride_p + stride_p;
                    s += 16;
                    i += 4;
                }
                while (i < ssmd_datacount)
                {
                    p[0] = -s[0];
                    p += stride_p;
                    s += 4;
                    i++;
                }
            }
            else
            {
                for (int i = 0; i < ssmd_datacount; i++)
                {
                    ((float*)indexbuffer[i])[index] = -f4[i].x;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public void move_f4_array_to_indexed_data_as_f2([NoAlias] void** indexbuffer, int index_rowsize, bool is_aligned, int index, [NoAlias] float4* f4, int ssmd_datacount)
        {
            if (is_aligned)
            {
                float* p = &((float*)indexbuffer[0])[index];
                float* s = (float*)(void*)f4;

                int stride_p = index_rowsize >> 2;

                int i = 0;
                while (i < (ssmd_datacount & ~3))
                {
                    p[0] = s[0];
                    p[1] = s[1];
                    p += stride_p;
                    p[0] = s[4];
                    p[1] = s[5];
                    p += stride_p;
                    p[0] = s[8];
                    p[1] = s[9];
                    p += stride_p;
                    p[0] = s[12];
                    p[1] = s[13];
                    p += stride_p;
                    s += 16;
                    i += 4;
                }
                while (i < ssmd_datacount)
                {
                    p[0] = s[0];
                    p[1] = s[1];
                    p += stride_p;
                    s += 4;
                    i += 1;
                }
            }
            else
            {
                for (int i = 0; i < ssmd_datacount; i++)
                {
                    ((float2*)(void*)&((float*)indexbuffer[i])[index])[0] = f4[i].xy;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public void move_f4_array_to_indexed_data_as_f2_negated([NoAlias] void** indexbuffer, int index_rowsize, bool is_aligned, int index, [NoAlias] float4* f4, int ssmd_datacount)
        {
            if (is_aligned)
            {
                float* p = &((float*)indexbuffer[0])[index];
                float* s = (float*)(void*)f4;

                int stride_p = index_rowsize >> 2;


                int i = 0;
                while (i < (ssmd_datacount & ~3))
                {
                    p[0] = -s[0];
                    p[1] = -s[1];
                    p += stride_p;
                    p[0] = -s[4];
                    p[1] = -s[5];
                    p += stride_p;
                    p[0] = -s[8];
                    p[1] = -s[9];
                    p += stride_p;
                    p[0] = -s[12];
                    p[1] = -s[13];
                    p += stride_p;
                    s += 16;
                    i += 4;
                }
                while (i < ssmd_datacount)
                {
                    p[0] = -s[0];
                    p[1] = -s[1];
                    p += stride_p;
                    s += 4;
                    i += 1;
                }
            }
            else
            {
                for (int i = 0; i < ssmd_datacount; i++)
                {
                    ((float2*)(void*)&((float*)indexbuffer[i])[index])[0] = -f4[i].xy;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void move_f4_array_to_indexed_data_as_f3([NoAlias] void** indexbuffer, int index_rowsize, bool is_aligned, int index, [NoAlias] float4* f4, int ssmd_datacount)
        {
            if (is_aligned)
            {
                float* p = &((float*)indexbuffer[0])[index];
                float* s = (float*)(void*)f4;

                int stride_p = index_rowsize >> 2;
                int i = 0;

                while (i < (ssmd_datacount & ~3))
                {
                    p[0] = s[0];
                    p[1] = s[1];
                    p[2] = s[2];
                    p += stride_p;
                    p[0] = s[4];
                    p[1] = s[5];
                    p[2] = s[6];
                    p += stride_p;
                    p[0] = s[8];
                    p[1] = s[9];
                    p[2] = s[10];
                    p += stride_p;
                    p[0] = s[12];
                    p[1] = s[13];
                    p[2] = s[14];
                    p += stride_p;
                    s += 16;
                    i += 4;
                }
                while (i < ssmd_datacount)
                {
                    p[0] = s[0];
                    p[1] = s[1];
                    p[2] = s[2];
                    p += stride_p;
                    s += 4;
                    i++;
                }
            }
            else
            {
                for (int i = 0; i < ssmd_datacount; i++)
                {
                    ((float3*)(void*)&((float*)indexbuffer[i])[index])[0] = f4[i].xyz;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public void move_f4_array_to_indexed_data_as_f3_negated([NoAlias] void** indexbuffer, int index_rowsize, bool is_aligned, int index, [NoAlias] float4* f4, int ssmd_datacount)
        {
            if (is_aligned)
            {
                float* p = &((float*)indexbuffer[0])[index];
                float* s = (float*)(void*)f4;

                int stride_p = index_rowsize >> 2;
                int i = 0;

                while (i < (ssmd_datacount & ~3))
                {
                    p[0] = -s[0];
                    p[1] = -s[1];
                    p[2] = -s[2];
                    p += stride_p;
                    p[0] = -s[4];
                    p[1] = -s[5];
                    p[2] = -s[6];
                    p += stride_p;
                    p[0] = -s[8];
                    p[1] = -s[9];
                    p[2] = -s[10];
                    p += stride_p;
                    p[0] = -s[12];
                    p[1] = -s[13];
                    p[2] = -s[14];
                    p += stride_p;
                    i += 4;
                    s += 16;
                }
                while (i < ssmd_datacount)
                {
                    p[0] = s[0];
                    p[1] = s[1];
                    p[2] = s[2];
                    p += stride_p;
                    s += 4;
                    i++;
                }
            }
            else
            {
                for (int i = 0; i < ssmd_datacount; i++)
                {
                    ((float3*)(void*)&((float*)indexbuffer[i])[index])[0] = -f4[i].xyz;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public void move_f4_array_to_indexed_data_as_f4([NoAlias] void** indexbuffer, int index_rowsize, bool is_aligned, int index, [NoAlias] float4* f4, int ssmd_datacount)
        {
            if (is_aligned)
            {
                float* p = &((float*)indexbuffer[0])[index];
                float* s = (float*)(void*)f4;

                int stride_p = index_rowsize >> 2;
                int i = 0;

                while (i < (ssmd_datacount & ~3))
                {
                    p[0] = s[0];
                    p[1] = s[1];
                    p[2] = s[2];
                    p[3] = s[3];
                    p += stride_p;
                    p[0] = s[4];
                    p[1] = s[5];
                    p[2] = s[6];
                    p[3] = s[7];
                    p += stride_p;
                    p[0] = s[8];
                    p[1] = s[9];
                    p[2] = s[10];
                    p[3] = s[11];
                    p += stride_p;
                    p[0] = s[12];
                    p[1] = s[13];
                    p[2] = s[14];
                    p[3] = s[15];
                    p += stride_p;
                    s += 16;
                    i += 4;
                }
                while (i < ssmd_datacount)
                {
                    p[0] = s[0];
                    p[1] = s[1];
                    p[2] = s[2];
                    p[3] = s[3];
                    p += stride_p;
                    s += 4;
                    i++;
                }
            }
            else
            {
                for (int i = 0; i < ssmd_datacount; i++)
                {
                    ((float4*)(void*)&((float*)indexbuffer[i])[index])[0] = f4[i];
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public void move_f4_array_to_indexed_data_as_f4_negated([NoAlias] void** indexbuffer, int index_rowsize, bool is_aligned, int index, [NoAlias] float4* f4, int ssmd_datacount)
        {
            if (is_aligned)
            {
                float* p = &((float*)indexbuffer[0])[index];
                float* s = (float*)(void*)f4;

                int stride_p = index_rowsize >> 2;
                int i = 0;

                while (i < (ssmd_datacount & ~3))
                {
                    p[0] = -s[0];
                    p[1] = -s[1];
                    p[2] = -s[2];
                    p[3] = -s[3];
                    p += stride_p;
                    p[0] = -s[4];
                    p[1] = -s[5];
                    p[2] = -s[6];
                    p[3] = -s[7];
                    p += stride_p;
                    p[0] = -s[8];
                    p[1] = -s[9];
                    p[2] = -s[10];
                    p[3] = -s[11];
                    p += stride_p;
                    p[0] = -s[12];
                    p[1] = -s[13];
                    p[2] = -s[14];
                    p[3] = -s[15];
                    p += stride_p;
                    s += 16;
                    i += 4;
                }
                while (i < ssmd_datacount)
                {
                    p[0] = -s[0];
                    p[1] = -s[1];
                    p[2] = -s[2];
                    p[3] = -s[3];
                    p += stride_p;
                    s += 4;
                    i++;
                }
            }
            else
            {
                for (int i = 0; i < ssmd_datacount; i++)
                {
                    ((float4*)(void*)&((float*)indexbuffer[i])[index])[0] = -f4[i];
                }
            }
        }

        #endregion

        #region  move_fx_constant_to_indexed_data_as_fx

        /// <summary>
        /// move a constant float value as array into an indexed segment 
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public void move_f1_constant_to_indexed_data_as_f1([NoAlias] void** indexbuffer, int index_rowsize, bool is_aligned, int index, float constant, int ssmd_datacount)
        {
            if (is_aligned)
            {
                float* p = &((float*)indexbuffer[0])[index];
                int stride_p = index_rowsize >> 2;

                for (int i = 0; i < ssmd_datacount; i++)
                {
                    p[0] = constant;
                    p += stride_p;
                }
            }
            else
            {
                for (int i = 0; i < ssmd_datacount; i++)
                {
                    ((float*)indexbuffer[i])[index] = constant;
                }
            }
        }

        /// <summary>
        /// move a constant float value as array into an indexed segment : [][] = xy
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public void move_f1_constant_to_indexed_data_as_f2([NoAlias] void** indexbuffer, int index_rowsize, bool is_aligned, int index, float constant, int ssmd_datacount)
        {
            if (is_aligned)
            {
                float* p = &((float*)indexbuffer[0])[index];
                int stride_p = index_rowsize >> 2;

                for (int i = 0; i < ssmd_datacount; i++)
                {
                    p[0] = constant;
                    p[1] = constant;
                    p += stride_p;
                }
            }
            else
            {
                for (int i = 0; i < ssmd_datacount; i++)
                {
                    ((float2*)(void*)&((float*)indexbuffer[i])[index])[0] = constant;
                }
            }
        }


        /// <summary>
        /// move a constant float value as array into an indexed segment : [][] = xyz
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public void move_f1_constant_to_indexed_data_as_f3([NoAlias] void** indexbuffer, int index_rowsize, bool is_aligned, int index, float constant, int ssmd_datacount)
        {
            if (is_aligned)
            {
                float* p = &((float*)indexbuffer[0])[index];
                int stride_p = index_rowsize >> 2;

                for (int i = 0; i < ssmd_datacount; i++)
                {
                    p[0] = constant;
                    p[1] = constant;
                    p[2] = constant;
                    p += stride_p;
                }
            }
            else
            {
                for (int i = 0; i < ssmd_datacount; i++)
                {
                    ((float3*)(void*)&((float*)indexbuffer[i])[index])[0] = constant;
                }
            }
        }

        /// <summary>
        /// move a constant float value as array into an indexed segment [][] = xyzw
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public void move_f1_constant_to_indexed_data_as_f4([NoAlias] void** indexbuffer, int index_rowsize, bool is_aligned, int index, float constant, int ssmd_datacount)
        {
            if (is_aligned)
            {
                float* p = &((float*)indexbuffer[0])[index];
                int stride_p = index_rowsize >> 2;

                for (int i = 0; i < ssmd_datacount; i++)
                {
                    p[0] = constant;
                    p[1] = constant;
                    p[2] = constant;
                    p[3] = constant;
                    p += stride_p;
                }
            }
            else
            {
                for (int i = 0; i < ssmd_datacount; i++)
                {
                    ((float4*)(void*)&((float*)indexbuffer[i])[index])[0] = constant;
                }
            }
        }



        #endregion 

    }
}
