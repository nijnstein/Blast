using NSS.Blast.Compiler.Stage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using UnityEngine;

namespace NSS.Blast.Compiler
{
    /// <summary>
    /// interface into compilationdata, we should restrict outside use a bit via this 
    /// </summary>
    public interface IBlastCompilationData
    {
        Version Version { get; }
        Blast Blast { get; }
        BlastScript Script { get; }
        BlastCompilerOptions CompilerOptions { get; }

        node AST { get; }

        List<Tuple<BlastScriptToken, string>> Tokens { get; set; }
        List<BlastVariable> Variables { get; set; }
        List<byte> Offsets { get; set; }
        List<Tuple<int, int>> Jumps { get; set; }
        Dictionary<string, string> Defines { get; set; }
        List<BlastVariableMapping> Inputs { get; set; }
        List<BlastVariableMapping> Outputs { get; set; }
        Dictionary<string, string> Validations { get; set; }

        void LogError(string msg, int code = 0, int linenr = 0, [CallerMemberName] string member = "");
        void LogTrace(string msg, int linenr = 0, [CallerMemberName] string member = "");
        void LogWarning(string msg, int linenr = 0, [CallerMemberName] string member = "");
        void LogToDo(string msg, int linenr = 0, [CallerMemberName] string member = "");

        bool IsOK { get; }
        bool CanValidate { get; }
        bool HasDefines { get; }

        BlastVariable GetVariable(string name);
        BlastVariable GetVariableFromOffset(byte offset);
        bool TryGetDefine(string identifier, out string defined_value);

        bool ExistsVariable(string name);
        bool HasInput(int id);
        bool HasInput(string name);
        bool HasVariables { get; }
        bool HasOffsets { get; }
        int VariableCount { get; }
        int OffsetCount { get; }

    }

    /// <summary>
    /// Data created during compilation and used for analysis and packaging
    /// </summary>
    public class CompilationData : IBlastCompilationData
    {
        protected Version version = new Version(0, 1, 0);
        public Version Version => version; 

        #region Compiler Messages 
        public class Message
        {
            public enum MessageType { Normal, Warning, Error, ToDo, Trace };

            public DateTime Timestamp = DateTime.Now;
            public MessageType Type = MessageType.Normal;

            public int LineNumber = 0;
            public string CallerMember = string.Empty;

            public string Content = string.Empty;
            public int Code = 0;

            public bool IsError => Type == MessageType.Error;
            public bool IsWarning => Type == MessageType.Warning || Type == MessageType.ToDo;
            public bool IsNormal => Type == MessageType.Normal || Type == MessageType.Trace;
            public bool IsTrace => Type == MessageType.Trace;

            public override string ToString()
            {
                return $"{DateTime.Now.ToString()} {(Content ?? string.Empty)}";
            }
        }

        public List<Message> CompilerMessages = new List<Message>();

        /// <summary>
        /// on older .net versions we could get the stackframe 
        /// see: https://stackoverflow.com/questions/12556767/how-do-i-get-the-current-line-number
        /// and: https://stackoverflow.com/questions/38476796/how-to-set-net-core-in-if-statement-for-compilation
        /// </summary>

        public void LogMessage(string msg, [CallerLineNumber] int linenr = 0, [CallerMemberName] string member = "")
        {
            if (CompilerOptions.Report || CompilerOptions.Trace) CompilerMessages.Add(new Message() { Content = msg, LineNumber = linenr, CallerMember = member });
            if (CompilerOptions.Verbose)
            {
#if !NOT_USING_UNITY
                Standalone.Debug.Log(msg);
#else 
                System.Diagnostics.Debug.WriteLine(msg);
#endif
            }
        }
        public void LogTrace(string msg, [CallerLineNumber] int linenr = 0, [CallerMemberName] string member = "")
        {
            if (CompilerOptions.Report || CompilerOptions.Trace) CompilerMessages.Add(new Message() { Content = msg, LineNumber = linenr, CallerMember = member });
            if (CompilerOptions.Verbose && CompilerOptions.Trace)
            {
                Standalone.Debug.Log(msg);
            }
        }

