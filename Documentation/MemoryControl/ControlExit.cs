using System;
using UnityEngine;
using Unity.Profiling;

public sealed class ControlExit : MonoBehaviour
{
    ProfilerRecorder recorder;
    CharacterController mover;
    ParticleSystem[] particles;
    float started, duration=15;
    void Start()
    {
        var args=Environment.GetCommandLineArgs();
        if(Array.IndexOf(args,"-long-control")>=0) duration=60;
        if(Array.IndexOf(args,"-record-control")>=0)
            recorder=ProfilerRecorder.StartNew(ProfilerCategory.Memory,"GC Allocated In Frame",1);
        if(Array.IndexOf(args,"-controller-control")>=0) mover=gameObject.AddComponent<CharacterController>();
        if(Array.IndexOf(args,"-particles-control")>=0)
        {
            particles=new ParticleSystem[3];
            for(int i=0;i<particles.Length;i++)
            {
                particles[i]=new GameObject("Control particles "+i).AddComponent<ParticleSystem>();
                var settings=particles[i].main;settings.maxParticles=256;settings.startLifetime=.1f;
                particles[i].GetComponent<ParticleSystemRenderer>().enabled=false;
            }
        }
        Application.runInBackground=true;QualitySettings.vSyncCount=0;Application.targetFrameRate=60;
#if ENABLE_INPUT_SYSTEM
        Debug.Log("INPUT_CONTROL devices="+UnityEngine.InputSystem.InputSystem.devices.Count);
#endif
        started=Time.realtimeSinceStartup;
    }
    void OnDestroy() { if(recorder.Valid)recorder.Dispose(); }
    void Update()
    {
        if(particles!=null)for(int i=0;i<particles.Length;i++)particles[i].Emit(1);
        if(mover!=null)mover.Move(new Vector3(Mathf.Sin(Time.time),0,Mathf.Cos(Time.time))*.01f);
        if(Time.realtimeSinceStartup-started>=duration)
        {
            Debug.Log("EMPTY_CONTROL_COMPLETE unity="+Application.unityVersion+" seconds="+(Time.realtimeSinceStartup-started));
            Application.Quit(0);
        }
    }
}
