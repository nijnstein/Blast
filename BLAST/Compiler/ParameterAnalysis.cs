using System.Collections.Generic;
using Unity.Mathematics;

namespace NSS.Blast.Compiler.Stage
{

    /// <summary>
    /// Parameter Analysis
    /// !! all identifiers have been defined by the parser, any undefined should have raised an error 
    /// - determine parameter types (float, vectorsize) (todo future: bool, int, matrix)
    /// - confirm inputs used (warnings if not) and outputs set (not set = error)
    /// - validate correct parameter usage
    /// </summary>
    public class BlastParameterAnalysis : IBlastCompilerStage
    {
        public System.Version Version => new System.Version(0, 1, 0);
        public BlastCompilerStageType StageType => BlastCompilerStageType.ParameterAnalysis;

        bool check_input_output(IBlastCompilationData data)
        {
            int o = 0;

            // - validate alignment on input variable mappings 
            // - make sure no duplicate id is defined 
            for (int i = 0; i < data.Inputs.Count; i++)
            {
                // count and check byte offset 
                if (data.Inputs[i].offset != o)
                {
                    data.LogError($"analyze.parameters: input variables not aligned, ensure alignment to make block copies possible on script initialization.");
                    return false;
                }
                o += data.Inputs[i].bytesize;

                // check for double mapped ids... 
                for (int j = 0; j < data.Inputs.Count; j++)
                {
                    if (j != i && data.Inputs[i].id == data.Inputs[j].id)
                    {
                        data.LogError($"analyze.parameters: input variable {data.Inputs[i].variable.Name} mapped more then once");
                        return false;
                    }
                }

                // warn if not used in script
                if (data.Inputs[i].variable.ReferenceCount == 0)
                {
                    // input only used in input. 
                    data.LogWarning($"analyze.parameters: input variable {data.Inputs[i].variable.Name} is only used in its definition");
                }
            }

            // validate reference counts on outputs, 
            // - an id should never be the first reference (its value would be undefined)
            // - an id should also not be the second reference if its also an input (that would mean the script is wasting time) 
            for (int i = 0; i < data.Outputs.Count; i++)
            {
                int out_id = data.Outputs[i].id;
                bool in_input = false;

                // need to determine if its in input to determine minimal reference count
                for (int j = 0; !in_input && j < data.Inputs.Count; j++)
                {
                    in_input = data.Inputs[j].id == out_id;
                }
                int min_ref_count = in_input ? 2 : 1;

                if (data.Variables[out_id].ReferenceCount <= min_ref_count)
                {
                    data.LogError($"analyze.scan_parameters: output variable '{data.Variables[out_id]}' not used except in output and/or input, its reference is therefore either undefined or unneeded");
                    return false;
                }
            }

            return true;
        }


