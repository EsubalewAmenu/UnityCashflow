using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Random = UnityEngine.Random;

// Self-contained 2D implementation of the JavaScript Cashflow game.
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
    Canvas canvas; Text header, statement, log, cashReadout, flowReadout, modeReadout, hoverCopy; RectTransform board; GameObject token, hoverPanel; Sprite circleSprite; readonly List<Button> tileButtons = new List<Button>();

    void Awake() { BuildCanvas(); ShowSetup(); }
    Font Font() { return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); }
    Color C(string hex) { ColorUtility.TryParseHtmlString(hex, out var c); return c; }
    Sprite CircleSprite()
    {
        if (circleSprite != null) return circleSprite;
        const int size = 128; var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var pixels = new Color[size * size]; var center = (size - 1) * .5f; var radius = center - 1;
        for (int y=0; y<size; y++) for (int x=0; x<size; x++) { float distance = Vector2.Distance(new Vector2(x,y),new Vector2(center,center)); pixels[y*size+x] = distance <= radius ? Color.white : Color.clear; }
        texture.SetPixels(pixels); texture.Apply(); circleSprite = Sprite.Create(texture,new Rect(0,0,size,size),new Vector2(.5f,.5f)); return circleSprite;
    }
    Text Label(Transform parent, string value, int size, TextAnchor align = TextAnchor.MiddleCenter) { var go=new GameObject("Text",typeof(RectTransform),typeof(Text)); go.transform.SetParent(parent,false); var t=go.GetComponent<Text>(); t.font=Font(); t.text=value; t.fontSize=size; t.alignment=align; t.color=Color.white; t.horizontalOverflow=HorizontalWrapMode.Wrap; t.verticalOverflow=VerticalWrapMode.Overflow; return t; }
    Button Button(Transform parent,string title,Color color,Action action) { var go=new GameObject(title,typeof(RectTransform),typeof(Image),typeof(Button)); go.transform.SetParent(parent,false); go.GetComponent<Image>().color=color; var b=go.GetComponent<Button>(); b.onClick.AddListener(()=>action()); var tx=Label(go.transform,title,16); Stretch(tx.rectTransform); return b; }
    void Stretch(RectTransform r) { r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=new Vector2(5,4);r.offsetMax=new Vector2(-5,-4); }
    void BuildCanvas() { var root=new GameObject("Cashflow Unity",typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster)); canvas=root.GetComponent<Canvas>(); canvas.renderMode=RenderMode.ScreenSpaceOverlay; var scaler=root.GetComponent<CanvasScaler>(); scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution=new Vector2(1280,720); var bg=new GameObject("Background",typeof(Image)); bg.transform.SetParent(root.transform,false); bg.GetComponent<Image>().color=C("#0b1220"); Stretch(bg.GetComponent<RectTransform>()); }
    void ClearUI() { foreach(Transform child in canvas.transform) if(child.name!="Background") Destroy(child.gameObject); }
    // Landing page: designed to match the original game's financial-strategy dashboard.
    void ShowSetup()
    {
        if (profession == null) profession = professions[0];
        ClearUI();

        var shell = Panel(canvas.transform, new Vector2(.5f,.5f), new Vector2(.5f,.5f), new Vector2(1220,650), C("#080f1d"));
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

        var next=Button(shell.transform,"Next: Choose Your Dream  →",C("#079d70"),ShowDreams); Place(next.GetComponent<RectTransform>(),0,-262,1135,52); SetButtonText(next,Color.white,19);
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
        if (selectedDream < 0 || selectedDream >= dreams.Length) selectedDream = 0;
        ClearUI();
        var shell = Panel(canvas.transform,new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(1220,650),C("#080f1d"));

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
        var previous=Button(carousel.transform,"‹",C("#1b2c45"),()=>{selectedDream=(selectedDream+dreams.Length-1)%dreams.Length;ShowDreams();}); Place(previous.GetComponent<RectTransform>(),-500,0,50,50); SetButtonText(previous,C("#cbd5e1"),36);
        var next=Button(carousel.transform,"›",C("#1b2c45"),()=>{selectedDream=(selectedDream+1)%dreams.Length;ShowDreams();}); Place(next.GetComponent<RectTransform>(),500,0,50,50); SetButtonText(next,C("#cbd5e1"),36);
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
        var start=Button(confirmation.transform,"⊙  START CASHFLOW GAME",C("#079d70"),()=>{dream=dreams[selectedDream];StartGame();}); Place(start.GetComponent<RectTransform>(),0,-25,1095,66); SetButtonText(start,Color.white,23);
    }
    void StartGame(){ cash=profession.savings; passive=0;children=0;position=0;fastPosition=0;fastFlow=0;fastTrack=false;charity=false;skipTurns=0;BuildGame(); Log("Welcome, "+profession.name+". Roll the dice to begin."); }
    void BuildGame()
    {
        ClearUI();
        // Compact status bar, inspired by the reference game's play screen.
        var top=Panel(canvas.transform,new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(1280,74),C("#090f1b")); Place(top.GetComponent<RectTransform>(),0,323,1280,74);
        var money=Panel(top.transform,new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(42,42),C("#c98400")); Place(money.GetComponent<RectTransform>(),-605,0,42,42);
        var cashIcon=Label(money.transform,"$",22); cashIcon.color=C("#071224"); cashIcon.fontStyle=FontStyle.Bold; Stretch(cashIcon.rectTransform);
        var mode=Panel(top.transform,new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(82,29),C("#202458")); Place(mode.GetComponent<RectTransform>(),-540,0,82,29);
        modeReadout=Label(mode.transform,fastTrack?"FAST TRACK":"RAT RACE",13); modeReadout.color=C("#a8b3ff"); modeReadout.fontStyle=FontStyle.Bold; Stretch(modeReadout.rectTransform);
        var tier=Label(top.transform,"T1",15,TextAnchor.MiddleLeft); tier.color=C("#00db9b"); tier.fontStyle=FontStyle.Bold; Place(tier.rectTransform,-470,0,30,30);
        var status=Panel(top.transform,new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(425,46),C("#101827")); Place(status.GetComponent<RectTransform>(),-210,0,425,46);
        header=Label(status.transform,"",18,TextAnchor.MiddleLeft); header.color=C("#d6deeb"); header.fontStyle=FontStyle.Bold; Place(header.rectTransform,15,0,395,35);
        var coin=Panel(top.transform,new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(90,40),C("#101827")); Place(coin.GetComponent<RectTransform>(),255,0,90,40);
        cashReadout=Label(coin.transform,"◉  "+Money(cash),16); cashReadout.color=C("#e6e2d0"); cashReadout.fontStyle=FontStyle.Bold; Stretch(cashReadout.rectTransform);
        var flow=Panel(top.transform,new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(120,40),C("#101827")); Place(flow.GetComponent<RectTransform>(),370,0,120,40);
        flowReadout=Label(flow.transform,"↗  "+Money(passive),15); flowReadout.color=C("#00e2a4"); flowReadout.fontStyle=FontStyle.Bold; Stretch(flowReadout.rectTransform);
        var statement=Button(top.transform,"▤",C("#079d70"),ShowStatement); Place(statement.GetComponent<RectTransform>(),495,0,52,38); SetButtonText(statement,Color.white,22);
        var menu=Button(top.transform,"⋮",C("#26354d"),()=>Log("How to play: roll, invest, grow passive income, then reach your dream.")); Place(menu.GetComponent<RectTransform>(),555,0,42,42); SetButtonText(menu,Color.white,25);

        board=new GameObject("Game Board",typeof(RectTransform),typeof(Image)).GetComponent<RectTransform>(); board.SetParent(canvas.transform,false); board.GetComponent<Image>().color=C("#060c17"); Place(board,0,-22,1095,490);
        var help=Panel(board,new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(495,45),C("#101b2e")); Place(help.GetComponent<RectTransform>(),-292,210,495,45);
        var helpText=Label(help.transform,"●  Drag to Pan • Pinch/Scroll to Zoom • Tap any tile to inspect",16); helpText.color=C("#b9c5d7"); Stretch(helpText.rectTransform);

        DrawBoard();
        var roll=Button(board,"♙\nROLL\nTap",C("#079d70"),Roll); roll.GetComponent<Image>().sprite=CircleSprite(); Place(roll.GetComponent<RectTransform>(),0,0,118,118); SetButtonText(roll,Color.white,19);
        var rollOutline=roll.gameObject.AddComponent<Outline>(); rollOutline.effectColor=C("#2de0ae"); rollOutline.effectDistance=new Vector2(4,4);
        var rollText=roll.GetComponentInChildren<Text>(); rollText.fontStyle=FontStyle.Bold;
        hoverPanel=Panel(board,new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(410,47),C("#14213a")); Place(hoverPanel.GetComponent<RectTransform>(),0,-206,410,47); hoverPanel.GetComponent<Image>().raycastTarget=false;
        hoverCopy=Label(hoverPanel.transform,"Hover over a board tile to view its effect",13); hoverCopy.color=C("#d4ddea"); hoverCopy.raycastTarget=false; Stretch(hoverCopy.rectTransform); hoverPanel.SetActive(false);
        // Camera/board controls are decorative but the focus action remains useful.
        string[] controls={"⊕","⊖","↗","◉"}; for(int i=0;i<controls.Length;i++){int n=i;var control=Button(board,controls[i],n==3?C("#078c6e"):C("#1b2c45"),()=>{if(n==3)MoveToken(); else Log(n==0?"Zoomed in.":n==1?"Zoomed out.":"Full board view." );});Place(control.GetComponent<RectTransform>(),500,55-i*55,46,46);SetButtonText(control,Color.white,25);}
        log=Label(canvas.transform,"",15); log.color=C("#b9c5d7"); Place(log.rectTransform,0,-325,1050,26);
        Refresh();
    }
    GameObject Panel(Transform p,Vector2 aMin,Vector2 aMax,Vector2 size,Color c){var go=new GameObject("Panel",typeof(RectTransform),typeof(Image));go.transform.SetParent(p,false);var r=go.GetComponent<RectTransform>();r.anchorMin=aMin;r.anchorMax=aMax;r.sizeDelta=size;go.GetComponent<Image>().color=c;return go;}
    void Place(RectTransform r,float x,float y,float w,float h){r.anchorMin=r.anchorMax=new Vector2(.5f,.5f);r.anchoredPosition=new Vector2(x,y);r.sizeDelta=new Vector2(w,h);}
    void DrawBoard()
    {
        foreach(var b in tileButtons) Destroy(b.gameObject); tileButtons.Clear();
        // A large outer Fast Track is always visible; it becomes the active route after escape.
        var fastLabel=Label(board,"T H E   F A S T   T R A C K",12); fastLabel.color=C("#00a8d7"); fastLabel.fontStyle=FontStyle.Bold; Place(fastLabel.rectTransform,0,168,300,22);
        CreateTrack(fastTiles, 500, 205, 80, 36, true);
        var ratLabel=Label(board,"CASHFLOW 101  •  RAT RACE",14); ratLabel.color=C("#c98400"); ratLabel.fontStyle=FontStyle.Bold; Place(ratLabel.rectTransform,0,117,350,24);
        CreateTrack(ratTiles, 265, 132, 62, 34, false);
        var innerGlow=new GameObject("Rat Race Center",typeof(RectTransform),typeof(Image)); innerGlow.transform.SetParent(board,false); innerGlow.transform.SetAsFirstSibling(); var glow=innerGlow.GetComponent<Image>(); glow.sprite=CircleSprite(); glow.color=C("#0b1727"); glow.raycastTarget=false; Place(innerGlow.GetComponent<RectTransform>(),0,0,350,220);
        var innerOutline=innerGlow.AddComponent<Outline>(); innerOutline.effectColor=C("#c98400"); innerOutline.effectDistance=new Vector2(2,2);
        CreateDeck(board,-420,35,"▾ SMALL DEALS","Cost < $20,000",C("#075e50"));
        CreateDeck(board,-420,-105,"▴ BIG DEALS","Cost ≥ $20,000",C("#075e50"));
        CreateDeck(board,420,35,"▤ THE MARKET","Offers & Splits",C("#5b2185"));
        CreateDeck(board,420,-105,"♥ DOODADS","Consumer Debt",C("#7f1d36"));
        var guide=Panel(board,new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(125,114),C("#0f2230")); Place(guide.GetComponent<RectTransform>(),-420,-28,125,114);
        var guideText=Label(guide.transform,"▼ RAT RACE GUIDE\n\n▶ Payday: Earn $\n▶ Deal: Real Estate\n▶ Charity: Roll 2 Dice\n\nESCAPE TO FAST TRACK",9,TextAnchor.UpperLeft); guideText.color=C("#dce6ef"); Stretch(guideText.rectTransform);
        token=new GameObject("Player Token",typeof(RectTransform),typeof(Image)); token.transform.SetParent(board,false); token.GetComponent<Image>().sprite=CircleSprite(); token.GetComponent<Image>().color=C("#facc15"); token.GetComponent<RectTransform>().sizeDelta=new Vector2(26,26); var tokenOutline=token.AddComponent<Outline>(); tokenOutline.effectColor=Color.white; tokenOutline.effectDistance=new Vector2(2,2); MoveToken();
    }
    void CreateTrack(string[] tiles, float rx, float ry, float w, float h, bool outer)
    {
        for(int i=0;i<24;i++)
        {
            float angle=(i/24f)*Mathf.PI*2-Mathf.PI/2; int n=i; string title=tiles[i];
            var b=Button(board,"",TileColor(title),()=>ShowTileDetails(title,outer));
            Place(b.GetComponent<RectTransform>(),Mathf.Cos(angle)*rx,Mathf.Sin(angle)*ry,w,h);
            var outline=b.gameObject.AddComponent<Outline>(); outline.effectColor=outer?C("#38bdf8"):C("#facc15"); outline.effectDistance=new Vector2(1,1);
            var glyph=Label(b.transform,TileGlyph(title),outer?13:12); glyph.fontStyle=FontStyle.Bold; Place(glyph.rectTransform,0,5,w-6,18);
            var caption=Label(b.transform,ShortTitle(title),outer?6:6); caption.fontStyle=FontStyle.Bold; Place(caption.rectTransform,0,-10,w-5,14);
            if (!outer) { var number=Label(b.transform,(n+1).ToString(),5,TextAnchor.UpperLeft); number.color=C("#fef3c7"); Place(number.rectTransform,-w*.35f,h*.22f,13,10); }
            AddTileHover(b, title, outer);
            tileButtons.Add(b);
        }
    }
    void AddTileHover(Button button, string title, bool outer)
    {
        var trigger=button.gameObject.AddComponent<EventTrigger>();
        if (trigger.triggers == null) trigger.triggers = new List<EventTrigger.Entry>();
        var enter=new EventTrigger.Entry { eventID=EventTriggerType.PointerEnter }; enter.callback.AddListener(_=>ShowHover((outer?"FAST TRACK: ":"RAT RACE: ")+title+" — "+TileDescription(title))); trigger.triggers.Add(enter);
        var exit=new EventTrigger.Entry { eventID=EventTriggerType.PointerExit }; exit.callback.AddListener(_=>HideHover()); trigger.triggers.Add(exit);
    }
    void ShowHover(string value) { if(hoverPanel!=null){hoverPanel.SetActive(true);hoverCopy.text=value;} }
    void HideHover() { if(hoverPanel!=null)hoverPanel.SetActive(false); }
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
    void ShowTileDetails(string title, bool outer) { ShowEventPanel((outer?"FAST TRACK":"RAT RACE")+" • "+title,TileDescription(title)+"\n\nClick Roll to land here and resolve the event.",outer?C("#075e50"):TileColor(title)); }
    void ShowEventPanel(string title, string details, Color color)
    {
        var shade=Panel(canvas.transform,new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(470,255),C("#101b2e"));
        var bar=Panel(shade.transform,new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(470,45),color); Place(bar.GetComponent<RectTransform>(),0,105,470,45);
        var heading=Label(bar.transform,title,18); heading.fontStyle=FontStyle.Bold; Stretch(heading.rectTransform);
        var copy=Label(shade.transform,details,17); copy.color=C("#d4ddea"); Place(copy.rectTransform,0,10,410,115);
        var ok=Button(shade.transform,"CONTINUE",C("#079d70"),()=>Destroy(shade)); Place(ok.GetComponent<RectTransform>(),0,-91,170,40); SetButtonText(ok,Color.white,16);
    }
    string TileGlyph(string title)
    {
        if(title.Contains("PAYDAY")||title.Contains("CASHFLOW")) return "$";
        if(title.Contains("OPPORTUNITY")||title.Contains("DEAL")||title.Contains("+")) return "+";
        if(title.Contains("MARKET")) return "M";
        if(title.Contains("DOODAD")) return "!";
        if(title.Contains("CHARITY")) return "G";
        if(title.Contains("BABY")) return "B";
        if(title.Contains("DREAM")) return "*";
        if(title.Contains("DOWNSIZED")||title.Contains("AUDIT")||title.Contains("LAWSUIT")||title.Contains("DIVORCE")||title.Contains("CORRECTION")) return "!";
        return "•";
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
    void CreateDeck(Transform parent,float x,float y,string title,string sub,Color color)
    {
        var deck=Panel(parent,new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(120,61),color); Place(deck.GetComponent<RectTransform>(),x,y,120,61);
        var t=Label(deck.transform,title,9); t.fontStyle=FontStyle.Bold; Place(t.rectTransform,0,12,110,20);
        var s=Label(deck.transform,sub,8); s.color=C("#c9d4df"); Place(s.rectTransform,0,-8,110,17);
        var count=Label(deck.transform,"◉  20 CARDS",7); count.color=C("#9fb0c8"); Place(count.rectTransform,0,-22,100,14);
    }
    Color TileColor(string s){if(s.Contains("PAYDAY")||s.Contains("CASHFLOW"))return C("#a16207");if(s.Contains("OPPORTUNITY")||s.Contains("+"))return C("#047857");if(s.Contains("DOODAD")||s.Contains("AUDIT")||s.Contains("LAWSUIT")||s.Contains("DIVORCE"))return C("#b91c1c");if(s.Contains("DREAM"))return C("#7e22ce");if(s.Contains("MARKET"))return C("#6b21a8");return C("#0369a1");}
    void MoveToken(){int p=fastTrack?fastPosition:position;float angle=(p/24f)*Mathf.PI*2-Mathf.PI/2;float rx=fastTrack?500:265,ry=fastTrack?205:132;token.GetComponent<RectTransform>().anchoredPosition=new Vector2(Mathf.Cos(angle)*rx,Mathf.Sin(angle)*ry);}
    void Roll(){if(skipTurns>0){skipTurns--;Log("Downsized: turn skipped ("+skipTurns+" left).");return;}int die=Random.Range(1,charity?13:7);if(charity)charity=false;int old=fastTrack?fastPosition:position;if(fastTrack)fastPosition=(fastPosition+die)%24;else position=(position+die)%24;if(!fastTrack&&old+die>=24)Payday();MoveToken();Land(fastTrack?fastTiles[fastPosition]:ratTiles[position]);Refresh();}
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
    void OfferDeal(Deal d){if(cash<d.down){Log("Opportunity: "+d.title+" needs "+Money(d.down)+" — insufficient cash.");return;}var popup=Panel(canvas.transform,new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(440,260),C("#1e3a5f"));var text=Label(popup.transform,"OPPORTUNITY\n"+d.title+"\nDown payment "+Money(d.down)+" • +"+Money(d.flow)+"/month",20);Place(text.rectTransform,0,60,400,145);var buy=Button(popup.transform,"BUY",C("#16a34a"),()=>{cash-=d.down;passive+=d.flow;Destroy(popup);Log("Bought "+d.title+". Passive income +"+Money(d.flow));CheckEscape();Refresh();});Place(buy.GetComponent<RectTransform>(),-100,-80,130,42);var pass=Button(popup.transform,"PASS",C("#64748b"),()=>{Destroy(popup);Log("Passed on "+d.title);});Place(pass.GetComponent<RectTransform>(),100,-80,130,42);}
    int Expenses(){return profession.taxes+profession.mortgagePay+profession.schoolPay+profession.carPay+profession.cardPay+profession.retailPay+profession.other+children*profession.childPay;}
    int Income(){return (fastTrack?0:profession.salary)+passive;}
    void Payday(){int amount=Income()-Expenses();cash+=amount;Log("PAYDAY: collected "+Money(amount));}
    void CheckEscape(){if(!fastTrack&&passive>Expenses()){fastTrack=true;cash=passive*100;DrawBoard();Log("You escaped the Rat Race! Welcome to the Fast Track.");}}
    void Refresh()
    {
        if(header==null)return;
        header.text="●  Game started! Welcome, Player.";
        if(modeReadout!=null) modeReadout.text=fastTrack?"FAST TRACK":"RAT RACE";
        if(cashReadout!=null) cashReadout.text="◉  "+Money(cash);
        if(flowReadout!=null) flowReadout.text="↗  "+Money(fastTrack?fastFlow:passive);
    }
    void Log(string s){if(log!=null)log.text=s;}
    string Money(int v){return (v<0?"-":"")+"$"+Mathf.Abs(v).ToString("N0");}
    void ShowStatement(){var popup=Panel(canvas.transform,new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(480,370),C("#172554"));var tx=Label(popup.transform,"FINANCIAL STATEMENT\n\nSalary: "+Money(fastTrack?0:profession.salary)+"\nPassive income: "+Money(passive)+"\nExpenses: "+Money(Expenses())+"\nMonthly cashflow: "+Money(Income()-Expenses())+"\nFreedom: "+(Expenses()==0?100:Mathf.Min(100,passive*100/Expenses()))+"%",20);Place(tx.rectTransform,0,20,430,280);var close=Button(popup.transform,"CLOSE",C("#475569"),()=>Destroy(popup));Place(close.GetComponent<RectTransform>(),0,-145,150,38);}
    void Win(){var p=Panel(canvas.transform,new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(520,260),C("#581c87"));var t=Label(p.transform,"DREAM ACHIEVED!\n\nYou reached your dream: "+dream+"\n\nCongratulations!",26);Place(t.rectTransform,0,20,470,190);var again=Button(p.transform,"PLAY AGAIN",C("#16a34a"),StartGame);Place(again.GetComponent<RectTransform>(),0,-95,160,42);}
}
