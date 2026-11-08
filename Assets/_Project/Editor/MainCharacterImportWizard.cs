#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

public static class MainCharacterImportWizard
{
    private const string OutputFolder = "Assets/_Project/Resources";
    private const string OutputPath = OutputFolder + "/PlayerCharacter.prefab";

    [MenuItem("Eclipse Frontier/Use Selected Model as Main Character")]
    private static void ImportSelectedModel()
    {
        GameObject selected = Selection.activeObject as GameObject;
        if (selected == null)
        {
            EditorUtility.DisplayDialog("Main Character Import", "Select an imported FBX/model asset in the Project window first.", "OK");
            return;
        }

        string sourcePath = AssetDatabase.GetAssetPath(selected);
        if (string.IsNullOrEmpty(sourcePath) || (!PrefabUtility.IsPartOfModelPrefab(selected) && !PrefabUtility.IsPartOfPrefabAsset(selected)))
        {
            EditorUtility.DisplayDialog("Main Character Import", "The selection must be an imported model asset or prefab. For Sketchfab, FBX is the recommended download format.", "OK");
            return;
        }

        if (!AssetDatabase.IsValidFolder(OutputFolder))
            Directory.CreateDirectory(OutputFolder);

        GameObject instance = PrefabUtility.InstantiatePrefab(selected) as GameObject;
        if (instance == null)
        {
            EditorUtility.DisplayDialog("Main Character Import", "Unity could not instantiate the selected model asset.", "OK");
            return;
        }

        instance.name = "PlayerCharacter";
        GameObject saved = PrefabUtility.SaveAsPrefabAsset(instance, OutputPath);
        Object.DestroyImmediate(instance);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        if (saved != null)
        {
            Selection.activeObject = saved;
            EditorUtility.DisplayDialog("Main Character Import", "Saved as PlayerCharacter. The game loads it automatically on the next Play.", "OK");
        }
        else
        {
            EditorUtility.DisplayDialog("Main Character Import", "Unity could not save the PlayerCharacter prefab.", "OK");
        }
    }

    [MenuItem("Eclipse Frontier/Use Selected Model as Main Character", true)]
    private static bool ValidateImportSelectedModel() => Selection.activeObject is GameObject;
}
#endif
