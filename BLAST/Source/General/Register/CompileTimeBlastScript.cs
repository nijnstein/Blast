//##########################################################################################################
// Copyright © 2022 Rob Lemmens | NijnStein Software <rob.lemmens.s31@gmail.com> All Rights Reserved  ^__^\#
// Unauthorized copying of this file, via any medium is strictly prohibited                           (oo)\#
// Proprietary and confidential                                                                       (__) #
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
