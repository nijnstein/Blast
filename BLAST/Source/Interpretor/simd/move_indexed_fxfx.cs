//############################################################################################################################
// BLAST v1.0.4c                                                                                                             #
// Copyright © 2022 Rob Lemmens | NijnStein Software <rob.lemmens.s31 gmail com> All Rights Reserved                   ^__^\ #
// Unauthorized copying of this file, via any medium is strictly prohibited proprietary and confidential               (oo)\ #
//                                                                                                                     (__)  #
//############################################################################################################################

using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Mathematics;

namespace NSS.Blast.SSMD
{
    unsafe public partial struct simd
    {
        // 
        // move float[1..4] from indexed [][] to indexed [][]
        // - handles alignment 
        // - handles negation 
        // 

        #region move_indexed_fx
        /// <summary>
        /// data[][].x = data[][].x
        /// </summary>

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public void move_indexed_f1([NoAlias] void** destination_indexbuffer, int destination_index_rowsize, bool destination_is_aligned, int destination_index, [NoAlias] void** source_indexbuffer, int source_index_rowsize, bool source_is_aligned, int source_index, int ssmd_datacount)
        {
            if (destination_is_aligned && source_is_aligned)
            {
                float* s = &((float*)source_indexbuffer[0])[source_index];
                float* p = &((float*)destination_indexbuffer[0])[destination_index];

                int stride_s = source_index_rowsize >> 2;
                int stride_d = destination_index_rowsize >> 2;

                int stride_s_2 = stride_s + stride_s;
                int stride_d_2 = stride_d + stride_d;

                int stride_d_3 = stride_d_2 + stride_d;
                int stride_s_3 = stride_s_2 + stride_s;

                int stride_d_4 = stride_d_3 + stride_d;
                int stride_s_4 = stride_s_3 + stride_s;

                int i = 0;
                while (i < (ssmd_datacount & ~3))
                {
                    p[0] = s[0];
                    p[stride_d] = s[stride_s];
                    p[stride_d_2] = s[stride_s_2];
                    p[stride_d_3] = s[stride_s_3];

                    s += stride_s_4;
                    p += stride_d_4;
                    i += 4;
                }
                while (i < ssmd_datacount)
                {
                    p[0] = s[0];
                    s += stride_s;
                    p += stride_d;
                    i++;
                }
                return;
            }
            if (destination_is_aligned)
            {
                // there is a high penalty when we need to index each array, especially for float 2 and 3 arrays
                float* p = &((float*)destination_indexbuffer[0])[destination_index];

                int stride_d = destination_index_rowsize >> 2;
                int stride_d_2 = stride_d + stride_d;
                int stride_d_3 = stride_d_2 + stride_d;
                int stride_d_4 = stride_d_3 + stride_d;

                int i = 0;
                while (i < (ssmd_datacount & ~3))
                {
                    p[0] = ((float*)source_indexbuffer[i])[source_index];
                    p[stride_d] = ((float*)source_indexbuffer[i + 1])[source_index];
                    p[stride_d_2] = ((float*)source_indexbuffer[i + 2])[source_index];
                    p[stride_d_3] = ((float*)source_indexbuffer[i + 3])[source_index];

                    p += stride_d_4;
                    i += 4;
                }
                while (i < ssmd_datacount)
                {
                    p[0] = ((float*)source_indexbuffer[i])[source_index];
                    p += stride_d;
                    i++;
                }
                return;
            }
            if (source_is_aligned)
            {
                float* s = &((float*)source_indexbuffer[0])[source_index];

                int stride_s = source_index_rowsize >> 2;
                int stride_s_2 = stride_s + stride_s;
                int stride_s_3 = stride_s_2 + stride_s;
                int stride_s_4 = stride_s_3 + stride_s;

                int i = 0;
                while (i < (ssmd_datacount & ~3))
                {
                    ((float*)destination_indexbuffer[i + 0])[destination_index] = s[0];
                    ((float*)destination_indexbuffer[i + 1])[destination_index] = s[stride_s];
                    ((float*)destination_indexbuffer[i + 2])[destination_index] = s[stride_s_2];
                    ((float*)destination_indexbuffer[i + 3])[destination_index] = s[stride_s_3];

                    s += stride_s_4;
                    i += 4;
                }
                while (i < ssmd_datacount)
                {
                    ((float*)destination_indexbuffer[i])[destination_index] = s[0];
                    s += stride_s;
                    i++;
                }
                return;
            }
            for (int i = 0; i < ssmd_datacount; i++)
            {
                ((float*)destination_indexbuffer[i])[destination_index] = ((float*)source_indexbuffer[i])[source_index];
            }
        }



        /// <summary>
        /// data expansion of f1 to f2 in indexed data arrays: data[][].xy = data[][].x
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public void move_indexed_f1f2(in DATAREC destination, [NoAlias] void** source_indexbuffer, int source_index_rowsize, bool source_is_aligned, int source_index, int ssmd_datacount)
        {
            move_indexed_f1f2(destination.data, destination.row_size, destination.is_aligned, destination.index, source_indexbuffer, source_index_rowsize, source_is_aligned, source_index, ssmd_datacount);
        }

        /// <summary>
        /// data expansion of f1 to f2 in indexed data arrays: data[][].xy = -data[][].x
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public void move_indexed_f1f2_negated(in DATAREC destination, [NoAlias] void** source_indexbuffer, int source_index_rowsize, bool source_is_aligned, int source_index, int ssmd_datacount)
        {
            move_indexed_f1f2_negated(destination.data, destination.row_size, destination.is_aligned, destination.index, source_indexbuffer, source_index_rowsize, source_is_aligned, source_index, ssmd_datacount);
        }

        /// <summary>
        /// data expansion of f1 to f2 in indexed data arrays: data[][].xy = data[][].x
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public void move_indexed_f1f2([NoAlias] void** destination_indexbuffer, int destination_index_rowsize, bool destination_is_aligned, int destination_index, [NoAlias] void** source_indexbuffer, int source_index_rowsize, bool source_is_aligned, int source_index, int ssmd_datacount)
        {
            if (destination_is_aligned && source_is_aligned)
            {
                float* s = &((float*)source_indexbuffer[0])[source_index];
                float* p = &((float*)destination_indexbuffer[0])[destination_index];

                int stride_s = source_index_rowsize >> 2;
                int stride_d = destination_index_rowsize >> 2;

                int stride_s_2 = stride_s + stride_s;
                int stride_d_2 = stride_d + stride_d;

                int stride_d_3 = stride_d_2 + stride_d;
                int stride_s_3 = stride_s_2 + stride_s;

                int stride_d_4 = stride_d_3 + stride_d;
                int stride_s_4 = stride_s_3 + stride_s;

                int i = 0;
                while (i < (ssmd_datacount & ~3))
                {
                    p[0] = s[0];
                    p[1] = s[0];
                    p[stride_d] = s[stride_s];
                    p[stride_d + 1] = s[stride_s];
                    p[stride_d_2] = s[stride_s_2];
                    p[stride_d_2 + 1] = s[stride_s_2];
                    p[stride_d_3] = s[stride_s_3];
                    p[stride_d_3 + 1] = s[stride_s_3];

                    s += stride_s_4;
                    p += stride_d_4;
                    i += 4;
                }
                while (i < ssmd_datacount)
                {
                    p[0] = s[0];
                    p[1] = s[0];
                    s += stride_s;
                    p += stride_d;
                    i++;
                }
                return;
            }
            if (destination_is_aligned)
            {
                // there is a high penalty when we need to index each array, especially for float 2 and 3 arrays
                float* p = &((float*)destination_indexbuffer[0])[destination_index];

                int stride_d = destination_index_rowsize >> 2;
                int stride_d_2 = stride_d + stride_d;
                int stride_d_3 = stride_d_2 + stride_d;
                int stride_d_4 = stride_d_3 + stride_d;

                int i = 0;
                float f;
                while (i < (ssmd_datacount & ~3))
                {
                    f = ((float*)source_indexbuffer[i])[source_index];
                    p[0] = f;
                    p[1] = f;
                    f = ((float*)source_indexbuffer[i + 1])[source_index];
                    p[stride_d] = f;
                    p[stride_d + 1] = f;
                    f = ((float*)source_indexbuffer[i + 2])[source_index];
                    p[stride_d_2] = f;
                    p[stride_d_2 + 1] = f;
                    f = ((float*)source_indexbuffer[i + 3])[source_index];
                    p[stride_d_3] = f;
                    p[stride_d_3 + 1] = f;
                    p += stride_d_4;
                    i += 4;
                }
                while (i < ssmd_datacount)
                {
                    p[0] = ((float*)source_indexbuffer[i])[source_index];
                    p[1] = ((float*)source_indexbuffer[i])[source_index];
                    p += stride_d;
                    i++;
                }
                return;
            }
            if (source_is_aligned)
            {
                float* s = &((float*)source_indexbuffer[0])[source_index];

                int stride_s = source_index_rowsize >> 2;
                int stride_s_2 = stride_s + stride_s;
                int stride_s_3 = stride_s_2 + stride_s;
                int stride_s_4 = stride_s_3 + stride_s;

                int i = 0;
                while (i < (ssmd_datacount & ~3))
                {
                    ((float*)destination_indexbuffer[i + 0])[destination_index] = s[0];
                    ((float*)destination_indexbuffer[i + 1])[destination_index] = s[stride_s];
                    ((float*)destination_indexbuffer[i + 2])[destination_index] = s[stride_s_2];
                    ((float*)destination_indexbuffer[i + 3])[destination_index] = s[stride_s_3];
                    ((float*)destination_indexbuffer[i + 0])[destination_index + 1] = s[0];
                    ((float*)destination_indexbuffer[i + 1])[destination_index + 1] = s[stride_s];
                    ((float*)destination_indexbuffer[i + 2])[destination_index + 1] = s[stride_s_2];
                    ((float*)destination_indexbuffer[i + 3])[destination_index + 1] = s[stride_s_3];
                    s += stride_s_4;
                    i += 4;
                }
                while (i < ssmd_datacount)
                {
                    ((float*)destination_indexbuffer[i])[destination_index] = s[0];
                    ((float*)destination_indexbuffer[i])[destination_index + 1] = s[0];
                    s += stride_s;
                    i++;
                }
                return;
            }
            for (int i = 0; i < ssmd_datacount; i++)
            {
                float f = ((float*)source_indexbuffer[i])[source_index];
                float* p = &((float*)destination_indexbuffer[i])[destination_index];
                p[0] = f;
                p[1] = f;
            }
        }


