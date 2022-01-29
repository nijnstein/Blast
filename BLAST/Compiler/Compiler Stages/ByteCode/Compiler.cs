using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Mathematics;

namespace NSS.Blast.Compiler.Stage
{
    /// <summary>
    /// Blast Compiler Stage 
    /// - compiles the ast into bytecode
    /// </summary>
    public class BlastBytecodeCompiler : IBlastCompilerStage
    {
        const bool compile_nops = true; 

        /// <summary>
        /// Version 0.1.0 - Initial 
        /// </summary>
        public Version Version => new Version(0, 1, 0);
        
        /// <summary>
        /// Compilation Stage
        /// </summary>
        public BlastCompilerStageType StageType => BlastCompilerStageType.Compile;

        /// <summary>
        /// translate a token representing an operation into its matching opcode, logs an error if operation cant be mapped
        /// </summary>
        /// <param name="data"></param>
        /// <param name="token">the token to map</param>
        /// <returns>script opcode</returns>
        static public blast_operation GetOperationOpCode(IBlastCompilationData data, BlastScriptToken token)
        {
            blast_operation op = Blast.GetBlastOperationFromToken(token);
            if (op == blast_operation.nop)
            {
                data.LogError($"GetOperationOpCode: invalid token for mapping to operation opcode: {token}");
            }
            return op;
        }

        /// <summary>
        /// compile a single parameter node into the code stream
        /// </summary>
        /// <param name="data"></param>
        /// <param name="ast_param">the parameter node</param>
        /// <param name="code">the code to append to</param>
        /// <param name="allow_pop"></param>
        /// <returns>false on failure</returns>
        static public bool CompileParameter(CompilationData data, node ast_param, IMByteCodeList code, bool allow_pop = true)
        {
            if (ast_param.IsOperation)
            {
                // this means we attempt to have an operation sequence in our parameter list 
                data.LogError($"CompileParameter: can not compile an operation as a parameter, node: <{ast_param.parent}>.<{ast_param}>");
                return false;
            }

            if (ast_param == null || (string.IsNullOrWhiteSpace(ast_param.identifier)))//&& !ast_param.IsOperation))
            {
                data.LogError($"CompileParameter: can not compile parameter with unset identifiers, node:  <{ast_param.parent}>.<{ast_param}>");
                return false;
            }

            // encode any constant value that is frequently used 
            if (ast_param.constant_op != blast_operation.nop)
            {
                // just encode the value into the code 
                code.Add(ast_param.constant_op);
                return true;
            }

            if (allow_pop
                &&
                ast_param.type == nodetype.function
                &&
                ast_param.function.FunctionId == (int)ReservedBlastScriptFunctionIds.Pop)
            {
                // must be a pop, parameters cant push 
                code.Add(ast_param.function.ScriptOp);
            }
            else
            {
                BlastVariable p_id = ast_param.variable;

                if (p_id != null)
                {
                    // numeric/id value, add its variableId to output code
                    // it should add p_id.Id as its offset into data 
                    if(p_id.Id < 0 || p_id.Id >= data.OffsetCount)
                    {
                        data.LogError($"CompileParameter: node: <{ast_param}>, identifier/variable not correctly offset: '{ast_param.identifier}'");
                        return false; 
                    }

                    int offset = data.Offsets[p_id.Id];
                    
                    code.Add((byte)(offset + BlastCompiler.opt_ident));
                    // code.Add((byte)(p_id.Id + BlastCompiler.opt_ident));
                }
                else
                {
                    // parameter (it must have been asssinged somewhere before comming here) 
                    data.LogError($"CompileParameter: node: <{ast_param.parent}>.<{ast_param}>, identifier/variable not connected: '{ast_param.identifier}'");
                    return false;
                }
            }

            return true; 
        }

        /// <summary>
        /// compile a list of nodes as a list of parameters
        /// </summary>
        /// <param name="data"></param>
        /// <param name="ast_function">the node with the function</param>
        /// <param name="code">the bytecode to add compiled code to</param>
        /// <param name="parameter_nodes">the parameter list</param>
        /// <param name="min_validate">min parameter count, error will be raised if less</param>
        /// <param name="max_validate">max parameter count, error will be raised if more</param>
        /// <returns></returns>
        static public bool CompileParameters(CompilationData data, node ast_function, IMByteCodeList code, IEnumerable<node> parameter_nodes, int min_validate, int max_validate)
        {
            int c = parameter_nodes == null ? 0 : parameter_nodes.Count();

            if (c == 0)
            {
                // nothing to compile, ok if min_validate == 0 
                if (min_validate == 0)
                {
                    return true;
                }
                else
                {
                    data.LogError($"CompileParameters: failed to compile parameter list, no parameters found for function that needs at least {min_validate} parameters");
                    return false; 
                }
            }

            // verify parameter count 
            if (c < min_validate || c > max_validate)
            {
                data.LogError($"CompileParameters: node <{ast_function}>, parameter count mismatch, expecting {min_validate}-{max_validate} parameters but found {c}");
                return false;
            }

            // verify each node has no children, flatten should have removed any nesting 
            foreach (node n_param in parameter_nodes)
            {
                if (n_param.children.Count > 0)
                {
                    data.LogError($"CompileParameters: failed to compile parameter list, only single values, constants and stack operations are supported, parameter node: <{n_param}> ");
                    return false;
                }
            }

            // compile each parameter into the code list
            foreach(node n_param in parameter_nodes)
            {
                if(!CompileParameter(data, n_param, code))
                {
                    data.LogError($"CompileParameters: failed to compile parameter: <{ast_function}>.<{n_param}>");
                    return false; 
                }
            }

            return true; 
        }

