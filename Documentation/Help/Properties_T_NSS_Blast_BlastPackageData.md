# BlastPackageData Properties
 

The <a href="T_NSS_Blast_BlastPackageData">BlastPackageData</a> type exposes the following members.


## Properties
&nbsp;<table><tr><th></th><th>Name</th><th>Description</th></tr><tr><td>![Public property](media/pubproperty.gif "Public property")</td><td><a href="P_NSS_Blast_BlastPackageData_AllocatedDataSegmentSize">AllocatedDataSegmentSize</a></td><td>
the size with stack included depending on flags</td></tr><tr><td>![Public property](media/pubproperty.gif "Public property")</td><td><a href="P_NSS_Blast_BlastPackageData_Code">Code</a></td><td>
Code[] pointer</td></tr><tr><td>![Public property](media/pubproperty.gif "Public property")</td><td><a href="P_NSS_Blast_BlastPackageData_CodeSegmentSize">CodeSegmentSize</a></td><td>
the allocated datasize in bytes of the codesegment pointer</td></tr><tr><td>![Public property](media/pubproperty.gif "Public property")</td><td><a href="P_NSS_Blast_BlastPackageData_CodeSize">CodeSize</a></td><td>
size of code in bytes, maps to O1 in all package modes</td></tr><tr><td>![Public property](media/pubproperty.gif "Public property")</td><td><a href="P_NSS_Blast_BlastPackageData_Data">Data</a></td><td>
data[] pointer</td></tr><tr><td>![Public property](media/pubproperty.gif "Public property")</td><td><a href="P_NSS_Blast_BlastPackageData_DataSegmentPtr">DataSegmentPtr</a></td><td>
pointer to data segment as calculated from package configuration</td></tr><tr><td>![Public property](media/pubproperty.gif "Public property")</td><td><a href="P_NSS_Blast_BlastPackageData_DataSegmentSize">DataSegmentSize</a></td><td>
the size if stack would be included, while on normal packaging mode there is no seperate buffer there is a size</td></tr><tr><td>![Public property](media/pubproperty.gif "Public property")</td><td><a href="P_NSS_Blast_BlastPackageData_DataSegmentStackOffset">DataSegmentStackOffset</a></td><td>
stack offset in bytes as seen from the start of datasegment - stack and datasegment MUST ALWAYS be in the same segment so that Data[last] == Stack[-1] - when using .Stack[] offset is 0</td></tr><tr><td>![Public property](media/pubproperty.gif "Public property")</td><td><a href="P_NSS_Blast_BlastPackageData_DataSize">DataSize</a></td><td>
data size in bytes, may include alignment bytes</td></tr><tr><td>![Public property](media/pubproperty.gif "Public property")</td><td><a href="P_NSS_Blast_BlastPackageData_HasStack">HasStack</a></td><td>
true if memory for stack is allocated, false if not: - yield not possible without persistant stack - we call it TLS, thread level stack/storage - Faster in small multithreaded bursts, benefit fades in very large bursts</td></tr><tr><td>![Public property](media/pubproperty.gif "Public property")</td><td><a href="P_NSS_Blast_BlastPackageData_IsAllocated">IsAllocated</a></td><td>
true if memory has been allocated for this object</td></tr><tr><td>![Public property](media/pubproperty.gif "Public property")</td><td><a href="P_NSS_Blast_BlastPackageData_Metadata">Metadata</a></td><td>
metadata[], 1 byte of metadata for each dataelement in stack and datasegment</td></tr><tr><td>![Public property](media/pubproperty.gif "Public property")</td><td><a href="P_NSS_Blast_BlastPackageData_MetadataPtr">MetadataPtr</a></td><td>
Calculated pointer to metadata</td></tr><tr><td>![Public property](media/pubproperty.gif "Public property")</td><td><a href="P_NSS_Blast_BlastPackageData_MetadataSize">MetadataSize</a></td><td>
metadata size in bytes, may include alignment bytes in normal and SSMD modes</td></tr><tr><td>![Public property](media/pubproperty.gif "Public property")</td><td><a href="P_NSS_Blast_BlastPackageData_SSMDDataSize">SSMDDataSize</a></td><td>
bytesize for needed BlastSSMDDataStack records</td></tr><tr><td>![Public property](media/pubproperty.gif "Public property")</td><td><a href="P_NSS_Blast_BlastPackageData_Stack">Stack</a></td><td>
Stack[] pointer -> Data[last] == Stack[-1]</td></tr><tr><td>![Public property](media/pubproperty.gif "Public property")</td><td><a href="P_NSS_Blast_BlastPackageData_StackCapacity">StackCapacity</a></td><td>
the maxiumum stack capacity in elements (1 element == 32 bit) - looks at flags, if == nostack then returns a capacity of 0 - IMPORTANT NOTE: looking at profiler this really should be cached if asked a lot</td></tr><tr><td>![Public property](media/pubproperty.gif "Public property")</td><td><a href="P_NSS_Blast_BlastPackageData_StackSegmentPtr">StackSegmentPtr</a></td><td>
pointer to stack segment as calculated from package configuration</td></tr><tr><td>![Public property](media/pubproperty.gif "Public property")</td><td><a href="P_NSS_Blast_BlastPackageData_StackSize">StackSize</a></td><td>
stack size in bytes, may not be a multiple of elementsize/4bytes</td></tr></table>&nbsp;
<a href="#blastpackagedata-properties">Back to Top</a>

## See Also


#### Reference
<a href="T_NSS_Blast_BlastPackageData">BlastPackageData Structure</a><br /><a href="N_NSS_Blast">NSS.Blast Namespace</a><br />