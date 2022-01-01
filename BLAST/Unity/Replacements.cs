#if !UNITY

using System;
using System.Collections.Generic;
using System.Text;

namespace Unity.Collections
{
    public enum Allocator
    {
        Invalid = 0,
        None = 1,
        Temp = 2,
        TempJob = 3,
        Persistent = 4,
        AudioKernel = 5
    }
    /*
   public struct NativeArray<T> : IDisposable, IEnumerable<T>, IEnumerable, IEquatable<NativeArray<T>> where T : struct
   {
       public NativeArray(T[] array, Allocator allocator);
       public NativeArray(NativeArray<T> array, Allocator allocator);
       public NativeArray(int length, Allocator allocator, NativeArrayOptions options = NativeArrayOptions.ClearMemory);

       public T this[int index] { get; set; }

       public bool IsCreated { get; }
       public int Length { get; }

       public static void Copy(NativeArray<T> src, int srcIndex, T[] dst, int dstIndex, int length);
       public static void Copy(T[] src, int srcIndex, NativeArray<T> dst, int dstIndex, int length);
       public static void Copy(NativeArray<T> src, int srcIndex, NativeArray<T> dst, int dstIndex, int length);
       public static void Copy(NativeArray<T> src, T[] dst, int length);
       public static void Copy(T[] src, NativeArray<T> dst, int length);
       public static void Copy(NativeArray<T> src, NativeArray<T> dst, int length);
       public static void Copy(NativeArray<T> src, T[] dst);
       public static void Copy(NativeArray<T> src, NativeArray<T> dst);
       public static void Copy(T[] src, NativeArray<T> dst);
       public ReadOnly AsReadOnly();
       [WriteAccessRequired]
       public void CopyFrom(NativeArray<T> array);
       [WriteAccessRequired]
       public void CopyFrom(T[] array);
       public void CopyTo(T[] array);
       public void CopyTo(NativeArray<T> array);
       public JobHandle Dispose(JobHandle inputDeps);
       [WriteAccessRequired]
       public void Dispose();
       public bool Equals(NativeArray<T> other);
       public override bool Equals(object obj);
       public Enumerator GetEnumerator();
       public override int GetHashCode();
       public NativeArray<T> GetSubArray(int start, int length);
       public NativeArray<U> Reinterpret<U>() where U : struct;
       public NativeArray<U> Reinterpret<U>(int expectedTypeSize) where U : struct;
       public U ReinterpretLoad<U>(int sourceIndex) where U : struct;
       public void ReinterpretStore<U>(int destIndex, U data) where U : struct;
       public T[] ToArray();

       public static bool operator ==(NativeArray<T> left, NativeArray<T> right);
       public static bool operator !=(NativeArray<T> left, NativeArray<T> right);

       [ExcludeFromDocs]
       public struct Enumerator : IEnumerator<T>, IEnumerator, IDisposable
       {
           public Enumerator(ref NativeArray<T> array);

           public T Current { get; }

           public void Dispose();
           public bool MoveNext();
           public void Reset();
       }
       //
       // Summary:
       //     NativeArray interface constrained to read-only operation.
       [DefaultMember("Item")]
       [NativeContainer]
       [NativeContainerIsReadOnly]
       public struct ReadOnly
       {
           public T this[int index] { get; }
       }
   }
      */

}

namespace Unity.Collections.LowLevel.Unsafe
{ 
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class NativeDisableUnsafePtrRestrictionAttribute : Attribute
    {
        public NativeDisableUnsafePtrRestrictionAttribute() { }
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


namespace UnityEngine
{
    public static class Time
    {
        public static float time = 0;
        public static float deltaTime = 0;
        public static float fixedDeltaTime = 0;
        public static float frameCount = 0;
    }

    public static class Debug
    {
        public static void LogError(string msg)
        {
            System.Diagnostics.Debug.Write($"ERROR: {msg}");
        }
        public static void Log(string msg)
        {
            System.Diagnostics.Debug.Write($"LOG: {msg}");
        }
        public static void LogWarning(string msg)
        {
            System.Diagnostics.Debug.Write($"WARNING: {msg}");
        }
    }
}

namespace Unity.Assertions
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


#endif