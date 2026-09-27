using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

public sealed class SongPackageCreatorWindow : EditorWindow
{
    private const string SongsAssetPath = "Assets/StreamingAssets/Songs";
    private static readonly string[] DifficultyCharts =
    {
        "chart_normal.json",
        "chart_hard.json",
        "chart_expert.json"
    };

    private string songTitle = "";
    private string artist = "Unknown Artist";
    private string folderName = "";
    private double bpm = 120.0;
    private double offset;
    private AudioClip audioClip;
    private string createMessage = "";
    private MessageType createMessageType = MessageType.Info;
    private string createdFolderPath = "";

    private string[] songFolders = Array.Empty<string>();
    private int selectedSongIndex;
    private string loadedFolderName;
    private SongMetadata existingMetadata;
    private string existingMetadataError = "";
    private string editMessage = "";
    private MessageType editMessageType = MessageType.Info;
    private string validationReport = "";
    private MessageType validationMessageType = MessageType.Info;

    [MenuItem("Tools/Guitar King/Song Package Creator")]
    public static void Open()
    {
        SongPackageCreatorWindow window = GetWindow<SongPackageCreatorWindow>("Song Package Creator");
        window.minSize = new Vector2(460f, 620f);
        window.RefreshSongFolders();
        window.Show();
    }

    private void OnEnable()
    {
        RefreshSongFolders();
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Song Package Creator", EditorStyles.largeLabel);
        EditorGUILayout.HelpBox("Create and validate song folders used by the runtime Song Select.", MessageType.Info);

        DrawCreateSection();
        EditorGUILayout.Space(14f);
        DrawExistingSongSection();
    }

    private void DrawCreateSection()
    {
        EditorGUILayout.LabelField("CREATE NEW SONG", EditorStyles.boldLabel);
        songTitle = EditorGUILayout.TextField("Song Title", songTitle);
        artist = EditorGUILayout.TextField("Artist", artist);

        if (string.IsNullOrWhiteSpace(folderName) && !string.IsNullOrWhiteSpace(songTitle))
            folderName = SanitizeFolderName(songTitle);
        folderName = EditorGUILayout.TextField("Folder Name", folderName);
        bpm = EditorGUILayout.DoubleField("BPM", bpm);
        offset = EditorGUILayout.DoubleField("Offset", offset);
        audioClip = (AudioClip)EditorGUILayout.ObjectField("Audio File (WAV)", audioClip, typeof(AudioClip), false);

        EditorGUILayout.HelpBox($"Package location: {SongsAssetPath}/<FolderName>/", MessageType.None);
        if (GUILayout.Button("CREATE SONG PACKAGE", GUILayout.Height(32f)))
            CreateSongPackage();

        if (!string.IsNullOrEmpty(createMessage))
            EditorGUILayout.HelpBox(createMessage, createMessageType);
        if (!string.IsNullOrEmpty(createdFolderPath) && GUILayout.Button("OPEN SONG FOLDER"))
            EditorUtility.RevealInFinder(createdFolderPath);
    }

