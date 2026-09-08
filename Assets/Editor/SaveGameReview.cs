using System;
using System.IO;
using System.Collections;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public static class SaveGameReview
{
    const string Root="work/save-review/";
    const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;
    static bool running;static int step;static double next;static string path,expected,nextDeal;static int priorCash;
    static CashflowGame G=>UnityEngine.Object.FindObjectOfType<CashflowGame>();
    static object Get(string name)=>typeof(CashflowGame).GetField(name,F).GetValue(G);
    static void Set(string name,object value)=>typeof(CashflowGame).GetField(name,F).SetValue(G,value);
    static object Call(string name,params object[] args)=>typeof(CashflowGame).GetMethod(name,F).Invoke(G,args);
    static void Check(bool value,string message){if(!value)throw new Exception(message);}
    static void Save(){Check((bool)Call("SaveCheckpoint",false),"Checkpoint write failed");}
    static void Click(string name)=>GameObject.Find(name).GetComponent<Button>().onClick.Invoke();
    static string Snapshot()=>JsonUtility.ToJson(Call("CaptureGame"));
    static void Recreate(){
        var old=G;Set("saveActive",false);
        var canvas=(Canvas)Get("canvas");var world=(BoardWorld)Get("world");
        if(world!=null)UnityEngine.Object.DestroyImmediate(world.gameObject);
        UnityEngine.Object.DestroyImmediate(canvas.gameObject);UnityEngine.Object.DestroyImmediate(old.gameObject);
        new GameObject("Save Review Controller").AddComponent<CashflowGame>();Set("savePathOverride",path);
        typeof(CashflowGame).GetField("resumeRequested",BindingFlags.Static|BindingFlags.NonPublic).SetValue(null,true);
        G.OpenScene("GameBoard");Check((bool)Get("saveActive"),"GameBoard entry could not resume");
    }
    static void Shot(string name){Canvas.ForceUpdateCanvases();ScreenCapture.CaptureScreenshot(Root+name+".png");}
    static SaveGameReview(){EditorApplication.update+=Tick;}
    static void Tick(){
        if(EditorApplication.isCompiling)return;
        if(File.Exists("work/run-save-review")){
            if(!EditorApplication.isPlaying){EditorApplication.isPlaying=true;return;}
            File.Delete("work/run-save-review");Directory.CreateDirectory(Root);path=Path.GetFullPath(Root+"test-"+Guid.NewGuid().ToString("N")+".json");
            running=true;step=0;next=EditorApplication.timeSinceStartup+2;
        }
        if(!running||EditorApplication.timeSinceStartup<next)return;
        try{
            switch(step){
                case 0:
                    // Keep all generated checkpoints out of the player's persistent save.
                    Set("savePathOverride",path);Call("StartGame");((BoardWorld)Get("world")).ReducedMotion=true;
                    Set("cash",400);Call("ResetHistory");Call("OfferDeal",(CashflowGame.Deal)Call("DrawDeal",false),false);Save();expected=Snapshot();nextDeal=((CashflowGame.Deal)Call("DrawDeal",false)).title;Set("saveActive",false);break;
                case 1:
                    Recreate();Check(Snapshot()==expected,"Pending deal or deck changed on restore");Check(((CashflowGame.Deal)Call("DrawDeal",false)).title==nextDeal,"Draw order changed");Shot("restored-deal");
                    Call("OfferDeal",new CashflowGame.Deal("Save test house",5000,250),false);Click("Buy Deal");Save();expected=Snapshot();break;
                case 2:
                    Recreate();Check(Snapshot()==expected,"Loan confirmation lost");Click("Confirm Loan");Save();Check((int)Get("cash")==400&&(int)Get("bankLoan")==5000,"Financed purchase accounting");expected=Snapshot();break;
                case 3:
                    Recreate();Check(Snapshot()==expected&&((IList)Get("portfolio")).Count==1,"Purchase duplicated/lost");
                    Call("ShowRequiredExpense","CAR REPAIR","A fixed saved bill.",900,2);Save();expected=Snapshot();break;
                case 4:
                    Recreate();Check(Snapshot()==expected,"Bill amount changed");var pay=GameObject.Find("Pay Required Expense").GetComponent<Button>();pay.onClick.Invoke();pay.onClick.Invoke();Save();Check((int)Get("cash")==500&&(int)Get("bankLoan")==6000,"Bill duplicated");expected=Snapshot();break;
                case 5:
                    Recreate();Check(Snapshot()==expected&&(int)Get("skipTurns")==2,"Paid bill/turn effect lost");
                    var asset=((IList)Get("portfolio"))[0];Call("ReviewMarketSale",asset,7000,"Fixed buyer offer.");Save();expected=Snapshot();break;
                case 6:
                    Recreate();Check(Snapshot()==expected,"Market offer rerolled");Click("Pass Market Sale");
                    Call("CloseModal");Set("skipTurns",0);Set("position",4);Set("charity",false);Set("lastRoll",1);Set("rolling",true);
                    var pending=typeof(CashflowGame).GetNestedType("PendingDecision",BindingFlags.NonPublic);var move=Activator.CreateInstance(pending);pending.GetField("kind").SetValue(move,"move");pending.GetField("amount").SetValue(move,1);Set("pendingDecision",move);
                    priorCash=(int)Get("cash")+(int)Call("Income")-(int)Call("Expenses");Check((bool)Call("SaveCheckpoint",true),"Movement checkpoint failed");break;
                case 7:
                    Recreate();Check((int)Get("position")==5&&(int)Get("cash")==priorCash,"Interrupted movement changed result or Payday");expected=Snapshot();break;
                case 8:
                    Recreate();Check(Snapshot()==expected,"Completed movement paid again");
                    Set("cash",20000);Set("charity",true);((BoardWorld)Get("world")).ReducedMotion=true;Call("ShowCharityChoice");Call("OnApplicationPause",true);expected=Snapshot();break;
                case 9:
                    Recreate();Check(Snapshot()==expected,"Pause lost charity/state/settings");Click("Pass Charity");Call("Land","BABY");Save();expected=Snapshot();break;
                case 10:
                    Recreate();Check(Snapshot()==expected&&(int)Get("children")==1,"Baby event applied twice");Call("CloseModal");
                    Set("fastTrack",true);Call("ShowDreamPurchase");Save();expected=Snapshot();break;
                case 11:
                    Recreate();Check(Snapshot()==expected,"Fast Track dream decision lost");Click("Pass Dream");
                    // Generate two known valid checkpoints and corrupt only the newest.
                    Set("cash",12345);Save();expected=Snapshot();Set("cash",23456);Save();File.WriteAllText(path,"truncated");break;
                case 12:
                    Recreate();Check(Snapshot()==expected&&(int)Get("cash")==12345,"Recovery did not use complete backup");
                    Set("cash",34567);Save();Check(File.ReadAllText(path+".bak")!="truncated","Recovery overwrote valid backup with corrupt primary");
                    Call("CloseModal");Set("saveActive",false);Call("ShowSetup");Check(GameObject.Find("Resume Game")!=null,"Resume button missing");Shot("resume-opening");break;
                case 13:
                    string disk=File.ReadAllText(path);Call("StartRandomGame");Check(GameObject.Find("Replace Saved Game")!=null,"New game did not require replacement confirmation");Click("Keep Saved Game");Check(File.ReadAllText(path)==disk,"Cancel replaced save");
                    var data=Call("CaptureGame");data.GetType().GetField("version").SetValue(data,99);
                    string payload=JsonUtility.ToJson(data);
                    var envelopeType=typeof(CashflowGame).GetNestedType("SaveEnvelope",BindingFlags.NonPublic);var envelope=Activator.CreateInstance(envelopeType);
                    envelopeType.GetField("payload").SetValue(envelope,payload);envelopeType.GetField("checksum").SetValue(envelope,typeof(CashflowGame).GetMethod("Digest",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{payload}));
                    string future=JsonUtility.ToJson(envelope);File.WriteAllText(path,future);File.WriteAllText(path+".bak",future);Check(!(bool)Call("ResumeGame"),"Unsupported save version accepted");
                    File.WriteAllText(path,"broken");File.WriteAllText(path+".bak","broken");Check(!(bool)Call("ResumeGame"),"Corrupt data accepted");Check(File.ReadAllText(path)=="broken","Corrupt save silently replaced");
                    File.WriteAllText(Root+"results.txt","PASS: fresh-controller GameBoard entry restores exact deal/deck, financed purchase, unpaid and paid bill, duplicate guard, fixed market offer, movement checkpoint and Payday once, pause, charity, recurring expense once, Fast Track dream, reduced motion/history, corrupt-primary recovery, recovery-copy preservation, Resume UI, cancelled replacement, unsupported version and corrupt-save rejection. Test saves isolated from player data.\n");
                    Set("saveActive",false);running=false;UnityEngine.SceneManagement.SceneManager.LoadScene("ProfessionSelection");return;
            }
            step++;next=EditorApplication.timeSinceStartup+2;
        }catch(Exception e){running=false;File.WriteAllText(Root+"results.txt","FAIL at step "+step+": "+e);Debug.LogException(e);if(G!=null)Set("saveActive",false);}
    }
}