        /// <summary>
        /// Compile a list of parameters as they are typically supplied to a function into a code stream 
        /// - the parameters are all assumed to be simplex
        /// </summary>
        /// <param name="data">compiler data</param>
        /// <param name="ast_function">ast function node</param>
        /// <param name="children">child nodes to render, dont need to be children of the node</param>
        /// <param name="code">the code list to output to</param>
        /// <returns></returns>
        static public bool CompileSimplexFunctionParameterList(CompilationData data, node ast_function, List<node> children, IMByteCodeList code)
        {
            foreach (node ast_param in children)
            {
                if (!CompileParameter(data, ast_param, code))
                {
                    data.LogError($"CompileFunction: failed to compile parameter <{ast_param}> of function <{ast_function}> as part of a vector push");
                    return false;
                }
            }

            return true; 
        }

        /// <summary>
        /// Compile a function and its parameters 
        /// </summary>
        /// <param name="data"></param>
        /// <param name="ast_function">the node/function to compile</param>
        /// <param name="code"></param>
        /// <returns>true on success, false otherwise, errors will be logged in data</returns>
        static public bool CompileFunction(CompilationData data, node ast_function, IMByteCodeList code)
        {
            if (!ast_function.IsFunction)
            {
                data.LogError($"CompileFunction: node <{ast_function}> has no function set");
                return false;
            }

            // skip compilation of debug if not enabled 
            if (!data.CompilerOptions.CompileDebug)
            {
                if (ast_function.function.FunctionId == (int)ReservedBlastScriptFunctionIds.Debug)
                {
                    // debug function: skip
                    return true;
                }
            }

            // special cases: yield and push
            switch (ast_function.function.ScriptOp)
            {
                case blast_operation.yield:
                    {
                        code.Add((byte)blast_operation.yield);
                        if (!CompileParameters(data, ast_function, code, ast_function.children, 0, 1))
                        {
                            return false;
                        }
                    }
                    return true;

                case blast_operation.push:
                case blast_operation.pushv:
                    {
                        // check for direct pushv with n childs 
                        if (ast_function.IsNonNestedVectorDefinition())
                        {
                            code.Add((byte)blast_operation.pushv);
                            code.Add((byte)node.encode44(ast_function, (byte)ast_function.ChildCount));

                            if(!CompileSimplexFunctionParameterList(data, ast_function, ast_function.children, code))
                            {
                                data.LogError($"Compile: error compiling {ast_function.function.ScriptOp} function parameters"); 
                                return false; 
                            }
                        }
                        else
                        {
                            // push each child but first decide if vector or not based on what is pushed 
                            foreach (node ast_param in ast_function.children)
                            {
                                switch (ast_param.type)
                                {
                                    case nodetype.compound:
                                        {
                                            //
                                            // this grows into a vector via get_compound_result in the interpretor 
                                            //
                                            // - for vectors it would be much more performant to use pushV or assignV
                                            // - this should not be called anymore, flatten should have taken it out.. 
                                            //
                                            if (node.IsNonNestedVectorDefinition(ast_param)
                                               &&
                                                ast_param.vector_size > 1)
                                            {
                                                // insert using pushv, besides using 1 less opcode
                                                // it is also more efficient executing 
                                                code.Add(blast_operation.pushv);
                                                code.Add(node.encode44(ast_param));  // (byte)ast_param.vector_size);

                                                if (ast_param.vector_size != 1 && ast_param.vector_size != ast_param.ChildCount)
                                                {
                                                    data.LogError($"CompileFunction: failed to compile {ast_param.type} child node: <{ast_param.parent}>.<{ast_param}>, cannot generate pushv: vectorsize = {ast_param.vector_size}, paramcount = {ast_param.ChildCount}");
                                                    return false;
                                                }

                                                for (int i_param_child = 0; i_param_child < ast_param.ChildCount; i_param_child++)
                                                {
                                                    node ast_child = ast_param.children[i_param_child];
                                                    node parameter_to_compile = ast_child;

                                                    // if the parameter is a compound, we should only compile its result
                                                    // - this only happens for negated values
                                                    if (ast_child.IsCompound
                                                        && ast_child.ChildCount == 2
                                                        && ast_child.children[0].token == BlastScriptToken.Substract)
                                                    {
                                                        // we have an identifier, that should be mapped to a constant
                                                        blast_operation constant = ast_child.children[1].constant_op;

                                                        // - error if not a constant operation value 
                                                        if (constant < blast_operation.pi || constant >= blast_operation.id)
                                                        {
                                                            data.LogError($"CompileFunction: parameter mapping failure, element should map to a constant at: <{ast_param.parent}>.<{ast_param}>.<{ast_child}> but instead has token {constant}");
                                                            return false;
                                                        }

                                                        // get constant value and negate it 
                                                        float f = -Blast.GetConstantValue(constant);

                                                        BlastVariable negated_variable = null;

                                                        // check all constant variables 
                                                        // - these variables are set by constant values in script
                                                        foreach (BlastVariable variable in data.ConstantVariables)
                                                        {
                                                            // does one match our new value? 
                                                            if (variable.VectorSize == 1)
                                                            {
                                                                if (float.TryParse(variable.Name, out float fvar))
                                                                {
                                                                    if (f == fvar ||
                                                                       (
                                                                       f >= (fvar - data.CompilerOptions.ConstantEpsilon)
                                                                       &&
                                                                       f <= (fvar + data.CompilerOptions.ConstantEpsilon)
                                                                       ))
                                                                    {
                                                                        // yes matched
                                                                        negated_variable = variable;
                                                                    }
                                                                }
                                                            }
                                                        }

                                                        if (negated_variable == null)
                                                        {
                                                            // we have to create one
                                                            negated_variable = data.CreateVariable(f.ToString(), false, false);

                                                            // recalculate data-offsets 
                                                            data.CalculateVariableOffsets();
                                                        }

                                                        parameter_to_compile = ast_child.children[1];
                                                        parameter_to_compile.variable = negated_variable;
                                                        parameter_to_compile.identifier = negated_variable.Name;
                                                        parameter_to_compile.constant_op = blast_operation.nop;

                                                        // remove substract operation 
                                                        ast_child.children.RemoveAt(0);

                                                        // reduce compound 
                                                        ast_param.children[i_param_child] = parameter_to_compile;
                                                        parameter_to_compile.parent = ast_param;
                                                    }

                                                    {
                                                        // compile each child as a simple param (variable or constant or pop)
                                                        if (!CompileParameter(data, parameter_to_compile, code, true))
                                                        {
                                                            data.LogError($"CompileFunction: failed to compile {ast_child.type} child node: <{ast_child.parent}>.<{ast_child}> as a [simple] parameter (may only be constant, scriptvar or pop)");
                                                            return false;
                                                        }
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                // push result of compound on stack
                                                code.Add(blast_operation.push);
                                                code.Add(blast_operation.begin);

                                                foreach (node ast_child in ast_param.children)
                                                {
                                                    if (CompileNode(data, ast_child, code) == null)
                                                    {
                                                        data.LogError($"CompileFunction: failed to compile {ast_child.type} child node: <{ast_child.parent}>.<{ast_child}>");
                                                        return false;
                                                    }
                                                }

                                                code.Add(blast_operation.end);
                                            }
                                        }
                                        break;

                                    default:
                                        {
                                            if (ast_param.is_vector)
                                            {
                                                code.Add((byte)blast_operation.pushv);
                                                code.Add((byte)node.encode44(ast_param, 1));
                                            }
                                            else
                                            {
                                                code.Add((byte)blast_operation.push);
                                            }
                                            if (!CompileParameter(data, ast_param, code))
                                            {

                                                data.LogError($"CompileFunction: failed to compile parameter <{ast_param}> of function <{ast_function}>");
                                                return false;
                                            }
                                        }
                                        break;
                                }
                            }
                        }
                    }
                    return true;

                case blast_operation.pushf:
                case blast_operation.pushc:
                    {
                        // pushes either a function or an operation list
                        if (ast_function.ChildCount == 1)
                        {
                            // must be function 
                            if (ast_function.FirstChild.IsFunction)
                            {
                                if (ast_function.FirstChild.function.IsPopVariant)
                                {
                                    // pushing result of pop == same as doing nothing 
                                    // >> this should be considered a bug .. 
#if DEVELOPMENT_BUILD
                                    data.LogWarning($"bug in compilation: <{ast_function}>.<{ast_function.FirstChild}>, tries to push a pop operation, somehow the pop was wrongfully flattened out. Its compilation is skipped");
#endif
                                }
                                else
                                {
                                    code.Add(blast_operation.pushf);
                                    CompileFunction(data, ast_function.FirstChild, code);
                                }
                            }
                            else
                            {
                                data.LogError($"compile: error compiling push[f|c] operation list: <{ast_function}>.<{ast_function.FirstChild}>, a single operation could have been left where it was and should not have been flattened out.");
                                return false;
                            }
                        }
                        else
                        {
                            if (ast_function.IsOperationList())
                            {
                                // is operationlist, pushc expects compound ending on 0|end
                                code.Add(blast_operation.pushc);

                                // should we include resulting vectorsize for interpretor ?? 

                                // should be flat at this point: todo : check this
                                foreach(node n_op in ast_function.children)
                                {
                                    if (n_op.IsOperation)
                                    {
                                        switch (n_op.token)
                                        {
                                            case BlastScriptToken.Nop: code.Add(blast_operation.nop); break;
                                            case BlastScriptToken.Add: code.Add(blast_operation.add); break;
                                            case BlastScriptToken.Substract: code.Add(blast_operation.substract); break;
                                            case BlastScriptToken.Divide: code.Add(blast_operation.divide); break;
                                            case BlastScriptToken.Multiply: code.Add(blast_operation.multiply); break;
                                            case BlastScriptToken.Equals: code.Add(blast_operation.equals); break;
                                            case BlastScriptToken.SmallerThen: code.Add(blast_operation.smaller); break;
                                            case BlastScriptToken.GreaterThen: code.Add(blast_operation.greater); break;
                                            case BlastScriptToken.SmallerThenEquals: code.Add(blast_operation.smaller_equals); break;
                                            case BlastScriptToken.GreaterThenEquals: code.Add(blast_operation.greater_equals); break;
                                            case BlastScriptToken.NotEquals: code.Add(blast_operation.not_equals); break;
                                            case BlastScriptToken.And: code.Add(blast_operation.and); break;
                                            case BlastScriptToken.Or: code.Add(blast_operation.or); break;
                                            case BlastScriptToken.Xor: code.Add(blast_operation.xor); break;
                                            case BlastScriptToken.Not: code.Add(blast_operation.not); break;
                                            default:
                                                data.LogError($"compile: encountered an unsupported operation type, node: <{n_op.parent}><{n_op}>");
                                                return false;
                                        }
                                    }
                                    else
                                    {
                                        if (n_op.IsFunction)
                                        {
                                            if(!CompileFunction(data, n_op, code))
                                            {
                                                data.LogError($"compile: error compiling function in pushc operation list: <{n_op.parent.parent}>.<{n_op.parent}>, function: {n_op}");
                                                return false;
                                            }   
                                        }
                                        else
                                        {
                                            if (!CompileParameter(data, n_op, code, true))
                                            {
                                                data.LogError($"compile: error compiling parameter in pushc operation list: <{n_op.parent.parent}>.<{n_op.parent}>, operation: {n_op}");
                                                return false;
                                            }
                                        }
                                    }
                                }

                                // close with a nop, signal to interpretor its done
                                code.Add((byte)blast_operation.nop); 
                            }
                            else
                            {
                                // error 
                                data.LogError($"compile: error compiling push[f|c] command: <{ast_function}>.<{ast_function.FirstChild}>, pushf can only push an operation list or a function");
                                return false;
                            }
                        }
                    }
                    return true;
            }

            // if its a compound with a flat list also.. 

            bool is_flat_parameter_list = node.IsFlatParameterList(ast_function.children);

            // if not a flat parameter list => attempt to make it flat 
            if (!is_flat_parameter_list)
            {
                // if mostly this is just a compound holding all parameters


                // todo will fail on a vector
                if (ast_function.ChildCount == 1 
                    && ast_function.children[0].type == nodetype.compound 
                    && ast_function.children[0].ChildCount > 0)
                {
                    node[] children = ast_function.children[0].children.ToArray();
                    foreach (node c in children) c.parent = null; 
                    
                    ast_function.children.Clear(); 
                    ast_function.SetChildren(children);
                    is_flat_parameter_list = node.IsFlatParameterList(ast_function.children);
                }
                //
                // TODO 
                // 
                // 
            }
            



            // check if the list of children is flat                
            if (is_flat_parameter_list)
            {
                bool has_variable_paramcount = ast_function.function.MinParameterCount != ast_function.function.MaxParameterCount;

                if (ast_function.function.ScriptOp != blast_operation.nop)
                {
                    // function known to blast
                    // encode function op 
                    code.Add(ast_function.function.ScriptOp);

                    if (ast_function.function.ScriptOp == blast_operation.ex_op)
                    {
                        // if extended then add extended operation 
                        code.Add((byte)ast_function.function.ExtendedScriptOp);
                    }

                    if (ast_function.function.IsExternalCall)
                    {
                        // encode function id (4 byte int)
                        int id = ast_function.function.FunctionId;
                        code.Add((byte)((uint)id >> 24));
                        code.Add((byte)(((uint)id >> 16) & 0b1111_1111));
                        code.Add((byte)(((uint)id >> 8) & 0b1111_1111));
                        code.Add((byte)((uint)id & 0b1111_1111));
                    }
                    else
                    {
                        // no variable parameter counts on external function counts 
                        // also do this for functions supporting vectorsizes > 1
                        if (has_variable_paramcount)
                        {
                            if (ast_function.children.Count > 63)
                            {
                                data.LogError($"CompileFunction: too many parameters, only 63 are max supported but found {ast_function.children.Count}");
                                return false;
                            }

                            // encode vector size in count 
                            // - count never higher then 63
                            // - vectorsize max 15
                            // if 255 then 2 bytes for supporting larger vectors and count?
                            byte opcode = (byte)((ast_function.children.Count << 2) | (byte)ast_function.vector_size & 0b11);
                            code.Add(opcode);
                        }
                        else
                        {
                            // functions like abs sqrt etc have 1 parameter and their outputvectorsize matches inputsize 
                        }
                    }

                    if (ast_function.function.CanHaveParameters)
                    {
                        if (!CompileParameters(data, ast_function, code, ast_function.children, ast_function.function.MinParameterCount, ast_function.function.MaxParameterCount))
                        {
                            data.LogError($"CompileFunction: failed to compile parameters for fuction: <{ast_function}>");
                            return false;
                        }
                    }
                }
                else
                {
                    data.LogError($"CompileFunction; unsupported operation: {ast_function.function.ScriptOp}");
                    return false;
                }
            }
            else
            {
                data.LogError($"CompileFunction: complex children are not allowed in this stage; node <{ast_function}> not flat");
                return false;
            }
            return true;
        }


        /// <summary>
        /// Compile a node into bytecode 
        /// </summary>
        /// <param name="data"></param>
        /// <param name="ast_node">the node to compile, may not be the root ast node</param>
        /// <param name="code">an optional existing codelist to append to</param>
        /// <returns>an intermediate bytecode list</returns>
        static IMByteCodeList CompileNode(CompilationData data, node ast_node, IMByteCodeList code = null)
        {
            if(code == null)
            {
                code = new IMByteCodeList();
            }

            // detect if current node is compound with 1 child only
            // >> this should not happen if the flatten operation went ok, but its fixable be we should log warnings  
            if (ast_node.IsCompound && ast_node.HasOneChild && ast_node.type != nodetype.whilecompound)
            {
                data.LogWarning($"CompileNode: skipping compound of node <{ast_node.parent}>.<{ast_node}>");

                // if so just skip it 
                if (ast_node.HasDependencies)
                {
                    // but keep dependencies 
                    ast_node.children[0].depends_on.InsertRange(0, ast_node.depends_on);
                    data.LogWarning($"CompileNode: skipping compound of node <{ast_node.parent}>.<{ast_node}>, {ast_node.DependencyCount} dependencies moved to <{ast_node.children[0]}>");
                }
                ast_node = ast_node.children[0];
            }


            // compile dependencies 
            if (ast_node.depends_on != null && ast_node.depends_on.Count > 0)
            {
                for (int i = ast_node.depends_on.Count - 1; i >= 0; i--)
                {
                    // insert each node at target parent 
                    node initializer = ast_node.depends_on[i];
                    if (initializer.skip_compilation) continue; 

                    if(Blast.IsError(data.LastError = AnalyzeCompoundNesting(data, initializer)))                        
                    {
                        data.LogError($"CompileNode: failed to compile resolve compound nesting in dependencies for node: <{ast_node}>.<{initializer}>, failed with error {data.LastError}");
                        return null;
                    }
                    if (CompileNode(data, initializer, code) == null)
                    {
                        data.LogError($"CompileNode: failed to compile dependencies for node: <{ast_node}>.<{initializer}>");
                        return null;
                    }
                }
            }

            // further compilation steps depend on the node's type
            switch (ast_node.type)
            {
                // something might inject nops, we dont need to keep them (should not .... todo) 
                case nodetype.none:
                    if (ast_node.HasChildren)
                    {
                        data.LogError($"CompileNode: encountered [nop] node <{ast_node}> with child nodes => this is not allowed without a valid type");
                        return null;
                    }
                    if (compile_nops) code.Add(blast_operation.nop);
                    break;

                // if we get here something is very wrong 
                case nodetype.root:
                    data.LogError($"CompileNode: encountered a node typed as root => this is only allowed for the root of the AST");
                    return null;

                // an operation may be encountered only if != root 
                case nodetype.operation:
                    if (ast_node.parent != null && ast_node.parent.type != nodetype.root)
                    {
                        switch (ast_node.token)
                        {
                            case BlastScriptToken.Nop: code.Add(blast_operation.nop); break;
                            case BlastScriptToken.Add: code.Add(blast_operation.add); break;
                            case BlastScriptToken.Substract: code.Add(blast_operation.substract); break;
                            case BlastScriptToken.Divide: code.Add(blast_operation.divide); break;
                            case BlastScriptToken.Multiply: code.Add(blast_operation.multiply); break;
                            case BlastScriptToken.Equals: code.Add(blast_operation.equals); break;
                            case BlastScriptToken.SmallerThen: code.Add(blast_operation.smaller); break;
                            case BlastScriptToken.GreaterThen: code.Add(blast_operation.greater); break;
                            case BlastScriptToken.SmallerThenEquals: code.Add(blast_operation.smaller_equals); break;
                            case BlastScriptToken.GreaterThenEquals: code.Add(blast_operation.greater_equals); break;
                            case BlastScriptToken.NotEquals: code.Add(blast_operation.not_equals); break;
                            case BlastScriptToken.And: code.Add(blast_operation.and); break;
                            case BlastScriptToken.Or: code.Add(blast_operation.or); break;
                            case BlastScriptToken.Xor: code.Add(blast_operation.xor); break;
                            case BlastScriptToken.Not: code.Add(blast_operation.not); break;
                            default:
                                data.LogError($"CompileNode: encountered an unsupported operation type, node: <{ast_node.parent}><{ast_node}>");
                                return null;
                        }
                    }
                    else
                    {
                        data.LogError($"CompileNode: encountered an unsupported operation type for direct compilation into root, node: <{ast_node}>");
                        return null;
                    }
                    break;

                // unexpected node types: something is wrong 
                default:
                // indices should not be attached to child nodes of an ast node
                case nodetype.index:                // transform should have removed the switch nodes 
                case nodetype.switchnode:
                case nodetype.switchcase:
                case nodetype.switchdefault:
                    data.LogError($"CompileNode: encountered an unsupported node type for direct compilation into root, node: <{ast_node}>");
                    return null;

                // only insert yields if supported by options 
                case nodetype.yield:
                    if (data.CompilerOptions.SupportYield)
                    {
                        if (data.CompilerOptions.PackageStack)
                        {
                            code.Add(blast_operation.yield);
                        }
                        else
                        {
                            data.LogError("CompileNode: skipped yield opcode, yield is not supported by current conflicting compilation options, yield cannot work without packaging a stack");
                        }
                    }
                    else
                    {
                        data.LogWarning("CompileNode: skipped yield opcode, yield is not supported by current compilation options");
                    }
                    break;

                // function 
                case nodetype.function:
                    {
                        if (!ast_node.IsFunction || ast_node.function.FunctionId <= 0)
                        {
                            data.LogError($"CompileNode: encountered function node with no function set, node: <{ast_node}>");
                            return null;
                        }
                        if ((ast_node.parent == null || ast_node.parent.type == nodetype.root) && ast_node.function.ReturnsVectorSize > 0)
                        {
                            switch ((ReservedBlastScriptFunctionIds)ast_node.function.FunctionId)
                            {
                                // these procedures are allowed at the root, the rest is not 
                                case ReservedBlastScriptFunctionIds.Push:
                                case ReservedBlastScriptFunctionIds.PushFunction:
                                case ReservedBlastScriptFunctionIds.PushCompound:
                                case ReservedBlastScriptFunctionIds.PushVector:
                                case ReservedBlastScriptFunctionIds.Yield:
                                case ReservedBlastScriptFunctionIds.Pop:
                                case ReservedBlastScriptFunctionIds.Seed:
                                case ReservedBlastScriptFunctionIds.Debug:
                                    {
                                        if (!CompileFunction(data, ast_node, code))
                                        {
                                            data.LogError($"CompileNode: failed to compile function: {ast_node}.");
                                            return null;
                                        }
                                    }
                                    break;

                                default:
                                    data.LogToDo($"todo: allow functions on root");
                                    data.LogError($"CompileNode: only procedures are allowed to execute at root, no functions, found {ast_node.function.GetFunctionName()}");
                                    return null;
                            }
                        }
                        else
                        {
                            if (!CompileFunction(data, ast_node, code))
                            {
                                data.LogError($"CompileNode: failed to compile function: {ast_node}.");
                                return null;
                            }
                        }
                        break;
                    }

                // unconditional jump 
                case nodetype.jump_to:
                    {
                        code.Add(blast_operation.jump, IMJumpLabel.Jump(ast_node.identifier));
                        code.Add((byte)0, IMJumpLabel.Offset(ast_node.identifier));
                        break;
                    }

                // jump target 
                case nodetype.label:
                    {
                        // add nop with label, in post processing we will remove these
                        code.Add((byte)0, IMJumpLabel.Label(ast_node.identifier));
                        break;
                    }

                case nodetype.assignment:

                    BlastVariable assignee = ast_node.variable;
                    if (assignee == null)
                    {
                        data.LogError($"CompileNode: assignment node: <{ast_node}> could not get id for assignee identifier {ast_node.identifier}");
                        return null;
                    }
                    // numeric/id value, add its variableId to output code
                    // it should add p_id.Id as its offset into data 
                    if (assignee.Id < 0 || assignee.Id >= data.OffsetCount)
                    {
                        data.LogError($"CompileNode: assignment node: <{ast_node}>, assignment destination variable not correctly offset: '{assignee}'");
                        break;
                    }

                    // assigning a single value or pop?  
                    if (ast_node.ChildCount == 1 && node.IsSingleValueOrPop(ast_node.FirstChild))
                    {
                        //
                        // encode assigns instead of assing, this saves a trip through get_compound and 1 byte closing that compound
                        //
                        code.Add(blast_operation.assigns);
                        code.Add((byte)(data.Offsets[assignee.Id] + BlastCompiler.opt_ident)); // nop + 1 for ids on variables 

                        // encode variable, constant value or pop instruction 
                        if (!CompileParameter(data, ast_node.FirstChild, code, true))
                        {
                            data.LogError($"CompileNode: assignment node: <{ast_node.parent}>.<{ast_node}>, failed to compile single parameter <{ast_node.FirstChild}> into assigns operation");
                            return null;
                        }
                    }
                    else
                    // assigning a single function result | and not a stack pusp/pop
                    if (ast_node.ChildCount == 1 && ast_node.FirstChild.type == nodetype.function && !ast_node.IsStackFunction)
                    {
                        code.Add(ast_node.function.IsExternalCall ? blast_operation.assignfe : blast_operation.assignf);
                        code.Add((byte)(data.Offsets[assignee.Id] + BlastCompiler.opt_ident));
                        CompileNode(data, ast_node.FirstChild, code);
                        if (!data.IsOK)
                        {
                            data.LogError($"CompileNode: assignment node: <{ast_node.parent}>.<{ast_node}>, failed to compile assignment of function <{ast_node.FirstChild}> into assignf operation"); 
                            return null;
                        }
                    }
                    else
                    // assigning a negated / notted function result 
                    if (ast_node.ChildCount == 2
                        &&
                        (ast_node.FirstChild.type == nodetype.operation && (ast_node.FirstChild.token == BlastScriptToken.Substract || ast_node.FirstChild.token == BlastScriptToken.Not))
                        &&
                        !ast_node.IsStackFunction                                                    
                        &&
                        ast_node.LastChild.type == nodetype.function)
                    {
                        code.Add(ast_node.function.IsExternalCall ? blast_operation.assignfen : blast_operation.assignfn);
                        code.Add((byte)(data.Offsets[assignee.Id] + BlastCompiler.opt_ident));
                        CompileNode(data, ast_node.FirstChild, code);                                                         
                        {
                            data.LogError($"CompileNode: assignment node: <{ast_node.parent}>.<{ast_node}>, failed to compile negated assignment of function <{ast_node.FirstChild}> into assignf operation");
                            return null;
                        }
                    }
                    else
                    //
                    // assigning a simple vector built from components 
                    // - flatten will have removed all nesting at this point, still check it though
                    //
                    if (ast_node.is_vector && ast_node.IsSimplexVectorDefinition())
                    {

                        code.Add((byte)blast_operation.assignv);
                        code.Add((byte)(data.Offsets[assignee.Id] + BlastCompiler.opt_ident)); // nop + 1 for ids on variables 
                        
                        // assingv deduces vectorsize and parametercount at interpretation and assumes compiler has generated correct code 
                        // code.Add((byte)node.encode44(ast_node, (byte)ast_node.ChildCount));

                        if (!CompileSimplexFunctionParameterList(data, ast_node, ast_node.children, code))
                        {
                            data.LogError($"Compile: error compiling {ast_node.function.ScriptOp} function parameters for node <{ast_node.parent}>.<{ast_node}>");
                            return null;
                        }

                        // 
                        // assignV allows the minus symbol directly in the vector definition..... 
                        // because we flattened that away it cant easily use that feature here 
                        // 
                        // TODO: update flattener, allow it to leave the minus in this case 
                        //       IsSimplexVectorDefinition should then also allow the -compounds 
                        //       CompileSimplexfunctionParameterList must then also write these in only this situatiion 
                        //
                        // 
                        

                    }
                    // default compound assignment 
                    else
                    {
                        // encode first part of assignment 
                        // [op:assign][var:id+128]
                        code.Add(blast_operation.assign);
                        code.Add((byte)(data.Offsets[assignee.Id] + BlastCompiler.opt_ident)); // nop + 1 for ids on variables 

                        // compile child nodes 
                        foreach (node ast_child in ast_node.children)
                        {
                            // make sure only valid node types are supplied 
                            if (ast_child.ValidateType(data, new nodetype[] {
                            nodetype.parameter,
                            nodetype.operation,
                            nodetype.compound,
                            nodetype.function
                        }))
                            {
                                CompileNode(data, ast_child, code);
                                if (!data.IsOK)
                                {
                                    data.LogError($"CompileNode: failed to compile part of assignment: <{ast_node}>");
                                    return null;
                                }
                            }
                        }
                        // end with a nop operation
                        // - signal interpretor that this statement is complete.
                        // - variable length vectors really need either this or a counter... 
                        //   better always have a nop, makes the assembly easier to read from bytes
                        code.Add(blast_operation.nop);
                    }
                    break;

                // groups of statements, for compilation they are all the same  
                case nodetype.ifthen:
                case nodetype.ifelse:
                case nodetype.whilecompound:
                case nodetype.condition:
                case nodetype.compound:
                    code.Add(blast_operation.begin);
                    foreach (node ast_child in ast_node.children)
                    {
                        if(CompileNode(data, ast_child, code) == null)
                        {
                            data.LogError($"CompileNode: failed to compile {ast_node.type} child node: <{ast_node}>.<{ast_child}>");
                            return null;  
                        }
                    }
                    code.Add(blast_operation.end);
                    break;

                // a single parameter 
                case nodetype.parameter:
                    if(!CompileParameter(data, ast_node, code))
                    {
                        data.LogError($"CompileNode: failed to compile parameter node: <{ast_node.parent}>.<{ast_node}>");
                        return null; 
                    }
                    break;

                // a while loop, consisting of a conditional and a compound 
                case nodetype.whileloop:
                    {
                        node n_while_condition = ast_node.GetChild(nodetype.condition);
                        if (n_while_condition == null)
                        {
                            data.LogError($"CompileNode: malformed while node: <{ast_node}>, no while condition found");
                            return null;
                        }

                        node n_while_compound = ast_node.GetChild(nodetype.whilecompound);
                        if (n_while_compound == null)
                        {
                            data.LogError($"CompileNode: malformed while node: <{ast_node}>, no while compound found");
                            return null;
                        }

                        n_while_condition.EnsureIdentifierIsUniquelySet("whilecond_cset");

                        // while structure:
                        //
                        // [while_depends_on=initializer from for loops]  [condition_label][condition_depends_on] [jz_condition][offset_compound](condition)(compound)[jumpback condition_label][jump_back offset]

                        // THIS IS NOW DONE FOR EVERY NODE 

                        // compile each initializer node, the while loop may actually be a for loop transformed into a while and
                        // a for loop specifies an initializer 
                      //  if (ast_node.depends_on != null)
                      //  {
                      //      foreach (node initializer in ast_node.depends_on)
                      //      {
                      //          if (CompileNode(data, initializer, code) == null)
                      //          {
                      //              data.LogError($"CompileNode: failed to compile initializer for while loop: <{ast_node}>.<{initializer}>");
                      //              return null;
                      //          }
                      //      }
                      //  }

                        // add dependencies before while condition (result of flatten), maintain jump label
                        code.Add((byte)0, IMJumpLabel.Label(n_while_condition.identifier));

                        // compile each node the condition depends on 
                        foreach (node ast_condition_child in n_while_condition.depends_on)
                        {
                            if (CompileNode(data, ast_condition_child, code) == null)
                            {
                                data.LogError($"CompileNode: failed to compile child node of while condition: <{n_while_condition}>.<{ast_condition_child}>");
                                return null;
                            }
                        }

                        // start of while loop: jump over while compound when condition zero 
                        code.Add((byte)blast_operation.jz, IMJumpLabel.Jump(n_while_compound.identifier));
                        code.Add((byte)0, IMJumpLabel.Offset(n_while_compound.identifier));

                        // compile the condition 
                        if (CompileNode(data, n_while_condition, code) == null)
                        {
                            data.LogError($"CompileNode: failed to compile while condition: <{n_while_condition}>");
                            return null;
                        }

                        // followed by the compound 
                        if (CompileNode(data, n_while_compound, code) == null)
                        {
                            data.LogError($"CompileNode: failed to compile while compound: <{n_while_compound}>");
                            return null;
                        }
                                                                   
                        // jump back to condition and re evaluate
                        code.Add((byte)blast_operation.jump_back, IMJumpLabel.Jump(n_while_condition.identifier));
                        code.Add((byte)0, IMJumpLabel.Offset(n_while_condition.identifier));

                        // provide a stub for the label to jump to when skipping the while 
                        code.Add((byte)0, IMJumpLabel.Label(n_while_compound.identifier));
                    }
                    break;

                // an if then else statement 
                case nodetype.ifthenelse:
                    {
                        // get relevant nodes 
                        node if_condition = ast_node.GetChild(nodetype.condition);
                        node if_then = ast_node.GetChild(nodetype.ifthen);
                        node if_else = ast_node.GetChild(nodetype.ifelse);

                        if (if_condition == null)
                        {
                            data.LogError($"CompileNode: malformed ifthenelse node <{ast_node}> no conditional found");
                            return null;
                        }

                        if (if_then == null && if_else == null)
                        {
                            data.LogError($"CompileNode: malformed ifthenelse node <{ast_node}> no then or else clause found, need at least one to form a valid statement");
                            return null;
                        }

                        // make sure identifiers are set 
                        if_condition.EnsureIdentifierIsUniquelySet("ifcond_cset"); 
                        if (if_then != null)
                        {
                            if_then.EnsureIdentifierIsUniquelySet("ifthen_cset");
                        }
                        if (if_else != null)
                        {
                            if_then.EnsureIdentifierIsUniquelySet("ifelse_cset"); 
                        }

                        // push result of conditional  (need jump list...)
                        // jump to else or then 
                        // then node -> jump over else 
                        // else node 

                        if (if_else != null && if_then != null)
                        {
                            // both THEN and ELSE
                            // - eval statement, jump over then on JZ
                            code.Add(blast_operation.jz, IMJumpLabel.Jump(if_condition.identifier)); // jump on false to else
                            code.Add(blast_operation.nop, IMJumpLabel.Offset(if_condition.identifier));

                            CompileNode(data, if_condition, code);
                            CompileNode(data, if_then, code);

                            // insert an else and jump over it by default 
                            code.Add(blast_operation.jump, IMJumpLabel.Jump(if_else.identifier));
                            code.Add(blast_operation.nop, IMJumpLabel.Offset(if_else.identifier));

                            // provide a label to jump to on JZ
                            code.Add(blast_operation.nop, IMJumpLabel.Label(if_condition.identifier));

                            CompileNode(data, if_else, code);

                            // provide label to skip ELSE 
                            code.Add(blast_operation.nop, IMJumpLabel.Label(if_else.identifier));
                        }
                        else
                        if (if_then != null)
                        {
                            // only THEN 
                            code.Add(blast_operation.jz, IMJumpLabel.Jump(if_condition.identifier)); // jump on false over then
                            code.Add(blast_operation.nop, IMJumpLabel.Offset(if_condition.identifier));
                            CompileNode(data, if_condition, code);
                            CompileNode(data, if_then, code);
                            code.Add(blast_operation.nop, IMJumpLabel.Label(if_condition.identifier));
                        }
                        else
                        {
                            // only ELSE
                            code.Add(blast_operation.jnz, IMJumpLabel.Jump(if_condition.identifier)); // jump on true over else
                            code.Add(blast_operation.nop, IMJumpLabel.Offset(if_condition.identifier));
                            CompileNode(data, if_condition, code);
                            CompileNode(data, if_else, code);
                            code.Add(blast_operation.nop, IMJumpLabel.Label(if_condition.identifier));
                        }
                        if (!data.IsOK)
                        {
                            data.LogError($"CompileNode: failed to compile ifthenelse node: <{ast_node}>");
                            return null;
                        }
                    }
                    break;

            }


            return code;
        }

        /// <summary>
        /// analyze node tree structure, find any nested compound that shouldnt be 
        /// - fix simple cases
        /// - error on others 
        /// - this 'analysis' must always run independant of analyzer 
        /// </summary>
        /// <param name="data">compiler data</param>
        /// <param name="root">root to start from</param>
        /// <returns>success or error code</returns>
        static BlastError AnalyzeCompoundNesting(CompilationData data, node root)
        {
            // CASE 1 => single compound below assignment 
            //        => result of extra compounds in script or injected push;s 
            //
            //        => THE SAME HAPPENS IN EXTRaCTED PUSH  push ( ( 2 2 2 2 ) *(2 (-2) 2 2 ) )
            // 
            //            root of 1
            //       vector[4]assignment of a
            //          vector[4]compound statement of 3[depends on: vector[4]function push  vector[4]function push]
            //             vector[4]function pop
            //            operation Multiply
            //             vector[4]function pop
            //         /
            //      /
            //   /
            // 
            BlastError case1(node current, bool is_disconnected)
            {
                bool changed = true;
                while (changed)
                {
                    int i = 0;
                    changed = false; 
                    while (i < current.ChildCount || (is_disconnected && i == 0))
                    {
                        node child = is_disconnected ? current : current.children[i]; 
                        if (child.IsAssignment || child.IsPushFunction)
                        {
                            // check for case 1: assignment with useless compound in between 
                            if (child.HasChildren && child.HasOneChild)
                            {
                                node grandchild = child.FirstChild;
                                if (grandchild.IsCompound)
                                {
                                    // we have found a match to our case, reduce the node 
                                    // - any vector / datatype information has already been passed up earlier
                                    // - need to move dependencies 
                                    if (grandchild.HasDependencies)
                                    {
                                        foreach (node d in grandchild.depends_on)
                                        {
                                            child.AppendDependency(d);
                                        }
                                    }
                                    child.children.Clear();
                                    child.children.AddRange(grandchild.children);
                                    foreach (node c in child.children)
                                    {
                                        c.parent = child;
                                    }
                                    grandchild = null;
                                    changed = true;
                                    break;
                                }
                            }
                        }
                        else
                        {
                            if (child.HasChildren)
                            {
                                BlastError res = case1(child, false);
                                if (res == BlastError.yield) changed = true;
                                if (Blast.IsError(res)) return res; 
                            }
                        }

                        // next 
                        i++;
                    }

                    if (changed && (current != root || is_disconnected))
                    {
                        if (is_disconnected) continue;
                        else// propagate up to root if something changed 
                            return BlastError.yield;
                    }
                    if(is_disconnected)
                    {
                        break;
                    }
                }
                return BlastError.success;     
            }

            if (root.IsRoot)
            {
                // run from root 
                BlastError result = case1(root, false);
                if (result != BlastError.success) return result;


            }
            else
            {
                // run from a disconnected node 
                BlastError result = case1(root, true);
                if (result != BlastError.success) return result;

            }

            return BlastError.success; 
        }


        /// <summary>
        /// compile all nodes in the given root node into intermediate bytecode
        /// - if allowed by options, all child nodes will be processed in parallel
        /// </summary>
        /// <param name="data">compiler data</param>
        /// <param name="ast_root">the root node to compile</param>
        /// <returns>a list with bytecode possibly with extra data, hence intermediate bytecode</returns>
        static IMByteCodeList CompileNodes(CompilationData data, node ast_root)
        {
            IMByteCodeList code = new IMByteCodeList();

            if(AnalyzeCompoundNesting(data, ast_root) != 0)
            {
                data.LogError("Failed to compile node list from <{ast_root}>, there are unresolved nested compounds");
                return null; 
            }

            if (data.CompilerOptions.ParallelCompilation)
            {
                IMByteCodeList[] code_for_node = new IMByteCodeList[ast_root.children.Count];
                Parallel.ForEach(ast_root.children,
                    ast_node 
                    =>
                    code_for_node[ast_root.children.IndexOf(ast_node)] = CompileNode(data, ast_node, null)
                );
                foreach (IMByteCodeList l in code_for_node)
                {
                    code.AddSegment(l);
                }
            }
            else
            {
                foreach (node ast_node in ast_root.children)
                {
                    // parse the statement from the token array
                    // CompileNode(result, ast_root, code);      // more efficient 
                    IMByteCodeList bcl = CompileNode(data, ast_node);
                    if (bcl != null)
                    {
                        code.AddRange(bcl); //CompileNode(data, ast_node)); // easier debugging 
                    }
                    else
                    {
                        data.LogError($"Failed to compile node: <{ast_node}>");
                        return code;
                    }
                }
            }
            
            return code; 
        }
      
        /// <summary>
        /// Compile the abstract syntax tree into bytecode
        /// </summary>
        /// <param name="data">compilerdata, among which the AST</param>
        /// <returns>a list of bytecode, null on failure</returns>
        static IMByteCodeList CompileAST(CompilationData data)
        {
            if(data == null || data.AST == null)
            {
                return null; 
            }
            IMByteCodeList code = CompileNodes(data, data.AST);
            if(code != null)
            {
                if (code.IsSegmented)
                {
                    // if segmented add the closing nop as a segment 
                    IMByteCodeList segment = new IMByteCodeList();
                    segment.Add((byte)blast_operation.nop);
                    code.AddSegment(segment); 
                }
                else
                {
                    code.Add((byte)blast_operation.nop); // this signals end of script for interpretor \
                }
            }
            return code; 
        }


        /// <summary>
        /// Execute the compilation stage, prepares bytecode from the AST
        /// </summary>
        /// <param name="data">compiler data</param>
        /// <returns>non zero on error conditions</returns>
        public int Execute(IBlastCompilationData data)
        {
            CompilationData cdata = (CompilationData)data;

            // make sure data offsets are calculated and set in im data. 
            // the packager creates this data but it might not have executed yet depending on configuration 
            cdata.CalculateVariableOffsets(); 

            // compile into bytecode 
            cdata.code = CompileAST(cdata);
            if (cdata.code == null)
            {
                return (int)BlastError.error;
            }

            return data.IsOK ? (int)BlastError.success : (int)BlastError.error;
        }


    }

}
