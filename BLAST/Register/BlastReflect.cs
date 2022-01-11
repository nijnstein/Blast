using NSS.Blast.Cache;
#if NOT_USING_UNITY
using NSS.Blast.Standalone;
#else
using UnityEngine; 
#endif 
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;

namespace NSS.Blast.Register
{
    /// <summary>
    /// provides utilities for finding and loading compile-time blast scripts and hpc jobs
    /// </summary>
    public static class BlastReflect
    {
        static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
        {
            if (assembly == null) throw new ArgumentNullException("assembly");
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException e)
            {
                return e.Types.Where(t => t != null);
            }
        }

        static public List<CompileTimeBlastScript> FindCompileTimeBlastScripts()
        {
            try
            {
                Type t = typeof(CompileTimeBlastScript);
                List<CompileTimeBlastScript> jobs = new List<CompileTimeBlastScript>();

                foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    List<Type> types = GetLoadableTypes(asm).Where(t.IsAssignableFrom).ToList();
                    for (int i = 0; i < types.Count; i++)
                    {
                        if (!types[i].IsAbstract)
                        {
                            Object obj = Activator.CreateInstance(types[i]);
                            jobs.Add((CompileTimeBlastScript)obj);
                        }
                    }
                }

                return jobs;
            }
            catch (Exception ex)
            {
                Debug.LogError("BlastReflect.FindCompileTimeBlastScripts: failed to enumerate blast script to pre-compile: " + ex.ToString());
                return null;
            }
        }

        static public List<IBlastHPCScriptJob> FindHPCJobs()
        {
            try
            {
                Type t = typeof(IBlastHPCScriptJob);
                List<IBlastHPCScriptJob> jobs = new List<IBlastHPCScriptJob>();

                foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    List<Type> types = GetLoadableTypes(asm).Where(t.IsAssignableFrom).ToList();
                    for (int i = 0; i < types.Count; i++)
                    {
                        Type type = types[i];
                        if (!type.IsInterface && !type.IsAbstract)
                        {
                            Object obj = FormatterServices.GetUninitializedObject(type);
                            jobs.Add((IBlastHPCScriptJob)obj);
                        }
                    }
                }                                                             

                return jobs;    
            }
            catch (Exception ex)
            {
                Standalone.Debug.LogError("BlastReflect.FindHPCJobs: failed to enumerate pre-compiled blast jobs: " + ex.ToString());
                return new List<IBlastHPCScriptJob>(); 
            }
        }
    } 
}