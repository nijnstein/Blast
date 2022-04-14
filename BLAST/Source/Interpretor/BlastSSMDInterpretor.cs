//##########################################################################################################
// Copyright © 2022 Rob Lemmens | NijnStein Software <rob.lemmens.s31@gmail.com> All Rights Reserved  ^__^\#
// Unauthorized copying of this file, via any medium is strictly prohibited                           (oo)\#
// Proprietary and confidential                                                                       (__) #
//##########################################################################################################

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
            return new DATAREC()
            {
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
            return new DATAREC()
            {
                data = stack,
                row_size = stack_rowsize,
                index = index,
                is_aligned = stack_is_aligned,
                is_constant = false,
                vectorsize = vectorsize,
                datatype = BlastVariableDataType.Numeric
            };
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

            if (ssmddata == null)
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
                for (int i = 0; i < ssmd_data_count; i++)
                {
                    datastack[i] = ((byte*)ssmddata) + (datastack_stride * i);
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

            if (stack_is_aligned)
            {
                // calc destination offset  
                float* p_stack = &((float*)stack[0])[stack_offset];

                // fast aligned pointer walk 
                float* p_data = (float*)(void*)register;

                int i_stack = stack_rowsize >> 2;
                int i_row = 4;

                for (int i = 0; i < ssmd_datacount; i++) // we could force to always allocate a multiple 4 
                {
                    ((float*)p_stack)[0] = ((float*)p_data)[0];
                    p_stack += i_stack;
                    p_data += i_row;
                }
            }
            else
            {
                // then push the actual data for each element 
                for (int i = 0; i < ssmd_datacount; i++)
                {
                    ((float*)stack[i])[stack_offset] = register[i].x;
                }
            }

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

            // perform a simd copy if all data is aligned
            if (data_is_aligned && stack_is_aligned)
            {
                // NOTE:  after testing, pointer walks seem to speed things up greatly in vs2019/win32 release builds 
                if (stack_is_aligned && data_is_aligned)
                {
                    float* p_stack = &((float*)stack[0])[stack_offset];
                    float* p_data = &((float*)data[0])[index];

                    int i_stack = stack_rowsize >> 2;
                    int i_row = data_rowsize >> 2;

                    for (int i = 0; i < ssmd_datacount; i++)
                    {
                        ((float*)p_stack)[0] = ((float*)p_data)[0];
                        p_stack += i_stack;
                        p_data += i_row;
                    }
                }
                else
                {
                    for (int i = 0; i < ssmd_datacount; i++)
                    {
                        float* p = ((float*)stack[i]);
                        p[stack_offset] = ((float*)(data[i]))[index];
                    }
                }
            }

            // advance 1 element in stack offset 
            stack_offset += 1;
        }

        /// <summary>
        /// push register[n].xy as float2 onto stack
        /// </summary>
        void push_register_f2()
        {
            SetStackMetaData(BlastVariableDataType.Numeric, 2, (byte)stack_offset);

            if (stack_is_aligned)
            {
                // calc destination offset  
                float* p_stack = &((float*)stack[0])[stack_offset];

                // fast aligned pointer walk 
                float* p_data = (float*)(void*)register;

                int i_stack = stack_rowsize >> 2;
                int i_row = 4;

                for (int i = 0; i < ssmd_datacount; i++) // we could force to always allocate a multiple 4 
                {
                    ((float*)p_stack)[0] = ((float*)p_data)[0];
                    ((float*)p_stack)[1] = ((float*)p_data)[1];
                    p_stack += i_stack;
                    p_data += i_row;
                }
            }
            else
            {
                for (int i = 0; i < ssmd_datacount; i++)
                {
                    ((float*)stack[i])[stack_offset] = register[i].x;
                    ((float*)stack[i])[stack_offset + 1] = register[i].y;
                }
            }
            stack_offset += 2;
        }

        /// <summary>
        /// push register[n].xyz as float3 onto stack
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void push_register_f3()
        {
            push_f3(register);
        }
        void push_f3(float4* f4)
        {
            SetStackMetaData(BlastVariableDataType.Numeric, 3, (byte)stack_offset);
            if (stack_is_aligned)
            {
                // calc destination offset  
                float* p_stack = &((float*)stack[0])[stack_offset];

                // fast aligned pointer walk 
                float* p_data = (float*)(void*)register;

                int i_stack = stack_rowsize >> 2;
                int i_row = 4;

                for (int i = 0; i < ssmd_datacount; i++) // we could force to always allocate a multiple 4 
                {
                    ((float*)p_stack)[0] = ((float*)p_data)[0];
                    ((float*)p_stack)[1] = ((float*)p_data)[1];
                    ((float*)p_stack)[2] = ((float*)p_data)[2];
                    p_stack += i_stack;
                    p_data += i_row;
                }
            }
            else
            {
                for (int i = 0; i < ssmd_datacount; i++)
                {
                    ((float*)stack[i])[stack_offset] = register[i].x;
                    ((float*)stack[i])[stack_offset + 1] = register[i].y;
                    ((float*)stack[i])[stack_offset + 2] = register[i].z;
                }
            }
            stack_offset += 3;  // size 3
        }

        /// <summary>
        /// push register[n] as float4 onto stack
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void push_register_f4()
        {
            push_f4(register);
        }

        void push_f4(float4* f4)
        {
            SetStackMetaData(BlastVariableDataType.Numeric, 4, (byte)stack_offset);

            if (stack_is_aligned)
            {
                // calc destination offset  
                float* p_stack = &((float*)stack[0])[stack_offset];

                // fast aligned pointer walk 
                float* p_data = (float*)(void*)register;

                int i_stack = stack_rowsize >> 2;
                int i_row = 4;

                for (int i = 0; i < ssmd_datacount; i++) // we could force to always allocate a multiple 4 
                {
                    p_stack[0] = p_data[0];
                    p_stack[1] = p_data[1];
                    p_stack[2] = p_data[2];
                    p_stack[3] = p_data[3];
                    p_stack += i_stack;
                    p_data += i_row;
                }
            }
            else
            {
                for (int i = 0; i < ssmd_datacount; i++)
                {
                    // when stack is not aligned we need to follow each pointer in the segment array 
                    ((float4*)(void*)&((float*)stack[i])[stack_offset])[0] = register[i];
                }
            }
            stack_offset += 4;  // size 4
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

            if (package.StackSize > 0)
            {
                Debug.Log($"\nSTACK Segment   ({(package.O4 - package.O3)} bytes)");
                for (i = 0; i < (package.O4 - package.O3 - 1) >> 2;)
                {
                    i = DebugDisplayVector(i, ssmdi, stack, metadata);
                }
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
                // -todo this will currently give vectorsize errors
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

        #region Expand fx 

        /// <summary>
        /// 
        /// - only grows codepointer on operations
        /// pop|data data[1] and expand into a vector of size n [2,3,4]
        /// </summary>
        void expand_f1_into_fn(in int expand_into_n, ref int code_pointer, ref byte vector_size, [NoAlias] float4* destination)
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
                                    Debug.LogError($"blast.ssmd.stack.expand_f1_into_fn -> stackdata mismatch, expecting numeric of size 1, found {type} of size {size} at stack offset {stack_offset}");
                                    return;
                                }
                            }
                            else
                            {
                                // indexed vector, index must be smaller then vectorsize 
                                if (indexer > size)
                                {
                                    Debug.LogError($"blast.ssmd.stack.expand_f1_into_fn -> index {indexer} is out of bounds for vector of size {size} at stack offset {stack_offset}");
                                    return;
                                }

                            }
#endif
                            // stack pop 
                            stack_offset = stack_offset - 1;   // element size of value is always 1 if expanding

                            // if indexed, the source data has a different cardinality
                            int offset = stack_offset + math.select(0, indexer, indexer >= 0);

                            expand_indexed_into_n(expand_into_n, destination, substract, c, offset, stack, stack_is_aligned, stack_rowsize);

                            // output vectorsize equals what we expanded into 
                            vector_size = (byte)expand_into_n;
                            return;
                        }


                    case blast_operation.constant_f1:
                        {
                            // handle operation with inlined constant data 
                            float constant = default;
                            byte* p = (byte*)(void*)&constant;
                            {
                                p[0] = code[code_pointer + 1];
                                p[1] = code[code_pointer + 2];
                                p[2] = code[code_pointer + 3];
                                p[3] = code[code_pointer + 4];
                            }
                            code_pointer += 4; // function ends on this and should leave pointer at last processed token

                            constant = math.select(constant, -constant, substract);
                            expand_constant_into_n(expand_into_n, destination, constant);
                        }
                        return;

                    case blast_operation.constant_f1_h:
                        {
                            // handle operation with inlined constant data 
                            float constant = default;
                            byte* p = (byte*)(void*)&constant;
                            {
                                p[0] = 0;
                                p[1] = 0;
                                p[2] = code[code_pointer + 1];
                                p[3] = code[code_pointer + 2];
                            }
                            code_pointer += 2;  // function ends on this and should leave pointer at last processed token

                            constant = math.select(constant, -constant, substract);
                            expand_constant_into_n(expand_into_n, destination, constant);
                        }
                        return;

                    case blast_operation.constant_short_ref:
                        {
                            code_pointer += 1;
                            int c_index = code_pointer - code[code_pointer];
                            //code_pointer += 1;  // function ends on this and should leave pointer at last processed token

                            float constant = default;
                            byte* p = (byte*)(void*)&constant;

                            if (code[c_index] == (byte)blast_operation.constant_f1)
                            {
                                p[0] = code[c_index + 1];
                                p[1] = code[c_index + 2];
                                p[2] = code[c_index + 3];
                                p[3] = code[c_index + 4];
                            }
                            else
                            {
#if DEVELOPMENT_BUILD || TRACE
                                if (code[c_index] != (byte)blast_operation.constant_f1_h)
                                {
                                    Debug.LogError($"blast.ssmd.expand_f1_into_fn: constant reference error, short constant reference at {code_pointer - 1} points at invalid operation {code[c_index]} at {c_index}");
                                }
#endif

                                // 16 bit encoding
                                p[0] = 0;
                                p[1] = 0;
                                p[2] = code[c_index + 1];
                                p[3] = code[c_index + 2];
                            }

                            constant = math.select(constant, -constant, substract);
                            expand_constant_into_n(expand_into_n, destination, constant);
                        }
                        return;

                    case blast_operation.constant_long_ref:
                        {
                            code_pointer += 1;
                            int c_index = code_pointer - ((code[code_pointer] << 8) + code[code_pointer + 1]) + 1;
                            code_pointer += 1;      // += 2; function ends on this and should leave pointer at last processed token

                            float constant = default;
                            byte* p = (byte*)(void*)&constant;

                            if (code[c_index] == (byte)blast_operation.constant_f1)
                            {
                                p[0] = code[c_index + 1];
                                p[1] = code[c_index + 2];
                                p[2] = code[c_index + 3];
                                p[3] = code[c_index + 4];
                            }
                            else
                            {
#if DEVELOPMENT_BUILD || TRACE
                                if (code[c_index] != (byte)blast_operation.constant_f1_h)
                                {
                                    Debug.LogError($"blast.ssmd.expand_f1_into_fn: constant reference error, short constant reference at {code_pointer - 1} points at invalid operation {code[c_index]} at {c_index}");
                                }
#endif
                                // 16 bit encoding
                                p[0] = 0;
                                p[1] = 0;
                                p[2] = code[c_index + 1];
                                p[3] = code[c_index + 2];
                            }

                            constant = math.select(constant, -constant, substract);
                            expand_constant_into_n(expand_into_n, destination, constant);
                        }
                        return;



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
                            expand_constant_into_n(expand_into_n, destination, constant);
                        }
                        return;


                    default:
                    case blast_operation.id:
                        {
                            byte offset = (byte)(c - BlastInterpretor.opt_id);

#if DEVELOPMENT_BUILD || TRACE
                            // validate its a valid id in debug mode 
                            if (c < (byte)blast_operation.id || c == 255)
                            {
                                Debug.LogError($"blast.ssmd.stack.expand_f1_into_fn -> operation {c} not supported at codepointer {code_pointer}");
                                return;
                            }


                            BlastVariableDataType type = BlastInterpretor.GetMetaDataType(metadata, offset);
                            int size = BlastInterpretor.GetMetaDataSize(metadata, offset);

                            if (indexer >= 0)
                            {
                                if (size < indexer)
                                {
                                    Debug.LogError($"blast.ssmd.stack.expand_f1_into_fn -> indexer beyond vectorsize at codepointer {code_pointer}");
                                    return;
                                }
                            }
                            else
                            {
                                if (size != 1 || type != BlastVariableDataType.Numeric)
                                {
                                    Debug.LogError($"blast.ssmd.stack.expand_f1_into_fn -> data mismatch, expecting numeric of size 1, found {type} of size {size} at data offset {c - BlastInterpretor.opt_id}");
                                    return;
                                }
                            }
#endif

                            offset = (byte)math.select(offset, offset + indexer, indexer >= 0);

                            expand_indexed_into_n(expand_into_n, destination, substract, c, offset, data, data_is_aligned, data_rowsize);
                        }
                        return;
                }
            }
        }


        void expand_indexed_into_n(int expand_into_n, [NoAlias] float4* destination, bool substract, byte c, int offset, void** data, bool is_aligned, int stride)
        {
            switch (expand_into_n)
            {
                case 2:
                    if (!substract)
                    {
                        simd.move_indexed_data_as_f1_to_f2(destination, data, data_rowsize, data_is_aligned, offset, ssmd_datacount);
                    }
                    else
                    {
                        simd.move_indexed_data_as_f1_to_f2_negated(destination, data, data_rowsize, data_is_aligned, offset, ssmd_datacount);
                    }
                    break;

                case 3:
                    if (!substract)
                    {
                        simd.move_indexed_data_as_f1_to_f3(destination, data, data_rowsize, data_is_aligned, offset, ssmd_datacount);
                    }
                    else
                    {
                        simd.move_indexed_data_as_f1_to_f3_negated(destination, data, data_rowsize, data_is_aligned, offset, ssmd_datacount);
                    }
                    break;

                case 4:
                    if (!substract)
                    {
                        simd.move_indexed_data_as_f1_to_f4(destination, data, data_rowsize, data_is_aligned, offset, ssmd_datacount);
                    }
                    else
                    {
                        simd.move_indexed_data_as_f1_to_f4_negated(destination, data, data_rowsize, data_is_aligned, offset, ssmd_datacount);
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
        void expand_constant_into_n(int expand_into_n, [NoAlias] float4* destination, float constant)
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

            input_ssmd_datacount = math.select(input_ssmd_datacount, ssmd_datacount, input_ssmd_datacount == -1); 

 
            while (true)
            {
                byte c = code[code_pointer];

                switch ((blast_operation)c)
                {
                    case blast_operation.substract: is_negated = true; code_pointer++; continue;
                    case blast_operation.index_x: indexed = 0; code_pointer++; continue;
                    case blast_operation.index_y: indexed = 1; code_pointer++; continue;
                    case blast_operation.index_z: indexed = 2; code_pointer++; continue;
                    case blast_operation.index_w: indexed = 3; code_pointer++; continue;

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
                                switch(vector_size)
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
                            bf[0] = code[code_pointer + 1];
                            bf[1] = code[code_pointer + 2];
                            bf[2] = code[code_pointer + 3];
                            bf[3] = code[code_pointer + 4];

                            code_pointer += 5;

                            constant = ((float*)(void*)bf)[0];
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
                            bf[0] = 0;
                            bf[1] = 0;
                            bf[2] = code[code_pointer + 1];
                            bf[3] = code[code_pointer + 2];

                            code_pointer += 3;

                            constant = ((float*)(void*)bf)[0];
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
                        {
                            code_pointer += 1;
                            int c_index = code_pointer - code[code_pointer];
                            code_pointer += 1;

                            if (code[c_index] == (byte)blast_operation.constant_f1)
                            {
                                bf[0] = code[c_index + 1];
                                bf[1] = code[c_index + 2];
                                bf[2] = code[c_index + 3];
                                bf[3] = code[c_index + 4];
                            }
                            else
                            {
#if DEVELOPMENT_BUILD || TRACE
                                if (code[c_index] != (byte)blast_operation.constant_f1_h)
                                {
                                    Debug.LogError($"blast.ssmd.pop_fx_int_ref<t>: constant reference error, short constant reference at {code_pointer - 1} points at invalid operation {code[c_index]} at {c_index}");
                                }
#endif

                                // 16 bit encoding
                                bf[0] = 0;
                                bf[1] = 0;
                                bf[2] = code[c_index + 1];
                                bf[3] = code[c_index + 2];
                            }

                            constant = ((float*)(void*)bf)[0];
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

                    case blast_operation.constant_long_ref:
                        {
                            int offset = (code[code_pointer + 1] << 8) + code[code_pointer + 2];
                            int c_index = code_pointer - offset + 1;
                            code_pointer += 3;

                            if (code[c_index] == (byte)blast_operation.constant_f1)
                            {
                                bf[0] = code[c_index + 1];
                                bf[1] = code[c_index + 2];
                                bf[2] = code[c_index + 3];
                                bf[3] = code[c_index + 4];
                            }
                            else
                            {
#if DEVELOPMENT_BUILD || TRACE
                                if (code[c_index] != (byte)blast_operation.constant_f1_h)
                                {
                                    Debug.LogError($"blast.ssmd.pop_fx_int_ref<t>: constant reference error, long constant reference at {code_pointer - 1} points at invalid operation {code[c_index]} at {c_index}");
                                }
#endif
                                // 16 bit encoding
                                bf[0] = 0;
                                bf[1] = 0;
                                bf[2] = code[c_index + 1];
                                bf[3] = code[c_index + 2];
                            }

                            constant = ((float*)(void*)bf)[0];
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
                    if (is_aligned)
                    {
                        float* dest = &((float*)data[0])[destination_offset];
                        int i_dest = stride >> 2;

                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            dest[0] = constant;
                            dest += i_dest;
                        }
                    }
                    else
                    {
                        // unaligned 
                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            ((float*)data[i])[destination_offset] = constant;
                        }
                    }
                    return;

                case 2:
                    if (is_aligned)
                    {
                        float* dest = &((float*)data[0])[destination_offset];
                        int i_dest = stride >> 2;

                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            dest[0] = constant;
                            dest[1] = constant;
                            dest += i_dest;
                        }
                    }
                    else
                    {
                        // unaligned 
                        float2 constant_f2 = constant;
                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            ((float2*)(void*)&((float*)data[i])[destination_offset])[0] = constant_f2;
                        }
                    }
                    return;

                case 3:
                    if (is_aligned)
                    {
                        float* dest = &((float*)data[0])[destination_offset];
                        int i_dest = stride >> 2;

                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            dest[0] = constant;
                            dest[1] = constant;
                            dest[2] = constant;
                            dest += i_dest;
                        }
                    }
                    else
                    {
                        float3 constant_f3 = constant;
                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            ((float3*)(void*)&((float*)data[i])[destination_offset])[0] = constant_f3;
                        }
                    }
                    return;

                case 4:
                    if (is_aligned)
                    {
                        float* dest = &((float*)data[0])[destination_offset];
                        int i_dest = stride >> 2;

                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            dest[0] = constant;
                            dest[1] = constant;
                            dest[2] = constant;
                            dest += i_dest;
                        }
                    }
                    else
                    {
                        float4 constant_f4 = constant;
                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            ((float4*)(void*)&((float*)data[i])[destination_offset])[0] = constant_f4;
                        }
                    }
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
                        bf[0] = code[code_pointer + 0];
                        bf[1] = code[code_pointer + 1];
                        bf[2] = code[code_pointer + 2];
                        bf[3] = code[code_pointer + 3];

                        code_pointer += 4;

                        constant = ((float*)(void*)bf)[0];
                        pop_fx_with_op_into_fx_constant_handler<Tin, Tout>(buffer, output, op, constant, datatype);
                    }
                    break;

                case blast_operation.constant_f1_h:
                    {
                        bf[0] = 0;
                        bf[1] = 0;
                        bf[2] = code[code_pointer + 0];
                        bf[3] = code[code_pointer + 1];

                        code_pointer += 2;

                        constant = ((float*)(void*)bf)[0];
                        pop_fx_with_op_into_fx_constant_handler<Tin, Tout>(buffer, output, op, constant, datatype);
                    }
                    break;

                case blast_operation.constant_short_ref:
                    {
                        int c_index = code_pointer - code[code_pointer]; // + 1
                        code_pointer += 1;

                        if (code[c_index] == (byte)blast_operation.constant_f1)
                        {
                            bf[0] = code[c_index + 1];
                            bf[1] = code[c_index + 2];
                            bf[2] = code[c_index + 3];
                            bf[3] = code[c_index + 4];
                        }
                        else
                        {
#if DEVELOPMENT_BUILD || TRACE
                            if (code[c_index] != (byte)blast_operation.constant_f1_h)
                            {
                                Debug.LogError($"blast.ssmd.pop_fx_with_op_into_fx_ref: constant reference error, short constant reference at {code_pointer - 1} points at invalid operation {code[c_index]} at {c_index}");
                            }
#endif
                            // 16 bit encoding
                            bf[0] = 0;
                            bf[1] = 0;
                            bf[2] = code[c_index + 1];
                            bf[3] = code[c_index + 2];
                        }

                        constant = ((float*)(void*)bf)[0];
                        pop_fx_with_op_into_fx_constant_handler<Tin, Tout>(buffer, output, op, constant, datatype);
                    }
                    break;

                case blast_operation.constant_long_ref:
                    {
                        int c_index = code_pointer - ((code[code_pointer]) << 8 + code[code_pointer + 1]) + 1;
                        code_pointer += 2;

                        if (code[c_index] == (byte)blast_operation.constant_f1)
                        {
                            bf[0] = code[c_index + 1];
                            bf[1] = code[c_index + 2];
                            bf[2] = code[c_index + 3];
                            bf[3] = code[c_index + 4];
                        }
                        else
                        {
#if DEVELOPMENT_BUILD || TRACE
                            if (code[c_index] != (byte)blast_operation.constant_f1_h)
                            {
                                Debug.LogError($"blast.ssmd.pop_fx_with_op_into_fx_ref: constant reference error, long constant reference at {code_pointer - 1} points at invalid operation {code[c_index]} at {c_index}");
                            }
#endif
                            // 16 bit encoding
                            bf[0] = 0;
                            bf[1] = 0;
                            bf[2] = code[c_index + 1];
                            bf[3] = code[c_index + 2];
                        }

                        constant = ((float*)(void*)bf)[0];
                        pop_fx_with_op_into_fx_constant_handler<Tin, Tout>(buffer, output, op, constant, datatype);
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
                            else simd.move_f1_array_to_indexed_data_as_f1(stack, stack_rowsize, stack_is_aligned, stack_offset + 0, t1, ssmd_datacount);
                            if (c2) simd.move_f1_constant_to_indexed_data_as_f1(stack, stack_rowsize, stack_is_aligned, stack_offset + 1, t2[0], ssmd_datacount);
                            else simd.move_f1_array_to_indexed_data_as_f1(stack, stack_rowsize, stack_is_aligned, stack_offset + 1, t2, ssmd_datacount);
                            if (c3) simd.move_f1_constant_to_indexed_data_as_f1(stack, stack_rowsize, stack_is_aligned, stack_offset + 2, t3[0], ssmd_datacount);
                            else simd.move_f1_array_to_indexed_data_as_f1(stack, stack_rowsize, stack_is_aligned, stack_offset + 2, t3, ssmd_datacount);
                            if (c4) simd.move_f1_constant_to_indexed_data_as_f1(stack, stack_rowsize, stack_is_aligned, stack_offset + 3, t4[0], ssmd_datacount);
                            else simd.move_f1_array_to_indexed_data_as_f1(stack, stack_rowsize, stack_is_aligned, stack_offset + 3, t4, ssmd_datacount);
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



        #region Operations Handlers 

        /// <summary>                    
        /// perform operation: a.x = a.x [op] [sub|not] b.x
        /// </summary>
        void handle_op_f1_f1(in blast_operation op, [NoAlias] float4* a, [NoAlias] float4* b, ref byte vector_size)
        {
            vector_size = 1;
            switch (op)
            {
                default:
#if DEVELOPMENT_BUILD || TRACE
                    Debug.LogError($"blast.ssmd.handle_op_fx_fy: operation {op} not handled, vectorsize = {vector_size}, a[0] = {a[0]}, b[0] = {b[0]}");
#endif
                    return;
                case blast_operation.nop: simd.move_f1_array_into_f4_array_as_f1(a, b, ssmd_datacount); break;
                case blast_operation.not: for (int i = 0; i < ssmd_datacount; i++) a[i].x = math.select(1f, 0f, b[i].x != 0); break;

                case blast_operation.add: simd.add_array_f1f1(a, b, ssmd_datacount); return;
                case blast_operation.substract: simd.sub_array_f1f1(a, b, ssmd_datacount); return;
                case blast_operation.multiply: simd.mul_array_f1f1(a, b, ssmd_datacount); return;
                case blast_operation.divide: simd.div_array_f1f1(a, b, ssmd_datacount); return;

                case blast_operation.and: for (int i = 0; i < ssmd_datacount; i++) a[i].x = math.select(0, 1, a[i].x != 0 && b[i].x != 0); return;
                case blast_operation.or: for (int i = 0; i < ssmd_datacount; i++) a[i].x = math.select(0, 1, a[i].x != 0 || b[i].x != 0); return;
                case blast_operation.xor: for (int i = 0; i < ssmd_datacount; i++) a[i].x = math.select(0, 1, a[i].x != 0 ^ b[i].x != 0); return;

                case blast_operation.greater: for (int i = 0; i < ssmd_datacount; i++) a[i].x = math.select(0f, 1f, a[i].x > b[i].x); return;
                case blast_operation.smaller: for (int i = 0; i < ssmd_datacount; i++) a[i].x = math.select(0f, 1f, a[i].x < b[i].x); return;
                case blast_operation.smaller_equals: for (int i = 0; i < ssmd_datacount; i++) a[i].x = math.select(0f, 1f, a[i].x <= b[i].x); return;
                case blast_operation.greater_equals: for (int i = 0; i < ssmd_datacount; i++) a[i].x = math.select(0f, 1f, a[i].x >= b[i].x); return;
                case blast_operation.equals: for (int i = 0; i < ssmd_datacount; i++) a[i].x = math.select(0f, 1f, a[i].x == b[i].x); return;
                case blast_operation.not_equals: for (int i = 0; i < ssmd_datacount; i++) a[i].x = math.select(0f, 1f, a[i].x != b[i].x); return;
            }
        }



        /// <summary>
        /// perform:  a.x = a.x [op] constant
        /// </summary>
        void handle_op_f1_f1(in blast_operation op, [NoAlias] float4* a, float constant, ref byte vector_size)
        {
            vector_size = 1;
            switch (op)
            {
                default:
#if DEVELOPMENT_BUILD || TRACE
                    Debug.LogError($"blast.ssmd.handle_op_fx_fy: operation {op} not handled, vectorsize = {vector_size}, a[0] = {a[0]}, constant = {constant}");
#endif
                    return;
                case blast_operation.nop: simd.move_f1_constant_into_f4_array_as_f1(a, constant, ssmd_datacount); break;
                case blast_operation.not: simd.move_f1_constant_into_f4_array_as_f1(a, math.select(0f, 1f, constant == 0), ssmd_datacount); break;

                case blast_operation.add: simd.add_array_f1f1(a, constant, ssmd_datacount); return;
                case blast_operation.substract: simd.sub_array_f1f1(a, constant, ssmd_datacount); return;
                case blast_operation.multiply: simd.mul_array_f1f1(a, constant, ssmd_datacount); return;
                case blast_operation.divide: simd.div_array_f1f1(a, constant, ssmd_datacount); return;

                case blast_operation.and: { bool bconstant = constant != 0; for (int i = 0; i < ssmd_datacount; i++) a[i].x = math.select(0, 1, a[i].x != 0 && bconstant); return; }
                case blast_operation.or: { bool bconstant = constant != 0; for (int i = 0; i < ssmd_datacount; i++) a[i].x = math.select(0, 1, a[i].x != 0 || bconstant); return; }
                case blast_operation.xor: { bool bconstant = constant != 0; for (int i = 0; i < ssmd_datacount; i++) a[i].x = math.select(0, 1, a[i].x != 0 ^ bconstant); return; }

                case blast_operation.greater: for (int i = 0; i < ssmd_datacount; i++) a[i].x = math.select(0f, 1f, a[i].x > constant); return;
                case blast_operation.smaller: for (int i = 0; i < ssmd_datacount; i++) a[i].x = math.select(0f, 1f, a[i].x < constant); return;
                case blast_operation.smaller_equals: for (int i = 0; i < ssmd_datacount; i++) a[i].x = math.select(0f, 1f, a[i].x <= constant); return;
                case blast_operation.greater_equals: for (int i = 0; i < ssmd_datacount; i++) a[i].x = math.select(0f, 1f, a[i].x >= constant); return;
                case blast_operation.equals: for (int i = 0; i < ssmd_datacount; i++) a[i].x = math.select(0f, 1f, a[i].x == constant); return;
                case blast_operation.not_equals: for (int i = 0; i < ssmd_datacount; i++) a[i].x = math.select(0f, 1f, a[i].x != constant); return;
            }
        }

        /// <summary>
        /// perform:  target.x = a.x [op] constant
        /// </summary>
        void handle_op_f1_f1(in blast_operation op, [NoAlias] float4* a, float constant, ref byte vector_size, in DATAREC target)
        {
            if (!target.is_set)
            {
#if DEVELOPMENT_BUILD || TRACE
                Debug.LogError($"blast.ssmd.handle_op_fx_fy [constant b]: target not set");
#endif
                return;
            }
                               
            vector_size = 1;
            switch (op)
            {
                default:
#if DEVELOPMENT_BUILD || TRACE
                    Debug.LogError($"blast.ssmd.handle_op_fx_fy: operation {op} not handled, vectorsize = {vector_size}, a[0] = {a[0]}, constant = {constant}");
#endif
                    break; 

                case blast_operation.nop:               simd.move_f1_constant_to_indexed_data_as_f1(target.data, target.row_size, target.is_aligned, target.index, constant, ssmd_datacount); break;
                case blast_operation.not:               simd.move_f1_constant_to_indexed_data_as_f1(target.data, target.row_size, target.is_aligned, target.index, math.select(0f, 1f, constant == 0), ssmd_datacount); break;

                case blast_operation.add:               simd.add_array_f1f1(a, constant, target, ssmd_datacount); return;
                case blast_operation.substract:         simd.sub_array_f1f1(a, constant, target, ssmd_datacount); return;
                case blast_operation.multiply:          simd.mul_array_f1f1(a, constant, target, ssmd_datacount); return;
                case blast_operation.divide:            simd.div_array_f1f1(a, constant, target, ssmd_datacount); return;

                case blast_operation.and:               { bool bconstant = constant != 0; for (int i = 0; i < ssmd_datacount; i++) ((float**)target.data)[i][target.index] = math.select(0, 1, a[i].x != 0 && bconstant); } break;
                case blast_operation.or:                { bool bconstant = constant != 0; for (int i = 0; i < ssmd_datacount; i++) ((float**)target.data)[i][target.index] = math.select(0, 1, a[i].x != 0 || bconstant);  } break; 
                case blast_operation.xor:               { bool bconstant = constant != 0; for (int i = 0; i < ssmd_datacount; i++) ((float**)target.data)[i][target.index] = math.select(0, 1, a[i].x != 0 ^ bconstant); } break; 

                case blast_operation.greater:           for (int i = 0; i < ssmd_datacount; i++) ((float**)target.data)[i][target.index] = math.select(0f, 1f, a[i].x > constant); break;
                case blast_operation.smaller:           for (int i = 0; i < ssmd_datacount; i++) ((float**)target.data)[i][target.index] = math.select(0f, 1f, a[i].x < constant); break;
                case blast_operation.smaller_equals:    for (int i = 0; i < ssmd_datacount; i++) ((float**)target.data)[i][target.index] = math.select(0f, 1f, a[i].x <= constant); break;
                case blast_operation.greater_equals:    for (int i = 0; i < ssmd_datacount; i++) ((float**)target.data)[i][target.index] = math.select(0f, 1f, a[i].x >= constant); break;
                case blast_operation.equals:            for (int i = 0; i < ssmd_datacount; i++) ((float**)target.data)[i][target.index] = math.select(0f, 1f, a[i].x == constant); break;
                case blast_operation.not_equals:        for (int i = 0; i < ssmd_datacount; i++) ((float**)target.data)[i][target.index] = math.select(0f, 1f, a[i].x != constant); break;
            }
        }

        /// <summary>
        /// perform:  target.xy = a.x [op] [sub|not] b.xy
        /// </summary>
        void handle_op_f1_f2(in blast_operation op, [NoAlias] float4* a, [NoAlias] float4* b, ref byte vector_size)
        {
            vector_size = 2;
            switch (op)
            {
                default:
#if DEVELOPMENT_BUILD || TRACE
                    Debug.LogError($"blast.ssmd.handle_op_fx_fy: operation {op} not handled, vectorsize = {vector_size}, a[0] = {a[0]}, b[0] = {b[0]}");
#endif
                    return;
                case blast_operation.nop: simd.move_f2_array_into_f4_array_as_f2(a, b, ssmd_datacount); break; // for (int i = 0; i < ssmd_datacount; i++) a[i].xy = b[i].xy; break;
                case blast_operation.not: for (int i = 0; i < ssmd_datacount; i++) a[i].xy = math.select((float2)1, (float2)0, b[i].xy != 0); break;

                case blast_operation.add: simd.add_array_f1f2(a, b, ssmd_datacount); return;
                case blast_operation.substract: simd.sub_array_f1f2(a, b, ssmd_datacount); return;
                case blast_operation.multiply: simd.mul_array_f1f2(a, b, ssmd_datacount); return;
                case blast_operation.divide: simd.div_array_f1f2(a, b, ssmd_datacount); return;

                case blast_operation.and: for (int i = 0; i < ssmd_datacount; i++) a[i].xy = math.select(0, 1, a[i].x != 0 && math.any(b[i].xy)); return;
                case blast_operation.or: for (int i = 0; i < ssmd_datacount; i++) a[i].xy = math.select(0, 1, a[i].x != 0 || math.any(b[i].xy)); return;
                case blast_operation.xor: for (int i = 0; i < ssmd_datacount; i++) a[i].xy = math.select(0, 1, a[i].x != 0 ^ math.any(b[i].xy)); return;

                case blast_operation.greater: for (int i = 0; i < ssmd_datacount; i++) a[i].xy = math.select(0f, 1f, a[i].x > b[i].xy); return;
                case blast_operation.smaller: for (int i = 0; i < ssmd_datacount; i++) a[i].xy = math.select(0f, 1f, a[i].x < b[i].xy); return;
                case blast_operation.smaller_equals: for (int i = 0; i < ssmd_datacount; i++) a[i].xy = math.select(0f, 1f, a[i].x <= b[i].xy); return;
                case blast_operation.greater_equals: for (int i = 0; i < ssmd_datacount; i++) a[i].xy = math.select(0f, 1f, a[i].x >= b[i].xy); return;
                case blast_operation.equals: for (int i = 0; i < ssmd_datacount; i++) a[i].xy = math.select(0f, 1f, a[i].x == b[i].xy); return;
                case blast_operation.not_equals: for (int i = 0; i < ssmd_datacount; i++) a[i].xy = math.select(0f, 1f, a[i].x != b[i].xy); return;
            }
        }

        /// <summary>
        /// perform:  target.x = a.x [op] [sub|not] b.x
        /// </summary>
        void handle_op_f1_f1(in blast_operation op, [NoAlias] float4* a, [NoAlias] float4* b, ref byte vector_size, in bool minus, in bool not, in DATAREC target)
        {
            if(!target.is_set)
            {
#if DEVELOPMENT_BUILD || TRACE
                Debug.LogError($"blast.ssmd.handle_op_fx_fy: target not set");
#endif
                return; 
            }

            vector_size = 1;
            switch (op)
            {
                default:
#if DEVELOPMENT_BUILD || TRACE
                    Debug.LogError($"blast.ssmd.handle_op_fx_fy: operation {op} not handled, vectorsize = {vector_size}, a[0] = {a[0]}, b[0] = {b[0]} target index = {target.index}");
#endif
                    return;

                case blast_operation.nop:
                    if(minus)   simd.move_f4_array_to_indexed_data_as_f1            (target.data, target.row_size, target.is_aligned, target.index, b, ssmd_datacount); 
                    else        simd.move_f4_array_to_indexed_data_as_f1_negated    (target.data, target.row_size, target.is_aligned, target.index, b, ssmd_datacount);
                    break;

                case blast_operation.add: 
                    if(minus)   simd.sub_array_f1f1(a, b, target, ssmd_datacount); 
                    else        simd.add_array_f1f1(a, b, target, ssmd_datacount); 
                    break;

                case blast_operation.multiply:
                    if (minus)  simd.mul_array_f1f1_negated(a, b, target, ssmd_datacount);
                    else        simd.mul_array_f1f1(a, b, target, ssmd_datacount);
                    break; 

                case blast_operation.divide: 
                    if (minus)  simd.div_array_f1f1_negated(a, b, target, ssmd_datacount);
                    else        simd.div_array_f1f1(a, b, target, ssmd_datacount);
                    break;

                case blast_operation.substract:
                    if (minus)  simd.add_array_f1f1(a, b, target, ssmd_datacount);
                    else        simd.sub_array_f1f1(a, b, target, ssmd_datacount);
                    break;

                case blast_operation.not:
                    if (!not)
                    {
                        // only set if not a double not 
                        for (int i = 0; i < ssmd_datacount; i++)  ((float**)target.data)[i][target.index] = math.select(1f, 0f, b[i].x != 0);
                    }
                    break;

                case blast_operation.and:
                    if (not)
                    {
                        // the not applies to b[]
                        for (int i = 0; i < ssmd_datacount; i++) ((float**)target.data)[i][target.index] = math.select(0, 1, a[i].x != 0 && b[i].x == 0); 
                    }
                    else
                    {
                        for (int i = 0; i < ssmd_datacount; i++) ((float**)target.data)[i][target.index] = math.select(0, 1, a[i].x != 0 && b[i].x != 0);
                    }
                    break; 

                case blast_operation.or:
                    if (not)
                    {
                        for (int i = 0; i < ssmd_datacount; i++) ((float**)target.data)[i][target.index] = math.select(0, 1, a[i].x != 0 || b[i].x == 0); 
                    }
                    else
                    {
                        for (int i = 0; i < ssmd_datacount; i++) ((float**)target.data)[i][target.index] = math.select(0, 1, a[i].x != 0 || b[i].x != 0); 
                    }
                    break;

                case blast_operation.xor:
                    if (not)
                    {
                        for (int i = 0; i < ssmd_datacount; i++) ((float**)target.data)[i][target.index] = math.select(0, 1, a[i].x != 0 ^ b[i].x == 0); 
                    }
                    else
                    {
                        for (int i = 0; i < ssmd_datacount; i++) ((float**)target.data)[i][target.index] = math.select(0, 1, a[i].x != 0 ^ b[i].x != 0);
                    }
                    break; 

                case blast_operation.greater:           
                    if(minus)
                    {
                        for (int i = 0; i < ssmd_datacount; i++) ((float**)target.data)[i][target.index] = math.select(0f, 1f, a[i].x > -b[i].x);
                    }
                    else
                    {
                        for (int i = 0; i < ssmd_datacount; i++) ((float**)target.data)[i][target.index] = math.select(0f, 1f, a[i].x > b[i].x);
                    }
                    break; 

                case blast_operation.smaller:           
                    if (minus)
                    {
                        for (int i = 0; i < ssmd_datacount; i++) ((float**)target.data)[i][target.index] = math.select(0f, 1f, a[i].x < -b[i].x); 
                    }
                    else
                    {
                        for (int i = 0; i < ssmd_datacount; i++) ((float**)target.data)[i][target.index] = math.select(0f, 1f, a[i].x < b[i].x);
                    }
                    break;

                case blast_operation.smaller_equals:    
                    if (minus)
                    {
                        for (int i = 0; i < ssmd_datacount; i++) ((float**)target.data)[i][target.index] = math.select(0f, 1f, a[i].x <= -b[i].x);
                    }
                    else
                    {
                        for (int i = 0; i < ssmd_datacount; i++) ((float**)target.data)[i][target.index] = math.select(0f, 1f, a[i].x <= b[i].x);
                    }
                    break;

                case blast_operation.greater_equals:    
                    if (minus)
                    {
                        for (int i = 0; i < ssmd_datacount; i++) ((float**)target.data)[i][target.index] = math.select(0f, 1f, a[i].x >= -b[i].x);
                    }
                    else
                    {
                        for (int i = 0; i < ssmd_datacount; i++) ((float**)target.data)[i][target.index] = math.select(0f, 1f, a[i].x >= b[i].x);
                    }
                    break;

                case blast_operation.equals:            
                    if (not)
                    {
                        for (int i = 0; i < ssmd_datacount; i++) ((float**)target.data)[i][target.index] = math.select(0f, 1f, a[i].x == math.select(0, 1, b[i].x == 0)); 
                    }
                    if (minus)
                    {
                        for (int i = 0; i < ssmd_datacount; i++) ((float**)target.data)[i][target.index] = math.select(0f, 1f, a[i].x == -b[i].x);
                    }
                    else
                    {
                        for (int i = 0; i < ssmd_datacount; i++) ((float**)target.data)[i][target.index] = math.select(0f, 1f, a[i].x == b[i].x); 
                    }
                    break;

                case blast_operation.not_equals:
                    if (not)
                    {
                        for (int i = 0; i < ssmd_datacount; i++) ((float**)target.data)[i][target.index] = math.select(0f, 1f, a[i].x != math.select(0, 1, b[i].x == 0));
                    }
                    if (minus)
                    {
                        for (int i = 0; i < ssmd_datacount; i++) ((float**)target.data)[i][target.index] = math.select(0f, 1f, a[i].x != -b[i].x);
                    }
                    else
                    {
                        for (int i = 0; i < ssmd_datacount; i++) ((float**)target.data)[i][target.index] = math.select(0f, 1f, a[i].x != b[i].x);
                    }
                    break;
            }
        }

        /// <summary>
        /// perform:  target.xyz = a.x [op] [minus|not] b.xyz
        /// </summary>
        void handle_op_f1_f2(in blast_operation op, [NoAlias] float4* a, [NoAlias] float4* b, ref byte vector_size, in bool minus, in bool not, in DATAREC target)
        {
            if (!target.is_set)
            {
#if DEVELOPMENT_BUILD || TRACE
                Debug.LogError($"blast.ssmd.handle_op_f1_f2: target not set");
#endif
                return;
            }

            vector_size = 2;
            switch (op)
            {
                default:
#if DEVELOPMENT_BUILD || TRACE
                    Debug.LogError($"blast.ssmd.handle_op_f1_f2: operation {op} not handled, vectorsize = {vector_size}, a[0] = {a[0]}, b[0] = {b[0]}");
#endif
                    return;
                case blast_operation.nop:
                    if(minus)   simd.move_f4_array_to_indexed_data_as_f2            (target.data, target.row_size, target.is_aligned, target.index, b, ssmd_datacount); 
                    else        simd.move_f4_array_to_indexed_data_as_f2_negated    (target.data, target.row_size, target.is_aligned, target.index, b, ssmd_datacount);
                    break;

                case blast_operation.add: 
                    if(minus)   simd.sub_array_f1f2(a, b, target, ssmd_datacount); 
                    else        simd.add_array_f1f2(a, b, target, ssmd_datacount); 
                    break;

                case blast_operation.multiply:
                    if (minus)  simd.mul_array_f1f2_negated(a, b, target, ssmd_datacount);
                    else        simd.mul_array_f1f2(a, b, target, ssmd_datacount);
                    break; 

                case blast_operation.divide: 
                    if (minus)  simd.div_array_f1f2_negated(a, b, target, ssmd_datacount);
                    else        simd.div_array_f1f2(a, b, target, ssmd_datacount);
                    break;

                case blast_operation.substract:
                    if (minus)  simd.add_array_f1f2(a, b, target, ssmd_datacount);
                    else        simd.sub_array_f1f2(a, b, target, ssmd_datacount);
                    break;

                case blast_operation.not:
                    if (!not)
                    {
                        // only set if not a double not 
                        for (int i = 0; i < ssmd_datacount; i++) ((float2*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(1f, 0f, b[i].xy != 0);
                    }
                    break;

                case blast_operation.and:
                    if (not)
                    {
                        // the not applies to b[]
                        for (int i = 0; i < ssmd_datacount; i++) ((float2*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].x != 0 && b[i].x == 0 && b[i].y == 0);
                    }
                    else
                    {
                        for (int i = 0; i < ssmd_datacount; i++) ((float2*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].x != 0 && b[i].x != 0 && b[i].y != 0);
                    }
                    break;

                case blast_operation.or:
                    if (not)
                    {
                        for (int i = 0; i < ssmd_datacount; i++) ((float2*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].x != 0 || b[i].x == 0 || b[i].y == 0);
                    }
                    else
                    {
                        for (int i = 0; i < ssmd_datacount; i++) ((float2*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].x != 0 || b[i].x != 0 || b[i].y != 0);
                    }
                    break;

                case blast_operation.xor:
                    if (not)
                    {
                        for (int i = 0; i < ssmd_datacount; i++) ((float2*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].x != 0 ^ b[i].xy == 0);
                    }
                    else
                    {
                        for (int i = 0; i < ssmd_datacount; i++) ((float2*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].x != 0 ^ b[i].xy != 0);
                    }
                    break;

                case blast_operation.greater:
                    if (minus)
                    {
                        for (int i = 0; i < ssmd_datacount; i++) ((float2*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].x > -b[i].xy);
                    }
                    else
                    {
                        for (int i = 0; i < ssmd_datacount; i++) ((float2*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].x > b[i].xy);
                    }
                    break;

                case blast_operation.smaller:
                    if (minus)
                    {
                        for (int i = 0; i < ssmd_datacount; i++) ((float2*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].x < -b[i].xy);
                    }
                    else
                    {
                        for (int i = 0; i < ssmd_datacount; i++) ((float2*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].x < b[i].xy);
                    }
                    break;

                case blast_operation.smaller_equals:
                    if (minus)
                    {
                        for (int i = 0; i < ssmd_datacount; i++) ((float2*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].x <= -b[i].xy);
                    }
                    else
                    {
                        for (int i = 0; i < ssmd_datacount; i++) ((float2*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].x <= b[i].xy);
                    }
                    break;

                case blast_operation.greater_equals:
                    if (minus)
                    {
                        for (int i = 0; i < ssmd_datacount; i++) ((float2*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].x >= -b[i].xy);
                    }
                    else
                    {
                        for (int i = 0; i < ssmd_datacount; i++) ((float2*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].x >= b[i].xy);
                    }
                    break;

                case blast_operation.equals:
                    if (not)
                    {
                        for (int i = 0; i < ssmd_datacount; i++) ((float2*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].x == math.select(0f, 1f, b[i].xy == 0));
                    }
                    if (minus)
                    {
                        for (int i = 0; i < ssmd_datacount; i++) ((float2*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].x == -b[i].xy);
                    }
                    else
                    {
                        for (int i = 0; i < ssmd_datacount; i++) ((float2*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].x == b[i].xy);
                    }
                    break;

                case blast_operation.not_equals:
                    if (not)
                    {
                        for (int i = 0; i < ssmd_datacount; i++) ((float2*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].x != math.select(0f, 1f, b[i].xy == 0));
                    }
                    if (minus)
                    {
                        for (int i = 0; i < ssmd_datacount; i++) ((float2*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].x != -b[i].xy);
                    }
                    else
                    {
                        for (int i = 0; i < ssmd_datacount; i++) ((float2*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].x != b[i].xy);
                    }
                    break;
            }
        }


        /// <summary>
        /// perform:  target.xyz = a.x [op] [minus|not] b.xyz
        /// </summary>
        void handle_op_f1_f3(in blast_operation op, [NoAlias] float4* a, [NoAlias] float4* b, ref byte vector_size, in bool minus, in bool not, in DATAREC target)
        {
            if (!target.is_set)
            {
#if DEVELOPMENT_BUILD || TRACE
                Debug.LogError($"blast.ssmd.handle_op_f1_f3: target not set");
#endif
                return;
            }

            vector_size = 3;
            switch (op)
            {
                default:
#if DEVELOPMENT_BUILD || TRACE
                    Debug.LogError($"blast.ssmd.handle_op_f1_f3: operation {op} not handled, vectorsize = {vector_size}, a[0] = {a[0]}, b[0] = {b[0]}");
#endif
                    return;
                case blast_operation.nop:
                    if (minus) simd.move_f4_array_to_indexed_data_as_f3(target.data, target.row_size, target.is_aligned, target.index, b, ssmd_datacount);
                    else simd.move_f4_array_to_indexed_data_as_f3_negated(target.data, target.row_size, target.is_aligned, target.index, b, ssmd_datacount);
                    break;

                case blast_operation.add:
                    if (minus) simd.sub_array_f1f3(a, b, target, ssmd_datacount);
                    else simd.add_array_f1f3(a, b, target, ssmd_datacount);
                    break;

                case blast_operation.multiply:
                    if (minus) simd.mul_array_f1f3_negated(a, b, target, ssmd_datacount);
                    else simd.mul_array_f1f3(a, b, target, ssmd_datacount);
                    break;

                case blast_operation.divide:
                    if (minus) simd.div_array_f1f3_negated(a, b, target, ssmd_datacount);
                    else simd.div_array_f1f3(a, b, target, ssmd_datacount);
                    break;

                case blast_operation.substract:
                    if (minus) simd.add_array_f1f3(a, b, target, ssmd_datacount);
                    else simd.sub_array_f1f3(a, b, target, ssmd_datacount);
                    break;

                case blast_operation.not:
                    if (!not)
                    {
                        // only set if not a double not 
                        for (int i = 0; i < ssmd_datacount; i++) ((float3*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(1f, 0f, b[i].xyz != 0);
                    }
                    break;

                case blast_operation.and:
                    if (not)
                    {
                        // the not applies to b[]
                        for (int i = 0; i < ssmd_datacount; i++) ((float3*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].x != 0 && b[i].x == 0 && b[i].y == 0 && b[i].z == 0);
                    }
                    else
                    {
                        for (int i = 0; i < ssmd_datacount; i++) ((float3*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].x != 0 && b[i].x != 0 && b[i].y != 0 && b[i].z != 0);
                    }
                    break;

                case blast_operation.or:
                    if (not)
                    {
                        for (int i = 0; i < ssmd_datacount; i++) ((float3*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].x != 0 || b[i].x == 0 || b[i].y == 0 || b[i].z == 0);
                    }
                    else
                    {
                        for (int i = 0; i < ssmd_datacount; i++) ((float3*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].x != 0 || b[i].x != 0 || b[i].y != 0 || b[i].z != 0);
                    }
                    break;

                case blast_operation.xor:
                    if (not)
                    {
                        for (int i = 0; i < ssmd_datacount; i++) ((float3*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].x != 0 ^ (b[i].xyz == 0));
                    }
                    else
                    {
                        for (int i = 0; i < ssmd_datacount; i++) ((float3*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].x != 0 ^ (b[i].xyz != 0));
                    }
                    break;

                case blast_operation.greater:
                    if (minus)
                    {
                        for (int i = 0; i < ssmd_datacount; i++) ((float3*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].x > -b[i].xyz);
                    }
                    else
                    {
                        for (int i = 0; i < ssmd_datacount; i++) ((float3*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].x > b[i].xyz);
                    }
                    break;

                case blast_operation.smaller:
                    if (minus)
                    {
                        for (int i = 0; i < ssmd_datacount; i++) ((float3*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].x < -b[i].xyz);
                    }
                    else
                    {
                        for (int i = 0; i < ssmd_datacount; i++) ((float3*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].x < b[i].xyz);
                    }
                    break;

                case blast_operation.smaller_equals:
                    if (minus)
                    {
                        for (int i = 0; i < ssmd_datacount; i++) ((float3*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].x <= -b[i].xyz);
                    }
                    else
                    {
                        for (int i = 0; i < ssmd_datacount; i++) ((float3*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].x <= b[i].xyz);
                    }
                    break;

                case blast_operation.greater_equals:
                    if (minus)
                    {
                        for (int i = 0; i < ssmd_datacount; i++) ((float3*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].x >= -b[i].xyz);
                    }
                    else
                    {
                        for (int i = 0; i < ssmd_datacount; i++) ((float3*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].x >= b[i].xyz);
                    }
                    break;

                case blast_operation.equals:
                    if (not)
                    {
                        for (int i = 0; i < ssmd_datacount; i++) ((float3*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].x == math.select(0f, 1f, b[i].xyz == 0));
                    }
                    if (minus)
                    {
                        for (int i = 0; i < ssmd_datacount; i++) ((float3*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].x == -b[i].xyz);
                    }
                    else
                    {
                        for (int i = 0; i < ssmd_datacount; i++) ((float3*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].x == b[i].xyz);
                    }
                    break;

                case blast_operation.not_equals:
                    if (not)
                    {
                        for (int i = 0; i < ssmd_datacount; i++) ((float3*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].x != math.select(0f, 1f, b[i].xyz == 0));
                    }
                    if (minus)
                    {
                        for (int i = 0; i < ssmd_datacount; i++) ((float3*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].x != -b[i].xyz);
                    }
                    else
                    {
                        for (int i = 0; i < ssmd_datacount; i++) ((float3*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].x != b[i].xyz);
                    }
                    break;
            }
        }


        /// <summary>
        /// perform:  target.xyzw = a.x [op] [minus|not] b.xyzw
        /// </summary>
        void handle_op_f1_f4(in blast_operation op, [NoAlias] float4* a, [NoAlias] float4* b, ref byte vector_size, in bool minus, in bool not, in DATAREC target)
        {
            if (!target.is_set)
            {
#if DEVELOPMENT_BUILD || TRACE
                Debug.LogError($"blast.ssmd.handle_op_f1_f4: target not set");
#endif
                return;
            }

            vector_size = 4;
            switch (op)
            {
                default:
#if DEVELOPMENT_BUILD || TRACE
                    Debug.LogError($"blast.ssmd.handle_op_f1_f4: operation {op} not handled, vectorsize = {vector_size}, a[0] = {a[0]}, b[0] = {b[0]}");
#endif
                    return;
                case blast_operation.nop:
                    if (minus) simd.move_f4_array_to_indexed_data_as_f4(target.data, target.row_size, target.is_aligned, target.index, b, ssmd_datacount);
                    else simd.move_f4_array_to_indexed_data_as_f4_negated(target.data, target.row_size, target.is_aligned, target.index, b, ssmd_datacount);
                    break;

                case blast_operation.add:
                    if (minus) simd.sub_array_f1f4(a, b, target, ssmd_datacount);
                    else simd.add_array_f1f4(a, b, target, ssmd_datacount);
                    break;

                case blast_operation.multiply:
                    if (minus) simd.mul_array_f1f4_negated(a, b, target, ssmd_datacount);
                    else simd.mul_array_f1f4(a, b, target, ssmd_datacount);
                    break;

                case blast_operation.divide:
                    if (minus) simd.div_array_f1f4_negated(a, b, target, ssmd_datacount);
                    else simd.div_array_f1f4(a, b, target, ssmd_datacount);
                    break;

                case blast_operation.substract:
                    if (minus) simd.add_array_f1f4(a, b, target, ssmd_datacount);
                    else simd.sub_array_f1f4(a, b, target, ssmd_datacount);
                    break;

                case blast_operation.not:
                    if (!not)
                    {
                        // only set if not a double not 
                        for (int i = 0; i < ssmd_datacount; i++) ((float4*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(1f, 0f, b[i].xyzw != 0);
                    }
                    break;

                case blast_operation.and:
                    if (not)
                    {
                        // the not applies to b[]
                        for (int i = 0; i < ssmd_datacount; i++) ((float4*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].x != 0 && !math.any(b[i]));
                    }
                    else
                    {
                        for (int i = 0; i < ssmd_datacount; i++) ((float4*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].x != 0 && math.any(b[i]));
                    }
                    break;

                case blast_operation.or:
                    if (not)
                    {
                        for (int i = 0; i < ssmd_datacount; i++) ((float4*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].x != 0 || !math.any(b[i]));
                    }
                    else
                    {
                        for (int i = 0; i < ssmd_datacount; i++) ((float4*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].x != 0 || math.any(b[i]));
                    }
                    break;

                case blast_operation.xor:
                    if (not)
                    {
                        for (int i = 0; i < ssmd_datacount; i++) ((float4*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].x != 0 ^ (b[i] == 0));
                    }
                    else
                    {
                        for (int i = 0; i < ssmd_datacount; i++) ((float4*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].x != 0 ^ (b[i] != 0));
                    }
                    break;

                case blast_operation.greater:
                    if (minus)
                    {
                        for (int i = 0; i < ssmd_datacount; i++) ((float4*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].x > -b[i]);
                    }
                    else
                    {
                        for (int i = 0; i < ssmd_datacount; i++) ((float4*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].x > b[i]);
                    }
                    break;

                case blast_operation.smaller:
                    if (minus)
                    {
                        for (int i = 0; i < ssmd_datacount; i++) ((float4*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].x < -b[i]);
                    }
                    else
                    {
                        for (int i = 0; i < ssmd_datacount; i++) ((float4*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].x < b[i]);
                    }
                    break;

                case blast_operation.smaller_equals:
                    if (minus)
                    {
                        for (int i = 0; i < ssmd_datacount; i++) ((float4*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].x <= -b[i]);
                    }
                    else
                    {
                        for (int i = 0; i < ssmd_datacount; i++) ((float4*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].x <= b[i]);
                    }
                    break;

                case blast_operation.greater_equals:
                    if (minus)
                    {
                        for (int i = 0; i < ssmd_datacount; i++) ((float4*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].x >= -b[i]);
                    }
                    else
                    {
                        for (int i = 0; i < ssmd_datacount; i++) ((float4*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].x >= b[i]);
                    }
                    break;

                case blast_operation.equals:
                    if (not)
                    {
                        for (int i = 0; i < ssmd_datacount; i++) ((float4*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].x == math.select(0f, 1f, b[i] == 0));
                    }
                    if (minus)
                    {
                        for (int i = 0; i < ssmd_datacount; i++) ((float4*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].x == -b[i]);
                    }
                    else
                    {
                        for (int i = 0; i < ssmd_datacount; i++) ((float4*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].x == b[i]);
                    }
                    break;

                case blast_operation.not_equals:
                    if (not)
                    {
                        for (int i = 0; i < ssmd_datacount; i++) ((float4*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].x != math.select(0f, 1f, b[i] == 0));
                    }
                    if (minus)
                    {
                        for (int i = 0; i < ssmd_datacount; i++) ((float4*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].x != -b[i]);
                    }
                    else
                    {
                        for (int i = 0; i < ssmd_datacount; i++) ((float4*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].x != b[i]);
                    }
                    break;
            }
        }



        /// <summary>
        /// perform:  target.xyz = a.x [op] b.xyz
        /// </summary>
        void handle_op_f1_f3(in blast_operation op, [NoAlias] float4* a, [NoAlias] float4* b, ref byte vector_size)
        {
            vector_size = 3;
            switch (op)
            {
                default:
#if DEVELOPMENT_BUILD || TRACE
                    Debug.LogError($"blast.ssmd.handle_op_fx_fy: operation {op} not handled, vectorsize = {vector_size}, a[0] = {a[0]}, b[0] = {b[0]}");
#endif
                    return;
                case blast_operation.nop: simd.move_f1_array_into_f4_array_as_f3(a, b, ssmd_datacount); break;
                case blast_operation.not: for (int i = 0; i < ssmd_datacount; i++) a[i].xyz = math.select((float3)1, (float3)0, b[i].xyz != 0); break;

                case blast_operation.add: simd.add_array_f1f3(a, b, ssmd_datacount); return;
                case blast_operation.substract: simd.sub_array_f1f3(a, b, ssmd_datacount); return;
                case blast_operation.multiply: simd.mul_array_f1f3(a, b, ssmd_datacount); return;
                case blast_operation.divide: simd.div_array_f1f3(a, b, ssmd_datacount); return;

                case blast_operation.and: for (int i = 0; i < ssmd_datacount; i++) a[i].xyz = math.select(0, 1, a[i].x != 0 && math.any(b[i].xyz)); return;
                case blast_operation.or: for (int i = 0; i < ssmd_datacount; i++) a[i].xyz = math.select(0, 1, a[i].x != 0 || math.any(b[i].xyz)); return;
                case blast_operation.xor: for (int i = 0; i < ssmd_datacount; i++) a[i].xyz = math.select(0, 1, a[i].x != 0 ^ math.any(b[i].xyz)); return;

                case blast_operation.greater: for (int i = 0; i < ssmd_datacount; i++) a[i].xyz = math.select(0f, 1f, a[i].x > b[i].xyz); return;
                case blast_operation.smaller: for (int i = 0; i < ssmd_datacount; i++) a[i].xyz = math.select(0f, 1f, a[i].x < b[i].xyz); return;
                case blast_operation.smaller_equals: for (int i = 0; i < ssmd_datacount; i++) a[i].xyz = math.select(0f, 1f, a[i].x <= b[i].xyz); return;
                case blast_operation.greater_equals: for (int i = 0; i < ssmd_datacount; i++) a[i].xyz = math.select(0f, 1f, a[i].x >= b[i].xyz); return;
                case blast_operation.equals: for (int i = 0; i < ssmd_datacount; i++) a[i].xyz = math.select(0f, 1f, a[i].x == b[i].xyz); return;
                case blast_operation.not_equals: for (int i = 0; i < ssmd_datacount; i++) a[i].xyz = math.select(0f, 1f, a[i].x != b[i].xyz); return;
            }
        }
        void handle_op_f1_f4(in blast_operation op, [NoAlias] float4* a, [NoAlias] float4* b, ref byte vector_size)
        {
            vector_size = 4;
            switch (op)
            {
                default:
#if DEVELOPMENT_BUILD || TRACE
                    Debug.LogError($"blast.ssmd.handle_op_fx_fy: operation {op} not handled, vectorsize = {vector_size}, a[0] = {a[0]}, b[0] = {b[0]}");
#endif
                    return;
                case blast_operation.nop: simd.move_f1_array_into_f4_array_as_f4(a, b, ssmd_datacount); break; // for (int i = 0; i < ssmd_datacount; i++) a[i] = b[i]; break;
                case blast_operation.not: for (int i = 0; i < ssmd_datacount; i++) a[i] = math.select((float4)1, (float4)0, b[i] != 0); break;

                case blast_operation.add: simd.add_array_f1f4(a, b, ssmd_datacount); return;
                case blast_operation.substract: simd.sub_array_f1f4(a, b, ssmd_datacount); return;
                case blast_operation.multiply: simd.mul_array_f1f4(a, b, ssmd_datacount); return;
                case blast_operation.divide: simd.div_array_f1f4(a, b, ssmd_datacount); return;

                case blast_operation.and: for (int i = 0; i < ssmd_datacount; i++) a[i] = math.select(0, 1, a[i].x != 0 && math.any(b[i])); return;
                case blast_operation.or: for (int i = 0; i < ssmd_datacount; i++) a[i] = math.select(0, 1, a[i].x != 0 || math.any(b[i])); return;
                case blast_operation.xor: for (int i = 0; i < ssmd_datacount; i++) a[i] = math.select(0, 1, a[i].x != 0 ^ math.any(b[i])); return;

                case blast_operation.greater: for (int i = 0; i < ssmd_datacount; i++) a[i] = math.select(0f, 1f, a[i].x > b[i]); return;
                case blast_operation.smaller: for (int i = 0; i < ssmd_datacount; i++) a[i] = math.select(0f, 1f, a[i].x < b[i]); return;
                case blast_operation.smaller_equals: for (int i = 0; i < ssmd_datacount; i++) a[i] = math.select(0f, 1f, a[i].x <= b[i]); return;
                case blast_operation.greater_equals: for (int i = 0; i < ssmd_datacount; i++) a[i] = math.select(0f, 1f, a[i].x >= b[i]); return;
                case blast_operation.equals: for (int i = 0; i < ssmd_datacount; i++) a[i] = math.select(0f, 1f, a[i].x == b[i]); return;
                case blast_operation.not_equals: for (int i = 0; i < ssmd_datacount; i++) a[i] = math.select(0f, 1f, a[i].x != b[i]); return;
            }
        }
        void handle_op_f2_f1(in blast_operation op, [NoAlias] float4* a, [NoAlias] float4* b, ref byte vector_size)
        {
            vector_size = 2;
            switch (op)
            {
                default:
#if DEVELOPMENT_BUILD || TRACE
                    Debug.LogError($"blast.ssmd.handle_op_fx_fy: operation {op} not handled, vectorsize = {vector_size}, a[0] = {a[0]}, b[0] = {b[0]}");
#endif
                    return;

                //case blast_operation.nop: 
                case blast_operation.nop: simd.move_f1_array_into_f4_array_as_f2(a, b, ssmd_datacount); break;

                case blast_operation.not: for (int i = 0; i < ssmd_datacount; i++) a[i].xy = math.select((float2)1, (float2)0, b[i].x != 0); break;

                case blast_operation.add: simd.add_array_f2f1(a, b, ssmd_datacount); return;
                case blast_operation.substract: simd.sub_array_f2f1(a, b, ssmd_datacount); return;
                case blast_operation.multiply: simd.mul_array_f2f1(a, b, ssmd_datacount); return;
                case blast_operation.divide: simd.div_array_f2f1(a, b, ssmd_datacount); return;

                case blast_operation.and: for (int i = 0; i < ssmd_datacount; i++) a[i].xy = math.select(0, 1, math.any(a[i].xy) && b[i].x != 0); return;
                case blast_operation.or: for (int i = 0; i < ssmd_datacount; i++) a[i].xy = math.select(0, 1, math.any(a[i].xy) || b[i].x != 0); return;
                case blast_operation.xor: for (int i = 0; i < ssmd_datacount; i++) a[i].xy = math.select(0, 1, math.any(a[i].xy) ^ b[i].x != 0); return;

                case blast_operation.greater: for (int i = 0; i < ssmd_datacount; i++) a[i].xy = math.select(0f, 1f, a[i].xy > b[i].x); return;
                case blast_operation.smaller: for (int i = 0; i < ssmd_datacount; i++) a[i].xy = math.select(0f, 1f, a[i].xy < b[i].x); return;
                case blast_operation.smaller_equals: for (int i = 0; i < ssmd_datacount; i++) a[i].xy = math.select(0f, 1f, a[i].xy <= b[i].x); return;
                case blast_operation.greater_equals: for (int i = 0; i < ssmd_datacount; i++) a[i].xy = math.select(0f, 1f, a[i].xy >= b[i].x); return;
                case blast_operation.equals: for (int i = 0; i < ssmd_datacount; i++) a[i].xy = math.select(0f, 1f, a[i].xy == b[i].x); return;
                case blast_operation.not_equals: for (int i = 0; i < ssmd_datacount; i++) a[i].xy = math.select(0f, 1f, a[i].xy != b[i].x); return;
            }
        }

        void handle_op_f2_f1(in blast_operation op, [NoAlias] float4* a, float constant, ref byte vector_size)
        {
            vector_size = 2;
            bool bconstant;
            float2 f2 = constant;
            switch (op)
            {
                default:
#if DEVELOPMENT_BUILD || TRACE
                    Debug.LogError($"blast.ssmd.handle_op_fx_fy: operation {op} not handled, vectorsize = {vector_size}, a[0] = {a[0]}, constant = {constant}");
#endif
                    return;
                case blast_operation.nop:
                    simd.move_f1_constant_into_f4_array_as_f2(a, constant, ssmd_datacount);
                    break;

                case blast_operation.not:
                    {
                        bconstant = constant != 0;
                        constant = math.select(1, 0, bconstant);
                        simd.move_f1_constant_into_f4_array_as_f2(a, constant, ssmd_datacount);
                    }
                    break;

                case blast_operation.add: simd.add_array_f2f1(a, constant, ssmd_datacount); return;
                case blast_operation.substract: simd.sub_array_f2f1(a, constant, ssmd_datacount); return;
                case blast_operation.multiply: simd.mul_array_f2f1(a, constant, ssmd_datacount); return;
                case blast_operation.divide: simd.div_array_f2f1(a, constant, ssmd_datacount); return;


                case blast_operation.and: bconstant = constant != 0; for (int i = 0; i < ssmd_datacount; i++) a[i].xy = math.select(0, 1, math.any(a[i].xy) && bconstant); return;
                case blast_operation.or: bconstant = constant != 0; for (int i = 0; i < ssmd_datacount; i++) a[i].xy = math.select(0, 1, math.any(a[i].xy) || bconstant); return;
                case blast_operation.xor: bconstant = constant != 0; for (int i = 0; i < ssmd_datacount; i++) a[i].xy = math.select(0, 1, math.any(a[i].xy) ^ bconstant); return;

                case blast_operation.greater: for (int i = 0; i < ssmd_datacount; i++) a[i].xy = math.select(0f, 1f, a[i].xy > constant); return;
                case blast_operation.smaller: for (int i = 0; i < ssmd_datacount; i++) a[i].xy = math.select(0f, 1f, a[i].xy < constant); return;
                case blast_operation.smaller_equals: for (int i = 0; i < ssmd_datacount; i++) a[i].xy = math.select(0f, 1f, a[i].xy <= constant); return;
                case blast_operation.greater_equals: for (int i = 0; i < ssmd_datacount; i++) a[i].xy = math.select(0f, 1f, a[i].xy >= constant); return;
                case blast_operation.equals: for (int i = 0; i < ssmd_datacount; i++) a[i].xy = math.select(0f, 1f, a[i].xy == constant); return;
                case blast_operation.not_equals: for (int i = 0; i < ssmd_datacount; i++) a[i].xy = math.select(0f, 1f, a[i].xy != constant); return;
            }
        }

        /// <summary>
        /// perform:  target.xy = a.xy [op] constant
        /// </summary>
        void handle_op_f2_f1(in blast_operation op, [NoAlias] float4* a, float constant, ref byte vector_size, in DATAREC target)
        {
            if (!target.is_set)
            {
#if DEVELOPMENT_BUILD || TRACE
                Debug.LogError($"blast.ssmd.handle_op_f2_f1 [constant b]: target not set");
#endif
                return;
            }

            vector_size = 2;
            switch (op)
            {
                default:
#if DEVELOPMENT_BUILD || TRACE
                    Debug.LogError($"blast.ssmd.handle_op_f2_f1: operation {op} not handled, vectorsize = {vector_size}, a[0] = {a[0]}, constant = {constant}");
#endif
                    break;

                case blast_operation.nop:               simd.move_f1_constant_to_indexed_data_as_f2(target.data, target.row_size, target.is_aligned, target.index, constant, ssmd_datacount); break;
                case blast_operation.not:               simd.move_f1_constant_to_indexed_data_as_f2(target.data, target.row_size, target.is_aligned, target.index, math.select(0f, 1f, constant == 0), ssmd_datacount); break;

                case blast_operation.add:               simd.add_array_f2f1(a, constant, target, ssmd_datacount); break;
                case blast_operation.substract:         simd.sub_array_f2f1(a, constant, target, ssmd_datacount); break;
                case blast_operation.multiply:          simd.mul_array_f2f1(a, constant, target, ssmd_datacount); break;
                case blast_operation.divide:            simd.div_array_f2f1(a, constant, target, ssmd_datacount); break;

                case blast_operation.and:               { bool bconstant = constant != 0; for (int i = 0; i < ssmd_datacount; i++) ((float2*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0, 1, math.any(a[i].xy) && bconstant); } break; 
                case blast_operation.or:                { bool bconstant = constant != 0; for (int i = 0; i < ssmd_datacount; i++) ((float2*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0, 1, math.any(a[i].xy) || bconstant); } break; 
                case blast_operation.xor:               { bool bconstant = constant != 0; for (int i = 0; i < ssmd_datacount; i++) ((float2*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0, 1, math.any(a[i].xy) ^ bconstant); }break;

                case blast_operation.greater:           for (int i = 0; i < ssmd_datacount; i++) ((float2*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].xy > constant);  break;
                case blast_operation.smaller:           for (int i = 0; i < ssmd_datacount; i++) ((float2*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].xy < constant);  break;
                case blast_operation.smaller_equals:    for (int i = 0; i < ssmd_datacount; i++) ((float2*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].xy <= constant); break;
                case blast_operation.greater_equals:    for (int i = 0; i < ssmd_datacount; i++) ((float2*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].xy >= constant); break;
                case blast_operation.equals:            for (int i = 0; i < ssmd_datacount; i++) ((float2*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].xy == constant); break;
                case blast_operation.not_equals:        for (int i = 0; i < ssmd_datacount; i++) ((float2*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].xy != constant); break;
            }
        }



        void handle_op_f2_f2(in blast_operation op, [NoAlias] float4* a, [NoAlias] float4* b, ref byte vector_size)
        {
            vector_size = 2;
            switch (op)
            {
                default:
#if DEVELOPMENT_BUILD || TRACE
                    Debug.LogError($"blast.ssmd.handle_op_fx_fy: operation {op} not handled, vectorsize = {vector_size}, a[0] = {a[0]}, b[0] = {b[0]}");
#endif
                    return;
                case blast_operation.nop:
                    {
                        if (a != b)
                        {
                            simd.move_f2_array_into_f4_array_as_f2(a, b, ssmd_datacount);
                        }
                    }
                    break;
                case blast_operation.not:
                    {
                        for (int i = 0; i < ssmd_datacount; i++) a[i].xy = math.select((float2)1, (float2)0, b[i].xy != 0); break;
                    }


                case blast_operation.add: simd.add_array_f2f2(a, b, ssmd_datacount); return;
                case blast_operation.substract: simd.sub_array_f2f2(a, b, ssmd_datacount); return;
                case blast_operation.multiply: simd.mul_array_f2f2(a, b, ssmd_datacount); return;
                case blast_operation.divide: simd.div_array_f2f2(a, b, ssmd_datacount); return;

                case blast_operation.and: for (int i = 0; i < ssmd_datacount; i++) a[i].xy = math.select(0, 1, math.any(a[i].xy) && math.any(b[i].xy)); return;
                case blast_operation.or: for (int i = 0; i < ssmd_datacount; i++) a[i].xy = math.select(0, 1, math.any(a[i].xy) || math.any(b[i].xy)); return;
                case blast_operation.xor: for (int i = 0; i < ssmd_datacount; i++) a[i].xy = math.select(0, 1, math.any(a[i].xy) ^ math.any(b[i].xy)); return;

                case blast_operation.greater: for (int i = 0; i < ssmd_datacount; i++) a[i].xy = math.select(0f, 1f, a[i].xy > b[i].xy); return;
                case blast_operation.smaller: for (int i = 0; i < ssmd_datacount; i++) a[i].xy = math.select(0f, 1f, a[i].xy < b[i].xy); return;
                case blast_operation.smaller_equals: for (int i = 0; i < ssmd_datacount; i++) a[i].xy = math.select(0f, 1f, a[i].xy <= b[i].xy); return;
                case blast_operation.greater_equals: for (int i = 0; i < ssmd_datacount; i++) a[i].xy = math.select(0f, 1f, a[i].xy >= b[i].xy); return;
                case blast_operation.equals: for (int i = 0; i < ssmd_datacount; i++) a[i].xy = math.select(0f, 1f, a[i].xy == b[i].xy); return;
                case blast_operation.not_equals: for (int i = 0; i < ssmd_datacount; i++) a[i].xy = math.select(0f, 1f, a[i].xy != b[i].xy); return;
            }
        }
        void handle_op_f3_f1(in blast_operation op, [NoAlias] float4* a, [NoAlias] float4* b, ref byte vector_size)
        {
            vector_size = 3;
            switch (op)
            {
                default:
#if DEVELOPMENT_BUILD || TRACE
                    Debug.LogError($"blast.ssmd.handle_op_fx_fy: operation {op} not handled, vectorsize = {vector_size}, a[0] = {a[0]}, b[0] = {b[0]}");
#endif
                    return;
                case blast_operation.nop: simd.move_f1_array_into_f4_array_as_f3(a, b, ssmd_datacount); break;
                case blast_operation.not: for (int i = 0; i < ssmd_datacount; i++) a[i].xyz = math.select((float3)1, (float3)0, b[i].x != 0); break;

                case blast_operation.add:       simd.add_array_f3f1(a, b, ssmd_datacount); return;
                case blast_operation.substract: simd.sub_array_f3f1(a, b, ssmd_datacount); return;
                case blast_operation.multiply:  simd.mul_array_f3f1(a, b, ssmd_datacount); return;
                case blast_operation.divide:    simd.div_array_f3f1(a, b, ssmd_datacount); return;

                case blast_operation.and: for (int i = 0; i < ssmd_datacount; i++) a[i].xyz = math.select(0, 1, math.any(a[i].xyz) && b[i].x != 0); return;
                case blast_operation.or: for (int i = 0; i < ssmd_datacount; i++) a[i].xyz = math.select(0, 1, math.any(a[i].xyz) || b[i].x != 0); return;
                case blast_operation.xor: for (int i = 0; i < ssmd_datacount; i++) a[i].xyz = math.select(0, 1, math.any(a[i].xyz) ^ b[i].x != 0); return;

                case blast_operation.greater: for (int i = 0; i < ssmd_datacount; i++) a[i].xyz = math.select(0f, 1f, a[i].xyz > b[i].x); return;
                case blast_operation.smaller: for (int i = 0; i < ssmd_datacount; i++) a[i].xyz = math.select(0f, 1f, a[i].xyz < b[i].x); return;
                case blast_operation.smaller_equals: for (int i = 0; i < ssmd_datacount; i++) a[i].xyz = math.select(0f, 1f, a[i].xyz <= b[i].x); return;
                case blast_operation.greater_equals: for (int i = 0; i < ssmd_datacount; i++) a[i].xyz = math.select(0f, 1f, a[i].xyz >= b[i].x); return;
                case blast_operation.equals: for (int i = 0; i < ssmd_datacount; i++) a[i].xyz = math.select(0f, 1f, a[i].xyz == b[i].x); return;
                case blast_operation.not_equals: for (int i = 0; i < ssmd_datacount; i++) a[i].xyz = math.select(0f, 1f, a[i].xyz != b[i].x); return;
            }
        }

        void handle_op_f3_f1(in blast_operation op, [NoAlias] float4* a, float constant, ref byte vector_size)
        {
            vector_size = 3;
            bool bconstant;
            switch (op)
            {
                default:
#if DEVELOPMENT_BUILD || TRACE
                    Debug.LogError($"blast.ssmd.handle_op_fx_fy: operation {op} not handled, vectorsize = {vector_size}, a[0] = {a[0]}, constant = {constant}");
#endif
                    return;
                case blast_operation.nop: simd.move_f1_constant_into_f4_array_as_f3(a, constant, ssmd_datacount); break;
                case blast_operation.not: simd.move_f1_constant_into_f4_array_as_f3(a, math.select(1f, 0f, constant != 0), ssmd_datacount); break;

                //case blast_operation.nop: for (int i = 0; i < ssmd_datacount; i++) a[i].xyz = constant; break;
                //case blast_operation.not: constant = math.select(1, 0, constant != 0); for (int i = 0; i < ssmd_datacount; i++) a[i].xyz = constant; break;

                case blast_operation.add: simd.add_array_f3f1(a, constant, ssmd_datacount); break;
                case blast_operation.substract: simd.sub_array_f3f1(a, constant, ssmd_datacount); break;
                case blast_operation.multiply: simd.mul_array_f3f1(a, constant, ssmd_datacount); break;
                case blast_operation.divide: simd.div_array_f3f1(a, constant, ssmd_datacount); break;

                case blast_operation.and: bconstant = constant != 0; for (int i = 0; i < ssmd_datacount; i++) a[i].xyz = math.select(0, 1, math.any(a[i].xyz) && bconstant); return;
                case blast_operation.or: bconstant = constant != 0; for (int i = 0; i < ssmd_datacount; i++) a[i].xyz = math.select(0, 1, math.any(a[i].xyz) || bconstant); return;
                case blast_operation.xor: bconstant = constant != 0; for (int i = 0; i < ssmd_datacount; i++) a[i].xyz = math.select(0, 1, math.any(a[i].xyz) ^ bconstant); return;

                case blast_operation.greater: for (int i = 0; i < ssmd_datacount; i++) a[i].xyz = math.select(0f, 1f, a[i].xyz > constant); return;
                case blast_operation.smaller: for (int i = 0; i < ssmd_datacount; i++) a[i].xyz = math.select(0f, 1f, a[i].xyz < constant); return;
                case blast_operation.smaller_equals: for (int i = 0; i < ssmd_datacount; i++) a[i].xyz = math.select(0f, 1f, a[i].xyz <= constant); return;
                case blast_operation.greater_equals: for (int i = 0; i < ssmd_datacount; i++) a[i].xyz = math.select(0f, 1f, a[i].xyz >= constant); return;
                case blast_operation.equals: for (int i = 0; i < ssmd_datacount; i++) a[i].xyz = math.select(0f, 1f, a[i].xyz == constant); return;
                case blast_operation.not_equals: for (int i = 0; i < ssmd_datacount; i++) a[i].xyz = math.select(0f, 1f, a[i].xyz != constant); return;
            }
        }

        void handle_op_f3_f1(in blast_operation op, [NoAlias] float4* a, float constant, ref byte vector_size, in DATAREC target)
        {
            if (!target.is_set)
            {
#if DEVELOPMENT_BUILD || TRACE
                Debug.LogError($"blast.ssmd.handle_op_f3_f1 [constant b]: target not set");
#endif
                return;
            }

            vector_size = 3;
            bool bconstant;
            switch (op)
            {
                default:
#if DEVELOPMENT_BUILD || TRACE
                    Debug.LogError($"blast.ssmd.handle_op_fx_fy: operation {op} not handled, vectorsize = {vector_size}, a[0] = {a[0]}, constant = {constant}");
#endif
                    return;
                case blast_operation.nop: simd.move_f1_constant_to_indexed_data_as_f3(target.data, target.row_size, target.is_aligned, target.index, constant, ssmd_datacount); break;
                case blast_operation.not: simd.move_f1_constant_to_indexed_data_as_f3(target.data, target.row_size, target.is_aligned, target.index, math.select(1f, 0f, constant != 0), ssmd_datacount); break;

                //case blast_operation.nop: for (int i = 0; i < ssmd_datacount; i++) a[i].xyz = constant; break;
                //case blast_operation.not: constant = math.select(1, 0, constant != 0); for (int i = 0; i < ssmd_datacount; i++) a[i].xyz = constant; break;

                case blast_operation.add: simd.add_array_f3f1(a, constant, target, ssmd_datacount); break;
                case blast_operation.substract: simd.sub_array_f3f1(a, constant, target, ssmd_datacount); break;
                case blast_operation.multiply: simd.mul_array_f3f1(a, constant, target, ssmd_datacount); break;
                case blast_operation.divide: simd.div_array_f3f1(a, constant, target, ssmd_datacount); break;

                case blast_operation.and: bconstant = constant != 0; for (int i = 0; i < ssmd_datacount; i++) ((float3*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0, 1, math.any(a[i].xyz) && bconstant); return;
                case blast_operation.or: bconstant = constant != 0; for (int i = 0; i < ssmd_datacount; i++) ((float3*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0, 1, math.any(a[i].xyz) || bconstant); return;
                case blast_operation.xor: bconstant = constant != 0; for (int i = 0; i < ssmd_datacount; i++) ((float3*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0, 1, math.any(a[i].xyz) ^ bconstant); return;

                case blast_operation.greater: for (int i = 0; i < ssmd_datacount; i++)          ((float3*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].xyz > constant); return;
                case blast_operation.smaller: for (int i = 0; i < ssmd_datacount; i++)          ((float3*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].xyz < constant); return;
                case blast_operation.smaller_equals: for (int i = 0; i < ssmd_datacount; i++)   ((float3*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].xyz <= constant); return;
                case blast_operation.greater_equals: for (int i = 0; i < ssmd_datacount; i++)   ((float3*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].xyz >= constant); return;
                case blast_operation.equals: for (int i = 0; i < ssmd_datacount; i++)           ((float3*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].xyz == constant); return;
                case blast_operation.not_equals: for (int i = 0; i < ssmd_datacount; i++)       ((float3*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i].xyz != constant); return;
            }
        }


        void handle_op_f3_f3(in blast_operation op, [NoAlias] float4* a, [NoAlias] float4* b, ref byte vector_size)
        {
            vector_size = 3;
            switch (op)
            {
                default:
#if DEVELOPMENT_BUILD || TRACE
                    Debug.LogError($"blast.ssmd.handle_op_fx_fy: operation {op} not handled, vectorsize = {vector_size}, a[0] = {a[0]}, b[0] = {b[0]}");
#endif
                    return;
                case blast_operation.nop: simd.move_f3_array_into_f4_array_as_f3(a, b, ssmd_datacount); break; // a[i].xyz = b[i].xyz; break;

                case blast_operation.not:
                    {
                        float* fa = (float*)(void*)a;
                        float* fb = (float*)(void*)b;
                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            fa[0] = math.select(1f, 0f, fb[0] != 0f); // x
                            fa[1] = math.select(1f, 0f, fb[1] != 0f); // y
                            fa[2] = math.select(1f, 0f, fb[2] != 0f); // z
                            fa += 4;
                        }
                        // for (int i = 0; i < ssmd_datacount; i++) a[i].xyz = math.select((float3)1, (float3)0, b[i].xyz != 0); break;
                    }
                    break;

                case blast_operation.add:       simd.add_array_f3f3(a, b, ssmd_datacount); break;
                case blast_operation.substract: simd.sub_array_f3f3(a, b, ssmd_datacount); break;
                case blast_operation.multiply:  simd.mul_array_f3f3(a, b, ssmd_datacount); break;
                case blast_operation.divide:    simd.div_array_f3f3(a, b, ssmd_datacount); break; 

                case blast_operation.and: for (int i = 0; i < ssmd_datacount; i++) a[i].xyz = math.select(0, 1, math.any(a[i].xyz) && math.any(b[i].xyz)); return;
                case blast_operation.or: for (int i = 0; i < ssmd_datacount; i++) a[i].xyz = math.select(0, 1, math.any(a[i].xyz) || math.any(b[i].xyz)); return;
                case blast_operation.xor: for (int i = 0; i < ssmd_datacount; i++) a[i].xyz = math.select(0, 1, math.any(a[i].xyz) ^ math.any(b[i].xyz)); return;

                case blast_operation.greater: for (int i = 0; i < ssmd_datacount; i++) a[i].xyz = math.select(0f, 1f, a[i].xyz > b[i].xyz); return;
                case blast_operation.smaller: for (int i = 0; i < ssmd_datacount; i++) a[i].xyz = math.select(0f, 1f, a[i].xyz < b[i].xyz); return;
                case blast_operation.smaller_equals: for (int i = 0; i < ssmd_datacount; i++) a[i].xyz = math.select(0f, 1f, a[i].xyz <= b[i].xyz); return;
                case blast_operation.greater_equals: for (int i = 0; i < ssmd_datacount; i++) a[i].xyz = math.select(0f, 1f, a[i].xyz >= b[i].xyz); return;
                case blast_operation.equals: for (int i = 0; i < ssmd_datacount; i++) a[i].xyz = math.select(0f, 1f, a[i].xyz == b[i].xyz); return;
                case blast_operation.not_equals: for (int i = 0; i < ssmd_datacount; i++) a[i].xyz = math.select(0f, 1f, a[i].xyz != b[i].xyz); return;
            }
        }
        void handle_op_f4_f1(in blast_operation op, [NoAlias] float4* a, [NoAlias] float4* b, ref byte vector_size)
        {
            vector_size = 4;
            switch (op)
            {
                default:
#if DEVELOPMENT_BUILD || TRACE
                    Debug.LogError($"blast.ssmd.handle_op_fx_fy: operation {op} not handled, vectorsize = {vector_size}, a[0] = {a[0]}, b[0] = {b[0]}");
#endif
                    return;
                case blast_operation.nop: simd.move_f1_array_into_f4_array_as_f4(a, b, ssmd_datacount); break;
                case blast_operation.not: for (int i = 0; i < ssmd_datacount; i++) a[i] = math.select(1, 0, b[i].x != 0); break;

                case blast_operation.add:       simd.add_array_f4f1(a, b, ssmd_datacount); return;
                case blast_operation.substract: simd.sub_array_f4f1(a, b, ssmd_datacount); return;
                case blast_operation.multiply:  simd.mul_array_f4f1(a, b, ssmd_datacount); return;
                case blast_operation.divide:    simd.div_array_f4f1(a, b, ssmd_datacount); return;

                case blast_operation.and: for (int i = 0; i < ssmd_datacount; i++) a[i] = math.select(0, 1, math.any(a[i]) && b[i].x != 0); return;
                case blast_operation.or: for (int i = 0; i < ssmd_datacount; i++) a[i] = math.select(0, 1, math.any(a[i]) || b[i].x != 0); return;
                case blast_operation.xor: for (int i = 0; i < ssmd_datacount; i++) a[i] = math.select(0, 1, math.any(a[i]) ^ b[i].x != 0); return;

                case blast_operation.greater: for (int i = 0; i < ssmd_datacount; i++) a[i] = math.select(0f, 1f, a[i] > b[i].x); return;
                case blast_operation.smaller: for (int i = 0; i < ssmd_datacount; i++) a[i] = math.select(0f, 1f, a[i] < b[i].x); return;
                case blast_operation.smaller_equals: for (int i = 0; i < ssmd_datacount; i++) a[i] = math.select(0f, 1f, a[i] <= b[i].x); return;
                case blast_operation.greater_equals: for (int i = 0; i < ssmd_datacount; i++) a[i] = math.select(0f, 1f, a[i] >= b[i].x); return;
                case blast_operation.equals: for (int i = 0; i < ssmd_datacount; i++) a[i] = math.select(0f, 1f, a[i] == b[i].x); return;
                case blast_operation.not_equals: for (int i = 0; i < ssmd_datacount; i++) a[i] = math.select(0f, 1f, a[i] != b[i].x); return;
            }
        }
        void handle_op_f4_f1(in blast_operation op, [NoAlias] float4* a, float constant, ref byte vector_size)
        {
            vector_size = 4;
            bool bconstant;
            switch (op)
            {
                default:
#if DEVELOPMENT_BUILD || TRACE
                    Debug.LogError($"blast.ssmd.handle_op_fx_fy: operation {op} not handled, vectorsize = {vector_size}, a[0] = {a[0]}, constant = {constant}");
#endif
                    return;
                case blast_operation.nop: simd.move_f1_constant_into_f4_array_as_f4(a, constant, ssmd_datacount); break;
                // case blast_operation.nop: for (int i = 0; i < ssmd_datacount; i++) a[i] = constant; break;
                case blast_operation.not: simd.move_f1_constant_into_f4_array_as_f4(a, math.select(1f, 0f, constant != 0), ssmd_datacount); break;
                // case blast_operation.not: constant = math.select(1, 0, constant != 0); for (int i = 0; i < ssmd_datacount; i++) a[i] = constant; break;

                case blast_operation.add: simd.add_array_f4f1(a, constant, ssmd_datacount); return;
                case blast_operation.substract: simd.sub_array_f4f1(a, constant, ssmd_datacount); return;
                case blast_operation.multiply: simd.mul_array_f4f1(a, constant, ssmd_datacount); return;
                case blast_operation.divide: simd.div_array_f4f1(a, constant, ssmd_datacount); return;

                case blast_operation.and: bconstant = constant != 0; for (int i = 0; i < ssmd_datacount; i++) a[i] = math.select(0, 1, math.any(a[i]) && bconstant); return;
                case blast_operation.or: bconstant = constant != 0; for (int i = 0; i < ssmd_datacount; i++) a[i] = math.select(0, 1, math.any(a[i]) || bconstant); return;
                case blast_operation.xor: bconstant = constant != 0; for (int i = 0; i < ssmd_datacount; i++) a[i] = math.select(0, 1, math.any(a[i]) ^ bconstant); return;

                case blast_operation.greater: for (int i = 0; i < ssmd_datacount; i++) a[i] = math.select(0f, 1f, a[i] > constant); return;
                case blast_operation.smaller: for (int i = 0; i < ssmd_datacount; i++) a[i] = math.select(0f, 1f, a[i] < constant); return;
                case blast_operation.smaller_equals: for (int i = 0; i < ssmd_datacount; i++) a[i] = math.select(0f, 1f, a[i] <= constant); return;
                case blast_operation.greater_equals: for (int i = 0; i < ssmd_datacount; i++) a[i] = math.select(0f, 1f, a[i] >= constant); return;
                case blast_operation.equals: for (int i = 0; i < ssmd_datacount; i++) a[i] = math.select(0f, 1f, a[i] == constant); return;
                case blast_operation.not_equals: for (int i = 0; i < ssmd_datacount; i++) a[i] = math.select(0f, 1f, a[i] != constant); return;
            }
        }


        void handle_op_f4_f1(in blast_operation op, [NoAlias] float4* a, float constant, ref byte vector_size, in DATAREC target)
        {
            if (!target.is_set)
            {
#if DEVELOPMENT_BUILD || TRACE
                Debug.LogError($"blast.ssmd.handle_op_f4_f1 [constant b]: target not set");
#endif
                return;
            }

            vector_size = 4;
            bool bconstant;
            switch (op)
            {
                default:
#if DEVELOPMENT_BUILD || TRACE
                    Debug.LogError($"blast.ssmd.handle_op_f4_f1: operation {op} not handled, vectorsize = {vector_size}, a[0] = {a[0]}, constant = {constant}");
#endif
                    return;
                case blast_operation.nop: simd.move_f1_constant_to_indexed_data_as_f4(target.data, target.row_size, target.is_aligned, target.index, constant, ssmd_datacount); break;
                case blast_operation.not: simd.move_f1_constant_to_indexed_data_as_f4(target.data, target.row_size, target.is_aligned, target.index, math.select(1f, 0f, constant != 0), ssmd_datacount); break;

                case blast_operation.add: simd.add_array_f4f1(a, constant, target, ssmd_datacount); return;
                case blast_operation.substract: simd.sub_array_f4f1(a, constant, target, ssmd_datacount); return;
                case blast_operation.multiply: simd.mul_array_f4f1(a, constant, target, ssmd_datacount); return;
                case blast_operation.divide: simd.div_array_f4f1(a, constant, target, ssmd_datacount); return;

                case blast_operation.and: bconstant = constant != 0; for (int i = 0; i < ssmd_datacount; i++) ((float4*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0, 1, math.any(a[i]) && bconstant); return;
                case blast_operation.or: bconstant = constant != 0; for (int i = 0; i < ssmd_datacount; i++) ((float4*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0, 1, math.any(a[i]) || bconstant); return;
                case blast_operation.xor: bconstant = constant != 0; for (int i = 0; i < ssmd_datacount; i++) ((float4*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0, 1, math.any(a[i]) ^ bconstant); return;

                case blast_operation.greater: for (int i = 0; i < ssmd_datacount; i++) ((float4*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i] > constant); return;
                case blast_operation.smaller: for (int i = 0; i < ssmd_datacount; i++) ((float4*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i] < constant); return;
                case blast_operation.smaller_equals: for (int i = 0; i < ssmd_datacount; i++) ((float4*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i] <= constant); return;
                case blast_operation.greater_equals: for (int i = 0; i < ssmd_datacount; i++) ((float4*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i] >= constant); return;
                case blast_operation.equals: for (int i = 0; i < ssmd_datacount; i++) ((float4*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i] == constant); return;
                case blast_operation.not_equals: for (int i = 0; i < ssmd_datacount; i++) ((float4*)(void*)&((float**)target.data)[i][target.index])[0] = math.select(0f, 1f, a[i] != constant); return;
            }
        }



        void handle_op_f4_f4(in blast_operation op, [NoAlias] float4* a, [NoAlias] float4* b, ref byte vector_size)
        {
            vector_size = 4;
            switch (op)
            {
                default:
#if DEVELOPMENT_BUILD || TRACE
                    Debug.LogError($"blast.ssmd.handle_op_fx_fy: operation {op} not handled, vectorsize = {vector_size}, a[0] = {a[0]}, b[0] = {b[0]}");
#endif
                    return;
                case blast_operation.nop: simd.move_f4_array_into_f4_array_as_f4(a, b, ssmd_datacount); break;
                case blast_operation.not: for (int i = 0; i < ssmd_datacount; i++) a[i] = math.select((float4)1f, (float4)0f, b[i] != 0); break;

                case blast_operation.add: simd.add_array_f4f1(a, b, ssmd_datacount); return;
                case blast_operation.substract: simd.sub_array_f4f1(a, b, ssmd_datacount); return;
                case blast_operation.multiply: simd.mul_array_f4f1(a, b, ssmd_datacount); return;
                case blast_operation.divide: simd.div_array_f4f1(a, b, ssmd_datacount); return;

                case blast_operation.and: for (int i = 0; i < ssmd_datacount; i++) a[i] = math.select(0, 1, math.any(a[i]) && math.any(b[i])); return;
                case blast_operation.or: for (int i = 0; i < ssmd_datacount; i++) a[i] = math.select(0, 1, math.any(a[i]) || math.any(b[i])); return;
                case blast_operation.xor: for (int i = 0; i < ssmd_datacount; i++) a[i] = math.select(0, 1, math.any(a[i]) ^ math.any(b[i])); return;

                case blast_operation.greater: for (int i = 0; i < ssmd_datacount; i++) a[i] = math.select(0f, 1f, a[i] > b[i]); return;
                case blast_operation.smaller: for (int i = 0; i < ssmd_datacount; i++) a[i] = math.select(0f, 1f, a[i] < b[i]); return;
                case blast_operation.smaller_equals: for (int i = 0; i < ssmd_datacount; i++) a[i] = math.select(0f, 1f, a[i] <= b[i]); return;
                case blast_operation.greater_equals: for (int i = 0; i < ssmd_datacount; i++) a[i] = math.select(0f, 1f, a[i] >= b[i]); return;
                case blast_operation.equals: for (int i = 0; i < ssmd_datacount; i++) a[i] = math.select(0f, 1f, a[i] == b[i]); return;
                case blast_operation.not_equals: for (int i = 0; i < ssmd_datacount; i++) a[i] = math.select(0f, 1f, a[i] != b[i]); return;
            }
        }


        #endregion

        #region Single Input functions (abs, sin etc) 

        private void handle_single_op_constant([NoAlias] float4* register, ref byte vector_size, float constant, in blast_operation op, in extended_blast_operation ex_op, in bool is_negated = false, in bool not = false, bool is_sequenced = false)
        {
            handle_constant_value_operation(ref vector_size, ref constant, in op, in ex_op, in is_negated, in not, ref is_sequenced);
            handle_single_op_constant_to_register(register, ref vector_size, constant, op, is_sequenced); 
        }

        void handle_single_op_constant([NoAlias] float4* register, ref byte vector_size, float constant, in blast_operation op, in extended_blast_operation ex_op, bool is_negated, bool not, bool is_sequenced, in DATAREC target_rec)
        {
            handle_constant_value_operation(ref vector_size, ref constant, in op, in ex_op, in is_negated, in not, ref is_sequenced);

            if (target_rec.is_set)
            {
                // output to indexed records 
                handle_single_op_constant_to_target(register, ref vector_size, constant, op, is_sequenced, target_rec);
            }
            else
            {
                // output to register 
                handle_single_op_constant_to_register(register, ref vector_size, constant, op, is_sequenced);
            }
        }

        private void handle_constant_value_operation(ref byte vector_size, ref float constant, in blast_operation op, in extended_blast_operation ex_op, in bool is_negated, in bool not, ref bool is_sequenced)
        {
            // perform op on value
            switch (op)
            {
                // for completeness.. as long as constants are 1 element all return the same data and are pointless
                case blast_operation.index_x:
                case blast_operation.index_y:
                case blast_operation.index_z:
                case blast_operation.index_w:
                    vector_size = 1;
                    break;

                case blast_operation.expand_v2: vector_size = 2; break;
                case blast_operation.expand_v3: vector_size = 3; break;
                case blast_operation.expand_v4: vector_size = 4; break;

                case blast_operation.nop: break;
                case blast_operation.not: constant = math.select(0, 1, constant == 0); break;
                case blast_operation.abs: constant = math.abs(constant); break;
                case blast_operation.trunc: constant = math.trunc(constant); break;

                // in-sequence operations: handle sequenced operation on register
                case blast_operation.add:
                case blast_operation.multiply:
                case blast_operation.divide:
                case blast_operation.and:
                case blast_operation.or:
                case blast_operation.xor:
                case blast_operation.greater:
                case blast_operation.smaller:
                case blast_operation.smaller_equals:
                case blast_operation.greater_equals:
                case blast_operation.equals:
                case blast_operation.not_equals:
                // if handling a sequence this would be handled in getsequence
                case blast_operation.substract:
                    {
                        is_sequenced = true;
                    }
                    break;

                // extended operations 
                case blast_operation.ex_op:
                    switch (ex_op)
                    {
                        case extended_blast_operation.sqrt: constant = math.sqrt(constant); break;
                        case extended_blast_operation.rsqrt: constant = math.rsqrt(constant); break;
                        case extended_blast_operation.sin: constant = math.sin(constant); break;
                        case extended_blast_operation.cos: constant = math.cos(constant); break;
                        case extended_blast_operation.tan: constant = math.tan(constant); break;
                        case extended_blast_operation.sinh: constant = math.sinh(constant); break;
                        case extended_blast_operation.cosh: constant = math.cosh(constant); break;
                        case extended_blast_operation.atan: constant = math.atan(constant); break;
                        case extended_blast_operation.degrees: constant = math.degrees(constant); break;
                        case extended_blast_operation.radians: constant = math.radians(constant); break;
                        case extended_blast_operation.ceil: constant = math.ceil(constant); break;
                        case extended_blast_operation.floor: constant = math.floor(constant); break;
                        case extended_blast_operation.frac: constant = math.frac(constant); break;
                        case extended_blast_operation.normalize: break; // constant = constant; break; // not defined on size 1
                        case extended_blast_operation.saturate: constant = math.saturate(constant); break;
                        case extended_blast_operation.logn: constant = math.log(constant); break;
                        case extended_blast_operation.log10: constant = math.log10(constant); break;
                        case extended_blast_operation.log2: constant = math.log2(constant); break;
                        case extended_blast_operation.exp10: constant = math.exp10(constant); break;
                        case extended_blast_operation.exp: constant = math.exp(constant); break;
                        case extended_blast_operation.ceillog2: constant = math.ceillog2((int)constant); break;
                        case extended_blast_operation.ceilpow2: constant = math.ceilpow2((int)constant); break;
                        case extended_blast_operation.floorlog2: constant = math.floorlog2((int)constant); break;

                        default:
#if DEVELOPMENT_BUILD || TRACE
                            Debug.LogError($"blast.ssmd.handle_single_op_constant -> operation {ex_op} not supported");
#endif
                            return;
                    }
                    break;

                default:
#if DEVELOPMENT_BUILD || TRACE
                    Debug.LogError($"blast.ssmd.handle_single_op_constant -> operation {op} not supported");
#endif
                    return;

            }

            // apply negate or not to result
            if (is_negated || not)
            {
                if (is_negated)
                {
                    constant = math.select(constant, -constant, is_negated);
                }
                else
                {
                    constant = math.select(0, 1, constant == 0);
                }
            }
        }

        private void handle_single_op_constant_to_register(float4* register, ref byte vector_size, float constant, blast_operation op, bool is_sequenced)
        {
            if (is_sequenced)
            {
                switch (vector_size)
                {
                    case 0:
                    case 4: handle_op_f4_f1(op, register, constant, ref vector_size); return;
                    case 3: handle_op_f3_f1(op, register, constant, ref vector_size); return;
                    case 2: handle_op_f2_f1(op, register, constant, ref vector_size); return;
                    case 1: handle_op_f1_f1(op, register, constant, ref vector_size); return;
                    default:
#if DEVELOPMENT_BUILD || TRACE
                        Debug.LogError($"blast.ssmd.handle_single_op_constant -> sequence operation {op} not supported");
#endif
                        return;

                }
            }
            else
            {
                // set all registers to the constant
                switch (vector_size)
                {
                    case 0:
                    case 4: simd.set_f4_from_constant(register, constant, ssmd_datacount); return;
                    case 1: simd.set_f1_from_constant(register, constant, ssmd_datacount); return;
                    case 2: simd.set_f2_from_constant(register, constant, ssmd_datacount); return;
                    case 3: simd.set_f3_from_constant(register, constant, ssmd_datacount); return;

                }
            }
        }

        private void handle_single_op_constant_to_target(float4* register, ref byte vector_size, float constant, blast_operation op, bool is_sequenced, in DATAREC target_rec)
        {
            if (!target_rec.is_set)
            {
#if DEVELOPMENT_BUILD || TRACE
                Debug.LogError($"blast.ssmd.handle_single_op_constant_to_target -> target not set");
#endif
                return;
            }

            if (is_sequenced)
            {
                // op is used in a sequenced operation (mul / add etc. ) 
                switch (vector_size)
                {
                    case 0:
                    case 4: handle_op_f4_f1(op, register, constant, ref vector_size, target_rec); break;
                    case 3: handle_op_f3_f1(op, register, constant, ref vector_size, target_rec); break;
                    case 2: handle_op_f2_f1(op, register, constant, ref vector_size, target_rec); break;
                    case 1: handle_op_f1_f1(op, register, constant, ref vector_size, target_rec); break;
                    default:
#if DEVELOPMENT_BUILD || TRACE
                        Debug.LogError($"blast.ssmd.handle_single_op_constant -> sequence operation {op} not supported");
#endif
                        return;

                }
            }
            else
            {
                // set all registers to the constant
                switch (vector_size)
                {
                    case 0:
                    case 4: simd.move_f1_constant_to_indexed_data_as_f4(target_rec.data, target_rec.row_size, target_rec.is_aligned, target_rec.index, constant, ssmd_datacount); break; 
                    case 1: simd.move_f1_constant_to_indexed_data_as_f1(target_rec.data, target_rec.row_size, target_rec.is_aligned, target_rec.index, constant, ssmd_datacount); break; 
                    case 2: simd.move_f1_constant_to_indexed_data_as_f2(target_rec.data, target_rec.row_size, target_rec.is_aligned, target_rec.index, constant, ssmd_datacount); break;
                    case 3: simd.move_f1_constant_to_indexed_data_as_f3(target_rec.data, target_rec.row_size, target_rec.is_aligned, target_rec.index, constant, ssmd_datacount); break;

                }
            }
        }


        private void handle_single_op_fn([NoAlias] float4* register, ref byte vector_size, [NoAlias] void** data, int source_index, in blast_operation op, in extended_blast_operation ex_op, in bool is_negated, in int indexed, in bool is_aligned, int stride)
        {
            // when indexed -> vectorsize is 1 element and we adjust index to point to it 
            if (indexed >= 0)
            {
                source_index += indexed;
                vector_size = 1;
            }

            switch (vector_size)
            {
                case 0:
                case 4:
                    switch (op)
                    {
                        // path when indexer is used as a function an not parsed inline 
                        case blast_operation.index_x:
                            if(!is_negated) simd.move_indexed_data_as_f1_to_f1f4(register, data, stride, is_aligned, source_index, ssmd_datacount);
                            else simd.move_indexed_data_as_f1_to_f1f4_negated(register, data, stride, is_aligned, source_index, ssmd_datacount); 
                            vector_size = 1;
                            return;

                        case blast_operation.index_y:
                            if (!is_negated) simd.move_indexed_data_as_f1_to_f1f4(register, data, stride, is_aligned, source_index + 1, ssmd_datacount);
                            else simd.move_indexed_data_as_f1_to_f1f4_negated(register, data, stride, is_aligned, source_index + 1, ssmd_datacount);
                            vector_size = 1;
                            return;

                        case blast_operation.index_z:
                            if (!is_negated) simd.move_indexed_data_as_f1_to_f1f4(register, data, stride, is_aligned, source_index + 2, ssmd_datacount);
                            else simd.move_indexed_data_as_f1_to_f1f4_negated(register, data, stride, is_aligned, source_index + 2, ssmd_datacount);
                            vector_size = 1;
                            return;

                        case blast_operation.index_w:
                            if (!is_negated) simd.move_indexed_data_as_f1_to_f1f4(register, data, stride, is_aligned, source_index + 3, ssmd_datacount);
                            else simd.move_indexed_data_as_f1_to_f1f4_negated(register, data, stride, is_aligned, source_index + 3, ssmd_datacount);
                            vector_size = 1;
                            return;

                        case blast_operation.abs:
                            for (int i = 0; i < ssmd_datacount; i++) register[i] = math.abs(f4(data, source_index, i));
                            return;

                        case blast_operation.trunc:
                            if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i] = math.trunc(f4(data, source_index, i));
                            else for (int i = 0; i < ssmd_datacount; i++) register[i] = math.trunc(-f4(data, source_index, i));
                            return;

                        case blast_operation.mina:
                            vector_size = 1;
                            if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i].x = math.cmin(f4(data, source_index, i));
                            else for (int i = 0; i < ssmd_datacount; i++) register[i].x = math.cmin(-f4(data, source_index, i));
                            return;

                        case blast_operation.maxa:
                            vector_size = 1;
                            if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i].x = math.cmax(f4(data, source_index, i));
                            else for (int i = 0; i < ssmd_datacount; i++) register[i].x = math.cmax(-f4(data, source_index, i));
                            return;

                        case blast_operation.csum:
                            vector_size = 1;
                            if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i].x = math.csum(f4(data, source_index, i));
                            else for (int i = 0; i < ssmd_datacount; i++) register[i].x = math.csum(-f4(data, source_index, i));
                            return;

                        case blast_operation.ex_op:
                            switch (ex_op)
                            {
                                case extended_blast_operation.sqrt:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i] = math.sqrt(f4(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i] = math.sqrt(-f4(data, source_index, i));
                                    return;

                                case extended_blast_operation.rsqrt:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i] = math.rsqrt(f4(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i] = math.rsqrt(-f4(data, source_index, i));
                                    return;

                                case extended_blast_operation.sin:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i] = math.sin(f4(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i] = math.sin(-f4(data, source_index, i));
                                    return;

                                case extended_blast_operation.cos:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i] = math.cos(f4(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i] = math.cos(-f4(data, source_index, i));
                                    return;

                                case extended_blast_operation.tan:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i] = math.tan(f4(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i] = math.tan(-f4(data, source_index, i));
                                    return;

                                case extended_blast_operation.sinh:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i] = math.sinh(f4(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i] = math.sinh(-f4(data, source_index, i));
                                    return;

                                case extended_blast_operation.cosh:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i] = math.cosh(f4(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i] = math.cosh(-f4(data, source_index, i));
                                    return;

                                case extended_blast_operation.atan:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i] = math.atan(f4(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i] = math.atan(-f4(data, source_index, i));
                                    return;
                                case extended_blast_operation.degrees:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i] = math.degrees(f4(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i] = math.degrees(-f4(data, source_index, i));
                                    return;

                                case extended_blast_operation.radians:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i] = math.radians(f4(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i] = math.radians(-f4(data, source_index, i));
                                    return;

                                case extended_blast_operation.ceil:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i] = math.ceil(f4(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i] = math.ceil(-f4(data, source_index, i));
                                    return;

                                case extended_blast_operation.floor:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i] = math.floor(f4(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i] = math.floor(-f4(data, source_index, i));
                                    return;

                                case extended_blast_operation.frac:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i] = math.frac(f4(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i] = math.frac(-f4(data, source_index, i));
                                    return;

                                case extended_blast_operation.normalize:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i] = math.normalize(f4(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i] = math.normalize(-f4(data, source_index, i));
                                    return;

                                case extended_blast_operation.saturate:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i] = math.saturate(f4(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i] = math.saturate(-f4(data, source_index, i));
                                    return;

                                case extended_blast_operation.logn:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i] = math.log(f4(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i] = math.log(-f4(data, source_index, i));
                                    return;

                                case extended_blast_operation.log10:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i] = math.log10(f4(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i] = math.log10(-f4(data, source_index, i));
                                    return;

                                case extended_blast_operation.log2:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i] = math.log2(f4(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i] = math.log2(-f4(data, source_index, i));
                                    return;

                                case extended_blast_operation.exp10:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i] = math.exp10(f4(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i] = math.exp10(-f4(data, source_index, i));
                                    return;

                                case extended_blast_operation.exp:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i] = math.exp(f4(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i] = math.exp(-f4(data, source_index, i));
                                    return;

                                case extended_blast_operation.ceillog2:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i] = math.ceillog2((int4)f4(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i] = math.ceillog2(-(int4)f4(data, source_index, i));
                                    return;

                                case extended_blast_operation.ceilpow2:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i] = math.ceilpow2((int4)f4(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i] = math.ceilpow2(-(int4)f4(data, source_index, i));
                                    return;

                                case extended_blast_operation.floorlog2:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i] = math.floorlog2((int4)f4(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i] = math.floorlog2(-(int4)f4(data, source_index, i));
                                    return;
                            }
                            break;


                    }
                    break;


                case 1:
                    switch (op)
                    {
                        case blast_operation.abs:
                            for (int i = 0; i < ssmd_datacount; i++) register[i].x = math.abs(((float*)data[i])[source_index]);
                            return;

                        case blast_operation.trunc:
                            if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i].x = math.trunc(f1(data, source_index, i));
                            else for (int i = 0; i < ssmd_datacount; i++) register[i].x = math.trunc(-f1(data, source_index, i));
                            return;

                        case blast_operation.csum:
                        case blast_operation.mina:
                        case blast_operation.maxa:
                            vector_size = 1;
                            if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i] = f1(data, source_index, i);
                            else for (int i = 0; i < ssmd_datacount; i++) register[i] = -f1(data, source_index, i);
                            return;

                        case blast_operation.ex_op:
                            switch (ex_op)
                            {
                                case extended_blast_operation.sqrt:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i].x = math.sqrt(f1(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i].x = math.sqrt(-f1(data, source_index, i));
                                    return;

                                case extended_blast_operation.rsqrt:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i].x = math.rsqrt(f1(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i].x = math.rsqrt(-f1(data, source_index, i));
                                    return;

                                case extended_blast_operation.sin:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i].x = math.sin(f1(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i].x = math.sin(-f1(data, source_index, i));
                                    return;

                                case extended_blast_operation.cos:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i].x = math.cos(f1(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i].x = math.cos(-f1(data, source_index, i));
                                    return;

                                case extended_blast_operation.tan:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i].x = math.tan(f1(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i].x = math.tan(-f1(data, source_index, i));
                                    return;

                                case extended_blast_operation.sinh:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i].x = math.sinh(f1(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i].x = math.sinh(-f1(data, source_index, i));
                                    return;

                                case extended_blast_operation.cosh:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i].x = math.cosh(f1(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i].x = math.cosh(-f1(data, source_index, i));
                                    return;

                                case extended_blast_operation.atan:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i].x = math.atan(f1(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i].x = math.atan(-f1(data, source_index, i));
                                    return;
                                case extended_blast_operation.degrees:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i].x = math.degrees(f1(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i].x = math.degrees(-f1(data, source_index, i));
                                    return;

                                case extended_blast_operation.radians:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i].x = math.radians(f1(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i].x = math.radians(-f1(data, source_index, i));
                                    return;

                                case extended_blast_operation.ceil:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i].x = math.ceil(f1(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i].x = math.ceil(-f1(data, source_index, i));
                                    return;

                                case extended_blast_operation.floor:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i].x = math.floor(f1(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i].x = math.floor(-f1(data, source_index, i));
                                    return;

                                case extended_blast_operation.frac:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i].x = math.frac(f1(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i].x = math.frac(-f1(data, source_index, i));
                                    return;

                                case extended_blast_operation.normalize:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i].x = f1(data, source_index, i);
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i].x = -f1(data, source_index, i);
                                    return;

                                case extended_blast_operation.saturate:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i].x = math.saturate(f1(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i].x = math.saturate(-f1(data, source_index, i));
                                    return;

                                case extended_blast_operation.logn:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i].x = math.log(f1(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i].x = math.log(-f1(data, source_index, i));
                                    return;

                                case extended_blast_operation.log10:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i].x = math.log10(f1(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i].x = math.log10(-f1(data, source_index, i));
                                    return;

                                case extended_blast_operation.log2:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i].x = math.log2(f1(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i].x = math.log2(-f1(data, source_index, i));
                                    return;

                                case extended_blast_operation.exp10:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i].x = math.exp10(f1(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i].x = math.exp10(-f1(data, source_index, i));
                                    return;

                                case extended_blast_operation.exp:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i].x = math.exp(f1(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i].x = math.exp(-f1(data, source_index, i));
                                    return;

                                case extended_blast_operation.ceillog2:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i].x = math.ceillog2((int)f1(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i].x = math.ceillog2(-(int)f1(data, source_index, i));
                                    return;

                                case extended_blast_operation.ceilpow2:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i].x = math.ceilpow2((int)f1(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i].x = math.ceilpow2(-(int)f1(data, source_index, i));
                                    return;

                                case extended_blast_operation.floorlog2:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i].x = math.floorlog2((int)f1(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i].x = math.floorlog2(-(int)f1(data, source_index, i));
                                    return;
                            }
                            break;

                    }
                    break;

                case 2:
                    switch (op)
                    {
                        // path when indexer is used as a function an not parsed inline 
                        case blast_operation.index_x:
                            // the index of float.x is the same as the index of float2.x in the index buffer 
                            if (!is_negated) simd.move_indexed_data_as_f1_to_f1f4(register, data, stride, is_aligned, source_index, ssmd_datacount);
                            else simd.move_indexed_data_as_f1_to_f1f4_negated(register, data, stride, is_aligned, source_index, ssmd_datacount);
                            //if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i].x = f2(data, source_index, i).x;
                            //else for (int i = 0; i < ssmd_datacount; i++) register[i].x = (-f2(data, source_index, i)).x;
                            vector_size = 1;
                            return;

                        case blast_operation.index_y:
                            if (!is_negated) simd.move_indexed_data_as_f1_to_f1f4(register, data, stride, is_aligned, source_index + 1, ssmd_datacount);
                            else simd.move_indexed_data_as_f1_to_f1f4_negated(register, data, stride, is_aligned, source_index + 1, ssmd_datacount);
                            //if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i].x = f2(data, source_index, i).y;
                            //else for (int i = 0; i < ssmd_datacount; i++) register[i].x = (-f2(data, source_index, i)).y;
                            vector_size = 1;
                            return;

                        case blast_operation.abs:
                            for (int i = 0; i < ssmd_datacount; i++) register[i].xy = math.abs(f2(data, source_index, i));
                            return;

                        case blast_operation.trunc:
                            if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i].xy = math.trunc(f2(data, source_index, i));
                            else for (int i = 0; i < ssmd_datacount; i++) register[i].xy = math.trunc(-f2(data, source_index, i));
                            return;

                        case blast_operation.mina:
                            vector_size = 1;
                            if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i].x = math.cmin(f2(data, source_index, i));
                            else for (int i = 0; i < ssmd_datacount; i++) register[i].x = math.cmin(-f2(data, source_index, i));
                            return;

                        case blast_operation.maxa:
                            vector_size = 1;
                            if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i].x = math.cmax(f2(data, source_index, i));
                            else for (int i = 0; i < ssmd_datacount; i++) register[i].x = math.cmax(-f2(data, source_index, i));
                            return;

                        case blast_operation.csum:
                            vector_size = 1;
                            if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i].x = math.csum(f2(data, source_index, i));
                            else for (int i = 0; i < ssmd_datacount; i++) register[i].x = math.csum(-f2(data, source_index, i));
                            return;


                        case blast_operation.ex_op:
                            switch (ex_op)
                            {
                                case extended_blast_operation.sqrt:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i].xy = math.sqrt(f2(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i].xy = math.sqrt(-f2(data, source_index, i));
                                    return;

                                case extended_blast_operation.rsqrt:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i].xy = math.rsqrt(f2(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i].xy = math.rsqrt(-f2(data, source_index, i));
                                    return;

                                case extended_blast_operation.sin:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i].xy = math.sin(f2(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i].xy = math.sin(-f2(data, source_index, i));
                                    return;

                                case extended_blast_operation.cos:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i].xy = math.cos(f2(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i].xy = math.cos(-f2(data, source_index, i));
                                    return;

                                case extended_blast_operation.tan:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i].xy = math.tan(f2(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i].xy = math.tan(-f2(data, source_index, i));
                                    return;

                                case extended_blast_operation.sinh:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i].xy = math.sinh(f2(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i].xy = math.sinh(-f2(data, source_index, i));
                                    return;

                                case extended_blast_operation.cosh:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i].xy = math.cosh(f2(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i].xy = math.cosh(-f2(data, source_index, i));
                                    return;

                                case extended_blast_operation.atan:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i].xy = math.atan(f2(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i].xy = math.atan(-f2(data, source_index, i));
                                    return;
                                case extended_blast_operation.degrees:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i].xy = math.degrees(f2(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i].xy = math.degrees(-f2(data, source_index, i));
                                    return;

                                case extended_blast_operation.radians:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i].xy = math.radians(f2(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i].xy = math.radians(-f2(data, source_index, i));
                                    return;

                                case extended_blast_operation.ceil:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i].xy = math.ceil(f2(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i].xy = math.ceil(-f2(data, source_index, i));
                                    return;

                                case extended_blast_operation.floor:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i].xy = math.floor(f2(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i].xy = math.floor(-f2(data, source_index, i));
                                    return;

                                case extended_blast_operation.frac:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i].xy = math.frac(f2(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i].xy = math.frac(-f2(data, source_index, i));
                                    return;

                                case extended_blast_operation.normalize:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i].xy = math.normalize(f2(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i].xy = math.normalize(-f2(data, source_index, i));
                                    return;

                                case extended_blast_operation.saturate:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i].xy = math.saturate(f2(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i].xy = math.saturate(-f2(data, source_index, i));
                                    return;

                                case extended_blast_operation.logn:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i].xy = math.log(f2(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i].xy = math.log(-f2(data, source_index, i));
                                    return;

                                case extended_blast_operation.log10:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i].xy = math.log10(f2(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i].xy = math.log10(-f2(data, source_index, i));
                                    return;

                                case extended_blast_operation.log2:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i].xy = math.log2(f2(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i].xy = math.log2(-f2(data, source_index, i));
                                    return;

                                case extended_blast_operation.exp10:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i].xy = math.exp10(f2(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i].xy = math.exp10(-f2(data, source_index, i));
                                    return;

                                case extended_blast_operation.exp:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i].xy = math.exp(f2(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i].xy = math.exp(-f2(data, source_index, i));
                                    return;

                                case extended_blast_operation.ceillog2:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i].xy = math.ceillog2((int2)f2(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i].xy = math.ceillog2(-(int2)f2(data, source_index, i));
                                    return;

                                case extended_blast_operation.ceilpow2:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i].xy = math.ceilpow2((int2)f2(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i].xy = math.ceilpow2(-(int2)f2(data, source_index, i));
                                    return;

                                case extended_blast_operation.floorlog2:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i].xy = math.floorlog2((int2)f2(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i].xy = math.floorlog2(-(int2)f2(data, source_index, i));
                                    return;
                            }
                            break;

                    }
                    break;


                case 3:
                    switch (op)
                    {
                        // path when indexer is used as a function an not parsed inline 
                        case blast_operation.index_x:
                            if (!is_negated) simd.move_indexed_data_as_f1_to_f1f4(register, data, stride, is_aligned, source_index, ssmd_datacount);
                            else simd.move_indexed_data_as_f1_to_f1f4_negated(register, data, stride, is_aligned, source_index, ssmd_datacount);
                            //if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i].x = f3(data, source_index, i).x;
                            //else for (int i = 0; i < ssmd_datacount; i++) register[i].x = (-f4(data, source_index, i)).x;
                            vector_size = 1;
                            return;

                        case blast_operation.index_y:
                            if (!is_negated) simd.move_indexed_data_as_f1_to_f1f4(register, data, stride, is_aligned, source_index + 1, ssmd_datacount);
                            else simd.move_indexed_data_as_f1_to_f1f4_negated(register, data, stride, is_aligned, source_index + 1, ssmd_datacount);
                            //if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i].x = f3(data, source_index, i).y;
                            //else for (int i = 0; i < ssmd_datacount; i++) register[i].x = (-f3(data, source_index, i)).y;
                            vector_size = 1;
                            return;

                        case blast_operation.index_z:
                            if (!is_negated) simd.move_indexed_data_as_f1_to_f1f4(register, data, stride, is_aligned, source_index + 2, ssmd_datacount);
                            else simd.move_indexed_data_as_f1_to_f1f4_negated(register, data, stride, is_aligned, source_index + 2, ssmd_datacount);
                            vector_size = 1;
                            return;

                        case blast_operation.abs:
                            for (int i = 0; i < ssmd_datacount; i++) register[i].xyz = math.abs(f3(data, source_index, i));
                            return;

                        case blast_operation.trunc:
                            if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i].xyz = math.trunc(f3(data, source_index, i));
                            else for (int i = 0; i < ssmd_datacount; i++) register[i].xyz = math.trunc(-f3(data, source_index, i));
                            return;

                        case blast_operation.mina:
                            vector_size = 1;
                            if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i].xy = math.cmin(f3(data, source_index, i));
                            else for (int i = 0; i < ssmd_datacount; i++) register[i].x = math.cmin(-f3(data, source_index, i));
                            return;

                        case blast_operation.maxa:
                            vector_size = 1;
                            if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i].x = math.cmax(f3(data, source_index, i));
                            else for (int i = 0; i < ssmd_datacount; i++) register[i].x = math.cmax(-f3(data, source_index, i));
                            return;

                        case blast_operation.csum:
                            vector_size = 1;
                            if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i].x = math.csum(f3(data, source_index, i));
                            else for (int i = 0; i < ssmd_datacount; i++) register[i].x = math.csum(-f3(data, source_index, i));
                            return;


                        case blast_operation.ex_op:
                            switch (ex_op)
                            {
                                case extended_blast_operation.sqrt:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i].xyz = math.sqrt(f3(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i].xyz = math.sqrt(-f3(data, source_index, i));
                                    return;

                                case extended_blast_operation.rsqrt:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i].xyz = math.rsqrt(f3(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i].xyz = math.rsqrt(-f3(data, source_index, i));
                                    return;

                                case extended_blast_operation.sin:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i].xyz = math.sin(f3(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i].xyz = math.sin(-f3(data, source_index, i));
                                    return;

                                case extended_blast_operation.cos:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i].xyz = math.cos(f3(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i].xyz = math.cos(-f3(data, source_index, i));
                                    return;

                                case extended_blast_operation.tan:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i].xyz = math.tan(f3(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i].xyz = math.tan(-f3(data, source_index, i));
                                    return;

                                case extended_blast_operation.sinh:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i].xyz = math.sinh(f3(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i].xyz = math.sinh(-f3(data, source_index, i));
                                    return;

                                case extended_blast_operation.cosh:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i].xyz = math.cosh(f3(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i].xyz = math.cosh(-f3(data, source_index, i));
                                    return;

                                case extended_blast_operation.atan:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i].xyz = math.atan(f3(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i].xyz = math.atan(-f3(data, source_index, i));
                                    return;
                                case extended_blast_operation.degrees:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i].xyz = math.degrees(f3(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i].xyz = math.degrees(-f3(data, source_index, i));
                                    return;

                                case extended_blast_operation.radians:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i].xyz = math.radians(f3(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i].xyz = math.radians(-f3(data, source_index, i));
                                    return;

                                case extended_blast_operation.ceil:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i].xyz = math.ceil(f3(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i].xyz = math.ceil(-f3(data, source_index, i));
                                    return;

                                case extended_blast_operation.floor:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i].xyz = math.floor(f3(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i].xyz = math.floor(-f3(data, source_index, i));
                                    return;

                                case extended_blast_operation.frac:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i].xyz = math.frac(f3(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i].xyz = math.frac(-f3(data, source_index, i));
                                    return;

                                case extended_blast_operation.normalize:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i].xyz = math.normalize(f3(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i].xyz = math.normalize(-f3(data, source_index, i));
                                    return;

                                case extended_blast_operation.saturate:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i].xyz = math.saturate(f3(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i].xyz = math.saturate(-f3(data, source_index, i));
                                    return;

                                case extended_blast_operation.logn:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i].xyz = math.log(f3(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i].xyz = math.log(-f3(data, source_index, i));
                                    return;

                                case extended_blast_operation.log10:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i].xyz = math.log10(f3(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i].xyz = math.log10(-f3(data, source_index, i));
                                    return;

                                case extended_blast_operation.log2:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i].xyz = math.log2(f3(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i].xyz = math.log2(-f3(data, source_index, i));
                                    return;

                                case extended_blast_operation.exp10:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i].xyz = math.exp10(f3(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i].xyz = math.exp10(-f3(data, source_index, i));
                                    return;

                                case extended_blast_operation.exp:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i].xyz = math.exp(f3(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i].xyz = math.exp(-f3(data, source_index, i));
                                    return;

                                case extended_blast_operation.ceillog2:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i].xyz = math.ceillog2((int3)f3(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i].xyz = math.ceillog2(-(int3)f3(data, source_index, i));
                                    return;

                                case extended_blast_operation.ceilpow2:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i].xyz = math.ceilpow2((int3)f3(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i].xyz = math.ceilpow2(-(int3)f3(data, source_index, i));
                                    return;

                                case extended_blast_operation.floorlog2:
                                    if (!is_negated) for (int i = 0; i < ssmd_datacount; i++) register[i].xyz = math.floorlog2((int3)f3(data, source_index, i));
                                    else for (int i = 0; i < ssmd_datacount; i++) register[i].xyz = math.floorlog2(-(int3)f3(data, source_index, i));
                                    return;
                            }
                            break;

                    }
                    break;
            }

            // reaching here should be an error

