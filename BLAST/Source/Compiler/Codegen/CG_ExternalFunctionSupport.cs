﻿//############################################################################################################################
// BLAST v1.0.4c                                                                                                             #
// Copyright © 2022 Rob Lemmens | NijnStein Software <rob.lemmens.s31 gmail com> All Rights Reserved                   ^__^\ #
// Unauthorized copying of this file, via any medium is strictly prohibited proprietary and confidential               (oo)\ #
//                                                                                                                     (__)  #
//############################################################################################################################
#if UNITY_EDITOR || STANDALONE_VSBUILD

using System;
using System.Collections.Generic;
using System.Text;

namespace NSS.Blast.Compiler.CodeGen
{
    static public class CG_ExternalFunctionSupport
    {
        public static string BlastSolutionFileName = "G:\\Repo\\Nijnstein\\BLAST V1\\Assets\\BLAST\\BLAST\\Source\\Interpretor\\codegen\\cg_ef_support.cs"; 
        public static string AdditionalNamespaceName = ".External"; 


        static string[] Usings = new string[]
        {
            "System",
            "Unity.Burst",
            "Unity.Mathematics"
        };

        /// <summary>
        /// BLAST licenseheader 
        /// </summary>
        public static string LicenseHeader = @"//###########################################################################################################################
// BLAST v1.0.4c - Copyright © 2022 Rob Lemmens | NijnStein Software <rob.lemmens.s31 gmail com> All Rights Reserved   ^__^\#
// Unauthorized copying of this file, via any medium is strictly prohibited proprietary and confidential               (oo)\#
//                                                                                                                     (__) #
//###########################################################################################################################";

        /// <summary>
        /// Layout for file to generate 
        /// </summary>
        public static string GenerationHeader = @"{0}

// *** This file has been generated by BLAST: modification is futile an will be assimilated ***

#if STANDALONE_VSBUILD
    using NSS.Blast.Standalone;
#else
    using UnityEngine;
    using Unity.Burst.CompilerServices;
#endif

{1}
     
#pragma warning disable CS1591

namespace NSS.Blast{2}
{{

    #region External Function Delegates
    {3}
    #endregion 

}}

namespace NSS.Blast
{{
    #region ScriptAPI registrations and function delegate enumeration
    
    public abstract partial class BlastScriptAPI
    {{
        {8}
    }}

    public enum EFDelegateType : byte
    {{
        {9}
    }}

    #endregion 
}}

namespace NSS.Blast.Interpretor
{{
    #region External Function Calls 

    unsafe public partial struct BlastInterpretor
    {{
        {5}

        {4}
    }}   

    #endregion 

}}

namespace NSS.Blast.SSMD
{{
    #region SSMD External Function Calls 

    unsafe public partial struct BlastSSMDInterpretor
    {{
        {7}

        {6}
    }}

    #endregion

}}

#pragma warning restore CS1591
";


        static string[] types = new string[]
        {
            "float", "float2", "float3", "float4", "Bool32",
            // "int", "int2", "int3", "int4", "half2", "half4"
        };

        static string[] shorts = new string[]
        {
            "f1", "f2", "f3", "f4", "b"
        };

        static int[] sizes = new int[]
        {
            1, 2, 3, 4, 1
        };





        /// <summary>
        /// merge namespace into a single document
        /// </summary>
        /// <param name="delegate_definitions"></param>
        /// <returns></returns>
        public static string CreateNamespace(string delegate_definitions, string normal_delegate_calls, string normal_call, string ssmd_delegate_calls, string ssmd_call, string api_registrations, string enums)
        {
            StringBuilder s = new StringBuilder();
            foreach(string u in Usings)
            {
                s.AppendLine($"using {u};"); 
            }
            return string.Format(
                GenerationHeader, 
                LicenseHeader, 
                s.ToString(), 
                AdditionalNamespaceName, 
                delegate_definitions,
                normal_delegate_calls,
                normal_call,
                ssmd_delegate_calls,
                ssmd_call, 
                api_registrations, 
                enums); 
        }


