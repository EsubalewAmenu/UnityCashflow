using System;
using System.Collections.Generic;

namespace Cashflow.Domain
{
    public enum ActionKind { Roll, Buy, Borrow, Pay, Pass, EndTurn, Donate, Sell, BuyDream, Acknowledge, DrawSmall, DrawBig }
    public enum DecisionKind { Financial, Charity, Market, Dream, Baby, Loss, DeckChoice }
    [Serializable] public sealed class InvestmentState
    {
        public string id;public int monthlyIncome,linkedLoan;
        public InvestmentState Copy()=>new InvestmentState{id=id,monthlyIncome=monthlyIncome,linkedLoan=linkedLoan};
    }
    public enum TurnPhase { AwaitRoll, Moving, Decision, Complete }
    [Serializable] public sealed class PlayerState
    {
        public string id;
        public int cash,bankLoan,passive,position,skipTurns;
        public bool eliminated,bonusDice,fastTrack,won;
        public int children;
        public List<InvestmentState> investments=new List<InvestmentState>();
        public List<string> investmentIds=new List<string>();
        public PlayerState Copy()=>new PlayerState{id=id,cash=cash,bankLoan=bankLoan,passive=passive,position=position,skipTurns=skipTurns,eliminated=eliminated,bonusDice=bonusDice,fastTrack=fastTrack,won=won,children=children,investments=investments.ConvertAll(x=>x.Copy()),investmentIds=new List<string>(investmentIds)};
    }
    // Populated by the trusted rules adapter, never accepted from a player's request.
    [Serializable] public sealed class Decision
    {
        public string id,assetId;
        public DecisionKind kind;
        public int income;
        public bool purchase,required;
        public int cost,monthlyIncome,skipTurns;
    }
    [Serializable] public sealed class ActionRequest
    {
        public string id,playerId,decisionId;
        public long expectedRevision;
        public ActionKind kind;
        public int amount;
        public bool borrowShortfall;
    }
    [Serializable] public sealed class ActionReceipt
    {
        public string id,playerId,fingerprint;
        public long revision;
        public int diceTotal;
    }
    [Serializable] public sealed class MatchState
    {
        public string id;
        public long revision;
        public int active,turn=1,diceCount=1,diceTotal;
        public bool finished;
        public TurnPhase phase;
        public Decision decision;
        public List<PlayerState> players=new List<PlayerState>();
        public List<Decision> smallDeck=new List<Decision>(),bigDeck=new List<Decision>();
        public List<ActionReceipt> receipts=new List<ActionReceipt>();
    }
    public sealed class ActionResult
    {
        public bool accepted,duplicate;
        public string error;
        public ActionReceipt receipt;
    }
    public interface IDiceSource { int NextDie(); }
    public interface IBoardRules
    {
        int TrackLength(PlayerState player);
        bool IsPayday(PlayerState player,int position);
        int Income(PlayerState player);
        int Expenses(PlayerState player);
        Decision Landing(PlayerState player,int position);
    }