        /// <summary>
        /// perform a check to see if a node results in a vector
        /// </summary>
        /// <param name="data"></param>
        /// <param name="leaf_node"></param>
        void check_if_vector(IBlastCompilationData data, node leaf_node)
        {
            // should only be called on a leaf node 
            if (leaf_node.children.Count > 0) return;

            // propagate type of paramters that are known
            node current = leaf_node;
            int fsize = 1;
            while (current != null && current.parent != null)
            {
                if (current.is_vector)
                {
                    switch (current.type)
                    {
                        case nodetype.assignment:
                            {
                                BlastVariable v = data.GetVariable(current.identifier);
                                v.IsVector = true;
                                v.VectorSize = current.vector_size;
                            }
                            break;

                        case nodetype.function:

                            switch (current.function.ScriptOp)
                            {
                                case script_op.mina:
                                case script_op.maxa:
                                    // these always return a single value 
                                    current.vector_size = 1;
                                    break;

                                case script_op.pop4:
                                    // these always return a float 4 
                                    current.vector_size = 4; break;

                                default:
                                    {// update vector size from children ( they should equal by now.. )
                                     // same as compound 
                                        fsize = 1;
                                        foreach (node fc in current.children)
                                        {
                                            fsize = math.max(fsize, fc.vector_size);
                                        }
                                        current.vector_size = fsize;
                                    }
                                    break;
                            }

                            break;

                        case nodetype.compound:
                            fsize = 1;
                            foreach (node fc in current.children)
                            {
                                fsize = math.max(fsize, fc.vector_size);
                            }
                            current.vector_size = fsize;
                            break;
                    }
                }
                else
                {
                    switch (current.type)
                    {
                        case nodetype.parameter:
                            {
                                BlastVariable v = data.GetVariable(current.identifier);
                                if (v != null && v.IsVector)
                                {
                                    current.is_vector = true;
                                    current.vector_size = v.VectorSize;
                                }
                            }
                            break;

                        case nodetype.compound:
                            {
                                // - check (pop pop c)
                                //
                                // need way to get pop size by determining last push in parser
                                // setdatasize would work but it would add branches.. 

                                // update vector size from children 
                                fsize = 1;
                                foreach (node fc in current.children)
                                {
                                    fsize = math.max(fsize, fc.vector_size);
                                }
                                current.vector_size = fsize;
                                current.is_vector = current.vector_size > 1;
                            }
                            break;

                        case nodetype.function:
                        case nodetype.assignment:
                            {
                                // all children either operation or vector then also vector
                                int size = 1;
                                bool all_numeric = current.children.Count > 0 ? true : false;
                                foreach (node c in current.children)
                                {
                                    switch (c.type)
                                    {
                                        case nodetype.parameter:
                                            {
                                                BlastVariable v = data.GetVariable(c.identifier);
                                                if (v != null)
                                                {
                                                    size = math.max(size, v.VectorSize);
                                                    all_numeric = true;
                                                }
                                                continue;
                                            }
                                        case nodetype.compound:
                                            {
                                                size = math.max(size, c.vector_size);
                                                continue;
                                            }
                                        case nodetype.function:
                                            {
                                                if (c.children.Count == 0)
                                                {
                                                    if (c.function.IsPopVariant())
                                                    {
                                                        switch ((ReservedScriptFunctionIds)c.function.FunctionId)
                                                        {
                                                            case ReservedScriptFunctionIds.Pop2:
                                                                c.vector_size = 2;
                                                                c.is_vector = true;
                                                                break;
                                                            case ReservedScriptFunctionIds.Pop3:
                                                                c.vector_size = 3;
                                                                c.is_vector = true;
                                                                break;
                                                            case ReservedScriptFunctionIds.Pop4:
                                                                c.vector_size = 4;
                                                                c.is_vector = true;
                                                                break;
                                                        }
                                                    }
                                                }
                                                else
                                                {
                                                    // update vector size from children 
                                                    fsize = 1;
                                                    foreach (node fc in current.children)
                                                    {
                                                        fsize = math.max(fsize, fc.vector_size);
                                                    }

                                                    c.vector_size = fsize;
                                                    c.is_vector = fsize > 1;
                                                }
                                                size = math.max(size, c.vector_size);
                                                all_numeric = true;
                                            }
                                            break;
                                        case nodetype.operation:
                                            {
                                                continue;
                                            }
                                    }
                                }
                                current.is_vector = size > 1 && all_numeric;
                                current.vector_size = size;
                            }
                            break;
                    }
                }
                current = current.parent;
            }
        }


        /// <summary>
        /// the parameter analyzer only looks at the parameters and their usage 
        /// - !! it wont make any node changes !!
        /// - checks vectorsizes
        /// - checks input/output settings 
        /// - 
        /// </summary>
        /// <param name="data">current compilationdata</param>
        /// <returns>0 for success, other exitcode = error</returns>
        public int Execute(IBlastCompilationData data)
        {
            // check each leaf for being a vector and propagate upward 
            List<node> leafs = data.AST.GatherLeafNodes();
            foreach (node leaf in leafs)
            {
                check_if_vector(data, leaf);
            }

            // check defined input / outputs 
            if (!check_input_output(data))
            {
                return (int)BlastError.error;
            }

            data.LogToDo("Validate parameter usage / vector flow");

            return (int)BlastError.success;
        }
    }

}