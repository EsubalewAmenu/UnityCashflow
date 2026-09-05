using UnityEngine;
using UnityEngine.UI;

/// <summary>Scalable board illustrations, with no low-resolution icon sheet.</summary>
public sealed class BoardTileIcon : MaskableGraphic
{
    public string title;
    static readonly Color Ink=new Color(.19f,.18f,.25f),Paper=new Color(1,.91f,.68f);
    protected override void OnPopulateMesh(VertexHelper h)
    {
        h.Clear();string s=title??"";
        if(s.Contains("DREAM"))
        {
            Vector2 last=Vector2.zero;
            for(int i=0;i<=10;i++){float a=i*Mathf.PI/5+Mathf.PI/2;Vector2 p=new Vector2(Mathf.Cos(a),Mathf.Sin(a))*(i%2==0?11:5);if(i>0)Triangle(h,Vector2.zero,last,p,Paper);last=p;}
        }
        else if(s.Contains("MARKET"))
        {Rect(h,-10,-9,4,8,Ink);Rect(h,-3,-9,4,13,Ink);Rect(h,4,-9,4,19,Ink);Triangle(h,new Vector2(5,11),new Vector2(12,11),new Vector2(12,4),Paper);}
        else if(s.Contains("CHARITY"))
        {Circle(h,new Vector2(-5,3),6,Paper);Circle(h,new Vector2(5,3),6,Paper);Triangle(h,new Vector2(-10,3),new Vector2(10,3),new Vector2(0,-11),Paper);}
        else if(s.Contains("BABY"))
        {Circle(h,Vector2.zero,10,Paper);Circle(h,new Vector2(-3,2),1,Ink);Circle(h,new Vector2(3,2),1,Ink);Rect(h,-3,-4,6,1.5f,Ink);}
        else if(s.Contains("PAYDAY")||s.Contains("CASHFLOW"))
        {Rect(h,-12,-8,24,16,Ink);Rect(h,-10,-6,20,12,Paper);Circle(h,Vector2.zero,4,Ink);}
        else if(s.Contains("OPPORTUNITY")||s.Contains("+"))
        {Rect(h,-5,5,10,5,Ink);Rect(h,-3,5,6,3,Paper);Rect(h,-12,-9,24,16,Ink);Rect(h,-10,-7,20,12,Paper);Rect(h,-2,-1,4,5,Ink);}
        else
        {Triangle(h,new Vector2(0,11),new Vector2(-12,-10),new Vector2(12,-10),Paper);Rect(h,-1.5f,-2,3,8,Ink);Circle(h,new Vector2(0,-6),1.5f,Ink);}
    }
    static void Rect(VertexHelper h,float x,float y,float w,float height,Color c)
    {int n=h.currentVertCount;h.AddVert(new Vector3(x,y),c,Vector2.zero);h.AddVert(new Vector3(x+w,y),c,Vector2.zero);h.AddVert(new Vector3(x+w,y+height),c,Vector2.zero);h.AddVert(new Vector3(x,y+height),c,Vector2.zero);h.AddTriangle(n,n+1,n+2);h.AddTriangle(n,n+2,n+3);}
    static void Triangle(VertexHelper h,Vector2 a,Vector2 b,Vector2 c,Color color)
    {int n=h.currentVertCount;h.AddVert(a,color,Vector2.zero);h.AddVert(b,color,Vector2.zero);h.AddVert(c,color,Vector2.zero);h.AddTriangle(n,n+1,n+2);}
    static void Circle(VertexHelper h,Vector2 center,float radius,Color color)
    {for(int i=0;i<24;i++){float a=i*Mathf.PI/12,b=(i+1)*Mathf.PI/12;Triangle(h,center,center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius,center+new Vector2(Mathf.Cos(b),Mathf.Sin(b))*radius,color);}}
}
