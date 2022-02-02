//##########################################################################################################
// Copyright © 2022 Rob Lemmens | NijnStein Software <rob.lemmens.s31@gmail.com> All Rights Reserved       #
// Unauthorized copying of this file, via any medium is strictly prohibited                                #
// Proprietary and confidential                                                                            #
//##########################################################################################################
#if STANDALONE_VSBUILD
using System;
using Unity.Collections;
using System.Runtime.InteropServices;

namespace NSS.Blast.Standalone
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member 'Debug'
    public struct Debug
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member 'Debug'
    {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member 'Debug.Log(string)'
        public static void Log(string message)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member 'Debug.Log(string)'
        {
            System.Diagnostics.Debug.WriteLine(message); 
        }
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member 'Debug.LogWarning(string)'
        public static void LogWarning(string warning)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member 'Debug.LogWarning(string)'
        {
            System.Diagnostics.Debug.WriteLine("WARNING: " + warning); 
        }
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member 'Debug.LogError(string)'
        public static void LogError(string error)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member 'Debug.LogError(string)'
        {
            System.Diagnostics.Debug.WriteLine("ERROR: " + error);
            throw new Exception(error); 
        }
    }
}
#endif
