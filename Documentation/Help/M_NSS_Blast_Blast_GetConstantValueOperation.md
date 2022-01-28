# Blast.GetConstantValueOperation Method 
 

get the script_op belonging to a constant value, eiter by name or value

**Namespace:**&nbsp;<a href="N_NSS_Blast">NSS.Blast</a><br />**Assembly:**&nbsp;BLAST (in BLAST.dll) Version: 1.0.0

## Syntax

**C#**<br />
``` C#
public static blast_operation GetConstantValueOperation(
	string value,
	float constant_epsilon = 0,0001f
)
```


#### Parameters
&nbsp;<dl><dt>value</dt><dd>Type: <a href="https://docs.microsoft.com/dotnet/api/system.string" target="_blank" rel="noopener noreferrer">System.String</a><br />the value to match</dd><dt>constant_epsilon (Optional)</dt><dd>Type: <a href="https://docs.microsoft.com/dotnet/api/system.single" target="_blank" rel="noopener noreferrer">System.Single</a><br />the epsilon to use matching constant values</dd></dl>

#### Return Value
Type: <a href="T_NSS_Blast_blast_operation">blast_operation</a><br />nop on no match, nan of not a string match and no float, operation on match

## See Also


#### Reference
<a href="T_NSS_Blast_Blast">Blast Structure</a><br /><a href="N_NSS_Blast">NSS.Blast Namespace</a><br />