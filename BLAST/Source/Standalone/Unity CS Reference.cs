//##########################################################################################################
// Copyright © 2022 Rob Lemmens | NijnStein Software <rob.lemmens.s31@gmail.com> All Rights Reserved  ^__^\#
// Unauthorized copying of this file, via any medium is strictly prohibited                           (oo)\#
// Proprietary and confidential                                                                       (__) #
//##########################################################################################################
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
#if STANDALONE_VSBUILD

using System;

namespace UnityEngine.Assertions
{
    public static class Assert
    {
        public static void IsTrue(bool condition, string msg = null)
        {
            if (!condition) throw new Exception(msg ?? "assertion failed");
        }
        public static void IsFalse(bool condition, string msg = null)
        {
            if (condition) throw new Exception(msg ?? "assertion failed");
        }
        public static void IsNull(object condition, string msg = null)
        {
            if (null != condition) throw new Exception(msg ?? "assertion failed");
        }
        public static void IsNotNull(object condition, string msg = null)
        {
            if (null == condition) throw new Exception(msg ?? "assertion failed");
        }
    }
}

namespace Unity.Collections
{
    /*    public enum Allocator
        {
            Invalid = 0,
            None = 1,
            Temp = 2,
            TempJob = 3,
            Persistent = 4,
            AudioKernel = 5
        }*/

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Method | AttributeTargets.Property, AllowMultiple = true)]
    public class BurstCompatible : Attribute { }

    public enum FloatMode
    {
        Default = 0,
        Strict = 1,
        Deterministic = 2,
        Fast = 3
    }
    public enum FloatPrecision
    {
        Standard = 0,
        High = 1,
        Medium = 2,
        Low = 3
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Method)]
    public class BurstCompileAttribute : Attribute
    {
        public BurstCompileAttribute() { }
        public BurstCompileAttribute(FloatPrecision floatPrecision, FloatMode floatMode) { }
        public FloatMode FloatMode { get; set; }
        public FloatPrecision FloatPrecision { get; set; }
        public bool CompileSynchronously { get; set; }
        public bool Debug { get; set; }
        public bool DisableSafetyChecks { get; set; }
    }

}


namespace Unity.Burst
{
    public readonly struct FunctionPointer<T>
    {
        public FunctionPointer(IntPtr ptr) { Value = ptr; Invoke = default; IsCreated = false; }
        public IntPtr Value { get; }
        public T Invoke { get; }
        public bool IsCreated { get; }
    }


    [AttributeUsage(AttributeTargets.Struct | AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.ReturnValue)]
    public class NoAliasAttribute : Attribute
    {
        public NoAliasAttribute() { }
    }

    namespace Intrinsics
    {
        public struct v128
        {
            public float Float0;
            public float Float1;
            public float Float2;
            public float Float3;
        }

        namespace X86
        {
            public static class Sse
            {
                public static v128 mul_ps(v128 a, v128 b)
                {
                    a.Float0 *= b.Float0;
                    a.Float1 *= b.Float1;
                    a.Float2 *= b.Float2;
                    a.Float3 *= b.Float3;
                    return a;
                }
            }
        }
    }

}


#endif


