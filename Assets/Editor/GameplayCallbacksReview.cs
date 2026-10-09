using System;
using System.IO;
using System.Reflection;
using System.Collections;
using Cashflow.Domain;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad] public static class GameplayCallbacksReview
{
    const BindingFlags F=BindingFlags.NonPublic|BindingFlags.Instance;
    static bool Active {get=>SessionState.GetBool("CallbacksReview",false);set=>SessionState.SetBool("CallbacksReview",value);}
    static CashflowGame G=>UnityEngine.Object.FindObjectOfType<CashflowGame>();
    static object Get(string n)=>typeof(CashflowGame).GetField(n,F).GetValue(G);
    static void Set(string n,object value)=>typeof(CashflowGame).GetField(n,F).SetValue(G,value);
    static object Call(string n,params object[] args)=>typeof(CashflowGame).GetMethod(n,F).Invoke(G,args);
    static MatchState State=>(MatchState)typeof(CashflowGame).GetProperty("GameplayCommands",F).GetValue(G);
    static int checks,stage;static double next,deadline;
    static void Check(bool ok,string message){checks++;if(!ok)throw new Exception(message);}
    static Button B(string name)=>GameObject.Find(name).GetComponent<Button>();
    static void Click(string name,ActionKind action){var b=B(name);long before=State.revision;b.onClick.Invoke();Check(State.revision>before,name+" did not dispatch");Check(State.receipts.Exists(r=>r.revision>before&&r.fingerprint.StartsWith(((int)action)+"|")),name+" wrong command");long after=State.revision;b.onClick.Invoke();Check(State.revision==after,name+" duplicate changed state");}
    static GameplayCallbacksReview(){EditorApplication.update+=Tick;}
    public static void Run(){EditorSceneManager.OpenScene("Assets/Scenes/ProfessionSelection.unity");Active=true;EditorApplication.isPlaying=true;}
    static void Tick(){
        if(!Active||!EditorApplication.isPlaying||EditorApplication.isCompiling||G==null||EditorApplication.timeSinceStartup<next)return;
        try{
            if(stage==0){
                Directory.CreateDirectory("work/callback-review");Set("savePathOverride",Path.GetFullPath("work/callback-review/solo.json"));Set("localPathOverride",Path.GetFullPath("work/callback-review/local.json"));Call("StartGame");Set("cash",100000);
                Call("ShowDealChoice");Click("Choose Small Deal",ActionKind.DrawSmall);Click("Buy Deal",ActionKind.Buy);Check(((IList)Get("portfolio")).Count==1,"Purchase missing");
                Call("CloseModal");Call("ShowDealChoice");Click("Choose Big Deal",ActionKind.DrawBig);Click("Pass Deal",ActionKind.Pass);
                Set("cash",0);Call("OfferDeal",new CashflowGame.Deal("Financed rental",1000,50),false);B("Buy Deal").onClick.Invoke();Click("Confirm Loan",ActionKind.Buy);Check((int)Get("bankLoan")==1000,"Loan missing");
                Call("ShowRequiredExpense","BILL","Required payment",500,2);Click("Pay Required Expense",ActionKind.Pay);Check((int)Get("skipTurns")==2,"Skip effect missing");
                Set("cash",100000);Call("ShowCharityChoice");Click("Donate Charity",ActionKind.Donate);Check((bool)Get("charity"),"Bonus missing");
                Call("ShowCharityChoice");Click("Pass Charity",ActionKind.Pass);
                Call("ShowBankBorrow",1000);Click("Confirm Borrowing",ActionKind.Borrow);Call("ShowDebtRepayment",-1);Click("Confirm Repayment",ActionKind.RepayBank);
                Call("ShowDebtRepayment",0);Click("Confirm Repayment",ActionKind.RepayDebt);
                Call("ShowBankBorrow",1000);var stale=B("Confirm Borrowing");Call("ShowStatement");long rev=State.revision;stale.onClick.Invoke();Check(State.revision==rev,"Closed finance callback mutated state");
                var asset=((IList)Get("portfolio"))[0];Call("ReviewMarketSale",asset,2000,"Sale test");Click("Accept Market Sale",ActionKind.Sell);Call("CloseModal");
                int kids=(int)Get("children");rev=State.revision;Call("Land","BABY");Check(State.revision==rev+1&&(int)Get("children")==kids+1,"Baby not dispatched");Call("CloseModal");
                Set("fastTrack",true);int cash=(int)Get("cash");rev=State.revision;Call("ResolveFastTrack","TAX AUDIT");Check(State.revision==rev+1&&(int)Get("cash")==cash-cash/10,"Loss not dispatched");Call("CloseModal");
                Call("ShowDreamPurchase");Click("Pass Dream",ActionKind.Pass);Set("cash",100000000);Call("ShowDreamPurchase");Click("Buy Dream",ActionKind.BuyDream);Check(State.finished,"Dream did not finish match");
                Call("StartLocalMatch");((BoardWorld)Get("world")).ReducedMotion=true;Call("Roll");stage=1;deadline=EditorApplication.timeSinceStartup+45;next=EditorApplication.timeSinceStartup+1;return;
            }
            if(stage==1){
                if((bool)Get("rolling")){if(EditorApplication.timeSinceStartup>deadline)throw new Exception("Roll never completed");next=EditorApplication.timeSinceStartup+1;return;}
                Check(State.receipts.Exists(r=>r.fingerprint.StartsWith(((int)ActionKind.Roll)+"|")),"Roll button bypassed engine");Check(State.receipts.Exists(r=>r.fingerprint.StartsWith(((int)ActionKind.FinishMove)+"|")),"Movement bypassed engine");
                long settled=State.revision;int settledCash=(int)Get("cash");Call("CompleteMovement",0,State.diceTotal);Check(State.revision==settled&&(int)Get("cash")==settledCash,"Movement settled twice");
                // Resolve a known offer through the actual UI before resume/handoff.
                Call("ShowDealChoice");Click("Choose Small Deal",ActionKind.DrawSmall);Check((bool)Call("SaveCheckpoint",false),"Pending card save failed");Call("ResumeLocalMatch");Click("Pass Deal",ActionKind.Pass);
                Check((bool)Call("SaveCheckpoint",false),"Completed turn save failed");Call("ResumeLocalMatch");Call("UpdateLocalTurn");int seat=State.active;B("Local End Turn").onClick.Invoke();Check(State.active!=seat&&State.phase==TurnPhase.AwaitRoll,"Resumed End Turn failed");
                Set("skipTurns",2);Set("charity",true);Call("Roll");Check((int)Get("skipTurns")==1&&(bool)Get("charity"),"Skipped roll consumed dice bonus");Call("CloseModal");Call("UpdateLocalTurn");B("Local End Turn").onClick.Invoke();
                Finish(true,"PASS: "+checks+" checks through live buttons: deck choice, buy/pass, loan, bill, donation, borrow/repay, sale, events, dream, stale/duplicate callbacks, animated roll/movement, pending-card resume and End Turn.");
            }
        }catch(Exception e){Finish(false,"FAIL: "+e);}
    }
    static void Finish(bool ok,string result){Active=false;if(G!=null)Set("saveActive",false);File.WriteAllText("work/callback-review/result.txt",result);EditorApplication.Exit(ok?0:1);}
}
