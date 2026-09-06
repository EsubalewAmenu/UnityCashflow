using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[InitializeOnLoad]
public static class OpeningReview
{
    static int step;static double next;static bool running;
    static readonly int[,] Sizes={{1280,720},{2532,1170},{2340,1080},{1024,768}};
    static string Folder=>"work/opening-review";
    static BindingFlags Flags=>BindingFlags.NonPublic|BindingFlags.Instance;
    static CashflowGame Game=>UnityEngine.Object.FindObjectOfType<CashflowGame>();
    static void Call(string name)=>typeof(CashflowGame).GetMethod(name,Flags).Invoke(Game,null);
    static void Click(string name)=>GameObject.Find(name).GetComponent<Button>().onClick.Invoke();
    static OpeningReview(){EditorApplication.update+=Tick;}
    static void Tick()
    {
        if(File.Exists("work/run-opening-review")&&!EditorApplication.isCompiling&&EditorApplication.isPlaying)
        {File.Delete("work/run-opening-review");Directory.CreateDirectory(Folder);File.WriteAllText(Folder+"/results.txt","");File.WriteAllText(Folder+"/error.txt","");running=true;step=0;next=EditorApplication.timeSinceStartup+1;}
        if(!running||EditorApplication.isCompiling||EditorApplication.timeSinceStartup<next)return;
        try
        {
            if(step==0)SceneManager.LoadScene("ProfessionSelection");
            else if(step<=8)
            {
                int i=(step-1)/2;
                if(step%2==1)typeof(BoardReview).GetMethod("SetSize",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{Sizes[i,0],Sizes[i,1]});
                else
                {
                    Canvas.ForceUpdateCanvases();
                    foreach(var button in UnityEngine.Object.FindObjectsOfType<Button>())
                    {
                        var corners=new Vector3[4];button.GetComponent<RectTransform>().GetWorldCorners(corners);
                        foreach(var corner in corners)if(corner.x<0||corner.y<0||corner.x>Screen.width||corner.y>Screen.height)throw new Exception("Opening button exceeds screen: "+button.name);
                    }
                    if(GameObject.Find("Selected Opening Card").GetComponentsInChildren<Image>()[1].sprite==null)throw new Exception("Missing portrait");
                    ScreenCapture.CaptureScreenshot(Folder+"/profession-"+Screen.width+"x"+Screen.height+".png");
                    File.AppendAllText(Folder+"/results.txt","PASS: opening card/button bounds and artwork at "+Screen.width+"x"+Screen.height+".\n");
                }
            }
            else if(step==9){Click("Next Selection");}
            else if(step==10)
            {
                var selected=(CashflowGame.Profession)typeof(CashflowGame).GetField("profession",Flags).GetValue(Game);SessionState.SetString("OpeningProfession",selected.name);Click("Choose Dream Next");
            }
            else if(step==11){Click("Next Selection");}
            else if(step==12){ScreenCapture.CaptureScreenshot(Folder+"/dream-selection.png");}
            else if(step==13){SessionState.SetInt("OpeningDream",PlayerPrefs.GetInt("CashflowDream"));Click("Start Selected Game");}
            else
            {
                var selected=(CashflowGame.Profession)typeof(CashflowGame).GetField("profession",Flags).GetValue(Game);
                if(selected.name!=SessionState.GetString("OpeningProfession","")||SessionState.GetInt("OpeningDream",-1)!=(int)typeof(CashflowGame).GetField("selectedDream",Flags).GetValue(Game)||UnityEngine.Object.FindObjectOfType<BoardWorld>()==null)throw new Exception("Selections did not reach gameplay");
                File.AppendAllText(Folder+"/results.txt","PASS: carousel selection, profession-to-dream transition, saved selections and start-game handoff.\n");running=false;return;
            }
            step++;next=EditorApplication.timeSinceStartup+2;
        }
        catch(Exception e){running=false;File.WriteAllText(Folder+"/error.txt",e.ToString());Debug.LogException(e);}
    }
}
