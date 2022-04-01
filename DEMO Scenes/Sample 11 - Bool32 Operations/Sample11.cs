using NSS.Blast;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

public class Sample11 : MonoBehaviour
{

    /// <summary>
    /// the script to execute
    /// </summary>
    [TextArea(5, 20)]
    public string Script = @"#input b32 bool32; 
#input b float; 
#input counter float 0; 
#input state float 0; 

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
            counter = counter + 1; 
        )
       
        case 1:
        (
            counter = counter + 1; 
        )
    );

    if(counter > 1024) then 
    (
        state = fmod(state + 1, 2);
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
    NativeArray<data> data_array = default;
    
    /// <summary>
    /// flag to hold error state
    /// </summary>
    bool error_state = true;

    /// <summary>
    /// overview of data after execution 
    /// </summary>
    [TextArea(10, 20)]
    public string DataView = "";

    // Start is called before the first frame update
    void Start()
    {
        Blast.Initialize();

        // setup some data to process 
        data_array = new NativeArray<data>(DataCount, Allocator.Persistent);
        for (int i = 0; i < 32; i++)
        {
            data_array[i] = new data()
            {
                b32 = Bool32.From(1 << i),
                b = i
            };
        }
    }

    private void OnDestroy()
    {
        if (data_array.IsCreated) data_array.Dispose(); 
    }

    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct data
    {
        public Bool32 b32;
        public float b;
        public float counter;
        public float state; 
    }

    // Update is called once per frame
    void Update()
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
        }
        if (error_state) return; 

        // execute the script
        BlastError result = bscript.Execute(data_array);

        if (result == BlastError.success)
        {
            // show results 
            StringBuilder sb = StringBuilderCache.Acquire();
            for (int i = 0; i < DataCount; i++)
            {
                sb.AppendLine($"{data_array[i].b32.ToString()} {(int)data_array[i].b}");
            }
            DataView = StringBuilderCache.GetStringAndRelease(ref sb);
        }
        else
        {
            DataView = $"Error: {result}, check the logs for more details";
        }


    }
}
