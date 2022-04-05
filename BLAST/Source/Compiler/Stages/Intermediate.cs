//##########################################################################################################
// Copyright © 2022 Rob Lemmens | NijnStein Software <rob.lemmens.s31@gmail.com> All Rights Reserved  ^__^\#
// Unauthorized copying of this file, via any medium is strictly prohibited                           (oo)\#
// Proprietary and confidential                                                                       (__) #
//##########################################################################################################
using NSS.Blast.Interpretor;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine.Assertions;

namespace NSS.Blast.Compiler
{
    namespace Stage
    {
        /// <summary>
        /// jump label types, used for reference during compilation 
        /// </summary>
        internal enum JumpLabelType
        {
            Jump,
            Offset,
            Label,
            
            Constant, // = Label, 
            ReferenceToConstant, // = Jump

            Skip // after resolving jump-offsets any nop labeled with skip should be removed 
        }



        /// <summary>
        /// a jump label, for help keeping track of jump targets 
        /// </summary>
        public class IMJumpLabel
        {
            internal string id { get; private set; }
            internal JumpLabelType type { get; private set; }
            internal bool is_label => type == JumpLabelType.Label;
            internal bool is_offset => type == JumpLabelType.Offset;
            internal bool is_jump => type == JumpLabelType.Jump;
            internal bool is_constant => type == JumpLabelType.Constant;
            internal bool is_constantreference => type == JumpLabelType.ReferenceToConstant;
            internal IMJumpLabel(string _id, JumpLabelType _type)
            {
                id = _id;
                type = _type;
            }

            static internal IMJumpLabel Jump(string _id)
            {
                return new IMJumpLabel(_id, JumpLabelType.Jump);
            }

            static internal IMJumpLabel Skip()
            {
                return new IMJumpLabel(Guid.NewGuid().ToString(), JumpLabelType.Skip); 
            }

            static internal IMJumpLabel Label(string _id)
            {
                return new IMJumpLabel(_id, JumpLabelType.Label);
            }

            static internal string ConstantName(string _id)
            {
                return $"constant_{_id}";
            }

            static internal IMJumpLabel Constant(string _id)
            {
                return new IMJumpLabel(ConstantName(_id), JumpLabelType.Constant);
            }

            static internal IMJumpLabel ReferenceToConstant(IMJumpLabel constantlabel)
            {
                Assert.IsNotNull(constantlabel);
                Assert.IsTrue(constantlabel.is_constant); 
                return new IMJumpLabel(constantlabel.id, JumpLabelType.ReferenceToConstant);
            }

            static internal IMJumpLabel Offset(string _id)
            {
                return new IMJumpLabel(_id, JumpLabelType.Offset);
            }

            static internal IMJumpLabel OffsetToConstant(IMJumpLabel constantlabel)
            {
                Assert.IsNotNull(constantlabel);
                Assert.IsTrue(constantlabel.is_constant);
                return new IMJumpLabel(constantlabel.id, JumpLabelType.Offset);
            }

            /// <summary>
            /// tostring override
            /// </summary>
            public override string ToString()
            {
                switch (type)
                {
                    case JumpLabelType.Offset: return $"offset: {id}";
                    case JumpLabelType.Jump: return $"jump to: {id}";

                    default:
                    case JumpLabelType.Label: return $"label: {id}";

                    case JumpLabelType.Constant: return $"constant: {id}";
                    case JumpLabelType.ReferenceToConstant: return $"constantreference: {id}"; 
                }
            }
        }


        /// <summary>
        /// intermediate bytecode - can contain additional data
        /// </summary>
        public class IMByteCode
        {
            /// <summary>
            /// the bytecode
            /// </summary>
            public byte code { get; private set; }

            /// <summary>
            /// possible translation
            /// </summary>
            public blast_operation op => (blast_operation)code;

            internal List<IMJumpLabel> labels { get; private set; }
            internal IMByteCode(byte _code, IMJumpLabel label = null)
            {
                code = _code;
                if (label == null)
                {
                    labels = null;
                }
                else
                {
                    labels = new List<IMJumpLabel>();
                    labels.Add(label);
                }
            }

