using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public static class CashflowHudReview
{
    const string Root="work/cashflow-hud-review/";
    static readonly BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;
    static readonly int[,] Sizes={{1280,720},{2532,1170},{2340,1080}};
    static bool running;static int step;static double next;
    static CashflowGame G=>UnityEngine.Object.FindObjectOfType<CashflowGame>();
    static object Call(string name,params object[] args)=>typeof(CashflowGame).GetMethod(name,F).Invoke(G,args);
    static object Get(string name)=>typeof(CashflowGame).GetField(name,F).GetValue(G);
    static void Set(string name,object value)=>typeof(CashflowGame).GetField(name,F).SetValue(G,value);
    static void Check(bool value,string message){if(!value)throw new Exception(message);}
    static Text Net=>(Text)Get("bottomNetCashflow");
    static int NetAmount=>(int)Call("Income")-(int)Call("Expenses");
    static void Verify(){Call("Refresh");Check(Net.text==(NetAmount>0?"+":"")+(string)Call("Money",NetAmount),"Net cashflow differs from accounting");}
    static CashflowHudReview(){EditorApplication.update+=Tick;}
    static void Size(int w,int h){
        var a=typeof(Editor).Assembly;var t=a.GetType("UnityEditor.GameViewSizes");
        var instance=typeof(ScriptableSingleton<>).MakeGenericType(t).GetProperty("instance").GetValue(null);
        var flags=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance;
        var groupType=t.GetProperty("currentGroupType",flags).GetValue(instance);
        var group=t.GetMethod("GetGroup").Invoke(instance,new[]{groupType});
        var size=Activator.CreateInstance(a.GetType("UnityEditor.GameViewSize"),Enum.ToObject(a.GetType("UnityEditor.GameViewSizeType"),1),w,h,"Cashflow HUD "+w+"x"+h);
        group.GetType().GetMethod("AddCustomSize").Invoke(group,new[]{size});
        int count=(int)group.GetType().GetMethod("GetTotalCount").Invoke(group,null);
        var view=EditorWindow.GetWindow(a.GetType("UnityEditor.GameView"));
        view.GetType().GetProperty("selectedSizeIndex",flags).SetValue(view,count-1);view.Focus();view.Repaint();
    }
    static void Tick(){
        if(EditorApplication.isCompiling)return;
        if(File.Exists("work/run-cashflow-hud-review")){
            if(!EditorApplication.isPlaying){EditorApplication.isPlaying=true;return;}
            File.Delete("work/run-cashflow-hud-review");Directory.CreateDirectory(Root);File.WriteAllText(Root+"results.txt","");
            running=true;step=0;next=EditorApplication.timeSinceStartup+2;
        }
        if(!running||EditorApplication.timeSinceStartup<next)return;
        try{
            if(step==0)Call("StartGame");
            else if(step==1){
                Verify();int initial=NetAmount;Set("cash",10000);
                Call("OfferDeal",new CashflowGame.Deal("HUD test investment",4000,220),false);
                GameObject.Find("Buy Deal").GetComponent<Button>().onClick.Invoke();Verify();Check(NetAmount==initial+220,"Purchase cashflow update");
                Call("ShowBankBorrow",1000);GameObject.Find("Confirm Borrowing").GetComponent<Button>().onClick.Invoke();Verify();Check(NetAmount==initial+120,"Loan interest update");
                Call("CloseModal");int cash=(int)Get("cash");Call("Payday");Verify();Check((int)Get("cash")==cash+NetAmount,"Payday amount differs from HUD");
                Call("Land","BABY");Verify();Check(NetAmount<initial+120,"Baby expense update");Call("CloseModal");
                Set("bankLoan",1000000);Verify();Check(Net.text.StartsWith("-"),"Negative cashflow sign");
                Color negative=Net.color;Set("bankLoan",0);Set("passive",(int)Call("Expenses")-(int)Call("Income")+(int)Get("passive"));Verify();Check(Net.text=="$0"&&Net.color!=negative,"Zero cashflow treatment");
                Set("fastTrack",true);Set("passive",(int)Call("Expenses")+500);Verify();Check(NetAmount==500,"Fast Track must exclude salary");Check(((Text)Get("freedomTitle")).text=="DREAM SAVINGS","Fast Track progress title");
                File.AppendAllText(Root+"results.txt","PASS: purchase, borrowing, Payday, child expenses, negative and zero cashflow, Fast Track salary exclusion and dream label.\n");
                Call("StartGame");
            }else if(step<=7){
                int i=(step-2)/2;
                if(step%2==0)Size(Sizes[i,0],Sizes[i,1]);
                else{
                    Check(Screen.width==Sizes[i,0]&&Screen.height==Sizes[i,1],"Requested resolution not active: "+Screen.width+"x"+Screen.height);
                    Canvas.ForceUpdateCanvases();ScreenCapture.CaptureScreenshot(Root+Screen.width+"x"+Screen.height+".png");
                    foreach(var t in Net.transform.parent.GetComponentsInChildren<Text>()){
                        Check(t.preferredHeight<=t.rectTransform.rect.height+2,"Clipped label: "+t.text+" preferred="+t.preferredHeight+" available="+t.rectTransform.rect.height);
                        var corners=new Vector3[4];t.rectTransform.GetWorldCorners(corners);
                        foreach(var p in corners)Check(p.x>=0&&p.y>=0&&p.x<=Screen.width&&p.y<=Screen.height,"HUD outside screen: "+t.text);
                    }
                    ScreenCapture.CaptureScreenshot(Root+Screen.width+"x"+Screen.height+".png");
                    File.AppendAllText(Root+"results.txt","PASS: HUD label and screen bounds at "+Screen.width+"x"+Screen.height+".\n");
                }
            }else{running=false;return;}
            step++;next=EditorApplication.timeSinceStartup+2;
        }catch(Exception e){running=false;File.AppendAllText(Root+"results.txt","FAIL: "+e);Debug.LogException(e);}
    }
}
