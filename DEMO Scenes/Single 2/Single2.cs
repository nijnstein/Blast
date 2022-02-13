using NSS.Blast;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class Single2 : MonoBehaviour
{
    BlastScript script;


    void Start()
    {
        Blast.Initialize();

        // prepare the script once 
        script = BlastScript.FromText("#input position float3 0 0 0\n position.x = position.x + 0.001;");


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
