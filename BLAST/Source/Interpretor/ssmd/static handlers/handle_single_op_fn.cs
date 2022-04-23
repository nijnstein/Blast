//############################################################################################################################
// BLAST v1.0.4c                                                                                                             #
// Copyright © 2022 Rob Lemmens | NijnStein Software <rob.lemmens.s31 gmail com> All Rights Reserved                   ^__^\ #
// Unauthorized copying of this file, via any medium is strictly prohibited proprietary and confidential               (oo)\ #
//                                                                                                                     (__)  #
//############################################################################################################################

#if STANDALONE_VSBUILD
using NSS.Blast.Standalone;
using System.Reflection;
#else
using UnityEngine;
using Unity.Burst.CompilerServices;
#endif


using Unity.Burst;
using Unity.Mathematics;

namespace NSS.Blast.SSMD
{
    unsafe public partial struct BlastSSMDInterpretor
    {
        static void handle_single_op_fn([NoAlias] float4* register, ref byte vector_size, int ssmd_datacount, [NoAlias] in DATAREC target, in blast_operation op, in extended_blast_operation ex_op, in bool is_negated, in int indexed)
        {
            handle_single_op_fn(register, ref vector_size, ssmd_datacount, target.data, target.index, op, ex_op, is_negated, indexed, target.is_aligned, target.row_size); 
        }

        /// <summary>
        /// 
        /// </summary>
        static void handle_single_op_fn([NoAlias] float4* register, ref byte vector_size, int ssmd_datacount, [NoAlias] void** data, int source_index, in blast_operation op, in extended_blast_operation ex_op, in bool is_negated, in int indexed, in bool is_aligned, int stride)
        {
            // when indexed -> vectorsize is 1 element and we adjust index to point to it 
            if (indexed >= 0)
            {
                if (op >= blast_operation.index_x && op <= blast_operation.index_n)
                {
                }
                else
                {
                   source_index += indexed;
                   vector_size = 1;
                }
            }

            switch (vector_size)
            {
                case 0:
                case 4:
                    switch (op)
                    {
                        // path when indexer is used as a function an not parsed inline 
                        case blast_operation.index_x:
                            if (!is_negated) simd.move_indexed_data_as_f1_to_f1f4(register, data, stride, is_aligned, source_index, ssmd_datacount);
                            else simd.move_indexed_data_as_f1_to_f1f4_negated(register, data, stride, is_aligned, source_index, ssmd_datacount);
                            vector_size = 1;
                            return;

                        case blast_operation.index_y:
                            if (!is_negated) simd.move_indexed_data_as_f1_to_f1f4(register, data, stride, is_aligned, source_index + 1, ssmd_datacount);
                            else simd.move_indexed_data_as_f1_to_f1f4_negated(register, data, stride, is_aligned, source_index + 1, ssmd_datacount);
                            vector_size = 1;
                            return;

                        case blast_operation.index_z:
                            if (!is_negated) simd.move_indexed_data_as_f1_to_f1f4(register, data, stride, is_aligned, source_index + 2, ssmd_datacount);
                            else simd.move_indexed_data_as_f1_to_f1f4_negated(register, data, stride, is_aligned, source_index + 2, ssmd_datacount);
                            vector_size = 1;
                            return;

                        case blast_operation.index_w:
                            if (!is_negated) simd.move_indexed_data_as_f1_to_f1f4(register, data, stride, is_aligned, source_index + 3, ssmd_datacount);
                            else simd.move_indexed_data_as_f1_to_f1f4_negated(register, data, stride, is_aligned, source_index + 3, ssmd_datacount);
                            vector_size = 1;
                            return;


                        case blast_operation.index_n:
                            {
                                // somewhat special indexer: every index for each record CAN be different
                                // - should handle as a 2 parameter function thats handled in sequence 

                                Debug.Log("Handle MEEEEEEE");
                            }



                            return;

                        case blast_operation.abs:
                            for (int i = 0; i < ssmd_datacount; i++) register[i] = math.abs(f4(data, source_index, i));
                            return;

                        case blast_operation.trunc:
                            if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i] = math.trunc(f4(data, source_index, i));
                            else for (int i = 0; i < ssmd_datacount; i++) register[i] = math.trunc(-f4(data, source_index, i));
                            return;

                        case blast_operation.mina:
                            vector_size = 1;
                            if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i].x = math.cmin(f4(data, source_index, i));
                            else for (int i = 0; i < ssmd_datacount; i++) register[i].x = math.cmin(-f4(data, source_index, i));
                            return;

                        case blast_operation.maxa:
                            vector_size = 1;
                            if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i].x = math.cmax(f4(data, source_index, i));
                            else for (int i = 0; i < ssmd_datacount; i++) register[i].x = math.cmax(-f4(data, source_index, i));
                            return;

