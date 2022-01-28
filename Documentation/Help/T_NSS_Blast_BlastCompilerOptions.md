# BlastCompilerOptions Class
 

Options for compiling and packaging scripts


## Inheritance Hierarchy
<a href="https://docs.microsoft.com/dotnet/api/system.object" target="_blank" rel="noopener noreferrer">System.Object</a><br />&nbsp;&nbsp;NSS.Blast.BlastCompilerOptions<br />
**Namespace:**&nbsp;<a href="N_NSS_Blast">NSS.Blast</a><br />**Assembly:**&nbsp;BLAST (in BLAST.dll) Version: 1.0.0

## Syntax

**C#**<br />
``` C#
public class BlastCompilerOptions
```

The BlastCompilerOptions type exposes the following members.


## Constructors
&nbsp;<table><tr><th></th><th>Name</th><th>Description</th></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_NSS_Blast_BlastCompilerOptions__ctor">BlastCompilerOptions()</a></td><td>
initialize with default options</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_NSS_Blast_BlastCompilerOptions__ctor_1">BlastCompilerOptions(BlastPackageMode)</a></td><td>
initilize default options in given packaging mode</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_NSS_Blast_BlastCompilerOptions__ctor_2">BlastCompilerOptions(Int32)</a></td><td>
initialize default options with given stacksize</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_NSS_Blast_BlastCompilerOptions__ctor_3">BlastCompilerOptions(Int32, BlastPackageMode)</a></td><td>
initialize default options in given packagemode with a pre-determined stack size</td></tr></table>&nbsp;
<a href="#blastcompileroptions-class">Back to Top</a>

## Properties
&nbsp;<table><tr><th></th><th>Name</th><th>Description</th></tr><tr><td>![Public property](media/pubproperty.gif "Public property")![Static member](media/static.gif "Static member")</td><td><a href="P_NSS_Blast_BlastCompilerOptions_Default">Default</a></td><td>
Default compiler options: BS1 - Normal - Optimizing</td></tr><tr><td>![Public property](media/pubproperty.gif "Public property")![Static member](media/static.gif "Static member")</td><td><a href="P_NSS_Blast_BlastCompilerOptions_Entity">Entity</a></td><td>
Default compiler options for packaging entities: BS1 - Entity Packagemode - Optimizing</td></tr><tr><td>![Public property](media/pubproperty.gif "Public property")![Static member](media/static.gif "Static member")</td><td><a href="P_NSS_Blast_BlastCompilerOptions_SSMD">SSMD</a></td><td>
Default SSMD compiler options: BS1 - SSMD Packagemode - Optimizing</td></tr></table>&nbsp;
<a href="#blastcompileroptions-class">Back to Top</a>

## Methods
&nbsp;<table><tr><th></th><th>Name</th><th>Description</th></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_NSS_Blast_BlastCompilerOptions_AddDefine">AddDefine</a></td><td>
add a compiled define</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_NSS_Blast_BlastCompilerOptions_ClearDefines">ClearDefines</a></td><td>
remove all defines from compiler options</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_NSS_Blast_BlastCompilerOptions_DisableOptimisations">DisableOptimisations</a></td><td>
disable compiler optimisations, default = enabled</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_NSS_Blast_BlastCompilerOptions_EnableOptimisations">EnableOptimisations</a></td><td>
enable compiler optimizations, default == enabled</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_NSS_Blast_BlastCompilerOptions_EnableValidation">EnableValidation</a></td><td>
enable validation (default = off)</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="https://docs.microsoft.com/dotnet/api/system.object.equals#system-object-equals(system-object)" target="_blank" rel="noopener noreferrer">Equals</a></td><td>
Determines whether the specified object is equal to the current object.
 (Inherited from <a href="https://docs.microsoft.com/dotnet/api/system.object" target="_blank" rel="noopener noreferrer">Object</a>.)</td></tr><tr><td>![Protected method](media/protmethod.gif "Protected method")</td><td><a href="https://docs.microsoft.com/dotnet/api/system.object.finalize#system-object-finalize" target="_blank" rel="noopener noreferrer">Finalize</a></td><td>
