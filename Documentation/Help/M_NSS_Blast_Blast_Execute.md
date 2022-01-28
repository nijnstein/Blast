# Blast.Execute Method (BlastEngineDataPtr, BlastPackageData, IntPtr, IntPtr)
 

Execute a Package

**Namespace:**&nbsp;<a href="N_NSS_Blast">NSS.Blast</a><br />**Assembly:**&nbsp;BLAST (in BLAST.dll) Version: 1.0.0

## Syntax

**C#**<br />
``` C#
public static BlastError Execute(
	 in BlastEngineDataPtr blast,
	 in BlastPackageData package,
	IntPtr environment,
	IntPtr caller
)
```


#### Parameters
&nbsp;<dl><dt>blast</dt><dd>Type: <a href="T_NSS_Blast_BlastEngineDataPtr">NSS.Blast.BlastEngineDataPtr</a><br />Blast engine data</dd><dt>package</dt><dd>Type: <a href="T_NSS_Blast_BlastPackageData">NSS.Blast.BlastPackageData</a><br />the package data to execute</dd><dt>environment</dt><dd>Type: <a href="https://docs.microsoft.com/dotnet/api/system.intptr" target="_blank" rel="noopener noreferrer">System.IntPtr</a><br />the environment data to use</dd><dt>caller</dt><dd>Type: <a href="https://docs.microsoft.com/dotnet/api/system.intptr" target="_blank" rel="noopener noreferrer">System.IntPtr</a><br />\[Missing <param name="caller"/> documentation for "M:NSS.Blast.Blast.Execute(NSS.Blast.BlastEngineDataPtr@,NSS.Blast.BlastPackageData@,System.IntPtr,System.IntPtr)"\]</dd></dl>

#### Return Value
Type: <a href="T_NSS_Blast_BlastError">BlastError</a><br />succes or an error code

## See Also


#### Reference
<a href="T_NSS_Blast_Blast">Blast Structure</a><br /><a href="Overload_NSS_Blast_Blast_Execute">Execute Overload</a><br /><a href="N_NSS_Blast">NSS.Blast Namespace</a><br />