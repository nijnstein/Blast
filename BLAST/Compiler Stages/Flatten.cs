using System;

namespace NSS.Blast.Compiler.Stage
{
    /// <summary>
    /// Flatten Nodes 
    /// - removes all nested things and reduces them to a linear stream of instructions using stack operations 
    /// </summary>
    public class BlastFlatten : IBlastCompilerStage
    {
        public Version Version => new Version(0, 1, 0);
        public BlastCompilerStageType StageType => BlastCompilerStageType.Flatten;

        /// <summary>
        /// lessen recursion while executing the script by flattening compound nodes,
        /// we keep taking out groups and stack their results
        /// </summary>
        /// <param name="data"></param>
        /// <param name="root"></param>
        static void flatten(IBlastCompilationData data, node root)
        {
            int c_replaced;

            do
            {
                c_replaced = 0;

                // start from each root node
                for (int i = 0; i < root.children.Count; i++)
                {
                    node c1 = root.children[i];

                    switch (c1.type)
                    {
                        case nodetype.assignment:
                            {
                                // assigns the result of a compound 

                                // case 1: assignment of compound, move forward child compounds
                                // case 2: assign with list possibly having compounds
                                flatten_compounds(data, ref c_replaced, ref i, c1, c1.has_single_compound_as_child() ? c1.children[0] : c1);
                            }
                            break;

                        case nodetype.whileloop:

                            // flatten condition, expand while condition into its root

                            // -->  upon re-entry we need to jump to before these objects.... 
                            // ->> compiler note: make sure to use the dependencies on condition when determining jump back location 

                            node n_condition = c1.get_child(nodetype.condition);
                            flatten_compounds(data, ref c_replaced, ref i, c1, n_condition);

                            // skip compilation, the while loop compiler will handle these in order to get their jump label 
                            foreach (node n in n_condition.depends_on)
                            {
                                n.skip_compilation = true;
                            }

                            // flatten inner while compound, expand at start of compound
                            flatten(data, c1.get_child(nodetype.whilecompound));

                            break;

                        case nodetype.ifthenelse:

                            // flatten condition, expand before if condition (into its root)
                            flatten_compounds(data, ref c_replaced, ref i, c1, c1.get_child(nodetype.condition));

                            // dont care about dependancies.. unlike in the while loop we wont return here 

                            // flatten then and else compound, expand at start of then else
                            node n_then = c1.get_child(nodetype.ifthen);
                            if (n_then != null)
                            {
                                flatten(data, n_then);
                                if (!data.IsOK) return;
                            }

                            node n_else = c1.get_child(nodetype.ifelse);
                            if (n_else != null)
                            {
                                flatten(data, n_else);
                                if (!data.IsOK) return;
                            }

                            break;
                    }
                }
            }
            // keep on it while something was replaced as we only look one compound level deep 
            while (c_replaced > 0);
        }


