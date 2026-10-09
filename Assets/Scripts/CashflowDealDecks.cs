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

    string DealPriceRange(bool big)
    {
        var pool=big?bigDeals:smallDeals;
        int min=int.MaxValue,max=0;
        foreach(var deal in pool){min=Mathf.Min(min,deal.down);max=Mathf.Max(max,deal.down);}
        return Money(min)+"–"+Money(max)+" upfront";
    }
}
