//############################################################################################################################
// BLAST v1.0.4c                                                                                                             #
// Copyright © 2022 Rob Lemmens | NijnStein Software <rob.lemmens.s31 gmail com> All Rights Reserved                   ^__^\ #
// Unauthorized copying of this file, via any medium is strictly prohibited proprietary and confidential               (oo)\ #
//                                                                                                                     (__)  #
//############################################################################################################################

#if STANDALONE_VSBUILD
using NSS.Blast.Standalone;
using System.Reflection;
using System.Runtime.CompilerServices;
#else
    using UnityEngine;
    using Unity.Burst.CompilerServices;
#endif


using Unity.Burst;
using Unity.Mathematics;

namespace NSS.Blast.Interpretor
{

    /// <summary>
    /// CData sections encode data in a given format and expose that in the type configured
    /// </summary>

    public enum CDATAEncodingType : byte
    {
        /// <summary>
        /// none - default - array of float32 
        /// </summary>
        None = 0,

        /// <summary>
        /// 0..255, 8 bit encoded, float32 datatype
        /// </summary>
        u8_fp32 = 1,

        /// <summary>
        /// -127..127 8 bit encoded, float32 datatype
        /// </summary>
        s8_fp32 = 2,

        /// <summary>
        /// 8 bit encoded float, float32 datatype
        /// </summary>
        fp8_fp32 = 3,

        /// <summary>
        /// fp16 encoded array, typed as float32 
        /// </summary>
        fp16_fp32 = 4,

        /// <summary>
        /// fp32 encoded typed also as normal floats, same as none 
        /// </summary>
        fp32_fp32 = 5,


        /// <summary>
        /// encoded as sbyte, typed as int32
        /// </summary>
        i8_i32 = 10, 

        /// <summary>
        /// encoded as short, typed as int32
        /// </summary>
        i16_i32 = 11, 

        /// <summary>
        /// encoded as int32, type as int32
        /// </summary>
        i32_i32 = 12, 
        

        /// <summary>
        /// bool32 - 32 bit boolean flag, backed with 32bits 
        /// </summary>
        bool32 = 20,


        /// <summary>
        /// the untyped CDATA 
        /// </summary>
        CDATA = (byte)blast_operation.cdata
    }



    unsafe public partial struct BlastInterpretor
    {

