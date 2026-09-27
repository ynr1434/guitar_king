using System;
using UnityEngine;
using UnityEngine.UI;

// Owns only the set-list presentation. Song discovery, selection and loading stay in SongSelectController.
public sealed class SetListUIController : MonoBehaviour
{
    public static readonly Color Ink = new(.18f,.14f,.12f);
    public static readonly Color RedInk = new(.52f,.16f,.12f);
    private Texture2D woodTexture, paperTexture, markerTexture;
    private Text countText;
    public Transform ListContent { get; private set; }
    public Text DetailsText { get; private set; }
    public Text StatusText { get; private set; }
    public Text DifficultyText { get; private set; }
    public Button PlayButton { get; private set; }
    public Button[] DifficultyButtons { get; private set; }
    public ScrollRect Scroll { get; private set; }

    public void Build(Action play, Action<SongDifficulty> selectDifficulty, Action openSettings)
    {
        Canvas canvas = RockUIStyle.Canvas("SongSelectCanvas", 0);
        canvas.gameObject.AddComponent<GraphicRaycaster>();
        woodTexture = CreateWood(); paperTexture = CreatePaper(); markerTexture = CreateMarker();
        Raw(canvas.transform,"BackstageWood",woodTexture,Vector2.zero,Vector2.one,Color.white);
        Text brand = Label(canvas.transform,"Brand","GUITAR KING\n<size=15>BACKSTAGE / LIVE SESSIONS</size>",22,new(.55f,.48f,.38f),new(.025f,.02f),new(.35f,.075f));
        brand.fontStyle = FontStyle.Bold;
        // Multiple shallow layers make a soft paper shadow with no post processing.
        for(int i=7;i>=0;i--)
        {
            Image shadow=RockUIStyle.Box(canvas.transform,"PaperShadow",new(.132f,.055f),new(.882f,.94f),new Color(0,0,0,.055f));
            shadow.rectTransform.offsetMin=new(-i*3,-i*3); shadow.rectTransform.offsetMax=new(i*3,i*3);
            shadow.transform.localRotation=Quaternion.Euler(0,0,-.8f);
        }
        RawImage sheet=Raw(canvas.transform,"SetListPaper",paperTexture,new(.125f,.065f),new(.875f,.95f),Color.white);
        sheet.transform.localRotation=Quaternion.Euler(0,0,-.8f);
        Transform paper=sheet.transform;
        RockUIStyle.Box(paper,"RedMargin",new(.091f,.065f),new(.0925f,.94f),new Color(.66f,.24f,.20f,.38f));
        RockUIStyle.Box(paper,"MarginEcho",new(.095f,.065f),new(.0957f,.94f),new Color(.66f,.24f,.20f,.15f));
        Label(paper,"Kicker","GUITAR KING  /  TONIGHT'S SET",18,RedInk,new(.125f,.908f),new(.7f,.95f));
        Text title=Label(paper,"SetListTitle","SET LIST",88,Ink,new(.12f,.795f),new(.68f,.925f));
        title.fontStyle=FontStyle.BoldAndItalic; title.transform.localRotation=Quaternion.Euler(0,0,1.6f);
        Image underline=RockUIStyle.Box(paper,"TitleMarker",new(.125f,.794f),new(.47f,.80f),Ink);
        underline.transform.localRotation=Quaternion.Euler(0,0,.6f);
        countText=Label(paper,"SongCount","",18,RedInk,new(.72f,.835f),new(.94f,.9f),TextAnchor.MiddleRight);
        Label(paper,"SongColumn","SONG / ARTIST",15,new(.45f,.38f,.31f),new(.125f,.755f),new(.6f,.80f));
        Label(paper,"BestColumn","PERSONAL BEST",15,new(.45f,.38f,.31f),new(.69f,.755f),new(.94f,.80f),TextAnchor.MiddleRight);
        BuildScroll(paper);
        // The lower portion stays fixed while only the handwritten rows scroll.
        RockUIStyle.Box(paper,"FooterRule",new(.12f,.237f),new(.95f,.239f),new(.40f,.33f,.26f,.45f));
        DifficultyText=Label(paper,"SelectedDifficulty","DIFFICULTY / NORMAL",17,RedInk,new(.125f,.188f),new(.57f,.231f));
        DifficultyButtons=new Button[3];
        SongDifficulty[] choices={SongDifficulty.Normal,SongDifficulty.Hard,SongDifficulty.Expert};
        for(int i=0;i<3;i++)
        {
            float x=.125f+i*.147f;
            bool available=i==0;
            Button button=ActionButton(paper,"Difficulty_"+choices[i],available ? "NORMAL" : choices[i].ToString().ToUpperInvariant()+" / LOCKED",available ? 20 : 14,
                new(x,.135f),new(x+.138f,.184f),available ? new(.33f,.26f,.19f) : new(.75f,.70f,.60f,.15f));
            button.interactable=available;
            button.GetComponentInChildren<Text>().color=available ? new(.96f,.90f,.75f) : new(.52f,.46f,.38f);
            if(available) { SongDifficulty difficulty=choices[i]; button.onClick.AddListener(()=>selectDifficulty(difficulty)); }
            DifficultyButtons[i]=button;
        }
        DetailsText=Label(paper,"SelectedSongDetails","Choose a song from tonight's set.",19,Ink,new(.125f,.081f),new(.64f,.127f));
        StatusText=Label(paper,"Status","",14,new(.48f,.39f,.31f),new(.125f,.031f),new(.64f,.079f));
        // Masking-tape label for the primary action.
        Image tape=RockUIStyle.Box(paper,"PlayTapeShadow",new(.68f,.083f),new(.949f,.199f),new Color(.22f,.14f,.08f,.16f));
        tape.transform.localRotation=Quaternion.Euler(0,0,2.2f);
        PlayButton=ActionButton(paper,"PlayButton","PLAY  →",36,new(.677f,.092f),new(.944f,.207f),RedInk);
        PlayButton.transform.localRotation=Quaternion.Euler(0,0,2.2f);
        PlayButton.interactable=false; PlayButton.onClick.AddListener(()=>play());
        Label(paper,"PlayHint","SELECT A ROW  /  THEN PLAY",12,new(.48f,.39f,.31f),new(.67f,.035f),new(.95f,.077f),TextAnchor.MiddleCenter);
        Tape(paper,"TopTape",new(.025f,.945f),new(.205f,1.01f),-13);
        Tape(paper,"BottomTape",new(.84f,-.015f),new(.98f,.048f),12);
        Text note=Label(paper,"Scribble","make it loud.",17,RedInk,new(.70f,.945f),new(.94f,.985f),TextAnchor.MiddleRight);
        note.fontStyle=FontStyle.Italic; note.transform.localRotation=Quaternion.Euler(0,0,3);
        Button settings=ActionButton(canvas.transform,"SettingsButton","⚙  SETTINGS",17,new(.76f,.018f),new(.875f,.062f),new(.16f,.13f,.11f,.95f));
        settings.onClick.AddListener(()=>openSettings());
    }

