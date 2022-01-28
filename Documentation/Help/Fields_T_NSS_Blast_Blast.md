# Blast Fields
 

The <a href="T_NSS_Blast_Blast">Blast</a> type exposes the following members.


## Fields
&nbsp;<table><tr><th></th><th>Name</th><th>Description</th></tr><tr><td>![Public field](media/pubfield.gif "Public field")</td><td><a href="F_NSS_Blast_Blast_API">API</a></td><td>
the current set of script accessible functions, check OwnScriptAPIMemory to see if this instance of blast is the owner of the memory used in the API</td></tr><tr><td>![Public field](media/pubfield.gif "Public field")![Static member](media/static.gif "Static member")</td><td><a href="F_NSS_Blast_Blast_Comment">Comment</a></td><td>
the comment character</td></tr><tr><td>![Public field](media/pubfield.gif "Public field")![Static member](media/static.gif "Static member")</td><td><a href="F_NSS_Blast_Blast_InvalidNumeric">InvalidNumeric</a></td><td>
the value used for invalid numerics</td></tr><tr><td>![Public field](media/pubfield.gif "Public field")![Static member](media/static.gif "Static member")</td><td><a href="F_NSS_Blast_Blast_MaximumFunctionNameLength">MaximumFunctionNameLength</a></td><td>
the maximum length of a function name in number of ASCII characters</td></tr><tr><td>![Public field](media/pubfield.gif "Public field")![Static member](media/static.gif "Static member")</td><td><a href="F_NSS_Blast_Blast_StackFillPattern">StackFillPattern</a></td><td>
The fill pattern for stack on initialize, easier to spot bugs if stack is filled with a pattern instead of zeros or random which might have different causes, something bugged setting all bytes to 101 should be very suspicious</td></tr><tr><td>![Public field](media/pubfield.gif "Public field")![Static member](media/static.gif "Static member")</td><td><a href="F_NSS_Blast_Blast_Tokens">Tokens</a></td><td>
defines tokens that can be used in script</td></tr><tr><td>![Public field](media/pubfield.gif "Public field")![Static member](media/static.gif "Static member")</td><td><a href="F_NSS_Blast_Blast_ValueOperations">ValueOperations</a></td><td>
list all value operations, these operations directly encode constant values -> burst should be able to access this if needed..</td></tr></table>&nbsp;
<a href="#blast-fields">Back to Top</a>

## See Also


#### Reference
<a href="T_NSS_Blast_Blast">Blast Structure</a><br /><a href="N_NSS_Blast">NSS.Blast Namespace</a><br />