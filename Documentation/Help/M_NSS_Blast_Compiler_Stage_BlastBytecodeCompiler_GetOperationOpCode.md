# BlastBytecodeCompiler.GetOperationOpCode Method 
 

translate a token representing an operation into its matching opcode, logs an error if operation cant be mapped

**Namespace:**&nbsp;<a href="N_NSS_Blast_Compiler_Stage">NSS.Blast.Compiler.Stage</a><br />**Assembly:**&nbsp;BLAST (in BLAST.dll) Version: 1.0.0

## Syntax

**C#**<br />
``` C#
public static blast_operation GetOperationOpCode(
	IBlastCompilationData data,
	BlastScriptToken token
)
```


#### Parameters
&nbsp;<dl><dt>data</dt><dd>Type: <a href="T_NSS_Blast_Compiler_IBlastCompilationData">NSS.Blast.Compiler.IBlastCompilationData</a><br />\[Missing <param name="data"/> documentation for "M:NSS.Blast.Compiler.Stage.BlastBytecodeCompiler.GetOperationOpCode(NSS.Blast.Compiler.IBlastCompilationData,NSS.Blast.BlastScriptToken)"\]</dd><dt>token</dt><dd>Type: <a href="T_NSS_Blast_BlastScriptToken">NSS.Blast.BlastScriptToken</a><br />the token to map</dd></dl>

#### Return Value
Type: <a href="T_NSS_Blast_blast_operation">blast_operation</a><br />script opcode

## See Also


#### Reference
<a href="T_NSS_Blast_Compiler_Stage_BlastBytecodeCompiler">BlastBytecodeCompiler Class</a><br /><a href="N_NSS_Blast_Compiler_Stage">NSS.Blast.Compiler.Stage Namespace</a><br />