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
using Unity.Mathematics;

namespace NSS.Blast.Compiler.Stage
{

   
    /// <summary>
    /// The Tokenizer:
    /// 
    /// - splits input into list of tokens and identifiers 
    /// - splits out: comments, defines and validations (all stuff starting with # until eol)
    /// 
    /// </summary>
    public class BlastTokenizer : IBlastCompilerStage
    {
        /// <summary>
        /// version
        /// </summary>
        public Version Version => new Version(0, 2, 1);
        
        /// <summary>
        /// Tokenizer -> converts input text into array of tokens 
        /// </summary>
        public BlastCompilerStageType StageType => BlastCompilerStageType.Tokenizer;


        #region static parsing helpers 
        /// <summary>
        /// classify char as token, default to identifier 
        /// </summary>
        /// <param name="ch"></param>
        /// <returns></returns>
        static BlastScriptToken parse_token(char ch)
        {
            if (is_comment_start(ch) || is_whitespace(ch))
            {
                return BlastScriptToken.Nop;
            }

            // match to any token
            foreach (var d in Blast.Tokens)
            {
                if (ch == d.Match) return d.Token;
            }

            // classify as identifier 
            return BlastScriptToken.Identifier;
        }

        static bool is_comment_start(char ch)
        {
            return ch == Blast.Comment;
        }

        /// <summary>
        /// read past eol 
        /// </summary>
        static int scan_to_comment_end(string code, int i)
        {
            // read past any eol characters
            while (i < code.Length && code[i] != '\n' && code[i] != '\r' && code[i] != ';')
            {
                i++;
            };
            return i;
        }

        static int scan_until(string code, char until, int start)
        {
            for(int i = start; i < code.Length; i++)
            {
                if (code[i] == until) return i;
            }
            return -1;
        }

        static int scan_until(string code, char until1, char until2, int start)
        {
            for (int i = start; i < code.Length; i++)
            {
                if (code[i] == until1 || code[i] == until2) return i;
            }
            return -1;
        }


        static string scan_until_stripend(string comment, int start = 6)
        {
            int i_end = scan_until(comment, '#', start);
            if (i_end >= 0)
            {
                // and remove that part, assume it to be a comment on the output def. 
                comment = comment.Substring(0, i_end);
            }

            return comment;
        }

        static bool is_operation_that_allows_to_combine_minus(BlastScriptToken token)
        {
            switch(token)
            {
                // the obvious =-*/
                case BlastScriptToken.Add:
                case BlastScriptToken.Substract:
                case BlastScriptToken.Multiply:
                case BlastScriptToken.Divide:
                // the booleans &|^
                case BlastScriptToken.And:
                case BlastScriptToken.Or:
                case BlastScriptToken.Xor:
                case BlastScriptToken.Not:
                case BlastScriptToken.GreaterThenEquals:
                case BlastScriptToken.GreaterThen:
                case BlastScriptToken.SmallerThen:
                case BlastScriptToken.SmallerThenEquals:
                // assignments count
                case BlastScriptToken.Equals:
                // as wel as opening a closure (
                case BlastScriptToken.OpenParenthesis:
                    return true; 
            }
            return false;
        }


        /// <summary>
        /// classify char as whitespace: space, tabs, cr/lf, , (comma)
        /// </summary>
        /// <param name="ch"></param>
        /// <returns>true if a whitespace character</returns>
        static bool is_whitespace(char ch)
        {
            return ch == ' ' || ch == '\t' || ch == (char)10 || ch == (char)13;// || ch == ',';
        }

        /// <summary>
        /// check if char is a defined script token
        /// </summary>
        /// <returns>true if a token</returns>
        static bool is_token(char ch)
        {
            if (is_whitespace(ch)) return false;

            foreach (var d in Blast.Tokens)
            {
                if (ch == d.Match) return true;
            }
            return false;
        }

        #endregion

