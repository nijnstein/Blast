namespace NSS.Blast.Compiler
{
    /// <summary>
    /// compiler data for the hpc compiler 
    /// </summary>
    public class HPCCompilationData : CompilationData
    {
        public string HPCCode;

        public HPCCompilationData(Blast blast, BlastScript script, BlastCompilerOptions options)
            : base(blast, script, options)
        {

        }
    }

}