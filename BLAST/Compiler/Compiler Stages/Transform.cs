
#if STANDALONE_VSBUILD
    using NSS.Blast.Standalone;
    using Unity.Assertions; 
#else
using UnityEngine;
using UnityEngine.Assertions;
#endif

using System;
using System.Collections.Generic;

namespace NSS.Blast.Compiler.Stage
{
    /// <summary>
    /// Transform higher level constructs into their simpler constituents,
    /// [while, for, switch] into ifthen sequences 
    /// </summary>
    public class BlastTransform : IBlastCompilerStage
    {
        public Version Version => new Version(0, 1, 4);
        public BlastCompilerStageType StageType => BlastCompilerStageType.Transform;

        /// <summary>
        /// reduce a singular function in a sequence
        /// </summary>
        /// <param name="data"></param>
        /// <param name="ast_node"></param>
        /// <returns></returns>
        bool reduce_simple_function_sequence(IBlastCompilationData data, node ast_node)
        {
            return true;
        }


        /// <summary>
        /// make a deepclone of the list of nodes 
        /// </summary>
        /// <param name="nodes">input list to clone</param>
        /// <returns>yields each cloned node</returns>
        static IEnumerable<node> clone_nodes(IEnumerable<node> nodes)
        {
            foreach (node n in nodes)
            {
                yield return n.DeepClone();
            }
        }

        /// <summary>
        /// transform a switch into a series of ifthen statements 
        /// </summary>
        /// <param name="result"></param>
        /// <param name="n_switch">the node containing the switch statement</param>
        static void transform_switch(IBlastCompilationData data, node n_switch)
        {
            if (n_switch.parent == null)
            {
                data.LogError($"Transform.Switch: parent node null: <{n_switch}>"); 
                return; // this should be impossible 
            }
            // get injection point 
            node parent = n_switch.parent;
            int i_inject = n_switch.parent.children.IndexOf(n_switch);

            // remove switch node from tree 
            n_switch.parent.children.Remove(n_switch);
            n_switch.parent = null;

            // gather all nodes not assuming any order 
            node condition = null, default_case = null;
            List<node> cases = new List<node>();

            foreach (node c in n_switch.children)
            {
                // dont assume order but do assume distinct node types 
                switch (c.type)
                {
                    case nodetype.condition:
                        condition = c;
                        // ditch any compound 
                        if (c.children.Count == 1 && c.children[0].type == nodetype.compound && c.children[0].children.Count > 0)
                        {
                            condition = c.children[0];
                        }
                        break;
                    case nodetype.switchcase: cases.Add(c); break;
                    case nodetype.switchdefault: default_case = c; break;
                }
            }

            // validate that there is at least 1 case or a default 
            if (cases.Count < 1 && default_case == null)
            {
                data.LogError($"transform.transform_switch: switch statement malformed; no case or default.");
                return;
            }

            string jump_label = node.GenerateUniqueId("label"); 

            // transform each case into a if then, end with the default case
            for (int i = 0; i < cases.Count; i++)
            {
                node case_node = cases[i];

                node n_ifthen = new node(null);
                n_ifthen.type = nodetype.ifthenelse;

                // re-parent it where the switch was 
                n_ifthen.parent = parent;
                parent.children.Insert(i_inject, n_ifthen);
                i_inject++;

                // combine conditions into an if statement 
                node n_ifthen_condition = new node(n_ifthen);
                n_ifthen_condition.type = nodetype.condition;

                n_ifthen_condition.children.AddRange(clone_nodes(condition.children));
                n_ifthen_condition.CreateChild(nodetype.operation, BlastScriptToken.Equals, "__gen_switch_case__=");
                n_ifthen_condition.children.AddRange(case_node.GetChild(nodetype.condition).children);

                foreach (node child in n_ifthen_condition.children) child.parent = n_ifthen_condition;

                // then create the then compound 
                node n_then = new node(n_ifthen);
                n_then.type = nodetype.ifthen;

                n_then.children.AddRange(case_node.GetOtherChildren(nodetype.condition));

                // add a jump to after the last item of this if then sequence, 
                // skip on last case if no default
                if (i < cases.Count - 1 || default_case != null)
                {
                    node jump = new node(n_then);
                    jump.type = nodetype.jump_to;
                    jump.identifier = jump_label;
                }

                foreach (node child in n_then.children) child.parent = n_then;
            }

            // inject statements from default  
            if (default_case != null)
            {
                // add all children from the default case 
                foreach (node ch in default_case.children)
                {
                    parent.children.Insert(i_inject, ch);
                    ch.parent = parent;
                    i_inject++;
                }
            }

            // create a jump label to jump to after the default 
            node label = new node(null);
            label.parent = parent;
            parent.children.Insert(i_inject, label); i_inject++;
            label.type = nodetype.label;
            label.identifier = jump_label;
        }