        public void LogError(string msg, int code = 0, [CallerLineNumber] int linenr = 0, [CallerMemberName] string member = "")
        {
            LastErrorMessage = msg;
            CompilerMessages.Add(new Message() { Type = Message.MessageType.Error, Content = msg, Code = 0, LineNumber = linenr, CallerMember = member });
            Standalone.Debug.LogError(msg);
        }

        public void LogWarning(string msg, [CallerLineNumber] int linenr = 0, [CallerMemberName] string member = "")
        {
            CompilerMessages.Add(new Message() { Type = Message.MessageType.Warning, Content = msg, LineNumber = linenr, CallerMember = member });
            Standalone.Debug.LogWarning(msg);
        }

#if !NOT_USING_UNITY
        string FormatWithColor(string msg, Color rgb)
        {
            return string.Format(
                "<color=#{0:X2}{1:X2}{2:X2}>{3}</color>", 
                (byte)(rgb.r * 255f), (byte)(rgb.g * 255f), (byte)(rgb.b * 255f), msg);
        }
#endif

        public void LogToDo(string msg, [CallerLineNumber] int linenr = 0, [CallerMemberName] string member = "")
        {
            if (CompilerOptions.Report || CompilerOptions.Trace) CompilerMessages.Add(new Message() { Type = Message.MessageType.ToDo, Content = msg, LineNumber = linenr, CallerMember = member });
            if (CompilerOptions.Verbose || CompilerOptions.Trace)
            {
                Standalone.Debug.LogWarning("TODO: " + msg);
            }
        }

        public string LastErrorMessage { get; private set; }
        public int ErrorCount => CompilerMessages.Count(x => x.IsError);
        public bool HasErrors => CompilerMessages.Any(x => x.IsError);
        public int WarningCount => CompilerMessages.Count(x => x.IsWarning);
        public bool HasWarnings => CompilerMessages.Any(x => x.IsWarning);
        public bool HasErrorsOrWarnings => HasErrors || HasWarnings;
        public bool Success { get { return string.IsNullOrEmpty(LastErrorMessage); } }


#endregion

        public CompilationData(Blast blast, BlastScript script, BlastCompilerOptions options)
        {
            Blast = blast;
            Script = script;
            CompilerOptions = options;

            // -- tokenizer 
            Tokens = new List<Tuple<BlastScriptToken, string>>();
            Defines = new Dictionary<string, string>();
            Validations = new Dictionary<string, string>();

            // -- parser 
            root = new node(null) { type = nodetype.root };
            Inputs = new List<BlastVariableMapping>();
            Outputs = new List<BlastVariableMapping>();
            Variables = new List<BlastVariable>();
            Offsets = new List<byte>();

            // -- compiler 
            Jumps = new List<Tuple<int, int>>();
        }

        /// <summary>
        /// the data is OK when there is no known errormessage 
        /// </summary>
        public bool IsOK { get { return Success; } }

        /// <summary>
        /// global blast manager 
        /// </summary>
        public Blast Blast { get; set; }

        /// <summary>
        /// the input script 
        /// </summary>
        public BlastScript Script { get; }

        /// <summary>
        /// the intermediate 
        /// </summary>
        public BlastIntermediate Executable;

        /// <summary>
        /// Options used during compilation 
        /// </summary>
        public BlastCompilerOptions CompilerOptions { get; set; }

#region Code Display 


