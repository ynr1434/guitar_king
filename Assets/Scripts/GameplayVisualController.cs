using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DefaultExecutionOrder(9000)]
public sealed class GameplayVisualController : MonoBehaviour
{
    [SerializeField, Range(0f, 1f)] private float highwayOpacity = 0.70f;

    private ScoreManager score;
    private RockMeter rock;
    private SongPackageLoader loader;
    private SongClock clock;
    private SongAudioController songAudio;
    private BeatGrid grid;
    private ChartRecorder recorder;
    private RhythmInputController input;
    private Text scoreText, accuracyText, comboText, multiplierText, introText, recText;
    private Text countdownText;
    private Image multiplierAccent, rockMarker;
    private CanvasGroup intro;
    private readonly List<Material> materials = new();
    private readonly List<Mesh> meshes = new();
    private readonly Dictionary<Color, Material> materialCache = new();
    private readonly Transform[] beatLines = new Transform[24];
    private readonly Renderer[] rings = new Renderer[5];
    private MaterialPropertyBlock properties;
    private Texture2D stageTexture;
    private RawImage backgroundImage;
    private Material videoBackgroundMaterial;
    private readonly List<Material> highwayMaterials = new();
    private Mesh discMesh, ringMesh;
    private Material steel, black, silver;
    private float introAge, comboPulse, multiplierPulse, rockPulse;
    private int previousCombo, previousMultiplier = 1, previousRock = 70;
    private bool ready;
    private static GameplayVisualController instance;
    public static GameplayVisualController Instance => instance;
    private static readonly Color[] LaneColors = { new(.16f,1f,.29f), new(1f,.12f,.18f), new(1f,.82f,.09f), new(.12f,.51f,1f), new(1f,.38f,.07f) };

    private void Start()
    {
        properties = new MaterialPropertyBlock();
        highwayOpacity = SettingsManager.Instance.HighwayOpacity;
        SettingsManager.Instance.Changed += ApplyVisualSettings;
        instance = this;
        score = FindFirstObjectByType<ScoreManager>(); rock = FindFirstObjectByType<RockMeter>();
        loader = FindFirstObjectByType<SongPackageLoader>(); clock = FindFirstObjectByType<SongClock>();
        songAudio = FindFirstObjectByType<SongAudioController>();
        grid = FindFirstObjectByType<BeatGrid>(); recorder = FindFirstObjectByType<ChartRecorder>();
        input = FindFirstObjectByType<RhythmInputController>();
        BuildWorld(); BuildHUD(); HideLegacyHUD();
        SongBackgroundVideoController pendingVideo = FindFirstObjectByType<SongBackgroundVideoController>();
        if (pendingVideo != null && pendingVideo.IsPrepared)
            SetVideoBackground(pendingVideo.TargetTexture, pendingVideo.VideoDarkness);
        if (score != null) { score.ValuesChanged += RefreshScore; RefreshScore(); }
        if (rock != null) { rock.ValueChanged += RefreshRock; RefreshRock(); }
        if (loader != null) { loader.StateChanged += RefreshSong; RefreshSong(); }
        // A loader can finish before this presentation component starts.
        foreach (NoteController note in FindObjectsByType<NoteController>(FindObjectsSortMode.None)) DecorateNote(note);
    }

    public void SetVideoBackground(RenderTexture videoTexture, float darkness)
    {
        if (backgroundImage == null) return;
        backgroundImage.texture = videoTexture != null ? videoTexture : stageTexture;
        if (videoTexture == null)
        {
            backgroundImage.material = null;
            if (videoBackgroundMaterial != null) { Destroy(videoBackgroundMaterial); videoBackgroundMaterial = null; }
            return;
        }
        if (videoBackgroundMaterial == null)
            videoBackgroundMaterial = new Material(Resources.Load<Shader>("VideoDarkOverlay"));
        videoBackgroundMaterial.SetFloat("_Darkness", SettingsManager.Instance.VideoDarkness);
        backgroundImage.material = videoBackgroundMaterial;
    }

    private static GameObject FindPanel(string name)
    {
        foreach (Canvas canvas in FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            Transform panel = canvas.transform.Find(name);
            if (panel != null) return panel.gameObject;
        }
        return null;
    }

