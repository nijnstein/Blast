using NSS.Blast;
using NSS.Blast.SSMD;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;
using Unity.Burst;
using Unity.Burst.CompilerServices;

public class Sample10 : MonoBehaviour
{
    public int ObjectCount = 1024; 
    public GameObject Prefab;

    // state 0 -> initial state 
    // - move around object around in squares with each state moving the object in a different direction
    [TextArea(5, 20)]
    public string State0 = @"#input state    float  0
#input position float3 0 0 0

position.x = select(position.x + 0.1, position.x, position.x > 100); 
state = select(0, 1, position.x > 100); 
";

    [TextArea(5, 20)]
    public string State1 = @"#input state    float  1
#input position float3 100 0 0

position.y = select(position.y + 0.1, position.y, position.y > 100); 
state = select(1, 2, position.y > 100); 
";

    [TextArea(5, 20)]
    public string State2 = @"#input state    float  2
#input position float3 100 100 0

position.x = select(position.x - 0.1, position.x, position.x < 0); 
state = select(2, 3, position.x < 0); 
";

    [TextArea(5, 20)]
    public string State3 = @"#input state    float  3
#input position float3 0 100 0

position.y = select(position.y - 0.1, position.y, position.y < 0); 
state = select(3, 0, position.y < 0); 
";

    int StateCount = 4;
    string[] StateScripts = null; 




    public struct state_data
    {
        public float  state;
        public float3 position;
    }

    List<GameObject> prefabs = new List<GameObject>();
    NativeArray<float> data = default;
    BlastScript[] scripts = null;

    NativeArray<IntPtr> ssmd_data = default;


    // Start is called before the first frame update
    void Start()
    {
        if (Prefab == null) return;
        
        Blast.Initialize(); 

        // allocate 1 script for each state 
        StateScripts = new string[] { State0, State1, State2, State3 };
        scripts = new BlastScript[StateCount];

        var r = new Unity.Mathematics.Random();
        r.InitState(); 

        float z = this.transform.position.z;

        // prepare data 
        // nr of dataelements = 4 per datarecord (1 float + 1 float3 = 4 elements = 16 bytes / record)
        data = new NativeArray<float>(ObjectCount * 4, Allocator.Persistent);

        for (int i = 0; i < ObjectCount; i++)
        {
            Vector3 pos = new Vector3(r.NextFloat(-200, 200), r.NextFloat(-200, 200), z + r.NextFloat(-200, 200));
            prefabs.Add(GameObject.Instantiate(Prefab, pos, Quaternion.Euler(0, 0, 180)));

            // note we directly set the floats, we could use the structure but it amounts to the same result 
            data[i * 4 + 0] = 0f; // state 
            data[i * 4 + 1] = pos.x; // x
            data[i * 4 + 2] = pos.y; // y
            data[i * 4 + 3] = pos.z; // z
        }

        // 
        // as no control flow statements were used in the scripts we can use the ssmd interpretor
        // we bin each state to seperate the data out for those states 
        //
        ssmd_data = new NativeArray<IntPtr>(ObjectCount, Allocator.Persistent, NativeArrayOptions.ClearMemory);

        CompileScripts(); 
    }

    void CompileScripts()
    { 
        // compile each script 
        for (int i = 0; i < StateCount; i++)
        {
            var bs = BlastScript.FromText(StateScripts[i]);
            bs.Prepare(false, true);
            scripts[i] = bs;
        }
    }


    void OnDestroy()
    {
        if (data.IsCreated) data.Dispose();
        if (ssmd_data.IsCreated) ssmd_data.Dispose();
        if (scripts != null) foreach (var bs in scripts) bs.Dispose();
        if (prefabs != null) foreach (var go in prefabs) GameObject.Destroy(go); 
    }



    unsafe void Update()
    {
        if (Prefab == null || prefabs.Count <= 0 || !data.IsCreated) return;
        if(StateScripts[0] != State0 || StateScripts[1] != State1 || StateScripts[2] != State2 || StateScripts[3] != State3)
        {
            // scripts changed 
            if (scripts != null) foreach (var bs in scripts) bs.Dispose();
            StateScripts = new string[] { State0, State1, State2, State3 };

            // recompile them 
            CompileScripts(); 
        }

        float* p = (float*)data.GetUnsafeReadOnlyPtr(); 

        // for each state, gather data for each object in that state
        for (float istate = 0; istate < StateCount; istate++)
        {
            int count = 0;
            for (int i = 0; i < ObjectCount; i++)
            {
                if (data[i * 4] == istate)
                {
                    // store a reference to the objects data at object index i
                    ssmd_data[count] = new IntPtr(&p[i * 4]); 
                    count++; 
                }
            }

            if (count > 0)
            {
                // ssmd execution of the data in that state
                scripts[(int)istate].Package.Execute(
                    Blast.Instance.Engine,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    ssmd_data.Slice(0, count),
                    true);
            }
        }

        // for each object, update prefab from data 
        for (int i = 0; i < ObjectCount; i++)
        {
            // we could cast the start index into a state data record for ease of use
            state_data* sd = (state_data*)(void*)&p[i * 4];

            // update prefabs position 
            prefabs[i].transform.position = (Vector3)sd->position;
        }

    }
}
