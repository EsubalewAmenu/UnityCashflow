using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public static class KingdomReview
{
    const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;
    static bool running;static int step;static double next;
    static CashflowGame G=>UnityEngine.Object.FindObjectOfType<CashflowGame>();
    static object Get(string n)=>typeof(CashflowGame).GetField(n,F).GetValue(G);
    static void Set(string n,object v)=>typeof(CashflowGame).GetField(n,F).SetValue(G,v);
    static object Call(string n,params object[] a)=>typeof(CashflowGame).GetMethod(n,F).Invoke(G,a);
    static void Check(bool b,string m){if(!b)throw new Exception(m);}
    static void Shot(string n){Canvas.ForceUpdateCanvases();ScreenCapture.CaptureScreenshot("work/play-store/"+n+".png");}
    static KingdomReview(){EditorApplication.update+=Tick;}
    static void Tick(){
        if(EditorApplication.isCompiling)return;
        if(File.Exists("work/run-kingdom-review")){
            if(!EditorApplication.isPlaying){EditorApplication.isPlaying=true;return;}
            File.Delete("work/run-kingdom-review");Directory.CreateDirectory("work/play-store");running=true;step=0;next=EditorApplication.timeSinceStartup+2;
        }
        if(!running||EditorApplication.timeSinceStartup<next)return;
        try{
            switch(step){
                case 0:
                    Set("savePathOverride",Path.GetFullPath("work/play-store/gameover-test-"+Guid.NewGuid().ToString("N")+".json"));
                    Call("StartGame");((BoardWorld)Get("world")).ReducedMotion=true;
                    typeof(CashflowHudReview).GetMethod("Size",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{1920,1080});break;
                case 1:
                    Set("cash",0);Set("bankLoan",1000000);Call("Refresh");Check(!(bool)Get("gameOver"),"Zero cash must not end the game");
                    Set("cash",100);Call("Refresh");Check(!(bool)Get("gameOver"),"Positive cash must allow negative flow");
                    Set("bankLoan",0);Set("cash",-1);Call("Refresh");Check(!(bool)Get("gameOver"),"Positive flow must allow negative cash");
                    Set("passive",(int)Call("Expenses")-(int)Call("Income"));Call("Refresh");Check(!(bool)Get("gameOver"),"Zero flow must not end the game");
                    Set("passive",0);Set("bankLoan",1000000);Call("Refresh");Check((bool)Get("gameOver"),"Both negative must end game");
                    Check(GameObject.Find("Game Over Try Again")!=null,"Missing game over UI");Call("SaveCheckpoint",false);break;
                case 2:
                    foreach(var t in GameObject.Find("Event Card").GetComponentsInChildren<Text>())Check(t.preferredHeight<=t.rectTransform.rect.height+2,"Clipped game over text: "+t.text);
                    Shot("game-over");Call("CloseModal");Call("Roll");Check(!(bool)Get("rolling")&&!((Button)Get("rollButton")).interactable,"Game over still allows rolls");
                    Check((bool)Call("ResumeGame")&&(bool)Get("gameOver"),"Resume revives ended game");break;
                case 3:
                    GameObject.Find("Game Over Try Again").GetComponent<Button>().onClick.Invoke();Check(!(bool)Get("gameOver")&&(int)Get("cash")>=0,"Try again failed");
                    Set("cash",1);Set("bankLoan",1000000);Call("Payday");Call("Refresh");Check((bool)Get("gameOver"),"Negative Payday must end game");
                    Set("profession",null);Call("StartGame");((BoardWorld)Get("world")).ReducedMotion=true;break;
                case 4:Check(Screen.width==1920&&Screen.height==1080,"Screenshots require actual 16:9 Game view");Shot("01-board");break;
                case 5:Call("ShowDealChoice");break;
                case 6:Shot("02-deal-choice");break;
                case 7:Call("OfferDeal",new CashflowGame.Deal("Growth Share",10,0,"Buy one growth-company share for $10. It pays no dividends or Payday income. Keep it for a future Market sale; the offer may be higher or lower than your entry price."),false);break;
                case 8:Shot("03-small-deal");break;
                case 9:Call("CloseModal");Call("ShowStatement");break;
                case 10:Shot("04-finances");break;
                case 11:Set("saveActive",false);Call("ShowSetup");break;
                case 12:Shot("05-professions");break;
                case 13:File.WriteAllText("work/play-store/gameover-results.txt","PASS: strictly negative cash AND net flow required; zero boundaries and single-negative states stay playable; negative Payday ends game; rolls blocked; game over survives resume; Try Again resets. Game-over text bounds checked. Store screenshots captured in Unity at 1920x1080. Player save isolated.\n");running=false;UnityEngine.SceneManagement.SceneManager.LoadScene("ProfessionSelection");return;
            }
            step++;next=EditorApplication.timeSinceStartup+2;
        }catch(Exception e){running=false;if(G!=null)Set("saveActive",false);File.WriteAllText("work/play-store/gameover-results.txt","FAIL step "+step+": "+e);Debug.LogException(e);}
    }
}