    private void DrawExistingSongSection()
    {
        EditorGUILayout.LabelField("EDIT EXISTING SONG", EditorStyles.boldLabel);
        if (songFolders.Length == 0)
        {
            EditorGUILayout.HelpBox("No song folders found.", MessageType.Warning);
            if (GUILayout.Button("Refresh"))
                RefreshSongFolders();
            return;
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            DrawSongFolderPopup();
            if (GUILayout.Button("Refresh", GUILayout.Width(72f)))
                RefreshSongFolders();
        }

        EnsureSelectedMetadataLoaded();
        if (!string.IsNullOrEmpty(existingMetadataError))
            EditorGUILayout.HelpBox(existingMetadataError, MessageType.Error);
        else if (existingMetadata == null)
            EditorGUILayout.HelpBox("Select a song folder to load its metadata.", MessageType.Info);

        if (existingMetadata != null)
        {
            existingMetadata.title = EditorGUILayout.TextField("Song Title", existingMetadata.title);
            existingMetadata.artist = EditorGUILayout.TextField("Artist", existingMetadata.artist);
            existingMetadata.bpm = EditorGUILayout.DoubleField("BPM", existingMetadata.bpm);
            existingMetadata.offset = EditorGUILayout.DoubleField("Offset", existingMetadata.offset);
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            using (new EditorGUI.DisabledScope(existingMetadata == null))
            if (GUILayout.Button("SAVE METADATA"))
                SaveExistingMetadata();
            if (GUILayout.Button("VALIDATE PACKAGE"))
                ValidateSelectedPackage();
        }

        if (GUILayout.Button("CREATE MISSING CHARTS"))
            CreateMissingCharts();

        if (!string.IsNullOrEmpty(editMessage))
            EditorGUILayout.HelpBox(editMessage, editMessageType);
        if (!string.IsNullOrEmpty(validationReport))
            EditorGUILayout.HelpBox(validationReport, validationMessageType);
    }

    private void DrawSongFolderPopup()
    {
        string[] labels = Array.ConvertAll(songFolders, folder => folder);
        int newIndex = EditorGUILayout.Popup("Song Folder", selectedSongIndex, labels);
        if (newIndex != selectedSongIndex)
        {
            selectedSongIndex = newIndex;
            LoadExistingMetadata();
        }
    }

    private void CreateSongPackage()
    {
        createMessage = "";
        createdFolderPath = "";

        if (string.IsNullOrWhiteSpace(songTitle))
        {
            SetCreateError("Song Title cannot be empty.");
            return;
        }
        if (bpm <= 0.0 || double.IsNaN(bpm) || double.IsInfinity(bpm))
        {
            SetCreateError("BPM must be a finite number greater than zero.");
            return;
        }
        if (audioClip == null)
        {
            SetCreateError("Assign a WAV AudioClip.");
            return;
        }

        string sourceAssetPath = AssetDatabase.GetAssetPath(audioClip);
        if (string.IsNullOrWhiteSpace(sourceAssetPath) ||
            !string.Equals(Path.GetExtension(sourceAssetPath), ".wav", StringComparison.OrdinalIgnoreCase))
        {
            SetCreateError("The selected AudioClip must come from a .wav file. MP3 conversion is not supported.");
            return;
        }

        string sourceAbsolutePath = Path.GetFullPath(sourceAssetPath);
        if (!File.Exists(sourceAbsolutePath))
        {
            SetCreateError($"The selected WAV file does not exist on disk:\n{sourceAbsolutePath}");
            return;
        }

        string safeFolderName = SanitizeFolderName(folderName);
        if (string.IsNullOrWhiteSpace(safeFolderName))
        {
            SetCreateError("Folder Name is empty after removing unsafe characters. Enter a usable name.");
            return;
        }
        folderName = safeFolderName;

        string songsAbsolutePath = Path.GetFullPath(SongsAssetPath);
        Directory.CreateDirectory(songsAbsolutePath);
        string destinationPath = Path.Combine(songsAbsolutePath, safeFolderName);
        if (Directory.Exists(destinationPath) || File.Exists(destinationPath))
        {
            EditorUtility.DisplayDialog("Song package already exists",
                $"The folder already exists and will not be overwritten:\n{destinationPath}", "Cancel");
            SetCreateError("Song package already exists. Existing files were left untouched.");
            return;
        }

        try
        {
            Directory.CreateDirectory(destinationPath);
            File.Copy(sourceAbsolutePath, Path.Combine(destinationPath, "song.wav"), false);

            SongMetadata metadata = new()
            {
                title = songTitle.Trim(),
                artist = artist?.Trim() ?? "",
                bpm = bpm,
                offset = offset,
                audioFile = "song.wav",
                chartFile = "chart_normal.json"
            };
            File.WriteAllText(Path.Combine(destinationPath, "metadata.json"), JsonUtility.ToJson(metadata, true));

            string emptyChartJson = JsonUtility.ToJson(new ChartData(), true);
            foreach (string chartName in DifficultyCharts)
                File.WriteAllText(Path.Combine(destinationPath, chartName), emptyChartJson);

            AssetDatabase.Refresh();
            createdFolderPath = destinationPath;
            createMessage = $"Song Package Created\n{destinationPath}";
            createMessageType = MessageType.Info;
            RefreshSongFolders();
        }
        catch (Exception exception)
        {
            SetCreateError($"Could not create song package:\n{exception.Message}");
        }
    }

