//##########################################################################################################
// Copyright © 2022 Rob Lemmens | NijnStein Software <rob.lemmens.s31@gmail.com> All Rights Reserved  ^__^\#
// Unauthorized copying of this file, via any medium is strictly prohibited                           (oo)\#
// Proprietary and confidential                                                                       (__) #
//##########################################################################################################
#if NET_4_6 && (UNITY_STANDALONE_VSBUILD_WIN || UNITY_EDITOR_WIN)
#define CSC
#endif
#if STANDALONE_VSBUILD
    using NSS.Blast.Standalone; 
#else
    using UnityEngine;    
#endif 
using Microsoft.CSharp;
using NSS.Blast.Compiler.Stage;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;


namespace NSS.Blast.Compiler.Stage
{
    // for all unity defines see:
    // https://docs.unity3d.com/Manual/PlatformDependentCompilation.html

#if CSC
    internal static class CSC
    {
        internal static void test()
        {
            var csc = new CSharpCodeProvider(new Dictionary<string, string>() { { "CompilerVersion", "v3.5" } });
            var parameters = new CompilerParameters(new[] { "mscorlib.dll", "System.Core.dll" }, "foo.exe", true);
            parameters.GenerateExecutable = true;
            CompilerResults results = csc.CompileAssemblyFromSource(parameters,
            @"using System.Linq;
            class Program {
              public static void Main(string[] args) {
                var q = from i in Enumerable.Range(1,100)
                          where i % 2 == 0
                          select i;
              }
            }");
            results.Errors.Cast<CompilerError>().ToList().ForEach(error =>
                Debug.LogError(error.ErrorText)
            );
        }
    }
#endif

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member 'BlastCSCompiler'
    public class BlastCSCompiler : IBlastCompilerStage
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member 'BlastCSCompiler'
    {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member 'BlastCSCompiler.Version'
        public Version Version => new Version(0, 1, 0);
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member 'BlastCSCompiler.Version'
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member 'BlastCSCompiler.StageType'
        public BlastCompilerStageType StageType => BlastCompilerStageType.Compile;
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member 'BlastCSCompiler.StageType'

#if CSC
        public int Compile(CSCompilationData data)
        {
            // take output from hpc and pack into program 
            string code = CSNamespace.MergeScriptIntoNamespace(data.HPCCode);

            // compile assembly from code
            // Assembly asm = CSC.Compile(code);


            return 0; 
        }
#endif


        /// <summary>
        /// compile script using the cs compiler supplied with .net
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public int Execute(IBlastCompilationData data)
        {
#if CSC
            CSCompilationData cdata = (CSCompilationData)data;
            return Compile(cdata);
#else
            return (int)BlastError.error_cs_compiler_not_supported_on_platform;
#endif
        }

        internal static void Test()
        {
#if CSC
            CSC.test();
#endif
        }
        
    }

}