using System;
using UnityEngine;
using UnityEngine.UI;

public partial class CashflowGame
{
    void UpdateBannerPlacement(bool openingModal)
    {
        bool opening=openingBackdrop!=null&&openingBackdrop.activeSelf;
        bool gameplay=!opening&&world!=null&&!gameOver&&!gameWon;
        CashflowAds.Ensure().SetBannerPlacement(gameplay||(!openingModal&&(opening||gameOver||gameWon)),gameplay);
    }
    void CompleteMovement(int start,int steps)
    {
        int count=0,previousCash=cash;
        for(int step=1;step<=steps;step++)
            if(fastTrack?fastTiles[(start+step)%24]=="CASHFLOW DAY":ratTiles[(start+step)%24]=="PAYDAY")count++;
        CollectMovementPaydays(start,steps);
        if(CheckGameOver())return;
        string destination=fastTrack?fastTiles[fastPosition]:ratTiles[position];
        if(localMatch==null&&count>0&&IsPaydayDestination(destination))ShowPaydayBreak(destination,cash-previousCash,count);
        else Land(destination);
    }
    bool IsPaydayDestination(string destination)=>destination==(fastTrack?"CASHFLOW DAY":"PAYDAY");
    void ShowPaydayBreak(string destination,int amount,int count)
    {
        // Older saves may have paused after passing Payday. Payment was already
        // settled; resolve their destination without replaying a break or ad.
        if(!IsPaydayDestination(destination)){pendingDecision=new PendingDecision();Land(destination);return;}
        string heading=fastTrack?"IT'S CASHFLOW DAY":"IT'S SALARY DAY";
        string details=(count>1?count+" Paydays settled\n":"")+"Income minus expenses: "+SignedMoney(amount)+
            "\nCash now: "+Money(cash)+"\n\nYour payment is already settled. Take a short break.\nAn ad may play after OK; if unavailable, play continues.";
        ShowAdBreak(heading,details,new PendingDecision{kind="payday-break",title=destination,amount=amount,turns=count},()=>{
            CloseModal();pendingDecision=new PendingDecision{kind="land-after-break",title=destination};
        },()=>{pendingDecision=new PendingDecision();if(!CheckGameOver())Land(destination);Refresh();SaveCheckpoint();});
    }
    void ShowDownsizeBreak()
    {
        if(localMatch!=null){Refresh();SaveCheckpoint();return;}
        ShowAdBreak("TAKE A SHORT BREAK","Your Downsized payment is settled.\nYou will skip "+skipTurns+" turns.\n\nAn ad may play after OK; if unavailable, play continues.",
            new PendingDecision{kind="downsize-break"},CloseModal,()=>{Refresh();SaveCheckpoint();});
    }
    void ShowAdBreak(string title,string details,PendingDecision decision,Action prepare,Action resume)
    {
        var card=CardShell("TURN BREAK",title,C("#21756a"),new Vector2(800,500));
        pendingDecision=decision;
        var copy=Label(card.transform,details,22);copy.color=C("#51445b");Place(copy.rectTransform,0,0,710,235);
        bool accepted=false,finished=false;
        var ok=Button(card.transform,"OK",C("#21756a"),()=>{
            if(accepted||CashflowAds.Ensure().FullScreen)return;accepted=true;
            prepare();SaveCheckpoint();
            // Keep input blocked while the native fullscreen ad owns the screen.
            if(world!=null)world.InputBlocked=true;if(rollButton!=null)rollButton.interactable=false;
            Action finish=()=>{if(finished||this==null)return;finished=true;if(world!=null)world.InputBlocked=false;resume();if(rollButton!=null)rollButton.interactable=modal==null&&!rolling&&!gameOver&&!gameWon;};
            if(!CashflowAds.Ensure().TryInterstitial(finish))finish();
        });
        ok.name="Ad Break OK";Place(ok.GetComponent<RectTransform>(),0,-195,280,54);SetButtonText(ok,Color.white,18);
    }
}
