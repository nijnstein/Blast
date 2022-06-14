//############################################################################################################################
//                                                                                                                           #
//  ██████╗ ██╗      █████╗ ███████╗████████╗                           Copyright © 2022 Rob Lemmens | NijnStein Software    #
//  ██╔══██╗██║     ██╔══██╗██╔════╝╚══██╔══╝                                    <rob.lemmens.s31 gmail com>                 #
//  ██████╔╝██║     ███████║███████╗   ██║                                                                                   #
//  ██╔══██╗██║     ██╔══██║╚════██║   ██║                                                                                   #
//  ██████╔╝███████╗██║  ██║███████║   ██║     V1.0.4e                                                                       #
//  ╚═════╝ ╚══════╝╚═╝  ╚═╝╚══════╝   ╚═╝                                                                                   #
//                                                                                                                           #
//############################################################################################################################
#pragma warning disable CS1591
#pragma warning disable CS0162

#if STANDALONE_VSBUILD

#else

using NSS.Blast.Interpretor;
using NSS.Blast.SSMD;

using System;
using Unity.Burst;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

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

        /// <summary>
        /// if true, array is interpreted as an array of pointers pointing to the data 
        /// </summary>
        public bool data_is_referenced;

        [NoAlias]
        [NativeDisableUnsafePtrRestriction]
        public IntPtr environment;

        /// <summary>
        /// we should hold a pointer to exitstate because in jobs this wont persist as this is a structure copied by value
        /// </summary>
        unsafe int exitcode
        {
            get
            {
                return exitstate == IntPtr.Zero ? (int)BlastError.error_job_exitstate_not_set : ((int*)exitstate)[0];
            }
            set
            {
                if (exitstate != IntPtr.Zero)
                {
                    ((int*)exitstate)[0] = value;
                }
            }
        }

        /// <summary>
        /// pointer to exitstate 
        /// </summary>
        [NativeDisableUnsafePtrRestriction]
        public IntPtr exitstate;


        public void Execute()
        {
            unsafe
            {
                int i = 0;
                exitcode = 0;

                blaster.SetPackage(in package);

                // divide load in slices of max_ssmd_count long 
                if (data_is_referenced)
                {
                    void** pp = (void**)data_buffer;

                    while (i < data_count && exitcode == 0)
                    {
                        int j = math.min(data_count, i + max_ssmd_count);
                        if (j > 0)
                        {
                            exitcode = blaster.Execute(in package, engine, environment, (BlastSSMDDataStack*)(void*)&pp[i], j - i, true);
                            i = j;
                        }
                    }
                }
                else
                {
                    int max_count = math.min(max_ssmd_count, data_count);
                    byte** dataptrs = stackalloc byte*[max_count];

                    byte* p = (byte*)data_buffer;

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
                            exitcode = blaster.Execute(in package, engine, environment, (BlastSSMDDataStack*)(void*)dataptrs, j, true);
                        }
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


        /// <summary>
        /// we should hold a pointer to exitstate because in jobs this wont persist as this is a structure copied by value
        /// </summary>
        unsafe int exitcode
        {
            get
            {
                return exitstate == IntPtr.Zero ? (int)BlastError.error_job_exitstate_not_set : ((int*)exitstate)[0];
            }
            set
            {
                if (exitstate != IntPtr.Zero)
                {
                    ((int*)exitstate)[0] = value;
                }
            }
        }

        /// <summary>
        /// pointer to exitstate 
        /// </summary>
        [NativeDisableUnsafePtrRestriction]
        public IntPtr exitstate;


        public static BlastError ValidateDataSize(BlastPackageData package, int datasize, int datacount)
        {
            if (datasize == 0 || datacount == 0)
            {
                return BlastError.error_execute_package_datasize_count_invalid;
            }

            if (package.DataSize != datasize)
            {
                // if alignment issue.. 
#if TRACE || DEVELOPMENT_BUILD || STANDALONE_VSBUILD
                Debug.LogError($"blast job validation error: specified datasize {datasize} mismatches package datasize {package.DataSize}");
#endif

                return BlastError.error_execute_package_datasize_mismatch;

            }


            return BlastError.success;
        }


        [BurstCompile]
        unsafe public void Execute()
        {
            if (package.PackageMode != BlastPackageMode.Normal)
            {
#if TRACE || DEVELOPMENT_BUILD || STANDALONE_VSBUILD
                Debug.LogError($"blast job validation error: packagemode '{package.PackageMode}' invalid, should be NORMAL");
#endif
                exitcode = (int)BlastError.error_invalid_packagemode_should_be_normal;
                return;
            }


#if TRACE || DEVELOPMENT_BUILD
            exitcode = (int)ValidateDataSize(package, data_size, data_count);
#else
            exitcode = 0; 
#endif
            if (exitcode == 0)
            {
                if (data_count <= 0)
                {
                    return;
                }
                else if (data_count == 1)
                {
                    blaster.SetPackage(package, data.ToPointer());
                    exitcode = blaster.Execute(engine, environment, caller);
                }
                else
                {
                    exitcode = 0;
                    blaster.SetPackage(package);
                    for (int i = 0; i < data_count && exitcode == 0; i++)
                    {
                        void* p = &((byte*)data)[data_size * i];
                        blaster.ResetPackageData(p);
                        exitcode = blaster.Execute(engine, environment, caller);
                    }
                }
            }

#if TRACE || DEVELOPMENT_BUILD || STANDALONE_VSBUILD
            if (exitcode != 0)
            {
                Debug.LogError("blast job execution failed, exitcode = {exitcode}");
                return;
            }
#endif
        }

        public int GetExitCode()
        {
            return exitcode;
        }
    }

}


#endif
