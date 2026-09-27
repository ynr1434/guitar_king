using System.Collections.Generic;
using System;
using UnityEngine;

public sealed class SustainController : MonoBehaviour
{
    [SerializeField] private RhythmInputController inputController;
    [SerializeField] private SongClock songClock;
    [SerializeField] private ScoreManager scoreManager;

    private sealed class ActiveSustain
    {
        public ChartNoteEvent Event;
        public double LastScoredSongTime;
    }

    private readonly List<ActiveSustain> activeSustains = new();

    public event Action<RhythmLane, bool> SustainStateChanged;

    private void Awake()
    {
        if (inputController == null)
            inputController = FindFirstObjectByType<RhythmInputController>();
        if (songClock == null)
            songClock = FindFirstObjectByType<SongClock>();
        if (scoreManager == null)
            scoreManager = FindFirstObjectByType<ScoreManager>();
    }

    private void Update()
    {
        if (songClock == null || inputController == null || scoreManager == null ||
            ResultsController.GameplayFinished || PauseController.IsPaused)
            return;

        double now = songClock.SongTime;
        for (int i = activeSustains.Count - 1; i >= 0; i--)
        {
            ActiveSustain active = activeSustains[i];
            ChartNoteEvent noteEvent = active.Event;
            if (noteEvent == null)
            {
                activeSustains.RemoveAt(i);
                continue;
            }

            if (!ChartRecorder.IsRecordingActive && !AllRequiredLanesHeld(noteEvent))
            {
                BreakEvent(active);
                activeSustains.RemoveAt(i);
                continue;
            }

            double scoredUntil = Math.Min(now, noteEvent.HitTime + noteEvent.Duration);
            if (!ChartRecorder.IsRecordingActive)
                scoreManager.AddSustainScore(Math.Max(0.0, scoredUntil - active.LastScoredSongTime), scoreManager.Multiplier);
            active.LastScoredSongTime = scoredUntil;

            if (now >= noteEvent.HitTime + noteEvent.Duration)
            {
                CompleteEvent(active);
                activeSustains.RemoveAt(i);
            }
        }
    }

    public void BeginSustain(ChartNoteEvent noteEvent, double currentSongTime)
    {
        if (noteEvent == null || noteEvent.Duration <= 0.0 || inputController == null || !AllRequiredLanesHeld(noteEvent))
            return;

        foreach (NoteController head in noteEvent.Heads)
            if (head != null) head.BeginSustain(currentSongTime);

        activeSustains.Add(new ActiveSustain { Event = noteEvent, LastScoredSongTime = currentSongTime });
        SetVisualState(noteEvent, true);
    }

    public void BeginSustain(NoteController note, double currentSongTime)
    {
        if (note == null)
            return;
        BeginSustain(new ChartNoteEvent(new[] { note.Lane }, note.HitTime, note.Duration,
            new List<NoteController> { note }), currentSongTime);
    }

    private bool AllRequiredLanesHeld(ChartNoteEvent noteEvent)
    {
        foreach (RhythmLane lane in noteEvent.Lanes)
            if (!inputController.IsLaneHeld(lane)) return false;
        return true;
    }

    private void CompleteEvent(ActiveSustain active)
    {
        foreach (NoteController head in active.Event.Heads)
            if (head != null && head.IsSustainActive) head.CompleteSustain();
        SetVisualState(active.Event, false);
    }

    private void BreakEvent(ActiveSustain active)
    {
        foreach (NoteController head in active.Event.Heads)
            if (head != null && head.IsSustainActive) head.BreakSustain();
        SetVisualState(active.Event, false);
    }

    private void SetVisualState(ChartNoteEvent noteEvent, bool active)
    {
        foreach (RhythmLane lane in noteEvent.Lanes)
            SustainStateChanged?.Invoke(lane, active);
    }
}