        /// <summary>
        /// this version has a little more information than the generic package bytecode reader 
        /// </summary>
        /// <returns></returns>
        public unsafe string GetHumanReadableCode()
        {
            StringBuilder sb = StringBuilderCache.Acquire();

            if (Executable.code_size > 0)
            {
                List<byte> bytes = new List<byte>(Executable.code_size);
                for (int i = 0; i < bytes.Count; i++) bytes.Add(Executable.code[i]);
                WriteHumanReadableCode(sb, bytes);

                // ending a jump after program end?
                foreach (var jump in Jumps.Where(x => x.Item1 + x.Item2 >= bytes.Count))
                {
                    if (jump != null)
                    {
                        sb.Append($"@label_{Jumps.IndexOf(jump)} <<< ERROR CONDITION - OUT OF BOUNDS - {jump.Item1} {jump.Item2} >>> ");
                    }
                }
            }
            else
            {
                if (code != null && code.list != null && code.list.Count > 0)
                {
                    List<byte> bytes = new List<byte>(code.list.Count);
                    for (int i = 0; i < code.list.Count; i++) bytes.Add(code.list[i].code); 
                    WriteHumanReadableCode(sb, bytes);
                }
                else
                {
                    sb.Append("-no code available to convert to string-");
                }
            }

            return StringBuilderCache.GetStringAndRelease(ref sb);


        }
        public unsafe void WriteHumanReadableCode(StringBuilder sb, List<byte> code)
        {
            int i = 0;
            script_op prev = script_op.nop;

            for (; i < code.Count; i++)
            {
                byte op = code[i];

                if (i % 10 == 0)
                {
                    if (i != 0) sb.Append("\n");
                    sb.Append("#" + i.ToString().PadLeft(3, '0') + " ");
                }


                // starting a jump ? 
                Tuple<int, int> jump = Jumps.FirstOrDefault(x => x.Item1 == i);
                if (jump != null)
                {
                    sb.Append($"@jmp_{Jumps.IndexOf(jump)} ");
                }

                // ending a jump?
                jump = Jumps.FirstOrDefault(x => x.Item1 + x.Item2 == i);
                if (jump != null)
                {
                    sb.Append($"@label_{Jumps.IndexOf(jump)} ");
                }

                if (prev == script_op.jump ||
                    prev == script_op.jump_back ||
                    prev == script_op.jz ||
                    prev == script_op.jnz)
                {
                    sb.Append(((byte)op).ToString() + " ");

                    prev = script_op.nop;
                }
                else
                {
                    switch ((script_op)op)
                    {
                        case script_op.nop: sb.Append("nop "); break;
                        case script_op.assign: sb.Append("set "); break;
                        case script_op.add: sb.Append("+ "); break;
                        case script_op.substract: sb.Append("- "); break;
                        case script_op.multiply: sb.Append("* "); break;
                        case script_op.divide: sb.Append("/ "); break;
                        case script_op.begin: sb.Append("( "); break;
                        case script_op.end: sb.Append(") "); break;
                        case script_op.and: sb.Append("& "); break;
                        case script_op.or: sb.Append("| "); break;
                        case script_op.not: sb.Append("! "); break;
                        case script_op.xor: sb.Append("^ "); break;
                        case script_op.smaller: sb.Append("< "); break;
                        case script_op.greater: sb.Append("> "); break;
                        case script_op.smaller_equals: sb.Append("<= "); break;
                        case script_op.greater_equals: sb.Append(">= "); break;
                        case script_op.equals: sb.Append("= "); break;
                        case script_op.not_equals: sb.Append("!= "); break;
                        case script_op.fma: sb.Append("fma "); break;
                        case script_op.fsm: sb.Append("fsm "); break;
                        case script_op.fms: sb.Append("fms "); break;
                        case script_op.adda: sb.Append("adda "); break;
                        case script_op.mula: sb.Append("mula "); break;
                        case script_op.diva: sb.Append("diva "); break;
                        case script_op.suba: sb.Append("suba "); break;
                        case script_op.all: sb.Append("all "); break;
                        case script_op.any: sb.Append("any "); break;
                        case script_op.max: sb.Append("max "); break;
                        case script_op.min: sb.Append("min "); break;
                        case script_op.peek: sb.Append("peek "); break;
                        case script_op.peekv: sb.Append("peek "); break;
                        case script_op.push: sb.Append("push "); break;
                        case script_op.pushf: sb.Append("push function "); break;
                        case script_op.pushv: sb.Append("push vector "); break;
                        case script_op.pop: sb.Append("pop "); break;
                        case script_op.pop2: sb.Append("pop2 "); break;
                        case script_op.pop3: sb.Append("pop3 "); break;
                        case script_op.pop4: sb.Append("pop4 "); break;
                        case script_op.popv: sb.Append("popv "); break;

                        case script_op.abs: sb.Append("abs "); break;

                        case script_op.random: sb.Append("random "); break;
                        case script_op.seed: sb.Append("seed "); break;

                        case script_op.yield: sb.Append("yield "); break;
                        case script_op.jz: sb.Append("jz "); break;
                        case script_op.jnz: sb.Append("jnz "); break;
                        case script_op.jump: sb.Append("jump "); break;
                        case script_op.jump_back: sb.Append("jumpback "); break;

                        case script_op.lerp: sb.Append("lerp "); break;
                        case script_op.slerp: sb.Append("slerp "); break;

                        case script_op.saturate: sb.Append("saturate "); break;
                        case script_op.clamp: sb.Append("clamp "); break;

                        case script_op.sqrt: sb.Append("sqrt "); break;
                        case script_op.rsqrt: sb.Append("rsqrt "); break;

                        case script_op.ceil: sb.Append("ceil "); break;
                        case script_op.floor: sb.Append("floor "); break;
                        case script_op.frac: sb.Append("frac "); break;

                        case script_op.sin: sb.Append("sin "); break;
                        case script_op.cos: sb.Append("cos "); break;
                        case script_op.tan: sb.Append("tan "); break;
                        case script_op.atan: sb.Append("atan "); break;
                        case script_op.cosh: sb.Append("cosh "); break;
                        case script_op.sinh: sb.Append("sinh "); break;

                        case script_op.degrees: sb.Append("degrees "); break;
                        case script_op.radians: sb.Append("radians "); break;

                        case script_op.distance: sb.Append("distance "); break;
                        case script_op.distancesq: sb.Append("distancesq "); break;
                        case script_op.length: sb.Append("lengthsq "); break;
                        case script_op.lengthsq: sb.Append("lengthsq "); break;

                        case script_op.dot: sb.Append("dot "); break;

                        case script_op.log2: sb.Append("log2 "); break;
                        case script_op.exp: sb.Append("exponent "); break;

                        case script_op.ret: sb.Append("return "); break;

                        case script_op.value_0: sb.Append("0 "); break;
                        case script_op.value_1: sb.Append("1 "); break;
                        case script_op.value_2: sb.Append("2 "); break;
                        case script_op.value_3: sb.Append("3 "); break;
                        case script_op.value_4: sb.Append("4 "); break;
                        case script_op.value_8: sb.Append("8 "); break;
                        case script_op.value_10: sb.Append("10 "); break;
                        case script_op.value_16: sb.Append("16 "); break;
                        case script_op.value_24: sb.Append("24 "); break;
                        case script_op.value_30: sb.Append("30 "); break;
                        case script_op.value_32: sb.Append("32 "); break;
                        case script_op.value_45: sb.Append("45 "); break;
                        case script_op.value_64: sb.Append("64 "); break;
                        case script_op.value_90: sb.Append("90 "); break;
                        case script_op.value_100: sb.Append("100 "); break;
                        case script_op.value_128: sb.Append("128 "); break;
                        case script_op.value_180: sb.Append("180 "); break;
                        case script_op.value_256: sb.Append("256 "); break;
                        case script_op.value_270: sb.Append("270 "); break;
                        case script_op.value_360: sb.Append("360 "); break;
                        case script_op.value_512: sb.Append("512 "); break;
                        case script_op.value_1024: sb.Append("1024 "); break;

                        case script_op.inv_value_2: sb.Append((1f / 2f).ToString("0.000") + " "); break;
                        case script_op.inv_value_3: sb.Append((1f / 3f).ToString("0.000") + " "); break;
                        case script_op.inv_value_4: sb.Append((1f / 4f).ToString("0.000") + " "); break;
                        case script_op.inv_value_8: sb.Append((1f / 8f).ToString("0.000") + " "); break;
                        case script_op.inv_value_10: sb.Append((1f / 10f).ToString("0.000") + " "); break;
                        case script_op.inv_value_16: sb.Append((1f / 16f).ToString("0.000") + " "); break;
                        case script_op.inv_value_24: sb.Append((1f / 24f).ToString("0.000") + " "); break;
                        case script_op.inv_value_30: sb.Append((1f / 30f).ToString("0.000") + " "); break;
                        case script_op.inv_value_32: sb.Append((1f / 32f).ToString("0.000") + " "); break;
                        case script_op.inv_value_45: sb.Append((1f / 45f).ToString("0.000") + " "); break;
                        case script_op.inv_value_64: sb.Append((1f / 64f).ToString("0.000") + " "); break;
                        case script_op.inv_value_90: sb.Append((1f / 90f).ToString("0.000") + " "); break;
                        case script_op.inv_value_100: sb.Append((1f / 100f).ToString("0.000") + " "); break;
                        case script_op.inv_value_128: sb.Append((1f / 128f).ToString("0.000") + " "); break;
                        case script_op.inv_value_180: sb.Append((1f / 180f).ToString("0.000") + " "); break;
                        case script_op.inv_value_256: sb.Append((1f / 256f).ToString("0.000") + " "); break;
                        case script_op.inv_value_270: sb.Append((1f / 270f).ToString("0.000") + " "); break;
                        case script_op.inv_value_360: sb.Append((1f / 360f).ToString("0.000") + " "); break;
                        case script_op.inv_value_512: sb.Append((1f / 512f).ToString("0.000") + " "); break;
                        case script_op.inv_value_1024: sb.Append((1f / 1024f).ToString("0.000") + " "); break;

                        case script_op.ex_op:
                            i++;
                            extended_script_op exop = (extended_script_op)code[i];
                            switch (exop)
                            {
                                case extended_script_op.exp10: sb.Append("exp10 "); break;
                                case extended_script_op.log10: sb.Append("log10 "); break;
                                case extended_script_op.logn: sb.Append("logn "); break;
                                case extended_script_op.cross: sb.Append("cross "); break;
                                case extended_script_op.debug: sb.Append("debug "); break;
                                case extended_script_op.call:
                                    sb.Append("call ");
                                    // next 4 bytes are the function id 
                                    int id = this.code[i + 1].code << 24;
                                    id += this.code[i + 2].code << 16;
                                    id += this.code[i + 3].code << 8;
                                    id += this.code[i + 4].code;
                                    i += 4;
                                    var def = Blast.GetFunctionById(id);
                                    if (def != null)
                                    {
                                        sb.Append(def.Match + " ");
                                    }
                                    else
                                    {
                                        sb.Append("<unknown_function_id>");
                                    }

                                    break;
                                default:
                                    sb.Append($"\n\noperation {(script_op)op} {exop} not yet translated in source debug\n\n");
                                    break;
                            }
                            break;

                        default:
                            if (op >= BlastCompiler.opt_ident)
                            {
                                int idx = Offsets.IndexOf((byte)(op - BlastCompiler.opt_ident));

                                //
                                //   THIS FAILS WHEN USING VECTORS    
                                //
                                //

                                if (idx >= 0)
                                {
                                    BlastVariable v = Variables[idx];
                                    if (v.IsConstant)
                                    {
                                        if (v.IsVector)
                                        {
                                            string value = string.Empty;
                                            for (int j = 0; j < v.VectorSize; j++)
                                            {
                                                value += $"{Executable.data[op - BlastCompiler.opt_ident].ToString("R")} ";
                                            }
                                            sb.Append($"[{value.Trim()}] ");
                                        }
                                        else
                                        {
                                            sb.Append($"{v.Name} ");
                                        }
                                    }
                                    else
                                    {
                                        sb.Append($"{v.Name} ");
                                    }
                                }
                                else
                                {
                                    sb.Append($"#{(byte)op} ");
                                }
                            }
                            else if (op >= BlastCompiler.opt_value)
                            {
                                sb.Append(BlastValues.GetConstantValue((script_op)op) + " ");
                            }
                            else
                            {
                                sb.Append($"\n\noperation {(script_op)op} not yet translated in source debug\n\n");
                            }
                            break;
                    }

                    prev = (script_op)op;
                }

            }
        }

