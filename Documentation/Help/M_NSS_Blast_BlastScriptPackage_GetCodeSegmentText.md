# BlastScriptPackage.GetCodeSegmentText Method 
 

Get a string representation of the bytecode, example output: 000| push compound 1 + 2 nop push function max ^ pop 2 010| debug pop nop 000| 030 085 002 086 000 029 042 009 025 086 010| 255 253 025 000

**Namespace:**&nbsp;<a href="N_NSS_Blast">NSS.Blast</a><br />**Assembly:**&nbsp;BLAST (in BLAST.dll) Version: 1.0.0

## Syntax

**C#**<br />
``` C#
public string GetCodeSegmentText(
	int width = 16,
	bool show_index = true
)
```


#### Parameters
&nbsp;<dl><dt>width (Optional)</dt><dd>Type: <a href="https://docs.microsoft.com/dotnet/api/system.int32" target="_blank" rel="noopener noreferrer">System.Int32</a><br />number of columns to render</dd><dt>show_index (Optional)</dt><dd>Type: <a href="https://docs.microsoft.com/dotnet/api/system.boolean" target="_blank" rel="noopener noreferrer">System.Boolean</a><br />\[Missing <param name="show_index"/> documentation for "M:NSS.Blast.BlastScriptPackage.GetCodeSegmentText(System.Int32,System.Boolean)"\]</dd></dl>

#### Return Value
Type: <a href="https://docs.microsoft.com/dotnet/api/system.string" target="_blank" rel="noopener noreferrer">String</a><br />A formatted string

## See Also


#### Reference
<a href="T_NSS_Blast_BlastScriptPackage">BlastScriptPackage Class</a><br /><a href="N_NSS_Blast">NSS.Blast Namespace</a><br />