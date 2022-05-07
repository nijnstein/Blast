//############################################################################################################################
//                                                                                                                           #
//  ██████╗ ██╗      █████╗ ███████╗████████╗                           Copyright © 2022 Rob Lemmens | NijnStein Software    #
//  ██╔══██╗██║     ██╔══██╗██╔════╝╚══██╔══╝                                    <rob.lemmens.s31 gmail com>                 #
//  ██████╔╝██║     ███████║███████╗   ██║                                           All Rights Reserved                     #
//  ██╔══██╗██║     ██╔══██║╚════██║   ██║                                                                                   #
//  ██████╔╝███████╗██║  ██║███████║   ██║     V1.0.4e                                                                       #
//  ╚═════╝ ╚══════╝╚═╝  ╚═╝╚══════╝   ╚═╝                                                                                   #
//                                                                                                                           #
//############################################################################################################################
//                                                                                                                     (__)  #
//       Unauthorized copying of this file, via any medium is strictly prohibited proprietary and confidential         (oo)  #
//                                                                                                                     (__)  #
//############################################################################################################################
#pragma warning disable CS1591
#pragma warning disable CS0162

#if STANDALONE_VSBUILD
using NSS.Blast.Standalone;
#else
    using UnityEngine;
    using Unity.Burst.CompilerServices;
#endif


