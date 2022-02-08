//##########################################################################################################
// Copyright © 2022 Rob Lemmens | NijnStein Software <rob.lemmens.s31@gmail.com> All Rights Reserved  ^__^\#
// Unauthorized copying of this file, via any medium is strictly prohibited                           (oo)\#
// Proprietary and confidential                                                                       (__) #
//##########################################################################################################
using System;
using Unity.Mathematics;

namespace NSS.Blast
{
    /// <summary>
    /// Versions of blast present in build
    /// </summary>
    public enum BlastRuntimeVersions : int
    {
#if BS_BETA
        Beta = 0,
#endif 
        /// <summary>
        /// the final version 1 = release 1
        /// </summary>
        Final1 = 1,

#if BS_EXPERIMENTAL
        Experimental = 2
#endif 
    }

    /// <summary>
    /// the different compiler outputs / language targets 
    /// </summary>
    public enum BlastLanguageVersion : int
    {
        /// <summary>
        /// unknown language version
        /// </summary>
        None = 0,

        /// <summary>
        /// BLAST Script, default script / bytecode 
        /// </summary>
        BS1 = 1,

        /// <summary>
        /// BLAST Single Script Multiple Data
        /// </summary>
        BSSMD1 = 2,

        /// <summary>
        /// Burstable C# code packed in functions for burst to compile at designtime
        /// </summary>
        HPC = 3,

        /// <summary>
        /// Runtime compiled c# code, .net framework and windows only 
        /// </summary>
        CS = 4

    }

    /// <summary>
    /// supported vectorsizes 
    /// </summary>
    public enum BlastVectorSizes : byte
    {
        /// <summary>
        /// size 1: 4 bytes
        /// </summary>
        float1 = 1,
        /// <summary>
        /// size 2: 8 bytes
        /// </summary>
        float2 = 2,
        /// <summary>
        /// size 3: 12 bytes
        /// </summary>
        float3 = 3,
        /// <summary>
        /// size 4: 16 bytes
        /// </summary>
        float4 = 4
    }

    /// <summary>
    /// supported variable data types 
    /// </summary>
    public enum BlastVariableDataType : byte
    {
        /// <summary>
        /// float datatype
        /// </summary>
        Numeric = 0,

        /// <summary>
        /// double datatype 
        /// </summary>
        Numeric64 = 1,

        /// <summary>
        /// integer datatype 
        /// </summary>
        ID = 2,

        /// <summary>
        /// long datatype 
        /// </summary>
        ID64 = 3
    }

    /// <summary>
    /// when encoding xxx xxy etc:
    /// 
    /// byte: 00112233
    /// 
    /// </summary>
    public enum vector_index : byte
    {
        x = 0, y = 1, z = 2, w = 3
    }

    /// <summary>
    /// combination of vectorsize and datatype 
    /// </summary>
    public enum floatindex : byte
    {
       float4 = 0, // float 4 base 
       float1 = 1, // float 1 base 
       float2 = 2, // float 2 base 
       float3 = 3, // float 3 base 

       float4x, float4y, float4z, float4w,
       float3x, float3y, float3w,
       float2x, float2y
    }



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
        begin,

        /// <summary>
        /// end a compounded sequence
        /// </summary>
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
        /// jump short value (+- 32K), short is encoded in net 2 bytes 
        /// </summary>
        long_jump, 

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

        /// <summary>
        /// floating point modulus operation 
        /// </summary>
        fmod,

        /// <summary>
        /// component sum of vector 
        /// </summary>
        csum,

        /// <summary>
        /// truncate float into integer part only 
        /// </summary>
        trunc,

        /// <summary>
        /// index the x component of a vector
        /// </summary>
        index_x,

        /// <summary>
        /// index the y component of a vector
        /// </summary>
        index_y,

        /// <summary>
        /// index the z component of a vector
        /// </summary>
        index_z,
        
        /// <summary>
        /// index the w component of a vector 
        /// </summary>
        index_w,

        /// <summary>
        /// index the vector of n size with an m size indexer: x, xy, zzz etc. 
        /// </summary>
        index_n,

        reserved5,
        reserved6,
        reserved7,
        reserved8,
        reserved9,
        reserved0,
        reserved10,
        reserved11,
        reserved12,
        reserved13,
        reserved14,
        reserved15,
        reserved16,
        reserved17,
        reserved18,
        reserved19,
        reserved20,
 



        //------------------------

        /// <summary>
        /// PI
        /// </summary>
        pi,       // = 77

