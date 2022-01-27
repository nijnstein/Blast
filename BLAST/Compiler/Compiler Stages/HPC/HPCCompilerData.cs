namespace NSS.Blast.Compiler
{
    /// <summary>
    /// compiler data for the hpc compiler 
    /// </summary>
    public class HPCCompilationData : CompilationData
    {
        /// <summary>
        /// resulting burstable C# code
        /// </summary>
        public string HPCCode;

        /// <summary>
        /// setup compilation data for the HPC compiler chain
        /// </summary>
        /// <param name="blast">blast</param>
        /// <param name="script">the blast script to compile</param>
        /// <param name="options">blast compiler options</param>
        public HPCCompilationData(BlastEngineDataPtr blast, BlastScript script, BlastCompilerOptions options) : base(blast, script, options)
        {

        }
    }

}