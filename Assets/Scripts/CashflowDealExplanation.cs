using UnityEngine;
using UnityEngine.UI;

public partial class CashflowGame
{
    void AddDealExplanation(GameObject card,Deal deal)
    {
        var button=Button(card.transform,"HOW THIS DEAL WORKS",C("#e8dcc5"),()=>{
            // Keep the original offer alive so reading never redraws or buys a deal.
            var overlay=Panel(card.transform,Vector2.zero,Vector2.one,Vector2.zero,C("#fff5df"));
            overlay.name="Deal Explanation Review";
            var heading=Label(overlay.transform,deal.title,28);heading.color=C("#302540");Place(heading.rectTransform,0,235,810,48);
            DealExplanationSection(overlay.transform,"WHAT YOU OWN",deal.description,165);
            DealExplanationSection(overlay.transform,"WHAT YOU PAY","Pay once for an investment interest. No extra mortgage is added. Borrowing creates monthly interest; principal must be repaid separately.",65);
            DealExplanationSection(overlay.transform,"WHEN YOU EARN",deal.flow==0?"This investment pays no monthly income. Any return comes from selling at a Market offer, which may be below your entry payment. Bank interest still applies if you borrow.":"Income is fixed and paid at Payday, not when you buy. No separate running-cost bills apply. Any bank interest is a separate expense.",-35);
            DealExplanationSection(overlay.transform,"HOW YOU SELL","Market offers can be below your entry payment. Selling stops the income and clears its linked loan; you cover any shortfall. Pass to keep it.",-135);
            var back=Button(overlay.transform,"BACK TO THIS OFFER",C("#21756a"),()=>{overlay.SetActive(false);Destroy(overlay);});
            back.name="Return From Deal Explanation";Place(back.GetComponent<RectTransform>(),0,-245,370,52);SetButtonText(back,Color.white,18);
        });
        button.name="Explain Deal";Place(button.GetComponent<RectTransform>(),-310,252,215,32);SetButtonText(button,C("#51445b"),12);
    }
    void DealExplanationSection(Transform parent,string title,string copy,float y)
    {
        var caption=Label(parent,title,14,TextAnchor.MiddleLeft);caption.color=C("#21756a");Place(caption.rectTransform,0,y,810,22);
        var body=Label(parent,copy,18,TextAnchor.MiddleLeft);body.color=C("#51445b");Place(body.rectTransform,0,y-44,810,66);
    }
}
