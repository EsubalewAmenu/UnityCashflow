using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

// Financial controller and screen HUD for the 3D Cashflow board.
// It builds its playable UI at runtime; no manually wired scene is required.
public partial class CashflowGame : MonoBehaviour
{
    [Serializable] public class Profession { public string name; public int salary, savings, taxes, mortgagePay, schoolPay, carPay, cardPay, retailPay, other, childPay, mortgageBalance, schoolBalance, carBalance, cardBalance, retailBalance; }
    [Serializable] public class Deal
    {
        public string title, description;
        public int down, flow;
        public Deal(string t, int d, int f, string story = "") { title=t; down=d; flow=f; description=story; }
    }

    Profession[] professions = {
        new Profession { name="Engineer", mortgageBalance=75000, schoolBalance=12000, carBalance=4000, cardBalance=2000, retailBalance=1000, salary=4900, savings=400, taxes=1050, mortgagePay=400, schoolPay=60, carPay=80, cardPay=120, retailPay=50, other=1090, childPay=250 },
        new Profession { name="Teacher", mortgageBalance=90000, schoolBalance=12000, carBalance=4000, cardBalance=2000, retailBalance=1000, salary=3300, savings=400, taxes=630, mortgagePay=500, schoolPay=60, carPay=60, cardPay=90, retailPay=50, other=760, childPay=180 },
        new Profession { name="Medical Doctor", mortgageBalance=202000, schoolBalance=150000, carBalance=19000, cardBalance=9000, retailBalance=1000, salary=13200, savings=400, taxes=3420, mortgagePay=1900, schoolPay=750, carPay=380, cardPay=270, retailPay=50, other=2880, childPay=640 },
        new Profession { name="Janitor", mortgageBalance=20000, carBalance=4000, cardBalance=2000, retailBalance=1000, salary=1600, savings=560, taxes=280, mortgagePay=200, schoolPay=0, carPay=60, cardPay=60, retailPay=50, other=300, childPay=70 },
        new Profession { name="Lawyer", mortgageBalance=115000, schoolBalance=78000, carBalance=11000, cardBalance=6000, retailBalance=1000, salary=7500, savings=400, taxes=1830, mortgagePay=1100, schoolPay=390, carPay=220, cardPay=180, retailPay=50, other=1650, childPay=380 },        new Profession { name="Nurse", salary=4200, savings=600, taxes=850, mortgagePay=600, schoolPay=100, carPay=100, cardPay=80, retailPay=40, other=950, childPay=220, mortgageBalance=85000, schoolBalance=20000, carBalance=5000, cardBalance=3000, retailBalance=800 },
        new Profession { name="Chef", salary=3600, savings=700, taxes=650, mortgagePay=500, schoolPay=60, carPay=90, cardPay=60, retailPay=40, other=850, childPay=200, mortgageBalance=70000, schoolBalance=12000, carBalance=4500, cardBalance=2000, retailBalance=800 },
        new Profession { name="Electrician", salary=4500, savings=900, taxes=900, mortgagePay=650, schoolPay=40, carPay=150, cardPay=80, retailPay=50, other=1000, childPay=240, mortgageBalance=95000, schoolBalance=8000, carBalance=7500, cardBalance=3000, retailBalance=1000 },
        new Profession { name="Designer", salary=4000, savings=500, taxes=780, mortgagePay=550, schoolPay=90, carPay=80, cardPay=100, retailPay=40, other=950, childPay=210, mortgageBalance=80000, schoolBalance=18000, carBalance=4000, cardBalance=3500, retailBalance=800 },
        new Profession { name="Small Business Owner", salary=5200, savings=1200, taxes=1050, mortgagePay=700, schoolPay=0, carPay=170, cardPay=150, retailPay=50, other=1250, childPay=260, mortgageBalance=105000, schoolBalance=0, carBalance=8500, cardBalance=5000, retailBalance=1000 }
    };
    readonly string[] dreams = { "Stock Market for Kids", "Save the Coral Reefs", "Private Superyacht", "African Wildlife Sanctuary", "Heli-Skiing Chalet", "Private Tropical Island", "7 Wonders Expedition", "Community Health Clinic", "Animal Rescue", "Music Studio", "Mountain Retreat", "Clean Water Project", "Community Library" };
    readonly int[] dreamGoals = { 125000, 175000, 220000, 140000, 130000, 250000, 90000, 200000, 100000, 110000, 160000, 180000, 120000 };
    readonly string[] ratTiles = { "OPPORTUNITY", "DOODAD", "OPPORTUNITY", "CHARITY", "OPPORTUNITY", "PAYDAY", "OPPORTUNITY", "MARKET", "OPPORTUNITY", "DOODAD", "OPPORTUNITY", "BABY", "OPPORTUNITY", "PAYDAY", "OPPORTUNITY", "MARKET", "OPPORTUNITY", "DOODAD", "OPPORTUNITY", "DOWNSIZED", "OPPORTUNITY", "PAYDAY", "OPPORTUNITY", "MARKET" };
    readonly string[] fastTiles = { "CASHFLOW DAY", "BIO-TECH +25K", "DREAM", "PIZZA +18K", "TAX AUDIT", "DREAM", "CASHFLOW DAY", "GOLF +35K", "DREAM", "CLOUD +40K", "LAWSUIT", "DREAM", "CASHFLOW DAY", "CAR WASH +15K", "DREAM", "EV +30K", "DIVORCE", "DREAM", "CASHFLOW DAY", "CARGO +28K", "DREAM", "HOTEL +22K", "CORRECTION", "DREAM" };
    readonly Deal[] smallDeals = {
        new Deal("Growth Share", 10, 0, "Buy one growth-company share for $10. It pays no dividends or Payday income. Keep it for a future Market sale; the offer may be higher or lower than your entry price."),
        new Deal("Dividend Basket", 100, 2, "Buy a small income interest in a dividend-paying investment basket. It adds $2 at each Payday in this game; selling ends that income."),
        new Deal("Tool Rental Share", 250, 15, "Buy an income interest in a tool-rental service. It pays a fixed amount at Payday; equipment purchases and repair bills are not charged separately in this game."),
        new Deal("Digital Templates", 400, 20, "Buy an income interest in a catalogue of digital templates. Sales provide a fixed Payday payment; you do not create designs or pay separate hosting costs."),
        new Deal("Parking Space", 750, 40, "Buy an income interest in a rented parking space. It provides one fixed Payday payment and is sold as one investment when a Market offer arrives."),
        new Deal("Laundry Share", 1200, 75, "Buy an income interest in a neighbourhood laundry. The fixed Payday amount has no separate utility or machine-maintenance bills in this game."),
        new Deal("Storage Units", 2000, 130, "Buy an income interest in a small storage business. Rental receipts are combined into one fixed Payday payment; a sale transfers the whole investment interest."),
        new Deal("Coffee Cart", 2500, 190, "Buy an income interest in a coffee-cart business. It pays a fixed amount at Payday without separate staffing or supply bills in this game."),
        new Deal("2/1 Condo", 4000, 220, "Buy an income-producing interest in a two-bedroom, one-bath condo. Rental income adds to your Payday receipts; borrowing to enter the deal creates a separate monthly interest bill."),
        new Deal("3/2 House", 5000, 250, "Buy a rental-income interest in a three-bedroom, two-bath house. The entry payment ties up cash until you sell; weigh its monthly income against financing costs and the cash you want to keep."),
        new Deal("Vending Route", 4000, 350, "Buy an income interest in a vending-machine route. Sales provide a fixed monthly payment in this game; you do not manage stock or pay separate restocking bills."),
        new Deal("Micro SaaS", 3000, 400, "Buy an income interest in a subscription software business. Customer subscriptions provide a fixed monthly payment in this game; there are no separate development or hosting bills.") };
    readonly Deal[] bigDeals = {
        new Deal("Warehouse Rentals", 15000, 1000, "Buy an income interest in warehouse rentals. Several rental receipts become one fixed Payday payment; the investment is sold together rather than unit by unit."),
        new Deal("Car Wash", 20000, 1500, "Buy an income interest in a car wash. Its Payday payment has no separate staffing or equipment bills; bank borrowing still adds a monthly interest expense."),
        new Deal("Neighbourhood Shops", 25000, 1800, "Buy an income interest in a group of shops. Rents are combined into one fixed Payday payment. Your entry cash remains invested until a Market sale."),
        new Deal("Distribution Business", 30000, 2400, "Buy an income interest in a distribution business. It pays a fixed amount at Payday; its large entry payment may leave less cash available for other events."),
        new Deal("4-Plex", 10000, 800, "Buy an income interest in a building with four rental homes. Their rental income is combined into one payment; the game treats this as one investment that is sold together."),
        new Deal("Pizza Franchise", 35000, 3000, "Buy an income interest in a pizza franchise. The monthly payment has no separate staffing or ingredient bills in this game. Its large entry payment can require substantial bank borrowing.") };

