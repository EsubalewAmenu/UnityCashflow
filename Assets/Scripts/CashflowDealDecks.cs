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
        if(pile.Count==0)
        {
            pile.AddRange(pool);
            for(int i=pile.Count-1;i>0;i--)
            {
                int j=Random.Range(0,i+1);
                var swap=pile[i];pile[i]=pile[j];pile[j]=swap;
            }
            // A fresh shuffle must not immediately repeat the last discarded card.
            if(pile.Count>1 && previous!=null && pile[pile.Count-1].title==previous.title)
            {
                var swap=pile[0];pile[0]=pile[pile.Count-1];pile[pile.Count-1]=swap;
            }
        }
        var deal=pile[pile.Count-1];pile.RemoveAt(pile.Count-1);
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
