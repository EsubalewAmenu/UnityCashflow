using System;
using System.IO;
using Cashflow.Domain;
using UnityEditor;
using UnityEngine;

public static class CommandRulesReview
{
    static int checks;
    static void Check(bool ok,string message){checks++;if(!ok)throw new Exception(message);}
    sealed class Dice:IDiceSource {public int calls;public int NextDie(){calls++;return 2;}}
    sealed class Board:IBoardRules
    {
        public int TrackLength(PlayerState p)=>24;
        public bool IsPayday(PlayerState p,int pos)=>pos==5;
        public int Income(PlayerState p)=>500;
        public int Expenses(PlayerState p)=>200;
        public Decision Landing(PlayerState p,int pos)=>new Decision{id="deal-turn-1",purchase=true,cost=2000,monthlyIncome=200};
    }
    static ActionRequest Request(MatchState s,ActionKind kind,string id)=>new ActionRequest{id=id,playerId=s.players[s.active].id,expectedRevision=s.revision,kind=kind,decisionId=s.decision?.id};
    static MatchState Match()=>new MatchState{id="match",players={new PlayerState{id="one",cash=100},new PlayerState{id="two",cash=900}}};
    public static void Run()
    {
        string dir="work/command-rules-review";Directory.CreateDirectory(dir);
        try{
            var s=Match();var dice=new Dice();var roll=Request(s,ActionKind.Roll,"roll");
            Check(!CommandRules.Execute(s,"two",roll,dice).accepted,"Spoofed identity accepted");
            var wrong=Request(s,ActionKind.Roll,"wrong");wrong.playerId="two";
            Check(!CommandRules.Execute(s,"two",wrong,dice).accepted,"Out of turn accepted");
            s.players[0].position=4;
            Check(CommandRules.Execute(s,"one",roll,dice).accepted&&s.diceTotal==2&&s.phase==TurnPhase.Moving,"Roll failed");
            Check(CommandRules.Execute(s,"one",roll,dice).duplicate&&dice.calls==1,"Retry rerolled dice");
            var changed=Request(s,ActionKind.Borrow,"roll");changed.amount=1000;
            Check(!CommandRules.Execute(s,"one",changed,dice).accepted,"Reused action ID accepted different data");
            Check(!CommandRules.Execute(s,"one",Request(s,ActionKind.Roll,"roll2"),dice).accepted,"Second roll accepted");
            Check(CommandRules.ResolveMovement(s,s.revision,new Board())&&s.players[0].cash==400&&s.players[0].position==6,"Passing Payday incorrect");
            Check(!CommandRules.ResolveMovement(s,s.revision,new Board())&&s.players[0].cash==400,"Movement retried payment");
            Check(!CommandRules.Execute(s,"one",Request(s,ActionKind.EndTurn,"early"),dice).accepted,"Unresolved turn completed");
            var buy=Request(s,ActionKind.Buy,"buy");
            string before=JsonUtility.ToJson(s);
            Check(!CommandRules.Execute(s,"one",buy,dice).accepted&&JsonUtility.ToJson(s)==before,"Unaffordable purchase mutated state");
            buy.borrowShortfall=true;
            Check(CommandRules.Execute(s,"one",buy,dice).accepted&&s.players[0].cash==400&&s.players[0].bankLoan==2000&&s.players[0].passive==200,"Borrow and buy incorrect");
            Check(CommandRules.Execute(s,"one",buy,dice).duplicate&&s.players[0].investmentIds.Count==1,"Duplicate bought second investment");
            Check(s.players[1].cash==900&&s.players[1].bankLoan==0,"Other player's finances changed");
            var end=Request(s,ActionKind.EndTurn,"end");Check(CommandRules.Execute(s,"one",end,dice).accepted&&s.active==1&&s.turn==2,"Turn rotation failed");
            Check(CommandRules.Execute(s,"one",end,dice).duplicate&&s.turn==2,"Duplicate turn skipped player");
            s=JsonUtility.FromJson<MatchState>(JsonUtility.ToJson(s));
            Check(CommandRules.Execute(s,"one",buy,dice).duplicate,"Reload lost duplicate protection");
            var stale=Request(s,ActionKind.Borrow,"stale");stale.amount=1000;stale.expectedRevision--;
            Check(!CommandRules.Execute(s,"two",stale,dice).accepted,"Stale revision accepted");
            var borrow=Request(s,ActionKind.Borrow,"borrow");borrow.amount=-1000;
            Check(!CommandRules.Execute(s,"two",borrow,dice).accepted,"Negative borrowing accepted");
            borrow.amount=1000;Check(CommandRules.Execute(s,"two",borrow,dice).accepted&&s.players[1].cash==1900,"Borrow failed");
            s.players[1].cash=int.MaxValue;var overflow=Request(s,ActionKind.Borrow,"overflow");overflow.amount=1000;before=JsonUtility.ToJson(s);
            Check(!CommandRules.Execute(s,"two",overflow,dice).accepted&&JsonUtility.ToJson(s)==before,"Overflow partially applied");
            s=Match();s.phase=TurnPhase.Decision;s.decision=new Decision{id="bill",required=true,cost=1100,skipTurns=2};
            Check(!CommandRules.Execute(s,"one",Request(s,ActionKind.Pass,"pass"),dice).accepted,"Mandatory bill passed");
            var payment=Request(s,ActionKind.Pay,"pay");payment.borrowShortfall=true;
            Check(CommandRules.Execute(s,"one",payment,dice).accepted&&s.players[0].cash==0&&s.players[0].skipTurns==2,"Bill settlement failed");
            Check(CommandRules.Execute(s,"one",payment,dice).duplicate&&s.players[0].bankLoan==1000,"Duplicate bill charged twice");
            s=Match();s.players[0].skipTurns=2;int calls=dice.calls;
            Check(CommandRules.Execute(s,"one",Request(s,ActionKind.Roll,"skip"),dice).accepted&&s.players[0].skipTurns==1&&dice.calls==calls,"Skipped turn rolled dice");
            s.phase=TurnPhase.Complete;s.players[1].eliminated=true;
            Check(CommandRules.Execute(s,"one",Request(s,ActionKind.EndTurn,"skip-eliminated"),dice).accepted&&s.active==0,"Eliminated player retained turn");
            s=Match();s.phase=TurnPhase.Decision;s.decision=new Decision{id="charity",kind=DecisionKind.Charity,income=1000};
            var donation=Request(s,ActionKind.Donate,"donate");
            Check(CommandRules.Execute(s,"one",donation,dice).accepted&&s.players[0].cash==0&&s.players[0].bonusDice,"Donation failed");
            Check(CommandRules.Execute(s,"one",donation,dice).duplicate&&s.players[0].cash==0,"Donation charged twice");
            s.phase=TurnPhase.AwaitRoll;calls=dice.calls;
            Check(CommandRules.Execute(s,"one",Request(s,ActionKind.Roll,"bonus-roll"),dice).accepted&&dice.calls==calls+2&&!s.players[0].bonusDice,"Charity dice not consumed once");
            s=Match();s.phase=TurnPhase.Decision;s.players[0].bankLoan=1000;s.players[0].passive=200;
            s.players[0].investments.Add(new InvestmentState{id="asset",linkedLoan=1000,monthlyIncome=200});s.players[0].investmentIds.Add("asset");
            s.decision=new Decision{id="sale",kind=DecisionKind.Market,assetId="asset",cost=1200};
            var sale=Request(s,ActionKind.Sell,"sell");
            Check(CommandRules.Execute(s,"one",sale,dice).accepted&&s.players[0].cash==300&&s.players[0].bankLoan==0&&s.players[0].passive==0&&s.players[0].investments.Count==0,"Sale did not settle owned asset");
            Check(CommandRules.Execute(s,"one",sale,dice).duplicate&&s.players[0].cash==300,"Sale paid twice");
            s=Match();s.phase=TurnPhase.Decision;s.decision=new Decision{id="dream",kind=DecisionKind.Dream,cost=100};
            var dream=Request(s,ActionKind.BuyDream,"dream-buy");
            Check(!CommandRules.Execute(s,"one",dream,dice).accepted,"Rat Race player bought dream");
            Check(!CommandRules.Execute(s,"one",Request(s,ActionKind.Pay,"dream-bypass"),dice).accepted,"Pay bypassed dream rules");
            s.players[0].fastTrack=true;
            Check(CommandRules.Execute(s,"one",dream,dice).accepted&&s.finished&&s.players[0].won&&s.players[0].cash==0,"Dream victory failed");
            Check(CommandRules.Execute(s,"one",dream,dice).duplicate,"Winning retry rejected or duplicated");
            s=Match();s.phase=TurnPhase.Decision;s.decision=new Decision{id="baby",kind=DecisionKind.Baby,required=true};
            var baby=Request(s,ActionKind.Acknowledge,"baby-event");
            Check(CommandRules.Execute(s,"one",baby,dice).accepted&&s.players[0].children==1,"Baby event missing");
            Check(CommandRules.Execute(s,"one",baby,dice).duplicate&&s.players[0].children==1,"Baby repeated");
            Check(CashflowEventRules.AddChild(3)==3&&CashflowEventRules.FastTrackLoss("TAX AUDIT",1000)==100&&CashflowEventRules.FastTrackLoss("DIVORCE",-100)==0,"Event limits");
            s=Match();s.phase=TurnPhase.Decision;s.decision=new Decision{id="choice",kind=DecisionKind.DeckChoice};
            s.smallDeck.Add(new Decision{id="card",purchase=true,cost=10,monthlyIncome=0});
            var draw=Request(s,ActionKind.DrawSmall,"draw");
            Check(CommandRules.Execute(s,"one",draw,dice).accepted&&s.smallDeck.Count==0&&s.decision.cost==10,"Deck draw failed");
            s=JsonUtility.FromJson<MatchState>(JsonUtility.ToJson(s));
            Check(CommandRules.Execute(s,"one",draw,dice).duplicate&&s.smallDeck.Count==0,"Restored draw consumed extra card");
            var pile=new System.Collections.Generic.List<string>();var pool=new[]{"a","b","c"};
            var seen=new System.Collections.Generic.HashSet<string>();
            for(int n=0;n<3;n++)seen.Add(CashflowEventRules.Draw(pile,pool,null,(a,b)=>a==b,max=>max-1));
            Check(seen.Count==3&&pile.Count==0,"Deck repeated within cycle");
            Check(CashflowEventRules.Draw(pile,pool,"c",(a,b)=>a==b,max=>max-1)!="c","Reshuffle immediately repeated last card");
            s=Match();s.players[0].cash=5000;s.players[0].bankLoan=2000;
            s.players[0].investments.Add(new InvestmentState{id="rental",linkedLoan=2000});
            var repay=Request(s,ActionKind.RepayBank,"repay-bank");repay.amount=1000;
            Check(CommandRules.Execute(s,"one",repay,dice).accepted&&s.players[0].cash==4000&&s.players[0].investments[0].linkedLoan==1000,"Bank repayment allocation");
            Check(CommandRules.Execute(s,"one",repay,dice).duplicate&&s.players[0].bankLoan==1000,"Duplicate bank repayment");
            s.players[0].debts.Add(new DebtState{id="car",balance=500,payment=50});
            var debtPay=Request(s,ActionKind.RepayDebt,"repay-car");debtPay.assetId="car";debtPay.amount=500;
            Check(CommandRules.Execute(s,"one",debtPay,dice).accepted&&s.players[0].cash==3500&&s.players[0].debts[0].balance==0,"Personal debt repayment");
            Check(CommandRules.Execute(s,"one",debtPay,dice).duplicate,"Duplicate personal debt repayment");
            s=Match();s.phase=TurnPhase.Decision;s.decision=new Decision{id="charity",kind=DecisionKind.Charity};
            var reward=Request(s,ActionKind.RewardDice,"reward");
            Check(!CommandRules.Execute(s,"one",reward,dice).accepted&&!s.players[0].bonusDice,"Unverified reward granted");
            Check(CommandRules.Execute(s,"one",reward,dice,true).accepted&&s.players[0].bonusDice&&s.players[0].cash==100,"Reward spent money or did not grant dice");
            Check(CommandRules.Execute(s,"one",reward,dice,true).duplicate,"Reward retry");
            s=Match();s.players[0].passive=500;s.players[0].baseExpenses=500;
            Check(!CommandRules.Execute(s,"one",Request(s,ActionKind.EnterFastTrack,"escape-equal"),dice).accepted,"Escape at equal income");
            s.players[0].passive=501;
            Check(CommandRules.Execute(s,"one",Request(s,ActionKind.EnterFastTrack,"escape"),dice).accepted&&s.players[0].fastTrack,"Escape unavailable");
            s=Match();s.phase=TurnPhase.Decision;s.decision=new Decision{id="notice",kind=DecisionKind.Notice,resumeAwaitRoll=true};
            Check(CommandRules.Execute(s,"one",Request(s,ActionKind.Acknowledge,"notice"),dice).accepted&&s.phase==TurnPhase.AwaitRoll,"Pre-roll notice consumed turn");
            s=Match();
            Check(!CommandRules.Execute(s,"one",Request(s,ActionKind.CollectPayday,"untrusted-payday"),dice).accepted,"Untrusted payday accepted");
            File.WriteAllText(dir+"/result.txt","PASS: "+checks+" checks: identity, turn ownership, stale revisions, retries, restore, dice, movement, Payday, purchase, borrowing, bills, overflow and eliminated players.");
            EditorApplication.Exit(0);
        }catch(Exception e){File.WriteAllText(dir+"/result.txt","FAIL: "+e);EditorApplication.Exit(1);}
    }
}
