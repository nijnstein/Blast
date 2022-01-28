# Blast Structure
 

Blast Engine

**Namespace:**&nbsp;<a href="N_NSS_Blast">NSS.Blast</a><br />**Assembly:**&nbsp;BLAST (in BLAST.dll) Version: 1.0.0

## Syntax

**C#**<br />
``` C#
public struct Blast
```

The Blast type exposes the following members.


## Properties
&nbsp;<table><tr><th></th><th>Name</th><th>Description</th></tr><tr><td>![Public property](media/pubproperty.gif "Public property")</td><td><a href="P_NSS_Blast_Blast_Engine">Engine</a></td><td>
IntPtr to global data object used by interpretor, it holds references to constant values and function pointers</td></tr><tr><td>![Public property](media/pubproperty.gif "Public property")</td><td><a href="P_NSS_Blast_Blast_HPCJobs">HPCJobs</a></td><td /></tr><tr><td>![Public property](media/pubproperty.gif "Public property")</td><td><a href="P_NSS_Blast_Blast_IsCreated">IsCreated</a></td><td>
true if the structure is initialized and memory is allocated</td></tr><tr><td>![Public property](media/pubproperty.gif "Public property")![Static member](media/static.gif "Static member")</td><td><a href="P_NSS_Blast_Blast_Scripts">Scripts</a></td><td>
Enumerates all scripts known by blast</td></tr></table>&nbsp;
<a href="#blast-structure">Back to Top</a>

## Methods
&nbsp;<table><tr><th></th><th>Name</th><th>Description</th></tr><tr><td>![Public method](media/pubmethod.gif "Public method")![Static member](media/static.gif "Static member")</td><td><a href="M_NSS_Blast_Blast_CompileIntoDesignTimeRegistry">CompileIntoDesignTimeRegistry</a></td><td>
compile the script into the designtime registry</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")![Static member](media/static.gif "Static member")</td><td><a href="M_NSS_Blast_Blast_Create_1">Create(Allocator)</a></td><td>
create a new instance of BLAST use the core scriptfunction api</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")![Static member](media/static.gif "Static member")</td><td><a href="M_NSS_Blast_Blast_Create">Create(BlastScriptAPI, Allocator)</a></td><td>
create a new instance of blast using a customized api</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_NSS_Blast_Blast_Destroy">Destroy</a></td><td>
release all memory allocated by this instance of blast</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="https://docs.microsoft.com/dotnet/api/system.valuetype.equals#system-valuetype-equals(system-object)" target="_blank" rel="noopener noreferrer">Equals</a></td><td>
Indicates whether this instance and a specified object are equal.
 (Inherited from <a href="https://docs.microsoft.com/dotnet/api/system.valuetype" target="_blank" rel="noopener noreferrer">ValueType</a>.)</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_NSS_Blast_Blast_Execute_2">Execute(BlastPackageData)</a></td><td>
Execute a Package</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_NSS_Blast_Blast_Execute_7">Execute(BlastScriptPackage)</a></td><td>
Execute a Package</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_NSS_Blast_Blast_Execute_3">Execute(BlastPackageData, IntPtr)</a></td><td>
Execute a Package</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_NSS_Blast_Blast_Execute_8">Execute(BlastScriptPackage, IntPtr)</a></td><td>
Execute a Package</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_NSS_Blast_Blast_Execute_4">Execute(BlastPackageData, IntPtr, Int32)</a></td><td>
Execute a Package in ssmd mode</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_NSS_Blast_Blast_Execute_5">Execute(BlastPackageData, IntPtr, IntPtr)</a></td><td>
Execute a Package</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_NSS_Blast_Blast_Execute_9">Execute(BlastScriptPackage, IntPtr, Int32)</a></td><td>
Execute a Package in ssmd mode</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_NSS_Blast_Blast_Execute_10">Execute(BlastScriptPackage, IntPtr, IntPtr)</a></td><td>
Execute a Package</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")![Static member](media/static.gif "Static member")</td><td><a href="M_NSS_Blast_Blast_Execute">Execute(BlastEngineDataPtr, BlastPackageData, IntPtr, IntPtr)</a></td><td>
Execute a Package</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_NSS_Blast_Blast_Execute_6">Execute(BlastPackageData, IntPtr, IntPtr, Int32)</a></td><td>
Execute a Package in ssmd mode</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_NSS_Blast_Blast_Execute_11">Execute(BlastScriptPackage, IntPtr, IntPtr, Int32)</a></td><td>
Execute a Package in ssmd mode</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")![Static member](media/static.gif "Static member")</td><td><a href="M_NSS_Blast_Blast_Execute_1">Execute(BlastEngineDataPtr, BlastPackageData, IntPtr, IntPtr, Int32)</a></td><td>
Execute a Package in ssmd mode</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")![Static member](media/static.gif "Static member")</td><td><a href="M_NSS_Blast_Blast_GetBlastOperationFromToken">GetBlastOperationFromToken</a></td><td>
get the corresponding operation opcode from a given script token</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")![Static member](media/static.gif "Static member")</td><td><a href="M_NSS_Blast_Blast_GetConstantValue">GetConstantValue</a></td><td>
get the constant numeric value of the operation</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")![Static member](media/static.gif "Static member")</td><td><a href="M_NSS_Blast_Blast_GetConstantValueOperation">GetConstantValueOperation</a></td><td>
get the script_op belonging to a constant value, eiter by name or value</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="https://docs.microsoft.com/dotnet/api/system.valuetype.gethashcode#system-valuetype-gethashcode" target="_blank" rel="noopener noreferrer">GetHashCode</a></td><td>
Returns the hash code for this instance.
 (Inherited from <a href="https://docs.microsoft.com/dotnet/api/system.valuetype" target="_blank" rel="noopener noreferrer">ValueType</a>.)</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_NSS_Blast_Blast_GetHPCJob">GetHPCJob</a></td><td /></tr><tr><td>![Public method](media/pubmethod.gif "Public method")![Static member](media/static.gif "Static member")</td><td><a href="M_NSS_Blast_Blast_GetNamedSystemConstantValue">GetNamedSystemConstantValue</a></td><td>
