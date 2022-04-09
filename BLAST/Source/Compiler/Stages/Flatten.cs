//##########################################################################################################
// Copyright © 2022 Rob Lemmens | NijnStein Software <rob.lemmens.s31@gmail.com> All Rights Reserved  ^__^\#
// Unauthorized copying of this file, via any medium is strictly prohibited                           (oo)\#
// Proprietary and confidential                                                                       (__) #
//##########################################################################################################
using System;
using System.Collections.Generic;

#if STANDALONE_VSBUILD
using NSS.Blast.Standalone;
#else
using UnityEngine;
#endif

using UnityEngine.Assertions;

namespace NSS.Blast.Compiler.Stage
{
    /// <summary>
    /// Flatten Nodes 
    /// - removes all nested things and reduces them to a linear stream of instructions using stack operations 
    /// </summary>
    public class BlastFlatten : IBlastCompilerStage
    {
        /// <summary>
        /// Version 1
        /// </summary>
        public Version Version => new Version(0, 1, 0);

        /// <summary>
        /// Flatten Stage
        /// </summary>
        public BlastCompilerStageType StageType => BlastCompilerStageType.Flatten;

        /// <summary>
        /// flatten a compound 
        /// </summary>
        /// <param name="data"></param>
        /// <param name="compound"></param>
        /// <param name="flattened_output"></param>
        /// <returns></returns>
        public BlastError FlattenCompound(IBlastCompilationData data, in node compound, out List<node> flattened_output)
        {
            Assert.IsNotNull(compound);

            flattened_output = new List<node>();

            // does the compound contain nested compounds? we will reduce them all
            int i = 0;
            while(i < compound.ChildCount)
            {
                node child = compound.children[i];
                switch (child.type)
                {
                    case nodetype.function:
                        // dont further flatten stack operations
                        if (!(child.function.IsPopVariant || child.function.IsPushVariant))
                        {
                            //
                            // should we allow indexing functions to persist? they are leafs by definition, it would ad a layer to pops... TODO 
                            //

                            // this must be flattened out 
                            BlastError res;
                            if (BlastError.success == (res = FlattenFunction(data, child, out List<node> flattened_output_of_compound, true, out node pusher)))
                            {
                                // insert in output 
                                flattened_output.InsertRange(0, flattened_output_of_compound);

                                // replace flattened function node with the newly created pop node linked to the push
                                compound.children[i] = node.CreatePopNode(data.Blast, pusher);
                                compound.children[i].parent = compound;
                            }
                            else
                            {
                                data.LogError($"Flatten.Compound: failed to flatten child function: <{compound}>.<{child}>", (int)res);
                                return res;
                            }
                        }
                        break; 

                    case nodetype.operation:
                        // todo  should filter on allowed ops 
                        break;

                    case nodetype.parameter:
                        break;

                    case nodetype.compound:
                        // append this node to flattened
                        node push = CreatePushNodeAndAddChildsChildren(data, child);

                        // child.parent = push;
                        //  push.children.Add(child);

                        // insert nodes to beginning of output
                        flattened_output.Insert(0, push);

                        // replace function parameter with the newly created pop node linked to the push
                        node pop = node.CreatePopNode(data.Blast, push);
                        compound.children[i] = pop;
                        pop.parent = compound;
                        break;
                }
                i++; 
            }

            return BlastError.success;
        }


