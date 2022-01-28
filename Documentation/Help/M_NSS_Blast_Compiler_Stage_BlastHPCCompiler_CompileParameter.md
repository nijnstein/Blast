# BlastHPCCompiler.CompileParameter Method 
 

compile a single parameter node into the code stream

**Namespace:**&nbsp;<a href="N_NSS_Blast_Compiler_Stage">NSS.Blast.Compiler.Stage</a><br />**Assembly:**&nbsp;BLAST (in BLAST.dll) Version: 1.0.0

## Syntax

**C#**<br />
``` C#
public static bool CompileParameter(
	CompilationData data,
	node ast_param,
	StringBuilder code
)
```


#### Parameters
&nbsp;<dl><dt>data</dt><dd>Type: <a href="T_NSS_Blast_Compiler_CompilationData">NSS.Blast.Compiler.CompilationData</a><br />\[Missing <param name="data"/> documentation for "M:NSS.Blast.Compiler.Stage.BlastHPCCompiler.CompileParameter(NSS.Blast.Compiler.CompilationData,NSS.Blast.Compiler.node,System.Text.StringBuilder)"\]</dd><dt>ast_param</dt><dd>Type: <a href="T_NSS_Blast_Compiler_node">NSS.Blast.Compiler.node</a><br />the parameter node</dd><dt>code</dt><dd>Type: <a href="https://docs.microsoft.com/dotnet/api/system.text.stringbuilder" target="_blank" rel="noopener noreferrer">System.Text.StringBuilder</a><br />the code to append to</dd></dl>

#### Return Value
Type: <a href="https://docs.microsoft.com/dotnet/api/system.boolean" target="_blank" rel="noopener noreferrer">Boolean</a><br />false on failure

## See Also


#### Reference
<a href="T_NSS_Blast_Compiler_Stage_BlastHPCCompiler">BlastHPCCompiler Class</a><br /><a href="N_NSS_Blast_Compiler_Stage">NSS.Blast.Compiler.Stage Namespace</a><br />