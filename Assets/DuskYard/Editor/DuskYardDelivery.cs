using System;
using System.IO;
using UnityEditor;
using UnityEditor.Rendering.HighDefinition;
using UnityEngine;

public static class DuskYardDelivery
{
    [MenuItem("Dusk Yard/Build verified delivery")]
    public static void Build()
    {
        string report="HDRP transparent VFX material validation\n";
        foreach(string name in new[]{"Ember warm","Ember cold","Blade motion warm","Blade motion cold","Brazier small flame"})
        {
            var material=AssetDatabase.LoadAssetAtPath<Material>("Assets/DuskYard/Materials/"+name+".mat");
            if(material==null)throw new Exception("VFX material missing: "+name);
            material.SetFloat("_SurfaceType",1);material.SetFloat("_TransparentZWrite",0);material.SetFloat("_ZWrite",0);
            material.SetFloat("_DoubleSidedEnable",1);material.SetFloat("_TransparentCullMode",0);material.SetFloat("_CullMode",0);material.SetFloat("_CullModeForward",0);
            material.renderQueue=3000;
            if(!HDShaderUtils.ResetMaterialKeywords(material))throw new Exception("HDRP material setup failed: "+name);
            if(material.GetFloat("_SurfaceType")!=1||material.GetFloat("_ZWrite")!=0||material.renderQueue<3000)throw new Exception("Transparent state not retained: "+name);
            EditorUtility.SetDirty(material);
            report+=name+": SurfaceType="+material.GetFloat("_SurfaceType")+", ZWrite="+material.GetFloat("_ZWrite")+", Queue="+material.renderQueue+"\n";
        }
        AssetDatabase.SaveAssets();File.WriteAllText("Documentation/Verification/vfx-materials.txt",report);
        DuskYardFinalize.Build();
    }
}
