using UnityEngine;
using UnityEngine.UI;

public partial class CashflowGame
{
    GameObject CardShell(string category,string title,Color accent,Vector2 size,bool flip=false)
    {
        HideHover();
        var card=Modal(size);card.name="Event Card";
        card.GetComponent<Image>().color=C("#fff5df");
        var shadow=card.AddComponent<Shadow>();shadow.effectColor=new Color(.04f,.02f,.09f,.5f);shadow.effectDistance=new Vector2(0,-12);
        var border=card.AddComponent<Outline>();border.effectColor=C("#dfbd76");border.effectDistance=new Vector2(2,2);
        var ribbon=Panel(card.transform,Vector2.one*.5f,Vector2.one*.5f,new Vector2(size.x-24,8),accent);
        Place(ribbon.GetComponent<RectTransform>(),0,size.y/2-13,size.x-24,8);
        var tag=Label(card.transform,category,13);tag.color=accent;tag.fontStyle=FontStyle.Bold;Place(tag.rectTransform,0,size.y/2-42,size.x-50,22);
        var heading=Label(card.transform,title,29);heading.color=C("#302540");heading.fontStyle=FontStyle.Bold;Place(heading.rectTransform,0,size.y/2-85,size.x-60,48);
        card.AddComponent<CardReveal>().Play(world!=null&&world.ReducedMotion,flip);
        if(world!=null&&world.Audio!=null)world.Audio.Card();
        return card;
    }
    void ShowDealChoice()
    {
        var card=CardShell("OPPORTUNITY","Choose your deal",C("#a56b26"),new Vector2(800,480));
        pendingDecision=new PendingDecision{kind="choice"};
        var hint=Label(card.transform,"Pick a deck. Review the opportunity, then buy or pass.",17);hint.color=C("#6a6071");Place(hint.rectTransform,0,105,720,30);
        DealDeck(card.transform,false,-185);DealDeck(card.transform,true,185);
    }
    void DealDeck(Transform parent,bool big,float x)
    {
        Color accent=C(big?"#654174":"#21756a");
        bool chosen=false;
        var button=Button(parent,"",accent,()=>
        {
            if(chosen)return;chosen=true;
            OfferDeal(DrawDeal(big),big);
        });
        button.gameObject.name=big?"Choose Big Deal":"Choose Small Deal";
        Place(button.GetComponent<RectTransform>(),x,-60,340,278);
        button.GetComponent<Image>().sprite=RoundedSprite();button.GetComponent<Image>().type=Image.Type.Sliced;
        DealArt(button.transform,big,new Vector2(0,45),new Vector2(175,115));
        var title=Label(button.transform,big?"BIG DEAL":"SMALL DEAL",24);title.fontStyle=FontStyle.Bold;Place(title.rectTransform,0,-38,310,35);
        var cost=Label(button.transform,DealPriceRange(big),17);cost.color=C("#f5e4bc");Place(cost.rectTransform,0,-77,310,28);
        var draw=Label(button.transform,"DRAW A CARD  →",15);Place(draw.rectTransform,0,-112,300,25);
        foreach(var graphic in button.GetComponentsInChildren<Graphic>())if(graphic!=button.targetGraphic)graphic.raycastTarget=false;
    }
    void DealArt(Transform parent,bool big,Vector2 position,Vector2 size)
    {
        var go=new GameObject("Deal illustration",typeof(RectTransform),typeof(CanvasRenderer),typeof(DealIllustration));go.transform.SetParent(parent,false);
        var art=go.GetComponent<DealIllustration>();art.big=big;art.raycastTarget=false;Place(art.rectTransform,position.x,position.y,size.x,size.y);
    }
    void OfferDeal(Deal deal,bool big)
    {
        Color accent=C(big?"#654174":"#21756a");
        var card=CardShell(big?"BIG DEAL • OPPORTUNITY":"SMALL DEAL • OPPORTUNITY",deal.title,accent,new Vector2(900,590),true);
        pendingDecision=new PendingDecision{kind="deal",deal=deal,big=big};
        AddDecisionReview(card);
        AddDealExplanation(card,deal);
        var story=Label(card.transform,string.IsNullOrWhiteSpace(deal.description)?"Review the upfront payment and monthly income before deciding whether this investment fits your budget.":deal.description,19,TextAnchor.MiddleLeft);
        story.name="Deal Explanation";story.color=C("#51445b");Place(story.rectTransform,0,137,810,88);
        DealFigure(card.transform,"PAY UPFRONT",Money(deal.down),-210,53,accent);
        DealFigure(card.transform,deal.flow==0?"NO MONTHLY INCOME":"ADDED MONTHLY CASHFLOW",Money(deal.flow)+" / mo",210,53,C("#21756a"));
        bool affordable=cash>=deal.down, resolved=false;
        int loan=affordable?0:LoanNeeded(deal),interest=loan/10;
        var preview=Panel(card.transform,Vector2.one*.5f,Vector2.one*.5f,new Vector2(810,105),C("#e8eee3"));Place(preview.GetComponent<RectTransform>(),0,-48,810,105);
        var current=Label(preview.transform,"Current cash: "+Money(cash)+
            "\nCurrent monthly cashflow: "+Money(Income()-Expenses()),21,TextAnchor.MiddleLeft);
        current.name="Current Deal Finances";current.color=C("#302540");Place(current.rectTransform,0,9,770,62);
        var funding=Label(preview.transform,affordable?"Can be paid from your cash. No additional bank loan needed.":"Buying requires a "+Money(loan)+" bank loan at "+Money(interest)+" per month. Review before confirming.",16,TextAnchor.MiddleLeft);
        funding.name="Deal Funding";funding.color=C(affordable?"#21756a":"#a84036");Place(funding.rectTransform,0,-32,770,28);
        var terms=Label(card.transform,"Entry payment buys this investment; it is not a guaranteed resale value.\n"+(deal.flow==0?"No income at Payday. Your return depends on a future sale; profit is not guaranteed.":"Income is paid at Payday, before bank interest. Buying pays no income immediately.")+"\nA Market offer lets you sell. Passing costs nothing.",17,TextAnchor.MiddleLeft);
        terms.name="Deal Terms";terms.color=C("#6a6071");Place(terms.rectTransform,0,-156,810,90);
        var buy=Button(card.transform,affordable?"BUY DEAL":"BORROW & BUY...",accent,()=>
        {
            if(resolved)return;resolved=true;
            if(cash<deal.down){ShowLoanOffer(deal,big);return;}
            PurchaseInvestment(deal,0);CloseModal();Log("Bought "+deal.title+". Passive income +"+Money(deal.flow));CheckEscape();Refresh();PurchaseCelebration(deal);
        });buy.gameObject.name="Buy Deal";buy.interactable=true;Place(buy.GetComponent<RectTransform>(),190,-245,350,54);SetButtonText(buy,Color.white,18);
        var pass=Button(card.transform,"PASS",C("#e8dcc5"),()=>{if(resolved)return;resolved=true;CloseModal();Log("Passed on "+deal.title);});pass.gameObject.name="Pass Deal";Place(pass.GetComponent<RectTransform>(),-230,-245,270,54);SetButtonText(pass,C("#51445b"),18);
    }
    void PurchaseCelebration(Deal deal)
    {
        world.Audio.Purchase();
        var toast=Panel(uiRoot,Vector2.one*.5f,Vector2.one*.5f,new Vector2(520,88),C("#21756a"));
        toast.name="Property Acquired";Place(toast.GetComponent<RectTransform>(),0,230,520,88);
        toast.GetComponent<Image>().sprite=RoundedSprite();
        var text=Label(toast.transform,"INVESTMENT ACQUIRED!\n"+deal.title+(deal.flow==0?"  •  No monthly income":"  •  +"+Money(deal.flow)+" / month"),20);Stretch(text.rectTransform);
        foreach(var graphic in toast.GetComponentsInChildren<Graphic>())graphic.raycastTarget=false;
        toast.AddComponent<CardReveal>().Play(world.ReducedMotion);
        if(!world.ReducedMotion)CardSparkles.Create(toast.transform);
        StartCoroutine(DismissPurchase(toast));
    }
    System.Collections.IEnumerator DismissPurchase(GameObject toast)
    {
        yield return new WaitForSecondsRealtime(2);
        if(toast!=null)Destroy(toast);
    }
    void DealFigure(Transform parent,string caption,string value,float x,float y,Color color)
    {
        var label=Label(parent,caption,13,TextAnchor.MiddleLeft);label.color=C("#6a6071");Place(label.rectTransform,x,y+15,390,23);
        var amount=Label(parent,value,29,TextAnchor.MiddleLeft);amount.color=color;amount.fontStyle=FontStyle.Bold;Place(amount.rectTransform,x,y-17,390,40);
    }
}
