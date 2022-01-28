# BlastScriptFunction Structure
 

burst compatible function description

**Namespace:**&nbsp;<a href="N_NSS_Blast">NSS.Blast</a><br />**Assembly:**&nbsp;BLAST (in BLAST.dll) Version: 1.0.0

## Syntax

**C#**<br />
``` C#
public struct BlastScriptFunction
```

The BlastScriptFunction type exposes the following members.


## Properties
&nbsp;<table><tr><th></th><th>Name</th><th>Description</th></tr><tr><td>![Public property](media/pubproperty.gif "Public property")</td><td><a href="P_NSS_Blast_BlastScriptFunction_CanHaveParameters">CanHaveParameters</a></td><td>
true if the function can have parameters</td></tr><tr><td>![Public property](media/pubproperty.gif "Public property")</td><td><a href="P_NSS_Blast_BlastScriptFunction_HasVariableParameterCount">HasVariableParameterCount</a></td><td>
returns true if the function accepts a variable sized list of paramaters</td></tr><tr><td>![Public property](media/pubproperty.gif "Public property")</td><td><a href="P_NSS_Blast_BlastScriptFunction_IsExternalCall">IsExternalCall</a></td><td>
True if the functioncall is an external functionpointer</td></tr><tr><td>![Public property](media/pubproperty.gif "Public property")</td><td><a href="P_NSS_Blast_BlastScriptFunction_IsNotValid">IsNotValid</a></td><td>
returns if the function is NOT valid (checks id, assumes memory is initialized to zeros)</td></tr><tr><td>![Public property](media/pubproperty.gif "Public property")</td><td><a href="P_NSS_Blast_BlastScriptFunction_IsPopVariant">IsPopVariant</a></td><td>
true if function encodes a pop function</td></tr><tr><td>![Public property](media/pubproperty.gif "Public property")</td><td><a href="P_NSS_Blast_BlastScriptFunction_IsPushVariant">IsPushVariant</a></td><td>
true function encodes a push operation</td></tr><tr><td>![Public property](media/pubproperty.gif "Public property")</td><td><a href="P_NSS_Blast_BlastScriptFunction_IsReserved">IsReserved</a></td><td>
true if the function targets a reserved function name</td></tr><tr><td>![Public property](media/pubproperty.gif "Public property")</td><td><a href="P_NSS_Blast_BlastScriptFunction_IsValid">IsValid</a></td><td>
returns if the function is valid (checks id, assumes memory is initialized to zeros)</td></tr></table>&nbsp;
<a href="#blastscriptfunction-structure">Back to Top</a>

## Methods
&nbsp;<table><tr><th></th><th>Name</th><th>Description</th></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="https://docs.microsoft.com/dotnet/api/system.valuetype.equals#system-valuetype-equals(system-object)" target="_blank" rel="noopener noreferrer">Equals</a></td><td>
Indicates whether this instance and a specified object are equal.
 (Inherited from <a href="https://docs.microsoft.com/dotnet/api/system.valuetype" target="_blank" rel="noopener noreferrer">ValueType</a>.)</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_NSS_Blast_BlastScriptFunction_Generic__1">Generic(T)</a></td><td>
cast our functionpointer into the delegate used to call it</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_NSS_Blast_BlastScriptFunction_GetFunctionName">GetFunctionName</a></td><td>
rebuilds the functionname from a native character array (expensive operation)</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="https://docs.microsoft.com/dotnet/api/system.valuetype.gethashcode#system-valuetype-gethashcode" target="_blank" rel="noopener noreferrer">GetHashCode</a></td><td>
Returns the hash code for this instance.
 (Inherited from <a href="https://docs.microsoft.com/dotnet/api/system.valuetype" target="_blank" rel="noopener noreferrer">ValueType</a>.)</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="https://docs.microsoft.com/dotnet/api/system.object.gettype#system-object-gettype" target="_blank" rel="noopener noreferrer">GetType</a></td><td>
Gets the <a href="https://docs.microsoft.com/dotnet/api/system.type" target="_blank" rel="noopener noreferrer">Type</a> of the current instance.
 (Inherited from <a href="https://docs.microsoft.com/dotnet/api/system.object" target="_blank" rel="noopener noreferrer">Object</a>.)</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="https://docs.microsoft.com/dotnet/api/system.valuetype.tostring#system-valuetype-tostring" target="_blank" rel="noopener noreferrer">ToString</a></td><td>
Returns the fully qualified type name of this instance.
 (Inherited from <a href="https://docs.microsoft.com/dotnet/api/system.valuetype" target="_blank" rel="noopener noreferrer">ValueType</a>.)</td></tr></table>&nbsp;
<a href="#blastscriptfunction-structure">Back to Top</a>

## Fields
&nbsp;<table><tr><th></th><th>Name</th><th>Description</th></tr><tr><td>![Public field](media/pubfield.gif "Public field")</td><td><a href="F_NSS_Blast_BlastScriptFunction_AcceptsVectorSize">AcceptsVectorSize</a></td><td>
vectorsize of accepted values in parameters, set to 0 to perform no check - TODO, V2/3/4?? could make this more strict</td></tr><tr><td>![Public field](media/pubfield.gif "Public field")</td><td><a href="F_NSS_Blast_BlastScriptFunction_ExtendedScriptOp">ExtendedScriptOp</a></td><td>
Built-in functions directly compile into a script operation</td></tr><tr><td>![Public field](media/pubfield.gif "Public field")</td><td><a href="F_NSS_Blast_BlastScriptFunction_Flags">Flags</a></td><td>
Flags: - reserved: function targets a reserved function</td></tr><tr><td>![Public field](media/pubfield.gif "Public field")</td><td><a href="F_NSS_Blast_BlastScriptFunction_FunctionId">FunctionId</a></td><td>
function Id, doubles as index into function tables</td></tr><tr><td>![Public field](media/pubfield.gif "Public field")</td><td><a href="F_NSS_Blast_BlastScriptFunction_Match">Match</a></td><td>
char array to match to in lowercase, only ASCII - length == Blast.MaximumFunctionNameLength</td></tr><tr><td>![Public field](media/pubfield.gif "Public field")</td><td><a href="F_NSS_Blast_BlastScriptFunction_MaxParameterCount">MaxParameterCount</a></td><td>
maximal number of parameters, 0 for none, max = 31</td></tr><tr><td>![Public field](media/pubfield.gif "Public field")</td><td><a href="F_NSS_Blast_BlastScriptFunction_MinParameterCount">MinParameterCount</a></td><td>
minimal number of parameters</td></tr><tr><td>![Public field](media/pubfield.gif "Public field")</td><td><a href="F_NSS_Blast_BlastScriptFunction_NativeFunctionPointer">NativeFunctionPointer</a></td><td>
a native function pointer (1st member, makes sure its aligned if this struct is)</td></tr><tr><td>![Public field](media/pubfield.gif "Public field")</td><td><a href="F_NSS_Blast_BlastScriptFunction_ReturnsVectorSize">ReturnsVectorSize</a></td><td>
vectorsize of returnvalue, 0 if no value is returned</td></tr><tr><td>![Public field](media/pubfield.gif "Public field")</td><td><a href="F_NSS_Blast_BlastScriptFunction_ScriptOp">ScriptOp</a></td><td>
Built-in functions directly compile into a script operation</td></tr></table>&nbsp;
<a href="#blastscriptfunction-structure">Back to Top</a>

## See Also


#### Reference
<a href="N_NSS_Blast">NSS.Blast Namespace</a><br />