        /// <summary>
        /// flatten a list of function parameters 
        /// </summary>
        /// <param name="data"></param>
        /// <param name="function"></param>
        /// <param name="flattened_output"></param>
        /// <returns></returns>
        public BlastError FlattenFunctionParameters(IBlastCompilationData data, in node function, out List<node> flattened_output)
        {
            Assert.IsNotNull(function);
            flattened_output = new List<node>();

            for (int i = 0; i < function.ChildCount; i++)
            {
                node child = function.children[i];

                // a node is flat when: 
                // - not a function 
                // - not list of operations (or compound)
                // - IS single parameter 

                // check if children of child form an operation list, if so push it as compound 
                if (!child.IsCompound && node.IsOperationList(child))
                {
                    //
                    // probably wont ever reach here anymore after !child.IsCompound is added to above condition
                    //

                    List<node> flat_compound_nodes;

                    BlastError res = FlattenCompound(data, child, out flat_compound_nodes);
                    if (res != BlastError.success)
                    {
                        data.LogError($"flatten.FlattenFunction: error flattening operation list in compound parameter: <{child}> of node <{function.parent}>.<{function}>, returned error {res}");
                        return res;
                    }

                    if (flat_compound_nodes == null || flat_compound_nodes.Count == 0)
                    {
                        node push;
                        // its an already flat compound that can be pushed as a whole
                        if (child.IsNonNestedVectorDefinition())
                        {
                            push = CreatePushNodeAndAddChild(data, child);
                        }
                        else
                        {
                            push = node.CreatePushNode(data.Blast, child);
                            if (child.IsCompound)
                            {
                                // add children of compound 
                                push.children.AddRange(child.children);
                                foreach (node pch in push.children) pch.parent = push;
                            }
                            else
                            {
                                push.children.Add(child);
                                child.parent = push;
                            }
                        }
                        flattened_output.Insert(0, push);

                        // need to replace function param with pop op  
                        node pop = node.CreatePopNode(data.Blast, push);
                        pop.parent = function;
                        function.children[i] = pop;
                    }
                    else
                    {
                        flattened_output.InsertRange(0, flat_compound_nodes);
                    }

                    // param in function is replace, function not rendered yet
                    // - if the parameter left is a vector, it should also be pushed 

                }
                else
                {
                    if ((child.IsFunction && !child.IsPopFunction) || child.IsCompound)
                    {
                        
                        switch (child.type)
                        {
                            case nodetype.function:
                                {
                                    List<node> flatten_inner_function;
                                    node pusher_for_function;

                                    BlastError res = FlattenFunction(data, child, out flatten_inner_function, true, out pusher_for_function);
                                    if (res != BlastError.success)
                                    {
                                        data.LogError($"flatten.FlattenFunction: error flattening function parameter: <{child}> of node <{function.parent}>.<{function}>, returned error {res}");
                                        return res;
                                    }

                                    flattened_output.InsertRange(0, flatten_inner_function);

                                    node pop = node.CreatePopNode(data.Blast, pusher_for_function);
                                    function.children[i] = pop;
                                    pop.parent = function;
                                    break;
                                }

                            case nodetype.compound:
                                {
                                    //
                                    //  FLATTEN ANY COMPOUND !!!   TEST TILL DEATH
                                    // 
                                    List<node> flattened_compound; 
                                    BlastError res = FlattenCompound(data, child, out flattened_compound);
                                    if (res != BlastError.success)
                                    {
                                        data.LogError($"flatten.FlattenFunction.Compound: error flattening function parameter: <{child}> of node <{function.parent}>.<{function}>, returned error {res}");
                                        return res;
                                    }

                                    node push = CreatePushNodeAndAddChild(data, child);

                                    if (flattened_compound != null && flattened_compound.Count > 0)
                                    {
                                        flattened_compound.Add(push);
                                        flattened_output.InsertRange(0, flattened_compound); 
                                    }
                                    else
                                    {
                                        // insert nodes to beginning of output
                                        flattened_output.Insert(0, push);
                                    }

                                    // replace function parameter with the newly created pop node linked to the push
                                    node pop = node.CreatePopNode(data.Blast, push);
                                    function.children[i] = pop;
                                    pop.parent = function;

                                    break;
                                }
                        }
                    }
                    else
                    {
                        // at this point we should allow only single values or pop, anything else is no good. 
                        if (child.ChildCount != 0 || child.type != nodetype.parameter)
                        {
                            data.LogError($"flatten: failed to flatten function parameter <{function}>.<{child}>");
                            return BlastError.error_failed_to_flatten_function_parameters;
                        }
                        // node doesnt change 
                    }
                }
            }

            return BlastError.success; 
        }


