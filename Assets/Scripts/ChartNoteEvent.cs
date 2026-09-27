using System.Collections.Generic;

public sealed class ChartNoteEvent
{
    private readonly List<NoteController> heads;

    public RhythmLane[] Lanes { get; }
    public double HitTime { get; }
    public double Duration { get; }
    public int LaneCount => Lanes.Length;
    public IReadOnlyList<NoteController> Heads => heads;
    public bool IsPending { get; private set; } = true;
    public bool IsSingleSustain => LaneCount == 1 && Duration > 0.0;

    public ChartNoteEvent(RhythmLane[] lanes, double hitTime, double duration, List<NoteController> noteHeads)
    {
        Lanes = lanes;
        HitTime = hitTime;
        Duration = duration;
        heads = noteHeads;
    }

    public void ResolveHit(SustainController sustainController, double currentSongTime)
    {
        if (!IsPending)
            return;

        IsPending = false;
        if (Duration > 0.0)
        {
            sustainController.BeginSustain(this, currentSongTime);
            return;
        }

        foreach (NoteController head in heads)
        {
            if (head != null)
                head.ResolveHit();
        }
    }

    public void ResolveMiss()
    {
        if (!IsPending)
            return;

        IsPending = false;
        foreach (NoteController head in heads)
        {
            if (head != null)
                head.ResolveMiss();
        }
    }

    public void Cancel()
    {
        if (!IsPending)
            return;

        IsPending = false;
        foreach (NoteController head in heads)
        {
            if (head != null && head.State == NoteState.Pending)
                head.ResolveMiss();
        }
    }

    public void ForceCleanup()
    {
        IsPending = false;
        foreach (NoteController head in heads)
        {
            if (head != null)
                UnityEngine.Object.Destroy(head.gameObject);
        }
    }
}
