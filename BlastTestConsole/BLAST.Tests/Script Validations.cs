using NSS.Blast;
using NSS.Blast.Compiler;
using System;
using System.IO;
using Unity.Collections;
using Xunit;

namespace BlastTestConsole
{
    public class BlastValidations
    {           
        bool Validate(string scriptfile)
        {
            string code = File.ReadAllText($"Blast Test Scripts/{scriptfile}");

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

            if (res.CanValidate)
            {
                blast.SyncConstants();
                validated = BlastCompiler.Validate(res, blast.Engine);
            }

            blast.Destroy();
            return validated;
        }


        [Fact]
        public void Blast_Validate_F1()
        {
            Xunit.Assert.True(Validate("F1.bs"));
        }


        [Fact]
        public void Blast_Validate_F2()
        {
            Xunit.Assert.True(Validate("F2.bs"));
        }

        [Fact]
        public void Blast_Validate_F3()
        {
            Xunit.Assert.True(Validate("F3.bs"));
        }

    }
}