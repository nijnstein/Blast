using NSS.Blast;
using NSS.Blast.Compiler;
using NSS.Blast.SSMD;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Unity.Mathematics;

public class Program
{
    public static void Main(string[] args)
    {
        Console.WriteLine("".PadLeft(270, '='));
        Console.WriteLine("= Blast Test Console - Copyright © 2022 - Nijnstein Software".PadRight(269, ' ') + "=");
        Console.WriteLine("".PadLeft(270, '='));

        int n_runs;

#if DEBUG
        n_runs = 1000; 
        Console.WriteLine(">");
        Console.WriteLine("> DEBUG BUILD: C#/.NET");
        Console.WriteLine($"> executing {n_runs} cycles");
        Console.WriteLine(">");
#else
        n_runs = 1000 * 1000 * 100;
        Console.WriteLine(">");
        Console.WriteLine("> RELEASE BUILD: C#/.NET");
        Console.WriteLine("> ");
        Console.WriteLine($"> Benchmarking {n_runs} cycles (8 cores, 16 threads)");
        Console.WriteLine($"> - using {n_runs / 20} runs for slower tests"); 
        Console.WriteLine(">");
        Console.WriteLine("> <ENTER> to start"); 
        Console.WriteLine(">");
        Console.ReadLine();
#endif



        Console.WriteLine($"> Benchmark   Script".PadRight(121) + "Default".PadRight(23) + "Optimized".PadRight(14) + "Optimized-MT".PadRight(17) + "Opt-ST vs   " + "      SSMD".PadRight(21) + "      SSMD-MT".PadRight(16) + "     SSMD TLS".PadRight(15) + "   SSMD-TLS-MT".PadRight(15));

        Benchmark1("01", "a = 8;", n_runs, true);
        Benchmark1("02", "a = (2 2);", n_runs, true);
        Benchmark1("03", "a = (2 2 2);", n_runs, true);
        Benchmark1("04", "a = (2 2 2 2);", n_runs, true);

        Benchmark1("05", "a = 1 * 2;", n_runs, true);
        Benchmark1("06", "a = 4 * -22;", n_runs, true);
        Benchmark1("07", "a = 4 / 2;", n_runs, true);
        Benchmark1("08", "a = 4 / 2.2;", n_runs, true);

        Benchmark1("09", "a = 1 * 2; a = a + 3", n_runs, true);
        Benchmark1("10", "a = 1 * 2; a = a + 3; a = a / 8;", n_runs, true);
        Benchmark1("11", "a = 1 * 2; a = a + 3; a = a / 8; a = a - 20;", n_runs, true);
        Benchmark1("12", "a = 1 * 2; a = a + 3; a = a / 8; a = a - 20; a = a * 3.3;", n_runs, true);

        Benchmark1("13", "a = 1 * 2; b = a + 3", n_runs, true);
        Benchmark1("14", "a = 1 * 2; b = a + 3; c = b / 8;", n_runs, true);
        Benchmark1("15", "a = 1 * 2; b = a + 3; c = b / 8; d = c - 20;", n_runs, true);
        Benchmark1("16", "a = 1 * 2; b = a + 3; c = b / 8; d = c - 20; e = d * 3.3;", n_runs, true);

        Benchmark1("17", "a = 34 * 555 * 2 + 10.2;", n_runs, true);
        Benchmark1("18", "a = 34 * 55 * 0.33 * 555 * 2 + 10.2;", n_runs, true);
        Benchmark1("19", "a = 34 * 55 * 44 * 555 * 2 * 8 + 10.2;", n_runs, true);
        Benchmark1("20", "a = 34 * 55 * 44 * 555 * 2 * 8 * 9 + 10.2;", n_runs, true);


        Benchmark1("21", "a = (23 34 33) * (55 555 2) + 10.2;", n_runs, true);
        Benchmark1("22", "a = (66 34 33.6) * (333 34 3) * (55 555 2) + 10.2;", n_runs, true);
        Benchmark1("23", "a = (23 4 3) * (3 3 33) *(2 3 3) * (5 55 2) + 10.2;", n_runs, true);
        Benchmark1("24", "a = (23 4 3) * (3 3 33) *(2 3 3) * (5 55 2) * (5 55 2) + 10.2;", n_runs, true);
        Benchmark1("25", "a = (23 1 4 3) * (3 1 3 33) *(2 1 3 3) * (5 1 55 2) + 10.2;", n_runs, true);
        Benchmark1("26", "a = (1 4 3) * (1 3 33) * (1 3 3) * (5 155 2) + 10.2;", n_runs, true);
        Benchmark1("27", "a = (1 3) * (1 33) * (1 33) * (5 2) + 10.2;", n_runs, true);
        

        Benchmark1("fma", "a = 2 * 3 + 4;", n_runs, true);
        Benchmark1("fma coded", "a = fma(2, 3, 4);", n_runs, true);

        Benchmark1("vector2", "a = (3 3) * (4 4) + (1.2 1.2);", n_runs, true);
        Benchmark1("vector2 fma", "a = fma((3 3), (4 4), (1.2 1.2));", n_runs, true);

        Benchmark1("vector3", "a = (3 3 3) * (4 4 3) + (1.2 1.2 2);", n_runs, true);
        Benchmark1("vector3 fma", "a = fma((3 3 3), (4 4 3), (1.2 1.2 2));", n_runs, true);

        Benchmark1("vector4", "a = (3 3 3 3) * (4 4 4 4) + (1.2 1.2 4 4);", n_runs, true);
        Benchmark1("vector4 fma", "a = fma((3 3 3 3), (4 4 4 4), (1.2 1.2 1.2 1.2));", n_runs, true);

        Benchmark1("mul", "a = 1 * 2 * 3 * 4 * 5 * 6 * 7;", n_runs);
        Benchmark1("mula", "a = mula(1, 2, 3, 4, 5, 6, 7);", n_runs);

        Benchmark1("mul", "a = (3 3 3) * (4 4 4) * (1.2 1.2 1.2) * (9.9 9.1 9.2) * (5 5 3) * (92 22 4455);", n_runs);
        Benchmark1("mula", "a = mula((3 3 3), (4 4 4), (1.2 1.2 1.2), (9.9 9.1 9.2), (5 5 3), (92 22 4455));", n_runs);

        Benchmark1("mul", "a = (3 3 3 5) * (4 4 4 9) * (1.2 1 1.2 4) * (9.9 9.1 9 5) * (5 5 3 5) * (92 2 4 5);", n_runs);
        Benchmark1("mula", "a = mula((3 3 3 5), (4 4 4 9), (1.2 1 1.2 4), (9.9 9.1 9 5), (5 5 3 5), (92 2 4 5));", n_runs);

        Benchmark1("lerp", "a = lerp((100 1 9 2), (200 1 99 32), (6.7 6.7 6.7 6.7));", n_runs);

        Benchmark1("while", "a = 1000; b = 0; while(b < 10) ( a = a - 1; b = b + 1;);", n_runs);

        Benchmark1("switch", "a = 100; switch(a > b)( case 100: (b = 0.99;) default: (b = 1.22;) );", n_runs);
        Benchmark1("ifthen", "a = 1; if(a == 1) then (a = 12;) else (a = 44);", n_runs);

        Benchmark1("for", "h = 3; for(i = 1; i < 10; i = i + 1)( h = h + 1; );", n_runs);
        Benchmark1("for_m", "h = 3; for(i = 1; i < 10; i = i + 1)( h = h + 1; h = sqrt(h); );", n_runs);
        Benchmark1("for_vec", "h = (3 3 3); for(i = 1; i < 10; i = i + 1)( h = sqrt(h + 1); );", n_runs);
        Benchmark1("while2", "a = 1; b = 10; c = 1; while(a <= b) (a = a + 1; c = c * 1 * 2 * 3 * 1/100 * 1;);", n_runs);



        Console.ReadLine();
    }