using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine.Assertions;

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
    /// vector types\sizes, encodes vectorsize and datatype in 1 enum 
    /// </summary>
    public enum BlastVectorSizes : byte
    {
        /// <summary>
        /// vectorsize|datatype NOT set 
        /// </summary>
        none = 0, 

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
        float4 = 4,

        /// <summary>
        /// bool32: 4 byte, 32bit bool value
        /// </summary>
        bool32 = 5,

        /// <summary>
        /// int32
        /// </summary>
        id1 = 6,

        /// <summary>
        /// int2
        /// </summary>
        id2 = 7,

        /// <summary>
        /// int3
        /// </summary>
        id3 = 8,

        /// <summary>
        /// int 4
        /// </summary>
        id4 = 9,

        id64 = 10,
        half1 = 11,
        half2 = 12,
        half3 = 13,
        half4 = 14,
        ptr = 15,
    }

    /// <summary>
    /// not all functions use the same parameter type encoding when the parameterlist has a veriable size 
    /// </summary>
    public enum BlastParameterEncoding : byte 
    { 
        /// <summary>
        /// used for functions accepting only float1-4, supports 63 parameters 
        /// </summary>
        Encode62 = 0,  
        
        /// <summary>
        /// used for functions supporting full datatypes, supports 15 parameters 
        /// </summary>
        Encode44 = 1
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
        /// integer datatype 
        /// </summary>
        ID = 1,

        /// <summary>
        /// each data element is to be interpreted as 32 boolean flags 
        /// </summary>
        Bool32 = 2,

        /// <summary>
        /// constant data blob, unless typed on access it holds a blob or a float array 
        /// - the blob can be used for text:    #input msg cdata 'abcd'; alert(msg); 
        /// - or just float data:  #input msg cdata numeric 1 2 3 4 5 6 7; alert(msg); 
        /// - or other data:  #input msg cdata Bool32 1111..0 111..11 11...11; alert(msg); 
        /// </summary>
        CData = 3, 
        // max n = 7 // 3 bits 
    }

    public static class BlastVariableDataTypeExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsValidOnStack(this BlastVariableDataType datatype)
        {
            return (datatype == BlastVariableDataType.Numeric) || (datatype == BlastVariableDataType.ID) || (datatype == BlastVariableDataType.Bool32);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsValidOnStack(this BlastVariableDataType datatype, byte vectorsize)
        {
            switch (datatype)
            {
                case BlastVariableDataType.ID:
                case BlastVariableDataType.Bool32: 
                case BlastVariableDataType.Numeric: return vectorsize > 0 && vectorsize <= 4;
                case BlastVariableDataType.CData: return vectorsize == 1; 
            }
            return false;
        }
    }


    public static class BlastVectorTypeExtensions
    {
        /// <summary>
        /// combine datatype, vectorsize and negation into 1 bytesized enumeration, save 2 branches when going through these in nested switches 
        /// </summary>
        /// <param name="vectorsize">a vectorsize of 0 is not assumed to be 4, use the safe version if this is needed</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static BlastVectorType Combine(this BlastVariableDataType datatype, byte vectorsize, bool negate_or_not)
        {
#if TRACE
            if (vectorsize == 0) Debug.LogError("BlastVectorType.Combine: 0 vectorsize, if this is inteded to be an encoded size 4 use CombineSafe");
#endif

            return (BlastVectorType)(negate_or_not ? 0b1000_0000 : 0b0000_0000) + (byte)((byte)datatype << 4) + vectorsize;
        }


        /// <summary>
        /// deue to encoding vectorsize is sometimes 0 this actually means its size 4 (capped after 2 bits)
        /// this safe version of the combine function uses a select to make sure a vectorsize 0 is treated as size 4
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static BlastVectorType CombineSafe(this BlastVariableDataType datatype, byte vectorsize, bool negate_or_not)
        {
            return (BlastVectorType)(negate_or_not ? 0b1000_0000 : 0b0000_0000) + (byte)((byte)datatype << 4) + (byte)math.select(vectorsize, 4, vectorsize == 0);
        }
    }


    public enum BlastVectorType : byte
    {
        unknown     = 0,

        float1      = ((byte)BlastVariableDataType.Numeric << 4) + 1,
        float2      = ((byte)BlastVariableDataType.Numeric << 4) + 2,
        float3      = ((byte)BlastVariableDataType.Numeric << 4) + 3,
        float4      = ((byte)BlastVariableDataType.Numeric << 4) + 4,
                    
        bool32      = ((byte)BlastVariableDataType.Bool32  << 4) + 1,
        bool64      = ((byte)BlastVariableDataType.Bool32  << 4) + 2,
        bool96      = ((byte)BlastVariableDataType.Bool32  << 4) + 3,
        bool128     = ((byte)BlastVariableDataType.Bool32  << 4) + 4,
                    
        int1        = ((byte)BlastVariableDataType.ID      << 4) + 1,
        int2        = ((byte)BlastVariableDataType.ID      << 4) + 2,
        int3        = ((byte)BlastVariableDataType.ID      << 4) + 3,
        int4        = ((byte)BlastVariableDataType.ID      << 4) + 4,

        // negated 
        float1_n    = ((byte)BlastVariableDataType.Numeric << 4) + 1 + 0b1000_0000,
        float2_n    = ((byte)BlastVariableDataType.Numeric << 4) + 2 + 0b1000_0000,
        float3_n    = ((byte)BlastVariableDataType.Numeric << 4) + 3 + 0b1000_0000,
        float4_n    = ((byte)BlastVariableDataType.Numeric << 4) + 4 + 0b1000_0000,

        bool32_n    = ((byte)BlastVariableDataType.Bool32  << 4) + 1 + 0b1000_0000,
        bool64_n    = ((byte)BlastVariableDataType.Bool32  << 4) + 2 + 0b1000_0000,
        bool96_n    = ((byte)BlastVariableDataType.Bool32  << 4) + 3 + 0b1000_0000,
        bool128_n   = ((byte)BlastVariableDataType.Bool32  << 4) + 4 + 0b1000_0000,
        
        int1_n      = ((byte)BlastVariableDataType.ID      << 4) + 1 + 0b1000_0000,
        int2_n      = ((byte)BlastVariableDataType.ID      << 4) + 2 + 0b1000_0000,
        int3_n      = ((byte)BlastVariableDataType.ID      << 4) + 3 + 0b1000_0000,
        int4_n      = ((byte)BlastVariableDataType.ID      << 4) + 4 + 0b1000_0000,
    }


 
   


    /// <summary>
    /// Bool32 structure 
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = 4)]
    public struct Bool32 :
        IStructuralComparable,
        IStructuralEquatable,
        IComparable,
        IComparable<float>,
        IComparable<int>,
        IComparable<uint>,
        IComparable<Bool32>,
        IEquatable<float>,
        IEquatable<uint>,
        IEquatable<int>,
        IEquatable<Bool32>
    {
        [FieldOffset(0)] public float Single;
        [FieldOffset(0)] public int Signed;
        [FieldOffset(0)] public uint Unsigned;

        [FieldOffset(0)] public byte Byte1;
        [FieldOffset(1)] public byte Byte2;
        [FieldOffset(2)] public byte Byte3;
        [FieldOffset(3)] public byte Byte4;

        public const uint FailPatternConstant
#if TRACE || DEVELOPMENT_BUILD            
            = 0b00110011_00110011_00110011_00110011;
#else
            = 0b00000000_00000000_00000000_00000000;
#endif

        public static Bool32 FailPattern { get { return Bool32.From(FailPatternConstant); } }

        static public Bool32 From(int i32)
        {
            Bool32 b32 = default;
            b32.Signed = i32;
            return b32;
        }

        static public Bool32 From(uint ui32)
        {
            Bool32 b32 = default;
            b32.Unsigned = ui32;
            return b32;
        }

        static public Bool32 From(float f)
        {
            Bool32 b32 = default;
            b32.Single = f;
            return b32;
        }

        /// <summary>
        /// not burst compatible.. 
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        [BurstDiscard]
        static public Bool32 From(string s)
        {
            uint b32; 
            if(CodeUtils.TryAsBool32(s, true, out b32))
            {
                return Bool32.From(b32); 
            }
            return Bool32.FailPattern;
        }


#region Get/Set Properties for Bool1 - 32 
        public bool b1 { get { return (Byte1 & 0b0000_0001) == 0b0000_0001; } set { SetBit(0, true); } }
        public bool b2 { get { return (Byte1 & 0b0000_0010) == 0b0000_0010; } set { SetBit(1, true); } }
        public bool b3 { get { return (Byte1 & 0b0000_0100) == 0b0000_0100; } set { SetBit(2, true); } }
        public bool b4 { get { return (Byte1 & 0b0000_1000) == 0b0000_1000; } set { SetBit(3, true); } }
        public bool b5 { get { return (Byte1 & 0b0001_0000) == 0b0001_0000; } set { SetBit(4, true); } }
        public bool b6 { get { return (Byte1 & 0b0010_0000) == 0b0010_0000; } set { SetBit(5, true); } }
        public bool b7 { get { return (Byte1 & 0b0100_0000) == 0b0100_0000; } set { SetBit(6, true); } }
        public bool b8 { get { return (Byte1 & 0b1000_0000) == 0b1000_0000; } set { SetBit(7, true); } }
        public bool b9 { get { return (Byte2 & 0b0000_0001) == 0b0000_0001; } set { SetBit(8, true); } }
        public bool b10 { get { return (Byte2 & 0b0000_0010) == 0b0000_0010; } set { SetBit(9, true); } }
        public bool b11 { get { return (Byte2 & 0b0000_0100) == 0b0000_0100; } set { SetBit(10, true); } }
        public bool b12 { get { return (Byte2 & 0b0000_1000) == 0b0000_1000; } set { SetBit(11, true); } }
        public bool b13 { get { return (Byte2 & 0b0001_0000) == 0b0001_0000; } set { SetBit(12, true); } }
        public bool b14 { get { return (Byte2 & 0b0010_0000) == 0b0010_0000; } set { SetBit(13, true); } }
        public bool b15 { get { return (Byte2 & 0b0100_0000) == 0b0100_0000; } set { SetBit(14, true); } }
        public bool b16 { get { return (Byte2 & 0b1000_0000) == 0b1000_0000; } set { SetBit(15, true); } }
        public bool b17 { get { return (Byte3 & 0b0000_0001) == 0b0000_0001; } set { SetBit(16, true); } }
        public bool b18 { get { return (Byte3 & 0b0000_0010) == 0b0000_0010; } set { SetBit(17, true); } }
        public bool b19 { get { return (Byte3 & 0b0000_0100) == 0b0000_0100; } set { SetBit(18, true); } }
        public bool b20 { get { return (Byte3 & 0b0000_1000) == 0b0000_1000; } set { SetBit(19, true); } }
        public bool b21 { get { return (Byte3 & 0b0001_0000) == 0b0001_0000; } set { SetBit(20, true); } }
        public bool b22 { get { return (Byte3 & 0b0010_0000) == 0b0010_0000; } set { SetBit(21, true); } }
        public bool b23 { get { return (Byte3 & 0b0100_0000) == 0b0100_0000; } set { SetBit(22, true); } }
        public bool b24 { get { return (Byte3 & 0b1000_0000) == 0b1000_0000; } set { SetBit(23, true); } }
        public bool b25 { get { return (Byte4 & 0b0000_0001) == 0b0000_0001; } set { SetBit(24, true); } }
        public bool b26 { get { return (Byte4 & 0b0000_0010) == 0b0000_0010; } set { SetBit(25, true); } }
        public bool b27 { get { return (Byte4 & 0b0000_0100) == 0b0000_0100; } set { SetBit(26, true); } }
        public bool b28 { get { return (Byte4 & 0b0000_1000) == 0b0000_1000; } set { SetBit(27, true); } }
        public bool b29 { get { return (Byte4 & 0b0001_0000) == 0b0001_0000; } set { SetBit(28, true); } }
        public bool b30 { get { return (Byte4 & 0b0010_0000) == 0b0010_0000; } set { SetBit(29, true); } }
        public bool b31 { get { return (Byte4 & 0b0100_0000) == 0b0100_0000; } set { SetBit(30, true); } }
        public bool b32 { get { return (Byte4 & 0b1000_0000) == 0b1000_0000; } set { SetBit(31, true); } }

        /// <summary>
        /// returns true if any bit is true 
        /// </summary>
        public bool Any { get { return Unsigned > 0; } }

        /// <summary>
        /// returns true if all bits are set 
        /// </summary>
        public bool All { get { return Unsigned == 0b11111111_11111111_11111111_11111111; } }

        #endregion

            /// <summary>
            /// index bits
            /// </summary>
            /// <param name="index">0 based index of bit</param>
            /// <returns>true if bit is set</returns>
        public bool this[int index]
        {
            get
            {
                return GetBit(index);
            }
            set
            {
                SetBit(index, value);
            }
        }

#region Get/Set bit

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetBit(int index, bool value)
        {
            int mask = 1 << index;
            Signed = value ? Signed | mask : Signed & ~mask;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool GetBit(int index)
        {
            int mask = 1 << index;
            return (Signed & mask) == mask;
        }

        /// <summary>
        /// multithread safe bit set through atomic cmpexch
        /// </summary>
        public void SetBitSafe(int index, bool value)
        {
            int bit = 1 << index;

            int i = 0;

            do
            {
                int current = Signed;

                // check if set 
                if ((current & bit) == bit) return;

                // you cannot do this once as other bits might change
                int next = value ? current | bit : current & ~bit;

                // attempt to attomically change the value 
                if (Interlocked.CompareExchange(ref Signed, next, current) == current)
                {
                    // value set, it didnt change meanwhile 
                    return;
                }

                // value was changed while trying to set it, restart procedure 
#if TRACE || DEVELOPMENT_BUILD
                Assert.IsTrue(i++ < 100);
#endif
            }
#if TRACE || DEVELOPMENT_BUILD
            while (true);
#else
            while (i++ < 100);
#endif
        }
#endregion

#region Compare | Equals interface implementations 

        public int CompareTo(float other)
        {
            return Comparer<float>.Default.Compare(Single, other);
        }
        public int CompareTo(uint other)
        {
            return Comparer<uint>.Default.Compare(Unsigned, other);
        }
        public int CompareTo(int other)
        {
            return Comparer<int>.Default.Compare(Signed, other);
        }
        public int CompareTo(Bool32 other, IComparer comparer)
        {
            return Comparer<uint>.Default.Compare(Unsigned, other.Unsigned);
        }

        public bool Equals(object other, IEqualityComparer comparer)
        {
            if (other == null) return false;
            if (!(other is Bool32)) return false;
            return comparer.Equals(other); 

        }

        public int GetHashCode(IEqualityComparer comparer)
        {
            return Signed.GetHashCode(); 
        }

        public override string ToString()
        {
            return CodeUtils.FormatBool32(Unsigned); 
        }

        public int CompareTo(object other)
        {
            if (other == null) return 1;
            Assert.IsTrue(!(other is Bool32));
            return CompareTo(((Bool32)other).Unsigned);
        }

        int IStructuralComparable.CompareTo(object other, IComparer comparer)
        {
            if (other == null) return 1;
            Assert.IsTrue(!(other is Bool32));
            return CompareTo(((Bool32)other).Unsigned);
        }

        public int CompareTo(Bool32 other)
        {
            return Comparer<uint>.Default.Compare(Unsigned, other.Unsigned);
        }

        public bool Equals(float other)
        {
            return Single == other;
        }

        public bool Equals(uint other)
        {
            return Unsigned == other;
        }

        public bool Equals(int other)
        {
            return Signed == other;
        }

        public bool Equals(Bool32 other)
        {
            return Unsigned == other.Unsigned;
        }

#endregion
    }

    /// <summary>
    /// blast_operation jumptargets, in loops w e use these to override flow with cases not used in that flow
    /// giving them a different name makes it a little more readable
    /// </summary>

    public enum blast_operation_jumptarget : byte
    {
        jump_assign_indexed = blast_operation.ex_op - 2,
        jump_assign_result = blast_operation.ex_op - 3,
        jump_assigns = blast_operation.ex_op - 4,
        jump_assignf = blast_operation.ex_op - 5,
        jump_assignfn = blast_operation.ex_op - 6,
        jump_assignfe = blast_operation.ex_op - 7,
        jump_assignfen = blast_operation.ex_op - 8,
        jump_ct_jz_eval = blast_operation.ex_op - 9,

        max_id_for_direct_assign = blast_operation.ex_op - 20 
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
        /// long jump on zero
        /// </summary>
        jz_long,

        /// <summary>
        /// long jump on not zero
        /// </summary>
        jnz_long, 

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
        /// get component sum of a vector 
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
        /// index a vector at [i] with a 16 bit index
        /// - also works on cdata, which it interprets as NUMERIC[], later versions will allow multiple datatypes 
        /// </summary>
        index_n,

        /// <summary>
        /// expand vector from given size to wanted size, if input = size 1, then the input is set to each component. otherwise 0 is set to new vector components
        /// </summary>
        /// <remarks>
        /// <c>expand(1, 4)</c>   expands value 1 to a vector of size 4: (1 1 1 1) 
        /// <c>expand(a, 4)</c>   expands variable a to a vector of size 4, if a = 1: (1 1 1 1), if a = (1 1), then result is (1 1 0 0) 
        /// </remarks>
        expand_v2,

        /// <summary>
        /// expand vector from given size to wanted size, if input = size 1, then the input is set to each component. otherwise 0 is set to new vector components
        /// </summary>
        /// <remarks>
        /// <c>expand(1, 4)</c>   expands value 1 to a vector of size 4: (1 1 1 1) 
        /// <c>expand(a, 4)</c>   expands variable a to a vector of size 4, if a = 1: (1 1 1 1), if a = (1 1), then result is (1 1 0 0) 
        /// </remarks>
        expand_v3,

        /// <summary>
        /// expand vector from given size to wanted size, if input = size 1, then the input is set to each component. otherwise 0 is set to new vector components
        /// </summary>
        /// <remarks>
        /// <c>expand(1, 4)</c>   expands value 1 to a vector of size 4: (1 1 1 1) 
        /// <c>expand(a, 4)</c>   expands variable a to a vector of size 4, if a = 1: (1 1 1 1), if a = (1 1), then result is (1 1 0 0) 
        /// </remarks>
        expand_v4,

        /// <summary>
        /// packagemode ssmd|entity only: inline constant definition of f1
        /// 
        /// DO NOT CHANGE BYTE VALUE ORDER OF CONSTANT REFS!!!
        /// </summary>
        constant_f1,

        /// <summary>
        /// packagemode ssmd|entity only: inline constant definition of a float1 encoded in 2 bytes 
        /// 
        /// DO NOT CHANGE BYTE VALUE ORDER OF CONSTANT REFS!!!
        /// </summary>
        constant_f1_h,

        /// <summary>
        /// packagemode ssmd|entity only: short (8bit) reference to earlier definition of a constant 
        /// 2 bytes large -> for def_f1 total byte count = 5 so even with smallest datatype it saves 3 bytes referencing the constant
        /// </summary>
        constant_short_ref,

        /// <summary>
        /// packagemode ssmd|entity only: long (16bit) reference to earlier definition of a constant 
        /// 3 bytes large -> for def_f1 total byte count = 5 so even with smallest datatype it saves 2 bytes referencing the constant
        /// </summary>
        /// <remarks>
        /// ref_f -addressoffset (2 bytes)... ref_f2 data data 
        /// </remarks>
        constant_long_ref,
                                                                                          
        /// <summary>
        /// DO NOT CHANGE BYTE VALUE ORDER OF CONSTANT REFS!!!
        /// </summary>
        constant_i1,

        /// <summary>
        /// DO NOT CHANGE BYTE VALUE ORDER OF CONSTANT REFS!!!
        /// </summary>
        constant_i1_h,

        /// <summary>
        /// DO NOT CHANGE BYTE VALUE ORDER OF CONSTANT REFS!!!
        /// </summary>
        constant_reserved_1,

        /// <summary>
        /// DO NOT CHANGE BYTE VALUE ORDER OF CONSTANT REFS!!!
        /// </summary>
        constant_reserved_2, 

        
        /// <summary>
        /// set data at index to zero 
        /// </summary>
        zero,

        /// <summary>
        /// get element size of a given variable, for vectors returns vectorsize, for cdata returns number of elements in interpreted datatype
        /// </summary>
        size,  

        /// <summary>
        /// constant data declaration - untyped == array of float values unless otherwise interpreted
        /// - future work might expand this into multiple ops or add a parameter
        /// </summary>
        cdata,

        /// <summary>
        /// constant data reference, use size to get its length
        /// - in normal packaging: compiler puts all cdata at start of code segment and references it there
        /// - in ssmd packaging: compiler inlines cdata at first location of use and only after that references it 
        /// </summary>
        cdataref,


        /// <summary>
        /// SSMD only, jump if zero, used in a frame-constant terminated sequence or conditional based on frame-constant data
        /// - this will allow ssmd to jump without maintaining its state and causig disalignment in the datasegments 
        /// </summary>
        cjz,

        /// <summary>
        /// SSMD only, jump if zero, used in a frame-constant terminated sequence or conditional based on frame-constant data
        /// - this will allow ssmd to jump without maintaining its state and causig disalignment in the datasegments 
        /// </summary>
        cjz_long,



        /// <summary>
        /// PI
	///
	/// Direct use of value opcodes in the codestream assign that value to the next opcode, making for some very fast assignments 
	///
	///
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
        
        framecount,
        fixedtime,
        time,
        fixeddeltatime,
        deltatime = 127,

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
        saturate,  
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

        /// <summary>
        /// floating point modulus operation 
        /// </summary>
        fmod,


// these all need SSMD updates 
        sign,
        length,
        lengthsq,
        square,

        distance,
        distancesq,

        reflect, 
        project, 

        up,
        down,
        forward, 
        back, 
        left, 
        right, 

        // the quaternion -> radian angles 
        EulerXYZ,
        EulerXZY,
        EulerYZX,
        EulerYXZ,
        EulerZXY,
        EulerZYX,
        Euler,

        // the angles in randians to quaternion 
        QuaternionXYZ,
        QuaternionXZY,
        QuaternionYZX,
        QuaternionYXZ,
        QuaternionZXY,
        QuaternionZYX,
        Quaternion,

        // single param functions returning quaternions 
        RotateX, RotateY, RotateZ, Conjugate, Inverse,

        // dual v3 param functions returning quaternions 
        LookRotation, LookRotationSafe,

        /// <summary>
        /// q4 = mul(q4, q4)
        /// f3 = mul(q4, f3)
        /// </summary>
        Mul,
        
        /// <summary>
        /// rotate(q4, f3)
        /// </summary>
        Rotate,
        
        /// <summary>
        /// angle(q4, q4) 
        /// </summary>
        Angle,

        Magnitude,
        SqrMagnitude,

 //// a lot to do in ssmd mode... 


        // to add in both modes: 
        CircleCenter, 










        /// <summary>
        /// send all data in the element to a sink defined in blast 
        /// </summary>
        send,

        /// <summary>
        /// seed the random number generator 
        /// </summary>
        seed,

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
        /// bitwise shift left 
        /// </summary>        
        shl,

        /// <summary>
        /// bitwise shift right 
        /// </summary>
        shr,

        /// <summary>
        /// bitwise rol right
        /// </summary>
        ror,

        /// <summary>
        /// bitwise rol left 
        /// </summary>
        rol, 

        /// <summary>
        /// leading zero bit count
        /// </summary>
        lzcnt,
        
        /// <summary>
        /// trailing zero bit count 
        /// </summary>
        tzcnt,

        /// <summary>
        /// count high bits 
        /// </summary>
        count_bits,

        /// <summary>
        /// reverse bits 
        /// </summary>
        reverse_bits,   

        /// <summary>
        /// set(variable, mask, value[0|1])
        /// </summary>
        set_bits,

        /// <summary>
        /// get(variable, mask) => [0|1]
        /// </summary>
        get_bits,

        /// <summary>
        /// set a bit: set_bit(variable, index, 0|1); 
        /// </summary>
        set_bit,

        /// <summary>
        /// get(variable, index) => 0|1
        /// </summary>
        get_bit,



        // bool32 bit operations  -> these are explicetly in extended ops to force things to be more flexible in the interpretors        \
        // when they accept this, ID and HALF should not be a big problem  
        or32, and32, xor32, not32,


        /// <summary>
        /// reinterpret data at index as an integer, updates metadata only 
        /// </summary>
        reinterpret_int32 = 248,

        /// <summary>
        /// reinterpret data at index as a float, updates metadata only 
        /// </summary>
        reinterpret_float = 249,

        /// <summary>
        /// reinterpret data at index as a bool, updates metadata only 
        /// </summary>
        reinterpret_bool32 = 250,


        /// <summary>
        /// runtime validation of values, raises script errors if values are not equal
        /// </summary>
        validate = 251,

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
        Function,


        /// <summary>
        /// inline increment: i++; 
        /// </summary>
        Increment,

        /// <summary>
        /// inlined decrement: i--;
        /// </summary>
        Decrement 




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
        /// <summary>
        /// failure enumerating all function attributed with the BlastFunctionAttribute, most probably blast could not match the method definition to a matching delegate
        /// </summary>
        error_enumerate_attributed_external_functions = -69,
        /// <summary>
        /// invalid data count for ssmd loops, should be > 0 and smaller then 32x1024
        /// </summary>
        error_invalid_ssmd_count = -70,
        /// <summary>
        /// ssmd interpretor error: datasegment buffer == null 
        /// </summary>
        error_execute_ssmddata_null = -71,
        /// <summary>
        /// the script defines no variables and as such they cannot be set 
        /// </summary>
        error_script_has_no_variables = -72,
        /// <summary>
        /// the script is not yet packaged|compiled|prepared 
        /// </summary>
        error_script_not_packaged = -73,
        /// <summary>
        /// attempted to set data with a buffer that doesnt have the correct size
        /// </summary>
        error_setdata_size_mismatch = -74,
        /// <summary>
        /// datatype is not supported as a constant that is to be inlined 
        /// </summary>
        error_compile_inlined_constant_datatype_not_supported = -75,
        /// <summary>
        /// interpretor validationmode cannot be used on referenced data 
        /// </summary>
        error_ssmd_validationmode_cannot_run_on_referenced_data = -76,
        /// <summary>
        /// compiler failed to infer datatype from given nodes 
        /// </summary>
        error_analyzer_failed_to_infer_parameter_types = -77,
        /// <summary>
        /// job datasize or count does not match datasegment 
        /// </summary>
        error_execute_package_datasize_count_invalid = -78,
        /// <summary>
        /// a package with the wrong packagemode is sent for execution to an interpretor that only supports normal packages 
        /// </summary>
        error_invalid_packagemode_should_be_normal = -79,
       
        /// <summary>
        /// a package is executed with a given datasize that is different from the datasize in the packaged datasegment (alignment errors? constants in datasegment not accounted for?)
        /// </summary>
        error_execute_package_datasize_mismatch = -80,                             
        /// <summary>
        /// there is no memory location set to store the exitstate of a job
        /// </summary>
        error_job_exitstate_not_set = -81,
        /// <summary>
        /// while executing packages in normal package modes with datasegments packed in nativearrays the backing type should be of 32 bits size dueue to offset calculations used
        /// </summary>
        error_execute_invalid_backing_datasegment = -82,
        /// <summary>
        /// referencing datasegments from a pointerlist is currently only supported in SSMD packagemodes
        /// </summary>
        error_execute_referenced_datasegments_not_supported = -83,
        /// <summary>
        /// tokenizer encountered an invalid binary stream
        /// a binary stream may contain: 0 1 b _ in the from b00000000_000....
        /// </summary>
        error_tokenizer_invalid_binary_stream = -84,
        /// <summary>
        /// failed to read #cdata define 
        /// </summary>
        error_failed_to_read_cdata_define = -85,
        /// <summary>
        /// failed to package cdata element, its too large to fit as vector in the datasegment
        /// - max size for variable cdata = 58 bytes (15*4-2)
        /// - #cdata constants have no max size other then total codesize of 32kb
        /// </summary>
        error_package_cdata_variable_too_large = -86,
        /// <summary>
        /// there are too deeply nested statements or some pattern made the compiler fail to flatten out the ast 
        /// </summary>
        error_unresolved_nested_statements = -87,
        /// <summary>
        /// failed to compile constant cdata node into the codestream 
        /// </summary>
        error_failed_to_compile_cdata_node = -88,
        /// <summary>
        /// failed to read cdata defined as array of numerics
        /// </summary>
        error_tokenizer_invalid_cdata_array = -89,
        /// <summary>
        /// indexer is out of bounds or invalid (negative)
        /// </summary>
        error_indexer_out_of_bounds = -90,
        /// <summary>
        /// interpretor failed to read indexer on cdata object 
        /// </summary>
        error_failed_to_read_cdata_indexer = -91,
        /// <summary>
        /// interpretor failed to assing to the cdata segment 
        /// </summary>
        error_failed_to_interpret_cdata_assignment = -92,
        /// <summary>
        /// attempting to index cdata with an out of range indexer. 
        /// </summary>
        error_cdata_indexer_out_of_bounds = -93,
        /// <summary>
        /// if the sequence is executed with a target and it returns without setting that target then this error is called. something is wrong with the sequencer  
        /// </summary>
        error_target_not_set_in_sequence = -94,
        /// <summary>
        /// the interpretor failed to follow a cdatareference 
        /// </summary>
        error_failed_to_read_cdata_reference = -95,
        /// <summary>
        /// compile has conflicting results about amount of stack memory used by the compiled script
        /// </summary>
        stack_size_determination_error = -96,
        /// <summary>
        /// the target of index_n is not a cdataref or vector in the datasegment 
        /// </summary>
        invalid_index_n_target = -97,
        /// <summary>
        /// the assignment attemted is not valid on a cdata reference (assigning vectors to component indices?)
        /// </summary>
        error_in_cdata_assignment_size = -98,
        /// <summary>
        /// the parameter to the current function must be a variable id 
        /// </summary>
        ssmd_parameter_must_be_variable = -99,
        /// <summary>
        /// an attempt was made to assign a vector to a component of a cdata 
        /// </summary>
        error_cannot_assign_vector_to_cdata_component = -100,
        /// <summary>
        /// failed to follow a constant reference in the codestream
        /// </summary>
        error_constant_reference_error = -101,
        /// <summary>
        /// the referenced codesegment is null  
        /// </summary>
        error_codesegment_null = -102,
        /// <summary>
        /// attempting to assing a datatype to a cdata index of a different datatype 
        /// </summary>
        error_cdata_datatype_mismatch = -103,
        /// <summary>
        /// encountered an unsupported encoding for the format of the constant data buffer encoded in the code stream
        /// </summary>
        error_unsupported_cdata_encoding = -104,
        /// <summary>
        /// unable to decode cdata as request value
        /// </summary>
        error_failed_to_decode_cdata = -105,
        error_incrementor_transform = -106,
        error_compiling_div_by_zero = -107
    }

}
