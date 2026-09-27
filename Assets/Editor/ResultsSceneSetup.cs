using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ResultsSceneSetup
{
    private const string RhythmGamePath = "Assets/RhythmGame.unity";

    public static void Setup()
    {
        var scene = EditorSceneManager.OpenScene(RhythmGamePath, OpenSceneMode.Single);
        GameObject controllerObject = GameObject.Find("ResultsController");
        if (controllerObject == null)
            controllerObject = new GameObject("ResultsController");

        if (controllerObject.GetComponent<ResultsController>() == null)
            controllerObject.AddComponent<ResultsController>();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, RhythmGamePath);
        AssetDatabase.SaveAssets();
        Debug.Log("ResultsController added to RhythmGame.");
    }
}
