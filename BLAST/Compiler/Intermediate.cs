﻿using NSS.Blast.Interpretor;
using System;
using Unity.Collections;
using Unity.Mathematics;

namespace NSS.Blast.Compiler
{

    /// <summary>
    /// intermediate bytecode data for use by compiler 
    /// </summary>
    unsafe public struct BlastIntermediate
    {
        const byte opt_id = (byte)script_op.id;
        const byte opt_value = (byte)script_op.pi;

        // large max size 
        public const int data_capacity = 256 * 4;
        public const int code_capacity = 1024;

        /// <summary>
        /// unique script id
        /// </summary>
        public int Id;


        /// <summary>
        /// size of code in bytes
        /// </summary>
        public int code_size;

        /// <summary>
        /// index into bytecode, next point of execution, if == code_size then end of script is reached
        /// </summary>
        public int code_pointer;

        /// <summary>
        /// offset into data after the last variable, from here the stack starts 
        /// </summary>
        internal byte data_offset;

        /// <summary>
        /// maximum reached stack size in floats  
        /// </summary>
        public byte max_stack_size;

        public byte padding;

        // -------------------- 16 byte boundary here --------------------

        /// <summary>
        /// byte code compiled from script
        /// </summary>
        public fixed byte code[code_capacity];

        /// <summary>
        /// input, output and scratch data fields
        /// </summary>
        public fixed float data[data_capacity];         // 64 * 4 = 256 bytes

        // we support max 16n vectors (float4x4), need 4 bits / stack element of 32 bits == 1/8 of size
        //
        // whenever we push we could set datasize
        // and on pop check datasize 
        // 
        //  this would introduce at least 1 branch in al hot loops, let compiler fix it with popn and pushv
        // 
        // -- we could make it a compile switch for debugging --
        //
        public fixed byte data_sizes[data_capacity / 4];


        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public float get_float(in int variable_index)
        {
            return data[variable_index];
        }

        internal void SetMetaData(in byte size, in byte offset)
        {
            fixed (byte* metadata = data_sizes)
            {
                BlastInterpretor.SetMetaData(metadata, size, offset);
            }
        }

        internal int Validate(in IntPtr blast)
        {
            return Execute(in blast, true); 
        }

        /// <summary>
        /// execute the intermediate for validation and stack info 
        /// </summary>
        /// <param name="blast"></param>
        /// <returns></returns>
        internal int Execute(in IntPtr blast, bool validation_run = false)
        {
            // setup a package to run 
            BlastPackage pkg = new BlastPackage()
            {
                code_pointer = 0,
                data_offset = data_offset,
                data_sizes_start = 0,
                data_start = 0,
                stack_offset = 0,
                info = new BlastPackageInfo()
                {
                    allocator = (byte)Allocator.Invalid,
                    code_size = (ushort)code_size,
                    data_size = (ushort)data_offset,              // offset in #float
                    package_mode = BlastPackageMode.Compiler,
                    package_size = (ushort)(code_capacity + data_capacity),
                    stack_size = (ushort)((data_capacity / 4) - data_offset),   // todo verify this?? todo
                    unused_padding_1 = 0,
                    unused_padding_2 = 0
                },
            };

            // run the interpretation 
            BlastInterpretor blaster = new BlastInterpretor();

            fixed (byte* pcode = code)
            fixed (float* pdata = data)
            fixed (byte* pmetadata = data_sizes)
            {
                // estimate stack size while at it: set stack memory too all INF's, 
                // this assumes the intermediate has more then enough stack
                float* stack = pdata + pkg.data_offset;
                for (int i = 0; i < pkg.info.stack_size; i++)
                {
                    stack[i] = math.INFINITY;
                }

                // set package 
                blaster.SetPackage(&pkg, pcode, pdata, pmetadata, pdata + pkg.data_offset);
                blaster.ValidationMode = validation_run; 

                // run it 
                int exitcode = blaster.Execute(blast);
                if (exitcode != (int)BlastError.success) return exitcode;

                // determine used stack size from nr of stack slots not INF anymore 
                max_stack_size = 0;
                while (!math.isinf(stack[max_stack_size]) && max_stack_size < pkg.info.stack_size) max_stack_size++;

                // if we ran out of stack we should return that error 
                if (max_stack_size >= pkg.info.stack_size)
                {
                    return (int)BlastError.validate_error_stack_too_small;
                }
            }

            return (int)BlastError.success;
        }
    }
}