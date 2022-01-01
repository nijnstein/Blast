﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Unity.Assertions;
using Unity.Mathematics;

namespace NSS.Blast.Compiler
{
    /// <summary>
    /// the different nodetypes used in the ast
    /// </summary>
    public enum nodetype
    {
        none,
        root,
        function,
        assignment,
        parameter, index,
        operation,
        yield,
        compound,

        ifthenelse, ifthen, ifelse,

        condition,

        whileloop, whilecompound,

        switchnode, switchcase, switchdefault,

        forloop,

        jump_to, label
    }

    /// <summary>
    /// an ast node used by the blast compiler
    /// </summary>
    public class node
    {
        public node parent;
        public nodetype type;
        public ScriptFunctionDefinition function;
        public BlastScriptToken token;
        public string identifier;
        public BlastVariable variable = null;

        public List<node> children = new List<node>();
        public List<node> depends_on = new List<node>();
        public List<node> indexers = new List<node>();

        public script_op constant_op = script_op.nop;
        public bool skip_compilation;
        public bool is_constant;
        public int vector_size;
        public bool is_vector;


        public bool is_float {
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
                    if (ch == '-') {
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
                        if(c != 0 || ch == 0 || ch == identifier.Length - 1) return false;
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

        public static float AsFloat(string s)
        {
            float f = float.NaN;

            if (string.IsNullOrWhiteSpace(s))
            {
                return f;
            }

            if (CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator == ",")
            {
                /*if (s.Contains('.')) */
                s = s.Replace('.', ',');
            }
            else
            {
                s = s.Replace(',', '.');
            }


            //if(
            float.TryParse(s, out f);//)
            {
                return f;
            }
            //else
            //{
            // NaN
            //  return f;
            //}
        }
        public float as_float
        {
            get
            {
                return AsFloat(identifier);
            }
        }


        public node(node _parent, params node[] _children)
        {
            parent = _parent;
            skip_compilation = false;
            is_vector = false;
            is_constant = false;
            if (parent != null) parent.children.Add(this);
            if (_children != null) children.AddRange(_children);
        }
        public node(nodetype _type, BlastScriptToken _token)
        {
            parent = null;
            skip_compilation = false;
            is_vector = false;
            is_constant = false;
            token = _token;
            type = _type;
        }

        public override string ToString()
        {
            string sconstant = is_constant ? "constant " : "";
            string svector = is_vector ? " vector " : "";
            switch (type)
            {
                case nodetype.root: return $"{sconstant}{svector}root of {children.Count}";
                case nodetype.assignment: return $"{sconstant}{svector}assignment of {identifier}";
                case nodetype.compound: return $"{sconstant}{svector}compound statement of {children.Count}";
                case nodetype.function: return $"{sconstant}{svector}function {function.Match}";
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
                default: return base.ToString();
            }
        }

        /// <summary>
        /// generate a multiline string representing the node tree structure
        /// </summary>
        /// <param name="indentation">nr of space to indent for each child iteration</param>
        public string ToNodeTreeString(int indentation = 3)
        {
            return log_nodes(this, null, indentation);

            string log_nodes(node n, StringBuilder sb = null, int indent = 3)
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

                sb.AppendLine($"{"".PadLeft(indent, ' ')}{n.ToString()} {dependencies}");
                if (n.children.Count > 0)
                {
                    foreach (node c in n.children)
                    {
                        log_nodes(c, sb, indent + 3);
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
        }

        /// <summary>
        /// add a node to the list of nodes to depend on, these are tobe inserted before 
        /// this node during compilation, the parent of the node is updated to this
        /// </summary>
        /// <param name="n">the node to add</param>
        internal void AddNodeToDependsOn(node n)
        {
            if(n == null)
            {
                return; 
            }
            if(depends_on == null) 
            {
                depends_on = new List<node>(); 
            }
            n.parent = this;
            depends_on.Add(n); 
        }

        /// <summary>
        /// get the first child found of a given nodetype
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public node get_child(nodetype t)
        {
            return children.FirstOrDefault(x => x.type == t);
        }

        public node get_child(nodetype first_choice, nodetype second_choice)
        {
            node c = get_child(first_choice); 
            if(c == null)
            {
                c = get_child(second_choice);
            }
            return c; 
        }

        /// <summary>
        /// get child nodes not of the type t
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public node[] get_other_children(nodetype t)
        {
            return children.Where(x => x.type != t).ToArray();
        }

        /// <summary>
        /// deep clones node without root parent set
        /// </summary>
        /// <returns></returns>
        public node deep_clone()
        {
            node n = new node(null);
            n.parent = parent;
            n.children = new List<node>();
            n.indexers = new List<node>();

            // note:  dependancy cloning not supported  

            foreach (node child in children) n.children.Add(child.deep_clone());
            foreach (node child in n.children) child.parent = n;

            foreach (node child in indexers) n.children.Add(child.deep_clone());
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
        public List<node> GatherLeafNodes()
        {
            List<node> leafs = new List<node>();
            gather_leaf_nodes(leafs, this);
            return leafs;
        }

        static void gather_leaf_nodes(List<node> leafs, node n)
        {
            if (n.children.Count == 0)
            {
                leafs.Add(n);
            }
            else
            {
                foreach (node c in n.children)
                {
                    if (c.children.Count > 0)
                    {
                        gather_leaf_nodes(leafs, c);
                    }
                    else
                    {
                        leafs.Add(c);
                    }
                }
            }
        }


        /// <summary>
        /// check if the node contains only a single function call 
        /// </summary>
        /// <returns></returns>
        public bool is_only_1_function()
        {
            //    
            return false;
        }

        /// <summary>
        /// check if we have 1 child and that it is a compounded statement list
        /// </summary>
        /// <returns></returns>
        public bool has_single_compound_as_child()
        {
            return children.Count == 1 && children[0].type == nodetype.compound;
        }

        /// <summary>
        /// check if a given function is used in this node or its children
        /// </summary>
        /// <param name="op">the op representing the function</param>
        /// <returns>true if used</returns>
        internal bool check_if_function_is_used_in_tree(script_op op)
        {
            if (this.function != null && this.function.ScriptOp == op)
            {
                return true;
            }
            foreach (var ch in children)
            {
                if (ch.check_if_function_is_used_in_tree(op))
                {
                    return true;
                }
            }
            return false;
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
                            case (int)ReservedScriptFunctionIds.Pop:
                            case (int)ReservedScriptFunctionIds.Pop2:
                            case (int)ReservedScriptFunctionIds.Pop3:
                            case (int)ReservedScriptFunctionIds.Pop4:
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
        /// get maximum depth of node tree 
        /// </summary>
        /// <param name="depth"></param>
        /// <returns></returns>
        internal int get_maximum_depth(int depth = 0)
        {
            int t = depth;

            foreach (node n in children)
            {
                t = math.max(t, n.get_maximum_depth(depth + 1));
            }

            return t;
        }

        /// <summary>
        /// set a node to be a child of this node (appends to end of child list),
        /// updates the parent of the node and removes it from its possible previous parent
        /// </summary>
        /// <param name="ast_node">the node to set as child</param>
        internal void SetChild(node ast_node)
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
        }

        /// <summary>
        /// create a new node as a child of this node
        /// </summary>
        /// <param name="type">nodetype to create</param>
        /// <param name="token">token to set</param>
        /// <param name="identifier">identifier used</param>
        /// <returns></returns>
        internal node CreateChild(nodetype type, BlastScriptToken token, string identifier)
        {
            node child_node = new node(null) { type = type, token = token, identifier = identifier };
            children.Add(child_node);
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
            return n;
        }

        /// <summary>
        /// count child nodes of the given type
        /// </summary>
        /// <param name="type">the nodetype to count</param>
        /// <returns></returns>
        internal int CountChildType(nodetype type)
        {
            return children == null ? 0 : children.Count(x => x.type == type);
        }

        internal bool HasChildNodes => children.Count > 0;

        internal void SetIsVector(int _vector_size, bool propagate_to_parents = true)
        {
            if (parent == null) return; // the root cannot be set as vector

            is_vector = _vector_size > 1;
            vector_size = _vector_size;

            // propagate vector state to parent -> dont do this if not a vector // todo verify 
            if (parent != null && propagate_to_parents && !is_vector)
            {
                switch (type)
                {
                    case nodetype.function:
                        // depends on function if it returns a vector
                        break;

                    default:
                        parent.SetIsVector(_vector_size, true);
                        break;
                }
            }
        }

        internal bool ValidateType(IBlastCompilationData data, IEnumerable<nodetype> valid_node_types)
        {
            foreach(nodetype node_type in valid_node_types)
            {
                if(this.type == node_type)
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
        /// <param name="op">script operation</param>
        internal node InsertBeforeThisNodeInParent(nodetype type, BlastScriptToken token)
        {
            if (parent != null)
            {
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
        /// get the constant float value encoded by the variable 
        /// - float: vector size is checked and errors will be logged if incorrect 
        /// </summary>
        /// <returns></returns>
        internal float GetConstantFloatValue(IBlastCompilationData data)
        {
            if (is_constant)
            {
                /// node should have op set on constant map
                if (constant_op != script_op.nop)
                {
                    // maps to constant op 
                    return data.Blast.GetScriptOpFloatValue(constant_op);
                }

                // if it encodes to some system constant op then get its value 
                float f = data.Blast.GetSystemConstantFloatValue(this.identifier);
                if (float.IsNaN(f))
                {
                    // not a system constant: we should be able to just parse it 
                    if (!float.IsNaN(f = this.as_float))
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
            if(string.IsNullOrWhiteSpace(identifier))
            {
                identifier = GenerateUniqueId(tag); 
            }
        }

    }
}