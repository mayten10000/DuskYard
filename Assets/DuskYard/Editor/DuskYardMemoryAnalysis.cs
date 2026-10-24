using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class DuskYardMemoryAnalysis
{
    [Serializable] public class Row { public string name;public long count,bytes; }
    [Serializable] public class SnapshotStats { public string path;public long nativeObjects,nativeObjectBytes,nativeAllocations,nativeAllocationBytes;public Row[] objectsByType,allocationsByRoot; }
    [Serializable] public class Comparison { public SnapshotStats before,after;public string note="Finite before/after capture comparison. Snapshot capture and audit bookkeeping have their own allocations; increased allocator pools do not alone prove a leak."; }
    static object Get(object instance,string property){return instance.GetType().GetProperty(property).GetValue(instance);}
    static long Count(object entries){return Convert.ToInt64(entries.GetType().GetMethod("GetNumEntries").Invoke(entries,null));}
    static Array Read(object entryArray)
    {
        uint count=checked((uint)Count(entryArray));Type element=entryArray.GetType().GetGenericArguments()[0];
        if(count==0)return Array.CreateInstance(element,0);
        object[] args={0u,count,Array.CreateInstance(element,(int)count)};
        entryArray.GetType().GetMethod("GetEntries").Invoke(entryArray,args);return (Array)args[2];
    }
    static Row[] Rows(Dictionary<string,Row> map)
    {
        var names=new List<string>(map.Keys);names.Sort(StringComparer.Ordinal);var rows=new Row[names.Count];for(int i=0;i<names.Count;i++)rows[i]=map[names[i]];return rows;
    }
    static void Add(Dictionary<string,Row> map,string name,long bytes)
    {
        if(!map.TryGetValue(name,out var row)){row=new Row{name=name};map.Add(name,row);}row.count++;row.bytes+=bytes;
    }
    static SnapshotStats Analyze(string path)
    {
        Type type=null;foreach(var assembly in AppDomain.CurrentDomain.GetAssemblies()){type=assembly.GetType("UnityEditor.Profiling.Memory.Experimental.PackedMemorySnapshot");if(type!=null)break;}
        if(type==null)throw new Exception("Built-in snapshot reader unavailable");
        object snapshot=type.GetMethod("Load",BindingFlags.Public|BindingFlags.Static).Invoke(null,new object[]{Path.GetFullPath(path)});
        if(snapshot==null)throw new Exception("Snapshot reader returned null");
        try
        {
            var result=new SnapshotStats{path=Path.GetFileName(path)};
            var objects=Get(snapshot,"nativeObjects");result.nativeObjects=Count(objects);
            var sizes=Read(Get(objects,"size"));var indices=Read(Get(objects,"nativeTypeArrayIndex"));var names=Read(Get(Get(snapshot,"nativeTypes"),"typeName"));
            var groups=new Dictionary<string,Row>();
            for(int i=0;i<sizes.Length;i++){long size=checked((long)Convert.ToUInt64(sizes.GetValue(i)));result.nativeObjectBytes+=size;Add(groups,(string)names.GetValue(Convert.ToInt32(indices.GetValue(i))),size);}
            result.objectsByType=Rows(groups);
            var roots=Get(snapshot,"nativeRootReferences");var ids=Read(Get(roots,"id"));var areas=Read(Get(roots,"areaName"));var rootNames=Read(Get(roots,"objectName"));var labels=new Dictionary<ulong,string>();
            for(int i=0;i<ids.Length;i++)labels[Convert.ToUInt64(ids.GetValue(i))]=areas.GetValue(i)+" / "+rootNames.GetValue(i);
            var allocations=Get(snapshot,"nativeAllocations");result.nativeAllocations=Count(allocations);var allocationSizes=Read(Get(allocations,"size"));var allocationRoots=Read(Get(allocations,"rootReferenceId"));groups.Clear();
            for(int i=0;i<allocationSizes.Length;i++)
            {
                long size=checked((long)Convert.ToUInt64(allocationSizes.GetValue(i)));result.nativeAllocationBytes+=size;
                ulong id=Convert.ToUInt64(allocationRoots.GetValue(i));Add(groups,labels.TryGetValue(id,out var label)?label:"Unattributed root "+id,size);
            }
            result.allocationsByRoot=Rows(groups);return result;
        }
        finally{type.GetMethod("Dispose").Invoke(snapshot,null);}
    }
    public static void Run()
    {
        Directory.CreateDirectory("Documentation/Verification/MemoryAudit");
        try
        {
            var comparison=new Comparison{before=Analyze("Deliverables/MemoryAudit/before.snap"),after=Analyze("Deliverables/MemoryAudit/after.snap")};
            File.WriteAllText("Documentation/Verification/MemoryAudit/snapshot-comparison.json",JsonUtility.ToJson(comparison,true));Debug.Log("SNAPSHOT_COMPARISON_COMPLETE");
        }
        catch(Exception error)
        {
            File.WriteAllText("Documentation/Verification/MemoryAudit/snapshot-reader-diagnostic.txt",error.ToString());throw;
        }
    }
}
