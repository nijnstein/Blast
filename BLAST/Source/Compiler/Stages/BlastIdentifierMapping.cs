//##########################################################################################################
// Copyright © 2022 Rob Lemmens | NijnStein Software <rob.lemmens.s31@gmail.com> All Rights Reserved  ^__^\#
// Unauthorized copying of this file, via any medium is strictly prohibited                           (oo)\#
// Proprietary and confidential                                                                       (__) #
//##########################################################################################################
using System.Collections.Generic;
using System.Threading;
using UnityEngine.Assertions;
using Unity.Mathematics;

namespace NSS.Blast.Compiler.Stage
{

    /// <summary>
    /// Parameter Analysis
    /// - determine parameter types (float, vectorsize) 
    /// - confirm inputs used (warnings if not) and outputs set (not set = error)
    /// - validate correct parameter usage
    /// </summary>
    [System.Runtime.InteropServices.Guid("AFB826E6-A220-4D6E-A214-B447179C69F1")]
    public class BlastIdentifierMapping : IBlastCompilerStage
    {

        /// <summary>
        /// v0.1.0 - initial version
        /// </summary>
        public System.Version Version => new System.Version(0, 1, 0);

        /// <summary>
        /// compiler stage: parameter analysis
        /// </summary>
        public BlastCompilerStageType StageType => BlastCompilerStageType.IdentifierMapping;


        /// <summary>
        /// check mapping of inlined function
        /// </summary>
        bool map_inline_identifier(IBlastCompilationData data, node node)
        {
            Assert.IsNotNull(node);
            Assert.IsTrue(node.type == nodetype.inline_function); 

            // the identifier must be unique at this stage, parser ensures this by running a check on the ast
            // we should still check, maybe catch bugs early
            string id = node.identifier;
            BlastScriptInlineFunction function; 

            if(!data.TryGetInlinedFunction(node.identifier, out function))
            {
                data.LogError($"map_identifiers: failed to map inline function definition, a definition named '{node.identifier}' does not exist, node: <{node}>", (int)BlastError.error_inlinefunction_already_exists); 
                return false; 
            }

            Assert.IsNotNull(function);
            Assert.IsNotNull(function.Node);

            // as soon as the identifier for the functioncall gets mapped it means that the inline function 
            // is actually called and we thus need to include any of its defined constants 

            List<node> work = NodeListCache.Acquire(); 
            work.Push(function.Node); 

            while(work.TryPop(out node current))
            {
                if (current.type == nodetype.parameter && current.variable == null)
                {
                    // skip checks if a parameter 
                    bool is_parameter = false; 
                    
                    for(int i = 0; i < function.Node.depends_on.Count && !is_parameter; i++)
                    {
                        if(string.Compare(function.Node.depends_on[i].identifier, current.identifier, true) == 0)
                        {
                            is_parameter = true; 
                        }                               
                    }

                    // also skip if already set to be a constant
                    if(current.is_constant && current.constant_op != blast_operation.nop)
                    {
                        is_parameter = true; 
                    }
                    
                    if (!is_parameter)
                    {
                        // is it a constant 
                        bool is_negated;
                        bool is_constant;
                        bool is_define;

                        IsConstantDefinedIdentifier(data, ref current.identifier, out is_constant, out is_define, out is_negated);

                        if (!((CompilationData)data).TryGetVariable(current.identifier, out BlastVariable variable))
                        {
                            if (is_define)
                            {
                                // reeval if define replaced identifier 
                                is_negated = current.identifier[0] == '-';
                                is_constant = is_negated || char.IsDigit(current.identifier[0]);
                            }

                            if (!is_constant)
                            {
                                // not a constant, functions may not declare variables
                                data.LogError($"map_inline_identifiers: ({current.identifier}) could not identify node as a constant, variable declarations are not allowed inside inlined functions|macros", (int)BlastError.error_inlinefunction_may_not_declare_variables);
                                NodeListCache.Release(work); 
                                return false;
                            }

                            // its a constant, only lookup the positive part of values, if a match on negated we add back a minus op
                            string value = is_negated ? current.identifier.Substring(1) : current.identifier;

                            blast_operation op = Blast.GetConstantValueDefaultOperation(value, data.CompilerOptions.ConstantEpsilon);
                            if (op == blast_operation.nan)
                            {
                                data.LogError($"map_inline_identifiers: ({current.identifier}) could not convert to a float or use as mathematical constant (pi, epsilon, infinity, minflt, euler)");
                                NodeListCache.Release(work); 
                                return false;
                            }

                            //                       
                            if (op != blast_operation.nop)
                            {
                                // dont create variables for constant ops 
                                current.is_constant = true;
                                current.constant_op = op;

                                // if the value was negated before matching then add the minus sign before it
                                // - this is cheaper then adding a full 32 bit value
                                // - this makes it more easy to see for analyzers if stuff is negated 
                                if (is_negated)
                                {
                                    NegateCompound(current, value);
                                }

                                // we know its constant, if no other vectorsize is set force 1
                                if (current.vector_size < 1)
                                {
                                    current.vector_size = 1;
                                }
                                current.is_vector = current.vector_size > 1;
                            }
                            else
                            {
                                // ----------------------------------------------------------
                                // - this is the only variable allowed in inlined functions - 
                                // - its a constant and its vectorsize is always 1 ----------
                                // non op encoded constant, create a variable for it 
                                current.variable = ((CompilationData)data).CreateVariable(current.identifier);
                                current.vector_size = 1;
                                current.is_vector = false;
                            }
                        }
                        else
                        {
                            if(current.variable == null)
                            {
                                current.variable = variable;
                                current.vector_size = variable.VectorSize;
                                current.is_vector = variable.VectorSize > 1;
                            }
                        }
                    }
                }

                // check all child nodes 
                for (int i = 0; i < current.ChildCount; i++) work.Push(current.children[i]); 
            }


            NodeListCache.Release(work); 
            return true;
        }

