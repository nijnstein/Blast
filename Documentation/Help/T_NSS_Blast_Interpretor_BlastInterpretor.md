# BlastInterpretor Structure
 

V2 - use state machine instead of get_compound_result and internal grow-vector / operation sequence loop, should reduce load a lot on handling vectors state ---- read root statement ----\ \------read compound ----------/ \-----grow value-------------/ \-----get result ----------/

**Namespace:**&nbsp;<a href="N_NSS_Blast_Interpretor">NSS.Blast.Interpretor</a><br />**Assembly:**&nbsp;BLAST (in BLAST.dll) Version: 1.0.0

## Syntax

**C#**<br />
``` C#
public struct BlastInterpretor
```

The BlastInterpretor type exposes the following members.


## Methods
&nbsp;<table><tr><th></th><th>Name</th><th>Description</th></tr><tr><td>![Public method](media/pubmethod.gif "Public method")![Static member](media/static.gif "Static member")</td><td><a href="M_NSS_Blast_Interpretor_BlastInterpretor_decode62">decode62</a></td><td>
decode info byte of data, 62 uses the upper 6 bits from the parametercount and the lower 2 for vectorsize, resulting in 64 parameters and size 4 vectors.. - update to decode44? 16 params max and 16 vec size?</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="https://docs.microsoft.com/dotnet/api/system.valuetype.equals#system-valuetype-equals(system-object)" target="_blank" rel="noopener noreferrer">Equals</a></td><td>
Indicates whether this instance and a specified object are equal.
 (Inherited from <a href="https://docs.microsoft.com/dotnet/api/system.valuetype" target="_blank" rel="noopener noreferrer">ValueType</a>.)</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_NSS_Blast_Interpretor_BlastInterpretor_Execute">Execute(BlastEngineDataPtr)</a></td><td>
execute bytecode set to the interpretor</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_NSS_Blast_Interpretor_BlastInterpretor_Execute_1">Execute(BlastEngineDataPtr, IntPtr, IntPtr)</a></td><td>
execute bytecode</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="https://docs.microsoft.com/dotnet/api/system.valuetype.gethashcode#system-valuetype-gethashcode" target="_blank" rel="noopener noreferrer">GetHashCode</a></td><td>
Returns the hash code for this instance.
 (Inherited from <a href="https://docs.microsoft.com/dotnet/api/system.valuetype" target="_blank" rel="noopener noreferrer">ValueType</a>.)</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")![Static member](media/static.gif "Static member")</td><td><a href="M_NSS_Blast_Interpretor_BlastInterpretor_GetMetaData">GetMetaData</a></td><td /></tr><tr><td>![Public method](media/pubmethod.gif "Public method")![Static member](media/static.gif "Static member")</td><td><a href="M_NSS_Blast_Interpretor_BlastInterpretor_GetMetaDataSize">GetMetaDataSize</a></td><td /></tr><tr><td>![Public method](media/pubmethod.gif "Public method")![Static member](media/static.gif "Static member")</td><td><a href="M_NSS_Blast_Interpretor_BlastInterpretor_GetMetaDataType">GetMetaDataType</a></td><td /></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="https://docs.microsoft.com/dotnet/api/system.object.gettype#system-object-gettype" target="_blank" rel="noopener noreferrer">GetType</a></td><td>
Gets the <a href="https://docs.microsoft.com/dotnet/api/system.type" target="_blank" rel="noopener noreferrer">Type</a> of the current instance.
 (Inherited from <a href="https://docs.microsoft.com/dotnet/api/system.object" target="_blank" rel="noopener noreferrer">Object</a>.)</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")![Static member](media/static.gif "Static member")</td><td><a href="M_NSS_Blast_Interpretor_BlastInterpretor_IsBooleanOperation">IsBooleanOperation</a></td><td>
