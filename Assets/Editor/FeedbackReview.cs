using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad] public static class FeedbackReview
{
    static int step;static bool running;static double next;
    static readonly BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;
    static CashflowGame G=>UnityEngine.Object.FindObjectOfType<CashflowGame>();
    static object Get(string n)=>typeof(CashflowGame).GetField(n,F).GetValue(G);
    static void Set(string n,object v)=>typeof(CashflowGame).GetField(n,F).SetValue(G,v);
    static object Call(string n,params object[] a)=>typeof(CashflowGame).GetMethod(n,F).Invoke(G,a);
    static void Click(string n)=>GameObject.Find(n).GetComponent<Button>().onClick.Invoke();
    static void Check(bool b,string m){if(!b)throw new Exception(m);}
    static FeedbackReview(){EditorApplication.update+=Tick;}
    static void Shot(string name){Canvas.ForceUpdateCanvases();foreach(var t in GameObject.Find("Event Card").GetComponentsInChildren<Text>())Check(t.preferredHeight<=t.rectTransform.rect.height+2,"Clipped: "+t.text);ScreenCapture.CaptureScreenshot("work/feedback-review/"+name+".png");}
    static void Tick()
    {
        if(EditorApplication.isCompiling)return;
        if(File.Exists("work/run-feedback-review")){
            if(!EditorApplication.isPlaying){EditorApplication.isPlaying=true;return;}
            File.Delete("work/run-feedback-review");Directory.CreateDirectory("work/feedback-review");running=true;step=0;next=EditorApplication.timeSinceStartup+2;
        }
        if(!running||EditorApplication.timeSinceStartup<next)return;
        try{
            switch(step){
                case 0:Screen.orientation=ScreenOrientation.LandscapeLeft;Call("StartGame");Set("cash",400);Call("ResetHistory");Call("ShowRequiredExpense","CAR REPAIR","Your car needs an urgent repair.",900,2);Check((int)Get("cash")==400&&(int)Get("bankLoan")==0,"Charged before confirmation");break;
                case 1:Shot("bill");break;
                case 2:
                    var pay=GameObject.Find("Pay Required Expense").GetComponent<Button>();pay.onClick.Invoke();pay.onClick.Invoke();Check((int)Get("cash")==500&&(int)Get("bankLoan")==1000&&(int)Get("skipTurns")==2,"Bill accounting or duplicate guard");
                    Call("Roll");Check((int)Get("skipTurns")==1&&!(bool)Get("rolling"),"First skip failed");Call("CloseModal");Call("Roll");Check((int)Get("skipTurns")==0,"Second skip failed");Call("CloseModal");
                    Set("charity",true);Call("Refresh");Check(((Text)Get("diceResult")).text.Contains("2 DICE"),"Charity status missing");
                    Set("cash",10000);Call("OfferDeal",new CashflowGame.Deal("Review business",3000,400),false);Click("Review Offer Finances");break;
                case 3:Shot("review-finances");break;
                case 4:Click("Return To Offer");Check((int)Get("cash")==10000,"Review changed cash");Click("Buy Deal");Check((int)Get("cash")==7000,"Offer lost after review");Call("ShowHistory",0);break;
                case 5:Shot("history");break;
                case 6:
                    Click("History Older");Call("CloseModal");Set("children",3);int expenses=(int)Call("Expenses");Call("Land","BABY");Check((int)Call("Expenses")==expenses,"Fourth child changed expenses");
                    Call("StartGame");Check(((System.Collections.ICollection)Get("history")).Count==0,"History did not reset");
                    File.WriteAllText("work/feedback-review/results.txt","PASS: bill preview, shortfall borrowing, duplicate guard, skipped turns, charity status, finance review preserves offer, purchase after review, history navigation/reset, child limit and text fit.\n");running=false;return;
            }
            step++;next=EditorApplication.timeSinceStartup+2;
        }catch(Exception e){running=false;File.WriteAllText("work/feedback-review/results.txt","FAIL: "+e);Debug.LogException(e);}
    }
}
