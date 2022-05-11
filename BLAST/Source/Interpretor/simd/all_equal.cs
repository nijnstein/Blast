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
#pragma warning disable CS0164


#if STANDALONE_VSBUILD

#else
    using Unity.Jobs;
    using static Unity.Burst.Intrinsics.X86.Avx2;
    using static Unity.Burst.Intrinsics.X86.Avx;
    using static Unity.Burst.Intrinsics.X86.Ssse3;
    using static Unity.Burst.Intrinsics.X86.Sse2;
    using static Unity.Burst.Intrinsics.X86.Sse;
using static Unity.Burst.Intrinsics.X86.Popcnt;
using Unity.Burst.Intrinsics;
#endif

using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Mathematics;

namespace NSS.Blast.SSMD
{
    unsafe public partial struct simd
    {


        public static bool AllEqual(float* p, int datacount)
        {
            int i = 0;
            float f = p[0];
            bool equal = true;

            unchecked
            {
                if (IsAvx2Supported)
                {
                    v256* p256 = (v256*)(void*)p;

                    // fill v256 with all the first value 
                    v256 pop_0th = mm256_set1_ps(f);

                    // compare 64 floats as 1111 or 0000 to 256 bits of 1
                    v256 ones = mm256_setzero_si256();
                    ones = mm256_cmpeq_epi32(ones, ones);

                    while ((i < (datacount - 64)) & equal)
                    {
                        // shuffle 1111 into each nibble for each match
                        v256 a = new v256(
                            mm256_movemask_epi8(mm256_cmpeq_epi32(p256[0], pop_0th)),
                            mm256_movemask_epi8(mm256_cmpeq_epi32(p256[1], pop_0th)),
                            mm256_movemask_epi8(mm256_cmpeq_epi32(p256[2], pop_0th)),
                            mm256_movemask_epi8(mm256_cmpeq_epi32(p256[3], pop_0th)),
                            mm256_movemask_epi8(mm256_cmpeq_epi32(p256[4], pop_0th)),
                            mm256_movemask_epi8(mm256_cmpeq_epi32(p256[5], pop_0th)),
                            mm256_movemask_epi8(mm256_cmpeq_epi32(p256[6], pop_0th)),
                            mm256_movemask_epi8(mm256_cmpeq_epi32(p256[7], pop_0th)));

                        // equal if 256i == all ones 
                        equal = ((uint)mm256_movemask_epi8(mm256_cmpeq_epi32(a, ones)) == 0xFFFFFFFF);

                        p += 64;
                        i += 64;
                    }
                    while ((i < (datacount & ~7)) & equal)
                    {
                        v256 eq = mm256_cmpeq_epi32(((v256*)(void*)p)[0], pop_0th);
                        equal = (uint)mm256_movemask_epi8(eq) == 0xffffffff;
                        p += 8;
                        i += 8;
                    }
                }
                // fallback for old stuff
                while ((i < datacount) & equal)
                {
                    if (f != p[0]) return false;
                    p++;
                    i++;
                }
            }

            return equal;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool all_equal_x([NoAlias] in float4* f4, in int datacount)
        {
            if (datacount > 1)
            {
                float a = f4[0].x;
                for (int i = 1; i < datacount; i++)
                {
                    if (a != f4[i].x) return false;
                }
            }
            return true;
        }

#if !STANDALONE_VSBUILD




        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool all_equal_float(v256 input)
        {
            if (IsAvx2Supported)
            {
                // fill v256 with all the first value 
                v256 pop_0th = mm256_set1_ps(input.Float0); 

                // compare to input  (does it matter to compare as int? 
                v256 eq = mm256_cmpeq_epi32(input, pop_0th);

                // should all be the same 
                return ((uint)mm256_movemask_epi8(eq) == 0xffffffff);
            }
            return input.Float0 == input.Float1 & input.Float0 == input.Float2 & input.Float0 == input.Float3 & input.Float0 == input.Float4 & input.Float0 == input.Float5 & input.Float0 == input.Float6 & input.Float0 == input.Float7;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool all_equal_byte(v256 input)
        {
            if (IsAvx2Supported)
            {
                v128 lane0 = mm256_castsi256_si128(input); // nop instruction, for typing 
                v128 tmp = shuffle_epi8(lane0, setzero_si128()); 
                v256 populated_0th_byte = mm256_set_m128i(tmp, tmp);
                v256 eq = mm256_cmpeq_epi8(input, populated_0th_byte);

                return ((uint)mm256_movemask_epi8(eq) == 0xffffffff);
            }
            return all_equal_byte(input.Lo128) & all_equal_byte(input.Hi128); 
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool all_equal_byte(v128 input)
        {
            if (IsSsse3Supported & IsSse2Supported)
            {
                //    input = [42 | 42 | 42 | 42 | 42 | 42 | 42 | 42 | 42 | 42 | 42 | 42 | 03 | 42 | 42 | 42]
                //  rotated = [42 | 42 | 42 | 42 | 42 | 42 | 42 | 42 | 42 | 42 | 42 | 03 | 42 | 42 | 42]42]
                //      cmp = (input == rotated)
                //          = [ff | ff | ff | ff | ff | ff | ff | ff | ff | ff | ff | 00 | 00 | ff | ff | ff]
                v128 rotated = alignr_epi8(input, input, 1);
                v128 eq = cmpeq_epi8(input, rotated);
                return ((ushort)movemask_epi8(eq) == 0xffff);
            }

            // fallback 
            return (input.Byte0 == input.Byte1 & input.Byte1 == input.Byte2 & input.Byte1 == input.Byte3) & (input.SInt0 == input.SInt1 && input.SInt0 == input.SInt2 && input.SInt0 == input.SInt3); 
        }

#endif 

    }
}
