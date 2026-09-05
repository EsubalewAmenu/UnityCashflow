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
public class CashflowGame : MonoBehaviour
{
    [Serializable] public class Profession { public string name; public int salary, savings, taxes, mortgagePay, schoolPay, carPay, cardPay, retailPay, other, childPay; }
    [Serializable] public class Deal { public string title; public int down, flow; public Deal(string t, int d, int f) { title=t; down=d; flow=f; } }

    Profession[] professions = {
        new Profession { name="Engineer", salary=4900, savings=400, taxes=1050, mortgagePay=400, schoolPay=60, carPay=80, cardPay=120, retailPay=50, other=1090, childPay=250 },
        new Profession { name="Teacher", salary=3300, savings=400, taxes=630, mortgagePay=500, schoolPay=60, carPay=60, cardPay=90, retailPay=50, other=760, childPay=180 },
        new Profession { name="Medical Doctor", salary=13200, savings=400, taxes=3420, mortgagePay=1900, schoolPay=750, carPay=380, cardPay=270, retailPay=50, other=2880, childPay=640 },
        new Profession { name="Janitor", salary=1600, savings=560, taxes=280, mortgagePay=200, schoolPay=0, carPay=60, cardPay=60, retailPay=50, other=300, childPay=70 },
        new Profession { name="Lawyer", salary=7500, savings=400, taxes=1830, mortgagePay=1100, schoolPay=390, carPay=220, cardPay=180, retailPay=50, other=1650, childPay=380 }
    };
    readonly string[] dreams = { "Stock Market for Kids", "Save the Coral Reefs", "Private Superyacht", "African Wildlife Sanctuary", "Heli-Skiing Chalet", "Private Tropical Island", "7 Wonders Expedition", "Community Health Clinic" };
    readonly int[] dreamGoals = { 125000, 175000, 220000, 140000, 130000, 250000, 90000, 200000 };
    readonly string[] ratTiles = { "OPPORTUNITY", "DOODAD", "OPPORTUNITY", "CHARITY", "OPPORTUNITY", "PAYDAY", "OPPORTUNITY", "MARKET", "OPPORTUNITY", "DOODAD", "OPPORTUNITY", "BABY", "OPPORTUNITY", "PAYDAY", "OPPORTUNITY", "MARKET", "OPPORTUNITY", "DOODAD", "OPPORTUNITY", "DOWNSIZED", "OPPORTUNITY", "PAYDAY", "OPPORTUNITY", "MARKET" };
    readonly string[] fastTiles = { "CASHFLOW DAY", "BIO-TECH +25K", "DREAM", "PIZZA +18K", "TAX AUDIT", "DREAM", "CASHFLOW DAY", "GOLF +35K", "DREAM", "CLOUD +40K", "LAWSUIT", "DREAM", "CASHFLOW DAY", "CAR WASH +15K", "DREAM", "EV +30K", "DIVORCE", "DREAM", "CASHFLOW DAY", "CARGO +28K", "DREAM", "HOTEL +22K", "CORRECTION", "DREAM" };
    readonly Deal[] deals = { new Deal("2/1 Condo", 4000, 220), new Deal("3/2 House", 5000, 250), new Deal("Vending Route", 4000, 350), new Deal("Micro SaaS", 3000, 400), new Deal("4-Plex", 10000, 800), new Deal("Pizza Franchise", 35000, 3000) };

    Profession profession; string dream; int selectedDream; int cash, passive, children, position, fastPosition, fastFlow; bool fastTrack, charity; int skipTurns;
    BoardWorld world; Button rollButton; bool rolling; GameObject modal;
    RectTransform uiRoot; Text bottomCash, freedomText, diceResult; Image freedomFill;
    Canvas canvas; Text header, statement, log, cashReadout, flowReadout, modeReadout, hoverCopy; RectTransform board; GameObject hoverPanel; Sprite circleSprite, roundedSprite; Font uiFont; readonly Dictionary<string, Sprite> artSprites = new Dictionary<string, Sprite>(); readonly List<Button> tileButtons = new List<Button>(); readonly List<GameObject> boardDecorations = new List<GameObject>();

