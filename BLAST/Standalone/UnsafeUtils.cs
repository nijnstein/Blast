using System;
using Unity.Collections;

#if NOT_USING_UNITY
using System.Runtime.InteropServices;
#endif

namespace NSS.Blast.Standalone
{
    unsafe public struct UnsafeUtils
    {
#if NOT_USING_UNITY
        unsafe public static void* Malloc(long size, int alignment, Allocator allocator)
        {
            return Marshal.AllocHGlobal((int)size).ToPointer();
        }

        unsafe public static void Free(void* memory, Allocator allocator)
        {
            Marshal.FreeHGlobal((IntPtr)memory); 
        }

        unsafe public void MemCpy(void* destination, void* source, long size)
        {
            byte* d = (byte*)destination;
            byte* s = (byte*)source; 
            for(int i = 0; i < size; i++)
            {
                d[i] = s[i];
            }
        }

        unsafe public void MemCpyReplicate(void* destination, void* source, int size, int count)
        {
            byte* d = (byte*)destination;
            byte* s = (byte*)source;

            int i = 0;
            while (i <  count)
            {
                int j = 0;
                while (j < size)
                {
                    d[i] = s[j]; 
                    j++; 
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

#else
        unsafe public static void* Malloc(long size, int alignment, Allocator allocator)
        {
            return UnsafeUtility.Malloc(size, alignment, allocator); 
        }

        unsafe public static void Free(void* memory, Allocator allocator)
        {
            UnsafeUtility.Free(memory, allocator); 
        }

        unsafe public void MemCpy(void* destination, void* source, long size)
        {
            UnsafeUtility.MemCpy(destination, source, size);
        }

        unsafe public void MemCpyReplicate(void* destination, void* source, int size, int count)
        {
            UnsafeUtility.MemCpyReplicate(desination, source,size, count); 
        } 

        unsafe public static void MemSet(void* destination, byte value, long size)
        {
            UnsafeUtility.MemSet(destination, value, size); 
        }

        unsafe public static void MemClear(void* destination, long size)
        {
            UnsafeUtility.MemClear(destination, size);
        }

        public static int SizeOf(Type type)
        {
            return UnsafeUtility.SizeOf(type); 
        }
#endif
    }
}
