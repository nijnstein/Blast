using System;

namespace NSS.Blast
{

    /// <summary>
    /// instruction set bs1
    /// </summary>
    public enum blast_operation : byte
    {
        /// <summary>
        /// signals end of a stream
        /// </summary>
        nop = 0,

        /// <summary>
        /// starts reading a stream of opcodes
        /// </summary>
        assign = 1,
                /// <summary>
        /// assign single variable / pop to assignee
        /// </summary>
        assigns,

        /// <summary>
        /// assign function result to assignee
        /// </summary>
        assignf, 

        /// <summary>
        /// assign result of external function call 
        /// </summary>
        assignfe,

        /// <summary>
        /// assign negated function result to assignee
        /// </summary>
        assignfn,

        /// <summary>
        /// assign negated result of external function call to assignee
        /// </summary>
        assignfen,

        /// <summary>
        /// assign a single vector, assignv knows the size of the parameter it writes to so it doesnt need a datapoint for vectorsize 
        /// </summary>
        assignv,

        //
        // Operations from add until not_equals MUST be in sequence for quick matches
        //

        /// <summary>
        /// add 2 operands
        /// </summary>
        add,

        /// <summary>
        /// substract 2 operands, doubles as negative sign 
        /// </summary>
        substract,

        /// <summary>
        /// divide 2 operands
        /// </summary>
        divide,

        /// <summary>
        /// multiply 2 operands 
        /// </summary>
        multiply,

        /// <summary>
        /// boolean and operation
        /// </summary>
        and,

        /// <summary>
        /// boolean or operation
        /// </summary>
        or,

        /// <summary>
        /// boolean not 
        /// </summary>
        not,

        /// <summary>
        /// boolean xor
        /// </summary>
        xor,

        /// <summary>
        /// boolean greater >
        /// </summary>
        greater,

        /// <summary>
        /// boolean greater equals >=
        /// </summary>
        greater_equals,

        /// <summary>
        /// boolean smaller then 
        /// </summary>
        smaller,

        /// <summary>
        /// boolean smaller equals
        /// </summary>
        smaller_equals,

        /// <summary>
        /// boolean equals: = 
        /// </summary>
        equals,

        /// <summary>
        /// boolean inequality: != 
        /// </summary>
        not_equals,

        /// <summary>
        /// return -> terminate script
        /// </summary>
        ret,         // return  

        /// <summary>
        /// Yield script, writing state to stack, must have packaged stack data
        /// </summary>
        yield,       // yield execution back to caller for n frame s

        /// <summary>
        /// begin a compounded sequence
        /// </summary>
        [Obsolete]
        begin,

        /// <summary>
        /// end a compounded sequence
        /// </summary>
        [Obsolete]
        end,

        /// <summary>
        /// jump if zero
        /// </summary>
        jz,           // conditional jump

        /// <summary>
        /// jump if not zero
        /// </summary>
        jnz,

        /// <summary>
        /// unconditional jump forward
        /// </summary>
        jump,         // unconditional forward jump

        /// <summary>
        /// unconditional jump backward
        /// </summary>
        jump_back,    // unconditional backward jump 

        /// <summary>
        /// push a value onto the stack
        /// </summary>
        push,

        /// <summary>
        /// pop a value from the stack
        /// </summary>
        pop,

        /// <summary>
        /// Push a vector to the stack
        /// </summary>
        pushv,

        /// <summary>
        /// push the result of a function onto the stack 
        /// </summary>
        pushf,        // push a function's result 

        /// <summary>
        /// push the result of a sequence onto the stack 
        /// </summary>
        pushc,        // push result of a compound 

        /// <summary>
        /// peek stack data 
        /// </summary>
        peek,  

        /// <summary>
        /// peek vector data 
        /// </summary>
        [Obsolete]
        peekv,        

        /// <summary>
        /// Multiply add: a = m1 * m2 + a1
        /// </summary>
        fma,          // fused multiply add => variants ??        could bit encode op sequences.. +-+- etc.  it would save on control code 

        /// <summary>
        /// add all operands in sequence
        /// </summary>
        adda,        

        /// <summary>
        /// multiply all operands in sequence
        /// </summary>
        mula,

        /// <summary>
        /// divide all operands by eachother in sequence 
        /// </summary>
        diva,        

        /// <summary>
        /// substract all operands from eachother in sequence
        /// </summary>
        suba,

