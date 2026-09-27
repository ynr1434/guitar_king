using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class FailController : MonoBehaviour
{
    [SerializeField] private RockMeter rockMeter;
    [SerializeField] private SongAudioController songAudio;
    [SerializeField] private SongClock songClock;
    [SerializeField] private ChartPlayer chartPlayer;
    [SerializeField] private SongPackageLoader songPackageLoader;
    [SerializeField] private ScoreManager scoreManager;
    [SerializeField] private ResultsController resultsController;

    private GameObject failPanel;
    private ResultsVisualController visuals;

    public static bool SongFailed { get; private set; }

    private void Awake()
    {
        SongFailed = false;
        ResolveReferences();
        BuildFailPanel();
    }

    public void Initialize(RockMeter meter)
    {
        if (rockMeter == null)
            rockMeter = meter;
    }

    public void TriggerFail()
    {
        if (SongFailed)
            return;

        SongFailed = true;
        ResultsController.MarkGameplayFailed();

        if (songClock != null)
            songClock.Freeze();
        if (songAudio != null)
            songAudio.StopPlayback();
        if (chartPlayer != null)
            chartPlayer.StopForFail();

        double currentAccuracy = scoreManager != null ? scoreManager.Accuracy : 0.0;
        int accuracyStars = resultsController != null
            ? resultsController.CalculateStarsForAccuracy(currentAccuracy)
            : StarRatingCalculator.Calculate(currentAccuracy);

        visuals.Show(
            songPackageLoader != null ? songPackageLoader.SongTitle : "Unknown Song",
            songPackageLoader != null ? songPackageLoader.Artist : "",
            songPackageLoader != null ? songPackageLoader.Difficulty : GameSession.SelectedDifficulty,
            scoreManager, accuracyStars, default, false, false,
            () => { if (resultsController != null && SongFailed) resultsController.PlayFailureVoice(accuracyStars); });
    }

    private void ResolveReferences()
    {
        if (rockMeter == null)
            rockMeter = FindFirstObjectByType<RockMeter>();
        if (songAudio == null)
            songAudio = FindFirstObjectByType<SongAudioController>();
        if (songClock == null)
            songClock = FindFirstObjectByType<SongClock>();
        if (chartPlayer == null)
            chartPlayer = FindFirstObjectByType<ChartPlayer>();
        if (songPackageLoader == null)
            songPackageLoader = FindFirstObjectByType<SongPackageLoader>();
        if (scoreManager == null)
            scoreManager = FindFirstObjectByType<ScoreManager>();
        if (resultsController == null)
            resultsController = FindFirstObjectByType<ResultsController>();
    }

    private void BuildFailPanel()
    {
        visuals = gameObject.AddComponent<ResultsVisualController>();
        visuals.Build(Retry, BackToSongs, true);
        failPanel = visuals.Panel;
    }

    private void Retry()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void BackToSongs()
    {
        GameSession.ClearSelection();
        SceneManager.LoadScene("SongSelect");
    }

}
