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
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Collections.LowLevel.Unsafe;
using System.Runtime.CompilerServices;
using Unity.Burst.Intrinsics;

#if !STANDALONE_VSBUILD
using static Unity.Burst.Intrinsics.X86.Avx2;
using static Unity.Burst.Intrinsics.X86.Avx;
#endif


namespace NSS.Blast
{
    /// <summary>
    /// debugging / standalone interface to UnsafeUtility 
    /// </summary>
#if ENABLE_IL2CPP
    [Unity.IL2CPP.CompilerServices.Il2CppSetOption(Unity.IL2CPP.CompilerServices.Option.NullChecks, false)]
    [Unity.IL2CPP.CompilerServices.Il2CppSetOption(Unity.IL2CPP.CompilerServices.Option.ArrayBoundsChecks, false)]
    [Unity.IL2CPP.CompilerServices.Il2CppEagerStaticClassConstructionAttribute()]
#elif !STANDALONE_VSBUILD

    [Unity.Burst.BurstCompile]
#endif
    unsafe public struct UnsafeUtils
    {                                                                      

        /// <summary>
        /// check if the data arrays are equally spaced
        /// </summary>
        /// <param name="data">a pointer array</param>
        /// <param name="datacount">the number of pointers in that array</param>
        /// <param name="rowsize">if equally spaced, the rowstride in bytes</param>
        /// <returns>true if equally spaced (aligned in ssmd)</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#if !STANDALONE_VSBUILD
        [Unity.Burst.BurstCompile]
#endif
        public static bool CheckIfEquallySpaced(void** data, int datacount, out int rowsize)
        {
            // for performance reasons: only check the first as a long substraction
            rowsize = (int)(((ulong*)data)[1] - ((ulong*)data)[0]);

            // only say indices are aligned when below some max value of spacing
            // by setting max to int32 (-1 bit) we can interpret the lsw of the 64bit 
            // pointer as a signed integer, speeding up the mainloop greatly as we can work
            // on 32bit integers instead of 64 bit uints 
            bool data_is_aligned = (rowsize % 4) == 0 && rowsize >= 0 && rowsize < int.MaxValue;

            // meeting these conditions we can check only the lower int of each long -> way faster 
            int i = 0;
            int* p = (int*)data;

            // if building for unity we might be able to use intrinsics 
#if !STANDALONE_VSBUILD
            if (IsAvx2Supported)
            {
                v256 cmp = mm256_set1_epi32(rowsize);
                v256 perm = mm256_set_epi32(1, 2, 3, 4, 5, 6, 7, 0);

                unchecked
                {
                    while (data_is_aligned & (i < (datacount - 16)))
                    {
                        // load only 1 vector and shift it saving 1 memory gathering operation
                        //v256 v1 = mm256_i32gather_epi32(p, index, 4);
                        v256 v1 = mm256_Stride2LoadEven(p);

                        // permute vector 1 index to left
                        v256 v2 = mm256_permutevar8x32_epi32(v1, perm);

                        i += 16;
                        p += 16;

                        // fill last with value from next row 
                        v2 = mm256_insert_epi32(v2, p[0], 7);

                        data_is_aligned = 0b11111111_11111111_11111111_11111111 == CodeUtils.ReInterpret<int, uint>(
                             mm256_movemask_epi8(
                                 mm256_cmpeq_epi32(
                                     mm256_sub_epi32(v2, v1),
                                     cmp)));
                    }
                }
            }
#endif
            // .net standard likes unrolled pointer walk loops a lot according to my benchmarks 
            // - if built for unity with avx then this will just handle the rest 
            unchecked
            {
                while (data_is_aligned & (i < (datacount - 8)))
                {
                    data_is_aligned =
                        rowsize == (int)(p[2] - p[0])
                        &&
                        rowsize == (int)(p[4] - p[2])
                        &&
                        rowsize == (int)(p[6] - p[4])
                        &&
                        rowsize == (int)(p[8] - p[6]);

                    i += 8;
                    p += 8;
                }
                while (data_is_aligned & (i < (datacount - 2)))
                {
                    data_is_aligned = rowsize == (int)(p[2] - p[0]);
                    i += 2;
                    p += 2;
                }
            }
            return data_is_aligned;
        }


#if !STANDALONE_VSBUILD 

        /// <summary>
        /// load even indices from p into v256
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static v256 mm256_Stride2LoadEven(float* p)
        {
            if (IsAvx2Supported)
            {
                v256 x_lo = mm256_loadu_ps(&p[0]);
                v256 x_hi = mm256_loadu_ps(&p[7]);
                return mm256_permutevar8x32_ps(mm256_blend_ps(x_lo, x_hi, 0b10101010), mm256_set_epi32(7, 5, 3, 1, 6, 4, 2, 0));
            }
            else
            {
                return new v256(p[0], p[2], p[4], p[6], p[8], p[10], p[12], p[14]); 
            }
        }

