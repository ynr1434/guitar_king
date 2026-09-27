using UnityEngine;
using UnityEngine.UI;

// Shared presentation primitives. No gameplay state lives here.
public static class RockUIStyle
{
    public static readonly Color Gold = new(1f, .64f, .19f);
    public static readonly Color Steel = new(.30f, .34f, .42f);
    public static readonly Color Ink = new(.035f, .035f, .055f, .97f);

    public static RectTransform Rect(GameObject go, Transform parent, Vector2 min, Vector2 max)
    {
        go.transform.SetParent(parent, false);
        RectTransform r = go.GetComponent<RectTransform>();
        r.anchorMin = min; r.anchorMax = max;
        r.offsetMin = r.offsetMax = Vector2.zero;
        return r;
    }

    public static Image Box(Transform parent, string name, Vector2 min, Vector2 max, Color color)
    {
        GameObject go = new(name, typeof(RectTransform), typeof(Image));
        Rect(go, parent, min, max);
        Image image = go.GetComponent<Image>();
        image.color = color; image.raycastTarget = false;
        return image;
    }

    public static RectTransform Plate(Transform parent, string name, Vector2 min, Vector2 max, Color accent)
    {
        Image border = Box(parent, name, min, max, Steel);
        RectTransform panel = border.rectTransform;
        border.color = Color.clear;
        GameObject surface = new("MachinedMetal", typeof(RectTransform), typeof(RockPlateGraphic));
        Rect(surface,panel,Vector2.zero,Vector2.one);
        surface.GetComponent<RockPlateGraphic>().raycastTarget=false;
        Image accentLine = Box(panel, "Accent", new Vector2(0, 1), Vector2.one, accent);
        accentLine.rectTransform.pivot = new Vector2(.5f,1);
        accentLine.rectTransform.sizeDelta = new Vector2(0,4);
        Box(panel, "Bevel", new Vector2(.01f, .01f), new Vector2(.99f, .02f), new Color(.14f,.15f,.19f));
        for (int i = 0; i < 4; i++)
        {
            Image bolt = Box(panel, "SteelRivet", Vector2.zero, Vector2.zero, new Color(.44f,.46f,.49f));
            bolt.rectTransform.anchorMin = bolt.rectTransform.anchorMax = new Vector2(i % 2, i / 2);
            bolt.rectTransform.sizeDelta = new Vector2(4,4);
            bolt.rectTransform.anchoredPosition = new Vector2(i % 2 == 0 ? 8 : -8, i < 2 ? 8 : -8);
        }
        return panel;
    }

    public static Text Label(Transform parent, string name, string value, int size, Color color,
        Vector2 min, Vector2 max, TextAnchor alignment = TextAnchor.MiddleLeft)
    {
        GameObject go = new(name, typeof(RectTransform), typeof(Text));
        Rect(go, parent, min, max);
        Text t = go.GetComponent<Text>();
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize = size; t.fontStyle = FontStyle.Bold; t.color = color;
        t.text = value; t.alignment = alignment; t.raycastTarget = false;
        t.resizeTextForBestFit = true; t.resizeTextMinSize = Mathf.Max(12, size / 2); t.resizeTextMaxSize = size;
        return t;
    }

    public static Canvas Canvas(string name, int order)
    {
        GameObject go = new(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        Canvas canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = order;
        CanvasScaler scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920,1080); scaler.matchWidthOrHeight = .5f;
        return canvas;
    }

    // Restyle existing panels in place, retaining all references and button listeners.
    public static void StyleModal(GameObject panel, Color accent, string eyebrow)
    {
        if (panel == null) return;
        panel.GetComponent<Image>().color = new Color(.009f,.008f,.019f,.94f);
        RectTransform frame = Plate(panel.transform, "MetalFrame", new Vector2(.065f,.08f), new Vector2(.935f,.97f), accent);
        frame.SetAsFirstSibling();
        if (panel.name == "ResultsPanel")
        {
            Box(frame,"Divider",new Vector2(.51f,.26f),new Vector2(.511f,.72f),Steel);
            Box(frame,"TitleDivider",new Vector2(.04f,.77f),new Vector2(.96f,.771f),Steel);
        }
        Label(frame, "Eyebrow", eyebrow, 16, accent, new Vector2(.04f,.93f), new Vector2(.28f,.975f));
        foreach (Text text in panel.GetComponentsInChildren<Text>(true))
        {
            text.fontStyle = FontStyle.Bold;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 15; text.resizeTextMaxSize = text.fontSize;
            if (text.name.Contains("Title")) text.color = accent;
            if (text.name == "CurrentRunScore") { text.fontSize=70; text.resizeTextMaxSize=70; text.color=Gold; }
            if (text.name == "CurrentRunStars") { text.fontSize=48; text.resizeTextMaxSize=48; }
        }
        foreach (Button button in panel.GetComponentsInChildren<Button>(true))
        {
            Image image = button.GetComponent<Image>();
            image.color = Color.white;
            ColorBlock colors = button.colors;
            colors.normalColor = new Color(.16f,.13f,.12f);
            colors.highlightedColor = accent * .8f;
            colors.selectedColor = accent * .65f;
            colors.pressedColor = accent;
            button.colors = colors;
            Box(button.transform, "ButtonEdge", new Vector2(0,0), new Vector2(.012f,1), accent);
            Outline outline = button.GetComponent<Outline>() ?? button.gameObject.AddComponent<Outline>();
            outline.effectColor = accent * .65f; outline.effectDistance = new Vector2(1,-1);
        }
    }
}
