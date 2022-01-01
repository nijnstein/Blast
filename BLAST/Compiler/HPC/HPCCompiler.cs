using System;
using System.CodeDom;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace NSS.Blast.Compiler.Stage
{
    /// <summary>
    /// Blast Compiler Stage                                       
    /// - compiles the ast into hpc code for compile-time scripts 
    /// </summary>
    public class BlastHPCCompiler : IBlastCompilerStage
    {
        public Version Version => new Version(0, 1, 0);
        public BlastCompilerStageType StageType => BlastCompilerStageType.Compile;


        static string GetOperationString(CompilationData data, BlastScriptToken token)
        {
            switch (token)
            {
                case BlastScriptToken.Nop: return string.Empty;
                case BlastScriptToken.Add: return "+"; 
                case BlastScriptToken.Substract: return "-";
                case BlastScriptToken.Divide: return "/"; 
                case BlastScriptToken.Multiply: return "*"; 
                case BlastScriptToken.Equals: return "=="; 
                case BlastScriptToken.Ternary: return "?";
                case BlastScriptToken.TernaryOption: return ":"; 
                case BlastScriptToken.SmallerThen: return "<";
                case BlastScriptToken.GreaterThen: return ">";
                case BlastScriptToken.SmallerThenEquals: return "<="; 
                case BlastScriptToken.GreaterThenEquals: return ">="; 
                case BlastScriptToken.NotEquals: return "!="; 
                case BlastScriptToken.And: return "&&"; 
                case BlastScriptToken.Or: return "||"; 
                case BlastScriptToken.Xor: return "^";
                case BlastScriptToken.Not: return "!";
                case BlastScriptToken.Comma: return ",";
                case BlastScriptToken.Indexer: return "."; 
                case BlastScriptToken.IndexOpen: return "[";
                case BlastScriptToken.IndexClose: return "]"; 

                case BlastScriptToken.OpenParenthesis: 
                case BlastScriptToken.CloseParenthesis:
                case BlastScriptToken.DotComma:
                case BlastScriptToken.If:
                case BlastScriptToken.Then:
                case BlastScriptToken.Else:
                case BlastScriptToken.While:
                case BlastScriptToken.For:
                case BlastScriptToken.Switch:
                case BlastScriptToken.Case:
                case BlastScriptToken.Default:
                    break;
            }
            data.LogError($"HPCCompiler.GetOperationString: Token is not an operation, token: {token}");
            return string.Empty; 
        }

        static string GetFunctionName(CompilationData data, ScriptFunctionDefinition function)
        {
            if (function != null)
            {
                // any function not standard
                if (!string.IsNullOrWhiteSpace(function.CSName))
                {
                    return function.CSName; 
                }

                switch (function.ScriptOp)
                {
                    case script_op.ret: return "return"; 
                    case script_op.fma: return "blast.fma";
                    case script_op.fsm: return "blast.fsm";
                    case script_op.fms: return "blast.fms"; // todo replace with fsm? 
                    case script_op.adda: return "blast.adda"; 
                    case script_op.mula: return "blast.mula"; 
                    case script_op.diva: return "blast.diva"; 
                    case script_op.suba: return "blast.suba";
                    case script_op.all: return "blast.all"; 
                    case script_op.any: return "blast.any"; 
                    case script_op.abs: return "math.abs"; 
                    case script_op.select: return "math.select";
                    
                    case script_op.random: return "blast.random"; 
                    case script_op.seed: return "blast.seed"; 
                    case script_op.max: return "blast.max"; 
                    case script_op.min: return "blast.min"; 
                    case script_op.maxa: return "blast.maxa";
                    case script_op.mina: return "blast.mina"; 

                    case script_op.lerp: return "math.lerp"; 
                    case script_op.slerp: return "math.slerp";
                    case script_op.saturate: return "math.saturate"; 
                    case script_op.clamp: return "math.clamp"; 
                    case script_op.normalize: return "math.normalize"; 
                    case script_op.ceil: return "math.ceil"; 
                    case script_op.floor: return "math.floor"; 
                    
                    case script_op.frac:
                        break;
                    
                    case script_op.sin: return "math.sin";
                    case script_op.cos: return "math.cos"; 
                    case script_op.tan: return "math.tan";
                    case script_op.atan: return "math.atan";
                    case script_op.cosh: return "math.cosh";
                    case script_op.sinh: return "math.sinh";
                    
                    case script_op.distance:
                        break;
                    case script_op.distancesq:
                        break;
                    case script_op.length:
                        break;
                    case script_op.lengthsq:
                        break;
                    case script_op.dot:
                        break;
                    case script_op.degrees:
                        break;
                    case script_op.radians:
                        break;
                    
                    case script_op.sqrt: return "math.sqrt";
                    case script_op.rsqrt: return "math.rsqrt"; 
                    case script_op.pow: return "math.pow"; 
                    case script_op.exp: return "math.exp"; 
                    case script_op.log2: return "math.log2"; 

                    case script_op.ex_op:
                        break;
                }
            }
            data.LogError("null function");
            return string.Empty;
        }

        /// <summary>
        /// compile a single parameter node into the code stream
        /// </summary>
        /// <param name="data"></param>
        /// <param name="ast_param">the parameter node</param>
        /// <param name="code">the code to append to</param>
        /// <returns>false on failure</returns>
        static public bool CompileParameter(CompilationData data, node ast_param, StringBuilder code)
        {
            if (ast_param == null || string.IsNullOrWhiteSpace(ast_param.identifier))
            {
                data.LogError($"HPCCompiler.CompileParameter: can not compile parameter with unset identifiers, node: <{ast_param.parent}>.<{ast_param}>");
                return false;
            }

            if (ast_param.type == nodetype.function)
            {
                data.LogError($"HPCCompiler.CompileParameter: function as parameter, this is not supported: <{ast_param.parent}>.<{ast_param}>");
                return false;
            }

            if (ast_param.is_constant)
            {
                code.Append(ast_param.GetConstantFloatValue(data));
            }
            else
            {
                if (ast_param.type == nodetype.function
                    &&
                    (ast_param.function.FunctionId == (int)ReservedScriptFunctionIds.Pop
                    ||
                    ast_param.function.FunctionId == (int)ReservedScriptFunctionIds.Pop2
                    ||
                    ast_param.function.FunctionId == (int)ReservedScriptFunctionIds.Pop3
                    ||
                    ast_param.function.FunctionId == (int)ReservedScriptFunctionIds.Pop4
                ))
                {
                    switch ((ReservedScriptFunctionIds)ast_param.function.FunctionId)
                    {
                        case ReservedScriptFunctionIds.Pop:
                            code.Append("(--___bs_stack)[0]");
                            break;
                        default:
                            data.LogError($"HPCCompiler.CompileParameter: vector pop not yet supported, node: <{ast_param.parent}>.<{ast_param}>");
                            return false; 
                    }
                }
                else
                {
                    code.Append($"___gen_bs_variables[{ data.Variables.IndexOf(ast_param.variable) }]");
                }
            }              

            return true;
        }

        /// <summary>
        /// compile a list of nodes as a list of parameters
        /// </summary>
        /// <param name="data"></param>
        /// <param name="ast_node">the node with the function</param>
        /// <param name="code">the stringbuilder to add hpc code to</param>
        /// <param name="parameter_nodes">the parameter list</param>
        /// <param name="min_validate">min parameter count, error will be raised if less</param>
        /// <param name="max_validate">max parameter count, error will be raised if more</param>
        /// <returns></returns>
        static public bool CompileParameters(CompilationData data, node ast_node, StringBuilder code, IEnumerable<node> parameter_nodes, int min_validate, int max_validate)
        {
            int c = parameter_nodes == null ? 0 : parameter_nodes.Count();

            if (c == 0)
            {
                // nothing to compile, ok if min_validate == 0 
                if (min_validate == 0)
                {
                    return true;
                }
                else
                {
                    data.LogError($"HPCCompiler.CompileParameters: failed to compile parameter list in node <{ast_node}>, no parameters found for function that needs at least {min_validate} parameters");
                    return false;
                }
            }

            // verify parameter count 
            if (c < min_validate || c > max_validate)
            {
                data.LogError($"HPCCompiler.CompileParameters: node <{ast_node}>, parameter count mismatch, expecting {min_validate}-{max_validate} parameters but found {c}");
                return false;
            }

            // verify each node has no children, flatten should have removed any nesting 
            foreach (node n_param in parameter_nodes)
            {
                if (n_param.children.Count > 0)
                {
                    data.LogError($"HPCCompiler.CompileParameters: failed to compile parameter list, only single values, constants and stack operations are supported, parameter node: <{n_param}> ");
                    return false;
                }
            }

            // compile each parameter into the code list
            var nodes = parameter_nodes.ToArray(); 
            for(int i = 0; i < nodes.Length; i++)
            {
                node n_param = nodes[i];
            
                if (!CompileParameter(data, n_param, code))
                {
                    data.LogError($"HPCCompiler.CompileParameters: failed to compile parameter: <{ast_node}>.<{n_param}>");
                    return false;
                }

                if(i != nodes.Length - 1)
                {
                    // add , if not the last param
                    code.Append(", ");
                }
            }

            return true;
        }


        static int CompileAssignment(HPCCompilationData data, StringBuilder code, node n)
        {
            int ires = (int)BlastError.success;

            code.Append($"___gen_bs_variables[{ data.GetVariable(n.identifier).Id }] = ");

            foreach (node c in n.children)
            {
                switch(c.type)
                {
                    case nodetype.parameter:
                        if(!CompileParameter(data, c, code))
                        {
                            data.LogError($"HPCCompiler.CompileAssignment: failed to compile parameter <{n}>.<{c}>");
                        }
                        code.Append(" ");
                        break;
                    
                    case nodetype.operation: 
                        code.Append(GetOperationString(data, c.token)); 
                        code.Append(" "); 
                        break;

                    case nodetype.function:
                        ires = CompileFunction(data, code, c);
                        code.Append(" ");
                        break; 

                    default:
                        data.LogError($"HPCCompiler.CompileAssignement: construct not supported yet <{n}>.<{c}>");
                        break; 
                }

                if(!data.IsOK)
                {
                    ires = (int)BlastError.error;
                    break; 
                }
            }

            code.AppendLine(";");  
            return ires; 
        }

        static int CompileFunction(HPCCompilationData data, StringBuilder code, node n)
        {
            if(n == null)
            {
                data.LogError("HPCCompiler.CompileFunction: tried to compile NULL node"); 
                return (int)BlastError.error; 
            }
            if(n.type != nodetype.function)
            {
                data.LogError($"HPCCompiler.CompileFunction: node not a function: <{n.parent}>.<{n}>");
                return (int)BlastError.error_invalid_nodetype;
            }
            if (n.function == null)
            {
                data.LogError($"HPCCompiler.CompileFunction: node <{n}> has no function set");
                return (int)BlastError.error;
            }

            // special cases: yield and push
            switch (n.function.ScriptOp)
            {
                case script_op.yield:
                    code.Append("Yield(___bs_stack_root, ___bs_stack_size "); 
                    if(n.HasChildNodes)
                    {
                        code.Append(", ");
                        if (!CompileParameters(data, n, code, n.children, 0, 1))
                        {
                            data.LogError($"HPCCompiler.CompileFunction: failed to compile yield parameters in node <{n}>");
                            return (int)BlastError.error_failed_to_compile_function_parameters;
                        }
                        code.Append(");");
                    }
                    return (int)BlastError.success; 

                    //            float* stack = stackalloc float[stack_size];
                    //            stack[0] = 3; stack++;
                    //            variables[0] = (--stack)[0];
                case script_op.push:

                    code.Append($"___bs_stack[0] = ");
                    if (!CompileParameter(data, n.children[0], code))
                    {
                        data.LogError($"HPCCompiler.CompileFunction.push: failed to compile parameter <{n.children[0]}> of function <{n}>");
                        return (int)BlastError.error_failed_to_compile_function_parameters;
                    }
                    code.AppendLine("; ___bs_stack++;");

                    return (int)BlastError.success;

                case script_op.pushv:
                    data.LogError($"HPCCompiler.CompileFunction: node <{n}> encodes pushv operation, this is not supported in hpc code and should have been converted into a variable assignment");
                    return (int)BlastError.error_unsupported_operation;
            }

            // check if the list of children is flat     
            // although it is enforced in the bytecode, hpc could allow but we dont
            if (node.IsFlatParameterList(n.children))
            {
                bool has_variable_paramcount = n.function.MinParameterCount != n.function.MaxParameterCount;

                if (n.function.ScriptOp != script_op.nop)
                {
                    // output name of function followed by parameter name 
                    code.Append(GetFunctionName(data, n.function));

                    if (n.children.Count > 63)
                    {
                        // still enforce this although it is supported by hpc
                        data.LogError($"HPCCompiler.CompileFunction: too many parameters in node <{n.parent}>.<{n}>, only 63 are max supported but found {n.children.Count}");
                        return (int)BlastError.error_too_many_parameters;                                                              
                    }

                    code.Append("("); 
                    if (n.function.CanHaveParameters)
                    {
                        if (!CompileParameters(data, n, code, n.children, n.function.MinParameterCount, n.function.MaxParameterCount))
                        {
                            data.LogError($"HPCCompiler.CompileFunction: failed to compile parameters for fuction: <{n.parent}>.<{n}>");
                            return (int)BlastError.error_failed_to_compile_function_parameters;
                        }
                    }
                    code.Append(")");
                }
                else
                {
                    // called via external...
                    data.LogToDo($"CompileFunction - support for external function calls via function pointers (burst)");
                    data.LogError("CompileFunction external function calls not yet supported");
                    return (int)BlastError.error_unsupported_operation;  
                }
            }
            else
            {
                data.LogError($"HPCCompiler.CompileFunction: complex children are not allowed in this stage; node <{n.parent}>.<{n}> not flat");
                return (int)BlastError.error_node_not_flat;
            }

            return (int)BlastError.success;
        }



        static int CompileNode(HPCCompilationData data, StringBuilder code, node n)
        {
            if (n == null)
            {
                data.LogError("HPCCompiler.CompileFunction: tried to compile NULL node");
                return (int)BlastError.error;
            }
            int ires = (int)BlastError.success; 
            switch (n.type)
            {
                case nodetype.assignment:
                    {
                        ires = CompileAssignment(data, code, n); 
                    }  
                    break;

                    /// functions 
                    /// if then else
                    /// while..... 

                default:
                    {
                        data.LogError($"BlastHPCCompiler: failed to compile node: <{n}>, nodetype {n.type} is not supported for compilation yet ");
                        ires = (int)BlastError.error;
                        break;
                    }
            }

            return ires; 
        }


        /// <summary>
        /// Compile the abstract syntax tree into a c# job to be used as hpc for burst by unity
        /// </summary>
        /// <param name="data">compilerdata, among which the AST</param>
        /// <returns>a string containg the code</returns>
        static string CompileAST(HPCCompilationData data, int indent = 0)
        {
            if(data == null || data.AST == null)
            {
                return null; 
            }

            StringBuilder code = new StringBuilder();

            // should determine stack size, if null, dont use it ... TODO 
            data.LogToDo("determine stack size at hpccompile"); 

            int max_stack_size = 128;
            code.AppendLine($"const int ___bs_stack_size = {max_stack_size};"); 
            code.AppendLine($"float* ___bs_stack_root = stackalloc float[___bs_stack_size];");
            code.AppendLine("float* ___bs_stack = ___bs_stack_root;"); 

            // structure 
            foreach (node n in data.AST.children)
            {
                int ires = CompileNode(data, code, n);
                if (ires != 0) return string.Empty; 
            }

            if(indent > 0)
            {
                // indent all code 
                using(StringReader sr = new StringReader(code.ToString()))
                {
                    code.Clear();
                    string s, padding = "".PadLeft(indent, ' ');
                    while((s = sr.ReadLine()) != null)
                    {
                        code.AppendLine(padding + s); 
                    }
                }
            }

            return code.ToString(); 
        }


        /// <summary>
        /// Execute the compilation stage, prepares c# code ready for the burst compiler
        /// </summary>
        /// <param name="data">compiler data</param>
        /// <returns>non zero on error conditions</returns>
        public int Execute(IBlastCompilationData data)
        {
            HPCCompilationData cdata = (HPCCompilationData)data;

            if(string.IsNullOrWhiteSpace(data.Script.Name))
            {
                data.Script.Name = "bsjob_" + Guid.NewGuid().ToString().Replace('-', '_');  
            }
            string code_id = data.Script.Name; 

            // compile into bytecode 
            string code = CompileAST(cdata, 12);

            if (cdata.IsOK && !string.IsNullOrWhiteSpace(code))
            {
                // package inside a job struct (for now.. function pointers might be better suited)
                string job = @"  
    [BurstCompile]
    public unsafe struct {0} : IBlastHPCScriptJob
    {{
        public int ScriptId => {2};

        [BurstCompile]
        static void Execute(IntPtr engine, float* ___gen_bs_variables)
        {{
{1}
        }}

        [BurstDiscard]
        public FunctionPointer<BSExecuteDelegate> GetBSD()
        {{
            return BurstCompiler.CompileFunctionPointer<BSExecuteDelegate>(Execute);
        }}
    }} ";
                string result = string.Format(job, code_id, code, data.Script.Id);

                UnityEngine.Debug.Log(result);

                cdata.HPCCode = result; 

            }
   
            return data.IsOK ? (int)BlastError.success : (int)BlastError.error;
        }


    }


           /*
    [BurstCompile]
    public unsafe struct bsjob_a70adf42_4c34_4cdb_978c_70733dbde991 : IBlastHPCScriptJob
    {
        public int ScriptId => 1;

        [BurstCompile]
        static void Execute(IntPtr engine, float* ___gen_bs_variables)
        {
            const int ___gen_stack_size = 2;
            float* ___gen_stack_root = stackalloc float[___gen_stack_size];
            float* ___gen_stack = ___gen_stack_root;
            ___gen_stack[0] = 3; ___gen_stack++;
            ___gen_bs_variables[0] = (--___gen_stack)[0];
            ___gen_bs_variables[0] = 2 * 3;
            ___gen_bs_variables[1] = ___gen_bs_variables[0] * 5;
            ___gen_bs_variables[2] = ___gen_bs_variables[0] * ___gen_bs_variables[1];
            ___gen_bs_variables[3] = ___gen_bs_variables[0] / ___gen_bs_variables[2];
        }

        [BurstDiscard]
        public FunctionPointer<BSExecuteDelegate> GetBSD()
        {
            return BurstCompiler.CompileFunctionPointer<BSExecuteDelegate>(Execute);
        }
    }        */



}
