﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NSS.Blast
{
    /// <summary>
    /// copied with minor changes from ms cli reference source  
    /// https://referencesource.microsoft.com/#mscorlib/system/text/stringbuildercache.cs,40
    /// </summary>
    static class StringBuilderCache
    {
        // The value 360 was chosen in discussion with performance experts as a compromise between using
        // as litle memory (per thread) as possible and still covering a large part of short-lived
        // StringBuilder creations on the startup path of VS designers.
        private const int MAX_BUILDER_SIZE = 360;

        [ThreadStatic]
        private static StringBuilder CachedInstance;

        public static StringBuilder Acquire(int capacity = MAX_BUILDER_SIZE)
        {
            if (capacity <= MAX_BUILDER_SIZE)
            {
                StringBuilder sb = StringBuilderCache.CachedInstance;
                if (sb != null)
                {
                    // Avoid stringbuilder block fragmentation by getting a new StringBuilder
                    // when the requested size is larger than the current capacity
                    if (capacity <= sb.Capacity)
                    {
                        StringBuilderCache.CachedInstance = null;
                        sb.Clear();
                        return sb;
                    }
                }
            }
            return new StringBuilder(capacity);
        }

        public static void Release(StringBuilder sb)
        {
            if (sb.Capacity <= MAX_BUILDER_SIZE)
            {
                StringBuilderCache.CachedInstance = sb;
            }
        }

        public static string GetStringAndRelease(ref StringBuilder sb)
        {
            string result = sb.ToString();
            Release(sb);
            sb = null; 
            return result;
        }
    }
}