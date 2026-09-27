using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class RhythmGameStage2Setup
{
    private const string ScenePath = "Assets/RhythmGame.unity";
    private const string PrefabFolder = "Assets/Prefabs";
    private const string PrefabPath = PrefabFolder + "/Note.prefab";
    private const string GreenMaterialPath = "Assets/Materials/GreenTarget.mat";

    private static readonly string[] LaneNames =
    {
        "Green", "Red", "Yellow", "Blue", "Orange"
    };

    private static readonly Color[] LaneColors =
    {
        new(0.12f, 0.95f, 0.25f),
        new(1.00f, 0.12f, 0.12f),
        new(1.00f, 0.85f, 0.08f),
        new(0.12f, 0.42f, 1.00f),
        new(1.00f, 0.38f, 0.05f)
    };

    [MenuItem("Tools/Guitar King/Set Up Stage 2 Test Note")]
    public static void Setup()
    {
        Directory.CreateDirectory(PrefabFolder);
        GameObject notePrefab = CreateOrUpdateNotePrefab();

        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        SongClock songClock = GetOrCreateComponent<SongClock>("SongClock");
        SongAudioController songAudio = GetOrCreateSongAudio();
        SerializedObject serializedClock = new(songClock);
        serializedClock.FindProperty("songAudio").objectReferenceValue = songAudio;
        serializedClock.ApplyModifiedPropertiesWithoutUndo();

        BeatGrid beatGrid = GetOrCreateComponent<BeatGrid>("BeatGrid");
        SerializedObject serializedBeatGrid = new(beatGrid);
        serializedBeatGrid.FindProperty("songClock").objectReferenceValue = songClock;
        serializedBeatGrid.FindProperty("songSettings").objectReferenceValue = songAudio;
        serializedBeatGrid.ApplyModifiedPropertiesWithoutUndo();

        ChartRecorder chartRecorder = GetOrCreateComponent<ChartRecorder>("ChartRecorder");
        SerializedObject serializedChartRecorder = new(chartRecorder);
        serializedChartRecorder.FindProperty("recording").boolValue = false;
        serializedChartRecorder.FindProperty("snapToBeat").boolValue = false;
        serializedChartRecorder.FindProperty("snapSubdivision").intValue = 4;
        serializedChartRecorder.FindProperty("songClock").objectReferenceValue = songClock;
        serializedChartRecorder.FindProperty("beatGrid").objectReferenceValue = beatGrid;
        serializedChartRecorder.ApplyModifiedPropertiesWithoutUndo();

        RhythmInputController inputController = GameObject.Find("InputManager").GetComponent<RhythmInputController>();
        HitFeedbackUI feedbackUI = GetOrCreateFeedbackUI();
        ScoreManager scoreManager = GetOrCreateComponent<ScoreManager>("ScoreManager");
        GetOrCreateScoreUI(scoreManager);
        GetOrCreateBeatMetronome(beatGrid);
        GetOrCreateChartRecorderUI(chartRecorder);
        HitDetector hitDetector = GetOrCreateComponent<HitDetector>("HitDetector");
        SerializedObject serializedHitDetector = new(hitDetector);
        serializedHitDetector.FindProperty("perfectWindowMs").doubleValue = 45.0;
        serializedHitDetector.FindProperty("greatWindowMs").doubleValue = 90.0;
        serializedHitDetector.FindProperty("goodWindowMs").doubleValue = 140.0;
        serializedHitDetector.FindProperty("missWindowMs").doubleValue = 160.0;
        serializedHitDetector.FindProperty("songClock").objectReferenceValue = songClock;
        serializedHitDetector.FindProperty("inputController").objectReferenceValue = inputController;
        serializedHitDetector.FindProperty("feedbackUI").objectReferenceValue = feedbackUI;
        serializedHitDetector.FindProperty("scoreManager").objectReferenceValue = scoreManager;
        serializedHitDetector.ApplyModifiedPropertiesWithoutUndo();

        Transform highway = GameObject.Find("Highway").transform;
        Transform hitLine = GameObject.Find("HitLine").transform;
        Transform[] spawnPoints = new Transform[5];
        Transform[] hitPoints = new Transform[5];

        for (int i = 0; i < 5; i++)
        {
            float x = (i - 2) * 1.08f;
            spawnPoints[i] = GetOrCreatePoint($"{LaneNames[i]}SpawnPoint", highway, new Vector3(x, 0.24f, 15f));
            hitPoints[i] = GetOrCreatePoint($"{LaneNames[i]}HitPoint", hitLine, new Vector3(x, 0.24f, -1.55f));
        }

        GameObject oldSpawnerObject = GameObject.Find("TestNoteSpawner");
        if (oldSpawnerObject != null)
            Object.DestroyImmediate(oldSpawnerObject);

        ChartPlayer chartPlayer = GetOrCreateComponent<ChartPlayer>("ChartPlayer");
        SerializedObject serializedPlayer = new(chartPlayer);
        serializedPlayer.FindProperty("notePrefab").objectReferenceValue = notePrefab;
        serializedPlayer.FindProperty("songClock").objectReferenceValue = songClock;
        serializedPlayer.FindProperty("hitDetector").objectReferenceValue = hitDetector;
        serializedPlayer.FindProperty("chartRecorder").objectReferenceValue = chartRecorder;
        SerializedProperty lanes = serializedPlayer.FindProperty("lanes");
        lanes.arraySize = 5;
        for (int i = 0; i < 5; i++)
        {
            SerializedProperty lane = lanes.GetArrayElementAtIndex(i);
            lane.FindPropertyRelative("lane").enumValueIndex = i;
            lane.FindPropertyRelative("spawnPoint").objectReferenceValue = spawnPoints[i];
            lane.FindPropertyRelative("hitPoint").objectReferenceValue = hitPoints[i];
            lane.FindPropertyRelative("color").colorValue = LaneColors[i];
        }
        serializedPlayer.ApplyModifiedPropertiesWithoutUndo();

        SongPackageLoader packageLoader = GetOrCreateComponent<SongPackageLoader>("SongPackageLoader");
        SerializedObject serializedLoader = new(packageLoader);
        serializedLoader.FindProperty("songFolderName").stringValue = "TestSong";
        serializedLoader.FindProperty("songAudioController").objectReferenceValue = songAudio;
        serializedLoader.FindProperty("chartPlayer").objectReferenceValue = chartPlayer;
        serializedLoader.ApplyModifiedPropertiesWithoutUndo();

        serializedChartRecorder.Update();
        serializedChartRecorder.FindProperty("songPackageLoader").objectReferenceValue = packageLoader;
        serializedChartRecorder.ApplyModifiedPropertiesWithoutUndo();
        GetOrCreateSongPackageUI(packageLoader);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        Debug.Log("Song package loading, chart playback, and recording configured successfully.");
    }

    private static GameObject CreateOrUpdateNotePrefab()
    {
        GameObject note = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        note.name = "Note";
        note.transform.localScale = new Vector3(0.34f, 0.08f, 0.34f);

        Object.DestroyImmediate(note.GetComponent<Collider>());
        Material greenMaterial = AssetDatabase.LoadAssetAtPath<Material>(GreenMaterialPath);
        note.GetComponent<Renderer>().sharedMaterial = greenMaterial;

        NoteController controller = note.AddComponent<NoteController>();
        SerializedObject serializedController = new(controller);
        serializedController.FindProperty("hitTime").doubleValue = 3.0;
        serializedController.FindProperty("travelTime").doubleValue = 2.0;
        serializedController.FindProperty("postHitTravel").floatValue = 0.35f;
        serializedController.ApplyModifiedPropertiesWithoutUndo();

        PrefabUtility.SaveAsPrefabAsset(note, PrefabPath);
        Object.DestroyImmediate(note);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
    }

    private static T GetOrCreateComponent<T>(string objectName) where T : Component
    {
        GameObject item = GameObject.Find(objectName);
        if (item == null)
            item = new GameObject(objectName);

        T component = item.GetComponent<T>();
        return component != null ? component : item.AddComponent<T>();
    }

    private static SongAudioController GetOrCreateSongAudio()
    {
        GameObject item = GameObject.Find("SongAudio");
        if (item == null)
            item = new GameObject("SongAudio");

        AudioSource source = item.GetComponent<AudioSource>();
        if (source == null)
            source = item.AddComponent<AudioSource>();

        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 0f;
        source.volume = 1f;

        SongAudioController controller = item.GetComponent<SongAudioController>();
        if (controller == null)
            controller = item.AddComponent<SongAudioController>();

        SerializedObject serializedController = new(controller);
        serializedController.FindProperty("startDelay").doubleValue = 1.0;
        serializedController.FindProperty("bpm").doubleValue = 120.0;
        serializedController.FindProperty("songOffset").doubleValue = 0.0;
        serializedController.ApplyModifiedPropertiesWithoutUndo();
        return controller;
    }

    private static HitFeedbackUI GetOrCreateFeedbackUI()
    {
        GameObject canvasObject = GameObject.Find("HitFeedbackCanvas");
        if (canvasObject == null)
        {
            canvasObject = new GameObject(
                "HitFeedbackCanvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
        }

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        Transform existingText = canvasObject.transform.Find("FeedbackText");
        GameObject textObject;
        if (existingText == null)
        {
            textObject = new GameObject(
                "FeedbackText",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Text));
            textObject.transform.SetParent(canvasObject.transform, false);
        }
        else
        {
            textObject = existingText.gameObject;
        }

        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0f, -120f);
        rect.sizeDelta = new Vector2(700f, 140f);

        Text text = textObject.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 64;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleCenter;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;
        text.enabled = false;

        HitFeedbackUI feedback = canvasObject.GetComponent<HitFeedbackUI>();
        if (feedback == null)
            feedback = canvasObject.AddComponent<HitFeedbackUI>();

        SerializedObject serializedFeedback = new(feedback);
        serializedFeedback.FindProperty("feedbackText").objectReferenceValue = text;
        serializedFeedback.FindProperty("displayDuration").floatValue = 0.45f;
        serializedFeedback.ApplyModifiedPropertiesWithoutUndo();
        return feedback;
    }

    private static void GetOrCreateScoreUI(ScoreManager scoreManager)
    {
        GameObject canvasObject = GameObject.Find("ScoreCanvas");
        if (canvasObject == null)
        {
            canvasObject = new GameObject(
                "ScoreCanvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
        }

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 5;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        Text scoreText = GetOrCreateScoreText(
            canvasObject.transform, "ScoreText", new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(35f, -25f), TextAnchor.UpperLeft, 42);
        Text accuracyText = GetOrCreateScoreText(
            canvasObject.transform, "AccuracyText", new Vector2(1f, 1f), new Vector2(1f, 1f),
            new Vector2(-35f, -25f), TextAnchor.UpperRight, 42);
        Text comboText = GetOrCreateScoreText(
            canvasObject.transform, "ComboText", new Vector2(1f, 0f), new Vector2(1f, 0f),
            new Vector2(-55f, 150f), TextAnchor.LowerRight, 42);
        Text multiplierText = GetOrCreateScoreText(
            canvasObject.transform, "MultiplierText", new Vector2(1f, 0f), new Vector2(1f, 0f),
            new Vector2(-55f, 55f), TextAnchor.LowerRight, 60);

        ScoreDisplayUI display = canvasObject.GetComponent<ScoreDisplayUI>();
        if (display == null)
            display = canvasObject.AddComponent<ScoreDisplayUI>();

        SerializedObject serializedDisplay = new(display);
        serializedDisplay.FindProperty("scoreManager").objectReferenceValue = scoreManager;
        serializedDisplay.FindProperty("scoreText").objectReferenceValue = scoreText;
        serializedDisplay.FindProperty("comboText").objectReferenceValue = comboText;
        serializedDisplay.FindProperty("multiplierText").objectReferenceValue = multiplierText;
        serializedDisplay.FindProperty("accuracyText").objectReferenceValue = accuracyText;
        serializedDisplay.ApplyModifiedPropertiesWithoutUndo();
    }

    private static Text GetOrCreateScoreText(
        Transform parent,
        string objectName,
        Vector2 anchor,
        Vector2 pivot,
        Vector2 position,
        TextAnchor alignment,
        int fontSize)
    {
        Transform existing = parent.Find(objectName);
        GameObject textObject;
        if (existing == null)
        {
            textObject = new GameObject(
                objectName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Text));
            textObject.transform.SetParent(parent, false);
        }
        else
        {
            textObject = existing.gameObject;
        }

        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(430f, 130f);

        Text text = textObject.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.fontStyle = FontStyle.Bold;
        text.alignment = alignment;
        text.color = Color.white;
        text.raycastTarget = false;
        return text;
    }

    private static void GetOrCreateBeatMetronome(BeatGrid beatGrid)
    {
        GameObject canvasObject = GameObject.Find("ScoreCanvas");
        Text beatText = GetOrCreateScoreText(
            canvasObject.transform,
            "BeatPulseText",
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0f, -30f),
            TextAnchor.UpperCenter,
            46);
        beatText.text = "BEAT";
        beatText.rectTransform.sizeDelta = new Vector2(260f, 80f);

        BeatMetronomeDebug metronome = canvasObject.GetComponent<BeatMetronomeDebug>();
        if (metronome == null)
            metronome = canvasObject.AddComponent<BeatMetronomeDebug>();

        SerializedObject serializedMetronome = new(metronome);
        serializedMetronome.FindProperty("beatGrid").objectReferenceValue = beatGrid;
        serializedMetronome.FindProperty("beatText").objectReferenceValue = beatText;
        serializedMetronome.FindProperty("flashDuration").floatValue = 0.12f;
        serializedMetronome.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void GetOrCreateChartRecorderUI(ChartRecorder recorder)
    {
        GameObject canvasObject = GameObject.Find("ScoreCanvas");
        Text recorderText = GetOrCreateScoreText(
            canvasObject.transform,
            "ChartRecorderText",
            new Vector2(0f, 0f),
            new Vector2(0f, 0f),
            new Vector2(35f, 45f),
            TextAnchor.LowerLeft,
            26);
        recorderText.rectTransform.sizeDelta = new Vector2(600f, 180f);

        ChartRecorderDisplayUI display = canvasObject.GetComponent<ChartRecorderDisplayUI>();
        if (display == null)
            display = canvasObject.AddComponent<ChartRecorderDisplayUI>();

        SerializedObject serializedDisplay = new(display);
        serializedDisplay.FindProperty("recorder").objectReferenceValue = recorder;
        serializedDisplay.FindProperty("recorderText").objectReferenceValue = recorderText;
        serializedDisplay.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void GetOrCreateSongPackageUI(SongPackageLoader loader)
    {
        GameObject canvasObject = GameObject.Find("ScoreCanvas");
        Text songInfoText = GetOrCreateScoreText(
            canvasObject.transform,
            "SongPackageText",
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0f, -105f),
            TextAnchor.UpperCenter,
            28);
        songInfoText.rectTransform.sizeDelta = new Vector2(700f, 180f);

        SongPackageDisplayUI display = canvasObject.GetComponent<SongPackageDisplayUI>();
        if (display == null)
            display = canvasObject.AddComponent<SongPackageDisplayUI>();

        SerializedObject serializedDisplay = new(display);
        serializedDisplay.FindProperty("loader").objectReferenceValue = loader;
        serializedDisplay.FindProperty("songInfoText").objectReferenceValue = songInfoText;
        serializedDisplay.ApplyModifiedPropertiesWithoutUndo();
    }

    private static Transform GetOrCreatePoint(string objectName, Transform parent, Vector3 position)
    {
        GameObject point = GameObject.Find(objectName);
        if (point == null)
            point = new GameObject(objectName);

        point.transform.SetParent(parent, true);
        point.transform.position = position;
        return point.transform;
    }
}
