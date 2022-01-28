# BlastEngineData.GetFunction Method (blast_operation)
 

lookup function attached to operation, this assumes the function exists - TODO -> we could update this to a tableindexer instead of a scan by building a lookuptable[operation] == function

**Namespace:**&nbsp;<a href="N_NSS_Blast">NSS.Blast</a><br />**Assembly:**&nbsp;BLAST (in BLAST.dll) Version: 1.0.0

## Syntax

**C#**<br />
``` C#
public BlastScriptFunction GetFunction(
	blast_operation op
)
```


#### Parameters
&nbsp;<dl><dt>op</dt><dd>Type: <a href="T_NSS_Blast_blast_operation">NSS.Blast.blast_operation</a><br />the operation to lookup the function for</dd></dl>

#### Return Value
Type: <a href="T_NSS_Blast_BlastScriptFunction">BlastScriptFunction</a><br />function record

## See Also


#### Reference
<a href="T_NSS_Blast_BlastEngineData">BlastEngineData Structure</a><br /><a href="Overload_NSS_Blast_BlastEngineData_GetFunction">GetFunction Overload</a><br /><a href="N_NSS_Blast">NSS.Blast Namespace</a><br />