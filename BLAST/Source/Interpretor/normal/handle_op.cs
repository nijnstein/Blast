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


#if STANDALONE_VSBUILD
    using NSS.Blast.Standalone;
    using System.Reflection;
#else
    using UnityEngine;
    using Unity.Burst.CompilerServices;
#endif


using Unity.Burst;
using Unity.Mathematics;

using V1 = System.Single;
using V2 = Unity.Mathematics.float2;
using V3 = Unity.Mathematics.float3;
using V4 = Unity.Mathematics.float4;

namespace NSS.Blast.Interpretor
{
    unsafe public partial struct BlastInterpretor
    {

        /// <summary>
        /// handle operation between 2 singles of vectorsize 1, handles in the form: <c>result = a + b;</c>
        /// </summary>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        static void handle_op_f1f1(in blast_operation op, ref V1 a, in V1 b)
        {
            switch (op)
            {
                default:
                case blast_operation.nop:               a = b;                                      break;
                case blast_operation.add:               a = a + b;                                  break; 
                case blast_operation.substract:         a = a - b;                                  break;
                case blast_operation.multiply:          a = a * b;                                  break; 
                case blast_operation.divide:            a = a / b;                                  break; 
                case blast_operation.and:               a = math.select(0f, 1f, a != 0 && b != 0);  break;
                case blast_operation.or:                a = math.select(0f, 1f, a != 0 || b != 0);  break;
                case blast_operation.xor:               a = math.select(0f, 1f, a != 0 ^ b != 0);   break;
                case blast_operation.greater:           a = math.select(0f, 1f, a > b);             break;
                case blast_operation.smaller:           a = math.select(0f, 1f, a < b);             break;
                case blast_operation.smaller_equals:    a = math.select(0f, 1f, a <= b);            break;
                case blast_operation.greater_equals:    a = math.select(0f, 1f, a >= b);            break;
                case blast_operation.equals:            a = math.select(0f, 1f, a == b);            break;
                case blast_operation.not_equals:        a = math.select(0f, 1f, a != b);            break;
                case blast_operation.not:               a = math.select(0f, 1f, b == 0);            break;
            }
        }

        /// <summary>
        /// handle operation between 2 integers of vectorsize 1, handles in the form: <c>result = a + b;</c>
        /// </summary>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        static void handle_op_i1i1(in blast_operation op, ref int a, in int b)
        {
            switch (op)
            {
                default:
                case blast_operation.nop:               a = b;                                    break;
                case blast_operation.add:               a = a + b;                                break; 
                case blast_operation.substract:         a = a - b;                                break;
                case blast_operation.multiply:          a = a * b;                                break; 
                case blast_operation.divide:            a = a / b;                                break; 
                case blast_operation.and:               a = math.select(0, 1, a != 0 && b != 0);  break;
                case blast_operation.or:                a = math.select(0, 1, a != 0 || b != 0);  break;
                case blast_operation.xor:               a = math.select(0, 1, a != 0 ^ b != 0);   break;
                case blast_operation.greater:           a = math.select(0, 1, a > b);             break;
                case blast_operation.smaller:           a = math.select(0, 1, a < b);             break;
                case blast_operation.smaller_equals:    a = math.select(0, 1, a <= b);            break;
                case blast_operation.greater_equals:    a = math.select(0, 1, a >= b);            break;
                case blast_operation.equals:            a = math.select(0, 1, a == b);            break;
                case blast_operation.not_equals:        a = math.select(0, 1, a != b);            break;
                case blast_operation.not:               a = math.select(0, 1, b == 0);            break;
            }
        }

        /// <summary>
        /// i1 = i1 * f1 
        /// </summary>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        static void handle_op_i1f1(in blast_operation op, ref V1 a, in float b)
        {
            int ia = CodeUtils.ReInterpret<float, int>(a);
            handle_op_i1i1(in op, ref ia, (int)b);
            a = CodeUtils.ReInterpret<int, float>(ia);
        }

        /// <summary>
        /// handle:  f1 = f1 * i1
        /// this reinterprets the integer backed by float memory in v1 
        /// </summary>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        static void handle_op_f1i1(in blast_operation op, ref V1 a, V1 b)
        {
            b = (float)CodeUtils.ReInterpret<float, int>(b);
            handle_op_f1f1(in op, ref a, b); 
        }

        /// <summary>
        /// handle:  f1 = f1 * -i1
        /// this reinterprets the integer backed by float memory in v1 
        /// </summary>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        static void handle_op_f1i1_n(in blast_operation op, ref V1 a, V1 b)
        {
            b = (float)CodeUtils.ReInterpret<float, int>(b);
            handle_op_f1f1(in op, ref a, -b);
        }

        /// <summary>
        /// handle:  f1 = f1 * ~b1
        /// this reinterprets the integer backed by float memory in v1 
        /// </summary>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        static void handle_op_f1b1_n(in blast_operation op, ref V1 a, V1 b)
        {
            b = (float)~CodeUtils.ReInterpret<float, int>(b);
            handle_op_f1f1(in op, ref a, b);
        }


        /// <summary>
        /// handle:  i1 = i1 * i1
        /// this reinterprets the integer backed by float memory in v1 
        /// </summary>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        static void handle_op_i1i1(in blast_operation op, ref V1 a, V1 b)
        {
            int ia = CodeUtils.ReInterpret<float, int>(a);
            int ib = CodeUtils.ReInterpret<float, int>(b);
            handle_op_i1i1(in op, ref ia, ib);
            a = CodeUtils.ReInterpret<int, float>(ia);
        }
        /// <summary>
        /// handle:  i1 = i1 * -i1
        /// this reinterprets the integer backed by float memory in v1 
        /// </summary>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        static void handle_op_i1i1_n(in blast_operation op, ref V1 a, V1 b)
        {
            int ia = CodeUtils.ReInterpret<float, int>(a);
            int ib = CodeUtils.ReInterpret<float, int>(b);
            handle_op_i1i1(in op, ref ia, -ib);
            a = CodeUtils.ReInterpret<int, float>(ia);
        }
        /// <summary>
        /// handle:  i1 = i1 * ~b1
        /// this reinterprets the integer backed by float memory in v1 
        /// </summary>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        static void handle_op_i1b1_n(in blast_operation op, ref V1 a, V1 b)
        {
            int ia = CodeUtils.ReInterpret<float, int>(a);
            int ib = CodeUtils.ReInterpret<float, int>(b);
            handle_op_i1i1(in op, ref ia, ~ib);
            a = CodeUtils.ReInterpret<int, float>(ia);
        }

