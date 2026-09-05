from pathlib import Path
p=Path('Assets/Scripts/CashflowGame.cs');s=p.read_text(encoding='utf-8').replace('int skipTurns;','int skipTurns; int bankLoan;')
s=s.replace('charity=false;skipTurns=0;BuildGame();','charity=false;skipTurns=0;bankLoan=0;BuildGame();')
a=s.index('        var statementButton=Button(');b=s.index('\n',a)
s=s[:a]+'''        var statementButton=Button(uiRoot,"$  FINANCES",C("#fff0cb"),ShowStatement);statementButton.gameObject.name="Finances Button";Place(statementButton.GetComponent<RectTransform>(),-160,325,160,44);statementButton.GetComponent<Image>().sprite=RoundedSprite();SetButtonText(statementButton,C("#594125"),14);
        var financeTrim=statementButton.gameObject.AddComponent<Outline>();financeTrim.effectColor=C("#d6ae5e");financeTrim.effectDistance=new Vector2(2,2);'''+s[b:]
a=s.index('        if(tile=="CHARITY")');b=s.index('\n',a);s=s[:a]+'        if(tile=="CHARITY"){ShowCharityChoice();return;}'+s[b:]
s=s.replace('profession.other+children*profession.childPay;}','profession.other+children*profession.childPay+LoanPayment();}')
a=s.index('    void ShowStatement()');b=s.index('\n',a);s=s[:a]+s[b:]
s=s.replace('return "donate 10% of income and roll two dice next turn.";','return "choose to donate for two dice next roll, or pass and keep one die.";')
p.write_text(s,encoding='utf-8')
p=Path('Assets/Scripts/CashflowCards.cs');s=p.read_text(encoding='utf-8').replace('affordable?"BUY DEAL":"NOT ENOUGH CASH"','affordable?"BUY DEAL":"BORROW & BUY..."').replace('affordable?accent:C("#807a81")','accent').replace('            if(resolved||cash<deal.down)return;resolved=true;','            if(resolved)return;resolved=true;\n            if(cash<deal.down){ShowLoanOffer(deal,big);return;}').replace('buy.interactable=affordable;','buy.interactable=true;')
p.write_text(s,encoding='utf-8')
