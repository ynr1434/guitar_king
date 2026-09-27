using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;

[Serializable]
public sealed class RecordedNote
{
    // `lane` is retained for compatibility with older single-lane chart code.
    public RhythmLane lane;
    public RhythmLane[] lanes;
    public double hitTime;
    public double duration;

    public RecordedNote(RhythmLane noteLane, double noteHitTime, double noteDuration = 0.0)
        : this(new[] { noteLane }, noteHitTime, noteDuration)
    {
    }

    public RecordedNote(RhythmLane[] noteLanes, double noteHitTime, double noteDuration = 0.0)
    {
        lanes = noteLanes ?? Array.Empty<RhythmLane>();
        lane = lanes.Length > 0 ? lanes[0] : default;
        hitTime = noteHitTime;
        duration = Math.Max(0.0, noteDuration);
    }

    public RhythmLane[] GetLanes()
    {
        return lanes != null && lanes.Length > 0 ? lanes : new[] { lane };
    }
}

[DefaultExecutionOrder(-100)]
public sealed class ChartRecorder : MonoBehaviour
{
    private sealed class LanePress
    {
        public RhythmLane Lane;
        public double PressTime;
        public double ReleaseTime;
        public bool IsHeld = true;
    }

    private sealed class PendingCapture
    {
        public double HitTime;
        public double GroupDeadline;
        public readonly List<LanePress> Presses = new();
    }

    private const double DuplicateToleranceSeconds = 0.02;

    [Header("Recording")]
    [SerializeField] private bool recording;
    [SerializeField] private bool snapToBeat;
    [SerializeField] private int snapSubdivision = 4;
    [SerializeField, Min(0f)] private float sustainThreshold = 0.35f;
    [SerializeField, Min(0f)] private float chordGroupWindow = 0.08f;
    [SerializeField, Range(2, 3)] private int maximumChordLanes = 3;

    [Header("References")]
    [SerializeField] private SongClock songClock;
    [SerializeField] private BeatGrid beatGrid;
    [SerializeField] private SongPackageLoader songPackageLoader;

    [Header("Recorded Data")]
    [SerializeField] private List<RecordedNote> recordedNotes = new();

    [Header("Debug")]
    [SerializeField] private double lastRecordedHitTime = -1.0;
    [SerializeField] private string lastStatus = "Ready";

    private static readonly Key[] RecordKeys =
    {
        Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4, Key.Digit5
    };

    private readonly List<PendingCapture> pendingCaptures = new();

    public static bool IsRecordingActive { get; private set; }
    public bool Recording => recording;
    public bool SnapToBeat => snapToBeat;
    public float SustainThreshold => sustainThreshold;
    public float ChordGroupWindow => chordGroupWindow;
    public int RecordedNoteCount => recordedNotes.Count;
    public double LastRecordedHitTime => lastRecordedHitTime;
    public string LastStatus => lastStatus;
    public string SavePath => songPackageLoader != null
        ? songPackageLoader.CurrentDifficultyChartPath
        : Path.Combine(Application.streamingAssetsPath, "Songs", "TestSong", GetDifficultyChartFileName());

    public event Action ValuesChanged;
    public event Action<bool> RecordingChanged;
    public event Action<IReadOnlyList<RecordedNote>> ChartLoaded;

    private void Awake()
    {
        recording = false;
        IsRecordingActive = false;
    }

    private void OnDisable()
    {
        if (recording)
            StopRecording(songClock != null ? songClock.SongTime : 0.0);
        else
            IsRecordingActive = false;
    }

    private void OnValidate()
    {
        if (snapSubdivision != 1 && snapSubdivision != 2 && snapSubdivision != 4)
            snapSubdivision = 4;
        sustainThreshold = Mathf.Max(0f, sustainThreshold);
        chordGroupWindow = Mathf.Max(0f, chordGroupWindow);
        maximumChordLanes = Mathf.Clamp(maximumChordLanes, 2, 3);
    }

    private void Update()
    {
        if (PauseController.IsPaused)
            return;

        if (ResultsController.GameplayFinished)
        {
            if (recording)
                StopRecording(songClock != null ? songClock.SongTime : 0.0);
            return;
        }

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
            return;

        if (keyboard.rKey.wasPressedThisFrame)
        {
            if (recording)
                StopRecording(CurrentSongTime());
            else
                SetRecording(true);
        }

        if (recording)
        {
            double currentTime = CurrentSongTime();
            for (int i = 0; i < RecordKeys.Length; i++)
            {
                if (keyboard[RecordKeys[i]].wasPressedThisFrame)
                    RecordPress((RhythmLane)i, currentTime);
                if (keyboard[RecordKeys[i]].wasReleasedThisFrame)
                    RecordRelease((RhythmLane)i, currentTime);
            }

            ProcessPendingCaptures(currentTime, false);
            return;
        }

        if (keyboard.cKey.wasPressedThisFrame)
            ClearChart();
        if (keyboard.pKey.wasPressedThisFrame)
            SaveChart();
        if (keyboard.oKey.wasPressedThisFrame)
            LoadChart();
    }

