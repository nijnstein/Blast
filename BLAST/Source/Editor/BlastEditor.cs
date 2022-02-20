#if UNITY_EDITOR && BS2

using UnityEngine;
using UnityEditor;
using System.Collections;
using NSS.Blast.Register;
using System.Text;
using System.IO;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace NSS.Blast.Editor
{
    class BlastBuildProcessor : IPreprocessBuildWithReport
    {
        public int callbackOrder { get { return 0; } }
        public void OnPreprocessBuild(BuildReport report)
        {
            Debug.Log("MyCustomBuildProcessor.OnPreprocessBuild for target " + report.summary.platform + " at path " + report.summary.outputPath);
        }
    }


    class BlastConfigurationWindow : EditorWindow
    {
        string _compiler_defines = "editor=true;blastversion=1;";
        string hpc_cache = "Assets/Resources/AI/Scripts/blast_precompiled_scripts.cs";
        bool hpc_enabled = true;  

        public string CompilerDefines { get { return _compiler_defines; } set { _compiler_defines = value; } }

        public bool HPCEnabled
        {
            get 
            { 
                return hpc_enabled; 
            }
            set
            {
                hpc_enabled = value;
            }
        }

        public string HPCCache
        {
            get { return hpc_cache; }
            set { hpc_cache = value; }
        }


#region Blast Language Version 
        private int _selected_language_version = 0; 
        public BlastLanguageVersion LanguageVersion 
        { 
            get { return supported_language_types[_selected_language_version]; }
            set
            {                                                      
                for (int i = 0; i < supported_language_types.Length; i++)
                {
                    if(supported_language_types[i] == value)
                    {
                        _selected_language_version = i;
                        return;
                    }
                }
                _selected_language_version = 0; 
            }
        }

        string[] supported_language_versions = new string[]
        {
            BlastLanguageVersion.BS1.ToString()
        };
        BlastLanguageVersion[] supported_language_types = new BlastLanguageVersion[]
        {
            BlastLanguageVersion.BS1
        };
#endregion



        [MenuItem("Window/Blast")]
        public static void ShowWindow()
        {
            EditorWindow.GetWindow(typeof(BlastConfigurationWindow));
        }


        Vector2 scroll_pos_jobs;
        Vector2 scroll_pos_scripts;

        void OnGUI()
        {
            GUILayout.Label("BLAST Configuration", EditorStyles.boldLabel);
            GUILayout.Space(10);
            
            GUILayout.Label("Compiler Settings :", EditorStyles.boldLabel);
            GUILayout.Label("Language Version:", EditorStyles.miniLabel);
            LanguageVersion = supported_language_types[EditorGUILayout.Popup(_selected_language_version, supported_language_versions)];
            GUILayout.Space(3);

            GUILayout.Label("';' seperated list of key=value defines", EditorStyles.miniLabel);
            CompilerDefines = GUILayout.TextArea(CompilerDefines, GUILayout.Height(40));
            GUILayout.Space(10);

            
            GUILayout.Label("Designtime Compilation", EditorStyles.boldLabel);
            hpc_cache = EditorGUILayout.TextField("Compilation Target:", hpc_cache);
            if(GUILayout.Button("Pre-Compile Scripts"))
            {
                // pre-compile known scripts into cache 
                Blast blast = Blast.Create(Unity.Collections.Allocator.Persistent);

                Blast.CompileRegistry(blast, hpc_cache); 

                blast.Destroy();

                AssetDatabase.Refresh();
                Repaint(); 
            }
            GUILayout.Space(3);

            
            GUILayout.Label("Scripts in Project", EditorStyles.miniLabel);

            scroll_pos_scripts = GUILayout.BeginScrollView(scroll_pos_scripts);
            StringBuilder sb = new StringBuilder();
            foreach (BlastScript bs in BlastScriptRegistry.All())
            {
                GUILayout.Label($"{bs.Id.ToString().PadLeft(3, '0')} {bs.ToString()}");
            }
            GUILayout.EndScrollView(); 
            GUILayout.Space(3);


            GUILayout.Label("Precompiled HPC Jobs in Project", EditorStyles.miniLabel);
            var jobs = BlastReflect.FindHPCJobs();
            if (jobs.Count > 0 && File.Exists(hpc_cache))
            {
                if (GUILayout.Button("Remove Precompiled Data"))
                {
                    // pre-compile known scripts into cache 
                    File.Delete(hpc_cache);
                    File.Delete(hpc_cache + ".meta"); 
                        
                    // refresh asset database 
                    AssetDatabase.Refresh();
                    Repaint();
                }
            }

            scroll_pos_jobs = GUILayout.BeginScrollView(scroll_pos_jobs);
            sb = new StringBuilder();
            foreach (var job in jobs)
            {
                GUILayout.Label($"{job.ScriptId.ToString().PadLeft(3, '0')} {job.GetType().Name}", EditorStyles.miniLabel);
            }
            GUILayout.EndScrollView(); 
            GUILayout.Space(3);


        }
    }

}
#endif 