        /// <summary>
        /// data expansion of f1 negated to f2 in indexed data arrays: data[][].xy = -data[][].x
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public void move_indexed_f1f2_negated([NoAlias] void** destination_indexbuffer, int destination_index_rowsize, bool destination_is_aligned, int destination_index, [NoAlias] void** source_indexbuffer, int source_index_rowsize, bool source_is_aligned, int source_index, int ssmd_datacount)
        {
            if (destination_is_aligned && source_is_aligned)
            {
                float* s = &((float*)source_indexbuffer[0])[source_index];
                float* p = &((float*)destination_indexbuffer[0])[destination_index];

                int stride_s = source_index_rowsize >> 2;
                int stride_d = destination_index_rowsize >> 2;

                int stride_s_2 = stride_s + stride_s;
                int stride_d_2 = stride_d + stride_d;

                int stride_d_3 = stride_d_2 + stride_d;
                int stride_s_3 = stride_s_2 + stride_s;

                int stride_d_4 = stride_d_3 + stride_d;
                int stride_s_4 = stride_s_3 + stride_s;

                int i = 0;
                while (i < (ssmd_datacount & ~3))
                {
                    float f1 = -s[0];
                    float f2 = -s[stride_s];
                    float f3 = -s[stride_s_2];
                    float f4 = -s[stride_s_3];

                    p[0] = f1;
                    p[1] = f1;
                    p[stride_d] = f1;
                    p[stride_d + 1] = f1;
                    p[stride_d_2] = f3;
                    p[stride_d_2 + 1] = f3;
                    p[stride_d_3] = f4;
                    p[stride_d_3 + 1] = f4;

                    s += stride_s_4;
                    p += stride_d_4;
                    i += 4;
                }
                while (i < ssmd_datacount)
                {
                    float f = -s[0];
                    p[0] = f;
                    p[1] = f;
                    s += stride_s;
                    p += stride_d;
                    i++;
                }
                return;
            }
            if (destination_is_aligned)
            {
                // there is a high penalty when we need to index each array, especially for float 2 and 3 arrays
                float* p = &((float*)destination_indexbuffer[0])[destination_index];

                int stride_d = destination_index_rowsize >> 2;
                int stride_d_2 = stride_d + stride_d;
                int stride_d_3 = stride_d_2 + stride_d;
                int stride_d_4 = stride_d_3 + stride_d;

                int i = 0;
                float f;
                while (i < (ssmd_datacount & ~3))
                {
                    f = -((float*)source_indexbuffer[i])[source_index];
                    p[0] = f;
                    p[1] = f;
                    f = -((float*)source_indexbuffer[i + 1])[source_index];
                    p[stride_d] = f;
                    p[stride_d + 1] = f;
                    f = -((float*)source_indexbuffer[i + 2])[source_index];
                    p[stride_d_2] = f;
                    p[stride_d_2 + 1] = f;
                    f = -((float*)source_indexbuffer[i + 3])[source_index];
                    p[stride_d_3] = f;
                    p[stride_d_3 + 1] = f;
                    p += stride_d_4;
                    i += 4;
                }
                while (i < ssmd_datacount)
                {
                    f = -((float*)source_indexbuffer[i])[source_index];
                    p[0] = f;
                    p[1] = f;
                    p += stride_d;
                    i++;
                }
                return;
            }
            if (source_is_aligned)
            {
                float* s = &((float*)source_indexbuffer[0])[source_index];

                int stride_s = source_index_rowsize >> 2;
                int stride_s_2 = stride_s + stride_s;
                int stride_s_3 = stride_s_2 + stride_s;
                int stride_s_4 = stride_s_3 + stride_s;

                int i = 0;
                while (i < (ssmd_datacount & ~3))
                {
                    float f = -s[0];
                    float f2 = -s[stride_s];
                    float f3 = -s[stride_s_2];
                    float f4 = -s[stride_s_3];
                    ((float*)destination_indexbuffer[i + 0])[destination_index] = f;
                    ((float*)destination_indexbuffer[i + 0])[destination_index + 1] = f;
                    ((float*)destination_indexbuffer[i + 1])[destination_index] = f2;
                    ((float*)destination_indexbuffer[i + 1])[destination_index + 1] = f2;
                    ((float*)destination_indexbuffer[i + 2])[destination_index] = f3;
                    ((float*)destination_indexbuffer[i + 2])[destination_index + 1] = f3;
                    ((float*)destination_indexbuffer[i + 3])[destination_index] = f4;
                    ((float*)destination_indexbuffer[i + 3])[destination_index + 1] = f4;
                    s += stride_s_4;
                    i += 4;
                }
                while (i < ssmd_datacount)
                {
                    float f = -s[0];
                    ((float*)destination_indexbuffer[i])[destination_index] = f;
                    ((float*)destination_indexbuffer[i])[destination_index + 1] = f;
                    s += stride_s;
                    i++;
                }
                return;
            }
            for (int i = 0; i < ssmd_datacount; i++)
            {
                float f = -((float*)source_indexbuffer[i])[source_index];
                float* p = &((float*)destination_indexbuffer[i])[destination_index];
                p[0] = f;
                p[1] = f;
            }
        }

        /// <summary>
        /// data expansion of f1 to f3 in indexed data arrays: data[][].xyz = data[][].x
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public void move_indexed_f1f3(in DATAREC destination, [NoAlias] void** source_indexbuffer, int source_index_rowsize, bool source_is_aligned, int source_index, int ssmd_datacount)
        {
            move_indexed_f1f3(destination.data, destination.row_size, destination.is_aligned, destination.index, source_indexbuffer, source_index_rowsize, source_is_aligned, source_index, ssmd_datacount);
        }

        /// <summary>
        /// data expansion of f1 to f3 in indexed data arrays: data[][].xyz = -data[][].x
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public void move_indexed_f1f3_negated(in DATAREC destination, [NoAlias] void** source_indexbuffer, int source_index_rowsize, bool source_is_aligned, int source_index, int ssmd_datacount)
        {
            move_indexed_f1f3_negated(destination.data, destination.row_size, destination.is_aligned, destination.index, source_indexbuffer, source_index_rowsize, source_is_aligned, source_index, ssmd_datacount);
        }

