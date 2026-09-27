using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

public static class RhythmGameSceneBuilder
{
    private const string ScenePath = "Assets/RhythmGame.unity";
    private const string MaterialFolder = "Assets/Materials";

    private static readonly string[] LaneNames =
    {
        "LaneGreen", "LaneRed", "LaneYellow", "LaneBlue", "LaneOrange"
    };

    private static readonly string[] TargetNames =
    {
        "GreenTarget", "RedTarget", "YellowTarget", "BlueTarget", "OrangeTarget"
    };

    private static readonly Color[] Colors =
    {
        new(0.12f, 0.95f, 0.25f),
        new(1.00f, 0.12f, 0.12f),
        new(1.00f, 0.85f, 0.08f),
        new(0.12f, 0.42f, 1.00f),
        new(1.00f, 0.38f, 0.05f)
    };

    [MenuItem("Tools/Guitar King/Build RhythmGame Stage 1")]
    public static void Build()
    {
        Directory.CreateDirectory(MaterialFolder);
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        foreach (GameObject root in scene.GetRootGameObjects())
            Object.DestroyImmediate(root);

        RenderSettings.skybox = null;
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.09f, 0.09f, 0.12f);

        CreateCamera();
        CreateLight();
        new GameObject("GameManager");

        GameObject highway = new("Highway");
        Material roadMaterial = CreateMaterial("Highway", new Color(0.025f, 0.028f, 0.04f), false);
        Material dividerMaterial = CreateMaterial("LaneDivider", new Color(0.24f, 0.26f, 0.32f), true);
        Material hitLineMaterial = CreateMaterial("HitLine", new Color(0.75f, 0.8f, 0.9f), true);

        CreatePrimitive(PrimitiveType.Cube, "RoadBase", highway.transform,
            new Vector3(0f, -0.08f, 7f), new Vector3(5.65f, 0.12f, 19f), roadMaterial);

        Transform[] targets = new Transform[5];
        Renderer[] targetRenderers = new Renderer[5];

        for (int i = 0; i < 5; i++)
        {
            float x = (i - 2) * 1.08f;
            Color laneColor = Color.Lerp(new Color(0.035f, 0.035f, 0.05f), Colors[i], 0.24f);
            Material laneMaterial = CreateMaterial(LaneNames[i], laneColor, false);
            CreatePrimitive(PrimitiveType.Cube, LaneNames[i], highway.transform,
                new Vector3(x, 0f, 7f), new Vector3(1.02f, 0.08f, 18.8f), laneMaterial);
        }

        for (int i = 0; i < 6; i++)
        {
            float x = -2.7f + i * 1.08f;
            CreatePrimitive(PrimitiveType.Cube, $"LaneDivider{i + 1}", highway.transform,
                new Vector3(x, 0.08f, 7f), new Vector3(0.035f, 0.035f, 18.8f), dividerMaterial);
        }

        GameObject hitLine = new("HitLine");
        hitLine.transform.SetParent(highway.transform, false);
        CreatePrimitive(PrimitiveType.Cube, "HitLineBar", hitLine.transform,
            new Vector3(0f, 0.14f, -1.55f), new Vector3(5.8f, 0.07f, 0.16f), hitLineMaterial);

        for (int i = 0; i < 5; i++)
        {
            float x = (i - 2) * 1.08f;
            Material targetMaterial = CreateMaterial(TargetNames[i], Colors[i], true);
            GameObject target = CreatePrimitive(PrimitiveType.Cylinder, TargetNames[i], hitLine.transform,
                new Vector3(x, 0.24f, -1.55f), new Vector3(0.43f, 0.09f, 0.43f), targetMaterial);
            targets[i] = target.transform;
            targetRenderers[i] = target.GetComponent<Renderer>();
        }

        GameObject inputManager = new("InputManager");
        RhythmInputController controller = inputManager.AddComponent<RhythmInputController>();
        AssignControllerBindings(controller, targets, targetRenderers);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        Debug.Log("RhythmGame Stage 1 scene built and saved successfully.");
    }

    private static void CreateCamera()
    {
        GameObject cameraObject = new("MainCamera");
        cameraObject.tag = "MainCamera";
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.008f, 0.009f, 0.015f);
        camera.fieldOfView = 54f;
        camera.nearClipPlane = 0.1f;
        camera.farClipPlane = 100f;
        cameraObject.AddComponent<AudioListener>();
        cameraObject.transform.position = new Vector3(0f, 7.5f, -8.8f);
        cameraObject.transform.rotation = Quaternion.LookRotation(new Vector3(0f, 0.2f, 7.0f) - cameraObject.transform.position);
    }

    private static void CreateLight()
    {
        GameObject lightObject = new("Directional Light");
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.color = new Color(0.78f, 0.84f, 1f);
        light.intensity = 1.35f;
        light.shadows = LightShadows.Soft;
        lightObject.transform.rotation = Quaternion.Euler(48f, -25f, 0f);
    }

    private static GameObject CreatePrimitive(PrimitiveType type, string name, Transform parent,
        Vector3 position, Vector3 scale, Material material)
    {
        GameObject item = GameObject.CreatePrimitive(type);
        item.name = name;
        item.transform.SetParent(parent, false);
        item.transform.localPosition = position;
        item.transform.localScale = scale;
        item.GetComponent<Renderer>().sharedMaterial = material;
        Object.DestroyImmediate(item.GetComponent<Collider>());
        return item;
    }

    private static Material CreateMaterial(string name, Color color, bool emission)
    {
        string path = $"{MaterialFolder}/{name}.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (material == null)
        {
            material = new Material(shader) { name = name };
            AssetDatabase.CreateAsset(material, path);
        }

        material.shader = shader;
        material.SetColor("_BaseColor", color);
        material.SetFloat("_Smoothness", 0.35f);
        if (emission)
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color * 0.45f);
        }
        else
        {
            material.DisableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", Color.black);
        }

        EditorUtility.SetDirty(material);
        return material;
    }

    private static void AssignControllerBindings(RhythmInputController controller, Transform[] targets,
        Renderer[] renderers)
    {
        SerializedObject serializedController = new(controller);
        SerializedProperty bindings = serializedController.FindProperty("targets");
        bindings.arraySize = 5;
        for (int i = 0; i < 5; i++)
        {
            SerializedProperty binding = bindings.GetArrayElementAtIndex(i);
            binding.FindPropertyRelative("target").objectReferenceValue = targets[i];
            binding.FindPropertyRelative("targetRenderer").objectReferenceValue = renderers[i];
            binding.FindPropertyRelative("baseColor").colorValue = Colors[i];
        }

        serializedController.ApplyModifiedPropertiesWithoutUndo();
    }
}