        public unsafe string GetHumanReadableBytes()
        {
            StringBuilder sb = StringBuilderCache.Acquire();

            if (Executable.code_size > 0)
            {
                for (int i = 0; i < Executable.code_size; i++)
                {
                    if (i % 10 == 0)
                    {
                        sb.Append($"{i.ToString().PadLeft(3, '0')}  ");
                    }
                    sb.Append($"{Executable.code[i].ToString().PadLeft(3, '0')} ");
                    if (i % 10 == 9)
                    {
                        sb.AppendLine();
                    }
                }
            }
            else
            {
                if (code != null && code.list != null && code.list.Count > 0)
                {
                    sb.AppendLine("INTERMEDIATE CODE BYTES: ");
                    for (int i = 0; i < code.list.Count; i++)
                    {
                        if (i % 10 == 0)
                        {
                            sb.Append($"{i.ToString().PadLeft(3, '0')}  ");
                        }
                        sb.Append($"{code.list[i].ToString().PadLeft(3, '0')} ");
                        if (i % 10 == 9)
                        {
                            sb.AppendLine();
                        }
                    }
                }
                else
                {
                    sb.Append("-no code bytes available to convert to string-"); 
                }
            }

            return StringBuilderCache.GetStringAndRelease(ref sb); 
        }

#endregion


