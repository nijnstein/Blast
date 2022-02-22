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
using Unity.Mathematics;

namespace NSS.Blast.Jobs 
{

    public interface IBlastJob
    {
        public int GetExitCode(); 
    }


    [BurstCompile]
    public struct blast_execute_package_ssmd_burst_job : IJob, IBlastJob
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
    public struct blast_execute_package_ssmd_array_burst_job : IJob, IBlastJob
    {
        public BlastSSMDInterpretor blaster;
        public NSS.Blast.BlastPackageData package;

        [NoAlias]
        [NativeDisableUnsafePtrRestriction]
        public IntPtr engine;

        [NoAlias]
        [NativeDisableUnsafePtrRestriction]
        public IntPtr data_buffer;

        public int data_count;
        public int data_size;
        public int max_ssmd_count; 

        [NoAlias]
        [NativeDisableUnsafePtrRestriction]
        public IntPtr environment;

        public int exitcode;

        public void Execute()
        {
            unsafe
            {
                int max_count = math.min(max_ssmd_count, data_count); 
                byte** dataptrs = stackalloc byte*[max_count];

                byte* p = (byte*)data_buffer;

                int i = 0;
                exitcode = 0;

                blaster.SetPackage(in package);

                // divide load in slices of max_ssmd_count long 
                while (i < data_count && exitcode == 0)
                {
                    int j = 0;
                    while (i < data_count && j < max_count)
                    {
                        dataptrs[j] = (byte*)&p[i * data_size];
                        i++;
                        j++;
                    }
                    if (j > 0)
                    {
                        // execute slice
                        exitcode = blaster.Execute(in package, engine, environment, (BlastSSMDDataStack*)(void*)dataptrs, j);
                    }
                }
            }
        }

        public int GetExitCode()
        {
            return exitcode;
        }
    }




    [BurstCompile]
    public struct blast_execute_package_burst_job : IJob, IBlastJob
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


    [BurstCompile]
    public struct blast_execute_package_with_multiple_data_burst_job : IJob, IBlastJob
    {
        public BlastInterpretor blaster;
        public NSS.Blast.BlastPackageData package;

        /// <summary>
        /// datasegment pointer 
        /// </summary>
        [NoAlias]
        [NativeDisableUnsafePtrRestriction]
        public IntPtr data;
        
        /// <summary>
        /// number of datarecords 
        /// </summary>
        public int data_count;
        /// <summary>
        /// size in bytes of 1 data record 
        /// </summary>
        public int data_size; 

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
        unsafe public void Execute()
        {
            if(data_count <= 0)
            {
                return; 
            }
            else if(data_count == 1)
            {
                blaster.SetPackage(package, data.ToPointer());
                exitcode = blaster.Execute(engine, environment, caller);
            }
            else
            {
                exitcode = 0;
                for (int i = 0; i < data_count && exitcode == 0; i++)
                {
                    void* p = &((byte*)data)[data_size * i];
                    blaster.SetPackage(package, p);
                    exitcode = blaster.Execute(engine, environment, caller);
                }
            }
        }

        public int GetExitCode()
        {
            return exitcode;
        }
    }

}


#endif