        /// <summary>
        /// data expansion of f1 to f3 in indexed data arrays: data[][].xyz = data[][].x
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public void move_indexed_f1f3([NoAlias] void** destination_indexbuffer, int destination_index_rowsize, bool destination_is_aligned, int destination_index, [NoAlias] void** source_indexbuffer, int source_index_rowsize, bool source_is_aligned, int source_index, int ssmd_datacount)
        {
            if (destination_is_aligned && source_is_aligned)
            {
                float* s = &((float*)source_indexbuffer[0])[source_index];
                float* p = &((float*)destination_indexbuffer[0])[destination_index];

                int stride_s = source_index_rowsize >> 2;
                int stride_d = destination_index_rowsize >> 2;

                int stride_s_2 = stride_s + stride_s;
                int stride_d_2 = stride_d + stride_d;

                int stride_d_3 = stride_d_2 + stride_d;
                int stride_s_3 = stride_s_2 + stride_s;

                int stride_d_4 = stride_d_3 + stride_d;
                int stride_s_4 = stride_s_3 + stride_s;

                int i = 0;
                while (i < (ssmd_datacount & ~3))
                {
                    p[0] = s[0];
                    p[1] = s[0];
                    p[2] = s[0];
                    p[stride_d] = s[stride_s];
                    p[stride_d + 1] = s[stride_s];
                    p[stride_d + 2] = s[stride_s];
                    p[stride_d_2] = s[stride_s_2];
                    p[stride_d_2 + 1] = s[stride_s_2];
                    p[stride_d_2 + 2] = s[stride_s_2];
                    p[stride_d_3] = s[stride_s_3];
                    p[stride_d_3 + 1] = s[stride_s_3];
                    p[stride_d_3 + 2] = s[stride_s_3];

                    s += stride_s_4;
                    p += stride_d_4;
                    i += 4;
                }
                while (i < ssmd_datacount)
                {
                    p[0] = s[0];
                    p[1] = s[0];
                    p[2] = s[0];
                    s += stride_s;
                    p += stride_d;
                    i++;
                }
                return;
            }
            if (destination_is_aligned)
            {
                // there is a high penalty when we need to index each array, especially for float 2 and 3 arrays
                float* p = &((float*)destination_indexbuffer[0])[destination_index];

                int stride_d = destination_index_rowsize >> 2;
                int stride_d_2 = stride_d + stride_d;
                int stride_d_3 = stride_d_2 + stride_d;
                int stride_d_4 = stride_d_3 + stride_d;

                int i = 0;
                float f;
                while (i < (ssmd_datacount & ~3))
                {
                    f = ((float*)source_indexbuffer[i])[source_index];
                    p[0] = f;
                    p[1] = f;
                    p[2] = f;
                    f = ((float*)source_indexbuffer[i + 1])[source_index];
                    p[stride_d] = f;
                    p[stride_d + 1] = f;
                    p[stride_d + 2] = f;
                    f = ((float*)source_indexbuffer[i + 2])[source_index];
                    p[stride_d_2] = f;
                    p[stride_d_2 + 1] = f;
                    p[stride_d_2 + 2] = f;
                    f = ((float*)source_indexbuffer[i + 3])[source_index];
                    p[stride_d_3] = f;
                    p[stride_d_3 + 1] = f;
                    p[stride_d_3 + 2] = f;
                    p += stride_d_4;
                    i += 4;
                }
                while (i < ssmd_datacount)
                {
                    p[0] = ((float*)source_indexbuffer[i])[source_index];
                    p[1] = ((float*)source_indexbuffer[i])[source_index];
                    p[2] = ((float*)source_indexbuffer[i])[source_index];
                    p += stride_d;
                    i++;
                }
                return;
            }
            if (source_is_aligned)
            {
                float* s = &((float*)source_indexbuffer[0])[source_index];

                int stride_s = source_index_rowsize >> 2;
                int stride_s_2 = stride_s + stride_s;
                int stride_s_3 = stride_s_2 + stride_s;
                int stride_s_4 = stride_s_3 + stride_s;

                int i = 0;
                while (i < (ssmd_datacount & ~3))
                {
                    ((float*)destination_indexbuffer[i + 0])[destination_index] = s[0];
                    ((float*)destination_indexbuffer[i + 0])[destination_index + 1] = s[0];
                    ((float*)destination_indexbuffer[i + 0])[destination_index + 2] = s[0];
                    ((float*)destination_indexbuffer[i + 1])[destination_index] = s[stride_s];
                    ((float*)destination_indexbuffer[i + 1])[destination_index + 1] = s[stride_s];
                    ((float*)destination_indexbuffer[i + 1])[destination_index + 2] = s[stride_s];
                    ((float*)destination_indexbuffer[i + 2])[destination_index] = s[stride_s_2];
                    ((float*)destination_indexbuffer[i + 2])[destination_index + 1] = s[stride_s_2];
                    ((float*)destination_indexbuffer[i + 2])[destination_index + 2] = s[stride_s_2];
                    ((float*)destination_indexbuffer[i + 3])[destination_index] = s[stride_s_3];
                    ((float*)destination_indexbuffer[i + 3])[destination_index + 1] = s[stride_s_3];
                    ((float*)destination_indexbuffer[i + 3])[destination_index + 2] = s[stride_s_3];
                    s += stride_s_4;
                    i += 4;
                }
                while (i < ssmd_datacount)
                {
                    ((float*)destination_indexbuffer[i])[destination_index] = s[0];
                    ((float*)destination_indexbuffer[i])[destination_index + 1] = s[0];
                    ((float*)destination_indexbuffer[i])[destination_index + 2] = s[0];
                    s += stride_s;
                    i++;
                }
                return;
            }
            for (int i = 0; i < ssmd_datacount; i++)
            {
                float f = ((float*)source_indexbuffer[i])[source_index];
                float* p = &((float*)destination_indexbuffer[i])[destination_index];
                p[0] = f;
                p[1] = f;
                p[2] = f;
            }
        }

        /// <summary>
        /// data expansion of f1 negated to f3 in indexed data arrays: data[][].xyzw = -data[][].x
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public void move_indexed_f1f3_negated([NoAlias] void** destination_indexbuffer, int destination_index_rowsize, bool destination_is_aligned, int destination_index, [NoAlias] void** source_indexbuffer, int source_index_rowsize, bool source_is_aligned, int source_index, int ssmd_datacount)
        {
            if (destination_is_aligned && source_is_aligned)
            {
                float* s = &((float*)source_indexbuffer[0])[source_index];
                float* p = &((float*)destination_indexbuffer[0])[destination_index];

                int stride_s = source_index_rowsize >> 2;
                int stride_d = destination_index_rowsize >> 2;

                int stride_s_2 = stride_s + stride_s;
                int stride_d_2 = stride_d + stride_d;

                int stride_d_3 = stride_d_2 + stride_d;
                int stride_s_3 = stride_s_2 + stride_s;

                int stride_d_4 = stride_d_3 + stride_d;
                int stride_s_4 = stride_s_3 + stride_s;

                int i = 0;
                while (i < (ssmd_datacount & ~3))
                {
                    float f1 = -s[0];
                    float f2 = -s[stride_s];
                    float f3 = -s[stride_s_2];
                    float f4 = -s[stride_s_3];

                    p[0] = f1;
                    p[1] = f1;
                    p[2] = f1;
                    p[stride_d] = f1;
                    p[stride_d + 1] = f1;
                    p[stride_d + 2] = f1;
                    p[stride_d_2] = f3;
                    p[stride_d_2 + 1] = f3;
                    p[stride_d_2 + 2] = f3;
                    p[stride_d_3] = f4;
                    p[stride_d_3 + 1] = f4;
                    p[stride_d_3 + 2] = f4;

                    s += stride_s_4;
                    p += stride_d_4;
                    i += 4;
                }
                while (i < ssmd_datacount)
                {
                    float f = -s[0];
                    p[0] = f;
                    p[1] = f;
                    p[2] = f;
                    s += stride_s;
                    p += stride_d;
                    i++;
                }
                return;
            }
            if (destination_is_aligned)
            {
                // there is a high penalty when we need to index each array, especially for float 2 and 3 arrays
                float* p = &((float*)destination_indexbuffer[0])[destination_index];

                int stride_d = destination_index_rowsize >> 2;
                int stride_d_2 = stride_d + stride_d;
                int stride_d_3 = stride_d_2 + stride_d;
                int stride_d_4 = stride_d_3 + stride_d;

                int i = 0;
                float f;
                while (i < (ssmd_datacount & ~3))
                {
                    f = -((float*)source_indexbuffer[i])[source_index];
                    p[0] = f;
                    p[1] = f;
                    p[2] = f;
                    f = -((float*)source_indexbuffer[i + 1])[source_index];
                    p[stride_d] = f;
                    p[stride_d + 1] = f;
                    p[stride_d + 2] = f;
                    f = -((float*)source_indexbuffer[i + 2])[source_index];
                    p[stride_d_2] = f;
                    p[stride_d_2 + 1] = f;
                    p[stride_d_2 + 2] = f;
                    f = -((float*)source_indexbuffer[i + 3])[source_index];
                    p[stride_d_3] = f;
                    p[stride_d_3 + 1] = f;
                    p[stride_d_3 + 2] = f;
                    p += stride_d_4;
                    i += 4;
                }
                while (i < ssmd_datacount)
                {
                    f = -((float*)source_indexbuffer[i])[source_index];
                    p[0] = f;
                    p[1] = f;
                    p[2] = f;
                    p += stride_d;
                    i++;
                }
                return;
            }
            if (source_is_aligned)
            {
                float* s = &((float*)source_indexbuffer[0])[source_index];

                int stride_s = source_index_rowsize >> 2;
                int stride_s_2 = stride_s + stride_s;
                int stride_s_3 = stride_s_2 + stride_s;
                int stride_s_4 = stride_s_3 + stride_s;

                int i = 0;
                while (i < (ssmd_datacount & ~3))
                {
                    float f = -s[0];
                    float f2 = -s[stride_s];
                    float f3 = -s[stride_s_2];
                    float f4 = -s[stride_s_3];
                    ((float*)destination_indexbuffer[i + 0])[destination_index] = f;
                    ((float*)destination_indexbuffer[i + 0])[destination_index + 1] = f;
                    ((float*)destination_indexbuffer[i + 0])[destination_index + 2] = f;
                    ((float*)destination_indexbuffer[i + 1])[destination_index] = f2;
                    ((float*)destination_indexbuffer[i + 1])[destination_index + 1] = f2;
                    ((float*)destination_indexbuffer[i + 1])[destination_index + 2] = f2;
                    ((float*)destination_indexbuffer[i + 2])[destination_index] = f3;
                    ((float*)destination_indexbuffer[i + 2])[destination_index + 1] = f3;
                    ((float*)destination_indexbuffer[i + 2])[destination_index + 2] = f3;
                    ((float*)destination_indexbuffer[i + 3])[destination_index] = f4;
                    ((float*)destination_indexbuffer[i + 3])[destination_index + 1] = f4;
                    ((float*)destination_indexbuffer[i + 3])[destination_index + 2] = f4;
                    s += stride_s_4;
                    i += 4;
                }
                while (i < ssmd_datacount)
                {
                    float f = -s[0];
                    ((float*)destination_indexbuffer[i])[destination_index] = f;
                    ((float*)destination_indexbuffer[i])[destination_index + 1] = f;
                    ((float*)destination_indexbuffer[i])[destination_index + 2] = f;
                    s += stride_s;
                    i++;
                }
                return;
            }
            for (int i = 0; i < ssmd_datacount; i++)
            {
                float f = -((float*)source_indexbuffer[i])[source_index];
                float* p = &((float*)destination_indexbuffer[i])[destination_index];
                p[0] = f;
                p[1] = f;
                p[2] = f;
            }
        }

        /// <summary>
        /// data expansion of f1 to f4 in indexed data arrays: data[][].xyzw = data[][].x
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public void move_indexed_f1f4(in DATAREC destination, [NoAlias] void** source_indexbuffer, int source_index_rowsize, bool source_is_aligned, int source_index, int ssmd_datacount)
        {
            move_indexed_f1f4(destination.data, destination.row_size, destination.is_aligned, destination.index, source_indexbuffer, source_index_rowsize, source_is_aligned, source_index, ssmd_datacount);
        }

