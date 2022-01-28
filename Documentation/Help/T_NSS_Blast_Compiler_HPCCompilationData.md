# HPCCompilationData Class
 

compiler data for the hpc compiler


## Inheritance Hierarchy
<a href="https://docs.microsoft.com/dotnet/api/system.object" target="_blank" rel="noopener noreferrer">System.Object</a><br />&nbsp;&nbsp;<a href="T_NSS_Blast_Compiler_CompilationData">NSS.Blast.Compiler.CompilationData</a><br />&nbsp;&nbsp;&nbsp;&nbsp;NSS.Blast.Compiler.HPCCompilationData<br />&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;<a href="T_NSS_Blast_Compiler_CSCompilationData">NSS.Blast.Compiler.CSCompilationData</a><br />
**Namespace:**&nbsp;<a href="N_NSS_Blast_Compiler">NSS.Blast.Compiler</a><br />**Assembly:**&nbsp;BLAST (in BLAST.dll) Version: 1.0.0

## Syntax

**C#**<br />
``` C#
public class HPCCompilationData : CompilationData
```

The HPCCompilationData type exposes the following members.


## Constructors
&nbsp;<table><tr><th></th><th>Name</th><th>Description</th></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_NSS_Blast_Compiler_HPCCompilationData__ctor">HPCCompilationData</a></td><td>
setup compilation data for the HPC compiler chain</td></tr></table>&nbsp;
<a href="#hpccompilationdata-class">Back to Top</a>

## Properties
&nbsp;<table><tr><th></th><th>Name</th><th>Description</th></tr><tr><td>![Public property](media/pubproperty.gif "Public property")</td><td><a href="P_NSS_Blast_Compiler_CompilationData_AST">AST</a></td><td>
the rootnode of the abstract syntax tree
 (Inherited from <a href="T_NSS_Blast_Compiler_CompilationData">CompilationData</a>.)</td></tr><tr><td>![Public property](media/pubproperty.gif "Public property")</td><td><a href="P_NSS_Blast_Compiler_CompilationData_Blast">Blast</a></td><td>
blast engine data
 (Inherited from <a href="T_NSS_Blast_Compiler_CompilationData">CompilationData</a>.)</td></tr><tr><td>![Public property](media/pubproperty.gif "Public property")</td><td><a href="P_NSS_Blast_Compiler_CompilationData_CanValidate">CanValidate</a></td><td> (Inherited from <a href="T_NSS_Blast_Compiler_CompilationData">CompilationData</a>.)</td></tr><tr><td>![Public property](media/pubproperty.gif "Public property")</td><td><a href="P_NSS_Blast_Compiler_CompilationData_CompilerOptions">CompilerOptions</a></td><td>
Options used during compilation
 (Inherited from <a href="T_NSS_Blast_Compiler_CompilationData">CompilationData</a>.)</td></tr><tr><td>![Public property](media/pubproperty.gif "Public property")</td><td><a href="P_NSS_Blast_Compiler_CompilationData_ConstantVariables">ConstantVariables</a></td><td>
List of constant variables (constant data needs to be somewhere)
 (Inherited from <a href="T_NSS_Blast_Compiler_CompilationData">CompilationData</a>.)</td></tr><tr><td>![Public property](media/pubproperty.gif "Public property")</td><td><a href="P_NSS_Blast_Compiler_CompilationData_Defines">Defines</a></td><td>
list of defines defined by this script
 (Inherited from <a href="T_NSS_Blast_Compiler_CompilationData">CompilationData</a>.)</td></tr><tr><td>![Public property](media/pubproperty.gif "Public property")</td><td><a href="P_NSS_Blast_Compiler_CompilationData_ErrorCount">ErrorCount</a></td><td>
