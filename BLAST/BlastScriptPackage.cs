using NSS.Blast.Standalone;
using System;
using System.Text;
using Unity.Assertions;
using Unity.Collections;

namespace NSS.Blast
{

    /// <summary>
    /// Blast Package returned from compiler 
    /// - contains BlastPackage
    /// - contains information about input and outputs (if defined) 
    /// - contains additional variable information (names & offsets)
    /// </summary>
    public class BlastScriptPackage
    {
        public IntPtr PackagePtr;

        /// <summary>
        /// the actual native package 
        /// </summary>
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
                        UnsafeUtils.Free((void*)PackagePtr, BlastPackage->info.Allocator);
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
                    sb.Append(Blast.GetByteCodeByteText(code, BlastPackage->info.code_size, width));
                    sb.Append("\n");
                    sb.Append(Blast.GetReadableByteCode(code, BlastPackage->info.code_size));
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
                Debug.LogError($"SetupDataSegment: size larger then destination: {i} < {bytesize}");
                return (int)BlastError.datasegment_size_larger_then_target;
            }
            else
            {
                // setup initial data segment for script 
                UnsafeUtils.MemCpy(data, data_segment, i);

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