        /// <summary>
        /// handle a.xy = a.x op b.xy
        /// </summary>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        static void handle_op_f1f2(in blast_operation op, ref V4 a, in V2 b)
        {
            switch (op)
            {
                default:
                case blast_operation.nop:               a.xy = b;                                        break;
                case blast_operation.add:               a.xy = a.x + b;                                  break; 
                case blast_operation.substract:         a.xy = a.x - b;                                  break;
                case blast_operation.multiply:          a.xy = a.x * b;                                  break; 
                case blast_operation.divide:            a.xy = a.x / b;                                  break; 

                case blast_operation.or:                a.xy = math.select(0f, 1f, a.x != 0 | b != 0);   break; // not the avoidance of short-circuiting, this forces the compiler to consider a fixed b and results in a vector2 select 
                case blast_operation.and:               a.xy = math.select(0f, 1f, a.x != 0 & b != 0);   break;

                case blast_operation.xor:               a.xy = math.select(0f, 1f, a.x != 0 ^ b != 0);   break;
                case blast_operation.greater:           a.xy = math.select(0f, 1f, a.x > b);             break; 
                case blast_operation.smaller:           a.xy = math.select(0f, 1f, a.x < b);             break;
                case blast_operation.greater_equals:    a.xy = math.select(0f, 1f, a.x >= b);            break;
                case blast_operation.smaller_equals:    a.xy = math.select(0f, 1f, a.x <= b);            break; 

                case blast_operation.equals:            a.xy = math.select(0f, 1f, a.x == b);            break;
                case blast_operation.not_equals:        a.xy = math.select(0f, 1f, a.x != b);            break; 

                case blast_operation.not:               a.xy = math.select(0f, 1f, b == 0);              break;
            }
        }


        /// <summary>
        /// handle operation between 2 integers of vectorsize 1 and 2
        /// </summary>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        static void handle_op_i1i2(in blast_operation op, ref int4 a, in int2 b)
        {
            switch (op)
            {
                default:
                case blast_operation.nop:               a.xy = b;                                            break;
                case blast_operation.add:               a.xy = a.x + b;                                      break; 
                case blast_operation.substract:         a.xy = a.x - b;                                      break;
                case blast_operation.multiply:          a.xy = a.x * b;                                      break; 
                case blast_operation.divide:            a.xy = a.x / b;                                      break; 

                case blast_operation.or:                a.xy = math.select((int2)0, 1, a.x != 0 | b != 0);   break; // not the avoidance of short-circuiting, this forces the compiler to consider a fixed b and results in a vector2 select 
                case blast_operation.and:               a.xy = math.select((int2)0, 1, a.x != 0 & b != 0);   break;

                case blast_operation.xor:               a.xy = math.select((int2)0, 1, a.x != 0 ^ b != 0);   break;
                case blast_operation.greater:           a.xy = math.select((int2)0, 1, a.x > b);             break; 
                case blast_operation.smaller:           a.xy = math.select((int2)0, 1, a.x < b);             break;
                case blast_operation.greater_equals:    a.xy = math.select((int2)0, 1, a.x >= b);            break;
                case blast_operation.smaller_equals:    a.xy = math.select((int2)0, 1, a.x <= b);            break; 

                case blast_operation.equals:            a.xy = math.select((int2)0, 1, a.x == b);            break;
                case blast_operation.not_equals:        a.xy = math.select((int2)0, 1, a.x != b);            break; 

                case blast_operation.not:               a.xy = math.select((int2)0, 1, b == 0);              break;
            }
        }


        /// <summary>
        /// i2 = i1 * f2 
        /// </summary>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        static void handle_op_i1f2(in blast_operation op, ref V4 a, in float2 b)
        {
            int4 ia = CodeUtils.ReInterpret<float4, int4>(a);
            handle_op_i1i2(in op, ref ia, (int2)b);
            a = CodeUtils.ReInterpret<int4, float4>(ia);
        }

        /// <summary>
        /// handle a.xy = a.x op int.xy
        /// </summary>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        static void handle_op_f1i2(in blast_operation op, ref V4 a, V2 b)
        {
            b = math.float2(CodeUtils.ReInterpret<float2, int2>(b));
            handle_op_f2f2(op, ref a, in b);
        }

        /// <summary>
        /// handle a.xy = a.x op -int.xy
        /// </summary>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        static void handle_op_f1i2_n(in blast_operation op, ref V4 a, V2 b)
        {
            b = math.float2(CodeUtils.ReInterpret<float2, int2>(b));
            handle_op_f2f2(op, ref a, -b);
        }

        /// <summary>
        /// handle:  i2 = i1 * i2
        /// this reinterprets the integer backed by float memory in v1 
        /// </summary>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        static void handle_op_i1i2(in blast_operation op, ref V4 a, V2 b)
        {
            int4 ia = CodeUtils.ReInterpret<float4, int4>(a);
            int2 ib = CodeUtils.ReInterpret<float2, int2>(b);
            handle_op_i1i2(in op, ref ia, ib);
            a = CodeUtils.ReInterpret<int4, float4>(ia);
        }

        /// <summary>          
        /// handle:  i2 = i1 * -i2
        /// this reinterprets the integer backed by float memory in v1 
        /// </summary>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        static void handle_op_i1i2_n(in blast_operation op, ref V4 a, V2 b)
        {
            int4 ia = CodeUtils.ReInterpret<float4, int4>(a);
            int2 ib = CodeUtils.ReInterpret<float2, int2>(b);
            handle_op_i1i2(in op, ref ia, -ib);
            a = CodeUtils.ReInterpret<int4, float4>(ia);
        }

