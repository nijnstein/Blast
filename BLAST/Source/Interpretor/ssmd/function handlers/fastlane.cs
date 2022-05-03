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


using NSS.Blast.Interpretor;
using Unity.Burst;
using Unity.Mathematics;

namespace NSS.Blast.SSMD
{
    unsafe public partial struct BlastSSMDInterpretor
    {


        #region Direct Assignments & Increments of constant|ID to target 

        private BlastError AssignDataFromConstant(ref int code_pointer, in float constant_value)
        {
            // get id of target 
            byte offset = (byte)code[code_pointer];
            code_pointer++;

            // the negation is combined into the offset
            // - we need to substract it from the offset on indexing the target in each negated switch case 
            bool substract = offset >= 0b1000_0000;
            offset = (byte)(offset & 0b0111_1111);

            BlastInterpretor.GetMetaData(in metadata, in offset, out byte vsize, out BlastVariableDataType dtype);


            // get the target dest
            vsize = (byte)math.select(vsize, 4, vsize == 0);
            DATAREC target = IndexData(offset, vsize);

            float value = math.select(constant_value, -constant_value, substract);

            switch (vsize)
            {
                case 1: simd.move_f1_constant_to_indexed_data_as_f1(target, value, ssmd_datacount); break;
                case 2: simd.move_f1_constant_to_indexed_data_as_f2(target, value, ssmd_datacount); break;
                case 3: simd.move_f1_constant_to_indexed_data_as_f3(target, value, ssmd_datacount); break;
                case 4: simd.move_f1_constant_to_indexed_data_as_f4(target, value, ssmd_datacount); break;
                default:
                    return BlastError.ssmd_error_unsupported_vector_size;
            }

            return BlastError.success;
        }

        private BlastError DirectTargetAssignment(ref int code_pointer, byte offset)
        {
            // setup target 
            BlastInterpretor.GetMetaData(in metadata, in offset, out byte vsize, out BlastVariableDataType dtype);
            vsize = (byte)math.select(vsize, 4, vsize == 0);

            bool substract = offset >= 0b1000_0000;
            offset = (byte)(offset & 0b0111_1111);

            DATAREC target = IndexData(offset, vsize);


            // identify source location 
            byte source_offset = (byte)code[code_pointer];
            code_pointer++;

            // 
            if (IsTrace)
            {
                BlastInterpretor.GetMetaData(in metadata, in offset, out byte vsource, out BlastVariableDataType stype);
                if (vsource != 1)
                {
                    Debug.LogError("Blast.Interpretor.ssmd.DirectTargetAssignment: compiler error, assignment of non v1");
                    return BlastError.ssmd_error_unsupported_vector_size;
                }
            }

            DATAREC source = IndexData(source_offset, 1);

            // currently compiler will only allow this on assignments of a v1 as it expands over the target 
            switch (math.select(vsize, vsize + 100, substract))
            {
                case 1: simd.move_indexed_f1(in target, in source, ssmd_datacount); break;
                case 2: simd.move_indexed_f1f2(in target, in source, ssmd_datacount); break;
                case 3: simd.move_indexed_f1f3(in target, in source, ssmd_datacount); break;
                case 4: simd.move_indexed_f1f4(in target, in source, ssmd_datacount); break;

                case 101: simd.move_indexed_f1_n(in target, in source, ssmd_datacount); break;
                case 102: simd.move_indexed_f1f2_negated(in target, in source, ssmd_datacount); break;
                case 103: simd.move_indexed_f1f3_negated(in target, in source, ssmd_datacount); break;
                case 104: simd.move_indexed_f1f4_negated(in target, in source, ssmd_datacount); break;

                default:
                    return BlastError.ssmd_error_unsupported_vector_size;
            }

            return BlastError.success;
        }

        /// <summary>
        /// increment id with constant value 
        /// </summary>
        public BlastError DirectlyIncrementData(ref int code_pointer, float constant_value)
        {
            byte offset = (byte)(code[code_pointer] - (byte)blast_operation.id);
            code_pointer++;

            // get target info 
            BlastInterpretor.GetMetaData(in metadata, in offset, out byte vsize, out BlastVariableDataType dtype);
            vsize = (byte)math.select(vsize, 4, vsize == 0);

            DATAREC target = IndexData(offset, vsize);

            switch (vsize)
            {
                case 1: simd.add_indexed_f1f1_constant(target, constant_value, ssmd_datacount); break;
                case 2: simd.add_indexed_f1f2_constant(target, constant_value, ssmd_datacount); break;
                case 3: simd.add_indexed_f1f3_constant(target, constant_value, ssmd_datacount); break;
                case 4: simd.add_indexed_f1f4_constant(target, constant_value, ssmd_datacount); break;
                default:
                    return BlastError.ssmd_error_unsupported_vector_size;
            }

            return BlastError.success;
        }

        /// <summary>
        /// multiply id with constant value 
        /// </summary>
        public BlastError DirectlyMultiplyData(ref int code_pointer, float constant_value)
        {
            byte offset = (byte)(code[code_pointer] - (byte)blast_operation.id);
            code_pointer++;

            // get target info 
            BlastInterpretor.GetMetaData(in metadata, in offset, out byte vsize, out BlastVariableDataType dtype);
            vsize = (byte)math.select(vsize, 4, vsize == 0);

            DATAREC target = IndexData(offset, vsize);

            switch (vsize)
            {
                case 1: simd.mul_indexed_f1f1_constant(target, constant_value, ssmd_datacount); break;
                case 2: simd.mul_indexed_f1f2_constant(target, constant_value, ssmd_datacount); break;
                case 3: simd.mul_indexed_f1f3_constant(target, constant_value, ssmd_datacount); break;
                case 4: simd.mul_indexed_f1f4_constant(target, constant_value, ssmd_datacount); break;
                default:
                    return BlastError.ssmd_error_unsupported_vector_size;
            }

            return BlastError.success;
        }

