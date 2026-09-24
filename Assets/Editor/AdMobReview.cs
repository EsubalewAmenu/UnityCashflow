using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public static class AdMobReview
{
    const string Root="work/admob-review/";
    const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;
    static bool running;static int step,oldCash;static double next;static ReviewProvider fake;
    static CashflowGame G=>UnityEngine.Object.FindObjectOfType<CashflowGame>();
    static object Get(string n)=>typeof(CashflowGame).GetField(n,F).GetValue(G);
    static void Set(string n,object v)=>typeof(CashflowGame).GetField(n,F).SetValue(G,v);
    static object Call(string n,params object[] a)=>typeof(CashflowGame).GetMethod(n,F).Invoke(G,a);
    static void Check(bool condition,string text){if(!condition)throw new Exception(text);}
    static Button B(string n)=>GameObject.Find(n).GetComponent<Button>();
    static void Size(int w,int h)=>typeof(CashflowHudReview).GetMethod("Size",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{w,h});
    static string pendingShot; static bool Shot(string name){if(pendingShot==name){pendingShot=null;return true;} Canvas.ForceUpdateCanvases();ScreenCapture.CaptureScreenshot(Root+name+".png");pendingShot=name;next=EditorApplication.timeSinceStartup+1;return false;}
    static AdMobReview(){EditorApplication.update+=Tick;}
    static void Tick()
    {
        if(EditorApplication.isCompiling)return;
        if(File.Exists("work/run-admob-review")){
            if(!EditorApplication.isPlaying){EditorApplication.isPlaying=true;return;}
            File.Delete("work/run-admob-review");Directory.CreateDirectory(Root);running=true;step=0;next=EditorApplication.timeSinceStartup+2;
        }
        if(!running||EditorApplication.timeSinceStartup<next)return;
        try{
            switch(step){
                case 0:
                    Set("savePathOverride",Path.GetFullPath(Root+"save-"+Guid.NewGuid().ToString("N")+".json"));
                    fake=new ReviewProvider();CashflowAds.Ensure().UseReviewProvider(fake);
                    Call("StartGame");((BoardWorld)Get("world")).ReducedMotion=true;Set("cash",10000);Size(1280,720);Call("ShowCharityChoice");break;
                case 1:
                    Check(!B("Watch Charity Ad").interactable&&B("Donate Charity").interactable&&B("Pass Charity").interactable,"No-fill fallback");
                    oldCash=(int)Get("cash");B("Pass Charity").onClick.Invoke();Check((int)Get("cash")==oldCash&&!(bool)Get("charity"),"Pass must be free");
                    fake.ready=true;Call("ShowCharityChoice");break;
                case 2:
                    Check(B("Watch Charity Ad").interactable,"Ready reward hidden");
                    foreach(var t in GameObject.Find("Event Card").GetComponentsInChildren<Text>())Check(t.preferredHeight<=t.rectTransform.rect.height+2,"Clipped: "+t.text);
                    if(!Shot("charity-1280x720"))return;B("Watch Charity Ad").onClick.Invoke();
                    Check(CashflowAds.Instance.FullScreen&&AudioListener.pause&&Time.timeScale==0,"Ad must pause audio/game");
                    Check(!B("Donate Charity").interactable&&!B("Pass Charity").interactable,"Choices active during ad");
                    fake.close();Check(!CashflowAds.Instance.FullScreen&&Time.timeScale==1&&!AudioListener.pause,"Early close did not resume");
                    Check(!(bool)Get("charity")&&(int)Get("cash")==oldCash,"Early close rewarded or charged");break;
                case 3:
                    fake.ready=true;B("Watch Charity Ad").onClick.Invoke();fake.earn();fake.earn();
                    Check((bool)Get("charity")&&(int)Get("cash")==oldCash,"Reward must grant free dice once");
                    string payload=JsonUtility.ToJson(Call("CaptureGame"));Check(payload.Contains("\"charity\":true")&&payload.Contains("\"kind\":\"\""),"Reward checkpoint unresolved");
                    fake.close();fake.close();
                    Set("charity",false);Check((bool)Call("ResumeGame")&&(bool)Get("charity"),"Earned reward lost on resume");
                    Call("ShowCharityChoice");break;
                case 4:
                    Check(!B("Watch Charity Ad").interactable&&!B("Donate Charity").interactable,"Bonus stacking permitted");
                    B("Pass Charity").onClick.Invoke();Check((bool)Get("charity"),"Pass removed earned reward");
                    Set("charity",false);Call("ShowCharityChoice");oldCash=(int)Get("cash");
                    int donation=Mathf.Max(100,(int)Call("Income")/10);B("Donate Charity").onClick.Invoke();
                    Check((bool)Get("charity")&&(int)Get("cash")==oldCash-donation,"Normal donation changed");
                    Set("charity",false);Set("cash",0);fake.ready=false;Call("ShowCharityChoice");break;
                case 5:
                    Check(!B("Donate Charity").interactable&&!B("Watch Charity Ad").interactable&&B("Pass Charity").interactable,"Low cash/no fill traps player");
                    B("Pass Charity").onClick.Invoke();Set("cash",10000);fake.ready=true;Call("ShowCharityChoice");Size(2532,1170);break;
                case 6:if(!Shot("charity-iphone12"))return;Call("CloseModal");Set("saveActive",false);Call("ShowSetup");break;
                case 7:
                    Check(CashflowAds.Instance.ReservedBottomPixels==136,"Menu banner reservation");
                    var root=(RectTransform)Get("uiRoot");var corners=new Vector3[4];root.GetWorldCorners(corners);
                    Check(corners[0].y>=Screen.safeArea.yMin+136,"Menu overlaps banner slot");if(!Shot("menu-iphone12"))return;
                    Call("ShowSaveMessage","TEST","Test dialog",null);Check(CashflowAds.Instance.ReservedBottomPixels==0,"Banner visible over menu dialog");
                    Call("CloseModal");Check(CashflowAds.Instance.ReservedBottomPixels>0,"Menu banner not restored");
                    Call("StartGame");Check(CashflowAds.Instance.ReservedTopPixels==136,"Gameplay top banner missing");
                    Size(2340,1080);Call("ShowCharityChoice");break;
                case 8:if(!Shot("charity-pixel5"))return;Call("CloseModal");Set("cash",-1);Set("bankLoan",1000000);Call("Refresh");break;
                case 9:
                    Check(CashflowAds.Instance.ReservedBottomPixels>0,"Results banner missing");if(!Shot("results-pixel5"))return;
                    Call("StartGame");Check(CashflowAds.Instance.ReservedTopPixels==136&&CashflowAds.Instance.ReservedBottomPixels==0,"Gameplay banner not restored on retry");
                    Call("ShowCharityChoice");break;
                case 10:
                    Check(CashflowAds.Instance.ReservedTopPixels==136,"Popup hid gameplay banner");
                    var gameplayRoot=(RectTransform)Get("uiRoot");var topCorners=new Vector3[4];gameplayRoot.GetWorldCorners(topCorners);
                    Check(topCorners[1].y<=Screen.safeArea.yMax-136+1,"Gameplay popup overlaps top banner");
                    Call("CloseModal");Set("cash",10000);Set("bankLoan",0);Set("position",6);
                    oldCash=(int)Get("cash");Call("CompleteMovement",4,2);
                    Check((int)Get("cash")==oldCash+(int)Call("Income")-(int)Call("Expenses"),"Payday settlement wrong");
                    Check(JsonUtility.ToJson(Get("pendingDecision")).Contains("choice")&&!CashflowAds.Instance.FullScreen&&fake.interstitialShows==0,"Passing Payday interrupted destination with an ad break");
                    Call("CloseModal");oldCash=(int)Get("cash");Call("ShowPaydayBreak","OPPORTUNITY",123,1);
                    Check((int)Get("cash")==oldCash&&JsonUtility.ToJson(Get("pendingDecision")).Contains("choice"),"Legacy passing-Payday checkpoint replayed break or payment");
                    Call("CloseModal");Set("position",5);Call("CompleteMovement",4,1);
                    Check((int)Get("cash")==oldCash+(int)Call("Income")-(int)Call("Expenses"),"Landing Payday settlement wrong");
                    Check(Get("modal")!=null&&JsonUtility.ToJson(Get("pendingDecision")).Contains("payday-break"),"Salary popup missing");
                    Check((bool)Call("SaveCheckpoint",false),"Payday checkpoint failed");oldCash=(int)Get("cash");
                    Check((bool)Call("ResumeGame")&&(int)Get("cash")==oldCash,"Payday resume paid twice");
                    fake.interstitialReady=false;B("Ad Break OK").onClick.Invoke();
                    Check(!CashflowAds.Instance.FullScreen&&!JsonUtility.ToJson(Get("pendingDecision")).Contains("payday-break"),"No fill blocked landing");
                    Call("CloseModal");Set("skipTurns",2);Call("ShowDownsizeBreak");break;
                case 11:
                    CashflowAds.Instance.Settings.interstitialCooldownSeconds=0;fake.interstitialReady=true;
                    var confirm=B("Ad Break OK");oldCash=(int)Get("cash");confirm.onClick.Invoke();confirm.onClick.Invoke();
                    Check(fake.interstitialShows==1&&CashflowAds.Instance.FullScreen&&AudioListener.pause&&Time.timeScale==0,"Interstitial did not pause exactly once");
                    fake.close();fake.close();Check(!CashflowAds.Instance.FullScreen&&Time.timeScale==1&&!AudioListener.pause,"Interstitial close did not restore game");
                    Check((int)Get("cash")==oldCash&&(int)Get("skipTurns")==2,"Downsized ad changed finances/turns");
                    CashflowAds.Instance.Settings.interstitialCooldownSeconds=90;fake.interstitialReady=true;
                    Call("ShowDownsizeBreak");B("Ad Break OK").onClick.Invoke();Check(fake.interstitialShows==1&&!CashflowAds.Instance.FullScreen,"Cooldown failed");
                    CashflowAds.Instance.Settings.interstitialCooldownSeconds=0;fake.throwInterstitial=true;
                    Call("ShowDownsizeBreak");B("Ad Break OK").onClick.Invoke();
                    Check(!CashflowAds.Instance.FullScreen&&Time.timeScale==1&&Get("modal")==null,"Show failure blocked play");
                    CashflowAds.Instance.Settings.interstitialCooldownSeconds=90;
                    Call("CloseModal");Set("fastTrack",true);Set("fastPosition",8);Set("cash",100000);Set("bankLoan",0);
                    oldCash=(int)Get("cash");Call("CompleteMovement",5,3);
                    Check((int)Get("cash")!=oldCash&&!JsonUtility.ToJson(Get("pendingDecision")).Contains("payday-break")&&!CashflowAds.Instance.FullScreen,"Passing Cashflow Day showed a break or missed settlement");
                    Call("CloseModal");Set("fastPosition",6);Call("CompleteMovement",5,1);
                    Check(JsonUtility.ToJson(Get("pendingDecision")).Contains("payday-break"),"Landing Cashflow Day popup missing");
                    Call("CloseModal");Set("saveActive",false);Call("ShowSetup");
                    File.WriteAllText(Root+"results.txt","PASS: reward lifecycle; gameplay top banner and popup safe area at 1280x720, 2532x1170, 2340x1080; Payday settlement and resume without duplicate payment; no-fill continuation; Downsized interstitial exactly once; close/failure recovery; cooldown and audio/time restoration. Deterministic fake ads; mobile SDK delivery needs device verification.\n");
                    running=false;EditorApplication.isPlaying=false;return;
            }
            step++;next=EditorApplication.timeSinceStartup+2;
        }catch(Exception e){running=false;if(G!=null)Set("saveActive",false);File.WriteAllText(Root+"results.txt","FAIL step "+step+": "+e);Debug.LogException(e);EditorApplication.isPlaying=false;}
    }
    sealed class ReviewProvider : ICashflowAdProvider
    {
        public bool ready,visible;public Action earn,close;
        public bool RewardReady=>ready;
        public bool interstitialReady,throwInterstitial;public int interstitialShows;
        public bool InterstitialReady=>interstitialReady;
        public bool PrivacyRequired=>false;
        public float BannerHeight=>visible?136:0;
        public void Start(CashflowAdSettings s){}
        public void Tick(){}
        public void SetBanner(bool show,bool gameplay=false){visible=show;}
        public void ShowInterstitial(Action closed){interstitialReady=false;interstitialShows++;if(throwInterstitial)throw new Exception("Simulated show failure");close=closed;}
        public void ShowReward(Action earned,Action closed){ready=false;earn=earned;close=closed;}
        public void ShowPrivacy(Action closed){closed();}
        public void Dispose(){}
    }
}

