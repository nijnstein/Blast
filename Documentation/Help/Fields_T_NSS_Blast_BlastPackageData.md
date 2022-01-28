# BlastPackageData Fields
 

The <a href="T_NSS_Blast_BlastPackageData">BlastPackageData</a> type exposes the following members.


## Fields
&nbsp;<table><tr><th></th><th>Name</th><th>Description</th></tr><tr><td>![Public field](media/pubfield.gif "Public field")</td><td><a href="F_NSS_Blast_BlastPackageData_Allocator">Allocator</a></td><td>
Package memory allocator</td></tr><tr><td>![Public field](media/pubfield.gif "Public field")</td><td><a href="F_NSS_Blast_BlastPackageData_CodeSegmentPtr">CodeSegmentPtr</a></td><td>
points to codesegment, may be shared with other segments, see packagemode</td></tr><tr><td>![Public field](media/pubfield.gif "Public field")</td><td><a href="F_NSS_Blast_BlastPackageData_Flags">Flags</a></td><td>
Package flags, see flags enumeration</td></tr><tr><td>![Public field](media/pubfield.gif "Public field")</td><td><a href="F_NSS_Blast_BlastPackageData_LanguageVersion">LanguageVersion</a></td><td>
Package languageversion, determines feature support</td></tr><tr><td>![Public field](media/pubfield.gif "Public field")</td><td><a href="F_NSS_Blast_BlastPackageData_O1">O1</a></td><td>
Offset 1, intent depends on packagemode</td></tr><tr><td>![Public field](media/pubfield.gif "Public field")</td><td><a href="F_NSS_Blast_BlastPackageData_O2">O2</a></td><td>
Offset 2, intent depends on packagemode</td></tr><tr><td>![Public field](media/pubfield.gif "Public field")</td><td><a href="F_NSS_Blast_BlastPackageData_O3">O3</a></td><td>
Offset 3, intent depends on packagemode</td></tr><tr><td>![Public field](media/pubfield.gif "Public field")</td><td><a href="F_NSS_Blast_BlastPackageData_O4">O4</a></td><td>
Offset 4, intent depends on packagemode</td></tr><tr><td>![Public field](media/pubfield.gif "Public field")</td><td><a href="F_NSS_Blast_BlastPackageData_P2">P2</a></td><td>
points to the other segment, see packagemode</td></tr><tr><td>![Public field](media/pubfield.gif "Public field")</td><td><a href="F_NSS_Blast_BlastPackageData_PackageMode">PackageMode</a></td><td>
Packaging mode NORMAL: [----CODE----|----METADATA----|----DATA----|----STACK----] 1 2 3 4 SSMD: [----CODE----|----METADATA----] [----DATA----|----STACK----] 1 2 3 4 ENTITY: [----CODE----] [----METADATA----] [----DATA----|----STACK----] 1 2 3 4</td></tr></table>&nbsp;
<a href="#blastpackagedata-fields">Back to Top</a>

## See Also


#### Reference
<a href="T_NSS_Blast_BlastPackageData">BlastPackageData Structure</a><br /><a href="N_NSS_Blast">NSS.Blast Namespace</a><br />