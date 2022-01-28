# BlastIntermediate Structure
 

intermediate bytecode data for use by compiler

**Namespace:**&nbsp;<a href="N_NSS_Blast_Compiler">NSS.Blast.Compiler</a><br />**Assembly:**&nbsp;BLAST (in BLAST.dll) Version: 1.0.0

## Syntax

**C#**<br />
``` C#
public struct BlastIntermediate
```

The BlastIntermediate type exposes the following members.


## Properties
&nbsp;<table><tr><th></th><th>Name</th><th>Description</th></tr><tr><td>![Public property](media/pubproperty.gif "Public property")</td><td><a href="P_NSS_Blast_Compiler_BlastIntermediate_DataByteSize">DataByteSize</a></td><td /></tr><tr><td>![Public property](media/pubproperty.gif "Public property")</td><td><a href="P_NSS_Blast_Compiler_BlastIntermediate_DataCount">DataCount</a></td><td>
nr of data elements (presumably 32bits so 4bytes/element) - same as data_offset, added for clarity</td></tr></table>&nbsp;
<a href="#blastintermediate-structure">Back to Top</a>

## Methods
&nbsp;<table><tr><th></th><th>Name</th><th>Description</th></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="https://docs.microsoft.com/dotnet/api/system.valuetype.equals#system-valuetype-equals(system-object)" target="_blank" rel="noopener noreferrer">Equals</a></td><td>
Indicates whether this instance and a specified object are equal.
 (Inherited from <a href="https://docs.microsoft.com/dotnet/api/system.valuetype" target="_blank" rel="noopener noreferrer">ValueType</a>.)</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_NSS_Blast_Compiler_BlastIntermediate_Execute">Execute</a></td><td>
execute the intermediate for validation and stack info</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_NSS_Blast_Compiler_BlastIntermediate_GetDataSegmentElement">GetDataSegmentElement</a></td><td>
read a float from the datasegement at given element index</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="https://docs.microsoft.com/dotnet/api/system.valuetype.gethashcode#system-valuetype-gethashcode" target="_blank" rel="noopener noreferrer">GetHashCode</a></td><td>
Returns the hash code for this instance.
 (Inherited from <a href="https://docs.microsoft.com/dotnet/api/system.valuetype" target="_blank" rel="noopener noreferrer">ValueType</a>.)</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="https://docs.microsoft.com/dotnet/api/system.object.gettype#system-object-gettype" target="_blank" rel="noopener noreferrer">GetType</a></td><td>
Gets the <a href="https://docs.microsoft.com/dotnet/api/system.type" target="_blank" rel="noopener noreferrer">Type</a> of the current instance.
 (Inherited from <a href="https://docs.microsoft.com/dotnet/api/system.object" target="_blank" rel="noopener noreferrer">Object</a>.)</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="https://docs.microsoft.com/dotnet/api/system.valuetype.tostring#system-valuetype-tostring" target="_blank" rel="noopener noreferrer">ToString</a></td><td>
Returns the fully qualified type name of this instance.
 (Inherited from <a href="https://docs.microsoft.com/dotnet/api/system.valuetype" target="_blank" rel="noopener noreferrer">ValueType</a>.)</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_NSS_Blast_Compiler_BlastIntermediate_Validate">Validate</a></td><td>
validate intermediate</td></tr></table>&nbsp;
<a href="#blastintermediate-structure">Back to Top</a>

## Fields
&nbsp;<table><tr><th></th><th>Name</th><th>Description</th></tr><tr><td>![Public field](media/pubfield.gif "Public field")</td><td><a href="F_NSS_Blast_Compiler_BlastIntermediate_code">code</a></td><td>
byte code compiled from script</td></tr><tr><td>![Public field](media/pubfield.gif "Public field")![Static member](media/static.gif "Static member")</td><td><a href="F_NSS_Blast_Compiler_BlastIntermediate_code_capacity">code_capacity</a></td><td /></tr><tr><td>![Public field](media/pubfield.gif "Public field")</td><td><a href="F_NSS_Blast_Compiler_BlastIntermediate_code_pointer">code_pointer</a></td><td>
index into bytecode, next point of execution, if == code_size then end of script is reached</td></tr><tr><td>![Public field](media/pubfield.gif "Public field")</td><td><a href="F_NSS_Blast_Compiler_BlastIntermediate_code_size">code_size</a></td><td>
size of code in bytes</td></tr><tr><td>![Public field](media/pubfield.gif "Public field")</td><td><a href="F_NSS_Blast_Compiler_BlastIntermediate_data">data</a></td><td>
input, output and scratch data fields</td></tr><tr><td>![Public field](media/pubfield.gif "Public field")![Static member](media/static.gif "Static member")</td><td><a href="F_NSS_Blast_Compiler_BlastIntermediate_data_capacity">data_capacity</a></td><td /></tr><tr><td>![Public field](media/pubfield.gif "Public field")![Static member](media/static.gif "Static member")</td><td><a href="F_NSS_Blast_Compiler_BlastIntermediate_data_element_bytesize">data_element_bytesize</a></td><td /></tr><tr><td>![Public field](media/pubfield.gif "Public field")</td><td><a href="F_NSS_Blast_Compiler_BlastIntermediate_Id">Id</a></td><td>
unique script id</td></tr><tr><td>![Public field](media/pubfield.gif "Public field")</td><td><a href="F_NSS_Blast_Compiler_BlastIntermediate_max_stack_size">max_stack_size</a></td><td>
maximum reached stack size in floats</td></tr><tr><td>![Public field](media/pubfield.gif "Public field")</td><td><a href="F_NSS_Blast_Compiler_BlastIntermediate_metadata">metadata</a></td><td /></tr></table>&nbsp;
<a href="#blastintermediate-structure">Back to Top</a>

## See Also


#### Reference
<a href="N_NSS_Blast_Compiler">NSS.Blast.Compiler Namespace</a><br />