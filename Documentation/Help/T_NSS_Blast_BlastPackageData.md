# BlastPackageData Structure
 

the bare minimum data (28 bytes) to package a script for execution

**Namespace:**&nbsp;<a href="N_NSS_Blast">NSS.Blast</a><br />**Assembly:**&nbsp;BLAST (in BLAST.dll) Version: 1.0.0

## Syntax

**C#**<br />
``` C#
public struct BlastPackageData
```

The BlastPackageData type exposes the following members.


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
<a href="#blastpackagedata-structure">Back to Top</a>

## Methods
&nbsp;<table><tr><th></th><th>Name</th><th>Description</th></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_NSS_Blast_BlastPackageData_Clone">Clone()</a></td><td /></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_NSS_Blast_BlastPackageData_Clone_1">Clone(Allocator)</a></td><td /></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_NSS_Blast_BlastPackageData_CloneData">CloneData(Int32)</a></td><td>
clone n data segments into 1 block and index it with pointers - the first block contains the root pointer which can be freed</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_NSS_Blast_BlastPackageData_CloneData_1">CloneData(Int32, BlastMetaDataStack*)</a></td><td>
clone n segments into 1 memory block and index it with pointers - the first block contains the root pointer to free later</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_NSS_Blast_BlastPackageData_CloneData_2">CloneData(Int32, Allocator)</a></td><td>
clone n segments into 1 memory block and index it with pointers - the first block contains the root pointer to free later</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_NSS_Blast_BlastPackageData_CloneDataStack">CloneDataStack(Int32)</a></td><td>
clone n data segments into 1 block and index it with pointers - the first block contains the root pointer which can be freed</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_NSS_Blast_BlastPackageData_CloneDataStack_1">CloneDataStack(Int32, BlastSSMDDataStack*)</a></td><td>
clone n segments into 1 memory block and index it with pointers - the first block contains the root pointer to free later</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_NSS_Blast_BlastPackageData_CloneDataStack_2">CloneDataStack(Int32, Allocator)</a></td><td>
clone n segments into 1 memory block and index it with pointers - the first block contains the root pointer to free later</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="https://docs.microsoft.com/dotnet/api/system.valuetype.equals#system-valuetype-equals(system-object)" target="_blank" rel="noopener noreferrer">Equals</a></td><td>
Indicates whether this instance and a specified object are equal.
 (Inherited from <a href="https://docs.microsoft.com/dotnet/api/system.valuetype" target="_blank" rel="noopener noreferrer">ValueType</a>.)</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_NSS_Blast_BlastPackageData_Free">Free</a></td><td>
free any memory allocated</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")![Static member](media/static.gif "Static member")</td><td><a href="M_NSS_Blast_BlastPackageData_FreeData">FreeData(BlastMetaDataStack*, Allocator)</a></td><td>
free memory used by an ssmd data block</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")![Static member](media/static.gif "Static member")</td><td><a href="M_NSS_Blast_BlastPackageData_FreeData_1">FreeData(BlastSSMDDataStack*, Allocator)</a></td><td>
free memory used by an ssmd data block</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="https://docs.microsoft.com/dotnet/api/system.valuetype.gethashcode#system-valuetype-gethashcode" target="_blank" rel="noopener noreferrer">GetHashCode</a></td><td>
Returns the hash code for this instance.
 (Inherited from <a href="https://docs.microsoft.com/dotnet/api/system.valuetype" target="_blank" rel="noopener noreferrer">ValueType</a>.)</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="https://docs.microsoft.com/dotnet/api/system.object.gettype#system-object-gettype" target="_blank" rel="noopener noreferrer">GetType</a></td><td>
Gets the <a href="https://docs.microsoft.com/dotnet/api/system.type" target="_blank" rel="noopener noreferrer">Type</a> of the current instance.
 (Inherited from <a href="https://docs.microsoft.com/dotnet/api/system.object" target="_blank" rel="noopener noreferrer">Object</a>.)</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="https://docs.microsoft.com/dotnet/api/system.valuetype.tostring#system-valuetype-tostring" target="_blank" rel="noopener noreferrer">ToString</a></td><td>
Returns the fully qualified type name of this instance.
 (Inherited from <a href="https://docs.microsoft.com/dotnet/api/system.valuetype" target="_blank" rel="noopener noreferrer">ValueType</a>.)</td></tr></table>&nbsp;
<a href="#blastpackagedata-structure">Back to Top</a>

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
<a href="#blastpackagedata-structure">Back to Top</a>

## See Also


#### Reference
<a href="N_NSS_Blast">NSS.Blast Namespace</a><br />