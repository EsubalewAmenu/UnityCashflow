using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public partial class CashflowGame
{
    bool gameOver;
    bool CheckGameOver()
    {
        if(gameOver)return true;
        if(gameWon||profession==null||cash>=0||Income()-Expenses()>=0)return false;
        gameOver=true;fastTrackIntro=false;pendingDecision=new PendingDecision();
        Log("Game over: cash and net monthly cashflow are both negative.");
        if(world!=null)ShowGameOver();
        return true;
    }
    void ShowGameOver()
    {
        var card=CardShell("GAME OVER","Your finances need a fresh start",C("#a84036"),new Vector2(800,500));
        FinanceRow(card.transform,"Cash balance",Money(cash),85);
        FinanceRow(card.transform,"Net monthly cashflow",Money(Income()-Expenses())+" / month",30);
        var copy=Label(card.transform,"Cash and monthly cashflow are both below zero.\nThis game has ended. Try again with a new plan for\ncash reserves, recurring expenses and loan interest.",21);
        copy.color=C("#51445b");Place(copy.rectTransform,0,-80,720,110);
        var again=Button(card.transform,"TRY AGAIN",C("#21756a"),StartGame);again.name="Game Over Try Again";Place(again.GetComponent<RectTransform>(),-180,-195,300,54);SetButtonText(again,Color.white,18);
        var menu=Button(card.transform,"MAIN MENU",C("#e8dcc5"),()=>{if(SaveCheckpoint()){saveActive=false;SceneManager.LoadScene("ProfessionSelection");}});menu.name="Game Over Main Menu";Place(menu.GetComponent<RectTransform>(),180,-195,300,54);SetButtonText(menu,C("#51445b"),18);
        if(rollButton!=null)rollButton.interactable=false;
        CashflowAds.Ensure().SetBannerPlacement(true);
    }
}
