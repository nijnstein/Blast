using System;
using System.Collections.Generic;
using System.Text;
using Unity.Assertions;
using Unity.Collections;

namespace NSS.Blast
{                      
    /// <summary>
    /// Options for compiling and packaging scripts 
    /// </summary>
    public class BlastCompilerOptions
    {
        /// <summary>
        /// The languageversion to use for the output of the compilation. Packagemode and language version are coupled but there may be deviations:
        /// - You can execute BS1 language version files with SSMD package modes if you guarentee that the code doesnt branch (the interpretor wont check this)
        /// - You can exectue BS1SSMD compiled packages with the normal interpretor in normal package mode
        /// </summary>
        public BlastLanguageVersion Language = BlastLanguageVersion.BS1;

        /// <summary>
        /// the package mode for the output, this determines how code and data is organized in memory 
        /// </summary>
        public BlastPackageMode PackageMode = BlastPackageMode.Normal;

        /// <summary>
        /// Allocator for data package 
        /// </summary>
        public Allocator PackageAllocator = Allocator.Persistent;

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
        public readonly bool CompileWithSystemConstants = true;

        /// <summary>
        /// compare epsilon for constants, on fast float mode it wont match on float.epsilon
        /// </summary>
        public float ConstantEpsilon = 0.001f;     // in fast floating point mode it wont match constants on float.epsilon

        /// <summary>
        /// constant precision expressed as a string value
        /// </summary>
        public string ConstantPrecision = "0.000"; // precision used when using constant values as identifier names, everything is rounded to this 

        /// <summary>
        /// default stack size in number of bytes
        /// - overridden if the script defines a stack size 
        /// </summary>
        public int DefaultStackSize = 0;

        /// <summary>
        /// packages include stackdata 
        /// </summary>
        public bool PackageStack = true; 

        /// <summary>
        /// estimate stack size using input, output and validation parameters 
        /// - overridden if the script defines a stack size
        /// </summary>
        public bool EstimateStackSize = true;

        /// <summary>
        /// verbose report logging, log is also blitted to unity
        /// </summary>
        public bool VerboseLogging = false;

        /// <summary>
        /// enable trace level logging -> will generate a massive report 
        /// </summary>
        public bool TraceLogging = false;

        /// <summary>
        /// if yield is not emitted we can save 20 bytes on the stack, with very small stuff that might be handy
        /// not implemented .. todo
        /// </summary>
        public readonly bool SupportYield = false;

        /// <summary>
        /// compile the debug function only if enabled 
        /// </summary>
        public readonly bool CompileDebug = true; 

        /// <summary>
        /// enable for partially parallel compilation [forcebly disabled]
        /// </summary>
        public readonly bool ParallelCompilation = false;

        /// <summary>
        /// additional compiler defines 
        /// </summary>
        public Dictionary<string, string> Defines = new Dictionary<string, string>();

        /// <summary>
        /// dont touch if you need to ask 
        /// </summary>
        public bool Experimental = false;

        /// <summary>
        /// Default compiler options:
        /// BS1 - Normal - Optimizing
        /// </summary>
        public static BlastCompilerOptions Default => new BlastCompilerOptions();

        /// <summary>
        /// Default SSMD compiler options:
        /// BS1 - SSMD Packagemode - Optimizing
        /// </summary>
        public static BlastCompilerOptions SSMD => new BlastCompilerOptions().SetPackageMode(BlastPackageMode.SSMD).PackageWithoutStack();

        /// <summary>
        /// Default compiler options for packaging entities:
        /// BS1 - Entity Packagemode - Optimizing
        /// </summary>
        public static BlastCompilerOptions Entity => new BlastCompilerOptions().SetPackageMode(BlastPackageMode.Entity);

        /// <summary>
        /// initialize default options with given stacksize 
        /// </summary>
        /// <param name="stack_size">stacksize, stack estimation will be turned off</param>
        public BlastCompilerOptions(int stack_size)
        {
            DefaultStackSize = stack_size;
            EstimateStackSize = false;
        }

        /// <summary>
        /// initilize default options in given packaging mode
        /// </summary>
        /// <param name="packagemode">the package mode to set</param>
        public BlastCompilerOptions(BlastPackageMode packagemode)
        {
            PackageMode = packagemode; 
        }

        /// <summary>
        /// initialize default options in given packagemode with a pre-determined stack size
        /// </summary>
        /// <param name="stack_size">stacksize, stack estimation will be turned off</param>
        /// <param name="packagemode">packaging mode</param>
        public BlastCompilerOptions(int stack_size, BlastPackageMode packagemode)
        {
            DefaultStackSize = stack_size;
            EstimateStackSize = false;
            PackageMode = packagemode; 
        }

        /// <summary>
        /// initialize with default options 
        /// </summary>
        public BlastCompilerOptions()
        {
        }

        #region Method chains to setup options 


        /// <summary>
        /// enable validation (default = off) 
        /// </summary>
        /// <param name="run_auto_validation">true to enable validation</param>
        /// <returns>options</returns>
        public BlastCompilerOptions EnableValidation(bool run_auto_validation = true)
        {
            this.AutoValidate = run_auto_validation;
            return this;           
        }

