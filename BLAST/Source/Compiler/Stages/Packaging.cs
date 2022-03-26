//##########################################################################################################
// Copyright © 2022 Rob Lemmens | NijnStein Software <rob.lemmens.s31@gmail.com> All Rights Reserved  ^__^\#
// Unauthorized copying of this file, via any medium is strictly prohibited                           (oo)\#
// Proprietary and confidential                                                                       (__) #
//##########################################################################################################
using System;

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

                if (v.IsVector)
                {
                    // variable sized float 
                    cdata.LogToDo("compile: set constant float vector not implemented ");

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

                    // float of size 1
                    if (v.IsConstant)
                    {
                        float f;
                        if (!float.IsNaN(f = v.Name.AsFloat()))
                        {
                            // constant numeric value
                            cdata.Executable.data[offset++] = f;
                        }
                        else
                        {
                            cdata.LogError($"data segment error, not a float? >> {v.Name}");
                            cdata.Executable.data[offset++] = 0;
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
                        // skip interpreting next value if it is hardcoded
                        Blast.IsJumpOperation(op)
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
                        switch (op)
                        {
                            case blast_operation.constant_f1: next_is_hardcoded_value += 4; break;
                            case blast_operation.constant_f1_h: next_is_hardcoded_value += 2; break;
                            case blast_operation.constant_long_ref: next_is_hardcoded_value += 1; break;
                            case blast_operation.constant_short_ref: next_is_hardcoded_value += 2; break;
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
