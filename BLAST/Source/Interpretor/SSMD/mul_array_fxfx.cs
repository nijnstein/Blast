using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;

namespace NSS.Blast.SSMD
{
    unsafe public partial struct BlastSSMDInterpretor
    {

        #region mul_array_fxfx
        /// <summary>
        /// a.xyzw = a.xyzw * b.x
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public void mul_array_f4f1([NoAlias] float4* a, [NoAlias] float4* b, in int ssmd_datacount)
        {
            float* fa = (float*)(void*)a;
            float* fb = (float*)(void*)b;
            int i = 0;
                            
            while(i < (ssmd_datacount & ~3))
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

        /// <summary>
        /// a.xyzw = a.xyzw * constant
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public void mul_array_f4f1([NoAlias] float4* a, in float constant, in int ssmd_datacount)
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

        /// <summary>
        /// a.xyzw = a.xyzw * b.xyzw
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public void mul_array_f4f4([NoAlias] float4* a, [NoAlias] float4* b, in int ssmd_datacount)
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

        /// <summary>
        /// a.xyzw = a.xyzw * b.x
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public void mul_array_f3f1([NoAlias] float4* a, [NoAlias] float4* b, in int ssmd_datacount)
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

        /// <summary>
        /// a.xyzw = a.xyzw * constant
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public void mul_array_f3f1([NoAlias] float4* a, in float constant, in int ssmd_datacount)
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

        /// <summary>
        /// a.xyz = a.xyz * b.xyz (in float4 arrays)
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public void mul_array_f3f3([NoAlias] float4* a, [NoAlias] float4* b, in int ssmd_datacount)
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

        /// <summary>
        /// a.xy = a.xy * b.x (in float4 arrays) 
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public void mul_array_f2f1([NoAlias] float4* a, [NoAlias] float4* b, in int ssmd_datacount)
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

        /// <summary>
        /// a.xy = a.xy * constant (in float4)
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public void mul_array_f2f1([NoAlias] float4* a, in float constant, in int ssmd_datacount)
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

        /// <summary>
        /// a.xy = a.xy * b.xy (in float4 arrays)
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public void mul_array_f2f2([NoAlias] float4* a, [NoAlias] float4* b, in int ssmd_datacount)
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

        /// <summary>
        /// a.xy = a.x * b.xy (in float4 arrays)
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public void mul_array_f1f2([NoAlias] float4* a, [NoAlias] float4* b, in int ssmd_datacount)
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

        /// <summary>
        /// a.xyz = a.x * b.xyz (in float4 arrays)
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public void mul_array_f1f3([NoAlias] float4* a, [NoAlias] float4* b, in int ssmd_datacount)
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

        /// <summary>
        /// a.xyz = a.x * b.xyz (in float4 arrays)
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public void mul_array_f1f4([NoAlias] float4* a, [NoAlias] float4* b, in int ssmd_datacount)
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


        /// <summary>
        /// a.x = a.x * b.x (in float4 arrays) 
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public void mul_array_f1f1([NoAlias] float4* a, [NoAlias] float4* b, in int ssmd_datacount)
        {
            float* fa = (float*)(void*)a;
            float* fb = (float*)(void*)b;
            int i = 0;

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

        /// <summary>
        /// a.x = a.x * constant (in float4)
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public void mul_array_f1f1([NoAlias] float4* a, in float constant, in int ssmd_datacount)
        {
            float* fa = (float*)(void*)a;
            int i = 0;

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

                for (int i = 0; i < ssmd_datacount; i++)
                {
                    p_out[0] = p_in[0] * p_data[0];
                    p_out += out_stride;
                    p_in += 1;
                    p_data += data_stride;
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

                for (int i = 0; i < ssmd_datacount; i++)
                {
                    p_out[0] = p_in[0] * p_data[0];
                    p_out[1] = p_in[1] * p_data[1];

                    p_out += out_stride;
                    p_in += 2;
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
        /// for (int i = 0; i<ssmd_datacount; i++) f4_out[i] = f4_in[i] * constant
        /// </summary>
        /// <typeparam name="T"></typeparam>
        static public void mul_array_f4f4_constant([NoAlias] float4* f4_out, [NoAlias] float4* f4_in, float constant, in int ssmd_datacount)
        {
            float* p_out = (float*)(void*)f4_out;
            float* p_in = (float*)(void*)f4_in;

            for (int i = 0; i < ssmd_datacount; i++)
            {
                // when not alligned we have to calculate the offset on each data word
                p_out[0] = p_in[0] * constant;
                p_out[1] = p_in[1] * constant;
                p_out[2] = p_in[2] * constant;
                p_out[3] = p_in[3] * constant;
                p_out += 4;
                p_in += 4;
            }
        }
        /// <summary>
        /// for (int i = 0; i<ssmd_datacount; i++) f3_out[i] = f3_in[i] * constant
        /// </summary>
        /// <typeparam name="T"></typeparam>
        static public void mul_array_f3f3_constant([NoAlias] float3* f3_out, [NoAlias] float3* f3_in, float constant, in int ssmd_datacount)
        {
            float* p_out = (float*)(void*)f3_out;
            float* p_in = (float*)(void*)f3_in;

            for (int i = 0; i < ssmd_datacount; i++)
            {
                // when not alligned we have to calculate the offset on each data word
                p_out[0] = p_in[0] * constant;
                p_out[1] = p_in[1] * constant;
                p_out[2] = p_in[2] * constant;
                p_out += 3;
                p_in += 3;
            }
        }
        /// <summary>
        /// for (int i = 0; i<ssmd_datacount; i++) f4_out[i].xyz = f3_in[i] * constant
        /// </summary>
        /// <typeparam name="T"></typeparam>
        static public void mul_array_f3f3_constant([NoAlias] float4* f4_out, [NoAlias] float3* f3_in, float constant, in int ssmd_datacount)
        {
            float* p_out = (float*)(void*)f4_out;
            float* p_in = (float*)(void*)f3_in;

            for (int i = 0; i < ssmd_datacount; i++)
            {
                // when not alligned we have to calculate the offset on each data word
                p_out[0] = p_in[0] * constant;
                p_out[1] = p_in[1] * constant;
                p_out[2] = p_in[2] * constant;
                p_out += 4;
                p_in += 3;
            }
        }
        /// <summary>
        /// for (int i = 0; i<ssmd_datacount; i++) f2_out[i] = f2_in[i] * constant
        /// </summary>
        /// <typeparam name="T"></typeparam>
        static public void mul_array_f2f2_constant([NoAlias] float2* f2_out, [NoAlias] float2* f2_in, float constant, in int ssmd_datacount)
        {
            float* p_out = (float*)(void*)f2_out;
            float* p_in = (float*)(void*)f2_in;

            for (int i = 0; i < ssmd_datacount; i++)
            {
                p_out[0] = p_in[0] * constant;
                p_out[1] = p_in[1] * constant;
                p_out += 2;
                p_in += 2;
            }
        }
        /// <summary>
        /// for (int i = 0; i<ssmd_datacount; i++) f4_out[i].xyz = f2_in[i] * constant
        /// </summary>
        /// <typeparam name="T"></typeparam>
        static public void mul_array_f2f2_constant([NoAlias] float4* f4_out, [NoAlias] float2* f2_in, float constant, in int ssmd_datacount)
        {
            float* p_out = (float*)(void*)f4_out;
            float* p_in = (float*)(void*)f2_in;

            for (int i = 0; i < ssmd_datacount; i++)
            {
                p_out[0] = p_in[0] * constant;
                p_out[1] = p_in[1] * constant;
                p_out += 4;
                p_in += 2;
            }
        }
        /// <summary>
        /// for (int i = 0; i<ssmd_datacount; i++) f1_out[i] = f1_in[i] * constant
        /// </summary>
        /// <typeparam name="T"></typeparam>
        static public void mul_array_f1f1_constant([NoAlias] float* f1_out, [NoAlias] float* f1_in, float constant, in int ssmd_datacount)
        {
            float* p_out = f1_out;
            float* p_in = f1_in;

            for (int i = 0; i < ssmd_datacount; i++)
            {
                p_out[0] = p_in[0] * constant;
                p_out += 1;
                p_in += 1;
            }
        }

        #endregion 

    }
}