    Profession profession; string dream; int selectedDream; int cash, passive, children, position, fastPosition; bool fastTrack, charity; int skipTurns; int bankLoan;
    BoardWorld world; Button rollButton; bool rolling; GameObject modal;
    RectTransform uiRoot; Text bottomCash, bottomNetCashflow, freedomTitle, freedomText, diceResult; Image freedomFill;
    Canvas canvas; Text header, statement, log, cashReadout, flowReadout, modeReadout, hoverCopy; RectTransform board; GameObject hoverPanel; Sprite circleSprite, roundedSprite; Font uiFont; readonly Dictionary<string, Sprite> artSprites = new Dictionary<string, Sprite>(); readonly List<Button> tileButtons = new List<Button>(); readonly List<GameObject> boardDecorations = new List<GameObject>();

    void Awake() { Application.runInBackground=true; CashflowAds.Ensure(); BuildCanvas(); }
    public void OpenScene(string sceneName)
    {
        if (sceneName == "ProfessionSelection") ShowSetup();
        else if (sceneName == "DreamSelection") ShowDreams();
        else if (sceneName == "GameBoard") { if(resumeRequested){resumeRequested=false;ResumeGame();}else StartGame(); }
        else ShowSetup();
    }
    void GoToDreamSelection()
    {
        PlayerPrefs.SetInt("CashflowProfession", Array.IndexOf(professions, profession));
        PlayerPrefs.Save();
        SceneManager.LoadScene("DreamSelection");
    }
    void LoadProfessionSelection()
    {
        int index = Mathf.Clamp(PlayerPrefs.GetInt("CashflowProfession", 0), 0, professions.Length - 1);
        profession = professions[index];
    }
    Font Font()
    {
        if (uiFont == null) uiFont = Resources.Load<Font>("Fonts/ModernBold");
        return uiFont != null ? uiFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }
    Color C(string hex) { ColorUtility.TryParseHtmlString(hex, out var c); return c; }
    Sprite CircleSprite()
    {
        if (circleSprite != null) return circleSprite;
        const int size = 128; var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var pixels = new Color[size * size]; var center = (size - 1) * .5f; var radius = center - 1;
        for (int y=0; y<size; y++) for (int x=0; x<size; x++) { float distance = Vector2.Distance(new Vector2(x,y),new Vector2(center,center)); pixels[y*size+x] = distance <= radius ? Color.white : Color.clear; }
        texture.SetPixels(pixels); texture.Apply(); circleSprite = Sprite.Create(texture,new Rect(0,0,size,size),new Vector2(.5f,.5f)); return circleSprite;
    }
    Sprite RoundedSprite()
    {
        if (roundedSprite != null) return roundedSprite;
        const int width=128, height=72, radius=15; var texture=new Texture2D(width,height,TextureFormat.RGBA32,false); var pixels=new Color[width*height];
        for(int y=0;y<height;y++) for(int x=0;x<width;x++)
        {
            float dx=Mathf.Max(radius-x-1,0,x-(width-radius)); float dy=Mathf.Max(radius-y-1,0,y-(height-radius));
            pixels[y*width+x]=new Color(1,1,1,Mathf.Clamp01(radius-Mathf.Sqrt(dx*dx+dy*dy)+.5f));
        }
        texture.SetPixels(pixels); texture.Apply(); roundedSprite=Sprite.Create(texture,new Rect(0,0,width,height),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect,new Vector4(16,16,16,16)); return roundedSprite;
    }
    Sprite ArtSprite(string key, string resource, Rect normalizedTopLeft)
    {
        if (artSprites.TryGetValue(key, out var cached)) return cached;
        var texture = Resources.Load<Texture2D>(resource);
        if (texture == null) return null;
        var rect = new Rect(normalizedTopLeft.x * texture.width, (1f-normalizedTopLeft.y-normalizedTopLeft.height) * texture.height, normalizedTopLeft.width * texture.width, normalizedTopLeft.height * texture.height);
        var sprite = Sprite.Create(texture,rect,new Vector2(.5f,.5f),100);
        artSprites[key] = sprite;
        return sprite;
    }
    Text Label(Transform parent, string value, int size, TextAnchor align = TextAnchor.MiddleCenter) { var go=new GameObject("Text",typeof(RectTransform),typeof(Text)); go.transform.SetParent(parent,false); var t=go.GetComponent<Text>(); t.font=Font(); t.text=value; t.fontSize=size; t.alignment=align; t.color=Color.white; t.horizontalOverflow=HorizontalWrapMode.Wrap; t.verticalOverflow=VerticalWrapMode.Overflow; t.raycastTarget=false; return t; }
    Button Button(Transform parent,string title,Color color,Action action) { var go=new GameObject(title,typeof(RectTransform),typeof(Image),typeof(Button)); go.transform.SetParent(parent,false); go.GetComponent<Image>().color=color; var b=go.GetComponent<Button>(); b.onClick.AddListener(()=>{action();SaveCheckpoint();}); var tx=Label(go.transform,title,16); Stretch(tx.rectTransform); return b; }
    void Stretch(RectTransform r) { r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=new Vector2(5,4);r.offsetMax=new Vector2(-5,-4); }
    void BuildCanvas() { var root=new GameObject("Cashflow Unity",typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster)); canvas=root.GetComponent<Canvas>(); canvas.renderMode=RenderMode.ScreenSpaceOverlay; var scaler=root.GetComponent<CanvasScaler>(); scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution=new Vector2(1280,720); scaler.matchWidthOrHeight=.5f; uiRoot=new GameObject("Safe Area",typeof(RectTransform),typeof(SafeBoardLayout)).GetComponent<RectTransform>(); uiRoot.SetParent(root.transform,false); var bg=new GameObject("Background",typeof(Image)); bg.transform.SetParent(uiRoot,false); bg.GetComponent<Image>().color=C("#0b1220"); Stretch(bg.GetComponent<RectTransform>()); }
    void ClearUI() { CashflowAds.Ensure().SetBannerPlacement(false); foreach(Transform child in uiRoot) if(child.name!="Background"){child.gameObject.SetActive(false);Destroy(child.gameObject);} }
    void SetButtonText(Button button, Color color, int size) { var text=button.GetComponentInChildren<Text>(); if(text!=null){text.color=color;text.fontSize=size;text.fontStyle=FontStyle.Bold;} }
    void StartGame(){ if(localMatch!=null&&!localInitializing){StartLocalMatch();return;} if(profession==null)LoadProfessionSelection(); selectedDream=Mathf.Clamp(PlayerPrefs.GetInt("CashflowDream",0),0,dreams.Length-1); dream=dreams[selectedDream]; cash=profession.savings; portfolio.Clear(); passive=0;children=0;position=0;fastPosition=0;fastTrack=false;charity=false;skipTurns=0;bankLoan=0;gameWon=false;gameOver=false;fastTrackIntro=false;ResetPersonalDebts();ResetHistory();ResetDealDecks();pendingDecision=new PendingDecision();BuildGame();saveActive=true;lastSavedPayload=null;SaveCheckpoint(); }
    void BuildGame()
    {
        if(world!=null){world.gameObject.SetActive(false);Destroy(world.gameObject);}
        rolling=false;modal=null;
        ClearUI();
        SetOpeningBackdrop(false);
        CashflowAds.Ensure().SetBannerPlacement(true,true);
        uiRoot.Find("Background").GetComponent<Image>().color=Color.clear;
        var top=Panel(uiRoot,new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(360,44),C("#49375f"));Place(top.GetComponent<RectTransform>(),-433,325,360,44);top.GetComponent<Image>().sprite=RoundedSprite();top.GetComponent<Image>().type=Image.Type.Sliced;
        header=Label(top.transform,"",16);Stretch(header.rectTransform);
        var menu=Button(uiRoot,"MENU",C("#fff0cb"),ShowExitGame);menu.name="Board Menu";Place(menu.GetComponent<RectTransform>(),535,325,145,44);SetButtonText(menu,C("#594125"),14);
        var statementButton=Button(uiRoot,"$  FINANCES",C("#fff0cb"),ShowStatement);statementButton.gameObject.name="Finances Button";Place(statementButton.GetComponent<RectTransform>(),-160,325,160,44);statementButton.GetComponent<Image>().sprite=RoundedSprite();SetButtonText(statementButton,C("#594125"),14);
        var financeTrim=statementButton.gameObject.AddComponent<Outline>();financeTrim.effectColor=C("#d6ae5e");financeTrim.effectDistance=new Vector2(2,2);
        // The board occupies the complete game area below the persistent header.
        board=new GameObject("Game Board",typeof(RectTransform),typeof(Image)).GetComponent<RectTransform>(); board.SetParent(uiRoot,false); board.GetComponent<Image>().color=new Color(1,1,1,0); board.GetComponent<Image>().raycastTarget=false; Place(board,0,8,1280,540);
        world=new GameObject("Cashflow 3D World").AddComponent<BoardWorld>();
        world.Initialize(board);

        DrawBoard();
        hoverPanel=Panel(uiRoot,new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(490,42),C("#182b4b")); Place(hoverPanel.GetComponent<RectTransform>(),0,-265,490,42); hoverPanel.GetComponent<Image>().raycastTarget=false;
        hoverCopy=Label(hoverPanel.transform,"Hover over a board tile to view its effect",13); hoverCopy.color=C("#d4ddea"); hoverCopy.raycastTarget=false; Stretch(hoverCopy.rectTransform); hoverPanel.SetActive(false);
        CreateGameHUD();
        log=Label(uiRoot,"",14); log.color=C("#ffffff"); log.fontStyle=FontStyle.Bold; Place(log.rectTransform,0,-265,700,18);
        Refresh();
    }
    void CreateGameHUD()
    {
        // Fixed HUD: it remains readable while the board world stays unobstructed.
        var bottom=Panel(uiRoot,new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(1280,86),C("#2d2450")); Place(bottom.GetComponent<RectTransform>(),0,-317,1280,86);
        var portrait=Panel(bottom.transform,new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(60,60),C("#f6b34d")); Place(portrait.GetComponent<RectTransform>(),-560,0,60,60); portrait.GetComponent<Image>().sprite=CircleSprite();
        var portraitArt=new GameObject("Player Portrait Art",typeof(RectTransform),typeof(Image)); portraitArt.transform.SetParent(portrait.transform,false); var portraitImage=portraitArt.GetComponent<Image>(); portraitImage.sprite=ProfessionPortrait(System.Array.IndexOf(professions,profession)); portraitImage.preserveAspect=true; portraitImage.raycastTarget=false; Stretch(portraitArt.GetComponent<RectTransform>());
        var cashLabel=Label(bottom.transform,"CASH BALANCE",11,TextAnchor.MiddleLeft); cashLabel.color=C("#eadfff"); Place(cashLabel.rectTransform,-440,16,150,20);
        var cashValue=Label(bottom.transform,Money(cash),25,TextAnchor.MiddleLeft); bottomCash=cashValue; cashValue.gameObject.name="HUD Cash Balance"; cashValue.color=C("#f8d45a"); cashValue.fontStyle=FontStyle.Bold; Place(cashValue.rectTransform,-440,-11,150,36);
        cashValue.resizeTextForBestFit=true;cashValue.resizeTextMinSize=16;cashValue.resizeTextMaxSize=25;
        var netLabel=Label(bottom.transform,"NET MONTHLY CASHFLOW",11,TextAnchor.MiddleLeft);netLabel.color=C("#eadfff");Place(netLabel.rectTransform,-260,16,190,20);
        bottomNetCashflow=Label(bottom.transform,"",25,TextAnchor.MiddleLeft);bottomNetCashflow.gameObject.name="HUD Net Monthly Cashflow";bottomNetCashflow.fontStyle=FontStyle.Bold;Place(bottomNetCashflow.rectTransform,-260,-11,190,36);
        bottomNetCashflow.resizeTextForBestFit=true;bottomNetCashflow.resizeTextMinSize=16;bottomNetCashflow.resizeTextMaxSize=25;
        var passiveLabel=Label(bottom.transform,"PASSIVE / EXPENSES",12);freedomTitle=passiveLabel;passiveLabel.color=C("#eadfff"); passiveLabel.fontStyle=FontStyle.Bold; Place(passiveLabel.rectTransform,-30,23,230,20);
        var progressBack=Panel(bottom.transform,new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(230,20),C("#16112a")); Place(progressBack.GetComponent<RectTransform>(),-30,-4,230,20);
        var progress=Panel(progressBack.transform,new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(145,13),C("#86c94f")); Place(progress.GetComponent<RectTransform>(),-112,0,145,13);
        freedomFill=progress.GetComponent<Image>(); freedomFill.sprite=RoundedSprite(); freedomFill.type=Image.Type.Filled; freedomFill.fillMethod=Image.FillMethod.Horizontal; Place(progress.GetComponent<RectTransform>(),0,0,220,13);
        var progressText=Label(bottom.transform,"Build your monthly freedom",11); freedomText=progressText; progressText.color=C("#ffffff"); Place(progressText.rectTransform,-30,-27,230,17);
        progressText.resizeTextForBestFit=true;progressText.resizeTextMinSize=9;progressText.resizeTextMaxSize=11;
        var roll=Button(bottom.transform,"ROLL DICE",C("#69b642"),Roll); rollButton=roll; roll.GetComponent<Image>().sprite=RoundedSprite(); Place(roll.GetComponent<RectTransform>(),485,0,235,60); SetButtonText(roll,Color.white,25); var outline=roll.gameObject.AddComponent<Outline>(); outline.effectColor=C("#f6f1ca"); outline.effectDistance=new Vector2(3,3);
        var diceArt=new GameObject("Dice Icon",typeof(RectTransform),typeof(Image)); diceArt.transform.SetParent(roll.transform,false);
        var diceImage=diceArt.GetComponent<Image>(); diceImage.sprite=RoundedSprite(); diceImage.type=Image.Type.Sliced; diceImage.color=Color.white; diceImage.raycastTarget=false;
        Place(diceArt.GetComponent<RectTransform>(),-78,0,38,38);
        foreach(var dot in new[]{new Vector2(-9,-9),new Vector2(9,-9),Vector2.zero,new Vector2(-9,9),new Vector2(9,9)})
        {
            var pip=new GameObject("Pip",typeof(RectTransform),typeof(Image)); pip.transform.SetParent(diceArt.transform,false);
            pip.GetComponent<Image>().sprite=CircleSprite(); pip.GetComponent<Image>().color=C("#39485d"); pip.GetComponent<Image>().raycastTarget=false;
            Place(pip.GetComponent<RectTransform>(),dot.x,dot.y,6,6);
        }
        var rollLabel=roll.GetComponentInChildren<Text>(); Place(rollLabel.rectTransform,22,0,165,48);
        diceResult=Label(bottom.transform,"READY TO ROLL",12); Place(diceResult.rectTransform,285,0,140,40);
        var settings=Button(bottom.transform,"SETTINGS",C("#544566"),ShowBoardSettings);
        settings.gameObject.name="Board Settings Button";
        Place(settings.GetComponent<RectTransform>(),160,0,100,52);
        settings.GetComponent<Image>().sprite=RoundedSprite();
        SetButtonText(settings,Color.white,12);
        CreateEventReceipt();

    }
    void ShowBoardSettings()
    {
        var panel=Modal(new Vector2(440,560));
        var title=Label(panel.transform,"BOARD SETTINGS",22);Place(title.rectTransform,0,235,400,36);
        var cameraTitle=Label(panel.transform,"CAMERA VIEW",13);Place(cameraTitle.rectTransform,0,190,340,24);
        var overview=Button(panel.transform,world.Follow?"OVERVIEW":"OVERVIEW  •",C("#544566"),()=>{});
        var follow=Button(panel.transform,world.Follow?"FOLLOW  •":"FOLLOW",C("#544566"),()=>{});
        overview.onClick.AddListener(()=>{world.SetOverview(true);overview.GetComponentInChildren<Text>().text="OVERVIEW  •";follow.GetComponentInChildren<Text>().text="FOLLOW";});
        follow.onClick.AddListener(()=>{world.SetOverview(false);overview.GetComponentInChildren<Text>().text="OVERVIEW";follow.GetComponentInChildren<Text>().text="FOLLOW  •";});
        Place(overview.GetComponent<RectTransform>(),-88,150,164,44);Place(follow.GetComponent<RectTransform>(),88,150,164,44);
        var sound=Button(panel.transform,world.Sound?"SOUND: ON":"SOUND: OFF",C("#544566"),()=>{});sound.onClick.AddListener(()=>{world.Sound=!world.Sound;sound.GetComponentInChildren<Text>().text=world.Sound?"SOUND: ON":"SOUND: OFF";});Place(sound.GetComponent<RectTransform>(),0,90,340,40);
        var motion=Button(panel.transform,world.ReducedMotion?"REDUCED MOTION: ON":"REDUCED MOTION: OFF",C("#544566"),()=>{});motion.onClick.AddListener(()=>{world.ReducedMotion=!world.ReducedMotion;motion.GetComponentInChildren<Text>().text=world.ReducedMotion?"REDUCED MOTION: ON":"REDUCED MOTION: OFF";});Place(motion.GetComponent<RectTransform>(),0,-52,340,40);
        var scenery=Button(panel.transform,"SHOW / HIDE CITY",C("#544566"),()=>world.ToggleScenery());Place(scenery.GetComponent<RectTransform>(),0,-104,340,40);
        AddAudioVolume(panel.transform);
        AddAdPrivacyButton(panel.transform,0,-195,340,30);
        var close=Button(panel.transform,"DONE",C("#69a747"),()=>{world.Audio.SavePreferences();CloseModal();});Place(close.GetComponent<RectTransform>(),0,-230,180,44);
    }
    GameObject Modal(Vector2 size)
    {
        CloseModal(); UpdateBannerPlacement(true); modal=Panel(uiRoot,Vector2.zero,Vector2.one,Vector2.zero,new Color(.06f,.04f,.12f,.7f));modal.name="Modal Blocker";
        if(eventReceipt!=null)eventReceipt.transform.SetAsLastSibling();
        var panel=Panel(modal.transform,new Vector2(.5f,.5f),new Vector2(.5f,.5f),size,C("#302540"));panel.GetComponent<Image>().sprite=RoundedSprite();panel.GetComponent<Image>().type=Image.Type.Sliced;
        if(world!=null)world.InputBlocked=true;if(rollButton!=null)rollButton.interactable=false;return panel;
    }
    void CloseModal(){pendingDecision=new PendingDecision();if(modal!=null){modal.SetActive(false);Destroy(modal);modal=null;}if(world!=null)world.InputBlocked=false;if(rollButton!=null)rollButton.interactable=!rolling&&!gameOver;UpdateBannerPlacement(false);}
    GameObject Panel(Transform p,Vector2 aMin,Vector2 aMax,Vector2 size,Color c){var go=new GameObject("Panel",typeof(RectTransform),typeof(Image));go.transform.SetParent(p,false);var r=go.GetComponent<RectTransform>();r.anchorMin=aMin;r.anchorMax=aMax;r.sizeDelta=size;go.GetComponent<Image>().color=c;return go;}
    void Place(RectTransform r,float x,float y,float w,float h){r.anchorMin=r.anchorMax=new Vector2(.5f,.5f);r.anchoredPosition=new Vector2(x,y);r.sizeDelta=new Vector2(w,h);}
    void DrawBoard()
    {
        foreach(var b in tileButtons) {b.gameObject.SetActive(false);Destroy(b.gameObject);} tileButtons.Clear();
        foreach(var decoration in boardDecorations) {decoration.SetActive(false);Destroy(decoration);} boardDecorations.Clear();


        BoardLayer("Board Frame",0,0,1180,520,C("#edd38c"));
        BoardLayer("Slate Board Surround",0,0,1168,508,C("#39485d"));
        BoardLayer("Gold Mat Trim",0,0,662,186,C("#e8bd55"));
        BoardLayer("Cashflow Center",0,0,652,176,C("#281440"));
        CreateTrack(fastTiles,138,70,true);
        CreateTrack(ratTiles,222,70,false);
        CenterPresentation();
        MoveToken();world.ApplyPrint(board);
    }
    GameObject BoardLayer(string name,float x,float y,float width,float height,Color color)
    {
        if(name=="Board Frame"||name=="Slate Board Surround")
        {
            var plate=new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(BoardPlateGraphic));plate.transform.SetParent(board,false);
            var graphic=plate.GetComponent<BoardPlateGraphic>();graphic.radius=name=="Board Frame"?265:261;graphic.color=color;graphic.raycastTarget=false;
            Place(graphic.rectTransform,x,y,1280,540);boardDecorations.Add(plate);return plate;
        }
        if(name=="Board Frame"||name=="Board Shadow"||name=="Slate Board Surround"||name=="Gold Mat Trim"||name=="Cashflow Center")width+=2*(BoardTrackGenerator.Straight-328);
        var layer=new GameObject(name,typeof(RectTransform),typeof(Image)); layer.transform.SetParent(board,false); layer.transform.SetAsLastSibling();
        var image=layer.GetComponent<Image>(); image.sprite=RoundedSprite(); image.type=Image.Type.Sliced; image.color=color; image.raycastTarget=false; Place(layer.GetComponent<RectTransform>(),x,y,width,height); boardDecorations.Add(layer); return layer;
    }
    void CenterPresentation()
    {
        var title=Label(board,"CASHFLOW",39);boardDecorations.Add(title.gameObject);title.fontStyle=FontStyle.Bold;title.color=C("#f5ce62");Place(title.rectTransform,0,52,330,48);
        var subtitle=Label(board,"THE ROAD TO FINANCIAL FREEDOM",10);boardDecorations.Add(subtitle.gameObject);subtitle.color=C("#f4e6ca");Place(subtitle.rectTransform,0,17,310,20);
        var hint=Label(board,"ROLL  •  INVEST  •  BUILD YOUR DREAM",9);boardDecorations.Add(hint.gameObject);hint.color=C("#c8bbdf");Place(hint.rectTransform,0,-69,340,20);
    }
    void CreateTrack(string[] tiles, float ry, float h, bool outer)
    {
        for(int i=0;i<tiles.Length;i++)
        {
            string title=tiles[i];
            var segment=BoardTrackGenerator.Segment(board,i,tiles.Length,ry,h,TileColor(title));
            var button=segment.gameObject.AddComponent<Button>(); button.targetGraphic=segment;
            button.onClick.AddListener(()=>ShowTileDetails(title,outer));
            var content=new GameObject("Space Label",typeof(RectTransform)); content.transform.SetParent(segment.transform,false);
            Vector2 p=BoardTrackGenerator.Point(i/(float)tiles.Length,ry);
            Place(content.GetComponent<RectTransform>(),p.x,p.y,70,60);
            var iconObject=new GameObject("Tile illustration",typeof(RectTransform),typeof(CanvasRenderer),typeof(BoardTileIcon));iconObject.transform.SetParent(content.transform,false);var icon=iconObject.GetComponent<BoardTileIcon>();icon.title=title;icon.raycastTarget=false;Place(icon.rectTransform,0,11,26,26);
            var number=Label(content.transform,(i+1).ToString(),7);number.color=C("#30273e");Place(number.rectTransform,29,23,16,12);
            var caption=Label(content.transform,ShortTitle(title),10); caption.fontStyle=FontStyle.Bold; caption.color=C("#30273e"); Place(caption.rectTransform,0,-15,72,24);
            AddTileHover(button,title,outer); tileButtons.Add(button);
        }
    }
    void AddTileHover(Button button, string title, bool outer)
    {
        var trigger=button.gameObject.AddComponent<EventTrigger>();
        if (trigger.triggers == null) trigger.triggers = new List<EventTrigger.Entry>();
        var enter=new EventTrigger.Entry { eventID=EventTriggerType.PointerEnter }; enter.callback.AddListener(_=>ShowHover((outer?"FAST TRACK: ":"RAT RACE: ")+title+" — "+TileDescription(title))); trigger.triggers.Add(enter);
        var exit=new EventTrigger.Entry { eventID=EventTriggerType.PointerExit }; exit.callback.AddListener(_=>HideHover()); trigger.triggers.Add(exit);
    }
    void ShowHover(string value) { if(hoverPanel!=null){hoverPanel.SetActive(true);hoverCopy.text=value;if(log!=null)log.gameObject.SetActive(false);} }
    void HideHover() { if(hoverPanel!=null)hoverPanel.SetActive(false);if(log!=null)log.gameObject.SetActive(true); }
    string TileDescription(string title)
    {
        if(title.Contains("PAYDAY")||title.Contains("CASHFLOW")) return "collect your monthly cashflow.";
        if(title.Contains("OPPORTUNITY")) return "draw an investment deal.";
        if(title.Contains("DOODAD")) return "pay an unexpected expense.";
        if(title.Contains("CHARITY")) return "choose to donate for two dice next roll, or pass and keep one die.";
        if(title.Contains("MARKET")) return "review a buyer offer for an investment you own. Sell it or keep its income.";
        if(title.Contains("BABY")) return "add a new monthly expense.";
        if(title.Contains("DOWNSIZED")) return "pay expenses and lose two turns.";
        if(title.Contains("DREAM")) return "buy your lifelong dream to win.";
        if(title.Contains("+")) return "buy a Fast Track venture for major cashflow.";
        return "resolve this Fast Track event.";
    }
    void ShowTileDetails(string title, bool outer) { if(gameOver||rolling||world.Dragged)return; ShowEventPanel((outer?"FAST TRACK":"RAT RACE")+" • "+title,TileDescription(title)+"\n\nClick Roll to land here and resolve the event.",outer?C("#075e50"):TileColor(title)); }
    void ShowEventPanel(string title, string details, Color color)
    {
        var shade=CardShell("BOARD EVENT",title,color,new Vector2(700,440));
        pendingDecision=new PendingDecision{kind="event",title=title,detail=details,color=color};
        var copy=Label(shade.transform,details,23);copy.color=C("#40394c");Place(copy.rectTransform,0,-5,610,170);
        var ok=Button(shade.transform,"CONTINUE",color,CloseModal);Place(ok.GetComponent<RectTransform>(),0,-165,260,54);SetButtonText(ok,Color.white,18);

    }
    string ShortTitle(string title)
    {
        if(title.Contains("OPPORTUNITY")) return "DEAL";
        if(title.Contains("CASHFLOW")) return "CASHFLOW";
        if(title.Contains("DREAM")) return "DREAM";
        if(title.Contains("BIO-TECH")) return "BIO TECH";
        if(title.Contains("PIZZA")) return "PIZZA";
        if(title.Contains("GOLF")) return "GOLF";
        if(title.Contains("CLOUD")) return "CLOUD";
        if(title.Contains("AUDIT")) return "AUDIT";
        if(title.Contains("CORRECTION")) return "CORRECT";
        return title.Length > 9 ? title.Substring(0,9) : title;
    }
    Color TileColor(string s){if(s.Contains("PAYDAY")||s.Contains("CASHFLOW"))return C("#80b66b");if(s.Contains("OPPORTUNITY")||s.Contains("+"))return C("#d89458");if(s.Contains("DOODAD")||s.Contains("AUDIT")||s.Contains("LAWSUIT")||s.Contains("DIVORCE"))return C("#c9756c");if(s.Contains("DREAM"))return C("#9c80b2");if(s.Contains("MARKET"))return C("#68a6b2");return C("#839ac0");}
    void MoveToken(){if(world!=null)world.PlacePawn(fastTrack?fastPosition:position,fastTrack);}
    void Roll(){if(localMatch!=null&&localMatch.rolled)return;if(gameWon||gameOver||rolling||modal!=null||CashflowAds.Ensure().FullScreen)return;if(fastTrackIntro){fastTrackIntro=false;ShowFastTrackIntro();return;}if(skipTurns>0){skipTurns--;if(localMatch!=null)localMatch.rolled=true;Log("Downsized: turn skipped ("+skipTurns+" left).");Refresh();ShowEventPanel("TURN SKIPPED",skipTurns>0?skipTurns+" skipped turn remains.":"You can roll again on your next turn.",C("#654174"));return;}StartCoroutine(RollTurn());}
    IEnumerator RollTurn()
    {
        SaveCheckpoint();rolling=true;rollButton.interactable=false;HideHover();diceResult.text="ROLLING...";
        yield return world.ThrowDice(charity?2:1);charity=false;
        int die=world.DiceTotal;if(localMatch!=null)localMatch.rolled=true;lastRoll=die;diceResult.text="ROLLED  "+die;
        int old=fastTrack?fastPosition:position;
        pendingDecision=new PendingDecision{kind="move",amount=die};SaveCheckpoint(true);
        yield return world.MovePawn(old,die,fastTrack);
        if(fastTrack)fastPosition=(old+die)%24;else position=(old+die)%24;
        pendingDecision=new PendingDecision();CompleteMovement(old,die);
        Refresh();rolling=false;UpdateTurnStatus();rollButton.interactable=modal==null&&!gameOver;SaveCheckpoint();
    }
    // Include the destination, but never the starting space. Movement owns the
    // payment so landing on Payday cannot collect it a second time.
    void CollectMovementPaydays(int start,int steps)
    {
        for(int step=1;step<=steps;step++)
            if(fastTrack?fastTiles[(start+step)%fastTiles.Length]=="CASHFLOW DAY":ratTiles[(start+step)%ratTiles.Length]=="PAYDAY")Payday();
    }
    void Land(string tile)
    {
        if(tile=="PAYDAY")return; // Movement already paid and displayed its receipt.
        if(tile=="OPPORTUNITY"){ ShowDealChoice(); return; }
        if(tile=="DOODAD"){ ShowUnexpectedExpense(); return; }
        if(tile=="CHARITY"){ShowCharityChoice();return;}
        if(tile=="BABY"){ if(children>=3){ShowEventPanel("FAMILY UPDATE","Your family is already at the three-child game limit. Monthly expenses stay the same.",C("#be185d"));return;}children++; Log("New baby: monthly expenses +"+Money(profession.childPay)+"; monthly cashflow -"+Money(profession.childPay)+"."); ShowEventPanel("NEW BABY • MONTHLY CHANGE","Monthly expenses +"+Money(profession.childPay)+"\nNet monthly cashflow -"+Money(profession.childPay)+"\n\nThis repeats each month. No cash is paid now.",C("#be185d")); return; }
        if(tile=="DOWNSIZED"){ ShowRequiredExpense("DOWNSIZED","Your job is interrupted. Pay one month of expenses, then skip two turns.",Expenses(),2); return; }
        if(tile=="MARKET"){ ShowMarketOffer(); return; }
        if(fastTrack)ResolveFastTrack(tile);
    }
    int Expenses(){return profession.taxes+PersonalDebtPayments()+profession.other+children*profession.childPay+LoanPayment();}
    int Income(){return (fastTrack?0:profession.salary)+passive;}
    void Payday(){int amount=CashflowRules.NetCashflow(Income(),Expenses());cash=CashflowRules.SettlePayday(cash,Income(),Expenses());string title=fastTrack?"CASHFLOW DAY":"PAYDAY";Log(title+": cash "+SignedMoney(amount)+".");if(world!=null)world.Audio.Payday();}
    void CheckEscape(){if(!fastTrack&&passive>Expenses()){fastTrack=true;fastPosition=0;fastTrackIntro=true;DrawBoard();MoveToken();Log("You escaped the Rat Race! Welcome to the Fast Track.");}}
    void Refresh()
    {
        CheckGameOver();UpdateTurnStatus();if(header==null)return;
        if(bottomCash!=null)bottomCash.text=Money(cash);
        if(bottomNetCashflow!=null){int net=Income()-Expenses();bottomNetCashflow.text=(net>0?"+":"")+Money(net);bottomNetCashflow.color=net<0?C("#ffaaa8"):net>0?C("#b8ec8a"):C("#eadfff");}
        if(freedomTitle!=null)freedomTitle.text=fastTrack?"DREAM SAVINGS":"PASSIVE / EXPENSES";
        if(freedomFill!=null)freedomFill.fillAmount=fastTrack?Mathf.Clamp01(cash/(float)dreamGoals[selectedDream]):Mathf.Clamp01(passive/(float)Mathf.Max(1,Expenses()));
        if(freedomText!=null)freedomText.text=fastTrack?Money(cash)+" / "+Money(dreamGoals[selectedDream]):Money(passive)+" / "+Money(Expenses())+" per month";
        header.text=profession.name.ToUpper()+"  /  "+(fastTrack?"FAST TRACK":"RAT RACE");
        if(modeReadout!=null) modeReadout.text=fastTrack?"FAST TRACK":"RAT RACE";
        if(cashReadout!=null) cashReadout.text="◉  "+Money(cash);
        if(flowReadout!=null) flowReadout.text="↗  "+Money(passive);
    }
    void Log(string s){RecordHistory(s);if(log!=null)log.text=s;UpdateTurnStatus();}
    string Money(int v){return (v<0?"-":"")+"$"+Mathf.Abs(v).ToString("N0");}

    void Win(){if(!gameWon)return;var p=Modal(new Vector2(520,260));var t=Label(p.transform,"DREAM ACHIEVED!\n\nYou reached your dream: "+dream+"\n\nCongratulations!",26);Place(t.rectTransform,0,20,470,190);var again=Button(p.transform,"PLAY AGAIN",C("#16a34a"),StartGame);Place(again.GetComponent<RectTransform>(),0,-95,160,42);CashflowAds.Ensure().SetBannerPlacement(true);}
}