        /// <summary>
        /// data expansion of f1 to f4 in indexed data arrays: data[][].xyzw = data[][].x
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public void move_indexed_f1f4_negated(in DATAREC destination, [NoAlias] void** source_indexbuffer, int source_index_rowsize, bool source_is_aligned, int source_index, int ssmd_datacount)
        {
            move_indexed_f1f4_negated(destination.data, destination.row_size, destination.is_aligned, destination.index, source_indexbuffer, source_index_rowsize, source_is_aligned, source_index, ssmd_datacount);
        }

        /// <summary>
        /// data expansion of f1 to f4 in indexed data arrays: data[][].xyzw = data[][].x
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public void move_indexed_f1f4([NoAlias] void** destination_indexbuffer, int destination_index_rowsize, bool destination_is_aligned, int destination_index, [NoAlias] void** source_indexbuffer, int source_index_rowsize, bool source_is_aligned, int source_index, int ssmd_datacount)
        {
            if (destination_is_aligned && source_is_aligned)
            {
                float* s = &((float*)source_indexbuffer[0])[source_index];
                float* p = &((float*)destination_indexbuffer[0])[destination_index];

                int stride_s = source_index_rowsize >> 2;
                int stride_d = destination_index_rowsize >> 2;

                int stride_s_2 = stride_s + stride_s;
                int stride_d_2 = stride_d + stride_d;

                int stride_d_3 = stride_d_2 + stride_d;
                int stride_s_3 = stride_s_2 + stride_s;

                int stride_d_4 = stride_d_3 + stride_d;
                int stride_s_4 = stride_s_3 + stride_s;

                int i = 0;
                while (i < (ssmd_datacount & ~3))
                {
                    p[0] = s[0];
                    p[1] = s[0];
                    p[2] = s[0];
                    p[3] = s[0];
                    p[stride_d] = s[stride_s];
                    p[stride_d + 1] = s[stride_s];
                    p[stride_d + 2] = s[stride_s];
                    p[stride_d + 3] = s[stride_s];
                    p[stride_d_2] = s[stride_s_2];
                    p[stride_d_2 + 1] = s[stride_s_2];
                    p[stride_d_2 + 2] = s[stride_s_2];
                    p[stride_d_2 + 3] = s[stride_s_2];
                    p[stride_d_3] = s[stride_s_3];
                    p[stride_d_3 + 1] = s[stride_s_3];
                    p[stride_d_3 + 2] = s[stride_s_3];
                    p[stride_d_3 + 3] = s[stride_s_3];

                    s += stride_s_4;
                    p += stride_d_4;
                    i += 4;
                }
                while (i < ssmd_datacount)
                {
                    p[0] = s[0];
                    p[1] = s[0];
                    p[2] = s[0];
                    p[3] = s[0];
                    s += stride_s;
                    p += stride_d;
                    i++;
                }
                return;
            }
            if (destination_is_aligned)
            {
                // there is a high penalty when we need to index each array, especially for float 2 and 3 arrays
                float* p = &((float*)destination_indexbuffer[0])[destination_index];

                int stride_d = destination_index_rowsize >> 2;
                int stride_d_2 = stride_d + stride_d;
                int stride_d_3 = stride_d_2 + stride_d;
                int stride_d_4 = stride_d_3 + stride_d;

                int i = 0;
                float f;
                while (i < (ssmd_datacount & ~3))
                {
                    f = ((float*)source_indexbuffer[i])[source_index];
                    p[0] = f;
                    p[1] = f;
                    p[2] = f;
                    p[3] = f;
                    f = ((float*)source_indexbuffer[i + 1])[source_index];
                    p[stride_d] = f;
                    p[stride_d + 1] = f;
                    p[stride_d + 2] = f;
                    p[stride_d + 3] = f;
                    f = ((float*)source_indexbuffer[i + 2])[source_index];
                    p[stride_d_2] = f;
                    p[stride_d_2 + 1] = f;
                    p[stride_d_2 + 2] = f;
                    p[stride_d_2 + 3] = f;
                    f = ((float*)source_indexbuffer[i + 3])[source_index];
                    p[stride_d_3] = f;
                    p[stride_d_3 + 1] = f;
                    p[stride_d_3 + 2] = f;
                    p[stride_d_3 + 3] = f;
                    p += stride_d_4;
                    i += 4;
                }
                while (i < ssmd_datacount)
                {
                    p[0] = ((float*)source_indexbuffer[i])[source_index];
                    p[1] = ((float*)source_indexbuffer[i])[source_index];
                    p[2] = ((float*)source_indexbuffer[i])[source_index];
                    p[3] = ((float*)source_indexbuffer[i])[source_index];
                    p += stride_d;
                    i++;
                }
                return;
            }
            if (source_is_aligned)
            {
                float* s = &((float*)source_indexbuffer[0])[source_index];

                int stride_s = source_index_rowsize >> 2;
                int stride_s_2 = stride_s + stride_s;
                int stride_s_3 = stride_s_2 + stride_s;
                int stride_s_4 = stride_s_3 + stride_s;

                int i = 0;
                while (i < (ssmd_datacount & ~3))
                {
                    ((float*)destination_indexbuffer[i + 0])[destination_index] = s[0];
                    ((float*)destination_indexbuffer[i + 0])[destination_index + 1] = s[0];
                    ((float*)destination_indexbuffer[i + 0])[destination_index + 2] = s[0];
                    ((float*)destination_indexbuffer[i + 0])[destination_index + 3] = s[0];
                    ((float*)destination_indexbuffer[i + 1])[destination_index] = s[stride_s];
                    ((float*)destination_indexbuffer[i + 1])[destination_index + 1] = s[stride_s];
                    ((float*)destination_indexbuffer[i + 1])[destination_index + 2] = s[stride_s];
                    ((float*)destination_indexbuffer[i + 1])[destination_index + 3] = s[stride_s];
                    ((float*)destination_indexbuffer[i + 2])[destination_index] = s[stride_s_2];
                    ((float*)destination_indexbuffer[i + 2])[destination_index + 1] = s[stride_s_2];
                    ((float*)destination_indexbuffer[i + 2])[destination_index + 2] = s[stride_s_2];
                    ((float*)destination_indexbuffer[i + 2])[destination_index + 3] = s[stride_s_2];
                    ((float*)destination_indexbuffer[i + 3])[destination_index] = s[stride_s_3];
                    ((float*)destination_indexbuffer[i + 3])[destination_index + 1] = s[stride_s_3];
                    ((float*)destination_indexbuffer[i + 3])[destination_index + 2] = s[stride_s_3];
                    ((float*)destination_indexbuffer[i + 3])[destination_index + 3] = s[stride_s_3];
                    s += stride_s_4;
                    i += 4;
                }
                while (i < ssmd_datacount)
                {
                    ((float*)destination_indexbuffer[i])[destination_index] = s[0];
                    ((float*)destination_indexbuffer[i])[destination_index + 1] = s[0];
                    ((float*)destination_indexbuffer[i])[destination_index + 2] = s[0];
                    ((float*)destination_indexbuffer[i])[destination_index + 3] = s[0];
                    s += stride_s;
                    i++;
                }
                return;
            }
            for (int i = 0; i < ssmd_datacount; i++)
            {
                float f = ((float*)source_indexbuffer[i])[source_index];
                float* p = &((float*)destination_indexbuffer[i])[destination_index];
                p[0] = f;
                p[1] = f;
                p[2] = f;
                p[3] = f;
            }
        }

