from pathlib import Path
p=Path('Assets/Scripts/CashflowGame.cs');s=p.read_text(encoding='utf-8').replace('public class CashflowGame :','public partial class CashflowGame :')
s=s.replace('readonly Deal[] deals = {','readonly Deal[] smallDeals = {').replace('new Deal("Micro SaaS", 3000, 400), new Deal("4-Plex", 10000, 800), new Deal("Pizza Franchise", 35000, 3000) };','new Deal("Micro SaaS", 3000, 400) };\n    readonly Deal[] bigDeals = { new Deal("4-Plex", 10000, 800), new Deal("Pizza Franchise", 35000, 3000) };')
s=s.replace('var d=deals[Random.Range(0,deals.Length)]; OfferDeal(d);','ShowDealChoice();')
a=s.index('    void OfferDeal(Deal d)');b=s.index('    int Expenses()',a);s=s[:a]+s[b:]
a=s.index('        var shade=Modal(new Vector2(470,255));');b=s.index('\n    }',a)
s=s[:a]+'''        var shade=CardShell("BOARD EVENT",title,color,new Vector2(700,440));
        var copy=Label(shade.transform,details,23);copy.color=C("#40394c");Place(copy.rectTransform,0,-5,610,170);
        var ok=Button(shade.transform,"CONTINUE",color,CloseModal);Place(ok.GetComponent<RectTransform>(),0,-165,260,54);SetButtonText(ok,Color.white,18);
'''+s[b:];p.write_text(s,encoding='utf-8')
