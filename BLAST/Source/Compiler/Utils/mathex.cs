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


using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Unity.Mathematics;
using Unity.Collections; 

namespace NSS.Blast.Interpretor
{
    /// <summary>
    /// implements functions dropped from unity.mathematics between versions.. 
    /// 
    /// - square 
    /// - euler 
    /// </summary>
#if ENABLE_IL2CPP
    [Unity.Burst.BurstCompile(Unity.Burst.FloatPrecision.Medium, Unity.Burst.FloatMode.Fast)]
    [Unity.IL2CPP.CompilerServices.Il2CppSetOption(Unity.IL2CPP.CompilerServices.Option.NullChecks, false)]
    [Unity.IL2CPP.CompilerServices.Il2CppSetOption(Unity.IL2CPP.CompilerServices.Option.ArrayBoundsChecks, false)]
    [Unity.IL2CPP.CompilerServices.Il2CppEagerStaticClassConstructionAttribute()]
#elif !STANDALONE_VSBUILD

    [Unity.Burst.BurstCompile]
#endif

    public static class mathex
    {

        /// <summary>
        /// Returns the Euler angle representation of the quaternion. The returned angles depend on the specified order to apply the
        /// three rotations around the principal axes. All rotation angles are in radians and clockwise when looking along the
        /// rotation axis towards the origin.
        /// When the rotation order is known at compile time, to get the best performance you should use the specific
        /// Euler rotation constructors such as EulerZXY(...).
        /// </summary>
        /// <param name="q">The quaternion to convert to Euler angles.</param>
        /// <param name="order">The order in which the rotations are applied.</param>
        /// <returns>The Euler angle representation of the quaternion in the specified order.</returns>
#if STANDALONE_VSBUILD || ENABLE_IL2CPP
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static float3 Euler(float4 q, math.RotationOrder order = math.RotationOrder.Default)
        {
            switch (order)
            {
                case math.RotationOrder.XYZ:
                    return mathex.EulerXYZ(q);
                case math.RotationOrder.XZY:
                    return mathex.EulerXZY(q);
                case math.RotationOrder.YXZ:
                    return mathex.EulerYXZ(q);
                case math.RotationOrder.YZX:
                    return mathex.EulerYZX(q);
                case math.RotationOrder.ZXY:
                    return mathex.EulerZXY(q);
                case math.RotationOrder.ZYX:
                    return mathex.EulerZYX(q);
                default:
                    return Unity.Mathematics.float3.zero;
            }
        }



        /// <summary>
        /// Returns the Euler angle representation of the quaternion following the XYZ rotation order.
        /// All rotation angles are in radians and clockwise when looking along the rotation axis towards the origin.
        /// </summary>
        /// <param name="q">The quaternion to convert to Euler angles.</param>
        /// <returns>The Euler angle representation of the quaternion in XYZ order.</returns>
#if STANDALONE_VSBUILD || ENABLE_IL2CPP
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static float3 EulerXYZ(float4 qv)
        {
            const float epsilon = 1e-6f;
            const float cutoff = (1f - 2f * epsilon) * (1f - 2f * epsilon);

            // prepare the data
            var d1 = qv * qv.wwww * math.float4(2f); //xw, yw, zw, ww
            var d2 = qv * qv.yzxw * math.float4(2f); //xy, yz, zx, ww
            var d3 = qv * qv;
            var euler = Unity.Mathematics.float3.zero;

            var y1 = d2.z - d1.y;
            if (y1 * y1 < cutoff)
            {
                var x1 = d2.y + d1.x;
                var x2 = d3.z + d3.w - d3.y - d3.x;
                var z1 = d2.x + d1.z;
                var z2 = d3.x + d3.w - d3.y - d3.z;
                euler = math.float3(math.atan2(x1, x2), -math.asin(y1), math.atan2(z1, z2));
            }
            else //xzx
            {
                y1 = math.clamp(y1, -1f, 1f);
                var abcd = new float4(d2.z, d1.y, d2.x, d1.z);
                var x1 = 2f * (abcd.x * abcd.w + abcd.y * abcd.z); //2(ad+bc)
                var x2 = math.csum(abcd * abcd * math.float4(-1f, 1f, -1f, 1f));
                euler = math.float3(math.atan2(x1, x2), -math.asin(y1), 0f);
            }

            return euler;
        }

