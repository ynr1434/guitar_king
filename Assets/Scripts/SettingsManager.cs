using System;
using System.Collections.Generic;
using UnityEngine;

public enum VfxIntensity { Low, Normal, High }

[DefaultExecutionOrder(-5000)]
public sealed class SettingsManager : MonoBehaviour
{
    private const string Prefix = "GK_Settings_";
    private static SettingsManager instance;
    public static bool HasInstance => instance != null;
    public static SettingsManager Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject go = new("SettingsManager");
                instance = go.AddComponent<SettingsManager>();
            }
            return instance;
        }
    }

    public float MasterVolume { get; private set; } = 1f;
    public float MusicVolume { get; private set; } = 1f;
    public float ResultVoiceVolume { get; private set; } = 1f;
    public float HighwayOpacity { get; private set; } = .70f;
    public float VideoDarkness { get; private set; } = .45f;
    public VfxIntensity VfxIntensity { get; private set; } = VfxIntensity.Normal;
    public bool ScreenShake { get; private set; } = true;
    public bool Fullscreen { get; private set; }
    public int ResolutionWidth { get; private set; }
    public int ResolutionHeight { get; private set; }
    public float VfxScale => VfxIntensity == VfxIntensity.Low ? .62f : VfxIntensity == VfxIntensity.High ? 1.35f : 1f;
    public event Action Changed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap() { _ = Instance; }

    private void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this; DontDestroyOnLoad(gameObject); Load(); ApplyDisplay(); ApplyMaster();
    }

    private void Load()
    {
        MasterVolume = GetFloat("MasterVolume", 1f, 0, 1);
        MusicVolume = GetFloat("MusicVolume", 1f, 0, 1);
        ResultVoiceVolume = GetFloat("ResultVoiceVolume", 1f, 0, 1);
        HighwayOpacity = GetFloat("HighwayOpacity", .70f, .4f, 1f);
        VideoDarkness = GetFloat("VideoDarkness", .45f, 0, .8f);
        VfxIntensity = (VfxIntensity)Mathf.Clamp(PlayerPrefs.GetInt(Prefix+"VFXIntensity", 1), 0, 2);
        ScreenShake = PlayerPrefs.GetInt(Prefix+"ScreenShake", 1) != 0;
        Fullscreen = PlayerPrefs.GetInt(Prefix+"Fullscreen", Screen.fullScreen ? 1 : 0) != 0;
        ResolutionWidth = Mathf.Max(640, PlayerPrefs.GetInt(Prefix+"ResolutionWidth", Screen.width));
        ResolutionHeight = Mathf.Max(360, PlayerPrefs.GetInt(Prefix+"ResolutionHeight", Screen.height));
    }

    public void SetMaster(float v) { MasterVolume=Mathf.Clamp01(v); SaveFloat("MasterVolume",MasterVolume); ApplyMaster(); Notify(); }
    public void SetMusic(float v) { MusicVolume=Mathf.Clamp01(v); SaveFloat("MusicVolume",MusicVolume); Notify(); }
    public void SetResultVoice(float v) { ResultVoiceVolume=Mathf.Clamp01(v); SaveFloat("ResultVoiceVolume",ResultVoiceVolume); Notify(); }
    public void SetHighwayOpacity(float v) { HighwayOpacity=Mathf.Clamp(v,.4f,1f); SaveFloat("HighwayOpacity",HighwayOpacity); Notify(); }
    public void SetVideoDarkness(float v) { VideoDarkness=Mathf.Clamp(v,0,.8f); SaveFloat("VideoDarkness",VideoDarkness); Notify(); }
    public void SetVfxIntensity(int v) { VfxIntensity=(VfxIntensity)Mathf.Clamp(v,0,2); PlayerPrefs.SetInt(Prefix+"VFXIntensity",(int)VfxIntensity); SaveAndNotify(); }
    public void SetScreenShake(bool v) { ScreenShake=v; PlayerPrefs.SetInt(Prefix+"ScreenShake",v?1:0); SaveAndNotify(); }
    public void SetFullscreen(bool v) { Fullscreen=v; PlayerPrefs.SetInt(Prefix+"Fullscreen",v?1:0); Screen.fullScreenMode=v?FullScreenMode.FullScreenWindow:FullScreenMode.Windowed; Screen.fullScreen=v; SaveAndNotify(); }
    public void SetResolution(int width,int height) { ResolutionWidth=width;ResolutionHeight=height;PlayerPrefs.SetInt(Prefix+"ResolutionWidth",width);PlayerPrefs.SetInt(Prefix+"ResolutionHeight",height);Screen.SetResolution(width,height,Fullscreen?FullScreenMode.FullScreenWindow:FullScreenMode.Windowed);SaveAndNotify(); }

    public void ResetDefaults()
    {
        MasterVolume=MusicVolume=ResultVoiceVolume=1f; HighwayOpacity=.70f; VideoDarkness=.45f;
        VfxIntensity=VfxIntensity.Normal; ScreenShake=true;
        SaveFloat("MasterVolume",1);SaveFloat("MusicVolume",1);SaveFloat("ResultVoiceVolume",1);SaveFloat("HighwayOpacity",.7f);SaveFloat("VideoDarkness",.45f);
        PlayerPrefs.SetInt(Prefix+"VFXIntensity",1);PlayerPrefs.SetInt(Prefix+"ScreenShake",1);
        ApplyMaster(); SaveAndNotify();
    }

    public static List<Vector2Int> GetResolutions()
    {
        List<Vector2Int> list=new(); HashSet<string> seen=new();
        foreach(Resolution r in Screen.resolutions) if(seen.Add(r.width+"x"+r.height)) list.Add(new(r.width,r.height));
        // Some editor/player configurations expose only the current desktop mode.
        // Keep that real mode and add safe common choices so the dropdown remains useful.
        Vector2Int[] safeModes = { new(1280,720), new(1600,900), new(1920,1080), new(2560,1440) };
        if(list.Count < 2)
            foreach(Vector2Int mode in safeModes)
                if(seen.Add(mode.x+"x"+mode.y)) list.Add(mode);
        return list;
    }
    private void ApplyMaster() => AudioListener.volume=MasterVolume;
    private void ApplyDisplay() => Screen.SetResolution(ResolutionWidth,ResolutionHeight,Fullscreen?FullScreenMode.FullScreenWindow:FullScreenMode.Windowed);
    private float GetFloat(string key,float fallback,float min,float max)=>Mathf.Clamp(PlayerPrefs.GetFloat(Prefix+key,fallback),min,max);
    private void SaveFloat(string key,float value){PlayerPrefs.SetFloat(Prefix+key,value);PlayerPrefs.Save();}
    private void SaveAndNotify(){PlayerPrefs.Save();Notify();}
    private void Notify()=>Changed?.Invoke();
}
