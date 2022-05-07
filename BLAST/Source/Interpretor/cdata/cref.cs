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

// #define DONT_TRUST_MYSELF

#if STANDALONE_VSBUILD
using NSS.Blast.Standalone;
    using System.Reflection;
#else
using UnityEngine;
    using Unity.Burst.CompilerServices;
#endif


using Unity.Burst;
using Unity.Mathematics;
using System.Runtime.CompilerServices;

namespace NSS.Blast.SSMD
{

    /// <summary>
    /// the constant data reference functions are not used in normal interpretor because of the additional branching
    /// in ssmd this doesnt matter much
    /// </summary>


    unsafe public partial struct BlastSSMDInterpretor
    {


        /// <summary>
        /// read a constant float1 from codestream
        /// - code_pointer MUST be on code[constant_f1]
        /// - code pointer ends 1 after last datapoint processed 
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public float read_constant_f1([NoAlias] byte* code, ref int code_pointer)
        {
#if TRACE
            if (code[code_pointer] != (byte)blast_operation.constant_f1)
            {
                Debug.LogError($"constant_f1: codepointer not correctly indexed at {code_pointer}"); 
            }
#endif
            float constant = ((float*)(void*)&code[code_pointer + 1])[0];

#if DONT_TRUST_MYSELF
            float constant2 = default;
            
            byte* p = (byte*)(void*)&constant2;
            {
                p[0] = code[code_pointer + 1];
                p[1] = code[code_pointer + 2];
                p[2] = code[code_pointer + 3];
                p[3] = code[code_pointer + 4];
            }

            UnityEngine.Assertions.Assert.IsTrue(constant2 == constant); 

#endif
            code_pointer += 5;

            return constant; 
        }

        /// <summary>
        /// read a constant float1 from codestream that is encoded in 2 bytes 
        /// - code_pointer MUST be on code[constant_f1]
        /// - code pointer ends 1 after last datapoint processed 
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public float read_constant_f1_h([NoAlias] byte* code, ref int code_pointer)
        {
#if TRACE
            if (code[code_pointer] != (byte)blast_operation.constant_f1_h)
            {
                Debug.LogError($"constant_f1_h: codepointer not correctly indexed at {code_pointer}"); 
            }
#endif
            float constant = default;
            byte* p = (byte*)(void*)&constant;
            {
                p[0] = 0;
                p[1] = 0;
                p[2] = code[code_pointer + 1];
                p[3] = code[code_pointer + 2];
            }
            code_pointer += 3;

            return constant;
        }

        /// <summary>
        /// follow and read a float from codestream 
        /// </summary>
        /// <param name="code"></param>
        /// <param name="reference_index"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public float read_constant_float([NoAlias] byte* code, in int reference_index)
        {
            float constant = default; 

            if (code[reference_index] == (byte)blast_operation.constant_f1)
            {
                constant = ((float*)(void*)&code[reference_index + 1])[0];

#if DONT_TRUST_MYSELF
            float constant2 = default;
            
            byte* p2 = (byte*)(void*)&constant2;
            {
                p2[0] = code[reference_index + 1];
                p2[1] = code[reference_index + 2];
                p2[2] = code[reference_index + 3];
                p2[3] = code[reference_index + 4];
            }

            UnityEngine.Assertions.Assert.IsTrue(constant2 == constant); 
#endif

            }
            else
            {
#if TRACE
                if (code[reference_index] != (byte)blast_operation.constant_f1_h)
                {
                    Debug.LogError($"blast.ssmd.read_constant_float: constant reference error, short constant reference points at invalid operation {code[reference_index]} at {reference_index}");
                    return float.NaN;
                }
#endif
                // 16 bit encoding
                byte* p = (byte*)(void*)&constant;
                p[0] = 0;
                p[1] = 0;
                p[2] = code[reference_index + 1];
                p[3] = code[reference_index + 2];
            }

            return constant;
        }



        /// <summary>
        /// follow a reference to a constant encoded in code
        /// - while this might seem expensive its much cheaper then adressing some memory not in cache,
        ///   remember, even the fastest ram need 7ns to get data where you need it, we intent to run scripts in less then 2ns so everything counts
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public bool follow_constant_ref([NoAlias] byte* code, ref int code_pointer, out BlastVariableDataType datatype, out int reference_index, bool is_short)
        {
#if TRACE
            if (code[code_pointer] != (is_short ? (byte)blast_operation.constant_short_ref : (byte)blast_operation.constant_long_ref))
            {
                Debug.LogError($"constant_f1_h: codepointer not correctly indexed at {code_pointer}, codepointer should be located at the reference opcode when calling this function");
            }
#endif
            if (is_short)
            {
                reference_index = (code_pointer + 1) - code[code_pointer + 1];
                code_pointer += 2;
            }
            else
            {
                reference_index = (code_pointer + 1) - ((code[code_pointer + 1] << 8) + code[code_pointer + 2]);
                code_pointer += 3;
            }

            // a smart compiler will generate a cmov|select from this... TODO Check this or combine with short branch
            switch ((blast_operation)code[reference_index])
            {

                case blast_operation.constant_f1:
                case blast_operation.constant_f1_h:
                    datatype = BlastVariableDataType.Numeric;
                    return true;

                case blast_operation.constant_i1:
                case blast_operation.constant_i1_h:
                    datatype = BlastVariableDataType.ID;
                    return true;

                default:
                    datatype = BlastVariableDataType.Numeric;
                    return false;
            }
        }
    }
}