        static public string RunExternalFunctionDelegateCodeGen(BlastScriptAPI api)
        {
            StringBuilder delegates = new StringBuilder();
            StringBuilder calls = new StringBuilder();
            StringBuilder ssmd_calls = new StringBuilder();

            // create delegate definitions 
            foreach (ExternalFunctionProfile profile in api.ExternalFunctionProfiles)
            {
                delegates.Append(CreateDelegate(profile));
                if (profile.IsSSMD)
                {
                    ssmd_calls.Append(CreateDelegateCall(profile));
                }
                else
                {
                    calls.Append(CreateDelegateCall(profile));
                }
            }

            // create switch to call the efs
            string normal_call = CreateSwitch(api.ExternalFunctionProfiles, false);
            string ssmd_call = CreateSwitch(api.ExternalFunctionProfiles, true);

            string apiregs = CreateRegisterFunction(api.ExternalFunctionProfiles);
            string enums = CreateEnumeration(api.ExternalFunctionProfiles); 


            // create the cs file  
            return CreateNamespace(delegates.ToString(), calls.ToString(), normal_call, ssmd_calls.ToString(), ssmd_call, apiregs, enums);
        }


        static public string CreateSwitch(List<ExternalFunctionProfile> profiles, bool ssmd)
        {
            StringBuilder s = new StringBuilder();

            s.AppendLine("internal float4 CALL_EF(ref int code_pointer, in BlastScriptFunction function)");
            s.AppendLine("{");
            s.AppendLine("    int id = function.FunctionId;");
            s.AppendLine("    byte delegate_type = function.FunctionDelegateId;");
            s.AppendLine("");
            s.AppendLine("    switch(delegate_type)");
            s.AppendLine("    {");

            foreach(ExternalFunctionProfile profile in profiles)
            {
                if(profile.IsSSMD == ssmd)
                {
                    // generate name of function 
                    string functionname = GetCallFunctionName(profile);

                    // add case for profile and call delegate caller 
                    switch (profile.ReturnParameter)
                    {
                        case BlastVectorSizes.none:
                            s.AppendLine($"        case {profile.DelegateType}: {functionname}(ref code_pointer, id); return 0;");
                            break; 

                        case BlastVectorSizes.bool32:
                            s.AppendLine($"        case {profile.DelegateType}: return {functionname}(ref code_pointer, id).Single; ");
                            break;

                        case BlastVectorSizes.float2:
                            s.AppendLine($"        case {profile.DelegateType}: return new float4({functionname}(ref code_pointer, id), 0, 0);");
                            break;
                        case BlastVectorSizes.float3:
                            s.AppendLine($"        case {profile.DelegateType}: return new float4({functionname}(ref code_pointer, id), 0); ");
                            break;
                        case BlastVectorSizes.float1:
                        case BlastVectorSizes.float4:
                            s.AppendLine($"        case {profile.DelegateType}: return {functionname}(ref code_pointer, id); ");
                            break;
                    }
                }
            }

            s.AppendLine("    }");
            s.AppendLine("");
            s.AppendLine("    return float.NaN;");
            s.AppendLine("}");


            return s.ToString(); 
        }

        static public string CreateEnumeration(List<ExternalFunctionProfile> profiles)
        {
            StringBuilder sb = new StringBuilder();
            foreach(ExternalFunctionProfile profile in profiles)
            {
                sb.AppendLine(CreateEnumeration(profile)); 
            }

            return sb.ToString(); 
        }

        static public string CreateEnumeration(ExternalFunctionProfile profile)
        {
            return $"{GetCallFunctionName(profile)} = {profile.DelegateType},"; 
        }