    private void HideLegacyHUD()
    {
        foreach (ScoreDisplayUI display in FindObjectsByType<ScoreDisplayUI>(FindObjectsSortMode.None)) display.enabled = false;
        foreach (SongPackageDisplayUI display in FindObjectsByType<SongPackageDisplayUI>(FindObjectsSortMode.None)) display.enabled = false;
        foreach (ChartRecorderDisplayUI display in FindObjectsByType<ChartRecorderDisplayUI>(FindObjectsSortMode.None)) display.enabled = false;
        foreach (BeatMetronomeDebug display in FindObjectsByType<BeatMetronomeDebug>(FindObjectsSortMode.None)) display.enabled = false;
        string[] names = { "ScoreText", "ComboText", "MultiplierText", "AccuracyText", "SongPackageText", "ChartRecorderText", "BeatText", "BeatPulseText" };
        foreach (Text t in FindObjectsByType<Text>(FindObjectsSortMode.None))
            if (Array.IndexOf(names, t.name) >= 0) t.gameObject.SetActive(false);
        GameObject oldRock = GameObject.Find("RockMeterCanvas");
        if (oldRock != null) oldRock.GetComponent<Canvas>().enabled = false;
    }

    private void BuildHUD()
    {
        Canvas canvas = RockUIStyle.Canvas("ConcertHUD", 25);
        Transform root = canvas.transform;
        RectTransform scorePlate = RockUIStyle.Plate(root, "ScoreInstrument", new(.035f,.785f), new(.245f,.95f), RockUIStyle.Gold);
        RockUIStyle.Label(scorePlate, "ScoreCaption", "S C O R E", 20, RockUIStyle.Gold, new(.065f,.64f), new(.93f,.92f));
        scoreText = RockUIStyle.Label(scorePlate, "ScoreDigits", "0000000", 64, Color.white, new(.06f,.12f), new(.94f,.70f));
        RockUIStyle.Label(root, "Brand", "GUITAR KING  /  LIVE", 16, new(.55f,.55f,.63f), new(.037f,.95f), new(.3f,.99f));
        RectTransform accPlate = RockUIStyle.Plate(root, "AccuracyInstrument", new(.795f,.83f), new(.965f,.95f), new(.47f,.39f,.65f));
        RockUIStyle.Label(accPlate, "AccCaption", "A C C U R A C Y", 17, new(.65f,.62f,.74f), new(.08f,.63f), new(.92f,.92f));
        accuracyText = RockUIStyle.Label(accPlate, "AccuracyDigits", "0.0%", 42, Color.white, new(.08f,.1f), new(.92f,.68f));
        RectTransform multi = RockUIStyle.Plate(root, "MultiplierInstrument", new(.805f,.24f), new(.955f,.51f), RockUIStyle.Gold);
        multiplierAccent = RockUIStyle.Box(multi, "Heat", new(.05f,.48f), new(.95f,.93f), new(.13f,.095f,.055f));
        multiplierText = RockUIStyle.Label(multi, "MultiplierDigits", "x1", 94, RockUIStyle.Gold, new(.07f,.45f), new(.93f,.98f), TextAnchor.MiddleCenter);
        RockUIStyle.Label(multi, "ComboCaption", "N O T E  S T R E A K", 16, new(.65f,.63f,.65f), new(.05f,.31f), new(.95f,.46f), TextAnchor.MiddleCenter);
        comboText = RockUIStyle.Label(multi, "ComboDigits", "0", 48, Color.white, new(.1f,.04f), new(.9f,.33f), TextAnchor.MiddleCenter);
        RectTransform rockPlate = RockUIStyle.Plate(root, "RockInstrument", new(.035f,.31f), new(.215f,.54f), new(.7f,.19f,.13f));
        RockUIStyle.Label(rockPlate, "RockCaption", "R O C K", 26, Color.white, new(.09f,.67f), new(.91f,.94f));
        RockUIStyle.Label(rockPlate, "RockZones", "DANGER                         LIVE", 13, new(.6f,.6f,.64f), new(.09f,.09f), new(.91f,.3f));
        for (int i = 0; i < 20; i++)
        {
            Color color = i < 6 ? new(1,.18f,.14f) : i < 12 ? new(1,.67f,.15f) : new(.35f,.86f,.36f);
            RockUIStyle.Box(rockPlate, "MeterSegment", new(.09f + i*.041f,.34f), new(.12f + i*.041f,.61f), color);
        }
        rockMarker = RockUIStyle.Box(rockPlate, "Needle", new(.65f,.29f), new(.661f,.65f), Color.white);
        RectTransform introCard = RockUIStyle.Plate(root, "SongIntro", new(.30f,.83f), new(.70f,.97f), RockUIStyle.Gold);
        intro = introCard.gameObject.AddComponent<CanvasGroup>(); intro.blocksRaycasts = false;
        introText = RockUIStyle.Label(introCard, "SongIntroText", "TAKING THE STAGE...", 27, Color.white, new(.06f,.08f), new(.94f,.91f), TextAnchor.MiddleCenter);
        recText = RockUIStyle.Label(root, "RecordingOverlay", "", 19, new(1,.25f,.25f), new(.035f,.16f), new(.245f,.24f));
        countdownText = RockUIStyle.Label(root, "ReadyCountdown", "", 100, RockUIStyle.Gold,
            new(.35f,.39f), new(.65f,.68f), TextAnchor.MiddleCenter);
        Shadow countdownShadow = countdownText.gameObject.AddComponent<Shadow>();
        countdownShadow.effectColor = new Color(0,0,0,.9f);
        countdownShadow.effectDistance = new Vector2(3,-4);
        RockUIStyle.Label(root, "Controls", "1 — 5   FRETS\nSPACE   STRUM\nESC   PAUSE", 14, new(.43f,.43f,.52f), new(.038f,.05f), new(.20f,.14f));
    }

