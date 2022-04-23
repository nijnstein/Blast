using NSS.Blast;
using System;
using System.Runtime.InteropServices;
using System.Text;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;

public class Sample13 : MonoBehaviour
{

    /// <summary>
    /// the script to execute
    /// </summary>
    [TextArea(5, 20)]
    public string Script = "r = sqrt(1);\n\n a = (0.1 1.2 2.2 3.2) * (0.2 4.1 5.1 6.1) * (0.3 7.3 8.3 9.3);";
        
    // "a = (0.1 1.2 2.2 3.2) * (0.2 4.1 5.1 6.1) * (0.3 7.3 8.3 9.3);"; 
        
        
        
//@"#input a float4
//#input b float4
//#input c float4	
//a = (1 2 3 4);
//b = (5 6 7 8);
//c = (9 10 11 12);
//a = a * b * c;
//d = (1 1 1 1) * (1 1 1 1) * (2 3 4 5);";

    public bool PackageStack = true;

    /// <summary>
    /// keep track, on change rerun script 
    /// </summary>
    public bool packaged_stack = true;

    /// <summary>
    /// the last executed script 
    /// </summary>
    string last_script;

    /// <summary>
    /// overview of data after execution 
    /// </summary>
    [TextArea(10, 20)]
    public string DataView = "";

    // every 4 columns is 1 data point
    const int DataCount = 32;

    // every row in the texture is 1 ssmd record 
    const int SSMDCount = 32; 


    public Texture2D DataStackTexture;
    public TMPro.TMP_InputField ScriptInput;
    public TMPro.TMP_Text NodeStructure;
    public TMPro.TMP_Text ErrorMessage;
    public TMPro.TMP_Text Bytecode;
    public TMPro.TMP_Text Metadata;
    public TMPro.TMP_Text PackageInfo;
    public TMPro.TMP_Text DataInfo;
    public TMPro.TMP_InputField SSMDRecordNumber;
    public Button ExecuteButton; 



    /// <summary>
    /// 1 script_data record has exactly as many bytes as 1 row in the image
    /// 
    /// - if package stack is enabled, the stack is part of this structure
    /// - if not packaged in stack, then the interpretor holds a seperate buffer 
    /// 
    /// </summary>
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct script_data
    {
        public float4 a;
        public float4 b;
        public float4 c;
        public float4 d;
        public float4 e;
        public float4 f;
        public float4 g;
        public float4 h;
    }


    // Start is called before the first frame update
    void Start()
    {
        if(ScriptInput != null)
        {
            ScriptInput.text = Script; 
        }
        if(ExecuteButton != null)
        {
            ExecuteButton.onClick.AddListener(Execute);
        }
        ClearAll(); 
    }

    /// <summary>
    /// trigger execution of script
    /// </summary>
    void Execute()
    {
        last_script = null; 
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Application.Quit();
        }
        if(DataStackTexture == null || string.IsNullOrWhiteSpace(Script))
        {
            return; 
        }
        if (ScriptInput != null)
        {
            Script = ScriptInput.text;
        }

        // remember the last script so we dont run the same script twice 
        if (string.Compare(Script, last_script, true) == 0 && packaged_stack == PackageStack)
        {
            // script didnt change, dont update 
            return;
        }
        if (string.IsNullOrWhiteSpace(Script))
        {
            ClearAll(); 
        }
        if (!Blast.IsInstantiated)
        {
            Blast.Initialize();
        }

        last_script = Script;
        packaged_stack = PackageStack;
        DataView = "";

        // prepare the new package from text 
        BlastScript script = BlastScript.FromText(Script);

        if(NodeStructure != null)
        {
            var ast = script.CompileAST(true);
            if (ast != null) NodeStructure.text = ast.ToNodeTreeString();
            else NodeStructure.text = string.Empty;
        }


        BlastError res = script.Prepare(true, true, PackageStack);
        if (res != BlastError.success)
        {
            DataView = "Failed to compile input script, check the log for more details.";
            SetError(res);
            return;
        }
        else
        {
            UpdateInfo(script);
        }

        // setup backing memory from a texture 
        NativeArray<byte> texture_data = DataStackTexture.GetRawTextureData<byte>();

        //if(memory == IntPtr.Zero)
        if(!texture_data.IsCreated)
        {
            Debug.LogError("texture pointer null");
            return;
        }
         

        unsafe
        {
            float* data = (float*)NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(texture_data);
            NativeArray<script_data> ssmd_data = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<script_data>(data, texture_data.Length / sizeof(script_data), Allocator.None);
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref ssmd_data, AtomicSafetyHandle.Create());
#endif

            for (int i = 0; i < texture_data.Length / 4; i++) data[i] = 0; 

            // execute the script
            BlastError result = script.Execute(ssmd_data);

