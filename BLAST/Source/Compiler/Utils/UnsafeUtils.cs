//##########################################################################################################
// Copyright © 2022 Rob Lemmens | NijnStein Software <rob.lemmens.s31@gmail.com> All Rights Reserved  ^__^\#
// Unauthorized copying of this file, via any medium is strictly prohibited                           (oo)\#
// Proprietary and confidential                                                                       (__) #
//##########################################################################################################
#if !STANDALONE_VSBUILD
    using Unity.Collections.LowLevel.Unsafe; 
#endif

using System;
using Unity.Collections;
using Unity.Mathematics;

namespace NSS.Blast
{
    unsafe public struct UnsafeUtils
    {
#if STANDALONE_VSBUILD 
        unsafe public static void* Malloc(long size, int alignment, Allocator allocator)
        {
            return System.Runtime.InteropServices.Marshal.AllocHGlobal((int)size).ToPointer();
        }
        unsafe public static void Free(void* memory, Allocator allocator)
        {
            System.Runtime.InteropServices.Marshal.FreeHGlobal((IntPtr)memory); 
        }

        unsafe public static void MemCpy(void* destination, void* source, long size)
        {
            byte* d = (byte*)destination;
            byte* s = (byte*)source; 
            for(int i = 0; i < size; i++)
            {
                d[i] = s[i];
            }
        }

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

        unsafe public static void MemSet(void* destination, byte value, long size)
        {
            byte* d = (byte*)destination;
            for (int i = 0; i < size; i++)
            {
                d[i] = value; 
            }
        }

        unsafe public static void MemClear(void* destination, long size)
        {
            MemSet(destination, 0, size); 
        }

        public static int SizeOf(Type type)
        {
            return Unity.Collections.LowLevel.Unsafe.UnsafeUtility.SizeOf(type); 
        }

        public static void MemCpyStride(void* pout, int stride1, float* pin, int stride2, int size, int ssmd_datacount)
        {
            byte* d = (byte*)pout;
            byte* s = (byte*)pin;

            int ii = 0;
            int io = 0; 
            for(int i = 0; i < ssmd_datacount; i++)
            {
                for(int j = 0; j < size; j++)
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
    }
}
