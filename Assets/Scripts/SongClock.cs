using UnityEngine;

[DefaultExecutionOrder(-1000)]
public sealed class SongClock : MonoBehaviour
{
    [SerializeField] private SongAudioController songAudio;

    [Header("Debug (read only in Play Mode)")]
    [SerializeField] private double dspTime;
    [SerializeField] private double songStartDSPTime;
    [SerializeField, Tooltip("Current song time in seconds. Shown for debugging.")]
    private double songTime;
    [SerializeField] private float audioSourceTime;
    private bool isFrozen;
    private double frozenSongTime;
    private bool isPaused;
    private double pausedSongTime;
    private double pauseStartDSPTime;
    private double totalPausedDSPDuration;

    public bool IsPaused => isPaused;

    public double SongTime
    {
        get
        {
            if (isFrozen)
                return frozenSongTime;
            if (isPaused)
                return pausedSongTime;
            return songAudio != null && songAudio.IsScheduled
                ? AudioSettings.dspTime - songAudio.SongStartDSPTime - totalPausedDSPDuration
                : 0.0;
        }
    }

    public void PauseClock()
    {
        if (isFrozen || isPaused)
            return;

        pausedSongTime = SongTime;
        pauseStartDSPTime = AudioSettings.dspTime;
        isPaused = true;
    }

    public void ResumeClock()
    {
        if (!isPaused)
            return;

        totalPausedDSPDuration += System.Math.Max(0.0, AudioSettings.dspTime - pauseStartDSPTime);
        isPaused = false;
    }

    public void Freeze()
    {
        if (isFrozen)
            return;

        frozenSongTime = SongTime;
        isFrozen = true;
        isPaused = false;
    }

    private void Update()
    {
        dspTime = AudioSettings.dspTime;
        songStartDSPTime = songAudio != null ? songAudio.SongStartDSPTime : 0.0;
        songTime = SongTime;
        audioSourceTime = songAudio != null && songAudio.AudioSource != null
            ? songAudio.AudioSource.time
            : 0f;
    }
}
