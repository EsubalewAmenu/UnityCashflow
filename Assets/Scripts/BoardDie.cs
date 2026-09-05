using UnityEngine;
using System.Collections.Generic;

/// <summary>The uppermost physical face supplies the gameplay value.</summary>
public sealed class BoardDie : MonoBehaviour
{
    Rigidbody body;
    Mesh roundedMesh;
    PhysicMaterial physics;
    public int Value { get; private set; }
    static readonly Vector3[] normals={Vector3.up,Vector3.right,Vector3.forward,Vector3.back,Vector3.left,Vector3.down};
    public bool Settled => body.IsSleeping() || (body.velocity.sqrMagnitude<.002f && body.angularVelocity.sqrMagnitude<.02f);
    public void Build(Material ivory,Material ink)
    {
        var cube=new GameObject("Rounded ivory die",typeof(MeshFilter),typeof(MeshRenderer),typeof(BoxCollider));cube.transform.SetParent(transform,false);cube.GetComponent<Renderer>().sharedMaterial=ivory;
        roundedMesh=RoundedCube();cube.GetComponent<MeshFilter>().sharedMesh=roundedMesh;
        var collider=cube.GetComponent<BoxCollider>();collider.size=Vector3.one*.42f;physics=new PhysicMaterial("Dice bounce"){bounciness=.3f,dynamicFriction=.6f,staticFriction=.65f};collider.material=physics;
        body=gameObject.AddComponent<Rigidbody>();body.mass=.08f;body.interpolation=RigidbodyInterpolation.Interpolate;body.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;body.angularDrag=.3f;body.isKinematic=true;
        for(int face=0;face<6;face++)
        {
            int number=face+1;Quaternion rotation=Quaternion.FromToRotation(Vector3.forward,normals[face]);
            for(int row=-1;row<=1;row++)for(int col=-1;col<=1;col++)
            {
                bool draw=(number%2==1 && row==0&&col==0) || (number>=2&&row==-1&&col==-1) || (number>=2&&row==1&&col==1) || (number>=4&&row==-1&&col==1) || (number>=4&&row==1&&col==-1) || (number==6&&row==0&&col!=0);
                if(!draw)continue;
                var pip=GameObject.CreatePrimitive(PrimitiveType.Sphere);pip.name="Pip";pip.transform.SetParent(transform,false);pip.transform.localPosition=rotation*new Vector3(col*.11f,row*.11f,.21f);pip.transform.localRotation=rotation;pip.transform.localScale=new Vector3(.062f,.062f,.012f);pip.GetComponent<Renderer>().sharedMaterial=ink;Destroy(pip.GetComponent<Collider>());
            }
        }
    }
    static Mesh RoundedCube()
    {
        var vertices=new List<Vector3>();var ns=new List<Vector3>();var indices=new List<int>();const int steps=8;
        foreach(var face in normals)
        {
            Quaternion rotation=Quaternion.FromToRotation(Vector3.forward,face);int start=vertices.Count;
            for(int y=0;y<=steps;y++)for(int x=0;x<=steps;x++)
            {
                Vector3 v=rotation*new Vector3((x/(float)steps-.5f)*.42f,(y/(float)steps-.5f)*.42f,.21f);
                Vector3 q=new Vector3(Mathf.Clamp(v.x,-.17f,.17f),Mathf.Clamp(v.y,-.17f,.17f),Mathf.Clamp(v.z,-.17f,.17f));
                Vector3 n=(v-q).normalized;vertices.Add(q+n*.04f);ns.Add(n);
            }
            for(int y=0;y<steps;y++)for(int x=0;x<steps;x++){int a=start+y*(steps+1)+x,b=a+1,c=a+steps+1,d=c+1;indices.AddRange(new[]{a,b,c,b,d,c});}
        }
        var mesh=new Mesh{name="Rounded die"};mesh.SetVertices(vertices);mesh.SetNormals(ns);mesh.SetTriangles(indices,0);mesh.RecalculateBounds();return mesh;
    }
    public void Toss(Vector3 position,bool reduced)
    {
        body.isKinematic=false;transform.position=position;transform.rotation=Random.rotationUniform;
        body.velocity=new Vector3(Random.Range(-.3f,.3f),reduced?0:1.6f,Random.Range(-.2f,.2f));body.angularVelocity=Random.onUnitSphere*(reduced?2:14);body.WakeUp();
    }
    public void Finish()
    {
        int face=0;float max=-1;
        for(int i=0;i<normals.Length;i++){float dot=Vector3.Dot(transform.TransformDirection(normals[i]),Vector3.up);if(dot>max){max=dot;face=i;}}
        Value=face+1;body.velocity=Vector3.zero;body.angularVelocity=Vector3.zero;body.isKinematic=true;
        // Resolve an exceptionally cocked die to the same visible upper face.
        transform.rotation=Quaternion.FromToRotation(transform.TransformDirection(normals[face]),Vector3.up)*transform.rotation;
        transform.position=new Vector3(Mathf.Clamp(transform.position.x,-1.4f,1.4f),.36f,Mathf.Clamp(transform.position.z,-.7f,.5f));
    }
    void OnDestroy(){if(roundedMesh!=null)Destroy(roundedMesh);if(physics!=null)Destroy(physics);}
}
