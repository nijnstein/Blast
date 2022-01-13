using NSS.Blast;
using NSS.Blast.Compiler;
using System;
using System.Diagnostics;

public class Program
{
    public static void Main(string[] args)
    {
        Console.WriteLine("".PadLeft(134, '='));
        Console.WriteLine("= Blast Test Console - Copyright © 2022 - Nijnstein Software".PadRight(133, ' ') + "=");
        Console.WriteLine("".PadLeft(134, '='));

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
        Console.WriteLine("> <ENTER> to start"); 
        Console.WriteLine(">");
        Console.ReadLine();   
#endif

        Console.WriteLine($"> Benchmark   Script".PadRight(110) + "Default".PadRight(12) + "Optimized".PadRight(12));

        Benchmark1("1", "a = 34 * 55 * 555 * 2 + 10.2;", n_runs);
        Benchmark1("2", "a = (23 34 33) * (55 555 2) + 10.2;", n_runs);
        Benchmark1("fma", "a = 2 * 3 + 4;", n_runs);
        Benchmark1("fma coded", "a = fma(2, 3, 4);", n_runs);
        Benchmark1("vector", "a = (3 3 3) * (4 4 4) + (1.2 1.2 1.2);", n_runs);
        Benchmark1("vector fma", "a = fma((3 3 3), (4 4 4), (1.2 1.2 1.2));", n_runs);

        Benchmark1("mul", "a = 1 * 2 * 3 * 4 * 5 * 6 * 7;", n_runs);
        Benchmark1("mula", "a = mula(1, 2, 3, 4, 5, 6, 7);", n_runs);

        Benchmark1("mul", "a = (3 3 3) * (4 4 4) * (1.2 1.2 1.2) * (9.9 9.1 9.2) * (5 5 3) * (92 22 4455);", n_runs);
        Benchmark1("mula", "a = mula((3 3 3), (4 4 4), (1.2 1.2 1.2), (9.9 9.1 9.2), (5 5 3), (92 22 4455));", n_runs);

        Benchmark1("mul", "a = (3 3 3 5) * (4 4 4 9) * (1.2 1 1.2 4) * (9.9 9.1 9 5) * (5 5 3 5) * (92 2 4 5);", n_runs);
        Benchmark1("mula", "a = mula((3 3 3 5), (4 4 4 9), (1.2 1 1.2 4), (9.9 9.1 9 5), (5 5 3 5), (92 2 4 5));", n_runs);

        Benchmark1("lerp", "a = lerp((100 1 9 2), (200 1 99 32), (6.7 6.7 6.7 6.7));", n_runs);

        Benchmark1("while", "a = 1000; b = 0; while(b < 1000) ( a = a - 1; b = b + 1;);");

        Benchmark1("switch", "a = 100; switch(a > b)( case 100: (b = 0.99;) default: (b = 1.22;) );");
        Benchmark1("ifthen", "a = 1; if(a == 1) then (a = 12;) else (a = 44);");

        Benchmark1("for", "h = 3; for(i = 1; i < 100; i = i + 1)( h = h + 1; );");
        Benchmark1("for_m", "h = 3; for(i = 1; i < 10; i = i + 1)( h = h + 1; h = sqrt(h); );");
        Benchmark1("for_vec", "h = (3 3 3); for(i = 1; i < 100; i = i + 1)( h = sqrt(h + 1); );");
        Benchmark1("while2", "a = 1; b = 100; c = 1; while(a <= b) (a = a + 1; c = c * 1 * 2 * 3 * 1/100 * 1;);");



        Console.ReadKey();
    }


    static void Benchmark1(string name, string code, int n_runs = 10000)
    {
        Console.Write($"> {name.ToUpper().PadRight(12)} => {code}".PadRight(110));

        Blast blast = Blast.Create(Unity.Collections.Allocator.Persistent);

        BlastScript script = BlastScript.FromText(code);

        var non_optimizing_options = new BlastCompilerOptions();
        non_optimizing_options.AutoValidate = false;
        non_optimizing_options.DefaultStackSize = 64;
        non_optimizing_options.EstimateStackSize = false;  // forces validations to run we dont want that while testing
        non_optimizing_options.Optimize = false; 
       
        var optimizing_options = new BlastCompilerOptions();
        optimizing_options.AutoValidate = false;
        optimizing_options.DefaultStackSize = 64;
        optimizing_options.EstimateStackSize = false;  // forces validations to run we dont want that while testing
        optimizing_options.Optimize = true; 

        non_optimizing_options.AddDefine("TEST_COMPILER_DEFINE", "666,66");

        double ys;

        if (!Benchmark(blast, script, non_optimizing_options, n_runs, out ys)) return; 
        Console.Write($"{(ys.ToString("0.000").PadLeft(8) + "ys").PadRight(12)}");

        if (!Benchmark(blast, script, optimizing_options, n_runs, out ys)) return;
        Console.WriteLine($"{(ys.ToString("0.000").PadLeft(8) + "ys").PadRight(12)}");

        blast.Destroy();
    }

    static bool Benchmark(Blast blast, BlastScript script, BlastCompilerOptions options, int n_runs, out double ys)
    {
        ys = 0;

        var res = BlastCompiler.Compile(blast, script, options);
        if (!res.Success)
        {
            Console.WriteLine($"ERROR: {res.LastErrorMessage}");
            Console.WriteLine("\n<ENTER> to exit");
            Console.ReadLine();
            return false;
        }

        for (int i = 0; i < 10; i++)
        {
            int exitcode = res.Executable.Execute(blast.Engine);
            if (exitcode != 0)
            {
                Console.WriteLine($"ERROR: exitcode = {exitcode}");
                Console.WriteLine("\n<ENTER> to exit");
                Console.ReadLine();
                return false;
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
                return false;
            }
        }
        sw_execute.Stop();

        var total_ms = sw_execute.Elapsed.TotalMilliseconds;
        ys = (total_ms / (float)n_runs) * 1000.0f;

        return true; 
    }

}