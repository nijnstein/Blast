//############################################################################################################################
//                                                                                                                           #
//  ██████╗ ██╗      █████╗ ███████╗████████╗                           Copyright © 2022 Rob Lemmens | NijnStein Software    #
//  ██╔══██╗██║     ██╔══██╗██╔════╝╚══██╔══╝                                    <rob.lemmens.s31 gmail com>                 #
//  ██████╔╝██║     ███████║███████╗   ██║                                           All Rights Reserved                     #
//  ██╔══██╗██║     ██╔══██║╚════██║   ██║                                                                                   #
//  ██████╔╝███████╗██║  ██║███████║   ██║     V1.0.4e                                                                       #
//  ╚═════╝ ╚══════╝╚═╝  ╚═╝╚══════╝   ╚═╝                                                                                   #
//                                                                                                                           #
//############################################################################################################################
//                                                                                                                     (__)  #
//       Unauthorized copying of this file, via any medium is strictly prohibited proprietary and confidential         (oo)  #
//                                                                                                                     (__)  #
//############################################################################################################################
#pragma warning disable CS1591
#pragma warning disable CS0162

//
// Warning: spaghetti ahead 
//


#if UNITY_EDITOR || STANDALONE_VSBUILD

using System;
using System.Collections.Generic;
using System.Text;

namespace NSS.Blast.Compiler.CodeGen
{

    /// <summary>
    /// 
    /// Only run this on mayor changes as it requires manual touches (useless to spend time on this too much)
    /// 
    /// </summary>

    static public class CG_NormalFunctionHandlers
    {

        public static string BlastSolutionFileName = "G:\\Repo\\Nijnstein\\BLAST V1\\Assets\\BLAST\\BLAST\\Source\\Interpretor\\codegen\\cg_normal_sp.cs";
        public static string DualSolutionFileName = "G:\\Repo\\Nijnstein\\BLAST V1\\Assets\\BLAST\\BLAST\\Source\\Interpretor\\codegen\\cg_normal_2p.cs";



        /// <summary>
        /// 0 = function name: ABS
        /// 1 = function call: math.abs
        /// </summary>

