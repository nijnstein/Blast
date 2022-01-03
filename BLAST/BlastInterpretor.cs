#if  true
#define LOG_ERRORS
#define CHECK_STACK
#define HANDLE_DEBUG_OP    // forces inclusion of BurstString.h
#endif

#if NOT_USING_UNITY
// for debug functions 
using NSS.Blast.Standalone;
#else 
// burst wont compile strings in debug functions other then unityengine.debug.log
using UnityEngine;
#endif 

using System;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

namespace NSS.Blast.Interpretor
{
    
    /// <summary>
    /// 
    ///  todo - use state machine instead of get_compound_result and internal grow-vector / operation sequence loop, should reduce load on stack frames
    /// 
    ///   state ---- read root statement ----\ 
    ///       \------read compound ----------/
    ///        \-----grow value-------------/
    ///         \-----get result ----------/
    /// 
    /// 
    /// </summary>
    //[BurstCompile(FloatMode = FloatMode.Fast, FloatPrecision = FloatPrecision.Low, DisableSafetyChecks = true)]
    [BurstCompatible]
    unsafe public struct BlastInterpretor
    {
        /// <summary>
        /// >= opt_value < opt_id is opcode for constant
        /// </summary>
        const byte opt_value = (byte)script_op.pi;

        /// <summary>
        /// >= opt_id is opcode for parameter
        /// </summary>
        const byte opt_id = (byte)script_op.id;

        /// <summary>
        /// maxiumum iteration count, usefull to avoid endless loop bugs
        /// </summary>
        const int max_iterations = 10000;

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
        /// optional pointer to caller information
        /// </summary>
        [NoAlias]
        [NativeDisableUnsafePtrRestriction]
        internal unsafe IntPtr caller_ptr;


        /// <summary>
        /// package info, data segment may not be present depending on action mode
        /// </summary>
        [NoAlias]
        [NativeDisableUnsafePtrRestriction]
        internal unsafe BlastPackage* package;

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
        internal unsafe void* data;

        /// <summary>
        /// metadata segment pointer
        /// </summary>
        [NoAlias]
        [NativeDisableUnsafePtrRestriction]
        internal unsafe byte* metadata;

        /// <summary>
        /// stack segment pointer
        /// </summary>
        [NoAlias]
        [NativeDisableUnsafePtrRestriction]
        internal unsafe float* stack;

        /// <summary>
        /// if true, the script is executed in validation mode:
        /// - external calls just return 0's
        /// - ... 
        /// </summary>
        public bool ValidationMode;

        #region Reset & Set Package

        public void Reset([NoAlias]BlastEngineData* blast)
        {
            engine_ptr = blast;

            switch (package->info.package_mode)
            {
                case BlastPackageMode.CodeDataStack:
                    {
                        package->code_pointer = 0;
                        // stack is shared on data segment 
                        package->stack_offset = package->data_offset;
                    }
                    break;

                case BlastPackageMode.Compiler:
                    {
                        package->code_pointer = 0;
                        // stack is shared on data segment 
                        package->stack_offset = package->data_offset;
                    }
                    break;

                default:
                case BlastPackageMode.Code:
                    {
                        package->code_pointer = 0;
                        // stack seperate 
                        package->stack_offset = 0;
                    }
                    break;

            }
        }

        /// <summary>
        /// we might need to know if we need to copy back data  (or we use a dummy)
        /// </summary>
        public void SetPackage(BlastPackage* ptr)
        {
            package = ptr;

            byte* code_segment = (byte*)ptr;
            code_segment = code_segment + 32;

            code = code_segment;
            data = (float*)(code_segment + ptr->data_start);
            metadata = (byte*)(code_segment + ptr->data_sizes_start);
            stack = (float*)(code_segment + ptr->data_offset);
        }

        /// <summary>
        /// set package with code and data segment seperate
        /// </summary>
        /// <param name="ptr"></param>
        /// <param name="data_segment"></param>
        public void SetPackage(BlastPackage* ptr, byte* data_segment)
        {
            package = ptr;
            code = (byte*)ptr + 32;

            data = (float*)data_segment;
            metadata = (byte*)(data_segment + (ptr->data_sizes_start - ptr->data_start));
            stack = (float*)(data_segment + (ptr->data_offset - ptr->data_start));
        }


        /// <summary>
        /// set package data from package and seperate buffers 
        /// </summary>
        /// <param name="ptr"></param>
        /// <param name="_code"></param>
        /// <param name="_data"></param>
        /// <param name="_metadata"></param>
        /// <param name="_stack"></param>
        public void SetPackage(BlastPackage* ptr, byte* _code, float* _data, byte* _metadata, float* _stack)
        {
            package = ptr;
            code = _code;
            data = _data;
            metadata = _metadata;
            stack = _stack;
        }

        #endregion

        #region Execute Overloads       
        /// <summary>
        /// execute bytecode
        /// </summary>
        /// <param name="blast">engine data</param>
        /// <param name="resume_state">resume state from yield or reset state to initial</param>
        /// <returns>success of coarse</returns>
        unsafe int execute([NoAlias]BlastEngineData* blast, [NoAlias]IntPtr environment, [NoAlias]IntPtr caller, bool resume_state)
        {
            float4 register = float4.zero;

            // yield state or reset it otherwise
            if (resume_state & peek(1))
            {
                // resume execution, pop frame counter 
                pop(out register.x);

                if (register.x > 0)
                {
                    register--;
                    push(register);
                    // return to yield state
                    return (int)BlastError.yield;
                }
                else
                {
                    // pop state and resume execution
                    pop(out register);
                }
            }
            else
            {
                Reset(blast);
            }

            // killing yield.. 
            int code_pointer = 0;
            return execute(blast, environment, caller, register, ref code_pointer); // ref package->code_pointer);
        }

        /// <summary>
        /// execute bytecode
        /// </summary>
        /// <param name="blast">engine data</param>
        /// <returns>exit code</returns>
        public int Execute([NoAlias]IntPtr blast, [NoAlias]IntPtr environment, [NoAlias]IntPtr caller)
        {
            return execute((BlastEngineData*)blast, environment, caller, false);
        }

        /// <summary>
        /// execute bytecode
        /// </summary>
        public int Execute([NoAlias]IntPtr blast)
        {
            return execute((BlastEngineData*)blast, IntPtr.Zero, IntPtr.Zero, false);
        }

        /// <summary>
        /// resume executing bytecode after yielding
        /// </summary>
        /// <param name="blast">engine data</param>
        /// <returns>exit code</returns>
        public int ResumeYield([NoAlias]IntPtr blast, [NoAlias]IntPtr environment, [NoAlias]IntPtr caller)
        {
            return execute((BlastEngineData*)blast, environment, caller, true);
        }

        /// <summary>
        /// resume executing bytecode after yielding
        /// </summary>
        public int ResumeYield([NoAlias]IntPtr blast)
        {
            return execute((BlastEngineData*)blast, IntPtr.Zero, IntPtr.Zero, true);
        }
        #endregion

        #region Stack 

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void push(float f)
        {
#if CHECK_STACK
       //     if (data_offset + stack_offset + 1 > data_capacity)
       //     {
       //         Standalone.Debug.LogError($"blast.stack: attempting to push [{f}] onto a full stack, stacksize = {data_capacity - data_offset}");
       //          return;
       //     }
#endif
            ushort offset = package->stack_offset;

            ((float*)data)[offset] = f;
            SetMetaData(metadata, BlastVariableDataType.Numeric, 1, (byte)offset);

            offset += 1;
            package->stack_offset = offset;
        }

