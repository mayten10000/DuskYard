using System;
using System.IO;
using DuskYard;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class DuskYardShowcase
{
    const string Root = "Assets/DuskYard/";
    public static void CleanLegacySettingsAndBuild()
    {
        var settings=AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.RenderPipelineGlobalSettings>(Root+"HDRPDefaultResources/HDRenderPipelineGlobalSettings.asset");
        if(settings!=null&&UnityEditor.SerializationUtility.HasManagedReferencesWithMissingTypes(settings))
        {
            UnityEditor.SerializationUtility.ClearAllManagedReferencesWithMissingTypes(settings);
            EditorUtility.SetDirty(settings);AssetDatabase.SaveAssets();
        }
        DuskYardFinalize.Build();
    }
    [MenuItem("Dusk Yard/Prepare showcase build")]
    public static void Build()
    {
        EditorSceneManager.OpenScene(Root + "Scenes/DuskYard.unity");
        var demo = UnityEngine.Object.FindAnyObjectByType<CombatDemo>();
        demo.hero.SetPositionAndRotation(new Vector3(0,0,-2),Quaternion.identity);
        demo.blade.localRotation=CombatDemo.GuardRotation;
        var sword=demo.blade.Find("Sword");
        // A tapered, diamond-section blade, rather than a square rod.
        Vector3[] vertices={new Vector3(-.043f,0,.11f),new Vector3(0,.012f,.11f),new Vector3(.043f,0,.11f),new Vector3(0,-.012f,.11f),new Vector3(-.027f,0,1.04f),new Vector3(0,.009f,1.04f),new Vector3(.027f,0,1.04f),new Vector3(0,-.009f,1.04f),new Vector3(0,0,1.20f)};
        int[] triangles={0,4,1,1,4,5,1,5,2,2,5,6,2,6,3,3,6,7,3,7,0,0,7,4,4,8,5,5,8,6,6,8,7,7,8,4,0,1,2,0,2,3};
        var mesh=new Mesh();mesh.vertices=vertices;mesh.triangles=triangles;mesh.RecalculateNormals();mesh.RecalculateBounds();
        string path=Root+"Meshes/Showcase sword.asset";var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(old==null)AssetDatabase.CreateAsset(mesh,path);else{EditorUtility.CopySerialized(mesh,old);UnityEngine.Object.DestroyImmediate(mesh);mesh=old;}
        sword.localPosition=Vector3.zero;sword.localRotation=Quaternion.identity;sword.localScale=Vector3.one;sword.GetComponent<MeshFilter>().sharedMesh=mesh;
        sword.GetComponent<Renderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(Root+"Materials/Brushed tempered steel.mat");
        AddGrip(demo.blade,"Leather sword grip",PrimitiveType.Cylinder,new Vector3(0,0,-.09f),new Vector3(.045f,.09f,.045f),"Worn brown leather");
        AddGrip(demo.blade,"Sword pommel",PrimitiveType.Sphere,new Vector3(0,0,-.22f),new Vector3(.075f,.075f,.075f),"Brushed tempered steel");
        foreach(var collider in demo.blade.GetComponentsInChildren<Collider>())collider.enabled=false;
        var pose=demo.hero.GetComponent<GuardSwordPose>();if(pose==null)pose=demo.hero.gameObject.AddComponent<GuardSwordPose>();
        pose.combat=demo;pose.body=demo.hero.Find("Medieval guard");
        foreach(Transform t in pose.body)
        {
            if(t.localPosition.x<.1f)continue;
            if(t.name=="Mail upper sleeve")pose.upperArm=t;
            if(t.name=="Leather vambrace")pose.forearm=t;
            if(t.name=="Glove")pose.glove=t;
        }
        if(pose.upperArm==null||pose.forearm==null||pose.glove==null)throw new Exception("Sword arm references missing");
        demo.trail.time=.065f;demo.trail.startWidth=.045f;demo.trail.endWidth=.003f;
        // Scene reference belongs in the scene component, not in the reusable player prefab.
        pose.combat=null;PrefabUtility.SaveAsPrefabAsset(demo.hero.gameObject,Root+"Prefabs/Player.prefab");pose.combat=demo;
        EditorUtility.SetDirty(pose);EditorUtility.SetDirty(demo);
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());AssetDatabase.SaveAssets();
        DuskYardFinalize.Build();
    }
    static void AddGrip(Transform parent,string name,PrimitiveType type,Vector3 p,Vector3 scale,string material)
    {
        var t=parent.Find(name);if(t==null){var g=GameObject.CreatePrimitive(type);g.name=name;t=g.transform;t.SetParent(parent,false);UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());}
        t.localPosition=p;t.localScale=scale;t.localRotation=type==PrimitiveType.Cylinder?Quaternion.Euler(90,0,0):Quaternion.identity;
        t.GetComponent<Renderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(Root+"Materials/"+material+".mat");
    }
}