        // -- data used internally while compiling ----------------------------------------------------
        public node root;
        public IMByteCodeList code; 

        public node AST => root;
        public List<BlastVariable> Variables { get; set; }
        public List<byte> Offsets { get; set; }
        public List<Tuple<int, int>> Jumps { get; set; }
        public Dictionary<string, string> Defines { get; set; }
        public List<BlastVariableMapping> Inputs { get; set; }
        public List<BlastVariableMapping> Outputs { get; set; }
        public Dictionary<string, string> Validations { get; set; }
        public List<Tuple<BlastScriptToken, string>> Tokens { get; set; }

        // -- getter methods & props ------------------------------------------------------------------

        /// <summary>
        /// create a holder for variable data collected during compilation 
        /// - will log errors if the variable exists and returns null 
        /// - initializes reference count at 1
        /// </summary>
        /// <param name="name">first part of identifier - the name</param>
        /// <param name="is_inputoutput_define">if part of #input of #output we dont want to add references</param>
        /// <returns>null on failure</returns>
        public BlastVariable CreateVariable(string name, bool is_input = false, bool is_output = false)
        {
            name = name.ToLower();

            if (string.IsNullOrWhiteSpace(name))
            {
                return null;
            }

            lock (Variables)
            {
                // on create it may not exist 
                foreach (BlastVariable variable in Variables)
                {
                    if (string.Compare(name, variable.Name, true) == 0)
                    {
                        LogError($"data.CreateVariable: a variable with the identifier '{name}' has already been created");
                        return null;
                    }
                }

                // create a variable with 1 reference, 
                BlastVariable v = new BlastVariable()
                {
                    Id = Variables.Count,

                    // values like 42 will be named 42 this is nice when looking at it in debugger 
                    Name = name,

                    // - inputs dont count as a reference, if we dont use an input we could remove it 
                    // (todo - intentially not using input because of padding for memcpy ????) 
                    // - outputs do refcount 
                    ReferenceCount = is_input ? 0 : 1,

                    DataType = BlastVariableDataType.Numeric,

                    // assume constant value if the first char is a digit or minus sign 
                    IsConstant = char.IsDigit(name[0]) || name[0] == '-',
                    IsInput = is_input,
                    IsOutput = is_output,

                    // initally assume this is not a vector                     
                    IsVector = false,
                    VectorSize = 1
                };

                Variables.Add(v);
                return v;
            }
        }


