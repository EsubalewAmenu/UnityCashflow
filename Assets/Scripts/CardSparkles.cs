using UnityEngine;
using UnityEngine.UI;

/// <summary>A short, non-interactive gold sparkle accent drawn without textures.</summary>
public sealed class CardSparkles : MaskableGraphic
{
    float age;
    public static void Create(Transform parent)
    {
        var go=new GameObject("Card sparkle accent",typeof(RectTransform),typeof(CanvasRenderer),typeof(CardSparkles));go.transform.SetParent(parent,false);
        var effect=go.GetComponent<CardSparkles>();effect.raycastTarget=false;
        effect.rectTransform.anchorMin=Vector2.zero;effect.rectTransform.anchorMax=Vector2.one;effect.rectTransform.offsetMin=effect.rectTransform.offsetMax=Vector2.zero;
    }
    void Update(){age+=Time.unscaledDeltaTime;SetVerticesDirty();if(age>1)Destroy(gameObject);}
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();var r=rectTransform.rect;
        for(int i=0;i<20;i++)
        {
            float a=i*Mathf.PI*2/20;Vector2 p=new Vector2(Mathf.Cos(a)*(r.width*.5f+age*25),Mathf.Sin(a)*(r.height*.5f+age*25));
            float size=(5+i%3*2)*Mathf.Sin(Mathf.Clamp01(age)*Mathf.PI);
            Color tint=new Color(1,.77f,.3f,1-age);int n=vh.currentVertCount;
            vh.AddVert(p,tint,Vector2.zero);
            for(int j=0;j<8;j++){float angle=j*Mathf.PI/4;vh.AddVert(p+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*size*(j%2==0?1:.25f),tint,Vector2.zero);}
            for(int j=0;j<8;j++)vh.AddTriangle(n,n+1+j,n+1+(j+1)%8);
        }
    }
}
