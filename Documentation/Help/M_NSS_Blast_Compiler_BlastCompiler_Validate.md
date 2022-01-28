# BlastCompiler.Validate Method 
 

Validate output using data set in script for NULL inputs

**Namespace:**&nbsp;<a href="N_NSS_Blast_Compiler">NSS.Blast.Compiler</a><br />**Assembly:**&nbsp;BLAST (in BLAST.dll) Version: 1.0.0

## Syntax

**C#**<br />
``` C#
public static bool Validate(
	IBlastCompilationData result,
	IntPtr blast
)
```


#### Parameters
&nbsp;<dl><dt>result</dt><dd>Type: <a href="T_NSS_Blast_Compiler_IBlastCompilationData">NSS.Blast.Compiler.IBlastCompilationData</a><br />bytecode compiler data</dd><dt>blast</dt><dd>Type: <a href="https://docs.microsoft.com/dotnet/api/system.intptr" target="_blank" rel="noopener noreferrer">System.IntPtr</a><br />blast engine data</dd></dl>

#### Return Value
Type: <a href="https://docs.microsoft.com/dotnet/api/system.boolean" target="_blank" rel="noopener noreferrer">Boolean</a><br />true if validation succeeded

## See Also


#### Reference
<a href="T_NSS_Blast_Compiler_BlastCompiler">BlastCompiler Class</a><br /><a href="N_NSS_Blast_Compiler">NSS.Blast.Compiler Namespace</a><br />