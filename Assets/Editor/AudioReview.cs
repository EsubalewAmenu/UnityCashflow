using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public static class AudioReview
{
    static bool running,hadEnabled,hadVolume;
    static int step,oldEnabled;
    static float oldVolume;
    static double next,deadline;
    const string Root="work/audio-review/";
    static readonly BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;
    static CashflowGame G=>UnityEngine.Object.FindObjectOfType<CashflowGame>();
    static BoardAudio A=>UnityEngine.Object.FindObjectOfType<BoardAudio>();
    static void Call(string method,params object[] args)=>typeof(CashflowGame).GetMethod(method,F).Invoke(G,args);
    static void Check(bool ok,string why){if(!ok)throw new Exception(why);}
    static AudioReview(){EditorApplication.update+=Tick;}
    static void Restore(){
        if(hadEnabled)PlayerPrefs.SetInt("Cashflow.SoundEnabled",oldEnabled);else PlayerPrefs.DeleteKey("Cashflow.SoundEnabled");
        if(hadVolume)PlayerPrefs.SetFloat("Cashflow.SoundVolume",oldVolume);else PlayerPrefs.DeleteKey("Cashflow.SoundVolume");
        PlayerPrefs.Save();
        if(A!=null){A.SoundEnabled=oldEnabled!=0;A.Volume=oldVolume;}
    }
    static void Tick(){
        if(EditorApplication.isCompiling)return;
        if(File.Exists("work/run-audio-review")){
            if(!EditorApplication.isPlaying){EditorApplication.isPlaying=true;return;}
            File.Delete("work/run-audio-review");Directory.CreateDirectory(Root);
            hadEnabled=PlayerPrefs.HasKey("Cashflow.SoundEnabled");hadVolume=PlayerPrefs.HasKey("Cashflow.SoundVolume");
            oldEnabled=PlayerPrefs.GetInt("Cashflow.SoundEnabled",1);oldVolume=PlayerPrefs.GetFloat("Cashflow.SoundVolume",.65f);
            running=true;step=0;next=EditorApplication.timeSinceStartup+1;deadline=next+60;
        }
        if(!running||EditorApplication.timeSinceStartup<next)return;
        try{
            if(EditorApplication.timeSinceStartup>deadline)throw new Exception("Audio review timed out");
            switch(step){
                case 0:Call("StartGame");A.SoundEnabled=true;A.Volume=.37f;break;
                case 1:
                    foreach(string name in new[]{"dice-roll","dice-hit-0","dice-hit-1","dice-hit-2","card","payday","purchase","expense"}){
                        var clip=Resources.Load<AudioClip>("Audio/"+name);Check(clip!=null&&clip.length>0&&clip.channels==1,"Missing or invalid clip "+name);
                    }
                    var state=UnityEngine.Random.state;float expected=UnityEngine.Random.value;UnityEngine.Random.state=state;
                    A.Roll();A.Impact(3);A.Purchase();Check(UnityEngine.Random.value==expected,"Audio changed gameplay randomness");UnityEngine.Random.state=state;
                    Check(A.GetComponents<AudioSource>().Length==3,"Unbounded audio voices");
                    A.SoundEnabled=false;Check(A.GetComponents<AudioSource>().All(s=>!s.isPlaying),"Mute did not stop active sound");
                    A.SoundEnabled=true;A.Volume=0;A.Preview();Check(A.GetComponents<AudioSource>().All(s=>!s.isPlaying),"Zero volume plays sound");
                    A.Volume=.37f;A.SoundEnabled=false;Call("StartGame");break;
                case 2:
                    Check(!A.SoundEnabled&&Mathf.Approximately(A.Volume,.37f),"New board lost sound preferences");
                    A.SoundEnabled=true;Call("Roll");break;
                case 3:
                    if((bool)typeof(CashflowGame).GetField("rolling",F).GetValue(G))return;
                    Check(A.GetComponents<AudioSource>().Any(s=>s.clip!=null&&s.clip.name.StartsWith("dice-hit-")),"Physical dice collisions produced no cue");
                    Call("CloseModal");Call("Payday");
                    Check(A.GetComponents<AudioSource>().Any(s=>s.clip!=null&&s.clip.name=="payday"),"Payday cue missing");
                    A.Card();Check(A.GetComponents<AudioSource>().Any(s=>s.clip!=null&&s.clip.name=="payday"),"Card interrupted Payday");
                    Call("ShowBoardSettings");break;
                case 4:
                    var slider=GameObject.Find("Sound Volume").GetComponent<Slider>();slider.value=.25f;
                    Check(Mathf.Approximately(A.Volume,.25f),"Slider did not set volume");
                    GameObject.Find("Preview Sound").GetComponent<Button>().onClick.Invoke();
                    Canvas.ForceUpdateCanvases();
                    foreach(var t in GameObject.Find("Sound Volume").transform.parent.GetComponentsInChildren<Text>())Check(t.preferredHeight<=t.rectTransform.rect.height+2,"Clipped settings label "+t.text);
                    ScreenCapture.CaptureScreenshot(Root+"settings.png");break;
                case 5:
                    Call("CloseModal");typeof(CashflowGame).GetField("cash",F).SetValue(G,5000);
                    Call("OfferDeal",new CashflowGame.Deal("Audio review investment",4000,220),false);
                    GameObject.Find("Buy Deal").GetComponent<Button>().onClick.Invoke();
                    Check(A.GetComponents<AudioSource>().Any(s=>s.clip!=null&&s.clip.name=="purchase"),"Purchase cue missing");
                    Call("ShowRequiredExpense","Audio review expense","Test payment",100,0);
                    GameObject.Find("Pay Required Expense").GetComponent<Button>().onClick.Invoke();
                    Check(A.GetComponents<AudioSource>().Any(s=>s.clip!=null&&s.clip.name=="expense"),"Expense cue missing");
                    A.SendMessage("OnApplicationPause",true);A.Preview();
                    Check(A.GetComponents<AudioSource>().All(s=>!s.isPlaying),"Paused app continued audio");A.SendMessage("OnApplicationPause",false);
                    Restore();
                    File.WriteAllText(Root+"results.txt","PASS: eight mono clips, bounded voices, gameplay RNG unchanged, immediate mute, zero volume, saved preferences across a new board, real dice collision cues, Payday priority, purchase and expense cues, pause handling, settings volume slider and text bounds.");
                    running=false;return;
            }
            step++;next=EditorApplication.timeSinceStartup+1.2;
        }catch(Exception e){Restore();running=false;File.WriteAllText(Root+"results.txt","FAIL: "+e);Debug.LogException(e);}
    }
}