        /// <summary>
        /// divide id with constant value 
        /// </summary>
        public BlastError DirectlyDivideData(ref int code_pointer, float constant_value)
        {
            byte offset = (byte)(code[code_pointer] - (byte)blast_operation.id);
            code_pointer++;

            // get target info 
            BlastInterpretor.GetMetaData(in metadata, in offset, out byte vsize, out BlastVariableDataType dtype);
            vsize = (byte)math.select(vsize, 4, vsize == 0);

            DATAREC target = IndexData(offset, vsize);

            switch (vsize)
            {
                case 1: simd.div_indexed_f1f1_constant(target, constant_value, ssmd_datacount); break;
                case 2: simd.div_indexed_f1f2_constant(target, constant_value, ssmd_datacount); break;
                case 3: simd.div_indexed_f1f3_constant(target, constant_value, ssmd_datacount); break;
                case 4: simd.div_indexed_f1f4_constant(target, constant_value, ssmd_datacount); break;
                default:
                    return BlastError.ssmd_error_unsupported_vector_size;
            }

            return BlastError.success;
        }


        /// <summary>
        /// increment id with datapoint 
        /// </summary>
        public BlastError DirectlyHandleDataOperation(ref int code_pointer, blast_operation op_to_handle)
        {
            blast_operation op = (blast_operation)code[code_pointer];

            if (try_pop_constant(op, ref code_pointer, out float constant))
            {
                switch (op_to_handle)
                {
                    case blast_operation.suba:
                    case blast_operation.substract: return DirectlyIncrementData(ref code_pointer, -constant);

                    case blast_operation.adda:
                    case blast_operation.add: return DirectlyIncrementData(ref code_pointer, constant);

                    case blast_operation.multiply: return DirectlyMultiplyData(ref code_pointer, constant);

                    case blast_operation.divide: return DirectlyDivideData(ref code_pointer, constant);

                    default: return BlastError.error_unsupported_operation;
                }
            }
            else
            {
                // its a data id 
                byte offset = (byte)(op - blast_operation.id);
                code_pointer++;

                // get target info 
                BlastInterpretor.GetMetaData(in metadata, in offset, out byte vsize, out BlastVariableDataType dtype);
                vsize = (byte)math.select(vsize, 4, vsize == 0);

                DATAREC target = IndexData(offset, vsize);

                // get source location of inc/dec value 
                byte source_offset = (byte)code[code_pointer];
                code_pointer++;

                // 
                if (IsTrace)
                {
                    BlastInterpretor.GetMetaData(in metadata, in offset, out byte vsource, out BlastVariableDataType stype);
                    if (vsource != 1)
                    {
                        Debug.LogError($"Blast.Interpretor.ssmd.DirectlyHandleDataOperation: compiler error, assignment of non v1 with operation {op_to_handle}");
                    }
                }
                DATAREC source = IndexData(source_offset, 1);

                // switch on target vectorsize & op_to_handkle
                switch (op_to_handle)
                {
                    case blast_operation.adda:
                    case blast_operation.add:
                        {
                            switch (vsize)
                            {
                                case 1: simd.add_indexed_f1f1(target, source, ssmd_datacount); break;
                                case 2: simd.add_indexed_f1f2(target, source, ssmd_datacount); break;
                                case 3: simd.add_indexed_f1f3(target, source, ssmd_datacount); break;
                                case 4: simd.add_indexed_f1f4(target, source, ssmd_datacount); break;
                                default:
                                    return BlastError.ssmd_error_unsupported_vector_size;
                            }
                        }
                        break;

                    case blast_operation.suba:
                    case blast_operation.substract:
                        {
                            switch (vsize)
                            {
                                case 1: simd.sub_indexed_f1f1(target, source, ssmd_datacount); break;
                                case 2: simd.sub_indexed_f1f2(target, source, ssmd_datacount); break;
                                case 3: simd.sub_indexed_f1f3(target, source, ssmd_datacount); break;
                                case 4: simd.sub_indexed_f1f4(target, source, ssmd_datacount); break;
                                default:
                                    return BlastError.ssmd_error_unsupported_vector_size;
                            }
                        }
                        break;

                    case blast_operation.multiply:
                        {
                            switch (vsize)
                            {
                                case 1: simd.mul_indexed_f1f1(target, source, ssmd_datacount); break;
                                case 2: simd.mul_indexed_f1f2(target, source, ssmd_datacount); break;
                                case 3: simd.mul_indexed_f1f3(target, source, ssmd_datacount); break;
                                case 4: simd.mul_indexed_f1f4(target, source, ssmd_datacount); break;
                                default:
                                    return BlastError.ssmd_error_unsupported_vector_size;
                            }
                        }
                        break;

                    case blast_operation.divide:
                        {
                            switch (vsize)
                            {
                                case 1: simd.div_indexed_f1f1(target, source, ssmd_datacount); break;
                                case 2: simd.div_indexed_f1f2(target, source, ssmd_datacount); break;
                                case 3: simd.div_indexed_f1f3(target, source, ssmd_datacount); break;
                                case 4: simd.div_indexed_f1f4(target, source, ssmd_datacount); break;
                                default:
                                    return BlastError.ssmd_error_unsupported_vector_size;
                            }
                        }
                        break;
                }
            }
            return BlastError.success;
        }

        #endregion 






    }
}
