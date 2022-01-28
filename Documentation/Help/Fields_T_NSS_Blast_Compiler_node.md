# node Fields
 

The <a href="T_NSS_Blast_Compiler_node">node</a> type exposes the following members.


## Fields
&nbsp;<table><tr><th></th><th>Name</th><th>Description</th></tr><tr><td>![Public field](media/pubfield.gif "Public field")</td><td><a href="F_NSS_Blast_Compiler_node_children">children</a></td><td>
children of node</td></tr><tr><td>![Public field](media/pubfield.gif "Public field")</td><td><a href="F_NSS_Blast_Compiler_node_constant_op">constant_op</a></td><td>
any operation connected to this node (type == function | operation | parameter (pop))</td></tr><tr><td>![Public field](media/pubfield.gif "Public field")</td><td><a href="F_NSS_Blast_Compiler_node_depends_on">depends_on</a></td><td>
dependencies of node</td></tr><tr><td>![Public field](media/pubfield.gif "Public field")</td><td><a href="F_NSS_Blast_Compiler_node_function">function</a></td><td>
if the node is a function then its this one</td></tr><tr><td>![Public field](media/pubfield.gif "Public field")</td><td><a href="F_NSS_Blast_Compiler_node_identifier">identifier</a></td><td>
any identifier attached to this node</td></tr><tr><td>![Public field](media/pubfield.gif "Public field")</td><td><a href="F_NSS_Blast_Compiler_node_indexers">indexers</a></td><td>
BS2: indexers, arrays and compound types [].</td></tr><tr><td>![Public field](media/pubfield.gif "Public field")</td><td><a href="F_NSS_Blast_Compiler_node_is_constant">is_constant</a></td><td>
true if the node represents a constant value</td></tr><tr><td>![Public field](media/pubfield.gif "Public field")</td><td><a href="F_NSS_Blast_Compiler_node_linked_pop">linked_pop</a></td><td>
internal use: a linked pop operation, used during compilation to keep track of pushpop pairs</td></tr><tr><td>![Public field](media/pubfield.gif "Public field")</td><td><a href="F_NSS_Blast_Compiler_node_linked_push">linked_push</a></td><td>
internal use: a linked push operation, used during compilation to keep track of pushpop pairs</td></tr><tr><td>![Public field](media/pubfield.gif "Public field")</td><td><a href="F_NSS_Blast_Compiler_node_parent">parent</a></td><td>
the parent node, null if the node is the root</td></tr><tr><td>![Public field](media/pubfield.gif "Public field")</td><td><a href="F_NSS_Blast_Compiler_node_skip_compilation">skip_compilation</a></td><td>
skip general compilation of this node, some nodes control when to compile their own dependencies like initializers in loops</td></tr><tr><td>![Public field](media/pubfield.gif "Public field")</td><td><a href="F_NSS_Blast_Compiler_node_token">token</a></td><td>
any token attached to this node</td></tr><tr><td>![Public field](media/pubfield.gif "Public field")</td><td><a href="F_NSS_Blast_Compiler_node_type">type</a></td><td>
nodetype of node</td></tr><tr><td>![Public field](media/pubfield.gif "Public field")</td><td><a href="F_NSS_Blast_Compiler_node_variable">variable</a></td><td>
variable data inferred from the ast and connected to the nodes identifier</td></tr><tr><td>![Public field](media/pubfield.gif "Public field")</td><td><a href="F_NSS_Blast_Compiler_node_vector_size">vector_size</a></td><td>
the vector size</td></tr></table>&nbsp;
<a href="#node-fields">Back to Top</a>

## See Also


#### Reference
<a href="T_NSS_Blast_Compiler_node">node Class</a><br /><a href="N_NSS_Blast_Compiler">NSS.Blast.Compiler Namespace</a><br />