using NSS.Blast;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;

public class Sample11 : MonoBehaviour
{

    /// <summary>
    /// the script to execute
    /// </summary>
    [TextArea(5, 30)]
    public string Script = @"#input b32 bool32; 
#input b float; 
#input counter float 0; 
#input state float -1;
#input temp float 0; 

    # on initial state, determine start state with some random bit functions
    if(state < 0) then
    (
        # while in editor|trace we can output values to the log 
        if(b = 0) then ( debug(state); )

        # set the numeric temp to a value expressed in binary
        temp = 00000000_00000000_00000000_00000001; 
                                  
        # although a bitmask is assigned to temp, the compiler treats it as a normal numeric
        # reinterpret updates the metadata for temp to set datatype (bool32 in this case)
        # this is very different from a cast in c# as we reinterpret the original data and dont return the casted variable 
        reinterpret_bool32(temp);     
        
        # without the bool cast, any nor all would accept 1 parameter and they would work asif they were supplied numerics
        if(any(temp)) then
        (
            state = 2;     
        )
        
        if(all(temp)) then
        (
            state = 0; 
        )
    )

    switch(state)
    (
        case 0:
        (
            if(get_bit(b32, b)) then 
            ( 
                set_bit(b32, b, 0);  # unset bit at index b
                b = fmod(b + 1, 32); # increase index and keep it from 0 through 31
                set_bit(b32, b, 1);  # set bit at index b
            );
            counter = counter + 0.33; 
        )
       
        case 1:
        (
            counter = counter + 1; 
        )

        case 2:
        (   
            counter = counter + 1; 
        )
    );
                                                                                         
    if(counter > 555) then 
    (
        state = select(state + 1, 0, state >= 2); 
        counter = 0;
    );
";

    /// <summary>
    /// nr of data segments to execute
    /// </summary>
    const int DataCount = 32;

    /// <summary>
    /// the last executed script 
    /// </summary>
    string last_script;

    /// <summary>
    /// the compiled script object
    /// </summary>
    BlastScript bscript = null;
    
    /// <summary>
    /// data to process
    /// </summary>
    NativeArray<float> data_array = default;
    
    /// <summary>
    /// flag to hold error state
    /// </summary>
    bool error_state = true;

    /// <summary>
    /// overview of data after execution 
    /// </summary>
    [TextArea(10, 32)]
    public string DataView = "";

    // Start is called before the first frame update
    void Start()
    {
        Blast.Initialize();
      
    }

    private void OnDestroy()
    {
        if (data_array.IsCreated) data_array.Dispose(); 
    }

    [Serializable]
    [StructLayout(LayoutKind.Explicit, Size = 20)]
    public struct data
    {
        [FieldOffset(0)]
        public Bool32 b32;
        [FieldOffset(4)]
        public float b;
        [FieldOffset(8)]
        public float counter;
        [FieldOffset(12)]
        public float state;
        [FieldOffset(16)]
        public float temp;
    }

    // Update is called once per frame
    unsafe void Update()
    {
        if (string.IsNullOrWhiteSpace(Script)) return;

        // remember the last script so we dont run the same script twice 
        if (bscript == null || string.Compare(Script, last_script, true) != 0)
        {
            // first|changed - prepare script
            bscript = BlastScript.FromText(Script);
            last_script = Script;

            if (bscript.Prepare(true, false) != BlastError.success)
            {
                DataView = "Failed to compile input script, check the log for more details.";
                error_state = true;
                return;
            }
            else
            {
                error_state = false;
            }

            // setup datasegments 
            if (data_array.IsCreated)
            {
                data_array.Dispose();
            }
            data_array = new NativeArray<float>(DataCount * (bscript.Package.Package.DataSize >> 2), Allocator.Persistent);

            // initialize constants, in normal packaging mode, which is used here, these are located in the datasegment 
            // - this is wonky.. it will improve 
            bscript.Package.InitializeDataSegments(ref data_array, DataCount, bscript.PackageData.DataSize);

            // fill the variable fields 
            byte* p = (byte*)NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(data_array);
            for (int i = 0; i < 32; i++)
            {
                // fetch a pointer to our data mapping 
                data* pdata = (data*)(void*)&p[bscript.Package.Package.DataSize * i];

                // set datafields
                pdata->b32 = Bool32.From(1 << i);
                pdata->b = i;
                pdata->state = -1;
                pdata->counter = 0;
                pdata->temp = 0;
            }
        }
        if (error_state) return; 

        // execute the script
        BlastError result = bscript.Execute(data_array);

        if (result == BlastError.success)
        {
            // show results 
            StringBuilder sb = StringBuilderCache.Acquire();

            byte* p = (byte*)NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(data_array);

            for (int i = 0; i < DataCount; i++)
            {
                data* pdata = (data*)(void*)&p[bscript.Package.Package.DataSize * i];

                sb.AppendLine($"{pdata->b32.ToString()} {(int)pdata->b} {pdata->counter} {pdata->state}");
            }
            DataView = StringBuilderCache.GetStringAndRelease(ref sb);
        }
        else
        {
            DataView = $"Error: {result}, check the logs for more details";
            error_state = true; 
        }


    }
}
