using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class DuskYardImportSetup {
 const string Root="Assets/DuskYard/";
 const string Report="Documentation/DuskYard-import-check.txt";
 const string Marker="ProjectSettings/DuskYardImported.txt";
 static double started;
 static DuskYardImportSetup(){started=EditorApplication.timeSinceStartup;EditorApplication.update+=Wait;}
 static void Wait(){
  if(File.Exists(Marker)){if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;RepairFill();EditorApplication.update-=Wait;return;}
  if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.timeSinceStartup-started<5)return;
  EditorApplication.update-=Wait;
  try {Configure();}catch(Exception e){Directory.CreateDirectory("Documentation");File.WriteAllText(Report,"IMPORT SETUP FAILED\n"+e);Debug.LogException(e);}
 }
 static void RepairFill(){
  if(EditorApplication.isPlayingOrWillChangePlaymode)return;
  var scene=SceneManager.GetActiveScene();if(scene.path!=Root+"Scenes/DuskYard.unity")return;
  var obj=GameObject.Find("Sky fill");if(obj==null)return;
  var light=obj.GetComponent<Light>();if(light==null||light.shadows==LightShadows.None)return;
  light.shadows=LightShadows.None;EditorUtility.SetDirty(light);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
  Debug.Log("DUSK_YARD_FILL_SHADOW_FIXED");
 }
 [MenuItem("Dusk Yard/Configure imported scene")]
 public static void Configure(){
  var pipeline=AssetDatabase.LoadAssetAtPath<HDRenderPipelineAsset>(Root+"Settings/DuskHDRP.asset");
  var global=AssetDatabase.LoadAssetAtPath<RenderPipelineGlobalSettings>(Root+"HDRPDefaultResources/HDRenderPipelineGlobalSettings.asset");
  if(pipeline==null||global==null)throw new Exception("Imported HDRP settings not found");
  EditorGraphicsSettings.SetRenderPipelineGlobalSettingsAsset<HDRenderPipeline>(global);
  GraphicsSettings.defaultRenderPipeline=pipeline;
  int previous=QualitySettings.GetQualityLevel();
  for(int i=0;i<QualitySettings.names.Length;i++){QualitySettings.SetQualityLevel(i,false);QualitySettings.renderPipeline=pipeline;QualitySettings.vSyncCount=0;}
  QualitySettings.SetQualityLevel(previous,false);
  PlayerSettings.colorSpace=ColorSpace.Linear;PlayerSettings.defaultScreenWidth=1920;PlayerSettings.defaultScreenHeight=1080;
  var tags=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
  tags.FindProperty("layers").GetArrayElementAtIndex(8).stringValue="Target";
  tags.FindProperty("layers").GetArrayElementAtIndex(9).stringValue="Player";tags.ApplyModifiedProperties();
  string scenePath=Root+"Scenes/DuskYard.unity";
  var list=new System.Collections.Generic.List<EditorBuildSettingsScene>{new EditorBuildSettingsScene(scenePath,true)};
  foreach(var s in EditorBuildSettings.scenes)if(s.path!=scenePath)list.Add(s);
  EditorBuildSettings.scenes=list.ToArray();AssetDatabase.SaveAssets();
  var active=SceneManager.GetActiveScene();
  bool opened=!active.isDirty;
  if(opened)EditorSceneManager.OpenScene(scenePath,OpenSceneMode.Single);RepairFill();
  var text=new StringBuilder();text.AppendLine("DUSK YARD IMPORT CONFIGURED");text.AppendLine("Unity: "+Application.unityVersion);text.AppendLine("HDRP: "+GraphicsSettings.defaultRenderPipeline.name);text.AppendLine("Scene opened: "+opened);text.AppendLine("Input: Input System with legacy fallback");
  int errors=0;
  foreach(string id in AssetDatabase.FindAssets("t:Material",new[]{Root})){
   string path=AssetDatabase.GUIDToAssetPath(id);var m=AssetDatabase.LoadAssetAtPath<Material>(path);
   if(m==null||m.shader==null){errors++;text.AppendLine("FAIL material: "+path);}
   else if(ShaderUtil.ShaderHasError(m.shader)){errors++;text.AppendLine("FAIL shader: "+path);}
  }
  if(opened){foreach(var r in SceneManager.GetActiveScene().GetRootGameObjects())foreach(var t in r.GetComponentsInChildren<Transform>(true))errors+=GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject);
   var demo=UnityEngine.Object.FindAnyObjectByType<DuskYard.CombatDemo>();
   if(demo==null||demo.hero==null||demo.target==null||demo.blade==null||demo.cameraRig==null||demo.impactLight==null||demo.sparks==null||demo.trail==null||demo.readyMaterial==null||demo.hitMaterial==null){errors++;text.AppendLine("FAIL combat dependency");}
  }
  text.AppendLine("Dependency/shader errors: "+errors);text.AppendLine("Visual and Play-mode validation still required.");
  Directory.CreateDirectory("Documentation");File.WriteAllText(Report,text.ToString());File.WriteAllText(Marker,DateTime.UtcNow.ToString("O"));
  Debug.Log("DUSK_YARD_IMPORT_CONFIGURED errors="+errors);
 }
}

