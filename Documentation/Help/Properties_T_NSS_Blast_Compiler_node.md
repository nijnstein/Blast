# node Properties
 

The <a href="T_NSS_Blast_Compiler_node">node</a> type exposes the following members.


## Properties
&nbsp;<table><tr><th></th><th>Name</th><th>Description</th></tr><tr><td>![Public property](media/pubproperty.gif "Public property")</td><td><a href="P_NSS_Blast_Compiler_node_AsFloat">AsFloat</a></td><td>
interpret the node's attached identifier as a floating point value, returns Blast.InvalidNumeric if it could not parse the identifier as a numeric</td></tr><tr><td>![Public property](media/pubproperty.gif "Public property")</td><td><a href="P_NSS_Blast_Compiler_node_ChildCount">ChildCount</a></td><td>
number of child nodes below this node</td></tr><tr><td>![Public property](media/pubproperty.gif "Public property")</td><td><a href="P_NSS_Blast_Compiler_node_DependencyCount">DependencyCount</a></td><td>
get the number of dependency nodes</td></tr><tr><td>![Public property](media/pubproperty.gif "Public property")</td><td><a href="P_NSS_Blast_Compiler_node_FirstChild">FirstChild</a></td><td>
first child of node, null if there are no children</td></tr><tr><td>![Public property](media/pubproperty.gif "Public property")</td><td><a href="P_NSS_Blast_Compiler_node_HasChildNodes">HasChildNodes</a></td><td>
True if the node contains children</td></tr><tr><td>![Public property](media/pubproperty.gif "Public property")</td><td><a href="P_NSS_Blast_Compiler_node_HasChildren">HasChildren</a></td><td>
true if the node contains child nodes</td></tr><tr><td>![Public property](media/pubproperty.gif "Public property")</td><td><a href="P_NSS_Blast_Compiler_node_HasDependencies">HasDependencies</a></td><td>
true if the node has dependencies</td></tr><tr><td>![Public property](media/pubproperty.gif "Public property")</td><td><a href="P_NSS_Blast_Compiler_node_HasIdentifier">HasIdentifier</a></td><td>
true if node is represented by an identifier</td></tr><tr><td>![Public property](media/pubproperty.gif "Public property")</td><td><a href="P_NSS_Blast_Compiler_node_HasIndexers">HasIndexers</a></td><td>
true if node contains indexers</td></tr><tr><td>![Public property](media/pubproperty.gif "Public property")</td><td><a href="P_NSS_Blast_Compiler_node_HasOneChild">HasOneChild</a></td><td>
true if the node has exactly 1 node</td></tr><tr><td>![Public property](media/pubproperty.gif "Public property")</td><td><a href="P_NSS_Blast_Compiler_node_IsAssignment">IsAssignment</a></td><td>
True if this is an assignment</td></tr><tr><td>![Public property](media/pubproperty.gif "Public property")</td><td><a href="P_NSS_Blast_Compiler_node_IsCompound">IsCompound</a></td><td>
True if this a compound node</td></tr><tr><td>![Public property](media/pubproperty.gif "Public property")</td><td><a href="P_NSS_Blast_Compiler_node_IsFloat">IsFloat</a></td><td>
interpret this node as a float value and return that</td></tr><tr><td>![Public property](media/pubproperty.gif "Public property")</td><td><a href="P_NSS_Blast_Compiler_node_IsFunction">IsFunction</a></td><td>
True if this is a function</td></tr><tr><td>![Public property](media/pubproperty.gif "Public property")</td><td><a href="P_NSS_Blast_Compiler_node_IsLeaf">IsLeaf</a></td><td>
True if this is a leaf nod</td></tr><tr><td>![Public property](media/pubproperty.gif "Public property")</td><td><a href="P_NSS_Blast_Compiler_node_IsOperation">IsOperation</a></td><td>
True if this is an operation</td></tr><tr><td>![Public property](media/pubproperty.gif "Public property")</td><td><a href="P_NSS_Blast_Compiler_node_IsPopFunction">IsPopFunction</a></td><td>
True if this is a function popping from the stack</td></tr><tr><td>![Public property](media/pubproperty.gif "Public property")</td><td><a href="P_NSS_Blast_Compiler_node_IsPushFunction">IsPushFunction</a></td><td>
True if this is a function pushing to the stack</td></tr><tr><td>![Public property](media/pubproperty.gif "Public property")</td><td><a href="P_NSS_Blast_Compiler_node_IsRoot">IsRoot</a></td><td>
Only true if this is the root of the ast</td></tr><tr><td>![Public property](media/pubproperty.gif "Public property")</td><td><a href="P_NSS_Blast_Compiler_node_IsScriptVariable">IsScriptVariable</a></td><td>
True if this node represents a scripted variable</td></tr><tr><td>![Public property](media/pubproperty.gif "Public property")</td><td><a href="P_NSS_Blast_Compiler_node_IsVector">IsVector</a></td><td>
True if this is data with a cardinality larger then 1 (a vector)</td></tr><tr><td>![Public property](media/pubproperty.gif "Public property")</td><td><a href="P_NSS_Blast_Compiler_node_LastChild">LastChild</a></td><td>
last child of node, null if there are no children, equals first if childcount == 1</td></tr></table>&nbsp;
<a href="#node-properties">Back to Top</a>

## See Also


#### Reference
<a href="T_NSS_Blast_Compiler_node">node Class</a><br /><a href="N_NSS_Blast_Compiler">NSS.Blast.Compiler Namespace</a><br />