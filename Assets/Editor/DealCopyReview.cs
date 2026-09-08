using System;
using System.IO;
using System.Reflection;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public static class DealCopyReview
{
    static int step;
    static double next;
    static bool running;
    static readonly BindingFlags Flags=BindingFlags.NonPublic|BindingFlags.Instance;
    static readonly int[,] Sizes={{1280,720},{2532,1170},{2340,1080},{1024,768}};
    static DealCopyReview(){EditorApplication.update+=Tick;}
    static void Tick()
    {
        if(EditorApplication.isCompiling)return;
        if(File.Exists("work/run-deal-copy-review"))
        {
            if(!EditorApplication.isPlaying){EditorApplication.isPlaying=true;return;}
            File.Delete("work/run-deal-copy-review");Directory.CreateDirectory("work/deal-copy-review");
            File.WriteAllText("work/deal-copy-review/results.txt","");running=true;step=0;next=EditorApplication.timeSinceStartup+2;
        }
        if(!running||EditorApplication.timeSinceStartup<next)return;
        try
        {
            var game=UnityEngine.Object.FindObjectOfType<CashflowGame>();
            Action<string,object[]> call=(name,args)=>typeof(CashflowGame).GetMethod(name,Flags).Invoke(game,args);
            if(step==0)call("StartGame",null);
            else if(step<=18)
            {
                int i=(step-1)/3;
                if(step%3==1)
                {
                    int size=i%Sizes.GetLength(0);
                    typeof(BoardReview).GetMethod("SetSize",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{Sizes[size,0],Sizes[size,1]});
                    typeof(CashflowGame).GetField("cash",Flags).SetValue(game,i%2==0?50000:400);
                    var small=(CashflowGame.Deal[])typeof(CashflowGame).GetField("smallDeals",Flags).GetValue(game);
                    var big=(CashflowGame.Deal[])typeof(CashflowGame).GetField("bigDeals",Flags).GetValue(game);
                    var deals=small.Concat(big).ToArray();
                    call("OfferDeal",new object[]{deals[i],i>=small.Length});
                }
                else
                {
                    Canvas.ForceUpdateCanvases();
                    var card=GameObject.Find("Event Card");
                    foreach(var text in card.GetComponentsInChildren<Text>())
                    {
                        if(text.preferredHeight>text.rectTransform.rect.height+2)throw new Exception("Clipped copy: "+text.text);
                        var corners=new Vector3[4];text.rectTransform.GetWorldCorners(corners);
                        foreach(var c in corners)if(c.x<0||c.y<0||c.x>Screen.width||c.y>Screen.height)throw new Exception("Text outside screen: "+text.name);
                    }
                    bool details=step%3==0;
                    if(details){
                        GameObject.Find("Return From Deal Explanation").GetComponent<Button>().onClick.Invoke();
                        if(GameObject.Find("Event Card")!=card || (int)typeof(CashflowGame).GetField("cash",Flags).GetValue(game)!=(i%2==0?50000:400))throw new Exception("Reading details changed the offer or cash");
                    } else GameObject.Find("Explain Deal").GetComponent<Button>().onClick.Invoke();
                    // CaptureScreenshot renders at frame end, after the button action.
                    ScreenCapture.CaptureScreenshot("work/deal-copy-review/deal-"+i+(details?"-offer":"-details")+".png");
                    File.AppendAllText("work/deal-copy-review/results.txt","PASS: deal "+i+(details?" explanation and return":" offer")+" at "+Screen.width+"x"+Screen.height+"\n");
                }
            }
            else {call("CloseModal",null);File.WriteAllText("work/run-deal-review","");running=false;return;}
            step++;next=EditorApplication.timeSinceStartup+2;
        }
        catch(Exception error){running=false;File.WriteAllText("work/deal-copy-review/results.txt","FAIL: "+error);Debug.LogException(error);}
    }
}
