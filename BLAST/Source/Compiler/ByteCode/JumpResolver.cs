//############################################################################################################################
// BLAST v1.0.4c                                                                                                             #
// Copyright © 2022 Rob Lemmens | NijnStein Software <rob.lemmens.s31 gmail com> All Rights Reserved                   ^__^\ #
// Unauthorized copying of this file, via any medium is strictly prohibited proprietary and confidential               (oo)\ #
//                                                                                                                     (__)  #
//############################################################################################################################

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NSS.Blast.Compiler.Stage
{
    /// <summary>
    /// Resolve Jumps
    /// </summary>
    public class BlastJumpResolver : IBlastCompilerStage
    {
        /// <summary>
        /// initial versio 
        /// </summary>
        public Version Version => new Version(0, 1, 0);

        /// <summary>
        /// jump resolver stage, connect jumps with there labels  
        /// </summary>
        public BlastCompilerStageType StageType => BlastCompilerStageType.JumpResolver;


        /// <summary>
        /// Resolve all jumps
        /// </summary>
        /// <param name="cdata">compilation data</param>
        /// <param name="code">intermediate bytecode</param>
        /// <returns>true on success</returns>
        static bool ResolveJumps(CompilationData cdata, IMByteCodeList code)
        {
            // - labeling cleanup 
            // move each jump label to the next non-nop operation and remove the nop anchor
            // 
            int i = 0;
            while (i < code.Count)
            {
                // if a label on a nop instruction
                if (code[i].code == 0 && code[i].HasLabel(JumpLabelType.Label))
                {
                    // move to next non-nop instruction 
                    int j = i + 1;
                    while (j < code.Count)
                    {
                        if (code[j].code != 0)
                        {
                            foreach (IMJumpLabel label in code[i].labels)
                            {
                                if (label.is_label)
                                {
                                    code[j].AddLabel(label);
                                }
                            }
                            code[i].labels.Clear(); 
                            j = code.Count;
                        }
                        j++;
                    }

                    // if no labels left -> remove nop 
                    if (code[i].labels == null || code[i].labels.Count == 0)
                    {
                        code.list.RemoveAt(i);
                        continue; // skip i++, we removed an item from the list 
                    }
                }
                i++;
            }

            // foreach jump-offset label, find jump distance to its label
            // - get all label indices 
            Dictionary<string, int> label_idx = new Dictionary<string, int>();
            List<Tuple<string, int>> offset_idx = new List<Tuple<string, int>>();

            i = 0; 
            while (i < code.Count)
            {
                if (code[i].labels != null)
                {
                    foreach (IMJumpLabel label in code[i].labels)
                    {
                        if(string.IsNullOrWhiteSpace(label.id))
                        {
                            cdata.LogError($"Compiler Failure: jump label at token {i} has no identifier set"); 
                        }

                        // gather label indices 
                        if (label.is_label || label.is_constant)

                        {
                            if (label_idx.ContainsKey(label.id))
                            {
                                // double label id => compile error 
                                cdata.LogError($"Compiler Failure: failed to resolve jumps, found duplicate jump id: {label.id}");
                                return false;
                            }
                            else
                            {
                                label_idx.Add(label.id, i);
                            }
                        }

                        // gather jump offsets (may contain duplicate ids) 
                        if (label.is_offset)
                        {
                            offset_idx.Add(new Tuple<string, int>(label.id, i));
                        }
                    }
                }
                i++;
            }

            // some sanity checking: no offset may also be a label location -> warning
            foreach (KeyValuePair<string, int> label in label_idx)
            {
                foreach (Tuple<string, int> offset in offset_idx)
                {
                    if (label.Value == offset.Item2)
                    {
                        cdata.LogWarning($"Compiler Warning: jump offset targeted as jump label at {offset.Item2}, jump id: {label.Key}, offset id: {offset.Item1}");
                    }
                }
            }

            // foreach jump-offset find target label, calculate distance and update offset in codelist
            // - offset.item2 == index of jumpsource in code


            // jump_over table: <source_of_jump, jumpsize>
            List<Tuple<short, short, string>> jump_over = new List<Tuple<short, short, string>>(); 

            int additional_long_offset = 0;

            foreach (Tuple<string, int> offset in offset_idx)
            {
                int idx;

                if (!label_idx.TryGetValue(offset.Item1, out idx))
                {
                    cdata.LogError($"Compiler Failure: failed to resolve jumps, could not find label for offset id: {offset.Item1}");
                    return false;
                }

                // calculate and verify jump size
                short source_offset = (short)(offset.Item2 + additional_long_offset);
                short jump_size = (short)(idx - source_offset);
                short abs_jump_size = Math.Abs(jump_size);

                if (jump_size == 0 || abs_jump_size == 1)
                {
                    // this can only be wrong
                    cdata.LogError($"Compiler Failure: failed to resolve jumps, jump {offset.Item1} has size 1 or 2 which cannot be correct");
                    return false;
                }

                // get jump operation (should be before the offset)
                blast_operation op = (blast_operation)code[source_offset - 1].code;
                if (op == blast_operation.nop) continue; 

                if (jump_size > 0)
                {
                    // verify jump is a JZ, JNZ or JUMP 
                    //
                    // - forward jumps might mis-align when backward jump offsets increase in size 
                    //
                    // 

                    if (!(op == blast_operation.jump || op == blast_operation.jnz || op == blast_operation.jz || op == blast_operation.cjz))
                    {
                        cdata.LogError($"Compiler Failure: encountered invalid jumptype <{op}> for forward jump of size {jump_size}");
                        return false;
                    }

                    jump_over.Add(new Tuple<short, short, string>(source_offset, jump_size, offset.Item1));

                    // if the jump is too large or this is a constant jump condition 
                    if (jump_size > 255)
                    {
                        // update to a long jump with positive jumpsize 
                        switch (op)
                        {
                            case blast_operation.cjz:
                                op = blast_operation.cjz_long;
                                break;
                            case blast_operation.jz:
                                op = blast_operation.jz_long;
                                break;
                            case blast_operation.jnz:
                                op = blast_operation.jnz_long;
                                break;
                            case blast_operation.jump:
                                op = blast_operation.long_jump;
                                break;
                            default:
                                cdata.LogError($"Compiler Failure: failed to resolve jumps, jump {op} {offset.Item1} larger then max jump size of 255 bytes, size: {jump_size}");
                                return false;
                        }

                        code[source_offset - 1].UpdateOpCode((byte)op);
                        code[source_offset + 0].UpdateOpCode((byte)(jump_size >> 8));
                        code[source_offset + 1].UpdateOpCode((byte)(jump_size & 0b11111111));

                        // add offset index
                        // - at this point we could jump over an enlarged long_jump going backware (jump_back or constant_long_ref) 

                        // shift nxt offsets  
                        additional_long_offset++;
                    }
                    else
                    {
                        // update jump offset
                        code[source_offset].UpdateOpCode((byte)(jump_size));
                        code[source_offset+1].AddLabel(IMJumpLabel.Skip()); 
                    }
                }
                else
                {
                    // verify jump is a JUMPBACK or constant reference, these are the only possible backward jumps 
                    if (!(op == blast_operation.jump_back
                        ||
                        op == blast_operation.constant_long_ref
                        ||
                        op == blast_operation.constant_short_ref
                        ||
                        op == blast_operation.cdataref
                        ))
                    {
                        cdata.LogError($"Compiler Failure: encountered invalid operation <{op}> for backward jump of size {jump_size}");
                        return false;
                    }

                    // if we have a small jump and jumpsize is larger then that 
                    // - update to a long jump 
                    // - cdata is always assumed to be a long jump 
                    if (abs_jump_size > 255 || op == blast_operation.cdataref)
                    {
                        if (op == blast_operation.jump_back)
                        {
                            // update to a long jump with negative jumpsize 
                            code[source_offset - 1].UpdateOpCode((byte)blast_operation.long_jump);
                            code[source_offset + 0].UpdateOpCode((byte)(jump_size >> 8));
                            code[source_offset + 1].UpdateOpCode((byte)(jump_size & 0b11111111));

                            // shift nxt offsets  
                            additional_long_offset++;
                        }
                        if(op == blast_operation.cdataref)
                        {
                            // ok as is, dont need to update instruction 
                            code[source_offset + 0].UpdateOpCode((byte)(abs_jump_size >> 8));
                            code[source_offset + 1].UpdateOpCode((byte)(abs_jump_size & 0b11111111));
                        }
                        else
                        {
                            // == long constant reference
                            // encode long reference at code[offset.Item2] 
                            code[source_offset - 1].UpdateOpCode((byte)blast_operation.constant_long_ref);
                            code[source_offset + 0].UpdateOpCode((byte)(abs_jump_size >> 8));
                            code[source_offset + 1].UpdateOpCode((byte)(abs_jump_size & 0b11111111));
                        }
                    }
                    else
                    {
                        // update jump offset (negative!!!)
                        // - constant references ALWAYS jump back
                        code[source_offset].UpdateOpCode((byte)(abs_jump_size));
                        code[source_offset + 1].AddLabel(IMJumpLabel.Skip());
                    }
                }
            }

            // 
            // remove reserved offsets and update any jump target locations accordingly 
            // - note that jumps always get shorter in this operation voiding need to check for larger size
            // 
            i = 0;
            bool skipped;

            do
            {
                skipped = false; 

                while (i < code.Count)
                {
                    if (code[i].IsSkipped)
                    {
                        skipped = true; 

                        // any backward jump jumping over i after i shorten by 1
                        for (int j = i + 1; j < code.Count; j++)
                        {
                            // skip over anything not jumping
                            if (!(code[j].HasLabel(JumpLabelType.Jump) || code[j].HasLabel(JumpLabelType.ReferenceToConstant)))
                            {
                                continue;
                            }

                            blast_operation op = code[j].op;
                            short jumpsize = 0;
                            switch (op)
                            {
                                case blast_operation.cdataref:
                                case blast_operation.constant_long_ref:
                                    jumpsize = (short)((code[j + 1].code << 8) + code[j + 2].code);
                                    if (j - jumpsize > i) continue;
                                    jumpsize--;
                                    code[j + 1].UpdateOpCode((byte)(jumpsize >> 8));
                                    code[j + 2].UpdateOpCode((byte)(jumpsize & 0b11111111));
                                    break;

                                case blast_operation.long_jump:
                                    jumpsize = (short)((code[j + 1].code << 8) + code[j + 2].code);
                                    if (jumpsize < 0)
                                    {
                                        if (j + jumpsize > i) continue;

                                        jumpsize++; // shorter negative jump is enlarged towards zero
                                        code[j + 1].UpdateOpCode((byte)(jumpsize >> 8));
                                        code[j + 2].UpdateOpCode((byte)(jumpsize & 0b11111111));
                                    }
                                    break;

                                case blast_operation.constant_short_ref:
                                case blast_operation.jump_back:
                                    jumpsize = code[j + 1].code;
                                    if (j - jumpsize > i) continue;
                                    jumpsize--;
                                    code[j + 1].UpdateOpCode((byte)jumpsize);
                                    break;
                            }
                        }

                        // any forward jump jumping over i before i shorten by 1 
                        for (int j = 0; j < i; j++)
                        {
                            if (!(code[j].HasLabel(JumpLabelType.Jump) || code[j].HasLabel(JumpLabelType.ReferenceToConstant)))
                            {
                                continue;
                            }

                            blast_operation op = code[j].op;
                            short jumpsize = 0;
                            switch (op)
                            {
                                case blast_operation.jz:
                                case blast_operation.jump:
                                case blast_operation.jnz:
                                    jumpsize = code[j + 1].code;
                                    if (j + jumpsize >= i)
                                    {
                                        jumpsize--;
                                        code[j + 1].UpdateOpCode((byte)jumpsize);
                                    }
                                    break;

                                case blast_operation.jnz_long:
                                case blast_operation.jz_long:
                                case blast_operation.long_jump:
                                    jumpsize = (short)((code[j + 1].code << 8) + code[j + 2].code);
                                    if (jumpsize > 0)
                                    {
                                        if (j + jumpsize > i) continue;

                                        jumpsize--;
                                        code[j + 1].UpdateOpCode((byte)(jumpsize >> 8));
                                        code[j + 2].UpdateOpCode((byte)(jumpsize & 0b11111111));
                                    }
                                    break;

                            }
                        }

                        // remove the nop 
                        code.RemoveAt(i);
                        continue; // i++ would skip 1 opcode
                    }

                    i++;
                }

                // check for jumps that became smaller in the last step (longjump + 1) and changed jumptype
                i = 0;
                while (i < code.Count)
                {
                    // only jumps/offsets 
                    if (! (code[i].HasLabel(JumpLabelType.Jump) || code[i].HasLabel(JumpLabelType.ReferenceToConstant)))
                    {
                        i++;
                        continue;
                    }

                    blast_operation op = code[i].op;
                    switch(op)
                    {
                        // skip cdata, there is no short jump for cdata
                        case blast_operation.cdataref: 
                            break; 

                        case blast_operation.long_jump:
                        case blast_operation.constant_long_ref:
                        {
                                int jumpsize = (short)((code[i + 1].code << 8) + code[i + 2].code);
                                if (jumpsize > 0)
                                {
                                    if(jumpsize <= 255)
                                    {
                                        if (op == blast_operation.long_jump)
                                        {
                                            code[i + 1].UpdateOpCode((byte)blast_operation.jump);
                                        }
                                        else
                                        {
                                            code[i + 1].UpdateOpCode((byte)blast_operation.constant_short_ref);
                                        }
                                        code[i + 1].UpdateOpCode((byte)jumpsize);
                                        code[i + 2].UpdateOpCode((byte)0);
                                        code[i + 2].AddLabel(IMJumpLabel.Skip());
                                        skipped = true; // only applicable if entering with wierd state 
                                    }
                                }
                                else
                                {
                                    if (jumpsize < -255)
                                    {
                                        // backward jump
                                        code[i + 1].UpdateOpCode((byte)blast_operation.jump_back);
                                        code[i + 1].UpdateOpCode((byte)(-jumpsize));
                                        code[i + 2].UpdateOpCode((byte)0);
                                        code[i + 2].AddLabel(IMJumpLabel.Skip());
                                        skipped = true; 
                                    }
                                }

                            }
                            break; 

                        case blast_operation.jz_long:
                        case blast_operation.jnz_long:
                            {
                                int jumpsize = (short)((code[i + 1].code << 8) + code[i + 2].code);
                                if (jumpsize <= 255)
                                {
                                    switch (op)
                                    {
                                        case blast_operation.jz_long: code[i].UpdateOpCode((byte)blast_operation.jz); break;
                                        case blast_operation.jnz_long: code[i].UpdateOpCode((byte)blast_operation.jnz); break;
                                    }

                                    code[i + 1].UpdateOpCode((byte)jumpsize);
                                    code[i + 2].UpdateOpCode((byte)0);
                                    code[i + 2].AddLabel(IMJumpLabel.Skip());
                                    skipped = true;
                                }
                            }
                            break; 
                    }

                    i++;
                }
            
                // keep updateing jumps until nothing is skipped 
            }
            while (skipped == true);
            return true;
        }


        /// <summary>
        /// resolve all jumps - connect jump-offsets with jump labels
        /// </summary>
        public int Execute(IBlastCompilationData data)
        {
            if (!data.IsOK || data.AST == null) return (int)BlastError.error;

            // resolve all jumps
            // as long as we dont support goto this should be able to run in parallel on segments 
            CompilationData cdata = (CompilationData)data;

            if (cdata.code.IsSegmented)
            {
                if (cdata.CompilerOptions.ParallelCompilation)
                {
                    // run in parallel 
                    bool[] ires = new bool[cdata.code.SegmentCount];
                    Parallel.ForEach(
                        cdata.code.segments,
                        segment
                        =>
                        ires[cdata.code.segments.IndexOf(segment)] = ResolveJumps(cdata, segment));

                    bool any_false = false;
                    foreach (bool b in ires) if (b == false)
                        {
                            any_false = true;
                            break;
                        }

                    return any_false ? (int)BlastError.error : (int)BlastError.success;
                }
                else
                {
                    // run single threaded
                    foreach (IMByteCodeList segment in cdata.code.segments)
                    {
                        if (!ResolveJumps(cdata, segment))
                        {
                            data.LogError("JumpResolver: failed to resolve all jumps in code semgment.");
                            return (int)BlastError.error;
                        }
                    }
                }
            }
            else
            {
                if (!ResolveJumps(cdata, cdata.code))
                {
                    data.LogError("JumpResolver: failed to resolve all jumps.");
                    return (int)BlastError.error;
                }
            }


            return data.IsOK ? (int)BlastError.success : (int)BlastError.error;
        }
    }
}
