//##########################################################################################################
// Copyright © 2022 Rob Lemmens | NijnStein Software <rob.lemmens.s31@gmail.com> All Rights Reserved  ^__^\#
// Unauthorized copying of this file, via any medium is strictly prohibited                           (oo)\#
// Proprietary and confidential                                                                       (__) #
//##########################################################################################################
#if STANDALONE_VSBUILD
using NSS.Blast.Standalone;
using UnityEngine.Assertions;
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
    /// - [while, for, switch] into ifthen sequences 
    /// - vector expansions
    /// - inline functions 
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
            if (n.depends_on.Count > 0)
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
        static public bool ClassifyIndexer(node n, out blast_operation op)
        {
            Assert.IsNotNull(n);

            if (n.indexers != null && n.indexers.Count > 0)
            {
                // . indexers 
                if (n.indexers.Count == 2
                    &&
                    n.indexers[0].type == nodetype.index
                    &&
                    !string.IsNullOrWhiteSpace(n.indexers[1].identifier))
                {
                    // 1 contains index var
                    char index = n.indexers[1].identifier.ToLower().Trim()[0];
                    switch (index)
                    {
                        case 'x':
                        case 'r': op = blast_operation.index_x; return true;

                        case 'y':
                        case 'g': op = blast_operation.index_y; return true;

                        case 'z':
                        case 'b': op = blast_operation.index_z; return true;

                        case 'w':
                        case 'a': op = blast_operation.index_w; return true;
                    }
                }
                else
                // [ ] indexing 
                if (n.indexers.Count == 3  // could someday contain a sequence then this needs to allow more
                    &&
                    n.indexers[0].token == BlastScriptToken.IndexOpen
                    &&
                    n.indexers[n.indexers.Count - 1].token == BlastScriptToken.IndexClose
                    &&
                    !string.IsNullOrWhiteSpace(n.indexers[1].identifier))
                {
                    op = blast_operation.index_n;

                    // if we index with a constant 0 1 2 3 then replace op with the corresponding function 
                    node id = n.indexers[1];

                    if (id.identifier.Length == 1 || (id.identifier.Length > 2 && id.identifier[1] == '.'))
                    {
                        switch (id.identifier[0])
                        {
                            case '0': op = blast_operation.index_x; return true;
                            case '1': op = blast_operation.index_y; return true;
                            case '2': op = blast_operation.index_z; return true;
                            case '3': op = blast_operation.index_w; return true;
                        }
                    }
                    return true;
                }
            }

            op = blast_operation.nop;
            return false;
        }

        BlastError TransformIndexedAssignment(IBlastCompilationData data, node n, blast_operation op)
        {
            Assert.IsNotNull(n);
            Assert.IsTrue(n.HasIndexers, "only call on nodes with indexers!");

            if (op == blast_operation.index_n)
            {
                 // get the identifier between []
                node id = n.indexers[1];

                // reorder node
                n.indexers.Clear();
                n.AppendIndexer(BlastScriptToken.Indexer, op).EnsureIdentifierIsUniquelySet();

                n.indexers[0].children.Add(id);                

                return BlastError.error; 
            }
            else
            {
                // setting a value, replace indexchain with a single node holding operation 
                n.indexers.Clear();
                n.AppendIndexer(BlastScriptToken.Indexer, op).EnsureIdentifierIsUniquelySet();

                return BlastError.success;
            }
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
            Assert.IsTrue(n.HasIndexers, "only call on nodes with indexers!");

            if (ClassifyIndexer(n, out blast_operation op))
            {
                if (n.IsAssignment)
                {
                    return TransformIndexedAssignment(data, n, op); 
                }
                else
                {
                    // reading a value, remove indexers and insert indexing function as new parent of current 
                    // - this will insert: paramnode<function idx(param)> 
                    // - -> should we allow indexing on parameters to functions ? this saves a push pop 
                    // - ->  for now: modify nodetype to function
                    node f = new node(null, n);
                    f.EnsureIdentifierIsUniquelySet();
                    f.type = nodetype.function;

                    int idx = n.parent.children.IndexOf(n);

                    f.parent = n.parent;
                    n.parent = f;

                    f.parent.children.RemoveAt(idx);
                    f.parent.children.Insert(idx, f);

                    // get index variable before removing indexers if index_n
                    node id = null;
                    if(op == blast_operation.index_n)
                    {
                        id = n.indexers[1]; 
                    }
                    
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

                    if(id != null)
                    {
                        // set indexing parameter for index_n function
                        f.children.Add(id);

                        id.is_constant = false;
                        id.is_vector = false;
                        id.vector_size = 1; 
                        id.type = nodetype.parameter; 
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
                if (current.HasIndexers)
                {
                    bool allow_inline_indexer = data.CompilerOptions.InlineIndexers;
                    // if indexers are inlined, allow inline indexer definition on:
                    // - functions parameters
                    // - TODO : expand cases 
                    if (allow_inline_indexer)
                    {
                        if (current.parent == null || !current.parent.IsFunction)
                        {
                            allow_inline_indexer = false;
                        }
                    }

                    if (!allow_inline_indexer)
                    {
                        res = transform_indexer(data, current);
                        if (res != BlastError.success) break;
                    }
                }

                work.PushRange(current.children);
            }

            NodeListCache.Release(work);
            return res;
        }

        /// <summary>
        /// 
        /// replace a vector definition of equal elements into a functional expansion, this function assumes n is a non-nested vector definition 
        ///
        /// <code>
        /// (1 1 1) => expand3(1)                             
        /// (-1, -1, -1) == ((-1) (-1) (-1)) => expand3(-1)
        /// (a a a) => expand3(a) 
        /// </code>                              
        /// </summary>
        bool transform_into_vector_expansion(IBlastCompilationData data, node n)
        {
            if (n.ChildCount <= 1) return false;

            // examine nodes, all equal intent and:
            //
            // - constants 
            // - direct value offsets
            // - negated compound of constant 
            // - negated compound of variableoffset 
            //
            // nothing else is allowed
            // 

            bool is_constant;
            bool is_negation_compound;
            float id; // either from constant or variable 

            if (!classify_constant_vector_component(n.FirstChild, out is_constant, out id, out is_negation_compound))
            {
                return false;
            }

            for (int i = 1; i < n.children.Count; i++)
            {
                if (classify_constant_vector_component(n.children[i], out bool isc, out float id2, out bool isneg))
                {
                    if (isc == is_constant && id2 == id && isneg == is_negation_compound)
                    {
                        continue;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }

            // reaching here all children are the same and valid 
            // transform this node into a expand function 

            // - validate vectorsize -> if not a supported size we should report that 
            if (n.ChildCount > 4)
            {
                data.LogError($"blast.compiler.transform: vector size {n.ChildCount} is not supported in node {n}", (int)BlastError.error_vector_size_not_supported);
                return false;
            }


            int vector_size = n.ChildCount;

            n.type = nodetype.function;
            n.identifier = "expand" + vector_size.ToString();

            n.children.RemoveRange(1, vector_size - 1);
            n.variable = null;
            n.is_vector = true;
            n.vector_size = vector_size;
            n.token = BlastScriptToken.Nop;

            switch (vector_size)
            {
                case 2:
                    n.constant_op = blast_operation.expand_v2;
                    n.identifier = "expand2";
                    break;

                case 3:
                    n.constant_op = blast_operation.expand_v3;
                    n.identifier = "expand3";
                    break;

                case 4:
                    n.constant_op = blast_operation.expand_v4;
                    n.identifier = "expand4";
                    break;
            }

            unsafe
            {
                n.function = data.Blast.Data->GetFunction(n.identifier);
            }

            if (!n.function.IsValid)
            {
                data.LogError($"blast.compiler.transform: {n.identifier} function not found in current blast api");
                return false;
            }

            return true;
        }


        /// <summary>
        /// determine if a vectorcomponent is one of the following things: 
        /// - constant
        /// - offset 
        /// - or a negated compound of the above   
        /// </summary>
        bool classify_constant_vector_component(in node node, out bool is_constant, out float id, out bool is_negation_compound)
        {
            id = 0;
            is_constant = false;
            is_negation_compound = false;

            if (node.IsCompound)
            {
                is_negation_compound = true;

                if (node.ChildCount == 2
                    &&
                    node.FirstChild.IsOperation && node.FirstChild.token == BlastScriptToken.Substract)
                {
                    // second element may be a constant, a value and nothign more
                    if (node.LastChild.IsScriptVariable)
                    {
                        is_constant = false;
                        id = node.LastChild.variable.Id;
                        return true;
                    }
                    if (node.LastChild.is_constant)
                    {
                        is_constant = true;
                        id = node.LastChild.AsFloat;
                        return true;
                    }
                }
            }
            else
            {
                if (node.ChildCount == 0)
                {
                    if (node.is_constant)
                    {
                        is_constant = true;
                        switch(node.datatype)
                        {
                            case BlastVariableDataType.Numeric: id = node.AsFloat; return true;
                            case BlastVariableDataType.Bool32: id = Bool32.From(node.identifier).Single; return true; 
                        }
                        id = node.AsFloat;
                        return true;
                    }
                    if (node.IsScriptVariable)
                    {
                        is_constant = false;
                        id = node.variable.Id;
                        return true;
                    }
                }
            }
            return false;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        /// <param name="ast_root"></param>
        /// <returns></returns>
        BlastError locate_and_transform_vector_expansions(IBlastCompilationData data, node ast_root)
        {
            Assert.IsNotNull(data);
            Assert.IsNotNull(ast_root);

            BlastError res = BlastError.success;
            List<node> work = NodeListCache.Acquire();

            work.Push(ast_root);
            while (work.TryPop(out node current))
            {
                if (current.IsCompound && current.IsNonNestedVectorDefinition())
                {
                    transform_into_vector_expansion(data, current);
                }
                else
                {
                    work.PushRange(current.children);
                }
            }

            NodeListCache.Release(work);
            return res;
        }


        /// <summary>
        /// transform an assigment of zero into the function call: zero(id) 
        /// - saves 1 byte of code but also some memory copies associated with moving registerdata around
        /// - makes it easier for the interpretor to validate stuff while running 
        /// </summary>
        BlastError transform_zero_assignment(IBlastCompilationData data, node n)
        {
            Assert.IsNotNull(data);
            Assert.IsNotNull(n);

            BlastError res = BlastError.success;

            // assignment of something -> target for zero(id) replacement 
            if (n.HasOneChild && n.IsAssignment)
            {
                node child = n.FirstChild;

                // iterate through the leaf of all single node compounds 
                while (child != null && child.IsCompound && child.HasOneChild) child = child.FirstChild;

                // if the single child is a constant zero then we can replace it with zero(id) 
                if (!child.HasChildren && child.is_constant)
                {
                    if (child.constant_op == blast_operation.value_0)
                    {
                        // n being an assignment, it has a variable attached with vectorsize
                        Assert.IsNotNull(n.variable); 
                        int vector_size = n.variable.VectorSize;

                        // node n is an assignment of zero scalar 
                        ReplaceAssignmentWithZero(data, n);

                        n.vector_size = vector_size; 
                        n.FirstChild.vector_size = vector_size;
                        n.FirstChild.variable.VectorSize = vector_size;
                    }
                }
                else
                if(child.ChildCount > 0)
                {
                    // we could have handled both cases in the same way but in this case i would like them seperate 

                    // if all children are constant zero 
                    bool zero = true; 

                    for(int i = 0; i < child.ChildCount && zero; i++)
                    {
                        node gc = child.children[i];
                        zero = gc.ChildCount == 0 && gc.is_constant && gc.constant_op == blast_operation.value_0;
                    }

                    int vector_size = child.ChildCount; 

                    if(zero)
                    {
                        // node n is an assignment of a zero vector 
                        ReplaceAssignmentWithZero(data, n);

                        n.vector_size = vector_size;
                        n.FirstChild.vector_size = vector_size; 
                        n.FirstChild.variable.VectorSize = vector_size; 
                    }
                }
            }                 

            return res; 
        }

        /// <summary>
        /// replace the assingment in node n with a call to the zero function saving some bytes in code
        /// </summary>
        /// <param name="data"></param>
        /// <param name="n"></param>
        private static void ReplaceAssignmentWithZero(IBlastCompilationData data, node n)
        {
            Assert.IsNotNull(n); 

            n.children.Clear();
            n.type = nodetype.function;
            unsafe
            {
                n.function = data.Blast.Data->GetFunction("zero");
            }
            // create a child parameter from assignee
            node parameter = n.CreateChild(nodetype.parameter, BlastScriptToken.Nop, n.identifier);
            parameter.variable = n.variable;
            parameter.datatype = n.datatype;
            parameter.vector_size = n.vector_size;
            n.variable = null;
            n.identifier = "zero";
        }



        /// <summary>
        /// create nodes foreach constant cdata element 
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        BlastError transform_constant_cdata(IBlastCompilationData data)
        {
            Assert.IsNotNull(data);
            Assert.IsNotNull(data.AST);

            if (data.VariableCount > 0)
            {
                int n_cdata = 0; 

                foreach (BlastVariable v in data.Variables)
                {
                    if (v.IsConstant && v.IsCData)
                    {
                        if (v.ConstantData == null)
                        {
                            data.LogError($"Blast.Transform.transform_constant_cdata: constantdata null in variable: {v.Name}");
                            return BlastError.error; 
                        }

                        // create a node to compile into  jump [jumpoffset] [lenght 16bits] [cdata bytes]
                        node n = new node(null, null); 
                        n.type = nodetype.cdata;
                        n.variable = v;
                        n.vector_size = v.VectorSize;
                        n.is_constant = true;

                        // insert the node into the front of the ast                         
                        data.AST.children.Insert(n_cdata, n);
                        n_cdata++; 

                        // replace all references to the variable with constant cdata 
                        // with a reference to this node in a later stage 
                    }
                }
            }

            return BlastError.success;
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

                case nodetype.assignment:
                    {
                        res = transform_zero_assignment(data, n);
                        if (res != BlastError.success) return res; 
                    }
                    break; 

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
            BlastError res = transform_constant_cdata(data);
            if (res != BlastError.success)
            {
                return (int)res;
            }

            // recursively look for statements to transform
            res = transform(data, data.AST);
            if (res != BlastError.success)
            {
                return (int)res;
            }

            // after expanding everything, check the tree for indexers 
            res = transform_indexers(data, data.AST);
            if (res != BlastError.success)
            {
                return (int)res;
            }

            // check for possible replacements of vector defines into expandn functions 
            res = locate_and_transform_vector_expansions(data, data.AST);
            if (res != BlastError.success)
            {
                return (int)res;
            }

            return data.IsOK ? (int)BlastError.success : (int)BlastError.error;
        }
    }
}
