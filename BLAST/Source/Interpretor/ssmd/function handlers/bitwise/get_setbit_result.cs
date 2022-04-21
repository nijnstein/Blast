//############################################################################################################################
// BLAST v1.0.4c                                                                                                             #
// Copyright © 2022 Rob Lemmens | NijnStein Software <rob.lemmens.s31 gmail com> All Rights Reserved                   ^__^\ #
// Unauthorized copying of this file, via any medium is strictly prohibited proprietary and confidential               (oo)\ #
//                                                                                                                     (__)  #
//############################################################################################################################

#if STANDALONE_VSBUILD
    using NSS.Blast.Standalone;
    using System.Reflection;
#else
    using UnityEngine;
    using Unity.Burst.CompilerServices;
#endif


using Unity.Burst;
using Unity.Mathematics;
using NSS.Blast.Interpretor;

namespace NSS.Blast.SSMD
{
    unsafe public partial struct BlastSSMDInterpretor
    {

        /// <summary>
        /// Note: sets bit in place, first parameter must be a direct data index (compiler should force this)
        /// </summary>
        void get_setbit_result([NoAlias] void* temp, ref int code_pointer, ref byte vector_size, int ssmd_datacount)
        {
            float* fp_index = (float*)temp;
            float* fp_value = &fp_index[ssmd_datacount];

            // index would be the same for each ssmd record
            int dataindex = code[code_pointer] - BlastInterpretor.opt_id;
            code_pointer++;

#if DEVELOPMENT_BUILD || TRACE
            // dataindex must point to a valid id 
            if (dataindex < 0 || dataindex + BlastInterpretor.opt_id >= 255)
            {
                Debug.LogError($"ssmd.set_bit: first parameter must directly point to a dataindex but its '{dataindex + BlastInterpretor.opt_id}' instead");
                return;
            }
#else
            // dont check anything in release and vectorsize must be 1 so
            vector_size = 1;
#endif

            // get index
#if DEVELOPMENT_BUILD || TRACE
            BlastVariableDataType datatype;
            pop_op_meta_cp(code_pointer, out datatype, out vector_size);
            if (vector_size != 1)
            {
                Debug.LogError($"ssmd.set_bit: the index to set may only be of vectorsize 1 p1 = {datatype}.{vector_size}");
                return;
            }
#endif

            bool b_constant_index = pop_fx_into_ref<float>(ref code_pointer, fp_index);

            // get value
#if DEVELOPMENT_BUILD || TRACE
            pop_op_meta_cp(code_pointer, out datatype, out vector_size);
            if (vector_size != 1)
            {
                Debug.LogError($"ssmd.set_bit: the value to set may only be of vectorsize 1 p1 = {datatype}.{vector_size}");
                return;
            }
#endif

            // NOTE: if the source of fp_value is a constant then in ssmd they would be the same value for each record 
            // OPTIMIZE: and this loop can be optimized a lot... 

            bool b_constant_value = pop_fx_into_ref<float>(ref code_pointer, fp_value);

            // setbit & setbits override the datatype of the target to bool32 
            BlastInterpretor.SetMetaData(in metadata, BlastVariableDataType.Bool32, 1, (byte)dataindex);

#if DEVELOPMENT_BUILD || TRACE

            // check that all constant arrays are actually of all the same values 
            // this would indicate a serious bug in constant handling early 

            if (b_constant_index)
            {
                float f1 = fp_index[0];
                for (int i = 1; i < ssmd_datacount; i++)
                {
                    if (fp_index[i] != f1)
                    {
                        Debug.LogError($"ssmd.set_bit: the index is marked constant but is not equal for all ssmd records at {code_pointer}");
                        return;
                    }
                }
            }

            if (b_constant_value)
            {
                float f0 = fp_value[0];
                for (int i = 1; i < ssmd_datacount; i++)
                {
                    if (fp_value[i] != f0)
                    {
                        Debug.LogError($"ssmd.set_bit: the value is marked constant but is not equal for all ssmd records at {code_pointer}");
                        return;
                    }
                }
            }

#endif

            //
            // when index or value is constant we can do some very nice optimizations, in this case we can even skip stuff 
            // 

            if (b_constant_value)
            {
                if (b_constant_index)
                {
                    // mask always the same 
                    uint mask = (uint)(1 << (byte)fp_index[0]);

                    // value always the same 
                    if (((uint*)fp_value)[0] == 0)
                    {
                        // unset bits: AND the mask 
                        if (mask == uint.MaxValue)
                        {
                            // mask is all 1, data remains equal 
                        }
                        else
                        {
                            for (int i = 0; i < ssmd_datacount; i++)
                            {
                                uint current = ((uint*)data[i])[dataindex];

                                current = (uint)(current & ~mask);

                                ((uint*)data[i])[dataindex] = current;
                            }
                        }
                    }
                    else
                    {
                        // set bits: OR the mask
                        if (mask == 0)
                        {
                            // mask all 0, data remains equal 
                        }
                        else
                        {
                            for (int i = 0; i < ssmd_datacount; i++)
                            {
                                uint current = ((uint*)data[i])[dataindex];

                                current = current | mask;

                                ((uint*)data[i])[dataindex] = current;
                            }
                        }
                    }
                }
                else
                {
                    // the value is always the same but the mask changes for every i
                    if (((uint*)fp_value)[0] == 0)
                    {
                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            uint current = ((uint*)data[i])[dataindex];

                            uint mask = (uint)(1 << (byte)fp_index[i]);

                            current = (uint)(current & ~mask);

                            ((uint*)data[i])[dataindex] = current;
                        }
                    }
                    else
                    {
                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            uint current = ((uint*)data[i])[dataindex];

                            uint mask = (uint)(1 << (byte)fp_index[i]);

                            current = (uint)(current | mask);

                            ((uint*)data[i])[dataindex] = current;
                        }
                    }
                }
            }
            else
            {
                if (b_constant_index)
                {
                    // mask always the same 
                    uint mask = (uint)(1 << (byte)fp_index[0]);

                    for (int i = 0; i < ssmd_datacount; i++)
                    {
                        uint current = ((uint*)data[i])[dataindex];

                        current = math.select((uint)(current | mask), (uint)(current & ~mask), ((uint*)fp_value)[i] == 0);

                        ((uint*)data[i])[dataindex] = current;
                    }
                }
                else
                {
                    // nothing constant 
                    for (int i = 0; i < ssmd_datacount; i++)
                    {
                        uint current = ((uint*)data[i])[dataindex];

                        uint mask = (uint)(1 << (byte)fp_index[i]);

                        current = math.select((uint)(current | mask), (uint)(current & ~mask), ((uint*)fp_value)[i] == 0);

                        ((uint*)data[i])[dataindex] = current;
                    }
                }
            }


            code_pointer--;
        }
    }
}