#if DEVELOPMENT_BUILD || TRACE
            Debug.LogError($"blast.ssmd.handle_single_op_fn -> operation {op}, extended: {ex_op} not supported on vectorsize {vector_size}");
#endif

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


        /// <summary>
        /// get result from a single input function
        /// - the parameter fed may be negated, indexed and can be an inlined constant 
        /// - the codepointer must be on the first value to read after op|function opcodes
        /// </summary>
        void get_single_op_result([NoAlias] void* temp, ref int code_pointer, ref byte vector_size, [NoAlias] float4* register, in blast_operation op, in extended_blast_operation ex_op = extended_blast_operation.nop, int indexed = -1, bool is_negated = false)
        {
            // is negated?
            byte code_byte = code[code_pointer];

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

                    case blast_operation.index_x: indexed = 0; code_pointer++; code_byte = code[code_pointer]; continue;
                    case blast_operation.index_y: indexed = 1; code_pointer++; code_byte = code[code_pointer]; continue;
                    case blast_operation.index_z: indexed = 2; code_pointer++; code_byte = code[code_pointer]; continue;
                    case blast_operation.index_w: indexed = 3; code_pointer++; code_byte = code[code_pointer]; continue;

                    case blast_operation.constant_f1:
                        {
                            // handle operation with inlined constant data 
                            float constant = default;
                            byte* p = (byte*)(void*)&constant;
                            {
                                p[0] = code[code_pointer + 1];
                                p[1] = code[code_pointer + 2];
                                p[2] = code[code_pointer + 3];
                                p[3] = code[code_pointer + 4];
                            }
                            code_pointer += 4;
                            handle_single_op_constant(register, ref vector_size, constant, op, ex_op, is_negated);
                        }
                        return;

                    case blast_operation.constant_f1_h:
                        {
                            // handle operation with inlined constant data 
                            float constant = default;
                            byte* p = (byte*)(void*)&constant;
                            {
                                p[0] = 0;
                                p[1] = 0;
                                p[2] = code[code_pointer + 1];
                                p[3] = code[code_pointer + 2];
                            }
                            code_pointer += 2;
                            handle_single_op_constant(register, ref vector_size, constant, op, ex_op, is_negated);
                        }
                        return;

                    case blast_operation.constant_short_ref:
                        {
                            int c_index = code_pointer - code[code_pointer + 1];
                            code_pointer++;

                            float constant = default;
                            byte* p = (byte*)(void*)&constant;

                            if (code[c_index + 1] == (byte)blast_operation.constant_f1)
                            {
                                p[0] = code[c_index + 2];
                                p[1] = code[c_index + 3];
                                p[2] = code[c_index + 4];
                                p[3] = code[c_index + 5];
                            }
                            else
                            {
                                // 16 bit encoding
                                p[0] = 0;
                                p[1] = 0;
                                p[2] = code[c_index + 2];
                                p[3] = code[c_index + 3];
                            }

                            handle_single_op_constant(register, ref vector_size, constant, op, ex_op, is_negated);
                        }
                        return;

                    case blast_operation.constant_long_ref:
                        {
                            int c_index = code_pointer - ((code[code_pointer] << 8) + code[code_pointer + 1]) + 1;
                            code_pointer += 2;

                            float constant = default;
                            byte* p = (byte*)(void*)&constant;

                            if (code[c_index + 1] == (byte)blast_operation.constant_f1)
                            {
                                p[0] = code[c_index + 2];
                                p[1] = code[c_index + 3];
                                p[2] = code[c_index + 4];
                                p[3] = code[c_index + 5];
                            }
                            else
                            {
                                // 16 bit encoding
                                p[0] = 0;
                                p[1] = 0;
                                p[2] = code[c_index + 2];
                                p[3] = code[c_index + 3];
                            }

                            handle_single_op_constant(register, ref vector_size, constant, op, ex_op, is_negated);
                        }
                        return;

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
                            handle_single_op_constant(register, ref vector_size, constant, op, ex_op, is_negated);
                            code_pointer++;
                        }
                        return;

                    case blast_operation.pop:
                        {
                            //datatype = BlastInterpretor.GetMetaDataType(metadata, (byte)(stack_offset - 1));
                            vector_size = BlastInterpretor.GetMetaDataSize(metadata, (byte)(stack_offset - 1));
                            vector_size = (byte)math.select(vector_size, 4, vector_size == 0);
                            stack_offset = stack_offset - vector_size;
                            handle_single_op_fn(register, ref vector_size, stack, stack_offset, op, ex_op, is_negated, indexed, stack_is_aligned, stack_rowsize);
                        }
                        return;

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
                                return;
                            }

                            // datatype = BlastInterpretor.GetMetaDataType(metadata, (byte)(source_index));
                            vector_size = BlastInterpretor.GetMetaDataSize(metadata, (byte)(source_index));
                            vector_size = (byte)math.select(vector_size, 4, vector_size == 0);

                            handle_single_op_fn(register, ref vector_size, data, source_index, op, ex_op, is_negated, indexed, data_is_aligned, data_rowsize);
                        }
                        return;
                }
            }
        }



        //
        // Although for all of these we could use functionpointers the compiler will have a hard time
        // vectorizing most inner loops if the use functionpointers instead of the known math library for
        // which it has buildin support
        //
        // -> we could use some form of codegen in future version but these will not really change much i guess
        //


        #endregion

        #region Dual input functions: math.pow fmod cross etc. 

