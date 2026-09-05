from pathlib import Path
p=Path('Assets/Scripts/BoardWorld.cs');s=p.read_text();s=s.replace('Mathf.Max(distance,Mathf.Abs(p.x)/tanH-p.z,Mathf.Abs(p.y)/tanV-p.z)','Mathf.Max(distance,Mathf.Max(Mathf.Abs(p.x)/tanH-p.z,Mathf.Abs(p.y)/tanV-p.z))');p.write_text(s)
p=Path('Assets/Editor/BoardReview.cs');s=p.read_text();s=s.replace('                    typeof(CashflowGame).GetMethod("ShowBoardSettings",flags).Invoke(game,null);','''                    GameObject.Find("Board Settings Button").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
                    var settings=GameObject.Find("Modal Blocker");
                    foreach(string mode in new[]{"OVERVIEW","FOLLOW"})
                    {
                        var choice=settings.GetComponentsInChildren<UnityEngine.UI.Button>().First(b=>b.GetComponentInChildren<UnityEngine.UI.Text>().text.StartsWith(mode));
                        choice.onClick.Invoke();
                        if(world.Follow!=(mode=="FOLLOW"))throw new Exception("Settings camera selection failed");
                    }
                    File.AppendAllText(Path.Combine(Folder,"results.txt"),"PASS: bottom-bar settings opens; Overview and Follow selections work.\\n");''');p.write_text(s)
