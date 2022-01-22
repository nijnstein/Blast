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

    public enum ReservedScriptFunctionIds : int
    {
        Input = 1001,
        Output = 1002,
        Yield = 1003,
        Push = 1004,
        Pop = 1005,
        Pop2 = 1006,
        Pop3 = 1007,
        Pop4 = 1008,
        Peek = 1009,
        Seed = 1010,
        PushFunction = 1011,
        PushCompound = 1013,
        PushVector = 1014,
        Debug = 1012
    }
}