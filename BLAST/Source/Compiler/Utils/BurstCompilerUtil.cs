//##########################################################################################################
// Copyright © 2022 Rob Lemmens | NijnStein Software <rob.lemmens.s31@gmail.com> All Rights Reserved  ^__^\#
// Unauthorized copying of this file, via any medium is strictly prohibited                           (oo)\#
// Proprietary and confidential                                                                       (__) #
//##########################################################################################################
using System;
using System.Globalization;
using System.Reflection;
using System.Text;
using Unity.Burst;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

namespace NSS.Blast
{

    public static class CodeUtils
    {
        public static float AsFloat(this string s)
        {
            float f = float.NaN;

            if (string.IsNullOrWhiteSpace(s))
            {
                return f;
            }

            if (CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator == ",")
            {
                s = s.Replace('.', ',');
            }
            else
            {
                s = s.Replace(',', '.');
            }


            float.TryParse(s, out f);//)
            {
                return f;
            }
        }


        /// <summary>
        /// accept either an unsigned integer or a string of 32 0 and 1's
        /// </summary>
        public static bool TryAsBool32(this string s, bool allow_uint32, out uint b32)
        {
            if (string.IsNullOrWhiteSpace(s)) {
                b32 = 0; 
                return false;
            }

            if(s.Length < 32)
            {
                // has to be an int 
                if(allow_uint32 && uint.TryParse(s, out b32))
                {
                    return true; 
                }
                b32 = 0;
                return false;
            }

            b32 = 0;
            byte i = 0;
            foreach (char ch in s)
            {
                if (ch == '1')
                {
                    b32 = (uint)(b32 | (1 << i));
                    i++;
                    continue; 
                }
                
                if(ch == '0')
                {
                    i++;
                    continue; 
                }
            }

           // b32 = math.reversebits(b32);

            return i == 32;
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
        public static unsafe void FillCharArray(char* pch, string text, int n_max)
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

        [BurstDiscard]
        public static string FormatBool32(uint b32, bool reverse = false)
        {
            if (reverse)
            {
                b32 = math.reversebits(b32);
            }

            // in vs give a nicer output in debug 
            System.Text.StringBuilder sb = StringBuilderCache.Acquire();

            int ii = 0;
            for (int x = 0; x < 4; x++)
            {
                for (int y = 0; y < 8; y++)
                {
                    int mask = 1 << ii;
                    sb.Append((b32 & mask) == mask ? "1" : "0");
                    ii++;
                }
                if (x < 3) sb.Append("_");
            }

            return StringBuilderCache.GetStringAndRelease(ref sb); 
        }


        /// <summary>
        /// return true if item2 contains only: 
        /// - digits 0-9
        /// - . , -
        /// </summary>
        [BurstDiscard]
        public static bool OnlyNumericChars(string item2)
        {
            if (string.IsNullOrWhiteSpace(item2)) return false; 
            foreach(char ch in item2)
            {
                switch(ch)
                {
                    case '0':
                    case '1':
                    case '2':
                    case '3':
                    case '4':
                    case '5':
                    case '6':
                    case '7':
                    case '8':
                    case '9':
                    case '-':
                    case '.':
                    case ',': continue;

                    default: return false; 
                }
            }
            return true; 
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
    
        public static unsafe IntPtr CompileFunctionPointer(T del)
        {
            var obj = compileMethodInfo.Invoke(null, new object[] { del, true });
            return new IntPtr(Pointer.Unbox(obj)); 
        }
    }

#endif
}