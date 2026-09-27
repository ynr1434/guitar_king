using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

// Welcome guide and reusable pause-menu help. Does not schedule or pause audio.
public sealed class GameplayTutorialController : MonoBehaviour
{
    public static bool IsTutorialOpen { get; private set; }
    private static bool welcomeShown;
    private bool isOpen;
    private readonly GameObject[] pages = new GameObject[4];
    private readonly Image[] steps = new Image[4];
    private Canvas canvas;
    private CanvasGroup cardGroup, pageGroup;
    private RectTransform card;
    private Text heading, counter, nextLabel;
    private Button back, next;
    private int page;
    private bool playRequested;
    private float cardAge, pageAge;
    private int lastActionFrame = -1;
    private static readonly Color Paper = new(.94f, .91f, .83f);
    private static readonly Color Muted = new(.59f, .62f, .69f);
    private static readonly Color[] Colors =
    {
        new(.16f,1f,.29f), new(1f,.12f,.18f), new(1f,.82f,.09f),
        new(.12f,.51f,1f), new(1f,.38f,.07f)
    };
    private static readonly string[] Titles = { "КАК ИГРАТЬ", "УПРАВЛЕНИЕ", "НОТЫ", "ДЕРЖИ РИТМ" };

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSession()
    {
        welcomeShown = false;
        IsTutorialOpen = false;
    }

    public static void ShowWelcomeOnce(GameObject owner)
    {
        if (welcomeShown) return;
        welcomeShown = true;
        owner.AddComponent<GameplayTutorialController>().Open();
    }

    public void Open(System.Action closed = null)
    {
        if (IsTutorialOpen) return;
        isOpen = IsTutorialOpen = true;
        StartCoroutine(ShowAndWait(closed));
    }

    private IEnumerator ShowAndWait(System.Action closed)
    {
        playRequested = false;
        cardAge = 0;
        lastActionFrame = Time.frameCount;
        if (canvas == null) Build();
        canvas.gameObject.SetActive(true);
        next.interactable = back.interactable = true;
        ShowPage(0);
        while (!playRequested) yield return null;

        // Consume the closing key before returning control to the parent menu.
        while (AnyGameplayKeyHeld()) yield return null;
        yield return null;
        canvas.gameObject.SetActive(false);
        isOpen = IsTutorialOpen = false;
        closed?.Invoke();
    }

    private static bool AnyGameplayKeyHeld()
    {
        Keyboard k = Keyboard.current;
        return k != null && (k.spaceKey.isPressed || k.enterKey.isPressed || k.numpadEnterKey.isPressed ||
            k.digit1Key.isPressed || k.digit2Key.isPressed || k.digit3Key.isPressed ||
            k.digit4Key.isPressed || k.digit5Key.isPressed || k.rKey.isPressed ||
            k.cKey.isPressed || k.pKey.isPressed || k.oKey.isPressed || k.escapeKey.isPressed);
    }

    private void Update()
    {
        if (canvas == null || !isOpen) return;
        cardAge += Time.unscaledDeltaTime;
        pageAge += Time.unscaledDeltaTime;
        float entry = Mathf.SmoothStep(0, 1, Mathf.Clamp01(cardAge / .22f));
        cardGroup.alpha = entry;
        card.localScale = Vector3.one * Mathf.Lerp(.95f, 1, entry);
        pageGroup.alpha = Mathf.Clamp01(pageAge / .18f);
        if (playRequested) return;
        Keyboard k = Keyboard.current;
        if (k == null) return;
        if (k.leftArrowKey.wasPressedThisFrame || k.aKey.wasPressedThisFrame) Back();
        else if (k.rightArrowKey.wasPressedThisFrame || k.dKey.wasPressedThisFrame ||
                 k.enterKey.wasPressedThisFrame || k.numpadEnterKey.wasPressedThisFrame ||
                 k.spaceKey.wasPressedThisFrame) Advance();
    }

    private void Advance()
    {
        if (playRequested || lastActionFrame == Time.frameCount) return;
        lastActionFrame = Time.frameCount;
        if (page < 3) ShowPage(page + 1);
        else
        {
            playRequested = true;
            next.interactable = back.interactable = false;
            nextLabel.text = "ГОТОВО";
        }
    }

    private void Back()
    {
        if (playRequested || page == 0 || lastActionFrame == Time.frameCount) return;
        lastActionFrame = Time.frameCount;
        ShowPage(page - 1);
    }

