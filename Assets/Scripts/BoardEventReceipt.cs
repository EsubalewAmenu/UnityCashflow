using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// A separate, non-interactive receipt survives modal changes but belongs to this board.
public sealed class BoardEventReceipt : MonoBehaviour
{
    struct Receipt { public string title,detail; public Color color; }
    readonly Queue<Receipt> pending=new Queue<Receipt>();
    public Text Title,Detail;
    public Image Background;
    CanvasGroup group;
    float remaining;
    void Awake(){group=gameObject.AddComponent<CanvasGroup>();group.blocksRaycasts=false;group.interactable=false;group.alpha=0;}
    public void Show(string title,string detail,Color color){pending.Enqueue(new Receipt{title=title,detail=detail,color=color});if(remaining<=0)Next();}
    void Next(){
        if(pending.Count==0){group.alpha=0;return;}
        var receipt=pending.Dequeue();Title.text=receipt.title;Detail.text=receipt.detail;Background.color=receipt.color;
        group.alpha=1;remaining=6;transform.SetAsLastSibling();
    }
    void Update(){if(remaining<=0)return;remaining-=Time.unscaledDeltaTime;if(remaining<=0)Next();}
}
