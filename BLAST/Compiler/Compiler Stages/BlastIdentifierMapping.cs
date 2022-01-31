using System.Collections.Generic;
using System.Threading;
using Unity.Mathematics;

namespace NSS.Blast.Compiler.Stage
{

    /// <summary>
    /// Parameter Analysis
    /// - determine parameter types (float, vectorsize) 
    /// - confirm inputs used (warnings if not) and outputs set (not set = error)
    /// - validate correct parameter usage
    /// </summary>
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
        /// map identifiers to variables
        /// - determine if a opcode constant (1,2 , pi and stuff) or just a constant number on the data stack 
        /// </summary>
        /// <param name="data">compiler data</param>
        /// <param name="ast_node">the node to scan including child nodes</param>
        /// <returns>true if succeeded / IsOK</returns>
        bool map_identifiers(IBlastCompilationData data, node ast_node)
        {
            // if a parameter and not already mapped to something 
            if (ast_node.type == nodetype.parameter && ast_node.variable == null)
            {
                CompilationData cdata = data as CompilationData;

                // although the tokenizer recombines the minus sign with variables we split them here 
                // to lookup a possible match in constants, if matched to any the negation is put back
                // seperate as an operand before the positive constant, essentially undoing the work from tokenizer in the case of known constants 
                bool is_negated = ast_node.identifier[0] == '-';
                bool is_constant = is_negated || char.IsDigit(ast_node.identifier[0]);
                bool is_define = false;

                // check if its a define 
                string defined_value;
                if (!is_constant && data.TryGetDefine(ast_node.identifier, out defined_value))
                {
                    ast_node.identifier = defined_value;
                    is_define = true;
                }

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
                        data.LogError($"parser.map_identifiers: identifier '{ast_node.identifier}' is not constant and not defined in input and as such is undefined.");
                        return false;
                    }

                    // if we compile using system constant then use opcodes for known constants 
                    if (data.CompilerOptions.CompileWithSystemConstants)
                    {

                        // its a constant, only lookup the positive part of values, if a match on negated we add back a minus op
                        string value = is_negated ? ast_node.identifier.Substring(1) : ast_node.identifier;

                        blast_operation op = Blast.GetConstantValueOperation(value, data.CompilerOptions.ConstantEpsilon);
                        if (op == blast_operation.nan)
                        {
                            data.LogError($"parse.map_identifiers: ({ast_node.identifier}) could not convert to a float or use as mathematical constant (pi, epsilon, infinity, minflt, euler)");
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
                                // put this node in a compound
                                // - effectively do this:  parent->child  ->  parent->compound->child
                                node compound = ast_node.InsertParent(new node(nodetype.compound, BlastScriptToken.Identifier));

                                // and replace it at parent 
                                ast_node.InsertBeforeThisNodeInParent(nodetype.operation, BlastScriptToken.Substract);
                                ast_node.identifier = value; // update to the non-negated value 
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