        /// <summary>
        /// Inverse of PI 
        /// </summary>
        inv_pi,

        /// <summary>
        /// The difference between 1.0f and the next representable f32/single precision number.
        /// </summary>
        epsilon,

        /// <summary>
        /// positive infinity value 
        /// </summary>
        infinity,

        /// <summary>
        /// negative infinity value
        /// </summary>
        negative_infinity,

        /// <summary>
        /// Not a number 
        /// </summary>
        nan,

        /// <summary>
        /// smallest positive value for datatype
        /// </summary>
        min_value,

        //------------------------
        // common values

        /// <summary>
        /// constant 0
        /// </summary>
        value_0,

        /// <summary>
        /// constant 1
        /// </summary>
        value_1,

        /// <summary>
        /// constant 2
        /// </summary>
        value_2,

        /// <summary>
        /// constant 3
        /// </summary>
        value_3,

        /// <summary>
        /// constant 4 
        /// </summary>
        value_4,

        /// <summary>
        /// constant 8
        /// </summary>
        value_8,

        /// <summary>
        /// constant 10
        /// </summary>
        value_10,

        /// <summary>
        /// constant 16
        /// </summary>
        value_16,

        /// <summary>
        /// constant 24
        /// </summary>
        value_24,

        /// <summary>
        /// constant 32
        /// </summary>
        value_32,

        /// <summary>
        /// constant 64
        /// </summary>
        value_64,

        /// <summary>
        /// constant 100
        /// </summary>
        value_100,

        /// <summary>
        /// constant 128
        /// </summary>
        value_128,
        
        /// <summary>
        /// constant 256
        /// </summary>
        value_256,
        
        /// <summary>
        /// constant 512
        /// </summary>
        value_512,

        /// <summary>
        /// constant 1000
        /// </summary>
        value_1000,

        /// <summary>
        /// constant 1024
        /// </summary>
        value_1024,

        /// <summary>
        /// constant 30
        /// </summary>
        value_30,

        /// <summary>
        /// constant 45
        /// </summary>
        value_45,

        /// <summary>
        /// constant 90
        /// </summary>
        value_90,

        /// <summary>
        /// constant 180
        /// </summary>
        value_180,

        /// <summary>
        /// constant 270
        /// </summary>
        value_270,

        /// <summary>
        /// constant 360
        /// </summary>
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


        sin,
        cos,
        tan,
        atan,
        atan2,
        cosh,
        sinh,

        degrees,
        radians,

        lerp,
        slerp,
        nlerp,
        saturate,         //   return clamp(x, new float3(0.0f), new float3(1.0f));
        clamp,
        normalize,
        ceil,
        floor,
        frac,


        /// <summary>
        /// remap e from range a-b to c-d  
        /// </summary>
        remap,

        /// <summary>
        /// normalize x to range a-b |  (x - a) / (b - a).
        /// </summary>
        unlerp,

        /// <summary>
        /// get log2 ceiling of value
        /// </summary>
        ceillog2,

        /// <summary>
        /// get log2 floor of value
        /// </summary>
        floorlog2,

        /// <summary>
        /// get next power of 2 of value
        /// </summary>
        ceilpow2,


        // <summary>
        // snap value to its closest interval
        // </summary>
        // <remarks>
        // // Snaps the value x to the current increment of step. Ex: snap(44, 5) results in 145.
        // <code>
        //     function snap(x, step) {
        //         return Math.round(x / step) * step;
        //     }
        //</code>
        //</remarks>
        //snap,



        // step, smoothstep? 


        /// <summary>
        /// log stack contents in debugstream
        /// </summary>
        debugstack = 252,

        /// <summary>
        /// log variable contents in debugstream 
        /// </summary>
        debug = 253,