#if !STANDALONE_VSBUILD
        [SkipLocalsInit]
#endif
        void get_fmod_result([NoAlias] void* temp, ref int code_pointer, ref byte vector_size, [NoAlias] float4* register)
        {
            BlastVariableDataType datatype;
            pop_op_meta_cp(code_pointer, out datatype, out vector_size);

#if DEVELOPMENT_BUILD || TRACE
            if (datatype != BlastVariableDataType.Numeric)
            {
                Debug.LogError($"blast.ssmd.interpretor.fmod: datatype|vectorsize mismatch, vector_size: {vector_size}, datatype: {datatype} not supported");
                return;
            }
#endif

            switch (vector_size)
            {
                case 1:
                    {
                        float* fpa = (float*)temp;
                        float* fpb = &fpa[ssmd_datacount];

                        pop_fx_into_ref<float>(ref code_pointer, fpa, ssmd_datacount);   // _ref increases code_pointer according to data
                        pop_fx_into_ref<float>(ref code_pointer, fpb);   // _ref increases code_pointer according to data

                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            register[i].x = math.fmod(fpa[i], fpb[i]);
                        }
                    }
                    break;

                case 2:
                    {
                        float2* fpa = (float2*)temp;
                        float2* fpb = &fpa[ssmd_datacount];

                        pop_fx_into_ref<float2>(ref code_pointer, fpa);
                        pop_fx_into_ref<float2>(ref code_pointer, fpb);

                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            register[i].xy = math.fmod(fpa[i], fpb[i]);
                        }
                    }
                    break;

                case 3:
                    {
                        float3* fpa = (float3*)temp;
                        float3* fpb = stackalloc float3[ssmd_datacount]; // TODO test if this does not result in 2 x allocation in every case.,,..

                        pop_fx_into_ref<float3>(ref code_pointer, fpa);
                        pop_fx_into_ref<float3>(ref code_pointer, fpb);

                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            register[i].xyz = math.fmod(fpa[i], fpb[i]);
                        }
                    }
                    break;

                case 0:
                case 4:
                    {
                        vector_size = 4;

                        float4* fpa = (float4*)temp;
                        float4* fpb = stackalloc float4[ssmd_datacount]; // TODO test if this does not result in 2 x allocation in every case.,,..

                        pop_fx_into_ref<float4>(ref code_pointer, fpa);
                        pop_fx_into_ref<float4>(ref code_pointer, fpb);

                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            register[i] = math.fmod(fpa[i], fpb[i]);
                        }
                    }
                    break;

