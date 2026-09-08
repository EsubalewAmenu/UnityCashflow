using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public static class EventReceiptReview
{
    const string Root="work/event-receipt-review/";
    const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;
    static bool running;static int step;static double next;
    static CashflowGame G=>UnityEngine.Object.FindObjectOfType<CashflowGame>();
    static object Call(string name,params object[] args)=>typeof(CashflowGame).GetMethod(name,F).Invoke(G,args);
    static object Get(string name)=>typeof(CashflowGame).GetField(name,F).GetValue(G);
    static void Set(string name,object value)=>typeof(CashflowGame).GetField(name,F).SetValue(G,value);
    static BoardEventReceipt R=>(BoardEventReceipt)Get("eventReceipt");
    static void Check(bool value,string message){if(!value)throw new Exception(message);}
    static void Shot(string name){
        Canvas.ForceUpdateCanvases();ScreenCapture.CaptureScreenshot(Root+name+".png");
        foreach(var t in UnityEngine.Object.FindObjectsOfType<Text>()){
            if(t.transform.IsChildOf(R.transform)||t.transform.IsChildOf(((GameObject)Get("modal")).transform))
                Check(t.preferredHeight<=t.rectTransform.rect.height+2,"Clipped text: "+t.text);
        }
    }
    static EventReceiptReview(){EditorApplication.update+=Tick;}
    static void Tick(){
        if(EditorApplication.isCompiling)return;
        if(File.Exists("work/run-event-receipt-review")){
            if(!EditorApplication.isPlaying){EditorApplication.isPlaying=true;return;}
            File.Delete("work/run-event-receipt-review");Directory.CreateDirectory(Root);running=true;step=0;next=EditorApplication.timeSinceStartup+2;
        }
        if(!running||EditorApplication.timeSinceStartup<next)return;
        try{
            switch(step){
                case 0:Call("StartGame");((BoardWorld)Get("world")).ReducedMotion=true;break;
                case 1:
                    int before=(int)Get("cash"),net=(int)Call("Income")-(int)Call("Expenses");
                    Call("CollectMovementPaydays",4,1);Call("Land","PAYDAY");
                    Check((int)Get("cash")==before+net&&Get("modal")==null,"Landing duplicated payment or opened a blocking modal");
                    Call("Refresh");Call("ShowDealChoice");Check(R.transform.GetSiblingIndex()>((GameObject)Get("modal")).transform.GetSiblingIndex(),"Next card obscures Payday receipt");
                    Check(!R.GetComponent<CanvasGroup>().blocksRaycasts,"Receipt blocks input");Check(R.Title.text.Contains("PAYDAY"),"Missing Payday receipt");Shot("payday-with-deal");break;
                case 2:Call("CloseModal");Set("cash",400);Call("ShowRequiredExpense","CAR REPAIR","Your car needs an urgent repair.",900,2);Shot("specific-bill");break;
                case 3:
                    var pay=GameObject.Find("Pay Required Expense").GetComponent<Button>();pay.onClick.Invoke();pay.onClick.Invoke();
                    Check((int)Get("cash")==500&&(int)Get("bankLoan")==1000,"Expense payment duplicated or miscalculated");break;
                case 4:Check(R.Title.text.Contains("CAR REPAIR")&&R.Detail.text.Contains("Monthly interest +$100"),"Expense receipt missing borrowing cost");Call("ShowStatement");Shot("paid-receipt");break;
                case 5:Call("CloseModal");int cash=(int)Get("cash"),expense=(int)Call("Expenses");Call("Land","BABY");Check((int)Get("cash")==cash&&(int)Call("Expenses")>expense,"Recurring event changed cash");Shot("monthly-change");break;
                case 6:Call("StartGame");break;
                case 7:Set("fastTrack",true);Set("bankLoan",100000);Call("Payday");Check(R.Title.text.Contains("CASHFLOW DAY • -$"),"Negative Cashflow Day presented as collection");Call("Land","CASHFLOW DAY");Check(Get("modal")==null,"Cashflow Day blocks play");break;
                case 8:
                    // Verify FIFO instead of overwriting a receipt when two events arrive together.
                    R.Show("SECOND RECEIPT","Queued",Color.black);Check(!R.Title.text.Contains("SECOND"),"Receipt overwritten");break;
                case 11:Check(R.Title.text=="SECOND RECEIPT","Queued receipt never appeared");Call("StartGame");break;
                case 12:Check(R.GetComponent<CanvasGroup>().alpha==0,"Old receipt survived a new game");File.WriteAllText(Root+"results.txt","PASS: landing paid once without modal; receipt visible above following deal and non-interactive; specific bill and borrowing receipt; duplicate-payment guard; recurring child expense without immediate cash charge; negative Fast Track receipt; FIFO queue and new-game reset; card text bounds. Screenshots at "+Screen.width+"x"+Screen.height+".\n");running=false;return;
            }
            step++;next=EditorApplication.timeSinceStartup+2;
        }catch(Exception e){running=false;File.WriteAllText(Root+"results.txt","FAIL: "+e);Debug.LogException(e);}
    }
}
