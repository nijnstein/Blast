using NSS.Blast.Compiler;

namespace NSS.Blast
{

    /// <summary>
    /// the types of compiler stages that run in sequence to produce the output
    /// </summary>
    public enum BlastCompilerStageType
    {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member 'BlastCompilerStageType.None'
        None,
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member 'BlastCompilerStageType.None'
        /// <summary>
        /// convert input script into a list of tokens 
        /// </summary>
        Tokenizer,
        /// <summary>
        /// parses the tokens into an ast-tree and identifies identifiers 
        /// </summary>
        Parser,
        /// <summary>
        /// transform constructs in the ast: switch -> ifthen, while,for, etc -> ifthen 
        /// making later stages having less to worry about 
        /// </summary>
        Transform,
        /// <summary>
        /// analyse parameter use
        /// - determine vectors 
        /// - enforce multiplication rules 
        /// </summary>
        ParameterAnalysis,
        /// <summary>
        /// analyze ast structure
        /// - basic removal of some useless structures
        /// - rules of multiplication
        /// </summary>
        Analysis,
        /// <summary>
        /// flatten execution path 
        /// </summary>
        Flatten,
        /// <summary>
        /// optimize ast structure
        /// - transform expensive constructs into less expensive ones
        /// - this should be done after flattening the tree, any optimization that reduces compounds should happen in analysis
        /// </summary>
        Optimization,
        /// <summary>
        /// pre compile cleanup 
        /// </summary>
        Cleanup,
        /// <summary>
        /// resolve stack operations into stack-variables (HPC/CS only)
        /// </summary>
        StackResolver, 
        /// <summary>
        /// a [bytecode/hpc/cs] compiler
        /// </summary>
        Compile,
        /// <summary>
        /// post-compile: bytecode optimizer
        /// </summary>
        BytecodeOptimizer,
        /// <summary>
        /// post-compile: resolve jumps 
        /// </summary>
        JumpResolver,
        /// <summary>
        /// post-compile: package result
        /// </summary>
        Packaging
    }

    /// <summary>
    /// a compiler stage - employs 1 step of the compilation process
    /// </summary>
    public interface IBlastCompilerStage
    {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member 'IBlastCompilerStage.Version'
        System.Version Version
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member 'IBlastCompilerStage.Version'
        {
            get;
        }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member 'IBlastCompilerStage.StageType'
        BlastCompilerStageType StageType
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member 'IBlastCompilerStage.StageType'
        {
            get;
        }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member 'IBlastCompilerStage.Execute(IBlastCompilationData)'
        int Execute(IBlastCompilationData data);
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member 'IBlastCompilerStage.Execute(IBlastCompilationData)'
    }
}
