//############################################################################################################################
//                                                                                                                           #
//  ██████╗ ██╗      █████╗ ███████╗████████╗                           Copyright © 2022 Rob Lemmens | NijnStein Software    #
//  ██╔══██╗██║     ██╔══██╗██╔════╝╚══██╔══╝                                    <rob.lemmens.s31 gmail com>                 #
//  ██████╔╝██║     ███████║███████╗   ██║                                           All Rights Reserved                     #
//  ██╔══██╗██║     ██╔══██║╚════██║   ██║                                                                                   #
//  ██████╔╝███████╗██║  ██║███████║   ██║     V1.0.4e                                                                       #
//  ╚═════╝ ╚══════╝╚═╝  ╚═╝╚══════╝   ╚═╝                                                                                   #
//                                                                                                                           #
//############################################################################################################################
//                                                                                                                     (__)  #
//       Unauthorized copying of this file, via any medium is strictly prohibited proprietary and confidential         (oo)  #
//                                                                                                                     (__)  #
//############################################################################################################################
#pragma warning disable CS1591
#pragma warning disable CS0162



#if STANDALONE_VSBUILD
using NSS.Blast.Standalone;
#else
using UnityEngine;
    using Unity.Burst.CompilerServices;
#endif


using Unity.Burst;
using Unity.Mathematics;
using System.Reflection;
using System.Runtime.CompilerServices;
using Unity.Collections;

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
        /// ascii encoded string data 
        /// </summary>
        ASCII = 21,

        /// <summary>
        /// the untyped CDATA 
        /// </summary>
        CDATA = (byte)blast_operation.cdata
    }

    [BurstCompile]
    public static class CDATAEncodingTypeExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsIntegerType(this CDATAEncodingType encoding)
        {
            return (encoding == CDATAEncodingType.i8_i32) || (encoding == CDATAEncodingType.i16_i32) || (encoding == CDATAEncodingType.i32_i32);
        }
    }

    public struct CDATAREC
    {
        public ushort index;
        public ushort length;
        public ushort id; 
        public CDATAEncodingType encoding;
    }


    unsafe public partial struct BlastInterpretor
    {


        /// <summary>
        /// enumerate cdata records from bytecode, the returned data needs to be freed
        /// </summary>
        /// <param name="code">code bytes</param>
        /// <param name="codelength">lenght of code in bytes</param>
        /// <param name="allocator">allocator to use for returned data</param>
        /// <param name="cdata_count">the number of cdatarecords found</param>
        /// <param name="cdata_records">the actual cdatarecords information in native memory</param>
        /// <returns></returns>
        static public BlastError EnumerateCDATA([NoAlias] byte* code, int codelength, Allocator allocator, out int cdata_count, out CDATAREC* cdata_records)
        {
            cdata_count = 0;
            cdata_records = null;

            if (code == null) return BlastError.error; // null code -> error
            if (codelength < 4) return BlastError.success; // code too small to contain any cdata reference

            // if there is cdata the script starts with a jump over them
            int i = 0;
            int max = codelength;
            switch ((blast_operation)code[i])
            {

                case blast_operation.jump: max = code[i + 1]; i += 2; break; 
                case blast_operation.long_jump: max = (code[i + 1] << 8) + code[i + 2]; i += 3; break;
                default:
                    {
                        // any other instruction means that there is no cdata. As an unconditional jump at script 
                        // start would be useless to compile this should be a fact
                        //
                        // if a small jump is used here, all cdata records could be encoded using 1 byte for length
                        // the interpretors would need to track it t
                        // 
                        return BlastError.success; 
                    }
            }

            // if max > codelength then that is wierd 
            if (max >= codelength)
            {
                return BlastError.error;
            }

            // count the number of cdata records 
            int j = i;
            int c = 0;
            while (j < max)
            {
                if (!IsValidCDATAStart(code[j]))
                {
                    return BlastError.error;
                }
                int length = (code[j + 1] << 8) + code[j + 2];
                j = j + length + 3;
                c++;
            }
            // we should end directly after max 
            if (j != max + 1)
            {
                return BlastError.error; 
            }

            // alloc memory for a description of each 
            cdata_count = c;
            cdata_records = (CDATAREC*)UnsafeUtils.Malloc(sizeof(CDATAREC) * cdata_count, 8, allocator);

            // get information for each cdata section and return it 
            j = i;
            c = 0; 
            while (j < max)
            {
                int length = (code[j + 1] << 8) + code[j + 2];

                cdata_records[c].encoding = (CDATAEncodingType)code[j];
                cdata_records[c].length = (ushort)length;
                cdata_records[c].id = (ushort)c;
                cdata_records[c].index = (ushort)j;

                j = j + length + 3;
                c++; 
            }

            return BlastError.success; 
        }


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
        static internal int map_cdata_int([NoAlias] byte* code, in int offset_into_codebuffer)
        {
            int i = default;
            byte* p = (byte*)(void*)&i;

            p[3] = code[offset_into_codebuffer + 0];
            p[2] = code[offset_into_codebuffer + 1];
            p[1] = code[offset_into_codebuffer + 2];
            p[0] = code[offset_into_codebuffer + 3];

            return i;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static internal short map_cdata_short([NoAlias] byte* code, in int offset_into_codebuffer)
        {
            short i = default;
            byte* p = (byte*)(void*)&i;

            p[1] = code[offset_into_codebuffer + 0];
            p[0] = code[offset_into_codebuffer + 1];

            return i;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static internal int map_cdata_int32([NoAlias] byte* code, in int offset_into_codebuffer)
        {
            return map_cdata_int(code, offset_into_codebuffer);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static internal int map_cdata_16_int32([NoAlias] byte* code, in int offset_into_codebuffer)
        {                  
            short s = default;
            byte* p = (byte*)(void*)&s;

            p[0] = code[offset_into_codebuffer + 0];
            p[1] = code[offset_into_codebuffer + 1];
            return (int)s;
        }



        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static internal float map_cdata_16_float32([NoAlias] byte* code, in int offset_into_codebuffer)
        {
            half f = default;
            byte* p = (byte*)(void*)&f;

            p[0] = code[offset_into_codebuffer + 0];
            p[1] = code[offset_into_codebuffer + 1];
            return (float)f;
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
        static internal int map_cdata_s8_int32([NoAlias] byte* code, in int offset_into_codebuffer)
        {
            return (int)((sbyte*)(void*)&code[offset_into_codebuffer])[0];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static internal float map_cdata_fp8_float32([NoAlias] byte* code, in int offset_into_codebuffer)
        {
            byte b = code[offset_into_codebuffer];
            sbyte s = ((sbyte*)(void*)&b)[0];
            return (float)(s / 10f);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static internal float map_cdata_i16_float32([NoAlias] byte* code, in int offset_into_codebuffer)
        {
            return (float)map_cdata_short(code, offset_into_codebuffer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static internal float map_cdata_i32_float32([NoAlias] byte* code, in int offset_into_codebuffer)
        {
            return (float)map_cdata_int(code, offset_into_codebuffer); 
        }



        /// <summary>
        /// determine if the cdata block starts with a valid opcode 
        /// </summary>
        /// <param name="code">the opcode the block starts with</param>
        /// <returns></returns>
        static internal bool IsValidCDATAStart(byte code)
        {
            return IsValidCDATAStart((CDATAEncodingType)code); 
        }
        static internal bool IsValidCDATAStart(CDATAEncodingType code)
        {
            switch (code)
            {
                case CDATAEncodingType.CDATA:
                case CDATAEncodingType.ASCII: 

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
        /// write a float to a cdata index according to cdata encoding 
        /// </summary>
        /// <param name="code">code segment pointer</param>
        /// <param name="cdata_reference">the index of the initial reference</param>
        /// <param name="index">the element index into the cdata array</param>
        /// <param name="constant">the constant float value to set</param>
        /// <returns></returns>
        static public BlastError WriteCDATA([NoAlias]byte* code,  int cdata_reference, int index, float constant)
        {
            CDATAEncodingType encoding = (CDATAEncodingType)code[cdata_reference]; 

            switch (encoding)
            {

                case CDATAEncodingType.fp16_fp32:
                case CDATAEncodingType.fp32_fp32:
                case CDATAEncodingType.fp8_fp32:
                case CDATAEncodingType.u8_fp32:
                case CDATAEncodingType.s8_fp32: break;

                default: 
                case CDATAEncodingType.bool32:
                case CDATAEncodingType.i8_i32: 
                case CDATAEncodingType.i16_i32: 
                case CDATAEncodingType.i32_i32: 
                case CDATAEncodingType.ASCII:
                case CDATAEncodingType.CDATA:  return BlastError.error_cdata_datatype_mismatch; 
            }

            // get length, validate it
            int size = GetCDATAElementCount(code, cdata_reference); 
            if (index >= size)
            {
                if (IsTrace)
                {
                    Debug.LogError($"blast.cdata.write_cdata(float): failed to read cdata at {cdata_reference}, index {index} is out of bounds, cdata bytesize = {size}, elementcount = {GetCDATAContentByteSize(code, cdata_reference)}");
                }
                return BlastError.error_cdata_indexer_out_of_bounds;
            }

            set_cdata_float(code, cdata_reference, index, constant);

            return BlastError.success;
        }


        /// <summary>
        /// write an int32 to a cdata index according to cdata encoding 
        /// </summary>
        /// <param name="code">code segment pointer</param>
        /// <param name="cdata_reference">the index of the initial reference</param>
        /// <param name="index">the element index into the cdata array</param>
        /// <param name="constant">the constant integer value to set</param>
        /// <returns></returns>
        static public BlastError WriteCDATA([NoAlias] byte* code, int cdata_reference, int index, int constant)
        {
            CDATAEncodingType encoding = (CDATAEncodingType)code[cdata_reference];

            switch (encoding)
            {

                case CDATAEncodingType.ASCII:
                case CDATAEncodingType.CDATA:
                case CDATAEncodingType.fp16_fp32:
                case CDATAEncodingType.fp32_fp32:
                case CDATAEncodingType.fp8_fp32:
                case CDATAEncodingType.u8_fp32:
                case CDATAEncodingType.s8_fp32: 
                default:  return BlastError.error_cdata_datatype_mismatch; 

                case CDATAEncodingType.bool32:
                case CDATAEncodingType.i8_i32:
                case CDATAEncodingType.i16_i32:
                case CDATAEncodingType.i32_i32: break;
            }

            // get length, validate it
            int size = GetCDATAElementCount(code, cdata_reference);
            if (index >= size)
            {
                if (IsTrace)
                {
                    Debug.LogError($"blast.cdata.write_cdata(int): failed to read cdata at {cdata_reference}, index {index} is out of bounds, cdata bytesize = {size}, elementcount = {GetCDATAContentByteSize(code, cdata_reference)}");
                }
                return BlastError.error_cdata_indexer_out_of_bounds;
            }

            set_cdata_int(code, cdata_reference, index, constant);

            return BlastError.success;
        }


        /// <summary>
        /// write a byte to a cdata index according to cdata encoding 
        /// </summary>
        /// <param name="code">code segment pointer</param>
        /// <param name="cdata_reference">the index of the initial reference</param>
        /// <param name="index">the element index into the cdata array</param>
        /// <param name="constant">the constant byte value to set</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public BlastError WriteCDATA([NoAlias] byte* code, int cdata_reference, int index, byte constant)
        {
            CDATAEncodingType encoding = (CDATAEncodingType)code[cdata_reference];
           
            // check we find a valid encoding
            switch (encoding)
            {
                case CDATAEncodingType.CDATA:
                case CDATAEncodingType.ASCII:
                case CDATAEncodingType.fp16_fp32:
                case CDATAEncodingType.fp32_fp32:
                case CDATAEncodingType.fp8_fp32:
                case CDATAEncodingType.u8_fp32:
                case CDATAEncodingType.s8_fp32:
                case CDATAEncodingType.bool32:
                case CDATAEncodingType.i8_i32:
                case CDATAEncodingType.i16_i32:
                case CDATAEncodingType.i32_i32: break; // ok
                default:
                    if (IsTrace)
                    {
                        Debug.LogError($"blast.cdata: failed to read cdata reference at {cdata_reference}");
                    }
                    return BlastError.error_failed_to_read_cdata_reference;
            }

            // get byte length, validate it
            int size = GetCDATAContentByteSize(code, cdata_reference);
            if (index >= size)
            {
                if (IsTrace)
                {
                    Debug.LogError($"blast.cdata.write_cdata(byte): failed to read cdata at {cdata_reference}, index {index} is out of bounds, cdata bytesize = {size}");
                }
                return BlastError.error_cdata_indexer_out_of_bounds;
            }

            // set it 
            set_cdata_byte(code, cdata_reference, index, constant);

            return BlastError.success;
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
                case CDATAEncodingType.ASCII:
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
                case "half":
                case "fp16_fp32": return CDATAEncodingType.fp16_fp32;
                case "fp32fp32":
                case "float":
                case "fp32_fp32": return CDATAEncodingType.fp32_fp32;
                case "fp8fp32":
                case "fp8_fp32": return CDATAEncodingType.fp8_fp32;
                case "u8fp32":
                case "u8_fp32": return CDATAEncodingType.u8_fp32;
                case "s8fp32":
                case "s8_fp32": return CDATAEncodingType.s8_fp32;


                // int 32 and its backing
                case "i8i32":
                case "byte":
                case "i8_i32": return CDATAEncodingType.i8_i32;
                case "i16i32":
                case "short":
                case "i16_i32": return CDATAEncodingType.i16_i32;
                case "id":
                case "i32i32":
                case "int":
                case "i32_i32": return CDATAEncodingType.i32_i32;

                case "bool32":
                case "b32": return CDATAEncodingType.bool32;

                case "ascii":
                case "text":
                case "string": return CDATAEncodingType.ASCII;
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

                case CDATAEncodingType.ASCII: 
                case CDATAEncodingType.s8_fp32:
                case CDATAEncodingType.u8_fp32:
                case CDATAEncodingType.fp8_fp32:
                case CDATAEncodingType.i8_i32: return 1; 

                case CDATAEncodingType.fp16_fp32:
                case CDATAEncodingType.i16_i32: return 2;
            }
        }


        static internal BlastError reinterpret_cdata(byte* code, int offset, int index, int byte_length, ref float4 f4_backing_field, out BlastVariableDataType datatype, out byte vector_size)
        {
            // read cdata value and fill backing memory in bitform of type:   float or int|bool
            int bytes_per_element;
            int max_i = offset + 3 + byte_length;

            CDATAEncodingType type = (CDATAEncodingType)code[offset];
            switch (type)
            {
                case CDATAEncodingType.ASCII: 
                default:
#if TRACE
                    Debug.LogError($"blast.cdata.reinterpret_cdata: unsupported encoding type accessing cdata at offset: {offset}, index: {index}, length: {byte_length}, encoding: {type}");
#endif 
                    datatype = BlastVariableDataType.Numeric;
                    vector_size = 0; 
                    return BlastError.error_unsupported_cdata_encoding;

                case CDATAEncodingType.fp32_fp32:
                    {
                        bytes_per_element = 4;

                        // calc index                         
                        int i = offset + 3 + index * bytes_per_element;

                        // check bounds 
                        if (IsTrace)
                            if (i >= max_i - (bytes_per_element - 1) || i < offset + 3) goto OUT_OF_BOUNDS;

                        f4_backing_field.x = map_cdata_float32(code, i);
                        vector_size = 1;
                        datatype = BlastVariableDataType.Numeric;
                    }
                    return BlastError.success;

                case CDATAEncodingType.fp16_fp32:
                    {
                        bytes_per_element = 2;
                        int i = offset + 3 + index * bytes_per_element;

                        if (IsTrace)
                            if (i >= max_i - (bytes_per_element - 1) || i < offset + 3) goto OUT_OF_BOUNDS;

                        f4_backing_field.x = map_cdata_16_float32(code, i);
                        vector_size = 1;
                        datatype = BlastVariableDataType.Numeric; 
                    }
                    return BlastError.success;

                case CDATAEncodingType.fp8_fp32:
                    {
                        bytes_per_element = 1;
                        int i = offset + 3 + index * bytes_per_element;

                        if (IsTrace)
                            if (i >= max_i - (bytes_per_element - 1) || i < offset + 3) goto OUT_OF_BOUNDS;

                        f4_backing_field.x = map_cdata_fp8_float32(code, i);
                        vector_size = 1;
                        datatype = BlastVariableDataType.Numeric;
                    }
                    return BlastError.success;
                                

                case CDATAEncodingType.u8_fp32:
                    {
                        bytes_per_element = 1;
                        int i = offset + 3 + index * bytes_per_element;

                        if (IsTrace)
                            if (i >= max_i - (bytes_per_element - 1) || i < offset + 3) goto OUT_OF_BOUNDS;
                        
                        f4_backing_field.x = map_cdata_u8_float32(code, i);
                        vector_size = 1;
                        datatype = BlastVariableDataType.Numeric;
                    }
                    return BlastError.success;

                case CDATAEncodingType.s8_fp32:
                    {
                        bytes_per_element = 1;
                        int i = offset + 3 + index * bytes_per_element;

                        if (IsTrace)
                            if (i >= max_i - (bytes_per_element - 1) || i < offset + 3) goto OUT_OF_BOUNDS;
                        
                        f4_backing_field.x = map_cdata_s8_float32(code, i);
                        vector_size = 1;
                        datatype = BlastVariableDataType.Numeric;
                    }
                    return BlastError.success;

                case CDATAEncodingType.i32_i32:
                    {
                        bytes_per_element = 4;
                        int i = offset + 3 + index * bytes_per_element;

                        if (IsTrace)
                            if (i >= max_i - (bytes_per_element - 1) || i < offset + 3) goto OUT_OF_BOUNDS;

                        unsafe
                        {
                            fixed (float4* p4 = &f4_backing_field)
                            {
                                ((int*)(void*)p4)[0] = map_cdata_int32(code, i);
                            }
                        }
                        vector_size = 1;
                        datatype = BlastVariableDataType.ID;
                    }
                    return BlastError.success;

                case CDATAEncodingType.i16_i32:
                    {
                        bytes_per_element = 2;
                        int i = offset + 3 + index * bytes_per_element;

                        if (IsTrace)
                            if (i >= max_i - (bytes_per_element - 1) || i < offset + 3) goto OUT_OF_BOUNDS;
                                                 // 1 nul teveel?
                        unsafe
                        {
                            fixed (float4* p4 = &f4_backing_field)
                            {
                                ((int*)(void*)p4)[0] = map_cdata_16_int32(code, i);
                            }
                        }
                        vector_size = 1;
                        datatype = BlastVariableDataType.ID;
                    }
                    return BlastError.success;

                case CDATAEncodingType.i8_i32:
                    {
                        bytes_per_element = 1;
                        int i = offset + 3 + index * bytes_per_element;

                        if (IsTrace)
                            if (i >= max_i - (bytes_per_element - 1) || i < offset + 3) goto OUT_OF_BOUNDS;

                        unsafe
                        {
                            fixed (float4* p4 = &f4_backing_field)
                            {
                                ((int*)(void*)p4)[0] = map_cdata_s8_int32(code, i);
                            }
                        }
                        vector_size = 1;
                        datatype = BlastVariableDataType.ID;
                    }
                    return BlastError.success;
            }

        OUT_OF_BOUNDS:
            datatype = BlastVariableDataType.Numeric;
            vector_size = 0;
            if (IsTrace)
            {
                Debug.LogError($"blast.cdata.reinterpret_cdata: cdata index out of bounds at offset: {offset}, index: {index}, length: {byte_length}, encoding: {type}");
            }
            return BlastError.error_cdata_indexer_out_of_bounds; 
        }


        /// <summary>
        /// get a float from cdata[index], raises errors on datatype mismatch 
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static internal float index_cdata_f1([NoAlias] byte* code, in int offset, in int index, in int byte_length)
        {
            int bytes_per_element;
            int max_i = offset + 3 + byte_length;

            CDATAEncodingType type = (CDATAEncodingType)code[offset];
            switch (type)
            {
                default:
#if TRACE
                    Debug.LogError($"blast.cdata.index_cdata_f1: unsupported encoding type accessing cdata as float[1] at offset: {offset}, index: {index}, length: {byte_length}, encoding: {type}");
                    break;
#endif 

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


                case CDATAEncodingType.ASCII: 
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


        /// <summary>
        /// this works the same as index_cdata_f1 but also works on integer data by casting the integer into the float datatype 
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static internal float index_cdata_as_f1([NoAlias] byte* code, in int offset, in int index, in int byte_length)
        {
            int bytes_per_element;
            int max_i = offset + 3 + byte_length;

            CDATAEncodingType type = (CDATAEncodingType)code[offset];
            switch (type)
            {
                default:
#if TRACE
                    Debug.LogError($"blast.cdata.index_cdata_f1: unsupported encoding type accessing cdata as float[1] at offset: {offset}, index: {index}, length: {byte_length}, encoding: {type}");
                    break;
#endif 

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


                case CDATAEncodingType.ASCII:
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

                case CDATAEncodingType.i8_i32:
                    {
                        bytes_per_element = 1;
                        int i = offset + 3 + index * bytes_per_element;
                        if (i >= max_i - (bytes_per_element - 1) || i < offset + 3) break;
                        // a signed byte == i8
                        return map_cdata_s8_float32(code, i);
                    }

                case CDATAEncodingType.i16_i32:
                    {
                        bytes_per_element = 1;
                        int i = offset + 3 + index * bytes_per_element;
                        if (i >= max_i - (bytes_per_element - 1) || i < offset + 3) break;
                        return map_cdata_i16_float32(code, i);
                    }

                case CDATAEncodingType.i32_i32:
                    {
                        bytes_per_element = 1;
                        int i = offset + 3 + index * bytes_per_element;
                        if (i >= max_i - (bytes_per_element - 1) || i < offset + 3) break;
                        return map_cdata_i32_float32(code, i);
                    }
            }

            if (IsTrace)
            {
                Debug.LogError($"Blast.Interpretor.index_cdata_f1: index [{index}] out of bounds for cdata at {offset} of bytesize: {byte_length} ");
            }
            return float.NaN;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static internal void set_cdata_byte([NoAlias] byte* code, in int offset_into_codebuffer, int index, byte b)
        {
            code[offset_into_codebuffer + index + 3] = b;
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


                case CDATAEncodingType.ASCII:
                case CDATAEncodingType.i8_i32:  // writes float to integer backing
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
                        sbyte s = (sbyte)(f * 10f);
                        int offset = offset_into_codebuffer + index + 3; // 1 byte/element 
                        code[offset + 0] = ((byte*)(void*)&s)[0];
                    }
                    return;

                case CDATAEncodingType.fp16_fp32:
                    {
                        // float16 backed set with float
                        half h = new half(f); 
                        byte* p = (byte*)(void*)&h;
                        int offset = offset_into_codebuffer + (index * 2) + 3; // half == 2 bytes 
                        code[offset + 0] = p[0];
                        code[offset + 1] = p[1];
                    }
                    return;

                case CDATAEncodingType.i16_i32:  // writes float to integer backing
                    {
                        // short backed set with float
                        short s = (short)f;
                        byte* p = (byte*)(void*)&s;
                        int offset = offset_into_codebuffer + (index * 2) + 3; // half == 2 bytes 
                        code[offset + 0] = p[0];
                        code[offset + 1] = p[1];
                    }
                    return;

                case CDATAEncodingType.i32_i32:  // writes float to integer backing
                    {
                        // short backed set with float
                        int i = (int)f;
                        byte* p = (byte*)(void*)&i;
                        int offset = offset_into_codebuffer + (index * 4) + 3; // int = 4 bytes 
                        code[offset + 0] = p[0];
                        code[offset + 1] = p[1];
                        code[offset + 2] = p[2];
                        code[offset + 3] = p[3];
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


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static internal void set_cdata_int([NoAlias] byte* code, in int offset_into_codebuffer, int index, int i)
        {
            CDATAEncodingType encoding = (CDATAEncodingType)code[offset_into_codebuffer];
            switch (encoding)
            {
                case CDATAEncodingType.CDATA:  // we force any cdata reaching here to map to 32 bit integers 

                case CDATAEncodingType.i32_i32:
                    {
                        // cdata, int32 backed
                        byte* p = (byte*)(void*)&i;
                        int offset = offset_into_codebuffer + (index * 4) + 3; // float == 4 bytes 

                        code[offset + 0] = p[0];
                        code[offset + 1] = p[1];
                        code[offset + 2] = p[2];
                        code[offset + 3] = p[3];
                    }
                    return;

                case CDATAEncodingType.i8_i32:
                    {
                        // unsigned byte as backing: 0 through 255
                        byte b = (byte)i;
                        int offset = offset_into_codebuffer + index + 3; // 1 byte/element 
                        code[offset + 0] = b;
                    }
                    return;

                    // backed by signed short
                case CDATAEncodingType.i16_i32:
                    {
                        short s = (short)i; 
                        byte* p = (byte*)(void*)&s;
                        int offset = offset_into_codebuffer + (index * 2) + 3; // short == 4 bytes 

                        code[offset + 0] = p[0];
                        code[offset + 1] = p[1];
                    }
                    return;
            }

            if (IsTrace)
            {
                Debug.LogError($"Blast.Interpretor.set_cdata_float: failed to identify backing type ({encoding}) of cdata object at {offset_into_codebuffer}, indexed at {index}");
            }
        }






    }




}
#pragma warning restore CS1591