number of errors that occured during compilation
 (Inherited from <a href="T_NSS_Blast_Compiler_CompilationData">CompilationData</a>.)</td></tr><tr><td>![Public property](media/pubproperty.gif "Public property")</td><td><a href="P_NSS_Blast_Compiler_CompilationData_HasDefines">HasDefines</a></td><td> (Inherited from <a href="T_NSS_Blast_Compiler_CompilationData">CompilationData</a>.)</td></tr><tr><td>![Public property](media/pubproperty.gif "Public property")</td><td><a href="P_NSS_Blast_Compiler_CompilationData_HasErrors">HasErrors</a></td><td>
number of errors that occured during compilation
 (Inherited from <a href="T_NSS_Blast_Compiler_CompilationData">CompilationData</a>.)</td></tr><tr><td>![Public property](media/pubproperty.gif "Public property")</td><td><a href="P_NSS_Blast_Compiler_CompilationData_HasErrorsOrWarnings">HasErrorsOrWarnings</a></td><td>
true if any erorr or warning occured
 (Inherited from <a href="T_NSS_Blast_Compiler_CompilationData">CompilationData</a>.)</td></tr><tr><td>![Public property](media/pubproperty.gif "Public property")</td><td><a href="P_NSS_Blast_Compiler_CompilationData_HasOffsets">HasOffsets</a></td><td> (Inherited from <a href="T_NSS_Blast_Compiler_CompilationData">CompilationData</a>.)</td></tr><tr><td>![Public property](media/pubproperty.gif "Public property")</td><td><a href="P_NSS_Blast_Compiler_CompilationData_HasVariables">HasVariables</a></td><td> (Inherited from <a href="T_NSS_Blast_Compiler_CompilationData">CompilationData</a>.)</td></tr><tr><td>![Public property](media/pubproperty.gif "Public property")</td><td><a href="P_NSS_Blast_Compiler_CompilationData_HasWarnings">HasWarnings</a></td><td>
true if any warning was logged during compilation
 (Inherited from <a href="T_NSS_Blast_Compiler_CompilationData">CompilationData</a>.)</td></tr><tr><td>![Public property](media/pubproperty.gif "Public property")</td><td><a href="P_NSS_Blast_Compiler_CompilationData_Inputs">Inputs</a></td><td>
list of inputs defined by this script
 (Inherited from <a href="T_NSS_Blast_Compiler_CompilationData">CompilationData</a>.)</td></tr><tr><td>![Public property](media/pubproperty.gif "Public property")</td><td><a href="P_NSS_Blast_Compiler_CompilationData_IsOK">IsOK</a></td><td>
true if everything went ok
 (Inherited from <a href="T_NSS_Blast_Compiler_CompilationData">CompilationData</a>.)</td></tr><tr><td>![Public property](media/pubproperty.gif "Public property")</td><td><a href="P_NSS_Blast_Compiler_CompilationData_Jumps">Jumps</a></td><td>
list of jumps
 (Inherited from <a href="T_NSS_Blast_Compiler_CompilationData">CompilationData</a>.)</td></tr><tr><td>![Public property](media/pubproperty.gif "Public property")</td><td><a href="P_NSS_Blast_Compiler_CompilationData_LastError">LastError</a></td><td>
returns the last error code or success if nothing went wrong
 (Inherited from <a href="T_NSS_Blast_Compiler_CompilationData">CompilationData</a>.)</td></tr><tr><td>![Public property](media/pubproperty.gif "Public property")</td><td><a href="P_NSS_Blast_Compiler_CompilationData_LastErrorMessage">LastErrorMessage</a></td><td>
keep reference of any last error message, voiding the need to search for it
 (Inherited from <a href="T_NSS_Blast_Compiler_CompilationData">CompilationData</a>.)</td></tr><tr><td>![Public property](media/pubproperty.gif "Public property")</td><td><a href="P_NSS_Blast_Compiler_CompilationData_OffsetCount">OffsetCount</a></td><td> (Inherited from <a href="T_NSS_Blast_Compiler_CompilationData">CompilationData</a>.)</td></tr><tr><td>![Public property](media/pubproperty.gif "Public property")</td><td><a href="P_NSS_Blast_Compiler_CompilationData_Offsets">Offsets</a></td><td>
