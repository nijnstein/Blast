

using NSS.Blast.Compiler;
using NSS.Blast.Interpretor;
using System;
using System.Collections.Generic;
using System.Linq;
#if !STANDALONE_VSBUILD
using System.Runtime.InteropServices.WindowsRuntime;
#endif
using System.Threading;
using Unity.Assertions;
using UnityEngine;

namespace NSS.Blast
{


    /// <summary>
    /// A BLAST Script
    /// </summary>
    public class BlastScript : IDisposable
    {
        /// <summary>
        /// Target language vesion, depending on compilation settings it might change in the package
        /// but the user should ensure that the code is compatible; 
        /// </summary>
        public BlastLanguageVersion LanguageVersion { get; internal set; } = BlastLanguageVersion.BS1; 

        /// <summary>
        /// Blast scriptid, used to uniquely identify the script. It is used throughout blast to id the packaged script
        /// </summary>
        public int Id { get; internal set; }

        /// <summary>
        /// The name of the script as used in messages 
        /// </summary>
        public string Name { get; internal set; }
        
        /// <summary>
        /// the actual scriptcode conforming to the languageversion set
        /// </summary>
        public string Code { get; internal set; }

        /// <summary>
        /// native package data, contains references to code and data segments 
        /// </summary>
        public BlastScriptPackage Package { get; internal set; }

        /// <summary>
        ///  true if the script has been packaged / prepared
        /// </summary>
        public bool IsPackaged => Package != null;

        /// <summary>
        ///  true if the script has been packaged / prepared
        /// </summary>
        public bool IsPrepared => IsPackaged; 

        /// <summary>
        /// create script object from code 
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


        /// <summary>
        /// Prepare the script for execution
        /// </summary>
        /// <param name="blast">blast engine data</param>
        /// <returns>success if all is ok </returns>
        public BlastError Prepare(IntPtr blast)
        {
            if (blast == IntPtr.Zero) return BlastError.error_blast_not_initialized;
            if (IsPackaged) return BlastError.error_already_packaged;

            BlastScriptPackage package = new BlastScriptPackage();

            IBlastCompilationData data = BlastCompiler.Compile(blast, this, default);
            if (!data.IsOK) return BlastError.error_compilation_failure; 

            package.Package = BlastCompiler.Package(data, default);
            if (!data.IsOK)
            {
                this.Package = package;
            }
            return data.IsOK ? BlastError.success : BlastError.compile_packaging_error; 
        }

        /// <summary>
        /// execute the script in the given environment with the supplied data
        /// </summary>
        /// <param name="blast">blastengine data</param>
        /// <param name="environment">[optional] pointer to environment data</param>
        /// <param name="caller">[ooptional] caller data</param>
        /// <returns>success if all is ok</returns>
       public BlastError Execute(IntPtr blast, IntPtr environment, IntPtr caller)
        {
            if (blast == IntPtr.Zero) return BlastError.error_blast_not_initialized;
            if (!IsPackaged)
            {
                BlastError res = Prepare(blast);
                if (res != BlastError.success) return res; 
            }
            return Package.Execute(blast, environment, caller); 
        }


#if !STANDALONE_VSBUILD
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

        /// <summary>
        /// cleanup package data if allocated on dispose 
        /// </summary>
        public void Dispose()
        {
            ReleasePackage(); 
        }

        /// <summary>
        /// release package memory, nulls reference to it and free;s any native package memory with it 
        /// </summary>
        public void ReleasePackage()
        {
            if (IsPackaged)
            {
                Package.Destroy();
                Package = null; 
            }
        }

    }

}