        /// <summary>
        /// Returns the Euler angle representation of the quaternion following the XZY rotation order.
        /// All rotation angles are in radians and clockwise when looking along the rotation axis towards the origin.
        /// </summary>
        /// <param name="q">The quaternion to convert to Euler angles.</param>
        /// <returns>The Euler angle representation of the quaternion in XZY order.</returns>
#if STANDALONE_VSBUILD || ENABLE_IL2CPP
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static float3 EulerXZY(float4 qv)
        {
            const float epsilon = 1e-6f;
            const float cutoff = (1f - 2f * epsilon) * (1f - 2f * epsilon);

            // prepare the data
            var d1 = qv * qv.wwww * math.float4(2f); //xw, yw, zw, ww
            var d2 = qv * qv.yzxw * math.float4(2f); //xy, yz, zx, ww
            var d3 = qv * qv;
            var euler = Unity.Mathematics.float3.zero;

            var y1 = d2.x + d1.z;
            if (y1 * y1 < cutoff)
            {
                var x1 = -d2.y + d1.x;
                var x2 = d3.y + d3.w - d3.z - d3.x;
                var z1 = -d2.z + d1.y;
                var z2 = d3.x + d3.w - d3.y - d3.z;
                euler = math.float3(math.atan2(x1, x2), math.asin(y1), math.atan2(z1, z2));
            }
            else //xyx
            {
                y1 = math.clamp(y1, -1f, 1f);
                var abcd = new float4(d2.x, d1.z, d2.z, d1.y);
                var x1 = 2f * (abcd.x * abcd.w + abcd.y * abcd.z); //2(ad+bc)
                var x2 = math.csum(abcd * abcd * math.float4(-1f, 1f, -1f, 1f));
                euler = math.float3(math.atan2(x1, x2), math.asin(y1), 0f);
            }

            return euler.xzy;
        }

        /// <summary>
        /// Returns the Euler angle representation of the quaternion following the YXZ rotation order.
        /// All rotation angles are in radians and clockwise when looking along the rotation axis towards the origin.
        /// </summary>
        /// <param name="q">The quaternion to convert to Euler angles.</param>
        /// <returns>The Euler angle representation of the quaternion in YXZ order.</returns>
#if STANDALONE_VSBUILD || ENABLE_IL2CPP
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static float3 EulerYXZ(float4 qv)
        {
            const float epsilon = 1e-6f;
            const float cutoff = (1f - 2f * epsilon) * (1f - 2f * epsilon);

            // prepare the data
            var d1 = qv * qv.wwww * math.float4(2f); //xw, yw, zw, ww
            var d2 = qv * qv.yzxw * math.float4(2f); //xy, yz, zx, ww
            var d3 = qv * qv;
            var euler = Unity.Mathematics.float3.zero;

            var y1 = d2.y + d1.x;
            if (y1 * y1 < cutoff)
            {
                var x1 = -d2.z + d1.y;
                var x2 = d3.z + d3.w - d3.x - d3.y;
                var z1 = -d2.x + d1.z;
                var z2 = d3.y + d3.w - d3.z - d3.x;
                euler = math.float3(math.atan2(x1, x2), math.asin(y1), math.atan2(z1, z2));
            }
            else //yzy
            {
                y1 = math.clamp(y1, -1f, 1f);
                var abcd = math.float4(d2.x, d1.z, d2.y, d1.x);
                var x1 = 2f * (abcd.x * abcd.w + abcd.y * abcd.z); //2(ad+bc)
                var x2 = math.csum(abcd * abcd * math.float4(-1f, 1f, -1f, 1f));
                euler = math.float3(math.atan2(x1, x2), math.asin(y1), 0f);
            }

            return euler.yxz;
        }

