# node.IsOperationList Method (node, blast_operation)
 

check if the node is an operation sequence in the form: 3 + a + 4 + 4 + max(3093) + (4 + 0)

**Namespace:**&nbsp;<a href="N_NSS_Blast_Compiler">NSS.Blast.Compiler</a><br />**Assembly:**&nbsp;BLAST (in BLAST.dll) Version: 1.0.0

## Syntax

**C#**<br />
``` C#
public static bool IsOperationList(
	node node,
	out blast_operation singleop
)
```


#### Parameters
&nbsp;<dl><dt>node</dt><dd>Type: <a href="T_NSS_Blast_Compiler_node">NSS.Blast.Compiler.node</a><br />the node to check</dd><dt>singleop</dt><dd>Type: <a href="T_NSS_Blast_blast_operation">NSS.Blast.blast_operation</a><br />the operation found, if single it maps to the operation, if none its nop, on many different it will be op.ex</dd></dl>

#### Return Value
Type: <a href="https://docs.microsoft.com/dotnet/api/system.boolean" target="_blank" rel="noopener noreferrer">Boolean</a><br />true if a list of operations

## See Also


#### Reference
<a href="T_NSS_Blast_Compiler_node">node Class</a><br /><a href="Overload_NSS_Blast_Compiler_node_IsOperationList">IsOperationList Overload</a><br /><a href="N_NSS_Blast_Compiler">NSS.Blast.Compiler Namespace</a><br />