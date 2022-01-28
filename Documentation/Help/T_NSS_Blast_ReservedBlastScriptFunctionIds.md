# ReservedBlastScriptFunctionIds Enumeration
 

IDs reserved for functions used in blast, these get registered to blast with a name (that doesnt need to be equal)

**Namespace:**&nbsp;<a href="N_NSS_Blast">NSS.Blast</a><br />**Assembly:**&nbsp;BLAST (in BLAST.dll) Version: 1.0.0

## Syntax

**C#**<br />
``` C#
public enum ReservedBlastScriptFunctionIds
```


## Members
&nbsp;<table><tr><th></th><th>Member name</th><th>Value</th><th>Description</th></tr><tr><td /><td target="F:NSS.Blast.ReservedBlastScriptFunctionIds.Input">**Input**</td><td>1</td><td>input mapping function - reserved for internal use</td></tr><tr><td /><td target="F:NSS.Blast.ReservedBlastScriptFunctionIds.Output">**Output**</td><td>2</td><td>output mapping function - reserved for internal use</td></tr><tr><td /><td target="F:NSS.Blast.ReservedBlastScriptFunctionIds.Yield">**Yield**</td><td>3</td><td>Yield execution</td></tr><tr><td /><td target="F:NSS.Blast.ReservedBlastScriptFunctionIds.Pop">**Pop**</td><td>4</td><td>Pop something from the stack</td></tr><tr><td /><td target="F:NSS.Blast.ReservedBlastScriptFunctionIds.Peek">**Peek**</td><td>5</td><td>Peek stack top</td></tr><tr><td /><td target="F:NSS.Blast.ReservedBlastScriptFunctionIds.Seed">**Seed**</td><td>6</td><td>Seed the random number generator</td></tr><tr><td /><td target="F:NSS.Blast.ReservedBlastScriptFunctionIds.Push">**Push**</td><td>7</td><td>Push something to the stack</td></tr><tr><td /><td target="F:NSS.Blast.ReservedBlastScriptFunctionIds.PushFunction">**PushFunction**</td><td>8</td><td>Push a function's return value to the stack</td></tr><tr><td /><td target="F:NSS.Blast.ReservedBlastScriptFunctionIds.PushCompound">**PushCompound**</td><td>9</td><td>Push the result from executing a compound onto the stack</td></tr><tr><td /><td target="F:NSS.Blast.ReservedBlastScriptFunctionIds.PushVector">**PushVector**</td><td>10</td><td>Push a vector directly onto the stack</td></tr><tr><td /><td target="F:NSS.Blast.ReservedBlastScriptFunctionIds.Debug">**Debug**</td><td>11</td><td>Output debuginformation to the debug stream about given parameter</td></tr><tr><td /><td target="F:NSS.Blast.ReservedBlastScriptFunctionIds.DebugStack">**DebugStack**</td><td>12</td><td>Output debuginformation to the debug stream</td></tr><tr><td /><td target="F:NSS.Blast.ReservedBlastScriptFunctionIds.Offset">**Offset**</td><td>32</td><td>all other functions start indexing from this offset</td></tr></table>

## See Also


#### Reference
<a href="N_NSS_Blast">NSS.Blast Namespace</a><br />