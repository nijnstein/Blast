# BlastSSMDInterpretor.OperationIsSSMDHandled Method 
 

returns true for ssmd valid operations: add = 2, substract = 3, divide = 4, multiply = 5, and = 6, or = 7, not = 8, xor = 9, greater = 10, greater_equals = 11, smaller = 12, smaller_equals, equals, not_equals

**Namespace:**&nbsp;<a href="N_NSS_Blast_SSMD">NSS.Blast.SSMD</a><br />**Assembly:**&nbsp;BLAST (in BLAST.dll) Version: 1.0.0

## Syntax

**C#**<br />
``` C#
public static bool OperationIsSSMDHandled(
	blast_operation op
)
```


#### Parameters
&nbsp;<dl><dt>op</dt><dd>Type: <a href="T_NSS_Blast_blast_operation">NSS.Blast.blast_operation</a><br />the operation to check</dd></dl>

#### Return Value
Type: <a href="https://docs.microsoft.com/dotnet/api/system.boolean" target="_blank" rel="noopener noreferrer">Boolean</a><br />true if handled by the ssmd interpretor

## See Also


#### Reference
<a href="T_NSS_Blast_SSMD_BlastSSMDInterpretor">BlastSSMDInterpretor Structure</a><br /><a href="N_NSS_Blast_SSMD">NSS.Blast.SSMD Namespace</a><br />