        static string single_template = @"

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void CALL_FN_{0}(ref int code_pointer, ref V4 v4, out byte vector_size, out BlastVariableDataType datatype)
        {{
            code_pointer += 1;
            void* fdata = (void*)pop_p_info(ref code_pointer, out datatype, out vector_size, out bool is_negated);

            switch (datatype.Combine(vector_size, is_negated))
            {{

                case BlastVectorType.float1: v4{2}     =  {1}(((float*)fdata)[0]){7};  break;
                case BlastVectorType.float2: v4{3}    =  {1}(((float2*)fdata)[0]){7}; break;
                case BlastVectorType.float3: v4{4}   =  {1}(((float3*)fdata)[0]){7}; break;
                case BlastVectorType.float4: v4{5}       =  {1}(((float4*)fdata)[0]){7}; break;

                case BlastVectorType.int1: v4{2}       =  {1}(((int*)fdata)[0]){7};    break;
                case BlastVectorType.int2: v4{3}      =  {1}(((int2*)fdata)[0]){7};   break;
                case BlastVectorType.int3: v4{4}     =  {1}(((int3*)fdata)[0]){7};   break;
                case BlastVectorType.int4: v4{5}         =  {1}(((int4*)fdata)[0]){7};   break;

                case BlastVectorType.float1_n: v4{2}   = {1}(-((float*)fdata)[0]){7};  break;
                case BlastVectorType.float2_n: v4{3}  = {1}(-((float2*)fdata)[0]){7}; break;
                case BlastVectorType.float3_n: v4{4} = {1}(-((float3*)fdata)[0]){7}; break;
                case BlastVectorType.float4_n: v4{5}     = {1}(-((float4*)fdata)[0]){7}; break;

                case BlastVectorType.int1_n: v4{2}     = {1}(-((int*)fdata)[0]){7};    break;
                case BlastVectorType.int2_n: v4{3}    = {1}(-((int2*)fdata)[0]){7};   break;
                case BlastVectorType.int3_n: v4{4}   = {1}(-((int3*)fdata)[0]){7};   break;
                case BlastVectorType.int4_n: v4{5}       = {1}(-((int4*)fdata)[0]){7};   break;

#if DEVELOPMENT_BUILD || TRACE
                default:
                    {{
                        Debug.LogError($""Blast.Interpretor.{0}: vectorsize {{vector_size}}, datatype {{datatype}} not supported"");
                        break;
                    }}
#endif
            }}
            {6}            
        }}
";
        static string single_template_1 = @"

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void CALL_FN_{0}(ref int code_pointer, ref V4 v4, out byte vector_size, out BlastVariableDataType datatype)
        {{
            code_pointer += 1;
            void* fdata = (void*)pop_p_info(ref code_pointer, out datatype, out vector_size, out bool is_negated);

            switch (datatype.Combine(vector_size, is_negated))
            {{

                case BlastVectorType.float1: v4{2}     =  {1}(((float*)fdata)[0]){7};  break;

                case BlastVectorType.int1: v4{2}       =  {1}(((int*)fdata)[0]){7};    break;

                case BlastVectorType.float1_n: v4{2}   = {1}(-((float*)fdata)[0]){7};  break;

                case BlastVectorType.int1_n: v4{2}     = {1}(-((int*)fdata)[0]){7};    break;

#if DEVELOPMENT_BUILD || TRACE
                default:
                    {{
                        Debug.LogError($""Blast.Interpretor.{0}: vectorsize {{vector_size}}, datatype {{datatype}} not supported"");
                        break;
                    }}
#endif
            }}
            {6}            
        }}
";
        static string single_template_4 = @"

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void CALL_FN_{0}(ref int code_pointer, ref V4 v4, out byte vector_size, out BlastVariableDataType datatype)
        {{
            code_pointer += 1;
            void* fdata = (void*)pop_p_info(ref code_pointer, out datatype, out vector_size, out bool is_negated);

            switch (datatype.Combine(vector_size, is_negated))
            {{

                case BlastVectorType.float4: v4{5}       =  {1}(((float4*)fdata)[0]){7}; break;
                case BlastVectorType.int4: v4{5}         =  {1}(((int4*)fdata)[0]){7};   break;
                case BlastVectorType.float4_n: v4{5}     = {1}(-((float4*)fdata)[0]){7}; break;
                case BlastVectorType.int4_n: v4{5}       = {1}(-((int4*)fdata)[0]){7};   break;

#if DEVELOPMENT_BUILD || TRACE
                default:
                    {{
                        Debug.LogError($""Blast.Interpretor.{0}: vectorsize {{vector_size}}, datatype {{datatype}} not supported"");
                        break;
                    }}
#endif
            }}
            {6}            
        }}
";

        static string single_template_int = @"

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void CALL_FN_{0}(ref int code_pointer, ref V4 v4, out byte vector_size, out BlastVariableDataType datatype)
        {{
            code_pointer += 1;
            void* fdata = (void*)pop_p_info(ref code_pointer, out datatype, out vector_size, out bool is_negated);

            switch (datatype.Combine(vector_size, is_negated))
            {{
                case BlastVectorType.int1: v4{2}       =  {1}(((int*)fdata)[0]){7};    break;
                case BlastVectorType.int2: v4{3}      =  {1}(((int2*)fdata)[0]){7};   break;
                case BlastVectorType.int3: v4{4}     =  {1}(((int3*)fdata)[0]){7};   break;
                case BlastVectorType.int4: v4{5}         =  {1}(((int4*)fdata)[0]){7};   break;

                case BlastVectorType.int1_n: v4{2}     = {1}(-((int*)fdata)[0]){7};    break;
                case BlastVectorType.int2_n: v4{3}    = {1}(-((int2*)fdata)[0]){7};   break;
                case BlastVectorType.int3_n: v4{4}   = {1}(-((int3*)fdata)[0]){7};   break;
                case BlastVectorType.int4_n: v4{5}       = {1}(-((int4*)fdata)[0]){7};   break;

#if DEVELOPMENT_BUILD || TRACE
                default:
                    {{
                        Debug.LogError($""Blast.Interpretor.{0}: vectorsize {{vector_size}}, datatype {{datatype}} not supported"");
                        break;
                    }}
#endif
            }}
            {6}            
        }}
";