        static public string CreateRegisterFunction(List<ExternalFunctionProfile> profiles)
        {
            StringBuilder sb = new StringBuilder();
            foreach (ExternalFunctionProfile profile in profiles)
            {
                sb.AppendLine(CreateRegisterFunction(profile));
            }

            return sb.ToString();
        }
        static public string CreateRegisterFunction(ExternalFunctionProfile profile)
        {
            string parameters = "";
            string postfix = "";
            string delegate_name = "";

            GetDelegateParameterInfo(profile, ref parameters, ref postfix, ref delegate_name);
            string shortname = profile.IsShortDefinition ? "_SHORT" : "";
            string returns = profile.ReturnsValue ? "_" + profile.ReturnParameter.ShortTypeName().ToUpper() : "";
            string ssmd = profile.IsSSMD ? "_SSMD" : "";

            string function_name = $"CALL_PROC_EF{shortname}{ssmd}{returns}{postfix}";
            string isshort = profile.IsShortDefinition ? "true" : "false";

            string enumname = "EFDelegateType." + function_name;

            return $"public int Register(External.{delegate_name} function, string name = null) {{ return RegisterFunction(function, name, {profile.ParameterCount}, {isshort}, (byte){enumname}); }}"; 
        }








        static public string GetCallFunctionName(ExternalFunctionProfile profile)
        {
            string parameters = "";
            string postfix = "";
            string delegate_name = "";

            GetDelegateParameterInfo(profile, ref parameters, ref postfix, ref delegate_name);

            string shortname = profile.IsShortDefinition ? "_SHORT" : "";
            string returns = profile.ReturnsValue ? "_" + profile.ReturnParameter.ShortTypeName().ToUpper() : "";
            string ssmd = profile.IsSSMD ? "_SSMD" : "";

            return $"CALL_PROC_EF{shortname}{ssmd}{returns}{postfix}";
        }

