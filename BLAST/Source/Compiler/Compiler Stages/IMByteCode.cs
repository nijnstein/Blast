//##########################################################################################################
// Copyright © 2022 Rob Lemmens | NijnStein Software <rob.lemmens.s31@gmail.com> All Rights Reserved  ^__^\#
// Unauthorized copying of this file, via any medium is strictly prohibited                           (oo)\#
// Proprietary and confidential                                                                       (__) #
//##########################################################################################################
using System;
using System.Collections.Generic;
using System.Linq;

namespace NSS.Blast.Compiler.Stage
{
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
            if(labels == null)
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
            if(code != null)
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
            if(index < 0 || index >= Count)
            {
                return false; 
            }
            if(list == null)
            {
                // it is removed... and considering the list was empty we also wont have to move labels
                return true;
            }

            IMByteCode bc = list[index]; 

            if(bc.LabelCount() > 0)
            {
                // move all to the next if it was not the last item 
                if(index < list.Count - 1)
                {
                    list.RemoveAt(index);
                    list[index].AddLabel(bc.labels); 
                }
                else
                {
                    // if on last (wierd situation)
                    // - set nop, leave labels, log trace ??
                    list[index].UpdateOpCode(0);
                    if(data != null)
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