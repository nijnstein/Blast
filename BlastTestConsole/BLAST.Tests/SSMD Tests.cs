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
    public class SSMDTests
    {

        [Theory]
        [InlineData("a = 34 * 55 * 555 * 2 + 10.2;", 32)]
        unsafe public void SSMD_Tests_Operations(string code, int nruns)
        {
            Blast blast = Blast.Create(Allocator.Temp);

            BlastCompilerOptions options = new BlastCompilerOptions();
            options.PackageMode = BlastPackageMode.SSMD;
            options.PackageStack = false;

            BlastScriptPackage package = blast.Package(code, options);

            byte* data = stackalloc byte[package.Package.SSMDDataSize * nruns];
           
            BlastSSMDDataStack* ssmd_data = (BlastSSMDDataStack*)(void*)data; 
            package.Package.CloneDataStack(nruns, ssmd_data);

            blast.Execute(package.Package, IntPtr.Zero, ssmd_data, nruns);
            blast.Destroy();          
        }

        [Theory]
        [InlineData("a = 1 + 2;", 32, 3)]
        [InlineData("a = fma(2,4,3);", 32, 11)]     // the variants are for constant parameter testing
        [InlineData("a = fma(3,5,2);", 32, 17)]     /// looks like a constant data variable location in data is not counted, this variant should have 2 parameters, the second being the 5 which is no encoded constant
        [InlineData("a = fma(2,8,4);", 32, 20)]

        [InlineData("a = fma((2 2), (8 3), (4 3));", 32, 20)]
        [InlineData("a = fma((2 2 2), (8 3 1), (4 3 2));", 32, 20)]
        [InlineData("a = fma((2 2 2 2), (8 3 1 1), (4 3 2 1));", 32, 20)]


        unsafe public void SSMD_Tests_Functions(string code, int nruns, float f1)
        {
            Blast blast = Blast.Create(Allocator.Temp);

            BlastCompilerOptions options = new BlastCompilerOptions();
            options.PackageMode = BlastPackageMode.SSMD;
            options.PackageStack = false;

            BlastScriptPackage package = blast.Package(code, options);

            byte* data = stackalloc byte[package.Package.SSMDDataSize * nruns];

            BlastSSMDDataStack* ssmd_data = (BlastSSMDDataStack*)(void*)data;
            package.Package.CloneDataStack(nruns, ssmd_data);

            blast.Execute(package.Package, IntPtr.Zero, ssmd_data, nruns);

            for(int i = 0; i < nruns;i++)
            {
                float* p = (float*)(data + package.Package.SSMDDataSize * i);
                Xunit.Assert.True(p[0] == f1, $"expected {f1} but found result {p[0]}");
            }

            blast.Destroy();
        }


    }
}
            