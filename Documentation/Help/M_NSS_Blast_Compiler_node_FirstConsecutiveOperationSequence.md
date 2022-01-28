# node.FirstConsecutiveOperationSequence Method (node, Int32, Int32, blast_operation, Int32, node)
 

get the largest group of operations of the same type

**Namespace:**&nbsp;<a href="N_NSS_Blast_Compiler">NSS.Blast.Compiler</a><br />**Assembly:**&nbsp;BLAST (in BLAST.dll) Version: 1.0.0

## Syntax

**C#**<br />
``` C#
public static bool FirstConsecutiveOperationSequence(
	node node,
	int min_group_size,
	int from,
	out blast_operation op,
	out int op_count,
	out node first_op_in_sequence
)
```


#### Parameters
&nbsp;<dl><dt>node</dt><dd>Type: <a href="T_NSS_Blast_Compiler_node">NSS.Blast.Compiler.node</a><br />the parent node of the operation list</dd><dt>min_group_size</dt><dd>Type: <a href="https://docs.microsoft.com/dotnet/api/system.int32" target="_blank" rel="noopener noreferrer">System.Int32</a><br />minimal group size</dd><dt>from</dt><dd>Type: <a href="https://docs.microsoft.com/dotnet/api/system.int32" target="_blank" rel="noopener noreferrer">System.Int32</a><br />start check from this node</dd><dt>op</dt><dd>Type: <a href="T_NSS_Blast_blast_operation">NSS.Blast.blast_operation</a><br />outputs operation of largest group or nop</dd><dt>op_count</dt><dd>Type: <a href="https://docs.microsoft.com/dotnet/api/system.int32" target="_blank" rel="noopener noreferrer">System.Int32</a><br />nr of operations in group</dd><dt>first_op_in_sequence</dt><dd>Type: <a href="T_NSS_Blast_Compiler_node">NSS.Blast.Compiler.node</a><br />\[Missing <param name="first_op_in_sequence"/> documentation for "M:NSS.Blast.Compiler.node.FirstConsecutiveOperationSequence(NSS.Blast.Compiler.node,System.Int32,System.Int32,NSS.Blast.blast_operation@,System.Int32@,NSS.Blast.Compiler.node@)"\]</dd></dl>

#### Return Value
Type: <a href="https://docs.microsoft.com/dotnet/api/system.boolean" target="_blank" rel="noopener noreferrer">Boolean</a><br />\[Missing <returns> documentation for "M:NSS.Blast.Compiler.node.FirstConsecutiveOperationSequence(NSS.Blast.Compiler.node,System.Int32,System.Int32,NSS.Blast.blast_operation@,System.Int32@,NSS.Blast.Compiler.node@)"\]

## See Also


#### Reference
<a href="T_NSS_Blast_Compiler_node">node Class</a><br /><a href="Overload_NSS_Blast_Compiler_node_FirstConsecutiveOperationSequence">FirstConsecutiveOperationSequence Overload</a><br /><a href="N_NSS_Blast_Compiler">NSS.Blast.Compiler Namespace</a><br />