        /// <summary>
        /// Returns the Euler angle representation of the quaternion following the YZX rotation order.
        /// All rotation angles are in radians and clockwise when looking along the rotation axis towards the origin.
        /// </summary>
        /// <param name="q">The quaternion to convert to Euler angles.</param>
        /// <returns>The Euler angle representation of the quaternion in YZX order.</returns>
#if STANDALONE_VSBUILD || ENABLE_IL2CPP
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static float3 EulerYZX(float4 qv)
        {
            const float epsilon = 1e-6f;
            const float cutoff = (1f - 2f * epsilon) * (1f - 2f * epsilon);

            // prepare the data
            var d1 = qv * qv.wwww * math.float4(2f); //xw, yw, zw, ww
            var d2 = qv * qv.yzxw * math.float4(2f); //xy, yz, zx, ww
            var d3 = qv * qv;
            var euler = Unity.Mathematics.float3.zero;

            var y1 = d2.x - d1.z;
            if (y1 * y1 < cutoff)
            {
                var x1 = d2.z + d1.y;
                var x2 = d3.x + d3.w - d3.z - d3.y;
                var z1 = d2.y + d1.x;
                var z2 = d3.y + d3.w - d3.x - d3.z;
                euler = math.float3(math.atan2(x1, x2), -math.asin(y1), math.atan2(z1, z2));
            }
            else //yxy
            {
                y1 = math.clamp(y1, -1f, 1f);
                var abcd = math.float4(d2.x, d1.z, d2.y, d1.x);
                var x1 = 2f * (abcd.x * abcd.w + abcd.y * abcd.z); //2(ad+bc)
                var x2 = math.csum(abcd * abcd * math.float4(-1f, 1f, -1f, 1f));
                euler = math.float3(math.atan2(x1, x2), -math.asin(y1), 0f);
            }

            return euler.zxy;
        }

        /// <summary>
        /// Returns the Euler angle representation of the quaternion following the ZXY rotation order.
        /// All rotation angles are in radians and clockwise when looking along the rotation axis towards the origin.
        /// </summary>
        /// <param name="q">The quaternion to convert to Euler angles.</param>
        /// <returns>The Euler angle representation of the quaternion in ZXY order.</returns>
#if STANDALONE_VSBUILD || ENABLE_IL2CPP
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static float3 EulerZXY(float4 qv)
        {
            const float epsilon = 1e-6f;
            const float cutoff = (1f - 2f * epsilon) * (1f - 2f * epsilon);

            // prepare the data
            var d1 = qv * qv.wwww * math.float4(2f); //xw, yw, zw, ww
            var d2 = qv * qv.yzxw * math.float4(2f); //xy, yz, zx, ww
            var d3 = qv * qv;
            var euler = Unity.Mathematics.float3.zero;

            var y1 = d2.y - d1.x;
            if (y1 * y1 < cutoff)
            {
                var x1 = d2.x + d1.z;
                var x2 = d3.y + d3.w - d3.x - d3.z;
                var z1 = d2.z + d1.y;
                var z2 = d3.z + d3.w - d3.x - d3.y;
                euler = math.float3(math.atan2(x1, x2), -math.asin(y1), math.atan2(z1, z2));
            }
            else //zxz
            {
                y1 = math.clamp(y1, -1f, 1f);
                var abcd = math.float4(d2.z, d1.y, d2.y, d1.x);
                var x1 = 2f * (abcd.x * abcd.w + abcd.y * abcd.z); //2(ad+bc)
                var x2 = math.csum(abcd * abcd * math.float4(-1f, 1f, -1f, 1f));
                euler = math.float3(math.atan2(x1, x2), -math.asin(y1), 0f);
            }

            return euler.yzx;
        }

