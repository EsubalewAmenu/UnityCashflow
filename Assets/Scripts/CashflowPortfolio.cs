using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public partial class CashflowGame
{
    [Serializable]
    public class OwnedInvestment
    {
        public string id, title, description;
        public int upfrontCost, monthlyIncome, originalBankLoan, outstandingBankLoan;
    }

    [SerializeField] List<OwnedInvestment> portfolio = new List<OwnedInvestment>();

    void PurchaseInvestment(Deal deal,int loan)
    {
        portfolio.Add(new OwnedInvestment {
            id=Guid.NewGuid().ToString("N"),title=deal.title,description=deal.description,
            upfrontCost=deal.down,monthlyIncome=deal.flow,
            originalBankLoan=loan,outstandingBankLoan=loan
        });
        cash+=loan-deal.down;
        bankLoan+=loan;
        passive=0;
        foreach(var asset in portfolio)passive+=asset.monthlyIncome;
    }

    // The existing bank repayment pays the oldest investment loan first.
    void RepayInvestmentLoan(int amount)
    {
        int remaining=amount;
        foreach(var asset in portfolio)
        {
            int payment=Math.Min(remaining,asset.outstandingBankLoan);
            asset.outstandingBankLoan-=payment;remaining-=payment;
            if(remaining==0)break;
        }
        bankLoan-=amount;cash-=amount;
    }

    void ShowPortfolio(int page)
    {
        const int perPage=3;
        int pages=Math.Max(1,(portfolio.Count+perPage-1)/perPage);
        page=Mathf.Clamp(page,0,pages-1);
        var card=CardShell("YOUR INVESTMENTS","Portfolio",C("#21756a"),new Vector2(900,590));
        var summary=Label(card.transform,portfolio.Count+" investments  •  "+Money(passive)+" / month before bank interest",19);
        summary.color=C("#21756a");Place(summary.rectTransform,0,155,810,35);
        if(portfolio.Count==0)
        {
            var empty=Label(card.transform,"Your first investment starts here.\nBuy a deal to see its cost, income and bank loan in your portfolio.",23);
            empty.color=C("#51445b");Place(empty.rectTransform,0,10,780,140);
        }
        for(int i=0;i<perPage&&page*perPage+i<portfolio.Count;i++)
        {
            var asset=portfolio[page*perPage+i];int currentPage=page;
            var row=Button(card.transform,"",C("#e8eee3"),()=>ShowInvestment(asset,currentPage));
            row.name="Portfolio Asset "+asset.id;Place(row.GetComponent<RectTransform>(),0,75-i*95,810,85);
            var title=Label(row.transform,asset.title,23,TextAnchor.MiddleLeft);title.color=C("#302540");Place(title.rectTransform,-145,15,470,32);
            var income=Label(row.transform,"+"+Money(asset.monthlyIncome)+" / mo",22,TextAnchor.MiddleRight);income.color=C("#21756a");Place(income.rectTransform,255,15,250,32);
            var cost=Label(row.transform,"Paid upfront: "+Money(asset.upfrontCost)+"  •  Bank loan remaining: "+Money(asset.outstandingBankLoan)+"  •  View details →",16,TextAnchor.MiddleLeft);
            cost.color=C("#6a6071");Place(cost.rectTransform,0,-22,770,28);
            foreach(var graphic in row.GetComponentsInChildren<Graphic>())if(graphic!=row.targetGraphic)graphic.raycastTarget=false;
        }
        var note=Label(card.transform,"Each purchase is listed separately. Loan repayments reduce the oldest loan first.",16);note.color=C("#6a6071");Place(note.rectTransform,0,-175,810,32);
        var previous=Button(card.transform,"←",C("#e8dcc5"),()=>ShowPortfolio(page-1));previous.name="Portfolio Previous";previous.interactable=page>0;Place(previous.GetComponent<RectTransform>(),-340,-240,90,52);
        var count=Label(card.transform,(page+1)+" / "+pages,18);count.color=C("#51445b");Place(count.rectTransform,-220,-240,120,35);
        var next=Button(card.transform,"→",C("#e8dcc5"),()=>ShowPortfolio(page+1));next.name="Portfolio Next";next.interactable=page+1<pages;Place(next.GetComponent<RectTransform>(),-100,-240,90,52);
        var back=Button(card.transform,"BACK TO FINANCES",C("#21756a"),ShowStatement);back.name="Close Portfolio";Place(back.GetComponent<RectTransform>(),220,-240,320,52);SetButtonText(back,Color.white,18);
    }

    void ShowInvestment(OwnedInvestment asset,int page)
    {
        var card=CardShell("OWNED INVESTMENT",asset.title,C("#21756a"),new Vector2(900,590));
        var story=Label(card.transform,string.IsNullOrWhiteSpace(asset.description)?"This investment contributes income at each Payday.":asset.description,19,TextAnchor.MiddleLeft);
        story.color=C("#51445b");Place(story.rectTransform,0,137,810,88);
        FinanceRow(card.transform,"Paid upfront",Money(asset.upfrontCost),60);
        FinanceRow(card.transform,"Original bank borrowing",Money(asset.originalBankLoan),18);
        FinanceRow(card.transform,"Bank loan remaining",Money(asset.outstandingBankLoan),-24);
        FinanceRow(card.transform,"Monthly investment income",Money(asset.monthlyIncome),-66);
        FinanceRow(card.transform,"Income after bank interest",Money(asset.monthlyIncome-asset.outstandingBankLoan/10),-108);
        var note=Label(card.transform,"Entry payment records what you paid, not a current resale valuation.\nIncome is paid at Payday. A Market sale clears the linked loan and stops this income.",17);note.color=C("#6a6071");Place(note.rectTransform,0,-175,810,60);
        var back=Button(card.transform,"BACK TO PORTFOLIO",C("#21756a"),()=>ShowPortfolio(page));back.name="Back To Portfolio";Place(back.GetComponent<RectTransform>(),0,-245,350,52);SetButtonText(back,Color.white,18);
    }
}
