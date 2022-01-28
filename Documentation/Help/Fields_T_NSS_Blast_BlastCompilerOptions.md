# BlastCompilerOptions Fields
 

The <a href="T_NSS_Blast_BlastCompilerOptions">BlastCompilerOptions</a> type exposes the following members.


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
<a href="#blastcompileroptions-fields">Back to Top</a>

## See Also


#### Reference
<a href="T_NSS_Blast_BlastCompilerOptions">BlastCompilerOptions Class</a><br /><a href="N_NSS_Blast">NSS.Blast Namespace</a><br />