WARNING checks if boolean operaion, uses value op enum!!</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")![Static member](media/static.gif "Static member")</td><td><a href="M_NSS_Blast_Interpretor_BlastInterpretor_IsMathematicalOperation">IsMathematicalOperation</a></td><td>
WARNING checks if +-*/ uses value op enum!!</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")![Static member](media/static.gif "Static member")</td><td><a href="M_NSS_Blast_Interpretor_BlastInterpretor_IsMathematicalOrBooleanOperation">IsMathematicalOrBooleanOperation</a></td><td>
WARNING checks if is operation, uses value op enum blast_operation.add until blast_operation.not_equals</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_NSS_Blast_Interpretor_BlastInterpretor_peek">peek</a></td><td /></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_NSS_Blast_Interpretor_BlastInterpretor_pop_2">pop(float4)</a></td><td /></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_NSS_Blast_Interpretor_BlastInterpretor_pop">pop(Int32)</a></td><td /></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_NSS_Blast_Interpretor_BlastInterpretor_pop_3">pop(int4)</a></td><td /></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_NSS_Blast_Interpretor_BlastInterpretor_pop_1">pop(Single)</a></td><td /></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_NSS_Blast_Interpretor_BlastInterpretor_push_2">push(float2)</a></td><td /></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_NSS_Blast_Interpretor_BlastInterpretor_push_3">push(float3)</a></td><td /></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_NSS_Blast_Interpretor_BlastInterpretor_push_4">push(float4)</a></td><td /></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_NSS_Blast_Interpretor_BlastInterpretor_push">push(Int32)</a></td><td /></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_NSS_Blast_Interpretor_BlastInterpretor_push_1">push(Single)</a></td><td /></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_NSS_Blast_Interpretor_BlastInterpretor_Reset">Reset</a></td><td>
reset code_pointer and stack_offset to their initial states</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_NSS_Blast_Interpretor_BlastInterpretor_ResumeYield">ResumeYield(BlastEngineDataPtr)</a></td><td>
resume executing bytecode after yielding</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_NSS_Blast_Interpretor_BlastInterpretor_ResumeYield_1">ResumeYield(BlastEngineDataPtr, IntPtr, IntPtr)</a></td><td>
resume executing bytecode after yielding</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_NSS_Blast_Interpretor_BlastInterpretor_SetPackage">SetPackage(BlastPackageData)</a></td><td>
we might need to know if we need to copy back data (or we use a dummy)</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_NSS_Blast_Interpretor_BlastInterpretor_SetPackage_1">SetPackage(BlastPackageData, Byte*, Single*, Byte*, Int32)</a></td><td>
set package data from package and seperate buffers</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="https://docs.microsoft.com/dotnet/api/system.valuetype.tostring#system-valuetype-tostring" target="_blank" rel="noopener noreferrer">ToString</a></td><td>
Returns the fully qualified type name of this instance.
 (Inherited from <a href="https://docs.microsoft.com/dotnet/api/system.valuetype" target="_blank" rel="noopener noreferrer">ValueType</a>.)</td></tr></table>&nbsp;
<a href="#blastinterpretor-structure">Back to Top</a>

## Fields
&nbsp;<table><tr><th></th><th>Name</th><th>Description</th></tr><tr><td>![Public field](media/pubfield.gif "Public field")![Static member](media/static.gif "Static member")</td><td><a href="F_NSS_Blast_Interpretor_BlastInterpretor_max_iterations">max_iterations</a></td><td>
maxiumum iteration count, usefull to avoid endless loop bugs</td></tr><tr><td>![Public field](media/pubfield.gif "Public field")![Static member](media/static.gif "Static member")</td><td><a href="F_NSS_Blast_Interpretor_BlastInterpretor_opt_id">opt_id</a></td><td>
>= opt_id is starting opcode for parameters, everything until 255|ExOp is considered a datasegment index</td></tr><tr><td>![Public field](media/pubfield.gif "Public field")![Static member](media/static.gif "Static member")</td><td><a href="F_NSS_Blast_Interpretor_BlastInterpretor_opt_value">opt_value</a></td><td>
>= if the opcode between opt_value and (including) opt_id then its an opcode for a constant</td></tr><tr><td>![Public field](media/pubfield.gif "Public field")</td><td><a href="F_NSS_Blast_Interpretor_BlastInterpretor_ValidationMode">ValidationMode</a></td><td>
stack segment pointer</td></tr></table>&nbsp;
<a href="#blastinterpretor-structure">Back to Top</a>

## See Also


#### Reference
<a href="N_NSS_Blast_Interpretor">NSS.Blast.Interpretor Namespace</a><br />