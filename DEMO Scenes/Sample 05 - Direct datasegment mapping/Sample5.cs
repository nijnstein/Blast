using NSS.Blast;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Mathematics;
using UnityEngine;

public class Sample5 : MonoBehaviour
{
    [TextArea(5, 20)]
    public string Script = "#input a float4 1 1 1 1\n#input b float 2\n\na = a * b;";

    public data InputAndOutput = default; 

 
    /// <summary>
    /// the last executed script 
    /// </summary>
    string last_script;

    /// <summary>
    /// a data structure matching the inputs 
    /// </summary>
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct data
    {
        public float4 a;
        public float b;
    }


    void Start()
    {
        last_script = string.Empty;
        Blast.Initialize();
    }

    // Update is called once per frame
    unsafe void Update()
    {
        if (string.IsNullOrWhiteSpace(Script)) return;

        // remember the last script so we dont run the same script twice 
        if (string.Compare(Script, last_script, true) == 0)
        {
            // script didnt change, dont update 
            return;
        }
        last_script = Script;

        // compile the script
        BlastScript script = BlastScript.FromText(Script);
        if (script.Prepare() == BlastError.success)
        {
            // execute using the data structure as data segment 
            fixed (void* p = &InputAndOutput)
            {
                script.Execute(p);
            }
        }

    }
}
