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
                if (blast.IsCreated) blast.Destroy();
            }
        }

        [Fact]
        public void Blast_SyncConstants()
        {
            Blast blast = default;
            try
            {
                blast = Blast.Create(Allocator.Persistent);
                blast.SyncConstants(); 

                Assert.True(blast.IsCreated);
            }
            finally
            {
                if (blast.IsCreated) blast.Destroy();
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

        [Fact]
        public void Blast_Validate()
        {
            string code = @"
a = 1;
b = 1 + a;	   

#validate b 2";

            Blast blast = Blast.Create(Allocator.Persistent);
            BlastScript script = BlastScript.FromText(code);

            var options = new BlastCompilerOptions();
            options.AutoValidate = false;
            options.Optimize = true;
            options.CompileWithSystemConstants = true;
            options.ConstantEpsilon = 0.001f;
            options.DefaultStackSize = 64;
            options.EstimateStackSize = false;
            
            bool validated = false;
            var res = BlastCompiler.Compile(blast, script, options);

            if(res.CanValidate)
            {
                blast.SyncConstants();
                validated = BlastCompiler.Validate(res, blast.Engine); 
            }

            blast.Destroy();

            Xunit.Assert.True(validated);
        }
    }
}