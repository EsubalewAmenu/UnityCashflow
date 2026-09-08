using UnityEngine;
using UnityEngine.SceneManagement;

public partial class CashflowGame
{
    void AddRandomStart()
    {
        var random=OpeningButton(uiRoot,"RANDOM START",StartRandomGame);random.name="Random Start";
        Place(random.GetComponent<RectTransform>(),465,-230,260,44);SetButtonText(random,C("#443050"),18);
    }
    void ChooseRandomSelections()
    {
        int index=Random.Range(0,professions.Length);
        profession=professions[index];selectedDream=Random.Range(0,dreams.Length);
        PlayerPrefs.SetInt("CashflowProfession",index);PlayerPrefs.SetInt("CashflowDream",selectedDream);PlayerPrefs.Save();
    }
    void StartRandomGame(){ConfirmNewGame(()=>{ChooseRandomSelections();SceneManager.LoadScene("GameBoard");});}
    void ShowExitGame()
    {
        if(rolling||modal!=null)return;
        var card=CardShell("GAME MENU","Return to the main menu?",C("#654174"),new Vector2(800,440));
        var copy=Label(card.transform,"Your progress is saved automatically.\nChoose Resume Game on the main menu to continue.",22);
        copy.color=C("#51445b");Place(copy.rectTransform,0,-10,720,120);
        var stay=Button(card.transform,"KEEP PLAYING",C("#21756a"),CloseModal);stay.name="Stay In Game";Place(stay.GetComponent<RectTransform>(),-185,-155,310,54);SetButtonText(stay,Color.white,18);
        var leave=Button(card.transform,"RETURN TO MENU",C("#e8dcc5"),()=>{if(SaveCheckpoint()){saveActive=false;SceneManager.LoadScene("ProfessionSelection");}});leave.name="Exit To Menu";Place(leave.GetComponent<RectTransform>(),185,-155,310,54);SetButtonText(leave,C("#51445b"),18);
    }
}
