using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

public partial class CashflowGame
{
    [Serializable] class PendingDecision
    {
        public string kind="",title,detail,assetId; public Deal deal;
        public bool big; public int amount,turns; public Color color;
    }
    [Serializable] class SavedGame
    {
        public int version=1,selectedDream,cash,children,position,fastPosition,skipTurns,bankLoan,lastRoll;
        public bool fastTrack,charity,gameWon,gameOver,fastTrackIntro,reducedMotion;
        public Profession profession;
        public List<OwnedInvestment> portfolio;
        public List<PersonalDebt> debts;
        public List<Deal> smallPile,bigPile;
        public Deal lastSmall,lastBig;
        public List<HistoryEntry> history;
        public int historyCash,historyFlow,historyDebt;
        public PendingDecision pending;
    }
    [Serializable] class SaveEnvelope { public string payload,checksum; }
    PendingDecision pendingDecision=new PendingDecision();
    bool saveActive,restoring,saveErrorShown,recoveredSave;
    string lastSavedPayload;
    float nextSaveCheck;
    // Tests override this path on an isolated controller; production always uses persistentDataPath.
    string savePathOverride;
    string SavePath=>savePathOverride??Path.Combine(Application.persistentDataPath,"cashflow-save-v1.json");
    // Desktop Unity uses the product name in its data folder; keep pre-branding saves discoverable.
    string LegacySavePath=>savePathOverride==null?Path.Combine(Path.GetDirectoryName(Application.persistentDataPath),"Cashflow Unity","cashflow-save-v1.json"):SavePath;
    bool HasSavedGame()=>File.Exists(SavePath)||File.Exists(SavePath+".bak")||File.Exists(LegacySavePath)||File.Exists(LegacySavePath+".bak");
    static bool resumeRequested;
    static string Digest(string payload){using(var hash=SHA256.Create())return Convert.ToBase64String(hash.ComputeHash(Encoding.UTF8.GetBytes(payload)));}
    SavedGame CaptureGame()=>new SavedGame {
        profession=profession,selectedDream=selectedDream,cash=cash,children=children,position=position,fastPosition=fastPosition,
        skipTurns=skipTurns,bankLoan=bankLoan,lastRoll=lastRoll,fastTrack=fastTrack,charity=charity,gameWon=gameWon,gameOver=gameOver,fastTrackIntro=fastTrackIntro,
        reducedMotion=world!=null&&world.ReducedMotion,portfolio=portfolio,debts=personalDebts,smallPile=smallDrawPile,bigPile=bigDrawPile,
        lastSmall=lastSmallDeal,lastBig=lastBigDeal,history=history,historyCash=historyCash,historyFlow=historyFlow,historyDebt=historyDebt,pending=pendingDecision
    };
    bool SaveCheckpoint(bool moving=false)
    {
        if(!saveActive||restoring||(!moving&&rolling))return true;
        if(!moving)CheckGameOver();
        try{
            string payload=JsonUtility.ToJson(CaptureGame());if(payload==lastSavedPayload)return true;
            string text=JsonUtility.ToJson(new SaveEnvelope{payload=payload,checksum=Digest(payload)});
            Directory.CreateDirectory(Path.GetDirectoryName(SavePath));
            using(var file=new FileStream(SavePath+".tmp",FileMode.Create,FileAccess.Write,FileShare.None)){
                byte[] bytes=Encoding.UTF8.GetBytes(text);file.Write(bytes,0,bytes.Length);file.Flush(true);
            }
            // Replace leaves the last complete checkpoint as a recovery copy.
            if(File.Exists(SavePath))File.Replace(SavePath+".tmp",SavePath,recoveredSave?null:SavePath+".bak");else File.Move(SavePath+".tmp",SavePath);
            lastSavedPayload=payload;saveErrorShown=false;recoveredSave=false;return true;
        }catch(Exception e){
            if(!saveErrorShown){Debug.LogWarning("Cashflow save failed: "+e.Message);Receipt("PROGRESS COULD NOT BE SAVED","Keep the game open and check device storage.",C("#873d3a"));saveErrorShown=true;}
            return false;
        }
    }
    bool ReadSave(out SavedGame saved,out bool recovered)
    {
        recovered=false;saved=null;
        var paths=new List<string>{SavePath,SavePath+".bak"};
        if(!File.Exists(SavePath)&&!File.Exists(SavePath+".bak")&&LegacySavePath!=SavePath){paths.Add(LegacySavePath);paths.Add(LegacySavePath+".bak");}
        foreach(string path in paths){
            try{
                if(!File.Exists(path))continue;
                var envelope=JsonUtility.FromJson<SaveEnvelope>(File.ReadAllText(path));
                if(envelope==null||string.IsNullOrEmpty(envelope.payload)||Digest(envelope.payload)!=envelope.checksum)continue;
                var data=JsonUtility.FromJson<SavedGame>(envelope.payload);
                if(!ValidSave(data))continue;
                saved=data;recovered=path!=SavePath;return true;
            }catch(Exception){ /* An incomplete/corrupt checkpoint must never reset a game silently. */ }
        }
        return false;
    }
    bool ValidSave(SavedGame s)
    {
        if(s==null||s.version!=1||s.profession==null||string.IsNullOrEmpty(s.profession.name)||s.selectedDream<0||s.selectedDream>=dreams.Length||s.position<0||s.position>=24||s.fastPosition<0||s.fastPosition>=24||s.children<0||s.children>3||s.bankLoan<0||s.skipTurns<0||s.portfolio==null||s.debts==null||s.smallPile==null||s.bigPile==null||s.history==null||s.pending==null)return false;
        long linked=0;var ids=new HashSet<string>();
        foreach(var a in s.portfolio){if(a==null||string.IsNullOrEmpty(a.id)||!ids.Add(a.id)||a.upfrontCost<0||a.monthlyIncome<0||a.outstandingBankLoan<0)return false;linked+=a.outstandingBankLoan;}
        if(linked>s.bankLoan)return false;
        foreach(var d in s.debts)if(d==null||d.balance<0||d.payment<0)return false;
        foreach(var d in s.smallPile)if(d==null||d.down<0)return false;
        foreach(var d in s.bigPile)if(d==null||d.down<0)return false;
        var p=s.pending;
        switch(p.kind){
            case "":case "choice":case "charity":case "dream":case "intro":case "event":return true;
            case "deal":case "loan":return p.deal!=null&&p.deal.down>=0&&p.deal.flow>=0;
            case "bill":return p.amount>=0&&p.turns>=0;
            case "market":return p.amount>=0&&ids.Contains(p.assetId);
            case "move":return p.amount>=1&&p.amount<=12;
            default:return false;
        }
    }
    bool ResumeGame()
    {
        if(!ReadSave(out var s,out bool recovered)){saveActive=false;ShowSetup();ShowSaveMessage("Save unavailable","The saved game could not be read. It has not been replaced. You can start a new game.");return false;}
        restoring=true;
        try{
            profession=s.profession;int professionIndex=Array.FindIndex(professions,p=>p.name==profession.name);if(professionIndex>=0)professions[professionIndex]=profession;
            selectedDream=s.selectedDream;dream=dreams[selectedDream];cash=s.cash;children=s.children;position=s.position;fastPosition=s.fastPosition;
            skipTurns=s.skipTurns;bankLoan=s.bankLoan;lastRoll=s.lastRoll;fastTrack=s.fastTrack;charity=s.charity;gameWon=s.gameWon;gameOver=s.gameOver;fastTrackIntro=s.fastTrackIntro;
            portfolio.Clear();portfolio.AddRange(s.portfolio);personalDebts.Clear();personalDebts.AddRange(s.debts);passive=0;foreach(var a in portfolio)passive+=a.monthlyIncome;
            smallDrawPile.Clear();smallDrawPile.AddRange(s.smallPile);bigDrawPile.Clear();bigDrawPile.AddRange(s.bigPile);lastSmallDeal=s.lastSmall;lastBigDeal=s.lastBig;
            history.Clear();history.AddRange(s.history);historyCash=s.historyCash;historyFlow=s.historyFlow;historyDebt=s.historyDebt;
            pendingDecision=new PendingDecision();BuildGame();world.ReducedMotion=s.reducedMotion;
            RestoreDecision(s.pending);Refresh();saveActive=true;lastSavedPayload=null;recoveredSave=recovered;
            if(recovered)Receipt("PROGRESS RECOVERED","Loaded the previous complete checkpoint.",C("#873d3a"));
        }finally{restoring=false;}
        // Leave a valid recovery copy intact until the next real state change.
        lastSavedPayload=JsonUtility.ToJson(CaptureGame());
        if(s.pending.kind=="move"){lastSavedPayload=null;SaveCheckpoint();}
        return true;
    }
    void RestoreDecision(PendingDecision p)
    {
        if(gameWon){Win();return;}
        if(gameOver){ShowGameOver();return;}
        switch(p.kind){
            case "choice":ShowDealChoice();break;
            case "deal":OfferDeal(p.deal,p.big);break;
            case "loan":ShowLoanOffer(p.deal,p.big);break;
            case "bill":ShowRequiredExpense(p.title,p.detail,p.amount,p.turns);break;
            case "market":ReviewMarketSale(portfolio.Find(a=>a.id==p.assetId),p.amount,p.detail);break;
            case "charity":ShowCharityChoice();break;
            case "dream":ShowDreamPurchase();break;
            case "intro":ShowFastTrackIntro();break;
            case "event":ShowEventPanel(p.title,p.detail,p.color);break;
            case "move":FinishSavedMovement(p.amount);break;
        }
    }
    void FinishSavedMovement(int steps)
    {
        int old=fastTrack?fastPosition:position;
        if(fastTrack)fastPosition=(old+steps)%24;else position=(old+steps)%24;
        pendingDecision=new PendingDecision();MoveToken();CollectMovementPaydays(old,steps);if(!CheckGameOver())Land(fastTrack?fastTiles[fastPosition]:ratTiles[position]);
    }
    void LateUpdate(){if(Time.unscaledTime<nextSaveCheck)return;nextSaveCheck=Time.unscaledTime+.5f;SaveCheckpoint();}
    void OnApplicationPause(bool paused){if(paused)SaveCheckpoint();}
    void OnApplicationFocus(bool focused){if(!focused)SaveCheckpoint();}
    void OnApplicationQuit(){SaveCheckpoint();}
    void AddResumeButton()
    {
        if(!HasSavedGame())return;
        var button=OpeningButton(uiRoot,"RESUME GAME",()=>{resumeRequested=true;SceneManager.LoadScene("GameBoard");});button.name="Resume Game";
        Place(button.GetComponent<RectTransform>(),-450,302,270,46);SetButtonText(button,C("#21756a"),20);
    }
    void ShowSaveMessage(string title,string text,Action confirm=null)
    {
        var panel=Modal(new Vector2(700,340));panel.GetComponent<UnityEngine.UI.Image>().color=C("#fff5df");
        var heading=Label(panel.transform,title,26);heading.color=C("#302540");Place(heading.rectTransform,0,110,640,48);
        var copy=Label(panel.transform,text,21);copy.color=C("#51445b");Place(copy.rectTransform,0,15,630,115);
        var back=Button(panel.transform,confirm==null?"BACK":"KEEP SAVED GAME",C("#21756a"),CloseModal);back.name="Keep Saved Game";Place(back.GetComponent<RectTransform>(),confirm==null?0:-165,-115,290,52);
        if(confirm!=null){var start=Button(panel.transform,"START NEW GAME",C("#a84036"),()=>{CloseModal();confirm();});start.name="Replace Saved Game";Place(start.GetComponent<RectTransform>(),165,-115,290,52);}
    }
    void ConfirmNewGame(Action start)
    {
        if(HasSavedGame())ShowSaveMessage("Start a new game?","Starting over replaces your saved game. Choose Resume Game to continue your progress.",start);
        else start();
    }
}
