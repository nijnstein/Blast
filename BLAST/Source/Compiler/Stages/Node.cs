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
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Unity.Mathematics;

namespace NSS.Blast.Compiler
{
    /// <summary>
    /// the different nodetypes used in the ast
    /// </summary>
    public enum nodetype
    {
        /// <summary>
        /// nodetype is not set
        /// </summary>
        none,
        /// <summary>
        /// this is the root node
        /// </summary>
        root,
        /// <summary>
        /// this node represents a function
        /// </summary>
        function,
        /// <summary>
        /// this node represents an assignment to identifier
        /// </summary>
        assignment,
        /// <summary>
        /// node represents a parameter to a function or sequence with identifier name/value
        /// </summary>
        parameter,
        /// <summary>
        /// BS2 parameter indexer, .[] 
        /// </summary>
        index,
        /// <summary>
        /// node represents an operation: +-/ etc.
        /// </summary>
        operation,
        /// <summary>
        /// yield operation, must be in root
        /// </summary>
        yield,
        /// <summary>
        /// represents a compound: ( () )
        /// </summary>
        compound,
        /// <summary>
        /// the root of an if then else structure
        /// </summary>
        ifthenelse,
        /// <summary>
        /// the ifthen clausule
        /// </summary>
        ifthen,
        /// <summary>
        /// the ifelse clausule
        /// </summary>
        ifelse,
        /// <summary>
        /// a condition, either in if statements or while/for loops 
        /// </summary>
        condition,
        /// <summary>
        /// the root of a while loop
        /// </summary>
        whileloop,
        /// <summary>
        /// the while loop body, handled as a compound 
        /// </summary>
        whilecompound,
        /// <summary>
        /// the switch statement root node, transformed into ifthenelse statements during compilation
        /// </summary>
        switchnode,
        /// <summary>
        /// a switch case 
        /// </summary>
        switchcase,
        /// <summary>
        /// the default case 
        /// </summary>
        switchdefault,
        /// <summary>
        /// the root of a for loop 
        /// </summary>
        forloop,
        /// <summary>
        /// a jump instruction inserted by the compiler that jumps to a given label in the ast
        /// </summary>
        jump_to,
        /// <summary>
        /// a label (a jump target) inserted by the compiler 
        /// </summary>
        label,
        /// <summary>
        /// an inline function defined in script 
        /// </summary>
        inline_function,
        /// <summary>
        /// a constant cdata element to be inlined into the code segment 
        /// </summary>
        cdata
    }

    /// <summary>
    /// an ast node used by the blast compiler
    /// </summary>
    public class node
    {
        /// <summary>
        /// the parent node, null if the node is the root
        /// </summary>
        public node parent;

        /// <summary>
        /// nodetype of node
        /// </summary>
        public nodetype type;

        /// <summary>
        /// if the node is a function then its this one
        /// </summary>
        public BlastScriptFunction function;

        /// <summary>
        /// any token attached to this node 
        /// </summary>
        public BlastScriptToken token;

        /// <summary>
        /// any identifier attached to this node
        /// </summary>
        public string identifier;

        /// <summary>
        /// variable data inferred from the ast and connected to the nodes identifier 
        /// </summary>
        public BlastVariable variable = null;

        /// <summary>
        /// datatype is by default always a numeric unless set otherwise 
        /// </summary>
        public BlastVariableDataType datatype = BlastVariableDataType.Numeric; 

        /// <summary>
        /// children of node 
        /// </summary>
        public List<node> children = new List<node>();

        /// <summary>
        /// dependencies of node 
        /// </summary>
        public List<node> depends_on = new List<node>();

        /// <summary>
        /// BS2: indexers, arrays and compound types []. 
        /// </summary>
        public List<node> indexers = new List<node>();

        /// <summary>
        /// any operation connected to this node (type == function | operation | parameter (pop))
        /// </summary>
        public blast_operation constant_op = blast_operation.nop;

        /// <summary>
        /// skip general compilation of this node, some nodes control when to compile their own dependencies like initializers in loops
        /// </summary>
        public bool skip_compilation;

        /// <summary>
        /// true if the node represents a constant value 
        /// </summary>
        public bool is_constant = false;

        /// <summary>
        /// the vector size 
        /// </summary>
        public int vector_size = 0; //  = 1;  setting it to 0 will have it fail on unset with vectorsize mismatches, thats because somewhere we forget to set it

        /// <summary>
        /// true if the value represented is a vector, dont use this directly as the compilers interpretation might change during the process. 
        /// check variable or function property instead to find out if something is a vector 
        /// </summary>
        internal bool is_vector = false;

        /// <summary>
        /// internal use: a linked push operation, used during compilation to keep track of pushpop pairs
        /// </summary>
        public node linked_push = null;

        /// <summary>
        /// internal use: a linked pop operation, used during compilation to keep track of pushpop pairs
        /// </summary>
        public node linked_pop = null;

        /// <summary>
        /// Only true if this is the root of the ast
        /// </summary>
        public bool IsRoot => parent == null && type == nodetype.root;

        /// <summary>
        /// True if this a compound node
        /// </summary>
        public bool IsCompound => type == nodetype.compound;

        /// <summary>
        /// True if this is an assignment
        /// </summary>
        public bool IsAssignment => type == nodetype.assignment;

        /// <summary>
        /// True if this is a function 
        /// </summary>
        public bool IsFunction => type == nodetype.function;

        /// <summary>
        /// true if this node represents constant cdata to be compiled into the code stream 
        /// </summary>
        public bool IsCData => type == nodetype.cdata; 

        /// <summary>
        /// true if the function maps to a stack operation: push, pop etc. 
        /// </summary>
        public bool IsStackFunction => type == nodetype.function && (function.IsPopVariant || function.IsPushVariant);


        /// <summary>
        /// True if this is a function pushing to the stack 
        /// </summary>
        public bool IsPushFunction => type == nodetype.function && function.IsPushVariant;

        /// <summary>
        /// True if this is a function popping from the stack
        /// </summary>
        public bool IsPopFunction => type == nodetype.function && function.IsPopVariant;

        /// <summary>
        /// True if this node represents an inlined function 
        /// </summary>
        public bool IsInlinedFunction => type == nodetype.inline_function;

        /// <summary>
        /// returns true if this is a function that maps to an inlined function and NOT to an api function 
        /// </summary>
        public bool IsInlinedFunctionCall => type == nodetype.function && function.FunctionId == -1;

        /// <summary>
        /// True if this is an operation 
        /// </summary>
        public bool IsOperation => type == nodetype.operation;

        /// <summary>
        /// True if this is a leaf nod
        /// </summary>
        public bool IsLeaf => ChildCount == 0 && parent != null;

        /// <summary>
        /// True if this is data with a cardinality larger then 1 (a vector) 
        /// </summary>
        public bool IsVector => is_vector && vector_size > 1;

        /// <summary>
        /// True if this node represents a scripted variable 
        /// </summary>
        public bool IsScriptVariable => variable != null;

        /// <summary>
        /// true if the node has dependencies
        /// </summary>
        public bool HasDependencies => depends_on != null && depends_on.Count > 0;

