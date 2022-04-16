//############################################################################################################################
// BLAST v1.0.4c                                                                                                             #
// Copyright © 2022 Rob Lemmens | NijnStein Software <rob.lemmens.s31 gmail com> All Rights Reserved                   ^__^\ #
// Unauthorized copying of this file, via any medium is strictly prohibited proprietary and confidential               (oo)\ #
//                                                                                                                     (__)  #
//############################################################################################################################

using System;

#if STANDALONE_VSBUILD

namespace NSS.Blast.Standalone
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member 'Debug'
    public struct Debug
    {
        public static void Log(string message)
        {
            System.Diagnostics.Debug.WriteLine(message); 
        }
        public static void LogWarning(string warning)
        {
            System.Diagnostics.Debug.WriteLine("WARNING: " + warning); 
        }
        public static void LogError(string error)
        {
            System.Diagnostics.Debug.WriteLine("ERROR: " + error);
            throw new Exception(error); 
        }
    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member 'Debug.LogError(string)'
}
#endif
