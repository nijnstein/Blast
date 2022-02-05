﻿//##########################################################################################################
// Copyright © 2022 Rob Lemmens | NijnStein Software <rob.lemmens.s31@gmail.com> All Rights Reserved  ^__^\#
// Unauthorized copying of this file, via any medium is strictly prohibited                           (oo)\#
// Proprietary and confidential                                                                       (__) #
//##########################################################################################################
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
        /// <summary>
        /// current version 
        /// </summary>
        public Version Version => new Version(0, 1, 4);

        /// <summary>
        /// transform stage 
        /// </summary>
        public BlastCompilerStageType StageType => BlastCompilerStageType.Transform;

  


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
        /// <param name="data"></param>
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



        /// <summary>
        /// transform a function call to an inlined funtion into the code it generates 
        /// </summary>
        /// <param name="data"></param>
        /// <param name="n"></param>
        BlastError transform_inline_function_call(IBlastCompilationData data, node n)
        {
            BlastScriptInlineFunction function;

            // lookup the inlined function 
            if (!data.TryGetInlinedFunction(n.identifier, out function))
            {
                data.LogError($"transform: couldnt locate inlined function body for identifier: '{n.identifier}'", (int)BlastError.error_inlinefunction_doenst_exist);
                return BlastError.error_inlinefunction_doenst_exist;
            }

            // setup translation map for variables/parameters 
            node[] parameters = n.children.ToArray();
            Dictionary<string, node> varmap = new Dictionary<string, node>();

            for (int i = 0; i < parameters.Length; i++)
            {
                varmap.Add(function.Node.depends_on[i].identifier, parameters[i]);
            }

            // input node n can be like:
            //
            //  assignment of result
            //   function test
            //      constant parameter 2
            //         constant parameter 3
            //      /
            //   /
            //
            //
            // when funtion = 
            //
            //  inlined - function test[depends on: parameter a parameter b]
            //      assignment of c
            //         parameter b
            //         operation Substract
            //         constant parameter 1
            //      /
            //      compound statement of 1
            //         function return                      ->CHANGE RETURN IN ASSIGNMENT OF RESULT
            //           compound statement of 5
            //               parameter a
            //               operation Multiply
            //               parameter b
            //               operation Multiply
            //               parameter c
            //            /
            //         /
            //      /
            //   /
            //
            //
            // result should be: 
            //
            //      assignment of c
            //         parameter b
            //         operation Substract
            //         constant parameter 1
            //      /
            //      compound statement of 1
            //         assignment of result
            //           compound statement of 5
            //               parameter a
            //               operation Multiply
            //               parameter b
            //               operation Multiply
            //               parameter c
            //            /
            //         /
            //      /

            //
            // > copy the inlined function nodes while scanning for <return> in the function.node
            //

            int i_insert = n.parent.parent.children.IndexOf(n.parent);
            node t = inline_nodes(function.Node);


            node inline_nodes(node current)
            {
                node inlined = new node(null);

                for (int i = 0; i < current.ChildCount; i++)
                {
                    node child = current.children[i];

                    if (child.IsFunction && child.function.ScriptOp == blast_operation.ret)
                    {
                        // the return
                        node new_return = child.DeepClone().FirstChild;
                        new_return.type = nodetype.compound;
                        new_return.identifier = n.identifier;
                        new_return.vector_size = n.vector_size;
                        new_return.is_vector = n.is_vector;
                        new_return.variable = n.variable;

                        // - TODO : check if its enforced that the return function is at the root level... TODO
                        node p = n.parent;
                        p.children.Clear();
                        p.SetChild(new_return); // .children.Add(new_return);

                        // any node after the return node is not used 
                        break;
                    }
                    else
                    {
                        if (child.IsCompound)
                        {
                            inlined.SetChild(inline_nodes(child));
                        }
                        else
                        {
                            inlined.SetChild(child.DeepClone(true));
                        }
                    }
                }

                return inlined;
            }

            bool remap_variables(node remapnode, Dictionary<string, node> map)
            {
                // get the id  
                string id = remapnode.identifier;

                // is it a parameter ?  
                if (!string.IsNullOrWhiteSpace(remapnode.identifier)
                    &&
                    map.TryGetValue(remapnode.identifier, out node vnode))
                {
                    remapnode.identifier = vnode.identifier;
                    if (vnode.is_constant)
                    {
                        remapnode.is_constant = true;
                        remapnode.constant_op = vnode.constant_op;
                        remapnode.variable = vnode.variable;
                        remapnode.vector_size = vnode.vector_size;
                        remapnode.is_vector = vnode.is_vector; 
                    }
                    else
                    {
                        remapnode.variable = vnode.variable;
                        remapnode.vector_size = vnode.vector_size;
                        remapnode.is_vector = vnode.is_vector;
                        vnode.variable.AddReference();
                    }
                }
                else
                {
                    for (int i = 0; i < remapnode.ChildCount; i++)
                    {
                        if (!remap_variables(remapnode.children[i], map))
                        {
                            return false;
                        }
                    }
                }
                return true;
            }

            bool resolve_identifiers(node resolvenode)
            {
                // check for any constant not setup correctly  

                //          tidi resolve param not connected in compiler 
                // any undeclared variable|identifier is an error at this point 
                return true;

            }

            // n is no longer attached to anything if correct but its parent field is still valid 

            // rebuild target node
            for (int i = 0; i < t.children.Count; i++)
            {
                node child = t.children[i];

                // skip null or empty nodes that resulted
                if (child == null || (child.type == nodetype.none && child.ChildCount == 0)) continue;

                n.parent.parent.children.Insert(i_insert + i, child);
                child.parent = n.parent.parent;

            }

            // now remap variables . ..
            // - parameters hold the variables from the callsite 
            // - translate these into the variable-names used in the inlined code


            // the preludes 
            if (!remap_variables(t, varmap))
            {
                return BlastError.error_inlinefunction_failed_to_remap_identifiers;
            }

            // the return / was replaced inline 
            if (!remap_variables(n.parent, varmap))
            {
                return BlastError.error_inlinefunction_failed_to_remap_identifiers;
            }

            // - we should run parameter analysis again on the remapped nodes 
            // - it could be the inlined function used constants that are not mapped yet
            // - we only need to do this on the FIRST inline.

            if (!resolve_identifiers(t) || !resolve_identifiers(n.parent))
            {
                return BlastError.error_inlinefunction_failed_to_remap_identifiers; 
            }

            // finally, we need to run transform again, the function might encode a for loop or switch
            transform(data, t);
            transform(data, n.parent);  

            return BlastError.success; 
        }


        /// <summary>
        /// classify the indexers on a node
        /// </summary>
        /// <remarks>
        /// 
        /// !!!! currently supports the following indexes only:
        /// 
        /// .[x|y|z|w]
        /// .[r|g|b|a]
        /// 
        /// </remarks>
        static public bool ClassifyIndexer(node n, out blast_operation op, out byte spec)
        {
            Assert.IsNotNull(n); 

            if( n.indexers != null 
                && 
                n.indexers.Count >= 2
                &&
                n.indexers[0].type == nodetype.index
                &&
                !string.IsNullOrWhiteSpace(n.indexers[1].identifier))
            {

                // determine length of chain
                if (n.indexers.Count == 2)
                {

                    // 1 contains index var
                    char index = n.indexers[1].identifier.ToLower().Trim()[0];
                    switch (index)
                    {
                        case 'x':
                        case 'r': op = blast_operation.index_x; spec = 0; return true; 
                                       
                        case 'y':
                        case 'g': op = blast_operation.index_y; spec = 0; return true; 

                        case 'z':
                        case 'b': op = blast_operation.index_z; spec = 0; return true; 

                        case 'w':
                        case 'a': op = blast_operation.index_w; spec = 0; return true; 
                    }                     
                }
                else
                {
                    //
                    // supporting all variations of xyzw would require additional encoding on the opcode index_n
                    // TODO 
                    //

                    op = blast_operation.index_n;
                    spec = 0;
                    return false; 
                }
            }
            op = blast_operation.nop;
            spec = 0;
            return false; 
        }


        /// <summary>
        /// only call this on nodes that actually have an indexer, for now create functions 
        /// for indices, we could allow the interpretor to directly index it but then we would
        /// lose all debugging features (we would get invalid metadata types on indexing it,
        /// because we index a f4 for example expecting a f1 back, the interpretor has no knowledge 
        /// of indexers and for now i'd like to keep it like that)
        /// </summary>
        /// <param name="data"></param>
        /// <param name="n"></param>
        /// <returns></returns>
        BlastError transform_indexer(IBlastCompilationData data, node n)
        {
            Assert.IsNotNull(n);
            Assert.IsTrue(n.HasIndexers, "only call transform_indexer on nodes with indexers!");
                        
            if(ClassifyIndexer(n, out blast_operation op, out byte spec))
            {
                if (n.IsAssignment)
                {
                    // setting a value, replace indexchain with a single node holding operation 
                    n.indexers.Clear();
                    n.AppendIndexer(BlastScriptToken.Indexer, op).EnsureIdentifierIsUniquelySet(); 
                }
                else
                {
                    // reading a value, remove indexers and  insert indexing function as new parent of current 
                    node f = new node(null, n);
                    f.EnsureIdentifierIsUniquelySet();
                    f.type = nodetype.function;


                    int idx = n.parent.children.IndexOf(n);

                    f.parent = n.parent;
                    n.parent = f;

                    f.parent.children.RemoveAt(idx);
                    f.parent.children.Insert(idx, f);

                    n.indexers.Clear();
                    unsafe
                    {
                        f.function = data.Blast.Data->GetFunction(op);
                    }
                    if (!f.function.IsValid)
                    {
                        data.LogError($"transform_indexer: failed to transform operation {op}, could not find corresponding function in api");
                        return BlastError.error_indexer_transform;
                    }
                }
            }
            else
            {
                data.LogError($"transform_indexer: failed to classify indexers on <{n}>, only .[xyzwrgba] are allowed");
                return BlastError.error_indexer_transform;
            }

            return BlastError.success; 
        }


        /// <summary>
        /// find and transform all indexers
        /// </summary>
        /// <param name="data"></param>
        /// <param name="ast_root"></param>
        /// <returns></returns>
        BlastError transform_indexers(IBlastCompilationData data, node ast_root)
        {
            Assert.IsNotNull(data);
            Assert.IsNotNull(ast_root);

            BlastError res = BlastError.success; 
            List<node> work = NodeListCache.Acquire(); 

            work.Push(ast_root);
            while (work.TryPop(out node current))
            {
                if(current.HasIndexers)
                {
                    res = transform_indexer(data, current);
                    if (res != BlastError.success) break;                     
                }

                work.PushRange(current.children);            
            }

            NodeListCache.Release(work);
            return res; 
        }


        /// <summary>
        /// run transform depending on nodetype 
        /// - TODO -> would be nice if this all returned errors.. 
        /// </summary>
        /// <param name="data"></param>
        /// <param name="n"></param>
        BlastError transform(IBlastCompilationData data, node n)
        {
            BlastError res = BlastError.success; 

            switch (n.type)
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

                case nodetype.function:
                    {
                        if (n.IsFunction && n.IsInlinedFunctionCall)
                        {
                            // inline the macro instead of the function call 
                            res = transform_inline_function_call(data, n);
                        }
                        break;
                    }

                case nodetype.inline_function:
                    {
                        // dont iterate through inline functions, only after they are inlined
                        return BlastError.success;
                    }


                default:
                    {
                        break;
                    }
            }

            if (res == BlastError.success)
            {
                foreach (node c in n.children.ToArray())
                {
                    res = transform(data, c);
                    if (res != BlastError.success) break;
                }
            }

            return res;
        }



        /// <summary>
        /// execute the transform stage:
        /// - merge compounds
        /// - transform for loops => while
        /// - transform switch => ifthen 
        /// - transfrom inlined functions 
        /// - transform indexers
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public int Execute(IBlastCompilationData data)
        {
            if (!data.IsOK || data.AST == null) return (int)BlastError.error;

            // transform node iterator 

            // recursively look for statements to transform
            BlastError res = transform(data, data.AST);
            if (res != BlastError.success)
            {
                return (int)res; 
            }

            // after expanding everything, check the tree for indexers 
            res = transform_indexers(data, data.AST); 
            if(res != BlastError.success)
            {
                return (int)res; 
            }

            return data.IsOK ? (int)BlastError.success : (int)BlastError.error;
        }
    }
}
