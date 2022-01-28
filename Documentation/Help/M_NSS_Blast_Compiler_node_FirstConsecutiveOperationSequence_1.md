# node.FirstConsecutiveOperationSequence Method (Int32, Int32, blast_operation, Int32, node)
 

scan children of node for the first group of operations of a given minimal size

**Namespace:**&nbsp;<a href="N_NSS_Blast_Compiler">NSS.Blast.Compiler</a><br />**Assembly:**&nbsp;BLAST (in BLAST.dll) Version: 1.0.0

## Syntax

**C#**<br />
``` C#
public bool FirstConsecutiveOperationSequence(
	int min_groupsize,
	int from,
	out blast_operation op,
	out int op_count,
	out node first_op_in_sequence
)
```


#### Parameters
&nbsp;<dl><dt>min_groupsize</dt><dd>Type: <a href="https://docs.microsoft.com/dotnet/api/system.int32" target="_blank" rel="noopener noreferrer">System.Int32</a><br />the minimal group size</dd><dt>from</dt><dd>Type: <a href="https://docs.microsoft.com/dotnet/api/system.int32" target="_blank" rel="noopener noreferrer">System.Int32</a><br />start check from this node</dd><dt>op</dt><dd>Type: <a href="T_NSS_Blast_blast_operation">NSS.Blast.blast_operation</a><br />operation to scan for</dd><dt>op_count</dt><dd>Type: <a href="https://docs.microsoft.com/dotnet/api/system.int32" target="_blank" rel="noopener noreferrer">System.Int32</a><br />the operation count in the group found</dd><dt>first_op_in_sequence</dt><dd>Type: <a href="T_NSS_Blast_Compiler_node">NSS.Blast.Compiler.node</a><br />first operation found in sequence</dd></dl>

#### Return Value
Type: <a href="https://docs.microsoft.com/dotnet/api/system.boolean" target="_blank" rel="noopener noreferrer">Boolean</a><br />\[Missing <returns> documentation for "M:NSS.Blast.Compiler.node.FirstConsecutiveOperationSequence(System.Int32,System.Int32,NSS.Blast.blast_operation@,System.Int32@,NSS.Blast.Compiler.node@)"\]

## See Also


#### Reference
<a href="T_NSS_Blast_Compiler_node">node Class</a><br /><a href="Overload_NSS_Blast_Compiler_node_FirstConsecutiveOperationSequence">FirstConsecutiveOperationSequence Overload</a><br /><a href="N_NSS_Blast_Compiler">NSS.Blast.Compiler Namespace</a><br />