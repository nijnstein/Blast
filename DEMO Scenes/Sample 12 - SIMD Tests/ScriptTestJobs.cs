using NSS.Blast;
using NSS.Blast.Interpretor;
using NSS.Blast.SSMD;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Burst;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;

namespace ScriptTestJobs
{
    /// <summary>
    /// test ssmd 
    /// </summary>
    [BurstCompile]
    unsafe public struct test_script_ssmd : IJob
    {
        public int datacount;
        public int repeatcount;


        [NativeDisableUnsafePtrRestriction]
        public BlastSSMDInterpretor blaster;

        [NoAlias]
        [NativeDisableUnsafePtrRestriction]
        public BlastEngineDataPtr engine; 

        public void Execute()
        {
            // our block of data 
            int ssmddatasize = blaster.CurrentSSMDDataSize;
            byte* ssmddata = //stackalloc byte[ssmddatasize * datacount];

                (byte*)UnsafeUtility.Malloc(ssmddatasize * datacount, 16, Unity.Collections.Allocator.Temp);

            if (BlastSSMDInterpretor.IsTrace)
            {
                UnityEngine.Debug.Log($"datasize = {ssmddatasize}, datacount = {datacount}"); 
            }

            

            // generate a list of datasegment pointers on the stack:
           /* byte** datastack = stackalloc byte*[datacount];

            // although we allocate everything in 1 block, we will index a pointer to every block,
            // this will allow us to accept non-block aligned lists of memory pointers  (set referenced to true on execute)_
            for (int i = 0; i < datacount; i++)
            {
                datastack[i] = ((byte*)ssmddata) + (ssmddatasize * i);
            }*/ 

            // execute 
            for (int i = 0; i < repeatcount; i++)
            {
                blaster.Execute(engine, IntPtr.Zero, (BlastSSMDDataStack*)ssmddata, datacount, false);
            }

            UnsafeUtility.Free(ssmddata, Unity.Collections.Allocator.Temp); 
        }
    }

 
    /// <summary>
    /// test normal 
    /// </summary>
    [BurstCompile]
    unsafe public struct test_script_normal : IJob
    {
        public int repeatcount;

        [NativeDisableUnsafePtrRestriction]
        public BlastInterpretor blaster;

        [NoAlias]
        [NativeDisableUnsafePtrRestriction]
        public BlastEngineDataPtr engine;

        public void Execute()
        {
            // our block of data is packaged within the package data 

            // execute 
            for (int i = 0; i < repeatcount; i++)
            {
                blaster.Execute(engine, IntPtr.Zero, IntPtr.Zero); 
            }
        }
    }
   

}
