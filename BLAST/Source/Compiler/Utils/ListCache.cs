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
using System.Text;
using System.Threading.Tasks;

namespace NSS.Blast
{
    /// <summary>
    ///  a static list cache, caches only a single list of a preset small capacity
    /// </summary>
    public class ListCache<T> where T : class
    {
        const int CACHED_CAPACITY = 64;
        [ThreadStatic]
        private static List<T> CachedInstance;

        /// <summary>
        /// 
        /// </summary>
        public static List<T> Acquire(int capacity = CACHED_CAPACITY)
        {
            if (capacity <= CACHED_CAPACITY)
            {
                List<T> s = CachedInstance;
                if (s != null)
                {
                    if (capacity <= s.Capacity)
                    {
                        CachedInstance = null;
                        s.Clear();
                        return s;
                    }
                }
            }
            return new List<T>(capacity);
        }

        /// <summary>
        /// 
        /// </summary>
        public static void Release(List<T> s)
        {
            if (s.Capacity <= CACHED_CAPACITY)
            {
                CachedInstance = s;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public static T[] ReleaseArray(List<T> s)
        {
            if (s.Capacity <= CACHED_CAPACITY)
            {
                CachedInstance = s;
            }
            return s.ToArray();
        }
    }
}