        /// <summary>
        /// load even indices from p into v256
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static v256 mm256_Stride2LoadEven(int* p)
        {
            if (IsAvx2Supported)
            {
                v256 x_lo = mm256_load_si256(&p[0]);
                v256 x_hi = mm256_load_si256(&p[7]);
                return mm256_permutevar8x32_epi32(mm256_blend_epi32(x_lo, x_hi, 0b10101010), mm256_set_epi32(7, 5, 3, 1, 6, 4, 2, 0));
            }
            else
            {
                return new v256(p[0], p[2], p[4], p[6], p[8], p[10], p[12], p[14]);
            }
        }


        /// <summary>
        /// load odd indices from p into v256
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static v256 mm256_Stride2LoadOdd(float* p)
        {
            if (IsAvx2Supported)
            {
                v256 x_lo = mm256_loadu_ps(&p[1]);
                v256 x_hi = mm256_loadu_ps(&p[8]);
                return mm256_permutevar8x32_ps(mm256_blend_ps(x_lo, x_hi, 0b10101010), mm256_set_epi32(7, 5, 3, 1, 6, 4, 2, 0));
            }
            else
            {
                return new v256(p[1], p[3], p[5], p[7], p[9], p[11], p[13], p[15]);
            }
        }

        /// <summary>
        /// load odd indices from p into v256
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static v256 mm256_Stride2LoadOdd(int* p)
        {
            if (IsAvx2Supported)
            {
                v256 x_lo = mm256_loadu_si256(&p[1]);
                v256 x_hi = mm256_loadu_si256(&p[8]);
                return mm256_permutevar8x32_epi32(mm256_blend_epi32(x_lo, x_hi, 0b10101010), mm256_set_epi32(7, 5, 3, 1, 6, 4, 2, 0));
            }
            else
            {
                return new v256(p[1], p[3], p[5], p[7], p[9], p[11], p[13], p[15]);
            }
        }


        /// <summary>
        /// conversion based on https://gist.github.com/powturbo/2b06a84b6008dfffef11e53edba297d3
        /// </summary>
        /// <returns></returns>
        static long memcount_avx2(byte* s, byte c, long n)
        {
            v256 cv = mm256_set1_epi8(c);
            v256 zv = mm256_setzero_si256();
            v256 sum = zv, acr0, acr1, acr2, acr3;

            byte* p; byte* pe;
            for (p = s; p != (char*)s + (n - (n % (252 * 32)));)
            {
                for (acr0 = acr1 = acr2 = acr3 = zv, pe = p + 252 * 32; p != pe; p += 128)
                {
                    acr0 = mm256_add_epi8(acr0, mm256_cmpeq_epi8(cv, mm256_lddqu_si256((v256*)p)));
                    acr1 = mm256_add_epi8(acr1, mm256_cmpeq_epi8(cv, mm256_lddqu_si256((v256*)(p + 32))));
                    acr2 = mm256_add_epi8(acr2, mm256_cmpeq_epi8(cv, mm256_lddqu_si256((v256*)(p + 64))));
                    acr3 = mm256_add_epi8(acr3, mm256_cmpeq_epi8(cv, mm256_lddqu_si256((v256*)(p + 96))));

                    // __builtin_prefetch(p+1024);
                }
                sum = mm256_add_epi64(sum, mm256_sad_epu8(mm256_sub_epi8(zv, acr0), zv));
                sum = mm256_add_epi64(sum, mm256_sad_epu8(mm256_sub_epi8(zv, acr1), zv));
                sum = mm256_add_epi64(sum, mm256_sad_epu8(mm256_sub_epi8(zv, acr2), zv));
                sum = mm256_add_epi64(sum, mm256_sad_epu8(mm256_sub_epi8(zv, acr3), zv));
            }
            
            for (acr0 = zv; p + 32 < (char*)s + n; p += 32)
            {
                acr0 = mm256_add_epi8(acr0, mm256_cmpeq_epi8(cv, mm256_lddqu_si256((v256*)p)));
            }
            sum = mm256_add_epi64(sum, mm256_sad_epu8(mm256_sub_epi8(zv, acr0), zv));
            long count = mm256_extract_epi64(sum, 0) + mm256_extract_epi64(sum, 1) + mm256_extract_epi64(sum, 2) + mm256_extract_epi64(sum, 3);
            
            while (p != (byte*)s + n)
            {
                count += math.select(0, 1, p[0] == c);
                p++;
            }
            return count;
        }
#endif


#if STANDALONE_VSBUILD
        /// <summary>
        /// allocate native memory 
        /// </summary>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        unsafe public static void* Malloc(long size, int alignment, Allocator allocator)
        {
            return System.Runtime.InteropServices.Marshal.AllocHGlobal((int)size).ToPointer();
        }


