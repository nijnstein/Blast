# NSS.Blast Namespace
 

\[Missing <summary> documentation for "N:NSS.Blast"\]


## Classes
&nbsp;<table><tr><th></th><th>Class</th><th>Description</th></tr><tr><td>![Public class](media/pubclass.gif "Public class")</td><td><a href="T_NSS_Blast_BlastCompilerOptions">BlastCompilerOptions</a></td><td>
Options for compiling and packaging scripts</td></tr><tr><td>![Public class](media/pubclass.gif "Public class")</td><td><a href="T_NSS_Blast_BlastScript">BlastScript</a></td><td>
A BLAST Script</td></tr><tr><td>![Public class](media/pubclass.gif "Public class")</td><td><a href="T_NSS_Blast_BlastScriptAPI">BlastScriptAPI</a></td><td>
For now, just a collection of function pointers that holds a native list with function information</td></tr><tr><td>![Public class](media/pubclass.gif "Public class")</td><td><a href="T_NSS_Blast_BlastScriptFunctionInfo">BlastScriptFunctionInfo</a></td><td>
managed information on functions</td></tr><tr><td>![Public class](media/pubclass.gif "Public class")</td><td><a href="T_NSS_Blast_BlastScriptPackage">BlastScriptPackage</a></td><td>
BlastScriptPackage contains all data needed to use and execute scriptcode, it consists of: - BlastPackage -> contains the native bytecode - Variable information - IO mapping - Bursted functionpointer mapping</td></tr><tr><td>![Public class](media/pubclass.gif "Public class")</td><td><a href="T_NSS_Blast_BlastScriptTokenDefinition">BlastScriptTokenDefinition</a></td><td>
definition of a token used in blast script</td></tr><tr><td>![Public class](media/pubclass.gif "Public class")</td><td><a href="T_NSS_Blast_BlastVariable">BlastVariable</a></td><td>
Description of a variable as determined during compilation of script. It is not needed for execution and is thus not available through the native BlastPackageData. Native code can query metadata.</td></tr><tr><td>![Public class](media/pubclass.gif "Public class")</td><td><a href="T_NSS_Blast_BlastVariableMapping">BlastVariableMapping</a></td><td>
description mapping a variable to an input or output</td></tr><tr><td>![Public class](media/pubclass.gif "Public class")</td><td><a href="T_NSS_Blast_CodeUtils">CodeUtils</a></td><td /></tr><tr><td>![Public class](media/pubclass.gif "Public class")</td><td><a href="T_NSS_Blast_CompileTimeBlastScript">CompileTimeBlastScript</a></td><td>
any script derived from this will be compiled during compilation/design public class BS1_compiletime : CompileTimeBlastScript { public override string Code => @"a = a + 1;"; }</td></tr><tr><td>![Public class](media/pubclass.gif "Public class")</td><td><a href="T_NSS_Blast_CoreAPI">CoreAPI</a></td><td>
Implements Core-API functionality</td></tr><tr><td>![Public class](media/pubclass.gif "Public class")</td><td><a href="T_NSS_Blast_math_extensions">math_extensions</a></td><td /></tr></table>

## Structures
&nbsp;<table><tr><th></th><th>Structure</th><th>Description</th></tr><tr><td>![Public structure](media/pubstructure.gif "Public structure")</td><td><a href="T_NSS_Blast_Blast">Blast</a></td><td>
Blast Engine</td></tr><tr><td>![Public structure](media/pubstructure.gif "Public structure")</td><td><a href="T_NSS_Blast_BlastEngineData">BlastEngineData</a></td><td>
BLAST execution data - used during execution of scripts - shared by all threads</td></tr><tr><td>![Public structure](media/pubstructure.gif "Public structure")</td><td><a href="T_NSS_Blast_BlastEngineDataPtr">BlastEngineDataPtr</a></td><td>
abstraction for enginedata pointer</td></tr><tr><td>![Public structure](media/pubstructure.gif "Public structure")</td><td><a href="T_NSS_Blast_BlastMetaDataStack">BlastMetaDataStack</a></td><td>
a pointer to meta-data-stack memory</td></tr><tr><td>![Public structure](media/pubstructure.gif "Public structure")</td><td><a href="T_NSS_Blast_BlastPackageData">BlastPackageData</a></td><td>
the bare minimum data (28 bytes) to package a script for execution</td></tr><tr><td>![Public structure](media/pubstructure.gif "Public structure")</td><td><a href="T_NSS_Blast_BlastScriptFunction">BlastScriptFunction</a></td><td>
burst compatible function description</td></tr><tr><td>![Public structure](media/pubstructure.gif "Public structure")</td><td><a href="T_NSS_Blast_BlastSSMDDataStack">BlastSSMDDataStack</a></td><td>
data for ssmd operation modes: data-stack only - only used as pointer, fields handy in debugger</td></tr><tr><td>![Public structure](media/pubstructure.gif "Public structure")</td><td><a href="T_NSS_Blast_NonGenericFunctionPointer">NonGenericFunctionPointer</a></td><td /></tr><tr><td>![Public structure](media/pubstructure.gif "Public structure")</td><td><a href="T_NSS_Blast_UnsafeUtils">UnsafeUtils</a></td><td /></tr></table>

## Interfaces
&nbsp;<table><tr><th></th><th>Interface</th><th>Description</th></tr><tr><td>![Public interface](media/pubinterface.gif "Public interface")</td><td><a href="T_NSS_Blast_IBlastCompilerStage">IBlastCompilerStage</a></td><td>
a compiler stage - employs 1 step of the compilation process</td></tr></table>

