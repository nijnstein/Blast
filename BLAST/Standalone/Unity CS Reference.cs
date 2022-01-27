#if STANDALONE_VSBUILD

using System;

namespace Unity.Assertions
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member 'Assert'
    public static class Assert
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member 'Assert'
    {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member 'Assert.IsTrue(bool, string)'
        public static void IsTrue(bool condition, string msg = null)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member 'Assert.IsTrue(bool, string)'
        {
            if (!condition) throw new Exception(msg ?? "assertion failed");
        }
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member 'Assert.IsFalse(bool, string)'
        public static void IsFalse(bool condition, string msg = null)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member 'Assert.IsFalse(bool, string)'
        {
            if (condition) throw new Exception(msg ?? "assertion failed");
        }
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member 'Assert.IsNull(object, string)'
        public static void IsNull(object condition, string msg = null)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member 'Assert.IsNull(object, string)'
        {
            if (null != condition) throw new Exception(msg ?? "assertion failed");
        }
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member 'Assert.IsNotNull(object, string)'
        public static void IsNotNull(object condition, string msg = null)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member 'Assert.IsNotNull(object, string)'
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
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member 'BurstCompatible'
    public class BurstCompatible : Attribute { }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member 'BurstCompatible'

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member 'FloatMode'
    public enum FloatMode
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member 'FloatMode'
    {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member 'FloatMode.Default'
        Default = 0,
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member 'FloatMode.Default'
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member 'FloatMode.Strict'
        Strict = 1,
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member 'FloatMode.Strict'
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member 'FloatMode.Deterministic'
        Deterministic = 2,
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member 'FloatMode.Deterministic'
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member 'FloatMode.Fast'
        Fast = 3
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member 'FloatMode.Fast'
    }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member 'FloatPrecision'
    public enum FloatPrecision
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member 'FloatPrecision'
    {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member 'FloatPrecision.Standard'
        Standard = 0,
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member 'FloatPrecision.Standard'
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member 'FloatPrecision.High'
        High = 1,
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member 'FloatPrecision.High'
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member 'FloatPrecision.Medium'
        Medium = 2,
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member 'FloatPrecision.Medium'
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member 'FloatPrecision.Low'
        Low = 3
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member 'FloatPrecision.Low'
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Method)]
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member 'BurstCompileAttribute'
    public class BurstCompileAttribute : Attribute
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member 'BurstCompileAttribute'
    {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member 'BurstCompileAttribute.BurstCompileAttribute()'
        public BurstCompileAttribute() { }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member 'BurstCompileAttribute.BurstCompileAttribute()'
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member 'BurstCompileAttribute.BurstCompileAttribute(FloatPrecision, FloatMode)'
        public BurstCompileAttribute(FloatPrecision floatPrecision, FloatMode floatMode) { }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member 'BurstCompileAttribute.BurstCompileAttribute(FloatPrecision, FloatMode)'
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member 'BurstCompileAttribute.FloatMode'
        public FloatMode FloatMode { get; set; }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member 'BurstCompileAttribute.FloatMode'
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member 'BurstCompileAttribute.FloatPrecision'
        public FloatPrecision FloatPrecision { get; set; }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member 'BurstCompileAttribute.FloatPrecision'
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member 'BurstCompileAttribute.CompileSynchronously'
        public bool CompileSynchronously { get; set; }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member 'BurstCompileAttribute.CompileSynchronously'
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member 'BurstCompileAttribute.Debug'
        public bool Debug { get; set; }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member 'BurstCompileAttribute.Debug'
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member 'BurstCompileAttribute.DisableSafetyChecks'
        public bool DisableSafetyChecks { get; set; }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member 'BurstCompileAttribute.DisableSafetyChecks'
    }

}


namespace Unity.Burst
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member 'FunctionPointer<T>'
    public readonly struct FunctionPointer<T>
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member 'FunctionPointer<T>'
    {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member 'FunctionPointer<T>.FunctionPointer(IntPtr)'
        public FunctionPointer(IntPtr ptr) { Value = ptr; Invoke = default; IsCreated = false; }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member 'FunctionPointer<T>.FunctionPointer(IntPtr)'
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member 'FunctionPointer<T>.Value'
        public IntPtr Value { get; }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member 'FunctionPointer<T>.Value'
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member 'FunctionPointer<T>.Invoke'
        public T Invoke { get; }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member 'FunctionPointer<T>.Invoke'
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member 'FunctionPointer<T>.IsCreated'
        public bool IsCreated { get; }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member 'FunctionPointer<T>.IsCreated'
    }


    [AttributeUsage(AttributeTargets.Struct | AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.ReturnValue)]
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member 'NoAliasAttribute'
    public class NoAliasAttribute : Attribute
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member 'NoAliasAttribute'
    {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member 'NoAliasAttribute.NoAliasAttribute()'
        public NoAliasAttribute() { }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member 'NoAliasAttribute.NoAliasAttribute()'
    }

    namespace Intrinsics
    {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member 'v128'
        public struct v128
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member 'v128'
        {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member 'v128.Float0'
            public float Float0;
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member 'v128.Float0'
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member 'v128.Float1'
            public float Float1;
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member 'v128.Float1'
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member 'v128.Float2'
            public float Float2;
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member 'v128.Float2'
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member 'v128.Float3'
            public float Float3;
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member 'v128.Float3'
        }

        namespace X86
        {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member 'Sse'
            public static class Sse
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member 'Sse'
            {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member 'Sse.mul_ps(v128, v128)'
                public static v128 mul_ps(v128 a, v128 b)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member 'Sse.mul_ps(v128, v128)'
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


