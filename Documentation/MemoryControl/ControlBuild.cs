using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using System.IO;
public static class ControlBuild
{
    public static void RunBuiltinPopulated()
    {
        GraphicsSettings.defaultRenderPipeline=null;
        for(int i=0;i<QualitySettings.names.Length;i++){QualitySettings.SetQualityLevel(i,false);QualitySettings.renderPipeline=null;}
        Run();
    }
    public static void Run()
    {
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        new GameObject("Exit after 15 seconds").AddComponent<ControlExit>();
        var camera=new GameObject("Camera").AddComponent<Camera>();camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.gray;
        if(System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-populated-control")>=0)
        {
            Material material=null;
            if(GraphicsSettings.defaultRenderPipeline!=null){material=AssetDatabase.LoadAssetAtPath<Material>("Assets/ControlLit.mat");if(material==null){material=new Material(Shader.Find("HDRP/Lit"));AssetDatabase.CreateAsset(material,"Assets/ControlLit.mat");}}
            for(int i=0;i<300;i++){var cube=GameObject.CreatePrimitive(PrimitiveType.Cube);cube.name="Static control cube "+i;cube.transform.position=new Vector3(i%20-10,0,i/20+10);if(material!=null)cube.GetComponent<Renderer>().sharedMaterial=material;}
            var light=new GameObject("Control directional light").AddComponent<Light>();light.type=LightType.Directional;light.intensity=1;
            var extra=System.Type.GetType("UnityEngine.Rendering.HighDefinition.HDAdditionalLightData, Unity.RenderPipelines.HighDefinition.Runtime");if(extra!=null)light.gameObject.AddComponent(extra);
        }
        EditorSceneManager.SaveScene(scene,"Assets/Control.unity");
        PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64,new[]{GraphicsDeviceType.Direct3D11});
        PlayerSettings.colorSpace=ColorSpace.Linear;
        PlayerSettings.defaultScreenWidth=1280;PlayerSettings.defaultScreenHeight=720;
        var report=BuildPipeline.BuildPlayer(new[]{"Assets/Control.unity"},"Build/Control.exe",BuildTarget.StandaloneWindows64,BuildOptions.Development);
        if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)throw new System.Exception("Control build failed");
    }
}
