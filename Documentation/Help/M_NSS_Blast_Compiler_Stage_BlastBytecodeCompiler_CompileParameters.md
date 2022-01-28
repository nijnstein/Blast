# BlastBytecodeCompiler.CompileParameters Method 
 

compile a list of nodes as a list of parameters

**Namespace:**&nbsp;<a href="N_NSS_Blast_Compiler_Stage">NSS.Blast.Compiler.Stage</a><br />**Assembly:**&nbsp;BLAST (in BLAST.dll) Version: 1.0.0

## Syntax

**C#**<br />
``` C#
public static bool CompileParameters(
	CompilationData data,
	node ast_function,
	IMByteCodeList code,
	IEnumerable<node> parameter_nodes,
	int min_validate,
	int max_validate
)
```


#### Parameters
&nbsp;<dl><dt>data</dt><dd>Type: <a href="T_NSS_Blast_Compiler_CompilationData">NSS.Blast.Compiler.CompilationData</a><br />\[Missing <param name="data"/> documentation for "M:NSS.Blast.Compiler.Stage.BlastBytecodeCompiler.CompileParameters(NSS.Blast.Compiler.CompilationData,NSS.Blast.Compiler.node,NSS.Blast.Compiler.Stage.IMByteCodeList,System.Collections.Generic.IEnumerable{NSS.Blast.Compiler.node},System.Int32,System.Int32)"\]</dd><dt>ast_function</dt><dd>Type: <a href="T_NSS_Blast_Compiler_node">NSS.Blast.Compiler.node</a><br />the node with the function</dd><dt>code</dt><dd>Type: <a href="T_NSS_Blast_Compiler_Stage_IMByteCodeList">NSS.Blast.Compiler.Stage.IMByteCodeList</a><br />the bytecode to add compiled code to</dd><dt>parameter_nodes</dt><dd>Type: <a href="https://docs.microsoft.com/dotnet/api/system.collections.generic.ienumerable-1" target="_blank" rel="noopener noreferrer">System.Collections.Generic.IEnumerable</a>(<a href="T_NSS_Blast_Compiler_node">node</a>)<br />the parameter list</dd><dt>min_validate</dt><dd>Type: <a href="https://docs.microsoft.com/dotnet/api/system.int32" target="_blank" rel="noopener noreferrer">System.Int32</a><br />min parameter count, error will be raised if less</dd><dt>max_validate</dt><dd>Type: <a href="https://docs.microsoft.com/dotnet/api/system.int32" target="_blank" rel="noopener noreferrer">System.Int32</a><br />max parameter count, error will be raised if more</dd></dl>

#### Return Value
Type: <a href="https://docs.microsoft.com/dotnet/api/system.boolean" target="_blank" rel="noopener noreferrer">Boolean</a><br />\[Missing <returns> documentation for "M:NSS.Blast.Compiler.Stage.BlastBytecodeCompiler.CompileParameters(NSS.Blast.Compiler.CompilationData,NSS.Blast.Compiler.node,NSS.Blast.Compiler.Stage.IMByteCodeList,System.Collections.Generic.IEnumerable{NSS.Blast.Compiler.node},System.Int32,System.Int32)"\]

## See Also


#### Reference
<a href="T_NSS_Blast_Compiler_Stage_BlastBytecodeCompiler">BlastBytecodeCompiler Class</a><br /><a href="N_NSS_Blast_Compiler_Stage">NSS.Blast.Compiler.Stage Namespace</a><br />