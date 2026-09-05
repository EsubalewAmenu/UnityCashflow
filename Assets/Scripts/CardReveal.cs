using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public sealed class CardReveal : MonoBehaviour
{
    public void Play(bool reducedMotion,bool flip=false){StartCoroutine(Reveal(reducedMotion,flip));}
    IEnumerator Reveal(bool reduced,bool flip)
    {
        var rect=(RectTransform)transform;var group=gameObject.AddComponent<CanvasGroup>();
        Vector2 destination=rect.anchoredPosition;group.interactable=false;
        GameObject back=null;
        if(flip&&!reduced)
        {
            back=new GameObject("Opportunity card back",typeof(RectTransform),typeof(Image));back.transform.SetParent(transform,false);
            var br=back.GetComponent<RectTransform>();br.anchorMin=Vector2.zero;br.anchorMax=Vector2.one;br.offsetMin=br.offsetMax=Vector2.zero;
            var bg=back.GetComponent<Image>();bg.sprite=GetComponent<Image>().sprite;bg.type=Image.Type.Sliced;bg.color=new Color(.83f,.48f,.16f);bg.raycastTarget=false;
            var label=new GameObject("Deck stamp",typeof(RectTransform),typeof(Text));label.transform.SetParent(back.transform,false);
            var lr=label.GetComponent<RectTransform>();lr.anchorMin=Vector2.zero;lr.anchorMax=Vector2.one;lr.offsetMin=lr.offsetMax=Vector2.zero;
            var text=label.GetComponent<Text>();text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");text.fontSize=40;text.fontStyle=FontStyle.Bold;text.alignment=TextAnchor.MiddleCenter;text.color=new Color(1,.92f,.72f);text.text="OPPORTUNITY\n\nCASHFLOW";text.raycastTarget=false;
        }
        float duration=reduced?.12f:flip?.78f:.48f;
        for(float time=0;time<duration;time+=Time.unscaledDeltaTime)
        {
            float t=Mathf.Clamp01(time/duration),ease=1-Mathf.Pow(1-t,3);group.alpha=Mathf.Clamp01(t*3);
            if(!reduced)
            {
                rect.anchoredPosition=destination+Vector2.down*(1-ease)*65;
                float width=flip?Mathf.Max(.015f,Mathf.Abs(Mathf.Cos(Mathf.Clamp01((t-.15f)/.7f)*Mathf.PI))):1;
                rect.localScale=new Vector3(width,1,1)*Mathf.Lerp(.86f,1,ease);
                rect.localRotation=Quaternion.Euler(0,(1-ease)*18,(1-ease)*-4);
                if(back!=null&&t>=.5f){Destroy(back);back=null;}
            }
            yield return null;
        }
        if(back!=null)Destroy(back);
        rect.anchoredPosition=destination;rect.localScale=Vector3.one;rect.localRotation=Quaternion.identity;group.alpha=1;group.interactable=true;
        if(flip&&!reduced)CardSparkles.Create(transform);
    }
}
