using System;
using System.Collections.Generic;
using System.Text;
using Unity.Collections;

namespace NSS.Blast
{                      
    /// <summary>
    /// Options for compiling and packaging scripts 
    /// </summary>
    public class BlastCompilerOptions
    {
        /// <summary>
        /// run validation if set in script 
        /// </summary>
        public bool AutoValidate = false;

        /// <summary>
        /// run code optimizer 
        /// </summary>
        public bool Optimize = true;

        /// <summary>
        /// compile script using constant data fields 
        /// </summary>
        public bool CompileWithSystemConstants = true;

        /// <summary>
        /// compare epsilon for constants, on fast float mode it wont match on float.epsilon
        /// </summary>
        public float ConstantEpsilon = 0.001f;     // in fast floating point mode it wont match constants on float.epsilon
        public string ConstantPrecision = "0.000"; // precision used when using constant values as identifier names, everything is rounded to this 

        /// <summary>
        /// default stack size in number of bytes
        /// - overridden if the script defines a stack size 
        /// </summary>
        public int DefaultStackSize = 16;

        /// <summary>
        /// estimate stack size using input, output and validation parameters 
        /// - overridden if the script defines a stack size
        /// </summary>
        public bool EstimateStackSize = true;

        /// <summary>
        /// verbose report logging, log is also blitted to unity
        /// </summary>
        public bool Verbose = true;

        /// <summary>
        /// enable trace level logging -> will generate a massive report 
        /// </summary>
        public bool Trace = true;

        /// <summary>
        /// build a report during compilation 
        /// </summary>
        public bool Report = true;

        /// <summary>
        /// if yield is not emitted we can save 20 bytes on the stack, with very small stuff that might be handy
        /// not implemented .. todo
        /// </summary>
        public bool SupportYield = true;

        /// <summary>
        /// compile the debug function only if enabled 
        /// </summary>
        public bool CompileDebug = true; 

        /// <summary>
        /// package mode
        /// </summary>
        public BlastPackageMode PackageMode = BlastPackageMode.CodeDataStack;

        public Allocator PackageAllocator = Allocator.Temp;

        /// <summary>
        /// enable for partially parallel compilation 
        /// - fails somewhere in optimizer or jumpresolver 
        /// </summary>
        public bool ParallelCompilation = false;

        /// <summary>
        /// additional compiler defines 
        /// </summary>
        public Dictionary<string, string> Defines = new Dictionary<string, string>();

        /// <summary>
        /// default compiler options 
        /// </summary>
        public static BlastCompilerOptions Default => new BlastCompilerOptions();


        /// <summary>
        /// initialize default options with given stacksize 
        /// </summary>
        /// <param name="stack_size"></param>
        public BlastCompilerOptions(int stack_size)
        {
            DefaultStackSize = stack_size;
            EstimateStackSize = false; 
        }

        /// <summary>
        /// initialize with default options 
        /// </summary>
        public BlastCompilerOptions()
        {
        }


        /// <summary>
        /// add a compiler define 
        /// </summary>
        /// <param name="key">the key value</param>
        /// <param name="value">single, or parameter name when brave</param>
        public void AddDefine(string key, string value)
        {
            key = key.ToLower().Trim();
            if (!Defines.ContainsKey(key))
            {
                // set new
                Defines.Add(key, value);
            }
            else
            {
                // update existing
                Defines[key] = value;
            }
        }
    }

}
