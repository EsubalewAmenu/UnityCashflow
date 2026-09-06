using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad] public static class FastTrackReview
{
    static int step;static double next;static bool running;
    static readonly BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;
    static CashflowGame G=>UnityEngine.Object.FindObjectOfType<CashflowGame>();
    static object Get(string n)=>typeof(CashflowGame).GetField(n,F).GetValue(G);
    static void Set(string n,object v)=>typeof(CashflowGame).GetField(n,F).SetValue(G,v);
    static object Call(string n,params object[] a)=>typeof(CashflowGame).GetMethod(n,F).Invoke(G,a);
    static void Check(bool b,string m){if(!b)throw new Exception(m);}
    static void Click(string n)=>GameObject.Find(n).GetComponent<Button>().onClick.Invoke();
    static FastTrackReview(){EditorApplication.update+=Tick;}
    static void Shot(string name){Canvas.ForceUpdateCanvases();foreach(var t in GameObject.Find("Event Card").GetComponentsInChildren<Text>())Check(t.preferredHeight<=t.rectTransform.rect.height+2,"Clipped: "+t.text);ScreenCapture.CaptureScreenshot("work/fast-track-review/"+name+".png");}
    static void Tick()
    {
        if(EditorApplication.isCompiling)return;
        if(File.Exists("work/run-fast-track-review")){
            if(!EditorApplication.isPlaying){EditorApplication.isPlaying=true;return;}
            File.Delete("work/run-fast-track-review");Directory.CreateDirectory("work/fast-track-review");step=0;running=true;next=EditorApplication.timeSinceStartup+2;
        }
        if(!running||EditorApplication.timeSinceStartup<next)return;
        try{
            switch(step){
                case 0:
                    Screen.orientation=ScreenOrientation.LandscapeLeft;Call("StartGame");Set("cash",200000);
                    int expenses=(int)Call("Expenses");Set("passive",expenses);Call("CheckEscape");Check(!(bool)Get("fastTrack"),"Escaped at equality");
                    Call("PurchaseInvestment",new CashflowGame.Deal("Income business",1000,expenses+500),0);int before=(int)Get("cash");
                    Call("CheckEscape");Check((bool)Get("fastTrack")&&(int)Get("cash")==before,"Transition changed cash");Check((int)Call("Income")==expenses+500,"Salary did not stop");
                    Call("CheckEscape");Check((int)Get("cash")==before,"Repeated transition changed cash");Call("ShowFastTrackIntro");break;
                case 1:Shot("intro");break;
                case 2:
                    Call("CloseModal");int cash=(int)Get("cash");Call("CollectMovementPaydays",23,7);Check((int)Get("cash")==cash+1000,"Crossed cashflow days wrong");
                    Call("Land","CASHFLOW DAY");Check((int)Get("cash")==cash+1000,"Landing paid twice");Call("CloseModal");
                    int passive=(int)Get("passive");Call("Land","BIO-TECH +25K");Check((int)Get("passive")==passive,"Free Fast Track income");break;
                case 3:Shot("business");break;
                case 4:
                    int balance=(int)Get("cash");Click("Pass Deal");Check((int)Get("cash")==balance,"Pass spent cash");
                    Call("Land","BIO-TECH +25K");int income=(int)Get("passive");Click("Buy Deal");Check((int)Get("cash")==balance-125000&&(int)Get("passive")==income+25000,"Business purchase failed");
                    Set("cash",0);Call("Land","DREAM");Check(!(bool)Get("gameWon")&&!GameObject.Find("Buy Dream").GetComponent<Button>().interactable,"Unaffordable dream won");break;
                case 5:Shot("dream");break;
                case 6:
                    Click("Pass Dream");Set("selectedDream",0);Set("dream","Stock Market for Kids");Set("cash",125000);Call("Land","DREAM");
                    var buy=GameObject.Find("Buy Dream").GetComponent<Button>();buy.onClick.Invoke();buy.onClick.Invoke();Check((bool)Get("gameWon")&&(int)Get("cash")==0,"Dream payment or duplicate guard failed");
                    Check(!(bool)Call("PurchaseDream"),"Dream bought twice");Call("CloseModal");Call("Roll");Check(!(bool)Get("rolling"),"Roll after victory allowed");
                    Call("StartGame");Check(!(bool)Get("gameWon")&&!(bool)Get("fastTrack"),"New game retained endgame state");
                    File.WriteAllText("work/fast-track-review/results.txt","PASS: escape threshold, cash preservation, retirement income, repeat transition, crossed Cashflow Days and no double pay, business purchase/pass, dream affordability/payment/duplicate guard, victory roll block, new-game reset and text fit.\n");running=false;return;
            }
            step++;next=EditorApplication.timeSinceStartup+2;
        }catch(Exception e){running=false;File.WriteAllText("work/fast-track-review/results.txt","FAIL: "+e);Debug.LogException(e);}
    }
}
