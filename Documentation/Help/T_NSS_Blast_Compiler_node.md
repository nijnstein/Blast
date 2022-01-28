# node Class
 

an ast node used by the blast compiler


## Inheritance Hierarchy
<a href="https://docs.microsoft.com/dotnet/api/system.object" target="_blank" rel="noopener noreferrer">System.Object</a><br />&nbsp;&nbsp;NSS.Blast.Compiler.node<br />
**Namespace:**&nbsp;<a href="N_NSS_Blast_Compiler">NSS.Blast.Compiler</a><br />**Assembly:**&nbsp;BLAST (in BLAST.dll) Version: 1.0.0

## Syntax

**C#**<br />
``` C#
public class node
```

The node type exposes the following members.


## Constructors
&nbsp;<table><tr><th></th><th>Name</th><th>Description</th></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_NSS_Blast_Compiler_node__ctor">node(node, node[])</a></td><td>
general constructor</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_NSS_Blast_Compiler_node__ctor_1">node(nodetype, BlastScriptToken)</a></td><td>
general constructor</td></tr></table>&nbsp;
<a href="#node-class">Back to Top</a>

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
<a href="#node-class">Back to Top</a>

## Methods
&nbsp;<table><tr><th></th><th>Name</th><th>Description</th></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_NSS_Blast_Compiler_node_AppendDependencies">AppendDependencies</a></td><td>
append multiple dependant nodes</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_NSS_Blast_Compiler_node_AppendDependency">AppendDependency</a></td><td>
add a node to the list of nodes to depend on, these are tobe inserted before this node during compilation, the parent of the node is updated to this</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")![Static member](media/static.gif "Static member")</td><td><a href="M_NSS_Blast_Compiler_node_CountChildNodes">CountChildNodes</a></td><td>
get the total number of nodes in the children of n</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_NSS_Blast_Compiler_node_CountChildren">CountChildren</a></td><td>
count children of given nodetype</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_NSS_Blast_Compiler_node_CountChildType">CountChildType(nodetype)</a></td><td>
count child nodes of the given type</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_NSS_Blast_Compiler_node_CountChildType_1">CountChildType(nodetype, nodetype)</a></td><td>
count all children matching one of 2 types</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_NSS_Blast_Compiler_node_CountNodes">CountNodes</a></td><td>
get the total number of nodes in the tree as seen from current node</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_NSS_Blast_Compiler_node_CountOtherChildren">CountOtherChildren</a></td><td>
count nr of childnodes with not nodetype t</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")![Static member](media/static.gif "Static member")</td><td><a href="M_NSS_Blast_Compiler_node_CreateConstantParameter">CreateConstantParameter</a></td><td>
create a constant value parameter from operation</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")![Static member](media/static.gif "Static member")</td><td><a href="M_NSS_Blast_Compiler_node_CreatePopNode">CreatePopNode</a></td><td>
create a pop node based on the information pushed, links the push and pop together</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")![Static member](media/static.gif "Static member")</td><td><a href="M_NSS_Blast_Compiler_node_CreatePushNode">CreatePushNode</a></td><td>
create a push node with the information from the given node, THIS DOES NOT ADD THAT NODE AS CHILD</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_NSS_Blast_Compiler_node_DeepClone">DeepClone</a></td><td>
deep clones node without root parent set</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")![Static member](media/static.gif "Static member")</td><td><a href="M_NSS_Blast_Compiler_node_encode44">encode44(node)</a></td><td>
-encode vectorsize in lower nibble -encode childcount == parametercount in high nibble</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")![Static member](media/static.gif "Static member")</td><td><a href="M_NSS_Blast_Compiler_node_encode44_1">encode44(node, Byte)</a></td><td>
-encode vectorsize in lower nibble -encode parametercount in high nibble</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_NSS_Blast_Compiler_node_EnsureIdentifierIsUniquelySet">EnsureIdentifierIsUniquelySet</a></td><td>
make sure the identifier is set to a UniqueID if it is not set a new id is generated</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="https://docs.microsoft.com/dotnet/api/system.object.equals#system-object-equals(system-object)" target="_blank" rel="noopener noreferrer">Equals</a></td><td>
Determines whether the specified object is equal to the current object.
 (Inherited from <a href="https://docs.microsoft.com/dotnet/api/system.object" target="_blank" rel="noopener noreferrer">Object</a>.)</td></tr><tr><td>![Protected method](media/protmethod.gif "Protected method")</td><td><a href="https://docs.microsoft.com/dotnet/api/system.object.finalize#system-object-finalize" target="_blank" rel="noopener noreferrer">Finalize</a></td><td>
