
#if STANDALONE
    using NSS.Blast.Standalone;
    using Unity.Assertions; 
#else
using UnityEngine;
#endif

using Unity.Mathematics;

namespace NSS.Blast.Compiler.Stage
{
    /// <summary>
    /// Basic Node Analysis 
    /// - force multiplication rules 
    /// - simplifies arithmetic 
    /// - refactors divisions 
    /// </summary>
    public class BlastAnalysis : IBlastCompilerStage
    {
        public System.Version Version => new System.Version(0, 1, 0);
        public BlastCompilerStageType StageType => BlastCompilerStageType.Analysis;

        /// <summary>
        /// simplify non-vector non-function compounds by removing nested compounds when operations allow it. 
        /// 
        /// -> all operations equal then move compound up, returns nr of moves made
        /// </summary>
        static int simplify_compound_arithmetic(IBlastCompilationData data, node n)
        {   
            if (n.type == nodetype.function || n.is_vector) return 0;
            
            // leave alone vectors
            if (n.is_vector) return 0; 

            // 1 or more compounds
            // fully arithmetic 
            int n_compounds;
            int n_moves = 0;
            bool b_again;
            do
            {
                n_compounds = 0;
                b_again = false;
                BlastScriptToken op = BlastScriptToken.Nop;

                foreach (var c in n.children)
                {
                    switch (c.type)
                    {
                        case nodetype.operation:
                            {
                                if (op == BlastScriptToken.Nop)
                                {
                                    op = c.token;
                                }
                                else
                                {
                                    if (op != c.token)
                                    {
                                        // not all same, wont optimize
                                        // todo : determine operator precedence .,,,, 
                                        return n_moves;
                                    }
                                }
                            }
                            break;
                        case nodetype.compound:
                            {
                                // move up compounds if not vector definitions 
                                if(node.IsVectorDefinition(c))
                                {
                                    return n_moves; 
                                }

                                n_compounds++;

                                /// remove compound only if:
                                foreach (var cc in c.children)
                                {
                                    switch (cc.type)
                                    {
                                        case nodetype.operation:
                                            {
                                                if (op == BlastScriptToken.Nop)
                                                {
                                                    op = cc.token;
                                                }
                                                else
                                                {
                                                    if (op != cc.token)
                                                    {
                                                        // not all same, wont optimize
                                                        // todo : determine operator precedence .,,,, 
                                                        return n_moves;
                                                    }
                                                }
                                            }
                                            break;
                                    }
                                }
                            }
                            break;
                    }
                }

                if (n_compounds > 0)
                {
                    // move up compounds
                    for (int i = 0; i < n.children.Count; i++)
                    {
                        if (n.children[i].type == nodetype.compound)
                        {
                            //
                            //   detect if parent is a function, then we should push it to stack and pop as param => flatten
                            //

                            var cmp = n.children[i];
                            cmp.parent = null;
                            foreach (var cc in cmp.children)
                            {
                                cc.parent = n;
                                if (cc.type == nodetype.compound)
                                {
                                    b_again = true; // this should not happen if leaf 
#if DEVELOPMENT_BUILD
                                    Debug.LogWarning("nested compound.. should not happen here");
#endif
                                }
                            }

                            // 
                            //  here   mina(1 (-2) 3) became mina( 1 - 2  3)
                            // 


                            n.children.RemoveAt(i);
                            n.children.InsertRange(i, cmp.children);
                            i += cmp.children.Count;

                            cmp.children.Clear();
                            b_again = true;
                        }
                    }


                    n_moves += n_compounds;
                }

            } while (b_again);

            return n_moves;
        }

