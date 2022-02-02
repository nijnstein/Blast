//##########################################################################################################
// Copyright © 2022 Rob Lemmens | NijnStein Software <rob.lemmens.s31@gmail.com> All Rights Reserved       #
// Unauthorized copying of this file, via any medium is strictly prohibited                                #
// Proprietary and confidential                                                                            #
//##########################################################################################################
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Rendering;

namespace NSS.Blast.Cache
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member 'BSExecuteDelegate'
    unsafe public delegate void BSExecuteDelegate(IntPtr engine, float* variables);
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member 'BSExecuteDelegate'

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member 'IBlastHPCScriptJob'
    public interface IBlastHPCScriptJob
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member 'IBlastHPCScriptJob'
    {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member 'IBlastHPCScriptJob.ScriptId'
        int ScriptId { get; }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member 'IBlastHPCScriptJob.ScriptId'

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member 'IBlastHPCScriptJob.GetBSD()'
        FunctionPointer<BSExecuteDelegate> GetBSD();
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member 'IBlastHPCScriptJob.GetBSD()'
    }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member 'blast'
    public struct blast
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member 'blast'
    {


        /// <summary>
        /// hmmmmz we would neeed very many overloads when supporting 63 parameters... 
        /// </summary>
        static float max(float a, float b)
        {
            return a > b ? a : b;
        }
        static float max(float a, float b, float c)
        {
            a = a > b ? a : b;
            a = a > c ? a : c;
            return a;
        }
        static float max(float a, float b, float c, float d)
        {
            a = a > b ? a : b;
            c = c > d ? c : d;
            a = a > c ? a : c;
            return a;
        }
        static float max(float a, float b, float c, float d, float e)
        {
            a = a > b ? a : b;
            c = c > d ? c : d;
            a = a > c ? a : c;
            return a > e ? a : e;
        }
        static float max(float a, float b, float c, float d, float e, float f)
        {
            a = a > b ? a : b;
            c = c > d ? c : d;
            a = a > c ? a : c;
            e = e > f ? e : f;
            return a > e ? a : e;
        }
        static float max(float a, float b, float c, float d, float e, float f, float g)
        {
            a = a > b ? a : b;
            c = c > d ? c : d;
            a = a > c ? a : c;
            e = e > f ? e : f;
            a = a > g ? a : g; 
            return a > e ? a : e;
        }
        static float max(float a, float b, float c, float d, float aa, float bb, float cc, float dd)
        {
            a = a > b ? a : b;
            c = c > d ? c : d;
            a = a > c ? a : c;
            aa = aa > bb ? aa : bb;
            cc = cc > dd ? cc : dd;
            aa = aa > cc ? aa : cc;
            return a > aa ? a : aa;
        }

    }

}
