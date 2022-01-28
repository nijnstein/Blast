# node.GetChild Method (nodetype, nodetype)
 

get first child matching first choice, if none found, try the second choice

**Namespace:**&nbsp;<a href="N_NSS_Blast_Compiler">NSS.Blast.Compiler</a><br />**Assembly:**&nbsp;BLAST (in BLAST.dll) Version: 1.0.0

## Syntax

**C#**<br />
``` C#
public node GetChild(
	nodetype first_choice,
	nodetype second_choice
)
```


#### Parameters
&nbsp;<dl><dt>first_choice</dt><dd>Type: <a href="T_NSS_Blast_Compiler_nodetype">NSS.Blast.Compiler.nodetype</a><br />first to try to locate</dd><dt>second_choice</dt><dd>Type: <a href="T_NSS_Blast_Compiler_nodetype">NSS.Blast.Compiler.nodetype</a><br />next attemt to locate</dd></dl>

#### Return Value
Type: <a href="T_NSS_Blast_Compiler_node">node</a><br />a node if any was found, otherwise null

## See Also


#### Reference
<a href="T_NSS_Blast_Compiler_node">node Class</a><br /><a href="Overload_NSS_Blast_Compiler_node_GetChild">GetChild Overload</a><br /><a href="N_NSS_Blast_Compiler">NSS.Blast.Compiler Namespace</a><br />