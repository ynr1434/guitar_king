using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

[RequireComponent(typeof(AudioSource))]
public sealed class ResultVoiceController : MonoBehaviour
{
    private const float VoiceDelaySeconds = 0.7f;
    private AudioSource voiceSource;
    private AudioSource musicSource;
    private float resultVoiceVolume = 1f;
    private float musicDuckVolume = 0.25f;
    private float originalMusicVolume;
    private bool musicIsDucked;

    private void Awake()
    {
        voiceSource = GetComponent<AudioSource>();
        voiceSource.playOnAwake = false;
        voiceSource.loop = false;
        voiceSource.spatialBlend = 0f;
        voiceSource.volume = 1f;
    }

    public void PlayForStars(int stars, bool allowOnFail = false)
    {
        if (stars < 1 || stars > 5 || (FailController.SongFailed && !allowOnFail))
            return;

        RestoreMusicVolume();
        StopAllCoroutines();
        voiceSource.Stop();
        StartCoroutine(LoadAndPlay(stars, allowOnFail));
    }

    public void Configure(AudioSource songSource, float voiceVolume, float duckVolume)
    {
        musicSource = songSource;
        resultVoiceVolume = Mathf.Clamp(voiceVolume, 0f, 8f);
        musicDuckVolume = Mathf.Clamp01(duckVolume);
    }

    private IEnumerator LoadAndPlay(int stars, bool allowOnFail)
    {
        yield return new WaitForSecondsRealtime(VoiceDelaySeconds);
        if (FailController.SongFailed && !allowOnFail)
            yield break;

        string voicePath = Path.Combine(Application.streamingAssetsPath, "ResultVoices", $"star_{stars}.wav");
        if (!File.Exists(voicePath))
            yield break;

        using UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip(
            new System.Uri(voicePath).AbsoluteUri,
            AudioType.WAV);
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogWarning($"Result voice could not be loaded: {voicePath} ({request.error})", this);
            yield break;
        }

        AudioClip clip = DownloadHandlerAudioClip.GetContent(request);
        if (clip != null && voiceSource != null && (!FailController.SongFailed || allowOnFail))
        {
            bool shouldDuckMusic = musicSource != null && musicSource.isPlaying;
            if (shouldDuckMusic)
            {
                originalMusicVolume = musicSource.volume;
                musicSource.volume = originalMusicVolume * musicDuckVolume;
                musicIsDucked = true;
            }

            voiceSource.PlayOneShot(clip, resultVoiceVolume * SettingsManager.Instance.ResultVoiceVolume);
            while (voiceSource != null && voiceSource.isPlaying)
                yield return null;

            RestoreMusicVolume();
        }
    }

    private void OnDisable()
    {
        RestoreMusicVolume();
    }

    private void RestoreMusicVolume()
    {
        if (!musicIsDucked)
            return;

        if (musicSource != null)
            musicSource.volume = originalMusicVolume;

        musicIsDucked = false;
    }
}
