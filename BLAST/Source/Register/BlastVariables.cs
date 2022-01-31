using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace NSS.Blast
{

    /// <summary>
    /// description mapping a variable to an input or output
    /// </summary>
    public class BlastVariableMapping
    {
        /// <summary>
        /// the variable that is mapped 
        /// </summary>
        public BlastVariable Variable;
        
        /// <summary>
        /// Id of variable 
        /// </summary>
        public int VariableId => Variable.Id;

        /// <summary>
        /// Datatype of variable 
        /// </summary>
        public BlastVariableDataType DataType => Variable.DataType;

        /// <summary>
        /// Offset of variable into datasegment, in bytes 
        /// </summary>
        public int Offset;
        
        /// <summary>
        /// Bytesize of variable  
        /// </summary>
        public int ByteSize;


        /// <summary>
        /// ToString override providing a more usefull description while debugging 
        /// </summary>
        /// <returns>a formatted string</returns>
        public override string ToString()
        {
            if (Variable == null)
            {
                return $"variable mapping: var = null, bytesize: {ByteSize}, offset: {Offset}";
            }
            else
            {
                return $"variable mapping: var: {VariableId} {Variable.Name} {Variable.DataType}, vectorsize: {Variable.VectorSize} , bytesize: {ByteSize}, offset: {Offset}";
            }
        }
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
    /// Description of a variable as determined during compilation of script. It is not needed for execution
    /// and is thus not available through the native BlastPackageData. Native code can query metadata.
    /// </summary>
    public class BlastVariable
    {
        /// <summary>
        /// the unique id of the variable as used within the script
        /// </summary>
        public int Id;

        /// <summary>
        /// The name of the variable as it was used in the script 
        /// </summary>
        public string Name;

        /// <summary>
        /// Number of times blast references the variable
        /// </summary>
        public int ReferenceCount;

        /// <summary>
        /// The datatype of the variable:
        /// - V1: only numeric datatypes are supported
        /// - V2: support for ID types 
        /// - V3: ID64, NUMERIC64 
        /// </summary>
        public BlastVariableDataType DataType = BlastVariableDataType.Numeric;

        /// <summary>
        /// is this variable constant? 
        /// </summary>
        public bool IsConstant = false;


        /// <summary>
        /// true if this is a vector of size > 1
        /// </summary>
        public bool IsVector = false;

        /// <summary>
        /// vectorsize of the variable
        /// </summary>
        public int VectorSize = 0;

        /// <summary>
        /// true if specified in inputs 
        /// </summary>
        public bool IsInput = false;

        /// <summary>
        /// true if specified in outputs 
        /// </summary>
        public bool IsOutput = false;


        /// <summary>
        /// ToString override for a more detailed view during debugging 
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"{(IsConstant ? "Constant: " : "Variable: ")} {Id}, '{Name}', ref {ReferenceCount}, size {VectorSize}, type {DataType} {(IsInput ? "[INPUT]" : "")} {(IsOutput ? "[OUTPUT]" : "")}";
        }

        /// <summary>
        /// Add a reference to this variable 
        /// </summary>
        internal void AddReference()
        {
            Interlocked.Increment(ref ReferenceCount);
        }
    }
}
