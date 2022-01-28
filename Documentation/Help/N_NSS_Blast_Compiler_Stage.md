# NSS.Blast.Compiler.Stage Namespace
 

\[Missing <summary> documentation for "N:NSS.Blast.Compiler.Stage"\]


## Classes
&nbsp;<table><tr><th></th><th>Class</th><th>Description</th></tr><tr><td>![Public class](media/pubclass.gif "Public class")</td><td><a href="T_NSS_Blast_Compiler_Stage_BlastAnalysis">BlastAnalysis</a></td><td>
Basic Node Analysis - force multiplication rules - simplifies arithmetic - refactors divisions</td></tr><tr><td>![Public class](media/pubclass.gif "Public class")</td><td><a href="T_NSS_Blast_Compiler_Stage_BlastBytecodeCompiler">BlastBytecodeCompiler</a></td><td>
Blast Compiler Stage - compiles the ast into bytecode</td></tr><tr><td>![Public class](media/pubclass.gif "Public class")</td><td><a href="T_NSS_Blast_Compiler_Stage_BlastBytecodeOptimizer">BlastBytecodeOptimizer</a></td><td>
Bytecode Optimizer - optimizes bytecode based on pattern recognition</td></tr><tr><td>![Public class](media/pubclass.gif "Public class")</td><td><a href="T_NSS_Blast_Compiler_Stage_BlastBytecodeOptimizer_optimizer_pattern">BlastBytecodeOptimizer.optimizer_pattern</a></td><td /></tr><tr><td>![Public class](media/pubclass.gif "Public class")</td><td><a href="T_NSS_Blast_Compiler_Stage_BlastCSCompiler">BlastCSCompiler</a></td><td /></tr><tr><td>![Public class](media/pubclass.gif "Public class")</td><td><a href="T_NSS_Blast_Compiler_Stage_BlastFlatten">BlastFlatten</a></td><td>
Flatten Nodes - removes all nested things and reduces them to a linear stream of instructions using stack operations</td></tr><tr><td>![Public class](media/pubclass.gif "Public class")</td><td><a href="T_NSS_Blast_Compiler_Stage_BlastHPCCompiler">BlastHPCCompiler</a></td><td>
Blast Compiler Stage - compiles the ast into hpc code for compile-time scripts</td></tr><tr><td>![Public class](media/pubclass.gif "Public class")</td><td><a href="T_NSS_Blast_Compiler_Stage_BlastHPCStackResolver">BlastHPCStackResolver</a></td><td>
Stack Resolver for HPC code - transmute push-pops to stack-var assignments - re-use variables as much as possible</td></tr><tr><td>![Public class](media/pubclass.gif "Public class")</td><td><a href="T_NSS_Blast_Compiler_Stage_BlastJumpResolver">BlastJumpResolver</a></td><td>
Resolve Jumps</td></tr><tr><td>![Public class](media/pubclass.gif "Public class")</td><td><a href="T_NSS_Blast_Compiler_Stage_BlastOptimizer">BlastOptimizer</a></td><td>
Basic Optimization Stage</td></tr><tr><td>![Public class](media/pubclass.gif "Public class")</td><td><a href="T_NSS_Blast_Compiler_Stage_BlastPackaging">BlastPackaging</a></td><td>
Package Stage: process compiled bytecode into packages</td></tr><tr><td>![Public class](media/pubclass.gif "Public class")</td><td><a href="T_NSS_Blast_Compiler_Stage_BlastParameterAnalysis">BlastParameterAnalysis</a></td><td>
Parameter Analysis - determine parameter types (float, vectorsize) - confirm inputs used (warnings if not) and outputs set (not set = error) - validate correct parameter usage</td></tr><tr><td>![Public class](media/pubclass.gif "Public class")</td><td><a href="T_NSS_Blast_Compiler_Stage_BlastParser">BlastParser</a></td><td>
The Parser: - Parses list of tokens into a tree of nodes representing the flow of operations - Identifies unique parameters - Spaghetti warning - handcrafted parser ahead..</td></tr><tr><td>![Public class](media/pubclass.gif "Public class")</td><td><a href="T_NSS_Blast_Compiler_Stage_BlastPreCompileCleanup">BlastPreCompileCleanup</a></td><td>
Pre-Compile Cleanup - for now only cleans up parameter references</td></tr><tr><td>![Public class](media/pubclass.gif "Public class")</td><td><a href="T_NSS_Blast_Compiler_Stage_BlastTokenizer">BlastTokenizer</a></td><td>
The Tokenizer: - splits input into list of tokens and identifiers - splits out: comments, defines and validations (all stuff starting with # until eol)</td></tr><tr><td>![Public class](media/pubclass.gif "Public class")</td><td><a href="T_NSS_Blast_Compiler_Stage_BlastTransform">BlastTransform</a></td><td>
Transform higher level constructs into their simpler constituents, [while, for, switch] into ifthen sequences</td></tr><tr><td>![Public class](media/pubclass.gif "Public class")</td><td><a href="T_NSS_Blast_Compiler_Stage_IMByteCode">IMByteCode</a></td><td>
intermediate bytecode - can contain additional data</td></tr><tr><td>![Public class](media/pubclass.gif "Public class")</td><td><a href="T_NSS_Blast_Compiler_Stage_IMByteCodeList">IMByteCodeList</a></td><td>
list of intermediate bytecode</td></tr><tr><td>![Public class](media/pubclass.gif "Public class")</td><td><a href="T_NSS_Blast_Compiler_Stage_IMJumpLabel">IMJumpLabel</a></td><td>
a jump label, for help keeping track of jump targets</td></tr></table>

## Enumerations
&nbsp;<table><tr><th></th><th>Enumeration</th><th>Description</th></tr><tr><td>![Public enumeration](media/pubenumeration.gif "Public enumeration")</td><td><a href="T_NSS_Blast_Compiler_Stage_JumpLabelType">JumpLabelType</a></td><td /></tr></table>&nbsp;
