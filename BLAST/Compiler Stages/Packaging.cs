using System;

namespace NSS.Blast.Compiler.Stage
{
    /// <summary>
    /// Package Stage: process compiled bytecode into packages 
    /// </summary>
    public class BlastPackaging : IBlastCompilerStage
    {
        public Version Version => new Version(0, 1, 0);
        public BlastCompilerStageType StageType => BlastCompilerStageType.Packaging;

      

        /// <summary>
        /// package the compiled code into BlastIntermediate 
        /// </summary>
        /// <param name="cdata"></param>
        /// <param name="code">the code to package</param>
        /// <returns>non-zero error code on failure</returns>
        static internal unsafe int PackageIntermediate(CompilationData cdata, IMByteCodeList code)
        {
            // reduce any segmented code list that resulted from multithreading
            if(code != null && code.HasSegments)
            {
                code.ReduceSegments(); 
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
                        if (!float.IsNaN(f = node.AsFloat(v.Name)))
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
                is_call =
                    i > 2
                    &&
                    code[i - 2].op == script_op.ex_op
                    &&
                    code[i - 1].code == (byte)extended_script_op.call;

                if(is_call)
                {
                    next_is_hardcoded_value += 4;
                }

                if (next_is_hardcoded_value > 0)
                {
                    next_is_hardcoded_value--;
                    // dont interpret as a variable, it is hardcoded
                    cdata.Executable.code[i] = code[i].code;
                }
                else
                {
                    script_op op = code[i].op;

                    // skip next if extended op 
                    if (op == script_op.ex_op
                        ||
                        // skip interpreting next value if it is hardcoded
                        Blast.IsJumpOperation(op)
                        ||
                        // or if its the sizor of a function call with variable parameters 
                        // !!!!!
                        // from this it is hardcoded that any extended op function may never have variable parameter length
                        // !! IMPORTANT !!
                        cdata.Blast.IsVariableParamFunction(op))
                    {
                        next_is_hardcoded_value += 1; 
                    }

                    if (op != script_op.ex_op && (byte)op >= BlastCompiler.opt_ident)
                    {
                        cdata.Executable.code[i] = (byte)(replace[(byte)(code[i].code - BlastCompiler.opt_ident)] + BlastCompiler.opt_ident);
                    }
                    else
                    {
                        cdata.Executable.code[i] = code[i].code;
                    }
                }
            }
            return (int)(cdata.IsOK ? BlastError.success : BlastError.error);
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

            if(res == BlastError.success)
            {
                // if packaged into intermediate 

            }


            return (int)res; 
        }
    }
}
