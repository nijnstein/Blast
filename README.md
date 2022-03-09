# BLAST
#### See https://github.com/nijnstein/BLAST-Documentation for documentation


- re test all functions in ssmd especially constants
- any function parameter can nest its indexer and minus sign inline, compiler can then output:
  ` function(  -idx_x(f4) ) ` instead of ` push sequence - idx_x(f4); function(pop) ` 
- allow statements to omit ; when possible 

- manual

- RELEASE V1
 
- Long jumps
- Long constant references

#### V2 | V3:

- ID datatypes
- SSMD control flow support
- HPC transpiler
- Tree based optimizer
- Improved node optimizer with more patterns