    private double CurrentSongTime()
    {
        return songClock != null ? songClock.SongTime : 0.0;
    }

    private void SetRecording(bool enabled)
    {
        if (enabled == recording)
            return;

        recording = enabled;
        IsRecordingActive = enabled;
        lastStatus = enabled ? "Recording" : "Recording stopped";
        RecordingChanged?.Invoke(enabled);
        ValuesChanged?.Invoke();
    }

    private void StopRecording(double currentTime)
    {
        foreach (PendingCapture capture in pendingCaptures)
        {
            foreach (LanePress press in capture.Presses)
            {
                if (press.IsHeld)
                {
                    press.IsHeld = false;
                    press.ReleaseTime = Math.Max(press.PressTime, currentTime);
                }
            }
        }

        ProcessPendingCaptures(currentTime, true);
        SetRecording(false);
    }

    private void RecordPress(RhythmLane lane, double currentTime)
    {
        PendingCapture capture = null;
        for (int i = pendingCaptures.Count - 1; i >= 0; i--)
        {
            PendingCapture candidate = pendingCaptures[i];
            if (currentTime > candidate.GroupDeadline)
                continue;

            bool laneAlreadyInGroup = candidate.Presses.Exists(press => press.Lane == lane);
            if (laneAlreadyInGroup)
                continue;

            if (candidate.Presses.Count >= maximumChordLanes)
            {
                lastStatus = $"Chord limit is {maximumChordLanes} lanes; extra press ignored";
                Debug.LogWarning(lastStatus, this);
                ValuesChanged?.Invoke();
                return;
            }

            capture = candidate;
            break;
        }

        if (capture == null)
        {
            capture = new PendingCapture
            {
                HitTime = currentTime,
                GroupDeadline = currentTime + chordGroupWindow
            };
            pendingCaptures.Add(capture);
        }

        capture.Presses.Add(new LanePress { Lane = lane, PressTime = currentTime });
    }

    private void RecordRelease(RhythmLane lane, double currentTime)
    {
        for (int i = pendingCaptures.Count - 1; i >= 0; i--)
        {
            PendingCapture capture = pendingCaptures[i];
            for (int j = capture.Presses.Count - 1; j >= 0; j--)
            {
                LanePress press = capture.Presses[j];
                if (press.Lane != lane || !press.IsHeld)
                    continue;

                press.IsHeld = false;
                press.ReleaseTime = Math.Max(press.PressTime, currentTime);
                return;
            }
        }
    }

    private void ProcessPendingCaptures(double currentTime, bool force)
    {
        for (int i = pendingCaptures.Count - 1; i >= 0; i--)
        {
            PendingCapture capture = pendingCaptures[i];
            if (!force && currentTime < capture.GroupDeadline)
                continue;

            bool isChord = capture.Presses.Count > 1;
            bool hasHeldPress = capture.Presses.Exists(press => press.IsHeld);
            bool hasReleasedPress = capture.Presses.Exists(press => !press.IsHeld);
            // Keep a held chord pending until its first release establishes the
            // common sustain end. A normal chord with no release is finalized
            // only when recording stops.
            if (!force && hasHeldPress && (!isChord || !hasReleasedPress))
                continue;

            AddCapturedEvent(capture, isChord);
            pendingCaptures.RemoveAt(i);
        }
    }

    private void AddCapturedEvent(PendingCapture capture, bool isChord)
    {
        RhythmLane[] lanes = new RhythmLane[capture.Presses.Count];
        for (int i = 0; i < capture.Presses.Count; i++)
            lanes[i] = capture.Presses[i].Lane;
        Array.Sort(lanes);

        double hitTime = SnapHitTime(capture.HitTime);
        double duration = 0.0;
        if (!isChord && capture.Presses.Count == 1)
        {
            LanePress press = capture.Presses[0];
            double heldDuration = Math.Max(0.0, press.ReleaseTime - press.PressTime);
            if (heldDuration >= sustainThreshold)
                duration = heldDuration;
        }
        else if (isChord)
        {
            // A chord sustain ends when the first required lane is released.
            // This produces one shared duration for the complete chord event.
            double firstRelease = double.MaxValue;
            foreach (LanePress press in capture.Presses)
            {
                if (!press.IsHeld)
                    firstRelease = Math.Min(firstRelease, press.ReleaseTime);
            }

            if (firstRelease < double.MaxValue)
            {
                double heldDuration = Math.Max(0.0, firstRelease - capture.HitTime);
                if (heldDuration >= sustainThreshold)
                    duration = heldDuration;
            }
        }

        if (IsDuplicate(lanes, hitTime, duration))
        {
            lastStatus = $"Duplicate ignored: {FormatLanes(lanes)}";
            ValuesChanged?.Invoke();
            return;
        }

        recordedNotes.Add(new RecordedNote(lanes, hitTime, duration));
        recordedNotes.Sort((left, right) => left.hitTime.CompareTo(right.hitTime));
        lastRecordedHitTime = hitTime;
        lastStatus = isChord
            ? FormatLanes(lanes)
            : duration > 0.0
                ? $"{lanes[0]} SUSTAIN {duration:0.0}s"
                : lanes[0].ToString().ToUpperInvariant();
        ValuesChanged?.Invoke();
    }

