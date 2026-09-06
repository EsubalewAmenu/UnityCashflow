using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public static class PortfolioReview
{
    static bool running;static int step;static double next;
    static readonly BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
    static CashflowGame Game=>UnityEngine.Object.FindObjectOfType<CashflowGame>();
    static object Get(string name)=>typeof(CashflowGame).GetField(name,Flags).GetValue(Game);
    static void Set(string name,object value)=>typeof(CashflowGame).GetField(name,Flags).SetValue(Game,value);
    static void Call(string name,params object[] args)=>typeof(CashflowGame).GetMethod(name,Flags).Invoke(Game,args);
    static void Click(string name)=>GameObject.Find(name).GetComponent<Button>().onClick.Invoke();
    static void Require(bool ok,string message){if(!ok)throw new Exception(message);}
    static List<CashflowGame.OwnedInvestment> Assets=>(List<CashflowGame.OwnedInvestment>)Get("portfolio");
    static PortfolioReview(){EditorApplication.update+=Tick;}
    static void Capture(string name)
    {
        Canvas.ForceUpdateCanvases();
        foreach(var text in GameObject.Find("Event Card").GetComponentsInChildren<Text>())
        {
            Require(text.preferredHeight<=text.rectTransform.rect.height+2,"Clipped text: "+text.text);
            var corners=new Vector3[4];text.rectTransform.GetWorldCorners(corners);
            foreach(var c in corners)Require(c.x>=0&&c.y>=0&&c.x<=Screen.width&&c.y<=Screen.height,"Outside screen: "+text.name);
        }
        ScreenCapture.CaptureScreenshot("work/portfolio-review/"+name+".png");
    }
    static void Tick()
    {
        if(EditorApplication.isCompiling)return;
        if(File.Exists("work/run-portfolio-review"))
        {
            if(!EditorApplication.isPlaying){EditorApplication.isPlaying=true;return;}
            File.Delete("work/run-portfolio-review");Directory.CreateDirectory("work/portfolio-review");
            running=true;step=0;next=EditorApplication.timeSinceStartup+2;
        }
        if(!running||EditorApplication.timeSinceStartup<next)return;
        try
        {
            var deal=new CashflowGame.Deal("Review rental",4000,220,"A rental investment with monthly income.");
            switch(step)
            {
                case 0:Screen.orientation=ScreenOrientation.LandscapeLeft;Call("StartGame");Call("ShowPortfolio",0);break;
                case 1:Capture("empty");Set("cash",20000);Call("OfferDeal",deal,false);Click("Buy Deal");break;
                case 2:
                    Require(Assets.Count==1&&(int)Get("cash")==16000&&(int)Get("passive")==220,"Cash purchase accounting");
                    Call("OfferDeal",deal,false);var buy=GameObject.Find("Buy Deal").GetComponent<Button>();buy.onClick.Invoke();buy.onClick.Invoke();
                    Require(Assets.Count==2&&Assets[0].id!=Assets[1].id&&(int)Get("cash")==12000,"Repeated purchase identity or duplicate click");
                    Call("OfferDeal",deal,false);Click("Pass Deal");Require(Assets.Count==2,"Pass created asset");
                    Set("cash",400);Call("OfferDeal",deal,false);Click("Buy Deal");Click("Confirm Loan");break;
                case 3:
                    Require(Assets.Count==3&&Assets[2].outstandingBankLoan==4000&&(int)Get("bankLoan")==4000&&(int)Get("cash")==400,"Borrowed purchase accounting");
                    Call("OfferDeal",deal,false);Click("Buy Deal");Click("Pass Loan");Require(Assets.Count==3,"Loan cancellation created asset");
                    Call("OfferDeal",deal,false);Click("Buy Deal");Click("Confirm Loan");
                    Require(Assets.Count==4&&(int)Get("passive")==880,"Portfolio income sum");
                    Set("cash",10000);Call("ShowStatement");Click("Repay Loan");Click("Confirm Repayment");
                    Require(Assets[2].outstandingBankLoan==3000&&Assets[3].outstandingBankLoan==4000&&(int)Get("bankLoan")==7000,"Repayment allocation");
                    Click("Open Portfolio");break;
                case 4:Capture("portfolio");break;
                case 5:Click("Portfolio Next");break;
                case 6:Capture("second-page");break;
                case 7:Click("Portfolio Asset "+Assets[3].id);break;
                case 8:Capture("details");break;
                case 9:Click("Back To Portfolio");break;
                case 10:
                    Require(!GameObject.Find("Portfolio Next").GetComponent<Button>().interactable,"Detail lost current page");
                    Click("Close Portfolio");break;
                case 11:Capture("finances");break;
                case 12:Call("CloseModal");
                    Require(!UnityEngine.Object.FindObjectOfType<BoardWorld>().InputBlocked,"Portfolio left board blocked");
                    Call("StartGame");Require(Assets.Count==0&&(int)Get("passive")==0&&(int)Get("bankLoan")==0,"New game retained portfolio");
                    File.WriteAllText("work/portfolio-review/results.txt","PASS: cash and financed purchases; repeated investments; duplicate click guard; buy/pass and loan cancellation; income sum; debt repayment allocation; empty state, pagination, details and finance navigation; text bounds; new-game reset.\n");running=false;return;
            }
            step++;next=EditorApplication.timeSinceStartup+1.5;
        }
        catch(Exception e){running=false;File.WriteAllText("work/portfolio-review/results.txt","FAIL: "+e);Debug.LogException(e);}
    }
}