        /// <summary>
        /// Returns the Euler angle representation of the quaternion following the ZYX rotation order.
        /// All rotation angles are in radians and clockwise when looking along the rotation axis towards the origin.
        /// </summary>
        /// <param name="q">The quaternion to convert to Euler angles.</param>
        /// <returns>The Euler angle representation of the quaternion in ZYX order.</returns>
#if STANDALONE_VSBUILD || ENABLE_IL2CPP
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static float3 EulerZYX(float4 qv)
        {
            const float epsilon = 1e-6f;
            const float cutoff = (1f - 2f * epsilon) * (1f - 2f * epsilon);

            var d1 = qv * qv.wwww * math.float4(2f); //xw, yw, zw, ww
            var d2 = qv * qv.yzxw * math.float4(2f); //xy, yz, zx, ww
            var d3 = qv * qv;
            var euler = Unity.Mathematics.float3.zero;

            var y1 = d2.z + d1.y;
            if (y1 * y1 < cutoff)
            {
                var x1 = -d2.x + d1.z;
                var x2 = d3.x + d3.w - d3.y - d3.z;
                var z1 = -d2.y + d1.x;
                var z2 = d3.z + d3.w - d3.y - d3.x;
                euler = math.float3(math.atan2(x1, x2), math.asin(y1), math.atan2(z1, z2));
            }
            else //zxz
            {
                y1 = math.clamp(y1, -1f, 1f);
                var abcd = math.float4(d2.z, d1.y, d2.y, d1.x);
                var x1 = 2f * (abcd.x * abcd.w + abcd.y * abcd.z); //2(ad+bc)
                var x2 = math.csum(abcd * abcd * math.float4(-1f, 1f, -1f, 1f));
                euler = math.float3(math.atan2(x1, x2), math.asin(y1), 0f);
            }

            return euler.zyx;
        }


        /// <summary>Returns the angle in radians between two unit quaternions.</summary>
        /// <param name="q1">The first quaternion.</param>
        /// <param name="q2">The second quaternion.</param>
        /// <returns>The angle between two unit quaternions.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float angle(float4 q1, float4 q2)
        {
            float diff = math.asin(math.length(math.normalize(math.mul(math.conjugate(new quaternion(q1)), new quaternion(q2))).value.xyz));
            return diff + diff;
        }

        /// <summary>
        /// Computes the square (x * x) of the input argument x.
        /// </summary>
        /// <param name="x">Value to square.</param>
        /// <returns>Returns the square of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float square(float x)
        {
            return x * x;
        }

        /// <summary>
        /// Computes the component-wise square (x * x) of the input argument x.
        /// </summary>
        /// <param name="x">Value to square.</param>
        /// <returns>Returns the square of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 square(float2 x)
        {
            return x * x;
        }

        /// <summary>
        /// Computes the component-wise square (x * x) of the input argument x.
        /// </summary>
        /// <param name="x">Value to square.</param>
        /// <returns>Returns the square of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 square(float3 x)
        {
            return x * x;
        }

        /// <summary>
        /// Computes the component-wise square (x * x) of the input argument x.
        /// </summary>
        /// <param name="x">Value to square.</param>
        /// <returns>Returns the square of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float4 square(float4 x)
        {
            return x * x;
        }

        /// <summary>
        /// Computes the square (x * x) of the input argument x.
        /// </summary>
        /// <param name="x">Value to square.</param>
        /// <returns>Returns the square of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double square(double x)
        {
            return x * x;
        }

        /// <summary>
        /// Computes the component-wise square (x * x) of the input argument x.
        /// </summary>
        /// <param name="x">Value to square.</param>
        /// <returns>Returns the square of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double2 square(double2 x)
        {
            return x * x;
        }

        /// <summary>
        /// Computes the component-wise square (x * x) of the input argument x.
        /// </summary>
        /// <param name="x">Value to square.</param>
        /// <returns>Returns the square of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double3 square(double3 x)
        {
            return x * x;
        }

        /// <summary>
        /// Computes the component-wise square (x * x) of the input argument x.
        /// </summary>
        /// <param name="x">Value to square.</param>
        /// <returns>Returns the square of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double4 square(double4 x)
        {
            return x * x;
        }

