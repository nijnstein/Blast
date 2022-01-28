# CSCompilationData Methods
 

The <a href="T_NSS_Blast_Compiler_CSCompilationData">CSCompilationData</a> type exposes the following members.


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
<a href="#cscompilationdata-methods">Back to Top</a>

## See Also


#### Reference
<a href="T_NSS_Blast_Compiler_CSCompilationData">CSCompilationData Class</a><br /><a href="N_NSS_Blast_Compiler">NSS.Blast.Compiler Namespace</a><br />