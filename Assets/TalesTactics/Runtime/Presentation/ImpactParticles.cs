using System.Collections.Generic;
using UnityEngine;
namespace TalesTactics
{
    // Local bounded pool; presentation never samples UnityEngine.Random or changes battle state.
    public sealed class ImpactPool:MonoBehaviour
    {
        public const int Capacity=16;
        readonly List<ImpactParticles> instances=new List<ImpactParticles>();
        Material solid,atlas;
        public int ActiveCount{get{int n=0;foreach(var p in instances)if(p.gameObject.activeSelf)n++;return n;}}
        public void Play(Camera camera,Vector3 from,Vector3 to,Element element,CombatVisualStyle style,CombatFeedback feedback,float size,bool ultimate,bool reduced)
        {
            if(solid==null)
            {
                var shader=Resources.Load<Shader>("TalesTactics/Impact");
                solid=new Material(shader);solid.SetFloat("_UseTexture",0);
                atlas=new Material(shader);atlas.mainTexture=Resources.Load<Texture2D>("TalesTactics/CombatFire");atlas.SetFloat("_UseTexture",1);
            }
            ImpactParticles chosen=null;foreach(var p in instances)if(!p.gameObject.activeSelf){chosen=p;break;}
            if(chosen==null&&instances.Count<Capacity)
            {var g=new GameObject("Elemental impact");g.transform.SetParent(transform,false);chosen=g.AddComponent<ImpactParticles>();chosen.Initialize(solid,atlas);instances.Add(chosen);}
            if(chosen==null){chosen=instances[0];foreach(var p in instances)if(p.Age>chosen.Age)chosen=p;}
            chosen.Play(camera,from,to,element,style,feedback,size,ultimate,reduced);
        }
        void OnDestroy(){if(solid!=null)Destroy(solid);if(atlas!=null)Destroy(atlas);}
    }
    public sealed class ImpactParticles:MonoBehaviour
    {
        sealed class Geometry
        {
            public Mesh Mesh;public readonly List<Vector3> V=new List<Vector3>(512);public readonly List<Color> C=new List<Color>(512);public readonly List<Vector2> U=new List<Vector2>(512);public readonly List<int> I=new List<int>(1024);
            public void Clear(){V.Clear();C.Clear();U.Clear();I.Clear();}
            public void Vertex(Vector3 p,Color c,Vector2 uv){V.Add(p);C.Add(c);U.Add(uv);}
            public void Triangle(int a,int b,int c){I.Add(a);I.Add(b);I.Add(c);}
            public void Upload(){Mesh.Clear();Mesh.SetVertices(V);Mesh.SetColors(C);Mesh.SetUVs(0,U);Mesh.SetTriangles(I,0);Mesh.RecalculateBounds();}
        }
        readonly Geometry solid=new Geometry(),atlas=new Geometry();Camera view;Vector3 from;Element element;CombatVisualStyle style;CombatFeedback feedback;float size,age;bool reduced;
        public float Age=>age;public Element Element=>element;public const float Duration=1.05f;
        public void Initialize(Material plain,Material texture)
        {
            Create(solid,plain,"Impact volume");Create(atlas,texture,"Flame and smoke");
        }
        void Create(Geometry geometry,Material material,string name)
        {var g=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));g.transform.SetParent(transform,false);geometry.Mesh=new Mesh{name=name};geometry.Mesh.MarkDynamic();g.GetComponent<MeshFilter>().sharedMesh=geometry.Mesh;var r=g.GetComponent<MeshRenderer>();r.sharedMaterial=material;r.sortingOrder=23;}
        public void Play(Camera camera,Vector3 origin,Vector3 target,Element kind,CombatVisualStyle visual,CombatFeedback hit,float scale,bool ultimate,bool low)
        {view=camera;transform.position=target;from=origin-target;element=kind;style=visual;feedback=hit;size=Mathf.Clamp(scale,.7f,1.6f)*(ultimate?1.3f:1);reduced=low;age=0;gameObject.SetActive(true);Draw();}
        void Update(){age+=Time.deltaTime;if(view==null||age>=Duration){gameObject.SetActive(false);return;}Draw();}
        Vector3 Plane(float x,float y)=>view.transform.right*x*size+view.transform.up*y*size;
        static Color Alpha(Color c,float a){c.a=a;return c;}
        void Quad(Geometry mesh,Vector3 center,Vector3 right,Vector3 up,Color tint,int cell=-1)
        {
            int n=mesh.V.Count;float x=cell<0?0:(cell%4)*.25f,y=cell<0?0:(3-cell/4)*.25f,w=cell<0?1:.25f;
            mesh.Vertex(center-right-up,tint,new Vector2(x,y));mesh.Vertex(center-right+up,tint,new Vector2(x,y+w));mesh.Vertex(center+right+up,tint,new Vector2(x+w,y+w));mesh.Vertex(center+right-up,tint,new Vector2(x+w,y));mesh.Triangle(n,n+1,n+2);mesh.Triangle(n,n+2,n+3);
        }
        void Disc(float x,float y,float radius,Color c,int sides=12)
        {
            int n=solid.V.Count;solid.Vertex(Plane(x,y),c,Vector2.one*.5f);
            for(int j=0;j<=sides;j++){float a=j*Mathf.PI*2/sides;solid.Vertex(Plane(x+Mathf.Cos(a)*radius,y+Mathf.Sin(a)*radius),Alpha(c,0),Vector2.zero);if(j>0)solid.Triangle(n,n+j,n+j+1);}
        }
        void Ribbon(Vector3 a,Vector3 b,float width,Color c)
        {
            var side=Vector3.Cross(view.transform.forward,b-a).normalized*width*size;int n=solid.V.Count;
            solid.Vertex(a-side,c,Vector2.zero);solid.Vertex(a+side,c,Vector2.up);solid.Vertex(b+side,c,Vector2.one);solid.Vertex(b-side,c,Vector2.right);solid.Triangle(n,n+1,n+2);solid.Triangle(n,n+2,n+3);
        }
        void Arc(float radius,float start,float sweep,float width,Color c,float y=0)
        {for(int j=0;j<24;j++){float a=(start+sweep*j/24)*Mathf.Deg2Rad,b=(start+sweep*(j+1)/24)*Mathf.Deg2Rad;Ribbon(Plane(Mathf.Cos(a)*radius,Mathf.Sin(a)*radius+y),Plane(Mathf.Cos(b)*radius,Mathf.Sin(b)*radius+y),width*Mathf.Sin((j+1)*Mathf.PI/25),c);}}
        void Shard(float x,float y,float width,float height,float angle,Color color)
        {
            Vector2 axis=new Vector2(Mathf.Cos(angle),Mathf.Sin(angle)),side=new Vector2(-axis.y,axis.x);int n=solid.V.Count;
            foreach(var p in new[]{axis*height,side*width,-axis*height,-side*width})solid.Vertex(Plane(x+p.x,y+p.y),color,Vector2.zero);
            solid.Vertex(Plane(x,y),Color.Lerp(color,Color.white,.55f),Vector2.zero);
            for(int i=0;i<4;i++)solid.Triangle(n+i,n+(i+1)%4,n+4);
        }
        void Smoke(float t,int count=3)
        {
            for(int i=0;i<count;i++)
            {float delay=i*.06f,p=Mathf.Clamp01((t-.32f-delay)/.65f);if(p<=0)continue;float x=(i-1)*.21f,y=.1f+p*.5f;Quad(atlas,Plane(x,y),Plane(.28f+p*.3f,0),Plane(0,.25f+p*.28f),new Color(.65f,.63f,.61f,(1-p)*.5f),12+Mathf.Min(3,(int)(p*4)));}
        }
        void Draw()
        {
            solid.Clear();atlas.Clear();float t=age/Duration,fade=(1-t)*(reduced?.65f:1);int count=reduced?7:16;
            if(feedback!=CombatFeedback.Strike)
            {
                var color=feedback==CombatFeedback.Heal?new Color(.35f,1,.65f):new Color(1,.89f,.58f);
                for(int i=0;i<count;i++){float a=i*2.39996f;float y=Mathf.Repeat(i*.17f+t*.8f,1.4f)-.6f;Disc(Mathf.Cos(a)*.45f,y,.065f,Alpha(color,fade));}
                Arc(.35f+t*.35f,0,360,.035f,Alpha(color,fade),-.35f);
            }
            else
            {
                if(element==Element.Fire)
                {
                    int frame=Mathf.Min(11,(int)(t*12));
                    Quad(atlas,Plane(0,.12f),Plane(.85f,0),Plane(0,.9f),new Color(1,1,1,reduced?.72f:1),frame);
                    if(!reduced)Quad(atlas,Plane(-.22f,-.1f),Plane(.45f,0),Plane(0,.58f),new Color(1,.8f,.5f,.6f),Mathf.Min(11,frame+2));
                    Smoke(t);
                    for(int i=0;i<count;i++){float a=i*2.39996f,r=.1f+t*(.3f+(i%3)*.18f);Disc(Mathf.Cos(a)*r,Mathf.Sin(a)*r+t*.7f,.018f+(i%3)*.007f,new Color(1,.43f,.07f,fade));}
                }
                else if(element==Element.Ice||element==Element.Earth)
                {
                    var color=element==Element.Ice?new Color(.38f,.81f,1,fade):new Color(.64f,.45f,.26f,fade);
                    for(int i=0;i<count;i++){float a=i*2.39996f,r=.12f+t*(.55f+(i%3)*.15f);Shard(Mathf.Cos(a)*r,Mathf.Sin(a)*r-t*t*.4f,.055f,element==Element.Ice?.22f:.10f,a+t,color);}
                    if(element==Element.Earth)Smoke(t);
                }
                else if(element==Element.Lightning)
                {
                    float flash=reduced?.55f:.8f;var c=new Color(.73f,.84f,1,fade*flash);
                    for(int branch=0;branch<3;branch++)for(int i=0;i<7;i++)
                    {float x=(branch-1)*.24f,y=.8f-i*.22f;float jolt=Mathf.Sin(i*19+Mathf.Floor(age*18)*5+branch)*.15f;Ribbon(Plane(x+jolt,y),Plane(x-jolt,y-.22f),.026f,c);}
                    Disc(0,0,.24f,Alpha(c,fade));
                }
                else if(element==Element.Water||element==Element.Wind||element==Element.Dark)
                {
                    var c=element==Element.Water?new Color(.22f,.65f,1,fade):element==Element.Wind?new Color(.54f,.91f,.76f,fade):new Color(.59f,.29f,.82f,fade);
                    for(int i=0;i<3;i++)Arc(.22f+i*.15f+t*.3f,t*360+i*120,200,.055f,c,(i-1)*.13f);
                    if(element==Element.Water)for(int i=0;i<count;i++){float a=i*2.39996f;Disc(Mathf.Cos(a)*t*.7f,Mathf.Sin(a)*t*.7f+.5f*t-t*t,.04f,Alpha(c,fade));}
                    if(element==Element.Dark)Disc(0,0,.35f,new Color(.06f,.025f,.13f,fade));
                }
                else if(element==Element.Light)
                {
                    for(int i=0;i<8;i++){float a=i*Mathf.PI*.25f;Ribbon(Plane(Mathf.Cos(a)*.08f,Mathf.Sin(a)*.08f),Plane(Mathf.Cos(a)*(.4f+t*.5f),Mathf.Sin(a)*(.4f+t*.5f)),.025f,new Color(1,.88f,.55f,fade));}
                    Disc(0,0,.24f,new Color(1,.98f,.75f,fade));
                }
                if(element==Element.None||style==CombatVisualStyle.FlameSword||style==CombatVisualStyle.Claw||style==CombatVisualStyle.Sword||style==CombatVisualStyle.Martial||style==CombatVisualStyle.Shield||style==CombatVisualStyle.Spear)Physical(t,fade);
            }
            solid.Upload();atlas.Upload();
        }
        void Physical(float t,float fade)
        {
            float contact=Mathf.Clamp01((t-.075f)/.55f);var white=new Color(.91f,.96f,1,fade);
            if(style==CombatVisualStyle.Sword||style==CombatVisualStyle.FlameSword||style==CombatVisualStyle.Claw)
            {
                float direction=Vector3.Dot(from,view.transform.right)>0?-1:1;
                for(int i=0;i<(style==CombatVisualStyle.Claw?3:1);i++)Arc(.64f+i*.1f,-140+contact*120*direction,150*direction,.10f,white,(i-1)*.1f);
            }
            else if(style==CombatVisualStyle.Spear||style==CombatVisualStyle.Rifle||style==CombatVisualStyle.Bow)
            {var start=Vector3.Lerp(from,Vector3.zero,Mathf.Clamp01(t*5));Ribbon(start,Vector3.zero,style==CombatVisualStyle.Spear?.07f:.025f,white);}
            else Arc(.14f+contact*.7f,0,360,style==CombatVisualStyle.Shield?.09f:.06f,white,-.08f);
            if(t<.32f)Disc(0,0,.35f,new Color(1,.91f,.65f,(1-t/.32f)*(reduced?.4f:.9f)));
            for(int i=0;i<(reduced?4:9);i++){float a=i*2.39996f,r=t*.95f;Ribbon(Plane(Mathf.Cos(a)*r,Mathf.Sin(a)*r),Plane(Mathf.Cos(a)*(r+.1f),Mathf.Sin(a)*(r+.1f)),.013f,new Color(1,.69f,.33f,fade));}
            Smoke(t,reduced?1:2);
        }
        void OnDestroy(){if(solid.Mesh!=null)Destroy(solid.Mesh);if(atlas.Mesh!=null)Destroy(atlas.Mesh);}
    }
}
