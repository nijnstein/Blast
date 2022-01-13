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