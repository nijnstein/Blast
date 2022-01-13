using NSS.Blast;
using NSS.Blast.Compiler;
using System;
using System.IO;
using Unity.Collections;
using Xunit;
using NSS.Blast.Standalone; 

namespace BlastTestConsole
{
    public class BlastTests
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

        [Theory]
        [InlineData("features/debug.bs")]
        [InlineData("features/debugstack.bs")]
        public void BlastScript_Debug(string scriptfile)
        {
            Blast blast = Blast.Create(Allocator.Persistent);
            string code = File.ReadAllText($"Blast Test Scripts/{scriptfile}");

            BlastScript script = BlastScript.FromText(code);

            var options = new BlastCompilerOptions();
            options.AutoValidate = false;
            options.DefaultStackSize = 64;
            options.EstimateStackSize = false;

            var res = BlastCompiler.Compile(blast, script, options);

            Xunit.Assert.True(res.IsOK, "failed to compile");

            unsafe
            {
                Xunit.Assert.True(res.Executable.Execute((IntPtr)blast.Engine) == 0, "failed to execute"); 
            }            

            blast.Destroy();
        }

        [Theory]
        [InlineData("Features/Vector 1.bs")]
        [InlineData("Features/Vector 2.bs")]
        [InlineData("Features/Vector 3.bs")]
        public void BlastScript_Vectors(string scriptfile)
        {
            Blast blast = Blast.Create(Allocator.Persistent);
            string code = File.ReadAllText($"Blast Test Scripts/{scriptfile}");

            BlastScript script = BlastScript.FromText(code);

            var options = new BlastCompilerOptions();
            options.AutoValidate = false;
            options.DefaultStackSize = 64;
            options.EstimateStackSize = false;

            var res = BlastCompiler.Compile(blast, script, options);

            Xunit.Assert.True(res.IsOK, "failed to compile");

            unsafe
            {
                Xunit.Assert.True(res.Executable.Execute((IntPtr)blast.Engine) == 0, "failed to execute");


                NSS.Blast.Standalone.Debug.Log(res.Executable.data[res.Executable.DataByteSize / 4].ToString()); 
            }


            blast.Destroy();
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

            var options = new BlastCompilerOptions(64);
            options.DefaultStackSize = 64;
            options.EstimateStackSize = false;

            var res = BlastCompiler.Compile(blast, script, options);
            blast.Destroy();

            Xunit.Assert.True(res.Success);
        }

        [Theory]
        [InlineData("value = random(1, 2);")]
        [InlineData("value = random(10);")]
        [InlineData("value = random(100)")]
        [InlineData("value = random(-100, -90);")]
        public void Blast_Random(string code)
        {
            Assert.True(Blast.Execute(code) == 0);
        }

        [Theory]
        [InlineData("value = random(1, 2);")]
        public void Blast_Package(string code)
        {

            ///  THE PACKEGER IS COMPLETELY BROKEN :=)

            BlastScriptPackage package = Blast.Package(code);
            Assert.True(Blast.Execute(package) == 0); 
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
            options.DefaultStackSize = 64;
            options.EstimateStackSize = false;

            bool validated = false;
            var res = BlastCompiler.Compile(blast, script, options);

            if (res.CanValidate)
            {
                validated = BlastCompiler.Validate(res, blast.Engine);
            }

            blast.Destroy();

            Xunit.Assert.True(validated);
        }

  

    }
}