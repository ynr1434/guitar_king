using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class ResultsButtonFeedback : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
    public Image accent;
    public Color highlightColor = new(1f, .69f, .25f);
    private bool hovered, selected;
    public void OnPointerEnter(PointerEventData e) => hovered = true;
    public void OnPointerExit(PointerEventData e) => hovered = false;
    public void OnSelect(BaseEventData e) => selected = true;
    public void OnDeselect(BaseEventData e) => selected = false;
    private void Update()
    {
        bool lit = hovered || selected;
        transform.localScale = Vector3.Lerp(transform.localScale, Vector3.one * (lit ? 1.025f : 1f), 1 - Mathf.Exp(-18 * Time.unscaledDeltaTime));
        if (accent != null) { Color c = highlightColor; c.a = lit ? 1f : .28f; accent.color = c; }
    }
    private void OnDisable() { hovered = selected = false; }
}