## Delegates
&nbsp;<table><tr><th></th><th>Delegate</th><th>Description</th></tr><tr><td>![Public delegate](media/pubdelegate.gif "Public delegate")</td><td><a href="T_NSS_Blast_Blast_BlastDelegate_f0">Blast.BlastDelegate_f0</a></td><td /></tr><tr><td>![Public delegate](media/pubdelegate.gif "Public delegate")</td><td><a href="T_NSS_Blast_Blast_BlastDelegate_f1">Blast.BlastDelegate_f1</a></td><td /></tr><tr><td>![Public delegate](media/pubdelegate.gif "Public delegate")</td><td><a href="T_NSS_Blast_Blast_BlastDelegate_f2">Blast.BlastDelegate_f2</a></td><td /></tr><tr><td>![Public delegate](media/pubdelegate.gif "Public delegate")</td><td><a href="T_NSS_Blast_Blast_BlastDelegate_f3">Blast.BlastDelegate_f3</a></td><td /></tr><tr><td>![Public delegate](media/pubdelegate.gif "Public delegate")</td><td><a href="T_NSS_Blast_Blast_BlastDelegate_f4">Blast.BlastDelegate_f4</a></td><td /></tr><tr><td>![Public delegate](media/pubdelegate.gif "Public delegate")</td><td><a href="T_NSS_Blast_Blast_BlastDelegate_f5">Blast.BlastDelegate_f5</a></td><td /></tr><tr><td>![Public delegate](media/pubdelegate.gif "Public delegate")</td><td><a href="T_NSS_Blast_Blast_BlastDelegate_f6">Blast.BlastDelegate_f6</a></td><td /></tr><tr><td>![Public delegate](media/pubdelegate.gif "Public delegate")</td><td><a href="T_NSS_Blast_Blast_BlastDelegate_f7">Blast.BlastDelegate_f7</a></td><td /></tr><tr><td>![Public delegate](media/pubdelegate.gif "Public delegate")</td><td><a href="T_NSS_Blast_Blast_BlastDelegate_f8">Blast.BlastDelegate_f8</a></td><td /></tr><tr><td>![Public delegate](media/pubdelegate.gif "Public delegate")</td><td><a href="T_NSS_Blast_Blast_BlastExecute">Blast.BlastExecute</a></td><td>
delegate used to execute scripts</td></tr></table>

## Enumerations
&nbsp;<table><tr><th></th><th>Enumeration</th><th>Description</th></tr><tr><td>![Public enumeration](media/pubenumeration.gif "Public enumeration")</td><td><a href="T_NSS_Blast_blast_operation">blast_operation</a></td><td>
instruction set</td></tr><tr><td>![Public enumeration](media/pubenumeration.gif "Public enumeration")</td><td><a href="T_NSS_Blast_BlastCompilerStageType">BlastCompilerStageType</a></td><td>
the types of compiler stages that run in sequence to produce the output</td></tr><tr><td>![Public enumeration](media/pubenumeration.gif "Public enumeration")</td><td><a href="T_NSS_Blast_BlastError">BlastError</a></td><td>
Errorcodes that can be returned by blast</td></tr><tr><td>![Public enumeration](media/pubenumeration.gif "Public enumeration")</td><td><a href="T_NSS_Blast_BlastLanguageVersion">BlastLanguageVersion</a></td><td>
the different compiler outputs / language targets</td></tr><tr><td>![Public enumeration](media/pubenumeration.gif "Public enumeration")</td><td><a href="T_NSS_Blast_BlastPackageFlags">BlastPackageFlags</a></td><td>
Package Flags</td></tr><tr><td>![Public enumeration](media/pubenumeration.gif "Public enumeration")</td><td><a href="T_NSS_Blast_BlastPackageMode">BlastPackageMode</a></td><td>
Blast script packaging types</td></tr><tr><td>![Public enumeration](media/pubenumeration.gif "Public enumeration")</td><td><a href="T_NSS_Blast_BlastRuntimeVersions">BlastRuntimeVersions</a></td><td>
Versions of blast present in build</td></tr><tr><td>![Public enumeration](media/pubenumeration.gif "Public enumeration")</td><td><a href="T_NSS_Blast_BlastScriptFunctionFlag">BlastScriptFunctionFlag</a></td><td>
flags for extra information on script functions</td></tr><tr><td>![Public enumeration](media/pubenumeration.gif "Public enumeration")</td><td><a href="T_NSS_Blast_BlastScriptToken">BlastScriptToken</a></td><td /></tr><tr><td>![Public enumeration](media/pubenumeration.gif "Public enumeration")</td><td><a href="T_NSS_Blast_BlastVariableDataType">BlastVariableDataType</a></td><td>
supported variable data types</td></tr><tr><td>![Public enumeration](media/pubenumeration.gif "Public enumeration")</td><td><a href="T_NSS_Blast_BlastVectorSizes">BlastVectorSizes</a></td><td>
supported vectorsizes</td></tr><tr><td>![Public enumeration](media/pubenumeration.gif "Public enumeration")</td><td><a href="T_NSS_Blast_BlastVectorTypes">BlastVectorTypes</a></td><td>
supported vectortypes</td></tr><tr><td>![Public enumeration](media/pubenumeration.gif "Public enumeration")</td><td><a href="T_NSS_Blast_extended_blast_operation">extended_blast_operation</a></td><td /></tr><tr><td>![Public enumeration](media/pubenumeration.gif "Public enumeration")</td><td><a href="T_NSS_Blast_ReservedBlastScriptFunctionIds">ReservedBlastScriptFunctionIds</a></td><td>
IDs reserved for functions used in blast, these get registered to blast with a name (that doesnt need to be equal)</td></tr></table>&nbsp;
