using System.Collections.Generic;
using System;
using UnityEngine;

public sealed class HitDetector : MonoBehaviour
{
    [Header("Hit Windows (milliseconds)")]
    [SerializeField, Min(0f)] private double perfectWindowMs = 45.0;
    [SerializeField, Min(0f)] private double greatWindowMs = 90.0;
    [SerializeField, Min(0f)] private double goodWindowMs = 140.0;
    [SerializeField, Min(0f)] private double missWindowMs = 160.0;

    [Header("References")]
    [SerializeField] private SongClock songClock;
    [SerializeField] private RhythmInputController inputController;
    [SerializeField] private HitFeedbackUI feedbackUI;
    [SerializeField] private ScoreManager scoreManager;
    [SerializeField] private SustainController sustainController;
    [SerializeField] private RockMeter rockMeter;

    [Header("Last Attempt Debug")]
    [SerializeField] private string lastLane = "None";
    [SerializeField] private double lastNoteHitTime;
    [SerializeField] private double lastActualSongTime;
    [SerializeField] private double lastTimingErrorMs;
    [SerializeField] private string lastResult = "None";

    private readonly List<ChartNoteEvent> activeEvents = new();

    public event Action<RhythmLane[], HitResult> NoteJudged;
    public event Action<RhythmLane[]> WrongStrum;

    private void Awake()
    {
        if (sustainController == null)
            sustainController = FindFirstObjectByType<SustainController>();
        if (sustainController == null)
        {
            GameObject sustainObject = new("SustainController");
            sustainController = sustainObject.AddComponent<SustainController>();
        }

        if (rockMeter == null)
            rockMeter = FindFirstObjectByType<RockMeter>();
        if (rockMeter == null)
        {
            GameObject rockObject = new("RockMeter");
            rockMeter = rockObject.AddComponent<RockMeter>();
        }
    }

    private void OnEnable()
    {
        if (inputController != null)
            inputController.Strummed += HandleStrum;
    }

    private void OnDisable()
    {
        if (inputController != null)
            inputController.Strummed -= HandleStrum;
    }

    private void Update()
    {
        if (songClock == null || ResultsController.GameplayFinished || PauseController.IsPaused || ChartRecorder.IsRecordingActive)
            return;

        double currentSongTime = songClock.SongTime;
        double missWindowSeconds = missWindowMs / 1000.0;

        for (int i = activeEvents.Count - 1; i >= 0; i--)
        {
            ChartNoteEvent noteEvent = activeEvents[i];
            if (noteEvent == null || !noteEvent.IsPending)
            {
                activeEvents.RemoveAt(i);
                continue;
            }

            if (currentSongTime > noteEvent.HitTime + missWindowSeconds)
            {
                RecordAttempt(FormatLanes(noteEvent.Lanes), noteEvent.HitTime, currentSongTime, "MISS");
                NoteJudged?.Invoke(noteEvent.Lanes, HitResult.Miss);
                scoreManager.RegisterResult(HitResult.Miss);
                noteEvent.ResolveMiss();
                activeEvents.RemoveAt(i);
                feedbackUI.Show("MISS", new Color(1f, 0.2f, 0.2f));
                rockMeter.RegisterResult(HitResult.Miss);
                if (ResultsController.GameplayFinished)
                    break;
            }
        }
    }

    public void Register(NoteController note)
    {
        if (note != null)
            Register(new ChartNoteEvent(new[] { note.Lane }, note.HitTime, note.Duration, new List<NoteController> { note }));
    }

    public void Register(ChartNoteEvent noteEvent)
    {
        if (noteEvent != null && !activeEvents.Contains(noteEvent))
            activeEvents.Add(noteEvent);
    }