    void Awake() { Application.runInBackground=true; BuildCanvas(); }
    public void OpenScene(string sceneName)
    {
        if (sceneName == "ProfessionSelection" || sceneName == "Main") ShowSetup();
        else if (sceneName == "DreamSelection") ShowDreams();
        else if (sceneName == "GameBoard") StartGame();
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
    Button Button(Transform parent,string title,Color color,Action action) { var go=new GameObject(title,typeof(RectTransform),typeof(Image),typeof(Button)); go.transform.SetParent(parent,false); go.GetComponent<Image>().color=color; var b=go.GetComponent<Button>(); b.onClick.AddListener(()=>action()); var tx=Label(go.transform,title,16); Stretch(tx.rectTransform); return b; }
    void Stretch(RectTransform r) { r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=new Vector2(5,4);r.offsetMax=new Vector2(-5,-4); }
    void BuildCanvas() { var root=new GameObject("Cashflow Unity",typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster)); canvas=root.GetComponent<Canvas>(); canvas.renderMode=RenderMode.ScreenSpaceOverlay; var scaler=root.GetComponent<CanvasScaler>(); scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution=new Vector2(1280,720); scaler.matchWidthOrHeight=.5f; uiRoot=new GameObject("Safe Area",typeof(RectTransform),typeof(SafeBoardLayout)).GetComponent<RectTransform>(); uiRoot.SetParent(root.transform,false); var bg=new GameObject("Background",typeof(Image)); bg.transform.SetParent(uiRoot,false); bg.GetComponent<Image>().color=C("#0b1220"); Stretch(bg.GetComponent<RectTransform>()); }
    void ClearUI() { foreach(Transform child in uiRoot) if(child.name!="Background") Destroy(child.gameObject); }
    // Landing page: designed to match the original game's financial-strategy dashboard.
    void ShowSetup()
    {
        if (profession == null) profession = professions[0];
        ClearUI();

        var shell = Panel(uiRoot, new Vector2(.5f,.5f), new Vector2(.5f,.5f), new Vector2(1220,650), C("#080f1d"));
        var shellImage = shell.GetComponent<Image>();

        // Header ---------------------------------------------------------------
        var icon = Panel(shell.transform, new Vector2(.5f,.5f), new Vector2(.5f,.5f), new Vector2(52,52), C("#00c98d"));
        Place(icon.GetComponent<RectTransform>(), -548, 270, 52, 52);
        var dollar = Label(icon.transform, "$", 30); dollar.color = C("#061224"); Stretch(dollar.rectTransform);
        var title = Label(shell.transform, "CASHFLOW 101", 27, TextAnchor.MiddleLeft); title.fontStyle = FontStyle.Bold; Place(title.rectTransform, -488, 282, 280, 34);
        var tagline = Label(shell.transform, "RICH DAD FINANCIAL STRATEGY", 14, TextAnchor.MiddleLeft); tagline.color = C("#00e2a4"); tagline.fontStyle = FontStyle.Bold; Place(tagline.rectTransform, -487, 257, 320, 24);
        var nameBox = Panel(shell.transform, new Vector2(.5f,.5f), new Vector2(.5f,.5f), new Vector2(312,44), C("#18243a")); Place(nameBox.GetComponent<RectTransform>(), 12, 270, 312, 44);
        var name = Label(nameBox.transform, "♙  Name:  Player", 17, TextAnchor.MiddleLeft); name.color = C("#b4c0d3"); Place(name.rectTransform, 18, 0, 275, 35);
        var resume = Button(shell.transform, "✣  Resume Saved", C("#1b2c45"), ()=>Log("No saved game is available yet.")); Place(resume.GetComponent<RectTransform>(), 496,270,168,44); SetButtonText(resume, C("#00e2a4"), 15);
        Divider(shell.transform, 235, 1140);

        // Progress bar ---------------------------------------------------------
        var stepOne = Panel(shell.transform, new Vector2(.5f,.5f), new Vector2(.5f,.5f), new Vector2(565,35), C("#079d70")); Place(stepOne.GetComponent<RectTransform>(), -287, 196, 565, 35);
        var stepOneText = Label(stepOne.transform, "▣  1. Profession (" + profession.name + ")", 17); stepOneText.fontStyle=FontStyle.Bold; Stretch(stepOneText.rectTransform);
        var stepTwo = Label(shell.transform, "♙  2. Dream & Start", 17); stepTwo.color=C("#91a3bf"); stepTwo.fontStyle=FontStyle.Bold; Place(stepTwo.rectTransform, 310,196,430,35);
        Divider(shell.transform, 166, 1140);

        var section = Label(shell.transform, "▣  STEP 1: CHOOSE PROFESSION", 18, TextAnchor.MiddleLeft); section.color=C("#cbd5e1"); section.fontStyle=FontStyle.Bold; Place(section.rectTransform,-540,130,460,30);
        var randomize = Button(shell.transform, "⤨  Randomize", C("#080f1d"), ()=> { profession=professions[Random.Range(0,professions.Length)]; ShowSetup(); }); Place(randomize.GetComponent<RectTransform>(), 505,130,150,30); SetButtonText(randomize,C("#facc15"),15);

        // Profession cards -----------------------------------------------------
        for (int i=0; i<professions.Length; i++)
        {
            int n=i; var p=professions[i]; bool selected=p==profession;
            var card=Button(shell.transform, "", selected ? C("#123b39") : C("#17243a"), ()=> { profession=professions[n]; ShowSetup(); });
            Place(card.GetComponent<RectTransform>(), -440+i*220, 55, 205, 84);
            var cardImage=card.GetComponent<Image>(); if(selected) cardImage.color=C("#123f3c");
            var cardTitle=Label(card.transform,p.name,18,TextAnchor.MiddleLeft); cardTitle.fontStyle=FontStyle.Bold; Place(cardTitle.rectTransform,12,17,180,28);
            var salary=Label(card.transform,Money(p.salary)+"/mo",15,TextAnchor.MiddleLeft); salary.color=C("#00d99a"); salary.fontStyle=FontStyle.Bold; Place(salary.rectTransform,12,-13,110,22);
            var start=Label(card.transform,"Cash:  "+Money(p.savings),14,TextAnchor.MiddleRight); start.color=C("#9fb0c8"); Place(start.rectTransform,85,-13,108,22);
            if(selected) { var dot=Label(card.transform,"●",20); dot.color=C("#00d99a"); Place(dot.rectTransform,82,17,20,22); }
        }

        // Financial preview ----------------------------------------------------
        int costs = Expenses(); int startingFlow = profession.salary-costs;
        var detail=Panel(shell.transform,new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(1135,172),C("#151e31")); Place(detail.GetComponent<RectTransform>(),0,-115,1135,172);
        var selectedLabel=Label(detail.transform,"Selected: "+profession.name,18,TextAnchor.MiddleLeft); selectedLabel.fontStyle=FontStyle.Bold; Place(selectedLabel.rectTransform,20,60,340,28);
        var savings=Label(detail.transform,"Savings:  "+Money(profession.savings),15,TextAnchor.MiddleRight); savings.fontStyle=FontStyle.Bold; Place(savings.rectTransform,430,60,665,28);
        FinancialMetric(detail.transform,-370,"SALARY",Money(profession.salary),C("#00d99a"));
        FinancialMetric(detail.transform,0,"EXPENSES",Money(costs),C("#fb7185"));
        FinancialMetric(detail.transform,370,"STARTING CASHFLOW",(startingFlow >= 0 ? "+" : "")+Money(startingFlow)+"/mo",C("#facc15"));
        var debtLeft=Label(detail.transform,"Home Mortgage: $38,000",15,TextAnchor.MiddleLeft); debtLeft.color=C("#9fb0c8"); Place(debtLeft.rectTransform,20,-59,360,25);
        var debtRight=Label(detail.transform,"School Loans: $12,000",15,TextAnchor.MiddleRight); debtRight.color=C("#9fb0c8"); Place(debtRight.rectTransform,350,-59,745,25);

        var next=Button(shell.transform,"Next: Choose Your Dream  →",C("#079d70"),GoToDreamSelection); Place(next.GetComponent<RectTransform>(),0,-262,1135,52); SetButtonText(next,Color.white,19);
    }

