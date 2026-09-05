from pathlib import Path
p=Path('Assets/Scripts/CashflowGame.cs'); s=p.read_text(encoding="utf-8"); a=s.index('        string[] controls='); b=s.index('\n    }\n    void ShowBoardSettings',a)
s=s[:a]+'''        var settings=Button(bottom.transform,"SETTINGS",C("#544566"),ShowBoardSettings);
        settings.gameObject.name="Board Settings Button";
        Place(settings.GetComponent<RectTransform>(),160,0,100,52);
        settings.GetComponent<Image>().sprite=RoundedSprite();
        SetButtonText(settings,Color.white,12);
'''+s[b:]
s=s.replace('270,0,145,40','285,0,140,40')
s=s.replace('Modal(new Vector2(440,330))','Modal(new Vector2(440,450))').replace('0,120,400,36','0,180,400,36')
s=s.replace('        var sound=Button(panel.transform', '''        var cameraTitle=Label(panel.transform,"CAMERA VIEW",13);Place(cameraTitle.rectTransform,0,133,340,24);
        var overview=Button(panel.transform,world.Follow?"OVERVIEW":"OVERVIEW  •",C("#544566"),()=>{});
        var follow=Button(panel.transform,world.Follow?"FOLLOW  •":"FOLLOW",C("#544566"),()=>{});
        overview.onClick.AddListener(()=>{world.SetOverview(true);overview.GetComponentInChildren<Text>().text="OVERVIEW  •";follow.GetComponentInChildren<Text>().text="FOLLOW";});
        follow.onClick.AddListener(()=>{world.SetOverview(false);overview.GetComponentInChildren<Text>().text="OVERVIEW";follow.GetComponentInChildren<Text>().text="FOLLOW  •";});
        Place(overview.GetComponent<RectTransform>(),-88,94,164,44);Place(follow.GetComponent<RectTransform>(),88,94,164,44);
        var sound=Button(panel.transform''')
s=s.replace('0,62,340,40','0,30,340,40').replace('0,10,340,40','0,-22,340,40').replace('0,-42,340,40','0,-74,340,40').replace('0,-110,180,44','0,-160,180,44'); p.write_text(s,encoding="utf-8")
p=Path('Assets/Editor/BoardReview.cs'); s=p.read_text(encoding="utf-8").replace('{2340,1080}, {1024,768}','{2532,1170}, {2340,1080}, {1024,768}')
s=s.replace('step <= 8','step <= sizes.GetLength(0)*2').replace('step==9','step==sizes.GetLength(0)*2+1').replace('step==10','step==sizes.GetLength(0)*2+2').replace('step==11','step==sizes.GetLength(0)*2+3')
s=s.replace('                if(world.Overview.targetTexture==null||!world.Overview.targetTexture.IsCreated())throw new Exception("Live minimap unavailable");','                if(GameObject.Find("Live minimap")!=null)throw new Exception("Minimap was not removed");')
s=s.replace('follow camera and live minimap','follow camera; minimap removed')
needle='                    retries=0;'
s=s.replace(needle,needle+'''
                    var camera=UnityEngine.Object.FindObjectOfType<BoardWorld>().View;
                    if(camera.rect.x!=0||camera.rect.width!=1)throw new Exception("Camera does not fill screen width");
                    for(int sample=0;sample<96;sample++)
                    {
                        var edge=BoardTrackGenerator.Point(sample/96f,267);
                        var projected=camera.WorldToViewportPoint(new Vector3(edge.x*.01f,.16f,edge.y*.01f));
                        if(projected.z<=0||projected.x<0||projected.x>1||projected.y<0||projected.y>1)throw new Exception("Board clipped at "+Screen.width+"x"+Screen.height);
                    }
''',1)
p.write_text(s,encoding="utf-8")