    private void BuildScroll(Transform paper)
    {
        GameObject list=new("SongList",typeof(RectTransform),typeof(Image),typeof(ScrollRect));
        RockUIStyle.Rect(list,paper,new(.03f,.257f),new(.958f,.75f));
        list.GetComponent<Image>().color=Color.clear;
        Scroll=list.GetComponent<ScrollRect>(); Scroll.horizontal=false; Scroll.vertical=true;
        Scroll.movementType=ScrollRect.MovementType.Clamped; Scroll.scrollSensitivity=45;
        GameObject viewport=new("Viewport",typeof(RectTransform),typeof(RectMask2D));
        RectTransform viewRect=RockUIStyle.Rect(viewport,list.transform,Vector2.zero,Vector2.one);
        GameObject content=new("Content",typeof(RectTransform),typeof(VerticalLayoutGroup),typeof(ContentSizeFitter));
        RectTransform contentRect=RockUIStyle.Rect(content,viewport.transform,new(0,1),Vector2.one);
        contentRect.pivot=new(.5f,1); contentRect.sizeDelta=Vector2.zero;
        VerticalLayoutGroup layout=content.GetComponent<VerticalLayoutGroup>();
        layout.padding=new RectOffset(0,0,4,4); layout.spacing=0;
        layout.childControlWidth=layout.childControlHeight=true;
        layout.childForceExpandWidth=true; layout.childForceExpandHeight=false;
        content.GetComponent<ContentSizeFitter>().verticalFit=ContentSizeFitter.FitMode.PreferredSize;
        Scroll.viewport=viewRect; Scroll.content=contentRect; ListContent=content.transform;
    }

