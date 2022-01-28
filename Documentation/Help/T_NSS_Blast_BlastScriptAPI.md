# BlastScriptAPI Class
 

For now, just a collection of function pointers that holds a native list with function information


## Inheritance Hierarchy
<a href="https://docs.microsoft.com/dotnet/api/system.object" target="_blank" rel="noopener noreferrer">System.Object</a><br />&nbsp;&nbsp;NSS.Blast.BlastScriptAPI<br />&nbsp;&nbsp;&nbsp;&nbsp;<a href="T_NSS_Blast_CoreAPI">NSS.Blast.CoreAPI</a><br />
**Namespace:**&nbsp;<a href="N_NSS_Blast">NSS.Blast</a><br />**Assembly:**&nbsp;BLAST (in BLAST.dll) Version: 1.0.0

## Syntax

**C#**<br />
``` C#
public abstract class BlastScriptAPI : IDisposable
```

The BlastScriptAPI type exposes the following members.


## Constructors
&nbsp;<table><tr><th></th><th>Name</th><th>Description</th></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_NSS_Blast_BlastScriptAPI__ctor">BlastScriptAPI</a></td><td>
Constructor for a new blastscriptapi instance, provides native allocator</td></tr></table>&nbsp;
<a href="#blastscriptapi-class">Back to Top</a>

## Properties
&nbsp;<table><tr><th></th><th>Name</th><th>Description</th></tr><tr><td>![Public property](media/pubproperty.gif "Public property")</td><td><a href="P_NSS_Blast_BlastScriptAPI_FunctionCount">FunctionCount</a></td><td>
number of functions defined in this api</td></tr><tr><td>![Public property](media/pubproperty.gif "Public property")</td><td><a href="P_NSS_Blast_BlastScriptAPI_IsInitialized">IsInitialized</a></td><td>
true if native function pointer data is initialized/allocated</td></tr><tr><td>![Public property](media/pubproperty.gif "Public property")</td><td><a href="P_NSS_Blast_BlastScriptAPI_Name">Name</a></td><td>
name of the api</td></tr></table>&nbsp;
<a href="#blastscriptapi-class">Back to Top</a>

## Methods
&nbsp;<table><tr><th></th><th>Name</th><th>Description</th></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_NSS_Blast_BlastScriptAPI_Dispose">Dispose</a></td><td>
dispose of any native memory that could be allocated for holding function pointer information</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="https://docs.microsoft.com/dotnet/api/system.object.equals#system-object-equals(system-object)" target="_blank" rel="noopener noreferrer">Equals</a></td><td>
Determines whether the specified object is equal to the current object.
 (Inherited from <a href="https://docs.microsoft.com/dotnet/api/system.object" target="_blank" rel="noopener noreferrer">Object</a>.)</td></tr><tr><td>![Protected method](media/protmethod.gif "Protected method")</td><td><a href="https://docs.microsoft.com/dotnet/api/system.object.finalize#system-object-finalize" target="_blank" rel="noopener noreferrer">Finalize</a></td><td>
Allows an object to try to free resources and perform other cleanup operations before it is reclaimed by garbage collection.
 (Inherited from <a href="https://docs.microsoft.com/dotnet/api/system.object" target="_blank" rel="noopener noreferrer">Object</a>.)</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_NSS_Blast_BlastScriptAPI_GetFunctionById_1">GetFunctionById(Int32)</a></td><td>
lookup a function definition that is directly linked to an interpretor operation</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_NSS_Blast_BlastScriptAPI_GetFunctionById">GetFunctionById(ReservedBlastScriptFunctionIds)</a></td><td>
lookup a function definition that is directly linked to an interpretor operation</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_NSS_Blast_BlastScriptAPI_GetFunctionByName">GetFunctionByName</a></td><td>
lookup a function definition that is directly linked to an interpretor operation</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_NSS_Blast_BlastScriptAPI_GetFunctionByOpCode">GetFunctionByOpCode</a></td><td>
lookup a function definition that is directly linked to an interpretor operation</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_NSS_Blast_BlastScriptAPI_GetFunctionDataById">GetFunctionDataById</a></td><td>
Directly get a function using the id as an index</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="https://docs.microsoft.com/dotnet/api/system.object.gethashcode#system-object-gethashcode" target="_blank" rel="noopener noreferrer">GetHashCode</a></td><td>
Serves as the default hash function.
 (Inherited from <a href="https://docs.microsoft.com/dotnet/api/system.object" target="_blank" rel="noopener noreferrer">Object</a>.)</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="https://docs.microsoft.com/dotnet/api/system.object.gettype#system-object-gettype" target="_blank" rel="noopener noreferrer">GetType</a></td><td>
