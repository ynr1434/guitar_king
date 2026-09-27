using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Presentation only. All values and navigation callbacks are supplied by ResultsController.
public sealed class ResultsVisualController : MonoBehaviour
{
    private Color gold = new(1f, .73f, .28f);
    private bool failureTheme;
    private readonly Color muted = new(.52f, .53f, .58f);
    private CanvasGroup overlay, panelGroup, contentGroup, buttonsGroup;
    private RectTransform plate, content;
    private Text title, artist, difficulty, score, accuracy, combo, best, badge, preview;
    private readonly Text[] counts = new Text[4];
    private readonly RectTransform[] stars = new RectTransform[5];
    private readonly ResultsAccentGraphic[] fills = new ResultsAccentGraphic[5];
    private readonly ResultsAccentGraphic[] halos = new ResultsAccentGraphic[5];
    private ResultsAccentGraphic stageGlow;
    private Button replay, back;
    private bool shown;
    public GameObject Panel { get; private set; }

    public void Build(Action onReplay, Action onBack, bool failed = false)
    {
        failureTheme = failed;
        if (failed) gold = new Color(1f,.22f,.20f);
        Canvas canvas = RockUIStyle.Canvas(failed ? "FailCanvas" : "ResultsCanvas", failed ? 120 : 100);
        canvas.gameObject.AddComponent<GraphicRaycaster>();
        Panel = new GameObject(failed ? "FailPanel" : "ResultsPanel", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
        RockUIStyle.Rect(Panel, canvas.transform, Vector2.zero, Vector2.one);
        Panel.GetComponent<Image>().color = new Color(.004f, .005f, .012f, .78f);
        overlay = Panel.GetComponent<CanvasGroup>();
        Transform root = Panel.transform;
        stageGlow = Graphic(root, "AmberStageHaze", ResultsAccentGraphic.Shape.Glow, new(.12f,.13f), new(.9f,1.15f), new(1f,.35f,.08f,.18f));
        Graphic(root, "VioletHaze", ResultsAccentGraphic.Shape.Glow, new(-.3f,-.25f), new(.55f,.75f), failed ? new(.55f,.025f,.07f,.22f) : new(.28f,.17f,.6f,.22f));
        Graphic(root, "LeftStageLight", ResultsAccentGraphic.Shape.Beam, new(-.10f,-.2f), new(.40f,1.1f), failed ? new(.65f,.035f,.025f,.14f) : new(.52f,.37f,.22f,.14f));
        Graphic(root, "RightStageLight", ResultsAccentGraphic.Shape.Beam, new(.60f,-.2f), new(1.10f,1.1f), failed ? new(.5f,.02f,.07f,.12f) : new(.40f,.30f,.53f,.12f));
        Graphic(root, "EdgeVignette", ResultsAccentGraphic.Shape.Vignette, Vector2.zero, Vector2.one, new(0,0,0,.65f));
        Label(root, "Brand", "GUITAR KING   /   LIVE TOUR", 18, muted, new(.13f,.925f), new(.7f,.96f));
        Label(root, "Footer", failed ? "ONE MORE TRY. TAKE BACK THE STAGE." : "THE LIGHTS GO DOWN. THE RECORD STAYS.", 14, muted, new(.13f,.04f), new(.70f,.08f));
        Label(root, "EscapeHint", "ESC   /   SET LIST", 14, muted, new(.73f,.04f), new(.87f,.08f), TextAnchor.MiddleRight);
        RockUIStyle.Box(root, "PanelShadow", new(.138f,.083f), new(.878f,.89f), new(0,0,0,.4f));
        plate = RockUIStyle.Plate(root, "TourReportPlate", new(.13f,.10f), new(.87f,.90f), gold);
        panelGroup = plate.gameObject.AddComponent<CanvasGroup>();
        Label(plate, "Eyebrow", failed ? "SONG FAILED" : "SHOW RESULTS", failed ? 38 : 20, gold, new(.045f,.931f), new(.5f,.985f));
        Label(plate, "ReportNumber", failed ? "PERFORMANCE  /  CUT SHORT" : "PERFORMANCE  /  COMPLETE", 14, muted, new(.62f,.935f), new(.955f,.978f), TextAnchor.MiddleRight);
        Rule(plate, .925f);
        title = Label(plate, "SongTitle", "", 48, Color.white, new(.055f,.815f), new(.78f,.914f));
        artist = Label(plate, "Artist", "", 23, muted, new(.058f,.768f), new(.71f,.822f));
        difficulty = Label(plate, "Difficulty", "", 17, gold, new(.80f,.85f), new(.944f,.90f), TextAnchor.MiddleRight);
        badge = Label(plate, "NewBestStamp", "NEW BEST!", 25, gold, new(.78f,.773f), new(.95f,.836f), TextAnchor.MiddleCenter);
        badge.transform.localRotation = Quaternion.Euler(0,0,5);
        Outline badgeOutline = badge.gameObject.AddComponent<Outline>(); badgeOutline.effectColor = new(.6f,.2f,.03f,.5f); badgeOutline.effectDistance = new(2,-2);
        for (int i = 0; i < 5; i++)
        {
            float x = .29f + i * .105f;
            GameObject slot = new("RatingStar" + (i+1), typeof(RectTransform));
            stars[i] = RockUIStyle.Rect(slot, plate, new(x-.035f,.635f), new(x+.035f,.75f));
            halos[i] = Graphic(stars[i], "StarHalo", ResultsAccentGraphic.Shape.Glow, new(-.55f,-.55f), new(1.55f,1.55f), Color.clear);
            Graphic(stars[i], "MetalOutline", ResultsAccentGraphic.Shape.Star, Vector2.zero, Vector2.one, new(.40f,.39f,.36f));
            Graphic(stars[i], "EmptyStar", ResultsAccentGraphic.Shape.Star, new(.04f,.04f), new(.96f,.96f), new(.10f,.10f,.12f));
            fills[i] = Graphic(stars[i], "EarnedStar", ResultsAccentGraphic.Shape.Star, new(.025f,.025f), new(.975f,.975f), Color.clear);
        }
        GameObject stats = new("Statistics", typeof(RectTransform), typeof(CanvasGroup));
        content = RockUIStyle.Rect(stats, plate, Vector2.zero, Vector2.one);
        contentGroup = stats.GetComponent<CanvasGroup>();
        Label(content, "ScoreCaption", failed ? "ATTEMPT SCORE" : "FINAL SCORE", 18, gold, new(.06f,.536f), new(.60f,.58f));
        score = Label(content, "ScoreNumber", "0", 112, failed ? new(1f,.9f,.89f) : new(1f,.96f,.85f), new(.055f,.365f), new(.615f,.537f));
        Shadow scoreShadow = score.gameObject.AddComponent<Shadow>(); scoreShadow.effectColor = failed ? new(.8f,.03f,.04f,.3f) : new(.8f,.35f,.07f,.3f); scoreShadow.effectDistance = new(0,-3);
        Label(content, "AccuracyCaption", "ACCURACY", 16, muted, new(.06f,.319f), new(.32f,.363f));
        accuracy = Label(content, "AccuracyValue", "", 43, Color.white, new(.06f,.256f), new(.32f,.32f));
        Label(content, "ComboCaption", "MAX COMBO", 16, muted, new(.35f,.319f), new(.60f,.363f));
        combo = Label(content, "ComboValue", "", 43, Color.white, new(.35f,.256f), new(.60f,.32f));
        RockUIStyle.Box(content, "StatDivider", new(.637f,.268f), new(.638f,.575f), new(.3f,.3f,.34f,.45f));
        string[] names = { "PERFECT", "GREAT", "GOOD", "MISS" };
        Color[] colors = { new(.65f,.89f,.92f), new(.55f,.77f,.63f), new(.94f,.69f,.34f), new(.85f,.34f,.32f) };
        Label(content, "BreakdownCaption", "HIT BREAKDOWN", 16, muted, new(.68f,.536f), new(.94f,.58f));
        for (int i = 0; i < 4; i++)
        {
            float y = .465f - i*.062f;
            RockUIStyle.Box(content, names[i]+"Marker", new(.68f,y+.01f), new(.683f,y+.041f), colors[i]);
            Label(content, names[i]+"Label", names[i], 19, colors[i], new(.70f,y), new(.84f,y+.052f));
            counts[i] = Label(content, names[i]+"Count", "0", 28, Color.white, new(.845f,y), new(.94f,y+.052f), TextAnchor.MiddleRight);
        }
        RockUIStyle.Box(content, "BestRibbon", new(.045f,.153f), new(.955f,.233f), new(.012f,.014f,.02f,.7f));
        Label(content, "BestCaption", failed ? "RUN\nFAILED" : "PERSONAL\nBEST", 14, gold, new(.06f,.165f), new(.16f,.222f));
        best = Label(content, "BestRecords", "", 18, muted, new(.185f,.16f), new(.94f,.227f));
        preview = Label(plate, "DebugPreview", "", 13, muted, new(.60f,.585f), new(.944f,.62f), TextAnchor.MiddleRight);
        GameObject buttons = new("Actions", typeof(RectTransform), typeof(CanvasGroup));
        RockUIStyle.Rect(buttons, plate, Vector2.zero, Vector2.one);
        buttonsGroup = buttons.GetComponent<CanvasGroup>();
        replay = CreateButton(buttons.transform, failed ? "Retry" : "Replay", failed ? "RETRY" : "REPLAY", new(.06f,.04f), new(.475f,.122f), onReplay);
        back = CreateButton(buttons.transform, "BackToSetList", "BACK TO SET LIST", new(.505f,.04f), new(.94f,.122f), onBack);
        Navigation nav = new() { mode = Navigation.Mode.Explicit, selectOnRight = back, selectOnDown = back }; replay.navigation = nav;
        nav.selectOnLeft = replay; nav.selectOnUp = replay; nav.selectOnRight = null; nav.selectOnDown = null; back.navigation = nav;
        Panel.SetActive(false);
    }

    public void Show(string song, string performer, SongDifficulty level, ScoreManager values, int starCount, BestRecord record, bool newBest, bool debug, Action afterStars)
    {
        if (shown) return;
        shown = true;
        title.text = song.ToUpperInvariant(); artist.text = performer;
        difficulty.text = level.ToString().ToUpperInvariant();
        accuracy.text = $"{(values != null ? values.Accuracy : 0):0.0}%"; combo.text = (values != null ? values.MaxCombo : 0).ToString("N0");
        int[] hits = { values != null ? values.PerfectCount : 0, values != null ? values.GreatCount : 0, values != null ? values.GoodCount : 0, values != null ? values.MissCount : 0 };
        for (int i = 0; i < 4; i++) counts[i].text = hits[i].ToString("N0");
        best.text = failureTheme ? "ATTEMPT RATING ONLY   /   PERSONAL BESTS UNCHANGED" : $"SCORE  {record.Score:N0}     /     STARS  {record.Stars}/5     /     ACCURACY  {record.Accuracy:0.0}%     /     COMBO  {record.MaxCombo:N0}";
        badge.gameObject.SetActive(!failureTheme && newBest && !debug);
        preview.text = debug ? "PREVIEW  /  RECORDS NOT SAVED" : "";
        Panel.SetActive(true);
        StartCoroutine(Reveal(values != null ? values.Score : 0, Mathf.Clamp(starCount,1,5), afterStars));
    }

    private IEnumerator Reveal(int finalScore, int earned, Action afterStars)
    {
        float time = 0;
        bool notified = false, selected = false;
        buttonsGroup.interactable = false; buttonsGroup.blocksRaycasts = false;
        while (time < 1.75f)
        {
            time += Time.unscaledDeltaTime;
            overlay.alpha = Ease(time/.25f);
            panelGroup.alpha = Ease((time-.08f)/.27f);
            float entrance = Mathf.Clamp01((time-.08f)/.45f);
            plate.localScale = Vector3.one * Mathf.LerpUnclamped(.92f,1f,BackOut(entrance));
            for (int i=0; i<5; i++)
            {
                if (i>=earned) continue;
                float progress = Mathf.Clamp01((time-.36f-i*.18f)/.27f);
                fills[i].color = Color.Lerp(new Color(gold.r,gold.g,gold.b,0),gold,Mathf.Clamp01(progress*5));
                stars[i].localScale = Vector3.one * (progress < .6f ? Mathf.Lerp(.4f,1.23f,Ease(progress/.6f)) : Mathf.Lerp(1.23f,1f,Ease((progress-.6f)/.4f)));
                halos[i].color = new Color(1f,failureTheme ? .06f : .58f,failureTheme ? .08f : .12f,progress>0 ? .13f + Mathf.Sin(progress*Mathf.PI)*.38f : 0);
            }
            float reveal = Ease((time-.66f)/.38f);
            contentGroup.alpha = reveal; content.anchoredPosition = new(0,-16*(1-reveal));
            float count = Ease((time-.65f)/.85f);
            score.text = ((int)Math.Round(finalScore * (double)count)).ToString("N0");
            badge.transform.localScale = Vector3.one * (1 + Mathf.Sin(Mathf.Clamp01((time-.9f)/.5f)*Mathf.PI)*.12f);
            stageGlow.color = failureTheme ? new(1f,.025f,.045f,.16f*panelGroup.alpha) : new(1f,.35f,.08f,Mathf.Lerp(.08f,.24f,(earned-1)/4f)*panelGroup.alpha);
            buttonsGroup.alpha = Ease((time-1.12f)/.28f);
            if (!selected && time>=1.4f)
            {
                selected = true; buttonsGroup.interactable = buttonsGroup.blocksRaycasts = true;
                if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(replay.gameObject);
            }
            if (!notified && time>=.36f+(earned-1)*.18f+.27f)
            {
                notified = true; afterStars?.Invoke();
            }
            yield return null;
        }
        score.text = finalScore.ToString("N0");
    }

    private void OnDestroy() { if (Panel != null) Destroy(Panel.transform.parent.gameObject); }
    private static float Ease(float t) { t=Mathf.Clamp01(t); return t*t*(3-2*t); }
    private static float BackOut(float t) { float p=t-1; return 1+2.3f*p*p*p+1.3f*p*p; }
    private void Rule(Transform p,float y) => RockUIStyle.Box(p,"Hairline",new(.045f,y),new(.955f,y+.0015f),new(.3f,.3f,.34f,.5f));
    private Text Label(Transform p,string name,string value,int size,Color c,Vector2 min,Vector2 max,TextAnchor alignment=TextAnchor.MiddleLeft)
        => RockUIStyle.Label(p,name,value,size,c,min,max,alignment);
    private ResultsAccentGraphic Graphic(Transform p,string name,ResultsAccentGraphic.Shape shape,Vector2 min,Vector2 max,Color c)
    {
        GameObject go=new(name,typeof(RectTransform),typeof(ResultsAccentGraphic));
        RockUIStyle.Rect(go,p,min,max);
        ResultsAccentGraphic graphic=go.GetComponent<ResultsAccentGraphic>(); graphic.shape=shape; graphic.color=c; graphic.raycastTarget=false; return graphic;
    }
    private Button CreateButton(Transform p,string name,string caption,Vector2 min,Vector2 max,Action action)
    {
        RectTransform rect=RockUIStyle.Plate(p,name,min,max,gold);
        Image hit=rect.GetComponent<Image>(); hit.raycastTarget=true;
        Button button=rect.gameObject.AddComponent<Button>(); button.targetGraphic=hit; button.transition=Selectable.Transition.None;
        button.onClick.AddListener(()=>action());
        Image accent=RockUIStyle.Box(rect,"SelectionEdge",new(.005f,.13f),new(.012f,.87f),gold);
        Label(rect,"Label",caption,24,Color.white,new(.045f,.1f),new(.95f,.9f),TextAnchor.MiddleCenter);
        ResultsButtonFeedback feedback=rect.gameObject.AddComponent<ResultsButtonFeedback>(); feedback.accent=accent;
        if (failureTheme) feedback.highlightColor=gold;
        return button;
    }
}
