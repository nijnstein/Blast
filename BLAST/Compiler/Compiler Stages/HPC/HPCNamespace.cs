﻿using System.Collections.Generic;
using System.Text;

namespace NSS.Blast.Compiler
{
    public static class HPCNamespace
    {
        static string[] usings = new string[]
        {
            "System",
            "Unity.Burst",
            "Unity.Collections",
            "Unity.Collections.LowLevel.Unsafe",
            "Unity.Jobs",
        };
        static string ns = @"//***********************************************************************************************/
//* generated by <BLAST SCRIPT ENGINE> do not modify ********************************************/                 
//***********************************************************************************************/
{0}

namespace NSS.Blast.Cache
{{

    {1}

}}";

        static public string CombineScriptsIntoNamespace(IEnumerable<string> scripts)
        {
            StringBuilder sb_using = new StringBuilder();
            foreach (string u in usings)
            {
                sb_using.AppendLine($"using {u};");
            }

            StringBuilder sb_code = new StringBuilder();
            foreach (string s in scripts)
            {
                sb_code.AppendLine("");
                sb_code.AppendLine(s);
            }

            return string.Format(ns, sb_using, sb_code.ToString());
        }

    }
}