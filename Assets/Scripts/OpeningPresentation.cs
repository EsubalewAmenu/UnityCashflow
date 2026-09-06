using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public sealed class OpeningBackdrop : MonoBehaviour
{
    void LateUpdate()
    {
        var rect=(RectTransform)transform;var parent=(RectTransform)rect.parent;var image=GetComponent<RawImage>();
        rect.anchorMin=rect.anchorMax=Vector2.one*.5f;
        float aspect=image.texture!=null?(float)image.texture.width/image.texture.height:16f/9;
        float width=Mathf.Max(parent.rect.width,parent.rect.height*aspect);rect.sizeDelta=new Vector2(width,width/aspect);
    }
}
public sealed class OpeningSwipe : MonoBehaviour,IBeginDragHandler,IEndDragHandler,IDragHandler
{
    public Action<int> Move;
    Vector2 start;
    public void OnBeginDrag(PointerEventData e){start=e.position;e.eligibleForClick=false;}
    public void OnDrag(PointerEventData e){e.eligibleForClick=false;}
    public void OnEndDrag(PointerEventData e){float distance=e.position.x-start.x;if(Mathf.Abs(distance)>Screen.width*.04f)Move?.Invoke(distance<0?1:-1);}
}
public sealed class OpeningCardEntrance : MonoBehaviour
{
    Vector2 destination;float age;RectTransform rect;
    public void Begin(int slot){rect=(RectTransform)transform;destination=rect.anchoredPosition;age=-Mathf.Abs(slot)*.025f;}
    void Update()
    {
        if(rect==null)return;age+=Time.unscaledDeltaTime;float t=Mathf.Clamp01(age/.24f);float ease=1-Mathf.Pow(1-t,3);
        rect.anchoredPosition=destination+Vector2.down*18*(1-ease);rect.localScale=Vector3.one*Mathf.Lerp(.97f,1,ease);
        if(t>=1)Destroy(this);
    }
}