    private bool IsDuplicate(RhythmLane[] lanes, double hitTime, double duration)
    {
        foreach (RecordedNote existing in recordedNotes)
        {
            if (Math.Abs(existing.hitTime - hitTime) <= DuplicateToleranceSeconds &&
                Math.Abs(existing.duration - duration) <= DuplicateToleranceSeconds &&
                LaneSetsEqual(existing.GetLanes(), lanes))
                return true;
        }
        return false;
    }

    private static bool LaneSetsEqual(RhythmLane[] left, RhythmLane[] right)
    {
        if (left == null || right == null || left.Length != right.Length)
            return false;
        for (int i = 0; i < left.Length; i++)
            if (left[i] != right[i])
                return false;
        return true;
    }

    private static string FormatLanes(RhythmLane[] lanes)
    {
        string[] names = Array.ConvertAll(lanes, lane => lane.ToString().ToUpperInvariant());
        return string.Join(" + ", names);
    }

    private double SnapHitTime(double hitTime)
    {
        if (!snapToBeat)
            return hitTime;
        if (beatGrid == null)
        {
            Debug.LogWarning("Snap To Beat is enabled, but BeatGrid is not assigned. Recording exact Song Time.", this);
            return hitTime;
        }

        return snapSubdivision switch
        {
            1 => beatGrid.GetNearestBeatTime(hitTime),
            2 => beatGrid.GetNearestHalfBeatTime(hitTime),
            4 => beatGrid.GetNearestQuarterBeatTime(hitTime),
            _ => beatGrid.GetNearestQuarterBeatTime(hitTime)
        };
    }

    public void SetCurrentChartForRecording(IReadOnlyList<RecordedNote> notes)
    {
        if (recording)
            return;

        List<RecordedNote> copy = new();
        if (notes != null)
        {
            foreach (RecordedNote note in notes)
                if (note != null)
                    copy.Add(new RecordedNote(note.GetLanes(), note.hitTime, note.duration));
        }
        recordedNotes.Clear();
        recordedNotes.AddRange(copy);
        recordedNotes.Sort((left, right) => left.hitTime.CompareTo(right.hitTime));
        lastRecordedHitTime = recordedNotes.Count > 0 ? recordedNotes[^1].hitTime : -1.0;
        lastStatus = $"Chart ready: {recordedNotes.Count} events";
        ValuesChanged?.Invoke();
    }

    private void ClearChart()
    {
        recordedNotes.Clear();
        pendingCaptures.Clear();
        lastRecordedHitTime = -1.0;
        lastStatus = "Chart cleared";
        ValuesChanged?.Invoke();
    }

    private void SaveChart()
    {
        try
        {
            ChartData chart = ChartData.FromRecordedNotes(recordedNotes);
            string directory = Path.GetDirectoryName(SavePath);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            File.WriteAllText(SavePath, JsonUtility.ToJson(chart, true));
            lastStatus = $"Saved {recordedNotes.Count} events";
            Debug.Log($"Chart saved: {SavePath}");
        }
        catch (Exception exception)
        {
            lastStatus = "Chart save failed";
            Debug.LogError($"Could not save chart to {SavePath}\n{exception.Message}", this);
        }
        ValuesChanged?.Invoke();
    }

    private static string GetDifficultyChartFileName()
    {
        return GameSession.SelectedDifficulty switch
        {
            SongDifficulty.Normal => "chart_normal.json",
            SongDifficulty.Hard => "chart_hard.json",
            SongDifficulty.Expert => "chart_expert.json",
            _ => "chart_normal.json"
        };
    }

    private void LoadChart()
    {
        if (!File.Exists(SavePath))
        {
            lastStatus = "Chart file not found";
            Debug.LogWarning($"Chart file not found: {SavePath}");
            ValuesChanged?.Invoke();
            return;
        }

        try
        {
            ChartData chart = JsonUtility.FromJson<ChartData>(File.ReadAllText(SavePath));
            recordedNotes.Clear();
            if (chart != null)
                recordedNotes.AddRange(chart.ToRecordedNotes());
        }
        catch (Exception exception)
        {
            lastStatus = "Chart load failed";
            Debug.LogError($"Could not load chart from {SavePath}\n{exception.Message}", this);
            ValuesChanged?.Invoke();
            return;
        }
        lastRecordedHitTime = recordedNotes.Count > 0 ? recordedNotes[^1].hitTime : -1.0;
        lastStatus = $"Loaded {recordedNotes.Count} events";
        Debug.Log($"Chart loaded: {SavePath}");
        ChartLoaded?.Invoke(recordedNotes);
        ValuesChanged?.Invoke();
    }
}
