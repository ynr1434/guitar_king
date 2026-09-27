using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public sealed class HitFeedbackUI : MonoBehaviour
{
    [SerializeField] private Text feedbackText;
    [SerializeField, Min(0.1f)] private float displayDuration = 0.45f;

    private Coroutine hideRoutine;

    private void Start()
    {
        if (feedbackText == null) return;
        RectTransform rect = feedbackText.rectTransform;
        rect.anchorMin = new Vector2(.755f, .56f);
        rect.anchorMax = new Vector2(.98f, .64f);
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        feedbackText.fontSize = 42;
        feedbackText.fontStyle = FontStyle.Bold;
        feedbackText.alignment = TextAnchor.MiddleCenter;
        feedbackText.resizeTextForBestFit = true;
        feedbackText.resizeTextMinSize = 22;
        feedbackText.resizeTextMaxSize = 42;
        Shadow shadow = feedbackText.GetComponent<Shadow>() ?? feedbackText.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0,0,0,.8f);
        shadow.effectDistance = new Vector2(2,-3);
    }

    public void Show(string message, Color color)
    {
        if (feedbackText == null)
            return;

        feedbackText.text = message;
        feedbackText.color = color;
        feedbackText.enabled = true;

        if (hideRoutine != null)
            StopCoroutine(hideRoutine);
        hideRoutine = StartCoroutine(HideAfterDelay());
    }

    private IEnumerator HideAfterDelay()
    {
        Color color = feedbackText.color;
        float age = 0;
        while (age < displayDuration)
        {
            if (!PauseController.IsPaused) age += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(age / displayDuration);
            feedbackText.rectTransform.anchoredPosition = new Vector2(0, progress * 16);
            feedbackText.transform.localScale = Vector3.one * (1 + .18f * Mathf.Sin(progress * Mathf.PI));
            feedbackText.color = new Color(color.r,color.g,color.b,1-Mathf.Clamp01((progress-.4f)/.6f));
            yield return null;
        }
        feedbackText.enabled = false;
        hideRoutine = null;
    }
}
