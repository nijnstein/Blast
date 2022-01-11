using NSS.Blast;
using NSS.Blast.Compiler;
using System;
using System.Diagnostics;

public class Program
{
    public static void Main(string[] args)
    {
        Console.WriteLine("".PadLeft(80, '='));
        Console.WriteLine("= Blast Test Console - Copyright © 2022 - Nijnstein Software".PadRight(79, ' ') + "=");
        Console.WriteLine("".PadLeft(80, '='));

        int n_runs;

#if DEBUG
        n_runs = 1000; 
        Console.WriteLine(">");
        Console.WriteLine("> DEBUG BUILD: C#/.NET");
        Console.WriteLine($"> executing {n_runs} cycles");
        Console.WriteLine(">");
#else
        n_runs = 1000 * 1000;
        Console.WriteLine(">");
        Console.WriteLine("> RELEASE BUILD: C#/.NET");
        Console.WriteLine($"> executing {n_runs} cycles");
        Console.WriteLine(">");
#endif 

        Benchmark1("1", "a = 34 * 55 * 555 * 2 + 10.2;", n_runs);
        Benchmark1("2", "a = (23 34 33) * (55 555 2) + 10.2;", n_runs);
        Benchmark1("fma optimizer", "a = 2 * 3 + 4;", n_runs);
        Benchmark1("fma coded", "a = fma(2, 3, 4);", n_runs);
        Benchmark1("fma optimizer", "a = (3 3 3) * (4 4 4) + (1.2 1.2 1.2);", n_runs);
        Benchmark1("fma coded", "a = fma((3 3 3), (4 4 4), (1.2 1.2 1.2));", n_runs);

        Benchmark1("mul", "a = 1 * 2 * 3 * 4 * 5 * 6 * 7;", n_runs);
        Benchmark1("mula", "a = mula(1, 2, 3, 4, 5, 6, 7);", n_runs);

        Benchmark1("mul", "a = (3 3 3) * (4 4 4) * (1.2 1.2 1.2) * (9.9 9.1 9.2) * (5 5 3) * (92 22 4455);", n_runs);
        Benchmark1("mula", "a = mula((3 3 3), (4 4 4), (1.2 1.2 1.2), (9.9 9.1 9.2), (5 5 3), (92 22 4455));", n_runs);

        Benchmark1("mul", "a = (3 3 3 5) * (4 4 4 9) * (1.2 1 1.2 4) * (9.9 9.1 9 5) * (5 5 3 5) * (92 2 4 5);", n_runs);
        Benchmark1("mula", "a = mula((3 3 3 5), (4 4 4 9), (1.2 1 1.2 4), (9.9 9.1 9 5), (5 5 3 5), (92 2 4 5));", n_runs);

        Benchmark1("lerp", "a = lerp((100 1 9 2), (200 1 99 32), (6.7 6.7 6.7 6.7));", n_runs);

        Benchmark1("loop", "a = 1000; b = 0; while(b < 1000) ( a = a - 1; b = b + 1;);");

        Console.ReadKey();
    }


    static void Benchmark1(string name, string code, int n_runs = 10000)
    {
        Console.Write($"> Benchmark: {name.ToUpper()} => {code}".PadRight(110));

        Blast blast = Blast.Create(Unity.Collections.Allocator.Persistent);

        BlastScript script = BlastScript.FromText(code);

        var options = new BlastCompilerOptions();
        options.AutoValidate = false;
        options.Optimize = true;
        options.CompileWithSystemConstants = true;
        options.ConstantEpsilon = 0.001f;
        options.DefaultStackSize = 64;
        options.EstimateStackSize = false;  // forces validations to run we dont want that while testing

        options.AddDefine("TEST_COMPILER_DEFINE", "666,66");

        var res = BlastCompiler.Compile(blast, script, options);
        if (!res.Success)
        {
            Console.WriteLine($"ERROR: {res.LastErrorMessage}");
            Console.WriteLine("\n<ENTER> to exit");
            Console.ReadLine(); 
            return; 
        }

        for (int i = 0; i < 10; i++)
        {
            int exitcode = res.Executable.Execute(blast.Engine);
            if (exitcode != 0)
            {
                Console.WriteLine($"ERROR: exitcode = {exitcode}");
                Console.WriteLine("\n<ENTER> to exit");
                Console.ReadLine();
                return;
            }
        }

        Stopwatch sw_execute = new Stopwatch();

        sw_execute.Start();
        for (int i = 0; i < n_runs; i++)
        {
            int exitcode = res.Executable.Execute(blast.Engine);
            if (exitcode != 0)
            {
                Console.WriteLine($"ERROR: run {i}/{n_runs} exitcode = {exitcode}");
                Console.WriteLine("\n<ENTER> to exit");
                Console.ReadLine();
                return;
            }
        }
        sw_execute.Stop();

        var total_ms = sw_execute.Elapsed.TotalMilliseconds;
        var ys_per_run = (total_ms / (float)n_runs) * 1000.0f; 
        
        Console.WriteLine($"{ys_per_run.ToString("0.000")} ys");
        blast.Destroy();
    }
}