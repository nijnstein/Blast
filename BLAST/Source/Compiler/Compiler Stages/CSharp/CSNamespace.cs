﻿//##########################################################################################################
// Copyright © 2022 Rob Lemmens | NijnStein Software <rob.lemmens.s31@gmail.com> All Rights Reserved  ^__^\#
// Unauthorized copying of this file, via any medium is strictly prohibited                           (oo)\#
// Proprietary and confidential                                                                       (__) #
//##########################################################################################################
using System;
using System.Collections.Generic;
using System.Text;

namespace NSS.Blast.Compiler
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member 'CSNamespace'
    public static class CSNamespace
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member 'CSNamespace'
    {
        static string[] usings = new string[]
        {
            "System",
            "Unity.Burst",
            "Unity.Collections",
            "Unity.Collections.LowLevel.Unsafe",
            "Unity.Jobs",
        };

        static string ns = @"/*Generated by Blast*/
{0}

namespace NSS.Blast.Cache.CS
{{
    {1}
}}";

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member 'CSNamespace.CombineScriptsIntoNamespace(IEnumerable<string>)'
        static public string CombineScriptsIntoNamespace(IEnumerable<string> scripts)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member 'CSNamespace.CombineScriptsIntoNamespace(IEnumerable<string>)'
        {
            StringBuilder sb_using = GetUsings();

            StringBuilder sb_code = new StringBuilder();
            foreach (string s in scripts)
            {
                sb_code.AppendLine("");
                sb_code.AppendLine(s);
            }

            return string.Format(ns, sb_using, sb_code.ToString());
        }

        private static StringBuilder GetUsings()
        {
            StringBuilder sb_using = new StringBuilder();
            foreach (string u in usings)
            {
                sb_using.AppendLine($"using {u};");
            }

            return sb_using;
        }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member 'CSNamespace.MergeScriptIntoNamespace(string)'
        static public string MergeScriptIntoNamespace(string script)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member 'CSNamespace.MergeScriptIntoNamespace(string)'
        {
            return string.Format(ns, GetUsings(), Environment.NewLine + script);
        }

    }
}