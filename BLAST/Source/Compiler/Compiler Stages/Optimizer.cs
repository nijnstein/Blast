//##########################################################################################################
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

using System.Collections.Generic;
using Unity.Mathematics;

namespace NSS.Blast.Compiler.Stage
{
    /// <summary>
    /// Basic Optimization Stage 
    /// </summary>
    public class BlastOptimizer : IBlastCompilerStage
    {
        /// <summary>
        /// V0.1.0 - initial version 
        /// </summary>
        public System.Version Version => new System.Version(0, 1, 0);


        /// <summary>
        /// optimizer stage 
        /// </summary>
        public BlastCompilerStageType StageType => BlastCompilerStageType.Optimization;



        /// <summary>
        /// replace sequence IN PLACE with a function 
        /// </summary>
        /// <param name="blast">blast engine data -> needs access to functions</param>
        /// <param name="node">node to be updated</param>
        /// <param name="singleop">the singleop</param>
        /// <param name="replacement_function">the replacement fucntion</param>
        /// <param name="from">only use childnodes starting from this index</param>
        /// <param name="operation_count">the number of operations to replace if > 0</param>
        /// <returns></returns>
        unsafe BlastError ReplaceSequence(BlastEngineDataPtr blast, node node, blast_operation singleop, blast_operation replacement_function, int from = 0, int operation_count = -1)
        {
            Assert.IsNotNull(node);
            Assert.IsTrue(replacement_function != blast_operation.nop);
            Assert.IsTrue(singleop != blast_operation.nop);
            Assert.IsTrue(node.ChildCount > 0);
            Assert.IsTrue(from < node.ChildCount - 1 && from >= 0);

            // replace the sequence of operation by the function call 
            // the input sequence is expected to be in the form: id op id op etc.. 
            List<node> parameters = new List<node>();
            List<node> ops = new List<node>();
            BlastScriptFunction function = blast.Data->GetFunction(replacement_function);

            // gather parameters 
            int n_ops = 0;
            for (int i = from; i < node.ChildCount; i++)
            {
                node child = node.children[i];

                if (child.type == nodetype.parameter
                    ||
                    child.IsPopFunction || child.IsFunction
                    ||
                    node.IsCompoundWithSingleNegationOfValue(child))
                {
                    parameters.Add(child);
                    if (operation_count > 0 && n_ops >= operation_count)
                    {
                        // break out of processing 
                        break;
                    }
                    continue;
                }

                if (child.IsOperation)
                {
                    if (Blast.GetBlastOperationFromToken(child.token) != singleop)
                    {
                        return BlastError.error_optimizer_operation_mismatch;
                    }
                    n_ops++;
                    ops.Add(child);  
                }
            }

            if (parameters.Count == 0)
            {
                return BlastError.error_optimizer_parameter_mismatch;
            }

            // in normal sequence handling its no problem to start with a minus but we lose it when replacing with this function  
            bool starts_with_minus_op = node.children[from].token == BlastScriptToken.Substract;
            node first_child_to_add = null;

            // if replacing with suba, best is to add 0 as first parameter as: 0 - a = -a
            if (starts_with_minus_op)
            {
                if (replacement_function == blast_operation.suba)
                {
                    node param0 = node.CreateConstantParameter(blast_operation.value_0);
                    parameters.Insert(from, param0);
                }
                else
                {
                    //  we cant just compound it and negate result?
                    //  -(1 op 2) 
                    // i think we can in this case so add back the first - (the first child) 
                    first_child_to_add = node.children[from];
                }
            }


            // clear node children in 'partial' sequence, add back a new function using the parameters taken from the sequence 
            foreach (node n in parameters) node.children.Remove(n);
            foreach (node n in ops) node.children.Remove(n);


            if (first_child_to_add != null)
            {
                // adding negation back that we removed when a sequence is started with 1 child 
                node.SetChild(first_child_to_add, from);
                from += 1;
            }

            // create replacement node 
            node replacement = node.CreateChild(nodetype.function, BlastScriptToken.Nop, null, from);

            replacement.EnsureIdentifierIsUniquelySet();
            replacement.function = function;
            replacement.SetChildren(parameters);
            replacement.vector_size = node.vector_size;
            replacement.is_vector = node.is_vector;


            // 
            // we could scan for (-id) compounds and try to remove them 
            // 

            //
            // we have an assing id function or assign id -function
            // - both should be optimized further into assignf 
            // 


            // ok
            return BlastError.success;
        }

         


