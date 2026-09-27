using UnityEngine;
using UnityEngine.UI;

// UI counterpart of the existing note's chrome housing, neon rim, dark
// recess and gloss stripe. No gameplay note or collider is instantiated.
public sealed class TutorialNoteGraphic : MaskableGraphic
{
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        Rect r=GetPixelAdjustedRect();
        Disc(vh,r,1f,new(.012f,.014f,.02f));
        Disc(vh,r,.91f,new(.65f,.70f,.77f));
        Disc(vh,r,.78f,color);
        Disc(vh,r,.57f,new(color.r*.24f,color.g*.24f,color.b*.24f,color.a));
        int n=vh.currentVertCount;
        Vector2 a=r.center+new Vector2(-r.width*.24f,r.height*.17f);
        Vector2 b=a+new Vector2(r.width*.48f,r.height*.055f);
        Color gloss=new(.93f,.96f,1f,.85f);
        vh.AddVert(a,gloss,Vector2.zero); vh.AddVert(new(b.x,a.y),gloss,Vector2.zero);
        vh.AddVert(b,gloss,Vector2.zero); vh.AddVert(new(a.x,b.y),gloss,Vector2.zero);
        vh.AddTriangle(n,n+1,n+2); vh.AddTriangle(n,n+2,n+3);
    }

    private static void Disc(VertexHelper vh,Rect r,float radius,Color color)
    {
        int start=vh.currentVertCount;
        vh.AddVert(r.center,color,Vector2.zero);
        for(int i=0;i<48;i++)
        {
            float angle=i*Mathf.PI*2/48;
            vh.AddVert(r.center+new Vector2(Mathf.Cos(angle)*r.width,Mathf.Sin(angle)*r.height)*(.5f*radius),color,Vector2.zero);
        }
        for(int i=0;i<48;i++) vh.AddTriangle(start,start+i+1,start+(i+1)%48+1);
    }
}
