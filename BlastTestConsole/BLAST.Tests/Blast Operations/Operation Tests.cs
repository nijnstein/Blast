using NSS.Blast;
using NSS.Blast.Compiler;
using NSS.Blast.Standalone;
using System;
using System.IO;
using Unity.Collections;
using Xunit;

namespace BlastTestConsole
{
    public class OperationTests
    {
        [Theory]
        [InlineData("a = 1 + 2;", 3)]
        [InlineData("a = -1 + 2;", 1)]
        [InlineData("a = 1 + -2;", -1)]

        [InlineData("a = 1 / 2;", 0.5f)]
        [InlineData("a = -1 / 2;", -0.5f)]
        [InlineData("a = 1 / -2;", -0.5f)]
        [InlineData("a = -1 / -2;", 0.5f)]

        [InlineData("a = 1 - 2;", -1)]
        [InlineData("a = -1 - 2;", -3)]
        [InlineData("a = 1 - -2;", 3)]
        [InlineData("a = -1 - -2;", 1)]

        [InlineData("a = 1 * 2;", 2)]
        [InlineData("a = -1 * 2;", -2)]
        [InlineData("a = 1 * -2;", -2)]
        [InlineData("a = -1 * -2;", 2)]

        public void BlastScript_Aritmetic_1(string code, float v1)
        {
            Blast blast = Blast.Create(Allocator.Persistent);
            
            BlastScript script = BlastScript.FromText(code);

            var options = new BlastCompilerOptions();
            options.AutoValidate = false;
            options.Optimize = true;
            options.CompileWithSystemConstants = true;
            options.ConstantEpsilon = 0.001f;
            options.DefaultStackSize = 64;
            options.EstimateStackSize = false;

            var res = BlastCompiler.Compile(blast, script, options);

            Xunit.Assert.True(res.IsOK, "failed to compile");

            unsafe
            {
                Xunit.Assert.True(res.Executable.Execute((IntPtr)blast.Engine) == 0, "failed to execute");
                Xunit.Assert.True(res.Executable.data[0] == v1, $"failed to validate data, should be {v1} but found {res.Executable.data[0]}");
            }

            blast.Destroy();
        }


    }
}