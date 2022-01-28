# BlastPackageData.HasStack Property 
 

true if memory for stack is allocated, false if not: - yield not possible without persistant stack - we call it TLS, thread level stack/storage - Faster in small multithreaded bursts, benefit fades in very large bursts

**Namespace:**&nbsp;<a href="N_NSS_Blast">NSS.Blast</a><br />**Assembly:**&nbsp;BLAST (in BLAST.dll) Version: 1.0.0

## Syntax

**C#**<br />
``` C#
public bool HasStack {
	get;
 }
```


#### Property Value
Type: <a href="https://docs.microsoft.com/dotnet/api/system.boolean" target="_blank" rel="noopener noreferrer">Boolean</a>

## See Also


#### Reference
<a href="T_NSS_Blast_BlastPackageData">BlastPackageData Structure</a><br /><a href="N_NSS_Blast">NSS.Blast Namespace</a><br />