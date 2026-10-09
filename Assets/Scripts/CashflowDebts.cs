using Cashflow.Domain;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public partial class CashflowGame
{
    [Serializable] public class PersonalDebt { public string title; public int balance,payment; }
    [SerializeField] List<PersonalDebt> personalDebts=new List<PersonalDebt>();
    void ResetPersonalDebts()
    {
        personalDebts.Clear();
        AddPersonalDebt("Home mortgage",profession.mortgageBalance,profession.mortgagePay);
        AddPersonalDebt("Student loan",profession.schoolBalance,profession.schoolPay);
        AddPersonalDebt("Car loan",profession.carBalance,profession.carPay);
        AddPersonalDebt("Credit card",profession.cardBalance,profession.cardPay);
        AddPersonalDebt("Retail debt",profession.retailBalance,profession.retailPay);
    }
    void AddPersonalDebt(string title,int balance,int payment){personalDebts.Add(new PersonalDebt{title=title,balance=balance,payment=payment});}
    int PersonalDebtPayments()
    {
        int total=0;foreach(var debt in personalDebts)if(debt.balance>0)total+=debt.payment;return total;
    }
    void DebtBack(Transform parent,Action action)
    {
        var back=Button(parent,"BACK",C("#e8dcc5"),action);back.name="Debt Back";Place(back.GetComponent<RectTransform>(),-230,-240,270,52);SetButtonText(back,C("#51445b"),18);
    }
    void ShowExpenseDetails()
    {
        var card=CardShell("MONTHLY EXPENSES","Where your money goes",C("#21756a"),new Vector2(900,590));
        FinanceRow(card.transform,"Taxes",Money(profession.taxes),150);
        for(int i=0;i<personalDebts.Count;i++)FinanceRow(card.transform,personalDebts[i].title,Money(personalDebts[i].balance>0?personalDebts[i].payment:0),114-i*36);
        FinanceRow(card.transform,"Other living costs",Money(profession.other),-66);
        FinanceRow(card.transform,"Children ("+children+")",Money(children*profession.childPay),-102);
        FinanceRow(card.transform,"Bank loan interest",Money(LoanPayment()),-138);
        FinanceRow(card.transform,"TOTAL MONTHLY EXPENSES",Money(Expenses()),-182);
        DebtBack(card.transform,ShowStatement);
    }
    void ShowDebts(int page)
    {
        int pages=(personalDebts.Count+3)/3;page=Mathf.Clamp(page,0,pages-1);
        var card=CardShell("LIABILITIES","Choose a debt to repay",C("#654174"),new Vector2(900,590));
        var hint=Label(card.transform,"Pay off a personal debt in full to remove its monthly payment.\nBank borrowing can be repaid in $1,000 steps.",19);hint.color=C("#51445b");Place(hint.rectTransform,0,140,810,60);
        for(int row=0;row<3;row++)
        {
            int index=page*3+row;if(index>personalDebts.Count)break;
            bool bank=index==personalDebts.Count;int target=bank?-1:index;
            int balance=bank?bankLoan:personalDebts[index].balance;
            int payment=bank?LoanPayment():(balance>0?personalDebts[index].payment:0);
            var button=Button(card.transform,(bank?"Bank borrowing":personalDebts[index].title)+"\n"+Money(balance)+" remaining  •  "+Money(payment)+" / month"+(balance==0?"  •  PAID OFF":""),C("#e8eee3"),()=>ShowDebtRepayment(target));
            button.name="Debt "+target;button.interactable=balance>0;Place(button.GetComponent<RectTransform>(),0,45-row*90,810,76);SetButtonText(button,C("#302540"),21);
        }
        DebtBack(card.transform,ShowStatement);
        var next=Button(card.transform,page==0?"MORE DEBTS →":"← FIRST PAGE",C("#654174"),()=>ShowDebts(page==0?1:0));next.name="Debt Page";Place(next.GetComponent<RectTransform>(),220,-240,320,52);SetButtonText(next,Color.white,18);
    }
    void ShowDebtRepayment(int index)
    {
        bool bank=index<0;var debt=bank?null:personalDebts[index];
        int amount=bank?Math.Min(1000,bankLoan):debt.balance;
        int reduction=bank?amount/10:debt.payment;
        var card=CardShell("REPAYMENT • REVIEW",bank?"Repay bank borrowing":debt.title,C("#654174"),new Vector2(900,590));
        FinanceRow(card.transform,"Pay now",Money(amount),130);
        FinanceRow(card.transform,"Cash after repayment",Money(cash-amount),80);
        FinanceRow(card.transform,"Monthly expenses",Money(Expenses())+" → "+Money(Expenses()-reduction),30);
        FinanceRow(card.transform,"Monthly cashflow",Money(Income()-Expenses())+" → "+Money(Income()-Expenses()+reduction),-20);
        FinanceRow(card.transform,"Debt remaining",Money(bank?bankLoan-amount:0),-70);
        var note=Label(card.transform,cash<amount?"You need "+Money(amount-cash)+" more cash. Nothing has been paid.":"Repaying reduces expenses, not investment income.\nYour next Payday reflects the lower payment.",20);note.color=C(cash<amount?"#a84036":"#21756a");Place(note.rectTransform,0,-150,810,75);
        var intent=CommandIntent(bank?ActionKind.RepayBank:ActionKind.RepayDebt,amount,bank?null:debt.title);
        bool resolved=false;var confirm=Button(card.transform,"CONFIRM REPAYMENT",C("#654174"),()=>{
            if(resolved||amount<=0||cash<amount||(bank?bankLoan<amount:debt.balance!=amount))return;
            if(!DispatchGameplay(intent))return;resolved=true;
            Log("Repaid "+(bank?"bank borrowing":debt.title)+": "+Money(amount)+". Monthly expenses reduced by "+Money(reduction)+".");CloseModal();CheckEscape();Refresh();ShowStatement();
        });confirm.name="Confirm Repayment";confirm.interactable=amount>0&&cash>=amount;Place(confirm.GetComponent<RectTransform>(),200,-240,360,52);SetButtonText(confirm,Color.white,18);
        DebtBack(card.transform,()=>ShowDebts(0));
    }
    void ShowBankBorrow(int amount)
    {
        amount=Mathf.Clamp(amount,1000,1000000);
        var card=CardShell("BANK BORROWING • REVIEW","Borrow money",C("#654174"),new Vector2(900,590));
        FinanceRow(card.transform,"Borrow",Money(amount),130);
        FinanceRow(card.transform,"Cash after borrowing",Money(cash+amount),80);
        FinanceRow(card.transform,"Added monthly interest",Money(amount/10),30);
        FinanceRow(card.transform,"Monthly cashflow after",Money(Income()-Expenses()-amount/10),-20);
        var terms=Label(card.transform,"Game rule: 10% interest per month, in $1,000 steps.\nInterest does not pay down the balance. Repay principal separately.",19);terms.color=C("#51445b");Place(terms.rectTransform,0,-90,810,65);
        var less=Button(card.transform,"− $1,000",C("#e8dcc5"),()=>ShowBankBorrow(amount-1000));less.name="Borrow Less";less.interactable=amount>1000;Place(less.GetComponent<RectTransform>(),-190,-160,300,45);SetButtonText(less,C("#51445b"),18);
        var more=Button(card.transform,"+ $1,000",C("#e8dcc5"),()=>ShowBankBorrow(amount+1000));more.name="Borrow More";more.interactable=amount<1000000;Place(more.GetComponent<RectTransform>(),190,-160,300,45);SetButtonText(more,C("#51445b"),18);
        var intent=CommandIntent(ActionKind.Borrow,amount);
        bool resolved=false;var confirm=Button(card.transform,"CONFIRM BORROWING",C("#654174"),()=>{if(resolved||!DispatchGameplay(intent))return;resolved=true;Log("Borrowed "+Money(amount)+". Monthly interest added: "+Money(amount/10)+".");Refresh();ShowStatement();});confirm.name="Confirm Borrowing";Place(confirm.GetComponent<RectTransform>(),200,-240,360,52);SetButtonText(confirm,Color.white,18);
        DebtBack(card.transform,ShowStatement);
    }
}
