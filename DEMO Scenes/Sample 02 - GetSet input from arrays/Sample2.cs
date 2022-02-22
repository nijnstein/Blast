using NSS.Blast;
using NSS.Blast.Compiler;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class Sample2 : MonoBehaviour
{
    /// <summary>
    /// the script to run 
    /// </summary>
    [TextArea(8, 20)]
    public string Script = "#input a float 1.88\n\na = a + 1;";


    public bool Verbose = false; 

    /// <summary>
    /// the values to input 
    /// </summary>
    public float[] Values;

    /// <summary>
    /// results 
    /// </summary>
    [TextArea(3, 10)]
    public string Results;

    /// <summary>
    /// script that was ran last 
    /// </summary>
    string last_script;



    private void Start()
    {
        last_script = string.Empty;
    }



    // Update is called once per frame
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

        if (Verbose)
        {
            Blast.ReInitialize().Verbose();
        }
        else
        {
            Blast.ReInitialize().Silent();
        }

        // initialize script from inputs
        BlastScript script = BlastScript.FromText(Script);


        // 'prepare' it, compiles the script and prepares parameters 
        BlastError error = script.Prepare();
        if (error != BlastError.success)
        {
            Results = $"ERROR: {error}";
            return;
        }

        // set input(s)
        if (Values.Length > 0)
        {
            script.SetData(Values); 
        }

        // execute 
        error = script.Execute();

        // check for error condition 
        if (error != BlastError.success)
        {
            Results = $"ERROR: {error}, please see the log for details";
            return;
        }

        // success, show results 
        var s = StringBuilderCache.Acquire();
        foreach (var result in script.GetData())
        {
            s.AppendLine(result.ToString());
        }

        Results = StringBuilderCache.GetStringAndRelease(ref s);
    }




}
