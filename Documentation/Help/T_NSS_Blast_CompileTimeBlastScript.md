# CompileTimeBlastScript Class
 

any script derived from this will be compiled during compilation/design public class BS1_compiletime : CompileTimeBlastScript { public override string Code => @"a = a + 1;"; }


## Inheritance Hierarchy
<a href="https://docs.microsoft.com/dotnet/api/system.object" target="_blank" rel="noopener noreferrer">System.Object</a><br />&nbsp;&nbsp;<a href="T_NSS_Blast_BlastScript">NSS.Blast.BlastScript</a><br />&nbsp;&nbsp;&nbsp;&nbsp;NSS.Blast.CompileTimeBlastScript<br />
**Namespace:**&nbsp;<a href="N_NSS_Blast">NSS.Blast</a><br />**Assembly:**&nbsp;BLAST (in BLAST.dll) Version: 1.0.0

## Syntax

**C#**<br />
``` C#
public abstract class CompileTimeBlastScript : BlastScript
```

The CompileTimeBlastScript type exposes the following members.


## Constructors
&nbsp;<table><tr><th></th><th>Name</th><th>Description</th></tr><tr><td>![Protected method](media/protmethod.gif "Protected method")</td><td><a href="M_NSS_Blast_CompileTimeBlastScript__ctor">CompileTimeBlastScript</a></td><td>
Initializes a new instance of the CompileTimeBlastScript class</td></tr></table>&nbsp;
<a href="#compiletimeblastscript-class">Back to Top</a>

## Properties
&nbsp;<table><tr><th></th><th>Name</th><th>Description</th></tr><tr><td>![Public property](media/pubproperty.gif "Public property")</td><td><a href="P_NSS_Blast_BlastScript_Code">Code</a></td><td>
the actual scriptcode conforming to the languageversion set
 (Inherited from <a href="T_NSS_Blast_BlastScript">BlastScript</a>.)</td></tr><tr><td>![Public property](media/pubproperty.gif "Public property")</td><td><a href="P_NSS_Blast_BlastScript_Id">Id</a></td><td>
Blast scriptid, used to uniquely identify the script. It is used throughout blast to id the packaged script
 (Inherited from <a href="T_NSS_Blast_BlastScript">BlastScript</a>.)</td></tr><tr><td>![Public property](media/pubproperty.gif "Public property")</td><td><a href="P_NSS_Blast_BlastScript_IsPackaged">IsPackaged</a></td><td>
true if the script has been packaged / prepared
 (Inherited from <a href="T_NSS_Blast_BlastScript">BlastScript</a>.)</td></tr><tr><td>![Public property](media/pubproperty.gif "Public property")</td><td><a href="P_NSS_Blast_BlastScript_IsPrepared">IsPrepared</a></td><td>
true if the script has been packaged / prepared
 (Inherited from <a href="T_NSS_Blast_BlastScript">BlastScript</a>.)</td></tr><tr><td>![Public property](media/pubproperty.gif "Public property")</td><td><a href="P_NSS_Blast_BlastScript_LanguageVersion">LanguageVersion</a></td><td>
Target language vesion, depending on compilation settings it might change in the package but the user should ensure that the code is compatible;
 (Inherited from <a href="T_NSS_Blast_BlastScript">BlastScript</a>.)</td></tr><tr><td>![Public property](media/pubproperty.gif "Public property")</td><td><a href="P_NSS_Blast_BlastScript_Name">Name</a></td><td>
The name of the script as used in messages
 (Inherited from <a href="T_NSS_Blast_BlastScript">BlastScript</a>.)</td></tr><tr><td>![Public property](media/pubproperty.gif "Public property")</td><td><a href="P_NSS_Blast_BlastScript_Package">Package</a></td><td>
native package data, contains references to code and data segments
 (Inherited from <a href="T_NSS_Blast_BlastScript">BlastScript</a>.)</td></tr></table>&nbsp;
<a href="#compiletimeblastscript-class">Back to Top</a>

## Methods
&nbsp;<table><tr><th></th><th>Name</th><th>Description</th></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_NSS_Blast_BlastScript_Dispose">Dispose</a></td><td>
cleanup package data if allocated on dispose
 (Inherited from <a href="T_NSS_Blast_BlastScript">BlastScript</a>.)</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="https://docs.microsoft.com/dotnet/api/system.object.equals#system-object-equals(system-object)" target="_blank" rel="noopener noreferrer">Equals</a></td><td>
Determines whether the specified object is equal to the current object.
 (Inherited from <a href="https://docs.microsoft.com/dotnet/api/system.object" target="_blank" rel="noopener noreferrer">Object</a>.)</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_NSS_Blast_BlastScript_Execute">Execute</a></td><td>
execute the script in the given environment with the supplied data
 (Inherited from <a href="T_NSS_Blast_BlastScript">BlastScript</a>.)</td></tr><tr><td>![Protected method](media/protmethod.gif "Protected method")</td><td><a href="https://docs.microsoft.com/dotnet/api/system.object.finalize#system-object-finalize" target="_blank" rel="noopener noreferrer">Finalize</a></td><td>
Allows an object to try to free resources and perform other cleanup operations before it is reclaimed by garbage collection.
 (Inherited from <a href="https://docs.microsoft.com/dotnet/api/system.object" target="_blank" rel="noopener noreferrer">Object</a>.)</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="https://docs.microsoft.com/dotnet/api/system.object.gethashcode#system-object-gethashcode" target="_blank" rel="noopener noreferrer">GetHashCode</a></td><td>
Serves as the default hash function.
 (Inherited from <a href="https://docs.microsoft.com/dotnet/api/system.object" target="_blank" rel="noopener noreferrer">Object</a>.)</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="https://docs.microsoft.com/dotnet/api/system.object.gettype#system-object-gettype" target="_blank" rel="noopener noreferrer">GetType</a></td><td>
Gets the <a href="https://docs.microsoft.com/dotnet/api/system.type" target="_blank" rel="noopener noreferrer">Type</a> of the current instance.
 (Inherited from <a href="https://docs.microsoft.com/dotnet/api/system.object" target="_blank" rel="noopener noreferrer">Object</a>.)</td></tr><tr><td>![Protected method](media/protmethod.gif "Protected method")</td><td><a href="https://docs.microsoft.com/dotnet/api/system.object.memberwiseclone#system-object-memberwiseclone" target="_blank" rel="noopener noreferrer">MemberwiseClone</a></td><td>
Creates a shallow copy of the current <a href="https://docs.microsoft.com/dotnet/api/system.object" target="_blank" rel="noopener noreferrer">Object</a>.
 (Inherited from <a href="https://docs.microsoft.com/dotnet/api/system.object" target="_blank" rel="noopener noreferrer">Object</a>.)</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_NSS_Blast_BlastScript_Prepare">Prepare</a></td><td>
Prepare the script for execution
 (Inherited from <a href="T_NSS_Blast_BlastScript">BlastScript</a>.)</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_NSS_Blast_BlastScript_ReleasePackage">ReleasePackage</a></td><td>
release package memory, nulls reference to it and free;s any native package memory with it
 (Inherited from <a href="T_NSS_Blast_BlastScript">BlastScript</a>.)</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_NSS_Blast_BlastScript_ToString">ToString</a></td><td>
get the string representation of a blast script as ID:Name
 (Inherited from <a href="T_NSS_Blast_BlastScript">BlastScript</a>.)</td></tr></table>&nbsp;
<a href="#compiletimeblastscript-class">Back to Top</a>

## See Also


#### Reference
<a href="N_NSS_Blast">NSS.Blast Namespace</a><br />