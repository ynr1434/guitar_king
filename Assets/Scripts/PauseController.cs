using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class PauseController : MonoBehaviour
{
    [SerializeField] private SongAudioController songAudio;
    [SerializeField] private SongClock songClock;
    [SerializeField] private ChartRecorder chartRecorder;

    private GameObject pausePanel;
    private SettingsPanel settingsPanel;

    public static bool IsPaused { get; private set; }

    private void Awake()
    {
        IsPaused = false;
        ResolveReferences();
        BuildPausePanel();
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null || !keyboard.escapeKey.wasPressedThisFrame)
            return;

        if (GameplayTutorialController.IsTutorialOpen || SettingsPanel.IsAnyOpen || SettingsPanel.ClosedThisFrame)
            return;

        if (ChartRecorder.IsRecordingActive || (chartRecorder != null && chartRecorder.Recording))
            return;

        if (FailController.SongFailed || ResultsController.GameplayFinished)
        {
            BackToSongs();
            return;
        }

        if (IsPaused)
        {
            Resume();
            return;
        }

        if (CanPause())
            Pause();
    }

    private bool CanPause()
    {
        return songAudio != null && songAudio.IsScheduled &&
               (songAudio.IsPaused || AudioSettings.dspTime < songAudio.SongStartDSPTime ||
                (songAudio.AudioSource != null && songAudio.AudioSource.isPlaying));
    }

    private void Pause()
    {
        if (IsPaused || ResultsController.GameplayFinished || FailController.SongFailed)
            return;

        IsPaused = true;
        songAudio.PausePlayback();
        songClock?.PauseClock();
        pausePanel.SetActive(true);
    }

    private void Resume()
    {
        if (!IsPaused)
            return;

        pausePanel.SetActive(false);
        songClock?.ResumeClock();
        songAudio?.ResumePlayback();
        IsPaused = false;
    }

    private void RestartSong()
    {
        IsPaused = false;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void BackToSongs()
    {
        IsPaused = false;
        if (songAudio != null)
            songAudio.StopPlayback();
        GameSession.ClearSelection();
        SceneManager.LoadScene("SongSelect");
    }

    private void ResolveReferences()
    {
        if (songAudio == null)
            songAudio = FindFirstObjectByType<SongAudioController>();
        if (songClock == null)
            songClock = FindFirstObjectByType<SongClock>();
        if (chartRecorder == null)
            chartRecorder = FindFirstObjectByType<ChartRecorder>();
    }

    private void BuildPausePanel()
    {
        GameObject canvasObject = new(
            "PauseCanvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 110;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        pausePanel = new GameObject("PausePanel", typeof(RectTransform), typeof(Image));
        pausePanel.transform.SetParent(canvasObject.transform, false);
        RectTransform panelRect = pausePanel.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;
        pausePanel.GetComponent<Image>().color = new Color(0.008f, 0.012f, 0.025f, 0.88f);

        RectTransform card = RockUIStyle.Plate(pausePanel.transform, "PauseRoadCase",
            new Vector2(.27f, .10f), new Vector2(.73f, .90f), RockUIStyle.Gold);
        RockUIStyle.Label(card, "Brand", "GUITAR KING  /  BACKSTAGE", 17, RockUIStyle.Gold,
            new(.09f, .915f), new(.91f, .96f));
        RockUIStyle.Label(card, "PausedTitle", "PAUSED", 68, Color.white,
            new(.09f, .785f), new(.91f, .915f));
        RockUIStyle.Label(card, "Subtitle", "TAKE A BREATH. THE STAGE IS YOURS.", 16,
            new(.61f, .63f, .69f), new(.09f, .737f), new(.91f, .79f));
        RockUIStyle.Box(card, "HeaderRule", new(.09f, .715f), new(.91f, .717f), RockUIStyle.Steel);

        // One layout owns every button: equal row heights, fixed spacing and
        // enough room inside the frame at both 720p and 1080p.
        GameObject actions = new("PauseActions", typeof(RectTransform), typeof(VerticalLayoutGroup));
        RockUIStyle.Rect(actions, card, new(.09f, .145f), new(.91f, .68f));
        VerticalLayoutGroup layout = actions.GetComponent<VerticalLayoutGroup>();
        layout.spacing = 14f;
        layout.childControlWidth = layout.childControlHeight = true;
        layout.childForceExpandWidth = layout.childForceExpandHeight = true;

        Button resume = CreateButton("ResumeButton", actions.transform, "RESUME", true);
        resume.onClick.AddListener(Resume);
        Button restart = CreateButton("RestartButton", actions.transform, "RESTART SONG");
        restart.onClick.AddListener(RestartSong);
        Button settings = CreateButton("SettingsButton", actions.transform, "SETTINGS");
        settings.onClick.AddListener(OpenSettings);
        Button howToPlay = CreateButton("HowToPlayButton", actions.transform, "HOW TO PLAY");
        howToPlay.onClick.AddListener(OpenTutorial);
        Button back = CreateButton("BackToSongsButton", actions.transform, "BACK TO SONGS");
        back.onClick.AddListener(BackToSongs);
        RockUIStyle.Box(card, "FooterRule", new(.09f, .105f), new(.91f, .107f), RockUIStyle.Steel);
        RockUIStyle.Label(card, "EscapeHint", "ESC  /  RETURN TO THE STAGE", 16,
            new(.61f, .63f, .69f), new(.09f, .04f), new(.91f, .09f), TextAnchor.MiddleCenter);

        settingsPanel = gameObject.AddComponent<SettingsPanel>();
        settingsPanel.Build(210);

        pausePanel.SetActive(false);
    }

    private void OpenTutorial()
    {
        if (!IsPaused || GameplayTutorialController.IsTutorialOpen) return;
        pausePanel.SetActive(false);
        GameplayTutorialController tutorial = GetComponent<GameplayTutorialController>();
        if (tutorial == null) tutorial = gameObject.AddComponent<GameplayTutorialController>();
        tutorial.Open(() => pausePanel.SetActive(true));
    }

    private void OpenSettings()
    {
        if (!IsPaused) return;
        pausePanel.SetActive(false);
        settingsPanel.Open(() => pausePanel.SetActive(true));
    }

    private static Text CreateText(string objectName, Transform parent, string value,
        int fontSize, FontStyle style)
    {
        GameObject textObject = new(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        textObject.transform.SetParent(parent, false);
        Text text = textObject.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.text = value;
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.raycastTarget = false;
        return text;
    }

    private static Button CreateButton(string objectName, Transform parent, string label, bool primary = false)
    {
        GameObject buttonObject = new(objectName, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);
        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        Image image = buttonObject.GetComponent<Image>();
        image.color = Color.white;
        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        ColorBlock colors = button.colors;
        colors.normalColor = primary ? RockUIStyle.Gold : new Color(.105f, .11f, .14f);
        colors.highlightedColor = primary ? new Color(1f, .79f, .4f) : new Color(.23f, .20f, .17f);
        colors.selectedColor = colors.highlightedColor;
        colors.pressedColor = primary ? new Color(.82f, .40f, .09f) : new Color(.075f, .08f, .10f);
        button.colors = colors;
        Image edge = RockUIStyle.Box(buttonObject.transform, "Accent", Vector2.zero,
            new Vector2(.007f, 1f), primary ? new Color(1f, .86f, .56f) : RockUIStyle.Steel);
        ResultsButtonFeedback feedback = buttonObject.AddComponent<ResultsButtonFeedback>();
        feedback.accent = edge;
        feedback.highlightColor = primary ? Color.white : RockUIStyle.Gold;

        Text text = CreateText("Label", buttonObject.transform, label, 25, FontStyle.Bold);
        text.color = primary ? new Color(.09f, .055f, .02f) : new Color(.90f, .91f, .94f);
        text.alignment = TextAnchor.MiddleLeft;
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = 16;
        text.resizeTextMaxSize = 25;
        RectTransform textRect = text.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(28f, 4f);
        textRect.offsetMax = new Vector2(-60f, -4f);
        RockUIStyle.Label(buttonObject.transform, "Arrow", ">", 24, text.color,
            new(.87f, 0), new(.95f, 1), TextAnchor.MiddleCenter);
        return button;
    }

    private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = new Vector2(12f, 8f);
        rect.offsetMax = new Vector2(-12f, -8f);
    }
}
