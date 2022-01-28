# BlastCompiler Class
 

Blast Compiler Compile scripts into bytecode, the process consists of several stages: For bytecode these stages might be executed: 1 - tokenize: convert code into a list of tokens 2 - parse: parse list of tokens into a tree of nodes 3 - transform: transform nodes sequences 4.1 - analyse parameters: determine parameter types and check inputs and outputs 4.2 - analyse: analyse nodes, reorder were needed, optimize for execution 5 - flatten: flatten execution path 6 - node optimizer: optimize nodes 7 - compile: generate bytecode from the internal representation 8 - optimize bytecode: optimize bytecode patterns 9 - package: package bytecode and set it up for execution


## Inheritance Hierarchy
<a href="https://docs.microsoft.com/dotnet/api/system.object" target="_blank" rel="noopener noreferrer">System.Object</a><br />&nbsp;&nbsp;NSS.Blast.Compiler.BlastCompiler<br />
**Namespace:**&nbsp;<a href="N_NSS_Blast_Compiler">NSS.Blast.Compiler</a><br />**Assembly:**&nbsp;BLAST (in BLAST.dll) Version: 1.0.0

## Syntax

**C#**<br />
``` C#
public static class BlastCompiler
```

The BlastCompiler type exposes the following members.


## Methods
&nbsp;<table><tr><th></th><th>Name</th><th>Description</th></tr><tr><td>![Public method](media/pubmethod.gif "Public method")![Static member](media/static.gif "Static member")</td><td><a href="M_NSS_Blast_Compiler_BlastCompiler_Compile">Compile(BlastEngineDataPtr, BlastScript, BlastCompilerOptions)</a></td><td>
Compile a script</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")![Static member](media/static.gif "Static member")</td><td><a href="M_NSS_Blast_Compiler_BlastCompiler_Compile_1">Compile(BlastEngineDataPtr, String, BlastCompilerOptions)</a></td><td>
Compile script from text</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")![Static member](media/static.gif "Static member")</td><td><a href="M_NSS_Blast_Compiler_BlastCompiler_CompileCS">CompileCS</a></td><td>
Compile using .net c# compiler - only on windows .net framework > 4.5</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")![Static member](media/static.gif "Static member")</td><td><a href="M_NSS_Blast_Compiler_BlastCompiler_CompileDataPackage">CompileDataPackage</a></td><td>
Compile the script into a native blastscript datapackage</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")![Static member](media/static.gif "Static member")</td><td><a href="M_NSS_Blast_Compiler_BlastCompiler_CompileHPC">CompileHPC</a></td><td></td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")![Static member](media/static.gif "Static member")</td><td><a href="M_NSS_Blast_Compiler_BlastCompiler_CompilePackage">CompilePackage</a></td><td>
compile script into a managed blastscriptpackage containing blastpackagedata</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")![Static member](media/static.gif "Static member")</td><td><a href="M_NSS_Blast_Compiler_BlastCompiler_EstimateStackSize">EstimateStackSize</a></td><td>
estimate stack size by running script with a selection of parameters from input, output and validation settings TODO: EstimateStackSize and Validate should be combined</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")![Static member](media/static.gif "Static member")</td><td><a href="M_NSS_Blast_Compiler_BlastCompiler_Package">Package</a></td><td>
package the result of the compiler into an executable package</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")![Static member](media/static.gif "Static member")</td><td><a href="M_NSS_Blast_Compiler_BlastCompiler_Validate">Validate</a></td><td>
Validate output using data set in script for NULL inputs</td></tr></table>&nbsp;
<a href="#blastcompiler-class">Back to Top</a>

## Fields
&nbsp;<table><tr><th></th><th>Name</th><th>Description</th></tr><tr><td>![Public field](media/pubfield.gif "Public field")![Static member](media/static.gif "Static member")</td><td><a href="F_NSS_Blast_Compiler_BlastCompiler_ByteCodeStages">ByteCodeStages</a></td><td>
Default compilation setup</td></tr><tr><td>![Public field](media/pubfield.gif "Public field")![Static member](media/static.gif "Static member")</td><td><a href="F_NSS_Blast_Compiler_BlastCompiler_CSStages">CSStages</a></td><td>
C# compilation uses the output from the hpc stage</td></tr><tr><td>![Public field](media/pubfield.gif "Public field")![Static member](media/static.gif "Static member")</td><td><a href="F_NSS_Blast_Compiler_BlastCompiler_HPCStages">HPCStages</a></td><td>
compilation stages for compilation into a job, - use the same precompilation, make sure behaviour is equal even if some unknown bug is there</td></tr><tr><td>![Public field](media/pubfield.gif "Public field")![Static member](media/static.gif "Static member")</td><td><a href="F_NSS_Blast_Compiler_BlastCompiler_opt_ident">opt_ident</a></td><td>
form this opcode until 255 variables are encoded</td></tr><tr><td>![Public field](media/pubfield.gif "Public field")![Static member](media/static.gif "Static member")</td><td><a href="F_NSS_Blast_Compiler_BlastCompiler_opt_value">opt_value</a></td><td>
from this opcode until opt_ident constant values are encoded</td></tr></table>&nbsp;
<a href="#blastcompiler-class">Back to Top</a>

## See Also


#### Reference
<a href="N_NSS_Blast_Compiler">NSS.Blast.Compiler Namespace</a><br />