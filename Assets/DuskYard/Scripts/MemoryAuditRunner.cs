using System;
using System.Collections.Generic;
using System.IO;
using Unity.Profiling.Memory;
using UnityEngine;
using UnityEngine.Profiling;

namespace DuskYard
{
    // Separate from the FPS benchmark: snapshot pauses and inventory allocations must not enter frame statistics.
    public sealed class MemoryAuditRunner : MonoBehaviour
    {
        [Serializable] public class TypeCount { public string type; public int count; public long bytes; }
        [Serializable] public class Inventory
        {
            public long allocated,reserved,managed,gpu;
            public int nativeObjects,gameObjects;
            public TypeCount[] types;
        }
        [Serializable] public class AuditReport
        {
            public string unity,method="15s warmup, settle, GC, native/managed snapshot; 180s attacks; settle, GC, snapshot. Snapshot pauses excluded from the separate FPS benchmark.";
            public Inventory before,after;
            public int snapshotsSucceeded,errors,attacks,hits,peakContactParticles,comboHitMask;
            public float stressSeconds;
            public string limitation="Native object inventories and snapshots support a bounded test; they do not prove absence of every engine/driver allocation leak. The auditor retains its own reports and samples.";
        }
        public CombatDemo demo;
        readonly long[] memory=new long[256];
        readonly float[] seconds=new float[256];
        AuditReport report=new AuditReport();
        string output,phase="warmup";
        float elapsed,sampleTimer,nextAttack;
        int sampleCount,attackStart,hitStart;
        void Start()
        {
            output=Path.Combine(Application.persistentDataPath,"MemoryAudit");var args=Environment.GetCommandLineArgs();
            for(int i=0;i<args.Length-1;i++)if(args[i]=="-output")output=args[++i];
            Directory.CreateDirectory(output);report.unity=Application.unityVersion;
            demo.automated=true;demo.SetMode("Gameplay");Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
            Application.logMessageReceived+=OnLog;
        }
        void OnLog(string message,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)report.errors++;}
        void Update()
        {
            if(phase=="capture"||phase=="done")return;
            elapsed+=Mathf.Min(Time.unscaledDeltaTime,.1f);
            if((phase=="warmup"||phase=="stress")&&Time.unscaledTime>=nextAttack){demo.TryAttack();nextAttack=Time.unscaledTime+(phase=="stress"?.05f:.85f);}
            if(phase=="warmup"&&elapsed>=15){demo.SetMode("Baseline");phase="settle-before";elapsed=0;}
            else if(phase=="settle-before"&&elapsed>=2)
            {
                GC.Collect();GC.WaitForPendingFinalizers();GC.Collect();report.before=InventoryNow();Capture("before.snap");
            }
            else if(phase=="stress")
            {
                sampleTimer+=Time.unscaledDeltaTime;
                if(sampleTimer>=1&&sampleCount<memory.Length){sampleTimer=0;seconds[sampleCount]=elapsed;memory[sampleCount++]=Profiler.GetTotalAllocatedMemoryLong();}
                if(elapsed>=180)
                {
                    report.stressSeconds=elapsed;report.attacks=demo.TotalAttacks-attackStart;report.hits=demo.TotalHits-hitStart;report.comboHitMask=demo.ComboStepsHit;report.peakContactParticles=demo.PeakParticles;
                    demo.SetMode("Baseline");phase="settle-after";elapsed=0;
                }
            }
            else if(phase=="settle-after"&&elapsed>=2)
            {
                GC.Collect();GC.WaitForPendingFinalizers();GC.Collect();report.after=InventoryNow();
                using(var writer=new StreamWriter(Path.Combine(output,"memory-samples.csv")))
                {
                    writer.WriteLine("stress_seconds,allocated_bytes");
                    for(int i=0;i<sampleCount;i++)writer.WriteLine(seconds[i].ToString("F3",System.Globalization.CultureInfo.InvariantCulture)+","+memory[i]);
                }
                Capture("after.snap");
            }
        }
        void Capture(string file)
        {
            phase="capture";
            MemoryProfiler.TakeSnapshot(Path.Combine(output,file),OnSnapshot,CaptureFlags.NativeObjects|CaptureFlags.ManagedObjects|CaptureFlags.NativeAllocations);
        }
        void OnSnapshot(string path,bool success)
        {
            if(success)report.snapshotsSucceeded++;else report.errors++;
            if(report.after==null)
            {
                phase="stress";elapsed=0;nextAttack=Time.unscaledTime;attackStart=demo.TotalAttacks;hitStart=demo.TotalHits;demo.SetMode("Stress");
            }
            else
            {
                phase="done";File.WriteAllText(Path.Combine(output,"memory-audit.json"),JsonUtility.ToJson(report,true));
                Debug.Log("MEMORY_AUDIT_COMPLETE snapshots="+report.snapshotsSucceeded+" errors="+report.errors);
                Application.Quit(report.snapshotsSucceeded==2&&report.errors==0?0:2);
            }
        }
        static Inventory InventoryNow()
        {
            var result=new Inventory{allocated=Profiler.GetTotalAllocatedMemoryLong(),reserved=Profiler.GetTotalReservedMemoryLong(),managed=GC.GetTotalMemory(false),gpu=Profiler.GetAllocatedMemoryForGraphicsDriver()};
            var counts=new Dictionary<string,TypeCount>();var objects=Resources.FindObjectsOfTypeAll<UnityEngine.Object>();result.nativeObjects=objects.Length;
            foreach(var obj in objects)
            {
                if(obj==null)continue;string type=obj.GetType().FullName;
                if(!counts.TryGetValue(type,out var row)){row=new TypeCount{type=type};counts.Add(type,row);}
                row.count++;row.bytes+=Profiler.GetRuntimeMemorySizeLong(obj);if(obj is GameObject)result.gameObjects++;
            }
            var keys=new List<string>(counts.Keys);keys.Sort(StringComparer.Ordinal);result.types=new TypeCount[keys.Count];for(int i=0;i<keys.Count;i++)result.types[i]=counts[keys[i]];
            return result;
        }
        void OnDestroy(){Application.logMessageReceived-=OnLog;}
    }
}
