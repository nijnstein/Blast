using System;
using System.Reflection;
using Unity.Burst;
using Unity.Collections.LowLevel.Unsafe;

namespace NSS.Blast
{
    public struct NonGenericFunctionPointer
    {
        public readonly int id;
        public readonly int parameter_count; 

        [NativeDisableUnsafePtrRestriction]
        private readonly IntPtr ptr;

        public NonGenericFunctionPointer(IntPtr ptr, int id, int parameter_count)
        {
            this.ptr = ptr;
            this.id = id;
            this.parameter_count = parameter_count; 
        }

        public FunctionPointer<T> Generic<T>()
        {
            return new FunctionPointer<T>(ptr);
        }
    }

    /// <summary>
    /// 
    /// cast 1 refernce to another
    /// 
    /// Quaternion q;
    /// ref float4 f = ref CastUtil.RefToRef(q);
    /// f.w = 123;
    /// print(q.w); // 123
    /// </summary>
    static class CastUtil
    {
        public static unsafe ref TDest RefToRef<TSrc, TDest>(in TSrc src)
            where TSrc : unmanaged
            where TDest : unmanaged
        {
            fixed (TSrc* pSrc = &src)
            {
                TDest* dest = (TDest*)pSrc;
                return ref *dest;
            }
        }
    }

#if !NOT_USING_UNITY
    static class BurstCompilerUtil<T>
        where T : class
    {
        private static readonly MethodInfo compileMethodInfo;

        static BurstCompilerUtil()
        {
            foreach (var mi in typeof(BurstCompiler).GetMethods(
                BindingFlags.Default
                | BindingFlags.Static
                | BindingFlags.NonPublic))
            {
                if (mi.Name == "Compile")
                {
                    compileMethodInfo = mi.MakeGenericMethod(typeof(T));
                    break;
                }
            }
        }

        public static unsafe NonGenericFunctionPointer CompileFunctionPointer(T del, int id, int parameter_count)
        {
            var obj = compileMethodInfo.Invoke(null, new object[] { del, true });
            var ptr = Pointer.Unbox(obj);
            var intPtr = new IntPtr(ptr);

            return new NonGenericFunctionPointer(intPtr, id, parameter_count);
        }
    }

#endif
}