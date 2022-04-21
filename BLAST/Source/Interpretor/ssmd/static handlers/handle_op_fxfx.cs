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

        #region Operations Handlers 

        /// <summary>                    
        /// perform operation: a.x = a.x [op] [sub|not] b.x
        /// </summary>
        static void handle_op_f1_f1(in blast_operation op, [NoAlias] float4* a, [NoAlias] float4* b, ref byte vector_size, int ssmd_datacount)
        {
            vector_size = 1;
            switch (op)
            {
                default:
#if DEVELOPMENT_BUILD || TRACE
                    Debug.LogError($"blast.ssmd.handle_op_fx_fy: operation {op} not handled, vectorsize = {vector_size}, a[0] = {a[0]}, b[0] = {b[0]}");
#endif
                    return;
                case blast_operation.nop: simd.move_f1_array_into_f4_array_as_f1(a, b, ssmd_datacount); break;
                case blast_operation.not: for (int i = 0; i < ssmd_datacount; i++) a[i].x = math.select(1f, 0f, b[i].x != 0); break;

                case blast_operation.add: simd.add_array_f1f1(a, b, ssmd_datacount); return;
                case blast_operation.substract: simd.sub_array_f1f1(a, b, ssmd_datacount); return;
                case blast_operation.multiply: simd.mul_array_f1f1(a, b, ssmd_datacount); return;
                case blast_operation.divide: simd.div_array_f1f1(a, b, ssmd_datacount); return;

                case blast_operation.and: for (int i = 0; i < ssmd_datacount; i++) a[i].x = math.select(0, 1, a[i].x != 0 && b[i].x != 0); return;
                case blast_operation.or: for (int i = 0; i < ssmd_datacount; i++) a[i].x = math.select(0, 1, a[i].x != 0 || b[i].x != 0); return;
                case blast_operation.xor: for (int i = 0; i < ssmd_datacount; i++) a[i].x = math.select(0, 1, a[i].x != 0 ^ b[i].x != 0); return;

                case blast_operation.greater: for (int i = 0; i < ssmd_datacount; i++) a[i].x = math.select(0f, 1f, a[i].x > b[i].x); return;
                case blast_operation.smaller: for (int i = 0; i < ssmd_datacount; i++) a[i].x = math.select(0f, 1f, a[i].x < b[i].x); return;
                case blast_operation.smaller_equals: for (int i = 0; i < ssmd_datacount; i++) a[i].x = math.select(0f, 1f, a[i].x <= b[i].x); return;
                case blast_operation.greater_equals: for (int i = 0; i < ssmd_datacount; i++) a[i].x = math.select(0f, 1f, a[i].x >= b[i].x); return;
                case blast_operation.equals: for (int i = 0; i < ssmd_datacount; i++) a[i].x = math.select(0f, 1f, a[i].x == b[i].x); return;
                case blast_operation.not_equals: for (int i = 0; i < ssmd_datacount; i++) a[i].x = math.select(0f, 1f, a[i].x != b[i].x); return;
            }
        }



        /// <summary>
        /// perform:  a.x = a.x [op] constant
        /// </summary>
        static void handle_op_f1_f1(in blast_operation op, [NoAlias] float4* a, float constant, ref byte vector_size, int ssmd_datacount)
        {
            vector_size = 1;
            switch (op)
            {
                default:
#if DEVELOPMENT_BUILD || TRACE
                    Debug.LogError($"blast.ssmd.handle_op_fx_fy: operation {op} not handled, vectorsize = {vector_size}, a[0] = {a[0]}, constant = {constant}");
#endif
                    return;
                case blast_operation.nop: simd.move_f1_constant_into_f4_array_as_f1(a, constant, ssmd_datacount); break;
                case blast_operation.not: simd.move_f1_constant_into_f4_array_as_f1(a, math.select(0f, 1f, constant == 0), ssmd_datacount); break;

                case blast_operation.add: simd.add_array_f1f1(a, constant, ssmd_datacount); return;
                case blast_operation.substract: simd.sub_array_f1f1(a, constant, ssmd_datacount); return;
                case blast_operation.multiply: simd.mul_array_f1f1(a, constant, ssmd_datacount); return;
                case blast_operation.divide: simd.div_array_f1f1(a, constant, ssmd_datacount); return;

                case blast_operation.and: { bool bconstant = constant != 0; for (int i = 0; i < ssmd_datacount; i++) a[i].x = math.select(0, 1, a[i].x != 0 && bconstant); return; }
                case blast_operation.or: { bool bconstant = constant != 0; for (int i = 0; i < ssmd_datacount; i++) a[i].x = math.select(0, 1, a[i].x != 0 || bconstant); return; }
                case blast_operation.xor: { bool bconstant = constant != 0; for (int i = 0; i < ssmd_datacount; i++) a[i].x = math.select(0, 1, a[i].x != 0 ^ bconstant); return; }

                case blast_operation.greater: for (int i = 0; i < ssmd_datacount; i++) a[i].x = math.select(0f, 1f, a[i].x > constant); return;
                case blast_operation.smaller: for (int i = 0; i < ssmd_datacount; i++) a[i].x = math.select(0f, 1f, a[i].x < constant); return;
                case blast_operation.smaller_equals: for (int i = 0; i < ssmd_datacount; i++) a[i].x = math.select(0f, 1f, a[i].x <= constant); return;
                case blast_operation.greater_equals: for (int i = 0; i < ssmd_datacount; i++) a[i].x = math.select(0f, 1f, a[i].x >= constant); return;
                case blast_operation.equals: for (int i = 0; i < ssmd_datacount; i++) a[i].x = math.select(0f, 1f, a[i].x == constant); return;
                case blast_operation.not_equals: for (int i = 0; i < ssmd_datacount; i++) a[i].x = math.select(0f, 1f, a[i].x != constant); return;
            }
        }

        /// <summary>
        /// perform:  target.x = a.x [op] constant
        /// </summary>
        static void handle_op_f1_f1(in blast_operation op, [NoAlias] float4* a, float constant, ref byte vector_size, int ssmd_datacount, in DATAREC target)
        {
            if (!target.is_set)
            {
#if DEVELOPMENT_BUILD || TRACE
                Debug.LogError($"blast.ssmd.handle_op_fx_fy [constant b]: target not set");
#endif
                return;
            }

            vector_size = 1;
            switch (op)
            {
                default:
#if DEVELOPMENT_BUILD || TRACE
                    Debug.LogError($"blast.ssmd.handle_op_fx_fy: operation {op} not handled, vectorsize = {vector_size}, a[0] = {a[0]}, constant = {constant}");
#endif
                    break;

                case blast_operation.nop: simd.move_f1_constant_to_indexed_data_as_f1(target.data, target.row_size, target.is_aligned, target.index, constant, ssmd_datacount); break;
                case blast_operation.not: simd.move_f1_constant_to_indexed_data_as_f1(target.data, target.row_size, target.is_aligned, target.index, math.select(0f, 1f, constant == 0), ssmd_datacount); break;

                case blast_operation.add: simd.add_array_f1f1(a, constant, target, ssmd_datacount); return;
                case blast_operation.substract: simd.sub_array_f1f1(a, constant, target, ssmd_datacount); return;
                case blast_operation.multiply: simd.mul_array_f1f1(a, constant, target, ssmd_datacount); return;
                case blast_operation.divide: simd.div_array_f1f1(a, constant, target, ssmd_datacount); return;

                case blast_operation.and: { bool bconstant = constant != 0; for (int i = 0; i < ssmd_datacount; i++) ((float**)target.data)[i][target.index] = math.select(0, 1, a[i].x != 0 && bconstant); } break;
                case blast_operation.or: { bool bconstant = constant != 0; for (int i = 0; i < ssmd_datacount; i++) ((float**)target.data)[i][target.index] = math.select(0, 1, a[i].x != 0 || bconstant); } break;
                case blast_operation.xor: { bool bconstant = constant != 0; for (int i = 0; i < ssmd_datacount; i++) ((float**)target.data)[i][target.index] = math.select(0, 1, a[i].x != 0 ^ bconstant); } break;

                case blast_operation.greater: for (int i = 0; i < ssmd_datacount; i++) ((float**)target.data)[i][target.index] = math.select(0f, 1f, a[i].x > constant); break;
                case blast_operation.smaller: for (int i = 0; i < ssmd_datacount; i++) ((float**)target.data)[i][target.index] = math.select(0f, 1f, a[i].x < constant); break;
                case blast_operation.smaller_equals: for (int i = 0; i < ssmd_datacount; i++) ((float**)target.data)[i][target.index] = math.select(0f, 1f, a[i].x <= constant); break;
                case blast_operation.greater_equals: for (int i = 0; i < ssmd_datacount; i++) ((float**)target.data)[i][target.index] = math.select(0f, 1f, a[i].x >= constant); break;
                case blast_operation.equals: for (int i = 0; i < ssmd_datacount; i++) ((float**)target.data)[i][target.index] = math.select(0f, 1f, a[i].x == constant); break;
                case blast_operation.not_equals: for (int i = 0; i < ssmd_datacount; i++) ((float**)target.data)[i][target.index] = math.select(0f, 1f, a[i].x != constant); break;
            }
        }

        /// <summary>
        /// perform:  target.xy = a.x [op] [sub|not] b.xy
        /// </summary>
        static void handle_op_f1_f2(in blast_operation op, [NoAlias] float4* a, [NoAlias] float4* b, ref byte vector_size, int ssmd_datacount)
        {
            vector_size = 2;
            switch (op)
            {
                default:
#if DEVELOPMENT_BUILD || TRACE
                    Debug.LogError($"blast.ssmd.handle_op_fx_fy: operation {op} not handled, vectorsize = {vector_size}, a[0] = {a[0]}, b[0] = {b[0]}");
#endif
                    return;
                case blast_operation.nop: simd.move_f2_array_into_f4_array_as_f2(a, b, ssmd_datacount); break; // for (int i = 0; i < ssmd_datacount; i++) a[i].xy = b[i].xy; break;
                case blast_operation.not: for (int i = 0; i < ssmd_datacount; i++) a[i].xy = math.select((float2)1, (float2)0, b[i].xy != 0); break;

                case blast_operation.add: simd.add_array_f1f2(a, b, ssmd_datacount); return;
                case blast_operation.substract: simd.sub_array_f1f2(a, b, ssmd_datacount); return;
                case blast_operation.multiply: simd.mul_array_f1f2(a, b, ssmd_datacount); return;
                case blast_operation.divide: simd.div_array_f1f2(a, b, ssmd_datacount); return;

                case blast_operation.and: for (int i = 0; i < ssmd_datacount; i++) a[i].xy = math.select(0, 1, a[i].x != 0 && math.any(b[i].xy)); return;
                case blast_operation.or: for (int i = 0; i < ssmd_datacount; i++) a[i].xy = math.select(0, 1, a[i].x != 0 || math.any(b[i].xy)); return;
                case blast_operation.xor: for (int i = 0; i < ssmd_datacount; i++) a[i].xy = math.select(0, 1, a[i].x != 0 ^ math.any(b[i].xy)); return;

                case blast_operation.greater: for (int i = 0; i < ssmd_datacount; i++) a[i].xy = math.select(0f, 1f, a[i].x > b[i].xy); return;
                case blast_operation.smaller: for (int i = 0; i < ssmd_datacount; i++) a[i].xy = math.select(0f, 1f, a[i].x < b[i].xy); return;
                case blast_operation.smaller_equals: for (int i = 0; i < ssmd_datacount; i++) a[i].xy = math.select(0f, 1f, a[i].x <= b[i].xy); return;
                case blast_operation.greater_equals: for (int i = 0; i < ssmd_datacount; i++) a[i].xy = math.select(0f, 1f, a[i].x >= b[i].xy); return;
                case blast_operation.equals: for (int i = 0; i < ssmd_datacount; i++) a[i].xy = math.select(0f, 1f, a[i].x == b[i].xy); return;
                case blast_operation.not_equals: for (int i = 0; i < ssmd_datacount; i++) a[i].xy = math.select(0f, 1f, a[i].x != b[i].xy); return;
            }
        }

        /// <summary>
        /// perform:  target.x = a.x [op] [sub|not] b.x
        /// </summary>
        static void handle_op_f1_f1(in blast_operation op, [NoAlias] float4* a, [NoAlias] float4* b, ref byte vector_size, int ssmd_datacount, in bool minus, in bool not, in DATAREC target)
        {
            if (!target.is_set)
            {
#if DEVELOPMENT_BUILD || TRACE
                Debug.LogError($"blast.ssmd.handle_op_fx_fy: target not set");
#endif
                return;
            }

            vector_size = 1;
            switch (op)
            {
                default:
#if DEVELOPMENT_BUILD || TRACE
                    Debug.LogError($"blast.ssmd.handle_op_fx_fy: operation {op} not handled, vectorsize = {vector_size}, a[0] = {a[0]}, b[0] = {b[0]} target index = {target.index}");
#endif
                    return;

                case blast_operation.nop:
                    if (minus) simd.move_f4_array_to_indexed_data_as_f1(target.data, target.row_size, target.is_aligned, target.index, b, ssmd_datacount);
                    else simd.move_f4_array_to_indexed_data_as_f1_negated(target.data, target.row_size, target.is_aligned, target.index, b, ssmd_datacount);
                    break;

                case blast_operation.add:
                    if (minus) simd.sub_array_f1f1(a, b, target, ssmd_datacount);
                    else simd.add_array_f1f1(a, b, target, ssmd_datacount);
                    break;

                case blast_operation.multiply:
                    if (minus) simd.mul_array_f1f1_negated(a, b, target, ssmd_datacount);
                    else simd.mul_array_f1f1(a, b, target, ssmd_datacount);
                    break;

                case blast_operation.divide:
                    if (minus) simd.div_array_f1f1_negated(a, b, target, ssmd_datacount);
                    else simd.div_array_f1f1(a, b, target, ssmd_datacount);
                    break;

                case blast_operation.substract:
                    if (minus) simd.add_array_f1f1(a, b, target, ssmd_datacount);
                    else simd.sub_array_f1f1(a, b, target, ssmd_datacount);
                    break;

                case blast_operation.not:
                    if (!not)
                    {
                        // only set if not a double not 
                        for (int i = 0; i < ssmd_datacount; i++) ((float**)target.data)[i][target.index] = math.select(1f, 0f, b[i].x != 0);
                    }
                    break;

                case blast_operation.and:
                    if (not)
                    {
                        // the not applies to b[]
                        for (int i = 0; i < ssmd_datacount; i++) ((float**)target.data)[i][target.index] = math.select(0, 1, a[i].x != 0 && b[i].x == 0);
                    }
                    else
                    {
                        for (int i = 0; i < ssmd_datacount; i++) ((float**)target.data)[i][target.index] = math.select(0, 1, a[i].x != 0 && b[i].x != 0);
                    }
                    break;

                case blast_operation.or:
                    if (not)
                    {
                        for (int i = 0; i < ssmd_datacount; i++) ((float**)target.data)[i][target.index] = math.select(0, 1, a[i].x != 0 || b[i].x == 0);
                    }
                    else
                    {
                        for (int i = 0; i < ssmd_datacount; i++) ((float**)target.data)[i][target.index] = math.select(0, 1, a[i].x != 0 || b[i].x != 0);
                    }
                    break;

                case blast_operation.xor:
                    if (not)
                    {
                        for (int i = 0; i < ssmd_datacount; i++) ((float**)target.data)[i][target.index] = math.select(0, 1, a[i].x != 0 ^ b[i].x == 0);
                    }
                    else
                    {
                        for (int i = 0; i < ssmd_datacount; i++) ((float**)target.data)[i][target.index] = math.select(0, 1, a[i].x != 0 ^ b[i].x != 0);
                    }
                    break;

                case blast_operation.greater:
                    if (minus)
                    {
                        for (int i = 0; i < ssmd_datacount; i++) ((float**)target.data)[i][target.index] = math.select(0f, 1f, a[i].x > -b[i].x);
                    }
                    else
                    {
                        for (int i = 0; i < ssmd_datacount; i++) ((float**)target.data)[i][target.index] = math.select(0f, 1f, a[i].x > b[i].x);
                    }
                    break;

                case blast_operation.smaller:
                    if (minus)
                    {
                        for (int i = 0; i < ssmd_datacount; i++) ((float**)target.data)[i][target.index] = math.select(0f, 1f, a[i].x < -b[i].x);
                    }
                    else
                    {
                        for (int i = 0; i < ssmd_datacount; i++) ((float**)target.data)[i][target.index] = math.select(0f, 1f, a[i].x < b[i].x);
                    }
                    break;

                case blast_operation.smaller_equals:
                    if (minus)
                    {
                        for (int i = 0; i < ssmd_datacount; i++) ((float**)target.data)[i][target.index] = math.select(0f, 1f, a[i].x <= -b[i].x);
                    }
                    else
                    {
                        for (int i = 0; i < ssmd_datacount; i++) ((float**)target.data)[i][target.index] = math.select(0f, 1f, a[i].x <= b[i].x);
                    }
                    break;

                case blast_operation.greater_equals:
                    if (minus)
                    {
                        for (int i = 0; i < ssmd_datacount; i++) ((float**)target.data)[i][target.index] = math.select(0f, 1f, a[i].x >= -b[i].x);
                    }
                    else
                    {
                        for (int i = 0; i < ssmd_datacount; i++) ((float**)target.data)[i][target.index] = math.select(0f, 1f, a[i].x >= b[i].x);
                    }
                    break;

                case blast_operation.equals:
                    if (not)
                    {
                        for (int i = 0; i < ssmd_datacount; i++) ((float**)target.data)[i][target.index] = math.select(0f, 1f, a[i].x == math.select(0, 1, b[i].x == 0));
                    }
                    if (minus)
                    {
                        for (int i = 0; i < ssmd_datacount; i++) ((float**)target.data)[i][target.index] = math.select(0f, 1f, a[i].x == -b[i].x);
                    }
                    else
                    {
                        for (int i = 0; i < ssmd_datacount; i++) ((float**)target.data)[i][target.index] = math.select(0f, 1f, a[i].x == b[i].x);
                    }
                    break;

                case blast_operation.not_equals:
                    if (not)
                    {
                        for (int i = 0; i < ssmd_datacount; i++) ((float**)target.data)[i][target.index] = math.select(0f, 1f, a[i].x != math.select(0, 1, b[i].x == 0));
                    }
                    if (minus)
                    {
                        for (int i = 0; i < ssmd_datacount; i++) ((float**)target.data)[i][target.index] = math.select(0f, 1f, a[i].x != -b[i].x);
                    }
                    else
                    {
                        for (int i = 0; i < ssmd_datacount; i++) ((float**)target.data)[i][target.index] = math.select(0f, 1f, a[i].x != b[i].x);
                    }
                    break;
            }
        }

        /// <summary>
        /// perform:  target.xyz = a.x [op] [minus|not] b.xyz
        /// </summary>
        static void handle_op_f1_f2(in blast_operation op, [NoAlias] float4* a, [NoAlias] float4* b, ref byte vector_size, int ssmd_datacount, in bool minus, in bool not, in DATAREC target)
        {
            if (!target.is_set)
            {
#if DEVELOPMENT_BUILD || TRACE
                Debug.LogError($"blast.ssmd.handle_op_f1_f2: target not set");
#endif
                return;
            }

            vector_size = 2;
            switch (op)
            {
                default:
#if DEVELOPMENT_BUILD || TRACE
                    Debug.LogError($"blast.ssmd.handle_op_f1_f2: operation {op} not handled, vectorsize = {vector_size}, a[0] = {a[0]}, b[0] = {b[0]}");
#endif
                    return;

                case blast_operation.nop:
                    if (minus) simd.move_f4_array_to_indexed_data_as_f2(target.data, target.row_size, target.is_aligned, target.index, b, ssmd_datacount);
                    else simd.move_f4_array_to_indexed_data_as_f2_negated(target.data, target.row_size, target.is_aligned, target.index, b, ssmd_datacount);
                    break;

                case blast_operation.add:
                    if (minus) simd.sub_array_f1f2(a, b, target, ssmd_datacount);
                    else simd.add_array_f1f2(a, b, target, ssmd_datacount);
                    break;

                case blast_operation.multiply:
                    if (minus) simd.mul_array_f1f2_negated(a, b, target, ssmd_datacount);
                    else simd.mul_array_f1f2(a, b, target, ssmd_datacount);
                    break;

                case blast_operation.divide:
                    if (minus) simd.div_array_f1f2_negated(a, b, target, ssmd_datacount);
                    else simd.div_array_f1f2(a, b, target, ssmd_datacount);
                    break;

                case blast_operation.substract:
                    if (minus) simd.add_array_f1f2(a, b, target, ssmd_datacount);
                    else simd.sub_array_f1f2(a, b, target, ssmd_datacount);
                    break;

                case blast_operation.not:
                    if (!not)
                    {
                        // only set if not a double not 
                        for (int i = 0; i < ssmd_datacount; i++) ((float2*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(1f, 0f, b[i].xy != 0);
                    }
                    break;

                case blast_operation.and:
                    if (not)
                    {
                        // the not applies to b[]
                        for (int i = 0; i < ssmd_datacount; i++) ((float2*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].x != 0 && b[i].x == 0 && b[i].y == 0);
                    }
                    else
                    {
                        for (int i = 0; i < ssmd_datacount; i++) ((float2*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].x != 0 && b[i].x != 0 && b[i].y != 0);
                    }
                    break;

                case blast_operation.or:
                    if (not)
                    {
                        for (int i = 0; i < ssmd_datacount; i++) ((float2*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].x != 0 || b[i].x == 0 || b[i].y == 0);
                    }
                    else
                    {
                        for (int i = 0; i < ssmd_datacount; i++) ((float2*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].x != 0 || b[i].x != 0 || b[i].y != 0);
                    }
                    break;

                case blast_operation.xor:
                    if (not)
                    {
                        for (int i = 0; i < ssmd_datacount; i++) ((float2*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].x != 0 ^ b[i].xy == 0);
                    }
                    else
                    {
                        for (int i = 0; i < ssmd_datacount; i++) ((float2*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].x != 0 ^ b[i].xy != 0);
                    }
                    break;

                case blast_operation.greater:
                    if (minus)
                    {
                        for (int i = 0; i < ssmd_datacount; i++) ((float2*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].x > -b[i].xy);
                    }
                    else
                    {
                        for (int i = 0; i < ssmd_datacount; i++) ((float2*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].x > b[i].xy);
                    }
                    break;

                case blast_operation.smaller:
                    if (minus)
                    {
                        for (int i = 0; i < ssmd_datacount; i++) ((float2*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].x < -b[i].xy);
                    }
                    else
                    {
                        for (int i = 0; i < ssmd_datacount; i++) ((float2*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].x < b[i].xy);
                    }
                    break;

                case blast_operation.smaller_equals:
                    if (minus)
                    {
                        for (int i = 0; i < ssmd_datacount; i++) ((float2*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].x <= -b[i].xy);
                    }
                    else
                    {
                        for (int i = 0; i < ssmd_datacount; i++) ((float2*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].x <= b[i].xy);
                    }
                    break;

                case blast_operation.greater_equals:
                    if (minus)
                    {
                        for (int i = 0; i < ssmd_datacount; i++) ((float2*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].x >= -b[i].xy);
                    }
                    else
                    {
                        for (int i = 0; i < ssmd_datacount; i++) ((float2*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].x >= b[i].xy);
                    }
                    break;

                case blast_operation.equals:
                    if (not)
                    {
                        for (int i = 0; i < ssmd_datacount; i++) ((float2*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].x != b[i].xy);
                    }
                    if (minus)
                    {
                        for (int i = 0; i < ssmd_datacount; i++) ((float2*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].x == -b[i].xy);
                    }
                    else
                    {
                        for (int i = 0; i < ssmd_datacount; i++) ((float2*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].x == b[i].xy);
                    }
                    break;

                case blast_operation.not_equals:
                    if (not)
                    {
                        for (int i = 0; i < ssmd_datacount; i++) ((float2*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].x != math.select(0f, 1f, b[i].xy == 0));
                    }
                    if (minus)
                    {
                        for (int i = 0; i < ssmd_datacount; i++) ((float2*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].x != -b[i].xy);
                    }
                    else
                    {
                        for (int i = 0; i < ssmd_datacount; i++) ((float2*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].x != b[i].xy);
                    }
                    break;
            }
        }

        /// <summary>
        /// perform:  target.xyz = a.x [op] [minus|not] b.xyz
        /// </summary>
        static void handle_op_f1_f3(in blast_operation op, [NoAlias] float4* a, [NoAlias] float4* b, ref byte vector_size, int ssmd_datacount, in bool minus, in bool not, in DATAREC target)
        {
            if (!target.is_set)
            {
#if DEVELOPMENT_BUILD || TRACE
                Debug.LogError($"blast.ssmd.handle_op_f1_f3: target not set");
#endif
                return;
            }

            vector_size = 3;
            switch (op)
            {
                default:
#if DEVELOPMENT_BUILD || TRACE
                    Debug.LogError($"blast.ssmd.handle_op_f1_f3: operation {op} not handled, vectorsize = {vector_size}, a[0] = {a[0]}, b[0] = {b[0]}");
#endif
                    return;
                case blast_operation.nop:
                    if (minus) simd.move_f4_array_to_indexed_data_as_f3(target.data, target.row_size, target.is_aligned, target.index, b, ssmd_datacount);
                    else simd.move_f4_array_to_indexed_data_as_f3_negated(target.data, target.row_size, target.is_aligned, target.index, b, ssmd_datacount);
                    break;

                case blast_operation.add:
                    if (minus) simd.sub_array_f1f3(a, b, target, ssmd_datacount);
                    else simd.add_array_f1f3(a, b, target, ssmd_datacount);
                    break;

                case blast_operation.multiply:
                    if (minus) simd.mul_array_f1f3_negated(a, b, target, ssmd_datacount);
                    else simd.mul_array_f1f3(a, b, target, ssmd_datacount);
                    break;

                case blast_operation.divide:
                    if (minus) simd.div_array_f1f3_negated(a, b, target, ssmd_datacount);
                    else simd.div_array_f1f3(a, b, target, ssmd_datacount);
                    break;

                case blast_operation.substract:
                    if (minus) simd.add_array_f1f3(a, b, target, ssmd_datacount);
                    else simd.sub_array_f1f3(a, b, target, ssmd_datacount);
                    break;

                case blast_operation.not:
                    if (!not)
                    {
                        // only set if not a double not 
                        for (int i = 0; i < ssmd_datacount; i++) ((float3*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(1f, 0f, b[i].xyz != 0);
                    }
                    break;

                case blast_operation.and:
                    if (not)
                    {
                        // the not applies to b[]
                        for (int i = 0; i < ssmd_datacount; i++) ((float3*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].x != 0 && b[i].x == 0 && b[i].y == 0 && b[i].z == 0);
                    }
                    else
                    {
                        for (int i = 0; i < ssmd_datacount; i++) ((float3*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].x != 0 && b[i].x != 0 && b[i].y != 0 && b[i].z != 0);
                    }
                    break;

                case blast_operation.or:
                    if (not)
                    {
                        for (int i = 0; i < ssmd_datacount; i++) ((float3*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].x != 0 || b[i].x == 0 || b[i].y == 0 || b[i].z == 0);
                    }
                    else
                    {
                        for (int i = 0; i < ssmd_datacount; i++) ((float3*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].x != 0 || b[i].x != 0 || b[i].y != 0 || b[i].z != 0);
                    }
                    break;

                case blast_operation.xor:
                    if (not)
                    {
                        for (int i = 0; i < ssmd_datacount; i++) ((float3*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].x != 0 ^ (b[i].xyz == 0));
                    }
                    else
                    {
                        for (int i = 0; i < ssmd_datacount; i++) ((float3*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].x != 0 ^ (b[i].xyz != 0));
                    }
                    break;

                case blast_operation.greater:
                    if (minus)
                    {
                        for (int i = 0; i < ssmd_datacount; i++) ((float3*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].x > -b[i].xyz);
                    }
                    else
                    {
                        for (int i = 0; i < ssmd_datacount; i++) ((float3*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].x > b[i].xyz);
                    }
                    break;

                case blast_operation.smaller:
                    if (minus)
                    {
                        for (int i = 0; i < ssmd_datacount; i++) ((float3*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].x < -b[i].xyz);
                    }
                    else
                    {
                        for (int i = 0; i < ssmd_datacount; i++) ((float3*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].x < b[i].xyz);
                    }
                    break;

                case blast_operation.smaller_equals:
                    if (minus)
                    {
                        for (int i = 0; i < ssmd_datacount; i++) ((float3*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].x <= -b[i].xyz);
                    }
                    else
                    {
                        for (int i = 0; i < ssmd_datacount; i++) ((float3*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].x <= b[i].xyz);
                    }
                    break;

                case blast_operation.greater_equals:
                    if (minus)
                    {
                        for (int i = 0; i < ssmd_datacount; i++) ((float3*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].x >= -b[i].xyz);
                    }
                    else
                    {
                        for (int i = 0; i < ssmd_datacount; i++) ((float3*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].x >= b[i].xyz);
                    }
                    break;

                case blast_operation.equals:
                    if (not)
                    {
                        for (int i = 0; i < ssmd_datacount; i++) ((float3*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].x == math.select(0f, 1f, b[i].xyz == 0));
                    }
                    if (minus)
                    {
                        for (int i = 0; i < ssmd_datacount; i++) ((float3*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].x == -b[i].xyz);
                    }
                    else
                    {
                        for (int i = 0; i < ssmd_datacount; i++) ((float3*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].x == b[i].xyz);
                    }
                    break;

                case blast_operation.not_equals:
                    if (not)
                    {
                        for (int i = 0; i < ssmd_datacount; i++) ((float3*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].x != math.select(0f, 1f, b[i].xyz == 0));
                    }
                    if (minus)
                    {
                        for (int i = 0; i < ssmd_datacount; i++) ((float3*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].x != -b[i].xyz);
                    }
                    else
                    {
                        for (int i = 0; i < ssmd_datacount; i++) ((float3*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].x != b[i].xyz);
                    }
                    break;
            }
        }

        /// <summary>
        /// perform:  target.xyzw = a.x [op] [minus|not] b.xyzw
        /// </summary>
        static void handle_op_f1_f4(in blast_operation op, [NoAlias] float4* a, [NoAlias] float4* b, ref byte vector_size, int ssmd_datacount, in bool minus, in bool not, in DATAREC target)
        {
            if (!target.is_set)
            {
#if DEVELOPMENT_BUILD || TRACE
                Debug.LogError($"blast.ssmd.handle_op_f1_f4: target not set");
#endif
                return;
            }

            vector_size = 4;
            switch (op)
            {
                default:
#if DEVELOPMENT_BUILD || TRACE
                    Debug.LogError($"blast.ssmd.handle_op_f1_f4: operation {op} not handled, vectorsize = {vector_size}, a[0] = {a[0]}, b[0] = {b[0]}");
#endif
                    return;
                case blast_operation.nop:
                    if (minus) simd.move_f4_array_to_indexed_data_as_f4(target.data, target.row_size, target.is_aligned, target.index, b, ssmd_datacount);
                    else simd.move_f4_array_to_indexed_data_as_f4_negated(target.data, target.row_size, target.is_aligned, target.index, b, ssmd_datacount);
                    break;

                case blast_operation.add:
                    if (minus) simd.sub_array_f1f4(a, b, target, ssmd_datacount);
                    else simd.add_array_f1f4(a, b, target, ssmd_datacount);
                    break;

                case blast_operation.multiply:
                    if (minus) simd.mul_array_f1f4_negated(a, b, target, ssmd_datacount);
                    else simd.mul_array_f1f4(a, b, target, ssmd_datacount);
                    break;

                case blast_operation.divide:
                    if (minus) simd.div_array_f1f4_negated(a, b, target, ssmd_datacount);
                    else simd.div_array_f1f4(a, b, target, ssmd_datacount);
                    break;

                case blast_operation.substract:
                    if (minus) simd.add_array_f1f4(a, b, target, ssmd_datacount);
                    else simd.sub_array_f1f4(a, b, target, ssmd_datacount);
                    break;

                case blast_operation.not:
                    if (!not)
                    {
                        // only set if not a double not 
                        for (int i = 0; i < ssmd_datacount; i++) ((float4*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(1f, 0f, b[i].xyzw != 0);
                    }
                    break;

                case blast_operation.and:
                    if (not)
                    {
                        // the not applies to b[]
                        for (int i = 0; i < ssmd_datacount; i++) ((float4*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].x != 0 && !math.any(b[i]));
                    }
                    else
                    {
                        for (int i = 0; i < ssmd_datacount; i++) ((float4*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].x != 0 && math.any(b[i]));
                    }
                    break;

                case blast_operation.or:
                    if (not)
                    {
                        for (int i = 0; i < ssmd_datacount; i++) ((float4*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].x != 0 || !math.any(b[i]));
                    }
                    else
                    {
                        for (int i = 0; i < ssmd_datacount; i++) ((float4*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].x != 0 || math.any(b[i]));
                    }
                    break;

                case blast_operation.xor:
                    if (not)
                    {
                        for (int i = 0; i < ssmd_datacount; i++) ((float4*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].x != 0 ^ (b[i] == 0));
                    }
                    else
                    {
                        for (int i = 0; i < ssmd_datacount; i++) ((float4*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].x != 0 ^ (b[i] != 0));
                    }
                    break;

                case blast_operation.greater:
                    if (minus)
                    {
                        for (int i = 0; i < ssmd_datacount; i++) ((float4*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].x > -b[i]);
                    }
                    else
                    {
                        for (int i = 0; i < ssmd_datacount; i++) ((float4*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].x > b[i]);
                    }
                    break;

                case blast_operation.smaller:
                    if (minus)
                    {
                        for (int i = 0; i < ssmd_datacount; i++) ((float4*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].x < -b[i]);
                    }
                    else
                    {
                        for (int i = 0; i < ssmd_datacount; i++) ((float4*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].x < b[i]);
                    }
                    break;

                case blast_operation.smaller_equals:
                    if (minus)
                    {
                        for (int i = 0; i < ssmd_datacount; i++) ((float4*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].x <= -b[i]);
                    }
                    else
                    {
                        for (int i = 0; i < ssmd_datacount; i++) ((float4*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].x <= b[i]);
                    }
                    break;

                case blast_operation.greater_equals:
                    if (minus)
                    {
                        for (int i = 0; i < ssmd_datacount; i++) ((float4*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].x >= -b[i]);
                    }
                    else
                    {
                        for (int i = 0; i < ssmd_datacount; i++) ((float4*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].x >= b[i]);
                    }
                    break;

                case blast_operation.equals:
                    if (not)
                    {
                        for (int i = 0; i < ssmd_datacount; i++) ((float4*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].x == math.select(0f, 1f, b[i] == 0));
                    }
                    if (minus)
                    {
                        for (int i = 0; i < ssmd_datacount; i++) ((float4*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].x == -b[i]);
                    }
                    else
                    {
                        for (int i = 0; i < ssmd_datacount; i++) ((float4*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].x == b[i]);
                    }
                    break;

                case blast_operation.not_equals:
                    if (not)
                    {
                        for (int i = 0; i < ssmd_datacount; i++) ((float4*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].x != math.select(0f, 1f, b[i] == 0));
                    }
                    if (minus)
                    {
                        for (int i = 0; i < ssmd_datacount; i++) ((float4*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].x != -b[i]);
                    }
                    else
                    {
                        for (int i = 0; i < ssmd_datacount; i++) ((float4*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].x != b[i]);
                    }
                    break;
            }
        }

        /// <summary>
        /// perform:  target.xyz = a.x [op] b.xyz
        /// </summary>
        static void handle_op_f1_f3(in blast_operation op, [NoAlias] float4* a, [NoAlias] float4* b, ref byte vector_size, int ssmd_datacount)
        {
            vector_size = 3;
            switch (op)
            {
                default:
#if DEVELOPMENT_BUILD || TRACE
                    Debug.LogError($"blast.ssmd.handle_op_fx_fy: operation {op} not handled, vectorsize = {vector_size}, a[0] = {a[0]}, b[0] = {b[0]}");
#endif
                    return;
                case blast_operation.nop: simd.move_f1_array_into_f4_array_as_f3(a, b, ssmd_datacount); break;
                case blast_operation.not: for (int i = 0; i < ssmd_datacount; i++) a[i].xyz = math.select((float3)1, (float3)0, b[i].xyz != 0); break;

                case blast_operation.add: simd.add_array_f1f3(a, b, ssmd_datacount); return;
                case blast_operation.substract: simd.sub_array_f1f3(a, b, ssmd_datacount); return;
                case blast_operation.multiply: simd.mul_array_f1f3(a, b, ssmd_datacount); return;
                case blast_operation.divide: simd.div_array_f1f3(a, b, ssmd_datacount); return;

                case blast_operation.and: for (int i = 0; i < ssmd_datacount; i++) a[i].xyz = math.select(0, 1, a[i].x != 0 && math.any(b[i].xyz)); return;
                case blast_operation.or: for (int i = 0; i < ssmd_datacount; i++) a[i].xyz = math.select(0, 1, a[i].x != 0 || math.any(b[i].xyz)); return;
                case blast_operation.xor: for (int i = 0; i < ssmd_datacount; i++) a[i].xyz = math.select(0, 1, a[i].x != 0 ^ math.any(b[i].xyz)); return;

                case blast_operation.greater: for (int i = 0; i < ssmd_datacount; i++) a[i].xyz = math.select(0f, 1f, a[i].x > b[i].xyz); return;
                case blast_operation.smaller: for (int i = 0; i < ssmd_datacount; i++) a[i].xyz = math.select(0f, 1f, a[i].x < b[i].xyz); return;
                case blast_operation.smaller_equals: for (int i = 0; i < ssmd_datacount; i++) a[i].xyz = math.select(0f, 1f, a[i].x <= b[i].xyz); return;
                case blast_operation.greater_equals: for (int i = 0; i < ssmd_datacount; i++) a[i].xyz = math.select(0f, 1f, a[i].x >= b[i].xyz); return;
                case blast_operation.equals: for (int i = 0; i < ssmd_datacount; i++) a[i].xyz = math.select(0f, 1f, a[i].x == b[i].xyz); return;
                case blast_operation.not_equals: for (int i = 0; i < ssmd_datacount; i++) a[i].xyz = math.select(0f, 1f, a[i].x != b[i].xyz); return;
            }
        }
        static void handle_op_f1_f4(in blast_operation op, [NoAlias] float4* a, [NoAlias] float4* b, ref byte vector_size, int ssmd_datacount)
        {
            vector_size = 4;
            switch (op)
            {
                default:
#if DEVELOPMENT_BUILD || TRACE
                    Debug.LogError($"blast.ssmd.handle_op_fx_fy: operation {op} not handled, vectorsize = {vector_size}, a[0] = {a[0]}, b[0] = {b[0]}");
#endif
                    return;
                case blast_operation.nop: simd.move_f1_array_into_f4_array_as_f4(a, b, ssmd_datacount); break; // for (int i = 0; i < ssmd_datacount; i++) a[i] = b[i]; break;
                case blast_operation.not: for (int i = 0; i < ssmd_datacount; i++) a[i] = math.select((float4)1, (float4)0, b[i] != 0); break;

                case blast_operation.add: simd.add_array_f1f4(a, b, ssmd_datacount); return;
                case blast_operation.substract: simd.sub_array_f1f4(a, b, ssmd_datacount); return;
                case blast_operation.multiply: simd.mul_array_f1f4(a, b, ssmd_datacount); return;
                case blast_operation.divide: simd.div_array_f1f4(a, b, ssmd_datacount); return;

                case blast_operation.and: for (int i = 0; i < ssmd_datacount; i++) a[i] = math.select(0, 1, a[i].x != 0 && math.any(b[i])); return;
                case blast_operation.or: for (int i = 0; i < ssmd_datacount; i++) a[i] = math.select(0, 1, a[i].x != 0 || math.any(b[i])); return;
                case blast_operation.xor: for (int i = 0; i < ssmd_datacount; i++) a[i] = math.select(0, 1, a[i].x != 0 ^ math.any(b[i])); return;

                case blast_operation.greater: for (int i = 0; i < ssmd_datacount; i++) a[i] = math.select(0f, 1f, a[i].x > b[i]); return;
                case blast_operation.smaller: for (int i = 0; i < ssmd_datacount; i++) a[i] = math.select(0f, 1f, a[i].x < b[i]); return;
                case blast_operation.smaller_equals: for (int i = 0; i < ssmd_datacount; i++) a[i] = math.select(0f, 1f, a[i].x <= b[i]); return;
                case blast_operation.greater_equals: for (int i = 0; i < ssmd_datacount; i++) a[i] = math.select(0f, 1f, a[i].x >= b[i]); return;
                case blast_operation.equals: for (int i = 0; i < ssmd_datacount; i++) a[i] = math.select(0f, 1f, a[i].x == b[i]); return;
                case blast_operation.not_equals: for (int i = 0; i < ssmd_datacount; i++) a[i] = math.select(0f, 1f, a[i].x != b[i]); return;
            }
        }
        static void handle_op_f2_f1(in blast_operation op, [NoAlias] float4* a, [NoAlias] float4* b, ref byte vector_size, int ssmd_datacount)
        {
            vector_size = 2;
            switch (op)
            {
                default:
#if DEVELOPMENT_BUILD || TRACE
                    Debug.LogError($"blast.ssmd.handle_op_fx_fy: operation {op} not handled, vectorsize = {vector_size}, a[0] = {a[0]}, b[0] = {b[0]}");
#endif
                    return;

                //case blast_operation.nop: 
                case blast_operation.nop: simd.move_f1_array_into_f4_array_as_f2(a, b, ssmd_datacount); break;

                case blast_operation.not: for (int i = 0; i < ssmd_datacount; i++) a[i].xy = math.select((float2)1, (float2)0, b[i].x != 0); break;

                case blast_operation.add: simd.add_array_f2f1(a, b, ssmd_datacount); return;
                case blast_operation.substract: simd.sub_array_f2f1(a, b, ssmd_datacount); return;
                case blast_operation.multiply: simd.mul_array_f2f1(a, b, ssmd_datacount); return;
                case blast_operation.divide: simd.div_array_f2f1(a, b, ssmd_datacount); return;

                case blast_operation.and: for (int i = 0; i < ssmd_datacount; i++) a[i].xy = math.select(0, 1, math.any(a[i].xy) && b[i].x != 0); return;
                case blast_operation.or: for (int i = 0; i < ssmd_datacount; i++) a[i].xy = math.select(0, 1, math.any(a[i].xy) || b[i].x != 0); return;
                case blast_operation.xor: for (int i = 0; i < ssmd_datacount; i++) a[i].xy = math.select(0, 1, math.any(a[i].xy) ^ b[i].x != 0); return;

                case blast_operation.greater: for (int i = 0; i < ssmd_datacount; i++) a[i].xy = math.select(0f, 1f, a[i].xy > b[i].x); return;
                case blast_operation.smaller: for (int i = 0; i < ssmd_datacount; i++) a[i].xy = math.select(0f, 1f, a[i].xy < b[i].x); return;
                case blast_operation.smaller_equals: for (int i = 0; i < ssmd_datacount; i++) a[i].xy = math.select(0f, 1f, a[i].xy <= b[i].x); return;
                case blast_operation.greater_equals: for (int i = 0; i < ssmd_datacount; i++) a[i].xy = math.select(0f, 1f, a[i].xy >= b[i].x); return;
                case blast_operation.equals: for (int i = 0; i < ssmd_datacount; i++) a[i].xy = math.select(0f, 1f, a[i].xy == b[i].x); return;
                case blast_operation.not_equals: for (int i = 0; i < ssmd_datacount; i++) a[i].xy = math.select(0f, 1f, a[i].xy != b[i].x); return;
            }
        }



        /// <summary>
        /// perform: [][].xy = a[].xy * b[].x
        /// </summary>
        static void handle_op_f2_f1(in blast_operation op, [NoAlias] float4* a, [NoAlias] float4* b, ref byte vector_size, int ssmd_datacount, in bool minus, in bool not, in DATAREC target)
        {
            if (!target.is_set)
            {
#if DEVELOPMENT_BUILD || TRACE
                Debug.LogError($"blast.ssmd.handle_op_f2_f1: target not set");
#endif
                return;
            }

            vector_size = 2;
            switch (op)
            {
                default:
#if DEVELOPMENT_BUILD || TRACE
                    Debug.LogError($"blast.ssmd.handle_op_f2_f1: operation {op} not handled, vectorsize = {vector_size}, a[0] = {a[0]}, b[0] = {b[0]}");
#endif
                    return;

                case blast_operation.nop:
                    if (minus) simd.move_f4_array_to_indexed_data_as_f2_negated(target.data, target.row_size, target.is_aligned, target.index, b, ssmd_datacount);
                    else simd.move_f4_array_to_indexed_data_as_f2(target.data, target.row_size, target.is_aligned, target.index, b, ssmd_datacount);
                    break;

                case blast_operation.add:
                    // order does not matter but we handle that in simd.sub_array_2f1
                    if (minus) simd.sub_array_f2f1(a, b, target, ssmd_datacount);
                    else simd.add_array_f2f1(a, b, target, ssmd_datacount);
                    break;

                case blast_operation.substract:
                    if (minus) simd.add_array_f2f1(a, b, target, ssmd_datacount);
                    else simd.sub_array_f2f1(a, b, target, ssmd_datacount);
                    break;

                case blast_operation.multiply:
                    if (minus) simd.mul_array_f2f1_negated(a, b, target, ssmd_datacount);
                    else simd.mul_array_f2f1(a, b, target, ssmd_datacount);
                    break;

                case blast_operation.divide:
                    if (minus) simd.div_array_f2f1_negated(a, b, target, ssmd_datacount);
                    else simd.div_array_f2f1(a, b, target, ssmd_datacount);
                    break;

                case blast_operation.not:
                    if (not) for (int i = 0; i < ssmd_datacount; i++) ((float2*)(void*)&((float**)target.data)[i][target.index])[0] = math.select((float2)1, (float2)0, b[i].x == 0);
                    else for (int i = 0; i < ssmd_datacount; i++) ((float2*)(void*)&((float**)target.data)[i][target.index])[0] = math.select((float2)0, (float2)1, b[i].x == 0);
                    break;

                case blast_operation.and:
                    if (not) for (int i = 0; i < ssmd_datacount; i++) ((float2*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(1, 0, math.any(a[i].xy) && b[i].x != 0);
                    else for (int i = 0; i < ssmd_datacount; i++) ((float2*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0, 1, math.any(a[i].xy) && b[i].x != 0);
                    break;

                case blast_operation.or:
                    if (not) for (int i = 0; i < ssmd_datacount; i++) ((float2*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(1, 0, math.any(a[i].xy) || b[i].x != 0);
                    else for (int i = 0; i < ssmd_datacount; i++) ((float2*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0, 1, math.any(a[i].xy) || b[i].x != 0);
                    break;

                case blast_operation.xor:
                    if (not) for (int i = 0; i < ssmd_datacount; i++) ((float2*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(1, 0, math.any(a[i].xy) ^ b[i].x != 0);
                    else for (int i = 0; i < ssmd_datacount; i++) ((float2*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0, 1, math.any(a[i].xy) ^ b[i].x != 0);
                    break;


                case blast_operation.greater:
                    if (not) for (int i = 0; i < ssmd_datacount; i++) ((float2*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].xy <= b[i].x);
                    else for (int i = 0; i < ssmd_datacount; i++) ((float2*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].xy > b[i].x);
                    break;

                case blast_operation.smaller:
                    if (not) for (int i = 0; i < ssmd_datacount; i++) ((float2*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].xy >= b[i].x);
                    else for (int i = 0; i < ssmd_datacount; i++) ((float2*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].xy < b[i].x);
                    break;

                case blast_operation.smaller_equals:
                    if (not) for (int i = 0; i < ssmd_datacount; i++) ((float2*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].xy > b[i].x);
                    else for (int i = 0; i < ssmd_datacount; i++) ((float2*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].xy <= b[i].x);
                    break;

                case blast_operation.greater_equals:
                    if (not) for (int i = 0; i < ssmd_datacount; i++) ((float2*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].xy < b[i].x);
                    else for (int i = 0; i < ssmd_datacount; i++) ((float2*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].xy >= b[i].x);
                    break;

                case blast_operation.equals:
                    if (not) for (int i = 0; i < ssmd_datacount; i++) ((float2*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].xy != b[i].x);
                    else for (int i = 0; i < ssmd_datacount; i++) ((float2*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].xy == b[i].x);
                    break;

                case blast_operation.not_equals:
                    if (not) for (int i = 0; i < ssmd_datacount; i++) ((float2*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].xy == b[i].x);
                    else for (int i = 0; i < ssmd_datacount; i++) ((float2*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].xy != b[i].x);
                    break;
            }
        }


        static void handle_op_f2_f1(in blast_operation op, [NoAlias] float4* a, float constant, ref byte vector_size, int ssmd_datacount)
        {
            vector_size = 2;
            bool bconstant;
            float2 f2 = constant;
            switch (op)
            {
                default:
#if DEVELOPMENT_BUILD || TRACE
                    Debug.LogError($"blast.ssmd.handle_op_fx_fy: operation {op} not handled, vectorsize = {vector_size}, a[0] = {a[0]}, constant = {constant}");
#endif
                    return;
                case blast_operation.nop:
                    simd.move_f1_constant_into_f4_array_as_f2(a, constant, ssmd_datacount);
                    break;

                case blast_operation.not:
                    {
                        bconstant = constant != 0;
                        constant = math.select(1, 0, bconstant);
                        simd.move_f1_constant_into_f4_array_as_f2(a, constant, ssmd_datacount);
                    }
                    break;

                case blast_operation.add: simd.add_array_f2f1(a, constant, ssmd_datacount); return;
                case blast_operation.substract: simd.sub_array_f2f1(a, constant, ssmd_datacount); return;
                case blast_operation.multiply: simd.mul_array_f2f1(a, constant, ssmd_datacount); return;
                case blast_operation.divide: simd.div_array_f2f1(a, constant, ssmd_datacount); return;


                case blast_operation.and: bconstant = constant != 0; for (int i = 0; i < ssmd_datacount; i++) a[i].xy = math.select(0, 1, math.any(a[i].xy) && bconstant); return;
                case blast_operation.or: bconstant = constant != 0; for (int i = 0; i < ssmd_datacount; i++) a[i].xy = math.select(0, 1, math.any(a[i].xy) || bconstant); return;
                case blast_operation.xor: bconstant = constant != 0; for (int i = 0; i < ssmd_datacount; i++) a[i].xy = math.select(0, 1, math.any(a[i].xy) ^ bconstant); return;

                case blast_operation.greater: for (int i = 0; i < ssmd_datacount; i++) a[i].xy = math.select(0f, 1f, a[i].xy > constant); return;
                case blast_operation.smaller: for (int i = 0; i < ssmd_datacount; i++) a[i].xy = math.select(0f, 1f, a[i].xy < constant); return;
                case blast_operation.smaller_equals: for (int i = 0; i < ssmd_datacount; i++) a[i].xy = math.select(0f, 1f, a[i].xy <= constant); return;
                case blast_operation.greater_equals: for (int i = 0; i < ssmd_datacount; i++) a[i].xy = math.select(0f, 1f, a[i].xy >= constant); return;
                case blast_operation.equals: for (int i = 0; i < ssmd_datacount; i++) a[i].xy = math.select(0f, 1f, a[i].xy == constant); return;
                case blast_operation.not_equals: for (int i = 0; i < ssmd_datacount; i++) a[i].xy = math.select(0f, 1f, a[i].xy != constant); return;
            }
        }

        /// <summary>
        /// perform:  target.xy = a.xy [op] constant
        /// </summary>
        static void handle_op_f2_f1(in blast_operation op, [NoAlias] float4* a, float constant, ref byte vector_size, int ssmd_datacount, in DATAREC target)
        {
            if (!target.is_set)
            {              
#if DEVELOPMENT_BUILD || TRACE
                Debug.LogError($"blast.ssmd.handle_op_f2_f1 [constant b]: target not set");
#endif
                return;
            }

            vector_size = 2;
            switch (op)
            {
                default:
#if DEVELOPMENT_BUILD || TRACE
                    Debug.LogError($"blast.ssmd.handle_op_f2_f1: operation {op} not handled, vectorsize = {vector_size}, a[0] = {a[0]}, constant = {constant}");
#endif
                    break;

                case blast_operation.nop: simd.move_f1_constant_to_indexed_data_as_f2(target.data, target.row_size, target.is_aligned, target.index, constant, ssmd_datacount); break;
                case blast_operation.not: simd.move_f1_constant_to_indexed_data_as_f2(target.data, target.row_size, target.is_aligned, target.index, math.select(0f, 1f, constant == 0), ssmd_datacount); break;

                case blast_operation.add: simd.add_array_f2f1(a, constant, target, ssmd_datacount); break;
                case blast_operation.substract: simd.sub_array_f2f1(a, constant, target, ssmd_datacount); break;
                case blast_operation.multiply: simd.mul_array_f2f1(a, constant, target, ssmd_datacount); break;
                case blast_operation.divide: simd.div_array_f2f1(a, constant, target, ssmd_datacount); break;

                case blast_operation.and: { bool bconstant = constant != 0; for (int i = 0; i < ssmd_datacount; i++) ((float2*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0, 1, math.any(a[i].xy) && bconstant); } break;
                case blast_operation.or: { bool bconstant = constant != 0; for (int i = 0; i < ssmd_datacount; i++) ((float2*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0, 1, math.any(a[i].xy) || bconstant); } break;
                case blast_operation.xor: { bool bconstant = constant != 0; for (int i = 0; i < ssmd_datacount; i++) ((float2*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0, 1, math.any(a[i].xy) ^ bconstant); } break;

                case blast_operation.greater: for (int i = 0; i < ssmd_datacount; i++) ((float2*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].xy > constant); break;
                case blast_operation.smaller: for (int i = 0; i < ssmd_datacount; i++) ((float2*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].xy < constant); break;
                case blast_operation.smaller_equals: for (int i = 0; i < ssmd_datacount; i++) ((float2*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].xy <= constant); break;
                case blast_operation.greater_equals: for (int i = 0; i < ssmd_datacount; i++) ((float2*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].xy >= constant); break;
                case blast_operation.equals: for (int i = 0; i < ssmd_datacount; i++) ((float2*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].xy == constant); break;
                case blast_operation.not_equals: for (int i = 0; i < ssmd_datacount; i++) ((float2*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].xy != constant); break;
            }
        }
        /// <summary>
        /// perform:  a.xy = a.xy [op] b.xy
        /// </summary>
        static void handle_op_f2_f2(in blast_operation op, [NoAlias] float4* a, [NoAlias] float4* b, ref byte vector_size, int ssmd_datacount)
        {
            vector_size = 2;
            switch (op)
            {
                default:
#if DEVELOPMENT_BUILD || TRACE
                    Debug.LogError($"blast.ssmd.handle_op_fx_fy: operation {op} not handled, vectorsize = {vector_size}, a[0] = {a[0]}, b[0] = {b[0]}");
#endif
                    return;
                case blast_operation.nop:
                    {
                        if (a != b)
                        {
                            simd.move_f2_array_into_f4_array_as_f2(a, b, ssmd_datacount);
                        }
                    }
                    break;
                case blast_operation.not:
                    {
                        for (int i = 0; i < ssmd_datacount; i++) a[i].xy = math.select((float2)1, (float2)0, b[i].xy != 0); break;
                    }


                case blast_operation.add: simd.add_array_f2f2(a, b, ssmd_datacount); return;
                case blast_operation.substract: simd.sub_array_f2f2(a, b, ssmd_datacount); return;
                case blast_operation.multiply: simd.mul_array_f2f2(a, b, ssmd_datacount); return;
                case blast_operation.divide: simd.div_array_f2f2(a, b, ssmd_datacount); return;

                case blast_operation.and: for (int i = 0; i < ssmd_datacount; i++) a[i].xy = math.select(0, 1, math.any(a[i].xy) && math.any(b[i].xy)); return;
                case blast_operation.or: for (int i = 0; i < ssmd_datacount; i++) a[i].xy = math.select(0, 1, math.any(a[i].xy) || math.any(b[i].xy)); return;
                case blast_operation.xor: for (int i = 0; i < ssmd_datacount; i++) a[i].xy = math.select(0, 1, math.any(a[i].xy) ^ math.any(b[i].xy)); return;

                case blast_operation.greater: for (int i = 0; i < ssmd_datacount; i++) a[i].xy = math.select(0f, 1f, a[i].xy > b[i].xy); return;
                case blast_operation.smaller: for (int i = 0; i < ssmd_datacount; i++) a[i].xy = math.select(0f, 1f, a[i].xy < b[i].xy); return;
                case blast_operation.smaller_equals: for (int i = 0; i < ssmd_datacount; i++) a[i].xy = math.select(0f, 1f, a[i].xy <= b[i].xy); return;
                case blast_operation.greater_equals: for (int i = 0; i < ssmd_datacount; i++) a[i].xy = math.select(0f, 1f, a[i].xy >= b[i].xy); return;
                case blast_operation.equals: for (int i = 0; i < ssmd_datacount; i++) a[i].xy = math.select(0f, 1f, a[i].xy == b[i].xy); return;
                case blast_operation.not_equals: for (int i = 0; i < ssmd_datacount; i++) a[i].xy = math.select(0f, 1f, a[i].xy != b[i].xy); return;
            }
        }
        static void handle_op_f3_f1(in blast_operation op, [NoAlias] float4* a, [NoAlias] float4* b, ref byte vector_size, int ssmd_datacount)
        {
            vector_size = 3;
            switch (op)
            {
                default:
#if DEVELOPMENT_BUILD || TRACE
                    Debug.LogError($"blast.ssmd.handle_op_fx_fy: operation {op} not handled, vectorsize = {vector_size}, a[0] = {a[0]}, b[0] = {b[0]}");
#endif
                    return;
                case blast_operation.nop: simd.move_f1_array_into_f4_array_as_f3(a, b, ssmd_datacount); break;
                case blast_operation.not: for (int i = 0; i < ssmd_datacount; i++) a[i].xyz = math.select((float3)1, (float3)0, b[i].x != 0); break;

                case blast_operation.add: simd.add_array_f3f1(a, b, ssmd_datacount); return;
                case blast_operation.substract: simd.sub_array_f3f1(a, b, ssmd_datacount); return;
                case blast_operation.multiply: simd.mul_array_f3f1(a, b, ssmd_datacount); return;
                case blast_operation.divide: simd.div_array_f3f1(a, b, ssmd_datacount); return;

                case blast_operation.and: for (int i = 0; i < ssmd_datacount; i++) a[i].xyz = math.select(0, 1, math.any(a[i].xyz) && b[i].x != 0); return;
                case blast_operation.or: for (int i = 0; i < ssmd_datacount; i++) a[i].xyz = math.select(0, 1, math.any(a[i].xyz) || b[i].x != 0); return;
                case blast_operation.xor: for (int i = 0; i < ssmd_datacount; i++) a[i].xyz = math.select(0, 1, math.any(a[i].xyz) ^ b[i].x != 0); return;

                case blast_operation.greater: for (int i = 0; i < ssmd_datacount; i++) a[i].xyz = math.select(0f, 1f, a[i].xyz > b[i].x); return;
                case blast_operation.smaller: for (int i = 0; i < ssmd_datacount; i++) a[i].xyz = math.select(0f, 1f, a[i].xyz < b[i].x); return;
                case blast_operation.smaller_equals: for (int i = 0; i < ssmd_datacount; i++) a[i].xyz = math.select(0f, 1f, a[i].xyz <= b[i].x); return;
                case blast_operation.greater_equals: for (int i = 0; i < ssmd_datacount; i++) a[i].xyz = math.select(0f, 1f, a[i].xyz >= b[i].x); return;
                case blast_operation.equals: for (int i = 0; i < ssmd_datacount; i++) a[i].xyz = math.select(0f, 1f, a[i].xyz == b[i].x); return;
                case blast_operation.not_equals: for (int i = 0; i < ssmd_datacount; i++) a[i].xyz = math.select(0f, 1f, a[i].xyz != b[i].x); return;
            }
        }
        static void handle_op_f3_f1(in blast_operation op, [NoAlias] float4* a, float constant, ref byte vector_size, int ssmd_datacount)
        {
            vector_size = 3;
            bool bconstant;
            switch (op)
            {
                default:
#if DEVELOPMENT_BUILD || TRACE
                    Debug.LogError($"blast.ssmd.handle_op_fx_fy: operation {op} not handled, vectorsize = {vector_size}, a[0] = {a[0]}, constant = {constant}");
#endif
                    return;
                case blast_operation.nop: simd.move_f1_constant_into_f4_array_as_f3(a, constant, ssmd_datacount); break;
                case blast_operation.not: simd.move_f1_constant_into_f4_array_as_f3(a, math.select(1f, 0f, constant != 0), ssmd_datacount); break;

                //case blast_operation.nop: for (int i = 0; i < ssmd_datacount; i++) a[i].xyz = constant; break;
                //case blast_operation.not: constant = math.select(1, 0, constant != 0); for (int i = 0; i < ssmd_datacount; i++) a[i].xyz = constant; break;

                case blast_operation.add: simd.add_array_f3f1(a, constant, ssmd_datacount); break;
                case blast_operation.substract: simd.sub_array_f3f1(a, constant, ssmd_datacount); break;
                case blast_operation.multiply: simd.mul_array_f3f1(a, constant, ssmd_datacount); break;
                case blast_operation.divide: simd.div_array_f3f1(a, constant, ssmd_datacount); break;

                case blast_operation.and: bconstant = constant != 0; for (int i = 0; i < ssmd_datacount; i++) a[i].xyz = math.select(0, 1, math.any(a[i].xyz) && bconstant); return;
                case blast_operation.or: bconstant = constant != 0; for (int i = 0; i < ssmd_datacount; i++) a[i].xyz = math.select(0, 1, math.any(a[i].xyz) || bconstant); return;
                case blast_operation.xor: bconstant = constant != 0; for (int i = 0; i < ssmd_datacount; i++) a[i].xyz = math.select(0, 1, math.any(a[i].xyz) ^ bconstant); return;

                case blast_operation.greater: for (int i = 0; i < ssmd_datacount; i++) a[i].xyz = math.select(0f, 1f, a[i].xyz > constant); return;
                case blast_operation.smaller: for (int i = 0; i < ssmd_datacount; i++) a[i].xyz = math.select(0f, 1f, a[i].xyz < constant); return;
                case blast_operation.smaller_equals: for (int i = 0; i < ssmd_datacount; i++) a[i].xyz = math.select(0f, 1f, a[i].xyz <= constant); return;
                case blast_operation.greater_equals: for (int i = 0; i < ssmd_datacount; i++) a[i].xyz = math.select(0f, 1f, a[i].xyz >= constant); return;
                case blast_operation.equals: for (int i = 0; i < ssmd_datacount; i++) a[i].xyz = math.select(0f, 1f, a[i].xyz == constant); return;
                case blast_operation.not_equals: for (int i = 0; i < ssmd_datacount; i++) a[i].xyz = math.select(0f, 1f, a[i].xyz != constant); return;
            }
        }
        
        static void handle_op_f3_f1(in blast_operation op, [NoAlias] float4* a, float constant, ref byte vector_size, int ssmd_datacount, in DATAREC target)
        {
            if (!target.is_set)
            {
#if DEVELOPMENT_BUILD || TRACE
                Debug.LogError($"blast.ssmd.handle_op_f3_f1 [constant b]: target not set");
#endif
                return;
            }

            vector_size = 3;
            bool bconstant;
            switch (op)
            {
                default:
#if DEVELOPMENT_BUILD || TRACE
                    Debug.LogError($"blast.ssmd.handle_op_fx_fy: operation {op} not handled, vectorsize = {vector_size}, a[0] = {a[0]}, constant = {constant}");
#endif
                    return;
                case blast_operation.nop: simd.move_f1_constant_to_indexed_data_as_f3(target.data, target.row_size, target.is_aligned, target.index, constant, ssmd_datacount); break;
                case blast_operation.not: simd.move_f1_constant_to_indexed_data_as_f3(target.data, target.row_size, target.is_aligned, target.index, math.select(1f, 0f, constant != 0), ssmd_datacount); break;

                //case blast_operation.nop: for (int i = 0; i < ssmd_datacount; i++) a[i].xyz = constant; break;
                //case blast_operation.not: constant = math.select(1, 0, constant != 0); for (int i = 0; i < ssmd_datacount; i++) a[i].xyz = constant; break;

                case blast_operation.add: simd.add_array_f3f1(a, constant, target, ssmd_datacount); break;
                case blast_operation.substract: simd.sub_array_f3f1(a, constant, target, ssmd_datacount); break;
                case blast_operation.multiply: simd.mul_array_f3f1(a, constant, target, ssmd_datacount); break;
                case blast_operation.divide: simd.div_array_f3f1(a, constant, target, ssmd_datacount); break;

                case blast_operation.and: bconstant = constant != 0; for (int i = 0; i < ssmd_datacount; i++) ((float3*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0, 1, math.any(a[i].xyz) && bconstant); return;
                case blast_operation.or: bconstant = constant != 0; for (int i = 0; i < ssmd_datacount; i++) ((float3*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0, 1, math.any(a[i].xyz) || bconstant); return;
                case blast_operation.xor: bconstant = constant != 0; for (int i = 0; i < ssmd_datacount; i++) ((float3*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0, 1, math.any(a[i].xyz) ^ bconstant); return;

                case blast_operation.greater: for (int i = 0; i < ssmd_datacount; i++) ((float3*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].xyz > constant); return;
                case blast_operation.smaller: for (int i = 0; i < ssmd_datacount; i++) ((float3*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].xyz < constant); return;
                case blast_operation.smaller_equals: for (int i = 0; i < ssmd_datacount; i++) ((float3*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].xyz <= constant); return;
                case blast_operation.greater_equals: for (int i = 0; i < ssmd_datacount; i++) ((float3*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].xyz >= constant); return;
                case blast_operation.equals: for (int i = 0; i < ssmd_datacount; i++) ((float3*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].xyz == constant); return;
                case blast_operation.not_equals: for (int i = 0; i < ssmd_datacount; i++) ((float3*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].xyz != constant); return;
            }
        }

        /// <summary>
        /// perform: [][].xyz = a[].xyz * b[].x
        /// </summary>
        static void handle_op_f3_f1(in blast_operation op, [NoAlias] float4* a, [NoAlias] float4* b, ref byte vector_size, int ssmd_datacount, in bool minus, in bool not, in DATAREC target)
        {
            if (!target.is_set)
            {
#if DEVELOPMENT_BUILD || TRACE
                Debug.LogError($"blast.ssmd.handle_op_f3_f1: target not set");
#endif
                return;
            }

            vector_size = 2;
            switch (op)
            {
                default:
#if DEVELOPMENT_BUILD || TRACE
                    Debug.LogError($"blast.ssmd.handle_op_f3_f1: operation {op} not handled, vectorsize = {vector_size}, a[0] = {a[0]}, b[0] = {b[0]}");
#endif
                    return;

                case blast_operation.nop:
                    if (minus) simd.move_f4_array_to_indexed_data_as_f3_negated(target.data, target.row_size, target.is_aligned, target.index, b, ssmd_datacount);
                    else simd.move_f4_array_to_indexed_data_as_f3(target.data, target.row_size, target.is_aligned, target.index, b, ssmd_datacount);
                    break;

                case blast_operation.add:
                    // order does not matter but we handle that in simd.sub_array_3f1
                    if (minus) simd.sub_array_f3f1(a, b, target, ssmd_datacount);
                    else simd.add_array_f3f1(a, b, target, ssmd_datacount);
                    break;

                case blast_operation.substract:
                    if (minus) simd.add_array_f3f1(a, b, target, ssmd_datacount);
                    else simd.sub_array_f3f1(a, b, target, ssmd_datacount);
                    break;

                case blast_operation.multiply:
                    if (minus) simd.mul_array_f3f1_negated(a, b, target, ssmd_datacount);
                    else simd.mul_array_f3f1(a, b, target, ssmd_datacount);
                    break;

                case blast_operation.divide:
                    if (minus) simd.div_array_f3f1_negated(a, b, target, ssmd_datacount);
                    else simd.div_array_f3f1(a, b, target, ssmd_datacount);
                    break;

                case blast_operation.not:
                    if (not) for (int i = 0; i < ssmd_datacount; i++) ((float3*)(void*)&((float**)target.data)[i][target.index])[0] = math.select((float3)1, (float3)0, b[i].x == 0);
                    else for (int i = 0; i < ssmd_datacount; i++) ((float3*)(void*)&((float**)target.data)[i][target.index])[0] = math.select((float3)0, (float3)1, b[i].x == 0);
                    break;

                case blast_operation.and:
                    if (not) for (int i = 0; i < ssmd_datacount; i++) ((float3*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(1, 0, math.any(a[i].xyz) && b[i].x != 0);
                    else for (int i = 0; i < ssmd_datacount; i++) ((float3*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0, 1, math.any(a[i].xyz) && b[i].x != 0);
                    break;

                case blast_operation.or:
                    if (not) for (int i = 0; i < ssmd_datacount; i++) ((float3*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(1, 0, math.any(a[i].xyz) || b[i].x != 0);
                    else for (int i = 0; i < ssmd_datacount; i++) ((float3*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0, 1, math.any(a[i].xyz) || b[i].x != 0);
                    break;

                case blast_operation.xor:
                    if (not) for (int i = 0; i < ssmd_datacount; i++) ((float3*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(1, 0, math.any(a[i].xyz) ^ b[i].x != 0);
                    else for (int i = 0; i < ssmd_datacount; i++) ((float3*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0, 1, math.any(a[i].xyz) ^ b[i].x != 0);
                    break;

                case blast_operation.greater:
                    if (not) for (int i = 0; i < ssmd_datacount; i++) ((float3*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].xyz <= b[i].x);
                    else for (int i = 0; i < ssmd_datacount; i++) ((float3*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].xyz > b[i].x);
                    break;

                case blast_operation.smaller:
                    if (not) for (int i = 0; i < ssmd_datacount; i++) ((float3*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].xyz >= b[i].x);
                    else for (int i = 0; i < ssmd_datacount; i++) ((float3*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].xyz < b[i].x);
                    break;

                case blast_operation.smaller_equals:
                    if (not) for (int i = 0; i < ssmd_datacount; i++) ((float3*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].xyz > b[i].x);
                    else for (int i = 0; i < ssmd_datacount; i++) ((float3*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].xyz <= b[i].x);
                    break;

                case blast_operation.greater_equals:
                    if (not) for (int i = 0; i < ssmd_datacount; i++) ((float3*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].xyz < b[i].x);
                    else for (int i = 0; i < ssmd_datacount; i++) ((float3*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].xyz >= b[i].x);
                    break;

                case blast_operation.equals:
                    if (not) for (int i = 0; i < ssmd_datacount; i++) ((float3*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].xyz != b[i].x);
                    else for (int i = 0; i < ssmd_datacount; i++) ((float3*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].xyz == b[i].x);
                    break;

                case blast_operation.not_equals:
                    if (not) for (int i = 0; i < ssmd_datacount; i++) ((float3*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].xyz == b[i].x);
                    else for (int i = 0; i < ssmd_datacount; i++) ((float3*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].xyz != b[i].x);
                    break;
            }
        }

        static void handle_op_f3_f3(in blast_operation op, [NoAlias] float4* a, [NoAlias] float4* b, ref byte vector_size, int ssmd_datacount)
        {
            vector_size = 3;
            switch (op)
            {
                default:
#if DEVELOPMENT_BUILD || TRACE
                    Debug.LogError($"blast.ssmd.handle_op_fx_fy: operation {op} not handled, vectorsize = {vector_size}, a[0] = {a[0]}, b[0] = {b[0]}");
#endif
                    return;
                case blast_operation.nop: simd.move_f3_array_into_f4_array_as_f3(a, b, ssmd_datacount); break; // a[i].xyz = b[i].xyz; break;

                case blast_operation.not:
                    {
                        float* fa = (float*)(void*)a;
                        float* fb = (float*)(void*)b;
                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            fa[0] = math.select(1f, 0f, fb[0] != 0f); // x
                            fa[1] = math.select(1f, 0f, fb[1] != 0f); // y
                            fa[2] = math.select(1f, 0f, fb[2] != 0f); // z
                            fa += 4;
                        }
                    }
                    break;

                case blast_operation.add: simd.add_array_f3f3(a, b, ssmd_datacount); break;
                case blast_operation.substract: simd.sub_array_f3f3(a, b, ssmd_datacount); break;
                case blast_operation.multiply: simd.mul_array_f3f3(a, b, ssmd_datacount); break;
                case blast_operation.divide: simd.div_array_f3f3(a, b, ssmd_datacount); break;

                case blast_operation.and: for (int i = 0; i < ssmd_datacount; i++) a[i].xyz = math.select(0, 1, math.any(a[i].xyz) && math.any(b[i].xyz)); return;
                case blast_operation.or: for (int i = 0; i < ssmd_datacount; i++) a[i].xyz = math.select(0, 1, math.any(a[i].xyz) || math.any(b[i].xyz)); return;
                case blast_operation.xor: for (int i = 0; i < ssmd_datacount; i++) a[i].xyz = math.select(0, 1, math.any(a[i].xyz) ^ math.any(b[i].xyz)); return;

                case blast_operation.greater: for (int i = 0; i < ssmd_datacount; i++) a[i].xyz = math.select(0f, 1f, a[i].xyz > b[i].xyz); return;
                case blast_operation.smaller: for (int i = 0; i < ssmd_datacount; i++) a[i].xyz = math.select(0f, 1f, a[i].xyz < b[i].xyz); return;
                case blast_operation.smaller_equals: for (int i = 0; i < ssmd_datacount; i++) a[i].xyz = math.select(0f, 1f, a[i].xyz <= b[i].xyz); return;
                case blast_operation.greater_equals: for (int i = 0; i < ssmd_datacount; i++) a[i].xyz = math.select(0f, 1f, a[i].xyz >= b[i].xyz); return;
                case blast_operation.equals: for (int i = 0; i < ssmd_datacount; i++) a[i].xyz = math.select(0f, 1f, a[i].xyz == b[i].xyz); return;
                case blast_operation.not_equals: for (int i = 0; i < ssmd_datacount; i++) a[i].xyz = math.select(0f, 1f, a[i].xyz != b[i].xyz); return;
            }
        }

        /// <summary>
        /// [][].xy = [].xy [op] [minus|not] [].xy
        /// </summary>
        static void handle_op_f2_f2(in blast_operation op, [NoAlias] float4* a, [NoAlias] float4* b, ref byte vector_size, int ssmd_datacount, in bool minus, in bool not, in DATAREC target)
        {
            if (!target.is_set)
            {
#if DEVELOPMENT_BUILD || TRACE
                Debug.LogError($"blast.ssmd.handle_op_f2_f2: target not set");
#endif
                return;
            }
            vector_size = 2;
            switch (op)
            {
                default:
#if DEVELOPMENT_BUILD || TRACE
                    Debug.LogError($"blast.ssmd.handle_op_f2_f2: operation {op} not handled, vectorsize = {vector_size}, a[0] = {a[0]}, b[0] = {b[0]}");
#endif
                    return;
                case blast_operation.nop:
                    if (minus) simd.move_f4_array_to_indexed_data_as_f2(target.data, target.row_size, target.is_aligned, target.index, b, ssmd_datacount);
                    else simd.move_f4_array_to_indexed_data_as_f2_negated(target.data, target.row_size, target.is_aligned, target.index, b, ssmd_datacount);
                    break; 
                                    
                case blast_operation.not:
                    {
                        if (!not)
                        {
                            for (int i = 0; i < ssmd_datacount; i++)
                                ((float2*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, b[i].xy == 0f); 
                        }
                    }
                    break;

                case blast_operation.add:
                    if (minus) simd.sub_array_f2f2(a, b, target, ssmd_datacount);
                    else simd.add_array_f2f2(a, b, target, ssmd_datacount); 
                    break;

                case blast_operation.substract:
                    if (minus) simd.add_array_f2f2(a, b, target, ssmd_datacount);
                    else simd.sub_array_f2f2(a, b, target, ssmd_datacount);
                    break;

                case blast_operation.multiply:
                    if (minus) simd.mul_array_f2f2_negated(a, b, target, ssmd_datacount);
                    else simd.mul_array_f2f2(a, b, target, ssmd_datacount);
                    break;

                case blast_operation.divide:
                    if (minus) simd.div_array_f2f2_negated(a, b, target, ssmd_datacount);
                    else simd.div_array_f2f2(a, b, target, ssmd_datacount);
                    break;

                case blast_operation.and: 
                    if(not) for (int i = 0; i < ssmd_datacount; i++)    ((float2*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(1, 0, math.any(a[i].xy) && math.any(b[i].xy)); 
                    else for (int i = 0; i < ssmd_datacount; i++)       ((float2*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0, 1, math.any(a[i].xy) && math.any(b[i].xy));
                    break; 

                case blast_operation.or: 
                    if(not) for (int i = 0; i < ssmd_datacount; i++)    ((float2*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(1, 0, math.any(a[i].xy) || math.any(b[i].xy));
                    else for (int i = 0; i < ssmd_datacount; i++)       ((float2*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0, 1, math.any(a[i].xy) || math.any(b[i].xy));
                    break; 

                case blast_operation.xor:
                    if(not) for (int i = 0; i < ssmd_datacount; i++)    ((float2*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(1, 0, math.any(a[i].xy) ^ math.any(b[i].xy));
                    for (int i = 0; i < ssmd_datacount; i++)            ((float2*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0, 1, math.any(a[i].xy) ^ math.any(b[i].xy));
                    break; 

                case blast_operation.greater: 
                    if(not)     for (int i = 0; i < ssmd_datacount; i++) ((float2*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].xy <= b[i].xy); 
                    else 
                    if(minus)   for (int i = 0; i < ssmd_datacount; i++) ((float2*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].xy > -b[i].xy);
                    else
                                for (int i = 0; i < ssmd_datacount; i++) ((float2*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].xy > b[i].xy);
                    break; 

                case blast_operation.smaller:
                    if (not)    for (int i = 0; i < ssmd_datacount; i++) ((float2*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].xy > b[i].xy);
                    else
                    if (minus)  for (int i = 0; i < ssmd_datacount; i++) ((float2*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].xy < -b[i].xy);
                    else
                                for (int i = 0; i < ssmd_datacount; i++) ((float2*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].xy < b[i].xy);
                    break;

                case blast_operation.smaller_equals:
                    if (not)    for (int i = 0; i < ssmd_datacount; i++) ((float2*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].xy >= b[i].xy);
                    else
                    if (minus)  for (int i = 0; i < ssmd_datacount; i++) ((float2*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].xy <= -b[i].xy);
                    else
                                for (int i = 0; i < ssmd_datacount; i++) ((float2*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].xy <= b[i].xy);
                    break;

                case blast_operation.greater_equals: 
                    if (not)    for (int i = 0; i < ssmd_datacount; i++) ((float2*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].xy >= b[i].xy);
                    else
                    if (minus)  for (int i = 0; i < ssmd_datacount; i++) ((float2*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].xy <= -b[i].xy);
                    else
                                for (int i = 0; i < ssmd_datacount; i++) ((float2*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].xy <= b[i].xy);
                    break;

                case blast_operation.equals:
                    if (not)    for (int i = 0; i < ssmd_datacount; i++) ((float2*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].xy != b[i].xy);
                    else
                    if (minus)  for (int i = 0; i < ssmd_datacount; i++) ((float2*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].xy == -b[i].xy);
                    else
                                for (int i = 0; i < ssmd_datacount; i++) ((float2*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].xy == b[i].xy);
                    break;

                case blast_operation.not_equals:
                    if (not)    for (int i = 0; i < ssmd_datacount; i++) ((float2*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].xy == b[i].xy);
                    else
                    if (minus)  for (int i = 0; i < ssmd_datacount; i++) ((float2*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].xy != -b[i].xy);
                    else
                                for (int i = 0; i < ssmd_datacount; i++) ((float2*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].xy != b[i].xy);
                    break;
            }
        }


        /// <summary>
        /// [][].xyz = [].xyz [op] [minus|not] [].xyz
        /// </summary>
        static void handle_op_f3_f3(in blast_operation op, [NoAlias] float4* a, [NoAlias] float4* b, ref byte vector_size, int ssmd_datacount, in bool minus, in bool not, in DATAREC target)
        {
            if (!target.is_set)
            {
#if DEVELOPMENT_BUILD || TRACE
                Debug.LogError($"blast.ssmd.handle_op_f3_f3: target not set");
#endif
                return;
            }

            vector_size = 3;
            switch (op)
            {
                default:
#if DEVELOPMENT_BUILD || TRACE
                    Debug.LogError($"blast.ssmd.handle_op_f3_f3: operation {op} not handled, vectorsize = {vector_size}, a[0] = {a[0]}, b[0] = {b[0]}");
#endif
                    return;
                case blast_operation.nop:
                    if (minus) simd.move_f4_array_to_indexed_data_as_f3(target.data, target.row_size, target.is_aligned, target.index, b, ssmd_datacount);
                    else simd.move_f4_array_to_indexed_data_as_f3_negated(target.data, target.row_size, target.is_aligned, target.index, b, ssmd_datacount);
                    break; 
                                  

                case blast_operation.not:
                    {
                        if (!not)
                        {
                            for (int i = 0; i < ssmd_datacount; i++)
                                ((float3*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, b[i].xyz == 0f); 
                        }
                    }
                    break;

                case blast_operation.add:
                    if (minus) simd.sub_array_f3f3(a, b, target, ssmd_datacount);
                    else simd.add_array_f3f3(a, b, target, ssmd_datacount); 
                    break;

                case blast_operation.substract:
                    if (minus) simd.add_array_f3f3(a, b, target, ssmd_datacount);
                    else simd.sub_array_f3f3(a, b, target, ssmd_datacount);
                    break;

                case blast_operation.multiply:
                    if (minus) simd.mul_array_f3f3_negated(a, b, target, ssmd_datacount);
                    else simd.mul_array_f3f3(a, b, target, ssmd_datacount);
                    break;

                case blast_operation.divide:
                    if (minus) simd.div_array_f3f3_negated(a, b, target, ssmd_datacount);
                    else simd.div_array_f3f3(a, b, target, ssmd_datacount);
                    break;

                case blast_operation.and: 
                    if(not) for (int i = 0; i < ssmd_datacount; i++)    ((float3*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(1, 0, math.any(a[i].xyz) && math.any(b[i].xyz)); 
                    else for (int i = 0; i < ssmd_datacount; i++)       ((float3*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0, 1, math.any(a[i].xyz) && math.any(b[i].xyz));
                    break; 

                case blast_operation.or: 
                    if(not) for (int i = 0; i < ssmd_datacount; i++)    ((float3*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(1, 0, math.any(a[i].xyz) || math.any(b[i].xyz));
                    else for (int i = 0; i < ssmd_datacount; i++)       ((float3*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0, 1, math.any(a[i].xyz) || math.any(b[i].xyz));
                    break; 

                case blast_operation.xor:
                    if(not) for (int i = 0; i < ssmd_datacount; i++)    ((float3*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(1, 0, math.any(a[i].xyz) ^ math.any(b[i].xyz));
                    for (int i = 0; i < ssmd_datacount; i++)            ((float3*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0, 1, math.any(a[i].xyz) ^ math.any(b[i].xyz));
                    break; 

                case blast_operation.greater: 
                    if(not)     for (int i = 0; i < ssmd_datacount; i++) ((float3*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].xyz <= b[i].xyz); 
                    else 
                    if(minus)   for (int i = 0; i < ssmd_datacount; i++) ((float3*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].xyz > -b[i].xyz);
                    else
                                for (int i = 0; i < ssmd_datacount; i++) ((float3*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].xyz > b[i].xyz);
                    break; 

                case blast_operation.smaller:
                    if (not)    for (int i = 0; i < ssmd_datacount; i++) ((float3*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].xyz > b[i].xyz);
                    else
                    if (minus)  for (int i = 0; i < ssmd_datacount; i++) ((float3*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].xyz < -b[i].xyz);
                    else
                                for (int i = 0; i < ssmd_datacount; i++) ((float3*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].xyz < b[i].xyz);
                    break;

                case blast_operation.smaller_equals:
                    if (not)    for (int i = 0; i < ssmd_datacount; i++) ((float3*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].xyz >= b[i].xyz);
                    else
                    if (minus)  for (int i = 0; i < ssmd_datacount; i++) ((float3*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].xyz <= -b[i].xyz);
                    else
                                for (int i = 0; i < ssmd_datacount; i++) ((float3*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].xyz <= b[i].xyz);
                    break;

                case blast_operation.greater_equals: 
                    if (not)    for (int i = 0; i < ssmd_datacount; i++) ((float3*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].xyz >= b[i].xyz);
                    else
                    if (minus)  for (int i = 0; i < ssmd_datacount; i++) ((float3*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].xyz <= -b[i].xyz);
                    else
                                for (int i = 0; i < ssmd_datacount; i++) ((float3*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].xyz <= b[i].xyz);
                    break;

                case blast_operation.equals:
                    if (not)    for (int i = 0; i < ssmd_datacount; i++) ((float3*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].xyz != b[i].xyz);
                    else
                    if (minus)  for (int i = 0; i < ssmd_datacount; i++) ((float3*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].xyz == -b[i].xyz);
                    else
                                for (int i = 0; i < ssmd_datacount; i++) ((float3*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].xyz == b[i].xyz);
                    break;

                case blast_operation.not_equals:
                    if (not)    for (int i = 0; i < ssmd_datacount; i++) ((float3*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].xyz == b[i].xyz);
                    else
                    if (minus)  for (int i = 0; i < ssmd_datacount; i++) ((float3*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].xyz != -b[i].xyz);
                    else
                                for (int i = 0; i < ssmd_datacount; i++) ((float3*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].xyz != b[i].xyz);
                    break;
            }
        }

        /// <summary>
        /// [][].xyzw = [].xyzw [op] [minus|not] [].xyzw
        /// </summary>
        static void handle_op_f4_f4(in blast_operation op, [NoAlias] float4* a, [NoAlias] float4* b, ref byte vector_size, int ssmd_datacount, in bool minus, in bool not, in DATAREC target)
        {
            if (!target.is_set)
            {
#if DEVELOPMENT_BUILD || TRACE
                Debug.LogError($"blast.ssmd.handle_op_f4_f4: target not set");
#endif
                return;
            }
            vector_size = 4;
            switch (op)
            {
                default:
#if DEVELOPMENT_BUILD || TRACE
                    Debug.LogError($"blast.ssmd.handle_op_f4_f4: operation {op} not handled, vectorsize = {vector_size}, a[0] = {a[0]}, b[0] = {b[0]}");
#endif
                    return;
                case blast_operation.nop:
                    if (minus) simd.move_f4_array_to_indexed_data_as_f4(target.data, target.row_size, target.is_aligned, target.index, b, ssmd_datacount);
                    else simd.move_f4_array_to_indexed_data_as_f4_negated(target.data, target.row_size, target.is_aligned, target.index, b, ssmd_datacount);
                    break; 
                                    
                case blast_operation.not:
                    {
                        if (!not)
                        {
                            for (int i = 0; i < ssmd_datacount; i++)
                                ((float4*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, b[i] == 0f); 
                        }
                    }
                    break;

                case blast_operation.add:           
                    if (minus) simd.sub_array_f4f4(a, b, target, ssmd_datacount);
                    else simd.add_array_f4f4(a, b, target, ssmd_datacount); 
                    break;

                case blast_operation.substract:
                    if (minus) simd.add_array_f4f4(a, b, target, ssmd_datacount);
                    else simd.sub_array_f4f4(a, b, target, ssmd_datacount);
                    break;

                case blast_operation.multiply:
                    if (minus) simd.mul_array_f4f4_negated(a, b, target, ssmd_datacount);
                    else simd.mul_array_f4f4(a, b, target, ssmd_datacount);
                    break;

                case blast_operation.divide:
                    if (minus) simd.div_array_f4f4_negated(a, b, target, ssmd_datacount);
                    else simd.div_array_f4f4(a, b, target, ssmd_datacount);
                    break;

                case blast_operation.and: 
                    if(not) for (int i = 0; i < ssmd_datacount; i++)    ((float4*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(1, 0, math.any(a[i]) && math.any(b[i])); 
                    else for (int i = 0; i < ssmd_datacount; i++)       ((float4*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0, 1, math.any(a[i]) && math.any(b[i]));
                    break; 

                case blast_operation.or: 
                    if(not) for (int i = 0; i < ssmd_datacount; i++)    ((float4*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(1, 0, math.any(a[i]) || math.any(b[i]));
                    else for (int i = 0; i < ssmd_datacount; i++)       ((float4*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0, 1, math.any(a[i]) || math.any(b[i]));
                    break; 

                case blast_operation.xor:
                    if(not) for (int i = 0; i < ssmd_datacount; i++)    ((float4*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(1, 0, math.any(a[i]) ^ math.any(b[i]));
                    for (int i = 0; i < ssmd_datacount; i++)            ((float4*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0, 1, math.any(a[i]) ^ math.any(b[i]));
                    break; 

                case blast_operation.greater: 
                    if(not)     for (int i = 0; i < ssmd_datacount; i++) ((float4*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i] <= b[i]); 
                    else 
                    if(minus)   for (int i = 0; i < ssmd_datacount; i++) ((float4*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i] > -b[i]);
                    else
                                for (int i = 0; i < ssmd_datacount; i++) ((float4*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i] > b[i]);
                    break; 

                case blast_operation.smaller:
                    if (not)    for (int i = 0; i < ssmd_datacount; i++) ((float4*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i] > b[i]);
                    else
                    if (minus)  for (int i = 0; i < ssmd_datacount; i++) ((float4*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i] < -b[i]);
                    else
                                for (int i = 0; i < ssmd_datacount; i++) ((float4*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i] < b[i]);
                    break;

                case blast_operation.smaller_equals:
                    if (not)    for (int i = 0; i < ssmd_datacount; i++) ((float4*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i] >= b[i]);
                    else
                    if (minus)  for (int i = 0; i < ssmd_datacount; i++) ((float4*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i] <= -b[i]);
                    else
                                for (int i = 0; i < ssmd_datacount; i++) ((float4*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i] <= b[i]);
                    break;

                case blast_operation.greater_equals: 
                    if (not)    for (int i = 0; i < ssmd_datacount; i++) ((float4*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i] >= b[i]);
                    else
                    if (minus)  for (int i = 0; i < ssmd_datacount; i++) ((float4*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i] <= -b[i]);
                    else
                                for (int i = 0; i < ssmd_datacount; i++) ((float4*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i] <= b[i]);
                    break;

                case blast_operation.equals:
                    if (not)    for (int i = 0; i < ssmd_datacount; i++) ((float4*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i] != b[i]);
                    else
                    if (minus)  for (int i = 0; i < ssmd_datacount; i++) ((float4*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i] == -b[i]);
                    else
                                for (int i = 0; i < ssmd_datacount; i++) ((float4*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i] == b[i]);
                    break;

                case blast_operation.not_equals:
                    if (not)    for (int i = 0; i < ssmd_datacount; i++) ((float4*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i] == b[i]);
                    else
                    if (minus)  for (int i = 0; i < ssmd_datacount; i++) ((float4*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i] != -b[i]);
                    else
                                for (int i = 0; i < ssmd_datacount; i++) ((float4*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i] != b[i]);
                    break;
            }
        }


        static void handle_op_f4_f1(in blast_operation op, [NoAlias] float4* a, [NoAlias] float4* b, ref byte vector_size, int ssmd_datacount)
        {
            vector_size = 4;
            switch (op)
            {
                default:
#if DEVELOPMENT_BUILD || TRACE
                    Debug.LogError($"blast.ssmd.handle_op_fx_fy: operation {op} not handled, vectorsize = {vector_size}, a[0] = {a[0]}, b[0] = {b[0]}");
#endif
                    return;
                case blast_operation.nop: simd.move_f1_array_into_f4_array_as_f4(a, b, ssmd_datacount); break;
                case blast_operation.not: for (int i = 0; i < ssmd_datacount; i++) a[i] = math.select(1, 0, b[i].x != 0); break;

                case blast_operation.add: simd.add_array_f4f1(a, b, ssmd_datacount); return;
                case blast_operation.substract: simd.sub_array_f4f1(a, b, ssmd_datacount); return;
                case blast_operation.multiply: simd.mul_array_f4f1(a, b, ssmd_datacount); return;
                case blast_operation.divide: simd.div_array_f4f1(a, b, ssmd_datacount); return;

                case blast_operation.and: for (int i = 0; i < ssmd_datacount; i++) a[i] = math.select(0, 1, math.any(a[i]) && b[i].x != 0); return;
                case blast_operation.or: for (int i = 0; i < ssmd_datacount; i++) a[i] = math.select(0, 1, math.any(a[i]) || b[i].x != 0); return;
                case blast_operation.xor: for (int i = 0; i < ssmd_datacount; i++) a[i] = math.select(0, 1, math.any(a[i]) ^ b[i].x != 0); return;

                case blast_operation.greater: for (int i = 0; i < ssmd_datacount; i++) a[i] = math.select(0f, 1f, a[i] > b[i].x); return;
                case blast_operation.smaller: for (int i = 0; i < ssmd_datacount; i++) a[i] = math.select(0f, 1f, a[i] < b[i].x); return;
                case blast_operation.smaller_equals: for (int i = 0; i < ssmd_datacount; i++) a[i] = math.select(0f, 1f, a[i] <= b[i].x); return;
                case blast_operation.greater_equals: for (int i = 0; i < ssmd_datacount; i++) a[i] = math.select(0f, 1f, a[i] >= b[i].x); return;
                case blast_operation.equals: for (int i = 0; i < ssmd_datacount; i++) a[i] = math.select(0f, 1f, a[i] == b[i].x); return;
                case blast_operation.not_equals: for (int i = 0; i < ssmd_datacount; i++) a[i] = math.select(0f, 1f, a[i] != b[i].x); return;
            }
        }
        static void handle_op_f4_f1(in blast_operation op, [NoAlias] float4* a, float constant, ref byte vector_size, int ssmd_datacount)
        {
            vector_size = 4;
            bool bconstant;
            switch (op)
            {
                default:
#if DEVELOPMENT_BUILD || TRACE
                    Debug.LogError($"blast.ssmd.handle_op_fx_fy: operation {op} not handled, vectorsize = {vector_size}, a[0] = {a[0]}, constant = {constant}");
#endif
                    return;
                case blast_operation.nop: simd.move_f1_constant_into_f4_array_as_f4(a, constant, ssmd_datacount); break;
                // case blast_operation.nop: for (int i = 0; i < ssmd_datacount; i++) a[i] = constant; break;
                case blast_operation.not: simd.move_f1_constant_into_f4_array_as_f4(a, math.select(1f, 0f, constant != 0), ssmd_datacount); break;
                // case blast_operation.not: constant = math.select(1, 0, constant != 0); for (int i = 0; i < ssmd_datacount; i++) a[i] = constant; break;

                case blast_operation.add: simd.add_array_f4f1(a, constant, ssmd_datacount); return;
                case blast_operation.substract: simd.sub_array_f4f1(a, constant, ssmd_datacount); return;
                case blast_operation.multiply: simd.mul_array_f4f1(a, constant, ssmd_datacount); return;
                case blast_operation.divide: simd.div_array_f4f1(a, constant, ssmd_datacount); return;

                case blast_operation.and: bconstant = constant != 0; for (int i = 0; i < ssmd_datacount; i++) a[i] = math.select(0, 1, math.any(a[i]) && bconstant); return;
                case blast_operation.or: bconstant = constant != 0; for (int i = 0; i < ssmd_datacount; i++) a[i] = math.select(0, 1, math.any(a[i]) || bconstant); return;
                case blast_operation.xor: bconstant = constant != 0; for (int i = 0; i < ssmd_datacount; i++) a[i] = math.select(0, 1, math.any(a[i]) ^ bconstant); return;

                case blast_operation.greater: for (int i = 0; i < ssmd_datacount; i++) a[i] = math.select(0f, 1f, a[i] > constant); return;
                case blast_operation.smaller: for (int i = 0; i < ssmd_datacount; i++) a[i] = math.select(0f, 1f, a[i] < constant); return;
                case blast_operation.smaller_equals: for (int i = 0; i < ssmd_datacount; i++) a[i] = math.select(0f, 1f, a[i] <= constant); return;
                case blast_operation.greater_equals: for (int i = 0; i < ssmd_datacount; i++) a[i] = math.select(0f, 1f, a[i] >= constant); return;
                case blast_operation.equals: for (int i = 0; i < ssmd_datacount; i++) a[i] = math.select(0f, 1f, a[i] == constant); return;
                case blast_operation.not_equals: for (int i = 0; i < ssmd_datacount; i++) a[i] = math.select(0f, 1f, a[i] != constant); return;
            }
        }
        static void handle_op_f4_f1(in blast_operation op, [NoAlias] float4* a, float constant, ref byte vector_size, int ssmd_datacount, in DATAREC target)
        {
            if (!target.is_set)
            {
#if DEVELOPMENT_BUILD || TRACE
                Debug.LogError($"blast.ssmd.handle_op_f4_f1 [constant b]: target not set");
#endif
                return;
            }

            vector_size = 4;
            bool bconstant;
            switch (op)
            {
                default:
#if DEVELOPMENT_BUILD || TRACE
                    Debug.LogError($"blast.ssmd.handle_op_f4_f1: operation {op} not handled, vectorsize = {vector_size}, a[0] = {a[0]}, constant = {constant}");
#endif
                    return;
                case blast_operation.nop: simd.move_f1_constant_to_indexed_data_as_f4(target.data, target.row_size, target.is_aligned, target.index, constant, ssmd_datacount); break;
                case blast_operation.not: simd.move_f1_constant_to_indexed_data_as_f4(target.data, target.row_size, target.is_aligned, target.index, math.select(1f, 0f, constant != 0), ssmd_datacount); break;

                case blast_operation.add: simd.add_array_f4f1(a, constant, target, ssmd_datacount); return;
                case blast_operation.substract: simd.sub_array_f4f1(a, constant, target, ssmd_datacount); return;
                case blast_operation.multiply: simd.mul_array_f4f1(a, constant, target, ssmd_datacount); return;
                case blast_operation.divide: simd.div_array_f4f1(a, constant, target, ssmd_datacount); return;

                case blast_operation.and: bconstant = constant != 0; for (int i = 0; i < ssmd_datacount; i++) ((float4*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0, 1, math.any(a[i]) && bconstant); return;
                case blast_operation.or: bconstant = constant != 0; for (int i = 0; i < ssmd_datacount; i++) ((float4*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0, 1, math.any(a[i]) || bconstant); return;
                case blast_operation.xor: bconstant = constant != 0; for (int i = 0; i < ssmd_datacount; i++) ((float4*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0, 1, math.any(a[i]) ^ bconstant); return;

                case blast_operation.greater: for (int i = 0; i < ssmd_datacount; i++) ((float4*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i] > constant); return;
                case blast_operation.smaller: for (int i = 0; i < ssmd_datacount; i++) ((float4*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i] < constant); return;
                case blast_operation.smaller_equals: for (int i = 0; i < ssmd_datacount; i++) ((float4*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i] <= constant); return;
                case blast_operation.greater_equals: for (int i = 0; i < ssmd_datacount; i++) ((float4*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i] >= constant); return;
                case blast_operation.equals: for (int i = 0; i < ssmd_datacount; i++) ((float4*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i] == constant); return;
                case blast_operation.not_equals: for (int i = 0; i < ssmd_datacount; i++) ((float4*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i] != constant); return;
            }
        }




        /// <summary>
        /// perform: [][].xyzw = a[].xyzw * b[].x
        /// </summary>
        static void handle_op_f4_f1(in blast_operation op, [NoAlias] float4* a, [NoAlias] float4* b, ref byte vector_size, int ssmd_datacount, in bool minus, in bool not, in DATAREC target)
        {
            if (!target.is_set)
            {
#if DEVELOPMENT_BUILD || TRACE
                Debug.LogError($"blast.ssmd.handle_op_f4_f1: target not set");
#endif
                return;
            }

            vector_size = 2;
            switch (op)
            {
                default:
#if DEVELOPMENT_BUILD || TRACE
                    Debug.LogError($"blast.ssmd.handle_op_f4_f1: operation {op} not handled, vectorsize = {vector_size}, a[0] = {a[0]}, b[0] = {b[0]}");
#endif
                    return;

                case blast_operation.nop:
                    if (minus) simd.move_f4_array_to_indexed_data_as_f4_negated(target.data, target.row_size, target.is_aligned, target.index, b, ssmd_datacount);
                    else simd.move_f4_array_to_indexed_data_as_f4(target.data, target.row_size, target.is_aligned, target.index, b, ssmd_datacount);
                    break;

                case blast_operation.add:
                    // order does not matter but we handle that in simd.sub_array_3f1
                    if (minus) simd.sub_array_f4f1(a, b, target, ssmd_datacount);
                    else simd.add_array_f4f1(a, b, target, ssmd_datacount);
                    break;

                case blast_operation.substract:
                    if (minus) simd.add_array_f4f1(a, b, target, ssmd_datacount);
                    else simd.sub_array_f4f1(a, b, target, ssmd_datacount);
                    break;

                case blast_operation.multiply:
                    if (minus) simd.mul_array_f4f1_negated(a, b, target, ssmd_datacount);
                    else simd.mul_array_f4f1(a, b, target, ssmd_datacount);
                    break;

                case blast_operation.divide:
                    if (minus) simd.div_array_f4f1_negated(a, b, target, ssmd_datacount);
                    else simd.div_array_f4f1(a, b, target, ssmd_datacount);
                    break;

                case blast_operation.not:
                    if (not) for (int i = 0; i < ssmd_datacount; i++) ((float4*)(void*)&((float**)target.data)[i][target.index])[0] = math.select((float4)1, (float4)0, b[i].x == 0);
                    else for (int i = 0; i < ssmd_datacount; i++) ((float4*)(void*)&((float**)target.data)[i][target.index])[0] = math.select((float4)0, (float4)1, b[i].x == 0);
                    break;

                case blast_operation.and:
                    if (not) for (int i = 0; i < ssmd_datacount; i++) ((float4*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(1, 0, math.any(a[i]) && b[i].x != 0);
                    else for (int i = 0; i < ssmd_datacount; i++) ((float4*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0, 1, math.any(a[i]) && b[i].x != 0);
                    break;

                case blast_operation.or:
                    if (not) for (int i = 0; i < ssmd_datacount; i++) ((float4*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(1, 0, math.any(a[i]) || b[i].x != 0);
                    else for (int i = 0; i < ssmd_datacount; i++) ((float4*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0, 1, math.any(a[i]) || b[i].x != 0);
                    break;

                case blast_operation.xor:
                    if (not) for (int i = 0; i < ssmd_datacount; i++) ((float4*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(1, 0, math.any(a[i]) ^ b[i].x != 0);
                    else for (int i = 0; i < ssmd_datacount; i++) ((float4*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0, 1, math.any(a[i]) ^ b[i].x != 0);
                    break;

                case blast_operation.greater:
                    if (not) for (int i = 0; i < ssmd_datacount; i++) ((float4*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i] <= b[i].x);
                    else for (int i = 0; i < ssmd_datacount; i++) ((float4*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i] > b[i].x);
                    break;

                case blast_operation.smaller:
                    if (not) for (int i = 0; i < ssmd_datacount; i++) ((float4*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i] >= b[i].x);
                    else for (int i = 0; i < ssmd_datacount; i++) ((float4*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i] < b[i].x);
                    break;

                case blast_operation.smaller_equals:
                    if (not) for (int i = 0; i < ssmd_datacount; i++) ((float4*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i] > b[i].x);
                    else for (int i = 0; i < ssmd_datacount; i++) ((float4*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i] <= b[i].x);
                    break;

                case blast_operation.greater_equals:
                    if (not) for (int i = 0; i < ssmd_datacount; i++) ((float4*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i] < b[i].x);
                    else for (int i = 0; i < ssmd_datacount; i++) ((float4*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i] >= b[i].x);
                    break;

                case blast_operation.equals:
                    if (not) for (int i = 0; i < ssmd_datacount; i++) ((float4*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i] != b[i].x);
                    else for (int i = 0; i < ssmd_datacount; i++) ((float4*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i] == b[i].x);
                    break;

                case blast_operation.not_equals:
                    if (not) for (int i = 0; i < ssmd_datacount; i++) ((float4*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i] == b[i].x);
                    else for (int i = 0; i < ssmd_datacount; i++) ((float4*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i] != b[i].x);
                    break;
            }
        }

        static void handle_op_f4_f4(in blast_operation op, [NoAlias] float4* a, [NoAlias] float4* b, ref byte vector_size, int ssmd_datacount)
        {
            vector_size = 4;
            switch (op)
            {
                default:
#if DEVELOPMENT_BUILD || TRACE
                    Debug.LogError($"blast.ssmd.handle_op_fx_fy: operation {op} not handled, vectorsize = {vector_size}, a[0] = {a[0]}, b[0] = {b[0]}");
#endif
                    return;
                case blast_operation.nop: simd.move_f4_array_into_f4_array_as_f4(a, b, ssmd_datacount); break;
                case blast_operation.not: for (int i = 0; i < ssmd_datacount; i++) a[i] = math.select((float4)1f, (float4)0f, b[i] != 0); break;

                case blast_operation.add: simd.add_array_f4f1(a, b, ssmd_datacount); return;
                case blast_operation.substract: simd.sub_array_f4f1(a, b, ssmd_datacount); return;
                case blast_operation.multiply: simd.mul_array_f4f1(a, b, ssmd_datacount); return;
                case blast_operation.divide: simd.div_array_f4f1(a, b, ssmd_datacount); return;

                case blast_operation.and: for (int i = 0; i < ssmd_datacount; i++) a[i] = math.select(0, 1, math.any(a[i]) && math.any(b[i])); return;
                case blast_operation.or: for (int i = 0; i < ssmd_datacount; i++) a[i] = math.select(0, 1, math.any(a[i]) || math.any(b[i])); return;
                case blast_operation.xor: for (int i = 0; i < ssmd_datacount; i++) a[i] = math.select(0, 1, math.any(a[i]) ^ math.any(b[i])); return;

                case blast_operation.greater: for (int i = 0; i < ssmd_datacount; i++) a[i] = math.select(0f, 1f, a[i] > b[i]); return;
                case blast_operation.smaller: for (int i = 0; i < ssmd_datacount; i++) a[i] = math.select(0f, 1f, a[i] < b[i]); return;
                case blast_operation.smaller_equals: for (int i = 0; i < ssmd_datacount; i++) a[i] = math.select(0f, 1f, a[i] <= b[i]); return;
                case blast_operation.greater_equals: for (int i = 0; i < ssmd_datacount; i++) a[i] = math.select(0f, 1f, a[i] >= b[i]); return;
                case blast_operation.equals: for (int i = 0; i < ssmd_datacount; i++) a[i] = math.select(0f, 1f, a[i] == b[i]); return;
                case blast_operation.not_equals: for (int i = 0; i < ssmd_datacount; i++) a[i] = math.select(0f, 1f, a[i] != b[i]); return;
            }
        }

        #endregion

    }
}
