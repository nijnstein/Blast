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
        public BlastVariable variable;

        public int id => variable.Id;
        public BlastVariableDataType DataType => variable.DataType;

        public int offset;
        public int bytesize;

        public override string ToString()
        {
            if (variable == null)
            {
                return $"variable mapping: var = null, bytesize: {bytesize}, offset: {offset}";
            }
            else
            {
                return $"variable mapping: var: {id} {variable.Name} {variable.DataType}, vectorsize: {variable.VectorSize} , bytesize: {bytesize}, offset: {offset}";
            }
        }
    }

    /// <summary>
    /// supported variable data types 
    /// </summary>
    public enum BlastVariableDataType : byte
    {
        Numeric = 0,
        ID = 1
       //  ID64 = 2  // => duals as a pointer type 
    }      

    /// <summary>
    /// description of a variable as determined during compilation 
    /// </summary>
    public class BlastVariable
    {
        public int Id;
        public string Name;
        public int ReferenceCount;

        public BlastVariableDataType DataType = BlastVariableDataType.Numeric;

        public bool IsConstant = false;
        public bool IsVector = false;

        public bool IsInput = false;
        public bool IsOutput = false;

        public int VectorSize = 0;

        public override string ToString()
        {
            return $"{(IsConstant ? "Constant: " : "Variable: ")} {Id}, '{Name}', ref {ReferenceCount}, size {VectorSize}, type {DataType} {(IsInput ? "[INPUT]" : "")} {(IsOutput ? "[OUTPUT]" : "")}";
        }

        internal void AddReference()
        {
            Interlocked.Increment(ref ReferenceCount);
        }
    }
}
