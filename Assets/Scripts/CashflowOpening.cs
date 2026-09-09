using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public partial class CashflowGame
{
    GameObject openingBackdrop;
    Sprite ProfessionPortrait(int index){return index<5?ArtSprite("profession-"+index,"Onboarding/professions",new Rect(Mathf.Clamp(index,0,4)/5f,0,.2f,.55f)):ArtSprite("profession-"+index,"Onboarding/expansion",new Rect((index-5)/5f,0,.2f,.5f));}
    void SetOpeningBackdrop(bool visible)
    {
        if(openingBackdrop==null&&visible)
        {
            openingBackdrop=new GameObject("Opening city backdrop",typeof(RectTransform),typeof(RawImage),typeof(OpeningBackdrop));openingBackdrop.transform.SetParent(canvas.transform,false);openingBackdrop.transform.SetAsFirstSibling();
            var image=openingBackdrop.GetComponent<RawImage>();image.texture=Resources.Load<Texture2D>("GameBoard/city");image.raycastTarget=false;
        }
        if(openingBackdrop!=null)openingBackdrop.SetActive(visible);
    }
    void OpeningShell(bool dreamsPage)
    {
        ClearUI();SetOpeningBackdrop(true);uiRoot.Find("Background").GetComponent<Image>().color=Color.clear;
        var logo=Label(uiRoot,"CASHFLOW KINGDOM",34);logo.fontStyle=FontStyle.Bold;logo.color=C("#ffe3a0");Place(logo.rectTransform,30,302,550,58);
        var crown=new GameObject("Kingdom Crown",typeof(RectTransform),typeof(RawImage));crown.transform.SetParent(uiRoot,false);crown.GetComponent<RawImage>().texture=Resources.Load<Texture2D>("Branding/kingdom-icon");crown.GetComponent<RawImage>().raycastTarget=false;Place(crown.GetComponent<RectTransform>(),-274,302,48,48);
        var outline=logo.gameObject.AddComponent<Outline>();outline.effectColor=C("#443050");outline.effectDistance=new Vector2(3,-3);
        var tag=Label(uiRoot,"THE GAME OF FINANCIAL FREEDOM",12);tag.color=C("#443050");Place(tag.rectTransform,0,261,520,22);
        var title=Label(uiRoot,dreamsPage?"Choose your dream":"Choose your profession",32);title.fontStyle=FontStyle.Bold;Place(title.rectTransform,0,207,750,46);
        var shadow=title.gameObject.AddComponent<Shadow>();shadow.effectColor=C("#443050");shadow.effectDistance=new Vector2(1,-2);
        var previous=OpeningButton(uiRoot,"‹",()=>ChangeOpeningSelection(dreamsPage,-1));previous.name="Previous Selection";Place(previous.GetComponent<RectTransform>(),-589,-20,54,74);SetButtonText(previous,C("#443050"),40);
        var next=OpeningButton(uiRoot,"›",()=>ChangeOpeningSelection(dreamsPage,1));next.name="Next Selection";Place(next.GetComponent<RectTransform>(),589,-20,54,74);SetButtonText(next,C("#443050"),40);
        AddRandomStart();if(!dreamsPage)AddResumeButton();
        AddAdPrivacyButton(uiRoot,455,302,265,36);
        CashflowAds.Ensure().SetBannerPlacement(true);
        if(dreamsPage){var back=OpeningButton(uiRoot,"‹  PROFESSION",ShowSetup);back.name="Back to Profession";Place(back.GetComponent<RectTransform>(),-495,301,210,44);SetButtonText(back,C("#443050"),14);}
    }
    Button OpeningButton(Transform parent,string title,Action action)
    {
        var button=Button(parent,title,C("#fff2d5"),action);var image=button.GetComponent<Image>();image.sprite=RoundedSprite();image.type=Image.Type.Sliced;
        var outline=button.gameObject.AddComponent<Outline>();outline.effectColor=C("#d8bb80");outline.effectDistance=new Vector2(2,2);return button;
    }
    void ChangeOpeningSelection(bool dreamsPage,int delta)
    {
        if(dreamsPage){selectedDream=(selectedDream+delta+dreams.Length)%dreams.Length;PlayerPrefs.SetInt("CashflowDream",selectedDream);ShowDreams();}
        else{int index=Array.IndexOf(professions,profession);profession=professions[(index+delta+professions.Length)%professions.Length];ShowSetup();}
    }
    void ShowSetup()
    {
        if(profession==null)LoadProfessionSelection();OpeningShell(false);
        int selected=Array.IndexOf(professions,profession);
        for(int slot=-2;slot<=2;slot++)
        {
            int index=(selected+slot+professions.Length)%professions.Length;var item=professions[index];
            OpeningCard(slot,item.name,ProfessionPortrait(index),"STARTING SALARY",Money(item.salary)+" / mo",()=>{profession=item;ShowSetup();},false);
        }
        int expenses=profession.taxes+profession.mortgagePay+profession.schoolPay+profession.carPay+profession.cardPay+profession.retailPay+profession.other;
        OpeningSummary("STARTING CASH",Money(profession.savings),"MONTHLY EXPENSES",Money(expenses),"MONTHLY CASHFLOW",Money(profession.salary-expenses));
        var next=OpeningButton(uiRoot,"NEXT  →",GoToDreamSelection);next.name="Choose Dream Next";next.GetComponent<Image>().color=C("#579342");Place(next.GetComponent<RectTransform>(),465,-290,260,64);SetButtonText(next,Color.white,25);
    }
    void ShowDreams()
    {
        if(profession==null)LoadProfessionSelection();selectedDream=Mathf.Clamp(PlayerPrefs.GetInt("CashflowDream",selectedDream),0,dreams.Length-1);OpeningShell(true);
        for(int slot=-2;slot<=2;slot++)
        {
            int index=(selectedDream+slot+dreams.Length)%dreams.Length;
            var art=index<8?ArtSprite("dream-"+index,"Onboarding/dreams",new Rect(index%4/4f,index/4/2f,.25f,.5f)):ArtSprite("dream-"+index,"Onboarding/expansion",new Rect((index-8)/5f,.5f,.2f,.5f));
            OpeningCard(slot,dreams[index],art,"YOUR DREAM GOAL",Money(dreamGoals[index]),()=>{selectedDream=index;PlayerPrefs.SetInt("CashflowDream",index);ShowDreams();},true);
        }
        OpeningSummary("YOUR PROFESSION",profession.name,"YOUR DREAM",(selectedDream+1)+" / "+dreams.Length,"TARGET",Money(dreamGoals[selectedDream]));
        var start=OpeningButton(uiRoot,"START GAME  →",()=>ConfirmNewGame(()=>{PlayerPrefs.SetInt("CashflowDream",selectedDream);PlayerPrefs.Save();SceneManager.LoadScene("GameBoard");}));start.name="Start Selected Game";start.GetComponent<Image>().color=C("#579342");Place(start.GetComponent<RectTransform>(),465,-290,260,64);SetButtonText(start,Color.white,23);
    }
    void OpeningCard(int slot,string title,Sprite art,string caption,string value,Action select,bool dreamsPage)
    {
        bool active=slot==0;float width=active?236:187,height=active?362:296;
        var card=OpeningButton(uiRoot,"",select);card.name=active?"Selected Opening Card":"Opening Card "+slot;
        Place(card.GetComponent<RectTransform>(),slot*218,-17,width,height);
        card.GetComponent<Outline>().effectColor=C(active?"#f8d477":"#c7b796");card.GetComponent<Outline>().effectDistance=Vector2.one*(active?4:2);
        var shadow=card.gameObject.AddComponent<Shadow>();shadow.effectColor=new Color(.17f,.12f,.22f,.35f);shadow.effectDistance=new Vector2(0,-8);
        var picture=new GameObject("Card illustration",typeof(RectTransform),typeof(Image));picture.transform.SetParent(card.transform,false);picture.GetComponent<Image>().sprite=art;picture.GetComponent<Image>().preserveAspect=false;picture.GetComponent<Image>().raycastTarget=false;
        Place(picture.GetComponent<RectTransform>(),0,active?34:24,width-18,active?236:196);
        var name=Label(card.transform,title,active?21:17);name.color=C("#443050");name.fontStyle=FontStyle.Bold;Place(name.rectTransform,0,active?-102:-94,width-20,48);
        if(active)
        {
            var metric=Label(card.transform,caption+"\n"+value,15);metric.color=C("#397446");Place(metric.rectTransform,0,-149,width-20,44);
            var check=Label(card.transform,"✓",24);check.color=C("#397446");Place(check.rectTransform,width/2-20,height/2-19,32,32);
        }
        card.gameObject.AddComponent<OpeningSwipe>().Move=delta=>ChangeOpeningSelection(dreamsPage,delta);
        card.gameObject.AddComponent<OpeningCardEntrance>().Begin(slot);
    }
    void OpeningSummary(string a,string av,string b,string bv,string c,string cv)
    {
        var panel=Panel(uiRoot,Vector2.one*.5f,Vector2.one*.5f,new Vector2(875,77),C("#fff2d5"));Place(panel.GetComponent<RectTransform>(),-155,-290,875,77);panel.GetComponent<Image>().sprite=RoundedSprite();panel.GetComponent<Image>().type=Image.Type.Sliced;
        string[] names={a,b,c},values={av,bv,cv};
        for(int i=0;i<3;i++){var label=Label(panel.transform,names[i],11);label.color=C("#8b7458");Place(label.rectTransform,(i-1)*285,16,275,20);var val=Label(panel.transform,values[i],20);val.color=C("#397446");Place(val.rectTransform,(i-1)*285,-11,275,30);}
    }
}
