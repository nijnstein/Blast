using NSS.Blast;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class Sample4 : MonoBehaviour
{
    [TextArea(5, 20)]
    public string Script = "#input a float4 1 1 1 1\n#input b float 2\n\na = a * b;";

    [TextArea(10, 20)]
    public string DataView = "";  



    /// <summary>
    /// the last executed script 
    /// </summary>
    string last_script;




    void Start()
    {
        last_script = string.Empty;
        Blast.Initialize();
    }            

    
    void Update()
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
        if(script.Prepare() == BlastError.success)
        {

            unsafe
            {
                float* data = script.Package.Package.Data;

                // cast as float4 to directly set a float 4 
                ((float4*)data)[0] = new float4(1, 2, 3, 4);

                // or set compoments seperately: 
                data[0] = 1f;
                data[1] = 2f;
                data[2] = 3f;
                data[3] = 4f;

                // 
                // - you can use script.Package.VariableOffsets to directly get the offset
                //   of a variable id (the id == the index)
                //
                // - input & output fields are always the first fields in the datasegment
                // 
                data[4] = 5f;
            }

            // execute it 
            if (script.Execute() == BlastError.success)
            {
                //
                // - show overview of datasegment 
                // 
                DataView = script.Package.GetDataSegmentText(); 
            }
        }




    }
}