        /// <summary>
        /// handle a.xyz = a.x op b.xyz
        /// </summary>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        static void handle_op_f1f3(in blast_operation op, ref V4 a, in V3 b)
        {
            switch (op)
            {
                default:
                case blast_operation.nop:               a.xyz = b;                                        break;
                case blast_operation.add:               a.xyz = a.x + b;                                  break; 
                case blast_operation.substract:         a.xyz = a.x - b;                                  break;
                case blast_operation.multiply:          a.xyz = a.x * b;                                  break; 
                case blast_operation.divide:            a.xyz = a.x / b;                                  break; 

                case blast_operation.or:                a.xyz = math.select(0f, 1f, a.x != 0 | b != 0);   break; 
                case blast_operation.and:               a.xyz = math.select(0f, 1f, a.x != 0 & b != 0);   break;

                case blast_operation.xor:               a.xyz = math.select(0f, 1f, a.x != 0 ^ b != 0);   break;
                case blast_operation.greater:           a.xyz = math.select(0f, 1f, a.x > b);             break; 
                case blast_operation.smaller:           a.xyz = math.select(0f, 1f, a.x < b);             break;
                case blast_operation.greater_equals:    a.xyz = math.select(0f, 1f, a.x >= b);            break;
                case blast_operation.smaller_equals:    a.xyz = math.select(0f, 1f, a.x <= b);            break; 

                case blast_operation.equals:            a.xyz = math.select(0f, 1f, a.x == b);            break;
                case blast_operation.not_equals:        a.xyz = math.select(0f, 1f, a.x != b);            break; 

                case blast_operation.not:               a.xyz = math.select(0f, 1f, b == 0);              break;
            }
        }

        /// <summary>
        /// handle i.xyz = i.x op i.xyz
        /// </summary>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        static void handle_op_i1i3(in blast_operation op, ref int4 a, in int3 b)
        {
            switch (op)
            {
                default:
                case blast_operation.nop:               a.xyz = b;                                            break;
                case blast_operation.add:               a.xyz = a.x + b;                                      break; 
                case blast_operation.substract:         a.xyz = a.x - b;                                      break;
                case blast_operation.multiply:          a.xyz = a.x * b;                                      break; 
                case blast_operation.divide:            a.xyz = a.x / b;                                      break; 

                case blast_operation.or:                a.xyz = math.select((int3)0, 1, a.x != 0 | b != 0);   break; 
                case blast_operation.and:               a.xyz = math.select((int3)0, 1, a.x != 0 & b != 0);   break;

                case blast_operation.xor:               a.xyz = math.select((int3)0, 1, a.x != 0 ^ b != 0);   break;
                case blast_operation.greater:           a.xyz = math.select((int3)0, 1, a.x > b);             break; 
                case blast_operation.smaller:           a.xyz = math.select((int3)0, 1, a.x < b);             break;
                case blast_operation.greater_equals:    a.xyz = math.select((int3)0, 1, a.x >= b);            break;
                case blast_operation.smaller_equals:    a.xyz = math.select((int3)0, 1, a.x <= b);            break; 

                case blast_operation.equals:            a.xyz = math.select((int3)0, 1, a.x == b);            break;
                case blast_operation.not_equals:        a.xyz = math.select((int3)0, 1, a.x != b);            break; 

                case blast_operation.not:               a.xyz = math.select((int3)0, 1, b == 0);              break;
            }
        }

        /// <summary>
        /// i2 = i1 * f2 
        /// </summary>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        static void handle_op_i1f3(in blast_operation op, ref V4 a, in float3 b)
        {
            int4 ia = CodeUtils.ReInterpret<float4, int4>(a);
            handle_op_i1i3(in op, ref ia, (int3)b);
            a = CodeUtils.ReInterpret<int4, float4>(ia);
        }


        /// <summary>
        /// handle a.xyz = a.x op int.xyz
        /// </summary>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        static void handle_op_f1i3(in blast_operation op, ref V4 a, ref V3 b)
        {
            b = math.float3(CodeUtils.ReInterpret<float3, int3>(b));
            handle_op_f1f3(in op, ref a, in b);
        }

        /// <summary>
        /// handle a.xyz = a.x op -int.xyz
        /// </summary>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        static void handle_op_f1i3_n(in blast_operation op, ref V4 a, ref V3 b)
        {
            b = math.float3(CodeUtils.ReInterpret<float3, int3>(b));
            handle_op_f1f3(in op, ref a, -b);
        }

        /// <summary>
        /// handle:  i3 = i1 * i3
        /// this reinterprets the integer backed by float memory in v1 
        /// </summary>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        static void handle_op_i1i3(in blast_operation op, ref V4 a, V3 b)
        {
            int4 ia = CodeUtils.ReInterpret<float4, int4>(a);
            int3 ib = CodeUtils.ReInterpret<float3, int3>(b);
            handle_op_i1i3(in op, ref ia, ib);
            a = CodeUtils.ReInterpret<int4, float4>(ia);
        }

        /// <summary>          
        /// handle:  i3 = i1 * -i3
        /// this reinterprets the integer backed by float memory in v1 
        /// </summary>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        static void handle_op_i1i3_n(in blast_operation op, ref V4 a, V3 b)
        {
            int4 ia = CodeUtils.ReInterpret<float4, int4>(a);
            int3 ib = CodeUtils.ReInterpret<float3, int3>(b);
            handle_op_i1i3(in op, ref ia, -ib);
            a = CodeUtils.ReInterpret<int4, float4>(ia);
        }
        /// <summary>
        /// handle a.xyzw = a.x op b.xyzw
        /// </summary>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        static void handle_op_f1f4(in blast_operation op, ref V4 a, in V4 b)
        {
            switch (op)
            {
                default:
                case blast_operation.nop:               a = b;                                        break;
                case blast_operation.add:               a = a.x + b;                                  break; 
                case blast_operation.substract:         a = a.x - b;                                  break;
                case blast_operation.multiply:          a = a.x * b;                                  break; 
                case blast_operation.divide:            a = a.x / b;                                  break; 

                case blast_operation.or:                a = math.select(0f, 1f, a.x != 0 | b != 0);   break; 
                case blast_operation.and:               a = math.select(0f, 1f, a.x != 0 & b != 0);   break;

                case blast_operation.xor:               a = math.select(0f, 1f, a.x != 0 ^ b != 0);   break;
                case blast_operation.greater:           a = math.select(0f, 1f, a.x > b);             break; 
                case blast_operation.smaller:           a = math.select(0f, 1f, a.x < b);             break;
                case blast_operation.greater_equals:    a = math.select(0f, 1f, a.x >= b);            break;
                case blast_operation.smaller_equals:    a = math.select(0f, 1f, a.x <= b);            break; 

                case blast_operation.equals:            a = math.select(0f, 1f, a.x == b);            break;
                case blast_operation.not_equals:        a = math.select(0f, 1f, a.x != b);            break; 

                case blast_operation.not:               a = math.select(0f, 1f, b == 0);              break;
            }
        }
        
