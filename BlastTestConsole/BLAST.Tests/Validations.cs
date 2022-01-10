using NSS.Blast;
using NSS.Blast.Compiler;
using NSS.Blast.Standalone;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Unity.Collections;
using Xunit;

namespace BlastTestConsole
{
    public class Validations
    {
        static unsafe public bool ValidateStack(CompilationData data, float v1, float v2, float v3)
        {
            if ((data.Executable.data[0] == v1 || float.IsNaN(v1))
                &&
                (data.Executable.data[1] == v2 || float.IsNaN(v2))
                &&
                (data.Executable.data[2] == v3 || float.IsNaN(v3)))
            {
                return true;
            }

            Debug.LogError($"failed to validate data, should be {v1} {v2} {v3} but found {data.Executable.data[0]} {data.Executable.data[1]} {data.Executable.data[2]}");
            return false;
        }
        static unsafe public bool ValidateStack(CompilationData data, float v1, float v2)
        {
            if ((data.Executable.data[0] == v1 || float.IsNaN(v1))
                &&
                data.Executable.data[1] == v2 || float.IsNaN(v2))
            {
                return true;
            }

            Debug.LogError($"failed to validate data, should be {v1} {v2} but found {data.Executable.data[0]} {data.Executable.data[1]}");
            return false;
        }
        static public bool Validate(string scriptfile)
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
            catch (Exception ex)
            {
                NSS.Blast.Standalone.Debug.LogWarning("EXCEPTION: " + ex.ToString());
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
            Xunit.Assert.False(Validations.Validate(scriptfile));
        }

        [Theory]
        [InlineData("Validation/Validate Input Output.bs")]
        public void Blast_Validation_InputOutput(string scriptfile)
        {
            Xunit.Assert.True(Validations.Validate(scriptfile));
        }

        [Theory]
        [InlineData("Validation/Validate Input Output Failure.bs")]
        public void Blast_Validation_InputOutput_Failure(string scriptfile)
        {
            Xunit.Assert.False(Validations.Validate(scriptfile));
        }

        [Fact]
        public void Blast_Validate_VariableTypes()
        {
            Xunit.Assert.True(Validations.Validate("F4 - DataTypes.bs"));
        }
    }
}
            