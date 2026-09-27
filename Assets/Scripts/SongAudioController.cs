using UnityEngine;

[RequireComponent(typeof(AudioSource))]
[DefaultExecutionOrder(-1100)]
public sealed class SongAudioController : MonoBehaviour
{
    [SerializeField] private AudioClip songClip;
    [SerializeField, Min(0f), Tooltip("Ready countdown before notes may enter the highway.")] private double startDelay = 3.0;
    [SerializeField, Min(1f)] private double bpm = 120.0;
    [SerializeField] private double songOffset;

    private AudioSource audioSource;
    private bool playbackPaused;
    private bool pausedBeforeScheduledStart;
    private double remainingScheduledDelay;

    public double SongStartDSPTime { get; private set; }
    public double NotePreRollSeconds { get; private set; }
    public double BPM => bpm;
    public double SongOffset => songOffset;
    public double SecondsPerBeat => 60.0 / bpm;
    public double SecondsPerHalfBeat => SecondsPerBeat / 2.0;
    public double SecondsPerQuarterBeat => SecondsPerBeat / 4.0;
    public AudioSource AudioSource => audioSource;
    public bool IsScheduled { get; private set; }
    public bool IsPaused => playbackPaused;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = 0f;
        ApplyVolume();
        SettingsManager.Instance.Changed += ApplyVolume;
    }

    private void ApplyVolume() { if (audioSource != null) audioSource.volume = 0.6f * SettingsManager.Instance.MusicVolume; }
    private void OnDestroy() { if (SettingsManager.HasInstance) SettingsManager.Instance.Changed -= ApplyVolume; }

    public void ConfigureAndSchedule(AudioClip clip, double loadedBpm, double loadedOffset, double notePreRollSeconds = 0.0)
    {
        if (clip == null)
        {
            Debug.LogError("SongAudioController cannot schedule a null AudioClip.", this);
            return;
        }

        audioSource.Stop();
        songClip = clip;
        bpm = System.Math.Max(1.0, loadedBpm);
        songOffset = loadedOffset;
        NotePreRollSeconds = System.Math.Max(0.0, notePreRollSeconds);
        // Reserve the full ready countdown, then the normal note approach before song zero.
        SongStartDSPTime = AudioSettings.dspTime + startDelay + NotePreRollSeconds;
        IsScheduled = true;

        audioSource.clip = songClip;
        audioSource.PlayScheduled(SongStartDSPTime);
    }

    public void StopPlayback()
    {
        if (audioSource != null)
            audioSource.Stop();
        IsScheduled = false;
        playbackPaused = false;
        pausedBeforeScheduledStart = false;
    }

    public void PausePlayback()
    {
        if (audioSource == null || playbackPaused || !IsScheduled)
            return;

        playbackPaused = true;
        pausedBeforeScheduledStart = AudioSettings.dspTime < SongStartDSPTime;
        if (pausedBeforeScheduledStart)
        {
            remainingScheduledDelay = System.Math.Max(0.0, SongStartDSPTime - AudioSettings.dspTime);
            audioSource.Stop();
        }
        else
        {
            audioSource.Pause();
        }
    }

    public void ResumePlayback()
    {
        if (audioSource == null || !playbackPaused)
            return;

        playbackPaused = false;
        if (pausedBeforeScheduledStart)
        {
            audioSource.PlayScheduled(AudioSettings.dspTime + remainingScheduledDelay);
            pausedBeforeScheduledStart = false;
        }
        else
        {
            audioSource.UnPause();
        }
    }
}