        /// <summary>
        /// try to lookup a reference 
        /// - doens not reference count 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="variable"></param>
        /// <returns></returns>
        internal bool TryGetVariable(string name, out BlastVariable variable)
        {
            if (!string.IsNullOrWhiteSpace(name))
            {
                lock (Variables)
                {
                    foreach (BlastVariable v in Variables)
                    {
                        if (string.Compare(name, v.Name, true) == 0)
                        {
                            variable = v; 
                            return true; 
                        }
                    }
                }

            }
            variable = null; 
            return false;
        }

        /// <summary>
        /// get or create a holder for variable data during compilation 
        /// - maintains reference count
        /// </summary>
        /// <param name="name">identifier name</param>
        /// <returns>null on failure</returns>
        public BlastVariable GetOrCreateVariable(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return null;
            }

            lock (Variables)
            {
                // if it exists 
                foreach (BlastVariable variable in Variables)
                {
                    if (string.Compare(name, variable.Name, true) == 0)
                    {
                        Interlocked.Increment(ref variable.ReferenceCount);
                        return variable;
                    }
                }

                // it doesnt exist yet, create a new variable with 1 reference, 
                BlastVariable v = new BlastVariable()
                {
                    Id = Variables.Count,
                    Name = name,
                    
                    ReferenceCount = 1,

                    DataType = BlastVariableDataType.Numeric,

                    // assume constant value if the first char is a digit or minus sign 
                    IsConstant = char.IsDigit(name[0]) || name[0] == '-',
                    IsInput = false,
                    IsOutput = false,

                    // initally assume this is not a vector                     
                    IsVector = false,
                    VectorSize = 1
                };

                Variables.Add(v);
                return v;
            }
        }


