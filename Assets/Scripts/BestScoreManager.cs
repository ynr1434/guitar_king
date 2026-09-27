using System;
using System.Globalization;
using UnityEngine;

public struct BestRecord
{
    public int Score;
    public int Stars;
    public double Accuracy;
    public int MaxCombo;
}

public struct BestRecordUpdate
{
    public BestRecord Record;
    public bool NewBestScore;
    public bool NewBestStars;
    public bool NewBestAccuracy;
    public bool NewBestCombo;

    public bool HasAnyNewBest => NewBestScore || NewBestStars || NewBestAccuracy || NewBestCombo;
}

public static class BestScoreManager
{
    // Versioned namespace for best records. Changing this namespace gives a
    // fresh first-launch record slate without deleting unrelated settings
    // (volume, resolution, video options, etc.) stored in PlayerPrefs.
    private const string KeyPrefix = "GuitarKing_Best_v2";

    public static BestRecord GetBest(string songFolder, SongDifficulty difficulty)
    {
        string baseKey = BuildBaseKey(songFolder, difficulty);
        string accuracyText = PlayerPrefs.GetString(baseKey + "_BestAccuracy", "0");
        double.TryParse(accuracyText, NumberStyles.Float, CultureInfo.InvariantCulture, out double accuracy);

        return new BestRecord
        {
            Score = PlayerPrefs.GetInt(baseKey + "_BestScore", 0),
            Stars = PlayerPrefs.GetInt(baseKey + "_BestStars", 0),
            Accuracy = accuracy,
            MaxCombo = PlayerPrefs.GetInt(baseKey + "_BestCombo", 0)
        };
    }

    public static BestRecordUpdate SaveSuccessfulRun(
        string songFolder,
        SongDifficulty difficulty,
        int score,
        int stars,
        double accuracy,
        int maxCombo)
    {
        BestRecord best = GetBest(songFolder, difficulty);
        string baseKey = BuildBaseKey(songFolder, difficulty);

        BestRecordUpdate update = new() { Record = best };
        if (score > best.Score)
        {
            best.Score = score;
            update.NewBestScore = true;
            PlayerPrefs.SetInt(baseKey + "_BestScore", score);
        }
        if (stars > best.Stars)
        {
            best.Stars = stars;
            update.NewBestStars = true;
            PlayerPrefs.SetInt(baseKey + "_BestStars", stars);
        }
        if (accuracy > best.Accuracy)
        {
            best.Accuracy = accuracy;
            update.NewBestAccuracy = true;
            PlayerPrefs.SetString(baseKey + "_BestAccuracy", accuracy.ToString("R", CultureInfo.InvariantCulture));
        }
        if (maxCombo > best.MaxCombo)
        {
            best.MaxCombo = maxCombo;
            update.NewBestCombo = true;
            PlayerPrefs.SetInt(baseKey + "_BestCombo", maxCombo);
        }

        update.Record = best;
        if (update.HasAnyNewBest)
            PlayerPrefs.Save();
        return update;
    }

    public static string FormatStars(int stars)
    {
        int clampedStars = Mathf.Clamp(stars, 0, 5);
        string activeStars = new string('★', clampedStars);
        string inactiveStars = new string('☆', 5 - clampedStars);
        return $"<color=#FFD44D>{activeStars}</color><color=#697386>{inactiveStars}</color>";
    }

    private static string BuildBaseKey(string songFolder, SongDifficulty difficulty)
    {
        string safeSongFolder = string.IsNullOrWhiteSpace(songFolder) ? "UnknownSong" : songFolder.Trim();
        return $"{KeyPrefix}_{safeSongFolder}_{difficulty}";
    }
}
