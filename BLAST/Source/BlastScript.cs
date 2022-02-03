//##########################################################################################################
// Copyright © 2022 Rob Lemmens | NijnStein Software <rob.lemmens.s31@gmail.com> All Rights Reserved  ^__^\#
// Unauthorized copying of this file, via any medium is strictly prohibited                           (oo)\#
// Proprietary and confidential                                                                       (__) #
//##########################################################################################################
using NSS.Blast.Compiler;
using NSS.Blast.Interpretor;
using System;
using System.Collections.Generic;
using System.Linq;
#if !STANDALONE_VSBUILD
using System.Runtime.InteropServices.WindowsRuntime;
using UnityEngine.Assertions;
#else
using Unity.Assertions;

#endif
using System.Threading;
using UnityEngine;
using System.Runtime.CompilerServices;
using Unity.Mathematics;
using Unity.Collections;

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


        #region Variables 

        /// <summary>
        /// Lookup a variable by its index, the script must be prepared first
        /// </summary>
        /// <param name="index">the variable index</param>
        /// <param name="assert_on_fail">disable assertion on failure to lookup variable</param>
        /// <returns>BlastVariable or null on errors in release (asserts in debug)</returns>
        protected BlastVariable GetVariable(int index, bool assert_on_fail = true)
        {
            Assert.IsTrue(IsPrepared, $"BlastScript.GetVariable: script {Id} {Name}, bytecode not packaged, run Prepare() first to compile its script package.");
            if(assert_on_fail) Assert.IsFalse(Package.Variables == null || index < 0 || index >= Package.Variables.Length, $"BlastScript.GetVariable: script {Id} {Name}, variable index {index} is out of range");
            return Package.Variables[index];
        }

        /// <summary>
        /// Lookup a variable by its name, the script must be prepared first, this will assert if the name does not exist or return null on failure
        /// </summary>
        /// <param name="name">the name</param>
        /// <param name="assert_on_fail">disable assertion on failure to lookup variable</param>
        /// <returns>BlastVariable or null on errors in release (asserts in debug)</returns>
        protected BlastVariable GetVariable(string name, bool assert_on_fail = true)
        {
            Assert.IsTrue(IsPrepared, $"BlastScript.GetVariable: script {Id} {Name}, bytecode not packaged, run Prepare() first to compile its script package.");
            if(assert_on_fail) Assert.IsFalse(Package.Variables == null || Package.Variables.Length == 0, $"BlastScript.GetVariable: script {Id} {Name}, variable '{name}' could not be found");

            foreach(BlastVariable v in Package.Variables)
            {
                if (v != null && !string.IsNullOrWhiteSpace(v.Name))
                {
                    if(string.Compare(v.Name, name, true) == 0)
                    {
                        return v; 
                    }
                }
            }

            if (assert_on_fail) Assert.IsFalse(Package.Variables == null || Package.Variables.Length == 0, $"BlastScript.GetVariable: script {Id} {Name}, variable '{name}' could not be found");
            return null; 
        }

        /// <summary>
        /// Get the number of variables defined by the script, the script must be prepared first 
        /// </summary>
        /// <returns>the number of variables in the script</returns>
        protected int GetVariableCount()
        {
            Assert.IsTrue(IsPrepared, $"BlastScript.GetVariable: script {Id} {Name}, bytecode not packaged, run Prepare() first to compile its script package.");
            return Package.Variables == null ? 0 : Package.Variables.Length; 
        }

        /// <summary>
        /// get offset of given variable
        /// </summary>
        /// <param name="variable_index">index of variable</param>
        /// <returns></returns>
        protected int GetVariableOffset(in int variable_index)
        {
            Assert.IsTrue(IsPrepared, $"BlastScript.GetVariableOffset: script {Id} {Name}, bytecode not packaged, run Prepare() first to compile its script package.");
            Assert.IsTrue(variable_index >= 0 && variable_index < Package.Variables.Length, $"BlastScriptOffset.GetVariable: script {Id} {Name}, variable index {variable_index} out of range");

            return Package.VariableOffsets[variable_index]; 
        }

        #endregion

        #region Inputs|Outputs 

        //
        // if the script does not define inputs or outputs then the compiler should create a list
        // of inputs/outputs containing all variables 
        // 

        void assert_is_valid_input(in int input_index)
        {
#if DEVELOPMENT_BUILD || TRACE
            Assert.IsTrue(IsPrepared, $"BlastScript: script {Id} {Name}, bytecode not packaged, run Prepare() first to compile its script package.");
            Assert.IsTrue(Package.Inputs != null && input_index >= 0 && input_index < Package.Inputs.Length, $"GetVariable: script {Id} {Name}, input index {input_index} invalid");
#endif
        }
        void assert_is_valid_input_size(in int input_index, in int size)
        { 
#if DEVELOPMENT_BUILD || TRACE
            Assert.IsTrue(IsPrepared, $"BlastScript: script {Id} {Name}, bytecode not packaged, run Prepare() first to compile its script package.");
            Assert.IsTrue(Package.Inputs != null && input_index >= 0 && input_index < Package.Inputs.Length, $"GetVariable: script {Id} {Name}, input index {input_index} invalid");

            Assert.IsTrue(Package.Inputs[input_index].Variable.VectorSize == size, $"BlastScript: script {Id} {Name}, bytecode not packaged, run Prepare() first to compile its script package.");
#endif
        }


        /// <summary>
        /// get number of variables that are defined as input 
        /// </summary>
        /// <returns></returns>
        public int GetInputCount()
        {
#if DEVELOPMENT_BUILD || TRACE
            Assert.IsTrue(IsPrepared, $"BlastScript.GetVariable: script {Id} {Name}, bytecode not packaged, run Prepare() first to compile its script package.");
#endif 
            return Package != null ? (Package.Inputs == null ? 0 : Package.Inputs.Length) : 0;
        }
        
        /// <summary>
        /// get variable representing input_index 
        /// </summary>
        /// <param name="input_index"></param>
        /// <returns></returns>
        public BlastVariable GetInput(in int input_index)
        {
            assert_is_valid_input(input_index);
            return Package.Inputs[input_index].Variable;
        }

        /// <summary>
        /// get offset of given input
        /// </summary>
        /// <param name="input_index"></param>
        /// <returns></returns>
        public int GetInputOffset(in int input_index)
        {
            assert_is_valid_input(input_index);
            return Package.Inputs[input_index].Offset;
        }

        /// <summary>
        /// get a float input/output value, asserts on errors
        /// </summary>
        /// <param name="input_index"></param>
        /// <returns></returns>
        public float GetFloat(in int input_index)
        {
            assert_is_valid_input_size(input_index, 1);


            int offset = Package.Inputs[input_index].Offset;

            unsafe
            {
                return Package.Package.Data[offset];
            }
        }

        /// <summary>
        /// get a float2 input/output value, asserts on errors
        /// </summary>
        /// <param name="input_index"></param>
        /// <returns></returns>
        public float2 GetFloat2(in int input_index)
        {
            assert_is_valid_input_size(input_index, 2);

            int offset = Package.Inputs[input_index].Offset;

            unsafe
            {
                return ((float2*)(void*)&Package.Package.Data[offset])[0];
            }
        }


        /// <summary>
        /// get a float3 input/output value, asserts on errors
        /// </summary>
        /// <param name="input_index"></param>
        /// <returns></returns>
        public float3 GetFloat3(in int input_index)
        {
            assert_is_valid_input_size(input_index, 3);

            int offset = Package.Inputs[input_index].Offset;

            unsafe
            {
                return ((float3*)(void*)&Package.Package.Data[offset])[0];
            }
        }


        /// <summary>
        /// get a float4 input/output value, asserts on errors
        /// </summary>
        /// <param name="input_index"></param>
        /// <returns></returns>
        public float4 GetFloat4(in int input_index)
        {
            assert_is_valid_input_size(input_index, 4);

            int offset = Package.Inputs[input_index].Offset;

            unsafe
            {
                return ((float4*)(void*)&Package.Package.Data[offset])[0];
            }
        }

        #endregion

        #region Static Get|Set Data overloads

        /// <summary>
        /// set variable in package data
        /// </summary>
        /// <param name="variable_offset">the variable offset</param>
        /// <param name="packages">array of package data</param>
        /// <param name="data">array of float data to fill</param>
        /// <returns></returns>
        static public bool SetData(in int variable_offset, in NativeArray<BlastPackageData> packages, ref NativeArray<float> data)
        {
            if (!packages.IsCreated || !data.IsCreated || packages.Length != data.Length || variable_offset < 0) return false;
            if (packages.Length > data.Length) return false;
            if (variable_offset + 1 > packages[0].DataSize) return false;
            if (packages.Length == 0) return true;

            unsafe
            {
                for (int i = 0; i < packages.Length; i++)
                {
                    data[i] = packages[i].Data[variable_offset];
                }
            }

            return true;
        }

        /// <summary>
        /// set variable in package data
        /// </summary>
        /// <param name="variable_offset">the variable offset</param>
        /// <param name="packages">array of package data</param>
        /// <param name="data">array of float2 data to fill</param>
        /// <returns></returns>
        static public bool SetData(in int variable_offset, in NativeArray<BlastPackageData> packages, ref NativeArray<float2> data)
        {
            if (!packages.IsCreated || !data.IsCreated || packages.Length != data.Length || variable_offset < 0) return false;
            if (packages.Length > data.Length) return false;
            if (variable_offset + 2 > packages[0].DataSize) return false;
            if (packages.Length == 0) return true;

            unsafe
            {
                for (int i = 0; i < packages.Length; i++)
                {
                    data[i] = ((float2*)(void*)&packages[i].Data[variable_offset])[0];
                }
            }

            return true;
        }


        /// <summary>
        /// set variable in package data
        /// </summary>
        /// <param name="variable_offset">the variable offset</param>
        /// <param name="packages">array of package data</param>
        /// <param name="data">array of float2 data to fill</param>
        /// <returns></returns>
        static public bool SetData(in int variable_offset, in NativeArray<BlastPackageData> packages, ref NativeArray<float3> data)
        {
            if (!packages.IsCreated || !data.IsCreated || packages.Length != data.Length || variable_offset < 0) return false;
            if (packages.Length > data.Length) return false;
            if (variable_offset + 3 > packages[0].DataSize) return false;
            if (packages.Length == 0) return true;

            unsafe
            {
                for (int i = 0; i < packages.Length; i++)
                {
                    data[i] = ((float3*)(void*)&packages[i].Data[variable_offset])[0];
                }
            }

            return true;
        }


        /// <summary>
        /// set variable in package data
        /// </summary>
        /// <param name="variable_offset">the variable offset</param>
        /// <param name="packages">array of package data</param>
        /// <param name="data">array of float2 data to fill</param>
        /// <returns></returns>
        static public bool SetData(in int variable_offset, in NativeArray<BlastPackageData> packages, ref NativeArray<float4> data)
        {
            if (!packages.IsCreated || !data.IsCreated || packages.Length != data.Length || variable_offset < 0) return false;
            if (packages.Length > data.Length) return false;
            if (variable_offset + 4 > packages[0].DataSize) return false;
            if (packages.Length == 0) return true;

            unsafe
            {
                for (int i = 0; i < packages.Length; i++)
                {
                    data[i] = ((float4*)(void*)&packages[i].Data[variable_offset])[0];
                }
            }

            return true;
        }


        /// <summary>
        /// get all data for a given variable offset for a collection of packages 
        /// </summary>
        /// <param name="variable_offset">the offset of the variable into the datasegment</param>
        /// <param name="packages">the packages to get the data from</param>
        /// <param name="data">the output to write to</param>
        /// <returns>true if ok, false on errors</returns>
        [BurstCompatible]
        static public bool GetData(in int variable_offset, in NativeArray<BlastPackageData> packages, ref NativeArray<float> data)
        {
            if (!packages.IsCreated || !data.IsCreated || packages.Length != data.Length || variable_offset < 0) return false;
            if (packages.Length > data.Length) return false;
            if (variable_offset + 1 > packages[0].DataSize) return false;
            if (packages.Length == 0) return true;

            unsafe
            {
                for (int i = 0; i < packages.Length; i++)
                {
                    data[i] = packages[i].Data[variable_offset];
                }
            }

            return true;
        }


        /// <summary>
        /// get all data for a given variable offset for a collection of packages 
        /// </summary>
        /// <param name="variable_offset">the offset of the variable into the datasegment</param>
        /// <param name="packages">the packages to get the data from</param>
        /// <param name="data">the output to write to</param>
        /// <param name="offset">the offset into output from where to start writing output data</param>
        /// <returns>true if ok, false on errors</returns>
        [BurstCompatible]
        static public bool GetData(in int variable_offset, in NativeArray<BlastPackageData> packages, ref NativeArray<float2> data, int offset = 0)
        {
            if (!packages.IsCreated || !data.IsCreated || packages.Length != data.Length || variable_offset < 0) return false;
            if (packages.Length > data.Length) return false;
            if (variable_offset + 2 > packages[0].DataSize) return false;
            if (packages.Length == 0) return true;

            unsafe
            {
                for (int i = 0; i < packages.Length; i++)
                {
                    data[i] = ((float2*)(void*)&packages[i].Data[variable_offset])[0];
                }
            }

            return true;
        }


        /// <summary>
        /// get all data for a given variable offset for a collection of packages 
        /// </summary>
        /// <param name="variable_offset">the offset of the into the datasegment</param>
        /// <param name="packages">the packages to get the data from</param>
        /// <param name="data">the output to write to</param>
        /// <param name="offset">the offset into output from where to start writing output data</param>
        /// <returns>true if ok, false on errors</returns>
        [BurstCompatible]
        static public bool GetData(in int variable_offset, in NativeArray<BlastPackageData> packages, ref NativeArray<float3> data, int offset = 0)
        {
            if (!packages.IsCreated || !data.IsCreated || packages.Length != data.Length || variable_offset < 0) return false;
            if (packages.Length > data.Length) return false;
            if (variable_offset + 3 > packages[0].DataSize) return false;
            if (packages.Length == 0) return true;

            unsafe
            {
                for (int i = 0; i < packages.Length; i++)
                {
                    data[i] = ((float3*)(void*)&packages[i].Data[variable_offset])[0];
                }
            }

            return true;
        }
        
                
        /// <summary>
        /// get all data for a given input offset for a collection of packages 
        /// </summary>
        /// <param name="variable_offset">the offset of the input into the datasegment</param>
        /// <param name="packages">the packages to get the data from</param>
        /// <param name="data">the output to write to</param>
        /// <param name="offset">the offset into output from where to start writing output data</param>
        /// <returns>true if ok, false on errors</returns>
        [BurstCompatible]
        static public bool GetData(in int variable_offset, in NativeArray<BlastPackageData> packages, ref NativeArray<float4> data, int offset = 0)
        {
            if (!packages.IsCreated || !data.IsCreated || packages.Length != data.Length || variable_offset < 0) return false;
            if (packages.Length > data.Length) return false;
            if (variable_offset + 4 > packages[0].DataSize) return false;
            if (packages.Length == 0) return true;

            unsafe
            {
                for (int i = 0; i < packages.Length; i++)
                {
                    data[i] = ((float4*)(void*)&packages[i].Data[variable_offset])[0];
                }
            }

            return true;
        }

        #endregion 
    }

}