get the value of a named system constant</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")![Static member](media/static.gif "Static member")</td><td><a href="M_NSS_Blast_Blast_GetReadableByteCode">GetReadableByteCode</a></td><td>
get somewhat readable assembly from bytes</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="https://docs.microsoft.com/dotnet/api/system.object.gettype#system-object-gettype" target="_blank" rel="noopener noreferrer">GetType</a></td><td>
Gets the <a href="https://docs.microsoft.com/dotnet/api/system.type" target="_blank" rel="noopener noreferrer">Type</a> of the current instance.
 (Inherited from <a href="https://docs.microsoft.com/dotnet/api/system.object" target="_blank" rel="noopener noreferrer">Object</a>.)</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_NSS_Blast_Blast_Intermediate">Intermediate</a></td><td>
compile the compiler intermediate</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")![Static member](media/static.gif "Static member")</td><td><a href="M_NSS_Blast_Blast_IsError">IsError</a></td><td>
return if an error (!yield and !success)</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")![Static member](media/static.gif "Static member")</td><td><a href="M_NSS_Blast_Blast_IsJumpOperation">IsJumpOperation</a></td><td>
check if the operation is a jump (jz, jnz, jump, jump_back)</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")![Static member](media/static.gif "Static member")</td><td><a href="M_NSS_Blast_Blast_IsNamedSystemConstant">IsNamedSystemConstant</a></td><td>
check if name matches a named system constant like 'PI' or 'NaN'</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")![Static member](media/static.gif "Static member")</td><td><a href="M_NSS_Blast_Blast_IsSuccess">IsSuccess</a></td><td>
return if an error code actually means success</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_NSS_Blast_Blast_Package_2">Package(BlastScript, BlastCompilerOptions)</a></td><td>
Compile and package the script</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_NSS_Blast_Blast_Package_3">Package(String, BlastCompilerOptions)</a></td><td>
Complile the code and package into blastscriptpackage</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")![Static member](media/static.gif "Static member")</td><td><a href="M_NSS_Blast_Blast_Package">Package(BlastEngineDataPtr, BlastScript, BlastCompilerOptions)</a></td><td>
Compile and package script code</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")![Static member](media/static.gif "Static member")</td><td><a href="M_NSS_Blast_Blast_Package_1">Package(BlastEngineDataPtr, String, BlastCompilerOptions)</a></td><td>
Compile and package script code</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="https://docs.microsoft.com/dotnet/api/system.valuetype.tostring#system-valuetype-tostring" target="_blank" rel="noopener noreferrer">ToString</a></td><td>
Returns the fully qualified type name of this instance.
 (Inherited from <a href="https://docs.microsoft.com/dotnet/api/system.valuetype" target="_blank" rel="noopener noreferrer">ValueType</a>.)</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")![Static member](media/static.gif "Static member")</td><td><a href="M_NSS_Blast_Blast_VisualizeTokens">VisualizeTokens</a></td><td>
visualize a list of tokens and identifiers into a somewhat readable string</td></tr></table>&nbsp;
<a href="#blast-structure">Back to Top</a>

## Fields
&nbsp;<table><tr><th></th><th>Name</th><th>Description</th></tr><tr><td>![Public field](media/pubfield.gif "Public field")</td><td><a href="F_NSS_Blast_Blast_API">API</a></td><td>
the current set of script accessible functions, check OwnScriptAPIMemory to see if this instance of blast is the owner of the memory used in the API</td></tr><tr><td>![Public field](media/pubfield.gif "Public field")![Static member](media/static.gif "Static member")</td><td><a href="F_NSS_Blast_Blast_Comment">Comment</a></td><td>
the comment character</td></tr><tr><td>![Public field](media/pubfield.gif "Public field")![Static member](media/static.gif "Static member")</td><td><a href="F_NSS_Blast_Blast_InvalidNumeric">InvalidNumeric</a></td><td>
the value used for invalid numerics</td></tr><tr><td>![Public field](media/pubfield.gif "Public field")![Static member](media/static.gif "Static member")</td><td><a href="F_NSS_Blast_Blast_MaximumFunctionNameLength">MaximumFunctionNameLength</a></td><td>
the maximum length of a function name in number of ASCII characters</td></tr><tr><td>![Public field](media/pubfield.gif "Public field")![Static member](media/static.gif "Static member")</td><td><a href="F_NSS_Blast_Blast_StackFillPattern">StackFillPattern</a></td><td>
The fill pattern for stack on initialize, easier to spot bugs if stack is filled with a pattern instead of zeros or random which might have different causes, something bugged setting all bytes to 101 should be very suspicious</td></tr><tr><td>![Public field](media/pubfield.gif "Public field")![Static member](media/static.gif "Static member")</td><td><a href="F_NSS_Blast_Blast_Tokens">Tokens</a></td><td>
defines tokens that can be used in script</td></tr><tr><td>![Public field](media/pubfield.gif "Public field")![Static member](media/static.gif "Static member")</td><td><a href="F_NSS_Blast_Blast_ValueOperations">ValueOperations</a></td><td>
list all value operations, these operations directly encode constant values -> burst should be able to access this if needed..</td></tr></table>&nbsp;
<a href="#blast-structure">Back to Top</a>

## See Also


#### Reference
<a href="N_NSS_Blast">NSS.Blast Namespace</a><br />