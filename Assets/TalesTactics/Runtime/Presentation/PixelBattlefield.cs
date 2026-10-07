using System.Collections.Generic;
using UnityEngine;
namespace TalesTactics
{
    // Visuals only: no colliders, occupancy, terrain costs or combat RNG are changed.
    public sealed class PixelBattlefield : MonoBehaviour
    {
        readonly List<Material> owned=new List<Material>();
        readonly List<Renderer> water=new List<Renderer>();
        readonly List<float> waterHeight=new List<float>();
        readonly List<Transform> billboards=new List<Transform>();
        readonly Dictionary<string,Sprite> props=new Dictionary<string,Sprite>();
        Camera view;
        Material spriteMaterial;
        Transform backdrop;
        Material[] surfaces;
        Material stone,bronze,moss;
        int stage;
        public void Initialize(int chapter,Material source,Material sprites,Camera camera)
        {
            stage=chapter;view=camera;spriteMaterial=sprites;
            foreach(var s in Resources.LoadAll<Sprite>("TalesTactics/PixelProps"))props[s.name]=s;
            var atlas=Resources.Load<Texture2D>("TalesTactics/PixelTerrain");
            surfaces=new Material[16];
            for(int i=0;i<16;i++)
            {
                var m=new Material(source);owned.Add(m);m.color=Color.white;
                if(atlas!=null){m.mainTexture=atlas;m.mainTextureScale=Vector2.one*0.249f;m.mainTextureOffset=new Vector2(i%4*0.25f+0.0005f,(3-i/4)*0.25f+0.0005f);}
                surfaces[i]=m;
            }
            stone=surfaces[4];bronze=surfaces[10];moss=surfaces[2];
            var distant=Resources.Load<Texture2D>("TalesTactics/PixelBackdrops");
            if(distant!=null)
            {
                int panel=Mathf.Clamp(stage,0,5);var m=new Material(source);owned.Add(m);
                m.mainTexture=distant;m.color=Color.white;
                m.mainTextureScale=new Vector2(.498f,.3313f);
                m.mainTextureOffset=new Vector2(panel%2*.5f+.001f,(2-panel/2)/3f+.001f);
                backdrop=Piece("Distant chapter scenery",Vector3.zero,Vector3.one,m,PrimitiveType.Quad).transform;
            }
        }
        void Prop(int index,Vector3 position,float height)
        {
            string key="PixelProps_"+(index/4)+"_"+(index%4);
            if(!props.TryGetValue(key,out var sprite))return;
            var g=new GameObject("Authored scenery "+index);g.transform.SetParent(transform,false);
            g.transform.localPosition=position;g.transform.localScale=Vector3.one*(height/sprite.bounds.size.y);
            var renderer=g.AddComponent<SpriteRenderer>();renderer.sharedMaterial=spriteMaterial;renderer.sprite=sprite;
            g.transform.rotation=view.transform.rotation;billboards.Add(g.transform);
        }
        public Material Surface(GridTile tile)
        {
            if(tile.Terrain==TerrainType.Water)return surfaces[8];
            if(!tile.Walkable)return surfaces[4];
            int variation=(tile.Coordinate.x*17+tile.Coordinate.y*31)%3;
            int x=tile.Coordinate.x,z=tile.Coordinate.y;
            bool weathered=(x==0||z==0||x==4&&z%3==0)&&stage<=1;
            return surfaces[stage==2?5:stage==3?12+variation%2:stage==4?7:stage==5?15:weathered?2:variation%2];
        }
        public Color Tint(GridTile tile)
        {
            float v=0.9f+((tile.Coordinate.x*13+tile.Coordinate.y*7)%5)*0.02f;
            return new Color(v,v,v,1);
        }
        GameObject Piece(string name,Vector3 position,Vector3 scale,Material material,PrimitiveType shape=PrimitiveType.Cube)
        {
            var g=GameObject.CreatePrimitive(shape);g.name=name;g.transform.SetParent(transform,false);
            g.transform.localPosition=position;g.transform.localScale=scale;
            Destroy(g.GetComponent<Collider>());var r=g.GetComponent<Renderer>();r.sharedMaterial=material;
            r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;return g;
        }
        public void Decorate(GridTile tile,GridMap map,float step)
        {
            var p=tile.WorldPosition(step);int x=tile.Coordinate.x,z=tile.Coordinate.y;
            // Foundation extends below the unchanged walkable surface.
            Piece("Masonry foundation",new Vector3(x,-0.48f+tile.Height*step/2,z),new Vector3(0.99f,0.5f+tile.Height*step,0.99f),stone);
            if(tile.Terrain==TerrainType.Water)
            {
                var foam=Piece("Water shimmer",p+new Vector3(0,0.018f,0),new Vector3(0.88f,0.015f,0.13f),surfaces[9]);
                water.Add(foam.GetComponent<Renderer>());
                waterHeight.Add(foam.transform.localPosition.y);
            }
            if(!tile.Walkable)
            {
                int v=(x+z)%3;
                int index=stage==2?(v==0?5:4):stage==3?(v==0?12:8):stage==4?(v==0?11:9):stage==5?10:stage==1?(v==0?6:0):(v==0?7:1);
                Prop(index,p+Vector3.up*0.02f,stage==2?1.05f:1.42f);
            }
            // Low edge ornaments never obscure units on valid movement tiles.
            var dirs=new[]{Vector2Int.left,Vector2Int.right,Vector2Int.up,Vector2Int.down};
            foreach(var d in dirs)
            {
                var n=tile.Coordinate+d;
                if(map.Tiles.ContainsKey(n))continue;
                var edge=p+new Vector3(d.x*0.44f,0.055f,d.y*0.44f);
                Piece("Edge coping",edge,new Vector3(d.x==0?0.95f:0.10f,0.1f,d.y==0?0.95f:0.10f),stone);
                if((x+z)%3==0&&stage!=5)
                    Piece("Trailing moss",edge+Vector3.down*0.2f,new Vector3(d.x==0?0.36f:0.08f,0.4f,d.y==0?0.36f:0.08f),moss);
                if((x+z)%5==0)
                {
                    Prop(stage==5?10:2,edge,0.48f);
                }
                else if((x+z)%3==1)Prop(stage==4?11:stage==2?4:stage==5?1:3,edge,0.3f);
            }
            if(stage==5&&x==6&&z==6)
            {Ring(p+Vector3.up*0.025f,1.35f,bronze,true);Ring(p+Vector3.up*0.029f,0.85f,surfaces[11],true);}
            if(stage==4&&(x==5||x==8)&&(z==2||z==7))
                for(int k=0;k<3;k++)Piece("Bridge brass rivet",p+new Vector3(-0.32f+k*0.32f,0.02f,0.37f),new Vector3(0.04f,0.02f,0.04f),bronze);
        }
        void Ring(Vector3 center,float radius,Material material,bool horizontal)
        {
            for(int i=0;i<16;i++)
            {
                float a=i*Mathf.PI/8;
                var offset=horizontal?new Vector3(Mathf.Cos(a),0,Mathf.Sin(a)):new Vector3(Mathf.Cos(a),Mathf.Sin(a),0);
                var g=Piece("Brass ring",center+offset*radius,horizontal?new Vector3(radius*0.4f,0.035f,0.04f):new Vector3(radius*0.4f,0.04f,0.04f),material);
                g.transform.localRotation=horizontal?Quaternion.Euler(0,-i*22.5f+90,0):Quaternion.Euler(0,0,i*22.5f+90);
            }
        }
        void Update()
        {
            // Small surface motion, independent from gameplay and deterministic RNG.
            if(surfaces==null)return;
            for(int i=0;i<water.Count;i++)
            {
                var t=water[i].transform;var p=t.localPosition;
                p.y=waterHeight[i]+Mathf.Sin(Time.time*2+i)*0.008f;t.localPosition=p;
            }
        }
        void LateUpdate()
        {
            if(view==null)return;
            foreach(var t in billboards)t.rotation=view.transform.rotation;
            if(backdrop!=null)
            {
                backdrop.SetPositionAndRotation(view.transform.position+view.transform.forward*Mathf.Min(80,view.farClipPlane-1),view.transform.rotation);
                backdrop.localScale=new Vector3(view.orthographicSize*2*view.aspect,view.orthographicSize*2,1);
            }
        }
        void OnDestroy(){foreach(var m in owned)if(m!=null)Destroy(m);}
    }
}
