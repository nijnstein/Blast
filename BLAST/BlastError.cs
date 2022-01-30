namespace NSS.Blast
{
    
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
        error_mapping_parameters = -50
    }

}

