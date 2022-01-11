using System.Text;

namespace NSS.Blast
{
    public static class BlastUtils
    {
        unsafe public static string GetByteCodeByteText(in byte* bytes, in int length, int column_count)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < length; i++)
            {
                sb.Append($"{bytes[i].ToString().PadLeft(3, '0').PadRight(4)}");
                if ((i + 1) % column_count == 0 || i == length - 1)
                {
                    sb.Append("\n");
                }
            }
            return sb.ToString();
        }


        unsafe public static string GetReadableByteCode(in byte* bytes, in int length)
        {
            StringBuilder sb = new StringBuilder();

            int i = 0;

            while (i < length)
            {
                script_op op = (script_op)bytes[i];

                // generate a somewhat readable assembly view
                //
                // offset |  translation
                //-------------------------------
                // 000    |  assign a 2 nop
                // 004    |  assign b (4 * 4) nop
                // etc. 
                //
                //

                switch (op)
                {
                    case script_op.nop: sb.Append("\n"); break;

                    case script_op.assign: sb.Append("assign "); break;

                    case script_op.add: sb.Append("+ "); break;
                    case script_op.substract: sb.Append("- "); break;
                    case script_op.multiply: sb.Append("* "); break;
                    case script_op.divide: sb.Append("/ "); break;
                    case script_op.and: sb.Append("& "); break;
                    case script_op.or: sb.Append("| "); break;
                    case script_op.not: sb.Append("! "); break;
                    case script_op.xor: sb.Append("^ "); break;
                    case script_op.smaller: sb.Append("< "); break;
                    case script_op.greater: sb.Append("> "); break;
                    case script_op.smaller_equals: sb.Append("<= "); break;
                    case script_op.greater_equals: sb.Append(">= "); break;
                    case script_op.equals: sb.Append("= "); break;
                    case script_op.not_equals: sb.Append("!= "); break;

                    case script_op.ret: sb.Append("return "); break;
                    case script_op.yield: sb.Append("yield "); break;

                    case script_op.begin: sb.Append("("); break;
                    case script_op.end: sb.Append(")"); break;

                    case script_op.jz: sb.Append("jz "); break;
                    case script_op.jnz: sb.Append("jnz "); break;

                    case script_op.jump: sb.Append("jump "); break;
                    case script_op.jump_back: sb.Append("jumpback "); break;

                    case script_op.push: sb.Append("push "); break;
                    case script_op.pop: sb.Append("pop "); break;
                    case script_op.pushv: sb.Append("pushv "); break;
                    case script_op.popv: sb.Append("popv "); break;
                    case script_op.peek: sb.Append("peek "); break;
                    case script_op.peekv: sb.Append("peekv "); break;
                    case script_op.pushf: sb.Append("pushf "); break;

                    case script_op.fma:
                        break;
                    case script_op.undefined1:
                        break;
                    case script_op.undefined2:
                        break;
                    case script_op.adda:
                        break;
                    case script_op.mula:
                        break;
                    case script_op.diva:
                        break;
                    case script_op.suba:
                        break;
                    case script_op.all:
                        break;
                    case script_op.any:
                        break;
                    case script_op.abs:
                        break;
                    case script_op.select:
                        break;
                    case script_op.random:
                        break;
                    case script_op.seed:
                        break;
                    case script_op.max:
                        break;
                    case script_op.min:
                        break;
                    case script_op.maxa:
                        break;
                    case script_op.mina:
                        break;
                    case script_op.lerp:
                        break;
                    case script_op.slerp:
                        break;
                    case script_op.saturate:
                        break;
                    case script_op.clamp:
                        break;
                    case script_op.normalize:
                        break;
                    case script_op.ceil:
                        break;
                    case script_op.floor:
                        break;
                    case script_op.frac:
                        break;
                    case script_op.sin:
                        break;
                    case script_op.cos:
                        break;
                    case script_op.tan:
                        break;
                    case script_op.atan:
                        break;
                    case script_op.cosh:
                        break;
                    case script_op.sinh:
                        break;
                    case script_op.distance:
                        break;
                    case script_op.distancesq:
                        break;
                    case script_op.length:
                        break;
                    case script_op.lengthsq:
                        break;
                    case script_op.degrees:
                        break;
                    case script_op.radians:
                        break;
                    case script_op.sqrt:
                        break;
                    case script_op.rsqrt:
                        break;
                    case script_op.pow:
                        break;
                   

                    case script_op.value_0: sb.Append("0 "); break;
                    case script_op.value_1: sb.Append("1 "); break;
                    case script_op.value_2: sb.Append("2 "); break;
                    case script_op.value_3: sb.Append("3 "); break;
                    case script_op.value_4: sb.Append("4 "); break;
                    case script_op.value_8: sb.Append("8 "); break;
                    case script_op.value_10: sb.Append("10 "); break;
                    case script_op.value_16: sb.Append("16 "); break;
                    case script_op.value_24: sb.Append("24 "); break;
                    case script_op.value_30: sb.Append("30 "); break;
                    case script_op.value_32: sb.Append("32 "); break;
                    case script_op.value_45: sb.Append("45 "); break;
                    case script_op.value_64: sb.Append("64 "); break;
                    case script_op.value_90: sb.Append("90 "); break;
                    case script_op.value_100: sb.Append("100 "); break;
                    case script_op.value_128: sb.Append("128 "); break;
                    case script_op.value_180: sb.Append("180 "); break;
                    case script_op.value_256: sb.Append("256 "); break;
                    case script_op.value_270: sb.Append("270 "); break;
                    case script_op.value_360: sb.Append("360 "); break;
                    case script_op.value_512: sb.Append("512 "); break;
                    case script_op.value_1024: sb.Append("1024 "); break;
                    case script_op.inv_value_2: sb.Append((1f / 2f).ToString("0.000") + " "); break;
                    case script_op.inv_value_3: sb.Append((1f / 3f).ToString("0.000") + " "); break;
                    case script_op.inv_value_4: sb.Append((1f / 4f).ToString("0.000") + " "); break;
                    case script_op.inv_value_8: sb.Append((1f / 8f).ToString("0.000") + " "); break;
                    case script_op.inv_value_10: sb.Append((1f / 10f).ToString("0.000") + " "); break;
                    case script_op.inv_value_16: sb.Append((1f / 16f).ToString("0.000") + " "); break;
                    case script_op.inv_value_24: sb.Append((1f / 24f).ToString("0.000") + " "); break;
                    case script_op.inv_value_30: sb.Append((1f / 30f).ToString("0.000") + " "); break;
                    case script_op.inv_value_32: sb.Append((1f / 32f).ToString("0.000") + " "); break;
                    case script_op.inv_value_45: sb.Append((1f / 45f).ToString("0.000") + " "); break;
                    case script_op.inv_value_64: sb.Append((1f / 64f).ToString("0.000") + " "); break;
                    case script_op.inv_value_90: sb.Append((1f / 90f).ToString("0.000") + " "); break;
                    case script_op.inv_value_100: sb.Append((1f / 100f).ToString("0.000") + " "); break;
                    case script_op.inv_value_128: sb.Append((1f / 128f).ToString("0.000") + " "); break;
                    case script_op.inv_value_180: sb.Append((1f / 180f).ToString("0.000") + " "); break;
                    case script_op.inv_value_256: sb.Append((1f / 256f).ToString("0.000") + " "); break;
                    case script_op.inv_value_270: sb.Append((1f / 270f).ToString("0.000") + " "); break;
                    case script_op.inv_value_360: sb.Append((1f / 360f).ToString("0.000") + " "); break;
                    case script_op.inv_value_512: sb.Append((1f / 512f).ToString("0.000") + " "); break;
                    case script_op.inv_value_1024: sb.Append((1f / 1024f).ToString("0.000") + " "); break;

                    case script_op.ex_op:
                        i++;
                        extended_script_op ex = (extended_script_op)bytes[i];
                        switch (ex)
                        {
                            case extended_script_op.nop: break;
                            case extended_script_op.exp:
                            case extended_script_op.log2:
                            case extended_script_op.exp10:
                            case extended_script_op.log10:
                            case extended_script_op.logn:
                            case extended_script_op.cross:
                            case extended_script_op.dot:
                            case extended_script_op.ex:
                                sb.Append($"{ex} ");
                                break;

                            case extended_script_op.call:
                                break;
                        }
                        break;


                    default:
                        // value by id 
                        sb.Append($"var{(byte)op} ");
                        break;
                };


                i++;
            }
            return sb.ToString();
        }
    }

}