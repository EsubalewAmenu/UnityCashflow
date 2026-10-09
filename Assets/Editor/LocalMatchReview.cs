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
    static void CompleteMove(int start,int steps){
        Call("ImportCommandPlayer");var state=(Cashflow.Domain.MatchState)typeof(CashflowGame).GetProperty("GameplayCommands",F).GetValue(G);
        state.phase=Cashflow.Domain.TurnPhase.Moving;state.diceTotal=steps;Call("CompleteMovement",start,steps);
    }
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
                Call("StartLocalMatch");Set("cash",0);
                Call("PurchaseInvestment",new CashflowGame.Deal("Test rental",1000,100),1000);firstCash=(int)Get("cash");
                var match=Get("localMatch");match.GetType().GetField("rolled").SetValue(match,true);
                Call("EndLocalTurn");Check((int)Get("cash")!=firstCash,"Player cash leaked across seats");
                Check((int)Get("bankLoan")==0&&(int)Get("passive")==0,"Investment or debt leaked across seats");
                Set("cash",6789);match.GetType().GetField("rolled").SetValue(match,true);((Cashflow.Domain.MatchState)match.GetType().GetField("commands").GetValue(match)).phase=Cashflow.Domain.TurnPhase.Complete;Call("EndLocalTurn");
                Check((int)Get("cash")==firstCash,"First player's balance was not restored");
                Check((int)Get("bankLoan")==1000&&(int)Get("passive")==100,"Investment lost at handoff");
                Check((bool)Call("SaveCheckpoint",false),"Match save failed");
                Set("cash",1);Call("ResumeLocalMatch");Check((int)Get("cash")==firstCash,"Resume lost active finances");
                var activeBefore=Get("localMatch").GetType().GetField("active").GetValue(Get("localMatch"));Call("EndLocalTurn");
                Check(activeBefore.Equals(Get("localMatch").GetType().GetField("active").GetValue(Get("localMatch"))),"End turn allowed before rolling");
                Check(CashflowRules.SettlePayday(100,300,50)==350&&CashflowRules.IsBankrupt(-1,-1)&&!CashflowRules.IsBankrupt(-1,0),"Shared financial rules");
                Set("position",5);int net=(int)Call("Income")-(int)Call("Expenses");CompleteMove(4,1);
                Check((int)Get("cash")==firstCash+net&&Get("modal")==null,"Local Payday payment or ad suppression failed");
                ScreenCapture.CaptureScreenshot("work/local-match-review/board.png");
                step++;next=EditorApplication.timeSinceStartup+3;return;
            }
            if(step==3){
                // Exercise the actual saved-state bug: completed turn, reload, then
                // click the newly created button (not just the controller method).
                var match=Get("localMatch");match.GetType().GetField("rolled").SetValue(match,true);
                Check((bool)Call("SaveCheckpoint",false),"Completed turn checkpoint failed");
                Call("ResumeLocalMatch");Call("UpdateLocalTurn");
                int prior=(int)Get("localMatch").GetType().GetField("active").GetValue(Get("localMatch"));
                var button=GameObject.Find("Local End Turn").GetComponent<Button>();
                Check(button.interactable,"Resumed End Turn is disabled");button.onClick.Invoke();
                Check((int)Get("localMatch").GetType().GetField("active").GetValue(Get("localMatch"))!=prior,"Resumed End Turn did not switch player");
                button.onClick.Invoke();
                Check((int)Get("localMatch").GetType().GetField("active").GetValue(Get("localMatch"))!=prior,"Old button callback switched twice");
                string fixture=Path.GetFullPath("work/local-match-review/reported-save.json");
                if(File.Exists(fixture)){
                    Set("localPathOverride",Path.GetFullPath("work/local-match-review/reported-save-copy.json"));
                    File.Copy(fixture,(string)typeof(CashflowGame).GetProperty("LocalMatchPath",F).GetValue(G),true);
                    Call("ResumeLocalMatch");Call("UpdateLocalTurn");
                    prior=(int)Get("localMatch").GetType().GetField("active").GetValue(Get("localMatch"));
                    var restored=GameObject.Find("Local End Turn").GetComponent<Button>();Check(restored.interactable,"Reported save button disabled");restored.onClick.Invoke();
                    Check((int)Get("localMatch").GetType().GetField("active").GetValue(Get("localMatch"))!=prior,"Reported save did not advance");
                }
                step++;next=EditorApplication.timeSinceStartup+2;return;
            }
            Finish(true,"Opening buttons, player isolation, save/resume, completed-turn resume button, duplicate button callback, reported-save fixture (when present), turn guards and shared finance rules passed.");
        }catch(Exception e){Finish(false,e.ToString());}
    }
    static void Finish(bool pass,string message){active=false;Directory.CreateDirectory("work/local-match-review");File.WriteAllText("work/local-match-review/result.txt",(pass?"PASS: ":"FAIL: ")+message);EditorApplication.Exit(pass?0:1);}
}