        static string single_template_234 = @"

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void CALL_FN_{0}(ref int code_pointer, ref V4 v4, out byte vector_size, out BlastVariableDataType datatype)
        {{
            bool negated;
            code_pointer += 1;
            void* fdata = (void*)pop_p_info(ref code_pointer, out datatype, out vector_size, out bool is_negated);

            switch (datatype.Combine(vector_size, is_negated))
            {{

                case BlastVectorType.float2: v4{3}    =  {1}(((float2*)fdata)[0]){7}; break;
                case BlastVectorType.float3: v4{4}   =  {1}(((float3*)fdata)[0]){7}; break;
                case BlastVectorType.float4: v4{5}       =  {1}(((float4*)fdata)[0]){7}; break;

                case BlastVectorType.int2: v4{3}      =  {1}(((int2*)fdata)[0]){7};   break;
                case BlastVectorType.int3: v4{4}     =  {1}(((int3*)fdata)[0]){7};   break;
                case BlastVectorType.int4: v4{5}         =  {1}(((int4*)fdata)[0]){7};   break;

                case BlastVectorType.float2_n: v4{3}  = {1}(-((float2*)fdata)[0]){7}; break;
                case BlastVectorType.float3_n: v4{4} = {1}(-((float3*)fdata)[0]){7}; break;
                case BlastVectorType.float4_n: v4{5}     = {1}(-((float4*)fdata)[0]){7}; break;

                case BlastVectorType.int2_n: v4{3}    = {1}(-((int2*)fdata)[0]){7};   break;
                case BlastVectorType.int3_n: v4{4}   = {1}(-((int3*)fdata)[0]){7};   break;
                case BlastVectorType.int4_n: v4{5}       = {1}(-((int4*)fdata)[0]){7};   break;

#if DEVELOPMENT_BUILD || TRACE
                default:
                    {{
                        Debug.LogError($""Blast.Interpretor.{0}: vectorsize {{vector_size}}, datatype {{datatype}} not supported"");
                        break;
                    }}
#endif
            }}
            {6}            
        }}
";


        static string dual_template = @"
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void CALL_FN_{0}(ref int code_pointer, ref V4 v4, out byte vector_size, out BlastVariableDataType datatype)
        {{
            code_pointer += 1;
            void* fdata = (void*)pop_p_info(ref code_pointer, out datatype, out vector_size, out bool is_negated);

            switch (datatype.Combine(vector_size, is_negated))
            {{

                case BlastVectorType.float1: v4{2}       = {1}(((float*)fdata)[0], pop_as_f1(ref code_pointer)){7}; break;
                case BlastVectorType.float2: v4{3}      = {1}(((float2*)fdata)[0], pop_as_f2(ref code_pointer)){7}; break;
                case BlastVectorType.float3: v4{4}     = {1}(((float3*)fdata)[0], pop_as_f3(ref code_pointer)){7}; break;
                case BlastVectorType.float4: v4{5}         = {1}(((float4*)fdata)[0], pop_as_f4(ref code_pointer)){7}; break;

                case BlastVectorType.int1: v4{2}         = {1}(((int*)fdata)[0], pop_as_i1(ref code_pointer)){7}; break;
                case BlastVectorType.int2: v4{3}        = {1}(((int2*)fdata)[0], pop_as_i1(ref code_pointer)){7}; break;
                case BlastVectorType.int3: v4{4}       = {1}(((int3*)fdata)[0], pop_as_i1(ref code_pointer)){7}; break;
                case BlastVectorType.int4: v4{5}           = {1}(((int4*)fdata)[0], pop_as_i1(ref code_pointer)){7}; break;

                case BlastVectorType.float1_n: v4{2}     = {1}(-((float*)fdata)[0], pop_as_f1(ref code_pointer)){7}; break;
                case BlastVectorType.float2_n: v4{3}    = {1}(-((float2*)fdata)[0], pop_as_f2(ref code_pointer)){7}; break;
                case BlastVectorType.float3_n: v4{4}   = {1}(-((float3*)fdata)[0], pop_as_f3(ref code_pointer)){7}; break;
                case BlastVectorType.float4_n: v4{5}       = {1}(-((float4*)fdata)[0], pop_as_f4(ref code_pointer)){7}; break;

                case BlastVectorType.int1_n: v4{2}       = {1}(-((int*)fdata)[0], pop_as_i1(ref code_pointer)){7}; break;
                case BlastVectorType.int2_n: v4{3}      = {1}(-((int2*)fdata)[0], pop_as_i2(ref code_pointer)){7}; break;
                case BlastVectorType.int3_n: v4{4}     = {1}(-((int3*)fdata)[0], pop_as_i3(ref code_pointer)){7}; break;
                case BlastVectorType.int4_n: v4{5}         = {1}(-((int4*)fdata)[0], pop_as_i4(ref code_pointer)){7}; break;

#if DEVELOPMENT_BUILD || TRACE
                default:
                    {{
                        Debug.LogError($""Blast.Interpretor.{0}: vectorsize {{vector_size}}, datatype {{datatype}} not supported"");
                        break;
                    }}
#endif
            }}
            {6}            
        }}
";
        static string dual_template_int = @"
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void CALL_FN_{0}(ref int code_pointer, ref V4 v4, out byte vector_size, out BlastVariableDataType datatype)
        {{
            code_pointer += 1;
            void* fdata = (void*)pop_p_info(ref code_pointer, out datatype, out vector_size, out bool is_negated);

            switch (datatype.Combine(vector_size, is_negated))
            {{

                case BlastVectorType.int1: v4{2}         = {1}(((int*)fdata)[0], pop_as_i1(ref code_pointer)){7}; break;
                case BlastVectorType.int2: v4{3}        = {1}(((int2*)fdata)[0], pop_as_i1(ref code_pointer)){7}; break;
                case BlastVectorType.int3: v4{4}       = {1}(((int3*)fdata)[0], pop_as_i1(ref code_pointer)){7}; break;
                case BlastVectorType.int4: v4{5}           = {1}(((int4*)fdata)[0], pop_as_i1(ref code_pointer)){7}; break;

                case BlastVectorType.int1_n: v4{2}       = {1}(-((int*)fdata)[0], pop_as_i1(ref code_pointer)){7}; break;
                case BlastVectorType.int2_n: v4{3}      = {1}(-((int2*)fdata)[0], pop_as_i2(ref code_pointer)){7}; break;
                case BlastVectorType.int3_n: v4{4}     = {1}(-((int3*)fdata)[0], pop_as_i3(ref code_pointer)){7}; break;
                case BlastVectorType.int4_n: v4{5}         = {1}(-((int4*)fdata)[0], pop_as_i4(ref code_pointer)){7}; break;

#if DEVELOPMENT_BUILD || TRACE
                default:
                    {{
                        Debug.LogError($""Blast.Interpretor.{0}: vectorsize {{vector_size}}, datatype {{datatype}} not supported"");
                        break;
                    }}
#endif
            }}
            {6}            
        }}
";

