# IBlastCompilationData Interface
 

interface into compilationdata, we should restrict outside use a bit via this

**Namespace:**&nbsp;<a href="N_NSS_Blast_Compiler">NSS.Blast.Compiler</a><br />**Assembly:**&nbsp;BLAST (in BLAST.dll) Version: 1.0.0

## Syntax

**C#**<br />
``` C#
public interface IBlastCompilationData
```

The IBlastCompilationData type exposes the following members.


## Properties
&nbsp;<table><tr><th></th><th>Name</th><th>Description</th></tr><tr><td>![Public property](media/pubproperty.gif "Public property")</td><td><a href="P_NSS_Blast_Compiler_IBlastCompilationData_AST">AST</a></td><td>
Compiler node tree</td></tr><tr><td>![Public property](media/pubproperty.gif "Public property")</td><td><a href="P_NSS_Blast_Compiler_IBlastCompilationData_Blast">Blast</a></td><td>
blast engine data used in this compilation</td></tr><tr><td>![Public property](media/pubproperty.gif "Public property")</td><td><a href="P_NSS_Blast_Compiler_IBlastCompilationData_CanValidate">CanValidate</a></td><td>
true if the script can be validated (it contains validation defines)</td></tr><tr><td>![Public property](media/pubproperty.gif "Public property")</td><td><a href="P_NSS_Blast_Compiler_IBlastCompilationData_CompilerOptions">CompilerOptions</a></td><td>
Compiler Options used during compilation</td></tr><tr><td>![Public property](media/pubproperty.gif "Public property")</td><td><a href="P_NSS_Blast_Compiler_IBlastCompilationData_Defines">Defines</a></td><td>
Compiler defines used during compilation, this contains only the unique defines set by this script and more defines might apply depending on the setup</td></tr><tr><td>![Public property](media/pubproperty.gif "Public property")</td><td><a href="P_NSS_Blast_Compiler_IBlastCompilationData_HasDefines">HasDefines</a></td><td>
true if the script defines compiler defines</td></tr><tr><td>![Public property](media/pubproperty.gif "Public property")</td><td><a href="P_NSS_Blast_Compiler_IBlastCompilationData_HasErrors">HasErrors</a></td><td>
true if an errorcode is set or errors are present in the compilation log</td></tr><tr><td>![Public property](media/pubproperty.gif "Public property")</td><td><a href="P_NSS_Blast_Compiler_IBlastCompilationData_HasOffsets">HasOffsets</a></td><td>
true if the script defines variables</td></tr><tr><td>![Public property](media/pubproperty.gif "Public property")</td><td><a href="P_NSS_Blast_Compiler_IBlastCompilationData_HasVariables">HasVariables</a></td><td>
true if the script defines variables</td></tr><tr><td>![Public property](media/pubproperty.gif "Public property")</td><td><a href="P_NSS_Blast_Compiler_IBlastCompilationData_Inputs">Inputs</a></td><td>
Defined Inputs</td></tr><tr><td>![Public property](media/pubproperty.gif "Public property")</td><td><a href="P_NSS_Blast_Compiler_IBlastCompilationData_IsOK">IsOK</a></td><td>
true if no errors are set in the compilation log and no errorcode is set</td></tr><tr><td>![Public property](media/pubproperty.gif "Public property")</td><td><a href="P_NSS_Blast_Compiler_IBlastCompilationData_Jumps">Jumps</a></td><td>
Jumps found in the script</td></tr><tr><td>![Public property](media/pubproperty.gif "Public property")</td><td><a href="P_NSS_Blast_Compiler_IBlastCompilationData_OffsetCount">OffsetCount</a></td><td>
number of variable offsets defined in the script</td></tr><tr><td>![Public property](media/pubproperty.gif "Public property")</td><td><a href="P_NSS_Blast_Compiler_IBlastCompilationData_Offsets">Offsets</a></td><td>
Offsets into the datasegment for variable indices</td></tr><tr><td>![Public property](media/pubproperty.gif "Public property")</td><td><a href="P_NSS_Blast_Compiler_IBlastCompilationData_Outputs">Outputs</a></td><td>
Defined Outputs</td></tr><tr><td>![Public property](media/pubproperty.gif "Public property")</td><td><a href="P_NSS_Blast_Compiler_IBlastCompilationData_Script">Script</a></td><td>
the script</td></tr><tr><td>![Public property](media/pubproperty.gif "Public property")</td><td><a href="P_NSS_Blast_Compiler_IBlastCompilationData_Tokens">Tokens</a></td><td>
List of tokens as parsed from the input</td></tr><tr><td>![Public property](media/pubproperty.gif "Public property")</td><td><a href="P_NSS_Blast_Compiler_IBlastCompilationData_Validations">Validations</a></td><td>
Validations defined in script</td></tr><tr><td>![Public property](media/pubproperty.gif "Public property")</td><td><a href="P_NSS_Blast_Compiler_IBlastCompilationData_VariableCount">VariableCount</a></td><td>
number of variables defined in the script</td></tr><tr><td>![Public property](media/pubproperty.gif "Public property")</td><td><a href="P_NSS_Blast_Compiler_IBlastCompilationData_Variables">Variables</a></td><td>
List of variables as found in input</td></tr><tr><td>![Public property](media/pubproperty.gif "Public property")</td><td><a href="P_NSS_Blast_Compiler_IBlastCompilationData_Version">Version</a></td><td>
compiler version</td></tr></table>&nbsp;
<a href="#iblastcompilationdata-interface">Back to Top</a>

## Methods
&nbsp;<table><tr><th></th><th>Name</th><th>Description</th></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_NSS_Blast_Compiler_IBlastCompilationData_ExistsVariable">ExistsVariable</a></td><td>
returns true if a variable exists with the given name</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_NSS_Blast_Compiler_IBlastCompilationData_GetVariable">GetVariable</a></td><td>
lookup a variablemapping defined by the script by its name</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_NSS_Blast_Compiler_IBlastCompilationData_GetVariableFromOffset">GetVariableFromOffset</a></td><td>
lookup a variable defined by script based on its offset</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_NSS_Blast_Compiler_IBlastCompilationData_HasInput">HasInput(Int32)</a></td><td>
check if there is an input defined by the script with the given id</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_NSS_Blast_Compiler_IBlastCompilationData_HasInput_1">HasInput(String)</a></td><td>
check if there is an input defined by the script with the given id</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_NSS_Blast_Compiler_IBlastCompilationData_LogError">LogError</a></td><td>
log an error to the compiler log</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_NSS_Blast_Compiler_IBlastCompilationData_LogToDo">LogToDo</a></td><td>
log a message to the compiler log</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_NSS_Blast_Compiler_IBlastCompilationData_LogTrace">LogTrace</a></td><td>
trace information, does nothing in release</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_NSS_Blast_Compiler_IBlastCompilationData_LogWarning">LogWarning</a></td><td>
log a warning to the compiler log</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_NSS_Blast_Compiler_IBlastCompilationData_TryGetDefine">TryGetDefine</a></td><td>
attempt to get a defined value from script defined compilerdefines</td></tr></table>&nbsp;
<a href="#iblastcompilationdata-interface">Back to Top</a>

## See Also


#### Reference
<a href="N_NSS_Blast_Compiler">NSS.Blast.Compiler Namespace</a><br />