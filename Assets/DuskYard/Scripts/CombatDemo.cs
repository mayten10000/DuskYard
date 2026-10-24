using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace DuskYard {
public sealed class CombatDemo : MonoBehaviour {
 public Transform hero, target, blade, cameraRig;
 public Renderer targetRenderer;
 public Light impactLight;
 public ParticleSystem sparks;
 public TrailRenderer trail;
 public Material readyMaterial, hitMaterial;
 CharacterController controller; float yaw, pitch=18, attack=-1, cooldown, recovery, stressTimer; bool connected, stress, automatic; Vector3 bladeRest;
 readonly RaycastHit[] hits=new RaycastHit[16];
 float[] samples=new float[1800]; int count; float elapsed; string mode="Gameplay";
 void Start(){ controller=hero.GetComponent<CharacterController>(); bladeRest=blade.localPosition; QualitySettings.vSyncCount=0; Application.targetFrameRate=-1; Cursor.lockState=CursorLockMode.Locked; automatic=System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-autodemo")>=0; }
 void Update(){
  float dt=Time.deltaTime; cooldown-=dt;
  if(DemoInput.GetKeyDown(KeyCode.Escape)){Cursor.lockState=CursorLockMode.None;Cursor.visible=true;}
  if(DemoInput.GetMouseButtonDown(1)){Cursor.lockState=CursorLockMode.Locked;Cursor.visible=false;}
  if(Cursor.lockState==CursorLockMode.Locked){yaw+=DemoInput.GetAxis("Mouse X")*2;pitch=Mathf.Clamp(pitch-DemoInput.GetAxis("Mouse Y")*2,5,45);}
  Vector3 move=Quaternion.Euler(0,yaw,0)*new Vector3(DemoInput.GetAxisRaw("Horizontal"),0,DemoInput.GetAxisRaw("Vertical"));
  if(move.sqrMagnitude>1)move.Normalize(); controller.Move((move*4+Vector3.down*9)*dt);
  if(move.sqrMagnitude>.01f) hero.rotation=Quaternion.Slerp(hero.rotation,Quaternion.LookRotation(move),dt*12);
  if(DemoInput.GetKeyDown(KeyCode.F2)){stress=!stress;SetMode(stress?"Stress":"Gameplay");}
  if(DemoInput.GetKeyDown(KeyCode.F1))SetMode(mode=="Baseline"?"Gameplay":"Baseline");
  if(DemoInput.GetKeyDown(KeyCode.F3))Export();
  if(automatic){hero.rotation=Quaternion.LookRotation(new Vector3(target.position.x-hero.position.x,0,target.position.z-hero.position.z));}
  stressTimer-=dt;
  if(mode!="Baseline" && (DemoInput.GetMouseButtonDown(0)||DemoInput.GetKeyDown(KeyCode.Space)||((stress||automatic)&&stressTimer<=0))&&cooldown<=0){attack=0;cooldown=.72f;connected=false;trail.emitting=true;stressTimer=stress?.75f:1.5f;}
  if(attack>=0){attack+=dt; float t=Mathf.Clamp01(attack/.42f);blade.localPosition=bladeRest;blade.localRotation=Quaternion.Euler(0,Mathf.Lerp(-75,85,t),Mathf.Lerp(25,-35,t));
   if(!connected&&attack>=.2f){connected=true;Contact();}
   if(attack>=.42f){attack=-1;trail.emitting=false;blade.localRotation=Quaternion.identity;}}
  recovery=Mathf.Max(0,recovery-dt); targetRenderer.sharedMaterial=recovery>0?hitMaterial:readyMaterial;
  target.localRotation=Quaternion.Euler(Mathf.Sin(recovery*24)*recovery*8,0,0);
  impactLight.intensity=Mathf.MoveTowards(impactLight.intensity,0,dt*8000);
  if(elapsed>3&&count<samples.Length)samples[count++]=dt*1000; elapsed+=dt;
 }
 void LateUpdate(){cameraRig.rotation=Quaternion.Euler(pitch,yaw,0);Vector3 focus=hero.position+Vector3.up*1.5f;Vector3 desired=focus-cameraRig.forward*5+cameraRig.right*.65f; if(Physics.Linecast(focus,desired,out RaycastHit h,1<<0))desired=h.point+ h.normal*.2f;cameraRig.position=desired;}
 void Contact(){Vector3 origin=hero.position+Vector3.up*1.2f; int n=Physics.SphereCastNonAlloc(origin,.65f,hero.forward,hits,2.5f,1<<8,QueryTriggerInteraction.Ignore); if(n==0)return;
  RaycastHit nearest=hits[0];for(int i=1;i<n;i++)if(hits[i].distance<nearest.distance)nearest=hits[i];
  Vector3 point=nearest.point; sparks.transform.position=point;sparks.transform.rotation=Quaternion.LookRotation(nearest.normal);sparks.Emit(stress?40:14);impactLight.transform.position=point+nearest.normal*.2f;impactLight.intensity=1300;recovery=.55f;
 }
 void SetMode(string value){mode=value;count=0;elapsed=0;}
 void Export(){if(count==0)return;float[] sorted=new float[count];System.Array.Copy(samples,sorted,count);System.Array.Sort(sorted);string file=System.IO.Path.Combine(Application.persistentDataPath,"benchmark-"+mode+".csv");System.IO.File.WriteAllText(file,"Mode,Frames,MedianFPS,P95ms,CPU,GPU,Resolution\n"+mode+","+count+","+(1000/sorted[count/2]).ToString(System.Globalization.CultureInfo.InvariantCulture)+","+sorted[Mathf.Min(count-1,(int)(count*.95f))].ToString(System.Globalization.CultureInfo.InvariantCulture)+",\""+SystemInfo.processorType+"\",\""+SystemInfo.graphicsDeviceName+"\","+Screen.width+"x"+Screen.height);Debug.Log("Benchmark exported: "+file);}
 void OnGUI(){GUI.color=new Color(.9f,.86f,.72f);GUI.Label(new Rect(24,20,700,30),"DUSK YARD  /  TECHNICAL COMBAT STUDY");GUI.Label(new Rect(24,48,900,26),"WASD move  |  Mouse camera  |  LMB / Space strike  |  F1 baseline  F2 stress  F3 export  Esc cursor");GUI.Label(new Rect(24,75,700,26),mode+"  |  Target: "+(recovery>0?"HIT / RECOVERING":"READY")+"  |  Samples: "+count);}
}
}
