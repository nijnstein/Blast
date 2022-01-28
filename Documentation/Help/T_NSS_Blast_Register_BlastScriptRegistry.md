# BlastScriptRegistry Class
 

Here we maintain all scripts registered, either by manually calling Register(script) or by finding it in the binary through reflection. -> the registry is a static singleton -> this registry only stores script, not package data -> access is threadsafe s


## Inheritance Hierarchy
<a href="https://docs.microsoft.com/dotnet/api/system.object" target="_blank" rel="noopener noreferrer">System.Object</a><br />&nbsp;&nbsp;NSS.Blast.Register.BlastScriptRegistry<br />
**Namespace:**&nbsp;<a href="N_NSS_Blast_Register">NSS.Blast.Register</a><br />**Assembly:**&nbsp;BLAST (in BLAST.dll) Version: 1.0.0

## Syntax

**C#**<br />
``` C#
public static class BlastScriptRegistry
```

The BlastScriptRegistry type exposes the following members.


## Methods
&nbsp;<table><tr><th></th><th>Name</th><th>Description</th></tr><tr><td>![Public method](media/pubmethod.gif "Public method")![Static member](media/static.gif "Static member")</td><td><a href="M_NSS_Blast_Register_BlastScriptRegistry_All">All</a></td><td>
Enumerate all registred blast scripts</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")![Static member](media/static.gif "Static member")</td><td><a href="M_NSS_Blast_Register_BlastScriptRegistry_Exists">Exists(Int32)</a></td><td>
lookup the id and check if it exists</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")![Static member](media/static.gif "Static member")</td><td><a href="M_NSS_Blast_Register_BlastScriptRegistry_Exists_1">Exists(String)</a></td><td>
scan the registry for a script with the given name</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")![Static member](media/static.gif "Static member")</td><td><a href="M_NSS_Blast_Register_BlastScriptRegistry_Get">Get(Int32)</a></td><td>
lookup a script with a given script id</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")![Static member](media/static.gif "Static member")</td><td><a href="M_NSS_Blast_Register_BlastScriptRegistry_Get_1">Get(String)</a></td><td>
perform a linear scan for a script with the given name and return iot if found. Case Insensitive</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")![Static member](media/static.gif "Static member")</td><td><a href="M_NSS_Blast_Register_BlastScriptRegistry_Register">Register(BlastScript)</a></td><td /></tr><tr><td>![Public method](media/pubmethod.gif "Public method")![Static member](media/static.gif "Static member")</td><td><a href="M_NSS_Blast_Register_BlastScriptRegistry_Register_1">Register(String, String, Int32)</a></td><td>
register code with given name and id with registry</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")![Static member](media/static.gif "Static member")</td><td><a href="M_NSS_Blast_Register_BlastScriptRegistry_TryGetReferenceId">TryGetReferenceId</a></td><td>
try to find the id of a script with a given name, performs a linear search</td></tr></table>&nbsp;
<a href="#blastscriptregistry-class">Back to Top</a>

## See Also


#### Reference
<a href="N_NSS_Blast_Register">NSS.Blast.Register Namespace</a><br />