    public SetListRowView CreateRow(string folder, string title, string artist, BestRecord best, bool valid, int number, Action select)
    {
        GameObject go=new("Song_"+folder,typeof(RectTransform),typeof(Image),typeof(Button),typeof(LayoutElement),typeof(SetListRowView));
        go.transform.SetParent(ListContent,false);
        Image hitArea=go.GetComponent<Image>(); hitArea.color=Color.clear;
        Button button=go.GetComponent<Button>(); button.transition=Selectable.Transition.None; button.interactable=valid;
        button.onClick.AddListener(()=>select());
        LayoutElement layout=go.GetComponent<LayoutElement>(); layout.minHeight=130; layout.preferredHeight=130;
        RawImage marker=Raw(go.transform,"SelectedMarker",markerTexture,new(.084f,.13f),new(.995f,.93f),new(1,.54f,.20f,.32f));
        marker.transform.localRotation=Quaternion.Euler(0,0,-.35f);
        Image focus=RockUIStyle.Box(go.transform,"FocusUnderline",new(.112f,.11f),new(.69f,.13f),new Color(.51f,.17f,.12f,.65f));
        Label(go.transform,"Number",number.ToString("00")+".",24,new(.45f,.37f,.29f),new(.008f,.31f),new(.067f,.75f),TextAnchor.MiddleCenter).fontStyle=FontStyle.Italic;
        Text titleText=Label(go.transform,"SongTitle",title,36,valid ? Ink : new(.55f,.35f,.3f),new(.11f,.44f),new(.69f,.90f));
        Label(go.transform,"Artist",artist,23,new(.42f,.34f,.27f),new(.113f,.16f),new(.69f,.47f)).fontStyle=FontStyle.Normal;
        int stars=Mathf.Clamp(best.Stars,0,5);
        string starText="<color=#785D29>"+new string('★',stars)+"</color><color=#998971>"+new string('☆',5-stars)+"</color>";
        Label(go.transform,"BestStars",starText,34,Ink,new(.71f,.44f),new(.983f,.92f),TextAnchor.MiddleRight);
        Label(go.transform,"BestScore",best.Score>0 || best.Stars>0 ? best.Score.ToString("N0") : "—",23,Ink,new(.71f,.14f),new(.978f,.48f),TextAnchor.MiddleRight);
        RockUIStyle.Box(go.transform,"NotebookRule",new(0,.04f),new(1,.055f),new Color(.32f,.43f,.47f,.25f));
        Text arrow=Label(go.transform,"SelectedArrow","›",46,RedInk,new(.067f,.28f),new(.105f,.78f),TextAnchor.MiddleCenter);
        SetListRowView row=go.GetComponent<SetListRowView>(); row.Initialize(button,marker,focus,arrow,titleText,Scroll);
        return row;
    }

    public void SetSongCount(int count) { countText.text=$"{count:00} SONGS\n<size=12>TONIGHT'S SET</size>"; }

    private static Button ActionButton(Transform parent,string name,string caption,int fontSize,Vector2 min,Vector2 max,Color color)
    {
        Image image=RockUIStyle.Box(parent,name,min,max,Color.white); image.raycastTarget=true;
        Button button=image.gameObject.AddComponent<Button>();
        ColorBlock colors=button.colors; colors.normalColor=color; colors.highlightedColor=Color.Lerp(color,Color.white,.16f);
        colors.selectedColor=colors.highlightedColor; colors.pressedColor=Color.Lerp(color,Color.black,.18f);
        colors.disabledColor=new(.57f,.49f,.39f,.40f); button.colors=colors;
        Label(button.transform,"Label",caption,fontSize,new(.99f,.93f,.80f),new(.035f,.06f),new(.965f,.94f),TextAnchor.MiddleCenter);
        return button;
    }

    private static Text Label(Transform parent,string name,string caption,int size,Color color,Vector2 min,Vector2 max,TextAnchor align=TextAnchor.MiddleLeft)
        => RockUIStyle.Label(parent,name,caption,size,color,min,max,align);