        /// <summary>
        /// flatten a function into a list of flat operations, its optional to push the resulting function to the end of the list and return the pushing node reference 
        /// </summary>
        /// <param name="data"></param>
        /// <param name="function"></param>
        /// <param name="flattened_output"></param>
        /// <param name="push_function"></param>
        /// <param name="pusher"></param>
        /// <returns></returns>
        public BlastError FlattenFunction(IBlastCompilationData data, in node function, out List<node> flattened_output, bool push_function, out node pusher)
        {
            Assert.IsNotNull(function);
            
            flattened_output = new List<node>();
            pusher = null;

            if (!function.IsFunction || function.function.FunctionId <= 0)
            {
                return BlastError.error_node_function_unknown;
            }

            // scan parameters:
            // - if a simple parameter list: nothing needs to be done, we can move the node asis
            bool flat = true;
            foreach (node param in function.children)
            {
                if (param.type == nodetype.parameter && param.ChildCount == 1)
                {
                    continue;
                }

                if (param.IsCompound || (param.IsFunction && !param.function.IsPopVariant) || param.IsOperation)
                {
                    flat = false;
                    break;
                }
            }
            // functions that have a single vector define as parameter are also flat 
            if (!flat && function.children.Count == 1 && function.FirstChild.IsNonNestedVectorDefinition())
            {
                flat = true;
            }

            // if flat => add to flat node list and exit 
            if (!flat)
            {
                // is the function fed an operation list as list of children => push it as compound 
                if (node.IsOperationList(function))
                {
                    List<node> flat_compound_nodes;

                    BlastError res = FlattenCompound(data, function, out flat_compound_nodes);
                    if (res != BlastError.success)
                    {
                        data.LogError($"flatten.FlattenFunction: error flattening operation list into a stacked compounded parameter from node <{function.parent}>.<{function}>, returned error {res}");
                        return res;
                    }

                    // if anything resulted in pushes add them (flattened_output should be empty at this point)  
                    flattened_output.AddRange(flat_compound_nodes);

                    // push remaining child list as operation push 
                    node push_ol  = node.CreatePushNode(data.Blast, function);
                    push_ol.children.AddRange(function.children);
                    foreach (node c_ol in push_ol.children) c_ol.parent = push_ol;
                    
                    function.children.Clear();

                    node pop_ol = node.CreatePopNode(data.Blast, push_ol);
                    pop_ol.parent = function;
                    function.children.Add(pop_ol); 
                    flattened_output.Add(push_ol);                 
                }
                else
                {
                    // else we go through each parameter and flatten it
                    BlastError res = FlattenFunctionParameters(data, function, out flattened_output); // overwrites (but should be empty anyway)
                    if (res != BlastError.success)
                    {
                        data.LogError($"flatten.FlattenFunction: error flattening function parameters in node <{function.parent}>.<{function}>, returned error {res}");
                        return res;
                    }
                }
            }

            // add function to output
            if (push_function)
            {
                pusher = CreatePushNodeAndAddChild(data, function); 
                flattened_output.Add(pusher); 
            }
            else
            {
                flattened_output.Add(function);
            }

            // all ok 
            return BlastError.success;
        }

        private static node CreatePushNodeAndAddChild(IBlastCompilationData data, node child)
        {
            node push = node.CreatePushNode(data.Blast, child);

            child.parent = push;
            push.children.Add(child);

            return push;
        }

        private static node CreatePushNodeAndAddChildsChildren(IBlastCompilationData data, node child)
        {
            node push = node.CreatePushNode(data.Blast, child);
            
            push.children.AddRange(child.children);
            foreach (node pch in push.children) pch.parent = push;

            return push; 
        }