        static string dual_template_234 = @"
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void CALL_FN_{0}(ref int code_pointer, ref V4 v4, out byte vector_size, out BlastVariableDataType datatype)
        {{
            code_pointer += 1;
            void* fdata = (void*)pop_p_info(ref code_pointer, out datatype, out vector_size, out bool is_negated);

            switch (datatype.Combine(vector_size, is_negated))
            {{

                case BlastVectorType.float2: v4{3}      = {1}(((float2*)fdata)[0], pop_as_f2(ref code_pointer)){7}; break;
                case BlastVectorType.float3: v4{4}     = {1}(((float3*)fdata)[0], pop_as_f3(ref code_pointer)){7}; break;
                case BlastVectorType.float4: v4{5}         = {1}(((float4*)fdata)[0], pop_as_f4(ref code_pointer)){7}; break;

                case BlastVectorType.int2: v4{3}        = {1}(((int2*)fdata)[0], pop_as_i1(ref code_pointer)){7}; break;
                case BlastVectorType.int3: v4{4}       = {1}(((int3*)fdata)[0], pop_as_i1(ref code_pointer)){7}; break;
                case BlastVectorType.int4: v4{5}           = {1}(((int4*)fdata)[0], pop_as_i1(ref code_pointer)){7}; break;

                case BlastVectorType.float2_n: v4{3}    = {1}(-((float2*)fdata)[0], pop_as_f2(ref code_pointer)){7}; break;
                case BlastVectorType.float3_n: v4{4}   = {1}(-((float3*)fdata)[0], pop_as_f3(ref code_pointer)){7}; break;
                case BlastVectorType.float4_n: v4{5}       = {1}(-((float4*)fdata)[0], pop_as_f4(ref code_pointer)){7}; break;

                case BlastVectorType.int2_n: v4{3}      = {1}(-((int2*)fdata)[0], pop_as_i2(ref code_pointer)){7}; break;
                case BlastVectorType.int3_n: v4{4}     = {1}(-((int3*)fdata)[0], pop_as_i3(ref code_pointer)){7}; break;
                case BlastVectorType.int4_n: v4{5}         = {1}(-((int4*)fdata)[0], pop_as_i4(ref code_pointer)){7}; break;

#if DEVELOPMENT_BUILD || TRACE
                default:
                    {{
                        Debug.LogError($""Blast.Interpretor.{0}: vectorsize {{vector_size}}, datatype {{datatype}} not supported"");
                        break;
                    }}
#endif
            }}
            {6}            
        }}
";
        static string dual_template_1 = @"
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void CALL_FN_{0}(ref int code_pointer, ref V4 v4, out byte vector_size, out BlastVariableDataType datatype)
        {{
            code_pointer += 1;
            void* fdata = (void*)pop_p_info(ref code_pointer, out datatype, out vector_size, out bool is_negated);

            switch (datatype.Combine(vector_size, is_negated))
            {{

                case BlastVectorType.float1: v4{2}       = {1}(((float*)fdata)[0], pop_as_f1(ref code_pointer)){7}; break;
                case BlastVectorType.int1: v4{2}         = {1}(((int*)fdata)[0], pop_as_i1(ref code_pointer)){7}; break;
                case BlastVectorType.float1_n: v4{2}     = {1}(-((float*)fdata)[0], pop_as_f1(ref code_pointer)){7}; break;
                case BlastVectorType.int1_n: v4{2}       = {1}(-((int*)fdata)[0], pop_as_i1(ref code_pointer)){7}; break;

#if DEVELOPMENT_BUILD || TRACE
                default:
                    {{
                        Debug.LogError($""Blast.Interpretor.{0}: vectorsize {{vector_size}}, datatype {{datatype}} not supported"");
                        break;
                    }}
#endif
            }}
            {6}            
        }}
";