    // No Unity, scene, animation or networking dependencies. A future server calls
    // this with its authenticated player ID and its own random source. Serialize
    // request handling per match and persist state + receipts in one transaction.
    public static class CommandRules
    {
        public static void NormalizeRestoredState(MatchState state)
        {
            // Unity's inline serializer materializes a null Decision as an empty
            // object. Only a Decision-phase entry with an ID represents a card.
            // Never discard a real pending card or reset IDs/revision/receipts.
            if(state!=null&&state.phase!=TurnPhase.Decision&&state.decision!=null&&string.IsNullOrEmpty(state.decision.id))state.decision=null;
        }
        // Trusted continuation after Roll. Clients cannot supply payments or cards.
        // Retry after persistence is safe: the match is no longer in Moving phase.
        public static bool ResolveMovement(MatchState state,long revision,IBoardRules rules)
        {
            if(state==null||rules==null||state.finished||state.revision!=revision||revision==long.MaxValue||state.phase!=TurnPhase.Moving)return false;
            var player=state.players[state.active].Copy();int length=rules.TrackLength(player);
            if(length<1||player.position<0||player.position>=length||state.diceTotal<1||state.diceTotal>12)return false;
            try{
                for(int i=0;i<state.diceTotal;i++){
                    player.position=(player.position+1)%length;
                    if(rules.IsPayday(player,player.position))player.cash=CashflowRules.SettlePayday(player.cash,rules.Income(player),rules.Expenses(player));
                }
                player.eliminated=CashflowRules.IsBankrupt(player.cash,CashflowRules.NetCashflow(rules.Income(player),rules.Expenses(player)));
                var decision=player.eliminated?null:rules.Landing(player,player.position);
                state.players[state.active]=player;state.decision=decision;state.phase=decision==null?TurnPhase.Complete:TurnPhase.Decision;state.revision++;return true;
            }catch(OverflowException){return false;}
        }
        static ActionResult Reject(string reason)=>new ActionResult{error=reason};
        static string Fingerprint(ActionRequest r)=>((int)r.kind)+"|"+r.expectedRevision+"|"+(r.decisionId??"")+"|"+r.amount+"|"+r.borrowShortfall;
        public static ActionResult Execute(MatchState state,string authenticatedPlayer,ActionRequest request,IDiceSource dice)
        {
            if(state==null||request==null||string.IsNullOrWhiteSpace(request.id)||request.id.Length>128)return Reject("Invalid request");
            if(string.IsNullOrWhiteSpace(authenticatedPlayer)||request.playerId!=authenticatedPlayer)return Reject("Player identity mismatch");
            string fingerprint=Fingerprint(request);
            foreach(var old in state.receipts)
                if(old.id==request.id)return old.playerId==authenticatedPlayer&&old.fingerprint==fingerprint
                    ?new ActionResult{accepted=true,duplicate=true,receipt=old}:Reject("Action ID reused with different data");
            if(state.finished)return Reject("Match finished");
            if(request.expectedRevision!=state.revision)return Reject("Stale match revision");
            if(state.active<0||state.active>=state.players.Count||state.players[state.active].id!=authenticatedPlayer)return Reject("Not your turn");
            if(state.revision==long.MaxValue)return Reject("Revision exhausted");
            var player=state.players[state.active].Copy();
            var phase=state.phase;int total=state.diceTotal,next=state.active,turn=state.turn;bool finished=false;var decision=state.decision;List<Decision> drawnDeck=null;
            if(player.eliminated&&request.kind!=ActionKind.EndTurn)return Reject("Player eliminated");
            try{
                switch(request.kind){
                    case ActionKind.Roll:
                        if(phase!=TurnPhase.AwaitRoll)return Reject("Already rolled or decision pending");
                        if(player.skipTurns>0){player.skipTurns--;total=0;phase=TurnPhase.Complete;break;}
                        if(dice==null||state.diceCount<1||state.diceCount>2)return Reject("Dice source unavailable");
                        total=0;for(int i=0;i<(player.bonusDice?2:state.diceCount);i++){int die=dice.NextDie();if(die<1||die>6)return Reject("Invalid dice outcome");total+=die;}
                        player.bonusDice=false;phase=TurnPhase.Moving;break;
                    case ActionKind.Buy:
                    case ActionKind.Pay:
                        if(phase!=TurnPhase.Decision||decision==null||decision.id!=request.decisionId)return Reject("Decision no longer available");
                        if(decision.kind!=DecisionKind.Financial)return Reject("Wrong decision action");
                        if((request.kind==ActionKind.Buy)!=decision.purchase)return Reject("Wrong decision action");
                        if(decision.cost<0||decision.skipTurns<0)return Reject("Invalid decision terms");
                        int loan=request.borrowShortfall?CashflowRules.RequiredLoan(player.cash,decision.cost):0;
                        var paid=CashflowRules.Purchase(player.cash,player.bankLoan,decision.cost,loan);
                        player.cash=paid.cash;player.bankLoan=paid.loan;
                        if(decision.purchase){player.passive=checked(player.passive+decision.monthlyIncome);string assetId=state.id+":"+request.id;player.investmentIds.Add(assetId);player.investments.Add(new InvestmentState{id=assetId,monthlyIncome=decision.monthlyIncome,linkedLoan=loan});}
                        else player.skipTurns=decision.skipTurns;
                        decision=null;phase=TurnPhase.Complete;break;
                    case ActionKind.DrawSmall:
                    case ActionKind.DrawBig:
                        if(phase!=TurnPhase.Decision||decision==null||decision.kind!=DecisionKind.DeckChoice||decision.id!=request.decisionId)return Reject("Not choosing a deal deck");
                        drawnDeck=request.kind==ActionKind.DrawSmall?state.smallDeck:state.bigDeck;
                        if(drawnDeck==null||drawnDeck.Count==0)return Reject("Deck requires trusted refill");
                        var card=drawnDeck[drawnDeck.Count-1];
                        if(card==null||card.kind!=DecisionKind.Financial||!card.purchase)return Reject("Invalid deal card");
                        decision=new Decision{id=state.id+":"+request.id,kind=card.kind,purchase=true,cost=card.cost,monthlyIncome=card.monthlyIncome};
                        break;
                    case ActionKind.Donate:
                    case ActionKind.Sell:
                    case ActionKind.BuyDream:
                    case ActionKind.Acknowledge:
                        if(phase!=TurnPhase.Decision||decision==null||decision.id!=request.decisionId)return Reject("Decision no longer available");
                        if(request.kind==ActionKind.Donate){
                            if(decision.kind!=DecisionKind.Charity)return Reject("Not a charity decision");
                            player.cash=CashflowEventRules.Donate(player.cash,decision.income,player.bonusDice);player.bonusDice=true;
                        }else if(request.kind==ActionKind.Sell){
                            if(decision.kind!=DecisionKind.Market)return Reject("Not a market decision");
                            var asset=player.investments.Find(x=>x.id==decision.assetId);
                            if(asset==null)return Reject("Investment not owned");
                            var sale=CashflowEventRules.Sell(player.cash,player.bankLoan,decision.cost,asset.linkedLoan);
                            player.cash=sale.cash;player.bankLoan=sale.loan;player.passive=checked(player.passive-asset.monthlyIncome);
                            player.investments.Remove(asset);player.investmentIds.Remove(asset.id);
                        }else if(request.kind==ActionKind.BuyDream){
                            if(decision.kind!=DecisionKind.Dream)return Reject("Not a dream decision");
                            player.cash=CashflowEventRules.BuyDream(player.cash,decision.cost,player.fastTrack,player.won);player.won=true;finished=true;
                        }else{
                            if(decision.kind==DecisionKind.Baby)player.children=CashflowEventRules.AddChild(player.children);
                            else if(decision.kind==DecisionKind.Loss)player.cash=CashflowEventRules.Spend(player.cash,decision.cost);
                            else return Reject("Event requires a different action");
                        }
                        decision=null;phase=TurnPhase.Complete;break;
                    case ActionKind.Borrow:
                        if(phase==TurnPhase.Moving)return Reject("Movement unresolved");
                        var borrowed=CashflowRules.Borrow(player.cash,player.bankLoan,request.amount);player.cash=borrowed.cash;player.bankLoan=borrowed.loan;break;
                    case ActionKind.Pass:
                        if(phase!=TurnPhase.Decision||decision==null||decision.id!=request.decisionId||decision.required)return Reject("Cannot pass this decision");
                        decision=null;phase=TurnPhase.Complete;break;
                    case ActionKind.EndTurn:
                        if(phase!=TurnPhase.Complete||decision!=null)return Reject("Turn not resolved");
                        finished=true;
                        for(int i=1;i<=state.players.Count;i++){int candidate=(state.active+i)%state.players.Count;if(!state.players[candidate].eliminated){next=candidate;finished=false;break;}}
                        turn=checked(turn+1);phase=TurnPhase.AwaitRoll;total=0;break;
                    default:return Reject("Unknown action");
                }
            }catch(ArgumentException e){return Reject(e.Message);}catch(OverflowException){return Reject("Numeric limit exceeded");}
            // All calculations finish before any state is changed.
            if(drawnDeck!=null)drawnDeck.RemoveAt(drawnDeck.Count-1);
            state.players[state.active]=player;state.active=next;state.turn=turn;state.finished=finished;
            state.phase=phase;state.decision=decision;state.diceTotal=total;state.revision++;
            var receipt=new ActionReceipt{id=request.id,playerId=authenticatedPlayer,fingerprint=fingerprint,revision=state.revision,diceTotal=total};
            state.receipts.Add(receipt);return new ActionResult{accepted=true,receipt=receipt};
        }
    }
}
