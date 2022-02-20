### Sample 2

Demonstrates setting script inputs as an array and allows user to test its datasegment use | input mapping through inspector controls.


- A global blast instance is initialized through `Blast.Initialize();`
- Verbose|Silent log modes 
- A script is created from text and prepared for execution. The action of preparing the script compiles the text and extracts variable information. This only needs to be done when the script is updated.
- Inputs to the script are set from an array on index which is much more efficient then setting by name 