        static string template_start = @"

// *** This file has been generated by BLAST: modification is futile an will be assimilated ***

#if STANDALONE_VSBUILD
    using NSS.Blast.Standalone;
    using System.Reflection;
#else
    using UnityEngine;
    using Unity.Burst.CompilerServices;
#endif


using Unity.Burst;
using Unity.Mathematics;
using System.Runtime.CompilerServices;

                                    
using V1 = System.Single;
using V2 = Unity.Mathematics.float2;
using V3 = Unity.Mathematics.float3;
using V4 = Unity.Mathematics.float4;


namespace NSS.Blast.Interpretor
{
    unsafe public partial struct BlastInterpretor
    {
";

        static string template_end = @"
    }
}
";



        static string GenSingleFunction(string function_name, string function_call, string allowed_returns, string allowed_params)
        {
            if (allowed_params == "1")
            {
                return GenFunction(single_template_1, function_name, function_call, allowed_returns, allowed_params);
            }
            else
            if (allowed_params == "4")
            {
                return GenFunction(single_template_4, function_name, function_call, allowed_returns, allowed_params);
            }
            else
            if (allowed_params == "234")
            {
                return GenFunction(single_template_234, function_name, function_call, allowed_returns, allowed_params);
            }
            else
            if (allowed_params == "int")
            {
                return GenFunction(single_template_int, function_name, function_call, allowed_returns, allowed_params);
            }
            else
            {
                return GenFunction(single_template, function_name, function_call, allowed_returns, allowed_params);
            }
        }

        static string GenDualFunction(string function_name, string function_call, string allowed_returns, string allowed_params)
        {
            if (allowed_params == "1")
            {
                return GenFunction(dual_template_1, function_name, function_call, allowed_returns, allowed_params);
            }
            else
            if (allowed_params == "234")
            {
                return GenFunction(dual_template_234, function_name, function_call, allowed_returns, allowed_params);
            }
            else
            if (allowed_params == "int")
            {
                return GenFunction(dual_template_int, function_name, function_call, allowed_returns, allowed_params);
            }
            else
            {
                return GenFunction(dual_template, function_name, function_call, allowed_returns, allowed_params);
            }
        }

