//##########################################################################################################
// Copyright © 2022 Rob Lemmens | NijnStein Software <rob.lemmens.s31@gmail.com> All Rights Reserved       #
// Unauthorized copying of this file, via any medium is strictly prohibited                                #
// Proprietary and confidential                                                                            #
//##########################################################################################################
#if STANDALONE_VSBUILD
    using NSS.Blast.Standalone;
#else
    using UnityEngine;
#endif

using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

namespace NSS.Blast
{

    /// <summary>
    /// Blast script packaging types 
    /// </summary>
    public enum BlastPackageMode : byte
    {
        /// <summary>
        /// Compile all into a single package, code data and stack combined  
        /// <code>
        /// [----CODE----|----METADATA----|----DATA----|----STACK----]
        ///              1                2            3             4  
        /// 
        /// ** ALL OFFSETS IN PACKAGE IN BYTES ***
        /// 
        /// 1 = metadata offset 
        /// 2 = data_offset 
        /// 3 = stack_offset 
        /// 4 = package_size  
        /// </code>
        /// </summary>
        Normal,

        /// <summary>
        /// SSMD Package with code and metadata seperate from datastack
        /// <code>
        /// - SSMD requirement: while data changes, its intent does not so metadata is shared
        /// 
        /// - SSDM in V2/BS2 allows for (nested) branching by setting sync points on non constant jumps: 
        /// -- loop ends
        /// -- if then else jumps 
        /// 
        /// [----CODE----|----METADATA----]       [----DATA----|----STACK----]
        ///              1                2                    3             4
        /// 
        /// 1 = metadata offset 
        /// 2 = codesegment size 
        /// 3 = stack offset 
        /// 4 = datasegment size 
        /// 
        /// prop stacksize (in elements) => (datasegment size - stack_offset) / 4
        /// </code>
        /// </summary>
        SSMD,

        /// <summary>
        /// Entity Package: the script's code is seperated from all data
        /// <code>
        /// [----CODE----]      [----METADATA----|----DATA----|----STACK----]
        ///              1                       2            3             4
        /// 
        /// 1 = codesegment size 
        /// 2 = metadata size 
        /// 3 = stack offset 
        /// 4 = datasegment size 
        /// </code>
        /// </summary>
        Entity,

        /// <summary>
        /// <code>
        /// package type used by compiler 
        /// 
        /// - THE POINTERS ARE INVALID
        /// 
        /// [----CODE----]      [----METADATA----]     [----DATA----|----STACK----]
        ///              1                       2                  3             4
        /// 
        /// 1 = codesegment size 
        /// 2 = metadata size 
        /// 3 = datasize in bytes / stack offset in bytes 
        /// 4 = datasegment size = stackcapacity = 4 - 3  
        /// </code>
        /// </summary>
        Compiler
    }

    /// <summary>
    /// Package Flags
    /// </summary>
    [Flags]
    public enum BlastPackageFlags : byte
    {
        /// <summary>
        /// full data allocated, package can be directly executed 
        /// </summary>
        None = 0,

        /// <summary>
        /// package is allocated without stack data (template for SSMD) 
        /// </summary>
        NoStack = 1,

        /// <summary>
        /// align data offsets on a 4 byte boundary
        /// </summary>
        Aligned4 = 2,

        /// <summary>
        /// align data offsets on a 4 byte boundary
        /// </summary>
        Aligned8 = 4
    }


    /// <summary>
    /// a pointer to meta-data-stack memory
    /// </summary>
    [BurstCompile]
    unsafe public struct BlastMetaDataStack
    {
    }

    /// <summary>
    /// data for ssmd operation modes: data-stack only
    /// - only used as pointer, fields handy in debugger
    /// </summary>
    [BurstCompile]
    unsafe public struct BlastSSMDDataStack {
#if DEVELOPMENT_BUILD
        /// <summary>
        /// fictional - for debug view only
        /// </summary>
        public float4 a;
        /// <summary>
        /// fictional - for debug view only
        /// </summary>
        public float4 b;
        /// <summary>
        /// fictional - for debug view only
        /// </summary>
        public float4 c;
        /// <summary>
        /// fictional - for debug view only
        /// </summary>
        public float4 d;
#endif 
    }

    /// <summary>
    /// the bare minimum data (28 bytes) to package a script for execution 
    /// </summary>
    [BurstCompile]
    unsafe public struct BlastPackageData
    {
        /// <summary>
        /// points to codesegment, may be shared with other segments, see packagemode
        /// </summary>
        [NoAlias]
        [NativeDisableUnsafePtrRestriction]
        public IntPtr CodeSegmentPtr;