        static public string CreateDelegateCall(ExternalFunctionProfile profile)
        {
            StringBuilder s = StringBuilderCache.Acquire();

            string parameters = "";
            string postfix = "";
            string delegate_name = ""; 

            GetDelegateParameterInfo(profile, ref parameters, ref postfix, ref delegate_name);

            string shortname = profile.IsShortDefinition ? "_SHORT" : ""; 
            string returns = profile.ReturnsValue ? "_" + profile.ReturnParameter.ShortTypeName().ToUpper() : "";
            string ssmd = profile.IsSSMD ? "_SSMD" : "";
            string ptr = profile.IsSSMD ? "*" : "";

            s.AppendLine($"internal {profile.ReturnParameter.FullTypeName()}{ptr} CALL_PROC_EF{shortname}{ssmd}{returns}{postfix}(ref int code_pointer, in int function_id)");
            s.AppendLine("{");

            string delegate_parameters = ""; 
            if (profile.ParameterCount > 0)
            {
                // these are only needed if we have params 
                s.AppendLine("   byte vector_size;");
                s.AppendLine("   bool is_negated;");
                s.AppendLine("   BlastVariableDataType vtype;");
                s.AppendLine($"  void* vdata;");

                // pop each parameter
                for(int  i = 0; i < profile.ParameterCount; i++)
                {
                    if (profile.IsSSMD)
                    {

                    }
                    else
                    {
                        s.AppendLine($"   vdata = (void*)pop_p_info(ref code_pointer, out vtype, out vector_size, out is_negated);");
                    }
                    s.AppendLine("#if TRACE");
                    s.AppendLine($"   if (vector_size != {profile.ParameterType(i).VectorSize()}) goto CALLERROR;"); 
                    s.AppendLine("#endif");

                    switch (profile.ParameterType(i))
                    {
                        case BlastVectorSizes.float1: s.AppendLine($"   float p{(i + 1)} = math.select(((float*)vdata)[0], -((float*)vdata)[0], is_negated);"); break;
                        case BlastVectorSizes.float2: s.AppendLine($"   float2 p{(i + 1)} = math.select(((float2*)vdata)[0], -((float2*)vdata)[0], is_negated);"); break;
                        case BlastVectorSizes.float3: s.AppendLine($"   float3 p{(i + 1)} = math.select(((float3*)vdata)[0], -((float3*)vdata)[0], is_negated);"); break;
                        case BlastVectorSizes.float4: s.AppendLine($"   float4 p{(i + 1)} = math.select(((float4*)vdata)[0], -((float4*)vdata)[0], is_negated);"); break;
                        case BlastVectorSizes.bool32: s.AppendLine($"   Bool32 p{(i + 1)} = Bool32.From(math.select(((Bool32*)vdata)[0].Unsigned, ~((Bool32*)vdata)[0].Unsigned, is_negated));"); break;
                    }
                    if (i > 0) delegate_parameters += ", "; 
                    delegate_parameters += $"p{(i + 1)}";
                }
            }

            // result value 
            switch (profile.ReturnParameter)
            {
                case BlastVectorSizes.none: break;
                case BlastVectorSizes.float1: s.AppendLine($"   float{ptr} result;"); break; 
                case BlastVectorSizes.float2: s.AppendLine($"   float2{ptr} result;"); break;
                case BlastVectorSizes.float3: s.AppendLine($"   float3{ptr} result;"); break;
                case BlastVectorSizes.float4: s.AppendLine($"   float4{ptr} result;"); break;
                case BlastVectorSizes.bool32: s.AppendLine($"   Bool32{ptr} result;"); break; 
                case BlastVectorSizes.id1:    s.AppendLine($"   int{ptr} result;"); break;
                case BlastVectorSizes.id2:    s.AppendLine($"   int2{ptr} result;"); break;
                case BlastVectorSizes.id3:    s.AppendLine($"   int3{ptr} result;"); break;
                case BlastVectorSizes.id4:    s.AppendLine($"   int4{ptr} result;"); break;
                case BlastVectorSizes.id64:   s.AppendLine($"   long{ptr} result;"); break;
                case BlastVectorSizes.half2:  s.AppendLine($"   half2{ptr} result;"); break; 
                case BlastVectorSizes.half4:  s.AppendLine($"   half4{ptr} result;"); break; 
                case BlastVectorSizes.ptr:    s.AppendLine($"   void*{ptr} result;"); break;
            }

            // call external 
            string long_params = profile.IsShortDefinition ? "" : "(IntPtr)engine_ptr, environment_ptr, caller_ptr";
            if(!profile.IsShortDefinition && !string.IsNullOrWhiteSpace(delegate_parameters)) long_params += ", ";

            s.AppendLine("#if STANDALONE_VSBUILD");
            if (profile.ReturnsValue)
            {
                s.AppendLine($"   result = (Blast.Instance.API.FunctionInfo[function_id].FunctionDelegate as External.{delegate_name}).Invoke({long_params}{delegate_parameters});");
                s.AppendLine("   return result;");
            }
            else
            {
                s.AppendLine($"  (Blast.Instance.API.FunctionInfo[function_id].FunctionDelegate as External.{delegate_name}).Invoke({long_params}{delegate_parameters});");
                s.AppendLine("   return;");
            }
            

            s.AppendLine("#else");

            // burstcompatible native function pointer call 
            s.AppendLine($"   FunctionPointer<External.{delegate_name}> fp = engine_ptr->Functions[function_id].Generic<External.{delegate_name}>;");
            s.AppendLine("    if (fp.IsCreated && fp.Value != IntPtr.Zero)");
            s.AppendLine("    {");

            if (profile.ReturnsValue)
            {
                s.AppendLine($"       result = fp.Invoke({long_params}{delegate_parameters});");
                s.AppendLine("       return result;");
            }
            else
            {
                s.AppendLine($"       fp.Invoke({long_params}{delegate_parameters});");
                s.AppendLine("       return;");
            }
            s.AppendLine("    }");

            s.AppendLine("#endif");            


            // error catch|log
            if(profile.ParameterCount > 0) s.AppendLine("CALLERROR:");
            s.AppendLine("#if TRACE");
            s.AppendLine($"    Debug.LogError($\"Blast.interpretor.{delegate_name}: error calling external function with id {{function_id}}\");");
            s.AppendLine("#endif");

            // return return error value
            switch (profile.ReturnParameter)
            {
                case BlastVectorSizes.none:     s.AppendLine("   return;"); break;
                case BlastVectorSizes.float1: 
                case BlastVectorSizes.float2:
                case BlastVectorSizes.float3:
                case BlastVectorSizes.float4:   s.AppendLine("   return float.NaN;"); break;
                case BlastVectorSizes.bool32:   s.AppendLine("   return Bool32.FailPattern;"); break; 
                case BlastVectorSizes.id1:
                case BlastVectorSizes.id2:
                case BlastVectorSizes.id3:
                case BlastVectorSizes.id4:      s.AppendLine("   return 0;"); break;
                case BlastVectorSizes.id64:     s.AppendLine("   return 0;"); break;
                case BlastVectorSizes.half2:
                case BlastVectorSizes.half4:    s.AppendLine("   return new half(float.NaN);"); break;
                case BlastVectorSizes.ptr:      s.AppendLine("   return null;"); break; 
            }
              
            // done            
            s.AppendLine("}");

            return StringBuilderCache.GetStringAndRelease(ref s);
        }

