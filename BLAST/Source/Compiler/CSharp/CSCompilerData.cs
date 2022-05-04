//############################################################################################################################
//                                                                                                                           #
//  ██████╗ ██╗      █████╗ ███████╗████████╗                           Copyright © 2022 Rob Lemmens | NijnStein Software    #
//  ██╔══██╗██║     ██╔══██╗██╔════╝╚══██╔══╝                                    <rob.lemmens.s31 gmail com>                 #
//  ██████╔╝██║     ███████║███████╗   ██║                                           All Rights Reserved                     #
//  ██╔══██╗██║     ██╔══██║╚════██║   ██║                                                                                   #
//  ██████╔╝███████╗██║  ██║███████║   ██║     V1.0.4e                                                                       #
//  ╚═════╝ ╚══════╝╚═╝  ╚═╝╚══════╝   ╚═╝                                                                                   #
//                                                                                                                           #
//############################################################################################################################
//                                                                                                                     (__)  #
//       Unauthorized copying of this file, via any medium is strictly prohibited proprietary and confidential         (oo)  #
//                                                                                                                     (__)  #
//############################################################################################################################
#pragma warning disable CS1591
#pragma warning disable CS0162

using System.Reflection;

namespace NSS.Blast.Compiler
{
    /// <summary>
    /// compiler data for the C# compiler 
    /// - as it uses the output from hpc we base data on its data
    /// </summary>
    public class CSCompilationData : HPCCompilationData
    {
        /// <summary>
        /// Target namespace for compiling scripts into
        /// </summary>
        public Assembly Namespace;


        /// <summary>
        /// Setup CS Compiler chain
        /// </summary>
        /// <param name="blast">blast engine data</param>
        /// <param name="script">script code</param>
        /// <param name="options">compiler options</param>
        public CSCompilationData(BlastEngineDataPtr blast, BlastScript script, BlastCompilerOptions options) : base(blast, script, options)
        {

        }
    }

}