        /// <summary>
        /// set the size of the stack, if 0 is set stack is estimated during compilation 
        /// </summary>
        /// <param name="stack_size"></param>
        /// <returns>compiler options</returns>
        public BlastCompilerOptions SetStackSize(int stack_size = 0)
        {
            this.DefaultStackSize = stack_size;
            return this;
        }

        /// <summary>
        /// package without a stack segment, use stackmemory in the interpretor 
        /// </summary>
        /// <returns>compiler options</returns>
        public BlastCompilerOptions PackageWithoutStack()
        {
            this.PackageStack = false;
            return this; 
        }

        /// <summary>
        /// Set language target 
        /// </summary>
        /// <param name="language">Supported language: BS1 | BSSSMD1 | HPC </param>
        /// <returns>compiler options</returns>
        public BlastCompilerOptions SetLanguage(BlastLanguageVersion language = BlastLanguageVersion.BS1)
        {
            this.Language = language;
            return this; 
        }

        /// <summary>
        /// Set packaging mode
        /// </summary>
        /// <param name="packagemode">Packagemode: Normal, SSMD or Entity</param>
        /// <returns>compiler options</returns>
        public BlastCompilerOptions SetPackageMode(BlastPackageMode packagemode = BlastPackageMode.Normal)
        {
            this.PackageMode = packagemode;
            return this; 
        }

        /// <summary>
        /// Set allocation mode for packages, use temp with caution in combination with bursted code
        /// </summary>
        /// <param name="allocator">the unity memory allocation type</param>
        /// <returns>compiler options</returns>
        public BlastCompilerOptions SetAllocator(Allocator allocator)
        {
            this.PackageAllocator = allocator;
            return this; 
        }

        /// <summary>
        /// enable compiler optimizations, default == enabled
        /// </summary>
        /// <returns>compiler options</returns>
        public BlastCompilerOptions EnableOptimisations()
        {
            this.Optimize = true;
            return this; 
        }

        /// <summary>
        /// disable compiler optimisations, default = enabled
        /// </summary>
        /// <returns>compiler options</returns>
        public BlastCompilerOptions DisableOptimisations()
        {
            this.Optimize = false;
            return this; 
        }

        /// <summary>
        /// disable all logging, fails silently
        /// </summary>
        /// <returns>compiler options</returns>
        public BlastCompilerOptions Silent()
        {
            this.VerboseLogging = false;
            this.TraceLogging = false;
            return this;   
        }

        /// <summary>
        /// set verbose logging
        /// </summary>
        /// <returns>compiler options</returns>
        public BlastCompilerOptions Verbose()
        {
            this.VerboseLogging = true;
            this.TraceLogging = false;
            return this;
        }

        /// <summary>
        /// set trace logging
        /// </summary>
        /// <returns>compiler options</returns>
        public BlastCompilerOptions Trace()
        {
            this.VerboseLogging = true;
            this.TraceLogging = true;
            return this;
        }


        /// <summary>
        /// add a compiled define
        /// </summary>
        /// <param name="key">defined name</param>
        /// <param name="value">value</param>
        /// <param name="allow_overwrite_existing"></param>
        /// <returns>compiler options</returns>
        public BlastCompilerOptions AddDefine(string key, string value, bool allow_overwrite_existing = false)
        {
            Assert.IsNotNull(Defines);
            Assert.IsFalse(string.IsNullOrEmpty(key));
            Assert.IsFalse(string.IsNullOrEmpty(value));

            key = key.ToLower().Trim();
            if (!Defines.ContainsKey(key))
            {
                // set new
                Defines.Add(key, value);
            }
            else
            {
                Assert.IsTrue(allow_overwrite_existing, $"key {key} is already defined"); 

                // update existing
                Defines[key] = value;
            }
            return this;
        }


        /// <summary>
        /// Check if the key is defined in compiler options 
        /// </summary>
        /// <param name="key">the case insensitive key to check</param>
        /// <returns>true if its defined</returns>
        public bool IsDefined(string key)
        {
            Assert.IsNotNull(Defines);
            if (string.IsNullOrEmpty(key)) return false;

            key = key.Trim().ToLowerInvariant(); 

            return Defines.ContainsKey(key); 
        }

        /// <summary>
        /// Try to lookup a key and get its value
        /// </summary>
        /// <param name="key">case insensitive key</param>
        /// <param name="value">the value if present</param>
        /// <returns>true if the key was defined</returns>
        public bool TryGetDefine(string key, string value)
        {
            Assert.IsNotNull(Defines);
            if (string.IsNullOrEmpty(key)) return false;

            key = key.Trim().ToLowerInvariant();

            return Defines.TryGetValue(key, out value);
        }

        /// <summary>
        /// remove all defines from compiler options 
        /// </summary>
        public void ClearDefines()
        {
            Assert.IsNotNull(Defines);
            Defines.Clear(); 
        }

        /// <summary>
        /// Remove any value possibly assosiated with the key
        /// </summary>
        /// <param name="key">case insensitive key</param>
        /// <returns>true if the key was defined and subsequently removed</returns>
        public bool RemoveDefine(string key)
        {
            Assert.IsNotNull(Defines);
            return Defines.Remove(key); 
        }

        #endregion 
    }

}