#if DEVELOPMENT_BUILD || TRACE
                default:
                    Debug.LogError($"blast.ssmd.interpretor.fmod: datatype|vectorsize mismatch, vector_size: {vector_size}, datatype: {datatype} not supported");
                    break;
#endif
            }


            // pop into ref ends at the next byte, but we want functions to end on the last processed byte
            code_pointer--;
        }

#if !STANDALONE_VSBUILD
        [SkipLocalsInit]
#endif
        void get_pow_result([NoAlias] void* temp, ref int code_pointer, ref byte vector_size, [NoAlias] float4* register)
        {
            BlastVariableDataType datatype;

            //
            // get metadata of next variable/constant minding any substract or indexing operator 
            //
            pop_op_meta_cp(code_pointer, out datatype, out vector_size);

#if DEVELOPMENT_BUILD || TRACE
            if (datatype != BlastVariableDataType.Numeric)
            {
                Debug.LogError($"blast.ssmd.interpretor.pow: datatype|vectorsize mismatch, vector_size: {vector_size}, datatype: {datatype} not supported");
                return;
            }
#endif

            switch (vector_size)
            {
                case 1:
                    {
                        float* fpa = (float*)temp;
                        float* fpb = &fpa[ssmd_datacount];

                        pop_fx_into_ref<float>(ref code_pointer, fpa);
                        pop_fx_into_ref<float>(ref code_pointer, fpb);

                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            register[i].x = math.pow(fpa[i], fpb[i]);
                        }
                    }
                    break;

                case 2:
                    {
                        float2* fpa = (float2*)temp;
                        float2* fpb = &fpa[ssmd_datacount];

                        pop_fx_into_ref<float2>(ref code_pointer, fpa);
                        pop_fx_into_ref<float2>(ref code_pointer, fpb);

                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            register[i].xy = math.pow(fpa[i], fpb[i]);
                        }
                    }
                    break;

                case 3:
                    {
                        float3* fpa = (float3*)temp;
                        float3* fpb = stackalloc float3[ssmd_datacount]; // TODO test if this does not result in 2 x allocation in every case.,,..

                        pop_fx_into_ref<float3>(ref code_pointer, fpa);
                        pop_fx_into_ref<float3>(ref code_pointer, fpb);

                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            register[i].xyz = math.pow(fpa[i], fpb[i]);
                        }
                    }
                    break;

                case 0:
                case 4:
                    {
                        vector_size = 4;

                        float4* fpa = (float4*)temp;
                        float4* fpb = stackalloc float4[ssmd_datacount]; // TODO test if this does not result in 2 x allocation in every case.,,..

                        pop_fx_into_ref<float4>(ref code_pointer, fpa);
                        pop_fx_into_ref<float4>(ref code_pointer, fpb);

                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            register[i] = math.pow(fpa[i], fpb[i]);
                        }
                    }
                    break;

