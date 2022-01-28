# CompilationData.CreateVariable Method 
 

create a holder for variable data collected during compilation - will log errors if the variable exists and returns null - initializes reference count at 1

**Namespace:**&nbsp;<a href="N_NSS_Blast_Compiler">NSS.Blast.Compiler</a><br />**Assembly:**&nbsp;BLAST (in BLAST.dll) Version: 1.0.0

## Syntax

**C#**<br />
``` C#
public BlastVariable CreateVariable(
	string name,
	bool is_input = false,
	bool is_output = false
)
```


#### Parameters
&nbsp;<dl><dt>name</dt><dd>Type: <a href="https://docs.microsoft.com/dotnet/api/system.string" target="_blank" rel="noopener noreferrer">System.String</a><br />first part of identifier - the name</dd><dt>is_input (Optional)</dt><dd>Type: <a href="https://docs.microsoft.com/dotnet/api/system.boolean" target="_blank" rel="noopener noreferrer">System.Boolean</a><br />true if used as an input</dd><dt>is_output (Optional)</dt><dd>Type: <a href="https://docs.microsoft.com/dotnet/api/system.boolean" target="_blank" rel="noopener noreferrer">System.Boolean</a><br />true if used as an output</dd></dl>

#### Return Value
Type: <a href="T_NSS_Blast_BlastVariable">BlastVariable</a><br />null on failure, any error will be logged

## See Also


#### Reference
<a href="T_NSS_Blast_Compiler_CompilationData">CompilationData Class</a><br /><a href="N_NSS_Blast_Compiler">NSS.Blast.Compiler Namespace</a><br />