        public void push(int i)
        {
#if CHECK_STACK
        //   if (data_offset + stack_offset + 1 > data_capacity)
        //    {
        //        Standalone.Debug.LogError($"blast.stack: attempting to push [{f}] onto a full stack, stacksize = {data_capacity - data_offset}");
        //        return;
        //    }
#endif
            ushort offset = package->stack_offset;

            ((int*)data)[offset] = i;
            SetMetaData(metadata, BlastVariableDataType.Numeric, 1, (byte)offset);

            offset += 1;
            package->stack_offset = offset;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void push(float4 f)
        {
#if CHECK_STACK
        //    if (data_offset + stack_offset + 4 > data_capacity)
        //    {
        //        Standalone.Debug.LogError($"blast.stack: attempting to push [{f}] onto a full stack, stacksize = {data_capacity - data_offset}");
        //        return;
        //    }
#endif
            ushort offset = package->stack_offset;
            
            float* fdata = (float*)data; 
            fdata[offset] = f.x;
            fdata[offset + 1] = f.y;
            fdata[offset + 2] = f.z;
            fdata[offset + 3] = f.w;

            offset += 4;
            package->stack_offset = offset;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void push(int4 i)
        {
#if CHECK_STACK
         //   if (data_offset + stack_offset + 4 > data_capacity)
         //   {
         //       Standalone.Debug.LogError($"blast.stack: attempting to push [{f}] onto a full stack, stacksize = {data_capacity - data_offset}");
         //       return;
         //   }
#endif
            ushort offset = package->stack_offset;

            float* fdata = (float*)data;
            fdata[offset] = i.x;
            fdata[offset + 1] = i.y;
            fdata[offset + 2] = i.z;
            fdata[offset + 3] = i.w;

            SetMetaData(metadata, BlastVariableDataType.ID, 4, (byte)offset);

            offset += 4;
            package->stack_offset = offset;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void pop(out float f)
        {
            int stack_offset = (ushort)(package->stack_offset - 1);

#if CHECK_STACK
            if (stack_offset < 0)
            {
                Standalone.Debug.LogError($"blast.stack: attempting to pop too much data (1 float) from the stack, offset = {stack_offset}");
                f = float.NaN; 
                return;
            }
            if (GetMetaDataType(metadata, (byte)stack_offset) != BlastVariableDataType.Numeric)
            {
                Standalone.Debug.LogError($"blast.stack: pop(float) type mismatch, stack contains Integer/ID datatype");
                f = float.NaN;
                return;
            }
            byte size = GetMetaDataSize(metadata, (byte)stack_offset); 
            if (size != 1)
            {
                Standalone.Debug.LogError($"blast.stack: pop(float) size mismatch, stack contains vectorsize {size} datatype while size 1 is expected");
                f = float.NaN;
                return;
            }
#endif

            f = ((float*)data)[stack_offset];
            package->stack_offset = (ushort)stack_offset;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void pop(out int i)
        {
            int stack_offset = (package->stack_offset - 1);

#if CHECK_STACK
            if (stack_offset < 0)
            {
                Standalone.Debug.LogError($"blast.stack: attempting to pop too much data (1 int) from the stack, offset = {stack_offset}");
                i = int.MinValue;
                return;
            }
            if (GetMetaDataType(metadata, (byte)stack_offset) != BlastVariableDataType.ID)
            {
                Standalone.Debug.LogError($"blast.stack: pop(int) type mismatch, stack contains Numeric datatype");
                i = int.MinValue; 
                return;
            }
            byte size = GetMetaDataSize(metadata, (byte)stack_offset);
            if (size != 1)
            {
                Standalone.Debug.LogError($"blast.stack: pop(int) size mismatch, stack contains vectorsize {size} datatype while size 1 is expected");
                i = int.MinValue; 
                return;
            }
#endif

            i = ((int*)data)[stack_offset];
            package->stack_offset = (ushort)stack_offset;
        }



        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void pop(out float4 f)
        {
            int stack_offset = package->stack_offset - 4;
#if CHECK_STACK
            if (stack_offset < 0)
            {
                Standalone.Debug.LogError($"blast.stack: attempting to pop too much data (4 numerics) from the stack, offset = {stack_offset}");
                f = float.NaN;
                return;
            }
            if (GetMetaDataType(metadata, (byte)stack_offset) != BlastVariableDataType.Numeric)
            {
                Standalone.Debug.LogError($"blast.stack: pop(numeric4) type mismatch, stack contains ID datatype");
                f = float.NaN;
                return;
            }
            byte size = GetMetaDataSize(metadata, (byte)stack_offset);
            if (size != 4)
            {
                Standalone.Debug.LogError($"blast.stack: pop(numeric4) size mismatch, stack contains vectorsize {size} datatype while size 4 is expected");
                f = float.NaN;
                return;
            }
#endif
            float* fdata = (float*)data;
            f.x = fdata[stack_offset];
            f.y = fdata[stack_offset + 1];
            f.z = fdata[stack_offset + 2];
            f.w = fdata[stack_offset + 3];
            package->stack_offset = (ushort)stack_offset;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void pop(out int4 i)
        {
            int stack_offset = package->stack_offset - 4;
#if CHECK_STACK
            if (stack_offset < 0)
            {
                Standalone.Debug.LogError($"blast.stack: attempting to pop too much data (4 ints) from the stack, offset = {stack_offset}");
                i = int.MinValue;
                return;
            }
            BlastVariableDataType type = GetMetaDataType(metadata, (byte)stack_offset);
            if (type != BlastVariableDataType.ID)
            {
                Standalone.Debug.LogError($"blast.stack: pop(int4) type mismatch, stack contains {type} datatype");
                i = int.MinValue;
                return;
            }
            byte size = GetMetaDataSize(metadata, (byte)stack_offset);
            if (size != 4)
            {
                Standalone.Debug.LogError($"blast.stack: pop(int4) size mismatch, stack contains vectorsize {size} datatype while size 4 is expected");
                i = int.MinValue;
                return;
            }
#endif
            int* idata = (int*)data;
            i.x = idata[stack_offset];
            i.y = idata[stack_offset + 1];
            i.z = idata[stack_offset + 2];
            i.w = idata[stack_offset + 3];
            package->stack_offset = (ushort)stack_offset;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool peek(int n = 1)
        {
            return package->stack_offset >= n;
        }

        #region pop_or_value

        /// <summary>
        /// pops a variable/constant from data or stack, it only returns a reference to the FIRST variable
        /// if the data is part of a vector pop_next should be used for each component of it 
        /// 
        /// - this shoould not worklike this......................
        /// 
        /// 
        /// </summary>
        /// <param name="code_pointer"></param>
        /// <param name="type"></param>
        /// <param name="vector_size"></param>
        /// <returns></returns>
        internal void* pop_with_info(in int code_pointer, out BlastVariableDataType type, out byte vector_size)
        {
            // we get either a pop instruction or an instruction to get a value 
            byte c = code[code_pointer];

            if (c >= opt_id)
            {
                // variable 
                type = GetMetaDataType(metadata, (byte)(c - opt_id));
                vector_size = GetMetaDataSize(metadata, (byte)(c - opt_id));
                return &((float*)data)[c - opt_id];
            }
            else
            if (c == (byte)script_op.pop)
            {
                // stacked 
                package->stack_offset--;
                type = GetMetaDataType(metadata, (byte)(package->stack_offset - 1));
                vector_size = GetMetaDataSize(metadata, (byte)(package->stack_offset - 1));
                return &stack[package->stack_offset];
            }
            else
            if (c >= opt_value)
            {
                type = BlastVariableDataType.Numeric;
                vector_size = 1; 
                return &engine_ptr->constants[c];
            }
            else
            {
                // error or.. constant by operation value.... 
#if DEBUG
                Standalone.Debug.LogError("BlastInterpretor: pop_or_value -> select op by constant value is not supported");
#endif
                type = BlastVariableDataType.Numeric;
                vector_size = 1;
                return null;
            }
        }

        /// <summary>
        /// 
        /// 1.68 ms on 100000x on the same script as the non-branch version
        /// 
        /// without the branch and 2 selects its 2.23 ms on the same test
        /// </summary>
        /// <param name="code_pointer"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal float pop_or_value(in int code_pointer)
        {
            // we get either a pop instruction or an instruction to get a value 
            byte c = code[code_pointer];

            if (c >= opt_id)
            {
#if CHECK_STACK
                // verify type of data / eventhough it is not stack 
                BlastVariableDataType type = GetMetaDataType(metadata, (byte)(c - opt_id)); 
                int size = GetMetaDataSize(metadata, (byte)(c - opt_id));
                if(size != 1 || type != BlastVariableDataType.Numeric)
                {
                    Standalone.Debug.LogError($"blast.stack, pop_or_value -> data mismatch, expecting numeric of size 1, found {type} of size {size} at data offset {c - opt_id}");
                    return float.NaN;
                }
#endif 
                // variable acccess
                return ((float*)data)[c - opt_id];
            }
            else
            if (c == (byte)script_op.pop)
            {
#if CHECK_STACK
                // verify type of data / eventhough it is not stack 
                BlastVariableDataType type = GetMetaDataType(metadata, (byte)(package->stack_offset - 1));
                int size = GetMetaDataSize(metadata, (byte)(package->stack_offset - 1));
                if (size != 1 || type != BlastVariableDataType.Numeric)
                {
                    Standalone.Debug.LogError($"blast.stack, pop_or_value -> stackdata mismatch, expecting numeric of size 1, found {type} of size {size} at stack offset {package->stack_offset - 1}");
                    return float.NaN;
                }
#endif         
                // stack pop 
                package->stack_offset = (ushort)(package->stack_offset - 1);
                return stack[package->stack_offset];
            }
            else
            if (c >= opt_value)
            {
#if CHECK_STACK
                // verify type of data / eventhough it is not stack 
                //BlastVariableDataType type = GetMetaDataType(metadata, (byte)(c - opt_value));
                //int size = GetMetaDataSize(metadata, (byte)(c - opt_value));
                //if (size != 1 || type != BlastVariableDataType.Numeric)
                //{
                    //Standalone.Debug.LogError($"blast.stack, pop_or_value -> constant data mismatch, expecting numeric of size 1, found {type} of size {size} at offset {c - opt_value}");
                    //return float.NaN;
               // }
#endif         
                // op encoded constant                
                // //
                return engine_ptr->constants[c];
            }
            else
            {
                // error or.. constant by operation value.... 
#if DEBUG
                Standalone.Debug.LogError("BlastInterpretor: pop_or_value -> select op by constant value is not supported");
#endif
                return float.NaN;
            }
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal float4 pop_or_value_4(in int code_pointer)
        {
            // no constant check for vectors, we dont use it

            // expect a single pop,  NOT A POPV inside functions that have var length params and vectorsize
            // vector size is always known when used inside vector functions 
            byte c = code[code_pointer];
            float* fdata = (float*)data; 

            if (c >= opt_id)
            {
#if CHECK_STACK
                // verify type of data / eventhough it is not stack 
                BlastVariableDataType type = GetMetaDataType(metadata, (byte)(c - opt_id));
                int size = GetMetaDataSize(metadata, (byte)(c - opt_id));
                if (size != 4 || type != BlastVariableDataType.Numeric)
                {
                    Standalone.Debug.LogError($"blast.stack, pop_or_value4 -> data mismatch, expecting numeric of size 4, found {type} of size {size} at data offset {c - opt_id}");
                    return float.NaN;
                }
#endif 

                int offset = c - opt_id;
                return new float4(fdata[offset + 0], fdata[offset + 1], fdata[offset + 2], fdata[offset + 3]);
            }
            else
            // pop4
            if (c == (byte)script_op.pop4)
            {
                int stack_offset = package->stack_offset - 4; 
#if CHECK_STACK
                // verify type of data / eventhough it is not stack 
                BlastVariableDataType type = GetMetaDataType(metadata, (byte)stack_offset);
                int size = GetMetaDataSize(metadata, (byte)stack_offset);
                if (size != 4 || type != BlastVariableDataType.Numeric)
                {
                    Standalone.Debug.LogError($"blast.stack, pop_or_value4 -> stackdata mismatch, expecting numeric of size 1, found {type} of size {size} at stack offset {package->stack_offset - 1}");
                    return float.NaN;
                }
#endif         

                package->stack_offset = (ushort)( package->stack_offset - 4);
                return new float4(stack[package->stack_offset + 0], stack[package->stack_offset + 1], stack[package->stack_offset + 2], stack[package->stack_offset + 3]);
            }
            else
            if (c >= opt_value)
            {
#if CHECK_STACK
                // verify type of data / eventhough it is not stack 
                BlastVariableDataType type = GetMetaDataType(metadata, (byte)(c - opt_value));
                int size = GetMetaDataSize(metadata, (byte)(c - opt_value));
                if (size != 4 || type != BlastVariableDataType.Numeric)
                {
                    Standalone.Debug.LogError($"blast.stack, pop_or_value4 -> constant data mismatch, expecting numeric of size 4, found {type} of size {size} at offset {c - opt_value}");
                    return float.NaN;
                }
#endif         
                // constant                
                Standalone.Debug.LogWarning("constant vectors ");
                return engine_ptr->constants[c];
            }
            else
            {
                // error or.. constant by operation value.... 
#if DEBUG
                Standalone.Debug.LogError("BlastInterpretor: pop_or_value4 -> select op by constant value is not supported");
#endif
                return float.NaN;
            }
        }
        #endregion


        #endregion

        #region Metadata 

        //
        // - the interpretor needs metadata for determining datatype and vector in some situations (example: pops to functions accepting multiple types and sizes..)
        // - ONLY the metadata for the variable data needs to be retained in the package for storage, but for execution it should have room matching stack size 
        //
        // - metadata holds 1 byte of information per datapoint
        // - lower 4 bits = vectorsize   1-16
        // - upper 4 bits = datatype     16 possible, 2 used 
        //
        // - on storage, metadata needs 1*datacount bytes
        // - on execution it needs 1*datacount + 1*stack_capacity bytes
        // 
        //  

        //

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static internal void SetMetaData(in byte* metadata, in BlastVariableDataType type, in byte size, in byte offset)
        {
            byte meta = (byte)((byte)(size & 0b0000_1111) | (byte)((byte)type << 4));
            metadata[offset] = meta; 
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static internal byte GetMetaDataSize(in byte* metadata, in byte offset)
        {
            return (byte)(metadata[offset] & 0b0000_1111);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static internal BlastVariableDataType GetMetaDataType(in byte* metadata, in byte offset)
        {
            return (BlastVariableDataType)((byte)(metadata[offset] & 0b1111_0000) >> 4);
        }


        #endregion

        #region Yield

        /// <summary>
        /// yield - stacks state
        /// - note that it consumes 20 bytes of stack space (on top of max used by script) to use yield
        /// </summary>
        /// <param name="f4_register"></param>
        internal void yield(float4 f4_register)
        {
            // stack state 
            push(f4_register);

            // get nr of frames to yield 
            byte b = code[package->code_pointer];
            if (b >= opt_value)
            {
                push(pop_or_value(package->code_pointer));
            }
            else
            {
                // return next frame (remember to pop everything back to correct state) 
                //  -> allow b to be a number < opt_value in opcode
                push(b);
            }
        }
        #endregion

        #region Static Execution Helpers

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool UpdateCurrentVector(ref byte vector_size, float f1, ref float4 f4)
        {
            switch ((BlastVectorSizes)vector_size)
            {
                case BlastVectorSizes.float1:
                    f4.x = f1;
                    break;
                case BlastVectorSizes.float2:
                    break;
                case BlastVectorSizes.float3:
                    break;
                case BlastVectorSizes.float4:
#if DEBUG
                    Standalone.Debug.LogError("interpretor error, expanding vector beyond size 4, this is not supported");
#endif
                    return false;
            }
            vector_size++;
            return true;
        }


        #endregion

        #region Operations Handlers 

        // 
        // HANDLE OPERATION 
        //
        //    - 1 copy foreach datatype....... generics fail me here
        // 
        
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        static void handle_op(in script_op op, in byte current, in byte previous, ref float4 a, in float4 b, ref bool last_is_op_or_first, ref byte vector_size)
        {
            last_is_op_or_first = false;
            switch (op)
            {
                default:
                case script_op.nop:
                    {
                        a = b;
                    }
                    return;

                case script_op.add: a = a + b; return;
                case script_op.substract: a = a - b; return;
                case script_op.multiply: a = a * b; return;
                case script_op.divide: a = a / b; return;

                case script_op.and: a = math.select(0f, 1f, math.all(a) && math.all(b)); return;
                case script_op.or: a = math.select(0f, 1f, math.any(a) || math.any(b)); return;
                case script_op.xor: a = math.select(0f, 1f, math.any(a) ^ math.any(b)); return;

                case script_op.greater: a = math.select(0f, 1f, a > b); return;
                case script_op.smaller: a = math.select(0f, 1f, a < b); return;
                case script_op.smaller_equals: a = math.select(0f, 1f, a <= b); return;
                case script_op.greater_equals: a = math.select(0f, 1f, a >= b); return;
                case script_op.equals: a = math.select(0f, 1f, a == b); return;

                case script_op.not:
#if UNITY_EDITOR || LOG_ERRORS
                    Standalone.Debug.LogError("not");
#endif
                    return;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        static void handle_op(in script_op op, in byte current, in byte previous, ref float4 a, in float3 b, ref bool last_is_op_or_first, ref byte vector_size)
        {
            last_is_op_or_first = false;
            switch (op)
            {
                default:
                case script_op.nop:
                    {
                        a.xyz = b;
                    }
                    return;

                case script_op.add: a.xyz = a.xyz + b; return;
                case script_op.substract: a.xyz = a.xyz - b; return;
                case script_op.multiply: a.xyz = a.xyz * b; return;
                case script_op.divide: a.xyz = a.xyz / b; return;

                case script_op.and: a.xyz = math.select(0f, 1f, math.all(a.xyz) && math.all(b)); return;
                case script_op.or: a.xyz = math.select(0f, 1f, math.any(a.xyz) || math.any(b)); return;
                case script_op.xor: a.xyz = math.select(0f, 1f, math.any(a.xyz) ^ math.any(b)); return;

                case script_op.greater: a.xyz = math.select(0f, 1f, a.xyz > b); return;
                case script_op.smaller: a.xyz = math.select(0f, 1f, a.xyz < b); return;
                case script_op.smaller_equals: a.xyz = math.select(0f, 1f, a.xyz <= b); return;
                case script_op.greater_equals: a.xyz = math.select(0f, 1f, a.xyz >= b); return;
                case script_op.equals: a.xyz = math.select(0f, 1f, a.xyz == b); return;

                case script_op.not:
#if UNITY_EDITOR || LOG_ERRORS
                    Standalone.Debug.LogError("not");
#endif
                    return;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        static void handle_op(in script_op op, in byte current, in byte previous, ref float4 a, in float2 b, ref bool last_is_op_or_first, ref byte vector_size)
        {
            last_is_op_or_first = false;
            switch (op)
            {
                default:
                case script_op.nop:
                    {
                        a.xy = b;
                    }
                    return;

                case script_op.add: a.xy = a.xy + b; return;
                case script_op.substract: a.xy = a.xy - b; return;
                case script_op.multiply: a.xy = a.xy * b; return;
                case script_op.divide: a.xy = a.xy / b; return;

                case script_op.and: a.xy = math.select(0, 1, math.all(a.xy) && math.all(b)); return;
                case script_op.or: a.xy = math.select(0, 1, math.any(a.xy) || math.any(b)); return;
                case script_op.xor: a.xy = math.select(0, 1, math.any(a.xy) ^ math.any(b)); return;

                case script_op.greater: a.xy = math.select(0f, 1f, a.xy > b); return;
                case script_op.smaller: a.xy = math.select(0f, 1f, a.xy < b); return;
                case script_op.smaller_equals: a.xy = math.select(0f, 1f, a.xy <= b); return;
                case script_op.greater_equals: a.xy = math.select(0f, 1f, a.xy >= b); return;
                case script_op.equals: a.xy = math.select(0f, 1f, a.xy == b); return;

                case script_op.not:
#if LOG_ERRORS
                    Standalone.Debug.LogError("not");
#endif
                    return;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        static void handle_op(in script_op op, in byte current, in byte previous, ref float a, in float b, ref bool last_is_op_or_first, ref byte vector_size)
        {
            last_is_op_or_first = false;
            switch (op)
            {
                default: return;
                case script_op.nop:
                    {
                        a = b;
                    }
                    return;

                case script_op.add: a = a + b; return;
                case script_op.substract: a = a - b; return;
                case script_op.multiply: a = a * b; return;
                case script_op.divide: a = a / b; return;

                case script_op.and: a = math.select(0, 1, a != 0 && b != 0); return;
                case script_op.or: a = math.select(0, 1, a != 0 || b != 0); return;
                case script_op.xor: a = math.select(0, 1, a != 0 ^ b != 0); return;

                case script_op.greater: a = math.select(0f, 1f, a > b); return;
                case script_op.smaller: a = math.select(0f, 1f, a < b); return;
                case script_op.smaller_equals: a = math.select(0f, 1f, a <= b); return;
                case script_op.greater_equals: a = math.select(0f, 1f, a >= b); return;
                case script_op.equals: a = math.select(0f, 1f, a == b); return;

                case script_op.not:
#if UNITY_EDITOR || LOG_ERRORS
                    Standalone.Debug.LogError("not");
#endif
                    return;
            }
        }

        #endregion

        static void Handle_DebugData(int code_pointer, byte vector_size, int op_id, BlastVariableDataType datatype, void* pdata)
        {
            if (pdata != null)
            {
                // in burst we can only directly log to debug.. so this gets verbose .. 
                switch (datatype)
                {
                    case BlastVariableDataType.ID:
                        int* idata = (int*)pdata;  
                        switch (vector_size)
                        {
                            case 1:  Debug.Log($"Blast.Debug - codepointer: {code_pointer}, id: {op_id}, ID: {idata[0]}, vectorsize: {vector_size}"); break;
                            case 2:  Debug.Log($"Blast.Debug - codepointer: {code_pointer}, id: {op_id}, ID: [{idata[0]}, {idata[1]}], vectorsize: {vector_size}"); break;
                            case 3:  Debug.Log($"Blast.Debug - codepointer: {code_pointer}, id: {op_id}, ID: [{idata[0]}, {idata[1]}, {idata[2]}], vectorsize: {vector_size}"); break;
                            case 4:  Debug.Log($"Blast.Debug - codepointer: {code_pointer}, id: {op_id}, ID: [{idata[0]}, {idata[1]}, {idata[2]}, {idata[3]}], vectorsize: {vector_size}"); break;
                            case 5:  Debug.Log($"Blast.Debug - codepointer: {code_pointer}, id: {op_id}, ID: [{idata[0]}, {idata[1]}, {idata[2]}, {idata[3]}, {idata[4]}], vectorsize: {vector_size}"); break;
                            case 6:  Debug.Log($"Blast.Debug - codepointer: {code_pointer}, id: {op_id}, ID: [{idata[0]}, {idata[1]}, {idata[2]}, {idata[3]}, {idata[4]}, {idata[5]}], vectorsize: {vector_size}"); break;
                            case 7:  Debug.Log($"Blast.Debug - codepointer: {code_pointer}, id: {op_id}, ID: [{idata[0]}, {idata[1]}, {idata[2]}, {idata[3]}, {idata[4]}, {idata[5]}, {idata[6]}], vectorsize: {vector_size}"); break;
                            case 8:  Debug.Log($"Blast.Debug - codepointer: {code_pointer}, id: {op_id}, ID: [{idata[0]}, {idata[1]}, {idata[2]}, {idata[3]}, {idata[4]}, {idata[5]}, {idata[6]}, {idata[7]}], vectorsize: {vector_size}"); break;
                            case 9:  Debug.Log($"Blast.Debug - codepointer: {code_pointer}, id: {op_id}, ID: [{idata[0]}, {idata[1]}, {idata[2]}, {idata[3]}, {idata[4]}, {idata[5]}, {idata[6]}, {idata[7]}, {idata[8]}], vectorsize: {vector_size}"); break;
                            case 10: Debug.Log($"Blast.Debug - codepointer: {code_pointer}, id: {op_id}, ID: [{idata[0]}, {idata[1]}, {idata[2]}, {idata[3]}, {idata[4]}, {idata[5]}, {idata[6]}, {idata[7]}, {idata[8]}, {idata[9]}], vectorsize: {vector_size}"); break;
                            case 11: Debug.Log($"Blast.Debug - codepointer: {code_pointer}, id: {op_id}, ID: [{idata[0]}, {idata[1]}, {idata[2]}, {idata[3]}, {idata[4]}, {idata[5]}, {idata[6]}, {idata[7]}, {idata[8]}, {idata[9]}, {idata[10]}], vectorsize: {vector_size}"); break;
                            case 12: Debug.Log($"Blast.Debug - codepointer: {code_pointer}, id: {op_id}, ID: [{idata[0]}, {idata[1]}, {idata[2]}, {idata[3]}, {idata[4]}, {idata[5]}, {idata[6]}, {idata[7]}, {idata[8]}, {idata[9]}, {idata[10]}, {idata[11]}], vectorsize: {vector_size}"); break;
                            case 13: Debug.Log($"Blast.Debug - codepointer: {code_pointer}, id: {op_id}, ID: [{idata[0]}, {idata[1]}, {idata[2]}, {idata[3]}, {idata[4]}, {idata[5]}, {idata[6]}, {idata[7]}, {idata[8]}, {idata[9]}, {idata[10]}, {idata[11]}, {idata[12]}], vectorsize: {vector_size}"); break;
                            case 14: Debug.Log($"Blast.Debug - codepointer: {code_pointer}, id: {op_id}, ID: [{idata[0]}, {idata[1]}, {idata[2]}, {idata[3]}, {idata[4]}, {idata[5]}, {idata[6]}, {idata[7]}, {idata[8]}, {idata[9]}, {idata[10]}, {idata[11]}, {idata[12]}, {idata[13]}], vectorsize: {vector_size}"); break;
                            case 15: Debug.Log($"Blast.Debug - codepointer: {code_pointer}, id: {op_id}, ID: [{idata[0]}, {idata[1]}, {idata[2]}, {idata[3]}, {idata[4]}, {idata[5]}, {idata[6]}, {idata[7]}, {idata[8]}, {idata[9]}, {idata[10]}, {idata[11]}, {idata[12]}, {idata[13]}, {idata[14]}], vectorsize: {vector_size}"); break;
                            case 16: Debug.Log($"Blast.Debug - codepointer: {code_pointer}, id: {op_id}, ID: [{idata[0]}, {idata[1]}, {idata[2]}, {idata[3]}, {idata[4]}, {idata[5]}, {idata[6]}, {idata[7]}, {idata[8]}, {idata[9]}, {idata[10]}, {idata[11]}, {idata[12]}, {idata[13]}, {idata[14]}, {idata[15]}], vectorsize: {vector_size}"); break;
                            default:
                                Debug.LogError($"Blast.Debug(data) - Vectorsize of {vector_size} is not supported in variable/operation with id {op_id} and datatype ID");
                                break; 
                        }
                        break;

                    case BlastVariableDataType.Numeric:
                        float* fdata = (float*)pdata;
                        switch (vector_size)
                        {
                            case 1: Debug.Log($"Blast.Debug - codepointer: {code_pointer}, id: {op_id}, NUMERIC: {fdata[0].ToString("0.00")}, vectorsize: {vector_size}"); break;
                            case 2: Debug.Log($"Blast.Debug - codepointer: {code_pointer}, id: {op_id}, NUMERIC: [{fdata[0].ToString("0.00")}, {fdata[1].ToString("0.00")}], vectorsize: {vector_size}"); break;
                            case 3: Debug.Log($"Blast.Debug - codepointer: {code_pointer}, id: {op_id}, NUMERIC: [{fdata[0].ToString("0.00")}, {fdata[1].ToString("0.00")}, {fdata[2].ToString("0.00")}], vectorsize: {vector_size}"); break;
                            case 4: Debug.Log($"Blast.Debug - codepointer: {code_pointer}, id: {op_id}, NUMERIC: [{fdata[0].ToString("0.00")}, {fdata[1].ToString("0.00")}, {fdata[2].ToString("0.00")}, {fdata[3].ToString("0.00")}], vectorsize: {vector_size}"); break;
                            case 5: Debug.Log($"Blast.Debug - codepointer: {code_pointer}, id: {op_id}, NUMERIC: [{fdata[0].ToString("0.00")}, {fdata[1].ToString("0.00")}, {fdata[2].ToString("0.00")}, {fdata[3].ToString("0.00")}, {fdata[4].ToString("0.00")}], vectorsize: {vector_size}"); break;
                            case 6: Debug.Log($"Blast.Debug - codepointer: {code_pointer}, id: {op_id}, NUMERIC: [{fdata[0].ToString("0.00")}, {fdata[1].ToString("0.00")}, {fdata[2].ToString("0.00")}, {fdata[3].ToString("0.00")}, {fdata[4].ToString("0.00")}, {fdata[5].ToString("0.00")}], vectorsize: {vector_size}"); break;
                            case 7: Debug.Log($"Blast.Debug - codepointer: {code_pointer}, id: {op_id}, NUMERIC: [{fdata[0].ToString("0.00")}, {fdata[1].ToString("0.00")}, {fdata[2].ToString("0.00")}, {fdata[3].ToString("0.00")}, {fdata[4].ToString("0.00")}, {fdata[5].ToString("0.00")}, {fdata[6].ToString("0.00")}], vectorsize: {vector_size}"); break;
                            case 8: Debug.Log($"Blast.Debug - codepointer: {code_pointer}, id: {op_id}, NUMERIC: [{fdata[0].ToString("0.00")}, {fdata[1].ToString("0.00")}, {fdata[2].ToString("0.00")}, {fdata[3].ToString("0.00")}, {fdata[4].ToString("0.00")}, {fdata[5].ToString("0.00")}, {fdata[6].ToString("0.00")}, {fdata[7].ToString("0.00")}], vectorsize: {vector_size}"); break;
                            case 9: Debug.Log($"Blast.Debug - codepointer: {code_pointer}, id: {op_id}, NUMERIC: [{fdata[0].ToString("0.00")}, {fdata[1].ToString("0.00")}, {fdata[2].ToString("0.00")}, {fdata[3].ToString("0.00")}, {fdata[4].ToString("0.00")}, {fdata[5].ToString("0.00")}, {fdata[6].ToString("0.00")}, {fdata[7].ToString("0.00")}, {fdata[8].ToString("0.00")}], vectorsize: {vector_size}"); break;
                            case 10: Debug.Log($"Blast.Debug - codepointer: {code_pointer}, id: {op_id}, NUMERIC: [{fdata[0].ToString("0.00")}, {fdata[1].ToString("0.00")}, {fdata[2].ToString("0.00")}, {fdata[3].ToString("0.00")}, {fdata[4].ToString("0.00")}, {fdata[5].ToString("0.00")}, {fdata[6].ToString("0.00")}, {fdata[7].ToString("0.00")}, {fdata[8].ToString("0.00")}, {fdata[9].ToString("0.00")}], vectorsize: {vector_size}"); break;
                            case 11: Debug.Log($"Blast.Debug - codepointer: {code_pointer}, id: {op_id}, NUMERIC: [{fdata[0].ToString("0.00")}, {fdata[1].ToString("0.00")}, {fdata[2].ToString("0.00")}, {fdata[3].ToString("0.00")}, {fdata[4].ToString("0.00")}, {fdata[5].ToString("0.00")}, {fdata[6].ToString("0.00")}, {fdata[7].ToString("0.00")}, {fdata[8].ToString("0.00")}, {fdata[9].ToString("0.00")}, {fdata[10].ToString("0.00")}], vectorsize: {vector_size}"); break;
                            case 12: Debug.Log($"Blast.Debug - codepointer: {code_pointer}, id: {op_id}, NUMERIC: [{fdata[0].ToString("0.00")}, {fdata[1].ToString("0.00")}, {fdata[2].ToString("0.00")}, {fdata[3].ToString("0.00")}, {fdata[4].ToString("0.00")}, {fdata[5].ToString("0.00")}, {fdata[6].ToString("0.00")}, {fdata[7].ToString("0.00")}, {fdata[8].ToString("0.00")}, {fdata[9].ToString("0.00")}, {fdata[10].ToString("0.00")}, {fdata[11].ToString("0.00")}], vectorsize: {vector_size}"); break;
                            case 13: Debug.Log($"Blast.Debug - codepointer: {code_pointer}, id: {op_id}, NUMERIC: [{fdata[0].ToString("0.00")}, {fdata[1].ToString("0.00")}, {fdata[2].ToString("0.00")}, {fdata[3].ToString("0.00")}, {fdata[4].ToString("0.00")}, {fdata[5].ToString("0.00")}, {fdata[6].ToString("0.00")}, {fdata[7].ToString("0.00")}, {fdata[8].ToString("0.00")}, {fdata[9].ToString("0.00")}, {fdata[10].ToString("0.00")}, {fdata[11].ToString("0.00")}, {fdata[12].ToString("0.00")}], vectorsize: {vector_size}"); break;
                            case 14: Debug.Log($"Blast.Debug - codepointer: {code_pointer}, id: {op_id}, NUMERIC: [{fdata[0].ToString("0.00")}, {fdata[1].ToString("0.00")}, {fdata[2].ToString("0.00")}, {fdata[3].ToString("0.00")}, {fdata[4].ToString("0.00")}, {fdata[5].ToString("0.00")}, {fdata[6].ToString("0.00")}, {fdata[7].ToString("0.00")}, {fdata[8].ToString("0.00")}, {fdata[9].ToString("0.00")}, {fdata[10].ToString("0.00")}, {fdata[11].ToString("0.00")}, {fdata[12].ToString("0.00")}, {fdata[13].ToString("0.00")}], vectorsize: {vector_size}"); break;
                            case 15: Debug.Log($"Blast.Debug - codepointer: {code_pointer}, id: {op_id}, NUMERIC: [{fdata[0].ToString("0.00")}, {fdata[1].ToString("0.00")}, {fdata[2].ToString("0.00")}, {fdata[3].ToString("0.00")}, {fdata[4].ToString("0.00")}, {fdata[5].ToString("0.00")}, {fdata[6].ToString("0.00")}, {fdata[7].ToString("0.00")}, {fdata[8].ToString("0.00")}, {fdata[9].ToString("0.00")}, {fdata[10].ToString("0.00")}, {fdata[11].ToString("0.00")}, {fdata[12].ToString("0.00")}, {fdata[13].ToString("0.00")}, {fdata[14].ToString("0.00")}], vectorsize: {vector_size}"); break;
                            case 16: Debug.Log($"Blast.Debug - codepointer: {code_pointer}, id: {op_id}, NUMERIC: [{fdata[0].ToString("0.00")}, {fdata[1].ToString("0.00")}, {fdata[2].ToString("0.00")}, {fdata[3].ToString("0.00")}, {fdata[4].ToString("0.00")}, {fdata[5].ToString("0.00")}, {fdata[6].ToString("0.00")}, {fdata[7].ToString("0.00")}, {fdata[8].ToString("0.00")}, {fdata[9].ToString("0.00")}, {fdata[10].ToString("0.00")}, {fdata[11].ToString("0.00")}, {fdata[12].ToString("0.00")}, {fdata[13].ToString("0.00")}, {fdata[14].ToString("0.00")}, {fdata[15].ToString("0.00")}], vectorsize: {vector_size}"); break;
                            default:
                                Debug.LogError($"Blast.Debug(data) - Vectorsize of {vector_size} is not supported in variable/operation with id {op_id} and datatype ID");
                                break;
                        }
                        break;

                    default: 
                        Debug.LogError($"Blast.Debug(data) - Datatype '{datatype}' not supported in variable/operation with id {op_id} and vectorsize {vector_size}");
                        break; 
                }
            }
        }


        float4 CallExternal(ref int code_pointer, ref bool minus, ref byte vector_size, ref float4 f4)
        {
            // get int id from next 4 ops/bytes 
            int id =
                (code[code_pointer + 1] << 24)
                +
                (code[code_pointer + 2] << 16)
                +
                (code[code_pointer + 3] << 8)
                +
                code[code_pointer + 4];

            // advance code pointer beyond the script id
            code_pointer += 4;

            // get function pointer 
            NonGenericFunctionPointer p;
            if (engine_ptr->TryGetFunctionPointer(id, out p))
            {
                // get parameter count (expected from script excluding the 3 data pointers: engine, env, caller) 
                // but dont call it in validation mode 
                if (ValidationMode == true)
                {
                    code_pointer += p.parameter_count;
                    return float4.zero;
                }

               // Debug.Log($"call fp id: {id} {environment_ptr.ToInt64()} {caller_ptr.ToInt64()}");

                switch (p.parameter_count)
                {
                    case 0:
                        FunctionPointer<Blast.BlastDelegate_f0> fp0 = p.Generic<Blast.BlastDelegate_f0>();
                        if (fp0.IsCreated && fp0.Value != IntPtr.Zero)
                        {
                            f4[0] = (int)fp0.Invoke((IntPtr)engine_ptr, environment_ptr, caller_ptr);
                            return v1_eval(f4, ref minus, ref vector_size);
                        }
                        break;

                    case 1:
                        FunctionPointer<Blast.BlastDelegate_f1> fp1 = p.Generic<Blast.BlastDelegate_f1>();
                        if (fp1.IsCreated && fp1.Value != IntPtr.Zero)
                        {
                            float a = pop_or_value(code_pointer + 1);
                            code_pointer++;

                            // Debug.Log($"call fp id: {id} {environment_ptr.ToInt64()} {caller_ptr.ToInt64()}, parameters: {a}");

                            f4[0] = (int)fp1.Invoke((IntPtr)engine_ptr, environment_ptr, caller_ptr, a);
                            return v1_eval(f4, ref minus, ref vector_size);
                        }
                        break;

                    case 2:
                        FunctionPointer<Blast.BlastDelegate_f2> fp2 = p.Generic<Blast.BlastDelegate_f2>();
                        if (fp2.IsCreated && fp2.Value != IntPtr.Zero)
                        {
                            float a = pop_or_value(code_pointer + 1);
                            float b = pop_or_value(code_pointer + 2);
                            code_pointer += 2;

                            // Debug.Log($"call fp id: {id} {environment_ptr.ToInt64()} {caller_ptr.ToInt64()}, parameters: {a}");

                            f4[0] = (int)fp2.Invoke((IntPtr)engine_ptr, environment_ptr, caller_ptr, a, b);
                            return v1_eval(f4, ref minus, ref vector_size);
                        }
                        break;

                    case 3:
                        FunctionPointer<Blast.BlastDelegate_f3> fp3 = p.Generic<Blast.BlastDelegate_f3>();
                        if (fp3.IsCreated && fp3.Value != IntPtr.Zero)
                        {
                            float a = pop_or_value(code_pointer + 1);
                            float b = pop_or_value(code_pointer + 2);
                            float c = pop_or_value(code_pointer + 3);
                            code_pointer += 3;

                            f4[0] = (int)fp3.Invoke((IntPtr)engine_ptr, environment_ptr, caller_ptr, a, b, c);
                            return v1_eval(f4, ref minus, ref vector_size); 
                        }
                        break;

                    case 4:
                        FunctionPointer<Blast.BlastDelegate_f4> fp4 = p.Generic<Blast.BlastDelegate_f4>();
                        if (fp4.IsCreated && fp4.Value != IntPtr.Zero)
                        {
                            float a = pop_or_value(code_pointer + 1);
                            float b = pop_or_value(code_pointer + 2);
                            float c = pop_or_value(code_pointer + 3);
                            float d = pop_or_value(code_pointer + 4);
                            code_pointer += 4;

                            f4[0] = (int)fp4.Invoke((IntPtr)engine_ptr, environment_ptr, caller_ptr, a, b, c, d);
                            return v1_eval(f4, ref minus, ref vector_size);
                        }
                        break;


                    default:
#if LOG_ERRORS
                        Debug.LogError($"function id {id}, with paremetercount {p.parameter_count} is not supported yet");
#endif 
                        break; 
                }

            }
#if LOG_ERRORS
            Debug.LogError($"blast: failed to call function pointer with id: {id}");
#endif
            return new float4(float.NaN, float.NaN, float.NaN, float.NaN);

            float4 v1_eval(float4 f, ref bool isminus, ref byte vectorsize)
            {
                f[0] = math.select(f[0], -f[0], isminus);
                vectorsize = 1;
                isminus = false;
                return f;
            }
        }

        #region Function Handlers 

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        float get_fma_result(ref int code_pointer, ref bool minus)
        {
            float m1 = pop_or_value(code_pointer + 1);
            float m2 = pop_or_value(code_pointer + 2);
            float a1 = pop_or_value(code_pointer + 3);

            float r = math.mad(m1, m2, a1);
            r = math.select(r, -r, minus);

            minus = false;
            code_pointer += 3;

            return r;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        float get_fms_result(ref int code_pointer, ref bool minus)
        {
            float m1 = pop_or_value(code_pointer + 1);
            float m2 = pop_or_value(code_pointer + 2);
            float a1 = pop_or_value(code_pointer + 3);

            float r = math.mad(m1, m2, -a1);
            r = math.select(r, -r, minus);

            minus = false;
            code_pointer += 3;

            return r;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        float get_fsm_result(ref int code_pointer, ref bool minus)
        {
            float m1 = pop_or_value(code_pointer + 1);
            float m2 = pop_or_value(code_pointer + 2);
            float a1 = pop_or_value(code_pointer + 3);

            // 10 - (2 * 3)  == 4
            // (-2 * 3) + 10 == 4

            //float r = a1 - (m1 * m2);
            float r = math.mad(-m1, m2, a1);
            r = math.select(r, -r, minus);

            minus = false;
            code_pointer += 3;

            return r;
        }

        float4 get_select_result(ref int code_pointer, ref bool minus, ref byte vector_size, ref float4 f4)
        {
            // vector size to be set from input?  
            switch ((BlastVectorSizes)vector_size)
            {
                case BlastVectorSizes.float4:
                    {
                        vector_size = 4;
                        f4 = math.select(pop_or_value_4(code_pointer + 1), pop_or_value_4(code_pointer + 2), pop_or_value_4(code_pointer + 3) != 0);
                        f4 = math.select(f4, -f4, minus);
                        code_pointer += 3;
                        break;
                    }

                case BlastVectorSizes.float1:
                    {
                        vector_size = 1;
                        f4.x = math.select(pop_or_value(code_pointer + 1), pop_or_value(code_pointer + 2), pop_or_value(code_pointer + 3) != 0);
                        f4.x = math.select(f4.x, -f4.x, minus);
                        code_pointer += 3;
                        break;
                    }

                case BlastVectorSizes.float2:
                case BlastVectorSizes.float3:
                default:
                    {
#if LOG_ERRORS
                        Standalone.Debug.LogError($"select: vector size '{vector_size}' not supported ");
#endif
                        f4 = new float4(float.NaN, float.NaN, float.NaN, float.NaN);
                        break;
                    }
            }

            minus = false;
            return f4;
        }

        float4 get_random_result(ref int code_pointer, ref bool minus, ref byte vector_size, ref float4 f4)
        {
            // get vector size from next byte  
            code_pointer++;
            byte c = code[code_pointer];

            vector_size = (byte)(c & 0b11);
            c = (byte)((c & 0b1111_1100) >> 2);

            // for random, c specifies range:
            // 0 = 0..1  == default
            // 2 =  2 ops with values after this op    min..max 
            // 1 = 1 ops   0...max 


            switch ((BlastVectorSizes)vector_size)
            {
                case BlastVectorSizes.float1:
                    switch (c)
                    {
                        default:
                            {
#if LOG_ERRORS
                    Standalone.Debug.LogError($"random: range type '{c}' not supported ");
#endif
                                minus = false;
                                return new float4(float.NaN, float.NaN, float.NaN, float.NaN);
                            }

                        case 0: f4.x = engine_ptr->random.NextFloat(); break;
                        case 1: f4.x = engine_ptr->random.NextFloat(pop_or_value(code_pointer + 1)); code_pointer++; break;
                        case 2: f4.x = engine_ptr->random.NextFloat(pop_or_value(code_pointer + 1), pop_or_value(code_pointer + 2)); code_pointer += 2; break;
                    }
                    f4.x = math.select(f4.x, -f4.x, minus);
                    minus = false;
                    vector_size = 1;
                    break;


                case BlastVectorSizes.float4:
                case 0:
                    switch (c)
                    {
                        default:
                            {
#if LOG_ERRORS
                    Standalone.Debug.LogError($"random: range type '{c}' not supported ");
#endif
                                minus = false;
                                f4 = new float4(float.NaN, float.NaN, float.NaN, float.NaN);
                                return f4;
                            }

                        case 0: f4 = engine_ptr->random.NextFloat4(); break;
                        case 1: f4 = engine_ptr->random.NextFloat4(pop_or_value(code_pointer + 1)); code_pointer++; break;
                        case 2: f4 = engine_ptr->random.NextFloat4(pop_or_value(code_pointer + 1), pop_or_value(code_pointer + 2)); code_pointer += 2; break;
                    }
                    f4 = math.select(f4, -f4, minus);
                    minus = false;
                    vector_size = 4;
                    break;

                case BlastVectorSizes.float2:
                case BlastVectorSizes.float3:
                default:
                    {
#if LOG_ERRORS
                    Standalone.Debug.LogError($"random: vector size '{vector_size}' not supported ");
#endif
                        minus = false;
                        f4 = new float4(float.NaN, float.NaN, float.NaN, float.NaN);
                        return f4;
                    }
            }

            return f4;
        }

        float4 get_ceil_result(ref int code_pointer, ref bool minus, ref byte vector_size, ref float4 f4)
        {
            // vector size to be set from input?  
            switch ((BlastVectorSizes)vector_size)
            {
                case BlastVectorSizes.float4:
                    {
                        f4 = math.ceil(pop_or_value_4(code_pointer + 1));
                        f4 = math.select(f4, -f4, minus);
                        vector_size = 4;
                        code_pointer += 1;
                        break;
                    }

                case BlastVectorSizes.float1:
                    {
                        vector_size = 1;
                        f4.x = math.ceil(pop_or_value(code_pointer + 1));
                        f4.x = math.select(f4.x, -f4.x, minus);
                        code_pointer += 1;
                        break;
                    }

                case BlastVectorSizes.float2:
                case BlastVectorSizes.float3:
                default:
                    {
#if LOG_ERRORS
                        Standalone.Debug.LogError($"ceil: vector size '{vector_size}' not supported ");
#endif
                        f4 = new float4(float.NaN, float.NaN, float.NaN, float.NaN);
                        break;
                    }
            }
            minus = false;
            return f4;
        }

        float4 get_floor_result(ref int code_pointer, ref bool minus, ref byte vector_size, ref float4 f4)
        {
            // vector size to be set from input?  
            switch ((BlastVectorSizes)vector_size)
            {
                case BlastVectorSizes.float4:
                    {
                        f4 = math.floor(pop_or_value_4(code_pointer + 1));
                        f4 = math.select(f4, -f4, minus);
                        vector_size = 4;
                        code_pointer += 1;
                        break;
                    }

                case BlastVectorSizes.float1:
                    {
                        vector_size = 1;
                        f4.x = math.floor(pop_or_value(code_pointer + 1));
                        f4.x = math.select(f4.x, -f4.x, minus);
                        code_pointer += 1;
                        break;
                    }

                case BlastVectorSizes.float2:
                case BlastVectorSizes.float3:
                default:
                    {
#if LOG_ERRORS
                        Standalone.Debug.LogError($"floor: vector size '{vector_size}' not supported ");
#endif
                        f4 = new float4(float.NaN, float.NaN, float.NaN, float.NaN);
                        break;
                    }
            }
            minus = false;
            return f4;
        }

        float4 get_frac_result(ref int code_pointer, ref bool minus, ref byte vector_size, ref float4 f4)
        {
            // vector size to be set from input?  
            switch ((BlastVectorSizes)vector_size)
            {
                case BlastVectorSizes.float4:
                    {
                        f4 = math.frac(pop_or_value_4(code_pointer + 1));
                        f4 = math.select(f4, -f4, minus);
                        vector_size = 4;
                        code_pointer += 1;
                        break;
                    }

                case BlastVectorSizes.float1:
                    {
                        vector_size = 1;
                        f4.x = math.frac(pop_or_value(code_pointer + 1));
                        f4.x = math.select(f4.x, -f4.x, minus);
                        code_pointer += 1;
                        break;
                    }

                case BlastVectorSizes.float2:
                case BlastVectorSizes.float3:
                default:
                    {
#if LOG_ERRORS
                        Standalone.Debug.LogError($"frac: vector size '{vector_size}' not supported ");
#endif
                        f4 = new float4(float.NaN, float.NaN, float.NaN, float.NaN);
                        break;
                    }
            }
            minus = false;
            return f4;
        }
 
        float4 get_sqrt_result(ref int code_pointer, ref bool minus, ref byte vector_size, ref float4 f4)
        {
            // vector size to be set from input?  
            switch ((BlastVectorSizes)vector_size)
            {
                case BlastVectorSizes.float4:
                    {
                        f4 = math.sqrt(pop_or_value_4(code_pointer + 1));
                        f4 = math.select(f4, -f4, minus);
                        vector_size = 4;
                        code_pointer += 1;
                        break;
                    }

                case BlastVectorSizes.float1:
                    {
                        vector_size = 1;
                        f4.x = math.sqrt(pop_or_value(code_pointer + 1));
                        f4.x = math.select(f4.x, -f4.x, minus);
                        code_pointer += 1;
                        break;
                    }

                case BlastVectorSizes.float2:
                case BlastVectorSizes.float3:
                default:
                    {
#if LOG_ERRORS
                        Standalone.Debug.LogError($"sqrt: vector size '{vector_size}' not supported ");
#endif
                        f4 = new float4(float.NaN, float.NaN, float.NaN, float.NaN);
                        break;
                    }
            }

            minus = false;
            return f4;
        }

        float4 get_rsqrt_result(ref int code_pointer, ref bool minus, ref byte vector_size, ref float4 f4)
        {
            // vector size to be set from input?  
            switch ((BlastVectorSizes)vector_size)
            {
                case BlastVectorSizes.float4:
                    {
                        f4 = math.rsqrt(pop_or_value_4(code_pointer + 1));
                        f4 = math.select(f4, -f4, minus);
                        vector_size = 4;
                        code_pointer += 1;
                        break;
                    }

                case BlastVectorSizes.float1:
                    {
                        vector_size = 1;
                        f4.x = math.rsqrt(pop_or_value(code_pointer + 1));
                        f4.x = math.select(f4.x, -f4.x, minus);
                        code_pointer += 1;
                        break;
                    }

                case BlastVectorSizes.float2:
                case BlastVectorSizes.float3:
                default:
                    {
#if LOG_ERRORS
                    Standalone.Debug.LogError($"rsqrt: vector size '{vector_size}' not supported ");
#endif
                        f4 = new float4(float.NaN, float.NaN, float.NaN, float.NaN);
                        break;
                    }
            }

            minus = false;
            return f4;
        }

        float4 get_sin_result(ref int code_pointer, ref bool minus, ref byte vector_size, ref float4 f4)
        {
            // vector size to be set from input?  
            switch ((BlastVectorSizes)vector_size)
            {
                case BlastVectorSizes.float4:
                    {
                        f4 = math.sin(pop_or_value_4(code_pointer + 1));
                        f4 = math.select(f4, -f4, minus);
                        vector_size = 4;
                        code_pointer += 1;
                        break;
                    }

                case BlastVectorSizes.float1:
                    {
                        vector_size = 1;
                        f4.x = math.sin(pop_or_value(code_pointer + 1));
                        f4.x = math.select(f4.x, -f4.x, minus);
                        code_pointer += 1;
                        break;
                    }

                case BlastVectorSizes.float2:
                case BlastVectorSizes.float3:
                default:
                    {
#if LOG_ERRORS
                    Standalone.Debug.LogError($"sin: vector size '{vector_size}' not supported ");
#endif
                        f4 = new float4(float.NaN, float.NaN, float.NaN, float.NaN);
                        break;
                    }
            }

            minus = false;
            return f4;
        }
        
        float4 get_cos_result(ref int code_pointer, ref bool minus, ref byte vector_size, ref float4 f4)
        {
            // vector size to be set from input?  
            switch ((BlastVectorSizes)vector_size)
            {
                case BlastVectorSizes.float4:
                    {
                        f4 = math.cos(pop_or_value_4(code_pointer + 1));
                        f4 = math.select(f4, -f4, minus);
                        vector_size = 4;
                        code_pointer += 1;
                        break;
                    }

                case BlastVectorSizes.float1:
                    {
                        vector_size = 1;
                        f4.x = math.cos(pop_or_value(code_pointer + 1));
                        f4.x = math.select(f4.x, -f4.x, minus);
                        code_pointer += 1;
                        break;
                    }

                case BlastVectorSizes.float2:
                case BlastVectorSizes.float3:
                default:
                    {
#if LOG_ERRORS
                    Standalone.Debug.LogError($"cos: vector size '{vector_size}' not supported ");
#endif
                        f4 = new float4(float.NaN, float.NaN, float.NaN, float.NaN);
                        break;
                    }
            }

            minus = false;
            return f4;
        }
        
        float4 get_tan_result(ref int code_pointer, ref bool minus, ref byte vector_size, ref float4 f4)
        {
            // vector size to be set from input?  
            switch ((BlastVectorSizes)vector_size)
            {
                case BlastVectorSizes.float4:
                    {
                        f4 = math.tan(pop_or_value_4(code_pointer + 1));
                        f4 = math.select(f4, -f4, minus);
                        vector_size = 4;
                        code_pointer += 1;
                        break;
                    }

                case BlastVectorSizes.float1:
                    {
                        vector_size = 1;
                        f4.x = math.tan(pop_or_value(code_pointer + 1));
                        f4.x = math.select(f4.x, -f4.x, minus);
                        code_pointer += 1;
                        break;
                    }

                case BlastVectorSizes.float2:
                case BlastVectorSizes.float3:
                default:
                    {
#if LOG_ERRORS
                    Standalone.Debug.LogError($"tan: vector size '{vector_size}' not supported ");
#endif
                        f4 = new float4(float.NaN, float.NaN, float.NaN, float.NaN);
                        break;
                    }
            }

            minus = false;
            return f4;
        }

        float4 get_sinh_result(ref int code_pointer, ref bool minus, ref byte vector_size, ref float4 f4)
        {
            // vector size to be set from input?  
            switch ((BlastVectorSizes)vector_size)
            {
                case BlastVectorSizes.float4:
                    {
                        f4 = math.sinh(pop_or_value_4(code_pointer + 1));
                        f4 = math.select(f4, -f4, minus);
                        vector_size = 4;
                        code_pointer += 1;
                        break;
                    }

                case BlastVectorSizes.float1:
                    {
                        vector_size = 1;
                        f4.x = math.sinh(pop_or_value(code_pointer + 1));
                        f4.x = math.select(f4.x, -f4.x, minus);
                        code_pointer += 1;
                        break;
                    }

                case BlastVectorSizes.float2:
                case BlastVectorSizes.float3:
                default:
                    {
#if LOG_ERRORS
                    Standalone.Debug.LogError($"sinh: vector size '{vector_size}' not supported ");
#endif
                        f4 = new float4(float.NaN, float.NaN, float.NaN, float.NaN);
                        break;
                    }
            }

            minus = false;
            return f4;
        }

        float4 get_atan_result(ref int code_pointer, ref bool minus, ref byte vector_size, ref float4 f4)
        {
            // vector size to be set from input?  
            switch ((BlastVectorSizes)vector_size)
            {
                case BlastVectorSizes.float4:
                    {
                        f4 = math.atan(pop_or_value_4(code_pointer + 1));
                        f4 = math.select(f4, -f4, minus);
                        vector_size = 4;
                        code_pointer += 1;
                        break;
                    }

                case BlastVectorSizes.float1:
                    {
                        vector_size = 1;
                        f4.x = math.atan(pop_or_value(code_pointer + 1));
                        f4.x = math.select(f4.x, -f4.x, minus);
                        code_pointer += 1;
                        break;
                    }

                case BlastVectorSizes.float2:
                case BlastVectorSizes.float3:
                default:
                    {
#if LOG_ERRORS
                    Standalone.Debug.LogError($"atan: vector size '{vector_size}' not supported ");
#endif
                        f4 = new float4(float.NaN, float.NaN, float.NaN, float.NaN);
                        break;
                    }
            }

            minus = false;
            return f4;
        }

        float4 get_cosh_result(ref int code_pointer, ref bool minus, ref byte vector_size, ref float4 f4)
        {
            // vector size to be set from input?  
            switch ((BlastVectorSizes)vector_size)
            {
                case BlastVectorSizes.float4:
                    {
                        f4 = math.cosh(pop_or_value_4(code_pointer + 1));
                        f4 = math.select(f4, -f4, minus);
                        vector_size = 4;
                        code_pointer += 1;
                        break;
                    }

                case BlastVectorSizes.float1:
                    {
                        vector_size = 1;
                        f4.x = math.cosh(pop_or_value(code_pointer + 1));
                        f4.x = math.select(f4.x, -f4.x, minus);
                        code_pointer += 1;
                        break;
                    }

                case BlastVectorSizes.float2:
                case BlastVectorSizes.float3:
                default:
                    {
#if LOG_ERRORS
                    Standalone.Debug.LogError($"cosh: vector size '{vector_size}' not supported ");
#endif
                        f4 = new float4(float.NaN, float.NaN, float.NaN, float.NaN);
                        break;
                    }
            }

            minus = false;
            return f4;
        }

        float4 get_degrees_result(ref int code_pointer, ref bool minus, ref byte vector_size, ref float4 f4)
        {
            // vector size to be set from input?  
            switch ((BlastVectorSizes)vector_size)
            {
                case BlastVectorSizes.float4:
                    {
                        f4 = math.degrees(pop_or_value_4(code_pointer + 1));
                        f4 = math.select(f4, -f4, minus);
                        vector_size = 4;
                        code_pointer += 1;
                        break;
                    }

                case BlastVectorSizes.float1:
                    {
                        vector_size = 1;
                        f4.x = math.degrees(pop_or_value(code_pointer + 1));
                        f4.x = math.select(f4.x, -f4.x, minus);
                        code_pointer += 1;
                        break;
                    }

                case BlastVectorSizes.float2:
                case BlastVectorSizes.float3:
                default:
                    {
#if LOG_ERRORS
                    Standalone.Debug.LogError($"degrees: vector size '{vector_size}' not supported ");
#endif
                        f4 = new float4(float.NaN, float.NaN, float.NaN, float.NaN);
                        break;
                    }
            }

            minus = false;
            return f4;
        }

        float4 get_rad_result(ref int code_pointer, ref bool minus, ref byte vector_size, ref float4 f4)
        {
            // vector size to be set from input?  
            switch ((BlastVectorSizes)vector_size)
            {
                case BlastVectorSizes.float4:
                    {
                        f4 = math.radians(pop_or_value_4(code_pointer + 1));
                        f4 = math.select(f4, -f4, minus);
                        vector_size = 4;
                        code_pointer += 1;
                        break;
                    }

                case BlastVectorSizes.float1:
                    {
                        vector_size = 1;
                        f4.x = math.radians(pop_or_value(code_pointer + 1));
                        f4.x = math.select(f4.x, -f4.x, minus);
                        code_pointer += 1;
                        break;
                    }

                case BlastVectorSizes.float2:
                case BlastVectorSizes.float3:
                default:
                    {
#if LOG_ERRORS
                    Standalone.Debug.LogError($"radians: vector size '{vector_size}' not supported ");
#endif
                        f4 = new float4(float.NaN, float.NaN, float.NaN, float.NaN);
                        break;
                    }
            }

            minus = false;
            return f4;
        }

        float4 get_lerp_result(ref int code_pointer, ref bool minus, ref byte vector_size, ref float4 f4)
        {
            // vector size to be set from input?  
            switch ((BlastVectorSizes)vector_size)
            {
                case BlastVectorSizes.float4:
                    {
                        vector_size = 4;
                        f4 = math.lerp(
                            pop_or_value_4(code_pointer + 1),
                            pop_or_value_4(code_pointer + 2),
                            pop_or_value(code_pointer + 3));
                        f4 = math.select(f4, -f4, minus);
                        code_pointer += 3;
                        break;
                    }

                case BlastVectorSizes.float1:
                    {
                        vector_size = 1;
                        f4.x = math.lerp(pop_or_value(code_pointer + 1), pop_or_value(code_pointer + 2), pop_or_value(code_pointer + 3));
                        f4.x = math.select(f4.x, -f4.x, minus);
                        code_pointer += 3;
                        break;
                    }

                case BlastVectorSizes.float2:
                case BlastVectorSizes.float3:
                default:
                    {
#if LOG_ERRORS
                    Standalone.Debug.LogError($"lerp: vector size '{vector_size}' not supported ");
#endif
                        f4 = new float4(float.NaN, float.NaN, float.NaN, float.NaN);
                        break;
                    }
            }

            minus = false;
            return f4;
        }

        float4 get_slerp_result(ref int code_pointer, ref bool minus, ref byte vector_size, ref float4 f4)
        {
            // vector size to be set from input?  
            switch ((BlastVectorSizes)vector_size)
            {
                case BlastVectorSizes.float4:
                    {
                        vector_size = 4;
                        f4 = math.slerp(
                            new quaternion(pop_or_value_4(code_pointer + 1)),
                            new quaternion(pop_or_value_4(code_pointer + 2)),
                            pop_or_value(code_pointer + 3)).value;
                        code_pointer += 3;
                        break;
                    }

                case BlastVectorSizes.float1:
                case BlastVectorSizes.float2:
                case BlastVectorSizes.float3:
                default:
                    {
#if LOG_ERRORS
                    Standalone.Debug.LogError($"slerp: vector size '{vector_size}' not supported ");
#endif
                        f4 = new float4(float.NaN, float.NaN, float.NaN, float.NaN);
                        break;
                    }
            }

            f4 = math.select(f4, -f4, minus);
            minus = false;
            return f4;
        }

        float4 get_normalize_result(ref int code_pointer, ref bool minus, ref byte vector_size, ref float4 f4)
        {
            // vector size to be set from input?  
            switch ((BlastVectorSizes)vector_size)
            {
                case BlastVectorSizes.float4:
                    {
                        vector_size = 4;
                        f4 = math.normalize(pop_or_value_4(code_pointer + 1));
                        f4 = math.select(f4, -f4, minus);
                        code_pointer += 1;
                        break;
                    }

                case BlastVectorSizes.float2:
                case BlastVectorSizes.float3:
                case BlastVectorSizes.float1:
                default:
                    {
#if LOG_ERRORS
                    Standalone.Debug.LogError($"normalize: vector size '{vector_size}' not supported ");
#endif
                        f4 = new float4(float.NaN, float.NaN, float.NaN, float.NaN);
                        break;
                    }
            }

            minus = false;
            return f4;
        }

        float4 get_clamp_result(ref int code_pointer, ref bool minus, ref byte vector_size, ref float4 f4)
        {
            // vector size to be set from input?  
            switch ((BlastVectorSizes)vector_size)
            {
                case BlastVectorSizes.float4:
                    {
                        vector_size = 4;
                        f4 = math.clamp(
                            pop_or_value_4(code_pointer + 1),
                            pop_or_value_4(code_pointer + 2),
                            pop_or_value_4(code_pointer + 3));
                        f4 = math.select(f4, -f4, minus);
                        code_pointer += 3;
                        break;
                    }

                case BlastVectorSizes.float1:
                    {
                        vector_size = 1;
                        f4.x = math.clamp(pop_or_value(code_pointer + 1), pop_or_value(code_pointer + 2), pop_or_value(code_pointer + 3));
                        f4.x = math.select(f4.x, -f4.x, minus);
                        code_pointer += 3;
                        break;
                    }

                case BlastVectorSizes.float2:
                case BlastVectorSizes.float3:
                default:
                    {
#if LOG_ERRORS
                        Standalone.Debug.LogError($"clamp: vector size '{vector_size}' not supported ");
#endif
                        f4 = new float4(float.NaN, float.NaN, float.NaN, float.NaN);
                        break;
                    }
            }

            minus = false;
            return f4;
        }
        
        float4 get_saturate_result(ref int code_pointer, ref bool minus, ref byte vector_size, ref float4 f4)
        {
            // vector size to be set from input?  
            switch ((BlastVectorSizes)vector_size)
            {
                case BlastVectorSizes.float4:
                    {
                        vector_size = 4;
                        f4 = math.saturate(pop_or_value_4(code_pointer + 1));
                        f4 = math.select(f4, -f4, minus);
                        code_pointer += 1;
                        break;
                    }

                case BlastVectorSizes.float1:
                    {
                        vector_size = 1;
                        f4.x = math.saturate(pop_or_value(code_pointer + 1));
                        f4.x = math.select(f4.x, -f4.x, minus);
                        code_pointer += 1;
                        break;
                    }

                case BlastVectorSizes.float2:
                case BlastVectorSizes.float3:
                default:
                    {
#if LOG_ERRORS
                        Standalone.Debug.LogError($"saturate: vector size '{vector_size}' not supported ");
#endif
                        f4 = new float4(float.NaN, float.NaN, float.NaN, float.NaN);
                        break;
                    }
            }

            minus = false;
            return f4;
        }

        float4 get_pow_result(ref int code_pointer, ref bool minus, ref byte vector_size, ref float4 f4)
        {
            // vector size to be set from input?  
            switch ((BlastVectorSizes)vector_size)
            {
                case BlastVectorSizes.float4:
                    {
                        vector_size = 4;
                        f4 = math.pow(pop_or_value_4(code_pointer + 1), pop_or_value_4(code_pointer + 2));
                        f4 = math.select(f4, -f4, minus);
                        code_pointer += 2;
                        break;
                    }

                case BlastVectorSizes.float1:
                    {
                        vector_size = 1;
                        f4.x = math.pow(pop_or_value(code_pointer + 1), pop_or_value(code_pointer + 2));
                        f4.x = math.select(f4.x, -f4.x, minus);
                        code_pointer += 2;
                        break;
                    }

                case BlastVectorSizes.float2:
                case BlastVectorSizes.float3:
                default:
                    {
#if LOG_ERRORS
                        Standalone.Debug.LogError($"pow: vector size '{vector_size}' not supported ");
#endif
                        f4 = new float4(float.NaN, float.NaN, float.NaN, float.NaN);
                        break;
                    }
            }

            minus = false;
            return f4;
        }

        float4 get_distance_result(ref int code_pointer, ref bool minus, ref byte vector_size, ref float4 f4)
        {
            // vector size to be set from input?  
            switch ((BlastVectorSizes)vector_size)
            {
                case BlastVectorSizes.float4:
                    {
                        vector_size = 4;
                        f4 = math.distance(pop_or_value_4(code_pointer + 1), pop_or_value_4(code_pointer + 2));
                        f4 = math.select(f4, -f4, minus);
                        code_pointer += 2;
                        break;
                    }

                case BlastVectorSizes.float1:
                    {
                        vector_size = 1;
                        f4.x = math.distance(pop_or_value(code_pointer + 1), pop_or_value(code_pointer + 2));
                        f4.x = math.select(f4.x, -f4.x, minus);
                        code_pointer += 2;
                        break;
                    }

                case BlastVectorSizes.float2:
                case BlastVectorSizes.float3:
                default:
                    {
#if LOG_ERRORS
                        Standalone.Debug.LogError($"distance: vector size '{vector_size}' not supported ");
#endif
                        f4 = new float4(float.NaN, float.NaN, float.NaN, float.NaN);
                        break;
                    }
            }

            minus = false;
            return f4;
        }
       
        float4 get_distancesq_result(ref int code_pointer, ref bool minus, ref byte vector_size, ref float4 f4)
        {
            // vector size to be set from input?  
            switch ((BlastVectorSizes)vector_size)
            {
                case BlastVectorSizes.float4:
                    {
                        vector_size = 4;
                        f4 = math.distancesq(pop_or_value_4(code_pointer + 1), pop_or_value_4(code_pointer + 2));
                        f4 = math.select(f4, -f4, minus);
                        code_pointer += 2;
                        break;
                    }

                case BlastVectorSizes.float1:
                    {
                        vector_size = 1;
                        f4.x = math.distancesq(pop_or_value(code_pointer + 1), pop_or_value(code_pointer + 2));
                        f4.x = math.select(f4.x, -f4.x, minus);
                        code_pointer += 2;
                        break;
                    }

                case BlastVectorSizes.float2:
                case BlastVectorSizes.float3:
                default:
                    {
#if LOG_ERRORS
                        Standalone.Debug.LogError($"distancesq: vector size '{vector_size}' not supported ");
#endif
                        f4 = new float4(float.NaN, float.NaN, float.NaN, float.NaN);
                        break;
                    }
            }

            minus = false;
            return f4;
        }

        float4 get_length_result(ref int code_pointer, ref bool minus, ref byte vector_size, ref float4 f4)
        {
            // vector size to be set from input?  
            switch ((BlastVectorSizes)vector_size)
            {
                case BlastVectorSizes.float4:
                    {
                        vector_size = 4;
                        f4 = math.length(pop_or_value_4(code_pointer + 1));
                        f4 = math.select(f4, -f4, minus);
                        code_pointer += 1;
                        break;
                    }

                case BlastVectorSizes.float1:
                    {
                        vector_size = 1;
                        f4.x = math.length(pop_or_value(code_pointer + 1));
                        f4.x = math.select(f4.x, -f4.x, minus);
                        code_pointer += 1;
                        break;
                    }

                case BlastVectorSizes.float2:
                case BlastVectorSizes.float3:
                default:
                    {
#if LOG_ERRORS
                        Standalone.Debug.LogError($"length: vector size '{vector_size}' not supported ");
#endif
                        f4 = new float4(float.NaN, float.NaN, float.NaN, float.NaN);
                        break;
                    }
            }

            minus = false;
            return f4;
        }

        float4 get_lengthsq_result(ref int code_pointer, ref bool minus, ref byte vector_size, ref float4 f4)
        {
            // vector size to be set from input?  
            switch ((BlastVectorSizes)vector_size)
            {
                case BlastVectorSizes.float4:
                    {
                        vector_size = 4;
                        f4 = math.lengthsq(pop_or_value_4(code_pointer + 1));
                        f4 = math.select(f4, -f4, minus);
                        code_pointer += 1;
                        break;
                    }

                case BlastVectorSizes.float1:
                    {
                        vector_size = 1;
                        f4.x = math.lengthsq(pop_or_value(code_pointer + 1));
                        f4.x = math.select(f4.x, -f4.x, minus);
                        code_pointer += 1;
                        break;
                    }

                case BlastVectorSizes.float2:
                case BlastVectorSizes.float3:
                default:
                    {
#if LOG_ERRORS
                        Standalone.Debug.LogError($"lengthsq: vector size '{vector_size}' not supported ");
#endif
                        f4 = new float4(float.NaN, float.NaN, float.NaN, float.NaN);
                        break;
                    }
            }

            minus = false;
            return f4;
        }

        float4 get_exp_result(ref int code_pointer, ref bool minus, ref byte vector_size, ref float4 f4)
        {
            // vector size to be set from input?  
            switch ((BlastVectorSizes)vector_size)
            {
                case BlastVectorSizes.float4:
                    {
                        vector_size = 4;
                        f4 = math.exp(pop_or_value_4(code_pointer + 1));
                        f4 = math.select(f4, -f4, minus);
                        code_pointer += 1;
                        break;
                    }

                case BlastVectorSizes.float1:
                    {
                        vector_size = 1;
                        f4.x = math.exp(pop_or_value(code_pointer + 1));
                        f4.x = math.select(f4.x, -f4.x, minus);
                        code_pointer += 1;
                        break;
                    }

                case BlastVectorSizes.float2:
                case BlastVectorSizes.float3:
                default:
                    {
#if LOG_ERRORS
                        Standalone.Debug.LogError($"exp: vector size '{vector_size}' not supported ");
#endif
                        f4 = new float4(float.NaN, float.NaN, float.NaN, float.NaN);
                        break;
                    }
            }

            minus = false;
            return f4;
        }
       
        float4 get_exp10_result(ref int code_pointer, ref bool minus, ref byte vector_size, ref float4 f4)
        {
            // vector size to be set from input?  
            switch ((BlastVectorSizes)vector_size)
            {
                case BlastVectorSizes.float4:
                    {
                        vector_size = 4;
                        f4 = math.exp10(pop_or_value_4(code_pointer + 1));
                        f4 = math.select(f4, -f4, minus);
                        code_pointer += 1;
                        break;
                    }

                case BlastVectorSizes.float1:
                    {
                        vector_size = 1;
                        f4.x = math.exp10(pop_or_value(code_pointer + 1));
                        f4.x = math.select(f4.x, -f4.x, minus);
                        code_pointer += 1;
                        break;
                    }

                case BlastVectorSizes.float2:
                case BlastVectorSizes.float3:
                default:
                    {
#if LOG_ERRORS
                        Standalone.Debug.LogError($"exp10: vector size '{vector_size}' not supported ");
#endif
                        f4 = new float4(float.NaN, float.NaN, float.NaN, float.NaN);
                        break;
                    }
            }

            minus = false;
            return f4;
        }

        float4 get_log2_result(ref int code_pointer, ref bool minus, ref byte vector_size, ref float4 f4)
        {
            // vector size to be set from input?  
            switch ((BlastVectorSizes)vector_size)
            {
                case BlastVectorSizes.float4:
                    {
                        vector_size = 4;
                        f4 = math.log2(pop_or_value_4(code_pointer + 1));
                        f4 = math.select(f4, -f4, minus);
                        code_pointer += 1;
                        break;
                    }

                case BlastVectorSizes.float1:
                    {
                        vector_size = 1;
                        f4.x = math.log2(pop_or_value(code_pointer + 1));
                        f4.x = math.select(f4.x, -f4.x, minus);
                        code_pointer += 1;
                        break;
                    }

                case BlastVectorSizes.float2:
                case BlastVectorSizes.float3:
                default:
                    {
#if LOG_ERRORS
                        Standalone.Debug.LogError($"log2: vector size '{vector_size}' not supported ");
#endif
                        f4 = new float4(float.NaN, float.NaN, float.NaN, float.NaN);
                        break;
                    }
            }

            minus = false;
            return f4;
        }
     
        float4 get_log_result(ref int code_pointer, ref bool minus, ref byte vector_size, ref float4 f4)
        {
            // vector size to be set from input?  
            switch ((BlastVectorSizes)vector_size)
            {
                case BlastVectorSizes.float4:
                    {
                        vector_size = 4;
                        f4 = math.log(pop_or_value_4(code_pointer + 1));
                        f4 = math.select(f4, -f4, minus);
                        code_pointer += 1;
                        break;
                    }

                case BlastVectorSizes.float1:
                    {
                        vector_size = 1;
                        f4.x = math.log(pop_or_value(code_pointer + 1));
                        f4.x = math.select(f4.x, -f4.x, minus);
                        code_pointer += 1;
                        break;
                    }

                case BlastVectorSizes.float2:
                case BlastVectorSizes.float3:
                default:
                    {
#if LOG_ERRORS
                        Standalone.Debug.LogError($"log: vector size '{vector_size}' not supported ");
#endif
                        f4 = new float4(float.NaN, float.NaN, float.NaN, float.NaN);
                        break;
                    }
            }

            minus = false;
            return f4;
        }

        float4 get_log10_result(ref int code_pointer, ref bool minus, ref byte vector_size, ref float4 f4)
        {
            // vector size to be set from input?  
            switch ((BlastVectorSizes)vector_size)
            {
                case BlastVectorSizes.float4:
                    {
                        vector_size = 4;
                        f4 = math.log10(pop_or_value_4(code_pointer + 1));
                        f4 = math.select(f4, -f4, minus);
                        code_pointer += 1;
                        break;
                    }

                case BlastVectorSizes.float1:
                    {
                        vector_size = 1;
                        f4.x = math.log10(pop_or_value(code_pointer + 1));
                        f4.x = math.select(f4.x, -f4.x, minus);
                        code_pointer += 1;
                        break;
                    }

                case BlastVectorSizes.float2:
                case BlastVectorSizes.float3:
                default:
                    {
#if LOG_ERRORS
                        Standalone.Debug.LogError($"log10: vector size '{vector_size}' not supported ");
#endif
                        f4 = new float4(float.NaN, float.NaN, float.NaN, float.NaN);
                        break;
                    }
            }

            minus = false;
            return f4;
        }

        float4 get_cross_result(ref int code_pointer, ref bool minus, ref byte vector_size, ref float4 f4)
        {
            // vector size to be set from input?  
            switch ((BlastVectorSizes)vector_size)
            {
                case BlastVectorSizes.float4:
                    {
                        vector_size = 4;
                        f4.xyz = math.cross(pop_or_value_4(code_pointer + 1).xyz, pop_or_value_4(code_pointer + 2).xyz);
                        f4 = math.select(f4, -f4, minus);
                        code_pointer += 2;
                        break;
                    }

                case BlastVectorSizes.float1:
                case BlastVectorSizes.float2:
                case BlastVectorSizes.float3:
                default:
                    {
#if LOG_ERRORS
                        Standalone.Debug.LogError($"cross: vector size '{vector_size}' not supported ");
#endif
                        f4 = new float4(float.NaN, float.NaN, float.NaN, float.NaN);
                        break;
                    }
            }

            minus = false;
            return f4;
        }

        float4 get_dot_result(ref int code_pointer, ref bool minus, ref byte vector_size, ref float4 f4)
        {
            // vector size to be set from input?  
            switch ((BlastVectorSizes)vector_size)
            {
                case BlastVectorSizes.float4:
                    {
                        vector_size = 4;
                        f4 = math.dot(pop_or_value_4(code_pointer + 1), pop_or_value_4(code_pointer + 2));
                        f4 = math.select(f4, -f4, minus);
                        code_pointer += 2;
                        break;
                    }

                case BlastVectorSizes.float1:
                    {
                        vector_size = 1;
                        f4.x = math.dot(pop_or_value(code_pointer + 1), pop_or_value(code_pointer + 2));
                        f4.x = math.select(f4.x, -f4.x, minus);
                        code_pointer += 2;
                        break;
                    }

                case BlastVectorSizes.float2:
                case BlastVectorSizes.float3:
                default:
                    {
#if LOG_ERRORS
                        Standalone.Debug.LogError($"dot: vector size '{vector_size}' not supported ");
#endif
                        f4 = new float4(float.NaN, float.NaN, float.NaN, float.NaN);
                        break;
                    }
            }

            minus = false;
            return f4;
        }

        float4 get_maxa_result(ref int code_pointer, ref bool minus, ref byte vector_size, ref float4 f4)
        {
            code_pointer++;
            byte c = code[code_pointer];

            vector_size = (byte)(c & 0b11);
            c = (byte)((c & 0b1111_1100) >> 2);
            switch ((BlastVectorSizes)vector_size)
            {
                case BlastVectorSizes.float1:
                    break;

                case 0:
                case BlastVectorSizes.float4:
                    {
                        vector_size = 1;
                        f4.x = math.cmax(f4);
                        break;
                    }

                case BlastVectorSizes.float2:
                    {
                        vector_size = 1;
                        f4.x = math.cmax(f4.xy);
                        break;
                    }

                case BlastVectorSizes.float3:
                    {
                        vector_size = 1;
                        f4.x = math.cmax(f4.xyz);
                        break;
                    }

                default:
                    {
#if LOG_ERRORS
                    Standalone.Debug.LogError($"maxa: vector size '{vector_size}' not supported ");
#endif
                        minus = false;
                        return new float4(float.NaN, float.NaN, float.NaN, float.NaN);
                    }
            }
            return f4;
        }

        float4 get_mina_result(ref int code_pointer, ref bool minus, ref byte vector_size, ref float4 f4)
        {
            code_pointer++;
            byte c = code[code_pointer];

            vector_size = (byte)(c & 0b11);
            c = (byte)((c & 0b1111_1100) >> 2);
            switch ((BlastVectorSizes)vector_size)
            {
                case BlastVectorSizes.float1:
                    break;

                case 0:
                case BlastVectorSizes.float4:
                    {
                        vector_size = 1;
                        f4.x = math.cmin(f4);
                        break;
                    }

                case BlastVectorSizes.float2:
                    {
                        vector_size = 1;
                        f4.x = math.cmin(f4.xy);
                        break;
                    }

                case BlastVectorSizes.float3:
                    {
                        vector_size = 1;
                        f4.x = math.cmin(f4.xyz);
                        break;
                    }

                default:
                    {
#if LOG_ERRORS
                    Standalone.Debug.LogError($"maxa: vector size '{vector_size}' not supported ");
#endif
                        minus = false;
                        return new float4(float.NaN, float.NaN, float.NaN, float.NaN);
                    }
            }
            return f4;
        }

        float4 get_abs_result(ref int code_pointer, ref bool minus, ref byte vector_size, ref float4 f4)
        {
            code_pointer++;

            // abs is a variable length instruction when using it on vectors because of defining vectorsize 
            // the first byte, if its lower < opt_value (constant/value index) then assume its vectorsize 
            byte b = code[code_pointer];
            if (b < opt_value)
            {
                switch (b)
                {
                    case 4:
                        {
                            // the actual value is encoded in the next byte 
                            code_pointer++;
                            f4 = math.abs(pop_or_value_4(code_pointer));
                            f4 = math.select(f4, -f4, minus);
                            minus = false;
                            vector_size = 4;
                        }
                        break;

                    default:
                        {
#if LOG_ERRORS
                    Standalone.Debug.LogError($"mula: vector size '{vector_size}' not supported ");
#endif
                            f4 = new float4(float.NaN, float.NaN, float.NaN, float.NaN);
                            minus = false;
                        }
                        break;
                }
            }
            else
            {
                // float 1 
                f4.x = math.abs(pop_or_value(code_pointer));
                f4.x = math.select(f4.x, -f4.x, minus);
                minus = false;
                vector_size = 1;
            }

            return f4;
        }

        float4 get_mula_result(ref int code_pointer, ref bool minus, ref byte vector_size)
        {
            code_pointer++;
            byte c = code[code_pointer];

            vector_size = (byte)(c & 0b11);
            c = (byte)((c & 0b1111_1100) >> 2);

            float4 r = float4.zero;
            Unity.Burst.Intrinsics.v128 v1 = new Unity.Burst.Intrinsics.v128();
            Unity.Burst.Intrinsics.v128 v2 = new Unity.Burst.Intrinsics.v128();
            switch (vector_size)
            {
                case 1:
                    {
                        switch (c)
                        {
                            case 1:  // should probably just raise a error on this and case 2 
                                r.x = pop_or_value(code_pointer + 1); //  data[code[code_pointer + 1] - opt_id];
                                code_pointer += c;
                                break;

                            case 2:
                                r.x = pop_or_value(code_pointer + 1) * pop_or_value(code_pointer + 2);
                                code_pointer += c;
                                break;

                            case 3:
                                r.x = pop_or_value(code_pointer + 1) * pop_or_value(code_pointer + 2) * pop_or_value(code_pointer + 3);
                                code_pointer += c;
                                break;

                            case 4:
                                v1.Float0 = pop_or_value(code_pointer + 1);
                                v1.Float1 = pop_or_value(code_pointer + 2);
                                v2.Float0 = pop_or_value(code_pointer + 3);
                                v2.Float1 = pop_or_value(code_pointer + 4);
                                v1 = Unity.Burst.Intrinsics.X86.Sse.mul_ps(v1, v2);
                                r.x = v1.Float0 * v1.Float1;
                                code_pointer += c;
                                break;

                            case 5:
                                v1.Float0 = pop_or_value(code_pointer + 1);
                                v1.Float1 = pop_or_value(code_pointer + 2);
                                v2.Float0 = pop_or_value(code_pointer + 3);
                                v2.Float1 = pop_or_value(code_pointer + 4);

                                v1 = Unity.Burst.Intrinsics.X86.Sse.mul_ps(v1, v2);
                                r.x = v1.Float0 * v1.Float1 * pop_or_value(code_pointer + 5);
                                code_pointer += c;
                                break;

                            case 6:
                                v1.Float0 = pop_or_value(code_pointer + 1);
                                v1.Float1 = pop_or_value(code_pointer + 2);
                                v1.Float2 = pop_or_value(code_pointer + 3);
                                v2.Float0 = pop_or_value(code_pointer + 4);
                                v2.Float1 = pop_or_value(code_pointer + 5);
                                v2.Float2 = pop_or_value(code_pointer + 6);

                                v1 = Unity.Burst.Intrinsics.X86.Sse.mul_ps(v1, v2);

                                r.x = v1.Float0 * v1.Float1 * v1.Float2;
                                code_pointer += c;
                                break;

                            case 7:
                                v1.Float0 = pop_or_value(code_pointer + 1);
                                v1.Float1 = pop_or_value(code_pointer + 2);
                                v1.Float2 = pop_or_value(code_pointer + 3);
                                v2.Float0 = pop_or_value(code_pointer + 4);
                                v2.Float1 = pop_or_value(code_pointer + 5);
                                v2.Float2 = pop_or_value(code_pointer + 6);
                                v1 = Unity.Burst.Intrinsics.X86.Sse.mul_ps(v1, v2);
                                r.x = v1.Float0 * v1.Float1 * v1.Float2 * pop_or_value(code_pointer + 7);
                                code_pointer += c;
                                break;

                            case 8:
                                v1.Float0 = pop_or_value(code_pointer + 1);
                                v1.Float1 = pop_or_value(code_pointer + 2);
                                v1.Float2 = pop_or_value(code_pointer + 3);
                                v1.Float3 = pop_or_value(code_pointer + 4);
                                v2.Float0 = pop_or_value(code_pointer + 5);
                                v2.Float1 = pop_or_value(code_pointer + 6);
                                v2.Float2 = pop_or_value(code_pointer + 7);
                                v2.Float3 = pop_or_value(code_pointer + 8);

                                v1 = Unity.Burst.Intrinsics.X86.Sse.mul_ps(v1, v2);

                                r.x = v1.Float0 * v1.Float1 * v1.Float2 * v1.Float3;
                                code_pointer += c;
                                break;

                            case 9:
                                v1.Float0 = pop_or_value(code_pointer + 1);
                                v1.Float1 = pop_or_value(code_pointer + 2);
                                v1.Float2 = pop_or_value(code_pointer + 3);
                                v1.Float3 = pop_or_value(code_pointer + 4);
                                v2.Float0 = pop_or_value(code_pointer + 5);
                                v2.Float1 = pop_or_value(code_pointer + 6);
                                v2.Float2 = pop_or_value(code_pointer + 7);
                                v2.Float3 = pop_or_value(code_pointer + 8);

                                v1 = Unity.Burst.Intrinsics.X86.Sse.mul_ps(v1, v2);

                                r.x = v1.Float0 * v1.Float1 * v1.Float2 * v1.Float3 * pop_or_value(code_pointer + 9);
                                code_pointer += c;
                                break;

                            case 10:
                                v1.Float0 = pop_or_value(code_pointer + 1);
                                v1.Float1 = pop_or_value(code_pointer + 2);
                                v1.Float2 = pop_or_value(code_pointer + 3);
                                v1.Float3 = pop_or_value(code_pointer + 4);
                                v2.Float0 = pop_or_value(code_pointer + 5);
                                v2.Float1 = pop_or_value(code_pointer + 6);
                                v2.Float2 = pop_or_value(code_pointer + 7);
                                v2.Float3 = pop_or_value(code_pointer + 8);

                                v1 = Unity.Burst.Intrinsics.X86.Sse.mul_ps(v1, v2);

                                v2.Float0 = pop_or_value(code_pointer + 9);
                                v2.Float1 = pop_or_value(code_pointer + 10);
                                v2.Float2 = v1.Float3; // save multiplication by v1.float3 later
                                v2.Float3 = 1;

                                v1 = Unity.Burst.Intrinsics.X86.Sse.mul_ps(v1, v2);

                                r.x = v1.Float0 * v1.Float1 * v1.Float2;// * v1.Float3;
                                code_pointer += c;
                                break;

                            case 11:
                                v1.Float0 = pop_or_value(code_pointer + 1);
                                v1.Float1 = pop_or_value(code_pointer + 2);
                                v1.Float2 = pop_or_value(code_pointer + 3);
                                v1.Float3 = pop_or_value(code_pointer + 4);
                                v2.Float0 = pop_or_value(code_pointer + 5);
                                v2.Float1 = pop_or_value(code_pointer + 6);
                                v2.Float2 = pop_or_value(code_pointer + 7);
                                v2.Float3 = pop_or_value(code_pointer + 8);

                                v1 = Unity.Burst.Intrinsics.X86.Sse.mul_ps(v1, v2);

                                v2.Float0 = pop_or_value(code_pointer + 9);
                                v2.Float1 = pop_or_value(code_pointer + 10);
                                v2.Float2 = pop_or_value(code_pointer + 11);
                                v2.Float3 = 1;

                                v1 = Unity.Burst.Intrinsics.X86.Sse.mul_ps(v1, v2);

                                r.x = v1.Float0 * v1.Float1 * v1.Float2 * v1.Float3;
                                code_pointer += c;
                                break;

                            case 12:
                                v1.Float0 = pop_or_value(code_pointer + 1);
                                v1.Float1 = pop_or_value(code_pointer + 2);
                                v1.Float2 = pop_or_value(code_pointer + 3);
                                v1.Float3 = pop_or_value(code_pointer + 4);
                                v2.Float0 = pop_or_value(code_pointer + 5);
                                v2.Float1 = pop_or_value(code_pointer + 6);
                                v2.Float2 = pop_or_value(code_pointer + 7);
                                v2.Float3 = pop_or_value(code_pointer + 8);

                                v1 = Unity.Burst.Intrinsics.X86.Sse.mul_ps(v1, v2);

                                v2.Float0 = pop_or_value(code_pointer + 9);
                                v2.Float1 = pop_or_value(code_pointer + 10);
                                v2.Float2 = pop_or_value(code_pointer + 11);
                                v2.Float3 = pop_or_value(code_pointer + 12);

                                v1 = Unity.Burst.Intrinsics.X86.Sse.mul_ps(v1, v2);

                                r.x = v1.Float0 * v1.Float1 * v1.Float2 * v1.Float3;
                                code_pointer += c;
                                break;

                            case 13:
                                v1.Float0 = pop_or_value(code_pointer + 1);
                                v1.Float1 = pop_or_value(code_pointer + 2);
                                v1.Float2 = pop_or_value(code_pointer + 3);
                                v1.Float3 = pop_or_value(code_pointer + 4);
                                v2.Float0 = pop_or_value(code_pointer + 5);
                                v2.Float1 = pop_or_value(code_pointer + 6);
                                v2.Float2 = pop_or_value(code_pointer + 7);
                                v2.Float3 = pop_or_value(code_pointer + 8);

                                v1 = Unity.Burst.Intrinsics.X86.Sse.mul_ps(v1, v2);

                                v2.Float0 = pop_or_value(code_pointer + 9);
                                v2.Float1 = pop_or_value(code_pointer + 10);
                                v2.Float2 = pop_or_value(code_pointer + 11);
                                v2.Float3 = pop_or_value(code_pointer + 12);

                                v1 = Unity.Burst.Intrinsics.X86.Sse.mul_ps(v1, v2);

                                r.x = v1.Float0 * v1.Float1 * v1.Float2 * v1.Float3 * pop_or_value(code_pointer + 13);
                                code_pointer += c;
                                break;

                            case 14:
                                v1.Float0 = pop_or_value(code_pointer + 1);
                                v1.Float1 = pop_or_value(code_pointer + 2);
                                v1.Float2 = pop_or_value(code_pointer + 3);
                                v1.Float3 = pop_or_value(code_pointer + 4);
                                v2.Float0 = pop_or_value(code_pointer + 5);
                                v2.Float1 = pop_or_value(code_pointer + 6);
                                v2.Float2 = pop_or_value(code_pointer + 7);
                                v2.Float3 = pop_or_value(code_pointer + 8);

                                v1 = Unity.Burst.Intrinsics.X86.Sse.mul_ps(v1, v2);

                                v2.Float0 = pop_or_value(code_pointer + 9);
                                v2.Float1 = pop_or_value(code_pointer + 10);
                                v2.Float2 = pop_or_value(code_pointer + 11);
                                v2.Float3 = pop_or_value(code_pointer + 12);

                                v1 = Unity.Burst.Intrinsics.X86.Sse.mul_ps(v1, v2);

                                v2.Float0 = pop_or_value(code_pointer + 13);
                                v2.Float1 = pop_or_value(code_pointer + 14);
                                v2.Float2 = v1.Float3; // save multiplication by v1.float3 later
                                v2.Float3 = 1;

                                v1 = Unity.Burst.Intrinsics.X86.Sse.mul_ps(v1, v2);

                                r.x = v1.Float0 * v1.Float1 * v1.Float2;
                                code_pointer += c;
                                break;

                            case 15:
                                v1.Float0 = pop_or_value(code_pointer + 1);
                                v1.Float1 = pop_or_value(code_pointer + 2);
                                v1.Float2 = pop_or_value(code_pointer + 3);
                                v1.Float3 = pop_or_value(code_pointer + 4);
                                v2.Float0 = pop_or_value(code_pointer + 5);
                                v2.Float1 = pop_or_value(code_pointer + 6);
                                v2.Float2 = pop_or_value(code_pointer + 7);
                                v2.Float3 = pop_or_value(code_pointer + 8);

                                v1 = Unity.Burst.Intrinsics.X86.Sse.mul_ps(v1, v2);

                                v2.Float0 = pop_or_value(code_pointer + 9);
                                v2.Float1 = pop_or_value(code_pointer + 10);
                                v2.Float2 = pop_or_value(code_pointer + 11);
                                v2.Float3 = pop_or_value(code_pointer + 12);

                                v1 = Unity.Burst.Intrinsics.X86.Sse.mul_ps(v1, v2);

                                v2.Float0 = pop_or_value(code_pointer + 13);
                                v2.Float1 = pop_or_value(code_pointer + 14);
                                v2.Float2 = pop_or_value(code_pointer + 15);
                                v2.Float3 = 1;

                                v1 = Unity.Burst.Intrinsics.X86.Sse.mul_ps(v1, v2);

                                r.x = v1.Float0 * v1.Float1 * v1.Float2 * v1.Float3;
                                code_pointer += c;
                                break;

                            case 16:
                                v1.Float0 = pop_or_value(code_pointer + 1);
                                v1.Float1 = pop_or_value(code_pointer + 2);
                                v1.Float2 = pop_or_value(code_pointer + 3);
                                v1.Float3 = pop_or_value(code_pointer + 4);
                                v2.Float0 = pop_or_value(code_pointer + 5);
                                v2.Float1 = pop_or_value(code_pointer + 6);
                                v2.Float2 = pop_or_value(code_pointer + 7);
                                v2.Float3 = pop_or_value(code_pointer + 8);

                                v1 = Unity.Burst.Intrinsics.X86.Sse.mul_ps(v1, v2);

                                v2.Float0 = pop_or_value(code_pointer + 9);
                                v2.Float1 = pop_or_value(code_pointer + 10);
                                v2.Float2 = pop_or_value(code_pointer + 11);
                                v2.Float3 = pop_or_value(code_pointer + 12);

                                v1 = Unity.Burst.Intrinsics.X86.Sse.mul_ps(v1, v2);

                                v2.Float0 = pop_or_value(code_pointer + 13);
                                v2.Float1 = pop_or_value(code_pointer + 14);
                                v2.Float2 = pop_or_value(code_pointer + 15);
                                v2.Float3 = pop_or_value(code_pointer + 16);

                                v1 = Unity.Burst.Intrinsics.X86.Sse.mul_ps(v1, v2);

                                r.x = v1.Float0 * v1.Float1 * v1.Float2 * v1.Float3;
                                code_pointer += c;
                                break;

                            // mula for float4
                            // case > opt_id == vector??

                            // any other size run manually as 1 float
                            default:
                                {
                                    code_pointer++;
                                    float r1 = pop_or_value(code_pointer);

                                    for (int i = 1; i < c; i++)
                                    {
                                        r1 *= pop_or_value(code_pointer + i);
                                    }

                                    r = new float4(r1, 0, 0, 0);
                                }
                                code_pointer += c - 1;
                                break;
                        }
                    }
                    r.x = math.select(r.x, -r.x, minus);
                    minus = false;
                    break;

                case 0: // actually 4
                    {
                        switch (c)
                        {
                            case 1:
                                {
                                    r = pop_or_value_4(++code_pointer);
                                }
                                break;

                            case 2:
                                {
                                    r = pop_or_value_4(++code_pointer) * pop_or_value_4(++code_pointer);
                                }
                                break;

                            case 3:
                                {
                                    r = pop_or_value_4(++code_pointer) *
                                        pop_or_value_4(++code_pointer) *
                                        pop_or_value_4(++code_pointer);
                                }
                                break;

                            case 4:
                                {
                                    r = pop_or_value_4(++code_pointer) *
                                        pop_or_value_4(++code_pointer) *
                                        pop_or_value_4(++code_pointer) *
                                        pop_or_value_4(++code_pointer);
                                }
                                break;

                            case 5:
                                {
                                    r = pop_or_value_4(++code_pointer) *
                                        pop_or_value_4(++code_pointer) *
                                        pop_or_value_4(++code_pointer) *
                                        pop_or_value_4(++code_pointer) *
                                        pop_or_value_4(++code_pointer);
                                }
                                break;

                            case 6:
                                {
                                    r = pop_or_value_4(++code_pointer) *
                                        pop_or_value_4(++code_pointer) *
                                        pop_or_value_4(++code_pointer) *
                                        pop_or_value_4(++code_pointer) *
                                        pop_or_value_4(++code_pointer) *
                                        pop_or_value_4(++code_pointer);
                                }
                                break;

                            case 7:
                                {
                                    r = pop_or_value_4(++code_pointer) *
                                        pop_or_value_4(++code_pointer) *
                                        pop_or_value_4(++code_pointer) *
                                        pop_or_value_4(++code_pointer) *
                                        pop_or_value_4(++code_pointer) *
                                        pop_or_value_4(++code_pointer) *
                                        pop_or_value_4(++code_pointer);
                                }
                                break;

                            case 8:
                                {
                                    r = pop_or_value_4(++code_pointer) *
                                        pop_or_value_4(++code_pointer) *
                                        pop_or_value_4(++code_pointer) *
                                        pop_or_value_4(++code_pointer) *
                                        pop_or_value_4(++code_pointer) *
                                        pop_or_value_4(++code_pointer) *
                                        pop_or_value_4(++code_pointer) *
                                        pop_or_value_4(++code_pointer);
                                }
                                break;

                            default:
                                {
                                    // this probably is just as fast as an unrolled version because of the jumping in pop.. 
                                    r = pop_or_value_4(code_pointer + 1);
                                    for (int i = 2; 2 <= c; i++)
                                    {
                                        r = r * pop_or_value_4(code_pointer + i);
                                    }
                                }
                                break;
                        }
                    }
                    r = math.select(r, -r, minus);
                    minus = false;
                    break;

                default:
                    {
#if LOG_ERRORS
                    Standalone.Debug.LogError($"mula: vector size '{vector_size}' not supported ");
#endif
                        r = new float4(float.NaN, float.NaN, float.NaN, float.NaN);
                        minus = false;
                    }
                    break;
            }

            return r;
        }

        float4 get_adda_result(ref int code_pointer, ref bool minus, ref byte vector_size)
        {
            code_pointer = code_pointer + 1;
            byte c = code[code_pointer];

            vector_size = (byte)(c & 0b11);
            c = (byte)((c & 0b1111_1100) >> 2);

            switch ((BlastVectorSizes)vector_size)
            {
                case BlastVectorSizes.float1:
                    {
                        float f = pop_or_value(code_pointer + 1);
                        for (int i = 2; i <= c; i++)
                        {
                            f += pop_or_value(code_pointer + i);
                        }
                        code_pointer += c;
                        f = math.select(f, -f, minus);
                        minus = false;
                        return f;
                    }

                case 0:
                case BlastVectorSizes.float4:
                    {
                        float4 f4 = pop_or_value_4(code_pointer + 1);
                        for (int i = 2; i <= c; i++)
                        {
                            f4 += pop_or_value_4(code_pointer + i);
                        }
                        code_pointer += c;
                        f4 = math.select(f4, -f4, minus);
                        minus = false;
                        return f4;
                    }
                default:
                    {
#if UNITY_EDITOR || LOG_ERRORS
                        Standalone.Debug.LogError($"get_adda_result: vector size '{vector_size}' not allowed");
#endif
                        return new float4(float.NaN, float.NaN, float.NaN, float.NaN);

                    }
            }
        }

        float4 get_suba_result(ref int code_pointer, ref bool minus, ref byte vector_size)
        {
            code_pointer = code_pointer + 1;
            byte c = code[code_pointer];

            vector_size = (byte)(c & 0b11);
            c = (byte)((c & 0b1111_1100) >> 2);

            switch ((BlastVectorSizes)vector_size)
            {
                case BlastVectorSizes.float1:
                    {
                        float f = pop_or_value(code_pointer + 1);
                        for (int i = 2; i <= c; i++)
                        {
                            f -= pop_or_value(code_pointer + i);
                        }
                        code_pointer += c;
                        f = math.select(f, -f, minus);
                        minus = false;
                        return f;
                    }

                case 0:
                case BlastVectorSizes.float4:
                    {
                        float4 f4 = pop_or_value_4(code_pointer + 1);
                        for (int i = 2; i <= c; i++)
                        {
                            f4 -= pop_or_value_4(code_pointer + i);
                        }
                        code_pointer += c;
                        f4 = math.select(f4, -f4, minus);
                        minus = false;
                        return f4;
                    }
                default:
                    {
#if UNITY_EDITOR || LOG_ERRORS
                        Standalone.Debug.LogError($"get_suba_result: vector size '{vector_size}' not allowed");
#endif
                        return new float4(float.NaN, float.NaN, float.NaN, float.NaN);

                    }
            }
        }

        float4 get_diva_result(ref int code_pointer, ref bool minus, ref byte vector_size)
        {
            code_pointer = code_pointer + 1;
            byte c = code[code_pointer];

            vector_size = (byte)(c & 0b11);
            c = (byte)((c & 0b1111_1100) >> 2);

            switch ((BlastVectorSizes)vector_size)
            {
                case BlastVectorSizes.float1:
                    {
                        float f = pop_or_value(code_pointer + 1);
                        for (int i = 2; i <= c; i++)
                        {
                            f /= pop_or_value(code_pointer + i);
                        }
                        code_pointer += c;
                        f = math.select(f, -f, minus);
                        minus = false;
                        return f;
                    }

                case 0:
                case BlastVectorSizes.float4:
                    {
                        float4 f4 = pop_or_value_4(code_pointer + 1);
                        for (int i = 2; i <= c; i++)
                        {
                            f4 /= pop_or_value_4(code_pointer + i);
                        }
                        code_pointer += c;
                        f4 = math.select(f4, -f4, minus);
                        minus = false;
                        return f4;
                    }
                default:
                    {
#if UNITY_EDITOR || LOG_ERRORS
                        Standalone.Debug.LogError($"get_diva_result: vector size '{vector_size}' not allowed");
#endif
                        return new float4(float.NaN, float.NaN, float.NaN, float.NaN);

                    }
            }
        }

        float4 get_max_result(ref int code_pointer, ref bool minus, ref bool not, ref byte vector_size)
        {
            // get compressed value of count and vector_size
            code_pointer++;
            byte c = code[code_pointer];

            vector_size = (byte)(c & 0b11);
            c = (byte)((c & 0b1111_1100) >> 2);

            float4 f4 = float4.zero;

            // this will compile into a jump table (CHECK THIS !!) todo
            switch ((BlastVectorSizes)vector_size)
            {
                case BlastVectorSizes.float1:
                    {
                        switch (c)
                        {
                            default:
#if UNITY_EDITOR || LOG_ERRORS
                                Standalone.Debug.LogError($"get_max_result: parameter count '{c}' not supported -> parser/compiler/optimizer error");
#endif
                                return new float4(float.NaN, float.NaN, float.NaN, float.NaN);

                            case 2:
                                f4.x = math.max(pop_or_value(code_pointer + 1), pop_or_value(code_pointer + 2));
                                code_pointer += 2;
                                return f4;

                            case 3:
                                f4.x = math.max(
                                        math.max(pop_or_value(code_pointer + 1), pop_or_value(code_pointer + 2)),
                                            pop_or_value(code_pointer + 3));
                                code_pointer += 3;
                                return f4;

                            case 4:
                                f4.x = math.max(
                                        math.max(pop_or_value(code_pointer + 1), pop_or_value(code_pointer + 2)),
                                        math.max(pop_or_value(code_pointer + 3), pop_or_value(code_pointer + 4)));
                                code_pointer += 4;
                                return f4;

                            case 5:
                                f4.x = math.max(
                                    math.max(
                                        math.max(pop_or_value(code_pointer + 1), pop_or_value(code_pointer + 2)),
                                        math.max(pop_or_value(code_pointer + 3), pop_or_value(code_pointer + 4))),
                                        pop_or_value(code_pointer + 5));
                                code_pointer += 5;
                                return f4;

                            case 6:
                                f4.x = math.max(
                                    math.max(
                                        math.max(pop_or_value(code_pointer + 1), pop_or_value(code_pointer + 2)),
                                        math.max(pop_or_value(code_pointer + 3), pop_or_value(code_pointer + 4))),
                                        math.max(pop_or_value(code_pointer + 5), pop_or_value(code_pointer + 6)));
                                code_pointer += 6;
                                return f4;

                            case 7:
                                f4.x = math.max(
                                        math.max(
                                        math.max(
                                        math.max(pop_or_value(code_pointer + 1), pop_or_value(code_pointer + 2)),
                                        math.max(pop_or_value(code_pointer + 3), pop_or_value(code_pointer + 4))),
                                        math.max(pop_or_value(code_pointer + 5), pop_or_value(code_pointer + 6))),
                                        pop_or_value(code_pointer + 7));
                                code_pointer += 7;
                                return f4;

                            case 8:
                                f4.x = math.max(
                                        math.max(
                                        math.max(
                                        math.max(pop_or_value(code_pointer + 1), pop_or_value(code_pointer + 2)),
                                        math.max(pop_or_value(code_pointer + 3), pop_or_value(code_pointer + 4))),
                                        math.max(pop_or_value(code_pointer + 5), pop_or_value(code_pointer + 6))),
                                        math.max(pop_or_value(code_pointer + 7), pop_or_value(code_pointer + 8)));
                                code_pointer += 8;
                                return f4;
                        }
                    }

                // vector_size 0 actually means 4 because of using only 2 bits 
                case 0:
                case BlastVectorSizes.float4:
                    {
                        vector_size = 4;
                        switch (c)
                        {
                            default:
#if UNITY_EDITOR || LOG_ERRORS
                                Standalone.Debug.LogError($"get_max_result: parameter count '{c}' not supported -> parser/compiler/optimizer error");
#endif
                                return new float4(float.NaN, float.NaN, float.NaN, float.NaN);

                            case 2:
                                f4 = math.max(pop_or_value_4(code_pointer + 1), pop_or_value_4(code_pointer + 2));
                                code_pointer += 2;
                                return f4;

                            case 3:
                                f4 = math.max(
                                    math.max(pop_or_value_4(code_pointer + 1), pop_or_value_4(code_pointer + 2)),
                                    pop_or_value_4(code_pointer + 3));
                                code_pointer += 3;
                                return f4;

                            case 4:
                                f4 = math.max(
                                    math.max(pop_or_value_4(code_pointer + 1), pop_or_value_4(code_pointer + 2)),
                                    math.max(pop_or_value_4(code_pointer + 3), pop_or_value_4(code_pointer + 4)));
                                code_pointer += 4;
                                return f4;

                            case 5:
                                f4 = math.max(
                                    math.max(
                                        math.max(pop_or_value_4(code_pointer + 1), pop_or_value_4(code_pointer + 2)),
                                        math.max(pop_or_value_4(code_pointer + 3), pop_or_value_4(code_pointer + 4))),
                                        pop_or_value_4(code_pointer + 5));
                                code_pointer += 5;
                                return f4;

                            case 6:
                                f4 = math.max(
                                    math.max(
                                        math.max(pop_or_value_4(code_pointer + 1), pop_or_value_4(code_pointer + 2)),
                                        math.max(pop_or_value_4(code_pointer + 3), pop_or_value_4(code_pointer + 4))),
                                        math.max(pop_or_value_4(code_pointer + 5), pop_or_value_4(code_pointer + 6)));
                                code_pointer += 6;
                                return f4;

                            case 7:
                                f4 = math.max(
                                        math.max(
                                        math.max(
                                        math.max(pop_or_value_4(code_pointer + 1), pop_or_value_4(code_pointer + 2)),
                                        math.max(pop_or_value_4(code_pointer + 3), pop_or_value_4(code_pointer + 4))),
                                        math.max(pop_or_value_4(code_pointer + 5), pop_or_value_4(code_pointer + 6))),
                                        pop_or_value_4(code_pointer + 7));
                                code_pointer += 7;
                                return f4;

                            case 8:
                                f4 = math.max(
                                        math.max(
                                        math.max(
                                        math.max(pop_or_value_4(code_pointer + 1), pop_or_value_4(code_pointer + 2)),
                                        math.max(pop_or_value_4(code_pointer + 3), pop_or_value_4(code_pointer + 4))),
                                        math.max(pop_or_value_4(code_pointer + 5), pop_or_value_4(code_pointer + 6))),
                                        math.max(pop_or_value_4(code_pointer + 7), pop_or_value_4(code_pointer + 8)));
                                code_pointer += 8;
                                return f4;
                        }
                    }

                default:
                    {
#if UNITY_EDITOR || LOG_ERRORS
                        Standalone.Debug.LogError($"get_max_result: vector size '{vector_size}' not allowed");
#endif
                        return new float4(float.NaN, float.NaN, float.NaN, float.NaN);
                    }
            }
        }

        float4 get_min_result(ref int code_pointer, ref bool minus, ref bool not, ref byte vector_size)
        {
            code_pointer++;
            byte c = code[code_pointer];

            vector_size = (byte)(c & 0b11);
            c = (byte)((c & 0b1111_1100) >> 2);

            float4 f;
            // this will compile into a jump table (CHECK THIS !!) todo
            switch ((BlastVectorSizes)vector_size)
            {
                case BlastVectorSizes.float1:
                    {
                        switch (c)
                        {
                            default:
#if LOG_ERRORS
                Standalone.Debug.LogError($"get_min_result: parameter count '{c}' not supported -> parser/compiler/optimizer error");
#endif
                                return 0;


                            case 2:
                                f.x = math.min(pop_or_value(code_pointer + 1), pop_or_value(code_pointer + 2));
                                code_pointer += 2;
                                return f.x;

                            case 3:
                                f.x = math.min(
                                        math.min(pop_or_value(code_pointer + 1), pop_or_value(code_pointer + 2)),
                                            pop_or_value(code_pointer + 3));
                                code_pointer += 3;
                                return f.x;

                            case 4:
                                f.x = math.min(
                                        math.min(pop_or_value(code_pointer + 1), pop_or_value(code_pointer + 2)),
                                        math.min(pop_or_value(code_pointer + 3), pop_or_value(code_pointer + 4)));
                                code_pointer += 4;
                                return f.x;

                            case 5:
                                f.x = math.min(
                                    math.min(
                                        math.min(pop_or_value(code_pointer + 1), pop_or_value(code_pointer + 2)),
                                        math.min(pop_or_value(code_pointer + 3), pop_or_value(code_pointer + 4))),
                                        pop_or_value(code_pointer + 5));
                                code_pointer += 5;
                                return f.x;

                            case 6:
                                f.x = math.min(
                                    math.min(
                                        math.min(pop_or_value(code_pointer + 1), pop_or_value(code_pointer + 2)),
                                        math.min(pop_or_value(code_pointer + 3), pop_or_value(code_pointer + 4))),
                                        math.min(pop_or_value(code_pointer + 4), pop_or_value(code_pointer + 5)));
                                code_pointer += 6;
                                return f.x;

                            case 7:
                                f.x = math.min(
                                        math.min(
                                        math.min(
                                            math.min(pop_or_value(code_pointer + 1), pop_or_value(code_pointer + 2)),
                                            math.min(pop_or_value(code_pointer + 3), pop_or_value(code_pointer + 4))),
                                            math.min(pop_or_value(code_pointer + 4), pop_or_value(code_pointer + 5))),
                                        pop_or_value(code_pointer + 6));
                                code_pointer += 7;
                                return f.x;

                            case 8:
                                f.x = math.min(
                                        math.min(
                                        math.min(
                                            math.min(pop_or_value(code_pointer + 1), pop_or_value(code_pointer + 2)),
                                            math.min(pop_or_value(code_pointer + 3), pop_or_value(code_pointer + 4))),
                                            math.min(pop_or_value(code_pointer + 4), pop_or_value(code_pointer + 5))),
                                        math.min(pop_or_value(code_pointer + 6), pop_or_value(code_pointer + 7)));
                                code_pointer += 8;
                                return f.x;
                        }
                    }

                case BlastVectorSizes.float4:
                    {
                        switch (c)
                        {
                            default:
#if LOG_ERRORS
                Standalone.Debug.LogError($"get_min_result: parameter count '{c}' not supported -> parser/compiler/optimizer error");
#endif
                                return 0;

                            case 2:
                                f = math.min(pop_or_value_4(code_pointer + 1), pop_or_value_4(code_pointer + 2));
                                code_pointer += 2;
                                return f;

                            case 3:
                                f = math.min(
                                        math.min(pop_or_value_4(code_pointer + 1), pop_or_value_4(code_pointer + 2)),
                                            pop_or_value_4(code_pointer + 3));
                                code_pointer += 3;
                                return f;

                            case 4:
                                f = math.min(
                                        math.min(pop_or_value_4(code_pointer + 1), pop_or_value_4(code_pointer + 2)),
                                        math.min(pop_or_value_4(code_pointer + 3), pop_or_value_4(code_pointer + 4)));
                                code_pointer += 4;
                                return f;

                            case 5:
                                f = math.min(
                                    math.min(
                                        math.min(pop_or_value_4(code_pointer + 1), pop_or_value_4(code_pointer + 2)),
                                        math.min(pop_or_value_4(code_pointer + 3), pop_or_value_4(code_pointer + 4))),
                                        pop_or_value_4(code_pointer + 5));
                                code_pointer += 5;
                                return f;

                            case 6:
                                f = math.min(
                                    math.min(
                                        math.min(pop_or_value_4(code_pointer + 1), pop_or_value_4(code_pointer + 2)),
                                        math.min(pop_or_value_4(code_pointer + 3), pop_or_value_4(code_pointer + 4))),
                                        math.min(pop_or_value_4(code_pointer + 4), pop_or_value_4(code_pointer + 5)));
                                code_pointer += 6;
                                return f;

                            case 7:
                                f = math.min(
                                        math.min(
                                        math.min(
                                            math.min(pop_or_value_4(code_pointer + 1), pop_or_value_4(code_pointer + 2)),
                                            math.min(pop_or_value_4(code_pointer + 3), pop_or_value_4(code_pointer + 4))),
                                            math.min(pop_or_value_4(code_pointer + 4), pop_or_value_4(code_pointer + 5))),
                                        pop_or_value_4(code_pointer + 6));
                                code_pointer += 7;
                                return f.x;

                            case 8:
                                f = math.min(
                                        math.min(
                                        math.min(
                                            math.min(pop_or_value_4(code_pointer + 1), pop_or_value_4(code_pointer + 2)),
                                            math.min(pop_or_value_4(code_pointer + 3), pop_or_value_4(code_pointer + 4))),
                                            math.min(pop_or_value_4(code_pointer + 4), pop_or_value_4(code_pointer + 5))),
                                        math.min(pop_or_value_4(code_pointer + 6), pop_or_value_4(code_pointer + 7)));
                                code_pointer += 8;
                                return f;
                        }
                    }

                default:
                    {
#if UNITY_EDITOR || LOG_ERRORS
                        Standalone.Debug.LogError($"get_min_result: vector size '{vector_size}' not (yet) supported");
#endif
                        return new float4(float.NaN, float.NaN, float.NaN, float.NaN);
                    }
            }
        }

        /// <summary>
        /// a = any(a b c d)
        /// </summary>
        /// <param name="code_pointer"></param>
        /// <param name="minus"></param>
        /// <param name="not"></param>
        /// <param name="code"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        float4 get_any_result(ref int code_pointer, ref bool minus, ref bool not, ref byte vector_size)
        {
            code_pointer++;
            byte c = code[code_pointer];
            float4 f;

            //  no way for bytecode optimizor to do this.. 
            vector_size = (byte)(c & 0b11);
            c = (byte)((c & 0b111_1100) >> 2);

            // this will compile into a jump table (CHECK THIS !!) todo
            switch ((BlastVectorSizes)vector_size)
            {
                case BlastVectorSizes.float1:
                    {
                        switch (c)
                        {
                            default:
#if LOG_ERRORS
                Standalone.Debug.LogError($"get_any_result: parameter count '{c}' not supported -> parser/compiler/optimizer error");
#endif
                                return 0;

                            case 2:
                                f = math.select(0f, 1f, pop_or_value(code_pointer + 1) != 0 || pop_or_value(code_pointer + 2) != 0);
                                code_pointer += 2;
                                return f;

                            case 3:
                                f = math.select(0f, 1f,
                                    pop_or_value(code_pointer + 1) != 0 ||
                                    pop_or_value(code_pointer + 2) != 0 ||
                                    pop_or_value(code_pointer + 3) != 0);
                                code_pointer += 3;
                                return f;

                            case 4:
                                f = math.select(0f, 1f,
                                    pop_or_value(code_pointer + 1) != 0 ||
                                    pop_or_value(code_pointer + 2) != 0 ||
                                    pop_or_value(code_pointer + 3) != 0 ||
                                    pop_or_value(code_pointer + 4) != 0);
                                code_pointer += 4;
                                return f;

                            case 5:
                                f = math.select(0f, 1f,
                                    pop_or_value(code_pointer + 1) != 0 ||
                                    pop_or_value(code_pointer + 2) != 0 ||
                                    pop_or_value(code_pointer + 3) != 0 ||
                                    pop_or_value(code_pointer + 4) != 0 ||
                                    pop_or_value(code_pointer + 5) != 0);
                                code_pointer += 5;
                                return f;

                            case 6:
                                f = math.select(0f, 1f,
                                    pop_or_value(code_pointer + 1) != 0 ||
                                    pop_or_value(code_pointer + 2) != 0 ||
                                    pop_or_value(code_pointer + 3) != 0 ||
                                    pop_or_value(code_pointer + 4) != 0 ||
                                    pop_or_value(code_pointer + 5) != 0 ||
                                    pop_or_value(code_pointer + 6) != 0);
                                code_pointer += 6;
                                return f;

                            case 7:
                                f = math.select(0f, 1f,
                                    pop_or_value(code_pointer + 1) != 0 ||
                                    pop_or_value(code_pointer + 2) != 0 ||
                                    pop_or_value(code_pointer + 3) != 0 ||
                                    pop_or_value(code_pointer + 4) != 0 ||
                                    pop_or_value(code_pointer + 5) != 0 ||
                                    pop_or_value(code_pointer + 6) != 0 ||
                                    pop_or_value(code_pointer + 7) != 0);
                                code_pointer += 7;
                                return f;

                            case 8:
                                f = math.select(0f, 1f,
                                    pop_or_value(code_pointer + 1) != 0 ||
                                    pop_or_value(code_pointer + 2) != 0 ||
                                    pop_or_value(code_pointer + 3) != 0 ||
                                    pop_or_value(code_pointer + 4) != 0 ||
                                    pop_or_value(code_pointer + 5) != 0 ||
                                    pop_or_value(code_pointer + 6) != 0 ||
                                    pop_or_value(code_pointer + 7) != 0 ||
                                    pop_or_value(code_pointer + 8) != 0);
                                code_pointer += 8;
                                return f;
                        }
                    }

                case BlastVectorSizes.float4:
                    {
                        switch (c)
                        {
                            default:
#if LOG_ERRORS
                Standalone.Debug.LogError($"get_any_result: parameter count '{c}' not supported -> parser/compiler/optimizer error");
#endif
                                return 0;

                            case 2:
                                f.x = math.select(0f, 1f,
                                    math.any(pop_or_value_4(code_pointer + 1)) ||
                                    math.any(pop_or_value_4(code_pointer + 2)));
                                code_pointer += 2;
                                return f.x;

                            case 3:
                                f.x = math.select(0f, 1f,
                                    math.any(pop_or_value_4(code_pointer + 1)) ||
                                    math.any(pop_or_value_4(code_pointer + 2)) ||
                                    math.any(pop_or_value_4(code_pointer + 3)));
                                code_pointer += 3;
                                return f.x;

                            case 4:
                                f.x = math.select(0f, 1f,
                                    math.any(pop_or_value_4(code_pointer + 1)) ||
                                    math.any(pop_or_value_4(code_pointer + 2)) ||
                                    math.any(pop_or_value_4(code_pointer + 3)) ||
                                    math.any(pop_or_value_4(code_pointer + 4)));
                                code_pointer += 4;
                                return f.x;

                            case 5:
                                f.x = math.select(0f, 1f,
                                    math.any(pop_or_value_4(code_pointer + 1)) ||
                                    math.any(pop_or_value_4(code_pointer + 2)) ||
                                    math.any(pop_or_value_4(code_pointer + 3)) ||
                                    math.any(pop_or_value_4(code_pointer + 4)) ||
                                    math.any(pop_or_value_4(code_pointer + 5)));
                                code_pointer += 5;
                                return f.x;

                            case 6:
                                f = math.select(0f, 1f,
                                    math.any(pop_or_value_4(code_pointer + 1)) ||
                                    math.any(pop_or_value_4(code_pointer + 2)) ||
                                    math.any(pop_or_value_4(code_pointer + 3)) ||
                                    math.any(pop_or_value_4(code_pointer + 4)) ||
                                    math.any(pop_or_value_4(code_pointer + 5)) ||
                                    math.any(pop_or_value_4(code_pointer + 6)));
                                code_pointer += 6;
                                return f;

                            case 7:
                                f = math.select(0f, 1f,
                                    math.any(pop_or_value_4(code_pointer + 1)) ||
                                    math.any(pop_or_value_4(code_pointer + 2)) ||
                                    math.any(pop_or_value_4(code_pointer + 3)) ||
                                    math.any(pop_or_value_4(code_pointer + 4)) ||
                                    math.any(pop_or_value_4(code_pointer + 5)) ||
                                    math.any(pop_or_value_4(code_pointer + 6)) ||
                                    math.any(pop_or_value_4(code_pointer + 7)));
                                code_pointer += 7;
                                return f;

                            case 8:
                                f = math.select(0f, 1f,
                                    math.any(pop_or_value_4(code_pointer + 1)) ||
                                    math.any(pop_or_value_4(code_pointer + 2)) ||
                                    math.any(pop_or_value_4(code_pointer + 3)) ||
                                    math.any(pop_or_value_4(code_pointer + 4)) ||
                                    math.any(pop_or_value_4(code_pointer + 5)) ||
                                    math.any(pop_or_value_4(code_pointer + 6)) ||
                                    math.any(pop_or_value_4(code_pointer + 7)) ||
                                    math.any(pop_or_value_4(code_pointer + 8)));
                                code_pointer += 8;
                                return f;
                        }
                    }

                default:
                    {
#if UNITY_EDITOR || LOG_ERRORS
                        Standalone.Debug.LogError($"get_any_result: vector size '{vector_size}' not (yet) supported");
#endif
                        return new float4(float.NaN, float.NaN, float.NaN, float.NaN);
                    }
            }
        }

        /// <summary>
        /// convert all calls to all to !any( == 0) 
        /// !any() is a lot cheaper then all() because we can drop out early and may not need to lookup every value /// </summary>
        /// <param name="code_pointer"></param>
        /// <param name="minus"></param>
        /// <param name="not"></param>
        /// <param name="vector_size"></param>
        /// <returns></returns>
        float4 get_all_result(ref int code_pointer, ref bool minus, ref bool not, ref byte vector_size)
        {
            code_pointer++;
            byte c = code[code_pointer];

            vector_size = (byte)(c & 0b11);
            c = (byte)((c & 0b111_1100) >> 2);

            float4 f;
            switch ((BlastVectorSizes)vector_size)
            {
                case BlastVectorSizes.float1:
                    {
                        switch (c)
                        {
                            default:
#if LOG_ERRORS
                Standalone.Debug.LogError($"get_all_result: parameter count '{c}' not supported -> parser/compiler/optimizer error");
#endif
                                return 0;

                            case 2:
                                f = math.select(0f, 1f, pop_or_value(code_pointer + 1) != 0 && pop_or_value(code_pointer + 2) != 0);
                                code_pointer += 2;
                                return f;

                            case 3:
                                f = math.select(0f, 1f, !(
                                    pop_or_value(code_pointer + 1) == 0 ||
                                    pop_or_value(code_pointer + 2) == 0 ||
                                    pop_or_value(code_pointer + 3) == 0));
                                code_pointer += 3;
                                return f;

                            case 4:
                                f = math.select(0f, 1f, !(
                                    pop_or_value(code_pointer + 1) == 0 ||
                                    pop_or_value(code_pointer + 2) == 0 ||
                                    pop_or_value(code_pointer + 3) == 0 ||
                                    pop_or_value(code_pointer + 4) == 0));
                                code_pointer += 4;
                                return f;

                            case 5:
                                f = math.select(0f, 1f, !(
                                    pop_or_value(code_pointer + 1) == 0 ||
                                    pop_or_value(code_pointer + 2) == 0 ||
                                    pop_or_value(code_pointer + 3) == 0 ||
                                    pop_or_value(code_pointer + 4) == 0 ||
                                    pop_or_value(code_pointer + 5) == 0));
                                code_pointer += 5;
                                return f;

                            case 6:
                                f = math.select(0f, 1f, !(
                                    pop_or_value(code_pointer + 1) == 0 ||
                                    pop_or_value(code_pointer + 2) == 0 ||
                                    pop_or_value(code_pointer + 3) == 0 ||
                                    pop_or_value(code_pointer + 4) == 0 ||
                                    pop_or_value(code_pointer + 5) == 0 ||
                                    pop_or_value(code_pointer + 6) == 0));
                                code_pointer += 6;
                                return f;

                            case 7:
                                f = math.select(0f, 1f, !(
                                    pop_or_value(code_pointer + 1) == 0 ||
                                    pop_or_value(code_pointer + 2) == 0 ||
                                    pop_or_value(code_pointer + 3) == 0 ||
                                    pop_or_value(code_pointer + 4) == 0 ||
                                    pop_or_value(code_pointer + 5) == 0 ||
                                    pop_or_value(code_pointer + 6) == 0 ||
                                    pop_or_value(code_pointer + 7) == 0));
                                code_pointer += 7;
                                return f;

                            case 8:
                                f = math.select(0f, 1f, !(
                                    pop_or_value(code_pointer + 1) == 0 ||
                                    pop_or_value(code_pointer + 2) == 0 ||
                                    pop_or_value(code_pointer + 3) == 0 ||
                                    pop_or_value(code_pointer + 4) == 0 ||
                                    pop_or_value(code_pointer + 5) == 0 ||
                                    pop_or_value(code_pointer + 6) == 0 ||
                                    pop_or_value(code_pointer + 7) == 0 ||
                                    pop_or_value(code_pointer + 8) == 0));
                                code_pointer += 8;
                                return f;
                        }
                    }

                case BlastVectorSizes.float4:
                    {
                        switch (c)
                        {
                            default:
#if LOG_ERRORS
                Standalone.Debug.LogError($"get_all_result: parameter count '{c}' not supported -> parser/compiler/optimizer error");
#endif
                                return 0;

                            case 2:
                                f = math.select(0f, 1f, !(
                                    math.all(pop_or_value_4(code_pointer + 1)) ||
                                    math.all(pop_or_value_4(code_pointer + 2) == 0)));
                                code_pointer += 2;
                                return f;

                            case 3:
                                f = math.select(0f, 1f, !(
                                    math.all(pop_or_value_4(code_pointer + 1)) ||
                                    math.all(pop_or_value_4(code_pointer + 2)) ||
                                    math.all(pop_or_value_4(code_pointer + 3) == 0)));
                                code_pointer += 3;
                                return f;

                            case 4:
                                f = math.select(0f, 1f, !(
                                    math.all(pop_or_value_4(code_pointer + 1)) ||
                                    math.all(pop_or_value_4(code_pointer + 2)) ||
                                    math.all(pop_or_value_4(code_pointer + 3)) ||
                                    math.all(pop_or_value_4(code_pointer + 4) == 0)));
                                code_pointer += 4;
                                return f;

                            case 5:
                                f = math.select(0f, 1f, !(
                                    math.all(pop_or_value_4(code_pointer + 1)) ||
                                    math.all(pop_or_value_4(code_pointer + 2)) ||
                                    math.all(pop_or_value_4(code_pointer + 3)) ||
                                    math.all(pop_or_value_4(code_pointer + 4)) ||
                                    math.all(pop_or_value_4(code_pointer + 5) == 0)));
                                code_pointer += 5;
                                return f;

                            case 6:
                                f = math.select(0f, 1f, !(
                                    math.all(pop_or_value_4(code_pointer + 1)) ||
                                    math.all(pop_or_value_4(code_pointer + 2)) ||
                                    math.all(pop_or_value_4(code_pointer + 3)) ||
                                    math.all(pop_or_value_4(code_pointer + 4)) ||
                                    math.all(pop_or_value_4(code_pointer + 5)) ||
                                    math.all(pop_or_value_4(code_pointer + 6) == 0)));
                                code_pointer += 6;
                                return f;

                            case 7:
                                f = math.select(0f, 1f, !(
                                    math.all(pop_or_value_4(code_pointer + 1)) ||
                                    math.all(pop_or_value_4(code_pointer + 2)) ||
                                    math.all(pop_or_value_4(code_pointer + 3)) ||
                                    math.all(pop_or_value_4(code_pointer + 4)) ||
                                    math.all(pop_or_value_4(code_pointer + 5)) ||
                                    math.all(pop_or_value_4(code_pointer + 6)) ||
                                    math.all(pop_or_value_4(code_pointer + 7) == 0)));
                                code_pointer += 7;
                                return f;

                            case 8:
                                f = math.select(0f, 1f, !(
                                    math.all(pop_or_value_4(code_pointer + 1)) ||
                                    math.all(pop_or_value_4(code_pointer + 2)) ||
                                    math.all(pop_or_value_4(code_pointer + 3)) ||
                                    math.all(pop_or_value_4(code_pointer + 4)) ||
                                    math.all(pop_or_value_4(code_pointer + 5)) ||
                                    math.all(pop_or_value_4(code_pointer + 6)) ||
                                    math.all(pop_or_value_4(code_pointer + 7)) ||
                                    math.all(pop_or_value_4(code_pointer + 8) == 0)));
                                code_pointer += 8;
                                return f;
                        }
                    }

                default:
                    {
#if UNITY_EDITOR || LOG_ERRORS
                        Standalone.Debug.LogError($"get_any_result: vector size '{vector_size}' not (yet) supported");
#endif
                        return new float4(float.NaN, float.NaN, float.NaN, float.NaN);
                    }
            }

        }

        float4 get_extended_operation_result(ref int code_pointer, ref bool minus, ref byte vector_size, ref float4 f4)
        {
            code_pointer++;
            extended_script_op op = (extended_script_op)code[code_pointer];

            switch(op)
            {
                case extended_script_op.logn: return get_log_result(ref code_pointer, ref minus, ref vector_size, ref f4); 
                case extended_script_op.log10: return get_log10_result(ref code_pointer, ref minus, ref vector_size, ref f4);
                case extended_script_op.exp10: return get_exp10_result(ref code_pointer, ref minus, ref vector_size, ref f4);
                case extended_script_op.cross: return get_cross_result(ref code_pointer, ref minus, ref vector_size, ref f4);
            }

            // handle op 
#if LOG_ERRORS
            Standalone.Debug.LogError($"get_extended_operation_result: operation {op} not handled / supported / implemented ");
#endif

            return f4;
        }


        #endregion

        #region ByteCode Execution


        // recursively handle a compounded statement and return its value
        // - the compiler should flatten execution, this will save a lot on stack allocations
        float4 get_compound_result(ref int code_pointer, ref byte vector_size)
        {
            byte op = 0, prev_op = 0;
            bool op_is_value = false, prev_op_is_value = false;

            bool last_is_op_or_first = true;

            bool minus = false;   // shouldnt we treat these as 1?
            bool not = false;

            float f1 = 0, temp = 0;
            float4 f4 = float4.zero;

            // vector_size = 1; now set by callee
            byte max_vector_size = 1;

            script_op current_operation = script_op.nop;

            while (code_pointer < package->info.code_size)
            {
                prev_op = op;
                prev_op_is_value = op_is_value;

                op = code[code_pointer];
                op_is_value = op >= opt_value && op != 255;

                // todo -> id all early outs 

                max_vector_size = max_vector_size < vector_size ? vector_size : max_vector_size;

                // increase vectorsize if a consecutive operation, reset to 1 if not, do nothing on nops or ends
                // results in wierdnes on wierd ops.. should filter those in the analyser if that could occur 

                // determine if growing a vector, ifso update the next element
                if (op != 0 && op != (byte)script_op.end)
                {
                    bool grow_vector = false;

                    if (op_is_value && prev_op_is_value)
                    {
                        grow_vector = true;
                    }
                    else
                    {
                        // if the previous op also gave back a value 
                        if (prev_op_is_value ||
                            prev_op == (byte)script_op.pop ||
                            prev_op == (byte)script_op.pop2 ||
                            prev_op == (byte)script_op.pop3 ||
                            prev_op == (byte)script_op.pop4 ||
                            prev_op == (byte)script_op.popv)
                        {
                            // we grow vector 
                            switch ((script_op)op)
                            {
                                case script_op.peek:
                                    f1 = peek(1) ? 1f : 0f;
                                    grow_vector = true;
                                    break;

                                case script_op.peekv:
                                    return (int)BlastError.stack_error_peek;

                                case script_op.pop:
                                    pop(out f1);
                                    grow_vector = true;
                                    break;

                                case script_op.pop2:
                                case script_op.pop3:
                                case script_op.pop4:
                                    return (int)BlastError.stack_error_pop_multi_from_root;

                                case script_op.popv:
                                    return (int)BlastError.stack_error_variable_pop_not_supported;

                                default:
                                    // reset vector on all other operations 
                                    vector_size = 1;
                                    break;
                            }

                        }
                    }

                    // update the current vector
                    if (grow_vector)
                    {
                        switch ((BlastVectorSizes)vector_size)
                        {
                            case BlastVectorSizes.float1:
                                f4.x = f1;
                                break;
                            case BlastVectorSizes.float2:
                                break;
                            case BlastVectorSizes.float3:
                                break;
                            case BlastVectorSizes.float4:
#if LOG_ERRORS
                    Standalone.Debug.LogError("interpretor error, expanding vector beyond size 4, this is not supported");
#endif
                                return (int)BlastError.error_update_vector_fail;
                        }
                        vector_size++;
                    }
                }

                // set result to current value 
                float4 f4_result = f4;

                // switch on an operation to take
                switch ((script_op)op)
                {
                    case script_op.end:
                        vector_size = vector_size > max_vector_size ? vector_size : max_vector_size;
                        return vector_size > 1 ? f4 : new float4(f1, 0, 0, 0);

                    case script_op.nop:
                        // assignments will end with nop and not with end as a compounded statement
                        // at this point vector_size is decisive for the returned value
                        code_pointer++;

                        vector_size = vector_size > max_vector_size ? vector_size : max_vector_size;
                        return vector_size > 1 ? f4 : new float4(f1, 0, 0, 0);

                    // arithmetic ops 
                    case script_op.add:
                    case script_op.multiply:
                    case script_op.divide:
                    // boolean ops 
                    case script_op.or:
                    case script_op.and:
                    case script_op.xor:
                    case script_op.smaller:
                    case script_op.smaller_equals:
                    case script_op.greater:
                    case script_op.greater_equals:
                    case script_op.equals:
                        {
                            current_operation = (script_op)op;
                            last_is_op_or_first = true;
                        }
                        break;

                    case script_op.not:
                        {
                            if (last_is_op_or_first)
                            {
#if LOG_ERRORS
                            if (not)
                            {
                                    Standalone.Debug.LogError("double token: ! !, this will be become nothing, it is not supported as it cant be determined to be intentional and will raise this error in editor mode only");
                            }
#endif
                                not = !not;
                            }
                            else
                            {
                                current_operation = script_op.not;
                                last_is_op_or_first = true;
                            }
                        }
                        break;

                    case script_op.substract:
                        {
                            if (last_is_op_or_first)
                            {
#if LOG_ERRORS
                            if (minus)
                            {
                                    Standalone.Debug.LogError("double token: - -, this will be become +, it is not supported as it cant be determined to be intentional and will raise this error in editor mode only");
                            }
#endif
                                minus = !minus;
                            }
                            else
                            {
                                current_operation = script_op.substract;
                                last_is_op_or_first = true;
                            }
                        }
                        break;

                    default:
                        {
                            // get result for op 
                            switch ((script_op)op)
                            {
                                case script_op.begin:
                                    ++code_pointer;
                                    // should not reach here
#if LOG_ERRORS
                                    Standalone.Debug.LogWarning("should not be nesting compounds... compiler did not do its job wel"); 
#endif
                                    f4_result = get_compound_result(ref code_pointer, ref vector_size);
                                    break;

                                case script_op.abs:
                                    get_abs_result(ref code_pointer, ref minus, ref vector_size, ref f4_result);
                                    break;

                                case script_op.select:
                                    get_select_result(ref code_pointer, ref minus, ref vector_size, ref f4_result);
                                    break;

                                case script_op.random:
                                    get_random_result(ref code_pointer, ref minus, ref vector_size, ref f4_result);
                                    break;

                                case script_op.maxa:
                                    // vector function: max of arguments 
                                    get_maxa_result(ref code_pointer, ref minus, ref vector_size, ref f4_result);
                                    break;

                                case script_op.mina:
                                    // vector function: min of arguments 
                                    get_mina_result(ref code_pointer, ref minus, ref vector_size, ref f4_result);
                                    break;

                                case script_op.fma:
                                    f4_result = get_fma_result(ref code_pointer, ref minus);
                                    break;

                                case script_op.fms:
                                    f4_result = get_fms_result(ref code_pointer, ref minus);
                                    break;

                                case script_op.fsm:
                                    f4_result = get_fsm_result(ref code_pointer, ref minus);
                                    break;

                                case script_op.mula:
                                    f4_result = get_mula_result(ref code_pointer, ref minus, ref vector_size);
                                    break;

                                case script_op.adda:
                                    f4_result = get_adda_result(ref code_pointer, ref minus, ref vector_size);
                                    break;

                                case script_op.suba:
                                    f4_result = get_suba_result(ref code_pointer, ref minus, ref vector_size);
                                    break;

                                case script_op.diva:
                                    f4_result = get_diva_result(ref code_pointer, ref minus, ref vector_size);
                                    break;

                                case script_op.all:
                                    f4_result = get_all_result(ref code_pointer, ref minus, ref not, ref vector_size);
                                    break;

                                case script_op.any:
                                    f4_result = get_any_result(ref code_pointer, ref minus, ref not, ref vector_size);
                                    break;

                                case script_op.max:
                                    f4_result = get_max_result(ref code_pointer, ref minus, ref not, ref vector_size);
                                    break;

                                case script_op.min:
                                    f4_result = get_min_result(ref code_pointer, ref minus, ref not, ref vector_size);
                                    break;

                                case script_op.lerp:
                                    get_lerp_result(ref code_pointer, ref minus, ref vector_size, ref f4_result);
                                    break;

                                case script_op.slerp:
                                    get_slerp_result(ref code_pointer, ref minus, ref vector_size, ref f4_result);
                                    break;

                                case script_op.normalize:
                                    get_normalize_result(ref code_pointer, ref minus, ref vector_size, ref f4_result);
                                    break;

                                case script_op.saturate:
                                    get_saturate_result(ref code_pointer, ref minus, ref vector_size, ref f4_result);
                                    break;

                                case script_op.clamp:
                                    get_clamp_result(ref code_pointer, ref minus, ref vector_size, ref f4_result);
                                    break;

                                case script_op.sqrt:
                                    get_sqrt_result(ref code_pointer, ref minus, ref vector_size, ref f4_result);
                                    break;

                                case script_op.rsqrt:
                                    get_rsqrt_result(ref code_pointer, ref minus, ref vector_size, ref f4_result);
                                    break;

                                case script_op.ceil:
                                    get_ceil_result(ref code_pointer, ref minus, ref vector_size, ref f4_result);
                                    break;

                                case script_op.floor:
                                    get_floor_result(ref code_pointer, ref minus, ref vector_size, ref f4_result);
                                    break;

                                case script_op.frac:
                                    get_frac_result(ref code_pointer, ref minus, ref vector_size, ref f4_result);
                                    break;

                                case script_op.sin:
                                    get_sin_result(ref code_pointer, ref minus, ref vector_size, ref f4_result);
                                    break;

                                case script_op.cos:
                                    get_cos_result(ref code_pointer, ref minus, ref vector_size, ref f4_result);
                                    break;

                                case script_op.tan:
                                    get_tan_result(ref code_pointer, ref minus, ref vector_size, ref f4_result);
                                    break;

                                case script_op.sinh:
                                    get_sinh_result(ref code_pointer, ref minus, ref vector_size, ref f4_result);
                                    break;

                                case script_op.cosh:
                                    get_cosh_result(ref code_pointer, ref minus, ref vector_size, ref f4_result);
                                    break;

                                case script_op.atan:
                                    get_atan_result(ref code_pointer, ref minus, ref vector_size, ref f4_result);
                                    break;

                                case script_op.degrees:
                                    get_degrees_result(ref code_pointer, ref minus, ref vector_size, ref f4_result);
                                    break;

                                case script_op.radians:
                                    get_rad_result(ref code_pointer, ref minus, ref vector_size, ref f4_result);
                                    break;

                                case script_op.pow:
                                    get_pow_result(ref code_pointer, ref minus, ref vector_size, ref f4_result);
                                    break;

                                case script_op.exp:
                                    get_exp_result(ref code_pointer, ref minus, ref vector_size, ref f4_result);
                                    break;

                                case script_op.log2:
                                    get_log2_result(ref code_pointer, ref minus, ref vector_size, ref f4_result);
                                    break;

                                case script_op.distance:
                                    get_distance_result(ref code_pointer, ref minus, ref vector_size, ref f4_result);
                                    break;

                                case script_op.distancesq:
                                    get_distancesq_result(ref code_pointer, ref minus, ref vector_size, ref f4_result);
                                    break;

                                case script_op.length:
                                    get_length_result(ref code_pointer, ref minus, ref vector_size, ref f4_result);
                                    break;

                                case script_op.lengthsq:
                                    get_lengthsq_result(ref code_pointer, ref minus, ref vector_size, ref f4_result);
                                    break;

                                case script_op.dot:
                                    get_dot_result(ref code_pointer, ref minus, ref vector_size, ref f4_result);
                                    break;

                                case script_op.pop:
                                    pop(out f4_result.x);
                                    f4_result.x = math.select(f4_result.x, -f4_result.x, minus);
                                    minus = false;
                                    vector_size = 1;
                                    break;

                                case script_op.pop2:
                                    pop(out f4_result.y);
                                    pop(out f4_result.x);
                                    f4_result.xy = math.select(f4_result.xy, -f4_result.xy, minus);
                                    minus = false;
                                    vector_size = 2;
                                    break;

                                case script_op.pop3:
                                    pop(out f4_result.z);
                                    pop(out f4_result.y);
                                    pop(out f4_result.x);
                                    f4_result.xyz = math.select(f4_result.xyz, -f4_result.xyz, minus);
                                    vector_size = 3;
                                    break;

                                case script_op.pop4:
                                    pop(out f4_result);
                                    f4_result = math.select(f4_result, -f4_result, minus);
                                    minus = false;
                                    vector_size = 4;
                                    break;

                                case script_op.ex_op:

                                    code_pointer++;
                                    extended_script_op exop = (extended_script_op)code[code_pointer];

                                    if (exop == extended_script_op.call)
                                    {
                                        // call external function 
                                        f4_result = CallExternal(ref code_pointer, ref minus, ref vector_size, ref f4);
                                    }
                                    else
                                    {
                                        switch (exop)
                                        {
                                            case extended_script_op.logn: f4_result = get_log_result(ref code_pointer, ref minus, ref vector_size, ref f4); break;
                                            case extended_script_op.log10: f4_result = get_log10_result(ref code_pointer, ref minus, ref vector_size, ref f4); break;
                                            case extended_script_op.exp10: f4_result = get_exp10_result(ref code_pointer, ref minus, ref vector_size, ref f4); break;
                                            case extended_script_op.cross: f4_result = get_cross_result(ref code_pointer, ref minus, ref vector_size, ref f4); break;
                                        }

                                        // handle op 
#if LOG_ERRORS
                                        Standalone.Debug.LogError($"get_extended_operation_result: operation {op} not handled / supported / implemented ");
#endif
                                    }
                                    // prev_op = (byte)script_op.ex_op;
                                    // prev_op_is_value = false;
                                    op = (byte)script_op.nop;
                                    op_is_value = true;
                                    last_is_op_or_first = code_pointer == 0;
                                    break;

                                case script_op.ret:
                                    code_pointer = package->info.code_size + 1;
                                    return float4.zero;

                                // this should give us a decent jump-table, saving some conditional jumps later  
                                case script_op.pi:
                                case script_op.inv_pi:
                                case script_op.epsilon:
                                case script_op.infinity:
                                case script_op.negative_infinity:
                                case script_op.nan:
                                case script_op.min_value:
                                case script_op.value_0:
                                case script_op.value_1:
                                case script_op.value_2:
                                case script_op.value_3:
                                case script_op.value_4:
                                case script_op.value_8:
                                case script_op.value_10:
                                case script_op.value_16:
                                case script_op.value_24:
                                case script_op.value_32:
                                case script_op.value_64:
                                case script_op.value_100:
                                case script_op.value_128:
                                case script_op.value_256:
                                case script_op.value_512:
                                case script_op.value_1000:
                                case script_op.value_1024:
                                case script_op.value_30:
                                case script_op.value_45:
                                case script_op.value_90:
                                case script_op.value_180:
                                case script_op.value_270:
                                case script_op.value_360:
                                case script_op.inv_value_2:
                                case script_op.inv_value_3:
                                case script_op.inv_value_4:
                                case script_op.inv_value_8:
                                case script_op.inv_value_10:
                                case script_op.inv_value_16:
                                case script_op.inv_value_24:
                                case script_op.inv_value_32:
                                case script_op.inv_value_64:
                                case script_op.inv_value_100:
                                case script_op.inv_value_128:
                                case script_op.inv_value_256:
                                case script_op.inv_value_512:
                                case script_op.inv_value_1000:
                                case script_op.inv_value_1024:
                                case script_op.inv_value_30:
                                case script_op.inv_value_45:
                                case script_op.inv_value_90:
                                case script_op.inv_value_180:
                                case script_op.inv_value_270:
                                case script_op.inv_value_360:
                                    {
                                        // index constant 
                                        temp = engine_ptr->constants[op];

                                        // assume constant is float[1] 
                                        temp = math.select(temp, -temp, minus);
                                        minus = false;
                                        if (last_is_op_or_first)
                                        {
                                            //the last thing was an operation or the first thing, set value
                                            switch ((BlastVectorSizes)vector_size)
                                            {
                                                case BlastVectorSizes.float1: f4_result.x = temp; break;
                                                case BlastVectorSizes.float2: f4_result.xy = temp; break;
                                                case BlastVectorSizes.float3: f4_result.xyz = temp; break;
                                                case BlastVectorSizes.float4: f4_result.xyzw = temp; break;
                                                default:
#if LOG_ERRORS
                                                    Standalone.Debug.LogError("error: growing vector beyond size 4 from constant");
#endif
                                                    break;
                                            }
                                        }
                                        else
                                        {
                                            // growing a vector
                                            switch ((BlastVectorSizes)vector_size)
                                            {
                                                case BlastVectorSizes.float1: f4_result.x = temp; break;
                                                case BlastVectorSizes.float2: f4_result.y = temp; break;
                                                case BlastVectorSizes.float3: f4_result.z = temp; break;
                                                case BlastVectorSizes.float4: f4_result.w = temp; break;
                                                default:
#if LOG_ERRORS
                                                    Standalone.Debug.LogError("error: growing vector beyond size 4 from constant");
#endif
                                                    break;
                                            }
                                        }
                                    }
                                    break; 

                                default:
                                    {
                                        if (op >= opt_id)
                                        {
                                            // lookup identifier value
                                            byte id = (byte)(op - opt_id);
                                            byte this_vector_size = BlastInterpretor.GetMetaDataSize(metadata, id); // this now uses an offsetted index into metadata...   this should work...

                                            if (last_is_op_or_first)
                                            {
                                                switch ((BlastVectorSizes)this_vector_size)
                                                {
                                                    case BlastVectorSizes.float1:
                                                        {
                                                            temp = ((float*)data)[id];
                                                            f4_result.x = math.select(temp, -temp, minus);
                                                            minus = false;
                                                        }
                                                        break;

                                                    case BlastVectorSizes.float2:
                                                        {
                                                            float2 t2 = new float2(((float*)data)[id], ((float*)data)[id + 1]);
                                                            f4_result.xy = math.select(t2, -t2, minus);
                                                            minus = false;
                                                        }
                                                        break;

                                                    case BlastVectorSizes.float3:
                                                        {
                                                            float3 t3 = new float3(((float*)data)[id], ((float*)data)[id + 1], ((float*)data)[id + 2]);
                                                            f4_result.xyz = math.select(t3, -t3, minus);
                                                            minus = false;
                                                        }
                                                        break;
                                                    case BlastVectorSizes.float4:
                                                        {
                                                            float4 t4 = new float4(((float*)data)[id], ((float*)data)[id + 1], ((float*)data)[id + 2], ((float*)data)[id + 3]);
                                                            f4_result = math.select(t4, -t4, minus);
                                                            minus = false;
                                                        }
                                                        break;
                                                }
                                            }
                                            else
                                            {
                                                // growing a vector
                                                switch ((BlastVectorSizes)this_vector_size)
                                                {
                                                    case BlastVectorSizes.float1:
                                                        temp = ((float*)data)[id];
                                                        f4_result[vector_size - 1] = math.select(temp, -temp, minus);
                                                        minus = false;
                                                        break;
                                                    case BlastVectorSizes.float2: 
                                                    case BlastVectorSizes.float3: 
                                                    case BlastVectorSizes.float4: 
                                                    default:
                                                        Debug.LogError("error: growing vector from other vectors not fully supported");
                                                        break;
                                                }
                                            }

                                            vector_size = vector_size > this_vector_size ? vector_size : this_vector_size; // probably massively incorrect but its too late 
                                        }
                                        else
                                        {
                                            // we receive an operation while expecting a value 
                                            //
                                            // if we predict this at the compiler we could use the opcodes value as value constant
                                            //
                                            Standalone.Debug.LogError($"encountered unknown op {(script_op)op}");

                                            // this should screw stuff up 
                                            return new float4(float.NaN, float.NaN, float.NaN, float.NaN);
                                        }
                                    }
                                    break;
                            }

                            // handle results according to vector size 
                            // max vector size is set before settings vector_size and will be update NEXT loop
                            switch ((BlastVectorSizes)vector_size)
                            {
                                case BlastVectorSizes.float1:
                                    {
                                        switch ((BlastVectorSizes)max_vector_size)
                                        {
                                            case BlastVectorSizes.float1:
                                                BlastInterpretor.handle_op(current_operation, op, prev_op, ref f1, f4_result.x, ref last_is_op_or_first, ref vector_size);
                                                break;
                                            case BlastVectorSizes.float2:
                                                BlastInterpretor.handle_op(current_operation, op, prev_op, ref f4, new float4(f4_result.xx, 0, 0), ref last_is_op_or_first, ref vector_size);
                                                break;
                                            case BlastVectorSizes.float3:
                                                BlastInterpretor.handle_op(current_operation, op, prev_op, ref f4, new float4(f4_result.xxx, 0), ref last_is_op_or_first, ref vector_size);
                                                break;
                                            case BlastVectorSizes.float4:
                                                BlastInterpretor.handle_op(current_operation, op, prev_op, ref f4, f4_result.xxxx, ref last_is_op_or_first, ref vector_size);
                                                break;
                                        }
                                    }
                                    break;

                                case BlastVectorSizes.float2:
                                    {
                                        switch ((BlastVectorSizes)max_vector_size)
                                        {
                                            case BlastVectorSizes.float1:
                                                // normal when growing a vector 
                                                break;
                                            case BlastVectorSizes.float2:
                                            case BlastVectorSizes.float3:
                                            case BlastVectorSizes.float4:
                                            default:
#if LOG_ERRORS
                                                // these will be in log because of growing a vector from a compound of unknown sizes  
                                                Standalone.Debug.LogWarning($"no support for vector of size {vector_size} -> {max_vector_size}");
#endif
                                                break;
                                        }
                                        BlastInterpretor.handle_op(current_operation, op, prev_op, ref f4, f4_result.xy, ref last_is_op_or_first, ref vector_size);
                                    }
                                    break;

                                case BlastVectorSizes.float3:
                                    {
                                        switch ((BlastVectorSizes)max_vector_size)
                                        {
                                            case BlastVectorSizes.float2:
                                                // normal when growing a vector
                                                break;

                                            case BlastVectorSizes.float1:
                                            case BlastVectorSizes.float3:
                                            case BlastVectorSizes.float4:
                                            default:
#if LOG_ERRORS
                                                // these will be in log because of growing a vector from a compound of unknown sizes  
                                                Standalone.Debug.LogWarning($"no support for vector of size {vector_size} -> {max_vector_size}");
#endif
                                                break;
                                        }
                                        BlastInterpretor.handle_op(current_operation, op, prev_op, ref f4, f4_result.xyz, ref last_is_op_or_first, ref vector_size);
                                    }
                                    break;

                                case BlastVectorSizes.float4:
                                    {
                                        switch ((BlastVectorSizes)max_vector_size)
                                        {
                                            case BlastVectorSizes.float1:
                                                // have a float 4 but are multiplying with a float1 from earlier
                                                f4 = new float4(f1, f1, f1, f1);
                                                break;
                                            case BlastVectorSizes.float2:
                                                f4 = new float4(f4.xy, 0, 0);
                                                break;
                                            case BlastVectorSizes.float3:
                                                f4 = new float4(f4.xyz, 0);
                                                // usually we are growing from float4 to a 4 vector, current op would be nop then 
                                                break;
                                            case BlastVectorSizes.float4:
                                                // have a float 4 and one from earlier.
                                                break;
                                            default:
#if LOG_ERRORS
                                                Standalone.Debug.LogError("implement me");
#endif
                                                break;
                                        }
                                        BlastInterpretor.handle_op(current_operation, op, prev_op, ref f4, f4_result, ref last_is_op_or_first, ref vector_size);
                                    }
                                    break;

                                default:
                                    {
#if LOG_ERRORS
                                        Standalone.Debug.LogError($"encountered unsupported vector size of {vector_size}");
#endif
                                        return new float4(float.NaN, float.NaN, float.NaN, float.NaN);
                                    }
                            }
                        }
                        break;

                }


                code_pointer++;

            } // while ()
            vector_size = vector_size > max_vector_size ? vector_size : max_vector_size;
            return vector_size > 0 ? f4 : new float4(f1, 0, 0, 0);
        }
 

        /// <summary>
        /// 
        /// </summary>
        /// <param name="blast"></param>
        /// <param name="register"></param>
        /// <param name="code_pointer"></param>
        /// <returns></returns>
        unsafe int execute([NoAlias]BlastEngineData* blast, [NoAlias]IntPtr environment, [NoAlias]IntPtr caller, in float4 register, ref int code_pointer)
        {
            if(code_pointer > 0)
            {
                ;
            }

            int iterations = 0;
            float4 f4_register = register;

            engine_ptr = blast; 
            environment_ptr = environment;
            caller_ptr = caller; 

            // main loop 
            while (code_pointer < package->info.code_size)
            {
                iterations++;
                if (iterations > max_iterations) return (int)BlastError.error_max_iterations;

                byte vector_size = 1;
                script_op op = (script_op)code[code_pointer];
                code_pointer++;

                // 
                //   determine if next op needs a compound result
                // - if compound needed determine offset into start 
                // - execute compound here inline 
                // - then switch further (codepointer needs different value) 
                //
                // this would eliminate the getcompoundresult function call 
                //


                //  if getcompound needed
                //   - copy codepointer
                //   - execute compound 
                //   - put back code pointer 
                //   - switch to operation 
                //   --- in that operation case we know it needed compound so there we advance codepointer
                float* fdata = (float*)data;

                switch (op)
                {
                    case script_op.ret:
                        {
                            return 0;
                        }

                    case script_op.nop:
                        break;

                    case script_op.pop:
                        pop(out f4_register.x);
                        fdata[code[code_pointer++] - opt_id] = f4_register.x;
                        break;

                    case script_op.pop2:
                        pop(out f4_register.y);
                        pop(out f4_register.x);
                        fdata[code[code_pointer] - opt_id] = f4_register.x;
                        fdata[code[code_pointer] - opt_id + 1] = f4_register.y;
                        code_pointer++;
                        break;

                    case script_op.pop3:
                        pop(out f4_register.z);
                        pop(out f4_register.y);
                        pop(out f4_register.x);
                        fdata[code[code_pointer] - opt_id] = f4_register.x;
                        fdata[code[code_pointer] - opt_id + 1] = f4_register.y;
                        fdata[code[code_pointer] - opt_id + 2] = f4_register.z;
                        code_pointer++;
                        break;

                    case script_op.pop4:
                        pop(out f4_register.w);
                        pop(out f4_register.z);
                        pop(out f4_register.y);
                        pop(out f4_register.x);
                        fdata[code[code_pointer] - opt_id] = f4_register.x;
                        fdata[code[code_pointer] - opt_id + 1] = f4_register.y;
                        fdata[code[code_pointer] - opt_id + 2] = f4_register.z;
                        fdata[code[code_pointer] - opt_id + 3] = f4_register.w;
                        code_pointer++;
                        break;

                    case script_op.popv:
                        switch (code[code_pointer++])
                        {
                            case 1:
                                pop(out f4_register.x);
                                fdata[code[code_pointer++] - opt_id] = f4_register.x;
                                break;
                            case 2:
                                pop(out f4_register.y);
                                pop(out f4_register.x);
                                fdata[code[code_pointer] - opt_id] = f4_register.x;
                                fdata[code[code_pointer] - opt_id + 1] = f4_register.y;
                                code_pointer++;
                                break;
                            case 3:
                                pop(out f4_register.z);
                                pop(out f4_register.y);
                                pop(out f4_register.x);
                                fdata[code[code_pointer] - opt_id] = f4_register.x;
                                fdata[code[code_pointer] - opt_id + 1] = f4_register.y;
                                fdata[code[code_pointer] - opt_id + 2] = f4_register.z;
                                code_pointer++;
                                break;
                            case 4:
                                pop(out f4_register.w);
                                pop(out f4_register.z);
                                pop(out f4_register.y);
                                pop(out f4_register.x);
                                fdata[code[code_pointer] - opt_id] = f4_register.x;
                                fdata[code[code_pointer] - opt_id + 1] = f4_register.y;
                                fdata[code[code_pointer] - opt_id + 2] = f4_register.z;
                                fdata[code[code_pointer] - opt_id + 3] = f4_register.w;
                                code_pointer++;
                                break;
                            default:

#if LOG_ERRORS
                            Standalone.Debug.LogError("burstscript.interpretor error: variable vector size not yet supported on stack pop");
#endif
                                // could use set_datasize for var size vectors 
                                break;
                        }
                        break;

                    case script_op.push:

                        if (code[code_pointer] == (byte)script_op.begin)
                        {
                            code_pointer++;

                            // push the vector result of a compound 
                            f4_register = get_compound_result(ref code_pointer, ref vector_size);
                            switch (vector_size)
                            {
                                case 1:
                                    push(f4_register.x);
                                    break;
                                case 2:
                                    push(f4_register.x);
                                    push(f4_register.y);
                                    break;
                                case 3:
                                    push(f4_register.x);
                                    push(f4_register.y);
                                    push(f4_register.z);
                                    break;
                                case 4:
                                    push(f4_register);
                                    break;
                                default:
#if LOG_ERRORS
                                Standalone.Debug.LogError("burstscript.interpretor error: variable vector size not yet supported on stack push of compound");
#endif
                                    return (int)BlastError.error_variable_vector_compound_not_supported;
                            }
                            // code_pointer should index after compound now
                        }
                        else
                        {
                            // else push 1 float of data
                            push(fdata[code[code_pointer++] - opt_id]);
                        }

                        break;

                    case script_op.pushv:
                        byte b;
                        switch (code[code_pointer++])
                        {
                            case 1:
                                b = (byte)(code[code_pointer] - opt_id);
                                push(fdata[b]);
                                code_pointer++;
                                break;
                            case 2:
                                b = (byte)(code[code_pointer] - opt_id);
                                push(fdata[b]);
                                push(fdata[b + 1]);
                                code_pointer++;
                                break;
                            case 3:
                                b = (byte)(code[code_pointer] - opt_id);
                                push(fdata[b]);
                                push(fdata[b + 1]);
                                push(fdata[b + 2]);
                                code_pointer++;
                                break;
                            case 4:
                                b = (byte)(code[code_pointer] - opt_id);
                                push(fdata[b]);
                                push(fdata[b + 1]);
                                push(fdata[b + 2]);
                                push(fdata[b + 3]);
                                code_pointer++;
                                break;
                            default:
#if LOG_ERRORS
                            Standalone.Debug.LogError("burstscript.interpretor error: variable vector size not yet supported on stack push");
#endif
                                return (int)BlastError.error_variable_vector_op_not_supported;
                        }
                        break;

                    case script_op.pushf:
                        // push the result of a function directly onto the stack instead of assigning it first or something

                        // get result of compound 
                        f4_register = get_compound_result(ref code_pointer, ref vector_size);

                        switch(vector_size)
                        {
                            case 1: push(f4_register.x); break;
                            case 2: push(f4_register.x); break;
                            case 3: push(f4_register.x); break;
                            case 4: push(f4_register.x); break;
                            default:
#if LOG_ERRORS
                            Standalone.Debug.LogError("burstscript.interpretor error: variable vector size not yet supported on stack push");
#endif
                                return (int)BlastError.error_variable_vector_op_not_supported;
                        }
                        break; 


                    case script_op.yield:
                        yield(f4_register);
                        return (int)BlastError.yield;

                    case script_op.seed:
                        f4_register.x = pop_or_value(++code_pointer);
                        engine_ptr->Seed(math.asuint(f4_register.x));
                        break;

                    case script_op.begin:
                        {
                            break;

#if LOG_ERRORS
                        Standalone.Debug.LogError("burstscript.interpretor error: begin-end sequence not expected in root");
#endif
                            return (int)BlastError.error_begin_end_sequence_in_root;
                        }

                    case script_op.end:
                        break;

                    /// if the compiler can detect:
                    /// - simple assign (1 function call or 1 parameter set)
                    /// then use an extra op:  assign1 that doesnt go into get_compound
                    case script_op.assign:
                        {
                            // advance 1 if on assign 
                            code_pointer += math.select(0, 1, code[code_pointer] == (byte)script_op.assign);

                            byte assignee_op = code[code_pointer];
                            byte assignee = (byte)(assignee_op - opt_id);

                            // advance 2 if on begin 1 otherwise
                            code_pointer += math.select(1, 2, code[code_pointer + 1] == (byte)script_op.begin);

                            // get vectorsize of the assigned parameter 
                            // vectorsize assigned MUST match 
                            byte s_assignee = BlastInterpretor.GetMetaDataSize(in metadata, in assignee);

                            // 3 options: 
                            // - a mixed sequence - no nested compound
                            // - a compound -> vector definition? push? compiler flattens everything else
                            // - a function call with sequence after... 
                            //

                            f4_register = get_compound_result(ref code_pointer, ref vector_size);

                            // set assigned data fields in stack 
                            switch ((byte)vector_size)
                            {
                                case (byte)BlastVectorSizes.float1:
                                    //if(Unity.Burst.CompilerServices.Hint.Unlikely(s_assignee != 1))
                                    if (s_assignee != 1)
                                    {
#if LOG_ERRORS  
                                          Standalone.Debug.LogError($"blast: assigned vector size mismatch at #{code_pointer}, should be size '{s_assignee}', evaluated '1', data id = {assignee}");
#endif
                                        return (int)BlastError.error_assign_vector_size_mismatch;
                                    }
                                    fdata[assignee] = f4_register.x;
                                    break;

                                case (byte)BlastVectorSizes.float2:
                                    if (s_assignee != 2)
                                    {
#if LOG_ERRORS
                                    Standalone.Debug.LogError($"blast: assigned vector size mismatch at #{code_pointer}, should be size '{s_assignee}', evaluated '2'");
#endif
                                        return (int)BlastError.error_assign_vector_size_mismatch;
                                    }
                                    fdata[assignee] = f4_register.x;
                                    fdata[assignee + 1] = f4_register.y;
                                    break;

                                case (byte)BlastVectorSizes.float3:
                                    if (s_assignee != 3)
                                    {
#if LOG_ERRORS
                                    Standalone.Debug.LogError($"blast: assigned vector size mismatch at #{code_pointer}, should be size '{s_assignee}', evaluated '3'");
#endif
                                        return (int)BlastError.error_assign_vector_size_mismatch;
                                    }
                                    fdata[assignee] = f4_register.x;
                                    fdata[assignee + 1] = f4_register.y;
                                    fdata[assignee + 2] = f4_register.z;
                                    break;

                                case (byte)BlastVectorSizes.float4:
                                    if (s_assignee != 4)
                                    {
#if LOG_ERRORS
                                    Standalone.Debug.LogError($"blast: assigned vector size mismatch at #{code_pointer}, should be size '{s_assignee}', evaluated '4'");
#endif
                                        return (int)BlastError.error_assign_vector_size_mismatch;
                                    }
                                    fdata[assignee] = f4_register.x;
                                    fdata[assignee + 1] = f4_register.y;
                                    fdata[assignee + 2] = f4_register.z;
                                    fdata[assignee + 3] = f4_register.w;
                                    break;

#if LOG_ERRORS
                            default:
                                Standalone.Debug.LogError($"burstscript.interpretor: vector size {vector_size} not allowed");
                                return (int)BlastError.error_unsupported_operation_in_root; 
#endif
                            }
                        }
                        break;

                    case script_op.jz:
                        {
                            // calc endlocation of 
                            // code_pointer++;
                            int offset = code[code_pointer];
                           // if(offset == 19)
                           // {
                              //  Debug.LogWarning(" TODO REMVOE ME");
                           // }
                            int jump_to = code_pointer + offset;

                            // eval condition 
                            code_pointer += math.select(1, 2, code[code_pointer + 1] == (byte)script_op.begin);
                            f4_register = get_compound_result(ref code_pointer, ref vector_size);

                            // jump if zero to else condition or after then when no else 
                            //int jump_to = code_pointer + offset;
                            code_pointer = math.select(code_pointer, jump_to, f4_register.x == 0);

                            //if(f4_register.x == 0)
                            //{
                            //    break; 
                            //}
                        }
                        break;

                    case script_op.jnz:
                        {
                            // calc endlocation of 
                            // code_pointer++;
                            int offset = code[code_pointer];
                            int jump_to = code_pointer + offset;

                            // eval condition 
                            code_pointer += math.select(1, 2, code[code_pointer + 1] == (byte)script_op.begin);
                            f4_register = get_compound_result(ref code_pointer, ref vector_size);

                            // jump if NOT zero to else condition or after then when no else 
                            code_pointer = math.select(code_pointer, jump_to, f4_register.x != 0);
                        }
                        break;

                    case script_op.jump:
                        {
                            int offset = code[code_pointer];
                            code_pointer = code_pointer + offset;
                            break;
                        }

                    case script_op.jump_back:
                        {
                            int offset = code[code_pointer];
                            code_pointer = code_pointer - offset;
                            break;
                        }


                    case script_op.ex_op:
                        {                                                                 
                            extended_script_op exop = (extended_script_op)code[code_pointer];

                            switch (exop)
                            {
                                case extended_script_op.call:
                                    {
                                        // call external function, any returned data is neglected on root 
                                        bool minus = false;
                                        CallExternal(ref code_pointer, ref minus, ref vector_size, ref f4_register);
                                        code_pointer++;
                                    }
                                    break;

                                case extended_script_op.debug:
                                    {
                                        code_pointer++;

                                        int op_id = code[code_pointer];
                                        BlastVariableDataType datatype = default;

                                        // even if not in debug, if the command is encoded any stack op needs to be popped 
                                        void* pdata = pop_with_info(code_pointer, out datatype, out vector_size);

#if HANDLE_DEBUG_OP
                                        Handle_DebugData(code_pointer, vector_size, op_id, datatype, pdata);
#endif 
                                        code_pointer++;
                                    }
                                  break; 

                                default:
                                    {
#if LOG_ERRORS
                                        Debug.LogError($"extended op: only call operation is supported from root, not {exop} ");
#endif
                                        return (int)BlastError.error_unsupported_operation_in_root;

                                    }
                            }
                            break;
                        }

                    default:
                        {
#if LOG_ERRORS
                            Debug.LogError($"burstscript.interpretor: operation {(byte)op} '{op}' not supported in root codepointer = {code_pointer}");
#endif
                            return (int)BlastError.error_unsupported_operation_in_root;
                        }
                }
            }

            return (int)BlastError.success;
        }


#endregion

    };



}



