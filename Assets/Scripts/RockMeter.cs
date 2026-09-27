using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public sealed class RockMeter : MonoBehaviour
{
    [SerializeField, Range(0, 100)] private int currentValue = 70;
    [SerializeField] private int consecutiveMisses;
    [SerializeField] private bool isFailed;
    [SerializeField] private FailController failController;

    private Text label;
    private Image fillImage;

    public int CurrentValue => currentValue;
    public int ConsecutiveMisses => consecutiveMisses;
    public bool IsFailed => isFailed;
    public event Action ValueChanged;

    private void Awake()
    {
        currentValue = Mathf.Clamp(currentValue, 0, 100);
        consecutiveMisses = Mathf.Max(0, consecutiveMisses);
        BuildUI();

        if (failController == null)
            failController = FindFirstObjectByType<FailController>();
        if (failController == null)
        {
            GameObject failObject = new("FailController");
            failController = failObject.AddComponent<FailController>();
        }
        failController.Initialize(this);
        RefreshUI();
    }

    private void Update()
    {
#if UNITY_EDITOR
        if (!isFailed && !ResultsController.GameplayFinished && !PauseController.IsPaused &&
            !ChartRecorder.IsRecordingActive && Keyboard.current != null &&
            Keyboard.current.f9Key.wasPressedThisFrame)
        {
            ChangeValue(-25);
        }
#endif
    }

    public void RegisterResult(HitResult result)
    {
        if (isFailed || ResultsController.GameplayFinished)
            return;

        int change;
        switch (result)
        {
            case HitResult.Perfect:
                consecutiveMisses = 0;
                change = 4;
                break;
            case HitResult.Great:
                consecutiveMisses = 0;
                change = 3;
                break;
            case HitResult.Good:
                consecutiveMisses = 0;
                change = 1;
                break;
            default:
                consecutiveMisses++;
                change = -Mathf.Min(18, 12 + (consecutiveMisses - 1) * 2);
                break;
        }

        ChangeValue(change);
    }

    private void ChangeValue(int amount)
    {
        if (isFailed)
            return;

        currentValue = Mathf.Clamp(currentValue + amount, 0, 100);
        RefreshUI();
        ValueChanged?.Invoke();

        if (currentValue == 0)
        {
            isFailed = true;
            failController.TriggerFail();
            RefreshUI();
        }
    }

    private void BuildUI()
    {
        GameObject canvasObject = new(
            "RockMeterCanvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 12;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject panel = new("RockMeter", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(canvasObject.transform, false);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0f, 1f);
        panelRect.anchorMax = new Vector2(0f, 1f);
        panelRect.pivot = new Vector2(0f, 1f);
        panelRect.anchoredPosition = new Vector2(35f, -165f);
        panelRect.sizeDelta = new Vector2(390f, 76f);
        panel.GetComponent<Image>().color = new Color(0.015f, 0.02f, 0.035f, 0.88f);

        label = CreateText("RockMeterLabel", panel.transform, "ROCK  70", 24, FontStyle.Bold);
        RectTransform labelRect = label.rectTransform;
        labelRect.anchorMin = new Vector2(0f, 0.52f);
        labelRect.anchorMax = new Vector2(1f, 1f);
        labelRect.offsetMin = new Vector2(12f, 0f);
        labelRect.offsetMax = new Vector2(-12f, -2f);

        GameObject barBackground = new("BarBackground", typeof(RectTransform), typeof(Image));
        barBackground.transform.SetParent(panel.transform, false);
        RectTransform barRect = barBackground.GetComponent<RectTransform>();
        barRect.anchorMin = new Vector2(0f, 0f);
        barRect.anchorMax = new Vector2(1f, 0.48f);
        barRect.offsetMin = new Vector2(12f, 9f);
        barRect.offsetMax = new Vector2(-12f, -2f);
        barBackground.GetComponent<Image>().color = new Color(0.12f, 0.14f, 0.18f, 1f);

        GameObject fill = new("Fill", typeof(RectTransform), typeof(Image));
        fill.transform.SetParent(barBackground.transform, false);
        RectTransform fillRect = fill.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        fillImage = fill.GetComponent<Image>();
        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Horizontal;
        fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
        fillImage.color = new Color(0.2f, 0.9f, 0.35f);
    }

    private void RefreshUI()
    {
        if (label == null || fillImage == null)
            return;

        label.text = isFailed ? "ROCK  0  •  FAILED" : $"ROCK  {currentValue}";
        fillImage.fillAmount = currentValue / 100f;
        fillImage.color = currentValue >= 60
            ? new Color(0.2f, 0.9f, 0.35f)
            : currentValue >= 30
                ? new Color(1f, 0.72f, 0.12f)
                : new Color(1f, 0.2f, 0.18f);
    }

    private static Text CreateText(string objectName, Transform parent, string value,
        int fontSize, FontStyle style)
    {
        GameObject textObject = new(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        textObject.transform.SetParent(parent, false);
        Text text = textObject.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.text = value;
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.alignment = TextAnchor.MiddleLeft;
        text.color = Color.white;
        text.raycastTarget = false;
        return text;
    }
}
