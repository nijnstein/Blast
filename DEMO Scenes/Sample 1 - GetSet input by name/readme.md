### Sample 1

Demonstrates setting script inputs and reading them back for use elsewhere. 

- A global blast instance is initialized through `Blast.Initialize();`
- A script is created from text and prepared for execution. The action of preparing the script compiles the text and extracts variable information. This only needs to be done when the script is updated.
- Inputs to the script can set through an index property on the BlastScript object. 

``` 
#input variablename float3

#input variablename float3 0 0 0       # defined with default value 0

``` 

- Demonstrates the use of vectors in the script, any list of values encloses in parenthesis that is not a function parameterlist is considered a vector definition. 
``` 
 defines a vector: a = (1 1 1);

 in case of ambiguity with an operation use comma's to seperate vector components: a = (1, -1 1); 

``` 
