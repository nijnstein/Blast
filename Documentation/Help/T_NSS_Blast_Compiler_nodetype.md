# nodetype Enumeration
 

the different nodetypes used in the ast

**Namespace:**&nbsp;<a href="N_NSS_Blast_Compiler">NSS.Blast.Compiler</a><br />**Assembly:**&nbsp;BLAST (in BLAST.dll) Version: 1.0.0

## Syntax

**C#**<br />
``` C#
public enum nodetype
```


## Members
&nbsp;<table><tr><th></th><th>Member name</th><th>Value</th><th>Description</th></tr><tr><td /><td target="F:NSS.Blast.Compiler.nodetype.none">**none**</td><td>0</td><td>nodetype is not set</td></tr><tr><td /><td target="F:NSS.Blast.Compiler.nodetype.root">**root**</td><td>1</td><td>this is the root node</td></tr><tr><td /><td target="F:NSS.Blast.Compiler.nodetype.function">**function**</td><td>2</td><td>this node represents a function</td></tr><tr><td /><td target="F:NSS.Blast.Compiler.nodetype.assignment">**assignment**</td><td>3</td><td>this node represents an assignment to identifier</td></tr><tr><td /><td target="F:NSS.Blast.Compiler.nodetype.parameter">**parameter**</td><td>4</td><td>node represents a parameter to a function or sequence with identifier name/value</td></tr><tr><td /><td target="F:NSS.Blast.Compiler.nodetype.index">**index**</td><td>5</td><td>BS2 parameter indexer, .[]</td></tr><tr><td /><td target="F:NSS.Blast.Compiler.nodetype.operation">**operation**</td><td>6</td><td>node represents an operation: +-/ etc.</td></tr><tr><td /><td target="F:NSS.Blast.Compiler.nodetype.yield">**yield**</td><td>7</td><td>yield operation, must be in root</td></tr><tr><td /><td target="F:NSS.Blast.Compiler.nodetype.compound">**compound**</td><td>8</td><td>represents a compound: ( () )</td></tr><tr><td /><td target="F:NSS.Blast.Compiler.nodetype.ifthenelse">**ifthenelse**</td><td>9</td><td>the root of an if then else structure</td></tr><tr><td /><td target="F:NSS.Blast.Compiler.nodetype.ifthen">**ifthen**</td><td>10</td><td>the ifthen clausule</td></tr><tr><td /><td target="F:NSS.Blast.Compiler.nodetype.ifelse">**ifelse**</td><td>11</td><td>the ifelse clausule</td></tr><tr><td /><td target="F:NSS.Blast.Compiler.nodetype.condition">**condition**</td><td>12</td><td>a condition, either in if statements or while/for loops</td></tr><tr><td /><td target="F:NSS.Blast.Compiler.nodetype.whileloop">**whileloop**</td><td>13</td><td>the root of a while loop</td></tr><tr><td /><td target="F:NSS.Blast.Compiler.nodetype.whilecompound">**whilecompound**</td><td>14</td><td>the while loop body, handled as a compound</td></tr><tr><td /><td target="F:NSS.Blast.Compiler.nodetype.switchnode">**switchnode**</td><td>15</td><td>the switch statement root node, transformed into ifthenelse statements during compilation</td></tr><tr><td /><td target="F:NSS.Blast.Compiler.nodetype.switchcase">**switchcase**</td><td>16</td><td>a switch case</td></tr><tr><td /><td target="F:NSS.Blast.Compiler.nodetype.switchdefault">**switchdefault**</td><td>17</td><td>the default case</td></tr><tr><td /><td target="F:NSS.Blast.Compiler.nodetype.forloop">**forloop**</td><td>18</td><td>the root of a for loop</td></tr><tr><td /><td target="F:NSS.Blast.Compiler.nodetype.jump_to">**jump_to**</td><td>19</td><td>a jump instruction inserted by the compiler that jumps to a given label in the ast</td></tr><tr><td /><td target="F:NSS.Blast.Compiler.nodetype.label">**label**</td><td>20</td><td>a label (a jump target) inserted by the compiler</td></tr></table>

## See Also


#### Reference
<a href="N_NSS_Blast_Compiler">NSS.Blast.Compiler Namespace</a><br />