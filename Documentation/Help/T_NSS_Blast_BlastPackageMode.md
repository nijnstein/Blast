# BlastPackageMode Enumeration
 

Blast script packaging types

**Namespace:**&nbsp;<a href="N_NSS_Blast">NSS.Blast</a><br />**Assembly:**&nbsp;BLAST (in BLAST.dll) Version: 1.0.0

## Syntax

**C#**<br />
``` C#
public enum BlastPackageMode
```


## Members
&nbsp;<table><tr><th></th><th>Member name</th><th>Value</th><th>Description</th></tr><tr><td /><td target="F:NSS.Blast.BlastPackageMode.Normal">**Normal**</td><td>0</td><td>Compile all into a single package, code data and stack combined [----CODE----|----METADATA----|----DATA----|----STACK----] 1 2 3 4 ** ALL OFFSETS IN PACKAGE IN BYTES *** 1 = metadata offset 2 = data_offset 3 = stack_offset 4 = package_size</td></tr><tr><td /><td target="F:NSS.Blast.BlastPackageMode.SSMD">**SSMD**</td><td>1</td><td>SSMD Package with code and metadata seperate from datastack - SSMD requirement: while data changes, its intent does not so metadata is shared - SSDM in V2/BS2 allows for (nested) branching by setting sync points on non constant jumps: -- loop ends -- if then else jumps [----CODE----|----METADATA----] [----DATA----|----STACK----] 1 2 3 4 1 = metadata offset 2 = codesegment size 3 = stack offset 4 = datasegment size prop stacksize (in elements) => (datasegment size - stack_offset) / 4</td></tr><tr><td /><td target="F:NSS.Blast.BlastPackageMode.Entity">**Entity**</td><td>2</td><td>Entity Package: the script's code is seperated from all data [----CODE----] [----METADATA----|----DATA----|----STACK----] 1 2 3 4 1 = codesegment size 2 = metadata size 3 = stack offset 4 = datasegment size</td></tr><tr><td /><td target="F:NSS.Blast.BlastPackageMode.Compiler">**Compiler**</td><td>3</td><td>package type used by compiler - THE POINTERS ARE INVALID [----CODE----] [----METADATA----] [----DATA----|----STACK----] 1 2 3 4 1 = codesegment size 2 = metadata size 3 = datasize in bytes / stack offset in bytes 4 = datasegment size = stackcapacity = 4 - 3</td></tr></table>

## See Also


#### Reference
<a href="N_NSS_Blast">NSS.Blast Namespace</a><br />