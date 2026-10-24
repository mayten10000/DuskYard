using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
[InitializeOnLoad]
public static class DuskYardMigrationClose {
 static DuskYardMigrationClose(){EditorApplication.update+=Wait;}
 static void Wait(){
  if(!File.Exists("Documentation/CloseForMigration.request")||EditorApplication.isCompiling||EditorApplication.isUpdating)return;
  if(EditorApplication.isPlayingOrWillChangePlaymode){EditorApplication.isPlaying=false;return;}
  EditorApplication.update-=Wait;
  if(!EditorSceneManager.SaveOpenScenes())throw new System.Exception("Cannot save scenes for migration");
  AssetDatabase.SaveAssets();File.WriteAllText("Documentation/ClosedForMigration.txt","Saved and closed for folder migration.");File.Delete("Documentation/CloseForMigration.request");EditorApplication.Exit(0);
 }
}
