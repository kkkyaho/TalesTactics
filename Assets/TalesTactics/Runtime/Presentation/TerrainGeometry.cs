using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace TalesTactics
{
    // One mesh per material. Decorative meshes have no collider and never consume combat RNG.
    internal sealed class TerrainGeometry
    {
        readonly List<Vector3> vertices=new List<Vector3>();
        readonly List<Vector2> uv=new List<Vector2>();
        readonly List<Color> colors=new List<Color>();
        readonly List<int> triangles=new List<int>();
        public void Quad(Vector3 a,Vector3 b,Vector3 c,Vector3 d,float shade=1,Vector2 repeat=default)
        {
            int n=vertices.Count;vertices.Add(a);vertices.Add(b);vertices.Add(c);vertices.Add(d);
            if(repeat==default)repeat=Vector2.one*.998f;
            uv.Add(new Vector2(.001f,.001f));uv.Add(new Vector2(.001f,repeat.y));uv.Add(repeat);uv.Add(new Vector2(repeat.x,.001f));
            for(int i=0;i<4;i++)colors.Add(new Color(shade,shade,shade,1));
            triangles.Add(n);triangles.Add(n+1);triangles.Add(n+2);triangles.Add(n);triangles.Add(n+2);triangles.Add(n+3);
        }
        public void Box(Vector3 center,Vector3 size,float shade=1,Quaternion rotation=default)
        {
            if(rotation==default)rotation=Quaternion.identity;
            var p=new Vector3[8];
            for(int i=0;i<8;i++)p[i]=center+rotation*Vector3.Scale(size*.5f,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1));
            Quad(p[2],p[6],p[7],p[3],shade,new Vector2(size.x,size.z));Quad(p[0],p[1],p[5],p[4],shade*.68f);
            Quad(p[0],p[2],p[3],p[1],shade,new Vector2(size.x,size.y));Quad(p[5],p[7],p[6],p[4],shade,new Vector2(size.x,size.y));
            Quad(p[4],p[6],p[2],p[0],shade,new Vector2(size.z,size.y));Quad(p[1],p[3],p[7],p[5],shade,new Vector2(size.z,size.y));
        }
        public void Panel(Vector3 a,Vector3 b,Vector3 c,Vector3 d,float u0,float u1)
        {
            Quad(a,b,c,d);int n=uv.Count-4;
            uv[n]=new Vector2(u0,.001f);uv[n+1]=new Vector2(u0,.999f);
            uv[n+2]=new Vector2(u1,.999f);uv[n+3]=new Vector2(u1,.001f);
        }
        public void Arch(Vector3 center,Vector3 across,float inner,float outer,float depth)
        {
            var normal=Vector3.Cross(across,Vector3.up).normalized*depth*.5f;
            for(int i=0;i<12;i++)
            {
                float a=i*Mathf.PI/12+.012f,b=(i+1)*Mathf.PI/12-.012f;
                Vector3 P(float angle,float radius)=>center+(across*Mathf.Cos(angle)+Vector3.up*Mathf.Sin(angle))*radius;
                var p=P(a,inner);var q=P(b,inner);var r=P(b,outer);var s=P(a,outer);
                Quad(p-normal,q-normal,r-normal,s-normal);Quad(s+normal,r+normal,q+normal,p+normal);
                Quad(q-normal,p-normal,p+normal,q+normal,.8f);Quad(s-normal,r-normal,r+normal,s+normal);
                Quad(p-normal,s-normal,s+normal,p+normal);Quad(r-normal,q-normal,q+normal,r+normal);
            }
        }
        public void Rock(Vector3 foot,float radius,float height,int seed)
        {
            const int sides=9,layers=7;
            Vector3 P(int side,int layer)
            {
                side%=sides;
                float angle=side*Mathf.PI*2/sides;
                float variation=.81f+.12f*Mathf.Sin(seed*.87f+side*3.7f)+.10f*Mathf.Cos(layer*1.7f+seed);
                float taper=1-.16f*layer/layers;
                return foot+new Vector3(Mathf.Cos(angle)*radius*variation*taper,height*layer/layers,Mathf.Sin(angle)*radius*variation*taper);
            }
            for(int j=0;j<layers;j++)for(int i=0;i<sides;i++)Quad(P(i,j),P(i,j+1),P(i+1,j+1),P(i+1,j),.90f+((i+seed)%3)*.035f,new Vector2(.7f,height/layers));
            for(int i=0;i<sides;i++)Quad(foot+Vector3.up*height,P(i+1,layers),P(i,layers),foot+Vector3.up*height);
        }
        public Mesh Flush(Transform parent,string name,Material material)
        {
            if(vertices.Count==0)return null;
            var mesh=new Mesh{name=name,indexFormat=vertices.Count>65535?IndexFormat.UInt32:IndexFormat.UInt16};
            mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetColors(colors);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
            var g=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));g.transform.SetParent(parent,false);
            g.GetComponent<MeshFilter>().sharedMesh=mesh;var renderer=g.GetComponent<MeshRenderer>();renderer.sharedMaterial=material;
            renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
            return mesh;
        }
    }
}