#if DEVELOPMENT_BUILD || TRACE
                default:
                    Debug.LogError($"blast.ssmd.interpretor.pow: datatype|vectorsize mismatch, vector_size: {vector_size}, datatype: {datatype} not supported");
                    break;
#endif
            }

            code_pointer--;
        }

        /// <summary>
        /// only accepts size 3 vectors 
        /// </summary>
#if !STANDALONE_VSBUILD
        [SkipLocalsInit]
#endif
        void get_cross_result([NoAlias] void* temp, ref int code_pointer, ref byte vector_size, [NoAlias] float4* register)
        {
            BlastVariableDataType datatype;
            pop_op_meta_cp(code_pointer, out datatype, out vector_size);

#if DEVELOPMENT_BUILD || TRACE
            if (datatype != BlastVariableDataType.Numeric || vector_size != 3)
            {
                Debug.LogError($"blast.ssmd.interpretor.cross: datatype|vectorsize mismatch, vector_size: {vector_size}, datatype: {datatype} not supported");
                return;
            }
#endif

            switch (vector_size)
            {
                case 3:
                    {
                        float3* fpa = (float3*)temp;
                        float3* fpb = stackalloc float3[ssmd_datacount];

                        pop_fx_into_ref<float3>(ref code_pointer, fpa);
                        pop_fx_into_ref<float3>(ref code_pointer, fpb);

                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            register[i].xyz = math.cross(fpa[i], fpb[i]);
                        }
                    }
                    break;

#if DEVELOPMENT_BUILD || TRACE
                default:
                    Debug.LogError($"blast.ssmd.interpretor.pow: datatype|vectorsize mismatch, vector_size: {vector_size}, datatype: {datatype} not supported");
                    break;
#endif
            }

            code_pointer--;

        }


        /// <summary>
        /// result vector size == 1 in all cases
        /// </summary>
        /// <param name="temp"></param>
        /// <param name="code_pointer"></param>
        /// <param name="vector_size"></param>
        /// <param name="register"></param>
#if !STANDALONE_VSBUILD
        [SkipLocalsInit]
#endif
        void get_dot_result([NoAlias] void* temp, ref int code_pointer, ref byte vector_size, [NoAlias] float4* register)
        {
            BlastVariableDataType datatype;
            pop_op_meta_cp(code_pointer, out datatype, out vector_size);

#if DEVELOPMENT_BUILD || TRACE
            if (datatype != BlastVariableDataType.Numeric)
            {
                Debug.LogError($"blast.ssmd.interpretor.dot: datatype|vectorsize mismatch, vector_size: {vector_size}, datatype: {datatype} not supported");
                return;
            }
#endif
            switch (vector_size)
            {
                case 1:
                    {
                        float* fpa = (float*)temp;
                        float* fpb = &fpa[ssmd_datacount];

                        pop_fx_into_ref<float>(ref code_pointer, fpa);
                        pop_fx_into_ref<float>(ref code_pointer, fpb);

                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            register[i].x = math.dot(fpa[i], fpb[i]);
                        }
                    }
                    break;

                case 2:
                    {
                        float2* fpa = (float2*)temp;
                        float2* fpb = &fpa[ssmd_datacount];

                        pop_fx_into_ref<float2>(ref code_pointer, fpa);
                        pop_fx_into_ref<float2>(ref code_pointer, fpb);

                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            register[i].x = math.dot(fpa[i], fpb[i]);
                        }
                    }
                    break;

                case 3:
                    {
                        float3* fpa = (float3*)temp;
                        float3* fpb = stackalloc float3[ssmd_datacount];

                        pop_fx_into_ref<float3>(ref code_pointer, fpa);
                        pop_fx_into_ref<float3>(ref code_pointer, fpb);

                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            register[i].x = math.dot(fpa[i], fpb[i]);
                        }
                    }
                    break;

                case 0:
                case 4:
                    {
                        float4* fpa = (float4*)temp;
                        float4* fpb = stackalloc float4[ssmd_datacount];

                        pop_fx_into_ref<float4>(ref code_pointer, fpa);
                        pop_fx_into_ref<float4>(ref code_pointer, fpb);

                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            register[i].x = math.dot(fpa[i], fpb[i]);
                        }
                    }
                    break;

#if DEVELOPMENT_BUILD || TRACE
                default:
                    Debug.LogError($"blast.ssmd.interpretor.dot: datatype|vectorsize mismatch, vector_size: {vector_size}, datatype: {datatype} not supported");
                    break;
#endif
            }

            code_pointer--;
            vector_size = 1;
        }

#if !STANDALONE_VSBUILD
        [SkipLocalsInit]
#endif
        void get_atan2_result([NoAlias] void* temp, ref int code_pointer, ref byte vector_size, [NoAlias] float4* register)
        {
            BlastVariableDataType datatype;
            pop_op_meta_cp(code_pointer, out datatype, out vector_size);

#if DEVELOPMENT_BUILD || TRACE
            if (datatype != BlastVariableDataType.Numeric)
            {
                Debug.LogError($"blast.ssmd.interpretor.dot: datatype|vectorsize mismatch, vector_size: {vector_size}, datatype: {datatype} not supported");
                return;
            }
#endif
            switch (vector_size)
            {
                case 1:
                    {
                        float* fpa = (float*)temp;
                        float* fpb = &fpa[ssmd_datacount];

                        pop_fx_into_ref<float>(ref code_pointer, fpa);
                        pop_fx_into_ref<float>(ref code_pointer, fpb);

                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            register[i].x = math.atan2(fpa[i], fpb[i]);
                        }
                    }
                    break;

                case 2:
                    {
                        float2* fpa = (float2*)temp;
                        float2* fpb = &fpa[ssmd_datacount];

                        pop_fx_into_ref<float2>(ref code_pointer, fpa);
                        pop_fx_into_ref<float2>(ref code_pointer, fpb);

                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            register[i].xy = math.atan2(fpa[i], fpb[i]);
                        }
                    }
                    break;

                case 3:
                    {
                        float3* fpa = (float3*)temp;
                        float3* fpb = stackalloc float3[ssmd_datacount];

                        pop_fx_into_ref<float3>(ref code_pointer, fpa);
                        pop_fx_into_ref<float3>(ref code_pointer, fpb);

                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            register[i].xyz = math.atan2(fpa[i], fpb[i]);
                        }
                    }
                    break;

                case 0:
                case 4:
                    {
                        float4* fpa = (float4*)temp;
                        float4* fpb = stackalloc float4[ssmd_datacount];

                        pop_fx_into_ref<float4>(ref code_pointer, fpa);
                        pop_fx_into_ref<float4>(ref code_pointer, fpb);

                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            register[i] = math.atan2(fpa[i], fpb[i]);
                        }
                    }
                    break;

#if DEVELOPMENT_BUILD || TRACE
                default:
                    Debug.LogError($"blast.ssmd.interpretor.atan2: datatype|vectorsize mismatch, vector_size: {vector_size}, datatype: {datatype} not supported");
                    break;
#endif
            }

            code_pointer--;
            vector_size = 1;
        }



        #endregion

        #region Tripple inputs: select, clamp, lerp etc

        /// <summary>
        /// select first input on a false condition, select the second on a true condition that is put in parameter 3
        /// </summary>
        /// <remarks>
        /// 3 inputs, first 2 any type, 3rd type equal or scalar bool
        /// 
        /// c[x] = select(a[x], b[x], condition[1/x])
        /// 
        /// - input = outputvectorsize
        /// - 1 output  
        /// 
        /// Assembly for scalar constants<c>a = select(1, 2, 1) => setf a select 1 2 1 nop</c>
        /// 
        /// Assembly sample with vectors: <c>a = select((1 1), (2 2), 1) => setf a select 1 pop pop nop</c>
        /// </remarks>
#if !STANDALONE_VSBUILD
        [SkipLocalsInit]
#endif
        void get_select_result([NoAlias] void* temp, ref int code_pointer, ref byte vector_size, [NoAlias] float4* f4)
        {
            // upon entry code_pointer is located on the function operation 
            BlastVariableDataType datatype;
            byte vector_size_condition;

            //
            // need to reorder parameters while compiling for better memory efficienty executing it 
            // - if we can execute the condition first we can skip reading 1 of the parameters (we do need to 
            //   move stackpointers though)
            // - compiler update for SSMD only? or also normal?
            // 

            // peek vectorsize of first option
            pop_op_meta_cp(code_pointer, out datatype, out vector_size);

            //
            // IMPORTANT
            // stack allocations are moved to the start of the function and are always done,
            // regardless conditionals, avoid allocating too much memory by calculating the amount needed 
            // and casting the resulting pointers in each vectorsize-case
            //
            int data_byte_size = 4 * ssmd_datacount * vector_size;

            // we could use memory from f4 to store 1 array, but that would probably mess up vectorizing the inner loops
            byte* local1 = stackalloc byte[data_byte_size];
            byte* local2 = stackalloc byte[data_byte_size];

            //
            // allocations could also be avoided, with each combination (constant/pop/var) and each vectorsize of each parameter in a seperate case... 
            // this will happen a lot as generics is unusable.. codegen? TODO
            //

            switch (vector_size)
            {

                case 1:

                    float* o1 = (float*)(void*)local1;
                    float* o2 = (float*)(void*)local2;

                    pop_fx_into_ref<float>(ref code_pointer, o1);
                    pop_fx_into_ref<float>(ref code_pointer, o2);
                    pop_fx_into_ref<float>(ref code_pointer, (float*)temp);

                    for (int i = 0; i < ssmd_datacount; i++)
                    {
                        // not using f4 for storing any operand should allow for the compiler to optimize better
                        // we could also optimize this by handling 4 at once in a float4.. TODO
                        f4[i].x = math.select(o1[i], o2[i], ((float*)temp)[i] != 0);
                    }
                    break;


                case 2:
                    float2* o21 = (float2*)(void*)local1;
                    float2* o22 = (float2*)(void*)local2;

                    pop_fx_into_ref<float2>(ref code_pointer, o21);
                    pop_fx_into_ref<float2>(ref code_pointer, o22);

                    pop_op_meta_cp(code_pointer, out datatype, out vector_size_condition);

                    if (vector_size_condition == 1)
                    {
                        pop_fx_into_ref<float>(ref code_pointer, (float*)temp);
                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            f4[i].xy = math.select(o21[i], o22[i], ((float*)temp)[i] != 0);
                        }
                    }
                    else
                    {
                        pop_fx_into_ref<float2>(ref code_pointer, (float2*)temp);
                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            f4[i].xy = math.select(o21[i], o22[i], ((float2*)temp)[i] != 0);
                        }
                    }
                    break;

                case 3:
                    float3* o31 = (float3*)(void*)local1;
                    float3* o32 = (float3*)(void*)local2;

                    pop_fx_into_ref<float3>(ref code_pointer, o31);
                    pop_fx_into_ref<float3>(ref code_pointer, o32);

                    pop_op_meta_cp(code_pointer, out datatype, out vector_size_condition);

                    if (vector_size_condition == 1)
                    {
                        pop_fx_into_ref<float>(ref code_pointer, (float*)temp);
                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            f4[i].xyz = math.select(o31[i], o32[i], ((float*)temp)[i] != 0);
                        }
                    }
                    else
                    {
                        pop_fx_into_ref<float3>(ref code_pointer, (float3*)temp);
                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            f4[i].xyz = math.select(o31[i], o32[i], ((float3*)temp)[i] != 0);
                        }
                    }
                    break;

                case 0:
                case 4:
                    {
                        vector_size = 4;

                        float4* o41 = (float4*)(void*)local1;
                        float4* o42 = (float4*)(void*)local2;

                        pop_fx_into_ref<float4>(ref code_pointer, o41);
                        pop_fx_into_ref<float4>(ref code_pointer, o42);
                        pop_op_meta_cp(code_pointer, out datatype, out vector_size_condition);

                        if (vector_size_condition == 1)
                        {
                            pop_fx_into_ref<float>(ref code_pointer, (float*)temp);
                            for (int i = 0; i < ssmd_datacount; i++)
                            {
                                f4[i] = math.select(o41[i], o42[i], ((float*)temp)[i] != 0);
                            }
                        }
                        else
                        {
                            pop_fx_into_ref<float4>(ref code_pointer, (float4*)temp);
                            for (int i = 0; i < ssmd_datacount; i++)
                            {
                                f4[i] = math.select(o41[i], o42[i], ((float4*)temp)[i] != 0);
                            }
                        }
                    }
                    break;

#if DEVELOPMENT_BUILD || TRACE
                default:
                    Debug.LogError($"blast.ssmd.interpretor.select: datatype|vectorsize mismatch, vector_size: {vector_size}, datatype: {datatype} not supported");
                    break;
#endif
            }

            code_pointer--;
        }


        /// <summary>
        /// clamp value between operands
        /// </summary>
        /// <remarks>
        /// first min then max => saves 1 memory buffer doing this in sequence (math lib will also do min(max( so this should be equal in performance but turned upside down in flow
        /// <code>
        /// 
        /// a = clamp(a, min, max); 
        /// 
        /// </code>
        /// </remarks>
#if !STANDALONE_VSBUILD
        [SkipLocalsInit]
#endif
        void get_clamp_result([NoAlias] void* temp, ref int code_pointer, ref byte vector_size, [NoAlias] float4* f4)
        {
            BlastVariableDataType datatype;
            BlastVariableDataType datatype2;
            byte vector_size_2;

            pop_op_meta_cp(code_pointer, out datatype, out vector_size);

#if DEVELOPMENT_BUILD || TRACE
            if (datatype != BlastVariableDataType.Numeric)
            {
                Debug.LogError($"blast.ssmd.interpretor.clamp: datatype|vectorsize mismatch, vector_size: {vector_size}, datatype: {datatype} not supported");
                return;
            }
#endif

            switch (vector_size)
            {
                case 1:
                    {
                        // in case of v1 there is no memory problem 
                        float* fpa = (float*)temp;
                        float* fp_min = &fpa[ssmd_datacount];
                        float* fp_max = &fp_min[ssmd_datacount];

                        pop_fx_into_ref<float>(ref code_pointer, fpa);
                        pop_fx_into_ref<float>(ref code_pointer, fp_min);
                        pop_fx_into_ref<float>(ref code_pointer, fp_max);

                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            register[i].x = math.clamp(fpa[i], fp_min[i], fp_max[i]);
                        }
                    }
                    break;

                case 2:
                    {
                        // 2 float2's fit in temp, split op in 2 to save on the allocation
                        float2* fpa = (float2*)temp;

                        pop_fx_into_ref<float2>(ref code_pointer, fpa);


                        // first the lower bound 
                        pop_op_meta_cp(code_pointer, out datatype2, out vector_size_2);
                        // must be size 1 or 2 
#if DEVELOPMENT_BUILD || TRACE
                        if (vector_size_2 != 1 && vector_size_2 != 2)
                        {
                            Debug.LogError($"blast.ssmd.interpretor.clamp: datatype|vectorsize mismatch, vector_size: {vector_size}, datatype: {datatype}, minmax vectorsize {vector_size_2} not supported");
                            return;
                        }
                        if (datatype != datatype2)
                        {
                            Debug.LogError($"blast.ssmd.interpretor.clamp: datatype|vectorsize mismatch, vector_size: {vector_size}, datatype: {datatype}, minmax datatype = {datatype2}");
                            return;
                        }
#endif
                        if (vector_size_2 == 2)
                        {
                            float2* fp_m = &fpa[ssmd_datacount];
                            pop_fx_into_ref<float2>(ref code_pointer, fp_m);

                            // constrain lower bound 
                            for (int i = 0; i < ssmd_datacount; i++)
                            {
                                fpa[i] = math.max(fpa[i], fp_m[i]);
                            }
                        }
                        else
                        {
                            float* fp_m = (float*)(void*)&fpa[ssmd_datacount];
                            pop_fx_into_ref<float>(ref code_pointer, fp_m);

                            // constrain lower bound 
                            for (int i = 0; i < ssmd_datacount; i++)
                            {
                                fpa[i] = math.max(fpa[i], (float2)fp_m[i]);
                            }
                        }

                        // then the upper bound and write to output 
                        pop_op_meta_cp(code_pointer, out datatype2, out vector_size_2);
#if DEVELOPMENT_BUILD || TRACE
                        if (vector_size_2 != 1 && vector_size_2 != 2)
                        {
                            Debug.LogError($"blast.ssmd.interpretor.clamp: datatype|vectorsize mismatch, vector_size: {vector_size}, datatype: {datatype}, minmax vectorsize {vector_size_2} not supported");
                            return;
                        }
                        if (datatype != datatype2)
                        {
                            Debug.LogError($"blast.ssmd.interpretor.clamp: datatype|vectorsize mismatch, vector_size: {vector_size}, datatype: {datatype}, minmax datatype = {datatype2}");
                            return;
                        }
#endif
                        if (vector_size_2 == 2)
                        {
                            float2* fp_m = &fpa[ssmd_datacount];
                            pop_fx_into_ref<float2>(ref code_pointer, fp_m);

                            // constrain lower bound 
                            for (int i = 0; i < ssmd_datacount; i++)
                            {
                                register[i].xy = math.min(fpa[i], fp_m[i]);
                            }
                        }
                        else
                        {
                            float* fp_m = (float*)(void*)&fpa[ssmd_datacount];
                            pop_fx_into_ref<float>(ref code_pointer, fp_m);

                            // constrain lower bound 
                            for (int i = 0; i < ssmd_datacount; i++)
                            {
                                register[i].xy = math.min(fpa[i], (float2)fp_m[i]);
                            }
                        }
                    }
                    break;

                case 3:
                    {
                        // 2 float3;s wont fit in temp anymore 
                        float3* fpa = (float3*)temp;
                        float3* fp_m = stackalloc float3[ssmd_datacount];


                        pop_fx_into_ref<float3>(ref code_pointer, fpa);

                        // first the lower bound 
                        pop_op_meta_cp(code_pointer, out datatype2, out vector_size_2);
                        // must be size 1 or 3 
#if DEVELOPMENT_BUILD || TRACE
                        if (vector_size_2 != 1 && vector_size_2 != vector_size)
                        {
                            Debug.LogError($"blast.ssmd.interpretor.clamp: datatype|vectorsize mismatch, vector_size: {vector_size}, datatype: {datatype}, minmax vectorsize {vector_size_2} not supported");
                            return;
                        }
                        if (datatype != datatype2)
                        {
                            Debug.LogError($"blast.ssmd.interpretor.clamp: datatype|vectorsize mismatch, vector_size: {vector_size}, datatype: {datatype}, minmax datatype = {datatype2}");
                            return;
                        }
#endif
                        if (vector_size_2 == vector_size)
                        {
                            pop_fx_into_ref<float3>(ref code_pointer, fp_m);

                            // constrain lower bound 
                            for (int i = 0; i < ssmd_datacount; i++)
                            {
                                fpa[i] = math.max(fpa[i], fp_m[i]);
                            }
                        }
                        else
                        {
                            float* fp_m1 = (float*)(void*)fp_m;
                            pop_fx_into_ref<float>(ref code_pointer, fp_m1);

                            // constrain lower bound 
                            for (int i = 0; i < ssmd_datacount; i++)
                            {
                                fpa[i] = math.max(fpa[i], (float3)fp_m1[i]);
                            }
                        }

                        // then the upper bound and write to output 
                        pop_op_meta_cp(code_pointer, out datatype2, out vector_size_2);
#if DEVELOPMENT_BUILD || TRACE
                        if (vector_size_2 != 1 && vector_size_2 != vector_size)
                        {
                            Debug.LogError($"blast.ssmd.interpretor.clamp: datatype|vectorsize mismatch, vector_size: {vector_size}, datatype: {datatype}, minmax vectorsize {vector_size_2} not supported");
                            return;
                        }
                        if (datatype != datatype2)
                        {
                            Debug.LogError($"blast.ssmd.interpretor.clamp: datatype|vectorsize mismatch, vector_size: {vector_size}, datatype: {datatype}, minmax datatype = {datatype2}");
                            return;
                        }
#endif
                        if (vector_size_2 == vector_size)
                        {
                            pop_fx_into_ref<float3>(ref code_pointer, fp_m);

                            // constrain lower bound 
                            for (int i = 0; i < ssmd_datacount; i++)
                            {
                                register[i].xyz = math.min(fpa[i], fp_m[i]);
                            }
                        }
                        else
                        {
                            float* fp_m1 = (float*)(void*)fp_m;
                            pop_fx_into_ref<float>(ref code_pointer, fp_m1);

                            // constrain lower bound 
                            for (int i = 0; i < ssmd_datacount; i++)
                            {
                                register[i].xyz = math.min(fpa[i], (float3)fp_m1[i]);
                            }
                        }
                    }
                    break;

                case 0:
                case 4:
                    {
                        // 2 float4;s wont fit in temp anymore 
                        float4* fpa = (float4*)temp;
                        float4* fp_m = stackalloc float4[ssmd_datacount];

                        pop_fx_into_ref<float4>(ref code_pointer, fpa);

                        // first the lower bound 
                        pop_op_meta_cp(code_pointer, out datatype2, out vector_size_2);
                        // must be size 1 or 4 
#if DEVELOPMENT_BUILD || TRACE
                        if (vector_size_2 != 1 && vector_size_2 != vector_size)
                        {
                            Debug.LogError($"blast.ssmd.interpretor.clamp: datatype|vectorsize mismatch, vector_size: {vector_size}, datatype: {datatype}, minmax vectorsize {vector_size_2} not supported");
                            return;
                        }
                        if (datatype != datatype2)
                        {
                            Debug.LogError($"blast.ssmd.interpretor.clamp: datatype|vectorsize mismatch, vector_size: {vector_size}, datatype: {datatype}, minmax datatype = {datatype2}");
                            return;
                        }
#endif
                        if (vector_size_2 == vector_size)
                        {
                            pop_fx_into_ref<float4>(ref code_pointer, fp_m);

                            // constrain lower bound 
                            for (int i = 0; i < ssmd_datacount; i++)
                            {
                                fpa[i] = math.max(fpa[i], fp_m[i]);
                            }
                        }
                        else
                        {
                            float* fp_m1 = (float*)(void*)fp_m;
                            pop_fx_into_ref<float>(ref code_pointer, fp_m1);

                            // constrain lower bound 
                            for (int i = 0; i < ssmd_datacount; i++)
                            {
                                fpa[i] = math.max(fpa[i], (float4)fp_m1[i]);
                            }
                        }

                        // then the upper bound and write to output 
                        pop_op_meta_cp(code_pointer, out datatype2, out vector_size_2);
#if DEVELOPMENT_BUILD || TRACE
                        if (vector_size_2 != 1 && vector_size_2 != vector_size)
                        {
                            Debug.LogError($"blast.ssmd.interpretor.clamp: datatype|vectorsize mismatch, vector_size: {vector_size}, datatype: {datatype}, minmax vectorsize {vector_size_2} not supported");
                            return;
                        }
                        if (datatype != datatype2)
                        {
                            Debug.LogError($"blast.ssmd.interpretor.clamp: datatype|vectorsize mismatch, vector_size: {vector_size}, datatype: {datatype}, minmax datatype = {datatype2}");
                            return;
                        }
#endif
                        if (vector_size_2 == vector_size)
                        {
                            pop_fx_into_ref<float4>(ref code_pointer, fp_m);

                            // constrain lower bound 
                            for (int i = 0; i < ssmd_datacount; i++)
                            {
                                register[i] = math.min(fpa[i], fp_m[i]);
                            }
                        }
                        else
                        {
                            float* fp_m1 = (float*)(void*)fp_m;
                            pop_fx_into_ref<float>(ref code_pointer, fp_m1);

                            // constrain lower bound 
                            for (int i = 0; i < ssmd_datacount; i++)
                            {
                                register[i] = math.min(fpa[i], (float4)fp_m1[i]);
                            }
                        }
                    }
                    break;

#if DEVELOPMENT_BUILD || TRACE
                default:
                    Debug.LogError($"blast.ssmd.interpretor.clamp: datatype|vectorsize mismatch, vector_size: {vector_size}, datatype: {datatype} not supported");
                    break;
#endif
            }

            code_pointer--;
        }

        /// <summary>
        /// lerp value from min to max 
        /// </summary>
        /// <remarks>
        /// 
        /// a = lerp(min, max, s); 
        /// 
        /// TODO :     for all 3 input functions:   we should test stackallocs if they are conditional or not
        /// - if they are always allocated move every case with stackalloc into its own function ? code explosin without templates/generics
        ///  
        /// </remarks>
