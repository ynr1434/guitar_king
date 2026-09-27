using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public sealed class SettingsPanel : MonoBehaviour
{
    public static bool IsAnyOpen { get; private set; }
    public static bool ClosedThisFrame { get; private set; }
    private GameObject panel;
    private Action closed;
    private readonly List<Slider> sliders=new();
    private sealed class ChoiceControl
    {
        public Text Caption;
        public int Value;
        public List<string> Options;
        public Action<int> Changed;
        public void SetValueWithoutNotify(int value){Value=Mathf.Clamp(value,0,(Options?.Count??1)-1);if(Caption!=null&&Options!=null&&Options.Count>0)Caption.text=Options[Value];}
    }
    private ChoiceControl vfx,resolution;
    private Toggle shake,fullscreen;
    private Text masterValue,musicValue,voiceValue,highwayValue,videoValue;
    private List<Vector2Int> resolutions;
    private bool refreshing;
    private GameObject activePopup;

    public void Build(int sortingOrder=200)
    {
        Canvas canvas=RockUIStyle.Canvas("SettingsCanvas",sortingOrder);canvas.gameObject.AddComponent<GraphicRaycaster>();
        panel=new GameObject("SettingsPanel",typeof(RectTransform),typeof(Image));RockUIStyle.Rect(panel,canvas.transform,Vector2.zero,Vector2.one);
        panel.GetComponent<Image>().color=new(.006f,.006f,.014f,.96f);
        RectTransform plate=RockUIStyle.Plate(panel.transform,"SettingsRoadCase",new(.20f,.07f),new(.80f,.94f),RockUIStyle.Gold);
        RockUIStyle.Label(plate,"Title","SETTINGS",54,Color.white,new(.06f,.88f),new(.65f,.98f));
        RockUIStyle.Label(plate,"Kicker","GUITAR KING  /  FRONT OF HOUSE",15,RockUIStyle.Gold,new(.60f,.91f),new(.94f,.97f),TextAnchor.MiddleRight);
        Row(plate,"MASTER VOLUME",.79f,0,1,()=>SettingsManager.Instance.MasterVolume,v=>SettingsManager.Instance.SetMaster(v),out masterValue);
        Row(plate,"MUSIC VOLUME",.70f,0,1,()=>SettingsManager.Instance.MusicVolume,v=>SettingsManager.Instance.SetMusic(v),out musicValue);
        Row(plate,"RESULT VOICE VOLUME",.61f,0,1,()=>SettingsManager.Instance.ResultVoiceVolume,v=>SettingsManager.Instance.SetResultVoice(v),out voiceValue);
        Row(plate,"HIGHWAY OPACITY",.52f,.4f,1,()=>SettingsManager.Instance.HighwayOpacity,v=>SettingsManager.Instance.SetHighwayOpacity(v),out highwayValue);
        Row(plate,"VIDEO DARKNESS",.43f,0,.8f,()=>SettingsManager.Instance.VideoDarkness,v=>SettingsManager.Instance.SetVideoDarkness(v),out videoValue);
        vfx=DropdownRow(plate,"VFX INTENSITY",.34f,new(){"LOW","NORMAL","HIGH"},i=>SettingsManager.Instance.SetVfxIntensity(i));
        shake=ToggleRow(plate,"SCREEN SHAKE",.265f,v=>SettingsManager.Instance.SetScreenShake(v));
        fullscreen=ToggleRow(plate,"FULLSCREEN",.195f,v=>SettingsManager.Instance.SetFullscreen(v));
        resolutions=SettingsManager.GetResolutions();List<string> options=new();foreach(Vector2Int r in resolutions)options.Add($"{r.x} × {r.y}");
        resolution=DropdownRow(plate,"RESOLUTION",.125f,options,i=>{if(i>=0&&i<resolutions.Count)SettingsManager.Instance.SetResolution(resolutions[i].x,resolutions[i].y);});
        Button reset=Button(plate,"Reset","RESET TO DEFAULTS",new(.06f,.025f),new(.44f,.095f),new(.42f,.12f,.11f));reset.onClick.AddListener(()=>{SettingsManager.Instance.ResetDefaults();Refresh();});
        Button back=Button(plate,"Back","BACK",new(.56f,.025f),new(.94f,.095f),new(.36f,.25f,.14f));back.onClick.AddListener(Close);
        panel.SetActive(false);
    }

    public void Open(Action onClosed)
    {
        closed=onClosed;Refresh();panel.SetActive(true);panel.transform.parent.SetAsLastSibling();IsAnyOpen=true;
    }
    public void Close(){if(!panel.activeSelf)return;panel.SetActive(false);IsAnyOpen=false;ClosedThisFrame=true;Action callback=closed;closed=null;callback?.Invoke();}
    private void LateUpdate(){ClosedThisFrame=false;}
    private void Update(){if(panel!=null&&panel.activeSelf&&Keyboard.current!=null&&Keyboard.current.escapeKey.wasPressedThisFrame)Close();}
    private void Refresh()
    {
        refreshing=true;SettingsManager s=SettingsManager.Instance;
        float[] values={s.MasterVolume,s.MusicVolume,s.ResultVoiceVolume,s.HighwayOpacity,s.VideoDarkness};for(int i=0;i<sliders.Count;i++)sliders[i].SetValueWithoutNotify(values[i]);
        masterValue.text=Percent(s.MasterVolume);musicValue.text=Percent(s.MusicVolume);voiceValue.text=Percent(s.ResultVoiceVolume);highwayValue.text=Percent(s.HighwayOpacity);videoValue.text=Percent(s.VideoDarkness);
        vfx.SetValueWithoutNotify((int)s.VfxIntensity);shake.SetIsOnWithoutNotify(s.ScreenShake);fullscreen.SetIsOnWithoutNotify(s.Fullscreen);
        int index=resolutions.FindIndex(r=>r.x==s.ResolutionWidth&&r.y==s.ResolutionHeight);
        if(index<0)
        {
            long bestDistance=long.MaxValue;
            for(int i=0;i<resolutions.Count;i++)
            {
                long dx=resolutions[i].x-s.ResolutionWidth, dy=resolutions[i].y-s.ResolutionHeight, distance=dx*dx+dy*dy;
                if(distance<bestDistance){bestDistance=distance;index=i;}
            }
        }
        resolution.SetValueWithoutNotify(Mathf.Max(0,index));refreshing=false;
    }
    private void Row(Transform p,string label,float y,float min,float max,Func<float> get,Action<float> set,out Text value)
    {
        RockUIStyle.Label(p,label,label,18,new(.8f,.8f,.84f),new(.07f,y),new(.34f,y+.06f));
        Slider slider=SliderAt(p,new(.35f,y+.012f),new(.82f,y+.052f),min,max);sliders.Add(slider);
        value=RockUIStyle.Label(p,label+"Value","",18,RockUIStyle.Gold,new(.84f,y),new(.94f,y+.06f),TextAnchor.MiddleRight);Text captured=value;
        slider.onValueChanged.AddListener(v=>{if(refreshing)return;set(v);captured.text=Percent(v);});
    }
    private ChoiceControl DropdownRow(Transform p,string label,float y,List<string> options,Action<int> changed)
    {
        RockUIStyle.Label(p,label,label,18,new(.8f,.8f,.84f),new(.07f,y),new(.38f,y+.055f));
        GameObject go=new(label+"Dropdown",typeof(RectTransform),typeof(Image),typeof(Button));RockUIStyle.Rect(go,p,new(.55f,y),new(.94f,y+.06f));Image background=go.GetComponent<Image>();background.color=new(.11f,.105f,.13f);background.raycastTarget=true;
        Text caption=RockUIStyle.Label(go.transform,"Label",options.Count>0?options[0]:"",18,Color.white,new(.05f,.05f),new(.85f,.95f));
        ChoiceControl choice=new(){Caption=caption,Options=options,Changed=changed};Button opener=go.GetComponent<Button>();opener.targetGraphic=background;opener.onClick.AddListener(()=>ToggleOptions(go.transform.parent,label,y,options,choice));
        return choice;
    }

    private void ToggleOptions(Transform parent,string label,float y,List<string> options,ChoiceControl choice)
    {
        if(activePopup!=null){Destroy(activePopup);activePopup=null;return;}
        GameObject popup=new(label+"Options",typeof(RectTransform),typeof(Image),typeof(VerticalLayoutGroup));
        RockUIStyle.Rect(popup,parent,new(.55f,y-.065f*options.Count),new(.94f,y+.005f));
        Image background=popup.GetComponent<Image>();background.color=new(.055f,.055f,.075f,.99f);background.raycastTarget=true;
        VerticalLayoutGroup layout=popup.GetComponent<VerticalLayoutGroup>();layout.padding=new RectOffset(5,5,5,5);layout.spacing=2;layout.childControlWidth=true;layout.childControlHeight=true;layout.childForceExpandWidth=true;layout.childForceExpandHeight=false;
        for(int index=0;index<options.Count;index++)
        {
            int selected=index;Image itemImage=RockUIStyle.Box(popup.transform,"Option_"+index,Vector2.zero,Vector2.one,new(.12f,.12f,.15f));itemImage.raycastTarget=true;
            LayoutElement itemLayout=itemImage.gameObject.AddComponent<LayoutElement>();itemLayout.minHeight=38;itemLayout.preferredHeight=38;
            Button item=itemImage.gameObject.AddComponent<Button>();item.targetGraphic=itemImage;item.transition=Selectable.Transition.ColorTint;
            ColorBlock colors=item.colors;colors.normalColor=new(.12f,.12f,.15f);colors.highlightedColor=RockUIStyle.Gold;colors.pressedColor=new(.8f,.35f,.08f);item.colors=colors;
            Text text=RockUIStyle.Label(itemImage.transform,"Label",options[index],16,Color.white,Vector2.zero,Vector2.one,TextAnchor.MiddleCenter);
            item.onClick.AddListener(()=>{choice.Value=selected;choice.Caption.text=options[selected];choice.Changed?.Invoke(selected);Destroy(activePopup);activePopup=null;});
        }
        activePopup=popup;popup.transform.SetAsLastSibling();
    }
    private Toggle ToggleRow(Transform p,string label,float y,Action<bool> changed)
    {
        RockUIStyle.Label(p,label,label,18,new(.8f,.8f,.84f),new(.07f,y),new(.55f,y+.055f));
        GameObject go=new(label+"Toggle",typeof(RectTransform),typeof(Image),typeof(Toggle));RockUIStyle.Rect(go,p,new(.84f,y+.006f),new(.94f,y+.055f));Image bg=go.GetComponent<Image>();bg.color=new(.12f,.12f,.15f);
        bg.raycastTarget=true;
        Image mark=RockUIStyle.Box(go.transform,"Checkmark",new(.12f,.18f),new(.88f,.82f),RockUIStyle.Gold);mark.raycastTarget=false;
        Toggle t=go.GetComponent<Toggle>();t.targetGraphic=bg;t.graphic=mark;t.onValueChanged.AddListener(v=>{if(!refreshing)changed(v);});return t;
    }
    private Slider SliderAt(Transform p,Vector2 min,Vector2 max,float low,float high)
    {
        GameObject go=new("Slider",typeof(RectTransform),typeof(Slider));RockUIStyle.Rect(go,p,min,max);Slider s=go.GetComponent<Slider>();s.minValue=low;s.maxValue=high;
        Image bg=RockUIStyle.Box(go.transform,"Track",new(0,.35f),new(1,.65f),new(.15f,.15f,.18f));bg.raycastTarget=true;
        Image fill=RockUIStyle.Box(go.transform,"Fill",new(0,.28f),new(1,.72f),RockUIStyle.Gold);fill.raycastTarget=false;s.fillRect=fill.rectTransform;
        Image handle=RockUIStyle.Box(go.transform,"Handle",Vector2.zero,Vector2.zero,Color.white);handle.raycastTarget=true;handle.rectTransform.sizeDelta=new(18,34);s.handleRect=handle.rectTransform;s.targetGraphic=bg;s.direction=Slider.Direction.LeftToRight;return s;
    }
    private Button Button(Transform p,string name,string text,Vector2 min,Vector2 max,Color color){Image image=RockUIStyle.Box(p,name,min,max,color);image.raycastTarget=true;Button b=image.gameObject.AddComponent<Button>();b.targetGraphic=image;b.interactable=true;RockUIStyle.Label(b.transform,"Label",text,19,Color.white,Vector2.zero,Vector2.one,TextAnchor.MiddleCenter);return b;}
    private static string Percent(float v)=>Mathf.RoundToInt(v*100)+"%";
    private void OnDestroy(){if(activePopup!=null)Destroy(activePopup);if(panel!=null&&panel.activeSelf)IsAnyOpen=false;if(panel!=null)Destroy(panel.transform.parent.gameObject);}
}
