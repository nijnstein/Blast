# BlastInterpretor.Execute Method (BlastEngineDataPtr, IntPtr, IntPtr)
 

execute bytecode

**Namespace:**&nbsp;<a href="N_NSS_Blast_Interpretor">NSS.Blast.Interpretor</a><br />**Assembly:**&nbsp;BLAST (in BLAST.dll) Version: 1.0.0

## Syntax

**C#**<br />
``` C#
public int Execute(
	BlastEngineDataPtr blast,
	IntPtr environment,
	IntPtr caller
)
```


#### Parameters
&nbsp;<dl><dt>blast</dt><dd>Type: <a href="T_NSS_Blast_BlastEngineDataPtr">NSS.Blast.BlastEngineDataPtr</a><br />engine data</dd><dt>environment</dt><dd>Type: <a href="https://docs.microsoft.com/dotnet/api/system.intptr" target="_blank" rel="noopener noreferrer">System.IntPtr</a><br />pointer to environment data in native memory</dd><dt>caller</dt><dd>Type: <a href="https://docs.microsoft.com/dotnet/api/system.intptr" target="_blank" rel="noopener noreferrer">System.IntPtr</a><br />pointer to caller data in native memory</dd></dl>

#### Return Value
Type: <a href="https://docs.microsoft.com/dotnet/api/system.int32" target="_blank" rel="noopener noreferrer">Int32</a><br />exit code

## See Also


#### Reference
<a href="T_NSS_Blast_Interpretor_BlastInterpretor">BlastInterpretor Structure</a><br /><a href="Overload_NSS_Blast_Interpretor_BlastInterpretor_Execute">Execute Overload</a><br /><a href="N_NSS_Blast_Interpretor">NSS.Blast.Interpretor Namespace</a><br />