        /// <summary>
        /// points to the other segment, see packagemode
        /// </summary>
        [NoAlias]
        [NativeDisableUnsafePtrRestriction]
        public IntPtr P2;
        //-- 16 bytes


        /// <summary>
        /// Script packaging mode
        /// </summary>
        /// <remarks>
        /// <code>
        /// Packaging mode 
        /// 
        /// NORMAL: 
        ///         [----CODE----|----METADATA----|----DATA----|----STACK----]
        ///                      1                2            3             4  
        /// 
        /// SSMD:
        ///         [----CODE----|----METADATA----]       [----DATA----|----STACK----]
        ///                      1                2                    3             4
        /// 
        /// ENTITY:
        ///         [----CODE----]      [----METADATA----]     [----DATA----|----STACK----]
        ///                      1                       2                  3             4
        /// </code>
        /// </remarks>
        public BlastPackageMode PackageMode;

        /// <summary>
        /// Package languageversion, determines feature support 
        /// </summary>
        public BlastLanguageVersion LanguageVersion;

        /// <summary>
        /// Package flags, see flags enumeration  
        /// </summary>
        public BlastPackageFlags Flags;

        /// <summary>
        /// Package memory allocator 
        /// </summary>
        public byte Allocator;
        //-- 20 bytes

        /// <summary>
        /// Offset 1, intent depends on packagemode
        /// </summary>
        public ushort O1;

        /// <summary>
        /// Offset 2, intent depends on packagemode
        /// </summary>
        public ushort O2;

        /// <summary>
        /// Offset 3, intent depends on packagemode
        /// </summary>
        public ushort O3;

        /// <summary>
        /// Offset 4, intent depends on packagemode
        /// </summary>
        public ushort O4;
                            //-- 28 bytes

        #region Properties deduced from fields 

        /// <summary>
        /// size of code in bytes, maps to O1 in all package modes 
        /// </summary>
        public readonly ushort CodeSize
        {
            get
            {
                // in every packagemode this is O1
                return O1;
            }
        }

        /// <summary>
        /// metadata size in bytes, may include alignment bytes in normal and SSMD modes  
        /// </summary>
        public readonly ushort MetadataSize
        {
            get
            {

#if DEVELOPMENT_BUILD
                switch (PackageMode)
                {
                    default:
                        Debug.LogError($"packagemode {PackageMode} not supported");
                        return 0;

                    case BlastPackageMode.Normal:
                    case BlastPackageMode.SSMD: return (ushort)(O2 - O1);

                    case BlastPackageMode.Compiler: 
                    case BlastPackageMode.Entity: return O2;
                }
#else 
                // less handy to read, no debug, but a lot faster and NO branch 
                return (ushort)math.select(O2 - O1, O2, (PackageMode == BlastPackageMode.Entity) || (PackageMode == BlastPackageMode.Compiler));
#endif 
            }
        }


        /// <summary>
        /// data size in bytes, may include alignment bytes 
        /// </summary>
        public readonly ushort DataSize
        {
            get
            {
#if DEVELOPMENT_BUILD
                switch (PackageMode)
                {
                    default:
                        Debug.LogError($"packagemode {PackageMode} not supported");
                        return 0;

                    case BlastPackageMode.Normal:
                    case BlastPackageMode.Entity: return (ushort)(O3 - O2);

                    case BlastPackageMode.Compiler: return O3; 
                    case BlastPackageMode.SSMD: return O3;
                }
#else 
                return (ushort)math.select(O3 - O1, O3, (PackageMode == BlastPackageMode.SSMD) || (PackageMode == BlastPackageMode.Compiler)); 
#endif
            }
        }

        /// <summary>
        /// stack size in bytes, may not be a multiple of elementsize/4bytes
        /// </summary>
        public readonly ushort StackSize
        {
            get
            {
#if DEVELOPMENT_BUILD
                switch (PackageMode)
                {
                    default:
                        Debug.LogError($"packagemode {PackageMode} not supported");
                        return 0;

                    case BlastPackageMode.Normal:
                    case BlastPackageMode.Entity:
                    case BlastPackageMode.Compiler:
                    case BlastPackageMode.SSMD: return (ushort)(O4 - O3);
                }
#else
                return (ushort)(O4 - O3);
#endif
            }
        }


        /// <summary>
        /// the allocated datasize in bytes of the codesegment pointer 
        /// </summary>
        public readonly ushort CodeSegmentSize
        {
            get
            {
                switch (PackageMode)
                {
                    default:
#if DEVELOPMENT_BUILD
                        Debug.LogError($"packagemode {PackageMode} not supported");
#endif 
                        return 0;

                    case BlastPackageMode.Normal: return O4;
                    case BlastPackageMode.SSMD: return O2;
                    case BlastPackageMode.Entity: return O1;
                }
            }
        }