        /// <summary>
        /// flatten assignement node 
        /// </summary>
        public BlastError FlattenAssignment(IBlastCompilationData data, in node assignment, out List<node> flattened_output, bool push_function, out node pusher)
        {
            Assert.IsNotNull(assignment);
            Assert.IsTrue(assignment.type == nodetype.assignment); 

            BlastError res;
            flattened_output = new List<node>();
            pusher = null;
            List<node> flat = new List<node>();

            int iter = 0;

            // special cases
            // - assigning single value     => compiler transforms into assignS - done 
            // - assigning function         => compiler transforms into assignF|FN|FE|FEN - done
            // - assigning vector        ?                                       

            // check for assigning a vector, first find out if the node is even a vector
            if (assignment.is_vector)
            {
                // assignment of a vector, should be n vectorsize children in this node 
                node n = assignment;
                while (n.HasSingleCompoundAsChild()) n = n.FirstChild;

                int total_vector_size = 0;

                if (n.HasChild(nodetype.operation) && n.IsOperationList())
                {
                    // the assignment node has at least 1 child with an operation 
                    total_vector_size = n.DetermineSequenceVectorSize();
                }
                else
                if(n.HasOneChild && n.FirstChild.IsFunction)
                {
                    total_vector_size = n.FirstChild.GetFunctionResultVectorSize(); 
                }
                else
                {
                    // this node should be n
                    if (n.ChildCount == assignment.vector_size)
                    {
                        // verify assignee variable is a vector

                        // every child should be a negation, value or function 
                        for (int i = 0; i < n.ChildCount; i++)
                        {
                            node c = n.children[i];
                            if (c.IsCompoundWithSingleNegationOfValue())
                            {
                                total_vector_size += c.GetNegatedCompoundVectorSize();
                            }
                            else
                            if (c.type == nodetype.parameter)
                            {
                                if (c.HasVariable)
                                {
                                    total_vector_size += c.variable.VectorSize;
                                }
                                else
                                if (c.IsConstantValue)
                                {
                                    // constant value
                                    total_vector_size += 1;
                                }
                                else
                                if (c.type == nodetype.function)
                                {
                                    total_vector_size += c.GetFunctionResultVectorSize();
                                }
                            }
                            else
                            if (c.type == nodetype.function)
                            {
                                total_vector_size += c.GetFunctionResultVectorSize();
                            }
                            else
                            {
                                // vector define error  -> THIS MUST BE CORRECT AFTER MAPIDENTIFIERS 
                                data.LogError($"Blast.Compiler.FlattenAssignment: failed to check vectorsize of assignment node: <{assignment}>");
                                return BlastError.error_assign_vector_size_mismatch;
                            }
                        }

                    }
                }

                // check the calculated total size 
                if (total_vector_size != n.vector_size)
                {
                    // error vectorsize mismatch  -> THIS MUST BE CORRECT AFTER MAPIDENTIFIERS 
                    data.LogError($"Blast.Compiler.FlattenAssignment: vectorsize mismatch, assignment set to size {assignment.vector_size} while flattener estimated a size of {total_vector_size}");
                    return BlastError.error_assign_vector_size_mismatch;
                }
            }


            // check size from any variable set to the assignment node 
            int vsize = assignment.HasIndexers ? 1 : (assignment.HasVariable ? assignment.variable.VectorSize : 1); 
            if (vsize != assignment.vector_size) 
            {
                if (assignment.vector_size == 0)
                {
                    assignment.vector_size = vsize;
                }
                else
                {
                    // error vectorsize mismatch in assignment  -> THIS MUST BE CORRECT AFTER MAPIDENTIFIERS 
                    data.LogError($"Blast.Compiler.FlattenAssignment: vectorsize mismatch, assignment set to size {assignment.vector_size} while the assigned variable {assignment.variable.Name} is of size {assignment.variable.VectorSize}");
                    return BlastError.error_assign_vector_size_mismatch;
                }
            }

            // 
            do
            {
                iter++; 

                for (int i = 0; i < assignment.children.Count; i++)
                {
                    node child = assignment.children[i];

                    // any compound nesting should have been resolved, any compound left should be there for a reason and we should keep it
                    switch (child.type)
                    {
                        case nodetype.compound:
                            {
                                List<node> flattened_output_of_compound;
                                res = FlattenCompound(data, child, out flattened_output_of_compound);
                                if (res != BlastError.success)
                                {
                                    data.LogError($"flatten.assignment: failed to flatten child compound: <{assignment.parent}>.<{assignment}>.<{child}>, error = {res}");
                                    return res;
                                }

                                // if there was no output flattened, push whole compound 
                                if (flattened_output_of_compound == null || flattened_output_of_compound.Count == 0)
                                {
                                    if (node.IsNegationOfValue(child, false))
                                    {
                                        // node stays
                                    }
                                    else
                                    {
                                        // else we need to push it 
                                        node push;
                                        if (child.IsNonNestedVectorDefinition())
                                        {
                                            // pushv vector 
                                            push = CreatePushNodeAndAddChild(data, child);
                                        }
                                        else
                                        {
                                            // pushc whole compound without the compound 
                                            push = CreatePushNodeAndAddChildsChildren(data, child);
                                        }
                                        flat.Insert(0, push);

                                        // and pop back its result in child
                                        node pop = node.CreatePopNode(data.Blast, push);

                                        assignment.children[i] = pop;
                                        pop.parent = assignment;
                                    }
                                }
                                else
                                {
                                    flat.InsertRange(0, flattened_output_of_compound);                                    
                                }
                            }
                            break;

                        case nodetype.function:
                            {
                                List<node> flattened_output_of_function;

                                
#pragma warning disable CS1587 // XML comment is not placed on a valid language element
///
                                /// need to account for operation lists
                                ///

                                res = FlattenFunctionParameters(data, child, out flattened_output_of_function);
#pragma warning restore CS1587 // XML comment is not placed on a valid language element
                                if (res != BlastError.success)
                                {
                                    data.LogError($"flatten.assignment: failed to flatten child function: <{assignment.parent}>.<{assignment}>.<{child}>, error = {res}");
                                    return res;
                                }

                                if (flattened_output_of_function != null && flattened_output_of_function.Count > 0)
                                {
                                    flat.InsertRange(0, flattened_output_of_function);
                                }

                                // now that which is left, is it a simple parameter list of operation list?
                                if (node.IsFlatParameterList(child.children))
                                {
                                    // ok
                                }
                                else
                                {
                                   // extract and push as compound 
                                    node push = CreatePushNodeAndAddChildsChildren(data, child);
                                    flat.Add(push);
                                    
                                    // replace children with pop
                                    node pop = node.CreatePopNode(data.Blast, push);
                                    child.children.Clear();
                                    child.SetChild(pop);
                                }
                            }
                            break;

                    }
                }
            }
            while (!assignment.IsFlat(false, true) && iter < 10);

            // probably should remove this but it was never the intention to have this.. 
            if(iter == 10)
            {
                data.LogError("compiler: too much nesting or couldnt resolve nested assignment.");
                return BlastError.error; 
            }

            // now run through the flattened list
            foreach(node f in flat)
            {
                // check flatness of each node, not all code paths handle depth too much extend 
                // - we could update flattencompound to recursively iterate in any depth... 
                // - we allow vector compounds, but not the (-v) that can be beneath or any other compound
                // - functions handle recursion like: flattenfunction -> flattenparameters -> flattenfuction (3 stage recursion) 
                // 
                // - is is nice to have a check here, even if compound was updated it would be a nice debug step 
                //
                if(!f.IsFlat(true, true))
                {
                    iter = 0; 
                    do
                    {
                        List<node> nodes;
                        node flatten = f.IsNonNestedVectorDefinition() ? f.FirstChild : f;

                        if ((res = FlattenCompound(data, f, out nodes)) != BlastError.success || iter++ >= 10)
                        {
                            data.LogError("compiler: too much nesting or couldnt resolve nested compound in assignment.");
                            return BlastError.error;
                        }
                    }
                    while (!f.IsFlat(true, true)); 
                }
            }


            // reconstruct push-pop order based on linkages in push pop                        
            flattened_output.Add(assignment);

            node current = assignment; 

            while(current != null)
            {
                if(current.linked_push != null)
                {
                   // flattened_output.Insert(0, current.linked_push);
                    flattened_output.Insert(flattened_output.IndexOf(current), current.linked_push);
                    flat.Remove(current.linked_push); 
                }

                int nthpop = 0; 

                foreach(node child in current.children)
                {
                    if(child.linked_push != null)
                    {
                        flattened_output.Insert(flattened_output.IndexOf(current) - nthpop, child.linked_push);
                        flat.Remove(child.linked_push);
                        
                        nthpop++; 
                    }
                    
#pragma warning disable CS1587 // XML comment is not placed on a valid language element
/// there is max 1 compound below
                    foreach(node gchild in child.children)
#pragma warning restore CS1587 // XML comment is not placed on a valid language element
                    {
                        if (gchild.linked_push != null)
                        {
                            flattened_output.Insert(flattened_output.IndexOf(current) - nthpop, gchild.linked_push);
                            flat.Remove(gchild.linked_push);

                            nthpop++; 
                        }
                    }
                }

                int idx = flattened_output.IndexOf(current) - 1;
                if (idx >= 0) current = flattened_output[idx];
                else current = null; 
            }
            
            // check if all nodes were used. if not that would be a big error
            if (flat.Count != 0)
            {
                data.LogError("flatten.assignment: failed to resolve push-pop heirarchy");
                return BlastError.error; 
            }

            return BlastError.success;
        }

