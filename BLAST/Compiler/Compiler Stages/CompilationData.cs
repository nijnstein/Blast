using NSS.Blast.Compiler.Stage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;

#if STANDALONE_VSBUILD
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
        /// <summary>
        /// compiler version 
        /// </summary>
        Version Version { get; }

        /// <summary>
        /// blast engine data used in this compilation
        /// </summary>
        BlastEngineDataPtr Blast { get; }

        /// <summary>
        /// the script 
        /// </summary>
        BlastScript Script { get; }

        /// <summary>
        /// Compiler Options used during compilation
        /// </summary>
        BlastCompilerOptions CompilerOptions { get; }

        /// <summary>
        /// Compiler node tree
        /// </summary>
        node AST { get; }

        /// <summary>
        /// List of tokens as parsed from the input
        /// </summary>
        List<Tuple<BlastScriptToken, string>> Tokens { get; set; }

        /// <summary>
        /// List of variables as found in input
        /// </summary>
        List<BlastVariable> Variables { get; set; }
        
        /// <summary>
        /// Offsets into the datasegment for variable indices 
        /// </summary>
        List<byte> Offsets { get; set; }

        /// <summary>
        /// Jumps found in the script 
        /// </summary>
        List<Tuple<int, int>> Jumps { get; set; }

        /// <summary>
        /// Compiler defines used during compilation, this contains only the unique defines set by this script
        /// and more defines might apply depending on the setup
        /// </summary>
        Dictionary<string, string> Defines { get; set; }

        /// <summary>
        /// Defined Inputs 
        /// </summary>
        List<BlastVariableMapping> Inputs { get; set; }

        /// <summary>
        /// Defined Outputs 
        /// </summary>
        List<BlastVariableMapping> Outputs { get; set; }
        
        /// <summary>
        /// Validations defined in script 
        /// </summary>
        Dictionary<string, string> Validations { get; set; }

        /// <summary>
        /// log an error to the compiler log
        /// </summary>
        /// <param name="msg">the message to log</param>
        /// <param name="code">an error code</param>
        /// <param name="linenr">possibly the linenr</param>
        /// <param name="member">possibly the callername</param>
        void LogError(string msg, int code = 0, int linenr = 0, [CallerMemberName] string member = "");

        /// <summary>
        /// trace information, does nothing in release 
        /// </summary>
        /// <param name="msg">the message to log</param>
        /// <param name="linenr">possibly the linenr</param>
        /// <param name="member">possibly the callername</param>
        void LogTrace(string msg, int linenr = 0, [CallerMemberName] string member = "");

        /// <summary>
        /// log a warning to the compiler log
        /// </summary>
        /// <param name="msg">the message to log</param>
        /// <param name="linenr">possibly the linenr</param>
        /// <param name="member">possibly the callername</param>
        void LogWarning(string msg, int linenr = 0, [CallerMemberName] string member = "");
 
        /// <summary>
        /// log a message to the compiler log
        /// </summary>
        /// <param name="msg">the message to log</param>
        /// <param name="linenr">possibly the linenr</param>
        /// <param name="member">possibly the callername</param>
        void LogToDo(string msg, int linenr = 0, [CallerMemberName] string member = "");

        /// <summary>
        /// true if no errors are set in the compilation log and no errorcode is set
        /// </summary>
        bool IsOK { get; }

        /// <summary>
        /// true if an errorcode is set or errors are present in the compilation log 
        /// </summary>
        bool HasErrors { get; }

        /// <summary>
        /// true if the script can be validated (it contains validation defines) 
        /// </summary>
        bool CanValidate { get; }

        /// <summary>
        /// true if the script defines compiler defines 
        /// </summary>
        bool HasDefines { get; }

        /// <summary>
        /// lookup a variablemapping defined by the script by its name
        /// </summary>
        /// <param name="name">the name of the variable as used in the script code</param>
        /// <returns>the variable if found, null otherwise</returns>
        BlastVariable GetVariable(string name);

        /// <summary>
        /// lookup a variable defined by script based on its offset 
        /// </summary>
        /// <param name="offset">the datasegment offset</param>
        /// <returns>the variable</returns>
        BlastVariable GetVariableFromOffset(byte offset);
        
        /// <summary>
        /// attempt to get a defined value from script defined compilerdefines
        /// </summary>
        /// <param name="identifier">the identifier</param>
        /// <param name="defined_value">the output value</param>
        /// <returns>true if a define was found with the given name</returns>
        bool TryGetDefine(string identifier, out string defined_value);


        /// <summary>
        /// returns true if a variable exists with the given name
        /// </summary>
        /// <param name="name">variable name</param>
        /// <returns>true if the variable exists</returns>
        bool ExistsVariable(string name);

        /// <summary>
        /// check if there is an input defined by the script with the given id 
        /// </summary>
        /// <param name="id">the integer identifier</param>
        /// <returns>true if defined</returns>
        bool HasInput(int id);

        /// <summary>
        /// check if there is an input defined by the script with the given id 
        /// </summary>
        /// <param name="name">the input variable name</param>
        /// <returns>true if defined</returns>
        bool HasInput(string name);

        /// <summary>
        /// true if the script defines variables 
        /// </summary>
        bool HasVariables { get; }
        
        /// <summary>
        /// true if the script defines variables 
        /// </summary>
        bool HasOffsets { get; }

        /// <summary>
        /// number of variables defined in the script 
        /// </summary>
        int VariableCount { get; }

        /// <summary>
        /// number of variable offsets defined in the script
        /// </summary>
        int OffsetCount { get; }

    }

    /// <summary>
    /// Data created during compilation and used for analysis, packaging and debugging/display purposes
    /// </summary>
    public class CompilationData : IBlastCompilationData
    {
        /// <summary>
        /// Version
        /// </summary>
        protected Version version = new Version(0, 1, 0);

        /// <summary>
        /// Version
        /// </summary>
        public Version Version => version;

        #region Compiler Messages 
        #pragma warning disable CS1591 // Missing XML comment for publicly visible type or member 'CompilationData.WriteHumanReadableCode(StringBuilder, List<byte>, int, bool)'

        /// <summary>
        /// a compiler message 
        /// </summary>
        public class Message
        {
            public enum MessageType 
            { 
                Normal, 
                Warning, 
                Error, 
                ToDo,
                Trace 
            };

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
        #pragma warning restore CS1591 // Missing XML comment for publicly visible type or member 'CompilationData.WriteHumanReadableCode(StringBuilder, List<byte>, int, bool)'

        /// <summary>
        /// List of messages issued during compilation 
        /// </summary>
        public List<Message> CompilerMessages = new List<Message>();

        /// <summary>
        /// on older .net versions we could get the stackframe 
        /// see: https://stackoverflow.com/questions/12556767/how-do-i-get-the-current-line-number
        /// and: https://stackoverflow.com/questions/38476796/how-to-set-net-core-in-if-statement-for-compilation
        /// </summary>
        public void LogMessage(string msg, [CallerLineNumber] int linenr = 0, [CallerMemberName] string member = "")
        {
            if (CompilerOptions.TraceLogging || CompilerOptions.TraceLogging) CompilerMessages.Add(new Message() { Content = msg, LineNumber = linenr, CallerMember = member });
            if (CompilerOptions.VerboseLogging)
            {
#if !STANDALONE_VSBUILD
                Debug.Log(msg);
#else
                System.Diagnostics.Debug.WriteLine(msg);
#endif
            }
        }

        /// <summary>
        /// Trace a message, does nothing in release builds 
        /// </summary>
        /// <param name="msg">the message to trace</param>
        /// <param name="linenr">line number</param>
        /// <param name="member">caller member name</param>
        public void LogTrace(string msg, [CallerLineNumber] int linenr = 0, [CallerMemberName] string member = "")
        {
#if DEVELOPMENT_BUILD && TRACE
            if (CompilerOptions.VerboseLogging || CompilerOptions.TraceLogging) CompilerMessages.Add(new Message() { Content = msg, LineNumber = linenr, CallerMember = member });
            if (CompilerOptions.VerboseLogging && CompilerOptions.TraceLogging)
            {
                Debug.Log(msg);
            }
#endif
        }

        /// <summary>
        /// Log an error to the log, also writes to player log / debugstream
        /// </summary>
        /// <param name="msg">the message</param>
        /// <param name="code">optional errorcode</param>
        /// <param name="linenr">optional linenr</param>
        /// <param name="member">optional caller member name</param>
        public void LogError(string msg, int code = 0, [CallerLineNumber] int linenr = 0, [CallerMemberName] string member = "")
        {
            LastErrorMessage = msg;
            CompilerMessages.Add(new Message() { Type = Message.MessageType.Error, Content = msg, Code = 0, LineNumber = linenr, CallerMember = member });
            Debug.LogError(msg);
        }

        /// <summary>
        /// Log a warning to the log, also writes to player log / debugstream
        /// </summary>
        /// <param name="msg">the message</param>
        /// <param name="linenr">optional linenr</param>
        /// <param name="member">optional caller member name</param>
        public void LogWarning(string msg, [CallerLineNumber] int linenr = 0, [CallerMemberName] string member = "")
        {
            CompilerMessages.Add(new Message() { Type = Message.MessageType.Warning, Content = msg, LineNumber = linenr, CallerMember = member });
            Debug.LogWarning(msg);
        }

#if !STANDALONE_VSBUILD
        public string FormatWithColor(string msg, Color rgb)
        {
            return string.Format(
                "<color=#{0:X2}{1:X2}{2:X2}>{3}</color>", 
                (byte)(rgb.r * 255f), (byte)(rgb.g * 255f), (byte)(rgb.b * 255f), msg);
        }
#endif

        /// <summary>
        /// logs a todo, only in standalone debug builds 
        /// </summary>
        /// <param name="msg">the message</param>
        /// <param name="linenr">optional line number</param>
        /// <param name="member">optional caller member name</param>
        public void LogToDo(string msg, [CallerLineNumber] int linenr = 0, [CallerMemberName] string member = "")
        {
#if !STANDALONE_VSBUILD && DEVELOPMENT_BUILD 
            if (CompilerOptions.Trace || CompilerOptions.Trace) CompilerMessages.Add(new Message() { Type = Message.MessageType.ToDo, Content = msg, LineNumber = linenr, CallerMember = member });
            if (CompilerOptions.Verbose || CompilerOptions.Trace)
            {
                Debug.LogWarning("TODO: " + msg);
            }
#endif
        }

        /// <summary>
        /// keep reference of any last error message, voiding the need to search for it 
        /// </summary>
        public string LastErrorMessage { get; private set; }
        /// <summary>
        /// returns the last error code or success if nothing went wrong 
        /// </summary>
        public BlastError LastError { get; internal set; }
        /// <summary>
        /// number of errors that occured during compilation 
        /// </summary>
        public int ErrorCount => CompilerMessages.Count(x => x.IsError);
        /// <summary>
        /// number of errors that occured during compilation
        /// </summary>
        public bool HasErrors => LastError != BlastError.success || !string.IsNullOrEmpty(LastErrorMessage); 
        /// <summary>
        /// number of warnings that occured during compilation 
        /// </summary>
        public int WarningCount => CompilerMessages.Count(x => x.IsWarning);
        /// <summary>
        /// true if any warning was logged during compilation
        /// </summary>
        public bool HasWarnings => CompilerMessages.Any(x => x.IsWarning);
        /// <summary>
        /// true if any erorr or warning occured 
        /// </summary>
        public bool HasErrorsOrWarnings => HasErrors || HasWarnings;

        /// <summary>
        /// true if everything went ok 
        /// </summary>
        public bool IsOK { get { return !HasErrors; } }


#endregion


        /// <summary>
        /// setup new compilation data
        /// </summary>
        /// <param name="blast">blast engine data</param>
        /// <param name="script">the script to compile</param>
        /// <param name="options">compiler options</param>
        public CompilationData(BlastEngineDataPtr blast, BlastScript script, BlastCompilerOptions options)
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
        /// blast engine data 
        /// </summary>
        public BlastEngineDataPtr Blast { get; set; }

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
        /// <returns>a readable string</returns>
        public unsafe string GetHumanReadableCode(int columns = 10, bool index = false)
        {
            StringBuilder sb = StringBuilderCache.Acquire();

            if (Executable.code_size > 0)
            {
                List<byte> bytes = new List<byte>(Executable.code_size);
                for (int i = 0; i < Executable.code_size; i++) bytes.Add(Executable.code[i]);
                WriteHumanReadableCode(sb, bytes, columns, index);

                // ending a jump after program end?
                foreach (var jump in Jumps.Where(x => x.Item1 + x.Item2 >= bytes.Count))
                {
                    if (jump != null)
                    {
                        sb.Append($"@label_{Jumps.IndexOf(jump)} <<< E: OUT OF BOUNDS - {jump.Item1} {jump.Item2} >>> ");
                    }
                }
            }
            else
            {
                if (code != null && code.list != null && code.list.Count > 0)
                {
                    List<byte> bytes = new List<byte>(code.list.Count);
                    for (int i = 0; i < code.list.Count; i++) bytes.Add(code.list[i].code); 
                    WriteHumanReadableCode(sb, bytes, columns, index);
                }
                else
                {
                    sb.Append("-no code available to convert to string-");
                }
            }
            return StringBuilderCache.GetStringAndRelease(ref sb);
        }

        unsafe void WriteHumanReadableCode(StringBuilder sb, List<byte> code, int columns = 10, bool index = true)
        {
            int i = 0;
            blast_operation prev = blast_operation.nop;

            for (; i < code.Count; i++)
            {
                byte op = code[i];

                if (index && i % columns == 0)
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
                        case blast_operation.assignf: sb.Append("setf "); break;
                        case blast_operation.assignfe: sb.Append("setfe "); break;
                        case blast_operation.assignfn: sb.Append("setf "); break;
                        case blast_operation.assignfen: sb.Append("setfen "); break;
                        case blast_operation.assignv: sb.Append("setv "); break;
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

                        case blast_operation.ret: sb.Append("return "); break;

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
                                case extended_blast_operation.sqrt: sb.Append("sqrt "); break;
                                case extended_blast_operation.rsqrt: sb.Append("rsqrt "); break;
                                case extended_blast_operation.pow: sb.Append("pow "); break;
                                case extended_blast_operation.sin: sb.Append("sin "); break;
                                case extended_blast_operation.cos: sb.Append("cos "); break;
                                case extended_blast_operation.tan: sb.Append("tan "); break;
                                case extended_blast_operation.atan: sb.Append("atan "); break;
                                case extended_blast_operation.cosh: sb.Append("cosh "); break;
                                case extended_blast_operation.sinh: sb.Append("sinh "); break;

                                case extended_blast_operation.degrees: sb.Append("degrees "); break;
                                case extended_blast_operation.radians: sb.Append("radians "); break;

                                case extended_blast_operation.lerp: sb.Append("lerp "); break;
                                case extended_blast_operation.slerp: sb.Append("slerp "); break;
                                case extended_blast_operation.saturate: sb.Append("saturate "); break;
                                case extended_blast_operation.clamp: sb.Append("clamp "); break;
                                case extended_blast_operation.ceil: sb.Append("ceil "); break;
                                case extended_blast_operation.floor: sb.Append("floor "); break;
                                case extended_blast_operation.frac: sb.Append("frac "); break;


                                case extended_blast_operation.call:
                                    sb.Append("call ");

                                    // next 4 bytes are the function id 
                                    int id = this.code[i + 1].code << 24;
                                    id += this.code[i + 2].code << 16;
                                    id += this.code[i + 3].code << 8;
                                    id += this.code[i + 4].code;
                                    i += 4;

                                    if (id > 0)
                                    {
                                        sb.Append(Blast.Data->Functions[id].GetFunctionName() + " ");
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
                                sb.Append(NSS.Blast.Blast.GetConstantValue((blast_operation)op) + " ");
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

        /// <summary>
        /// get a readable string from the compiled code
        /// </summary>
        /// <param name="columns">number of columns to use in the presentation of the bytes</param>
        /// <param name="index">true if you want an index (000| ) at the start of each line</param>
        /// <returns></returns>
        public unsafe string GetHumanReadableBytes(int columns = 10, bool index = true)
        {
            StringBuilder sb = StringBuilderCache.Acquire();

            if (Executable.code_size > 0)
            {
                for (int i = 0; i < Executable.code_size; i++)
                {
                    if (index && i % columns == 0)
                    {
                        sb.Append($"{i.ToString().PadLeft(3, '0')}| ");
                    }
                    sb.Append($"{Executable.code[i].ToString().PadLeft(3, '0')} ");
                    if (i % columns == columns - 1)
                    {
                        sb.AppendLine();
                    }
                }
            }
            else
            {
                if (code != null && code.list != null && code.list.Count > 0)
                {
                    sb.AppendLine("IM: ");
                    for (int i = 0; i < code.list.Count; i++)
                    {
                        if (index && i % columns == 0)
                        {
                            sb.Append($"{i.ToString().PadLeft(3, '0')}| ");
                        }
                        sb.Append($"{code.list[i].ToString().PadLeft(3, '0')} ");
                        if (i % columns == columns - 1)
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


        internal node root;
        internal bool use_new_stuff = false;

        /// <summary>
        /// intermediate bytecode, only public for debugging view purposes, dont use, dont modify
        /// </summary>
        public IMByteCodeList code;


        
        /// <summary>
        /// the rootnode of the abstract syntax tree
        /// </summary>
        public node AST => root;

        /// <summary>
        /// List of variables in script 
        /// </summary>
        public List<BlastVariable> Variables { get; set; }
                        
        /// <summary>
        /// List of constant variables (constant data needs to be somewhere)
        /// </summary>
        public IEnumerable<BlastVariable> ConstantVariables
        {
            get
            {
                foreach (BlastVariable v in Variables) if (v.IsConstant) yield return v;
                yield break;
            }
        }

        /// <summary>
        /// list of used variable offsets 
        /// </summary>
        public List<byte> Offsets { get; set; }

        /// <summary>
        /// list of jumps 
        /// </summary>
        public List<Tuple<int, int>> Jumps { get; set; }

        /// <summary>
        /// list of defines defined by this script 
        /// </summary>
        public Dictionary<string, string> Defines { get; set; }

        /// <summary>
        /// list of inputs defined by this script
        /// </summary>
        public List<BlastVariableMapping> Inputs { get; set; }

        /// <summary>
        /// list of outputs defined by this script
        /// </summary>
        public List<BlastVariableMapping> Outputs { get; set; }

        /// <summary>
        /// list of validations defined by this script
        /// </summary>
        public Dictionary<string, string> Validations { get; set; }

        /// <summary>
        /// list of tokens as parsed out of the script
        /// </summary>
        public List<Tuple<BlastScriptToken, string>> Tokens { get; set; }

        /// <summary>
        /// create a holder for variable data collected during compilation 
        /// - will log errors if the variable exists and returns null 
        /// - initializes reference count at 1
        /// </summary>
        /// <param name="name">first part of identifier - the name</param>
        /// <param name="is_input">true if used as an input</param>
        /// <param name="is_output">true if used as an output</param>
        /// <returns>null on failure, any error will be logged</returns>
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
        /// - does not reference count 
        /// </summary>
        /// <param name="name">the name of the variable to lookup</param>
        /// <param name="variable">output variable</param>
        /// <returns>true if found</returns>
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

        /// <summary>
        /// Try to lookup a input variable mapping 
        /// </summary>
        /// <param name="v">the variable</param>
        /// <param name="mapping">output mapping</param>
        /// <returns>true if found</returns>
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

        /// <summary>
        /// try to lookup an output variable mapping
        /// </summary>
        /// <param name="v">the variable</param>
        /// <param name="mapping">the output variable mapping</param>
        /// <returns>true if found</returns>
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
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member 'CompilationData.ExistsVariable(string)'
        public bool ExistsVariable(string name)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member 'CompilationData.ExistsVariable(string)'
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
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member 'CompilationData.GetVariable(string)'
        public BlastVariable GetVariable(string name)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member 'CompilationData.GetVariable(string)'
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
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member 'CompilationData.GetVariableFromOffset(byte)'
        public BlastVariable GetVariableFromOffset(byte offset)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member 'CompilationData.GetVariableFromOffset(byte)'
        {
            int index = Offsets.IndexOf(offset);

            // assume it only grows and never shrinks while multithreading 
            if (index < 0 || Variables == null || index >= Variables.Count) return null;

            lock (Variables)
            {
                return Variables[index];
            }
        }


#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member 'CompilationData.CanValidate'
        public bool CanValidate => Validations != null && Validations.Count > 0;
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member 'CompilationData.CanValidate'
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member 'CompilationData.HasDefines'
        public bool HasDefines => Defines != null && Defines.Count > 0;
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member 'CompilationData.HasDefines'
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member 'CompilationData.HasVariables'
        public bool HasVariables => Variables != null && Variables.Count > 0;
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member 'CompilationData.HasVariables'
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member 'CompilationData.HasOffsets'
        public bool HasOffsets => Offsets != null && Offsets.Count > 0;
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member 'CompilationData.HasOffsets'
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member 'CompilationData.VariableCount'
        public int VariableCount => Variables != null ? Variables.Count : 0;
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member 'CompilationData.VariableCount'
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member 'CompilationData.OffsetCount'
        public int OffsetCount => Offsets != null ? Offsets.Count : 0;
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member 'CompilationData.OffsetCount'

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member 'CompilationData.HasInput(int)'
        public bool HasInput(int id)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member 'CompilationData.HasInput(int)'
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
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member 'CompilationData.HasInput(string)'
        public bool HasInput(string name)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member 'CompilationData.HasInput(string)'
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

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member 'CompilationData.CalculateVariableOffsets()'
        internal unsafe int CalculateVariableOffsets()
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member 'CompilationData.CalculateVariableOffsets()'
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