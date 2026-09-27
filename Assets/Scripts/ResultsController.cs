using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

public sealed class ResultsController : MonoBehaviour
{
    private const float ResultsDelaySeconds = 0.5f;

    [SerializeField] private SongAudioController songAudio;
    [SerializeField] private SongPackageLoader songPackageLoader;
    [SerializeField] private ScoreManager scoreManager;
    [SerializeField] private ResultVoiceController resultVoiceController;

    [Header("Result Voice Audio")]
    [SerializeField, Range(0f, 8f)] private float resultVoiceVolume = 8f;
    [SerializeField, Range(0f, 1f)] private float musicDuckVolume = 0.25f;

    [Header("Star Accuracy Thresholds")]
    [SerializeField, Range(0f, 100f)] private float twoStarThreshold = 50f;
    [SerializeField, Range(0f, 100f)] private float threeStarThreshold = 65f;
    [SerializeField, Range(0f, 100f)] private float fourStarThreshold = 80f;
    [SerializeField, Range(0f, 100f)] private float fiveStarThreshold = 95f;

    private GameObject resultsPanel;
    private ResultsVisualController visuals;
    private int currentStars;
    private static readonly UnityEngine.InputSystem.Key[] DebugStarKeys =
    {
        UnityEngine.InputSystem.Key.Digit1,
        UnityEngine.InputSystem.Key.Digit2,
        UnityEngine.InputSystem.Key.Digit3,
        UnityEngine.InputSystem.Key.Digit4,
        UnityEngine.InputSystem.Key.Digit5
    };

    public static bool GameplayFinished { get; private set; }

    public static void MarkGameplayFailed()
    {
        GameplayFinished = true;
    }

    private void Awake()
    {
        GameplayFinished = false;
        EnsureEventSystem();
        ResolveReferences();
        BuildResultsPanel();
    }

    private void Start()
    {
        StartCoroutine(WatchSongEnd());
    }

    private void Update()
    {
#if UNITY_EDITOR
        UnityEngine.InputSystem.Keyboard keyboard = UnityEngine.InputSystem.Keyboard.current;
        if (!GameplayFinished && !FailController.SongFailed && !PauseController.IsPaused && keyboard != null)
        {
            if (keyboard.f10Key.wasPressedThisFrame)
            {
                ShowResults(true);
                return;
            }

            if (keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed)
            {
                for (int index = 0; index < DebugStarKeys.Length; index++)
                {
                    if (keyboard[DebugStarKeys[index]].wasPressedThisFrame)
                    {
                        ShowResults(true, index + 1);
                        return;
                    }
                }
            }
        }
#endif
    }

    private void ResolveReferences()
    {
        if (songAudio == null)
            songAudio = FindFirstObjectByType<SongAudioController>();
        if (songPackageLoader == null)
            songPackageLoader = FindFirstObjectByType<SongPackageLoader>();
        if (scoreManager == null)
            scoreManager = FindFirstObjectByType<ScoreManager>();
        if (resultVoiceController == null)
            resultVoiceController = FindFirstObjectByType<ResultVoiceController>();
        if (resultVoiceController == null)
        {
            GameObject voiceObject = new("ResultVoiceController");
            resultVoiceController = voiceObject.AddComponent<ResultVoiceController>();
        }

        resultVoiceController.Configure(
            songAudio != null ? songAudio.AudioSource : null,
            resultVoiceVolume,
            musicDuckVolume);
    }

    private static void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null)
            return;

        GameObject eventSystem = new("EventSystem");
        eventSystem.AddComponent<EventSystem>();
        eventSystem.AddComponent<InputSystemUIInputModule>();
    }

    private IEnumerator WatchSongEnd()
    {
        while (songPackageLoader != null && songPackageLoader.State == SongPackageState.Loading)
        {
            if (FailController.SongFailed)
                yield break;
            yield return null;
        }

        if (FailController.SongFailed ||
            (songPackageLoader != null && songPackageLoader.State == SongPackageState.Error))
            yield break;

        if (songAudio == null || songAudio.AudioSource == null || !songAudio.IsScheduled)
            yield break;

        while (AudioSettings.dspTime < songAudio.SongStartDSPTime)
        {
            if (FailController.SongFailed)
                yield break;
            yield return null;
        }

        AudioSource source = songAudio.AudioSource;
        while (!source.isPlaying)
        {
            if (FailController.SongFailed)
                yield break;
            yield return null;
        }

        while (source.isPlaying || songAudio.IsPaused)
        {
            if (FailController.SongFailed)
                yield break;
            yield return null;
        }

        while (songAudio.IsPaused)
            yield return null;

        yield return new WaitForSecondsRealtime(ResultsDelaySeconds);
        if (!FailController.SongFailed)
            ShowResults(false);
    }

    private void ShowResults(bool debugPreview, int forcedStars = 0)
    {
        if (GameplayFinished || FailController.SongFailed)
            return;

        GameplayFinished = true;
        currentStars = forcedStars > 0 ? forcedStars : CalculateStarsForAccuracy(scoreManager.Accuracy);

        string songFolder = songPackageLoader != null
            ? songPackageLoader.SongFolderName
            : GameSession.SelectedSongFolder;
        SongDifficulty difficulty = songPackageLoader != null
            ? songPackageLoader.Difficulty
            : GameSession.SelectedDifficulty;

        BestRecordUpdate update = debugPreview
            ? new BestRecordUpdate { Record = BestScoreManager.GetBest(songFolder, difficulty) }
            : BestScoreManager.SaveSuccessfulRun(
                songFolder,
                difficulty,
                scoreManager.Score,
                currentStars,
                scoreManager.Accuracy,
                scoreManager.MaxCombo);

        visuals.Show(songPackageLoader.SongTitle, songPackageLoader.Artist, difficulty,
            scoreManager, currentStars, update.Record, update.HasAnyNewBest, debugPreview,
            debugPreview ? null : () =>
            {
                if (!FailController.SongFailed && resultsPanel != null && resultsPanel.activeInHierarchy)
                    resultVoiceController.PlayForStars(currentStars);
            });
    }

    public int CalculateStarsForAccuracy(double runAccuracy)
    {
        return StarRatingCalculator.Calculate(
            runAccuracy,
            twoStarThreshold,
            threeStarThreshold,
            fourStarThreshold,
            fiveStarThreshold);
    }

    public void PlayFailureVoice(int stars)
    {
        if (resultVoiceController != null)
            resultVoiceController.PlayForStars(stars, true);
    }

    private void OnValidate()
    {
        twoStarThreshold = Mathf.Clamp(twoStarThreshold, 0f, 100f);
        threeStarThreshold = Mathf.Clamp(threeStarThreshold, twoStarThreshold, 100f);
        fourStarThreshold = Mathf.Clamp(fourStarThreshold, threeStarThreshold, 100f);
        fiveStarThreshold = Mathf.Clamp(fiveStarThreshold, fourStarThreshold, 100f);
    }

    private void Replay()
    {
        GameplayFinished = false;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void BackToSongs()
    {
        GameplayFinished = false;
        GameSession.ClearSelection();
        SceneManager.LoadScene("SongSelect");
    }

    private void BuildResultsPanel()
    {
        visuals = gameObject.AddComponent<ResultsVisualController>();
        visuals.Build(Replay, BackToSongs);
        resultsPanel = visuals.Panel;
    }
}
