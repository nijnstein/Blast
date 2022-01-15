namespace NSS.Blast
{

    /// <summary>
    /// instruction set 
    /// </summary>
    public enum blast_operation : byte
    {
        nop = 0,
        assign = 1,
        add = 2,
        substract = 3,
        divide = 4,
        multiply = 5,
        and = 6,
        or = 7,
        not = 8,
        xor = 9,

        greater = 10,
        greater_equals = 11,
        smaller = 12,
        smaller_equals,
        equals,
        not_equals,

        ret,         // return  
        yield,       // yield execution back to caller for n frame s

        begin,
        end,

        jz,           // conditional jump
        jnz,
        jump,         // unconditional forward jump

        jump_back,    // unconditional backward jump 

        push,         // stack functions
        pop,

        pushv,

        peek,
        peekv,        // == 31   

        pushf,        // push a function's result 

        //------------------------

        fma,          // fused multiply add

        adda,         // add all operands together until [nop]
        mula,         // add all operands together until [nop]
        diva,         // divide all operands by eachother unitl [nop]
        suba,         // substract all operands from eachtoer until [nop]

        all,          // math.all => a | b | c | (d ^ e)
        any,          // math.any

        abs,
        select,       // ternary / math.select      TODO 

        random,       // 0..1
        seed,         // set seed for random 

        max,
        min,
        maxa,
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

        sqrt,


        undefined_rsqrt,
        undefined_pow,

        undefined_232,
        undefined3,
        undefined4,
        
        undefined7, //distance,
        undefined8,// distancesq,
        undefined9,// length,
        undefined10, //lengthsq,
        undefined_pop2,     // UNDEFINED ???
        
        undefined_pop3,     // UNDEFINED ???
        undefined_pop4,     // UNDEFINED ???
        undefined3431,
        undefined1,

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