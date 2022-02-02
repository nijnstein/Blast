//##########################################################################################################
// Copyright © 2022 Rob Lemmens | NijnStein Software <rob.lemmens.s31@gmail.com> All Rights Reserved       #
// Unauthorized copying of this file, via any medium is strictly prohibited                                #
// Proprietary and confidential                                                                            #
//##########################################################################################################
#if !STANDALONE_VSBUILD
    using Unity.Collections.LowLevel.Unsafe; 
#endif

using System;
using Unity.Collections;

namespace NSS.Blast
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member 'UnsafeUtils'
    unsafe public struct UnsafeUtils
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member 'UnsafeUtils'
    {
#if STANDALONE_VSBUILD 
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member 'UnsafeUtils.Malloc(long, int, Allocator)'
        unsafe public static void* Malloc(long size, int alignment, Allocator allocator)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member 'UnsafeUtils.Malloc(long, int, Allocator)'
        {
            return System.Runtime.InteropServices.Marshal.AllocHGlobal((int)size).ToPointer();
        }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member 'UnsafeUtils.Free(void*, Allocator)'
        unsafe public static void Free(void* memory, Allocator allocator)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member 'UnsafeUtils.Free(void*, Allocator)'
        {
            System.Runtime.InteropServices.Marshal.FreeHGlobal((IntPtr)memory); 
        }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member 'UnsafeUtils.MemCpy(void*, void*, long)'
        unsafe public static void MemCpy(void* destination, void* source, long size)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member 'UnsafeUtils.MemCpy(void*, void*, long)'
        {
            byte* d = (byte*)destination;
            byte* s = (byte*)source; 
            for(int i = 0; i < size; i++)
            {
                d[i] = s[i];
            }
        }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member 'UnsafeUtils.MemCpyReplicate(void*, void*, int, int)'
        unsafe public static void MemCpyReplicate(void* destination, void* source, int size, int count)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member 'UnsafeUtils.MemCpyReplicate(void*, void*, int, int)'
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

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member 'UnsafeUtils.MemSet(void*, byte, long)'
        unsafe public static void MemSet(void* destination, byte value, long size)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member 'UnsafeUtils.MemSet(void*, byte, long)'
        {
            byte* d = (byte*)destination;
            for (int i = 0; i < size; i++)
            {
                d[i] = value; 
            }
        }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member 'UnsafeUtils.MemClear(void*, long)'
        unsafe public static void MemClear(void* destination, long size)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member 'UnsafeUtils.MemClear(void*, long)'
        {
            MemSet(destination, 0, size); 
        }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member 'UnsafeUtils.SizeOf(Type)'
        public static int SizeOf(Type type)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member 'UnsafeUtils.SizeOf(Type)'
        {
            return Unity.Collections.LowLevel.Unsafe.UnsafeUtility.SizeOf(type); 
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
