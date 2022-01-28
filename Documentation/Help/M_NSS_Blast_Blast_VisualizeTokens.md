# Blast.VisualizeTokens Method 
 

visualize a list of tokens and identifiers into a somewhat readable string

**Namespace:**&nbsp;<a href="N_NSS_Blast">NSS.Blast</a><br />**Assembly:**&nbsp;BLAST (in BLAST.dll) Version: 1.0.0

## Syntax

**C#**<br />
``` C#
public static string VisualizeTokens(
	List<Tuple<BlastScriptToken, string>> tokens,
	int idx,
	int idx_max
)
```


#### Parameters
&nbsp;<dl><dt>tokens</dt><dd>Type: <a href="https://docs.microsoft.com/dotnet/api/system.collections.generic.list-1" target="_blank" rel="noopener noreferrer">System.Collections.Generic.List</a>(<a href="https://docs.microsoft.com/dotnet/api/system.tuple-2" target="_blank" rel="noopener noreferrer">Tuple</a>(<a href="T_NSS_Blast_BlastScriptToken">BlastScriptToken</a>, <a href="https://docs.microsoft.com/dotnet/api/system.string" target="_blank" rel="noopener noreferrer">String</a>))<br />the tuples with token and identifier</dd><dt>idx</dt><dd>Type: <a href="https://docs.microsoft.com/dotnet/api/system.int32" target="_blank" rel="noopener noreferrer">System.Int32</a><br />start of range to view</dd><dt>idx_max</dt><dd>Type: <a href="https://docs.microsoft.com/dotnet/api/system.int32" target="_blank" rel="noopener noreferrer">System.Int32</a><br />end of range to view</dd></dl>

#### Return Value
Type: <a href="https://docs.microsoft.com/dotnet/api/system.string" target="_blank" rel="noopener noreferrer">String</a><br />a single line string with token descriptions

## See Also


#### Reference
<a href="T_NSS_Blast_Blast">Blast Structure</a><br /><a href="N_NSS_Blast">NSS.Blast Namespace</a><br />