using System.Collections.Generic;
using UnityEngine;
namespace TalesTactics
{
    // Transparent geometry follows the ground without raised bases or internal tile outlines.
    public sealed class GroundOverlay:MonoBehaviour
    {
        Mesh mesh;MeshRenderer surface;float cornerRadius=-1;Color cornerColor;
        readonly List<Vector3> vertices=new List<Vector3>();
        readonly List<Color> colors=new List<Color>();
        readonly List<int> triangles=new List<int>();
        public int QuadCount=>triangles.Count/6;
        public void Initialize(Material material,int order=-2)
        {
            mesh=new Mesh{name=name};mesh.MarkDynamic();gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;
            surface=gameObject.AddComponent<MeshRenderer>();surface.sharedMaterial=material;surface.sortingOrder=order;
            surface.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;surface.receiveShadows=false;
        }
        public void Clear(){vertices.Clear();colors.Clear();triangles.Clear();}
        public void Quad(Vector3 a,Vector3 b,Vector3 c,Vector3 d,Color color)
        {
            int n=vertices.Count;vertices.Add(a);vertices.Add(b);vertices.Add(c);vertices.Add(d);
            for(int i=0;i<4;i++)colors.Add(color);
            triangles.Add(n);triangles.Add(n+1);triangles.Add(n+2);triangles.Add(n);triangles.Add(n+2);triangles.Add(n+3);
        }
        public void Line(Vector3 a,Vector3 b,float width,Color color)
        {var side=Vector3.Cross(Vector3.up,(b-a).normalized)*width*.5f;Quad(a-side,a+side,b+side,b-side,color);}
        public void Tile(Vector3 center,float radius,Color color)
        {Quad(center+new Vector3(-radius,0,-radius),center+new Vector3(-radius,0,radius),center+new Vector3(radius,0,radius),center+new Vector3(radius,0,-radius),color);}
        public void Corners(float radius,Color color)
        {
            if(cornerRadius==radius&&cornerColor==color)return;cornerRadius=radius;cornerColor=color;
            Clear();var tint=color;tint.a=.045f;Tile(Vector3.zero,radius,tint);
            foreach(int x in new[]{-1,1})foreach(int z in new[]{-1,1})
            {var p=new Vector3(x*radius,0,z*radius);Line(p-new Vector3(x*.15f,0,0),p,.018f,color);Line(p,p-new Vector3(0,0,z*.15f),.018f,color);}
            Apply();
        }
        public void Shadow()
        {
            Clear();vertices.Add(Vector3.zero);colors.Add(new Color(.025f,.03f,.04f,.32f));
            const int sides=32;
            for(int i=0;i<=sides;i++){float a=i*Mathf.PI*2/sides;vertices.Add(new Vector3(Mathf.Cos(a)*.34f,0,Mathf.Sin(a)*.25f));colors.Add(new Color(.025f,.03f,.04f,0));}
            for(int i=1;i<=sides;i++){triangles.Add(0);triangles.Add(i);triangles.Add(i+1);}Apply();
        }
        public void Apply(){mesh.Clear();mesh.SetVertices(vertices);mesh.SetColors(colors);mesh.SetTriangles(triangles,0);mesh.RecalculateBounds();}
        void OnEnable(){if(surface!=null)surface.enabled=true;}
        void OnDisable(){if(surface!=null)surface.enabled=false;}
        void OnDestroy(){if(mesh!=null)Destroy(mesh);}
    }
}
