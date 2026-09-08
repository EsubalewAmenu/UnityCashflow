using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

public static class DealDeckReview
{
    public static void Validate(CashflowGame game)
    {
        var flags=BindingFlags.Instance|BindingFlags.NonPublic;
        Func<string,object> get=n=>typeof(CashflowGame).GetField(n,flags).GetValue(game);
        Func<string,object[],object> call=(n,a)=>typeof(CashflowGame).GetMethod(n,flags).Invoke(game,a);
        Action<bool,string> check=(ok,message)=>{if(!ok)throw new Exception(message);};
        var small=(CashflowGame.Deal[])get("smallDeals");var big=(CashflowGame.Deal[])get("bigDeals");
        call("ResetDealDecks",null);
        foreach(bool large in new[]{false,true}){
            var pool=large?big:small;CashflowGame.Deal last=null;
            for(int cycle=0;cycle<4;cycle++){
                var seen=new HashSet<CashflowGame.Deal>();
                for(int i=0;i<pool.Length;i++){
                    var card=(CashflowGame.Deal)call("DrawDeal",new object[]{large});
                    check(Array.IndexOf(pool,card)>=0 && seen.Add(card),"Wrong deck or repeated card within cycle");
                    check(card!=last,"Immediate repeat across shuffle boundary");last=card;
                }
            }
        }
        call("ResetDealDecks",null);call("DrawDeal",new object[]{false});
        int remaining=((List<CashflowGame.Deal>)get("smallDrawPile")).Count;
        call("DrawDeal",new object[]{true});
        check(((List<CashflowGame.Deal>)get("smallDrawPile")).Count==remaining,"Drawing big advanced the small deck");
        call("StartGame",null);
        check(((List<CashflowGame.Deal>)get("smallDrawPile")).Count==0 && get("lastSmallDeal")==null,"New game did not reset deck");
        var growth=Array.Find(small,d=>d.down==10 && d.flow==0);
        check(growth!=null,"Missing affordable resale-only investment");
        typeof(CashflowGame).GetField("cash",flags).SetValue(game,400);
        call("PurchaseInvestment",new object[]{growth,0});
        var portfolio=(List<CashflowGame.OwnedInvestment>)get("portfolio");
        check((int)get("cash")==390 && (int)get("passive")==0,"Resale-only purchase changed income or charged wrong price");
        check((bool)call("SellInvestment",new object[]{portfolio[0],17}),"Resale-only asset cannot sell");
        check((int)get("cash")==407 && portfolio.Count==0,"Resale settlement incorrect");
        call("StartGame",null);typeof(CashflowGame).GetField("cash",flags).SetValue(game,0);
        var tool=Array.Find(small,d=>d.down==250);
        int loan=(int)call("LoanNeeded",new object[]{tool});
        call("PurchaseInvestment",new object[]{tool,loan});
        portfolio=(List<CashflowGame.OwnedInvestment>)get("portfolio");var asset=portfolio[0];
        check((int)get("cash")==750 && (int)get("bankLoan")==1000,"Small investment loan rounding incorrect");
        check(!(bool)call("SellInvestment",new object[]{asset,187}) && portfolio.Count==1 && (int)get("cash")==750,"Underfunded sale changed state");
        check((bool)call("SellInvestment",new object[]{asset,437}) && (int)get("cash")==187 && (int)get("bankLoan")==0 && (int)get("passive")==0,"Financed sale failed to settle loan/income");
        call("StartGame",null);
        File.WriteAllText("work/deal-copy-review/deck-results.txt","PASS: four complete cycles per deck, no within-cycle or boundary repeats, independent decks, new-game reset, $10 resale-only purchase/sale, small-investment borrowing and loan-shortfall sale guards.");
    }
}
