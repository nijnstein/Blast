//##########################################################################################################
// Copyright © 2022 Rob Lemmens | NijnStein Software <rob.lemmens.s31@gmail.com> All Rights Reserved       #
// Unauthorized copying of this file, via any medium is strictly prohibited                                #
// Proprietary and confidential                                                                            #
//##########################################################################################################
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
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member 'BlastHPCStackResolver.Version'
        public Version Version => new Version(0, 1, 0);
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member 'BlastHPCStackResolver.Version'

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member 'BlastHPCStackResolver.StageType'
        public BlastCompilerStageType StageType => BlastCompilerStageType.StackResolver; 
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member 'BlastHPCStackResolver.StageType'

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member 'BlastHPCStackResolver.Execute(IBlastCompilationData)'
        public int Execute(IBlastCompilationData data)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member 'BlastHPCStackResolver.Execute(IBlastCompilationData)'
        {
            // find out max depth of stack 
            return 0; 

        }
    }
}
