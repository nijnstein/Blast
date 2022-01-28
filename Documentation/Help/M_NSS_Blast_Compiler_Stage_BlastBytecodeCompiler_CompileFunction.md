# BlastBytecodeCompiler.CompileFunction Method 
 

Compile a function and its parameters

**Namespace:**&nbsp;<a href="N_NSS_Blast_Compiler_Stage">NSS.Blast.Compiler.Stage</a><br />**Assembly:**&nbsp;BLAST (in BLAST.dll) Version: 1.0.0

## Syntax

**C#**<br />
``` C#
public static bool CompileFunction(
	CompilationData data,
	node ast_function,
	IMByteCodeList code
)
```


#### Parameters
&nbsp;<dl><dt>data</dt><dd>Type: <a href="T_NSS_Blast_Compiler_CompilationData">NSS.Blast.Compiler.CompilationData</a><br />\[Missing <param name="data"/> documentation for "M:NSS.Blast.Compiler.Stage.BlastBytecodeCompiler.CompileFunction(NSS.Blast.Compiler.CompilationData,NSS.Blast.Compiler.node,NSS.Blast.Compiler.Stage.IMByteCodeList)"\]</dd><dt>ast_function</dt><dd>Type: <a href="T_NSS_Blast_Compiler_node">NSS.Blast.Compiler.node</a><br />the node/function to compile</dd><dt>code</dt><dd>Type: <a href="T_NSS_Blast_Compiler_Stage_IMByteCodeList">NSS.Blast.Compiler.Stage.IMByteCodeList</a><br />\[Missing <param name="code"/> documentation for "M:NSS.Blast.Compiler.Stage.BlastBytecodeCompiler.CompileFunction(NSS.Blast.Compiler.CompilationData,NSS.Blast.Compiler.node,NSS.Blast.Compiler.Stage.IMByteCodeList)"\]</dd></dl>

#### Return Value
Type: <a href="https://docs.microsoft.com/dotnet/api/system.boolean" target="_blank" rel="noopener noreferrer">Boolean</a><br />true on success, false otherwise, errors will be logged in data

## See Also


#### Reference
<a href="T_NSS_Blast_Compiler_Stage_BlastBytecodeCompiler">BlastBytecodeCompiler Class</a><br /><a href="N_NSS_Blast_Compiler_Stage">NSS.Blast.Compiler.Stage Namespace</a><br />