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
    unsafe public partial struct simd
    {
#if UNROLL
        public const bool IsUnrolled = true;
#else
        public const bool IsUnrolled = false;
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
