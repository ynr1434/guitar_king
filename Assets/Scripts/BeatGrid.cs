using UnityEngine;

[DefaultExecutionOrder(-900)]
public sealed class BeatGrid : MonoBehaviour
{
    [SerializeField] private SongClock songClock;
    [SerializeField] private SongAudioController songSettings;

    [Header("Debug (read only in Play Mode)")]
    [SerializeField] private double bpm;
    [SerializeField] private double secondsPerBeat;
    [SerializeField] private long currentBeat;
    [SerializeField] private double currentBeatFraction;
    [SerializeField] private double currentBeatPosition;
    [SerializeField] private double nearestBeatTime;

    public double BPM => songSettings.BPM;
    public double SecondsPerBeat => songSettings.SecondsPerBeat;
    public double SecondsPerHalfBeat => songSettings.SecondsPerHalfBeat;
    public double SecondsPerQuarterBeat => songSettings.SecondsPerQuarterBeat;
    public long CurrentBeat => currentBeat;
    public double CurrentBeatFraction => currentBeatFraction;
    public double CurrentBeatPosition => GetBeatPosition(songClock.SongTime);

    public double GetBeatTime(long beatIndex)
    {
        return songSettings.SongOffset + beatIndex * SecondsPerBeat;
    }

    public double GetSubdivisionTime(long beatIndex, int subdivisionIndex, int subdivisionCount)
    {
        if (subdivisionCount <= 0)
            throw new System.ArgumentOutOfRangeException(nameof(subdivisionCount));
        if (subdivisionIndex < 0 || subdivisionIndex >= subdivisionCount)
            throw new System.ArgumentOutOfRangeException(nameof(subdivisionIndex));

        double subdivision = (double)subdivisionIndex / subdivisionCount;
        return songSettings.SongOffset + (beatIndex + subdivision) * SecondsPerBeat;
    }

    public double GetNearestBeatTime(double songTime)
    {
        return GetNearestSubdivisionTime(songTime, 1);
    }

    public double GetNearestHalfBeatTime(double songTime)
    {
        return GetNearestSubdivisionTime(songTime, 2);
    }

    public double GetNearestQuarterBeatTime(double songTime)
    {
        return GetNearestSubdivisionTime(songTime, 4);
    }

    private void Update()
    {
        bpm = BPM;
        secondsPerBeat = SecondsPerBeat;
        currentBeatPosition = CurrentBeatPosition;
        currentBeat = (long)System.Math.Floor(currentBeatPosition);
        currentBeatFraction = currentBeatPosition - currentBeat;
        nearestBeatTime = GetNearestBeatTime(songClock.SongTime);
    }

    private double GetBeatPosition(double songTime)
    {
        return (songTime - songSettings.SongOffset) / SecondsPerBeat;
    }

    private double GetNearestSubdivisionTime(double songTime, int subdivisionsPerBeat)
    {
        double step = SecondsPerBeat / subdivisionsPerBeat;
        double gridPosition = (songTime - songSettings.SongOffset) / step;
        double nearestIndex = System.Math.Round(gridPosition, System.MidpointRounding.AwayFromZero);
        return songSettings.SongOffset + nearestIndex * step;
    }
}