list of used variable offsets
 (Inherited from <a href="T_NSS_Blast_Compiler_CompilationData">CompilationData</a>.)</td></tr><tr><td>![Public property](media/pubproperty.gif "Public property")</td><td><a href="P_NSS_Blast_Compiler_CompilationData_Outputs">Outputs</a></td><td>
list of outputs defined by this script
 (Inherited from <a href="T_NSS_Blast_Compiler_CompilationData">CompilationData</a>.)</td></tr><tr><td>![Public property](media/pubproperty.gif "Public property")</td><td><a href="P_NSS_Blast_Compiler_CompilationData_Script">Script</a></td><td>
the input script
 (Inherited from <a href="T_NSS_Blast_Compiler_CompilationData">CompilationData</a>.)</td></tr><tr><td>![Public property](media/pubproperty.gif "Public property")</td><td><a href="P_NSS_Blast_Compiler_CompilationData_Tokens">Tokens</a></td><td>
list of tokens as parsed out of the script
 (Inherited from <a href="T_NSS_Blast_Compiler_CompilationData">CompilationData</a>.)</td></tr><tr><td>![Public property](media/pubproperty.gif "Public property")</td><td><a href="P_NSS_Blast_Compiler_CompilationData_Validations">Validations</a></td><td>
list of validations defined by this script
 (Inherited from <a href="T_NSS_Blast_Compiler_CompilationData">CompilationData</a>.)</td></tr><tr><td>![Public property](media/pubproperty.gif "Public property")</td><td><a href="P_NSS_Blast_Compiler_CompilationData_VariableCount">VariableCount</a></td><td> (Inherited from <a href="T_NSS_Blast_Compiler_CompilationData">CompilationData</a>.)</td></tr><tr><td>![Public property](media/pubproperty.gif "Public property")</td><td><a href="P_NSS_Blast_Compiler_CompilationData_Variables">Variables</a></td><td>
List of variables in script
 (Inherited from <a href="T_NSS_Blast_Compiler_CompilationData">CompilationData</a>.)</td></tr><tr><td>![Public property](media/pubproperty.gif "Public property")</td><td><a href="P_NSS_Blast_Compiler_CompilationData_Version">Version</a></td><td>
Version
 (Inherited from <a href="T_NSS_Blast_Compiler_CompilationData">CompilationData</a>.)</td></tr><tr><td>![Public property](media/pubproperty.gif "Public property")</td><td><a href="P_NSS_Blast_Compiler_CompilationData_WarningCount">WarningCount</a></td><td>
number of warnings that occured during compilation
 (Inherited from <a href="T_NSS_Blast_Compiler_CompilationData">CompilationData</a>.)</td></tr></table>&nbsp;
<a href="#hpccompilationdata-class">Back to Top</a>

## Methods
&nbsp;<table><tr><th></th><th>Name</th><th>Description</th></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_NSS_Blast_Compiler_CompilationData_CreateVariable">CreateVariable</a></td><td>
create a holder for variable data collected during compilation - will log errors if the variable exists and returns null - initializes reference count at 1
 (Inherited from <a href="T_NSS_Blast_Compiler_CompilationData">CompilationData</a>.)</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="https://docs.microsoft.com/dotnet/api/system.object.equals#system-object-equals(system-object)" target="_blank" rel="noopener noreferrer">Equals</a></td><td>
Determines whether the specified object is equal to the current object.
 (Inherited from <a href="https://docs.microsoft.com/dotnet/api/system.object" target="_blank" rel="noopener noreferrer">Object</a>.)</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_NSS_Blast_Compiler_CompilationData_ExistsVariable">ExistsVariable</a></td><td> (Inherited from <a href="T_NSS_Blast_Compiler_CompilationData">CompilationData</a>.)</td></tr><tr><td>![Protected method](media/protmethod.gif "Protected method")</td><td><a href="https://docs.microsoft.com/dotnet/api/system.object.finalize#system-object-finalize" target="_blank" rel="noopener noreferrer">Finalize</a></td><td>
