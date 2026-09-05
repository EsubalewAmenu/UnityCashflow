from pathlib import Path
p=Path('Assets/Scripts/CashflowGame.cs');s=p.read_text(encoding='utf-8').replace('(fastTrack?0:profession.salary)+passive','(fastTrack?fastFlow:profession.salary)+passive');p.write_text(s,encoding='utf-8')
p=Path('Assets/Scripts/CashflowFinance.cs');s=p.read_text(encoding='utf-8').replace('        var repay=Button','        bool repaid=false;\n        var repay=Button').replace('if(bankLoan<1000||cash<1000)return;bankLoan-=1000;','if(repaid||bankLoan<1000||cash<1000)return;repaid=true;bankLoan-=1000;');p.write_text(s,encoding='utf-8')
p=Path('Assets/Editor/DealReview.cs');s=p.read_text().replace('Require(!GameObject.Find("Buy Deal").GetComponent<Button>().interactable,"Unaffordable deal enabled");','Require(GameObject.Find("Buy Deal").GetComponent<Button>().interactable,"Borrow option missing");')
s=s.replace('Click("Buy Deal");Require(Get("cash")==0,"Unaffordable purchase allowed");Click("Pass Deal");','Click("Buy Deal");Require(Get("cash")==0&&Get("bankLoan")==0,"Loan charged before confirmation");Click("Pass Loan");')
s=s.replace('                    Call("CloseModal");running=false;return;', '''                    Call("CloseModal");break;
                case 12:Call("StartGame");Set("cash",5000);Call("Land","CHARITY");break;
                case 13:
                    ScreenCapture.CaptureScreenshot(Folder+"/charity-choice.png");Click("Pass Charity");
                    Require(Get("cash")==5000&&!(bool)typeof(CashflowGame).GetField("charity",Flags).GetValue(Game),"Charity pass changed cash or dice");
                    Call("Land","CHARITY");break;
                case 14:
                    var donate=GameObject.Find("Donate Charity").GetComponent<Button>();donate.onClick.Invoke();int after=Get("cash");donate.onClick.Invoke();
                    Require(after<5000&&Get("cash")==after&&(bool)typeof(CashflowGame).GetField("charity",Flags).GetValue(Game),"Donation failed or charged twice");
                    Set("cash",0);Call("Land","CHARITY");break;
                case 15:
                    Require(!GameObject.Find("Donate Charity").GetComponent<Button>().interactable,"Unaffordable donation enabled");Click("Pass Charity");
                    Set("cash",400);Call("OfferDeal",new CashflowGame.Deal("Review property",4000,220),false);break;
                case 16:Click("Buy Deal");break;
                case 17:
                    ScreenCapture.CaptureScreenshot(Folder+"/loan-review.png");
                    Require(Get("cash")==400&&Get("bankLoan")==0,"Loan review modified balance");break;
                case 18:
                    var loan=GameObject.Find("Confirm Loan").GetComponent<Button>();loan.onClick.Invoke();loan.onClick.Invoke();
                    Require(Get("cash")==400&&Get("bankLoan")==4000&&Get("passive")==220,"Borrow-and-buy accounting or duplicate guard failed");
                    int expected=(int)typeof(CashflowGame).GetMethod("Income",Flags).Invoke(Game,null)-(int)typeof(CashflowGame).GetMethod("Expenses",Flags).Invoke(Game,null);
                    Call("Payday");Require(Get("cash")==400+expected,"Payday excludes interest");Set("cash",5000);Call("ShowStatement");break;
                case 19:
                    ScreenCapture.CaptureScreenshot(Folder+"/finances.png");
                    int oldExpenses=(int)typeof(CashflowGame).GetMethod("Expenses",Flags).Invoke(Game,null);
                    var repay=GameObject.Find("Repay Loan").GetComponent<Button>();repay.onClick.Invoke();repay.onClick.Invoke();
                    Require(Get("bankLoan")==3000&&Get("cash")==4000,"Principal repayment accounting failed");
                    Require((int)typeof(CashflowGame).GetMethod("Expenses",Flags).Invoke(Game,null)==oldExpenses-100,"Repayment did not reduce interest");break;
                case 20:
                    Click("Close Finances");Require(!UnityEngine.Object.FindObjectOfType<BoardWorld>().InputBlocked,"Finances blocked board after close");
                    Call("StartGame");Require(Get("bankLoan")==0,"New game retained loan");
                    File.AppendAllText(Folder+"/results.txt","PASS: charity accept/pass and affordability; loan confirmation/pass, interest, duplicate guards, repayment, finance close and new-game reset.\\n");
                    running=false;return;''')
p.write_text(s)