        /// <summary>
        /// get the number of dependency nodes
        /// </summary>
        public int DependencyCount => depends_on != null ? depends_on.Count : 0;

        /// <summary>
        /// true if the node contains child nodes 
        /// </summary>
        public bool HasChildren => children != null && children.Count > 0;

        /// <summary>
        /// true if the node has exactly 1 node
        /// </summary>
        public bool HasOneChild => children != null && children.Count == 1;

        /// <summary>
        /// true if node contains indexers 
        /// </summary>
        public bool HasIndexers => indexers != null && indexers.Count > 0;

        /// <summary>
        /// true if node is represented by an identifier 
        /// </summary>
        public bool HasIdentifier => !string.IsNullOrWhiteSpace(identifier);

        /// <summary>
        /// get the total number of nodes in the tree as seen from current node
        /// </summary>
        public int CountNodes()
        {
            return CountChildNodes(this);
        }

        /// <summary>
        /// get the total number of nodes in the children of n
        /// </summary>
        /// <param name="n">the node to count all child nodes of</param>
        /// <returns></returns>
        static public int CountChildNodes(node n)
        {
            if (n == null || !n.HasChildren)
            {
                return 0;
            }

            int c = n.ChildCount;

            for (int i = 0; i < n.ChildCount; i++)
            {
                c += CountChildNodes(n.children[i]);
            }
            return c;
        }

        /// <summary>
        /// check if a node equals a vector definition: 
        /// 
        /// ->  ( 1 2 3 ) -> compound[3] ( id id id ) 
        ///               -> compound[n] ( n[pop | identifier[1]] ) 
        /// 
        /// -> root node == compound with n children representing its elements 
        /// 
        /// </summary>
        /// <param name="n"></param>
        /// <returns></returns>
        public static bool IsVectorDefinition(node n)
        {
            Assert.IsNotNull(n);

            // must be compound of n elements 
            if (!n.IsCompound) return false;

            // a vector has more then 1 element 
            if (n.ChildCount <= 1) return false;

            // each element is either a
            // - pop or other function resulting in 1 value 
            // - identiefieer resulting in 1 value

            for (int i = 0; i < n.ChildCount; i++)
            {
                node child = n.children[i];

                if (child.IsFunction)
                {
                    if (child.function.ReturnsVectorSize != 1)
                    {
                        // not a suitable element for a vector definition like this: (a, b, function) 
                        return false;
                    }
                }
                else
                {
                    if (child.IsScriptVariable)
                    {
                        if (child.vector_size != 1)
                        {
                            // should always be 1 in this kind of define
                            return false;
                        }
                    }
                    else
                    {
                        if (child.is_constant && child.vector_size == 1)
                        {

                        }
                        else
                        {
                            return false;
                        }
                    }
                }
            }

            return true;
        }


        /// <summary>
        /// create a constant value parameter from operation 
        /// </summary>
        /// <param name="value_0">the value to insert</param>
        /// <returns>the newly created paramater node with a constant value set</returns>
        public static node CreateConstantParameter(blast_operation value_0)
        {
            node n = new node(nodetype.parameter, BlastScriptToken.Identifier);
            n.EnsureIdentifierIsUniquelySet();
            n.constant_op = value_0;
            n.vector_size = 1;
            n.is_vector = false;
            return n;
        }


        /// <summary>
        /// check if this node is a definition of a vector that does not nest:
        /// (1 1 1 1) 
        /// (1 pop pop) 
        /// (2 maxa(1) 2 3)
        /// (3, -1 2 2) =>  (3 (-1) 2 2)
        /// 
        /// </summary>
        /// <returns></returns>
        public bool IsNonNestedVectorDefinition()
        {
            return IsNonNestedVectorDefinition(this);
        }

        /// <summary>
        /// check if given node contains a non nested vector define: node = (1 2 3 4)  | node = (1 2 (-3) 4)
        /// </summary>
        /// <param name="n">the node to check</param>
        /// <returns>true if it does</returns>
        public static bool IsNonNestedVectorDefinition(node n)
        {
            Assert.IsNotNull(n);

            // must be compound of n elements 
            // or a pusv instruction 
            if (!n.IsCompound)
            {
                if (n.IsFunction && n.function.FunctionId == (int)ReservedBlastScriptFunctionIds.PushVector)
                {
                }
                else
                { return false; }

            }

            // a vector has more then 1 element 
            if (n.ChildCount <= 1) return false;

            // each element is either a
            // - pop or other function resulting in 1 value 
            // - identiefieer resulting in 1 value

            for (int i = 0; i < n.ChildCount; i++)
            {
                node child = n.children[i];

                if (child.HasChildren) continue;  // may still be something resulting in 1 var 
                if (child.is_constant || child.IsScriptVariable) continue; // these might be ok

                // this does assume its a vectorsize 1 pop.... 
                if (child.IsFunction)
                {
                    // allow pops 
                    if (child.function.IsPopVariant) continue;

                    // allow returnsize 1 
                    if (child.function.ReturnsVectorSize == 1) continue;
                    else
                    {
                        // if we would allow this we would open a can of worms:
                        // - allow variable vector sizes to add up to 1 of total length ? 
                        // - getting vectorsize needs updates for that / probably rewrite 
                        return false;
                    }
                }


                switch (child.type)
                {
                    case nodetype.parameter: continue;

                    case nodetype.compound:
                        //
                        // allow 1 special case: compound (-2)
                        // - this is done by parser to negate constants 
                        //
                        {
                            if (child.ChildCount == 2
                                &&
                                child.children[0].token == BlastScriptToken.Substract
                                &&
                                child.children[1].token == BlastScriptToken.Identifier)
                            {
                                continue;
                            }
                        }
                        return false;

                    default: return false;
                }
            }
            return true;
        }

        /// <summary>
        /// get the largest group of operations of the same type 
        /// </summary>
        /// <param name="node">the parent node of the operation list</param>
        /// <param name="min_group_size">minimal group size</param>
        /// <param name="from">start check from this node</param>
        /// <param name="op">outputs operation of largest group or nop</param>
        /// <param name="op_count">nr of operations in group</param>
        /// <param name="first_op_in_sequence"></param>
        /// <returns></returns>

        static public bool FirstConsecutiveOperationSequence(node node, int min_group_size, int from, out blast_operation op, out int op_count, out node first_op_in_sequence)
        {
            Assert.IsNotNull(node);
            Assert.IsTrue(min_group_size > 0);

            op = blast_operation.nop;
            op_count = 0;
            first_op_in_sequence = null;

            // childcount must be larger otherwise we can bail out early
            if ((node.ChildCount - from) < min_group_size * 2 /* + 1*/ )
                // one exception: - group can be shorter if starting on 1
                return false;

            // scan trough children maintain operation list
            for (int i = from; i < node.ChildCount; i++)
            {
                node child = node.children[i];

                switch (child.type)
                {
                    case nodetype.function:
                        {
                            if (!child.function.IsPopVariant)
                            {
                                op = blast_operation.nop;
                                op_count = 0;
                                first_op_in_sequence = null;
                            }
                        }
                        break;

                    case nodetype.operation:
                        {
                            blast_operation node_op = Blast.GetBlastOperationFromToken(child.token);
                            if (op == blast_operation.nop)
                            {
                                op = node_op;
                                op_count = 1;
                                first_op_in_sequence = child;
                            }
                            else
                            {
                                if (op == node_op)
                                {
                                    op_count++;
                                }
                                else
                                {
                                    // different node typ 
                                    if (op_count >= min_group_size)
                                    {
                                        // sequence long enough
                                        return true;
                                    }
                                    else
                                    {
                                        // reset to different op
                                        op_count = 1;
                                        first_op_in_sequence = child;
                                        op = node_op;
                                    }

                                }
                            }
                        }
                        break;
                }
            }

            // we have a group of some size 
            return op_count >= min_group_size;
        }

