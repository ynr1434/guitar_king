using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class FriendsBuild
{
    private static readonly string[] Scenes = { "Assets/Scenes/SongSelect.unity", "Assets/RhythmGame.unity" };

    [MenuItem("Guitar King/Build/macOS for friends")]
    public static void Mac()
    {
        PlayerSettings.SetArchitecture(NamedBuildTarget.Standalone, 2); // Intel + Apple Silicon.
        Build(BuildTarget.StandaloneOSX, "Builds/macOS/Guitar King.app");
    }

    [MenuItem("Guitar King/Build/Windows for friends")]
    public static void Windows()
    {
        Build(BuildTarget.StandaloneWindows64, "Builds/Windows/Guitar King.exe");
    }

    private static void Build(BuildTarget target, string path)
    {
        if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone, target))
            throw new InvalidOperationException("Install Unity Build Support for " + target);
        PlayerSettings.productName = "Guitar King";
        PlayerSettings.bundleVersion = "0.1.2";
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = Scenes,
            target = target,
            locationPathName = path,
            options = BuildOptions.None
        });
        Debug.Log($"FRIENDS_BUILD {target}: {report.summary.result}; errors={report.summary.totalErrors}; bytes={report.summary.totalSize}");
        if (report.summary.result != BuildResult.Succeeded)
            throw new InvalidOperationException("Build failed: " + report.summary.result);
    }
}
