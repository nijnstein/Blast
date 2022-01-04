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

        [InlineData("a = 1 * 2 / 10;", 0.2f)]
        [InlineData("a = 10 / 1 * 2;", 20)]
        [InlineData("a = 10 + 5 / 1 * 2;", 20)]  // we could remove any division by 1 but who would use that.. 
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

        [Theory]
        [InlineData("a = 1 * -2;", -2)]
        [InlineData("a = -1 * 2;", -2)]
        [InlineData("a = 1 + 2 * 2 + 5;", 10)]             // if NOT terminated with a ; -> silent error -> should fail hard 
        [InlineData("a = 1 - 1 - 1 - 1 - 1;", -3)]
        [InlineData("a = 1 - 1 + 1 - 1 + 1;", 1)]
        [InlineData("a = 2 * 2 * 2 * 2 * 2;", 32)]
        [InlineData("a = 1 + 1 * (1 / 2) * 2;", 2)]
        [InlineData("a = 2 * (2 * 2 * 2) * 2;", 32)]
        [InlineData("a = 1 * 10 * 3 * (3 + 4 * 2);", 330)]
        [InlineData("a = 1 * (2 * 3 / 4) * 5;", 7.5f)]
        public void BlastScript_Aritmetic_2(string code, float v1)
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


        [Theory]
        // test up until 8 parameters for each vectorsize .. 
        [InlineData("a = max(1, 2);", 2)]
        [InlineData("a = max(1, 2, 3);", 3)]
        [InlineData("a = max(1, 2, 3, 4);", 4)]
        [InlineData("a = max(1, 2, 3, 4, 5);", 5)]
        [InlineData("a = max(1, 2, 3, 4, 5, 6);", 6)]
        [InlineData("a = max(1, 2, 3, 4, 5, 6, 7);", 7)]
        [InlineData("a = max(1, 2, 3, 4, 5, 6, 7, 8);", 8)]

        [InlineData("a = max(12345, 2.3, 4445, -553, 10101111);", 10101111)]
        [InlineData("a = max(8, 1, -111, 20);", 20)]

        // test up until 8 parameters for each vectorsize .. 
        [InlineData("a = min(1, 2);", 1)]
        [InlineData("a = min(1, 2, 3);", 1)]
        [InlineData("a = min(1, 2, 3, 4);", 1)]
        [InlineData("a = min(1, 2, 3, 4, 5);", 1)]
        [InlineData("a = min(1, 2, 3, 4, 5, 6);", 1)]
        [InlineData("a = min(1, 2, 3, 4, 5, 6, 7);", 1)]
        [InlineData("a = min(1, 2, 3, 4, 5, 6, 7, 8);", 1)]

        [InlineData("a = min(-1, -2, -111, 220, 0);", -111)]
        [InlineData("a = min(3234, 234, 121, 888888, 0, 222);", 0)]

        [InlineData("a = maxa(1, 2, 3);", 3)]
        [InlineData("a = maxa((1 2 3));", 3)]
        [InlineData("a = maxa(2, (1, -2, 3));", 3)]
        [InlineData("a = maxa(1, 2, 3, 4, 5, 6);", 6)]
        [InlineData("a = maxa((3, -4, 5), (12, -3), (33, 3, 3, 5));", 33)]

        [InlineData("a = mina(1, 2, 3);", 1)]
        [InlineData("a = mina((1, -2, 3));", 1)]
        [InlineData("a = mina(2, (1, 2, 3));", 1)]
        [InlineData("a = mina(1, 2, 3, -4, 5, 6);", -4)]
        [InlineData("a = mina(1, 2, 3, 4, 5, 6, 7, 8);", 1)]
        [InlineData("a = mina((3, 4, 5), (12, 3), (-33, 3, 3, 5));", -33)]

        public void BlastScript_Functions_1(string code, float v1)
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


        [Theory]
        [InlineData("push(321); a = pop;", 321)]
        [InlineData("push(321); push(555); push(444); a = pop; a = pop;", 555)]
        public void BlastScript_Stack_1(string code, float v1)
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