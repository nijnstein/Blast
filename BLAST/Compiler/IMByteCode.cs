using System;
using System.Collections.Generic;
using System.Linq;

namespace NSS.Blast.Compiler.Stage
{
    public enum JumpLabelType
    {
        Jump, Offset, Label
    }

    /// <summary>
    /// a jump label, for help keeping track of jump targets 
    /// </summary>
    public class IMJumpLabel
    {
        public string id { get; protected set; }
        public JumpLabelType type { get; protected set; }
        public bool is_label => type == JumpLabelType.Label;
        public bool is_offset => type == JumpLabelType.Offset;
        public bool is_jump => type == JumpLabelType.Jump;

        public IMJumpLabel(string _id, JumpLabelType _type) 
        { 
            id = _id;
            type = _type; 
        }

        static public IMJumpLabel Jump(string _id)
        {
            return new IMJumpLabel(_id, JumpLabelType.Jump);
        }
        static public IMJumpLabel Label(string _id)
        {
            return new IMJumpLabel(_id, JumpLabelType.Label);
        }
        static public IMJumpLabel Offset(string _id)
        {
            return new IMJumpLabel(_id, JumpLabelType.Offset);
        }

        public override string ToString()
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
        public byte code { get; protected set; }
        public script_op op => (script_op)code;

        public List<IMJumpLabel> labels { get; protected set; }

        public IMByteCode(byte _code, IMJumpLabel label = null)
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

        public void AddLabel(IMJumpLabel label)
        {
            if(labels == null)
            {
                labels = new List<IMJumpLabel>();                
            }
            labels.Add(label);
        }

        public void AddLabel(IEnumerable<IMJumpLabel> labels_to_add)
        {
            if (labels == null)
            {
                labels = new List<IMJumpLabel>();
            }
            labels.AddRange(labels_to_add);
        }

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

        public void UpdateOpCode(byte new_code)
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
        public int SegmentCount => segments == null ? 0 : segments.Count;
        public bool HasSegments => segments == null ? false : segments.Count > 0; 
        
        public List<IMByteCode> list { get; set; }
        public IMByteCodeList() { list = new List<IMByteCode>(); segments = null; }


        /// <summary>
        /// add code to list
        /// </summary>
        /// <param name="code">the opcode byte</param>
        /// <param name="label">optional jump label</param>
        /// <returns>returns index of opcode</returns>
        public int Add(byte code, IMJumpLabel label = null)
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
        public int Add(script_op op, IMJumpLabel label = null)
        {
            return Add((byte)op, label);
        }

        public byte[] ToBytes()
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

        public void AddRange(IMByteCodeList bcl)
        {
            if (bcl != null && bcl.Count > 0)
            {
                AddRange(bcl.list);
            }
        }

        public void AddRange(IEnumerable<byte> code)
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
        public void AddRange(IEnumerable<IMByteCode> code)
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

        public void AddSegment(IMByteCodeList segment)
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
            if (HasSegments)
            {
                foreach (IMByteCodeList segment in segments)
                {
                    this.AddRange(segment); 
                }
                segments.Clear();
                segments = null; 
            }
        }

        public void Insert(int index, byte code)
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