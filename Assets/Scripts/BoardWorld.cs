using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>Physical presentation of the board. Financial decisions remain in CashflowGame.</summary>
public sealed class BoardWorld : MonoBehaviour
{
    public Camera View { get; private set; }
    public Camera Overview { get; private set; }
    public RenderTexture OverviewTexture { get; private set; }
    public Transform Pawn { get; private set; }
    public int DiceTotal { get; private set; }
    public bool Follow { get; private set; }
    public bool Busy { get; private set; }
    public bool Dragged => moved;
    public bool Sound = true;
    public bool ReducedMotion;
    public bool InputBlocked;
    readonly List<Material> materials = new List<Material>();
    readonly List<BoardDie> dice = new List<BoardDie>();
    Transform scenery; Material boardInk;
    Vector3 focus;
    float yaw, pitch = 55, zoom = 1;
    Vector2 pointer;
    bool dragging, moved;
    AudioSource audioSource;
    AudioClip clack;

    public static Vector3 TrackPoint(int index, bool fast)
    {
        Vector2 p=BoardTrackGenerator.Point(index/24f,fast?138:222);
        return new Vector3(p.x*.01f,.16f,p.y*.01f);
    }

    public void Initialize(RectTransform face)
    {
        BoardTrackGenerator.Straight=328;
        // Clear the entire display, including the HUD area outside the 3D viewport.
        // A partial camera rect alone leaves old UI pixels behind after resizing.
        var backdrop=new GameObject("Full display clear").AddComponent<Camera>();backdrop.transform.SetParent(transform,false);
        backdrop.depth=-100;backdrop.cullingMask=0;backdrop.clearFlags=CameraClearFlags.SolidColor;backdrop.backgroundColor=Hex("#2d2450");
        View=Camera.main;
        View.depth=0;
        View.orthographic=false; View.fieldOfView=39; View.nearClipPlane=.05f; View.farClipPlane=150;
        View.clearFlags=CameraClearFlags.SolidColor; View.backgroundColor=Hex("#b6d6df"); View.allowMSAA=true;
        QualitySettings.antiAliasing=4;
        if(View.GetComponent<AudioListener>()==null)View.gameObject.AddComponent<AudioListener>();
        var sunlight=new GameObject("Soft afternoon light").AddComponent<Light>(); sunlight.transform.SetParent(transform,false);
        sunlight.type=LightType.Directional; sunlight.intensity=1.15f; sunlight.color=new Color(1,.95f,.85f);
        sunlight.shadows=LightShadows.Soft; sunlight.shadowStrength=.4f; sunlight.transform.rotation=Quaternion.Euler(48,-35,0);
        RenderSettings.ambientLight=new Color(.62f,.67f,.73f);
        QualitySettings.shadows=ShadowQuality.All; QualitySettings.shadowDistance=40;
        var surface=new GameObject("Printed board surface",typeof(RectTransform),typeof(Canvas),typeof(GraphicRaycaster));
        surface.transform.SetParent(transform,false);
        var canvas=surface.GetComponent<Canvas>(); canvas.renderMode=RenderMode.WorldSpace; canvas.worldCamera=View;
        var rect=surface.GetComponent<RectTransform>(); rect.sizeDelta=new Vector2(1280,540);
        rect.position=new Vector3(0,.151f,0); rect.rotation=Quaternion.Euler(90,0,0); rect.localScale=Vector3.one*.01f;
        face.SetParent(rect,false); face.anchoredPosition=Vector2.zero; face.localScale=Vector3.one;
        Box("Board edge",Vector3.zero,new Vector3(11.85f,.28f,5.25f),Mat("#302746",.2f,.4f));
        Box("Table",new Vector3(0,-.35f,0),new Vector3(30,.4f,22),Mat("#8fab9a"));
        BuildScenery(); BuildDecks(); BuildPawn();
        for(int i=0;i<2;i++) { var d=new GameObject("Die "+(i+1)).AddComponent<BoardDie>(); d.transform.SetParent(transform,false); d.Build(Mat("#fff9e9",0,.32f),Mat("#20283a",.1f,.2f)); d.transform.position=new Vector3((i==0?-.38f:.38f),.4f,-.3f); dice.Add(d); }
        Overview=new GameObject("Live overview camera").AddComponent<Camera>(); Overview.transform.SetParent(transform,false);
        Overview.transform.position=new Vector3(0,14,0); Overview.transform.rotation=Quaternion.Euler(90,0,0);
        Overview.orthographic=true; Overview.orthographicSize=2.9f; Overview.clearFlags=CameraClearFlags.SolidColor; Overview.backgroundColor=Hex("#6e9d78");
        OverviewTexture=new RenderTexture(640,320,16){name="Live board overview",antiAliasing=2}; Overview.targetTexture=OverviewTexture;
        audioSource=gameObject.AddComponent<AudioSource>(); audioSource.spatialBlend=0;
        clack=AudioClip.Create("Dice clack",2205,1,22050,false); var samples=new float[2205];
        for(int i=0;i<samples.Length;i++)samples[i]=Mathf.Sin(i*1.17f)*Mathf.Exp(-i/180f)*.25f; clack.SetData(samples,0);
        SetOverview(true); UpdateCamera(true);
    }

