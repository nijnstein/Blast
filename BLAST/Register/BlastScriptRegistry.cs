using System.Collections.Generic;
using System.Linq;

namespace NSS.Blast.Register
{
    /// <summary>
    /// Here we maintain all scripts registered, either by manually calling Register(script)
    /// or by finding it in the binary through reflection  
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

        static public int Register(BlastScript script)
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
                Standalone.Debug.Log(smsg);
                if(NSS.Console.ConsoleLog.Instance != null)
                {
                    NSS.Console.ConsoleLog.Instance.LogMessage(smsg); 
                }
#endif
            }
            return script.Id;
        }

        static public int Register(string code, string name = null, int id = 0)
        {
            return Register(BlastScript.FromText(code, name, id));
        }

#if !NOT_USING_UNITY
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

        static public bool Exists(int id)
        {
            if (scripts != null)
            {
                return scripts.ContainsKey(id); 
            }
            return false; 
        }

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