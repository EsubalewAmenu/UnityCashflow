using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public partial class CashflowGame
{
    // Transitional local adapter: reuse every existing financial rule while
    // isolating each seat. Online authority will replace this adapter, not UI rules.
    [Serializable] class LocalMatch
    {
        public int version=1,active,turn=1;
        public bool rolled;
        public SavedGame[] players=new SavedGame[2];
        public Cashflow.Domain.MatchState commands;
    }
    LocalMatch localMatch;
    bool localInitializing;
    Button localEndTurn;
    Text localTurnLabel;
    Cashflow.Domain.ActionRequest endTurnRequest;
    void EnsureLocalCommandState()
    {
        if(localMatch.commands!=null){Cashflow.Domain.CommandRules.NormalizeRestoredState(localMatch.commands);return;}
        localMatch.commands=new Cashflow.Domain.MatchState{id=Guid.NewGuid().ToString("N"),active=localMatch.active,turn=localMatch.turn};
        for(int i=0;i<2;i++)localMatch.commands.players.Add(new Cashflow.Domain.PlayerState{id=Guid.NewGuid().ToString("N")});
    }
    string localLastPayload;
    string localPathOverride;
    string LocalMatchPath=>localPathOverride??Path.Combine(Application.persistentDataPath,"cashflow-local-match-v1.json");
    SavedGame ClonePlayer(SavedGame state)=>JsonUtility.FromJson<SavedGame>(JsonUtility.ToJson(state));
    void AddLocalMatchButton()
    {
        var b=OpeningButton(uiRoot,"LOCAL 2 PLAYERS",ShowLocalMenu);b.name="Local Multiplayer";
        // Keep this in the action row above the finance summary, not behind it.
        Place(b.GetComponent<RectTransform>(),-450,-230,280,44);SetButtonText(b,C("#443050"),19);
    }
    void ShowLocalMenu()
    {
        var card=CardShell("LOCAL MULTIPLAYER","Play together on this phone",C("#21756a"),new Vector2(800,450));
        var copy=Label(card.transform,"Two players take turns on one device.\nRandom professions and dreams; shared deal decks.\nSingle-player progress stays separate.\nStarting a new match replaces the local match save.",21);copy.color=C("#51445b");Place(copy.rectTransform,0,25,720,175);
        var start=Button(card.transform,"NEW MATCH",C("#21756a"),StartLocalMatch);Place(start.GetComponent<RectTransform>(),-190,-120,300,50);
        var resume=Button(card.transform,"RESUME MATCH",C("#654174"),ResumeLocalMatch);Place(resume.GetComponent<RectTransform>(),190,-120,300,50);resume.interactable=File.Exists(LocalMatchPath)||File.Exists(LocalMatchPath+".bak");
        var back=Button(card.transform,"BACK",C("#654174"),CloseModal);Place(back.GetComponent<RectTransform>(),0,-185,200,40);
    }
    void StartLocalMatch()
    {
        CloseModal();localInitializing=true;localMatch=new LocalMatch();localLastPayload=null;
        int savedDream=PlayerPrefs.GetInt("CashflowDream",0);
        try{
            for(int i=0;i<2;i++){
                profession=professions[UnityEngine.Random.Range(0,professions.Length)];
                PlayerPrefs.SetInt("CashflowDream",UnityEngine.Random.Range(0,dreams.Length));
                StartGame();localMatch.players[i]=ClonePlayer(CaptureGame());
            }
            // Decks belong to the match. Transfer their current state at handoff.
            LoadLocalPlayer();
        }finally{PlayerPrefs.SetInt("CashflowDream",savedDream);localInitializing=false;}
        SaveCheckpoint();
    }
    void LoadLocalPlayer()
    {
        EnsureLocalCommandState();
        var s=localMatch.players[localMatch.active];
        ApplyPlayerState(ClonePlayer(s));RestoreDecision(s.pending);MoveToken();
        localTurnLabel=Label(uiRoot,"",17);Place(localTurnLabel.rectTransform,175,325,470,40);
        endTurnRequest=new Cashflow.Domain.ActionRequest{id=Guid.NewGuid().ToString("N"),playerId=localMatch.commands.players[localMatch.active].id,expectedRevision=localMatch.commands.revision,kind=Cashflow.Domain.ActionKind.EndTurn};
        var request=endTurnRequest;
        localEndTurn=Button(uiRoot,"END TURN",C("#21756a"),()=>EndLocalTurnCommand(request));localEndTurn.name="Local End Turn";Place(localEndTurn.GetComponent<RectTransform>(),460,-250,230,42);
        int other=1-localMatch.active;var opponent=localMatch.players[other];
        var marker=GameObject.CreatePrimitive(PrimitiveType.Sphere);marker.name="Player "+(other+1)+" token";marker.transform.SetParent(world.transform,false);
        marker.transform.position=BoardWorld.TrackPoint(opponent.fastTrack?opponent.fastPosition:opponent.position,opponent.fastTrack)+new Vector3(.12f,.12f,0);
        marker.transform.localScale=Vector3.one*.18f;
        // Reuse a renderer material so no additional material lifetime is needed.
        marker.GetComponent<Renderer>().sharedMaterial=world.Pawn.GetComponentInChildren<Renderer>().sharedMaterial;
        Destroy(marker.GetComponent<Collider>());
        UpdateLocalTurn();Refresh();
    }
    void UpdateLocalTurn()
    {
        if(localMatch==null||localInitializing)return;
        if(localTurnLabel!=null)localTurnLabel.text="PLAYER "+(localMatch.active+1)+"  •  TURN "+localMatch.turn;
        if(localEndTurn!=null)localEndTurn.interactable=localMatch.rolled&&!rolling&&modal==null&&!gameWon;
        if(rollButton!=null&&localMatch.rolled)rollButton.interactable=false;
    }
    void EndLocalTurn()
    {
        EndLocalTurnCommand(endTurnRequest);
    }
    void EndLocalTurnCommand(Cashflow.Domain.ActionRequest request)
    {
        if(localMatch==null||!localMatch.rolled||rolling||modal!=null||gameWon)return;
        localMatch.players[localMatch.active]=ClonePlayer(CaptureGame());
        if(localMatch.players[0].gameOver&&localMatch.players[1].gameOver){ShowEventPanel("MATCH OVER","Both players have been eliminated. Use the menu to leave.",C("#a84036"));return;}
        var commands=localMatch.commands;
        // During the local migration, the existing controller remains the trusted
        // rules adapter. Import its resolved phase; never expose this to a client API.
        commands.phase=Cashflow.Domain.TurnPhase.Complete;
        for(int i=0;i<2;i++)commands.players[i].eliminated=localMatch.players[i].gameOver;
        var result=Cashflow.Domain.CommandRules.Execute(commands,commands.players[localMatch.active].id,request,null);
        if(!result.accepted){Log("Could not end turn: "+result.error);return;}
        if(result.duplicate)return;
        int next=commands.active;
        var current=localMatch.players[localMatch.active];var target=localMatch.players[next];
        target.smallPile=current.smallPile;target.bigPile=current.bigPile;target.lastSmall=current.lastSmall;target.lastBig=current.lastBig;
        localMatch.active=next;localMatch.turn=commands.turn;localMatch.rolled=false;LoadLocalPlayer();SaveCheckpoint();
    }
    bool SaveLocalMatch(bool moving)
    {
        if(localInitializing||restoring||(!moving&&rolling))return true;
        try{
            localMatch.players[localMatch.active]=ClonePlayer(CaptureGame());
            string payload=JsonUtility.ToJson(localMatch);if(payload==localLastPayload)return true;
            Directory.CreateDirectory(Path.GetDirectoryName(LocalMatchPath));
            File.WriteAllText(LocalMatchPath+".tmp",JsonUtility.ToJson(new SaveEnvelope{payload=payload,checksum=Digest(payload)}));
            if(File.Exists(LocalMatchPath))File.Replace(LocalMatchPath+".tmp",LocalMatchPath,LocalMatchPath+".bak");else File.Move(LocalMatchPath+".tmp",LocalMatchPath);
            localLastPayload=payload;return true;
        }catch(Exception e){Debug.LogWarning("Local match save failed: "+e.Message);return false;}
    }
    void ResumeLocalMatch()
    {
        foreach(string path in new[]{LocalMatchPath,LocalMatchPath+".bak"}){
            try{
                if(!File.Exists(path))continue;
                var envelope=JsonUtility.FromJson<SaveEnvelope>(File.ReadAllText(path));
                if(envelope==null||envelope.payload==null||Digest(envelope.payload)!=envelope.checksum)continue;
                var match=JsonUtility.FromJson<LocalMatch>(envelope.payload);
                if(match==null||match.version!=1||match.active<0||match.active>1||match.players==null||match.players.Length!=2||!ValidSave(match.players[0])||!ValidSave(match.players[1]))continue;
                CloseModal();localMatch=match;restoring=true;
                try{LoadLocalPlayer();}finally{restoring=false;}
                saveActive=true;localLastPayload=null;SaveCheckpoint();return;
            }catch(Exception e){Debug.LogWarning("Cannot resume local match: "+e.Message);}
        }
        localMatch=null;ShowSaveMessage("MATCH UNAVAILABLE","The local match could not be restored. Your single-player save is unaffected.");
    }
    void ShowLocalElimination()
    {
        localMatch.rolled=true;
        ShowEventPanel("PLAYER "+(localMatch.active+1)+" ELIMINATED","Cash and monthly cashflow are both negative.\nContinue, then end your turn.",C("#a84036"));
    }
    void ShowLocalExit()
    {
        if(rolling||modal!=null)return;
        if(!SaveCheckpoint()){ShowSaveMessage("SAVE FAILED","Could not save the match. Keep playing and check device storage.");return;}
        localMatch=null;saveActive=false;SceneManager.LoadScene("ProfessionSelection");
    }
}