        /// <summary>
        /// flatten the contents of a compound
        /// > operates on child nodes:
        /// - compounds 
        /// - functions 
        /// </summary>
        /// <param name="c_replaced">counter maintaining replace count in run</param>
        /// <param name="i_insert">insertion point</param>
        /// <param name="n_node_to_flatten">node that needs to be flattened</param>
        /// <param name="n_insert">the insertion node (usually parent), if into insert at front, if not into insert just before</param>
        /// <param name="into">true to select into the insertion node (at front) or false to insert just before the insertion node</param>
        private static void flatten_compounds(IBlastCompilationData data, ref int c_replaced, ref int i_insert, node n_insert, node n_node_to_flatten, bool into = false)
        {
            int j = 0;
            for (; j < n_node_to_flatten.children.Count; j++)
            {
                node c2 = n_node_to_flatten.children[j];
                switch (c2.type)
                {
                    case nodetype.compound:
                        {
                            if (into)
                            {
                                int k = 0;
                                node inserted = flatten_replace_node(data, ref c_replaced, ref k, c2, c2, true);
                                if (inserted != null)
                                {
                                    n_node_to_flatten.depends_on.Insert(0, inserted);
                                }
                            }
                            else
                            {
                                node inserted = flatten_replace_node(data, ref c_replaced, ref i_insert, n_insert, c2, false);
                                if (inserted != null)
                                {
                                    n_node_to_flatten.depends_on.Add(inserted);
                                }
                            }
                        }
                        break;

                    case nodetype.function:
                        data.LogToDo("flatten function parameters");
                        // could have nested functions .. 
                        for (int k = 0; k < c2.children.Count; k++)
                        {
                            node inserted = null; 
                            node c3 = c2.children[k];
                            switch (c3.type)
                            {
                                //
                                //@ assignment.compound.function.function 
                                //
                                case nodetype.function:
                                   
                                    // allow only popX to remain 
                                    if (!c3.function.IsPopVariant())
                                    {

                                        inserted = flatten_replace_node(data, ref c_replaced, ref i_insert, n_insert, c3, false);
                                        if (inserted != null)
                                        {
                                            n_node_to_flatten.depends_on.Add(inserted);
                                        }


                                        // move function back, into is not an option with functions 

                                        //
                                        // if there is another pop then probably it will fuck the order up
                                        //
                                        // TODO 

                                        // results in endless loop

                                        ///     node inserted = flatten_replace_node(ref c_replaced, ref i_insert, n_insert, c3, false);
                                        ///     if (inserted != null) n_node_to_flatten.depends_on.Add(inserted);
                                    }    
                                    break;

                                case nodetype.compound:
                                    inserted = flatten_replace_node(data, ref c_replaced, ref i_insert, n_insert, c3, false);
                                    if (inserted != null)
                                    {
                                        n_node_to_flatten.depends_on.Add(inserted);
                                    }
                                    break;
                            }
                        }


                        break;

                    default:
                        // do nothing on other node types 
                        break;
                }
            }
        }

        private static node flatten_replace_node(IBlastCompilationData data, ref int c_replaced, ref int i, node insertion_node, node node_to_replace, bool front = false)
        {
            // determine type of insertion, if its a function only (possibly with parameters) then 
            // use pushf to instruct the interpretor to directly execute the function at hand and push its value to the stack

            bool pushing_function = node_to_replace.is_only_1_function();

            // create a new push op and manually parent it in root 
            node insert = new node(null);
            insert.identifier = "_gen_push";
            insert.parent = insertion_node.parent;
            insert.type = nodetype.function;
            insert.function =
                pushing_function
                ?
                data.Blast.GetFunctionById(ReservedScriptFunctionIds.PushFunction)
                :
                data.Blast.GetFunctionById(ReservedScriptFunctionIds.Push);

            // it should be inserted before the assigment node in the root-child list
            // - or at the front if requested 
            int idx_push = front ? 0 : insertion_node.parent.children.IndexOf(insertion_node);
            insertion_node.parent.children.Insert(idx_push, insert);

            // create a new pop instruction at c2
            node pop = new node(null);
            pop.identifier = "_gen_pop";
            pop.parent = node_to_replace.parent;
            pop.type = nodetype.function;
            pop.function = data.Blast.GetFunctionById(ReservedScriptFunctionIds.Pop);
            int idx_pop = node_to_replace.parent.children.IndexOf(node_to_replace);
            node_to_replace.parent.children.Insert(idx_pop, pop);

            // then we move the compound c2 into the newly inserted node as child
            node_to_replace.parent.children.Remove(node_to_replace);
            node_to_replace.parent = insert;
            insert.children.Add(node_to_replace);

            // and all should be well... 
            c_replaced++;
            i++; // we added a root item, skip it now

            return insert;
        }







        public int Execute(IBlastCompilationData data)
        {
            flatten(data, data.AST);
        
            return (int)(data.IsOK ? BlastError.success : BlastError.error);
        }
    }
}
