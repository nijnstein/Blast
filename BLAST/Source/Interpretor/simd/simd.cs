//############################################################################################################################
// BLAST v1.0.4c                                                                                                             #
// Copyright © 2022 Rob Lemmens | NijnStein Software <rob.lemmens.s31 gmail com> All Rights Reserved                   ^__^\ #
// Unauthorized copying of this file, via any medium is strictly prohibited proprietary and confidential               (oo)\ #
//                                                                                                                     (__)  #
//############################################################################################################################

#pragma warning disable CS0162   // disable warnings for paths not taken dueue to compiler defines 

#if STANDALONE_VSBUILD
#define UNROLL
#else
// even though not always we get packed instructions, overal its much faster. see sample 12
#define UNROLL
#endif

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#if STANDALONE_VSBUILD


#else
using static Unity.Burst.Intrinsics.X86;
using static Unity.Burst.Intrinsics.X86.Sse;
using static Unity.Burst.Intrinsics.X86.Sse2;
using static Unity.Burst.Intrinsics.X86.Sse3;
using static Unity.Burst.Intrinsics.X86.Ssse3;
using static Unity.Burst.Intrinsics.X86.Sse4_1;
using static Unity.Burst.Intrinsics.X86.Sse4_2;
using static Unity.Burst.Intrinsics.X86.Popcnt;
using static Unity.Burst.Intrinsics.X86.Avx;
using static Unity.Burst.Intrinsics.X86.Avx2;
using static Unity.Burst.Intrinsics.X86.Fma;
using static Unity.Burst.Intrinsics.X86.F16C;
using static Unity.Burst.Intrinsics.Arm.Neon;

using Unity.Burst.Intrinsics;
using Unity.Burst;
#endif

namespace NSS.Blast.SSMD
{

#if STANDALONE_VSBUILD

    public interface IJob {
        void Execute(); 
    }

#endif

#if !STANDALONE_VSBUILD
    [BurstCompile]
#endif 
    unsafe public struct DATAREC
    {
        public void** data;
        public int row_size;
        public int index;

        public bool is_aligned;
        public bool is_constant;

        public byte vectorsize;
        public BlastVariableDataType datatype;
        public bool is_set => data != null;
        public float* index00 => &((float**)data)[0][index]; 
    }


#if !STANDALONE_VSBUILD
    [BurstCompile]
#endif 
    unsafe public partial struct simd
    {
#if UNROLL
        public const bool IsUnrolled = false;
#else
        public const bool IsUnrolled = false;
#endif

#if STANDALONE_VSBUILD 
        public const bool IsStandalone = true;
        public const bool IsUnity = false;
#else
        public const bool IsStandalone = false;
        public const bool IsUnity = true;
#endif

#if ENABLE_MONO 
        public const bool IsMono = true;
#else
        public const bool IsMono = false;
#endif

#if ENABLE_IL2CPP
        public const bool IsIL2CPP = true;
#else
        public const bool IsIL2CPP = false;
#endif

#if DEVELOPMENT_BUILD || DEBUG
        public const bool IsDebugBuild = true;
#else
        public const bool IsDebugBuild = false;
#endif


#if STANDALONE_VSBUILD
        public static bool DOTNET = true;

        public static bool AVX   = false;
        public static bool AVX2  = false;
        public static bool SSE42 = false;
        public static bool SSE41 = false;
        public static bool SSE2  = false;
        public static bool SSE3  = false;
        public static bool NEON  = false;
        public static bool FMA   = false;
        public static bool F16C  = false;
#else
        public static bool DOTNET = false;

        public static bool AVX = IsAvxSupported;
        public static bool AVX2 = IsAvx2Supported;
        public static bool SSE42 = IsSse42Supported;
        public static bool SSE41 = IsSse42Supported;
        public static bool SSE2 = IsSse2Supported;
        public static bool SSE3 = IsSse3Supported;
        public static bool NEON = IsNeonSupported;
        public static bool FMA = IsFmaSupported;
        public static bool F16C = IsF16CSupported;
#endif




    }     



}