        public static string CreateDelegate(ExternalFunctionProfile profile)
        {
            StringBuilder code = StringBuilderCache.Acquire();

            string returntype = profile.ReturnParameter.FullTypeName();
            string returntype_short = profile.ReturnParameter.ShortTypeName();


            string parameters = "";
            string postfix = "";
            string delegate_name = "";

            GetDelegateParameterInfo(profile, ref parameters, ref postfix, ref delegate_name);

            if (profile.IsShortDefinition)
            {
                code.Append($"public delegate {returntype} {delegate_name}(");
            }
            else
            if (profile.IsSSMD)
            {
                code.Append($"unsafe public delegate {returntype}* {delegate_name}(IntPtr engine, IntPtr data, int ssmd_datacount");
            }
            else
            {
                code.Append($"public delegate {returntype} {delegate_name}(IntPtr engine, IntPtr data, IntPtr caller");
            }

            code.Append(parameters);
            code.AppendLine(");");

            return StringBuilderCache.GetStringAndRelease(ref code);
        }

        private static void GetDelegateParameterInfo(ExternalFunctionProfile profile, ref string parameters, ref string postfix, ref string delegate_name)
        {
            if (profile.ParameterCount > 0)
            {
                for (int i = 0; i < profile.ParameterCount; i++)
                {
                    BlastVectorSizes psize = profile.ParameterType(i);
                    if (i >= 1 || !profile.IsShortDefinition)
                    {
                        parameters += ", ";
                    }

                    if (!profile.IsSSMD)
                    {
                        parameters += $"{psize.FullTypeName()} p{i}";
                    }
                    else
                    {
                        parameters += $"[NoAlias]{psize.FullTypeName()}* p{i}";
                    }
                    postfix += psize.ShortTypeName();
                }
            }
            if (!string.IsNullOrWhiteSpace(postfix)) postfix = "_" + postfix;

            string returns = profile.ReturnsValue ? "_" + profile.ReturnParameter.ShortTypeName().ToUpper() : "";

            delegate_name = string.Format("{0}{1}Delegate{2}{3}",
                profile.IsShortDefinition ? "BSEF" : "BlastEF",
                profile.IsSSMD ? "SSMD" : "",
                returns,
                postfix);
        }



        // we generate a list of functionprofiles that we allow

        // - max = 256 items function|delegate types 

        // we generate all delegates and call functions for each profile in config 

        // generate an enum with all profile names <> ids

        //   [returntype] name[parameters + types] 


        // then make a giant switch case for it on the id
        //  - that cals the generated delegate caller 


        // then we could eventually in unity:
        // - create an editor
        // - allow to configure external function prototypes 
        // - allow to regenerate function delegates accoring to user specifications 



    }

}


#endif 