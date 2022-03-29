using AOT;
using NSS.Blast;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

[BurstCompile]
public class Sample9 : MonoBehaviour
{

    [BurstCompile]
    unsafe static public float test1(float a, float b)
    {
        return a * b; 
    }

    [BurstCompile]
    unsafe static public float test2(IntPtr blast, IntPtr environment, IntPtr caller, float a, float b)
    {
        var env = (environmental_data*)environment; 
        return env->id + (a * b); 
    }

    [BurstCompile]
    unsafe static public float test3(IntPtr blast, IntPtr environment, IntPtr caller, float a, float b)
    {
        return a * b;  
    }

    public float a = 2;
    public float b = 2;


    [BurstCompile]
    [Serializable]
    public struct environmental_data
    {
        // any unmanaged data can be put in environment
        public int id;
        public float4x4 value;
    }
    public environmental_data environment = new environmental_data() { id = 2048, value = 1f };



    [TextArea(8, 20)]
    public string Script = "#input a float\n#input b float\na = test1(a, b) + test2(a, b) + test3(a, b)";

    BlastScript script;
    string last_script = null;

    void Start()
    {
        // manually register external functions
        Blast.ScriptAPI.Register(test1);
        Blast.ScriptAPI.Register(test2);
        Blast.ScriptAPI.Register(test3);

        // initialize the static blast instance 
        Blast.Initialize();
    }

    void Update()
    {
        if (script == null) return;
        if (!Blast.IsInstantiated) return;
        if (string.Compare(Script, last_script, true) == 0) return;

        // prepare the script once from text input
        // - this compiles the bytecode and makes any variables known so we can read/write (to) them 
        script = BlastScript.FromText(Script);
        last_script = Script;

        BlastError result = script.Prepare();
        if (result != BlastError.success)
        {
            Debug.LogError($"Error during script compilation: {result}");
            script = null;
        }

        // set current position 
        script["a"] = a;
        script["b"] = b;

        // execute the script with environment data
        unsafe
        {
            fixed(environmental_data* penv = &environment)
            {
                result = script.Execute(Blast.Instance.Engine, (IntPtr)penv, IntPtr.Zero);
                if (result == BlastError.success)
                {
                    // and get updated data back 
                    a = (float)script["a"];
                }
                else
                {
                    Debug.LogError($"Error during script execution: {result}");
                }
            }
        }

 
    }
}
