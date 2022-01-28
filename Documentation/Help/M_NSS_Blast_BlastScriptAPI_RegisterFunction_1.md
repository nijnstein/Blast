# BlastScriptAPI.RegisterFunction Method (ReservedBlastScriptFunctionIds, String, Int32, Int32, Int32, Int32, extended_blast_operation)
 

Register a function with blast

**Namespace:**&nbsp;<a href="N_NSS_Blast">NSS.Blast</a><br />**Assembly:**&nbsp;BLAST (in BLAST.dll) Version: 1.0.0

## Syntax

**C#**<br />
``` C#
public int RegisterFunction(
	ReservedBlastScriptFunctionIds id,
	string name,
	int min_param_count,
	int max_param_count,
	int accept_vector_size,
	int return_vector_size,
	extended_blast_operation op
)
```


#### Parameters
&nbsp;<dl><dt>id</dt><dd>Type: <a href="T_NSS_Blast_ReservedBlastScriptFunctionIds">NSS.Blast.ReservedBlastScriptFunctionIds</a><br />a reserved function id</dd><dt>name</dt><dd>Type: <a href="https://docs.microsoft.com/dotnet/api/system.string" target="_blank" rel="noopener noreferrer">System.String</a><br />functino name</dd><dt>min_param_count</dt><dd>Type: <a href="https://docs.microsoft.com/dotnet/api/system.int32" target="_blank" rel="noopener noreferrer">System.Int32</a><br />minimal parameter count</dd><dt>max_param_count</dt><dd>Type: <a href="https://docs.microsoft.com/dotnet/api/system.int32" target="_blank" rel="noopener noreferrer">System.Int32</a><br />maximum parameter count</dd><dt>accept_vector_size</dt><dd>Type: <a href="https://docs.microsoft.com/dotnet/api/system.int32" target="_blank" rel="noopener noreferrer">System.Int32</a><br />minimal vector size, 0 for any</dd><dt>return_vector_size</dt><dd>Type: <a href="https://docs.microsoft.com/dotnet/api/system.int32" target="_blank" rel="noopener noreferrer">System.Int32</a><br />max verctor size, 0 for any</dd><dt>op</dt><dd>Type: <a href="T_NSS_Blast_extended_blast_operation">NSS.Blast.extended_blast_operation</a><br />extended blast operation used to encode it</dd></dl>

#### Return Value
Type: <a href="https://docs.microsoft.com/dotnet/api/system.int32" target="_blank" rel="noopener noreferrer">Int32</a><br />returns a unique (within the blast instance) function id

## See Also


#### Reference
<a href="T_NSS_Blast_BlastScriptAPI">BlastScriptAPI Class</a><br /><a href="Overload_NSS_Blast_BlastScriptAPI_RegisterFunction">RegisterFunction Overload</a><br /><a href="N_NSS_Blast">NSS.Blast Namespace</a><br />