Allows an object to try to free resources and perform other cleanup operations before it is reclaimed by garbage collection.
 (Inherited from <a href="https://docs.microsoft.com/dotnet/api/system.object" target="_blank" rel="noopener noreferrer">Object</a>.)</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="https://docs.microsoft.com/dotnet/api/system.object.gethashcode#system-object-gethashcode" target="_blank" rel="noopener noreferrer">GetHashCode</a></td><td>
Serves as the default hash function.
 (Inherited from <a href="https://docs.microsoft.com/dotnet/api/system.object" target="_blank" rel="noopener noreferrer">Object</a>.)</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_NSS_Blast_Compiler_CompilationData_GetHumanReadableBytes">GetHumanReadableBytes</a></td><td>
get a readable string from the compiled code
 (Inherited from <a href="T_NSS_Blast_Compiler_CompilationData">CompilationData</a>.)</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_NSS_Blast_Compiler_CompilationData_GetHumanReadableCode">GetHumanReadableCode</a></td><td>
this version has a little more information than the generic bytecode reader in blast due to having access to all compilation data
 (Inherited from <a href="T_NSS_Blast_Compiler_CompilationData">CompilationData</a>.)</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_NSS_Blast_Compiler_CompilationData_GetOrCreateVariable">GetOrCreateVariable</a></td><td>
get or create a holder for variable data during compilation - maintains reference count
 (Inherited from <a href="T_NSS_Blast_Compiler_CompilationData">CompilationData</a>.)</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="https://docs.microsoft.com/dotnet/api/system.object.gettype#system-object-gettype" target="_blank" rel="noopener noreferrer">GetType</a></td><td>
Gets the <a href="https://docs.microsoft.com/dotnet/api/system.type" target="_blank" rel="noopener noreferrer">Type</a> of the current instance.
 (Inherited from <a href="https://docs.microsoft.com/dotnet/api/system.object" target="_blank" rel="noopener noreferrer">Object</a>.)</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_NSS_Blast_Compiler_CompilationData_GetVariable">GetVariable</a></td><td> (Inherited from <a href="T_NSS_Blast_Compiler_CompilationData">CompilationData</a>.)</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_NSS_Blast_Compiler_CompilationData_GetVariableFromOffset">GetVariableFromOffset</a></td><td> (Inherited from <a href="T_NSS_Blast_Compiler_CompilationData">CompilationData</a>.)</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_NSS_Blast_Compiler_CompilationData_HasInput">HasInput(Int32)</a></td><td> (Inherited from <a href="T_NSS_Blast_Compiler_CompilationData">CompilationData</a>.)</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_NSS_Blast_Compiler_CompilationData_HasInput_1">HasInput(String)</a></td><td> (Inherited from <a href="T_NSS_Blast_Compiler_CompilationData">CompilationData</a>.)</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_NSS_Blast_Compiler_CompilationData_LogError">LogError</a></td><td>
Log an error to the log, also writes to player log / debugstream
 (Inherited from <a href="T_NSS_Blast_Compiler_CompilationData">CompilationData</a>.)</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_NSS_Blast_Compiler_CompilationData_LogMessage">LogMessage</a></td><td>
on older .net versions we could get the stackframe see: https://stackoverflow.com/questions/12556767/how-do-i-get-the-current-line-number and: https://stackoverflow.com/questions/38476796/how-to-set-net-core-in-if-statement-for-compilation
 (Inherited from <a href="T_NSS_Blast_Compiler_CompilationData">CompilationData</a>.)</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_NSS_Blast_Compiler_CompilationData_LogToDo">LogToDo</a></td><td>
logs a todo, only in standalone debug builds
 (Inherited from <a href="T_NSS_Blast_Compiler_CompilationData">CompilationData</a>.)</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_NSS_Blast_Compiler_CompilationData_LogTrace">LogTrace</a></td><td>
