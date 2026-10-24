using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using DuskYard;
[InitializeOnLoad]
public static class DuskYardSmokeCheck {
 const string Key="DuskYardSmokeStage";
 const string Result="Documentation/DuskYard-play-check.txt";
 static double next;
 static DuskYardSmokeCheck(){EditorApplication.update+=Tick;Application.logMessageReceived+=Log;}
 static void Log(string text,string stack,LogType type){if(SessionState.GetInt(Key,0)==1&&(type==LogType.Error||type==LogType.Exception))SessionState.SetInt("DuskYardSmokeErrors",SessionState.GetInt("DuskYardSmokeErrors",0)+1);}
 static void Tick(){
  if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
  int stage=SessionState.GetInt(Key,0);
  if(stage==0){if(File.Exists(Result)||SceneManager.GetActiveScene().path!="Assets/DuskYard/Scenes/DuskYard.unity")return;
   SessionState.SetInt(Key,1);SessionState.SetInt("DuskYardSmokeErrors",0);SessionState.SetFloat("DuskYardSmokeStart",0);EditorApplication.isPlaying=true;return;}
  if(stage!=1||!EditorApplication.isPlaying)return;
  float start=SessionState.GetFloat("DuskYardSmokeStart",0);
  if(start==0){SessionState.SetFloat("DuskYardSmokeStart",(float)EditorApplication.timeSinceStartup);return;}
  var demo=Object.FindAnyObjectByType<CombatDemo>();
  if(demo!=null&&EditorApplication.timeSinceStartup>next){demo.SendMessage("Contact",SendMessageOptions.RequireReceiver);next=EditorApplication.timeSinceStartup+1;}
  if(EditorApplication.timeSinceStartup-start<10)return;
  int errors=SessionState.GetInt("DuskYardSmokeErrors",0);
  string text="PLAY MODE SMOKE CHECK\nUnity: "+Application.unityVersion+"\nDuration: 10 seconds\nRuntime errors observed: "+errors+"\nCombat component: "+(demo!=null)+"\nCamera: "+(Camera.main!=null)+"\n";
  if(demo!=null)text+="Particle cap: "+demo.sparks.main.maxParticles+"\nCurrent particles: "+demo.sparks.particleCount+"\nHero height: "+demo.hero.position.y+"\n";
  text+="Input bindings and visual quality require manual review. Contact was invoked directly for smoke validation.\n";
  Directory.CreateDirectory("Documentation");File.WriteAllText(Result,text);Debug.Log("DUSK_YARD_PLAY_CHECK errors="+errors);SessionState.SetInt(Key,2);EditorApplication.isPlaying=false;
 }
}