Allows an object to try to free resources and perform other cleanup operations before it is reclaimed by garbage collection.
 (Inherited from <a href="https://docs.microsoft.com/dotnet/api/system.object" target="_blank" rel="noopener noreferrer">Object</a>.)</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="https://docs.microsoft.com/dotnet/api/system.object.gethashcode#system-object-gethashcode" target="_blank" rel="noopener noreferrer">GetHashCode</a></td><td>
Serves as the default hash function.
 (Inherited from <a href="https://docs.microsoft.com/dotnet/api/system.object" target="_blank" rel="noopener noreferrer">Object</a>.)</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="https://docs.microsoft.com/dotnet/api/system.object.gettype#system-object-gettype" target="_blank" rel="noopener noreferrer">GetType</a></td><td>
Gets the <a href="https://docs.microsoft.com/dotnet/api/system.type" target="_blank" rel="noopener noreferrer">Type</a> of the current instance.
 (Inherited from <a href="https://docs.microsoft.com/dotnet/api/system.object" target="_blank" rel="noopener noreferrer">Object</a>.)</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_NSS_Blast_BlastCompilerOptions_IsDefined">IsDefined</a></td><td>
Check if the key is defined in compiler options</td></tr><tr><td>![Protected method](media/protmethod.gif "Protected method")</td><td><a href="https://docs.microsoft.com/dotnet/api/system.object.memberwiseclone#system-object-memberwiseclone" target="_blank" rel="noopener noreferrer">MemberwiseClone</a></td><td>
Creates a shallow copy of the current <a href="https://docs.microsoft.com/dotnet/api/system.object" target="_blank" rel="noopener noreferrer">Object</a>.
 (Inherited from <a href="https://docs.microsoft.com/dotnet/api/system.object" target="_blank" rel="noopener noreferrer">Object</a>.)</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_NSS_Blast_BlastCompilerOptions_PackageWithoutStack">PackageWithoutStack</a></td><td>
package without a stack segment, use stackmemory in the interpretor</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_NSS_Blast_BlastCompilerOptions_RemoveDefine">RemoveDefine</a></td><td>
Remove any value possibly assosiated with the key</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_NSS_Blast_BlastCompilerOptions_SetAllocator">SetAllocator</a></td><td>
Set allocation mode for packages, use temp with caution in combination with bursted code</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_NSS_Blast_BlastCompilerOptions_SetLanguage">SetLanguage</a></td><td>
Set language target</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_NSS_Blast_BlastCompilerOptions_SetPackageMode">SetPackageMode</a></td><td>
Set packaging mode</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_NSS_Blast_BlastCompilerOptions_SetStackSize">SetStackSize</a></td><td>
set the size of the stack, if 0 is set stack is estimated during compilation</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_NSS_Blast_BlastCompilerOptions_Silent">Silent</a></td><td>
disable all logging, fails silently</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="https://docs.microsoft.com/dotnet/api/system.object.tostring#system-object-tostring" target="_blank" rel="noopener noreferrer">ToString</a></td><td>
Returns a string that represents the current object.
 (Inherited from <a href="https://docs.microsoft.com/dotnet/api/system.object" target="_blank" rel="noopener noreferrer">Object</a>.)</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_NSS_Blast_BlastCompilerOptions_Trace">Trace</a></td><td>
set trace logging</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_NSS_Blast_BlastCompilerOptions_TryGetDefine">TryGetDefine</a></td><td>
Try to lookup a key and get its value</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_NSS_Blast_BlastCompilerOptions_Verbose">Verbose</a></td><td>
set verbose logging</td></tr></table>&nbsp;
<a href="#blastcompileroptions-class">Back to Top</a>

