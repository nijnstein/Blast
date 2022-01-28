# Blast.BlastExecute Delegate
 

delegate used to execute scripts

**Namespace:**&nbsp;<a href="N_NSS_Blast">NSS.Blast</a><br />**Assembly:**&nbsp;BLAST (in BLAST.dll) Version: 1.0.0

## Syntax

**C#**<br />
``` C#
public delegate int BlastExecute(
	int scriptid,
	float* p_stack,
	IntPtr data,
	IntPtr caller
)
```


#### Parameters
&nbsp;<dl><dt>scriptid</dt><dd>Type: <a href="https://docs.microsoft.com/dotnet/api/system.int32" target="_blank" rel="noopener noreferrer">System.Int32</a><br />the id of the script</dd><dt>p_stack</dt><dd>Type: <a href="https://docs.microsoft.com/dotnet/api/system.single" target="_blank" rel="noopener noreferrer">System.Single</a>*<br />the stack pointer</dd><dt>data</dt><dd>Type: <a href="https://docs.microsoft.com/dotnet/api/system.intptr" target="_blank" rel="noopener noreferrer">System.IntPtr</a><br />environment data</dd><dt>caller</dt><dd>Type: <a href="https://docs.microsoft.com/dotnet/api/system.intptr" target="_blank" rel="noopener noreferrer">System.IntPtr</a><br />caller data</dd></dl>

#### Return Value
Type: <a href="https://docs.microsoft.com/dotnet/api/system.int32" target="_blank" rel="noopener noreferrer">Int32</a><br />

## See Also


#### Reference
<a href="N_NSS_Blast">NSS.Blast Namespace</a><br />