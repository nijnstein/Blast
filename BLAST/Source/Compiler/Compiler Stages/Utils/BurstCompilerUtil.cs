using System;
using System.Globalization;
using System.Reflection;
using System.Text;
using Unity.Burst;
using Unity.Collections.LowLevel.Unsafe;

namespace NSS.Blast
{

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member 'CodeUtils'
    public static class CodeUtils
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member 'CodeUtils'
    {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member 'CodeUtils.AsFloat(string)'
        public static float AsFloat(this string s)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member 'CodeUtils.AsFloat(string)'
        {
            float f = float.NaN;

            if (string.IsNullOrWhiteSpace(s))
            {
                return f;
            }

            if (CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator == ",")
            {
                /*if (s.Contains('.')) */
                s = s.Replace('.', ',');
            }
            else
            {
                s = s.Replace(',', '.');
            }


            //if(
            float.TryParse(s, out f);//)
            {
                return f;
            }
        }

        /// <summary>
        /// return bytes formatted as 000| 000 000 000 000 000 000 000 000 
        /// </summary>
        /// <param name="code"></param>
        /// <param name="length"></param>
        /// <param name="column_count"></param>
        /// <param name="use_index"></param>
        /// <returns></returns>
        unsafe public static string GetBytesView(byte* code, int length, int column_count = 8, bool use_index = false)
        {
            if (code == null) return string.Empty; 

            StringBuilder sb = StringBuilderCache.Acquire();

            for (int i = 0; i < length; i++)
            {
                if (i % column_count == 0)
                {
                    sb.Append($"{i.ToString().PadLeft(3, '0')}| ");
                }
                sb.Append($"{code[i].ToString().PadLeft(3, '0')} ");
                if (i % column_count == column_count - 1)
                {
                    sb.AppendLine();
                }
            }

            return StringBuilderCache.GetStringAndRelease(ref sb);
        }

        [BurstDiscard]
        internal static unsafe void FillCharArray(char* pch, string text, int n_max)
        {
            char[] ch = text.ToCharArray();

            for (int i = 0; i < ch.Length; i++)
            {
                pch[i] = ch[i];
            }
            for (int i = ch.Length; i < n_max; i++)
            {
                pch[i] = (char)0;
            }
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

#if !STANDALONE_VSBUILD
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