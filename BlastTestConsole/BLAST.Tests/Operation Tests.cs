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
        [InlineData("a = maxa(2, (1 -2 3));", 3)]
        [InlineData("a = maxa(1, 2, 3, 4, 5, 6);", 6)]
        [InlineData("a = maxa((3 -4 5), (12 -3), (33 3 3 5));", 33)]

        [InlineData("a = mina(1, 2, 3);", 1)]
        [InlineData("a = mina((1 -2 3));", 1)]
        [InlineData("a = mina(2, (1 2 3));", 1)]
        [InlineData("a = mina(1, 2, 3, -4, 5, 6);", -4)]
        [InlineData("a = mina(1, 2, 3, 4, 5, 6, 7, 8);", 1)]
        [InlineData("a = mina((3 4 5), (12 3), (-33 3 3 5));", -33)]

        [InlineData("a = abs(-1);", 1)]
        [InlineData("a = abs(2 - 4);", 2)]

        [InlineData("a = floor(1.3);", 1)]
        [InlineData("a = floor(1231.932);", 1231)]
        [InlineData("a = floor((12.9 23.3));", 12)]
        [InlineData("a = floor((12.22 11.22 21.33));", 12)]
        [InlineData("a = floor((-66.77 12.22 11.22 21.33));", -67)]

        [InlineData("a = ceil(1.3);", 2)]
        [InlineData("a = ceil(1231.932);", 1232)]
        [InlineData("a = ceil((12.9 23.3));", 13)]
        [InlineData("a = ceil((12.22 11.22 21.33));", 13)]
        [InlineData("a = ceil((-66.77 12.22 11.22 21.33));", -66)]

        [InlineData("a = frac(1.3);", 0.29999995)]
        [InlineData("a = frac(1231.932);", 0.93200684)]
        [InlineData("a = frac((12.9 23.3));", 0.8999996)]
        [InlineData("a = frac((12.22 11.22 21.33));", 0.22000027)]
        [InlineData("a = frac((-66.77 12.22 11.22 21.33));", 0.23000336)] // wierd but it is correct
        [InlineData("a = frac(-1.6);", 0.39999998f)] 

        [InlineData("a = sqrt(2);", 1.4142135)]
        [InlineData("a = sqrt((2 2));", 1.4142135)]
        [InlineData("a = sqrt((9 2));", 3)]
        [InlineData("a = sqrt((2 4 6 8));", 1.4142135)]

        [InlineData("a = rsqrt(2);", 0.70710677)]
        [InlineData("a = rsqrt((2 2));", 0.70710677)]
        [InlineData("a = rsqrt((9 2));", 0.33333334)]
        [InlineData("a = rsqrt((2 4 6 8));", 0.70710677)]

        [InlineData("a = sin(1);", 0.84147096)]
        [InlineData("a = sin(-1);", -0.84147096)]


        [InlineData("a = cos(1);", 0.5403023)]
        [InlineData("a = cos(-1);", 0.5403023)]

        [InlineData("a = tan(1);", 1.5574077)]
        [InlineData("a = tan(-1);", -1.5574077)]

        [InlineData("a = atan(1);", 0.7853982)]
        [InlineData("a = atan(-1);", -0.7853982)]

        [InlineData("a = sinh(0.84147096);", 0.9443504)]
        [InlineData("a = sinh(-0.84147096);", -0.9443504)]

        [InlineData("a = cosh(1);", 1.5430807)]
        [InlineData("a = cosh(-1);", 1.5430807)]

        [InlineData("a = 3 * 4 + 1.2;", 13.2f)]                             /// should compile to FMA through optimizer..  TODO we should be able to test the opcodes used to compile something
        [InlineData("a = fma(3, 4, 1.2);", 13.2f)]                          /// force use of fma by directly calling the op 
        [InlineData("a = fma((3 3), (4 4), (1.2 1.2));", 13.2f)]            /// force use of fma by directly calling the op 
        [InlineData("a = fma((3 3 3), (4 4 4), (1.2 1.2 1.2));", 13.2f)]    /// force use of fma by directly calling the op 
        [InlineData("a = (3 3 3) * (4 4 4) * (1.2 1.2 1.2);", 14.400001f)]       /// verification of normal ops

        [InlineData("a = (1 2 3) * (4 5 6) * (7 8 9) * (9.9 9.1 9.2);", 277.19998)] // should optimize to mula
        [InlineData("a = mula((3 3 3), (4 4 4), (1.2 1.2 1.2), (9.9 9.1 9.2), (5 5 3), (92 22 4455));", 65577.6)]

        [InlineData("a = 1 * 2 * 3 * 4 * 5 * 6 * 7;", 5040)]
        [InlineData("a = mula(1, 2, 3, 4, 5, 6, 7);", 5040)]

        [InlineData("a = (3 3 3 5) * (4 4 4 9) * (1.2 1.2 1.2 4) * (9.9 9.1 9.2 5) * (5 5 3 5) * (92 22 4455 555);", 65577.6f)]       // again should optimize to mula
        [InlineData("a = mula((3 3 3 5), (4 4 4 9), (1.2 1.2 1.2 4), (9.9 9.1 9.2 5), (5 5 3 5), (92 22 4455 555));", 1)]

        
        [InlineData("a = 2 + 3 + (3 + 4) + 1;", 13)]  // flatten should flatten this but we have no way to verify it other then checking the result   YES
        [InlineData("a = 2 + 3 + (3 - 4) + 1;", 5)]   // flatten may flatten this but probably wont TODO -> the optimizer SHOULD flatten it TODO !!   HALF -> it is popped.. not needed in this case 
        [InlineData("a = 2 + 3 - (3 + 4) + 1;", -1)]  // flatten should not be able to flatten this out
        [InlineData("a = 2 + 3 - (3 - 4) + 1;", 7)]   // flatten should not be able to flatten this out

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
        [InlineData("a = 34 * 55 * 555 * 2 + 10.2;", 2075710.2)]
        [InlineData("a = (23 34 33) * (55 555 2) + 10.2;", 1275.2)]
        public void BlastScript_Benchmarks_1(string code, float v1)
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
        [InlineData("a = (3 4 5) * -1;", -3, -4, -5)]
        [InlineData("a = abs((3 4 ) * -1);", 3, 4, float.NaN)]
        [InlineData("a = abs((3 4 5) * -1);", 3, 4, 5)]
        [InlineData("a = abs((3 4 5 6) * -1);", 3, 4, 5)]
        [InlineData("a = abs((3, -4)); b = 1;", 3, 4, 1)]
        [InlineData("a = abs((3 4, -5));", 3, 4, 5)]
        [InlineData("a = abs((3 4 5, -6));", 3, 4, 5)]
        
        [InlineData("a = sin((1 1, -1));", 0.84147096, 0.84147096, -0.84147096)]


        [InlineData("a = cos((1 1 1));", 0.5403023, 0.5403023, 0.5403023)]
        [InlineData("a = cos((-1 1 1));", 0.5403023, 0.5403023, 0.5403023)]

        [InlineData("a = tan((1 1 1));", 1.5574077, 1.5574077, 1.5574077)]
        [InlineData("a = tan((-1, -1, -1));", -1.5574077, -1.5574077, -1.5574077)]

        [InlineData("a = atan((1 1 1));", 0.7853982, 0.7853982, 0.7853982)]
        [InlineData("a = atan((-1 1 1));", -0.7853982, 0.7853982, 0.7853982)]

        [InlineData("a = sinh((0.84147096 0.84147096 0.84147096));", 0.9443504, 0.9443504, 0.9443504)]
        [InlineData("a = sinh((-0.84147096, -0.84147096, -0.84147096));", -0.9443504, -0.9443504, -0.9443504)]

        [InlineData("a = cosh((1 1 1));", 1.5430807, 1.5430807, 1.5430807)]
        [InlineData("a = cosh((-1, -1, -1));", -1.5430807, -1.5430807, -1.5430807)]

        public void BlastScript_Functions_2(string code, float v1, float v2, float v3)
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

                //check stack, shoud be 24 24 24
                Xunit.Assert.True(Validations.ValidateStack(res, v1, v2, v3));
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


        [Theory]
        [InlineData("Features/Stack/Stack 1.bs")]
        [InlineData("Features/Stack/Stack 2.bs")]
        [InlineData("Features/Stack/Stack 3.bs")]
        public void Blast_Stack_2(string scriptfile)
        {
            Xunit.Assert.True(Validations.Validate(scriptfile));
        }


        [Theory]
        [InlineData("Features/F1.bs")]
        [InlineData("Features/F2.bs")]
        [InlineData("Features/F3.bs")]
        [InlineData("Features/Minus 1.bs")]
        [InlineData("Features/Minus 2.bs")]
        [InlineData("Features/Minus 3.bs")]
        [InlineData("Features/Mina 1.bs")]
        public void Blast_Basic(string scriptfile)
        {
            Xunit.Assert.True(Validations.Validate(scriptfile));
        }

   

        [Theory]
        [InlineData("Features/Vector 3.bs", 24, 24, 24)]
        [InlineData("Features/Vector 4.bs", 8, 9, 10)]
        [InlineData("Features/Vector 5.bs", 3, 1, 2)]
        [InlineData("Features/Vector 6.bs", 9, 5, 6)]
        public void BlastScript_Vectors_1(string scriptfile, float v1, float v2, float v3)
        {
            Blast blast = Blast.Create(Allocator.Persistent);
            string code = File.ReadAllText($"Blast Test Scripts/{scriptfile}");

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

                //check stack, shoud be 24 24 24
                Xunit.Assert.True(Validations.ValidateStack(res, v1, v2, v3));
            }


            blast.Destroy();
        }

        [Theory]
        [InlineData("a = (1, -667 3);", 1, -667, 3)] // fails, it reduces num[3] to num[2] because it evals 1-667
        [InlineData("a = (1 - 2); b = (3 4);", -1, 3, 4)]   // first part should not be a vector but eval as operation 1-2 = -1 
        public void BlastScript_Vectors_2(string code, float v1, float v2, float v3)
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

                //check stack, shoud be 24 24 24
                Xunit.Assert.True(Validations.ValidateStack(res, v1, v2, v3));
            }


            blast.Destroy();
        }



    }
}