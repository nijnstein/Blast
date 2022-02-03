//##########################################################################################################
// Copyright © 2022 Rob Lemmens | NijnStein Software <rob.lemmens.s31@gmail.com> All Rights Reserved  ^__^\#
// Unauthorized copying of this file, via any medium is strictly prohibited                           (oo)\#
// Proprietary and confidential                                                                       (__) #
//##########################################################################################################
#if STANDALONE_VSBUILD

#else

using NSS.Blast.Interpretor;
using NSS.Blast.SSMD;

using System;
using Unity.Burst;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;

namespace NSS.Blast.Jobs 
{

    public interface IBlastJob
    {
        public int GetExitCode(); 
    }


    [BurstCompile]
    public struct blast_execute_package_ssmd_burst_job : IBlastJob, IJob
    {
        public BlastSSMDInterpretor blaster;
        public NSS.Blast.BlastPackageData package;

        [NoAlias]
        [NativeDisableUnsafePtrRestriction]
        public IntPtr engine;

        [NoAlias]
        [NativeDisableUnsafePtrRestriction]
        public IntPtr data_buffer;

        public int ssmd_datacount;

        [NoAlias]
        [NativeDisableUnsafePtrRestriction]
        public IntPtr environment;

        public int exitcode;

        public void Execute()
        {
            unsafe
            {
                exitcode = blaster.Execute(package, engine, environment, (NSS.Blast.BlastSSMDDataStack*)data_buffer, ssmd_datacount);
            }
        }

        public int GetExitCode()
        {
            return exitcode; 
        } 
    }

    [BurstCompile]
    public struct blast_execute_package_burst_job : IBlastJob, IJob
    {
        public BlastInterpretor blaster;
        public NSS.Blast.BlastPackageData package;

        [NoAlias]
        [NativeDisableUnsafePtrRestriction]
        public IntPtr engine;

        [NoAlias]
        [NativeDisableUnsafePtrRestriction]
        public IntPtr environment;

        [NoAlias]
        [NativeDisableUnsafePtrRestriction]
        public IntPtr caller;

        public int exitcode;

        [BurstCompile]
        public void Execute()
        {
            blaster.SetPackage(package);
            exitcode = blaster.Execute(engine, environment, caller);
        }

        public int GetExitCode()
        {
            return exitcode; 
        }
    }
}


#endif