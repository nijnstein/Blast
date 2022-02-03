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
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member 'JumpLabelType'
    public enum JumpLabelType
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member 'JumpLabelType'
    {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member 'JumpLabelType.Offset'
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member 'JumpLabelType.Jump'
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member 'JumpLabelType.Label'
        Jump, Offset, Label
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member 'JumpLabelType.Label'
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member 'JumpLabelType.Jump'
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member 'JumpLabelType.Offset'
    }

    /// <summary>
    /// a jump label, for help keeping track of jump targets 
    /// </summary>
    public class IMJumpLabel
    {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member 'IMJumpLabel.id'
        public string id { get; protected set; }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member 'IMJumpLabel.id'
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member 'IMJumpLabel.type'
        public JumpLabelType type { get; protected set; }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member 'IMJumpLabel.type'
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member 'IMJumpLabel.is_label'
        public bool is_label => type == JumpLabelType.Label;
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member 'IMJumpLabel.is_label'
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member 'IMJumpLabel.is_offset'
        public bool is_offset => type == JumpLabelType.Offset;
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member 'IMJumpLabel.is_offset'
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member 'IMJumpLabel.is_jump'
        public bool is_jump => type == JumpLabelType.Jump;
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member 'IMJumpLabel.is_jump'

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member 'IMJumpLabel.IMJumpLabel(string, JumpLabelType)'
        public IMJumpLabel(string _id, JumpLabelType _type) 
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member 'IMJumpLabel.IMJumpLabel(string, JumpLabelType)'
        { 
            id = _id;
            type = _type; 
        }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member 'IMJumpLabel.Jump(string)'
        static public IMJumpLabel Jump(string _id)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member 'IMJumpLabel.Jump(string)'
        {
            return new IMJumpLabel(_id, JumpLabelType.Jump);
        }
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member 'IMJumpLabel.Label(string)'
        static public IMJumpLabel Label(string _id)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member 'IMJumpLabel.Label(string)'
        {
            return new IMJumpLabel(_id, JumpLabelType.Label);
        }
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member 'IMJumpLabel.Offset(string)'
        static public IMJumpLabel Offset(string _id)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member 'IMJumpLabel.Offset(string)'
        {
            return new IMJumpLabel(_id, JumpLabelType.Offset);
        }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member 'IMJumpLabel.ToString()'
        public override string ToString()
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member 'IMJumpLabel.ToString()'
        {
            switch (type)
            {
                case JumpLabelType.Offset: return $"offset: {id}";
                case JumpLabelType.Jump: return $"jump to: {id}";

                default:
                case JumpLabelType.Label: return $"label: {id}";
            }
        }
    }

    /// <summary>
    /// intermediate bytecode - can contain additional data
    /// </summary>
    public class IMByteCode
    {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member 'IMByteCode.code'
        public byte code { get; protected set; }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member 'IMByteCode.code'
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member 'IMByteCode.op'
        public blast_operation op => (blast_operation)code;
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member 'IMByteCode.op'

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member 'IMByteCode.labels'
        public List<IMJumpLabel> labels { get; protected set; }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member 'IMByteCode.labels'

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member 'IMByteCode.IMByteCode(byte, IMJumpLabel)'
        public IMByteCode(byte _code, IMJumpLabel label = null)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member 'IMByteCode.IMByteCode(byte, IMJumpLabel)'
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

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member 'IMByteCode.AddLabel(IMJumpLabel)'
        public void AddLabel(IMJumpLabel label)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member 'IMByteCode.AddLabel(IMJumpLabel)'
        {
            if(labels == null)
            {
                labels = new List<IMJumpLabel>();                
            }
            labels.Add(label);
        }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member 'IMByteCode.AddLabel(IEnumerable<IMJumpLabel>)'
        public void AddLabel(IEnumerable<IMJumpLabel> labels_to_add)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member 'IMByteCode.AddLabel(IEnumerable<IMJumpLabel>)'
        {
            if (labels == null)
            {
                labels = new List<IMJumpLabel>();
            }
            labels.AddRange(labels_to_add);
        }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member 'IMByteCode.ToString()'
        public override string ToString()
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member 'IMByteCode.ToString()'
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

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member 'IMByteCode.UpdateOpCode(byte)'
        public void UpdateOpCode(byte new_code)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member 'IMByteCode.UpdateOpCode(byte)'
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
        public List<IMByteCodeList> segments { get; set; }
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member 'IMByteCodeList.SegmentCount'
        public int SegmentCount => segments == null ? 0 : segments.Count;
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member 'IMByteCodeList.SegmentCount'
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member 'IMByteCodeList.IsSegmented'
        public bool IsSegmented => segments == null ? false : segments.Count > 0; 
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member 'IMByteCodeList.IsSegmented'
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member 'IMByteCodeList.IsZeroTerminated'
        public bool IsZeroTerminated
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member 'IMByteCodeList.IsZeroTerminated'
        {
            get
            {
                if (list == null || list.Count < 1) return false;
                return list[list.Count - 1].code == 0; 
            }
        }
        
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member 'IMByteCodeList.list'
        public List<IMByteCode> list { get; set; }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member 'IMByteCodeList.list'
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member 'IMByteCodeList.IMByteCodeList()'
        public IMByteCodeList() { list = new List<IMByteCode>(); segments = null; }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member 'IMByteCodeList.IMByteCodeList()'


        /// <summary>
        /// add code to list
        /// </summary>
        /// <param name="code">the opcode byte</param>
        /// <param name="label">optional jump label</param>
        /// <returns>returns index of opcode</returns>
        public int Add(byte code, IMJumpLabel label = null)
        {
#pragma warning disable CS1587 // XML comment is not placed on a valid language element
            if (list == null) list = new List<IMByteCode>(); /// dont check each add it should be impossible to be null
            list
#pragma warning restore CS1587 // XML comment is not placed on a valid language element
.Add(new IMByteCode(code, label));
            return list.Count - 1;
        }

        /// <summary>
        /// add op to code list
        /// </summary>
        /// <param name="op"></param>
        /// <param name="label">optional jump label</param>
        /// <returns>the index of the op</returns>
        public int Add(blast_operation op, IMJumpLabel label = null)
        {
            return Add((byte)op, label);
        }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member 'IMByteCodeList.ToBytes()'
        public byte[] ToBytes()
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member 'IMByteCodeList.ToBytes()'
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

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member 'IMByteCodeList.AddRange(IMByteCodeList)'
        public void AddRange(IMByteCodeList bcl)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member 'IMByteCodeList.AddRange(IMByteCodeList)'
        {
            if (bcl != null && bcl.Count > 0)
            {
                AddRange(bcl.list);
            }
        }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member 'IMByteCodeList.AddRange(IEnumerable<byte>)'
        public void AddRange(IEnumerable<byte> code)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member 'IMByteCodeList.AddRange(IEnumerable<byte>)'
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
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member 'IMByteCodeList.AddRange(IEnumerable<IMByteCode>)'
        public void AddRange(IEnumerable<IMByteCode> code)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member 'IMByteCodeList.AddRange(IEnumerable<IMByteCode>)'
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

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member 'IMByteCodeList.AddSegment(IMByteCodeList)'
        public void AddSegment(IMByteCodeList segment)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member 'IMByteCodeList.AddSegment(IMByteCodeList)'
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
        public void ReduceSegments()
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

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member 'IMByteCodeList.Insert(int, byte)'
        public void Insert(int index, byte code)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member 'IMByteCodeList.Insert(int, byte)'
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

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member 'IMByteCodeList.this[int]'
        public IMByteCode this[int index]
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member 'IMByteCodeList.this[int]'
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

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member 'IMByteCodeList.Count'
        public int Count => list != null ? list.Count : 0;
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member 'IMByteCodeList.Count'

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