        /// <summary>
        /// data expansion of f1 negated to f4 in indexed data arrays: data[][].xyzw = -data[][].x
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public void move_indexed_f1f4_negated([NoAlias] void** destination_indexbuffer, int destination_index_rowsize, bool destination_is_aligned, int destination_index, [NoAlias] void** source_indexbuffer, int source_index_rowsize, bool source_is_aligned, int source_index, int ssmd_datacount)
        {
            if (destination_is_aligned && source_is_aligned)
            {
                float* s = &((float*)source_indexbuffer[0])[source_index];
                float* p = &((float*)destination_indexbuffer[0])[destination_index];

                int stride_s = source_index_rowsize >> 2;
                int stride_d = destination_index_rowsize >> 2;

                int stride_s_2 = stride_s + stride_s;
                int stride_d_2 = stride_d + stride_d;

                int stride_d_3 = stride_d_2 + stride_d;
                int stride_s_3 = stride_s_2 + stride_s;

                int stride_d_4 = stride_d_3 + stride_d;
                int stride_s_4 = stride_s_3 + stride_s;

                int i = 0;
                while (i < (ssmd_datacount & ~3))
                {
                    float f1 = -s[0];
                    float f2 = -s[stride_s];
                    float f3 = -s[stride_s_2];
                    float f4 = -s[stride_s_3];

                    p[0] = f1;
                    p[1] = f1;
                    p[2] = f1;
                    p[3] = f1;  
                    p[stride_d]     = f1;
                    p[stride_d + 1] = f1;
                    p[stride_d + 2] = f1;
                    p[stride_d + 3] = f1;
                    p[stride_d_2]     = f3;
                    p[stride_d_2 + 1] = f3;
                    p[stride_d_2 + 2] = f3;
                    p[stride_d_2 + 3] = f3;
                    p[stride_d_3]     = f4;
                    p[stride_d_3 + 1] = f4;
                    p[stride_d_3 + 2] = f4;
                    p[stride_d_3 + 3] = f4;

                    s += stride_s_4;
                    p += stride_d_4;
                    i += 4;
                }
                while (i < ssmd_datacount)
                {
                    float f = -s[0];
                    p[0] = f;
                    p[1] = f;
                    p[2] = f;
                    p[3] = f;
                    s += stride_s;
                    p += stride_d;
                    i++;
                }
                return;
            }
            if (destination_is_aligned)
            {
                // there is a high penalty when we need to index each array, especially for float 2 and 3 arrays
                float* p = &((float*)destination_indexbuffer[0])[destination_index];

                int stride_d = destination_index_rowsize >> 2;
                int stride_d_2 = stride_d + stride_d;
                int stride_d_3 = stride_d_2 + stride_d;
                int stride_d_4 = stride_d_3 + stride_d;

                int i = 0;
                float f;
                while (i < (ssmd_datacount & ~3))
                {
                    f = -((float*)source_indexbuffer[i])[source_index];
                    p[0] = f;
                    p[1] = f;
                    p[2] = f;
                    p[3] = f;
                    f = -((float*)source_indexbuffer[i + 1])[source_index];
                    p[stride_d] = f;
                    p[stride_d + 1] = f;
                    p[stride_d + 2] = f;
                    p[stride_d + 3] = f;
                    f = -((float*)source_indexbuffer[i + 2])[source_index];
                    p[stride_d_2] = f;
                    p[stride_d_2 + 1] = f;
                    p[stride_d_2 + 2] = f;
                    p[stride_d_2 + 3] = f;
                    f = -((float*)source_indexbuffer[i + 3])[source_index];
                    p[stride_d_3] = f;
                    p[stride_d_3 + 1] = f;
                    p[stride_d_3 + 2] = f;
                    p[stride_d_3 + 3] = f;
                    p += stride_d_4;
                    i += 4;
                }
                while (i < ssmd_datacount)
                {
                    f = -((float*)source_indexbuffer[i])[source_index];
                    p[0] = f;
                    p[1] = f; 
                    p[2] = f; 
                    p[3] = f; 
                    p += stride_d;
                    i++;
                }
                return;
            }
            if (source_is_aligned)
            {
                float* s = &((float*)source_indexbuffer[0])[source_index];

                int stride_s = source_index_rowsize >> 2;
                int stride_s_2 = stride_s + stride_s;
                int stride_s_3 = stride_s_2 + stride_s;
                int stride_s_4 = stride_s_3 + stride_s;

                int i = 0;
                while (i < (ssmd_datacount & ~3))
                {
                    float f = -s[0];
                    float f2 = -s[stride_s];
                    float f3 = -s[stride_s_2];
                    float f4 = -s[stride_s_3];
                    ((float*)destination_indexbuffer[i + 0])[destination_index]     = f;
                    ((float*)destination_indexbuffer[i + 0])[destination_index + 1] = f;
                    ((float*)destination_indexbuffer[i + 0])[destination_index + 2] = f;
                    ((float*)destination_indexbuffer[i + 0])[destination_index + 3] = f;
                    ((float*)destination_indexbuffer[i + 1])[destination_index] = f2;
                    ((float*)destination_indexbuffer[i + 1])[destination_index + 1] = f2;
                    ((float*)destination_indexbuffer[i + 1])[destination_index + 2] = f2;
                    ((float*)destination_indexbuffer[i + 1])[destination_index + 3] = f2;
                    ((float*)destination_indexbuffer[i + 2])[destination_index]     = f3;
                    ((float*)destination_indexbuffer[i + 2])[destination_index + 1] = f3;
                    ((float*)destination_indexbuffer[i + 2])[destination_index + 2] = f3;
                    ((float*)destination_indexbuffer[i + 2])[destination_index + 3] = f3;
                    ((float*)destination_indexbuffer[i + 3])[destination_index]     = f4;
                    ((float*)destination_indexbuffer[i + 3])[destination_index + 1] = f4;
                    ((float*)destination_indexbuffer[i + 3])[destination_index + 2] = f4;
                    ((float*)destination_indexbuffer[i + 3])[destination_index + 3] = f4;
                    s += stride_s_4;
                    i += 4;
                }
                while (i < ssmd_datacount)
                {
                    float f = -s[0];
                    ((float*)destination_indexbuffer[i])[destination_index] = f;
                    ((float*)destination_indexbuffer[i])[destination_index + 1] = f;
                    ((float*)destination_indexbuffer[i])[destination_index + 2] = f;
                    ((float*)destination_indexbuffer[i])[destination_index + 3] = f;
                    s += stride_s;
                    i++;
                }
                return;
            }
            for (int i = 0; i < ssmd_datacount; i++)
            {
                float f = -((float*)source_indexbuffer[i])[source_index];
                float* p = &((float*)destination_indexbuffer[i])[destination_index];
                p[0] = f;
                p[1] = f;
                p[2] = f;
                p[3] = f;
            }
        }


        /// <summary>
        /// void[][].xy = void[][].xy
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public void move_indexed_f2([NoAlias] void** destination_indexbuffer, int destination_index_rowsize, bool destination_is_aligned, int destination_index, [NoAlias] void** source_indexbuffer, int source_index_rowsize, bool source_is_aligned, int source_index, int ssmd_datacount)
        {
            if (destination_is_aligned && source_is_aligned)
            {
                float* s = &((float*)source_indexbuffer[0])[source_index];
                float* p = &((float*)destination_indexbuffer[0])[destination_index];

                int stride_s = source_index_rowsize >> 2;
                int stride_d = destination_index_rowsize >> 2;

                int stride_s_2 = stride_s + stride_s;
                int stride_d_2 = stride_d + stride_d;

                int stride_d_3 = stride_d_2 + stride_d;
                int stride_s_3 = stride_s_2 + stride_s;

                int stride_d_4 = stride_d_3 + stride_d;
                int stride_s_4 = stride_s_3 + stride_s;

                int i = 0;
                while (i < (ssmd_datacount & ~3))
                {
                    p[0] = s[0];
                    p[1] = s[1];
                    p[stride_d] = s[stride_s];
                    p[stride_d + 1] = s[stride_s + 1];
                    p[stride_d_2] = s[stride_s_2];
                    p[stride_d_2 + 1] = s[stride_s_2 + 1];
                    p[stride_d_3] = s[stride_s_3];
                    p[stride_d_3 + 1] = s[stride_s_3 + 1];

                    s += stride_s_4;
                    p += stride_d_4;
                    i += 4;
                }
                while (i < ssmd_datacount)
                {
                    p[0] = s[0];
                    p[1] = s[1];
                    s += stride_s;
                    p += stride_d;
                    i++;
                }

                return;
            }
            if (destination_is_aligned)
            {
                float* p = &((float*)destination_indexbuffer[0])[destination_index];

                int stride_d = destination_index_rowsize >> 2;
                int stride_d_2 = stride_d + stride_d;
                int stride_d_3 = stride_d_2 + stride_d;
                int stride_d_4 = stride_d_3 + stride_d;

                int i = 0;
                while (i < (ssmd_datacount & ~3))
                {
                    ((float2*)p)[0] = ((float2*)(void*)&((float*)source_indexbuffer[i])[source_index])[0];

                    ((float2*)(void*)&p[stride_d])[0] = ((float2*)(void*)&((float*)source_indexbuffer[i + 1])[source_index])[0];
                    ((float2*)(void*)&p[stride_d_2])[0] = ((float2*)(void*)&((float*)source_indexbuffer[i + 2])[source_index])[0];
                    ((float2*)(void*)&p[stride_d_3])[0] = ((float2*)(void*)&((float*)source_indexbuffer[i + 3])[source_index])[0];

                    p += stride_d_4;
                    i += 4;
                }
                while (i < ssmd_datacount)
                {
                    ((float2*)p)[0] = ((float2*)(void*)&((float*)source_indexbuffer[i])[source_index])[0];
                    p += stride_d;
                    i++;
                }

                return;
            }
            if (source_is_aligned)
            {
                float* s = &((float*)source_indexbuffer[0])[source_index];

                int stride_s = source_index_rowsize >> 2;
                int stride_s_2 = stride_s + stride_s;
                int stride_s_3 = stride_s_2 + stride_s;
                int stride_s_4 = stride_s_3 + stride_s;

                int i = 0;
                float2 f2_1, f2_2, f2_3, f2_4;
                while (i < (ssmd_datacount & ~3))
                {
                    f2_1.x = s[0];
                    f2_1.y = s[1];

                    f2_2.x = s[stride_s];
                    f2_2.y = s[stride_s + 1];

                    f2_3.x = s[stride_s_2];
                    f2_3.y = s[stride_s_2 + 1];

                    f2_4.x = s[stride_s_3];
                    f2_4.y = s[stride_s_3 + 1];

                    ((float2*)(void*)&((float*)destination_indexbuffer[i + 0])[destination_index])[0] = f2_1;
                    ((float2*)(void*)&((float*)destination_indexbuffer[i + 1])[destination_index])[0] = f2_2;
                    ((float2*)(void*)&((float*)destination_indexbuffer[i + 2])[destination_index])[0] = f2_3;
                    ((float2*)(void*)&((float*)destination_indexbuffer[i + 3])[destination_index])[0] = f2_4;

                    s += stride_s_4;
                    i += 4;
                }
                while (i < ssmd_datacount)
                {
                    f2_1.x = s[0];
                    f2_1.y = s[1];
                    ((float2*)(void*)&((float*)destination_indexbuffer[i + 0])[destination_index])[0] = f2_1;
                    s += stride_s;
                    i++;
                }

                return;
            }

            for (int i = 0; i < ssmd_datacount; i++)
            {
                ((float2*)(void*)&((float*)destination_indexbuffer[i])[destination_index])[0] = ((float2*)(void*)&((float*)source_indexbuffer[i])[source_index])[0];
            }
        }

