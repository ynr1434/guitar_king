using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public sealed class ChartPlayer : MonoBehaviour
{
    [System.Serializable]
    private sealed class LaneBinding
    {
        public RhythmLane lane;
        public Transform spawnPoint;
        public Transform hitPoint;
        public Color color;
    }

    [SerializeField] private GameObject notePrefab;
    [SerializeField] private SongClock songClock;
    [SerializeField] private HitDetector hitDetector;
    [SerializeField] private ChartRecorder chartRecorder;
    [SerializeField] private LaneBinding[] lanes = new LaneBinding[5];
#if UNITY_EDITOR
    [Header("Editor Debug Only")]
    [SerializeField] private bool useSustainDebugChart;
    [SerializeField] private bool useChordDebugChart;
#endif

    private readonly List<ChartNoteEvent> spawnedEvents = new();
    private readonly Dictionary<RhythmLane, LaneBinding> laneMap = new();
    private Coroutine playbackRoutine;

    public double NoteTravelTime => notePrefab != null
        ? notePrefab.GetComponent<NoteController>().TravelTime : 2.0;

    private void Awake()
    {
        laneMap.Clear();
        foreach (LaneBinding binding in lanes)
        {
            if (binding != null && binding.spawnPoint != null && binding.hitPoint != null)
                laneMap[binding.lane] = binding;
        }
    }

    private void OnEnable()
    {
        if (chartRecorder != null)
        {
            chartRecorder.RecordingChanged += HandleRecordingChanged;
            chartRecorder.ChartLoaded += PlayChart;
        }
    }

    private void OnDisable()
    {
        if (chartRecorder != null)
        {
            chartRecorder.RecordingChanged -= HandleRecordingChanged;
            chartRecorder.ChartLoaded -= PlayChart;
        }
    }

    public void PlayChart(IReadOnlyList<RecordedNote> notes)
    {
        CancelPlayback();
        if (chartRecorder != null)
            chartRecorder.SetCurrentChartForRecording(notes);
#if UNITY_EDITOR
        if (useChordDebugChart)
        {
            if (useSustainDebugChart)
                Debug.LogWarning("ChartPlayer: both debug charts were enabled; Chord Debug Chart takes priority.", this);
            notes = LoadDebugChart("chord_test_chart.json");
        }
        else if (useSustainDebugChart)
        {
            notes = LoadDebugChart("sustain_test_chart.json");
        }

        if (notes == null)
            return;
#endif
        playbackRoutine = StartCoroutine(SpawnChart(notes));
    }

    public void StopForFail()
    {
        if (playbackRoutine != null)
        {
            StopCoroutine(playbackRoutine);
            playbackRoutine = null;
        }

        foreach (ChartNoteEvent noteEvent in spawnedEvents)
        {
            if (noteEvent != null)
                noteEvent.ForceCleanup();
        }
        spawnedEvents.Clear();
    }

#if UNITY_EDITOR
    private IReadOnlyList<RecordedNote> LoadDebugChart(string fileName)
    {
        string debugPath = Path.Combine(Application.streamingAssetsPath, "Songs", "TestSong", fileName);
        if (!File.Exists(debugPath))
        {
            Debug.LogError($"ChartPlayer: debug chart not found at {debugPath}.", this);
            return null;
        }

        try
        {
            ChartData debugChart = JsonUtility.FromJson<ChartData>(File.ReadAllText(debugPath));
            if (debugChart == null)
            {
                Debug.LogError($"ChartPlayer: debug chart JSON is invalid ({fileName}).", this);
                return null;
            }
            return debugChart.ToRecordedNotes();
        }
        catch (System.Exception exception)
        {
            Debug.LogError($"ChartPlayer: couldn't load debug chart {debugPath}: {exception.Message}", this);
            return null;
        }
    }
#endif

    private IEnumerator SpawnChart(IReadOnlyList<RecordedNote> notes)
    {
        foreach (RecordedNote note in notes)
            yield return SpawnNoteAtTime(note);

        playbackRoutine = null;
    }

    private IEnumerator SpawnNoteAtTime(RecordedNote recordedNote)
    {
        RhythmLane[] eventLanes = recordedNote.GetLanes();
        if (eventLanes.Length == 0)
        {
            Debug.LogError("ChartPlayer: chart event has no lanes.", this);
            yield break;
        }

        NoteController prefabController = notePrefab.GetComponent<NoteController>();
        double spawnTime = recordedNote.hitTime - prefabController.TravelTime;
        while (PauseController.IsPaused || songClock.SongTime < spawnTime)
            yield return null;

        double eventDuration = recordedNote.duration;

        List<NoteController> heads = new(eventLanes.Length);
        foreach (RhythmLane lane in eventLanes)
        {
            if (!laneMap.TryGetValue(lane, out LaneBinding binding))
            {
                Debug.LogError($"ChartPlayer: no points configured for {lane}.", this);
                foreach (NoteController head in heads)
                    if (head != null) Destroy(head.gameObject);
                yield break;
            }

            GameObject noteObject = Instantiate(notePrefab, binding.spawnPoint.position, Quaternion.identity);
            noteObject.name = $"{lane}Note_{recordedNote.hitTime:0.000}";
            NoteController noteController = noteObject.GetComponent<NoteController>();
            noteController.Initialize(
                songClock,
                binding.spawnPoint,
                binding.hitPoint,
                lane,
                recordedNote.hitTime,
                eventDuration,
                binding.color);
            heads.Add(noteController);
        }

        ChartNoteEvent noteEvent = new(eventLanes, recordedNote.hitTime, eventDuration, heads);
        spawnedEvents.Add(noteEvent);
        hitDetector.Register(noteEvent);
    }

    private void HandleRecordingChanged(bool isRecording)
    {
        if (isRecording)
            CancelPlayback();
    }

    private void CancelPlayback()
    {
        if (playbackRoutine != null)
        {
            StopCoroutine(playbackRoutine);
            playbackRoutine = null;
        }

        foreach (ChartNoteEvent noteEvent in spawnedEvents)
        {
            if (noteEvent != null)
                noteEvent.Cancel();
        }
        spawnedEvents.Clear();
    }
}
