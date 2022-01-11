

using Unity.Mathematics;

namespace NSS.Blast
{

    public enum BlastVectorSizes : byte
    {
        float1 = 1,
        float2 = 2,
        float3 = 3,
        float4 = 4
    }

    public enum BlastVectorTypes : byte
    {
        v1 = 0,
        v2 = 1,
        v3 = 2,
        v4 = 3,
        v3x2 = 4,
        v3x3 = 5,
        v3x4 = 6,
        v4x4 = 7
    }



    public enum BlastPackageCapacities : int
    {
        c32 = 32,
        c48 = 48,
        c64 = 64,
        c96 = 96,
        c128 = 128,
        c192 = 192,
        c256 = 256,
        c320 = 320,
        c384 = 384,
        c448 = 448,
        c512 = 512,
        c640 = 640,
        c768 = 768,
        c896 = 896,
        c960 = 960
    }


}
