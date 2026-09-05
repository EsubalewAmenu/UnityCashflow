using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public static class DealReview
{
    static int step,frames;
    static double captureNext;
    static double next;
    static bool running;
    static string Folder=>"work/deal-review";
    static readonly BindingFlags Flags=BindingFlags.NonPublic|BindingFlags.Instance;
    static CashflowGame Game=>UnityEngine.Object.FindObjectOfType<CashflowGame>();
    static void Call(string method,params object[] args)=>typeof(CashflowGame).GetMethod(method,Flags).Invoke(Game,args);
    static void Set(string field,object value)=>typeof(CashflowGame).GetField(field,Flags).SetValue(Game,value);
    static int Get(string field)=>(int)typeof(CashflowGame).GetField(field,Flags).GetValue(Game);
    static void Click(string name)=>GameObject.Find(name).GetComponent<Button>().onClick.Invoke();
    static void Require(bool condition,string message){if(!condition)throw new Exception(message);}
    static DealReview(){EditorApplication.update+=Tick;}
    static void Tick()
    {
        if(File.Exists("work/run-deal-review")&&!EditorApplication.isCompiling&&EditorApplication.isPlaying)
        {
            File.Delete("work/run-deal-review");Directory.CreateDirectory(Folder);File.WriteAllText(Folder+"/results.txt","");File.WriteAllText(Folder+"/error.txt","");
            step=0;frames=0;running=true;next=EditorApplication.timeSinceStartup+1;
        }
        if(running&&step==3&&EditorApplication.timeSinceStartup>=captureNext&&frames<12)
        {
            ScreenCapture.CaptureScreenshot(Folder+"/reveal-"+(frames++)+".png");captureNext=EditorApplication.timeSinceStartup+.07;
        }
        if(!running||EditorApplication.isCompiling||!EditorApplication.isPlaying||EditorApplication.timeSinceStartup<next)return;
        try
        {
            switch(step)
            {
                case 0: Call("StartGame");Set("cash",50000);Call("Refresh");Call("Land","OPPORTUNITY");break;
                case 1:
                    Require(GameObject.Find("Choose Small Deal")&&GameObject.Find("Choose Big Deal"),"Deck choice missing");
                    Require(UnityEngine.Object.FindObjectOfType<BoardWorld>().InputBlocked,"Choice must block board input");
                    ScreenCapture.CaptureScreenshot(Folder+"/choose-deal.png");break;
                case 2:Click("Choose Small Deal");break;
                case 3:
                    ScreenCapture.CaptureScreenshot(Folder+"/small-deal.png");
                    Require(GameObject.Find("Buy Deal").GetComponent<Button>().interactable,"Affordable deal disabled");break;
                case 4:
                    var buy=GameObject.Find("Buy Deal").GetComponent<Button>();buy.onClick.Invoke();int cash=Get("cash"),flow=Get("passive");buy.onClick.Invoke();
                    Require(Get("cash")==cash&&Get("passive")==flow,"Purchase charged twice");Require(cash<50000&&flow>0,"Purchase did not update balances");
                    Require(!UnityEngine.Object.FindObjectOfType<BoardWorld>().InputBlocked,"Buy did not unblock board");
                    Call("Land","OPPORTUNITY");break;
                case 5:Click("Choose Big Deal");break;
                case 6:ScreenCapture.CaptureScreenshot(Folder+"/big-deal.png");break;
                case 7:
                    int before=Get("cash"),income=Get("passive");Click("Pass Deal");Require(Get("cash")==before&&Get("passive")==income,"Pass changed balances");
                    Require(!UnityEngine.Object.FindObjectOfType<BoardWorld>().InputBlocked,"Pass left modal blocked");Set("cash",0);Call("Land","OPPORTUNITY");break;
                case 8:Click("Choose Big Deal");break;
                case 9:
                    Require(!GameObject.Find("Buy Deal").GetComponent<Button>().interactable,"Unaffordable deal enabled");
                    ScreenCapture.CaptureScreenshot(Folder+"/insufficient-cash.png");break;
                case 10:
                    Click("Buy Deal");Require(Get("cash")==0,"Unaffordable purchase allowed");Click("Pass Deal");
                    UnityEngine.Object.FindObjectOfType<BoardWorld>().ReducedMotion=true;Call("Land","PAYDAY");break;
                case 11:
                    var group=GameObject.Find("Event Card").GetComponent<CanvasGroup>();Require(group.alpha==1&&group.interactable,"Reduced motion reveal incomplete");
                    ScreenCapture.CaptureScreenshot(Folder+"/event-card.png");
                    File.WriteAllText(Folder+"/results.txt","PASS: both deck choices, small purchase, duplicate click guard, big deal pass, insufficient cash, modal cleanup, reduced-motion event reveal.\n");
                    Call("CloseModal");running=false;return;
            }
            step++;next=EditorApplication.timeSinceStartup+1.2;
        }
        catch(Exception e){File.WriteAllText(Folder+"/error.txt",e.ToString());running=false;Debug.LogException(e);}
    }
}
