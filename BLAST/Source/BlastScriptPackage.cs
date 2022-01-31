
#if STANDALONE_VSBUILD
    using NSS.Blast.Standalone;
    using Unity.Assertions; 
#else
using UnityEngine;
using UnityEngine.Assertions;
#endif

using System;
using System.Text;
using Unity.Collections;
using NSS.Blast.Interpretor;

namespace NSS.Blast
{

    /// <summary>
    /// BlastScriptPackage contains all data needed to use and execute scriptcode, it consists of: 
    /// 
    /// - BlastPackage -> contains the native bytecode 
    /// - Variable information 
    /// - IO mapping 
    /// - Bursted functionpointer mapping 
    /// 
    /// </summary>
    public class BlastScriptPackage
    {
        /// <summary>
        /// A functionpointer to burst transpiled bytecode, possible if the package was known at compile time. 
        /// SSMD mode will still use the bytecode package. 
        /// </summary>
        public IntPtr Bursted;

        /// <summary>
        /// the bytecode package 
        /// </summary>
        public BlastPackageData Package;

        /// <summary>
        /// True if this package has also been burstcompiled and can be executed with a native function pointer 
        /// </summary>
        public bool IsBurstCompiled { get; private set; } = false; 
        
        /// <summary>
        /// defined inputs 
        /// </summary>
        public BlastVariableMapping[] Inputs;

        /// <summary>
        /// defined outputs
        /// </summary>
        public BlastVariableMapping[] Outputs;

        /// <summary>
        /// Variable information
        /// </summary>
        public BlastVariable[] Variables;

        /// <summary>
        /// Offsets for variables in element count into datasegment, an elements is 4 bytes large == 1 float
        /// </summary>
        public byte[] VariableOffsets;

        /// <summary>
        /// Returns true if native memory is allocated for the package 
        /// </summary>
        public bool IsAllocated => Package.IsAllocated;


        /// <summary>
        /// size of package data 
        /// </summary>
        public int PackageSize 
        {
            get
            {
                if (!IsAllocated) return 0; 
                return Package.CodeSize + Package.MetadataSize + Package.AllocatedDataSegmentSize; 
            }
        }

        /// <summary>
        /// packaging mode set by attached package data
        /// </summary>
        public BlastPackageMode PackageMode => Package.PackageMode;


        /// <summary>
        /// package flags as set by attached package
        /// </summary>
        public BlastPackageFlags Flags => Package.Flags; 


        /// <summary>
        /// Destroy any allocated native memory 
        /// </summary>
        public void Destroy()
        {
            if (Package.IsAllocated)
            {
                Package.Free();
#if DEVELOPMENT_BUILD
                Package = default; 
#endif
            }
            Inputs = null;
#pragma warning disable CS0612 // 'BlastScriptPackage.Outputs' is obsolete
            Outputs = null;
#pragma warning restore CS0612 // 'BlastScriptPackage.Outputs' is obsolete
            Variables = null;
            VariableOffsets = null;
            Package = default;
            IsBurstCompiled = false; 
        }

        #region ToString + getxxxxText()

        /// <summary>
        /// ToString overload for more information during debugging
        /// </summary>
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

        /// <summary>
        /// Get a string representation of the bytecode, example output: 
        ///
        /// 000| push compound 1 + 2 nop push function max ^ pop 2 
        /// 010| debug pop nop 
        /// 
        /// 000| 030 085 002 086 000 029 042 009 025 086 
        /// 010| 255 253 025 000 
        ///
        /// </summary>
        /// <param name="width">number of columns to render</param>
        /// <returns>A formatted string</returns>
#pragma warning disable CS1573 // Parameter 'show_index' has no matching param tag in the XML comment for 'BlastScriptPackage.GetCodeSegmentText(int, bool)' (but other parameters do)
        public string GetCodeSegmentText(int width = 16, bool show_index = true)
#pragma warning restore CS1573 // Parameter 'show_index' has no matching param tag in the XML comment for 'BlastScriptPackage.GetCodeSegmentText(int, bool)' (but other parameters do)
        {
            if (IsAllocated)
            {
                unsafe
                {
                    byte* code = Package.Code;

                    StringBuilder sb = new StringBuilder();
                    sb.Append($"Code Segment: { Package.CodeSize} bytes\n\n");
                    sb.Append(GetPackageCodeBytesText(width, show_index) );
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


        /// <summary>
        /// Get a string representation of the datasegement
        /// </summary>
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
        /// get datasegment as 000| 000 000 000 000 
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

        /// <summary>
        /// return overview of package information 
        /// </summary>
        /// <returns></returns>
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



        /// <summary>
        /// execute the script in the given environment with the supplied data
        /// </summary>
        /// <param name="blast">blastengine data</param>
        /// <param name="environment">[optional] pointer to environment data</param>
        /// <param name="caller">[ooptional] caller data</param>
        /// <returns>success if all is ok</returns>
        public BlastError Execute(IntPtr blast, IntPtr environment, IntPtr caller)
        {
            if (blast == IntPtr.Zero) return BlastError.error_blast_not_initialized;
            if (!IsAllocated) return BlastError.error_package_not_allocated;

            switch (Package.PackageMode)
            {
                case BlastPackageMode.Normal:
                     BlastInterpretor blaster = default;
                    blaster.SetPackage(Package);
                    return (BlastError)blaster.Execute(blast, environment, caller);

                case BlastPackageMode.Entity:
                case BlastPackageMode.Compiler:
                case BlastPackageMode.SSMD:
                    return BlastError.error_packagemode_not_supported_for_direct_execution;
            }

            return BlastError.success;
        }

    }
}