Gets the <a href="https://docs.microsoft.com/dotnet/api/system.type" target="_blank" rel="noopener noreferrer">Type</a> of the current instance.
 (Inherited from <a href="https://docs.microsoft.com/dotnet/api/system.object" target="_blank" rel="noopener noreferrer">Object</a>.)</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_NSS_Blast_BlastScriptAPI_Initialize">Initialize</a></td><td>
Initialize native function pointer stack</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_NSS_Blast_BlastScriptAPI_IsVariableParamFunction">IsVariableParamFunction</a></td><td>
returns true if the function accepts a variable sized list of paramaters</td></tr><tr><td>![Protected method](media/protmethod.gif "Protected method")</td><td><a href="https://docs.microsoft.com/dotnet/api/system.object.memberwiseclone#system-object-memberwiseclone" target="_blank" rel="noopener noreferrer">MemberwiseClone</a></td><td>
Creates a shallow copy of the current <a href="https://docs.microsoft.com/dotnet/api/system.object" target="_blank" rel="noopener noreferrer">Object</a>.
 (Inherited from <a href="https://docs.microsoft.com/dotnet/api/system.object" target="_blank" rel="noopener noreferrer">Object</a>.)</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_NSS_Blast_BlastScriptAPI_RegisterFunction_4">RegisterFunction(String, IntPtr, Int32, Int32, Int32)</a></td><td>
Register a function with blast</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_NSS_Blast_BlastScriptAPI_RegisterFunction_2">RegisterFunction(String, Int32, Int32, Int32, Int32, blast_operation)</a></td><td>
Register a function with blast</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_NSS_Blast_BlastScriptAPI_RegisterFunction_3">RegisterFunction(String, Int32, Int32, Int32, Int32, extended_blast_operation)</a></td><td>
Register a function with blast</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_NSS_Blast_BlastScriptAPI_RegisterFunction">RegisterFunction(ReservedBlastScriptFunctionIds, String, Int32, Int32, Int32, Int32, blast_operation)</a></td><td>
Register a function with blast</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_NSS_Blast_BlastScriptAPI_RegisterFunction_1">RegisterFunction(ReservedBlastScriptFunctionIds, String, Int32, Int32, Int32, Int32, extended_blast_operation)</a></td><td>
Register a function with blast</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="https://docs.microsoft.com/dotnet/api/system.object.tostring#system-object-tostring" target="_blank" rel="noopener noreferrer">ToString</a></td><td>
Returns a string that represents the current object.
 (Inherited from <a href="https://docs.microsoft.com/dotnet/api/system.object" target="_blank" rel="noopener noreferrer">Object</a>.)</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_NSS_Blast_BlastScriptAPI_TryGetFunctionCallById">TryGetFunctionCallById</a></td><td>
try to get a function by id, in release it just gets it, in debug it checks</td></tr></table>&nbsp;
<a href="#blastscriptapi-class">Back to Top</a>

## Fields
&nbsp;<table><tr><th></th><th>Name</th><th>Description</th></tr><tr><td>![Public field](media/pubfield.gif "Public field")</td><td><a href="F_NSS_Blast_BlastScriptAPI_Allocator">Allocator</a></td><td>
allocated to be used for any data comming from this api</td></tr><tr><td>![Public field](media/pubfield.gif "Public field")</td><td><a href="F_NSS_Blast_BlastScriptAPI_FunctionInfo">FunctionInfo</a></td><td>
managed information, includes a copy of the native info</td></tr><tr><td>![Public field](media/pubfield.gif "Public field")</td><td><a href="F_NSS_Blast_BlastScriptAPI_Functions">Functions</a></td><td>
native function definition info</td></tr></table>&nbsp;
<a href="#blastscriptapi-class">Back to Top</a>

## See Also


#### Reference
<a href="N_NSS_Blast">NSS.Blast Namespace</a><br />