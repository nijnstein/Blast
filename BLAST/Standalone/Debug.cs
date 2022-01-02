
#if NOT_USING_UNITY
using System;
using Unity.Collections;
using System.Runtime.InteropServices;

namespace NSS.Blast.Standalone
{
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
}
#endif