        /// <summary>
        ///  refactor divisions into multiplications with inverse of constants where possible 
        /// </summary>
        /// <param name="data"></param>
        /// <param name="n"></param>
        static void refactor_divisions(IBlastCompilationData data, node n)
        {
            // refactor a division by constant value
            int i = 1; // a division wont ever be the first element nor will it be the last
            while (i < n.children.Count)
            {
                // trigger on each division 
                if (n.children[i].type == nodetype.operation && n.children[i].token == BlastScriptToken.Divide)
                {
                    bool minus = false;
                    node right = n.children[i + 1];
                    if (right.type == nodetype.operation)
                    {
                        // only minus is allowed 
                        if (right.token == BlastScriptToken.Substract)
                        {
                            minus = true;
                            if (i + 2 >= n.children.Count)
                            {
                                // possible bugged code, dont refactor 
                                i += 2;
                                continue;
                            }
                            right = n.children[i + 2];
                        }
                        else
                        {
                            // cant refactor
                            i += 2;
                            continue;
                        }
                    }

                    // whatever the left side is, if the right side is a constant then we can replace it with its inverse
                    if (right.type == nodetype.parameter)
                    {
                        if (right.is_constant)
                        {
                            float f = 1f / right.GetConstantFloatValue(data);

                            if (right.constant_op == blast_operation.nop)
                            {
                                // encoded in variable 
                                BlastVariable existing_script_var = data.GetVariable(right.identifier);
                                if (existing_script_var == null)
                                {
                                    data.LogError($"analyse.refactor_divisions: failed to reference variable belonging to node identifier '{right.identifier}' in node {right.ToString()} ");
                                    return;
                                }

                                // decrease referencecount as we are using it 1 less time 
                                existing_script_var.ReferenceCount--;
                            }
                            else
                            {
                                // encoded in operation, reset that 
                                right.constant_op = blast_operation.nop;
                            }

                            // is the new variable already mapped ? 
                            string value = f.ToString("R");
                            blast_operation op = data.Blast.GetConstantValueOperation(value, data.CompilerOptions.ConstantEpsilon);
                            if (op == blast_operation.nan)
                            {
                                data.LogError($"analyse.refactor_divisions: failure converting constant value to its reciprocal and remapping it");
                                return;
                            }
                            else
                            // get / create variable, reference count is updated accordingly 
                            if (op != blast_operation.nop)
                            {
                                // dont create variables for constant ops 
                                right.is_constant = true;
                                right.vector_size = 1; // constants => vectorsize 1 
                                right.constant_op = op;
                                right.identifier = value;
                            }
                            else
                            {
                                // non op encoded constant, create a variable for it if needed 
                                right.variable = ((CompilationData)data).GetOrCreateVariable(value);
                                right.constant_op = blast_operation.nop;
                                right.identifier = value;
                            }

                            // alter operation from division into multiply
                            n.children[i].token = BlastScriptToken.Multiply;
                        }
                        // advance to next node
                        i += minus ? 2 : 1;
                        continue;
                    }
                }

                if (n.children[i].type == nodetype.compound)
                {
                    // recursively refactor divisions in child compounds 
                    refactor_divisions(data, n.children[i]);
                }

                i++;
            }
        }

        /// <summary>
        /// apply rules of multiplication if needed to children of this node 
        /// </summary>
        /// <param name="data"></param>
        /// <param name="n"></param>
        static void apply_multiplication_rules(IBlastCompilationData data, node n)
        {
            bool need_reorder;
            do
            {
                need_reorder = false;
                bool has_addsub = false;
                bool last_was_op = false;

                int i_child = 0;
                while (!need_reorder && i_child < n.children.Count - 1)
                {
                    i_child++;
                    node child = n.children[i_child];
                    switch (child.type)
                    {
                        case nodetype.compound:                                        
                            apply_multiplication_rules(data, child); 
                            last_was_op = false; 
                            continue;

                        case nodetype.parameter: last_was_op = false; continue;

                        case nodetype.function: apply_multiplication_rules(data, child); last_was_op = false; continue;
                        case nodetype.operation:
                            if (last_was_op)
                            {
                                if (child.token != BlastScriptToken.Substract)
                                {
                                    // error
                                    data.LogError("analyze.apply_multiplication_rules: found double operater, this is only allowed for negation with '-'");
                                    return;
                                }
                            }
                            switch (child.token)
                            {
                                case BlastScriptToken.Add:
                                case BlastScriptToken.Substract:
                                    has_addsub = true;
                                    last_was_op = true;
                                    continue;

                                case BlastScriptToken.Multiply:
                                case BlastScriptToken.Divide:
                                    // muldiv after an addition -> reorder operation 
                                    if (has_addsub) need_reorder = true;
                                    last_was_op = true;
                                    break;

                                case BlastScriptToken.And:
                                case BlastScriptToken.Or:
                                case BlastScriptToken.GreaterThen:
                                case BlastScriptToken.GreaterThenEquals:
                                case BlastScriptToken.SmallerThen:
                                case BlastScriptToken.SmallerThenEquals:
                                case BlastScriptToken.Xor:
                                    return;

                                default:
                                    last_was_op = true;
                                    continue;
                            }
                            break;

                        default:
                        case nodetype.root:
                        case nodetype.assignment:
                        case nodetype.yield:
                            last_was_op = false;
                            // at least report that the given nodetype is invalid in this location 
                            data.LogError($"analyze.apply_multiplication_rules: invalid nodetype '{child.type}' in assignment");
                            return;
                    }
                }

                if (need_reorder)
                {
                    // scan for all * and / after this and put all these children in a new compound object
                    int i_last = i_child + 1;
                    last_was_op = false;
                    bool b_end = false;

                    while (!b_end && i_last < n.children.Count)
                    {
                        if (n.children[i_last].type == nodetype.operation)
                        {
                            switch (n.children[i_last].token)
                            {
                                case BlastScriptToken.Substract:
                                    if (last_was_op)
                                    {
                                        i_last++;
                                        continue;
                                    }
                                    else
                                    {
                                        b_end = true;
                                        break;
                                    }

                                case BlastScriptToken.Divide:
                                case BlastScriptToken.Multiply:
                                    {
                                        i_last++;
                                        continue;
                                    }
                                default:
                                    {
                                        // found end of 8/ sequence
                                        b_end = true;
                                        break;
                                    }
                            }
                        }
                        else
                        {
                            last_was_op = false;
                            i_last++;
                        }
                    }

                    // the part between i_child - 1 and i_last - 1 needs to be extracted
                    node new_sequence = new node(null) { type = nodetype.compound, token = BlastScriptToken.Nop };
                    i_child = i_child - 1;
                    int n_to_move = i_last - i_child;
                    for (int move = 0; move < n_to_move; move++)
                    {
                        new_sequence.children.Add(n.children[i_child]);
                        n.children.RemoveAt(i_child);
                    }
                    new_sequence.parent = n;
                    n.children.Insert(i_child, new_sequence);
                }
            }
            while (need_reorder);
        }


