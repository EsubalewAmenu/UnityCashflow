using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

// Run for both ordinary Build & Run and the local phone build command.
public sealed class BoardMaterialAssets : IPreprocessBuildWithReport
{
    public int callbackOrder => -100;

    [InitializeOnLoadMethod]
    static void OnLoad() { EditorApplication.delayCall += EnsureMaterial; }

    public void OnPreprocessBuild(BuildReport report) { EnsureMaterial(); }

    static void EnsureMaterial()
    {
        const string path = "Assets/Resources/BoardSurface.mat";
        if (AssetDatabase.LoadAssetAtPath<Material>(path) != null) return;
        var shader = Shader.Find("Standard");
        if (shader == null) throw new BuildFailedException("The built-in Standard shader is unavailable.");
        AssetDatabase.CreateAsset(new Material(shader) { name = "BoardSurface" }, path);
        AssetDatabase.SaveAssets();
    }
}
