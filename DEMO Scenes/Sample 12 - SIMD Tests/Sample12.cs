using NSS.Blast;
using NSS.Blast.SSMD;
using NSS.Blast.SSMD.vectorization_tests;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Sample12 : MonoBehaviour
{

    public TMPro.TMP_Text View;

    public int DataCount = 64;
    public int RepeatCount = 1024 * 1024;




    /// overview of data after execution 
    /// </summary>
    [TextArea(10, 32)]
    public string DataView = "";

    public bool Once = true; 

    // Start is called before the first frame update
    void Start()
    {
      
    }

    private void OnDestroy()
    {
        
    }

    [Serializable]
    [StructLayout(LayoutKind.Explicit, Size = 20)]
    public struct data
    {
        [FieldOffset(0)]
        public Bool32 b32;
        [FieldOffset(4)]
        public float b;
        [FieldOffset(8)]
        public float counter;
        [FieldOffset(12)]
        public float state;
        [FieldOffset(16)]
        public float temp;
    }




    // Update is called once per frame
    unsafe void Update()
    {
        if(Input.GetKeyDown(KeyCode.Escape))
        {
            Application.Quit(); 
        }
        if(Input.GetKeyDown(KeyCode.C))
        {
            SceneManager.LoadScene("Sample10");
        }
        if (Once || Input.GetKeyDown(KeyCode.Space))
        {
            Once = false;
            var s = StringBuilderCache.Acquire();

            s.Append("BLAST v1.0.4b\n");
            s.Append($"Array operations benchmark, repeats: {RepeatCount}, array length: {DataCount}, {(simd.IsUnrolled ? "[unrolled]" : "")}\n\n");

            s.Append(FormatHeader());
            s.Append(FormatScore("mul_array_f4f4",                    "f4.xyzw = f4.xyzw * f4.xyzw",     bench(new test_mul_array_f4f4() { repeatcount = RepeatCount, datacount = DataCount })));
            s.Append(FormatScore("mul_array_f4f4_constant",           "f4.xyzw = f4.xyzw * constant",    bench(new test_mul_array_f4f4_constant() { repeatcount = RepeatCount, datacount = DataCount })));
            s.Append(FormatScore("mul_array_f4f4_indexed",            "f4.xyzw = f4.xyzw * [][].xyzw",   bench(new test_mul_array_f4f4_indexed() { repeatcount = RepeatCount, datacount = DataCount })));
            s.Append(FormatScore("mul_array_f4f4_indexed_unaligned",  "f4.xyzw = f4.xyzw * [][].xyzw",   bench(new test_mul_array_f4f4_indexed_unaligned() { repeatcount = RepeatCount, datacount = DataCount })));
            s.Append(FormatScore("mul_array_f4f1",                    "f4.xyzw = f4.xyzw * f4.x",        bench(new test_mul_array_f4f1() { repeatcount = RepeatCount, datacount = DataCount })));
            s.Append(FormatScore("mul_array_f4f1_constant",           "f4.xyzw = f1.x    * constant",    bench(new test_mul_array_f4f1_constant() { repeatcount = RepeatCount, datacount = DataCount })));
            s.Append(FormatScore("mul_array_f3f3_indexed",            "f3.xyz  = f3.xyz  * [][].xyz",    bench(new test_mul_array_f3f3_indexed() { repeatcount = RepeatCount, datacount = DataCount })));
            s.Append(FormatScore("mul_array_f2f2_indexed",            "f2.xy   = f2.xy   * [][].xy",     bench(new test_mul_array_f2f2_indexed() { repeatcount = RepeatCount, datacount = DataCount })));
            s.Append(FormatScore("mul_array_f1f1",                    "f4.x    = f4.x    * f4.x",        bench(new test_mul_array_f1f1() { repeatcount = RepeatCount, datacount = DataCount })));
            s.Append(FormatScore("mul_array_f1f1_constant",           "f4.x    = f4.x    * constant",    bench(new test_mul_array_f1f1_constant() { repeatcount = RepeatCount, datacount = DataCount })));
            s.Append(FormatScore("mul_array_f1f1_indexed",            "f1.x    = f1.x    * [][].x",      bench(new test_mul_array_f1f1_indexed() { repeatcount = RepeatCount, datacount = DataCount })));
            s.Append(FormatScore("mul_array_f1f1_indexed_unaligned",  "f1.x    = f1.x    * [][].x",      bench(new test_mul_array_f1f1_indexed_unaligned() { repeatcount = RepeatCount, datacount = DataCount })));

            DataView = StringBuilderCache.GetStringAndRelease(ref s);
            if (View != null) View.text = DataView;
        }
    }

    string FormatScore(string testname, string type, double score)
    {                                                                                                            
        return string.Format($"{testname.PadRight(32)} {type.PadRight(36)} {score.ToString("0").PadLeft(20)} {(score / (float)DataCount).ToString("0.00").PadLeft(20)}\n");
    }

    string FormatHeader()
    {
        return string.Format($"{"TEST".PadRight(32)} {"DETAIL".PadRight(36)} {"TOTAL".PadLeft(20)} {"ELEMENT".PadLeft(20)}\n{"".PadLeft(120, '-')}\n");
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
}