            internal void AddLabel(IMJumpLabel label)
            {
                if (labels == null)
                {
                    labels = new List<IMJumpLabel>();
                }
                labels.Add(label);
            }

            internal void AddLabel(IEnumerable<IMJumpLabel> labels_to_add)
            {
                if (labels == null)
                {
                    labels = new List<IMJumpLabel>();
                }
                labels.AddRange(labels_to_add);
            }

            /// <summary>
            /// 
            /// </summary>
            public override string ToString()
            {
                string l = "";
                if (labels != null)
                {
                    foreach (var label in labels)
                    {
                        l += label.id + " ";
                    }
                }

                return $"{code.ToString().PadLeft(3, '0')}{(string.IsNullOrWhiteSpace(l) ? "" : $" [{l.Trim()}]")}";
            }

            internal void UpdateOpCode(byte new_code)
            {
                code = new_code;
            }

            internal bool HasLabel(JumpLabelType labeltype)
            {
                if (labels == null)
                {
                    return false;
                }
                return labels.Any(x => x.type == labeltype);
            }

            internal bool IsSkipped => labels != null && labels.Any(x => x.type == JumpLabelType.Skip);

            internal int LabelCount()
            {
                if (labels == null)
                {
                    return 0;
                }
                return labels.Count;
            }
            internal int LabelCount(JumpLabelType labeltype)
            {
                if (labels == null)
                {
                    return 0;
                }
                return labels.Count(x => x.type == labeltype);
            }
        }

        /// <summary>
        /// list of intermediate bytecode
        /// </summary>
        public class IMByteCodeList
        {
            /// <summary>
            /// segments, used when compiling multithreaded;
            /// the compiler will build a tree of codelists with segments 
            /// after that following stages might run using multithreading on the segments 
            /// </summary>
            internal List<IMByteCodeList> segments { get; set; }
            internal int SegmentCount => segments == null ? 0 : segments.Count;
            internal bool IsSegmented => segments == null ? false : segments.Count > 0;
            internal bool IsZeroTerminated
            {
                get
                {
                    if (list == null || list.Count < 1) return false;
                    return list[list.Count - 1].code == 0;
                }
            }

            internal List<IMByteCode> list { get; set; }
            internal IMByteCodeList() { list = new List<IMByteCode>(); segments = null; }


            /// <summary>
            /// add code to list
            /// </summary>
            /// <param name="code">the opcode byte</param>
            /// <param name="label">optional jump label</param>
            /// <returns>returns index of opcode</returns>
            internal int Add(byte code, IMJumpLabel label = null)
            {
                if (list == null) list = new List<IMByteCode>(); /// dont check each add it should be impossible to be null
                list.Add(new IMByteCode(code, label));
                return list.Count - 1;
            }


            /// <summary>
            /// add op to code list
            /// </summary>
            /// <param name="op"></param>
            /// <param name="label">optional jump label</param>
            /// <returns>the index of the op</returns>
            internal int Add(blast_operation op, IMJumpLabel label = null)
            {
                return Add((byte)op, label);
            }

            internal byte[] ToBytes()
            {
                if (list == null)
                {
                    return new byte[0];
                }

                byte[] bytes = new byte[list.Count];
                for (int i = 0; i < list.Count; i++)
                {
                    bytes[i] = list[i].code;
                }

                return bytes;
            }

            internal void AddRange(IMByteCodeList bcl)
            {
                if (bcl != null && bcl.Count > 0)
                {
                    AddRange(bcl.list);
                }
            }

            internal void AddRange(IEnumerable<byte> code)
            {
                if (code != null)
                {
                    if (list == null)
                    {
                        list = new List<IMByteCode>();
                    }
                    foreach (byte b in code)
                    {
                        list.Add(new IMByteCode(b));
                    }
                }
            }

