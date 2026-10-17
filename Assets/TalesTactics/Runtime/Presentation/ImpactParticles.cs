using System.Collections.Generic;
using UnityEngine;
namespace TalesTactics
{
    // Local bounded pool; presentation never samples UnityEngine.Random or changes battle state.
    public sealed class ImpactPool:MonoBehaviour
    {
        public const int Capacity=16;
        readonly List<ImpactParticles> instances=new List<ImpactParticles>();
        Material solid,atlas,artMaterial,signatureMaterial,weaponMaterial,spellMaterial;Camera depthCamera;
        public int ActiveCount{get{int n=0;foreach(var p in instances)if(p.gameObject.activeSelf)n++;return n;}}
        public void Play(Camera camera,Vector3 from,Vector3 to,Element element,CombatVisualStyle style,CombatFeedback feedback,float size,bool ultimate,bool reduced,SkillVisualPattern pattern=SkillVisualPattern.Automatic,bool compact=false,SkillSignature signature=SkillSignature.None,ArteProfile arte=default)
        {Acquire(camera).Play(camera,from,to,element,style,feedback,size,ultimate,reduced,pattern,compact,signature,arte);}
        public void Prepare(Camera camera,Transform caster,Vector3 to,Element element,CombatVisualStyle style,float size,bool ultimate,bool reduced,float duration,SkillVisualPattern pattern=SkillVisualPattern.Automatic,SkillSignature signature=SkillSignature.None,ArteProfile arte=default)
        {Acquire(camera).Prepare(camera,caster,to,element,style,size,ultimate,reduced,duration,pattern,signature,arte);}
        public void Travel(Camera camera,Vector3 from,Vector3 to,Element element,CombatVisualStyle style,float size,bool ultimate,bool reduced,float duration,SkillVisualPattern pattern,SkillSignature signature=SkillSignature.None,ArteProfile arte=default)
        {Acquire(camera).Travel(camera,from,to,element,style,size,ultimate,reduced,duration,pattern,signature,arte);}
        ImpactParticles Acquire(Camera camera)
        {
            if(solid==null)
            {
                var shader=Resources.Load<Shader>("TalesTactics/Impact");
                solid=new Material(shader);solid.SetFloat("_UseTexture",0);
                atlas=new Material(shader);atlas.mainTexture=Resources.Load<Texture2D>("TalesTactics/CombatFire");atlas.SetFloat("_UseTexture",1);
                artMaterial=new Material(shader);artMaterial.mainTexture=Resources.Load<Texture2D>("TalesTactics/CombatElements");artMaterial.SetFloat("_UseTexture",1);
                signatureMaterial=new Material(shader);signatureMaterial.mainTexture=Resources.Load<Texture2D>("TalesTactics/OriginalSkillTrio");signatureMaterial.SetFloat("_UseTexture",1);
                weaponMaterial=new Material(shader);weaponMaterial.mainTexture=Resources.Load<Texture2D>("TalesTactics/ArteWeapons");weaponMaterial.SetFloat("_UseTexture",1);weaponMaterial.SetFloat("_CellFeather",1);
                spellMaterial=new Material(shader);spellMaterial.mainTexture=Resources.Load<Texture2D>("TalesTactics/ArteMagic");spellMaterial.SetFloat("_UseTexture",1);spellMaterial.SetFloat("_CellFeather",1);
            }
            if(depthCamera!=camera)
            {
                depthCamera=camera;bool depth=(camera.depthTextureMode&DepthTextureMode.Depth)!=0;
                // Query the configured URP camera once without coupling the rules assembly to URP.
                foreach(var component in camera.GetComponents<Component>())
                    if(component!=null&&component.GetType().Name=="UniversalAdditionalCameraData")
                    {var property=component.GetType().GetProperty("requiresDepthTexture");if(property!=null)depth=(bool)property.GetValue(component);break;}
                atlas.SetFloat("_SoftParticles",depth?1:0);artMaterial.SetFloat("_SoftParticles",depth?1:0);signatureMaterial.SetFloat("_SoftParticles",depth?1:0);weaponMaterial.SetFloat("_SoftParticles",depth?1:0);spellMaterial.SetFloat("_SoftParticles",depth?1:0);
            }
            ImpactParticles chosen=null;foreach(var p in instances)if(!p.gameObject.activeSelf){chosen=p;break;}
            if(chosen==null&&instances.Count<Capacity)
            {var g=new GameObject("Elemental impact");g.transform.SetParent(transform,false);chosen=g.AddComponent<ImpactParticles>();chosen.Initialize(solid,atlas,artMaterial,signatureMaterial,weaponMaterial,spellMaterial);instances.Add(chosen);}
            if(chosen==null){chosen=instances[0];foreach(var p in instances)if(p.Age>chosen.Age)chosen=p;}
            return chosen;
        }
        void OnDestroy(){if(solid!=null)Destroy(solid);if(atlas!=null)Destroy(atlas);if(artMaterial!=null)Destroy(artMaterial);if(signatureMaterial!=null)Destroy(signatureMaterial);if(weaponMaterial!=null)Destroy(weaponMaterial);if(spellMaterial!=null)Destroy(spellMaterial);}
    }
    public sealed partial class ImpactParticles:MonoBehaviour
    {
        sealed class Geometry
        {
            public Mesh Mesh;public readonly List<Vector3> V=new List<Vector3>(512);public readonly List<Color> C=new List<Color>(512);public readonly List<Vector2> U=new List<Vector2>(512);public readonly List<Vector3> N=new List<Vector3>(512);public readonly List<int> I=new List<int>(1024);
            public void Clear(){V.Clear();C.Clear();U.Clear();N.Clear();I.Clear();}
            public void Vertex(Vector3 p,Color c,Vector2 uv){Vertex(p,c,uv,uv,0);}
            public void Vertex(Vector3 p,Color c,Vector2 uv,Vector2 next,float blend){V.Add(p);C.Add(c);U.Add(uv);N.Add(new Vector3(next.x,next.y,blend));}
            public void Triangle(int a,int b,int c){I.Add(a);I.Add(b);I.Add(c);}
            public void Upload(){Mesh.Clear();Mesh.SetVertices(V);Mesh.SetColors(C);Mesh.SetUVs(0,U);Mesh.SetUVs(1,N);Mesh.SetTriangles(I,0);Mesh.RecalculateBounds();}
        }
        readonly Geometry solid=new Geometry(),atlas=new Geometry();Camera view;Vector3 from;Element element;CombatVisualStyle style;CombatFeedback feedback;float size,age;bool reduced;
        public float Age=>age;public Element Element=>element;public const float Duration=1.05f;
        public void Initialize(Material plain,Material texture,Material elements=null,Material signatures=null,Material weapons=null,Material spells=null)
        {
            Create(solid,plain,"Impact volume");Create(atlas,texture,"Flame and smoke");defaultArt=elements??texture;signatureArt=signatures??defaultArt;artRenderer=Create(art,defaultArt,"Authored elemental silhouette");defaultBack=texture;weaponArt=weapons??defaultArt;spellArt=spells??defaultArt;backRenderer=Create(backAtlas,texture,"Depth flame and mist",-1);
        }
        MeshRenderer Create(Geometry geometry,Material material,string name,int order=23)
        {var g=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));g.transform.SetParent(transform,false);geometry.Mesh=new Mesh{name=name};geometry.Mesh.MarkDynamic();g.GetComponent<MeshFilter>().sharedMesh=geometry.Mesh;var r=g.GetComponent<MeshRenderer>();r.sharedMaterial=material;r.sortingOrder=order;return r;}
        public void Play(Camera camera,Vector3 origin,Vector3 target,Element kind,CombatVisualStyle visual,CombatFeedback hit,float scale,bool ultimate,bool low,SkillVisualPattern visualPattern=SkillVisualPattern.Automatic,bool small=false,SkillSignature skillSignature=SkillSignature.None,ArteProfile arte=default)
        {signature=skillSignature;SetArte(arte);pattern=visualPattern;compact=small;this.ultimate=ultimate;travelling=false;view=camera;transform.position=target;from=origin-target;element=kind;style=visual;feedback=hit;size=Mathf.Clamp(scale,.35f,1.6f)*(ultimate?1.3f:1);reduced=low;age=0;preparing=false;casterAnchor=null;gameObject.SetActive(true);Draw();}
        void Update(){age+=Time.deltaTime;if(view==null||age>=(preparing?prepareDuration:travelling?travelDuration:Duration)){gameObject.SetActive(false);return;}Draw();}
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
            solid.Clear();atlas.Clear();art.Clear();backAtlas.Clear();float t=age/Duration,fade=(1-t)*(reduced?.65f:1);int count=reduced?7:16;
            if(preparing){DrawPreparation();UploadAll();return;}
            if(travelling){DrawTravel();UploadAll();return;}
            if(compact){DrawCompact(t);UploadAll();return;}
            if(arte.Enabled){DrawArteImpact(t);UploadAll();return;}
            if(signature!=SkillSignature.None&&feedback==CombatFeedback.Strike){DrawSignatureImpact(t);UploadAll();return;}
            if(HasAuthoredArt(pattern,element,feedback)){DrawAuthored(t);UploadAll();return;}
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
                    FireImpact(t);
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
                bool physical=element==Element.None||style==CombatVisualStyle.FlameSword||style==CombatVisualStyle.Claw||style==CombatVisualStyle.Sword||style==CombatVisualStyle.Martial||style==CombatVisualStyle.Shield||style==CombatVisualStyle.Spear;
                if(physical&&(element!=Element.Fire||HasWeaponSweep(style)))Physical(t,fade);
            }
            UploadAll();
        }
        void UploadAll(){solid.Upload();atlas.Upload();art.Upload();backAtlas.Upload();}
        void Physical(float t,float fade)
        {
            float contact=Mathf.Clamp01((t-.075f)/.55f);var white=new Color(.91f,.96f,1,fade);
            if(style==CombatVisualStyle.Sword||style==CombatVisualStyle.FlameSword||style==CombatVisualStyle.Claw)
            {
                WeaponSweep(Mathf.Lerp(.68f,1,Mathf.Clamp01(t/.38f)),Mathf.Pow(1-Mathf.Clamp01(t/.65f),1.4f));
            }
            else if(style==CombatVisualStyle.Spear)
            {var start=Vector3.Lerp(from,Vector3.zero,Mathf.Clamp01(t*5));Ribbon(start,Vector3.zero,style==CombatVisualStyle.Spear?.07f:.025f,white);}
            else Arc(.14f+contact*.7f,0,360,style==CombatVisualStyle.Shield?.09f:.06f,white,-.08f);
            if(t<.25f){Disc(0,0,.72f,new Color(1,.68f,.19f,(1-t/.25f)*(reduced?.25f:.72f)));Disc(0,0,.25f,new Color(1,1,.87f,1-t/.25f));}
            for(int i=0;i<(reduced?5:16);i++){float a=i*2.39996f,r=t*2;Ribbon(Plane(Mathf.Cos(a)*r,Mathf.Sin(a)*r),Plane(Mathf.Cos(a)*(r+.22f),Mathf.Sin(a)*(r+.22f)),.022f,new Color(1,.79f,.39f,fade));}
            GroundDust(t);
        }
        void OnDestroy(){if(solid.Mesh!=null)Destroy(solid.Mesh);if(atlas.Mesh!=null)Destroy(atlas.Mesh);if(art.Mesh!=null)Destroy(art.Mesh);if(backAtlas.Mesh!=null)Destroy(backAtlas.Mesh);}
    }
}
