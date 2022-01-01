using System;
using System.Text;
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
        public byte unused_padding_1;
        public byte unused_padding_2;

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
    /// Blast Package 
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
        public int code_pointer;

        /// <summary>
        /// offset into segment, here variable data starts (4 byte aligned)
        /// </summary>
        public int data_start;

        /// <summary>
        /// offset into datasize array between vars and stack start
        /// </summary>
        public int data_sizes_start;

        /// <summary>
        /// offset into data after the last variable, from here the stack starts 
        /// </summary>
        public int data_offset;

        /// <summary>
        /// stack offset 
        /// </summary>
        public int stack_offset;

        //@ 32 byte offset 
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
        public int offset;
        public int bytesize;
    }


    /// <summary>
    /// description of a variable as determined from compiled code
    /// </summary>
    public class BlastVariable
    {
        public int Id;
        public string Name;
        public int ReferenceCount;

        public bool IsConstant = false;
        public bool IsVector = false;

        public bool IsInput = false;
        public bool IsOutput = false; 

        public int VectorSize = 0;

        public override string ToString()
        {
            return $"{(IsConstant ? "Constant:" : "Variable:")} {Id}, '{Name}', ref {ReferenceCount}, size {VectorSize} {(IsInput ? "[INPUT]" : "")} {(IsOutput ? "[OUTPUT]" : "")}";
        }
    }



    /// <summary>
    /// Blast Package returned from compiler 
    /// - contains BlastPackage
    /// - contains information about input and outputs (if defined) 
    /// - contains additional variable information (names & offsets)
    /// </summary>
    public class BlastScriptPackage
    {
        public IntPtr PackagePtr;
        public unsafe BlastPackage* BlastPackage => (BlastPackage*)PackagePtr;

        /// <summary>
        /// points into the codesegment in blastpackage
        /// </summary>
        public unsafe byte* CodeSegment
        {
            get
            {
                byte* code_segment = (byte*)PackagePtr;
                return (byte*)(code_segment + 32);
            }
        }

        /// <summary>
        /// points into the datasegment in blastpackage 
        /// </summary>
        public unsafe float* DataSegment
        {
            get
            {
                byte* code_segment = (byte*)PackagePtr;

                int offset = BlastPackage->info.code_size;
                while (offset % 4 != 0) offset++;

                return (float*)(code_segment + offset + 32);
            }
        }

        // -- input / output mapping -------------------------------------
        public BlastVariableMapping[] Inputs;
        public BlastVariableMapping[] Outputs;

        // -- managed information about execution, mainly for debugging -- 
        public BlastVariable[] Variables;

        /// <summary>
        /// offset in float count into datasegment
        /// </summary>
        public byte[] VariableOffsets;

        // -- methods & properties ---------------------------------------
        public bool IsAllocated => PackagePtr != IntPtr.Zero;
        public void Destroy()
        {
            if (IsAllocated)
            {
                unsafe
                {
                    if (BlastPackage->info.Allocator != Allocator.Invalid && BlastPackage->info.Allocator != Allocator.None)
                    {
                        UnsafeUtility.Free((void*)PackagePtr, BlastPackage->info.Allocator);
                    }
                    PackagePtr = IntPtr.Zero;
                }
            }

            Inputs = null;
            Outputs = null;
            Variables = null;
            VariableOffsets = null;
        }

        #region ToString + getxxxxText()

        public override string ToString()
        {
            if (IsAllocated)
            {
                unsafe
                {
                    return $"BlastScriptPackage, size = {this.BlastPackage->info.package_size}";
                }
            }
            else
            {
                return $"BlastScriptPackage [not allocated]";
            }
        }

        public string GetCodeSegmentText(int width = 16)
        {
            if (IsAllocated)
            {
                unsafe
                {
                    byte* code = CodeSegment;

                    StringBuilder sb = new StringBuilder();
                    sb.Append($"Code Segment: { BlastPackage->info.code_size} bytes\n\n");
                    sb.Append(BlastUtils.GetByteCodeByteText(code, BlastPackage->info.code_size, width));
                    sb.Append("\n");
                    sb.Append(BlastUtils.GetReadableByteCode(code, BlastPackage->info.code_size));
                    sb.Append("\n");

                    //TextReader tr = new StringReader(Blast.GetHumanReadableCode(byte* code, int size));
                    //sb.Append("\nTranslated Bytecode:\n\n"); 
                    //string scode;
                    //while ((scode = tr.ReadLine()) != null)
                    //{
                    //   sb.Append(scode + "\n");
                    //}
                    return sb.ToString();
                }
            }
            else
            {
                return "BlastScriptPackage.CodeSegment [not allocated]";
            }
        }

        public string GetDataSegmentText()
        {
            if (IsAllocated)
            {

                unsafe
                {
                    StringBuilder sb = new StringBuilder();
                    sb.Append($"Data Segment, data = {BlastPackage->info.data_size} bytes, metadata = {BlastPackage->info.data_sizes_size} bytes \n");

                    float* data_segment = DataSegment;

                    for (int i = 0; i < Variables.Length; i++)
                    {
                        var v = Variables[i];
                        int o = VariableOffsets[i];

                        string name = v.Name;
                        name = char.IsDigit(name[0]) ? "constant" : "name = " + name;

                        string value = string.Empty;

                        if (v.IsVector)
                        {
                            name += $" [{v.VectorSize}]";
                            for (int j = 0; j < v.VectorSize; j++)
                            {
                                value += $"{data_segment[o + j].ToString("R")} ";
                            }
                        }
                        else
                        {
                            value = data_segment[o].ToString("R");
                        }
                        sb.Append($" var {(i + 1).ToString().PadRight(4, ' ')} {name.PadRight(40, ' ')} value = {value.PadRight(32, ' ')} refcount = {(i < Variables.Length ? Variables[i].ReferenceCount.ToString() : "")}\n");
                    }

                    return sb.ToString();
                }
            }
            else
            {
                return "BlastScriptPackage.DataSegment [not allocated]";
            }
        }

        public string GetPackageInfoText()
        {
            if (IsAllocated)
            {
                unsafe
                {
                    StringBuilder sb = new StringBuilder();
                    sb.Append("BlastScriptPackage Information\n");
                    sb.Append($"- size = {this.BlastPackage->info.package_size}\n");
                    sb.Append($"- mode = {this.BlastPackage->info.package_mode}\n");
                    sb.Append($"- memory = {(this.BlastPackage->info.is_fixed_size ? "fixed" : "variable")}\n");

                    sb.Append($"- code size = {this.BlastPackage->info.code_size} bytes\n");
                    sb.Append($"- data size = {this.BlastPackage->info.data_size} bytes, offset = {this.BlastPackage->data_start} bytes\n");
                    sb.Append($"- meta size = {this.BlastPackage->info.data_sizes_size} bytes, offset = {this.BlastPackage->data_sizes_start} bytes\n");

                    int stack_offset = 0;
                    int stack_capacity = 0;

                    switch (this.BlastPackage->info.package_mode)
                    {
                        case BlastPackageMode.CodeDataStack:
                        case BlastPackageMode.Compiler:
                            stack_offset = this.BlastPackage->data_sizes_start + this.BlastPackage->info.data_sizes_size;
                            stack_capacity = BlastPackage->info.package_size - stack_offset;
                            break;

                        default:
                        case BlastPackageMode.Code:
                            stack_offset = 0;
                            stack_capacity = 0;
                            break;
                    }

                    sb.Append($"- stack capacity = {stack_capacity} bytes, offset = {stack_offset} bytes\n\n");

                    return sb.ToString();
                }
            }
            else
            {
                return "BlastScriptPackage [not allocated]";
            }
        }

        #endregion 

        /// <summary>
        /// setup data segment 
        /// - only for packagemode == code 
        /// 
        /// returns bytes used by data and metadata
        /// </summary>
        /// <param name="data"></param>
        unsafe public int SetupDataSegment(byte* data_segment, byte* data, int bytesize, bool zero_variables = true)
        {
            Assert.IsTrue(PackagePtr != IntPtr.Zero);
           
            int i = BlastPackage->info.data_size;
            i += BlastPackage->info.data_sizes_size;
            while (i % 4 != 0) i++;

            if (i > bytesize)
            {
#if UNITY_EDITOR
                Debug.LogError($"SetupDataSegment: size large then destination: {i} < {bytesize}");
#endif
                return -1;
            }
            else
            {
                // setup initial data segment for script 
                UnsafeUtility.MemCpy(data, data_segment, i);

                // zero data possibly from any previous execution   
                if(zero_variables)
                {
                    for(int j = 0; j < Variables.Length; j++)
                    {
                        BlastVariable v = Variables[j]; 
                        if(!v.IsConstant)
                        {
                            int o = VariableOffsets[j];

                            for(int k = 0; k < v.VectorSize; k++)
                            {
                                ((float*)data)[o + k] = 0f; 
                            }
                        }
                    }
                }
                return i;
            }
        }

    }

}