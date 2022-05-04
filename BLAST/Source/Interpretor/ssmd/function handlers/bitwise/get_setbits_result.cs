//############################################################################################################################
//                                                                                                                           #
//  ██████╗ ██╗      █████╗ ███████╗████████╗                           Copyright © 2022 Rob Lemmens | NijnStein Software    #
//  ██╔══██╗██║     ██╔══██╗██╔════╝╚══██╔══╝                                    <rob.lemmens.s31 gmail com>                 #
//  ██████╔╝██║     ███████║███████╗   ██║                                           All Rights Reserved                     #
//  ██╔══██╗██║     ██╔══██║╚════██║   ██║                                                                                   #
//  ██████╔╝███████╗██║  ██║███████║   ██║     V1.0.4e                                                                       #
//  ╚═════╝ ╚══════╝╚═╝  ╚═╝╚══════╝   ╚═╝                                                                                   #
//                                                                                                                           #
//############################################################################################################################
//                                                                                                                     (__)  #
//       Unauthorized copying of this file, via any medium is strictly prohibited proprietary and confidential         (oo)  #
//                                                                                                                     (__)  #
//############################################################################################################################
#pragma warning disable CS1591
#pragma warning disable CS0162


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

        void get_setbits_result([NoAlias] void* temp, ref int code_pointer, ref byte vector_size, int ssmd_datacount)
        {
            float* fp_mask = (float*)temp;
            float* fp_value = &fp_mask[ssmd_datacount];

            // index would be the same for each ssmd record
            int dataindex = code[code_pointer] - BlastInterpretor.opt_id;
            code_pointer++;

#if DEVELOPMENT_BUILD || TRACE
            // dataindex must point to a valid id 
            if (dataindex < 0 || dataindex + BlastInterpretor.opt_id >= 255)
            {
                Debug.LogError($"ssmd.set_bits: first parameter must directly point to a dataindex but its '{dataindex + BlastInterpretor.opt_id}' instead");
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
                Debug.LogError($"ssmd.set_bits: the mask to set may only be of vectorsize 1 p1 = {datatype}.{vector_size}");
                return;
            }
#endif

            bool mask_is_constant = pop_fx_into_ref<float>(ref code_pointer, fp_mask);

            // get value
#if DEVELOPMENT_BUILD || TRACE
            pop_op_meta_cp(code_pointer, out datatype, out vector_size);
            if (vector_size != 1)
            {
                Debug.LogError($"ssmd.set_bits: the value to set may only be of vectorsize 1 p1 = {datatype}.{vector_size}");
                return;
            }
#endif

            bool value_is_constant = pop_fx_into_ref<float>(ref code_pointer, fp_value);


#if DEVELOPMENT_BUILD || TRACE

            // check that all constant arrays are actually of all the same values 
            // this would indicate a serious bug in constant handling early 

            if (mask_is_constant)
            {
                float f1 = fp_mask[0];
                for (int i = 1; i < ssmd_datacount; i++)
                {
                    if (fp_mask[i] != f1)
                    {
                        Debug.LogError($"ssmd.set_bits: the masks is marked constant but is not equal for all ssmd records at {code_pointer}");
                        return;
                    }
                }
            }

            if (value_is_constant)
            {
                float f0 = fp_value[0];
                for (int i = 1; i < ssmd_datacount; i++)
                {
                    if (fp_value[i] != f0)
                    {
                        Debug.LogError($"ssmd.set_bits: the value is marked constant but is not equal for all ssmd records at {code_pointer}");
                        return;
                    }
                }
            }

#endif
            // setbit & setbits override the datatype of the target to bool32 
            BlastInterpretor.SetMetaData(in metadata, BlastVariableDataType.Bool32, 1, (byte)dataindex);

            if (mask_is_constant)
            {
                uint mask = ((uint*)fp_mask)[0];

                if (value_is_constant)
                {
                    // constant mask constant value 
                    if (((uint*)fp_value)[0] == 0)
                    {
                        mask = ~mask; // not it for clarity  

                        if (mask == 0)
                        {
                            // something & 0 == always zero 
                            for (int i = 0; i < ssmd_datacount; i++)
                            {
                                ((uint*)data[i])[dataindex] = 0;
                            }
                        }
                        else
                        if (mask == uint.MaxValue)
                        {
                            // all 1;s & something == copy value == nothing changes 
                        }
                        else
                        {
                            for (int i = 0; i < ssmd_datacount; i++)
                            {
                                uint current = ((uint*)data[i])[dataindex];
                                current = (uint)(current & mask);
                                ((uint*)data[i])[dataindex] = current;
                            }
                        }
                    }
                    else
                    {
                        if (mask == uint.MaxValue)
                        {
                            // or with all 1's => nothing changes 
                        }
                        else
                        {
                            for (int i = 0; i < ssmd_datacount; i++)
                            {
                                uint current = ((uint*)data[i])[dataindex];
                                current = (uint)(current | mask);
                                ((uint*)data[i])[dataindex] = current;
                            }
                        }
                    }
                }
                else
                {
                    // constant mask, variable value 
                    for (int i = 0; i < ssmd_datacount; i++)
                    {
                        uint current = ((uint*)data[i])[dataindex];
                        current = math.select((uint)(current | mask), (uint)(current & ~mask), ((uint*)fp_value)[i] == 0);
                        ((uint*)data[i])[dataindex] = current;
                    }
                }
            }
            else
            {
                if (value_is_constant)
                {
                    if (((uint*)fp_value)[0] == 0)
                    {
                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            uint current = ((uint*)data[i])[dataindex];
                            uint mask = ((uint*)fp_mask)[i];
                            current = (uint)(current & ~mask);
                            ((uint*)data[i])[dataindex] = current;
                        }
                    }
                    else
                    {
                        for (int i = 0; i < ssmd_datacount; i++)
                        {
                            uint current = ((uint*)data[i])[dataindex];
                            uint mask = ((uint*)fp_mask)[i];
                            current = (uint)(current | mask);
                            ((uint*)data[i])[dataindex] = current;
                        }
                    }
                }
                else
                {
                    // nothing is constant 
                    for (int i = 0; i < ssmd_datacount; i++)
                    {
                        uint current = ((uint*)data[i])[dataindex];
                        uint mask = ((uint*)fp_mask)[i];
                        current = math.select((uint)(current | mask), (uint)(current & ~mask), ((uint*)fp_value)[i] == 0);
                        ((uint*)data[i])[dataindex] = current;
                    }
                }
            }

            code_pointer--;
        }
    }
}
