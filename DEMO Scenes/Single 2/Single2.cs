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

position.x = position.x + random(-0.01, 0.01);
position.y = position.y + random(-0.01, 0.01);
position.z = position.z + random(-0.01, 0.01);

position = min((100 100 100), position);
position = max((-100 -100 -100), position);

";
    BlastScript script;


    void Start()
    {
        Blast.Initialize();

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
