//############################################################################################################################
// BLAST v1.0.4c                                                                                                             #
// Copyright © 2022 Rob Lemmens | NijnStein Software <rob.lemmens.s31 gmail com> All Rights Reserved                   ^__^\ #
// Unauthorized copying of this file, via any medium is strictly prohibited proprietary and confidential               (oo)\ #
//                                                                                                                     (__)  #
//############################################################################################################################

#pragma warning disable CS0162   // disable warnings for paths not taken dueue to compiler defines 

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
        static public void move_indexed_data_as_f2_to_f2([NoAlias] float2* f2, [NoAlias] void** indexbuffer, int index_rowsize, bool is_aligned, int index, int ssmd_datacount)
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
        static public void move_indexed_data_as_f3_to_f3([NoAlias] float3* f3, [NoAlias] void** indexbuffer, int index_rowsize, bool is_aligned, int index, int ssmd_datacount)
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
        static public void move_indexed_data_as_f4_to_f4([NoAlias] float4* f4, [NoAlias] void** indexbuffer, int index_rowsize, bool is_aligned, int index, int ssmd_datacount)
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


        #region move_indexed_data_as_fx_to_fx negated    expanding from f1 

        /// <summary>
        /// set a dest.x from indexed source[offset].x
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public void move_indexed_data_as_f1_to_f1f4([NoAlias] float4* destination, in DATAREC source, int ssmd_datacount)
        {
            move_indexed_data_as_f1_to_f1f4(destination, source.data, source.row_size, source.is_aligned, source.index, ssmd_datacount);
        }


        /// <summary>
        /// set a dest.x from indexed data[offset].x
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public void move_indexed_data_as_f1_to_f1f4([NoAlias] float4* destination, [NoAlias] void** indexbuffer, int index_rowsize, bool is_aligned, in int index, int ssmd_datacount)
        {
            if (is_aligned)
            {
                float* s = &((float*)indexbuffer[0])[index];
                float* p = (float*)(void*)destination;
 
                int stride_s = index_rowsize >> 2;
                int stride_s8 = stride_s * 8;
                int stride_s2 = stride_s * 2;
                int stride_s4 = stride_s * 2;

                int i = 0;

                while (i < (ssmd_datacount & ~7))
                {
                    p[0] = s[0];
                    p[4] = s[stride_s];
                    p[8] = s[stride_s2];
                    p[12] = s[stride_s2 + stride_s];
                    p[16] = s[stride_s4];
                    p[20] = s[stride_s4 + stride_s];
                    p[24] = s[stride_s4 + stride_s2];
                    p[28] = s[stride_s4 + stride_s2 + stride_s];
                    s += stride_s8;
                    p += 4 * 8;
                    i += 8;
                }
                while (i < ssmd_datacount)
                {
                    p[0] = s[0];
                    s += stride_s;
                    p += 4;
                    i++;
                }
            }
            else
            {
                for (int i = 0; i < ssmd_datacount; i++)
                {
                    destination[i].x = ((float*)indexbuffer[i])[index];
                }
            }
        }


        /// <summary>
        /// set f4[].x from -data[][].x
        /// </summary>
        /// <param name="destination"></param>
        /// <param name="indexbuffer"></param>
        /// <param name="index_rowsize"></param>
        /// <param name="is_aligned"></param>
        /// <param name="index"></param>
        /// <param name="ssmd_datacount"></param>

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public void move_indexed_data_as_f1_to_f1f4_negated([NoAlias] float4* destination, [NoAlias] void** indexbuffer, int index_rowsize, bool is_aligned, in int index, int ssmd_datacount)
        {
            if (is_aligned)
            {
                float* s = &((float*)indexbuffer[0])[index];
                float* p = (float*)(void*)destination;

                int stride_s = index_rowsize >> 2;
                int stride_s8 = stride_s * 8;
                int stride_s2 = stride_s * 2;
                int stride_s4 = stride_s * 2;

                int i = 0;

                while(i < (ssmd_datacount & ~7))
                {
                    p[0]  = -s[0];
                    p[4]  = -s[stride_s];
                    p[8]  = -s[stride_s2];
                    p[12] = -s[stride_s2 + stride_s];
                    p[16] = -s[stride_s4];
                    p[20] = -s[stride_s4 + stride_s];
                    p[24] = -s[stride_s4 + stride_s2];
                    p[28] = -s[stride_s4 + stride_s2 + stride_s];
                    s += stride_s8;
                    p += 4 * 8;
                    i += 8;
                }
                while (i < ssmd_datacount)
                {
                    p[0] = -s[0];
                    s += stride_s;
                    p += 4;
                    i++;
                }
            }
            else
            {
                for (int i = 0; i < ssmd_datacount; i++)
                {
                    destination[i].x = -((float*)indexbuffer[i])[index];
                }
            }
        }


        /// <summary>
        /// set a dest.xyz from indexed data[offset].x
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public void move_indexed_data_as_f1_to_f3([NoAlias] float4* destination, [NoAlias] void** indexbuffer, int index_rowsize, bool is_aligned, in int index, int ssmd_datacount)
        {
            if (is_aligned)
            {
                float* s = &((float*)indexbuffer[0])[index];
                float* p = (float*)(void*)destination;

                int stride_s = index_rowsize >> 2;

                for (int i = 0; i < ssmd_datacount; i++)
                {
                    p[0] = s[0];
                    p[1] = s[0];
                    p[2] = s[0];
                    s += stride_s;
                    p += 4;
                }
            }
            else
            {
                for (int i = 0; i < ssmd_datacount; i++)
                {
                    destination[i].xyz = ((float*)indexbuffer[i])[index];
                }
            }
        }

        /// <summary>
        /// set a dest.xyz from indexed data[offset].x
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public void move_indexed_data_as_f1_to_f3_negated([NoAlias] float4* destination, [NoAlias] void** indexbuffer, int index_rowsize, bool is_aligned, in int index, int ssmd_datacount)
        {
            if (is_aligned)
            {
                float* s = &((float*)indexbuffer[0])[index];
                float* p = (float*)(void*)destination;

                int stride_s = index_rowsize >> 2;

                for (int i = 0; i < ssmd_datacount; i++)
                {
                    p[0] = -s[0];
                    p[1] = -s[0];
                    p[2] = -s[0];
                    s += stride_s;
                    p += 4;
                }
            }
            else
            {
                for (int i = 0; i < ssmd_datacount; i++)
                {
                    destination[i].xyz = -((float*)indexbuffer[i])[index];
                }
            }
        }


        /// <summary>
        /// set a dest.xy from indexed data[offset].x
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public void move_indexed_data_as_f1_to_f2([NoAlias] float4* destination, [NoAlias] void** indexbuffer, int index_rowsize, bool is_aligned, in int index, int ssmd_datacount)
        {
            if (is_aligned)
            {
                float* s = &((float*)indexbuffer[0])[index];
                float* p = (float*)(void*)destination;

                int stride_s = index_rowsize >> 2;

                for (int i = 0; i < ssmd_datacount; i++)
                {
                    p[0] = s[0];
                    p[1] = s[0];
                    s += stride_s;
                    p += 4;
                }
            }
            else
            {
                for (int i = 0; i < ssmd_datacount; i++)
                {
                    destination[i].xy = ((float*)indexbuffer[i])[index];
                }
            }
        }

        /// <summary>
        /// set a dest.xy from indexed data[offset].x
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public void move_indexed_data_as_f1_to_f2_negated([NoAlias] float4* destination, [NoAlias] void** indexbuffer, int index_rowsize, bool is_aligned, in int index, int ssmd_datacount)
        {
            if (is_aligned)
            {
                float* s = &((float*)indexbuffer[0])[index];
                float* p = (float*)(void*)destination;

                int stride_s = index_rowsize >> 2;

                for (int i = 0; i < ssmd_datacount; i++)
                {
                    float f = -s[0];
                    p[0] = f;
                    p[1] = f;
                    s += stride_s;
                    p += 4;
                }
            }
            else
            {
                for (int i = 0; i < ssmd_datacount; i++)
                {
                    destination[i].xy = -((float*)indexbuffer[i])[index];
                }
            }
        }


        /// <summary>
        /// set f4[i].xyzw from  data[][].x
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public void move_indexed_data_as_f1_to_f4([NoAlias] float4* destination, [NoAlias] void** indexbuffer, int index_rowsize, bool is_aligned, in int index, int ssmd_datacount)
        {
            if (is_aligned)
            {
                float* s = &((float*)indexbuffer[0])[index];
                float* p = (float*)(void*)destination;

                int stride_s = index_rowsize >> 2;

                for (int i = 0; i < ssmd_datacount; i++)
                {
                    p[0] = s[0];
                    p[1] = s[0];
                    p[2] = s[0];
                    p[3] = s[0];
                    s += stride_s;
                    p += 4;
                }
            }
            else
            {
                for (int i = 0; i < ssmd_datacount; i++)
                {
                    destination[i] = ((float*)indexbuffer[i])[index];
                }
            }
        }


        /// <summary>
        /// set f4[i].xyzw from  - (data[][].x)
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public void move_indexed_data_as_f1_to_f4_negated([NoAlias] float4* destination, [NoAlias] void** indexbuffer, int index_rowsize, bool is_aligned, in int index, int ssmd_datacount)
        {
            if (is_aligned)
            {
                float* s = &((float*)indexbuffer[0])[index];
                float* p = (float*)(void*)destination;

                int stride_s = index_rowsize >> 2;

                for (int i = 0; i < ssmd_datacount; i++)
                {
                    float f = -s[0];
                    p[0] = f;
                    p[1] = f;
                    p[2] = f;
                    p[3] = f;
                    s += stride_s;
                    p += 4;
                }
            }
            else
            {
                for (int i = 0; i < ssmd_datacount; i++)
                {
                    destination[i] = -((float*)indexbuffer[i])[index];
                }
            }
        }



        /// <summary>
        /// set a dest.xy from indexed data[offset]
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public void move_indexed_data_as_f2_to_f2([NoAlias] float4* destination, [NoAlias] void** indexbuffer, int index_rowsize, bool is_aligned, in int index, int ssmd_datacount)
        {
            if (is_aligned)
            {
                float* s = &((float*)indexbuffer[0])[index];
                float* p = (float*)(void*)destination;

                int stride_s = index_rowsize >> 2;

                for (int i = 0; i < ssmd_datacount; i++)
                {
                    p[0] = s[0];
                    p[1] = s[1];
                    s += stride_s;
                    p += 4;
                }
            }
            else
            {
                for (int i = 0; i < ssmd_datacount; i++)
                {
                    destination[i].xy = ((float2*)(void*)&((float*)indexbuffer[i])[index])[0];
                }
            }
        }


        /// <summary>
        /// set a dest.xy from negated indexed data[offset]
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public void move_indexed_data_as_f2_to_f2_negated([NoAlias] float4* destination, [NoAlias] void** indexbuffer, int index_rowsize, bool is_aligned, in int index, int ssmd_datacount)
        {
            if (is_aligned)
            {
                float* s = &((float*)indexbuffer[0])[index];
                float* p = (float*)(void*)destination;

                int stride_s = index_rowsize >> 2;

                for (int i = 0; i < ssmd_datacount; i++)
                {
                    float f = -s[0];
                    p[0] = f;
                    p[1] = f;
                    s += stride_s;
                    p += 4;
                }
            }
            else
            {
                for (int i = 0; i < ssmd_datacount; i++)
                {
                    destination[i].xy = -((float2*)(void*)&((float*)indexbuffer[i])[index])[0];
                }
            }
        }


        /// <summary>
        /// set a dest.xyz from indexed data[offset]
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public void move_indexed_data_as_f3_to_f3([NoAlias] float4* destination, [NoAlias] void** indexbuffer, int index_rowsize, bool is_aligned, in int index, int ssmd_datacount)
        {
            if (is_aligned)
            {
                float* s = &((float*)indexbuffer[0])[index];
                float* p = (float*)(void*)destination;

                int stride_s = index_rowsize >> 2;

                for (int i = 0; i < ssmd_datacount; i++)
                {
                    p[0] = s[0];
                    p[1] = s[1];
                    p[2] = s[2];
                    s += stride_s;
                    p += 4;
                }
            }                                                                 
            else
            {
                for (int i = 0; i < ssmd_datacount; i++)
                {
                    destination[i].xyz = ((float3*)(void*)&((float*)indexbuffer[i])[index])[0];
                }
            }
        }


        /// <summary>
        /// set a dest.xyz from negated indexed data[offset]
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public void move_indexed_data_as_f3_to_f3_negated([NoAlias] float4* destination, [NoAlias] void** indexbuffer, int index_rowsize, bool is_aligned, in int index, int ssmd_datacount)
        {
            if (is_aligned)
            {
                float* s = &((float*)indexbuffer[0])[index];
                float* p = (float*)(void*)destination;

                int stride_s = index_rowsize >> 2;

                for (int i = 0; i < ssmd_datacount; i++)
                {
                    float f = -s[0];
                    p[0] = f;
                    p[1] = f;
                    p[2] = f;
                    s += stride_s;
                    p += 4;
                }
            }
            else
            {
                for (int i = 0; i < ssmd_datacount; i++)
                {
                    destination[i].xyz = -((float3*)(void*)&((float*)indexbuffer[i])[index])[0];
                }
            }
        }


        /// <summary>
        /// set a dest.xyzw from indexed data[offset]
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public void move_indexed_data_as_f4_to_f4([NoAlias] float4* destination, [NoAlias] void** indexbuffer, in int index_rowsize, bool is_aligned, int index, int ssmd_datacount)
        {
            if (is_aligned)
            {
                float* s = &((float*)indexbuffer[0])[index];
                float* p = (float*)(void*)destination;

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
                    destination[i] = ((float4*)(void*)&((float*)indexbuffer[i])[index])[0];
                }
            }
        }


        /// <summary>
        /// set a dest.xyzw from negated indexed data[offset]
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public void move_indexed_data_as_f4_to_f4_negated([NoAlias] float4* destination, [NoAlias] void** indexbuffer, in int index_rowsize, bool is_aligned, int index, int ssmd_datacount)
        {
            if (is_aligned)
            {
                float* s = &((float*)indexbuffer[0])[index];
                float* p = (float*)(void*)destination;

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
                    destination[i] = -((float4*)(void*)&((float*)indexbuffer[i])[index])[0];
                }
            }
        }


        #endregion
    }
}
