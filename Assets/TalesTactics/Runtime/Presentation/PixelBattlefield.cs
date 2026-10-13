using System.Collections.Generic;
using UnityEngine;
namespace TalesTactics
{
    // Presentation only: GridMap heights, movement and combat RNG are never modified.
    public sealed class PixelBattlefield : MonoBehaviour
    {
        const string Art="TalesTactics/TerrainArt/";
        static readonly string[] Backgrounds={"Ruins","Sanctum","Canyon","Observatory","Sluice","Relay"};
        static readonly Vector2Int[] Directions={Vector2Int.left,Vector2Int.right,Vector2Int.up,Vector2Int.down};
        readonly List<Material> owned=new List<Material>();
        readonly List<Mesh> meshes=new List<Mesh>();
        readonly List<Sprite> sprites=new List<Sprite>();
        readonly List<Transform> billboards=new List<Transform>();
        readonly TerrainGeometry[] geometry=new TerrainGeometry[16];
        readonly Material[] surfaces=new Material[16];
        readonly List<Vector4> shadowCasters=new List<Vector4>();
        Texture2D architecture;Sprite[] architectureSprites;
        Material floorMaterial;
        Camera view;Material spriteMaterial;Transform backdrop;Texture2D foliage;Sprite[] foliageSprites;
        int stage,wall;float bottom,backgroundAspect;
        public int DecorationMeshCount=>meshes.Count;
        public int Chapter=>stage;
        public void Initialize(int chapter,Material source,Material spriteSource,Camera camera)
        {
            stage=Mathf.Clamp(chapter,0,5);wall=stage>=3?14:4;bottom=stage==2?-2.5f:-1.9f;
            view=camera;spriteMaterial=spriteSource;
            var atlas=Resources.Load<Texture2D>(Art+"Surfaces");var shader=Resources.Load<Shader>(Art+"Terrain");
            for(int i=0;i<16;i++)
            {
                var m=shader!=null?new Material(shader):new Material(source);m.name="Architectural terrain "+i;owned.Add(m);
                m.SetTexture("_MainTex",atlas);m.SetVector("_AtlasRect",new Vector4(i%4*.25f+.0025f,(3-i/4)*.25f+.0025f,.245f,.245f));
                m.SetColor("_BaseColor",Color.white);
                m.SetColor("_KeyColor",stage==2?new Color(1.08f,.98f,.85f):stage==3?new Color(.92f,.99f,1.10f):stage==5?new Color(1.08f,1.02f,.97f):new Color(1.02f,1.01f,.94f));
                m.SetColor("_FillColor",stage==2?new Color(.92f,.78f,.70f):new Color(.76f,.86f,.95f));
                if(i==8||i==9)m.SetFloat("_Water",1);surfaces[i]=m;geometry[i]=new TerrainGeometry();
            }
            surfaces[6].SetTexture("_MainTex",Resources.Load<Texture2D>(Art+"Cliff"));
            surfaces[6].SetVector("_AtlasRect",new Vector4(0,0,1,1));surfaces[6].SetFloat("_WorldCliff",1);
            foliage=Resources.Load<Texture2D>(Art+"Foliage");foliageSprites=new Sprite[16];
            architecture=Resources.Load<Texture2D>(Art+"Architecture");architectureSprites=new Sprite[16];
            floorMaterial=new Material(surfaces[0]);owned.Add(floorMaterial);floorMaterial.name="Continuous chapter pavement";
            floorMaterial.SetTexture("_MainTex",Resources.Load<Texture2D>(Art+(stage==2?"CanyonFloor":stage==3||stage==5?"ObservatoryFloor":stage==4?"SluiceFloor":"RuinsFloor")));
            floorMaterial.SetVector("_AtlasRect",new Vector4(0,0,1,1));floorMaterial.SetFloat("_WorldFloor",1);
            floorMaterial.SetFloat("_DetailContrast",.76f);
            var distant=Resources.Load<Texture2D>(Art+Backgrounds[stage]);
            if(distant!=null)
            {
                var panoramaShader=Resources.Load<Shader>(Art+"Panorama");
                var m=panoramaShader!=null?new Material(panoramaShader):new Material(source);m.name="Chapter panorama "+stage;owned.Add(m);m.mainTexture=distant;
                m.SetColor("_Atmosphere",stage==2?new Color(.25f,.22f,.22f):new Color(.13f,.21f,.27f));
                backdrop=new GameObject("Chapter panorama").transform;backdrop.SetParent(transform,false);
                var quad=new TerrainGeometry();quad.Quad(new Vector3(-.5f,-.5f,0),new Vector3(-.5f,.5f,0),new Vector3(.5f,.5f,0),new Vector3(.5f,-.5f,0));
                meshes.Add(quad.Flush(backdrop,"Panorama surface",m));backgroundAspect=(float)distant.width/distant.height;
            }
        }
        public Material Surface(GridTile tile)
        {
            if(tile.Terrain==TerrainType.Water)return surfaces[8];
            return floorMaterial;
        }
        public Color Tint(GridTile tile){float v=.94f+((tile.Coordinate.x*13+tile.Coordinate.y*7)%5)*.013f;return new Color(v,v,v,1);}
        public void Decorate(GridTile tile,GridMap map,float step)
        {
            Vector3 p=tile.WorldPosition(step);int x=tile.Coordinate.x,z=tile.Coordinate.y;bool wet=tile.Terrain==TerrainType.Water;
            foreach(var d in Directions)
            {
                var n=map[tile.Coordinate+d];var outward=new Vector3(d.x,0,d.y);var across=new Vector3(d.y,0,-d.x);
                if(n==null)
                {
                    var edge=p+outward*.50f;
                    if(wet){Waterfall(edge,outward,1,p.y-bottom);continue;}
                    geometry[wall].Box(edge+Vector3.down*.17f,new Vector3(d.x==0?1.015f:.20f,.34f,d.y==0?1.015f:.20f));
                    var support=edge+Vector3.up*(bottom-p.y)*.5f;
                    geometry[wall].Box(support-outward*.15f,new Vector3(d.x==0?1.01f:.24f,p.y-bottom,d.y==0?1.01f:.24f),.76f);
                    geometry[wall].Box(edge-Vector3.up*.28f,new Vector3(d.x==0?.95f:.28f,.25f,d.y==0?.95f:.28f));
                    geometry[stage==2?5:wall].Box(edge+Vector3.up*.075f,new Vector3(d.x==0?.99f:.16f,.15f,d.y==0?.99f:.16f));
                    if((x+z)%2==0){geometry[wall].Box(edge+Vector3.up*.17f,Vector3.one*.24f);Foliage(stage==2?6:0,edge+outward*.27f-Vector3.up*.95f,1.05f);}
                    if((x+z)%4==1)DressedProp(stage>=3?11:3,edge-outward*.02f,.74f,false);
                    if((x+z)%3==1)Foliage(stage==2?4:2,edge+outward*.06f,.32f);
                    if(stage==2)geometry[6].Rock(edge+outward*.25f+Vector3.up*(bottom-p.y-.5f),1.15f,p.y-bottom+.4f,x*31+z*17);
                }
                else if(n.Terrain==TerrainType.Water&&!wet)
                {
                    var edge=p+outward*.47f;
                    geometry[wall].Box(edge-Vector3.up*.15f,new Vector3(d.x==0?.99f:.12f,.4f,d.y==0?.99f:.12f));
                    if((x+z)%3==0)Foliage(stage==2?4:10,edge-Vector3.up*.08f,.3f);
                }
                else if(n.Height<tile.Height&&!wet)
                {
                    float rise=(tile.Height-n.Height)*step;
                    for(int k=0;k<2;k++)
                    {
                        float stairHeight=rise*(2-k)/3f;
                        var center=p+outward*(.55f+k*.12f)-Vector3.up*(rise-stairHeight*.5f);
                        geometry[wall].Box(center,new Vector3(d.x==0?.99f:.13f,stairHeight,d.y==0?.99f:.13f));
                    }
                }
            }
            if(!tile.Walkable)
            {
                int prop=stage==2?4:stage==3?9:stage==4?13:stage==5?14:(x+z)%2;
                DressedProp(prop,p,stage==4?1.13f:1.55f,true);
            }
        }
        public void Complete(GridMap map,float step)
        {
            int maxX=0,maxZ=0;foreach(var t in map.Tiles.Values){maxX=Mathf.Max(maxX,t.Coordinate.x);maxZ=Mathf.Max(maxZ,t.Coordinate.y);}
            floorMaterial.SetVector("_FloorExtent",new Vector4(maxX+1,maxZ+1,0,0));
            if(stage!=2)BoundaryVaults(map,step,maxX,maxZ);
            float northHeight=map[new Vector2Int(maxX/2,maxZ)].Height*step;
            if(stage==3||stage==5)
            {
                var center=new Vector3(maxX*.5f,northHeight,maxZ+1.1f);
                geometry[wall].Box(center+Vector3.down*.38f,new Vector3(3.2f,.75f,1.2f));DressedProp(stage==3?8:14,center,2.6f,true);
                for(int side=-1;side<=1;side+=2){var foot=center+Vector3.right*side*2.9f;Buttress(foot,.9f);DressedProp(10,foot,2.4f,true);}
            }
            if(stage==4)
            {
                float h=map[new Vector2Int(maxX,maxZ/2)].Height*step;
                for(int z=2;z<maxZ;z+=5){var c=new Vector3(maxX+.92f,h,z);geometry[wall].Box(c-Vector3.up*.27f,new Vector3(1,.55f,1.8f));DressedProp(12,c,2.25f,true);}
            }
            if(stage==2)
            {
                var beacon=new Vector3(maxX+.8f,map[new Vector2Int(maxX,maxZ-1)].Height*step,maxZ-1);
                geometry[5].Box(beacon-Vector3.up*.2f,new Vector3(1.4f,.4f,1.4f));DressedProp(6,beacon,1.75f,true);
            }
            if(stage<=1)for(int side=0;side<2;side++){var p=new Vector3(side==0?-.65f:maxX+.65f,northHeight,maxZ+.25f);Buttress(p,.92f);DressedProp(1,p,2.25f,true);}
            // Exterior silhouettes never add colliders or alter playable coordinates.
            if(stage==2)
            {
                for(int i=0;i<4;i++)geometry[6].Rock(new Vector3(-.9f-i*.3f,bottom-.2f,maxZ*.35f+i*.48f),1.2f-i*.12f,1.7f+i*.22f,61+i);
                var bridge=new Vector3(maxX*.5f,northHeight-.2f,maxZ+1.35f);
                geometry[5].Box(bridge,new Vector3(1.25f,.3f,2.1f));
                for(int i=0;i<3;i++)geometry[5].Box(bridge+new Vector3(.62f,-.06f,i*.6f-.6f),new Vector3(.16f,.55f,.24f));
            }
            else if(stage<=1)
            {
                for(int i=0;i<4;i++)geometry[wall].Box(new Vector3(maxX*.3f,northHeight-.18f-i*.2f,maxZ+.65f+i*.3f),new Vector3(1.7f,.24f,.36f));
                for(int i=0;i<3;i++)geometry[wall].Box(new Vector3(-.85f,northHeight-.18f-i*.2f,maxZ*.4f+i*.55f),new Vector3(.65f,.35f,.45f));
            }
            var shadows=new Vector4[24];for(int i=0;i<Mathf.Min(shadowCasters.Count,24);i++)shadows[i]=shadowCasters[i];
            foreach(var m in surfaces){m.SetVectorArray("_TerrainShadows",shadows);m.SetInt("_ShadowCount",Mathf.Min(shadowCasters.Count,24));}
            floorMaterial.SetVectorArray("_TerrainShadows",shadows);floorMaterial.SetInt("_ShadowCount",Mathf.Min(shadowCasters.Count,24));
            for(int i=0;i<16;i++){var mesh=geometry[i].Flush(transform,"Architecture batch "+i,surfaces[i]);if(mesh!=null)meshes.Add(mesh);}
        }
        void BoundaryVaults(GridMap map,float step,int maxX,int maxZ)
        {
            var facade=new TerrainGeometry();var material=new Material(surfaces[wall]);owned.Add(material);
            material.name="Carved stone facade";material.SetTexture("_MainTex",Resources.Load<Texture2D>(Art+"WallFacade"));
            material.SetVector("_AtlasRect",new Vector4(0,0,1,1));
            material.SetColor("_BaseColor",stage>=3?new Color(.73f,.82f,.94f):Color.white);
            foreach(var tile in map.Tiles.Values)
            {
                if(tile.Terrain==TerrainType.Water)continue;
                foreach(var d in Directions)
                {
                    if(map[tile.Coordinate+d]!=null)continue;
                    var outward=new Vector3(d.x,0,d.y);var across=Vector3.Cross(outward,Vector3.up);
                    var edge=tile.WorldPosition(step)+outward*.525f;
                    var left=edge-across*.5f;var right=edge+across*.5f;
                    float length=d.x==0?maxX+1:maxZ+1;float k=d.x==0?tile.Coordinate.x:tile.Coordinate.y;float sign=d.x==0?across.x:across.z;
                    float u0=Mathf.Clamp((k+.5f-sign*.5f)/length,.001f,.999f),u1=Mathf.Clamp((k+.5f+sign*.5f)/length,.001f,.999f);
                    facade.Panel(new Vector3(left.x,bottom,left.z),left-Vector3.up*.3f,right-Vector3.up*.3f,new Vector3(right.x,bottom,right.z),u0,u1);
                }
            }
            var mesh=facade.Flush(transform,"Architecture facade",material);if(mesh!=null)meshes.Add(mesh);
        }
        void Buttress(Vector3 top,float width)
        {
            float height=top.y-bottom;
            geometry[wall].Box(top-Vector3.up*height*.5f,new Vector3(width,height,width));
            geometry[wall].Box(top-Vector3.up*.1f,new Vector3(width+.15f,.2f,width+.15f));
            Foliage(0,top+Vector3.back*width*.6f-Vector3.up*1.15f,1.2f);
        }
        void Waterfall(Vector3 edge,Vector3 outward,float width,float height)
        {
            var across=Vector3.Cross(outward,Vector3.up)*width*.48f;var top=edge+outward*.035f-Vector3.up*.04f;
            geometry[9].Quad(top-across-Vector3.up*height,top-across,top+across,top+across-Vector3.up*height);Foliage(12,edge-Vector3.up*height+outward*.10f,.8f);
        }
        Transform Foliage(int index,Vector3 position,float height)
        {
            if(foliage==null)return null;
            if(foliageSprites[index]==null){float w=foliage.width/4f,h=foliage.height/4f;var sprite=Sprite.Create(foliage,new Rect(index%4*w,(3-index/4)*h,w,h),new Vector2(.5f,0),100,0,SpriteMeshType.FullRect);sprite.name="Terrain accent "+index;sprites.Add(sprite);foliageSprites[index]=sprite;}
            var art=foliageSprites[index];var go=new GameObject("Terrain accent "+index,typeof(SpriteRenderer));go.transform.SetParent(transform,false);go.transform.localPosition=position;go.transform.localScale=Vector3.one*(height/art.bounds.size.y);
            var sr=go.GetComponent<SpriteRenderer>();sr.sprite=art;sr.sharedMaterial=spriteMaterial;sr.color=index==13?new Color(1,1,.88f,1):index==15?new Color(.72f,.92f,1,1):new Color(.85f,.88f,.80f,1);
            billboards.Add(go.transform);if(view!=null)go.transform.rotation=view.transform.rotation;return go.transform;
        }
        void DressedProp(int index,Vector3 position,float height,bool shadow)
        {
            if(architecture==null)return;
            if(architectureSprites[index]==null)
            {
                float w=architecture.width/4f,h=architecture.height/4f;
                var art=Sprite.Create(architecture,new Rect(index%4*w,(3-index/4)*h,w,h),new Vector2(.5f,.06f),100,0,SpriteMeshType.FullRect);
                art.name="Architectural detail "+index;sprites.Add(art);architectureSprites[index]=art;
            }
            var sprite=architectureSprites[index];var go=new GameObject("Architectural detail "+index,typeof(SpriteRenderer));go.transform.SetParent(transform,false);
            go.transform.localPosition=position+Vector3.up*.015f;go.transform.localScale=Vector3.one*(height/sprite.bounds.size.y);
            var renderer=go.GetComponent<SpriteRenderer>();renderer.sharedMaterial=spriteMaterial;renderer.sprite=sprite;
            billboards.Add(go.transform);go.transform.rotation=view.transform.rotation;
            if(shadow)shadowCasters.Add(new Vector4(position.x,position.z,height,position.y));
        }
        void LateUpdate()
        {
            if(view==null)return;foreach(var t in billboards)t.rotation=view.transform.rotation;
            if(backdrop!=null){backdrop.SetPositionAndRotation(view.transform.position+view.transform.forward*Mathf.Min(80,view.farClipPlane-1),view.transform.rotation);float h=view.orthographicSize*2*Mathf.Max(1,view.aspect/backgroundAspect);backdrop.localScale=new Vector3(h*backgroundAspect,h,1);}
        }
        void OnDestroy(){foreach(var m in owned)if(m!=null)Destroy(m);foreach(var m in meshes)if(m!=null)Destroy(m);foreach(var s in sprites)if(s!=null)Destroy(s);}
    }
}
