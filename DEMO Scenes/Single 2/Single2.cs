using NSS.Blast;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class Single2 : MonoBehaviour
{
    

    [TextArea(8, 20)]
    public string Script = @"

#input position float3 0 0 0

position = position + random(-0.1, 0.1);

position = min((10 10 10), max((-10, -10, -10), position));

";

    BlastScript script;


    void Start()
    {
        Blast.Initialize().Silent().Verbose().Trace();

        // prepare the script once 
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

        // the each frame, update current position 
        script["position"] = (float3)transform.position; 

        // execute the script
        BlastError result = script.Execute(); 
        if (result == BlastError.success)
        {
            // and set value 
            transform.position = (float3)script["position"];
        }
        else
        {
            Debug.LogError($"Error during script execution: {result}"); 
        }
       
    }
}
