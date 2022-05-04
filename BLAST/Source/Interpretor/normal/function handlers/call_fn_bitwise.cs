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
    using System.Reflection;
#else
    using UnityEngine;
    using Unity.Burst.CompilerServices;
#endif


using Unity.Burst;
using Unity.Mathematics;

using V1 = System.Single;
using V2 = Unity.Mathematics.float2;
using V3 = Unity.Mathematics.float3;
using V4 = Unity.Mathematics.float4;

namespace NSS.Blast.Interpretor
{
    unsafe public partial struct BlastInterpretor
    {


        #region Bitwise operations 


        /// <summary>
        /// Note: sets bit in place, first parameter must be a direct data index (compiler should force this)
        /// </summary>
        void get_setbit_result(ref int code_pointer, ref byte vector_size)
        {
            code_pointer++;
            int dataindex = code[code_pointer] - opt_id;
            code_pointer++;

#if DEVELOPMENT_BUILD || TRACE
            // dataindex must point to a valid id 
            if (dataindex < 0 || dataindex + opt_id >= 255)
            {
                Debug.LogError($"set_bit: first parameter must directly point to a dataindex but its '{dataindex + opt_id}' instead");
                return;
            }
#endif
            // TODO could support up to 128 bits packed in a float4   up to compiler to allow this in functiondefinition
            // vector_size = GetMetaDataSize(metadata, (byte)dataindex);

            // the bit to set is either 1 or 0
            int index;
            uint value;

#if DEVELOPMENT_BUILD || TRACE

            BlastVariableDataType datatype;
            bool neg1, neg2;

            // 
            // TODO: could pack into 1 value with additional compiler support
            // - 1 bit for value, 5 bits for index
            // 
            index = (int)((float*)pop_p_info(ref code_pointer, out datatype, out vector_size, out neg1))[0];

            if (vector_size != 1)
            {
                Debug.LogError($"set_bit: the index to set may only be of vectorsize 1 p1 = {datatype}.{vector_size}");
                return;
            }

            value = (uint)((float*)pop_p_info(ref code_pointer, out datatype, out vector_size, out neg2))[0];

            if (vector_size != 1)
            {
                Debug.LogError($"set_bit: the value to set may only be of vectorsize 1 p1 = {datatype}.{vector_size}");
                return;
            }

#else
            // dont check anything in release
            index = (int)pop_as_i1(ref code_pointer);
            value = (uint)pop_as_f1(ref code_pointer);
            vector_size = 1;
#endif

            uint current = ((uint*)data)[dataindex];
            uint mask = (uint)(1 << (byte)index);

            current = math.select((uint)(current | mask), (uint)(current & ~mask), value == 0);
            ((uint*)data)[dataindex] = current;

            // only set_bit & setbits force datatype change
            SetMetaData(metadata, BlastVariableDataType.Bool32, 1, (byte)dataindex);
        }

        void get_getbit_result(ref int code_pointer, ref byte vector_size, out float4 f4)
        {
            code_pointer++;
            int dataindex = code[code_pointer] - opt_id;
            code_pointer++;

#if DEVELOPMENT_BUILD || TRACE
            // dataindex must point to a valid id 
            if (dataindex < 0 || dataindex + opt_id >= 255)
            {
                f4 = float.NaN;
                Debug.LogError($"get_bit: first parameter must directly point to a dataindex but its '{dataindex + opt_id}' instead");
                return;
            }

#endif
            // TODO could support up to 128 bits packed in a float4   up to compiler to allow this in functiondefinition
            // vector_size = GetMetaDataSize(metadata, (byte)dataindex);

            // the bit to set is either 1 or 0
            int index;

#if DEVELOPMENT_BUILD || TRACE

            BlastVariableDataType datatype;
            bool neg1;

            // 
            // TODO: could pack into 1 value with additional compiler support
            // - 1 bit for value, 5 bits for index
            // 
            index = (int)((float*)pop_p_info(ref code_pointer, out datatype, out vector_size, out neg1))[0];

            if (vector_size != 1)
            {
                f4 = float.NaN;
                Debug.LogError($"get_bit: the index to check may only be of vectorsize 1 p1 = {datatype}.{vector_size}");
                return;
            }
#else
            // dont check anything in release
            index = (int)pop_as_f1(ref code_pointer);
            vector_size = 1;
#endif

            uint mask = (uint)(1 << (byte)index);

            f4 = (((uint*)data)[dataindex] & mask) == mask ? 1 : 0;
        }