        /// <summary>
        /// flatten a while loop (which might originally have been a for loop dueue to transform stage) 
        /// - pops away operations from condition 
        /// - generates flat list of statements for loop compound 
        /// - maintains same root object 
        /// --- THE WHILELOOP SHOULD BE EXCLUDED FROM NESTING TESTS
        /// --- while this nests in the node tree it nests control flow and that will flatten out when compiling into jumps
        /// </summary>
        public BlastError FlattenWhileLoop(IBlastCompilationData data, node while_root)
        {
            List<node> flattened_condition = new List<node>();
            List<node> flattened_compound = new List<node>();

            node condition = while_root.GetChild(nodetype.condition);
            Assert.IsNotNull(condition); 

            // the condition 
            BlastError res = FlattenCompound(data, condition, out flattened_condition);
            if (res != BlastError.success)
            {
                data.LogError($"FlattenWhileLoop: failed to flatten while condition <{while_root}>.<{condition}>");
                return res; 
            }
            if (flattened_condition.Count == 0)
            {
                flattened_condition.AddRange(flattened_condition);
            }
            flattened_condition.Add(condition);

            // the compound 
            node compound = while_root.GetChild(nodetype.whilecompound);
            res = FlattenStatements(data, compound, out flattened_compound);
            if (res != BlastError.success)
            {
                data.LogError($"FlattenWhileLoop: failed to flatten while compund <{while_root}>.<{compound}>, error: {res}");
                return res;
            }
            if(flattened_compound.Count == 0)
            {
                data.LogError($"FlattenWhileLoop: failed to flatten while compund into statement list: <{while_root}>.<{compound}>");
                return BlastError.error; 
            }

            // handle returned 
            while_root.children.Clear();
            if (flattened_condition.Count == 1)
            {
                node new_condition = flattened_condition[0];
                if (new_condition.type == nodetype.condition)
                {
                    while_root.SetChild(new_condition);
                }
                else
                {
                    new_condition = new node(nodetype.condition, BlastScriptToken.Nop);
                    new_condition.EnsureIdentifierIsUniquelySet();
                    new_condition.SetChildren(flattened_condition);
                    while_root.children.Add(new_condition);
                }

            }
            else
            {
                node new_condition = new node(nodetype.condition, BlastScriptToken.Nop);
                new_condition.EnsureIdentifierIsUniquelySet(); 
                new_condition.SetChildren(flattened_condition);
                while_root.children.Add(new_condition); 
            }

            if(flattened_compound.Count > 0)
            {
                node new_compound = new node(nodetype.whilecompound, BlastScriptToken.Nop);
                new_compound.EnsureIdentifierIsUniquelySet(); 
                new_compound.SetChildren(flattened_compound);
                while_root.children.Add(new_compound); 
            }

            return BlastError.success; 
        }

