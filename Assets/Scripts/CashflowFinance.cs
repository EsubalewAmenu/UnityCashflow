using UnityEngine;
using UnityEngine.UI;

public partial class CashflowGame
{
    // Game bank rules: $1,000 principal increments, $100 monthly interest per increment.
    // Interest is an expense; principal is paid back separately from Finances.
    int LoanPayment(){return bankLoan/10;}
    int LoanNeeded(Deal deal){return Mathf.CeilToInt(Mathf.Max(0,deal.down-cash)/1000f)*1000;}
    void ShowCharityChoice()
    {
        int donation=Mathf.Max(100,Income()/10);bool resolved=false;
        var card=CardShell("CHARITY • YOUR CHOICE","Give a little. Make a difference.",C("#a56b26"),new Vector2(740,450));
        pendingDecision=new PendingDecision{kind="charity"};
        var copy=Label(card.transform,"Donate "+Money(donation)+" to roll two dice on your next roll.\nOr pass and continue with one die.",22);copy.color=C("#51445b");Place(copy.rectTransform,0,30,660,90);
        var amount=Label(card.transform,"YOUR DONATION  "+Money(donation),29);amount.color=C("#21756a");amount.fontStyle=FontStyle.Bold;Place(amount.rectTransform,0,-45,650,46);
        var balance=Label(card.transform,cash>=donation?"Cash after donating: "+Money(cash-donation):"Available cash: "+Money(cash)+" • Not enough to donate",16);balance.color=C("#6a6071");Place(balance.rectTransform,0,-99,650,30);
        var donate=Button(card.transform,"DONATE • 2 DICE",C("#21756a"),()=>
        {
            if(resolved||cash<donation)return;resolved=true;cash-=donation;charity=true;CloseModal();Refresh();Log("Donated "+Money(donation)+". Next roll: two dice.");
        });donate.name="Donate Charity";donate.interactable=cash>=donation;Place(donate.GetComponent<RectTransform>(),155,-165,310,54);SetButtonText(donate,Color.white,18);
        var pass=Button(card.transform,"PASS • 1 DIE",C("#e8dcc5"),()=>{if(resolved)return;resolved=true;charity=false;CloseModal();Log("Passed on charity. Next roll: one die.");});pass.name="Pass Charity";Place(pass.GetComponent<RectTransform>(),-180,-165,270,54);SetButtonText(pass,C("#51445b"),18);
    }
    void ShowLoanOffer(Deal deal,bool big)
    {
        int principal=LoanNeeded(deal),payment=principal/10;bool resolved=false;
        var card=CardShell("BANK LOAN • REVIEW BEFORE BUYING",deal.title,C("#654174"),new Vector2(900,590));
        pendingDecision=new PendingDecision{kind="loan",deal=deal,big=big};
        FinanceRow(card.transform,"Deal upfront payment",Money(deal.down),140);
        FinanceRow(card.transform,"Deal monthly income",Money(deal.flow)+" / mo",98);
        FinanceRow(card.transform,"Borrow",Money(principal),56);
        FinanceRow(card.transform,"Added monthly interest",Money(payment)+" / mo",14);
        FinanceRow(card.transform,"Current cash",Money(cash),-28);
        FinanceRow(card.transform,"Current monthly cashflow",Money(Income()-Expenses())+" / mo",-70);
        var terms=Label(card.transform,"Deal income is before this new loan's interest.\nGame rule: borrow in $1,000 steps at $100/month per step.\nInterest does not reduce the loan. Repay principal in Finances.",16);terms.color=C("#6a6071");Place(terms.rectTransform,0,-153,810,78);
        var accept=Button(card.transform,"BORROW & BUY",C("#654174"),()=>
        {
            if(resolved)return;resolved=true;
            // Recompute against the current balance, so accepting cannot overdraw cash.
            int loan=LoanNeeded(deal);PurchaseInvestment(deal,loan);
            CloseModal();CheckEscape();Refresh();Log("Borrowed "+Money(loan)+" and bought "+deal.title+". Loan interest: "+Money(LoanPayment())+"/mo.");PurchaseCelebration(deal);
        });accept.name="Confirm Loan";Place(accept.GetComponent<RectTransform>(),190,-245,350,54);SetButtonText(accept,Color.white,18);
        var pass=Button(card.transform,"PASS ON DEAL",C("#e8dcc5"),()=>{if(resolved)return;resolved=true;CloseModal();Log("Passed on "+deal.title+". No loan taken.");});pass.name="Pass Loan";Place(pass.GetComponent<RectTransform>(),-230,-245,270,54);SetButtonText(pass,C("#51445b"),18);
    }
    void ShowStatement()
    {
        var card=CardShell("YOUR MONEY • YOUR PROGRESS","Financial statement",C("#21756a"),new Vector2(800,590));
        AddHistoryButton(card);
        var cashBox=Panel(card.transform,Vector2.one*.5f,Vector2.one*.5f,new Vector2(340,78),C("#21756a"));Place(cashBox.GetComponent<RectTransform>(),-185,126,340,78);
        var cashText=Label(cashBox.transform,"CASH AVAILABLE\n"+Money(cash),23);Stretch(cashText.rectTransform);
        int net=Income()-Expenses();
        var flowBox=Panel(card.transform,Vector2.one*.5f,Vector2.one*.5f,new Vector2(340,78),C(net>=0?"#654174":"#a84036"));Place(flowBox.GetComponent<RectTransform>(),185,126,340,78);
        var flowText=Label(flowBox.transform,"MONTHLY CASHFLOW\n"+Money(net),23);Stretch(flowText.rectTransform);
        FinanceRow(card.transform,fastTrack?"Salary (retired)":"Salary",Money(fastTrack?0:profession.salary),53);
        FinanceRow(card.transform,"Passive income",Money(passive),11);
        FinanceRow(card.transform,"Living expenses",Money(Expenses()-LoanPayment()),-31);
        FinanceRow(card.transform,"Bank loan interest / month",Money(LoanPayment()),-73);
        FinanceRow(card.transform,"Outstanding bank loan",Money(bankLoan),-115);
        var expenses=Button(card.transform,"EXPENSE DETAILS",C("#e8dcc5"),ShowExpenseDetails);expenses.name="Expense Details";Place(expenses.GetComponent<RectTransform>(),-245,-170,220,44);SetButtonText(expenses,C("#51445b"),16);
        var debts=Button(card.transform,"YOUR DEBTS",C("#e8dcc5"),()=>ShowDebts(0));debts.name="Your Debts";Place(debts.GetComponent<RectTransform>(),0,-170,220,44);SetButtonText(debts,C("#51445b"),16);
        var borrow=Button(card.transform,"BORROW",C("#e8dcc5"),()=>ShowBankBorrow(1000));borrow.name="Borrow Money";Place(borrow.GetComponent<RectTransform>(),245,-170,220,44);SetButtonText(borrow,C("#51445b"),16);
        var repay=Button(card.transform,"REPAY BANK LOAN",C("#654174"),()=>ShowDebtRepayment(-1));repay.name="Repay Loan";repay.interactable=bankLoan>=1000;Place(repay.GetComponent<RectTransform>(),-245,-230,220,52);SetButtonText(repay,Color.white,16);
        var assets=Button(card.transform,"PORTFOLIO",C("#654174"),()=>ShowPortfolio(0));assets.name="Open Portfolio";Place(assets.GetComponent<RectTransform>(),0,-230,220,52);SetButtonText(assets,Color.white,18);
        var done=Button(card.transform,"BACK TO BOARD",C("#21756a"),CloseModal);done.name="Close Finances";Place(done.GetComponent<RectTransform>(),245,-230,220,52);SetButtonText(done,Color.white,18);
    }
    void FinanceRow(Transform parent,string caption,string value,float y)
    {
        var label=Label(parent,caption,18,TextAnchor.MiddleLeft);label.color=C("#6a6071");Place(label.rectTransform,-132,y,405,34);
        var amount=Label(parent,value,21,TextAnchor.MiddleRight);amount.color=C("#302540");amount.fontStyle=FontStyle.Bold;Place(amount.rectTransform,210,y,245,34);
    }
}
