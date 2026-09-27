using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

public enum SongPackageState
{
    Loading,
    Ready,
    Error
}

[DefaultExecutionOrder(-1200)]
public sealed class SongPackageLoader : MonoBehaviour
{
    [SerializeField] private string songFolderName = "TestSong";

    [Header("References")]
    [SerializeField] private SongAudioController songAudioController;
    [SerializeField] private ChartPlayer chartPlayer;

    [Header("Runtime State")]
    [SerializeField] private SongPackageState state = SongPackageState.Loading;
    [SerializeField] private string songTitle = "---";
    [SerializeField] private string artist = "---";
    [SerializeField] private double bpm;
    [SerializeField] private SongDifficulty difficulty = SongDifficulty.Normal;
    [SerializeField] private string lastError = "";

    private SongMetadata metadata;

    public SongPackageState State => state;
    public string SongTitle => songTitle;
    public string Artist => artist;
    public string SongFolderName => songFolderName;
    public double BPM => bpm;
    public SongDifficulty Difficulty => difficulty;
    public string LastError => lastError;
    public string CurrentSongFolderPath => Path.Combine(
        Application.streamingAssetsPath,
        "Songs",
        songFolderName);
    public string CurrentDifficultyChartPath => Path.Combine(CurrentSongFolderPath, GetChartFileName(difficulty));
    public string CurrentChartPath { get; private set; }

    public event Action StateChanged;

    private IEnumerator Start()
    {
        if (string.IsNullOrWhiteSpace(GameSession.SelectedSongFolder))
        {
            Debug.LogWarning("No song was selected. Loading TestSong as fallback.", this);
            songFolderName = "TestSong";
        }
        else
        {
            songFolderName = GameSession.SelectedSongFolder;
        }
        difficulty = GameSession.SelectedDifficulty;

        yield return LoadCurrentSong();
    }

    private IEnumerator LoadCurrentSong()
    {
        SetState(SongPackageState.Loading, "");

        string metadataPath = Path.Combine(CurrentSongFolderPath, "metadata.json");
        string metadataJson = null;
        yield return LoadTextFile(metadataPath, value => metadataJson = value);
        if (state == SongPackageState.Error)
            yield break;

        metadata = JsonUtility.FromJson<SongMetadata>(metadataJson);
        if (!ValidateMetadata(metadata))
            yield break;

        songTitle = metadata.title;
        artist = metadata.artist;
        bpm = metadata.bpm;
        StateChanged?.Invoke();

        string preferredChartPath = CurrentDifficultyChartPath;
        if (File.Exists(preferredChartPath))
        {
            CurrentChartPath = preferredChartPath;
        }
        else
        {
            string fallbackChartPath = Path.Combine(CurrentSongFolderPath, "chart.json");
            if (!File.Exists(fallbackChartPath))
            {
                Fail($"Missing {Path.GetFileName(preferredChartPath)} and chart.json fallback in {CurrentSongFolderPath}.");
                yield break;
            }

            CurrentChartPath = fallbackChartPath;
            Debug.LogWarning(
                $"Missing {Path.GetFileName(preferredChartPath)}, using chart.json fallback.", this);
        }

        string chartJson = null;
        yield return LoadTextFile(CurrentChartPath, value => chartJson = value);
        if (state == SongPackageState.Error)
            yield break;

        ChartData chartData = JsonUtility.FromJson<ChartData>(chartJson);
        List<RecordedNote> notes = chartData?.ToRecordedNotes();
        if (notes == null)
        {
            Fail($"Invalid chart JSON: {CurrentChartPath}");
            yield break;
        }

        string audioPath = Path.Combine(CurrentSongFolderPath, metadata.audioFile);
        AudioClip audioClip = null;
        yield return LoadWavFile(audioPath, value => audioClip = value);
        if (state == SongPackageState.Error)
            yield break;

        string backgroundVideoPath = Path.Combine(CurrentSongFolderPath, "background.mp4");
        if (File.Exists(backgroundVideoPath))
        {
            GameObject videoObject = new("SongBackgroundVideo");
            SongBackgroundVideoController videoController = videoObject.AddComponent<SongBackgroundVideoController>();
            videoController.PrepareVideo(CurrentSongFolderPath, songTitle, songAudioController,
                FindFirstObjectByType<SongClock>());
        }

        songAudioController.ConfigureAndSchedule(audioClip, metadata.bpm, metadata.offset, chartPlayer.NoteTravelTime);
        chartPlayer.PlayChart(notes);
        SetState(SongPackageState.Ready, "");
        Debug.Log($"Song package ready: {metadata.title} — {metadata.artist} ({notes.Count} notes)");
    }

    private IEnumerator LoadTextFile(string path, Action<string> onLoaded)
    {
        using UnityWebRequest request = UnityWebRequest.Get(ToRequestUri(path));
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Fail($"Could not load file: {path}\n{request.error}");
            yield break;
        }

        onLoaded(request.downloadHandler.text);
    }

    private IEnumerator LoadWavFile(string path, Action<AudioClip> onLoaded)
    {
        using UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip(
            ToRequestUri(path),
            AudioType.WAV);
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Fail($"Could not load WAV: {path}\n{request.error}");
            yield break;
        }

        AudioClip clip = DownloadHandlerAudioClip.GetContent(request);
        if (clip == null)
        {
            Fail($"Loaded WAV did not produce an AudioClip: {path}");
            yield break;
        }

        onLoaded(clip);
    }

    private bool ValidateMetadata(SongMetadata value)
    {
        if (value == null)
        {
            Fail("metadata.json is empty or invalid.");
            return false;
        }
        if (value.bpm <= 0.0)
        {
            Fail("metadata.json must contain bpm > 0.");
            return false;
        }
        if (string.IsNullOrWhiteSpace(value.audioFile))
        {
            Fail("metadata.json must contain audioFile.");
            return false;
        }
        return true;
    }

    private static string GetChartFileName(SongDifficulty selectedDifficulty)
    {
        return selectedDifficulty switch
        {
            SongDifficulty.Normal => "chart_normal.json",
            SongDifficulty.Hard => "chart_hard.json",
            SongDifficulty.Expert => "chart_expert.json",
            _ => "chart_normal.json"
        };
    }

    private void SetState(SongPackageState newState, string error)
    {
        state = newState;
        lastError = error;
        StateChanged?.Invoke();
    }

    private void Fail(string message)
    {
        SetState(SongPackageState.Error, message);
        Debug.LogError($"SongPackageLoader: {message}", this);
    }

    private static string ToRequestUri(string path)
    {
        return path.Contains("://", StringComparison.Ordinal)
            ? path
            : new Uri(path).AbsoluteUri;
    }
}
