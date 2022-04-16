//############################################################################################################################
// BLAST v1.0.4c                                                                                                             #
// Copyright © 2022 Rob Lemmens | NijnStein Software <rob.lemmens.s31 gmail com> All Rights Reserved                   ^__^\ #
// Unauthorized copying of this file, via any medium is strictly prohibited proprietary and confidential               (oo)\ #
//                                                                                                                     (__)  #
//############################################################################################################################

using System;
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
    static public class StringBuilderCache
    {
        // The value 360 was chosen in discussion with performance experts as a compromise between using
        // as litle memory (per thread) as possible and still covering a large part of short-lived
        // StringBuilder creations on the startup path of VS designers.
        //
        // for blast i updated it to a larger size to fit larger console messages 
        private const int MAX_BUILDER_SIZE = 768;

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
            if (sb.Capacity <= MAX_BUILDER_SIZE * 4) // allow the cached ref to grow but not by too much
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