        /// <summary>
        /// void[][].xy = void[][].xy
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public void move_indexed_f3([NoAlias] void** destination_indexbuffer, int destination_index_rowsize, bool destination_is_aligned, int destination_index, [NoAlias] void** source_indexbuffer, int source_index_rowsize, bool source_is_aligned, int source_index, int ssmd_datacount)
        {
            if (destination_is_aligned && source_is_aligned)
            {
                float* s = &((float*)source_indexbuffer[0])[source_index];
                float* p = &((float*)destination_indexbuffer[0])[destination_index];

                int stride_s = source_index_rowsize >> 2;
                int stride_d = destination_index_rowsize >> 2;


                int i = 0;
                while (i < (ssmd_datacount & ~3))
                {
                    p[0] = s[0];
                    p[1] = s[1];
                    p[2] = s[2];
                    s += stride_s;
                    p += stride_d;
                    p[0] = s[0];
                    p[1] = s[1];
                    p[2] = s[2];
                    s += stride_s;
                    p += stride_d;
                    p[0] = s[0];
                    p[1] = s[1];
                    p[2] = s[2];
                    s += stride_s;
                    p += stride_d;
                    p[0] = s[0];
                    p[1] = s[1];
                    p[2] = s[2];
                    s += stride_s;
                    p += stride_d;
                    i += 4;
                }
                while (i < ssmd_datacount)
                {
                    p[0] = s[0];
                    p[1] = s[1];
                    p[2] = s[2];
                    s += stride_s;
                    p += stride_d;
                    i++;
                }
                return;
            }
            if (destination_is_aligned)
            {
                float* p = &((float*)destination_indexbuffer[0])[destination_index];

                int stride_d = destination_index_rowsize >> 2;

                int i = 0;
                while (i < (ssmd_datacount & ~3))
                {
                    float3 f3 = ((float3*)(void*)&((float*)source_indexbuffer[i + 0])[source_index])[0];
                    p[0] = f3[0];
                    p[1] = f3[1];
                    p[2] = f3[2];
                    f3 = ((float3*)(void*)&((float*)source_indexbuffer[i + 1])[source_index])[0];
                    p += stride_d;
                    p[0] = f3[0];
                    p[1] = f3[1];
                    p[2] = f3[2];
                    f3 = ((float3*)(void*)&((float*)source_indexbuffer[i + 2])[source_index])[0];
                    p += stride_d;
                    p[0] = f3[0];
                    p[1] = f3[1];
                    p[2] = f3[2];
                    f3 = ((float3*)(void*)&((float*)source_indexbuffer[i + 3])[source_index])[0];
                    p += stride_d;
                    p[0] = f3[0];
                    p[1] = f3[1];
                    p[2] = f3[2];
                    p += stride_d;
                    i += 4;
                }
                while (i < ssmd_datacount)
                {
                    float3 f3 = ((float3*)(void*)&((float*)source_indexbuffer[i + 0])[source_index])[0];
                    p[0] = f3[0];
                    p[1] = f3[1];
                    p[2] = f3[2];
                    p += stride_d;
                    i++;
                }
                return;
            }
            if (source_is_aligned)
            {
                float* s = &((float*)source_indexbuffer[0])[source_index];

                int stride_s = source_index_rowsize >> 2;
                int i = 0;
                float3 f3 = 0;
                while (i < (ssmd_datacount & ~3))
                {
                    f3[0] = s[0];
                    f3[1] = s[1];
                    f3[2] = s[2];
                    ((float3*)(void*)&((float*)destination_indexbuffer[i + 0])[destination_index])[0] = f3;
                    s += stride_s;
                    f3[0] = s[0];
                    f3[1] = s[1];
                    f3[2] = s[2];
                    s += stride_s;
                    ((float3*)(void*)&((float*)destination_indexbuffer[i + 1])[destination_index])[0] = f3;
                    f3[0] = s[0];
                    f3[1] = s[1];
                    f3[2] = s[2];
                    s += stride_s;
                    ((float3*)(void*)&((float*)destination_indexbuffer[i + 2])[destination_index])[0] = f3;
                    f3[0] = s[0];
                    f3[1] = s[1];
                    f3[2] = s[2];
                    s += stride_s;
                    ((float3*)(void*)&((float*)destination_indexbuffer[i + 3])[destination_index])[0] = f3;
                    i += 4;
                }
                while (i < ssmd_datacount)
                {
                    f3[0] = s[0];
                    f3[1] = s[1];
                    f3[2] = s[2];
                    ((float3*)(void*)&((float*)destination_indexbuffer[i])[destination_index])[0] = f3;
                    s += stride_s;
                    i++;
                }

                return;
            }
            for (int i = 0; i < ssmd_datacount; i++)
            {
                ((float3*)(void*)&((float*)destination_indexbuffer[i])[destination_index])[0] = ((float3*)(void*)&((float*)source_indexbuffer[i])[source_index])[0];
            }
        }

        /// <summary>
        /// [][].xyzw = [][].xyzw
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public void move_indexed_f4([NoAlias] void** destination_indexbuffer, int destination_index_rowsize, bool destination_is_aligned, int destination_index, [NoAlias] void** source_indexbuffer, int source_index_rowsize, bool source_is_aligned, int source_index, int ssmd_datacount)
        {
            if (destination_is_aligned && source_is_aligned)
            {
                float* s = &((float*)source_indexbuffer[0])[source_index];
                float* p = &((float*)destination_indexbuffer[0])[destination_index];

                int stride_s = source_index_rowsize >> 2;
                int stride_d = destination_index_rowsize >> 2;


                int i = 0;
                while (i < (ssmd_datacount & ~3))
                {
                    p[0] = s[0];
                    p[1] = s[1];
                    p[2] = s[2];
                    p[3] = s[3];
                    s += stride_s;
                    p += stride_d;
                    p[0] = s[0];
                    p[1] = s[1];
                    p[2] = s[2];
                    p[3] = s[3];
                    s += stride_s;
                    p += stride_d;
                    p[0] = s[0];
                    p[1] = s[1];
                    p[2] = s[2];
                    p[3] = s[3];
                    s += stride_s;
                    p += stride_d;
                    p[0] = s[0];
                    p[1] = s[1];
                    p[2] = s[2];
                    p[3] = s[3];
                    s += stride_s;
                    p += stride_d;
                    i += 4;
                }
                while (i < ssmd_datacount)
                {
                    p[0] = s[0];
                    p[1] = s[1];
                    p[2] = s[2];
                    p[3] = s[3];
                    s += stride_s;
                    p += stride_d;
                    i++;
                }

                return;
            }
            if (source_is_aligned)
            {
                float* s = &((float*)source_indexbuffer[0])[source_index];

                int stride_s = source_index_rowsize >> 2;
                int i = 0;
                float4 f4 = 0;
                while (i < (ssmd_datacount & ~3))
                {
                    f4[0] = s[0];
                    f4[1] = s[1];
                    f4[2] = s[2];
                    f4[3] = s[3];
                    ((float4*)(void*)&((float*)destination_indexbuffer[i + 0])[destination_index])[0] = f4;
                    s += stride_s;
                    f4[0] = s[0];
                    f4[1] = s[1];
                    f4[2] = s[2];
                    f4[3] = s[3];
                    s += stride_s;
                    ((float4*)(void*)&((float*)destination_indexbuffer[i + 1])[destination_index])[0] = f4;
                    f4[0] = s[0];
                    f4[1] = s[1];
                    f4[2] = s[2];
                    f4[3] = s[3];
                    s += stride_s;
                    ((float4*)(void*)&((float*)destination_indexbuffer[i + 2])[destination_index])[0] = f4;
                    f4[0] = s[0];
                    f4[1] = s[1];
                    f4[2] = s[2];
                    f4[3] = s[3];
                    s += stride_s;
                    ((float4*)(void*)&((float*)destination_indexbuffer[i + 3])[destination_index])[0] = f4;
                    i += 4;
                }
                while (i < ssmd_datacount)
                {
                    f4[0] = s[0];
                    f4[1] = s[1];
                    f4[2] = s[2];
                    f4[3] = s[3];
                    ((float4*)(void*)&((float*)destination_indexbuffer[i])[destination_index])[0] = f4;
                    s += stride_s;
                    i++;
                }

                return;
            }
            if (destination_is_aligned)
            {
                float* p = &((float*)destination_indexbuffer[0])[destination_index];
                int stride_d = destination_index_rowsize >> 2;
                int i = 0;
                float4 f4;
                while (i < (ssmd_datacount & ~3))
                {
                    f4 = ((float4*)(void*)&((float*)source_indexbuffer[i + 0])[source_index])[0];
                    p[0] = f4[0];
                    p[1] = f4[1];
                    p[2] = f4[2];
                    p[3] = f4[3];
                    f4 = ((float4*)(void*)&((float*)source_indexbuffer[i + 1])[source_index])[0];
                    p += stride_d;
                    p[0] = f4[0];
                    p[1] = f4[1];
                    p[2] = f4[2];
                    p[3] = f4[3];
                    f4 = ((float4*)(void*)&((float*)source_indexbuffer[i + 2])[source_index])[0];
                    p += stride_d;
                    p[0] = f4[0];
                    p[1] = f4[1];
                    p[2] = f4[2];
                    p[3] = f4[3];
                    f4 = ((float4*)(void*)&((float*)source_indexbuffer[i + 3])[source_index])[0];
                    p += stride_d;
                    p[0] = f4[0];
                    p[1] = f4[1];
                    p[2] = f4[2];
                    p[3] = f4[3];
                    p += stride_d;
                    i += 4;
                }
                while (i < ssmd_datacount)
                {
                    f4 = ((float4*)(void*)&((float*)source_indexbuffer[i + 0])[source_index])[0];
                    p[0] = f4[0];
                    p[1] = f4[1];
                    p[2] = f4[2];
                    p[3] = f4[3];
                    p += stride_d;
                    i++;
                }

                return;
            }
            for (int i = 0; i < ssmd_datacount; i++)
            {
                ((float4*)(void*)&((float*)destination_indexbuffer[i])[destination_index])[0] = ((float4*)(void*)&((float*)source_indexbuffer[i])[source_index])[0];
            }
        }

