using NSS.Blast.Standalone;
using System;
using System.Text;
using System.Threading;
using Unity.Assertions;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace NSS.Blast
{

    /// <summary>
    /// Blast script packaging types 
    /// </summary>
    public enum BlastPackageMode : byte
    {
        /// <summary>
        /// compile all into a single package, code data and stack combined  
        /// </summary>
        CodeDataStack,

        /// <summary>
        /// package with code and data but with the execution stack seperate 
        /// </summary>
        CodeData,

        /// <summary>
        /// only package the code, uses code from manager, data is to be supplied by caller before execution  
        /// </summary>
        Code,

        /// <summary>
        /// package containing only data, code and stack are handled by the manager 
        /// </summary>
        Data,

        /// <summary>
        /// package containing data and stack only with script kept in manager 
        /// </summary>
        DataStack,

        /// <summary>
        /// package type used by compiler 
        /// </summary>
        Compiler
    }


    /// <summary>
    /// Package Information Header
    /// - contains sizes of buffers and package settings
    /// - should be 12 byte large (assert on that ??) 
    /// </summary>
    [BurstCompatible]
    public struct BlastPackageInfo
    {
        //------------------------- data -----------------------------------
        public BlastPackageMode package_mode;
        public byte allocator;
        public byte reserved_1;   // => language version 
        public byte reserved_2;

        /// <summary>
        /// code size in bytes 
        /// </summary>
        public ushort code_size;

        /// <summary>
        /// stack size in slots (float, 4 bytes)
        /// </summary>
        public ushort stack_size;

        /// <summary>
        /// data size in bytes
        /// </summary>
        public ushort data_size;

        /// <summary>
        /// package size in bytes 
        /// </summary>
        public ushort package_size;

        // 12 byte boundary

        //------------------------- properties -----------------------------------
        public bool is_variable_size => (Allocator)allocator != Allocator.Invalid;
        public bool is_fixed_size => (Allocator)allocator == Allocator.Invalid;
        public int data_capacity => package_size - code_size;
        public int data_sizes_size => data_capacity / 4;
        public Allocator Allocator => (Allocator)Allocator;
        public BlastPackageCapacities PackageCapacity => (BlastPackageCapacities)package_size;
    }


    /// <summary>
    /// Blast Package as executed by the interpretor  
    /// - starts with the header struct: BlastPackageInfo
    /// - contains code pointer and offsets 
    /// - including header should be exactly 32 bytes large 
    /// </summary>
    [BurstCompatible]
    public struct BlastPackage
    {
        //------------------------- data -----------------------------------
        public BlastPackageInfo info;
        // @ 12 bytes 

        /// <summary>
        /// index into bytecode, next point of execution, if == code_size then end of script is reached
        /// </summary>
        public ushort code_pointer;

        /// <summary>
        /// offset into metadata
        /// </summary>
        public ushort data_sizes_start;

        /// <summary>
        /// offset into segment, here variable data starts (4 byte aligned)
        /// </summary>
        public ushort data_start;

        /// <summary>
        /// here stackdata starts on codedatastack packages 
        /// </summary>
        public ushort stack_start; 

        /// <summary>
        /// offset into data after the last variable
        /// </summary>
        public ushort data_offset;

        /// <summary>
        /// stack offset in #dataelements 
        /// </summary>
        public ushort stack_offset;

        //@ 24 byte offset 
    }


    public interface IBlastSegment
    {
        public void Allocate(ushort byte_size, Allocator allocator);
        public void Free();
    }


    [BurstCompatible]
    public struct BlastCodeSegment : IBlastSegment
    {
        public IntPtr CodeSegmentPtr;

        public ushort ByteSize;
        public byte Allocator;
        public byte LanguageVersion;
        unsafe public byte* Code => (byte*)CodeSegmentPtr;
        public bool IsAllocated => CodeSegmentPtr != IntPtr.Zero && Allocator != (byte)Unity.Collections.Allocator.None;
        public unsafe void Allocate(ushort byte_size, Allocator allocator)
        {
            Assert.IsFalse(IsAllocated);
            CodeSegmentPtr = new IntPtr(UnsafeUtils.Malloc(byte_size, 8, allocator));
            ByteSize = byte_size;
            Allocator = (byte)allocator;
        }
        public unsafe void Free()
        {
            Assert.IsTrue(IsAllocated);
            UnsafeUtils.Free(CodeSegmentPtr.ToPointer(), (Unity.Collections.Allocator)Allocator);
            Allocator = (byte)Unity.Collections.Allocator.None;
            ByteSize = 0;
        }
    }


    [BurstCompatible]
    public struct BlastMetaDataSegment : IBlastSegment
    {
        public IntPtr MetaDataSegmentPtr;

        public ushort ByteSize;
        public byte Allocator;
        public byte reserved; 

        unsafe public byte* MetaData => (byte*)MetaDataSegmentPtr;
        public bool IsAllocated => MetaDataSegmentPtr != IntPtr.Zero && Allocator != (byte)Unity.Collections.Allocator.None;
        public unsafe void Allocate(ushort byte_size, Allocator allocator)
        {
            Assert.IsFalse(IsAllocated);
            MetaDataSegmentPtr = new IntPtr(UnsafeUtils.Malloc(byte_size, 8, allocator));
            ByteSize = byte_size;
            Allocator = (byte)allocator;
        }
        public unsafe void Free()
        {
            Assert.IsTrue(IsAllocated);
            UnsafeUtils.Free(MetaDataSegmentPtr.ToPointer(),(Unity.Collections.Allocator)Allocator);
            Allocator = (byte)Unity.Collections.Allocator.None;
            ByteSize = 0;
        }
    }


    [BurstCompatible]
    public struct BlastDataStackSegment : IBlastSegment
    {
        public IntPtr DataStackSegmentPtr;

        public ushort ByteSize;
        public byte Allocator;
        public byte reserved;
        unsafe public float* DataStack => (float*)DataStackSegmentPtr;
        public bool IsAllocated => DataStackSegmentPtr != IntPtr.Zero && Allocator != (byte)Unity.Collections.Allocator.None;
        public unsafe void Allocate(ushort byte_size, Allocator allocator)
        {
            Assert.IsFalse(IsAllocated); 
            DataStackSegmentPtr = new IntPtr(UnsafeUtils.Malloc(byte_size, 8, allocator));
            ByteSize = byte_size; 
            Allocator = (byte)allocator;
        }
        public unsafe void Free()
        {
            Assert.IsTrue(IsAllocated);
            UnsafeUtils.Free(DataStackSegmentPtr.ToPointer(), (Unity.Collections.Allocator)Allocator);
            Allocator = (byte)Unity.Collections.Allocator.None;
            ByteSize = 0; 
        }
    }


    /// <summary>
    /// a package of fixed size, starting with the package information followed by a fixed databuffer 
    /// </summary>
    [BurstCompatible]
    public struct BlastPackage512
    {
        //------------------------- data -----------------------------------
        public BlastPackage info;
        // @ 32 bytes 

        /// <summary>
        /// the script buffer:
        /// [....code...|...variables&constants...|.datasizes.|...stack.....................]
        /// </summary>
        unsafe public fixed byte segment[512];
    }


    #region various blastpackage types other then 512 

    [BurstCompatible]
    public struct BlastPackage32
    {
        public BlastPackage info;
        unsafe public fixed byte segment[32];
    }
    [BurstCompatible]
    public struct BlastPackage48
    {
        public BlastPackage info;
        unsafe public fixed byte segment[48];
    }
    [BurstCompatible]
    public struct BlastPackage64
    {
        public BlastPackage info;
        unsafe public fixed byte segment[64];
    }
    [BurstCompatible]
    public struct BlastPackage96
    {
        public BlastPackage info;
        unsafe public fixed byte segment[96];
    }
    [BurstCompatible]
    public struct BlastPackage128
    {
        public BlastPackage info;
        unsafe public fixed byte segment[128];
    }
    [BurstCompatible]
    public struct BlastPackage192
    {
        public BlastPackage info;
        unsafe public fixed byte segment[192];
    }
    [BurstCompatible]
    public struct BlastPackage256
    {
        public BlastPackage info;
        unsafe public fixed byte segment[256];
    }
    [BurstCompatible]
    public struct BlastPackage320
    {
        public BlastPackage info;
        unsafe public fixed byte segment[320];
    }
    [BurstCompatible]
    public struct BlastPackage384
    {
        public BlastPackage info;
        unsafe public fixed byte segment[384];
    }
    [BurstCompatible]
    public struct BlastPackage448
    {
        public BlastPackage info;
        unsafe public fixed byte segment[448];
    }
    [BurstCompatible]
    public struct BlastPackage640
    {
        public BlastPackage info;
        unsafe public fixed byte segment[640];
    }
    [BurstCompatible]
    public struct BlastPackage768
    {
        public BlastPackage info;
        unsafe public fixed byte segment[768];
    }
    [BurstCompatible]
    public struct BlastPackage896
    {
        public BlastPackage info;
        unsafe public fixed byte segment[896];
    }
    [BurstCompatible]
    public struct BlastPackage960
    {
        public BlastPackage info;
        unsafe public fixed byte segment[960];
    }
    [BurstCompatible]
    public struct BlastPackagePtr
    {
        public BlastPackage info;
        unsafe public byte* segment;
    }

    #endregion


    /// <summary>
    /// description mapping a variable to an input or output
    /// </summary>
    public class BlastVariableMapping
    {
        public BlastVariable variable;

        public int id => variable.Id;
        public BlastVariableDataType DataType => variable.DataType; 

        public int offset;
        public int bytesize;
        
        public override string ToString()
        {
            if(variable == null)
            {
                return $"variable mapping: var = null, bytesize: {bytesize}, offset: {offset}";
            }
            else
            {
                return $"variable mapping: var: {id} {variable.Name} {variable.DataType}, vectorsize: {variable.VectorSize} , bytesize: {bytesize}, offset: {offset}";
            }
        }
    }

    /// <summary>
    /// supported variable data types 
    /// </summary>
    public enum BlastVariableDataType : byte
    {
        Numeric = 0,
        ID = 1
    }


    /// <summary>
    /// description of a variable as determined during compilation 
    /// </summary>
    public class BlastVariable
    {
        public int Id;
        public string Name;
        public int ReferenceCount;

        public BlastVariableDataType DataType = BlastVariableDataType.Numeric; 

        public bool IsConstant = false;
        public bool IsVector = false;

        public bool IsInput = false;
        public bool IsOutput = false; 

        public int VectorSize = 0;

        public override string ToString()
        {
            return $"{(IsConstant ? "Constant: " : "Variable: ")} {Id}, '{Name}', ref {ReferenceCount}, size {VectorSize}, type {DataType} {(IsInput ? "[INPUT]" : "")} {(IsOutput ? "[OUTPUT]" : "")}";
        }

        internal void AddReference()
        {
            Interlocked.Increment(ref ReferenceCount); 
        }
    }
}