        static string GenFunction(string template, string function_name, string function_call, string allowed_returns, string allowed_params)
        {
            string x = ".x";
            string xy = ".xy";
            string xyz = ".xyz";
            string xyzw = "";
            string vsize = "";
            string val = "";
            if (allowed_returns == "1") // only option for now .. 
            {
                x = ".x";
                xy = ".x";
                xyz = ".x";
                xyzw = ".x";
                vsize = "vector_size = 1;";
            }
            else
            if(allowed_returns == "q4")
            {
                x = "";
                xy = "";
                xyz = "";
                xyzw = "";
                vsize = "vector_size = 4;";
                val = ".value";
            }
            return string.Format(template, function_name, function_call, x, xy, xyz, xyzw, vsize, val);
        }


        static string[] singles = new string[] {
            "ABS", "math.abs", "abs", "none", "", "",
            "TRUNC", "math.trunc", "trunc", "none", "", "",
            "EXP", "math.exp", "ex_op", "exp", "", "",
            "EXP10", "math.exp10", "ex_op", "exp10", "", "",
            "LOG10", "math.log10", "ex_op", "log10", "", "",
            "LOGN", "math.log", "ex_op", "logn", "", "",
            "LOG2", "math.log2", "ex_op", "log2", "", "",
            "SQRT", "math.sqrt", "ex_op", "sqrt", "", "",
            "RSQRT", "math.rsqrt", "ex_op", "rsqrt", "", "",
            "SIN", "math.sin", "ex_op", "sin", "", "",
            "COS", "math.cos", "ex_op", "cos", "", "",
            "TAN", "math.tan", "ex_op", "tan", "", "",
            "ATAN", "math.atan", "ex_op", "atan", "", "",
            "SINH", "math.sinh", "ex_op", "sinh", "", "",
            "COSH", "math.cosh", "ex_op", "cosh", "", "",
            "DEGREES", "math.degrees", "ex_op", "degrees", "", "",
            "RADIANS", "math.radians", "ex_op", "radians", "", "",
            "SATURATE", "math.saturate", "ex_op", "saturate", "", "",
            "NORMALIZE", "math.normalize", "ex_op", "normalize", "", "234",    // not all vectorsizes > only > 1
            "CEIL", "math.ceil", "ex_op", "ceil", "", "",
            "FLOOR", "math.floor", "ex_op", "floor", "", "",
            "FRAC", "math.frac", "ex_op", "frac", "", "",
            "CEILLOG2", "math.ceillog2", "ex_op", "ceillog2", "", "int",       // only int 
            "CEILPOW2", "math.ceilpow2", "ex_op", "ceilpow2", "", "int",       // only int 
            "FLOORLOG2", "math.floorlog2", "ex_op", "floorlog2", "", "int",     // only int 
            // new v1.0.5
            "SIGN", "math.sign", "ex_op", "sign", "", "",
            "LENGTH", "math.length", "ex_op", "length", "1", "",            // forces returnsize to size 1 
            "LENGTHSQ", "math.lengthsq", "ex_op", "lengthsq",  "1", "",     // forces returnsize 1 
            "SQUARE", "mathex.square", "ex_op", "square", "", "",

            "ROTATEX", "quaternion.RotateX", "ex_op", "rotatex", "q4", "1",         // forced quaternoin -> float4 , only v1 input
            "ROTATEY", "quaternion.RotateY", "ex_op", "rotatey", "q4", "1",         // forced quaternoin -> float4 
            "ROTATEZ", "quaternion.RotateZ", "ex_op", "rotatez", "q4", "1",         // forced quaternoin -> float4 

            "CONJUGATE", "math.conjugate", "ex_op", "conjugate", "q4", "4",
            "INVERSE", "math.inverse", "ex_op", "inverse", "q4", "4",
        };

        static string[] duals = new string[]
        {
            "POW", "math.pow", "ex_op", "pow", "", "",
            "FMOD", "math.fmod", "ex_op", "fmod", "", "",
            "ATAN2", "math.atan2", "ex_op", "atan2",  "", "",

            // new v1.0.5
            "DISTANCE", "math.distance", "ex_op", "distance", "1", "",         // forces returnsize 1 
            "DISTANCESQ", "math.distancesq", "ex_op", "distancesq",  "1", "",  // forces returnsize 1 

            "REFLECT", "math.reflect", "ex_op", "reflect", "", "234",           // no vector 1 
            "PROJECT", "math.project", "ex_op", "project",  "", "234",           // no vector 1 

            "LOOKROTATION", "quaternion.LookRotation", "ex_op", "lookrotation", "q4", "",
            "LOOKROTATIONSAFE", "quaternion.LookRotationSafe", "ex_op", "lookrotationsafe", "q4", "", // limit to vector3 innput manually.. 
        };

