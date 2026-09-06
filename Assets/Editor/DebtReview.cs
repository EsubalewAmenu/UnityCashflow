using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad] public static class DebtReview
{
    static bool running;static int step;static double next;static int expenses;
    static readonly BindingFlags F=BindingFlags.NonPublic|BindingFlags.Instance;
    static CashflowGame G=>UnityEngine.Object.FindObjectOfType<CashflowGame>();
    static object Get(string n)=>typeof(CashflowGame).GetField(n,F).GetValue(G);
    static void Set(string n,object v)=>typeof(CashflowGame).GetField(n,F).SetValue(G,v);
    static object Call(string n,params object[] a)=>typeof(CashflowGame).GetMethod(n,F).Invoke(G,a);
    static void Click(string n)=>GameObject.Find(n).GetComponent<Button>().onClick.Invoke();
    static void Check(bool b,string m){if(!b)throw new Exception(m);}
    static DebtReview(){EditorApplication.update+=Tick;}
    static void Shot(string n)
    {
        Canvas.ForceUpdateCanvases();foreach(var t in GameObject.Find("Event Card").GetComponentsInChildren<Text>())Check(t.preferredHeight<=t.rectTransform.rect.height+2,"Clipped: "+t.text);
        ScreenCapture.CaptureScreenshot("work/debt-review/"+n+".png");
    }
    static void Tick()
    {
        if(EditorApplication.isCompiling)return;
        if(File.Exists("work/run-debt-review")){
            if(!EditorApplication.isPlaying){EditorApplication.isPlaying=true;return;}
            File.Delete("work/run-debt-review");Directory.CreateDirectory("work/debt-review");running=true;step=0;next=EditorApplication.timeSinceStartup+2;
        }
        if(!running||EditorApplication.timeSinceStartup<next)return;
        try {
            switch(step){
                case 0:
                    Screen.orientation=ScreenOrientation.LandscapeLeft;
                    foreach(var p in (CashflowGame.Profession[])Get("professions")){
                        Set("profession",p);Call("ResetPersonalDebts");Set("children",0);Set("bankLoan",0);
                        Check((int)Call("Expenses")==p.taxes+p.mortgagePay+p.schoolPay+p.carPay+p.cardPay+p.retailPay+p.other,"Starting expenses changed");
                    }
                    Call("StartGame");Set("cash",500000);expenses=(int)Call("Expenses");Call("ShowExpenseDetails");break;
                case 1:Shot("expenses");break;
                case 2:Call("ShowDebts",0);break;
                case 3:Shot("debts");break;
                case 4:Call("ShowDebtRepayment",2);break;
                case 5:Shot("repayment");break;
                case 6:
                    var d=((List<CashflowGame.PersonalDebt>)Get("personalDebts"))[2];int cost=d.balance,payment=d.payment;
                    Click("Debt Back");Check((int)Get("cash")==500000&&d.balance==cost,"Cancel changed finances");
                    Call("ShowDebtRepayment",2);var button=GameObject.Find("Confirm Repayment").GetComponent<Button>();button.onClick.Invoke();button.onClick.Invoke();
                    Check((int)Get("cash")==500000-cost&&d.balance==0&&(int)Call("Expenses")==expenses-payment,"Payoff or duplicate guard failed");
                    int before=(int)Get("cash");Call("Payday");Check((int)Get("cash")==before+(int)Call("Income")-(int)Call("Expenses"),"Payday after payoff failed");
                    Set("cash",0);Call("ShowDebtRepayment",0);Check(!GameObject.Find("Confirm Repayment").GetComponent<Button>().interactable,"Unaffordable repayment enabled");
                    Call("ShowBankBorrow",2000);break;
                case 7:Shot("borrow");break;
                case 8:
                    var borrow=GameObject.Find("Confirm Borrowing").GetComponent<Button>();borrow.onClick.Invoke();borrow.onClick.Invoke();
                    Check((int)Get("cash")==2000&&(int)Get("bankLoan")==2000,"Borrow duplicate guard failed");
                    Call("ShowDebtRepayment",-1);Click("Confirm Repayment");Check((int)Get("cash")==1000&&(int)Get("bankLoan")==1000,"Bank repayment failed");
                    Call("ShowStatement");break;
                case 9:Shot("statement");break;
                case 10:
                    Call("StartGame");Check((int)Call("Expenses")==expenses&&(int)Get("bankLoan")==0,"New game did not restore debts");
                    File.WriteAllText("work/debt-review/results.txt","PASS: all profession starting expenses; expense and debt UI; repayment preview/cancel; full payoff; duplicate guards; affordability; Payday after payoff; standalone borrowing; bank repayment; reset and text fit.\n");
                    File.WriteAllText("work/run-portfolio-review","");running=false;return;
            }
            step++;next=EditorApplication.timeSinceStartup+2;
        }catch(Exception e){File.WriteAllText("work/debt-review/results.txt","FAIL: "+e);running=false;Debug.LogException(e);}
    }
}