            internal void AddRange(IEnumerable<IMByteCode> code)
            {
                if (code != null)
                {
                    if (list == null)
                    {
                        list = new List<IMByteCode>();
                    }
                    list.AddRange(code);
                }
            }

            internal void AddSegment(IMByteCodeList segment)
            {
                if (segment != null)
                {
                    if (segments == null)
                    {
                        lock (this)
                        {
                            if (segments == null)
                            {
                                segments = new List<IMByteCodeList>();
                            }
                        }
                    }
                    lock (segments)
                    {
                        segments.Add(segment);
                    }
                }
            }

            internal bool TryGetConstantReference(BlastVariable p_id, out IMJumpLabel label)
            {
                Assert.IsNotNull(p_id); 

                if (list != null || list.Count >= 0)
                {
                    string labelname = $"constant_{p_id.Id}";

                    for (int i = 0; i < list.Count; i++)
                    {
                        IMByteCode code = list[i];
                        if (code.labels != null && code.labels.Count > 0)
                        {
                            for (int j = 0; j < code.labels.Count; j++)
                            {
                                label = code.labels[j];

                                if (label.is_constant)
                                {
                                    if (string.Compare(labelname, label.id, true) == 0)
                                    {
                                        return true;
                                    }
                                    else
                                    {
                                        // only 1 constant def per code byte is possible so skip any other label after the first 
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
                label = null;
                return false;                         
            }

            /// <summary>
            /// remove code element at index
            /// </summary>
            /// <param name="i"></param>
            internal void RemoveAt(int i)
            {
                this.list.RemoveAt(i);
            }

            /// <summary>
            /// add all segments to this list clearing the segment list afterwards 
            /// </summary>
            internal void ReduceSegments()
            {
                if (IsSegmented)
                {
                    foreach (IMByteCodeList segment in segments)
                    {
                        this.AddRange(segment);
                    }
                    segments.Clear();
                    segments = null;
                }
            }

            internal void Insert(int index, byte code)
            {
                if (index > list.Count || index < 0)
                {
                    throw new IndexOutOfRangeException("cannot insert at negative index or more then 1 beyond size of list");
                }
                if (list == null)
                {
                    list = new List<IMByteCode>();
                }
                if (index == list.Count)
                {
                    list.Add(new IMByteCode(code));
                }
                else
                {
                    list.Insert(index, new IMByteCode(code));
                }
            }

            /// <summary>
            /// get opcode at index
            /// </summary>
            public IMByteCode this[int index]
            {
                get
                {
                    return list != null ? list[index] : null;
                }
                set
                {
                    if (list != null) list[index] = value;
                }
            }

            /// <summary>
            /// get count of opcodes
            /// </summary>
            public int Count => list != null ? list.Count : 0;

            internal bool RemoveAtAndMoveJumpLabels(IBlastCompilationData data, int index)
            {
                if (index < 0 || index >= Count)
                {
                    return false;
                }
                if (list == null)
                {
                    // it is removed... and considering the list was empty we also wont have to move labels
                    return true;
                }

                IMByteCode bc = list[index];

                if (bc.LabelCount() > 0)
                {
                    // move all to the next if it was not the last item 
                    if (index < list.Count - 1)
                    {
                        list.RemoveAt(index);
                        list[index].AddLabel(bc.labels);
                    }
                    else
                    {
                        // if on last (wierd situation)
                        // - set nop, leave labels, log trace ??
                        list[index].UpdateOpCode(0);
                        if (data != null)
                        {
                            data.LogWarning("IMByteCodeList.RemoveAtAndMoveJumpLabels: attempted to remove last code op, it however is marked as a jump label and being the last operation we cant move the jumplabels forward; inserting nop");
                        }
                    }
                }
                else
                {
                    // no label can just remove at index 
                    list.RemoveAt(index);
                }
                return true;
            }
        }
    }

    /// <summary>
    /// intermediate bytecode data for use by compiler 
    /// </summary>
    unsafe public struct BlastIntermediate
    {
        const byte opt_id = (byte)blast_operation.id;
        const byte opt_value = (byte)blast_operation.pi;

        /// <summary>
        /// nr of datapoints
        /// </summary>
        public const int data_capacity = 128; // in elements

        /// <summary>
        /// max size of bytecode in bytes
        /// </summary>
        public const int code_capacity = 64 * 1024; // in bytes 

        /// <summary>
        /// 
        /// </summary>
        public const int data_element_bytesize = 4; 


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

        
        /// <summary>
        ///  max 16n vectors up until (float4x4)   datasize = lower 4 bits + 1
        ///  - otherwise its a pointer (FUTURE)      
        ///  - datatype                              datatype = high 4 bits >> 4
        /// </summary>
        public fixed byte metadata[data_capacity];


        /// <summary>
        /// nr of data elements (presumably 32bits so 4bytes/element) - same as data_offset, added for clarity
        /// </summary>
        public byte DataCount => data_count;

        /// <summary>
        /// data byte size 
        /// </summary>
        public int DataByteSize => data_count * data_element_bytesize;

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
        /// validate intermediate package -> ssmd packaging mode 
        /// </summary>
        public int ValidateSSMD(in IntPtr blast)
        {
            return Validate(blast, true); 
        }

        /// <summary>
        /// validate intermediate package -> normal packaging mode 
        /// </summary>
        public int Validate(in IntPtr blast, bool is_ssmd_packaged = false)
        {
            return Execute(in blast, true, is_ssmd_packaged);
        }

        /// <summary>
        /// execute the intermediate for validation and stack info 
        /// </summary>
        public int Execute(in IntPtr blast, bool validation_run = false, bool is_ssmd_packaged = false)
        {
            // setup a package to run 
            BlastPackageData package = default;
            
            package.PackageMode = BlastPackageMode.Compiler;
            package.LanguageVersion = is_ssmd_packaged ? BlastLanguageVersion.BSSMD1 : BlastLanguageVersion.BS1;
            package.Flags = BlastPackageFlags.None;
            package.Allocator = (byte)Allocator.None; 
            
            package.O1 = (ushort)code_size;
            package.O2 = (ushort)data_capacity; // 1 byte of metadata for each capacity 
            package.O3 = (ushort)(data_count * 4);  // 4 bytes / dataelement 
            package.O4 = (ushort)(data_capacity * 4); // capacity is in byte count on intermediate 

            int initial_stack_offset = package.DataSegmentStackOffset / 4;

            fixed (byte* pcode = code)
            fixed (float* pdata = data)
            fixed (byte* pmetadata = metadata)
            {
                // estimate stack size while at it: set stack memory too all INF's, 
                // this assumes the intermediate has more then enough stack
                float* stack = &pdata[initial_stack_offset];
                for (int i = 0; i < package.StackCapacity; i++)
                {
                    stack[i] = math.INFINITY;
                }

                if (!is_ssmd_packaged)
                {
                    // Normal Packaging mode: run the interpretation 

                    // set package 
                    Blast.blaster.SetPackage(package, pcode, pdata, pmetadata, initial_stack_offset);
                    Blast.blaster.SetValidationMode(validation_run);

                    // run it 
                    int exitcode = Blast.blaster.Execute(blast);
                    if (exitcode != (int)BlastError.success) return exitcode;
                }
                else
                {
                    // ssmd packaging mode, run ssmd interpretor on 1 record
                    // - make sure stack is packaged, otherwise stack estimation cannot check stack use

                    Blast.ssmd_blaster.SetPackage(package, pcode, pmetadata);
                    Blast.ssmd_blaster.ValidateOnce = validation_run; // it should be set by setting a compiler package but set anyway to make it clear to any reader

                    BlastSSMDDataStack* p_ssmd_data = (BlastSSMDDataStack*)(void*)pdata;

                    int exitcode = Blast.ssmd_blaster.Execute(blast, IntPtr.Zero, p_ssmd_data, 1, false);

                    if (exitcode != (int)BlastError.success) return exitcode;
                }

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