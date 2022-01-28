# BlastEngineData Methods
 

The <a href="T_NSS_Blast_BlastEngineData">BlastEngineData</a> type exposes the following members.


## Methods
&nbsp;<table><tr><th></th><th>Name</th><th>Description</th></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_NSS_Blast_BlastEngineData_CanBeAValidFunctionId">CanBeAValidFunctionId</a></td><td>
return if the id is in the correct range and its pointer is not null</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="https://docs.microsoft.com/dotnet/api/system.valuetype.equals#system-valuetype-equals(system-object)" target="_blank" rel="noopener noreferrer">Equals</a></td><td>
Indicates whether this instance and a specified object are equal.
 (Inherited from <a href="https://docs.microsoft.com/dotnet/api/system.valuetype" target="_blank" rel="noopener noreferrer">ValueType</a>.)</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_NSS_Blast_BlastEngineData_GetFunction">GetFunction(blast_operation)</a></td><td>
lookup function attached to operation, this assumes the function exists - TODO -> we could update this to a tableindexer instead of a scan by building a lookuptable[operation] == function</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_NSS_Blast_BlastEngineData_GetFunction_1">GetFunction(String)</a></td><td>
get the function matching to the name</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="https://docs.microsoft.com/dotnet/api/system.valuetype.gethashcode#system-valuetype-gethashcode" target="_blank" rel="noopener noreferrer">GetHashCode</a></td><td>
Returns the hash code for this instance.
 (Inherited from <a href="https://docs.microsoft.com/dotnet/api/system.valuetype" target="_blank" rel="noopener noreferrer">ValueType</a>.)</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="https://docs.microsoft.com/dotnet/api/system.object.gettype#system-object-gettype" target="_blank" rel="noopener noreferrer">GetType</a></td><td>
Gets the <a href="https://docs.microsoft.com/dotnet/api/system.type" target="_blank" rel="noopener noreferrer">Type</a> of the current instance.
 (Inherited from <a href="https://docs.microsoft.com/dotnet/api/system.object" target="_blank" rel="noopener noreferrer">Object</a>.)</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_NSS_Blast_BlastEngineData_IsVariableParamFunction">IsVariableParamFunction</a></td><td>
lookup a function mapping to the given operation, performs a linear scan through the function table</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_NSS_Blast_BlastEngineData_Seed">Seed</a></td><td>
seed the base random number generator, altering the origen for all random actions in blast</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="https://docs.microsoft.com/dotnet/api/system.valuetype.tostring#system-valuetype-tostring" target="_blank" rel="noopener noreferrer">ToString</a></td><td>
Returns the fully qualified type name of this instance.
 (Inherited from <a href="https://docs.microsoft.com/dotnet/api/system.valuetype" target="_blank" rel="noopener noreferrer">ValueType</a>.)</td></tr></table>&nbsp;
<a href="#blastenginedata-methods">Back to Top</a>

## See Also


#### Reference
<a href="T_NSS_Blast_BlastEngineData">BlastEngineData Structure</a><br /><a href="N_NSS_Blast">NSS.Blast Namespace</a><br />