        /// <summary>
        /// handle i.xyzw = i.x op i.xyzw
        /// </summary>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        static void handle_op_i1i4(in blast_operation op, ref int4 a, in int4 b)
        {
            switch (op)
            {
                default:
                case blast_operation.nop:               a = b;                                            break;
                case blast_operation.add:               a = a.x + b;                                      break; 
                case blast_operation.substract:         a = a.x - b;                                      break;
                case blast_operation.multiply:          a = a.x * b;                                      break; 
                case blast_operation.divide:            a = a.x / b;                                      break; 

                case blast_operation.or:                a = math.select((int4)0, 1, a.x != 0 | b != 0);   break; 
                case blast_operation.and:               a = math.select((int4)0, 1, a.x != 0 & b != 0);   break;

                case blast_operation.xor:               a = math.select((int4)0, 1, a.x != 0 ^ b != 0);   break;
                case blast_operation.greater:           a = math.select((int4)0, 1, a.x > b);             break; 
                case blast_operation.smaller:           a = math.select((int4)0, 1, a.x < b);             break;
                case blast_operation.greater_equals:    a = math.select((int4)0, 1, a.x >= b);            break;
                case blast_operation.smaller_equals:    a = math.select((int4)0, 1, a.x <= b);            break; 

                case blast_operation.equals:            a = math.select((int4)0, 1, a.x == b);            break;
                case blast_operation.not_equals:        a = math.select((int4)0, 1, a.x != b);            break; 

                case blast_operation.not:               a = math.select((int4)0, 1, b == 0);              break;
            }
        }


        /// <summary>
        /// i4 = i1 * f4 
        /// </summary>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        static void handle_op_i1f4(in blast_operation op, ref V4 a, in float4 b)
        {
            int4 ia = CodeUtils.ReInterpret<float4, int4>(a);
            handle_op_i1i4(in op, ref ia, (int4)b);
            a = CodeUtils.ReInterpret<int4, float4>(ia);
        }

        /// <summary>
        /// handle a.xyzw = a.x op int.xyzw
        /// </summary>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        static void handle_op_f1i4(in blast_operation op, ref V4 a, V4 b)
        {
            b = math.float4(CodeUtils.ReInterpret<float4, int4>(b));
            handle_op_f1f4(in op, ref a, in b);
        }

        /// <summary>
        /// handle a.xyzw = a.x op -int.xyzw
        /// </summary>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        static void handle_op_f1i4_n(in blast_operation op, ref V4 a, V4 b)
        {
            b = math.float4(CodeUtils.ReInterpret<float4, int4>(b));
            handle_op_f1f4(in op, ref a, -b);
        }

        /// <summary>
        /// handle:  i4 = i1 * i4
        /// this reinterprets the integer backed by float memory in v1 
        /// </summary>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        static void handle_op_i1i4(in blast_operation op, ref V4 a, V4 b)
        {
            int4 ia = CodeUtils.ReInterpret<float4, int4>(a);
            int4 ib = CodeUtils.ReInterpret<float4, int4>(b);
            handle_op_i1i4(in op, ref ia, ib);
            a = CodeUtils.ReInterpret<int4, float4>(ia);
        }

        /// <summary>          
        /// handle:  i4 = i1 * -i4
        /// this reinterprets the integer backed by float memory in v1 
        /// </summary>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        static void handle_op_i1i4_n(in blast_operation op, ref V4 a, V4 b)
        {
            int4 ia = CodeUtils.ReInterpret<float4, int4>(a);
            int4 ib = CodeUtils.ReInterpret<float4, int4>(b);
            handle_op_i1i4(in op, ref ia, -ib);
            a = CodeUtils.ReInterpret<int4, float4>(ia);
        }

        /// <summary>
        /// f4.xy = f4.xy op f1 
        /// </summary>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        static void handle_op_f2f1(in blast_operation op, ref V4 a, in V1 b)
        {
            switch (op)
            {
                default:
                case blast_operation.nop:               a.xy = b;                                           break;
                case blast_operation.add:               a.xy = a.xy + b;                                    break;
                case blast_operation.substract:         a.xy = a.xy - b;                                    break;
                case blast_operation.multiply:          a.xy = a.xy * b;                                    break;
                case blast_operation.divide:            a.xy = a.xy / b;                                    break;

                case blast_operation.and:               a.xy = math.select(0f, 1f, a.xy != 0 & b != 0);     break;
                case blast_operation.or:                a.xy = math.select(0f, 1f, a.xy != 0 | b != 0);     break;

                case blast_operation.xor:               a.xy = math.select(0f, 1f, a.xy != 0 ^ b != 0);     break;
                case blast_operation.greater:           a.xy = math.select(0f, 1f, a.xy > b);               break;
                case blast_operation.smaller:           a.xy = math.select(0f, 1f, a.xy < b);               break;
                case blast_operation.smaller_equals:    a.xy = math.select(0f, 1f, a.xy <= b);              break;
                case blast_operation.greater_equals:    a.xy = math.select(0f, 1f, a.xy >= b);              break;
                case blast_operation.equals:            a.xy = math.select(0f, 1f, a.xy == b);              break;
                case blast_operation.not_equals:        a.xy = math.select(0f, 1f, a.xy != b);              break;
                case blast_operation.not:               a.xy = math.select(0f, 1f, b == 0);                 break;
            }
        }

