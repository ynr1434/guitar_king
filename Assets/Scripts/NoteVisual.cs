using UnityEngine;

// Presentation only: the existing tail transform supplies the exact timed length.
public sealed class NoteVisual : MonoBehaviour
{
    private NoteController note;
    private Transform tail;
    private LineRenderer trail;
    private Material material;
    private Color color;
    private bool wasHolding;

    private void Start()
    {
        note = GetComponent<NoteController>();
        tail = transform.Find("SustainTail");
        if (tail == null) return;
        color = FindFirstObjectByType<RhythmInputController>().GetLaneColor(note.Lane);
        tail.GetComponent<Renderer>().enabled = false;
        GameObject go = new("SustainLightTrail"); go.transform.SetParent(transform,false);
        trail = go.AddComponent<LineRenderer>();
        material = new Material(Resources.Load<Shader>("ConcertGlow"));
        // Render sustain glow after the translucent highway surface so depth-sorted
        // transparent geometry cannot make the moving trail appear to lag or dim.
        material.renderQueue = 3100;
        trail.sharedMaterial = material;
        trail.useWorldSpace = true; trail.positionCount = 2;
        trail.alignment = LineAlignment.View;
        trail.textureMode = LineTextureMode.Stretch;
        trail.numCapVertices = 4;
        SetColor(false);
    }

    private void SetColor(bool holding)
    {
        trail.startColor = new Color(color.r,color.g,color.b,holding ? 1f : .8f);
        trail.endColor = new Color(color.r,color.g,color.b,.05f);
        trail.startWidth = holding ? .46f : .34f;
        trail.endWidth = .20f;
    }

    private void LateUpdate()
    {
        if (trail == null || tail == null) return;
        if (wasHolding != note.IsSustainActive) { wasHolding=note.IsSustainActive; SetColor(wasHolding); }
        Vector3 half=tail.forward * tail.lossyScale.z*.5f;
        trail.SetPosition(0,tail.position-half+Vector3.up*.015f);
        trail.SetPosition(1,tail.position+half+Vector3.up*.015f);
    }

    private void OnDestroy() { if(material != null) Destroy(material); }
}
