using System;
using System.Collections.Generic;

// Financial effects and deck ordering shared by the UI adapter and command engine.
public static class CashflowEventRules
{
    public static int DonationCost(int income)=>Math.Max(100,income/10);
    public static int Donate(int cash,int income,bool bonusActive)
    {
        if(bonusActive)throw new ArgumentException("Dice bonus already active");
        return Spend(cash,DonationCost(income));
    }
    public static int Spend(int cash,int amount)
    {
        if(amount<0||cash<amount)throw new ArgumentException("Insufficient cash or invalid price");
        return checked(cash-amount);
    }
    public static CashflowRules.Balance Sell(int cash,int loan,int price,int linkedLoan)
    {
        if(price<0||linkedLoan<0||loan<linkedLoan)throw new ArgumentException("Invalid sale terms");
        long remaining=(long)cash+price-linkedLoan;
        if(remaining<0)throw new ArgumentException("Cash does not cover linked loan");
        return new CashflowRules.Balance{cash=checked((int)remaining),loan=loan-linkedLoan};
    }
    public static int BuyDream(int cash,int price,bool fastTrack,bool won)
    {
        if(!fastTrack||won)throw new ArgumentException("Dream purchase unavailable");
        return Spend(cash,price);
    }
    public static int AddChild(int children)=>children>=3?children:checked(children+1);
    public static int FastTrackLoss(string tile,int cash)
    {
        cash=Math.Max(0,cash);
        switch(tile){case "TAX AUDIT":return cash/10;case "LAWSUIT":return Math.Min(cash,25000);case "DIVORCE":return cash/2;case "CORRECTION":return cash/5;default:throw new ArgumentException("Unknown loss event");}
    }
    public static T Draw<T>(List<T> pile,IList<T> pool,T previous,Func<T,T,bool> same,Func<int,int> random)
    {
        if(pool==null||pool.Count==0)throw new ArgumentException("Empty deck");
        if(pile.Count==0){
            // Shuffle a temporary copy so an invalid random source cannot damage the deck.
            var shuffled=new List<T>(pool);
            for(int i=shuffled.Count-1;i>0;i--){int j=random(i+1);if(j<0||j>i)throw new ArgumentException("Invalid random index");var temp=shuffled[i];shuffled[i]=shuffled[j];shuffled[j]=temp;}
            if(shuffled.Count>1&&same(shuffled[shuffled.Count-1],previous)){var temp=shuffled[0];shuffled[0]=shuffled[shuffled.Count-1];shuffled[shuffled.Count-1]=temp;}
            pile.AddRange(shuffled);
        }
        T card=pile[pile.Count-1];pile.RemoveAt(pile.Count-1);return card;
    }
}
