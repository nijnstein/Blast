//##########################################################################################################
// Copyright © 2022 Rob Lemmens | NijnStein Software <rob.lemmens.s31@gmail.com> All Rights Reserved  ^__^\#
// Unauthorized copying of this file, via any medium is strictly prohibited                           (oo)\#
// Proprietary and confidential                                                                       (__) #
//##########################################################################################################
#if STANDALONE_VSBUILD
    using NSS.Blast.Standalone;
    using Unity.Assertions; 
#else
using UnityEngine;
#endif


using System.Collections.Generic;
using System.Linq;

namespace NSS.Blast.Register
{
    /// <summary>
    /// Here we maintain all scripts registered, either by manually calling Register(script)
    /// or by finding it in the binary through reflection. 
    /// 
    /// -> the registry is a static singleton
    /// -> this registry only stores script, not package data
    /// -> access is threadsafe                                 s
    /// 
    /// </summary>
#if UNITY_EDITOR
    [UnityEditor.InitializeOnLoad]
#endif
    static public partial class BlastScriptRegistry
    {
        static private object locker;
        static Dictionary<int, BlastScript> scripts;
        static Dictionary<string, int> names; 

        static void EnsureInitialized()
        {
            if (locker == null) locker = new object();
            if (scripts == null) scripts = new Dictionary<int, BlastScript>();
            if (names == null) names = new Dictionary<string, int>();
        }

        static BlastScriptRegistry()
        {
            EnsureInitialized(); 

            List<CompileTimeBlastScript> cts = BlastReflect.FindCompileTimeBlastScripts();
            if (cts != null)
            {
                foreach (CompileTimeBlastScript bs in cts)
                {
                    BlastScriptRegistry.Register(bs);
                }
            }
        }

        #region Register

        
#pragma warning disable CS1570 // XML comment has badly formed XML -- 'An identifier was expected.'
#pragma warning disable CS1570 // XML comment has badly formed XML -- '1'
/// <summary>
        /// Register a given script with the registry. If the given script id < 1 then a new id is generated that is 1 higher then the max known id. 
        /// If the name already exists then the given scripts name is appended '_1'. 
        /// </summary>
        /// <param name="script">a reference to the script to register</param>
        /// <returns>the script id</returns>
        static public int Register(BlastScript script)
#pragma warning restore CS1570 // XML comment has badly formed XML -- '1'
#pragma warning restore CS1570 // XML comment has badly formed XML -- 'An identifier was expected.'
        {
            if (script == null)
            {
                return -1;
            }
            
            EnsureInitialized();

            lock (locker)
            {
                if (script.Id < 1)
                {
                    script.Id = !scripts.Any() ? 1 : scripts.Max(x => x.Key) + 1;
                }
                if (string.IsNullOrWhiteSpace(script.Name))
                {
                    script.Name = script.GetType().Name.ToString();
                }
                scripts.Add(script.Id, script);

                string name = script.Name;
                int n = 0;
                while (names.ContainsKey(script.Name))
                {
                    n++;
                    script.Name = name + $"_{n}"; 
                }

                names.Add(script.Name, script.Id);

#if UNITY_EDITOR
                string smsg = $"BLAST Registry: {script.Id} {script.Name}";
                Debug.Log(smsg);
#if NSSCONSOLE
                if(NSS.Console.ConsoleLog.Instance != null)
                {
                    NSS.Console.ConsoleLog.Instance.LogMessage(smsg); 
                }
#endif
#endif
            }
            return script.Id;
        }


        /// <summary>
        /// register code with given name and id with registry
        /// </summary>
        /// <param name="code">the script code to register</param>
        /// <param name="name">the name to attach, should be unique and is case insensitive</param>
        /// <param name="id">the script id to use, 0 to generate one, should be > 0 and unique</param>
        /// <returns>the script id</returns>
        static public int Register(string code, string name = null, int id = 0)
        {
            return Register(BlastScript.FromText(code, name, id));
        }

#if !STANDALONE_VSBUILD

