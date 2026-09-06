using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[InitializeOnLoad] public static class NavigationReview
{
    static int step,randomProfession,randomDream;static bool running;static double next;
    static readonly BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;
    static CashflowGame G=>UnityEngine.Object.FindObjectOfType<CashflowGame>();
    static object Get(string n)=>typeof(CashflowGame).GetField(n,F).GetValue(G);
    static void Set(string n,object v)=>typeof(CashflowGame).GetField(n,F).SetValue(G,v);
    static object Call(string n,params object[] a)=>typeof(CashflowGame).GetMethod(n,F).Invoke(G,a);
    static void Click(string n)=>GameObject.Find(n).GetComponent<Button>().onClick.Invoke();
    static void Check(bool b,string m){if(!b)throw new Exception(m);}
    static NavigationReview(){EditorApplication.update+=Tick;}
    static void Shot(string name)
    {
        Canvas.ForceUpdateCanvases();
        foreach(var t in UnityEngine.Object.FindObjectsOfType<Text>()){
            Check(!t.text.Contains("STEP 1 OF 2")&&!t.text.Contains("STEP 2 OF 2")&&!t.text.Contains("Roll the dice to begin"),"Removed text present");
            if(t.transform.parent.name=="Selected Opening Card")Check(t.preferredHeight<=t.rectTransform.rect.height+2,"Clipped selected card");
        }
        ScreenCapture.CaptureScreenshot("work/navigation-review/"+name+".png");
    }
    static void Tick()
    {
        if(EditorApplication.isCompiling)return;
        if(File.Exists("work/run-navigation-review")){
            if(!EditorApplication.isPlaying){EditorApplication.isPlaying=true;return;}
            File.Delete("work/run-navigation-review");Directory.CreateDirectory("work/navigation-review");step=0;running=true;next=EditorApplication.timeSinceStartup+2;
        }
        if(!running||EditorApplication.timeSinceStartup<next)return;
        try{
            switch(step){
                case 0:
                    Screen.orientation=ScreenOrientation.LandscapeLeft;
                    var professions=(CashflowGame.Profession[])Get("professions");var dreams=(string[])Get("dreams");var goals=(int[])Get("dreamGoals");
                    Check(professions.Length==10&&dreams.Length==13&&dreams.Length==goals.Length,"Catalog counts");
                    for(int i=0;i<10;i++){Check(Call("ProfessionPortrait",i)!=null,"Missing portrait");Set("profession",professions[i]);Call("ResetPersonalDebts");Check(professions[i].salary>(int)Call("Expenses"),"Starting cashflow not positive");}
                    for(int i=0;i<80;i++){Call("ChooseRandomSelections");Check(PlayerPrefs.GetInt("CashflowProfession")>=0&&PlayerPrefs.GetInt("CashflowProfession")<10&&PlayerPrefs.GetInt("CashflowDream")>=0&&PlayerPrefs.GetInt("CashflowDream")<13,"Random outside catalog");}
                    Set("profession",professions[9]);Call("ShowSetup");break;
                case 1:Shot("professions");break;
                case 2:Click("Choose Dream Next");break;
                case 3:PlayerPrefs.SetInt("CashflowDream",12);Call("ShowDreams");break;
                case 4:Shot("dreams");Check(GameObject.Find("Selected Opening Card").GetComponentsInChildren<Image>()[1].sprite!=null,"Missing dream art");break;
                case 5:Click("Random Start");randomProfession=PlayerPrefs.GetInt("CashflowProfession");randomDream=PlayerPrefs.GetInt("CashflowDream");break;
                case 6:
                    Check(SceneManager.GetActiveScene().name=="GameBoard","Random did not start board");
                    Check(Array.IndexOf((CashflowGame.Profession[])Get("professions"),Get("profession"))==randomProfession&&(int)Get("selectedDream")==randomDream,"Random selections lost");
                    Click("Board Menu");break;
                case 7:Shot("exit");Click("Stay In Game");Check(SceneManager.GetActiveScene().name=="GameBoard","Cancel exited");break;
                case 8:Click("Board Menu");Click("Exit To Menu");break;
                case 9:
                    Check(SceneManager.GetActiveScene().name=="ProfessionSelection"&&GameObject.Find("Random Start")!=null,"Exit did not return to menu");
                    File.WriteAllText("work/navigation-review/results.txt","PASS: 10 professions / 13 dreams, portraits, positive starting finances, random selection bounds and actual direct start, selection persistence, removed text, card text fit, exit cancellation and return to menu.\n");running=false;return;
            }
            step++;next=EditorApplication.timeSinceStartup+2;
        }catch(Exception e){running=false;File.WriteAllText("work/navigation-review/results.txt","FAIL: "+e);Debug.LogException(e);}
    }
}