        /// <summary>
        /// call an external function, opcode is followed by 4 byte id 
        /// TODO: update to also have a callsmall and call medium with 1 and 2 bytes for external function index? 
        /// </summary>
        call = 254, // call external
        /// <summary>
        /// extensions on extensions... better use another ex in blast_operation, save 1 byte if ever needed
        /// </summary>
        ex = 255 // extend further if ever needed. 
    }


    /// <summary>
    /// The tokens used in languageversions: BS1|BS2|BSSSDM1
    /// </summary>
    public enum BlastScriptToken : byte
    {
        /// <summary>
        /// nop instruction 
        /// </summary>
        Nop,

        /// <summary>
        /// add 2 operands together <c>a = a + b;</c>
        /// </summary>
        Add,

        /// <summary>
        /// substract 2 operands from eachother <c>a = a - b;</c>
        /// </summary>
        Substract,

        /// <summary>
        /// divide 2 operands: <c>a = a / b;</c>
        /// </summary>
        Divide,

        /// <summary>
        /// multiply 2 operands: <c>a = a * b;</c>
        /// </summary>
        Multiply,

        /// <summary>
        /// boolean equals, returns 1 for true, 0 for false; <c>a = a == b;</c>
        /// </summary>
        Equals,
#if SUPPORT_TERNARY
        Ternary,        // ?          
#endif
        /// <summary>
        /// de double colon ':', used in switch statements and ternary operations: 
        /// <remarks>
        /// <code>
        /// a = 1 ? 1 : 0; 
        /// switch(a) 
        /// (
        ///     case 1: 
        ///     (
        ///         b = 2;
        ///     )
        ///     default: 
        ///     (  
        ///         b = 3;
        ///     )
        /// )    
        /// </code>
        /// </remarks>
        /// </summary>
        TernaryOption,

        /// <summary>
        /// boolean comparison smaller then, not including
        /// </summary>
        SmallerThen,

        /// <summary>
        /// boolean comparison greater then, not including
        /// </summary>
        GreaterThen,

        /// <summary>
        /// boolean comparison smaller then, including
        /// </summary>
        SmallerThenEquals,

        /// <summary>
        /// boolean comparison greater then, including
        /// </summary>
        GreaterThenEquals,

        /// <summary>
        /// boolean comparison not equals 
        /// </summary>
        NotEquals,

        /// <summary>
        /// boolean AND operation, numeric 0 == false
        /// </summary>
        And,

        /// <summary>
        /// boolean OR operation, numeric 0 == false
        /// </summary>
        Or,

        /// <summary>
        /// boolean XOR operation, number 0 == false
        /// </summary>
        Xor,

        /// <summary>
        /// boolean NOT operation, number 0 == false 
        /// </summary>
        Not,

        /// <summary>
        /// opens a set of statement(s): a compound | sequence
        /// </summary>
        OpenParenthesis,

        /// <summary>
        /// closes a set of statement(s): a compound | sequence
        /// </summary>
        CloseParenthesis,

        /// <summary>
        /// statement terminator: ; 
        /// </summary>
        DotComma,

        /// <summary>
        /// parameter seperator: , 
        /// </summary>
        /// <remarks>
        /// Prevents parameters from growing into vectors or algorithmic sequences:
        /// 
        /// Define vector with 1 element minus 1. without the , the statement would be ambigious
        /// <code>
        /// a = (1, -1);  // a 2 vector 
        /// a = (1 - 1);  // a 1 vector | scalar
        /// </code>
        /// </remarks>
        Comma,

        /// <summary>
        /// a general identifier: values functions keywords 
        /// </summary>
        Identifier,     // [a..z][0..9|a..z]*[.|[][a..z][0..9|a..z]*[]]

        /// <summary>
        /// a decimal seperator doubling as an indexer: .  indexing is not yet supported
        /// </summary>
        Indexer,        // .        * using the indexer on a numeric will define its fractional part 

        /// <summary>
        /// an index opener [, not yet supported
        /// </summary>
        IndexOpen,      // [

        /// <summary>
        /// an index closer [, not yet supported
        /// </summary>
        IndexClose,     // ]

        /// <summary>
        /// the if condition of an if then else statement clause
        /// </summary>
        If,

        /// <summary>
        /// the then compound of an if then else clause
        /// </summary>
        Then,
        
        /// <summary>
        /// the else compound of an if then else clause
        /// </summary>
        Else,

        /// <summary>
        /// a while loop 
        /// </summary>
        While,

        /// <summary>
        /// a for loop 
        /// </summary>
        /// <remarks>
        /// for loops are always transformed into while loops in the AST tree, this wil help later
        /// 
        /// <c>for (i = 0; i < 16; i++) ( statements )</c>
        /// 
        /// </remarks>
        For,

        /// <summary>
        /// the switch statement, there must be at least 1 case or 1 default statement. 
        /// </summary>
        /// <remarks>
        /// <code>
        /// a = 1 ? 1 : 0; 
        /// switch(a) 
        /// (
        ///     case 1: 
        ///     (
        ///         b = 2;
        ///     )
        ///     default: 
        ///     (  
        ///         b = 3;
        ///     )
        /// )    
        /// </code>
        /// </remarks>
        Switch,

        /// <summary>
        /// the case statement, the condition in switch is matched to the value in case, if they match that path is taken
        /// </summary>
        /// <remarks>
        /// <code>
        /// a = 1 ? 1 : 0; 
        /// switch(a) 
        /// (
        ///     case 1: 
        ///     (
        ///         b = 2;
        ///     )
        ///     default: 
        ///     (  
        ///         b = 3;
        ///     )
        /// )    
        /// </code>
        /// </remarks>
        Case,

        /// <summary>
        /// the default case, part of the switch statement and chosen when no other case applies 
        /// </summary>
        /// <remarks>
        /// <code>
        /// a = 1 ? 1 : 0; 
        /// switch(a) 
        /// (
        ///     case 1: 
        ///     (
        ///         b = 2;
        ///     )
        ///     default: 
        ///     (  
        ///         b = 3;
        ///     )
        /// )    
        /// </code>
        /// </remarks>
        Default,

        /// <summary>
        /// function defining a value  (or does nothing to registers and is actually a procedure) 
        /// </summary>
        /// <remarks>
        /// 
        /// </remarks>
        Function

    }

    /// <summary>
    /// definition of a token used in blast script 
    /// </summary>
    /// <remarks>
    /// A token definition is used to provide more information on tokens, mainly for debugging, but also for ease
    /// of possible future extensions 
    /// </remarks>
    public class BlastScriptTokenDefinition
    {
        /// <summary>
        /// the token being defined
        /// </summary>
        public BlastScriptToken Token { get; set; }

        /// <summary>
        /// the character to match to 
        /// </summary>
        public char Match { get; set; }

        /// <summary>
        /// description of token 
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// token constructor 
        /// </summary>
        /// <param name="token">token id</param>
        /// <param name="match">char to match</param>
        /// <param name="description">description of the token</param>
        public BlastScriptTokenDefinition(BlastScriptToken token, char match, string description)
        {
            Token = token;
            Match = match;
            Description = description;
        }
    }

    namespace Compiler
    {
        /// <summary>
        /// jump label types, used for reference during compilation 
        /// </summary>
        internal enum JumpLabelType
        {
            Jump,
            Offset,
            Label
        }



        /// <summary>
        /// a jump label, for help keeping track of jump targets 
        /// </summary>
        public class IMJumpLabel
        {
            internal string id { get; private set; }
            internal JumpLabelType type { get; private set; }
            internal bool is_label => type == JumpLabelType.Label;
            internal bool is_offset => type == JumpLabelType.Offset;
            internal bool is_jump => type == JumpLabelType.Jump;
            internal IMJumpLabel(string _id, JumpLabelType _type)
            {
                id = _id;
                type = _type;
            }

            static internal IMJumpLabel Jump(string _id)
            {
                return new IMJumpLabel(_id, JumpLabelType.Jump);
            }
            static internal IMJumpLabel Label(string _id)
            {
                return new IMJumpLabel(_id, JumpLabelType.Label);
            }
            static internal IMJumpLabel Offset(string _id)
            {
                return new IMJumpLabel(_id, JumpLabelType.Offset);
            }

            /// <summary>
            /// tostring override
            /// </summary>
            public override string ToString()
            {
                switch (type)
                {
                    case JumpLabelType.Offset: return $"offset: {id}";
                    case JumpLabelType.Jump: return $"jump to: {id}";

                    default:
                    case JumpLabelType.Label: return $"label: {id}";
                }
            }
        }
    }


    /// <summary>
    /// Errorcodes that can be returned by blast 
    /// </summary>
    public enum BlastError : int
    {
        /// <summary>
        /// execution returned back to caller 
        /// </summary>
        yield = 1,
        /// <summary>
        /// everything went OK
        /// </summary>
        success = 0,
        /// <summary>
        /// a generic error occured 
        /// </summary>
        error = -1,
        /// <summary>
        /// an unknown operation was found
        /// </summary>
        error_unknown_op = -2,
        /// <summary>
        /// a vector is assigned to a variable of differnt vectorsize and no automatic conversion is possible or implemented
        /// </summary>
        error_assign_vector_size_mismatch = -3,
        /// <summary>
        /// a begin-end opcode sequence is found in root while that should not be possible
        /// </summary>
        error_begin_end_sequence_in_root = -4,
        /// <summary>
        /// an operation is encountered that is not supported while executing root level nodes
        /// </summary>
        error_unsupported_operation_in_root = -5,
        /// <summary>
        /// a variable vector operation is not supported by the current sequence of operations
        /// </summary>
        error_variable_vector_op_not_supported = -6,
        /// <summary>
        /// failed to update a vector
        /// </summary>
        error_update_vector_fail = -7,
        /// <summary>
        /// error peeking stack information 
        /// </summary>
        stack_error_peek = -8,
        /// <summary>
        /// error taking values from stack
        /// </summary>
        stack_error_pop = -9,
        /// <summary>
        /// a variable sized pop is not supported
        /// </summary>
        stack_error_variable_pop_not_supported = -10,
        /// <summary>
        /// a pop instruction is found where it doesnt belong 
        /// </summary>
        stack_error_pop_multi_from_root = -11,
        /// <summary>
        /// a variably sized compound is not supported in the current sequence of operations 
        /// </summary>
        error_variable_vector_compound_not_supported = -12,
        /// <summary>
        /// the interpretor reached the maximum number of iterations, this is an indication of scripts running in an endless loop
        /// </summary>
        error_max_iterations = -13,
        /// <summary>
        /// the allocated space for the stack is too small (to few stack memory locations)
        /// </summary>
        validate_error_stack_too_small = -14,
        /// <summary>
        /// the given script id could not be found
        /// </summary>
        script_id_not_found = -15,
        /// <summary>
        /// blast engine data is not initialized (use: Blast.Create()         
        /// </summary>
        error_blast_not_initialized = -16,
        /// <summary>
        /// the given script id is invalid in the current context 
        /// </summary>
        error_invalid_script_id = -17,
        /// <summary>
        /// the c-sharp compiler is only supported in .Net Framework builds on windows. 
        /// </summary>
        error_cs_compiler_not_supported_on_platform = -18,
        /// <summary>
        /// the node type is invalid in the current context 
        /// </summary>
        error_invalid_nodetype = -19,
        /// <summary>
        /// the operation is not supported in the current context 
        /// </summary>
        error_unsupported_operation = -20,
        /// <summary>
        /// the node has not been sufficiently flattened for execution 
        /// </summary>
        error_node_not_flat = -21,
        /// <summary>
        /// 1 or more function parameters failed to be compiled 
        /// </summary>
        error_failed_to_compile_function_parameters = -22,
        /// <summary>
        /// a function is used with too many parameters 
        /// </summary>
        error_too_many_parameters = -23,
        /// <summary>
        /// failed to translate a dataoffset into a variable index, the bytecode uses offsets instead of id;s voiding the need for some checks
        /// </summary>
        error_failed_to_translate_offset_into_index = -24,
        /// <summary>
        /// the given vectorsize is not supported by the target operation 
        /// </summary>
        error_vector_size_not_supported = -25,
        /// <summary>
        /// the datasegment is too large to be mapped to the target buffer 
        /// </summary>
        datasegment_size_larger_then_target = -26,
        /// <summary>
        /// the compiler failed to package the compiled bytecode 
        /// </summary>
        compile_packaging_error = -27,
        /// <summary>
        /// invalid packagemode set in package for ssmd execution
        /// - packages need to be compiled in SSMD mode for SSMD interpretation (it can still run normal interpretation on ssmd packages though)
        /// </summary>
        ssmd_invalid_packagemode = -28,
        /// <summary>
        /// the current operation is not supported at the root level during ssmd interpretation
        /// </summary>
        error_unsupported_operation_in_ssmd_root = -29,
        /// <summary>
        /// the ssmd interpretor expected a value but received something else
        /// </summary>
        ssmd_error_expecting_value = -30,
        /// <summary>
        /// the current vectorsize is not supported in the current sequence of operations
        /// </summary>
        ssmd_error_unsupported_vector_size = -31,
        /// <summary>
        /// package not correctly set to interpretor, use interpretor.SetPackage();
        /// </summary>
        error_execute_package_not_correctly_set = -32,
        /// <summary>
        /// the ast nodetype is not allowed in the root, this indicates compiler errors 
        /// </summary>
        error_invalid_nodetype_in_root = -33,
        /// <summary>
        /// the ast node encodes an unknown function 
        /// </summary>
        error_node_function_unknown = -34,
        /// <summary>
        /// the flatten operation failed on parameters of the target function node
        /// </summary>
        error_failed_to_flatten_function_parameters = -35,
        /// <summary>
        /// the currentvector size is not supported to be pushed to the stack 
        /// </summary>
        error_pushv_vector_size_not_supported = -36,
        /// <summary>
        /// the optimizer failed to match operations 
        /// </summary>
        error_optimizer_operation_mismatch = -37,
        /// <summary>
        /// the optimizer failed to match parameters
        /// </summary>
        error_optimizer_parameter_mismatch = -38,
        /// <summary>
        /// the optimizer failed to replace a sequence it targetted to optimize
        /// </summary>
        error_optimizer_failed_to_replace_sequence = -39,
        /// <summary>
        /// the given package doesnt have any allocated code or data segments 
        /// </summary>
        error_package_not_allocated = -40,
        /// <summary>
        /// there is nothing to execute
        /// </summary>
        error_nothing_to_execute = -41,
        /// <summary>
        /// the packagemode is not supported in the given execution method
        /// </summary>
        error_packagemode_not_supported_for_direct_execution = -42,
        /// <summary>
        /// package alread created, usually means that 'Prepare' is called while the script is already prepared 
        /// </summary>
        error_already_packaged = -43,
        /// <summary>
        /// generic error during compilation of the script, the log could contain more data depending on compilation options
        /// </summary>
        error_compilation_failure = -44,
        /// <summary>
        /// language version not supported in given context
        /// </summary>
        error_language_version_not_supported = -45,
        /// <summary>
        /// invalid operation in ssmd sequence
        /// </summary>
        ssmd_invalid_operation = -46,
        /// <summary>
        /// function not handled in ssmd operation 
        /// </summary>
        ssmd_function_not_handled = -47,
        /// <summary>
        /// error in cleanup stage before compilation 
        /// </summary>
        error_pre_compile_cleanup = -48,
        /// <summary>
        /// error in analyzer
        /// </summary>
        error_analyzer = -49,
        /// <summary>
        /// analyzer failed to determine all parameter types, vectorsizes and reference counts
        /// </summary>
        error_mapping_parameters = -50,
        /// <summary>
        /// an attempt was made to register a function with the scriptapi that already exists 
        /// </summary>
        error_scriptapi_function_already_exists = -51,
        /// <summary>
        /// an attempt was made to use a function that is not known to the script api registry
        /// </summary>
        error_scriptapi_function_not_registered = -52,
        /// <summary>
        /// syncbuffer growing to deep, either reduce nesting or increase sync buffer depth
        /// </summary>
        error_ssmd_sync_buffer_to_deep = -53,           
        /// <summary>
        /// inline function definition has a malformed parameter list 
        /// </summary>
        error_inlinefunction_parameter_list_syntax = -54,
        /// <summary>
        /// inline function definition has a malformed body compound
        /// </summary>
        error_inlinefunction_body_syntax = -55,
        /// <summary>
        /// inline function declaration is malformed (probably malformed function name)
        /// </summary>
        error_inlinefunction_declaration_syntax = -57,
        /// <summary>
        /// inline function already exists
        /// </summary>
        error_inlinefunction_already_exists = -58,
        /// <summary>
        /// inline function definition doesnt exist/couldnt be found
        /// </summary>
        error_inlinefunction_doenst_exist = -59,
        /// <summary>
        /// error re-mapping parameter list during inlining of function 
        /// </summary>
        error_inlinefunction_parameters = -60,
        /// <summary>
        /// error: inlined functions may not allocate memory/declare variables 
        /// </summary>
        error_inlinefunction_may_not_declare_variables = -61,
        /// <summary>
        /// error failed to remap identifiers in inlined function segment  
        /// </summary>
        error_inlinefunction_failed_to_remap_identifiers = -62,
        /// <summary>
        /// error transforming node indexers 
        /// </summary>
        error_indexer_transform = -63,
        /// <summary>
        /// error in assign, cannot set a vector component from a multicomponent vector
        /// </summary>
        error_assign_component_from_vector = -64,
        /// <summary>
        /// error in input define, the supplied typename could not be matched with any supported type 
        /// </summary>
        error_input_type_invalid = -65,
        /// <summary>
        /// error in input define, failed to create variable for input define
        /// </summary>
        error_input_failed_to_create = -66,
        /// <summary>
        /// error in output define, datatype mismatched 
        /// </summary>
        error_output_datatype_mismatch = -67,
        /// <summary>
        /// error in input or output define, the default set could not be interpreted into the correct datatype
        /// </summary>
        error_input_ouput_invalid_default = -68,
    }

}