        /// <summary>
        /// Register a resource file as a blast script with the registry
        /// </summary>
        /// <param name="resource_filename_without_extension">the resource name</param>
        /// <param name="name">the unique case insensive script name to use</param>
        /// <param name="id">the unique id to assign or 0 to generate one</param>
        /// <returns>the script id</returns>
        static public int RegisterScriptResource(string resource_filename_without_extension, string name = null, int id = 0)
        {
            BlastScript script = BlastScript.FromResource(resource_filename_without_extension);
            script.Id = id;
            script.Name = name; 
            if (script != null)
            {
                return Register(script);
            }
            else
            {
                return -1;                  
            }
        }
#endif


        #endregion

        #region Registration Information 

        /// <summary>
        /// Enumerate all registred blast scripts 
        /// </summary>
        /// <returns>a blastscript ienumerator</returns>
        static public IEnumerable<BlastScript> All()
        {
            if (scripts != null)
            {
                foreach (KeyValuePair<int, BlastScript> k in scripts)
                {
                    yield return k.Value;
                }
            }
            yield break; 
        }

        /// <summary>
        /// lookup a script with a given script id 
        /// </summary>
        /// <param name="id">integer id, must be positive</param>
        /// <returns>a script, or null if not found</returns>
        static public BlastScript Get(int id)
        {
            BlastScript bs;
            if (scripts != null)
            {
                if (id >= 0 && scripts.TryGetValue(id, out bs))
                {
                    return bs;
                }
            }
            return null;
        }

        /// <summary>
        /// perform a linear scan for a script with the given name and return iot if found. Case Insensitive
        /// </summary>
        /// <param name="name">the name to lookup</param>
        /// <returns>the script if found or null if nothing was found with the given name</returns>
        static public BlastScript Get(string name)
        {
            BlastScript bs = null; 
            if(scripts != null && !string.IsNullOrWhiteSpace(name))
            {
                KeyValuePair<int, BlastScript> kvp = scripts.FirstOrDefault(x => string.Compare(name, x.Value.Name, true) == 0);
                if(kvp.Key >= 0 && kvp.Value != null)
                {
                    bs = kvp.Value;                
                }
            }
            return bs; 
        }

        /// <summary>
        /// scan the registry for a script with the given name
        /// </summary>
        /// <param name="name">the case insensitive name to search for</param>
        /// <returns>true if found, false otherwise</returns>
        static public bool Exists(string name)
        {
            if (scripts != null && !string.IsNullOrWhiteSpace(name))
            {
                KeyValuePair<int, BlastScript> kvp = scripts.FirstOrDefault(x => string.Compare(name, x.Value.Name, true) == 0);
                if (kvp.Key >= 0 && kvp.Value != null)
                {
                    return true; 
                }
            }
            return false; 
        }

        /// <summary>
        /// lookup the id and check if it exists
        /// </summary>
        /// <param name="id">the unique scriptid to search for</param>
        /// <returns>true if found, false otherwise</returns>
        static public bool Exists(int id)
        {
            if (scripts != null)
            {
                return scripts.ContainsKey(id); 
            }
            return false; 
        }

        /// <summary>
        /// try to find the id of a script with a given name, performs a linear search
        /// </summary>
        /// <param name="name">the case insensitive name to lookup</param>
        /// <param name="reference_id">the script id</param>
        /// <returns>true if a script with the given name was found</returns>
        static public bool TryGetReferenceId(string name, out int reference_id)
        {
            if(scripts != null)
            {
                if (scripts != null && !string.IsNullOrWhiteSpace(name))
                {
                    KeyValuePair<int, BlastScript> kvp = scripts.FirstOrDefault(x => string.Compare(name, x.Value.Name, true) == 0);
                    if (kvp.Key >= 0 && kvp.Value != null)
                    {
                        reference_id = kvp.Key;
                        return true; 
                    }
                }
            }
            reference_id = 0; 
            return false;
        }

#endregion
    }

}