//############################################################################################################################
//                                                                                                                           #
//  ██████╗ ██╗      █████╗ ███████╗████████╗                           Copyright © 2022 Rob Lemmens | NijnStein Software    #
//  ██╔══██╗██║     ██╔══██╗██╔════╝╚══██╔══╝                                    <rob.lemmens.s31 gmail com>                 #
//  ██████╔╝██║     ███████║███████╗   ██║                                           All Rights Reserved                     #
//  ██╔══██╗██║     ██╔══██║╚════██║   ██║                                                                                   #
//  ██████╔╝███████╗██║  ██║███████║   ██║     V1.0.4e                                                                       #
//  ╚═════╝ ╚══════╝╚═╝  ╚═╝╚══════╝   ╚═╝                                                                                   #
//                                                                                                                           #
//############################################################################################################################
//                                                                                                                     (__)  #
//       Unauthorized copying of this file, via any medium is strictly prohibited proprietary and confidential         (oo)  #
//                                                                                                                     (__)  #
//############################################################################################################################
#pragma warning disable CS1591
#pragma warning disable CS0162

using NSS.Blast.Interpretor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Unity.Mathematics;
using UnityEngine.Assertions;

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

            // if a negation, encode the substraction then the value 
            if(ast_param.IsNegationOfValue(false))
            {
                code.Add(blast_operation.substract);
                return CompileParameter(data, ast_param.LastChild, code, allow_pop); 
            }

            if (ast_param == null || (string.IsNullOrWhiteSpace(ast_param.identifier)))//&& !ast_param.IsOperation))
            {
                data.LogError($"CompileParameter: can not compile parameter with unset identifiers, node:  <{ast_param.parent}>.<{ast_param}>");
                return false;
            }

            // determine if indexed 
            blast_operation indexer = blast_operation.nop; 
            if(ast_param.HasIndexers && BlastTransform.ClassifyIndexer(ast_param, out indexer))
            {
                // this is an indexed constant|variable|anything
                if(!data.CompilerOptions.InlineIndexers || data.CompilerOptions.PackageMode != BlastPackageMode.SSMD)
                {
                    data.LogError($"CompileParameter: cannot inline indexers, only allowed in SSMD packaging mode with 'InlineIndexers' option enabled, node:  <{ast_param.parent}>.<{ast_param}>");
                    return false;
                }


                if (indexer == blast_operation.index_n)
                {
                    // we also need to add the indexed value 
                    node indexnode = ast_param.indexers[1];
                    if (indexnode.is_constant)
                    {
                        switch (indexnode.constant_op)
                        {

                            case blast_operation.value_0: indexer = blast_operation.index_x; break;
                            case blast_operation.value_1: indexer = blast_operation.index_y; break;
                            case blast_operation.value_2: indexer = blast_operation.index_z; break;
                            case blast_operation.value_3: indexer = blast_operation.index_w; break;
                        }
                    }
                }
                code.Add(indexer); 

                if(indexer == blast_operation.index_n)
                {
                    if (!CompileParameter(data, ast_param.indexers[1], code, true))
                    {
                        data.LogError($"CompileParameter: failed to compiled index parameter: <{ast_param}>.<{ast_param.indexers[1]}>");
                        return false;
                    }
                }
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
                    if (p_id.Id < 0 || p_id.Id >= data.OffsetCount)
                    {
                        data.LogError($"CompileParameter: node: <{ast_param}>, identifier/variable not correctly offset: '{ast_param.identifier}'");
                        return false;
                    }

                    int offset = data.Offsets[p_id.Id];

                    // cdata references should point to the code holding the data 
                    if (p_id.IsCData && p_id.IsConstant)
                    {
                        //
                        // encode a cdata reference 
                        // - cdataref is assumed to be a long ref always 
                        // 

                        if(!CompileCDATAREF(data, code, p_id))
                        { 
                            data.LogError($"Blast.Compiler.CompilerParameter: failed to compile reference to CDATA section, the jumplabel could not be located for variable: {p_id.Name} in node: <{ast_param}>");
                            return false; 
                        }
                    }
                    else

                    // deterimine if the constantdata should be inlined, referenced or indexed 
                    if (p_id.IsConstant && data.CompilerOptions.InlineConstantData)
                    {
                        // 
                        // - inline the constant data if this is its first occurance
                        // - reference constant data if this is a repeated occurance
                        // 

                        //
                        // constant references are different from jumplabels in that they can have multiple sources
                        // referencing the same target 
                        // - often this function is called with only a code segment and the search for the label will then fail 
                        // 
                                                
                        if (data.JumpLabels.TryGetValue(IMJumpLabel.ConstantName(p_id.Id.ToString()), out IMJumpLabel label)) // code.TryGetConstantReference(p_id, out IMJumpLabel label))
                        {
                            // reserve 2 bytes to reference the constant when resolving jumps 
                            code.Add((byte)blast_operation.constant_short_ref, IMJumpLabel.ReferenceToConstant(label));
                            code.Add((byte)blast_operation.nop, IMJumpLabel.OffsetToConstant(label));
                            code.Add((byte)blast_operation.nop); // reservation for easier jump resolving 
                        }
                        else
                        {
                            // encode instruction depending on datatype/vectorsize 
                            switch (p_id.DataType)
                            {
                                default:
                                    data.LogError($"CompileParameter: node: <{ast_param.parent}>.<{ast_param}>, inlining of constant '{ast_param.identifier}' not possible: datatype {p_id.DataType} with vectorsize {p_id.VectorSize} is not supported as a constant value", (int)BlastError.error_compile_inlined_constant_datatype_not_supported);
                                    return false;

                                case BlastVariableDataType.Bool32:
                                    // vectorsize MUST be 1 
                                    {
                                        if(p_id.VectorSize != 1) {
                                            data.LogError($"CompileParameter: node: <{ast_param.parent}>.<{ast_param}>, inlining of constant '{ast_param.identifier}' not possible: datatype {p_id.DataType} with vectorsize {p_id.VectorSize} is not supported as a constant value", (int)BlastError.error_compile_inlined_constant_datatype_not_supported);
                                            return false; 
                                        }

                                        //  inline as constant reference to its int32 value backed with float memory
                                        uint ui32;
                                        if (CodeUtils.TryAsBool32(p_id.Name, true, out ui32))
                                        {
                                            unsafe
                                            {
                                                byte* p = (byte*)(void*)&ui32;
                                                EncodeInlinedConstant(data, code, p_id, p);
                                            }
                                        }
                                        else
                                        {
                                            data.LogError($"CompileParameter: node: <{ast_param.parent}>.<{ast_param}>, failed to inline constant {ast_param.identifier} could not parse its value as a bool32");
                                            return false;
                                        }

                                    }
                                    break;


                                case BlastVariableDataType.Numeric:
                                    switch (p_id.VectorSize)
                                    {
                                        case 1:

                                            float f = p_id.Name.AsFloat(); 
                                            unsafe
                                            {
                                                byte* p = (byte*)(void*)&f;
                                                {
                                                    EncodeInlinedConstant(data, code, p_id, p);
                                                }
                                            }
                                            break;

                                        default: 
                                        case 2:
                                        case 3:
                                        case 4:
                                            // no support as of yet, only f1's
                                            data.LogError($"CompileParameter: node: <{ast_param.parent}>.<{ast_param}>, inlining of constant '{ast_param.identifier}' not possible: the datatype {p_id.DataType} with vectorsize {p_id.VectorSize} is not supported as a constant value", (int)BlastError.error_compile_inlined_constant_datatype_not_supported);
                                            break;
                                    }
                                    break;                            
                            }
                        }
                    }                      
                    else
                    {
                        // index the constant data in the datasegment 
                        code.Add((byte)(offset + BlastCompiler.opt_ident));
                    }
                }
                else
                {
                    // indexer if allowed by flatten 
                    if (ast_param.IsIndexFunction && data.CompilerOptions.PackageMode == BlastPackageMode.SSMD)
                    {
                        // inline the indexer 
                        if (!CompileFunction(data, ast_param, code))
                        {
                            data.LogError($"CompileParameter: node: <{ast_param.parent}>.<{ast_param}>, failed to compile inline indexfunction {ast_param.function.GetFunctionName()}");
                            return false;
                        }
                    }
                    else
                    {
                        // parameter (it must have been asssinged somewhere before comming here) 
                        // - did flatten fail and left stuff that cannot be a parameter to a function ? 
                        data.LogError($"CompileParameter: node: <{ast_param.parent}>.<{ast_param}>, identifier/variable not connected: '{ast_param.identifier}'");
                        return false;
                    }
                }
            }

            return true; 
        }

        private static unsafe void EncodeInlinedConstant(CompilationData data, IMByteCodeList code, BlastVariable p_id, byte* p)
        {
            //
            // whole small integers will have the first 2 bytes set to 0 (for float|bool32|uint32)
            // - leverage the space advantage by encoding this in the instruction used to get the constant data value
            //

            if (p[0] == 0 && p[1] == 0)
            {
                // encode it in 2 bytes 
                code.Add((byte)blast_operation.constant_f1_h, data.LinkJumpLabel(IMJumpLabel.Constant(p_id.Id.ToString())));
            }
            else
            {
                // encoding in 4 bytes 
                code.Add((byte)blast_operation.constant_f1, data.LinkJumpLabel(IMJumpLabel.Constant(p_id.Id.ToString())));
                code.Add(p[0]);
                code.Add(p[1]);
            }
            code.Add(p[2]);
            code.Add(p[3]);
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
                    if (!n_param.IsNegationOfValue(false))
                    {
                        data.LogError($"CompileParameters: failed to compile parameter list, only single values, constants and stack operations are supported, parameter node: <{n_param}> ");
                        return false;
                    }
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
                        // check if pushing pop 
                        if(ast_function.HasOneChild && ast_function.FirstChild.IsPopFunction)
                        {
                            // skip it
                            ast_function.skip_compilation = true;
                            return true;                        
                        }

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
                                                        float f = -Blast.GetConstantValueDefault(constant);

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
#if STANDALONE_VSBUILD
                                 ///   data.LogWarning($"bug in compilation: <{ast_function}>.<{ast_function.FirstChild}>, tries to push a pop operation, somehow the pop was wrongfully flattened out. Its compilation is skipped");
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

            bool is_flat_parameter_list = node.IsFlatParameterList(ast_function.children, true);

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
                    is_flat_parameter_list = node.IsFlatParameterList(ast_function.children, true);
                }
                //
                // TODO 
                // 
                // 
            }                                            

            // check if the list of children is flat (allowing negations..)                 
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
                        //code.Add((byte)((uint)id >> 24));
                        //code.Add((byte)(((uint)id >> 16) & 0b1111_1111));
                        code.Add((byte)(((uint)id >> 8) & 0b1111_1111));
                        code.Add((byte)((uint)id & 0b1111_1111));
                    }
                    else
                    {
                        // no variable parameter counts on external function counts 
                        // also do this for functions supporting vectorsizes > 1
                        if (has_variable_paramcount)
                        {
                            // variable parameter functions should have all parameters of equal type 
                            BlastVectorSizes parameter_datatype = VerifyFunctionParameterDataTypes(data, ast_function);
                            if (data.HasErrors) return false;

                            // encode parametercount and vectorsize|datatype in count 
                            byte opcode;
                            switch (ast_function.function.ParameterEncoding)
                            {
                                case BlastParameterEncoding.Encode44:
                                    // encode 44
                                    // 15 parameters, 15 datatypes 
                                    if (ast_function.children.Count > 15)
                                    {
                                        data.LogError($"CompileFunction: too many parameters, only 15 are max supported but found {ast_function.children.Count} in function {ast_function.function.GetFunctionName()} with parameter encoding {ast_function.function.ParameterEncoding} ");
                                        return false;
                                    }
                                    opcode = (byte)((ast_function.children.Count << 4) | (byte)parameter_datatype & 0b1111);
                                    break;

                                case BlastParameterEncoding.Encode62:
                                    // encode 62
                                    // 63 parameters, float1-4 where 0 == float4
                                    if (ast_function.children.Count > 63)
                                    {
                                        data.LogError($"CompileFunction: too many parameters, only 63 are max supported but found {ast_function.children.Count} in function {ast_function.function.GetFunctionName()} with parameter encoding {ast_function.function.ParameterEncoding} ");
                                        return false;
                                    }
                                    opcode = (byte)((ast_function.children.Count << 2) | (byte)ast_function.vector_size & 0b11);
                                    break;

                                default:
                                    {
                                        data.LogError($"CompileFunction: too many parameters in function {ast_function.function.GetFunctionName()}, parameter count {ast_function.children.Count}, invalid parameter encoding type: {ast_function.function.ParameterEncoding}");
                                        return false;
                                    }
                            }       
                            code.Add(opcode);
                        }
                        else
                        {
                            // functions like abs sqrt etc have 1 parameter and their outputvectorsize matches inputsize, unless
                            // does the function specify a fixed output type? 
                            if (ast_function.function.FixedOutputType != BlastVectorSizes.none)
                            {
                                // if assigning then the assignee is checked elsewhere: todo 

                                // if this is reinterpret then check and update datatype on given parameter
                                // 
                                // - during compilation the type of the variable changed.. 
                                // 

                                if (ast_function.function.ExtendedScriptOp == extended_blast_operation.reinterpret_bool32
                                    ||
                                    ast_function.function.ExtendedScriptOp == extended_blast_operation.reinterpret_float)
                                {
                                    // the child MUST be a variable 
                                    if (!ast_function.FirstChild.IsScriptVariable)
                                    {
                                        data.LogError($"CompileFunction: reinterpret_xxxxx can only operate on script variables, node: {ast_function}");
                                        return false;
                                    }

                                    // signal ast that after this the datatype is overriden 
                                    ast_function.FirstChild.variable.DataTypeOverride = ast_function.function.FixedOutputType;
                                    switch (ast_function.function.FixedOutputType)
                                    {
                                        case BlastVectorSizes.bool32: ast_function.datatype = BlastVariableDataType.Bool32; break;
                                        default: ast_function.datatype = BlastVariableDataType.Numeric; break;
                                    }
                                }
                            }
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

        #region CDATA 

        /// <summary>
        /// compile a reference to a cdata section including jump labels 
        /// </summary>
        private static bool CompileCDATAREF(CompilationData data, IMByteCodeList code, BlastVariable variable)
        {
            if (data.JumpLabels.TryGetValue(IMJumpLabel.ConstantName(variable.Id.ToString()), out IMJumpLabel label))
            {
                code.Add(blast_operation.cdataref, IMJumpLabel.ReferenceToConstant(label));
                code.Add((byte)blast_operation.nop, IMJumpLabel.OffsetToConstant(label));
                code.Add((byte)blast_operation.nop); // reservation for easier jump resolving, easier to grow smaller then larger
                return true;
            }
            else
            {
                return false;
            }
        }

        static void WarnUnusedCData(CompilationData data, node n)
        {
            if (n != null && data != null)
            {
                if (data.CompilerOptions.TraceLogging || data.CompilerOptions.VerboseLogging)
                {
                    data.LogWarning($"blast.compiler: skipped compilation of unused cdata node: <{n}>");
                }
            }
        }

        /// <summary>
        /// compile constant cdata nodes into the code stream as:
        /// jump [jump-over-offset] cdata length data |jumplabel|
        /// </summary>
        static BlastError CompileCDataNodes(CompilationData data, node ast_node, IMByteCodeList code)
        {
            Assert.IsNotNull(data);
            Assert.IsNotNull(ast_node);
            Assert.IsNotNull(code);
            
            if(code.Count != 0)
            {
                data.LogError($"CompileCDataNode: failed to compile cdata nodes, the first cdata node should be compiled into an empty code stream", (int)BlastError.error_failed_to_compile_cdata_node);
                return BlastError.error_failed_to_compile_cdata_node;
            }

            if (ast_node.ChildCount > 0)
            {
                //
                // transform will put cdata nodes on the start of the ast 
                // - we want the first instruction to jump over all constant data
                // 

                int constant_cdata_count = 0; 
                int total_cdata_size = 0;
                int not_referenced = 0; 
                foreach(BlastVariable v in data.Variables)
                {
                    if(v.IsCData && v.IsConstant)
                    {
                        if (v.ReferenceCount > 0)
                        {
                            constant_cdata_count++;

                            switch (v.ConstantDataEncoding)
                            {
                                case CDATAEncodingType.u8_fp32: 
                                case CDATAEncodingType.s8_fp32:
                                case CDATAEncodingType.fp8_fp32:
                                case CDATAEncodingType.i8_i32:
                                    total_cdata_size += v.ConstantData.Length / 4; 
                                    break;

                                case CDATAEncodingType.fp16_fp32:
                                case CDATAEncodingType.i16_i32:
                                    total_cdata_size += v.ConstantData.Length / 2; 
                                    break;

                                default:
                                case CDATAEncodingType.None:
                                case CDATAEncodingType.fp32_fp32:
                                case CDATAEncodingType.i32_i32:
                                case CDATAEncodingType.bool32:
                                case CDATAEncodingType.ASCII:
                                case CDATAEncodingType.CDATA:
                                    total_cdata_size += v.ConstantData.Length;  
                                    break; 
                            }


                            total_cdata_size += 3;                       // size of length 16bit == 2 bytes + cdata op
                        }
                        else
                        {
                            not_referenced++; 
                        }
                    }
                }

                if (constant_cdata_count == 0)
                {
                    // skip compilation of unused cdata nodes 
                    foreach (node n in ast_node.GetChildren(nodetype.cdata))
                    {
                        if (n != null)
                        {
                            n.SkipCompilation();
                            WarnUnusedCData(data, n);
                        }
                    }
                    return BlastError.success;
                }
                else
                {
                    // skip only the dereferenced cdata nodes 
                    foreach (node n in ast_node.GetChildren(nodetype.cdata))
                    {
                        if (n != null)
                        {
                            if (n.HasVariable && n.variable.ReferenceCount > 0)
                            {
                                // keep it, it is used 
                            }
                            else
                            {
                                n.SkipCompilation();
                                WarnUnusedCData(data, n);
                            }
                        }
                    }
                }

                // inject the jump
                int jump_size = total_cdata_size + 1; 
                if (jump_size > 255)
                {
                    // encode a long jump, we know whereto so set no label as to not trigger jumpresolving
                    code.Add(blast_operation.long_jump);
                    code.Add((byte)(jump_size >> 8));
                    code.Add((byte)(jump_size & 0b111_11111));
                }
                else
                {
                    // short jump
                    code.Add(blast_operation.jump);
                    code.Add((byte)jump_size);
                }

                // encode the data
                foreach (node n in ast_node.children)
                {
                    if (!n.skip_compilation && n.IsCData)
                    {
                        Assert.IsTrue(n.variable != null && n.variable.IsCData && n.variable.IsConstant && n.variable.ConstantData != null && n.variable.ConstantData.Length > 0);
                        BlastVariable v = n.variable;
                        
                        // only inline the data if its referenced 
                        if (v.ReferenceCount > 0)
                        {
                            // link the cdata as any other constant 
                            IMJumpLabel label = data.LinkJumpLabel(IMJumpLabel.Constant(v.Id.ToString()));

                            // encode [cdata]  ||  strictly, this is not needed but at the cost of a byte we simplify parsing it
                            // as we cant reliably indicate cdata while reading only forward in the interpretor without it
                            if (v.ConstantDataEncoding == CDATAEncodingType.None)
                            {
                                code.Add(blast_operation.cdata, label);
                            }
                            else
                            {
                                // could overload encoding to force short length reference (saves byte.., possibly many if many values)  todo someday
                                code.Add((byte)v.ConstantDataEncoding, label);
                            }



                            // encode the data 
                            // - format depends on encoding 
                            switch (v.ConstantDataEncoding)
                            {
                                // in these encodings data can be directly copied, in others we need top encode it
                                case CDATAEncodingType.None:
                                case CDATAEncodingType.bool32:
                                case CDATAEncodingType.ASCII:
                                case CDATAEncodingType.CDATA:
                                case CDATAEncodingType.fp32_fp32:
                                    // encode data size (in bytes)
                                    code.Add((byte)(v.ConstantData.Length >> 8));
                                    code.Add((byte)(v.ConstantData.Length & 0b1111_1111));

                                    // cdata contains a direct image of the data to write to the codestream (no encoding step)
                                    for (int i = 0; i < v.ConstantData.Length; i++)
                                    {
                                        code.Add(v.ConstantData[i]);
                                    }
                                    break; 

                                case CDATAEncodingType.u8_fp32:
                                    // each float is encoded as 8 bit unsigned 
                                    unsafe 
                                    {
                                        // encode data size (in bytes)  1 byte / value
                                        int l = v.ConstantData.Length / 4; 
                                        code.Add((byte)(l >> 8));        
                                        code.Add((byte)(l & 0b1111_1111));

                                        for (int i = 0; i < v.ConstantData.Length; i += 4)
                                        {
                                            float f = default;
                                            byte* pf = (byte*)(void*)&f;
                                            pf[0] = v.ConstantData[i + 0];
                                            pf[1] = v.ConstantData[i + 1];
                                            pf[2] = v.ConstantData[i + 2];
                                            pf[3] = v.ConstantData[i + 3];
                                            byte b = (byte)math.min(255, math.max(0, f));
                                            code.Add(b); 
                                        }
                                    }
                                    break;


                                case CDATAEncodingType.s8_fp32:
                                    // each float is encoded as 8 bit unsigned 
                                    unsafe
                                    {
                                        // encode data size (in bytes)  1 byte / value
                                        int l = v.ConstantData.Length / 4;
                                        code.Add((byte)(l >> 8));
                                        code.Add((byte)(l & 0b1111_1111));

                                        for (int i = 0; i < v.ConstantData.Length; i += 4)
                                        {
                                            float f = default;
                                            byte* pf = (byte*)(void*)&f;
                                            pf[0] = v.ConstantData[i + 0];
                                            pf[1] = v.ConstantData[i + 1];
                                            pf[2] = v.ConstantData[i + 2];
                                            pf[3] = v.ConstantData[i + 3];
                                            sbyte b = (sbyte)math.min(127, math.max(-128, f));
                                            code.Add( CodeUtils.ReInterpret<sbyte, byte>(b) );
                                        }
                                    }
                                    break; 

                                case CDATAEncodingType.fp8_fp32:
                                    // currently this is just a sbyte div 10
                                    unsafe
                                    {
                                        // encode data size (in bytes)  1 byte / value
                                        int l = v.ConstantData.Length / 4;
                                        code.Add((byte)(l >> 8));
                                        code.Add((byte)(l & 0b1111_1111));

                                        for (int i = 0; i < v.ConstantData.Length; i += 4)
                                        {
                                            float f = default;
                                            byte* pf = (byte*)(void*)&f;
                                            pf[0] = v.ConstantData[i + 0];
                                            pf[1] = v.ConstantData[i + 1];
                                            pf[2] = v.ConstantData[i + 2];
                                            pf[3] = v.ConstantData[i + 3];
                                            // multiply f by 10, giving a range of -12.7 to 12.8
                                            sbyte b = (sbyte)math.min(127f, math.max(-128f, f * 10f));
                                            code.Add(CodeUtils.ReInterpret<sbyte, byte>(b));
                                        }
                                    }
                                    break;
                                    

                                case CDATAEncodingType.fp16_fp32:
                                    // encode as half 
                                    unsafe
                                    {
                                        // encode data size (in bytes)  1 byte / value
                                        int l = v.ConstantData.Length / 2;
                                        code.Add((byte)(l >> 8));
                                        code.Add((byte)(l & 0b1111_1111));

                                        for (int i = 0; i < v.ConstantData.Length; i += 4)
                                        {
                                            float f = default;
                                            byte* pf = (byte*)(void*)&f;
                                            pf[0] = v.ConstantData[i + 0];
                                            pf[1] = v.ConstantData[i + 1];
                                            pf[2] = v.ConstantData[i + 2];
                                            pf[3] = v.ConstantData[i + 3];

                                            // convert to half
                                            half h = new half(f); 
                                            pf = (byte*)(void*)&h;

                                            // write its bytes
                                            code.Add(pf[0]);
                                            code.Add(pf[1]);
                                        }
                                    }
                                    break;

                                case CDATAEncodingType.i8_i32:
                                    unsafe
                                    {
                                        // encode data size (in bytes)  1 byte / value
                                        int l = v.ConstantData.Length / 4;
                                        code.Add((byte)(l >> 8));
                                        code.Add((byte)(l & 0b1111_1111));

                                        for (int i = 0; i < v.ConstantData.Length; i += 4)
                                        {
                                            int i32 = default;
                                            byte* pf = (byte*)(void*)&i32;
                                            pf[0] = v.ConstantData[i + 0];
                                            pf[1] = v.ConstantData[i + 1];
                                            pf[2] = v.ConstantData[i + 2];
                                            pf[3] = v.ConstantData[i + 3];
                                            sbyte b = (sbyte)math.min(127, math.max(-128, i32));
                                            code.Add(CodeUtils.ReInterpret<sbyte, byte>(b));
                                        }
                                    }
                                    break;

                                case CDATAEncodingType.i16_i32:
                                    unsafe
                                    {
                                        // encode data size (in bytes)  1 byte / value
                                        int l = v.ConstantData.Length / 2;
                                        code.Add((byte)(l >> 8));
                                        code.Add((byte)(l & 0b1111_1111));

                                        for (int i = 0; i < v.ConstantData.Length; i += 4)
                                        {
                                            int i32 = default; 
                                            byte* pf = (byte*)(void*)&i32;
                                            pf[0] = v.ConstantData[i + 0];
                                            pf[1] = v.ConstantData[i + 1];
                                            pf[2] = v.ConstantData[i + 2];
                                            pf[3] = v.ConstantData[i + 3];
                                            short s = (short)math.min(short.MaxValue, math.max(short.MinValue, i32));
                                            pf = (byte*)(void*)&s;
                                            code.Add(pf[0]);
                                            code.Add(pf[1]);
                                        }
                                    }
                                    break;

                                case CDATAEncodingType.i32_i32:
                                    unsafe
                                    {
                                        // encode data size (in bytes)  4 byte / value
                                        int l = v.ConstantData.Length;
                                        code.Add((byte)(l >> 8));
                                        code.Add((byte)(l & 0b1111_1111));
                                        for (int i = 0; i < v.ConstantData.Length; i += 4)
                                        {
                                            code.Add(v.ConstantData[i + 0]);
                                            code.Add(v.ConstantData[i + 1]);
                                            code.Add(v.ConstantData[i + 2]);
                                            code.Add(v.ConstantData[i + 3]);
                                        }
                                    }
                                    break;

                            }
                        }

                        // skip the node in further steps 
                        n.SkipCompilation();
                    }
                }
            }

            return BlastError.success;
        }

        #endregion



        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        /// <param name="ast_function"></param>
        /// <param name="verify_size"></param>
        /// <param name="verify_type"></param>
        /// <returns></returns>
        private static BlastVectorSizes VerifyFunctionParameterDataTypes(CompilationData data, node ast_function, bool verify_size = true, bool verify_type = true)
        {
            // when vectorsize == 0, there was no reason to touch it and the actual size is 1
            int vector_size = math.select(ast_function.vector_size, 1, ast_function.vector_size == 0);
            BlastVariableDataType datatype = ast_function.datatype;
 
            if(ast_function.function.ReturnsVectorSize > 0)
            {
                // the function returns a fixed output regardless the input size
                if(ast_function.function.ReturnsVectorSize != vector_size)
                {
                    // its a big fat error if the size doesnt match 
                    data.LogError($"BlastCompiler.VerifyFunctionParameterDataTypes: vectorsize error, function {ast_function.function.GetFunctionName()} has a fixed return vectorsize of {ast_function.function.ReturnsVectorSize} but the astnode specifies {vector_size}");
                    return BlastVectorSizes.none;
                }

                // the parameter size is NOT related to the output vectorsize 
                vector_size = -1; 
            }

            // a component instruction is an instruction that takes several vectors and returns a result based on operations on each component as a single item
            // - examples are csum, maxa, mina
            bool is_component_instruction = ast_function.function.ReturnsVectorSize == 1 && ast_function.function.AcceptsVectorSize == 0;

            for (int i = 0; i < ast_function.ChildCount; i++)
            {
                node child = ast_function.children[i];

                int child_size = math.select(child.vector_size, 1, child.vector_size == 0);

                if (is_component_instruction)
                {
                    // we only care about the max sized parameter if this is a component instruction 
                    vector_size = math.max(vector_size, child_size); 
                }
                else
                {
                    // if the function has a fixed output size the variablesize is not related to it 
                    // so we set from the first parameter
                    if (vector_size == -1)
                    {
                        vector_size = child_size;
                    }
                    if (verify_size && child_size != vector_size)
                    {
                        data.LogError($"BlastCompiler.VerifyFunctionParameterDataTypes: vectorsize mismatch, vectorsize should be equal for all parameters to function {ast_function.function.GetFunctionName()}");
                        return BlastVectorSizes.none;
                    }
                }

                BlastVariableDataType type = child.datatype;

                if (child.variable != null && child.variable.DataTypeOverride != BlastVectorSizes.none)
                {
                    switch (child.variable.DataTypeOverride)
                    {
                        case BlastVectorSizes.bool32: type = BlastVariableDataType.Bool32; break;
                        default: type = BlastVariableDataType.Numeric; break;
                    }

                    if (ast_function.function.ReturnsVectorSize == 0)
                    {
                        datatype = type;
                    }
                }

                if (verify_type && datatype != type)
                {
                    data.LogError($"BlastCompiler.VerifyFunctionParameterDataTypes: datatype mismatch, datatype should be equal for all parameters to function {ast_function.function.GetFunctionName()}");
                    return BlastVectorSizes.none;
                }
            }

            switch(datatype)
            {
                case BlastVariableDataType.Numeric:
                    {
                        switch (vector_size)
                        {
                            case 1: return BlastVectorSizes.float1;
                            case 2: return BlastVectorSizes.float2;
                            case 3: return BlastVectorSizes.float3;
                            case 4: return BlastVectorSizes.float4;
                            default:
                                data.LogError($"BlastCompiler.VerifyFunctionParameterDataTypes: datatype {datatype} with size {vector_size} not supported in function {ast_function.function.GetFunctionName()}");
                                return BlastVectorSizes.none;
                        }
                    }

                case BlastVariableDataType.Bool32:
                    {
                        if (vector_size != 1) 
                        {
                            data.LogError($"BlastCompiler.VerifyFunctionParameterDataTypes: datatype {datatype} with size {vector_size} not supported in function {ast_function.function.GetFunctionName()}");
                            return BlastVectorSizes.none;
                        }
                        return BlastVectorSizes.bool32;
                    }


                default:
                    data.LogError($"BlastCompiler.VerifyFunctionParameterDataTypes: datatype {datatype} not supported in function {ast_function.function.GetFunctionName()}");
                    return BlastVectorSizes.none;
            }
        }

        #region Assignment Node 

        private static IMByteCodeList CompileAssignmentNode(CompilationData data, node ast_node, IMByteCodeList code)
        {
            Assert.IsNotNull(ast_node);
            Assert.IsNotNull(data);
            Assert.IsNotNull(code); 

            BlastVariable assignee = ast_node.variable;
            if (assignee == null)
            {
                data.LogError($"Blast.CompileAssignmentNode: node: <{ast_node}> could not get id for assignee identifier {ast_node.identifier}");
                return null;
            }
            // numeric/id value, add its variableId to output code
            // it should add p_id.Id as its offset into data 
            if (assignee.Id < 0 || assignee.Id >= data.OffsetCount)
            {
                data.LogError($"Blast.CompileAssignmentNode: node: <{ast_node}>, assignment destination variable not correctly offset: '{assignee}'");
                return null;
            }

            // start out with assuming matching datatype
            BlastVariableDataType assigned_datatype = ast_node.variable.DataType;

            // if the assignment is indexed we have to add extra opcodes
            bool is_indexed_assignment = ast_node.HasIndexers;
            blast_operation op_indexer = blast_operation.nop;

            if (is_indexed_assignment)
            {
                // first node should contain the indexer operation
                op_indexer = ast_node.indexers[0].constant_op;
                if (op_indexer == blast_operation.nop)
                {
                    data.LogError($"Blast.CompileAssignmentNode: node: <{ast_node}>, assignment destination variable not correctly indexed: '{assignee}'");
                    return null;
                }
            }

            // constant cdata assignment 
            bool is_constant_cdata_assignment = assignee.IsCData && assignee.IsConstant;

            //
            // in _all_ (other :_) cases the first byte after assignment is opt_ident or higher
            // the index-x y z w n operations all have a lower byte value that the interpretor can check 
            //

            // the fastest case: assign a constant to a dataindex, possibly negated 
            // only for NORMAL
            // a = 1
            // a = -1
            // a = (-1)
            if (IsDirectConstantAssignmentForNormalPackager(data, ast_node, is_constant_cdata_assignment, is_indexed_assignment))
            {
                // encode the constant first, then the ID of the data element followed by a possible negation 
                // - this will provide the normal interpretor with a fast lane setting constants
                // - this also sets a vectors components to equal values (zero not really needed for normal interpretor with this)
                // - this is faster then assignV but less general, assign v should also compile this pattern if:
                // => the vector assigned has the same constant value operation (only ops) for each component 
                // => the element assigned is in the datasegment 
                CompileDirectConstantAssignmentForNormalPackager(ast_node, code, assignee);
            }
            else
            if (IsDirectIDAssignmentForNormalPackager(data, ast_node, is_constant_cdata_assignment, is_indexed_assignment))
            {
                // same story as with the constant, only now we encode assigning a non-indexed datapoint with another
                // - constant initializing in normal happens from constant data in the datasegment (the data has to come from somewere on reset)
                CompileDirectIDAssignmentForNormalPackager(ast_node, code, assignee);
            }
            else
            //************************************************************************************************************ 
            //************************************************************************************************************ 
            //************************************************************************************************************ 
            //************************************************************************************************************ 
            //
            //
            //  some opcodes are stil free to use, the direct assignment of a function 
            //  which has its id in blast_operation is another optimization in flow 
            //  - it will also result in -every bytevalue- being used in the root, this will save yet another byte in the output
            //
            //  - note: be sure to not conflict ids, only functions defined in blast_operation can use this encoding 
            //  -       this is why the most important most used function should have a non-extended opcode 
            //
            //
            //   
            // 
            //************************************************************************************************************ 
            //************************************************************************************************************ 
            //************************************************************************************************************ 
            //************************************************************************************************************ 

            // assigning a single value or pop?  
            if (ast_node.ChildCount == 1 && node.IsSingleValueOrPop(ast_node.FirstChild))
            {
                // ASSIGNS | op.sets 
                // encode assigns instead of assign, this saves a trip through get_compound and 1 byte closing that compound

                if (CompileIndexedAssignmentTarget(data, ast_node, assignee, code, blast_operation.assigns, is_indexed_assignment, op_indexer, is_constant_cdata_assignment) == null)
                {
                    return null;
                }


                // encode variable, constant value or pop instruction 
                if (!CompileParameter(data, ast_node.FirstChild, code, true))
                {
                    data.LogError($"Blast.CompileAssignmentNode: node: <{ast_node.parent}>.<{ast_node}>, failed to compile single parameter <{ast_node.FirstChild}> into assigns operation");
                    return null;
                }

                // if its a Bool32 then the node has this information and as we compiled only a single 
                // node we can be sure we set this datatype 
                assigned_datatype = ast_node.FirstChild.datatype;
            }
            else
            // assigning a single function result | and not a stack pusp/pop
            if (ast_node.ChildCount == 1 && ast_node.FirstChild.type == nodetype.function && !ast_node.IsStackFunction)
            {
                if (CompileIndexedAssignmentTarget(data, ast_node, assignee, code, ast_node.function.IsExternalCall ? blast_operation.assignfe : blast_operation.assignf, is_indexed_assignment, op_indexer, is_constant_cdata_assignment) == null)
                {
                    return null;
                }

                CompileNode(data, ast_node.FirstChild, code);
                if (!data.IsOK)
                {
                    data.LogError($"Blast.CompileAssignmentNode: node: <{ast_node.parent}>.<{ast_node}>, failed to compile assignment of function <{ast_node.FirstChild}> into assignf operation");
                    return null;
                }
            }
            else
            // assigning a negated function result 
            if (ast_node.ChildCount == 2
                &&
                (ast_node.FirstChild.type == nodetype.operation && (ast_node.FirstChild.token == BlastScriptToken.Substract))
                &&
                !ast_node.IsStackFunction
                &&
                ast_node.LastChild.type == nodetype.function)
            {
                if (CompileIndexedAssignmentTarget(data, ast_node, assignee, code, ast_node.function.IsExternalCall ? blast_operation.assignfen : blast_operation.assignfn, is_indexed_assignment, op_indexer, is_constant_cdata_assignment) == null)
                {
                    return null;
                }

                CompileNode(data, ast_node.FirstChild, code);
                {
                    data.LogError($"Blast.CompileAssignmentNode: node: <{ast_node.parent}>.<{ast_node}>, failed to compile negated assignment of function <{ast_node.FirstChild}> into assignf[e]n operation");
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
                // this should never be indexed 
                if (is_indexed_assignment)
                {
                    data.LogError($"Blast.CompileAssignmentNode: error compiling node <{ast_node.parent}>.<{ast_node}>, cannot set a component index with a vector", (int)BlastError.error_assign_component_from_vector);
                    return null;
                }

                //if (IsDirectSingleConstantVectorAssignmentForNormalPackager(data, ast_node, is_constant_cdata_assignment))
                //{
                // assignment of 1 constant to vector in data[index]
                // the interpretor will get metadata on data[index] and knows to assign a vector
                // if there is 1 constant to set, equal for each vector component then we use the same construct as in directconstant assignments 
                //}
                //else
                {
                    if (CompileIndexedAssignmentTarget(data, ast_node, assignee, code, blast_operation.assignv, is_indexed_assignment, op_indexer, is_constant_cdata_assignment) == null)
                    {
                        return null;
                    }

                    // assingv deduces vectorsize and parametercount at interpretation and assumes compiler has generated correct code 
                    // code.Add((byte)node.encode44(ast_node, (byte)ast_node.ChildCount));

                    if (!CompileSimplexFunctionParameterList(data, ast_node, ast_node.children, code))
                    {
                        data.LogError($"Blast.CompileAssignmentNode: error compiling {ast_node.function.ScriptOp} parameters for node <{ast_node.parent}>.<{ast_node}>");
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
            }
            // default compound assignment 
            else
            {
                // encode first part of assignment 
                // [op:assign][var:id+128]
                if (CompileIndexedAssignmentTarget(data, ast_node, assignee, code, blast_operation.assign, is_indexed_assignment, op_indexer, is_constant_cdata_assignment) == null)
                {
                    return null;
                }

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
                            data.LogError($"Blast.CompileAssignmentNode: failed to compile part of assignment: <{ast_node}>");
                            return null;
                        }
                    }
                }
                // end with a nop operation
                // - signal interpretor that this statement is complete.
                // - variable length vectors really need either this or a counter... 
                //   better always have a nop, makes the assembly easier to read from bytes
                code.Add(blast_operation.nop);

                // todo this nop may be skipped in certain situation we need a way to code like we do jumps
            }

            // detect datatype of assignemnt 

            // :assigns: 
            // the first node was succesfully compiled as a parameter, 
            // should the assigned value be interpreted as a boolean and is b.datatype different:
            // then inject a reinterpret_bool32( assignee ); 

            if (assigned_datatype != assignee.DataType && assignee.DataType != BlastVariableDataType.CData)
            {
                switch (assigned_datatype)
                {
                    case BlastVariableDataType.Bool32:
                        {
                            // encode: reinterpret_bool32( assignee_var );
                            code.Add(blast_operation.ex_op);
                            code.Add((byte)extended_blast_operation.reinterpret_bool32);
                            code.Add((byte)(data.Offsets[assignee.Id] + BlastCompiler.opt_ident));
                        }
                        break;

                    case BlastVariableDataType.Numeric:
                        {
                            // encode: reinterpret_float( assignee_var );
                            code.Add(blast_operation.ex_op);
                            code.Add((byte)extended_blast_operation.reinterpret_float);
                            code.Add((byte)(data.Offsets[assignee.Id] + BlastCompiler.opt_ident));
                        }
                        break;

                    default:
                        {
                            data.LogError($"Blast.CompileAssignmentNode: failed to compile assignment: <{ast_node}>. The assigned datatype: {assigned_datatype} mismatches the datatype of the assignee and this conversion is not supported");
                            return null;
                        }
                }
            }


            return code;
        }


        /// <summary>
        /// NORMAL MODE ONLY - Direct constant -> data[ID] 
        /// 
        /// checks if a constant is assigned to data, possibly negated
        /// a = 1; 
        /// a = (-1);
        /// a = -1;
        /// 
        /// </summary>
        private static bool IsDirectConstantAssignmentForNormalPackager(CompilationData data, node ast_node, bool is_constant_cdata_assignment, bool is_indexed_assignment)
        {
            return !is_constant_cdata_assignment &&
                    !is_indexed_assignment &&
                    data.CompilerOptions.PackageMode == BlastPackageMode.Normal
                    &&
                     (
                         (ast_node.ChildCount == 1 && ast_node.FirstChild.IsConstantValue) ||
                         (ast_node.ChildCount == 2 && ast_node.FirstChild.token == BlastScriptToken.Substract && ast_node.LastChild.IsConstantValue) ||
                         (ast_node.ChildCount == 1 && ast_node.FirstChild.IsCompound && ast_node.FirstChild.ChildCount == 2 && ast_node.FirstChild.FirstChild.token == BlastScriptToken.Substract && ast_node.FirstChild.LastChild.IsConstantValue)
                     );
        }


        /// <summary>
        /// the direct assignment of an id - see IsDirectConstantAssignmentForNormalPackager for more information 
        /// -> important: this is not allowed for variable ids > (byte)blast_operation_jumptarget.max_id_for_direct_assign, these opcodes can be used for other things and have double meaning
        ///               - any assignment from an id >= (byte)blast_operation_jumptarget.max_id_for_direct_assign has to go trough the longer route
        /// </summary>
        private static bool IsDirectIDAssignmentForNormalPackager(CompilationData data, node ast_node, bool is_constant_cdata_assignment, bool is_indexed_assignment)
        {
            byte maxid = (byte)blast_operation_jumptarget.max_id_for_direct_assign - (byte)blast_operation.id;
            return !is_constant_cdata_assignment &&
                    !is_indexed_assignment &&
                    data.CompilerOptions.PackageMode == BlastPackageMode.Normal
                    &&
                     (
                         (ast_node.ChildCount == 1 && ast_node.FirstChild.IsScriptVariable && ast_node.variable.Id < maxid) ||
                         (ast_node.ChildCount == 2 && ast_node.FirstChild.token == BlastScriptToken.Substract && ast_node.LastChild.IsScriptVariable && ast_node.LastChild.variable.Id < maxid) ||
                         (ast_node.ChildCount == 1 && ast_node.FirstChild.IsCompound && ast_node.FirstChild.ChildCount == 2 && ast_node.FirstChild.FirstChild.token == BlastScriptToken.Substract && ast_node.FirstChild.LastChild.IsScriptVariable && ast_node.FirstChild.LastChild.variable.Id < maxid)
                     );
        }


        /// <summary>
        ///  Direct constant -> data[ID] 
        /// 
        /// a = 1; 
        /// a = (-1);
        /// a = -1;
        /// 
        /// encode the constant first, then the ID of the data element with the 8bit encoding the boolean substract
        /// - this will provide the normal interpretor with a fast lane setting constants
        /// - this could also set a vectors components to equal values of the same constant -> needs further compiler updates
        /// - this is faster then assignV but less general, assign v should also compile this pattern if:
        /// => the vector assigned has the same constant value operation (only ops) for each component 
        /// => the element assigned is in the datasegment 
        /// 
        /// - saves 1-2 bytes of code, 1 for encoding the assign, and 1 for a possible negation.
        /// - saves a lot more on branches during interpretation:
        /// ==> a = 1; used to take 40-50 ns in normal , now it takes 12 ns as this is a very common operation it makes sence to do this
        /// 
        /// -> SSMD also benifits big as it will skip TWO buffer copies, one for starting the sequencer and 1 taking its result 
        /// -> the saved memory can add up to almost 50% in optimal cases
        /// 
        ///  a = a + 1; -> sets a a + 1 nop;   with this ->  1 a
        /// 
        /// </summary>
        private static void CompileDirectConstantAssignmentForNormalPackager(node ast_node, IMByteCodeList code, BlastVariable assignee)
        {
            if (ast_node.ChildCount == 1)
            {
                if (ast_node.FirstChild.IsCompound)
                {
                    // (- constant)
                    code.Add(ast_node.FirstChild.LastChild.constant_op);
        
                    //the id uses 7 bits, use the 8th for the minus sign 
                    code.Add((byte)((byte)assignee.Id + 0b1000_0000));
                }
                else
                {
                    // single constant 
                    code.Add(ast_node.FirstChild.constant_op);
                    code.Add((byte)assignee.Id);
                }
            }
            else
            {
                // - constant 
                code.Add(ast_node.LastChild.constant_op);
                //the id uses 7 bits, use the 8th for the minus sign 
                code.Add((byte)((byte)assignee.Id + 0b1000_0000));
            }
        }

        /// <summary>
        /// see CompileDirectConstantAssignmentForNormalPackager but then for variables 
        /// 
        /// -> important: this is not allowed for variable ids > (byte)blast_operation_jumptarget.max_id_for_direct_assign, these opcodes can be used for other things and have double meaning
        ///               - any assignment from an id > (byte)blast_operation_jumptarget.max_id_for_direct_assign has to go trough the longer route
        /// 
        /// </summary>
        private static void CompileDirectIDAssignmentForNormalPackager(node ast_node, IMByteCodeList code, BlastVariable assignee)
        {
            if (ast_node.ChildCount == 1)
            {
                if (ast_node.FirstChild.IsCompound)
                {
                    // (- constant)
                    code.Add((byte)(ast_node.FirstChild.LastChild.variable.Id + (byte)blast_operation.id));

                    //the id uses 7 bits, use the 8th for the minus sign 
                    code.Add((byte)((byte)assignee.Id + 0b1000_0000));
                }
                else
                {
                    // single constant 
                    code.Add((byte)(ast_node.FirstChild.variable.Id + (byte)blast_operation.id));
                    code.Add((byte)assignee.Id);
                }
            }
            else
            {
                // - constant 
                code.Add((byte)(ast_node.FirstChild.LastChild.variable.Id + (byte)blast_operation.id));
                //the id uses 7 bits, use the 8th for the minus sign 
                code.Add((byte)((byte)assignee.Id + 0b1000_0000));
            }
        }


        private static IMByteCodeList CompileIndexedAssignmentTarget(CompilationData data, node ast_node, BlastVariable assignee, IMByteCodeList code, blast_operation assignment_op, bool is_indexed_assignment, blast_operation op_indexer, bool is_constant_cdata_assignment)
        {
            bool inject = CompileAssignmentOpCode(data, code, assignment_op, is_indexed_assignment, is_constant_cdata_assignment);
            if (!CompileAssignee(data, ast_node, code, assignee, is_indexed_assignment, op_indexer, is_constant_cdata_assignment))
            {
                // an error is logged in compileassignee
                return null;
            }
            if (inject)
            {
                // add assignment type after any index index 
                code.Add(assignment_op);
            }

            return code; 
        }


        /// <summary>
        /// compile assignment opcode
        /// - the opcode is NOT compiled if the assignment is indexed and packagemode == normal, in that case the index opcode should be first and interpretor picks up from there 
        /// - the opcode is also not compiled if this is a cdata assignment
        /// 
        /// returns true if indexer is injected instead of assing op
        /// </summary>
        /// <param name="data"></param>
        /// <param name="code"></param>
        /// <param name="assign_operation"></param>
        private static bool CompileAssignmentOpCode(CompilationData data, IMByteCodeList code, blast_operation assign_operation, bool is_indexed_assignment, bool is_constant_cdata_assignment)
        {
            if(is_constant_cdata_assignment || is_indexed_assignment)
            {
                switch(data.CompilerOptions.PackageMode)
                {
                    case BlastPackageMode.Normal:
                    case BlastPackageMode.Compiler: return true; // in all other cases add the assignment operation 
                }
            }

            if (is_constant_cdata_assignment)
            {
                switch (data.CompilerOptions.PackageMode)
                {
                    case BlastPackageMode.SSMD: return true; // in all other cases add the assignment operation 
                }
            }
           

            code.Add(assign_operation);
            return false; 
        }


        /// <summary>
        /// compile indexer and assignee opcodes depending on current context 
        /// </summary>
        /// <param name="data"></param>
        /// <param name="ast_node"></param>
        /// <param name="code"></param>
        /// <param name="assignee"></param>
        /// <param name="is_indexed_assignment"></param>
        /// <param name="op_indexer"></param>
        /// <param name="is_constant_cdata_assignment"></param>
        /// <param name="indexer"></param>
        /// <returns></returns>
        private static bool CompileAssignee(CompilationData data, node ast_node, IMByteCodeList code, BlastVariable assignee, bool is_indexed_assignment, blast_operation op_indexer, bool is_constant_cdata_assignment, node indexer = null)
        {
            if (is_constant_cdata_assignment)
            {
                if (!CompileCDATAREF(data, code, assignee))
                {
                    data.LogError($"Blast.Compiler.CompilerAssignment: failed to compile reference to assigned CDATA section, the jumplabel could not be located for variable: {assignee.Name} in node: <{ast_node}>");
                    return false;
                }
            }

            if (is_indexed_assignment) code.Add(op_indexer);

            if (op_indexer == blast_operation.index_n)
            {
                // insert indexer, first validate it has one 
                if (indexer == null && ast_node != null && ast_node.HasIndexers && ast_node.indexers.Count == 1)
                {
                    indexer = ast_node.indexers[0];
                    while (indexer.HasChildren) indexer = indexer.FirstChild; 
                }
                if(indexer == null)
                {
                    data.LogError($"Blast.Compiler.CompilerAssignment: failed to compile assignment, indexer node invalid: {assignee.Name} in node: <{ast_node}>");
                    return false;
                }
                if (indexer.vector_size > 1) // explicetly dont check 0
                {
                    data.LogError($"Blast.Compiler.CompilerAssignment: failed to compile assignment, indexer node <{indexer}> invalid, assignee: {assignee.Name} in node: <{ast_node}>, indexer variable vectorsize should be 1 but it is {indexer.vector_size} instead");
                    return false;
                }
                if (!CompileParameter(data, indexer, code, true))
                {
                    data.LogError($"Blast.Compiler.CompilerAssignment: failed to compile assigned indexer node <{indexer}>, assignee: {assignee.Name} in node: <{ast_node}>");
                    return false;
                }
            }

            if (!is_constant_cdata_assignment)
            {
                code.Add((byte)(data.Offsets[assignee.Id] + BlastCompiler.opt_ident)); // nop + 1 for ids on variables 
            }

            return true; 
        }

        #endregion

        static IMByteCodeList CompileFastLaneOperation(CompilationData data, bool decrement, node ast_node, blast_operation op, IMByteCodeList code = null)
        {
            Assert.IsTrue(ast_node != null);

            if (!ast_node.HasVariable)
            {
                data.LogError($"CompileNode.CompileFastLaneOperation: <{ast_node}> variable not set");
                return null;
            }
            if (ast_node.HasChildren)
            {
                // i [op]= something 
                code.Add(op);

                // compile the data|constant element 
                CompileParameter(data, ast_node.FirstChild, code, false);

                // then target 
                code.Add((byte)(ast_node.variable.Id + (byte)blast_operation.id));
            }
            else
            {
                // i++
                code.Add(op);
                code.Add((byte)(ast_node.variable.Id + (byte)blast_operation.id));
            }

            return code;
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
            Assert.IsFalse(ast_node.IsInlinedFunction);

            if(code == null)
            {
                code = new IMByteCodeList();
            }

            if (ast_node.skip_compilation) return code; 

            // detect if current node is compound with 1 child only
            // >> this should not happen if the flatten operation went ok, but its fixable be we should log warnings  
            if (ast_node.IsCompound && ast_node.HasOneChild && ast_node.type != nodetype.whilecompound)
            {
                data.LogWarning($"Blast.Compiler.CompileNode: skipping compound of node <{ast_node.parent}>.<{ast_node}>");

                // if so just skip it 
                if (ast_node.HasDependencies)
                {
                    // but keep dependencies 
                    ast_node.children[0].depends_on.InsertRange(0, ast_node.depends_on);
                    data.LogWarning($"Blast.Compiler.CompileNode: skipping compound of node <{ast_node.parent}>.<{ast_node}>, {ast_node.DependencyCount} dependencies moved to <{ast_node.children[0]}>");
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
                        data.LogError($"Blast.Compiler.CompileNode: failed to compile resolve compound nesting in dependencies for node: <{ast_node}>.<{initializer}>, failed with error {data.LastError}");
                        return null;
                    }
                    if (CompileNode(data, initializer, code) == null)
                    {
                        data.LogError($"Blast.Compiler.CompileNode: failed to compile dependencies for node: <{ast_node}>.<{initializer}>");
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
                        data.LogError($"Blast.Compiler.CompileNode: encountered [nop] node <{ast_node}> with child nodes => this is not allowed without a valid type");
                        return null;
                    }
                    if (compile_nops) code.Add(blast_operation.nop);
                    break;

                // if we get here something is very wrong 
                case nodetype.root:
                    data.LogError($"Blast.Compiler.CompileNode: encountered a node typed as root => this is only allowed for the root of the AST");
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
                                data.LogError($"Blast.Compiler.CompileNode: encountered an unsupported operation type, node: <{ast_node.parent}><{ast_node}>");
                                return null;
                        }
                    }
                    else
                    {
                        data.LogError($"Blast.Compiler.CompileNode: encountered an unsupported operation type for direct compilation into root, node: <{ast_node}>");
                        return null;
                    }
                    break;

                case nodetype.cdata: 
                    if (ast_node.HasVariable && ast_node.variable.ReferenceCount == 0)
                    {
                        // only if not referenced a cdata node might end up here
                        break;
                    }
                    else goto case default; 

                // unexpected node types: something is wrong 
                default:
                // indices should not be attached to child nodes of an ast node
                case nodetype.index:                // transform should have removed the switch nodes 
                case nodetype.switchnode:
                case nodetype.switchcase:
                case nodetype.switchdefault:
                    data.LogError($"Blast.Compiler.CompileNode: encountered an unsupported node type for direct compilation into root, node: <{ast_node}>");
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
                            data.LogError("Blast.Compiler.CompileNode: skipped yield opcode, yield is not supported by current conflicting compilation options, yield cannot work without packaging a stack");
                        }
                    }
                    else
                    {
                        data.LogWarning("Blast.Compiler.CompileNode: skipped yield opcode, yield is not supported by current compilation options");
                    }
                    break;

                // function 
                case nodetype.function:
                    {
                        // is it inlined or api 
                        if (ast_node.IsFunction && ast_node.function.FunctionId == -1)
                        {
                            data.LogError($"Blast.Compiler.CompileNode: inline function: <{ast_node}> found in node tree, these should not be compiled as api calls."); 
                        }
                        else
                        {
                            // compile an API function call 
                            if (!ast_node.IsFunction || ast_node.function.FunctionId <= 0)
                            {
                                data.LogError($"Blast.Compiler.CompileNode: encountered function node with no function set, node: <{ast_node}>");
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
                                    case ReservedBlastScriptFunctionIds.DebugStack:
                                    case ReservedBlastScriptFunctionIds.Validate:
                                        {
                                            if (!CompileFunction(data, ast_node, code))
                                            {
                                                data.LogError($"Blast.Compiler.CompileNode: failed to compile function: {ast_node}.");
                                                return null;
                                            }
                                        }
                                        break;

                                    default:
                                        data.LogToDo($"todo: allow functions on root");
                                        data.LogError($"Blast.Compiler.CompileNode: only procedures are allowed to execute at root, no functions, found {ast_node.function.GetFunctionName()}");
                                        return null;
                                }
                            }
                            else
                            {
                                if (!CompileFunction(data, ast_node, code))
                                {
                                    data.LogError($"Blast.Compiler.CompileNode: failed to compile function: <{ast_node}>.");
                                    return null;
                                }
                            }
                        }


                        break;
                    }

                // unconditional jump 
                case nodetype.jump_to:
                    {
                        code.Add(blast_operation.jump, IMJumpLabel.Jump(ast_node.identifier));
                        code.Add((byte)0, IMJumpLabel.Offset(ast_node.identifier));
                        code.Add((byte)0);
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
                    if(CompileAssignmentNode(data, ast_node, code) == null)
                    {
                        data.LogError($"Blast.Compiler.CompileNode: failed to compile assignment <{ast_node}>");
                        return null; 
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
                            data.LogError($"Blast.Compiler.CompileNode: failed to compile {ast_node.type} child node: <{ast_node}>.<{ast_child}>");
                            return null;  
                        }
                    }
                    code.Add(blast_operation.end);
                    break;

                // a single parameter 
                case nodetype.parameter:
                    if(!CompileParameter(data, ast_node, code))
                    {
                        data.LogError($"Blast.Compiler.CompileNode: failed to compile parameter node: <{ast_node.parent}>.<{ast_node}>");
                        return null; 
                    }
                    break;

                // a while loop, consisting of a conditional and a compound 
                case nodetype.whileloop:
                    {
                        node n_while_condition = ast_node.GetChild(nodetype.condition);
                        if (n_while_condition == null)
                        {
                            data.LogError($"Blast.Compiler.CompileNode: malformed while node: <{ast_node}>, no while condition found");
                            return null;
                        }

                        node n_while_compound = ast_node.GetChild(nodetype.whilecompound);
                        if (n_while_compound == null)
                        {
                            data.LogError($"Blast.Compiler.CompileNode: malformed while node: <{ast_node}>, no while compound found");
                            return null;
                        }

                        n_while_condition.EnsureIdentifierIsUniquelySet("whilecond_cset");

                        // check that the loop is constant if packaged for SSMD interpretation
                        if (data.CompilerOptions.PackageMode == BlastPackageMode.SSMD)
                        {
                            if (!ast_node.IsTerminatedConstantly || ast_node.IsTerminatedConditionally)
                            {
                                data.LogError($"Blast.Compiler.CompileNode: while loop not terminated in a constant manner. this is not allowed while compiling ssmd packages, node: <{ast_node}>");
                                return null;
                            }
                        }



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
                                data.LogError($"Blast.Compiler.CompileNode: failed to compile child node of while condition: <{n_while_condition}>.<{ast_condition_child}>");
                                return null;
                            }
                        }

                        // start of while loop: jump over while compound when condition zero 
                        // -> do we want to encode the fact its a constantly sized loop?
                        //    or do we let the ssmd interpretor assume compiler only compiles jz if allowed... 
                        if (data.CompilerOptions.PackageMode == BlastPackageMode.SSMD)
                        {
                            code.Add((byte)blast_operation.cjz, IMJumpLabel.Jump(n_while_compound.identifier));
                        }
                        else
                        {
                            code.Add((byte)blast_operation.jz, IMJumpLabel.Jump(n_while_compound.identifier));
                        }
                        code.Add((byte)0, IMJumpLabel.Offset(n_while_compound.identifier));
                        code.Add((byte)0);

                        // compile the condition 
                        if (CompileNode(data, n_while_condition, code) == null)
                        {
                            data.LogError($"Blast.Compiler.CompileNode: failed to compile while condition: <{n_while_condition}>");
                            return null;
                        }

                        // followed by the compound 
                        if (CompileNode(data, n_while_compound, code) == null)
                        {
                            data.LogError($"Blast.Compiler.CompileNode: failed to compile while compound: <{n_while_compound}>");
                            return null;
                        }
                                                                   
                        // jump back to condition and re evaluate
                        // - jumpback is always allowed as its unconditional
                        code.Add((byte)blast_operation.jump_back, IMJumpLabel.Jump(n_while_condition.identifier));
                        code.Add((byte)0, IMJumpLabel.Offset(n_while_condition.identifier));
                        code.Add((byte)0); 

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
                            code.Add(blast_operation.nop);

                            CompileNode(data, if_condition, code);
                            CompileNode(data, if_then, code);

                            // insert an else and jump over it by default 
                            code.Add(blast_operation.jump, IMJumpLabel.Jump(if_else.identifier));
                            code.Add(blast_operation.nop, IMJumpLabel.Offset(if_else.identifier));
                            code.Add(blast_operation.nop);

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
                            code.Add(blast_operation.nop);
                            CompileNode(data, if_condition, code);
                            CompileNode(data, if_then, code);
                            code.Add(blast_operation.nop, IMJumpLabel.Label(if_condition.identifier));
                        }
                        else
                        {
                            // only ELSE
                            code.Add(blast_operation.jnz, IMJumpLabel.Jump(if_condition.identifier)); // jump on true over else
                            code.Add(blast_operation.nop, IMJumpLabel.Offset(if_condition.identifier));
                            code.Add(blast_operation.nop);
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

                // +=    ++
                case nodetype.increment:
                    if (CompileFastLaneOperation(data, false, ast_node, blast_operation.adda, code) == null) return null; 
                    break;

                // -=    --  
                case nodetype.decrement:
                    if (CompileFastLaneOperation(data, true, ast_node, blast_operation.suba, code) == null) return null;
                    break;

                // *=
                case nodetype.multiply:
                    if (CompileFastLaneOperation(data, true, ast_node, blast_operation.multiply, code) == null) return null;
                    break;

                // /=
                case nodetype.divide:
                    if (CompileFastLaneOperation(data, true, ast_node, blast_operation.divide, code) == null) return null;
                    break;

                // ==  !=   >=   <=   >   < 
                case nodetype.compare_equals:
                    if (CompileFastLaneOperation(data, true, ast_node, blast_operation.equals, code) == null) return null;
                    break;

                case nodetype.compare_not_equals:
                    if (CompileFastLaneOperation(data, true, ast_node, blast_operation.not_equals, code) == null) return null;
                    break;

                case nodetype.compare_larger:
                    if (CompileFastLaneOperation(data, true, ast_node, blast_operation.greater, code) == null) return null;
                    break;

                case nodetype.compare_smaller:
                    if (CompileFastLaneOperation(data, true, ast_node, blast_operation.smaller, code) == null) return null;
                    break;

                case nodetype.compare_larger_equals:
                    if (CompileFastLaneOperation(data, true, ast_node, blast_operation.greater_equals, code) == null) return null;
                    break;

                case nodetype.compare_smaller_equals:
                    if (CompileFastLaneOperation(data, true, ast_node, blast_operation.smaller_equals, code) == null) return null;
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
                        if (child.IsInlinedFunction)
                        {
                            // skip inlined functions
                            i++; 
                            continue; 
                        }

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

            // check for excessive nesting, just dont 
            if(AnalyzeCompoundNesting(data, ast_root) != 0)
            {
                data.LogError("Failed to compile node list from <{ast_root}>, there are unresolved nested compounds", (int)BlastError.error_unresolved_nested_statements);
                return null; 
            }

            // first compile all constant cdata nodes into the code stream 
            CompileCDataNodes(data, ast_root, code); 

            // then all other nodes 
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
                    if (ast_node.IsInlinedFunction) continue; 

                    // parse the statement from the token array
                    //CompileNode(data, ast_node, code);      
 
                    //
                    // this exposed a bug with label indices, we need some central storage for them in the compilation 
                    // - added a Dictionary<labelid, imjumplabel> to compilerdata to resolve the issue
                    // 

                    IMByteCodeList bcl = CompileNode(data, ast_node);
                    if (bcl != null)
                    {
                        code.AddRange(bcl); // easier debugging 
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

            // reset any datatype overrides before starting compilation 
            foreach(BlastVariable v in data.Variables)
            {
                v.DataTypeOverride = BlastVectorSizes.none; 
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
            cdata.CalculateVariableOffsets(true);

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
