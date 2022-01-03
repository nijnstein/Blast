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
            Blast blast = Blast.Create(Allocator.Persistent);
            try
            {
                string code = File.ReadAllText($"Blast Test Scripts/{scriptfile}");

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

                return validated;
            }
            catch
            {
                return false; 
            }
            finally
            {
                if (blast.IsCreated)
                {
                    blast.Destroy();
                }
            }
        }


        [Theory]
        [InlineData("Features/F1.bs")]
        [InlineData("Features/F2.bs")]
        [InlineData("Features/F3.bs")]
        public void Blast_Validate_Basic(string scriptfile)
        {
            Xunit.Assert.True(Validate(scriptfile));
        }

        /// <summary>
        /// test validations that should always fail 
        /// </summary>
        [Theory]
        [InlineData("Validation/Validation Fail 1.bs")]
        [InlineData("Validation/Validation Fail 2.bs")]
        [InlineData("Validation/Validation Fail 3.bs")]
        [InlineData("Validation/Validation Fail 4.bs")]
        public void Blast_ValidationFailures(string scriptfile)
        {
            Xunit.Assert.False(Validate(scriptfile));
        }

        [Theory]
        [InlineData("Validation/Validate Input Output.bs")]
        public void Blast_Validation_InputOutput(string scriptfile)
        {
            Xunit.Assert.True(Validate(scriptfile));
        }

        [Theory]
        [InlineData("Validation/Validate Input Output Failure.bs")]
        public void Blast_Validation_InputOutput_Failure(string scriptfile)
        {
            Xunit.Assert.False(Validate(scriptfile));
        }

        [Fact]
        public void Blast_Validate_VariableTypes()
        {
            Xunit.Assert.True(Validate("F4 - DataTypes.bs"));
        }

    }
}