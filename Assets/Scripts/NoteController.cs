using UnityEngine;

public sealed class NoteController : MonoBehaviour
{
    [SerializeField, Min(0f)] private double hitTime = 3.0;
    [SerializeField, Min(0.01f)] private double travelTime = 2.0;
    [SerializeField, Min(0f)] private double duration;
    [SerializeField, Min(0f)] private float postHitTravel = 0.35f;
    [SerializeField, Min(0.1f)] private float sustainTailWidth = 0.68f;

    private SongClock songClock;
    private Transform spawnPoint;
    private Transform hitPoint;
    private Renderer noteRenderer;
    private GameObject sustainTail;
    private Renderer sustainTailRenderer;
    private Color laneColor = Color.white;

    public NoteState State { get; private set; } = NoteState.Pending;
    public SustainState SustainState { get; private set; } = SustainState.None;
    public RhythmLane Lane { get; private set; }
    public bool IsSustain => duration > 0.0;
    public bool WasHeadHit { get; private set; }
    public bool IsSustainActive => SustainState == SustainState.Holding;
    public double HitTime => hitTime;
    public double Duration => duration;
    public double SustainEndTime => hitTime + duration;

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

    public double TravelTime => travelTime;

    public void Initialize(
        SongClock clock,
        Transform noteSpawnPoint,
        Transform noteHitPoint,
        RhythmLane noteLane,
        double noteHitTime,
        double noteDuration,
        Color noteColor)
    {
        songClock = clock;
        spawnPoint = noteSpawnPoint;
        hitPoint = noteHitPoint;
        Lane = noteLane;
        hitTime = noteHitTime;
        duration = System.Math.Max(0.0, noteDuration);
        laneColor = noteColor;
        State = NoteState.Pending;
        SustainState = IsSustain ? SustainState.WaitingForHead : SustainState.None;
        WasHeadHit = false;

        noteRenderer = GetComponent<Renderer>();
        MaterialPropertyBlock properties = new();
        noteRenderer.GetPropertyBlock(properties);
        properties.SetColor(BaseColorId, noteColor);
        properties.SetColor(EmissionColorId, noteColor * 0.45f);
        noteRenderer.SetPropertyBlock(properties);

        if (IsSustain)
            CreateSustainTail(noteColor);

        UpdatePosition();
        GameplayVisualController.DecorateNote(this);
    }

    public void ResolveHit()
    {
        if (State != NoteState.Pending)
            return;

        State = NoteState.Hit;
        WasHeadHit = true;
        if (!IsSustain)
        {
            gameObject.SetActive(false);
            Destroy(gameObject);
        }
    }

    public void BeginSustain(double currentSongTime)
    {
        if (!IsSustain || State != NoteState.Pending)
            return;

        State = NoteState.Hit;
        WasHeadHit = true;
        SustainState = SustainState.Holding;
    }

    public void CompleteSustain()
    {
        if (!IsSustainActive)
            return;

        SustainState = SustainState.Completed;
        gameObject.SetActive(false);
        Destroy(gameObject);
    }

    public void BreakSustain()
    {
        if (!IsSustainActive)
            return;

        SustainState = SustainState.Broken;
        gameObject.SetActive(false);
        Destroy(gameObject);
    }

    public void ResolveMiss()
    {
        if (State != NoteState.Pending)
            return;

        State = NoteState.Missed;
        SustainState = IsSustain ? SustainState.Missed : SustainState.None;
        gameObject.SetActive(false);
        Destroy(gameObject);
    }

    private void Update()
    {
        if (songClock == null || spawnPoint == null || hitPoint == null)
            return;

        UpdatePosition();
    }

    private void UpdatePosition()
    {
        if (IsSustainActive)
        {
            transform.position = hitPoint.position;
            UpdateTail(Mathf.Max(0f, (float)(SustainEndTime - songClock.SongTime)));
            return;
        }

        double spawnTime = hitTime - travelTime;
        float progress = (float)((songClock.SongTime - spawnTime) / travelTime);
        transform.position = Vector3.LerpUnclamped(spawnPoint.position, hitPoint.position, progress);
        if (IsSustain)
            UpdateTail((float)duration);

        if (progress >= 1f + postHitTravel && State == NoteState.Pending)
            ResolveMiss();
    }

    private void CreateSustainTail(Color color)
    {
        sustainTail = GameObject.CreatePrimitive(PrimitiveType.Cube);
        sustainTail.name = "SustainTail";
        sustainTail.transform.SetParent(transform, true);
        Collider tailCollider = sustainTail.GetComponent<Collider>();
        if (tailCollider != null)
            Destroy(tailCollider);

        sustainTailRenderer = sustainTail.GetComponent<Renderer>();
        if (noteRenderer != null)
            sustainTailRenderer.sharedMaterial = noteRenderer.sharedMaterial;

        MaterialPropertyBlock properties = new();
        sustainTailRenderer.GetPropertyBlock(properties);
        properties.SetColor(BaseColorId, color * 0.72f);
        properties.SetColor(EmissionColorId, color * 0.35f);
        sustainTailRenderer.SetPropertyBlock(properties);
        laneColor = color;
    }

    private void UpdateTail(float remainingDuration)
    {
        if (sustainTail == null || sustainTailRenderer == null || spawnPoint == null || hitPoint == null)
            return;

        Vector3 direction = (spawnPoint.position - hitPoint.position).normalized;
        float laneLength = Vector3.Distance(spawnPoint.position, hitPoint.position);
        float tailLength = laneLength * remainingDuration / (float)travelTime;
        if (!IsSustainActive)
            tailLength = laneLength * (float)duration / (float)travelTime;

        sustainTail.transform.position = transform.position + direction * (tailLength * 0.5f);
        sustainTail.transform.rotation = Quaternion.LookRotation(direction);
        Vector3 parentScale = transform.lossyScale;
        sustainTail.transform.localScale = new Vector3(
            sustainTailWidth / Mathf.Max(0.001f, parentScale.x),
            0.11f / Mathf.Max(0.001f, parentScale.y),
            Mathf.Max(0.01f, tailLength) / Mathf.Max(0.001f, parentScale.z));
    }
}

public enum NoteState
{
    Pending,
    Hit,
    Missed
}

public enum SustainState
{
    None,
    WaitingForHead,
    Holding,
    Completed,
    Broken,
    Missed
}
