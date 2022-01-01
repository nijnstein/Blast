using NSS.Blast;
using NSS.Blast.Compiler;
using System;
using Unity.Collections;
using Xunit;

namespace BlastTestConsole
{
    public class BlastTests1
    {
        [Fact]
        public void CompileTest1()
        {

            Blast blast = Blast.Create(Allocator.Persistent);
            BlastScript script = BlastScript.FromText("value = 1 + 2;");

            var options = new BlastCompilerOptions();
            options.AutoValidate = false;
            options.Optimize = true;
            options.CompileWithSystemConstants = true;
            options.ConstantEpsilon = 0.001f;
            options.DefaultStackSize = 64;
            options.EstimateStackSize = false;

            var res = BlastCompiler.Compile(blast, script, options);

            blast.Destroy();

            Xunit.Assert.True(!res.Success);
        }
    }
}