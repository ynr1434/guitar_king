using UnityEngine;
using UnityEngine.UI;

// A beveled, clipped metal panel drawn as a tiny UI mesh rather than a bitmap.
[RequireComponent(typeof(CanvasRenderer))]
public sealed class RockPlateGraphic : MaskableGraphic
{
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        Rect rect = GetPixelAdjustedRect();
        if (rect.width <= 4 || rect.height <= 4) return;
        Polygon(vh, rect, new Color(.39f,.40f,.44f), new Color(.11f,.12f,.16f));
        rect.xMin+=2; rect.yMin+=2; rect.xMax-=2; rect.yMax-=2;
        Polygon(vh, rect, new Color(.085f,.082f,.096f,.98f), new Color(.019f,.020f,.029f,.98f));
        // Brushed metal: sparse, low contrast horizontal strokes.
        for (float y=rect.yMin+16; y<rect.yMax-14; y+=9)
        {
            int start=vh.currentVertCount;
            Color c=new(.28f,.27f,.33f,.065f);
            vh.AddVert(new Vector3(rect.xMin+16,y),c,Vector2.zero);
            vh.AddVert(new Vector3(rect.xMax-16,y),c,Vector2.zero);
            vh.AddVert(new Vector3(rect.xMax-16,y+1),c,Vector2.zero);
            vh.AddVert(new Vector3(rect.xMin+16,y+1),c,Vector2.zero);
            vh.AddTriangle(start,start+1,start+2); vh.AddTriangle(start,start+2,start+3);
        }
    }

    private static void Polygon(VertexHelper vh,Rect r,Color top,Color bottom)
    {
        float cut=Mathf.Min(18,Mathf.Min(r.width,r.height)*.12f);
        Vector2[] points={new(r.xMin+cut,r.yMin),new(r.xMax-cut,r.yMin),new(r.xMax,r.yMin+cut),new(r.xMax,r.yMax-cut),new(r.xMax-cut,r.yMax),new(r.xMin+cut,r.yMax),new(r.xMin,r.yMax-cut),new(r.xMin,r.yMin+cut)};
        int start=vh.currentVertCount;
        vh.AddVert(r.center,Color.Lerp(bottom,top,.5f),Vector2.zero);
        foreach(Vector2 p in points) vh.AddVert(p,Color.Lerp(bottom,top,(p.y-r.yMin)/r.height),Vector2.zero);
        for(int i=0;i<8;i++) vh.AddTriangle(start,start+1+i,start+1+(i+1)%8);
    }
}
