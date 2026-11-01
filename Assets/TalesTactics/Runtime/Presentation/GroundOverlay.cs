using System.Collections.Generic;
using UnityEngine;
namespace TalesTactics
{
    // Transparent geometry follows the ground without raised bases or internal tile outlines.
    public sealed class GroundOverlay:MonoBehaviour
    {
        SpriteRenderer[] ornaments;
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
            if(ornaments==null)
            {
                ornaments=new SpriteRenderer[4];
                for(int i=0;i<4;i++){var g=new GameObject("Ground selection ornament");g.transform.SetParent(transform,false);var art=g.AddComponent<SpriteRenderer>();art.sprite=IllustratedIcons.Sprite("selected",i);art.sharedMaterial=surface.sharedMaterial;art.sortingOrder=surface.sortingOrder+1;ornaments[i]=art;}
            }
            for(int i=0;i<4;i++){var art=ornaments[i];art.transform.localPosition=new Vector3((i%2==0?-1:1)*(radius-.075f),.002f,(i/2==0?-1:1)*(radius-.075f));art.transform.localRotation=Quaternion.Euler(90,0,0);art.transform.localScale=Vector3.one*.22f;art.color=new Color(1,1,1,color.a);art.enabled=enabled;}
            Apply();
        }
        public void Shadow()=>Shadow(new Color(.09f,.14f,.16f));
        public void Shadow(Color tint)
        {
            Clear();
            // Broad cast shade plus denser foot contact, both feathered to transparent.
            ShadowLobe(new Vector3(.13f,0,.10f),new Vector2(.42f,.32f),tint,.22f);
            ShadowLobe(Vector3.zero,new Vector2(.22f,.16f),tint,.42f);
            Apply();
        }
        void ShadowLobe(Vector3 center,Vector2 radius,Color tint,float opacity)
        {
            int start=vertices.Count;vertices.Add(center);colors.Add(new Color(tint.r,tint.g,tint.b,opacity));
            const int sides=32;
            for(int i=0;i<=sides;i++){float a=i*Mathf.PI*2/sides;vertices.Add(center+new Vector3(Mathf.Cos(a)*radius.x,0,Mathf.Sin(a)*radius.y));colors.Add(new Color(tint.r,tint.g,tint.b,0));}
            for(int i=1;i<=sides;i++){triangles.Add(start);triangles.Add(start+i);triangles.Add(start+i+1);}
        }
        public void Apply(){mesh.Clear();mesh.SetVertices(vertices);mesh.SetColors(colors);mesh.SetTriangles(triangles,0);mesh.RecalculateBounds();}
        void OnEnable(){if(surface!=null)surface.enabled=true;if(ornaments!=null)foreach(var art in ornaments)art.enabled=true;}
        void OnDisable(){if(surface!=null)surface.enabled=false;if(ornaments!=null)foreach(var art in ornaments)art.enabled=false;}
        void OnDestroy(){if(mesh!=null)Destroy(mesh);}
    }
}