    static void Benchmark1(string name, string code, int n_runs = 10000, bool ssmd = false)
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
        
        
        ConsoleColor old = Console.ForegroundColor;
        void writepp(double a, double b)
        {
            double pp = (a / (b / 100.0f)) - 100f; 
            Console.ForegroundColor = pp >= 0 ? ConsoleColor.Green : ConsoleColor.Red;
            Console.Write($"{pp.ToString("0")}%".PadLeft(5));
            Console.ForegroundColor = old;
        }

        void writemem(double m)
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write($"{m.ToString("0")}byte".PadLeft(8));
            Console.ForegroundColor = old;
        }


        double ys_normal, ys_opt, ys_opt_mt, ys_ssmd, ys_ssmd_mt, ys_ssmd_tlc, ys_ssmd_tlc_mt;
        double mem_normal, mem_opt, mem_opt_mt, mem_ssmd, mem_ssmd_mt, mem_ssmd_tlc, mem_ssmd_tlc_mt;

        if (!Benchmark(blast, script, non_optimizing_options, n_runs, false, out ys_normal, out mem_normal)) return;
        writemem(mem_normal);
        Console.Write($"{(ys_normal.ToString("0.00").PadLeft(8) + "ns").PadRight(12)}");

        if (!Benchmark(blast, script, optimizing_options, n_runs, false, out ys_opt, out mem_opt)) return;
        writepp(ys_normal, ys_opt);
        writemem(mem_opt);
        Console.Write($"{(ys_opt.ToString("0.00").PadLeft(8) + "ns").PadRight(12)}");

        if (!Benchmark(blast, script, optimizing_options, n_runs, true, out ys_opt_mt, out mem_opt_mt)) return;
        writepp(ys_opt, ys_opt_mt);
        Console.Write($"{(ys_opt_mt.ToString("0.00").PadLeft(8) + "ns").PadRight(12)}");

        if (ssmd)
        {
            optimizing_options.PackageMode = BlastPackageMode.SSMD;
            optimizing_options.DefaultStackSize = 64;
            optimizing_options.EstimateStackSize = true;  // forces validations to run we dont want that while testing
            optimizing_options.PackageStack = false;

            if (!Benchmark(blast, script, optimizing_options, n_runs, false, out ys_ssmd, out mem_ssmd)) return;

            Console.Write(" -  ");
            writepp(ys_opt, ys_ssmd);
            writemem(mem_ssmd);
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write($"{(ys_ssmd.ToString("0.00").PadLeft(6) + "ns").PadRight(6)}");

            if (!Benchmark(blast, script, optimizing_options, n_runs, true, out ys_ssmd_mt, out mem_ssmd_mt)) return;

            Console.Write("   ");
            writepp(ys_ssmd, ys_ssmd_mt);
            writemem(mem_ssmd_mt); 
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write($"{(ys_ssmd_mt.ToString("0.00").PadLeft(6) + "ns").PadRight(6)}");
            Console.ForegroundColor = old;

            optimizing_options.PackageStack = true;

            if (!Benchmark(blast, script, optimizing_options, n_runs, false, out ys_ssmd_tlc, out mem_ssmd_tlc)) return;

            Console.Write("   ");
            writepp(ys_ssmd, ys_ssmd_tlc);
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write($"{(ys_ssmd_tlc.ToString("0.00").PadLeft(6) + "ns").PadRight(6)}");

            if (!Benchmark(blast, script, optimizing_options, n_runs, true, out ys_ssmd_tlc_mt, out mem_ssmd_tlc_mt)) return;

            Console.Write("   ");
            writepp(ys_ssmd_mt, ys_ssmd_tlc_mt);
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write($"{(ys_ssmd_tlc_mt.ToString("0.00").PadLeft(6) + "ns").PadRight(6)}");
            Console.ForegroundColor = old;
        }


        Console.WriteLine(); 

        blast.Destroy();
    }




    static bool Benchmark(Blast blast, BlastScript script, BlastCompilerOptions options, int n_runs, bool parallel, out double ns, out double mem)
    {
        ns = 0;
        mem = 0; 

        var res = BlastCompiler.Compile(blast, script, options);
        if (!res.Success)
        {
            Console.WriteLine($"ERROR: {res.LastErrorMessage}");
            Console.WriteLine("\n<ENTER> to exit");
            Console.ReadLine();
            return false;
        }
        Stopwatch sw_execute = new Stopwatch();


        if (options.PackageMode == BlastPackageMode.SSMD)
        {
            // ssmd package: by n 
            BlastPackageData pkg = BlastCompiler.Package(res, options);
            mem = pkg.DataSegmentSize; 

            BlastSSMDInterpretor blaster = default;
            
            blaster.SetPackage(pkg);

            unsafe {
                int n_stacks = Math.Min(n_runs, 1024);
                byte* stack = stackalloc byte[n_stacks * pkg.SSMDDataSize];
                BlastSSMDDataStack* ssmd_data = (BlastSSMDDataStack*)stack;

                pkg.CloneDataStack(n_stacks, ssmd_data);

                // warmup before benchmark 
                blaster.Execute((BlastEngineData*)blast.Engine, IntPtr.Zero, ssmd_data, n_stacks);

                sw_execute.Start();

                if (parallel)
                {
                    int np = n_runs / n_stacks;
                
                    Parallel.For(0, np, (x) =>
                    {
                        BlastSSMDInterpretor blaster2 = default;
                        blaster2.SetPackage(pkg);
                        blaster2.Execute((BlastEngineData*)blast.Engine, IntPtr.Zero, ssmd_data, n_stacks);
                    }); 
                }
                else
                {
                    int i = 0;
                    while (i < n_runs - n_stacks)
                    {
                        i += n_stacks;
                        blaster.Execute((BlastEngineData*)blast.Engine, IntPtr.Zero, ssmd_data, n_stacks);
                    }
                    if (i < n_runs)
                    {
                        blaster.Execute((BlastEngineData*)blast.Engine, IntPtr.Zero, ssmd_data, n_runs - i);
                    }
                }
                sw_execute.Stop();
            }                                 
        }
        else
        {
            // normal package: 1 by 1 
            // slow
            n_runs = n_runs / 20; 

            // warmup run
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

            sw_execute.Start();
            if (parallel)
            {
                Parallel.For(0, n_runs, (x) => 
                {
                    int exitcode = res.Executable.Execute(blast.Engine);
                    if(exitcode != (int)BlastError.success)
                    {
                        throw new Exception($"error benching, exitcode: {exitcode}");
                    }
                });
            }
            else
            {
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
            }
            sw_execute.Stop();

            mem = res.Executable.code_size;
            mem += res.Executable.DataCount + res.Executable.max_stack_size;

            while (mem % 8 != 0) mem++;

            mem += res.Executable.DataCount * 4 + res.Executable.max_stack_size * 4;
        }



        var total_ms = sw_execute.Elapsed.TotalMilliseconds;
        ns = (total_ms / (float)n_runs) * 1000.0f * 1000f;

        return true; 
    }

}