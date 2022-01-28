# BlastScriptPackage Class
 

BlastScriptPackage contains all data needed to use and execute scriptcode, it consists of: - BlastPackage -> contains the native bytecode - Variable information - IO mapping - Bursted functionpointer mapping


## Inheritance Hierarchy
<a href="https://docs.microsoft.com/dotnet/api/system.object" target="_blank" rel="noopener noreferrer">System.Object</a><br />&nbsp;&nbsp;NSS.Blast.BlastScriptPackage<br />
**Namespace:**&nbsp;<a href="N_NSS_Blast">NSS.Blast</a><br />**Assembly:**&nbsp;BLAST (in BLAST.dll) Version: 1.0.0

## Syntax

**C#**<br />
``` C#
public class BlastScriptPackage
```

The BlastScriptPackage type exposes the following members.


## Constructors
&nbsp;<table><tr><th></th><th>Name</th><th>Description</th></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_NSS_Blast_BlastScriptPackage__ctor">BlastScriptPackage</a></td><td>
Initializes a new instance of the BlastScriptPackage class</td></tr></table>&nbsp;
<a href="#blastscriptpackage-class">Back to Top</a>

## Properties
&nbsp;<table><tr><th></th><th>Name</th><th>Description</th></tr><tr><td>![Public property](media/pubproperty.gif "Public property")</td><td><a href="P_NSS_Blast_BlastScriptPackage_Flags">Flags</a></td><td>
package flags as set by attached package</td></tr><tr><td>![Public property](media/pubproperty.gif "Public property")</td><td><a href="P_NSS_Blast_BlastScriptPackage_IsAllocated">IsAllocated</a></td><td>
Returns true if native memory is allocated for the package</td></tr><tr><td>![Public property](media/pubproperty.gif "Public property")</td><td><a href="P_NSS_Blast_BlastScriptPackage_IsBurstCompiled">IsBurstCompiled</a></td><td>
True if this package has also been burstcompiled and can be executed with a native function pointer</td></tr><tr><td>![Public property](media/pubproperty.gif "Public property")</td><td><a href="P_NSS_Blast_BlastScriptPackage_PackageMode">PackageMode</a></td><td>
packaging mode set by attached package data</td></tr><tr><td>![Public property](media/pubproperty.gif "Public property")</td><td><a href="P_NSS_Blast_BlastScriptPackage_PackageSize">PackageSize</a></td><td>
size of package data</td></tr></table>&nbsp;
<a href="#blastscriptpackage-class">Back to Top</a>

## Methods
&nbsp;<table><tr><th></th><th>Name</th><th>Description</th></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_NSS_Blast_BlastScriptPackage_Destroy">Destroy</a></td><td>
Destroy any allocated native memory</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="https://docs.microsoft.com/dotnet/api/system.object.equals#system-object-equals(system-object)" target="_blank" rel="noopener noreferrer">Equals</a></td><td>
Determines whether the specified object is equal to the current object.
 (Inherited from <a href="https://docs.microsoft.com/dotnet/api/system.object" target="_blank" rel="noopener noreferrer">Object</a>.)</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_NSS_Blast_BlastScriptPackage_Execute">Execute</a></td><td>
execute the script in the given environment with the supplied data</td></tr><tr><td>![Protected method](media/protmethod.gif "Protected method")</td><td><a href="https://docs.microsoft.com/dotnet/api/system.object.finalize#system-object-finalize" target="_blank" rel="noopener noreferrer">Finalize</a></td><td>
Allows an object to try to free resources and perform other cleanup operations before it is reclaimed by garbage collection.
 (Inherited from <a href="https://docs.microsoft.com/dotnet/api/system.object" target="_blank" rel="noopener noreferrer">Object</a>.)</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_NSS_Blast_BlastScriptPackage_GetCodeSegmentText">GetCodeSegmentText</a></td><td>
