using UnityEditor;
using UnityEngine;

public static class AudioLibraryAssetBootstrap
{
    private const string AssetFolderPath = "Assets/Resources";
    private const string AssetPath = AssetFolderPath + "/AudioLibrary.asset";

    [InitializeOnLoadMethod]
    private static void EnsureAssetExists()
    {
        AudioLibrary existingAsset = AssetDatabase.LoadAssetAtPath<AudioLibrary>(AssetPath);
        if (existingAsset != null)
            return;

        if (!AssetDatabase.IsValidFolder(AssetFolderPath))
            AssetDatabase.CreateFolder("Assets", "Resources");

        AudioLibrary audioLibrary = ScriptableObject.CreateInstance<AudioLibrary>();
        AssetDatabase.CreateAsset(audioLibrary, AssetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("Created AudioLibrary asset at Assets/Resources/AudioLibrary.asset");
    }
}
