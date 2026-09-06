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
                    Require(GameObject.Find("Buy Deal").GetComponent<Button>().interactable,"Borrow option missing");
                    ScreenCapture.CaptureScreenshot(Folder+"/insufficient-cash.png");break;
                case 10:
                    Click("Buy Deal");Require(Get("cash")==0&&Get("bankLoan")==0,"Loan charged before confirmation");Click("Pass Loan");
                    UnityEngine.Object.FindObjectOfType<BoardWorld>().ReducedMotion=true;Call("Land","PAYDAY");break;
                case 11:
                    var group=GameObject.Find("Event Card").GetComponent<CanvasGroup>();Require(group.alpha==1&&group.interactable,"Reduced motion reveal incomplete");
                    ScreenCapture.CaptureScreenshot(Folder+"/event-card.png");
                    File.WriteAllText(Folder+"/results.txt","PASS: both deck choices, small purchase, duplicate click guard, big deal pass, insufficient cash, modal cleanup, reduced-motion event reveal.\n");
                    Call("CloseModal");break;
                case 12:Call("StartGame");Set("cash",5000);Call("Land","CHARITY");break;
                case 13:
                    ScreenCapture.CaptureScreenshot(Folder+"/charity-choice.png");Click("Pass Charity");
                    Require(Get("cash")==5000&&!(bool)typeof(CashflowGame).GetField("charity",Flags).GetValue(Game),"Charity pass changed cash or dice");
                    Call("Land","CHARITY");break;
                case 14:
                    var donate=GameObject.Find("Donate Charity").GetComponent<Button>();donate.onClick.Invoke();int after=Get("cash");donate.onClick.Invoke();
                    Require(after<5000&&Get("cash")==after&&(bool)typeof(CashflowGame).GetField("charity",Flags).GetValue(Game),"Donation failed or charged twice");
                    Set("cash",0);Call("Land","CHARITY");break;
                case 15:
                    Require(!GameObject.Find("Donate Charity").GetComponent<Button>().interactable,"Unaffordable donation enabled");Click("Pass Charity");
                    Set("cash",400);Call("OfferDeal",new CashflowGame.Deal("Review property",4000,220),false);break;
                case 16:Click("Buy Deal");break;
                case 17:
                    ScreenCapture.CaptureScreenshot(Folder+"/loan-review.png");
                    Require(Get("cash")==400&&Get("bankLoan")==0,"Loan review modified balance");break;
                case 18:
                    var loan=GameObject.Find("Confirm Loan").GetComponent<Button>();loan.onClick.Invoke();loan.onClick.Invoke();
                    Require(Get("cash")==400&&Get("bankLoan")==4000&&Get("passive")==220,"Borrow-and-buy accounting or duplicate guard failed");
                    int expected=(int)typeof(CashflowGame).GetMethod("Income",Flags).Invoke(Game,null)-(int)typeof(CashflowGame).GetMethod("Expenses",Flags).Invoke(Game,null);
                    Call("Payday");Require(Get("cash")==400+expected,"Payday excludes interest");Set("cash",5000);Call("ShowStatement");break;
                case 19:
                    int oldExpenses=(int)typeof(CashflowGame).GetMethod("Expenses",Flags).Invoke(Game,null);
                    Click("Repay Loan");var repay=GameObject.Find("Confirm Repayment").GetComponent<Button>();repay.onClick.Invoke();repay.onClick.Invoke();
                    Require(Get("bankLoan")==3000&&Get("cash")==4000,"Principal repayment accounting failed");
                    Require((int)typeof(CashflowGame).GetMethod("Expenses",Flags).Invoke(Game,null)==oldExpenses-100,"Repayment did not reduce interest");break;
                case 20:ScreenCapture.CaptureScreenshot(Folder+"/finances.png");break;
                case 21:
                    Click("Close Finances");Require(!UnityEngine.Object.FindObjectOfType<BoardWorld>().InputBlocked,"Finances blocked board after close");
                    Call("StartGame");Require(Get("bankLoan")==0,"New game retained loan");
                    File.AppendAllText(Folder+"/results.txt","PASS: charity accept/pass and affordability; loan confirmation/pass, interest, duplicate guards, repayment, finance close and new-game reset.\n");
                    running=false;return;
            }
            step++;next=EditorApplication.timeSinceStartup+1.2;
        }
        catch(Exception e){File.WriteAllText(Folder+"/error.txt",e.ToString());running=false;Debug.LogException(e);}
    }
}
