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
    unsafe public delegate void BSExecuteDelegate(IntPtr engine, float* variables);

    public interface IBlastHPCScriptJob
    {
        int ScriptId { get; }

        FunctionPointer<BSExecuteDelegate> GetBSD();
    }

    public struct blast
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