        #endregion

        #region move_indexed_fx negated
        /// <summary>
        /// data[][].x = data[][].x
        /// </summary>

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public void move_indexed_f1_n([NoAlias] void** destination_indexbuffer, int destination_index_rowsize, bool destination_is_aligned, int destination_index, [NoAlias] void** source_indexbuffer, int source_index_rowsize, bool source_is_aligned, int source_index, int ssmd_datacount)
        {
            if (destination_is_aligned && source_is_aligned)
            {
                float* s = &((float*)source_indexbuffer[0])[source_index];
                float* p = &((float*)destination_indexbuffer[0])[destination_index];

                int stride_s = source_index_rowsize >> 2;
                int stride_d = destination_index_rowsize >> 2;

                int stride_s_2 = stride_s + stride_s;
                int stride_d_2 = stride_d + stride_d;

                int stride_d_3 = stride_d_2 + stride_d;
                int stride_s_3 = stride_s_2 + stride_s;

                int stride_d_4 = stride_d_3 + stride_d;
                int stride_s_4 = stride_s_3 + stride_s;

                int i = 0;
                while (i < (ssmd_datacount & ~3))
                {
                    p[0] = -s[0];
                    p[stride_d] = -s[stride_s];
                    p[stride_d_2] = -s[stride_s_2];
                    p[stride_d_3] = -s[stride_s_3];

                    s += stride_s_4;
                    p += stride_d_4;
                    i += 4;
                }
                while (i < ssmd_datacount)
                {
                    p[0] = s[0];
                    s += stride_s;
                    p += stride_d;
                    i++;
                }
                return;
            }
            if (destination_is_aligned)
            {
                // there is a high penalty when we need to index each array, especially for float 2 and 3 arrays
                float* p = &((float*)destination_indexbuffer[0])[destination_index];

                int stride_d = destination_index_rowsize >> 2;
                int stride_d_2 = stride_d + stride_d;
                int stride_d_3 = stride_d_2 + stride_d;
                int stride_d_4 = stride_d_3 + stride_d;

                int i = 0;
                while (i < (ssmd_datacount & ~3))
                {
                    p[0] = -((float*)source_indexbuffer[i])[source_index];
                    p[stride_d] = -((float*)source_indexbuffer[i + 1])[source_index];
                    p[stride_d_2] = -((float*)source_indexbuffer[i + 2])[source_index];
                    p[stride_d_3] = -((float*)source_indexbuffer[i + 3])[source_index];

                    p += stride_d_4;
                    i += 4;
                }
                while (i < ssmd_datacount)
                {
                    p[0] = -((float*)source_indexbuffer[i])[source_index];
                    p += stride_d;
                    i++;
                }
                return;
            }
            if (source_is_aligned)
            {
                float* s = &((float*)source_indexbuffer[0])[source_index];

                int stride_s = source_index_rowsize >> 2;
                int stride_s_2 = stride_s + stride_s;
                int stride_s_3 = stride_s_2 + stride_s;
                int stride_s_4 = stride_s_3 + stride_s;

                int i = 0;
                while (i < (ssmd_datacount & ~3))
                {
                    ((float*)destination_indexbuffer[i + 0])[destination_index] = -s[0];
                    ((float*)destination_indexbuffer[i + 1])[destination_index] = -s[stride_s];
                    ((float*)destination_indexbuffer[i + 2])[destination_index] = -s[stride_s_2];
                    ((float*)destination_indexbuffer[i + 3])[destination_index] = -s[stride_s_3];

                    s += stride_s_4;
                    i += 4;
                }
                while (i < ssmd_datacount)
                {
                    ((float*)destination_indexbuffer[i])[destination_index] = -s[0];
                    s += stride_s;
                    i++;
                }
                return;
            }
            for (int i = 0; i < ssmd_datacount; i++)
            {
                ((float*)destination_indexbuffer[i])[destination_index] = -((float*)source_indexbuffer[i])[source_index];
            }
        }


        /// <summary>
        /// void[][].xy = void[][].xy
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public void move_indexed_f2_n([NoAlias] void** destination_indexbuffer, int destination_index_rowsize, bool destination_is_aligned, int destination_index, [NoAlias] void** source_indexbuffer, int source_index_rowsize, bool source_is_aligned, int source_index, int ssmd_datacount)
        {
            if (destination_is_aligned && source_is_aligned)
            {
                float* s = &((float*)source_indexbuffer[0])[source_index];
                float* p = &((float*)destination_indexbuffer[0])[destination_index];

                int stride_s = source_index_rowsize >> 2;
                int stride_d = destination_index_rowsize >> 2;

                int stride_s_2 = stride_s + stride_s;
                int stride_d_2 = stride_d + stride_d;

                int stride_d_3 = stride_d_2 + stride_d;
                int stride_s_3 = stride_s_2 + stride_s;

                int stride_d_4 = stride_d_3 + stride_d;
                int stride_s_4 = stride_s_3 + stride_s;

                int i = 0;
                while (i < (ssmd_datacount & ~3))
                {
                    p[0] = -s[0];
                    p[1] = -s[1];
                    p[stride_d] = -s[stride_s];
                    p[stride_d + 1] = -s[stride_s + 1];
                    p[stride_d_2] = -s[stride_s_2];
                    p[stride_d_2 + 1] = -s[stride_s_2 + 1];
                    p[stride_d_3] = -s[stride_s_3];
                    p[stride_d_3 + 1] = -s[stride_s_3 + 1];

                    s += stride_s_4;
                    p += stride_d_4;
                    i += 4;
                }
                while (i < ssmd_datacount)
                {
                    p[0] = -s[0];
                    p[1] = -s[1];
                    s += stride_s;
                    p += stride_d;
                    i++;
                }

                return;
            }
            if (destination_is_aligned)
            {
                float* p = &((float*)destination_indexbuffer[0])[destination_index];

                int stride_d = destination_index_rowsize >> 2;
                int stride_d_2 = stride_d + stride_d;
                int stride_d_3 = stride_d_2 + stride_d;
                int stride_d_4 = stride_d_3 + stride_d;

                int i = 0;
                while (i < (ssmd_datacount & ~3))
                {
                    ((float2*)p)[0] = -((float2*)(void*)&((float*)source_indexbuffer[i])[source_index])[0];
                    ((float2*)(void*)&p[stride_d])[0] = -((float2*)(void*)&((float*)source_indexbuffer[i + 1])[source_index])[0];
                    ((float2*)(void*)&p[stride_d_2])[0] = -((float2*)(void*)&((float*)source_indexbuffer[i + 2])[source_index])[0];
                    ((float2*)(void*)&p[stride_d_3])[0] = -((float2*)(void*)&((float*)source_indexbuffer[i + 3])[source_index])[0];

                    p += stride_d_4;
                    i += 4;
                }
                while (i < ssmd_datacount)
                {
                    ((float2*)p)[0] = -((float2*)(void*)&((float*)source_indexbuffer[i])[source_index])[0];
                    p += stride_d;
                    i++;
                }

                return;
            }
            if (source_is_aligned)
            {
                float* s = &((float*)source_indexbuffer[0])[source_index];

                int stride_s = source_index_rowsize >> 2;
                int stride_s_2 = stride_s + stride_s;
                int stride_s_3 = stride_s_2 + stride_s;
                int stride_s_4 = stride_s_3 + stride_s;

                int i = 0;
                float2 f2_1, f2_2, f2_3, f2_4;
                while (i < (ssmd_datacount & ~3))
                {
                    f2_1.x = s[0];
                    f2_1.y = s[1];

                    f2_2.x = s[stride_s];
                    f2_2.y = s[stride_s + 1];

                    f2_3.x = s[stride_s_2];
                    f2_3.y = s[stride_s_2 + 1];

                    f2_4.x = s[stride_s_3];
                    f2_4.y = s[stride_s_3 + 1];

                    ((float2*)(void*)&((float*)destination_indexbuffer[i + 0])[destination_index])[0] = -f2_1;
                    ((float2*)(void*)&((float*)destination_indexbuffer[i + 1])[destination_index])[0] = -f2_2;
                    ((float2*)(void*)&((float*)destination_indexbuffer[i + 2])[destination_index])[0] = -f2_3;
                    ((float2*)(void*)&((float*)destination_indexbuffer[i + 3])[destination_index])[0] = -f2_4;

                    s += stride_s_4;
                    i += 4;
                }
                while (i < ssmd_datacount)
                {
                    f2_1.x = s[0];
                    f2_1.y = s[1];
                    ((float2*)(void*)&((float*)destination_indexbuffer[i + 0])[destination_index])[0] = -f2_1;
                    s += stride_s;
                    i++;
                }

                return;
            }

            for (int i = 0; i < ssmd_datacount; i++)
            {
                ((float2*)(void*)&((float*)destination_indexbuffer[i])[destination_index])[0] = -((float2*)(void*)&((float*)source_indexbuffer[i])[source_index])[0];
            }
        }

