namespace NSS.Blast
{
    
    public enum BlastError : int
    {
        yield = 1,
        success = 0,
        error = -1,
        error_unknown_op = -2,
        error_assign_vector_size_mismatch = -3,
        error_begin_end_sequence_in_root = -4,
        error_unsupported_operation_in_root = -5,
        error_variable_vector_op_not_supported = -6,
        error_update_vector_fail = -7,
        stack_error_peek = -8,
        stack_error_pop = -9,
        stack_error_variable_pop_not_supported = -10,
        stack_error_pop_multi_from_root = -11,
        error_variable_vector_compound_not_supported = -12,
        error_max_iterations = -13,
        validate_error_stack_too_small = -14,
        script_id_not_found = -15,
        error_blast_not_initialized = -16,
        error_invalid_script_id = -17,
        error_cs_compiler_not_supported_on_platform = -18,
        error_invalid_nodetype = -19,
        error_unsupported_operation = -20,
        error_node_not_flat = -21,
        error_failed_to_compile_function_parameters = -22,
        error_too_many_parameters = -23,
        error_failed_to_translate_offset_into_index = -24,
        error_vector_size_not_supported = -25,
        datasegment_size_larger_then_target = -26,
        compile_packaging_error = -27,
        ssmd_invalid_packagemode = -28,
        error_unsupported_operation_in_ssmd_root = -29,
        ssmd_error_expecting_value = -30,
        ssmd_error_unsupported_vector_size = -31,
        error_execute_package_not_correctly_set = -32,
        error_invalid_nodetype_in_root = -33,
        error_node_function_unknown = -34,
        error_failed_to_flatten_function_parameters = -35,
        error_pushv_vector_size_not_supported = -36,
        error_optimizer_operation_mismatch = -37,
        error_optimizer_parameter_mismatch = -38,
        error_optimizer_failed_to_replace_sequence = -39,
    }

    public static class BlastErrorTranslator
    {
        public static string TranslateError(BlastError error, int locale = 0)
        {
            // todo, translations in NL + EN 



            return error.ToString(); 
        }
    }

}