#if !STANDALONE_VSBUILD
        [SkipLocalsInit]
#endif
        void get_lerp_result([NoAlias] void* temp, ref int code_pointer, ref byte vector_size, [NoAlias] float4* f4)
        {
            BlastVariableDataType datatype;
            pop_op_meta_cp(code_pointer, out datatype, out vector_size);

#if DEVELOPMENT_BUILD || TRACE
            if (datatype != BlastVariableDataType.Numeric)
            {
                Debug.LogError($"blast.ssmd.interpretor.lerp: datatype|vectorsize mismatch, vector_size: {vector_size}, datatype: {datatype} not supported");
                return;
            }
#endif

            //
            // NOTE the third parameter might have a different vectorsize, if different is MUST be size 1
            //
            BlastVariableDataType p_datatype;
            byte p_vector_size;


            switch (vector_size)
            {
                case 1:
                    {
                        // in case of v1 there is no memory problem 
                        float* fpa = (float*)temp;
                        float* fp_min = &fpa[ssmd_datacount];
                        float* fp_max = &fp_min[ssmd_datacount];

                        pop_fx_into_ref<float>(ref code_pointer, fp_min);
                        pop_fx_into_ref<float>(ref code_pointer, fp_max);

                        // the third param will always equal size 1
                        pop_fx_into_ref<float>(ref code_pointer, fpa);


                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            register[i].x = math.lerp(fp_min[i], fp_max[i], fpa[i]);
                        }
                    }
                    break;

                case 2:
                    {
                        float2* fpa = (float2*)temp;
                        float2* fp_min = &fpa[ssmd_datacount];
                        float2* fp_max = stackalloc float2[ssmd_datacount];

                        pop_fx_into_ref<float2>(ref code_pointer, fp_min);
                        pop_fx_into_ref<float2>(ref code_pointer, fp_max);

                        pop_op_meta_cp(code_pointer, out p_datatype, out p_vector_size);


                        // c param == size 1
                        if (p_vector_size == 1)
                        {
                            // just cast the first piece of temp to a float1, the second half is used for min
                            pop_fx_into_ref<float>(ref code_pointer, (float*)temp);

                            for (int i = 0; i < ssmd_datacount; i++)
                            {
                                register[i].xy = math.lerp(fp_min[i], fp_max[i], ((float*)temp)[i]);
                            }
                        }
                        // same vectorsize, this relies on pop_fx_into to log errors on vectorsize mismatch
                        else //if (p_vector_size == 2)
                        {
                            pop_fx_into_ref<float2>(ref code_pointer, fpa);

                            for (int i = 0; i < ssmd_datacount; i++)
                            {
                                register[i].xy = math.lerp(fp_min[i], fp_max[i], fpa[i]);
                            }
                        }
                    }
                    break;

                case 3:
                    {
                        float3* fpa = (float3*)temp;
                        float3* fp_min = stackalloc float3[ssmd_datacount];
                        float3* fp_max = stackalloc float3[ssmd_datacount];

                        pop_fx_into_ref<float3>(ref code_pointer, fp_min);
                        pop_fx_into_ref<float3>(ref code_pointer, fp_max);

                        pop_op_meta_cp(code_pointer, out p_datatype, out p_vector_size);


                        // c param == size 1
                        if (p_vector_size == 1)
                        {
                            // just cast the first piece of temp to a float1
                            pop_fx_into_ref<float>(ref code_pointer, (float*)temp);

                            for (int i = 0; i < ssmd_datacount; i++)
                            {
                                register[i].xyz = math.lerp(fp_min[i], fp_max[i], ((float*)temp)[i]);
                            }
                        }
                        else
                        {
                            pop_fx_into_ref<float3>(ref code_pointer, fpa);

                            for (int i = 0; i < ssmd_datacount; i++)
                            {
                                register[i].xyz = math.lerp(fp_min[i], fp_max[i], fpa[i]);
                            }
                        }
                    }
                    break;

                case 0:
                case 4:
                    {
                        float4* fpa = (float4*)temp;
                        float4* fp_min = stackalloc float4[ssmd_datacount];
                        float4* fp_max = f4; // only on equal vectorsize can we reuse output buffer because of how we overwrite our values writing to the output if the source is smaller in size

                        pop_fx_into_ref<float4>(ref code_pointer, fp_min);
                        pop_fx_into_ref<float4>(ref code_pointer, fp_max);

                        pop_op_meta_cp(code_pointer, out p_datatype, out p_vector_size);


                        // c param == size 1
                        if (p_vector_size == 1)
                        {
                            // just cast the first piece of temp to a float1
                            pop_fx_into_ref<float>(ref code_pointer, (float*)temp);

                            for (int i = 0; i < ssmd_datacount; i++)
                            {
                                register[i] = math.lerp(fp_min[i], fp_max[i], ((float*)temp)[i]);
                            }
                        }
                        else
                        {
                            pop_fx_into_ref<float4>(ref code_pointer, fpa);

                            for (int i = 0; i < ssmd_datacount; i++)
                            {
                                register[i] = math.lerp(fp_min[i], fp_max[i], fpa[i]);
                            }
                        }
                    }
                    break;

#if DEVELOPMENT_BUILD || TRACE
                default:
                    Debug.LogError($"blast.ssmd.interpretor.lerp: datatype|vectorsize mismatch, vector_size: {vector_size}, datatype: {datatype} not supported");
                    break;
#endif
            }
            code_pointer--;
        }


        /// <summary>
        /// undo lerp 
        /// </summary>
#if !STANDALONE_VSBUILD
        [SkipLocalsInit]
#endif
        void get_unlerp_result([NoAlias] void* temp, ref int code_pointer, ref byte vector_size, [NoAlias] float4* f4)
        {
            BlastVariableDataType datatype;
            pop_op_meta_cp(code_pointer + 1, out datatype, out vector_size);

#if DEVELOPMENT_BUILD || TRACE
            if (datatype != BlastVariableDataType.Numeric)
            {
                Debug.LogError($"blast.ssmd.interpretor.unlerp: datatype|vectorsize mismatch, vector_size: {vector_size}, datatype: {datatype} not supported");
                return;
            }
#endif

            switch (vector_size)
            {
                case 1:
                    {
                        // in case of v1 there is no memory problem 
                        float* fpa = (float*)temp;
                        float* fp_min = &fpa[ssmd_datacount];
                        float* fp_max = &fp_min[ssmd_datacount];

                        pop_fx_into_ref<float>(ref code_pointer, fp_min);
                        pop_fx_into_ref<float>(ref code_pointer, fp_max);
                        pop_fx_into_ref<float>(ref code_pointer, fpa);

                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            register[i].x = math.unlerp(fp_min[i], fp_max[i], fpa[i]);
                        }
                    }
                    break;

                case 2:
                    {
                        float2* fpa = (float2*)temp;
                        float2* fp_min = &fpa[ssmd_datacount];
                        float2* fp_max = stackalloc float2[ssmd_datacount];

                        pop_fx_into_ref<float2>(ref code_pointer, fp_min);
                        pop_fx_into_ref<float2>(ref code_pointer, fp_max);
                        pop_fx_into_ref<float2>(ref code_pointer, fpa);

                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            register[i].xy = math.unlerp(fp_min[i], fp_max[i], fpa[i]);
                        }

                    }
                    break;

                case 3:
                    {
                        float3* fpa = (float3*)temp;
                        float3* fp_min = stackalloc float3[ssmd_datacount];
                        float3* fp_max = stackalloc float3[ssmd_datacount];

                        pop_fx_into_ref<float3>(ref code_pointer, fp_min);
                        pop_fx_into_ref<float3>(ref code_pointer, fp_max);
                        pop_fx_into_ref<float3>(ref code_pointer, fpa);

                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            register[i].xyz = math.unlerp(fp_min[i], fp_max[i], fpa[i]);
                        }
                    }
                    break;

                case 0:
                case 4:
                    {
                        float4* fpa = (float4*)temp;
                        float4* fp_min = stackalloc float4[ssmd_datacount];
                        float4* fp_max = f4; // only on equal vectorsize can we reuse output buffer because of how we overwrite our values writing to the output if the source is smaller in size

                        pop_fx_into_ref<float4>(ref code_pointer, fp_min);
                        pop_fx_into_ref<float4>(ref code_pointer, fp_max);
                        pop_fx_into_ref<float4>(ref code_pointer, fpa);

                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            register[i] = math.unlerp(fp_min[i], fp_max[i], fpa[i]);
                        }
                    }
                    break;

#if DEVELOPMENT_BUILD || TRACE
                default:
                    Debug.LogError($"blast.ssmd.interpretor.unlerp: datatype|vectorsize mismatch, vector_size: {vector_size}, datatype: {datatype} not supported");
                    break;
#endif
            }

            code_pointer--;
        }


        /// <summary>
        /// sperical lerp on quaternions 
        /// </summary>
#if !STANDALONE_VSBUILD
        [SkipLocalsInit]
#endif
        void get_slerp_result([NoAlias] void* temp, ref int code_pointer, ref byte vector_size, [NoAlias] float4* f4)
        {
            BlastVariableDataType datatype;
            pop_op_meta_cp(code_pointer + 1, out datatype, out vector_size);

#if DEVELOPMENT_BUILD || TRACE
            if (datatype != BlastVariableDataType.Numeric)
            {
                Debug.LogError($"blast.ssmd.interpretor.slerp: datatype|vectorsize mismatch, vector_size: {vector_size}, datatype: {datatype} not supported");
                return;
            }
#endif

            switch (vector_size)
            {

                case 0:
                case 4:
                    {
                        float* fpa = stackalloc float[ssmd_datacount];
                        float4* fp_min = (float4*)temp;
                        float4* fp_max = f4; // only on equal vectorsize can we reuse output buffer because of how we overwrite our values writing to the output if the source is smaller in size

                        pop_fx_into_ref<float4>(ref code_pointer, fp_min);
                        pop_fx_into_ref<float4>(ref code_pointer, fp_max);
                        pop_fx_into_ref<float>(ref code_pointer, fpa);

                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            register[i] = math.slerp((quaternion)fp_min[i], (quaternion)fp_max[i], fpa[i]).value;
                        }
                    }
                    break;

#if DEVELOPMENT_BUILD || TRACE
                default:
                    Debug.LogError($"blast.ssmd.interpretor.slerp: datatype|vectorsize mismatch, vector_size: {vector_size}, datatype: {datatype} not supported");
                    break;
#endif
            }

            code_pointer--;
        }

        /// <summary>
        /// normalized lerp on quaternions
        /// </summary>
#if !STANDALONE_VSBUILD
        [SkipLocalsInit]
