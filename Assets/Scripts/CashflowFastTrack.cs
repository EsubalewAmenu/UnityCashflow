using Cashflow.Domain;
using UnityEngine;
using UnityEngine.UI;

public partial class CashflowGame
{
    bool gameWon,fastTrackIntro;
    void ShowFastTrackIntro()
    {
        var card=CardShell("FAST TRACK","You escaped the Rat Race",C("#21756a"),new Vector2(900,590));
        pendingDecision=new PendingDecision{kind="intro"};
        BindCommandDecision(new Decision{kind=DecisionKind.Notice});var intent=CommandIntent(ActionKind.Acknowledge);
        var copy=Label(card.transform, "Your investment income exceeds your expenses.\nCash, investments and debts carry over. Salary stops.\nCollect net investment cashflow when passing or landing on Cashflow Day.\nBuy businesses to grow income, then buy "+dream+" for "+Money(dreamGoals[selectedDream])+" on a Dream space.",23);copy.color=C("#51445b");Place(copy.rectTransform,0,0,810,320);
        var done=Button(card.transform,"CONTINUE",C("#21756a"),()=>{if(DispatchGameplay(intent))CloseModal();});Place(done.GetComponent<RectTransform>(),0,-240,320,52);SetButtonText(done,Color.white,18);
    }
    void ResolveFastTrack(string tile)
    {
        if(tile=="CASHFLOW DAY")
        {
            return; // Movement owns both the payment and its non-blocking receipt.
        }
        if(tile=="DREAM"){ShowDreamPurchase();return;}
        if(tile.Contains("+"))
        {
            string[] parts=tile.Split('+');int income=int.Parse(parts[1].Replace("K",""))*1000;
            OfferDeal(new Deal(parts[0].Trim(),income*5,income,"This Fast Track business requires an upfront investment. Its income is added to your portfolio and collected on Cashflow Day."),true);return;
        }
        int loss=CashflowEventRules.FastTrackLoss(tile,cash);
        loss=Mathf.Max(0,loss);if(!SystemDecision(new Decision{kind=DecisionKind.Loss,cost=loss,required=true}))return;Log(tile+": paid "+Money(loss)+".");
        ShowEventPanel(tile,"One-time payment made: "+Money(loss)+"\n\n"+(tile=="TAX AUDIT"?"An audit costs 10% of available cash.":tile=="LAWSUIT"?"A legal settlement costs up to $25,000 from available cash.":tile=="DIVORCE"?"A settlement divides your available cash in half.":"A market correction costs 20% of available cash.")+"\nInvestments, debts and monthly cashflow are unchanged.",C("#a84036"));
    }
    bool PurchaseDream()
    {
        int cost=dreamGoals[selectedDream];
        if(!fastTrack||gameWon||cash<cost)return false;
        BindCommandDecision(new Decision{kind=DecisionKind.Dream,cost=cost});if(!DispatchGameplay(CommandIntent(ActionKind.BuyDream)))return false;Log("Dream purchased: "+dream+" for "+Money(cost)+".");return true;
    }
    void ShowDreamPurchase()
    {
        int cost=dreamGoals[selectedDream];
        var card=CardShell("YOUR DREAM • FINAL GOAL",dream,C("#654174"),new Vector2(900,590));
        pendingDecision=new PendingDecision{kind="dream"};
        BindCommandDecision(new Decision{kind=DecisionKind.Dream,cost=cost});var buyIntent=CommandIntent(ActionKind.BuyDream);var passIntent=CommandIntent(ActionKind.Pass);
        FinanceRow(card.transform,"Dream purchase price",Money(cost),120);
        FinanceRow(card.transform,"Available cash",Money(cash),65);
        FinanceRow(card.transform,"Cash after purchase",Money(cash-cost),10);
        var copy=Label(card.transform,cash>=cost?"You can now fund your chosen dream.\nConfirm the purchase to complete the game.":"You need "+Money(cost-cash)+" more cash.\nKeep building income and return to a Dream space when ready.",23);copy.color=C("#51445b");Place(copy.rectTransform,0,-95,800,110);
        bool resolved=false;
        var buy=Button(card.transform,"BUY DREAM & WIN",C("#654174"),()=>{if(resolved||!DispatchGameplay(buyIntent))return;resolved=true;CloseModal();Refresh();Win();});buy.name="Buy Dream";buy.interactable=cash>=cost;Place(buy.GetComponent<RectTransform>(),200,-235,350,54);SetButtonText(buy,Color.white,18);
        var pass=Button(card.transform,"KEEP PLAYING",C("#e8dcc5"),()=>{if(resolved||!DispatchGameplay(passIntent))return;resolved=true;CloseModal();});pass.name="Pass Dream";Place(pass.GetComponent<RectTransform>(),-210,-235,350,54);SetButtonText(pass,C("#51445b"),18);
    }
}