        /// <summary>
        /// void[][].xyz = -void[][].xyz
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public void move_indexed_f3_n([NoAlias] void** destination_indexbuffer, int destination_index_rowsize, bool destination_is_aligned, int destination_index, [NoAlias] void** source_indexbuffer, int source_index_rowsize, bool source_is_aligned, int source_index, int ssmd_datacount)
        {
            if (destination_is_aligned && source_is_aligned)
            {
                float* s = &((float*)source_indexbuffer[0])[source_index];
                float* p = &((float*)destination_indexbuffer[0])[destination_index];

                int stride_s = source_index_rowsize >> 2;
                int stride_d = destination_index_rowsize >> 2;


                int i = 0;
                while (i < (ssmd_datacount & ~3))
                {
                    p[0] = -s[0];
                    p[1] = -s[1];
                    p[2] = -s[2];
                    s += stride_s;
                    p += stride_d;
                    p[0] = -s[0];
                    p[1] = -s[1];
                    p[2] = -s[2];
                    s += stride_s;
                    p += stride_d;
                    p[0] = -s[0];
                    p[1] = -s[1];
                    p[2] = -s[2];
                    s += stride_s;
                    p += stride_d;
                    p[0] = -s[0];
                    p[1] = -s[1];
                    p[2] = -s[2];
                    s += stride_s;
                    p += stride_d;
                    i += 4;
                }
                while (i < ssmd_datacount)
                {
                    p[0] = -s[0];
                    p[1] = -s[1];
                    p[2] = -s[2];
                    s += stride_s;
                    p += stride_d;
                    i++;
                }
                return;
            }
            if (destination_is_aligned)
            {
                float* p = &((float*)destination_indexbuffer[0])[destination_index];

                int stride_d = destination_index_rowsize >> 2;

                int i = 0;
                while (i < (ssmd_datacount & ~3))
                {
                    float3 f3 = -((float3*)(void*)&((float*)source_indexbuffer[i + 0])[source_index])[0];
                    p[0] = f3[0];
                    p[1] = f3[1];
                    p[2] = f3[2];
                    f3 = -((float3*)(void*)&((float*)source_indexbuffer[i + 1])[source_index])[0];
                    p += stride_d;
                    p[0] = f3[0];
                    p[1] = f3[1];
                    p[2] = f3[2];
                    f3 = -((float3*)(void*)&((float*)source_indexbuffer[i + 2])[source_index])[0];
                    p += stride_d;
                    p[0] = f3[0];
                    p[1] = f3[1];
                    p[2] = f3[2];
                    f3 = -((float3*)(void*)&((float*)source_indexbuffer[i + 3])[source_index])[0];
                    p += stride_d;
                    p[0] = f3[0];
                    p[1] = f3[1];
                    p[2] = f3[2];
                    p += stride_d;
                    i += 4;
                }
                while (i < ssmd_datacount)
                {
                    float3 f3 = -((float3*)(void*)&((float*)source_indexbuffer[i + 0])[source_index])[0];
                    p[0] = f3[0];
                    p[1] = f3[1];
                    p[2] = f3[2];
                    p += stride_d;
                    i++;
                }
                return;
            }
            if (source_is_aligned)
            {
                float* s = &((float*)source_indexbuffer[0])[source_index];

                int stride_s = source_index_rowsize >> 2;
                int i = 0;
                float3 f3 = 0;
                while (i < (ssmd_datacount & ~3))
                {
                    f3[0] = s[0];
                    f3[1] = s[1];
                    f3[2] = s[2];
                    ((float3*)(void*)&((float*)destination_indexbuffer[i + 0])[destination_index])[0] = -f3;
                    s += stride_s;
                    f3[0] = s[0];
                    f3[1] = s[1];
                    f3[2] = s[2];
                    s += stride_s;
                    ((float3*)(void*)&((float*)destination_indexbuffer[i + 1])[destination_index])[0] = -f3;
                    f3[0] = s[0];
                    f3[1] = s[1];
                    f3[2] = s[2];
                    s += stride_s;
                    ((float3*)(void*)&((float*)destination_indexbuffer[i + 2])[destination_index])[0] = -f3;
                    f3[0] = s[0];
                    f3[1] = s[1];
                    f3[2] = s[2];
                    s += stride_s;
                    ((float3*)(void*)&((float*)destination_indexbuffer[i + 3])[destination_index])[0] = -f3;
                    i += 4;
                }
                while (i < ssmd_datacount)
                {
                    f3[0] = s[0];
                    f3[1] = s[1];
                    f3[2] = s[2];
                    ((float3*)(void*)&((float*)destination_indexbuffer[i])[destination_index])[0] = -f3;
                    s += stride_s;
                    i++;
                }

                return;
            }
            for (int i = 0; i < ssmd_datacount; i++)
            {
                ((float3*)(void*)&((float*)destination_indexbuffer[i])[destination_index])[0] = -((float3*)(void*)&((float*)source_indexbuffer[i])[source_index])[0];
            }
        }

        /// <summary>
        /// [][].xyzw = [][].xyzw
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public void move_indexed_f4_n([NoAlias] void** destination_indexbuffer, int destination_index_rowsize, bool destination_is_aligned, int destination_index, [NoAlias] void** source_indexbuffer, int source_index_rowsize, bool source_is_aligned, int source_index, int ssmd_datacount)
        {
            if (destination_is_aligned && source_is_aligned)
            {
                float* s = &((float*)source_indexbuffer[0])[source_index];
                float* p = &((float*)destination_indexbuffer[0])[destination_index];

                int stride_s = source_index_rowsize >> 2;
                int stride_d = destination_index_rowsize >> 2;


                int i = 0;
                while (i < (ssmd_datacount & ~3))
                {
                    p[0] = -s[0];
                    p[1] = -s[1];
                    p[2] = -s[2];
                    p[3] = -s[3];
                    s += stride_s;
                    p += stride_d;
                    p[0] = -s[0];
                    p[1] = -s[1];
                    p[2] = -s[2];
                    p[3] = -s[3];
                    s += stride_s;
                    p += stride_d;
                    p[0] = -s[0];
                    p[1] = -s[1];
                    p[2] = -s[2];
                    p[3] = -s[3];
                    s += stride_s;
                    p += stride_d;
                    p[0] = -s[0];
                    p[1] = -s[1];
                    p[2] = -s[2];
                    p[3] = -s[3];
                    s += stride_s;
                    p += stride_d;
                    i += 4;
                }
                while (i < ssmd_datacount)
                {
                    p[0] = -s[0];
                    p[1] = -s[1];
                    p[2] = -s[2];
                    p[3] = -s[3];
                    s += stride_s;
                    p += stride_d;
                    i++;
                }

                return;
            }
            if (source_is_aligned)
            {
                float* s = &((float*)source_indexbuffer[0])[source_index];

                int stride_s = source_index_rowsize >> 2;
                int i = 0;
                float4 f4 = 0;
                while (i < (ssmd_datacount & ~3))
                {
                    f4[0] = s[0];
                    f4[1] = s[1];
                    f4[2] = s[2];
                    f4[3] = s[3];
                    ((float4*)(void*)&((float*)destination_indexbuffer[i + 0])[destination_index])[0] = -f4;
                    s += stride_s;
                    f4[0] = s[0];
                    f4[1] = s[1];
                    f4[2] = s[2];
                    f4[3] = s[3];
                    s += stride_s;
                    ((float4*)(void*)&((float*)destination_indexbuffer[i + 1])[destination_index])[0] = -f4;
                    f4[0] = s[0];
                    f4[1] = s[1];
                    f4[2] = s[2];
                    f4[3] = s[3];
                    s += stride_s;
                    ((float4*)(void*)&((float*)destination_indexbuffer[i + 2])[destination_index])[0] = -f4;
                    f4[0] = s[0];
                    f4[1] = s[1];
                    f4[2] = s[2];
                    f4[3] = s[3];
                    s += stride_s;
                    ((float4*)(void*)&((float*)destination_indexbuffer[i + 3])[destination_index])[0] = -f4;
                    i += 4;
                }
                while (i < ssmd_datacount)
                {
                    f4[0] = s[0];
                    f4[1] = s[1];
                    f4[2] = s[2];
                    f4[3] = s[3];
                    ((float4*)(void*)&((float*)destination_indexbuffer[i])[destination_index])[0] = -f4;
                    s += stride_s;
                    i++;
                }

                return;
            }
            if (destination_is_aligned)
            {
                float* p = &((float*)destination_indexbuffer[0])[destination_index];
                int stride_d = destination_index_rowsize >> 2;
                int i = 0;
                float4 f4;
                while (i < (ssmd_datacount & ~3))
                {
                    f4 = -((float4*)(void*)&((float*)source_indexbuffer[i + 0])[source_index])[0];
                    p[0] = f4[0];
                    p[1] = f4[1];
                    p[2] = f4[2];
                    p[3] = f4[3];
                    f4 = -((float4*)(void*)&((float*)source_indexbuffer[i + 1])[source_index])[0];
                    p += stride_d;
                    p[0] = f4[0];
                    p[1] = f4[1];
                    p[2] = f4[2];
                    p[3] = f4[3];
                    f4 = -((float4*)(void*)&((float*)source_indexbuffer[i + 2])[source_index])[0];
                    p += stride_d;
                    p[0] = f4[0];
                    p[1] = f4[1];
                    p[2] = f4[2];
                    p[3] = f4[3];
                    f4 = -((float4*)(void*)&((float*)source_indexbuffer[i + 3])[source_index])[0];
                    p += stride_d;
                    p[0] = f4[0];
                    p[1] = f4[1];
                    p[2] = f4[2];
                    p[3] = f4[3];
                    p += stride_d;
                    i += 4;
                }
                while (i < ssmd_datacount)
                {
                    f4 = -((float4*)(void*)&((float*)source_indexbuffer[i + 0])[source_index])[0];
                    p[0] = f4[0];
                    p[1] = f4[1];
                    p[2] = f4[2];
                    p[3] = f4[3];
                    p += stride_d;
                    i++;
                }

                return;
            }
            for (int i = 0; i < ssmd_datacount; i++)
            {
                ((float4*)(void*)&((float*)destination_indexbuffer[i])[destination_index])[0] = -((float4*)(void*)&((float*)source_indexbuffer[i])[source_index])[0];
            }
        }

        #endregion
    }
}