    private void ShowPage(int index)
    {
        page = index;
        pageAge = 0;
        for (int i = 0; i < 4; i++)
        {
            pages[i].SetActive(i == page);
            steps[i].color = i <= page ? RockUIStyle.Gold : new Color(.19f,.2f,.24f);
        }
        heading.text = Titles[page];
        counter.text = $"ОБУЧЕНИЕ     /     {page + 1:00} — 04";
        back.gameObject.SetActive(page > 0);
        nextLabel.text = page == 3 ? "ПОНЯТНО" : "ДАЛЕЕ  >";
        // Keyboard navigation is handled once here, not also by UI Submit.
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
    }

    private void Build()
    {
        if (EventSystem.current == null)
            new GameObject("TutorialEventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        canvas = RockUIStyle.Canvas("GameplayTutorialCanvas", 300);
        canvas.gameObject.AddComponent<GraphicRaycaster>();
        Image scrim = RockUIStyle.Box(canvas.transform, "Backdrop", Vector2.zero, Vector2.one, new(.008f,.006f,.018f,.82f));
        scrim.raycastTarget = true;
        Glow(canvas.transform, new(-.1f,.15f), new(.6f,1.15f), new(.44f,.09f,.72f,.26f));
        Glow(canvas.transform, new(.65f,-.25f), new(1.15f,.65f), new(1f,.29f,.06f,.20f));
        card = RockUIStyle.Plate(canvas.transform, "BackstageFieldGuide", new(.075f,.075f), new(.925f,.925f), RockUIStyle.Gold);
        cardGroup = card.gameObject.AddComponent<CanvasGroup>();
        RockUIStyle.Box(card,"EditorialRail",new(.0f,.02f),new(.015f,.998f),RockUIStyle.Gold);
        Label(card,"Brand","GUITAR KING  /  ЗА КУЛИСАМИ",19,RockUIStyle.Gold,.045f,.905f,.70f,.96f);
        counter = Label(card,"Page","",18,Muted,.69f,.905f,.95f,.96f,TextAnchor.MiddleRight);
        heading = Label(card,"Heading","",76,Paper,.045f,.755f,.95f,.90f);
        for(int i=0;i<4;i++) steps[i]=RockUIStyle.Box(card,"Progress"+i,new(.045f+i*.23f,.737f),new(.26f+i*.23f,.742f),Muted);
        GameObject content = new("Pages",typeof(RectTransform),typeof(CanvasGroup));
        RockUIStyle.Rect(content,card,new(.045f,.215f),new(.955f,.71f));
        pageGroup = content.GetComponent<CanvasGroup>();
        for(int i=0;i<4;i++)
        {
            pages[i]=new GameObject("Page"+(i+1),typeof(RectTransform));
            RockUIStyle.Rect(pages[i],content.transform,Vector2.zero,Vector2.one);
        }
        Goal(pages[0].transform);
        Controls(pages[1].transform);
        Notes(pages[2].transform);
        Survival(pages[3].transform);
        RockUIStyle.Box(card,"FooterRule",new(.045f,.185f),new(.955f,.187f),new(.25f,.26f,.29f));
        Label(card,"Song","ТВОЙ ВЫХОД НА СЦЕНУ",22,Paper,.045f,.11f,.50f,.17f);
        Label(card,"Artist","ПОДСКАЗКИ ВСЕГДА ДОСТУПНЫ В МЕНЮ ПАУЗЫ",14,Muted,.045f,.065f,.52f,.115f);
        back = MakeButton(card,"НАЗАД",new(.59f,.073f),new(.735f,.17f),false,Back);
        next = MakeButton(card,"ДАЛЕЕ  >",new(.755f,.073f),new(.955f,.17f),true,Advance);
        nextLabel = next.GetComponentInChildren<Text>();
        Label(canvas.transform,"KeyboardHint","A / ←  НАЗАД     ·     D / →  ДАЛЕЕ     ·     ENTER / ПРОБЕЛ  ПРОДОЛЖИТЬ",16,Muted,.1f,.014f,.9f,.06f,TextAnchor.MiddleCenter);
    }

    private static void Goal(Transform p)
    {
        Label(p,"Kicker","01  /  ПОКОРИ СЦЕНУ",20,RockUIStyle.Gold,0,.86f,.53f,1);
        Label(p,"Goal","Играй ноты, когда они\nдостигают линии попадания.",43,Paper,0,.49f,.53f,.85f);
        Label(p,"Strum","Зажми клавишу нужного цвета и нажми ПРОБЕЛ, чтобы сыграть ноту.",25,Paper,0,.27f,.53f,.47f);
        Label(p,"Combo","Собирай комбо, повышай множитель и не дай шкале рока опустеть.",23,Muted,0,.01f,.53f,.25f);
        Transform demo = Tile(p,"FretlineDemo",.59f,0,1,1);
        for(int i=0;i<5;i++)
        {
            float x=.1f+i*.17f;
            RockUIStyle.Box(demo,"Lane",new(x,.14f),new(x+.003f,.92f),new(.27f,.29f,.34f));
            Note(demo,x-.053f,.16f,.11f,.10f,Colors[i],false);
        }
        RockUIStyle.Box(demo,"HitLine",new(.04f,.195f),new(.95f,.20f),Paper);
        Note(demo,.217f,.47f,.11f,.10f,Colors[1],false);
        Note(demo,.557f,.74f,.11f,.10f,Colors[3],false);
        Label(demo,"Timing","ЗАЖМИ ЦИФРУ  +  ПРОБЕЛ",18,RockUIStyle.Gold,.05f,.01f,.95f,.12f,TextAnchor.MiddleCenter);
    }

    private static void Controls(Transform p)
    {
        string[] names={"ЗЕЛЁНЫЙ","КРАСНЫЙ","ЖЁЛТЫЙ","СИНИЙ","ОРАНЖЕВЫЙ"};
        for(int i=0;i<5;i++)
        {
            float x=i*.204f;
            Transform tile=Tile(p,names[i],x,.33f,x+.184f,.94f);
            RockUIStyle.Box(tile,"Stripe",new(0,.97f),Vector2.one,Colors[i]);
            Transform key=Tile(tile,"Keycap",.20f,.30f,.80f,.83f);
            RockUIStyle.Box(key,"KeyFace",new(.03f,.05f),new(.97f,.97f),Colors[i]*.7f);
            Label(key,"Number",(i+1).ToString(),65,Paper,0,0,1,1,TextAnchor.MiddleCenter);
            Label(tile,"Lane",names[i],20,Colors[i],.04f,.055f,.96f,.27f,TextAnchor.MiddleCenter);
        }
        Label(p,"Space","ПРОБЕЛ   /   СЫГРАТЬ НОТУ",31,Paper,.02f,.02f,.60f,.23f);
        Label(p,"Escape","ESC   /   ПАУЗА",25,Muted,.63f,.02f,1,.23f,TextAnchor.MiddleRight);
    }

    private static void Notes(Transform p)
    {
        string[] names={"ОДИНОЧНАЯ НОТА","АККОРД","ДЛИННАЯ НОТА","ДЛИННЫЙ АККОРД"};
        string[] copy={"Зажми нужную цифру\nи нажми ПРОБЕЛ.","Зажми все нужные цифры\nи нажми ПРОБЕЛ.","Сыграй ноту и держи клавишу\nдо конца её хвоста.","Удерживай все нужные цифры\nдо конца хвостов аккорда."};
        for(int i=0;i<4;i++)
        {
            float x=i*.255f;
            Transform tile=Tile(p,names[i],x,0,x+.235f,1);
            bool chord=i==1||i==3, sustain=i>=2;
            Note(tile,chord?.18f:.34f,.47f,.30f,.17f,Colors[0],sustain);
            if(chord) Note(tile,.54f,.47f,.30f,.17f,Colors[2],sustain);
            Label(tile,"Name",names[i],23,Paper,.06f,.26f,.94f,.42f,TextAnchor.MiddleCenter);
            Label(tile,"Description",copy[i],20,Muted,.06f,.04f,.94f,.27f,TextAnchor.MiddleCenter);
        }
    }

    private static void Survival(Transform p)
    {
        Transform left=Tile(p,"Timing",0,0,.48f,1);
        Label(left,"Quality","ТОЧНОСТЬ РЕШАЕТ",20,Muted,.06f,.83f,.94f,.98f);
        Label(left,"Perfect","PERFECT",43,RockUIStyle.Gold,.06f,.64f,.94f,.85f);
        Label(left,"GreatGood","GREAT    /    GOOD",27,Paper,.06f,.48f,.94f,.66f);
        Label(left,"Explain","PERFECT — максимум очков за точность.\nGREAT и GOOD — тоже успешные попадания.",22,Muted,.06f,.22f,.94f,.47f);
        Label(left,"Multiplier","x1   >   x2   >   x3   >   x4",36,RockUIStyle.Gold,.06f,.01f,.94f,.22f);
        Label(p,"Combo","Собирай комбо, чтобы повысить множитель.",24,Paper,.54f,.78f,1,1);
        Label(p,"Rock","ШКАЛА РОКА",23,RockUIStyle.Gold,.54f,.62f,1,.79f);
        for(int i=0;i<20;i++) RockUIStyle.Box(p,"Meter"+i,new(.54f+i*.023f,.49f),new(.558f+i*.023f,.59f),i<6?Colors[1]:i<12?Colors[2]:Colors[0]);
        Label(p,"Missing","Промахи снижают уровень шкалы рока.",23,Muted,.54f,.29f,1,.47f);
        Label(p,"Zero","Если шкала опустеет:",23,Paper,.54f,.14f,1,.30f);
        Label(p,"Failed","ПЕСНЯ ПРОВАЛЕНА",39,Colors[1],.54f,0,1,.17f);
    }

    private static void Note(Transform p,float x,float y,float w,float h,Color color,bool sustain)
    {
        if(sustain)
        {
            RockUIStyle.Box(p,"TailGlow",new(x+w*.35f,y+h*.5f),new(x+w*.65f,.9f),new(color.r,color.g,color.b,.25f));
            RockUIStyle.Box(p,"TailCore",new(x+w*.46f,y+h*.5f),new(x+w*.54f,.9f),color);
        }
        GameObject go=new("NoteExample",typeof(RectTransform),typeof(TutorialNoteGraphic));
        RockUIStyle.Rect(go,p,new(x,y),new(x+w,y+h));
        TutorialNoteGraphic graphic=go.GetComponent<TutorialNoteGraphic>();
        graphic.color=color; graphic.raycastTarget=false;
    }

    private static Transform Tile(Transform p,string name,float x,float y,float xx,float yy)
    {
        Image tile=RockUIStyle.Box(p,name,new(x,y),new(xx,yy),new(.055f,.058f,.077f,.96f));
        RockUIStyle.Box(tile.transform,"TopBevel",new(0,.994f),Vector2.one,new(.28f,.29f,.34f));
        return tile.transform;
    }

    private static Text Label(Transform p,string name,string text,int size,Color color,float x,float y,float xx,float yy,TextAnchor align=TextAnchor.MiddleLeft)
        => RockUIStyle.Label(p,name,text,size,color,new(x,y),new(xx,yy),align);

    private static void Glow(Transform p,Vector2 min,Vector2 max,Color color)
    {
        GameObject go=new("AmbientGlow",typeof(RectTransform),typeof(ResultsAccentGraphic));
        RockUIStyle.Rect(go,p,min,max);
        ResultsAccentGraphic graphic=go.GetComponent<ResultsAccentGraphic>();
        graphic.shape=ResultsAccentGraphic.Shape.Glow; graphic.color=color; graphic.raycastTarget=false;
    }

    private static Button MakeButton(Transform p,string caption,Vector2 min,Vector2 max,bool primary,UnityEngine.Events.UnityAction action)
    {
        Image image=RockUIStyle.Box(p,caption,min,max,Color.white);
        image.raycastTarget=true;
        Button button=image.gameObject.AddComponent<Button>();
        button.targetGraphic=image;
        ColorBlock colors=button.colors;
        colors.normalColor=primary?RockUIStyle.Gold:new(.14f,.15f,.19f);
        colors.highlightedColor=primary?new(1f,.82f,.43f):new(.28f,.29f,.34f);
        colors.pressedColor=primary?new(.8f,.38f,.08f):new(.08f,.09f,.12f);
        button.colors=colors;
        Navigation navigation=button.navigation; navigation.mode=Navigation.Mode.None; button.navigation=navigation;
        Label(image.transform,"Label",caption,29,primary?new(.07f,.04f,.02f):Paper,.05f,0,.95f,1,TextAnchor.MiddleCenter);
        button.onClick.AddListener(action);
        return button;
    }

    private void OnDestroy()
    {
        if (isOpen) IsTutorialOpen=false;
        if(canvas!=null) Destroy(canvas.gameObject);
    }
}
