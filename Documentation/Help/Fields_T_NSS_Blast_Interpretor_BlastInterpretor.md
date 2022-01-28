# BlastInterpretor Fields
 

The <a href="T_NSS_Blast_Interpretor_BlastInterpretor">BlastInterpretor</a> type exposes the following members.


## Fields
&nbsp;<table><tr><th></th><th>Name</th><th>Description</th></tr><tr><td>![Public field](media/pubfield.gif "Public field")![Static member](media/static.gif "Static member")</td><td><a href="F_NSS_Blast_Interpretor_BlastInterpretor_max_iterations">max_iterations</a></td><td>
maxiumum iteration count, usefull to avoid endless loop bugs</td></tr><tr><td>![Public field](media/pubfield.gif "Public field")![Static member](media/static.gif "Static member")</td><td><a href="F_NSS_Blast_Interpretor_BlastInterpretor_opt_id">opt_id</a></td><td>
>= opt_id is starting opcode for parameters, everything until 255|ExOp is considered a datasegment index</td></tr><tr><td>![Public field](media/pubfield.gif "Public field")![Static member](media/static.gif "Static member")</td><td><a href="F_NSS_Blast_Interpretor_BlastInterpretor_opt_value">opt_value</a></td><td>
>= if the opcode between opt_value and (including) opt_id then its an opcode for a constant</td></tr><tr><td>![Public field](media/pubfield.gif "Public field")</td><td><a href="F_NSS_Blast_Interpretor_BlastInterpretor_ValidationMode">ValidationMode</a></td><td>
stack segment pointer</td></tr></table>&nbsp;
<a href="#blastinterpretor-fields">Back to Top</a>

## See Also


#### Reference
<a href="T_NSS_Blast_Interpretor_BlastInterpretor">BlastInterpretor Structure</a><br /><a href="N_NSS_Blast_Interpretor">NSS.Blast.Interpretor Namespace</a><br />