        /// <summary>
        /// the size if stack would be included, while on normal packaging mode there is no seperate buffer there is a size
        /// </summary>
        public readonly ushort DataSegmentSize
        {
            get
            {
                switch (PackageMode)
                {
                    default:
#if DEVELOPMENT_BUILD
                        Debug.LogError($"packagemode {PackageMode} not supported");
#endif 
                        return 0;

                    case BlastPackageMode.Normal: return (ushort)(O4 - O1);
                    case BlastPackageMode.SSMD: return O4;
                    case BlastPackageMode.Entity: return O4;
                }
            }
        }

        /// <summary>
        /// bytesize for needed BlastSSMDDataStack records
        /// </summary>
        public readonly int SSMDDataSize => DataSegmentSize; 

        /// <summary>
        /// the size with stack included depending on flags
        /// </summary>
        public readonly ushort AllocatedDataSegmentSize
        {
            get
            {
                switch (PackageMode)
                {
                    default:
#if DEVELOPMENT_BUILD
                        Debug.LogError($"packagemode {PackageMode} not supported");
#endif 
                        return 0;

                    case BlastPackageMode.Normal: return 0;
                    case BlastPackageMode.SSMD: return HasStack ? O4 : O3;
                    case BlastPackageMode.Entity: return HasStack ? O4 : O3;
                }
            }
        }

        /// <summary>
        /// the maxiumum stack capacity in elements (1 element == 32 bit)
        /// - looks at flags, if == nostack then returns a capacity of 0
        /// - IMPORTANT NOTE: looking at profiler this really should be cached if asked a lot 
        /// </summary>
        public readonly ushort StackCapacity
        {
            get
            {
                return (ushort)((O4 - O3) >> 2);  // HasStack ? (ushort)((O4 - O3) / 4) : (ushort)0;
                //
                //                switch (PackageMode)
                //                {
                //                    default:
                //#if DEVELOPMENT_BUILD
                //                        Standalone.Debug.LogError($"packagemode {PackageMode} not supported");
                //#endif 
                //                        return 0;
                //
                //                    case BlastPackageMode.Normal: return (ushort)((O4 - O3) / 4);
                //                    case BlastPackageMode.SSMD: return (ushort)((O4 - O3) / 4);
                //                    case BlastPackageMode.Entity: return (ushort)((O4 - O3) / 4);
                //                }
            }
        }

        /// <summary>
        /// stack offset in bytes as seen from the start of datasegment
        /// - stack and datasegment MUST ALWAYS be in the same segment so that Data[last] == Stack[-1] 
        /// - when using .Stack[] offset is 0
        /// </summary>
        public readonly ushort DataSegmentStackOffset
        {
            get
            {
                // in every mode this is O3
                return O3;
            }
        }

        /// <summary>
        /// Calculated pointer to metadata 
        /// </summary>
        public readonly IntPtr MetadataPtr
        {
            get
            {
#if DEVELOPMENT_BUILD
                switch (PackageMode)
                {
                    default:
                        Debug.LogError($"packagemode {PackageMode} not supported");
                        return IntPtr.Zero;

                    case BlastPackageMode.Normal: return CodeSegmentPtr + O1;
                    case BlastPackageMode.SSMD: return CodeSegmentPtr + O1;
                    case BlastPackageMode.Entity: return P2;
                }
#else
                return PackageMode == BlastPackageMode.Entity ? P2 : CodeSegmentPtr + O1; 
#endif
            }
        }
        /// <summary>
        /// pointer to data segment as calculated from package configuration 
        /// </summary>
        public readonly IntPtr DataSegmentPtr
        {
            get
            {
                switch (PackageMode)
                {
                    default:
#if DEVELOPMENT_BUILD
                        Debug.LogError($"packagemode {PackageMode} not supported");
#endif                        
                        return IntPtr.Zero;

                    case BlastPackageMode.Normal: return CodeSegmentPtr + O2;
                    case BlastPackageMode.Entity: return P2 + O2;
                    case BlastPackageMode.SSMD: return P2;
                }
            }
        }

        /// <summary>
        /// pointer to stack segment as calculated from package configuration 
        /// </summary>
        public readonly IntPtr StackSegmentPtr
        {
            get
            {
                switch (PackageMode)
                {
                    default:
#if DEVELOPMENT_BUILD
                        Debug.LogError($"packagemode {PackageMode} not supported");
#endif 
                        return IntPtr.Zero;

                    case BlastPackageMode.Normal: return CodeSegmentPtr + O3;
                    case BlastPackageMode.SSMD: return DataSegmentPtr + O3;
                    case BlastPackageMode.Entity: return DataSegmentPtr + O3;
                }
            }
        }