        /// <summary>
        /// Note: sets bit in place, first parameter must be a direct data index (compiler should force this)
        /// </summary>
        void get_setbits_result(ref int code_pointer, ref byte vector_size)
        {
            code_pointer++;
            int dataindex = code[code_pointer] - opt_id;
            code_pointer++;

#if DEVELOPMENT_BUILD || TRACE
            // dataindex must point to a valid id 
            if (dataindex < 0 || dataindex + opt_id >= 255)
            {
                Debug.LogError($"set_bits: first parameter must directly point to a dataindex but its '{dataindex + opt_id}' instead");
                return;
            }

#endif
            // TODO could support up to 128 bits packed in a float4   up to compiler to allow this in functiondefinition
            // vector_size = GetMetaDataSize(metadata, (byte)dataindex);

            // the bit to set is either 1 or 0
            uint mask;
            uint value;

#if DEVELOPMENT_BUILD || TRACE

            BlastVariableDataType datatype;
            bool neg1;

            // 
            // TODO: could pack into 1 value with additional compiler support
            // - 1 bit for value, 5 bits for index
            // 
            mask = (uint)((float*)pop_p_info(ref code_pointer, out datatype, out vector_size, out neg1))[0];
            if (neg1)
            {
                ((float*)&mask)[0] = -((float*)&mask)[0];
            }

            if (vector_size != 1)
            {
                Debug.LogError($"set_bits: the mask to set may only be of vectorsize 1 p1 = {datatype}.{vector_size}");
                return;
            }

            value = (uint)((float*)pop_p_info(ref code_pointer, out datatype, out vector_size, out neg1))[0];
            if (neg1)
            {
                ((float*)&value)[0] = -((float*)&value)[0];
            }

            if (vector_size != 1)
            {
                Debug.LogError($"set_bits: the value to set may only be of vectorsize 1 p1 = {datatype}.{vector_size}");
                return;
            }
#else
            // dont check anything in release
            mask = (uint)pop_as_f1(ref code_pointer);
            value = (uint)pop_as_f1(ref code_pointer);
            vector_size = 1;
#endif

            uint current = ((uint*)data)[dataindex];
            current = math.select((uint)(current | mask), (uint)(current & ~mask), value == 0);
            ((uint*)data)[dataindex] = current;

            // only set_bit & setbits force datatype change
            SetMetaData(metadata, BlastVariableDataType.Bool32, 1, (byte)dataindex);
        }

        void get_getbits_result(ref int code_pointer, ref byte vector_size, out float4 f4)
        {
            code_pointer++;
            int dataindex = code[code_pointer] - opt_id;
            code_pointer++;

#if DEVELOPMENT_BUILD || TRACE
            // dataindex must point to a valid id 
            if (dataindex < 0 || dataindex + opt_id >= 255)
            {
                f4 = float.NaN;
                Debug.LogError($"get_bits: first parameter must directly point to a dataindex but its '{dataindex + opt_id}' instead");
                return;
            }

#endif
            // TODO could support up to 128 bits packed in a float4   up to compiler to allow this in functiondefinition
            // vector_size = GetMetaDataSize(metadata, (byte)dataindex);

            // the bit to set is either 1 or 0
            int mask;

#if DEVELOPMENT_BUILD || TRACE

            BlastVariableDataType datatype;
            bool neg1;

            // 
            // TODO: could pack into 1 value with additional compiler support
            // - 1 bit for value, 5 bits for index
            // 
            mask = (int)((float*)pop_p_info(ref code_pointer, out datatype, out vector_size, out neg1))[0];
            if (neg1)
            {
                ((float*)&mask)[0] = -((float*)&mask)[0];
            }

            if (vector_size != 1)
            {
                f4 = float.NaN;
                Debug.LogError($"get_bits: the mask to check may only be of vectorsize 1 p1 = {datatype}.{vector_size}");
                return;
            }

#else
            // dont check anything in release
            mask = pop_as_i1(ref code_pointer);
            vector_size = 1;
#endif

            f4 = (((uint*)data)[dataindex] & mask) == mask ? 1 : 0;
        }


        /// <summary>
        /// Note: updates data in place, first parameter must be a direct data index (compiler should force this)
        /// </summary>
        void get_ror_result(ref int code_pointer, ref byte vector_size)
        {
            code_pointer++;
            int dataindex = code[code_pointer] - opt_id;
            code_pointer++;

#if DEVELOPMENT_BUILD || TRACE
            // dataindex must point to a valid id 
            if (dataindex < 0 || dataindex + opt_id >= 255)
            {
                Debug.LogError($"ror: first parameter must directly point to a dataindex but its '{dataindex + opt_id}' instead");
                return;
            }

#endif

            // the bit to set is either 1 or 0
            int amount;

#if DEVELOPMENT_BUILD || TRACE

            BlastVariableDataType datatype;
            bool negate;

            // 
            // TODO: could pack into 1 value with additional compiler support
            // - 1 bit for value, 5 bits for index
            // 
            amount = (int)((float*)pop_p_info(ref code_pointer, out datatype, out vector_size, out negate))[0];
            amount = math.select(amount, -amount, negate);

            if (vector_size != 1)
            {
                Debug.LogError($"ror: to amount to rotate may only be of vectorsize 1 p1 = {datatype}.{vector_size}");
                return;
            }

#else
            // dont check anything in release
            amount = pop_as_i1(ref code_pointer);
            vector_size = 1;
#endif

            ((uint*)data)[dataindex] = math.ror(((uint*)data)[dataindex], amount);
        }


