//############################################################################################################################
// BLAST v1.0.4c                                                                                                             #
// Copyright © 2022 Rob Lemmens | NijnStein Software <rob.lemmens.s31 gmail com> All Rights Reserved                   ^__^\ #
// Unauthorized copying of this file, via any medium is strictly prohibited proprietary and confidential               (oo)\ #
//                                                                                                                     (__)  #
//############################################################################################################################

using System;
using Unity.Mathematics;
using UnityEngine.Assertions;

namespace NSS.Blast.Compiler.Stage
{
    /// <summary>
    /// Package Stage: process compiled bytecode into packages 
    /// </summary>
    public class BlastPackaging : IBlastCompilerStage
    {
        /// <summary>
        /// Version 0.1
        /// </summary>
        public Version Version => new Version(0, 1, 0);
        
        /// <summary>
        /// Packaging Stage -> creates package 
        /// </summary>
        public BlastCompilerStageType StageType => BlastCompilerStageType.Packaging;


        /// <summary>
        /// package the compiled code into BlastIntermediate 
        /// 
        /// - offset variable indices so the interpretor doesnt have to lookup offsets in runtime
        /// </summary>
        /// <param name="cdata"></param>
        /// <param name="code">the code to package</param>
        /// <returns>non-zero error code on failure</returns>
        static internal unsafe int PackageIntermediate(CompilationData cdata, IMByteCodeList code)
        {
            // reduce any segmented code list that resulted from multithreading
            if(code != null && code.IsSegmented)
            {
                code.ReduceSegments(); 
            }

            // make sure code is zero terminated, it might not always be possible to know for the compiler when to keep it 
            if (!cdata.code.IsZeroTerminated)
            {
                cdata.code.Add(blast_operation.nop);
            }

            // reset any packaged data already set by overwriting it
            cdata.Executable = new BlastIntermediate();

            // clear out any existing offsets (might have ran multiple times..) 
            // -> CalculateVariableOffsets in Optimizer also touches this 
            cdata.Offsets.Clear();

            // remove any constant cdata refs 
            int iremove = 0;

            while(iremove < cdata.Variables.Count)
            {
                if(cdata.Variables[iremove].IsCData && cdata.Variables[iremove].IsConstant)
                {
                    cdata.Variables.RemoveAt(iremove); 
                }
                else
                {
                    iremove++;  
                }
            }

            // setup data segment 
            byte offset = 0;
            byte[] replace = new byte[cdata.Variables.Count];
            for (int i = 0; i < cdata.Variables.Count; i++)
            {
                BlastVariable v = cdata.Variables[i];

                if (!cdata.IsOK)
                {
                    return (int)BlastError.error;
                }

                if (v.IsCData) // were not precise here, CData might get tagged as vector somewhere we dont care 
                {
                    if (v.IsConstant)
                    {
                        cdata.LogError($"Blast.Packaging: constant CData in variable data, variable: {v.Name}");
                        return (int)BlastError.error;
                    }
                    else
                    {
                        cdata.LogToDo("Blast.Packaging: CData packaging not fully implemented ");

                        // determine datasize in segment 
                        Assert.IsNotNull(v.ConstantData);

                        // NOTE max constant data size = 15 * 4 = 60 bytes when in datasegment 

                        // align to 4 bytes of data use, interpretors can only index every 4th byte
                        v.VectorSize = v.ConstantData.Length;
                        while ((v.VectorSize & 0x00000011) != 0) v.VectorSize++; 

                        if (v.VectorSize > 15)
                        {
                            cdata.LogError($"Blast.Packaging: CData element '{v.Name}' to large too package as variable, maximum size = 15*4-2 = 58 bytes, element = {v.ConstantData.Length} bytes large");
                            return (int)BlastError.error_package_cdata_variable_too_large;
                        }

                        cdata.Executable.SetMetaData(BlastVariableDataType.CData, (byte)v.VectorSize, offset);

                        replace[i] = offset;
                        cdata.Offsets.Add(offset);
                        offset = (byte)(offset + v.VectorSize);
                    }
                }
                else 
                if (v.IsVector)
                {
                    // variable sized float 
                    switch (v.VectorSize)
                    {
                        case 2: // float2
                            cdata.Executable.SetMetaData(v.DataType, 2, offset);
                            break;
                        case 3: // float3
                            cdata.Executable.SetMetaData(v.DataType, 3, offset);
                            break;
                        case 4: // float4 
                            cdata.Executable.SetMetaData(v.DataType, 4, offset);
                            break;
                        default: // float[]
                            cdata.LogError($"compile: only float vectors of size 2 3 and 4 are supported, the script attempted to create a float[{v.VectorSize}]");
                            return (int)BlastError.error;
                    }

                    replace[i] = offset;

                    cdata.Offsets.Add(offset);    // make offsets available to be able to name parameters in debug 
                    offset = (byte)(offset + v.VectorSize);
                }
                else
                {
                    cdata.Offsets.Add(offset);
                    replace[i] = offset;
                    cdata.Executable.SetMetaData(v.DataType, 1, offset);

                    if (v.IsConstant)
                    {
                        switch (v.DataType)
                        {
                            case BlastVariableDataType.Numeric:
                                {
                                    // float of size 1
                                    float f;
                                    if (!float.IsNaN(f = v.Name.AsFloat()))
                                    {
                                        // constant numeric value
                                        cdata.Executable.data[offset++] = f;
                                    }
                                    else
                                    {
                                        cdata.LogError($"packaging: data segment error, failed to parse constant as float {v.Name}");
                                        cdata.Executable.data[offset++] = 0;
                                    }
                                }
                                break;

                            case BlastVariableDataType.Bool32:
                                {
                                    uint b32;
                                    if (CodeUtils.TryAsBool32(v.Name, true, out b32))
                                    {
                                        unsafe 
                                        {
                                            float* f32 = (float*)(void*)&b32;
                                            cdata.Executable.data[offset++] = f32[0]; 
                                        }
                                    }
                                    else
                                    {
                                        cdata.LogError($"packaging: data segment error, failed to parse constant as bool32: {v.Name}");
                                        cdata.Executable.data[offset++] = 0;
                                    }
                                }
                                break;

                            default:
                                cdata.LogError($"packaging: data segment error, variable {v.Name} datatype: {v.DataType} not supported");
                                break;

                        }
                    }
                    else
                    {
                        // identifier 
                        cdata.Executable.data[offset++] = 0;
                    }
                }

            }
            cdata.Executable.data_count = offset;

            // initialize default values set through input|output defines 
            cdata.ResetIntermediateDefaults(); 

            // setup code segment 
            cdata.Executable.code_pointer = 0;
            cdata.Executable.code_size = code.Count;

            // offset variable indices so the interpretor doesnt have to lookup offsets in runtime
            // - jumps hardcode value
            // - sizors of variable param operations also are hardcoded
            int next_is_hardcoded_value = 0;
            bool is_call = false; 

            for (int i = 0; i < code.Count; i++)
            {
                // skip hardcoded values, after call and constant define/reference
                if (i > 0 && next_is_hardcoded_value <= 0)
                {
                    is_call =
                        i > 2
                        &&
                        code[i - 2].op == blast_operation.ex_op
                        &&
                        code[i - 1].code == (byte)extended_blast_operation.call;

                    if (is_call)
                    {
                        next_is_hardcoded_value += 2;// 4;  Using only 2 bytes now... 
                    }
                }

                if (next_is_hardcoded_value > 0)
                {
                    next_is_hardcoded_value--;
                    // dont interpret as a variable, it is hardcoded
                    cdata.Executable.code[i] = code[i].code;
                }
                else
                {
                    blast_operation op = code[i].op;

                    // skip next if extended op 
                    if (op == blast_operation.ex_op
                        ||
                        // or if its the sizor of a function call with variable parameters 
                        // !!!!!
                        // from this it is hardcoded that any extended op function may never have variable parameter length
                        // !! IMPORTANT !!
                        cdata.Blast.Data->IsVariableParamFunction(op))
                    {
                        next_is_hardcoded_value += 1;
                    }
                    else
                    {
                        // hardcode jump offsets 
                        switch (op)
                        {
                            case blast_operation.constant_f1: next_is_hardcoded_value += 4; break;
                            case blast_operation.constant_f1_h: next_is_hardcoded_value += 2; break;
                            case blast_operation.constant_long_ref: next_is_hardcoded_value += 2; break;
                            case blast_operation.constant_short_ref: next_is_hardcoded_value += 1; break;
                            case blast_operation.jnz: next_is_hardcoded_value += 1; break;
                            case blast_operation.jnz_long: next_is_hardcoded_value += 2; break;
                            case blast_operation.jump: next_is_hardcoded_value += 1; break;
                            case blast_operation.jump_back: next_is_hardcoded_value += 1; break;
                            case blast_operation.jz: next_is_hardcoded_value += 1; break;
                            case blast_operation.jz_long: next_is_hardcoded_value += 2; break;
                            case blast_operation.long_jump: next_is_hardcoded_value += 2; break;
                            case blast_operation.index_n: next_is_hardcoded_value += 2; break; 
                            case blast_operation.cdataref: next_is_hardcoded_value += 2; break;
                            case blast_operation.cdata:
                                {
                                    if (i < code.Count - 3)
                                    {
                                        int length = (short)((code[i + 1].code << 8) + code[i + 2].code);
                                        length += 2;
                                        next_is_hardcoded_value += length;
                                    }
                                    else
                                    {
                                        // error 
                                        cdata.LogError("Blast.Packaging: failed to determine cdata length at codepointer {i}");
                                        return (int)BlastError.compile_packaging_error; 
                                    }
                                }
                                break; 
                        }
                    }

                    if (op != blast_operation.ex_op && (byte)op >= BlastCompiler.opt_ident)
                    {
                        // validate parameter offset & index 
                        // - get parameter index from offset location 
                        byte id = (byte)(code[i].code - BlastCompiler.opt_ident);

                        // - its offset should be known 
                        byte offsetindex = (byte)cdata.Offsets.IndexOf(id);
                        if (offsetindex < 0)
                        {
                            cdata.LogError($"compiler.packaging: data segment error, failed to get variable index from offset {id} at codepointer {i}");
                            return (int)BlastError.error_failed_to_translate_offset_into_index;
                        }
                        else
                        {
                            // it should equal the value in replace
                            // - prevent an index out of range error on errors, we better log something related
                            if ((offsetindex >= 0 && offsetindex <= replace.Length) && id != replace[offsetindex])
                            {
                                cdata.LogError($"compiler.packaging: data segment error, failed to get variable index from offset {id} at codepointer {i} and match it to the calculated offset of {replace[offsetindex]}");
                                return (int)BlastError.error_failed_to_translate_offset_into_index;
                            }

                            // cdata.Executable.code[i] = (byte)(replace[ index  ] + BlastCompiler.opt_ident);
                            cdata.Executable.code[i] = code[i].code;
                        }
                    }
                    else
                    {
                        cdata.Executable.code[i] = code[i].code;
                    }
                }
            }
            return (int)(cdata.IsOK ? BlastError.success : BlastError.compile_packaging_error);
        }

        /// <summary>
        /// package bytecode into script package
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public int Execute(IBlastCompilationData data)
        {
            if (!data.IsOK || data.AST == null) return (int)BlastError.error;

            // package & return 
            CompilationData cdata = (CompilationData)data; 
            BlastError res = (BlastError)PackageIntermediate(cdata, cdata.code);

            return (int)res; 
        }
    }
}
