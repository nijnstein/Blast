using System;
using System.Collections.Generic;
using System.Text;

namespace NSS.Blast
{
    public static class math_extensions
    {
        static public int RoundUpToPowerOf2(this int n, int roundTo)
        {
            return (n + roundTo - 1) & -roundTo;
        }
        static public byte RoundUpToPowerOf2(this byte n, byte roundTo)
        {
            return (byte)((byte)(n + roundTo - 1) & (byte)-roundTo);
        }
    }
}
