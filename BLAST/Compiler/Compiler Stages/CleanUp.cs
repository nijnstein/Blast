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
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member 'BlastPreCompileCleanup.Version'
        public System.Version Version => new System.Version(0, 1, 0);
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member 'BlastPreCompileCleanup.Version'
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member 'BlastPreCompileCleanup.StageType'
        public BlastCompilerStageType StageType => BlastCompilerStageType.Cleanup;
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member 'BlastPreCompileCleanup.StageType'

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

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member 'BlastPreCompileCleanup.Execute(IBlastCompilationData)'
        public int Execute(IBlastCompilationData data)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member 'BlastPreCompileCleanup.Execute(IBlastCompilationData)'
        {
            remove_dereferenced_parameters(data, data.AST);
            return (int)(data.IsOK ? BlastError.success : BlastError.error);
        }
    }
}