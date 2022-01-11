using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NSS.Blast.Compiler.Stage
{
    /// <summary>
    /// Stack Resolver for HPC code
    /// - transmute push-pops to stack-var assignments
    /// - re-use variables as much as possible  
    /// </summary>
    public class BlastHPCStackResolver : IBlastCompilerStage
    {
        public Version Version => new Version(0, 1, 0);

        public BlastCompilerStageType StageType => BlastCompilerStageType.StackResolver; 

        public int Execute(IBlastCompilationData data)
        {
            // find out max depth of stack 
            return 0; 

        }
    }
}
