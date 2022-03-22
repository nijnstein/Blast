using NSS.Blast;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

public class Sample6 : MonoBehaviour
{
    /// <summary>
    /// the script to execute
    /// </summary>
    [TextArea(5, 20)]
    public string Script = "#input a float4\n#input b float4\n#input c float4\na = a * b + c;";

    /// <summary>
    /// nr of data segments to execute
    /// </summary>
    public int DataCount = 12;

    /// <summary>
    /// the last executed script 
    /// </summary>
    string last_script;

    /// <summary>
    /// last used datacount
    /// </summary>
    int last_count; 

    /// <summary>
    /// overview of data after execution 
    /// </summary>
    [TextArea(10, 20)]
    public string DataView = "";  

    // Start is called before the first frame update
    void Start()
    {
        Blast.Initialize();   
    }


    /// <summary>
    /// 
    /// object to hold variables for one call
    /// - as blast only supports floats we can directly map this into a float*
    /// - structure layout must be sequential (padding on float size|32 bits)
    ///
    /// </summary>
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct data
    {
        public float4 a;
        public float4 b;
        public float4 c;
    }

    // Update is called once per frame
    void Update()
    {
        if (string.IsNullOrWhiteSpace(Script)) return;

        // remember the last script so we dont run the same script twice 
        if (string.Compare(Script, last_script, true) == 0 && DataCount == last_count)
        {
            // script didnt change, dont update 
            return;
        }

        last_script = Script;
        last_count = DataCount;
        DataView = ""; 

        // create a script from the text 
        BlastScript script = BlastScript.FromText(Script);

        // compile it 
        if (script.Prepare() != BlastError.success)
        {
            DataView = "Failed to compile input script, check the log for more details."; 
            return; 
        }

        // setup some data to process 
        NativeArray<data> data_array = new NativeArray<data>(DataCount, Allocator.Persistent);
        {
            for (int i = 0; i < DataCount; i++)
            {
                data_array[i] = new data()
                {
                    a = 100f * i,
                    b = 101f * i,
                    c = 100.1f * i
                };
            }

            // execute the script
            BlastError result = script.Execute(data_array);

            if (result == BlastError.success)
            {
                // show results 
                StringBuilder sb = StringBuilderCache.Acquire();
                for (int i = 0; i < DataCount; i++)
                {
                    sb.AppendLine($"a = {data_array[i].a}");
                }
                DataView = StringBuilderCache.GetStringAndRelease(ref sb);
            }
            else
            {
                DataView = $"Error: {result}, check the logs for more details";
            }
        }
        data_array.Dispose(); 
    }
}