            if (result == BlastError.success)
            {
                // show results 
                int SSMDRecordToView = 0;
                if (SSMDRecordNumber != null) int.TryParse(SSMDRecordNumber.text, out SSMDRecordToView);

                StringBuilder sb = StringBuilderCache.Acquire();
                sb.Append($"SSMD Record #{SSMDRecordToView}");
                for(int i = (SSMDRecordToView * 32); i < 32 + (SSMDRecordToView * 32); i += 8)
                {
                    sb.AppendLine();
                    sb.Append($"{data[i + 0].ToString("0.00").PadLeft(8)}");
                    sb.Append($"{data[i + 1].ToString("0.00").PadLeft(8)}");
                    sb.Append($"{data[i + 2].ToString("0.00").PadLeft(8)}");
                    sb.Append($"{data[i + 3].ToString("0.00").PadLeft(8)}");
                    sb.Append($"{data[i + 4].ToString("0.00").PadLeft(8)}");
                    sb.Append($"{data[i + 5].ToString("0.00").PadLeft(8)}");
                    sb.Append($"{data[i + 6].ToString("0.00").PadLeft(8)}");
                    sb.Append($"{data[i + 7].ToString("0.00").PadLeft(8)}");
                }
                DataView = StringBuilderCache.GetStringAndRelease(ref sb);
                if(DataInfo != null)
                {
                    DataInfo.text = DataView; 
                }
            }
            else
            {
                DataView = $"Error: {result}, check the logs for more details";
            }


            DataStackTexture.SetPixelData<byte>(texture_data, 0);
            DataStackTexture.Apply();

        }
    }

    private void UpdateInfo(BlastScript script)
    {
        ClearError();

        // show bytecode 
        if (Bytecode != null)
        {
            Bytecode.text = $"Bytecode: ({script.Package.Package.CodeSize} bytes)\n{script.Package.GetPackageCodeBytesText(32, true)}";
        }
        // show metadata 
        if (Metadata != null)
        {
            StringBuilder sb = StringBuilderCache.Acquire();
            sb.AppendLine("Metadata Information:");
            int i = 0; int inlast = 0;  
            for (; i < script.PackageData.MetadataSize; i += 1)
            {
                BlastVariableDataType type;
                byte vectorsize;
                if (script.Package.GetMetaDataInfo(i, out type, out vectorsize))
                {
                    if (inlast > 0 || vectorsize == 0)
                    {
                        inlast--;
                        // later version will use this space left over by vectors for information on the structure or array
                        sb.Append($"{(i).ToString().PadLeft(2)}:                   ");
                    }
                    else
                    {
                        sb.Append($"{(i).ToString().PadLeft(2)}: {(type.ToString().PadLeft(10))}[{vectorsize}]     ");
                        if (vectorsize > 1)
                        {
                            inlast = vectorsize - 1;
                        }
                    }
                    if (i % 2 == 1) sb.AppendLine();
                }
            }
            Metadata.text = StringBuilderCache.GetStringAndRelease(ref sb);
        }
        // show some usefull information on the package 
        if (PackageInfo != null)
        {
            StringBuilder sb = StringBuilderCache.Acquire();
            sb.AppendLine($"Packagesize: {script.Package.PackageSize.ToString().PadLeft(5)     } bytes, mode:     {script.Package.PackageMode.ToString().PadLeft(5)         } ({script.PackageData.Flags})");
            sb.AppendLine($"Code:        {script.Package.Package.CodeSize.ToString().PadLeft(5)} bytes, metadata: {script.Package.Package.MetadataSize.ToString().PadLeft(5)} bytes, codesegment: {script.Package.Package.CodeSegmentSize.ToString().PadLeft(5)} bytes");
            sb.AppendLine($"Data:        {script.Package.Package.DataSize.ToString().PadLeft(5)} bytes, Stack:    {script.Package.Package.StackSize.ToString().PadLeft(5)   } bytes, datasegment: {script.Package.Package.DataSegmentSize.ToString().PadLeft(5)} bytes");
            PackageInfo.text = StringBuilderCache.GetStringAndRelease(ref sb);
        }
    }

    private void ClearError()
    {
        if (ErrorMessage != null)
        {
            ErrorMessage.text = "";
        }
    }

    public void ClearAll()
    {
        SetError(BlastError.success); 
    }
    
    private void SetError(BlastError res)
    {
        if(PackageInfo != null)
        {
            PackageInfo.text = ""; 
        }
        if(DataInfo != null)
        {
            DataInfo.text = ""; 
        }
        if (Metadata != null)
        {
            Metadata.text = ""; 
        }
        if (Bytecode != null)
        {
            Bytecode.text = ""; 
        }
        if (ErrorMessage != null)
        {
            if (res == BlastError.success)
            {
                ErrorMessage.text = "";
            }
            else
            {
                ErrorMessage.text = "Execution or scripterror: " + res.ToString();
            }
        }
    }




}
