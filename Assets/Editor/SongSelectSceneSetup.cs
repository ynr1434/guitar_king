using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;

public static class SongSelectSceneSetup
{
    private const string SongSelectPath = "Assets/Scenes/SongSelect.unity";
    private const string RhythmGamePath = "Assets/RhythmGame.unity";
    private const string SampleScenePath = "Assets/Scenes/SampleScene.unity";

    [MenuItem("Tools/Guitar King/Set Up Song Select")]
    public static void Setup()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        GameObject root = new("SongSelect");
        root.AddComponent<SongSelectController>();
        GameObject eventSystem = new("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        SceneManager.MoveGameObjectToScene(eventSystem, scene);

        EditorSceneManager.SaveScene(scene, SongSelectPath);
        AddGameplayBackNavigation();
        ConfigureBuildScenes();
        EditorSceneManager.OpenScene(SongSelectPath, OpenSceneMode.Single);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("SongSelect scene created, build scenes configured, and RhythmGame navigation added.");
    }

    private static void AddGameplayBackNavigation()
    {
        Scene gameScene = EditorSceneManager.OpenScene(RhythmGamePath, OpenSceneMode.Single);
        GameObject navigation = GameObject.Find("GameplaySceneNavigation");
        if (navigation == null)
            navigation = new GameObject("GameplaySceneNavigation");
        if (navigation.GetComponent<GameplaySceneNavigation>() == null)
            navigation.AddComponent<GameplaySceneNavigation>();
        EditorSceneManager.MarkSceneDirty(gameScene);
        EditorSceneManager.SaveScene(gameScene, RhythmGamePath);
    }

    private static void ConfigureBuildScenes()
    {
        List<EditorBuildSettingsScene> scenes = new()
        {
            new EditorBuildSettingsScene(SongSelectPath, true),
            new EditorBuildSettingsScene(RhythmGamePath, true)
        };

        if (System.IO.File.Exists(SampleScenePath))
            scenes.Add(new EditorBuildSettingsScene(SampleScenePath, true));

        EditorBuildSettings.scenes = scenes.ToArray();
    }
}
