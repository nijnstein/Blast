# BlastPackageData.DataSegmentStackOffset Property 
 

stack offset in bytes as seen from the start of datasegment - stack and datasegment MUST ALWAYS be in the same segment so that Data[last] == Stack[-1] - when using .Stack[] offset is 0

**Namespace:**&nbsp;<a href="N_NSS_Blast">NSS.Blast</a><br />**Assembly:**&nbsp;BLAST (in BLAST.dll) Version: 1.0.0

## Syntax

**C#**<br />
``` C#
public ushort DataSegmentStackOffset {
	get;
 }
```


#### Property Value
Type: <a href="https://docs.microsoft.com/dotnet/api/system.uint16" target="_blank" rel="noopener noreferrer">UInt16</a>

## See Also


#### Reference
<a href="T_NSS_Blast_BlastPackageData">BlastPackageData Structure</a><br /><a href="N_NSS_Blast">NSS.Blast Namespace</a><br />