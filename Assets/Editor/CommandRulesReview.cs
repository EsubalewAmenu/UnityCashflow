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
            File.WriteAllText(dir+"/result.txt","PASS: "+checks+" checks: identity, turn ownership, stale revisions, retries, restore, dice, movement, Payday, purchase, borrowing, bills, overflow and eliminated players.");
            EditorApplication.Exit(0);
        }catch(Exception e){File.WriteAllText(dir+"/result.txt","FAIL: "+e);EditorApplication.Exit(1);}
    }
}
