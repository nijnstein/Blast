using NSS.Blast.SSMD;
using Unity.Burst;
using Unity.Jobs;
using Unity.Mathematics;


/// <summary>
/// we check the pattern of operation once (for mul)
/// </summary>
namespace vectorization_tests
    {
        [BurstCompile]
        unsafe public struct test_mul_array_f4f4 : IJob
        {
            public int datacount;
            public int repeatcount;

            public void Execute()
            {
                float4* a = stackalloc float4[datacount];
                float4* b = stackalloc float4[datacount];

                for (int i = 0; i < repeatcount; i++)
                {
                    simd.mul_array_f4f4(a, b, datacount);
                }
            }
        }

        [BurstCompile]
        unsafe public struct test_mul_array_f4f4_constant : IJob
        {
            public int datacount;
            public int repeatcount;

            public void Execute()
            {
                float4* a = stackalloc float4[datacount];
                float4* b = stackalloc float4[datacount];

                for (int i = 0; i < repeatcount; i++)
                {
                    simd.mul_array_f4f4_constant(a, b, 3f, datacount);
                }
            }
        }

        [BurstCompile]
        unsafe public struct test_mul_array_f4f1 : IJob
        {
            public int datacount;
            public int repeatcount;

            public void Execute()
            {
                float4* a = stackalloc float4[datacount];
                float4* b = stackalloc float4[datacount];

                for (int i = 0; i < repeatcount; i++)
                {
                    simd.mul_array_f4f1(a, b, datacount);
                }
            }
        }

        [BurstCompile]
        unsafe public struct test_mul_array_f4f1_constant : IJob
        {
            public int datacount;
            public int repeatcount;

            public void Execute()
            {
                float4* a = stackalloc float4[datacount];
                float4* b = stackalloc float4[datacount];

                for (int i = 0; i < repeatcount; i++)
                {
                    simd.mul_array_f4f1(a, 3f, datacount);
                }
            }
        }


        [BurstCompile]
        unsafe public struct test_mul_array_f1f1 : IJob
        {
            public int datacount;
            public int repeatcount;

            public void Execute()
            {
                float4* a = stackalloc float4[datacount];
                float4* b = stackalloc float4[datacount];

                for (int i = 0; i < repeatcount; i++)
                {
                    simd.mul_array_f1f1(a, b, datacount);
                }
            }
        }

        [BurstCompile]
        unsafe public struct test_mul_array_f1f1_constant : IJob
        {
            public int datacount;
            public int repeatcount;

            public void Execute()
            {
                float4* a = stackalloc float4[datacount];

                for (int i = 0; i < repeatcount; i++)
                {
                    simd.mul_array_f1f1(a, 3f, datacount);
                }
            }
        }

        /// <summary>
        /// vn[].x = v1[] * v1_index[][]
        /// </summary>
        [BurstCompile]
        unsafe public struct test_mul_array_f1f1_indexed : IJob
        {
            public int datacount;
            public int repeatcount;

            public void Execute()
            {
                float* a = stackalloc float[datacount];
                float* b = stackalloc float[datacount];

                float* data = stackalloc float[datacount * 4];
                float** indexed = stackalloc float*[datacount];

                for (int i = 0; i < datacount; i++) indexed[i] = &data[i * 4];

                for (int i = 0; i < repeatcount; i++)
                {
                    simd.mul_array_f1f1_indexed<float>(b, a, indexed, 2, true, 4, datacount);
                }
            }
        }

        /// <summary>
        /// vn[].x = v1[] * v1_index[][]
        /// </summary>
        [BurstCompile]
        unsafe public struct test_mul_array_f1f1_indexed_unaligned : IJob
        {
            public int datacount;
            public int repeatcount;

            public void Execute()
            {
                float* a = stackalloc float[datacount];
                float* b = stackalloc float[datacount];

                float* data = stackalloc float[datacount * 4];
                float** indexed = stackalloc float*[datacount];

                for (int i = 0; i < datacount; i++) indexed[i] = &data[i * 4];

                for (int i = 0; i < repeatcount; i++)
                {
                    simd.mul_array_f1f1_indexed<float>(b, a, indexed, 2, false, 4, datacount);
                }
            }
        }



        /// <summary>
        /// f4_out[i] = f4_in[i] * ((float4*)(void*)&((float*) data[i])[offset])[0]
        /// </summary>
        [BurstCompile]
        unsafe public struct test_mul_array_f4f4_indexed : IJob
        {
            public int datacount;
            public int repeatcount;

            public void Execute()
            {
                float4* a = stackalloc float4[datacount];
                float4* b = stackalloc float4[datacount];

                float* data = stackalloc float[datacount * 4];
                float** indexed = stackalloc float*[datacount];

                for (int i = 0; i < datacount; i++) indexed[i] = &data[i * 4];

                for (int i = 0; i < repeatcount; i++)
                {
                    simd.mul_array_f4f4_indexed(b, a, indexed, 2, true, 4, datacount);
                }
            }
        }

        [BurstCompile]
        unsafe public struct test_mul_array_f4f4_indexed_unaligned : IJob
        {
            public int datacount;
            public int repeatcount;

            public void Execute()
            {
                float4* a = stackalloc float4[datacount];
                float4* b = stackalloc float4[datacount];

                float* data = stackalloc float[datacount * 4];
                float** indexed = stackalloc float*[datacount];

                for (int i = 0; i < datacount; i++) indexed[i] = &data[i * 4];

                for (int i = 0; i < repeatcount; i++)
                {
                    simd.mul_array_f4f4_indexed(b, a, indexed, 2, false, 4, datacount);
                }
            }
        }



        [BurstCompile]
        unsafe public struct test_mul_array_f3f3_indexed : IJob
        {
            public int datacount;
            public int repeatcount;

            public void Execute()
            {
                float3* a = stackalloc float3[datacount];
                float3* b = stackalloc float3[datacount];

                float* data = stackalloc float[datacount * 4];
                float** indexed = stackalloc float*[datacount];

                for (int i = 0; i < datacount; i++) indexed[i] = &data[i * 4];

                for (int i = 0; i < repeatcount; i++)
                {
                    simd.mul_array_f3f3_indexed(b, a, indexed, 2, true, 4, datacount);
                }
            }
        }

        [BurstCompile]
        unsafe public struct test_mul_array_f2f2_indexed : IJob
        {
            public int datacount;
            public int repeatcount;

            public void Execute()
            {
                float2* a = stackalloc float2[datacount];
                float2* b = stackalloc float2[datacount];

                float* data = stackalloc float[datacount * 4];
                float** indexed = stackalloc float*[datacount];

                for (int i = 0; i < datacount; i++) indexed[i] = &data[i * 4];

                for (int i = 0; i < repeatcount; i++)
                {
                    simd.mul_array_f2f2_indexed(b, a, indexed, 2, true, 4, datacount);
                }
            }
        }
    }
