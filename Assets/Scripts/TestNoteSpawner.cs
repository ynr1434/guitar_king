using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class TestNoteSpawner : MonoBehaviour
{
    [System.Serializable]
    private sealed class LaneBinding
    {
        public RhythmLane lane;
        public Transform spawnPoint;
        public Transform hitPoint;
        public Color color;
    }

    private readonly struct ChartNote
    {
        public readonly RhythmLane Lane;
        public readonly int BeatIndex;
        public readonly int SubdivisionIndex;
        public readonly int SubdivisionCount;

        public ChartNote(
            RhythmLane lane,
            int beatIndex,
            int subdivisionIndex = 0,
            int subdivisionCount = 1)
        {
            Lane = lane;
            BeatIndex = beatIndex;
            SubdivisionIndex = subdivisionIndex;
            SubdivisionCount = subdivisionCount;
        }
    }

    private static readonly ChartNote[] TestChart =
    {
        new(RhythmLane.Green, 0),
        new(RhythmLane.Red, 1),
        new(RhythmLane.Yellow, 2),
        new(RhythmLane.Blue, 3),
        new(RhythmLane.Orange, 4),
        new(RhythmLane.Green, 5),
        new(RhythmLane.Red, 6),
        new(RhythmLane.Yellow, 7),
        new(RhythmLane.Blue, 8),
        new(RhythmLane.Orange, 9),
        new(RhythmLane.Green, 10),
        new(RhythmLane.Red, 11),
        new(RhythmLane.Yellow, 12),
        new(RhythmLane.Blue, 13),
        new(RhythmLane.Orange, 14),
        new(RhythmLane.Green, 15)
    };

    [SerializeField] private GameObject notePrefab;
    [SerializeField] private SongClock songClock;
    [SerializeField] private BeatGrid beatGrid;
    [SerializeField] private HitDetector hitDetector;
    [SerializeField] private ChartRecorder chartRecorder;
    [SerializeField] private LaneBinding[] lanes = new LaneBinding[5];

    private readonly List<NoteController> spawnedNotes = new();
    private Dictionary<RhythmLane, LaneBinding> laneMap;
    private Coroutine playbackRoutine;

    private void OnEnable()
    {
        if (chartRecorder != null)
        {
            chartRecorder.RecordingChanged += HandleRecordingChanged;
            chartRecorder.ChartLoaded += HandleChartLoaded;
        }
    }

    private void OnDisable()
    {
        if (chartRecorder != null)
        {
            chartRecorder.RecordingChanged -= HandleRecordingChanged;
            chartRecorder.ChartLoaded -= HandleChartLoaded;
        }
    }

    private void Start()
    {
        if (notePrefab == null || songClock == null || beatGrid == null || hitDetector == null || chartRecorder == null)
        {
            Debug.LogError("TestNoteSpawner is missing a required reference.", this);
            return;
        }

        laneMap = new Dictionary<RhythmLane, LaneBinding>();
        foreach (LaneBinding binding in lanes)
        {
            if (binding != null && binding.spawnPoint != null && binding.hitPoint != null)
                laneMap[binding.lane] = binding;
        }

        playbackRoutine = StartCoroutine(SpawnTestChart());
    }

    private IEnumerator SpawnTestChart()
    {
        foreach (ChartNote chartNote in TestChart)
        {
            double hitTime = beatGrid.GetSubdivisionTime(
                chartNote.BeatIndex,
                chartNote.SubdivisionIndex,
                chartNote.SubdivisionCount);
            yield return SpawnNoteAtTime(chartNote.Lane, hitTime, $"Beat{chartNote.BeatIndex}");
        }

        playbackRoutine = null;
    }

    private IEnumerator SpawnRecordedChart(IReadOnlyList<RecordedNote> notes)
    {
        foreach (RecordedNote note in notes)
            yield return SpawnNoteAtTime(note.lane, note.hitTime, $"Time{note.hitTime:0.000}");

        playbackRoutine = null;
    }

    private IEnumerator SpawnNoteAtTime(RhythmLane lane, double hitTime, string label)
    {
        if (!laneMap.TryGetValue(lane, out LaneBinding binding))
        {
            Debug.LogError($"TestNoteSpawner: no points configured for {lane}.", this);
            yield break;
        }

        NoteController prefabController = notePrefab.GetComponent<NoteController>();
        double spawnTime = hitTime - prefabController.TravelTime;
        while (songClock.SongTime < spawnTime)
            yield return null;

        GameObject noteObject = Instantiate(notePrefab, binding.spawnPoint.position, Quaternion.identity);
        noteObject.name = $"{lane}Note_{label}";
        NoteController noteController = noteObject.GetComponent<NoteController>();
        noteController.Initialize(songClock, binding.spawnPoint, binding.hitPoint, lane, hitTime, 0.0, binding.color);
        spawnedNotes.Add(noteController);
        hitDetector.Register(noteController);
    }

    private void HandleRecordingChanged(bool isRecording)
    {
        if (isRecording)
            CancelPlayback();
    }

    private void HandleChartLoaded(IReadOnlyList<RecordedNote> notes)
    {
        CancelPlayback();
        playbackRoutine = StartCoroutine(SpawnRecordedChart(notes));
    }

    private void CancelPlayback()
    {
        if (playbackRoutine != null)
        {
            StopCoroutine(playbackRoutine);
            playbackRoutine = null;
        }

        foreach (NoteController note in spawnedNotes)
        {
            if (note != null && note.State == NoteState.Pending)
                note.ResolveMiss();
        }
        spawnedNotes.Clear();
    }
}
