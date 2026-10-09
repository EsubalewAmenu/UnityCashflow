using System;
using System.Collections.Generic;
using Cashflow.Domain;
using UnityEngine;

public partial class CashflowGame
{
    // Local authority boundary. UI supplies intents, not mutation callbacks.
    // Replace this adapter's trusted state/content/random providers for online play.
    MatchState soloCommands;
    string restoredDecisionId;
    MatchState GameplayCommands
    {
        get{
            if(localMatch!=null){EnsureLocalCommandState();return localMatch.commands;}
            if(soloCommands==null||string.IsNullOrEmpty(soloCommands.id)){
                soloCommands=new MatchState{id=Guid.NewGuid().ToString("N")};
                soloCommands.players.Add(new PlayerState{id=Guid.NewGuid().ToString("N")});
            }
            CommandRules.NormalizeRestoredState(soloCommands);return soloCommands;
        }
    }
    void ImportCommandPlayer()
    {
        var state=GameplayCommands;var old=state.players[state.active];
        var p=new PlayerState{id=old.id,cash=cash,bankLoan=bankLoan,passive=passive,children=children,position=fastTrack?fastPosition:position,
            skipTurns=skipTurns,fastTrack=fastTrack,won=gameWon,eliminated=gameOver,bonusDice=charity,
            salary=profession.salary,baseExpenses=profession.taxes+profession.other,childPayment=profession.childPay};
        foreach(var a in portfolio){p.investmentIds.Add(a.id);p.investments.Add(new InvestmentState{id=a.id,title=a.title,description=a.description,upfrontCost=a.upfrontCost,originalLoan=a.originalBankLoan,monthlyIncome=a.monthlyIncome,linkedLoan=a.outstandingBankLoan});}
        foreach(var d in personalDebts)p.debts.Add(new DebtState{id=d.title,balance=d.balance,payment=d.payment});
        state.players[state.active]=p;
    }
    void ApplyCommandPlayer()
    {
        var p=GameplayCommands.players[GameplayCommands.active];
        cash=p.cash;bankLoan=p.bankLoan;passive=p.passive;children=p.children;skipTurns=p.skipTurns;charity=p.bonusDice;fastTrack=p.fastTrack;gameWon=p.won;
        if(fastTrack)fastPosition=p.position;else position=p.position;
        var updated=new List<OwnedInvestment>();
        foreach(var a in p.investments){
            var asset=portfolio.Find(x=>x.id==a.id)??new OwnedInvestment{id=a.id};
            asset.title=a.title;asset.description=a.description;asset.upfrontCost=a.upfrontCost;asset.originalBankLoan=a.originalLoan;asset.monthlyIncome=a.monthlyIncome;asset.outstandingBankLoan=a.linkedLoan;updated.Add(asset);
        }
        portfolio.Clear();portfolio.AddRange(updated);
        foreach(var d in personalDebts){var saved=p.debts.Find(x=>x.id==d.title);if(saved!=null)d.balance=saved.balance;}
    }
    void BindCommandDecision(Decision decision)
    {
        ImportCommandPlayer();var state=GameplayCommands;
        decision.resumeAwaitRoll=decision.kind==DecisionKind.Notice&&(state.phase==TurnPhase.AwaitRoll||(localMatch!=null&&!localMatch.rolled));
        decision.id=string.IsNullOrEmpty(restoredDecisionId)?Guid.NewGuid().ToString("N"):restoredDecisionId;
        restoredDecisionId=null;pendingDecision.commandId=decision.id;
        state.decision=decision;state.phase=TurnPhase.Decision;
    }
    ActionRequest CommandIntent(ActionKind kind,int amount=0,string assetId=null,bool borrow=false)
    {
        ImportCommandPlayer();var s=GameplayCommands;
        return new ActionRequest{id=Guid.NewGuid().ToString("N"),playerId=s.players[s.active].id,expectedRevision=s.revision,decisionId=s.decision?.id,kind=kind,amount=amount,assetId=assetId,borrowShortfall=borrow};
    }
    bool DispatchGameplay(ActionRequest request,IDiceSource dice=null,bool reward=false,IBoardRules boardRules=null,bool continuation=false)
    {
        var s=GameplayCommands;
        var result=CommandRules.Execute(s,s.players[s.active].id,request,dice,reward,boardRules,continuation);
        if(!result.accepted){Log("Action unavailable: "+result.error);return false;}
        if(result.duplicate)return false;
        ApplyCommandPlayer();UpdateLocalTurn();return true;
    }
    bool SystemDecision(Decision decision)
    {
        BindCommandDecision(decision);return DispatchGameplay(CommandIntent(ActionKind.Acknowledge));
    }
    sealed class LocalDiceSource:IDiceSource
    {
        public readonly List<int> faces=new List<int>();
        public int NextDie(){int face=UnityEngine.Random.Range(1,7);faces.Add(face);return face;}
    }
    int[] RollCommand()
    {
        ImportCommandPlayer();var s=GameplayCommands;
        if(localMatch==null&&s.phase==TurnPhase.Complete&&s.decision==null){if(!DispatchGameplay(CommandIntent(ActionKind.EndTurn)))return null;}
        // The local presentation permits a new solo roll after an informational panel.
        if(s.phase!=TurnPhase.AwaitRoll)return null;
        var random=new LocalDiceSource();if(!DispatchGameplay(CommandIntent(ActionKind.Roll),random))return null;
        if(localMatch!=null)localMatch.rolled=true;
        lastRoll=s.diceTotal;return random.faces.ToArray();
    }
    sealed class LocalBoardRules:IBoardRules
    {
        readonly CashflowGame game;public LocalBoardRules(CashflowGame game){this.game=game;}
        public int TrackLength(PlayerState p)=>24;
        public bool IsPayday(PlayerState p,int pos)=>(p.fastTrack?game.fastTiles[pos]:game.ratTiles[pos])==(p.fastTrack?"CASHFLOW DAY":"PAYDAY");
        public int Income(PlayerState p)=>CommandRules.Income(p);
        public int Expenses(PlayerState p)=>CommandRules.Expenses(p);
        public Decision Landing(PlayerState p,int pos)=>null; // Existing local content presenter selects the next card.
    }
    bool SettleMovementCommand(int start,int steps)
    {
        var s=GameplayCommands;
        if(s.phase!=TurnPhase.Moving||s.diceTotal!=steps)return false;
        var request=CommandIntent(ActionKind.FinishMove);s.players[s.active].position=start;
        return DispatchGameplay(request,boardRules:new LocalBoardRules(this),continuation:true);
    }
    void ClearCommandDecision()
    {
        var s=localMatch!=null?localMatch.commands:soloCommands;
        if(s==null)return;
        if(s.phase==TurnPhase.Decision){bool awaiting=s.decision!=null&&s.decision.resumeAwaitRoll;s.decision=null;s.phase=awaiting||(localMatch!=null&&!localMatch.rolled)?TurnPhase.AwaitRoll:TurnPhase.Complete;}
    }
}