        /// <summary>
        /// scan children of node for the first group of operations of a given minimal size 
        /// </summary>
        /// <param name="min_groupsize">the minimal group size</param>
        /// <param name="from">start check from this node</param>
        /// <param name="op">operation to scan for</param>
        /// <param name="op_count">the operation count in the group found</param>
        /// <param name="first_op_in_sequence">first operation found in sequence</param>
        /// <returns></returns>
        public bool FirstConsecutiveOperationSequence(int min_groupsize, int from, out blast_operation op, out int op_count, out node first_op_in_sequence)
        {
            return node.FirstConsecutiveOperationSequence(this, min_groupsize, from, out op, out op_count, out first_op_in_sequence);
        }

        /// <summary>
        /// interpret this node as a float value and return that
        /// </summary>
        public bool IsFloat
        {
            get
            {
                if (vector_size != 1 || string.IsNullOrWhiteSpace(identifier)) return false;

                int i = 0, c = 0, d = 0;
                foreach (char ch in identifier)
                {
                    // rest must be digits 
                    if (char.IsDigit(ch))
                    {
                        d++;
                        i++;
                        continue;
                    }

                    // minus only as first 
                    if (ch == '-')
                    {
                        if (i == 0)
                        {
                            i++;
                            continue;
                        }
                        else
                        {
                            return false;
                        }
                    }

                    if (ch == '.' || ch == ',')
                    {
                        // ., not as first or last
                        if (c != 0 || ch == 0 || ch == identifier.Length - 1) return false;
                        // max 1 .,
                        c++;
                        i++;
                        continue;
                    }

                    return false;
                }

                if (c == 1)
                {
                    // if a ., min 2 digits 
                    return d > 1;
                }
                else
                {
                    // if not, min 1 
                    return d > 0;
                }
            }
        }


        /// <summary>
        /// interpret the node's attached identifier as a floating point value, returns Blast.InvalidNumeric if it could
        /// not parse the identifier as a numeric
        /// </summary>
        public float AsFloat
        {
            get
            {
                return identifier == null ? Blast.InvalidNumeric : identifier.AsFloat();
            }
        }


        /// <summary>
        /// general constructor
        /// </summary>
        /// <param name="_parent">parent node</param>
        /// <param name="_children">child nodes</param>
        public node(node _parent, params node[] _children)
        {
            parent = _parent;
            skip_compilation = false;
            is_vector = false;
            is_constant = false;
            if (parent != null) parent.children.Add(this);
            if (_children != null) children.AddRange(_children);
        }

        /// <summary>
        /// general constructor
        /// </summary>
        /// <param name="_type">the nodetype</param>
        /// <param name="_token">the attached token</param>
        public node(nodetype _type, BlastScriptToken _token)
        {
            parent = null;
            skip_compilation = false;
            is_vector = false;
            is_constant = false;
            token = _token;
            type = _type;
        }


        /// <summary>
        /// provides information in debug display through a tostring overload
        /// </summary>
        /// <returns>node formatted as string</returns>
        public override string ToString()
        {
            if (HasChildren)
            {
                StringBuilder sb = StringBuilderCache.Acquire();
                sb.Append(GetNodeDescription());
                sb.Append("[");
                foreach (node child in children)
                {
                    sb.Append($"[{child}] ");
                }
                sb.Append("]");
                return StringBuilderCache.GetStringAndRelease(ref sb);
            }
            else
            {
                return GetNodeDescription();
            }
        }

        /// <summary>
        /// get a textual description of this node 
        /// </summary>
        /// <returns>the text description</returns>
        public string GetNodeDescription()
        {
            string sconstant = is_constant ? "constant " : "";
            string svector = is_vector || true ? $" vector[{vector_size}]" : "";
            switch (type)
            {
                case nodetype.root: return $"{sconstant}{svector}root of {children.Count}";
                case nodetype.assignment: return $"{sconstant}{svector}assignment of {identifier}";
                case nodetype.compound: return $"{sconstant}{svector}compound statement of {children.Count}";
                case nodetype.function: return $"{sconstant}{svector}function {function.GetFunctionName()}";
                case nodetype.operation: return $"{sconstant}{svector}operation {token}";
                case nodetype.parameter: return $"{sconstant}{svector}parameter {identifier}";
                case nodetype.none: return $"{sconstant}{svector}nop";
                case nodetype.yield: return $"{sconstant}{svector}yield";
                case nodetype.ifthenelse: return $"if";
                case nodetype.ifthen: return $"then";
                case nodetype.ifelse: return $"else";
                case nodetype.condition: return $"condition";
                case nodetype.whileloop: return "while";
                case nodetype.whilecompound: return "while compound";
                case nodetype.switchnode: return "switch";
                case nodetype.switchcase: return "case";
                case nodetype.switchdefault: return "default";
                case nodetype.jump_to: return $"jump to {identifier}";
                case nodetype.label: return $"label {identifier}";
                case nodetype.forloop: return $"for ";
                case nodetype.inline_function: return $"inlined-function {identifier}";
                case nodetype.index: return $"indexer {identifier} {constant_op}";
                default: return base.ToString();
            }
        }

