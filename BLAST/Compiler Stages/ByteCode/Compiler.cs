using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NSS.Blast.Compiler.Stage
{
    /// <summary>
    /// Blast Compiler Stage 
    /// - compiles the ast into bytecode
    /// </summary>
    public class BlastBytecodeCompiler : IBlastCompilerStage
    {
        const bool compile_nops = true; 


        public Version Version => new Version(0, 1, 0);
        public BlastCompilerStageType StageType => BlastCompilerStageType.Compile;

        /// <summary>
        /// translate a token representing an operation into its matching opcode, logs an error if operation cant be mapped
        /// </summary>
        /// <param name="data"></param>
        /// <param name="token">the token to map</param>
        /// <returns>script opcode</returns>
        static public script_op GetOperationOpCode(IBlastCompilationData data, BlastScriptToken token)
        {
            switch (token)
            {
                case BlastScriptToken.Add: return script_op.add;
                case BlastScriptToken.Substract: return script_op.substract;
                case BlastScriptToken.Divide: return script_op.divide;
                case BlastScriptToken.Multiply: return script_op.multiply;
                case BlastScriptToken.And: return script_op.and;
                case BlastScriptToken.Or: return script_op.or;
                case BlastScriptToken.Not: return script_op.not;
                case BlastScriptToken.Xor: return script_op.xor;
                case BlastScriptToken.Equals: return script_op.equals;
                case BlastScriptToken.NotEquals: return script_op.not_equals;
                case BlastScriptToken.GreaterThen: return script_op.greater;
                case BlastScriptToken.SmallerThen: return script_op.smaller;
                case BlastScriptToken.GreaterThenEquals: return script_op.greater_equals;
                case BlastScriptToken.SmallerThenEquals: return script_op.smaller_equals;

            }
            data.LogError($"GetOperationOp: invalid token for mapping to operation opcode: {token}");
            return script_op.nop;
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
            if (ast_param == null || string.IsNullOrWhiteSpace(ast_param.identifier))
            {
                data.LogError($"CompileParameter: can not compile parameter with unset identifiers, node: <{ast_param}>");
                return false;
            }

            // encode any constant value that is frequently used 
            if (ast_param.constant_op != script_op.nop)
            {
                // just encode the value into the code 
                code.Add(ast_param.constant_op);
                return true;
            }

            if (allow_pop
                &&
                ast_param.type == nodetype.function
                &&
                    (ast_param.function.FunctionId == (int)ReservedScriptFunctionIds.Pop
                    ||
                    ast_param.function.FunctionId == (int)ReservedScriptFunctionIds.Pop2
                    ||
                    ast_param.function.FunctionId == (int)ReservedScriptFunctionIds.Pop3
                    ||
                    ast_param.function.FunctionId == (int)ReservedScriptFunctionIds.Pop4
                ))
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
                    data.LogError($"CompileParameter: node: <{ast_param}>, identifier/variable not connected: '{ast_param.identifier}'");
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
        /// Compile a function and its parameters 
        /// </summary>
        /// <param name="data"></param>
        /// <param name="ast_function">the node/function to compile</param>
        /// <param name="code"></param>
        /// <returns>true on success, false otherwise, errors will be logged in data</returns>
        static public bool CompileFunction(CompilationData data, node ast_function, IMByteCodeList code)
        {
            if (ast_function.function == null)
            {
                data.LogError($"CompileFunction: node <{ast_function}> has no function set");
                return false;
            }

            // skip compilation of debug if not enabled 
            if (!data.CompilerOptions.CompileDebug)
            {
                if (ast_function.function.FunctionId == (int)ReservedScriptFunctionIds.Debug)
                {
                    // debug function: skip
                    return true;
                }
            }

            // special cases: yield and push
            switch (ast_function.function.ScriptOp)            
            {
                case script_op.yield:
                    {
                        code.Add((byte)script_op.yield);
                        if(!CompileParameters(data, ast_function, code, ast_function.children, 0, 1))
                        {
                            return false; 
                        }
                    }
                    return true; 

                case script_op.push:
                case script_op.pushv:
                    {
                        // push each child 
                        foreach (node ast_param in ast_function.children)
                        {
                            switch (ast_param.type)
                            {
                                case nodetype.compound:
                                    {
                                        // push result of compound on stack
                                        code.Add(script_op.push);
                                        code.Add(script_op.begin);

                                        foreach (node ast_child in ast_param.children)
                                        {
                                            if (CompileNode(data, ast_child, code) == null)
                                            {
                                                data.LogError($"CompileFunction: failed to compile {ast_child.type} child node: <{ast_child.parent}>.<{ast_child}>");
                                                return false;
                                            }
                                        }

                                        code.Add(script_op.end);
                                    }
                                    break;

                                default:
                                    {
                                        if (ast_param.is_vector)
                                        {
                                            code.Add((byte)script_op.pushv);
                                            code.Add((byte)ast_param.vector_size);
                                        }
                                        else
                                        {
                                            code.Add((byte)script_op.push);
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
                    return true; 
            }

            // check if the list of children is flat                
            if (node.IsFlatParameterList(ast_function.children))
            {
                bool has_variable_paramcount = ast_function.function.MinParameterCount != ast_function.function.MaxParameterCount;

                if (ast_function.function.ScriptOp != script_op.nop)
                {
                    // function known to blast
                    // encode function op 
                    code.Add(ast_function.function.ScriptOp);

                    if (ast_function.function.ScriptOp == script_op.ex_op)
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

            switch (ast_node.type)
            {
                // something might inject nops, we dont need to keep them (should not .... todo) 
                case nodetype.none:
                    if (ast_node.HasChildNodes)
                    {
                        data.LogError($"CompileNode: encountered [nop] node <{ast_node}> with child nodes => this is not allowed without a valid type");
                        return null;
                    }
                    if (compile_nops) code.Add(script_op.nop);
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
                            case BlastScriptToken.Nop: code.Add(script_op.nop); break;  
                            case BlastScriptToken.Add: code.Add(script_op.add); break; 
                            case BlastScriptToken.Substract: code.Add(script_op.substract); break; 
                            case BlastScriptToken.Divide: code.Add(script_op.divide); break; 
                            case BlastScriptToken.Multiply:code.Add(script_op.multiply); break;
                            case BlastScriptToken.Equals: code.Add(script_op.equals); break;
                            case BlastScriptToken.SmallerThen: code.Add(script_op.smaller); break;
                            case BlastScriptToken.GreaterThen: code.Add(script_op.greater); break; 
                            case BlastScriptToken.SmallerThenEquals: code.Add(script_op.smaller_equals); break;                                
                            case BlastScriptToken.GreaterThenEquals: code.Add(script_op.greater_equals); break; 
                            case BlastScriptToken.NotEquals: code.Add(script_op.not_equals); break; 
                            case BlastScriptToken.And: code.Add(script_op.and); break;
                            case BlastScriptToken.Or: code.Add(script_op.or); break;
                            case BlastScriptToken.Xor: code.Add(script_op.xor); break;
                            case BlastScriptToken.Not: code.Add(script_op.not); break; 
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
                        code.Add(script_op.yield);
                    }
                    else
                    {
                        data.LogWarning("CompileNode: skipped yield opcode, yield is not supported by current compilation options"); 
                    }
                    break;

                // function 
                case nodetype.function:
                    {
                        if(ast_node.function == null)
                        {
                            data.LogError($"CompileNode: encountered function node with no function set, node: <{ast_node}>");
                            return null; 
                        }
                        if (ast_node.parent == null || ast_node.parent.type == nodetype.root)
                        {
                            switch ((ReservedScriptFunctionIds)ast_node.function.FunctionId)
                            {
                                // these procedures are allowed at the root, the rest is not 
                                case ReservedScriptFunctionIds.Push:
                                case ReservedScriptFunctionIds.Yield:
                                case ReservedScriptFunctionIds.Pop:
                                case ReservedScriptFunctionIds.Pop2:
                                case ReservedScriptFunctionIds.Pop3:
                                case ReservedScriptFunctionIds.Pop4:
                                case ReservedScriptFunctionIds.Seed:
                                case ReservedScriptFunctionIds.Debug:
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
                                    data.LogError($"CompileNode: only procedures are allowed to execute at root, no functions, found {(ast_node.function != null ? ast_node.function.Match : "")}");
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
                        code.Add(script_op.jump, IMJumpLabel.Jump(ast_node.identifier));
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
                        data.LogError($"CompileNode: node: <{ast_node}> could not get id for assignee identifier {ast_node.identifier}");
                        return null;
                    }

                    // encode first part of assignment 
                    // [op:assign][var:id+128]
                    code.Add(script_op.assign);

                    // Assingnee -> parameter must be offset in output 

                    // numeric/id value, add its variableId to output code
                    // it should add p_id.Id as its offset into data 
                    if (assignee.Id < 0 || assignee.Id >= data.OffsetCount)
                    {
                        data.LogError($"CompileParameter: node: <{ast_node}>, assignment destination variable not correctly offset: '{assignee}'");
                        break; 
                    }

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
                            if(!data.IsOK)
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
                    code.Add(script_op.nop);
                    break;

                // groups of statements, for compilation they are all the same  
                case nodetype.ifthen:
                case nodetype.ifelse:
                case nodetype.whilecompound:
                case nodetype.condition:
                case nodetype.compound:
                    code.Add(script_op.begin);
                    foreach (node ast_child in ast_node.children)
                    {
                        if(CompileNode(data, ast_child, code) == null)
                        {
                            data.LogError($"CompileNode: failed to compile {ast_node.type} child node: <{ast_node}>.<{ast_child}>");
                            return null;  
                        }
                    }
                    code.Add(script_op.end);
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
                        node n_while_condition = ast_node.get_child(nodetype.condition);
                        if (n_while_condition == null)
                        {
                            data.LogError($"CompileNode: malformed while node: <{ast_node}>, no while condition found");
                            return null;
                        }

                        node n_while_compound = ast_node.get_child(nodetype.whilecompound);
                        if (n_while_compound == null)
                        {
                            data.LogError($"CompileNode: malformed while node: <{ast_node}>, no while compound found");
                            return null;
                        }

                        n_while_condition.EnsureIdentifierIsUniquelySet("whilecond_cset");

                        // while structure:
                        //
                        // [while_depends_on=initializer from for loops]  [condition_label][condition_depends_on] [jz_condition][offset_compound](condition)(compound)[jumpback condition_label][jump_back offset]

                        // compile each initializer node, the while loop may actually be a for loop transformed into a while and
                        // a for loop specifies an initializer 
                        if (ast_node.depends_on != null)
                        {
                            foreach (node initializer in ast_node.depends_on)
                            {
                                if (CompileNode(data, initializer, code) == null)
                                {
                                    data.LogError($"CompileNode: failed to compile initializer for while loop: <{ast_node}>.<{initializer}>");
                                    return null;
                                }
                            }
                        }

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
                        code.Add((byte)script_op.jz, IMJumpLabel.Jump(n_while_compound.identifier));
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
                        code.Add((byte)script_op.jump_back, IMJumpLabel.Jump(n_while_condition.identifier));
                        code.Add((byte)0, IMJumpLabel.Offset(n_while_condition.identifier));

                        // provide a stub for the label to jump to when skipping the while 
                        code.Add((byte)0, IMJumpLabel.Label(n_while_compound.identifier));
                    }
                    break;

                // an if then else statement 
                case nodetype.ifthenelse:
                    {
                        // get relevant nodes 
                        node if_condition = ast_node.get_child(nodetype.condition);
                        node if_then = ast_node.get_child(nodetype.ifthen);
                        node if_else = ast_node.get_child(nodetype.ifelse);

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
                            code.Add(script_op.jz, IMJumpLabel.Jump(if_condition.identifier)); // jump on false to else
                            code.Add(script_op.nop, IMJumpLabel.Offset(if_condition.identifier));

                            CompileNode(data, if_condition, code);
                            CompileNode(data, if_then, code);

                            // insert an else and jump over it by default 
                            code.Add(script_op.jump, IMJumpLabel.Jump(if_else.identifier));
                            code.Add(script_op.nop, IMJumpLabel.Offset(if_else.identifier));

                            // provide a label to jump to on JZ
                            code.Add(script_op.nop, IMJumpLabel.Label(if_condition.identifier));

                            CompileNode(data, if_else, code);

                            // provide label to skip ELSE 
                            code.Add(script_op.nop, IMJumpLabel.Label(if_else.identifier));
                        }
                        else
                        if (if_then != null)
                        {
                            // only THEN 
                            code.Add(script_op.jz, IMJumpLabel.Jump(if_condition.identifier)); // jump on false over then
                            code.Add(script_op.nop, IMJumpLabel.Offset(if_condition.identifier));
                            CompileNode(data, if_condition, code);
                            CompileNode(data, if_then, code);
                            code.Add(script_op.nop, IMJumpLabel.Label(if_condition.identifier));
                        }
                        else
                        {
                            // only ELSE
                            code.Add(script_op.jnz, IMJumpLabel.Jump(if_condition.identifier)); // jump on true over else
                            code.Add(script_op.nop, IMJumpLabel.Offset(if_condition.identifier));
                            CompileNode(data, if_condition, code);
                            CompileNode(data, if_else, code);
                            code.Add(script_op.nop, IMJumpLabel.Label(if_condition.identifier));
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
        /// compile all nodes in the given root node into intermediate bytecode
        /// - if allowed by options, all child nodes will be processed in parallel
        /// </summary>
        /// <param name="data">compiler data</param>
        /// <param name="ast_root">the root node to compile</param>
        /// <returns>a list with bytecode possibly with extra data, hence intermediate bytecode</returns>
        static IMByteCodeList CompileNodes(CompilationData data, node ast_root)
        {
            IMByteCodeList code = new IMByteCodeList();

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
                        code.AddRange(CompileNode(data, ast_node)); // easier debugging 
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
                if (code.HasSegments)
                {
                    // if segmented add the closing nop as a segment 
                    IMByteCodeList segment = new IMByteCodeList();
                    segment.Add((byte)script_op.nop);
                    code.AddSegment(segment); 
                }
                else
                {
                    code.Add((byte)script_op.nop); // this signals end of script for interpretor \
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