## Fields
&nbsp;<table><tr><th></th><th>Name</th><th>Description</th></tr><tr><td>![Public field](media/pubfield.gif "Public field")</td><td><a href="F_NSS_Blast_BlastCompilerOptions_AutoValidate">AutoValidate</a></td><td>
run validation if set in script</td></tr><tr><td>![Public field](media/pubfield.gif "Public field")</td><td><a href="F_NSS_Blast_BlastCompilerOptions_CompileDebug">CompileDebug</a></td><td>
compile the debug function only if enabled</td></tr><tr><td>![Public field](media/pubfield.gif "Public field")</td><td><a href="F_NSS_Blast_BlastCompilerOptions_CompileWithSystemConstants">CompileWithSystemConstants</a></td><td>
compile script using constant data fields</td></tr><tr><td>![Public field](media/pubfield.gif "Public field")</td><td><a href="F_NSS_Blast_BlastCompilerOptions_ConstantEpsilon">ConstantEpsilon</a></td><td>
compare epsilon for constants, on fast float mode it wont match on float.epsilon</td></tr><tr><td>![Public field](media/pubfield.gif "Public field")</td><td><a href="F_NSS_Blast_BlastCompilerOptions_ConstantPrecision">ConstantPrecision</a></td><td>
constant precision expressed as a string value</td></tr><tr><td>![Public field](media/pubfield.gif "Public field")</td><td><a href="F_NSS_Blast_BlastCompilerOptions_DefaultStackSize">DefaultStackSize</a></td><td>
default stack size in number of bytes - overridden if the script defines a stack size</td></tr><tr><td>![Public field](media/pubfield.gif "Public field")</td><td><a href="F_NSS_Blast_BlastCompilerOptions_Defines">Defines</a></td><td>
additional compiler defines</td></tr><tr><td>![Public field](media/pubfield.gif "Public field")</td><td><a href="F_NSS_Blast_BlastCompilerOptions_EstimateStackSize">EstimateStackSize</a></td><td>
estimate stack size using input, output and validation parameters - overridden if the script defines a stack size</td></tr><tr><td>![Public field](media/pubfield.gif "Public field")</td><td><a href="F_NSS_Blast_BlastCompilerOptions_Experimental">Experimental</a></td><td>
dont touch if you need to ask</td></tr><tr><td>![Public field](media/pubfield.gif "Public field")</td><td><a href="F_NSS_Blast_BlastCompilerOptions_Language">Language</a></td><td>
The languageversion to use for the output of the compilation. Packagemode and language version are coupled but there may be deviations: - You can execute BS1 language version files with SSMD package modes if you guarentee that the code doesnt branch (the interpretor wont check this) - You can exectue BS1SSMD compiled packages with the normal interpretor in normal package mode</td></tr><tr><td>![Public field](media/pubfield.gif "Public field")</td><td><a href="F_NSS_Blast_BlastCompilerOptions_Optimize">Optimize</a></td><td>
run code optimizer</td></tr><tr><td>![Public field](media/pubfield.gif "Public field")</td><td><a href="F_NSS_Blast_BlastCompilerOptions_PackageAllocator">PackageAllocator</a></td><td>
Allocator for data package</td></tr><tr><td>![Public field](media/pubfield.gif "Public field")</td><td><a href="F_NSS_Blast_BlastCompilerOptions_PackageMode">PackageMode</a></td><td>
the package mode for the output, this determines how code and data is organized in memory</td></tr><tr><td>![Public field](media/pubfield.gif "Public field")</td><td><a href="F_NSS_Blast_BlastCompilerOptions_PackageStack">PackageStack</a></td><td>
packages include stackdata</td></tr><tr><td>![Public field](media/pubfield.gif "Public field")</td><td><a href="F_NSS_Blast_BlastCompilerOptions_ParallelCompilation">ParallelCompilation</a></td><td>
enable for partially parallel compilation [forcebly disabled]</td></tr><tr><td>![Public field](media/pubfield.gif "Public field")</td><td><a href="F_NSS_Blast_BlastCompilerOptions_SupportYield">SupportYield</a></td><td>
if yield is not emitted we can save 20 bytes on the stack, with very small stuff that might be handy not implemented .. todo</td></tr><tr><td>![Public field](media/pubfield.gif "Public field")</td><td><a href="F_NSS_Blast_BlastCompilerOptions_TraceLogging">TraceLogging</a></td><td>
enable trace level logging -> will generate a massive report</td></tr><tr><td>![Public field](media/pubfield.gif "Public field")</td><td><a href="F_NSS_Blast_BlastCompilerOptions_VerboseLogging">VerboseLogging</a></td><td>
verbose report logging, log is also blitted to unity</td></tr></table>&nbsp;
<a href="#blastcompileroptions-class">Back to Top</a>

## See Also


#### Reference
<a href="N_NSS_Blast">NSS.Blast Namespace</a><br />