//##########################################################################################################
// Copyright © 2022 Rob Lemmens | NijnStein Software <rob.lemmens.s31@gmail.com> All Rights Reserved  ^__^\#
// Unauthorized copying of this file, via any medium is strictly prohibited                           (oo)\#
// Proprietary and confidential                                                                       (__) #
//##########################################################################################################
using System.Collections.Generic;
using System.Linq;

namespace NSS.Blast.Compiler.Stage
{

    /// <summary>
    /// Pre-Compile Cleanup
    /// - for now only cleans up parameter references 
    /// </summary>
    public class BlastPreCompileCleanup : IBlastCompilerStage
    {

        /// <summary>
        /// version 0.1
        /// </summary>
        public System.Version Version => new System.Version(0, 1, 0);

        /// <summary>
        /// cleanup stage -> mainly cleans up references 
        /// </summary>
        public BlastCompilerStageType StageType => BlastCompilerStageType.Cleanup;

        static void remove_dereferenced_parameters(IBlastCompilationData data, node root)
        {
            // remove all constants that have become de-referenced
            var vars = data.Variables.ToArray();
            foreach (var v in vars)
            {
                if (v.IsConstant && v.ReferenceCount <= 0)
                {
                    data.Variables.Remove(v);
                }
            }

            // move constants to end of list after parameters 
            List<BlastVariable> somewhat_sorted = new List<BlastVariable>(data.Variables.Count);
            somewhat_sorted.AddRange(data.Variables.Where(x => !x.IsConstant));
            somewhat_sorted.AddRange(data.Variables.Where(x => x.IsConstant));
            data.Variables = somewhat_sorted;

            // update id's 
            for (int i = 0; i < data.Variables.Count; i++)
            {
                data.Variables[i].Id = i;
            }
        }


        /// <summary>
        /// execute cleanup procedures 
        /// </summary>        
        public int Execute(IBlastCompilationData data)
        {
            remove_dereferenced_parameters(data, data.AST);
            return (int)(data.IsOK ? BlastError.success : BlastError.error);
        }
    }
}