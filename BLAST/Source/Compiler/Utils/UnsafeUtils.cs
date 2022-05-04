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


namespace NSS.Blast
{
    /// <summary>
    /// debugging / standalone interface to UnsafeUtility 
    /// </summary>
    unsafe public struct UnsafeUtils
    {
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