        /// <summary>
        /// Computes the square (x * x) of the input argument x.
        /// </summary>
        /// <remarks>
        /// Due to integer overflow, it's not always guaranteed that <c>square(x)</c> is positive. For example, <c>square(46341)</c>
        /// will return <c>-2147479015</c>.
        /// </remarks>
        /// <param name="x">Value to square.</param>
        /// <returns>Returns the square of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int square(int x)
        {
            return x * x;
        }

        /// <summary>
        /// Computes the component-wise square (x * x) of the input argument x.
        /// </summary>
        /// <remarks>
        /// Due to integer overflow, it's not always guaranteed that <c>square(x)</c> is positive. For example, <c>square(new int2(46341))</c>
        /// will return <c>new int2(-2147479015)</c>.
        /// </remarks>
        /// <param name="x">Value to square.</param>
        /// <returns>Returns the square of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int2 square(int2 x)
        {
            return x * x;
        }

        /// <summary>
        /// Computes the component-wise square (x * x) of the input argument x.
        /// </summary>
        /// <remarks>
        /// Due to integer overflow, it's not always guaranteed that <c>square(x)</c> is positive. For example, <c>square(new int3(46341))</c>
        /// will return <c>new int3(-2147479015)</c>.
        /// </remarks>
        /// <param name="x">Value to square.</param>
        /// <returns>Returns the square of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int3 square(int3 x)
        {
            return x * x;
        }

        /// <summary>
        /// Computes the component-wise square (x * x) of the input argument x.
        /// </summary>
        /// <remarks>
        /// Due to integer overflow, it's not always guaranteed that <c>square(x)</c> is positive. For example, <c>square(new int4(46341))</c>
        /// will return <c>new int4(-2147479015)</c>.
        /// </remarks>
        /// <param name="x">Value to square.</param>
        /// <returns>Returns the square of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int4 square(int4 x)
        {
            return x * x;
        }

        /// <summary>
        /// Computes the square (x * x) of the input argument x.
        /// </summary>
        /// <remarks>
        /// Due to integer overflow, it's not always guaranteed that <c>square(x) &gt;= x</c>. For example, <c>square(4294967295u)</c>
        /// will return <c>1u</c>.
        /// </remarks>
        /// <param name="x">Value to square.</param>
        /// <returns>Returns the square of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint square(uint x)
        {
            return x * x;
        }

        /// <summary>
        /// Computes the component-wise square (x * x) of the input argument x.
        /// </summary>
        /// <remarks>
        /// Due to integer overflow, it's not always guaranteed that <c>square(x) &gt;= x</c>. For example, <c>square(new uint2(4294967295u))</c>
        /// will return <c>new uint2(1u)</c>.
        /// </remarks>
        /// <param name="x">Value to square.</param>
        /// <returns>Returns the square of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint2 square(uint2 x)
        {
            return x * x;
        }

        /// <summary>
        /// Computes the component-wise square (x * x) of the input argument x.
        /// </summary>
        /// <remarks>
        /// Due to integer overflow, it's not always guaranteed that <c>square(x) &gt;= x</c>. For example, <c>square(new uint3(4294967295u))</c>
        /// will return <c>new uint3(1u)</c>.
        /// </remarks>
        /// <param name="x">Value to square.</param>
        /// <returns>Returns the square of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint3 square(uint3 x)
        {
            return x * x;
        }

        /// <summary>
        /// Computes the component-wise square (x * x) of the input argument x.
        /// </summary>
        /// <remarks>
        /// Due to integer overflow, it's not always guaranteed that <c>square(x) &gt;= x</c>. For example, <c>square(new uint4(4294967295u))</c>
        /// will return <c>new uint4(1u)</c>.
        /// </remarks>
        /// <param name="x">Value to square.</param>
        /// <returns>Returns the square of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint4 square(uint4 x)
        {
            return x * x;
        }


    }
}