        // tripples to aadd:  smoothstep, step, refract, faceforward (no vector1), sincos 

        // zero param value returns : up down etc. 

   //     /// <summary>
   //     /// Unity's up axis (0, 1, 0).
   //     /// </summary>
   //     /// <remarks>Matches [https://docs.unity3d.com/ScriptReference/Vector3-up.html](https://docs.unity3d.com/ScriptReference/Vector3-up.html)</remarks>
   //     /// <returns>The up axis.</returns>
   //     [MethodImpl(MethodImplOptions.AggressiveInlining)]
   //     public static float3 up()
   //     {
   //         return new float3(0.0f, 1.0f, 0.0f);
   //     }  // for compatibility
   //
   //     /// <summary>
   //     /// Unity's down axis (0, -1, 0).
   //     /// </summary>
   //     /// <remarks>Matches [https://docs.unity3d.com/ScriptReference/Vector3-down.html](https://docs.unity3d.com/ScriptReference/Vector3-down.html)</remarks>
   //     /// <returns>The down axis.</returns>
   //     [MethodImpl(MethodImplOptions.AggressiveInlining)]
   //     public static float3 down()
   //     {
   //         return new float3(0.0f, -1.0f, 0.0f);
   //     }
   //
   //     /// <summary>
   //     /// Unity's forward axis (0, 0, 1).
   //     /// </summary>
   //     /// <remarks>Matches [https://docs.unity3d.com/ScriptReference/Vector3-forward.html](https://docs.unity3d.com/ScriptReference/Vector3-forward.html)</remarks>
   //     /// <returns>The forward axis.</returns>
   //     [MethodImpl(MethodImplOptions.AggressiveInlining)]
   //     public static float3 forward()
   //     {
   //         return new float3(0.0f, 0.0f, 1.0f);
   //     }
   //
   //     /// <summary>
   //     /// Unity's back axis (0, 0, -1).
   //     /// </summary>
   //     /// <remarks>Matches [https://docs.unity3d.com/ScriptReference/Vector3-back.html](https://docs.unity3d.com/ScriptReference/Vector3-back.html)</remarks>
   //     /// <returns>The back axis.</returns>
   //     [MethodImpl(MethodImplOptions.AggressiveInlining)]
   //     public static float3 back()
   //     {
   //         return new float3(0.0f, 0.0f, -1.0f);
   //     }
   //
   //     /// <summary>
   //     /// Unity's left axis (-1, 0, 0).
   //     /// </summary>
   //     /// <remarks>Matches [https://docs.unity3d.com/ScriptReference/Vector3-left.html](https://docs.unity3d.com/ScriptReference/Vector3-left.html)</remarks>
   //     /// <returns>The left axis.</returns>
   //     [MethodImpl(MethodImplOptions.AggressiveInlining)]
   //     public static float3 left()
   //     {
   //         return new float3(-1.0f, 0.0f, 0.0f);
   //     }
   //
   //     /// <summary>
   //     /// Unity's right axis (1, 0, 0).
   //     /// </summary>
   //     /// <remarks>Matches [https://docs.unity3d.com/ScriptReference/Vector3-right.html](https://docs.unity3d.com/ScriptReference/Vector3-right.html)</remarks>
   //     /// <returns>The right axis.</returns>
   //     [MethodImpl(MethodImplOptions.AggressiveInlining)]
   //     public static float3 right()
   //     {
   //         return new float3(1.0f, 0.0f, 0.0f);
   //     }
   //


        static public string GenerateSingles()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(CG_ExternalFunctionSupport.LicenseHeader);
            sb.AppendLine(template_start);                           
            for (int i = 0; i < singles.Length; i += 6)
            {
                sb.AppendLine(GenSingleFunction(singles[i], singles[i + 1], singles[i + 4], singles[i + 5]));
            }

            sb.AppendLine(template_end);
            return sb.ToString(); 
        }


        static public string GenerateDuals()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(CG_ExternalFunctionSupport.LicenseHeader);
            sb.AppendLine(template_start);
            for (int i = 0; i < duals.Length; i += 6)
            {
                sb.AppendLine(GenDualFunction(duals[i], duals[i + 1], duals[i + 4], duals[i + 5]));
            }

            sb.AppendLine(template_end);
            return sb.ToString();
        }






    }
}



#endif