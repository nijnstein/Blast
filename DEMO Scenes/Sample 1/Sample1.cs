using NSS.Blast;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class Sample1 : MonoBehaviour
{
    

    [TextArea(8, 20)]
    public string Script = @"

#input position float3 0 0 0

position = position + random((-0.1, -0.1, -0.1), (0.1 0.1 0.1));

position = min((20 20 20), max((-20, -20, -20), position));

";

    BlastScript script;


    void Start()
    {
        // initialize the static blast instance 
        Blast.Initialize();

        // prepare the script once from text input
        // - this compiles the bytecode and makes any variables known so we can read/write (to) them 
        script = BlastScript.FromText(Script);

        BlastError result = script.Prepare();
        if (result != BlastError.success)
        {
            Debug.LogError($"Error during script compilation: {result}");
            script = null;
        }
    }

    void Update()
    {
        if (script == null) return;
        if (!Blast.IsInstantiated) return;

        // set current position 
        script["position"] = transform.position;

        // execute the script
        BlastError result = script.Execute(); 
        if (result == BlastError.success)
        {
            // and get updated positon 
            transform.position = (float3)script["position"];
        }
        else
        {
            Debug.LogError($"Error during script execution: {result}"); 
        }
       
    }
}
