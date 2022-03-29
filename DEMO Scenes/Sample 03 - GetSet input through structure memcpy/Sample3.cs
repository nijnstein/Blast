using NSS.Blast;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using Unity.Burst.CompilerServices;

public class Sample3 : MonoBehaviour
{
    [TextArea(5, 20)]
    public string Script = "#input a float 0\n#input b float 0\n\na = a * b;";
    public data Input = default;
    public data Output = default;


    /// <summary>
    /// a data structure matching the inputs 
    /// </summary>
    [Serializable] [StructLayout(LayoutKind.Sequential)]
    public struct data
    {
        public float a;
        public float b;
    }


    /// <summary>
    /// the last executed script 
    /// </summary>
    string last_script;


    /// <summary>
    /// previous data 
    /// </summary>
    data previous; 



    void Start()
    {
        last_script = string.Empty;
        previous = Input; 
        Blast.Initialize();         
    }

    unsafe void Update()
    {
        if (string.IsNullOrWhiteSpace(Script)) return;

        // prevent running constantly 
        if (string.Compare(Script, last_script, true) == 0
            &&
            previous.a == Input.a && previous.b == Input.b)
        {
            // script didnt change, dont update 
            return;
        }

        last_script = Script;
        previous = Input; 
                                
        // initialize script from inputs
        BlastScript script = BlastScript.FromText(Script);
        script.Prepare();            
        
        // copy data in 
        fixed(data* input = &Input)
        {
            script.SetData(input); 
        }

        // execute 
        if (script.Execute() == BlastError.success)
        {
            // and read the new data
            fixed (data* output = &Output)
            {
                script.GetData(output);
            }
        }
    }
}