Get a string representation of the bytecode, example output: 000| push compound 1 + 2 nop push function max ^ pop 2 010| debug pop nop 000| 030 085 002 086 000 029 042 009 025 086 010| 255 253 025 000</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_NSS_Blast_BlastScriptPackage_GetDataSegmentText">GetDataSegmentText</a></td><td>
Get a string representation of the datasegement</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="https://docs.microsoft.com/dotnet/api/system.object.gethashcode#system-object-gethashcode" target="_blank" rel="noopener noreferrer">GetHashCode</a></td><td>
Serves as the default hash function.
 (Inherited from <a href="https://docs.microsoft.com/dotnet/api/system.object" target="_blank" rel="noopener noreferrer">Object</a>.)</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_NSS_Blast_BlastScriptPackage_GetPackageCodeBytesText">GetPackageCodeBytesText</a></td><td>
get code as 000| 000 000 000 000</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_NSS_Blast_BlastScriptPackage_GetPackageDataBytesText">GetPackageDataBytesText</a></td><td>
get datasegment as 000| 000 000 000 000</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_NSS_Blast_BlastScriptPackage_GetPackageInfoText">GetPackageInfoText</a></td><td>
return overview of package information</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="https://docs.microsoft.com/dotnet/api/system.object.gettype#system-object-gettype" target="_blank" rel="noopener noreferrer">GetType</a></td><td>
Gets the <a href="https://docs.microsoft.com/dotnet/api/system.type" target="_blank" rel="noopener noreferrer">Type</a> of the current instance.
 (Inherited from <a href="https://docs.microsoft.com/dotnet/api/system.object" target="_blank" rel="noopener noreferrer">Object</a>.)</td></tr><tr><td>![Protected method](media/protmethod.gif "Protected method")</td><td><a href="https://docs.microsoft.com/dotnet/api/system.object.memberwiseclone#system-object-memberwiseclone" target="_blank" rel="noopener noreferrer">MemberwiseClone</a></td><td>
Creates a shallow copy of the current <a href="https://docs.microsoft.com/dotnet/api/system.object" target="_blank" rel="noopener noreferrer">Object</a>.
 (Inherited from <a href="https://docs.microsoft.com/dotnet/api/system.object" target="_blank" rel="noopener noreferrer">Object</a>.)</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_NSS_Blast_BlastScriptPackage_ToString">ToString</a></td><td>
ToString overload for more information during debugging
 (Overrides <a href="https://docs.microsoft.com/dotnet/api/system.object.tostring#system-object-tostring" target="_blank" rel="noopener noreferrer">Object.ToString()</a>.)</td></tr></table>&nbsp;
<a href="#blastscriptpackage-class">Back to Top</a>

## Fields
&nbsp;<table><tr><th></th><th>Name</th><th>Description</th></tr><tr><td>![Public field](media/pubfield.gif "Public field")</td><td><a href="F_NSS_Blast_BlastScriptPackage_Bursted">Bursted</a></td><td>
A functionpointer to burst transpiled bytecode, possible if the package was known at compile time. SSMD mode will still use the bytecode package.</td></tr><tr><td>![Public field](media/pubfield.gif "Public field")</td><td><a href="F_NSS_Blast_BlastScriptPackage_Inputs">Inputs</a></td><td>
defined inputs</td></tr><tr><td>![Public field](media/pubfield.gif "Public field")</td><td><a href="F_NSS_Blast_BlastScriptPackage_Outputs">Outputs</a></td><td>
defined outputs</td></tr><tr><td>![Public field](media/pubfield.gif "Public field")</td><td><a href="F_NSS_Blast_BlastScriptPackage_Package">Package</a></td><td>
the bytecode package</td></tr><tr><td>![Public field](media/pubfield.gif "Public field")</td><td><a href="F_NSS_Blast_BlastScriptPackage_VariableOffsets">VariableOffsets</a></td><td>
Offsets for variables in element count into datasegment, an elements is 4 bytes large == 1 float</td></tr><tr><td>![Public field](media/pubfield.gif "Public field")</td><td><a href="F_NSS_Blast_BlastScriptPackage_Variables">Variables</a></td><td>
Variable information</td></tr></table>&nbsp;
<a href="#blastscriptpackage-class">Back to Top</a>

## See Also


#### Reference
<a href="N_NSS_Blast">NSS.Blast Namespace</a><br />