#endif
        void get_nlerp_result([NoAlias] void* temp, ref int code_pointer, ref byte vector_size, [NoAlias] float4* f4)
        {
            BlastVariableDataType datatype;
            pop_op_meta_cp(code_pointer + 1, out datatype, out vector_size);

#if DEVELOPMENT_BUILD || TRACE
            if (datatype != BlastVariableDataType.Numeric)
            {
                Debug.LogError($"blast.ssmd.interpretor.nlerp: datatype|vectorsize mismatch, vector_size: {vector_size}, datatype: {datatype} not supported");
                return;
            }
#endif

            switch (vector_size)
            {

                case 0:
                case 4:
                    {
                        float* fpa = stackalloc float[ssmd_datacount];
                        float4* fp_min = (float4*)temp;
                        float4* fp_max = f4; // only on equal vectorsize can we reuse output buffer because of how we overwrite our values writing to the output if the source is smaller in size

                        pop_fx_into_ref<float4>(ref code_pointer, fp_min);
                        pop_fx_into_ref<float4>(ref code_pointer, fp_max);
                        pop_fx_into_ref<float>(ref code_pointer, fpa);

                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            register[i] = math.nlerp((quaternion)fp_min[i], (quaternion)fp_max[i], fpa[i]).value;
                        }
                    }
                    break;

#if DEVELOPMENT_BUILD || TRACE
                default:
                    Debug.LogError($"blast.ssmd.interpretor.nlerp: datatype|vectorsize mismatch, vector_size: {vector_size}, datatype: {datatype} not supported");
                    break;
#endif
            }

            code_pointer--;
        }




        #endregion

        #region op-a  

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
        void get_op_a_result([NoAlias] void* temp, ref int code_pointer, ref byte vector_size, [NoAlias] ref float4* f4, blast_operation operation, BlastParameterEncoding encoding = BlastParameterEncoding.Encode62)
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
        void get_op_a_component_result([NoAlias] void* temp, ref int code_pointer, ref byte vector_size, [NoAlias] ref float4* f4, blast_operation operation)
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

        #region fused substract multiply actions

        /// <summary>
        /// fused multiply add 
        /// 
        /// todo: convert this into a fused condition list, with n conditions and n+1 parameters ?:
        ///
        /// fused_op:
        /// # 1 byte = encode62 = parametercount + vectorsize
        /// # 1 byte * ceil(parametercount / 4) = conditions: +-/* encoded in 2 bits, 4 in byte 
        /// # parameters 
        /// 
        /// - always 3 params, input vector size == output vectorsize
        /// </summary>
        /// <returns></returns>
        void get_fma_result([NoAlias] void* temp, ref int code_pointer, ref byte vector_size, [NoAlias] ref float4* f4)
        {
            BlastVariableDataType datatype, datatype2;
            byte vector_size2; 

            // peek metadata of the first parameter
            pop_op_meta_cp(code_pointer + 0, out datatype, out vector_size);

            // first 2 inputs share datatype and vectorsize 
            switch (vector_size)
            {
                case 1:
                    // in case of v1. all parameters should equal in size
                    float* m11 = (float*)temp;
                    pop_fx_into_ref<float>(ref code_pointer, m11);
                    pop_fx_with_op_into_fx_ref(ref code_pointer, m11, m11, blast_operation.multiply);
                    pop_fx_with_op_into_fx_ref(ref code_pointer, m11, f4, blast_operation.add);
                    // this is needed because pop_fx_with_op_into_fx_ref leaves codepointer after the last data while function should end at the last
                    code_pointer--;
                    break;

                case 2:
                    // the last op might be in another vectorsize 
                    float2* m12 = (float2*)temp;
                    pop_fx_into_ref<float2>(ref code_pointer, m12);
                    pop_fx_with_op_into_fx_ref<float2, float2>(ref code_pointer, m12, m12, blast_operation.multiply);

                    pop_op_meta_cp(code_pointer, out datatype2, out vector_size2);
                    pop_fx_with_op_into_fx_ref<float2, float4>(ref code_pointer, m12, f4, blast_operation.add, datatype2, vector_size2 != 2);
                    code_pointer--;
                    break;

                case 3:
                    float3* m13 = (float3*)temp;
                    pop_fx_into_ref<float3>(ref code_pointer, m13);
                    pop_fx_with_op_into_fx_ref<float3, float3>(ref code_pointer, m13, m13, blast_operation.multiply);

                    pop_op_meta_cp(code_pointer, out datatype2, out vector_size2);
                    pop_fx_with_op_into_fx_ref<float3, float4>(ref code_pointer, m13, f4, blast_operation.add, datatype2, vector_size2 != 3);
                    code_pointer--;
                    break;

                case 0:
                case 4:
                    float4* m14 = (float4*)temp;
                    pop_fx_into_ref<float4>(ref code_pointer, f4); // with f4 we can avoid reading/writing to the same buffer by alternating as long as we end at f4
                    pop_fx_with_op_into_fx_ref<float4, float4>(ref code_pointer, f4, m14, blast_operation.multiply);

                    // last may differ in vectorsize 
                    pop_op_meta_cp(code_pointer, out datatype2, out vector_size2);
                    pop_fx_with_op_into_fx_ref<float4, float4>(ref code_pointer, m14, f4, blast_operation.add, datatype2, vector_size2 != 4);

                    code_pointer--;
                    break;
            }
        }

        #endregion

        #region Bitwise operations

        /// <summary>
        /// Note: sets bit in place, first parameter must be a direct data index (compiler should force this)
        /// </summary>
        void get_setbit_result([NoAlias] void* temp, ref int code_pointer, ref byte vector_size)
        {
            float* fp_index = (float*)temp;
            float* fp_value = &fp_index[ssmd_datacount];

            // index would be the same for each ssmd record
            int dataindex = code[code_pointer] - BlastInterpretor.opt_id;
            code_pointer++;

#if DEVELOPMENT_BUILD || TRACE
            // dataindex must point to a valid id 
            if (dataindex < 0 || dataindex + BlastInterpretor.opt_id >= 255)
            {
                Debug.LogError($"ssmd.set_bit: first parameter must directly point to a dataindex but its '{dataindex + BlastInterpretor.opt_id}' instead");
                return;
            }
#else
            // dont check anything in release and vectorsize must be 1 so
            vector_size = 1;
#endif

            // get index
#if DEVELOPMENT_BUILD || TRACE
            BlastVariableDataType datatype;
            pop_op_meta_cp(code_pointer, out datatype, out vector_size);
            if (vector_size != 1)
            {
                Debug.LogError($"ssmd.set_bit: the index to set may only be of vectorsize 1 p1 = {datatype}.{vector_size}");
                return;
            }
#endif

            bool b_constant_index = pop_fx_into_ref<float>(ref code_pointer, fp_index);

            // get value
#if DEVELOPMENT_BUILD || TRACE
            pop_op_meta_cp(code_pointer, out datatype, out vector_size);
            if (vector_size != 1)
            {
                Debug.LogError($"ssmd.set_bit: the value to set may only be of vectorsize 1 p1 = {datatype}.{vector_size}");
                return;
            }
#endif

            // NOTE: if the source of fp_value is a constant then in ssmd they would be the same value for each record 
            // OPTIMIZE: and this loop can be optimized a lot... 

            bool b_constant_value = pop_fx_into_ref<float>(ref code_pointer, fp_value);

            // setbit & setbits override the datatype of the target to bool32 
            BlastInterpretor.SetMetaData(in metadata, BlastVariableDataType.Bool32, 1, (byte)dataindex);

#if DEVELOPMENT_BUILD || TRACE

            // check that all constant arrays are actually of all the same values 
            // this would indicate a serious bug in constant handling early 

            if (b_constant_index)
            {
                float f1 = fp_index[0];
                for (int i = 1; i < ssmd_datacount; i++)
                {
                    if (fp_index[i] != f1)
                    {
                        Debug.LogError($"ssmd.set_bit: the index is marked constant but is not equal for all ssmd records at {code_pointer}");
                        return;
                    }
                }
            }

            if (b_constant_value)
            {
                float f0 = fp_value[0];
                for (int i = 1; i < ssmd_datacount; i++)
                {
                    if (fp_value[i] != f0)
                    {
                        Debug.LogError($"ssmd.set_bit: the value is marked constant but is not equal for all ssmd records at {code_pointer}");
                        return;
                    }
                }
            }

#endif

            //
            // when index or value is constant we can do some very nice optimizations, in this case we can even skip stuff 
            // 

            if (b_constant_value)
            {
                if (b_constant_index)
                {
                    // mask always the same 
                    uint mask = (uint)(1 << (byte)fp_index[0]);

                    // value always the same 
                    if (((uint*)fp_value)[0] == 0)
                    {
                        // unset bits: AND the mask 
                        if (mask == uint.MaxValue)
                        {
                            // mask is all 1, data remains equal 
                        }
                        else
                        {
                            for (int i = 0; i < ssmd_datacount; i++)
                            {
                                uint current = ((uint*)data[i])[dataindex];

                                current = (uint)(current & ~mask);

                                ((uint*)data[i])[dataindex] = current;
                            }
                        }
                    }
                    else
                    {
                        // set bits: OR the mask
                        if (mask == 0)
                        {
                            // mask all 0, data remains equal 
                        }
                        else
                        {
                            for (int i = 0; i < ssmd_datacount; i++)
                            {
                                uint current = ((uint*)data[i])[dataindex];

                                current = current | mask;

                                ((uint*)data[i])[dataindex] = current;
                            }
                        }
                    }
                }
                else
                {
                    // the value is always the same but the mask changes for every i
                    if (((uint*)fp_value)[0] == 0)
                    {
                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            uint current = ((uint*)data[i])[dataindex];

                            uint mask = (uint)(1 << (byte)fp_index[i]);

                            current = (uint)(current & ~mask);

                            ((uint*)data[i])[dataindex] = current;
                        }
                    }
                    else
                    {
                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            uint current = ((uint*)data[i])[dataindex];

                            uint mask = (uint)(1 << (byte)fp_index[i]);

                            current = (uint)(current | mask);

                            ((uint*)data[i])[dataindex] = current;
                        }
                    }
                }
            }
            else
            {
                if (b_constant_index)
                {
                    // mask always the same 
                    uint mask = (uint)(1 << (byte)fp_index[0]);

                    for (int i = 0; i < ssmd_datacount; i++)
                    {
                        uint current = ((uint*)data[i])[dataindex];

                        current = math.select((uint)(current | mask), (uint)(current & ~mask), ((uint*)fp_value)[i] == 0);

                        ((uint*)data[i])[dataindex] = current;
                    }
                }
                else
                {
                    // nothing constant 
                    for (int i = 0; i < ssmd_datacount; i++)
                    {
                        uint current = ((uint*)data[i])[dataindex];

                        uint mask = (uint)(1 << (byte)fp_index[i]);

                        current = math.select((uint)(current | mask), (uint)(current & ~mask), ((uint*)fp_value)[i] == 0);

                        ((uint*)data[i])[dataindex] = current;
                    }
                }
            }


            code_pointer--;
        }


        /// <summary>
        /// get bit and return result to f4 register.x
        /// </summary>
        void get_getbit_result([NoAlias] void* temp, ref int code_pointer, ref byte vector_size, [NoAlias] float4* f4_result)
        {
            float* fp_index = (float*)temp;

            // index would be the same for each ssmd record
            int dataindex = code[code_pointer] - BlastInterpretor.opt_id;
            code_pointer++;

#if DEVELOPMENT_BUILD || TRACE
            // dataindex must point to a valid id 
            if (dataindex < 0 || dataindex + BlastInterpretor.opt_id >= 255)
            {
                Debug.LogError($"ssmd.get_bit: first parameter must directly point to a dataindex but its '{dataindex + BlastInterpretor.opt_id}' instead");
                return;
            }
            BlastVariableDataType datatype = BlastInterpretor.GetMetaDataType(metadata, (byte)dataindex);
            if (datatype != BlastVariableDataType.Bool32)
            {
                Debug.LogError($"ssmd.get_bit: retrieving a bit from a non Bool32 datatype, codepointer = {code_pointer}, data = {datatype}.{BlastInterpretor.GetMetaDataSize(metadata, (byte)dataindex)}");
                return;
            }
#else
            vector_size = 1;
#endif
            // get index
#if DEVELOPMENT_BUILD || TRACE
            pop_op_meta_cp(code_pointer, out datatype, out vector_size);
            if (vector_size != 1)
            {
                Debug.LogError($"ssmd.get_bit: the index to set may only be of vectorsize 1 p1 = {datatype}.{vector_size}");
                return;
            }
#else
            // in release we assume compiler did its job
#endif

            bool b_constant_index = pop_fx_into_ref<float>(ref code_pointer, fp_index);

#if DEVELOPMENT_BUILD || TRACE
            if (b_constant_index)
            {
                for (int i = 1; i < ssmd_datacount; i++)
                {
                    if (fp_index[i] != fp_index[0])
                    {
                        // without string magics this is burst compatible so we wont create a function but just have this code everywhere... codegen someday; 
                        Debug.LogError($"ssmd.get_bit: the index is marked constant but is not equal for all ssmd records at {code_pointer}");
                        return;
                    }
                }
            }
#endif

            if (b_constant_index)
            {
                // in most cases get bit is called with a constant index 
                uint mask = (uint)(1 << (byte)fp_index[0]);

                // saving a shift and memindex every index:
                for (int i = 0; i < ssmd_datacount; i++)
                {
                    uint current = ((uint*)data[i])[dataindex];
                    f4_result[i].x = math.select(0f, 1f, (mask & current) == mask);
                }

            }
            else
            {
                for (int i = 0; i < ssmd_datacount; i++)
                {
                    uint current = ((uint*)data[i])[dataindex];
                    uint mask = (uint)(1 << (byte)fp_index[i]);

                    f4_result[i].x = math.select(0f, 1f, (mask & current) == mask);
                }
            }

            code_pointer--;
        }





        void get_setbits_result([NoAlias] void* temp, ref int code_pointer, ref byte vector_size)
        {
            float* fp_mask = (float*)temp;
            float* fp_value = &fp_mask[ssmd_datacount];

            // index would be the same for each ssmd record
            int dataindex = code[code_pointer] - BlastInterpretor.opt_id;
            code_pointer++;

#if DEVELOPMENT_BUILD || TRACE
            // dataindex must point to a valid id 
            if (dataindex < 0 || dataindex + BlastInterpretor.opt_id >= 255)
            {
                Debug.LogError($"ssmd.set_bits: first parameter must directly point to a dataindex but its '{dataindex + BlastInterpretor.opt_id}' instead");
                return;
            }
#else
            // dont check anything in release and vectorsize must be 1 so
            vector_size = 1;
#endif

            // get index
#if DEVELOPMENT_BUILD || TRACE
            BlastVariableDataType datatype;
            pop_op_meta_cp(code_pointer, out datatype, out vector_size);
            if (vector_size != 1)
            {
                Debug.LogError($"ssmd.set_bits: the mask to set may only be of vectorsize 1 p1 = {datatype}.{vector_size}");
                return;
            }
#endif

            bool mask_is_constant = pop_fx_into_ref<float>(ref code_pointer, fp_mask);

            // get value
#if DEVELOPMENT_BUILD || TRACE
            pop_op_meta_cp(code_pointer, out datatype, out vector_size);
            if (vector_size != 1)
            {
                Debug.LogError($"ssmd.set_bits: the value to set may only be of vectorsize 1 p1 = {datatype}.{vector_size}");
                return;
            }
#endif

            bool value_is_constant = pop_fx_into_ref<float>(ref code_pointer, fp_value);


#if DEVELOPMENT_BUILD || TRACE

            // check that all constant arrays are actually of all the same values 
            // this would indicate a serious bug in constant handling early 

            if (mask_is_constant)
            {
                float f1 = fp_mask[0];
                for (int i = 1; i < ssmd_datacount; i++)
                {
                    if (fp_mask[i] != f1)
                    {
                        Debug.LogError($"ssmd.set_bits: the masks is marked constant but is not equal for all ssmd records at {code_pointer}");
                        return;
                    }
                }
            }

            if (value_is_constant)
            {
                float f0 = fp_value[0];
                for (int i = 1; i < ssmd_datacount; i++)
                {
                    if (fp_value[i] != f0)
                    {
                        Debug.LogError($"ssmd.set_bits: the value is marked constant but is not equal for all ssmd records at {code_pointer}");
                        return;
                    }
                }
            }

#endif
            // setbit & setbits override the datatype of the target to bool32 
            BlastInterpretor.SetMetaData(in metadata, BlastVariableDataType.Bool32, 1, (byte)dataindex);

            if (mask_is_constant)
            {
                uint mask = ((uint*)fp_mask)[0];

                if (value_is_constant)
                {
                    // constant mask constant value 
                    if (((uint*)fp_value)[0] == 0)
                    {
                        mask = ~mask; // not it for clarity  

                        if (mask == 0)
                        {
                            // something & 0 == always zero 
                            for (int i = 0; i < ssmd_datacount; i++)
                            {
                                ((uint*)data[i])[dataindex] = 0;
                            }
                        }
                        else
                        if (mask == uint.MaxValue)
                        {
                            // all 1;s & something == copy value == nothing changes 
                        }
                        else
                        {
                            for (int i = 0; i < ssmd_datacount; i++)
                            {
                                uint current = ((uint*)data[i])[dataindex];
                                current = (uint)(current & mask);
                                ((uint*)data[i])[dataindex] = current;
                            }
                        }
                    }
                    else
                    {
                        if (mask == uint.MaxValue)
                        {
                            // or with all 1's => nothing changes 
                        }
                        else
                        {
                            for (int i = 0; i < ssmd_datacount; i++)
                            {
                                uint current = ((uint*)data[i])[dataindex];
                                current = (uint)(current | mask);
                                ((uint*)data[i])[dataindex] = current;
                            }
                        }
                    }
                }
                else
                {
                    // constant mask, variable value 
                    for (int i = 0; i < ssmd_datacount; i++)
                    {
                        uint current = ((uint*)data[i])[dataindex];
                        current = math.select((uint)(current | mask), (uint)(current & ~mask), ((uint*)fp_value)[i] == 0);
                        ((uint*)data[i])[dataindex] = current;
                    }
                }
            }
            else
            {
                if (value_is_constant)
                {
                    if (((uint*)fp_value)[0] == 0)
                    {
                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            uint current = ((uint*)data[i])[dataindex];
                            uint mask = ((uint*)fp_mask)[i];
                            current = (uint)(current & ~mask);
                            ((uint*)data[i])[dataindex] = current;
                        }
                    }
                    else
                    {
                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            uint current = ((uint*)data[i])[dataindex];
                            uint mask = ((uint*)fp_mask)[i];
                            current = (uint)(current | mask);
                            ((uint*)data[i])[dataindex] = current;
                        }
                    }
                }
                else
                {
                    // nothing is constant 
                    for (int i = 0; i < ssmd_datacount; i++)
                    {
                        uint current = ((uint*)data[i])[dataindex];
                        uint mask = ((uint*)fp_mask)[i];
                        current = math.select((uint)(current | mask), (uint)(current & ~mask), ((uint*)fp_value)[i] == 0);
                        ((uint*)data[i])[dataindex] = current;
                    }
                }
            }

            code_pointer--;
        }


        /// <summary>
        /// get bit and return result to f4 register.x
        /// </summary>
        void get_getbits_result([NoAlias] void* temp, ref int code_pointer, ref byte vector_size, [NoAlias] float4* f4_result)
        {
            float* fp_mask = (float*)temp;

            // index would be the same for each ssmd record
            int dataindex = code[code_pointer] - BlastInterpretor.opt_id;
            code_pointer++;

#if DEVELOPMENT_BUILD || TRACE
            // dataindex must point to a valid id 
            if (dataindex < 0 || dataindex + BlastInterpretor.opt_id >= 255)
            {
                Debug.LogError($"ssmd.get_bits: first parameter must directly point to a dataindex but its '{dataindex + BlastInterpretor.opt_id}' instead");
                return;
            }
            BlastVariableDataType datatype = BlastInterpretor.GetMetaDataType(metadata, (byte)dataindex);
            if (datatype != BlastVariableDataType.Bool32)
            {
                Debug.LogError($"ssmd.get_bits: retrieving a bit from a non Bool32 datatype, codepointer = {code_pointer}, data = {datatype}.{BlastInterpretor.GetMetaDataSize(metadata, (byte)dataindex)}");
                return;
            }
#else
            vector_size = 1;
#endif
            // get index
#if DEVELOPMENT_BUILD || TRACE
            pop_op_meta_cp(code_pointer, out datatype, out vector_size);
            if (vector_size != 1)
            {
                Debug.LogError($"ssmd.get_bits: the mask to set may only be of vectorsize 1 p1 = {datatype}.{vector_size}");
                return;
            }
#else
            // in release we assume compiler did its job
#endif

            bool is_constant_mask = pop_fx_into_ref<float>(ref code_pointer, fp_mask);

#if DEVELOPMENT_BUILD || TRACE
            if (is_constant_mask && !UnsafeUtils.AllEqual(fp_mask, ssmd_datacount))
            {
                Debug.LogError($"ssmd.get_bits: the mask is marked constant but not all ssmd records have the same value at codepointer {code_pointer}");
                return;
            }
#endif

            if (is_constant_mask)
            {
                uint mask = ((uint*)fp_mask)[0];
                for (int i = 0; i < ssmd_datacount; i++)
                {
                    uint current = ((uint*)data[i])[dataindex];
                    f4_result[i].x = math.select(0f, 1f, (mask & current) == mask);
                }
            }
            else
            {
                for (int i = 0; i < ssmd_datacount; i++)
                {
                    uint current = ((uint*)data[i])[dataindex];
                    uint mask = ((uint*)fp_mask)[i];
                    f4_result[i].x = math.select(0f, 1f, (mask & current) == mask);
                }
            }

            code_pointer--;
        }


        /// <summary>
        /// count bits set and return result to f4 register.x
        /// </summary>
        void get_countbits_result([NoAlias] void* temp, ref int code_pointer, ref byte vector_size, [NoAlias] float4* f4_result)
        {
            // index would be the same for each ssmd record
            int dataindex = code[code_pointer] - BlastInterpretor.opt_id;
            code_pointer++;

#if DEVELOPMENT_BUILD || TRACE
            // dataindex must point to a valid id 
            if (dataindex < 0 || dataindex + BlastInterpretor.opt_id >= 255)
            {
                Debug.LogError($"ssmd.get_countbits: first parameter must directly point to a dataindex but its '{dataindex + BlastInterpretor.opt_id}' instead");
                return;
            }
            BlastVariableDataType datatype = BlastInterpretor.GetMetaDataType(metadata, (byte)dataindex);
            if (datatype != BlastVariableDataType.Bool32)
            {
                Debug.LogError($"ssmd.get_countbits: retrieving a bit from a non Bool32 datatype, codepointer = {code_pointer}, data = {datatype}.{BlastInterpretor.GetMetaDataSize(metadata, (byte)dataindex)}");
                return;
            }
#else
            vector_size = 1;
#endif


            // TODO lookup popcount -> could do this with 32bytes at once with avx2

            for (int i = 0; i < ssmd_datacount; i++)
            {
                uint current = ((uint*)data[i])[dataindex];

                f4_result[i].x = math.countbits(current);
            }

            code_pointer--;
        }

        /// <summary>
        /// count leading bits set to 0 and return result to f4 register.x
        /// </summary>
        void get_lzcnt_result([NoAlias] void* temp, ref int code_pointer, ref byte vector_size, [NoAlias] float4* f4_result)
        {
            // index would be the same for each ssmd record
            int dataindex = code[code_pointer] - BlastInterpretor.opt_id;
            code_pointer++;

#if DEVELOPMENT_BUILD || TRACE
            // dataindex must point to a valid id 
            if (dataindex < 0 || dataindex + BlastInterpretor.opt_id >= 255)
            {
                Debug.LogError($"ssmd.get_lzcnt: first parameter must directly point to a dataindex but its '{dataindex + BlastInterpretor.opt_id}' instead");
                return;
            }
            BlastVariableDataType datatype = BlastInterpretor.GetMetaDataType(metadata, (byte)dataindex);
            if (datatype != BlastVariableDataType.Bool32)
            {
                Debug.LogError($"ssmd.get_lzcnt: retrieving a bit from a non Bool32 datatype, codepointer = {code_pointer}, data = {datatype}.{BlastInterpretor.GetMetaDataSize(metadata, (byte)dataindex)}");
                return;
            }
#else
            vector_size = 1;
#endif

            for (int i = 0; i < ssmd_datacount; i++)
            {
                uint current = ((uint*)data[i])[dataindex];
                f4_result[i].x = math.lzcnt(current);
            }

            code_pointer--;
        }

        /// <summary>
        /// count tailing bits set to 0 and return result to f4 register.x
        /// </summary>
        void get_tzcnt_result([NoAlias] void* temp, ref int code_pointer, ref byte vector_size, [NoAlias] float4* f4_result)
        {
            // index would be the same for each ssmd record
            int dataindex = code[code_pointer] - BlastInterpretor.opt_id;
            code_pointer++;

#if DEVELOPMENT_BUILD || TRACE
            // dataindex must point to a valid id 
            if (dataindex < 0 || dataindex + BlastInterpretor.opt_id >= 255)
            {
                Debug.LogError($"ssmd.get_tzcnt: first parameter must directly point to a dataindex but its '{dataindex + BlastInterpretor.opt_id}' instead");
                return;
            }
            BlastVariableDataType datatype = BlastInterpretor.GetMetaDataType(metadata, (byte)dataindex);
            if (datatype != BlastVariableDataType.Bool32)
            {
                Debug.LogWarning($"ssmd.get_tzcnt: retrieving a bit from a non Bool32 datatype, codepointer = {code_pointer}, data = {datatype}.{BlastInterpretor.GetMetaDataSize(metadata, (byte)dataindex)}");
                return;
            }
#else
            vector_size = 1;
#endif

            for (int i = 0; i < ssmd_datacount; i++)
            {
                uint current = ((uint*)data[i])[dataindex];
                f4_result[i].x = math.tzcnt(current);
            }

            code_pointer--;
        }

        /// <summary>
        /// reverse bits in place
        /// </summary>
        void get_reversebits_result([NoAlias] void* temp, ref int code_pointer, ref byte vector_size)
        {
            // index would be the same for each ssmd record
            int dataindex = code[code_pointer] - BlastInterpretor.opt_id;
            code_pointer++;

#if DEVELOPMENT_BUILD || TRACE
            // dataindex must point to a valid id 
            if (dataindex < 0 || dataindex + BlastInterpretor.opt_id >= 255)
            {
                Debug.LogError($"ssmd.get_reversebits: first parameter must directly point to a dataindex but its '{dataindex + BlastInterpretor.opt_id}' instead");
                return;
            }
            BlastVariableDataType datatype = BlastInterpretor.GetMetaDataType(metadata, (byte)dataindex);
            if (datatype != BlastVariableDataType.Bool32)
            {
                Debug.LogError($"ssmd.get_reversebits: retrieving a bit from a non Bool32 datatype, codepointer = {code_pointer}, data = {datatype}.{BlastInterpretor.GetMetaDataSize(metadata, (byte)dataindex)}");
                return;
            }
#else
            vector_size = 1;
#endif

            for (int i = 0; i < ssmd_datacount; i++)
            {
                uint* p = &((uint*)data[i])[dataindex];
                p[0] = math.reversebits(p[0]);
            }

            code_pointer--;
        }


        /// <summary>
        /// rotate bits left in place 
        /// </summary>
        void get_rol_result([NoAlias] void* temp, ref int code_pointer, ref byte vector_size)
        {
            // index would be the same for each ssmd record
            int dataindex = code[code_pointer] - BlastInterpretor.opt_id;
            code_pointer++;

#if DEVELOPMENT_BUILD || TRACE
            // dataindex must point to a valid id 
            if (dataindex < 0 || dataindex + BlastInterpretor.opt_id >= 255)
            {
                Debug.LogError($"ssmd.get_rol: first parameter must directly point to a dataindex but its '{dataindex + BlastInterpretor.opt_id}' instead");
                return;
            }
            BlastVariableDataType datatype = BlastInterpretor.GetMetaDataType(metadata, (byte)dataindex);
            if (datatype != BlastVariableDataType.Bool32)
            {
                Debug.LogError($"ssmd.get_rol: retrieving a bit from a non Bool32 datatype, codepointer = {code_pointer}, data = {datatype}.{BlastInterpretor.GetMetaDataSize(metadata, (byte)dataindex)}");
                return;
            }
#else
            vector_size = 1;
#endif

            float* fp = (float*)temp;

#if DEVELOPMENT_BUILD || TRACE
            pop_op_meta_cp(code_pointer, out datatype, out vector_size);
            if (vector_size != 1)
            {
                Debug.LogError($"ssmd.get_rol: n may only be of vectorsize 1 p1 = {datatype}.{vector_size}");
                return;
            }
#else
            // in release we assume compiler did its job
#endif

            bool constant_rol = pop_fx_into_ref<float>(ref code_pointer, fp);

#if DEVELOPMENT_BUILD || TRACE
            if (constant_rol && !UnsafeUtils.AllEqual(fp, ssmd_datacount))
            {
                Debug.LogError($"ssmd.get_rol: constant defined rol does not have all values equal");
                return;
            }
#endif
            if (constant_rol)
            {
                int rol = (int)fp[0]; // save on memory access during loop
                for (int i = 0; i < ssmd_datacount; i++)
                {
                    uint* current = &((uint*)data[i])[dataindex];
                    current[0] = math.rol(current[0], rol);
                }
            }
            else
            {
                for (int i = 0; i < ssmd_datacount; i++)
                {
                    uint* current = &((uint*)data[i])[dataindex];
                    current[0] = math.rol(current[0], (int)fp[i]);
                }
            }


            code_pointer--;
        }
        /// <summary>
        /// rotate bits right in place 
        /// </summary>
        void get_ror_result([NoAlias] void* temp, ref int code_pointer, ref byte vector_size)
        {
            // index would be the same for each ssmd record
            int dataindex = code[code_pointer] - BlastInterpretor.opt_id;
            code_pointer++;

#if DEVELOPMENT_BUILD || TRACE
            // dataindex must point to a valid id 
            if (dataindex < 0 || dataindex + BlastInterpretor.opt_id >= 255)
            {
                Debug.LogError($"ssmd.get_ror: first parameter must directly point to a dataindex but its '{dataindex + BlastInterpretor.opt_id}' instead");
                return;
            }
            BlastVariableDataType datatype = BlastInterpretor.GetMetaDataType(metadata, (byte)dataindex);
            if (datatype != BlastVariableDataType.Bool32)
            {
                Debug.LogError($"ssmd.get_ror: retrieving a bit from a non Bool32 datatype, codepointer = {code_pointer}, data = {datatype}.{BlastInterpretor.GetMetaDataSize(metadata, (byte)dataindex)}");
                return;
            }
#else
            vector_size = 1;
#endif

            float* fp = (float*)temp;

#if DEVELOPMENT_BUILD || TRACE
            pop_op_meta_cp(code_pointer, out datatype, out vector_size);
            if (vector_size != 1)
            {
                Debug.LogError($"ssmd.get_ror: n may only be of vectorsize 1 p1 = {datatype}.{vector_size}");
                return;
            }
#else
            // in release we assume compiler did its job
#endif

            bool constant_ror = pop_fx_into_ref<float>(ref code_pointer, fp);

#if DEVELOPMENT_BUILD || TRACE
            if (constant_ror && !UnsafeUtils.AllEqual(fp, ssmd_datacount))
            {
                Debug.LogError($"ssmd.get_ror: although ror is called with a constant its values are not equal accross all ssmd records at {code_pointer}");
                return;
            }
#endif

            if (constant_ror)
            {
                int ror = (int)fp[0];
                for (int i = 0; i < ssmd_datacount; i++)
                {
                    uint* current = &((uint*)data[i])[dataindex];
                    current[0] = math.ror(current[0], ror);
                }
            }
            else
            {
                for (int i = 0; i < ssmd_datacount; i++)
                {
                    uint* current = &((uint*)data[i])[dataindex];
                    current[0] = math.ror(current[0], (int)fp[i]);
                }
            }

            code_pointer--;
        }
        /// <summary>
        /// shift bits left in place 
        /// </summary>
        void get_shl_result([NoAlias] void* temp, ref int code_pointer, ref byte vector_size)
        {
            // index would be the same for each ssmd record
            int dataindex = code[code_pointer] - BlastInterpretor.opt_id;
            code_pointer++;

#if DEVELOPMENT_BUILD || TRACE
            // dataindex must point to a valid id 
            if (dataindex < 0 || dataindex + BlastInterpretor.opt_id >= 255)
            {
                Debug.LogError($"ssmd.get_shl: first parameter must directly point to a dataindex but its '{dataindex + BlastInterpretor.opt_id}' instead");
                return;
            }
            BlastVariableDataType datatype = BlastInterpretor.GetMetaDataType(metadata, (byte)dataindex);
            if (datatype != BlastVariableDataType.Bool32)
            {
                Debug.LogError($"ssmd.get_shl: retrieving a bit from a non Bool32 datatype, codepointer = {code_pointer}, data = {datatype}.{BlastInterpretor.GetMetaDataSize(metadata, (byte)dataindex)}");
                return;
            }
#else
            vector_size = 1;
#endif

            float* fp = (float*)temp;

#if DEVELOPMENT_BUILD || TRACE
            pop_op_meta_cp(code_pointer, out datatype, out vector_size);
            if (vector_size != 1)
            {
                Debug.LogError($"ssmd.get_shl: n may only be of vectorsize 1 p1 = {datatype}.{vector_size}");
                return;
            }
#else
            // in release we assume compiler did its job
#endif

            bool constant_shift = pop_fx_into_ref<float>(ref code_pointer, fp);

#if DEVELOPMENT_BUILD || TRACE
            if (constant_shift && !UnsafeUtils.AllEqual(fp, ssmd_datacount))
            {
                Debug.LogError($"ssmd.get_shl: shift constant not equal across all ssmd records at codepointer {code_pointer}");
                return;
            }
#endif
            if (constant_shift)
            {
                int shift = (int)fp[0];
                for (int i = 0; i < ssmd_datacount; i++)
                {
                    uint* current = &((uint*)data[i])[dataindex];
                    current[0] = current[0] << shift;
                }
            }
            else
            {
                for (int i = 0; i < ssmd_datacount; i++)
                {
                    uint* current = &((uint*)data[i])[dataindex];
                    current[0] = current[0] << (int)fp[i];
                }
            }
            code_pointer--;
        }
        /// <summary>
        /// shift bits right in place 
        /// </summary>
        void get_shr_result([NoAlias] void* temp, ref int code_pointer, ref byte vector_size)
        {
            // index would be the same for each ssmd record
            int dataindex = code[code_pointer] - BlastInterpretor.opt_id;
            code_pointer++;

#if DEVELOPMENT_BUILD || TRACE
            // dataindex must point to a valid id 
            if (dataindex < 0 || dataindex + BlastInterpretor.opt_id >= 255)
            {
                Debug.LogError($"ssmd.get_shr: first parameter must directly point to a dataindex but its '{dataindex + BlastInterpretor.opt_id}' instead");
                return;
            }
            BlastVariableDataType datatype = BlastInterpretor.GetMetaDataType(metadata, (byte)dataindex);
            if (datatype != BlastVariableDataType.Bool32)
            {
                Debug.LogError($"ssmd.get_shr: retrieving a bit from a non Bool32 datatype, codepointer = {code_pointer}, data = {datatype}.{BlastInterpretor.GetMetaDataSize(metadata, (byte)dataindex)}");
                return;
            }
#else
            vector_size = 1;
#endif

            float* fp = (float*)temp;

#if DEVELOPMENT_BUILD || TRACE
            pop_op_meta_cp(code_pointer, out datatype, out vector_size);
            if (vector_size != 1)
            {
                Debug.LogError($"ssmd.get_shr: n may only be of vectorsize 1 p1 = {datatype}.{vector_size}");
                return;
            }
#else
            // in release we assume compiler did its job
#endif

            bool constant_shift = pop_fx_into_ref<float>(ref code_pointer, fp);

#if DEVELOPMENT_BUILD || TRACE
            if (constant_shift && !UnsafeUtils.AllEqual(fp, ssmd_datacount))
            {
                Debug.LogError($"ssmd.get_shr: shift constant not equal across all ssmd records at codepointer {code_pointer}");
                return;
            }
#endif
            if (constant_shift)
            {
                int shift = (int)fp[0];
                for (int i = 0; i < ssmd_datacount; i++)
                {
                    uint* current = &((uint*)data[i])[dataindex];
                    current[0] = current[0] >> shift;
                }
            }
            else
            {
                for (int i = 0; i < ssmd_datacount; i++)
                {
                    uint* current = &((uint*)data[i])[dataindex];
                    current[0] = current[0] >> (int)fp[i];
                }
            }

            code_pointer--;
        }

        /// <summary>
        /// set zero to given index
        /// </summary>
        void get_zero_result([NoAlias] void* temp, ref int code_pointer, ref byte vector_size)
        {
            // index would be the same for each ssmd record
            int dataindex = code[code_pointer] - BlastInterpretor.opt_id;

#if DEVELOPMENT_BUILD || TRACE
            // dataindex must point to a valid id 
            if (dataindex < 0 || dataindex + BlastInterpretor.opt_id >= 255)
            {
                Debug.LogError($"ssmd.zero: first parameter must directly point to a dataindex but its '{dataindex + BlastInterpretor.opt_id}' instead");
                return;
            }                
#endif

            switch (BlastInterpretor.GetMetaDataSize(in metadata, (byte)dataindex))
            {
                case 1:
                    simd.move_f1_constant_to_indexed_data_as_f1(data, data_rowsize, data_is_aligned, dataindex, 0, ssmd_datacount);
                    break;

                case 2:
                    simd.move_f1_constant_to_indexed_data_as_f2(data, data_rowsize, data_is_aligned, dataindex, 0, ssmd_datacount);
                    break;
                case 3:
                    simd.move_f1_constant_to_indexed_data_as_f3(data, data_rowsize, data_is_aligned, dataindex, 0, ssmd_datacount);
                    break;

                case 4:
                case 0:
                    simd.move_f1_constant_to_indexed_data_as_f4(data, data_rowsize, data_is_aligned, dataindex, 0, ssmd_datacount);
                    break;
            }
        }

        /// <summary>
        /// get size of given data element 
        /// </summary>
        void get_size_result(ref int code_pointer, ref byte vector_size, [NoAlias] float4* f4_result)
        {
            // index would be the same for each ssmd record
            byte c = code[code_pointer];
            int dataindex = c - BlastInterpretor.opt_id;

#if DEVELOPMENT_BUILD || TRACE
            // dataindex must point to a valid id 
            if (dataindex < 0 || dataindex + BlastInterpretor.opt_id >= 255)
            {
                Debug.LogError($"ssmd.size: first parameter must directly point to a dataindex but its '{dataindex + BlastInterpretor.opt_id}' instead");
                return;
            }
#endif
            // get size 
            int size = BlastInterpretor.GetMetaDataSize(in metadata, (byte)dataindex);
            size = math.select(size, 4, size == 0);

            // copy size into result variable 
            vector_size = 1;
            float fsize = (float)size;

            simd.move_f1_constant_into_f4_array_as_f1(f4_result, fsize, ssmd_datacount);
        }

        #endregion

        #region Reinterpret XXXX [DataIndex]

        /// <summary>
        /// reinterpret the value at index as a boolean value (set metadata type to bool32)
        /// </summary>
        void reinterpret_bool32(ref int code_pointer, ref byte vector_size)
        {
            // reinterpret operation would be the same for each ssmd record
            int dataindex = code[code_pointer] - BlastInterpretor.opt_id;

#if DEVELOPMENT_BUILD || TRACE
            // dataindex must point to a valid id 
            if (dataindex < 0 || dataindex + BlastInterpretor.opt_id >= 255)
            {
                Debug.LogError($"ssmd.reinterpret_bool32: first parameter must directly point to a dataindex but its '{dataindex + BlastInterpretor.opt_id}' instead");
                return;
            }

            // validate if within package bounds, catch packaging errors early on 
            // datasize is in bytes 
            if (dataindex >= package.DataSize >> 2)
            {
                Debug.LogError($"ssmd.reinterpret_bool32: compilation or packaging error, dataindex {dataindex} is out of bounds");
                return;
            }

#else
            vector_size = 1;
#endif

            BlastInterpretor.SetMetaData(in metadata, BlastVariableDataType.Bool32, 1, (byte)dataindex);
        }

        /// <summary>
        /// reinterpret the value at index as a float value (set metadata type to float[1])
        /// </summary>
        void reinterpret_float(ref int code_pointer, ref byte vector_size)
        {
            // reinterpret operation would be the same for each ssmd record
            int dataindex = code[code_pointer] - BlastInterpretor.opt_id;

#if DEVELOPMENT_BUILD || TRACE
            // dataindex must point to a valid id 
            if (dataindex < 0 || dataindex + BlastInterpretor.opt_id >= 255)
            {
                Debug.LogError($"ssmd.reinterpret_float: first parameter must directly point to a dataindex but its '{dataindex + BlastInterpretor.opt_id}' instead");
                return;
            }

            // validate if within package bounds, catch packaging errors early on 
            // datasize is in bytes 
            if (dataindex >= package.DataSize >> 2)
            {
                Debug.LogError($"ssmd.reinterpret_bool32: compilation or packaging error, dataindex {dataindex} is out of bounds");
                return;
            }
#else
            vector_size = 1;
#endif

            BlastInterpretor.SetMetaData(in metadata, BlastVariableDataType.Numeric, 1, (byte)dataindex);
        }

        #endregion

        #region External Function Handler 

#if !STANDALONE_VSBUILD
        [SkipLocalsInit]
