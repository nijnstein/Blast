using NSS.Blast;
using NSS.Blast.Compiler;
using NSS.Blast.Interpretor;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

public class Sample8 : MonoBehaviour
{
    [TextArea(5, 20)]
    public string Script = "#input a float\n#input b float\n#input c float\na = b + c;";

    [TextArea(3, 20)]
    public string Results = "";

    public float b = 1;
    public float c = 1; 
    
    string last = null; 

    // Update is called once per frame
    void Update()
    {
        if (Script == last || string.IsNullOrEmpty(Script)) return;

        Results = "";
        last = Script;

        using (Blast blast = Blast.Create(Allocator.Persistent))
        {
            BlastScript script = BlastScript.FromText(Script);

            BlastCompilerOptions options = BlastCompilerOptions.Default.Trace();

            // compile into an intermediate structure
            IBlastCompilationData intermediate_data = BlastCompiler.Compile(blast.Engine, script, options);

            if (!intermediate_data.IsOK)
            {
                Results = intermediate_data.CompileErrorMessage();
                return;
            }

            //
            // IMPORTANT: we only need the native package and the memory it points to to execute stuff
            //
            BlastPackageData package = BlastCompiler.Package(intermediate_data, options);

            // set data on index 
            unsafe
            {
                package.Data[1] = b;
                package.Data[2] = c;
            }

            // interpret the bytecode 
            BlastInterpretor interpretor = default;

            interpretor.SetPackage(package);
            BlastError res = (BlastError)interpretor.Execute(blast.Engine);

            if (res != BlastError.success)
            {
                Results = $"Error executing script: {res}";
            }
            else
            {
                unsafe
                {
                    Results = $"data at index 0 = {package.Data[0]}";
                }
            }
        }
    }
}
