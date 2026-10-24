using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEditor.Build.Reporting;
using DuskYard;

public static class YardBuilder {
 static Material stone,wood,iron,wet,cloth;
 static GameObject Box(string n, Vector3 p, Vector3 s, Material m,Transform parent=null,PrimitiveType type=PrimitiveType.Cube){var g=GameObject.CreatePrimitive(type);g.name=n;g.transform.SetParent(parent);g.transform.position=p;g.transform.localScale=s;g.GetComponent<Renderer>().sharedMaterial=m;return g;}
 static Material Mat(string name,Color color,float smooth=0,float metal=0){var m=new Material(Shader.Find("HDRP/Lit"));m.SetColor("_BaseColor",color);m.SetFloat("_Smoothness",smooth);m.SetFloat("_Metallic",metal);AssetDatabase.CreateAsset(m,"Assets/DuskYard/Materials/"+name+".mat");return m;}
 [MenuItem("Dusk Yard/Generate scene")]
 public static void Generate(){
  System.IO.Directory.CreateDirectory("Assets/DuskYard/Materials");System.IO.Directory.CreateDirectory("Assets/DuskYard/Scenes");System.IO.Directory.CreateDirectory("Assets/DuskYard/Prefabs");System.IO.Directory.CreateDirectory("Assets/DuskYard/Settings");
  var pipeline=ScriptableObject.CreateInstance<HDRenderPipelineAsset>();AssetDatabase.CreateAsset(pipeline,"Assets/DuskYard/Settings/DuskHDRP.asset");GraphicsSettings.defaultRenderPipeline=pipeline;QualitySettings.renderPipeline=pipeline;
  PlayerSettings.colorSpace=ColorSpace.Linear;PlayerSettings.defaultScreenWidth=1920;PlayerSettings.defaultScreenHeight=1080;PlayerSettings.fullScreenMode=FullScreenMode.Windowed;PlayerSettings.companyName="DuskYard";PlayerSettings.productName="Dusk Yard";
  var tags=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);tags.FindProperty("layers").GetArrayElementAtIndex(8).stringValue="Target";tags.FindProperty("layers").GetArrayElementAtIndex(9).stringValue="Player";tags.ApplyModifiedProperties();
  EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
  stone=Mat("Weathered limestone",new Color(.22f,.24f,.26f),.18f);wood=Mat("Dark oak",new Color(.17f,.095f,.045f),.22f);iron=Mat("Forged iron",new Color(.22f,.24f,.26f),.65f,.85f);wet=Mat("Wet cobble",new Color(.065f,.08f,.085f),.9f,.1f);cloth=Mat("Player cloak",new Color(.13f,.18f,.2f),.2f);
  var emberShader=AssetDatabase.LoadAssetAtPath<Shader>("Assets/DuskYard/Shaders/WorldEmber.shadergraph");
  if(emberShader==null)throw new System.Exception("WorldEmber shader did not import");
  var ember=new Material(emberShader);ember.SetColor("_BaseColor",new Color(5,1,.12f,1));AssetDatabase.CreateAsset(ember,"Assets/DuskYard/Materials/Ember warm.mat");
  var cold=new Material(emberShader);cold.SetColor("_BaseColor",new Color(.15f,1.2f,4,1));AssetDatabase.CreateAsset(cold,"Assets/DuskYard/Materials/Ember cold.mat");
  var ready=Mat("Target ready",new Color(.28f,.18f,.08f));var hit=Mat("Target impact",new Color(.65f,.22f,.045f));hit.SetColor("_EmissiveColor",new Color(2,.35f,.03f));
  var env=new GameObject("01 | Environment").transform;
  Box("Courtyard 28m",new Vector3(0,-.3f,0),new Vector3(28,.6f,28),stone,env);
  for(int i=-7;i<=7;i++)for(int j=-7;j<=7;j++){if((i*17+j*7)%4!=0)continue;Box("Wet paving",new Vector3(i*1.7f,.012f,j*1.7f),new Vector3(1.6f,.025f,1.6f),wet,env);}
  for(int side=-1;side<=1;side+=2){Box("Boundary wall",new Vector3(side*14,2,0),new Vector3(.8f,4,28),stone,env);Box("Rear parapet",new Vector3(side*9,1.5f,14),new Vector3(10,3,.8f),stone,env);}
  for(int x=-12;x<=12;x+=4){Box("Buttress",new Vector3(x,2.3f,13.3f),new Vector3(.8f,4.6f,1.4f),stone,env);}
  for(int i=0;i<5;i++){Vector3 p=new Vector3(i%2==0?-6:7,.65f,i*3-5);Box("Supply crate "+i,p,new Vector3(1.2f,1.3f,1.2f),wood,env);Box("Crate iron band",p,new Vector3(1.24f,.12f,1.24f),iron,env);}
  var player=new GameObject("02 | Player");player.layer=9;player.transform.position=new Vector3(0,0,-2);var cc=player.AddComponent<CharacterController>();cc.height=1.85f;cc.center=new Vector3(0,.95f,0);cc.radius=.32f;
  var body=Box("Cloaked test character",new Vector3(0,1,-2),new Vector3(.65f,.8f,.5f),cloth,player.transform,PrimitiveType.Capsule);Object.DestroyImmediate(body.GetComponent<Collider>());
  var head=Box("Helmet",new Vector3(0,1.85f,-2),Vector3.one*.4f,iron,player.transform,PrimitiveType.Sphere);Object.DestroyImmediate(head.GetComponent<Collider>());
  var pivot=new GameObject("Weapon pivot").transform;pivot.SetParent(player.transform);pivot.localPosition=new Vector3(.45f,1.2f,.3f);
  var sword=Box("Sword",Vector3.zero,new Vector3(.07f,.07f,1.3f),iron,pivot);sword.transform.localPosition=new Vector3(0,0,.55f);Object.DestroyImmediate(sword.GetComponent<Collider>());
  var trail=sword.AddComponent<TrailRenderer>();trail.time=.12f;trail.startWidth=.11f;trail.endWidth=0;trail.emitting=false;trail.minVertexDistance=.025f;trail.sharedMaterial=ember;
  var target=Box("03 | Training target",new Vector3(0,1.25f,.5f),new Vector3(.9f,1.4f,.5f),ready);target.layer=8;
  Box("Target post",new Vector3(0,.55f,.5f),new Vector3(.2f,1.1f,.2f),wood,env);
  var cam=new GameObject("04 | Gameplay camera");cam.tag="MainCamera";var camera=cam.AddComponent<Camera>();camera.nearClipPlane=.1f;camera.farClipPlane=120;cam.AddComponent<HDAdditionalCameraData>();cam.transform.position=new Vector3(0,3,-7);cam.transform.LookAt(new Vector3(0,1,0));
  var lights=new GameObject("05 | Lighting").transform;
  MakeLight("Dusk key",LightType.Directional,new Vector3(0,6,0),new Color(.48f,.59f,.8f),850,lights).transform.rotation=Quaternion.Euler(28,-35,0);
  MakeLight("Sky fill",LightType.Directional,new Vector3(0,6,0),new Color(.38f,.45f,.6f),90,lights).transform.rotation=Quaternion.Euler(65,145,0);
  MakeLight("Warm rim",LightType.Point,new Vector3(-3,3,3),new Color(1,.45f,.16f),1500,lights);
  var flash=MakeLight("Contact dynamic",LightType.Point,new Vector3(0,1,0),new Color(1,.65f,.25f),0,lights);flash.range=3;
  for(int i=-1;i<=1;i+=2){Box("Brazier stand",new Vector3(i*5,.7f,5),new Vector3(.2f,1.4f,.2f),iron,env);Box("Brazier bowl",new Vector3(i*5,1.4f,5),new Vector3(.8f,.15f,.8f),iron,env);MakeLight("Brazier warm pool",LightType.Point,new Vector3(i*5,1.7f,5),new Color(1,.35f,.08f),1800,lights);}
  var volume=new GameObject("Global Volume").AddComponent<Volume>();volume.isGlobal=true;var profile=ScriptableObject.CreateInstance<VolumeProfile>();AssetDatabase.CreateAsset(profile,"Assets/DuskYard/Settings/DuskVolume.asset");volume.sharedProfile=profile;
  var exposure=profile.Add<Exposure>();exposure.mode.Override(ExposureMode.Fixed);exposure.fixedExposure.Override(7);
  var tone=profile.Add<Tonemapping>();tone.mode.Override(TonemappingMode.ACES);
  var adjust=profile.Add<ColorAdjustments>();adjust.saturation.Override(-12);adjust.contrast.Override(8);
  var bloom=profile.Add<Bloom>();bloom.intensity.Override(.08f);
  var probe=new GameObject("Local baked reflection").AddComponent<ReflectionProbe>();probe.transform.position=new Vector3(0,2,2);probe.size=new Vector3(27,8,27);probe.mode=UnityEngine.Rendering.ReflectionProbeMode.Baked;probe.resolution=128;probe.boxProjection=true;
  var probes=new GameObject("Gameplay Light Probes").AddComponent<LightProbeGroup>();var pts=new System.Collections.Generic.List<Vector3>();for(int x=-8;x<=8;x+=4)for(int z=-8;z<=8;z+=4)foreach(float y in new[]{.4f,1.5f,3f})pts.Add(new Vector3(x,y,z));probes.probePositions=pts.ToArray();
  var fx=new GameObject("06 | Pooled contact sparks").AddComponent<ParticleSystem>();var main=fx.main;main.loop=false;main.playOnAwake=false;main.duration=.5f;main.startLifetime=new ParticleSystem.MinMaxCurve(.12f,.35f);main.startSpeed=new ParticleSystem.MinMaxCurve(1,4);main.startSize=new ParticleSystem.MinMaxCurve(.015f,.045f);main.startColor=new Color(1,.55f,.12f);main.maxParticles=128;main.simulationSpace=ParticleSystemSimulationSpace.World;var emission=fx.emission;emission.enabled=false;fx.GetComponent<ParticleSystemRenderer>().sharedMaterial=ember;fx.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
  var demo=new GameObject("07 | Combat and benchmark").AddComponent<CombatDemo>();demo.hero=player.transform;demo.target=target.transform;demo.targetRenderer=target.GetComponent<Renderer>();demo.blade=pivot;demo.cameraRig=cam.transform;demo.impactLight=flash;demo.sparks=fx;demo.trail=trail;demo.readyMaterial=ready;demo.hitMaterial=hit;
  PrefabUtility.SaveAsPrefabAsset(player,"Assets/DuskYard/Prefabs/Player.prefab");PrefabUtility.SaveAsPrefabAsset(target,"Assets/DuskYard/Prefabs/TrainingTarget.prefab");PrefabUtility.SaveAsPrefabAsset(fx.gameObject,"Assets/DuskYard/Prefabs/ContactSparks.prefab");
  QualitySettings.vSyncCount=0;EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(),"Assets/DuskYard/Scenes/DuskYard.unity");EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene("Assets/DuskYard/Scenes/DuskYard.unity",true)};AssetDatabase.SaveAssets();Debug.Log("DUSK_YARD_GENERATED");
 }
 static Light MakeLight(string n,LightType type,Vector3 p,Color c,float intensity,Transform parent){var g=new GameObject(n);g.transform.SetParent(parent);g.transform.position=p;var l=g.AddComponent<Light>();l.type=type;l.color=c;l.range=12;l.shadows=n=="Sky fill"?LightShadows.None:LightShadows.Soft;var hd=g.AddComponent<HDAdditionalLightData>();l.intensity=intensity;return l;}
 [MenuItem("Dusk Yard/Build Windows")]
 public static void Build(){var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/DuskYard/Scenes/DuskYard.unity"},locationPathName="Builds/Windows/DuskYard.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development});if(report.summary.result!=BuildResult.Succeeded)throw new System.Exception("Build failed: "+report.summary.result);}
}