    private void RefreshScore()
    {
        comboPulse = score.Combo != previousCombo ? .20f : comboPulse;
        multiplierPulse = score.Multiplier > previousMultiplier ? .45f : multiplierPulse;
        comboText.color = score.Combo == 0 && previousCombo > 0 ? new(1,.22f,.18f) : Color.white;
        previousCombo = score.Combo; previousMultiplier = score.Multiplier;
        scoreText.text = score.Score.ToString("D7"); comboText.text = score.Combo.ToString();
        multiplierText.text = "x" + score.Multiplier; accuracyText.text = $"{score.Accuracy:0.0}%";
        multiplierAccent.color = Color.Lerp(new(.09f,.075f,.065f), new(.53f,.13f,.035f), (score.Multiplier-1)/3f);
    }

    private void RefreshRock()
    {
        rockPulse = .25f;
        rockMarker.color = rock.CurrentValue < previousRock ? new(1,.35f,.3f) : Color.white;
        previousRock = rock.CurrentValue;
        float x = .09f + .81f * rock.CurrentValue/100f;
        rockMarker.rectTransform.anchorMin = new(x-.005f,.29f);
        rockMarker.rectTransform.anchorMax = new(x+.005f,.65f);
    }

    private void RefreshSong()
    {
        if (loader.State == SongPackageState.Ready)
        {
            ready = true; introAge = 0;
            introText.text = $"{loader.SongTitle.ToUpperInvariant()}\n<size=19>{loader.Artist}  /  {loader.Difficulty.ToString().ToUpperInvariant()}</size>";
        }
        else if (loader.State == SongPackageState.Error)
        {
            introText.text = "SONG COULD NOT LOAD\n<size=17>" + loader.LastError + "</size>";
            introText.color = new(1,.3f,.25f);
        }
    }

    private void Update()
    {
        UpdateCountdown();
        if (PauseController.IsPaused) return;
        float dt = Time.unscaledDeltaTime; // Presentation animation only; note timing remains DSP based.
        if (ready) { introAge += dt; intro.alpha = 1-Mathf.Clamp01((introAge-2f)/.65f); }
        comboPulse = Mathf.Max(0,comboPulse-dt); multiplierPulse = Mathf.Max(0,multiplierPulse-dt);
        if (comboPulse <= 0) comboText.color = Color.white;
        rockPulse = Mathf.Max(0,rockPulse-dt);
        comboText.transform.localScale = Vector3.one*(1+Mathf.Sin(comboPulse/.2f*Mathf.PI)*.13f);
        multiplierText.transform.localScale = Vector3.one*(1+Mathf.Sin(multiplierPulse/.45f*Mathf.PI)*.18f);
        rockMarker.transform.localScale = new Vector3(1+rockPulse*3,1,1);
        if (recorder != null) recText.text = recorder.Recording ? $"REC ●   /   EVENTS {recorder.RecordedNoteCount}" : "";
        for (int i = 0; i < 5; i++)
        {
            bool held = input != null && input.IsLaneHeld((RhythmLane)i);
            properties.Clear(); properties.SetColor("_BaseColor", LaneColors[i]*(held ? 1.7f : .8f));
            rings[i].SetPropertyBlock(properties);
        }
        if (grid != null && clock != null && ready)
        {
            long start = (long)Math.Floor(grid.CurrentBeatPosition*2);
            for (int i = 0; i < beatLines.Length; i++)
            {
                long half = start+i;
                double time = grid.GetBeatTime((long)Math.Floor(half/2.0)) + (half % 2 != 0 ? grid.SecondsPerHalfBeat : 0);
                float z = -1.55f + (float)((time-clock.SongTime)/2.0)*16.55f;
                beatLines[i].gameObject.SetActive(z >= -1.6f && z <= 15);
                beatLines[i].position = new Vector3(0,.105f,z);
                beatLines[i].localScale = new Vector3(5.35f,.008f,half%2==0 ? .035f : .012f);
            }
        }
    }

