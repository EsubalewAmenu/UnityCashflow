from pathlib import Path
p=Path('Assets/Scripts/BoardWorld.cs'); s=p.read_text()
s=s.replace('    public Camera Overview { get; private set; }\n    public RenderTexture OverviewTexture { get; private set; }\n','')
a=s.index('        Overview=new GameObject'); b=s.index('        audioSource=',a); s=s[:a]+s[b:]
s=s.replace('        if(OverviewTexture!=null){OverviewTexture.Release();Destroy(OverviewTexture);}\n','')
a=s.index('        Rect safe=Screen.safeArea;',s.index('void UpdateCamera')); b=s.index('        Vector3 target=',a)
s=s[:a]+'''        // Render scenery edge to edge; only interactive HUD elements use the safe area.
        View.rect=new Rect(0,.14f,1,.86f);
'''+s[b:]
s=s.replace('Mathf.Max(9.7f,6.4f/(Mathf.Tan(View.fieldOfView*Mathf.Deg2Rad/2)*View.aspect))','OverviewDistance()')
a=s.index('    public void PlacePawn')
s=s[:a]+'''    float OverviewDistance()
    {
        // Fit the actual curved perimeter in perspective at any aspect and orbit angle.
        float tanV=Mathf.Tan(View.fieldOfView*Mathf.Deg2Rad/2)*.94f;
        float tanH=tanV*View.aspect;
        Quaternion inverse=Quaternion.Inverse(Quaternion.Euler(pitch,yaw,0));
        float distance=0;
        for(int i=0;i<96;i++)
        {
            Vector2 edge=BoardTrackGenerator.Point(i/96f,267);
            Vector3 p=inverse*new Vector3(edge.x*.01f,.16f,edge.y*.01f);
            distance=Mathf.Max(distance,Mathf.Abs(p.x)/tanH-p.z,Mathf.Abs(p.y)/tanV-p.z);
        }
        return distance;
    }
'''+s[a:]; p.write_text(s)
p=Path('Assets/Scripts/CashflowGame.cs'); s=p.read_text(); a=s.index('        string[] controls='); b=s.index('\n    }\n    void ShowBoardSettings',a)
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
s=s.replace('0,62,340,40','0,30,340,40').replace('0,10,340,40','0,-22,340,40').replace('0,-42,340,40','0,-74,340,40').replace('0,-110,180,44','0,-160,180,44'); p.write_text(s)
p=Path('Assets/Editor/BoardReview.cs'); s=p.read_text().replace('{2340,1080}, {1024,768}','{2532,1170}, {2340,1080}, {1024,768}')
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
p.write_text(s)