    private static RawImage Raw(Transform parent,string name,Texture texture,Vector2 min,Vector2 max,Color color)
    {
        GameObject go=new(name,typeof(RectTransform),typeof(RawImage)); RockUIStyle.Rect(go,parent,min,max);
        RawImage image=go.GetComponent<RawImage>(); image.texture=texture; image.color=color; image.raycastTarget=false; return image;
    }

    private static void Tape(Transform parent,string name,Vector2 min,Vector2 max,float rotation)
    {
        Image tape=RockUIStyle.Box(parent,name,min,max,new(.74f,.61f,.38f,.76f)); tape.transform.localRotation=Quaternion.Euler(0,0,rotation);
        for(int i=0;i<5;i++) RockUIStyle.Box(tape.transform,"TapeFibers",new(.08f,.12f+i*.15f),new(.93f,.125f+i*.15f),new Color(1,.92f,.72f,.10f));
    }

    private static Texture2D CreateWood()
    {
        const int w=960,h=540; Texture2D tex=new(w,h,TextureFormat.RGBA32,false); Color32[] pixels=new Color32[w*h];
        for(int y=0;y<h;y++) for(int x=0;x<w;x++)
        {
            float u=(float)x/w,v=(float)y/h;
            float grain=Mathf.PerlinNoise(x*.016f,y*.35f)*.5f+Mathf.PerlinNoise(x*.005f,y*1.3f)*.5f;
            float seam=y%90<3 ? .48f : 1;
            float light=.56f+ .44f*Mathf.Exp(-((u-.47f)*(u-.47f)*3+(v-.58f)*(v-.58f)*2));
            Color c=Color.Lerp(new(.07f,.051f,.04f),new(.23f,.165f,.11f),grain)*seam*light;
            if((x%310<3) && y%90<8) c*=.45f;
            pixels[y*w+x]=new Color(c.r,c.g,c.b,1);
        }
        tex.SetPixels32(pixels); tex.Apply(false,true); return tex;
    }

    private static Texture2D CreatePaper()
    {
        const int w=768,h=768; Texture2D tex=new(w,h,TextureFormat.RGBA32,false); Color32[] pixels=new Color32[w*h];
        for(int y=0;y<h;y++) for(int x=0;x<w;x++)
        {
            float edge=Mathf.Min(Mathf.Min(x,w-1-x),Mathf.Min(y,h-1-y));
            float noise=Mathf.PerlinNoise(x*.11f,y*.11f);
            float fiber=((x*73+y*193)%97)/97f;
            Color c=Color.Lerp(new(.87f,.81f,.67f),new(.96f,.92f,.79f),.45f+noise*.4f+fiber*.15f);
            c*=.91f+.09f*Mathf.Clamp01(edge/26);
            float worn=2+2.5f*Mathf.PerlinNoise(x*.04f,y*.04f);
            c.a=Mathf.Clamp01(edge-worn);
            pixels[y*w+x]=c;
        }
        tex.wrapMode=TextureWrapMode.Clamp; tex.SetPixels32(pixels); tex.Apply(false,true); return tex;
    }

    private static Texture2D CreateMarker()
    {
        const int w=256,h=32; Texture2D tex=new(w,h,TextureFormat.RGBA32,false); Color32[] pixels=new Color32[w*h];
        for(int y=0;y<h;y++) for(int x=0;x<w;x++)
        {
            float edge=Mathf.Min(y,h-1-y)-Mathf.PerlinNoise(x*.1f,0)*3;
            float a=Mathf.Clamp01(edge/2)*Mathf.Clamp01(Mathf.Min(x,w-1-x)/7f);
            pixels[y*w+x]=new Color(1,1,1,a*(.7f+.3f*Mathf.PerlinNoise(x*.13f,y*.3f)));
        }
        tex.wrapMode=TextureWrapMode.Clamp; tex.SetPixels32(pixels); tex.Apply(false,true); return tex;
    }

    private void OnDestroy()
    {
        if(woodTexture!=null) Destroy(woodTexture);
        if(paperTexture!=null) Destroy(paperTexture);
        if(markerTexture!=null) Destroy(markerTexture);
    }
}
