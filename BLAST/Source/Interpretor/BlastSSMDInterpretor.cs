//############################################################################################################################
// BLAST v1.0.4c                                                                                                             #
// Copyright © 2022 Rob Lemmens | NijnStein Software <rob.lemmens.s31 gmail com> All Rights Reserved                   ^__^\ #
// Unauthorized copying of this file, via any medium is strictly prohibited proprietary and confidential               (oo)\ #
//                                                                                                                     (__)  #
//############################################################################################################################

#pragma warning disable CS0162   // disable warnings for paths not taken dueue to compiler defines 

#define TRACE

#if STANDALONE_VSBUILD
using NSS.Blast.Standalone;
using System.Reflection;
#else
using UnityEngine;
using Unity.Burst.CompilerServices;
#endif


using NSS.Blast.Interpretor;
using System;
using UnityEngine.Assertions;
using Unity.Burst;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using Unity.Collections;
using System.Runtime.CompilerServices;

namespace NSS.Blast.SSMD
{

    /// <summary>
    /// The SSMD Interpretor 
    /// </summary>
    [BurstCompile]
    unsafe public partial struct BlastSSMDInterpretor
    {
        #region Constant Compiletime Defines 

        /// <summary>
        /// true if compiled in devmode 
        /// </summary>
#if DEVELOPMENT_BUILD
        public const bool IsDevelopmentBuild = true;
#else
        public const bool IsDevelopmentBuild = false;
#endif
        /// <summary>
        /// TRUE if compiled with TRACE define enabled
        /// </summary>
#if TRACE
        public const bool IsTrace = true;
#else
        public const bool IsTrace = false;
#endif
        /// <summary>
        /// Warning code: this means that the elements in the dataarray given to process doenst have all 
        /// data segments evenly spaced, as such some optimizations are not possible 
        /// </summary>
        public const byte WARN_SSMD_DATASEGMENTS_NOT_EVENLY_SPACED = 0;


        /// <summary>
        /// Warning code: this means that the stacksegments are not evenly spaced in the given datasegment array
        /// CompilerOptions.PackageStack can be set to false, resulting in volatile thread local stack that is 
        /// always packed regardless the datasegment spacing, this can be a big optimization so warn 
        /// </summary>
        public const byte WARN_SSMD_STACKSEGMENTS_NOT_EVENLY_SPACED = 1;


        /// <summary>
        /// Warning code: this means that eh given ssmd record is not divisable by 4, when all data is aligned and
        /// the record count is a multiple of 4 then additional optimizations are possible. 
        /// </summary>
        public const byte WARN_SSMD_RECORD_COUNT_NOT_DIV4 = 2;



        #endregion 

        #region Internal Data 
        internal BlastPackageData package;

        internal int code_pointer;
        internal int stack_offset;

        /// <summary>
        /// pointer to engine data
        /// </summary>
        [NoAlias]
        [NativeDisableUnsafePtrRestriction]
        internal unsafe BlastEngineData* engine_ptr;

        /// <summary>
        /// optional pointer to environment data
        /// </summary>
        [NoAlias]
        [NativeDisableUnsafePtrRestriction]
        internal unsafe IntPtr environment_ptr;

        /// <summary>
        /// code segment pointer
        /// </summary>
        [NoAlias]
        [NativeDisableUnsafePtrRestriction]
        internal unsafe byte* code;

        /// <summary>
        /// data segment pointer
        /// - for now each data element is 4 bytes long FIXED
        /// </summary>
        [NoAlias]
        [NativeDisableUnsafePtrRestriction]
        internal unsafe void** data;
        internal int ssmd_datacount;

        /// <summary>
        /// temp buffer for indices (ssmd_count * 4byte)
        /// </summary>
        [NoAlias]
        [NativeDisableUnsafePtrRestriction]
        internal unsafe float* indices;    // TODO change to ushorts

        [NoAlias]
        [NativeDisableUnsafePtrRestriction]
        internal unsafe void** stack;


        [NoAlias]
        [NativeDisableUnsafePtrRestriction]
        internal unsafe float4* register;

        /// <summary>
        /// metadata segment pointer
        /// </summary>
        [NoAlias]
        [NativeDisableUnsafePtrRestriction]
        internal unsafe byte* metadata;


        /// <summary>
        /// sync buffer(s)
        /// </summary>
        /// <remarks>
        /// Sync buffers are composed of 1 bit for each datastream and level of nesting, every jump in ssmd mode 
        /// generates a new sync buffer level and these may nest until some maximum level <cref>syncbuffer_depth</cref> 
        /// 
        /// </remarks>        
        [NoAlias]
        [NativeDisableUnsafePtrRestriction]
        internal unsafe byte* syncbits;

        /// <summary>
        /// depth of syncbuffer 
        /// </summary>
        const byte syncbuffer_depth = 0;

        /// <summary>
        /// current maximum synclevel
        /// </summary>
        internal byte syncbuffer_level;

        /// <summary>
        /// a random number generetor set once and if needed updated by scripts with the seed instruction
        /// </summary>
        internal Unity.Mathematics.Random random;

        /// <summary>
        /// if true, the script is executed in validation mode:
        /// - external calls just return 0's
        /// </summary>
        public bool ValidateOnce;
        public int MaxStackSizeReachedDuringValidation;

        // 
        // gather alignment information of stack and data 
        // - if aligned -> space between records is equal 
        // 
        private int stack_rowsize;     // rowsize == recordsize + stride
        private int data_rowsize;      // note that row size might be negative

        private bool stack_is_aligned;
        private bool data_is_aligned;

        #endregion

