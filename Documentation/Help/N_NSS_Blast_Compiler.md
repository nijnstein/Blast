# NSS.Blast.Compiler Namespace
 

\[Missing <summary> documentation for "N:NSS.Blast.Compiler"\]


## Classes
&nbsp;<table><tr><th></th><th>Class</th><th>Description</th></tr><tr><td>![Public class](media/pubclass.gif "Public class")</td><td><a href="T_NSS_Blast_Compiler_BlastCompiler">BlastCompiler</a></td><td>
Blast Compiler Compile scripts into bytecode, the process consists of several stages: For bytecode these stages might be executed: 1 - tokenize: convert code into a list of tokens 2 - parse: parse list of tokens into a tree of nodes 3 - transform: transform nodes sequences 4.1 - analyse parameters: determine parameter types and check inputs and outputs 4.2 - analyse: analyse nodes, reorder were needed, optimize for execution 5 - flatten: flatten execution path 6 - node optimizer: optimize nodes 7 - compile: generate bytecode from the internal representation 8 - optimize bytecode: optimize bytecode patterns 9 - package: package bytecode and set it up for execution</td></tr><tr><td>![Public class](media/pubclass.gif "Public class")</td><td><a href="T_NSS_Blast_Compiler_CompilationData">CompilationData</a></td><td>
Data created during compilation and used for analysis, packaging and debugging/display purposes</td></tr><tr><td>![Public class](media/pubclass.gif "Public class")</td><td><a href="T_NSS_Blast_Compiler_CompilationData_Message">CompilationData.Message</a></td><td>
a compiler message</td></tr><tr><td>![Public class](media/pubclass.gif "Public class")</td><td><a href="T_NSS_Blast_Compiler_CSCompilationData">CSCompilationData</a></td><td>
compiler data for the C# compiler - as it uses the output from hpc we base data on its data</td></tr><tr><td>![Public class](media/pubclass.gif "Public class")</td><td><a href="T_NSS_Blast_Compiler_CSNamespace">CSNamespace</a></td><td /></tr><tr><td>![Public class](media/pubclass.gif "Public class")</td><td><a href="T_NSS_Blast_Compiler_HPCCompilationData">HPCCompilationData</a></td><td>
compiler data for the hpc compiler</td></tr><tr><td>![Public class](media/pubclass.gif "Public class")</td><td><a href="T_NSS_Blast_Compiler_HPCNamespace">HPCNamespace</a></td><td /></tr><tr><td>![Public class](media/pubclass.gif "Public class")</td><td><a href="T_NSS_Blast_Compiler_node">node</a></td><td>
an ast node used by the blast compiler</td></tr></table>

## Structures
&nbsp;<table><tr><th></th><th>Structure</th><th>Description</th></tr><tr><td>![Public structure](media/pubstructure.gif "Public structure")</td><td><a href="T_NSS_Blast_Compiler_BlastIntermediate">BlastIntermediate</a></td><td>
intermediate bytecode data for use by compiler</td></tr></table>

## Interfaces
&nbsp;<table><tr><th></th><th>Interface</th><th>Description</th></tr><tr><td>![Public interface](media/pubinterface.gif "Public interface")</td><td><a href="T_NSS_Blast_Compiler_IBlastCompilationData">IBlastCompilationData</a></td><td>
interface into compilationdata, we should restrict outside use a bit via this</td></tr></table>

## Enumerations
&nbsp;<table><tr><th></th><th>Enumeration</th><th>Description</th></tr><tr><td>![Public enumeration](media/pubenumeration.gif "Public enumeration")</td><td><a href="T_NSS_Blast_Compiler_CompilationData_Message_MessageType">CompilationData.Message.MessageType</a></td><td /></tr><tr><td>![Public enumeration](media/pubenumeration.gif "Public enumeration")</td><td><a href="T_NSS_Blast_Compiler_nodetype">nodetype</a></td><td>
the different nodetypes used in the ast</td></tr></table>&nbsp;
