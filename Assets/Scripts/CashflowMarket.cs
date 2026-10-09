using Cashflow.Domain;
using UnityEngine;
using UnityEngine.UI;

public partial class CashflowGame
{
    void ShowMarketOffer()
    {
        if(portfolio.Count==0)
        {
            ShowEventPanel("THE MARKET","Buyers are looking for investments. You do not own any yet.\n\nBuy a deal to become eligible for a future sale offer. No money changed hands.",C("#21756a"));
            return;
        }
        var asset=portfolio[Random.Range(0,portfolio.Count)];
        int market=Random.Range(0,3);
        // Offers purchase the player's investment interest, not an unmodelled
        // whole-property valuation. Price is fixed for this encounter.
        int price=asset.upfrontCost*(market==0?75:market==1?125:175)/100;
        string context=market==0?"Buyers are cautious. An investor has made a discounted offer for your investment.":
            market==1?"An investor wants the income from your investment and has offered more than your entry payment.":
            "Strong buyer interest has brought a premium offer for your investment.";
        ReviewMarketSale(asset,price,context);
    }

    bool SellInvestment(OwnedInvestment asset,int price)
    {
        if(asset==null||!portfolio.Contains(asset))return false;
        BindCommandDecision(new Decision{kind=DecisionKind.Market,assetId=asset.id,cost=price});
        return DispatchGameplay(CommandIntent(ActionKind.Sell));
    }

    void ReviewMarketSale(OwnedInvestment asset,int price,string context)
    {
        int debt=asset.outstandingBankLoan,net=price-debt;
        var card=CardShell("THE MARKET • BUYER OFFER",asset.title,C("#21756a"),new Vector2(900,590),true);
        pendingDecision=new PendingDecision{kind="market",assetId=asset.id,amount=price,detail=context};
        BindCommandDecision(new Decision{kind=DecisionKind.Market,assetId=asset.id,cost=price});var sellIntent=CommandIntent(ActionKind.Sell);var passIntent=CommandIntent(ActionKind.Pass);
        AddDecisionReview(card);
        var story=Label(card.transform,context+" You can sell this investment or keep collecting its income.",19,TextAnchor.MiddleLeft);
        story.color=C("#51445b");Place(story.rectTransform,0,135,810,70);
        FinanceRow(card.transform,"Buyer pays",Money(price),75);
        FinanceRow(card.transform,"Linked bank loan repaid",Money(debt),33);
        FinanceRow(card.transform,net>=0?"Cash you receive":"Cash you must contribute",Money(Mathf.Abs(net)),-9);
        FinanceRow(card.transform,"Cash after sale",Money(cash+net),-51);
        FinanceRow(card.transform,"Monthly income removed",Money(asset.monthlyIncome),-93);
        FinanceRow(card.transform,"Monthly cashflow after sale",Money(Income()-Expenses()-asset.monthlyIncome+debt/10),-135);
        bool affordable=cash+net>=0,resolved=false;
        var note=Label(card.transform,affordable?"Sells this one investment and clears its linked bank loan.\nPass to keep it. Other assets and debts stay with you.":"The offer will not cover the linked loan. You need "+Money(-cash-net)+" more cash to sell.\nPass to keep this investment and its income.",17);
        note.color=C(affordable?"#6a6071":"#a84036");Place(note.rectTransform,0,-188,810,55);
        var sell=Button(card.transform,"ACCEPT & SELL",C("#21756a"),()=>{
            if(resolved)return;
            if(!DispatchGameplay(sellIntent))return;
            resolved=true;CloseModal();CheckEscape();Refresh();
            Log("Sold "+asset.title+" for "+Money(price)+". Loan repaid: "+Money(debt)+".");
            ShowEventPanel("INVESTMENT SOLD",asset.title+"\nBuyer paid: "+Money(price)+"\nBank loan repaid: "+Money(debt)+"\nCash change: "+Money(net)+"\nMonthly investment income removed: "+Money(asset.monthlyIncome),C("#21756a"));
        });sell.name="Accept Market Sale";sell.interactable=affordable;Place(sell.GetComponent<RectTransform>(),200,-250,350,52);SetButtonText(sell,Color.white,18);
        var pass=Button(card.transform,"PASS • KEEP INVESTMENT",C("#e8dcc5"),()=>{if(resolved||!DispatchGameplay(passIntent))return;resolved=true;CloseModal();Log("Passed on the offer for "+asset.title+". Investment retained.");});
        pass.name="Pass Market Sale";Place(pass.GetComponent<RectTransform>(),-210,-250,370,52);SetButtonText(pass,C("#51445b"),16);
    }
}
