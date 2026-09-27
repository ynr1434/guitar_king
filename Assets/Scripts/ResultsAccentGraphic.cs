using UnityEngine;
using UnityEngine.UI;

// Lightweight vector decoration, used only by the successful results screen.
[RequireComponent(typeof(CanvasRenderer))]
public sealed class ResultsAccentGraphic : MaskableGraphic
{
    public enum Shape { Star, Glow, Beam, Vignette }
    public Shape shape;

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        Rect r = GetPixelAdjustedRect();
        if (shape == Shape.Vignette)
        {
            Vector2[] corners = { new(0,0), new(1,0), new(1,1), new(0,1) };
            Color clear = color; clear.a = 0;
            foreach (Vector2 corner in corners)
            {
                vh.AddVert(r.min + Vector2.Scale(corner,r.size), color, Vector2.zero);
                vh.AddVert(r.min + Vector2.Scale(Vector2.Lerp(corner,new(.5f,.5f),.30f),r.size), clear, Vector2.zero);
            }
            for (int i=0; i<4; i++)
            {
                int a=i*2, b=(i+1)%4*2;
                vh.AddTriangle(a,b,a+1); vh.AddTriangle(b,b+1,a+1);
            }
            return;
        }
        if (shape == Shape.Beam)
        {
            vh.AddVert(new Vector2(r.center.x, r.yMax), color, Vector2.zero);
            Color fade = color; fade.a = 0;
            vh.AddVert(new Vector2(r.xMin, r.yMin), fade, Vector2.zero);
            vh.AddVert(new Vector2(r.xMax, r.yMin), fade, Vector2.zero);
            vh.AddTriangle(0, 1, 2);
            return;
        }
        int count = shape == Shape.Star ? 10 : 48;
        vh.AddVert(r.center, color, Vector2.zero);
        for (int i = 0; i < count; i++)
        {
            float angle = Mathf.PI * .5f + i * Mathf.PI * 2 / count;
            float radius = shape == Shape.Star && i % 2 != 0 ? .45f : 1f;
            Vector2 p = r.center + new Vector2(Mathf.Cos(angle) * r.width, Mathf.Sin(angle) * r.height) * .5f * radius;
            Color edge = color;
            if (shape == Shape.Glow) edge.a = 0;
            else { float light = Mathf.Lerp(.65f, 1f, (p.y - r.yMin) / r.height); edge.r *= light; edge.g *= light; edge.b *= light; }
            vh.AddVert(p, edge, Vector2.zero);
        }
        for (int i = 0; i < count; i++) vh.AddTriangle(0, i + 1, (i + 1) % count + 1);
    }
}
