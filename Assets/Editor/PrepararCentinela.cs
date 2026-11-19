using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System.IO;

public static class PrepararCentinela
{
    [MenuItem("Centinela/Preparar escenas")]
    public static void Preparar()
    {
        Directory.CreateDirectory("Assets/Escenas");
        var scenes = new EditorBuildSettingsScene[4];
        for (int i = 0; i < scenes.Length; i++)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            new GameObject("Operacion Centinela").AddComponent<Centinela>().nivel = i;
            string path = "Assets/Escenas/" + Centinela.Escenas[i] + ".unity";
            EditorSceneManager.SaveScene(scene, path);
            scenes[i] = new EditorBuildSettingsScene(path, true);
        }
        EditorBuildSettings.scenes = scenes;
        EditorSceneManager.OpenScene(scenes[0].path);
        AssetDatabase.SaveAssets();
    }

    public static void Compilar()
    {
        Preparar();
        Directory.CreateDirectory("Build");
        var result = BuildPipeline.BuildPlayer(EditorBuildSettings.scenes, "Build/OperacionCentinela.exe",
            BuildTarget.StandaloneWindows64, BuildOptions.None);
        EditorApplication.Exit(result.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded ? 0 : 1);
    }
}
