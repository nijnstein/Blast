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

                // if not set as an input; 
                if (!data.HasInput(out_id))
                {
                    int min_ref_count = 2; // needs at least the definition in output and some assignment 
                    if (data.Variables[out_id].ReferenceCount < min_ref_count)
                    {
                        data.LogError($"analyze.scan_parameters: output variable '{data.Variables[out_id]}' not used except in output and/or input, its reference is therefore either undefined or unneeded");
                        return false;
                    }
                }
                else
                {
                    // defined as input, should always be assigned 
                }
            }

            return true;
        }

        int wouldbe_vector_size_of_calculation(node node)
        {
            if (node.ChildCount < 3) return 0;

            return 0;
        }

        void set_function_vector_size(int fsize, node c)
        {
            if (c.function.ReturnsVectorSize == 0)
            {
                c.vector_size = fsize;
            }
            else
            {
                c.vector_size = c.function.ReturnsVectorSize;
            }
            c.is_vector = fsize > 1;
        }


        int wouldbe_vector_size_of_same_sized_elements(node node)
        {
            if (node.type != nodetype.compound) return 0; 
           
            //a = (1 2 3) 
            //a = (1 pop 2)
            //b = ((1 2 3) (pop pop pop) (1 2 3));
            if (node.ChildCount > 0)
            {
                // - each element is either a function, a variable/constant or a pop
                // - all vector sizes of elements must be equal 

                int element_vector_size = 1;
                int element_count = 1; 

                if (node.children[0].type != nodetype.parameter
                    &&
                   node.children[0].type != nodetype.function)
                {
                    return 0;  
                }
                element_vector_size = math.max(1, node.children[0].vector_size); 

                for(int i = 1; i < node.ChildCount; i++)
                {
                    if (math.max(1, node.children[i].vector_size) != element_vector_size
                        ||
                       (node.children[i].type != nodetype.parameter
                        &&
                        node.children[i].type != nodetype.function))
                    {
                        // vector sizes mismatch or incorrect nodetypes to build a vector 
                        return 0; 
                    }
                    element_count++; 
                }

                return element_count * element_vector_size; 
            }
            return 0; 
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

                            if (node.IsNonNestedVectorDefinition(current))
                            {
                                // count children 
                                fsize = current.CountChildType(nodetype.parameter); 
                            }
                            else
                            {
                                foreach (node fc in current.children)
                                {
                                    fsize = math.max(fsize, fc.vector_size);
                                }
                            }
                            current.vector_size = fsize;
                            break;
                    }
                }
                else
                {
                    // current is NOT a vector
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

                                // does compound only contain values or pops?
                                // we could say its a vector then but we should decide at assignment (i think)

                                if (node.IsNonNestedVectorDefinition(current))
                                {
                                    fsize = current.CountChildType(nodetype.parameter);
                                }
                                else
                                {
                                    // update vector size from children 
                                    // 
                                    // TODO -> this assumes that when 2 vectors have a math operation the result is the largest vector... TODO
                                    fsize = 1;
                                    foreach (node fc in current.children)
                                    {
                                        fsize = math.max(fsize, fc.vector_size);
                                    }
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
                                                // expecting (1 2 3) but getting  ( (1 2 3) )  -> dual compound
                                                // we could remove the outer safely here 

                                                //remove dual compounds  TRANSFORM does that

                                                if (!c.is_vector)
                                                {
                                                    int cest = wouldbe_vector_size_of_same_sized_elements(c);
                                                    if (cest > 0) c.SetIsVector(cest, false); 
                                                }
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
                                                    else
                                                    {
                                                        c.vector_size = math.max(1, c.function.ReturnsVectorSize);
                                                        c.is_vector = false; 
                                                    }

                                                }
                                                else
                                                {
                                                    // update vector size from children 
                                                    fsize = 1;
                                                    foreach (node fc in c.children)
                                                    {
                                                        fsize = math.max(fsize, fc.vector_size);
                                                    }

                                                    if (c.function.AcceptsVectorSize == 0 || fsize == c.function.AcceptsVectorSize)
                                                    {
                                                        set_function_vector_size(fsize, c);
                                                    }
                                                    else
                                                    {
                                                        // errorrr 
                                                        data.LogError($"parameter analysis: vectorsize mismatch in parameters of function {c.function.Match} at {c}, function allows vectorsize {c.function.AcceptsVectorSize} but is supplied with parameters of vectorsize {fsize} ");
                                                        return; 
                                                    }
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

                                if (current.type == nodetype.function)
                                {
                                    if (current.function.AcceptsVectorSize == 0 || size == current.function.AcceptsVectorSize)
                                    {
                                        set_function_vector_size(size, current); 
                                    }
                                    else
                                    {
                                        // errorrr 
                                        data.LogError($"parameter analysis: vectorsize mismatch in parameters of function {current.function.Match} at {current}, function allows vectorsize {current.function.AcceptsVectorSize} but is supplied with parameters of vectorsize {size} ");
                                        return;
                                    }
                                }
                                else
                                {
                                    current.is_vector = size > 1 && all_numeric;
                                    current.vector_size = size;

                                    // if not a vector by now, lets see if we build one from components  
                                    if (!current.is_vector && all_numeric)
                                    {
                                        if (current.ChildCount == 1 && current.children[0].type == nodetype.compound)
                                        {
                                            // a = (1 2 3)                             float3       
                                            // b = (1 pop 2)                           float3
                                            // c = ((1 2 3) (pop pop pop) (1 2 3))     float3x3 or float9

                                            // each node must define the same number of elements  -> gets a mess otherwise 

                                            // attempt to get vector size from same sized elemetns in compound 
                                            int i_vectorsize_estimate = wouldbe_vector_size_of_same_sized_elements(current.children[0]);
                                            if (i_vectorsize_estimate <= 0)
                                            {
                                                //attempt to get from a calculus operation sequence: a = (1 2 3) * 4;  vectorsize = 3
                                                i_vectorsize_estimate = wouldbe_vector_size_of_calculation(current.children[0]);
                                            }
                                            if (i_vectorsize_estimate > 0)
                                            {
                                                current.children[0].SetIsVector(i_vectorsize_estimate, true);
                                            }
                                        }
                                    }
                                }
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