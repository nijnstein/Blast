# BlastScriptFunction Fields
 

The <a href="T_NSS_Blast_BlastScriptFunction">BlastScriptFunction</a> type exposes the following members.


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
<a href="#blastscriptfunction-fields">Back to Top</a>

## See Also


#### Reference
<a href="T_NSS_Blast_BlastScriptFunction">BlastScriptFunction Structure</a><br /><a href="N_NSS_Blast">NSS.Blast Namespace</a><br />