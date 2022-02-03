//##########################################################################################################
// Copyright © 2022 Rob Lemmens | NijnStein Software <rob.lemmens.s31@gmail.com> All Rights Reserved  ^__^\#
// Unauthorized copying of this file, via any medium is strictly prohibited                           (oo)\#
// Proprietary and confidential                                                                       (__) #
//##########################################################################################################
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace NSS.Blast
{       
        [StructLayout(LayoutKind.Explicit)]
        internal struct UIntFloatUnion
        {
            [FieldOffset(0)]
            public uint uintValue;
            [FieldOffset(0)]
            public float floatValue;
        }

    [StructLayout(LayoutKind.Explicit)]
    internal struct ULongDoubleUnion
    {
        [FieldOffset(0)]
        public ulong ulongValue;
        [FieldOffset(0)]
        public double doubleValue;
    }
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member 'math_extensions'
    public static class math_extensions
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member 'math_extensions'
    {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member 'math_extensions.RoundUpToPowerOf2(int, int)'
        static public int RoundUpToPowerOf2(this int n, int roundTo)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member 'math_extensions.RoundUpToPowerOf2(int, int)'
        {
            return (n + roundTo - 1) & -roundTo;
        }
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member 'math_extensions.RoundUpToPowerOf2(byte, byte)'
        static public byte RoundUpToPowerOf2(this byte n, byte roundTo)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member 'math_extensions.RoundUpToPowerOf2(byte, byte)'
        {
            return (byte)((byte)(n + roundTo - 1) & (byte)-roundTo);
        }







    }    
}