    private void RefreshSongFolders()
    {
        string absoluteSongsPath = Path.GetFullPath(SongsAssetPath);
        if (!Directory.Exists(absoluteSongsPath))
        {
            songFolders = Array.Empty<string>();
            selectedSongIndex = 0;
            existingMetadata = null;
            loadedFolderName = null;
            return;
        }

        string previousSelection = SelectedFolderName;
        songFolders = Directory.GetDirectories(absoluteSongsPath)
            .Select(Path.GetFileName)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        selectedSongIndex = Array.IndexOf(songFolders, previousSelection);
        if (selectedSongIndex < 0)
            selectedSongIndex = 0;
        LoadExistingMetadata();
    }

    private string SelectedFolderName => songFolders.Length > 0 &&
        selectedSongIndex >= 0 && selectedSongIndex < songFolders.Length
            ? songFolders[selectedSongIndex]
            : null;

    private void EnsureSelectedMetadataLoaded()
    {
        if (!string.Equals(loadedFolderName, SelectedFolderName, StringComparison.Ordinal))
            LoadExistingMetadata();
    }

    private void LoadExistingMetadata()
    {
        loadedFolderName = SelectedFolderName;
        existingMetadata = null;
        existingMetadataError = "";
        editMessage = "";
        validationReport = "";
        if (string.IsNullOrEmpty(loadedFolderName))
            return;

        string metadataPath = Path.Combine(GetSelectedSongPath(), "metadata.json");
        if (!File.Exists(metadataPath))
        {
            existingMetadataError = "metadata.json is missing.";
            return;
        }

        try
        {
            existingMetadata = JsonUtility.FromJson<SongMetadata>(File.ReadAllText(metadataPath));
            if (existingMetadata == null)
                existingMetadataError = "metadata.json is empty or invalid.";
        }
        catch (Exception exception)
        {
            existingMetadataError = $"Could not read metadata.json: {exception.Message}";
        }
    }

    private void SaveExistingMetadata()
    {
        if (existingMetadata == null)
            return;
        if (string.IsNullOrWhiteSpace(existingMetadata.title))
        {
            ShowEditError("Song Title cannot be empty.");
            return;
        }
        if (existingMetadata.bpm <= 0.0 || double.IsNaN(existingMetadata.bpm) || double.IsInfinity(existingMetadata.bpm))
        {
            ShowEditError("BPM must be a finite number greater than zero.");
            return;
        }

        try
        {
            string metadataPath = Path.Combine(GetSelectedSongPath(), "metadata.json");
            File.WriteAllText(metadataPath, JsonUtility.ToJson(existingMetadata, true));
            AssetDatabase.Refresh();
            editMessage = "Metadata saved. Audio and chart files were not changed.";
            editMessageType = MessageType.Info;
        }
        catch (Exception exception)
        {
            ShowEditError($"Could not save metadata.json: {exception.Message}");
        }
    }

