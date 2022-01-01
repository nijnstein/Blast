using Unity.Mathematics;

namespace NSS.Blast
{

    public struct BlastValues
    {
        public static float4 get_constant_vector_value(script_op op, BlastVectorSizes vector_size)
        {
            switch (vector_size)
            {
                case BlastVectorSizes.float1:
                    return new float4(GetConstantValue(op));

                case BlastVectorSizes.float2:
                case BlastVectorSizes.float3:
                case BlastVectorSizes.float4:
                    switch (op)
                    {
                        case script_op.pi: return new float4(math.PI);
                        case script_op.inv_pi: return new float4(1 / math.PI);
                        case script_op.epsilon: return new float4(math.EPSILON);
                        case script_op.infinity: return new float4(math.INFINITY);
                        case script_op.negative_infinity: return new float4(-math.INFINITY);
                        case script_op.nan: return new float4(math.NAN);
                        case script_op.min_value: return new float4(math.FLT_MIN_NORMAL);
                        case script_op.value_0: return new float4(0f);
                        case script_op.value_1: return new float4(1f);
                        case script_op.value_2: return new float4(2f);
                        case script_op.value_3: return new float4(3f);
                        case script_op.value_4: return new float4(4f);
                        case script_op.value_8: return new float4(8f);
                        case script_op.value_10: return new float4(10f);
                        case script_op.value_16: return new float4(16f);
                        case script_op.value_24: return new float4(24f);
                        case script_op.value_32: return new float4(32f);
                        case script_op.value_64: return new float4(64f);
                        case script_op.value_100: return new float4(100f);
                        case script_op.value_128: return new float4(128f);
                        case script_op.value_256: return new float4(256f);
                        case script_op.value_512: return new float4(512f);
                        case script_op.value_1000: return new float4(1000f);
                        case script_op.value_1024: return new float4(1024f);
                        case script_op.value_30: return new float4(30f);
                        case script_op.value_45: return new float4(45f);
                        case script_op.value_90: return new float4(90f);
                        case script_op.value_180: return new float4(180f);
                        case script_op.value_270: return new float4(270f);
                        case script_op.value_360: return new float4(360f);
                        case script_op.inv_value_2: return new float4(1f / 2f);
                        case script_op.inv_value_3: return new float4(1f / 3f);
                        case script_op.inv_value_4: return new float4(1f / 4f);
                        case script_op.inv_value_8: return new float4(1f / 8f);
                        case script_op.inv_value_10: return new float4(1f / 10f);
                        case script_op.inv_value_16: return new float4(1f / 16f);
                        case script_op.inv_value_24: return new float4(1f / 24f);
                        case script_op.inv_value_32: return new float4(1f / 32f);
                        case script_op.inv_value_64: return new float4(1f / 64f);
                        case script_op.inv_value_100: return new float4(1f / 100f);
                        case script_op.inv_value_128: return new float4(1f / 128f);
                        case script_op.inv_value_256: return new float4(1f / 256f);
                        case script_op.inv_value_512: return new float4(1f / 512f);
                        case script_op.inv_value_1000: return new float4(1f / 1000f);
                        case script_op.inv_value_1024: return new float4(1f / 1024f);
                        case script_op.inv_value_30: return new float4(1f / 30f);
                        case script_op.inv_value_45: return new float4(1f / 45f);
                        case script_op.inv_value_90: return new float4(1f / 90f);
                        case script_op.inv_value_180: return new float4(1f / 180f);
                        case script_op.inv_value_270: return new float4(1f / 270f);
                        case script_op.inv_value_360: return new float4(1f / 360f);

                        // what to do? screw up intentionally? yeah :)
                        default:
#if LOG_ERRORS
                        UnityEngine.Debug.LogError($"blast.get_constant_vector_value({op}, {vector_size}) -> not a value -> NaN returned to caller");
#endif
                            return float.NaN;
                    }

                default:
                    {
#if LOG_ERRORS
                    UnityEngine.Debug.LogError($"blast.get_constant_vector_value({op}, {vector_size}) -> vectorsize not supported");
#endif
                        return float.NaN;
                    }
            }
        }

        public static float GetConstantValue(script_op op)
        {
            switch (op)
            {
                //case script_op.deltatime: return Time.deltaTime;
                //case script_op.time: return Time.time;
                //case script_op.framecount: return Time.frameCount;
                //case script_op.fixeddeltatime: return Time.fixedDeltaTime; 

                case script_op.pi: return math.PI;
                case script_op.inv_pi: return 1 / math.PI;
                case script_op.epsilon: return math.EPSILON;
                case script_op.infinity: return math.INFINITY;
                case script_op.negative_infinity: return -math.INFINITY;
                case script_op.nan: return math.NAN;
                case script_op.min_value: return math.FLT_MIN_NORMAL;
                case script_op.value_0: return 0f;
                case script_op.value_1: return 1f;
                case script_op.value_2: return 2f;
                case script_op.value_3: return 3f;
                case script_op.value_4: return 4f;
                case script_op.value_8: return 8f;
                case script_op.value_10: return 10f;
                case script_op.value_16: return 16f;
                case script_op.value_24: return 24f;
                case script_op.value_32: return 32f;
                case script_op.value_64: return 64f;
                case script_op.value_100: return 100f;
                case script_op.value_128: return 128f;
                case script_op.value_256: return 256f;
                case script_op.value_512: return 512f;
                case script_op.value_1000: return 1000f;
                case script_op.value_1024: return 1024f;
                case script_op.value_30: return 30f;
                case script_op.value_45: return 45f;
                case script_op.value_90: return 90f;
                case script_op.value_180: return 180f;
                case script_op.value_270: return 270f;
                case script_op.value_360: return 360f;
                case script_op.inv_value_2: return 1f / 2f;
                case script_op.inv_value_3: return 1f / 3f;
                case script_op.inv_value_4: return 1f / 4f;
                case script_op.inv_value_8: return 1f / 8f;
                case script_op.inv_value_10: return 1f / 10f;
                case script_op.inv_value_16: return 1f / 16f;
                case script_op.inv_value_24: return 1f / 24f;
                case script_op.inv_value_32: return 1f / 32f;
                case script_op.inv_value_64: return 1f / 64f;
                case script_op.inv_value_100: return 1f / 100f;
                case script_op.inv_value_128: return 1f / 128f;
                case script_op.inv_value_256: return 1f / 256f;
                case script_op.inv_value_512: return 1f / 512f;
                case script_op.inv_value_1000: return 1f / 1000f;
                case script_op.inv_value_1024: return 1f / 1024f;
                case script_op.inv_value_30: return 1f / 30f;
                case script_op.inv_value_45: return 1f / 45f;
                case script_op.inv_value_90: return 1f / 90f;
                case script_op.inv_value_180: return 1f / 180f;
                case script_op.inv_value_270: return 1f / 270f;
                case script_op.inv_value_360: return 1f / 360f;

                // what to do? screw up intentionally? yeah :)
                default:
#if CHECK_STACK
                UnityEngine.Debug.LogError($"blast.get_constant_value({op}) -> not a value -> NaN returned to caller");
#endif
                    return float.NaN;
            }
        }

    }


    public enum BlastEngineSystemConstant : int
    {
        Time = 256 + extended_script_constant.time,
        DeltaTime = 256 + extended_script_constant.deltatime,
        FixedDeltaTime = 256 + extended_script_constant.fixeddeltatime,
        FrameCount = 256 + extended_script_constant.framecount
    }


}