        /// <summary>
        /// Code[] pointer 
        /// </summary>
        public readonly unsafe byte* Code => (byte*)CodeSegmentPtr.ToPointer();

        /// <summary>
        /// metadata[], 1 byte of metadata for each dataelement in stack and datasegment 
        /// </summary>
        public readonly unsafe byte* Metadata => (byte*)MetadataPtr.ToPointer();

        /// <summary>
        /// data[] pointer 
        /// </summary>
        public readonly unsafe float* Data => (float*)DataSegmentPtr.ToPointer();

        /// <summary>
        /// Stack[] pointer -> Data[last] == Stack[-1]
        /// </summary>
        public readonly unsafe float* Stack => (float*)StackSegmentPtr.ToPointer();

        /// <summary>
        /// true if memory for stack is allocated, 
        /// false if not: 
        /// - yield not possible without persistant stack 
        /// - we call it TLS, thread level stack/storage 
        /// - Faster in small multithreaded bursts, benefit fades in very large bursts 
        /// </summary>
        public readonly bool HasStack => (Flags & BlastPackageFlags.NoStack) != BlastPackageFlags.NoStack;

        /// <summary>
        /// true if memory has been allocated for this object
        /// </summary>
        public readonly bool IsAllocated => (Allocator)Allocator != Unity.Collections.Allocator.None && CodeSegmentPtr != IntPtr.Zero;

        #endregion

        #region Cloning Util

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member 'BlastPackageData.Clone()'
        public BlastPackageData Clone()
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member 'BlastPackageData.Clone()'
        {
            return Clone((Unity.Collections.Allocator)Allocator); 
        }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member 'BlastPackageData.Clone(Allocator)'
        public BlastPackageData Clone(Allocator allocator)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member 'BlastPackageData.Clone(Allocator)'
        {
            BlastPackageData clone = default;
            clone.PackageMode = PackageMode;
            clone.LanguageVersion = LanguageVersion;
            clone.Flags = Flags;
            clone.Allocator = (byte)allocator;
            clone.O1 = O1;
            clone.O2 = O2;
            clone.O3 = O3;
            clone.O4 = O4;

            if(IsAllocated)
            {
                switch (PackageMode)
                {
                    case BlastPackageMode.Normal:
                        clone.CodeSegmentPtr = new IntPtr(UnsafeUtils.Malloc(CodeSegmentSize, 8, allocator));
                        UnsafeUtils.MemCpy(clone.Code, Code, CodeSegmentSize);

                        clone.P2 = IntPtr.Zero;
                        break;
                    case BlastPackageMode.SSMD:
                    case BlastPackageMode.Entity:
                        clone.CodeSegmentPtr = new IntPtr(UnsafeUtils.Malloc(CodeSegmentSize, 8, allocator));
                        UnsafeUtils.MemCpy(clone.Code, Code, CodeSegmentSize);

                        clone.P2 = new IntPtr(UnsafeUtils.Malloc(clone.DataSegmentSize, 8, allocator));
                        UnsafeUtils.MemCpy(clone.P2.ToPointer(), P2.ToPointer(), clone.DataSegmentSize); 
                        break;

                    default:
                        Debug.LogError($"BlastPackageData.Clone: PackageMode {PackageMode} is not supported");
                        break; 
                }
            }
            return clone; 
        }


        /// <summary>
        /// clone n data segments into 1 block and index it with pointers 
        /// - the first block contains the root pointer which can be freed 
        /// </summary>
        /// <param name="n"></param>
        /// <returns></returns>
        unsafe public BlastMetaDataStack* CloneData(int n = 1)
        {
            return CloneData(n, (Unity.Collections.Allocator)Allocator);
        }

        /// <summary>
        /// clone n data segments into 1 block and index it with pointers 
        /// - the first block contains the root pointer which can be freed 
        /// </summary>
        /// <param name="n"></param>
        /// <returns></returns>
        unsafe public BlastSSMDDataStack* CloneDataStack(int n = 1)
        {
            return CloneDataStack(n, (Unity.Collections.Allocator)Allocator);
        }

        bool CheckPackageModeForDataCloning()
        {
            if (PackageMode == BlastPackageMode.Compiler)
            {
                Debug.LogError($"CloneData: packagemode {PackageMode} not supported");
                return false;
            }
            return true; 
        }

