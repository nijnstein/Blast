using System;
using System.Collections.Generic;

#if NOT_USING_UNITY
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
        public Version Version => new Version(0, 1, 0);
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
                switch(child.type)
                {
                    case nodetype.function:
                        break; 

                    case nodetype.operation:
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
                        node pop = node.CreatePopNode(push);
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
                if (node.IsOperationList(child))
                {
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
                            push = node.CreatePushNode(child);
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
                        node pop = node.CreatePopNode(push);
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
                        ///
                        ///  we might want to do this with recursion.. 
                        ///   
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

                                    node pop = node.CreatePopNode(pusher_for_function);
                                    function.children[i] = pop;
                                    pop.parent = function;
                                    break;
                                }

                            case nodetype.compound:
                                {
                                    //
                                    //  FLATTEN ANY COMPOUND !!!   TEST TILL DEATH
                                    // 

                                    /// this should allow for nested stuf: (1 2 3 (-1))

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
                                    node pop = node.CreatePopNode(push);
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
            
            if (function.function == null)
                return BlastError.error_node_function_unknown;

            // scan parameters:
            // - if a simple parameter list: nothing needs to be done, we can move the node asis
            bool flat = true;
            foreach (node param in function.children)
            {
                if (param.type == nodetype.parameter && param.ChildCount == 1)
                {
                    continue;
                }

                if (param.IsCompound || (param.IsFunction && !param.function.IsPopVariant()) || param.IsOperation)
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
                    node push_ol  = node.CreatePushNode(function);
                    push_ol.children.AddRange(function.children);
                    foreach (node c_ol in push_ol.children) c_ol.parent = push_ol;
                    
                    function.children.Clear();

                    node pop_ol = node.CreatePopNode(push_ol);
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
            node push = node.CreatePushNode(child);

            child.parent = push;
            push.children.Add(child);

            return push;
        }

        private static node CreatePushNodeAndAddChildsChildren(IBlastCompilationData data, node child)
        {
            node push = node.CreatePushNode(child);
            
            push.children.AddRange(child.children);
            foreach (node pch in push.children) pch.parent = push;

            return push; 
        }


        /// <summary>
        /// flatten assignement node 
        /// </summary>
        public BlastError FlattenAssignment(IBlastCompilationData data, in node assignment, out List<node> flattened_output, bool push_function, out node pusher)
        {
            BlastError res;
            flattened_output = new List<node>();
            pusher = null;
            List<node> flat = new List<node>();

            int iter = 0; 

            // special cases
            // - assigning single value     => compiler transforms into assignS - done 
            // - assigning function         => compiler transforms into assignF - TODO this would preferably after refactoring the interpretor

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
                                    node pop = node.CreatePopNode(push);

                                    assignment.children[i] = pop;
                                    pop.parent = assignment;
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

                                ///
                                /// need to account for operation lists
                                ///

                                res = FlattenFunctionParameters(data, child, out flattened_output_of_function);
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
                                    node pop = node.CreatePopNode(push);
                                    child.children.Clear();
                                    child.SetChild(pop);
                                }
                            }
                            break;

                    }
                }
            }
            while (!assignment.IsFlat() && iter < 10);

            // probably should remove this
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
                if(!f.IsFlat(true))
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
                    while (!f.IsFlat(true)); 
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
                    /// there is max 1 compound below
                    foreach(node gchild in child.children)
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

            /// handle returned 
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
                // remove any unneeded compounds from node and its children (note that this function may return a child of the input node if the parent was useless) 
                node node = node.ReduceSingularCompounds(input_node);

                // depending on nodetype things differ
                switch(node.type)
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
                        flat.Add(node);
                        break;

                    default:
                        data.LogError($"flatten: invalid nodetype in root: <{node.parent}>.<{node}>, type = {node.type}"); 
                        return BlastError.error_invalid_nodetype_in_root; 
                }
            }

            
            return BlastError.success;
        }

        public BlastError Flatten(IBlastCompilationData data, node root)
        {
            List<node> flat = new List<node>(); 

            BlastError res = FlattenStatements(data, root, out flat);
            if (res != BlastError.success) return res; 

            // update compiler node tree 
            data.AST.children.Clear();
            data.AST.children.AddRange(flat);
            foreach (node n in data.AST.children) n.parent = data.AST;

            // return success
            return BlastError.success; 
        }







        public int Execute(IBlastCompilationData data)
        {
          //  if (((CompilationData)data).use_new_stuff)
          //  {
                Flatten(data, data.AST);
          //  }
         //   else
          //  {
          //      flatten(data, data.AST);
          //  }


            return (int)(data.IsOK ? BlastError.success : BlastError.error);
        }
    }
}
