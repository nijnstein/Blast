using NSS.Blast;
using NSS.Blast.Compiler;
using System;
using System.Diagnostics;

public class Program
{
    public static void Main(string[] args)
    {
        Console.WriteLine("".PadLeft(80, '='));
        Console.WriteLine("= Blast Test Console - Copyright © 2020 - Nijnstein Sofware".PadRight(79, ' ') + "=");
        Console.WriteLine("".PadLeft(80, '='));

        int n_runs;

#if DEBUG
        n_runs = 1000; 
        Console.WriteLine(">");
        Console.WriteLine("> DEBUG BUILD");
        Console.WriteLine(">");
#else
        n_runs = 1000 * 100;
        Console.WriteLine(">");
        Console.WriteLine("> RELEASE BUILD");
        Console.WriteLine(">");
#endif 

        Benchmark1("BM1", "a = 34 * 55 * 555 * 2 + 10.2;", n_runs); 
    }


    static void Benchmark1(string name, string code, int n_runs = 10000)
    {
        Console.WriteLine($"> Benchmark: {name}");
        Console.WriteLine($"> Script: {code}");
        Console.WriteLine("> ");
        Console.Write("> starting... ");

        Blast blast = Blast.Create(Unity.Collections.Allocator.Persistent);
        blast.SyncConstants();

        BlastScript script = BlastScript.FromText(code);

        var options = new BlastCompilerOptions();
        options.AutoValidate = false;
        options.Optimize = true;
        options.CompileWithSystemConstants = true;
        options.ConstantEpsilon = 0.001f;
        options.DefaultStackSize = 64;
        options.EstimateStackSize = false;  // forces validations to run we dont want that while testing

        options.AddDefine("TEST_COMPILER_DEFINE", "666,66");

        Console.WriteLine("done."); 
        Console.Write("> compiling..."); 

        var res = BlastCompiler.Compile(blast, script, options);
        if (!res.Success)
        {
            Console.WriteLine($"ERROR: {res.LastErrorMessage}");
            Console.WriteLine("\n<any key> to exit");
            Console.ReadKey(); 
            return; 
        }

        Console.WriteLine("done.");
        Console.Write("> warming up benchmark...");

        for (int i = 0; i < 10; i++)
        {
            int exitcode = res.Executable.Execute(blast.Engine);
            if (exitcode != 0)
            {
                Console.WriteLine($"ERROR: exitcode = {exitcode}");
                Console.WriteLine("\n<any key> to exit");
                Console.ReadKey();
                return;
            }
        }

        Stopwatch sw_execute = new Stopwatch();
        
        Console.WriteLine("done.");
        Console.Write($"> benchmarking {n_runs} runs...");

        sw_execute.Start();
        for (int i = 0; i < n_runs; i++)
        {
            int exitcode = res.Executable.Execute(blast.Engine);
            if (exitcode != 0)
            {
                Console.WriteLine($"ERROR: run {i}/{n_runs} exitcode = {exitcode}");
                Console.WriteLine("\n<any key> to exit");
                Console.ReadKey();
                return;
            }
        }
        sw_execute.Stop();

        var total_ms = sw_execute.Elapsed.TotalMilliseconds;
        var ys_per_run = (total_ms / (float)n_runs) * 1000.0f; 
        
        Console.WriteLine("done.");
        Console.WriteLine($"> single thread c#/.NET: duration = {total_ms.ToString("0.000")}ms, {ys_per_run.ToString("0.000")} ys per run.");
        Console.WriteLine("\n<any key> to exit");

        Console.ReadKey();
        blast.Destroy();
    }
}