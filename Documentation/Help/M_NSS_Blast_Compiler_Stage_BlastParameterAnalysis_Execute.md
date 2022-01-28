# BlastParameterAnalysis.Execute Method 
 

the parameter analyzer only looks at the parameters and their usage - !! it wont make any node changes !! - checks vectorsizes - checks input/output settings -

**Namespace:**&nbsp;<a href="N_NSS_Blast_Compiler_Stage">NSS.Blast.Compiler.Stage</a><br />**Assembly:**&nbsp;BLAST (in BLAST.dll) Version: 1.0.0

## Syntax

**C#**<br />
``` C#
public int Execute(
	IBlastCompilationData data
)
```


#### Parameters
&nbsp;<dl><dt>data</dt><dd>Type: <a href="T_NSS_Blast_Compiler_IBlastCompilationData">NSS.Blast.Compiler.IBlastCompilationData</a><br />current compilationdata</dd></dl>

#### Return Value
Type: <a href="https://docs.microsoft.com/dotnet/api/system.int32" target="_blank" rel="noopener noreferrer">Int32</a><br />0 for success, other exitcode = error

#### Implements
<a href="M_NSS_Blast_IBlastCompilerStage_Execute">IBlastCompilerStage.Execute(IBlastCompilationData)</a><br />

## See Also


#### Reference
<a href="T_NSS_Blast_Compiler_Stage_BlastParameterAnalysis">BlastParameterAnalysis Class</a><br /><a href="N_NSS_Blast_Compiler_Stage">NSS.Blast.Compiler.Stage Namespace</a><br />