        /// <summary>
        /// Note: updates data in place, first parameter must be a direct data index (compiler should force this)
        /// </summary>
        void get_rol_result(ref int code_pointer, ref byte vector_size)
        {
            code_pointer++;
            int dataindex = code[code_pointer] - opt_id;
            code_pointer++;

            // the bit to set is either 1 or 0
            int amount = pop_as_i1(ref code_pointer);
            vector_size = 1;

            ((uint*)data)[dataindex] = math.rol(((uint*)data)[dataindex], amount);
        }


        /// <summary>
        /// Note: updates data in place, first parameter must be a direct data index (compiler should force this)
        /// </summary>
        void get_shl_result(ref int code_pointer, ref byte vector_size)
        {
            code_pointer++;
            int dataindex = code[code_pointer] - opt_id;
            code_pointer++;

            // the bit to set is either 1 or 0
            int amount = pop_as_i1(ref code_pointer);
            vector_size = 1;

            ((uint*)data)[dataindex] = ((uint*)data)[dataindex] << amount;
        }


        /// <summary>
        /// Note: updates data in place, first parameter must be a direct data index (compiler should force this)
        /// </summary>
        void get_shr_result(ref int code_pointer, ref byte vector_size)
        {
            code_pointer++;
            int dataindex = code[code_pointer] - opt_id;
            code_pointer++;

#if DEVELOPMENT_BUILD || TRACE
            // dataindex must point to a valid id 
            if (dataindex < 0 || dataindex + opt_id >= 255)
            {
                Debug.LogError($"shr: first parameter must directly point to a dataindex but its '{dataindex + opt_id}' instead");
                return;
            }
#endif

            // the bit to set is either 1 or 0
            int amount = pop_as_i1(ref code_pointer);
            vector_size = 1;

            ((uint*)data)[dataindex] = ((uint*)data)[dataindex] >> amount;
        }


        /// <summary>
        /// Note: updates data in place, first parameter must be a direct data index (compiler should force this)
        /// </summary>
        void get_reversebits_result(ref int code_pointer, ref byte vector_size)
        {
            code_pointer++;
            int dataindex = code[code_pointer] - opt_id;
            code_pointer++;

#if DEVELOPMENT_BUILD || TRACE
            // dataindex must point to a valid id 
            if (dataindex < 0 || dataindex + opt_id >= 255)
            {
                Debug.LogError($"reverse_bits: first parameter must directly point to a dataindex but its '{dataindex + opt_id}' instead");
                return;
            }

#endif
            ((uint*)data)[dataindex] = math.reversebits(((uint*)data)[dataindex]);
            vector_size = 1;
        }

        void get_countbits_result(ref int code_pointer, ref byte vector_size, out float4 f4)
        {
            code_pointer++;
            int dataindex = code[code_pointer] - opt_id;
            code_pointer++;

#if DEVELOPMENT_BUILD || TRACE
            // dataindex must point to a valid id 
            if (dataindex < 0 || dataindex + opt_id >= 255)
            {
                Debug.LogError($"count_bits: first parameter must directly point to a dataindex but its '{dataindex + opt_id}' instead");
                f4 = float.NaN;
                return;
            }

#endif
            f4 = (float4)(int4)math.countbits(((uint*)data)[dataindex]); // not setting f.x because we would be forced to write f4 = nan first to get it compiled because of undefined output
            vector_size = 1;
        }

        void get_lzcnt_result(ref int code_pointer, ref byte vector_size, out float4 f4)
        {
            code_pointer++;
            int dataindex = code[code_pointer] - opt_id;
            code_pointer++;

#if DEVELOPMENT_BUILD || TRACE
            // dataindex must point to a valid id 
            if (dataindex < 0 || dataindex + opt_id >= 255)
            {
                Debug.LogError($"lzcnt: first parameter must directly point to a dataindex but its '{dataindex + opt_id}' instead");
                f4 = float.NaN;
                return;
            }

#endif
            f4 = (float4)(int4)math.lzcnt(((uint*)data)[dataindex]); // not setting f.x because we would be forced to write f4 = nan first to get it compiled because of undefined output
            vector_size = 1;
        }
        void get_tzcnt_result(ref int code_pointer, ref byte vector_size, out float4 f4)
        {
            code_pointer++;
            int dataindex = code[code_pointer] - opt_id;
            code_pointer++;

#if DEVELOPMENT_BUILD || TRACE
            // dataindex must point to a valid id 
            if (dataindex < 0 || dataindex + opt_id >= 255)
            {
                Debug.LogError($"tzcnt: first parameter must directly point to a dataindex but its '{dataindex + opt_id}' instead");
                f4 = float.NaN;
                return;
            }

#endif
            f4 = (float4)(int4)math.tzcnt(((uint*)data)[dataindex]); // not setting f.x because we would be forced to write f4 = nan first to get it compiled because of undefined output
            vector_size = 1;
        }




        #endregion






    }
}