        /// <summary>
        /// simplify nodes until no changes occur 
        /// </summary>
        static int simplify_node(IBlastCompilationData data, node n)
        {
            int changes = 0, n_changes;
            do
            {
                n_changes = 0;

                foreach (node child in n.children)
                {
                    // reduces forms `(1 * 2) * 3` to `1 * 2 * 3`
                    n_changes += simplify_compound_arithmetic(data, child);


                    // recursively call on each node
                    n_changes += simplify_node(data, child);
                }

                changes += n_changes;
            }
            while (n_changes > 0);
            return changes;
        }

        static int replace_double_minus(IBlastCompilationData data, node n)
        {
            int i = 0;
            bool last = false;
            int c = 0;
            while (i < n.ChildCount)
            {
                node child = n.children[i];

                bool isminus = child.token == BlastScriptToken.Substract;


                if (last && isminus)
                {
                    // replace by remove the current and updating the last 
                    n.children.RemoveAt(i);
                    n.children[i - 1].token = BlastScriptToken.Add; 
                    c += 1;
                    i--;
                }
                else
                if (last)
                {
                    if (child.type == nodetype.parameter && child.variable != null)
                    {
                        if (child.identifier.Length > 1 && child.identifier[0] == '-')
                        {
                            // replace by removing - from identifier and updating the last to +
                            child.identifier = child.identifier.Substring(1);
                            if (child.variable.Name[0] == '-') { child.variable.Name = child.variable.Name.Substring(1); }
                            n.children[i - 1].token = BlastScriptToken.Add; 
                            c += 1;
                        }
                    }
                }               

                last = isminus; 
                i++; 
            }

            return c; 
        }


        /// <summary>
        /// analyze a node and its children, can be run in parallel on different node roots
        /// </summary>
        /// <param name="n"></param>
        static void analyze_node(IBlastCompilationData data, node n)
        {
            if (!n.skip_compilation)
            {
                switch (n.type)
                {
                    case nodetype.assignment:
                        if (n.children.Count > 0)
                        {
                            apply_multiplication_rules(data, n);
                            refactor_divisions(data, n);
                            simplify_compound_arithmetic(data, n);
                            replace_double_minus(data, n);
                         ///   encode_minus_into_identifier_of_vector_part(data, n); 
                        }
                        return;

                    case nodetype.function:
                        data.LogToDo("analyze function not implemented -> parameters may have incorrect multiplication rules");
                        return;

                    default:
                        foreach (node child in n.children)
                        {
                            analyze_node(data, child);
                        }
                        return;
                }
            }
        }


        /// <summary>
        /// analyse ast tree and perform corrections/optimizations 
        /// </summary>
        /// <param name="data"></param>
        public int Execute(IBlastCompilationData data)
        {
            // run root->leaf analysis that can run in parallel 
#if MULTITHREADED
            Parallel.ForEach(data->AST.children, child =>
#else
            foreach (node child in data.AST.children)
#endif
            {
                analyze_node(data, child);



                int n = 0;
                while (simplify_node(data, child) > 0 && n++ < 5);

            }
#if MULTITHREADED
            );
#endif

            return data.IsOK ? (int)BlastError.success : (int)BlastError.error;
        }
    }
}