        /// <summary>
        /// read #input and #output defines 
        /// </summary>
        /// <remarks>
        /// <code>
        /// #input id datatype default(s)
        /// #output id datatype default(s)
        /// </code>
        /// </remarks>
        /// 
        /// <param name="data"></param>
        /// <param name="comment"></param>
        /// <param name="a"></param>
        /// <param name="is_input"></param>
        /// <returns>null on failure (also logs error in data)</returns>
        static BlastVariableMapping read_input_output_mapping(IBlastCompilationData data, string comment, string[] a, bool is_input)
        {
            // get input|output name 
            string input_variable_id = a[1];

            int offset = 0;
            if (data.HasInputs)
            {
                for (int i = 0; i < data.Inputs.Count; i++)
                {
                    offset += data.Inputs[i].ByteSize;
                }
            }

            // read the datatype 
            BlastVariableDataType datatype = BlastVariableDataType.Numeric;
            int vector_size, byte_size;

            if(!Blast.GetDataTypeInfo(a[2], out datatype, out vector_size, out byte_size))
            {
                data.LogError($"tokenizer.read_input_output_mapping: failed to interpret input variable typename {a[1]} as a valid type", (int)BlastError.error_input_type_invalid);
                return null; 
            }

            // defaults? 
            float4 variable_default = default; 
            if(a.Length > 3)
            {
                // if there are defaults, then depending on datatype enforce vectorsize and type (only float now..)
                int c = a.Length - 3; 
                if(c != vector_size)
                {
                    data.LogError($"tokenizer.read_input_output_mapping: failed to interpret default as valid data for type, variable: {input_variable_id}, type: {datatype}, vectorsize: {vector_size}, bytesize: {byte_size}", (int)BlastError.error_input_ouput_invalid_default);
                    return null;  
                }
                switch(datatype)
                {
                    case BlastVariableDataType.Numeric:
                        {
                            for (int i = 0; i < vector_size; i++)
                            {
                                float f = CodeUtils.AsFloat(a[3 + i]);
                                if (float.IsNaN(f))
                                {
                                    data.LogError($"tokenizer.read_input_output_mapping: failed to parse default[{i}] as numeric, variable: {input_variable_id}, type: {datatype}, vectorsize: {vector_size}, bytesize: {byte_size}, data = {a[3 + i]}", (int)BlastError.error_input_ouput_invalid_default);
                                    return null;
                                }
                                variable_default[i] = f;
                            }
                        }
                        break;

                    case BlastVariableDataType.Bool32:
                        {
                            // vectorsize can only be 1
                            float f = 0;
                            unsafe
                            {
                                uint* b32 = (uint*)(void*)&f;
                                if (CodeUtils.TryAsBool32(a[3 + 1], out uint ui32))
                                {
                                    b32[0] = ui32; 
                                }
                                else
                                {
                                    data.LogError($"tokenizer.read_input_output_mapping: failed to parse default as Bool32, variable: {input_variable_id}, type: {datatype}, vectorsize: {vector_size}, bytesize: {byte_size}, data = {a[3]}", (int)BlastError.error_input_ouput_invalid_default);
                                    return null;
                                }
                            }
                            variable_default[0] = f; 
                        }
                        break; 
                }
            }


            BlastVariable v;
            CompilationData cdata = (CompilationData)data; 

            if (is_input)
            {
                // create variable, validate: it should not exist at this stage
                v = cdata.CreateVariable(input_variable_id, datatype, vector_size, true, false);
                if (v == null)
                {
                    data.LogError($"tokenizer.read_input_output_mapping: failed to create input variable with name {input_variable_id}, type: {datatype}, vectorsize: {vector_size}, bytesize: {byte_size}", (int)BlastError.error_input_failed_to_create);
                    return null;
                }
                v.DataType = datatype;
            }
            else
            {
                // output variable, it may exist: in that case validate its datatype and size 
                if (cdata.TryGetVariable(a[1], out v))
                {
                    // it exists : validate it and set as output 
                    v.IsOutput = true;
                    v.AddReference();

                    // validate datatype 
                    if (v.DataType != datatype || v.VectorSize != vector_size)
                    {
                        data.LogError($"tokenizer.read_input_output_mapping: failed to validate input&output variable with name {a[1]}, datatype mismatch, input specifies '{v.DataType}[{v.VectorSize}]', output says: '{datatype}[{vector_size}]'", (int)BlastError.error_output_datatype_mismatch);
                        return null;
                    }
                }
                else
                {
                    // create a new output variable 
                    v = cdata.CreateVariable(a[1], datatype, vector_size, false, true); // refcount is increased in create
                    if (v == null)
                    {
                        data.LogError($"tokenizer.read_input_output_mapping: failed to create output variable with name {a[1]}");
                        return null;
                    }
                    v.DataType = datatype;
                }
            }

            // setup mapping, this links variables with the input|output
            BlastVariableMapping mapping = new BlastVariableMapping()
            {
                Variable = v,
                ByteSize = byte_size,
                Offset = offset,
                Default = variable_default 
            };

            return mapping;
        }