        /// <summary>
        /// try to lookup define by identifier name
        /// </summary>
        /// <param name="identifier"></param>
        /// <param name="defined_value"></param>
        /// <returns></returns>
        public bool TryGetDefine(string identifier, out string defined_value)
        {
            if (HasDefines)
            {
                foreach (var k in Defines)
                {
                    if(string.Compare(k.Key, identifier, true) == 0)
                    {
                        defined_value = k.Value; 
                        return true; 
                    }
                }
            }
            defined_value = null; 
            return false;
        }

        public bool TryGetInput(BlastVariable v, out BlastVariableMapping mapping)
        {
            foreach (var i in Inputs)
            {
                if (i.id == v.Id)
                {
                    mapping = i; 
                    return true;
                }
            }
            mapping = null;
            return false; 
        }

        public bool TryGetOutput(BlastVariable v, out BlastVariableMapping mapping)
        {
            foreach (var i in Outputs)
            {
                if (i.id == v.Id)
                {
                    mapping = i;
                    return true;
                }
            }
            mapping = null;
            return false;
        }



        #region exposed via interface, non reference counting 
        public bool ExistsVariable(string name)
        {
            if (string.IsNullOrWhiteSpace(name) || Variables == null || Variables.Count == 0) return false;

            lock (Variables)
            {
                foreach (BlastVariable v in Variables)
                {
                    if (string.Compare(name, v.Name, true) == 0) return true;
                }
            }

            return false;
        }
        public BlastVariable GetVariable(string name)
        {
            if (string.IsNullOrWhiteSpace(name) || Variables == null || Variables.Count == 0) return null;

            lock (Variables)
            {
                foreach (BlastVariable v in Variables)
                {
                    if (string.Compare(name, v.Name, true) == 0) return v;
                }
            }

            return null;
        }
        public BlastVariable GetVariableFromOffset(byte offset)
        {
            int index = Offsets.IndexOf(offset);

            // assume it only grows and never shrinks while multithreading 
            if (index < 0 || Variables == null || index >= Variables.Count) return null;

            lock (Variables)
            {
                return Variables[index];
            }
        }


        public bool CanValidate => Validations != null && Validations.Count > 0;
        public bool HasDefines => Defines != null && Defines.Count > 0;
        public bool HasVariables => Variables != null && Variables.Count > 0;
        public bool HasOffsets => Offsets != null && Offsets.Count > 0;
        public int VariableCount => Variables != null ? Variables.Count : 0;
        public int OffsetCount => Offsets != null ? Offsets.Count : 0;

        public bool HasInput(int id)
        {
            foreach (var v in Inputs)
            {
                if (v.id == id)
                {
                    return true;
                }
            }
            return false;
        }
        public bool HasInput(string name)
        {
            foreach (var v in Inputs)
            {
                if (v.variable != null)
                {
                    if (string.Compare(name, v.variable.Name, true) == 0)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public unsafe int CalculateVariableOffsets()
        {
            // clear out any existing offsets (might have ran multiple times..) 
            Offsets.Clear();

            byte offset = 0;
            for (int i = 0; i < Variables.Count; i++)
            {
                BlastVariable v = Variables[i];
                                  
                if (v.IsVector)
                {
                    // variable sized float 
                    Offsets.Add(offset);
                    offset = (byte)(offset + v.VectorSize);
                }
                else
                {
                    Offsets.Add(offset);
                    offset++;
                }
            }

            return offset;
        }
        #endregion
    }


}