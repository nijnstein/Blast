using NSS.Blast.Standalone;
using System;
using System.Text;
using System.Threading;
using Unity.Assertions;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;

namespace NSS.Blast
{

    /// <summary>
    /// Blast script packaging types 
    /// </summary>
    public enum BlastPackageMode : byte
    {
        /// <summary>
        /// Compile all into a single package, code data and stack combined  
        /// 
        /// [----CODE----|----METADATA----|----DATA----|----STACK----]
        ///              1                2            3             4  
        /// 
        /// ** ALL OFFSETS IN PACKAGE IN BYTES ***
        /// 
        /// 1 = metadata offset 
        /// 2 = data_offset 
        /// 3 = stack_offset 
        /// 4 = package_size  
        /// 
        /// </summary>
        Normal,

        /// <summary>
        /// SSMD Package with code & metadata seperate from data&stack
        /// 
        /// - SSMD requirement: while data changes, its intent does not so metadata is shared
        /// 
        /// ? could allow branches and split from there? 
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
        /// 
        /// </summary>
        SSMD,

        /// <summary>
        /// Entity Package: the script's code is seperated from all data
        /// 
        /// [----CODE----]      [----METADATA----|----DATA----|----STACK----]
        ///              1                       2            3             4
        /// 
        /// 1 = codesegment size 
        /// 2 = metadata size 
        /// 3 = stack offset 
        /// 4 = datasegment size 
        /// 
        /// </summary>
        Entity,

        /// <summary>
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
        /// 
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
    /// the bare minimum data to package a script for execution 
    /// </summary>
    [BurstCompile]
    unsafe public struct BlastPackageData
    {
        public IntPtr CodeSegmentPtr;
        public IntPtr P2;
        //-- 16 bytes

        public BlastPackageMode PackageMode;
        public BlastLanguageVersion LanguageVersion;
        public BlastPackageFlags Flags;
        public byte Allocator;
        //-- 20 bytes

        public ushort O1;   // 1
        public ushort O2;   // 2
        public ushort O3;   // 3
        public ushort O4;   // 4
                            //-- 28 bytes

        #region Properties deduced from fields 

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

#if DEBUG
                switch (PackageMode)
                {
                    default:
                        Standalone.Debug.LogError($"packagemode {PackageMode} not supported");
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
#if DEBUG
                switch (PackageMode)
                {
                    default:
                        Standalone.Debug.LogError($"packagemode {PackageMode} not supported");
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
#if DEBUG
                switch (PackageMode)
                {
                    default:
                        Standalone.Debug.LogError($"packagemode {PackageMode} not supported");
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
#if DEBUG
                        Standalone.Debug.LogError($"packagemode {PackageMode} not supported");
#endif 
                        return 0;

                    case BlastPackageMode.Normal: return O4;
                    case BlastPackageMode.SSMD: return O2;
                    case BlastPackageMode.Entity: return O1;
                }
            }
        }

        /// <summary>
        /// the size if stack would be included
        /// </summary>
        public readonly ushort DataSegmentSize
        {
            get
            {
                switch (PackageMode)
                {
                    default:
#if DEBUG
                        Standalone.Debug.LogError($"packagemode {PackageMode} not supported");
#endif 
                        return 0;

                    case BlastPackageMode.Normal: return 0;
                    case BlastPackageMode.SSMD: return O4;
                    case BlastPackageMode.Entity: return O4;
                }
            }
        }

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
#if DEBUG
                        Standalone.Debug.LogError($"packagemode {PackageMode} not supported");
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
                //#if DEBUG
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
#if DEBUG
                switch (PackageMode)
                {
                    default:
                        Standalone.Debug.LogError($"packagemode {PackageMode} not supported");
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
#if DEBUG
                        Standalone.Debug.LogError($"packagemode {PackageMode} not supported");
#endif                        
                        return IntPtr.Zero;

                    case BlastPackageMode.Normal: return CodeSegmentPtr + O2;
                    case BlastPackageMode.Entity: return DataSegmentPtr + O2;
                    case BlastPackageMode.SSMD: return DataSegmentPtr;
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
#if DEBUG
                        Standalone.Debug.LogError($"packagemode {PackageMode} not supported");
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
        /// true if memory for stack is allocated 
        /// </summary>
        public readonly bool HasStack => (Flags & BlastPackageFlags.NoStack) != BlastPackageFlags.NoStack;

        /// <summary>
        /// true if memory has been allocated for this object
        /// </summary>
        public readonly bool IsAllocated => (Allocator)Allocator == Unity.Collections.Allocator.None && CodeSegmentPtr != IntPtr.Zero;

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
#if DEBUG 
                CodeSegmentPtr = IntPtr.Zero;
                P2 = IntPtr.Zero;
                Allocator = (byte)Unity.Collections.Allocator.None;
#endif
            }
        }
    }
}
