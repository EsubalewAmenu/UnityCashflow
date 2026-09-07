using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>A compact, batched city diorama framing the board on every camera angle.</summary>
public sealed class BoardEnvironment : MonoBehaviour
{
    sealed class Batch
    {
        public readonly List<Vector3> vertices=new List<Vector3>();
        public readonly List<int> triangles=new List<int>();
    }
    readonly Dictionary<string,Batch> batches=new Dictionary<string,Batch>();
    readonly List<Object> owned=new List<Object>();
    Vector3 origin;
    Quaternion rotation=Quaternion.identity;
    const string Stone="#ded4c4", Curb="#f2e8d8", Road="#596574", Glass="#436679", Trim="#fff1d8", Metal="#3d4054";

    public void Build()
    {
        Box(Vector3.down*.39f,new Vector3(70,.4f,70),"#b6b0c1");
        Box(new Vector3(0,-.16f,0),new Vector3(12.8f,.09f,6.1f),Stone);
        // Fine, shallow paving joints read as stone rather than an empty green plane.
        for(float x=-6.3f;x<=6.3f;x+=.45f)Box(new Vector3(x,-.109f,0),new Vector3(.013f,.003f,6.0f),"#bfb4a4");
        for(float z=-3;z<=3;z+=.45f)Box(new Vector3(0,-.109f,z),new Vector3(12.7f,.003f,.013f),"#bfb4a4");
        for(int sign=-1;sign<=1;sign+=2)
        {
            float z=sign*3.65f;
            Box(new Vector3(0,-.17f,z),new Vector3(34,.045f,1.25f),Road);
            Box(new Vector3(0,-.115f,sign*2.97f),new Vector3(12.9f,.1f,.15f),Curb);
            Box(new Vector3(0,-.105f,sign*4.4f),new Vector3(34,.11f,.25f),Curb);
            for(float x=-16;x<17;x+=.65f)Box(new Vector3(x,-.143f,z),new Vector3(.3f,.006f,.025f),Trim);
            float sx=sign*6.95f;
            Box(new Vector3(sx,-.17f,0),new Vector3(1.25f,.045f,34),Road);
            Box(new Vector3(sign*6.24f,-.11f,0),new Vector3(.15f,.1f,6.05f),Curb);
            Box(new Vector3(sign*7.7f,-.105f,0),new Vector3(.25f,.11f,34),Curb);
            for(float zz=-16;zz<17;zz+=.65f)Box(new Vector3(sx,-.141f,zz),new Vector3(.025f,.006f,.3f),Trim);
            for(int cross=-1;cross<=1;cross+=2)
            for(int i=0;i<7;i++)
            {
                Box(new Vector3(cross*5.83f,-.139f,z-.5f+i*.16f),new Vector3(.38f,.006f,.085f),Curb);
                Box(new Vector3(sx-.5f+i*.16f,-.138f,cross*2.64f),new Vector3(.085f,.006f,.38f),Curb);
            }
        }
        string[] walls={"#c37c67","#91b3bb","#d3af77","#b59faf","#9eafc1","#ce9584","#aeaaa0","#d5c6a4"};
        for(int i=0;i<8;i++)
        {
            float x=-5.3f+i*1.5f;
            Building(new Vector3(x,-.12f,5.13f),0,1.15f,1.05f+(i%3)*.24f,1.12f,walls[i],i);
            Building(new Vector3(x,-.12f,-5.13f),180,1.15f,.8f+(i%3)*.15f,1.12f,walls[7-i],i+2);
        }
        for(int side=-1;side<=1;side+=2)
        for(int i=0;i<6;i++)Building(new Vector3(side*8.4f,-.12f,-4.2f+i*1.7f),side*90,1.28f,1.05f+(i%3)*.24f,1.15f,walls[(i+3)%8],i);
        // Rear blocks give the streets depth, instead of isolated primitive houses.
        for(int i=0;i<11;i++)
        {
            Building(new Vector3(-9+i*1.8f,-.15f,7.5f),0,1.5f,1.7f+(i%4)*.35f,1.4f,walls[(i+1)%8],i+1);
            Building(new Vector3(-9+i*1.8f,-.15f,-7.5f),180,1.5f,1.4f+(i%4)*.27f,1.4f,walls[(i+4)%8],i+2);
        }
        origin=Vector3.zero;rotation=Quaternion.identity;
        for(int side=-1;side<=1;side+=2)
        {
            for(int i=0;i<5;i++)
            {
                float x=-5.3f+i*2.65f;
                Lamp(new Vector3(x,-.1f,side*4.35f));
                Bench(new Vector3(x+.6f,-.1f,side*4.36f));
            }
            Car(new Vector3(-4.35f,-.13f,side*3.37f),90,side>0?"#c65f53":"#e4bf70");
            Car(new Vector3(2.75f,-.13f,side*3.93f),-90,side>0?"#749faf":"#ebe5cf");
            Car(new Vector3(side*7.23f,-.13f,1.3f),0,"#b890aa");
        }
        origin=Vector3.zero;rotation=Quaternion.identity;
        Bake();
    }
    void Building(Vector3 p,float angle,float width,float height,float depth,string wall,int variation)
    {
        origin=p;rotation=Quaternion.Euler(0,angle,0);
        Box(new Vector3(0,-.015f,0),new Vector3(width+.16f,.05f,depth+.15f),Stone);
        Box(new Vector3(0,height/2,0),new Vector3(width,height,depth),wall);
        Box(new Vector3(0,.09f,-depth/2-.018f),new Vector3(width,.17f,.045f),Trim);
        Box(new Vector3(0,height+.025f,0),new Vector3(width+.11f,.075f,depth+.1f),Trim);
        int floors=Mathf.Max(1,Mathf.FloorToInt(height/.42f));
        for(int floor=0;floor<floors;floor++)
        {
            float y=.28f+floor*.41f;
            for(int col=-1;col<=1;col++)
            {
                float x=col*width*.27f;
                Box(new Vector3(x,y,-depth/2-.027f),new Vector3(.23f,.29f,.032f),Trim);
                Box(new Vector3(x,y,-depth/2-.047f),new Vector3(.185f,.245f,.012f),Glass);
                Box(new Vector3(x,y,-depth/2-.055f),new Vector3(.018f,.245f,.01f),Trim);
                Box(new Vector3(x,y-.155f,-depth/2-.05f),new Vector3(.26f,.03f,.075f),Trim);
                Box(new Vector3(width/2+.018f,y,col*depth*.26f),new Vector3(.03f,.25f,.19f),Glass);
            }
            if(floor>0)Box(new Vector3(0,y-.2f,-depth/2-.025f),new Vector3(width,.024f,.06f),Trim);
        }
        Box(new Vector3(0,.16f,-depth/2-.065f),new Vector3(.24f,.32f,.02f),Metal);
        Box(new Vector3(.065f,.16f,-depth/2-.079f),new Vector3(.015f,.055f,.015f),"#e0bd79");
        // Striped shop awnings and readable, modeled storefront fascia.
        string awning=variation%2==0?"#ad6557":"#657e92";
        Box(new Vector3(0,.43f,-depth/2-.115f),new Vector3(width*.87f,.055f,.27f),awning);
        for(int stripe=0;stripe<6;stripe++)Box(new Vector3(-width*.36f+stripe*width*.143f,.46f,-depth/2-.115f),new Vector3(width*.06f,.008f,.27f),Trim);
        if(variation%3==0)
        {
            Roof(width,depth,height+.07f,"#aa6c59");
            Box(new Vector3(width*.28f,height+.2f,.14f),new Vector3(.13f,.35f,.14f),wall);
        }
        else
        {
            Box(new Vector3(0,height+.08f,0),new Vector3(width-.07f,.07f,depth-.07f),"#747184");
            Box(new Vector3(width*.21f,height+.18f,.08f),new Vector3(.25f,.15f,.28f),"#9d9aa4");
        }
        origin=Vector3.zero;rotation=Quaternion.identity;
    }
    void Roof(float width,float depth,float y,string color)
    {
        float w=width/2+.07f,d=depth/2+.07f;
        Quad(new Vector3(-w,y,-d),new Vector3(-w,y+.25f,0),new Vector3(w,y+.25f,0),new Vector3(w,y,-d),color);
        Quad(new Vector3(-w,y+.25f,0),new Vector3(-w,y,d),new Vector3(w,y,d),new Vector3(w,y+.25f,0),color);
        Quad(new Vector3(-w,y,d),new Vector3(-w,y,-d),new Vector3(-w,y+.25f,0),new Vector3(-w,y+.25f,0),color);
        Quad(new Vector3(w,y,-d),new Vector3(w,y,d),new Vector3(w,y+.25f,0),new Vector3(w,y+.25f,0),color);
    }
    void Lamp(Vector3 p)
    {
        Box(p+new Vector3(0,.025f,0),new Vector3(.13f,.05f,.13f),Metal);
        Box(p+new Vector3(0,.35f,0),new Vector3(.038f,.7f,.038f),Metal);
        Box(p+new Vector3(0,.74f,0),new Vector3(.13f,.14f,.13f),"#fbe5af");
        Box(p+new Vector3(0,.83f,0),new Vector3(.18f,.035f,.18f),Metal);
    }
    void Bench(Vector3 p)
    {
        for(int x=-1;x<=1;x+=2)Box(p+new Vector3(x*.13f,.085f,0),new Vector3(.035f,.17f,.14f),Metal);
        for(int i=0;i<3;i++)Box(p+new Vector3(0,.18f,-.065f+i*.06f),new Vector3(.4f,.025f,.045f),"#a67c61");
        Box(p+new Vector3(0,.29f,.09f),new Vector3(.4f,.17f,.025f),"#a67c61");
    }
    void Car(Vector3 p,float angle,string paint)
    {
        origin=p;rotation=Quaternion.Euler(0,angle,0);
        Box(new Vector3(0,.13f,0),new Vector3(.26f,.14f,.55f),paint);
        Box(new Vector3(0,.23f,-.025f),new Vector3(.22f,.11f,.28f),Glass);
        Box(new Vector3(0,.29f,-.025f),new Vector3(.23f,.025f,.25f),paint);
        for(int side=-1;side<=1;side+=2)for(int end=-1;end<=1;end+=2)
            Box(new Vector3(side*.14f,.08f,end*.17f),new Vector3(.035f,.11f,.11f),Metal);
        Box(new Vector3(0,.14f,.277f),new Vector3(.2f,.035f,.015f),"#ffe9b5");
        Box(new Vector3(0,.13f,-.277f),new Vector3(.2f,.025f,.015f),"#ad5f61");
        origin=Vector3.zero;rotation=Quaternion.identity;
    }
    void Box(Vector3 center,Vector3 size,string color)
    {
        Vector3 a=center-size/2,b=center+size/2;
        Quad(new Vector3(a.x,b.y,a.z),new Vector3(a.x,b.y,b.z),new Vector3(b.x,b.y,b.z),new Vector3(b.x,b.y,a.z),color);
        Quad(new Vector3(a.x,a.y,b.z),new Vector3(a.x,a.y,a.z),new Vector3(b.x,a.y,a.z),new Vector3(b.x,a.y,b.z),color);
        Quad(new Vector3(a.x,a.y,a.z),new Vector3(a.x,b.y,a.z),new Vector3(b.x,b.y,a.z),new Vector3(b.x,a.y,a.z),color);
        Quad(new Vector3(b.x,a.y,b.z),new Vector3(b.x,b.y,b.z),new Vector3(a.x,b.y,b.z),new Vector3(a.x,a.y,b.z),color);
        Quad(new Vector3(a.x,a.y,b.z),new Vector3(a.x,b.y,b.z),new Vector3(a.x,b.y,a.z),new Vector3(a.x,a.y,a.z),color);
        Quad(new Vector3(b.x,a.y,a.z),new Vector3(b.x,b.y,a.z),new Vector3(b.x,b.y,b.z),new Vector3(b.x,a.y,b.z),color);
    }
    void Quad(Vector3 a,Vector3 b,Vector3 c,Vector3 d,string color)
    {
        if(!batches.TryGetValue(color,out var batch)){batch=new Batch();batches[color]=batch;}
        int n=batch.vertices.Count;batch.vertices.Add(origin+rotation*a);batch.vertices.Add(origin+rotation*b);batch.vertices.Add(origin+rotation*c);batch.vertices.Add(origin+rotation*d);
        batch.triangles.AddRange(new[]{n,n+1,n+2,n,n+2,n+3});
    }
    void Bake()
    {
        foreach(var entry in batches)
        {
            var mesh=new Mesh{name="City geometry "+entry.Key,indexFormat=IndexFormat.UInt32};mesh.SetVertices(entry.Value.vertices);mesh.SetTriangles(entry.Value.triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();owned.Add(mesh);
            var material=BoardMaterials.CreateSurface();ColorUtility.TryParseHtmlString(entry.Key,out var c);material.color=c;material.SetFloat("_Glossiness",entry.Key==Glass?.38f:.12f);owned.Add(material);
            var go=new GameObject("City finish "+entry.Key,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(transform,false);go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterial=material;
        }
        batches.Clear();
    }
    void OnDestroy(){foreach(var item in owned)if(item!=null)Destroy(item);}
}
