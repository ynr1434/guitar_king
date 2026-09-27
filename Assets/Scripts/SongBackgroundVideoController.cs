using System;
using System.IO;
using UnityEngine;
using UnityEngine.Video;

[RequireComponent(typeof(VideoPlayer))]
public sealed class SongBackgroundVideoController : MonoBehaviour
{
    [SerializeField, Range(0f, 1f)] private float videoDarkness = .45f;
    [SerializeField, Min(.15f)] private float driftCorrectionInterval = .5f;
    [SerializeField, Min(.05f)] private double maximumVideoDrift = .2;

    private VideoPlayer player;
    private SongAudioController songAudio;
    private SongClock songClock;
    private RenderTexture targetTexture;
    private bool prepared, started, ended, stopped, paused;
    private double nextDriftCheck;

    public bool IsPrepared => prepared && !stopped;
    public RenderTexture TargetTexture => targetTexture;
    public float VideoDarkness => SettingsManager.Instance.VideoDarkness;

    private void Awake()
    {
        player = GetComponent<VideoPlayer>();
        targetTexture = new RenderTexture(1920, 1080, 0, RenderTextureFormat.ARGB32)
        {
            name = "Song background video",
            useMipMap = false,
            autoGenerateMips = false,
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };
        targetTexture.Create();

        player.playOnAwake = false;
        player.isLooping = false;
        player.waitForFirstFrame = true;
        player.skipOnDrop = true;
        player.renderMode = VideoRenderMode.RenderTexture;
        player.targetTexture = targetTexture;
        player.aspectRatio = VideoAspectRatio.FitOutside;
        player.audioOutputMode = VideoAudioOutputMode.None;
        player.sendFrameReadyEvents = false;
        player.prepareCompleted += OnPrepareCompleted;
        player.errorReceived += OnErrorReceived;
        player.loopPointReached += OnVideoEnded;
    }

    public void PrepareVideo(string songFolderPath, string songName, SongAudioController audio, SongClock clock)
    {
        songAudio = audio;
        songClock = clock;
        string videoPath = Path.Combine(songFolderPath, "background.mp4");
        if (!File.Exists(videoPath))
        {
            Destroy(gameObject);
            return;
        }

        player.source = VideoSource.Url;
        player.url = new Uri(videoPath).AbsoluteUri;
        player.Prepare();
    }

    private void OnPrepareCompleted(VideoPlayer source)
    {
        if (stopped) return;
        prepared = true;
        source.time = 0;
        // Decode one frame while paused so a still is ready during the countdown.
        source.Play();
        source.Pause();
        source.time = 0;
        ApplyBackground();
    }

    private void ApplyBackground()
    {
        GameplayVisualController visual = GameplayVisualController.Instance;
        if (visual == null) visual = FindFirstObjectByType<GameplayVisualController>();
        if (visual != null) visual.SetVideoBackground(targetTexture, SettingsManager.Instance.VideoDarkness);
    }

    private void Update()
    {
        if (!prepared || stopped || songAudio == null || songClock == null) return;

        if (FailController.SongFailed || ResultsController.GameplayFinished)
        {
            StopVideo();
            return;
        }

        bool isPaused = PauseController.IsPaused || songClock.IsPaused || songAudio.IsPaused;
        if (isPaused)
        {
            if (!paused && player.isPlaying) player.Pause();
            paused = true;
            return;
        }

        if (paused)
        {
            paused = false;
            if (started && !ended)
            {
                ApplyBackground();
                player.Play();
            }
        }

        // AudioSource.isPlaying is also false while paused. Check for a real
        // song ending only after every pause state has been handled above.
        if (started && songClock.SongTime >= 0 && songAudio.AudioSource != null &&
            !songAudio.AudioSource.isPlaying)
        {
            StopVideo();
            return;
        }

        // SongClock is the source of truth; video stays frozen until song time reaches zero.
        if (!started && songClock.SongTime >= 0 && songAudio.AudioSource != null && songAudio.AudioSource.isPlaying)
        {
            player.time = 0;
            player.Play();
            started = true;
            nextDriftCheck = Time.realtimeSinceStartupAsDouble + driftCorrectionInterval;
        }

        if (started && !ended && Time.realtimeSinceStartupAsDouble >= nextDriftCheck)
        {
            nextDriftCheck = Time.realtimeSinceStartupAsDouble + driftCorrectionInterval;
            double songTime = songClock.SongTime;
            double drift = songTime - player.time;
            if (songTime >= 0 && Math.Abs(drift) > maximumVideoDrift)
                player.time = songTime;
        }
    }

    private void OnVideoEnded(VideoPlayer source)
    {
        ended = true;
        // Keep the final decoded image on screen while the song continues.
        if (source.isPlaying) source.Pause();
    }

    private void OnErrorReceived(VideoPlayer source, string message)
    {
        if (stopped) return;
        SongPackageLoader loader = FindFirstObjectByType<SongPackageLoader>();
        string songName = loader != null ? loader.SongTitle : "selected song";
        Debug.LogWarning($"Background video unavailable for {songName}: {message}", this);
        StopVideo();
    }

    public void PauseVideo()
    {
        paused = true;
        if (player != null && player.isPlaying) player.Pause();
    }

    public void ResumeVideo()
    {
        if (stopped || !started || ended || player == null) return;
        paused = false;
        ApplyBackground();
        player.Play();
    }

    public void RestartVideo()
    {
        if (stopped || !prepared || player == null) return;
        ended = false;
        started = false;
        paused = false;
        player.Stop();
        player.time = 0;
        player.Play();
        player.Pause();
    }

    public void StopVideo()
    {
        if (stopped) return;
        stopped = true;
        if (player != null) player.Stop();
        prepared = false;
        GameplayVisualController visual = GameplayVisualController.Instance;
        if (visual != null) visual.SetVideoBackground(null, 0);
    }

    private void OnDestroy()
    {
        if (player != null)
        {
            player.prepareCompleted -= OnPrepareCompleted;
            player.errorReceived -= OnErrorReceived;
            player.loopPointReached -= OnVideoEnded;
            player.Stop();
            player.targetTexture = null;
        }
        if (targetTexture != null)
        {
            targetTexture.Release();
            Destroy(targetTexture);
        }
    }
}
