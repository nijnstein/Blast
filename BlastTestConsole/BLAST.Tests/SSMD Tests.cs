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
        [InlineData("a = 1 + 2;", 32)]
        [InlineData("a = 34 * 55 * 555 * 2 + 10.2;", 32)]
        unsafe public void SSMD_Tests_Operations(string code, int nruns)
        {
            Blast blast = Blast.Create(Allocator.Temp); 

            BlastScriptPackage package = blast.Package(code, new BlastCompilerOptions(BlastPackageMode.SSMD));

            byte* data = stackalloc byte[package.Package.SSMDDataSize * nruns];
           
            BlastSSMDDataStack* ssmd_data = (BlastSSMDDataStack*)(void*)data; 
            package.Package.CloneDataStack(nruns, ssmd_data);

            blast.Execute(package.Package, IntPtr.Zero, ssmd_data, nruns);
            blast.Destroy();          
        }



    }
}
            