        /// <summary>
        /// generate a multiline string representing the node tree structure
        /// </summary>
        /// <param name="indentation">nr of space to indent for each child iteration</param>
        public string ToNodeTreeString(int indentation = 3)
        {
            return ToNodeTreeString(this, null, indentation);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="n"></param>
        /// <param name="sb"></param>
        /// <param name="indent"></param>
        /// <returns></returns>
        public static string ToNodeTreeString(node n, StringBuilder sb = null, int indent = 3)
        {
            if (n == null)
            {
                return string.Empty;
            }

            bool own_sb = sb == null;
            if (own_sb)
            {
                sb = new StringBuilder();
                sb.AppendLine("compiler node tree");
                sb.AppendLine();
            }

            string dependencies = string.Empty;
            foreach (var d in n.depends_on)
            {
                dependencies += d.ToString() + " ";
            }
            if (!string.IsNullOrWhiteSpace(dependencies))
            {
                dependencies = $"[depends on: {dependencies}]";
            }

            string indexers = string.Empty;
            foreach (var d in n.indexers)
            {
                indexers += d.ToString() + " ";
            }
            if (!string.IsNullOrWhiteSpace(indexers))
            {
                indexers = $"[indexed with: {indexers}]";
            }

            sb.AppendLine($"{"".PadLeft(indent, ' ')}{n.GetNodeDescription()} {dependencies} {indexers}");
            if (n.children.Count > 0)
            {
                foreach (node c in n.children)
                {
                    ToNodeTreeString(c, sb, indent + 3);
                }
                sb.AppendLine($"{"".PadLeft(indent, ' ')}/");
            }

            if (own_sb)
            {
                return sb.ToString();
            }
            else
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// check if the node is an operation sequence in the form: 3 + a + 4 + 4 + max(3093) + (4 + 0) 
        /// </summary>
        public bool IsOperationList()
        {
            return node.IsOperationList(this);
        }


        /// <summary>
        /// check if this node contains an operation sequence with only 1 different operator:
        /// a = 1 * 3 * 3 * 4; 
        /// </summary>
        /// <returns>true if an operation list and all operations used are the same</returns>
        public bool IsSingleOperationList(out blast_operation op)
        {
            if (node.IsOperationList(this, out op))
            {
                return op != blast_operation.nop && op != blast_operation.ex_op;
            }
            return false;
        }


        /// <summary>
        /// check if the node IS:
        /// [compound][-][param|pop_or_val][/compound]
        /// </summary>
        public bool IsCompoundWithSingleNegationOfValue()
        {
            return IsCompoundWithSingleNegationOfValue(this);
        }


        /// <summary>
        /// check if the node IS:
        /// [compound][-][param|pop_or_val][/compound]
        /// </summary>
        /// <param name="node">the node that should be the compound in the check</param>
        public static bool IsCompoundWithSingleNegationOfValue(node node)
        {
            return
                (node.IsCompound || node.IsPushFunction)
                &&
                node.ChildCount == 2
                &&
                node.FirstChild.token == BlastScriptToken.Substract
                &&
                node.LastChild.IsSingleValueOrPop();
        }



        /// <summary>
        /// check if the node is an operation sequence in the form: 3 + a + 4 + 4 + max(3093) + (4 + 0) 
        /// </summary>
        /// <param name="node">the node to check</param>
        /// <returns></returns>
        public static bool IsOperationList(node node)
        {
            return IsOperationList(node, out blast_operation op);
        }

        /// <summary>
        /// check if the node is an operation sequence in the form: 3 + a + 4 + 4 + max(3093) + (4 + 0) 
        /// </summary>
        /// <param name="node">the node to check</param>
        /// <param name="singleop">the operation found, if single it maps to the operation, if none its nop, on many different it will be op.ex</param>
        /// <returns>true if a list of operations</returns>
        public static bool IsOperationList(node node, out blast_operation singleop)
        {
            singleop = blast_operation.nop;
            bool has_ops = false;


            if (node.ChildCount == 2)
            {
                // only a negation should be possible to be 2 long 
                if (IsCompoundWithSingleNegationOfValue(node))
                {
                    singleop = blast_operation.substract;
                    return true;
                }
                else
                {
                    return false;
                }
            }

            // it has to have more then 2 items in order to form a list of operations 
            if (node.ChildCount > 2)
            {
                bool canbenegation = false;
                int consecutive_params = 0;
                bool lastwasparam = false;
                bool lastwasop = false;

                for (int i = 0; i < node.ChildCount; i++)
                {
                    node child = node.children[i];
                    switch (child.type)
                    {
                        case nodetype.operation:
                            lastwasparam = false;
                            consecutive_params = 0;

                            if (!canbenegation && lastwasop && child.token == BlastScriptToken.Substract)
                            {
                                canbenegation = true;
                            }
                            else
                            {
                                has_ops = true;
                            }
                            lastwasop = true;

                            blast_operation child_operation = Blast.GetBlastOperationFromToken(child.token);

                            if (singleop == blast_operation.nop) singleop = child_operation;
                            else if (singleop != child_operation) singleop = blast_operation.ex_op;

                            break;

                        case nodetype.compound:
                        case nodetype.parameter:

                            if (!lastwasparam)
                            {
                                lastwasparam = true;
                            }
                            else
                            {
                                consecutive_params++;
                                if (consecutive_params > 1) return false;
                            }
                            break;

                        case nodetype.function:
                            if (lastwasparam)
                            {
                                consecutive_params++;
                                if (consecutive_params > 1) return false;
                            }
                            else
                            {
                                lastwasparam = true;
                            }
                            break;


                        default:
                            lastwasop = false;

                            if (canbenegation) { has_ops = true; canbenegation = false; }
                            if (child.HasChildren && node.IsOperationList(child)) has_ops = true;
                            lastwasparam = false;
                            consecutive_params = 0;
                            break;
                    }
                }
            }
            return has_ops;
        }

        /// <summary>
        /// calculate number of bytes to reserve for stack from push pop pairs in the node
        /// - we need to get those overlapping and then the max sum of vector elements == stacksize 
        /// </summary>
        /// <param name="yield">bytes to add for supporting yield (this routine does not check if yield is used)</param>
        /// <returns>max bytes of stack memory used at any moment in the scripts execution</returns>
        public int EstimateStackUse(int yield = 0)
        {





            return 256;
        }



        /// <summary>
        /// -encode vectorsize in lower nibble
        /// -encode childcount == parametercount in high nibble 
        /// </summary>
        public static byte encode44(node node)
        {
            return (byte)((node.vector_size & 0b00001111) | ((node.ChildCount & 0b00001111) << 4));
        }

        /// <summary>
        /// -encode vectorsize in lower nibble
        /// -encode parametercount in high nibble 
        /// </summary>
        public static byte encode44(node node, byte parameter_count)
        {
            return (byte)((node.vector_size & 0b00001111) | ((parameter_count & 0b00001111) << 4));
        }



        /// <summary>
        /// set a dependency for this node, some constructs such as loops use this for the initializer
        /// </summary>
        /// <param name="ast_node">the node to add to dependencies</param>
        public void SetDependency(node ast_node)
        {
            Assert.IsNotNull(ast_node);

            // remove it from current parent if any
            if (ast_node.parent != null)
            {
                ast_node.parent.children.Remove(ast_node);
            }

            AppendDependency(ast_node);
        }

        /// <summary>
        /// insert (actually it appends) a depenency, updateing parent and chldren list 
        /// </summary>
        /// <param name="ast_node">the node to append</param>
        public void InsertDependency(node ast_node)
        {
            Assert.IsNotNull(ast_node);

            // remove it from current parent if any
            if (ast_node.parent != null)
            {
                ast_node.parent.children.Remove(ast_node);
            }

            AppendDependency(ast_node);

            if (depends_on == null)
            {
                depends_on = new List<node>();
            }
            ast_node.parent = this;
            depends_on.Insert(0, ast_node);
        }


        /// <summary>
        /// add a node to the list of nodes to depend on, these are tobe inserted before 
        /// this node during compilation, the parent of the node is updated to this
        /// </summary>
        /// <param name="n">the node to add</param>
        public void AppendDependency(node n)
        {
            if (n == null)
            {
                return;
            }
            if (depends_on == null)
            {
                depends_on = new List<node>();
            }
            n.parent = this;
            depends_on.Add(n);
        }

        /// <summary>
        /// append multiple dependant nodes
        /// </summary>
        /// <param name="nodes">nodes to add to dependencies</param>
        public void AppendDependencies(IEnumerable<node> nodes)
        {
            foreach (node n in nodes) AppendDependency(n);
        }


        /// <summary>
        /// get the first child found of a given nodetype
        /// </summary>
        public node GetChild(nodetype t)
        {
            return children.FirstOrDefault(x => x.type == t);
        }

        /// <summary>
        /// returns true if the node contains a child of the given type 
        /// </summary>
        public bool HasChild(nodetype t)
        {
            return children.Any(x => x.type == t); 
        }

        /// <summary>
        /// get first child matching first choice, if none found, try the second choice
        /// </summary>
        /// <param name="first_choice">first to try to locate</param>
        /// <param name="second_choice">next attemt to locate</param>
        /// <returns>a node if any was found, otherwise null</returns>
        public node GetChild(nodetype first_choice, nodetype second_choice)
        {
            node c = GetChild(first_choice);
            if (c == null)
            {
                c = GetChild(second_choice);
            }
            return c;
        }

        /// <summary>
        /// get children of given nodetype
        /// </summary>
        /// <returns>an array of nodes</returns>
        public node[] GetChildren(nodetype t)
        {
            node[] nodes = new node[CountChildren(t)];

            if (nodes.Length > 0)
            {
                int count = 0;
                foreach (node c in children)
                {
                    if (c.type == t) nodes[count] = c;
                }
            }

            return nodes;
        }

        /// <summary>
        /// get child nodes not of the type t
        /// </summary>
        /// <param name="t">the nodetype to discriminate</param>
        /// <returns>an array of nodes</returns>
        public node[] GetOtherChildren(nodetype t)
        {
            // count, allocate enough room
            node[] others = new node[CountOtherChildren(t)];

            // then set them 
            if (others.Length > 0)
            {
                int count = 0;
                foreach (node c in children)
                {
                    if (c.type != t)
                    {
                        others[count] = c;
                        count++; 
                    }
                }
            }

            return others;
        }


        /// <summary>
        /// count nr of childnodes with not nodetype t
        /// </summary>
        /// <returns>number of children not matching nodetype t</returns>
        public int CountOtherChildren(nodetype t)
        {
            int count = 0;
            foreach (node c in children)
            {
                if (c.type != t)
                {
                    count++;
                }
            }
            return count;
        }

        /// <summary>
        /// count children of given nodetype 
        /// </summary>
        /// <returns>number of children matching type t</returns>
        public int CountChildren(nodetype t)
        {
            int count = 0;
            foreach (node c in children)
            {
                if (c.type == t)
                {
                    count++;
                }
            }
            return count;
        }

        /// <summary>
        /// deep clones node without root parent set
        /// </summary>
        /// <returns></returns>
        public node DeepClone(bool include_children = true)
        {
            node n = new node(null);
            n.parent = parent;
            n.children = new List<node>();
            n.indexers = new List<node>();

            // note:  dependancy cloning not supported  

            if (include_children)
            {
                foreach (node child in children) n.children.Add(child.DeepClone());
                foreach (node child in n.children) child.parent = n;
            }

            foreach (node child in indexers) n.indexers.Add(child.DeepClone());
            foreach (node child in n.indexers) child.parent = n;

            n.type = type;
            n.function = function;
            n.token = token;
            n.identifier = identifier;
            n.constant_op = constant_op;
            n.skip_compilation = skip_compilation;
            n.is_constant = is_constant;
            n.is_vector = is_vector;
            n.vector_size = vector_size;
            n.variable = variable;
            return n;
        }

        /// <summary>
        /// get all leaf nodes from this node
        /// </summary>
        /// <returns></returns>
        public List<node> GetLeafNodes()
        {
            List<node> leafs = new List<node>();
            GetLeafNodes(leafs, this);
            return leafs;
        }


        /// <summary>
        /// gather all leaf nodes from a given node
        /// </summary>
        /// <param name="leafs">leaf node list to use</param>
        /// <param name="n">node to gather leafs from</param>
        static void GetLeafNodes(List<node> leafs, node n)
        {
            if (n.IsLeaf)
            {
                leafs.Add(n);
            }
            else
            {
                foreach (node c in n.children)
                {
                    if (c.IsLeaf)
                    {
                        leafs.Add(c);
                    }
                    else
                    {
                        GetLeafNodes(leafs, c);
                    }
                }
            }
        }


        /// <summary>
        /// check if we have 1 child and that it is a compounded statement list
        /// </summary>
        /// <returns></returns>
        public bool HasSingleCompoundAsChild()
        {
            return children.Count == 1 && children[0].type == nodetype.compound;
        }

        /// <summary>
        /// check if a given function is used in this node or its children
        /// </summary>
        /// <param name="op">the op representing the function</param>
        /// <returns>true if used</returns>
        internal bool CheckIfFunctionIsUsedInTree(blast_operation op)
        {
            if (function.FunctionId > 0 && this.function.ScriptOp == op)
            {
                return true;
            }
            foreach (var ch in children)
            {
                if (ch.CheckIfFunctionIsUsedInTree(op))
                {
                    return true;
                }
            }
            return false;
        }


        /// <summary>
        /// recursively reduces unneeded compound nesting 
        /// (((2 3))) => (2 2)
        /// </summary>
        /// <param name="node">the root node of the nested compound</param>
        static public node ReduceSingularCompounds(node node)
        {
            Assert.IsNotNull(node);

            // reduce root
            while (node.ChildCount == 1 && (node.FirstChild.IsCompound || node.FirstChild.IsFunction))
            {
                node child = node.FirstChild;
                if (child.IsCompound)
                {
                    // single compound as child, need to check these children and remove the comound
                    node.children.Clear();
                    node.SetChildren(child.children);
                    child.parent = node;
                }
                else
                {
                    // single function as child, swap with this node if this node is merely a compound
                    if (node.IsCompound)
                    {
                        child.parent = node.parent;
                        node = child;
                    }
                    else
                    {
                        // node is some other thing, leave it to be flattened below
                        break;
                    }
                }
            }

            // go through children 
            foreach (node child in node.children)
            {
                if (child.ChildCount > 0)
                {
                    switch (child.type)
                    {
                        case nodetype.compound:
                        case nodetype.assignment:
                            ReduceSingularCompounds(child);
                            break;
                    }
                }
            }

            return node;
        }



        /// <summary>
        /// create a pop node based on the information pushed, links the push and pop together 
        /// </summary>
        /// <param name="blast">current engine data</param>
        /// <param name="related_push">the earlier push op</param>
        /// <returns></returns>
        public static node CreatePopNode(BlastEngineDataPtr blast, node related_push)
        {
            node pop = new node(null);

            pop.identifier = "_gen_pop";
            pop.type = nodetype.function;
            unsafe
            {
                // reserved functions: can directly index on id 
                pop.function = blast.Data->Functions[(int)ReservedBlastScriptFunctionIds.Pop];
            }
            pop.is_vector = related_push.is_vector;
            pop.vector_size = related_push.vector_size;

            pop.linked_push = related_push;
            related_push.linked_pop = pop;

            return pop;
        }

        /// <summary>
        /// create a push node with the information from the given node, THIS DOES NOT ADD THAT NODE AS CHILD
        /// </summary>
        /// <param name="blast">blast engine data</param>
        /// <param name="topush">node to push</param>
        /// <returns>returns the pushing node</returns>
        static public node CreatePushNode(BlastEngineDataPtr blast, node topush)
        {
            node push = new node(null);
            push.identifier = "_gen_push";
            push.type = nodetype.function;

            unsafe
            {
                if (topush.IsFunction)
                {

                    push.function = blast.Data->Functions[(int)ReservedBlastScriptFunctionIds.PushFunction];

                }
                else
                {
                    if (topush.IsCompound)
                    {
                        if (topush.IsNonNestedVectorDefinition())
                        {
                            push.function = blast.Data->Functions[(int)ReservedBlastScriptFunctionIds.PushVector];
                        }
                        else
                        {
                            push.function = blast.Data->Functions[(int)ReservedBlastScriptFunctionIds.PushCompound];
                        }
                    }
                    else
                    {
                        push.function = blast.Data->Functions[(int)ReservedBlastScriptFunctionIds.Push];
                    }
                }
            }

            push.is_vector = topush.is_vector;
            push.vector_size = topush.vector_size;

            return push;
        }



        /// <summary>
        /// check if the list of given nodes is flat 
        /// - no nested things
        /// - no function calls ?? might want to be looser here... todo
        /// </summary>
        /// <param name="nodes">node list to check</param>
        /// <returns></returns>
        static public bool IsFlatParameterList(IEnumerable<node> nodes)
        {
            if (nodes == null || nodes.Count() == 0)
            {
                // an empty child list should be a very simple parameter list 
                return true;
            }

            foreach (node c in nodes)
            {
                if (c.children.Count > 0)
                {
                    // should have flattened in earlier stage 
                    return false;
                }
                if (c.type != nodetype.parameter)
                {
                    // allow a pop without parameters 
                    if (c.type == nodetype.function)
                    {
                        switch (c.function.FunctionId)
                        {
                            case (int)ReservedBlastScriptFunctionIds.Pop:
                                if (c.children.Count == 0)
                                {
                                    continue;
                                }
                                else
                                {
                                    break;
                                }
                        }
                    }
                    // todo check for vector compound .... 




                    // no should have been flattened 
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// check if the node defines a vector in a 'simple' way: no nesting, assumes vector-sizes in ast are set 
        /// <remarks>
        /// Performs a check on a node that only returns true if the node is marked as vector by analysis
        /// and its code is completely flattened, the vector elements must be a direct child of the node being 
        /// checked, there may be no compound in between as that should have been removed by the flatten operation
        /// 
        /// Targets: 
        /// 
        /// <code>
        /// a = (1 2); 
        /// a = (1 pop 2);
        /// a = (a 2 pop 3);
        /// </code>
        /// 
        /// It also matches vectors of differing sizes but the vector component count of the parameters/childnodes
        /// must equal the vectorsize of the assignee
        /// <code>
        /// a = (a 2 pop2);   
        /// </code>
        /// </remarks>
        /// </summary>
        public bool IsSimplexVectorDefinition()
        {
            if (!is_vector) return false;

            int c_vecsize = 0; // nr of components so far
            for (int i = 0; i < ChildCount; i++)
            {
                node child = children[i];
                switch (child.type)
                {
                    case nodetype.parameter:
                        if (child.HasChildren) return false;
                        break;

                    case nodetype.function:
                        // only pop is allowed 
                        if (!child.function.IsPopVariant) return false;
                        break;

                    case nodetype.compound:

                        //
                        // FLATTEN WILL REMOVE NESTED COMPOUNDS AND PUT THEM IN A PUSH CONSTRUCT 
                        // - we should allow flatten to keep it in this case, saving a push-pop pair during compilation 
                        //

                        if (!child.IsSimplexVectorDefinition()) return false;
                        break;

                    default: return false;
                }

                //
                // we have to assume this node.vectorsize is analyzed correctly before calling this function
                // - we dont check, could, but analyser should have taken care of it 
                // 
                c_vecsize += child.vector_size;

                if (c_vecsize > vector_size)
                {
                    return false;
                }
            }

            // if these match then OK 
            return c_vecsize == vector_size;
        }

        /// <summary>
        /// check if the node is a flat node                       
        /// - contains NO compounds 
        /// - contains no object with children other then a function
        /// </summary>
        /// <returns></returns>
        public bool IsFlat(bool allow_vector_root_compound = false)
        {
            if (ChildCount == 0) return true;
            foreach (node child in children)
            {
                switch (child.type)
                {
                    case nodetype.root:
                    case nodetype.function:
                    case nodetype.parameter:
                    case nodetype.operation:
                        break;

                    case nodetype.compound:
                        if (!allow_vector_root_compound) return false;
                        // only allowed if vector root is allowed 

                        foreach (node vc in child.children)
                        {
                            // eiter pops or parameters
                            if (!vc.IsSingleValueOrPop()) return false;
                        }
                        return child.ChildCount > 0;

                    default: return false;
                }
                if (!child.IsFunction && child.HasChildren) return false;
            }
            return true;
        }


        /// <summary>
        /// get maximum depth of node tree starting from this node 
        /// </summary>
        /// <param name="depth">depth from this node until deepest leaf</param>
        /// <returns>depth from this node until deepest leaf</returns>
        internal int GetMaximumTreeDepth(int depth)
        {
            int t = depth;

            foreach (node n in children)
            {
                t = math.max(t, n.GetMaximumTreeDepth(depth + 1));
            }

            return t;
        }


        /// <summary>
        /// get maximum depth of node tree starting from this node 
        /// </summary>
        /// <returns>depth from this node until deepest leaf</returns>
        public int GetMaximumTreeDepth()
        {
            int t = 0;

            foreach (node n in children)
            {
                t = math.max(t, n.GetMaximumTreeDepth(1));
            }

            return t;
        }

        /// <summary>
        /// set a node to be a child of this node (appends to end of child list),
        /// updates the parent of the node and removes it from its possible previous parent
        /// </summary>
        /// <param name="ast_node">the node to set as child is returned</param>
        internal node SetChild(node ast_node)
        {
            Assert.IsNotNull(ast_node);

            // remove it from current parent if any
            if (ast_node.parent != null)
            {
                ast_node.parent.children.Remove(ast_node);
            }

            // set node to this parent 
            ast_node.parent = this;
            children.Add(ast_node);

            return ast_node;
        }

        /// <summary>
        /// set a node to be a child of this node (appends to end of child list),
        /// updates the parent of the node and removes it from its possible previous parent
        /// </summary>
        /// <param name="ast_node">the node to set as child is returned</param>
        /// <param name="index">index to insert at at parent</param>
        internal node SetChild(node ast_node, int index)
        {
            Assert.IsNotNull(ast_node);

            // remove it from current parent if any
            if (ast_node.parent != null)
            {
                ast_node.parent.children.Remove(ast_node);
            }

            // set node to this parent 
            ast_node.parent = this;
            children.Insert(index, ast_node);

            return ast_node;
        }

        /// <summary>
        /// set a list of nodes a children to this node
        /// </summary>
        /// <param name="nodes">the nodes to set as child</param>
        /// <returns>this node</returns>
        internal node SetChildren(IEnumerable<node> nodes)
        {
            foreach (node n in nodes.ToArray()) // need to protect agains moving our own children and changing the collection
            {
                SetChild(n);
            }
            return this;
        }


        /// <summary>
        /// create a new node as a child of this node and returns the newly created node 
        /// </summary>
        /// <param name="type">nodetype to create</param>
        /// <param name="token">token to set</param>
        /// <param name="identifier">identifier used</param>
        /// <returns>the newly created node </returns>
        internal node CreateChild(nodetype type, BlastScriptToken token, string identifier)
        {
            node child_node = new node(null) { type = type, token = token, identifier = identifier };
            children.Add(child_node);
            child_node.parent = this;
            return child_node;
        }

        /// <summary>
        /// create a new node as a child of this node and returns the newly created node 
        /// </summary>
        /// <param name="type">nodetype to create</param>
        /// <param name="token">token to set</param>
        /// <param name="identifier">identifier used</param>
        /// <param name="index">index at which to insert node</param>
        /// <returns>the newly created node </returns>
        internal node CreateChild(nodetype type, BlastScriptToken token, string identifier, int index)
        {
            node child_node = new node(null) { type = type, token = token, identifier = identifier };
            children.Insert(index, child_node);
            child_node.parent = this;
            return child_node;
        }

        /// <summary>
        /// add an indexer to this node
        /// </summary>
        /// <param name="token"></param>
        /// <param name="identifier"></param>
        /// <returns></returns>
        internal node AppendIndexer(BlastScriptToken token, string identifier)
        {
            node n = new node(null) { type = nodetype.index, token = token, identifier = identifier };
            indexers.Add(n);
            n.parent = this;
            return n;
        }

        internal node AppendIndexer(BlastScriptToken token, blast_operation index_op)
        {
            node n = new node(null) { type = nodetype.index, token = token, identifier = string.Empty, constant_op = index_op };
            indexers.Add(n);
            n.parent = this;
            return n;
        }

        /// <summary>
        /// count child nodes of the given type
        /// </summary>
        /// <param name="type">the nodetype to count</param>
        /// <returns></returns>
        public int CountChildType(nodetype type)
        {
            return children == null ? 0 : children.Count(x => x.type == type);
        }

        /// <summary>
        /// count all children matching one of 2 types 
        /// </summary>
        public int CountChildType(nodetype type_a, nodetype type_b)
        {
            return children == null ? 0 : children.Count(x => x.type == type_a || x.type == type_b);
        }

        /// <summary>
        /// number of child nodes below this node
        /// </summary>
        public int ChildCount
        {
            get { return children == null ? 0 : children.Count; }
        }

        /// <summary>
        /// first child of node, null if there are no children
        /// </summary>
        public node FirstChild => ChildCount > 0 ? children[0] : null;

        /// <summary>
        /// last child of node, null if there are no children, equals first if childcount == 1
        /// </summary>
        public node LastChild => ChildCount > 0 ? children[ChildCount - 1] : null;


        internal bool SetIsVector(int _vector_size, bool propagate_to_parents = true)
        {
            // the root cannot be set as vector
            if (parent == null) return true;

            is_vector = _vector_size > 1;
            vector_size = _vector_size;

            // propagate vector state to parent -> dont do this if not a vector
            if (parent != null && propagate_to_parents && is_vector)
            {
                switch (type)
                {
                    case nodetype.function:
                        // depends on function if it returns a vector
                        if (parent.function.FunctionId > 0 && (parent.function.AcceptsVectorSize == 0 || parent.function.AcceptsVectorSize == _vector_size))
                        {
                            // function set, parameter vectorsize ok 
                            if (parent.function.ReturnsVectorSize == 0)
                            {
                                if (!parent.SetIsVector(_vector_size, true)) return false;
                            }
                            else
                            {
                                if (!parent.SetIsVector(parent.function.ReturnsVectorSize)) return false;
                            }
                        }
                        else
                        {
                            // half the callers of this method cannot know its relation so this is sometimes necessary 
                            // Debug.LogError($"setisvector: parameter vectorsize mismatch in node {parent}, function: {parent.function.Match} , expecting: {parent.function.AcceptsVectorSize}, given {_vector_size}"); 
                            return false;
                        }
                        break;

                    default:
                        if (!parent.SetIsVector(_vector_size, true)) return false;
                        break;
                }
            }

            return true;
        }


        internal bool ValidateType(IBlastCompilationData data, IEnumerable<nodetype> valid_node_types)
        {
            foreach (nodetype node_type in valid_node_types)
            {
                if (this.type == node_type)
                {
                    return true;
                }
            }
            data.LogError($"Node: <{this}> has a nodetype that is invalid in the current context");
            return false;
        }

        /// check if a node is a vector, its children should all be
        /// - identifiers
        /// - functions
        /// - compounds ( todo -> should validate if they result in a single number.... )
        internal bool CheckIfCouldBeVectorList()
        {
            foreach (node c in children)
            {
                switch (c.type)
                {
                    case nodetype.function:

                        if (c.is_vector)
                        {
                            break;
                        }
                        else
                        {
                            return false;
                        }


                    case nodetype.parameter:
                    case nodetype.compound:
                        continue;

                    default:
                        return false;
                }
            }
            // no children -> defines empty vector ... possibly error
            return true;
        }


        /// <summary>
        /// insert a new node of the given type and operation  before this node in parent 
        /// </summary>
        /// <param name="type">node type</param>
        /// <param name="token">token for script operation</param>
        public node InsertBeforeThisNodeInParent(nodetype type, BlastScriptToken token)
        {
            if (parent != null)
            {
                Assert.IsNotNull(parent.children);

                node n = new node(null) { type = type, token = token };
                n.parent = this.parent;

                parent.children.Insert(parent.children.IndexOf(this), n);
                return n;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// insert a child not at given index 
        /// - removes new child from old parent
        /// - set new childs parent to this node
        /// </summary>
        public node InsertChild(int index, node node)
        {
            Assert.IsNotNull(node);
            Assert.IsNotNull(children);

            if (node.parent != null && node.parent.ChildCount > 0) node.parent.children.Remove(node);

            children.Insert(0, node);
            node.parent = this;


            return node;
        }

        /// <summary>
        /// insert a node as new parent to this node => before: Parent.Child, after: Parent.NewNode.Child
        /// </summary>
        /// <param name="node">the node to insert/replace as a new parent</param>
        public node InsertParent(node node)
        {
            Assert.IsNotNull(node);
            Assert.IsNotNull(node.children);

            node.parent = this.parent;
            node.children.Add(this);

            this.parent = node;

            if (node.parent != null && node.parent.ChildCount > 0)
            {
                // replace old node in parents child list 
                int index = node.parent.children.IndexOf(this);
                Assert.IsTrue(index >= 0);
                node.parent.children[index] = node;
            }

            node.is_vector = this.is_vector;
            node.vector_size = this.vector_size;
            node.is_constant = this.is_constant;

            return node;
        }

        /// <summary>
        /// get the constant float value encoded by the variable 
        /// - float: vector size is checked and errors will be logged if incorrect 
        /// </summary>
        /// <returns></returns>
        internal float GetConstantFloatValue(IBlastCompilationData data)
        {
            if (is_constant)
            {
                // node should have op set on constant map
                if (constant_op != blast_operation.nop)
                {
                    // maps to constant op 
                    return Blast.GetConstantValueDefault(constant_op);
                }

                // if it encodes to some system constant op then get its value 
                float f = Blast.GetNamedSystemConstantValue(this.identifier);
                if (float.IsNaN(f))
                {
                    // not a system constant: we should be able to just parse it 
                    if (!float.IsNaN(f = this.AsFloat))
                    {
                        return f;
                    }
                    else
                    {
                        data.LogError($"node.getconstantfloatvalue: node [{this.ToString()}] value {this.identifier} could not be parsed as a constant");
                    }
                }
                else
                {
                    data.LogWarning($"node.GetConstantFloatValue.GetSystemConstantFloatValue(string): it should not be needed, any constant should have been captured earlier and mapped to operation linked to node, found id '{identifier}'");
                    return f;
                }
            }
            else
            {
                data.LogError($"node.getconstantfloatvalue: node [{this.ToString()}] is not a constant");
            }

            return float.NaN;
        }

        /// <summary>
        /// get the constant float4 value encoded by the variable 
        /// - float4: vector size is checked and errors will be logged if incorrect 
        /// </summary>
        public float4 GetConstantFloat4Value(IBlastCompilationData data)
        {
            data.LogToDo("vector constants not yet supported");
            return float4.zero;
        }


        /// <summary>
        /// generate a unique id based on a guid 
        /// </summary>
        /// <param name="tag">name to use for reference while debugging</param>
        /// <returns></returns>
        internal static string GenerateUniqueId(string tag = "gen")
        {
            return $"__{tag}_{Guid.NewGuid().ToString()}__";
        }


        /// <summary>
        /// make sure the identifier is set to a UniqueID  
        /// if it is not set a new id is generated
        /// </summary>
        /// <param name="tag">name to use for reference while debugging</param>
        public void EnsureIdentifierIsUniquelySet(string tag = "gen")
        {
            if (string.IsNullOrWhiteSpace(identifier))
            {
                identifier = GenerateUniqueId(tag);
            }
        }

        /// <summary>
        /// determine if the node is a single value or a pop operation
        /// </summary>
        public bool IsSingleValueOrPop()
        {
            return IsSingleValueOrPop(this);
        }

        /// <summary>
        /// determine if the node is a single value or a pop operation
        /// </summary>
        static public bool IsSingleValueOrPop(node node)
        {
            Assert.IsNotNull(node);

            if (node.HasChildren) return false;

            if (node.IsPopFunction) return true;
            if (node.IsScriptVariable) return true;
            if (node.constant_op != blast_operation.nop) return true;

            return false;
        }

        /// <summary>
        /// check if there is a function in the ast root with the given identifier as name 
        /// </summary>
        /// <param name="identifier">name of function</param>
        /// <returns>if a function with that name exists</returns>
        public bool HasInlineFunction(string identifier)
        {
            Assert.IsNotNull(identifier);
            Assert.IsTrue(!string.IsNullOrWhiteSpace(identifier), "calling this with an empty identifier is considered a bug");

            if (ChildCount <= 0) return false;
            if (string.IsNullOrWhiteSpace(identifier)) return false; // or should we assert as this is a buggy thing to do.. 

            identifier = identifier.Trim();
            for (int i = 0; i < ChildCount; i++)
            {
                node c = children[i];
                if (c.type == nodetype.inline_function)
                {
                    if (string.Compare(c.identifier, identifier, true) == 0) return true;
                }
            }

            return false;
        }

        /// <summary>
        /// check if the node has a parent of the given type, checks all nodes until reaching root  
        /// </summary>
        /// <param name="type">the type to find a parent for</param>
        /// <returns>true if the node has a parent with the given type</returns>
        public bool IsRootedIn(nodetype type)
        {
            node current = this;
            while (current.parent != null)
            {
                current = current.parent;
                if (current.type == type) return true;
            }
            return false;
        }

        /// <summary>
        /// skip compiling this node 
        /// </summary>
        public node SkipCompilation()
        {
            skip_compilation = true;
            return this;
        }
    }


    /// <summary>
    /// a node list cache, trashes the gc a little less using this, 
    /// only caches small lists, 1 for each thread using this
    /// </summary>
    public static class NodeListCache 
    {
        const int CACHED_CAPACITY = 64;
        [ThreadStatic]
        private static List<node> CachedInstance;

        /// <summary>
        /// get a new list of desired capacity
        /// </summary>
        public static List<node> Acquire(int capacity = CACHED_CAPACITY)
        {
            if (capacity <= CACHED_CAPACITY)
            {
                List<node> list = CachedInstance;
                if (list != null)
                {
                    if (capacity <= list.Capacity)
                    {
                        CachedInstance = null;
                        list.Clear();
                        return list;
                    }
                }
            }
            return new List<node>(capacity);
        }

        /// <summary>
        /// release the nodelist back to the static cache
        /// </summary>
        /// <param name="list"></param>
        public static void Release(List<node> list)
        {
            if (list.Capacity <= CACHED_CAPACITY)
            {
                CachedInstance = list;
            }
        }

        /// <summary>
        /// release the list and return its contents in an array
        /// </summary>
        public static node[] ReleaseArray(List<node> list)
        {
            if (list.Capacity <= CACHED_CAPACITY)
            {
                CachedInstance = list;
            }
            return list.ToArray();
        }
    }


    /// <summary>
    /// provide some extensions on list, using list as stack we hit the cache more
    /// </summary>
    public static class NodeListExtensions
    {
        /// <summary>
        /// push a node to the end of the list 
        /// </summary>
        /// <param name="list"></param>
        /// <param name="node"></param>
        public static void Push(this List<node> list, node node)
        {
            list.Add(node); 
        }

        /// <summary>
        /// push a list of nodes to the end of the list 
        /// - this reverses the input node order befor adding them to the end to preserve pop order
        /// </summary>
        /// <param name="list"></param>
        /// <param name="nodes"></param>
        public static void PushRange(this List<node> list, IEnumerable<node> nodes)
        {
            list.AddRange(nodes.Reverse());
        }

        /// <summary>
        /// try to pop a node from the end of the list 
        /// </summary>
        /// <param name="list"></param>
        /// <param name="node"></param>
        /// <returns></returns>
        public static bool TryPop(this List<node> list, out node node)
        { 
            if(list.Count > 0)
            {
                node = list[list.Count - 1];
                list.RemoveAt(list.Count - 1);
                return true; 
            }

            node = null;
            return false; 
        }
    }



}