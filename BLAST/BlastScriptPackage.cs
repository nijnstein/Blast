
#if NOT_USING_UNITY
    using NSS.Blast.Standalone;
    using Unity.Assertions; 
#else
using UnityEngine;
using UnityEngine.Assertions;
#endif

using System;
using System.Text;
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
        /// <summary>
        /// the actual native package 
        /// </summary>
        public BlastPackageData Package;

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
        public bool IsAllocated => Package.IsAllocated; 
        public void Destroy()
        {
            if (Package.IsAllocated)
            {
                Package.Free();
#if DEBUG
                Package = default; 
#endif
            }

            Inputs = null;
            Outputs = null;
            Variables = null;
            VariableOffsets = null;
            Package = default; 
        }

        #region ToString + getxxxxText()

        public override string ToString()
        {
            if (IsAllocated)
            {
                unsafe
                {
                    return $"BlastScriptPackage, package size = {Package.CodeSegmentSize + Package.DataSegmentSize}";
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
                    byte* code = Package.Code;

                    StringBuilder sb = new StringBuilder();
                    sb.Append($"Code Segment: { Package.CodeSize} bytes\n\n");
                    sb.Append(Blast.GetByteCodeByteText(code, Package.CodeSize, width));
                    sb.Append("\n");
                    sb.Append(Blast.GetReadableByteCode(code, Package.CodeSize));
                    sb.Append("\n");
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
                    sb.Append($"Data Segment, data = {Package.DataSize } bytes, metadata = {Package.MetadataSize} bytes, stack = {Package.StackSize} bytes \n");

                    float* data_segment = Package.Data;

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


        /// <summary>
        /// get code as 000| 000 000 000 000 
        /// </summary>
        /// <returns></returns>
        public string GetPackageCodeBytesText(int column_count = 8, bool show_index = false)
        {
            if (IntPtr.Zero == Package.DataSegmentPtr) return string.Empty;
            unsafe
            {
                return CodeUtils.GetBytesView((byte*)(void*)Package.Code, Package.DataSegmentSize, column_count, show_index);
            }
        }

        /// <summary>
        /// get data as 000| 000 000 000 000 
        /// </summary>
        /// <returns></returns>
        public unsafe string GetPackageDataBytesText(int column_count = 8, bool show_index = false)
        {
            if (IntPtr.Zero == Package.DataSegmentPtr) return string.Empty;
            unsafe
            {
                return CodeUtils.GetBytesView((byte*)(void*)Package.Data, Package.DataSegmentSize, column_count, show_index);
            }
        }


        public string GetPackageInfoText()
        {
            if (IsAllocated)
            {
                unsafe
                {
                    StringBuilder sb = StringBuilderCache.Acquire(); 
                    sb.Append("BlastScriptPackage Information\n");
                    sb.Append($"- language        = {Package.LanguageVersion.ToString()}\n");
                    sb.Append($"- mode            = {Package.PackageMode}\n");
                    sb.Append($"- flags           = {Package.PackageMode}\n");

                    switch(Package.PackageMode)
                    {
                        case BlastPackageMode.Normal:
                            sb.Append($"- package size    = {Package.CodeSegmentSize + Package.DataSegmentSize} bytes\n");
                            break;

                        default:
                            sb.Append("please implement me TODO");
                            break; 
                    }

                    sb.Append($"- code size       = {Package.CodeSize} bytes\n");
                    sb.Append($"- meta size       = {Package.MetadataSize} bytes\n");
                    sb.Append($"- data size       = {Package.DataSize} bytes\n");
                    sb.Append($"- stack size      = {Package.StackSize} bytes\n");
                    sb.Append($"- stack capacity  = {Package.StackCapacity} elements\n"); 
                    sb.Append($"- stack offset    = {Package.DataSegmentStackOffset}\n\n");

                    return StringBuilderCache.GetStringAndRelease(ref sb);
                }
            }
            else
            {
                return "BlastScriptPackage [not allocated]";
            }
        }

#endregion


    }
}