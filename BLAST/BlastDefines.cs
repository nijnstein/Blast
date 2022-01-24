

using Unity.Mathematics;

namespace NSS.Blast
{
    /// <summary>
    /// Versions of blast present in build
    /// </summary>
    public enum BlastRuntimeVersions : int
    {
#if BS_BETA
        Beta = 0,
#endif 
        /// <summary>
        /// the final version 1 = release 1
        /// </summary>
        Final1 = 1,

#if BS_EXPERIMENTAL
        Experimental = 2
#endif 
    }

    /// <summary>
    /// the different compiler outputs / language targets 
    /// </summary>
    public enum BlastLanguageVersion : int
    {
        /// <summary>
        /// unknown language version
        /// </summary>
        None = 0,

        /// <summary>
        /// BLAST Script, default script / bytecode 
        /// </summary>
        BS1 = 1,

        /// <summary>
        /// BLAST Single Script Multiple Data
        /// </summary>
        BSSMD1 = 2,

        /// <summary>
        /// Burstable C# code packed in functions for burst to compile at designtime
        /// </summary>
        HPC = 3
    }

    /// <summary>
    /// supported vectorsizes 
    /// </summary>
    public enum BlastVectorSizes : byte
    {
        /// <summary>
        /// size 1: 4 bytes
        /// </summary>
        float1 = 1,
        /// <summary>
        /// size 2: 8 bytes
        /// </summary>
        float2 = 2,
        /// <summary>
        /// size 3: 12 bytes
        /// </summary>
        float3 = 3,
        /// <summary>
        /// size 4: 16 bytes
        /// </summary>
        float4 = 4
    }

    /// <summary>
    /// supported vectortypes 
    /// </summary>
    public enum BlastVectorTypes : byte
    {
        /// <summary>
        /// size 1 vector
        /// </summary>
        v1 = 0,

        /// <summary>
        /// size 2 vector
        /// </summary>
        v2 = 1,

        /// <summary>
        /// size 3 vector
        /// </summary>
        v3 = 2,

        /// <summary>
        /// size 4 vector
        /// </summary>
        v4 = 3,
#if BS2
        v3x2 = 4,
        v3x3 = 5,
        v3x4 = 6,
        v4x4 = 7
#endif
    }



}