                        case blast_operation.csum:
                            vector_size = 1;
                            if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i].x = math.csum(f4(data, source_index, i));
                            else for (int i = 0; i < ssmd_datacount; i++) register[i].x = math.csum(-f4(data, source_index, i));
                            return;

                        case blast_operation.ex_op:
                            switch (ex_op)
                            {
                                case extended_blast_operation.sqrt:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i] = math.sqrt(f4(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i] = math.sqrt(-f4(data, source_index, i));
                                    return;

                                case extended_blast_operation.rsqrt:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i] = math.rsqrt(f4(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i] = math.rsqrt(-f4(data, source_index, i));
                                    return;

                                case extended_blast_operation.sin:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i] = math.sin(f4(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i] = math.sin(-f4(data, source_index, i));
                                    return;

                                case extended_blast_operation.cos:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i] = math.cos(f4(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i] = math.cos(-f4(data, source_index, i));
                                    return;

                                case extended_blast_operation.tan:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i] = math.tan(f4(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i] = math.tan(-f4(data, source_index, i));
                                    return;

                                case extended_blast_operation.sinh:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i] = math.sinh(f4(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i] = math.sinh(-f4(data, source_index, i));
                                    return;

                                case extended_blast_operation.cosh:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i] = math.cosh(f4(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i] = math.cosh(-f4(data, source_index, i));
                                    return;

                                case extended_blast_operation.atan:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i] = math.atan(f4(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i] = math.atan(-f4(data, source_index, i));
                                    return;
                                case extended_blast_operation.degrees:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i] = math.degrees(f4(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i] = math.degrees(-f4(data, source_index, i));
                                    return;

                                case extended_blast_operation.radians:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i] = math.radians(f4(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i] = math.radians(-f4(data, source_index, i));
                                    return;

                                case extended_blast_operation.ceil:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i] = math.ceil(f4(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i] = math.ceil(-f4(data, source_index, i));
                                    return;

                                case extended_blast_operation.floor:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i] = math.floor(f4(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i] = math.floor(-f4(data, source_index, i));
                                    return;

                                case extended_blast_operation.frac:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i] = math.frac(f4(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i] = math.frac(-f4(data, source_index, i));
                                    return;

                                case extended_blast_operation.normalize:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i] = math.normalize(f4(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i] = math.normalize(-f4(data, source_index, i));
                                    return;

                                case extended_blast_operation.saturate:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i] = math.saturate(f4(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i] = math.saturate(-f4(data, source_index, i));
                                    return;

                                case extended_blast_operation.logn:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i] = math.log(f4(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i] = math.log(-f4(data, source_index, i));
                                    return;

                                case extended_blast_operation.log10:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i] = math.log10(f4(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i] = math.log10(-f4(data, source_index, i));
                                    return;

                                case extended_blast_operation.log2:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i] = math.log2(f4(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i] = math.log2(-f4(data, source_index, i));
                                    return;

                                case extended_blast_operation.exp10:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i] = math.exp10(f4(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i] = math.exp10(-f4(data, source_index, i));
                                    return;

                                case extended_blast_operation.exp:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i] = math.exp(f4(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i] = math.exp(-f4(data, source_index, i));
                                    return;

                                case extended_blast_operation.ceillog2:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i] = math.ceillog2((int4)f4(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i] = math.ceillog2(-(int4)f4(data, source_index, i));
                                    return;

                                case extended_blast_operation.ceilpow2:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i] = math.ceilpow2((int4)f4(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i] = math.ceilpow2(-(int4)f4(data, source_index, i));
                                    return;

                                case extended_blast_operation.floorlog2:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i] = math.floorlog2((int4)f4(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i] = math.floorlog2(-(int4)f4(data, source_index, i));
                                    return;
                            }
                            break;


                    }
                    break;


                case 1:
                    switch (op)
                    {
                        case blast_operation.abs:
                            for (int i = 0; i < ssmd_datacount; i++) register[i].x = math.abs(((float*)data[i])[source_index]);
                            return;

                        case blast_operation.trunc:
                            if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i].x = math.trunc(f1(data, source_index, i));
                            else for (int i = 0; i < ssmd_datacount; i++) register[i].x = math.trunc(-f1(data, source_index, i));
                            return;

                        case blast_operation.csum:
                        case blast_operation.mina:
                        case blast_operation.maxa:
                            vector_size = 1;
                            if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i] = f1(data, source_index, i);
                            else for (int i = 0; i < ssmd_datacount; i++) register[i] = -f1(data, source_index, i);
                            return;

                        case blast_operation.ex_op:
                            switch (ex_op)
                            {
                                case extended_blast_operation.sqrt:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i].x = math.sqrt(f1(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i].x = math.sqrt(-f1(data, source_index, i));
                                    return;

                                case extended_blast_operation.rsqrt:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i].x = math.rsqrt(f1(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i].x = math.rsqrt(-f1(data, source_index, i));
                                    return;

                                case extended_blast_operation.sin:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i].x = math.sin(f1(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i].x = math.sin(-f1(data, source_index, i));
                                    return;

                                case extended_blast_operation.cos:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i].x = math.cos(f1(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i].x = math.cos(-f1(data, source_index, i));
                                    return;

                                case extended_blast_operation.tan:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i].x = math.tan(f1(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i].x = math.tan(-f1(data, source_index, i));
                                    return;

                                case extended_blast_operation.sinh:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i].x = math.sinh(f1(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i].x = math.sinh(-f1(data, source_index, i));
                                    return;

                                case extended_blast_operation.cosh:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i].x = math.cosh(f1(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i].x = math.cosh(-f1(data, source_index, i));
                                    return;

                                case extended_blast_operation.atan:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i].x = math.atan(f1(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i].x = math.atan(-f1(data, source_index, i));
                                    return;
                                case extended_blast_operation.degrees:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i].x = math.degrees(f1(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i].x = math.degrees(-f1(data, source_index, i));
                                    return;

                                case extended_blast_operation.radians:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i].x = math.radians(f1(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i].x = math.radians(-f1(data, source_index, i));
                                    return;

                                case extended_blast_operation.ceil:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i].x = math.ceil(f1(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i].x = math.ceil(-f1(data, source_index, i));
                                    return;

                                case extended_blast_operation.floor:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i].x = math.floor(f1(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i].x = math.floor(-f1(data, source_index, i));
                                    return;

                                case extended_blast_operation.frac:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i].x = math.frac(f1(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i].x = math.frac(-f1(data, source_index, i));
                                    return;

                                case extended_blast_operation.normalize:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i].x = f1(data, source_index, i);
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i].x = -f1(data, source_index, i);
                                    return;

                                case extended_blast_operation.saturate:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i].x = math.saturate(f1(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i].x = math.saturate(-f1(data, source_index, i));
                                    return;

                                case extended_blast_operation.logn:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i].x = math.log(f1(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i].x = math.log(-f1(data, source_index, i));
                                    return;

                                case extended_blast_operation.log10:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i].x = math.log10(f1(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i].x = math.log10(-f1(data, source_index, i));
                                    return;

                                case extended_blast_operation.log2:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i].x = math.log2(f1(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i].x = math.log2(-f1(data, source_index, i));
                                    return;

                                case extended_blast_operation.exp10:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i].x = math.exp10(f1(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i].x = math.exp10(-f1(data, source_index, i));
                                    return;

                                case extended_blast_operation.exp:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i].x = math.exp(f1(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i].x = math.exp(-f1(data, source_index, i));
                                    return;

                                case extended_blast_operation.ceillog2:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i].x = math.ceillog2((int)f1(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i].x = math.ceillog2(-(int)f1(data, source_index, i));
                                    return;

                                case extended_blast_operation.ceilpow2:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i].x = math.ceilpow2((int)f1(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i].x = math.ceilpow2(-(int)f1(data, source_index, i));
                                    return;

                                case extended_blast_operation.floorlog2:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i].x = math.floorlog2((int)f1(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i].x = math.floorlog2(-(int)f1(data, source_index, i));
                                    return;
                            }
                            break;

                    }
                    break;

                case 2:
                    switch (op)
                    {
                        // path when indexer is used as a function an not parsed inline 
                        case blast_operation.index_x:
                            // the index of float.x is the same as the index of float2.x in the index buffer 
                            if (!is_negated) simd.move_indexed_data_as_f1_to_f1f4(register, data, stride, is_aligned, source_index, ssmd_datacount);
                            else simd.move_indexed_data_as_f1_to_f1f4_negated(register, data, stride, is_aligned, source_index, ssmd_datacount);
                            //if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i].x = f2(data, source_index, i).x;
                            //else for (int i = 0; i < ssmd_datacount; i++) register[i].x = (-f2(data, source_index, i)).x;
                            vector_size = 1;
                            return;

                        case blast_operation.index_y:
                            if (!is_negated) simd.move_indexed_data_as_f1_to_f1f4(register, data, stride, is_aligned, source_index + 1, ssmd_datacount);
                            else simd.move_indexed_data_as_f1_to_f1f4_negated(register, data, stride, is_aligned, source_index + 1, ssmd_datacount);
                            //if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i].x = f2(data, source_index, i).y;
                            //else for (int i = 0; i < ssmd_datacount; i++) register[i].x = (-f2(data, source_index, i)).y;
                            vector_size = 1;
                            return;

                        case blast_operation.abs:
                            for (int i = 0; i < ssmd_datacount; i++) register[i].xy = math.abs(f2(data, source_index, i));
                            return;

                        case blast_operation.trunc:
                            if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i].xy = math.trunc(f2(data, source_index, i));
                            else for (int i = 0; i < ssmd_datacount; i++) register[i].xy = math.trunc(-f2(data, source_index, i));
                            return;

                        case blast_operation.mina:
                            vector_size = 1;
                            if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i].x = math.cmin(f2(data, source_index, i));
                            else for (int i = 0; i < ssmd_datacount; i++) register[i].x = math.cmin(-f2(data, source_index, i));
                            return;

                        case blast_operation.maxa:
                            vector_size = 1;
                            if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i].x = math.cmax(f2(data, source_index, i));
                            else for (int i = 0; i < ssmd_datacount; i++) register[i].x = math.cmax(-f2(data, source_index, i));
                            return;

                        case blast_operation.csum:
                            vector_size = 1;
                            if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i].x = math.csum(f2(data, source_index, i));
                            else for (int i = 0; i < ssmd_datacount; i++) register[i].x = math.csum(-f2(data, source_index, i));
                            return;


                        case blast_operation.ex_op:
                            switch (ex_op)
                            {
                                case extended_blast_operation.sqrt:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i].xy = math.sqrt(f2(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i].xy = math.sqrt(-f2(data, source_index, i));
                                    return;

                                case extended_blast_operation.rsqrt:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i].xy = math.rsqrt(f2(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i].xy = math.rsqrt(-f2(data, source_index, i));
                                    return;

                                case extended_blast_operation.sin:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i].xy = math.sin(f2(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i].xy = math.sin(-f2(data, source_index, i));
                                    return;

                                case extended_blast_operation.cos:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i].xy = math.cos(f2(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i].xy = math.cos(-f2(data, source_index, i));
                                    return;

                                case extended_blast_operation.tan:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i].xy = math.tan(f2(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i].xy = math.tan(-f2(data, source_index, i));
                                    return;

                                case extended_blast_operation.sinh:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i].xy = math.sinh(f2(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i].xy = math.sinh(-f2(data, source_index, i));
                                    return;

                                case extended_blast_operation.cosh:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i].xy = math.cosh(f2(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i].xy = math.cosh(-f2(data, source_index, i));
                                    return;

                                case extended_blast_operation.atan:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i].xy = math.atan(f2(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i].xy = math.atan(-f2(data, source_index, i));
                                    return;
                                case extended_blast_operation.degrees:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i].xy = math.degrees(f2(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i].xy = math.degrees(-f2(data, source_index, i));
                                    return;

                                case extended_blast_operation.radians:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i].xy = math.radians(f2(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i].xy = math.radians(-f2(data, source_index, i));
                                    return;

                                case extended_blast_operation.ceil:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i].xy = math.ceil(f2(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i].xy = math.ceil(-f2(data, source_index, i));
                                    return;

                                case extended_blast_operation.floor:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i].xy = math.floor(f2(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i].xy = math.floor(-f2(data, source_index, i));
                                    return;

                                case extended_blast_operation.frac:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i].xy = math.frac(f2(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i].xy = math.frac(-f2(data, source_index, i));
                                    return;

                                case extended_blast_operation.normalize:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i].xy = math.normalize(f2(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i].xy = math.normalize(-f2(data, source_index, i));
                                    return;

                                case extended_blast_operation.saturate:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i].xy = math.saturate(f2(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i].xy = math.saturate(-f2(data, source_index, i));
                                    return;

                                case extended_blast_operation.logn:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i].xy = math.log(f2(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i].xy = math.log(-f2(data, source_index, i));
                                    return;

                                case extended_blast_operation.log10:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i].xy = math.log10(f2(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i].xy = math.log10(-f2(data, source_index, i));
                                    return;

                                case extended_blast_operation.log2:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i].xy = math.log2(f2(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i].xy = math.log2(-f2(data, source_index, i));
                                    return;

                                case extended_blast_operation.exp10:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i].xy = math.exp10(f2(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i].xy = math.exp10(-f2(data, source_index, i));
                                    return;

                                case extended_blast_operation.exp:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i].xy = math.exp(f2(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i].xy = math.exp(-f2(data, source_index, i));
                                    return;

                                case extended_blast_operation.ceillog2:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i].xy = math.ceillog2((int2)f2(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i].xy = math.ceillog2(-(int2)f2(data, source_index, i));
                                    return;

                                case extended_blast_operation.ceilpow2:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i].xy = math.ceilpow2((int2)f2(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i].xy = math.ceilpow2(-(int2)f2(data, source_index, i));
                                    return;

                                case extended_blast_operation.floorlog2:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i].xy = math.floorlog2((int2)f2(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i].xy = math.floorlog2(-(int2)f2(data, source_index, i));
                                    return;
                            }
                            break;

                    }
                    break;


                case 3:
                    switch (op)
                    {
                        // path when indexer is used as a function an not parsed inline 
                        case blast_operation.index_x:
                            if (!is_negated) simd.move_indexed_data_as_f1_to_f1f4(register, data, stride, is_aligned, source_index, ssmd_datacount);
                            else simd.move_indexed_data_as_f1_to_f1f4_negated(register, data, stride, is_aligned, source_index, ssmd_datacount);
                            //if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i].x = f3(data, source_index, i).x;
                            //else for (int i = 0; i < ssmd_datacount; i++) register[i].x = (-f4(data, source_index, i)).x;
                            vector_size = 1;
                            return;

                        case blast_operation.index_y:
                            if (!is_negated) simd.move_indexed_data_as_f1_to_f1f4(register, data, stride, is_aligned, source_index + 1, ssmd_datacount);
                            else simd.move_indexed_data_as_f1_to_f1f4_negated(register, data, stride, is_aligned, source_index + 1, ssmd_datacount);
                            //if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i].x = f3(data, source_index, i).y;
                            //else for (int i = 0; i < ssmd_datacount; i++) register[i].x = (-f3(data, source_index, i)).y;
                            vector_size = 1;
                            return;

                        case blast_operation.index_z:
                            if (!is_negated) simd.move_indexed_data_as_f1_to_f1f4(register, data, stride, is_aligned, source_index + 2, ssmd_datacount);
                            else simd.move_indexed_data_as_f1_to_f1f4_negated(register, data, stride, is_aligned, source_index + 2, ssmd_datacount);
                            vector_size = 1;
                            return;

                        case blast_operation.abs:
                            for (int i = 0; i < ssmd_datacount; i++) register[i].xyz = math.abs(f3(data, source_index, i));
                            return;

                        case blast_operation.trunc:
                            if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i].xyz = math.trunc(f3(data, source_index, i));
                            else for (int i = 0; i < ssmd_datacount; i++) register[i].xyz = math.trunc(-f3(data, source_index, i));
                            return;

                        case blast_operation.mina:
                            vector_size = 1;
                            if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i].xy = math.cmin(f3(data, source_index, i));
                            else for (int i = 0; i < ssmd_datacount; i++) register[i].x = math.cmin(-f3(data, source_index, i));
                            return;

                        case blast_operation.maxa:
                            vector_size = 1;
                            if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i].x = math.cmax(f3(data, source_index, i));
                            else for (int i = 0; i < ssmd_datacount; i++) register[i].x = math.cmax(-f3(data, source_index, i));
                            return;

                        case blast_operation.csum:
                            vector_size = 1;
                            if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i].x = math.csum(f3(data, source_index, i));
                            else for (int i = 0; i < ssmd_datacount; i++) register[i].x = math.csum(-f3(data, source_index, i));
                            return;


                        case blast_operation.ex_op:
                            switch (ex_op)
                            {
                                case extended_blast_operation.sqrt:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i].xyz = math.sqrt(f3(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i].xyz = math.sqrt(-f3(data, source_index, i));
                                    return;

                                case extended_blast_operation.rsqrt:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i].xyz = math.rsqrt(f3(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i].xyz = math.rsqrt(-f3(data, source_index, i));
                                    return;

                                case extended_blast_operation.sin:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i].xyz = math.sin(f3(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i].xyz = math.sin(-f3(data, source_index, i));
                                    return;

                                case extended_blast_operation.cos:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i].xyz = math.cos(f3(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i].xyz = math.cos(-f3(data, source_index, i));
                                    return;

                                case extended_blast_operation.tan:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i].xyz = math.tan(f3(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i].xyz = math.tan(-f3(data, source_index, i));
                                    return;

                                case extended_blast_operation.sinh:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i].xyz = math.sinh(f3(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i].xyz = math.sinh(-f3(data, source_index, i));
                                    return;

                                case extended_blast_operation.cosh:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i].xyz = math.cosh(f3(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i].xyz = math.cosh(-f3(data, source_index, i));
                                    return;

                                case extended_blast_operation.atan:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i].xyz = math.atan(f3(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i].xyz = math.atan(-f3(data, source_index, i));
                                    return;
                                case extended_blast_operation.degrees:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i].xyz = math.degrees(f3(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i].xyz = math.degrees(-f3(data, source_index, i));
                                    return;

                                case extended_blast_operation.radians:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i].xyz = math.radians(f3(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i].xyz = math.radians(-f3(data, source_index, i));
                                    return;

                                case extended_blast_operation.ceil:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i].xyz = math.ceil(f3(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i].xyz = math.ceil(-f3(data, source_index, i));
                                    return;

                                case extended_blast_operation.floor:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i].xyz = math.floor(f3(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i].xyz = math.floor(-f3(data, source_index, i));
                                    return;

                                case extended_blast_operation.frac:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i].xyz = math.frac(f3(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i].xyz = math.frac(-f3(data, source_index, i));
                                    return;

                                case extended_blast_operation.normalize:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i].xyz = math.normalize(f3(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i].xyz = math.normalize(-f3(data, source_index, i));
                                    return;

                                case extended_blast_operation.saturate:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i].xyz = math.saturate(f3(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i].xyz = math.saturate(-f3(data, source_index, i));
                                    return;

                                case extended_blast_operation.logn:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i].xyz = math.log(f3(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i].xyz = math.log(-f3(data, source_index, i));
                                    return;

                                case extended_blast_operation.log10:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i].xyz = math.log10(f3(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i].xyz = math.log10(-f3(data, source_index, i));
                                    return;

                                case extended_blast_operation.log2:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i].xyz = math.log2(f3(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i].xyz = math.log2(-f3(data, source_index, i));
                                    return;

                                case extended_blast_operation.exp10:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i].xyz = math.exp10(f3(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i].xyz = math.exp10(-f3(data, source_index, i));
                                    return;

                                case extended_blast_operation.exp:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i].xyz = math.exp(f3(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i].xyz = math.exp(-f3(data, source_index, i));
                                    return;

                                case extended_blast_operation.ceillog2:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i].xyz = math.ceillog2((int3)f3(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i].xyz = math.ceillog2(-(int3)f3(data, source_index, i));
                                    return;

                                case extended_blast_operation.ceilpow2:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i].xyz = math.ceilpow2((int3)f3(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i].xyz = math.ceilpow2(-(int3)f3(data, source_index, i));
                                    return;

                                case extended_blast_operation.floorlog2:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i].xyz = math.floorlog2((int3)f3(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i].xyz = math.floorlog2(-(int3)f3(data, source_index, i));
                                    return;
                            }
                            break;

                    }
                    break;
            }

            // reaching here should be an error