    void FinancialMetric(Transform parent, float x, string heading, string amount, Color amountColor)
    {
        var box=Panel(parent,new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(290,66),C("#0d1729")); Place(box.GetComponent<RectTransform>(),x,-3,290,66);
        var h=Label(box.transform,heading,13); h.color=C("#9fb0c8"); Place(h.rectTransform,0,14,270,20);
        var a=Label(box.transform,amount,16); a.color=amountColor; a.fontStyle=FontStyle.Bold; Place(a.rectTransform,0,-11,270,22);
    }
    void Divider(Transform parent,float y,float width) { var d=new GameObject("Divider",typeof(RectTransform),typeof(Image)); d.transform.SetParent(parent,false); d.GetComponent<Image>().color=C("#26354d"); Place(d.GetComponent<RectTransform>(),0,y,width,1); }
    void SetButtonText(Button button, Color color, int size) { var text=button.GetComponentInChildren<Text>(); if(text!=null){text.color=color;text.fontSize=size;text.fontStyle=FontStyle.Bold;} }
    void ShowDreams()
    {
        if (profession == null) LoadProfessionSelection();
        selectedDream = Mathf.Clamp(PlayerPrefs.GetInt("CashflowDream", selectedDream), 0, dreams.Length - 1);
        if (selectedDream < 0 || selectedDream >= dreams.Length) selectedDream = 0;
        ClearUI();
        var shell = Panel(uiRoot,new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(1220,650),C("#080f1d"));

        // Shared branded header
        var icon=Panel(shell.transform,new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(52,52),C("#00c98d")); Place(icon.GetComponent<RectTransform>(),-548,270,52,52);
        var dollar=Label(icon.transform,"$",30); dollar.color=C("#061224"); Stretch(dollar.rectTransform);
        var title=Label(shell.transform,"CASHFLOW 101",27,TextAnchor.MiddleLeft); title.fontStyle=FontStyle.Bold; Place(title.rectTransform,-488,282,280,34);
        var tag=Label(shell.transform,"RICH DAD FINANCIAL STRATEGY",14,TextAnchor.MiddleLeft); tag.color=C("#00e2a4"); tag.fontStyle=FontStyle.Bold; Place(tag.rectTransform,-487,257,320,24);
        var nameBox=Panel(shell.transform,new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(312,44),C("#18243a")); Place(nameBox.GetComponent<RectTransform>(),12,270,312,44);
        var name=Label(nameBox.transform,"♙  Name:  Player",17,TextAnchor.MiddleLeft); name.color=C("#c1cbda"); Place(name.rectTransform,18,0,275,35);
        var resume=Button(shell.transform,"✣  Resume Saved",C("#1b2c45"),()=>Log("No saved game is available yet.")); Place(resume.GetComponent<RectTransform>(),496,270,168,44); SetButtonText(resume,C("#00e2a4"),15);
        Divider(shell.transform,235,1140);

        var one=Label(shell.transform,"▣  1. Profession ("+profession.name+")",16); one.color=C("#91a3bf"); one.fontStyle=FontStyle.Bold; Place(one.rectTransform,-300,196,500,35);
        var two=Panel(shell.transform,new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(565,35),C("#079d70")); Place(two.GetComponent<RectTransform>(),287,196,565,35);
        var twoText=Label(two.transform,"♙  2. Dream & Start",17); twoText.fontStyle=FontStyle.Bold; Stretch(twoText.rectTransform);
        Divider(shell.transform,166,1140);

        var step=Label(shell.transform,"♙  STEP 2: YOUR LIFELONG DREAM",18,TextAnchor.MiddleLeft); step.color=C("#facc15"); step.fontStyle=FontStyle.Bold; Place(step.rectTransform,-540,130,500,30);
        var count=Label(shell.transform,(selectedDream+1)+" of "+dreams.Length,15,TextAnchor.MiddleRight); count.color=C("#9fb0c8"); count.fontStyle=FontStyle.Bold; Place(count.rectTransform,420,130,650,30);

        // Dream carousel
        var carousel=Panel(shell.transform,new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(1135,178),C("#151e31")); Place(carousel.GetComponent<RectTransform>(),0,10,1135,178);
        var previous=Button(carousel.transform,"‹",C("#1b2c45"),()=>{selectedDream=(selectedDream+dreams.Length-1)%dreams.Length;PlayerPrefs.SetInt("CashflowDream",selectedDream);ShowDreams();}); Place(previous.GetComponent<RectTransform>(),-500,0,50,50); SetButtonText(previous,C("#cbd5e1"),36);
        var next=Button(carousel.transform,"›",C("#1b2c45"),()=>{selectedDream=(selectedDream+1)%dreams.Length;PlayerPrefs.SetInt("CashflowDream",selectedDream);ShowDreams();}); Place(next.GetComponent<RectTransform>(),500,0,50,50); SetButtonText(next,C("#cbd5e1"),36);
        var target=Label(carousel.transform,"FAST TRACK TARGET",15); target.color=C("#00d99a"); target.fontStyle=FontStyle.Bold; Place(target.rectTransform,0,52,400,25);
        var dreamName=Label(carousel.transform,dreams[selectedDream],27); dreamName.fontStyle=FontStyle.Bold; Place(dreamName.rectTransform,0,17,760,40);
        var goalBox=Panel(carousel.transform,new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(180,38),C("#0d1729")); Place(goalBox.GetComponent<RectTransform>(),0,-25,180,38);
        var goal=Label(goalBox.transform,"Goal:  "+Money(dreamGoals[selectedDream]),17); goal.color=C("#facc15"); goal.fontStyle=FontStyle.Bold; Stretch(goal.rectTransform);
        var dots=""; for(int i=0;i<dreams.Length;i++) dots += i==selectedDream ? "● " : "• "; var dotText=Label(carousel.transform,dots,22); dotText.color=C("#00d99a"); Place(dotText.rectTransform,0,-62,300,24);

        // Player confirmation and final start action
        int flow=profession.salary-Expenses();
        var confirmation=Panel(shell.transform,new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(1135,140),C("#101b2e")); Place(confirmation.GetComponent<RectTransform>(),0,-170,1135,140);
        var player=Label(confirmation.transform,"Player:  Player ("+profession.name+")",17,TextAnchor.MiddleLeft); player.color=C("#d4dbea"); Place(player.rectTransform,20,39,500,28);
        var cashflow=Label(confirmation.transform,(flow>=0?"+":"")+Money(flow)+"/mo",17,TextAnchor.MiddleRight); cashflow.color=C("#00e2a4"); cashflow.fontStyle=FontStyle.Bold; Place(cashflow.rectTransform,420,39,675,28);
        var start=Button(confirmation.transform,"⊙  START CASHFLOW GAME",C("#079d70"),()=>{PlayerPrefs.SetInt("CashflowDream",selectedDream);PlayerPrefs.Save();SceneManager.LoadScene("GameBoard");}); Place(start.GetComponent<RectTransform>(),0,-25,1095,66); SetButtonText(start,Color.white,23);
    }
    void StartGame(){ if(profession==null)LoadProfessionSelection(); selectedDream=Mathf.Clamp(PlayerPrefs.GetInt("CashflowDream",0),0,dreams.Length-1); dream=dreams[selectedDream]; cash=profession.savings; passive=0;children=0;position=0;fastPosition=0;fastFlow=0;fastTrack=false;charity=false;skipTurns=0;BuildGame(); Log("Welcome, "+profession.name+". Roll the dice to begin."); }
    void BuildGame()
    {
        if(world!=null){world.gameObject.SetActive(false);Destroy(world.gameObject);}
        rolling=false;modal=null;
        ClearUI();
        uiRoot.Find("Background").GetComponent<Image>().color=Color.clear;
        var top=Panel(uiRoot,new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(360,44),C("#49375f"));Place(top.GetComponent<RectTransform>(),-433,325,360,44);top.GetComponent<Image>().sprite=RoundedSprite();top.GetComponent<Image>().type=Image.Type.Sliced;
        header=Label(top.transform,"",16);Stretch(header.rectTransform);
        var statementButton=Button(uiRoot,"FINANCES",C("#49375f"),ShowStatement);Place(statementButton.GetComponent<RectTransform>(),-185,325,116,44);statementButton.GetComponent<Image>().sprite=RoundedSprite();SetButtonText(statementButton,Color.white,12);
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
        var portraitArt=new GameObject("Player Portrait Art",typeof(RectTransform),typeof(Image)); portraitArt.transform.SetParent(portrait.transform,false); var portraitImage=portraitArt.GetComponent<Image>(); portraitImage.sprite=ArtSprite("player-portrait","GameBoard/character-pieces-sheet",new Rect(.07f,.08f,.21f,.34f)); portraitImage.preserveAspect=true; portraitImage.raycastTarget=false; Stretch(portraitArt.GetComponent<RectTransform>());
        var cashLabel=Label(bottom.transform,"CASH BALANCE",11,TextAnchor.MiddleLeft); cashLabel.color=C("#eadfff"); Place(cashLabel.rectTransform,-395,16,220,20);
        var cashValue=Label(bottom.transform,Money(cash),25,TextAnchor.MiddleLeft); bottomCash=cashValue; cashValue.color=C("#f8d45a"); cashValue.fontStyle=FontStyle.Bold; Place(cashValue.rectTransform,-395,-12,220,30);
        var passiveLabel=Label(bottom.transform,"PASSIVE INCOME",12); passiveLabel.color=C("#eadfff"); passiveLabel.fontStyle=FontStyle.Bold; Place(passiveLabel.rectTransform,-90,23,280,20);
        var progressBack=Panel(bottom.transform,new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(380,20),C("#16112a")); Place(progressBack.GetComponent<RectTransform>(),-90,-4,380,20);
        var progress=Panel(progressBack.transform,new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(145,13),C("#86c94f")); Place(progress.GetComponent<RectTransform>(),-112,0,145,13);
        freedomFill=progress.GetComponent<Image>(); freedomFill.sprite=RoundedSprite(); freedomFill.type=Image.Type.Filled; freedomFill.fillMethod=Image.FillMethod.Horizontal; Place(progress.GetComponent<RectTransform>(),0,0,370,13);
        var progressText=Label(bottom.transform,"Build your monthly freedom",11); freedomText=progressText; progressText.color=C("#ffffff"); Place(progressText.rectTransform,-90,-27,290,17);
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
        diceResult=Label(bottom.transform,"READY TO ROLL",12); Place(diceResult.rectTransform,270,0,145,40);
        string[] controls={"SETTINGS","OVERVIEW","FOLLOW","ROTATE","ZOOM +","ZOOM -"};
        for(int i=0;i<controls.Length;i++)
        {
            int n=i;var control=Button(uiRoot,controls[i],C("#544566"),()=>{if(n==0)ShowBoardSettings();else if(n==1)world.SetOverview(true);else if(n==2)world.SetOverview(false);else if(n==3)world.Orbit(35);else world.Zoom(n==4?-.15f:.15f);});
            Place(control.GetComponent<RectTransform>(),585,198-i*47,100,39);control.GetComponent<Image>().sprite=RoundedSprite();control.GetComponent<Image>().type=Image.Type.Sliced;SetButtonText(control,Color.white,11);
        }
        var map=Panel(uiRoot,new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(200,108),C("#f4dfaa"));Place(map.GetComponent<RectTransform>(),530,290,200,108);
        var image=new GameObject("Live minimap",typeof(RectTransform),typeof(RawImage),typeof(Button));image.transform.SetParent(map.transform,false);Stretch(image.GetComponent<RectTransform>());image.GetComponent<RawImage>().texture=world.OverviewTexture;image.GetComponent<Button>().onClick.AddListener(()=>world.SetOverview(true));
    }
    void ShowBoardSettings()
    {
        var panel=Modal(new Vector2(440,330));
        var title=Label(panel.transform,"BOARD SETTINGS",22);Place(title.rectTransform,0,120,400,36);
        var sound=Button(panel.transform,world.Sound?"SOUND: ON":"SOUND: OFF",C("#544566"),()=>{});sound.onClick.AddListener(()=>{world.Sound=!world.Sound;sound.GetComponentInChildren<Text>().text=world.Sound?"SOUND: ON":"SOUND: OFF";});Place(sound.GetComponent<RectTransform>(),0,62,340,40);
        var motion=Button(panel.transform,world.ReducedMotion?"REDUCED MOTION: ON":"REDUCED MOTION: OFF",C("#544566"),()=>{});motion.onClick.AddListener(()=>{world.ReducedMotion=!world.ReducedMotion;motion.GetComponentInChildren<Text>().text=world.ReducedMotion?"REDUCED MOTION: ON":"REDUCED MOTION: OFF";});Place(motion.GetComponent<RectTransform>(),0,10,340,40);
        var scenery=Button(panel.transform,"SHOW / HIDE CITY",C("#544566"),()=>world.ToggleScenery());Place(scenery.GetComponent<RectTransform>(),0,-42,340,40);
        var close=Button(panel.transform,"DONE",C("#69a747"),()=>CloseModal());Place(close.GetComponent<RectTransform>(),0,-110,180,44);
    }
    GameObject Modal(Vector2 size)
    {
        CloseModal(); modal=Panel(uiRoot,Vector2.zero,Vector2.one,Vector2.zero,new Color(.06f,.04f,.12f,.7f));modal.name="Modal Blocker";
        var panel=Panel(modal.transform,new Vector2(.5f,.5f),new Vector2(.5f,.5f),size,C("#302540"));panel.GetComponent<Image>().sprite=RoundedSprite();panel.GetComponent<Image>().type=Image.Type.Sliced;
        if(world!=null)world.InputBlocked=true;if(rollButton!=null)rollButton.interactable=false;return panel;
    }
    void CloseModal(){if(modal!=null){modal.SetActive(false);Destroy(modal);modal=null;}if(world!=null)world.InputBlocked=false;if(rollButton!=null)rollButton.interactable=!rolling;}
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
        if(title.Contains("CHARITY")) return "donate to unlock a two-dice roll.";
        if(title.Contains("MARKET")) return "receive a market offer or stock split.";
        if(title.Contains("BABY")) return "add a new monthly expense.";
        if(title.Contains("DOWNSIZED")) return "pay expenses and lose two turns.";
        if(title.Contains("DREAM")) return "buy your lifelong dream to win.";
        if(title.Contains("+")) return "buy a Fast Track venture for major cashflow.";
        return "resolve this Fast Track event.";
    }
    void ShowTileDetails(string title, bool outer) { if(rolling||world.Dragged)return; ShowEventPanel((outer?"FAST TRACK":"RAT RACE")+" • "+title,TileDescription(title)+"\n\nClick Roll to land here and resolve the event.",outer?C("#075e50"):TileColor(title)); }
    void ShowEventPanel(string title, string details, Color color)
    {
        var shade=Modal(new Vector2(470,255));
        var bar=Panel(shade.transform,new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(470,45),color); Place(bar.GetComponent<RectTransform>(),0,105,470,45);
        var heading=Label(bar.transform,title,18); heading.fontStyle=FontStyle.Bold; Stretch(heading.rectTransform);
        var copy=Label(shade.transform,details,17); copy.color=C("#d4ddea"); Place(copy.rectTransform,0,10,410,115);
        var ok=Button(shade.transform,"CONTINUE",C("#079d70"),()=>CloseModal()); Place(ok.GetComponent<RectTransform>(),0,-91,170,40); SetButtonText(ok,Color.white,16);
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
    void Roll(){if(rolling||modal!=null)return;if(skipTurns>0){skipTurns--;Log("Downsized: turn skipped ("+skipTurns+" left).");return;}StartCoroutine(RollTurn());}
    IEnumerator RollTurn()
    {
        rolling=true;rollButton.interactable=false;HideHover();diceResult.text="ROLLING...";
        yield return world.ThrowDice(charity?2:1);charity=false;
        int die=world.DiceTotal;diceResult.text="ROLLED  "+die;
        int old=fastTrack?fastPosition:position;
        yield return world.MovePawn(old,die,fastTrack);
        if(fastTrack)fastPosition=(old+die)%24;else position=(old+die)%24;
        if(!fastTrack&&old+die>=24)Payday();
        Land(fastTrack?fastTiles[fastPosition]:ratTiles[position]);Refresh();rolling=false;rollButton.interactable=modal==null;
    }
    void Land(string tile)
    {
        if(tile=="PAYDAY"){ Payday(); ShowEventPanel("PAYDAY","Your monthly cashflow was added to cash.\n\nCollected: "+Money(Income()-Expenses()),C("#a16207")); return; }
        if(tile=="OPPORTUNITY"){ var d=deals[Random.Range(0,deals.Length)]; OfferDeal(d); return; }
        if(tile=="DOODAD"){ int cost=Random.Range(100,1001); cash-=cost; Log("Doodad: paid "+Money(cost)); ShowEventPanel("DOODAD","Unexpected expense paid.\n\nCost: "+Money(cost),C("#b91c1c")); return; }
        if(tile=="CHARITY"){ int cost=Mathf.Max(100,Income()/10); cash-=cost; charity=true; Log("Charity: paid "+Money(cost)+"; next roll uses two dice."); ShowEventPanel("CHARITY","You donated "+Money(cost)+".\n\nYour next roll uses two dice!",C("#b45309")); return; }
        if(tile=="BABY"){ children=Mathf.Min(3,children+1); Log("A new baby joins the family. Expenses increased."); ShowEventPanel("BABY","Your family grew.\n\nMonthly expenses increased by "+Money(profession.childPay)+".",C("#be185d")); return; }
        if(tile=="DOWNSIZED"){ cash-=Expenses(); skipTurns=2; Log("Downsized: paid expenses and lose 2 turns."); ShowEventPanel("DOWNSIZED","You paid one month of expenses and must skip two turns.\n\nCost: "+Money(Expenses()),C("#0369a1")); return; }
        if(tile=="MARKET"){ int gain=Mathf.Max(0,passive*5); cash+=gain; Log("Market day: portfolio value realized."); ShowEventPanel("THE MARKET","A market event produced a portfolio gain.\n\nCash received: "+Money(gain),C("#6b21a8")); return; }
        if(fastTrack)
        {
            if(tile.Contains("CASHFLOW")){cash+=fastFlow;Log("Cashflow Day: collected "+Money(fastFlow));ShowEventPanel("CASHFLOW DAY","Your Fast Track income was collected.\n\nReceived: "+Money(fastFlow),C("#a16207"));}
            else if(tile.Contains("DREAM")){Win();}
            else if(tile.Contains("+")){int gained=int.Parse(tile.Split('+')[1].Replace("K",""))*1000;fastFlow+=gained;Log("Fast Track deal: gained "+Money(gained)+"/mo.");ShowEventPanel("FAST TRACK DEAL","Your venture is now producing more income.\n\nNew cashflow: +"+Money(gained)+"/mo",C("#075e50"));}
            else{int lost=cash-cash/2;cash=Mathf.Max(0,cash/2);Log("Fast Track hazard reduced cash.");ShowEventPanel("FAST TRACK HAZARD","A setback reduced your available cash.\n\nLost: "+Money(lost),C("#b91c1c"));}
        }
    }
    void OfferDeal(Deal d){if(cash<d.down){Log("Opportunity: "+d.title+" needs "+Money(d.down)+" — insufficient cash.");return;}var popup=Modal(new Vector2(440,260));var text=Label(popup.transform,"OPPORTUNITY\n"+d.title+"\nDown payment "+Money(d.down)+" • +"+Money(d.flow)+"/month",20);Place(text.rectTransform,0,60,400,145);var buy=Button(popup.transform,"BUY",C("#16a34a"),()=>{cash-=d.down;passive+=d.flow;CloseModal();Log("Bought "+d.title+". Passive income +"+Money(d.flow));CheckEscape();Refresh();});Place(buy.GetComponent<RectTransform>(),-100,-80,130,42);var pass=Button(popup.transform,"PASS",C("#64748b"),()=>{Destroy(popup);Log("Passed on "+d.title);});Place(pass.GetComponent<RectTransform>(),100,-80,130,42);}
    int Expenses(){return profession.taxes+profession.mortgagePay+profession.schoolPay+profession.carPay+profession.cardPay+profession.retailPay+profession.other+children*profession.childPay;}
    int Income(){return (fastTrack?0:profession.salary)+passive;}
    void Payday(){int amount=Income()-Expenses();cash+=amount;Log("PAYDAY: collected "+Money(amount));}
    void CheckEscape(){if(!fastTrack&&passive>Expenses()){fastTrack=true;cash=passive*100;DrawBoard();Log("You escaped the Rat Race! Welcome to the Fast Track.");}}
    void Refresh()
    {
        if(header==null)return;
        if(bottomCash!=null)bottomCash.text=Money(cash);
        if(freedomFill!=null)freedomFill.fillAmount=Mathf.Clamp01(passive/(float)Mathf.Max(1,Expenses()));
        if(freedomText!=null)freedomText.text=Money(passive)+" / "+Money(Expenses())+" per month";
        header.text=profession.name.ToUpper()+"  /  "+(fastTrack?"FAST TRACK":"RAT RACE");
        if(modeReadout!=null) modeReadout.text=fastTrack?"FAST TRACK":"RAT RACE";
        if(cashReadout!=null) cashReadout.text="◉  "+Money(cash);
        if(flowReadout!=null) flowReadout.text="↗  "+Money(fastTrack?fastFlow:passive);
    }
    void Log(string s){if(log!=null)log.text=s;}
    string Money(int v){return (v<0?"-":"")+"$"+Mathf.Abs(v).ToString("N0");}
    void ShowStatement(){var popup=Modal(new Vector2(480,370));var tx=Label(popup.transform,"FINANCIAL STATEMENT\n\nSalary: "+Money(fastTrack?0:profession.salary)+"\nPassive income: "+Money(passive)+"\nExpenses: "+Money(Expenses())+"\nMonthly cashflow: "+Money(Income()-Expenses())+"\nFreedom: "+(Expenses()==0?100:Mathf.Min(100,passive*100/Expenses()))+"%",20);Place(tx.rectTransform,0,20,430,280);var close=Button(popup.transform,"CLOSE",C("#475569"),()=>CloseModal());Place(close.GetComponent<RectTransform>(),0,-145,150,38);}
    void Win(){var p=Modal(new Vector2(520,260));var t=Label(p.transform,"DREAM ACHIEVED!\n\nYou reached your dream: "+dream+"\n\nCongratulations!",26);Place(t.rectTransform,0,20,470,190);var again=Button(p.transform,"PLAY AGAIN",C("#16a34a"),StartGame);Place(again.GetComponent<RectTransform>(),0,-95,160,42);}
}
