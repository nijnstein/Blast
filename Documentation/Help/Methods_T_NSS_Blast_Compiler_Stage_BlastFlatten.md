# BlastFlatten Methods
 

The <a href="T_NSS_Blast_Compiler_Stage_BlastFlatten">BlastFlatten</a> type exposes the following members.


## Methods
&nbsp;<table><tr><th></th><th>Name</th><th>Description</th></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="https://docs.microsoft.com/dotnet/api/system.object.equals#system-object-equals(system-object)" target="_blank" rel="noopener noreferrer">Equals</a></td><td>
Determines whether the specified object is equal to the current object.
 (Inherited from <a href="https://docs.microsoft.com/dotnet/api/system.object" target="_blank" rel="noopener noreferrer">Object</a>.)</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_NSS_Blast_Compiler_Stage_BlastFlatten_Execute">Execute</a></td><td /></tr><tr><td>![Protected method](media/protmethod.gif "Protected method")</td><td><a href="https://docs.microsoft.com/dotnet/api/system.object.finalize#system-object-finalize" target="_blank" rel="noopener noreferrer">Finalize</a></td><td>
Allows an object to try to free resources and perform other cleanup operations before it is reclaimed by garbage collection.
 (Inherited from <a href="https://docs.microsoft.com/dotnet/api/system.object" target="_blank" rel="noopener noreferrer">Object</a>.)</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_NSS_Blast_Compiler_Stage_BlastFlatten_Flatten">Flatten</a></td><td /></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_NSS_Blast_Compiler_Stage_BlastFlatten_FlattenAssignment">FlattenAssignment</a></td><td>
flatten assignement node</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_NSS_Blast_Compiler_Stage_BlastFlatten_FlattenCompound">FlattenCompound</a></td><td>
flatten a compound</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_NSS_Blast_Compiler_Stage_BlastFlatten_FlattenFunction">FlattenFunction</a></td><td>
flatten a function into a list of flat operations, its optional to push the resulting function to the end of the list and return the pushing node reference</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_NSS_Blast_Compiler_Stage_BlastFlatten_FlattenFunctionParameters">FlattenFunctionParameters</a></td><td>
flatten a list of function parameters</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_NSS_Blast_Compiler_Stage_BlastFlatten_FlattenWhileLoop">FlattenWhileLoop</a></td><td>
flatten a while loop (which might originally have been a for loop dueue to transform stage) - pops away operations from condition - generates flat list of statements for loop compound - maintains same root object --- THE WHILELOOP SHOULD BE EXCLUDED FROM NESTING TESTS --- while this nests in the node tree it nests control flow and that will flatten out when compiling into jumps</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="https://docs.microsoft.com/dotnet/api/system.object.gethashcode#system-object-gethashcode" target="_blank" rel="noopener noreferrer">GetHashCode</a></td><td>
Serves as the default hash function.
 (Inherited from <a href="https://docs.microsoft.com/dotnet/api/system.object" target="_blank" rel="noopener noreferrer">Object</a>.)</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="https://docs.microsoft.com/dotnet/api/system.object.gettype#system-object-gettype" target="_blank" rel="noopener noreferrer">GetType</a></td><td>
Gets the <a href="https://docs.microsoft.com/dotnet/api/system.type" target="_blank" rel="noopener noreferrer">Type</a> of the current instance.
 (Inherited from <a href="https://docs.microsoft.com/dotnet/api/system.object" target="_blank" rel="noopener noreferrer">Object</a>.)</td></tr><tr><td>![Protected method](media/protmethod.gif "Protected method")</td><td><a href="https://docs.microsoft.com/dotnet/api/system.object.memberwiseclone#system-object-memberwiseclone" target="_blank" rel="noopener noreferrer">MemberwiseClone</a></td><td>
Creates a shallow copy of the current <a href="https://docs.microsoft.com/dotnet/api/system.object" target="_blank" rel="noopener noreferrer">Object</a>.
 (Inherited from <a href="https://docs.microsoft.com/dotnet/api/system.object" target="_blank" rel="noopener noreferrer">Object</a>.)</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="https://docs.microsoft.com/dotnet/api/system.object.tostring#system-object-tostring" target="_blank" rel="noopener noreferrer">ToString</a></td><td>
Returns a string that represents the current object.
 (Inherited from <a href="https://docs.microsoft.com/dotnet/api/system.object" target="_blank" rel="noopener noreferrer">Object</a>.)</td></tr></table>&nbsp;
<a href="#blastflatten-methods">Back to Top</a>

## See Also


#### Reference
<a href="T_NSS_Blast_Compiler_Stage_BlastFlatten">BlastFlatten Class</a><br /><a href="N_NSS_Blast_Compiler_Stage">NSS.Blast.Compiler.Stage Namespace</a><br />