        /// <summary>
        /// follow a cdata reference to its offset in the codesegment. its length is checked or ignored depending on defines 
        /// </summary>
        /// <param name="code">codesegment pointer</param>
        /// <param name="code_pointer">code index pointer</param>
        /// <param name="offset">offset of the cdata object if found</param>
        /// <param name="length">length of the cdata object if found</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static internal bool follow_cdataref([NoAlias] byte* code, int code_pointer, out int offset, out int length, out CDATAEncodingType encoding)
        {
            // validate codepointer is at cdataref 
#if DEVELOPMENT_BUILD || TRACE || CHECK_CDATA
            if (code[code_pointer] != (byte)blast_operation.cdataref)
            {
                // errror
                Debug.LogError($"Blast.Interpretor.follow_cdataref: failed to follow cdataref to cdata record, codepointer not at cdataref, codepointer = {code_pointer}");
                length = 0;
                offset = 0;
                encoding = CDATAEncodingType.None; 
                return false;
            }
#endif
            // get offset
            offset = code_pointer - ((code[code_pointer + 1] << 8) + code[code_pointer + 2]) + 1;

#if DEVELOPMENT_BUILD || TRACE || CHECK_CDATA
            if (offset < 0 || !BlastInterpretor.IsValidCDATAStart(code[offset]))
            {
                // errror
                Debug.LogError($"Blast.Interpretor.follow_cdataref: failed to follow cdataref to cdata record, cdataref codepointer = {code_pointer}, offset = {offset}");
                length = 0;
                encoding = CDATAEncodingType.None;
                return false;
            }
#endif

            encoding = (CDATAEncodingType)code[offset];
            length = (code[offset + 1] << 8) + code[offset + 2];
            return true;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static internal float map_cdata_float([NoAlias] byte* code, in int offset_into_codebuffer)
        {
            float f = default;
            byte* p = (byte*)(void*)&f;

            p[0] = code[offset_into_codebuffer + 0];
            p[1] = code[offset_into_codebuffer + 1];
            p[2] = code[offset_into_codebuffer + 2];
            p[3] = code[offset_into_codebuffer + 3];

            return f;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static internal float map_cdata_float32([NoAlias] byte* code, in int offset_into_codebuffer)
        {
            return map_cdata_float(code, offset_into_codebuffer); 
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static internal float map_cdata_16_float32([NoAlias] byte* code, in int offset_into_codebuffer)
        {
            float f = default;
            byte* p = (byte*)(void*)&f;

            p[0] = 0;
            p[1] = 0;
            p[2] = code[offset_into_codebuffer + 0];
            p[3] = code[offset_into_codebuffer + 1];
            return f;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static internal float map_cdata_u8_float32([NoAlias] byte* code, in int offset_into_codebuffer)
        {
            return (float)code[offset_into_codebuffer];
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static internal float map_cdata_s8_float32([NoAlias] byte* code, in int offset_into_codebuffer)
        {
            return (float)((sbyte*)(void*)&code[offset_into_codebuffer])[0];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static internal float map_cdata_fp8_float32([NoAlias] byte* code, in int offset_into_codebuffer)
        {
            sbyte* p = (sbyte*)(void*)code;
            return (float)code[offset_into_codebuffer];
        }


        /// <summary>
        /// determine if the cdata block starts with a valid opcode 
        /// </summary>
        /// <param name="code">the opcode the block starts with</param>
        /// <returns></returns>
        static internal bool IsValidCDATAStart(byte code)
        {
            switch ((CDATAEncodingType)code)
            {
                case CDATAEncodingType.CDATA:

                case CDATAEncodingType.fp16_fp32:
                case CDATAEncodingType.fp32_fp32:
                case CDATAEncodingType.fp8_fp32: 
                case CDATAEncodingType.u8_fp32:
                case CDATAEncodingType.s8_fp32:

                case CDATAEncodingType.bool32:

                case CDATAEncodingType.i8_i32:
                case CDATAEncodingType.i16_i32:
                case CDATAEncodingType.i32_i32: return true; 
            }
            return false;
        }


        /// <summary>
        /// determine bytesize of cdata object
        /// </summary>
        /// <param name="code">code</param>
        /// <param name="index">[cdata] index</param> 
        /// <returns>size of content in bytes</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static internal int GetCDATAContentByteSize(byte* code, int index)
        {
            return (code[index + 1] << 8) + code[index + 2];
        }

        /// <summary>
        /// determine element count of content in cdata object
        /// </summary>
        /// <param name="code">code</param>
        /// <param name="index">[cdata] index</param> 
        /// <returns>element count</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static internal int GetCDATAElementCount(byte* code, int index)
        {
            CDATAEncodingType encoding = (CDATAEncodingType) code[index];
            switch (encoding)
            {
                case CDATAEncodingType.CDATA:
                case CDATAEncodingType.fp32_fp32:
                case CDATAEncodingType.bool32:
                case CDATAEncodingType.i32_i32:
                    // 4 bytes per element
                    return GetCDATAContentByteSize(code, index) / 4; 
                    

                case CDATAEncodingType.fp16_fp32:
                case CDATAEncodingType.i16_i32:
                    // 2 bytes per element
                    return GetCDATAContentByteSize(code, index) / 2;


                case CDATAEncodingType.fp8_fp32:
                case CDATAEncodingType.u8_fp32:
                case CDATAEncodingType.s8_fp32:
                case CDATAEncodingType.i8_i32:
                    // 1 byte per elemennt
                    return GetCDATAContentByteSize(code, index); 
            }
            return 0;
        }


        /// <summary>
        /// translate a string into cdataencodingtype 
        /// </summary>
        /// <param name="encoding_name">name of the encoding </param>
        /// <returns>the encoding type</returns>
        [BurstDiscard]
        public static CDATAEncodingType GetCDATAEncodingType(string encoding_name)
        {
            switch (encoding_name.Trim().ToLowerInvariant())
            {
                // float 32 and its backing types 
                case "fp16fp32": 
                case "fp16_fp32": return CDATAEncodingType.fp16_fp32;
                case "fp32fp32": 
                case "fp32_fp32": return CDATAEncodingType.fp32_fp32;
                case "fp8fp32": 
                case "fp8_fp32": return CDATAEncodingType.fp8_fp32;
                case "u8fp32":
                case "u8_fp32": return CDATAEncodingType.u8_fp32;
                case "s8fp32": 
                case "s8_fp32": return CDATAEncodingType.s8_fp32; 


                // int 32 and its backing
                case "i8i32":
                case "i8_i32": return CDATAEncodingType.i8_i32;
                case "i16i32":
                case "i16_i32": return CDATAEncodingType.i16_i32;
                case "i32i32":
                case "i32_i32": return CDATAEncodingType.i32_i32;

                case "bool32":
                case "b32": return CDATAEncodingType.bool32;

            }

            return CDATAEncodingType.None;
        }

        /// <summary>
        /// get element bytesize for a given encoding type 
        /// </summary>
        /// <param name="encoding"></param>
        /// <returns></returns>
        public static int GetCDATAEncodingByteSize(CDATAEncodingType encoding)
        {
            switch (encoding)
            {
                default:
                case CDATAEncodingType.CDATA: 
                case CDATAEncodingType.None:
                case CDATAEncodingType.fp32_fp32:
                case CDATAEncodingType.i32_i32:
                case CDATAEncodingType.bool32: return 4;

                case CDATAEncodingType.s8_fp32:
                case CDATAEncodingType.u8_fp32:
                case CDATAEncodingType.fp8_fp32:
                case CDATAEncodingType.i8_i32: return 1; 
                case CDATAEncodingType.fp16_fp32:
                case CDATAEncodingType.i16_i32: return 2;
            }
        }



        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static internal float index_cdata_f1([NoAlias] byte* code, in int offset, in int index, in int byte_length)
        {
            int bytes_per_element;
            int max_i = offset + 3 + byte_length;

            CDATAEncodingType type = (CDATAEncodingType)code[offset];
            switch (type)
            {
                default: 
                case CDATAEncodingType.None:
                case CDATAEncodingType.fp32_fp32:
                    {
                        bytes_per_element = 4;

                        // calc index                         
                        int i = offset + 3 + index * bytes_per_element;
                        
                        // check bounds 
                        if (i >= max_i - (bytes_per_element - 1) || i < offset + 3) break;  // out of bounds 

                        return map_cdata_float32(code, i);
                    }
                    break;


                case CDATAEncodingType.fp16_fp32:
                    {
                        bytes_per_element = 2;
                        int i = offset + 3 + index * bytes_per_element;
                        if (i >= max_i - (bytes_per_element - 1) || i < offset + 3) break;  
                        return map_cdata_16_float32(code, i);
                    }
                    break;

                case CDATAEncodingType.fp8_fp32:
                    {
                        bytes_per_element = 1;
                        int i = offset + 3 + index * bytes_per_element;
                        if (i >= max_i - (bytes_per_element - 1) || i < offset + 3) break;  
                        return map_cdata_fp8_float32(code, i);
                    }
                    break;

                case CDATAEncodingType.u8_fp32:
                    {
                        bytes_per_element = 1;
                        int i = offset + 3 + index * bytes_per_element;
                        if (i >= max_i - (bytes_per_element - 1) || i < offset + 3) break;  
                        return map_cdata_u8_float32(code, i);
                    }
                    break;

                case CDATAEncodingType.s8_fp32:
                    {
                        bytes_per_element = 1;
                        int i = offset + 3 + index * bytes_per_element;
                        if (i >= max_i - (bytes_per_element - 1) || i < offset + 3) break;
                        return map_cdata_s8_float32(code, i);

                    }
            }

            if (IsTrace)
            {
                Debug.LogError($"Blast.Interpretor.index_cdata_f1: index [{index}] out of bounds for cdata at {offset} of bytesize: {byte_length} ");
            }
            return float.NaN;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static internal void set_cdata_float([NoAlias] byte* code, in int offset_into_codebuffer, int index, float f)
        {
            CDATAEncodingType encoding = (CDATAEncodingType)code[offset_into_codebuffer];
            switch (encoding)
            {
                case CDATAEncodingType.CDATA:
                case CDATAEncodingType.fp32_fp32:
                    {
                        // cdata, float32 backed
                        byte* p = (byte*)(void*)&f;
                        int offset = offset_into_codebuffer + (index * 4) + 3; // float == 4 bytes 

                        code[offset + 0] = p[0];
                        code[offset + 1] = p[1];
                        code[offset + 2] = p[2];
                        code[offset + 3] = p[3];
                    }
                    return;

                case CDATAEncodingType.u8_fp32:
                    {
                        // unsigned byte as backing: 0 through 255
                        byte b = (byte)f;
                        int offset = offset_into_codebuffer + index + 3; // 1 byte/element 
                        code[offset + 0] = b;
                    }
                    return;

                case CDATAEncodingType.s8_fp32:
                    {
                        // signed byte as backing: -128 through 127
                        sbyte sb = (sbyte)f;
                        int offset = offset_into_codebuffer + index + 3; // 1 byte/element 
                        code[offset + 0] =  ((byte*)(void*)&sb)[0];
                    }
                    return;

                case CDATAEncodingType.fp8_fp32:
                    {
                        // 1 byte float, needs work to be usefull.. 
                        sbyte s = (sbyte)f;
                        int offset = offset_into_codebuffer + index + 3; // 1 byte/element 
                        code[offset + 0] = ((byte*)(void*)&s)[0];
                    }
                    return;

                case CDATAEncodingType.fp16_fp32:
                    {
                        // float16 backed set with float
                        byte* p = (byte*)(void*)&f;
                        int offset = offset_into_codebuffer + (index * 2) + 3; // half == 2 bytes 
                        code[offset + 0] = p[2];
                        code[offset + 1] = p[3];
                    }
                    return;
            }

            if (IsTrace)
            {
                Debug.LogError($"Blast.Interpretor.set_cdata_float: failed to identify backing type ({encoding}) of cdata object at {offset_into_codebuffer}, indexed at {index}");
            }            
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static internal void set_cdata_float([NoAlias] byte* code, in int offset_into_codebuffer, int index, float2 f)
        {
            byte* p = (byte*)(void*)&f;

            int offset = offset_into_codebuffer + (index * 4) + 3; // float == 4 bytes 

            code[offset + 0] = p[0];
            code[offset + 1] = p[1];
            code[offset + 2] = p[2];
            code[offset + 3] = p[3];

            code[offset + 4] = p[4];
            code[offset + 5] = p[5];
            code[offset + 6] = p[6];
            code[offset + 7] = p[7];
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static internal void set_cdata_float([NoAlias] byte* code, in int offset_into_codebuffer, int index, float3 f)
        {
            byte* p = (byte*)(void*)&f;

            int offset = offset_into_codebuffer + (index * 4) + 3; // float == 4 bytes 

            code[offset + 0] = p[0];
            code[offset + 1] = p[1];
            code[offset + 2] = p[2];
            code[offset + 3] = p[3];

            code[offset + 4] = p[4];
            code[offset + 5] = p[5];
            code[offset + 6] = p[6];
            code[offset + 7] = p[7];

            code[offset + 8] = p[8];
            code[offset + 9] = p[9];
            code[offset + 10] = p[10];
            code[offset + 11] = p[11];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static internal void set_cdata_float([NoAlias] byte* code, in int offset_into_codebuffer, int index, float4 f)
        {
            byte* p = (byte*)(void*)&f;

            int offset = offset_into_codebuffer + (index * 4) + 3; // float == 4 bytes 

            code[offset + 0] = p[0];
            code[offset + 1] = p[1];
            code[offset + 2] = p[2];
            code[offset + 3] = p[3];

            code[offset + 4] = p[4];
            code[offset + 5] = p[5];
            code[offset + 6] = p[6];
            code[offset + 7] = p[7];

            code[offset + 8] = p[8];
            code[offset + 9] = p[9];
            code[offset + 10] = p[10];
            code[offset + 11] = p[11];

            code[offset + 12] = p[12];
            code[offset + 13] = p[13];
            code[offset + 14] = p[14];
            code[offset + 15] = p[15];
        }


    }









}
