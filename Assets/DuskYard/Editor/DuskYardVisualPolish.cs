using System;
using System.Collections.Generic;
using System.IO;
using DuskYard;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static class DuskYardVisualPolish
{
    const string Root="Assets/DuskYard/";
    static Material stone,iron,linen;
    static Transform Group(string name,Transform parent,Vector3 position)
    {
        var old=parent.Find(name);if(old!=null)return old;
        var t=new GameObject(name).transform;t.SetParent(parent,false);t.localPosition=position;return t;
    }
    static Material Mat(string name,Color color,float metal=0,float smooth=.2f)
    {
        string path=Root+"Materials/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(m==null){m=new Material(Shader.Find("HDRP/Lit"));AssetDatabase.CreateAsset(m,path);}
        m.SetColor("_BaseColor",color);m.SetFloat("_Metallic",metal);m.SetFloat("_Smoothness",smooth);EditorUtility.SetDirty(m);return m;
    }
    static Transform Box(string name,Transform parent,Vector3 position,Vector3 size,Material material)
    {
        var t=parent.Find(name);
        if(t==null){var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;t=g.transform;t.SetParent(parent,false);UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());}
        t.localPosition=position;t.localScale=size;t.GetComponent<Renderer>().sharedMaterial=material;return t;
    }
    static Mesh SaveMesh(string name,Mesh mesh)
    {
        mesh.RecalculateNormals();mesh.RecalculateBounds();
        if(mesh.uv.Length==mesh.vertexCount)mesh.RecalculateTangents();
        if(mesh.uv2.Length==0)Unwrapping.GenerateSecondaryUVSet(mesh);
        string path=Root+"Meshes/Polish/"+name+".asset";var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(old==null){AssetDatabase.CreateAsset(mesh,path);return mesh;}
        EditorUtility.CopySerialized(mesh,old);UnityEngine.Object.DestroyImmediate(mesh);return old;
    }
    static Transform MeshObject(string name,Transform parent,Mesh mesh,Material material)
    {
        var t=Group(name,parent,Vector3.zero);var f=t.GetComponent<MeshFilter>();if(f==null)f=t.gameObject.AddComponent<MeshFilter>();f.sharedMesh=SaveMesh(name,mesh);
        var r=t.GetComponent<MeshRenderer>();if(r==null)r=t.gameObject.AddComponent<MeshRenderer>();r.sharedMaterial=material;return t;
    }
    static void Rig(CombatDemo demo)
    {
        var root=demo.hero.Find("Medieval guard");var upper=Group("Upper body",root,Vector3.zero);
        var children=new List<Transform>();foreach(Transform t in root)if(t!=upper&&!t.name.EndsWith("leg pose",StringComparison.Ordinal))children.Add(t);
        foreach(var t in children)t.SetParent(upper,true);
        var head=Group("Head pivot",upper,new Vector3(0,1.68f,0));
        foreach(var name in new[]{"Nasal helmet","Helmet rim","Shadowed face","Nose guard"}){var t=upper.Find(name);if(t!=null)t.SetParent(head,true);}
        var pose=demo.hero.GetComponent<GuardSwordPose>();pose.body=upper;pose.head=head;
        foreach(Transform t in upper)
        {
            if(t.localPosition.x>=0){if(t.name=="Mail upper sleeve")pose.upperArm=t;if(t.name=="Leather vambrace")pose.forearm=t;if(t.name=="Glove")pose.glove=t;}
            else{if(t.name=="Mail upper sleeve")pose.leftUpperArm=t;if(t.name=="Leather vambrace")pose.leftForearm=t;if(t.name=="Glove")pose.leftGlove=t;}
        }
        var gait=demo.hero.GetComponent<GuardGait>();
        foreach(var leg in new[]{gait.left,gait.right})
        {
            var calf=Group("Knee pivot",leg,new Vector3(0,-.39f,0));var lower=new List<Transform>();
            foreach(Transform t in leg)if(t!=calf&&t.name!="Wool hose")lower.Add(t);
            foreach(var t in lower)t.SetParent(calf,true);
            if(leg==gait.left)gait.leftCalf=calf;else gait.rightCalf=calf;
        }
        // The old TrailRenderer followed the sword's origin, now at the hilt.
        var tip=Group("Blade tip trail",demo.blade,new Vector3(0,0,1.08f));var trail=tip.GetComponent<TrailRenderer>();
        if(trail==null){trail=tip.gameObject.AddComponent<TrailRenderer>();EditorUtility.CopySerialized(demo.trail,trail);demo.trail.enabled=false;}
        trail.enabled=true;trail.time=.055f;trail.startWidth=.025f;trail.endWidth=.001f;trail.minVertexDistance=.025f;trail.emitting=false;demo.trail=trail;
        Mat("Quilted olive gambeson",new Color(.63f,.53f,.33f),0,.14f);
        Mat("Worn brown leather",new Color(.20f,.12f,.062f),0,.24f);
        Mat("Dark riveted chainmail",new Color(.62f,.65f,.68f),.8f,.34f);
        Mat("Brushed tempered steel",new Color(.56f,.60f,.63f),.86f,.40f);
        EditorUtility.SetDirty(pose);EditorUtility.SetDirty(gait);EditorUtility.SetDirty(demo);
    }
    static void Puddles(Transform environment)
    {
        Vector3[] positions={new Vector3(-7,0,-4),new Vector3(7.8f,0,1),new Vector3(-8,0,7),new Vector3(10,0,9),new Vector3(-2.8f,0,8),new Vector3(4,0,-5),new Vector3(-8.5f,0,-9),new Vector3(2.5f,0,10),new Vector3(9,0,-6)};
        int i=0;foreach(Transform t in environment)if(t.name=="Wet paving")
        {
            t.gameObject.SetActive(i<positions.Length);if(i>=positions.Length){i++;continue;}
            t.localPosition=positions[i]+Vector3.up*.008f;t.localScale=new Vector3(1.7f+(i%3)*.45f,.004f,1.1f+(i%2)*.55f);t.localRotation=Quaternion.Euler(0,i*37,0);
            var f=t.GetComponent<MeshFilter>();var mesh=UnityEngine.Object.Instantiate(f.sharedMesh);var uv=new Vector2[mesh.vertexCount];var v=mesh.vertices;
            for(int k=0;k<v.Length;k++){var w=t.TransformPoint(v[k]);uv[k]=new Vector2(w.x,w.z)*.5f;}
            mesh.uv=uv;f.sharedMesh=SaveMesh("Puddle "+i,mesh);i++;
        }
    }
    static void Banner(Transform environment,int side)
    {
        var t=environment.Find("Banner "+side);if(t==null)return;t.localScale=new Vector3(.9f,1.65f,1);
        var v=new List<Vector3>();var uv=new List<Vector2>();var tris=new List<int>();
        for(int y=0;y<9;y++)for(int x=0;x<13;x++)
        {
            float u=x/12f,w=y/8f;v.Add(new Vector3(u-.5f,.5f-w-(y==8?.06f*Mathf.Sin(u*13):0),Mathf.Sin(u*Mathf.PI*6)*.045f));uv.Add(new Vector2(u,w));
            if(y<8&&x<12){int a=y*13+x,b=a+13;tris.AddRange(new[]{a,a+1,b,a+1,b+1,b});}
        }
        var mesh=new Mesh();mesh.SetVertices(v);mesh.SetUVs(0,uv);mesh.SetTriangles(tris,0);t.GetComponent<MeshFilter>().sharedMesh=SaveMesh("Folded banner "+side,mesh);
        var wool=Mat("Faded burgundy wool",new Color(.42f,.075f,.035f),0,.12f);wool.SetTexture("_BaseColorMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"Textures/Quilted linen.png"));t.GetComponent<Renderer>().sharedMaterial=wool;
        var crest=Box("Heraldic stripe "+side,environment,new Vector3(side*5,2.35f,13.19f),new Vector3(.10f,1.35f,.012f),linen);
        Box("Heraldic crossbar "+side,environment,new Vector3(side*5,2.59f,13.18f),new Vector3(.43f,.10f,.012f),linen);
    }
    static void Architecture(Transform environment,Transform details)
    {
        for(int side=-1;side<=1;side+=2)
        {
            Box("Coping side "+side,details,new Vector3(side*14,4.02f,0),new Vector3(1,.18f,28),stone);
            for(int j=0;j<11;j++)Box("Merlon "+side+" "+j,details,new Vector3(side*14,4.40f,-12+j*2.4f),new Vector3(.85f,.7f,.85f),stone);
            Box("Coping rear "+side,details,new Vector3(side*9,3.04f,14),new Vector3(10,.17f,1),stone);
            Banner(environment,side);
        }
        var crafted=environment.Find("Crafted details");if(crafted!=null)foreach(Transform t in crafted)if(t.name=="Gate vertical plank")
        {
            float x=t.localPosition.x,height=3+Mathf.Sqrt(Mathf.Max(0,2.55f*2.55f-x*x));
            t.localPosition=new Vector3(x,height*.5f-.025f,t.localPosition.z);t.localScale=new Vector3(t.localScale.x,height-.05f,t.localScale.z);
        }
    }
    static void Brazier(Transform environment,Transform details,int side)
    {
        foreach(Transform t in environment)
        {
            if(t.name=="Brazier bowl"&&Mathf.Sign(t.localPosition.x)==side)t.GetComponent<Renderer>().enabled=false;
            if(t.name=="Brazier stand"&&Mathf.Sign(t.localPosition.x)==side){t.localPosition=new Vector3(side*5,.63f,5);t.localScale=new Vector3(.12f,1.26f,.12f);}
            if(t.name.StartsWith("Charcoal "+side+" ",StringComparison.Ordinal)){var p=t.localPosition;p.y=1.42f;t.localPosition=p;}
        }
        var root=Group("Forged brazier "+side,details,new Vector3(side*5,0,5));
        Vector2[] rings={new Vector2(.07f,1.25f),new Vector2(.28f,1.34f),new Vector2(.36f,1.49f),new Vector2(.31f,1.49f),new Vector2(.22f,1.37f),new Vector2(.07f,1.34f)};
        var vertices=new List<Vector3>();var tris=new List<int>();
        for(int j=0;j<rings.Length;j++)for(int i=0;i<=24;i++)
        {
            float a=i*Mathf.PI*2/24;vertices.Add(new Vector3(Mathf.Sin(a)*rings[j].x,rings[j].y,Mathf.Cos(a)*rings[j].x));
            if(j>0&&i<24){int k=j*25+i;tris.AddRange(new[]{k,k-25,k+1,k+1,k-25,k-24});}
        }
        var bowl=new Mesh();bowl.SetVertices(vertices);bowl.SetTriangles(tris,0);MeshObject("Iron bowl "+side,root,bowl,iron);
        for(int i=0;i<3;i++)
        {
            float a=i*Mathf.PI*2/3;var foot=Box("Foot "+i,root,new Vector3(Mathf.Sin(a)*.18f,.20f,Mathf.Cos(a)*.18f),new Vector3(.055f,.5f,.055f),iron);
            foot.localRotation=Quaternion.Euler(Mathf.Cos(a)*38,0,-Mathf.Sin(a)*38);
        }
        var flame=Group("Small flame",root,new Vector3(0,1.43f,0));flame.localRotation=Quaternion.Euler(-90,0,0);
        var particles=flame.GetComponent<ParticleSystem>();if(particles==null)particles=flame.gameObject.AddComponent<ParticleSystem>();particles.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
        var main=particles.main;main.loop=true;main.playOnAwake=true;main.maxParticles=24;main.startLifetime=new ParticleSystem.MinMaxCurve(.5f,.85f);main.startSpeed=new ParticleSystem.MinMaxCurve(.24f,.5f);main.startSize=new ParticleSystem.MinMaxCurve(.07f,.14f);main.simulationSpace=ParticleSystemSimulationSpace.Local;
        var emission=particles.emission;emission.rateOverTime=9;var shape=particles.shape;shape.shapeType=ParticleSystemShapeType.Cone;shape.radius=.13f;shape.angle=8;
        var size=particles.sizeOverLifetime;size.enabled=true;size.size=new ParticleSystem.MinMaxCurve(1,new AnimationCurve(new Keyframe(0,.4f),new Keyframe(.25f,1),new Keyframe(1,0)));
        var color=particles.colorOverLifetime;color.enabled=true;var g=new Gradient();g.SetKeys(new[]{new GradientColorKey(new Color(1,.8f,.45f),0),new GradientColorKey(new Color(.65f,.16f,.015f),1)},new[]{new GradientAlphaKey(.8f,0),new GradientAlphaKey(0,1)});color.color=g;
        string materialPath=Root+"Materials/Brazier small flame.mat";var material=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if(material==null){material=new Material(AssetDatabase.LoadAssetAtPath<Material>(Root+"Materials/Ember warm.mat"));AssetDatabase.CreateAsset(material,materialPath);}
        material.SetColor("_BaseColor",new Color(3,.8f,.08f,1));EditorUtility.SetDirty(material);
        var renderer=particles.GetComponent<ParticleSystemRenderer>();renderer.sharedMaterial=material;renderer.renderMode=ParticleSystemRenderMode.Billboard;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
    }
    static void Target(CombatDemo demo)
    {
        var wood=AssetDatabase.LoadAssetAtPath<Material>(Root+"Materials/Dark oak.mat");
        var materials=new[]{demo.readyMaterial,demo.hitMaterial,demo.recoveryMaterial,demo.restoredMaterial};
        Color[] colors={new Color(.78f,.69f,.53f),new Color(.32f,.20f,.10f),new Color(.53f,.43f,.28f),new Color(.98f,.84f,.60f)};
        for(int i=0;i<materials.Length;i++){materials[i].CopyPropertiesFromMaterial(wood);materials[i].SetColor("_BaseColor",colors[i]);materials[i].SetColor("_EmissiveColor",Color.black);EditorUtility.SetDirty(materials[i]);}
        var plate=demo.target.Find("Iron striking plate");if(plate!=null){plate.localScale=new Vector3(.8f,.035f,.5143f);plate.GetComponent<Renderer>().sharedMaterial=Mat("Hammered training iron",new Color(.34f,.32f,.28f),.65f,.24f);}
    }
    [MenuItem("Dusk Yard/Apply visual polish and build")]
    public static void Run()
    {
        EditorSceneManager.OpenScene(Root+"Scenes/DuskYard.unity");Directory.CreateDirectory(Root+"Meshes/Polish");AssetDatabase.Refresh();
        var demo=UnityEngine.Object.FindAnyObjectByType<CombatDemo>();demo.hero.SetPositionAndRotation(new Vector3(0,0,-2),Quaternion.identity);demo.blade.localPosition=new Vector3(.32f,1.2f,.3f);demo.blade.localRotation=CombatDemo.GuardRotation;
        stone=AssetDatabase.LoadAssetAtPath<Material>(Root+"Materials/Weathered limestone.mat");iron=Mat("Weathered brazier iron",new Color(.19f,.17f,.14f),.72f,.24f);linen=Mat("Heraldic unbleached linen",new Color(.64f,.53f,.32f),0,.1f);
        var charcoal=AssetDatabase.LoadAssetAtPath<Material>(Root+"Materials/Glowing charcoal.mat");charcoal.SetColor("_EmissiveColor",new Color(4,.5f,.03f));EditorUtility.SetDirty(charcoal);
        var environment=GameObject.Find("01 | Environment").transform;var details=Group("Visual polish",environment,Vector3.zero);
        Rig(demo);Target(demo);Puddles(environment);Architecture(environment,details);Brazier(environment,details,-1);Brazier(environment,details,1);
        var key=GameObject.Find("Dusk key").GetComponent<Light>();key.color=new Color(.72f,.76f,.82f);key.intensity=160;EditorUtility.SetDirty(key);
        var bounce=GameObject.Find("Hero sky bounce").GetComponent<Light>();bounce.color=new Color(.64f,.70f,.80f);EditorUtility.SetDirty(bounce);
        var rim=GameObject.Find("Warm rim").GetComponent<Light>();rim.color=new Color(1,.70f,.40f);rim.intensity=90;EditorUtility.SetDirty(rim);
        foreach(var r in details.GetComponentsInChildren<MeshRenderer>())
        {
            GameObjectUtility.SetStaticEditorFlags(r.gameObject,StaticEditorFlags.ContributeGI|StaticEditorFlags.BatchingStatic|StaticEditorFlags.ReflectionProbeStatic);
            var filter=r.GetComponent<MeshFilter>();
            if(r.sharedMaterial==stone)
            {
                var mesh=UnityEngine.Object.Instantiate(filter.sharedMesh);var vertices=mesh.vertices;var normals=mesh.normals;var uv=new Vector2[vertices.Length];
                for(int i=0;i<uv.Length;i++){Vector3 w=r.transform.TransformPoint(vertices[i]),n=r.transform.TransformDirection(normals[i]);uv[i]=Mathf.Abs(n.y)>.7f?new Vector2(w.x,w.z)*.5f:Mathf.Abs(n.x)>.7f?new Vector2(w.z,w.y)*.5f:new Vector2(w.x,w.y)*.5f;}
                mesh.uv=uv;filter.sharedMesh=SaveMesh("Stone "+r.name,mesh);
            }
        }
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());AssetDatabase.SaveAssets();
        if(!Lightmapping.Bake())throw new Exception("Polish light bake failed");
        foreach(var probe in UnityEngine.Object.FindObjectsByType<ReflectionProbe>(FindObjectsSortMode.None))if(!Lightmapping.BakeReflectionProbe(probe,Root+"Settings/LocalReflection.exr"))throw new Exception("Reflection bake failed");
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());AssetDatabase.SaveAssets();
        File.WriteAllText("Documentation/Verification/bake.txt","Unity: "+Application.unityVersion+"\nLightmaps: "+LightmapSettings.lightmaps.Length+"\nLight probes: "+LightmapSettings.lightProbes.count+"\nReflection baked: True\nVisual polish rebake completed.\n");
        Debug.Log("DUSK_VISUAL_POLISH_COMPLETE");DuskYardFinalize.Build();
    }
}
