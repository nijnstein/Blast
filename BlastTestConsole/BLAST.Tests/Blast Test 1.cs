using NSS.Blast;
using NSS.Blast.Compiler;
using System;
using Unity.Collections;
using Xunit;

namespace BlastTestConsole
{
    public class BlastTests1
    {
        [Theory]  
        [InlineData("value = 1 + 2;")]
        [InlineData("value = 1 * 2;")]
        [InlineData("value = 1 - 2;")]
        [InlineData("value = 1 / 2;")]
        public void BlastScript_FromText(string script)
        {
            BlastScript bs = BlastScript.FromText(script);
            Assert.True(bs.LanguageVersion == BlastLanguageVersion.BS1); 
        }

        [Fact]
        public void Blast_Create()
        {
            Blast blast = default; 
            try
            {
                blast = Blast.Create(Allocator.Persistent);

                Assert.True(blast.IsCreated);
            }
            finally
            {
                if(blast.IsCreated) blast.Destroy();
            }
        }


        [Theory]
        [InlineData("value = 1 + 2;")]
        [InlineData("value = 1 * 2;")]
        [InlineData("value = 1 - 2;")]
        [InlineData("value = 1 / 2;")]
        public void Blast_Compile(string code)
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

            blast.Destroy();

            Xunit.Assert.True(res.Success);
        }
    }
}