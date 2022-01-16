

using System;
using System.Collections.Generic;
using System.Linq;
#if !NOT_USING_UNITY
using System.Runtime.InteropServices.WindowsRuntime;
#endif
using System.Threading;
using UnityEngine;

namespace NSS.Blast
{

    /// <summary>
    /// the different compiler outputs / language targets 
    /// </summary>
    public enum BlastLanguageVersion : int
    {
        None = 0,

        /// <summary>
        /// BLAST Script, default script / bytecode 
        /// </summary>
        BS1 = 1,

        /// <summary>
        /// BLAST Single Script Multiple Data
        /// </summary>
        BSSMD1 = 2,

        /// <summary>
        /// Burstable C# code packed in functions for burst to compile at designtime
        /// </summary>
        HPC = 3
    }

    /// <summary>
    /// A BLAST Script
    /// </summary>
    public class BlastScript
    {
        public BlastLanguageVersion LanguageVersion { get; internal set; } = BlastLanguageVersion.BS1; 
        public int Id { get; internal set; }
        public string Name { get; internal set; }
        public string Code { get; internal set; }

 
        /// <summary>
        /// create script from code 
        /// </summary>
        /// <param name="code">the code</param>
        /// <param name="name">name for the script</param>
        /// <param name="id">a unique id</param>
        /// <returns>a blast script</returns>
        public static BlastScript FromText(string code, string name = null, int id = 0)
        {
            return new BlastScript()
            {
                LanguageVersion = BlastLanguageVersion.BS1, 
                Id = id,
                Name = string.IsNullOrWhiteSpace(name) ? string.Empty : name,
                Code = code
            };
        }

#if !NOT_USING_UNITY
        /// <summary>
        /// load a script from resources
        /// </summary>
        /// <param name="resource_file_without_extension">the resource name</param>
        /// <returns>a blast script</returns>
        public static BlastScript FromResource(string resource_file_without_extension)
        {
            TextAsset scriptcode_asset = Resources.Load<TextAsset>(resource_file_without_extension);
            if (scriptcode_asset == null)
            {
                Debug.LogError($"BlastScript.FromResource: could not load resource file: '{resource_file_without_extension}\n\n'");
                return null;
            }
            else
            {
                return BlastScript.FromText(scriptcode_asset.text, resource_file_without_extension);
            }
        }
#endif

        /// <summary>
        /// get the string representation of a blast script as ID:Name
        /// </summary>
        public override string ToString()
        {
            return $"script: id: {Id}, name: {Name}";
        }

    }

}
