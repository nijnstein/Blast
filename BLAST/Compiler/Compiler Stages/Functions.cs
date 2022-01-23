namespace NSS.Blast
{
    public class ScriptFunctionDefinition
    {
        public int FunctionId { get; set; }
        public string Match { get; set; }
        public string[] Parameters { get; set; }
        public readonly bool IsReserved;

        public readonly blast_operation ScriptOp;
        public readonly extended_blast_operation ExtendedScriptOp;

        public readonly ExternalFunctionCall ExternalFunction;

        public readonly int MinParameterCount;
        public readonly int MaxParameterCount;

        /// <summary>
        /// if == 0 returns same as input 
        /// </summary>
        public readonly int ReturnsVectorSize;

        /// <summary>
        /// if == 0 accepts any 
        /// </summary>
        public readonly int AcceptsVectorSize; 


        public bool CanHaveParameters => MaxParameterCount > 0;

        public string CSName { get; set; }
        
        public bool IsExternalCall => ExternalFunction != null && ExtendedScriptOp == extended_blast_operation.call;
        public int ParameterCount {  get { return Parameters != null ? Parameters.Length : 0; } }

        public override string ToString()
        {
            return $"Function Definition: {FunctionId} {Match}, parameter count: {ParameterCount}"; 
        }

        public ScriptFunctionDefinition(int id, string match, int min_parameter_count, int max_parameter_count, int return_vector_size = 0, int input_vector_size = 0, extended_blast_operation extended_op = extended_blast_operation.nop, ExternalFunctionCall external_call = null, params string[] parameters)
        {
            FunctionId = id;
            Match = match;
            Parameters = parameters;
            ScriptOp = blast_operation.ex_op;
            ExtendedScriptOp = extended_op;

            if (external_call != null)
            {
                ExternalFunction = external_call;
                external_call.ScriptFunction = this;
            }

            MinParameterCount = min_parameter_count;
            MaxParameterCount = max_parameter_count;

            ReturnsVectorSize = return_vector_size;
            AcceptsVectorSize = input_vector_size; // not having this on each input kinda limits definitions  

            switch (id)
            {
                case (int)ReservedScriptFunctionIds.Input:
                case (int)ReservedScriptFunctionIds.Output:
                    {
                        IsReserved = true;
                    }
                    break;

                default:
                    {
                        IsReserved = false;
                    }
                    break;
            }
        }

        public ScriptFunctionDefinition(int id, string match, int min_parameter_count, int max_parameter_count, int return_vector_size = 0, int input_vector_size = 0, blast_operation op = blast_operation.nop, params string[] parameters)
        {
            FunctionId = id;
            Match = match;
            Parameters = parameters;
            ScriptOp = op;
            ExtendedScriptOp = extended_blast_operation.nop;

            MinParameterCount = min_parameter_count;
            MaxParameterCount = max_parameter_count;
            
            ReturnsVectorSize = return_vector_size;
            AcceptsVectorSize = input_vector_size; // not having this on each input kinda limits definitions  

            switch (id)
            {
                case (int)ReservedScriptFunctionIds.Input:
                case (int)ReservedScriptFunctionIds.Output:
                    {
                        IsReserved = true;
                    }
                    break;

                default:
                    {
                        IsReserved = false;
                    }
                    break;
            }
        }

        internal bool IsPopVariant()
        {
            switch ((ReservedScriptFunctionIds)FunctionId)
            {
                case ReservedScriptFunctionIds.Pop:
                case ReservedScriptFunctionIds.Pop2:
                case ReservedScriptFunctionIds.Pop3:
                case ReservedScriptFunctionIds.Pop4:
                    return true;

                default: return false;
            }
        }

        internal bool IsPushVariant()
        {
            switch ((ReservedScriptFunctionIds)FunctionId)
            {
                case ReservedScriptFunctionIds.Push:
                case ReservedScriptFunctionIds.PushFunction:
                case ReservedScriptFunctionIds.PushVector:
                case ReservedScriptFunctionIds.PushCompound:
                    return true;

                default: return false;
            }
        }
    }

    public class ExternalFunctionCall
    {
        public string Match;
        public int OutputVectorSize;
        public ScriptFunctionDefinition ScriptFunction;
        public NonGenericFunctionPointer FunctionPointer; 

        public ExternalFunctionCall(string match, int output_vector_size, NonGenericFunctionPointer fp)
        {
            Match = match;
            OutputVectorSize = output_vector_size;
            FunctionPointer = fp; 
        }
    }

    /// <summary>
    /// IDs reserved for functions used in blast, these get registered to blast with a name (that doesnt need to be equal) 
    /// </summary>
    public enum ReservedScriptFunctionIds : int
    {
        /// <summary>
        /// input mapping function - reserved for internal use
        /// </summary>
        Input = 1001,

        /// <summary>
        /// output mapping function - reserved for internal use
        /// </summary>
        Output = 1002,

        /// <summary>
        /// Yield execution 
        /// </summary>
        Yield = 1003,

        /// <summary>
        /// Pop something from the stack 
        /// </summary>
        Pop = 1005,

        /// <summary>
        /// Pop a vector of size 2 from the stack 
        /// </summary>
        Pop2 = 1006,

        /// <summary>
        /// Pop a vector of size 3 from the stack 
        /// </summary>
        Pop3 = 1007,

        /// <summary>
        /// Pop a vector of size 4 from the stack 
        /// </summary>
        Pop4 = 1008,

        /// <summary>
        /// Peek stack top 
        /// </summary>
        Peek = 1009,

        /// <summary>
        /// Seed the random number generator 
        /// </summary>
        Seed = 1010,

        /// <summary>
        /// Push something to the stack 
        /// </summary>
        Push = 1004,

        /// <summary>
        /// Push a function's return value to the stack 
        /// </summary>
        PushFunction = 1011,

        /// <summary>
        /// Push the result from executing a compound onto the stack 
        /// </summary>
        PushCompound = 1013,

        /// <summary>
        /// Push a vector directly onto the stack 
        /// </summary>
        PushVector = 1014,

        /// <summary>
        /// Output debuginformation to the debug stream about given parameter 
        /// </summary>
        Debug = 1012
    }
}