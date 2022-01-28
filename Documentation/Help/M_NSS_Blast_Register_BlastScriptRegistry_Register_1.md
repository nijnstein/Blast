# BlastScriptRegistry.Register Method (String, String, Int32)
 

register code with given name and id with registry

**Namespace:**&nbsp;<a href="N_NSS_Blast_Register">NSS.Blast.Register</a><br />**Assembly:**&nbsp;BLAST (in BLAST.dll) Version: 1.0.0

## Syntax

**C#**<br />
``` C#
public static int Register(
	string code,
	string name = null,
	int id = 0
)
```


#### Parameters
&nbsp;<dl><dt>code</dt><dd>Type: <a href="https://docs.microsoft.com/dotnet/api/system.string" target="_blank" rel="noopener noreferrer">System.String</a><br />the script code to register</dd><dt>name (Optional)</dt><dd>Type: <a href="https://docs.microsoft.com/dotnet/api/system.string" target="_blank" rel="noopener noreferrer">System.String</a><br />the name to attach, should be unique and is case insensitive</dd><dt>id (Optional)</dt><dd>Type: <a href="https://docs.microsoft.com/dotnet/api/system.int32" target="_blank" rel="noopener noreferrer">System.Int32</a><br />the script id to use, 0 to generate one, should be > 0 and unique</dd></dl>

#### Return Value
Type: <a href="https://docs.microsoft.com/dotnet/api/system.int32" target="_blank" rel="noopener noreferrer">Int32</a><br />the script id

## See Also


#### Reference
<a href="T_NSS_Blast_Register_BlastScriptRegistry">BlastScriptRegistry Class</a><br /><a href="Overload_NSS_Blast_Register_BlastScriptRegistry_Register">Register Overload</a><br /><a href="N_NSS_Blast_Register">NSS.Blast.Register Namespace</a><br />