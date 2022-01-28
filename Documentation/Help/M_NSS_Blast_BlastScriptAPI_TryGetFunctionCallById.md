# BlastScriptAPI.TryGetFunctionCallById Method 
 

try to get a function by id, in release it just gets it, in debug it checks

**Namespace:**&nbsp;<a href="N_NSS_Blast">NSS.Blast</a><br />**Assembly:**&nbsp;BLAST (in BLAST.dll) Version: 1.0.0

## Syntax

**C#**<br />
``` C#
public bool TryGetFunctionCallById(
	int id,
	out BlastScriptFunction function
)
```


#### Parameters
&nbsp;<dl><dt>id</dt><dd>Type: <a href="https://docs.microsoft.com/dotnet/api/system.int32" target="_blank" rel="noopener noreferrer">System.Int32</a><br />function id/index</dd><dt>function</dt><dd>Type: <a href="T_NSS_Blast_BlastScriptFunction">NSS.Blast.BlastScriptFunction</a><br />function</dd></dl>

#### Return Value
Type: <a href="https://docs.microsoft.com/dotnet/api/system.boolean" target="_blank" rel="noopener noreferrer">Boolean</a><br />true if found

## See Also


#### Reference
<a href="T_NSS_Blast_BlastScriptAPI">BlastScriptAPI Class</a><br /><a href="N_NSS_Blast">NSS.Blast Namespace</a><br />