    private void UpdateCountdown()
    {
        if (countdownText == null) return;
        if (!ready || songAudio == null || !songAudio.IsScheduled || clock == null || ResultsController.GameplayFinished)
        {
            countdownText.text = "";
            return;
        }
        // SongClock also freezes this countdown when the player pauses.
        double remaining = -clock.SongTime - songAudio.NotePreRollSeconds;
        countdownText.text = remaining > 0
            ? "<size=25>GET READY</size>\n" + Math.Ceiling(remaining).ToString("0")
            : remaining > -.45 ? "GO" : "";
        float pulse = remaining > 0 ? (float)(remaining - Math.Floor(remaining)) : 0;
        countdownText.transform.localScale = Vector3.one * (1 + pulse * .10f);
    }

    private Material Material(Color color)
    {
        if (materialCache.TryGetValue(color, out Material cached)) return cached;
        Material material = new Material(Resources.Load<Shader>("ConcertSurface"));
        material.SetColor("_BaseColor",color);
        material.SetColor("_EmissionColor", color*.18f);
        materials.Add(material); materialCache.Add(color, material); return material;
    }

    private Transform Cube(string name, Transform parent, Vector3 position, Vector3 scale, Material material)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = name;
        go.transform.SetParent(parent,false); go.transform.localPosition = position; go.transform.localScale = scale;
        Destroy(go.GetComponent<Collider>()); go.GetComponent<Renderer>().sharedMaterial = material;
        go.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        return go.transform;
    }

    private void BuildWorld()
    {
        Camera camera = Camera.main;
        if (camera != null)
        {
            camera.transform.SetPositionAndRotation(new Vector3(0,6,-7),Quaternion.Euler(28,0,0));
            camera.fieldOfView = 44; camera.backgroundColor = new(.009f,.007f,.02f);
            BuildStage(camera);
        }
        steel = Material(new(.12f,.13f,.17f)); black = Material(new(.018f,.018f,.026f)); silver = Material(new(.55f,.57f,.62f));
        GameObject road = GameObject.Find("RoadBase");
        if (road != null) road.GetComponent<Renderer>().sharedMaterial = HighwaySurfaceMaterial(new(.018f,.018f,.026f));
        discMesh = CreateRing(0,.90f); ringMesh = CreateRing(.96f,1.12f);
        Transform scenery = new GameObject("ConcertHighwayPresentation").transform;
        for (int i=0;i<5;i++)
        {
            RhythmLane lane = (RhythmLane)i;
            GameObject old = GameObject.Find("Lane"+lane);
            if (old != null) old.GetComponent<Renderer>().sharedMaterial = HighwaySurfaceMaterial(Color.Lerp(new(.022f,.020f,.030f),LaneColors[i],.035f));
            Transform target = input.GetLaneTarget(lane);
            DecorateHead(target,LaneColors[i],true);
            rings[i] = target.Find("NeonRim").GetComponent<Renderer>();
        }
        foreach (Renderer renderer in FindObjectsByType<Renderer>(FindObjectsSortMode.None))
            if (renderer.name.StartsWith("LaneDivider")) renderer.sharedMaterial = steel;
        for(int side=-1;side<=1;side+=2)
        {
            Cube("SteelRail",scenery,new(side*2.85f,.08f,6.6f),new(.24f,.20f,18.3f),steel);
            Cube("RailBevel",scenery,new(side*2.93f,.19f,6.6f),new(.045f,.025f,18.3f),silver);
            Cube("RailInnerLight",scenery,new(side*2.74f,.17f,6.6f),new(.025f,.03f,18.3f),Material(new(.48f,.24f,.13f)));
            for(int i=0;i<18;i++) Cube("RailRivet",scenery,new(side*2.86f,.20f,-1.7f+i),new(.07f,.025f,.07f),silver);
        }
        // Fine transverse etching gives the board texture without obscuring notes.
        Material grain = Material(new(.055f,.049f,.065f));
        for(int i=0;i<75;i++) Cube("FretboardEtch",scenery,new(0,.101f,-1.5f+i*.23f),new(5.32f,.003f,.008f),grain);
        Cube("HitBridgeBase",scenery,new(0,.06f,-1.55f),new(5.7f,.15f,.64f),black);
        Cube("HitBridgeChrome",scenery,new(0,.17f,-1.8f),new(5.7f,.06f,.075f),silver);
        Cube("HitBridgeLight",scenery,new(0,.18f,-1.31f),new(5.7f,.035f,.025f),Material(RockUIStyle.Gold));
        Material gridMaterial = Material(new(.22f,.19f,.24f));
        for(int i=0;i<beatLines.Length;i++) beatLines[i] = Cube("MusicalGrid",scenery,new(0,.105f,i),new(5.35f,.008f,.025f),gridMaterial);
    }

    private Material HighwaySurfaceMaterial(Color color)
    {
        Material material = new Material(Resources.Load<Shader>("HighwaySurface"));
        color.a = highwayOpacity;
        material.SetColor("_BaseColor", color);
        material.SetColor("_EmissionColor", Color.black);
        materials.Add(material);
        highwayMaterials.Add(material);
        return material;
    }

    private void ApplyVisualSettings()
    {
        highwayOpacity = SettingsManager.Instance.HighwayOpacity;
        foreach (Material material in highwayMaterials)
        {
            if (material == null) continue;
            Color color = material.GetColor("_BaseColor"); color.a = highwayOpacity; material.SetColor("_BaseColor", color);
        }
        if (videoBackgroundMaterial != null) videoBackgroundMaterial.SetFloat("_Darkness", SettingsManager.Instance.VideoDarkness);
    }

    private Mesh CreateRing(float inner, float outer)
    {
        const int segments=48;
        List<Vector3> vertices=new(); List<int> triangles=new(); List<Vector3> normals=new();
        for(int i=0;i<=segments;i++)
        {
            float a=i*Mathf.PI*2/segments; Vector3 direction=new(Mathf.Cos(a),0,Mathf.Sin(a));
            vertices.Add(direction*inner); vertices.Add(direction*outer);
            normals.Add(Vector3.up); normals.Add(Vector3.up);
            if(i<segments) { int v=i*2; triangles.AddRange(new[]{v,v+2,v+1,v+1,v+2,v+3}); }
        }
        Mesh mesh=new(); mesh.name="Arcade machined ring"; mesh.SetVertices(vertices); mesh.SetNormals(normals); mesh.SetTriangles(triangles,0); mesh.RecalculateBounds();
        meshes.Add(mesh); return mesh;
    }

    private Renderer MeshPart(Transform parent,string name,Mesh mesh,float y,Vector3 scale,Material material)
    {
        GameObject go=new(name,typeof(MeshFilter),typeof(MeshRenderer)); go.transform.SetParent(parent,false);
        go.transform.localPosition=new(0,y,0); go.transform.localScale=scale;
        go.GetComponent<MeshFilter>().sharedMesh=mesh; Renderer renderer=go.GetComponent<Renderer>(); renderer.sharedMaterial=material;
        renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off; return renderer;
    }

    private void DecorateHead(Transform head,Color color,bool target)
    {
        if(head == null || head.Find("NeonRim") != null) return;
        head.GetComponent<MeshFilter>().sharedMesh=discMesh;
        head.GetComponent<Renderer>().sharedMaterial=Material(color);
        MeshPart(head,"OuterHousing",discMesh,-.32f,new(1.35f,1,1.35f),black);
        MeshPart(head,"ChromeRing",ringMesh,-.10f,new(1.08f,1,1.08f),silver);
        MeshPart(head,"NeonRim",ringMesh,.06f,Vector3.one,Material(color));
        MeshPart(head,"Recess",discMesh,.08f,new(.73f,1,.73f),Material(color*.36f));
        Cube("Gloss",head,new(-.08f,.14f,.42f),new(.9f,.025f,.095f),silver);
        if(target) Cube("CenterStripe",head,new(0,.16f,-.05f),new(.42f,.03f,.09f),Material(color));
    }

    public static void DecorateNote(NoteController note)
    {
        if(instance == null || note == null || instance.discMesh == null) return;
        instance.DecorateHead(note.transform,LaneColors[(int)note.Lane],false);
        if (note.GetComponent<NoteVisual>() == null) note.gameObject.AddComponent<NoteVisual>();
    }

    private void BuildStage(Camera camera)
    {
        stageTexture = CreateStageTexture();
        Canvas background=RockUIStyle.Canvas("ConcertBackdrop",-100);
        background.renderMode=RenderMode.ScreenSpaceCamera; background.worldCamera=camera; background.planeDistance=90;
        GameObject image=new("ProceduralStage",typeof(RectTransform),typeof(RawImage));
        RockUIStyle.Rect(image,background.transform,Vector2.zero,Vector2.one);
        backgroundImage=image.GetComponent<RawImage>();
        backgroundImage.texture=stageTexture; backgroundImage.raycastTarget=false;
    }

    private static Texture2D CreateStageTexture()
    {
        const int width=960,height=540;
        Texture2D texture=new(width,height,TextureFormat.RGBA32,false); texture.name="Procedural concert ambience"; texture.wrapMode=TextureWrapMode.Clamp;
        Color32[] pixels=new Color32[width*height];
        for(int y=0;y<height;y++) for(int x=0;x<width;x++)
        {
            float u=(float)x/width,v=(float)y/height;
            float left=Mathf.Exp(-((u-.15f)*(u-.15f)*18+(v-.55f)*(v-.55f)*4));
            float right=Mathf.Exp(-((u-.88f)*(u-.88f)*22+(v-.5f)*(v-.5f)*5));
            Color color=new Color(.012f,.010f,.026f)+new Color(.095f,.019f,.17f)*left+new Color(.15f,.029f,.018f)*right;
            for(int j=0;j<6;j++)
            {
                float origin=.07f+j*.174f;
                float line=origin+(v-.92f)*(j%2==0 ? -.34f : .34f);
                float spread=.014f+(.95f-v)*.06f;
                float beam=Mathf.Exp(-Mathf.Pow((u-line)/spread,2))*Mathf.Clamp01((v-.14f)*1.1f);
                color+=new Color(.08f,.055f,.10f)*beam;
            }
            // Truss and backline speaker silhouettes, deliberately subdued.
            if(v>.865f && v<.89f) color*=.25f;
            if((u<.12f || u>.88f) && v>.13f && v<.57f)
            {
                color*=.35f;
                float cx=u<.5f ? .065f : .935f;
                for(int j=0;j<2;j++) { float d=Mathf.Sqrt(Mathf.Pow((u-cx)*1.78f,2)+Mathf.Pow(v-(.24f+j*.20f),2)); if(d>.060f && d<.065f) color+=new Color(.04f,.04f,.05f); }
            }
            float grain=((x*73+y*193)%97)/97f;
            color*=.91f+grain*.09f;
            float edge=Mathf.Clamp01(1.25f-((u-.5f)*(u-.5f)*1.6f+(v-.5f)*(v-.5f)*.8f));
            pixels[y*width+x]=color*edge;
        }
        texture.SetPixels32(pixels); texture.Apply(false,true); return texture;
    }

    private void OnDestroy()
    {
        if (SettingsManager.HasInstance) SettingsManager.Instance.Changed-=ApplyVisualSettings;
        if(score != null) score.ValuesChanged-=RefreshScore;
        if(rock != null) rock.ValueChanged-=RefreshRock;
        if(loader != null) loader.StateChanged-=RefreshSong;
        foreach(Material material in materials) Destroy(material);
        foreach(Mesh mesh in meshes) Destroy(mesh);
        if(stageTexture != null) Destroy(stageTexture);
        if(videoBackgroundMaterial != null) Destroy(videoBackgroundMaterial);
        if(instance == this) instance=null;
    }
}