        /// <summary>
        /// clone n segments into 1 memory block and index it with pointers 
        /// - the first block contains the root pointer to free later
        /// </summary>
        unsafe public BlastMetaDataStack* CloneData(int n, Allocator allocator)
        {
            if (!CheckPackageModeForDataCloning()) return null; 

            int byte_size = DataSegmentSize;
            int total_byte_size = byte_size * n;

            BlastMetaDataStack* data = (BlastMetaDataStack*)UnsafeUtils.Malloc(total_byte_size, 8, (Unity.Collections.Allocator)Allocator);

            return CloneData(n, data); 
        }


        /// <summary>
        /// clone n segments into 1 memory block and index it with pointers 
        /// - the first block contains the root pointer to free later
        /// </summary>
        unsafe public BlastSSMDDataStack* CloneDataStack(int n, Allocator allocator)
        {
            if (!CheckPackageModeForDataCloning()) return null;

            int byte_size = DataSegmentSize - MetadataSize;
            int total_byte_size = byte_size * n;

            BlastSSMDDataStack* data = (BlastSSMDDataStack*)UnsafeUtils.Malloc(total_byte_size, 8, (Unity.Collections.Allocator)Allocator);

            return CloneDataStack(n, data);
        }
        /// <summary>
        /// clone n segments into 1 memory block and index it with pointers 
        /// - the first block contains the root pointer to free later
        /// </summary>
        unsafe public BlastMetaDataStack* CloneData(int n, BlastMetaDataStack* data)
        {
            if (!CheckPackageModeForDataCloning()) return null;

            int byte_size = DataSegmentSize;

            // replicate memory 
            switch (PackageMode)
            {
                case BlastPackageMode.Normal:
                case BlastPackageMode.Entity:
                    // normal: continues pointer from O1 -> O4 (which is start of metadata)
                    // entity: continues pointer from 0 -> O4 (again from metadata start)
                    UnsafeUtils.MemCpyReplicate(data, Metadata, byte_size, n);
                    break;

                case BlastPackageMode.SSMD:

                    // copy 1st meta data & data/stack
                    UnsafeUtils.MemCpy(data, Metadata, MetadataSize);
                    UnsafeUtils.MemCpy(data, Data, DataSize);

                    // replicate from there 
                    if (n > 1)
                    {
                        UnsafeUtils.MemCpyReplicate(data + byte_size, data, byte_size, n - 1);
                    }
                    break;
            }

            return (BlastMetaDataStack*)data;
        }

        /// <summary>
        /// clone n segments into 1 memory block and index it with pointers 
        /// - the first block contains the root pointer to free later
        /// </summary>
        unsafe public BlastSSMDDataStack* CloneDataStack(int n, BlastSSMDDataStack* data)
        {
            if (!CheckPackageModeForDataCloning()) return null;

            int byte_size = SSMDDataSize;

            // replicate memory 
            switch (PackageMode)
            {
                case BlastPackageMode.SSMD:
                case BlastPackageMode.Normal:
                case BlastPackageMode.Entity:
                    // normal: continues pointer from O1 -> O4 (which is start of metadata)
                    // entity: continues pointer from 0 -> O4 (again from metadata start)
                    UnsafeUtils.MemCpyReplicate(data, DataSegmentPtr.ToPointer(), byte_size, n);
                    break;
            }

            return data;
        }


        /// <summary>
        /// free memory used by an ssmd data block
        /// </summary>
        unsafe public static void FreeData(BlastMetaDataStack* data, Allocator allocator)
        {
            if (data != null)
            {
                UnsafeUtils.Free(data, allocator);
                data = null;
            }
        }
        /// <summary>
        /// free memory used by an ssmd data block
        /// </summary>
        unsafe public static void FreeData(BlastSSMDDataStack* data, Allocator allocator)
        {
            if (data != null)
            {
                UnsafeUtils.Free(data, allocator);
                data = null;
            }
        }

        #endregion 

        /// <summary>
        /// free any memory allocated 
        /// </summary>
        public void Free()
        {
            if (IsAllocated)
            {
                unsafe
                {
                    UnsafeUtils.Free(CodeSegmentPtr.ToPointer(), (Unity.Collections.Allocator)Allocator);
                    if (P2 != IntPtr.Zero)
                    {
                        UnsafeUtils.Free(P2.ToPointer(), (Unity.Collections.Allocator)Allocator);
                    }
                }
#if DEVELOPMENT_BUILD 
                CodeSegmentPtr = IntPtr.Zero;
                P2 = IntPtr.Zero;
                Allocator = (byte)Unity.Collections.Allocator.None;
#endif
            }
        }
    }
}