#if DEVELOPMENT_BUILD || TRACE
            Debug.LogError($"blast.ssmd.handle_single_op_fn -> operation {op}, extended: {ex_op} not supported on vectorsize {vector_size}");
#endif

        }



        /// <summary>
        /// 
        /// </summary>
        static void handle_single_op_constant([NoAlias] float4* register, ref byte vector_size, int ssmd_datacount, float constant, in blast_operation op, in extended_blast_operation ex_op, in bool is_negated = false, in bool not = false, bool is_sequenced = false)
        {
            handle_constant_value_operation(ref vector_size, ssmd_datacount, ref constant, in op, in ex_op, in is_negated, in not, ref is_sequenced);
            handle_single_op_constant_to_register(register, ref vector_size, ssmd_datacount, constant, op, is_sequenced);
        }

        /// <summary>
        /// 
        /// </summary>
        static void handle_single_op_constant([NoAlias] float4* register, ref byte vector_size, int ssmd_datacount, float constant, in blast_operation op, in extended_blast_operation ex_op, bool is_negated, bool not, bool is_sequenced, in DATAREC target_rec)
        {
            handle_constant_value_operation(ref vector_size, ssmd_datacount, ref constant, in op, in ex_op, in is_negated, in not, ref is_sequenced);

            if (target_rec.is_set)
            {
                // output to indexed records 
                handle_single_op_constant_to_target(register, ref vector_size, ssmd_datacount, constant, op, is_sequenced, target_rec);
            }
            else
            {
                // output to register 
                handle_single_op_constant_to_register(register, ref vector_size, ssmd_datacount, constant, op, is_sequenced);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        static void handle_constant_value_operation(ref byte vector_size, int ssmd_datacount, ref float constant, in blast_operation op, in extended_blast_operation ex_op, in bool is_negated, in bool not, ref bool is_sequenced)
        {
            // perform op on value
            switch (op)
            {
                // for completeness.. as long as constants are 1 element all return the same data and are pointless
                case blast_operation.index_x:
                case blast_operation.index_y:
                case blast_operation.index_z:
                case blast_operation.index_w:
                    vector_size = 1;
                    break;

                case blast_operation.expand_v2: vector_size = 2; break;
                case blast_operation.expand_v3: vector_size = 3; break;
                case blast_operation.expand_v4: vector_size = 4; break;

                case blast_operation.nop: break;
                case blast_operation.not: constant = math.select(0, 1, constant == 0); break;
                case blast_operation.abs: constant = math.abs(constant); break;
                case blast_operation.trunc: constant = math.trunc(constant); break;

                // in-sequence operations: handle sequenced operation on register
                case blast_operation.add:
                case blast_operation.multiply:
                case blast_operation.divide:
                case blast_operation.and:
                case blast_operation.or:
                case blast_operation.xor:
                case blast_operation.greater:
                case blast_operation.smaller:
                case blast_operation.smaller_equals:
                case blast_operation.greater_equals:
                case blast_operation.equals:
                case blast_operation.not_equals:
                // if handling a sequence this would be handled in getsequence
                case blast_operation.substract:
                    {
                        is_sequenced = true;
                    }
                    break;

                // extended operations 
                case blast_operation.ex_op:
                    switch (ex_op)
                    {
                        case extended_blast_operation.sqrt: constant = math.sqrt(constant); break;
                        case extended_blast_operation.rsqrt: constant = math.rsqrt(constant); break;
                        case extended_blast_operation.sin: constant = math.sin(constant); break;
                        case extended_blast_operation.cos: constant = math.cos(constant); break;
                        case extended_blast_operation.tan: constant = math.tan(constant); break;
                        case extended_blast_operation.sinh: constant = math.sinh(constant); break;
                        case extended_blast_operation.cosh: constant = math.cosh(constant); break;
                        case extended_blast_operation.atan: constant = math.atan(constant); break;
                        case extended_blast_operation.degrees: constant = math.degrees(constant); break;
                        case extended_blast_operation.radians: constant = math.radians(constant); break;
                        case extended_blast_operation.ceil: constant = math.ceil(constant); break;
                        case extended_blast_operation.floor: constant = math.floor(constant); break;
                        case extended_blast_operation.frac: constant = math.frac(constant); break;
                        case extended_blast_operation.normalize: break; // constant = constant; break; // not defined on size 1
                        case extended_blast_operation.saturate: constant = math.saturate(constant); break;
                        case extended_blast_operation.logn: constant = math.log(constant); break;
                        case extended_blast_operation.log10: constant = math.log10(constant); break;
                        case extended_blast_operation.log2: constant = math.log2(constant); break;
                        case extended_blast_operation.exp10: constant = math.exp10(constant); break;
                        case extended_blast_operation.exp: constant = math.exp(constant); break;
                        case extended_blast_operation.ceillog2: constant = math.ceillog2((int)constant); break;
                        case extended_blast_operation.ceilpow2: constant = math.ceilpow2((int)constant); break;
                        case extended_blast_operation.floorlog2: constant = math.floorlog2((int)constant); break;

                        default:
#if DEVELOPMENT_BUILD || TRACE
                            Debug.LogError($"blast.ssmd.handle_single_op_constant -> operation {ex_op} not supported");
#endif
                            return;
                    }
                    break;

                default:
#if DEVELOPMENT_BUILD || TRACE
                    Debug.LogError($"blast.ssmd.handle_single_op_constant -> operation {op} not supported");
#endif
                    return;

            }

            // apply negate or not to result
            if (is_negated || not)
            {
                if (is_negated)
                {
                    constant = math.select(constant, -constant, is_negated);
                }
                else
                {
                    constant = math.select(0, 1, constant == 0);
                }
            }
        }


        /// <summary>
        /// 
        /// </summary>
        static void handle_single_op_constant_to_register(float4* register, ref byte vector_size, int ssmd_datacount, float constant, blast_operation op, bool is_sequenced)
        {
            if (is_sequenced)
            {
                switch (vector_size)
                {
                    case 0:
                    case 4: handle_op_f4_f1(op, register, constant, ref vector_size, ssmd_datacount); return;
                    case 3: handle_op_f3_f1(op, register, constant, ref vector_size, ssmd_datacount); return;
                    case 2: handle_op_f2_f1(op, register, constant, ref vector_size, ssmd_datacount); return;
                    case 1: handle_op_f1_f1(op, register, constant, ref vector_size, ssmd_datacount); return;
                    default:
#if DEVELOPMENT_BUILD || TRACE
                        Debug.LogError($"blast.ssmd.handle_single_op_constant -> sequence operation {op} not supported");
#endif
                        return;

                }
            }
            else
            {
                // set all registers to the constant
                switch (vector_size)
                {
                    case 0:
                    case 4: simd.set_f4_from_constant(register, constant, ssmd_datacount); return;
                    case 1: simd.set_f1_from_constant(register, constant, ssmd_datacount); return;
                    case 2: simd.set_f2_from_constant(register, constant, ssmd_datacount); return;
                    case 3: simd.set_f3_from_constant(register, constant, ssmd_datacount); return;

                }
            }
        }



        /// <summary>
        /// 
        /// </summary>
        static void handle_single_op_constant_to_target(float4* register, ref byte vector_size, int ssmd_datacount, float constant, blast_operation op, bool is_sequenced, in DATAREC target_rec)
        {
            if (!target_rec.is_set)
            {
#if DEVELOPMENT_BUILD || TRACE
                Debug.LogError($"blast.ssmd.handle_single_op_constant_to_target -> target not set");
#endif
                return;
            }

            if (is_sequenced)
            {
                // op is used in a sequenced operation (mul / add etc. ) 
                switch (vector_size)
                {
                    case 0:
                    case 4: handle_op_f4_f1(op, register, constant, ref vector_size, ssmd_datacount, target_rec); break;
                    case 3: handle_op_f3_f1(op, register, constant, ref vector_size, ssmd_datacount, target_rec); break;
                    case 2: handle_op_f2_f1(op, register, constant, ref vector_size, ssmd_datacount, target_rec); break;
                    case 1: handle_op_f1_f1(op, register, constant, ref vector_size, ssmd_datacount, target_rec); break;
                    default:
#if DEVELOPMENT_BUILD || TRACE
                        Debug.LogError($"blast.ssmd.handle_single_op_constant -> sequence operation {op} not supported");
#endif
                        return;

                }
            }
            else
            {
                // set all registers to the constant
                switch (vector_size)
                {
                    case 0:
                    case 4: simd.move_f1_constant_to_indexed_data_as_f4(target_rec.data, target_rec.row_size, target_rec.is_aligned, target_rec.index, constant, ssmd_datacount); break;
                    case 1: simd.move_f1_constant_to_indexed_data_as_f1(target_rec.data, target_rec.row_size, target_rec.is_aligned, target_rec.index, constant, ssmd_datacount); break;
                    case 2: simd.move_f1_constant_to_indexed_data_as_f2(target_rec.data, target_rec.row_size, target_rec.is_aligned, target_rec.index, constant, ssmd_datacount); break;
                    case 3: simd.move_f1_constant_to_indexed_data_as_f3(target_rec.data, target_rec.row_size, target_rec.is_aligned, target_rec.index, constant, ssmd_datacount); break;

                }
            }
        }



    }
}
