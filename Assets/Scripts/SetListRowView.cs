using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public sealed class SetListRowView : MonoBehaviour, ISelectHandler, IDeselectHandler, IPointerEnterHandler, IPointerExitHandler
{
    public Button Button { get; private set; }
    private RawImage marker;
    private Image focus;
    private Text arrow, title;
    private ScrollRect scroll;
    private bool selected, hovered, focused;

    public void Initialize(Button button,RawImage markerImage,Image focusImage,Text arrowText,Text titleText,ScrollRect list)
    {
        Button=button; marker=markerImage; focus=focusImage; arrow=arrowText; title=titleText; scroll=list; Refresh();
    }
    public void SetSelected(bool value) { selected=value; Refresh(); }
    private void Refresh()
    {
        if(marker==null) return;
        marker.enabled=selected;
        arrow.enabled=selected;
        focus.enabled=Button.interactable && (focused || hovered);
        title.color=selected ? SetListUIController.RedInk : Button.interactable ? SetListUIController.Ink : new Color(.55f,.35f,.3f);
    }
    public void OnSelect(BaseEventData data)
    {
        focused=true; Refresh();
        // Keep keyboard/controller focus visible inside the fixed paper viewport.
        if(scroll==null) return;
        Canvas.ForceUpdateCanvases();
        RectTransform row=(RectTransform)transform;
        float height=scroll.viewport.rect.height;
        float range=Mathf.Max(0,scroll.content.rect.height-height);
        float top=-row.anchoredPosition.y-row.rect.height*(1-row.pivot.y);
        float bottom=top+row.rect.height;
        Vector2 position=scroll.content.anchoredPosition;
        if(top<position.y) position.y=top;
        else if(bottom>position.y+height) position.y=bottom-height;
        position.y=Mathf.Clamp(position.y,0,range);
        scroll.StopMovement(); scroll.content.anchoredPosition=position;
    }
    public void OnDeselect(BaseEventData data) { focused=false; Refresh(); }
    public void OnPointerEnter(PointerEventData data) { hovered=true; Refresh(); }
    public void OnPointerExit(PointerEventData data) { hovered=false; Refresh(); }
}
