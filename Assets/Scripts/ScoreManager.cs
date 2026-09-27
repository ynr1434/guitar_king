using System;
using UnityEngine;

public sealed class ScoreManager : MonoBehaviour
{
    private const int BaseNoteScore = 50;

    [Header("Current Run")]
    [SerializeField] private int score;
    [SerializeField] private int combo;
    [SerializeField] private int maxCombo;
    [SerializeField] private int multiplier = 1;
    [SerializeField] private double accuracy;

    [Header("Result Counters")]
    [SerializeField] private int totalNotesJudged;
    [SerializeField] private int perfectCount;
    [SerializeField] private int greatCount;
    [SerializeField] private int goodCount;
    [SerializeField] private int missCount;

    private double accumulatedQuality;
    private double sustainScoreRemainder;

    public int Score => score;
    public int Combo => combo;
    public int MaxCombo => maxCombo;
    public int Multiplier => multiplier;
    public double Accuracy => accuracy;
    public int TotalNotesJudged => totalNotesJudged;
    public int PerfectCount => perfectCount;
    public int GreatCount => greatCount;
    public int GoodCount => goodCount;
    public int MissCount => missCount;

    public event Action ValuesChanged;

    public void RegisterResult(HitResult result, int laneCount = 1)
    {
        laneCount = Mathf.Max(1, laneCount);
        totalNotesJudged++;
        double qualityWeight;

        switch (result)
        {
            case HitResult.Perfect:
                perfectCount++;
                qualityWeight = 1.0;
                break;
            case HitResult.Great:
                greatCount++;
                qualityWeight = 0.8;
                break;
            case HitResult.Good:
                goodCount++;
                qualityWeight = 0.6;
                break;
            default:
                missCount++;
                qualityWeight = 0.0;
                break;
        }

        accumulatedQuality += qualityWeight;

        if (result == HitResult.Miss)
        {
            combo = 0;
            multiplier = 1;
        }
        else
        {
            combo++;
            maxCombo = Mathf.Max(maxCombo, combo);
            multiplier = CalculateMultiplier(combo);
            int qualityPoints = Mathf.RoundToInt(BaseNoteScore * laneCount * (float)qualityWeight);
            score += qualityPoints * multiplier;
        }

        accuracy = totalNotesJudged > 0
            ? accumulatedQuality / totalNotesJudged * 100.0
            : 0.0;

        ValuesChanged?.Invoke();
    }

    public void AddSustainScore(double heldSeconds, int activeMultiplier)
    {
        if (heldSeconds <= 0.0 || activeMultiplier <= 0)
            return;

        sustainScoreRemainder += heldSeconds * 25.0 * activeMultiplier;
        int wholePoints = (int)Math.Floor(sustainScoreRemainder);
        if (wholePoints <= 0)
            return;

        score += wholePoints;
        sustainScoreRemainder -= wholePoints;
        ValuesChanged?.Invoke();
    }

    private static int CalculateMultiplier(int currentCombo)
    {
        if (currentCombo >= 30)
            return 4;
        if (currentCombo >= 20)
            return 3;
        if (currentCombo >= 10)
            return 2;
        return 1;
    }
}
