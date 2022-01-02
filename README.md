# Blast

### Goal 

Provide a fast scripting language for running statemachines or other decision making processes from bursted code in unity.

- Limited datatype support: Numerics and IDs 
- Small memory footprint 
- Code & Data seperated 
- Integrates with DOTS
- Uses BURST function pointers to connect to basic functionality in its environment. 

## Data types 

There is limited datatype support: 

```Unknown: determined at runtime 

Numeric: 1.0 etc

ID: Integers

Vectors:  ID(n) and NUMERIC(n) up until n = 16, 
          id(3)       => (1 2 3)
          NUMERIC(3)  =>  (1.2 2.3 2.45)
```
### NUMERIC  
The default datatype is the numeric, unless otherwise set the compiler assumes the numeric datatype which is a 32 bit float. 

### ID
The ID datatype is used to represent an ID, because numerics will not round correctly on indices with more then 6/7 digits the ID is used to correct index large lists.

### Vectors
Vectors may be defined based on all supported datatypes and functions, constant and functions may be mixed but the number of dataelements must be equal for each element defined in the vector. 

1> Defines a, a vector of 2 constants and 1 pop operation popping 1 numeric or id:
` a = (1 pop 2); `


2> Defines b, a vector of 2 constants of size 3 and a pop3:
` b = ((1 2 3) (pop pop pop) (1 2 3)); ` 

#### Vector mapping assumptions
Vectors map automatically to matrices of the same element size: n(9) => m(3x3), n(12) => m(3x4) | m(4x3) etc. 

## Tokens

Arithmetic operators:   `+ - / *`

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
