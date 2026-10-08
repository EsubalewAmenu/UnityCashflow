using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

[InitializeOnLoad] public static class LocalMatchReview
{
    static bool active { get=>SessionState.GetBool("LocalMatchReview.active",false);set=>SessionState.SetBool("LocalMatchReview.active",value); }static double next;static int step;static int firstCash;
    const BindingFlags F=BindingFlags.NonPublic|BindingFlags.Instance;
    static CashflowGame G=>UnityEngine.Object.FindObjectOfType<CashflowGame>();
    static object Get(string n)=>typeof(CashflowGame).GetField(n,F).GetValue(G);
    static void Set(string n,object v)=>typeof(CashflowGame).GetField(n,F).SetValue(G,v);
    static object Call(string n,params object[] a)=>typeof(CashflowGame).GetMethod(n,F).Invoke(G,a);
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    static void CheckOpeningEntry(string page)
    {
        Canvas.ForceUpdateCanvases();
        var button=GameObject.Find("Local Multiplayer").GetComponent<Button>();
        var rect=button.GetComponent<RectTransform>();
        var point=RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(rect.rect.center));
        var hits=new List<RaycastResult>();
        EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=point},hits);
        Check(hits.Count>0&&hits[0].gameObject.GetComponentInParent<Button>()==button,page+": Local Multiplayer covered; point="+point+" hits="+string.Join(",",hits.ConvertAll(h=>h.gameObject.name)));
        button.onClick.Invoke();Check(Get("modal")!=null,"Local multiplayer menu did not open");Call("CloseModal");
    }
    static LocalMatchReview(){EditorApplication.update+=Tick;}
    public static void Run(){EditorSceneManager.OpenScene("Assets/Scenes/ProfessionSelection.unity");active=true;step=0;next=EditorApplication.timeSinceStartup+5;EditorApplication.isPlaying=true;}
    static void Tick(){
        if(!active||!EditorApplication.isPlaying||EditorApplication.isCompiling||EditorApplication.timeSinceStartup<next||G==null)return;
        try{
            if(step==0){Call("ShowSetup");step=1;next=EditorApplication.timeSinceStartup+2;return;}
            if(step==1){CheckOpeningEntry("ShowSetup");Call("ShowDreams");step=2;next=EditorApplication.timeSinceStartup+2;return;}
            if(step==2){
                CheckOpeningEntry("ShowDreams");
                Set("localPathOverride",Path.GetFullPath("work/local-match-review/save.json"));
                Call("StartLocalMatch");Set("cash",12345);
                Call("PurchaseInvestment",new CashflowGame.Deal("Test rental",1000,100),500);firstCash=(int)Get("cash");
                var match=Get("localMatch");match.GetType().GetField("rolled").SetValue(match,true);
                Call("EndLocalTurn");Check((int)Get("cash")!=firstCash,"Player cash leaked across seats");
                Check((int)Get("bankLoan")==0&&(int)Get("passive")==0,"Investment or debt leaked across seats");
                Set("cash",6789);match.GetType().GetField("rolled").SetValue(match,true);Call("EndLocalTurn");
                Check((int)Get("cash")==firstCash,"First player's balance was not restored");
                Check((int)Get("bankLoan")==500&&(int)Get("passive")==100,"Investment lost at handoff");
                Check((bool)Call("SaveCheckpoint",false),"Match save failed");
                Set("cash",1);Call("ResumeLocalMatch");Check((int)Get("cash")==firstCash,"Resume lost active finances");
                var activeBefore=Get("localMatch").GetType().GetField("active").GetValue(Get("localMatch"));Call("EndLocalTurn");
                Check(activeBefore.Equals(Get("localMatch").GetType().GetField("active").GetValue(Get("localMatch"))),"End turn allowed before rolling");
                Check(CashflowRules.SettlePayday(100,300,50)==350&&CashflowRules.IsBankrupt(-1,-1)&&!CashflowRules.IsBankrupt(-1,0),"Shared financial rules");
                Set("position",5);int net=(int)Call("Income")-(int)Call("Expenses");Call("CompleteMovement",4,1);
                Check((int)Get("cash")==firstCash+net&&Get("modal")==null,"Local Payday payment or ad suppression failed");
                ScreenCapture.CaptureScreenshot("work/local-match-review/board.png");
                step++;next=EditorApplication.timeSinceStartup+3;return;
            }
            Finish(true,"Local multiplayer entry unobstructed on profession and dream pages; menu opens; two-player cash isolation, handoff, separate save/resume, turn guard and shared finance rules passed.");
        }catch(Exception e){Finish(false,e.ToString());}
    }
    static void Finish(bool pass,string message){active=false;Directory.CreateDirectory("work/local-match-review");File.WriteAllText("work/local-match-review/result.txt",(pass?"PASS: ":"FAIL: ")+message);EditorApplication.Exit(pass?0:1);}
}