        /// <summary>
        /// f4.xy = f4.xy op i1 
        /// </summary>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        static void handle_op_f2i1(in blast_operation op, ref V4 a, in int b)
        {
            switch (op)
            {
                default:
                case blast_operation.nop: a.xy = b; break;
                case blast_operation.add: a.xy = a.xy + b; break;
                case blast_operation.substract: a.xy = a.xy - b; break;
                case blast_operation.multiply: a.xy = a.xy * b; break;
                case blast_operation.divide: a.xy = a.xy / b; break;

                case blast_operation.and: a.xy = math.select(0f, 1f, a.xy != 0 & b != 0); break;
                case blast_operation.or: a.xy = math.select(0f, 1f, a.xy != 0 | b != 0); break;

                case blast_operation.xor: a.xy = math.select(0f, 1f, a.xy != 0 ^ b != 0); break;
                case blast_operation.greater: a.xy = math.select(0f, 1f, a.xy > b); break;
                case blast_operation.smaller: a.xy = math.select(0f, 1f, a.xy < b); break;
                case blast_operation.smaller_equals: a.xy = math.select(0f, 1f, a.xy <= b); break;
                case blast_operation.greater_equals: a.xy = math.select(0f, 1f, a.xy >= b); break;
                case blast_operation.equals: a.xy = math.select(0f, 1f, a.xy == b); break;
                case blast_operation.not_equals: a.xy = math.select(0f, 1f, a.xy != b); break;
                case blast_operation.not: a.xy = math.select(0f, 1f, b == 0); break;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        static void handle_op_f2i1(in blast_operation op, ref V4 a, in V1 b)
        {
            handle_op_f2i1(in op, ref a, CodeUtils.ReInterpret<float, int>(b));
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        static void handle_op_f2i1_n(in blast_operation op, ref V4 a, in V1 b)
        {
            handle_op_f2i1(in op, ref a, -CodeUtils.ReInterpret<float, int>(b));
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        static void handle_op_i2i1(in blast_operation op, ref V4 a, in V1 b)
        {
            int4 i = CodeUtils.ReInterpret<V4, int4>(a); 
            handle_op_i2i1(in op, ref i, CodeUtils.ReInterpret<float, int>(b));
            a = CodeUtils.ReInterpret<int4, V4>(i);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        static void handle_op_i2i1_n(in blast_operation op, ref V4 a, in V1 b)
        {
            int4 i = CodeUtils.ReInterpret<V4, int4>(a);
            handle_op_i2i1(in op, ref i, -CodeUtils.ReInterpret<float, int>(b));
            a = CodeUtils.ReInterpret<int4, V4>(i);
        }

        /// <summary>
        /// f4.xy = f4.xy op i1 
        /// </summary>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        static void handle_op_i2i1(in blast_operation op, ref int4 a, in int b)
        {
            switch (op)
            {
                default:
                case blast_operation.nop: a.xy = b; break;
                case blast_operation.add: a.xy = a.xy + b; break;
                case blast_operation.substract: a.xy = a.xy - b; break;
                case blast_operation.multiply: a.xy = a.xy * b; break;
                case blast_operation.divide: a.xy = a.xy / b; break;

                case blast_operation.and: a.xy = math.select(math.int2(0), 1, a.xy != 0 & b != 0); break;
                case blast_operation.or: a.xy = math.select(math.int2(0), 1, a.xy != 0 | b != 0); break;

                case blast_operation.xor: a.xy = math.select(math.int2(0), 1, a.xy != 0 ^ b != 0); break;
                case blast_operation.greater: a.xy = math.select(math.int2(0), 1, a.xy > b); break;
                case blast_operation.smaller: a.xy = math.select(math.int2(0), 1, a.xy < b); break;
                case blast_operation.smaller_equals: a.xy = math.select(math.int2(0), 1, a.xy <= b); break;
                case blast_operation.greater_equals: a.xy = math.select(math.int2(0), 1, a.xy >= b); break;
                case blast_operation.equals: a.xy = math.select(math.int2(0), 1, a.xy == b); break;
                case blast_operation.not_equals: a.xy = math.select(math.int2(0), 1, a.xy != b); break;
                case blast_operation.not: a.xy = math.select(math.int2(0), 1, b == 0); break;
            }
        }


        /// <summary>
        /// f4.xy = f4.xy op f2 
        /// </summary>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        static void handle_op_f2f2(in blast_operation op, ref V4 a, in V2 b)
        {
            switch (op)
            {
                default:
                case blast_operation.nop:               a.xy = b.xy;                                           break;
                case blast_operation.add:               a.xy = a.xy + b.xy;                                    break;
                case blast_operation.substract:         a.xy = a.xy - b.xy;                                    break;
                case blast_operation.multiply:          a.xy = a.xy * b.xy;                                    break;
                case blast_operation.divide:            a.xy = a.xy / b.xy;                                    break;

                case blast_operation.and:               a.xy = math.select(0f, 1f, a.xy != 0 & b.xy != 0);     break;
                case blast_operation.or:                a.xy = math.select(0f, 1f, a.xy != 0 | b.xy != 0);     break;

                case blast_operation.xor:               a.xy = math.select(0f, 1f, a.xy != 0 ^ b.xy != 0);     break;
                case blast_operation.greater:           a.xy = math.select(0f, 1f, a.xy > b.xy);               break;
                case blast_operation.smaller:           a.xy = math.select(0f, 1f, a.xy < b.xy);               break;
                case blast_operation.smaller_equals:    a.xy = math.select(0f, 1f, a.xy <= b.xy);              break;
                case blast_operation.greater_equals:    a.xy = math.select(0f, 1f, a.xy >= b.xy);              break;
                case blast_operation.equals:            a.xy = math.select(0f, 1f, a.xy == b.xy);              break;
                case blast_operation.not_equals:        a.xy = math.select(0f, 1f, a.xy != b.xy);              break;
                case blast_operation.not:               a.xy = math.select(0f, 1f, b.xy == 0);                 break;
            }
        }

        /// <summary>
        /// f4.xy = f4.xy op f2 
        /// </summary>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        static void handle_op_f2i2(in blast_operation op, ref V4 a, in int2 b)
        {
            switch (op)
            {
                default:
                case blast_operation.nop: a.xy = b.xy; break;
                case blast_operation.add: a.xy = a.xy + b.xy; break;
                case blast_operation.substract: a.xy = a.xy - b.xy; break;
                case blast_operation.multiply: a.xy = a.xy * b.xy; break;
                case blast_operation.divide: a.xy = a.xy / b.xy; break;

                case blast_operation.and: a.xy = math.select(0f, 1f, a.xy != 0 & b.xy != 0); break;
                case blast_operation.or: a.xy = math.select(0f, 1f, a.xy != 0 | b.xy != 0); break;

                case blast_operation.xor: a.xy = math.select(0f, 1f, a.xy != 0 ^ b.xy != 0); break;
                case blast_operation.greater: a.xy = math.select(0f, 1f, a.xy > b.xy); break;
                case blast_operation.smaller: a.xy = math.select(0f, 1f, a.xy < b.xy); break;
                case blast_operation.smaller_equals: a.xy = math.select(0f, 1f, a.xy <= b.xy); break;
                case blast_operation.greater_equals: a.xy = math.select(0f, 1f, a.xy >= b.xy); break;
                case blast_operation.equals: a.xy = math.select(0f, 1f, a.xy == b.xy); break;
                case blast_operation.not_equals: a.xy = math.select(0f, 1f, a.xy != b.xy); break;
                case blast_operation.not: a.xy = math.select(0f, 1f, b.xy == 0); break;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        static void handle_op_f2i2(in blast_operation op, ref V4 a, in V2 b)
        {
            handle_op_f2i2(in op, ref a, CodeUtils.ReInterpret<float2, int2>(b));
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        static void handle_op_f2i2_n(in blast_operation op, ref V4 a, in V2 b)
        {
            handle_op_f2i2(in op, ref a, -CodeUtils.ReInterpret<float2, int2>(b));
        }



        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        static void handle_op_i2i2(in blast_operation op, ref V4 a, in V2 b)
        {
            int4 i = CodeUtils.ReInterpret<V4, int4>(a);
            handle_op_i2i2(in op, ref i, CodeUtils.ReInterpret<float2, int2>(b));
            a = CodeUtils.ReInterpret<int4, V4>(i);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        static void handle_op_i2i2_n(in blast_operation op, ref V4 a, in V2 b)
        {
            int4 i = CodeUtils.ReInterpret<V4, int4>(a);
            handle_op_i2i2(in op, ref i, -CodeUtils.ReInterpret<float2, int2>(b));
            a = CodeUtils.ReInterpret<int4, V4>(i);
        }

        /// <summary>
        /// f4.xy = f4.xy op f2 
        /// </summary>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        static void handle_op_i2i2(in blast_operation op, ref int4 a, in int2 b)
        {
            switch (op)
            {
                default:
                case blast_operation.nop: a.xy = b.xy; break;
                case blast_operation.add: a.xy = a.xy + b.xy; break;
                case blast_operation.substract: a.xy = a.xy - b.xy; break;
                case blast_operation.multiply: a.xy = a.xy * b.xy; break;
                case blast_operation.divide: a.xy = a.xy / b.xy; break;

                case blast_operation.and: a.xy = math.select(math.int2(0), 1, a.xy != 0 & b.xy != 0); break;
                case blast_operation.or: a.xy = math.select(math.int2(0), 1, a.xy != 0 | b.xy != 0); break;

                case blast_operation.xor: a.xy = math.select(math.int2(0), 1, a.xy != 0 ^ b.xy != 0); break;
                case blast_operation.greater: a.xy = math.select(math.int2(0), 1, a.xy > b.xy); break;
                case blast_operation.smaller: a.xy = math.select(math.int2(0), 1, a.xy < b.xy); break;
                case blast_operation.smaller_equals: a.xy = math.select(math.int2(0), 1, a.xy <= b.xy); break;
                case blast_operation.greater_equals: a.xy = math.select(math.int2(0), 1, a.xy >= b.xy); break;
                case blast_operation.equals: a.xy = math.select(math.int2(0), 1, a.xy == b.xy); break;
                case blast_operation.not_equals: a.xy = math.select(math.int2(0), 1, a.xy != b.xy); break;
                case blast_operation.not: a.xy = math.select(math.int2(0), 1, b.xy == 0); break;
            }
        }


        /// <summary>
        /// f4.xyz = f4.xyz op f1 
        /// </summary>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        static void handle_op_f3f1(in blast_operation op, ref V4 a, in V1 b)
        {
            switch (op)
            {
                default:
                case blast_operation.nop:               a.xyz = b;                                           break;
                case blast_operation.add:               a.xyz = a.xyz + b;                                    break;
                case blast_operation.substract:         a.xyz = a.xyz - b;                                    break;
                case blast_operation.multiply:          a.xyz = a.xyz * b;                                    break;
                case blast_operation.divide:            a.xyz = a.xyz / b;                                    break;

                case blast_operation.and:               a.xyz = math.select(0f, 1f, a.xyz != 0 & b != 0);     break;
                case blast_operation.or:                a.xyz = math.select(0f, 1f, a.xyz != 0 | b != 0);     break;

                case blast_operation.xor:               a.xyz = math.select(0f, 1f, a.xyz != 0 ^ b != 0);     break;
                case blast_operation.greater:           a.xyz = math.select(0f, 1f, a.xyz > b);               break;
                case blast_operation.smaller:           a.xyz = math.select(0f, 1f, a.xyz < b);               break;
                case blast_operation.smaller_equals:    a.xyz = math.select(0f, 1f, a.xyz <= b);              break;
                case blast_operation.greater_equals:    a.xyz = math.select(0f, 1f, a.xyz >= b);              break;
                case blast_operation.equals:            a.xyz = math.select(0f, 1f, a.xyz == b);              break;
                case blast_operation.not_equals:        a.xyz = math.select(0f, 1f, a.xyz != b);              break;
                case blast_operation.not:               a.xyz = math.select(0f, 1f, b == 0);                 break;
            }
        }

        /// <summary>
        /// i4.xyz = i4.xyz op i1 
        /// </summary>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        static void handle_op_i3i1(in blast_operation op, ref V4 a, in int b)
        {
            int4 i = CodeUtils.ReInterpret<V4, int4>(a);
            handle_op_i3i1(in op, ref i, in b);
            a = CodeUtils.ReInterpret<int4, V4>(i);
        }

        /// <summary>
        /// i4.xyz = i4.xyz op i1 
        /// </summary>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        static void handle_op_i3i1(in blast_operation op, ref int4 a, in int b)
        {
            switch (op)
            {
                default:
                case blast_operation.nop: a.xyz = math.int3(b); break;
                case blast_operation.add: a.xyz = a.xyz + math.int3(b); break;
                case blast_operation.substract: a.xyz = a.xyz - b; break;
                case blast_operation.multiply: a.xyz = a.xyz * b; break;
                case blast_operation.divide: a.xyz = a.xyz / b; break;

                case blast_operation.and: a.xyz = math.select(math.int3(0), 1, a.xyz != 0 & b != 0); break;
                case blast_operation.or: a.xyz = math.select(math.int3(0), 1, a.xyz != 0 | b != 0); break;

                case blast_operation.xor: a.xyz = math.select(math.int3(0), 1, a.xyz != 0 ^ b != 0); break;
                case blast_operation.greater: a.xyz = math.select(math.int3(0), 1, a.xyz > b); break;
                case blast_operation.smaller: a.xyz = math.select(math.int3(0), 1, a.xyz < b); break;
                case blast_operation.smaller_equals: a.xyz = math.select(math.int3(0), 1, a.xyz <= b); break;
                case blast_operation.greater_equals: a.xyz = math.select(math.int3(0), 1, a.xyz >= b); break;
                case blast_operation.equals: a.xyz = math.select(math.int3(0), 1, a.xyz == b); break;
                case blast_operation.not_equals: a.xyz = math.select(math.int3(0), 1, a.xyz != b); break;
                case blast_operation.not: a.xyz = math.select(0, 1, b == 0); break;
            }
        }



        /// <summary>
        /// f4.xyz = f4.xyz op f3 
        /// </summary>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        static void handle_op_f3f3(in blast_operation op, ref V4 a, in V3 b)
        {
            switch (op)
            {
                default:
                case blast_operation.nop:               a.xyz = b.xyz;                                            break;
                case blast_operation.add:               a.xyz = a.xyz + b.xyz;                                    break;
                case blast_operation.substract:         a.xyz = a.xyz - b.xyz;                                    break;
                case blast_operation.multiply:          a.xyz = a.xyz * b.xyz;                                    break;
                case blast_operation.divide:            a.xyz = a.xyz / b.xyz;                                    break;

                case blast_operation.and:               a.xyz = math.select(0f, 1f, a.xyz != 0 & b.xyz != 0);     break;
                case blast_operation.or:                a.xyz = math.select(0f, 1f, a.xyz != 0 | b.xyz != 0);     break;

                case blast_operation.xor:               a.xyz = math.select(0f, 1f, a.xyz != 0 ^ b.xyz != 0);     break;
                case blast_operation.greater:           a.xyz = math.select(0f, 1f, a.xyz > b.xyz);               break;
                case blast_operation.smaller:           a.xyz = math.select(0f, 1f, a.xyz < b.xyz);               break;
                case blast_operation.smaller_equals:    a.xyz = math.select(0f, 1f, a.xyz <= b.xyz);              break;
                case blast_operation.greater_equals:    a.xyz = math.select(0f, 1f, a.xyz >= b.xyz);              break;
                case blast_operation.equals:            a.xyz = math.select(0f, 1f, a.xyz == b.xyz);              break;
                case blast_operation.not_equals:        a.xyz = math.select(0f, 1f, a.xyz != b.xyz);              break;
                case blast_operation.not:               a.xyz = math.select(0f, 1f, b.xyz == 0);                  break;
            }
        }

        /// <summary>
        /// i4.xyz = i4.xyz op i3 
        /// </summary>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        static void handle_op_i3i3(in blast_operation op, ref int4 a, in int3 b)
        {                       
            switch (op)
            {
                default:
                case blast_operation.nop:            a.xyz = b.xyz; break;
                case blast_operation.add:            a.xyz = a.xyz + b.xyz; break;
                case blast_operation.substract:      a.xyz = a.xyz - b.xyz; break;
                case blast_operation.multiply:       a.xyz = a.xyz * b.xyz; break;
                case blast_operation.divide:         a.xyz = a.xyz / b.xyz; break;

                case blast_operation.and:            a.xyz = math.select(math.int3(0), 1, a.xyz != 0 & b.xyz != 0); break;
                case blast_operation.or:             a.xyz = math.select(math.int3(0), 1, a.xyz != 0 | b.xyz != 0); break;

                case blast_operation.xor:            a.xyz = math.select(math.int3(0), 1, a.xyz != 0 ^ b.xyz != 0); break;
                case blast_operation.greater:        a.xyz = math.select(math.int3(0), 1, a.xyz > b.xyz); break;
                case blast_operation.smaller:        a.xyz = math.select(math.int3(0), 1, a.xyz < b.xyz); break;
                case blast_operation.smaller_equals: a.xyz = math.select(math.int3(0), 1, a.xyz <= b.xyz); break;
                case blast_operation.greater_equals: a.xyz = math.select(math.int3(0), 1, a.xyz >= b.xyz); break;
                case blast_operation.equals:         a.xyz = math.select(math.int3(0), 1, a.xyz == b.xyz); break;
                case blast_operation.not_equals:     a.xyz = math.select(math.int3(0), 1, a.xyz != b.xyz); break;
                case blast_operation.not:            a.xyz = math.select(math.int3(0), 1, b.xyz == 0); break;
            }
        }


        /// <summary>
        /// i4.xyz = i4.xyz op i3 
        /// </summary>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        static void handle_op_i3i3(in blast_operation op, ref V4 a, in int3 b)
        {
            int4 i = CodeUtils.ReInterpret<V4, int4>(a);
            handle_op_i3i3(in op, ref i, in b);
            a = CodeUtils.ReInterpret<int4, V4>(i);
        }



        /// <summary>
        /// f4.xyzw = f4.xyzw op f1 
        /// </summary>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        static void handle_op_f4f1(in blast_operation op, ref V4 a, in V1 b)
        {
            switch (op)
            {
                default:
                case blast_operation.nop:               a = b;                                            break;
                case blast_operation.add:               a = a + b;                                        break;
                case blast_operation.substract:         a = a - b;                                        break;
                case blast_operation.multiply:          a = a * b;                                        break;
                case blast_operation.divide:            a = a / b;                                        break;
                                                                                                          
                case blast_operation.and:               a = math.select(0f, 1f, a != 0 & b != 0);         break;
                case blast_operation.or:                a = math.select(0f, 1f, a != 0 | b != 0);         break;
                                                                                                          
                case blast_operation.xor:               a = math.select(0f, 1f, a != 0 ^ b != 0);         break;
                case blast_operation.greater:           a = math.select(0f, 1f, a > b);                   break;
                case blast_operation.smaller:           a = math.select(0f, 1f, a < b);                   break;
                case blast_operation.smaller_equals:    a = math.select(0f, 1f, a <= b);                  break;
                case blast_operation.greater_equals:    a = math.select(0f, 1f, a >= b);                  break;
                case blast_operation.equals:            a = math.select(0f, 1f, a == b);                  break;
                case blast_operation.not_equals:        a = math.select(0f, 1f, a != b);                  break;
                case blast_operation.not:               a = math.select(0f, 1f, b == 0);                  break;
            }
        }



        /// <summary>
        /// i4.xyzw = i4.xyzw op i1 
        /// </summary>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        static void handle_op_i4i1(in blast_operation op, ref int4 a, in int b)
        {
            switch (op)
            {
                default:
                case blast_operation.nop: a = b; break;
                case blast_operation.add: a = a + b; break;
                case blast_operation.substract: a = a - b; break;
                case blast_operation.multiply: a = a * b; break;
                case blast_operation.divide: a = a / b; break;

                case blast_operation.and: a = math.select(int4.zero, 1, a != 0 & b != 0); break;
                case blast_operation.or: a = math.select(int4.zero, 1, a != 0 | b != 0); break;

                case blast_operation.xor: a = math.select(int4.zero, 1, a != 0 ^ b != 0); break;
                case blast_operation.greater: a = math.select(int4.zero, 1, a > b); break;
                case blast_operation.smaller: a = math.select(int4.zero, 1, a < b); break;
                case blast_operation.smaller_equals: a = math.select(int4.zero, 1, a <= b); break;
                case blast_operation.greater_equals: a = math.select(int4.zero, 1, a >= b); break;
                case blast_operation.equals: a = math.select(int4.zero, 1, a == b); break;
                case blast_operation.not_equals: a = math.select(int4.zero, 1, a != b); break;
                case blast_operation.not: a = math.select(int4.zero, 1, b == 0); break;
            }
        }

        /// <summary>
        /// i4.xyz = i4.xyz op i1 
        /// </summary>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        static void handle_op_i4i1(in blast_operation op, ref V4 a, in int b)
        {
            int4 i = CodeUtils.ReInterpret<V4, int4>(a);
            handle_op_i4i1(in op, ref i, in b);
            a = CodeUtils.ReInterpret<int4, V4>(i);
        }



        /// <summary>
        /// f4.xyzw = f4.xyzw op f4 
        /// </summary>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        static void handle_op_f4f4(in blast_operation op, ref V4 a, in V4 b)
        {
            switch (op)
            {
                default:
                case blast_operation.nop: a = b; break;
                case blast_operation.add: a = a + b; break;
                case blast_operation.substract: a = a - b; break;
                case blast_operation.multiply: a = a * b; break;
                case blast_operation.divide: a = a / b; break;

                case blast_operation.and: a = math.select(0f, 1f, a != 0 & b != 0); break;
                case blast_operation.or: a = math.select(0f, 1f, a != 0 | b != 0); break;

                case blast_operation.xor: a = math.select(0f, 1f, a != 0 ^ b != 0); break;
                case blast_operation.greater: a = math.select(0f, 1f, a > b); break;
                case blast_operation.smaller: a = math.select(0f, 1f, a < b); break;
                case blast_operation.smaller_equals: a = math.select(0f, 1f, a <= b); break;
                case blast_operation.greater_equals: a = math.select(0f, 1f, a >= b); break;
                case blast_operation.equals: a = math.select(0f, 1f, a == b); break;
                case blast_operation.not_equals: a = math.select(0f, 1f, a != b); break;
                case blast_operation.not: a = math.select(0f, 1f, b == 0); break;
            }
        }
        /// <summary>
        /// i4.xyzw = i4.xyzw op i4 
        /// </summary>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        static void handle_op_i4i4(in blast_operation op, ref int4 a, in int4 b)
        {
            switch (op)
            {
                default:
                case blast_operation.nop: a = b; break;
                case blast_operation.add: a = a + b; break;
                case blast_operation.substract: a = a - b; break;
                case blast_operation.multiply: a = a * b; break;
                case blast_operation.divide: a = a / b; break;

                case blast_operation.and: a = math.select(int4.zero, 1, a != 0 & b != 0); break;
                case blast_operation.or: a = math.select(int4.zero, 1, a != 0 | b != 0); break;

                case blast_operation.xor: a = math.select(int4.zero, 1, a != 0 ^ b != 0); break;
                case blast_operation.greater: a = math.select(int4.zero, 1, a > b); break;
                case blast_operation.smaller: a = math.select(int4.zero, 1, a < b); break;
                case blast_operation.smaller_equals: a = math.select(int4.zero, 1, a <= b); break;
                case blast_operation.greater_equals: a = math.select(int4.zero, 1, a >= b); break;
                case blast_operation.equals: a = math.select(int4.zero, 1, a == b); break;
                case blast_operation.not_equals: a = math.select(int4.zero, 1, a != b); break;
                case blast_operation.not: a = math.select(int4.zero, 1, b == 0); break;
            }
        }


        /// <summary>
        /// i4 = i4 op i4 
        /// </summary>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        static void handle_op_i4i4(in blast_operation op, ref V4 a, in int4 b)
        {
            int4 i = CodeUtils.ReInterpret<V4, int4>(a);
            handle_op_i4i4(in op, ref i, in b);
            a = CodeUtils.ReInterpret<int4, V4>(i);
        }
    }
}