        static int tokenize(IBlastCompilationData data, string code, bool replace_tabs_with_space = true)
        {
            BlastScriptToken previous_token = BlastScriptToken.Nop;
            BlastScriptToken token = BlastScriptToken.Nop;

            // replace all tab characters with a space char
            if(replace_tabs_with_space)
            {
                code = code.Replace('\t', ' '); 
            }

            List<Tuple<BlastScriptToken, string>> tokens = data.Tokens;

            // tokenize script into array of tokens
            int i1 = 0;
            while (i1 < code.Length)
            {
                if (is_whitespace(code[i1])) { i1++; continue; }

                // check if a #comment, #define or #validation #disable
                if (is_comment_start(code[i1]))
                {
                    int i_comment_end = scan_to_comment_end(code, i1 + 1);
                    string comment = code.Substring(i1, i_comment_end - i1);

                    // if defining stuff in scripts they must be the first statements 
                    if (comment.StartsWith("#define ", StringComparison.OrdinalIgnoreCase))
                    {
                        if (tokens.Count > 0)
                        {
                            // not allowed   must be first things 
                            data.LogError("tokenize: #defines must be the first statements in a script");
                            return (int)BlastError.error;
                        }

                        comment = scan_until_stripend(comment);

                        // get defined value seperated by spaces or tabs 
                        string[] a = comment.Trim().Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                        if (a.Length == 3)
                        {
                            string def_key = a[1].Trim().ToLowerInvariant();

                            unsafe
                            {
                                // check it does not map to a function name
                                if (data.Blast.Data->GetFunction(def_key).IsValid)
                                {
                                    data.LogError($"tokenize: #define identifier name: <{def_key}> is already mapped to a function");
                                }
                                else
                                {
                                    // it may also not be a constant name 
                                    if (Blast.IsNamedSystemConstant(def_key) != blast_operation.nop)
                                    {
                                        data.LogError($"tokenize: #define identifier name: <{def_key}> is already mapped to a system constant");
                                    }
                                    else
                                    {
                                        // it has to be unique
                                        if (!data.Defines.ContainsKey(def_key))
                                        {
                                            data.Defines.Add(def_key, a[2].Trim());
                                        }
                                        else
                                        {
                                            data.LogError($"tokenize: #define identifier name: <{def_key}> has already been defined with value: {data.Defines[def_key]}");
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            data.LogError($"tokenize: encountered malformed value define: {comment}\nExpecting a format like: #define NAME 3.3");
                        }
                    }
                    else
                    // store any validations for automatic testing
                    if (comment.StartsWith("#validate ", StringComparison.OrdinalIgnoreCase))
                    {
                        comment = scan_until_stripend(comment);

                        // get defined value seperated by spaces
                        string[] a = comment.Trim().Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                        if (a.Length == 3)
                        {
                            data.Validations.Add(a[1].Trim(), a[2].Trim());
                        }
                        else
                        {
                            data.LogError($"tokenize: encountered malformed validation define: {comment}");
                        }
                    }
                    else
                    // input mapping 
                    if (comment.StartsWith("#input ", StringComparison.OrdinalIgnoreCase))
                    {
                        if (tokens.Count > 0)
                        {
                            // not allowed   must be among the first things scanned 
                            data.LogError("tokenize: #defines must be the first statements in a script");
                            return (int)BlastError.error;
                        }

                        // scan for the next# in comment (if any)
                        comment = scan_until_stripend(comment).Trim();

                        // 
                        // Include the ; as allowed terminator. i forget it myself too often that i should not close defines
                        // 

                        int i_end = scan_until(comment, '#', ';', 1);
                        if (i_end >= 0)
                        {
                            // and remove that part, assume it to be a comment on the output def.                             
                            comment = comment.Substring(0, i_end);
                        }

                        // split result into strings 
                        string[] a = comment.Trim().Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                        if (a.Length >= 3)
                        {
                            BlastVariableMapping mapping = read_input_output_mapping(data, comment, a, true);
                            if (mapping != null)
                            {
                                data.Inputs.Add(mapping);
                            }
                            else
                            {
                                data.LogError($"tokenize: failed to read input define: {comment}\nExpecting format: '#input id offset bytesize'");
                            }
                        }
                        else
                        {
                            data.LogError($"tokenize: encountered malformed input define: {comment}\nExpecting format: '#input id offset bytesize'");
                        }
                    }
                    else
                    // output mapping 
                    if (comment.StartsWith("#output ", StringComparison.OrdinalIgnoreCase))
                    {
                        // we could merge input and output parsing as code duplicates.. TODO 
                        if (tokens.Count > 0)
                        {
                            // not allowed   must be first things 
                            data.LogError("tokenize: #defines must be the first statements in a script");
                            return (int)BlastError.error;
                        }

                        // 
                        // Include the ; as allowed terminator. i forget it myself too often that i should not close defines
                        // 

                        int i_end = scan_until(comment, '#', ';', 1);
                        if (i_end >= 0)
                        {
                            // and remove that part, assume it to be a comment on the output def.                             
                            comment = comment.Substring(0, i_end);
                        }

                        string[] a = comment.Trim().Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                        if (a.Length >= 3)
                        {
                            BlastVariableMapping mapping = read_input_output_mapping(data, comment, a, false);
                            if (mapping != null)
                            {
                                data.Outputs.Add(mapping);
                            }
                            else
                            {
                                data.LogError($"tokenize: failed to read output define: {comment}\nExpecting format: '#output id offset bytesize'");
                            }
                        }
                        else
                        {
                            data.LogError($"tokenize: encountered malformed output define: {comment}\nExpecting format: '#output id offset bytesize'");
                        }
                    }
                    else
                    {
                        // it has to be a comment, log trace 
                        // data.LogTrace($"trace: {comment}");
                    }

                    i1 = math.max(i1 + 1, i_comment_end + 1);
                    continue;
                }

                // check if a token 
                if (is_token(code[i1]))
                {
                    previous_token = i1 > 0 ? parse_token(code[i1 - 1]) : BlastScriptToken.Nop;
                    token = parse_token(code[i1]);

                    // some tokens may alter the previous token (multi char tokens) 
                    // <= >= !=
                    switch (token)
                    {
                        case BlastScriptToken.Equals:
                            {
                                switch (previous_token)
                                {
                                    case BlastScriptToken.Equals:
                                        {
                                            // this would also allow ======== 
                                            i1++;
                                        }
                                        continue; 
                                    case BlastScriptToken.GreaterThen:
                                        {
                                            token = BlastScriptToken.GreaterThenEquals;
                                            tokens[tokens.Count - 1] = new Tuple<BlastScriptToken, string>(BlastScriptToken.GreaterThenEquals, null);
                                            i1++;
                                        }
                                        continue;
                                    case BlastScriptToken.SmallerThen:
                                        {
                                            token = BlastScriptToken.SmallerThenEquals;
                                            tokens[tokens.Count - 1] = new Tuple<BlastScriptToken, string>(BlastScriptToken.SmallerThenEquals, null);
                                            i1++;
                                        }
                                        continue;
                                    case BlastScriptToken.Not:
                                        {
                                            token = BlastScriptToken.NotEquals;
                                            tokens[tokens.Count - 1] = new Tuple<BlastScriptToken, string>(BlastScriptToken.NotEquals, null);
                                            i1++;
                                        }
                                        continue;
                                }
                            }
                            break;

                        case BlastScriptToken.Substract:
                            {
                                switch(previous_token)
                                {
                                    case BlastScriptToken.Substract:
                                        {
                                            // -- => +
                                            token = BlastScriptToken.Add; 
                                            tokens[tokens.Count - 1] = new Tuple<BlastScriptToken, string>(BlastScriptToken.Add, null);
                                            i1++;
                                        }
                                        continue;

                                    case BlastScriptToken.Add:
                                        {
                                            // -+ => -
                                            token = BlastScriptToken.Substract;
                                            tokens[tokens.Count - 1] = new Tuple<BlastScriptToken, string>(BlastScriptToken.Substract, null);
                                            i1++;
                                        }
                                        continue; 
                                }
                            }
                            break;

                        case BlastScriptToken.Add:
                            {
                                switch (previous_token)
                                {
                                    case BlastScriptToken.Substract:
                                        {
                                            // -+ => -
                                            token = BlastScriptToken.Substract;
                                            tokens[tokens.Count - 1] = new Tuple<BlastScriptToken, string>(BlastScriptToken.Substract, null);
                                            i1++;
                                        }
                                        continue;

                                    case BlastScriptToken.Add:
                                        {
                                            // ++ => +
                                            token = BlastScriptToken.Add; 
                                            tokens[tokens.Count - 1] = new Tuple<BlastScriptToken, string>(BlastScriptToken.Add, null);
                                            i1++;
                                        }
                                        continue;
                                }
                            }
                            break;                        
                    }

                    // if reaching here it was a single char token
                    tokens.Add(new Tuple<BlastScriptToken, string>(token, null));
                    i1++;
                    continue;
                }
                else
                {
                    previous_token = BlastScriptToken.Nop;
                    token = BlastScriptToken.Nop;
                }

                // the rest are identifiers like: 
                // 
                //      Identifier,     // [a..z][0..9|a..z]*[.|[][a..z][0..9|a..z]*[]]
                //      Indexer,        // .        * using the indexer on a numeric will define its fractional part 
                //      IndexOpen,      // [
                //      IndexClose,     // ]
                //
                //
                //    23423.323
                //    1111
                //    a234.x
                //    a123[3].x
                //    a123[a - 3].x[2].x
                //
                // 
                //  OR INLINE FUNCTIONS 
                //
                //
                // whitespace is allowed in between indexers when not numeric 
                // parser will parse identifier into compounds if needed
                // tokenizer will keep it simple and will only read numerics (as it can be determined by the starting digit of the identifier)
                // 
                int i2 = i1 + 1;
                bool have_numeric = char.IsDigit(code[i1]);
                bool numeric_has_dot = false;
                while (i2 < code.Length)
                {
                    char ch = code[i2];
                    bool whitespace = is_whitespace(ch);
                    bool eof = i2 == code.Length;
                    bool istoken = is_token(ch);

                    if ((have_numeric && ch == '.') || (!whitespace && !istoken && !eof))
                    {
                        if (have_numeric && ch == '.')
                        {
                            if (numeric_has_dot)
                            {
                                data.LogError("tokenizer: failed to read numeral identifier, encountered multiple .'s");
                                return (int)BlastError.error;
                            }
                            else
                            {
                                numeric_has_dot = true;
                            }
                        }

                        // read next part of identifier 
                        i2++;
                        continue;
                    }

                    // indexed indentifiers are rebuilt in a later stage 
                    int l = i2 - i1;
                    if (l > 0)
                    {
                        // classify identifier
                        string identifier = code.Substring(i1, l);

                        if (have_numeric)
                        {
                            // check the last 2 tokens, if the last is - and before that is either nothing or another operation then 
                            // the minus can be combined with the numeric in identifier 
                            bool combine_minus =
                                // == second token, first is minus, 
                                (tokens.Count == 1  && tokens[0].Item1 == BlastScriptToken.Substract)
                                ||
                                (tokens.Count > 2   && tokens[tokens.Count - 1].Item1 == BlastScriptToken.Substract
                                                    && is_operation_that_allows_to_combine_minus(tokens[tokens.Count - 2].Item1));

                            // we combine the minus with the id, but 
                            // WE DONT replace - - with +, this could change operations if multiplication rules are applied
                            if (combine_minus)
                            {
                                // combine - with identifier
                                tokens[tokens.Count - 1] = new Tuple<BlastScriptToken, string>(BlastScriptToken.Identifier, "-" + identifier);
                            }
                            else
                            {
                                // add the numeric as an identifier 
                                tokens.Add(new Tuple<BlastScriptToken, string>(BlastScriptToken.Identifier, identifier));
                            }
                        }
                        else
                        {
                            // filter reserved words   todo  reserved word2token table ??
                            identifier = identifier.Trim().ToLowerInvariant();
                            switch (identifier)
                            {
                                case "if":
                                    tokens.Add(new Tuple<BlastScriptToken, string>(BlastScriptToken.If, identifier));
                                    break;

                                case "then":
                                    tokens.Add(new Tuple<BlastScriptToken, string>(BlastScriptToken.Then, identifier));
                                    break;

                                case "else":
                                    tokens.Add(new Tuple<BlastScriptToken, string>(BlastScriptToken.Else, identifier));
                                    break;

                                case "while":
                                    tokens.Add(new Tuple<BlastScriptToken, string>(BlastScriptToken.While, identifier));
                                    break;

                                case "switch":
                                    tokens.Add(new Tuple<BlastScriptToken, string>(BlastScriptToken.Switch, identifier));
                                    break;

                                case "case":
                                    tokens.Add(new Tuple<BlastScriptToken, string>(BlastScriptToken.Case, identifier));
                                    break;

                                case "default":
                                    tokens.Add(new Tuple<BlastScriptToken, string>(BlastScriptToken.Default, identifier));
                                    break;

                                case "for":
                                    tokens.Add(new Tuple<BlastScriptToken, string>(BlastScriptToken.For, identifier));
                                    break;

                                case "function":
                                    tokens.Add(new Tuple<BlastScriptToken, string>(BlastScriptToken.Function, identifier));
                                    break; 

                                default:
                                    {
                                        // if the identifier matches a script define then its value is replaced with the define

                                        // first check script defines 
                                        string v;
                                        if (data.Defines.TryGetValue(identifier, out v))
                                        {
                                            tokens.Add(new Tuple<BlastScriptToken, string>(BlastScriptToken.Identifier, v));
                                        }
                                        // then compiler defines, 
                                        // -> note that script defines overrule compiler defines 
                                        else if (data.CompilerOptions.Defines.TryGetValue(identifier, out v))
                                        {
                                            tokens.Add(new Tuple<BlastScriptToken, string>(BlastScriptToken.Identifier, v));
                                        }
                                        else
                                        {
                                            // add the identifier 
                                            tokens.Add(new Tuple<BlastScriptToken, string>(BlastScriptToken.Identifier, identifier));
                                        }
                                    }
                                    break;
                            }
                        }
                    }

                    // reaching here means we added an identifier and need to restart the outer loop
                    break;
                }

                i1 = i2;
            }

            return (int)BlastError.success;
        }

        /// <summary>
        /// Execute the tokenizer stage
        /// </summary>
        /// <param name="data">compilation data</param>
        /// <returns>success if ok, otherwise an errorcode</returns>
        public int Execute(IBlastCompilationData data)
        {
            // run tokenizer 
            return tokenize(data, data.Script.Code);
        }
    }



}