        /// <summary>
        /// returns true if all arguments are true
        /// </summary>
        all,
        
        /// <summary>
        /// returns true if any argument is true 
        /// </summary>
        any,        

        /// <summary>
        /// return absolute value of operand
        /// </summary>
        abs,

        /// <summary>
        /// select instruction 
        /// </summary>
        [Obsolete]        
        select,   

        /// <summary>
        /// generate a random number 
        /// </summary>
        random,      

        /// <summary>
        /// seed the random number generator 
        /// </summary>
        seed,       

        /// <summary>
        /// get max value from operands, returns vector of same size as inputs 
        /// </summary>
        max,          

        /// <summary>
        /// get min value from operands, returns vector of same size as inputs 
        /// </summary>
        min,

        /// <summary>
        /// get max argument from operands returns size 1 vector
        /// </summary>
        maxa,

        /// <summary>
        /// get min argument from operands returns size 1 vector
        /// </summary>
        mina,

        lerp,
        slerp,
        nlerp,

        saturate,         //   return clamp(x, new float3(0.0f), new float3(1.0f));
        clamp,
        normalize,

        ceil,
        floor,
        frac,

        sin,
        cos,
        tan,
        atan,
        cosh,
        sinh,

        degrees,
        radians,

        undefined33,
        undefined3,         
        undefined4,    
        undefined5, 
        undefined6, 
        undefined7, 
        undefined8,
        undefined9,

        //------------------------
        // constant math values start here at PI
        pi,       // = 77
        inv_pi,

        // The difference between 1.0f and the next representable f32/single precision number.
        epsilon,

        // positive infinity value
        infinity,
        negative_infinity,
        nan,

        // smallest positive value for datatype
        min_value,

        //------------------------
        // common values
        value_0,
        value_1,
        value_2,
        value_3,
        value_4,
        value_8,
        value_10,
        value_16,
        value_24,
        value_32,
        value_64,
        value_100,
        value_128,
        value_256,
        value_512,
        value_1000,
        value_1024,

        value_30,
        value_45,
        value_90,
        value_180,
        value_270,
        value_360,
        //-----------------------
        // inverses of commons 
        inv_value_2,
        inv_value_3,
        inv_value_4,
        inv_value_8,
        inv_value_10,
        inv_value_16,
        inv_value_24,
        inv_value_32,
        inv_value_64,
        inv_value_100,
        inv_value_128,
        inv_value_256,
        inv_value_512,
        inv_value_1000,
        inv_value_1024,

        inv_value_30,
        inv_value_45,
        inv_value_90,
        inv_value_180,
        inv_value_270,
        inv_value_360 = 127,

        //------------------------
        id = 128,             // parameter id's start from here up until 254

        ex_op = 255           // extend into a multi token instruction  
    }

    public enum extended_blast_operation : byte
    {
        nop = 0,

        exp,
        exp10,
        log10,
        logn,
        log2,
        cross,
        dot,
        sqrt,
        rsqrt,
        pow,

        debugstack = 252,
        debug = 253,
        call = 254, // call external
        ex = 255 // extend further if ever needed. 
    }


    public enum BlastScriptToken : byte
    {
        Nop,

        Add,
        Substract,
        Divide,
        Multiply,
        Equals,
#if SUPPORT_TERNARY
        Ternary,        // ?          
#endif
        TernaryOption,  // besides being part of the ternary it also is part of the case statement
        SmallerThen,
        GreaterThen,
        SmallerThenEquals,
        GreaterThenEquals,
        NotEquals,

        And,
        Or,
        Xor,
        Not,

        OpenParenthesis,
        CloseParenthesis,

        DotComma,
        Comma,

        Identifier,     // [a..z][0..9|a..z]*[.|[][a..z][0..9|a..z]*[]]

        Indexer,        // .        * using the indexer on a numeric will define its fractional part 
        IndexOpen,      // [
        IndexClose,     // ]

        If,
        Then,
        Else,

        While,
        For,

        Switch,
        Case,
        Default
    }

    /// <summary>
    /// definition of a token used in blast script 
    /// </summary>
    public class BlastScriptTokenDefinition
    {
        public BlastScriptToken Token { get; set; }
        public char Match { get; set; }

        public string Description { get; set; }
        public BlastScriptTokenDefinition(BlastScriptToken token, char match, string description)
        {
            Token = token;
            Match = match;
            Description = description;
        }
    }


}