        /// <summary>
        /// flatten an ifthenelse construct 
        /// </summary>
        BlastError FlattenIfThenElse(IBlastCompilationData data, node ifthenelse)
        {
            List<node> flattened_condition = new List<node>();
            List<node> flattened_then = new List<node>();
            List<node> flattened_else = new List<node>();

            node condition = ifthenelse.GetChild(nodetype.condition);
            Assert.IsNotNull(condition);

            // the condition 
            BlastError res = FlattenCompound(data, condition, out flattened_condition);
            if (res != BlastError.success)
            {
                data.LogError($"FlattenIfThenElse: failed to flatten if condition <{ifthenelse}>.<{condition}>");
                return res;
            }
            if (flattened_condition.Count == 0)
            {
                flattened_condition.AddRange(flattened_condition);
            }
            flattened_condition.Add(condition);

            // the ifthen 
            node ifthen = ifthenelse.GetChild(nodetype.ifthen);
            if (ifthen != null)
            {
                res = FlattenStatements(data, ifthen, out flattened_then);
                if (res != BlastError.success)
                {
                    data.LogError($"FlattenIfThenElse: failed to flatten then compound <{ifthenelse}>.<{ifthen}>, error: {res}");
                    return res;
                }
                if (flattened_then.Count == 0)
                {
                    data.LogError($"FlattenIfThenElse: failed to flatten then compund into statement list: <{ifthenelse}>.<{ifthen}>");
                    return BlastError.error;
                }
            }

            // the ifelse
            node ifelse = ifthenelse.GetChild(nodetype.ifelse);
            if (ifelse != null)
            {
                res = FlattenStatements(data, ifelse, out flattened_else);
                if (res != BlastError.success)
                {
                    data.LogError($"FlattenIfThenElse: failed to flatten else compound <{ifthenelse}>.<{ifelse}>, error: {res}");
                    return res;
                }
                if (flattened_else.Count == 0)
                {
                    data.LogError($"FlattenIfThenElse: failed to flatten else compund into statement list: <{ifthenelse}>.<{ifelse}>");
                    return BlastError.error;
                }
            }

            // reconstruct 
            ifthenelse.children.Clear();
            if (flattened_condition.Count == 1)
            {
                node new_condition = flattened_condition[0];
                if (new_condition.type == nodetype.condition)
                {
                    ifthenelse.SetChild(new_condition);
                }
                else
                {
                    new_condition = new node(nodetype.condition, BlastScriptToken.Nop);
                    new_condition.EnsureIdentifierIsUniquelySet();
                    new_condition.SetChildren(flattened_condition);
                    ifthenelse.children.Add(new_condition);
                }

            }
            else
            {
                node new_condition = new node(nodetype.condition, BlastScriptToken.Nop);
                new_condition.EnsureIdentifierIsUniquelySet();
                new_condition.SetChildren(flattened_condition);
                ifthenelse.children.Add(new_condition);
            }

            if (flattened_then.Count > 0)
            {
                node new_compound = new node(nodetype.ifthen, BlastScriptToken.Nop);
                new_compound.EnsureIdentifierIsUniquelySet();
                new_compound.SetChildren(flattened_then);
                ifthenelse.children.Add(new_compound);
            }

            if (flattened_else.Count > 0)
            {
                node new_compound = new node(nodetype.ifelse, BlastScriptToken.Nop);
                new_compound.EnsureIdentifierIsUniquelySet();
                new_compound.SetChildren(flattened_else);
                ifthenelse.children.Add(new_compound);
            }

            return BlastError.success;
        }