        /// <summary>
        /// transform a for loop into a while statement 
        /// </summary>
        /// <param name="data">compilation data holding the ast</param>
        /// <param name="n_for">the node with the for loop</param>
        static void transform_for(IBlastCompilationData data, node n_for)
        {
            if (n_for.parent == null)
            {
                data.LogError($"Transform.For: parent node null: <{n_for}>");
                return; 
            }
            // should have 4 child nodes, no more no less 
            if (n_for.children.Count != 4)
            {
                data.LogError($"Transform.For: for node must have exactly 4 child nodes: initializer, condition, iteratorm, compound. Found {n_for.children.Count} instead in <{n_for}> ");
                return;  
            }

            // get injection point and remove the for from the tree
            node parent = n_for.parent;
            int i_inject = n_for.parent.children.IndexOf(n_for);
            n_for.parent.children.Remove(n_for);
            n_for.parent = null;

            // first 3 nodes are the statements, the forth is the compound:
            // for(a = 0; a < b; a = a + 1) {} 
            // 
            // when transforming to a while we move it into the following form: 
            //
            // a = 0   // initializer 
            // while(a < b)   // condition 
            // {
            //      a = a + 1; // iterator 
            // } 
            //

            node n_while = parent.CreateChild(nodetype.whileloop, BlastScriptToken.While, "while_tfor");
            node n_initialize = n_for.children[0];
            node n_condition = n_for.children[1];
            node n_iterator = n_for.children[2];
            node n_compound = n_for.children[3];

            n_while.AppendDependency(n_initialize);
            n_while.SetChild(n_condition);

            n_condition.type = nodetype.condition;
            n_compound.type = nodetype.whilecompound; 

            n_compound.SetChild(n_iterator); 
            n_while.SetChild(n_compound);
        }

        node transform_merge_compound(IBlastCompilationData data, node n)
        {
            Assert.IsTrue(data.IsOK); 
            Assert.IsNotNull(n); 
            Assert.IsTrue(n.ChildCount == 1 && n.children[0].IsCompound);

            node new_node = n.children[0];
            new_node.parent = n.parent;

            n.parent.children[new_node.parent.children.IndexOf(n)] = new_node; 
            if(n.depends_on.Count > 0)
            {
                new_node.depends_on.InsertRange(0, n.depends_on); 
            }
            
            return new_node;
        }


        public int Execute(IBlastCompilationData data)
        {
            if (!data.IsOK || data.AST == null) return (int)BlastError.error;

            // transform node iterator 
            void transform(node n)
            {
                switch(n.type)
                {
                    case nodetype.switchnode:
                        {
                            // transform into a series of ifthen statements 
                            transform_switch(data, n);
                            break;
                        }

                    case nodetype.forloop:
                        {
                            // transform into a while loop 
                            transform_for(data, n); 
                            break; 
                        }

                    case nodetype.compound:
                        {
                            // if having only 1 child, merge with it 
                            while (n.ChildCount == 1 && n.children[0].IsCompound)
                            {
                                n = transform_merge_compound(data, n);
                            }
                            break;
                        }

                    default:
                        {
                            break; 
                        }                         
                }

                foreach (node c in n.children.ToArray())
                {
                    transform(c);
                }
            }

            // recursively look for statements to transform
            transform(data.AST);

            return data.IsOK ? (int)BlastError.success : (int)BlastError.error;
        }
    }
}
