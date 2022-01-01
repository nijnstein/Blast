# Blast


## Tokens

Arithmetic operators:   `+ - / * =`

Boolean operators:      `& | ^ !` 

Boolean evaluators:     `< > <= >= = ! !=`

Ternary operator:       `[condition] ? [true] : [false]` 

Assignment:             `=`


Enclosures:             `( )`
Statement terminator:   `;` 
Value seperator:        `,` 
Decimal seperator:      `.` 

Indexer                 `.` 
IndexOpen               `[`
IndexClose              `]`

Identifier: 	          `[a..z][0..9|a..z]*[.|[][a..z][0..9|a..z]*[]]`
Control Flow:           `if then else` 
                        `while for ` 
                        `switch case default`

## Code Structure

The # is used to define constants, input and output(s) and validations. Any sequence not matching `#[define/input/output/validate]` is considered a comment. 

- A `#[definition]` must appear at the start of the line 
- Defines and inputs must appear in the code BEFORE any other code 
- A comment can be started at any point in the line

### Use of #
``` 
#define   variable  [value]
#input    variable  [offset] [bytesize]
#output   variable  [offset] [bytesize]
#validate variable  [value] 
``` 

All other text that starts with # is a comment and may start at any point of a line, also after `#[definition]`:

``` 
#input    variable  [offset] [bytesize]    # comment 123
``` 
