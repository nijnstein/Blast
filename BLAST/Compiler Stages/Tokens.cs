namespace NSS.Blast
{

    /// <summary>
    /// instruction set 
    /// </summary>
    public enum script_op : byte
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

        pop2,     // UNDEFINED ???
        pop3,     // UNDEFINED ???
        pop4,     // UNDEFINED ???

        pushv,

        popv,

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

        distance,
        distancesq,
        length,
        lengthsq,

        dot,
        degrees,
        radians,

        sqrt,
        rsqrt,

        pow,

        exp,
        log2,








        undefined1,          
        undefined2,         

        //------------------------
        // constant math values start here at PI
        pi,
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
        id = 128,          // parameter id's start from here up until 254

        ex_op = 255           // extend into a multi token instruction  
    }

    public enum extended_script_op : byte
    {
        nop = 0,

        exp10,
        log10,
        logn,
        cross,

        debugstack = 243,
        debug = 244,
        call = 245, // call external
        ex = 255 // extend further if ever needed. 
    }

    /// <summary>
    /// todo        requires updates in pop or value... 
    /// </summary>
    public enum extended_script_constant : byte
    {
        none = 0,
        fixeddeltatime = 1,
        time = 2,
        framecount = 3,
        deltatime = 4
    }

    public enum BlastScriptToken : byte
    {
        Nop,

        Add,
        Substract,
        Divide,
        Multiply,
        Equals,

        Ternary,        // ?           todo
        TernaryOption,  // :           todo 

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

    public class BlastScriptTokenDefinition
    {
        public BlastScriptToken Token { get; set; }
        public char Match { get; set; }
        public BlastScriptTokenDefinition(BlastScriptToken token, char match)//, bool allow_leading_whitespace, bool allow_trailing_whitespace)
        {
            Token = token;
            Match = match;
        }
    }


}