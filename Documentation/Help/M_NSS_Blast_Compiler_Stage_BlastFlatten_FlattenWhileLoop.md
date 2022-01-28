# BlastFlatten.FlattenWhileLoop Method 
 

flatten a while loop (which might originally have been a for loop dueue to transform stage) - pops away operations from condition - generates flat list of statements for loop compound - maintains same root object --- THE WHILELOOP SHOULD BE EXCLUDED FROM NESTING TESTS --- while this nests in the node tree it nests control flow and that will flatten out when compiling into jumps

**Namespace:**&nbsp;<a href="N_NSS_Blast_Compiler_Stage">NSS.Blast.Compiler.Stage</a><br />**Assembly:**&nbsp;BLAST (in BLAST.dll) Version: 1.0.0

## Syntax

**C#**<br />
``` C#
public BlastError FlattenWhileLoop(
	IBlastCompilationData data,
	node while_root
)
```


#### Parameters
&nbsp;<dl><dt>data</dt><dd>Type: <a href="T_NSS_Blast_Compiler_IBlastCompilationData">NSS.Blast.Compiler.IBlastCompilationData</a><br />\[Missing <param name="data"/> documentation for "M:NSS.Blast.Compiler.Stage.BlastFlatten.FlattenWhileLoop(NSS.Blast.Compiler.IBlastCompilationData,NSS.Blast.Compiler.node)"\]</dd><dt>while_root</dt><dd>Type: <a href="T_NSS_Blast_Compiler_node">NSS.Blast.Compiler.node</a><br />\[Missing <param name="while_root"/> documentation for "M:NSS.Blast.Compiler.Stage.BlastFlatten.FlattenWhileLoop(NSS.Blast.Compiler.IBlastCompilationData,NSS.Blast.Compiler.node)"\]</dd></dl>

#### Return Value
Type: <a href="T_NSS_Blast_BlastError">BlastError</a><br />\[Missing <returns> documentation for "M:NSS.Blast.Compiler.Stage.BlastFlatten.FlattenWhileLoop(NSS.Blast.Compiler.IBlastCompilationData,NSS.Blast.Compiler.node)"\]

## See Also


#### Reference
<a href="T_NSS_Blast_Compiler_Stage_BlastFlatten">BlastFlatten Class</a><br /><a href="N_NSS_Blast_Compiler_Stage">NSS.Blast.Compiler.Stage Namespace</a><br />