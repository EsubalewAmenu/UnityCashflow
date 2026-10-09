using Cashflow.Domain;
using System.Collections.Generic;
using UnityEngine;

public partial class CashflowGame
{
    readonly List<Deal> smallDrawPile = new List<Deal>();
    readonly List<Deal> bigDrawPile = new List<Deal>();
    Deal lastSmallDeal, lastBigDeal;

    void ResetDealDecks()
    {
        smallDrawPile.Clear(); bigDrawPile.Clear();
        lastSmallDeal=null; lastBigDeal=null;
    }

    Deal DrawDeal(bool big)
    {
        var pile=big?bigDrawPile:smallDrawPile;
        var pool=big?bigDeals:smallDeals;
        var previous=big?lastBigDeal:lastSmallDeal;
        var deal=CashflowEventRules.Draw(pile,pool,previous,(a,b)=>b!=null&&a.title==b.title,max=>Random.Range(0,max));
        if(big)lastBigDeal=deal;else lastSmallDeal=deal;
        return deal;
    }

    Deal DrawDealCommand(bool big,ActionRequest intent)
    {
        var state=GameplayCommands;
        if(intent.expectedRevision!=state.revision||state.decision==null||intent.decisionId!=state.decision.id)return null;
        var pile=big?bigDrawPile:smallDrawPile;
        // Refill through the trusted local shuffler, then let the command consume it.
        if(pile.Count==0){var first=DrawDeal(big);pile.Add(first);}
        var deck=big?state.bigDeck:state.smallDeck;deck.Clear();foreach(var card in pile)deck.Add(DealDecision(card));
        var selected=pile[pile.Count-1];
        if(!DispatchGameplay(intent))return null;
        pile.RemoveAt(pile.Count-1);if(big)lastBigDeal=selected;else lastSmallDeal=selected;
        restoredDecisionId=state.decision.id;
        return selected;
    }
    string DealPriceRange(bool big)
    {
        var pool=big?bigDeals:smallDeals;
        int min=int.MaxValue,max=0;
        foreach(var deal in pool){min=Mathf.Min(min,deal.down);max=Mathf.Max(max,deal.down);}
        return Money(min)+"–"+Money(max)+" upfront";
    }
}