    private void ValidateSelectedPackage()
    {
        List<string> problems = new();
        string folderPath = GetSelectedSongPath();
        string metadataPath = Path.Combine(folderPath, "metadata.json");
        SongMetadata metadata = null;

        if (!File.Exists(metadataPath))
        {
            problems.Add("Missing metadata.json");
        }
        else
        {
            try
            {
                metadata = JsonUtility.FromJson<SongMetadata>(File.ReadAllText(metadataPath));
                if (metadata == null)
                    problems.Add("Invalid metadata.json");
                else if (metadata.bpm <= 0.0 || double.IsNaN(metadata.bpm) || double.IsInfinity(metadata.bpm))
                    problems.Add("Invalid BPM");
            }
            catch (Exception exception)
            {
                problems.Add($"Invalid metadata JSON: {exception.Message}");
            }
        }

        foreach (string chartName in DifficultyCharts)
            if (!File.Exists(Path.Combine(folderPath, chartName)))
                problems.Add($"Missing {chartName}");

        string songWavPath = Path.Combine(folderPath, "song.wav");
        if (!File.Exists(songWavPath))
            problems.Add("Missing song.wav");

        if (metadata != null && !string.IsNullOrWhiteSpace(metadata.audioFile))
        {
            string audioPath = GetSafePackageFilePath(folderPath, metadata.audioFile);
            if (audioPath == null || !File.Exists(audioPath))
                problems.Add($"audioFile points to a missing or invalid file: {metadata.audioFile}");
        }
        else if (metadata != null)
        {
            problems.Add("metadata audioFile is empty");
        }

        if (problems.Count == 0)
        {
            validationReport = "VALID";
            validationMessageType = MessageType.Info;
        }
        else
        {
            validationReport = "Package issues:\n• " + string.Join("\n• ", problems);
            validationMessageType = MessageType.Error;
        }
    }

    private void CreateMissingCharts()
    {
        if (string.IsNullOrEmpty(SelectedFolderName))
            return;

        List<string> created = new();
        try
        {
            string folderPath = GetSelectedSongPath();
            string emptyChartJson = JsonUtility.ToJson(new ChartData(), true);
            foreach (string chartName in DifficultyCharts)
            {
                string chartPath = Path.Combine(folderPath, chartName);
                if (File.Exists(chartPath))
                    continue;
                File.WriteAllText(chartPath, emptyChartJson);
                created.Add(chartName);
            }

            AssetDatabase.Refresh();
            editMessage = created.Count == 0
                ? "All difficulty charts already exist; no files changed."
                : "Created missing charts: " + string.Join(", ", created);
            editMessageType = MessageType.Info;
            validationReport = "";
        }
        catch (Exception exception)
        {
            ShowEditError($"Could not create missing charts: {exception.Message}");
        }
    }

    private string GetSelectedSongPath()
    {
        return Path.Combine(Path.GetFullPath(SongsAssetPath), SelectedFolderName ?? "");
    }

    private static string GetSafePackageFilePath(string folderPath, string relativePath)
    {
        if (Path.IsPathRooted(relativePath))
            return null;

        string fullFolderPath = Path.GetFullPath(folderPath) + Path.DirectorySeparatorChar;
        string fullPath = Path.GetFullPath(Path.Combine(folderPath, relativePath));
        return fullPath.StartsWith(fullFolderPath, StringComparison.OrdinalIgnoreCase) ? fullPath : null;
    }

    private static string SanitizeFolderName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "";

        StringBuilder result = new(value.Length);
        bool lastWasSeparator = false;
        foreach (char character in value.Trim())
        {
            bool safe = char.IsLetterOrDigit(character) || character == '-' || character == '_';
            char output = safe ? character : '_';
            if (output == '_')
            {
                if (lastWasSeparator)
                    continue;
                lastWasSeparator = true;
            }
            else
            {
                lastWasSeparator = false;
            }
            result.Append(output);
        }

        string sanitized = result.ToString().Trim('_', '-', '.', ' ');
        if (sanitized == "." || sanitized == "..")
            return "";
        return sanitized;
    }

    private void SetCreateError(string message)
    {
        createMessage = message;
        createMessageType = MessageType.Error;
    }

    private void ShowEditError(string message)
    {
        editMessage = message;
        editMessageType = MessageType.Error;
    }
}
