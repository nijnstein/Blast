namespace NSS.Blast
{
    /// <summary>
    /// any script derived from this will be compiled during compilation/design 
    /// 
    /// public class BS1_compiletime : CompileTimeBlastScript
    /// {
    ///        public override string Code => @"a = a + 1;";
    /// }
    /// 
    /// </summary>
    public abstract class CompileTimeBlastScript : BlastScript
    {
    }

}
