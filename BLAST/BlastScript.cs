

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
    /// A script, code with name and id 
    /// </summary>
    public class BlastScript
    {
        private bool changed = false;
        public bool HasChanged { get { return changed; } }

        public BlastLanguageVersion LanguageVersion = BlastLanguageVersion.BS1;
        public int Id;
        public string Name;

        private string _code;
        public virtual string Code { get { return _code; } }

 
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
                Id = id,
                Name = string.IsNullOrWhiteSpace(name) ? string.Empty : name,
                _code = code
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
                UnityEngine.Debug.LogError($"BlastScript.FromResource: could not load resource file: '{resource_file_without_extension}\n\n'");
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

        /// <summary>
        /// update script code and set the changed flag 
        /// </summary>
        public void UpdateCode(string code)
        {                               
            _code = code ?? string.Empty; // prevent nulls
            changed = true; 
        }
    }

}
