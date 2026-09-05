using UnityEngine;
using UnityEngine.UI;

/// <summary>The board perimeter follows the track, leaving no rectangular corner wedges.</summary>
[RequireComponent(typeof(CanvasRenderer))]
public sealed class BoardPlateGraphic : MaskableGraphic
{
    public float radius=263;
    const int Steps=192;
    protected override void OnPopulateMesh(VertexHelper h)
    {
        h.Clear();h.AddVert(Vector3.zero,color,Vector2.zero);
        for(int i=0;i<=Steps;i++)
        {
            h.AddVert(BoardTrackGenerator.Point(i/(float)Steps,radius),color,Vector2.zero);
            if(i>0)h.AddTriangle(0,i,i+1);
        }
    }
    public static Mesh Solid(float radius,float thickness)
    {
        var vertices=new Vector3[Steps*4+2];var indices=new int[Steps*12];
        vertices[0]=Vector3.up*thickness/2;vertices[1]=Vector3.down*thickness/2;
        for(int i=0;i<Steps;i++)
        {
            Vector2 p=BoardTrackGenerator.Point(i/(float)Steps,radius)*.01f;
            vertices[2+i*4]=new Vector3(p.x,thickness/2,p.y);
            vertices[3+i*4]=new Vector3(p.x,-thickness/2,p.y);
            vertices[4+i*4]=vertices[2+i*4];vertices[5+i*4]=vertices[3+i*4];
        }
        for(int i=0;i<Steps;i++)
        {
            int a=2+i*4,b=2+((i+1)%Steps)*4,k=i*12;
            indices[k]=0;indices[k+1]=b;indices[k+2]=a;
            indices[k+3]=1;indices[k+4]=a+1;indices[k+5]=b+1;
            indices[k+6]=a+2;indices[k+7]=b+2;indices[k+8]=a+3;
            indices[k+9]=a+3;indices[k+10]=b+2;indices[k+11]=b+3;
        }
        var mesh=new Mesh{name="Track-shaped board base"};mesh.vertices=vertices;mesh.triangles=indices;mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
    }
}