        #region DATAREC index record  

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal DATAREC IndexData(int index, byte vectorsize)
        {
            return new DATAREC() {
                data = data,
                row_size = data_rowsize,
                index = index,
                is_aligned = data_is_aligned,
                is_constant = false,
                vectorsize = vectorsize,
                datatype = BlastVariableDataType.Numeric
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal DATAREC IndexStack(int index, byte vectorsize)
        {
            return new DATAREC() {
                data = stack,
                row_size = stack_rowsize,
                index = index,
                is_aligned = stack_is_aligned,
                is_constant = false,
                vectorsize = vectorsize,
                datatype = BlastVariableDataType.Numeric
            };
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float f1([NoAlias] void** data, in int source_index, in int i)
        {
            return ((float*)data[i])[source_index];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float2 f2([NoAlias] void** data, in int source_index, in int i)
        {
            return ((float2*)(void*)&((float*)data[i])[source_index])[0];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float3 f3([NoAlias] void** data, in int source_index, in int i)
        {
            return ((float3*)(void*)&((float*)data[i])[source_index])[0];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float4 f4([NoAlias] void** data, in int source_index, in int i)
        {
            return ((float4*)(void*)&((float*)data[i])[source_index])[0];
        }

        #endregion

        #region Warnings

        /// <summary>
        /// reset any warning counters, blast wil sent most warnings only once
        /// </summary>
        public void ResetWarnings()
        {
            if (engine_ptr != null)
            {
                engine_ptr->warnings_shown_once = 0;
            }
        }

        /// <summary>
        /// disable showing a specifik warning 
        /// </summary>
        public void DisableWarning(byte warning)
        {
            if (engine_ptr != null)
            {
                ((Bool32*)&engine_ptr->warnings_shown_once)->SetBitSafe(warning, true);
            }
        }

        /// <summary>
        /// true is given warning was shown already
        /// </summary>
        public bool ShownWarning(byte warning)
        {
            if (engine_ptr != null)
            {
                return Bool32.From(engine_ptr->warnings_shown_once).GetBit(warning);
            }
            else return false;
        }

        /// <summary>
        /// show a warning once 
        /// </summary>
        public bool ShowWarningOnce(byte warning)
        {
            if (!ShownWarning(warning))
            {
                DisableWarning(warning);
                switch (warning)
                {
                    case BlastSSMDInterpretor.WARN_SSMD_DATASEGMENTS_NOT_EVENLY_SPACED:
                        {
                            Debug.LogWarning("Blast.SSMD.Interpretor: data segments not equally spaced, this will cost some performance");
                        }
                        break;
                    case BlastSSMDInterpretor.WARN_SSMD_STACKSEGMENTS_NOT_EVENLY_SPACED:
                        {
                            Debug.LogWarning("Blast.SSMD.Interpretor: stack segments not equally spaced, this will cost some performance");
                        }
                        break;
                    case BlastSSMDInterpretor.WARN_SSMD_RECORD_COUNT_NOT_DIV4:
                        {
                            Debug.LogWarning("Blast.SSMD.Interpretor: ssmd count not divisable by 4, this will cost some performance");
                        }
                        break;
                }
                return true;
            }
            return false;
        }

        #endregion

        #region Public SetPackage & Execute 

        public BlastPackageData CurrentPackage => package;

        public int CurrentSSMDDataSize => package.SSMDDataSize;

        public int CurrentCodeSize => package.CodeSize;

        /// <summary>
        /// set ssmd package data 
        /// </summary>
        public BlastError SetPackage(in BlastPackageData pkg)
        {
            ValidateOnce = false;

            if (pkg.PackageMode != BlastPackageMode.SSMD)
            {
                if (pkg.LanguageVersion == BlastLanguageVersion.BSSMD1 && pkg.PackageMode == BlastPackageMode.Compiler)
                {
                    ValidateOnce = true;
                }
                else
                {
                    return BlastError.ssmd_invalid_packagemode;
                }
            }

            package = pkg;

            code = package.Code;
            data = null;
            metadata = package.Metadata;

            return BlastError.success;
        }

        public BlastError SetPackage(BlastPackageData pkg, byte* _code, byte* _metadata)
        {
            ValidateOnce = false;

            if (pkg.PackageMode != BlastPackageMode.SSMD)
            {
                if (pkg.LanguageVersion == BlastLanguageVersion.BSSMD1 && pkg.PackageMode == BlastPackageMode.Compiler)
                {
                    ValidateOnce = true;
                }
                else
                {
                    return BlastError.ssmd_invalid_packagemode;
                }
            }

            package = pkg;

            code = _code;
            data = null;
            metadata = _metadata;

            return BlastError.success;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="packagedata"></param>
        /// <param name="blast"></param>
        /// <param name="environment"></param>
        /// <param name="ssmddata"></param>
        /// <param name="ssmd_data_count"></param>
        /// <returns></returns>
        public int Execute(in BlastPackageData packagedata, [NoAlias] BlastEngineDataPtr blast, [NoAlias] IntPtr environment, [NoAlias] BlastSSMDDataStack* ssmddata, int ssmd_data_count, bool is_referenced = false)
        {
            BlastError res = SetPackage(packagedata);
            if (res != BlastError.success) return (int)res;

            return Execute(blast, environment, ssmddata, ssmd_data_count, is_referenced);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="blast"></param>
        /// <param name="environment"></param>
        /// <param name="ssmddata"></param>
        /// <param name="ssmd_data_count"></param>
        /// <returns></returns>
#if !STANDALONE_VSBUILD
        [SkipLocalsInit]
#endif
        public int Execute([NoAlias] BlastEngineDataPtr blast, [NoAlias] IntPtr environment, [NoAlias] BlastSSMDDataStack* ssmddata, int ssmd_data_count, bool is_referenced = false)
        {
            if (!blast.IsCreated) return (int)BlastError.error_blast_not_initialized;

            if (ssmddata == null && package.SSMDDataSize > 0)
            {
#if DEVELOPMENT_BUILD || TRACE
                Debug.LogError("blast.ssmd.interpretor: data error, ssmd data == null");
#endif
                return (int)BlastError.error_execute_ssmddata_null;
            }
            if (code == null)
            {
#if DEVELOPMENT_BUILD || TRACE
                Debug.LogError("blast.ssmd.interpretor: package not correctly set, code == null");
#endif
                return (int)BlastError.error_execute_package_not_correctly_set;
            }
            if (metadata == null)
            {
#if DEVELOPMENT_BUILD || TRACE
                Debug.LogError("blast.ssmd.interpretor: package not correctly set, metadata == null");
#endif
                return (int)BlastError.error_execute_package_not_correctly_set;
            }
            if (package.PackageMode != BlastPackageMode.Compiler && IntPtr.Zero == package.CodeSegmentPtr)
            {
#if DEVELOPMENT_BUILD || TRACE
                Debug.LogError("blast.ssmd.interpretor: package not correctly set, codesegmentptr == null");
#endif
                return (int)BlastError.error_execute_package_not_correctly_set;
            }

            // set shared environment 
            engine_ptr = blast.Data;
            environment_ptr = environment;

            // setup sync buffer 
            syncbuffer_level = 0;
            byte* syncbuffer = null; // stackalloc byte[ssmd_datacount];

            // a register for each of max vectorsize
            float4* register = stackalloc float4[ssmd_data_count];

            if (!is_referenced)
            {
                // generate a list of datasegment pointers on the stack:
                byte** datastack = stackalloc byte*[ssmd_data_count];
                int datastack_stride = package.SSMDDataSize;

                // although we allocate everything in 1 block, we will index a pointer to every block,
                // this will allow us to accept non-block aligned lists of memory pointers 


                byte* ptr = (byte*)ssmddata;
                for (int i = 0; i < ssmd_data_count; i++)
                {
                    datastack[i] = &ptr[datastack_stride * i]; // ((byte*)ssmddata) + (datastack_stride * i);
                }

                // execute 
                return Execute(syncbuffer, datastack, ssmd_data_count, register);
            }
            else
            {
                if (ValidateOnce)
                {
                    return (int)BlastError.error_ssmd_validationmode_cannot_run_on_referenced_data;
                }

                return Execute(syncbuffer, (byte**)(void*)(ssmddata), ssmd_data_count, register);
            }
        }

        #endregion

        #region Stack Functions 
        /// <summary>
        /// push register[n].x as float
        /// </summary>
        void push_register_f1()
        {
            // update metadata with type set 
            SetStackMetaData(BlastVariableDataType.Numeric, 1, (byte)stack_offset);

            // move the data 
            simd.move_f4_array_to_indexed_data_as_f1(stack, stack_rowsize, stack_is_aligned, stack_offset, register, ssmd_datacount);

            // advance 1 element in stack offset 
            stack_offset += 1;
        }

        /// <summary>
        /// push data value on stack
        /// </summary>
        /// <param name="index">index of data fto lookup</param>
        void push_data_f1(int index)
        {
            // update metadata with type set 
            SetStackMetaData(BlastVariableDataType.Numeric, 1, (byte)stack_offset);
            // move indexed f1 from data to stack 
            simd.move_indexed_f1(stack, stack_rowsize, stack_is_aligned, stack_offset, data, data_rowsize, data_is_aligned, index, ssmd_datacount); 
            // advance 1 element in stack offset 
            stack_offset += 1;
        }

        /// <summary>
        /// push register[n].xy as float2 onto stack
        /// </summary>
        void push_register_f2()
        {
            SetStackMetaData(BlastVariableDataType.Numeric, 2, (byte)stack_offset);
            // stack[][].xy = register[].xy
            simd.move_f4_array_to_indexed_data_as_f2(stack, stack_rowsize, stack_is_aligned, stack_offset, register, ssmd_datacount); 
            stack_offset += 2;
        }

        /// <summary>
        /// push register[n].xyz as float3 onto stack
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void push_register_f3()
        {
            SetStackMetaData(BlastVariableDataType.Numeric, 3, (byte)stack_offset);
            // stack[][].xyz = register[].xyz
            simd.move_f4_array_to_indexed_data_as_f3(stack, stack_rowsize, stack_is_aligned, stack_offset, register, ssmd_datacount);
            stack_offset += 3;   
        }

        /// <summary>
        /// push register[n] as float4 onto stack
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void push_register_f4()
        {
            SetStackMetaData(BlastVariableDataType.Numeric, 4, (byte)stack_offset);
            simd.move_f4_array_to_indexed_data_as_f4(stack, stack_rowsize, stack_is_aligned, stack_offset, register, ssmd_datacount);
            stack_offset += 4;  
        }

        /// <summary>
        /// push a float1 value on te stack retrieved via 1 pop 
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void push_f1_pop_1_ref([NoAlias] void* temp, ref int code_pointer)
        {
            push_pop_f(temp, ref code_pointer, 1);
        }

        /// <summary>
        /// push a float2 value on te stack retrieved via 2x 1 pop 
        /// - each element may have a diferent origen, we need to account for that 
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void push_f2_pop_11([NoAlias] void* temp, ref int code_pointer)
        {
            push_pop_f(temp, ref code_pointer, 2);
        }

        /// <summary>
        /// push an float3 value on te stack retrieved via 3x 1 pop 
        /// - each element may have a diferent origen, we need to account for that 
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void push_f3_pop_111([NoAlias] void* temp, ref int code_pointer)
        {
            push_pop_f(temp, ref code_pointer, 3);
        }

        /// <summary>
        /// push an float4 value on te stack retrieved via 4x 1 pop 
        /// - each element may have a diferent origen, we need to account for that 
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void push_f4_pop_1111([NoAlias] void* temp, ref int code_pointer)
        {
            push_pop_f(temp, ref code_pointer, 4);
        }

        /// <summary>
        /// returns true if indexed
        /// </summary>
        int pop_op_meta_cp(int codepointer, out BlastVariableDataType datatype, out byte vector_size)
        {
            blast_operation operation = (blast_operation)code[codepointer];

            int indexer = -1;

            // skip any minus and indexer when reading metadata 
            while (true)
            {
                //
                // switch(enum) => the compiler should convert this into a jump table resulting in a single jump instruction
                // 
                switch (operation)
                {
                    case blast_operation.index_n:
                        indexer = -1;
                        goto case blast_operation.substract;

                    case blast_operation.index_w:
                        indexer = 3;
                        goto case blast_operation.substract;

                    case blast_operation.index_x:
                        indexer = 0;
                        goto case blast_operation.substract;

                    case blast_operation.index_y:
                        indexer = 1;
                        goto case blast_operation.substract;

                    case blast_operation.index_z:
                        indexer = 2;
                        goto case blast_operation.substract;

                    case blast_operation.substract:
                    case blast_operation.not:
                        codepointer++;
                        operation = (blast_operation)code[codepointer];
                        break;

                    case blast_operation.pop:
                        GetStackMetaData(metadata, (byte)stack_offset, out vector_size, out datatype);
                        return indexer;

                    case blast_operation.constant_f1_h:
                    case blast_operation.constant_long_ref:
                    case blast_operation.constant_short_ref:
                    case blast_operation.constant_f1:
                    case blast_operation.cdataref:
                        datatype = BlastVariableDataType.Numeric;
                        vector_size = 1;
                        return indexer;


                    default:
                        {
                            if ((byte)operation >= BlastInterpretor.opt_id)
                            {
                                datatype = BlastInterpretor.GetMetaDataType(metadata, (byte)(operation - BlastInterpretor.opt_id));
                                vector_size = BlastInterpretor.GetMetaDataSize(metadata, (byte)(operation - BlastInterpretor.opt_id));
                            }
                            else
                            if ((byte)operation >= BlastInterpretor.opt_value)
                            {
                                datatype = BlastVariableDataType.Numeric;
                                vector_size = 1;
                            }
                            else
                            {
#if DEVELOPMENT_BUILD || TRACE
                                Debug.LogError($"pop_op_meta _cp-> operation {operation} not supported at codepointer: {codepointer}");
#endif
                                datatype = BlastVariableDataType.Numeric;
                                vector_size = 1;
                            }
                        }
                        return indexer;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void GetStackMetaData(byte* metadata, int stack_offset, out byte vector_size, out BlastVariableDataType datatype)
        {
            int metadata_offset = stack_offset - 1 + math.select(package.O3 >> 2, 0, package.HasStackPackaged);

            BlastInterpretor.GetMetaData(metadata, (byte)metadata_offset, out vector_size, out datatype);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        BlastVariableDataType GetStackMetaDataType(int stack_offset)
        {
            int metadata_offset = stack_offset - 1 + math.select(package.O3 >> 2, 0, package.HasStackPackaged);
            return BlastInterpretor.GetMetaDataType(metadata, (byte)metadata_offset);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        byte GetStackMetaDataSize(int stack_offset)
        {
            int metadata_offset = stack_offset - 1 + math.select(package.O3 >> 2, 0, package.HasStackPackaged);
            return BlastInterpretor.GetMetaDataSize(metadata, (byte)metadata_offset);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void SetStackMetaData(in BlastVariableDataType datatype, in int vectorsize, byte stack_offset)
        {
            int metadata_offset = stack_offset + math.select(package.O3 >> 2, 0, package.HasStackPackaged);
            BlastInterpretor.SetMetaData(metadata, in datatype, (byte)vectorsize, (byte)metadata_offset);

        }


        #endregion

        #region Debug/Validation handlers 
        /// <summary>
        /// 
        /// </summary>
        void RunDebugStack()
        {
#if DEVELOPMENT_BUILD || TRACE
            if (ValidateOnce) return;

            int i = 0, ssmdi = 0;

            if (package.O3 > 0)
            {
                Debug.Log($"\nDATA Segment   ({package.O3} bytes) ");
                for (; i < package.O3 >> 2;)
                {
                    i = DebugDisplayVector(i, ssmdi, data, metadata);
                }
            }
            else
            {
                Debug.Log($"\nDATA Segment   (0 bytes) - no data from the datasegment is used by this package");
            }

            if (package.StackSize > 0)
            {
                Debug.Log($"\nSTACK Segment   ({(package.O4 - package.O3)} bytes)");
                for (i = 0; i < (package.O4 - package.O3 - 1) >> 2;)
                {
                    i = DebugDisplayVector(i, ssmdi, stack, metadata);
                }
            }
            else
            {
                Debug.Log($"\nSTACK Segment   (0 bytes) - no stack memory is used by this package");
            }
#endif
        }

        /// <summary>
        /// get 2 parameters from the code and raise an error if they are not equal 
        /// !! no error is raised if not in debug !!
        /// </summary>
        void RunValidate(ref int code_pointer, [NoAlias] void* temp, [NoAlias] void* temp2)
        {
            // read the 2 parameters, we can use temp and register
            BlastVariableDataType type1, type2;
            byte size1, size2;

            int indexer = pop_op_meta_cp(code_pointer, out type1, out size1);
            bool is_indexed = indexer >= 0;

            size1 = (byte)math.select(size1, 4, size1 == 0);

            if (is_indexed)
            {
                float* f1 = (float*)temp;

                // we know its indexed in advance, datasize is allocated accordingly
                pop_fx_into_ref<float>(ref code_pointer, f1);

                // it has to be size 1 or indexed to size 1                            
                int indexer2 = pop_op_meta_cp(code_pointer, out type2, out size2);

                float* f2 = &f1[ssmd_datacount];
                pop_fx_into_ref<float>(ref code_pointer, f2);
#if DEVELOPMENT_BUILD || TRACE
                for (int i = 0; i < ssmd_datacount; i++)
                {
                    if (f1[i] != f2[i]) Debug.LogError($"blast.ssmd.validate: validation error at ssmd index: {i}, {f1[i]} != {f2[i]}");
                }
#endif
            }
            else
            {
                switch (size1)
                {
                    // on size 1 all should be 1 and no indexers 
                    case 1:
                        {
                            float* f1 = (float*)temp;
                            float* f2 = &f1[ssmd_datacount];

                            pop_fx_into_ref<float>(ref code_pointer, f1);
#if DEVELOPMENT_BUILD || TRACE
                            pop_op_meta_cp(code_pointer, out type2, out size2);
                            if (type1 != type2 || size1 != size2)
                            {
                                Debug.LogError($"blast.ssmd.validate: datatype mismatch, type1 = {type1}, size1 = {size1}, type2 = {type2}, size2 = {size2}");
                            }
#endif
                            // in debug raise error in runtime just read value and crash or whatever
                            pop_fx_into_ref<float>(ref code_pointer, f2);

#if DEVELOPMENT_BUILD || TRACE
                            for (int i = 0; i < ssmd_datacount; i++)
                            {
                                if (f1[i] != f2[i]) Debug.LogError($"blast.ssmd.validate: validation error at ssmd index: {i}, {f1[i]} != {f2[i]}");
                            }
#endif
                        }
                        break;

                    case 2:
                        {
                            float2* f21 = (float2*)temp;
                            pop_fx_into_ref<float2>(ref code_pointer, f21);

                            // first is size 2, next also must be size 2
                            int indexer2 = pop_op_meta_cp(code_pointer, out type2, out size2);

                            float2* f22 = &f21[ssmd_datacount];
                            pop_fx_into_ref<float2>(ref code_pointer, f22);
#if DEVELOPMENT_BUILD || TRACE
                            for (int i = 0; i < ssmd_datacount; i++)
                            {
                                if (math.any(f21[i] != f22[i])) Debug.LogError($"blast.ssmd.validate: validation error at ssmd index: {i}, {f21[i]} != {f22[i]}");
                            }
#endif
                        }
                        break;

                    case 3:
                        {
                            float3* f31 = (float3*)temp;
                            pop_fx_into_ref<float3>(ref code_pointer, f31);

                            int indexer2 = pop_op_meta_cp(code_pointer, out type2, out size2);

                            float3* f32 = (float3*)temp2;
                            pop_fx_into_ref<float3>(ref code_pointer, f32);

#if DEVELOPMENT_BUILD || TRACE
                            for (int i = 0; i < ssmd_datacount; i++)
                            {
                                if (math.any(f31[i] != f32[i])) Debug.LogError($"blast.ssmd.validate: validation error at ssmd index: {i}, {f31[i]} != {f32[i]}");
                            }
#endif
                        }
                        break;

                    case 0:
                    case 4:
                        {
                            float4* f41 = (float4*)temp;
                            pop_fx_into_ref<float4>(ref code_pointer, f41);

                            int indexer2 = pop_op_meta_cp(code_pointer, out type2, out size2);

                            float4* f42 = &f41[ssmd_datacount];
                            pop_fx_into_ref<float4>(ref code_pointer, f42);
#if DEVELOPMENT_BUILD || TRACE
                            for (int i = 0; i < ssmd_datacount; i++)
                            {
                                if (math.any(f41[i] != f42[i])) Debug.LogError($"blast.ssmd.validate: validation error at ssmd index: {i}, {f41[i]} != {f42[i]}");
                            }
#endif
                        }
                        break;
                }
            }
        }

        void RunDebug(int offset)
        {
#if DEVELOPMENT_BUILD || TRACE
            if (ValidateOnce) return;

            if (offset < 0 || offset > (package.O3 >> 2))
            {
                Debug.LogError($"DATA_SEGMENT[{offset}] = offset out of range");
            }
            else
            {
                BlastVariableDataType dt;
                byte size;
                BlastInterpretor.GetMetaData(metadata, (byte)offset, out size, out dt);




#if STANDALONE_VSBUILD
                switch (dt)
                {
                    case BlastVariableDataType.Numeric:
                        switch (size)
                        {
                            case 1:
                                Debug.Log($"DATA_SEGMENT[{offset}] = {((float*)data[0])[offset].ToString("0.000")}, {dt}[{size}]");
                                break;

                            case 2:
                                Debug.Log($"DATA_SEGMENT[{offset}] = {((float*)data[0])[offset].ToString("0.000")} {((float*)data[0])[offset + 1].ToString("0.000")}, {dt}[{size}]");
                                break;

                            case 3:
                                Debug.Log($"DATA_SEGMENT[{offset}] = {((float*)data[0])[offset].ToString("0.000")} {((float*)data[0])[offset + 1].ToString("0.000")} {((float*)data[0])[offset + 2].ToString("0.000")}, {dt}[{size}]");
                                break;

                            case 0:
                            case 4:
                                Debug.Log($"DATA_SEGMENT[{offset}] = {((float*)data[0])[offset].ToString("0.000")} {((float*)data[0])[offset + 1].ToString("0.000")} {((float*)data[0])[offset + 2].ToString("0.000")} {((float*)data[0])[offset + 3].ToString("0.000")}, {dt}[{size}]");
                                break;
                        }
                        break;

                    case BlastVariableDataType.Bool32:
                        Debug.Log($"DATA_SEGMENT[{offset}] = {CodeUtils.FormatBool32(((uint*)data[0])[offset])}, BOOL32");
                        break;
                }
            }
#else
                // reduced string magic for burstable code path
                switch (size)
                {
                    case 1:
                        Debug.Log($"DATA_SEGMENT[{offset}] = {((float*)data[0])[offset]}, {dt}[{size}]");
                        break;

                    case 2:
                        Debug.Log($"DATA_SEGMENT[{offset}] = {((float*)data[0])[offset]} {((float*)data[0])[offset + 1]}, {dt}[{size}]");
                        break;

                    case 3:
                        Debug.Log($"DATA_SEGMENT[{offset}] = {((float*)data[0])[offset]} {((float*)data[0])[offset + 1]} {((float*)data[0])[offset + 2]}, {dt}[{size}]");
                        break;

                    case 0:
                    case 4:
                        Debug.Log($"DATA_SEGMENT[{offset}] = {((float*)data[0])[offset]} {((float*)data[0])[offset + 1]} {((float*)data[0])[offset + 2]} {((float*)data[0])[offset + 3]}, {dt}[{size}]");
                        break;
                }
            }
#endif
#endif
        }

        static internal int DebugDisplayVector(int idata, int ssmdi, void** data, byte* metadata)
        {
#if DEVELOPMENT_BUILD || TRACE

            BlastVariableDataType dt = BlastInterpretor.GetMetaDataType(metadata, (byte)idata);
            int size = BlastInterpretor.GetMetaDataSize(metadata, (byte)idata);

            switch (dt)
            {
                case BlastVariableDataType.Numeric:
                    {
#if STANDALONE_VSBUILD
                        switch (size)
                        {
                            case 1:
                                Debug.Log($"Element: {idata.ToString().PadLeft(2)} = {(((float*)data[ssmdi])[idata]).ToString("0.0000").PadRight(40)} {dt}[{size}]");
                                return idata + 1;

                            case 0:
                            case 4:
                                Debug.Log($"Element: {idata.ToString().PadLeft(2)} = {(((float*)data[ssmdi])[idata]).ToString("0.0000").PadRight(10)}{(((float*)data[ssmdi])[idata + 1]).ToString("0.0000").PadRight(10)}{(((float*)data[ssmdi])[idata + 2]).ToString("0.0000").PadRight(10)}{(((float*)data[ssmdi])[idata + 3]).ToString("0.0000").PadRight(10)} {dt}[{size}]");
                                return idata + 4;

                            case 2:
                                Debug.Log($"Element: {idata.ToString().PadLeft(2)} = {(((float*)data[ssmdi])[idata]).ToString("0.0000").PadRight(10)}{(((float*)data[ssmdi])[idata + 1]).ToString("0.0000").PadRight(30)} {dt}[{size}]");
                                return idata + 2;

                            case 3:
                                Debug.Log($"Element: {idata.ToString().PadLeft(2)} = {(((float*)data[ssmdi])[idata]).ToString("0.0000").PadRight(10)}{(((float*)data[ssmdi])[idata + 1]).ToString("0.0000").PadRight(10)}{(((float*)data[ssmdi])[idata + 2]).ToString("0.0000").PadRight(20)} {dt}[{size}]");
                                return idata + 3;
                        }
#else
                        // less string magic for burstable code 
                        switch (size)
                        {
                            case 1:
                                Debug.Log($"Element: {idata} = {(((float*)data[ssmdi])[idata])} {dt}[{size}]");
                                return idata + 1;

                            case 0:
                            case 4:
                                Debug.Log($"Element: {idata} = {(((float*)data[ssmdi])[idata])}{(((float*)data[ssmdi])[idata + 1])}{(((float*)data[ssmdi])[idata + 2])}{(((float*)data[ssmdi])[idata + 3])} {dt}[{size}]");
                                return idata + 4;

                            case 2:
                                Debug.Log($"Element: {idata} = {(((float*)data[ssmdi])[idata])}{(((float*)data[ssmdi])[idata + 1])} {dt}[{size}]");
                                return idata + 2;

                            case 3:
                                Debug.Log($"Element: {idata} = {(((float*)data[ssmdi])[idata])}{(((float*)data[ssmdi])[idata + 1])}{(((float*)data[ssmdi])[idata + 2])} {dt}[{size}]");
                                return idata + 3;
                        }
#endif
                    }
                    break;


                case BlastVariableDataType.Bool32:
                    {
                        // vectorsize must be 1 
                        uint b32 = ((uint*)data[ssmdi])[idata];
#if STANDALONE_VSBUILD
                        Debug.Log($"Element: {idata.ToString().PadLeft(2)} = {CodeUtils.FormatBool32(b32)} BOOL32[1]");
#else
                        Debug.Log($"Element: {idata} = {b32} BOOL32[1]");
#endif
                    }
                    break;
            }
#endif
            return idata + 1;
        }
        #endregion

        #region IndexF1 



        /// <summary>
        /// get the result of index-n[consant|variable] for all ssmd records 
        /// </summary>

        BlastError IndexF1IntoFn([NoAlias] void* temp, ref int code_pointer, ref byte vector_size, int ssmd_datacount, [NoAlias] float4* register, in DATAREC target = default, int indexed = -1, bool is_negated = false)
        {
            // next is a cdata or a vector that we index
            // any constant vector index of 0..3 would have been translated into index_x..w

            blast_operation op = (blast_operation)code[code_pointer];

            int offset = 0;
            int length = 0;
            bool is_cdata = op == blast_operation.cdataref;
            CDATAEncodingType encoding = CDATAEncodingType.None; 


            // op may be: cdatref OR id of variable 
            if (is_cdata)
            {
                // follow the cdata ref 
                if (!BlastInterpretor.follow_cdataref(code, code_pointer, out offset, out length, out encoding))
                {
                    if (IsTrace)
                    {
                        Debug.LogError($"Blast.ssmd.IndexF1IntoFn: failed to read cdata reference at codepointer {code_pointer}");
                    }
                    return BlastError.error_failed_to_read_cdata_reference;
                }

                // advance to index parameter
                code_pointer += 3;
            }
            else
            if (op >= blast_operation.id && op < blast_operation.ex_op)
            {
                // data is in datasegment at given id 
                offset = op - blast_operation.id;
                BlastInterpretor.GetMetaData(metadata, (byte)offset, out vector_size, out BlastVariableDataType datatype);
                length = vector_size;

                // advance to index 
                code_pointer++;
            }
            else
            {
                // invalid data 
                if (IsTrace)
                {
                    Debug.LogError($"Blast.ssmd.IndexF1IntoFn: invalid indexing target, expecting a cdataref or vector in datasegment. codepointer = {code_pointer}");
                }
                return BlastError.invalid_index_n_target;
            }



            // source is in op, next is the actual index
            float* f1 = (float*)temp;
            bool constant_index = pop_fx_into_ref<float>(ref code_pointer, f1, ssmd_datacount, true);

            // move back 1 codepoint, pop leaves pointer at next value
            code_pointer--;

            // if the index was not constant by type, it very probably is constant data 
            if (!constant_index)
            {
                constant_index = simd.all_equal(f1, ssmd_datacount);
            }

            // less work if index is constant AND we reference constant data 
            if (constant_index)
            {
                int index = (int)f1[0];

                // get once, set many 
                if (is_cdata)
                {
                    float f1data = BlastInterpretor.index_cdata_f1(code, offset, index, length);
                    if (target.is_set)
                    {
                        // direct set on target
                        simd.move_f1_constant_to_indexed_data_as_f1(target, f1data, ssmd_datacount);
                    }
                    else
                    {
                        simd.move_f1_constant_into_f4_array_as_f1(register, f1data, ssmd_datacount);
                    }
                }
                else
                {
                    // set result in datasegment from other value in segment at offset[index]
                    DATAREC source = IndexData(offset + index, 1);

                    if (target.is_set)
                    {
                        // directly set target  (end of sequence, direct assign)
                        simd.move_indexed_f1(target, source, ssmd_datacount);
                    }
                    else
                    {
                        simd.move_indexed_data_as_f1_to_f1f4(register, source, ssmd_datacount);
                    }
                }
            }
            else
            {
                // move indexed data to target 
                if (target.is_set)
                {
                    // get many, set many, each index is unique
                    float* f2 = &f1[ssmd_datacount];

                    // fetch data for each index 
                    if (is_cdata)
                    {
                        // this is far from efficient.. 
                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            f2[i] = BlastInterpretor.index_cdata_f1(code, offset, (int)f1[i], length);
                        }
                    }
                    else
                    {
                        // index each element seperately
                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            f2[i] = ((float**)data)[i][offset + (int)f1[i]];
                        }
                    }

                    // direct set of assignment target
                    simd.move_f1_array_to_indexed_data_as_f1(target, f2, ssmd_datacount);
                }
                else
                {
                    // write data[index] to intermediate register as f4[].x
                    if (is_cdata)
                    {
                        // this is far from efficient.. 
                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            register[i].x = BlastInterpretor.index_cdata_f1(code, offset, (int)f1[i], length);
                        }
                    }
                    else
                    {
                        // index each element seperately
                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            register[i].x = ((float**)data)[i][offset + (int)f1[i]];
                        }
                    }
                }
            }

            // the value set after indexing something is always of vectorsize 1 
            vector_size = 1;
            return BlastError.success;
        }


        #endregion

        #region Expand fx 

        /// <summary>
        /// 
        /// - only grows codepointer on operations
        /// pop|data data[1] and expand into a vector of size n [2,3,4]
        /// </summary>
        BlastError ExpandF1IntoFn(in int expand_into_n, ref int code_pointer, ref byte vector_size, int ssmd_datacount, [NoAlias] float4* destination, in DATAREC target)
        {
            // we get either a pop instruction or an instruction to get a value 

            // 
            //  allow expand( - a.x ) ?? -> expand(substract(idx(id)))
            // 

            bool substract = false;
            int indexer = -1;
            vector_size = (byte)expand_into_n;

            while (code_pointer < package.CodeSize)
            {

                byte c = code[code_pointer];

                switch ((blast_operation)c)
                {
                    case blast_operation.substract: substract = true; code_pointer++; continue;
                    case blast_operation.index_x: indexer = 0; code_pointer++; continue;
                    case blast_operation.index_y: indexer = 1; code_pointer++; continue;
                    case blast_operation.index_z: indexer = 2; code_pointer++; continue;
                    case blast_operation.index_w: indexer = 3; code_pointer++; continue;

                    case blast_operation.pop:
                        {
#if DEVELOPMENT_BUILD || TRACE
                            BlastVariableDataType type;
                            byte size;
                            GetStackMetaData(metadata, (byte)stack_offset, out size, out type);

                            if (indexer < 0)
                            {
                                // if the value is not indexed, it should be of size 1 
                                if (size != 1 || type != BlastVariableDataType.Numeric)
                                {
                                    Debug.LogError($"blast.ssmd.expand_f1_into_fn -> stackdata mismatch, expecting numeric of size 1, found {type} of size {size} at stack offset {stack_offset}");
                                    return BlastError.error;
                                }
                            }
                            else
                            {
                                // indexed vector, index must be smaller then vectorsize 
                                if (indexer > size)
                                {
                                    Debug.LogError($"blast.ssmd.expand_f1_into_fn -> index {indexer} is out of bounds for vector of size {size} at stack offset {stack_offset}");
                                    return BlastError.error;
                                }
                            }
#endif
                            // stack pop 
                            stack_offset = stack_offset - 1;   // element size of value is always 1 if expanding

                            // if indexed, the source data has a different cardinality
                            int offset = stack_offset + math.select(0, indexer, indexer >= 0);

                            if (target.is_set)
                            {
                                if (target.data == stack)
                                {
                                    Debug.LogError("target self");
                                }
                                expand_indexed_into_n(expand_into_n, ssmd_datacount, target, substract, c, offset, stack, stack_is_aligned, stack_rowsize);
                            }
                            else
                            {
                                expand_indexed_into_n(expand_into_n, ssmd_datacount, destination, substract, c, offset, stack, stack_is_aligned, stack_rowsize);
                            }

                            // output vectorsize equals what we expanded into 
                            vector_size = (byte)expand_into_n;
                            return BlastError.success;
                        }


                    case blast_operation.constant_f1:
                        {
                            // handle operation with inlined constant data 
                            float constant = read_constant_f1(code, ref code_pointer);
                            code_pointer--; ; // function ends on this and should leave pointer at last processed token

                            constant = math.select(constant, -constant, substract);
                            if (target.is_set)
                            {
                                expand_constant_into_n(expand_into_n, ssmd_datacount, target, constant);
                            }
                            else
                            {
                                expand_constant_into_n(expand_into_n, ssmd_datacount, destination, constant);
                            }
                        }
                        return BlastError.success;

                    case blast_operation.constant_f1_h:
                        {
                            // handle operation with inlined constant data 
                            float constant = read_constant_f1_h(code, ref code_pointer);
                            code_pointer--; // function ends on this and should leave pointer at last processed token

                            constant = math.select(constant, -constant, substract);
                            if (target.is_set)
                            {
                                expand_constant_into_n(expand_into_n, ssmd_datacount, target, constant);
                            }
                            else
                            {
                                expand_constant_into_n(expand_into_n, ssmd_datacount, destination, constant);
                            }
                        }
                        return BlastError.success;

                    case blast_operation.constant_long_ref:
                    case blast_operation.constant_short_ref:
                        {
                            if (follow_constant_ref(code, ref code_pointer, out BlastVariableDataType datatype, out int reference_index, c == (byte)blast_operation.constant_short_ref))
                            {
                                float constant = read_constant_float(code, reference_index); 

                                code_pointer--;   // function ends on this and should leave pointer at last processed token
                                
                                constant = math.select(constant, -constant, substract);
                                if (target.is_set)
                                {
                                    expand_constant_into_n(expand_into_n, ssmd_datacount, target, constant);
                                }
                                else
                                {
                                    expand_constant_into_n(expand_into_n, ssmd_datacount, destination, constant);
                                }
                            }
                            else
                            {
                                return BlastError.error_constant_reference_error;  
                            }                                  
                        }
                        return BlastError.success;

                    case blast_operation.pi:
                    case blast_operation.inv_pi:
                    case blast_operation.epsilon:
                    case blast_operation.infinity:
                    case blast_operation.negative_infinity:
                    case blast_operation.nan:
                    case blast_operation.min_value:
                    case blast_operation.value_0:
                    case blast_operation.value_1:
                    case blast_operation.value_2:
                    case blast_operation.value_3:
                    case blast_operation.value_4:
                    case blast_operation.value_8:
                    case blast_operation.value_10:
                    case blast_operation.value_16:
                    case blast_operation.value_24:
                    case blast_operation.value_32:
                    case blast_operation.value_64:
                    case blast_operation.value_100:
                    case blast_operation.value_128:
                    case blast_operation.value_256:
                    case blast_operation.value_512:
                    case blast_operation.value_1000:
                    case blast_operation.value_1024:
                    case blast_operation.value_30:
                    case blast_operation.value_45:
                    case blast_operation.value_90:
                    case blast_operation.value_180:
                    case blast_operation.value_270:
                    case blast_operation.value_360:
                    case blast_operation.inv_value_2:
                    case blast_operation.inv_value_3:
                    case blast_operation.inv_value_4:
                    case blast_operation.inv_value_8:
                    case blast_operation.inv_value_10:
                    case blast_operation.inv_value_16:
                    case blast_operation.inv_value_24:
                    case blast_operation.inv_value_32:
                    case blast_operation.inv_value_64:
                    case blast_operation.inv_value_100:
                    case blast_operation.inv_value_128:
                    case blast_operation.inv_value_256:
                    case blast_operation.inv_value_512:
                    case blast_operation.inv_value_1000:
                    case blast_operation.inv_value_1024:
                    case blast_operation.inv_value_30:
                    case blast_operation.framecount:
                    case blast_operation.fixedtime:
                    case blast_operation.time:
                    case blast_operation.fixeddeltatime:
                    case blast_operation.deltatime:
                        //
                        // CONSTANT operation                         
                        //
                        {
                            float constant = engine_ptr->constants[c];
                            constant = math.select(constant, -constant, substract);

                            if (target.is_set)
                            {
                                expand_constant_into_n(expand_into_n, ssmd_datacount, target, constant);
                            }
                            else
                            {
                                expand_constant_into_n(expand_into_n, ssmd_datacount, destination, constant);
                            }
                        }
                        return BlastError.success;


                    default:
                    case blast_operation.id:
                        {
                            byte offset = (byte)(c - BlastInterpretor.opt_id);

#if DEVELOPMENT_BUILD || TRACE
                            // validate its a valid id in debug mode 
                            if (c < (byte)blast_operation.id || c == 255)
                            {
                                Debug.LogError($"blast.ssmd.stack.expand_f1_into_fn -> operation {c} not supported at codepointer {code_pointer}");
                                return BlastError.error;
                            }


                            BlastVariableDataType type = BlastInterpretor.GetMetaDataType(metadata, offset);
                            int size = BlastInterpretor.GetMetaDataSize(metadata, offset);

                            if (indexer >= 0)
                            {
                                if (size < indexer)
                                {
                                    Debug.LogError($"blast.ssmd.stack.expand_f1_into_fn -> indexer beyond vectorsize at codepointer {code_pointer}");
                                    return BlastError.error;
                                }
                            }
                            else
                            {
                                if (size != 1 || type != BlastVariableDataType.Numeric)
                                {
                                    Debug.LogError($"blast.ssmd.stack.expand_f1_into_fn -> data mismatch, expecting numeric of size 1, found {type} of size {size} at data offset {c - BlastInterpretor.opt_id}");
                                    return BlastError.error;
                                }
                            }
#endif

                            offset = (byte)math.select(offset, offset + indexer, indexer >= 0);

                            if (target.is_set)
                            {
                                expand_indexed_into_n(expand_into_n, ssmd_datacount, target, substract, c, offset, data, data_is_aligned, data_rowsize);
                            }
                            else
                            {
                                expand_indexed_into_n(expand_into_n, ssmd_datacount, destination, substract, c, offset, data, data_is_aligned, data_rowsize);
                            }
                        }
                        return BlastError.success;
                }
            }
            return BlastError.success;
        }


        static void expand_indexed_into_n(int expand_into_n, int ssmd_datacount, [NoAlias] float4* destination, bool substract, byte c, int offset, void** data, bool is_aligned, int stride)
        {
            switch (expand_into_n)
            {
                case 2:
                    if (!substract)
                    {
                        simd.move_indexed_data_as_f1_to_f2(destination, data, stride, is_aligned, offset, ssmd_datacount);
                    }
                    else
                    {
                        simd.move_indexed_data_as_f1_to_f2_negated(destination, data, stride, is_aligned, offset, ssmd_datacount);
                    }
                    break;

                case 3:
                    if (!substract)
                    {
                        simd.move_indexed_data_as_f1_to_f3(destination, data, stride, is_aligned, offset, ssmd_datacount);
                    }
                    else
                    {
                        simd.move_indexed_data_as_f1_to_f3_negated(destination, data, stride, is_aligned, offset, ssmd_datacount);
                    }
                    break;

                case 4:
                    if (!substract)
                    {
                        simd.move_indexed_data_as_f1_to_f4(destination, data, stride, is_aligned, offset, ssmd_datacount);
                    }
                    else
                    {
                        simd.move_indexed_data_as_f1_to_f4_negated(destination, data, stride, is_aligned, offset, ssmd_datacount);
                    }
                    break;

#if DEVELOPMENT_BUILD || TRACE
                default:
                    Debug.LogError($"blast.ssmd.stack.expand_f1_into_fn -> expanding into unsupported vectorsize {expand_into_n} at data offset {c - BlastInterpretor.opt_id}");
                    break;
#endif
            }
        }
        static void expand_indexed_into_n(int expand_into_n, int ssmd_datacount, in DATAREC destination, bool substract, byte c, int offset, void** data, bool is_aligned, int stride)
        {
            switch (expand_into_n)
            {
                case 2:
                    if (!substract)
                    {
                        simd.move_indexed_f1f2(destination, data, stride, is_aligned, offset, ssmd_datacount);
                    }
                    else
                    {
                        simd.move_indexed_f1f2_negated(destination, data, stride, is_aligned, offset, ssmd_datacount);
                    }
                    break;

                case 3:
                    if (!substract)
                    {
                        simd.move_indexed_f1f3(destination, data, stride, is_aligned, offset, ssmd_datacount);
                    }
                    else
                    {
                        simd.move_indexed_f1f3_negated(destination, data, stride, is_aligned, offset, ssmd_datacount);
                    }
                    break;

                case 4:
                    if (!substract)
                    {
                        simd.move_indexed_f1f4(destination, data, stride, is_aligned, offset, ssmd_datacount);
                    }
                    else
                    {
                        simd.move_indexed_f1f4_negated(destination, data, stride, is_aligned, offset, ssmd_datacount);
                    }
                    break;

#if DEVELOPMENT_BUILD || TRACE
                default:
                    Debug.LogError($"blast.ssmd.stack.expand_f1_into_fn -> expanding into unsupported vectorsize {expand_into_n} at data offset {c - BlastInterpretor.opt_id}");
                    break;
#endif
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void expand_constant_into_n(int expand_into_n, int ssmd_datacount, [NoAlias] float4* destination, float constant)
        {
            switch (expand_into_n)
            {
                case 2:
                    simd.set_f2_from_constant(destination, constant, ssmd_datacount);
                    break;

                case 3:
                    simd.set_f3_from_constant(destination, constant, ssmd_datacount);
                    break;

                case 4:
                    simd.set_f4_from_constant(destination, constant, ssmd_datacount);
                    break;

#if DEVELOPMENT_BUILD || TRACE
                default:
                    Debug.LogError($"blast.ssmd.stack.expand_constant_into_fn -> expanding into unsupported vectorsize {expand_into_n}");
                    break;
#endif
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void expand_constant_into_n(int expand_into_n, int ssmd_datacount, in DATAREC destination, float constant)
        {
#if DEVELOPMENT_BUILD || TRACE
            if (destination.vectorsize != expand_into_n)
            {
                Debug.LogError($"expand_constant_into_n: vectorsize mismatch, expanding into {expand_into_n} but datarecord points to vectorsize  {destination.vectorsize}");
            }
#endif

            switch (expand_into_n)
            {
                case 2:
                    simd.move_f1_constant_to_indexed_data_as_f2(destination, constant, ssmd_datacount);
                    break;

                case 3:
                    simd.move_f1_constant_to_indexed_data_as_f3(destination, constant, ssmd_datacount);
                    break;

                case 4:
                    simd.move_f1_constant_to_indexed_data_as_f4(destination, constant, ssmd_datacount);
                    break;

#if DEVELOPMENT_BUILD || TRACE
                default:
                    Debug.LogError($"blast.ssmd.stack.expand_constant_into_fn -> expanding into unsupported vectorsize {expand_into_n}");
                    break;
#endif
            }
        }

        #endregion

        #region GetSizeResult


        /// <summary>
        /// get size of given data element 
        /// </summary>
        BlastError GetSizeResult(ref int code_pointer, ref byte vector_size, int ssmd_datacount, [NoAlias] float4* f4_result, in DATAREC target = default)
        {
            // in release this sets NaN on errors 
            float fsize = float.NaN;
            BlastError res = BlastError.success;

            // index would be the same for each ssmd record
            byte c = code[code_pointer];

            if (c == (byte)blast_operation.cdataref)
            {
                // get offset to the cdata from current location
                int offset = (code[code_pointer + 1] << 8) + code[code_pointer + 2];
                int index = code_pointer - offset + 1;

                // validate we end up at cdata
                if (!BlastInterpretor.IsValidCDATAStart(code[index]))
                {
#if DEVELOPMENT_BUILD || TRACE
                    Debug.LogError($"Blast.SSMD.Interpretor.size: error in cdata reference at {code_pointer} referencing code[index] {index} ");
#endif
                    vector_size = 0;
                    res = BlastError.error_failed_to_read_cdata_reference;
                }
                else
                {
                    fsize = BlastInterpretor.GetCDATAElementCount(code, index);
                    vector_size = 1;
                    code_pointer = code_pointer + 2;
                }
            }
            else
            {
                int dataindex = c - BlastInterpretor.opt_id;

                // dataindex must point to a valid id 
                if (dataindex < 0 || dataindex + BlastInterpretor.opt_id >= 255)
                {

#if DEVELOPMENT_BUILD || TRACE
                    Debug.LogError($"Blast.SSMD.Interpretor.Size: first parameter must directly point to a dataindex but its '{dataindex + BlastInterpretor.opt_id}' instead");
#endif
                    res = BlastError.error_failed_to_read_cdata_reference;
                    vector_size = 0;
                }
                else
                {
                    // get size from variable data 
                    int size = BlastInterpretor.GetMetaDataSize(in metadata, (byte)dataindex);
                    size = math.select(size, 4, size == 0);

                    // copy size into result variable 
                    vector_size = 1;
                    fsize = (float)size;
                }
            }

            // move sizedata to target location 
            if (target.is_set)
            {
                // direct to target from sequence assignment
                simd.move_f1_constant_to_indexed_data_as_f1(target.data, target.row_size, target.is_aligned, target.index, fsize, ssmd_datacount);
            }
            else
            {
                // into intermediate buffer 
                simd.move_f1_constant_into_f4_array_as_f1(f4_result, fsize, ssmd_datacount);
            }

            return res;
        }

        #endregion

        #region pop_f[1|2|3|4]_into 
        /// <summary>
        /// pop value from stack, codepointer should be on the pop 
        /// </summary>
        void stackpop_fn_info(int code_pointer, in DATAREC target, bool is_negated, int indexed, out byte vector_size)
        {
#if DEVELOPMENT_BUILD || TRACE
            Assert.IsTrue(target.is_set, $"blast.ssmd.interpretor.stack_pop_fn_into: target not set or invalid");
            Assert.IsTrue(code[code_pointer] == (byte)blast_operation.pop);
#endif

            BlastVariableDataType type;
            GetStackMetaData(metadata, (byte)stack_offset, out vector_size, out type);
            if (type != BlastVariableDataType.Numeric)
            {
#if DEVELOPMENT_BUILD || TRACE
                Debug.LogError($"blast.ssmd.stack, stack_pop_fn_into -> stackdata mismatch, expecting numeric of size 1, found {type} of size {vector_size} at stack offset {stack_offset}");
#endif
                vector_size = 0;
                return;
            }

            vector_size = (byte)math.select(vector_size, 4, vector_size == 0);
            stack_offset = stack_offset - vector_size;
            int indexed_stack_offset = stack_offset + math.select(0, indexed, indexed >= 0);

            // stack pop 
            switch (vector_size)
            {
                case 1:
                    if (!is_negated) simd.move_indexed_f1(target.data, target.row_size, target.is_aligned, target.index, stack, stack_rowsize, stack_is_aligned, indexed_stack_offset, ssmd_datacount);
                    else simd.move_indexed_f1_n(target.data, target.row_size, target.is_aligned, target.index, stack, stack_rowsize, stack_is_aligned, indexed_stack_offset, ssmd_datacount);
                    return;

                case 2:
                    if (!is_negated) simd.move_indexed_f2(target.data, target.row_size, target.is_aligned, target.index, stack, stack_rowsize, stack_is_aligned, indexed_stack_offset, ssmd_datacount);
                    else simd.move_indexed_f2_n(target.data, target.row_size, target.is_aligned, target.index, stack, stack_rowsize, stack_is_aligned, indexed_stack_offset, ssmd_datacount);
                    return;

                case 3:
                    if (!is_negated) simd.move_indexed_f3(target.data, target.row_size, target.is_aligned, target.index, stack, stack_rowsize, stack_is_aligned, indexed_stack_offset, ssmd_datacount);
                    else simd.move_indexed_f3_n(target.data, target.row_size, target.is_aligned, target.index, stack, stack_rowsize, stack_is_aligned, indexed_stack_offset, ssmd_datacount);
                    return;

                case 4:
                    if (!is_negated) simd.move_indexed_f4(target.data, target.row_size, target.is_aligned, target.index, stack, stack_rowsize, stack_is_aligned, indexed_stack_offset, ssmd_datacount);
                    else simd.move_indexed_f4_n(target.data, target.row_size, target.is_aligned, target.index, stack, stack_rowsize, stack_is_aligned, indexed_stack_offset, ssmd_datacount);
                    return;
            }

#if DEVELOPMENT_BUILD || TRACE
            Debug.LogError($"blast.ssmd.stack, stack_pop_fn_into -> unsupported vectorsize {vector_size} at stack offset {stack_offset}");
#endif
            vector_size = 0;
        }


        /// <summary>
        /// pop a stack location into the destination register 
        /// </summary>
        void stackpop_fn_info(int code_pointer, [NoAlias] float4* destination, bool is_negated, int indexed, out byte vector_size)
        {
#if DEVELOPMENT_BUILD || TRACE
            Assert.IsFalse(destination == null, $"blast.ssmd.interpretor.stack_pop_fn_into: destination NULL");
            Assert.IsTrue(code[code_pointer] == (byte)blast_operation.pop);
#endif

            BlastVariableDataType type;
            GetStackMetaData(metadata, (byte)stack_offset, out vector_size, out type);
            if (type != BlastVariableDataType.Numeric)
            {
#if DEVELOPMENT_BUILD || TRACE
                Debug.LogError($"blast.ssmd.stack, stack_pop_fn_into -> stackdata mismatch, expecting numeric of size 1, found {type} of size {vector_size} at stack offset {stack_offset}");
#endif
                vector_size = 0;
                return;
            }

            vector_size = (byte)math.select(vector_size, 4, vector_size == 0);
            stack_offset = stack_offset - vector_size;
            int indexed_stack_offset = stack_offset + math.select(0, indexed, indexed >= 0);

            // stack pop 
            switch (vector_size)
            {
                case 1:
                    if (!is_negated) simd.move_indexed_data_as_f1_to_f1f4(destination, stack, stack_rowsize, stack_is_aligned, indexed_stack_offset, ssmd_datacount);
                    else simd.move_indexed_data_as_f1_to_f1f4_negated(destination, stack, stack_rowsize, stack_is_aligned, indexed_stack_offset, ssmd_datacount);
                    return;

                case 2:
                    if (!is_negated) simd.move_indexed_data_as_f2_to_f2(destination, stack, stack_rowsize, stack_is_aligned, indexed_stack_offset, ssmd_datacount);
                    else simd.move_indexed_data_as_f2_to_f2_negated(destination, stack, stack_rowsize, stack_is_aligned, indexed_stack_offset, ssmd_datacount);
                    return;

                case 3:
                    if (!is_negated) simd.move_indexed_data_as_f3_to_f3(destination, stack, stack_rowsize, stack_is_aligned, indexed_stack_offset, ssmd_datacount);
                    else simd.move_indexed_data_as_f3_to_f3_negated(destination, stack, stack_rowsize, stack_is_aligned, indexed_stack_offset, ssmd_datacount);
                    return;

                case 4:
                    if (!is_negated) simd.move_indexed_data_as_f4_to_f4(destination, stack, stack_rowsize, stack_is_aligned, indexed_stack_offset, ssmd_datacount);
                    else simd.move_indexed_data_as_f4_to_f4_negated(destination, stack, stack_rowsize, stack_is_aligned, indexed_stack_offset, ssmd_datacount);
                    return;
            }

#if DEVELOPMENT_BUILD || TRACE
            Debug.LogError($"blast.ssmd.stack, stack_pop_fn_into -> unsupported vectorsize {vector_size} at stack offset {stack_offset}");
#endif
            vector_size = 0;
        }



        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        bool pop_fx_into_ref<T>(ref int code_pointer, [NoAlias] T* destination, int ssmd_datacount, bool copy_only_the_first_if_constant = false) where T : unmanaged
        {
            return pop_fx_into_ref<T>(ref code_pointer, destination, null, -1, false, 0, ssmd_datacount, copy_only_the_first_if_constant);
        }

        /// <summary>
        /// 
        /// - code_pointer is expected to be on the first token to process
        /// - code_pointer is 1 token after the last item processed 
        /// 
        /// pop a float[1|2|3|4] value form stack data or constant source and put it in destination 
        /// </summary>
        /// <returns>true if the value returned is a constant</returns>
        bool pop_fx_into_ref<T>(ref int code_pointer, [NoAlias] T* destination = null, void** destination_data = null, int destination_offset = -1, bool is_aligned = false, int stride = 0, int input_ssmd_datacount = -1, bool copy_only_the_first_if_constant = false) where T : unmanaged
        {
#if DEVELOPMENT_BUILD || TRACE
            if (destination == null && destination_offset < 0)
            {
                Debug.Log($"blast.ssmd.interpretor.pop_fx_into_ref<T>: destination NULL and an invalid destination datasegment offset, one of both must be valid");
                return false;
            }
            if (destination_offset >= 0 && destination_data == null)
            {
                Debug.Log($"blast.ssmd.interpretor.pop_fx_into_ref<T>: destination_data NULL while offset is set");
                return false;
            }
#endif

            int vector_size = sizeof(T) / 4;


#if DEVELOPMENT_BUILD || TRACE
            BlastVariableDataType type;
            int size;
#endif

            float constant;
            byte* bf = stackalloc byte[4];

            bool is_negated = false;
            int indexed = -1;
            bool constant_index = false;

            input_ssmd_datacount = math.select(input_ssmd_datacount, ssmd_datacount, input_ssmd_datacount == -1);


            while (true)
            {
                byte c = code[code_pointer];

                switch ((blast_operation)c)
                {
                    case blast_operation.substract: is_negated = true; code_pointer++; continue;
                    case blast_operation.index_x: indexed = 0; constant_index = true; code_pointer++; continue;
                    case blast_operation.index_y: indexed = 1; constant_index = true; code_pointer++; continue;
                    case blast_operation.index_z: indexed = 2; constant_index = true; code_pointer++; continue;
                    case blast_operation.index_w: indexed = 3; constant_index = true; code_pointer++; continue;

                    case blast_operation.index_n:
                        {
                            indexed = 4;
                            code_pointer++;
                            constant_index = pop_fx_into_ref<float>(ref code_pointer, indices, input_ssmd_datacount, true);
                            if (constant_index)
                            {
                                indexed = (int)indices[0];
                            }
                            continue;
                        }


                    //
                    // stack pop 
                    // 
                    case blast_operation.pop:
#if DEVELOPMENT_BUILD || TRACE
                        type = GetStackMetaDataType((byte)stack_offset);
                        size = GetStackMetaDataSize((byte)stack_offset);
                        if (size != vector_size || type != BlastVariableDataType.Numeric)
                        {
                            Debug.LogError($"blast.ssmd.pop_fx_into_ref<T> -> stackdata mismatch, expecting numeric of size {vector_size}, found {type} of size {size} at stack offset {stack_offset}");
                            return false;
                        }
#endif
                        // stack pop 
                        stack_offset = stack_offset - vector_size;

                        // index if applied
                        indexed = math.select(stack_offset + indexed, stack_offset, indexed < 0);

                        if (destination_offset < 0)
                        {
                            // set destination pointer 

                            if (!is_negated)
                            {
                                switch (vector_size)
                                {
                                    case 1: simd.move_indexed_data_as_f1_to_f1((float*)(void*)destination, stack, stack_rowsize, stack_is_aligned, indexed, input_ssmd_datacount); break;
                                    case 2: simd.move_indexed_data_as_f2_to_f2((float2*)(void*)destination, stack, stack_rowsize, stack_is_aligned, indexed, input_ssmd_datacount); break;
                                    case 3: simd.move_indexed_data_as_f3_to_f3((float3*)(void*)destination, stack, stack_rowsize, stack_is_aligned, indexed, input_ssmd_datacount); break;
                                    case 0:
                                    case 4: simd.move_indexed_data_as_f4_to_f4((float4*)(void*)destination, stack, stack_rowsize, stack_is_aligned, indexed, input_ssmd_datacount); break;
                                }


                                // for (int i = 0; i < input_ssmd_datacount; i++) destination[i] = ((T*)(void*)&((float*)stack[i])[indexed])[0];
                            }
                            else
                                switch (vector_size)
                                {
                                    // when indexed the compiler would have set the assignee to be vectorsize 1
                                    case 1: simd.move_indexed_data_as_f1_to_f1_n((float*)(void*)destination, stack, stack_rowsize, stack_is_aligned, indexed, input_ssmd_datacount); break;
                                    case 2: simd.move_indexed_data_as_f1_to_f2_n((float2*)(void*)destination, stack, stack_rowsize, stack_is_aligned, indexed, input_ssmd_datacount); break;
                                    case 3: simd.move_indexed_data_as_f1_to_f3_n((float3*)(void*)destination, stack, stack_rowsize, stack_is_aligned, indexed, input_ssmd_datacount); break;
                                    case 0:
                                    case 4: simd.move_indexed_data_as_f1_to_f4_n((float4*)(void*)destination, stack, stack_rowsize, stack_is_aligned, indexed, input_ssmd_datacount); break;
                                }
                        }
                        else
                        {
                            // set offset in datasegment
                            if (!is_negated)
                            {
                                switch (vector_size)
                                {
                                    case 1: simd.move_indexed_f1(destination_data, stride, is_aligned, destination_offset, stack, stack_rowsize, stack_is_aligned, indexed, input_ssmd_datacount); break;
                                    case 2: simd.move_indexed_f2(destination_data, stride, is_aligned, destination_offset, stack, stack_rowsize, stack_is_aligned, indexed, input_ssmd_datacount); break;
                                    case 3: simd.move_indexed_f3(destination_data, stride, is_aligned, destination_offset, stack, stack_rowsize, stack_is_aligned, indexed, input_ssmd_datacount); break;
                                    case 0:
                                    case 4: simd.move_indexed_f4(destination_data, stride, is_aligned, destination_offset, stack, stack_rowsize, stack_is_aligned, indexed, input_ssmd_datacount); break;
                                }
                            }
                            else
                            {
                                switch (vector_size)
                                {
                                    case 1: simd.move_indexed_f1_n(destination_data, stride, is_aligned, destination_offset, stack, stack_rowsize, stack_is_aligned, indexed, input_ssmd_datacount); break;
                                    case 2: simd.move_indexed_f2_n(destination_data, stride, is_aligned, destination_offset, stack, stack_rowsize, stack_is_aligned, indexed, input_ssmd_datacount); break;
                                    case 3: simd.move_indexed_f3_n(destination_data, stride, is_aligned, destination_offset, stack, stack_rowsize, stack_is_aligned, indexed, input_ssmd_datacount); break;
                                    case 0:
                                    case 4: simd.move_indexed_f4_n(destination_data, stride, is_aligned, destination_offset, stack, stack_rowsize, stack_is_aligned, indexed, input_ssmd_datacount); break;
                                }
                            }
                        }
                        code_pointer++;
                        return true;

                    case blast_operation.constant_f1:
                        {
                            constant = read_constant_f1(code, ref code_pointer);
                            constant = math.select(constant, -constant, is_negated);

                            if (destination_offset >= 0)
                            {
                                pop_fx_into_constant_handler<T>(destination_data, destination_offset, constant, is_aligned, stride);
                            }
                            else
                            {
                                pop_fx_into_constant_handler<T>(destination, constant, copy_only_the_first_if_constant);
                            }
                        }
                        return true;

                    case blast_operation.constant_f1_h:
                        {
                            constant = read_constant_f1_h(code, ref code_pointer);
               
                            constant = math.select(constant, -constant, is_negated);

                            if (destination_offset >= 0)
                            {
                                pop_fx_into_constant_handler<T>(destination_data, destination_offset, constant, is_aligned, stride);
                            }
                            else
                            {
                                pop_fx_into_constant_handler<T>(destination, constant, copy_only_the_first_if_constant);
                            }
                        }
                        return true;
                                                  
                    case blast_operation.constant_short_ref:
                    case blast_operation.constant_long_ref:
                        {
                            if (follow_constant_ref(code, ref code_pointer, out BlastVariableDataType datatype, out int reference_index, c == (byte)blast_operation.constant_short_ref))
                            {
                                constant = read_constant_float(code, reference_index);
                                constant = math.select(constant, -constant, is_negated);

                                if (destination_offset >= 0)
                                {
                                    pop_fx_into_constant_handler<T>(destination_data, destination_offset, constant, is_aligned, stride);
                                }
                                else
                                {
                                    pop_fx_into_constant_handler<T>(destination, constant, copy_only_the_first_if_constant);
                                }
                            }
                            else
                            {
                                return false;
                            }
                        }
                        return true;

                    case blast_operation.cdataref:
                        {
                            CDATAEncodingType encoding = CDATAEncodingType.None;
                            if (BlastInterpretor.follow_cdataref(code, code_pointer, out int offset, out int length, out encoding))
                            {
                                if (!BlastInterpretor.IsValidCDATAStart(code[offset]))  // at some point we probably replace cdata with a datatype cdata_float etc.. 
                                {
                                    if (IsTrace) Debug.LogError($"Blast.SSMD.Interpretor.pop_fx_into_ref: error in cdata reference at {code_pointer} in encoding {encoding}");
                                    vector_size = 0;
                                    //return BlastError.error_failed_to_read_cdata_reference;
                                    return false;
                                }
                                else
                                {
                                    // advance past the cdata 
                                    code_pointer = code_pointer + 3;
                                    constant_index = constant_index || (indexed >= 0 && indexed < 4);

                                    // indexer at location ? 
                                    if (!constant_index)
                                    {
                                        constant_index = simd.all_equal(indices, ssmd_datacount);
                                    }

                                    // index from variable 
                                    if (indexed > 3)
                                    {
                                        if (constant_index)
                                        {
                                            indexed = (int)indices[0];
                                        }
                                        else
                                        {
                                            // handle each index seperately
                                            for (int i = 0; i < ssmd_datacount; i++)
                                            {


                                                Debug.Log("BIG TODO (see pop) generalize this first "); 



                                             //
                                             //
                                             //
                                             //
                                             //   register[i] = BlastInterpretor.index_cdata_f1(code, offset, indexed, length);
                                             //
                                             //
                                             //




                                            }
                                            vector_size = 1;

                                            // value set was not constant across all
                                            return false;
                                        }
                                    }
                                    // unsported access as of this moment, no indexer 
                                    if (indexed == -1)
                                    {
                                        if (IsTrace) Debug.LogError($"Blast.SSMD.Interpretor.get_single_op_result: error in cdata reference at {code_pointer} referencing code[index] {indexed}, cdata must be indexed");
                                    }
                                    else
                                    {
                                        // single index returning a constant value 
                                        constant = BlastInterpretor.index_cdata_f1(code, offset, indexed, length);

                                        // set the constant to destination
                                        if (destination_offset >= 0)
                                        {
                                            pop_fx_into_constant_handler<T>(destination_data, destination_offset, constant, is_aligned, stride);
                                        }
                                        else
                                        {
                                            pop_fx_into_constant_handler<T>(destination, constant, copy_only_the_first_if_constant);
                                        }

                                        // returns constant value
                                        vector_size = 1;
                                        return true;
                                    }
                                }
                            }
                        }
                        return false;

                    case blast_operation.pi:
                    case blast_operation.inv_pi:
                    case blast_operation.epsilon:
                    case blast_operation.infinity:
                    case blast_operation.negative_infinity:
                    case blast_operation.nan:
                    case blast_operation.min_value:
                    case blast_operation.value_0:
                    case blast_operation.value_1:
                    case blast_operation.value_2:
                    case blast_operation.value_3:
                    case blast_operation.value_4:
                    case blast_operation.value_8:
                    case blast_operation.value_10:
                    case blast_operation.value_16:
                    case blast_operation.value_24:
                    case blast_operation.value_32:
                    case blast_operation.value_64:
                    case blast_operation.value_100:
                    case blast_operation.value_128:
                    case blast_operation.value_256:
                    case blast_operation.value_512:
                    case blast_operation.value_1000:
                    case blast_operation.value_1024:
                    case blast_operation.value_30:
                    case blast_operation.value_45:
                    case blast_operation.value_90:
                    case blast_operation.value_180:
                    case blast_operation.value_270:
                    case blast_operation.value_360:
                    case blast_operation.inv_value_2:
                    case blast_operation.inv_value_3:
                    case blast_operation.inv_value_4:
                    case blast_operation.inv_value_8:
                    case blast_operation.inv_value_10:
                    case blast_operation.inv_value_16:
                    case blast_operation.inv_value_24:
                    case blast_operation.inv_value_32:
                    case blast_operation.inv_value_64:
                    case blast_operation.inv_value_100:
                    case blast_operation.inv_value_128:
                    case blast_operation.inv_value_256:
                    case blast_operation.inv_value_512:
                    case blast_operation.inv_value_1000:
                    case blast_operation.inv_value_1024:
                    case blast_operation.inv_value_30:
                    case blast_operation.framecount:
                    case blast_operation.fixedtime:
                    case blast_operation.time:
                    case blast_operation.fixeddeltatime:
                    case blast_operation.deltatime:
                        //
                        // CONSTANT operation                         
                        //
                        constant = engine_ptr->constants[c];
                        constant = math.select(constant, -constant, is_negated);

                        if (destination_offset >= 0)
                        {
                            pop_fx_into_constant_handler<T>(destination_data, destination_offset, constant, is_aligned, stride);
                        }
                        else
                        {
                            pop_fx_into_constant_handler<T>(destination, constant, copy_only_the_first_if_constant);
                        }

                        code_pointer++;
                        return true;


                    default:
                    case blast_operation.id:
                        //
                        // DATASEG operation
                        // 
#if DEVELOPMENT_BUILD || TRACE
                        type = BlastInterpretor.GetMetaDataType(metadata, (byte)(c - BlastInterpretor.opt_id));
                        size = BlastInterpretor.GetMetaDataSize(metadata, (byte)(c - BlastInterpretor.opt_id));
                        size = math.select(size, 4, size == 0);
                        if (math.select(size, 1, indexed >= 0) != vector_size || type != BlastVariableDataType.Numeric)
                        {
                            Debug.LogError($"blast.ssmd.pop_fx_into_ref<T> -> data mismatch, expecting numeric of size {vector_size}, found type {type} of size {size} at data offset {c - BlastInterpretor.opt_id}");
                            return false;
                        }
#endif
                        // index if applied
                        indexed = math.select(c - BlastInterpretor.opt_id + indexed, c - BlastInterpretor.opt_id, indexed < 0);

                        // variable acccess
                        if (destination_offset < 0)
                        {
                            if (!is_negated)
                            {
                                switch (vector_size)
                                {
                                    case 1: simd.move_indexed_data_as_f1_to_f1((float*)(void*)destination, data, data_rowsize, data_is_aligned, indexed, ssmd_datacount); is_negated = false; break;
                                    case 2: simd.move_indexed_data_as_f2_to_f2((float2*)(void*)destination, data, data_rowsize, data_is_aligned, indexed, ssmd_datacount); is_negated = false; break;
                                    case 3: simd.move_indexed_data_as_f3_to_f3((float3*)(void*)destination, data, data_rowsize, data_is_aligned, indexed, ssmd_datacount); is_negated = false; break;
                                    case 0:
                                    case 4: simd.move_indexed_data_as_f4_to_f4((float4*)(void*)destination, data, data_rowsize, data_is_aligned, indexed, ssmd_datacount); is_negated = false; break;
                                }
                            }
                            else
                                switch (vector_size)
                                {
                                    case 1: simd.move_indexed_data_as_f1_to_f1((float*)(void*)destination, data, data_rowsize, data_is_aligned, indexed, ssmd_datacount); is_negated = false; break;
                                    case 2: simd.move_indexed_data_as_f2_to_f2((float2*)(void*)destination, data, data_rowsize, data_is_aligned, indexed, ssmd_datacount); is_negated = false; break;
                                    case 3: simd.move_indexed_data_as_f3_to_f3((float3*)(void*)destination, data, data_rowsize, data_is_aligned, indexed, ssmd_datacount); is_negated = false; break;
                                    case 0:
                                    case 4: simd.move_indexed_data_as_f4_to_f4((float4*)(void*)destination, data, data_rowsize, data_is_aligned, indexed, ssmd_datacount); is_negated = false; break;
                                }
                        }
                        else
                        {
                            // set offset in datasegment// setting 1 datafield to the other 
                            if (!is_negated)
                            {
                                switch (vector_size)
                                {
                                    // when indexed the compiler would have set the assignee to be vectorsize 1
                                    case 1: simd.move_indexed_f1(destination_data, stride, is_aligned, destination_offset, data, data_rowsize, data_is_aligned, indexed, ssmd_datacount); break;
                                    case 2: simd.move_indexed_f2(destination_data, stride, is_aligned, destination_offset, data, data_rowsize, data_is_aligned, indexed, ssmd_datacount); break;
                                    case 3: simd.move_indexed_f3(destination_data, stride, is_aligned, destination_offset, data, data_rowsize, data_is_aligned, indexed, ssmd_datacount); break;
                                    case 0:
                                    case 4: simd.move_indexed_f4(destination_data, stride, is_aligned, destination_offset, data, data_rowsize, data_is_aligned, indexed, ssmd_datacount); break;
                                }
                            }
                            else
                            {
                                switch (vector_size)
                                {
                                    // when indexed the compiler would have set the assignee to be vectorsize 1
                                    case 1: simd.move_indexed_f1_n(destination_data, stride, is_aligned, destination_offset, data, data_rowsize, data_is_aligned, indexed, ssmd_datacount); break;
                                    case 2: simd.move_indexed_f2_n(destination_data, stride, is_aligned, destination_offset, data, data_rowsize, data_is_aligned, indexed, ssmd_datacount); break;
                                    case 3: simd.move_indexed_f3_n(destination_data, stride, is_aligned, destination_offset, data, data_rowsize, data_is_aligned, indexed, ssmd_datacount); break;
                                    case 0:
                                    case 4: simd.move_indexed_f4_n(destination_data, stride, is_aligned, destination_offset, data, data_rowsize, data_is_aligned, indexed, ssmd_datacount); break;
                                }
                            }
                        }
                        code_pointer++;
                        return false;
                }
            }
        }

        void pop_fx_into_constant_handler<T>(void** data, int destination_offset, float constant, bool is_aligned, int stride) where T : unmanaged
        {
            byte vsize = (byte)(sizeof(T) >> 2);

            switch (vsize)
            {
                case 1:
                    simd.move_f1_constant_to_indexed_data_as_f1(data, stride, is_aligned, destination_offset, constant, ssmd_datacount);
                    return;

                case 2:
                    simd.move_f1_constant_to_indexed_data_as_f2(data, stride, is_aligned, destination_offset, constant, ssmd_datacount);
                    return;

                case 3:
                    simd.move_f1_constant_to_indexed_data_as_f3(data, stride, is_aligned, destination_offset, constant, ssmd_datacount);
                    return;

                case 4:
                    simd.move_f1_constant_to_indexed_data_as_f4(data, stride, is_aligned, destination_offset, constant, ssmd_datacount);
                    return;
            }
#if DEVELOPMENT_BUILD || TRACE
            Debug.LogError($"blast.pop_fx_into_constant_handler: -> codepointer {code_pointer} unsupported operation Vin: {vsize}");
            return;
#endif
        }

        void pop_fx_into_constant_handler<T>([NoAlias] T* buffer, float constant, bool copy_only_the_first = false)
            where T : unmanaged
        {
            byte vsize = (byte)(sizeof(T) >> 2);

            // byte switches give nice jumptables, save an if combining 
            switch ((byte)math.select(vsize, vsize + 100, copy_only_the_first))
            {
                case 1:
                    simd.set_f1_from_constant((float*)buffer, constant, ssmd_datacount);
                    return;

                case 101:
                    ((float*)buffer)[0] = constant;
                    return;

                case 2:
                    // by filling 2x as much we effectively fill the same float2 array 
                    simd.set_f1_from_constant((float*)buffer, constant, ssmd_datacount * 2);
                    return;

                case 102:
                    ((float2*)buffer)[0] = constant;
                    return;

                case 3:
                    // by filling 3x as much we effectively fill the same float3 array 
                    simd.set_f1_from_constant((float*)buffer, constant, ssmd_datacount * 3);
                    return;

                case 103:
                    ((float3*)buffer)[0] = constant;
                    return;

                case 4:
                    // by filling 4x as much we effectively fill the same float4 array 
                    simd.set_f1_from_constant((float*)buffer, constant, ssmd_datacount * 4);
                    return;

                case 104:
                    ((float4*)buffer)[0] = constant;
                    return;
            }
#if DEVELOPMENT_BUILD || TRACE
            Debug.LogError($"blast.pop_fx_into_constant_handler: -> codepointer {code_pointer} unsupported operation Vin: {vsize}");
            return;
#endif
        }

        void pop_fx_with_op_into_fx_constant_handler<Tin, Tout>([NoAlias] Tin* buffer, [NoAlias] Tout* output, blast_operation op, float constant, BlastVariableDataType datatype)
            where Tin : unmanaged
            where Tout : unmanaged
        {
            int vin_size = sizeof(Tin) >> 2;
            int vout_size = sizeof(Tout) >> 2;
            float4 f4;

            // at some point datattyype and vectorsize merge and this dual switch becomes 1
            switch (datatype)
            {
                case BlastVariableDataType.Numeric:
                    switch (vin_size)
                    {
                        case 1:
                            float* f1_in = (float*)buffer;
                            switch (vout_size)
                            {
                                // pop f1 with op into f1
                                case 1:
                                    {
                                        float* f1_out = (float*)output;
                                        switch (op)
                                        {
                                            case blast_operation.multiply:
                                                simd.mul_array_f1f1_constant(f1_out, f1_in, constant, ssmd_datacount);
                                                return;

                                            case blast_operation.add:
                                                simd.add_array_f1f1_constant(f1_out, f1_in, constant, ssmd_datacount);
                                                return;

                                            case blast_operation.substract:
                                                simd.sub_array_f1f1_constant(f1_out, f1_in, constant, ssmd_datacount);
                                                return;

                                            case blast_operation.divide:
                                                simd.div_array_f1f1_constant(f1_out, f1_in, constant, ssmd_datacount);
                                                return;

                                            case blast_operation.and:
                                                if (constant == 0)
                                                {
                                                    // for (int i = 0; i < ssmd_datacount; i++) f1_out[i] = 0f;
                                                    UnsafeUtils.MemClear(f1_out, ssmd_datacount * 4);
                                                }
                                                else
                                                {
                                                    for (int i = 0; i < ssmd_datacount; i++) f1_out[i] = math.select(0, 1f, f1_in[i] != 0);
                                                }
                                                return;

                                            case blast_operation.or:
                                                if (constant == 0)
                                                {
                                                    // nothing changes if its the same buffer so skip doing stuff in that case
                                                    if (output != buffer)
                                                    {
                                                        // if not the same buffer then we still need to copy the data 
                                                        // for (int i = 0; i < ssmd_datacount; i++) f1_out[i] = f1_in[i];
                                                        UnsafeUtils.MemCpy(f1_out, f1_in, ssmd_datacount * 4);
                                                    }
                                                }
                                                else
                                                {
                                                    // just write true,  | true is always true 
                                                    // for (int i = 0; i < ssmd_datacount; i++) f1_out[i] = 1f;
                                                    UnsafeUtils.Ones(f1_out, ssmd_datacount);
                                                }
                                                return;

                                            case blast_operation.min:
                                                for (int i = 0; i < ssmd_datacount; i++) f1_out[i] = math.min(f1_in[i], constant);
                                                return;

                                            case blast_operation.max:
                                                for (int i = 0; i < ssmd_datacount; i++) f1_out[i] = math.max(f1_in[i], constant);
                                                return;

                                            case blast_operation.mina:
                                            case blast_operation.maxa:
                                            case blast_operation.csum:
                                                for (int i = 0; i < ssmd_datacount; i++) f1_out[i] = f1_in[i] + constant;
                                                return;
                                        }
                                    }
                                    break;

                                // pop f1 with op into f4
                                case 4:
                                    {
                                        float4* f4_out = (float4*)output;
                                        switch (op)
                                        {
                                            case blast_operation.multiply:
                                                simd.mul_array_f14f1_constant(f4_out, f1_in, constant, ssmd_datacount);
                                                return;

                                            case blast_operation.add:
                                                simd.add_array_f14f1_constant(f4_out, f1_in, constant, ssmd_datacount);
                                                return;

                                            case blast_operation.substract:
                                                simd.sub_array_f14f1_constant(f4_out, f1_in, constant, ssmd_datacount);
                                                return;

                                            case blast_operation.divide:
                                                simd.div_array_f14f1_constant(f4_out, f1_in, constant, ssmd_datacount);
                                                return;

                                            case blast_operation.and:
                                                if (constant == 0)
                                                {
                                                    // for (int i = 0; i < ssmd_datacount; i++) //  f4_out[i].x = 0f;
                                                    UnsafeUtils.MemClear(f4_out, 16 * ssmd_datacount);
                                                }
                                                else
                                                {
                                                    for (int i = 0; i < ssmd_datacount; i++) f4_out[i].x = math.select(0, 1f, f1_in[i] != 0);
                                                }
                                                return;

                                            case blast_operation.or:
                                                if (constant == 0)
                                                {
                                                    if (output != buffer)
                                                    {
                                                        // if not the same buffer then we still need to copy the data 
                                                        for (int i = 0; i < ssmd_datacount; i++) f4_out[i].x = f1_in[i];
                                                    }
                                                }
                                                else
                                                {
                                                    for (int i = 0; i < ssmd_datacount; i++) f4_out[i].x = 1f;
                                                }
                                                return;

                                            case blast_operation.min:
                                                for (int i = 0; i < ssmd_datacount; i++) f4_out[i].x = math.min(f1_in[i], constant);
                                                return;

                                            case blast_operation.max:
                                                for (int i = 0; i < ssmd_datacount; i++) f4_out[i].x = math.max(f1_in[i], constant);
                                                return;

                                            case blast_operation.mina:
                                            case blast_operation.maxa:
                                            case blast_operation.csum:
                                                f4 = constant;
                                                simd.set_f1_from_constant(f4_out, constant, ssmd_datacount);
                                                //for (int i = 0; i < ssmd_datacount; i++) f4_out[i].x = constant;
                                                return;
                                        }
                                    }
                                    break;
                            }
                            break;


                        case 2:
                            float2* f2_in = (float2*)buffer;
                            switch (vout_size)
                            {
                                case 1:
                                    {
                                        float* f1_out = (float*)output;

                                        // only these operations will result in single sized outputs from size2 input
                                        switch (op)
                                        {
                                            case blast_operation.mina:
                                            case blast_operation.csum:
                                            case blast_operation.maxa:
                                                simd.set_f1_from_constant(f1_out, constant, ssmd_datacount);
                                                // for (int i = 0; i < ssmd_datacount; i++) f1_out[i] = constant;
                                                return;
                                        }
                                    }
                                    break;


                                // pop f2 with op into f2
                                case 2:
                                    {
                                        float2* f2_out = (float2*)output;
                                        switch (op)
                                        {
                                            case blast_operation.multiply:
                                                simd.mul_array_f2f2_constant(f2_out, f2_in, constant, ssmd_datacount);
                                                return;

                                            case blast_operation.add:
                                                simd.add_array_f2f2_constant(f2_out, f2_in, constant, ssmd_datacount);
                                                return;

                                            case blast_operation.substract:
                                                simd.sub_array_f2f2_constant(f2_out, f2_in, constant, ssmd_datacount);
                                                return;

                                            case blast_operation.divide:
                                                simd.div_array_f2f2_constant(f2_out, f2_in, constant, ssmd_datacount);
                                                return;

                                            case blast_operation.and:
                                                if (constant == 0)
                                                {
                                                    // for (int i = 0; i < ssmd_datacount; i++) f2_out[i] = 0f;
                                                    UnsafeUtils.MemClear(f2_out, ssmd_datacount * 8);
                                                }
                                                else
                                                {
                                                    for (int i = 0; i < ssmd_datacount; i++) f2_out[i] = math.select(0, 1f, f2_in[i] != 0);
                                                }
                                                return;

                                            case blast_operation.or:
                                                if (constant == 0)
                                                {
                                                    if (output != buffer)
                                                    {
                                                        // if not the same buffer then we still need to copy the data 
                                                        // for (int i = 0; i < ssmd_datacount; i++) f2_out[i] = f2_in[i];
                                                        UnsafeUtils.MemCpy(f2_out, f2_in, ssmd_datacount * 8);
                                                    }
                                                }
                                                else
                                                {
                                                    float2 f2 = 1f;
                                                    //if (standalone_vsbuild)
                                                    //{
                                                    //    for (int i = 0; i < ssmd_datacount; i++) f2_out[i] = 1f;
                                                    //}
                                                    //else
                                                    //{
                                                    UnsafeUtils.MemCpyReplicate(f2_out, &f2, 8, ssmd_datacount);
                                                    //}
                                                }
                                                return;

                                            case blast_operation.min:
                                                for (int i = 0; i < ssmd_datacount; i++) f2_out[i] = math.min(f2_in[i], constant);
                                                return;

                                            case blast_operation.max:
                                                for (int i = 0; i < ssmd_datacount; i++) f2_out[i] = math.max(f2_in[i], constant);
                                                return;
                                        }
                                    }
                                    break;

                                // pop f2 with op into f4
                                case 4:
                                    {
                                        float4* f4_out = (float4*)output;
                                        switch (op)
                                        {
                                            case blast_operation.multiply:
                                                simd.mul_array_f2f2_constant(f4_out, f2_in, constant, ssmd_datacount);
                                                return;

                                            case blast_operation.add:
                                                simd.add_array_f2f2_constant(f4_out, f2_in, constant, ssmd_datacount);
                                                return;

                                            case blast_operation.substract:
                                                simd.sub_array_f2f2_constant(f4_out, f2_in, constant, ssmd_datacount);
                                                return;

                                            case blast_operation.divide:
                                                simd.div_array_f2f2_constant(f4_out, f2_in, constant, ssmd_datacount);
                                                return;

                                            case blast_operation.and:
                                                if (constant == 0)
                                                {
                                                    UnsafeUtils.MemClear(f4_out, ssmd_datacount * 16);
                                                    //for (int i = 0; i < ssmd_datacount; i++) f4_out[i].xy = 0f;
                                                }
                                                else
                                                {
                                                    for (int i = 0; i < ssmd_datacount; i++) f4_out[i].xy = math.select(0, 1f, f2_in[i] != 0);
                                                }
                                                return;

                                            case blast_operation.or:
                                                if (constant == 0)
                                                {
                                                    if (output != buffer)
                                                    {
                                                        // if not the same buffer then we still need to copy the data 
                                                        //if (standalone_vsbuild)
                                                        {
                                                            for (int i = 0; i < ssmd_datacount; i++) f4_out[i].xy = f2_in[i];
                                                        }
                                                        // - really need to test when what is faster .. 
                                                        //else
                                                        //{
                                                        //    UnsafeUtils.MemCpyStride(f4_out, 16, f2_in, 8, 8, ssmd_datacount);
                                                        //}
                                                    }
                                                }
                                                else
                                                {
                                                    //for (int i = 0; i < ssmd_datacount; i++) f4_out[i].xy = 1f;
                                                    UnsafeUtils.Ones(f4_out, ssmd_datacount);
                                                }
                                                return;

                                            case blast_operation.min:
                                                for (int i = 0; i < ssmd_datacount; i++) f4_out[i].xy = math.min(f2_in[i], constant);
                                                return;

                                            case blast_operation.max:
                                                for (int i = 0; i < ssmd_datacount; i++) f4_out[i].xy = math.max(f2_in[i], constant);
                                                return;
                                        }
                                    }
                                    break;
                            }
                            break;

                        case 3:
                            float3* f3_in = (float3*)buffer;
                            switch (vout_size)
                            {
                                case 1:
                                    {
                                        float* f1_out = (float*)output;
                                        // only these operations will result in single sized outputs from size3 input
                                        switch (op)
                                        {
                                            case blast_operation.mina:
                                            case blast_operation.csum:
                                            case blast_operation.maxa:
                                                //if (standalone_vsbuild)
                                                {
                                                    for (int i = 0; i < ssmd_datacount; i++) f1_out[i] = constant;
                                                }
                                                //else
                                                //{
                                                //    UnsafeUtils.MemCpyReplicate(f1_out, &constant, 4, ssmd_datacount);
                                                //}
                                                return;
                                        }
                                    }
                                    break;

                                // pop f3 with op into f3
                                case 3:
                                    {
                                        float3* f3_out = (float3*)output;
                                        switch (op)
                                        {
                                            case blast_operation.multiply:
                                                simd.mul_array_f3f3_constant(f3_out, f3_in, constant, ssmd_datacount);
                                                return;

                                            case blast_operation.add:
                                                simd.add_array_f3f3_constant(f3_out, f3_in, constant, ssmd_datacount);
                                                return;

                                            case blast_operation.substract:
                                                simd.sub_array_f3f3_constant(f3_out, f3_in, constant, ssmd_datacount);
                                                return;

                                            case blast_operation.divide:
                                                simd.div_array_f3f3_constant(f3_out, f3_in, constant, ssmd_datacount);
                                                return;

                                            case blast_operation.and:
                                                if (constant == 0)
                                                {
                                                    UnsafeUtils.MemClear(f3_out, ssmd_datacount * 12);
                                                    // for (int i = 0; i < ssmd_datacount; i++) f3_out[i] = 0f;
                                                }
                                                else
                                                {
                                                    for (int i = 0; i < ssmd_datacount; i++) f3_out[i] = math.select(0, 1f, f3_in[i] != 0);
                                                }
                                                return;

                                            case blast_operation.or:
                                                if (constant == 0)
                                                {
                                                    // nothing changes if its the same buffer so skip doing stuff in that case
                                                    if (output != buffer)
                                                    {
                                                        // if not the same buffer then we still need to copy the data 
                                                        // for (int i = 0; i < ssmd_datacount; i++) f3_out[i] = f3_in[i];
                                                        UnsafeUtils.MemCpy(f3_out, f3_in, ssmd_datacount * 12);
                                                    }
                                                }
                                                else
                                                {
                                                    // just write true,  | true is always true 
                                                    float3 f3 = 1f;
                                                    //for (int i = 0; i < ssmd_datacount; i++) f3_out[i] = 1f;
                                                    UnsafeUtils.MemCpyReplicate(f3_out, &f3, 12, ssmd_datacount);
                                                }
                                                return;

                                            case blast_operation.min:
                                                for (int i = 0; i < ssmd_datacount; i++) f3_out[i] = math.min(f3_in[i], constant);
                                                return;

                                            case blast_operation.max:
                                                for (int i = 0; i < ssmd_datacount; i++) f3_out[i] = math.max(f3_in[i], constant);
                                                return;
                                        }
                                    }
                                    break;

                                // pop f3 with op into f4
                                case 4:
                                    {
                                        float4* f4_out = (float4*)output;
                                        switch (op)
                                        {
                                            case blast_operation.multiply:
                                                simd.mul_array_f3f3_constant(f4_out, f3_in, constant, ssmd_datacount);
                                                return;

                                            case blast_operation.add:
                                                simd.add_array_f3f3_constant(f4_out, f3_in, constant, ssmd_datacount);
                                                return;

                                            case blast_operation.substract:
                                                simd.sub_array_f3f3_constant(f4_out, f3_in, constant, ssmd_datacount);
                                                return;

                                            case blast_operation.divide:
                                                simd.div_array_f3f3_constant(f4_out, f3_in, constant, ssmd_datacount);
                                                return;

                                            case blast_operation.and:
                                                if (constant == 0)
                                                {
                                                    UnsafeUtils.MemClear(f4_out, ssmd_datacount * 16);
                                                    // for (int i = 0; i < ssmd_datacount; i++) f4_out[i].xyz = 0f;
                                                }
                                                else
                                                {
                                                    for (int i = 0; i < ssmd_datacount; i++) f4_out[i].xyz = math.select(0, 1f, f3_in[i] != 0);
                                                }
                                                return;

                                            case blast_operation.or:
                                                if (constant == 0)
                                                {
                                                    if (output != buffer)
                                                    {
                                                        // if not the same buffer then we still need to copy the data 
                                                        //for (int i = 0; i < ssmd_datacount; i++) f4_out[i].xyz = f3_in[i];
                                                        UnsafeUtils.MemCpyStride(f4_out, 16, f3_in, 12, 12, ssmd_datacount);
                                                    }
                                                }
                                                else
                                                {
                                                    // for (int i = 0; i < ssmd_datacount; i++) f4_out[i].xy = 1f;
                                                    UnsafeUtils.Ones(f4_out, ssmd_datacount);
                                                }
                                                return;

                                            case blast_operation.min:
                                                for (int i = 0; i < ssmd_datacount; i++) f4_out[i].xyz = math.min(f3_in[i], constant);
                                                return;

                                            case blast_operation.max:
                                                for (int i = 0; i < ssmd_datacount; i++) f4_out[i].xyz = math.max(f3_in[i], constant);
                                                return;
                                        }

                                    }
                                    break;
                            }
                            break;

                        case 4:
                            float4* f4_in = (float4*)buffer;

                            switch (vout_size)
                            {
                                case 1:
                                    {
                                        float* f1_out = (float*)output;
                                        // only these operations will result in single sized outputs from size3 input
                                        switch (op)
                                        {
                                            case blast_operation.mina:
                                            case blast_operation.csum:
                                            case blast_operation.maxa:

                                                UnsafeUtils.MemCpyReplicate(f1_out, &constant, 4, ssmd_datacount);
                                                // for (int i = 0; i < ssmd_datacount; i++) f1_out[i] = constant;
                                                return;
                                        }
                                    }
                                    break;

                                case 4:
                                    {
                                        float4* f4_out = (float4*)output;
                                        switch (op)
                                        {
                                            case blast_operation.multiply:
                                                simd.mul_array_f4f4_constant(f4_out, f4_in, constant, ssmd_datacount);
                                                return;

                                            case blast_operation.add:
                                                simd.add_array_f4f4_constant(f4_out, f4_in, constant, ssmd_datacount);
                                                return;

                                            case blast_operation.substract:
                                                simd.sub_array_f4f4_constant(f4_out, f4_in, constant, ssmd_datacount);
                                                return;

                                            case blast_operation.divide:
                                                simd.div_array_f4f4_constant(f4_out, f4_in, constant, ssmd_datacount);
                                                return;

                                            case blast_operation.and:
                                                if (constant == 0)
                                                {
                                                    UnsafeUtils.MemClear(f4_out, ssmd_datacount * 16);
                                                    //for (int i = 0; i < ssmd_datacount; i++) f4_out[i] = 0f;
                                                }
                                                else
                                                {
                                                    for (int i = 0; i < ssmd_datacount; i++) f4_out[i] = math.select(0, 1f, f4_in[i] != 0);
                                                }
                                                return;

                                            case blast_operation.or:
                                                if (constant == 0)
                                                {
                                                    // nothing changes if its the same buffer so skip doing stuff in that case
                                                    if (output != buffer)
                                                    {
                                                        // if not the same buffer then we still need to copy the data 
                                                        //for (int i = 0; i < ssmd_datacount; i++) f4_out[i] = f4_in[i];
                                                        UnsafeUtils.MemCpy(f4_out, f4_in, ssmd_datacount * 16);
                                                    }
                                                }
                                                else
                                                {
                                                    // just write true,  | true is always true 
                                                    UnsafeUtils.Ones(f4_out, ssmd_datacount);
                                                    // for (int i = 0; i < ssmd_datacount; i++) f4_out[i] = 1f;
                                                }
                                                return;

                                            case blast_operation.min:
                                                for (int i = 0; i < ssmd_datacount; i++) f4_out[i] = math.min(f4_in[i], constant);
                                                return;

                                            case blast_operation.max:
                                                for (int i = 0; i < ssmd_datacount; i++) f4_out[i] = math.max(f4_in[i], constant);
                                                return;
                                        }
                                    }
                                    break;
                            }
                            break;
                    }
                    break;

                case BlastVariableDataType.Bool32:
                    if (vin_size == 1)
                    {
                        float* f1_in = (float*)buffer;

                        switch (vout_size)
                        {
                            case 1:
                                {
                                    float* f1_out = (float*)output;

                                    switch (op)
                                    {
                                        case blast_operation.all:
                                            bool ball = Bool32.From(constant).All;
                                            if (ball)
                                            {
                                                UnsafeUtils.MemCpy(f1_out, f1_in, ssmd_datacount * 4);
                                                // for (int i = 0; i < ssmd_datacount; i++) f1_out[i] = f1_in[i];
                                            }
                                            else
                                            {
                                                UnsafeUtils.MemClear(f1_out, ssmd_datacount * 1);
                                                //for (int i = 0; i < ssmd_datacount; i++) f1_out[i] = 0f;
                                            }
                                            return;

                                        case blast_operation.any:
                                            bool bany = constant != 0;
                                            if (bany)
                                            {
                                                constant = 1f;
                                                UnsafeUtils.MemCpyReplicate(f1_out, &constant, 4, ssmd_datacount);
                                                //for (int i = 0; i < ssmd_datacount; i++) f1_out[i] = 1f;
                                            }
                                            else
                                            {
                                                UnsafeUtils.MemCpy(f1_out, f1_in, ssmd_datacount * 4);
                                                //for (int i = 0; i < ssmd_datacount; i++) f1_out[i] = f1_in[i];
                                            }
                                            return;

                                    }
                                }
                                break;

                            case 0:
                            case 4:
                                {
                                    float4* f4_out = (float4*)output;
                                    switch (op)
                                    {
                                        case blast_operation.all:
                                            bool ball = Bool32.From(constant).All;
                                            if (ball)
                                            {
                                                UnsafeUtils.MemCpyStride(f4_out, 16, f1_in, 4, 4, ssmd_datacount);
                                                //for (int i = 0; i < ssmd_datacount; i++) f4_out[i].x = f1_in[i];
                                            }
                                            else
                                            {
                                                UnsafeUtils.MemClear(f4_out, ssmd_datacount * 16);
                                                //for (int i = 0; i < ssmd_datacount; i++) f4_out[i].x = 0f;
                                            }

                                            return;

                                        case blast_operation.any:
                                            bool bany = constant != 0;
                                            if (bany)
                                            {
                                                f4 = 1;
                                                UnsafeUtils.MemCpyReplicate(f4_out, &f4, 16, ssmd_datacount);
                                                // for (int i = 0; i < ssmd_datacount; i++) f4_out[i].x = 1f;
                                            }
                                            else
                                            {
                                                UnsafeUtils.MemCpyStride(f4_out, 16, f1_in, 4, 4, ssmd_datacount);
                                                // for (int i = 0; i < ssmd_datacount; i++) f4_out[i].x = f1_in[i];
                                            }
                                            return;

                                    }
                                }
                                break;
                        }
                    }
                    // other vectorsize then 1 should result in the error below
                    break;

            }


#if DEVELOPMENT_BUILD || TRACE
            Debug.LogError($"blast.pop_fx_with_op_into_fx_constant_handler: -> codepointer {code_pointer} unsupported operation supplied: {op}, datatype: {datatype}, Vin: {vin_size}, Vout: {vout_size}");
            return;
#endif
        }

        void pop_fx_with_op_into_fx_data_handler<Tin, Tout, Tdata>([NoAlias] Tin* buffer, [NoAlias] Tout* output, blast_operation op, [NoAlias] void** data, int offset, bool is_aligned, int data_rowsize, BlastVariableDataType datatype)
            where Tin : unmanaged
            where Tout : unmanaged
        {
            int vin_size = sizeof(Tin) >> 2;
            int vout_size = sizeof(Tout) >> 2;
            int vdata_size = vin_size;

            switch (datatype)
            {
                case BlastVariableDataType.Numeric:
                    switch (vin_size)
                    {
                        case 1:
                            float* f1_in = (float*)(void*)buffer;
                            float** f1_data = (float**)(void**)data;
                            switch (vout_size)
                            {
                                // pop f1 with op into f1 => data == f1
                                case 1:
                                    float* f1_out = (float*)output;
                                    switch (op)
                                    {
                                        case blast_operation.multiply:
                                            simd.mul_array_f1f1_indexed<float>(f1_out, f1_in, f1_data, offset, is_aligned, data_rowsize, ssmd_datacount);
                                            return;

                                        case blast_operation.add:
                                            simd.add_array_f1f1_indexed<float>(f1_out, f1_in, f1_data, offset, is_aligned, data_rowsize, ssmd_datacount);
                                            return;

                                        case blast_operation.substract:
                                            simd.sub_array_f1f1_indexed<float>(f1_out, f1_in, f1_data, offset, is_aligned, data_rowsize, ssmd_datacount);
                                            return;

                                        case blast_operation.divide:
                                            simd.div_array_f1f1_indexed<float>(f1_out, f1_in, f1_data, offset, is_aligned, data_rowsize, ssmd_datacount);
                                            return;

                                        case blast_operation.and:
                                            for (int i = 0; i < ssmd_datacount; i++) f1_out[i] = math.select(0, 1f, f1_in[i] != f1_data[i][offset]);
                                            return;

                                        case blast_operation.or:
                                            for (int i = 0; i < ssmd_datacount; i++) f1_out[i] = math.select(0, 1f, f1_in[i] != 0f || f1_data[i][offset] != 0f);
                                            return;

                                        case blast_operation.min:
                                            for (int i = 0; i < ssmd_datacount; i++) f1_out[i] = math.min(f1_in[i], f1_data[i][offset]);
                                            return;

                                        case blast_operation.max:
                                            for (int i = 0; i < ssmd_datacount; i++) f1_out[i] = math.max(f1_in[i], f1_data[i][offset]);
                                            return;

                                        // csum mina and maxa will always result in a size1 vector, mina on size1 is the same as min  
                                        case blast_operation.csum:
                                        case blast_operation.mina:
                                        case blast_operation.maxa:
                                            for (int i = 0; i < ssmd_datacount; i++) f1_out[i] = f1_data[i][offset];
                                            return;

                                    }
                                    break;

                                // pop f1 with op into f4 => data == f1 
                                case 4:
                                    float4* f4_out = (float4*)output;
                                    switch (op)
                                    {
                                        case blast_operation.multiply:
                                            simd.mul_array_f1f1_indexed<float4>((float4*)output, f1_in, f1_data, offset, is_aligned, data_rowsize, ssmd_datacount);
                                            return;

                                        case blast_operation.add:
                                            simd.add_array_f1f1_indexed<float4>((float4*)output, f1_in, f1_data, offset, is_aligned, data_rowsize, ssmd_datacount);
                                            return;

                                        case blast_operation.substract:
                                            simd.sub_array_f1f1_indexed<float4>((float4*)output, f1_in, f1_data, offset, is_aligned, data_rowsize, ssmd_datacount);
                                            return;

                                        case blast_operation.divide:
                                            simd.div_array_f1f1_indexed<float4>((float4*)output, f1_in, f1_data, offset, is_aligned, data_rowsize, ssmd_datacount);
                                            return;

                                        case blast_operation.and:
                                            for (int i = 0; i < ssmd_datacount; i++) f4_out[i].x = math.select(0, 1f, f1_in[i] != f1_data[i][offset]);
                                            return;

                                        case blast_operation.or:
                                            for (int i = 0; i < ssmd_datacount; i++) f4_out[i].x = math.select(0, 1f, f1_in[i] != 0f || f1_data[i][offset] != 0f);
                                            return;

                                        case blast_operation.min:
                                            for (int i = 0; i < ssmd_datacount; i++) f4_out[i].x = math.min(f1_in[i], f1_data[i][offset]);
                                            return;

                                        case blast_operation.max:
                                            for (int i = 0; i < ssmd_datacount; i++) f4_out[i].x = math.max(f1_in[i], f1_data[i][offset]);
                                            return;

                                        case blast_operation.mina:
                                            for (int i = 0; i < ssmd_datacount; i++) f4_out[i].x = f1_data[i][offset];
                                            return;

                                        case blast_operation.maxa:
                                            for (int i = 0; i < ssmd_datacount; i++) f4_out[i].x = f1_data[i][offset];
                                            return;

                                        case blast_operation.csum:
                                            // csum is somewhat undefined on 1 param.. 
                                            for (int i = 0; i < ssmd_datacount; i++) f4_out[i].x = f1_in[i] + f1_data[i][offset];
                                            return;
                                    }
                                    break;

                            }
                            break;


                        case 2:
                            float2* f2_in = (float2*)(void*)buffer;

                            switch (vout_size)
                            {
                                // pop f2 with op into f2 ==>  data == f2 or f1 but we fix it to inputsize (dont call this function otherwise)
                                case 2:
                                    {
                                        float2* f2_out = (float2*)(void*)output;
                                        switch (op)
                                        {
                                            case blast_operation.multiply:
                                                simd.mul_array_f2f2_indexed<float2>(f2_out, f2_in, (float**)data, offset, is_aligned, data_rowsize, ssmd_datacount);
                                                // f2_out[i] = f2_in[i] * ((float2*)(void*)&((float*)data[i])[offset])[0];
                                                return;

                                            case blast_operation.add:
                                                simd.add_array_f2f2_indexed<float2>(f2_out, f2_in, (float**)data, offset, is_aligned, data_rowsize, ssmd_datacount);
                                                return;

                                            case blast_operation.substract:
                                                simd.sub_array_f2f2_indexed<float2>(f2_out, f2_in, (float**)data, offset, is_aligned, data_rowsize, ssmd_datacount);
                                                return;

                                            case blast_operation.divide:
                                                simd.div_array_f2f2_indexed<float2>(f2_out, f2_in, (float**)data, offset, is_aligned, data_rowsize, ssmd_datacount);
                                                return;

                                            case blast_operation.and:
                                                for (int i = 0; i < ssmd_datacount; i++) f2_out[i] = math.select(0, 1f, math.any(f2_in[i]) && math.any(((float2*)(void*)&((float*)data[i])[offset])[0]));
                                                return;

                                            case blast_operation.or:
                                                for (int i = 0; i < ssmd_datacount; i++) f2_out[i] = math.select(0, 1f, math.any(f2_in[i]) || math.any(((float2*)(void*)&((float*)data[i])[offset])[0]));
                                                return;

                                            case blast_operation.min:
                                                for (int i = 0; i < ssmd_datacount; i++) f2_out[i] = math.min(f2_in[i], ((float2*)(void*)&((float*)data[i])[offset])[0]);
                                                return;

                                            case blast_operation.max:
                                                for (int i = 0; i < ssmd_datacount; i++) f2_out[i] = math.max(f2_in[i], ((float2*)(void*)&((float*)data[i])[offset])[0]);
                                                return;
                                        }
                                    }
                                    break;

                                // pop f2 with op into f4
                                case 4:
                                    {
                                        float4* f4_out = (float4*)(void*)output;
                                        switch (op)
                                        {
                                            case blast_operation.multiply:
                                                simd.mul_array_f2f2_indexed<float4>(f4_out, f2_in, (float**)data, offset, is_aligned, data_rowsize, ssmd_datacount);
                                                return;

                                            case blast_operation.add:
                                                simd.add_array_f2f2_indexed<float4>(f4_out, f2_in, (float**)data, offset, is_aligned, data_rowsize, ssmd_datacount);
                                                return;

                                            case blast_operation.substract:
                                                simd.sub_array_f2f2_indexed<float4>(f4_out, f2_in, (float**)data, offset, is_aligned, data_rowsize, ssmd_datacount);
                                                return;

                                            case blast_operation.divide:
                                                simd.div_array_f2f2_indexed<float4>(f4_out, f2_in, (float**)data, offset, is_aligned, data_rowsize, ssmd_datacount);
                                                return;

                                            case blast_operation.and:
                                                for (int i = 0; i < ssmd_datacount; i++) f4_out[i].xy = math.select(0, 1f, math.any(f2_in[i]) && math.any(((float2*)(void*)&((float*)data[i])[offset])[0]));
                                                return;

                                            case blast_operation.or:
                                                for (int i = 0; i < ssmd_datacount; i++) f4_out[i].xy = math.select(0, 1f, math.any(f2_in[i]) || math.any(((float2*)(void*)&((float*)data[i])[offset])[0]));
                                                return;

                                            case blast_operation.min:
                                                for (int i = 0; i < ssmd_datacount; i++) f4_out[i].xy = math.min(f2_in[i], ((float2*)(void*)&((float*)data[i])[offset])[0]);
                                                return;

                                            case blast_operation.max:
                                                for (int i = 0; i < ssmd_datacount; i++) f4_out[i].xy = math.max(f2_in[i], ((float2*)(void*)&((float*)data[i])[offset])[0]);
                                                return;
                                        }
                                    }
                                    break;
                            }
                            break;

                        case 3:
                            float3* f3_in = (float3*)(void*)buffer;
                            switch (vout_size)
                            {
                                // pop f3 with op into f3
                                case 3:
                                    {
                                        float3* f3_out = (float3*)(void*)output;
                                        switch (op)
                                        {
                                            case blast_operation.multiply:
                                                simd.mul_array_f3f3_indexed<float3>(f3_out, f3_in, (float**)data, offset, is_aligned, data_rowsize, ssmd_datacount);
                                                return;

                                            case blast_operation.add:
                                                simd.add_array_f3f3_indexed<float3>(f3_out, f3_in, (float**)data, offset, is_aligned, data_rowsize, ssmd_datacount);
                                                return;

                                            case blast_operation.substract:
                                                simd.sub_array_f3f3_indexed<float3>(f3_out, f3_in, (float**)data, offset, is_aligned, data_rowsize, ssmd_datacount);
                                                return;

                                            case blast_operation.divide:
                                                simd.div_array_f3f3_indexed<float3>(f3_out, f3_in, (float**)data, offset, is_aligned, data_rowsize, ssmd_datacount);
                                                return;

                                            case blast_operation.and:
                                                for (int i = 0; i < ssmd_datacount; i++) f3_out[i] = math.select(0, 1f, math.any(f3_in[i]) && math.any(((float3*)(void*)&((float*)data[i])[offset])[0]));
                                                return;

                                            case blast_operation.or:
                                                for (int i = 0; i < ssmd_datacount; i++) f3_out[i] = math.select(0, 1f, math.any(f3_in[i]) || math.any(((float3*)(void*)&((float*)data[i])[offset])[0]));
                                                return;

                                            case blast_operation.min:
                                                for (int i = 0; i < ssmd_datacount; i++) f3_out[i] = math.min(f3_in[i], ((float3*)(void*)&((float*)data[i])[offset])[0]);
                                                return;

                                            case blast_operation.max:
                                                for (int i = 0; i < ssmd_datacount; i++) f3_out[i] = math.max(f3_in[i], ((float3*)(void*)&((float*)data[i])[offset])[0]);
                                                return;
                                        }
                                    }
                                    break;

                                // pop f3 with op into f4
                                case 4:
                                    {
                                        float4* f4_out = (float4*)(void*)output;
                                        switch (op)
                                        {
                                            case blast_operation.multiply:
                                                simd.mul_array_f3f3_indexed<float4>(f4_out, f3_in, (float**)data, offset, is_aligned, data_rowsize, ssmd_datacount);
                                                return;

                                            case blast_operation.add:
                                                simd.add_array_f3f3_indexed<float4>(f4_out, f3_in, (float**)data, offset, is_aligned, data_rowsize, ssmd_datacount);
                                                return;

                                            case blast_operation.substract:
                                                simd.sub_array_f3f3_indexed<float4>(f4_out, f3_in, (float**)data, offset, is_aligned, data_rowsize, ssmd_datacount);
                                                return;

                                            case blast_operation.divide:
                                                simd.div_array_f3f3_indexed<float4>(f4_out, f3_in, (float**)data, offset, is_aligned, data_rowsize, ssmd_datacount);
                                                return;

                                            case blast_operation.and:
                                                for (int i = 0; i < ssmd_datacount; i++) f4_out[i].xyz = math.select(0, 1f, math.any(f3_in[i]) && math.any(((float3*)(void*)&((float*)data[i])[offset])[0]));
                                                return;

                                            case blast_operation.or:
                                                for (int i = 0; i < ssmd_datacount; i++) f4_out[i].xyz = math.select(0, 1f, math.any(f3_in[i]) || math.any(((float3*)(void*)&((float*)data[i])[offset])[0]));
                                                return;

                                            case blast_operation.min:
                                                for (int i = 0; i < ssmd_datacount; i++) f4_out[i].xyz = math.min(f3_in[i], ((float3*)(void*)&((float*)data[i])[offset])[0]);
                                                return;

                                            case blast_operation.max:
                                                for (int i = 0; i < ssmd_datacount; i++) f4_out[i].xyz = math.max(f3_in[i], ((float3*)(void*)&((float*)data[i])[offset])[0]);
                                                return;
                                        }
                                    }
                                    break;
                            }
                            break;

                        case 4:
                            // vout_size can only be 4   
                            {
                                float4* f4_in = (float4*)buffer;
                                float4* f4_out = (float4*)output;
                                switch (op)
                                {
                                    case blast_operation.multiply:
                                        simd.mul_array_f4f4_indexed(f4_out, f4_in, (float**)data, offset, is_aligned, data_rowsize, ssmd_datacount);
                                        return;

                                    case blast_operation.add:
                                        simd.add_array_f4f4_indexed(f4_out, f4_in, (float**)data, offset, is_aligned, data_rowsize, ssmd_datacount);
                                        return;

                                    case blast_operation.substract:
                                        simd.sub_array_f4f4_indexed(f4_out, f4_in, (float**)data, offset, is_aligned, data_rowsize, ssmd_datacount);
                                        return;

                                    case blast_operation.divide:
                                        simd.div_array_f4f4_indexed(f4_out, f4_in, (float**)data, offset, is_aligned, data_rowsize, ssmd_datacount);
                                        return;

                                    case blast_operation.and:
                                        for (int i = 0; i < ssmd_datacount; i++) f4_out[i] = math.select(0, 1f, f4_in[i] != ((float4*)(void*)&((float*)data[i])[offset])[0]);
                                        return;

                                    case blast_operation.or:
                                        for (int i = 0; i < ssmd_datacount; i++) f4_out[i] = math.select(0, 1f, math.any(f4_in[i]) || math.any(((float4*)(void*)&((float*)data[i])[offset])[0]));
                                        return;

                                    case blast_operation.min:
                                        for (int i = 0; i < ssmd_datacount; i++) f4_out[i] = math.min(f4_in[i], ((float4*)(void*)&((float*)data[i])[offset])[0]);
                                        return;

                                    case blast_operation.max:
                                        for (int i = 0; i < ssmd_datacount; i++) f4_out[i] = math.max(f4_in[i], ((float4*)(void*)&((float*)data[i])[offset])[0]);
                                        return;
                                }

                            }
                            break;
                    }
                    break;



                case BlastVariableDataType.Bool32:
                    {
                        switch (vin_size)
                        {
                            case 1:
                                float* f1_in = (float*)(void*)buffer;
                                float** f1_data = (float**)(void**)data;
                                switch (vout_size)
                                {
                                    // pop f1 with op into f1 => data == f1
                                    case 1:
                                        float* f1_out = (float*)output;
                                        switch (op)
                                        {
                                            case blast_operation.all:
                                                for (int i = 0; i < ssmd_datacount; i++)
                                                {
                                                    f1_out[i] = math.select(0f, 1f, f1_in[i] == 1f && ((Bool32*)(void*)&f1_data[i][offset])->All);
                                                }
                                                break;

                                            case blast_operation.any:
                                                for (int i = 0; i < ssmd_datacount; i++)
                                                {
                                                    f1_out[i] = math.select(0f, 1f, f1_in[i] == 1f || f1_data[i][offset] != 0f);
                                                }
                                                break;

                                        }
                                        break;

                                    case 4:
                                        float4* f4_out = (float4*)output;
                                        switch (op)
                                        {
                                            case blast_operation.all:
                                                for (int i = 0; i < ssmd_datacount; i++)
                                                {
                                                    f4_out[i].x = math.select(0f, 1f, f1_in[i] == 1f && ((Bool32*)(void*)&f1_data[i][offset])->All);
                                                }
                                                break;

                                            case blast_operation.any:
                                                for (int i = 0; i < ssmd_datacount; i++)
                                                {
                                                    f4_out[i].x = math.select(0f, 1f, f1_in[i] == 1f || f1_data[i][offset] != 0f);
                                                }
                                                break;
                                        }
                                        break;
                                }
                                break;
                        }
                    }
                    break;
            }


#if DEVELOPMENT_BUILD || TRACE
            Debug.LogError($"blast.pop_fx_with_op_into_fx_data_handler: -> codepointer {code_pointer} unsupported operation supplied: {op}, datatype: {datatype}, Vin: {vin_size}, Vout: {vout_size}");
            return;
#endif

        }

        void pop_fx_with_op_into_fx_ref<Tin, Tout>(ref int code_pointer, [NoAlias] Tin* buffer, [NoAlias] Tout* output, blast_operation op, BlastVariableDataType datatype = BlastVariableDataType.Numeric, bool autoexpand = false)
            where Tin : unmanaged
            where Tout : unmanaged
        {
#if DEVELOPMENT_BUILD || TRACE
            Assert.IsFalse(buffer == null, $"blast.ssmd.interpretor.pop_fx_with_op_into_fx_ref: buffer NULL");
            Assert.IsFalse(output == null, $"blast.ssmd.interpretor.pop_fx_with_op_into_fx_ref: output NULL");

            if (!Blast.IsOperationSSMDHandled(op))
            {
                Debug.LogError($"blast.pop_fx_with_op_into_fx_ref: -> unsupported operation supplied: {op}, only + - * / are supported");
                return;
            }
#endif

            // we get either a pop instruction or an instruction to get a value 
            byte c = code[code_pointer];
            code_pointer++;


#if DEVELOPMENT_BUILD || TRACE
            BlastVariableDataType type;
#endif

            int size;
            float constant;
            byte* bf = stackalloc byte[4];

            int tin_size = sizeof(Tin) >> 2;

            switch ((blast_operation)c)
            {
                //
                // stack pop 
                // 
                case blast_operation.pop:

                    size = GetStackMetaDataSize(stack_offset);
#if DEVELOPMENT_BUILD || TRACE
                    type = GetStackMetaDataType(stack_offset);
                    if ((size != tin_size || (size == 0 && tin_size != 4)) || type != datatype)
                    {
                        Debug.LogError($"blast.pop_fx_with_op_into_fx_ref: stackdata mismatch, expecting {datatype} of size {tin_size}, found {type} of size {size} at stack offset {stack_offset} with operation: {op}");
                        return;
                    }
#endif
                    // stack pop 
                    stack_offset = stack_offset - size; // size in nr of elements and stackoffset in elementcount 
                    pop_fx_with_op_into_fx_data_handler<Tin, Tout, Tin>(buffer, output, op, stack, stack_offset, stack_is_aligned, stack_rowsize, datatype);
                    break;

                case blast_operation.constant_f1:
                    {
                        code_pointer--;
                        constant = read_constant_f1(code, ref code_pointer); 
                        pop_fx_with_op_into_fx_constant_handler<Tin, Tout>(buffer, output, op, constant, datatype);
                    }
                    break;

                case blast_operation.constant_f1_h:
                    {
                        code_pointer--;
                        constant = read_constant_f1_h(code, ref code_pointer);
                        pop_fx_with_op_into_fx_constant_handler<Tin, Tout>(buffer, output, op, constant, datatype);
                    }
                    break;

                case blast_operation.constant_short_ref:
                case blast_operation.constant_long_ref:
                    {
                        code_pointer--;
                        if (follow_constant_ref(code, ref code_pointer, out BlastVariableDataType data_type, out int reference_index, c == (byte)blast_operation.constant_short_ref))
                        {
                            constant = read_constant_float(code, reference_index);
                            pop_fx_with_op_into_fx_constant_handler<Tin, Tout>(buffer, output, op, constant, datatype);

                        }
                        else
                        {
                            if (IsTrace)
                            {
                                Debug.LogError($"blast.ssmd.pop_fx_with_op_into_fx_ref: constant reference error, constant reference at {code_pointer - 1} points at invalid operation");
                            }
                        }
                    }
                    break;

                case blast_operation.pi:
                case blast_operation.inv_pi:
                case blast_operation.epsilon:
                case blast_operation.infinity:
                case blast_operation.negative_infinity:
                case blast_operation.nan:
                case blast_operation.min_value:
                case blast_operation.value_0:
                case blast_operation.value_1:
                case blast_operation.value_2:
                case blast_operation.value_3:
                case blast_operation.value_4:
                case blast_operation.value_8:
                case blast_operation.value_10:
                case blast_operation.value_16:
                case blast_operation.value_24:
                case blast_operation.value_32:
                case blast_operation.value_64:
                case blast_operation.value_100:
                case blast_operation.value_128:
                case blast_operation.value_256:
                case blast_operation.value_512:
                case blast_operation.value_1000:
                case blast_operation.value_1024:
                case blast_operation.value_30:
                case blast_operation.value_45:
                case blast_operation.value_90:
                case blast_operation.value_180:
                case blast_operation.value_270:
                case blast_operation.value_360:
                case blast_operation.inv_value_2:
                case blast_operation.inv_value_3:
                case blast_operation.inv_value_4:
                case blast_operation.inv_value_8:
                case blast_operation.inv_value_10:
                case blast_operation.inv_value_16:
                case blast_operation.inv_value_24:
                case blast_operation.inv_value_32:
                case blast_operation.inv_value_64:
                case blast_operation.inv_value_100:
                case blast_operation.inv_value_128:
                case blast_operation.inv_value_256:
                case blast_operation.inv_value_512:
                case blast_operation.inv_value_1000:
                case blast_operation.inv_value_1024:
                case blast_operation.inv_value_30:
                case blast_operation.framecount:
                case blast_operation.fixedtime:
                case blast_operation.time:
                case blast_operation.fixeddeltatime:
                case blast_operation.deltatime:
                    //
                    // CONSTANT operation                         
                    //
                    pop_fx_with_op_into_fx_constant_handler<Tin, Tout>(buffer, output, op, engine_ptr->constants[c], datatype);
                    break;


                default:
                case blast_operation.id:
                    //
                    // DATASEG operation
                    // 
#if DEVELOPMENT_BUILD || TRACE
                    type = BlastInterpretor.GetMetaDataType(metadata, (byte)(c - BlastInterpretor.opt_id));
                    size = BlastInterpretor.GetMetaDataSize(metadata, (byte)(c - BlastInterpretor.opt_id));
                    if ((size != tin_size || (size == 0 && tin_size != 4)) || type != datatype)
                    {
                        Debug.LogError($"blast.pop_fx_with_op_into_fx_ref: data mismatch, expecting {datatype} of size {tin_size}, found {type} of size {size} at data offset {c - BlastInterpretor.opt_id} with operation: {op}");
                        return;
                    }
#endif
                    pop_fx_with_op_into_fx_data_handler<Tin, Tout, Tin>(buffer, output, op, data, (byte)(c - BlastInterpretor.opt_id), data_is_aligned, data_rowsize, datatype);
                    break;
            }
        }



        void assign_pop_f([NoAlias] void* temp, ref int code_pointer, in int vector_size, int assignee_index)
        {
            switch (vector_size)
            {
                case 0:
                case 4:
                    pop_fx_into_ref<float>(ref code_pointer, null, data, assignee_index + 0, data_is_aligned, data_rowsize, ssmd_datacount);
                    pop_fx_into_ref<float>(ref code_pointer, null, data, assignee_index + 1, data_is_aligned, data_rowsize, ssmd_datacount);
                    pop_fx_into_ref<float>(ref code_pointer, null, data, assignee_index + 2, data_is_aligned, data_rowsize, ssmd_datacount);
                    pop_fx_into_ref<float>(ref code_pointer, null, data, assignee_index + 3, data_is_aligned, data_rowsize, ssmd_datacount);
                    break;

                case 1:
                    pop_fx_into_ref<float>(ref code_pointer, null, data, assignee_index, data_is_aligned, data_rowsize, ssmd_datacount);
                    break;

                case 2:
                    pop_fx_into_ref<float>(ref code_pointer, null, data, assignee_index + 0, data_is_aligned, data_rowsize, ssmd_datacount);
                    pop_fx_into_ref<float>(ref code_pointer, null, data, assignee_index + 1, data_is_aligned, data_rowsize, ssmd_datacount);
                    break;

                case 3:
                    pop_fx_into_ref<float>(ref code_pointer, null, data, assignee_index + 0, data_is_aligned, data_rowsize, ssmd_datacount);
                    pop_fx_into_ref<float>(ref code_pointer, null, data, assignee_index + 1, data_is_aligned, data_rowsize, ssmd_datacount);
                    pop_fx_into_ref<float>(ref code_pointer, null, data, assignee_index + 2, data_is_aligned, data_rowsize, ssmd_datacount);
                    break;

#if DEVELOPMENT_BUILD || TRACE
                default:
                    Debug.LogError($"valst.ssmd.assign_pop_f: vector_size {vector_size} not supported at code pointer {code_pointer}");
                    break;
#endif

            }
        }

        /// <summary>
        /// read codebyte, determine data location, pop data and push|assign it on the dataoffset 
        /// </summary>
        void push_pop_f([NoAlias] void* temp, ref int code_pointer, in int vector_size)
        {
            float* t1 = (float*)(void*)temp;
            float* t2 = &t1[ssmd_datacount];
            float* t3 = &t2[ssmd_datacount];
            float* t4 = &t3[ssmd_datacount];

            bool c1, c2, c3, c4;

            // could be that instructions below pop 1 then we push, this will result in wrong stack setting when multiple values are popped
            switch (vector_size)
            {
                case 0:
                case 4:
                    {
                        c1 = pop_fx_into_ref<float>(ref code_pointer, t1, null, -1, false, 0, ssmd_datacount, true);
                        c2 = pop_fx_into_ref<float>(ref code_pointer, t2, null, -1, false, 0, ssmd_datacount, true);
                        c3 = pop_fx_into_ref<float>(ref code_pointer, t3, null, -1, false, 0, ssmd_datacount, true);
                        c4 = pop_fx_into_ref<float>(ref code_pointer, t4, null, -1, false, 0, ssmd_datacount, true);

                        SetStackMetaData(BlastVariableDataType.Numeric, 4, (byte)stack_offset);

                        if (c1 || c2 || c3 || c4)
                        {
                            if (c1) simd.move_f1_constant_to_indexed_data_as_f1(stack, stack_rowsize, stack_is_aligned, stack_offset + 0, t1[0], ssmd_datacount);
                            else    simd.move_f1_array_to_indexed_data_as_f1(stack, stack_rowsize, stack_is_aligned, stack_offset + 0, t1, ssmd_datacount);
                            if (c2) simd.move_f1_constant_to_indexed_data_as_f1(stack, stack_rowsize, stack_is_aligned, stack_offset + 1, t2[0], ssmd_datacount);
                            else    simd.move_f1_array_to_indexed_data_as_f1(stack, stack_rowsize, stack_is_aligned, stack_offset + 1, t2, ssmd_datacount);
                            if (c3) simd.move_f1_constant_to_indexed_data_as_f1(stack, stack_rowsize, stack_is_aligned, stack_offset + 2, t3[0], ssmd_datacount);
                            else    simd.move_f1_array_to_indexed_data_as_f1(stack, stack_rowsize, stack_is_aligned, stack_offset + 2, t3, ssmd_datacount);
                            if (c4) simd.move_f1_constant_to_indexed_data_as_f1(stack, stack_rowsize, stack_is_aligned, stack_offset + 3, t4[0], ssmd_datacount);
                            else    simd.move_f1_array_to_indexed_data_as_f1(stack, stack_rowsize, stack_is_aligned, stack_offset + 3, t4, ssmd_datacount);
                        }
                        else
                        {
                            simd.move_f1_constant_to_indexed_data_as_f1(stack, stack_rowsize, stack_is_aligned, stack_offset + 0, t1[0], ssmd_datacount);
                            simd.move_f1_constant_to_indexed_data_as_f1(stack, stack_rowsize, stack_is_aligned, stack_offset + 1, t2[0], ssmd_datacount);
                            simd.move_f1_constant_to_indexed_data_as_f1(stack, stack_rowsize, stack_is_aligned, stack_offset + 2, t3[0], ssmd_datacount);
                            simd.move_f1_constant_to_indexed_data_as_f1(stack, stack_rowsize, stack_is_aligned, stack_offset + 3, t4[0], ssmd_datacount);
                        }

                        stack_offset += 4;  // size 4
                    }
                    break;

                case 1:
                    {
                        // if pop_fx_into_ref returns a constant it should only set the first records when writing to a destination* 
                        // - when reading writing to stack or data it should read/fill all 
                        c1 = pop_fx_into_ref<float>(ref code_pointer, t1, null, -1, false, 0, ssmd_datacount, true);  //null, stack, stack_offset);

                        SetStackMetaData(BlastVariableDataType.Numeric, 1, (byte)stack_offset);

                        if (c1) simd.move_f1_constant_to_indexed_data_as_f1(stack, stack_rowsize, stack_is_aligned, stack_offset + 0, t1[0], ssmd_datacount);
                        else simd.move_f1_array_to_indexed_data_as_f1(stack, stack_rowsize, stack_is_aligned, stack_offset + 0, t1, ssmd_datacount);

                    }
                    stack_offset += 1;
                    break;

                case 2:
                    {
                        c1 = pop_fx_into_ref<float>(ref code_pointer, t1, null, -1, false, 0, ssmd_datacount, true);
                        c2 = pop_fx_into_ref<float>(ref code_pointer, t2, null, -1, false, 0, ssmd_datacount, true);

                        SetStackMetaData(BlastVariableDataType.Numeric, 2, (byte)stack_offset);

                        if (c1) simd.move_f1_constant_to_indexed_data_as_f1(stack, stack_rowsize, stack_is_aligned, stack_offset + 0, t1[0], ssmd_datacount);
                        else simd.move_f1_array_to_indexed_data_as_f1(stack, stack_rowsize, stack_is_aligned, stack_offset + 0, t1, ssmd_datacount);
                        if (c2) simd.move_f1_constant_to_indexed_data_as_f1(stack, stack_rowsize, stack_is_aligned, stack_offset + 1, t2[0], ssmd_datacount);
                        else simd.move_f1_array_to_indexed_data_as_f1(stack, stack_rowsize, stack_is_aligned, stack_offset + 1, t2, ssmd_datacount);

                        stack_offset += 2;
                    }
                    break;

                case 3:
                    {
                        c1 = pop_fx_into_ref<float>(ref code_pointer, t1, null, -1, false, 0, ssmd_datacount, true);
                        c2 = pop_fx_into_ref<float>(ref code_pointer, t2, null, -1, false, 0, ssmd_datacount, true);
                        c3 = pop_fx_into_ref<float>(ref code_pointer, t3, null, -1, false, 0, ssmd_datacount, true);

                        SetStackMetaData(BlastVariableDataType.Numeric, 3, (byte)stack_offset);

                        if (c1) simd.move_f1_constant_to_indexed_data_as_f1(stack, stack_rowsize, stack_is_aligned, stack_offset + 0, t1[0], ssmd_datacount);
                        else simd.move_f1_array_to_indexed_data_as_f1(stack, stack_rowsize, stack_is_aligned, stack_offset + 0, t1, ssmd_datacount);
                        if (c2) simd.move_f1_constant_to_indexed_data_as_f1(stack, stack_rowsize, stack_is_aligned, stack_offset + 1, t2[0], ssmd_datacount);
                        else simd.move_f1_array_to_indexed_data_as_f1(stack, stack_rowsize, stack_is_aligned, stack_offset + 1, t2, ssmd_datacount);
                        if (c3) simd.move_f1_constant_to_indexed_data_as_f1(stack, stack_rowsize, stack_is_aligned, stack_offset + 2, t3[0], ssmd_datacount);
                        else simd.move_f1_array_to_indexed_data_as_f1(stack, stack_rowsize, stack_is_aligned, stack_offset + 2, t3, ssmd_datacount);

                        stack_offset += 3;
                    }
                    break;

#if DEVELOPMENT_BUILD || TRACE
                default:
                    Debug.LogError($"blast.ssmd.push_pop_f: vector_size {vector_size} not supported at code pointer {code_pointer}");
                    break;
#endif
            }
        }


        #endregion

        #region set_data_from_register_fx

        /// <summary>
        /// set a float1 data location from register location  
        /// </summary>
        /// <param name="index">data index</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void set_data_from_register_f1(in int index)
        {
            simd.move_f4_array_to_indexed_data_as_f1(data, data_rowsize, data_is_aligned, index, register, ssmd_datacount);
        }

        /// <summary>
        /// set a float1 data location from register location  
        /// </summary>
        /// <param name="index">data index</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void set_data_from_register_f1_n(in int index)
        {
            simd.move_f4_array_to_indexed_data_as_f1_negated(data, data_rowsize, data_is_aligned, index, register, ssmd_datacount);
        }

        /// <summary>
        /// set a float2 data location from register location  
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void set_data_from_register_f2(in int index)
        {
            simd.move_f4_array_to_indexed_data_as_f2(data, data_rowsize, data_is_aligned, index, register, ssmd_datacount);
        }

        /// <summary>
        /// set a float2 data location from register location  
        /// </summary>
        /// <param name="index">data index</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void set_data_from_register_f2_n(in int index)
        {
            simd.move_f4_array_to_indexed_data_as_f2_negated(data, data_rowsize, data_is_aligned, index, register, ssmd_datacount);
        }

        /// <summary>
        /// set a float3 data location from register location  
        /// </summary>
        /// <param name="index">data index</param>
        void set_data_from_register_f3(in int index)
        {
            simd.move_f4_array_to_indexed_data_as_f3(data, data_rowsize, data_is_aligned, index, register, ssmd_datacount);
        }

        /// <summary>
        /// set a float3 data location from register location  
        /// </summary>
        /// <param name="index">data index</param>
        void set_data_from_register_f3_n(in int index)
        {
            simd.move_f4_array_to_indexed_data_as_f3_negated(data, data_rowsize, data_is_aligned, index, register, ssmd_datacount);
        }

        /// <summary>
        /// set a float4 data location from register location  
        /// </summary>
        /// <param name="index">data index</param>
        void set_data_from_register_f4(in int index)
        {
            simd.move_f4_array_to_indexed_data_as_f4(data, data_rowsize, data_is_aligned, index, register, ssmd_datacount);
        }
        /// <summary>
        /// set a float4 data location from register location  
        /// </summary>
        /// <param name="index">data index</param>
        void set_data_from_register_f4_n(in int index)
        {
            simd.move_f4_array_to_indexed_data_as_f4_negated(data, data_rowsize, data_is_aligned, index, register, ssmd_datacount);
        }

        void set_data_from_register(in byte vector_size, in byte assignee, bool minus)
        {
            if (minus)
            {
                // set minus register to assignee
                switch (vector_size)
                {
                    case 1: set_data_from_register_f1_n(assignee); break;
                    case 2: set_data_from_register_f2_n(assignee); break;
                    case 3: set_data_from_register_f3_n(assignee); break;
                    case 0:
                    case 4: set_data_from_register_f4_n(assignee); break;
                }
            }
            else
            {
                // set to assignee
                switch (vector_size)
                {
                    case 1: set_data_from_register_f1(assignee); break;
                    case 2: set_data_from_register_f2(assignee); break;
                    case 3: set_data_from_register_f3(assignee); break;
                    case 0:
                    case 4: set_data_from_register_f4(assignee); break;
                }
            }
        }


        #endregion

        #region op_a | op_a_component | get_sigle_op




        /// <summary>
        /// get result from a single input function
        /// - the parameter fed may be negated, indexed and can be an inlined constant 
        /// - the codepointer must be on the first value to read after op|function opcodes
        /// </summary>
        BlastError get_single_op_result([NoAlias] void* temp, ref int code_pointer, ref byte vector_size, int ssmd_datacount, [NoAlias] float4* register, in blast_operation op, in extended_blast_operation ex_op = extended_blast_operation.nop, int indexed = -1, bool is_negated = false)
        {
            bool constant_index = false;
            byte code_byte = code[code_pointer];
            float* indices = (float*)temp;

            while (true)
            {
                switch ((blast_operation)code_byte)
                {
                    case blast_operation.substract: is_negated = true; code_pointer++; code_byte = code[code_pointer]; continue;

                    // 
                    // case blast_operation.not: is_not = true; code_pointer++; continue;     
                    //
                    // not sure if this should be supported, math.abs(!a) ?? math.sin(!a) ?? => boolean error
                    //

                    case blast_operation.index_x: indexed = 0; code_pointer++; code_byte = code[code_pointer]; constant_index = true; continue;
                    case blast_operation.index_y: indexed = 1; code_pointer++; code_byte = code[code_pointer]; constant_index = true; continue;
                    case blast_operation.index_z: indexed = 2; code_pointer++; code_byte = code[code_pointer]; constant_index = true; continue;
                    case blast_operation.index_w: indexed = 3; code_pointer++; code_byte = code[code_pointer]; constant_index = true; continue;

                    case blast_operation.index_n:
                        {
                            indexed = 4;
                            code_pointer++;

                            constant_index = pop_fx_into_ref<float>(ref code_pointer, indices, ssmd_datacount, true);
                            if (constant_index)
                            {
                                indexed = (int)indices[0];
                            }
                            code_byte = code[code_pointer];
                            continue;
                        }


                    case blast_operation.constant_f1:
                        {
                            // handle operation with inlined constant data 
                            float constant = read_constant_f1(code, ref code_pointer); 
                            code_pointer--; 
                            handle_single_op_constant(register, ref vector_size, ssmd_datacount, constant, op, ex_op, is_negated);
                        }
                        return BlastError.success;

                    case blast_operation.constant_f1_h:
                        {
                            // handle operation with inlined constant data 
                            float constant = read_constant_f1_h(code, ref code_pointer);
                            code_pointer--; 
                            handle_single_op_constant(register, ref vector_size, ssmd_datacount, constant, op, ex_op, is_negated);
                        }
                        return BlastError.success;

                    case blast_operation.constant_long_ref:
                    case blast_operation.constant_short_ref:
                        {
                           if (follow_constant_ref(code, ref code_pointer, out BlastVariableDataType datatype, out int reference_index, code_byte == (byte)blast_operation.constant_short_ref))
                            {
                                float constant = read_constant_float(code, reference_index);
                                code_pointer--;
                                handle_single_op_constant(register, ref vector_size, ssmd_datacount, constant, op, ex_op, is_negated);
                            }
                            else
                            {
                                return BlastError.error_constant_reference_error; 
                            }
                        }
                        return BlastError.success;

                     case blast_operation.cdataref:
                        {
                            CDATAEncodingType encoding = CDATAEncodingType.None;

                            if (BlastInterpretor.follow_cdataref(code, code_pointer, out int offset, out int length, out encoding))
                            {
                                // validate we end up at cdata
                                if (!BlastInterpretor.IsValidCDATAStart(code[offset]))  // at some point we probably replace cdata with a datatype cdata_float etc.. 
                                {
                                    if (IsTrace) Debug.LogError($"Blast.SSMD.Interpretor.get_single_op_result: error in cdata reference at {code_pointer} in encoding {encoding}");
                                    vector_size = 0;
                                    return BlastError.error_failed_to_read_cdata_reference;
                                }
                                else
                                {
                                    // advance past the cdata 
                                    code_pointer = code_pointer + 2; // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
                                    constant_index = constant_index || (indexed >= 0 && indexed < 4);


                                    // indexer at location ? 
                                    if (!constant_index)
                                    {
                                        constant_index = simd.all_equal(indices, ssmd_datacount);
                                    }

                                    // if constant, set 1 otherwise need all 
                                    if (indexed > 3)
                                    {
                                        // each index may be different 
                                        if (constant_index)
                                        {
                                            indexed = (int)indices[0];
                                        }
                                        else
                                        {
                                            // handle each index seperately
                                            for (int i = 0; i < ssmd_datacount; i++)
                                            {
                                                register[i] = BlastInterpretor.index_cdata_f1(code, offset, indexed, length);
                                            }
                                            vector_size = 1;
                                            return BlastError.success;
                                        }
                                    }

                                    // if op to handle is the indexing itself 
                                    switch (op)
                                    {
                                        case blast_operation.index_x: indexed = 0; constant_index = true; break;
                                        case blast_operation.index_y: indexed = 1; constant_index = true; break;
                                        case blast_operation.index_z: indexed = 2; constant_index = true; break;
                                        case blast_operation.index_w: indexed = 3; constant_index = true; break;
                                    }


                                    // unsported access as of this moment, no indexer 
                                    if (indexed == -1)
                                    {
                                        if (IsTrace) Debug.LogError($"Blast.SSMD.Interpretor.get_single_op_result: error in cdata reference at {code_pointer} referencing code[index] {indexed}, cdata must be indexed");
                                        return BlastError.error_failed_to_read_cdata_reference;
                                    }
                                    else
                                    {
                                        // single index returning a constant value 
                                        float constant = BlastInterpretor.index_cdata_f1(code, offset, indexed, length);

                                        // set the constant to all registers
                                        handle_single_op_constant(register, ref vector_size, ssmd_datacount, constant, op, ex_op);

                                        vector_size = 1;
                                    }
                                    return BlastError.success;
                                }
                            }
                        }
                        return BlastError.error_failed_to_read_cdata_reference;

                    case blast_operation.pi:
                    case blast_operation.inv_pi:
                    case blast_operation.epsilon:
                    case blast_operation.infinity:
                    case blast_operation.negative_infinity:
                    case blast_operation.nan:
                    case blast_operation.min_value:
                    case blast_operation.value_0:
                    case blast_operation.value_1:
                    case blast_operation.value_2:
                    case blast_operation.value_3:
                    case blast_operation.value_4:
                    case blast_operation.value_8:
                    case blast_operation.value_10:
                    case blast_operation.value_16:
                    case blast_operation.value_24:
                    case blast_operation.value_32:
                    case blast_operation.value_64:
                    case blast_operation.value_100:
                    case blast_operation.value_128:
                    case blast_operation.value_256:
                    case blast_operation.value_512:
                    case blast_operation.value_1000:
                    case blast_operation.value_1024:
                    case blast_operation.value_30:
                    case blast_operation.value_45:
                    case blast_operation.value_90:
                    case blast_operation.value_180:
                    case blast_operation.value_270:
                    case blast_operation.value_360:
                    case blast_operation.inv_value_2:
                    case blast_operation.inv_value_3:
                    case blast_operation.inv_value_4:
                    case blast_operation.inv_value_8:
                    case blast_operation.inv_value_10:
                    case blast_operation.inv_value_16:
                    case blast_operation.inv_value_24:
                    case blast_operation.inv_value_32:
                    case blast_operation.inv_value_64:
                    case blast_operation.inv_value_100:
                    case blast_operation.inv_value_128:
                    case blast_operation.inv_value_256:
                    case blast_operation.inv_value_512:
                    case blast_operation.inv_value_1000:
                    case blast_operation.inv_value_1024:
                    case blast_operation.inv_value_30:
                    case blast_operation.framecount:
                    case blast_operation.fixedtime:
                    case blast_operation.time:
                    case blast_operation.fixeddeltatime:
                    case blast_operation.deltatime:
                        {
                            // handle operation using constant encoded op
                            float constant = engine_ptr->constants[code_byte];
                            handle_single_op_constant(register, ref vector_size, ssmd_datacount, constant, op, ex_op, is_negated);
                           // code_pointer++;
                        }
                        return BlastError.success;

                    case blast_operation.pop:
                        {
                            //datatype = BlastInterpretor.GetMetaDataType(metadata, (byte)(stack_offset - 1));
                            vector_size = BlastInterpretor.GetMetaDataSize(metadata, (byte)(stack_offset - 1));
                            vector_size = (byte)math.select(vector_size, 4, vector_size == 0);
                            stack_offset = stack_offset - vector_size;
                            handle_single_op_fn(register, ref vector_size, ssmd_datacount, stack, stack_offset, op, ex_op, is_negated, indexed, stack_is_aligned, stack_rowsize);
                        }
                        return BlastError.success;

                    default:
                        {
                            //
                            // DATASEG operation
                            // 
                            int source_index = (int)code_byte - (int)BlastInterpretor.opt_id;
                            if (source_index < 0 || code_byte == 255)
                            {
#if DEVELOPMENT_BUILD || TRACE
                                Debug.LogError($"blast.ssmd.get_single_op_result -> invalid id {source_index} at {code_pointer}:{code_byte}");
#endif
                                return BlastError.error;
                            }

                            // datatype = BlastInterpretor.GetMetaDataType(metadata, (byte)(source_index));
                            vector_size = BlastInterpretor.GetMetaDataSize(metadata, (byte)(source_index));
                            vector_size = (byte)math.select(vector_size, 4, vector_size == 0);

                            handle_single_op_fn(register, ref vector_size, ssmd_datacount, data, source_index, op, ex_op, is_negated, indexed, data_is_aligned, data_rowsize);
                        }
                        return BlastError.success;
                }
            }
        }


        /// <summary>
        /// 
        /// - token is just after function def
        /// 
        /// handle a sequence of operations
        /// - mula e62 p1 p2 .. pn
        /// - every parameter must be of same vector size 
        /// - future language versions might allow multiple vectorsizes in mula 
        /// 
        /// 
        /// 
        /// 
        /// sequence of:  + - / * min max mina maxa
        /// 
        /// 
        /// 
        /// 
        /// </summary>
        /// <param name="temp">temp scratch data</param>
        /// <param name="code_pointer">current codepointer</param>
        /// <param name="vector_size">output vectorsize</param>
        /// <param name="f4">output data vector</param>
        /// <param name="operation">operation to take on the vectors</param>
        void get_op_a_result([NoAlias] void* temp, ref int code_pointer, ref byte vector_size, int ssmd_datacount, [NoAlias] ref float4* f4, blast_operation operation, BlastParameterEncoding encoding = BlastParameterEncoding.Encode62)
        {
#if DEVELOPMENT_BUILD || TRACE
            Assert.IsFalse(temp == null, $"blast.ssmd.interpretor.get_op_a_result: temp buffer NULL");
            Assert.IsFalse(f4 == null, $"blast.ssmd.interpretor.get_op_a_result: output f4 NULL");
#endif

            // decode parametercount and vectorsize from code in 62 format 
            byte c;
            switch (encoding)
            {
                case BlastParameterEncoding.Encode62:
                    c = BlastInterpretor.decode62(code[code_pointer], ref vector_size);
                    break;

                case BlastParameterEncoding.Encode44:
                    c = BlastInterpretor.decode44(code[code_pointer], ref vector_size);
                    break;

#if DEVELOPMENT_BUILD || TRACE
                default:
                    Debug.LogError($"blast.ssmd.interpretor.get_op_a_result: encoding type not supported, op = {operation}, encoding = {encoding}");
                    return;
#else
                default: c = 0; break;
#endif
            }
            code_pointer++;

            // all operations should be of equal vectorsize we dont need to check this
            // - but could in debug 
            // - we could also allow it ....

            switch (vector_size)
            {
                case (byte)BlastVectorSizes.bool32:

                    vector_size = 1;

                    // encode44, c will max be 15
                    // if (c == 1)
                    //{
                    // we could handle 1 parameter more efficiently 
                    //    pop_with_op_into_fx_ref
                    //}
                    //else
                    {
                        pop_fx_into_ref<float>(ref code_pointer, (float*)temp);

                        for (int j = 1; j < c - 1; j++)
                        {
                            pop_fx_with_op_into_fx_ref<float, float>(ref code_pointer, (float*)temp, (float*)temp, operation, BlastVariableDataType.Bool32);
                        }

                        pop_fx_with_op_into_fx_ref<float, float4>(ref code_pointer, (float*)temp, f4, operation, BlastVariableDataType.Bool32);
                    }
                    break;

                case 0:
                case 4:
                    vector_size = 4;

                    pop_fx_into_ref<float4>(ref code_pointer, f4);   // _ref increases code_pointer according to data

                    for (int j = 1; j < c; j++)
                    {
                        pop_fx_with_op_into_fx_ref<float4, float4>(ref code_pointer, f4, f4, blast_operation.multiply);
                    }

                    break;


                case 1:
                    pop_fx_into_ref<float>(ref code_pointer, (float*)temp);

                    for (int j = 1; j < c - 1; j++)
                    {
                        pop_fx_with_op_into_fx_ref<float, float>(ref code_pointer, (float*)temp, (float*)temp, operation);
                    }

                    pop_fx_with_op_into_fx_ref<float, float4>(ref code_pointer, (float*)temp, f4, operation);
                    break;

                case 2:
                    pop_fx_into_ref<float2>(ref code_pointer, (float2*)temp);

                    for (int j = 1; j < c - 1; j++)
                    {
                        pop_fx_with_op_into_fx_ref<float2, float2>(ref code_pointer, (float2*)temp, (float2*)temp, operation);
                    }

                    pop_fx_with_op_into_fx_ref<float2, float4>(ref code_pointer, (float2*)temp, f4, operation);
                    break;

                case 3:
                    pop_fx_into_ref<float3>(ref code_pointer, (float3*)temp);

                    for (int j = 1; j < c - 1; j++)
                    {
                        pop_fx_with_op_into_fx_ref<float3, float3>(ref code_pointer, (float3*)temp, (float3*)temp, operation);
                    }

                    pop_fx_with_op_into_fx_ref<float3, float4>(ref code_pointer, (float3*)temp, f4, operation);
                    break;
            }

            // pop_fx_with_op_into_fx_ref puts code_pointer at next item but we want it to be left at last byte processed
            code_pointer--;
        }


        /// <summary>
        /// get result of operation with multiple parameters that is based on components of vectors: mina, maxa, csum
        /// 
        /// - somewhere in this function BURST crashes on a type conversion of a single 
        /// 
        /// </summary>
        void get_op_a_component_result([NoAlias] void* temp, ref int code_pointer, ref byte vector_size, int ssmd_datacount, [NoAlias] ref float4* f4, blast_operation operation)
        {
#if DEVELOPMENT_BUILD || TRACE
            Assert.IsFalse(temp == null, $"blast.ssmd.interpretor.get_op_a_component_result: temp buffer NULL");
            Assert.IsFalse(f4 == null, $"blast.ssmd.interpretor.get_op_a_component_result: output f4 NULL");

            if (operation != blast_operation.maxa && operation != blast_operation.mina && operation != blast_operation.csum)
            {
                Debug.LogError($"blast.ssmd.interpretor.get_op_a_component_result: op {operation} not supported, only component operations are allowed: maxa, mina, csum");
                return;
            }

#endif

            // decode parametercount and vectorsize from code in 62 format 
            byte c = BlastInterpretor.decode62(code[code_pointer], ref vector_size);
            code_pointer++;
            BlastVariableDataType datatype;

            // all parameters to the function have the same vectorsize, in future update compiler could support different vectorsizes
            for (int j = 0; j < c; j++)
            {
                pop_op_meta_cp(code_pointer, out datatype, out vector_size);

                switch (vector_size)
                {
                    case 0:
                    case 4:
                        float4* t4 = (float4*)temp;
                        pop_fx_into_ref<float4>(ref code_pointer, t4);
                        switch (operation)
                        {
                            case blast_operation.maxa:
                                if (j == 0) for (int i = 0; i < ssmd_datacount; i++) { f4[i][0] = math.cmax(t4[i]); }
                                else for (int i = 0; i < ssmd_datacount; i++) f4[i][0] = math.max(f4[i].x, math.cmax(t4[i]));
                                break;
                            case blast_operation.mina:
                                if (j == 0) for (int i = 0; i < ssmd_datacount; i++) f4[i][0] = math.cmin(t4[i]);
                                else for (int i = 0; i < ssmd_datacount; i++) f4[i][0] = math.min(f4[i][0], math.cmin(t4[i]));
                                break;
                            case blast_operation.csum:
                                if (j == 0) for (int i = 0; i < ssmd_datacount; i++) f4[i][0] = math.csum(t4[i]);
                                else for (int i = 0; i < ssmd_datacount; i++) f4[i][0] = f4[i][0] + math.csum(t4[i]);
                                break;
                        }
                        break;


                    case 1: // wierd on v1 but ok..
                        float* t1 = (float*)temp;
                        pop_fx_into_ref<float>(ref code_pointer, t1);
                        switch (operation)
                        {
                            case blast_operation.maxa:
                                if (j == 0) for (int i = 0; i < ssmd_datacount; i++) f4[i][0] = t1[i];
                                else for (int i = 0; i < ssmd_datacount; i++) f4[i][0] = math.max(f4[i][0], t1[i]);
                                break;
                            case blast_operation.mina:
                                if (j == 0) for (int i = 0; i < ssmd_datacount; i++) f4[i][0] = t1[i];
                                else for (int i = 0; i < ssmd_datacount; i++) f4[i][0] = math.min(f4[i][0], t1[i]);
                                break;
                            case blast_operation.csum:
                                if (j == 0) for (int i = 0; i < ssmd_datacount; i++) f4[i][0] = t1[i];
                                else for (int i = 0; i < ssmd_datacount; i++) f4[i][0] += t1[i];
                                break;
                        }
                        break;

                    case 2:
                        float2* t2 = (float2*)temp;
                        pop_fx_into_ref<float2>(ref code_pointer, t2);
                        switch (operation)
                        {
                            case blast_operation.maxa:
                                if (j == 0) for (int i = 0; i < ssmd_datacount; i++) f4[i][0] = math.cmax(t2[i]);
                                else for (int i = 0; i < ssmd_datacount; i++) f4[i][0] = math.max(f4[i][0], math.cmax(t2[i]));
                                break;
                            case blast_operation.mina:
                                if (j == 0) for (int i = 0; i < ssmd_datacount; i++) f4[i][0] = math.cmin(t2[i]);
                                else for (int i = 0; i < ssmd_datacount; i++) f4[i][0] = math.min(f4[i][0], math.cmin(t2[i]));
                                break;
                            case blast_operation.csum:
                                if (j == 0) for (int i = 0; i < ssmd_datacount; i++) f4[i][0] = math.csum(t2[i]);
                                else for (int i = 0; i < ssmd_datacount; i++) f4[i][0] = f4[i][0] + math.csum(t2[i]);
                                break;
                        }
                        break;

                    case 3:
                        float3* t3 = (float3*)temp;
                        pop_fx_into_ref<float3>(ref code_pointer, t3);
                        switch (operation)
                        {
                            case blast_operation.maxa:
                                if (j == 0) for (int i = 0; i < ssmd_datacount; i++) f4[i][0] = math.cmax(t3[i]);
                                else for (int i = 0; i < ssmd_datacount; i++) f4[i][0] = math.max(f4[i][0], math.cmax(t3[i]));
                                break;
                            case blast_operation.mina:
                                if (j == 0) for (int i = 0; i < ssmd_datacount; i++) f4[i][0] = math.cmin(t3[i]);
                                else for (int i = 0; i < ssmd_datacount; i++) f4[i][0] = math.min(f4[i][0], math.cmin(t3[i]));
                                break;
                            case blast_operation.csum:
                                if (j == 0) for (int i = 0; i < ssmd_datacount; i++) f4[i][0] = math.csum(t3[i]);
                                else for (int i = 0; i < ssmd_datacount; i++) f4[i][0] = f4[i][0] + math.csum(t3[i]);
                                break;
                        }
                        break;
                }
            }
            // pop_fx_with_op_into_fx_ref puts code_pointer at next item but we want it to be left at last byte processed
            code_pointer--;

            // the output of component functions is always of size 1 
            vector_size = 1;
        }

        #endregion

        #region Reinterpret XXXX [DataIndex]

        /// <summary>
        /// reinterpret the value at index as a different datatype (set metadata type to datatype)
        /// </summary>
        void reinterpret(ref int code_pointer, ref byte vector_size, BlastVariableDataType datatype)
        {
            // reinterpret operation would be the same for each ssmd record
            int dataindex = code[code_pointer] - BlastInterpretor.opt_id;

            // dataindex must point to a valid id 
            if (dataindex < 0 || dataindex + BlastInterpretor.opt_id >= 255)
            {
                if(IsTrace) Debug.LogError($"ssmd.reinterpret<{datatype}>: first parameter must directly point to a dataindex but its '{dataindex + BlastInterpretor.opt_id}' instead");
                return;
            }

            // validate if within package bounds, catch packaging errors early on 
            // datasize is in bytes 
            if (dataindex >= package.DataSize >> 2)
            {
                if(IsTrace) Debug.LogError($"ssmd.reinterpret<{datatype}>: compilation or packaging error, dataindex {dataindex} is out of bounds");
                return;
            }

            BlastInterpretor.GetMetaData(metadata, (byte) dataindex, out vector_size, out BlastVariableDataType old_type);
            BlastInterpretor.SetMetaData(in metadata, datatype, vector_size, (byte)dataindex);
        }


        #endregion

        #region External Function Handler 

#if !STANDALONE_VSBUILD
        [SkipLocalsInit]
#endif
        void CallExternalFunction([NoAlias] void* temp, ref int code_pointer, ref byte vector_size, int ssmd_datacount, [NoAlias] float4* f4)
        {
            // get int id from next 4 ops/bytes 
            int id = (code[code_pointer + 0] << 8) + code[code_pointer + 1];

            // advance code pointer beyond the script id
            code_pointer += 2;

            // get function pointer 
            if (!engine_ptr->CanBeAValidFunctionId(id))
            {
                if (IsTrace) Debug.LogError($"blast.ssmd.interpretor.call-external: function id {id} is invalid");
                return;

            }

            BlastScriptFunction p = engine_ptr->Functions[id];

            if (IsTrace)
            {
                Debug.Log($"call fp <id:{id}> <profile:{p.FunctionDelegateId}> <env:{environment_ptr.ToInt64()}> <parametercount:{p.MinParameterCount}>");
            }

            vector_size = (byte)math.select((int)p.ReturnsVectorSize, p.AcceptsVectorSize, p.ReturnsVectorSize == 0);
            if (vector_size != 1)
            {
                if (IsTrace) Debug.LogError($"blast.ssmd.interpretor.call-external: function {id} a return vector size > 1 is not currently supported in external functions");
                return;
            }
            if (p.AcceptsVectorSize != 1)
            {
                if (IsTrace) Debug.LogError($"blast.ssmd.interpretor.call-external: function {id} a parameter vector size > 1 is not currently supported in external functions");
                return;
            }

            void* result = CALL_EF(ref code_pointer, in p, ssmd_datacount, temp);

            if (result != null)
            {
                // copy to output depending on vectorsize 

                // todo -> need to fill in all values from profile in info native 
                switch (vector_size)
                {
                    case 1: simd.move_f1_array_into_f4_array_as_f1(f4, (float*)result, ssmd_datacount); return;
                    case 2: simd.move_f2_array_into_f4_array_as_f2(f4, (float2*)result, ssmd_datacount); return;
                    case 3: simd.move_f3_array_into_f4_array_as_f3(f4, (float3*)result, ssmd_datacount); return;
                    case 0:
                    case 4: simd.move_f4_array_into_f4_array_as_f4(f4, (float4*)result, ssmd_datacount); return;
                    default:
                        break;
                }
            }
            else
            {
                if (p.ReturnsVectorSize == 0)
                {
                    return;
                }
            }

            // reaching here is an error 
            if (IsTrace)
            {
                Debug.LogError($"blast.ssmd.interpretor.call-external: failed to call function pointer with id: {id}");
            }
        }
        #endregion

        #region GetFunction|Sequence and Execute (privates)



        /// <summary>
        /// 
        /// - codepointer starts at first token of function
        /// - codepointer is left on last token processed
        /// 
        /// get the result of a function encoded in the byte code, support all fuctions in op, exop and external calls 
        /// although external calls should be directly called when possible
        /// </summary>
        private BlastError GetFunctionResult([NoAlias] void* temp,

                                                ref int code_pointer,
                                                ref byte vector_size,
                                                in int ssmd_datacount,

                                                [NoAlias] float4* f4_result = null,
                                                in DATAREC target = default,

                                                bool error_if_not_exists = true)
        {
            byte op = code[code_pointer];
            code_pointer++;

            switch ((blast_operation)op)
            {
                // math functions
                //case blast_operation.abs: get_abs_result(temp, ref code_pointer, ref vector_size, f4_result); break;
                case blast_operation.random: CALL_RANDOM(ref code_pointer, ref vector_size, ssmd_datacount, f4_result);   break;

                case blast_operation.maxa: get_op_a_component_result(temp, ref code_pointer, ref vector_size, ssmd_datacount, ref f4_result, blast_operation.maxa); break;
                case blast_operation.mina: get_op_a_component_result(temp, ref code_pointer, ref vector_size, ssmd_datacount, ref f4_result, blast_operation.mina); break;
                case blast_operation.csum: get_op_a_component_result(temp, ref code_pointer, ref vector_size, ssmd_datacount, ref f4_result, blast_operation.csum); break;

                case blast_operation.max: get_op_a_result(temp, ref code_pointer, ref vector_size, ssmd_datacount, ref f4_result, blast_operation.max); break;
                case blast_operation.min: get_op_a_result(temp, ref code_pointer, ref vector_size, ssmd_datacount, ref f4_result, blast_operation.min); break;
                case blast_operation.all: get_op_a_result(temp, ref code_pointer, ref vector_size, ssmd_datacount, ref f4_result, blast_operation.and, BlastParameterEncoding.Encode44); break;
                case blast_operation.any: get_op_a_result(temp, ref code_pointer, ref vector_size, ssmd_datacount, ref f4_result, blast_operation.or, BlastParameterEncoding.Encode44); break;
                case blast_operation.mula: get_op_a_result(temp, ref code_pointer, ref vector_size, ssmd_datacount, ref f4_result, blast_operation.multiply); break;
                case blast_operation.adda: get_op_a_result(temp, ref code_pointer, ref vector_size, ssmd_datacount, ref f4_result, blast_operation.add); break;
                case blast_operation.suba: get_op_a_result(temp, ref code_pointer, ref vector_size, ssmd_datacount, ref f4_result, blast_operation.substract); break;
                case blast_operation.diva: get_op_a_result(temp, ref code_pointer, ref vector_size, ssmd_datacount, ref f4_result, blast_operation.diva); break;

                case blast_operation.abs: get_single_op_result(temp, ref code_pointer, ref vector_size, ssmd_datacount, f4_result, blast_operation.abs, extended_blast_operation.nop); break;
                case blast_operation.trunc: get_single_op_result(temp, ref code_pointer, ref vector_size, ssmd_datacount, f4_result, blast_operation.trunc, extended_blast_operation.nop); break;

                case blast_operation.index_x: get_single_op_result(temp, ref code_pointer, ref vector_size, ssmd_datacount, f4_result, blast_operation.index_x, extended_blast_operation.nop, 0); break;
                case blast_operation.index_y: get_single_op_result(temp, ref code_pointer, ref vector_size, ssmd_datacount, f4_result, blast_operation.index_y, extended_blast_operation.nop, 1); break;
                case blast_operation.index_z: get_single_op_result(temp, ref code_pointer, ref vector_size, ssmd_datacount, f4_result, blast_operation.index_z, extended_blast_operation.nop, 2); break;
                case blast_operation.index_w: get_single_op_result(temp, ref code_pointer, ref vector_size, ssmd_datacount, f4_result, blast_operation.index_w, extended_blast_operation.nop, 3); break;

                case blast_operation.index_n: IndexF1IntoFn(temp, ref code_pointer, ref vector_size, ssmd_datacount, f4_result, default); break;
                case blast_operation.expand_v2: ExpandF1IntoFn(2, ref code_pointer, ref vector_size, ssmd_datacount, f4_result, default); break;
                case blast_operation.expand_v3: ExpandF1IntoFn(3, ref code_pointer, ref vector_size, ssmd_datacount, f4_result, default); break;
                case blast_operation.expand_v4: ExpandF1IntoFn(4, ref code_pointer, ref vector_size, ssmd_datacount, f4_result, default); break;

                case blast_operation.select: get_select_result(temp, ref code_pointer, ref vector_size, ssmd_datacount, f4_result); break;
                case blast_operation.fma: get_fma_result(temp, ref code_pointer, ref vector_size, ssmd_datacount, ref f4_result); break;

                case blast_operation.zero: get_zero_result(temp, ref code_pointer, ref vector_size, ssmd_datacount); break;
                case blast_operation.size: GetSizeResult(ref code_pointer, ref vector_size, ssmd_datacount, f4_result); break;

                case blast_operation.ex_op:
                    {
                        extended_blast_operation exop = (extended_blast_operation)code[code_pointer];
                        code_pointer++;

                        switch (exop)
                        {
                            // external calls 
                            case extended_blast_operation.call: CallExternalFunction(temp, ref code_pointer, ref vector_size, ssmd_datacount, f4_result); break;

                            // single inputs
                            case extended_blast_operation.sqrt: get_single_op_result(temp, ref code_pointer, ref vector_size, ssmd_datacount, f4_result, blast_operation.ex_op, extended_blast_operation.sqrt); break;
                            case extended_blast_operation.rsqrt: get_single_op_result(temp, ref code_pointer, ref vector_size, ssmd_datacount, f4_result, blast_operation.ex_op, extended_blast_operation.rsqrt); break;
                            case extended_blast_operation.sin: get_single_op_result(temp, ref code_pointer, ref vector_size, ssmd_datacount, f4_result, blast_operation.ex_op, extended_blast_operation.sin); break;
                            case extended_blast_operation.cos: get_single_op_result(temp, ref code_pointer, ref vector_size, ssmd_datacount, f4_result, blast_operation.ex_op, extended_blast_operation.cos); break;
                            case extended_blast_operation.tan: get_single_op_result(temp, ref code_pointer, ref vector_size, ssmd_datacount, f4_result, blast_operation.ex_op, extended_blast_operation.tan); break;
                            case extended_blast_operation.sinh: get_single_op_result(temp, ref code_pointer, ref vector_size, ssmd_datacount, f4_result, blast_operation.ex_op, extended_blast_operation.sinh); break;
                            case extended_blast_operation.cosh: get_single_op_result(temp, ref code_pointer, ref vector_size, ssmd_datacount, f4_result, blast_operation.ex_op, extended_blast_operation.cosh); break;
                            case extended_blast_operation.atan: get_single_op_result(temp, ref code_pointer, ref vector_size, ssmd_datacount, f4_result, blast_operation.ex_op, extended_blast_operation.atan); break;
                            case extended_blast_operation.degrees: get_single_op_result(temp, ref code_pointer, ref vector_size, ssmd_datacount, f4_result, blast_operation.ex_op, extended_blast_operation.degrees); break;
                            case extended_blast_operation.radians: get_single_op_result(temp, ref code_pointer, ref vector_size, ssmd_datacount, f4_result, blast_operation.ex_op, extended_blast_operation.radians); break;
                            case extended_blast_operation.ceil: get_single_op_result(temp, ref code_pointer, ref vector_size, ssmd_datacount, f4_result, blast_operation.ex_op, extended_blast_operation.ceil); break;
                            case extended_blast_operation.floor: get_single_op_result(temp, ref code_pointer, ref vector_size, ssmd_datacount, f4_result, blast_operation.ex_op, extended_blast_operation.floor); break;
                            case extended_blast_operation.frac: get_single_op_result(temp, ref code_pointer, ref vector_size, ssmd_datacount, f4_result, blast_operation.ex_op, extended_blast_operation.frac); break;
                            case extended_blast_operation.normalize: get_single_op_result(temp, ref code_pointer, ref vector_size, ssmd_datacount, f4_result, blast_operation.ex_op, extended_blast_operation.normalize); break;
                            case extended_blast_operation.saturate: get_single_op_result(temp, ref code_pointer, ref vector_size, ssmd_datacount, f4_result, blast_operation.ex_op, extended_blast_operation.saturate); break;
                            case extended_blast_operation.logn: get_single_op_result(temp, ref code_pointer, ref vector_size, ssmd_datacount, f4_result, blast_operation.ex_op, extended_blast_operation.logn); break;
                            case extended_blast_operation.log10: get_single_op_result(temp, ref code_pointer, ref vector_size, ssmd_datacount, f4_result, blast_operation.ex_op, extended_blast_operation.log10); break;
                            case extended_blast_operation.log2: get_single_op_result(temp, ref code_pointer, ref vector_size, ssmd_datacount, f4_result, blast_operation.ex_op, extended_blast_operation.log2); break;
                            case extended_blast_operation.exp10: get_single_op_result(temp, ref code_pointer, ref vector_size, ssmd_datacount, f4_result, blast_operation.ex_op, extended_blast_operation.exp10); break;
                            case extended_blast_operation.exp: get_single_op_result(temp, ref code_pointer, ref vector_size, ssmd_datacount, f4_result, blast_operation.ex_op, extended_blast_operation.exp); break;
                            case extended_blast_operation.ceillog2: get_single_op_result(temp, ref code_pointer, ref vector_size, ssmd_datacount, f4_result, blast_operation.ex_op, extended_blast_operation.ceillog2); break;
                            case extended_blast_operation.ceilpow2: get_single_op_result(temp, ref code_pointer, ref vector_size, ssmd_datacount, f4_result, blast_operation.ex_op, extended_blast_operation.ceilpow2); break;
                            case extended_blast_operation.floorlog2: get_single_op_result(temp, ref code_pointer, ref vector_size, ssmd_datacount, f4_result, blast_operation.ex_op, extended_blast_operation.floorlog2); break;


                            case extended_blast_operation.fmod: get_fmod_result(temp, ref code_pointer, ref vector_size, ssmd_datacount, f4_result); break;

                            // dual inputs 
                            case extended_blast_operation.pow: get_pow_result(temp, ref code_pointer, ref vector_size, ssmd_datacount, f4_result); break;
                            case extended_blast_operation.cross: get_cross_result(temp, ref code_pointer, ref vector_size, ssmd_datacount, f4_result); break;
                            case extended_blast_operation.dot: get_dot_result(temp, ref code_pointer, ref vector_size, ssmd_datacount, f4_result); break;
                            case extended_blast_operation.atan2: get_atan2_result(temp, ref code_pointer, ref vector_size, ssmd_datacount, f4_result); break;

                            // tripple inputs                 
                            case extended_blast_operation.clamp: get_clamp_result(temp, ref code_pointer, ref vector_size, ssmd_datacount, f4_result); break;
                            case extended_blast_operation.lerp: get_lerp_result(temp, ref code_pointer, ref vector_size, ssmd_datacount, f4_result); break;
                            case extended_blast_operation.unlerp: get_unlerp_result(temp, ref code_pointer, ref vector_size, ssmd_datacount, f4_result); break;
                            case extended_blast_operation.slerp: get_slerp_result(temp, ref code_pointer, ref vector_size, ssmd_datacount, f4_result); break;
                            case extended_blast_operation.nlerp: get_nlerp_result(temp, ref code_pointer, ref vector_size, ssmd_datacount, f4_result); break;

                            // five inputs 
                            // 
                            //  -> build an indexer of ssmd_datacount wide with all dataroots so we can move that like a card over our data? so we dont need to copy 5!!! buffers ... 
                            // 
                            // case extended_blast_operation.remap: get_remap_result(temp, ref code_pointer, ref vector_size, f4_result); break;

                            // bitwise operations
                            case extended_blast_operation.get_bit: get_getbit_result(temp, ref code_pointer, ref vector_size, ssmd_datacount, f4_result); break;
                            case extended_blast_operation.get_bits: get_getbits_result(temp, ref code_pointer, ref vector_size, ssmd_datacount, f4_result); break;
                            case extended_blast_operation.set_bit: get_setbit_result(temp, ref code_pointer, ref vector_size, ssmd_datacount); break;
                            case extended_blast_operation.set_bits: get_setbits_result(temp, ref code_pointer, ref vector_size, ssmd_datacount); break;
                            case extended_blast_operation.count_bits: get_countbits_result(temp, ref code_pointer, ref vector_size, ssmd_datacount, f4_result); break;
                            case extended_blast_operation.reverse_bits: get_reversebits_result(temp, ref code_pointer, ref vector_size, ssmd_datacount); break;
                            case extended_blast_operation.tzcnt: get_tzcnt_result(temp, ref code_pointer, ref vector_size, ssmd_datacount, f4_result); break;
                            case extended_blast_operation.lzcnt: get_lzcnt_result(temp, ref code_pointer, ref vector_size, ssmd_datacount, f4_result); break;

                            case extended_blast_operation.rol: get_rol_result(temp, ref code_pointer, ref vector_size, ssmd_datacount); break;
                            case extended_blast_operation.ror: get_ror_result(temp, ref code_pointer, ref vector_size, ssmd_datacount); break;
                            case extended_blast_operation.shr: get_shr_result(temp, ref code_pointer, ref vector_size, ssmd_datacount); break;
                            case extended_blast_operation.shl: get_shl_result(temp, ref code_pointer, ref vector_size, ssmd_datacount); break;

                            // datatype reinterpretations 
                            case extended_blast_operation.reinterpret_bool32: reinterpret(ref code_pointer, ref vector_size, BlastVariableDataType.Bool32); break;
                            case extended_blast_operation.reinterpret_float: reinterpret(ref code_pointer, ref vector_size, BlastVariableDataType.Numeric); break;
                            case extended_blast_operation.reinterpret_int32: reinterpret(ref code_pointer, ref vector_size, BlastVariableDataType.ID); break;

                            // debugging
                            case extended_blast_operation.debugstack:
                                {
                                    // write contents of stack to the debug stream as a detailed report; 
                                    // write contents of a data element to the debug stream 
                                    RunDebugStack();
                                    code_pointer--;
                                }
                                break;

                            case extended_blast_operation.validate:
                                {
                                    RunValidate(ref code_pointer, temp, f4_result);
                                    code_pointer--;
                                }
                                break;

                            case extended_blast_operation.debug:
                                {
                                    // write contents of a data element to the debug stream 
                                    RunDebug(code[code_pointer] - BlastInterpretor.opt_id);

                                    //BlastVariableDataType datatype = default;

                                    // even if not in debug, if the command is encoded any stack op needs to be popped 
                                    // void* pdata = pop_with_info(code_pointer, out datatype, out vector_size);
                                }
                                break;

                            default:
#if DEVELOPMENT_BUILD || TRACE
                                if (error_if_not_exists)
                                {
                                    Debug.LogError($"SSMD Interpretor: GetFunctionResult: codepointer: {code_pointer} => {code[code_pointer]}, extended operation {exop} not handled");
                                }
#endif
                                return BlastError.ssmd_function_not_handled;
                        }
                    }
                    break;


                default:
#if DEVELOPMENT_BUILD || TRACE
                    if (error_if_not_exists)
                    {
                        Debug.LogError($"SSMD Interpretor: GetFunctionResult: codepointer: {code_pointer} => {code[code_pointer]}, operation {op} not handled");
                    }
#endif
                    return BlastError.ssmd_function_not_handled;
            }

            return BlastError.success;
        }


        /// <summary>
        /// this DOES NOT TRACK FUNCTIONS and will be inaccurate when used if current code_pointer is on a function 
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        bool is_last_in_sequence(int i)
        {
            // check multibyte signatures 
            blast_operation op = (blast_operation)code[i];
            switch (op)
            {
                case blast_operation.constant_f1: i += 4; break;
                case blast_operation.constant_f1_h: i += 3; break;
                case blast_operation.constant_long_ref: i += 3; break;
                case blast_operation.constant_short_ref: i += 2; break;

                case blast_operation.cdata:
                    Debug.LogError("handle me cdata");
                    break;


                // these all expect a single parameter 
                case blast_operation.size:
                case blast_operation.expand_v2:
                case blast_operation.expand_v3:
                case blast_operation.expand_v4:
                    // if next is an id >= id < 255 then + 1, 
                    // if next is constant ->
                    op = (blast_operation)code[i + 1];
                    switch (op)
                    {
                        case blast_operation.constant_f1: i += 5; break;
                        case blast_operation.constant_f1_h: i += 4; break;
                        case blast_operation.constant_long_ref: i += 4; break;
                        case blast_operation.constant_short_ref: i += 2; break;
                        case blast_operation.cdataref: i += 4; break;
                        // assume compiler did its job
                        default:
                            if ((int)op < 255 && op >= blast_operation.value_0)
                            {
                                i += 2;
                            }
                            else
                            {
                                return false;
                            }
                            break;
                    }
                    break;

                case blast_operation.pi:
                case blast_operation.inv_pi:
                case blast_operation.epsilon:
                case blast_operation.infinity:
                case blast_operation.negative_infinity:
                case blast_operation.nan:
                case blast_operation.min_value:
                case blast_operation.value_0:
                case blast_operation.value_1:
                case blast_operation.value_2:
                case blast_operation.value_3:
                case blast_operation.value_4:
                case blast_operation.value_8:
                case blast_operation.value_10:
                case blast_operation.value_16:
                case blast_operation.value_24:
                case blast_operation.value_32:
                case blast_operation.value_64:
                case blast_operation.value_100:
                case blast_operation.value_128:
                case blast_operation.value_256:
                case blast_operation.value_512:
                case blast_operation.value_1000:
                case blast_operation.value_1024:
                case blast_operation.value_30:
                case blast_operation.value_45:
                case blast_operation.value_90:
                case blast_operation.value_180:
                case blast_operation.value_270:
                case blast_operation.value_360:
                case blast_operation.inv_value_2:
                case blast_operation.inv_value_3:
                case blast_operation.inv_value_4:
                case blast_operation.inv_value_8:
                case blast_operation.inv_value_10:
                case blast_operation.inv_value_16:
                case blast_operation.inv_value_24:
                case blast_operation.inv_value_32:
                case blast_operation.inv_value_64:
                case blast_operation.inv_value_100:
                case blast_operation.inv_value_128:
                case blast_operation.inv_value_256:
                case blast_operation.inv_value_512:
                case blast_operation.inv_value_1000:
                case blast_operation.inv_value_1024:
                case blast_operation.inv_value_30:
                case blast_operation.framecount:
                case blast_operation.fixedtime:
                case blast_operation.time:
                case blast_operation.fixeddeltatime:
                case blast_operation.deltatime:
                    break;

                default: // assume single byte op if  
                    {
                        return (i >= package.CodeSize - 1)
                                ||
                                (op >= blast_operation.id)
                                &&
                                (code[i + 1] == (byte)blast_operation.nop || code[i + 1] == (byte)blast_operation.end);
                    }

                // for now if next is anything else 
                case blast_operation.ex_op: return false;
            }

            return (i >= package.CodeSize - 1)
                ||
                (code[i] == (byte)blast_operation.nop || code[i] == (byte)blast_operation.end);
        }

        /// <summary>
        /// process a sequence of operations into a new register value 
        /// </summary>        
#if !STANDALONE_VSBUILD
        [SkipLocalsInit]
#endif
        int GetSequenceResult([NoAlias] void* temp, ref int code_pointer, ref byte vector_size, int ssmd_datacount, in DATAREC target_rec = default)
        {
            byte op = 0, prev_op;

            blast_operation current_op = blast_operation.nop;

            bool not = false;
            bool minus = false;

            byte* buffer = stackalloc byte[ssmd_datacount * sizeof(float4)];

            float4* f4 = (float4*)temp;
            float4* f4_temp = (float4*)(void*)buffer;   // needed for getfunctionresult 
            float4* f4_result = register;               // here we accumelate the resulting value in 

            vector_size = 1; // vectors dont shrink in normal operations 
            byte prev_vector_size = 1;

            int indexer = -1;
            int nesting_level = 0;

            while (code_pointer < package.CodeSize)
            {
                prev_vector_size = vector_size;

                prev_op = op;
                op = code[code_pointer];

                bool is_last_token_in_sequence = nesting_level <= 0 && is_last_in_sequence(code_pointer);

                // process operation, put any value in f4, set current_op if something else
                switch ((blast_operation)op)
                {
                    case blast_operation.end:
                        {
                            if (nesting_level > 0)
                            {
                                nesting_level--;
                                code_pointer++;
                                continue;
                            }

                            //
                            //
                            // !! when assigning this might not be the end, peek next instruction.
                            //
                            // - if not a nop or a new assign, then continue reading the compound 
                            //
                            if (code_pointer < package.CodeSize - 1)
                            {
                                blast_operation next_op = (blast_operation)code[code_pointer + 1];

                                if (next_op != blast_operation.nop && BlastInterpretor.IsAssignmentOperation(next_op))
                                {
                                    // for now restrict ourselves to only accepting operations at this point 
                                    if (BlastInterpretor.IsMathematicalOrBooleanOperation(next_op))
                                    {
                                        // break out of switch before returning, there is more to this operation then known at the moment 
                                        break;
                                    }
                                    else
                                    {
                                        // most probably a bug, error when the message is usefull
#if DEVELOPMENT_BUILD || TRACE
                                        Debug.LogError($"SSMD Interpretor: Possible BUG: encountered non mathematical operation '{next_op}' after compound in statement at codepointer {code_pointer + 1}, expecting nop or assign");
#else
                                        //Debug.LogWarning($"SSMD Interpretor: Possible BUG: encountered non mathematical operation '{next_op}' after compound in statement at codepointer {code_pointer + 1}, expecting nop or assign");
#endif
                                    }
                                }
                            }
                            // handle end of sequence, might need to copy register to buffer 
                            goto end_seq;
                        }

                    case blast_operation.nop:
                        {
                            //
                            // assignments will end with nop and not with end as a compounded statement
                            // at this point vector_size is decisive for the returned value
                            //
                            code_pointer++;
                            goto end_seq;
                        }

                    case blast_operation.ret:
                        // termination of interpretation
                        code_pointer = package.CodeSize + 1;
                        goto end_seq;


                    /*  case blast_operation.index_n:
                      case blast_operation.index_x:
                          indexer = 0;
                          code_pointer++;
                          continue;
                      case blast_operation.index_y:
                          indexer = 1;
                          code_pointer++;
                          continue;
                      case blast_operation.index_z:
                          indexer = 2;
                          code_pointer++;
                          continue;
                      case blast_operation.index_w:
                          indexer = 3;
                          code_pointer++;
                          continue; */

                    // arithmetic ops 
                    case blast_operation.add:
                    case blast_operation.multiply:
                    case blast_operation.divide:
                    // boolean ops 
                    case blast_operation.or:
                    case blast_operation.and:
                    case blast_operation.xor:
                    case blast_operation.smaller:
                    case blast_operation.smaller_equals:
                    case blast_operation.greater:
                    case blast_operation.greater_equals:
                    case blast_operation.equals:
                    case blast_operation.not_equals:
                        {
                            // handle operation AFTER reading next value
                            current_op = (blast_operation)op;
                            not = minus = false;
                            indexer = -1;
                        }
                        // back to start of loop 
                        code_pointer++;
                        continue;

                    case blast_operation.not:
                        {
                            // if the first in a compound or previous was an operation
                            if (prev_op == 0 || BlastInterpretor.IsMathematicalOrBooleanOperation((blast_operation)prev_op))
                            {
#if DEVELOPMENT_BUILD || TRACE
                                if (not)
                                {
                                    Debug.LogError("blast.ssmd.interpretor: double token: ! !, this will be become nothing, it is not supported as it cant be determined to be intentional and will raise this error in editor mode only");
                                }
#endif
                                not = !not;
                            }
                            else
                            {
                                current_op = blast_operation.not;
                            }
                        }
                        code_pointer++;
                        continue;

                    case blast_operation.substract:
                        {
                            // first or after an op 
                            if (prev_op == 0 || BlastInterpretor.IsMathematicalOrBooleanOperation((blast_operation)prev_op))
                            {
#if DEVELOPMENT_BUILD || TRACE
                                if (minus)
                                {
                                    Debug.LogError("blast.ssmd.interpretor: double token: - -, this will be become +, it is not supported as it cant be determined to be intentional and will raise this error in editor mode only");
                                }
#endif
                                minus = !minus;
                            }
                            else
                            {
                                current_op = blast_operation.substract;
                            }
                        }
                        code_pointer++;
                        continue;

                    case blast_operation.begin:

                        // only allow to nest for a simple negation 
                        if (code[code_pointer + 1] == (byte)blast_operation.substract)
                        {
                            minus = true;
                            code_pointer += 2;
                            nesting_level++;

                            // level + 1, negate next value
                            continue;
                        }

                        Assert.IsTrue(false, "blast.ssmd.interpretor: should not be nesting compounds... compiler did not do its job wel");
                        break;

                    // in development builds assert on non supported operations
                    // in release mode skip over crashing into the unknown 

#if DEVELOPMENT_BUILD || TRACE
                    // jumps are not allowed in compounds 
                    case blast_operation.jz:
                    case blast_operation.jnz:
                    case blast_operation.jump:
                    case blast_operation.jump_back:
                        Assert.IsTrue(false, $"blast.ssmd.interpretor: jump operation {(blast_operation)op} not allowed in compounds, codepointer = {code_pointer}");
                        break;

                    // assignments are not allowed in compounds 
                    case blast_operation.yield:
                    case blast_operation.assigns:
                    case blast_operation.assign:
                    case blast_operation.assignf:
                    case blast_operation.assignfe:
                    case blast_operation.assignfn:
                    case blast_operation.assignfen:
                    case blast_operation.assignv:
                        Assert.IsTrue(false, $"blast.ssmd.interpretor: operation {(blast_operation)op} not allowed in compounds, codepointer = {code_pointer}");
                        break;

                    // any stack operation has no business in a compound other then pops
                    case blast_operation.push:
                    case blast_operation.pushv:
                    case blast_operation.peek:
                    case blast_operation.pushf:
                    case blast_operation.pushc:
                        Assert.IsTrue(false, $"blast.ssmd.interpretor: stack operation {(blast_operation)op} not allowed in compounds, codepointer = {code_pointer}");
                        break;


#endif
#if DEVELOPMENT_BUILD || TRACE
#endif

                    case blast_operation.constant_f1:
                        {
                            // handle operation with inlined constant data 
                            float constant = read_constant_f1(code, ref code_pointer); 

                            if (!is_last_token_in_sequence || !target_rec.is_set)
                            {
                                // put result into register
                                handle_single_op_constant(register, ref vector_size, ssmd_datacount, constant, current_op, extended_blast_operation.nop, minus, not);
                            }
                            else
                            {
                                // == last in sequence 
                                // == target is set
                                // output directly to target and skip end logics 
                                handle_single_op_constant(register, ref vector_size, ssmd_datacount, constant, current_op, extended_blast_operation.nop, minus, not, false, target_rec);
                                code_pointer++;
                                return (int)BlastError.success;
                            }

                            minus = false;
                            not = false;
                            indexer = -1;
                            current_op = blast_operation.nop;
                        }
                        continue;

                    case blast_operation.constant_f1_h:
                        {
                            // handle operation with inlined constant data 
                            float constant = read_constant_f1_h(code, ref code_pointer); 

                            if (!is_last_token_in_sequence || !target_rec.is_set)
                            {
                                // put result into register
                                handle_single_op_constant(register, ref vector_size, ssmd_datacount, constant, current_op, extended_blast_operation.nop, minus, not);
                            }
                            else
                            {
                                // output directly to target and skip end logics 
                                handle_single_op_constant(register, ref vector_size, ssmd_datacount, constant, current_op, extended_blast_operation.nop, minus, not, false, target_rec);
                                code_pointer++;
                                return (int)BlastError.success;
                            }

                            minus = false;
                            not = false;
                            indexer = -1;
                            current_op = blast_operation.nop;
                        }
                        continue;

                    case blast_operation.constant_short_ref:
                        {
                            if (follow_constant_ref(code, ref code_pointer, out BlastVariableDataType datatype, out int ref_index, true))
                            {
                                float constant = read_constant_float(code, in ref_index);

                                if (!is_last_token_in_sequence || !target_rec.is_set)
                                {
                                    // put result into register
                                    handle_single_op_constant(register, ref vector_size, ssmd_datacount, constant, current_op, extended_blast_operation.nop, minus, not);
                                }
                                else
                                {
                                    // output directly to target and skip end logics 
                                    handle_single_op_constant(register, ref vector_size, ssmd_datacount, constant, current_op, extended_blast_operation.nop, minus, not, false, target_rec);
                                    code_pointer++;
                                    return (int)BlastError.success;
                                }

                                minus = false;
                                not = false;
                                indexer = -1;
                                current_op = blast_operation.nop;
                            }
                            else
                            {
                                return (int)BlastError.error_constant_reference_error;
                            }
                        }
                        continue;

                    case blast_operation.constant_long_ref:
                        {
                            if (follow_constant_ref(code, ref code_pointer, out BlastVariableDataType datatype, out int ref_index, false))
                            {
                                float constant = read_constant_float(code, in ref_index);

                                if (!is_last_token_in_sequence || !target_rec.is_set)
                                {
                                    // put result into register
                                    handle_single_op_constant(register, ref vector_size, ssmd_datacount, constant, current_op, extended_blast_operation.nop, minus, not);
                                }
                                else
                                {
                                    // output directly to target and skip end logics 
                                    handle_single_op_constant(register, ref vector_size, ssmd_datacount, constant, current_op, extended_blast_operation.nop, minus, not, false, target_rec);
                                    code_pointer++;
                                    return (int)BlastError.success;
                                }

                                minus = false;
                                not = false;
                                indexer = -1;
                                current_op = blast_operation.nop;
                            }
                            else
                            {
                                return (int)BlastError.error_constant_reference_error;
                            }
                        }
                        continue;


                    case blast_operation.pop:
                        {
                            //
                            // -> pop into result if first op and continue
                            // -> pop into f4 temp buffer if another operation
                            // ! this saves 2 copies ! when prev_op == 0
                            //

                            // the pop instruction is ALWAYS 1 byte long, so we could check the next byte to see if this is the last operation in sequence 
                            // - only allow for this if not running in a nested operation 
                            // - no operation may be pending 
                            if (!is_last_token_in_sequence || current_op != blast_operation.nop || !target_rec.is_set)
                            {
                                // 
                                // - should pop with op into target if possible  - many scripts end on the sequence   [operation] pop
                                // 

                                // pop into buffer|register 
                                stackpop_fn_info(code_pointer, current_op == 0 ? f4_result : f4, minus, indexer, out vector_size);
                            }
                            else
                            {
                                // - no nesting
                                // - end of sequence
                                // - target is valid     => pop directly into targetlocation and skip endlogic
                                stackpop_fn_info(code_pointer, target_rec, minus, indexer, out vector_size);
                                code_pointer += 2;
                                return (int)BlastError.success;
                            }

                            minus = false;
                            indexer = -1;

#if DEVELOPMENT_BUILD || TRACE
                            if (vector_size > 4)
                            {
                                Debug.LogError($"blast.ssmd.interpretor.pop: codepointer: {code_pointer} => pop vector too large, vectortype {vector_size} not supported");
                                return (int)BlastError.ssmd_error_unsupported_vector_size;
                            }
#endif
                            vector_size = (byte)math.select(vector_size, 4, vector_size == 0);
                            if (current_op == 0)
                            {
                                // this is the first datapoint, nothing can happen to it so jump
                                code_pointer++;
                                continue;
                            }
                        }
                        break;

                    // direct handling of some often used function save an array copy in simple ops
                    case blast_operation.size:
                        {
                            // a lot more could be saved if we allow popwith op to run on it when we have an operation and its the last in sequence
                            code_pointer++;
                            if (!is_last_token_in_sequence || current_op != blast_operation.nop || !target_rec.is_set)
                            {
                                if (current_op == blast_operation.nop)
                                {
                                    GetSizeResult(ref code_pointer, ref vector_size, ssmd_datacount, f4_result);
                                }
                                else
                                {
                                    GetSizeResult(ref code_pointer, ref vector_size, ssmd_datacount, f4);
                                }

                                if (current_op == 0)
                                {
                                    code_pointer++;
                                    continue;
                                }
                            }
                            else
                            {
                                // not sure if it ends right.. 
                                GetSizeResult(ref code_pointer, ref vector_size, ssmd_datacount, null, target_rec);
                                return (int)BlastError.success;
                            }
                        }
                        break;


                    //
                    // CoreAPI functions mapping to opcodes not directly handled in sequencer 
                    // 
                    case blast_operation.abs:
                    case blast_operation.maxa:
                    case blast_operation.mina:
                    case blast_operation.max:
                    case blast_operation.min:
                    case blast_operation.select:
                    case blast_operation.fma:
                    case blast_operation.trunc:
                    case blast_operation.mula:
                    case blast_operation.adda:
                    case blast_operation.suba:
                    case blast_operation.diva:
                    case blast_operation.all:
                    case blast_operation.any:
                    case blast_operation.random:
                    case blast_operation.index_x:
                    case blast_operation.index_y:
                    case blast_operation.index_z:
                    case blast_operation.index_w:
                    case blast_operation.zero:
                    case blast_operation.ex_op:
                    // although we could handle there here the path is mosly the same and complicates stuff even more
                    // even if these are used in sequence, they will never output directly to target as that is only
                    // possible with no op pending and the last. it would always compile to assignf where expand is
                    // directly outputted to target if possible 
                    case blast_operation.expand_v2:
                    case blast_operation.expand_v3:
                    case blast_operation.expand_v4:
                    case blast_operation.index_n:
                        //
                        // on no operation pending
                        //
                        //  - we need a walker to walk the code bytes to determine if this is the last get_function so we can directly set target location 
                        //
                        if (current_op == 0)
                        {
                            // move directly to result
                            GetFunctionResult(f4_temp, ref code_pointer, ref vector_size, ssmd_datacount, f4_result);
                            if (minus || not)
                            {
                                if (minus)
                                {
                                    switch (vector_size)
                                    {
                                        case 0:
                                        case 4: simd.negate_f4(f4_result, ssmd_datacount); vector_size = 4; break;
                                        case 1: simd.negate_f1(f4_result, ssmd_datacount); vector_size = 1; break;
                                        case 2: simd.negate_f2(f4_result, ssmd_datacount); vector_size = 2; break;
                                        case 3: simd.negate_f3(f4_result, ssmd_datacount); vector_size = 3; break;
                                    }
                                }
                                if (not)
                                {
                                    switch (vector_size)
                                    {
                                        case 0:
                                        case 4: for (int i = 0; i < ssmd_datacount; i++) f4_result[i] = math.select((float4)0f, (float4)1f, f4_result[i] == 0); vector_size = 4; break;
                                        case 1: for (int i = 0; i < ssmd_datacount; i++) f4_result[i].x = math.select((float)0f, (float)1f, f4_result[i].x == 0); vector_size = 1; break;
                                        case 2: for (int i = 0; i < ssmd_datacount; i++) f4_result[i].xy = math.select((float2)0f, (float2)1f, f4_result[i].xy == 0); vector_size = 2; break;
                                        case 3: for (int i = 0; i < ssmd_datacount; i++) f4_result[i].xyz = math.select((float3)0f, (float3)1f, f4_result[i].xyz == 0); vector_size = 3; break;
                                    }
                                }
                                minus = false;
                                not = false;
                            }
                            code_pointer++;
                            continue;
                        }
                        else
                        {
                            GetFunctionResult(f4_temp, ref code_pointer, ref vector_size, ssmd_datacount, f4);
                            break;
                        }


                    case blast_operation.pi:
                    case blast_operation.inv_pi:
                    case blast_operation.epsilon:
                    case blast_operation.infinity:
                    case blast_operation.negative_infinity:
                    case blast_operation.nan:
                    case blast_operation.min_value:
                    case blast_operation.value_0:
                    case blast_operation.value_1:
                    case blast_operation.value_2:
                    case blast_operation.value_3:
                    case blast_operation.value_4:
                    case blast_operation.value_8:
                    case blast_operation.value_10:
                    case blast_operation.value_16:
                    case blast_operation.value_24:
                    case blast_operation.value_32:
                    case blast_operation.value_64:
                    case blast_operation.value_100:
                    case blast_operation.value_128:
                    case blast_operation.value_256:
                    case blast_operation.value_512:
                    case blast_operation.value_1000:
                    case blast_operation.value_1024:
                    case blast_operation.value_30:
                    case blast_operation.value_45:
                    case blast_operation.value_90:
                    case blast_operation.value_180:
                    case blast_operation.value_270:
                    case blast_operation.value_360:
                    case blast_operation.inv_value_2:
                    case blast_operation.inv_value_3:
                    case blast_operation.inv_value_4:
                    case blast_operation.inv_value_8:
                    case blast_operation.inv_value_10:
                    case blast_operation.inv_value_16:
                    case blast_operation.inv_value_24:
                    case blast_operation.inv_value_32:
                    case blast_operation.inv_value_64:
                    case blast_operation.inv_value_100:
                    case blast_operation.inv_value_128:
                    case blast_operation.inv_value_256:
                    case blast_operation.inv_value_512:
                    case blast_operation.inv_value_1000:
                    case blast_operation.inv_value_1024:
                    case blast_operation.inv_value_30:
                    case blast_operation.framecount:
                    case blast_operation.fixedtime:
                    case blast_operation.time:
                    case blast_operation.fixeddeltatime:
                    case blast_operation.deltatime:
                        {
                            // set constant, if a minus is present apply it 
                            float constant = engine_ptr->constants[op];
                            if (not || minus)
                            {
                                // these should not be both active 
                                constant = math.select(constant, -constant, minus);
                                constant = math.select(constant, constant == 0 ? 1 : 0, not);
                                minus = false;
                                not = false;
                            }

                            if (indexer >= 0)
                            {
                                vector_size = 1;
                            }
                            else
                            {
                                if (vector_size == 1) indexer = 0;
                            }

                            // detect if this is the last operation in sequence, were not pulling data for some pending operation and we may not be running nested 
                            blast_operation next_op = (blast_operation)code[code_pointer + 1];
                            if (target_rec.is_set && current_op == blast_operation.nop && (next_op == blast_operation.end || next_op == blast_operation.nop) && nesting_level <= 0)
                            {
                                // no current operation and at end of sequence => direct move of a value into target location 
                                switch (vector_size)
                                {
                                    case 0:
                                    case 4: simd.move_f1_constant_to_indexed_data_as_f4(target_rec.data, target_rec.row_size, target_rec.is_aligned, target_rec.index, constant, ssmd_datacount); break;
                                    case 3: simd.move_f1_constant_to_indexed_data_as_f3(target_rec.data, target_rec.row_size, target_rec.is_aligned, target_rec.index, constant, ssmd_datacount); break;
                                    case 2: simd.move_f1_constant_to_indexed_data_as_f2(target_rec.data, target_rec.row_size, target_rec.is_aligned, target_rec.index, constant, ssmd_datacount); break;
                                    case 1:
                                        {
                                            // if there is an indexer we can just add it to the target index 
                                            simd.move_f1_constant_to_indexed_data_as_f1(target_rec.data, target_rec.row_size, target_rec.is_aligned, math.select(target_rec.index, target_rec.index + indexer, indexer > 0), constant, ssmd_datacount); break;
                                        }
#if DEVELOPMENT_BUILD || TRACE
                                    default:
                                        Debug.LogError($"SSMD Interpretor, set constantvalue, codepointer: {code_pointer} => {code[code_pointer]}, error: vectorsize {vector_size} not supported");
                                        return (int)BlastError.ssmd_error_unsupported_vector_size;
#endif
                                }
                                // skip endlogic of sequence and return directly after setting the target 
                                code_pointer++;
                                return (int)BlastError.success;
                            }
                            else
                            {
                                // 
                                // there are multiple values AND an operation AND possibly a datatarget 
                                // - we set b here 
                                // - the op is handled later with handl_op 
                                //


                                // assign directly to result if this is the first operation (save a copy)                            
                                float4* f4_value = current_op != 0 ? f4 : f4_result;

                                switch ((BlastVectorSizes)vector_size)
                                {
                                    case BlastVectorSizes.float1:
                                        {
                                            // offset f4 destination by indexer count of words (1 word = 32 bit) 
                                            float4* p = ((float4*)(void*)&((float*)((void*)f4_value))[indexer]);
                                            // after that we can execute as if we index f4[0] 
                                            simd.move_f1_constant_into_f4_array_as_f1(p, constant, ssmd_datacount);
                                            // for (int i = 0; i < ssmd_datacount; i++) f4_value[i][indexer] = constant; break;
                                        }
                                        break;

                                    case BlastVectorSizes.float2: simd.move_f1_constant_into_f4_array_as_f2(f4_value, constant, ssmd_datacount); break;
                                    case BlastVectorSizes.float3: simd.move_f1_constant_into_f4_array_as_f3(f4_value, constant, ssmd_datacount); break;
                                    case 0:
                                    case BlastVectorSizes.float4: simd.move_f1_constant_into_f4_array_as_f4(f4_value, constant, ssmd_datacount); break;
#if DEVELOPMENT_BUILD || TRACE
                                    default:
                                        Debug.LogError($"SSMD Interpretor, set constantvalue, codepointer: {code_pointer} => {code[code_pointer]}, error: vectorsize {vector_size} not supported");
                                        return (int)BlastError.ssmd_error_unsupported_vector_size;
#endif

                                }
                            }
                            indexer = -1;
                        }
                        // if setting the first value, just start the loop again 
                        if (current_op == 0)
                        {
                            code_pointer++;
                            continue;
                        }
                        break;


                    // handle identifiers/variables
                    default:
                        {
                            if (op >= BlastInterpretor.opt_id) // only exop is higher and that is handled before default so this check is safe 
                            {
                                // lookup identifier value
                                byte id = (byte)(op - BlastInterpretor.opt_id);
                                byte this_vector_size = BlastInterpretor.GetMetaDataSize(metadata, id);

                                if (indexer >= 0)
                                {
                                    // offset id according to indexer and target vectorsize 1
                                    this_vector_size = 1;
                                    id = (byte)(id + indexer);
                                }

                                // if we have a target record: check end-of-sequence conditions: no nesting, no pending op, nextop == end|nop 
                                if (is_last_token_in_sequence && current_op == blast_operation.nop && nesting_level <= 0 && target_rec.is_set)
                                {
                                    // no operation => directly put value in target location 
                                    if (!(not || minus))
                                    {
                                        switch (this_vector_size)
                                        {
                                            case 0:
                                            case 4: simd.move_indexed_f4(target_rec.data, target_rec.row_size, target_rec.is_aligned, target_rec.index, data, data_rowsize, data_is_aligned, id, ssmd_datacount); vector_size = 4; break;
                                            case 3: simd.move_indexed_f3(target_rec.data, target_rec.row_size, target_rec.is_aligned, target_rec.index, data, data_rowsize, data_is_aligned, id, ssmd_datacount); vector_size = 3; break;
                                            case 2: simd.move_indexed_f2(target_rec.data, target_rec.row_size, target_rec.is_aligned, target_rec.index, data, data_rowsize, data_is_aligned, id, ssmd_datacount); vector_size = 2; break;
                                            case 1: simd.move_indexed_f1(target_rec.data, target_rec.row_size, target_rec.is_aligned, target_rec.index, data, data_rowsize, data_is_aligned, id, ssmd_datacount); vector_size = 1; break;
                                        }
                                    }
                                    else
                                    {
                                        switch (this_vector_size)
                                        {
                                            case 0:
                                            case 4: simd.move_indexed_f4_n(target_rec.data, target_rec.row_size, target_rec.is_aligned, target_rec.index, data, data_rowsize, data_is_aligned, id, ssmd_datacount); vector_size = 4; break;
                                            case 3: simd.move_indexed_f3_n(target_rec.data, target_rec.row_size, target_rec.is_aligned, target_rec.index, data, data_rowsize, data_is_aligned, id, ssmd_datacount); vector_size = 3; break;
                                            case 2: simd.move_indexed_f2_n(target_rec.data, target_rec.row_size, target_rec.is_aligned, target_rec.index, data, data_rowsize, data_is_aligned, id, ssmd_datacount); vector_size = 2; break;
                                            case 1: simd.move_indexed_f1_n(target_rec.data, target_rec.row_size, target_rec.is_aligned, target_rec.index, data, data_rowsize, data_is_aligned, id, ssmd_datacount); vector_size = 1; break;
                                        }
                                        minus = false;
                                        not = false;
                                    }
                                    // last in sequence, skip end logic and return
                                    code_pointer++;
                                    return (int)BlastError.success;
                                }
                                else
                                {
                                    // put value in a register  

                                    // if no operation is active
                                    float4* f4_value = current_op == blast_operation.nop ? f4_result : f4;

                                    // these should not possibly both be active 
                                    if (not || minus)
                                    {
                                        switch ((BlastVectorSizes)this_vector_size)
                                        {
                                            case BlastVectorSizes.float1: simd.set_from_data_f1(f4_value, data, id, minus, not, data_is_aligned, data_rowsize, ssmd_datacount); vector_size = 1; break;
                                            case BlastVectorSizes.float2: simd.set_from_data_f2(f4_value, data, id, minus, not, data_is_aligned, data_rowsize, ssmd_datacount); vector_size = 2; break;
                                            case BlastVectorSizes.float3: simd.set_from_data_f3(f4_value, data, id, minus, not, data_is_aligned, data_rowsize, ssmd_datacount); vector_size = 3; break;
                                            case 0:
                                            case BlastVectorSizes.float4: simd.set_from_data_f4(f4_value, data, id, minus, not, data_is_aligned, data_rowsize, ssmd_datacount); vector_size = 4; break;
                                        }
                                        minus = false;
                                        not = false;
                                    }
                                    else
                                    {
                                        // and put it in f4 
                                        switch ((BlastVectorSizes)this_vector_size)
                                        {
                                            case BlastVectorSizes.float1: simd.move_indexed_data_as_f1_to_f1f4(f4_value, data, data_rowsize, data_is_aligned, id, ssmd_datacount); vector_size = 1; break;
                                            case BlastVectorSizes.float2: simd.move_indexed_data_as_f2_to_f2(f4_value, data, data_rowsize, data_is_aligned, id, ssmd_datacount); vector_size = 2; break;
                                            case BlastVectorSizes.float3: simd.move_indexed_data_as_f3_to_f3(f4_value, data, data_rowsize, data_is_aligned, id, ssmd_datacount); vector_size = 3; break;
                                            case 0:
                                            case BlastVectorSizes.float4: simd.move_indexed_data_as_f4_to_f4(f4_value, data, data_rowsize, data_is_aligned, id, ssmd_datacount); vector_size = 4; break;
                                        }
                                    }
                                }

                                if (current_op == blast_operation.nop)
                                {
                                    code_pointer++;
                                    continue;
                                }
                            }
                            else
                            {
                                // we receive an operation while expecting a value 
                                //
                                // if we predict this at the compiler we could use the opcodes value as value constant
                                //
#if DEVELOPMENT_BUILD || TRACE
                                Debug.LogError($"SSMD Interpretor, codepointer: {code_pointer} => {code[code_pointer]}, encountered unknown operation {(blast_operation)op}");
#endif
                                return (int)BlastError.ssmd_invalid_operation;
                            }
                        }

                        break;
                }

                //
                // if not an operation overwrite result
                //
                // -> on first value
                //
                //
                if (current_op == 0 && !(minus && prev_op == 0))
                {
                    // just set according to vector size

                    // -> if target_rec is set, and this is the last operation in sequence 
                    //  - then we can skip this operation completely 
                    if (nesting_level <= 0 && target_rec.is_set && is_last_token_in_sequence)
                    {
                        // should not reach into this conditional path
                    }
                    else
                    {
                        if (minus)
                        {
                            switch ((BlastVectorSizes)vector_size)
                            {
                                case BlastVectorSizes.float1: simd.move_f1_array_into_f4_array_as_f1_n(f4_result, f4, ssmd_datacount); break;
                                case BlastVectorSizes.float2: simd.move_f2_array_into_f4_array_as_f2_n(f4_result, f4, ssmd_datacount); break;
                                case BlastVectorSizes.float3: simd.move_f3_array_into_f4_array_as_f3_n(f4_result, f4, ssmd_datacount); break;
                                case 0:
                                case BlastVectorSizes.float4: simd.move_f4_array_into_f4_array_as_f4_n(f4_result, f4, ssmd_datacount); break;
                            }
                            minus = false;
                        }
                        else
                        {
                            if (not && prev_op != 0)
                            {
                                switch ((BlastVectorSizes)vector_size)
                                {
                                    case BlastVectorSizes.float1: for (int i = 0; i < ssmd_datacount; i++) f4_result[i].x = math.select(0, 1, f4[i].x == 0f); break;
                                    case BlastVectorSizes.float2: for (int i = 0; i < ssmd_datacount; i++) f4_result[i].xy = math.select((float2)0f, (float2)1f, f4[i].xy == (float2)0f); break;
                                    case BlastVectorSizes.float3: for (int i = 0; i < ssmd_datacount; i++) f4_result[i].xyz = math.select((float3)0f, (float3)1f, f4[i].xyz == (float3)0f); break;
                                    case 0:
                                    case BlastVectorSizes.float4: for (int i = 0; i < ssmd_datacount; i++) f4_result[i] = math.select((float4)0f, (float4)1f, f4[i] == (float4)0f); break;
                                }
                                not = false;
                            }
                            else
                            {
                                switch ((BlastVectorSizes)vector_size)
                                {
                                    case BlastVectorSizes.float1: simd.move_f1_array_into_f4_array_as_f1(f4_result, f4, ssmd_datacount); break;
                                    case BlastVectorSizes.float2: simd.move_f2_array_into_f4_array_as_f2(f4_result, f4, ssmd_datacount); break;
                                    case BlastVectorSizes.float3: simd.move_f3_array_into_f4_array_as_f3(f4_result, f4, ssmd_datacount); break;
                                    case 0:
                                    case BlastVectorSizes.float4: simd.move_f4_array_into_f4_array_as_f4(f4_result, f4, ssmd_datacount); break;
                                }
                            }
                        }

                    }
                }
                else
                {
                    if (nesting_level <= 0 && target_rec.is_set && is_last_token_in_sequence)
                    {
                        // directly put result into target-rec 
                        handle_in_getsequence_operation_to_target(ref vector_size, ssmd_datacount, current_op, ref not, ref minus, f4, f4_result, prev_vector_size, target_rec);
                        code_pointer++;
                        return (int)BlastError.success;
                    }
                    else
                    {
                        // put result into register 
                        handle_in_getsequence_operation_to_register(ref vector_size, ssmd_datacount, current_op, ref not, ref minus, f4, f4_result, prev_vector_size);
                    }
                }

                // reset current operation when last thing was a value  (otherwise wont get here) 
                current_op = blast_operation.nop;

                // next
                code_pointer++;
            }

        end_seq:
            // if we reach here with a set target then it wasnt filled with data: the operation did not support output to data[][] directly 
            // - we need to copy the register to the target 
            if (target_rec.is_set)
            {
                switch (vector_size)
                {
                    case 0:
                    case 4: simd.move_f4_array_to_indexed_data_as_f4(target_rec.data, target_rec.row_size, target_rec.is_aligned, target_rec.index, f4_result, ssmd_datacount); break;
                    case 3: simd.move_f4_array_to_indexed_data_as_f3(target_rec.data, target_rec.row_size, target_rec.is_aligned, target_rec.index, f4_result, ssmd_datacount); break;
                    case 2: simd.move_f4_array_to_indexed_data_as_f2(target_rec.data, target_rec.row_size, target_rec.is_aligned, target_rec.index, f4_result, ssmd_datacount); break;
                    case 1: simd.move_f4_array_to_indexed_data_as_f1(target_rec.data, target_rec.row_size, target_rec.is_aligned, target_rec.index, f4_result, ssmd_datacount); break;
                    default: return (int)BlastError.success;
                }
                return (int)BlastError.success;
            }

            // check, if target record is set reaching here is an error
            // return math.select((int)BlastError.success, (int)BlastError.error_target_not_set_in_sequence, target_rec.is_set); 
            return (int)BlastError.success;
        }

        /// <summary>
        /// pulled out of getsequence for refactoring : handle op and put result back into target record location 
        /// </summary>
        void handle_in_getsequence_operation_to_target(ref byte vector_size, int ssmd_datacount, blast_operation current_op, ref bool not, ref bool minus, float4* f4, float4* f4_result, byte prev_vector_size, in DATAREC target)
        {
#if DEVELOPMENT_BUILD || TRACE
            if (!target.is_set)
            {
                Debug.LogError($"blast.ssmd.interpretor.handle-in-sequence-operation: target not set, codepointer = {code_pointer}");
                return;
            }
#else
            if (!target.is_set) return;
#endif


            switch ((BlastVectorSizes)prev_vector_size)
            {
                case BlastVectorSizes.float1:

                    switch ((BlastVectorSizes)vector_size)
                    {
                        case BlastVectorSizes.float1: handle_op_f1_f1(current_op, f4_result, f4, ref vector_size, ssmd_datacount, minus, not, in target); break;
                        case BlastVectorSizes.float2: handle_op_f1_f2(current_op, f4_result, f4, ref vector_size, ssmd_datacount, minus, not, in target); break;
                        case BlastVectorSizes.float3: handle_op_f1_f3(current_op, f4_result, f4, ref vector_size, ssmd_datacount, minus, not, in target); break;
                        case BlastVectorSizes.float4: handle_op_f1_f4(current_op, f4_result, f4, ref vector_size, ssmd_datacount, minus, not, in target); break;
#if DEVELOPMENT_BUILD || TRACE
                        default:
                            Debug.LogError($"SSMD Interpretor: execute-sequence: codepointer: {code_pointer} => {code[code_pointer]}, no support for vector of size {vector_size}");
                            break;
#endif
                    }
                    minus = false;
                    not = false;
                    break;


                case BlastVectorSizes.float2:

                    switch ((BlastVectorSizes)vector_size)
                    {
                        case BlastVectorSizes.float1: handle_op_f2_f1(current_op, f4_result, f4, ref vector_size, ssmd_datacount, minus, not, target); break;
                        case BlastVectorSizes.float2: handle_op_f2_f2(current_op, f4_result, f4, ref vector_size, ssmd_datacount, minus, not, target); break;
#if DEVELOPMENT_BUILD || TRACE
                        default:
                            Debug.LogError($"SSMD Interpretor: execute-sequence: codepointer: {code_pointer} => {code[code_pointer]}, vector operation undefined: {vector_size}");
                            break;
#endif
                    }
                    minus = false;
                    not = false;
                    break;

                case BlastVectorSizes.float3:

                    switch ((BlastVectorSizes)vector_size)
                    {
                        case BlastVectorSizes.float1: handle_op_f3_f1(current_op, f4_result, f4, ref vector_size, ssmd_datacount, minus, not, target); break;
                        case BlastVectorSizes.float3: handle_op_f3_f3(current_op, f4_result, f4, ref vector_size, ssmd_datacount, minus, not, target); break;

#if DEVELOPMENT_BUILD || TRACE
                        default:
                            Debug.LogError($"SSMD Interpretor: execute-sequence: codepointer: {code_pointer} => {code[code_pointer]}, vector operation undefined: {vector_size}");
                            break;
#endif
                    }
                    minus = false;
                    not = false;
                    break;


                case 0:
                case BlastVectorSizes.float4:

                    switch ((BlastVectorSizes)vector_size)
                    {
                        case BlastVectorSizes.float1: handle_op_f4_f1(current_op, f4_result, f4, ref vector_size, ssmd_datacount, minus, not, target); break;
                        case BlastVectorSizes.float4: handle_op_f4_f4(current_op, f4_result, f4, ref vector_size, ssmd_datacount, minus, not, target); break;
#if DEVELOPMENT_BUILD || TRACE
                        default:
                            Debug.LogError($"SSMD Interpretor: execute-sequence: codepointer: {code_pointer} => {code[code_pointer]}, vector operation undefined: {vector_size}");
                            break;
#endif
                    }
                    minus = false;
                    not = false;
                    break;


#if DEVELOPMENT_BUILD || TRACE
                default:
                    Debug.LogError($"SSMD Interpretor: execute-sequence: codepointer: {code_pointer} => {code[code_pointer]}, no support for vector of size {vector_size}");
                    break;
#endif


            }
        }


        /// <summary>
        /// pulled out of getsequence for refactoring : handle op and put result back into register 
        /// </summary>
        void handle_in_getsequence_operation_to_register(ref byte vector_size, int ssmd_datacount, blast_operation current_op, ref bool not, ref bool minus, float4* f4, float4* f4_result, byte prev_vector_size)
        {
            if ((minus || not))
            {
#if DEVELOPMENT_BUILD || TRACE
                if (minus && not)
                {
                    Debug.LogError("minus and not together active: error");
                }
#endif
                if (not)
                {
                    // this could be put in handle_op.. saving a loop
                    switch ((BlastVectorSizes)vector_size)
                    {
                        case BlastVectorSizes.float1: for (int i = 0; i < ssmd_datacount; i++) f4[i].x = math.select(0, 1, f4[i].x == 0f); break;
                        case BlastVectorSizes.float2: for (int i = 0; i < ssmd_datacount; i++) f4[i].xy = math.select((float2)0f, (float2)1f, f4[i].xy == (float2)0f); break;
                        case BlastVectorSizes.float3: for (int i = 0; i < ssmd_datacount; i++) f4[i].xyz = math.select((float3)0f, (float3)1f, f4[i].xyz == (float3)0f); break;
                        case 0:
                        case BlastVectorSizes.float4: for (int i = 0; i < ssmd_datacount; i++) f4[i] = math.select((float4)0f, (float4)1f, f4[i] == (float4)0f); break;
                    }
                    not = false;
                }

                if (minus)
                {
                    // this could be put in handle_op.. saving a loop
                    switch ((BlastVectorSizes)vector_size)
                    {
                        case BlastVectorSizes.float1: simd.move_f1_array_into_f4_array_as_f1_n(f4, f4, ssmd_datacount); break;
                        case BlastVectorSizes.float2: simd.move_f2_array_into_f4_array_as_f2_n(f4, f4, ssmd_datacount); break;
                        case BlastVectorSizes.float3: simd.move_f3_array_into_f4_array_as_f3_n(f4, f4, ssmd_datacount); break;
                        case 0:
                        case BlastVectorSizes.float4: simd.move_f4_array_into_f4_array_as_f4_n(f4, f4, ssmd_datacount); break;
                    }
                    minus = false;
                }
            }

            switch ((BlastVectorSizes)prev_vector_size)
            {
                case BlastVectorSizes.float1:

                    switch ((BlastVectorSizes)vector_size)
                    {
                        case BlastVectorSizes.float1: handle_op_f1_f1(current_op, f4_result, f4, ref vector_size, ssmd_datacount); break;
                        case BlastVectorSizes.float2: handle_op_f1_f2(current_op, f4_result, f4, ref vector_size, ssmd_datacount); break;
                        case BlastVectorSizes.float3: handle_op_f1_f3(current_op, f4_result, f4, ref vector_size, ssmd_datacount); break;
                        case BlastVectorSizes.float4: handle_op_f1_f4(current_op, f4_result, f4, ref vector_size, ssmd_datacount); break;
#if DEVELOPMENT_BUILD || TRACE
                        default:
                            Debug.LogError($"SSMD Interpretor: execute-sequence: codepointer: {code_pointer} => {code[code_pointer]}, no support for vector of size {vector_size}");
                            break;
#endif
                    }
                    break;


                case BlastVectorSizes.float2:

                    switch ((BlastVectorSizes)vector_size)
                    {
                        case BlastVectorSizes.float1: handle_op_f2_f1(current_op, f4_result, f4, ref vector_size, ssmd_datacount); break;
                        case BlastVectorSizes.float2: handle_op_f2_f2(current_op, f4_result, f4, ref vector_size, ssmd_datacount); break;
#if DEVELOPMENT_BUILD || TRACE
                        default:
                            Debug.LogError($"SSMD Interpretor: execute-sequence: codepointer: {code_pointer} => {code[code_pointer]}, vector operation undefined: {vector_size}");
                            break;
#endif
                    }
                    break;

                case BlastVectorSizes.float3:

                    switch ((BlastVectorSizes)vector_size)
                    {
                        case BlastVectorSizes.float1: handle_op_f3_f1(current_op, f4_result, f4, ref vector_size, ssmd_datacount); break;
                        case BlastVectorSizes.float3: handle_op_f3_f3(current_op, f4_result, f4, ref vector_size, ssmd_datacount); break;

#if DEVELOPMENT_BUILD || TRACE
                        default:
                            Debug.LogError($"SSMD Interpretor: execute-sequence: codepointer: {code_pointer} => {code[code_pointer]}, vector operation undefined: {vector_size}");
                            break;
#endif
                    }
                    break;


                case 0:
                case BlastVectorSizes.float4:

                    switch ((BlastVectorSizes)vector_size)
                    {
                        case BlastVectorSizes.float1: handle_op_f4_f1(current_op, f4_result, f4, ref vector_size, ssmd_datacount); break;
                        case BlastVectorSizes.float4: handle_op_f4_f4(current_op, f4_result, f4, ref vector_size, ssmd_datacount); break;
#if DEVELOPMENT_BUILD || TRACE
                        default:
                            Debug.LogError($"SSMD Interpretor: execute-sequence: codepointer: {code_pointer} => {code[code_pointer]}, vector operation undefined: {vector_size}");
                            break;
#endif
                    }
                    break;


#if DEVELOPMENT_BUILD || TRACE
                default:
                    Debug.LogError($"SSMD Interpretor: execute-sequence: codepointer: {code_pointer} => {code[code_pointer]}, no support for vector of size {vector_size}");
                    break;
#endif


            }
        }




        #region Stack Operation Handlers 

        BlastError push(ref int code_pointer, ref byte vector_size, [NoAlias] void* temp)
        {
            if (code[code_pointer] == (byte)blast_operation.begin)
            {
                code_pointer++;

                // push the vector result of a compound 
                BlastError res = (BlastError)GetSequenceResult(temp, ref code_pointer, ref vector_size, ssmd_datacount, default);
                if (res < 0)
                {
#if DEVELOPMENT_BUILD || TRACE
                    Debug.LogError($"blast.ssmd.execute.push: failed to read compound data for push operation at codepointer {code_pointer}");
#endif
                    return res;
                }

                switch (vector_size)
                {
                    case 1:
                        push_register_f1();
                        break;
                    case 2:
                        push_register_f2();
                        break;
                    case 3:
                        push_register_f3();
                        break;
                    case 4:
                        push_register_f4();
                        break;
                    default:
#if DEVELOPMENT_BUILD || TRACE
                        Debug.LogError("blast.ssmd push error: variable vector size not yet supported on stack push of compound");
#endif
                        return BlastError.error_variable_vector_compound_not_supported;
                }
                // code_pointer should index after compound now
            }
            else
            {
                // else directly push data
                BlastVariableDataType dt;
                byte size;

                pop_op_meta_cp(code_pointer, out dt, out size);
                size = (byte)math.select(size, 4, size == 0);
                push_pop_f(temp, ref code_pointer, size);
            }

            return BlastError.success;
        }

        BlastError pushv(ref int code_pointer, ref byte vector_size, [NoAlias] void* temp)
        {
            byte param_count = code[code_pointer++];
            vector_size = (byte)(param_count & 0b00001111); // vectorsize == lower 4 
            param_count = (byte)((param_count & 0b11110000) >> 4);  // param_count == upper 4 

            // either: vector_size or paramcount but NOT both 

            if (param_count == 1)
            {
                switch (vector_size)
                {
                    case 1:
                        push_pop_f(temp, ref code_pointer, vector_size);
                        break;

                    default:
#if DEVELOPMENT_BUILD || TRACE
                        Debug.LogError($"blast.ssmd.interpretor.pushv error: vectorsize {vector_size} not yet supported on stack push");
#endif
                        return BlastError.error_vector_size_not_supported;
                }
            }
            else
            {
#if DEVELOPMENT_BUILD || TRACE
                if (param_count != vector_size)
                {
                    Debug.LogError($"blast.ssmd.interpretor.pushv error: pushv => param_count > 1 && vector_size != param_count, this is not allowed. Vectorsize: {vector_size}, Paramcount: {param_count}");
                    return BlastError.error_unsupported_operation;
                }
#endif
                // param_count == vector_size and vector_size > 1, compiler enforces it so we should not check it in release 
                // this means that vector is build from multiple data points
                switch (vector_size)
                {
                    case 1:
                        push_pop_f(temp, ref code_pointer, 1);
                        break;
                    case 2:
                        push_f2_pop_11(temp, ref code_pointer);
                        break;
                    case 3:
                        push_f3_pop_111(temp, ref code_pointer);
                        break;
                    case 4:
                        push_f4_pop_1111(temp, ref code_pointer);
                        break;
                }
            }

            return BlastError.success;
        }

        BlastError pushc(ref int code_pointer, ref byte vector_size, [NoAlias] void* temp)
        {
            // push the vector result of a compound 
            BlastError res = (BlastError)GetSequenceResult(temp, ref code_pointer, ref vector_size, ssmd_datacount, default);
            if (res < 0)
            {
#if DEVELOPMENT_BUILD || TRACE
                Debug.LogError($"blast.ssmd.execute.pushc : failed to read compound data for pushc operation at codepointer {code_pointer}");
#endif
                return res;
            }
            switch (vector_size)
            {
                case 1:
                    push_register_f1();
                    break;
                case 2:
                    push_register_f2();
                    break;
                case 3:
                    push_register_f3();
                    break;
                case 4:
                    push_register_f4();
                    break;
                default:
#if DEVELOPMENT_BUILD || TRACE
                    Debug.LogError("blast.ssmd push error: variable vector size not yet supported on stack push of compound");
#endif
                    return BlastError.error_variable_vector_compound_not_supported;
            }

            return BlastError.success;
        }

        BlastError pushf(ref int code_pointer, ref byte vector_size, [NoAlias] void* temp)
        {
            //
            //  TODO -> split out GetFunctionResult() from GetCompoundResult 
            //
            BlastError res = (BlastError)GetFunctionResult(temp, ref code_pointer, ref vector_size, ssmd_datacount, register);
            if (res < 0)
            {
#if DEVELOPMENT_BUILD || TRACE
                Debug.LogError($"blast.ssmd.execute.pushf: failed to read function result data for pushf operation at codepointer {code_pointer}");
#endif
                return res;
            }
            switch (vector_size)
            {
                case 1: push_register_f1(); break;
                case 2: push_register_f2(); break;
                case 3: push_register_f3(); break;
                case 4: push_register_f4(); break;
                default:
#if DEVELOPMENT_BUILD || TRACE
                    Debug.LogError("blast.ssmd.interpretor.pushf error: variable vector size not yet supported on stack push");
#endif
                    return BlastError.error_variable_vector_op_not_supported;
            }

            code_pointer++;
            return BlastError.success;
        }

        #endregion


        #region Assignments

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void determine_assigned_indexer(ref int code_pointer, ref byte assignee_op, out bool is_indexed, out blast_operation indexer)
        {
            is_indexed = assignee_op < BlastInterpretor.opt_id;
            indexer = (blast_operation)math.select(0, assignee_op, is_indexed);
            code_pointer = math.select(code_pointer, code_pointer + 1, is_indexed);
            assignee_op = code[code_pointer];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        byte get_offset_from_indexed(in byte assignee, in blast_operation indexer, in bool is_indexed)
        {
            // if an indexer is set, then get offset (x = 0 , y = 1 etc) and add that to assignee
            return (byte)math.select(assignee, assignee + (int)(indexer - blast_operation.index_x), is_indexed);
        }


        /// <summary>
        /// assign single value 
        /// </summary>
        BlastError assigns(ref int code_pointer, [NoAlias] void* temp)
        {
#if DEVELOPMENT_BUILD || TRACE
            Assert.IsFalse(temp == null, "blast.ssmd.interpretor.assigns: temp buffer = NULL");
#endif

            // assign 1 value
            byte assignee_op = code[code_pointer];

            // determine indexer 
            bool is_indexed;
            blast_operation indexer;
            determine_assigned_indexer(ref code_pointer, ref assignee_op, out is_indexed, out indexer);

            // get assignee
            byte assignee = (byte)(assignee_op - BlastInterpretor.opt_id);
            byte s_assignee = BlastInterpretor.GetMetaDataSize(in metadata, in assignee);
            

            // the assigned must have room for the indexer if indexed 

#if DEVELOPMENT_BUILD || TRACE
            if (is_indexed && (s_assignee <= get_offset_from_indexed(0, indexer, is_indexed)))
            {
                Debug.LogError($"blast.ssmd.interpretor.assignsingle: cannot set component from vector at #{code_pointer}, component: {indexer}, vectorsize {s_assignee}, index out of range");
                return BlastError.error_indexer_out_of_bounds;
            }

            // the compiler should have cought this, if it didt wel that would be noticed before going in release
            // so we define this only for debug saving the if
            // cannot set a component from a vector (yet)
            if (s_assignee == 1 && is_indexed)
            {
                Debug.LogError($"blast.ssmd.interpretor.assignsingle: cannot set component from vector at #{code_pointer}, component: {indexer}");
                return BlastError.error_assign_component_from_vector;
            }
#endif

            // read into nxt value
            code_pointer++;

            // set at index depending on size of the assigned
            // s_assignee = data; 
            switch (math.select(s_assignee, 1, is_indexed))
            {
                case 1:
                    if (!is_indexed)
                    {
                        pop_fx_into_ref<float>(ref code_pointer, null, data, assignee, data_is_aligned, data_rowsize);
                    }
                    else
                    {
                        pop_fx_into_ref<float>(ref code_pointer, null, data, get_offset_from_indexed(in assignee, in indexer, in is_indexed), data_is_aligned, data_rowsize);
                    }
                    break;

                case 2:
                    pop_fx_into_ref<float2>(ref code_pointer, null, data, assignee, data_is_aligned, data_rowsize);
                    break;

                case 3:
                    pop_fx_into_ref<float3>(ref code_pointer, null, data, assignee, data_is_aligned, data_rowsize);
                    break;

                case 0:
                case 4:
                    pop_fx_into_ref<float4>(ref code_pointer, null, data, assignee, data_is_aligned, data_rowsize);
                    break;
            }

            return BlastError.success;
        }


        /// <summary>
        /// assign result of a sequence 
        /// </summary>
        BlastError assign(ref int code_pointer, ref byte vector_size, [NoAlias] void* temp)
        {
#if DEVELOPMENT_BUID || TRACE
            Assert.IsFalse(temp == null, "blast.ssmd.interpretor.assign: temp buffer = NULL");
#endif

            // advance 1 if on assign 
            code_pointer += math.select(0, 1, code[code_pointer] == (byte)blast_operation.assign);
            byte assignee_op = code[code_pointer];

            // determine indexer 
            bool is_indexed;
            blast_operation indexer;
            determine_assigned_indexer(ref code_pointer, ref assignee_op, out is_indexed, out indexer);

            if (indexer == blast_operation.index_n)
            {
                //
                // get array of indices to set for each assigned record
                //

                //    pop_fx_into_ref<>




            }

            // get assignee
            byte assignee = (byte)(assignee_op - BlastInterpretor.opt_id);

            // advance 2 if on begin 1 otherwise
            code_pointer += math.select(1, 2, code[code_pointer + 1] == (byte)blast_operation.begin);

            // get vectorsize of the assigned parameter 
            // vectorsize assigned MUST match 
            byte s_assignee = BlastInterpretor.GetMetaDataSize(in metadata, in assignee);


            if (ASSIGN_TO_TARGET)
            {
                // index data target 
                // - correct vectorsize 4 => it is 0 after compressing into 2 bits
                DATAREC data_target = IndexData(
                    get_offset_from_indexed(in assignee, in indexer, in is_indexed),
                    (byte)math.select(s_assignee, 4, s_assignee == 0));

                // run sequencer, putting the last step into the data-target             
                BlastError res = (BlastError)GetSequenceResult(temp, ref code_pointer, ref vector_size, ssmd_datacount, in data_target);
                if (res != BlastError.success)
                {
#if DEVELOPMENT_BUILD || TRACE
                    Debug.LogError($"blast.ssmd.assign: failed to read compound data and directly assign it to target at codepointer {code_pointer}");
#endif
                    return res;
                }

                // successfully set assignment target
                return BlastError.success;
            }
            else
            {
                // run sequence result into a register 
                BlastError res = (BlastError)GetSequenceResult(temp, ref code_pointer, ref vector_size, ssmd_datacount, default);
                if (res != BlastError.success)
                {
#if DEVELOPMENT_BUILD || TRACE
                    Debug.LogError($"blast.ssmd.assign: failed to read compound|sequence data for assign operation at codepointer {code_pointer}");
#endif
                    return res;
                }


#if DEVELOPMENT_BUILD || TRACE
                // compiler should have cought this 
                if (vector_size != 1 && is_indexed)
                {
                    Debug.LogError($"blast.ssmd.assign: cannot set component from vector at #{code_pointer}, component: {indexer}");
                    return BlastError.error_assign_component_from_vector;
                }
#endif

                // set assigned data fields in stack 
                switch ((byte)vector_size)
                {
                    case (byte)BlastVectorSizes.float1:
                        //if(Unity.Burst.CompilerServices.Hint.Unlikely(s_assignee != 1))
                        if (s_assignee != 1 && !is_indexed)
                        {
#if DEVELOPMENT_BUILD || TRACE
                            Debug.LogError($"blast.ssmd.assign: assigned vector size mismatch at #{code_pointer}, should be size '{s_assignee}', evaluated '1', data id = {assignee}");
#endif
                            return BlastError.error_assign_vector_size_mismatch;
                        }
                        set_data_from_register_f1(get_offset_from_indexed(in assignee, in indexer, in is_indexed));
                        break;

                    case (byte)BlastVectorSizes.float2:
                        if (s_assignee != 2)
                        {
#if DEVELOPMENT_BUILD || TRACE
                            Debug.LogError($"blast.ssmd.assign: assigned vector size mismatch at #{code_pointer}, should be size '{s_assignee}', evaluated '2'");
#endif
                            return BlastError.error_assign_vector_size_mismatch;
                        }
                        set_data_from_register_f2(assignee);
                        break;

                    case (byte)BlastVectorSizes.float3:
                        if (s_assignee != 3)
                        {
#if DEVELOPMENT_BUILD || TRACE || TRACE
                            Debug.LogError($"blast.ssmd.assign: assigned vector size mismatch at #{code_pointer}, should be size '{s_assignee}', evaluated '3'");
#endif
                            return BlastError.error_assign_vector_size_mismatch;
                        }
                        set_data_from_register_f3(assignee);
                        break;

                    case 0: // artifact from cutting 4 off at 2 bits 
                    case (byte)BlastVectorSizes.float4:
                        vector_size = 4;
                        if (s_assignee != 4)
                        {
#if DEVELOPMENT_BUILD || TRACE
                            Debug.LogError($"blast.ssmd.assign: assigned vector size mismatch at #{code_pointer}, should be size '{s_assignee}', evaluated '4'");
#endif
                            return BlastError.error_assign_vector_size_mismatch;
                        }
                        set_data_from_register_f4(assignee);
                        break;

                    default:
#if DEVELOPMENT_BUILD || TRACE
                        Debug.LogError($"blast.ssmd.assign: vector size {vector_size} not allowed at codepointer {code_pointer}");
#endif
                        return BlastError.error_unsupported_operation_in_root;
                }
            }

            return BlastError.success;
        }



        /// <summary>
        /// Assign the result of a function
        /// </summary>
        /// <param name="code_pointer"></param>
        /// <param name="vector_size"></param>
        /// <param name="temp"></param>
        /// <param name="minus"></param>
        /// <returns></returns>
        BlastError AssignF(ref int code_pointer, ref byte vector_size, [NoAlias] void* temp, bool minus)
        {
#if DEVELOPMENT_BUID || TRACE
            Assert.IsFalse(temp == null, "blast.ssmd.interpretor.assignf: temp buffer = NULL");
#endif

            // get assignee 
            byte assignee_op = code[code_pointer];

            // determine indexer 
            bool is_indexed;
            bool need_copy_to_register = true;
            blast_operation indexer;
            determine_assigned_indexer(ref code_pointer, ref assignee_op, out is_indexed, out indexer);

            // get assignee index
            byte assignee = (byte)(assignee_op - BlastInterpretor.opt_id);

#if DEVELOPMENT_BUILD || TRACE
            // check its size?? in debug only, functions return size in vector_size 
            byte s_assignee = BlastInterpretor.GetMetaDataSize(in metadata, in assignee);
#endif

            // process functions that can directly output to target first 
            code_pointer++;
            BlastError res;

            if (!minus)
            {
                switch ((blast_operation)code[code_pointer])
                {
                    //
                    // directly output to data skipping a register operation whenever possible
                    //
                    case blast_operation.size:
                        code_pointer++;
                        res = GetSizeResult(ref code_pointer, ref vector_size, ssmd_datacount, null,
                            IndexData(get_offset_from_indexed(in assignee, in indexer, in is_indexed), 1));

                        need_copy_to_register = false;
                        break;

                    case blast_operation.expand_v2:
                        code_pointer++;
                        res = ExpandF1IntoFn(2, ref code_pointer, ref vector_size, ssmd_datacount, null, IndexData(get_offset_from_indexed(in assignee, in indexer, in is_indexed), 2));
                        need_copy_to_register = false;
                        break;

                    case blast_operation.expand_v3:
                        code_pointer++;
                        res = ExpandF1IntoFn(3, ref code_pointer, ref vector_size, ssmd_datacount, null, IndexData(get_offset_from_indexed(in assignee, in indexer, in is_indexed), 3));
                        need_copy_to_register = false;
                        break;

                    case blast_operation.expand_v4:
                        code_pointer++;
                        res = ExpandF1IntoFn(4, ref code_pointer, ref vector_size, ssmd_datacount, null, IndexData(get_offset_from_indexed(in assignee, in indexer, in is_indexed), 4));
                        need_copy_to_register = false;
                        break;

                    case blast_operation.index_n:
                        code_pointer++;
                        res = IndexF1IntoFn(temp, ref code_pointer, ref vector_size, ssmd_datacount, null, IndexData(get_offset_from_indexed(in assignee, in indexer, in is_indexed), 1));
                        need_copy_to_register = false;
                        //code_pointer++;
                        break;

                    default:
                        res = (BlastError)GetFunctionResult(temp, ref code_pointer, ref vector_size, ssmd_datacount, register);
                        break;
                }
            }
            else
            {
                // with minus flag enabled we need to use the register returned 
                res = (BlastError)GetFunctionResult(temp, ref code_pointer, ref vector_size, ssmd_datacount, register);
            }

            //
            // CODE POINTER MOET GOED ZIJN NA GETFUNCTION 
            //
            code_pointer++; 
          

#if DEVELOPMENT_BUILD || TRACE
            if (res == (int)BlastError.success)
            {
                // compiler should have cought this 
                if (vector_size != 1 && is_indexed)
                {
                    Debug.LogError($"blast.ssmd.assignf: cannot set component from vector at #{code_pointer}, component: {indexer}");
                    return BlastError.error_assign_component_from_vector;
                }

                // 4 == 0 depending on decoding 
                s_assignee = (byte)math.select(s_assignee, 4, s_assignee == 0);
                if (!is_indexed && s_assignee != vector_size)
                {
                    Debug.LogError($"SSMD Interpretor: assign function, vector size mismatch at #{code_pointer}, should be size '{s_assignee}', function returned '{vector_size}', data id = {assignee}");
                    return BlastError.error_assign_vector_size_mismatch;
                }
            }
#endif

            if (need_copy_to_register)
            {
                set_data_from_register(
                    vector_size,
                    get_offset_from_indexed(in assignee, in indexer, in is_indexed),
                    minus);
            }
            return res;
        }

        BlastError assignfe(ref int code_pointer, ref byte vector_size, [NoAlias] void* temp, bool minus)
        {
#if DEVELOPMENT_BUID || TRACE
            Assert.IsFalse(temp == null, "blast.ssmd.interpretor.assignfe: temp buffer = NULL");
#endif

            // get assignee 
            byte assignee_op = code[code_pointer];

            // determine indexer 
            bool is_indexed;
            blast_operation indexer;
            determine_assigned_indexer(ref code_pointer, ref assignee_op, out is_indexed, out indexer);

            // get assignee index
            byte assignee = (byte)(assignee_op - BlastInterpretor.opt_id);

#if DEVELOPMENT_BUILD || TRACE
            // check its size?? in debug only, functions return size in vector_size 
            byte s_assignee = BlastInterpretor.GetMetaDataSize(in metadata, in assignee);
#endif
            code_pointer++;
            CallExternalFunction(temp, ref code_pointer, ref vector_size, ssmd_datacount, register);
            code_pointer++;

#if DEVELOPMENT_BUILD || TRACE
            // 4 == 0 depending on decoding 
            s_assignee = (byte)math.select(s_assignee, 4, s_assignee == 0);
            if (!is_indexed && s_assignee != vector_size)
            {
                Debug.LogError($"SSMD Interpretor: assign external function, vector size mismatch at #{code_pointer}, should be size '{s_assignee}', function returned '{vector_size}', data id = {assignee}");
                return BlastError.error_assign_vector_size_mismatch;
            }
#endif

#if DEVELOPMENT_BUILD || TRACE
            // compiler should have cought this 
            if (vector_size != 1 && is_indexed)
            {
                Debug.LogError($"blast.ssmd.assignf: cannot set component from vector at #{code_pointer}, component: {indexer}");
                return BlastError.error_assign_component_from_vector;
            }
#endif

            set_data_from_register(
                vector_size,
                get_offset_from_indexed(in assignee, in indexer, in is_indexed),
                minus);

            return BlastError.success;
        }

        BlastError assignv(ref int code_pointer, ref byte vector_size, [NoAlias] void* temp)
        {
#if DEVELOPMENT_BUID || TRACE
            Assert.IsFalse(temp == null, "blast.ssmd.interpretor.assignv: temp buffer = NULL");
#endif

            byte assignee_op = code[code_pointer];

            // determine indexer 
            bool is_indexed;
            blast_operation indexer;
            determine_assigned_indexer(ref code_pointer, ref assignee_op, out is_indexed, out indexer);

            // get assignee index
            byte assignee = (byte)(assignee_op - BlastInterpretor.opt_id);
            vector_size = BlastInterpretor.GetMetaDataSize(in metadata, in assignee);

            code_pointer++;

#if DEVELOPMENT_BUILD || TRACE
            // compiler should have cought this 
            if (vector_size != 1 && is_indexed)
            {
                Debug.LogError($"blast.ssmd.assignf: cannot set component from vector at #{code_pointer}, component: {indexer}");
                return BlastError.error_assign_component_from_vector;
            }
#endif

            assign_pop_f(temp, ref code_pointer, vector_size, assignee);


            return BlastError.success;
        }

        #region CDATA Assignments

        /// <summary>
        /// Assignment to CDATA 
        /// </summary>
        BlastError assign_cdata(ref int code_pointer, ref byte vector_size, [NoAlias] void* temp, [NoAlias] float4* register)
        {
            int cdata_offset, cdata_length;
            CDATAEncodingType encoding; 

            BlastInterpretor.follow_cdataref(code, code_pointer - 1, out cdata_offset, out cdata_length, out encoding);
            code_pointer += 2;

            int index = -1;
            bool constant_index;
            bool constant_data;
            //bool minus = false;

            // switch to assign|indexer 
            switch ((blast_operation)code[code_pointer])
            {
                case blast_operation.index_x: code_pointer++; index = 0; constant_index = true; break;
                case blast_operation.index_y: code_pointer++; index = 1; constant_index = true; break;
                case blast_operation.index_z: code_pointer++; index = 2; constant_index = true; break;
                case blast_operation.index_w: code_pointer++; index = 3; constant_index = true; break;
                case blast_operation.index_n:
                    {
                        // read indexer, must be vector_size 1
                        float* p = (float*)temp;
                        code_pointer++;

                        if (pop_fx_into_ref(ref code_pointer, p, ssmd_datacount, true))
                        {
                            if (math.isnan(p[0])) return BlastError.error_failed_to_read_cdata_indexer;

                            // constant index, use only 1 record as all will be the same anyway
                            index = (int)p[0];
                            constant_index = true;
                        }
                        else
                        {
                            constant_index = false;
                        }
                    }
                    break;

                // if indexer is skipped and we find an assignment, constantly index at 0
                case blast_operation.assigns:
                case blast_operation.assignf:
                case blast_operation.assignfe:
                case blast_operation.assign: index = 0; constant_index = true; break;

                case blast_operation.assignfn:
                case blast_operation.assignfen: index = 0; constant_index = true; break;

                case blast_operation.assignv:
                    {
                        if (IsTrace)
                        {
                            Debug.LogError($"Blast.ssmd.assign_cdata: cannot assign a vector to a cdata[] component at codepointer {code_pointer}");
                        }
                        return BlastError.error_in_cdata_assignment_size;
                    }

                // anything else should result in an error
                default:
                    return BlastError.error_failed_to_read_cdata_indexer;
            }



            // validate index if its constant 
            int element_bytesize = BlastInterpretor.GetCDATAEncodingByteSize(encoding); 
            if (constant_index && (index < 0 || index > cdata_length / element_bytesize))
            {
                if (IsTrace)
                {
                    Debug.LogError($"Blast.ssmd.assign_cdata: cdata indexer out of bounds, codepointer = {code_pointer}");
                }
                return BlastError.error_cdata_indexer_out_of_bounds;
            }

            // process the assign operation
            //
            // - this will overwrite each execution of each record and effectively sets the last value
            //   IN ALL CASES IF CONSTANTLY INDEXED
            //   we do however not know for sure when a script writes or reads in the compiler because it might be conditionally depending on data
            //
            //   if we reach here however WE know it is executed for this data stream
            //   and as blast gives no guarantee which record is executed when ...
            //
            BlastVariableDataType assigned_datatype;

            switch ((blast_operation)code[code_pointer])
            {
                // set value directly to target when possible 
                case blast_operation.assigns:
                    {
                        code_pointer++;
                        if (constant_index)
                        {
                            // can reuse temp buffer, we force pop_fx_into_ref into reading only 1 record into the buffer
                            pop_op_meta_cp(code_pointer, out assigned_datatype, out vector_size);
                            switch (vector_size)
                            {
                                case 0:
                                case 4: constant_data = pop_fx_into_ref<float4>(ref code_pointer, (float4*)temp, 1, true); break;
                                case 3: constant_data = pop_fx_into_ref<float3>(ref code_pointer, (float3*)temp, 1, true); break;
                                case 2: constant_data = pop_fx_into_ref<float2>(ref code_pointer, (float2*)temp, 1, true); break;
                                case 1: constant_data = pop_fx_into_ref<float>(ref code_pointer, (float*)temp, 1, true); break;
                                default: return BlastError.error_vector_size_not_supported;
                            }

                            // same index everywhere, set once 
                            switch (vector_size)
                            {
                                case 1: BlastInterpretor.set_cdata_float(code, cdata_offset, index, ((float*)temp)[0]); break;
                                case 2: BlastInterpretor.set_cdata_float(code, cdata_offset, index, ((float2*)temp)[0]); break;
                                case 3: BlastInterpretor.set_cdata_float(code, cdata_offset, index, ((float3*)temp)[0]); break;
                                case 0:
                                case 4: BlastInterpretor.set_cdata_float(code, cdata_offset, index, ((float4*)temp)[0]); break;
                                default: return BlastError.error_vector_size_not_supported;
                            }

                            // compensate for pop_fx_into_ref
                            // code_pointer--; 

                            // ok, assignment complete nxt token please
                            return BlastError.success;
                        }
                        else
                        {
                            // each record may write to a different index, we have to process them all for now
                            //
                            // = we only allow float1' in this way...  compiler does not know what size 
                            // 
                            // additional memory is needed, we use another function so we only allocate it in non-constant index cases
                            // 
                            pop_op_meta_cp(code_pointer, out assigned_datatype, out vector_size);
                            switch (vector_size)
                            {
                                case 1: assigns_cdata_indexed(ref code_pointer, cdata_offset, (float*)temp, vector_size); break;
                                case 2: assigns_cdata_indexed(ref code_pointer, cdata_offset, (float*)temp, vector_size); break;
                                case 3: assigns_cdata_indexed(ref code_pointer, cdata_offset, (float*)temp, vector_size); break;
                                case 0:
                                case 4: assigns_cdata_indexed_f4(ref code_pointer, cdata_offset, (float*)temp, vector_size); break;
                                default: return BlastError.error_vector_size_not_supported;
                            }

                            return BlastError.success;
                        }
                    }
                    break;


                /* TODO: compiler update in ssmd: we could omit the assignf by directly encoding the function opcodes and switch to function @ default, if not handled -> error */
                case blast_operation.assignf:
                    {
                        return assignf_cdata(ref code_pointer, ref vector_size, temp, ref register, constant_index, index, cdata_offset, cdata_length, false);
                    }

                // negation of the above 
                case blast_operation.assignfn:
                    {
                        return assignf_cdata(ref code_pointer, ref vector_size, temp, ref register, constant_index, index, cdata_offset, cdata_length, true);
                    }

                case blast_operation.assign:
                    {
                        return assign_cdata_sequence(ref code_pointer, ref vector_size, temp, ref register, constant_index, index, cdata_offset, cdata_length, true);
                    }

                case blast_operation.assignfe:
                case blast_operation.assignfen:
                    return BlastError.error_failed_to_interpret_cdata_assignment;

                default:
                    return BlastError.error_failed_to_interpret_cdata_assignment;

                // cannot assign vector to cdata components
                case blast_operation.assignv:
                    return BlastError.error_cannot_assign_vector_to_cdata_component;

            }

            return BlastError.error_failed_to_interpret_cdata_assignment;
        }


        /// <summary>
        /// Assign the result of running a sequence to the cdata component
        /// </summary>
        private BlastError assign_cdata_sequence(ref int code_pointer, ref byte vector_size, void* temp, ref float4* register, bool constant_index, int index, int cdata_offset, int cdata_length, bool v)
        {
            code_pointer++;

            if (constant_index)
            {
                // run sequence and put result in register 
                BlastError res = (BlastError)GetSequenceResult(temp, ref code_pointer, ref vector_size, 1, default);
                if (res != BlastError.success) return res;

                // assign data at constant index index 
                BlastInterpretor.set_cdata_float(code, cdata_offset, index, register[0].x);
            }
            else
            {
                // allocate new temp memory in another non-inlined function 
                BlastError res = (BlastError)assignf_cdata_getsequence_withmemory(ref code_pointer, ref vector_size, ssmd_datacount);
                if (res != BlastError.success) return res;

                // assign data to indices 
                float* indices = (float*)temp;
                for (int i = 0; i < ssmd_datacount; i++)
                {
                    BlastInterpretor.set_cdata_float(code, cdata_offset, (int)indices[i], register[i]);
                }
            }
            return BlastError.success;
        }

        private BlastError assignf_cdata_getsequence_withmemory(ref int code_pointer, ref byte vector_size, int ssmd_datacount)
        {
            // we need to get more memory..... 
            float4* temp2 = stackalloc float4[ssmd_datacount];
            BlastError res = (BlastError)GetSequenceResult((void*)temp2, ref code_pointer, ref vector_size, ssmd_datacount, default);
            if (res == BlastError.success)
            {
                // code_pointer--; 
            }
            return res;
        }

        /// <summary>
        /// assign result of function to a cdata index
        /// </summary>
        BlastError assignf_cdata(ref int code_pointer, ref byte vector_size, void* temp, ref float4* register, bool constant_index, int index, int cdata_offset, int cdata_length, bool is_negated)
        {
            code_pointer++;
            if (constant_index)
            {
                // get data once for one record, with a constant index to set its pointless to run all records  
                BlastError res = (BlastError)GetFunctionResult(temp, ref code_pointer, ref vector_size, 1, register, default, true);

                code_pointer++;
                if (res != BlastError.success) return res;

                // assign data at constant index index 
                BlastInterpretor.set_cdata_float(code, cdata_offset, index, math.select(register[0].x, -register[0].x, is_negated));
                return BlastError.success;
            }
            else
            {
                //
                // !!! ->> cant use temp buffer; it is used for storing the indices to set !!!
                // stackalloc in another function; in an attempt to remove it from the standard hot path
                //
                BlastError res = assignf_cdata_getfunction_withmemory(ref code_pointer, ref vector_size, ref register);
                if (res != BlastError.success) return res;


                // negate data if needed 
                if (is_negated) simd.negate_f1(register, ssmd_datacount);

                // assign data to indices 
                float* indices = (float*)temp;
                for (int i = 0; i < ssmd_datacount; i++)
                {
                    BlastInterpretor.set_cdata_float(code, cdata_offset, (int)indices[i], register[i]);
                }

                return BlastError.success;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private BlastError assignf_cdata_getfunction_withmemory(ref int code_pointer, ref byte vector_size, ref float4* register)
        {
            // we need to get more memory..... 
            float4* temp2 = stackalloc float4[ssmd_datacount];
            BlastError res = GetFunctionResult(temp2, ref code_pointer, ref vector_size, ssmd_datacount, register, default, true);
            if (res == BlastError.success)
            {
                // code_pointer--; 
            }
            return res;
        }


        bool assigns_cdata_indexed_f4(ref int code_pointer, int cdata_offset, float* cdata_indices, byte vector_size)
        {
            // allocate extra temp as the current buffer is used for cdata_indices 
            // - stackalloc is always allocated even if this is branched out 
            // all other indices can use the rest float3 in the temp data array after cdata_indices 
            float4* temp2 = stackalloc float4[ssmd_datacount];

            switch (vector_size)
            {
                case 0:
                case 4:
                    {
                        if (pop_fx_into_ref<float4>(ref code_pointer, temp2, ssmd_datacount, true))
                        {
                            // constant data assignment, but differing indices 
                            for (int i = 0; i < ssmd_datacount; i++)
                            {
                                BlastInterpretor.set_cdata_float(code, cdata_offset, (int)cdata_indices[i], temp2[0]);
                            }
                        }
                        else
                        {
                            // variable data    
                            for (int i = 0; i < ssmd_datacount; i++)
                            {
                                BlastInterpretor.set_cdata_float(code, cdata_offset, (int)cdata_indices[i], temp2[i]);
                            }
                        }
                        return true;
                    }
                    break;
            }
            return false;
        }

        bool assigns_cdata_indexed(ref int code_pointer, int cdata_offset, float* cdata_indices, byte vector_size)
        {
            // NOTE IMPORTANT 
            // cdata_indices is backed by temp filled with float1 indices, so we have room upto float3
            // 
            switch (vector_size)
            {
                case 1:
                    {
                        float* f1 = (float*)(void*)&cdata_indices[ssmd_datacount];
                        if (pop_fx_into_ref<float>(ref code_pointer, f1, ssmd_datacount, true))
                        {
                            // constant data assignment, but differing indices 
                            for (int i = 0; i < ssmd_datacount; i++) BlastInterpretor.set_cdata_float(code, cdata_offset, (int)cdata_indices[i], f1[0]);
                        }
                        else
                        {
                            // variable data    
                            for (int i = 0; i < ssmd_datacount; i++) BlastInterpretor.set_cdata_float(code, cdata_offset, (int)cdata_indices[i], f1[i]);
                        }
                        return true;
                    }
                    break;

                case 2:
                    {
                        float2* f2 = (float2*)(void*)&cdata_indices[ssmd_datacount];
                        if (pop_fx_into_ref<float2>(ref code_pointer, f2, ssmd_datacount, true))
                        {
                            // constant data assignment, but differing indices 
                            for (int i = 0; i < ssmd_datacount; i++) BlastInterpretor.set_cdata_float(code, cdata_offset, (int)cdata_indices[i], f2[0]);
                        }
                        else
                        {
                            // variable data    
                            for (int i = 0; i < ssmd_datacount; i++) BlastInterpretor.set_cdata_float(code, cdata_offset, (int)cdata_indices[i], f2[i]);
                        }
                        return true;
                    }
                    break;

                case 3:
                    {
                        float3* f3 = (float3*)(void*)&cdata_indices[ssmd_datacount];
                        if (pop_fx_into_ref<float3>(ref code_pointer, f3, ssmd_datacount, true))
                        {
                            // constant data assignment, but differing indices 
                            for (int i = 0; i < ssmd_datacount; i++) BlastInterpretor.set_cdata_float(code, cdata_offset, (int)cdata_indices[i], f3[0]);
                        }
                        else
                        {
                            // variable data    
                            for (int i = 0; i < ssmd_datacount; i++) BlastInterpretor.set_cdata_float(code, cdata_offset, (int)cdata_indices[i], f3[i]);
                        }
                        return true;
                    }
                    break;

                case 0: // reroute to other function, need to allocate memory to hold the f4 array on the stack 
                case 4: return assigns_cdata_indexed_f4(ref code_pointer, cdata_offset, cdata_indices, 4);
            }
            return false;
        }

        #endregion
        #endregion



        #region EvalPushedJumpCondition

        BlastError EvalPushedJumpCondition(ref int code_pointer, [NoAlias]float4* register, [NoAlias]void* temp, int ssmd_datacount)
        {
            // eval all push commands before running the compound
            bool is_push = true;
            byte vector_size = 1;

            BlastError res; 

            do
            {
                switch ((blast_operation)code[code_pointer])
                {
                    case blast_operation.push:
                        code_pointer++;
                        res = push(ref code_pointer, ref vector_size, temp);
                        MaxStackSizeReachedDuringValidation = math.max(MaxStackSizeReachedDuringValidation, stack_offset);
                        if (res != BlastError.success) return res;
                        code_pointer++;
                        break;

                    //
                    // pushv knows more about what it is pushing up the stack, it can do it without running the compound 
                    //
                    case blast_operation.pushv:
                        code_pointer++;
                        res = pushv(ref code_pointer, ref vector_size, temp);
                        MaxStackSizeReachedDuringValidation = math.max(MaxStackSizeReachedDuringValidation, stack_offset);
                        if (res != BlastError.success) return res;
                        code_pointer++;
                        break;

                    //
                    // push the result from a function directly onto the stack, saving a call to getcompound 
                    //
                    case blast_operation.pushf:
                        code_pointer++;
                        res = pushf(ref code_pointer, ref vector_size, temp);
                        MaxStackSizeReachedDuringValidation = math.max(MaxStackSizeReachedDuringValidation, stack_offset);
                        if (res != BlastError.success) return res;
                        code_pointer++;
                        break;

                    //
                    // push the result from a compound directly onto the stack 
                    //
                    case blast_operation.pushc:
                        code_pointer++;
                        res = pushc(ref code_pointer, ref vector_size, temp);
                        MaxStackSizeReachedDuringValidation = math.max(MaxStackSizeReachedDuringValidation, stack_offset);
                        if (res != BlastError.success) return res;
                        code_pointer++;
                        break;

                    default:
                        is_push = false;
                        break;
                }
            }
            while (is_push && code_pointer < package.CodeSize);

            // get result from condition
            // compiler has put braces|compound around it in certain situations    TODO FIX
            // while they are not always needed 
            code_pointer = math.select(code_pointer, code_pointer + 1, (blast_operation)code[code_pointer] == blast_operation.begin);


            // process sequence into register
            return (BlastError)GetSequenceResult(temp, ref code_pointer, ref vector_size, ssmd_datacount, default);
        }


        #endregion 









        const bool ASSIGN_TO_TARGET = true;


        /// <summary>
        /// main interpretor loop 
        /// </summary>
        /// <param name="syncbuffer"></param>
        /// <param name="datastack"></param>
        /// <param name="ssmd_data_count">should be divisable by 8</param>
        /// <param name="f4_register"></param>
        /// <returns></returns>
#if !STANDALONE_VSBUILD
        [SkipLocalsInit]
#endif
        int Execute([NoAlias] byte* syncbuffer, [NoAlias] byte** datastack, int ssmd_data_count, [NoAlias] float4* f4_register)
        {
            data = (void**)datastack;
            syncbits = syncbuffer;
            ssmd_datacount = ssmd_data_count;
            register = f4_register;

            if (ssmd_datacount <= 0 || ssmd_datacount > 1024 * 64)
            {
                return (int)BlastError.error_invalid_ssmd_count;
            }

            // init random state if needed 
            if (engine_ptr != null && random.state == 0)
            {
                random = Unity.Mathematics.Random.CreateFromIndex(engine_ptr->random.NextUInt());
            }

            // run with internal stacks? 
            if (package.HasStackPackaged)
            {
                // the stack is located directly after the last data element 
                stack = data;
                stack_offset = package.DataSegmentStackOffset / 4;
            }
            else
            {
                // run with the stack allocated only during interpretation 
                // - although stack is shared, metadata is not (1 byte/data index)
                //   in ssmd mode only the data may change, metadata will be the same
                //   and as such it is allocated along with the codesegment 
                //
                // this has 1 tiny problem while indexing our stack: as interpretor assumes the stack is directly
                // after the datasegment its stackoffset is located at the end of that segment and this stack 
                // offset is directly used to index metadata.
                //
                // when allocating local stack, we offset the memory pointer with -stacksize bytes
                // so that we can use the same indexing methods regardless the origin of the stack pointer 

                // allocate local stack 



                byte* p = stackalloc byte[package.StackSize * ssmd_datacount];

                // generate a list of stacksegment pointers on the stack:
                byte** stack_indices = stackalloc byte*[ssmd_datacount];

                for (int i = 0; i < ssmd_data_count; i++)
                {
                    //ulong u64 = (ulong)(p + (package.StackSize * i));   

                    // offset the index to match the stackoffset
                    //u64 -= package.DataSegmentStackOffset;

                    stack_indices[i] = (byte*)(p + (package.StackSize * i));//u64;
                }

                stack = (void**)stack_indices;

                // offset as if the stack is located in the datasegment  
                stack_offset = 0; //package.DataSegmentStackOffset >> 2;
            }

            // pre-fill stack with infs for validation/stack estimation
            if (ValidateOnce)
            {
                MaxStackSizeReachedDuringValidation = stack_offset;

                if (!package.HasStackPackaged)
                {
                    // there is no way to check the result
#if TRACE || DEVELOPMENT_BUILD
                    Debug.LogWarning("BlastSSMDInterpretor: executing with validation enabled but not with a packaged stack, stacksize estimation wont be possible");
#endif
                }
                // done by caller in packaged stack
                //else
                //{
                //    for (int i = 0; i < ssmd_data_count; i++)
                //    {
                //        // todo check if stacksize is correct on packaged stack with stackoffset != 0
                //        for (int j = stack_offset; j < package.StackSize; j++)
                //        {
                //            ((float*)stack[i])[j] = math.INFINITY;
                //        }
                //    }
                //}
            }

            // local temp buffer 
            void* temp = stackalloc float4[ssmd_datacount];  //temp buffer of max vectorsize * 4 * ssmd_datacount 

            // temp index buffer 
            void* temp_indices = stackalloc float[ssmd_datacount];
            indices = (float*)temp_indices; 

#if DEVELOPMENT_BUILD || TRACE

            Assert.IsTrue(data != null, $"blast.ssmd.interpretor: data == null");
            Assert.IsTrue(register != null, $"blast.ssmd.interpretor: register allocation failed: ssmd_datacount = {ssmd_datacount}");
            Assert.IsTrue(temp != null, $"blast.ssmd.interpretor: stack allocation failed: ssmd_datacount = {ssmd_datacount}");
#endif
            // assume all data is set 
            int iterations = 0;
            int ires = 0; // anything < 0 == error
            code_pointer = 0;

            //*************************************************************************************************************************
            // check data array, is it equaly spaced => ifso this opens up optimizations on dataaccess  
            //*************************************************************************************************************************
            data_is_aligned = ssmd_datacount > 1;
            if (data_is_aligned)
            {
                // TODO
                // - this check potentially hurts performance if its aligned and the script is very short 
                //   although it does make up for it when using float2 and float3 vectors: upto 3x faster in .net core builds 
                // 
                data_rowsize = (int)(((ulong*)data)[1] - ((ulong*)data)[0]);
                data_is_aligned = (data_rowsize % 4) == 0; // would be wierd if not 

                int i = 0;
                while (data_is_aligned && i < ssmd_datacount - 4)
                {
                    // on the very small runs this can slow down 
                    data_is_aligned =
                        data_rowsize == (int)(((ulong*)data)[i + 1] - ((ulong*)data)[i])
                        &&
                        data_rowsize == (int)(((ulong*)data)[i + 2] - ((ulong*)data)[i + 1])
                        &&
                        data_rowsize == (int)(((ulong*)data)[i + 3] - ((ulong*)data)[i + 2])
                        &&
                        data_rowsize == (int)(((ulong*)data)[i + 4] - ((ulong*)data)[i + 3]);
                    i += 4;
                }
                while (data_is_aligned && i < ssmd_datacount - 1)
                {
                    data_is_aligned = data_rowsize == (int)(((ulong*)data)[i + 1] - ((ulong*)data)[i]);
                    i++;
                }
                // for (int i = 1; data_is_aligned && i < ssmd_datacount - 1; i++)
                // {
                //     data_is_aligned = data_rowsize == (int)(((ulong*)data)[i + 1] - ((ulong*)data)[i]);
                // }
            }
            else
            {
                data_is_aligned = false;
                data_rowsize = 0;
            }

            // check out stack 
            // if stack is packaged, it is aligned the same as data, otherwise it has been packed on function stack (perfectly aligned)
            if (package.HasStackPackaged)
            {
                stack_is_aligned = data_is_aligned;
                stack_rowsize = data_rowsize;
            }
            else
            {
                // using thread stack, always packed 
                stack_rowsize = (int)package.StackSize;
                stack_is_aligned = (package.StackSize % 4) == 0;
            }


#if DEVELOPMENT_BUILD || TRACE
            if (ssmd_datacount > 1 && !data_is_aligned)
            {
                ShowWarningOnce(WARN_SSMD_DATASEGMENTS_NOT_EVENLY_SPACED);
            }

            if (ssmd_datacount > 1 && !stack_is_aligned)
            {
                ShowWarningOnce(WARN_SSMD_STACKSEGMENTS_NOT_EVENLY_SPACED);
            }
#endif

            //*************************************************************************************************************************
            // STATE Stack, allow for only a fixed amount of states                    
            //
            // we store the pointers to the stacks, so we need 8 x 2 x ssmd_datacount of memory per state stacked ( 16kb/1000ssmd/level ) 
            //
            //*************************************************************************************************************************


            // // 3 * 8 * 2 * 1024 + 3 * 2 * 1024 = 54kb 
            // const int max_sync_level = 3;
            // 
            // // 1 index foreach ssmd channel for each level supported
            // byte*** sync_data_indexspace = stackalloc byte**[max_sync_level];
            // byte*** sync_stack_indexspace = stackalloc byte**[max_sync_level];
            // 
            // // 1 byte for each ssmd channel 
            // byte* sync_level = stackalloc byte[ssmd_datacount];
            // byte current_sync_level = 0; 
            //  
            // 


            //*************************************************************************************************************************
            // start the main interpretor loop 
            //*************************************************************************************************************************
            while (code_pointer < package.CodeSize)
            {
                iterations++;
                if (iterations > BlastInterpretor.max_iterations) return (int)BlastError.error_max_iterations;

                byte vector_size = 1;
                blast_operation op = (blast_operation)code[code_pointer];
                code_pointer++;

                switch (op)
                {
                    case blast_operation.ret: return 0;
                    case blast_operation.begin: // jumping in from somewhere
                    case blast_operation.end:
                    case blast_operation.nop: break;

                    //
                    // push value or compound result 
                    //
                    case blast_operation.push:
                        ires = (int)push(ref code_pointer, ref vector_size, temp);
                        MaxStackSizeReachedDuringValidation = math.max(MaxStackSizeReachedDuringValidation, stack_offset);
                        if (ires < 0) return ires;
                        break;

                    //
                    // pushv knows more about what it is pushing up the stack, it can do it without running the compound 
                    //
                    case blast_operation.pushv:
                        ires = (int)pushv(ref code_pointer, ref vector_size, temp);
                        MaxStackSizeReachedDuringValidation = math.max(MaxStackSizeReachedDuringValidation, stack_offset);
                        if (ires < 0) return ires;
                        break;

                    //
                    // push the result from a function directly onto the stack, saving a call to getcompound 
                    //
                    case blast_operation.pushf:
                        ires = (int)pushf(ref code_pointer, ref vector_size, temp);
                        MaxStackSizeReachedDuringValidation = math.max(MaxStackSizeReachedDuringValidation, stack_offset);
                        if (ires < 0) return ires;
                        break;

                    //
                    // push the result from a compound directly onto the stack 
                    //
                    case blast_operation.pushc:
                        ires = (int)pushc(ref code_pointer, ref vector_size, temp);
                        MaxStackSizeReachedDuringValidation = math.max(MaxStackSizeReachedDuringValidation, stack_offset);
                        if (ires < 0) return ires;
                        break;

                    case blast_operation.assigns:
                        if ((ires = (int)assigns(ref code_pointer, temp)) < 0) return ires;
                        break;

                    case blast_operation.assignf:
                        if ((ires = (int)AssignF(ref code_pointer, ref vector_size, temp, false)) < 0) return ires;
                        break;

                    case blast_operation.assignfe:
                        if ((ires = (int)assignfe(ref code_pointer, ref vector_size, temp, false)) < 0) return ires;
                        break;

                    case blast_operation.assignfn:
                        if ((ires = (int)AssignF(ref code_pointer, ref vector_size, temp, true)) < 0) return ires;
                        break;

                    case blast_operation.assignfen:
                        if ((ires = (int)assignfe(ref code_pointer, ref vector_size, temp, true)) < 0) return ires;
                        break;

                    case blast_operation.assignv:
                        if ((ires = (int)assignv(ref code_pointer, ref vector_size, temp)) < 0) return ires;
                        break;

                    // assign result of a compound to some dataindex in the datasegment 
                    case blast_operation.assign:
                        if ((ires = (int)assign(ref code_pointer, ref vector_size, temp)) < 0) return ires;
                        break;

                    // assing something to a cdata 
                    case blast_operation.cdataref:
                        if ((ires = (int)assign_cdata(ref code_pointer, ref vector_size, temp, register)) < 0) return ires;
                        break;

                    // unconditional|fixed jump -> no problem 
                    case blast_operation.jump:
                        code_pointer = code_pointer + code[code_pointer];
                        break;

                    // unconditional|fixed jump -> no sync problem 
                    case blast_operation.jump_back:
                        code_pointer = code_pointer - code[code_pointer];
                        break;

                    // unconditional|fixed long-jump, singed (forward && backward)
                    case blast_operation.long_jump:
                        code_pointer = code_pointer + ((short)((code[code_pointer] << 8) + code[code_pointer + 1])) - 1;
                        break;

                    // constantly terminated short jump
                    case blast_operation.cjz:
                        {
                            // calc endlocation of jump
                            int offset = code[code_pointer];
                            int jump_to = code_pointer + offset - 2;

                            // jump over any opening statement 
                            code_pointer += math.select(1, 2, code[code_pointer + 1] == (byte)blast_operation.begin);

                            // evaluate condition once
                            EvalPushedJumpCondition(ref code_pointer, register, temp, 1);

                            // jump if NOT zero
                            code_pointer = math.select(code_pointer, jump_to, register[0].x == 0);
                        }
                        break;

                    // constantly terminated long jump
                    case blast_operation.cjz_long:
                        {
                            // calc endlocation of 
                            int offset = ((short)(code[code_pointer] << 8) + code[code_pointer + 1]);
                            int jump_to = code_pointer + offset - 3;

                            // jump over any opening statement 
                            code_pointer += math.select(2, 3, code[code_pointer + 2] == (byte)blast_operation.begin);

                            // evaluate condition once
                            EvalPushedJumpCondition(ref code_pointer, register, temp, 1);

                            // jump if NOT zero 
                            code_pointer = math.select(code_pointer, jump_to, register[0].x == 0);
                        }
                        break; 



                    default:
                        {
                            code_pointer--;
                            BlastError res = GetFunctionResult(temp, ref code_pointer, ref vector_size, ssmd_datacount, f4_register, default, false);
                            if (res != BlastError.success)
                            {
                                // if getting here its boom
#if DEVELOPMENT_BUILD || TRACE
                                Debug.LogError($"blast.ssmd: operation {(byte)op} '{op}' not supported in root codepointer = {code_pointer}");
#endif
                                return (int)BlastError.error_unsupported_operation_in_root;
                            }
                            code_pointer++;
                            break;
                        }
                }
            }

            return (int)BlastError.success;
        }


        #endregion
    }
}