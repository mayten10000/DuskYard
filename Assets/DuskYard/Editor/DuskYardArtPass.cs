using System;
using System.Collections.Generic;
using System.IO;
using DuskYard;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

// Explicit, repeatable art pass; never runs automatically when the project opens.
public static class DuskYardArtPass
{
    const string Root = "Assets/DuskYard/";
    static int meshId;
    static Material steel, leather, wool, mail, oak, black, brass;
    static Transform Group(string name, Transform parent, Vector3 position)
    {
        var g = new GameObject(name); g.transform.SetParent(parent, false); g.transform.localPosition = position; return g.transform;
    }
    static Material Mat(string name, Color color, float metal, float smooth)
    {
        string path = Root + "Materials/" + name + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null) { m = new Material(Shader.Find("HDRP/Lit")); AssetDatabase.CreateAsset(m, path); }
        m.SetColor("_BaseColor", color); m.SetFloat("_Metallic", metal); m.SetFloat("_Smoothness", smooth); EditorUtility.SetDirty(m); return m;
    }
    static GameObject MeshObject(string name, Transform parent, Mesh mesh, Material material)
    {
        mesh.RecalculateNormals(); mesh.RecalculateTangents(); mesh.RecalculateBounds();
        string path = Root + "Meshes/Art/" + meshId++ + ".asset";
        var old = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (old != null) { EditorUtility.CopySerialized(mesh, old); UnityEngine.Object.DestroyImmediate(mesh); mesh = old; } else AssetDatabase.CreateAsset(mesh, path);
        var t = Group(name, parent, Vector3.zero); t.gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
        var r = t.gameObject.AddComponent<MeshRenderer>(); r.sharedMaterial = material; r.lightProbeUsage = LightProbeUsage.BlendProbes; return t.gameObject;
    }
    // Elliptical rings give cloth/armour an anatomical silhouette without stretched capsules.
    static Transform Form(string name, Transform parent, Vector3 origin, Vector3[] rings, Material material, int sides = 24)
    {
        var vertices = new List<Vector3>(); var uv = new List<Vector2>(); var tris = new List<int>();
        for (int j = 0; j < rings.Length; j++) for (int i = 0; i <= sides; i++)
        {
            float a = i * Mathf.PI * 2 / sides; var ring = rings[j];
            vertices.Add(new Vector3(Mathf.Sin(a) * ring.x, ring.y, Mathf.Cos(a) * ring.z)); uv.Add(new Vector2(i / (float)sides, j / (float)(rings.Length - 1)));
            if (j > 0 && i < sides) { int k = j * (sides + 1) + i; tris.AddRange(new[] { k, k - sides - 1, k + 1, k + 1, k - sides - 1, k - sides }); }
        }
        int bottom = vertices.Count; vertices.Add(new Vector3(0, rings[0].y, 0)); uv.Add(Vector2.zero);
        int top = vertices.Count; vertices.Add(new Vector3(0, rings[rings.Length - 1].y, 0)); uv.Add(Vector2.one);
        for (int i = 0; i < sides; i++) { tris.AddRange(new[] { bottom, i + 1, i, top, (rings.Length - 1) * (sides + 1) + i, (rings.Length - 1) * (sides + 1) + i + 1 }); }
        var mesh = new Mesh(); mesh.SetVertices(vertices); mesh.SetUVs(0, uv); mesh.SetTriangles(tris, 0);
        var obj = MeshObject(name, parent, mesh, material); obj.transform.localPosition = origin; return obj.transform;
    }
    static Transform Box(string name, Transform parent, Vector3 p, Vector3 size, Material material)
    {
        var obj = GameObject.CreatePrimitive(PrimitiveType.Cube); obj.name = name; obj.transform.SetParent(parent, false); obj.transform.localPosition = p; obj.transform.localScale = size;
        UnityEngine.Object.DestroyImmediate(obj.GetComponent<Collider>()); obj.GetComponent<Renderer>().sharedMaterial = material; return obj.transform;
    }
    static Transform Link(string name, Transform parent, Vector3 a, Vector3 b, float width, float depth, Material material)
    {
        var t = Form(name, parent, a, new[] { new Vector3(width * .8f, 0, depth * .8f), new Vector3(width, .035f, depth), new Vector3(width * .78f, Vector3.Distance(a,b) - .025f, depth * .85f), new Vector3(width * .7f, Vector3.Distance(a,b), depth * .7f) }, material, 16);
        t.localRotation = Quaternion.FromToRotation(Vector3.up, b - a); return t;
    }
    static void SurfaceTextures()
    {
        const int n = 256;
        foreach (bool chain in new[] { false, true })
        {
            var texture = new Texture2D(n,n,TextureFormat.RGBA32,false); var pixels = new Color[n*n];
            for (int y=0;y<n;y++) for(int x=0;x<n;x++)
            {
                float u=x/(float)n, v=y/(float)n, shade;
                if(chain) { float cx=(u*16+(Mathf.Floor(v*24)%2)*.5f)%1-.5f, cy=(v*24)%1-.5f; float d=Mathf.Sqrt(cx*cx+cy*cy*1.7f); shade=Mathf.Lerp(.16f,.65f,Mathf.Exp(-Mathf.Pow((d-.32f)*22,2))); }
                else { float seam=Mathf.Pow(Mathf.Abs(Mathf.Sin(u*Mathf.PI*24)),.15f); shade=(.65f+.20f*Mathf.PerlinNoise(u*90,v*90))*seam; }
                pixels[y*n+x]=new Color(shade,shade,shade);
            }
            texture.SetPixels(pixels); texture.Apply(); string path=Root+"Textures/"+(chain?"Riveted mail":"Quilted linen")+".png";
            File.WriteAllBytes(path,texture.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(texture); AssetDatabase.ImportAsset(path);
            var m=chain?mail:wool; m.SetTexture("_BaseColorMap",AssetDatabase.LoadAssetAtPath<Texture2D>(path)); EditorUtility.SetDirty(m);
        }
    }
    static void Character(CombatDemo demo)
    {
        var hero=demo.hero;
        foreach(var r in hero.GetComponentsInChildren<Renderer>()) if(!r.transform.IsChildOf(demo.blade)) r.enabled=false;
        var previous=hero.Find("Medieval guard"); if(previous!=null) UnityEngine.Object.DestroyImmediate(previous.gameObject);
        var root=Group("Medieval guard",hero,Vector3.zero);
        Form("Quilted gambeson",root,Vector3.zero,new[]{new Vector3(.26f,.86f,.17f),new Vector3(.22f,1.07f,.15f),new Vector3(.24f,1.3f,.17f),new Vector3(.3f,1.44f,.17f),new Vector3(.16f,1.52f,.13f)},wool);
        Form("Mail skirt",root,Vector3.zero,new[]{new Vector3(.30f,.72f,.19f),new Vector3(.27f,.90f,.18f),new Vector3(.24f,1.04f,.16f)},mail);
        Form("Leather belt",root,Vector3.zero,new[]{new Vector3(.247f,1.04f,.171f),new Vector3(.244f,1.10f,.169f)},leather);
        Box("Buckle",root,new Vector3(.015f,1.07f,.177f),new Vector3(.085f,.075f,.018f),brass);
        Box("Buckle inset",root,new Vector3(.015f,1.07f,.189f),new Vector3(.049f,.042f,.008f),leather);
        Form("Belt pouch",root,new Vector3(-.27f,.89f,.02f),new[]{new Vector3(.06f,0,.06f),new Vector3(.085f,.04f,.075f),new Vector3(.07f,.15f,.055f)},leather,16);
        Form("Mail coif",root,Vector3.zero,new[]{new Vector3(.25f,1.43f,.17f),new Vector3(.16f,1.56f,.14f),new Vector3(.145f,1.72f,.145f)},mail);
        Form("Nasal helmet",root,Vector3.zero,new[]{new Vector3(.162f,1.67f,.168f),new Vector3(.163f,1.78f,.17f),new Vector3(.135f,1.86f,.14f),new Vector3(.065f,1.95f,.07f),new Vector3(.012f,1.985f,.015f)},steel,32);
        Form("Helmet rim",root,Vector3.zero,new[]{new Vector3(.17f,1.665f,.176f),new Vector3(.17f,1.70f,.176f)},steel,32);
        Box("Shadowed face",root,new Vector3(0,1.64f,.13f),new Vector3(.20f,.12f,.04f),black);
        Box("Nose guard",root,new Vector3(0,1.65f,.185f),new Vector3(.028f,.19f,.02f),steel);
        for(int side=-1;side<=1;side+=2)
        {
            float x=side*.13f;
            var leg=Group(side<0?"Left leg pose":"Right leg pose",root,new Vector3(x,.86f,0));
            Link("Wool hose",leg,Vector3.zero,new Vector3(0,-.41f,0),.10f,.105f,wool);
            Link("Leather greave",leg,new Vector3(0,-.38f,0),new Vector3(0,-.71f,.005f),.085f,.085f,leather);
            Form("Turnshoe",leg,new Vector3(0,-.80f,.07f),new[]{new Vector3(.09f,0,.16f),new Vector3(.09f,.06f,.17f),new Vector3(.073f,.13f,.10f)},leather,20);
            for(int i=0;i<2;i++) Form("Boot strap",leg,new Vector3(0,-.57f+i*.11f,0),new[]{new Vector3(.092f,0,.093f),new Vector3(.092f,.025f,.093f)},black,16);
            Vector3 shoulder=new Vector3(side*.30f,1.42f,0), elbow=new Vector3(side*.39f,1.16f,.03f), hand=new Vector3(side*.34f,1.10f,.24f);
            Link("Mail upper sleeve",root,shoulder,elbow,.115f,.12f,mail);
            Link("Leather vambrace",root,elbow,hand,.075f,.08f,leather);
            Form("Glove",root,hand,new[]{new Vector3(.066f,-.04f,.06f),new Vector3(.07f,.06f,.065f)},leather,16);
            Form("Shoulder cap",root,shoulder+Vector3.down*.06f,new[]{new Vector3(.135f,0,.145f),new Vector3(.14f,.09f,.15f),new Vector3(.08f,.13f,.09f)},steel,20);
        }
        // A split, folded cloth back panel replaces the single capsule cloak.
        var verts=new List<Vector3>(); var uv=new List<Vector2>(); var tris=new List<int>();
        for(int j=0;j<6;j++)for(int i=0;i<13;i++) { float t=j/5f,u=i/12f; verts.Add(new Vector3((u-.5f)*Mathf.Lerp(.42f,.61f,t),Mathf.Lerp(1.46f,.72f,t),-.18f-.035f*Mathf.Cos(u*Mathf.PI*12)-t*.035f));uv.Add(new Vector2(u,t)); if(j>0&&i<12){int k=j*13+i;tris.AddRange(new[]{k,k-13,k+1,k+1,k-13,k-12});} }
        var cape=new Mesh();cape.SetVertices(verts);cape.SetUVs(0,uv);cape.SetTriangles(tris,0);MeshObject("Folded surcoat back",root,cape,wool);
        var gait=hero.GetComponent<GuardGait>();if(gait==null)gait=hero.gameObject.AddComponent<GuardGait>();gait.left=root.Find("Left leg pose");gait.right=root.Find("Right leg pose");
        PrefabUtility.SaveAsPrefabAsset(hero.gameObject,Root+"Prefabs/Player.prefab");
    }
    static void Props()
    {
        var env=GameObject.Find("01 | Environment").transform;
        var old=env.Find("Crafted details");if(old!=null)UnityEngine.Object.DestroyImmediate(old.gameObject);
        var details=Group("Crafted details",env,Vector3.zero);
        var candidates=new List<Transform>();foreach(Transform t in env) if(t.name.StartsWith("Supply crate ",StringComparison.Ordinal))candidates.Add(t);
        foreach(var source in candidates)
        {
            source.GetComponent<Renderer>().enabled=false;
            var crate=Group("Oak chest",details,source.localPosition);crate.localRotation=source.localRotation;
            Vector3 size=source.localScale;
            for(int i=0;i<6;i++)Box("Separate oak stave",crate,new Vector3((i-2.5f)*size.x/6,0,0),new Vector3(size.x/6-.009f,size.y,size.z),oak);
            foreach(int side in new[]{-1,1})
            {
                Box("Forged binding",crate,new Vector3(side*size.x*.32f,0,0),new Vector3(.065f,size.y+.018f,size.z+.018f),steel);
                Box("Corner post",crate,new Vector3(side*size.x*.43f,0,-size.z*.5f-.025f),new Vector3(.11f,size.y,.065f),oak);
                for(int n=0;n<3;n++)Box("Hand-forged nail",crate,new Vector3(side*size.x*.32f,(n-1)*size.y*.32f,-size.z*.5f-.016f),new Vector3(.025f,.025f,.012f),black);
            }
        }
        // Remove the accidental central buttress hiding the actual gate.
        foreach(Transform t in env)if(t.name=="Buttress"&&Mathf.Abs(t.localPosition.x)<.1f)t.gameObject.SetActive(false);
        var gate=env.Find("Closed oak gate");if(gate!=null)gate.GetComponent<Renderer>().enabled=false;
        for(int i=0;i<16;i++)Box("Gate vertical plank",details,new Vector3((i-7.5f)*.326f,2.14f,13.99f),new Vector3(.314f,4.28f,.23f),oak);
        for(int side=-1;side<=1;side+=2)
        {
            var diagonal=Box("Gate diagonal brace",details,new Vector3(side*1.32f,2.1f,13.77f),new Vector3(.14f,3.5f,.10f),oak);diagonal.localRotation=Quaternion.Euler(0,0,side*31);
            Form("Gate pull ring",details,new Vector3(side*.32f,1.78f,13.67f),new[]{new Vector3(.075f,0,.023f),new Vector3(.09f,.12f,.023f)},steel,20).localRotation=Quaternion.Euler(90,0,0);
        }
        foreach(var r in details.GetComponentsInChildren<MeshRenderer>())
        {
            GameObjectUtility.SetStaticEditorFlags(r.gameObject,StaticEditorFlags.ContributeGI|StaticEditorFlags.BatchingStatic|StaticEditorFlags.ReflectionProbeStatic);
        }
    }
    [MenuItem("Dusk Yard/Apply crafted character and props")]
    public static void Run()
    {
        EditorSceneManager.OpenScene(Root+"Scenes/DuskYard.unity");
        Directory.CreateDirectory(Root+"Meshes/Art");AssetDatabase.Refresh();meshId=0;
        steel=Mat("Brushed tempered steel",new Color(.40f,.43f,.45f),.88f,.36f);leather=Mat("Worn brown leather",new Color(.13f,.075f,.035f),0,.25f);
        wool=Mat("Quilted olive gambeson",new Color(.32f,.29f,.19f),0,.12f);mail=Mat("Dark riveted chainmail",new Color(.44f,.46f,.48f),.85f,.32f);
        black=Mat("Iron recess",new Color(.025f,.022f,.018f),.3f,.2f);brass=Mat("Aged buckle brass",new Color(.30f,.22f,.09f),.7f,.32f);oak=AssetDatabase.LoadAssetAtPath<Material>(Root+"Materials/Dark oak.mat");
        SurfaceTextures();Character(UnityEngine.Object.FindAnyObjectByType<CombatDemo>());Props();
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());AssetDatabase.SaveAssets();
        if(!Lightmapping.Bake())throw new Exception("Art pass bake failed");
        foreach(var probe in UnityEngine.Object.FindObjectsByType<ReflectionProbe>(FindObjectsSortMode.None))if(!Lightmapping.BakeReflectionProbe(probe,Root+"Settings/LocalReflection.exr"))throw new Exception("Reflection bake failed");
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());AssetDatabase.SaveAssets();Debug.Log("DUSK_ART_PASS_COMPLETE");
        DuskYardFinalize.Build();
    }
}
