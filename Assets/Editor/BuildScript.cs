using UnityEditor;
using UnityEngine;

public static class BuildScript
{
    public static void PerformBuild()
    {
        string scenePath = "Assets/Scenes/SampleScene.unity";
        string buildPath = "Builds/Windows64";
        var scenes = new EditorBuildSettingsScene[]
        {
            new EditorBuildSettingsScene { path = scenePath, enabled = true },
        };
        var result = UnityEditor.BuildPipeline.BuildPlayer(scenes, buildPath, UnityEditor.BuildTarget.StandaloneWindows64, (UnityEditor.BuildOptions)0);
        Debug.Log("BUILD_RESULT=" + result.summary.result);
        Debug.Log("BUILD_SUMMARY=" + result.summary.totalSize);
    }
}
