//############################################################################################################################
// BLAST v1.0.4c                                                                                                             #
// Copyright © 2022 Rob Lemmens | NijnStein Software <rob.lemmens.s31 gmail com> All Rights Reserved                   ^__^\ #
// Unauthorized copying of this file, via any medium is strictly prohibited proprietary and confidential               (oo)\ #
//                                                                                                                     (__)  #
//############################################################################################################################

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
using NSS.Blast.Interpretor;

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
            for (int i = start; i < code.Length; i++)
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
            switch (token)
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

            if (!Blast.GetDataTypeInfo(a[2], out datatype, out vector_size, out byte_size))
            {
                data.LogError($"tokenizer.read_input_output_mapping: failed to interpret input variable typename {a[1]} as a valid type", (int)BlastError.error_input_type_invalid);
                return null;
            }

            // defaults? 
            float4 variable_default = default;
            byte[] constant_data = null;
            CDATAEncodingType cdata_encoding = CDATAEncodingType.None; 

            if (a.Length > 3)
            {
                // if there are defaults, then depending on datatype enforce vectorsize and type (only float now..)
                int c = a.Length - 3;
                if (datatype != BlastVariableDataType.CData && c != vector_size)
                {
                    data.LogError($"tokenizer.read_input_output_mapping: failed to interpret default as valid data for type, variable: {input_variable_id}, type: {datatype}, vectorsize: {vector_size}, bytesize: {byte_size}", (int)BlastError.error_input_ouput_invalid_default);
                    return null;
                }
                switch (datatype)
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
                                if (CodeUtils.TryAsBool32(a[3], true, out uint ui32))
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


                    case BlastVariableDataType.CData:
                        {
                            if (!is_input)
                            {
                                data.LogError($"Blast.Tokenizer.read_input_output_mapping: cdata is only allowed as input, variable: {input_variable_id}, type: {datatype}, vectorsize: {vector_size}, bytesize: {byte_size}, data = {a[3]}", (int)BlastError.error_input_ouput_invalid_default);
                                return null;
                            }

                            // all data after a[2] is part of the datastream 
                            constant_data = ReadCDATAFromMapping(data, comment, a, out cdata_encoding);
                            if (constant_data == null)
                            {
                                // it should already have logged why 
                                return null;
                            }

                            vector_size = 0; //(int)math.ceil((float)constant_data.Length / 4);
                            byte_size = constant_data.Length;
                        }
                        break;
                }
            }


            // setup the new variable  
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


            // if mapping to constantdata the data is stored in the variable 
            if (datatype == BlastVariableDataType.CData)
            {
                v.ConstantData = constant_data;
                v.ConstantDataEncoding = cdata_encoding;
                v.VectorSize = 0;
                v.IsConstant = false; // this is a constant data object added by input and thus resides in the datasegment
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


        /// <summary>
        /// read cdata data from mapping
        /// </summary>
        private static byte[] ReadCDATAFromMapping(IBlastCompilationData data, string comment, string[] a, out CDATAEncodingType cdata_encoding)
        {
            Assert.IsNotNull(data);
            Assert.IsTrue(a != null && a.Length > 3);


            int current_i = 3;
            bool is_typed = false;

            // first check if this cdata object is typed
            cdata_encoding = BlastInterpretor.GetCDATAEncodingType(a[current_i]);
            if (cdata_encoding != CDATAEncodingType.None)
            {
                is_typed = true;
                current_i++; 
            }


            // text: '2342sdfggd35'
            // float list: 235.4 234.3 2345.22 344.44 354234.23
            // bool32 list: 1111.>32x.111111    filled with 0;s until nxt byte

            // classify the first value, anything in a is at least 1 char long
            char ch = a[current_i][0];


            //#####################################################################################################
            // string: starts with opener: ' or " 
            // note: can span multiple elements if there is a space 
            //       we could account for that while parsing out the comment
            //       but this is cheaper to do considering its probably rare to be used 
            if (ch == '\'' || ch == '"')
            {
                // typing is out of the window, we look at string data as ascii unsigned bytes
                byte[] asciidata = ReadCDATAString(data, comment, a, current_i);
                cdata_encoding = CDATAEncodingType.ASCII;
                return asciidata; 
            }

            //#####################################################################################################
            // binary: allow only 1 single data element extra
            // binary: single bitstream of 1;s and 0s >= 16 bits 
            // binary: single bitstream starting with b
            if (ch == 'b' || ((ch == '1' || ch == '0') && a[current_i].Length >= 16))
            {
                if (a.Length == (current_i + 1) || a[current_i + 1].StartsWith("#"))
                {
                    if (is_typed)
                    {
                        if (cdata_encoding != CDATAEncodingType.bool32)
                        {
                            // keep it as is.. user might init an integer with a byte pattern who knows, as long as its done explicetly

                            // - verify its 32 bits long, we must be able to store the bool 
                            int bytesize = BlastInterpretor.GetCDATAEncodingByteSize(cdata_encoding);
                            if (bytesize != 4)
                            {
                                data.LogError($"blast.tokenizer: explicetly typed cdata object does not have the correct backing bitsize (32bits) to store the bool32 value.");
                                return null; 
                            }
                        }
                    }
                    else
                    {
                        cdata_encoding = CDATAEncodingType.bool32; 
                    }


                    return ReadCDATABinary(data, comment, a[current_i]);
                }
                else
                {
                    data.LogError($"Blast.Tokenizer.ReadCDATAFromMapping: failed to read cdata mapping, binary streams other then bool32 arrays should be continueus in 1 element, define: {comment}");
                    return null;
                }
            }

            //#####################################################################################################
            // anything else is assumed to be a list of numerics || bool32
            return ReadCDATAArray(data, comment, a, current_i);
        }

        /// <summary>
        /// read a constant data value from a define, parse as constant variable
        /// to be inlined as constant data 
        /// </summary>
        /// <param name="data"></param>
        /// <param name="comment"></param>
        /// <param name="a"></param>
        /// <returns></returns>
        static bool ReadCDATAFromDefine(IBlastCompilationData data, string comment, string[] a)
        {
            Assert.IsNotNull(data);
            Assert.IsTrue(a != null && a.Length > 2);

            // if first token is: float|numeric  bool32   
            string possible_datatype = a[2];
            int offset_for_datatype = 0;
            CDATAEncodingType encoding = CDATAEncodingType.None;

            switch(possible_datatype.ToLowerInvariant())
            {
                // default to numerics
                case "float":
                case "single":
                case "numeric": encoding = CDATAEncodingType.None; offset_for_datatype = 1; break;
                case "fp32":    encoding = CDATAEncodingType.None; offset_for_datatype = 1; break;

                case "fp16fp32":
                case "fp16_fp32": encoding = CDATAEncodingType.fp16_fp32; offset_for_datatype = 1; break;

                case "s8_fp32":
                case "s8fp32": encoding = CDATAEncodingType.s8_fp32; offset_for_datatype = 1; break;
                case "u8_fp32":
                case "u8fp32": encoding = CDATAEncodingType.u8_fp32; offset_for_datatype = 1; break;
                case "i32_i32":
                case "int": encoding = CDATAEncodingType.i32_i32; offset_for_datatype = 1; break;

                case "i16_i32": encoding = CDATAEncodingType.i16_i32; offset_for_datatype = 1; break;
                case "i8_i32": encoding = CDATAEncodingType.i8_i32; offset_for_datatype = 1; break;
                case "short": encoding = CDATAEncodingType.i16_i32; offset_for_datatype = 1; break;

                case "bool32": encoding = CDATAEncodingType.bool32; offset_for_datatype = 1; break;
                case "ascii":
                case "text":
                case "string": encoding = CDATAEncodingType.ASCII; offset_for_datatype = 1; break; 
            }                                 

            // classify the first value, anything in a is at least 1 char long
            char ch = a[2 + offset_for_datatype][0];
            byte[] constant_data = null;


            // string: starts with opener: ' or " 
            // note: can span multiple elements if there is a space 
            //       we could account for that while parsing out the comment
            //       but this is cheaper to do considering its probably rare to be used 
            if (ch == '\'' || ch == '"')
            {
                encoding = CDATAEncodingType.ASCII;
                constant_data = ReadCDATAString(data, comment, a, 2 + offset_for_datatype);
                if (constant_data == null) return false;
            }
            else

            // binary: allow only 1 single data element extra
            // binary: single bitstream of 1;s and 0s >= 16 bits 
            // binary: single bitstream starting with b
            if (ch == 'b' || ((ch == '1' || ch == '0') && a[2].Length >= 16))
            {
                if (a.Length == 3 + offset_for_datatype || a[3 + offset_for_datatype].StartsWith("#"))
                {
                    constant_data = ReadCDATABinary(data, comment, a[2 + offset_for_datatype]);
                    encoding = CDATAEncodingType.ASCII; 
                }
                else
                {
                    data.LogError($"Blast.Tokenizer.ReadCDATAFromDefine: failed to read cdata mapping, binary streams other then bool32 arrays should be continueus in 1 element, define: {comment}");
                    return false;
                }
            }
            else
            {
                // anything else is assumed to be a list of numerics || bool32
                constant_data = ReadCDATAArray(data, comment, a, 2 + offset_for_datatype);
                if (constant_data == null) return false;
            }


            // create a variable, the name MUST be unique at this point 
            string name = a[1];

            BlastVariable v = (data as CompilationData).CreateVariable(name, BlastVariableDataType.CData, 0, false, false);
            if (v == null)
            {
                data.LogError($"Blast.Tokenizer.ReadCDATAFromDefine: failed to create input variable with name {name}, type: CData, vectorsize: 0, bytesize: {constant_data.Length}", (int)BlastError.error_input_failed_to_create);
                return false;
            }

            // setup
            v.DataType = BlastVariableDataType.CData;
            v.ConstantData = constant_data;
            v.ConstantDataEncoding = encoding;
            v.VectorSize = constant_data == null ? 0 : constant_data.Length / 4;
            v.ReferenceCount = 0; // 0 references 
            v.IsConstant = true;  // this is a constant data object added by input and thus resides in the datasegment

            return true;
        }



        /// <summary>
        /// read an array of numerics, we have to assume we get numerics from input 
        /// </summary>
        static byte[] ReadCDATAArray(IBlastCompilationData data, string comment, string[] a, int offset)
        {
            Assert.IsNotNull(data);
            Assert.IsTrue(a != null && a.Length >= offset);

            byte[] bytes = new byte[(a.Length - offset) * 4];
            int c = 0;

            for (int i = offset; i < a.Length; i++)
            {
                float f = a[i].Trim().AsFloat();
                if (math.isnan(f))
                {
                    data.LogError($"Blast.Tokenizer.ReadCDATAArray: encountered invalid numeric '{a[i]}' in cdata define: {comment}", (int)BlastError.error_tokenizer_invalid_cdata_array);
                    return null;
                }
                unsafe
                {
                    byte* p = (byte*)(void*)&f;
                    Assert.IsTrue(c <= (bytes.Length - 4));
                    bytes[c++] = p[0];
                    bytes[c++] = p[1];
                    bytes[c++] = p[2];
                    bytes[c++] = p[3];
                }
            }

            return bytes;
        }

        /// <summary>
        /// read a binary stream into an array of bytes 
        /// - can start with 'b', ignore it 
        /// </summary>
        /// <param name="data"></param>
        /// <param name="comment"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        static byte[] ReadCDATABinary(IBlastCompilationData data, string comment, string value)
        {
            Assert.IsNotNull(data);
            Assert.IsTrue(!string.IsNullOrWhiteSpace(value));

            List<byte> bytes = new List<byte>();

            int bi = 0;
            byte b = 0;

            for (int i = 0; i < value.Length; i++)
            {
                // ignore any value thats not a 1 or 0
                char ch = value[i];
                switch (ch)
                {
                    case '1': b = (byte)((b << 1) | 1); bi++; break;
                    case '0': b = (byte)(b << 1); bi++; break;

                    case '_':
                    case 'b': continue;

                    // anything else is an error 
                    default:
                        data.LogError($"Blast.Tokenizer.ReadCDATABinary: encountered invalid token '{ch}' in cdata define: {comment}", (int)BlastError.error_tokenizer_invalid_binary_stream);
                        return null;
                }

                // byte complete? 
                if (bi == 8)
                {
                    bytes.Add(b);
                    b = 0;
                    bi = 0;
                }
            }

            // validate reading at least 1 bit 
            if (bi == 0 && bytes.Count == 0)
            {
                data.LogError($"Blast.Tokenizer.ReadCDATABinary: failed to read any binary data in cdata binary define: {comment}", (int)BlastError.error_tokenizer_invalid_binary_stream);
                return null;
            }

            // append 0 to align data to full bytes 
            if (bi > 0)
            {
                if (bi < 8)
                {
                    b = (byte)(b << (8 - bi));
                }
                bytes.Add(b);
            }

            return bytes.ToArray();
        }


        /// <summary>
        /// read string data from a cdata define, this isnt strict about '"
        /// </summary>
        static byte[] ReadCDATAString(IBlastCompilationData data, string comment, string[] a, int offset)
        {
            Assert.IsNotNull(data);
            Assert.IsTrue(a != null && a.Length >= offset);

            List<byte> bytes = new List<byte>();

            for (int i = offset; i < a.Length; i++)
            {
                string s = a[i].Trim();
                int start_offset = 0, end_offset = 0;

                if (s[0] == '\'' || s[0] == '"' && bytes.Count == 0)
                {
                    // skip opening '
                    start_offset = 1;
                }
                if (i == a.Length - 1)
                {
                    // could end with ' 
                    if (s[s.Length - 1] == '\'' || s[s.Length - 1] == '"')
                    {
                        end_offset = 1;
                    }
                }
                for (int j = start_offset; j < s.Length - end_offset; j++)
                {
                    bytes.Add((byte)s[j]);
                }
            }

            return bytes.ToArray();

        }


        static BlastError TokenizeMapping(IBlastCompilationData data, string comment, List<Tuple<BlastScriptToken, string>> tokens, bool is_input = true)
        {
            Assert.IsNotNull(data);
            Assert.IsTrue(!string.IsNullOrWhiteSpace(comment));

            if (tokens.Count > 0)
            {
                // not allowed   must be first things 
                data.LogError("Blast.Tokenizer: #input and #output defines must be the first statements in a script");
                return BlastError.error;
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
                BlastVariableMapping mapping = read_input_output_mapping(data, comment, a, is_input);
                if (mapping != null)
                {
                    if (is_input)
                    {
                        data.Inputs.Add(mapping);
                    }
                    else
                    {
                        data.Outputs.Add(mapping);
                    }
                }
                else
                {
                    data.LogError($"Blast.Tokenizer: failed to read {(is_input ? "input" : "output")} define: {comment}\nExpecting format: '#output id offset bytesize'");
                    return BlastError.error;
                }
            }
            else
            {
                data.LogError($"tokenize: encountered malformed output define: {comment}\nExpecting format: '#output id offset bytesize'");
                return BlastError.error;
            }

            return BlastError.success;
        }

        static BlastError TokenizeCDataMapping(IBlastCompilationData data, string comment, List<Tuple<BlastScriptToken, string>> tokens)
        {
            Assert.IsNotNull(data);
            Assert.IsTrue(!string.IsNullOrWhiteSpace(comment));

            // we could merge input and output parsing as code duplicates.. TODO 
            if (tokens.Count > 0)
            {
                // not allowed   must be first things 
                data.LogError("Blast.Tokenizer: #defines must be the first statements in a script");
                return BlastError.error;
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
            if (a.Length >= 2)
            {
                if (!ReadCDATAFromDefine(data, comment, a))
                {
                    return BlastError.error_failed_to_read_cdata_define;
                }
            }
            else
            {
                data.LogError($"Blast.Tokenizer: encountered malformed output define: {comment}\nExpecting format: '#output id offset bytesize'");
                return BlastError.error;
            }

            return BlastError.success;
        }

        static BlastError TokenizeDefine(IBlastCompilationData data, string comment, List<Tuple<BlastScriptToken, string>> tokens)
        {
            Assert.IsNotNull(data);
            Assert.IsTrue(!string.IsNullOrWhiteSpace(comment));

            // if defining stuff in scripts they must be the first statements 
            if (comment.StartsWith("#define ", StringComparison.OrdinalIgnoreCase))
            {
                if (tokens.Count > 0)
                {
                    // not allowed   must be first things 
                    data.LogError("tokenize: #defines must be the first statements in a script");
                    return BlastError.error;
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
                            return BlastError.error;
                        }
                        else
                        {
                            // it may also not be a constant name 
                            if (Blast.IsNamedSystemConstant(def_key) != blast_operation.nop)
                            {
                                data.LogError($"tokenize: #define identifier name: <{def_key}> is already mapped to a system constant");
                                return BlastError.error;
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
                                    return BlastError.error;
                                }
                            }
                        }
                    }
                }
                else
                {
                    data.LogError($"tokenize: encountered malformed value define: {comment}\nExpecting a format like: #define NAME 3.3");
                    return BlastError.error;
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
                    return BlastError.error;
                }
            }
            else
            // input mapping 
            if (comment.StartsWith("#input ", StringComparison.OrdinalIgnoreCase))
            {
                return TokenizeMapping(data, comment, tokens, true);
            }
            else
            // output mapping 
            if (comment.StartsWith("#output ", StringComparison.OrdinalIgnoreCase))
            {
                return TokenizeMapping(data, comment, tokens, false);
            }
            // constant mapping 
            if (comment.StartsWith("#cdata ", StringComparison.OrdinalIgnoreCase))
            {
                return TokenizeCDataMapping(data, comment, tokens);
            }
            else
            {
                // it has to be a comment, log trace 
                // data.LogTrace($"trace: {comment}");
            }

            return BlastError.success;
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

                    BlastError res = TokenizeDefine(data, comment, tokens);
                    if (res != BlastError.success) return (int)res;

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
                bool can_be_binary = code[i1] == '0' || code[i1] == '1' || code[i1] == 'b' || code[i1] == 'B' || code[i1] == '_';

                while (i2 < code.Length)
                {
                    char ch = code[i2];
                    bool whitespace = is_whitespace(ch);
                    bool eof = i2 == code.Length;
                    bool istoken = is_token(ch);

                    can_be_binary = can_be_binary && (code[i2] == '0' || code[i2] == '1' || code[i2] == 'b' || code[i2] == 'B' || code[i2] == '_');
                    have_numeric = have_numeric && (code[i2] != '_');


                    if ((have_numeric && ch == '.') || (!whitespace && !istoken && !eof) || can_be_binary)
                    {
                        if (have_numeric && ch == '.')
                        {
                            if (numeric_has_dot)
                            {
                                data.LogError("Blast.Tokenizer: failed to read numeral identifier, encountered multiple .'s");
                                return (int)BlastError.error;
                            }
                            else
                            {
                                numeric_has_dot = true;
                            }
                        }

                        if(can_be_binary && !have_numeric)
                        {
                            //                           
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

                                case "true":
                                    tokens.Add(new Tuple<BlastScriptToken, string>(BlastScriptToken.Identifier, "1"));
                                    break; 

                                case "false":
                                    tokens.Add(new Tuple<BlastScriptToken, string>(BlastScriptToken.Identifier, "0"));
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