        private static void NegateCompound(node current, string value)
        {
            // put this node in a compound
            // - effectively do this:  parent->child  ->  parent->compound->child
            node compound = current.InsertParent(new node(nodetype.compound, BlastScriptToken.Identifier));

            // and replace it at parent 
            current.InsertBeforeThisNodeInParent(nodetype.operation, BlastScriptToken.Substract);
            current.identifier = value; // update to the non-negated value 
        }

        void IsConstantDefinedIdentifier(IBlastCompilationData data, ref string identifier, out bool is_constant, out bool is_defined, out bool is_negated)
        {
            is_negated = identifier[0] == '-';
            is_constant = is_negated || char.IsDigit(identifier[0]);
            is_defined = false;

            string defined_value;
            if (!is_constant)
            {
                if (is_negated)
                {
                    if (data.TryGetDefine(identifier.Substring(1), out defined_value))
                    {
                        identifier = "-" + defined_value;
                        is_defined = true;
                    }
                }
                else
                {
                    if (data.TryGetDefine(identifier, out defined_value))
                    {
                        identifier = defined_value;
                        is_defined = true;
                    }
                }
            }
        }


        /// <summary>
        /// map identifiers to variables
        /// - determine if a opcode constant (1,2 , pi and stuff) or just a constant number on the data stack 
        /// </summary>
        /// <param name="data">compiler data</param>
        /// <param name="ast_node">the node to scan including child nodes</param>
        /// <returns>true if succeeded / IsOK</returns>
        bool map_identifiers(IBlastCompilationData data, node ast_node)
        {
            // if this is an inline function
            if(ast_node.type == nodetype.inline_function)
            {
                // map its name and assigned variables not known or set in parameterlist  
                if (!map_inline_identifier(data, ast_node))
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }

            // if a parameter and not already mapped to something 
            if (ast_node.type == nodetype.parameter && ast_node.variable == null
                &&
                !(ast_node.is_constant && ast_node.constant_op != blast_operation.nop))
            {
                CompilationData cdata = data as CompilationData;

                // although the tokenizer recombines the minus sign with variables we split them here 
                // to lookup a possible match in constants, if matched to any the negation is put back
                // seperate as an operand before the positive constant, essentially undoing the work from tokenizer in the case of known constants 
                bool is_negated;
                bool is_constant;
                bool is_define;

                IsConstantDefinedIdentifier(data,ref ast_node.identifier, out is_constant, out is_define, out is_negated); 

                // check if its a define if the value is not a constant
                /*string defined_value;
                if (!is_constant)
                {
                    if (is_negated)
                    {
                        if (data.TryGetDefine(ast_node.identifier.Substring(1), out defined_value))
                        {
                            ast_node.identifier = "-" + defined_value;
                            is_define = true;
                        }
                    }
                    else
                    {
                        if (data.TryGetDefine(ast_node.identifier, out defined_value))
                        {
                            ast_node.identifier = defined_value;
                            is_define = true;
                        }
                    }
                } */ 

                // if its an input or output variable it will already exist
                if (!cdata.ExistsVariable(ast_node.identifier))
                {
                    if (is_define)
                    {
                        // reeval if define replaced identifier 
                        is_negated = ast_node.identifier[0] == '-';
                        is_constant = is_negated || char.IsDigit(ast_node.identifier[0]);
                    }

                    // if not known or constant raise undefined error
                    if (!is_constant)
                    {
                        data.LogError($"map_identifiers: identifier '{ast_node.identifier}' is not constant and not defined in input and as such is undefined.");
                        return false;
                    }

                    // if we compile using system constant then use opcodes for known constants 
                    if (data.CompilerOptions.CompileWithSystemConstants)
                    {

                        // its a constant, only lookup the positive part of values, if a match on negated we add back a minus op
                        string value = is_negated ? ast_node.identifier.Substring(1) : ast_node.identifier;

                        blast_operation op = Blast.GetConstantValueDefaultOperation(value, data.CompilerOptions.ConstantEpsilon);
                        if (op == blast_operation.nan)
                        {
                            data.LogError($"map_identifiers: ({ast_node.identifier}) could not convert to a float or use as mathematical constant (pi, epsilon, infinity, minflt, euler)");
                            return false;
                        }
                        // get / create variable, reference count is updated accordingly 
                        if (is_constant && op != blast_operation.nop)
                        {
                            // dont create variables for constant ops 
                            ast_node.is_constant = true;
                            ast_node.constant_op = op;

                            // if the value was negated before matching then add the minus sign before it
                            // - this is cheaper then adding a full 32 bit value
                            // - this makes it more easy to see for analyzers if stuff is negated 
                            if (is_negated)
                            {
                                NegateCompound(ast_node, value);
                            }

                            // we know its constant, if no other vectorsize is set force 1
                            if (ast_node.vector_size < 1) ast_node.vector_size = 1;
                        }
                        else
                        {
                            // non op encoded constant, create a variable for it 
                            ast_node.variable = cdata.CreateVariable(ast_node.identifier);
                        }

                    }
                    else
                    {
                        // not compiling with system constants, just add a variable 
                        ast_node.variable = cdata.CreateVariable(ast_node.identifier);
                    }
                }
                else
                {
                    // get existing variable, updating reference count 
                    ast_node.variable = cdata.GetVariable(ast_node.identifier);
                    Interlocked.Increment(ref ast_node.variable.ReferenceCount);
                }
            }


            // verify vectorsize of node, if its 0 we update from anything set by variable 
            if (ast_node.vector_size == 0)
            {


                if (ast_node.type == nodetype.parameter)
                {
                    if (ast_node.variable != null)
                    {
                        ast_node.vector_size = ast_node.variable.VectorSize;
                        ast_node.is_vector = ast_node.variable.VectorSize > 1;
                    }
                    else
                    {
                        // is it a constant ? 
                        if (ast_node.is_constant && ast_node.constant_op != blast_operation.nop)
                        {
                            ast_node.vector_size = 1;
                            ast_node.is_vector = false; 
                        }
                    }
                }
            }


            // map children (collection may change) 
            int i = 0;
            while (i < ast_node.ChildCount)
            {
                int c = ast_node.ChildCount;
                map_identifiers(data, ast_node.children[i]);
                if (c != ast_node.ChildCount)
                {
                    // childlist changed 
                    i += ast_node.ChildCount - c;
                }
                i++;
            }

            return data.IsOK;
        }


  

        /// <summary>
        ///  
        /// </summary>
        /// <param name="data">current compilationdata</param>
        /// <returns>0 for success, other exitcode = error</returns>
        public int Execute(IBlastCompilationData data)
        {
            // identify parameters 
            // walk each node, map each identifier as a variable or system constant
            // we start at the root and walk to each leaf, anything not known should raise an error 
            if (!map_identifiers(data, data.AST))
            {
                data.LogError($"blast.compiler.identifiermapping: failed to map all identifiers");
                return (int)BlastError.error_mapping_parameters;
            }

            return (int)BlastError.success; 
        }
    }

}