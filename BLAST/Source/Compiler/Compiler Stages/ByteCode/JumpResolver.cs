//##########################################################################################################
// Copyright © 2022 Rob Lemmens | NijnStein Software <rob.lemmens.s31@gmail.com> All Rights Reserved       #
// Unauthorized copying of this file, via any medium is strictly prohibited                                #
// Proprietary and confidential                                                                            #
//##########################################################################################################
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
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member 'BlastJumpResolver.Version'
        public Version Version => new Version(0, 1, 0);
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member 'BlastJumpResolver.Version'
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member 'BlastJumpResolver.StageType'
        public BlastCompilerStageType StageType => BlastCompilerStageType.JumpResolver;
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member 'BlastJumpResolver.StageType'


        /// <summary>
        /// Resolve all jumps
        /// </summary>
        /// <param name="cdata">compilation data</param>
        /// <param name="code">intermediate bytecode</param>
        /// <returns>true on success</returns>
        static bool ResolveJumps(CompilationData cdata, IMByteCodeList code)
        {
            // move each jump label to the next non-nop operation and remove the nop anchor
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
                        if (label.is_label)
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
            foreach (Tuple<string, int> offset in offset_idx)
            {
                int idx;

                if (!label_idx.TryGetValue(offset.Item1, out idx))
                {
                    cdata.LogError($"Compiler Failure: failed to resolve jumps, could not find label for offset id: {offset.Item1}");
                    return false;
                }

                // calculate and verify jump size 
                int jump_size = idx - offset.Item2;
                int abs_jump_size = Math.Abs(jump_size);

                if (abs_jump_size > 255)
                {
                    cdata.LogToDo($"Compiler Feature: jump repeater for long jumps, if jumpsize > 255 inject new jumps at 255 until lenght reached");
                    cdata.LogError($"Compiler Failure: failed to resolve jumps, jump {offset.Item1} larger then max jump size of 255 bytes, size: {jump_size}");
                    return false;
                }
                if (jump_size == 0 || abs_jump_size == 1)
                {
                    // this can only be wrong
                    cdata.LogError($"Compiler Failure: failed to resolve jumps, jump {offset.Item1} has size 1 or 2 which cannot be correct");
                    return false;
                }

                // get jump operation (should be before the offset)
                blast_operation op = (blast_operation)code[(byte)(offset.Item2 - 1)].code;
                if (jump_size > 0)
                {
                    // verify jump is a JZ, JNZ or JUMP 
                    if (!(op == blast_operation.jump || op == blast_operation.jnz || op == blast_operation.jz))
                    {
                        cdata.LogError($"Compiler Failure: encountered invalid jumptype <{op}> for forward jump of size {jump_size}");
                        return false;
                    }
                    
                    // update jump offset
                    code[offset.Item2].UpdateOpCode((byte)(jump_size));
                }
                else
                {
                    // verify jump is a JUMPBACK. it is the only instruction that can jump backward 
                    if (op != blast_operation.jump_back)
                    {
                        cdata.LogError($"Compiler Failure: encountered invalid operation <{op}> for backward jump of size {jump_size}");
                        return false;
                    }

                    // update jump offset (negative!!!)
                    code[offset.Item2].UpdateOpCode((byte)(abs_jump_size));

                    // if we updated the last item in the list to a jump offset
                    if(offset.Item2 == code.Count - 1)
                    {
                        // list does not end with nop anymore
                        // - optimizer removed the double nop neglecting labels?
                    }
                }
            }

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
