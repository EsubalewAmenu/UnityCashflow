using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad] public static class MarketReview
{
    static bool running;static int step;static double next;
    static readonly BindingFlags F=BindingFlags.NonPublic|BindingFlags.Instance;
    static CashflowGame G=>UnityEngine.Object.FindObjectOfType<CashflowGame>();
    static object Get(string n)=>typeof(CashflowGame).GetField(n,F).GetValue(G);
    static void Set(string n,object v)=>typeof(CashflowGame).GetField(n,F).SetValue(G,v);
    static object Call(string n,params object[] a)=>typeof(CashflowGame).GetMethod(n,F).Invoke(G,a);
    static List<CashflowGame.OwnedInvestment> Assets=>(List<CashflowGame.OwnedInvestment>)Get("portfolio");
    static void Check(bool b,string m){if(!b)throw new Exception(m);}
    static void Click(string n)=>GameObject.Find(n).GetComponent<Button>().onClick.Invoke();
    static MarketReview(){EditorApplication.update+=Tick;}
    static void Capture(string name)
    {
        Canvas.ForceUpdateCanvases();
        foreach(var t in GameObject.Find("Event Card").GetComponentsInChildren<Text>())Check(t.preferredHeight<=t.rectTransform.rect.height+2,"Clipped text: "+t.text);
        ScreenCapture.CaptureScreenshot("work/market-review/"+name+".png");
    }
    static void Tick()
    {
        if(EditorApplication.isCompiling)return;
        if(File.Exists("work/run-market-review")){
            if(!EditorApplication.isPlaying){EditorApplication.isPlaying=true;return;}
            File.Delete("work/run-market-review");Directory.CreateDirectory("work/market-review");running=true;step=0;next=EditorApplication.timeSinceStartup+2;
        }
        if(!running||EditorApplication.timeSinceStartup<next)return;
        try{
            switch(step){
                case 0:Screen.orientation=ScreenOrientation.LandscapeLeft;Call("StartGame");Set("cash",5000);Call("Land","MARKET");Check((int)Get("cash")==5000,"Empty market paid cash");break;
                case 1:
                    Call("CloseModal");var deal=new CashflowGame.Deal("Review rental",4000,220);
                    Call("PurchaseInvestment",deal,0);Call("PurchaseInvestment",deal,4000);Set("bankLoan",6000);Set("cash",400);
                    Call("ReviewMarketSale",Assets[1],5000,"A buyer wants your rental investment.");break;
                case 2:Capture("offer");break;
                case 3:
                    Click("Pass Market Sale");Check(Assets.Count==2&&(int)Get("cash")==400&&(int)Get("bankLoan")==6000&&(int)Get("passive")==440,"Pass changed finances");
                    Call("ReviewMarketSale",Assets[1],3000,"A cautious buyer makes a lower offer.");Check(!GameObject.Find("Accept Market Sale").GetComponent<Button>().interactable,"Shortfall sale enabled");break;
                case 4:Capture("shortfall");break;
                case 5:
                    Check(!(bool)Call("SellInvestment",Assets[1],3000),"Unaffordable sale accepted");
                    Set("cash",1500);Call("ReviewMarketSale",Assets[1],3000,"A cautious buyer makes a lower offer.");
                    var asset=Assets[1];var sell=GameObject.Find("Accept Market Sale").GetComponent<Button>();sell.onClick.Invoke();sell.onClick.Invoke();
                    Check(Assets.Count==1&&Assets[0].id!=asset.id&&(int)Get("cash")==500&&(int)Get("bankLoan")==2000&&(int)Get("passive")==220,"Sale settlement or duplicate guard failed");
                    Check(!(bool)Call("SellInvestment",asset,3000),"Stale asset sold again");break;
                case 6:
                    Call("CloseModal");Call("ReviewMarketSale",Assets[0],7000,"Strong demand brings a premium offer.");break;
                case 7:Capture("cash-owned");break;
                case 8:
                    Click("Accept Market Sale");Check(Assets.Count==0&&(int)Get("cash")==7500&&(int)Get("bankLoan")==2000&&(int)Get("passive")==0,"Debt-free sale touched other debt");
                    int before=(int)Get("cash");Call("Payday");Check((int)Get("cash")==before+(int)Call("Income")-(int)Call("Expenses"),"Post-sale Payday failed");
                    Call("CloseModal");Call("Land","MARKET");Check(Assets.Count==0,"Empty market recreated asset");
                    File.WriteAllText("work/market-review/results.txt","PASS: empty market, pass, financed and debt-free sales, cash shortfall block and contribution, duplicate/stale sale guards, separate same-name assets, unrelated debt preservation, passive income and Payday, offer text fit.\n");running=false;Call("CloseModal");return;
            }
            step++;next=EditorApplication.timeSinceStartup+2;
        }catch(Exception e){running=false;File.WriteAllText("work/market-review/results.txt","FAIL: "+e);Debug.LogException(e);}
    }
}