        /// <summary>
        /// free native memory 
        /// </summary>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        unsafe public static void Free(void* memory, Allocator allocator)
        {
            System.Runtime.InteropServices.Marshal.FreeHGlobal((IntPtr)memory);
        }

        /// <summary>
        /// cpy memory 
        /// </summary>
        /// <param name="destination"></param>
        /// <param name="source"></param>
        /// <param name="size"></param>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        unsafe public static void MemCpy(void* destination, void* source, long size)
        {
            byte* d = (byte*)destination;
            byte* s = (byte*)source;
            for (int i = 0; i < size; i++)
            {
                d[i] = s[i];
            }
        }

        /// <summary>
        /// copy and replicate memory, copy count * size from source to destination 
        /// </summary>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        unsafe public static void MemCpyReplicate(void* destination, void* source, int size, int count)
        {
            byte* d = (byte*)destination;
            byte* s = (byte*)source;

            int i = 0, k = 0;
            while (i < count)
            {
                int j = 0;
                while (j < size)
                {
                    d[k] = s[j];
                    j++;
                    k++;
                }
                i++;
            }
        }

        /// <summary>
        /// set memory to some byte value 
        /// </summary>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        unsafe public static void MemSet(void* destination, byte value, long size)
        {
            byte* d = (byte*)destination;
            for (int i = 0; i < size; i++)
            {
                d[i] = value;
            }
        }

        /// <summary>
        /// clear memory 
        /// </summary>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        unsafe public static void MemClear(void* destination, long size)
        {
            MemSet(destination, 0, size);
        }

        /// <summary>
        /// get native sizeof a type 
        /// </summary>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static int SizeOf(Type type)
        {
            return Unity.Collections.LowLevel.Unsafe.UnsafeUtility.SizeOf(type);
        }

