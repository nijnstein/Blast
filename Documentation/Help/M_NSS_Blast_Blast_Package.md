# Blast.Package Method (BlastEngineDataPtr, BlastScript, BlastCompilerOptions)
 

Compile and package script code

**Namespace:**&nbsp;<a href="N_NSS_Blast">NSS.Blast</a><br />**Assembly:**&nbsp;BLAST (in BLAST.dll) Version: 1.0.0

## Syntax

**C#**<br />
``` C#
public static BlastScriptPackage Package(
	BlastEngineDataPtr blast,
	BlastScript script,
	BlastCompilerOptions options = null
)
```


#### Parameters
&nbsp;<dl><dt>blast</dt><dd>Type: <a href="T_NSS_Blast_BlastEngineDataPtr">NSS.Blast.BlastEngineDataPtr</a><br />blast engine data</dd><dt>script</dt><dd>Type: <a href="T_NSS_Blast_BlastScript">NSS.Blast.BlastScript</a><br />script code to compile</dd><dt>options (Optional)</dt><dd>Type: <a href="T_NSS_Blast_BlastCompilerOptions">NSS.Blast.BlastCompilerOptions</a><br />compiler options</dd></dl>

#### Return Value
Type: <a href="T_NSS_Blast_BlastScriptPackage">BlastScriptPackage</a><br />the code packaged and ready to execute

## See Also


#### Reference
<a href="T_NSS_Blast_Blast">Blast Structure</a><br /><a href="Overload_NSS_Blast_Blast_Package">Package Overload</a><br /><a href="N_NSS_Blast">NSS.Blast Namespace</a><br />