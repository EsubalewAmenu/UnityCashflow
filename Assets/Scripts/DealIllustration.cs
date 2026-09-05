using UnityEngine;
using UnityEngine.UI;

/// <summary>Crisp, resolution-independent property art for both opportunity decks.</summary>
public sealed class DealIllustration : MaskableGraphic
{
    public bool big;
    void Quad(VertexHelper vh,float x,float y,float w,float h,Color c)
    {
        var r=rectTransform.rect;int n=vh.currentVertCount;
        vh.AddVert(new Vector3(r.x+x*r.width,r.y+y*r.height),c,Vector2.zero);
        vh.AddVert(new Vector3(r.x+x*r.width,r.y+(y+h)*r.height),c,Vector2.zero);
        vh.AddVert(new Vector3(r.x+(x+w)*r.width,r.y+(y+h)*r.height),c,Vector2.zero);
        vh.AddVert(new Vector3(r.x+(x+w)*r.width,r.y+y*r.height),c,Vector2.zero);
        vh.AddTriangle(n,n+1,n+2);vh.AddTriangle(n+2,n+3,n);
    }
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();Color cream=new Color(1,.92f,.74f),gold=new Color(.95f,.7f,.31f),glass=new Color(.3f,.65f,.72f);
        Quad(vh,.04f,.05f,.92f,.04f,gold);
        int count=big?3:1;
        for(int i=0;i<count;i++)
        {
            float x=big?.08f+i*.29f:.2f,w=big?.25f:.6f,h=big?.5f+i*.14f:.55f;
            Quad(vh,x,.09f,w,h,cream);Quad(vh,x,.09f,w*.14f,h,gold);
            Quad(vh,x-.03f,.09f+h,w+.06f,.06f,gold);
            for(int row=0;row<(big?3:2);row++)for(int col=0;col<2;col++)Quad(vh,x+w*(.24f+col*.4f),.22f+row*.15f,w*.2f,.085f,glass);
            Quad(vh,x+w*.42f,.09f,w*.2f,.11f,new Color(.27f,.23f,.36f));
        }
    }
}