        /// <summary>
        /// cpy memory with strides  
        /// </summary>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static void MemCpyStride(void* pout, int stride1, void* pin, int stride2, int element_size, int ssmd_datacount)
        {
            byte* d = (byte*)pout;
            byte* s = (byte*)pin;

            int ii = 0;
            int io = 0;

            stride1 = stride1 - element_size; 
            stride2 = stride2 - element_size; 

            for (int i = 0; i < ssmd_datacount; i++)
            {
                for (int j = 0; j < element_size; j++)
                {
                    d[io++] = s[ii++];
                }
                io += stride1;
                ii += stride2;
            }
        }
      


#else
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        unsafe public static void* Malloc(long size, int alignment, Allocator allocator)
        {
            return UnsafeUtility.Malloc(size, alignment, allocator); 
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        unsafe public static void Free(void* memory, Allocator allocator)
        {
            UnsafeUtility.Free(memory, allocator); 
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        unsafe public static void MemCpy(void* destination, void* source, long size)
        {
            UnsafeUtility.MemCpy(destination, source, size);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static void MemCpyStride(void* pout, int stride1, void* pin, int stride2, int size, int ssmd_datacount)
        {
            Unity.Collections.LowLevel.Unsafe.UnsafeUtility.MemCpyStride(pout, stride1, pin, stride2, size, ssmd_datacount); 
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        unsafe public static void MemCpyReplicate(void* destination, void* source, int size, int count)
        {
            UnsafeUtility.MemCpyReplicate(destination, source,size, count); 
        } 

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        unsafe public static void MemSet(void* destination, byte value, long size)
        {
            UnsafeUtility.MemSet(destination, value, size); 
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        unsafe public static void MemClear(void* destination, long size)
        {
            UnsafeUtility.MemClear(destination, size);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static int SizeOf(Type type)
        {
            return UnsafeUtility.SizeOf(type); 
        }
#endif

        /// <summary>
        /// copy with strides and offset into destination
        /// </summary>
        /// <param name="pout"></param>
        /// <param name="stride1"></param>
        /// <param name="offset_into_pout"></param>
        /// <param name="pin"></param>
        /// <param name="stride2"></param>
        /// <param name="element_size"></param>
        /// <param name="ssmd_datacount"></param>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static void MemCpyStride([NoAlias] void* pout, int stride1, int offset_into_pout, [NoAlias] void* pin, int stride2, int element_size, int ssmd_datacount)
        {
            byte* p = (byte*)pout;
            MemCpyStride(&p[offset_into_pout], stride1, pin, stride2, element_size, ssmd_datacount);
        }


        /// <summary>
        /// replicate memory at pin 
        /// - stride is the total rowsize of each target
        /// - elementsize
        /// </summary>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static void MemCpyReplicateStride([NoAlias] void* pout, int stride1, [NoAlias] void* pin, int element_size, int ssmd_datacount)
        {
#if !STANDALONE_VSBUILD
            MemCpyStride(pout, stride1, pin, 0, element_size, ssmd_datacount);
#else
            byte* d = (byte*)pout;
            byte* s = (byte*)pin;
            
            int io = 0;
            stride1 = stride1 - element_size;  
            
            for (int i = 0; i < ssmd_datacount; i++)
            {
                for (int j = 0; j < element_size; j++)
                {
                    d[io++] = s[j];
                }
                io += stride1;
            }
#endif
        }


        /// <summary>
        /// fill with ones 
        /// </summary>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static void Ones([NoAlias]float* p, int element_count)
        {
            float f = 1f;
            MemCpyReplicate(p, &f, 4, element_count);
        }

        /// <summary>
        /// fill with ones 
        /// </summary>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static void Ones([NoAlias] float2* p, int element_count)
        {
            float2 f = 1f;
            MemCpyReplicate(p, &f, 8, element_count);
        }

        /// <summary>
        /// fill with ones 
        /// </summary>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static void Ones([NoAlias] float3* p, int element_count)
        {
            float3 f = 1f;
            MemCpyReplicate(p, &f, 12, element_count);
        }

        /// <summary>
        /// fill with ones 
        /// </summary>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static void Ones([NoAlias] float4* p, int element_count)
        {
            float4 f = 1f;
            MemCpyReplicate(p, &f, 16, element_count);
        }

        /// <summary>
        /// check if all values are equal 
        /// </summary>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        static public bool AllEqual([NoAlias] float* fp_mask, int ssmd_datacount)
        {
            float f = fp_mask[0];
            // dont start at 1 although we know it matches, it might kill optimizations when ssmd_datacount != dividable by 4 8 or 16\
            for (int i = 0; i < ssmd_datacount; i++)
            {
                if (fp_mask[i] != f) return false;
            }
            return true;
        }

        /// <summary>
        /// check if all values are equal 
        /// </summary>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        static public bool AllEqual([NoAlias] float2* fp_mask, int ssmd_datacount)
        {
            float2 f = fp_mask[0];
            for (int i = 0; i < ssmd_datacount; i++)
            {
                if (math.any(fp_mask[i] != f)) return false;
            }
            return true;
        }

        /// <summary>
        /// check if all values are equal 
        /// </summary>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        static public bool AllEqual([NoAlias] float3* fp_mask, int ssmd_datacount)
        {
            float3 f = fp_mask[0];
            for (int i = 0; i < ssmd_datacount; i++)
            {
                if (math.any(fp_mask[i] != f)) return false;
            }
            return true;
        }

        /// <summary>
        /// check if all values are equal 
        /// </summary>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        static public bool AllEqual([NoAlias] float4* fp_mask, int ssmd_datacount)
        {
            float4 f = fp_mask[0];
            for (int i = 0; i < ssmd_datacount; i++)
            {
                if (math.any(fp_mask[i] != f)) return false;
            }
            return true;
        }

        /// <summary>
        /// check if all values are equal 
        /// </summary>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        static public bool AllEqual([NoAlias] uint* fp_mask, int ssmd_datacount)
        {
            uint f = fp_mask[0];
            for (int i = 0; i < ssmd_datacount; i++)
            {
                if (fp_mask[i] != f) return false;
            }
            return true;
        }

        /// <summary>
        /// set memory 
        /// </summary>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        static public void MemSet([NoAlias] float4* destination, float constant, int count)
        {
            float4 f4 = constant;
            MemCpyReplicate(destination, &f4, 16, count);
        }

        /// <summary>
        /// set memory 
        /// </summary>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        static public void MemSet([NoAlias] float3* destination, float constant, int count)
        {
            float3 f3 = constant;
            MemCpyReplicate(destination, &f3, 12, count);
        }

        /// <summary>
        /// set memory 
        /// </summary>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        static public void MemSet([NoAlias] float2* destination, float constant, int count)
        {
            float2 f2 = constant;
            MemCpyReplicate(destination, &f2, 8, count);
        }

        /// <summary>
        /// set memory 
        /// </summary>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        static public void MemSet([NoAlias] float* destination, float constant, int count)
        {
            MemCpyReplicate(destination, &constant, 4, count);
        }
    }
}
