using System;
using System.Collections.Generic;

[Serializable]
public sealed class ChartNoteData
{
    // `lane` remains for charts authored before chord support.
    public string lane;
    public string[] lanes;
    public double hitTime;
    public double duration;
}

[Serializable]
public sealed class ChartData
{
    public List<ChartNoteData> notes = new();

    public static ChartData FromRecordedNotes(IReadOnlyList<RecordedNote> recordedNotes)
    {
        ChartData chart = new();
        foreach (RecordedNote note in recordedNotes)
        {
            chart.notes.Add(new ChartNoteData
            {
                lanes = Array.ConvertAll(note.GetLanes(), lane => lane.ToString()),
                hitTime = note.hitTime,
                duration = note.duration
            });
        }
        return chart;
    }

    public List<RecordedNote> ToRecordedNotes()
    {
        List<RecordedNote> result = new();
        if (notes == null)
            return result;

        foreach (ChartNoteData note in notes)
        {
            List<RhythmLane> parsedLanes = new();
            if (note.lanes != null && note.lanes.Length > 0)
            {
                foreach (string laneName in note.lanes)
                {
                    if (Enum.TryParse(laneName, true, out RhythmLane parsedLane) && !parsedLanes.Contains(parsedLane))
                        parsedLanes.Add(parsedLane);
                }
            }
            else if (Enum.TryParse(note.lane, true, out RhythmLane legacyLane))
            {
                parsedLanes.Add(legacyLane);
            }

            if (parsedLanes.Count > 0)
                result.Add(new RecordedNote(parsedLanes.ToArray(), note.hitTime, note.duration));
        }

        result.Sort((left, right) => left.hitTime.CompareTo(right.hitTime));
        return result;
    }
}
