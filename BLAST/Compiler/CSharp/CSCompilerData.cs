using System.Reflection;

namespace NSS.Blast.Compiler
{
    /// <summary>
    /// compiler data for the C# compiler 
    /// - as it uses the output from hpc we base data on its data
    /// </summary>
    public class CSCompilationData : HPCCompilationData
    {
        public Assembly Namespace;

        public CSCompilationData(Blast blast, BlastScript script, BlastCompilerOptions options)
            : base(blast, script, options)
        {

        }
    }

}