        BlastError OptimizeNonVectorAssignment(IBlastCompilationData data, node node)
        {
            Assert.IsNotNull(node);

            const int min_op_count = 2;
  
            if (node.vector_size > 1) return BlastError.success;
            if (node.ChildCount <= min_op_count * 2) return BlastError.success; 

            BlastError res;
            blast_operation op;

            //
            // 1> replace operation sequences into functions:  1 + 2 + 3 => adda(1, 2, 3)
            // - saves on controlflow processing (too much to leave hanging until a next release)
            // - this runs after flatten and should also apply to inline vectors
            // - (should we check type or assume analyser did its job?)
            //                            
            if (node.IsSingleOperationList(out op))
            {
                if (Blast.HasSequenceOperation(op))
                {
                    // transform the sequence into a function call matching the operation 
                    res = ReplaceSequence(data.Blast, node, op, Blast.GetSequenceOperation(op));
                    if (res != BlastError.success) return BlastError.error_optimizer_failed_to_replace_sequence;

                    // no use to look any further 
                    return BlastError.success;
                }
            }

            //
            // 2> scan for largest consecutive group of operations
            //  - would be nice to do: a = 34 * 55 * 44 * 555 * 2 * 8 * 9 + 3
            //  - to a = mula(34 * 55 * 44 * 555 * 2 * 8 * 9) + 3
            //
            int iterator = 0; // savegaurd for potential endless looping on bugs  
            int from = 0;
            while (iterator++ < 10 && node.FirstConsecutiveOperationSequence(min_op_count, from, out op, out int op_count, out node first_op_in_sequence))
            {
                if (first_op_in_sequence == null || op_count < min_op_count) break; 

                from = node.children.IndexOf(first_op_in_sequence);
                if (from < 0) break;

                // expand to include parameter|id|function before the first operation 
                if (from > 0 && node.children[from - 1].type != nodetype.operation && op != blast_operation.substract)
                {
                    from--; 
                }

                // replace the partial sequence 
                res = ReplaceSequence(data.Blast, node, op, Blast.GetSequenceOperation(op), from, op_count);
                if (res != BlastError.success) return BlastError.error_optimizer_failed_to_replace_sequence;


                // expand from past inserted nodes
                from = from + 2;


                // if not possible stop trying 
                if (node.ChildCount - from < min_op_count * 2) break; 
            }


            //
            // 3> FMA translations, although fma stands for fused multiply add we just do MA
            // - try to find all matching instances of an fma construct 
            // - user will often type: a = (a * b) + 3; we should make sure this compound is reduced and not flattened out into a push-pop because then we cant optimize it in this way


            // for now -> dont do more then the sequences and the fma, focus on the rest after that until V2


            return BlastError.success; 
        }


        BlastError Optimize(IBlastCompilationData data, node node)
        {
            BlastError res; 
            switch(node.type)
            {
                case nodetype.assignment: 
                    if ((res = OptimizeNonVectorAssignment(data, node)) != BlastError.success) return res;
                    break; 

            }


            return BlastError.success;
        }
 

        /// <summary>
        /// Optimize operations expressed by the ast if optimization is enabled through compiler settings 
        /// </summary>
        public int Execute(IBlastCompilationData data)
        {
            if (data.CompilerOptions.Optimize)
            {
                foreach (node node in data.AST.children)
                {
                    BlastError res = Optimize(data, node);
                    if (res != BlastError.success)
                    {
                        data.LogError($"BlastCompiler.NodeOptimizer: failed to optimize node: <{node.parent}>.<{node}>, error: {res}");
                    }
                }
            }
            else
            {
#if TRACE
                data.LogTrace("Optimizer.Execute: optimizations skipped dueue to compiler options");
#endif
            }
            return data.IsOK ? (int)BlastError.success : (int)BlastError.error;
        }
    }
}