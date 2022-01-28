# BlastEngineData Fields
 

The <a href="T_NSS_Blast_BlastEngineData">BlastEngineData</a> type exposes the following members.


## Fields
&nbsp;<table><tr><th></th><th>Name</th><th>Description</th></tr><tr><td>![Public field](media/pubfield.gif "Public field")</td><td><a href="F_NSS_Blast_BlastEngineData_constants">constants</a></td><td>
constant values selected by opcodes and shared among threads - these are set by the constant value operations and they can be replaced with other values... if you are mad</td></tr><tr><td>![Public field](media/pubfield.gif "Public field")</td><td><a href="F_NSS_Blast_BlastEngineData_FunctionCount">FunctionCount</a></td><td>
number of function pointers in the fp array. function id's dont match indices..... - TODO this means each function call needs a lookup...</td></tr><tr><td>![Public field](media/pubfield.gif "Public field")</td><td><a href="F_NSS_Blast_BlastEngineData_FunctionPointers">FunctionPointers</a></td><td>
function pointer data</td></tr><tr><td>![Public field](media/pubfield.gif "Public field")</td><td><a href="F_NSS_Blast_BlastEngineData_Functions">Functions</a></td><td>
external function info array. these provide api access to the script</td></tr><tr><td>![Public field](media/pubfield.gif "Public field")</td><td><a href="F_NSS_Blast_BlastEngineData_random">random</a></td><td>
base random number generator, all random actions have their origin in this random number generator</td></tr></table>&nbsp;
<a href="#blastenginedata-fields">Back to Top</a>

## See Also


#### Reference
<a href="T_NSS_Blast_BlastEngineData">BlastEngineData Structure</a><br /><a href="N_NSS_Blast">NSS.Blast Namespace</a><br />