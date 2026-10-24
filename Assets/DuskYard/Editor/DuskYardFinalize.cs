using System;
using System.IO;
using System.Reflection;
using DuskYard;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.SceneManagement;
using UnityEditor.Build.Reporting;

public static class DuskYardFinalize
{
    const string Root = "Assets/DuskYard/";
    const string ScenePath = Root + "Scenes/DuskYard.unity";
    static Material MaterialAsset(string name, Color color, float smoothness = .2f)
    {
        string path = Root + "Materials/" + name + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null) { material = new Material(Shader.Find("HDRP/Lit")); AssetDatabase.CreateAsset(material, path); }
        material.SetColor("_BaseColor", color); material.SetFloat("_Smoothness", smoothness);
        return material;
    }
    static GameObject Shape(string name, PrimitiveType type, Transform parent, Vector3 position, Vector3 scale, Material material)
    {
        Transform found = parent.Find(name);
        if (found != null) return found.gameObject;
        var obj = GameObject.CreatePrimitive(type); obj.name = name; obj.transform.SetParent(parent, false);
        obj.transform.localPosition = position; obj.transform.localScale = scale;
        UnityEngine.Object.DestroyImmediate(obj.GetComponent<Collider>());
        obj.GetComponent<Renderer>().sharedMaterial = material;
        return obj;
    }
    static Texture2D MakeTexture(string name, bool wood, bool radial)
    {
        string folder = Root + "Textures"; Directory.CreateDirectory(folder);
        string path = folder + "/" + name + ".png";
        var texture = new Texture2D(256, 256, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[256 * 256];
        for (int y = 0; y < 256; y++) for (int x = 0; x < 256; x++)
        {
            float u = x / 256f, v = y / 256f;
            float grain = Mathf.PerlinNoise(u * (wood ? 48 : 70), v * (wood ? 3 : 70));
            float low = Mathf.PerlinNoise(u * 7, v * 7);
            float shade = .55f + grain * .25f + low * .2f;
            if (!wood && !radial)
            {
                int row = y / 32;
                float cell = (x + (row % 2) * 16) % 32;
                if (cell < 1.5f || y % 32 < 1.5f) shade *= .38f;
            }
            float alpha = 1;
            if (radial) alpha = Mathf.Pow(Mathf.Clamp01(1 - Vector2.Distance(new Vector2(u, v), new Vector2(.5f, .5f)) * 2), 2);
            pixels[y * 256 + x] = radial ? new Color(1, 1, 1, alpha) : new Color(shade, shade, shade, 1);
        }
        texture.SetPixels(pixels); texture.Apply(); File.WriteAllBytes(path, texture.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path); importer.wrapMode = TextureWrapMode.Repeat; importer.mipmapEnabled = true;
        importer.alphaSource = TextureImporterAlphaSource.FromInput; importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }
    static void ApplyPBR(Material material, string asset, bool damp = false)
    {
        string folder = Root + "Textures/PolyHaven/";
        string normalPath = folder + asset + "_normal.jpg";
        var normalImporter = (TextureImporter)AssetImporter.GetAtPath(normalPath);
        normalImporter.textureType = TextureImporterType.NormalMap; normalImporter.sRGBTexture = false; normalImporter.SaveAndReimport();
        string armPath = folder + asset + "_arm.jpg";
        var armImporter = (TextureImporter)AssetImporter.GetAtPath(armPath);
        armImporter.sRGBTexture = false; armImporter.isReadable = true; armImporter.textureCompression = TextureImporterCompression.Uncompressed; armImporter.SaveAndReimport();
        var arm = AssetDatabase.LoadAssetAtPath<Texture2D>(armPath);
        var values = arm.GetPixels();
        for (int i = 0; i < values.Length; i++) { Color v = values[i]; values[i] = new Color(v.b, v.r, 1, 1 - v.g); }
        var packed = new Texture2D(arm.width, arm.height, TextureFormat.RGBA32, false, true); packed.SetPixels(values); packed.Apply();
        string maskPath = folder + asset + "_HDRP_mask.png";
        File.WriteAllBytes(maskPath, packed.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(packed);
        AssetDatabase.ImportAsset(maskPath, ImportAssetOptions.ForceSynchronousImport);
        var maskImporter = (TextureImporter)AssetImporter.GetAtPath(maskPath); maskImporter.sRGBTexture = false; maskImporter.SaveAndReimport();
        material.SetTexture("_BaseColorMap", AssetDatabase.LoadAssetAtPath<Texture2D>(folder + asset + "_diffuse.jpg"));
        material.SetTexture("_NormalMap", AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath));
        material.SetTexture("_MaskMap", AssetDatabase.LoadAssetAtPath<Texture2D>(maskPath));
        material.SetColor("_BaseColor", damp ? new Color(.62f, .65f, .67f) : Color.white);
        material.SetTextureScale("_BaseColorMap", Vector2.one); material.SetFloat("_NormalScale", .8f);
        material.SetFloat("_SmoothnessRemapMin", damp ? .66f : .02f); material.SetFloat("_SmoothnessRemapMax", damp ? .9f : .65f);
        material.EnableKeyword("_NORMALMAP"); material.EnableKeyword("_NORMALMAP_TANGENT_SPACE"); material.EnableKeyword("_MASKMAP");
        EditorUtility.SetDirty(material);
    }
    static void WorldScaleUV(MeshRenderer renderer, int index)
    {
        var filter = renderer.GetComponent<MeshFilter>(); if (filter == null) return;
        Directory.CreateDirectory(Root + "Meshes");
        string path = Root + "Meshes/Environment_v3_" + index + ".asset";
        var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (mesh == null)
        {
            mesh = UnityEngine.Object.Instantiate(filter.sharedMesh); mesh.name = renderer.name + " world-scaled UV";
            var vertices = mesh.vertices; var normals = mesh.normals; var uv = new Vector2[vertices.Length];
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 world = renderer.transform.TransformPoint(vertices[i]); Vector3 n = renderer.transform.TransformDirection(normals[i]);
                uv[i] = Mathf.Abs(n.y) > .7f ? new Vector2(world.x, world.z) * .5f : Mathf.Abs(n.x) > .7f ? new Vector2(world.z, world.y) * .5f : new Vector2(world.x, world.y) * .5f;
            }
            mesh.uv = uv; mesh.RecalculateTangents(); Unwrapping.GenerateSecondaryUVSet(mesh); AssetDatabase.CreateAsset(mesh, path);
        }
        filter.sharedMesh = mesh;
    }
    [MenuItem("Dusk Yard/Apply corrections and bake")]
    public static void Run()
    {
        EditorSceneManager.OpenScene(ScenePath);
        DuskYardImportSetup.Configure();
        var demo = UnityEngine.Object.FindAnyObjectByType<CombatDemo>();
        if (demo == null) throw new Exception("CombatDemo missing");
        demo.target.position = new Vector3(0, 1.25f, -.30f);
        var post = GameObject.Find("Target post"); if (post != null) post.transform.position = new Vector3(0, .55f, -.30f);
        demo.blade.localPosition = new Vector3(.32f, 1.2f, .3f);
        demo.recoveryMaterial = MaterialAsset("Target recovering", new Color(.14f, .095f, .055f));
        demo.restoredMaterial = MaterialAsset("Target restored", new Color(.4f, .28f, .14f));
        demo.restoredMaterial.SetColor("_EmissiveColor", Color.black);
        demo.warmEffect = AssetDatabase.LoadAssetAtPath<Material>(Root + "Materials/Ember warm.mat");
        demo.coldEffect = AssetDatabase.LoadAssetAtPath<Material>(Root + "Materials/Ember cold.mat");
        var radial = MakeTexture("Soft particle", false, true);
        foreach (var effect in new[] { demo.warmEffect, demo.coldEffect })
        {
            effect.SetTexture("_BaseMap", radial);
            effect.SetColor("_BaseColor", effect == demo.warmEffect ? new Color(250, 100, 10, 1) : new Color(160, 180, 210, 1));
            EditorUtility.SetDirty(effect);
        }
        demo.warmTrail = AssetDatabase.LoadAssetAtPath<Material>(Root + "Materials/Blade motion warm.mat");
        if (demo.warmTrail == null) { demo.warmTrail = new Material(demo.warmEffect); AssetDatabase.CreateAsset(demo.warmTrail, Root + "Materials/Blade motion warm.mat"); }
        demo.coldTrail = AssetDatabase.LoadAssetAtPath<Material>(Root + "Materials/Blade motion cold.mat");
        if (demo.coldTrail == null) { demo.coldTrail = new Material(demo.coldEffect); AssetDatabase.CreateAsset(demo.coldTrail, Root + "Materials/Blade motion cold.mat"); }
        demo.warmTrail.SetColor("_BaseColor", new Color(12, 9, 6, .5f)); demo.coldTrail.SetColor("_BaseColor", new Color(9, 11, 13, .5f));
        var main = demo.sparks.main; main.startColor = Color.white; main.maxParticles = 256; main.startLifetime = new ParticleSystem.MinMaxCurve(.06f, .18f); main.startSize = new ParticleSystem.MinMaxCurve(.004f, .012f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(1.2f, 3.5f); main.gravityModifier = .7f;
        var color = demo.sparks.colorOverLifetime; color.enabled = true;
        Gradient gradient = new Gradient(); gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(new Color(.35f, .35f, .35f), 1) }, new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(0, 1) }); color.color = gradient;
        demo.trail.colorGradient = gradient; demo.trail.startWidth = .035f; demo.trail.endWidth = .005f; demo.trail.time = .045f; demo.trail.sharedMaterial = demo.warmTrail;
        var renderer = demo.sparks.GetComponent<ParticleSystemRenderer>(); renderer.sharedMaterial = demo.warmEffect;
        renderer.renderMode = ParticleSystemRenderMode.Stretch; renderer.lengthScale = 2; renderer.velocityScale = .025f;
        renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
        var stone = AssetDatabase.LoadAssetAtPath<Material>(Root + "Materials/Weathered limestone.mat");
        stone.SetTexture("_BaseColorMap", MakeTexture("Weathered masonry", false, false)); stone.SetTextureScale("_BaseColorMap", new Vector2(4, 4));
        stone.SetColor("_BaseColor", new Color(.32f, .31f, .29f));
        var wood = AssetDatabase.LoadAssetAtPath<Material>(Root + "Materials/Dark oak.mat");
        wood.SetTexture("_BaseColorMap", MakeTexture("Oak grain", true, false)); wood.SetColor("_BaseColor", new Color(.3f, .18f, .085f));
        var wet = AssetDatabase.LoadAssetAtPath<Material>(Root + "Materials/Wet cobble.mat");
        wet.SetColor("_BaseColor", new Color(.12f, .13f, .13f)); wet.SetFloat("_Smoothness", .78f); wet.SetFloat("_Metallic", 0);
        ApplyPBR(stone, "castle_brick_01"); ApplyPBR(wood, "oak_wood_planks"); ApplyPBR(wet, "cobblestone_floor_02", true);
        var floor = MaterialAsset("Courtyard cobblestones", Color.white); ApplyPBR(floor, "cobblestone_floor_02");
        GameObject.Find("Courtyard 28m").GetComponent<Renderer>().sharedMaterial = floor;
        foreach (var paving in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None)) if (paving.name == "Wet paving")
        {
            Vector3 scale = paving.localScale; scale.y = .004f; paving.localScale = scale; Vector3 point = paving.position; point.y = .006f; paving.position = point;
            var puddle = new Mesh(); var vertices = new Vector3[25]; var normals = new Vector3[25]; var triangles = new int[72]; normals[0] = Vector3.up;
            for (int i = 0; i < 24; i++) { float angle = i * Mathf.PI * 2 / 24; float radius = .48f + .10f * Mathf.PerlinNoise(point.x + i * .31f, point.z); vertices[i + 1] = new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius * .8f); normals[i + 1] = Vector3.up; triangles[i * 3] = 0; triangles[i * 3 + 1] = (i + 1) % 24 + 1; triangles[i * 3 + 2] = i + 1; }
            puddle.vertices = vertices; puddle.normals = normals; puddle.triangles = triangles; paving.GetComponent<MeshFilter>().sharedMesh = puddle;
        }
        var iron = AssetDatabase.LoadAssetAtPath<Material>(Root + "Materials/Forged iron.mat");
        var cloth = AssetDatabase.LoadAssetAtPath<Material>(Root + "Materials/Player cloak.mat");
        Shape("Left boot", PrimitiveType.Cube, demo.hero, new Vector3(-.17f, .2f, .07f), new Vector3(.22f, .36f, .35f), wood);
        Shape("Right boot", PrimitiveType.Cube, demo.hero, new Vector3(.17f, .2f, .07f), new Vector3(.22f, .36f, .35f), wood);
        Shape("Left arm", PrimitiveType.Capsule, demo.hero, new Vector3(-.39f, 1.15f, .04f), new Vector3(.18f, .32f, .18f), cloth);
        Shape("Right arm", PrimitiveType.Capsule, demo.hero, new Vector3(.39f, 1.15f, .10f), new Vector3(.18f, .32f, .18f), cloth);
        Shape("Left pauldron", PrimitiveType.Sphere, demo.hero, new Vector3(-.34f, 1.45f, 0), new Vector3(.32f, .23f, .32f), iron);
        Shape("Right pauldron", PrimitiveType.Sphere, demo.hero, new Vector3(.34f, 1.45f, 0), new Vector3(.32f, .23f, .32f), iron);
        Shape("Sword guard", PrimitiveType.Cube, demo.blade, new Vector3(0, 0, .04f), new Vector3(.36f, .055f, .055f), iron);
        var shield = Shape("Iron striking plate", PrimitiveType.Cylinder, demo.target, new Vector3(0, 0, -.55f), new Vector3(.8f, .035f, .8f), iron);
        shield.transform.localRotation = Quaternion.Euler(90, 0, 0); shield.layer = 8; if (shield.GetComponent<BoxCollider>() == null) shield.AddComponent<BoxCollider>();
        var environment = GameObject.Find("01 | Environment");
        var env = environment.transform;
        Shape("Gate left pier", PrimitiveType.Cube, env, new Vector3(-3, 1.5f, 13.5f), new Vector3(1, 3, 1.2f), stone);
        Shape("Gate right pier", PrimitiveType.Cube, env, new Vector3(3, 1.5f, 13.5f), new Vector3(1, 3, 1.2f), stone);
        var oldLast = env.Find("Arch voussoir 16"); if (oldLast != null) UnityEngine.Object.DestroyImmediate(oldLast.gameObject);
        for (int i = 0; i < 16; i++)
        {
            var block = Shape("Arch voussoir " + i, PrimitiveType.Cube, env, new Vector3(0, 3, 13.5f), Vector3.one, stone);
            block.transform.localPosition = new Vector3(0, 3, 13.5f); block.transform.localScale = Vector3.one; block.transform.localRotation = Quaternion.identity;
            float a = i * Mathf.PI / 16 + .003f, b = (i + 1) * Mathf.PI / 16 - .003f;
            Vector3[] corners = new Vector3[8];
            for (int side = 0; side < 2; side++) { float z = side == 0 ? -.575f : .575f; corners[side * 4] = new Vector3(Mathf.Cos(a) * 2.55f, Mathf.Sin(a) * 2.55f, z); corners[side * 4 + 1] = new Vector3(Mathf.Cos(a) * 3.45f, Mathf.Sin(a) * 3.45f, z); corners[side * 4 + 2] = new Vector3(Mathf.Cos(b) * 3.45f, Mathf.Sin(b) * 3.45f, z); corners[side * 4 + 3] = new Vector3(Mathf.Cos(b) * 2.55f, Mathf.Sin(b) * 2.55f, z); }
            int[] faceIndices = { 0,3,2,1, 4,5,6,7, 0,4,7,3, 1,2,6,5, 0,1,5,4, 3,7,6,2 };
            Vector3[] vertices = new Vector3[24]; int[] triangles = new int[36];
            for (int f = 0; f < 6; f++) { for (int v = 0; v < 4; v++) vertices[f * 4 + v] = corners[faceIndices[f * 4 + v]]; int t = f * 6, j = f * 4; triangles[t] = j; triangles[t + 1] = j + 1; triangles[t + 2] = j + 2; triangles[t + 3] = j; triangles[t + 4] = j + 2; triangles[t + 5] = j + 3; }
            var wedge = new Mesh(); wedge.vertices = vertices; wedge.triangles = triangles; wedge.RecalculateNormals(); block.GetComponent<MeshFilter>().sharedMesh = wedge;
        }
        var gate = Shape("Closed oak gate", PrimitiveType.Cube, env, new Vector3(0, 2.15f, 14), new Vector3(5.3f, 4.3f, .22f), wood);
        if (gate.GetComponent<BoxCollider>() == null) gate.AddComponent<BoxCollider>();
        for (int i = 0; i < 3; i++) Shape("Gate iron strap " + i, PrimitiveType.Cube, env, new Vector3(0, .8f + i * 1.3f, 13.85f), new Vector3(5.2f, .12f, .06f), iron);
        var wool = MaterialAsset("Faded burgundy wool", new Color(.16f, .045f, .032f), .05f);
        for (int side = -1; side <= 1; side += 2)
        {
            Shape("Banner " + side, PrimitiveType.Cube, env, new Vector3(side * 5, 2.35f, 13.28f), new Vector3(.9f, 1.65f, .035f), wool);
            Shape("Banner rail " + side, PrimitiveType.Cylinder, env, new Vector3(side * 5, 3.22f, 13.22f), new Vector3(.045f, .55f, .045f), iron).transform.localRotation = Quaternion.Euler(0, 0, 90);
        }
        var embers = MaterialAsset("Glowing charcoal", new Color(.018f, .012f, .008f)); embers.SetColor("_EmissiveColor", new Color(30, 3, .1f));
        for (int side = -1; side <= 1; side += 2) for (int i = 0; i < 7; i++)
            Shape("Charcoal " + side + " " + i, PrimitiveType.Sphere, env, new Vector3(side * 5 + Mathf.Cos(i * 2.4f) * .18f, 1.50f, 5 + Mathf.Sin(i * 2.4f) * .18f), new Vector3(.18f, .085f, .14f), embers);
        var fillObject = GameObject.Find("Hero sky bounce"); if (fillObject == null) fillObject = new GameObject("Hero sky bounce");
        fillObject.transform.SetParent(GameObject.Find("05 | Lighting").transform); fillObject.transform.rotation = Quaternion.Euler(50, 0, 0);
        var bounce = fillObject.GetComponent<Light>(); if (bounce == null) bounce = fillObject.AddComponent<Light>();
        bounce.type = LightType.Directional; bounce.color = new Color(.45f, .55f, .7f); bounce.lightUnit = LightUnit.Lux; bounce.intensity = 80; bounce.shadows = LightShadows.None;
        if (fillObject.GetComponent<HDAdditionalLightData>() == null) fillObject.AddComponent<HDAdditionalLightData>();
        cloth.SetColor("_BaseColor", new Color(.22f, .25f, .22f)); EditorUtility.SetDirty(cloth);
        int meshIndex = 0;
        foreach (var r in environment.GetComponentsInChildren<MeshRenderer>())
        {
            GameObjectUtility.SetStaticEditorFlags(r.gameObject, StaticEditorFlags.ContributeGI | StaticEditorFlags.BatchingStatic | StaticEditorFlags.ReflectionProbeStatic | StaticEditorFlags.OccluderStatic);
            WorldScaleUV(r, meshIndex++);
            r.receiveGI = ReceiveGI.Lightmaps; r.scaleInLightmap = .5f;
        }
        foreach (var r in demo.hero.GetComponentsInChildren<Renderer>()) r.lightProbeUsage = LightProbeUsage.BlendProbes;
        demo.targetRenderer.lightProbeUsage = LightProbeUsage.BlendProbes;
        var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(Root + "Settings/DuskVolume.asset");
        profile.components.RemoveAll(component => component == null);
        if (!profile.TryGet<Exposure>(out var fixedExposure)) fixedExposure = profile.Add<Exposure>();
        fixedExposure.mode.Override(ExposureMode.Fixed); fixedExposure.fixedExposure.Override(7);
        if (!profile.TryGet<Tonemapping>(out var tone)) tone = profile.Add<Tonemapping>(); tone.mode.Override(TonemappingMode.ACES);
        if (!profile.TryGet<ColorAdjustments>(out var grade)) grade = profile.Add<ColorAdjustments>(); grade.saturation.Override(-10); grade.contrast.Override(8);
        if (!profile.TryGet<Bloom>(out var bloom)) bloom = profile.Add<Bloom>(); bloom.intensity.Override(.02f);
        if (!profile.TryGet<VisualEnvironment>(out var visual)) visual = profile.Add<VisualEnvironment>();
        visual.skyType.Override((int)SkyType.Gradient);
        if (!profile.TryGet<GradientSky>(out var sky)) sky = profile.Add<GradientSky>();
        sky.top.Override(new Color(.045f, .07f, .13f)); sky.middle.Override(new Color(.22f, .15f, .12f)); sky.bottom.Override(new Color(.04f, .045f, .055f));
        sky.active = false;
        string skyPath = Root + "Textures/PolyHaven/qwantani_dusk_1_puresky.hdr";
        var skyImporter = (TextureImporter)AssetImporter.GetAtPath(skyPath); skyImporter.textureShape = TextureImporterShape.TextureCube;
        skyImporter.generateCubemap = TextureImporterGenerateCubemap.AutoCubemap; skyImporter.sRGBTexture = false; skyImporter.SaveAndReimport();
        if (!profile.TryGet<HDRISky>(out var photographedSky)) photographedSky = profile.Add<HDRISky>();
        photographedSky.active = true; photographedSky.hdriSky.Override(AssetDatabase.LoadAssetAtPath<Cubemap>(skyPath));
        photographedSky.skyIntensityMode.Override(SkyIntensityMode.Exposure); photographedSky.exposure.Override(3); photographedSky.rotation.Override(110);
        visual.skyType.Override((int)SkyType.HDRI); fixedExposure.fixedExposure.Override(5);
        demo.hitMaterial.SetColor("_BaseColor", new Color(.20f, .10f, .035f)); demo.hitMaterial.SetColor("_EmissiveColor", Color.black); EditorUtility.SetDirty(demo.hitMaterial);
        foreach (var component in profile.components) { if (!AssetDatabase.Contains(component)) AssetDatabase.AddObjectToAsset(component, profile); EditorUtility.SetDirty(component); }
        var lighting = AssetDatabase.LoadAssetAtPath<LightingSettings>(Root + "Settings/DuskLighting.asset");
        if (lighting == null) { lighting = new LightingSettings(); AssetDatabase.CreateAsset(lighting, Root + "Settings/DuskLighting.asset"); }
        lighting.bakedGI = true; lighting.realtimeGI = false; lighting.lightmapper = LightingSettings.Lightmapper.ProgressiveCPU;
        lighting.lightmapResolution = 6; lighting.lightmapMaxSize = 1024; lighting.directSampleCount = 16;
        lighting.indirectSampleCount = 64; lighting.environmentSampleCount = 32; lighting.maxBounces = 2;
        lighting.mixedBakeMode = MixedLightingMode.Shadowmask; Lightmapping.lightingSettings = lighting;
        foreach (var light in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
        {
            if (light.name == "Dusk key") { light.lightmapBakeType = LightmapBakeType.Mixed; light.lightUnit = LightUnit.Lux; light.intensity = 180; }
            else if (light.name == "Sky fill" || light.name == "Brazier warm pool") { light.lightmapBakeType = LightmapBakeType.Baked; light.shadows = LightShadows.None; if (light.type == LightType.Point) { light.lightUnit = LightUnit.Candela; light.intensity = 140; } }
            else { light.lightmapBakeType = LightmapBakeType.Realtime; light.shadows = LightShadows.None; if (light.name == "Warm rim") { light.lightUnit = LightUnit.Candela; light.intensity = 70; } }
        }
        // HDRP wizard uses this same internal API for high-quality lightmap encoding.
        var encoding = typeof(PlayerSettings).Assembly.GetType("UnityEditor.LightmapEncodingQuality");
        var setter = typeof(PlayerSettings).GetMethod("SetLightmapEncodingQualityForPlatform", BindingFlags.NonPublic | BindingFlags.Static);
        if (encoding != null && setter != null) setter.Invoke(null, new[] { (object)BuildTarget.StandaloneWindows64, Enum.Parse(encoding, "High") });
        QualitySettings.vSyncCount = 0; QualitySettings.SetQualityLevel(QualitySettings.names.Length - 1, false); QualitySettings.vSyncCount = 0;
        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64, false); PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64, new[] { GraphicsDeviceType.Direct3D11 }); GraphicsSettings.lightsUseLinearIntensity = true; PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow; PlayerSettings.runInBackground = true; PlayerSettings.defaultScreenWidth = 1920; PlayerSettings.defaultScreenHeight = 1080;
        PlayerSettings.companyName = "DuskYard"; PlayerSettings.productName = "Dusk Yard";
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        EditorUtility.SetDirty(demo); EditorUtility.SetDirty(profile); EditorUtility.SetDirty(lighting); EditorUtility.SetDirty(stone); EditorUtility.SetDirty(wood); EditorUtility.SetDirty(wet);
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene()); AssetDatabase.SaveAssets();
        Debug.Log("DUSK_CORRECTIONS_APPLIED");
        if (!Lightmapping.Bake()) throw new Exception("Light bake failed");
        foreach (var probe in UnityEngine.Object.FindObjectsByType<ReflectionProbe>(FindObjectsSortMode.None))
        {
            string path = Root + "Settings/LocalReflection.exr";
            probe.mode = ReflectionProbeMode.Baked;
            if (!Lightmapping.BakeReflectionProbe(probe, path)) throw new Exception("Reflection bake failed");
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            probe.bakedTexture = AssetDatabase.LoadAssetAtPath<Texture>(path);
        }
        LightProbes.Tetrahedralize();
        PrefabUtility.SaveAsPrefabAsset(demo.hero.gameObject, Root + "Prefabs/Player.prefab");
        PrefabUtility.SaveAsPrefabAsset(demo.target.gameObject, Root + "Prefabs/TrainingTarget.prefab");
        PrefabUtility.SaveAsPrefabAsset(demo.sparks.gameObject, Root + "Prefabs/ContactSparks.prefab");
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene()); AssetDatabase.SaveAssets();
        Directory.CreateDirectory("Documentation/Verification");
        File.WriteAllText("Documentation/Verification/bake.txt", "Unity: " + Application.unityVersion + "\nLightmaps: " + LightmapSettings.lightmaps.Length + "\nLight probes: " + (LightmapSettings.lightProbes == null ? 0 : LightmapSettings.lightProbes.count) + "\nReflection baked: " + (UnityEngine.Object.FindAnyObjectByType<ReflectionProbe>().bakedTexture != null));
        Debug.Log("DUSK_BAKE_COMPLETE");
    }
    public static void Validate()
    {
        EditorSceneManager.OpenScene(ScenePath);
        var failures = new System.Collections.Generic.List<string>();
        foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects()) foreach (var t in root.GetComponentsInChildren<Transform>(true))
            if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject) > 0) failures.Add("Missing script: " + t.name);
        foreach (var renderer in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None)) foreach (var material in renderer.sharedMaterials)
        {
            if (material == null || material.shader == null) failures.Add("Missing material/shader: " + renderer.name);
            else if (ShaderUtil.ShaderHasError(material.shader))
            {
                failures.Add("Shader: " + renderer.name + " / " + material.name);
                foreach (var message in ShaderUtil.GetShaderMessages(material.shader)) failures.Add(message.message);
            }
        }
        var volume = AssetDatabase.LoadAssetAtPath<VolumeProfile>(Root + "Settings/DuskVolume.asset");
        if (volume == null || volume.components.Count < 6 || volume.components.Exists(component => component == null)) failures.Add("Global Volume components were not persisted");
        var demo = UnityEngine.Object.FindAnyObjectByType<CombatDemo>();
        if (demo == null || demo.recoveryMaterial == null || demo.restoredMaterial == null || demo.warmEffect == null || demo.coldEffect == null || demo.warmTrail == null || demo.coldTrail == null) failures.Add("Combat material dependency");
        if (LightmapSettings.lightmaps.Length == 0) failures.Add("No baked lightmaps");
        if (LightmapSettings.lightProbes == null || LightmapSettings.lightProbes.count < 1) failures.Add("No baked Light Probes");
        var probe = UnityEngine.Object.FindAnyObjectByType<HDAdditionalReflectionData>();
        if (probe == null || probe.bakedTexture == null) failures.Add("No HDRP baked reflection texture");
        Directory.CreateDirectory("Documentation/Verification");
        File.WriteAllText("Documentation/Verification/references.txt", "Unity: " + Application.unityVersion + "\nMissing references / shader / bake errors: " + failures.Count + "\n" + string.Join("\n", failures));
        if (failures.Count > 0) throw new Exception("Preflight failed: " + string.Join("; ", failures));
        Debug.Log("DUSK_PREFLIGHT_PASSED");
    }
    public static void PatchRuntime()
    {
        EditorSceneManager.OpenScene(ScenePath);
        var demo = UnityEngine.Object.FindAnyObjectByType<CombatDemo>();
        demo.blade.localRotation = Quaternion.Euler(-5, -55, -8);
        var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(Root + "Settings/DuskVolume.asset");
        if (profile.TryGet<Exposure>(out var exposure)) { exposure.fixedExposure.Override(5); EditorUtility.SetDirty(exposure); }
        var bounce = GameObject.Find("Hero sky bounce").GetComponent<Light>(); bounce.intensity = 160; EditorUtility.SetDirty(bounce);
        var fill = GameObject.Find("Sky fill").GetComponent<Light>(); fill.lightUnit = LightUnit.Lux; fill.intensity = 90; EditorUtility.SetDirty(fill);
        var plate = demo.target.Find("Iron striking plate");
        if (plate != null) { plate.gameObject.layer = 8; if (plate.GetComponent<BoxCollider>() == null) plate.gameObject.AddComponent<BoxCollider>(); }
        var leather = MaterialAsset("Dark leather", new Color(.075f, .048f, .026f), .22f);
        foreach (var name in new[] { "Left boot", "Right boot" }) { var boot = demo.hero.Find(name); if (boot != null) boot.GetComponent<Renderer>().sharedMaterial = leather; }
        PrefabUtility.SaveAsPrefabAsset(demo.hero.gameObject, Root + "Prefabs/Player.prefab");
        PrefabUtility.SaveAsPrefabAsset(demo.target.gameObject, Root + "Prefabs/TrainingTarget.prefab");
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene()); AssetDatabase.SaveAssets();
    }
    public static void Build()
    {
        PatchRuntime(); Validate();
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = new[] { ScenePath }, locationPathName = "Builds/Windows/DuskYard.exe", target = BuildTarget.StandaloneWindows64, options = BuildOptions.Development });
        if (report.summary.result != BuildResult.Succeeded) throw new Exception("Build failed: " + report.summary.result);
        File.WriteAllText("Documentation/Verification/build.txt", "Unity: " + Application.unityVersion + "\nResult: " + report.summary.result + "\nErrors: " + report.summary.totalErrors + "\nWarnings: " + report.summary.totalWarnings + "\nBytes: " + report.summary.totalSize);
        Debug.Log("DUSK_BUILD_COMPLETE");
    }
}