#endif
        void get_external_function_result([NoAlias] void* temp, ref int code_pointer, ref byte vector_size, [NoAlias] float4* f4)
        {
            // get int id from next 4 ops/bytes 
            int id =
                //(code[code_pointer + 1] << 24)
                //+
                //(code[code_pointer + 2] << 16)
                //+
                (code[code_pointer + 0] << 8)
                +
                code[code_pointer + 1];

            // advance code pointer beyond the script id
            code_pointer += 2;//4;

            // get function pointer 
#if DEVELOPMENT_BUILD || TRACE
            if (engine_ptr->CanBeAValidFunctionId(id))
            {
#endif
            BlastScriptFunction p = engine_ptr->Functions[id];

            // get parameter count (expected from script excluding the 3 data pointers: engine, env, caller) 
            // but dont call it in validation mode 
            if (ValidateOnce == true)
            {
                // external functions (to blast) always have a fixed amount of parameters 
#if DEVELOPMENT_BUILD || TRACE
                    if (id > (int)ReservedBlastScriptFunctionIds.Offset && p.MinParameterCount != p.MaxParameterCount)
                    {
                        Debug.LogError($"blast.ssmd.interpretor.call-external: function {id} min/max parameters should be of equal size");
                    }
#endif
                code_pointer += p.MinParameterCount;
                return;
            }

#if DEVELOPMENT_BUILD || TRACE
                Debug.Log($"call fp id: {id} <env:{environment_ptr.ToInt64()}> <ssmd:no-caller-data>, parmetercount = {p.MinParameterCount}");
#endif


#if DEVELOPMENT_BUILD || TRACE
                vector_size = (byte)math.select((int)p.ReturnsVectorSize, p.AcceptsVectorSize, p.ReturnsVectorSize == 0);
                if (vector_size != 1)
                {
                    Debug.LogError($"blast.ssmd.interpretor.call-external: function {id} a return vector size > 1 is not currently supported in external functions");
                    return;
                }
                if (p.AcceptsVectorSize != 1)
                {
                    Debug.LogError($"blast.ssmd.interpretor.call-external: function {id} a parameter vector size > 1 is not currently supported in external functions");
                    return;
                }
#else
            vector_size = 1;
#endif

            // 
            // first gather up to MinParameterCount parameter components 
            // - external functions have a fixed amount of parameters 
            // - there is no vectorsize check on external functions, user should make sure call is correct or errors will result 
            // 

            int p_count = 0;
            float* p_data = stackalloc float[p.MinParameterCount * ssmd_datacount];

            while (p_count < p.MinParameterCount)
            {
                byte vsize;
                // code_pointer++;

                pop_op_meta_cp(code_pointer, out BlastVariableDataType dtype, out vsize);

                switch (vsize)
                {
                    case 0:
                    case 4:
                        vsize = 4;
                        pop_fx_into_ref<float4>(ref code_pointer, (float4*)(void*)&p_data[p_count * ssmd_datacount]);
                        break;

                    case 1:
                        pop_fx_into_ref<float>(ref code_pointer, (float*)(void*)&p_data[p_count * ssmd_datacount]);
                        break;

                    case 2:
                        pop_fx_into_ref<float2>(ref code_pointer, (float2*)(void*)&p_data[p_count * ssmd_datacount]);
                        break;

                    case 3:
                        pop_fx_into_ref<float3>(ref code_pointer, (float3*)(void*)&p_data[p_count * ssmd_datacount]);
                        break;
                }

                for (int i = 0; i < vsize && p_count < p.MinParameterCount; i++)
                {
                    p_count++;
                }
            }


#if STANDALONE_VSBUILD
            MethodInfo mi = Blast.ScriptAPI.FunctionInfo[id].FunctionDelegate.Method;


            object[] param = new object[p.MinParameterCount + (p.IsShortDefinition ? 0 : 3)];

            int iparam = 0;

            if (!p.IsShortDefinition)
            {
                param[0] = new IntPtr(engine_ptr);
                param[1] = environment_ptr;
                param[2] = IntPtr.Zero; // SSMD has no caller data
                iparam = 3;
            }

            if (!ValidateOnce)
            {
                for (int i = 0; i < ssmd_datacount; i++)
                {
                    for (int j = 0; j < p.MinParameterCount; j++)
                    {
                        param[j + iparam] = p_data[j * ssmd_datacount + i];
                    }

                    f4[i].x = (float)mi.Invoke(null, param);
                }
            }

            vector_size = 1;
            return;
#else
                // the native bursted version is a little more invloved ... 

                if (!ValidateOnce)
                {
                    if (p.IsShortDefinition)
                    {

                        switch (p.MinParameterCount)
                        {
                            // failure case -> could not map delegate
                            case 112:
#if DEVELOPMENT_BUILD || TRACE
                                Debug.LogError($"blast.ssmd.interpretor.call_external: error mapping external fuction pointer, function id {p.FunctionId} parametercount = {p.MinParameterCount}");
#endif
                                for (int i = 0; i < ssmd_datacount; i++)
                                {
                                    f4[i].x = float.NaN;
                                }
                                break;

                            case 0:
                                FunctionPointer<BlastDelegate_f0_s> fp0_s = p.Generic<BlastDelegate_f0_s>();
                                if (fp0_s.IsCreated && fp0_s.Value != IntPtr.Zero)
                                {
                                    for (int i = 0; i < ssmd_datacount; i++) f4[i].x = fp0_s.Invoke();
                                    return;
                                }
                                goto case 112;


                            case 1:
                                FunctionPointer<BlastDelegate_f1_s> fp1_s = p.Generic<BlastDelegate_f1_s>();
                                if (fp1_s.IsCreated && fp1_s.Value != IntPtr.Zero)
                                {
                                    for (int i = 0; i < ssmd_datacount; i++) f4[i].x = fp1_s.Invoke(p_data[i]);
                                    return;
                                }
                                goto case 112;

                            case 2:
                                FunctionPointer<BlastDelegate_f11_s> fp11_s = p.Generic<BlastDelegate_f11_s>();
                                if (fp11_s.IsCreated && fp11_s.Value != IntPtr.Zero)
                                {
                                    for (int i = 0; i < ssmd_datacount; i++)
                                    {
                                        f4[i].x = fp11_s.Invoke(p_data[i], p_data[1 * ssmd_datacount + i]);
                                    }
                                    return;
                                }
                                goto case 112;

                        }
                    }
                    else
                    {
                        switch (p.MinParameterCount)
                        {
                            // zero parameters 
                            case 0:
                                FunctionPointer<BlastDelegate_f0> fp0 = p.Generic<BlastDelegate_f0>();
                                if (fp0.IsCreated && fp0.Value != IntPtr.Zero)
                                {
                                    vector_size = 1;

                                    for (int i = 0; i < ssmd_datacount; i++)
                                    {
                                        f4[i].x = fp0.Invoke((IntPtr)engine_ptr, environment_ptr, IntPtr.Zero);
                                    }
                                    return;
                                }
                                break;

                            //
                            // obviously this would be a whole lot more efficient if we vectorize the external functions 
                            // - this is however not always good: 
                            // - if the function is expensive to execute there would be very little benefit
                            // - small lightweight functions would benefit greatly
                            // - user should be able to optionally register a vectorized variant and we use that here if present
                            // 
                            case 1:
                                FunctionPointer<BlastDelegate_f1> fp1 = p.Generic<BlastDelegate_f1>();
                                if (fp1.IsCreated && fp1.Value != IntPtr.Zero)
                                {
                                    for (int i = 0; i < ssmd_datacount; i++)
                                    {
                                        f4[i].x = fp1.Invoke((IntPtr)engine_ptr, environment_ptr, IntPtr.Zero, p_data[i]);
                                    }
                                    return;
                                }
                                break;

                            case 2:
                                FunctionPointer<BlastDelegate_f11> fp2 = p.Generic<BlastDelegate_f11>();
                                if (fp2.IsCreated && fp2.Value != IntPtr.Zero)
                                {
                                    for (int i = 0; i < ssmd_datacount; i++)
                                    {
                                        f4[i].x = fp2.Invoke((IntPtr)engine_ptr, environment_ptr, IntPtr.Zero, p_data[i], p_data[ssmd_datacount + i]);
                                    }
                                    return;
                                }
                                break;

                            case 3:
                                FunctionPointer<BlastDelegate_f111> fp3 = p.Generic<BlastDelegate_f111>();
                                if (fp3.IsCreated && fp3.Value != IntPtr.Zero)
                                {
                                    for (int i = 0; i < ssmd_datacount; i++)
                                    {
                                        f4[i].x = fp3.Invoke((IntPtr)engine_ptr, environment_ptr, IntPtr.Zero, p_data[i], p_data[ssmd_datacount + i], p_data[2 * ssmd_datacount + i]);
                                    }
                                    return;
                                }
                                break;

                            case 4:
                                FunctionPointer<BlastDelegate_f1111> fp4 = p.Generic<BlastDelegate_f1111>();
                                if (fp4.IsCreated && fp4.Value != IntPtr.Zero)
                                {
                                    for (int i = 0; i < ssmd_datacount; i++)
                                    {
                                        f4[i].x = fp4.Invoke((IntPtr)engine_ptr, environment_ptr, IntPtr.Zero, p_data[i], p_data[ssmd_datacount + i], p_data[2 * ssmd_datacount + i], p_data[3 * ssmd_datacount + i]);
                                    }
                                    return;
                                }
                                break;



#if DEVELOPMENT_BUILD || TRACE
                            default:
                                Debug.LogError($"blast.ssmd.interpretor.call-external: function id {id}, with parametercount {p.MinParameterCount} is not supported yet");
                                break;
#endif
                        }
                    }
                }

#endif


#if DEVELOPMENT_BUILD || TRACE
            }
#endif

#if DEVELOPMENT_BUILD || TRACE
            Debug.LogError($"blast.ssmd.interpretor.call-external: failed to call function pointer with id: {id}");
#endif
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
        int GetFunctionResult([NoAlias] void* temp, ref int code_pointer, ref byte vector_size, [NoAlias] ref float4* f4_result, bool error_if_not_exists = true)
        {
            byte op = code[code_pointer];
            code_pointer++;

            switch ((blast_operation)op)
            {
                // math functions
                //case blast_operation.abs: get_abs_result(temp, ref code_pointer, ref vector_size, f4_result); break;
                case blast_operation.abs: get_single_op_result(temp, ref code_pointer, ref vector_size, f4_result, blast_operation.abs, extended_blast_operation.nop); break;
                case blast_operation.trunc: get_single_op_result(temp, ref code_pointer, ref vector_size, f4_result, blast_operation.trunc, extended_blast_operation.nop); break;

                case blast_operation.maxa: get_op_a_component_result(temp, ref code_pointer, ref vector_size, ref f4_result, blast_operation.maxa); break;
                case blast_operation.mina: get_op_a_component_result(temp, ref code_pointer, ref vector_size, ref f4_result, blast_operation.mina); break;

                case blast_operation.max: get_op_a_result(temp, ref code_pointer, ref vector_size, ref f4_result, blast_operation.max); break;
                case blast_operation.min: get_op_a_result(temp, ref code_pointer, ref vector_size, ref f4_result, blast_operation.min); break;

                case blast_operation.all: get_op_a_result(temp, ref code_pointer, ref vector_size, ref f4_result, blast_operation.and, BlastParameterEncoding.Encode44); break;
                case blast_operation.any: get_op_a_result(temp, ref code_pointer, ref vector_size, ref f4_result, blast_operation.or, BlastParameterEncoding.Encode44); break;

                case blast_operation.select: get_select_result(temp, ref code_pointer, ref vector_size, f4_result); break;
                case blast_operation.fma: get_fma_result(temp, ref code_pointer, ref vector_size, ref f4_result); break;
                case blast_operation.csum: get_op_a_component_result(temp, ref code_pointer, ref vector_size, ref f4_result, blast_operation.csum); break;

                case blast_operation.mula: get_op_a_result(temp, ref code_pointer, ref vector_size, ref f4_result, blast_operation.multiply); break;
                case blast_operation.adda: get_op_a_result(temp, ref code_pointer, ref vector_size, ref f4_result, blast_operation.add); break;
                case blast_operation.suba: get_op_a_result(temp, ref code_pointer, ref vector_size, ref f4_result, blast_operation.substract); break;
                case blast_operation.diva: get_op_a_result(temp, ref code_pointer, ref vector_size, ref f4_result, blast_operation.diva); break;

                case blast_operation.index_x: get_single_op_result(temp, ref code_pointer, ref vector_size, f4_result, blast_operation.index_x, extended_blast_operation.nop); break;
                case blast_operation.index_y: get_single_op_result(temp, ref code_pointer, ref vector_size, f4_result, blast_operation.index_y, extended_blast_operation.nop); break;
                case blast_operation.index_z: get_single_op_result(temp, ref code_pointer, ref vector_size, f4_result, blast_operation.index_z, extended_blast_operation.nop); break;
                case blast_operation.index_w: get_single_op_result(temp, ref code_pointer, ref vector_size, f4_result, blast_operation.index_w, extended_blast_operation.nop); break;

                case blast_operation.expand_v2: expand_f1_into_fn(2, ref code_pointer, ref vector_size, f4_result); break;
                case blast_operation.expand_v3: expand_f1_into_fn(3, ref code_pointer, ref vector_size, f4_result); break;
                case blast_operation.expand_v4: expand_f1_into_fn(4, ref code_pointer, ref vector_size, f4_result); break;

                case blast_operation.get_bit: get_getbit_result(temp, ref code_pointer, ref vector_size, f4_result); break;
                case blast_operation.get_bits: get_getbits_result(temp, ref code_pointer, ref vector_size, f4_result); break;

                case blast_operation.set_bit: get_setbit_result(temp, ref code_pointer, ref vector_size); break;
                case blast_operation.set_bits: get_setbits_result(temp, ref code_pointer, ref vector_size); break;

                case blast_operation.zero: get_zero_result(temp, ref code_pointer, ref vector_size); break;
                case blast_operation.size: get_size_result(ref code_pointer, ref vector_size, f4_result); break;

                case blast_operation.ex_op:
                    {
                        extended_blast_operation exop = (extended_blast_operation)code[code_pointer];
                        code_pointer++;

                        switch (exop)
                        {
                            // external calls 
                            case extended_blast_operation.call: get_external_function_result(temp, ref code_pointer, ref vector_size, f4_result); break;

                            // single inputs
                            case extended_blast_operation.sqrt: get_single_op_result(temp, ref code_pointer, ref vector_size, f4_result, blast_operation.ex_op, extended_blast_operation.sqrt); break;
                            case extended_blast_operation.rsqrt: get_single_op_result(temp, ref code_pointer, ref vector_size, f4_result, blast_operation.ex_op, extended_blast_operation.rsqrt); break;
                            case extended_blast_operation.sin: get_single_op_result(temp, ref code_pointer, ref vector_size, f4_result, blast_operation.ex_op, extended_blast_operation.sin); break;
                            case extended_blast_operation.cos: get_single_op_result(temp, ref code_pointer, ref vector_size, f4_result, blast_operation.ex_op, extended_blast_operation.cos); break;
                            case extended_blast_operation.tan: get_single_op_result(temp, ref code_pointer, ref vector_size, f4_result, blast_operation.ex_op, extended_blast_operation.tan); break;
                            case extended_blast_operation.sinh: get_single_op_result(temp, ref code_pointer, ref vector_size, f4_result, blast_operation.ex_op, extended_blast_operation.sinh); break;
                            case extended_blast_operation.cosh: get_single_op_result(temp, ref code_pointer, ref vector_size, f4_result, blast_operation.ex_op, extended_blast_operation.cosh); break;
                            case extended_blast_operation.atan: get_single_op_result(temp, ref code_pointer, ref vector_size, f4_result, blast_operation.ex_op, extended_blast_operation.atan); break;
                            case extended_blast_operation.degrees: get_single_op_result(temp, ref code_pointer, ref vector_size, f4_result, blast_operation.ex_op, extended_blast_operation.degrees); break;
                            case extended_blast_operation.radians: get_single_op_result(temp, ref code_pointer, ref vector_size, f4_result, blast_operation.ex_op, extended_blast_operation.radians); break;
                            case extended_blast_operation.ceil: get_single_op_result(temp, ref code_pointer, ref vector_size, f4_result, blast_operation.ex_op, extended_blast_operation.ceil); break;
                            case extended_blast_operation.floor: get_single_op_result(temp, ref code_pointer, ref vector_size, f4_result, blast_operation.ex_op, extended_blast_operation.floor); break;

                            case extended_blast_operation.frac: get_single_op_result(temp, ref code_pointer, ref vector_size, f4_result, blast_operation.ex_op, extended_blast_operation.frac); break;
                            case extended_blast_operation.normalize: get_single_op_result(temp, ref code_pointer, ref vector_size, f4_result, blast_operation.ex_op, extended_blast_operation.normalize); break;
                            case extended_blast_operation.saturate: get_single_op_result(temp, ref code_pointer, ref vector_size, f4_result, blast_operation.ex_op, extended_blast_operation.saturate); break;
                            case extended_blast_operation.logn: get_single_op_result(temp, ref code_pointer, ref vector_size, f4_result, blast_operation.ex_op, extended_blast_operation.logn); break;
                            case extended_blast_operation.log10: get_single_op_result(temp, ref code_pointer, ref vector_size, f4_result, blast_operation.ex_op, extended_blast_operation.log10); break;
                            case extended_blast_operation.log2: get_single_op_result(temp, ref code_pointer, ref vector_size, f4_result, blast_operation.ex_op, extended_blast_operation.log2); break;
                            case extended_blast_operation.exp10: get_single_op_result(temp, ref code_pointer, ref vector_size, f4_result, blast_operation.ex_op, extended_blast_operation.exp10); break;
                            case extended_blast_operation.exp: get_single_op_result(temp, ref code_pointer, ref vector_size, f4_result, blast_operation.ex_op, extended_blast_operation.exp); break;
                            case extended_blast_operation.ceillog2: get_single_op_result(temp, ref code_pointer, ref vector_size, f4_result, blast_operation.ex_op, extended_blast_operation.ceillog2); break;
                            case extended_blast_operation.ceilpow2: get_single_op_result(temp, ref code_pointer, ref vector_size, f4_result, blast_operation.ex_op, extended_blast_operation.ceilpow2); break;
                            case extended_blast_operation.floorlog2: get_single_op_result(temp, ref code_pointer, ref vector_size, f4_result, blast_operation.ex_op, extended_blast_operation.floorlog2); break;
                            case extended_blast_operation.fmod: get_fmod_result(temp, ref code_pointer, ref vector_size, f4_result); break;

                            // dual inputs 
                            case extended_blast_operation.pow: get_pow_result(temp, ref code_pointer, ref vector_size, f4_result); break;
                            case extended_blast_operation.cross: get_cross_result(temp, ref code_pointer, ref vector_size, f4_result); break;
                            case extended_blast_operation.dot: get_dot_result(temp, ref code_pointer, ref vector_size, f4_result); break;
                            case extended_blast_operation.atan2: get_atan2_result(temp, ref code_pointer, ref vector_size, f4_result); break;

                            // tripple inputs                 
                            case extended_blast_operation.clamp: get_clamp_result(temp, ref code_pointer, ref vector_size, f4_result); break;
                            case extended_blast_operation.lerp: get_lerp_result(temp, ref code_pointer, ref vector_size, f4_result); break;
                            case extended_blast_operation.unlerp: get_unlerp_result(temp, ref code_pointer, ref vector_size, f4_result); break;
                            case extended_blast_operation.slerp: get_slerp_result(temp, ref code_pointer, ref vector_size, f4_result); break;
                            case extended_blast_operation.nlerp: get_nlerp_result(temp, ref code_pointer, ref vector_size, f4_result); break;

                            // five inputs 
                            // 
                            //  -> build an indexer of ssmd_datacount wide with all dataroots so we can move that like a card over our data? so we dont need to copy 5!!! buffers ... 
                            // 
                            // case extended_blast_operation.remap: get_remap_result(temp, ref code_pointer, ref vector_size, f4_result); break;

                            // bitwise operations
                            case extended_blast_operation.count_bits: get_countbits_result(temp, ref code_pointer, ref vector_size, f4_result); break;
                            case extended_blast_operation.reverse_bits: get_reversebits_result(temp, ref code_pointer, ref vector_size); break;
                            case extended_blast_operation.tzcnt: get_tzcnt_result(temp, ref code_pointer, ref vector_size, f4_result); break;
                            case extended_blast_operation.lzcnt: get_lzcnt_result(temp, ref code_pointer, ref vector_size, f4_result); break;
                            case extended_blast_operation.rol: get_rol_result(temp, ref code_pointer, ref vector_size); break;
                            case extended_blast_operation.ror: get_ror_result(temp, ref code_pointer, ref vector_size); break;
                            case extended_blast_operation.shr: get_shr_result(temp, ref code_pointer, ref vector_size); break;
                            case extended_blast_operation.shl: get_shl_result(temp, ref code_pointer, ref vector_size); break;

                            // datatype reinterpretations 
                            case extended_blast_operation.reinterpret_bool32: reinterpret_bool32(ref code_pointer, ref vector_size); break;
                            case extended_blast_operation.reinterpret_float: reinterpret_float(ref code_pointer, ref vector_size); break;

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
                                return (int)BlastError.ssmd_function_not_handled;
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
                    return (int)BlastError.ssmd_function_not_handled;
            }

            return (int)BlastError.success;
        }


        /// <summary>
        /// this DOES NOT TRACK FUNCTIONS and will be inaccurate when used if current code_pointer is on a function 
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        bool is_last_in_sequence(int i)
        {
            bool is_last_token_in_sequence = 
                (i >= package.CodeSize - 1)
                ||
                (code[i + 1] == (byte)blast_operation.nop || code[i + 1] == (byte)blast_operation.end);

            if(!is_last_token_in_sequence)
            {
                // check multibyte signatures 
                blast_operation op = (blast_operation)code[i]; 
                switch(op)
                {
                    case blast_operation.constant_f1: i += 5; break;  
                    case blast_operation.constant_f1_h: i += 3; break;
                    case blast_operation.constant_long_ref: i += 3; break;
                    case blast_operation.constant_short_ref: i += 1; break;

                    case blast_operation.cdata:
                        Debug.LogError("handle me cdata");
                        break;

                    // for now if next is anything else 
                    case blast_operation.ex_op: return false; 
                }

               is_last_token_in_sequence = (i >= package.CodeSize - 1)
                    ||
                    (code[i] == (byte)blast_operation.nop || code[i] == (byte)blast_operation.end);
            }

            return is_last_token_in_sequence; 
        }

        /// <summary>
        /// process a sequence of operations into a new register value 
        /// </summary>        
#if !STANDALONE_VSBUILD
        [SkipLocalsInit]
#endif
        int GetSequenceResult([NoAlias] void* temp, ref int code_pointer, ref byte vector_size, in DATAREC target_rec = default)
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
                            // check, if target record is set reaching here is an error
                            return math.select((int)BlastError.success, (int)BlastError.error_target_not_set_in_sequence, target_rec.is_set);

                        }

                    case blast_operation.nop:
                        {
                            //
                            // assignments will end with nop and not with end as a compounded statement
                            // at this point vector_size is decisive for the returned value
                            //
                            code_pointer++;

                            // check, if target record is set (set to be filled) reaching here is an error
                            return math.select((int)BlastError.success, (int)BlastError.error_target_not_set_in_sequence, target_rec.is_set);
                        }

                    case blast_operation.ret:
                        // termination of interpretation
                        code_pointer = package.CodeSize + 1;

                        // check, if target record is set reaching here is an error
                        return math.select((int)BlastError.success, (int)BlastError.error_target_not_set_in_sequence, target_rec.is_set);


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
                            float constant = default;
                            byte* p = (byte*)(void*)&constant;
                            {
                                p[0] = code[code_pointer + 1];
                                p[1] = code[code_pointer + 2];
                                p[2] = code[code_pointer + 3];
                                p[3] = code[code_pointer + 4];
                            }
                            code_pointer += 5;
                            
                            if (!is_last_token_in_sequence || !target_rec.is_set)
                            {
                                // put result into register
                                handle_single_op_constant(register, ref vector_size, constant, current_op, extended_blast_operation.nop, minus, not);
                            }
                            else
                            {
                                // == last in sequence 
                                // == target is set
                                // output directly to target and skip end logics 
                                handle_single_op_constant(register, ref vector_size, constant, current_op, extended_blast_operation.nop, minus, not, false, target_rec);
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
                            float constant = default;
                            byte* p = (byte*)(void*)&constant;
                            {
                                p[0] = 0;
                                p[1] = 0;
                                p[2] = code[code_pointer + 1];
                                p[3] = code[code_pointer + 2];
                            }
                            code_pointer += 3;

                            if (!is_last_token_in_sequence || !target_rec.is_set)
                            {
                                // put result into register
                                handle_single_op_constant(register, ref vector_size, constant, current_op, extended_blast_operation.nop, minus, not);
                            }
                            else
                            {
                                // output directly to target and skip end logics 
                                handle_single_op_constant(register, ref vector_size, constant, current_op, extended_blast_operation.nop, minus, not, false, target_rec);
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
                            code_pointer += 1;
                            int c_index = code_pointer - code[code_pointer];
                            code_pointer += 1;

                            float constant = default;
                            byte* p = (byte*)(void*)&constant;

                            if (code[c_index] == (byte)blast_operation.constant_f1)
                            {
                                p[0] = code[c_index + 1];
                                p[1] = code[c_index + 2];
                                p[2] = code[c_index + 3];
                                p[3] = code[c_index + 4];
                            }
                            else
                            {
#if DEVELOPMENT_BUILD || TRACE
                                if (code[c_index] != (byte)blast_operation.constant_f1_h)
                                {
                                    Debug.LogError($"blast.ssmd.getsequenceresult: constant reference error, short constant reference at {code_pointer - 1} points at invalid operation {code[c_index]} at {c_index}");
                                }
#endif
                                // 16 bit encoding
                                p[0] = 0;
                                p[1] = 0;
                                p[2] = code[c_index + 1];
                                p[3] = code[c_index + 2];
                            }

                            if (!is_last_token_in_sequence || !target_rec.is_set)
                            {
                                // put result into register
                                handle_single_op_constant(register, ref vector_size, constant, current_op, extended_blast_operation.nop, minus, not);
                            }
                            else
                            {
                                // output directly to target and skip end logics 
                                handle_single_op_constant(register, ref vector_size, constant, current_op, extended_blast_operation.nop, minus, not, false, target_rec);
                                code_pointer++;
                                return (int)BlastError.success;
                            }

                            minus = false;
                            not = false;
                            indexer = -1;
                            current_op = blast_operation.nop;
                        }
                        continue;

                    case blast_operation.constant_long_ref:
                        {
                            code_pointer += 1;
                            int c_index = code_pointer - ((code[code_pointer] << 8) + code[code_pointer + 1]) + 1;
                            code_pointer += 2;

                            float constant = default;
                            byte* p = (byte*)(void*)&constant;

                            if (code[c_index] == (byte)blast_operation.constant_f1)
                            {
                                p[0] = code[c_index + 1];
                                p[1] = code[c_index + 2];
                                p[2] = code[c_index + 3];
                                p[3] = code[c_index + 4];
                            }
                            else
                            {
#if DEVELOPMENT_BUILD || TRACE
                                if (code[c_index] != (byte)blast_operation.constant_f1_h)
                                {
                                    Debug.LogError($"blast.ssmd.getsequenceresult: constant reference error, long constant reference at {code_pointer - 1} points at invalid operation {code[c_index]} at {c_index}");
                                }
#endif
                                // 16 bit encoding
                                p[0] = 0;
                                p[1] = 0;
                                p[2] = code[c_index + 1];
                                p[3] = code[c_index + 2];
                            }

                            if (!is_last_token_in_sequence || !target_rec.is_set)
                            {
                                // put result into register
                                handle_single_op_constant(register, ref vector_size, constant, current_op, extended_blast_operation.nop, minus, not);
                            }
                            else
                            {
                                // output directly to target and skip end logics 
                                handle_single_op_constant(register, ref vector_size, constant, current_op, extended_blast_operation.nop, minus, not, false, target_rec);
                                code_pointer++;
                                return (int)BlastError.success;
                            }

                            minus = false;
                            not = false;
                            indexer = -1;
                            current_op = blast_operation.nop;
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

                    //
                    // CoreAPI functions mapping to opcodes 
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
                    case blast_operation.expand_v2:
                    case blast_operation.expand_v3:
                    case blast_operation.expand_v4:
                    case blast_operation.get_bit:
                    case blast_operation.get_bits:
                    case blast_operation.set_bit:
                    case blast_operation.set_bits:
                    case blast_operation.zero:
                    case blast_operation.size:
                    case blast_operation.ex_op:
                        //
                        // on no operation pending
                        //
                        //  - we need a walker to walk the code bytes to determine if this is the last get_function so we can directly set target location 
                        //
                        if (current_op == 0)
                        {
                            // move directly to result
                            GetFunctionResult(f4_temp, ref code_pointer, ref vector_size, ref f4_result);
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
                            GetFunctionResult(f4_temp, ref code_pointer, ref vector_size, ref f4);
                            break;
                        }


#if DEVELOPMENT_BUILD || TRACE
                    case blast_operation.seed:
                        Assert.IsTrue(false, "NOT IMPLEMENTED YET");
                        break;
#endif

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
                                switch(vector_size)
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
                    if (nesting_level <= 0 && target_rec.is_set && is_last_token_in_sequence

                        &&
                        //
                        // limit to size 1 vectors for shortly 
                        //
                        vector_size == 1 && prev_vector_size == 1
                        )
                    {
                        // directly put result into target-rec 
                        handle_in_getsequence_operation_to_target(ref vector_size, current_op, ref not, ref minus, f4, f4_result, prev_vector_size, target_rec);
                        code_pointer++;
                        return (int)BlastError.success; 
                    }
                    else
                    {
                        // put result into register 
                        handle_in_getsequence_operation_to_register(ref vector_size, current_op, ref not, ref minus, f4, f4_result, prev_vector_size);
                    }
                }

                // reset current operation when last thing was a value  (otherwise wont get here) 
                current_op = blast_operation.nop;

                // next
                code_pointer++;
            }

            // check, if target record is set reaching here is an error
            return math.select((int)BlastError.success, (int)BlastError.error_target_not_set_in_sequence, target_rec.is_set); 
        }

        /// <summary>
        /// pulled out of getsequence for refactoring : handle op and put result back into target record location 
        /// </summary>
        void handle_in_getsequence_operation_to_target(ref byte vector_size, blast_operation current_op, ref bool not, ref bool minus, float4* f4, float4* f4_result, byte prev_vector_size, in DATAREC target)
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
                        case BlastVectorSizes.float1: handle_op_f1_f1(current_op, f4_result, f4, ref vector_size, minus, not, in target); break;
                        case BlastVectorSizes.float2: handle_op_f1_f2(current_op, f4_result, f4, ref vector_size, minus, not, target); break;
               //         case BlastVectorSizes.float3: handle_op_f1_f3(current_op, f4_result, f4, ref vector_size, minus, not, target); break;
               //         case BlastVectorSizes.float4: handle_op_f1_f4(current_op, f4_result, f4, ref vector_size, minus, not, target); break;
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
               //         case BlastVectorSizes.float1: handle_op_f2_f1(current_op, f4_result, f4, ref vector_size, minus, not, target); break;
               //         case BlastVectorSizes.float2: handle_op_f2_f2(current_op, f4_result, f4, ref vector_size, minus, not, target); break;
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
               //         case BlastVectorSizes.float1: handle_op_f3_f1(current_op, f4_result, f4, ref vector_size, minus, not, target); break;
               //         case BlastVectorSizes.float3: handle_op_f3_f3(current_op, f4_result, f4, ref vector_size, minus, not, target); break;

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
               //         case BlastVectorSizes.float1: handle_op_f4_f1(current_op, f4_result, f4, ref vector_size, minus, not, target); break;
              //         case BlastVectorSizes.float4: handle_op_f4_f4(current_op, f4_result, f4, ref vector_size, minus, not, target); break;
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
        void handle_in_getsequence_operation_to_register(ref byte vector_size, blast_operation current_op, ref bool not, ref bool minus, float4* f4, float4* f4_result, byte prev_vector_size)
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
                        case BlastVectorSizes.float1: handle_op_f1_f1(current_op, f4_result, f4, ref vector_size); break;
                        case BlastVectorSizes.float2: handle_op_f1_f2(current_op, f4_result, f4, ref vector_size); break;
                        case BlastVectorSizes.float3: handle_op_f1_f3(current_op, f4_result, f4, ref vector_size); break;
                        case BlastVectorSizes.float4: handle_op_f1_f4(current_op, f4_result, f4, ref vector_size); break;
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
                        case BlastVectorSizes.float1: handle_op_f2_f1(current_op, f4_result, f4, ref vector_size); break;
                        case BlastVectorSizes.float2: handle_op_f2_f2(current_op, f4_result, f4, ref vector_size); break;
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
                        case BlastVectorSizes.float1: handle_op_f3_f1(current_op, f4_result, f4, ref vector_size); break;
                        case BlastVectorSizes.float3: handle_op_f3_f3(current_op, f4_result, f4, ref vector_size); break;

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
                        case BlastVectorSizes.float1: handle_op_f4_f1(current_op, f4_result, f4, ref vector_size); break;
                        case BlastVectorSizes.float4: handle_op_f4_f4(current_op, f4_result, f4, ref vector_size); break;
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
                BlastError res = (BlastError)GetSequenceResult(temp, ref code_pointer, ref vector_size);
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
            BlastError res = (BlastError)GetSequenceResult(temp, ref code_pointer, ref vector_size);
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
            BlastError res = (BlastError)GetFunctionResult(temp, ref code_pointer, ref vector_size, ref register);
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


        #region Assign[s|f] operation handlers

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
        BlastError assigns(ref int code_pointer, ref byte vector_size, [NoAlias] void* temp)
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
            // the compiler should have cought this, if it didt wel that would be noticed before going in release
            // so we define this only for debug saving the if
            // cannot set a component from a vector (yet)
            if (vector_size != 1 && is_indexed)
            {
                Debug.LogError($"blast.ssmd.interpretor.assignsingle: cannot set component from vector at #{code_pointer}, component: {indexer}");
                return BlastError.error_assign_component_from_vector;
            }
#endif

            // read into nxt value
            code_pointer++;

            // set at index depending on size of the assigned
            // s_assignee = data; 
            switch (s_assignee)
            {
                case 1:
                    pop_fx_into_ref<float>(ref code_pointer, null, data, assignee, data_is_aligned, data_rowsize);  
                    vector_size = 1;
                    break;

                case 2:
                    pop_fx_into_ref<float>(ref code_pointer, null, data, get_offset_from_indexed(in assignee, in indexer, in is_indexed), data_is_aligned, data_rowsize);
                    vector_size = 2;
                    break;

                case 3:
                    pop_fx_into_ref<float>(ref code_pointer, null, data, get_offset_from_indexed(in assignee, in indexer, in is_indexed), data_is_aligned, data_rowsize);
                    vector_size = 3;
                    break;

                case 0:
                case 4:
                    pop_fx_into_ref<float>(ref code_pointer, null, data, get_offset_from_indexed(in assignee, in indexer, in is_indexed), data_is_aligned, data_rowsize);
                    vector_size = 4;
                    break;
            }

            return BlastError.success;
        }


        const bool ASSIGN_TO_TARGET = true; 

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
                BlastError res = (BlastError)GetSequenceResult(temp, ref code_pointer, ref vector_size, in data_target);
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
                BlastError res = (BlastError)GetSequenceResult(temp, ref code_pointer, ref vector_size);
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

        BlastError assignf(ref int code_pointer, ref byte vector_size, [NoAlias] void* temp, bool minus)
        {
#if DEVELOPMENT_BUID || TRACE
            Assert.IsFalse(temp == null, "blast.ssmd.interpretor.assignf: temp buffer = NULL");
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
            int res = GetFunctionResult(temp, ref code_pointer, ref vector_size, ref register);
            code_pointer++;

#if DEVELOPMENT_BUILD || TRACE
            // compiler should have cought this 
            if (vector_size != 1 && is_indexed)
            {
                Debug.LogError($"blast.ssmd.assignf: cannot set component from vector at #{code_pointer}, component: {indexer}");
                return BlastError.error_assign_component_from_vector;
            }
#endif

#if DEVELOPMENT_BUILD || TRACE
            // 4 == 0 depending on decoding 
            s_assignee = (byte)math.select(s_assignee, 4, s_assignee == 0);
            if (!is_indexed && s_assignee != vector_size)
            {
                Debug.LogError($"SSMD Interpretor: assign function, vector size mismatch at #{code_pointer}, should be size '{s_assignee}', function returned '{vector_size}', data id = {assignee}");
                return BlastError.error_assign_vector_size_mismatch;
            }
#endif
            set_data_from_register(
                vector_size,
                get_offset_from_indexed(in assignee, in indexer, in is_indexed),
                minus);

            return BlastError.success;
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
            get_external_function_result(temp, ref code_pointer, ref vector_size, register);
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
        BlastError assign_cdata(ref int code_pointer, ref byte vector_size, [NoAlias] void* temp)
        {
            int cdata_offset, cdata_length;

            BlastInterpretor.follow_cdataref(code, code_pointer - 1, out cdata_offset, out cdata_length);
            code_pointer += 2;

            int index = -1;
            bool constant_index;
            bool constant_data;
            
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
                case blast_operation.assignfn: 
                case blast_operation.assignfen:
                case blast_operation.assignv:  
                case blast_operation.assign: index = 0; constant_index = true; break; 

                // anything else should result in an error
                default:
                    return BlastError.error_failed_to_read_cdata_indexer; 
            }

            // validate index if its constant 
            if(constant_index && (index < 0 || index > cdata_length / 4))
            {
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

                case blast_operation.assigns:
                    {
                        code_pointer++;
                        if (constant_index)
                        {
                            // can reuse temp buffer, we force pop_fx_into_ref into reading only 1 record into the buffer
                            pop_op_meta_cp(code_pointer, out assigned_datatype, out vector_size); 
                            switch(vector_size)
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
                        // code_pointer++;
                        // get_function_result(ref code_pointer, ref vector_size, out f4_register);
                        return BlastError.error_failed_to_interpret_cdata_assignment;
                    }


                case blast_operation.assignfe:  
                case blast_operation.assignfn:  
                case blast_operation.assignfen: 
                case blast_operation.assignv:   
                case blast_operation.assign:    

                default:
                    return BlastError.error_failed_to_interpret_cdata_assignment; 
            }

            return BlastError.error_failed_to_interpret_cdata_assignment; 
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
                        if ((ires = (int)push(ref code_pointer, ref vector_size, temp)) < 0) return ires;
                        break;

                    //
                    // pushv knows more about what it is pushing up the stack, it can do it without running the compound 
                    //
                    case blast_operation.pushv:
                        if ((ires = (int)pushv(ref code_pointer, ref vector_size, temp)) < 0) return ires;
                        break;

                    //
                    // push the result from a function directly onto the stack, saving a call to getcompound 
                    //
                    case blast_operation.pushf:
                        if ((ires = (int)pushf(ref code_pointer, ref vector_size, temp)) < 0) return ires;
                        break;

                    //
                    // push the result from a compound directly onto the stack 
                    //
                    case blast_operation.pushc:
                        if ((ires = (int)pushc(ref code_pointer, ref vector_size, temp)) < 0) return ires;
                        break;

                    case blast_operation.assigns:
                        if ((ires = (int)assigns(ref code_pointer, ref vector_size, temp)) < 0) return ires;
                        break;

                    case blast_operation.assignf:
                        if ((ires = (int)assignf(ref code_pointer, ref vector_size, temp, false)) < 0) return ires;
                        break;

                    case blast_operation.assignfe:
                        if ((ires = (int)assignfe(ref code_pointer, ref vector_size, temp, false)) < 0) return ires;
                        break;

                    case blast_operation.assignfn:
                        if ((ires = (int)assignf(ref code_pointer, ref vector_size, temp, true)) < 0) return ires;
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
                        if ((ires = (int)assign_cdata(ref code_pointer, ref vector_size, temp)) < 0) return ires;
                        break;

                    // fixed jump -> no problem 
                    case blast_operation.jump:
                        code_pointer = code_pointer + code[code_pointer];
                        break;

                    // fixed jump -> no sync problem 
                    case blast_operation.jump_back:
                        code_pointer = code_pointer - code[code_pointer];
                        break;

                    // fixed long-jump, singed (forward && backward)
                    case blast_operation.long_jump:
                        code_pointer = code_pointer + ((short)((code[code_pointer] << 8) + code[code_pointer + 1])) - 1;
                        break;

                    //
                    // conditional jumps -> generate a sync point after evaluation of condition 
                    //
                    case blast_operation.jnz:


                        // calc endlocation of 
                        int offset = code[code_pointer];
                        int jump_to = code_pointer + offset;

                        // eval condition => TODO -> check if we still write the begin... 
                        code_pointer += math.select(1, 2, code[code_pointer + 1] == (byte)blast_operation.begin);

                        // eval all push commands before running the compound
                        bool is_push = true;
                        do
                        {
                            switch ((blast_operation)code[code_pointer])
                            {
                                case blast_operation.pushc:
                                    code_pointer++;
                                    pushc(ref code_pointer, ref vector_size, temp);
                                    code_pointer++;
                                    break;
                                case blast_operation.push:
                                    code_pointer++;
                                    push(ref code_pointer, ref vector_size, temp);
                                    code_pointer++;
                                    break;
                                case blast_operation.pushf:
                                    code_pointer++;
                                    pushf(ref code_pointer, ref vector_size, temp);
                                    code_pointer++;
                                    break;
                                case blast_operation.pushv:
                                    code_pointer++;
                                    pushv(ref code_pointer, ref vector_size, temp);
                                    code_pointer++;
                                    break;
                                default:
                                    is_push = false;
                                    break;
                            }
                        }
                        while (is_push && code_pointer < package.CodeSize);

                        // get result from condition 
                        int res = GetSequenceResult(temp, ref code_pointer, ref vector_size);
                        if (res != 0)
                        {
#if DEVELOPMENT_BUILD || TRACE
                            Debug.LogError($"blast.ssmd.interpretor: operation {(byte)op} '{op}', codepointer = {code_pointer}, sync buffer overflow, increase syncbuffer depth or reduce nesting");
#endif
                            return res;
                        }

                        // enter a new synclevel
                        // if (syncbuffer_level == syncbuffer_depth)
                        {
#if DEVELOPMENT_BUILD || TRACE
                            Debug.LogError($"blast.ssmd.interpretor: operation {(byte)op} '{op}', codepointer = {code_pointer}, sync buffer overflow, increase syncbuffer depth or reduce nesting");
#endif
                            return (int)BlastError.error_ssmd_sync_buffer_to_deep;
                        }

                    // code_pointer = math.select(code_pointer, jump_to, f4_register[i].x != 0);
                    //break; 


                    default:
                        {
                            code_pointer--;
                            if (GetFunctionResult(temp, ref code_pointer, ref vector_size, ref f4_register, false) == (int)BlastError.ssmd_function_not_handled)
                            {
                                // if not a function in root, then its nothing 
                                // - if not in debug keep running but with updated codepointer 
#if DEVELOPMENT_BUILD || TRACE
                                Debug.LogError($"blast.ssmd: operation {(byte)op} '{op}' not supported in root codepointer = {code_pointer}");
                                return (int)BlastError.error_unsupported_operation_in_root;
#endif
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