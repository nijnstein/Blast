using NSS.Blast.Compiler.Stage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;

#if NOT_USING_UNITY
    using NSS.Blast.Standalone; 
#else
    using UnityEngine;
#endif 


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
                Debug.Log(msg);
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
                Debug.Log(msg);
            }
        }

        public void LogError(string msg, int code = 0, [CallerLineNumber] int linenr = 0, [CallerMemberName] string member = "")
        {
            LastErrorMessage = msg;
            CompilerMessages.Add(new Message() { Type = Message.MessageType.Error, Content = msg, Code = 0, LineNumber = linenr, CallerMember = member });
            Debug.LogError(msg);
        }

        public void LogWarning(string msg, [CallerLineNumber] int linenr = 0, [CallerMemberName] string member = "")
        {
            CompilerMessages.Add(new Message() { Type = Message.MessageType.Warning, Content = msg, LineNumber = linenr, CallerMember = member });
            Debug.LogWarning(msg);
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
                Debug.LogWarning("TODO: " + msg);
            }
        }

        public string LastErrorMessage { get; private set; }
        public BlastError LastError { get; internal set; }

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

            // enable experimental stuff 
            use_new_stuff = options.Experimental; 
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
        /// this version has a little more information than the generic bytecode reader in blast due to having access to all compilation data
        /// </summary>
        /// <returns></returns>
        public unsafe string GetHumanReadableCode()
        {
            StringBuilder sb = StringBuilderCache.Acquire();

            if (Executable.code_size > 0)
            {
                List<byte> bytes = new List<byte>(Executable.code_size);
                for (int i = 0; i < Executable.code_size; i++) bytes.Add(Executable.code[i]);
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
            blast_operation prev = blast_operation.nop;

            for (; i < code.Count; i++)
            {
                byte op = code[i];

                if (i % 10 == 0)
                {
                    if (i != 0) sb.Append("\n");
                    sb.Append("" + i.ToString().PadLeft(3, '0') + "| ");
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

                if (prev == blast_operation.jump ||
                    prev == blast_operation.jump_back ||
                    prev == blast_operation.jz ||
                    prev == blast_operation.jnz)
                {
                    sb.Append(((byte)op).ToString() + " ");

                    prev = blast_operation.nop;
                }
                else
                {
                    switch ((blast_operation)op)
                    {
                        case blast_operation.nop: sb.Append("nop "); break;
                        case blast_operation.assign: sb.Append("set "); break;
                        case blast_operation.assigns: sb.Append("sets "); break;
                        case blast_operation.add: sb.Append("+ "); break;
                        case blast_operation.substract: sb.Append("- "); break;
                        case blast_operation.multiply: sb.Append("* "); break;
                        case blast_operation.divide: sb.Append("/ "); break;
                        case blast_operation.begin: sb.Append("( "); break;
                        case blast_operation.end: sb.Append(") "); break;
                        case blast_operation.and: sb.Append("& "); break;
                        case blast_operation.or: sb.Append("| "); break;
                        case blast_operation.not: sb.Append("! "); break;
                        case blast_operation.xor: sb.Append("^ "); break;
                        case blast_operation.smaller: sb.Append("< "); break;
                        case blast_operation.greater: sb.Append("> "); break;
                        case blast_operation.smaller_equals: sb.Append("<= "); break;
                        case blast_operation.greater_equals: sb.Append(">= "); break;
                        case blast_operation.equals: sb.Append("= "); break;
                        case blast_operation.not_equals: sb.Append("!= "); break;
                        case blast_operation.fma: sb.Append("fma "); break;
                        case blast_operation.adda: sb.Append("adda "); break;
                        case blast_operation.mula: sb.Append("mula "); break;
                        case blast_operation.diva: sb.Append("diva "); break;
                        case blast_operation.suba: sb.Append("suba "); break;
                        case blast_operation.all: sb.Append("all "); break;
                        case blast_operation.any: sb.Append("any "); break;
                        case blast_operation.max: sb.Append("max "); break;
                        case blast_operation.min: sb.Append("min "); break;
                        case blast_operation.peek: sb.Append("peek "); break;
                        case blast_operation.peekv: sb.Append("peek "); break;
                        case blast_operation.push: sb.Append("push "); break;
                        case blast_operation.pushf: sb.Append("push function "); break;
                        case blast_operation.pushc: sb.Append("push compound "); break;
                        case blast_operation.pushv: sb.Append("push vector "); break;
                        case blast_operation.pop: sb.Append("pop "); break;

                        case blast_operation.abs: sb.Append("abs "); break;

                        case blast_operation.random: sb.Append("random "); break;
                        case blast_operation.seed: sb.Append("seed "); break;

                        case blast_operation.yield: sb.Append("yield "); break;
                        case blast_operation.jz: sb.Append("jz "); break;
                        case blast_operation.jnz: sb.Append("jnz "); break;
                        case blast_operation.jump: sb.Append("jump "); break;
                        case blast_operation.jump_back: sb.Append("jumpback "); break;

                        case blast_operation.lerp: sb.Append("lerp "); break;
                        case blast_operation.slerp: sb.Append("slerp "); break;

                        case blast_operation.saturate: sb.Append("saturate "); break;
                        case blast_operation.clamp: sb.Append("clamp "); break;

                        case blast_operation.ceil: sb.Append("ceil "); break;
                        case blast_operation.floor: sb.Append("floor "); break;
                        case blast_operation.frac: sb.Append("frac "); break;

                        case blast_operation.sin: sb.Append("sin "); break;
                        case blast_operation.cos: sb.Append("cos "); break;
                        case blast_operation.tan: sb.Append("tan "); break;
                        case blast_operation.atan: sb.Append("atan "); break;
                        case blast_operation.cosh: sb.Append("cosh "); break;
                        case blast_operation.sinh: sb.Append("sinh "); break;

                        case blast_operation.degrees: sb.Append("degrees "); break;
                        case blast_operation.radians: sb.Append("radians "); break;

                        case blast_operation.ret: sb.Append("return "); break;
                        case blast_operation.sqrt: sb.Append("sqrt "); break;

                        case blast_operation.value_0: sb.Append("0 "); break;
                        case blast_operation.value_1: sb.Append("1 "); break;
                        case blast_operation.value_2: sb.Append("2 "); break;
                        case blast_operation.value_3: sb.Append("3 "); break;
                        case blast_operation.value_4: sb.Append("4 "); break;
                        case blast_operation.value_8: sb.Append("8 "); break;
                        case blast_operation.value_10: sb.Append("10 "); break;
                        case blast_operation.value_16: sb.Append("16 "); break;
                        case blast_operation.value_24: sb.Append("24 "); break;
                        case blast_operation.value_30: sb.Append("30 "); break;
                        case blast_operation.value_32: sb.Append("32 "); break;
                        case blast_operation.value_45: sb.Append("45 "); break;
                        case blast_operation.value_64: sb.Append("64 "); break;
                        case blast_operation.value_90: sb.Append("90 "); break;
                        case blast_operation.value_100: sb.Append("100 "); break;
                        case blast_operation.value_128: sb.Append("128 "); break;
                        case blast_operation.value_180: sb.Append("180 "); break;
                        case blast_operation.value_256: sb.Append("256 "); break;
                        case blast_operation.value_270: sb.Append("270 "); break;
                        case blast_operation.value_360: sb.Append("360 "); break;
                        case blast_operation.value_512: sb.Append("512 "); break;
                        case blast_operation.value_1024: sb.Append("1024 "); break;

                        case blast_operation.inv_value_2: sb.Append((1f / 2f).ToString("0.000") + " "); break;
                        case blast_operation.inv_value_3: sb.Append((1f / 3f).ToString("0.000") + " "); break;
                        case blast_operation.inv_value_4: sb.Append((1f / 4f).ToString("0.000") + " "); break;
                        case blast_operation.inv_value_8: sb.Append((1f / 8f).ToString("0.000") + " "); break;
                        case blast_operation.inv_value_10: sb.Append((1f / 10f).ToString("0.000") + " "); break;
                        case blast_operation.inv_value_16: sb.Append((1f / 16f).ToString("0.000") + " "); break;
                        case blast_operation.inv_value_24: sb.Append((1f / 24f).ToString("0.000") + " "); break;
                        case blast_operation.inv_value_30: sb.Append((1f / 30f).ToString("0.000") + " "); break;
                        case blast_operation.inv_value_32: sb.Append((1f / 32f).ToString("0.000") + " "); break;
                        case blast_operation.inv_value_45: sb.Append((1f / 45f).ToString("0.000") + " "); break;
                        case blast_operation.inv_value_64: sb.Append((1f / 64f).ToString("0.000") + " "); break;
                        case blast_operation.inv_value_90: sb.Append((1f / 90f).ToString("0.000") + " "); break;
                        case blast_operation.inv_value_100: sb.Append((1f / 100f).ToString("0.000") + " "); break;
                        case blast_operation.inv_value_128: sb.Append((1f / 128f).ToString("0.000") + " "); break;
                        case blast_operation.inv_value_180: sb.Append((1f / 180f).ToString("0.000") + " "); break;
                        case blast_operation.inv_value_256: sb.Append((1f / 256f).ToString("0.000") + " "); break;
                        case blast_operation.inv_value_270: sb.Append((1f / 270f).ToString("0.000") + " "); break;
                        case blast_operation.inv_value_360: sb.Append((1f / 360f).ToString("0.000") + " "); break;
                        case blast_operation.inv_value_512: sb.Append((1f / 512f).ToString("0.000") + " "); break;
                        case blast_operation.inv_value_1024: sb.Append((1f / 1024f).ToString("0.000") + " "); break;

                        case blast_operation.ex_op:
                            i++;
                            extended_blast_operation exop = (extended_blast_operation)code[i];
                            switch (exop)
                            {
                                case extended_blast_operation.log2: sb.Append("log2 "); break;
                                case extended_blast_operation.exp: sb.Append("exponent "); break;

                                case extended_blast_operation.exp10: sb.Append("exp10 "); break;
                                case extended_blast_operation.log10: sb.Append("log10 "); break;
                                case extended_blast_operation.logn: sb.Append("logn "); break;
                                case extended_blast_operation.cross: sb.Append("cross "); break;
                                case extended_blast_operation.dot: sb.Append("dot "); break;
                                case extended_blast_operation.debug: sb.Append("debug "); break;
                                case extended_blast_operation.debugstack: sb.Append("debugstack "); break;
                                case extended_blast_operation.rsqrt: sb.Append("rsqrt "); break;
                                case extended_blast_operation.pow: sb.Append("pow "); break;

                                case extended_blast_operation.call:
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
                                    // just append byte value instead of raising big error making it unreadable mess
                                    sb.Append(op.ToString().PadLeft(3, ' ') + " ");

                                    // sb.Append($"\n\noperation {(blast_operation)op} {exop} not yet translated in source debug\n\n");
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
                                sb.Append(Blast.GetConstantValue((blast_operation)op) + " ");
                            }
                            else
                            {
                                // just append byte value instead of raising big error making it unreadable mess
                                sb.Append(op.ToString().PadLeft(3, ' ') + " ");

                                // sb.Append($"\n\noperation {(blast_operation)op} not yet translated in source debug\n\n");
                            }
                            break;
                    }

                    prev = (blast_operation)op;
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
                        sb.Append($"{i.ToString().PadLeft(3, '0')}| ");
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
                            sb.Append($"{i.ToString().PadLeft(3, '0')}| ");
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


        public node root;
        public IMByteCodeList code;

        public bool use_new_stuff = false;

        public node AST => root;

        public List<BlastVariable> Variables { get; set; }
        public IEnumerable<BlastVariable> ConstantVariables
        {
            get
            {
                foreach (BlastVariable v in Variables) if (v.IsConstant) yield return v;
                yield break;
            }
        }
        public List<byte> Offsets { get; set; }
        public List<Tuple<int, int>> Jumps { get; set; }
        public Dictionary<string, string> Defines { get; set; }
        public List<BlastVariableMapping> Inputs { get; set; }
        public List<BlastVariableMapping> Outputs { get; set; }
        public Dictionary<string, string> Validations { get; set; }
        public List<Tuple<BlastScriptToken, string>> Tokens { get; set; }

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
                if (i.VariableId == v.Id)
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
                if (i.VariableId == v.Id)
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
                if (v.VariableId == id)
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
                if (v.Variable != null)
                {
                    if (string.Compare(name, v.Variable.Name, true) == 0)
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