    Material Mat(string hex,float metal=0,float gloss=.1f)
    {
        var m=new Material(Shader.Find("Standard")); m.color=Hex(hex); m.SetFloat("_Metallic",metal);m.SetFloat("_Glossiness",gloss);materials.Add(m);return m;
    }
    static Color Hex(string value){ColorUtility.TryParseHtmlString(value,out var c);return c;}
    GameObject Shape(string name,PrimitiveType type,Transform parent,Vector3 p,Vector3 scale,Material mat,bool collision=false)
    {
        var go=GameObject.CreatePrimitive(type);go.name=name;go.transform.SetParent(parent,false);go.transform.localPosition=p;go.transform.localScale=scale;go.GetComponent<Renderer>().sharedMaterial=mat;
        if(!collision)Destroy(go.GetComponent<Collider>());return go;
    }
    GameObject Box(string name,Vector3 p,Vector3 size,Material mat){return Shape(name,PrimitiveType.Cube,transform,p,size,mat,true);}
    void BuildPawn()
    {
        Pawn=new GameObject("Silver player pawn").transform;Pawn.SetParent(transform,false);
        var silver=Mat("#d3dce2",.82f,.78f);var gold=Mat("#eec76c",.65f,.6f);
        Shape("Base",PrimitiveType.Cylinder,Pawn,new Vector3(0,.07f,0),new Vector3(.42f,.07f,.42f),silver);
        Shape("Gold base ring",PrimitiveType.Cylinder,Pawn,new Vector3(0,.15f,0),new Vector3(.35f,.025f,.35f),gold);
        Shape("Body",PrimitiveType.Capsule,Pawn,new Vector3(0,.36f,0),new Vector3(.21f,.24f,.21f),silver);
        Shape("Collar",PrimitiveType.Cylinder,Pawn,new Vector3(0,.51f,0),new Vector3(.22f,.025f,.22f),gold);
        Shape("Head",PrimitiveType.Sphere,Pawn,new Vector3(0,.66f,0),Vector3.one*.26f,silver);
        Pawn.position=TrackPoint(0,false);
    }
    void BuildDecks()
    {
        string[] names={"OPPORTUNITY","MARKET","EXPENSE","CHARITY"}; string[] colors={"#d94b39","#258ac3","#df9c34","#409a61"};
        for(int i=0;i<4;i++)
        {
            float x=i<2?-2.55f:2.55f;float z=i%2==0?.53f:-.53f;
            for(int j=0;j<5;j++)Box(names[i]+" card "+j,new Vector3(x,.18f+j*.025f,z),new Vector3(.75f,.025f,.87f),Mat(j==4?colors[i]:"#f9edda"));
            WorldText(names[i],new Vector3(x,.302f,z-.21f),.085f,Color.white);
            WorldText(i==0?"+":i==1?"M":i==2?"$":"G",new Vector3(x,.303f,z+.08f),.27f,Hex("#ffe5a8"));
        }
    }
    void WorldText(string text,Vector3 point,float height,Color color)
    {
        var go=new GameObject(text);go.transform.SetParent(transform,false);go.transform.position=point;
        var tx=go.AddComponent<TextMesh>();tx.text=text;tx.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");tx.fontSize=64;tx.characterSize=.1f;tx.anchor=TextAnchor.MiddleCenter;tx.alignment=TextAlignment.Center;tx.color=color;
        go.GetComponent<Renderer>().sharedMaterial=tx.font.material;
        float h=go.GetComponent<Renderer>().bounds.size.y;if(h>.001f)go.transform.localScale=Vector3.one*height/h;
        go.transform.rotation=Quaternion.Euler(90,0,0);
    }
    void BuildScenery()
    {
        scenery=new GameObject("City surroundings").transform;scenery.SetParent(transform,false);
        var road=Mat("#72828a");var green=Mat("#579a61");var wall=Mat("#d9dfc8");var roof=Mat("#ca8566");
        Shape("North avenue",PrimitiveType.Cube,scenery,new Vector3(0,-.12f,4.1f),new Vector3(22,.03f,.7f),road);
        Shape("South avenue",PrimitiveType.Cube,scenery,new Vector3(0,-.12f,-4.1f),new Vector3(22,.03f,.7f),road);
        for(int i=0;i<14;i++)
        {
            float x=-9+i*1.4f,z=i%2==0?5.5f:-5.5f;
            Shape("Townhouse",PrimitiveType.Cube,scenery,new Vector3(x,.35f,z),new Vector3(.8f,1+(i%3)*.2f,.8f),wall);
            Shape("Roof",PrimitiveType.Cube,scenery,new Vector3(x,.9f+(i%3)*.1f,z),new Vector3(.9f,.12f,.9f),roof);
            Shape("Tree",PrimitiveType.Sphere,scenery,new Vector3(x+.6f,.4f,z-.6f),new Vector3(.55f,.8f,.55f),green);
        }
    }
    public void ApplyPrint(Transform board){if(boardInk==null){boardInk=new Material(Shader.Find("Cashflow/Printed Board"));materials.Add(boardInk);}foreach(var g in board.GetComponentsInChildren<Graphic>())g.material=g is Text?null:boardInk;}
    public void ToggleScenery(){scenery.gameObject.SetActive(!scenery.gameObject.activeSelf);}
    public void SetOverview(bool overview){Follow=!overview;zoom=1;yaw=overview?0:-18;pitch=overview?57:43;}
    public void Orbit(float degrees){yaw+=degrees;}
    public void Zoom(float amount){zoom=Mathf.Clamp(zoom+amount,.7f,1.7f);}
    bool OverHUD()
    {
        if(EventSystem.current==null)return false;var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=Input.mousePosition},hits);
        foreach(var hit in hits){var canvas=hit.gameObject.GetComponentInParent<Canvas>();if(canvas!=null&&canvas.renderMode!=RenderMode.WorldSpace)return true;}return false;
    }
    void Update()
    {
        if(InputBlocked || Busy)return;
        if(Input.GetMouseButtonDown(0)) { dragging=!OverHUD();pointer=Input.mousePosition;moved=false; }
        if(dragging&&Input.GetMouseButton(0))
        {
            Vector2 now=Input.mousePosition;Vector2 d=now-pointer;
            if(d.magnitude>2||moved){yaw+=d.x*.15f;pitch=Mathf.Clamp(pitch-d.y*.12f,30,82);moved=true;}
            pointer=now;
        }
        if(Input.GetMouseButtonUp(0))dragging=false;
        if(Input.touchCount==2)
        {
            var a=Input.GetTouch(0);var b=Input.GetTouch(1);
            float old=Vector2.Distance(a.position-a.deltaPosition,b.position-b.deltaPosition);
            Zoom((old-Vector2.Distance(a.position,b.position))*.002f);
        }
        if(Input.mouseScrollDelta.y!=0)Zoom(-Input.mouseScrollDelta.y*.08f);
    }
    void LateUpdate(){if(View!=null)UpdateCamera(ReducedMotion);}
    void UpdateCamera(bool instant)
    {
        Rect safe=Screen.safeArea;
        if(safe.width<=0||safe.height<=0||safe.xMax>Screen.width||safe.yMax>Screen.height)safe=new Rect(0,0,Screen.width,Screen.height);
        View.rect=new Rect(safe.x/Screen.width,safe.y/Screen.height+.14f*safe.height/Screen.height,safe.width/Screen.width,.86f*safe.height/Screen.height);
        Vector3 target=Busy?Vector3.zero:Follow?Pawn.position+new Vector3(0,0,.35f):Vector3.zero;
        focus=instant?target:Vector3.Lerp(focus,target,1-Mathf.Exp(-Time.unscaledDeltaTime*5));
        float distance=Busy?6.8f:Follow?5.8f:Mathf.Max(9.7f,6.4f/(Mathf.Tan(View.fieldOfView*Mathf.Deg2Rad/2)*View.aspect));
        Vector3 offset=Quaternion.Euler(pitch,yaw,0)*new Vector3(0,0,-distance*zoom);
        Vector3 desired=focus+offset;
        View.transform.position=instant?desired:Vector3.Lerp(View.transform.position,desired,1-Mathf.Exp(-Time.unscaledDeltaTime*6));
        View.transform.LookAt(focus);
    }
    public void PlacePawn(int p,bool fast){Pawn.position=TrackPoint(p,fast);}
    public IEnumerator MovePawn(int from,int steps,bool fast)
    {
        for(int i=1;i<=steps;i++)
        {
            Vector3 a=Pawn.position,b=TrackPoint((from+i)%24,fast);
            float duration=ReducedMotion?.025f:.18f;
            for(float t=0;t<duration;t+=Time.deltaTime){float u=t/duration;Pawn.position=Vector3.Lerp(a,b,u)+Vector3.up*Mathf.Sin(u*Mathf.PI)*.13f;yield return null;}
            Pawn.position=b;
        }
    }
    public IEnumerator ThrowDice(int count)
    {
        Busy=true;DiceTotal=0;
        for(int i=0;i<dice.Count;i++)
        {
            dice[i].gameObject.SetActive(i<count);
            if(i<count)dice[i].Toss(new Vector3((i==0?-.38f:.38f),1.6f,-.3f),ReducedMotion);
        }
        float elapsed=0;
        while(elapsed<5)
        {
            elapsed+=Time.deltaTime;yield return null;
            bool settled=elapsed>.8f;
            for(int i=0;i<count;i++)settled &= dice[i].Settled;
            if(settled)break;
        }
        for(int i=0;i<count;i++){dice[i].Finish();DiceTotal+=dice[i].Value;}
        if(Sound)audioSource.PlayOneShot(clack);
        yield return new WaitForSeconds(ReducedMotion?.1f:.4f);Busy=false;
    }
    void OnDestroy()
    {
        foreach(var m in materials)if(m!=null)Destroy(m);
        if(OverviewTexture!=null){OverviewTexture.Release();Destroy(OverviewTexture);}
        if(clack!=null)Destroy(clack);
    }
}
