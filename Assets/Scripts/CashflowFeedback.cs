using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public partial class CashflowGame
{
    BoardEventReceipt eventReceipt;
    void CreateEventReceipt()
    {
        var panel=Panel(uiRoot,Vector2.one*.5f,Vector2.one*.5f,new Vector2(480,50),C("#21756a"));panel.name="Board Event Receipt";
        Place(panel.GetComponent<RectTransform>(),180,325,480,50);panel.GetComponent<Image>().sprite=RoundedSprite();panel.GetComponent<Image>().type=Image.Type.Sliced;panel.GetComponent<Image>().raycastTarget=false;
        eventReceipt=panel.AddComponent<BoardEventReceipt>();eventReceipt.Background=panel.GetComponent<Image>();
        eventReceipt.Title=Label(panel.transform,"",14);eventReceipt.Title.fontStyle=FontStyle.Bold;Place(eventReceipt.Title.rectTransform,0,10,460,23);
        eventReceipt.Detail=Label(panel.transform,"",11);Place(eventReceipt.Detail.rectTransform,0,-12,460,20);
    }
    void Receipt(string title,string detail,Color color){if(eventReceipt!=null)eventReceipt.Show(title,detail,color);}
    [System.Serializable] class HistoryEntry { public string message,changes; }
    readonly List<HistoryEntry> history=new List<HistoryEntry>();
    int historyCash,historyFlow,historyDebt,lastRoll;
    void ResetHistory(){history.Clear();historyCash=cash;historyFlow=Income()-Expenses();historyDebt=bankLoan;lastRoll=0;}
    string SignedMoney(int value){return (value>0?"+":"")+Money(value);}
    void RecordHistory(string message)
    {
        int flow=Income()-Expenses();
        history.Add(new HistoryEntry{message=message,changes="Cash "+SignedMoney(cash-historyCash)+" → "+Money(cash)+
            "  •  Monthly flow "+SignedMoney(flow-historyFlow)+" → "+Money(flow)+
            "  •  Bank debt "+SignedMoney(bankLoan-historyDebt)});
        historyCash=cash;historyFlow=flow;historyDebt=bankLoan;
    }
    void UpdateTurnStatus()
    {
        if(diceResult==null||rolling)return;
        diceResult.text=gameWon?"DREAM ACHIEVED":skipTurns>0?"SKIP TURNS: "+skipTurns:
            charity?"CHARITY ACTIVE\nNEXT: 2 DICE":(lastRoll>0?"ROLLED "+lastRoll+"\n":"")+"NEXT: 1 DIE";
    }
    void AddHistoryButton(GameObject card)
    {
        var button=Button(card.transform,"HISTORY",C("#e8dcc5"),()=>ShowHistory(0));button.name="Transaction History";
        Place(button.GetComponent<RectTransform>(),290,252,140,32);SetButtonText(button,C("#51445b"),13);
    }
    void ShowHistory(int page)
    {
        int pages=Mathf.Max(1,(history.Count+2)/3);page=Mathf.Clamp(page,0,pages-1);
        var card=CardShell("YOUR MONEY • ACTIVITY","Transaction history",C("#21756a"),new Vector2(900,590));
        for(int row=0;row<3;row++)
        {
            int index=history.Count-1-page*3-row;if(index<0)break;var entry=history[index];
            var panel=Panel(card.transform,Vector2.one*.5f,Vector2.one*.5f,new Vector2(810,110),C("#e8eee3"));Place(panel.GetComponent<RectTransform>(),0,112-row*123,810,110);
            var title=Label(panel.transform,entry.message,18,TextAnchor.MiddleLeft);title.color=C("#302540");Place(title.rectTransform,0,22,770,53);
            var changes=Label(panel.transform,entry.changes,15,TextAnchor.MiddleLeft);changes.color=C("#21756a");Place(changes.rectTransform,0,-30,770,45);
        }
        var prev=Button(card.transform,"NEWER",C("#e8dcc5"),()=>ShowHistory(page-1));prev.name="History Newer";prev.interactable=page>0;Place(prev.GetComponent<RectTransform>(),-310,-245,180,50);SetButtonText(prev,C("#51445b"),16);
        var next=Button(card.transform,"OLDER",C("#e8dcc5"),()=>ShowHistory(page+1));next.name="History Older";next.interactable=page+1<pages;Place(next.GetComponent<RectTransform>(),-105,-245,180,50);SetButtonText(next,C("#51445b"),16);
        var back=Button(card.transform,"BACK TO FINANCES",C("#21756a"),ShowStatement);back.name="Close History";Place(back.GetComponent<RectTransform>(),220,-245,330,50);SetButtonText(back,Color.white,18);
    }
    void AddDecisionReview(GameObject card)
    {
        var button=Button(card.transform,"REVIEW FINANCES",C("#e8dcc5"),()=>{
            var overlay=Panel(card.transform,Vector2.zero,Vector2.one,Vector2.zero,C("#fff5df"));overlay.name="Decision Finance Review";
            var heading=Label(overlay.transform,"YOUR FINANCES",28);heading.color=C("#302540");Place(heading.rectTransform,0,215,780,50);
            FinanceRow(overlay.transform,"Cash available",Money(cash),125);FinanceRow(overlay.transform,"Monthly income",Money(Income()),75);
            FinanceRow(overlay.transform,"Monthly expenses",Money(Expenses()),25);FinanceRow(overlay.transform,"Net monthly cashflow",Money(Income()-Expenses()),-25);
            FinanceRow(overlay.transform,"Owned investments",portfolio.Count.ToString(),-75);FinanceRow(overlay.transform,"Bank borrowing",Money(bankLoan),-125);
            var back=Button(overlay.transform,"BACK TO THIS OFFER",C("#21756a"),()=>{overlay.SetActive(false);Destroy(overlay);});back.name="Return To Offer";Place(back.GetComponent<RectTransform>(),0,-235,370,52);SetButtonText(back,Color.white,18);
        });button.name="Review Offer Finances";Place(button.GetComponent<RectTransform>(),320,252,195,32);SetButtonText(button,C("#51445b"),12);
    }
    void ShowUnexpectedExpense()
    {
        string[] titles={"CAR REPAIR","APPLIANCE REPLACEMENT","HOME REPAIR","FAMILY TRIP"};
        string[] reasons={"Your car needs an urgent repair.","A broken appliance needs replacing.","An unexpected home repair bill arrives.","A family trip costs more than planned."};
        int index=Random.Range(0,reasons.Length);
        ShowRequiredExpense(titles[index],reasons[index],Random.Range(100,1001),0);
    }
    void ShowRequiredExpense(string title,string reason,int cost,int turns)
    {
        int loan=Mathf.CeilToInt(Mathf.Max(0,cost-cash)/1000f)*1000;
        var card=CardShell("ONE-TIME BILL • REVIEW",title,C("#a84036"),new Vector2(900,590));
        pendingDecision=new PendingDecision{kind="bill",title=title,detail=reason,amount=cost,turns=turns};
        var story=Label(card.transform,reason,22);story.color=C("#51445b");Place(story.rectTransform,0,145,810,70);
        FinanceRow(card.transform,"One-time payment",Money(cost),80);FinanceRow(card.transform,"Available cash",Money(cash),35);
        FinanceRow(card.transform,"Required bank borrowing",Money(loan),-10);FinanceRow(card.transform,"Cash after payment",Money(cash+loan-cost),-55);
        FinanceRow(card.transform,"Added monthly interest",Money(loan/10),-100);
        string noteText=loan>0?"Pay this bill once. Borrowing covers the shortfall.\nLoan interest repeats monthly: $100 per $1,000 borrowed.":"Pay this bill once. Your monthly expenses stay the same.";
        if(turns>0)noteText+="\nYou will also skip "+turns+" turns.";
        var note=Label(card.transform,noteText,19);note.color=C("#51445b");Place(note.rectTransform,0,-175,810,80);
        bool resolved=false;var pay=Button(card.transform,loan>0?"BORROW & PAY":"PAY EXPENSE",C("#a84036"),()=>{
            if(resolved)return;resolved=true;
            int needed=Mathf.CeilToInt(Mathf.Max(0,cost-cash)/1000f)*1000;bankLoan+=needed;cash+=needed-cost;skipTurns=turns;
            CloseModal();Log(title+": paid "+Money(cost)+(needed>0?"; borrowed "+Money(needed):"")+(turns>0?"; skip "+turns+" turns.":"."));Refresh();world.Audio.Expense();
            Receipt(title+" • PAID "+Money(cost),needed>0?"Borrowed "+Money(needed)+" • Monthly interest +"+Money(needed/10):"One-time payment • Monthly cashflow unchanged",C("#873d3a"));
        });pay.name="Pay Required Expense";Place(pay.GetComponent<RectTransform>(),0,-245,400,54);SetButtonText(pay,Color.white,18);
    }
}