Allows an object to try to free resources and perform other cleanup operations before it is reclaimed by garbage collection.
 (Inherited from <a href="https://docs.microsoft.com/dotnet/api/system.object" target="_blank" rel="noopener noreferrer">Object</a>.)</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_NSS_Blast_Compiler_node_FirstConsecutiveOperationSequence_1">FirstConsecutiveOperationSequence(Int32, Int32, blast_operation, Int32, node)</a></td><td>
scan children of node for the first group of operations of a given minimal size</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")![Static member](media/static.gif "Static member")</td><td><a href="M_NSS_Blast_Compiler_node_FirstConsecutiveOperationSequence">FirstConsecutiveOperationSequence(node, Int32, Int32, blast_operation, Int32, node)</a></td><td>
get the largest group of operations of the same type</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_NSS_Blast_Compiler_node_GetChild">GetChild(nodetype)</a></td><td>
get the first child found of a given nodetype</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_NSS_Blast_Compiler_node_GetChild_1">GetChild(nodetype, nodetype)</a></td><td>
get first child matching first choice, if none found, try the second choice</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_NSS_Blast_Compiler_node_GetChildren">GetChildren</a></td><td>
get children of given nodetype</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_NSS_Blast_Compiler_node_GetConstantFloat4Value">GetConstantFloat4Value</a></td><td>
get the constant float4 value encoded by the variable - float4: vector size is checked and errors will be logged if incorrect</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="https://docs.microsoft.com/dotnet/api/system.object.gethashcode#system-object-gethashcode" target="_blank" rel="noopener noreferrer">GetHashCode</a></td><td>
Serves as the default hash function.
 (Inherited from <a href="https://docs.microsoft.com/dotnet/api/system.object" target="_blank" rel="noopener noreferrer">Object</a>.)</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_NSS_Blast_Compiler_node_GetLeafNodes">GetLeafNodes</a></td><td>
get all leaf nodes from this node</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_NSS_Blast_Compiler_node_GetMaximumTreeDepth">GetMaximumTreeDepth</a></td><td>
get maximum depth of node tree starting from this node</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_NSS_Blast_Compiler_node_GetNodeDescription">GetNodeDescription</a></td><td>
get a textual description of this node</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_NSS_Blast_Compiler_node_GetOtherChildren">GetOtherChildren</a></td><td>
get child nodes not of the type t</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="https://docs.microsoft.com/dotnet/api/system.object.gettype#system-object-gettype" target="_blank" rel="noopener noreferrer">GetType</a></td><td>
Gets the <a href="https://docs.microsoft.com/dotnet/api/system.type" target="_blank" rel="noopener noreferrer">Type</a> of the current instance.
 (Inherited from <a href="https://docs.microsoft.com/dotnet/api/system.object" target="_blank" rel="noopener noreferrer">Object</a>.)</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_NSS_Blast_Compiler_node_HasSingleCompoundAsChild">HasSingleCompoundAsChild</a></td><td>
