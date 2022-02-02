//##########################################################################################################
// Copyright © 2022 Rob Lemmens | NijnStein Software <rob.lemmens.s31@gmail.com> All Rights Reserved       #
// Unauthorized copying of this file, via any medium is strictly prohibited                                #
// Proprietary and confidential                                                                            #
//##########################################################################################################
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
