using NSS.Blast.Interpretor;
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
        const byte opt_id = (byte)blast_operation.id;
        const byte opt_value = (byte)blast_operation.pi;

        // large max size 
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member 'BlastIntermediate.data_capacity'
        public const int data_capacity = 256; // in elements
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member 'BlastIntermediate.data_capacity'
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member 'BlastIntermediate.code_capacity'
        public const int code_capacity = 1024; // in bytes 
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member 'BlastIntermediate.code_capacity'
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member 'BlastIntermediate.data_element_bytesize'
        public const int data_element_bytesize = 4; 
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member 'BlastIntermediate.data_element_bytesize'

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
        internal byte data_count;


        /// <summary>
        /// maximum reached stack size in floats  
        /// </summary>
        public byte max_stack_size;

        /// <summary>
        /// byte code compiled from script
        /// </summary>
        public fixed byte code[code_capacity];

        /// <summary>
        /// input, output and scratch data fields
        /// </summary>
        public fixed float data[data_capacity];        

        //
        // - max 16n vectors up until (float4x4)   datasize = lower 4 bits + 1
        // - otherwise its a pointer (FUTURE)      
        // - datatype                              datatype = high 4 bits >> 4

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member 'BlastIntermediate.metadata'
        public fixed byte metadata[data_capacity];
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member 'BlastIntermediate.metadata'


        /// <summary>
        /// nr of data elements (presumably 32bits so 4bytes/element) - same as data_offset, added for clarity
        /// </summary>
        public byte DataCount => data_count;
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member 'BlastIntermediate.DataByteSize'
        public int DataByteSize => data_count * data_element_bytesize;
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member 'BlastIntermediate.DataByteSize'

        /// <summary>
        /// read a float from the datasegement at given element index
        /// </summary>
        /// <param name="data_segment_index">element index into data segment</param>
        /// <returns>the data </returns>

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public float GetDataSegmentElement(in int data_segment_index)
        {
            return data[data_segment_index];
        }


        internal void SetMetaData(in BlastVariableDataType datatype, in byte size, in byte offset)
        {
            fixed (byte* metadata = this.metadata)
            {
                BlastInterpretor.SetMetaData(metadata, datatype, size, offset);
            }
        }

        /// <summary>
        /// validate intermediate 
        /// </summary>
        public int Validate(in IntPtr blast)
        {
            return Execute(in blast, true); 
        }

        /// <summary>
        /// execute the intermediate for validation and stack info 
        /// </summary>
        /// <param name="blast"></param>
        /// <returns></returns>
#pragma warning disable CS1573 // Parameter 'validation_run' has no matching param tag in the XML comment for 'BlastIntermediate.Execute(in IntPtr, bool)' (but other parameters do)
        public int Execute(in IntPtr blast, bool validation_run = false)
#pragma warning restore CS1573 // Parameter 'validation_run' has no matching param tag in the XML comment for 'BlastIntermediate.Execute(in IntPtr, bool)' (but other parameters do)
        {
            // setup a package to run 
            BlastPackageData package = default;
            
            package.PackageMode = BlastPackageMode.Compiler;
            package.LanguageVersion = BlastLanguageVersion.BS1;
            package.Flags = BlastPackageFlags.None;
            package.Allocator = (byte)Allocator.None; 
            
            package.O1 = (ushort)code_size;
            package.O2 = (ushort)data_capacity; // 1 byte of metadata for each capacity 
            package.O3 = (ushort)(data_count * 4);  // 4 bytes / dataelement 
            package.O4 = (ushort)(data_capacity * 4); // capacity is in byte count on intermediate 

            int initial_stack_offset = package.DataSegmentStackOffset / 4; 

            // run the interpretation 
            BlastInterpretor blaster = new BlastInterpretor();

            fixed (byte* pcode = code)
            fixed (float* pdata = data)
            fixed (byte* pmetadata = metadata)
            {
                // estimate stack size while at it: set stack memory too all INF's, 
                // this assumes the intermediate has more then enough stack
                float* stack = pdata + initial_stack_offset;
                for (int i = 0; i < package.StackCapacity; i++)
                {
                    stack[i] = math.INFINITY;
                }

                // set package 
                blaster.SetPackage(package, pcode, pdata, pmetadata, initial_stack_offset);
                blaster.ValidationMode = validation_run; 

                // run it 
                int exitcode = blaster.Execute(blast);
                if (exitcode != (int)BlastError.success) return exitcode;

                // determine used stack size from nr of stack slots not INF anymore 
                max_stack_size = 0;
                while (!math.isinf(stack[max_stack_size]) && max_stack_size < package.StackCapacity) max_stack_size++;

                // if we ran out of stack we should return that error 
                if (max_stack_size >= package.StackCapacity)
                {
                    return (int)BlastError.validate_error_stack_too_small;
                }
            }

            return (int)BlastError.success;
        }


    }
}