        /// <summary>
        /// Flatten the statement
        /// </summary>
        /// <param name="data"></param>
        /// <param name="root"></param>
        /// <param name="flat"></param>
        /// <returns></returns>
        BlastError FlattenStatements(IBlastCompilationData data, node root, out List<node> flat)
        {
            BlastError res; 
            flat = new List<node>();
            List<node> output = new List<node>();
            node pusher = null; 
            
            foreach(node input_node in root.children)
            {
                if (input_node.IsInlinedFunction)
                {
                    res = FlattenRoot(data, input_node);
                    if (res != BlastError.success)
                    {
                        data.LogError($"flatten: error flattening inlined function:  <{input_node.parent}>.<{input_node}>[{input_node.ChildCount}], errorcode: {res}");
                        return res;
                    }

                    flat.Add(input_node);
                }
                else
                {
                    // remove any unneeded compounds from node and its children (note that this function may return a child of the input node if the parent was useless) 
                    node node = node.ReduceSingularCompounds(input_node);

                    // depending on nodetype things differ
                    switch (node.type)
                    {
                        case nodetype.assignment:
                            res = FlattenAssignment(data, in node, out output, false, out pusher);
                            if (res != BlastError.success)
                            {
                                data.LogError($"flatten: error flattening assignment: <{node.parent}>.<{node}>[{node.ChildCount}], errorcode: {res}");
                                return res;
                            }

                            flat.AddRange(output);
                            break;

                        // a while loop will always be either on the root, inside ifthen or inside while, in any case
                        // it will be handled by this loop and we dont need to worry about flattening its return
                        case nodetype.whileloop:
                            res = FlattenWhileLoop(data, node);
                            if (res != BlastError.success)
                            {
                                data.LogError($"flatten: error flattening while loop: <{node.parent}>.<{node}>[{node.ChildCount}], errorcode: {res}");
                                return res;
                            }

                            flat.Add(node);
                            break;

                        // same story as with while, these will not be flat in the root 
                        case nodetype.ifthenelse:
                            res = FlattenIfThenElse(data, node);
                            if (res != BlastError.success)
                            {
                                data.LogError($"flatten: error flattening if-then-else construct: <{node.parent}>.<{node}>[{node.ChildCount}], errorcode: {res}");
                                return res;
                            }

                            flat.Add(node);
                            break;

                        case nodetype.function:
                            // flatten function, add to output, dont push function result 
                            res = FlattenFunction(data, in node, out output, false, out pusher);
                            if (res != BlastError.success)
                            {
                                data.LogError($"flatten: error flattening function:  <{node.parent}>.<{node}>[{node.ChildCount}], errorcode: {res}");
                                return res;
                            }

                            flat.AddRange(output);
                            break;

                        case nodetype.jump_to:
                        case nodetype.label:
                        case nodetype.cdata:
                            flat.Add(node);
                            break;

                        default:
                            data.LogError($"flatten: invalid nodetype in root: <{node.parent}>.<{node}>, type = {node.type}");
                            return BlastError.error_invalid_nodetype_in_root;
                    }
                }
            }

            
            return BlastError.success;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        /// <param name="root"></param>
        /// <returns></returns>
        public BlastError FlattenRoot(IBlastCompilationData data, node root)
        {
            List<node> flat = new List<node>();

            BlastError res = FlattenStatements(data, root, out flat);
            if (res != BlastError.success) return res;

            root.children.Clear();
            root.children.AddRange(flat);
            foreach (node n in root.children)
            {
                n.parent = root;
            }

            return BlastError.success; 
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public int Execute(IBlastCompilationData data)
        {
            return (int)FlattenRoot(data, data.AST);
        }
    }
}
