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

#if STANDALONE_VSBUILD
    using NSS.Blast.Standalone;
    using UnityEngine.Assertions;
#else
    using UnityEngine;
    using UnityEngine.Assertions;
    using Unity.Collections.LowLevel.Unsafe;
#endif

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

        public bool HasVariables { get { return Package != null && Package.Variables != null && Package.Variables.Length > 0; } }
        public bool HasInputs { get { return Package != null && Package.Inputs != null && Package.Inputs.Length > 0; } }


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
        /// <returns>success if all is ok </returns>
        public BlastError Prepare(bool prepare_variable_info = true)
        {
            if (!Blast.IsInstantiated) return BlastError.error_blast_not_initialized; 
            return Prepare(Blast.Instance.Engine, prepare_variable_info); 
        }



        /// <summary>
        /// Prepare the script for execution
        /// </summary>
        /// <param name="blast">blast engine data</param>
        /// <returns>success if all is ok </returns>
        public BlastError Prepare(IntPtr blast, bool prepare_variable_info = true)
        {
            if (blast == IntPtr.Zero) return BlastError.error_blast_not_initialized;
            if (IsPackaged) return BlastError.error_already_packaged;

#if DEVELOPMENT_BUILD || TRACE
            BlastCompilerOptions options = BlastCompilerOptions.Default.Trace();
#else
            BlastCompilerOptions options = BlastCompilerOptions.Default; 
#endif
            if(prepare_variable_info)
            {
                BlastError res = BlastCompiler.CompilePackage(blast, this, options);
                return res;                 
            }
            else
            {
                this.Package = new BlastScriptPackage();

                IBlastCompilationData data = BlastCompiler.Compile(blast, this, options);
                if (!data.IsOK) return BlastError.error_compilation_failure;

                this.Package.Package = BlastCompiler.Package(data, options);
                if (!data.IsOK) return BlastError.compile_packaging_error;

                //if (!this.Package.HasInputs && this.Package.HasVariables)
                //{
                //    this.Package.DefineInputsFromVariables(); 
                //}

                return BlastError.success; 
            }
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

        /// <summary>
        /// execute the script
        /// </summary>
        /// <param name="blast">reference to blast</param>
        /// <returns>success if all is ok</returns>
        public BlastError Execute(IntPtr blast)
        {
            return Package.Execute(blast, IntPtr.Zero, IntPtr.Zero);
        }

        /// <summary>
        /// execute the script using static blast instance 
        /// </summary>
        /// <returns></returns>
        public BlastError Execute()
        {
            if (!Blast.IsInstantiated) return BlastError.error_blast_not_initialized; 

            if (!IsPackaged)
            {
                BlastError res = Prepare(Blast.Instance.Engine);
                if (res != BlastError.success) return res;
            }

            return Package.Execute(Blast.Instance.Engine, IntPtr.Zero, IntPtr.Zero);
        }

        /// <summary>
        /// Run (execute) the script 
        /// </summary>
        /// <param name="assert_on_errors"></param>
        /// <returns></returns>
        public BlastScript Run(bool assert_on_errors = true)
        {
            BlastError res = Execute();
            Assert.IsTrue(assert_on_errors && res != BlastError.success, "failed to execute script, error = {res}, please see the log for details");
            return res == BlastError.success ? this : null; // end any chaining on errors     
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


        #region Variable Indexers 


        /// <summary>
        /// Get|Set variable by name
        /// </summary>
        /// <param name="name">caseinsensitive name of variable to get|set</param>
        /// <returns>boxed object</returns>
        /// <remarks>
        /// 
        /// Lookup variable by name and set its value: 
        /// 
        /// <code language="cs">
        ///     script["a"] = 12f;
        ///     script["v3"] = new float3(1, 1, 1);
        /// </code>
        /// </remarks>
        public object this[string name]
        {
            get
            {
                BlastVariable v = GetVariable(name, true);
                if (v == null) return null; 

                int offset = Package.VariableOffsets[v.Id];

                unsafe
                {
                    if (GetDataPointer(out float* data, out int l))
                    {
                        switch (v.VectorSize)
                        {
                            case 1: return data[offset];
                            case 2: return ((float2*)(void*)&data[offset])[0];
                            case 3: return ((float3*)(void*)&data[offset])[0];
                            case 4: return ((float4*)(void*)&data[offset])[0];
                        }
                    }
                }
#if ENABLE_UNITY_COLLECTIONS_CHECKS || DEVELOPMENT_BUILD
                Assert.IsTrue(false, $"variable {v.Id} '{Name}' vectorsize {v.VectorSize} out of range");
#endif
                return null;                 
            }

            set
            {
                BlastVariable v = GetVariable(name, true);
                if (v == null) return;

                int offset = Package.VariableOffsets[v.Id];

                unsafe
                {
                    if (GetDataPointer(out float* data, out int l))
                    {
                        switch (v.VectorSize)
                        {
                            case 1: data[offset] = (float)value;break;
                            case 2: ((float2*)(void*)&data[offset])[0] = (float2)value; break;
                            case 3: ((float3*)(void*)&data[offset])[0] = (float3)value; break;
                            case 4: ((float4*)(void*)&data[offset])[0] = (float4)value; break;
#if ENABLE_UNITY_COLLECTIONS_CHECKS || DEVELOPMENT_BUILD
                            default:
                                Assert.IsTrue(false, $"variable {v.Id} '{Name}' vectorsize {v.VectorSize} out of range");
                                break; 
#endif
                        }
                    }
                }
                return;
            }
        }


        /// <summary>
        /// get|set a variable by index
        /// </summary>
        /// <param name="index">0 based index of variable, blast wil index them in order of appearance in script</param>
        /// <returns></returns>
        public object this[int index]
        {
            get
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS || DEVELOPMENT_BUILD
                if (index < 0 || index >= GetVariableCount())
                {
                    Assert.IsTrue(false, $"variable index {index} out of range");
                    return null;
                }
#endif
                int offset = Package.VariableOffsets[index];

                unsafe
                {
                    if (GetDataPointer(out float* data, out int l))
                    {
                        switch (Package.Variables[index].VectorSize)
                        {
                            case 1: return data[offset];
                            case 2: return ((float2*)(void*)&data[offset])[0];
                            case 3: return ((float3*)(void*)&data[offset])[0];
                            case 4: return ((float4*)(void*)&data[offset])[0];
                        }
                    }
                }
#if ENABLE_UNITY_COLLECTIONS_CHECKS || DEVELOPMENT_BUILD
                Assert.IsTrue(false, $"variable {index} '{Package.Variables[index].Name}', vectorsize {Package.Variables[index].VectorSize} out of range");
#endif
                return null;
            }

            set
            {

#if ENABLE_UNITY_COLLECTIONS_CHECKS || DEVELOPMENT_BUILD
                if (index < 0 || index >= GetVariableCount())
                {
                    Assert.IsTrue(false, $"variable index {index} out of range");
                    return;
                }
#endif

                int offset = Package.VariableOffsets[index];

                unsafe
                {
                    if (GetDataPointer(out float* data, out int l))
                    {
                        switch (Package.Variables[index].VectorSize)
                        {
                            case 1: data[offset] = (float)value; break;
                            case 2: ((float2*)(void*)&data[offset])[0] = (float2)value; break;
                            case 3: ((float3*)(void*)&data[offset])[0] = (float3)value; break;
                            case 4: ((float4*)(void*)&data[offset])[0] = (float4)value; break;
#if ENABLE_UNITY_COLLECTIONS_CHECKS || DEVELOPMENT_BUILD
                            default:
                                Assert.IsTrue(false, $"variable {index} '{Package.Variables[index].Name}' vectorsize {Package.Variables[index].VectorSize} out of range");
                                break;
#endif
                        }
                    }
                }
                return;
            }
        }


#endregion



#region Variables 

        /// <summary>
        /// Lookup a variable by its index, the script must be prepared first
        /// </summary>
        /// <param name="index">the variable index</param>
        /// <param name="assert_on_fail">disable assertion on failure to lookup variable</param>
        /// <returns>BlastVariable or null on errors in release (asserts in debug)</returns>
        public BlastVariable GetVariable(int index, bool assert_on_fail = true)
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
        public BlastVariable GetVariable(string name, bool assert_on_fail = true)
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
        /// get variable offset into datasegment 
        /// </summary>
        /// <param name="name">name of script variable</param>
        /// <param name="assert_on_fail">assert on failure to locate variable, default = true</param>
        /// <returns>the variable offset or -1 if not found</returns>
        public int GetVariableOffset(string variable_name, bool assert_on_fail = true)
        {
            Assert.IsTrue(IsPrepared, $"BlastScript.GetVariableOffset: script {Id} {Name}, bytecode not packaged, run Prepare() first to compile its script package.");
            if (assert_on_fail) Assert.IsFalse(Package.Variables == null || Package.Variables.Length == 0, $"BlastScript.GetVariableOffset: script {Id} {Name}, variable '{variable_name}' could not be found");

            for (int i = 0; i < Package.Variables.Length; i++)
            {
                BlastVariable v = Package.Variables[i];
                if (v != null && !string.IsNullOrWhiteSpace(v.Name))
                {
                    if (string.Compare(v.Name, variable_name, true) == 0)
                    {
                        return Package.VariableOffsets[i];
                    }
                }
            }

            if (assert_on_fail) Assert.IsFalse(Package.Variables == null || Package.Variables.Length == 0, $"BlastScript.GetVariableOffset: script {Id} {Name}, variable '{variable_name}' could not be found");
            return -1;
        }

        /// <summary>
        /// get index of a named variable 
        /// </summary>
        /// <param name="variable_name">variable name to lookup</param>
        /// <param name="assert_on_fail">assert on failed lookups if true</param>
        /// <returns>variable index into package</returns>
        public int GetVariableIndex(string variable_name, bool assert_on_fail = true)
        {
            Assert.IsTrue(IsPrepared, $"BlastScript.GetVariableIndex: script {Id} {Name}, bytecode not packaged, run Prepare() first to compile its script package.");
            if (assert_on_fail) Assert.IsFalse(Package.Variables == null || Package.Variables.Length == 0, $"BlastScript.GetVariableIndex: script {Id} {Name}, variable '{variable_name}' could not be found");

            for (int i = 0; i < Package.Variables.Length; i++)
            {
                BlastVariable v = Package.Variables[i];
                if (v != null && !string.IsNullOrWhiteSpace(v.Name))
                {
                    if (string.Compare(v.Name, variable_name, true) == 0)
                    {
                        return i;
                    }
                }
            }

            if (assert_on_fail) Assert.IsFalse(Package.Variables == null || Package.Variables.Length == 0, $"BlastScript.GetVariableIndex: script {Id} {Name}, variable '{variable_name}' could not be found");
            return -1;
        }

        /// <summary>
        /// Get the number of variables defined by the script, the script must be prepared first 
        /// </summary>
        /// <returns>the number of variables in the script</returns>
        public int GetVariableCount()
        {
            Assert.IsTrue(IsPrepared, $"BlastScript.GetVariable: script {Id} {Name}, bytecode not packaged, run Prepare() first to compile its script package.");
            return Package.Variables == null ? 0 : Package.Variables.Length; 
        }

        /// <summary>
        /// get offset of given variable
        /// </summary>
        /// <param name="variable_index">index of variable</param>
        /// <returns></returns>
        public int GetVariableOffset(in int variable_index)
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
        /// get a float input/output value
        /// </summary>
        /// <returns></returns>
        public float GetFloat(in int data_index)
        {
            unsafe
            {
                return Package.Package.Data[Package.VariableOffsets[data_index]];
            }
        }

        /// <summary>
        /// get a float2 input/output value
        /// </summary>
        public float2 GetFloat2(in int data_index)
        {
            int offset = Package.VariableOffsets[data_index];
            unsafe
            {
                return ((float2*)(void*)&Package.Package.Data[offset])[0];
            }
        }


        /// <summary>
        /// get a float3 input/output value, asserts on errors
        /// </summary>
        public float3 GetFloat3(in int data_index)
        {
            int offset = Package.VariableOffsets[data_index];
            unsafe
            {
                return ((float3*)(void*)&Package.Package.Data[offset])[0];
            }
        }


        /// <summary>
        /// get a float4 input/output value, asserts on errors
        /// </summary>
        public float4 GetFloat4(in int data_index)
        {
            int offset = Package.VariableOffsets[data_index];

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
        static public bool SetData(in int variable_offset, in NativeArray<BlastPackageData> packages, in NativeArray<float> data)
        {
            if (!packages.IsCreated || !data.IsCreated || packages.Length != data.Length || variable_offset < 0) return false;
            if (packages.Length > data.Length) return false;
            if (variable_offset + 1 > packages[0].DataSize) return false;
            if (packages.Length == 0) return true;

            unsafe
            {
                for (int i = 0; i < packages.Length; i++)
                {
                    packages[i].Data[variable_offset] = data[i];
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
        static public bool SetData(in int variable_offset, in NativeArray<BlastPackageData> packages, in NativeArray<float2> data)
        {
            if (!packages.IsCreated || !data.IsCreated || packages.Length != data.Length || variable_offset < 0) return false;
            if (packages.Length > data.Length) return false;
            if (variable_offset + 2 > packages[0].DataSize) return false;
            if (packages.Length == 0) return true;

            unsafe
            {
                for (int i = 0; i < packages.Length; i++)
                {
                    ((float2*)(void*)&packages[i].Data[variable_offset])[0] = data[i]; 
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
        static public bool SetData(in int variable_offset, in NativeArray<BlastPackageData> packages, in NativeArray<float3> data)
        {
            if (!packages.IsCreated || !data.IsCreated || packages.Length != data.Length || variable_offset < 0) return false;
            if (packages.Length > data.Length) return false;
            if (variable_offset + 3 > packages[0].DataSize) return false;
            if (packages.Length == 0) return true;

            unsafe
            {
                for (int i = 0; i < packages.Length; i++)
                {
                    ((float3*)(void*)&packages[i].Data[variable_offset])[0] = data[i];
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
        static public bool SetData(in int variable_offset, in NativeArray<BlastPackageData> packages, in NativeArray<float4> data)
        {
            if (!packages.IsCreated || !data.IsCreated || packages.Length != data.Length || variable_offset < 0) return false;
            if (packages.Length > data.Length) return false;
            if (variable_offset + 4 > packages[0].DataSize) return false;
            if (packages.Length == 0) return true;

            unsafe
            {
                for (int i = 0; i < packages.Length; i++)
                {
                    ((float4*)(void*)&packages[i].Data[variable_offset])[0] = data[i]; 
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
        /// reset default values in datasegment as they might have been overwritten during execution
        /// </summary>
        public void ResetDefaults()
        {
            if (IsPackaged && IsPrepared)
            {
                Package.ResetDefaults(); 
            }
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


#if !STANDALONE_VSBUILD

        /// <summary>
        /// get a direct pointer to the data in the package 
        /// </summary>
        /// <param name="package"></param>
        /// <param name="data"></param>
        /// <param name="data_length"></param>
        /// <returns></returns>
        [BurstCompatible]
        static unsafe public bool GetDataArray(in BlastPackageData package, out NativeArray<float> data)
        {
            if (package.IsAllocated)
            {
                data = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<float>(
                    (void*)package.Data,
                    package.DataSize >> 2,
                    Allocator.None);
                return true;
            }
            else
            {
                data = default;
                return false;
            }
        }

#endif


        /// <summary>
        /// get a direct pointer to the data in the package 
        /// </summary>
        unsafe public bool GetDataPointer(out float* data, out int data_length)
        {
            return GetDataPointer(Package.Package, out data, out data_length);
        }

        /// <summary>
        /// get a direct pointer to the data in the package 
        /// </summary>
        [BurstCompatible]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static unsafe public bool GetDataPointer(in BlastPackageData package, out float* data, out int data_length)
        {
            if (package.IsAllocated)
            {
                data = package.Data;
                data_length = package.DataSize >> 2; // elementsize 4 floats 
                return true;
            }
            else
            {
                data = null;
                data_length = 0;
                return false; 
            }
        }

#endregion

#region SetData 

        /// <summary>
        /// set variable data from a collection of variable-name-value tuples
        /// </summary>
        /// <param name="values">collection of name-value tuples</param>
        public BlastScript SetData<T>(params Tuple<string, object>[] values) 
        {
            // ok if called with empty list 
            if (values == null || values.Length == 0) return this;

            if (!IsPackaged)
            {
                Debug.LogError($"BlastScript.Set(value-tuples): package invalid'");
                return null;
            }
            foreach (Tuple<string, object> val in values)
            {
                int index = GetVerifyDataIndex(val.Item1);
                if (index < 0)
                {
                    return null;
                }
                if (null == SetData(index, val.Item2))
                {
                    return null;
                }
            }
            return this; 
        }

        /// <summary>
        /// set variable data from a dictionary of name-value 
        /// </summary>
        /// <param name="values">collection of name-value tuples</param>
        public BlastScript SetData(Dictionary<string, object> values) 
        {
            // ok if called with empty list 
            if (values == null || values.Count == 0) return this;

            if (!IsPackaged)
            {
                Debug.LogError($"BlastScript.Set(value-tuples): package invalid'");
                return null;
            }
            foreach (KeyValuePair<string, object> val in values)
            {
                int index = GetVerifyDataIndex(val.Key);
                if (index < 0)
                {
                    return null;
                }
                if (null == SetData(index, val.Value))
                {
                    return null;
                }
            }
            return this;
        }

        /// <summary>
        /// set variable data from a dictionary of index-value
        /// </summary>
        /// <param name="values">collection of name-value tuples</param>
        public BlastScript SetData(Dictionary<int, object> values)
        {
            // ok if called with empty list 
            if (values == null || values.Count == 0) return this;

            if (!IsPackaged)
            {
                Debug.LogError($"BlastScript.SetData(value-tuples): package invalid | not prepared'");
                return null;
            }
            foreach (KeyValuePair<int, object> val in values)
            {
                if (val.Key < 0)
                {
                    return null;
                }
                if (null == SetData(val.Key, val.Value))
                {
                    return null;
                }
            }
            return this;
        }

        public BlastScript SetData(float[] values)
        {
            // ok if called with empty list 
            if (values == null || values.Length == 0) return this;

            if (!IsPackaged)
            {
                Debug.LogError($"BlastScript.SetData(float[]): package invalid | not prepared'");
                return null;
            }
            for(int i = 0; i < values.Length; i++)
            {
                if (null == SetData(i, values[i])) 
                {
                    return null;
                }
            }
            return this;
        }


        /// <summary>
        /// set data index from object 
        /// </summary>
        /// <param name="index">set data at index</param>
        /// <param name="obj">the object to read the data from</param>
        /// <returns></returns>
        public BlastScript SetData(int index, object obj)
        {
            switch (obj)
            {
                // floats
                case float f1:
                    if (null == SetData(index, f1)) return null;
                    break;
                case float2 f2:
                    if (null == SetData(index, f2)) return null;
                    break;
                case float3 f3:
                    if (null == SetData(index, f3)) return null;
                    break;
                case float4 f4:
                    if (null == SetData(index, f4)) return null;
                    break;

                // vectors 
                case UnityEngine.Vector2 v2:
                    if (null == SetData(index, v2)) return null;
                    break;
                case UnityEngine.Vector3 v3:
                    if (null == SetData(index, v3)) return null;
                    break;
                case UnityEngine.Vector4 v4:
                    if (null == SetData(index, v4)) return null;
                    break;

                // doubles 
                case double d1:
                    if (null == SetData(index, (float)d1)) return null;
                    break;
                case double2 d2:
                    if (null == SetData(index, (float2)d2)) return null;
                    break;
                case double3 d3:
                    if (null == SetData(index, (float3)d3)) return null;
                    break;
                case double4 d4:
                    if (null == SetData(index, (float4)d4)) return null;
                    break;

                // integers
                case int i1:
                    if (null == SetData(index, (float)i1)) return null;
                    break;
                case int2 i2:
                    if (null == SetData(index, (float2)i2)) return null;
                    break;
                case int3 i3:
                    if (null == SetData(index, (float3)i3)) return null;
                    break;
                case int4 i4:
                    if (null == SetData(index, (float4)i4)) return null;
                    break;

                case uint ui1:
                    if (null == SetData(index, (float)ui1)) return null;
                    break;
                case uint2 ui2:
                    if (null == SetData(index, (float2)ui2)) return null;
                    break;
                case uint3 ui3:
                    if (null == SetData(index, (float3)ui3)) return null;
                    break;
                case uint4 ui4:
                    if (null == SetData(index, (float4)ui4)) return null;
                    break;

                // bytes, shorts & longs 
                case byte b1:
                    if (null == SetData(index, (float)b1)) return null;
                    break;

                case short s1:
                    if (null == SetData(index, (float)s1)) return null;
                    break;

                case long l1:
                    if (null == SetData(index, (float)l1)) return null;
                    break;

                case ushort us1:
                    if (null == SetData(index, (float)us1)) return null;
                    break;

                case ulong ul1:
                    if (null == SetData(index, (float)ul1)) return null;
                    break;

                case sbyte sb1:
                    if (null == SetData(index, (float)sb1)) return null;
                    break;

                // halfs 
                case half h1:
                    if (null == SetData(index, (float)h1)) return null;
                    break;
                case half2 h2:
                    if (null == SetData(index, (float2)h2)) return null;
                    break;
                case half3 h3:
                    if (null == SetData(index, (float3)h3)) return null;
                    break;
                case half4 h4:
                    if (null == SetData(index, (float4)h4)) return null;
                    break;

                // decimals 
                case decimal dec1:
                    if (null == SetData(index, (float)dec1)) return null;
                    break;

                // booleans  
                case bool bo1:
                    if (null == SetData(index, math.select((float)0, (float)1, bo1))) return null;
                    break;
                case bool2 bo2:
                    if (null == SetData(index, math.select((float2)0, (float2)1, bo2))) return null;
                    break;
                case bool3 bo3:
                    if (null == SetData(index, math.select((float3)0, (float3)1, bo3))) return null;
                    break;
                case bool4 bo4:
                    if (null == SetData(index, math.select((float4)0, (float4)1, bo4))) return null;
                    break;
            }
            return this;
        }

        /// <summary>
        /// set variable data
        /// </summary>
        /// <param name="name">variable name</param>
        /// <param name="value">float value</param>
        /// <returns></returns>
        public BlastScript SetData(string name, float value)

        {
            int index = GetVerifyDataIndex(name, 1);
            if (index >= 0)
            {
                unsafe
                {
                    int offset = Package.VariableOffsets[index];
                    Package.Package.Data[offset] = value;
                }
                return this;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// set variable data
        /// </summary>
        /// <param name="index">variable index</param>
        /// <param name="value">float value</param>
        /// <returns></returns>
        public BlastScript SetData(int index, float value)
        {
            if (index >= 0 && index < Package.VariableOffsets.Length)
            {
                unsafe
                {
                    int offset = Package.VariableOffsets[index];
                    Package.Package.Data[offset] = value;
                }
                return this;
            }
            else
            {
                return null;
            }
        }


        /// <summary>
        /// set variable data
        /// </summary>
        /// <param name="index">variable index</param>
        /// <param name="value">float2 value</param>
        /// <returns></returns>
        public BlastScript SetData(int index, float2 value)
        {
            if (index >= 0)
            {
                unsafe
                {
                    int offset = Package.VariableOffsets[index];
                    Package.Package.Data[offset] = value.x;
                    Package.Package.Data[offset + 1] = value.y;
                }
                return this;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// set variable data
        /// </summary>
        /// <param name="index">variable index</param>
        /// <param name="value">float value</param>
        /// <returns></returns>
        public BlastScript SetData(int index, float3 value)

        {
            if (index >= 0)
            {
                unsafe
                {
                    int offset = Package.VariableOffsets[index];
                    Package.Package.Data[offset] = value.x;
                    Package.Package.Data[offset + 1] = value.y;
                    Package.Package.Data[offset + 2] = value.z;
                }
                return this;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// set variable data
        /// </summary>
        /// <param name="index">variable index</param>
        /// <param name="value">float4 value</param>
        /// <returns></returns>
        public BlastScript SetData(int index, float4 value)

        {
            if (index >= 0)
            {                                
                unsafe
                {
                    int offset = Package.VariableOffsets[index];
                    Package.Package.Data[offset] = value.x;
                    Package.Package.Data[offset + 1] = value.y;
                    Package.Package.Data[offset + 2] = value.z;
                    Package.Package.Data[offset + 3] = value.w; 
                }
                return this;
            }
            else
            {
                return null;
            }
        }


#region data verify helpers 
        /// <summary>
        /// get index for named variable data, verify its datatype and log errors on fail
        /// </summary>
        /// <param name="name">variable name</param>
        /// <param name="vector_size">vectorsize expected</param>
        /// <returns>-1 on failure, variable index otherwise</returns>
        protected int GetVerifyDataIndex(string name, int vector_size)
        {
            int index = GetVerifyDataIndex(name);
            if (index >= 0)
            {
                BlastVariable var = Package.Variables[index];
                if (var.DataType != BlastVariableDataType.Numeric
                    &&
                   var.VectorSize != vector_size)
                {
                    Debug.LogError($"BlastScript.Set(name, value): datatype mismatch setting variable '{name}', expecting {var.DataType}[{var.VectorSize}]");
                    return -1;
                }
            }
            return index; 
        }

        /// <summary>
        /// get index for named variable data
        /// </summary>
        /// <param name="name">variable name</param>
        /// <returns>-1 on failure, variable index otherwise</returns>
        protected int GetVerifyDataIndex(string name)
        {
            if (!IsPackaged)
            {
                Debug.LogError($"BlastScript.Set(name, value): package invalid'");
                return -1;
            }

            int index = GetVariableIndex(name, false);
            if (index < 0)
            {
                Debug.LogError($"BlastScript.Set(name, value): variable '{name}' not found in package");
                return -1;
            }

            return index;
        }

#endregion

#region typed SetData overloads 
        /// <summary>
        /// set variable data
        /// </summary>
        /// <param name="name">variable name</param>
        /// <param name="value">float2 value</param>
        /// <returns></returns>
        public BlastScript SetData(string name, float2 value)
        {
            int index = GetVerifyDataIndex(name, 2);
            if (index >= 0)
            {
                unsafe
                {
                    int offset = Package.VariableOffsets[index];
                    Package.Package.Data[offset] = value.x;
                    Package.Package.Data[offset + 1] = value.y;
                }
                return this;
            }
            else
            {
                return null; 
            }
        }

        /// <summary>
        /// set variable data
        /// </summary>
        /// <param name="name">variable name</param>
        /// <param name="value">float3 value</param>
        /// <returns></returns>
        public BlastScript SetData(string name, float3 value)
        {
            int index = GetVerifyDataIndex(name, 3);
            if (index >= 0)
            {
                unsafe
                {
                    int offset = Package.VariableOffsets[index];
                    Package.Package.Data[offset] = value.x;
                    Package.Package.Data[offset + 1] = value.y;
                    Package.Package.Data[offset + 2] = value.z;
                }
                return this;
            }
            else
            {
                return null;
            }
        } 

       
        /// <summary>
        /// set variable data
        /// </summary>
        /// <param name="name">variable name</param>
        /// <param name="value">float4 value</param>
        /// <returns></returns>
        public BlastScript SetData(string name, float4 value)
        {
            int index = GetVerifyDataIndex(name, 4);
            if (index >= 0)
            {
                unsafe
                {
                    int offset = Package.VariableOffsets[index];
                    Package.Package.Data[offset] = value.x;
                    Package.Package.Data[offset + 1] = value.y;
                    Package.Package.Data[offset + 2] = value.z;
                    Package.Package.Data[offset + 3] = value.w;
                }
                return this;
            }
            else
            {
                return null;
            }
        }

#endregion
#endregion

#region GetData

        /// <summary>
        /// get a data element by looking up its variable index by name first 
        /// </summary>
        public object GetData(string name)
        {
            Assert.IsTrue(IsPrepared, $"BlastScript.GetData: script {Id} {Name}, bytecode not packaged, run Prepare() first to compile its script package.");
            return GetData(GetVerifyDataIndex(name)); 
        }

        /// <summary>
        /// get data by index
        /// </summary>
        public object GetData(int index)
        {
            Assert.IsTrue(IsPrepared, $"BlastScript.GetData(index): script {Id} {Name}, bytecode not packaged, run Prepare() first to compile its script package.");
            Assert.IsTrue(index >= 0, $"BlastScript.GetData(index): script {Id} {Name}, data index: {index} not valid");
            switch (Package.Variables[index].VectorSize)

            {
                case 1: return GetFloat(index);
                case 2: return GetFloat2(index);
                case 3: return GetFloat3(index);
                case 4: return GetFloat4(index);
            }
            Assert.IsTrue(false, $"BlastScript.GetData(index): script {Id} {Name}, unhandled vectorsize");
            return null; 
        }


        /// <summary>
        /// get all data elements 
        /// </summary>
        /// <returns></returns>
        public object[] GetData()
        {
            int c = GetVariableCount();

            object[] r = new object[c]; 

            for(int i = 0; i < c; i++)
            {
                r[i] = GetData(i); 
            }

            return r; 
        }

#endregion

    }


    /// <summary>
    /// any script derived from this will be compiled during compilation/design 
    /// 
    /// public class BS1_compiletime : CompileTimeBlastScript
    /// {
    ///        public override string Code => @"a = a + 1;";
    /// }
    /// 
    /// </summary>
    public abstract class CompileTimeBlastScript : BlastScript
    {
    }

}
