using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

public sealed class SongSelectController : MonoBehaviour
{
    private sealed class SongEntry
    {
        public string FolderName;
        public SongMetadata Metadata;
        public string Error;
        public SetListRowView Row;
    }

    private readonly List<SongEntry> songs = new();
    private SongEntry selectedSong;
    private SongDifficulty selectedDifficulty = SongDifficulty.Normal;
    private SetListUIController view;
    private Text detailsText;
    private Text statusText;
    private Text difficultyText;
    private Button playButton;
    private SettingsPanel settingsPanel;

    private void Start()
    {
        selectedDifficulty = SongDifficulty.Normal;
        GameSession.SelectSong(GameSession.SelectedSongFolder, SongDifficulty.Normal);
        EnsureEventSystem();
        BuildUI();
        ScanSongs();
        GameplayTutorialController.ShowWelcomeOnce(gameObject);
    }

    private void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null)
            return;

        GameObject eventSystem = new("EventSystem");
        eventSystem.AddComponent<EventSystem>();
        eventSystem.AddComponent<InputSystemUIInputModule>();
    }

    private void BuildUI()
    {
        view = gameObject.AddComponent<SetListUIController>();
        view.Build(PlaySelectedSong, SelectDifficulty, OpenSettings);
        detailsText = view.DetailsText;
        statusText = view.StatusText;
        difficultyText = view.DifficultyText;
        playButton = view.PlayButton;
        RefreshDifficultyUI();
        settingsPanel = gameObject.AddComponent<SettingsPanel>();
        settingsPanel.Build(200);
    }

    private void OpenSettings() { view.gameObject.SetActive(true); settingsPanel.Open(() => { }); }

    private void ScanSongs()
    {
        string songsDirectory = Path.Combine(Application.streamingAssetsPath, "Songs");
        if (!Directory.Exists(songsDirectory))
        {
            statusText.text = $"Songs folder not found:\n{songsDirectory}";
            Debug.LogError(statusText.text, this);
            return;
        }

        string[] directories = Directory.GetDirectories(songsDirectory);
        Array.Sort(directories, StringComparer.OrdinalIgnoreCase);
        foreach (string directory in directories)
        {
            SongEntry entry = ReadSongEntry(directory);
            songs.Add(entry);
            CreateSongButton(entry);
        }

        if (songs.Count == 0)
            statusText.text = "No song folders found.";
        else
            statusText.text = "Choose a song. Your personal best is shown on the right.";
        view.SetSongCount(songs.Count);
    }

    private SongEntry ReadSongEntry(string directory)
    {
        SongEntry entry = new() { FolderName = Path.GetFileName(directory) };
        string metadataPath = Path.Combine(directory, "metadata.json");
        if (!File.Exists(metadataPath))
        {
            entry.Error = "metadata.json is missing";
            Debug.LogError($"Invalid Song '{entry.FolderName}': {entry.Error}", this);
            return entry;
        }

        try
        {
            entry.Metadata = JsonUtility.FromJson<SongMetadata>(File.ReadAllText(metadataPath));
            if (entry.Metadata == null || string.IsNullOrWhiteSpace(entry.Metadata.title) || entry.Metadata.bpm <= 0)
                entry.Error = "metadata.json is invalid (title and positive bpm are required)";
        }
        catch (Exception exception)
        {
            entry.Error = $"metadata.json could not be read: {exception.Message}";
        }

        if (!string.IsNullOrEmpty(entry.Error))
            Debug.LogError($"Invalid Song '{entry.FolderName}': {entry.Error}", this);
        return entry;
    }

    private void CreateSongButton(SongEntry entry)
    {
        bool valid = entry.Metadata != null && string.IsNullOrEmpty(entry.Error);
        BestRecord best = valid ? BestScoreManager.GetBest(entry.FolderName, selectedDifficulty) : default;
        entry.Row = view.CreateRow(entry.FolderName,
            valid ? entry.Metadata.title : "Invalid Song",
            valid ? entry.Metadata.artist : entry.FolderName,
            best, valid, songs.Count, () => SelectSong(entry));
    }

    private void SelectSong(SongEntry entry)
    {
        selectedSong = entry;
        RefreshSelectedSongDetails();
        playButton.interactable = true;
        statusText.text = "Ready for the stage.";
        foreach (SongEntry song in songs)
            song.Row.SetSelected(song == selectedSong);
    }

    private void SelectDifficulty(SongDifficulty difficulty)
    {
        if (difficulty != SongDifficulty.Normal)
            return;

        selectedDifficulty = difficulty;
        RefreshDifficultyUI();
        RefreshSelectedSongDetails();
        if (selectedSong != null)
            statusText.text = "Ready for the stage.";
    }

    private void RefreshSelectedSongDetails()
    {
        detailsText.text = selectedSong == null
            ? "Choose a song from tonight's set."
            : selectedSong.Metadata.title + "  /  " + selectedSong.Metadata.bpm.ToString("0.##") + " BPM";
    }

    private void RefreshDifficultyUI()
    {
        difficultyText.text = $"DIFFICULTY / {selectedDifficulty.ToString().ToUpperInvariant()}";
    }

    private void PlaySelectedSong()
    {
        if (selectedSong == null || !string.IsNullOrEmpty(selectedSong.Error))
            return;

        selectedDifficulty = SongDifficulty.Normal;
        GameSession.SelectSong(selectedSong.FolderName, selectedDifficulty);
        SceneManager.LoadScene("RhythmGame");
    }

}