    private void HandleStrum()
    {
        if (ResultsController.GameplayFinished || PauseController.IsPaused || ChartRecorder.IsRecordingActive)
            return;

        double currentSongTime = songClock.SongTime;

        RhythmLane[] heldLanes = inputController.GetHeldLanes();
        if (heldLanes.Length == 0)
        {
            RecordInvalidAttempt(currentSongTime, "MISS (hold required fret set)");
            WrongStrum?.Invoke(heldLanes);
            feedbackUI.Show("MISS", new Color(1f, 0.2f, 0.2f));
            return;
        }

        ChartNoteEvent closest = FindClosestPendingEvent(heldLanes, currentSongTime);
        if (closest == null)
        {
            RecordInvalidAttempt(currentSongTime, $"MISS ({FormatLanes(heldLanes)}: no exact event in window)");
            lastLane = FormatLanes(heldLanes);
            WrongStrum?.Invoke(heldLanes);
            feedbackUI.Show("MISS", new Color(1f, 0.2f, 0.2f));
            return;
        }

        double timingErrorSeconds = currentSongTime - closest.HitTime;
        double absoluteErrorMs = System.Math.Abs(timingErrorSeconds * 1000.0);
        HitResult result;
        Color feedbackColor;

        if (absoluteErrorMs <= perfectWindowMs)
        {
            result = HitResult.Perfect;
            feedbackColor = new Color(0.3f, 1f, 1f);
        }
        else if (absoluteErrorMs <= greatWindowMs)
        {
            result = HitResult.Great;
            feedbackColor = new Color(0.35f, 1f, 0.35f);
        }
        else if (absoluteErrorMs <= goodWindowMs)
        {
            result = HitResult.Good;
            feedbackColor = new Color(1f, 0.85f, 0.15f);
        }
        else
        {
            result = HitResult.Miss;
            feedbackColor = new Color(1f, 0.2f, 0.2f);
        }

        string resultText = result.ToString().ToUpperInvariant();
        RecordAttempt(FormatLanes(closest.Lanes), closest.HitTime, currentSongTime, resultText);
        NoteJudged?.Invoke(closest.Lanes, result);
        scoreManager.RegisterResult(result, closest.LaneCount);
        activeEvents.Remove(closest);

        if (result == HitResult.Miss)
            closest.ResolveMiss();
        else
            closest.ResolveHit(sustainController, currentSongTime);

        feedbackUI.Show(resultText, feedbackColor);
        rockMeter.RegisterResult(result);
    }

    private ChartNoteEvent FindClosestPendingEvent(RhythmLane[] heldLanes, double currentSongTime)
    {
        ChartNoteEvent closest = null;
        double closestAbsoluteError = double.MaxValue;
        double missWindowSeconds = missWindowMs / 1000.0;

        foreach (ChartNoteEvent noteEvent in activeEvents)
        {
            if (noteEvent == null || !noteEvent.IsPending || !LaneSetsEqual(noteEvent.Lanes, heldLanes))
                continue;

            double absoluteError = System.Math.Abs(currentSongTime - noteEvent.HitTime);
            if (absoluteError <= missWindowSeconds && absoluteError < closestAbsoluteError)
            {
                closest = noteEvent;
                closestAbsoluteError = absoluteError;
            }
        }

        return closest;
    }

    private static bool LaneSetsEqual(RhythmLane[] required, RhythmLane[] held)
    {
        if (required == null || held == null || required.Length != held.Length)
            return false;

        foreach (RhythmLane lane in required)
        {
            bool found = false;
            foreach (RhythmLane heldLane in held)
            {
                if (lane == heldLane)
                {
                    found = true;
                    break;
                }
            }
            if (!found)
                return false;
        }
        return true;
    }

    private static string FormatLanes(RhythmLane[] lanes)
    {
        return lanes == null || lanes.Length == 0 ? "None" : string.Join(" + ", lanes);
    }

    private void RecordAttempt(string lane, double hitTime, double actualTime, string result)
    {
        lastLane = lane;
        lastNoteHitTime = hitTime;
        lastActualSongTime = actualTime;
        lastTimingErrorMs = (actualTime - hitTime) * 1000.0;
        lastResult = result;

        Debug.Log(
            $"Hit attempt: lane={lastLane}, hitTime={hitTime:0.000}, " +
            $"songTime={actualTime:0.000}, error={lastTimingErrorMs:+0.0;-0.0;0.0} ms, result={result}");
    }

    private void RecordInvalidAttempt(double actualTime, string result)
    {
        lastLane = "None";
        lastNoteHitTime = -1.0;
        lastActualSongTime = actualTime;
        lastTimingErrorMs = 0.0;
        lastResult = result;
        Debug.Log($"Hit attempt: songTime={actualTime:0.000}, result={result}");
    }
}
