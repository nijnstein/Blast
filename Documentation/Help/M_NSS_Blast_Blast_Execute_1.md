# Blast.Execute Method (BlastEngineDataPtr, BlastPackageData, IntPtr, IntPtr, Int32)
 

Execute a Package in ssmd mode

**Namespace:**&nbsp;<a href="N_NSS_Blast">NSS.Blast</a><br />**Assembly:**&nbsp;BLAST (in BLAST.dll) Version: 1.0.0

## Syntax

**C#**<br />
``` C#
public static BlastError Execute(
	 in BlastEngineDataPtr blast,
	 in BlastPackageData package,
	IntPtr environment,
	IntPtr ssmd_data,
	int ssmd_count
)
```


#### Parameters
&nbsp;<dl><dt>blast</dt><dd>Type: <a href="T_NSS_Blast_BlastEngineDataPtr">NSS.Blast.BlastEngineDataPtr</a><br />\[Missing <param name="blast"/> documentation for "M:NSS.Blast.Blast.Execute(NSS.Blast.BlastEngineDataPtr@,NSS.Blast.BlastPackageData@,System.IntPtr,System.IntPtr,System.Int32)"\]</dd><dt>package</dt><dd>Type: <a href="T_NSS_Blast_BlastPackageData">NSS.Blast.BlastPackageData</a><br />the package data to execute</dd><dt>environment</dt><dd>Type: <a href="https://docs.microsoft.com/dotnet/api/system.intptr" target="_blank" rel="noopener noreferrer">System.IntPtr</a><br />the environment data to use</dd><dt>ssmd_data</dt><dd>Type: <a href="https://docs.microsoft.com/dotnet/api/system.intptr" target="_blank" rel="noopener noreferrer">System.IntPtr</a><br />data for each entity|script|component</dd><dt>ssmd_count</dt><dd>Type: <a href="https://docs.microsoft.com/dotnet/api/system.int32" target="_blank" rel="noopener noreferrer">System.Int32</a><br />the number of data elements</dd></dl>

#### Return Value
Type: <a href="T_NSS_Blast_BlastError">BlastError</a><br />succes or an error code

## See Also


#### Reference
<a href="T_NSS_Blast_Blast">Blast Structure</a><br /><a href="Overload_NSS_Blast_Blast_Execute">Execute Overload</a><br /><a href="N_NSS_Blast">NSS.Blast Namespace</a><br />