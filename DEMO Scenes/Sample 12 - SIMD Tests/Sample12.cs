using NSS.Blast;
using NSS.Blast.Compiler;
using NSS.Blast.SSMD;
using ScriptTestJobs;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.SceneManagement;
using vectorization_tests;

public class Sample12 : MonoBehaviour
{

    public TMPro.TMP_Text View;

    public int DataCount = 64;
    public int RepeatCount = 1024 * 128;




    /// overview of data after execution 
    /// </summary>
    [TextArea(10, 32)]
    public string DataView = "";

    public bool Once = true; 

    // Start is called before the first frame update
    void Start()
    {
        View.text = $"Blast v{Blast.VersionString}\n\nPress [space] to run benchmark once, [B] to run it 100x, [C]ubes, [E]ditor";
    }

    private void OnDestroy()
    {
        
    }


    // Update is called once per frame
    unsafe void Update()
    {
        if(!Blast.IsInstantiated)
        {
            Blast.Initialize(); 
        }
        if(Input.GetKeyDown(KeyCode.Escape))
        {
            Application.Quit(); 
        }
        if (Input.GetKeyDown(KeyCode.C))
        {
            SceneManager.LoadScene("Sample10");
        }
        if (Input.GetKeyDown(KeyCode.E))
        {
            SceneManager.LoadScene("Sample13");
        }
        if (Once || (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.B)))
        try{
            Once = false;
            if(Input.GetKeyDown(KeyCode.B)) RepeatCount *= 100;
            var s = StringBuilderCache.Acquire();

            s.Append($"BLAST v{Blast.VersionString}\n");
            s.Append($"Array operations benchmark, repeats: {RepeatCount}, array length: {DataCount}, {(simd.IsUnrolled ? "[unrolled]" : "")}\n\n");

            s.Append(FormatHeaderForArrayTest());
            s.Append(FormatScore("mul_array_f4f4",                    "f4.xyzw = f4.xyzw * f4.xyzw",        bench(new test_mul_array_f4f4() { repeatcount = RepeatCount, datacount = DataCount })));
            s.Append(FormatScore("mul_array_f4f4_constant",           "f4.xyzw = f4.xyzw * constant",       bench(new test_mul_array_f4f4_constant() { repeatcount = RepeatCount, datacount = DataCount })));
            s.Append(FormatScore("mul_array_f4f4_indexed",            "f4.xyzw = f4.xyzw * [][].xyzw",      bench(new test_mul_array_f4f4_indexed() { repeatcount = RepeatCount, datacount = DataCount })));
            s.Append(FormatScore("mul_array_f4f4_indexed_unaligned",  "f4.xyzw = f4.xyzw * [][].xyzw",      bench(new test_mul_array_f4f4_indexed_unaligned() { repeatcount = RepeatCount, datacount = DataCount })));
            s.Append(FormatScore("mul_array_f4f1",                    "f4.xyzw = f4.xyzw * f4.x",           bench(new test_mul_array_f4f1() { repeatcount = RepeatCount, datacount = DataCount })));
            s.Append(FormatScore("mul_array_f4f1_constant",           "f4.xyzw = f1.x    * constant",       bench(new test_mul_array_f4f1_constant() { repeatcount = RepeatCount, datacount = DataCount })));
            s.Append(FormatScore("mul_array_f3f3_indexed",            "f3.xyz  = f3.xyz  * [][].xyz",       bench(new test_mul_array_f3f3_indexed() { repeatcount = RepeatCount, datacount = DataCount })));
            s.Append(FormatScore("mul_array_f2f2_indexed",            "f2.xy   = f2.xy   * [][].xy",        bench(new test_mul_array_f2f2_indexed() { repeatcount = RepeatCount, datacount = DataCount })));
            s.Append(FormatScore("mul_array_f1f1",                    "f4.x    = f4.x    * f4.x",           bench(new test_mul_array_f1f1() { repeatcount = RepeatCount, datacount = DataCount })));
            s.Append(FormatScore("mul_array_f1f1_constant",           "f4.x    = f4.x    * constant",       bench(new test_mul_array_f1f1_constant() { repeatcount = RepeatCount, datacount = DataCount })));
            s.Append(FormatScore("mul_array_f1f1_indexed",            "f1.x    = f1.x    * [][].x",         bench(new test_mul_array_f1f1_indexed() { repeatcount = RepeatCount, datacount = DataCount })));
            s.Append(FormatScore("mul_array_f1f1_indexed_unaligned",  "f1.x    = f1.x    * [][].x",         bench(new test_mul_array_f1f1_indexed_unaligned() { repeatcount = RepeatCount, datacount = DataCount })));
            
            s.Append(FormatScore("V1 NORMAL f1",    "a = 1;", bench("a = 1;"), 1));
            s.Append(FormatScore("V1 SSMD f1 1",    "a = 1;", bench("a = 1;", 1), 1));
            s.Append(FormatScore("V1 SSMD f1 64",   "a = 1;", bench("a = 1;", 64), 64));
            s.Append(FormatScore("V1 SSMD f1 512",  "a = 1;", bench("a = 1;", 512), 512));
            s.Append(FormatScore("V1 SSMD f1 4096", "a = 1;", bench("a = 1;", 4096), 4096));
            s.Append(FormatScore("V1 SSMD f1 8192", "a = 1;", bench("a = 1;", 8192), 8192));
            
            s.Append(FormatScore("V2 NORMAL f2",    "a = (1 1);", bench("a = (1 1);"), 1));
            s.Append(FormatScore("V2 SSMD f2 1",    "a = (1 1);", bench("a = (1 1);", 1), 1));
            s.Append(FormatScore("V2 SSMD f2 64",   "a = (1 1);", bench("a = (1 1);", 64), 64));
            s.Append(FormatScore("V2 SSMD f2 512",  "a = (1 1);", bench("a = (1 1);", 512), 512));
            s.Append(FormatScore("V2 SSMD f2 4096", "a = (1 1);", bench("a = (1 1);", 4096), 4096));
            s.Append(FormatScore("V2 SSMD f2 8192", "a = (1 1);", bench("a = (1 1);", 8192), 8192));
            
            s.Append(FormatScore("V3 NORMAL f3",    "a = (1 1 1);", bench("a = (1 1 1);"), 1));
            s.Append(FormatScore("V3 SSMD f3 1",    "a = (1 1 1);", bench("a = (1 1 1);", 1), 1));
            s.Append(FormatScore("V3 SSMD f3 64",   "a = (1 1 1);", bench("a = (1 1 1);", 64), 64));
            s.Append(FormatScore("V3 SSMD f3 512",  "a = (1 1 1);", bench("a = (1 1 1);", 512), 512));
            s.Append(FormatScore("V3 SSMD f3 4096", "a = (1 1 1);", bench("a = (1 1 1);", 4096), 4096));
            s.Append(FormatScore("V3 SSMD f3 8192", "a = (1 1 1);", bench("a = (1 1 1);", 8192), 8192));
            
            s.Append(FormatScore("V4 NORMAL f4",    "a = (1 1 1 1);", bench("a = (1 1 1 1);"), 1));
            s.Append(FormatScore("V4 SSMD f4 1",    "a = (1 1 1 1);", bench("a = (1 1 1 1);", 1), 1));
            s.Append(FormatScore("V4 SSMD f4 64",   "a = (1 1 1 1);", bench("a = (1 1 1 1);", 64), 64));
            s.Append(FormatScore("V4 SSMD f4 512",  "a = (1 1 1 1);", bench("a = (1 1 1 1);", 512), 512));
            s.Append(FormatScore("V4 SSMD f4 4096", "a = (1 1 1 1);", bench("a = (1 1 1 1);", 4096), 4096));
            s.Append(FormatScore("V4 SSMD f4 8192", "a = (1 1 1 1);", bench("a = (1 1 1 1);", 8192), 8192));
            
            s.Append(FormatScore("V1 NORMAL",    "a = 111 * 224 * 36 * 1.1;", bench("a = 111 * 224 * 36 * 1.1;"), 1));
            s.Append(FormatScore("V1 SSMD 1",    "a = 111 * 224 * 36 * 1.1;", bench("a = 111 * 224 * 36 * 1.1;", 1), 1));
            s.Append(FormatScore("V1 SSMD 16",   "a = 111 * 224 * 36 * 1.1;", bench("a = 111 * 224 * 36 * 1.1;", 16), 16));
            s.Append(FormatScore("V1 SSMD 32",   "a = 111 * 224 * 36 * 1.1;", bench("a = 111 * 224 * 36 * 1.1;", 32), 32));
            s.Append(FormatScore("V1 SSMD 64",   "a = 111 * 224 * 36 * 1.1;", bench("a = 111 * 224 * 36 * 1.1;", 64), 64));
            s.Append(FormatScore("V1 SSMD 128",  "a = 111 * 224 * 36 * 1.1;", bench("a = 111 * 224 * 36 * 1.1;", 128), 128));
            s.Append(FormatScore("V1 SSMD 256",  "a = 111 * 224 * 36 * 1.1;", bench("a = 111 * 224 * 36 * 1.1;", 256), 256));
            s.Append(FormatScore("V1 SSMD 512",  "a = 111 * 224 * 36 * 1.1;", bench("a = 111 * 224 * 36 * 1.1;", 512), 512));
            s.Append(FormatScore("V1 SSMD 1024", "a = 111 * 224 * 36 * 1.1;", bench("a = 111 * 224 * 36 * 1.1;", 1024), 1024));
            s.Append(FormatScore("V1 SSMD 2048", "a = 111 * 224 * 36 * 1.1;", bench("a = 111 * 224 * 36 * 1.1;", 2048), 2048));
            s.Append(FormatScore("V1 SSMD 4096", "a = 111 * 224 * 36 * 1.1;", bench("a = 111 * 224 * 36 * 1.1;", 4096), 4096));
            s.Append(FormatScore("V1 SSMD 8192", "a = 111 * 224 * 36 * 1.1;", bench("a = 111 * 224 * 36 * 1.1;", 8192), 8192));
                 
            s.Append(FormatScore("V2 NORMAL",    "a = (3.4 5.5) * (2 2) * (3 3) * (8 8);", bench("a = (3.4 5.5) * (2 2) * (3 3) * (8 8);"), 1));
            s.Append(FormatScore("V2 SSMD 1",    "a = (3.4 5.5) * (2 2) * (3 3) * (8 8);", bench("a = (3.4 5.5) * (2 2) * (3 3) * (8 8);", 1), 1));
            s.Append(FormatScore("V2 SSMD 16",   "a = (3.4 5.5) * (2 2) * (3 3) * (8 8);", bench("a = (3.4 5.5) * (2 2) * (3 3) * (8 8);", 16), 16));
            s.Append(FormatScore("V2 SSMD 32",   "a = (3.4 5.5) * (2 2) * (3 3) * (8 8);", bench("a = (3.4 5.5) * (2 2) * (3 3) * (8 8);", 32), 32));
            s.Append(FormatScore("V2 SSMD 64",   "a = (3.4 5.5) * (2 2) * (3 3) * (8 8);", bench("a = (3.4 5.5) * (2 2) * (3 3) * (8 8);", 64), 64));
            s.Append(FormatScore("V2 SSMD 128",  "a = (3.4 5.5) * (2 2) * (3 3) * (8 8);", bench("a = (3.4 5.5) * (2 2) * (3 3) * (8 8);", 128), 128));
            s.Append(FormatScore("V2 SSMD 256",  "a = (3.4 5.5) * (2 2) * (3 3) * (8 8);", bench("a = (3.4 5.5) * (2 2) * (3 3) * (8 8);", 256), 256));
            s.Append(FormatScore("V2 SSMD 512",  "a = (3.4 5.5) * (2 2) * (3 3) * (8 8);", bench("a = (3.4 5.5) * (2 2) * (3 3) * (8 8);", 512), 512));
            s.Append(FormatScore("V2 SSMD 1024", "a = (3.4 5.5) * (2 2) * (3 3) * (8 8);", bench("a = (3.4 5.5) * (2 2) * (3 3) * (8 8);", 1024), 1024));
            s.Append(FormatScore("V2 SSMD 2048", "a = (3.4 5.5) * (2 2) * (3 3) * (8 8);", bench("a = (3.4 5.5) * (2 2) * (3 3) * (8 8);", 2048), 2048));
            s.Append(FormatScore("V2 SSMD 4096", "a = (3.4 5.5) * (2 2) * (3 3) * (8 8);", bench("a = (3.4 5.5) * (2 2) * (3 3) * (8 8);", 4096), 4096));
            s.Append(FormatScore("V2 SSMD 8192", "a = (3.4 5.5) * (2 2) * (3 3) * (8 8);", bench("a = (3.4 5.5) * (2 2) * (3 3) * (8 8);", 8192), 8192));
            
            s.Append(FormatScore("V3 NORMAL",    "a = (3.4 5.5 1) * (2 4 2) * (5 3 3) * (7 8 8);", bench("a = (3.4 5.5 1) * (2 4 2) * (5 3 3) * (7 8 8);"), 1));
            s.Append(FormatScore("V3 SSMD 1",    "a = (2 2 2) * (2 2 2) * (2 2 2) * (2 2 2);", bench("a = (2 2 2) * (2 2 2) * (2 2 2) * (2 2 2);", 1), 1));
            s.Append(FormatScore("V3 SSMD 1",    "a = (3.4 5.5 1) * (2 4 2) * (5 3 3) * (7 8 8);", bench("a = (3.4 5.5 1) * (2 4 2) * (5 3 3) * (7 8 8);", 1), 1));
            s.Append(FormatScore("V3 SSMD 16",   "a = (3.4 5.5 1) * (2 4 2) * (5 3 3) * (7 8 8);", bench("a = (3.4 5.5 1) * (2 4 2) * (5 3 3) * (7 8 8);", 16), 16));
            s.Append(FormatScore("V3 SSMD 32",   "a = (3.4 5.5 1) * (2 4 2) * (5 3 3) * (7 8 8);", bench("a = (3.4 5.5 1) * (2 4 2) * (5 3 3) * (7 8 8);", 32), 32));
            s.Append(FormatScore("V3 SSMD 64",   "a = (3.4 5.5 1) * (2 4 2) * (5 3 3) * (7 8 8);", bench("a = (3.4 5.5 1) * (2 4 2) * (5 3 3) * (7 8 8);", 64), 64));
            s.Append(FormatScore("V3 SSMD 128",  "a = (3.4 5.5 1) * (2 4 2) * (3 3 3) * (7 8 8);", bench("a = (3.4 5.5 1) * (2 4 2) * (3 3 3) * (7 8 8);", 128), 128));
            s.Append(FormatScore("V3 SSMD 256",  "a = (3.4 5.5 1) * (2 4 2) * (3 3 3) * (7 8 8);", bench("a = (3.4 5.5 1) * (2 4 2) * (3 3 3) * (7 8 8);", 256), 256));
            s.Append(FormatScore("V3 SSMD 512",  "a = (3.4 5.5 1) * (2 4 2) * (3 3 3) * (7 8 8);", bench("a = (3.4 5.5 1) * (2 4 2) * (3 3 3) * (7 8 8);", 512), 512));
            s.Append(FormatScore("V3 SSMD 1024", "a = (3.4 5.5 1) * (2 4 2) * (3 3 3) * (7 8 8);", bench("a = (3.4 5.5 1) * (2 4 2) * (3 3 3) * (7 8 8);", 1024), 1024));
            s.Append(FormatScore("V3 SSMD 2048", "a = (3.4 5.5 1) * (2 4 2) * (3 3 3) * (7 8 8);", bench("a = (3.4 5.5 1) * (2 4 2) * (3 3 3) * (7 8 8);", 2048), 2048));
            s.Append(FormatScore("V3 SSMD 4096", "a = (3.4 5.5 1) * (2 4 2) * (3 3 3) * (7 8 8);", bench("a = (3.4 5.5 1) * (2 4 2) * (3 3 3) * (7 8 8);", 4096), 4096));
            s.Append(FormatScore("V3 SSMD 8192", "a = (3.4 5.5 1) * (2 4 2) * (3 3 3) * (7 8 8);", bench("a = (3.4 5.5 1) * (2 4 2) * (3 3 3) * (7 8 8);", 8192), 8192));
            
            s.Append(FormatScore("V4 NORMAL",    "a = (3.4 6 5.5 1) * (2 2 4 2) * (3 3 3 3) * (7 7 8 8);", bench("a = (3.4 6 5.5 1) * (2 2 4 2) * (3 3 3 3) * (7 7 8 8);"), 1));
            s.Append(FormatScore("V4 SSMD 1",    "a = (3.4 6 5.5 1) * (2 2 4 2) * (3 3 3 3) * (7 7 8 8);", bench("a = (3.4 6 5.5 1) * (2 2 4 2) * (3 3 3 3) * (7 7 8 8);", 1), 1));
            s.Append(FormatScore("V4 SSMD 16",   "a = (3.4 6 5.5 1) * (2 2 4 2) * (3 3 3 3) * (7 7 8 8);", bench("a = (3.4 6 5.5 1) * (2 2 4 2) * (3 3 3 3) * (7 7 8 8);", 16), 16));
            s.Append(FormatScore("V4 SSMD 32",   "a = (3.4 6 5.5 1) * (2 2 4 2) * (3 3 3 3) * (7 7 8 8);", bench("a = (3.4 6 5.5 1) * (2 2 4 2) * (3 3 3 3) * (7 7 8 8);", 32), 32));
            s.Append(FormatScore("V4 SSMD 64",   "a = (3.4 6 5.5 1) * (2 2 4 2) * (3 3 3 3) * (7 7 8 8);", bench("a = (3.4 6 5.5 1) * (2 2 4 2) * (3 3 3 3) * (7 7 8 8);", 64), 64));
            s.Append(FormatScore("V4 SSMD 128",  "a = (3.4 6 5.5 1) * (2 2 4 2) * (3 3 3 3) * (7 7 8 8);", bench("a = (3.4 6 5.5 1) * (2 2 4 2) * (3 3 3 3) * (7 7 8 8);", 128), 128));
            s.Append(FormatScore("V4 SSMD 256",  "a = (3.4 6 5.5 1) * (2 2 4 2) * (3 3 3 3) * (7 7 8 8);", bench("a = (3.4 6 5.5 1) * (2 2 4 2) * (3 3 3 3) * (7 7 8 8);", 256), 256));
            s.Append(FormatScore("V4 SSMD 512",  "a = (3.4 6 5.5 1) * (2 2 4 2) * (3 3 3 3) * (7 7 8 8);", bench("a = (3.4 6 5.5 1) * (2 2 4 2) * (3 3 3 3) * (7 7 8 8);", 512), 512));
            s.Append(FormatScore("V4 SSMD 1024", "a = (3.4 6 5.5 1) * (2 2 4 2) * (3 3 3 3) * (7 7 8 8);", bench("a = (3.4 6 5.5 1) * (2 2 4 2) * (3 3 3 3) * (7 7 8 8);", 1024), 1024));
            s.Append(FormatScore("V4 SSMD 2048", "a = (3.4 6 5.5 1) * (2 2 4 2) * (3 3 3 3) * (7 7 8 8);", bench("a = (3.4 6 5.5 1) * (2 2 4 2) * (3 3 3 3) * (7 7 8 8);", 2048), 2048));
            s.Append(FormatScore("V4 SSMD 4096", "a = (3.4 6 5.5 1) * (2 2 4 2) * (3 3 3 3) * (7 7 8 8);", bench("a = (3.4 6 5.5 1) * (2 2 4 2) * (3 3 3 3) * (7 7 8 8);", 4096), 4096));
            s.Append(FormatScore("V4 SSMD 8192", "a = (3.4 6 5.5 1) * (2 2 4 2) * (3 3 3 3) * (7 7 8 8);", bench("a = (3.4 6 5.5 1) * (2 2 4 2) * (3 3 3 3) * (7 7 8 8);", 8192), 8192));

                DataView = StringBuilderCache.GetStringAndRelease(ref s);
           if (View != null) View.text = DataView;
           if (Input.GetKeyDown(KeyCode.B)) RepeatCount /= 100;
        }
        catch(Exception ex)
        {
                View.text = "Error: " + ex.Message; 
        }
    }

    string FormatScore(string testname, string type, double score)
    {
        return string.Format($"{testname.PadRight(40)} {type.PadRight(60)} {score.ToString("0").PadLeft(20)} {(score / (float)DataCount).ToString("0.00").PadLeft(20)}\n");
    }
    string FormatScore(string testname, string type, double score, int ssmdcount)
    {
        return string.Format($"{testname.PadRight(40)} {type.PadRight(60)} {score.ToString("0").PadLeft(20)} {(score / (float)ssmdcount).ToString("0.00").PadLeft(20)}\n");
    }

    string FormatHeaderForArrayTest()
    {
        return string.Format($"{"TEST".PadRight(40)} {"DETAIL".PadRight(60)} {"TOTAL".PadLeft(20)} {"ELEMENT".PadLeft(20)}\n{"".PadLeft(150, '-')}\n");
    }

    unsafe double bench(IJob job)
    {
        Stopwatch sw = new Stopwatch();
        sw.Start();
        job.Execute(); 
        sw.Stop();

        double total_ns = sw.Elapsed.TotalMilliseconds / (double) RepeatCount * 1000000f;
        double ns_per_call = total_ns ;

        return ns_per_call; 
    }

    unsafe double bench(string script, int ssmd_count)
    {
        test_script_ssmd job = default;


        BlastCompilerOptions options = BlastCompilerOptions.SSMD.PackageWithoutStack();//.SetStackSize(128);


        using (BlastScript bs = BlastScript.FromText(script))
        {
            bs.Prepare(true, true, true);

            job.engine = Blast.Instance.Engine;
            job.blaster.SetPackage(bs.PackageData);
            job.datacount = ssmd_count;
            job.repeatcount = RepeatCount;


            Stopwatch sw = new Stopwatch();
            sw.Start();
            {
                job.Run();
            }
            sw.Stop();

            double total_ns = sw.Elapsed.TotalMilliseconds / (double)RepeatCount * 1000000f;
            double ns_per_call = total_ns;

            return ns_per_call;
        }
    }

    unsafe double bench(string script)
    {
        test_script_normal job = default;


        using (BlastScript bs = BlastScript.FromText(script))
        {
            bs.Prepare(true, false);

            job.engine = Blast.Instance.Engine;
            job.blaster.SetPackage(bs.PackageData);
            job.repeatcount = RepeatCount;


            Stopwatch sw = new Stopwatch();
            sw.Start();
            {
                job.Run();
            }
            sw.Stop();

            double total_ns = sw.Elapsed.TotalMilliseconds / (double)RepeatCount * 1000000f;
            double ns_per_call = total_ns;

            return ns_per_call;
        }
    }

    unsafe double bench(string script, bool multithreaded = false)
    {
        test_script_normal job = default;

        using (BlastScript bs = BlastScript.FromText(script))
        {
            bs.Prepare(true, false);

            job.engine = Blast.Instance.Engine;
            job.blaster.SetPackage(bs.PackageData);
            job.repeatcount = RepeatCount;


            Stopwatch sw = new Stopwatch();
            sw.Start();
            {
                job.Run();
            }
            sw.Stop();

            double total_ns = sw.Elapsed.TotalMilliseconds / (double)RepeatCount * 1000000f;
            double ns_per_call = total_ns;

            return ns_per_call;
        }
    }
}
