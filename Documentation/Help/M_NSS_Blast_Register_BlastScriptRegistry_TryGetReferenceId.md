# BlastScriptRegistry.TryGetReferenceId Method 
 

try to find the id of a script with a given name, performs a linear search

**Namespace:**&nbsp;<a href="N_NSS_Blast_Register">NSS.Blast.Register</a><br />**Assembly:**&nbsp;BLAST (in BLAST.dll) Version: 1.0.0

## Syntax

**C#**<br />
``` C#
public static bool TryGetReferenceId(
	string name,
	out int reference_id
)
```


#### Parameters
&nbsp;<dl><dt>name</dt><dd>Type: <a href="https://docs.microsoft.com/dotnet/api/system.string" target="_blank" rel="noopener noreferrer">System.String</a><br />the case insensitive name to lookup</dd><dt>reference_id</dt><dd>Type: <a href="https://docs.microsoft.com/dotnet/api/system.int32" target="_blank" rel="noopener noreferrer">System.Int32</a><br />the script id</dd></dl>

#### Return Value
Type: <a href="https://docs.microsoft.com/dotnet/api/system.boolean" target="_blank" rel="noopener noreferrer">Boolean</a><br />true if a script with the given name was found

## See Also


#### Reference
<a href="T_NSS_Blast_Register_BlastScriptRegistry">BlastScriptRegistry Class</a><br /><a href="N_NSS_Blast_Register">NSS.Blast.Register Namespace</a><br />