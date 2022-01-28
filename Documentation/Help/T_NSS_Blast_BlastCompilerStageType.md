# BlastCompilerStageType Enumeration
 

the types of compiler stages that run in sequence to produce the output

**Namespace:**&nbsp;<a href="N_NSS_Blast">NSS.Blast</a><br />**Assembly:**&nbsp;BLAST (in BLAST.dll) Version: 1.0.0

## Syntax

**C#**<br />
``` C#
public enum BlastCompilerStageType
```


## Members
&nbsp;<table><tr><th></th><th>Member name</th><th>Value</th><th>Description</th></tr><tr><td /><td target="F:NSS.Blast.BlastCompilerStageType.None">**None**</td><td>0</td><td /></tr><tr><td /><td target="F:NSS.Blast.BlastCompilerStageType.Tokenizer">**Tokenizer**</td><td>1</td><td>convert input script into a list of tokens</td></tr><tr><td /><td target="F:NSS.Blast.BlastCompilerStageType.Parser">**Parser**</td><td>2</td><td>parses the tokens into an ast-tree and identifies identifiers</td></tr><tr><td /><td target="F:NSS.Blast.BlastCompilerStageType.Transform">**Transform**</td><td>3</td><td>transform constructs in the ast: switch -> ifthen, while,for, etc -> ifthen making later stages having less to worry about</td></tr><tr><td /><td target="F:NSS.Blast.BlastCompilerStageType.ParameterAnalysis">**ParameterAnalysis**</td><td>4</td><td>analyse parameter use - determine vectors - enforce multiplication rules</td></tr><tr><td /><td target="F:NSS.Blast.BlastCompilerStageType.Analysis">**Analysis**</td><td>5</td><td>analyze ast structure - basic removal of some useless structures - rules of multiplication</td></tr><tr><td /><td target="F:NSS.Blast.BlastCompilerStageType.Flatten">**Flatten**</td><td>6</td><td>flatten execution path</td></tr><tr><td /><td target="F:NSS.Blast.BlastCompilerStageType.Optimization">**Optimization**</td><td>7</td><td>optimize ast structure - transform expensive constructs into less expensive ones - this should be done after flattening the tree, any optimization that reduces compounds should happen in analysis</td></tr><tr><td /><td target="F:NSS.Blast.BlastCompilerStageType.Cleanup">**Cleanup**</td><td>8</td><td>pre compile cleanup</td></tr><tr><td /><td target="F:NSS.Blast.BlastCompilerStageType.StackResolver">**StackResolver**</td><td>9</td><td>resolve stack operations into stack-variables (HPC/CS only)</td></tr><tr><td /><td target="F:NSS.Blast.BlastCompilerStageType.Compile">**Compile**</td><td>10</td><td>a [bytecode/hpc/cs] compiler</td></tr><tr><td /><td target="F:NSS.Blast.BlastCompilerStageType.BytecodeOptimizer">**BytecodeOptimizer**</td><td>11</td><td>post-compile: bytecode optimizer</td></tr><tr><td /><td target="F:NSS.Blast.BlastCompilerStageType.JumpResolver">**JumpResolver**</td><td>12</td><td>post-compile: resolve jumps</td></tr><tr><td /><td target="F:NSS.Blast.BlastCompilerStageType.Packaging">**Packaging**</td><td>13</td><td>post-compile: package result</td></tr></table>

## See Also


#### Reference
<a href="N_NSS_Blast">NSS.Blast Namespace</a><br />