Trace a message, does nothing in release builds
 (Inherited from <a href="T_NSS_Blast_Compiler_CompilationData">CompilationData</a>.)</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_NSS_Blast_Compiler_CompilationData_LogWarning">LogWarning</a></td><td>
Log a warning to the log, also writes to player log / debugstream
 (Inherited from <a href="T_NSS_Blast_Compiler_CompilationData">CompilationData</a>.)</td></tr><tr><td>![Protected method](media/protmethod.gif "Protected method")</td><td><a href="https://docs.microsoft.com/dotnet/api/system.object.memberwiseclone#system-object-memberwiseclone" target="_blank" rel="noopener noreferrer">MemberwiseClone</a></td><td>
Creates a shallow copy of the current <a href="https://docs.microsoft.com/dotnet/api/system.object" target="_blank" rel="noopener noreferrer">Object</a>.
 (Inherited from <a href="https://docs.microsoft.com/dotnet/api/system.object" target="_blank" rel="noopener noreferrer">Object</a>.)</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="https://docs.microsoft.com/dotnet/api/system.object.tostring#system-object-tostring" target="_blank" rel="noopener noreferrer">ToString</a></td><td>
Returns a string that represents the current object.
 (Inherited from <a href="https://docs.microsoft.com/dotnet/api/system.object" target="_blank" rel="noopener noreferrer">Object</a>.)</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_NSS_Blast_Compiler_CompilationData_TryGetDefine">TryGetDefine</a></td><td>
try to lookup define by identifier name
 (Inherited from <a href="T_NSS_Blast_Compiler_CompilationData">CompilationData</a>.)</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_NSS_Blast_Compiler_CompilationData_TryGetInput">TryGetInput</a></td><td>
Try to lookup a input variable mapping
 (Inherited from <a href="T_NSS_Blast_Compiler_CompilationData">CompilationData</a>.)</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_NSS_Blast_Compiler_CompilationData_TryGetOutput">TryGetOutput</a></td><td>
try to lookup an output variable mapping
 (Inherited from <a href="T_NSS_Blast_Compiler_CompilationData">CompilationData</a>.)</td></tr></table>&nbsp;
<a href="#hpccompilationdata-class">Back to Top</a>

## Fields
&nbsp;<table><tr><th></th><th>Name</th><th>Description</th></tr><tr><td>![Public field](media/pubfield.gif "Public field")</td><td><a href="F_NSS_Blast_Compiler_CompilationData_code">code</a></td><td>
intermediate bytecode, only public for debugging view purposes, dont use, dont modify
 (Inherited from <a href="T_NSS_Blast_Compiler_CompilationData">CompilationData</a>.)</td></tr><tr><td>![Public field](media/pubfield.gif "Public field")</td><td><a href="F_NSS_Blast_Compiler_CompilationData_CompilerMessages">CompilerMessages</a></td><td>
List of messages issued during compilation
 (Inherited from <a href="T_NSS_Blast_Compiler_CompilationData">CompilationData</a>.)</td></tr><tr><td>![Public field](media/pubfield.gif "Public field")</td><td><a href="F_NSS_Blast_Compiler_CompilationData_Executable">Executable</a></td><td>
the intermediate
 (Inherited from <a href="T_NSS_Blast_Compiler_CompilationData">CompilationData</a>.)</td></tr><tr><td>![Public field](media/pubfield.gif "Public field")</td><td><a href="F_NSS_Blast_Compiler_HPCCompilationData_HPCCode">HPCCode</a></td><td>
resulting burstable C# code</td></tr><tr><td>![Protected field](media/protfield.gif "Protected field")</td><td><a href="F_NSS_Blast_Compiler_CompilationData_version">version</a></td><td>
Version
 (Inherited from <a href="T_NSS_Blast_Compiler_CompilationData">CompilationData</a>.)</td></tr></table>&nbsp;
<a href="#hpccompilationdata-class">Back to Top</a>

## See Also


#### Reference
<a href="N_NSS_Blast_Compiler">NSS.Blast.Compiler Namespace</a><br />