check if we have 1 child and that it is a compounded statement list</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_NSS_Blast_Compiler_node_InsertBeforeThisNodeInParent">InsertBeforeThisNodeInParent</a></td><td>
insert a new node of the given type and operation before this node in parent</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_NSS_Blast_Compiler_node_InsertChild">InsertChild</a></td><td>
insert a child not at given index - removes new child from old parent - set new childs parent to this node</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_NSS_Blast_Compiler_node_InsertDependency">InsertDependency</a></td><td>
insert (actually it appends) a depenency, updateing parent and chldren list</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_NSS_Blast_Compiler_node_InsertParent">InsertParent</a></td><td>
insert a node as new parent to this node => before: Parent.Child, after: Parent.NewNode.Child</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_NSS_Blast_Compiler_node_IsCompoundWithSingleNegationOfValue">IsCompoundWithSingleNegationOfValue()</a></td><td>
check if the node IS: [compound][-][param|pop_or_val][/compound]</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")![Static member](media/static.gif "Static member")</td><td><a href="M_NSS_Blast_Compiler_node_IsCompoundWithSingleNegationOfValue_1">IsCompoundWithSingleNegationOfValue(node)</a></td><td>
check if the node IS: [compound][-][param|pop_or_val][/compound]</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_NSS_Blast_Compiler_node_IsFlat">IsFlat</a></td><td>
check if the node is a flat node - contains NO compounds - contains no object with children other then a function</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")![Static member](media/static.gif "Static member")</td><td><a href="M_NSS_Blast_Compiler_node_IsFlatParameterList">IsFlatParameterList</a></td><td>
check if the list of given nodes is flat - no nested things - no function calls ?? might want to be looser here... todo</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_NSS_Blast_Compiler_node_IsNonNestedVectorDefinition">IsNonNestedVectorDefinition()</a></td><td>
check if this node is a definition of a vector that does not nest: (1 1 1 1) (1 pop pop) (2 maxa(1) 2 3) (3, -1 2 2) => (3 (-1) 2 2)</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")![Static member](media/static.gif "Static member")</td><td><a href="M_NSS_Blast_Compiler_node_IsNonNestedVectorDefinition_1">IsNonNestedVectorDefinition(node)</a></td><td>
check if given node contains a non nested vector define: node = (1 2 3 4) | node = (1 2 (-3) 4)</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_NSS_Blast_Compiler_node_IsOperationList">IsOperationList()</a></td><td>
check if the node is an operation sequence in the form: 3 + a + 4 + 4 + max(3093) + (4 + 0)</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")![Static member](media/static.gif "Static member")</td><td><a href="M_NSS_Blast_Compiler_node_IsOperationList_1">IsOperationList(node)</a></td><td>
check if the node is an operation sequence in the form: 3 + a + 4 + 4 + max(3093) + (4 + 0)</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")![Static member](media/static.gif "Static member")</td><td><a href="M_NSS_Blast_Compiler_node_IsOperationList_2">IsOperationList(node, blast_operation)</a></td><td>
check if the node is an operation sequence in the form: 3 + a + 4 + 4 + max(3093) + (4 + 0)</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_NSS_Blast_Compiler_node_IsSingleOperationList">IsSingleOperationList</a></td><td>
check if this node contains an operation sequence with only 1 different operator: a = 1 * 3 * 3 * 4;</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_NSS_Blast_Compiler_node_IsSingleValueOrPop">IsSingleValueOrPop()</a></td><td>
determine if the node is a single value or a pop operation</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")![Static member](media/static.gif "Static member")</td><td><a href="M_NSS_Blast_Compiler_node_IsSingleValueOrPop_1">IsSingleValueOrPop(node)</a></td><td>
determine if the node is a single value or a pop operation</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")![Static member](media/static.gif "Static member")</td><td><a href="M_NSS_Blast_Compiler_node_IsVectorDefinition">IsVectorDefinition</a></td><td>
check if a node equals a vector definition: -> ( 1 2 3 ) -> compound[3] ( id id id ) -> compound[n] ( n[pop | identifier[1]] ) -> root node == compound with n children representing its elements</td></tr><tr><td>![Protected method](media/protmethod.gif "Protected method")</td><td><a href="https://docs.microsoft.com/dotnet/api/system.object.memberwiseclone#system-object-memberwiseclone" target="_blank" rel="noopener noreferrer">MemberwiseClone</a></td><td>
Creates a shallow copy of the current <a href="https://docs.microsoft.com/dotnet/api/system.object" target="_blank" rel="noopener noreferrer">Object</a>.
 (Inherited from <a href="https://docs.microsoft.com/dotnet/api/system.object" target="_blank" rel="noopener noreferrer">Object</a>.)</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")![Static member](media/static.gif "Static member")</td><td><a href="M_NSS_Blast_Compiler_node_ReduceSingularCompounds">ReduceSingularCompounds</a></td><td>
recursively reduces unneeded compound nesting (((2 3))) => (2 2)</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_NSS_Blast_Compiler_node_SetDependency">SetDependency</a></td><td>
set a dependency for this node, some constructs such as loops use this for the initializer</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_NSS_Blast_Compiler_node_ToNodeTreeString">ToNodeTreeString</a></td><td>
generate a multiline string representing the node tree structure</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_NSS_Blast_Compiler_node_ToString">ToString</a></td><td>
provides information in debug display through a tostring overload
 (Overrides <a href="https://docs.microsoft.com/dotnet/api/system.object.tostring#system-object-tostring" target="_blank" rel="noopener noreferrer">Object.ToString()</a>.)</td></tr></table>&nbsp;
<a href="#node-class">Back to Top</a>

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
<a href="#node-class">Back to Top</a>

## See Also


#### Reference
<a href="N_NSS_Blast_Compiler">NSS.Blast.Compiler Namespace</a><br />